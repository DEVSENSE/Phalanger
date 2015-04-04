/*

 Copyright (c) 2007- DEVSENSE
 Copyright (c) 2004-2006 Tomas Matousek, Ladislav Prosek, Vaclav Novak and Martin Maly.

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
/*
  
 NOTES:
     possible access values for all FunctionCall subclasses: Read, None, ReadRef
		 ReadRef is set even in cases when the function do NOT return ref:
		 
			function g(&$a) {}
			function f() {}
			g(f());  ... calling f has access ReadRef
			$a =& f(); ... dtto

*/

namespace PHP.Core.Compiler.AST
{
    partial class NodeCompilers
    {
        #region FunctionCall

        abstract class FunctionCallCompiler<T> : VarLikeConstructUseCompiler<T> where T : FunctionCall
        {
            public override Evaluation Analyze(T node, Analyzer analyzer, ExInfoFromParent info)
            {
                base.Analyze(node, analyzer, info);
                access = info.Access;
                return new Evaluation(node);
            }

            public override bool IsDeeplyCopied(T node, CopyReason reason, int nestingLevel)
            {
                // J: PhpVariable.Copy is always emitted in Emit method if needed (access == Read && resultTypeCode is copiable)
                return false;
            }

            /// <summary>
            /// Emit <see cref="PhpVariable.Copy"/> if needed. It means <see cref="IExpressionCompiler.Access"/> has to be <see cref="AccessType.Read"/> and <paramref name="returnType"/> has to be copiable.
            /// </summary>
            /// <param name="il">The <see cref="ILEmitter"/>.</param>
            /// <param name="returnType"><see cref="PhpTypeCode"/> of function call return value.</param>
            protected void EmitReturnValueCopy(ILEmitter/*!*/il, PhpTypeCode returnType)
            {
                Debug.Assert(il != null);

                // copy only if we are reading the return value &&
                // only if return type is copiable:
                if (access != AccessType.None &&   // reading, not literals:
                    PhpTypeCodeEnum.IsDeeplyCopied(returnType) &&
                    returnType != PhpTypeCode.PhpReference) // PhpSmartReference can be an issue if method returns an object field (but this is handled by binders)
                {
                    il.LdcI4((int)CopyReason.ReturnedByCopy);
                    il.Emit(OpCodes.Call, Methods.PhpVariable.Copy);
                }
            }
        }

        #endregion

        #region DirectFcnCall

        [NodeCompiler(typeof(DirectFcnCall))]
        sealed class DirectFcnCallCompiler : FunctionCallCompiler<DirectFcnCall>
        {
            /// <summary>
		    /// A list of inlined functions.
		    /// </summary>
		    private enum InlinedFunction
		    {
			    None,
			    CreateFunction
		    }

            public DRoutine routine;
            public int overloadIndex = DRoutine.InvalidOverloadIndex;

            /// <summary>
            /// Type of <see cref="VarLikeConstructUse.IsMemberOf"/> if can be resolved statically. Otherwise <c>null</c>.
            /// </summary>
            public DType isMemberOfType;

            /// <summary>
            /// An inlined function represented by the node (if any).
            /// </summary>
            private InlinedFunction inlined = InlinedFunction.None;
            
            /// <summary>
            /// Gets type of <see cref="VarLikeConstructUse.IsMemberOf"/> expression if can be resolved.
            /// </summary>
            /// <param name="node">Instance.</param>
            /// <param name="analyzer">Analyzer.</param>
            /// <returns><see cref="DType"/> or <c>null</c> reference if type could not be resolved.</returns>
            private DType GetIsMemberOfType(DirectFcnCall/*!*/node, Analyzer/*!*/analyzer)
            {
                if (node.IsMemberOf == null)
                    return null;

                DirectVarUse memberDirectVarUse = node.IsMemberOf as DirectVarUse;

                if (memberDirectVarUse != null && memberDirectVarUse.IsMemberOf == null &&  // isMemberOf is single variable
                    memberDirectVarUse.VarName.IsThisVariableName)                          // isMemberOf if $this
                {
                    // $this->
                    return analyzer.CurrentType;
                }
                else if (node.IsMemberOf is NewEx)
                {
                    // (new T)->
                    return TypeRefHelper.ResolvedType(((NewEx)node.IsMemberOf).ClassNameRef);
                }

                //
                return null;
            }

            public override Evaluation Analyze(DirectFcnCall node, Analyzer analyzer, ExInfoFromParent info)
            {
                base.Analyze(node, analyzer, info);

                if (node.IsMemberOf == null)
                {
                    // function call //

                    return AnalyzeFunctionCall(node, analyzer, ref info);
                }
                else
                {
                    // method call //

                    Debug.Assert(!(node.FallbackQualifiedName.HasValue));   // only valid for global function call
                    return AnalyzeMethodCall(node, analyzer, ref info);
                }
            }

