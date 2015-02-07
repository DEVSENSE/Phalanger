/*

 Copyright (c) 2007- DEVSENSE
 Copyright (c) 2004-2006 Tomas Matousek, Ladislav Prosek and Vaclav Novak.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;

using PHP.Core.AST;
using PHP.Core.Emit;
using PHP.Core.Parsers;
using PHP.Core.Reflection;

namespace PHP.Core.Compiler.AST
{
    partial class NodeCompilers
    {
        #region FormalTypeParam

        [NodeCompiler(typeof(FormalTypeParam))]
        sealed class FormalTypeParamCompiler : INodeCompiler, IFormalTypeParamCompiler
        {
            #region IPhpCustomAttributeProvider

            sealed class PhpCustomAttributeProvider : IPhpCustomAttributeProvider
            {
                private readonly FormalTypeParam/*!*/node;

                public PhpCustomAttributeProvider(FormalTypeParam/*!*/node)
                {
                    this.node = node;
                }

                #region IPhpCustomAttributeProvider Members

                public PhpAttributeTargets AttributeTarget
                {
                    get { return PhpAttributeTargets.GenericParameter; }
                }

                public AttributeTargets AcceptsTargets
                {
                    get { return AttributeTargets.GenericParameter; }
                }

                public int GetAttributeUsageCount(DType/*!*/ type, CustomAttribute.TargetSelectors selector)
                {
                    var attributes = node.Attributes;
                    return (attributes != null) ? attributes.Count(type, selector) : 0;
                }

                public void ApplyCustomAttribute(SpecialAttributes kind, Attribute attribute, CustomAttribute.TargetSelectors selector)
                {
                    Debug.Fail("N/A");
                }

                public void EmitCustomAttribute(CustomAttributeBuilder/*!*/ builder, CustomAttribute.TargetSelectors selector)
                {
                    Debug.Assert(selector == CustomAttribute.TargetSelectors.Default);
                    node.NodeCompiler<FormalTypeParamCompiler>().parameter.SetCustomAttribute(builder);
                }

                #endregion
            }

            #endregion

            private GenericParameter/*! PreAnalyze */ parameter;

            #region Analysis

            public void PreAnalyze(FormalTypeParam/*!*/node, Analyzer/*!*/ analyzer, GenericParameter/*!*/ parameter)
            {
                this.parameter = parameter;

                PhpRoutine routine = parameter.DeclaringMember as PhpRoutine;
                PhpType type = (routine != null) ? routine.DeclaringType as PhpType : parameter.DeclaringPhpType;

                parameter.WriteUp(analyzer.ResolveType(node.DefaultType, type, routine, node.Span, false));
            }

            public bool Merge(FormalTypeParam/*!*/node, ErrorSink/*!*/ errors, FormalTypeParam/*!*/ other)
            {
                if (node.Name != other.Name)
                {
                    PhpType declaring_type = (PhpType)parameter.DeclaringMember;

                    errors.Add(Errors.PartialDeclarationsDifferInTypeParameter, declaring_type.Declaration.SourceUnit, node.Span, declaring_type.FullName);

                    return false;
                }

                CustomAttributes.Merge(node, other.Attributes);

                return true;
            }

            public void AnalyzeMembers(FormalTypeParam/*!*/node, Analyzer/*!*/ analyzer, Scope referringScope)
            {
                var attributes = node.Attributes;
                if (attributes != null)
                    attributes.AnalyzeMembers(analyzer, referringScope);
            }

            public void Analyze(FormalTypeParam/*!*/node, Analyzer/*!*/ analyzer)
            {
                var attributes = node.Attributes;
                if (attributes != null)
                    attributes.Analyze(analyzer, new PhpCustomAttributeProvider(node));
            }

            #endregion

            #region Emission

            /// <summary>
            /// Parameters on generic types.
            /// </summary>
            public void Emit(FormalTypeParam/*!*/node, CodeGenerator/*!*/ codeGenerator)
            {
                var attributes = node.Attributes;
                if (attributes != null)
                    attributes.Emit(codeGenerator, new PhpCustomAttributeProvider(node));

                // persists default type to the [TypeHint] attribute: 
                if (parameter.DefaultType != null)
                {
                    DTypeSpec spec = parameter.DefaultType.GetTypeSpec(codeGenerator.SourceUnit);
                    parameter.SetCustomAttribute(spec.ToCustomAttributeBuilder());
                }
            }

            #endregion
        }

        #endregion

        #region TypeSignature

        struct TypeSignatureCompiler
        {
            #region Analysis

            public static void PreAnalyze(TypeSignature/*!*/node, Analyzer/*!*/ analyzer, PhpType/*!*/ declaringType)
            {
                Debug.Assert(analyzer != null && declaringType != null);

                if (node.TypeParams == null) return;

                Debug.Assert(declaringType.GenericParams.Length == node.TypeParams.Length);

                for (int i = 0; i < node.TypeParams.Length; i++)
                    node.TypeParams[i].PreAnalyze(analyzer, declaringType.GetGenericParameter(i));
            }

            public static void PreAnalyze(TypeSignature/*!*/node, Analyzer/*!*/ analyzer, PhpRoutine/*!*/ declaringRoutine)
            {
                Debug.Assert(analyzer != null && declaringRoutine != null);

                if (node.TypeParams == null) return;

                Debug.Assert(declaringRoutine.Signature.GenericParamCount == node.TypeParams.Length);

                for (int i = 0; i < node.TypeParams.Length; i++)
                    node.TypeParams[i].PreAnalyze(analyzer, declaringRoutine.Signature.GenericParams[i]);
            }

            public static bool Merge(TypeSignature/*!*/node, ErrorSink/*!*/ errors, PhpType/*!*/ declaringType, TypeSignature other)
            {
                if (node.TypeParams.Length != other.TypeParams.Length)
                {
                    errors.Add(Errors.PartialDeclarationsDifferInTypeParameterCount, declaringType.Declaration.SourceUnit,
                        declaringType.Declaration.Span, declaringType.FullName);

                    return false;
                }

                bool result = true;

                for (int i = 0; i < node.TypeParams.Length; i++)
                    result &= node.TypeParams[i].Merge(errors, other.TypeParams[i]);

                return result;
            }

            public static void AnalyzeMembers(TypeSignature/*!*/node, Analyzer/*!*/ analyzer, Scope referringScope)
            {
                foreach (FormalTypeParam param in node.TypeParams)
                    param.AnalyzeMembers(analyzer, referringScope);
            }

            public static void Analyze(TypeSignature/*!*/node, Analyzer/*!*/ analyzer)
            {
                foreach (FormalTypeParam param in node.TypeParams)
                    param.Analyze(analyzer);
            }

            #endregion

            #region Emission

            public static void Emit(TypeSignature/*!*/node, CodeGenerator/*!*/ codeGenerator)
            {
                foreach (FormalTypeParam param in node.TypeParams)
                    param.Emit(codeGenerator);
            }

            #endregion
        }

        #endregion

        #region TypeDecl

        [NodeCompiler(typeof(TypeDecl))]
        sealed class TypeDeclCompiler : StatementCompiler<TypeDecl>, ITypeDeclCompiler, IPhpCustomAttributeProvider, IDeclarationNode
        {
            #region Properties

            /// <summary>
            /// Type declaration node.
            /// </summary>
            private readonly TypeDecl node;

            /// <summary>
            /// Whether the node is partial and has been merged to another partial node (arggregate).
            /// </summary>
            private bool IsPartialMergeResiduum(TypeDecl node) { return node.Members == null; }

            /// <summary>
            /// Item of the table of classes. Partial classes merged to the aggregate has this field <B>null</B>ed.
            /// </summary>
            public PhpType Type { get { return type; } }
            private PhpType type;

            /// <summary>
            /// Code of the class used when declared deferred in Eval.
            /// </summary>
            private string typeDefinitionCode = null;

            #endregion

            #region Construction

            public TypeDeclCompiler(TypeDecl node)
            {
                this.node = node;

                // create stuff necessary for inclusion resolving process, other structures are created duirng analysis:
                QualifiedName qn = (node.Namespace != null) ? new QualifiedName(node.Name, node.Namespace.QualifiedName) : new QualifiedName(node.Name);
                this.type = new PhpType(
                    qn, node.MemberAttributes, node.PartialKeyword, node.typeSignature,
                    node.IsConditional, node.Scope, (CompilationSourceUnit)node.SourceUnit, node.Span);

                //// add alias for private classes (if not added yet by partial declaration):
                //if (type.IsPrivate)
                //    sourceUnit.AddTypeAlias(qn, this.name);

                // member-analysis needs the node:
                this.type.Declaration.Node = this;
            }

            #endregion

            #region Pre-analysis

            /// <summary>
            /// Invoked before member-analysis on the primary types.
            /// All types are known at this point.
            /// </summary>
            void IDeclarationNode.PreAnalyze(Analyzer/*!*/ analyzer)
            {
                TypeSignatureCompiler.PreAnalyze(node.typeSignature, analyzer, type);

                // all types are known:
                DTypeDesc base_type = ResolveBaseType(analyzer);
                List<DTypeDesc> base_interfaces = new List<DTypeDesc>(node.ImplementsList.Length);
                ResolveBaseInterfaces(analyzer, base_interfaces);

                // pre-analyze the other versions (include partial types merging):
                if (type.Version.Next != null)
                {
                    if (type.Declaration.IsPartial)
                    {
                        var nextPartialDecl = (TypeDeclCompiler)type.Version.Next.Declaration.Node;
                        nextPartialDecl.PreAnalyzePartialDeclaration(analyzer, this,
                            ref base_type, base_interfaces);

                        // drop the partial type version info:
                        type.Version = new VersionInfo(0, null);
                    }
                    else
                        type.Version.Next.Declaration.Node.PreAnalyze(analyzer);
                }

                type.TypeDesc.WriteUpBaseType(base_type);
                type.Builder.BaseInterfaces = base_interfaces;
            }

            private void PreAnalyzePartialDeclaration(Analyzer/*!*/ analyzer, TypeDeclCompiler/*!*/ aggregate,
                ref DTypeDesc aggregateBase, List<DTypeDesc>/*!*/ aggregateInterfaces)
            {
                //
                // little hack, change the sourceUnit in order to match the current partial class declaration with the right file and imported namespaces
                //
                var current_sourceUnit = analyzer.SourceUnit;
                analyzer.SourceUnit = this.type.Declaration.SourceUnit;
                //
                //
                //

                try
                {
                    bool valid = true;

                    if (type.IsInterface != aggregate.type.IsInterface)
                    {
                        analyzer.ErrorSink.Add(Errors.IncompatiblePartialDeclarations, type.Declaration.SourceUnit, type.Declaration.Span, type.FullName);
                        analyzer.ErrorSink.Add(Errors.RelatedLocation, aggregate.type.Declaration.SourceUnit, aggregate.type.Declaration.Span);
                        valid = false;
                    }

                    if (type.Visibility != aggregate.type.Visibility)
                    {
                        analyzer.ErrorSink.Add(Errors.ConflictingPartialVisibility, type.Declaration.SourceUnit, type.Declaration.Span, type.FullName);
                        analyzer.ErrorSink.Add(Errors.RelatedLocation, aggregate.type.Declaration.SourceUnit, aggregate.type.Declaration.Span);
                        valid = false;
                    }

                    // merge base types:
                    DTypeDesc base_type = ResolveBaseType(analyzer);
                    if (base_type != null)
                    {
                        if (aggregateBase != null)
                        {
                            if (!base_type.Type.Equals(aggregateBase.Type)) //Edited by Ðonny Jan 07 2009 - missing "!"?
                            {
                                analyzer.ErrorSink.Add(Errors.PartialDeclarationsDifferInBase, type.Declaration.SourceUnit, type.Declaration.Span, type.FullName);
                                analyzer.ErrorSink.Add(Errors.RelatedLocation, aggregate.type.Declaration.SourceUnit, aggregate.type.Declaration.Span);
                                valid = false;
                            }
                        }
                        else
                        {
                            aggregateBase = base_type;
                        }
                    }

                    TypeSignatureCompiler.PreAnalyze(node.typeSignature, analyzer, type);

                    // merge generic parameters:
                    valid &= TypeSignatureCompiler.Merge(aggregate.node.typeSignature, analyzer.ErrorSink, this.type, node.typeSignature);

                    // move members to the aggregate:
                    node.Members.ForEach(member => member.NodeCompiler<ITypeMemberDeclCompiler>().SourceUnit = analyzer.SourceUnit); // override SourceUnit of the members to match the debug information and imported namespaces properly furing the analysis
                    aggregate.node.Members.AddRange(node.Members);
                    node.Members = null;

                    // move attributes to the aggregate:
                    CustomAttributes.Merge(aggregate.node, node.Attributes);

                    // merge interfaces:
                    ResolveBaseInterfaces(analyzer, aggregateInterfaces);

                    // next partial declaration;
                    // (if the declaration is erroneous, stop analysis before reporting more messy errors):
                    if (valid && type.Version.Next != null)
                    {
                        ((TypeDeclCompiler)type.Version.Next.Declaration.Node).PreAnalyzePartialDeclaration(analyzer, aggregate,
                            ref aggregateBase, aggregateInterfaces);
                    }
                }
                finally
                {
                    // cut the AST off the tables:
                    type.Declaration.Node = null;
                    type = null;

                    //
                    // change the sourceUnit back
                    //
                    analyzer.SourceUnit = current_sourceUnit;
                }
            }

            private DTypeDesc ResolveBaseType(Analyzer/*!*/ analyzer)
            {
                if (node.BaseClassName.HasValue)
                {
                    DType base_type = analyzer.ResolveTypeName(node.BaseClassName.Value, type, null, node.Span, true);

                    if (base_type.IsGenericParameter)
                    {
                        analyzer.ErrorSink.Add(Errors.CannotDeriveFromTypeParameter, analyzer.SourceUnit, node.Span, base_type.FullName);
                        return null;
                    }
                    else if (base_type.IsIdentityDefinite)
                    {
                        if (base_type.IsInterface)
                        {
                            analyzer.ErrorSink.Add(Errors.NonClassExtended, analyzer.SourceUnit, node.Span, base_type.FullName);
                            return null;
                        }
                        else if (base_type.IsFinal)
                        {
                            analyzer.ErrorSink.Add(Errors.FinalClassExtended, analyzer.SourceUnit, node.Span, base_type.FullName);
                            // do not remove the base class to make the further error reports consistent
                        }
                    }

                    return base_type.TypeDesc;
                }
                else
                    return null;
            }

            private void ResolveBaseInterfaces(Analyzer/*!*/ analyzer, List<DTypeDesc>/*!*/ interfaces)
            {
                var implementsList = node.ImplementsList;
                var implementsSpan = node.ImplementsListPosition;

                for (int i = 0; i < implementsList.Length; i++)
                {
                    DType base_type = analyzer.ResolveTypeName(implementsList[i], type, null, implementsSpan[i], true);

                    if (base_type.IsGenericParameter)
                    {
                        analyzer.ErrorSink.Add(Errors.CannotDeriveFromTypeParameter, analyzer.SourceUnit, node.Span, base_type.FullName);
                    }
                    else if (base_type.IsIdentityDefinite && !base_type.IsInterface)
                    {
                        if (type.IsInterface)
                            analyzer.ErrorSink.Add(Errors.NonInterfaceExtended, analyzer.SourceUnit, node.Span, base_type.FullName);
                        else
                            analyzer.ErrorSink.Add(Errors.NonInterfaceImplemented, analyzer.SourceUnit, node.Span, base_type.FullName);
                    }
                    else
                    {
                        interfaces.Add(base_type.TypeDesc);
                    }
                }
            }

            #endregion

            #region Member Analysis

            /// <summary>
            /// Invoked by analyzer after all files are parsed and before the full analysis takes place.
            /// Invoked only on types directly stored on the compilation unit during parsing,
            /// i.e. invoked only on the primary version and not on the others.
            /// All types and their inheritance relationships are known at this point, partial types has already been merged.
            /// </summary>
            void IDeclarationNode.AnalyzeMembers(Analyzer/*!*/ analyzer)
            {
                var attributes = node.Attributes;
                if (attributes != null)
                    node.Attributes.AnalyzeMembers(analyzer, type.Declaration.Scope);

                TypeSignatureCompiler.AnalyzeMembers(node.typeSignature, analyzer, type.Declaration.Scope);

                // let members add themselves to the type:
                for (int i = 0; i < node.Members.Count; i++)
                    node.Members[i].AnalyzeMembers(analyzer, type);

                type.ValidateMembers(analyzer.ErrorSink);

                // analyze members of the other versions:
                if (type.Version.Next != null)
                {
                    var nextPartialDecl = type.Version.Next.Declaration.Node;

                    // little hack, change the SourceUnit during the analysis to match the partial declaration
                    var nextPartialDecl_sourceUnit = ((TypeDeclCompiler)nextPartialDecl).Type.Declaration.SourceUnit;
                    var current_sourceUnit = analyzer.SourceUnit;

                    analyzer.SourceUnit = nextPartialDecl_sourceUnit;
                    nextPartialDecl.AnalyzeMembers(analyzer);   // analyze partial class members
                    analyzer.SourceUnit = current_sourceUnit;
                }

                // cut the AST off the tables:
                type.Declaration.Node = null;
            }

            #endregion

            #region Analysis

            internal override Statement Analyze(TypeDecl node, Analyzer analyzer)
            {
                // remove classes that has been merged to the aggregate from the further processing:
                if (IsPartialMergeResiduum(node))
                    return EmptyStmt.PartialMergeResiduum;

                // functions in incomplete (not emitted) class can't be emitted
                type.Declaration.IsInsideIncompleteClass = analyzer.IsInsideIncompleteClass();

                // the ClassDecl is fully analyzed even if it will be replaced in the AST by EvalEx
                // and even if it is unreachable in order to discover all possible errors in compile-time

                type.Declaration.IsUnreachable = analyzer.IsThisCodeUnreachable();

                if (type.Declaration.IsUnreachable)
                    analyzer.ReportUnreachableCode(node.Span);

                var attributes = node.Attributes;
                if (attributes != null)
                    attributes.Analyze(analyzer, this);
                TypeSignatureCompiler.Analyze(node.typeSignature, analyzer);

                analyzer.EnterTypeDecl(type);

                foreach (var member in node.Members)
                {
                    var membercompiler = member.NodeCompiler<ITypeMemberDeclCompiler>();
                    membercompiler.EnterAnalyzer(analyzer);
                    membercompiler.Analyze(member, analyzer);
                    membercompiler.LeaveAnalyzer(analyzer);
                }

                analyzer.LeaveTypeDecl();

                // validate the type after all members has been analyzed and validated:
                type.Validate(analyzer.ErrorSink);

                if (type.Declaration.IsUnreachable)
                {
                    // only a conditional declaration can be unreachable
                    // => not emiting the declaration is ok

                    return EmptyStmt.Unreachable;
                }
                else if (!type.IsComplete)
                {
                    // mark all functions declared in incomplete class as 'non-compilable'

                    // convert incomplete class to an eval if applicable:
                    if (analyzer.SourceUnit.CompilationUnit.IsPure && analyzer.CurrentType == null &&
                        analyzer.CurrentRoutine == null)
                    {
                        // error, since there is no place for global code in pure mode:
                        analyzer.ErrorSink.Add(Errors.IncompleteClass, analyzer.SourceUnit, node.Span, node.Name);
                        return node;
                    }

                    if (analyzer.SourceUnit.CompilationUnit.IsTransient)
                    {
                        TransientCompilationUnit transient_unit = (TransientCompilationUnit)analyzer.SourceUnit.CompilationUnit;

                        // report an error only for synthetic evals as we are 100% sure that the class cannot be completed;
                        // note that a synthetic eval can be created even in transient code as some base types could be 
                        // declared there conditionally:
                        if (transient_unit.EvalKind == EvalKinds.SyntheticEval)
                        {
                            analyzer.ErrorSink.Add(Errors.IncompleteClass, analyzer.SourceUnit, node.Span, node.Name);
                            return node;
                        }
                    }

                    // report the warning, incomplete_class
                    analyzer.ErrorSink.Add(Warnings.IncompleteClass, analyzer.SourceUnit, node.Span, node.Name);

                    // TODO: instead of embedding class source code,
                    // embedd serialized AST, so avoid parsing again in runtime
                    this.typeDefinitionCode = analyzer.SourceUnit.GetSourceCode(node.EntireDeclarationPosition);
                    
                    // we emit eval
                    return node;
                }
                else
                {
                    return node;
                }
            }

            #endregion

            #region Emission

            internal void EmitDefinition(CodeGenerator/*!*/ codeGenerator)
            {
                if (type.IsComplete)
                {
                    Debug.Assert(type.IsComplete, "Incomplete types should be converted to evals.");
                    Debug.Assert(type.RealTypeBuilder != null, "A class declared during compilation should have a type builder.");

                    var attributes = node.Attributes;
                    if (attributes != null) 
                        attributes.Emit(codeGenerator, this);
                    TypeSignatureCompiler.Emit(node.typeSignature, codeGenerator);

                    codeGenerator.EnterTypeDeclaration(type);

                    foreach (TypeMemberDecl member in node.Members)
                    {
                        var membercompiler = member.NodeCompiler<ITypeMemberDeclCompiler>();
                        membercompiler.EnterCodegenerator(codeGenerator);
                        membercompiler.Emit(member, codeGenerator);
                        membercompiler.LeaveCodegenerator(codeGenerator);
                    }

                    // emit stubs for implemented methods & properties that were not declared by this type:
                    codeGenerator.EmitGhostStubs(type);

                    codeGenerator.LeaveTypeDeclaration();
                }
                else
                {
                    Debug.Assert(this.typeDefinitionCode != null);

                    // LOAD DynamicCode.Eval(<code>, context, definedVariables, self, includer, source, line, column, evalId)

                    // wrap Eval into static method
                    MethodBuilder method = codeGenerator.IL.TypeBuilder.DefineMethod(
                        string.Format("{0}{1}", ScriptModule.DeclareHelperNane, type.FullName),
                        MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.SpecialName,
                        Types.Void, Types.ScriptContext);

                    var il = new ILEmitter(method);

                    codeGenerator.EnterLambdaDeclaration(il, false, LiteralPlace.Null, new IndexedPlace(PlaceHolder.Argument, 0), LiteralPlace.Null, LiteralPlace.Null);
                    if (true)
                    {
                        codeGenerator.EmitEval(
                            EvalKinds.SyntheticEval,
                            LiteralUtils.Create(node.Span, this.typeDefinitionCode, AccessType.Read),
                            node.Span,
                            (node.Namespace != null) ? node.Namespace.QualifiedName : (QualifiedName?)null, node.validAliases);
                        il.Emit(OpCodes.Pop);
                        il.Emit(OpCodes.Ret);
                    }
                    codeGenerator.LeaveFunctionDeclaration();

                    //
                    il = codeGenerator.IL;

                    type.IncompleteClassDeclareMethodInfo = method;
                    type.IncompleteClassDeclarationId = String.Format("{0}${1}:{2}", type.FullName, unchecked((uint)codeGenerator.SourceUnit.SourceFile.ToString().GetHashCode()), node.Span.Start);

                    // sequence point here
                    codeGenerator.MarkSequencePoint(node.Span);

                    if (type.Declaration.IsConditional)
                    {
                        // CALL <Declare>.<FullName>(<context>)
                        codeGenerator.EmitLoadScriptContext();
                        il.Emit(OpCodes.Call, method);
                    }
                    else
                    {
                        // if (!<context>.IncompleteTypeDeclared(<id>))
                        //     CALL <Declare>.<FullName>(<context>)
                        var end_if = il.DefineLabel();

                        codeGenerator.EmitLoadScriptContext();
                        il.Emit(OpCodes.Ldstr, type.IncompleteClassDeclarationId);
                        il.Emit(OpCodes.Call, Methods.ScriptContext.IncompleteTypeDeclared);
                        il.Emit(OpCodes.Brtrue, end_if);
                        if (true)
                        {
                            codeGenerator.EmitLoadScriptContext();
                            il.Emit(OpCodes.Call, type.IncompleteClassDeclareMethodInfo);
                        }
                        il.MarkLabel(end_if);
                        il.ForgetLabel(end_if);
                    }
                }
            }

            internal void EmitDeclaration(CodeGenerator/*!*/ codeGenerator)
            {
                if (type.IsComplete)
                {
                    Debug.Assert(type.IsComplete, "Incomplete types should be converted to evals.");
                    Debug.Assert(type.RealTypeBuilder != null, "A class declared during compilation should have a type builder.");

                    if (type.Declaration.IsConditional)
                    {
                        ILEmitter il = codeGenerator.IL;

                        codeGenerator.MarkSequencePoint(node.Span);

                        // this class was conditionally declared, so we'll emit code that activates it:
                        type.EmitAutoDeclareOnScriptContext(il, codeGenerator.ScriptContextPlace);

                        if (codeGenerator.Context.Config.Compiler.Debug)
                            il.Emit(OpCodes.Nop);
                    }
                }
                else
                {
                    // declared in emitted Eval
                }
            }

            internal override void Emit(TypeDecl node, CodeGenerator codeGenerator)
            {
                Statistics.AST.AddNode("ClassDecl");
                EmitDeclaration(codeGenerator);
                EmitDefinition(codeGenerator);
            }

            #endregion

            #region IPhpCustomAttributeProvider Members

            public PhpAttributeTargets AttributeTarget { get { return PhpAttributeTargets.Class; } }

            public AttributeTargets AcceptsTargets
            {
                get
                {
                    return ((type.IsInterface) ? AttributeTargets.Interface : AttributeTargets.Class)
                        | AttributeTargets.Assembly | AttributeTargets.Module;
                }
            }

            public int GetAttributeUsageCount(DType/*!*/ type, CustomAttribute.TargetSelectors selector)
            {
                var attributes = node.Attributes;
                return (attributes != null) ? attributes.Count(type, selector) : 0;
            }

            public void ApplyCustomAttribute(SpecialAttributes kind, Attribute attribute, CustomAttribute.TargetSelectors selector)
            {
                switch (kind)
                {
                    case SpecialAttributes.AttributeUsage:
                        type.SetCustomAttributeUsage((AttributeUsageAttribute)attribute);
                        break;

                    case SpecialAttributes.Export:
                        type.Builder.ExportInfo = (ExportAttribute)attribute;
                        break;

                    default:
                        Debug.Fail("N/A");
                        throw null;
                }
            }

            public void EmitCustomAttribute(CustomAttributeBuilder/*!*/ builder, CustomAttribute.TargetSelectors selector)
            {
                type.RealTypeBuilder.SetCustomAttribute(builder);
            }

            #endregion
        }

        #endregion

        #region TypeMemberDecl

        abstract class TypeMemberDeclCompiler<T> : INodeCompiler, ITypeMemberDeclCompiler where T : TypeMemberDecl
        {
            #region SourceUnit nesting

            /// <summary>
            /// Overrides current sourceUnit for this TypeMemberDecl. This occurres
            /// when partial class is declared, after preanalysis, when members are
            /// merged into one TypeDecl.
            /// </summary>
            CompilationSourceUnit ITypeMemberDeclCompiler.SourceUnit { get { return this.sourceUnit; } set { this.sourceUnit = value; } }
            private CompilationSourceUnit sourceUnit = null;

            void ITypeMemberDeclCompiler.EnterAnalyzer(Analyzer/*!*/analyzer)
            {
                if (sourceUnit != null)
                {
                    if (currentSourceUnit != null)
                        throw new InvalidOperationException("TypeMemberDecl.EnterAnalyzer does not support nesting.");

                    currentSourceUnit = analyzer.SourceUnit;
                    analyzer.SourceUnit = sourceUnit;
                }
            }
            void ITypeMemberDeclCompiler.LeaveAnalyzer(Analyzer/*!*/analyzer)
            {
                if (sourceUnit != null)
                {
                    if (currentSourceUnit == null)
                        throw new InvalidOperationException("TypeMemberDecl.EnterAnalyzer was not called before.");

                    analyzer.SourceUnit = currentSourceUnit;
                    currentSourceUnit = null;
                }
            }
            void ITypeMemberDeclCompiler.EnterCodegenerator(CodeGenerator/*!*/ codeGenerator)
            {
                if (sourceUnit != null)
                {
                    if (currentSourceUnit != null)
                        throw new InvalidOperationException("TypeMemberDecl.EnterAnalyzer does not support nesting.");

                    currentSourceUnit = codeGenerator.SourceUnit;
                    codeGenerator.SourceUnit = sourceUnit;
                }
            }
            void ITypeMemberDeclCompiler.LeaveCodegenerator(CodeGenerator/*!*/ codeGenerator)
            {
                if (sourceUnit != null)
                {
                    if (currentSourceUnit == null)
                        throw new InvalidOperationException("TypeMemberDecl.EnterAnalyzer was not called before.");

                    codeGenerator.SourceUnit = currentSourceUnit;
                    currentSourceUnit = null;
                }
            }
            private CompilationSourceUnit currentSourceUnit = null;

            #endregion

            #region IPhpCustomAttributeProvider

            protected abstract class BasePhpCustomAttributeProvider : IPhpCustomAttributeProvider
            {
                protected readonly T node;

                public BasePhpCustomAttributeProvider(T node)
                {
                    this.node = node;
                }

                #region IPhpCustomAttributeProvider Members

                public abstract PhpAttributeTargets AttributeTarget { get; }
                public abstract AttributeTargets AcceptsTargets { get; }

                public abstract void EmitCustomAttribute(CustomAttributeBuilder/*!*/ builder, CustomAttribute.TargetSelectors selector);
                public abstract void ApplyCustomAttribute(SpecialAttributes kind, Attribute attribute, CustomAttribute.TargetSelectors selector);

                public int GetAttributeUsageCount(DType/*!*/ type, CustomAttribute.TargetSelectors selector)
                {
                    var attributes = node.Attributes;
                    if (attributes != null)
                        return attributes.Count(type, selector);
                    else
                        return 0;
                }

                #endregion
            }

            protected abstract IPhpCustomAttributeProvider CreatePhpCustomAttributeProvider(T/*!*/node);

            #endregion

            internal virtual void AnalyzeMembers(T/*!*/node, Analyzer/*!*/ analyzer, PhpType/*!*/ declaringType)
            {
                var attributes = node.Attributes;
                if (attributes != null)
                    attributes.AnalyzeMembers(analyzer, declaringType.Declaration.Scope);
            }

            internal virtual void Analyze(T/*!*/node, Analyzer/*!*/ analyzer)
            {
                var attributes = node.Attributes;
                if (attributes != null)
                    attributes.Analyze(analyzer, CreatePhpCustomAttributeProvider(node));
            }

            internal virtual void Emit(T/*!*/node, CodeGenerator/*!*/ codeGenerator)
            {
                var attributes = node.Attributes;
                if (attributes != null)
                    attributes.Emit(codeGenerator, CreatePhpCustomAttributeProvider(node));
            }

            #region ITypeMemberDeclCompiler Members

            void ITypeMemberDeclCompiler.AnalyzeMembers(TypeMemberDecl node, Analyzer analyzer, PhpType declaringType)
            {
                AnalyzeMembers((T)node, analyzer, declaringType);
            }

            void ITypeMemberDeclCompiler.Analyze(TypeMemberDecl node, Analyzer analyzer)
            {
                Analyze((T)node, analyzer);
            }

            void ITypeMemberDeclCompiler.Emit(TypeMemberDecl node, CodeGenerator codeGenerator)
            {
                Emit((T)node, codeGenerator);
            }

            #endregion
        }

        #endregion

        #region Methods

        [NodeCompiler(typeof(MethodDecl))]
        sealed class MethodDeclCompiler : TypeMemberDeclCompiler<MethodDecl>
        {
            #region IPhpCustomAttributeProvider

            sealed class PhpCustomAttributeProvider : BasePhpCustomAttributeProvider
            {
                private readonly PhpMethod method;

                public PhpCustomAttributeProvider(MethodDecl node, PhpMethod method)
                    :base(node)
                {
                    this.method = method;
                }

                #region IPhpCustomAttributeProvider Members
                
                public override PhpAttributeTargets AttributeTarget { get { return PhpAttributeTargets.Method; } }

                public override AttributeTargets AcceptsTargets
                {
                    get
                    {
                        return (method.IsConstructor ? AttributeTargets.Constructor : AttributeTargets.Method) |
                          AttributeTargets.ReturnValue;
                    }
                }

                public override void ApplyCustomAttribute(SpecialAttributes kind, Attribute attribute, CustomAttribute.TargetSelectors selector)
                {
                    switch (kind)
                    {
                        case SpecialAttributes.Export:
                            Debug.Assert(selector == CustomAttribute.TargetSelectors.Default);
                            method.Builder.ExportInfo = (ExportAttribute)attribute;
                            break;

                        default:
                            Debug.Fail("N/A");
                            break;
                    }
                }

                public override void EmitCustomAttribute(CustomAttributeBuilder/*!*/ builder, CustomAttribute.TargetSelectors selector)
                {
                    if (selector == CustomAttribute.TargetSelectors.Return)
                    {
                        method.Builder.ReturnParamBuilder.SetCustomAttribute(builder);
                    }
                    else
                    {
                        Debug.Assert(method.ArgLessInfo is MethodBuilder, "PHP methods cannot be dynamic");
                        ((MethodBuilder)method.ArgFullInfo).SetCustomAttribute(builder);
                    }
                }

                #endregion
            }

            protected override IPhpCustomAttributeProvider CreatePhpCustomAttributeProvider(MethodDecl node)
            {
                return new PhpCustomAttributeProvider(node, method);
            }

            #endregion

            /// <summary>
            /// Item in the table of methods or a <B>null</B> reference if added to the type yet or an error occured while adding.
            /// </summary>
            private PhpMethod method = null;

            #region Analysis

            internal override void AnalyzeMembers(MethodDecl node, Analyzer analyzer, PhpType declaringType)
            {
                method = declaringType.AddMethod(node.Name, node.Modifiers, node.Body != null,
                    node.Signature, node.TypeSignature, node.Span,
                    analyzer.SourceUnit, analyzer.ErrorSink);

                // method redeclared:
                if (method == null) return;

                method.WriteUp(node.TypeSignature.ToPhpRoutineSignature(method));

                TypeSignatureCompiler.PreAnalyze(node.TypeSignature, analyzer, method);

                base.AnalyzeMembers(node, analyzer, declaringType);

                TypeSignatureCompiler.AnalyzeMembers(node.TypeSignature, analyzer, declaringType.Declaration.Scope);
                SignatureCompiler.AnalyzeMembers(node.Signature, analyzer, method);
                method.IsDllImport = this.IsDllImport(node.Attributes);
                if (method.IsDllImport && node.Body.Any())
                    analyzer.ErrorSink.Add(Warnings.BodyOfDllImportedFunctionIgnored, analyzer.SourceUnit, node.Span);
            }

            internal override void Analyze(MethodDecl node, Analyzer analyzer)
            {
                // method redeclared:
                if (method == null) return;

                base.Analyze(node, analyzer);

                PhpType declaring_type = analyzer.CurrentType;

                analyzer.EnterMethodDeclaration(method);

                TypeSignatureCompiler.Analyze(node.TypeSignature, analyzer);
                SignatureCompiler.Analyze(node.Signature, analyzer);

                method.Validate(analyzer.ErrorSink);

                // note, if the declaring type's base is unknown then it cannot be a CLR type;
                ClrType base_clr_type = method.DeclaringType.Base as ClrType;

                if (node.BaseCtorParams != null)
                {
                    if (base_clr_type != null)
                    {
                        AnalyzeBaseCtorCallParams(node, analyzer, base_clr_type);
                    }
                    else if (!method.IsConstructor || method.DeclaringType.Base == null || node.Body == null)
                    {
                        analyzer.ErrorSink.Add(Errors.UnexpectedParentCtorInvocation, analyzer.SourceUnit, node.Span);
                        node.BaseCtorParams = null;
                    }
                    else if (method.DeclaringType.Base.Constructor == null)
                    {
                        // base class has no constructor, the default parameterless is silently called (and that does nothing);
                        // report error, if there are any parameters passed to the parameterless ctor:
                        if (node.BaseCtorParams.Length > 0)
                            analyzer.ErrorSink.Add(Errors.UnexpectedParentCtorInvocation, analyzer.SourceUnit, node.Span);
                        node.BaseCtorParams = null;
                    }
                    else
                    {
                        GenericQualifiedName parent_name = new GenericQualifiedName(new QualifiedName(Name.ParentClassName));
                        DirectStMtdCall call_expr = new DirectStMtdCall(
                            node.Span, parent_name, Text.Span.Invalid,
                            method.DeclaringType.Base.Constructor.Name, Text.Span.Invalid,
                            node.BaseCtorParams, TypeRef.EmptyList);

                        node.Body = ArrayUtils.Concat(new ExpressionStmt(node.Span, call_expr), node.Body);
                        node.BaseCtorParams = null;
                    }
                }
                else
                {
                    // the type immediately extends CLR type with no default ctor, yet there is no call to the base ctor;
                    // note, all constructor overloads reflected from the CLR type are visible as we are in a subclass:
                    if (method.IsConstructor && base_clr_type != null && !base_clr_type.ClrConstructor.HasParameterlessOverload)
                    {
                        analyzer.ErrorSink.Add(Errors.ExpectingParentCtorInvocation, analyzer.SourceUnit, node.Span);
                    }
                }
                if (method.IsDllImport && !method.IsStatic)
                    analyzer.ErrorSink.Add(Errors.DllImportMethodMustBeStatic, analyzer.SourceUnit, node.Span);
                if (method.IsDllImport && method.IsAbstract)
                    analyzer.ErrorSink.Add(Errors.DllImportMethodCannotBeAbstract, analyzer.SourceUnit, node.Span);

                if (node.Body != null)
                    node.Body.Analyze(analyzer);

                method.ValidateBody(analyzer.ErrorSink);

                analyzer.LeaveMethodDeclaration();

                // add entry point if applicable:
                analyzer.SetEntryPoint(method, node.Span);
            }

            private void AnalyzeBaseCtorCallParams(MethodDecl node, Analyzer/*!*/ analyzer, ClrType/*!*/ clrBase)
            {
                // we needn't to resolve the ctor here since the base class has to be known CLR type,
                // which has always a known ctor (may be a stub):
                ClrMethod base_ctor = clrBase.ClrConstructor;

                // create non-generic call signature:
                CallSignature call_sig = new CallSignature(node.BaseCtorParams, TypeRef.EmptyList);

                RoutineSignature signature;
                int overload_index = base_ctor.ResolveOverload(analyzer, call_sig, node.Span, out signature);

                if (overload_index == DRoutine.InvalidOverloadIndex)
                {
                    analyzer.ErrorSink.Add(Errors.ClassHasNoVisibleCtor, analyzer.SourceUnit, node.Span, clrBase.FullName);
                }
                else if (base_ctor.Overloads[overload_index].MandatoryParamCount != call_sig.Parameters.Length)
                {
                    // invalid argument count passed to the base ctor:
                    analyzer.ErrorSink.Add(Errors.InvalidArgumentCount, analyzer.SourceUnit, node.Span);
                }

                CallSignatureHelpers.Analyze(call_sig, analyzer, signature, AST.ExInfoFromParent.DefaultExInfo, true);

                // stores the signature on the type builder:
                method.DeclaringPhpType.Builder.BaseCtorCallSignature = call_sig;
                method.DeclaringPhpType.Builder.BaseCtorCallOverloadIndex = overload_index;

                // we don't need it any more:
                node.BaseCtorParams = null;
            }

            #endregion

            /// <summary>Gets value indicating if the method is decorated with <see cref="System.Runtime.InteropServices.DllImportAttribute"/></summary>
            internal bool IsDllImport(CustomAttributes attributes)
            {
                if (attributes == null || attributes.Attributes == null)
                    return false;

                foreach (CustomAttribute attr in attributes.Attributes)
                {
                    if (attr.GetResolvedType() == SpecialCustomAttribute.DllImportAttribute)
                        return true;
                }

                return false;
            }

            #region Emission

            internal override void Emit(MethodDecl node, CodeGenerator codeGenerator)
            {
                Statistics.AST.AddNode("Class.MethodDecl");

                base.Emit(node, codeGenerator);

                // emit attributes on return value, generic and regular parameters:
                SignatureCompiler.Emit(node.Signature, codeGenerator);
                TypeSignatureCompiler.Emit(node.TypeSignature, codeGenerator);

                if (method.IsDllImport)
                {
                    //TODO: Support for DllImport
                    Debug.Assert(false, "DllImport - not supported");
                }
                else if (!method.IsAbstract)
                {
                    // returns immediately if the method is abstract:
                    codeGenerator.EnterMethodDeclaration(method);

                    // emits the arg-full overload:
                    codeGenerator.EmitArgfullOverloadBody(method, node.Body, node.EntireDeclarationPosition, node.DeclarationBodyPosition);

                    // restores original code generator settings:
                    codeGenerator.LeaveMethodDeclaration();
                }
                else
                {
                    // static abstract method is non-abstract in CLR => needs to have a body:
                    if (method.IsStatic)
                    {
                        ILEmitter il = new ILEmitter(method.ArgFullInfo);
                        il.Emit(OpCodes.Ldstr, method.DeclaringType.FullName);
                        il.Emit(OpCodes.Ldstr, method.FullName);
                        codeGenerator.EmitPhpException(il, Methods.PhpException.AbstractMethodCalled);
                        il.Emit(OpCodes.Ldnull);
                        il.Emit(OpCodes.Ret);
                    }
                }

                // emits stubs for overridden/implemented methods and export stubs:
                codeGenerator.EmitOverrideAndExportStubs(method);
            }

            #endregion
        }

        #endregion

        #region Fields

        [NodeCompiler(typeof(FieldDeclList))]
        sealed class FieldDeclListCompiler : TypeMemberDeclCompiler<FieldDeclList>
        {
            #region IPhpCustomAttributeProvider

            sealed class PhpCustomAttributeProvider : BasePhpCustomAttributeProvider
            {
                public PhpCustomAttributeProvider(FieldDeclList node)
                    : base(node)
                {
                }

                #region IPhpCustomAttributeProvider Members

                public override PhpAttributeTargets AttributeTarget { get { return PhpAttributeTargets.Property; } }

                public override AttributeTargets AcceptsTargets
                {
                    get { return AttributeTargets.Property | AttributeTargets.Field; }
                }

                public override void ApplyCustomAttribute(SpecialAttributes kind, Attribute attribute, CustomAttribute.TargetSelectors selector)
                {
                    foreach (FieldDecl field in node.Fields)
                        field.ApplyCustomAttribute(kind, attribute, selector);
                }

                public override void EmitCustomAttribute(CustomAttributeBuilder/*!*/ builder, CustomAttribute.TargetSelectors selector)
                {
                    foreach (FieldDecl field in node.Fields)
                        field.EmitCustomAttribute(builder, selector);
                }

                #endregion
            }

            protected override IPhpCustomAttributeProvider CreatePhpCustomAttributeProvider(FieldDeclList node)
            {
                return new PhpCustomAttributeProvider(node);
            }

            #endregion

            internal override void AnalyzeMembers(FieldDeclList node, Analyzer analyzer, PhpType declaringType)
            {
                base.AnalyzeMembers(node, analyzer, declaringType);

                // no fields in interface:
                if (declaringType.IsInterface)
                {
                    analyzer.ErrorSink.Add(Errors.FieldInInterface, analyzer.SourceUnit, node.Span);
                    return;
                }

                foreach (FieldDecl field in node.Fields)
                {
                    PhpField php_field = declaringType.AddField(field.Name, node.Modifiers, field.HasInitVal, field.Span,
                        analyzer.SourceUnit, analyzer.ErrorSink);

                    field.AnalyzeMember(analyzer, php_field);
                }
            }

            internal override void Analyze(FieldDeclList node, Analyzer analyzer)
            {
                base.Analyze(node, analyzer);

                foreach (FieldDecl field in node.Fields)
                    field.Analyze(analyzer);
            }

            internal override void Emit(FieldDeclList node, CodeGenerator codeGenerator)
            {
                Statistics.AST.AddNode("Class.FieldDecl");
                base.Emit(node, codeGenerator);

                foreach (FieldDecl field in node.Fields)
                    field.Emit(codeGenerator);
            }
        }

        [NodeCompiler(typeof(FieldDecl))]
        public sealed class FieldDeclCompiler : INodeCompiler, IFieldDeclCompiler
        {
            /// <summary>
            /// Field representative, set by member analysis.
            /// </summary>
            public PhpField Field { get { return field; } }
            private PhpField field;

            void IFieldDeclCompiler.AnalyzeMember(FieldDecl/*!*/node, Analyzer/*!*/ analyzer, PhpField/*!*/ field)
            {
                this.field = field;
            }

            void IFieldDeclCompiler.Analyze(FieldDecl/*!*/node, Analyzer/*!*/ analyzer)
            {
                // field redeclared:
                if (field == null) return;

                if (node.Initializer != null)
                {
                    node.Initializer = node.Initializer.Analyze(analyzer, AST.ExInfoFromParent.DefaultExInfo).Literalize();
                }
            }

            void IFieldDeclCompiler.Emit(FieldDecl/*!*/node, CodeGenerator/*!*/ codeGenerator)
            {
                codeGenerator.InitializeField(field, node.Initializer);
                codeGenerator.EmitOverrideAndExportStubs(field);
            }

            void IFieldDeclCompiler.EmitCustomAttribute(CustomAttributeBuilder/*!*/ builder, CustomAttribute.TargetSelectors selector)
            {
                field.RealFieldBuilder.SetCustomAttribute(builder);
            }

            void IFieldDeclCompiler.ApplyCustomAttribute(SpecialAttributes kind, Attribute attribute, CustomAttribute.TargetSelectors selector)
            {
                // field redeclared:
                if (field == null) return;

                switch (kind)
                {
                    case SpecialAttributes.Export:
                        Debug.Assert(selector == CustomAttribute.TargetSelectors.Default);
                        field.Builder.ExportInfo = (ExportAttribute)attribute;
                        break;

                    case SpecialAttributes.AppStatic:
                        field.MemberDesc.MemberAttributes |= PhpMemberAttributes.AppStatic;
                        break;

                    default:
                        Debug.Fail("N/A");
                        throw null;
                }
            }
        }

        #endregion

        #region ConstDeclList

        [NodeCompiler(typeof(ConstDeclList))]
        sealed class ConstDeclListCompiler : TypeMemberDeclCompiler<ConstDeclList>
        {
            #region IPhpCustomAttributeProvider

            private class PhpCustomAttributeProvider : BasePhpCustomAttributeProvider
            {
                public PhpCustomAttributeProvider(ConstDeclList node)
                    : base(node)
                {
                }

                public override PhpAttributeTargets AttributeTarget { get { return PhpAttributeTargets.Constant; } }

                public override AttributeTargets AcceptsTargets
                {
                    get { return AttributeTargets.Field; }
                }

                public override void ApplyCustomAttribute(SpecialAttributes kind, Attribute attribute, CustomAttribute.TargetSelectors selector)
                {
                    foreach (ClassConstantDecl cd in node.Constants)
                        cd.ApplyCustomAttribute(kind, attribute, selector);
                }

                public override void EmitCustomAttribute(CustomAttributeBuilder/*!*/ builder, CustomAttribute.TargetSelectors selector)
                {
                    foreach (ClassConstantDecl cd in node.Constants)
                        cd.EmitCustomAttribute(builder, selector);
                }
            }

            #endregion

            protected override IPhpCustomAttributeProvider CreatePhpCustomAttributeProvider(ConstDeclList node)
            {
                return new PhpCustomAttributeProvider(node);
            }

            internal override void AnalyzeMembers(ConstDeclList node, Analyzer analyzer, PhpType declaringType)
            {
                base.AnalyzeMembers(node, analyzer, declaringType);

                foreach (ClassConstantDecl cd in node.Constants)
                {
                    // the value is filled later by full analysis:
                    ClassConstant php_constant = declaringType.AddConstant(cd.Name, node.Modifiers, cd.Span, analyzer.SourceUnit,
                        analyzer.ErrorSink);

                    cd.AnalyzeMember(analyzer, php_constant);
                }
            }

            internal override void Analyze(ConstDeclList node, Analyzer analyzer)
            {
                base.Analyze(node, analyzer);

                foreach (ClassConstantDecl cd in node.Constants)
                    cd.Analyze(analyzer);
            }

            internal override void Emit(ConstDeclList node, CodeGenerator codeGenerator)
            {
                base.Emit(node, codeGenerator);

                foreach (ClassConstantDecl cd in node.Constants)
                    cd.Emit(codeGenerator);
            }
        }

        #endregion

        #region ClassConstantDecl

        [NodeCompiler(typeof(ClassConstantDecl))]
        sealed class ClassConstantDeclCompiler : ConstantDeclCompiler<ClassConstantDecl>, IClassConstantDeclCompiler
        {
            public override KnownConstant Constant { get { return constant; } }
            internal ClassConstant ClassConstant { get { return constant; } }
            private ClassConstant constant;

            public void AnalyzeMember(ClassConstantDecl/*!*/node, Analyzer/*!*/ analyzer, ClassConstant/*!*/ constant)
            {
                this.constant = constant;

                // constant redeclared:
                if (constant == null) return;

                // initialize constant so that it has no value:
                this.constant.SetNode(node);
            }

            public void Emit(CodeGenerator/*!*/ codeGenerator)
            {
                Statistics.AST.AddNode("Class.ConstantDecl");

                codeGenerator.InitializeClassConstant(constant);
                if (constant.IsExported)
                {
                    string name = constant.FullName;

                    // avoid duplicate export property names
                    while (true)
                    {
                        DPropertyDesc prop_desc = constant.DeclaringPhpType.TypeDesc.GetProperty(new VariableName(name));
                        if (prop_desc != null && prop_desc.PhpField.IsExported)
                        {
                            name = name + "_const";
                        }
                        else break;
                    }

                    PropertyBuilder exported_property = ClrStubBuilder.DefineFieldExport(name, constant);
                    codeGenerator.EmitConstantExportStub(constant, exported_property);
                }
            }

            public void ApplyCustomAttribute(SpecialAttributes kind, Attribute attribute, CustomAttribute.TargetSelectors selector)
            {
                // constant redeclared:
                if (constant == null) return;

                switch (kind)
                {
                    case SpecialAttributes.Export:
                        Debug.Assert(selector == CustomAttribute.TargetSelectors.Default);
                        constant.ExportInfo = (ExportAttribute)attribute;
                        break;

                    default:
                        Debug.Fail("N/A");
                        break;
                }
            }

            public void EmitCustomAttribute(CustomAttributeBuilder/*!*/ builder, CustomAttribute.TargetSelectors selector)
            {
                constant.RealFieldBuilder.SetCustomAttribute(builder);
            }
        }

        #endregion

        #region Traits

        [NodeCompiler(typeof(TraitsUse))]
        sealed class TraitsUseCompiler : TypeMemberDeclCompiler<TraitsUse>
        {
            #region IPhpCustomAttributeProvider

            private class PhpCustomAttributeProvider : BasePhpCustomAttributeProvider
            {
                public PhpCustomAttributeProvider(TraitsUse node)
                    : base(node)
                {
                }

                public override PhpAttributeTargets AttributeTarget
                {
                    get { return PhpAttributeTargets.Types; }
                }

                public override AttributeTargets AcceptsTargets
                {
                    get { return (AttributeTargets)0; }
                }

                public override void EmitCustomAttribute(CustomAttributeBuilder builder, CustomAttribute.TargetSelectors selector)
                {
                    // nothing
                }

                public override void ApplyCustomAttribute(SpecialAttributes kind, Attribute attribute, CustomAttribute.TargetSelectors selector)
                {
                    // nothing
                }
            }

            #endregion

            protected override IPhpCustomAttributeProvider CreatePhpCustomAttributeProvider(TraitsUse node)
            {
                return new PhpCustomAttributeProvider(node);
            }
            
            internal override void AnalyzeMembers(TraitsUse node, Analyzer analyzer, PhpType declaringType)
            {
                base.AnalyzeMembers(node, analyzer, declaringType);
            }

            internal override void Analyze(TraitsUse node, Analyzer analyzer)
            {
                throw new NotImplementedException();
            }

            internal override void Emit(TraitsUse node, CodeGenerator codeGenerator)
            {
                throw new NotImplementedException();
            }
        }

        #endregion

        #region TraitAdaptation, TraitAdaptationPrecedence, TraitAdaptationAlias

        abstract class TraitAdaptationCompiler : INodeCompiler
        {
        }

        [NodeCompiler(typeof(TraitsUse.TraitAdaptationPrecedence))]
        sealed class TraitAdaptationPrecedenceCompiler : TraitAdaptationCompiler
        {
        }

        [NodeCompiler(typeof(TraitsUse.TraitAdaptationAlias))]
        sealed class TraitAdaptationAliasCompiler : TraitAdaptationCompiler
        {
        }

        #endregion
    }

    #region TypeSignatureHelper

    internal static class TypeSignatureHelper
    {
        /// <summary>
        /// Creates an array of generic parameters. 
        /// Used by generic types.
        /// </summary>
        public static GenericParameterDesc[]/*!!*/ ToGenericParameters(this TypeSignature typeSignature, DMember/*!*/ declaringType)
        {
            int mandatory_generic_param_count;
            return typeSignature.ToGenericParameters(declaringType, out mandatory_generic_param_count);
        }

        /// <summary>
        /// Creates a <see cref="PhpRoutineSignature"/> partially initialized with the type parameters of this type signature. 
        /// Used by generic routines.
        /// </summary>
        public static PhpRoutineSignature/*!*/ ToPhpRoutineSignature(this TypeSignature typeSignature, DMember/*!*/ declaringRoutine)
        {
            Debug.Assert(declaringRoutine != null);

            int mandatory_generic_param_count;
            GenericParameterDesc[] descs = typeSignature.ToGenericParameters(declaringRoutine, out mandatory_generic_param_count);

            GenericParameter[] types = new GenericParameter[descs.Length];
            for (int i = 0; i < descs.Length; i++)
                types[i] = descs[i].GenericParameter;

            return new PhpRoutineSignature(types, mandatory_generic_param_count);
        }

        private static GenericParameterDesc[]/*!!*/ ToGenericParameters(this TypeSignature typeSignature, DMember/*!*/ declaringMember, out int mandatoryCount)
        {
            Debug.Assert(declaringMember != null);

            if (typeSignature.TypeParams.Length == 0)
            {
                mandatoryCount = 0;
                return GenericParameterDesc.EmptyArray;
            }

            GenericParameterDesc[] result = new GenericParameterDesc[typeSignature.TypeParams.Length];
            mandatoryCount = 0;
            for (int i = 0; i < typeSignature.TypeParams.Length; i++)
            {
                result[i] = new GenericParameter(typeSignature.TypeParams[i].Name, i, declaringMember).GenericParameterDesc;
                if (typeSignature.TypeParams[i].DefaultType == null)
                    mandatoryCount++;
            }
            return result;
        }
    }

    #endregion

    #region IClassConstantDeclCompiler

    internal interface IClassConstantDeclCompiler
    {
        void AnalyzeMember(ClassConstantDecl/*!*/node, Analyzer/*!*/ analyzer, ClassConstant/*!*/ constant);
        void Emit(CodeGenerator/*!*/ codeGenerator);
        void ApplyCustomAttribute(SpecialAttributes kind, Attribute attribute, CustomAttribute.TargetSelectors selector);
        void EmitCustomAttribute(CustomAttributeBuilder/*!*/ builder, CustomAttribute.TargetSelectors selector);
    }

    internal static class ClassConstantDeclCompilerHelper
    {
        public static void AnalyzeMember(this ClassConstantDecl/*!*/node, Analyzer/*!*/ analyzer, ClassConstant/*!*/ constant)
        {
            node.NodeCompiler<IClassConstantDeclCompiler>().AnalyzeMember(node, analyzer, constant);
        }

        public static void Emit(this ClassConstantDecl/*!*/node, CodeGenerator/*!*/ codeGenerator)
        {
            node.NodeCompiler<IClassConstantDeclCompiler>().Emit(codeGenerator);
        }

        public static void ApplyCustomAttribute(this ClassConstantDecl/*!*/node, SpecialAttributes kind, Attribute attribute, CustomAttribute.TargetSelectors selector)
        {
            node.NodeCompiler<IClassConstantDeclCompiler>().ApplyCustomAttribute(kind, attribute, selector);
        }

        public static void EmitCustomAttribute(this ClassConstantDecl/*!*/node, CustomAttributeBuilder/*!*/ builder, CustomAttribute.TargetSelectors selector)
        {
            node.NodeCompiler<IClassConstantDeclCompiler>().EmitCustomAttribute(builder, selector);
        }
    }

    #endregion

    #region ITypeMemberDeclCompiler

    internal interface ITypeMemberDeclCompiler
    {
        CompilationSourceUnit SourceUnit { get; set; }
        void AnalyzeMembers(TypeMemberDecl/*!*/node, Analyzer/*!*/ analyzer, PhpType/*!*/ declaringType);
        void Analyze(TypeMemberDecl/*!*/node, Analyzer/*!*/ analyzer);
        void Emit(TypeMemberDecl/*!*/node, CodeGenerator/*!*/ codeGenerator);

        void EnterAnalyzer(Analyzer/*!*/analyzer);
        void LeaveAnalyzer(Analyzer/*!*/analyzer);
        void EnterCodegenerator(CodeGenerator/*!*/ codeGenerator);
        void LeaveCodegenerator(CodeGenerator/*!*/ codeGenerator);
    }

    internal static class TypeMemberDeclCompilerHelper
    {
        public static void AnalyzeMembers(this TypeMemberDecl/*!*/node, Analyzer/*!*/ analyzer, PhpType/*!*/ declaringType)
        {
            node.NodeCompiler<ITypeMemberDeclCompiler>().AnalyzeMembers(node, analyzer, declaringType);
        }
        public static void Analyze(this TypeMemberDecl/*!*/node, Analyzer/*!*/ analyzer)
        {
            node.NodeCompiler<ITypeMemberDeclCompiler>().Analyze(node, analyzer);
        }
        public static void Emit(this TypeMemberDecl/*!*/node, CodeGenerator/*!*/ codeGenerator)
        {
            node.NodeCompiler<ITypeMemberDeclCompiler>().Emit(node, codeGenerator);
        }
    }

    #endregion

    #region IFormalTypeParamCompiler

    internal interface IFormalTypeParamCompiler
    {
        #region Analysis

        void PreAnalyze(FormalTypeParam/*!*/node, Analyzer/*!*/ analyzer, GenericParameter/*!*/ parameter);

        bool Merge(FormalTypeParam/*!*/node, ErrorSink/*!*/ errors, FormalTypeParam/*!*/ other);

        void AnalyzeMembers(FormalTypeParam/*!*/node, Analyzer/*!*/ analyzer, Scope referringScope);

        void Analyze(FormalTypeParam/*!*/node, Analyzer/*!*/ analyzer);

        #endregion

        #region Emission

        void Emit(FormalTypeParam/*!*/node, CodeGenerator/*!*/ codeGenerator);

        #endregion
    }

    internal static class FormalTypeParamCompilerHelper
    {
        #region Analysis

        public static void PreAnalyze(this FormalTypeParam/*!*/node, Analyzer/*!*/ analyzer, GenericParameter/*!*/ parameter)
        {
            node.NodeCompiler<IFormalTypeParamCompiler>().PreAnalyze(node, analyzer, parameter);
        }

        public static bool Merge(this FormalTypeParam/*!*/node, ErrorSink/*!*/ errors, FormalTypeParam/*!*/ other)
        {
            return node.NodeCompiler<IFormalTypeParamCompiler>().Merge(node, errors, other);
        }

        public static void AnalyzeMembers(this FormalTypeParam/*!*/node, Analyzer/*!*/ analyzer, Scope referringScope)
        {
            node.NodeCompiler<IFormalTypeParamCompiler>().AnalyzeMembers(node, analyzer, referringScope);
        }

        public static void Analyze(this FormalTypeParam/*!*/node, Analyzer/*!*/ analyzer)
        {
            node.NodeCompiler<IFormalTypeParamCompiler>().Analyze(node, analyzer);
        }

        #endregion

        #region Emission

        public static void Emit(this FormalTypeParam/*!*/node, CodeGenerator/*!*/ codeGenerator)
        {
            node.NodeCompiler<IFormalTypeParamCompiler>().Emit(node, codeGenerator);
        }

        #endregion
    }

    #endregion

    #region IFieldDeclCompiler

    internal interface IFieldDeclCompiler
    {
        void AnalyzeMember(FieldDecl/*!*/node, Analyzer/*!*/ analyzer, PhpField/*!*/ field);
        void Analyze(FieldDecl/*!*/node, Analyzer/*!*/ analyzer);
        void Emit(FieldDecl/*!*/node, CodeGenerator/*!*/ codeGenerator);
        void EmitCustomAttribute(CustomAttributeBuilder/*!*/ builder, CustomAttribute.TargetSelectors selector);
        void ApplyCustomAttribute(SpecialAttributes kind, Attribute attribute, CustomAttribute.TargetSelectors selector);
    }

    internal static class FieldDeclCompilerHelper
    {
        public static void AnalyzeMember(this FieldDecl/*!*/node, Analyzer/*!*/ analyzer, PhpField/*!*/ field)
        {
            node.NodeCompiler<IFieldDeclCompiler>().AnalyzeMember(node, analyzer, field);
        }
        public static void Analyze(this FieldDecl/*!*/node, Analyzer/*!*/ analyzer)
        {
            node.NodeCompiler<IFieldDeclCompiler>().Analyze(node, analyzer);
        }
        public static void Emit(this FieldDecl/*!*/node, CodeGenerator/*!*/ codeGenerator)
        {
            node.NodeCompiler<IFieldDeclCompiler>().Emit(node, codeGenerator);
        }
        public static void EmitCustomAttribute(this FieldDecl/*!*/node, CustomAttributeBuilder/*!*/ builder, CustomAttribute.TargetSelectors selector)
        {
            node.NodeCompiler<IFieldDeclCompiler>().EmitCustomAttribute(builder, selector);
        }
        public static void ApplyCustomAttribute(this FieldDecl/*!*/node, SpecialAttributes kind, Attribute attribute, CustomAttribute.TargetSelectors selector)
        {
            node.NodeCompiler<IFieldDeclCompiler>().ApplyCustomAttribute(kind, attribute, selector);
        }
    }

    #endregion

    #region ITypeDeclCompiler

    internal interface ITypeDeclCompiler : IStatementCompiler
    {
        PhpType Type { get; }
    }

    internal static class TypeDeclCompilerHelper
    {
        public static PhpType Type(this TypeDecl node)
        {
            return node.NodeCompiler<ITypeDeclCompiler>().Type;
        }
    }

    #endregion
}
