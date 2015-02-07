/*

 Copyright (c) 2004-2006 Tomas Matousek, Ladislav Prosek and Vaclav Novak.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

using PHP.Core.AST;
using PHP.Core.Emit;
using PHP.Core.Parsers;
using PHP.Core.Reflection;
using System.IO;

namespace PHP.Core.Compiler.AST
{
    partial class NodeCompilers
    {
        #region NewEx

        [NodeCompiler(typeof(NewEx))]
        sealed class NewExCompiler : VarLikeConstructUseCompiler<NewEx>
        {
            private DRoutine constructor;
            private bool runtimeVisibilityCheck;
            private bool typeArgsResolved;

            public override Evaluation Analyze(NewEx node, Analyzer analyzer, ExInfoFromParent info)
            {
                Debug.Assert(node.IsMemberOf == null);

                access = info.Access;

                this.typeArgsResolved = TypeRefHelper.Analyze(node.ClassNameRef, analyzer);

                DType type = TypeRefHelper.ResolvedType(node.ClassNameRef);
                RoutineSignature signature;

                if (typeArgsResolved)
                    analyzer.AnalyzeConstructedType(type);

                if (type != null)
                {
                    bool error_reported = false;

                    // make checks if we are sure about character of the type:
                    if (type.IsIdentityDefinite)
                    {
                        if (type.IsAbstract || type.IsInterface)
                        {
                            analyzer.ErrorSink.Add(Errors.AbstractClassOrInterfaceInstantiated, analyzer.SourceUnit,
                                node.Span, type.FullName);
                            error_reported = true;
                        }
                    }

                    // disallow instantiation of Closure
                    if (type.RealType == typeof(PHP.Library.SPL.Closure))
                    {
                        analyzer.ErrorSink.Add(Errors.ClosureInstantiated, analyzer.SourceUnit, node.Span, type.FullName);
                        error_reported = true;
                    }

                    // type name resolved, look the constructor up:
                    constructor = analyzer.ResolveConstructor(type, node.Span, analyzer.CurrentType, analyzer.CurrentRoutine,
                      out runtimeVisibilityCheck);

                    if (constructor.ResolveOverload(analyzer, node.CallSignature, node.Span, out signature) == DRoutine.InvalidOverloadIndex)
                    {
                        if (!error_reported)
                        {
                            analyzer.ErrorSink.Add(Errors.ClassHasNoVisibleCtor, analyzer.SourceUnit, node.Span, type.FullName);
                        }
                    }
                }
                else
                {
                    signature = UnknownSignature.Default;
                }

                CallSignatureHelpers.Analyze(node.CallSignature, analyzer, signature, info, false);

                return new Evaluation(node);
            }

            public override bool IsDeeplyCopied(NewEx node, CopyReason reason, int nestingLevel)
            {
                return false;
            }

            public override PhpTypeCode Emit(NewEx node, CodeGenerator codeGenerator)
            {
                Statistics.AST.AddNode("NewEx");

                PhpTypeCode result;
                var newextype = TypeRefHelper.ResolvedType(node.ClassNameRef);

                if (newextype != null && typeArgsResolved)
                {
                    // constructor is resolvable (doesn't mean that known) //

                    result = newextype.EmitNew(codeGenerator, null, constructor, node.CallSignature, runtimeVisibilityCheck);
                }
                else
                {
                    // constructor is unresolvable (a variable is used in type name => type is unresolvable as well) //

                    codeGenerator.EmitNewOperator(null, node.ClassNameRef, null, node.CallSignature);
                    result = PhpTypeCode.Object;
                }

                codeGenerator.EmitReturnValueHandling(node, false, ref result);
                return result;
            }
        }

        #endregion

        #region InstanceOfEx

        [NodeCompiler(typeof(InstanceOfEx))]
        sealed class InstanceOfExCompiler : ExpressionCompiler<InstanceOfEx>
        {
            private bool typeArgsResolved;

            public override Evaluation Analyze(InstanceOfEx node, Analyzer analyzer, ExInfoFromParent info)
            {
                access = info.Access;

                node.Expression = node.Expression.Analyze(analyzer, ExInfoFromParent.DefaultExInfo).Literalize();

                typeArgsResolved = TypeRefHelper.Analyze(node.ClassNameRef, analyzer);

                if (typeArgsResolved)
                    analyzer.AnalyzeConstructedType(TypeRefHelper.ResolvedType(node.ClassNameRef));

                return new Evaluation(node);
            }

            public override bool IsDeeplyCopied(InstanceOfEx node, CopyReason reason, int nestingLevel)
            {
                return false;
            }

            public override PhpTypeCode Emit(InstanceOfEx node, CodeGenerator codeGenerator)
            {
                Statistics.AST.AddNode("InstanceOfEx");

                // emits load of expression value on the stack:
                codeGenerator.EmitBoxing(node.Expression.Emit(codeGenerator));

                var resolvedType = TypeRefHelper.ResolvedType(node.ClassNameRef);

                if (resolvedType != null && typeArgsResolved)
                {
                    // type is resolvable (doesn't mean known) //

                    resolvedType.EmitInstanceOf(codeGenerator, null);
                }
                else
                {
                    // type is unresolvable (there is some variable or the type is a generic parameter) //

                    codeGenerator.EmitInstanceOfOperator(null, node.ClassNameRef, null);
                }

                if (access == AccessType.None)
                {
                    codeGenerator.IL.Emit(OpCodes.Pop);
                    return PhpTypeCode.Void;
                }
                else
                {
                    return PhpTypeCode.Boolean;
                }
            }
        }

        #endregion

        #region TypeOfEx

        [NodeCompiler(typeof(TypeOfEx))]
        sealed class TypeOfExCompiler : ExpressionCompiler<TypeOfEx>
        {
            public bool/*!*/ TypeArgsResolved { get { return typeArgsResolved; } }
            private bool typeArgsResolved;

            public override Evaluation Analyze(TypeOfEx node, Analyzer analyzer, ExInfoFromParent info)
            {
                access = info.Access;

                typeArgsResolved = TypeRefHelper.Analyze(node.ClassNameRef, analyzer);

                if (typeArgsResolved)
                    analyzer.AnalyzeConstructedType(TypeRefHelper.ResolvedType(node.ClassNameRef));

                return new Evaluation(node);
            }

            public override bool IsCustomAttributeArgumentValue(TypeOfEx node)
            {
                var resolvedtype = TypeRefHelper.ResolvedType(node.ClassNameRef);
                return resolvedtype != null && typeArgsResolved && resolvedtype.IsDefinite;
            }

            public override bool IsDeeplyCopied(TypeOfEx node, CopyReason reason, int nestingLevel)
            {
                return false;
            }

            public override PhpTypeCode Emit(TypeOfEx node, CodeGenerator codeGenerator)
            {
                Statistics.AST.AddNode("TypeOfEx");

                var resolvedtype = TypeRefHelper.ResolvedType(node.ClassNameRef);

                if (resolvedtype != null && typeArgsResolved)
                {
                    // type is resolvable (doesn't mean known) //

                    resolvedtype.EmitTypeOf(codeGenerator, null);
                }
                else
                {
                    // type is unresolvable (there is some variable or the type is a generic parameter) //

                    codeGenerator.EmitTypeOfOperator(null, node.ClassNameRef, null);
                }

                if (access == AccessType.None)
                {
                    codeGenerator.IL.Emit(OpCodes.Pop);
                    return PhpTypeCode.Void;
                }
                else
                {
                    return PhpTypeCode.DObject;
                }
            }
        }

        #endregion
    }
}