            /// <summary>
            /// Analyze the function call (isMemberOf == null).
            /// </summary>
            /// <param name="node">Instance.</param>
            /// <param name="analyzer"></param>
            /// <param name="info"></param>
            /// <returns></returns>
            /// <remarks>This code fragment is separated to save the stack when too long Expression chain is being compiled.</remarks>
            private Evaluation AnalyzeFunctionCall(DirectFcnCall node, Analyzer/*!*/ analyzer, ref ExInfoFromParent info)
            {
                Debug.Assert(node.IsMemberOf == null);

                // resolve name:
                routine = analyzer.ResolveFunctionName(node.QualifiedName, node.Span);

                if (routine.IsUnknown)
                {
                    // note: we've to try following at run time, there can be dynamically added namespaced function matching qualifiedName
                    // try fallback
                    if (node.FallbackQualifiedName.HasValue)
                    {
                        var fallbackroutine = analyzer.ResolveFunctionName(node.FallbackQualifiedName.Value, node.Span);
                        if (fallbackroutine != null && !fallbackroutine.IsUnknown)
                        {
                            if (fallbackroutine is PhpLibraryFunction)  // we are calling library function directly
                                routine = fallbackroutine;
                        }
                    }

                    if (routine.IsUnknown)   // still unknown ?
                        Statistics.AST.AddUnknownFunctionCall(node.QualifiedName);
                }
                // resolve overload if applicable:
                RoutineSignature signature;
                overloadIndex = routine.ResolveOverload(analyzer, node.CallSignature, node.Span, out signature);

                Debug.Assert(overloadIndex != DRoutine.InvalidOverloadIndex, "A function should have at least one overload");

                if (routine is PhpLibraryFunction)
                {
                    var opts = ((PhpLibraryFunction)routine).Options;
                    // warning if not supported function call is detected
                    if ((opts & FunctionImplOptions.NotSupported) != 0)
                        analyzer.ErrorSink.Add(Warnings.NotSupportedFunctionCalled, analyzer.SourceUnit, node.Span, node.QualifiedName.ToString());

                    // warning if function requiring locals is detected (performance critical)
                    if ((opts & FunctionImplOptions.NeedsVariables) != 0 && !analyzer.CurrentScope.IsGlobal)
                        analyzer.ErrorSink.Add(Warnings.UnoptimizedLocalsInFunction, analyzer.SourceUnit, node.Span, node.QualifiedName.ToString());
                }

                // analyze parameters:
                CallSignatureHelpers.Analyze(node.CallSignature, analyzer, signature, info, false);

                // get properties:
                analyzer.AddCurrentRoutineProperty(routine.GetCallerRequirements());

                // HACK: handle call to assert() function
                if (node.QualifiedName.Name.Value.EqualsOrdinalIgnoreCase("assert"))
                {
                    // replace DirectFcnCall with AssertEx
                    var newnode = new AssertEx(node.Span, node.CallSignature);
                    return newnode.Analyze(analyzer, info);
                }

                // replaces the node if its value can be determined at compile-time:
                object value;
                return TryEvaluate(node, analyzer, out value) ?
                    new Evaluation(node, value) :
                    new Evaluation(node);
            }

            private bool AnalyzeMethodCallOnKnownType(DirectFcnCall node, Analyzer/*!*/ analyzer, ref ExInfoFromParent info, DType type)
            {
                if (type == null || type.IsUnknown)
                    return false;

                bool runtimeVisibilityCheck, isCallMethod;
                
                routine = analyzer.ResolveMethod(
                    type, node.QualifiedName.Name,
                    node.Span,
                    analyzer.CurrentType, analyzer.CurrentRoutine, false,
                    out runtimeVisibilityCheck, out isCallMethod);

                if (routine.IsUnknown)
                    return false;

                Debug.Assert(runtimeVisibilityCheck == false);  // can only be set to true if CurrentType or CurrentRoutine are null

                // check __call
                if (isCallMethod)
                {
                    // TODO: generic args

                    var arg1 = new StringLiteral(node.Span, node.QualifiedName.Name.Value);
                    var arg2 = node.CallSignature.BuildPhpArray();

                    node.CallSignature = new CallSignature(
                        new List<ActualParam>(2) {
                                new ActualParam(arg1.Span, arg1),
                                new ActualParam(arg2.Span, arg2)
                            },
                        new List<TypeRef>());
                }

                // resolve overload if applicable:
                RoutineSignature signature;
                overloadIndex = routine.ResolveOverload(analyzer, node.CallSignature, node.Span, out signature);

                Debug.Assert(overloadIndex != DRoutine.InvalidOverloadIndex, "A function should have at least one overload");

                // analyze parameters:
                CallSignatureHelpers.Analyze(node.CallSignature, analyzer, signature, info, false);

                // get properties:
                analyzer.AddCurrentRoutineProperty(routine.GetCallerRequirements());

                return true;
            }

