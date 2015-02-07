/*

 Copyright (c) 2013 DEVSENSE

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Diagnostics;
using System.Collections;

using PHP.Core.AST;
using PHP.Core.Emit;
using PHP.Core.Parsers;
using PHP.Core.Reflection;

namespace PHP.Core.Compiler.AST
{
    partial class NodeCompilers
    {
        #region ActualParam

        [NodeCompiler(typeof(ActualParam), Singleton = true)]
        sealed class ActualParamCompiler : INodeCompiler, IActualParamCompiler
        {
            public void Analyze(ActualParam/*!*/node, Analyzer/*!*/ analyzer, bool isBaseCtorCallConstrained)
            {
                // TODO: isBaseCtorCallConstrained

                ExInfoFromParent info = new ExInfoFromParent(node);

                analyzer.EnterActParam();

                if (analyzer.ActParamDeclIsUnknown())
                {
                    // we don't know whether the parameter will be passed by reference at run-time:
                    if (node.Expression.AllowsPassByReference)
                    {
                        info.Access = AccessType.ReadUnknown;

                        // Although we prepare to pass reference, value can be really passed.
                        // That's why we report warning when user use '&' in calling, 
                        // because it has no influence.
                        if (node.Ampersand)
                            analyzer.ErrorSink.Add(Warnings.ActualParamWithAmpersand, analyzer.SourceUnit, node.Span);
                    }
                    else
                    {
                        info.Access = AccessType.Read;
                    }
                }
                else
                {
                    if (analyzer.ActParamPassedByRef())
                    {
                        if (node.Expression.AllowsPassByReference)
                        {
                            info.Access = AccessType.ReadRef;
                        }
                        else
                        {
                            analyzer.ErrorSink.Add(Errors.NonVariablePassedByRef, analyzer.SourceUnit, node.Expression.Span);
                            analyzer.LeaveActParam();
                            return;
                        }
                    }
                    else
                    {
                        info.Access = AccessType.Read;
                        if (node.Ampersand) analyzer.ErrorSink.Add(Warnings.ActualParamWithAmpersand, analyzer.SourceUnit, node.Span);
                    }
                }

                node.expression = node.Expression.Analyze(analyzer, info).Literalize();

                // TODO: if signature is known, act. param has type hint and expression has known type; check if type hint matches expression

                analyzer.LeaveActParam();
            }

            public PhpTypeCode Emit(ActualParam/*!*/node, CodeGenerator/*!*/ codeGenerator, bool ensureChainWritable = false)
            {
                codeGenerator.ChainBuilder.Create();

                if (ensureChainWritable)
                    codeGenerator.ChainBuilder.EnsureWritable = true;

                try
                {
                    return node.Expression.Emit(codeGenerator);
                }
                finally
                {
                    codeGenerator.ChainBuilder.End();
                }
            }
        }

        #endregion

        #region NamedActualParam

        [NodeCompiler(typeof(NamedActualParam))]
        public sealed class NamedActualParamCompiler : INodeCompiler, INamedActualParamCompiler
        {
            public DProperty Property { get { return property; } }
            private DProperty property;

            public void Analyze(NamedActualParam/*!*/node, Analyzer/*!*/ analyzer, DType/*!*/ propertiesDeclarer)
            {
                // TODO: Named parameters can target the non-static, public, and read-write fields 
                // or properties of the attribute class

                bool visibility_check;

                if (!propertiesDeclarer.IsUnknown)
                {
                    property = analyzer.ResolveProperty(propertiesDeclarer, node.Name, node.Span, false, null, null, out visibility_check);
                }

                node.expression = node.Expression.Analyze(analyzer, ExInfoFromParent.DefaultExInfo).Literalize();
            }
        }

        #endregion

        #region CallSignature

        [NodeCompiler(typeof(CallSignature), Singleton = true)]
        sealed class CallSignatureCompiler : INodeCompiler, ICallSignatureCompiler
        {
            /// <summary>
            /// Gets true if all the Parameters (after the analysis) have the value and could be evaluated during the compilation time.
            /// </summary>
            public bool AllParamsHaveValue(CallSignature/*!*/node)
            {
                foreach (var p in node.Parameters)
                    if (!p.Expression.HasValue())
                        return false;

                return true;
            }

            public void Analyze(CallSignature/*!*/node, Analyzer/*!*/ analyzer, RoutineSignature/*!*/ signature, ExInfoFromParent info, bool isBaseCtorCallConstrained)
            {
                // generic:

                foreach (var p in node.GenericParams)
                    TypeRefHelper.Analyze(p, analyzer);

                // regular:

                analyzer.EnterActualParams(signature, node.Parameters.Length);

                foreach (var p in node.Parameters)
                    p.NodeCompiler<ActualParamCompiler>().Analyze(p, analyzer, isBaseCtorCallConstrained);

                analyzer.LeaveActualParams();
            }

            /// <summary>
            /// Builds <see cref="ArrayEx"/> with call signature parameters.
            /// </summary>
            /// <returns></returns>
            public ArrayEx/*!*/BuildPhpArray(CallSignature/*!*/node)
            {
                Debug.Assert(node.GenericParams.Empty());

                List<Item> arrayItems = new List<Item>(node.Parameters.Length);
                var pos = Text.Span.Invalid;

                foreach (var p in node.Parameters)
                {
                    arrayItems.Add(new ValueItem(null, p.Expression));
                    if (pos.IsValid)
                        pos = p.Span;
                    else
                        pos = Text.Span.FromBounds(pos.Start, p.Span.End);
                }

                return new ArrayEx(pos, arrayItems);
            }

            #region Emission

            /// <summary>
            /// Emits IL instructions that load actual parameters and optionally add a new stack frame to
            /// current <see cref="PHP.Core.ScriptContext.Stack"/>.
            /// </summary>
            /// <param name="node">Instance.</param>
            /// <param name="codeGenerator">Code generator.</param>
            /// <remarks>
            /// Nothing is expected on the evaluation stack. Nothing is left on the evaluation stack.
            /// </remarks>
            public void EmitLoadOnPhpStack(CallSignature/*!*/node, CodeGenerator/*!*/ codeGenerator)
            {
                var parameters = node.Parameters;
                var genericParams = node.GenericParams;

                PhpStackBuilder.EmitAddFrame(codeGenerator.IL, codeGenerator.ScriptContextPlace, genericParams.Length, parameters.Length,
                  delegate(ILEmitter il, int i)
                  {
                      // generic arguments:
                      genericParams[i].EmitLoadTypeDesc(codeGenerator, ResolveTypeFlags.UseAutoload | ResolveTypeFlags.ThrowErrors);
                  },
                  delegate(ILEmitter il, int i)
                  {
                      // regular arguments:
                      var p = parameters[i];
                      codeGenerator.EmitBoxing(p.NodeCompiler<ActualParamCompiler>().Emit(p, codeGenerator));
                  }
                );
            }

            /// <summary>
            /// Emits IL instructions that load actual parameters on the evaluation stack.
            /// </summary>
            /// <param name="node">Instance.</param>
            /// <param name="codeGenerator">Code generator.</param>
            /// <param name="routine">PHP method being called.</param>
            /// <remarks>
            /// <para>
            /// The function has mandatory and optional formal arguments.
            /// Mandatory arguments are those formal arguments which are not preceded by 
            /// any formal argument with default value. The others are optional.
            /// If a formal argument without default value is declared beyond the last mandatory argument
            /// it is treated as optional one by the caller. The callee checks this and throws warning.
            /// </para>
            /// Missing arguments handling:
            /// <list type="bullet">
            ///   <item>missing mandatory argument - WARNING; LOAD(null);</item>
            ///   <item>missing optional argument - LOAD(Arg.Default);</item>
            ///   <item>superfluous arguments are ignored</item>
            /// </list>
            /// </remarks>
            public void EmitLoadOnEvalStack(CallSignature/*!*/node, CodeGenerator/*!*/ codeGenerator, PhpRoutine/*!*/ routine)
            {
                EmitLoadTypeArgsOnEvalStack(node, codeGenerator, routine);
                EmitLoadArgsOnEvalStack(node, codeGenerator, routine);
            }

            internal void EmitLoadTypeArgsOnEvalStack(CallSignature/*!*/node, CodeGenerator/*!*/ codeGenerator, PhpRoutine/*!*/ routine)
            {
                ILEmitter il = codeGenerator.IL;

                int mandatory_count = (routine.Signature != null) ? routine.Signature.MandatoryGenericParamCount : 0;
                int formal_count = (routine.Signature != null) ? routine.Signature.GenericParamCount : 0;
                int actual_count = node.GenericParams.Length;

                // loads all actual parameters which are not superfluous:
                for (int i = 0; i < Math.Min(actual_count, formal_count); i++)
                    node.GenericParams[i].EmitLoadTypeDesc(codeGenerator, ResolveTypeFlags.UseAutoload | ResolveTypeFlags.ThrowErrors);

                // loads missing mandatory arguments:
                for (int i = actual_count; i < mandatory_count; i++)
                {
                    // CALL PhpException.MissingTypeArgument(<i+1>,<name>);
                    il.LdcI4(i + 1);
                    il.Emit(OpCodes.Ldstr, routine.FullName);
                    codeGenerator.EmitPhpException(Methods.PhpException.MissingTypeArgument);

                    // LOAD DTypeDesc.ObjectTypeDesc;
                    il.Emit(OpCodes.Ldsfld, Fields.DTypeDesc.ObjectTypeDesc);
                }

                // loads missing optional arguments:
                for (int i = Math.Max(mandatory_count, actual_count); i < formal_count; i++)
                {
                    // LOAD Arg.DefaultType;
                    il.Emit(OpCodes.Ldsfld, Fields.Arg_DefaultType);
                }
            }

            internal void EmitLoadArgsOnEvalStack(CallSignature/*!*/node, CodeGenerator/*!*/ codeGenerator, PhpRoutine/*!*/ routine)
            {
                ILEmitter il = codeGenerator.IL;

                int mandatory_count = (routine.Signature != null) ? routine.Signature.MandatoryParamCount : 0;
                int formal_count = (routine.Signature != null) ? routine.Signature.ParamCount : 0;
                int actual_count = node.Parameters.Length;
                PhpTypeCode param_type;

                // loads all actual parameters which are not superfluous:
                for (int i = 0; i < Math.Min(actual_count, formal_count); i++)
                {
                    var p = node.Parameters[i];
                    codeGenerator.EmitBoxing(param_type = p.NodeCompiler<ActualParamCompiler>().Emit(p, codeGenerator));

                    // Actual param emitter should emit "boxing" to a reference if its access type is ReadRef.
                    // That's why no operation is needed here and references should match.
                    Debug.Assert((routine.Signature == null || routine.Signature.IsAlias(i)) == (param_type == PhpTypeCode.PhpReference));
                }

                // loads missing mandatory arguments:
                for (int i = actual_count; i < mandatory_count; i++)
                {
                    // CALL PhpException.MissingArgument(<i+1>,<name>);
                    il.LdcI4(i + 1);
                    il.Emit(OpCodes.Ldstr, routine.FullName);
                    codeGenerator.EmitPhpException(Methods.PhpException.MissingArgument);

                    // LOAD null;
                    if (routine.Signature.IsAlias(i))
                        il.Emit(OpCodes.Newobj, Constructors.PhpReference_Void);
                    else
                        il.Emit(OpCodes.Ldnull);
                }

                // loads missing optional arguments:
                for (int i = Math.Max(mandatory_count, actual_count); i < formal_count; i++)
                {
                    // LOAD Arg.Default;
                    il.Emit(OpCodes.Ldsfld, Fields.Arg_Default);
                }
            }

            /// <summary>
            /// Emits parameter loading.
            /// </summary>
            /// <param name="node">Instance.</param>
            /// <param name="il">Emitter.</param>
            /// <param name="index">The index of the parameter starting from 0.</param>
            /// <param name="codeGenerator">Code generator.</param>
            /// <param name="param">Target <see cref="ParameterInfo"/>.</param>
            /// <returns>The type of the actual argument or its value if it is a leteral.</returns>
            public object EmitLibraryLoadArgument(CallSignature/*!*/node, ILEmitter/*!*/ il, int index, object/*!*/ codeGenerator, ParameterInfo param)
            {
                Debug.Assert(codeGenerator != null);
                Debug.Assert(index < node.Parameters.Length, "Missing arguments prevents code generation");

                // returns value if the parameter is evaluable at compile time:
                if (node.Parameters[index].Expression.HasValue())
                    return node.Parameters[index].Expression.GetValue();

                // emits parameter evaluation:
                var p = node.Parameters[index];
                return PhpTypeCodeEnum.ToType(p.NodeCompiler<ActualParamCompiler>().Emit(p, (CodeGenerator)codeGenerator, PhpRwAttribute.IsDefined(param)));
            }

            /// <summary>
            /// Emits load of optional parameters array on the evaluation stack.
            /// </summary>
            /// <param name="node">Instance.</param>
            /// <param name="builder">An overloads builder.</param>
            /// <param name="start">An index of the first optional parameter to be loaded into the array (indices start from 0).</param>
            /// <param name="param">
            /// A <see cref="ParameterInfo"/> of the formal parameter of the target method where the array will be passed.
            /// This information influences conversions all optional parameters.
            /// </param>
            /// <param name="optArgCount">Optional argument count (unused).</param>
            public void EmitLibraryLoadOptArguments(CallSignature/*!*/node, OverloadsBuilder/*!*/ builder, int start, ParameterInfo/*!*/ param, IPlace optArgCount)
            {
                Debug.Assert(start >= 0 && builder != null && param != null && builder.Aux is CodeGenerator);

                ILEmitter il = builder.IL;
                Type elem_type = param.ParameterType.GetElementType();
                Type array_type = elem_type.MakeArrayType();

                // NEW <alem_type>[<parameters count - start>]
                il.LdcI4(node.Parameters.Length - start);
                il.Emit(OpCodes.Newarr, elem_type);

                // loads each optional parameter into the appropriate bucket of the array:
                for (int i = start; i < node.Parameters.Length; i++)
                {
                    // <arr>[i - start]
                    il.Emit(OpCodes.Dup);
                    il.LdcI4(i - start);

                    // <parameter value>
                    object type_or_value = EmitLibraryLoadArgument(node, il, i, builder.Aux, param);
                    builder.EmitArgumentConversion(elem_type, type_or_value, false, param, 3);

                    // <arr>[i - start] = <parameter value>;
                    il.Stelem(elem_type);
                }

                // <arr>
            }

            #endregion
        }

        #endregion
    }

    #region INamedActualParamCompiler

    internal interface INamedActualParamCompiler
    {
        DProperty Property { get; }
        void Analyze(NamedActualParam/*!*/node, Analyzer/*!*/ analyzer, DType/*!*/ propertiesDeclarer);
    }

    internal static class NamedActualParamCompilerHelper
    {
        public static DProperty GetProperty(this NamedActualParam node)
        {
            return node.NodeCompiler<INamedActualParamCompiler>().Property;
        }
        public static void Analyze(this NamedActualParam/*!*/node, Analyzer/*!*/ analyzer, DType/*!*/ propertiesDeclarer)
        {
            node.NodeCompiler<INamedActualParamCompiler>().Analyze(node, analyzer, propertiesDeclarer);
        }
    }

    #endregion

    #region IActualParamCompiler

    internal interface IActualParamCompiler
    {
        PhpTypeCode Emit(ActualParam/*!*/node, CodeGenerator/*!*/ codeGenerator, bool ensureChainWritable);
    }

    internal static class ActualParamCompilerHelper
    {
        public static PhpTypeCode Emit(this ActualParam/*!*/node, CodeGenerator/*!*/ codeGenerator, bool ensureChainWritable = false)
        {
            return node.NodeCompiler<IActualParamCompiler>().Emit(node, codeGenerator, ensureChainWritable);
        }
    }

    #endregion

    #region ICallSignatureCompiler

    internal interface ICallSignatureCompiler
    {
        bool AllParamsHaveValue(CallSignature/*!*/node);
        ArrayEx/*!*/BuildPhpArray(CallSignature/*!*/node);
        void Analyze(CallSignature/*!*/node, Analyzer/*!*/ analyzer, RoutineSignature/*!*/ signature, ExInfoFromParent info, bool isBaseCtorCallConstrained);
        void EmitLoadOnPhpStack(CallSignature/*!*/node, CodeGenerator/*!*/ codeGenerator);
        void EmitLoadOnEvalStack(CallSignature/*!*/node, CodeGenerator/*!*/ codeGenerator, PhpRoutine/*!*/ routine);
        object EmitLibraryLoadArgument(CallSignature/*!*/node, ILEmitter/*!*/ il, int index, object/*!*/ codeGenerator, ParameterInfo param);
        void EmitLibraryLoadOptArguments(CallSignature/*!*/node, OverloadsBuilder/*!*/ builder, int start, ParameterInfo/*!*/ param, IPlace optArgCount);
    }

    internal static class CallSignatureHelpers
    {
        public static bool AllParamsHaveValue(this CallSignature/*!*/node)
        {
            return node.NodeCompiler<ICallSignatureCompiler>().AllParamsHaveValue(node);
        }
        public static ArrayEx/*!*/BuildPhpArray(this CallSignature/*!*/node)
        {
            return node.NodeCompiler<ICallSignatureCompiler>().BuildPhpArray(node);
        }
        public static void Analyze(this CallSignature/*!*/node, Analyzer/*!*/ analyzer, RoutineSignature/*!*/ signature, ExInfoFromParent info, bool isBaseCtorCallConstrained)
        {
            node.NodeCompiler<ICallSignatureCompiler>().Analyze(node, analyzer, signature, info, isBaseCtorCallConstrained);
        }
        public static void EmitLoadOnPhpStack(this CallSignature/*!*/node, CodeGenerator/*!*/ codeGenerator)
        {
            node.NodeCompiler<ICallSignatureCompiler>().EmitLoadOnPhpStack(node, codeGenerator);
        }
        public static void EmitLoadOnEvalStack(this CallSignature/*!*/node, CodeGenerator/*!*/ codeGenerator, PhpRoutine/*!*/ routine)
        {
            node.NodeCompiler<ICallSignatureCompiler>().EmitLoadOnEvalStack(node, codeGenerator, routine);
        }
        public static object EmitLibraryLoadArgument(this CallSignature/*!*/node, ILEmitter/*!*/ il, int index, object/*!*/ codeGenerator, ParameterInfo param)
        {
            return node.NodeCompiler<ICallSignatureCompiler>().EmitLibraryLoadArgument(node, il, index, codeGenerator, param);
        }
        public static void EmitLibraryLoadOptArguments(this CallSignature/*!*/node, OverloadsBuilder/*!*/ builder, int start, ParameterInfo/*!*/ param, IPlace optArgCount)
        {
            node.NodeCompiler<ICallSignatureCompiler>().EmitLibraryLoadOptArguments(node, builder, start, param, optArgCount);
        }
    }

    #endregion
}
