/*

 Copyright (c) 2007- DEVSENSE
 Copyright (c) 2004-2006 Tomas Matousek and Vaclav Novak.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.SymbolStore;

using PHP.Core.AST;
using PHP.Core.Emit;
using PHP.Core.Parsers;
using PHP.Core.Reflection;

namespace PHP.Core.Compiler.AST
{
    partial class NodeCompilers
    {
        #region FormalParam

        [NodeCompiler(typeof(FormalParam))]
        sealed class FormalParamCompiler : INodeCompiler, IFormalParamCompiler
        {
            #region CustomAttributeProvider

            private sealed class CustomAttributeProvider : IPhpCustomAttributeProvider
            {
                private readonly FormalParam node;

                public CustomAttributeProvider(FormalParam node)
                {
                    this.node = node;
                }

                #region IPhpCustomAttributeProvider

                public PhpAttributeTargets AttributeTarget { get { return PhpAttributeTargets.Parameter; } }
                public AttributeTargets AcceptsTargets { get { return AttributeTargets.Parameter; } }

                public int GetAttributeUsageCount(DType/*!*/ type, CustomAttribute.TargetSelectors selector)
                {
                    var attributes = node.Attributes;
                    if (attributes != null)
                        return attributes.Count(type, selector);
                    else
                        return 0;
                }

                public void ApplyCustomAttribute(SpecialAttributes kind, Attribute attribute, CustomAttribute.TargetSelectors selector)
                {
                    Debug.Assert(selector == CustomAttribute.TargetSelectors.Default);

                    switch (kind)
                    {
                        case SpecialAttributes.Out:
                            node.IsOut = true;
                            break;

                        default:
                            Debug.Fail("N/A");
                            throw null;
                    }
                }

                public void EmitCustomAttribute(CustomAttributeBuilder/*!*/ builder, CustomAttribute.TargetSelectors selector)
                {
                    Debug.Assert(selector == CustomAttribute.TargetSelectors.Default);

                    var nodecompiler = node.NodeCompiler<FormalParamCompiler>();
                    nodecompiler.routine.Builder.ParameterBuilders[nodecompiler.routine.FirstPhpParameterIndex + nodecompiler.index].SetCustomAttribute(builder);
                }

                #endregion
            }

            #endregion

            public DType ResolvedTypeHint { get { return resolvedTypeHint; } }
            private DType resolvedTypeHint;

            /// <summary>
            /// Declaring routine.
            /// </summary>
            private PhpRoutine/*!A*/ routine;

            /// <summary>
            /// Index in the <see cref="Signature"/> tuple.
            /// </summary>
            private int index;
            
            #region Analysis

            internal void AnalyzeMembers(FormalParam/*!*/node, Analyzer/*!*/ analyzer, PhpRoutine/*!*/ routine, int index)
            {
                this.routine = routine;
                this.index = index;

                PhpType referring_type;
                Scope referring_scope;

                if (routine.IsMethod)
                {
                    referring_type = routine.DeclaringPhpType;
                    referring_scope = referring_type.Declaration.Scope;
                }
                else if (routine.IsLambdaFunction)
                {
                    referring_type = analyzer.CurrentType;
                    referring_scope = analyzer.CurrentScope;
                }
                else
                {
                    referring_type = null;
                    referring_scope = ((PhpFunction)routine).Declaration.Scope;
                }

                var attributes = node.Attributes;
                if (attributes != null)
                {
                    attributes.AnalyzeMembers(analyzer, referring_scope);
                }

                resolvedTypeHint = analyzer.ResolveType(node.TypeHint, referring_type, routine, node.Span, false);
            }

            internal void Analyze(FormalParam/*!*/node, Analyzer/*!*/ analyzer)
            {
                var attributes = node.Attributes;
                if (attributes != null)
                {
                    node.Attributes.Analyze(analyzer, new CustomAttributeProvider(node));
                }

                if (node.InitValue != null)
                    node.InitValue = node.InitValue.Analyze(analyzer, ExInfoFromParent.DefaultExInfo).Literalize();

                // adds arguments to local variables table:
                if (!routine.Builder.LocalVariables.AddParameter(node.Name, node.PassedByRef))
                {
                    // parameter with the same name specified twice
                    analyzer.ErrorSink.Add(Errors.DuplicateParameterName, analyzer.SourceUnit, node.Span, node.Name);
                }

                if (node.IsOut && !node.PassedByRef)
                {
                    // out can be used only on by-ref params:
                    analyzer.ErrorSink.Add(Errors.OutAttributeOnByValueParam, analyzer.SourceUnit, node.Span, node.Name);
                }
            }

            #endregion

            #region Emission

            /// <summary>
            /// Emits type hint test on the argument if specified.
            /// </summary>
            public void EmitTypeHintTest(FormalParam/*!*/node, CodeGenerator/*!*/ codeGenerator)
            {
                int real_index = routine.FirstPhpParameterIndex + index;

                // not type hint specified:
                if (node.TypeHint == null) return;

                Debug.Assert(resolvedTypeHint != null);

                ILEmitter il = codeGenerator.IL;
                Label endif_label = il.DefineLabel();

                // IF (DEREF(ARG[argIdx]) is not of hint type) THEN
                il.Ldarg(real_index);
                if (node.PassedByRef) il.Emit(OpCodes.Ldfld, Fields.PhpReference_Value);

                resolvedTypeHint.EmitInstanceOf(codeGenerator, null);
                il.Emit(OpCodes.Brtrue, endif_label);

                // add a branch allowing null values if the argument is optional with null default value (since PHP 5.1.0);
                if (node.InitValue != null && node.InitValue.HasValue() && node.InitValue.GetValue() == null)
                {
                    // IF (DEREF(ARG[argIdx]) != null) THEN
                    il.Ldarg(real_index);
                    if (node.PassedByRef) il.Emit(OpCodes.Ldfld, Fields.PhpReference_Value);
                    il.Emit(OpCodes.Brfalse, endif_label);
                }

                // CALL PhpException.InvalidArgumentType(<param_name>, <class_name>);
                il.Emit(OpCodes.Ldstr, node.Name.ToString());
                il.Emit(OpCodes.Ldstr, resolvedTypeHint.FullName);
                codeGenerator.EmitPhpException(Methods.PhpException.InvalidArgumentType);

                // END IF;
                // END IF;
                il.MarkLabel(endif_label);
            }

            internal void Emit(FormalParam/*!*/node, CodeGenerator/*!*/ codeGenerator)
            {
                var attributes = node.Attributes;
                if (attributes != null)
                {
                    attributes.Emit(codeGenerator, new CustomAttributeProvider(node));
                }

                // persists type hint to the [TypeHint] attribute: 
                if (resolvedTypeHint != null)
                {
                    ParameterBuilder param_builder = routine.Builder.ParameterBuilders[routine.FirstPhpParameterIndex + index];
                    DTypeSpec spec = resolvedTypeHint.GetTypeSpec(codeGenerator.SourceUnit);
                    param_builder.SetCustomAttribute(spec.ToCustomAttributeBuilder());
                }
            }

            #endregion
        }

        #endregion

        #region Signature

        struct SignatureCompiler
        {
            internal static void AnalyzeMembers(Signature node, Analyzer/*!*/ analyzer, PhpRoutine/*!*/ routine)
            {
                int last_mandatory_param_index = -1;
                bool last_param_was_optional = false;
                var formalParams = node.FormalParams;
                BitArray alias_mask = new BitArray(formalParams.Length);
                DType[] type_hints = new DType[formalParams.Length];

                for (int i = 0; i < formalParams.Length; i++)
                {
                    var param = formalParams[i];
                    var paramcompiler = param.NodeCompiler<FormalParamCompiler>();

                    paramcompiler.AnalyzeMembers(param, analyzer, routine, i);

                    alias_mask[i] = param.PassedByRef;
                    type_hints[i] = paramcompiler.ResolvedTypeHint;

                    if (param.InitValue == null)
                    {
                        if (last_param_was_optional)
                        {
                            analyzer.ErrorSink.Add(Warnings.MandatoryBehindOptionalParam, analyzer.SourceUnit,
                                param.Span, param.Name);
                        }

                        last_mandatory_param_index = i;
                        last_param_was_optional = false;
                    }
                    else
                        last_param_was_optional = true;
                }

                routine.Signature.WriteUp(node.AliasReturn, alias_mask, type_hints, last_mandatory_param_index + 1);
            }

            internal static void Analyze(Signature node, Analyzer/*!*/ analyzer)
            {
                foreach (FormalParam param in node.FormalParams)
                    param.NodeCompiler<FormalParamCompiler>()
                        .Analyze(param, analyzer);
            }

            internal static void Emit(Signature node, CodeGenerator/*!*/ codeGenerator)
            {
                foreach (FormalParam param in node.FormalParams)
                    param.NodeCompiler<FormalParamCompiler>()
                        .Emit(param, codeGenerator);
            }
        }

        #endregion

        #region FunctionDecl

        [NodeCompiler(typeof(FunctionDecl))]
        sealed class FunctionDeclCompiler : StatementCompiler<FunctionDecl>, IFunctionDeclCompiler, IDeclarationNode, IPhpCustomAttributeProvider
        {
            public PhpFunction/*!*/ Function { get { return function; } }
            private readonly PhpFunction/*!*/ function;

            private readonly FunctionDecl/*!*/node;

            #region Construction

            public FunctionDeclCompiler(FunctionDecl/*!*/node)
            {
                this.node = node;

                QualifiedName qn = (node.Namespace != null)
                    ? new QualifiedName(node.Name, node.Namespace.QualifiedName)
                    : new QualifiedName(node.Name);

                function = new PhpFunction(
                    qn, node.MemberAttributes, node.Signature, node.TypeSignature,
                    node.IsConditional, node.Scope, (CompilationSourceUnit)node.SourceUnit, node.Span);

                function.WriteUp(node.TypeSignature.ToPhpRoutineSignature(function));
                function.Declaration.Node = this;
            }
            
            #endregion

            #region Analysis

            void IDeclarationNode.PreAnalyze(Analyzer/*!*/ analyzer)
            {
                TypeSignatureCompiler.PreAnalyze(node.TypeSignature, analyzer, function);

                if (function.Version.Next != null)
                    function.Version.Next.Declaration.Node.PreAnalyze(analyzer);
            }

            void IDeclarationNode.AnalyzeMembers(Analyzer/*!*/ analyzer)
            {
                var attributes = node.Attributes;
                if (attributes != null)
                    attributes.AnalyzeMembers(analyzer, function.Declaration.Scope);

                TypeSignatureCompiler.AnalyzeMembers(node.TypeSignature, analyzer, function.Declaration.Scope);
                SignatureCompiler.AnalyzeMembers(node.Signature, analyzer, function);

                // member-analyze the other versions:
                if (function.Version.Next != null)
                    function.Version.Next.Declaration.Node.AnalyzeMembers(analyzer);

                function.Declaration.Node = null;
            }

            internal override Statement Analyze(FunctionDecl node, Analyzer analyzer)
            {
                // functions in incomplete (not emitted) class can't be emitted
                function.Declaration.IsInsideIncompleteClass = analyzer.IsInsideIncompleteClass();

                var attributes = node.Attributes;
                if (attributes != null)
                    attributes.Analyze(analyzer, this);
                
                // function is analyzed even if it is unreachable in order to discover more errors at compile-time:
                function.Declaration.IsUnreachable = analyzer.IsThisCodeUnreachable();

                if (function.Declaration.IsUnreachable)
                    analyzer.ReportUnreachableCode(node.Span);

                analyzer.EnterFunctionDeclaration(function);

                TypeSignatureCompiler.Analyze(node.TypeSignature, analyzer);
                SignatureCompiler.Analyze(node.Signature, analyzer);

                function.Validate(analyzer.ErrorSink);

                node.Body.Analyze(analyzer);

                // validate function and its body:
                function.ValidateBody(analyzer.ErrorSink);

                /*
                if (docComment != null)
                    AnalyzeDocComment(analyzer);
                */

                analyzer.LeaveFunctionDeclaration();

                if (function.Declaration.IsUnreachable)
                {
                    return EmptyStmt.Unreachable;
                }
                else
                {
                    // add entry point if applicable:
                    analyzer.SetEntryPoint(function, node.Span);
                    return node;
                }
            }

            #endregion

            #region Emission

            internal override void Emit(FunctionDecl node, CodeGenerator codeGenerator)
            {
                Statistics.AST.AddNode("FunctionDecl");

                // marks a sequence point if function is declared here (i.e. is m-decl):
                //Note: this sequence point goes to the function where this function is declared not to this declared function!
                if (!function.IsLambda && function.Declaration.IsConditional)
                    codeGenerator.MarkSequencePoint(node.Span);

                // emits attributes on the function itself, its return value, type parameters and regular parameters:
                var attributes = node.Attributes;
                if (attributes != null)
                    attributes.Emit(codeGenerator, this);
                SignatureCompiler.Emit(node.Signature, codeGenerator);
                TypeSignatureCompiler.Emit(node.TypeSignature, codeGenerator);

                // prepares code generator for emitting arg-full overload;
                // false is returned when the body should not be emitted:
                if (!codeGenerator.EnterFunctionDeclaration(function)) return;

                // emits the arg-full overload:
                codeGenerator.EmitArgfullOverloadBody(function, node.Body, node.EntireDeclarationPosition, node.DeclarationBodyPosition);

                // restores original code generator settings:
                codeGenerator.LeaveFunctionDeclaration();

                // emits function declaration (if needed):
                // ignore s-decl function declarations except for __autoload;
                // __autoload function is declared in order to avoid using callbacks when called:
                if (function.Declaration.IsConditional && !function.QualifiedName.IsAutoloadName)
                {
                    Debug.Assert(!function.IsLambda);
                    codeGenerator.EmitDeclareFunction(function);
                }
            }

            #endregion

            #region IPhpCustomAttributeProvider Members

            public PhpAttributeTargets AttributeTarget { get { return PhpAttributeTargets.Function; } }

            public AttributeTargets AcceptsTargets
            {
                get { return AttributeTargets.Method | AttributeTargets.ReturnValue; }
            }

            public int GetAttributeUsageCount(DType/*!*/ type, CustomAttribute.TargetSelectors selector)
            {
                var attributes = node.Attributes;
                if (attributes != null)
                    return attributes.Count(type, selector);
                else
                    return 0;
            }

            public void ApplyCustomAttribute(SpecialAttributes kind, Attribute attribute, CustomAttribute.TargetSelectors selector)
            {
                switch (kind)
                {
                    case SpecialAttributes.Export:
                        function.Builder.ExportInfo = (ExportAttribute)attribute;
                        break;

                    default:
                        Debug.Fail("N/A");
                        throw null;
                }
            }

            public void EmitCustomAttribute(CustomAttributeBuilder/*!*/ builder, CustomAttribute.TargetSelectors selector)
            {
                if (selector == CustomAttribute.TargetSelectors.Return)
                {
                    function.Builder.ReturnParamBuilder.SetCustomAttribute(builder);
                }
                else
                {
                    // custom attributes ignored on functions in evals:
                    ReflectionUtils.SetCustomAttribute(function.ArgFullInfo, builder);
                }
            }

            #endregion

            public PhpFunction/*!*/ ConvertToLambda(Analyzer/*!*/ analyzer)
            {
                function.ConvertToLambda();

                // perform pre- and member- analyses:
                ((IDeclarationNode)this).PreAnalyze(analyzer);
                ((IDeclarationNode)this).AnalyzeMembers(analyzer);

                // full analysis is performed later //

                return function;
            }
        }

        #endregion
    }

    #region IFormalParamCompiler

    internal interface IFormalParamCompiler
    {
        void EmitTypeHintTest(FormalParam/*!*/node, CodeGenerator/*!*/ codeGenerator);
    }

    internal static class FormalParamCompilerHelper
    {
        public static void EmitTypeHintTest(this FormalParam/*!*/node, CodeGenerator/*!*/ codeGenerator)
        {
            node.NodeCompiler<IFormalParamCompiler>().EmitTypeHintTest(node, codeGenerator);
        }
    }

    #endregion

    #region IFunctionDeclCompiler

    internal interface IFunctionDeclCompiler : IStatementCompiler
    {
        PhpFunction/*!*/ Function { get; }
        PhpFunction/*!*/ ConvertToLambda(Analyzer/*!*/ analyzer);
    }

    /// <summary>
    /// Helper class for accessing function declaration compiler methods.
    /// </summary>
    public static class FunctionDeclCompilerHelper
    {
        public static PhpFunction/*!*/ GetFunction(this FunctionDecl node)
        {
            return node.NodeCompiler<IFunctionDeclCompiler>().Function;
        }
        public static PhpFunction/*!*/ ConvertToLambda(this FunctionDecl node, Analyzer/*!*/ analyzer)
        {
            return node.NodeCompiler<IFunctionDeclCompiler>().ConvertToLambda(analyzer);
        }
    }

    #endregion
}