            /// <summary>
            /// Analyze the method call (isMemberOf != null).
            /// </summary>
            /// <param name="node">Instance.</param>
            /// <param name="analyzer"></param>
            /// <param name="info"></param>
            /// <returns></returns>
            private Evaluation AnalyzeMethodCall(DirectFcnCall node, Analyzer/*!*/ analyzer, ref ExInfoFromParent info)
            {
                Debug.Assert(node.IsMemberOf != null);

                // resolve routine if IsMemberOf is resolved statically:
                isMemberOfType = this.GetIsMemberOfType(node, analyzer);
                if (this.AnalyzeMethodCallOnKnownType(node, analyzer, ref info, isMemberOfType))
                    return new Evaluation(node);

                // by default, fall back to dynamic method invocation
                routine = null;
                CallSignatureHelpers.Analyze(node.CallSignature, analyzer, UnknownSignature.Default, info, false);

                return new Evaluation(node);
            }

            #region Evaluation

            /// <summary>
            /// Modifies AST if possible, in order to generate better code.
            /// </summary>
            /// <remarks>Some well-known constructs can be modified to be analyzed and emitted better.</remarks>
            private void AnalyzeSpecial(DirectFcnCall node, Analyzer/*!*/ analyzer)
            {
                if (routine is PhpLibraryFunction)
                {
                    // basename(__FILE__, ...) -> basename("actual_file", ...)  // SourceRoot can be ignored in this case
                    if (routine.FullName.EqualsOrdinalIgnoreCase("basename"))
                        if (node.CallSignature.Parameters.Any())
                        {
                            var path_param = node.CallSignature.Parameters[0];
                            var path_expr = path_param.Expression;
                            if (path_expr is PseudoConstUse && ((PseudoConstUse)path_expr).Type == PseudoConstUse.Types.File)
                                node.CallSignature.Parameters[0] = new ActualParam(path_param.Span, new StringLiteral(path_expr.Span, analyzer.SourceUnit.SourceFile.RelativePath.Path));
                        }
                }
            }

