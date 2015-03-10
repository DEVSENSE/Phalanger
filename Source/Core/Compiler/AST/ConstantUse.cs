/*

 Copyright (c) 2004-2006 Tomas Matousek, Vaclav Novak and Martin Maly.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Reflection.Emit;
using System.Diagnostics;

using PHP.Core.AST;
using PHP.Core.Emit;
using PHP.Core.Parsers;
using PHP.Core.Reflection;

namespace PHP.Core.Compiler.AST
{
    partial class NodeCompilers
    {
        #region ConstantUse

        abstract class ConstantUseCompiler<T> : ExpressionCompiler<T> where T : ConstantUse
        {
            protected DConstant constant;

            internal abstract void ResolveName(T/*!*/node, Analyzer/*!*/ analyzer);

            /// <summary>
            /// Determines behavior on assignment.
            /// </summary>
            /// <returns>Always <B>false</B>, since constants contain immutable objects only.</returns>
            public override bool IsDeeplyCopied(T node, CopyReason reason, int nestingLevel)
            {
                return false;
            }

            public override Evaluation Analyze(T node, Analyzer analyzer, ExInfoFromParent info)
            {
                bool already_resolved = constant != null;

                if (!already_resolved)
                {
                    access = info.Access;
                    ResolveName(node, analyzer);
                }

                if (constant.IsUnknown)
                    return new Evaluation(node);

                KnownConstant known_const = (KnownConstant)constant;

                if (known_const.HasValue)
                {
                    // constant value is known:
                    return new Evaluation(node, known_const.Value);
                }
                else if (already_resolved)
                {
                    // circular definition:
                    constant.ReportCircularDefinition(analyzer.ErrorSink);
                    return new Evaluation(node);
                }
                else
                {
                    // value is not known yet, try to resolve it:
                    if (known_const.Node != null)
                        known_const.Node.Analyze(analyzer);
                    
                    return (known_const.HasValue) ? new Evaluation(node, known_const.Value) : new Evaluation(node);
                }
            }
        }

        #endregion

        #region GlobalConstUse

        [NodeCompiler(typeof(GlobalConstUse))]
        sealed class GlobalConstUseCompiler : ConstantUseCompiler<GlobalConstUse>
        {
            public override Evaluation EvaluatePriorAnalysis(GlobalConstUse node, CompilationSourceUnit sourceUnit)
            {
                constant = sourceUnit.TryResolveGlobalConstantGlobally(node.Name);
                return (constant != null && constant.HasValue) ? new Evaluation(node, constant.Value) : new Evaluation(node);
            }

            internal override void ResolveName(GlobalConstUse/*!*/node, Analyzer/*!*/ analyzer)
            {
                if (constant == null)
                    constant = analyzer.ResolveGlobalConstantName(node.Name, node.Span);
            }

            /// <include file='Doc/Nodes.xml' path='doc/method[@name="Emit"]/*'/>
            /// <param name="node">Instance.</param>
            /// <remarks>
            /// Emits IL instructions to load the value of the constant. If the value is known at compile 
            /// time (constant is system), its value is loaded on the stack. Otherwise the value is 
            /// obtained at runtime by calling <see cref="PHP.Core.ScriptContext.GetConstantValue"/>.
            /// </remarks>
            public override PhpTypeCode Emit(GlobalConstUse node, CodeGenerator codeGenerator)
            {
                Debug.Assert(access == AccessType.Read || access == AccessType.None);
                Statistics.AST.AddNode("ConstantUse.Global");

                // loads constant only if its value is read:
                if (access == AccessType.Read)
                {
                    return constant.EmitGet(codeGenerator, null, false, node.FallbackName.HasValue ? node.FallbackName.Value.ToString() : null);
                }
                else
                {
                    // to satisfy debugger; sequence point has already been defined:
                    if (codeGenerator.Context.Config.Compiler.Debug)
                        codeGenerator.IL.Emit(OpCodes.Nop);
                }

                return PhpTypeCode.Void;
            }
        }

        #endregion

        #region ClassConstUse

        /// <summary>
        /// Class constant use.
        /// </summary>
        [NodeCompiler(typeof(ClassConstUse))]
        class ClassConstUseCompiler<T> : ConstantUseCompiler<T> where T : ClassConstUse
        {
            protected DType/*!A*/type;

            bool runtimeVisibilityCheck;

            public override Evaluation EvaluatePriorAnalysis(T node, CompilationSourceUnit sourceUnit)
            {
                var className = node.ClassName;
                if (!string.IsNullOrEmpty(className.QualifiedName.Name.Value))
                    constant = sourceUnit.TryResolveClassConstantGlobally(className, node.Name);

                return (constant != null && constant.HasValue) ? new Evaluation(node, constant.Value) : new Evaluation(node);
            }

            internal override void ResolveName(T node, Analyzer analyzer)
            {
                var typeRef = node.TypeRef;

                TypeRefHelper.Analyze(typeRef, analyzer);
                this.type = TypeRefHelper.ResolvedTypeOrUnknown(typeRef);

                // analyze constructed type (we are in the full analysis):
                analyzer.AnalyzeConstructedType(type);

                constant = analyzer.ResolveClassConstantName(type, node.Name, node.Span, analyzer.CurrentType, analyzer.CurrentRoutine,
                      out runtimeVisibilityCheck);
            }

            public override PhpTypeCode Emit(T node, CodeGenerator codeGenerator)
            {
                Debug.Assert(access == AccessType.None || access == AccessType.Read);
                Statistics.AST.AddNode("ConstantUse.Class");

                if (access == AccessType.Read)
                {
                    return constant.EmitGet(codeGenerator, type as ConstructedType, runtimeVisibilityCheck, null);
                }
                else
                {
                    // to satisfy debugger; sequence point has already been defined:
                    if (codeGenerator.Context.Config.Compiler.Debug)
                        codeGenerator.IL.Emit(OpCodes.Nop);
                }

                return PhpTypeCode.Void;
            }
        }

        #endregion

        #region  PseudoClassConstUseCompiler

        [NodeCompiler(typeof(PseudoClassConstUse))]
        sealed class PseudoClassConstUseCompiler : ClassConstUseCompiler<PseudoClassConstUse>
        {
            private string TryGetValue(PseudoClassConstUse/*!*/node)
            {
                switch (node.Type)
                {
                    case PseudoClassConstUse.Types.Class:
                        var className = node.ClassName;
                        if (string.IsNullOrEmpty(className.QualifiedName.Name.Value) ||
                            className.QualifiedName.IsStaticClassName ||
                            className.QualifiedName.IsSelfClassName)
                            return null;

                        return className.QualifiedName.ToString();

                    default:
                        throw new InvalidOperationException();
                }
            }
            internal override void ResolveName(PseudoClassConstUse node, Analyzer analyzer)
            {
                if (this.type != null)
                    return;

                var typeRef = node.TypeRef;

                typeRef.Analyze(analyzer);
                this.type = typeRef.ResolvedTypeOrUnknown();

                // analyze constructed type (we are in the full analysis):
                analyzer.AnalyzeConstructedType(type);

                //
                this.constant = null;
            }

            public override Evaluation EvaluatePriorAnalysis(PseudoClassConstUse node, CompilationSourceUnit sourceUnit)
            {
                var value = TryGetValue(node);
                if (value != null)
                    return new Evaluation(node, value);
                else
                    return base.EvaluatePriorAnalysis(node, sourceUnit);
            }

            public override Evaluation Analyze(PseudoClassConstUse node, Analyzer analyzer, ExInfoFromParent info)
            {
                access = info.access;

                var value = TryGetValue(node);
                if (value != null)
                    return new Evaluation(node, value);

                //
                this.ResolveName(node, analyzer);

                //
                return new Evaluation(node);
            }

            public override PhpTypeCode Emit(PseudoClassConstUse node, CodeGenerator codeGenerator)
            {
                switch (node.Type)
                {
                    case PseudoClassConstUse.Types.Class:
                        this.type.EmitLoadTypeDesc(codeGenerator, ResolveTypeFlags.ThrowErrors | ResolveTypeFlags.UseAutoload);
                        codeGenerator.IL.Emit(OpCodes.Call, Methods.Operators.GetFullyQualifiedName);
                        return PhpTypeCode.String;

                    default:
                        throw new InvalidOperationException();
                }
            }
        }

        #endregion

        #region PseudoConstUse

        [NodeCompiler(typeof(PseudoConstUse))]
        sealed class PseudoConstUseCompiler : ExpressionCompiler<PseudoConstUse>
        {
            #region Analysis

            /// <summary>
            /// Get the value indicating if the given constant is evaluable in compile time.
            /// </summary>
            /// <param name="type"></param>
            /// <returns></returns>
            private bool IsEvaluable(PseudoConstUse.Types type)
            {
                switch (type)
                {
                    case PseudoConstUse.Types.File:
                    case PseudoConstUse.Types.Dir:
                        return false;
                    default:
                        return true;
                }
            }

            public override Evaluation Analyze(PseudoConstUse node, Analyzer analyzer, ExInfoFromParent info)
            {
                access = info.access;

                if (IsEvaluable(node.Type))
                    return new Evaluation(node, Evaluate(node, analyzer));
                else
                    return new Evaluation(node);
            }

            /// <summary>
            /// Gets value of __LINE__, __FUNCTION__, __METHOD__, __CLASS__, __NAMESPACE__ used in source code.
            /// Doesn't get value for __FILE__ and __DIR__. This value is combined from relative path and the current source root
            /// at run-time.
            /// </summary>
            /// <remarks>
            /// Analyzer maintains during AST walk information about its position in AST
            /// and that information uses (among others) to provide values of the pseudo constants.
            /// </remarks>
            private object Evaluate(PseudoConstUse node, Analyzer/*!*/ analyzer)
            {
                switch (node.Type)
                {
                    case PseudoConstUse.Types.Line:
                        return (int)new Text.TextPoint(analyzer.SourceUnit, node.Span.Start).Line; // __LINE__ is of type Integer in PHP

                    case PseudoConstUse.Types.Class:
                        if (analyzer.CurrentType != null)
                            return analyzer.CurrentType.FullName;

                        return string.Empty;

                    case PseudoConstUse.Types.Trait:
                        if (analyzer.CurrentType != null && analyzer.CurrentType.TypeDesc.IsTrait)
                            return analyzer.CurrentType.FullName;

                        return string.Empty;

                    case PseudoConstUse.Types.Function:
                        if (analyzer.CurrentRoutine != null)
                            return analyzer.CurrentRoutine.FullName;

                        return string.Empty;

                    case PseudoConstUse.Types.Method:
                        if (analyzer.CurrentRoutine != null)
                        {
                            if (analyzer.CurrentRoutine.IsMethod)
                            {
                                return ((KnownType)analyzer.CurrentRoutine.DeclaringType).QualifiedName.ToString(
                                  ((PhpMethod)analyzer.CurrentRoutine).Name, false);
                            }
                            else
                                return analyzer.CurrentRoutine.FullName;
                        }
                        return string.Empty;

                    case PseudoConstUse.Types.Namespace:
                        return analyzer.CurrentNamespace.HasValue ? analyzer.CurrentNamespace.Value.NamespacePhpName : string.Empty;

                    case PseudoConstUse.Types.File:
                    case PseudoConstUse.Types.Dir:
                        Debug.Fail("Evaluated at run-time.");
                        return null;

                    default:
                        throw null;
                }
            }

            #endregion

            /// <summary>
            /// Emit
            /// CALL Operators.ToAbsoluteSourcePath(relative source path level, remaining relative source path);
            /// </summary>
            /// <param name="codeGenerator">Code generator.</param>
            /// <returns>Type code of value that is on the top of the evaluation stack as the result of call of emitted code.</returns>
            private PhpTypeCode EmitToAbsoluteSourcePath(CodeGenerator/*!*/ codeGenerator)
            {
                ILEmitter il = codeGenerator.IL;

                // CALL Operators.ToAbsoluteSourcePath(<relative source path level>, <remaining relative source path>);
                RelativePath relative_path = codeGenerator.SourceUnit.SourceFile.RelativePath;
                il.LdcI4(relative_path.Level);
                il.Emit(OpCodes.Ldstr, relative_path.Path);
                il.Emit(OpCodes.Call, Methods.Operators.ToAbsoluteSourcePath);

                //
                return PhpTypeCode.String;
            }

            public override PhpTypeCode Emit(PseudoConstUse node, CodeGenerator codeGenerator)
            {
                switch (node.Type)
                {
                    case PseudoConstUse.Types.File:
                        EmitToAbsoluteSourcePath(codeGenerator);
                        break;

                    case PseudoConstUse.Types.Dir:
                        ILEmitter il = codeGenerator.IL;
                        // CALL Path.GetDirectory( Operators.ToAbsoluteSourcePath(...) )
                        EmitToAbsoluteSourcePath(codeGenerator);
                        il.Emit(OpCodes.Call, Methods.Path.GetDirectoryName);
                        break;

                    default:
                        Debug.Fail("Pseudo constant " + node.Type.ToString() + " expected to be already evaluated.");
                        throw null;
                }

                return PhpTypeCode.String;
            }
        }

        #endregion
    }
}