            /// <summary>
            /// Tries to determine a value of the node.
            /// </summary>
            /// <returns>
            /// Whether the function call can be evaluated at compile time. <B>true</B>, 
            /// if the function is a special library one and the correct number of arguments 
            /// is specified in the call and all that arguments are evaluable.
            /// </returns>
            private bool TryEvaluate(DirectFcnCall node, Analyzer/*!*/ analyzer, out object value)
            {
                // special cases, allow some AST transformation:
                this.AnalyzeSpecial(node, analyzer);

                // try evaluate function call in compile time:
                if (node.CallSignature.AllParamsHaveValue())
                {
                    PureFunctionAttribute pureAttribute;

                    // PhpLibraryFunction with PureFunctionAttribute can be evaluated
                    PhpLibraryFunction lib_function;

                    if ((lib_function = routine as PhpLibraryFunction) != null &&
                        (pureAttribute = PureFunctionAttribute.Reflect(lib_function.Overloads[overloadIndex].Method)) != null)
                    {
                        // the method to be used for evaluation
                        MethodInfo evaluableMethod = pureAttribute.CallSpecialMethod ?
                            pureAttribute.SpecialMethod :
                            lib_function.Overloads[overloadIndex].Method;

                        Debug.Assert(evaluableMethod != null);

                        if (evaluableMethod.ContainsGenericParameters)
                            throw new ArgumentException("Evaluable method '" + evaluableMethod.Name + "' cannot contain generic parameters.");

                        var parametersInfo = evaluableMethod.GetParameters();

                        object[] invokeParameters = new object[parametersInfo.Length];

                        // convert/create proper parameters value:
                        int nextCallParamIndex = 0;

                        for (int i = 0; i < parametersInfo.Length; ++i)
                        {
                            ParameterInfo paramInfo = parametersInfo[i];
                            Type paramType = paramInfo.ParameterType;

                            // only In parameters are allowed
#if !SILVERLIGHT
                            Debug.Assert(!paramInfo.IsOut && !paramInfo.IsRetval);
#else
                        Debug.Assert(!paramInfo.IsOut && !ParameterInfoEx.IsRetVal(paramInfo));
#endif

                            // perform parameter conversion:
                            Action<Converter<object, object>> PassArgument = (converter) =>
                                {
                                    if (nextCallParamIndex >= node.CallSignature.Parameters.Length)
                                        throw new ArgumentException("Not enough parameters in evaluable method.");

                                    object obj = node.CallSignature.Parameters[nextCallParamIndex++].Expression.GetValue();
                                    invokeParameters[i] = converter(obj);
                                };

                            // special params types:
                            if (paramType == typeof(Analyzer))
                            {
                                invokeParameters[i] = analyzer;
                            }
                            else if (paramType == typeof(CallSignature))
                            {
                                invokeParameters[i] = node.CallSignature;
                            }
                            else if (   // ... , params object[] // last parameter
                                paramType == typeof(object[]) &&
                                i == parametersInfo.Length - 1 &&
                                parametersInfo[i].IsDefined(typeof(ParamArrayAttribute), false))
                            {
                                // params object[]
                                var args = new object[node.CallSignature.Parameters.Length - nextCallParamIndex];
                                for (int arg = 0; arg < args.Length; ++nextCallParamIndex, ++arg)
                                    args[arg] = node.CallSignature.Parameters[nextCallParamIndex].Expression.GetValue();

                                invokeParameters[i] = args;
                            }
                            // PHP value types:
                            else if (paramType == typeof(object))
                                PassArgument(obj => obj);
                            else if (paramType == typeof(PhpBytes))
                                PassArgument(Convert.ObjectToPhpBytes);
                            else if (paramType == typeof(string))
                                PassArgument(Convert.ObjectToString);
                            else if (paramType == typeof(int))
                                PassArgument(obj => (object)Convert.ObjectToInteger(obj));
                            else if (paramType == typeof(bool))
                                PassArgument(obj => (object)Convert.ObjectToBoolean(obj));
                            else if (paramType == typeof(double))
                                PassArgument(obj => (object)Convert.ObjectToDouble(obj));
                            else if (paramType == typeof(long))
                                PassArgument(obj => (object)Convert.ObjectToLongInteger(obj));
                            else if (paramType == typeof(char))
                                PassArgument(obj => (object)Convert.ObjectToChar(obj));
                            else
                                throw new ArgumentException("Parameter type " + paramType.ToString() + " cannot be used in evaluable method.", paramInfo.Name);
                        }

                        // catch runtime errors
                        var oldErrorOverride = PhpException.ThrowCallbackOverride;
                        if (!(analyzer.ErrorSink is EvalErrorSink || analyzer.ErrorSink is WebErrorSink)) // avoid infinite recursion, PhpExceptions in such cases are passed
                            PhpException.ThrowCallbackOverride = (error, message) =>
                            {
                                var position = new Text.TextSpan(analyzer.SourceUnit, node.Span);
                                analyzer.ErrorSink.AddInternal(
                                    -2,
                                    message, (error == PhpError.Error || error == PhpError.CoreError || error == PhpError.UserError) ? ErrorSeverity.Error : ErrorSeverity.Warning,
                                    (int)WarningGroups.None,
                                    analyzer.SourceUnit.GetMappedFullSourcePath(position.FirstLine),
                                    new ErrorPosition(
                                        analyzer.SourceUnit.GetMappedLine(position.FirstLine) + 1, position.FirstColumn + 1,
                                        analyzer.SourceUnit.GetMappedLine(position.LastLine) + 1, position.LastColumn + 1),
                                    true
                                    );
                            };

                        // invoke the method and get the result
                        try
                        {
                            value = evaluableMethod.Invoke(null, invokeParameters);

                            if (evaluableMethod.ReturnType == typeof(FunctionCallEvaluateInfo))
                            {
                                var info = value as FunctionCallEvaluateInfo;

                                if (info != null && info.emitDeclareLamdaFunction && info.newRoutine != null)
                                {
                                    routine = info.newRoutine;
                                    inlined = InlinedFunction.CreateFunction;
                                    return false;   // 
                                }

                                if (info == null)
                                    return false;

                                value = info.value;
                            }

                            // apply automatic cast to false if CastToFalse attribute is defined:
                            if (evaluableMethod.ReturnTypeCustomAttributes.IsDefined(typeof(CastToFalseAttribute), false))
                            {
                                if ((value == null) ||
                                    (value is int && (int)value == -1))
                                    value = false;
                            }

                            // pass the value
                            return true;
                        }
                        finally
                        {
                            PhpException.ThrowCallbackOverride = oldErrorOverride;
                        }
                    }
                }

                // function cannot be evaluated
                value = null;
                return false;

                /*

                // skips functions without "special" flag set:
                //PhpLibraryFunction lib_function = routine as PhpLibraryFunction;
                if (lib_function == null || (lib_function.Options & FunctionImplOptions.Special) == 0)
                {
                    value = null;
                    return false;
                }

                switch (callSignature.Parameters.Length)
                {
                    case 0:
                        {
                            if (lib_function.Name.EqualsLowercase("phpversion"))
                            {
                                value = PhpVersion.Current;
                                return true;
                            }

                            if (lib_function.Name.EqualsLowercase("pi"))
                            {
                                value = Math.PI;
                                return true;
                            }
                            break;
                        }

                    case 1:
                        {
                            // tries to evaluate the parameter:
                            if (!callSignature.Parameters[0].Expression.HasValue) break;

                            object param = callSignature.Parameters[0].Expression.Value;

                            if (lib_function.Name.EqualsLowercase("function_exists"))
                            {
                                // jakub: if this returns true, it is evaluable, in case of false, we should try it during the runtime again

                                // TODO:
                                //Name function_name = new Name(Convert.ObjectToString(param));
                                //OverloadInfo overload;

                                //// only library functions can be checked; others depends on the current set of declarators:
                                //ApplicationContext.Functions.Get(function_name, 0, out overload);
                                //value = overload.GetUserEntryPoint != null;

                                //return overload.GetUserEntryPoint != null;
                                value = false;
                                return false;
                            }

                            if (lib_function.Name.EqualsLowercase("strlen"))
                            {
                                value = Convert.ObjectToString(param).Length;
                                return true;
                            }

                            if (lib_function.Name.EqualsLowercase("round"))
                            {
                                value = Math.Round(Convert.ObjectToDouble(param));
                                return true;
                            }

                            if (lib_function.Name.EqualsLowercase("sqrt"))
                            {
                                value = Math.Sqrt(Convert.ObjectToDouble(param));
                                return true;
                            }


                            if (lib_function.Name.EqualsLowercase("exp"))
                            {
                                value = Math.Exp(Convert.ObjectToDouble(param));
                                return true;
                            }

                            if (lib_function.Name.EqualsLowercase("log"))
                            {
                                value = Math.Log(Convert.ObjectToDouble(param));
                                return true;
                            }

                            if (lib_function.Name.EqualsLowercase("ceil"))
                            {
                                value = Math.Ceiling(Convert.ObjectToDouble(param));
                                return true;
                            }

                            if (lib_function.Name.EqualsLowercase("floor"))
                            {
                                value = Math.Floor(Convert.ObjectToDouble(param));
                                return true;
                            }

                            if (lib_function.Name.EqualsLowercase("deg2rad"))
                            {
                                value = Convert.ObjectToDouble(param) / 180 * Math.PI;
                                return true;
                            }

                            if (lib_function.Name.EqualsLowercase("cos"))
                            {
                                value = Math.Cos(Convert.ObjectToDouble(param));
                                return true;
                            }

                            if (lib_function.Name.EqualsLowercase("sin"))
                            {
                                value = Math.Sin(Convert.ObjectToDouble(param));
                                return true;
                            }

                            if (lib_function.Name.EqualsLowercase("tan"))
                            {
                                value = Math.Tan(Convert.ObjectToDouble(param));
                                return true;
                            }

                            if (lib_function.Name.EqualsLowercase("acos"))
                            {
                                value = Math.Acos(Convert.ObjectToDouble(param));
                                return true;
                            }

                            if (lib_function.Name.EqualsLowercase("asin"))
                            {
                                value = Math.Asin(Convert.ObjectToDouble(param));
                                return true;
                            }

                            if (lib_function.Name.EqualsLowercase("atan"))
                            {
                                value = Math.Atan(Convert.ObjectToDouble(param));
                                return true;
                            }

                            break;
                        }

                    case 2:
                        {
                            // tries to evaluate the parameters:
                            if (!callSignature.Parameters[0].Expression.HasValue) break;
                            if (!callSignature.Parameters[1].Expression.HasValue) break;

                            object param1 = callSignature.Parameters[0].Expression.Value;
                            object param2 = callSignature.Parameters[1].Expression.Value;

                            if (lib_function.Name.EqualsLowercase("version_compare"))
                            {
                                value = PhpVersion.Compare(Convert.ObjectToString(param1), Convert.ObjectToString(param2));
                                return true;
                            }

                            if (lib_function.Name.EqualsLowercase("log"))
                            {
                                value = MathEx.Log(Convert.ObjectToDouble(param1), Convert.ObjectToDouble(param2));
                                return true;
                            }

                            if (lib_function.Name.EqualsLowercase("create_function"))
                            {
                                // has to be a valid identifier:
                                string function_name = "__" + Guid.NewGuid().ToString().Replace('-', '_');

                                string prefix1, prefix2;
                                DynamicCode.GetLamdaFunctionCodePrefixes(function_name, Convert.ObjectToString(param1), out prefix1, out prefix2);

                                Position pos_args = callSignature.Parameters[0].Position;
                                Position pos_body = callSignature.Parameters[1].Position;

                                // function __XXXXXX(<args>){<fill><body>}
                                string fill = GetInlinedLambdaCodeFill(pos_args, pos_body);
                                string code = String.Concat(prefix2, fill, Convert.ObjectToString(param2), "}");

                                // the position of the first character of the parsed code:
                                // (note that escaped characters distort position a little bit, which cannot be eliminated so easily)
                                Position pos = Position.Initial;
                                pos.FirstOffset = pos_args.FirstOffset - prefix1.Length + 1;
                                pos.FirstColumn = pos_args.FirstColumn - prefix1.Length + 1;
                                pos.FirstLine = pos_args.FirstLine;

                                // parses function source code:
                                List<Statement> statements = analyzer.BuildAst(pos, code);

                                if (statements == null)
                                    break;

                                FunctionDecl decl_node = (FunctionDecl)statements[0];

                                // modify declaration:
                                this.routine = decl_node.ConvertToLambda(analyzer);

                                // adds declaration to the end of the global code statement list:
                                analyzer.AddLambdaFcnDeclaration(decl_node);

                                this.inlined = InlinedFunction.CreateFunction;

                                // we cannot replace the expression with literal (emission of lambda declaration is needed):
                                value = null;
                                return false;
                            }

                            break;
                        }

                    case 3:
                        {
                            // tries to evaluate the parameters:
                            if (!callSignature.Parameters[0].Expression.HasValue) break;
                            if (!callSignature.Parameters[1].Expression.HasValue) break;
                            if (!callSignature.Parameters[2].Expression.HasValue) break;

                            object param1 = callSignature.Parameters[0].Expression.Value;
                            object param2 = callSignature.Parameters[1].Expression.Value;
                            object param3 = callSignature.Parameters[2].Expression.Value;

                            if (lib_function.Name.EqualsLowercase("version_compare"))
                            {
                                value = PhpVersion.Compare(Convert.ObjectToString(param1), Convert.ObjectToString(param2),
                                    Convert.ObjectToString(param3));

                                return true;
                            }
                            break;
                        }
                }

                value = null;
                return false;
             
                */
            }

            #endregion

            ///// <include file='Doc/Nodes.xml' path='doc/method[@name="IsDeeplyCopied"]/*'/>
            //internal override bool IsDeeplyCopied(CopyReason reason, int nestingLevel)
            //{
            //    // emit copy only if the call itself don't do that:
            //    // J: PhpVariable.Copy is always emitted in Emit method if needed (access == Read && resultTypeCode is copiable)
            //    return routine == null || !routine.ReturnValueDeepCopyEmitted;  // true if Copy has to be emitted by parent expression ($a = func())
            //}

            public override PhpTypeCode Emit(DirectFcnCall node, CodeGenerator codeGenerator)
            {
                Debug.Assert(
                    access == AccessType.Read ||
                    access == AccessType.ReadRef ||
                    access == AccessType.ReadUnknown ||
                    access == AccessType.None,
                    "Invalid access type in FunctionCall");

                Statistics.AST.AddNode("FunctionCall.Direct");

                PhpTypeCode result;
                if (inlined != InlinedFunction.None)
                {
                    result = EmitInlinedFunctionCall(codeGenerator);
                }
                else
                {
                    if (alreadyEmittedPlace != null)
                    {
                        // continuation of HandleLongChain,
                        // this DirectFcnCall was already emitted
                        // and the result was stored into local variable.
                        codeGenerator.IL.Emit(OpCodes.Ldloc, alreadyEmittedPlace);
                        result = PhpTypeCodeEnum.FromType(alreadyEmittedPlace.LocalType);
                    }
                    else
                        // this node actually represents a method call:
                        if (node.IsMemberOf != null)
                        {
                            // to avoid StackOverflowException due to long isMemberOf chain,
                            // we will avoid recursion, and divide the chain into smaller pieces.
                            HandleLongChain((DirectFcnCall)node, codeGenerator);

                            if (routine == null)
                            {
                                //result = codeGenerator.EmitRoutineOperatorCall(null, isMemberOf, qualifiedName.ToString(), null, null, callSignature, access);
                                result = codeGenerator.CallSitesBuilder.EmitMethodCall(
                                    codeGenerator,
                                    CallSitesBuilder.AccessToReturnType(access),
                                    node.IsMemberOf, null,
                                    node.QualifiedName.ToString(), null,
                                    node.CallSignature);
                            }
                            else
                                result = routine.EmitCall(
                                    codeGenerator, null, node.CallSignature,
                                    new ExpressionPlace(codeGenerator, node.IsMemberOf), false, overloadIndex,
                                    isMemberOfType, node.Span, access, true);
                        }
                        else
                        {
                            var fallbackFunctionName = node.FallbackQualifiedName.HasValue ? node.FallbackQualifiedName.Value.ToString() : null;

                            // the node represents a function call:
                            result = routine.EmitCall(
                                codeGenerator, fallbackFunctionName,
                                node.CallSignature, null, false, overloadIndex,
                                null, node.Span, access, false);
                        }
                }

                // (J) Emit Copy if necessary:
                // routine == null => Copy emitted by EmitRoutineOperatorCall
                // routine.ReturnValueDeepCopyEmitted => Copy emitted
                // otherwise emit Copy if we are going to read it by value
                if (routine != null && !routine.ReturnValueDeepCopyEmitted)
                    EmitReturnValueCopy(codeGenerator.IL, result);

                // handles return value:
                codeGenerator.EmitReturnValueHandling(node, codeGenerator.ChainBuilder.LoadAddressOfFunctionReturnValue, ref result);

                return result;
            }

            /// <summary>
            /// To avoid <see cref="StackOverflowException"/> due to long <see cref="VarLikeConstructUse.IsMemberOf"/> chain,
            /// we will avoid recursion, and divide the chain into smaller pieces.
            /// </summary>
            private void HandleLongChain(DirectFcnCall node, CodeGenerator/*!*/ codeGenerator)
            {
                int length = 300;
                VarLikeConstructUse p = node.IsMemberOf;
                while (p != null && length > 0)
                {
                    if (p.GetType() == typeof(DirectFcnCall) &&
                        ((DirectFcnCall)p).NodeCompiler<DirectFcnCallCompiler>().alreadyEmittedPlace != null)
                        return; // chain already divided here

                    p = p.IsMemberOf;
                    length--;
                }

                if (length == 0 && p != null && p.GetType() == typeof(DirectFcnCall))
                {
                    var/*!*/fcn = (DirectFcnCall)p;
                    var/*!*/fcnCompiler = fcn.NodeCompiler<DirectFcnCallCompiler>();

                    var result = fcnCompiler.Emit(fcn, codeGenerator);
                    fcnCompiler.alreadyEmittedPlace = codeGenerator.IL.DeclareLocal(PhpTypeCodeEnum.ToType(result));
                    codeGenerator.IL.Emit(OpCodes.Stloc, fcnCompiler.alreadyEmittedPlace);
                }
            }

            /// <summary>
            /// Once function call is emitted into a local variable,
            /// remember it to load it next time when <see cref="Emit"/> is called.
            /// </summary>
            private LocalBuilder alreadyEmittedPlace = null;

            /// <summary>
            /// Emits library function that can be inlined.
            /// </summary>
            private PhpTypeCode EmitInlinedFunctionCall(CodeGenerator/*!*/ codeGenerator)
            {
                switch (inlined)
                {
                    case InlinedFunction.CreateFunction:
                        {
                            PhpFunction php_function = (PhpFunction)routine;

                            // define builders (not defined earlier as the lambda function it is not in the tables):
                            php_function.DefineBuilders();

                            // LOAD PhpFunction.DeclareLamda(context,<delegate>);
                            Debug.Assert(php_function.ArgLessInfo != null); 
                            codeGenerator.EmitDeclareLamdaFunction(php_function.ArgLessInfo);

                            // bake (not baked later as the lambda function it is not in the tables):
                            php_function.Bake();

                            return PhpTypeCode.String;
                        }

                    default:
                        Debug.Fail("Unimplemented inlined function.");
                        return PhpTypeCode.Invalid;
                }
            }
        }

        #endregion

        #region IndirectFcnCall

        [NodeCompiler(typeof(IndirectFcnCall))]
        sealed class IndirectFcnCallCompiler : FunctionCallCompiler<IndirectFcnCall>
        {
            public override Evaluation Analyze(IndirectFcnCall node, Analyzer analyzer, ExInfoFromParent info)
            {
                base.Analyze(node, analyzer, info);

                node.nameExpr = node.NameExpr.Analyze(analyzer, ExInfoFromParent.DefaultExInfo).Literalize();

                CallSignatureHelpers.Analyze(node.CallSignature, analyzer, UnknownSignature.Default, info, false);

                // function call:
                if (node.IsMemberOf == null)
                    analyzer.AddCurrentRoutineProperty(RoutineProperties.ContainsIndirectFcnCall);

                return new Evaluation(node);
            }

            public override PhpTypeCode Emit(IndirectFcnCall node, CodeGenerator codeGenerator)
            {
                Debug.Assert(access == AccessType.Read || access == AccessType.ReadRef ||
                    access == AccessType.ReadUnknown || access == AccessType.None);
                Statistics.AST.AddNode("FunctionCall.Indirect");

                PhpTypeCode result;
                result = codeGenerator.EmitRoutineOperatorCall(null, node.IsMemberOf, null, null, node.NameExpr, node.CallSignature, access);
                //EmitReturnValueCopy(codeGenerator.IL, result); // (J) already emitted by EmitRoutineOperatorCall

                codeGenerator.EmitReturnValueHandling(node, codeGenerator.ChainBuilder.LoadAddressOfFunctionReturnValue, ref result);

                return result;
            }
        }

        #endregion

        #region StaticMtdCall
        
        abstract class StaticMtdCallCompiler<T> : FunctionCallCompiler<T> where T : StaticMtdCall
        {
            protected DType/*!A*/type;

            public override Evaluation Analyze(T node, Analyzer analyzer, ExInfoFromParent info)
            {
                base.Analyze(node, analyzer, info);

                var typeRef = node.TypeRef;

                TypeRefHelper.Analyze(typeRef, analyzer);
                type = TypeRefHelper.ResolvedTypeOrUnknown(typeRef);

                // analyze constructed type (new constructed type cane be used here):
                analyzer.AnalyzeConstructedType(type);

                if (type.TypeDesc.Equals(DTypeDesc.InterlockedTypeDesc))
                    analyzer.ErrorSink.Add(Warnings.ClassBehaviorMayBeUnexpected, analyzer.SourceUnit, node.Span, type.FullName);

                return new Evaluation(node);
            }
        }

        #endregion

        #region DirectStMtdCall

        [NodeCompiler(typeof(DirectStMtdCall))]
        sealed class DirectStMtdCallCompiler : StaticMtdCallCompiler<DirectStMtdCall>
        {
            private DRoutine method;
            private int overloadIndex = DRoutine.InvalidOverloadIndex;
            private bool runtimeVisibilityCheck;

            public override Evaluation Analyze(DirectStMtdCall node, Analyzer analyzer, ExInfoFromParent info)
            {
                base.Analyze(node, analyzer, info);

                // look for the method:
                bool isCallMethod;
                method = analyzer.ResolveMethod(
                    type, node.MethodName, node.Span, analyzer.CurrentType, analyzer.CurrentRoutine,
                    true, out runtimeVisibilityCheck, out isCallMethod);

                if (!method.IsUnknown)
                {
                    // we are sure about the method //

                    if (method.IsAbstract)
                    {
                        analyzer.ErrorSink.Add(Errors.AbstractMethodCalled, analyzer.SourceUnit, node.Span,
                            method.DeclaringType.FullName, method.FullName);
                    }
                }

                // check __callStatic
                if (isCallMethod)
                {
                    // TODO: generic args

                    // create new CallSignature({function name},{args})
                    var arg1 = new StringLiteral(node.Span, node.MethodName.Value);
                    var arg2 = node.CallSignature.BuildPhpArray();

                    node.CallSignature = new CallSignature(
                        new List<ActualParam>(2) {
                                new ActualParam(arg1.Span, arg1),
                                new ActualParam(arg2.Span, arg2)
                            },
                        new List<TypeRef>());
                }

                // analyze the method
                RoutineSignature signature;
                overloadIndex = method.ResolveOverload(analyzer, node.CallSignature, node.Span, out signature);

                Debug.Assert(overloadIndex != DRoutine.InvalidOverloadIndex, "Each method should have at least one overload");

                // analyze arguments
                CallSignatureHelpers.Analyze(node.CallSignature, analyzer, signature, info, false);

                return new Evaluation(node);
            }

            public override PhpTypeCode Emit(DirectStMtdCall node, CodeGenerator codeGenerator)
            {
                Statistics.AST.AddNode("StaticMethodCall.Direct");

                IPlace instance = null;

                // PHP allows for static invocations of instance method
                if (!method.IsUnknown && !method.IsStatic)
                {
                    // if we are in an instance method and the $this for the callee is assignable from
                    // current $this, then invoke the method directly with current $this
                    if (codeGenerator.LocationStack.LocationType == LocationTypes.MethodDecl)
                    {
                        CompilerLocationStack.MethodDeclContext method_context = codeGenerator.LocationStack.PeekMethodDecl();
                        if (!method_context.Method.IsStatic && method.DeclaringType.IsAssignableFrom(method_context.Type))
                        {
                            instance = IndexedPlace.ThisArg;
                        }
                    }
                }

                // class context is unknown or the class is m-decl or completely unknown at compile-time -> call the operator			
                PhpTypeCode result = method.EmitCall(codeGenerator, null, node.CallSignature, instance, runtimeVisibilityCheck,
                    overloadIndex, type, node.Span, access, false/* TODO: __call must be called virtually */);

                if (/*method == null || */!method.ReturnValueDeepCopyEmitted)   // (J) Emit Copy only if method is known (=> known PhpRoutine do not emit Copy on return value)
                    EmitReturnValueCopy(codeGenerator.IL, result);  // only if we are going to read the resulting value

                // handles return value:
                codeGenerator.EmitReturnValueHandling(node, codeGenerator.ChainBuilder.LoadAddressOfFunctionReturnValue, ref result);

                return result;
            }
        }

        #endregion

        #region IndirectStMtdCall

        [NodeCompiler(typeof(IndirectStMtdCall))]
        sealed class IndirectStMtdCallCompiler : StaticMtdCallCompiler<IndirectStMtdCall>
        {
            public override Evaluation Analyze(IndirectStMtdCall node, Analyzer analyzer, ExInfoFromParent info)
            {
                base.Analyze(node, analyzer, info);

                node.MethodNameVar.Analyze(analyzer, ExInfoFromParent.DefaultExInfo);
                CallSignatureHelpers.Analyze(node.CallSignature, analyzer, UnknownSignature.Default, info, false);

                return new Evaluation(node);
            }

            public override PhpTypeCode Emit(IndirectStMtdCall node, CodeGenerator codeGenerator)
            {
                Statistics.AST.AddNode("StaticMethodCall.Indirect");

                PhpTypeCode result = codeGenerator.EmitRoutineOperatorCall(type, null, null, null, node.MethodNameVar, node.CallSignature, access);
                //EmitReturnValueCopy(codeGenerator.IL, result); // (J) already emitted by EmitRoutineOperatorCall

                // handles return value:
                codeGenerator.EmitReturnValueHandling(node, codeGenerator.ChainBuilder.LoadAddressOfFunctionReturnValue, ref result);

                return result;
            }
        }

        #endregion
    }

    /// <summary>
    /// Evaluation info used to get some info from evaluated functions.
    /// </summary>
    public class FunctionCallEvaluateInfo
    {
        public bool emitDeclareLamdaFunction;
        public DRoutine newRoutine;
        public object value;    // evaluated value
    }
}
