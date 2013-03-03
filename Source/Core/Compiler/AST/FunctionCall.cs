/*

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

using PHP.Core.Emit;
using PHP.Core.Parsers;
using PHP.Core.Reflection;

#if SILVERLIGHT
using MathEx = PHP.CoreCLR.MathEx;
using PHP.CoreCLR;
#else
using MathEx = System.Math;
#endif
/*
  
 NOTES:
     possible access values for all FunctionCall subclasses: Read, None, ReadRef
		 ReadRef is set even in cases when the function do NOT return ref:
		 
			function g(&$a) {}
			function f() {}
			g(f());  ... calling f has access ReadRef
			$a =& f(); ... dtto

*/

namespace PHP.Core.AST
{
	#region FunctionCall

	public abstract class FunctionCall : VarLikeConstructUse
	{
		protected CallSignature callSignature;
        /// <summary>GetUserEntryPoint calling signature</summary>
        public CallSignature CallSignature { get { return callSignature; } }

		/// <summary>
        /// Position of called function name in source code.
        /// </summary>
        public Position NamePosition { get; protected set; }

		public FunctionCall(Position position, Position namePosition, List<ActualParam>/*!*/ parameters, List<TypeRef>/*!*/ genericParams)
			: base(position)
		{
			Debug.Assert(parameters != null);

			this.callSignature = new CallSignature(parameters, genericParams);
            this.NamePosition = namePosition;
		}

		/// <include file='Doc/Nodes.xml' path='doc/method[@name="Expression.Analyze"]/*'/>
		internal override Evaluation Analyze(Analyzer/*!*/ analyzer, ExInfoFromParent info)
		{
			base.Analyze(analyzer, info);
			access = info.Access;
			return new Evaluation(this);
		}

        /// <include file='Doc/Nodes.xml' path='doc/method[@name="IsDeeplyCopied"]/*'/>
        internal override bool IsDeeplyCopied(CopyReason reason, int nestingLevel)
        {
            // J: PhpVariable.Copy is always emitted in Emit method if needed (access == Read && resultTypeCode is copiable)
            return false;
        }

        /// <summary>
        /// Emit <see cref="PhpVariable.Copy"/> if needed. It means <see cref="Expression.Access"/> has to be <see cref="AccessType.Read"/> and <paramref name="returnType"/> has to be copiable.
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

		internal void DumpArguments(AstVisitor/*!*/ visitor, TextWriter/*!*/ output)
		{
			output.Write('(');

			int i = 0;
			foreach (ActualParam param in callSignature.Parameters)
			{
				if (i++ > 0) output.Write(',');
				param.Expression.DumpTo(visitor, output);
			}

			output.Write(')');
		}
	}

	#endregion

	#region DirectFcnCall

    public sealed class DirectFcnCall : FunctionCall
	{
		internal override Operations Operation { get { return Operations.DirectCall; } }

		/// <summary>
		/// A list of inlined functions.
		/// </summary>
		private enum InlinedFunction
		{
			None,
			CreateFunction
		}

		/// <summary>
		/// Simple name for methods.
		/// </summary>
		private QualifiedName qualifiedName;
        private QualifiedName? fallbackQualifiedName;
        /// <summary>Simple name for methods.</summary>
        public QualifiedName QualifiedName { get { return qualifiedName; } }

        private DRoutine routine;
		private int overloadIndex = DRoutine.InvalidOverloadIndex;

		/// <summary>
		/// An inlined function represented by the node (if any).
		/// </summary>
		private InlinedFunction inlined = InlinedFunction.None;

		public DirectFcnCall(Position position,
            QualifiedName qualifiedName, QualifiedName? fallbackQualifiedName, Position qualifiedNamePosition,
            List<ActualParam>/*!*/ parameters, List<TypeRef>/*!*/ genericParams)
            : base(position, qualifiedNamePosition, parameters, genericParams)
		{
            this.qualifiedName = qualifiedName;
            this.fallbackQualifiedName = fallbackQualifiedName;
		}

		/// <include file='Doc/Nodes.xml' path='doc/method[@name="Expression.Analyze"]/*'/>
		internal override Evaluation Analyze(Analyzer/*!*/ analyzer, ExInfoFromParent info)
		{
            base.Analyze(analyzer, info);

            if (isMemberOf == null)
            {
                // function call //

                return AnalyzeFunctionCall(analyzer, ref info);
            }
            else
            {
				// method call //

                Debug.Assert(!this.fallbackQualifiedName.HasValue);   // only valid for global function call
                return AnalyzeMethodCall(analyzer, ref info);
			}
		}

        /// <summary>
        /// Analyze the function call (isMemberOf == null).
        /// </summary>
        /// <param name="analyzer"></param>
        /// <param name="info"></param>
        /// <returns></returns>
        /// <remarks>This code fragment is separated to save the stack when too long Expression chain is being compiled.</remarks>
        private Evaluation AnalyzeFunctionCall(Analyzer/*!*/ analyzer, ref ExInfoFromParent info)
        {
            Debug.Assert(isMemberOf == null);

            // resolve name:
            
            routine = analyzer.ResolveFunctionName(qualifiedName, position);

            if (routine.IsUnknown)
            {
                // note: we've to try following at run time, there can be dynamically added namespaced function matching qualifiedName
                // try fallback
                if (this.fallbackQualifiedName.HasValue)
                {
                    var fallbackroutine = analyzer.ResolveFunctionName(this.fallbackQualifiedName.Value, position);
                    if (fallbackroutine != null && !fallbackroutine.IsUnknown)
                    {
                        if (fallbackroutine is PhpLibraryFunction)  // we are calling library function directly
                            routine = fallbackroutine;
                    }
                }

                if (routine.IsUnknown)   // still unknown ?
                    Statistics.AST.AddUnknownFunctionCall(qualifiedName);
            }
            // resolve overload if applicable:
            RoutineSignature signature;
            overloadIndex = routine.ResolveOverload(analyzer, callSignature, position, out signature);

            Debug.Assert(overloadIndex != DRoutine.InvalidOverloadIndex, "A function should have at least one overload");

            if (routine is PhpLibraryFunction)
            {
                var opts = ((PhpLibraryFunction)routine).Options;
                // warning if not supported function call is detected
                if ((opts & FunctionImplOptions.NotSupported) != 0)
                    analyzer.ErrorSink.Add(Warnings.NotSupportedFunctionCalled, analyzer.SourceUnit, Position, QualifiedName.ToString());

                // warning if function requiring locals is detected (performance critical)
                if ((opts & FunctionImplOptions.NeedsVariables) != 0 && !analyzer.CurrentScope.IsGlobal)
                    analyzer.ErrorSink.Add(Warnings.UnoptimizedLocalsInFunction, analyzer.SourceUnit, Position, QualifiedName.ToString());
                
            }

            // analyze parameters:
            callSignature.Analyze(analyzer, signature, info, false);

            // get properties:
            analyzer.AddCurrentRoutineProperty(routine.GetCallerRequirements());

            // replaces the node if its value can be determined at compile-time:
            object value;
            return TryEvaluate(analyzer, out value) ?
                new Evaluation(this, value) :
                new Evaluation(this);
        }

        /// <summary>
        /// Analyze the method call (isMemberOf != null).
        /// </summary>
        /// <param name="analyzer"></param>
        /// <param name="info"></param>
        /// <returns></returns>
        private Evaluation AnalyzeMethodCall(Analyzer/*!*/ analyzer, ref ExInfoFromParent info)
        {
            Debug.Assert(isMemberOf != null);

            // $this->
            DirectVarUse memberDirectVarUse = isMemberOf as DirectVarUse;
            if (memberDirectVarUse != null && memberDirectVarUse.IsMemberOf == null &&  // isMemberOf is single variable
                memberDirectVarUse.VarName.IsThisVariableName &&                        // isMemberOf if $this
                analyzer.CurrentType != null)                                           // called in class context of known type
            {
                // $this->{qualifiedName}(callSignature)

                bool runtimeVisibilityCheck, isCallMethod;

                routine = analyzer.ResolveMethod(
                    analyzer.CurrentType,//typeof(this)
                    qualifiedName.Name,//.Namespace?
                    Position,
                    analyzer.CurrentType, analyzer.CurrentRoutine, false,
                    out runtimeVisibilityCheck, out isCallMethod);

                Debug.Assert(runtimeVisibilityCheck == false);  // can only be set to true if CurrentType or CurrentRoutine are null

                if (!routine.IsUnknown)
                {
                    // check __call
                    if (isCallMethod)
                    {
                        // TODO: generic args
                        
                        var arg1 = new StringLiteral(this.Position, qualifiedName.Name.Value);
                        var arg2 = this.callSignature.BuildPhpArray();

                        this.callSignature = new CallSignature(
                            new List<ActualParam>(2) {
                                new ActualParam(arg1.Position, arg1, false),
                                new ActualParam(arg2.Position, arg2, false)
                            },
                            new List<TypeRef>());
                    }
                    
                    // resolve overload if applicable:
                    RoutineSignature signature;
                    overloadIndex = routine.ResolveOverload(analyzer, callSignature, position, out signature);

                    Debug.Assert(overloadIndex != DRoutine.InvalidOverloadIndex, "A function should have at least one overload");

                    // analyze parameters:
                    callSignature.Analyze(analyzer, signature, info, false);

                    // get properties:
                    analyzer.AddCurrentRoutineProperty(routine.GetCallerRequirements());

                    return new Evaluation(this);
                }
            }

            // by default, fall back to dynamic method invocation
            routine = null;
            callSignature.Analyze(analyzer, UnknownSignature.Default, info, false);

            return new Evaluation(this);
        }

		#region Evaluation

        /// <summary>
        /// Evaluation info used to get some info from evaluated functions.
        /// </summary>
        public class EvaluateInfo
        {
            public bool emitDeclareLamdaFunction;
            public DRoutine newRoutine;
            public object value;    // evaluated value
        }

        /// <summary>
        /// Modifies AST if possible, in order to generate better code.
        /// </summary>
        /// <remarks>Some well-known constructs can be modified to be analyzed and emitted better.</remarks>
        private void AnalyzeSpecial(Analyzer/*!*/ analyzer)
        {
            if (routine is PhpLibraryFunction)
            {
                // basename(__FILE__, ...) -> basename("actual_file", ...)  // SourceRoot can be ignored in this case
                if (routine.FullName.EqualsOrdinalIgnoreCase("basename"))
                    if (callSignature.Parameters.Count > 0)
                    {
                        var path_param = callSignature.Parameters[0];
                        var path_expr = path_param.Expression;
                        if (path_expr is PseudoConstUse && ((PseudoConstUse)path_expr).Type == PseudoConstUse.Types.File)
                            callSignature.Parameters[0] = new ActualParam(path_param.Position, new StringLiteral(path_expr.Position, analyzer.SourceUnit.SourceFile.RelativePath.Path), false);
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
		private bool TryEvaluate(Analyzer/*!*/ analyzer, out object value)
		{
            // special cases, allow some AST transformation:
            this.AnalyzeSpecial(analyzer);

            // try evaluate function call in compile time:
            if (callSignature.AllParamsHaveValue)
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
                                if (nextCallParamIndex >= callSignature.Parameters.Count)
                                    throw new ArgumentException("Not enough parameters in evaluable method.");

                                object obj = callSignature.Parameters[nextCallParamIndex++].Expression.Value;
                                invokeParameters[i] = converter(obj);
                            };

                        // special params types:
                        if (paramType == typeof(Analyzer))
                        {
                            invokeParameters[i] = analyzer;
                        }
                        else if (paramType == typeof(CallSignature))
                        {
                            invokeParameters[i] = callSignature;
                        }
                        else if (   // ... , params object[] // last parameter
                            paramType == typeof(object[]) &&
                            i == parametersInfo.Length - 1 &&
                            parametersInfo[i].IsDefined(typeof(ParamArrayAttribute), false))
                        {
                            // params object[]
                            var args = new object[callSignature.Parameters.Count - nextCallParamIndex];
                            for (int arg = 0; arg < args.Length; ++nextCallParamIndex, ++arg)
                                args[arg] = callSignature.Parameters[nextCallParamIndex].Expression.Value;

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
                            PassArgument(obj=>(object)Convert.ObjectToInteger(obj));
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
                            analyzer.ErrorSink.AddInternal(
                                -2,
                                message, (error == PhpError.Error || error == PhpError.CoreError || error == PhpError.UserError) ? ErrorSeverity.Error : ErrorSeverity.Warning,
                                (int)WarningGroups.None,
                                analyzer.SourceUnit.GetMappedFullSourcePath(Position.FirstLine),
                                new ErrorPosition(
                                    analyzer.SourceUnit.GetMappedLine(Position.FirstLine), Position.FirstColumn,
                                    analyzer.SourceUnit.GetMappedLine(Position.LastLine), Position.LastColumn),
                                true
                                );
                        };
                    
                    // invoke the method and get the result
                    try
                    {
                        value = evaluableMethod.Invoke(null, invokeParameters);

                        if (evaluableMethod.ReturnType == typeof(EvaluateInfo))
                        {
                            var info = value as EvaluateInfo;

                            if (info != null && info.emitDeclareLamdaFunction && info.newRoutine != null)
                            {
                                this.routine = info.newRoutine;
                                this.inlined = InlinedFunction.CreateFunction;
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

			switch (callSignature.Parameters.Count)
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

        /// <summary>
        /// Returns string representation of <see cref="fallbackQualifiedName"/> or <c>null</c> reference if the fallback name is not needed.
        /// </summary>
        private string fallbackFunctionName { get { return fallbackQualifiedName.HasValue ? fallbackQualifiedName.Value.ToString() : null; } }

        /// <include file='Doc/Nodes.xml' path='doc/method[@name="Emit"]/*'/>
		internal override PhpTypeCode Emit(CodeGenerator/*!*/ codeGenerator)
		{
			Debug.Assert(access == AccessType.Read || access == AccessType.ReadRef || access == AccessType.ReadUnknown
				|| access == AccessType.None, "Invalid access type in FunctionCall");
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
                if (isMemberOf != null)
                {
                    // to avoid StackOverflowException due to long isMemberOf chain,
                    // we will avoid recursion, and divide the chain into smaller pieces.
                    HandleLongChain(codeGenerator);

                    if (routine == null)
                    {
                        //result = codeGenerator.EmitRoutineOperatorCall(null, isMemberOf, qualifiedName.ToString(), null, null, callSignature, access);
                        result = codeGenerator.CallSitesBuilder.EmitMethodCall(
                            codeGenerator,
                            Compiler.CodeGenerator.CallSitesBuilder.AccessToReturnType(access),
                            isMemberOf, null,
                            qualifiedName.ToString(), null,
                            callSignature);
                    }
                    else
                        result = routine.EmitCall(
                            codeGenerator, null, callSignature,
                            new ExpressionPlace(codeGenerator, isMemberOf), false, overloadIndex,
                            null/*TODO when CFG*/, position, access, true);
                }
                else
                {
                    // the node represents a function call:
                    result = routine.EmitCall(
                        codeGenerator, fallbackFunctionName,
                        callSignature, null, false, overloadIndex,
                        null, position, access, false);
                }
			}

            // (J) Emit Copy if necessary:
            // routine == null => Copy emitted by EmitRoutineOperatorCall
            // routine.ReturnValueDeepCopyEmitted => Copy emitted
            // otherwise emit Copy if we are going to read it by value
            if (routine != null && !routine.ReturnValueDeepCopyEmitted)
                EmitReturnValueCopy(codeGenerator.IL, result);
            
            // handles return value:
			codeGenerator.EmitReturnValueHandling(this, codeGenerator.ChainBuilder.LoadAddressOfFunctionReturnValue, ref result);

            return result;
		}

        /// <summary>
        /// To avoid <see cref="StackOverflowException"/> due to long <see cref="VarLikeConstructUse.IsMemberOf"/> chain,
        /// we will avoid recursion, and divide the chain into smaller pieces.
        /// </summary>
        private void HandleLongChain(CodeGenerator/*!*/ codeGenerator)
        {
            int length = 300;
            VarLikeConstructUse p = this.isMemberOf;
            while (p != null && length > 0)
            {
                if (p.GetType() == typeof(DirectFcnCall) && ((DirectFcnCall)p).alreadyEmittedPlace != null)
                    return; // chain already divided here

                p = p.IsMemberOf;
                length--;
            }

            if (length == 0 && p != null && p.GetType() == typeof(DirectFcnCall))
            {
                var fcn = (DirectFcnCall)p;

                var result = fcn.Emit(codeGenerator);
                fcn.alreadyEmittedPlace = codeGenerator.IL.DeclareLocal(PhpTypeCodeEnum.ToType(result));
                codeGenerator.IL.Emit(OpCodes.Stloc, fcn.alreadyEmittedPlace);
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


		internal override void DumpTo(AstVisitor visitor, System.IO.TextWriter output)
		{
			if (isMemberOf != null)
			{
				isMemberOf.DumpTo(visitor, output);
				output.Write("->");
			}

			output.Write(qualifiedName);
			DumpArguments(visitor, output);
			DumpAccess(output);
		}

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitDirectFcnCall(this);
        }
	}

	#endregion

	#region IndirectFcnCall

	public sealed class IndirectFcnCall : FunctionCall
	{
		internal override Operations Operation { get { return Operations.IndirectCall; } }

		internal Expression/*!*/ NameExpr { get { return nameExpr; } }
		private Expression/*!*/ nameExpr;

		public IndirectFcnCall(Position p, Expression/*!*/ nameExpr, List<ActualParam>/*!*/ parameters,
	  List<TypeRef>/*!*/ genericParams)
            : base(p, nameExpr.Position, parameters, genericParams)
		{
			this.nameExpr = nameExpr;
		}

		/// <include file='Doc/Nodes.xml' path='doc/method[@name="Expression.Analyze"]/*'/>
		internal override Evaluation Analyze(Analyzer/*!*/ analyzer, ExInfoFromParent info)
		{
			base.Analyze(analyzer, info);

			nameExpr = nameExpr.Analyze(analyzer, ExInfoFromParent.DefaultExInfo).Literalize();

			callSignature.Analyze(analyzer, UnknownSignature.Default, info, false);

			// function call:
			if (isMemberOf == null)
				analyzer.AddCurrentRoutineProperty(RoutineProperties.ContainsIndirectFcnCall);

			return new Evaluation(this);
		}

		/// <include file='Doc/Nodes.xml' path='doc/method[@name="Emit"]/*'/>
		internal override PhpTypeCode Emit(CodeGenerator codeGenerator)
		{
			Debug.Assert(access == AccessType.Read || access == AccessType.ReadRef ||
				access == AccessType.ReadUnknown || access == AccessType.None);
			Statistics.AST.AddNode("FunctionCall.Indirect");

			PhpTypeCode result;
			result = codeGenerator.EmitRoutineOperatorCall(null, isMemberOf, null, null, nameExpr, callSignature, access);
            //EmitReturnValueCopy(codeGenerator.IL, result); // (J) already emitted by EmitRoutineOperatorCall
            
            codeGenerator.EmitReturnValueHandling(this, codeGenerator.ChainBuilder.LoadAddressOfFunctionReturnValue, ref result);

            return result;
		}

		internal override void DumpTo(AstVisitor/*!*/ visitor, TextWriter/*!*/ output)
		{
			if (isMemberOf != null)
			{
				isMemberOf.DumpTo(visitor, output);
				output.Write("->");
			}

			output.Write('{');
			nameExpr.DumpTo(visitor, output);
			output.Write('}');
			DumpArguments(visitor, output);
			DumpAccess(output);
		}

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitIndirectFcnCall(this);
        }
	}

	#endregion

	#region StaticMtdCall

	public abstract class StaticMtdCall : FunctionCall
	{
        public GenericQualifiedName ClassName { get { return typeRef.GenericQualifiedName; } }
        protected readonly TypeRef/*!*/typeRef;

        /// <summary>
        /// Position of <see cref="ClassName"/> in source code.
        /// </summary>
        public Position ClassNamePosition { get { return this.typeRef.Position; } }

        protected DType/*!A*/type;

        public StaticMtdCall(Position position, Position methodNamePosition, GenericQualifiedName className, Position classNamePosition, List<ActualParam>/*!*/ parameters, List<TypeRef>/*!*/ genericParams)
            : this(position, methodNamePosition, DirectTypeRef.FromGenericQualifiedName(classNamePosition, className), parameters, genericParams)
		{	
		}

        public StaticMtdCall(Position position, Position methodNamePosition, TypeRef typeRef, List<ActualParam>/*!*/ parameters, List<TypeRef>/*!*/ genericParams)
            : base(position, methodNamePosition, parameters, genericParams)
        {
            Debug.Assert(typeRef != null);

            this.typeRef = typeRef;
        }

		internal override Evaluation Analyze(Analyzer/*!*/ analyzer, ExInfoFromParent info)
		{
			base.Analyze(analyzer, info);
            this.typeRef.Analyze(analyzer);
            this.type = this.typeRef.ResolvedTypeOrUnknown;

			// analyze constructed type (new constructed type cane be used here):
			analyzer.AnalyzeConstructedType(type);

			if (type.TypeDesc.Equals(DTypeDesc.InterlockedTypeDesc))
				analyzer.ErrorSink.Add(Warnings.ClassBehaviorMayBeUnexpected, analyzer.SourceUnit, position, type.FullName);

			return new Evaluation(this);
		}
	}

	#endregion

	#region DirectStMtdCall

	public sealed class DirectStMtdCall : StaticMtdCall
	{
		internal override Operations Operation { get { return Operations.DirectStaticCall; } }

		private Name methodName;
        public Name MethodName { get { return methodName; } }
		private DRoutine method;
		private int overloadIndex = DRoutine.InvalidOverloadIndex;
		private bool runtimeVisibilityCheck;

		public DirectStMtdCall(Position position, ClassConstUse/*!*/ classConstant, List<ActualParam>/*!*/ parameters,
	  List<TypeRef>/*!*/ genericParams)
			: base(position, classConstant.NamePosition, classConstant.TypeRef, parameters, genericParams)
		{
			this.methodName = new Name(classConstant.Name.Value);
		}

		public DirectStMtdCall(Position position, GenericQualifiedName className, Position classNamePosition, Name methodName, Position methodNamePosition, List<ActualParam>/*!*/ parameters,
		  List<TypeRef>/*!*/ genericParams)
			: base(position, methodNamePosition, className, classNamePosition, parameters, genericParams)
		{
			this.methodName = methodName;
		}

		/// <include file='Doc/Nodes.xml' path='doc/method[@name="Expression.Analyze"]/*'/>
		internal override Evaluation Analyze(Analyzer/*!*/ analyzer, ExInfoFromParent info)
		{
			base.Analyze(analyzer, info);

            // look for the method:
            bool isCallMethod;
            method = analyzer.ResolveMethod(
                type, methodName, position, analyzer.CurrentType, analyzer.CurrentRoutine,
                true, out runtimeVisibilityCheck, out isCallMethod);

			if (!method.IsUnknown)
			{
				// we are sure about the method //

				if (method.IsAbstract)
				{
					analyzer.ErrorSink.Add(Errors.AbstractMethodCalled, analyzer.SourceUnit, position,
						method.DeclaringType.FullName, method.FullName);
				}
			}

            // check __callStatic
            if (isCallMethod)
            {
                // TODO: generic args

                // create new CallSignature({function name},{args})
                var arg1 = new StringLiteral(this.Position, methodName.Value);
                var arg2 = this.callSignature.BuildPhpArray();

                this.callSignature = new CallSignature(
                    new List<ActualParam>(2) {
                                new ActualParam(arg1.Position, arg1, false),
                                new ActualParam(arg2.Position, arg2, false)
                            },
                    new List<TypeRef>());
            }

            // analyze the method
			RoutineSignature signature;
			overloadIndex = method.ResolveOverload(analyzer, callSignature, position, out signature);

			Debug.Assert(overloadIndex != DRoutine.InvalidOverloadIndex, "Each method should have at least one overload");

            // analyze arguments
			callSignature.Analyze(analyzer, signature, info, false);

			return new Evaluation(this);
		}

		/// <include file='Doc/Nodes.xml' path='doc/method[@name="Emit"]/*'/>
		internal override PhpTypeCode Emit(CodeGenerator/*!*/ codeGenerator)
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
			PhpTypeCode result = method.EmitCall(codeGenerator, null, callSignature, instance, runtimeVisibilityCheck,
				overloadIndex, type, position, access, false/* TODO: __call must be called virtually */);

            if (/*method == null || */!method.ReturnValueDeepCopyEmitted)   // (J) Emit Copy only if method is known (=> known PhpRoutine do not emit Copy on return value)
                EmitReturnValueCopy(codeGenerator.IL, result);  // only if we are going to read the resulting value

			// handles return value:
			codeGenerator.EmitReturnValueHandling(this, codeGenerator.ChainBuilder.LoadAddressOfFunctionReturnValue, ref result);

			return result;
		}

		internal override void DumpTo(AstVisitor/*!*/ visitor, TextWriter/*!*/ output)
		{
			if (isMemberOf != null)
			{
				isMemberOf.DumpTo(visitor, output);
				output.Write("->");
			}

            typeRef.DumpTo(visitor, output);
			output.Write("::");
			output.Write(methodName.ToString());
			DumpArguments(visitor, output);
			DumpAccess(output);
		}

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitDirectStMtdCall(this);
        }
	}

	#endregion

	#region IndirectStMtdCall

	public class IndirectStMtdCall : StaticMtdCall
	{
		internal override Operations Operation { get { return Operations.IndirectStaticCall; } }

		private CompoundVarUse/*!*/ methodNameVar;
        /// <summary>Expression that represents name of method</summary>
        public CompoundVarUse/*!*/ MethodNameVar { get { return methodNameVar; } }

		public IndirectStMtdCall(Position position,
                                 GenericQualifiedName className, Position classNamePosition, CompoundVarUse/*!*/ mtdNameVar,
	                             List<ActualParam>/*!*/ parameters, List<TypeRef>/*!*/ genericParams)
            : base(position, mtdNameVar.Position, className, classNamePosition, parameters, genericParams)
		{
			this.methodNameVar = mtdNameVar;
		}

        public IndirectStMtdCall(Position position,
                                 TypeRef/*!*/typeRef, CompoundVarUse/*!*/ mtdNameVar,
                                 List<ActualParam>/*!*/ parameters, List<TypeRef>/*!*/ genericParams)
            : base(position, mtdNameVar.Position, typeRef, parameters, genericParams)
        {
            this.methodNameVar = mtdNameVar;
        }

		/// <include file='Doc/Nodes.xml' path='doc/method[@name="Expression.Analyze"]/*'/>
		internal override Evaluation Analyze(Analyzer/*!*/ analyzer, ExInfoFromParent info)
		{
			base.Analyze(analyzer, info);

			methodNameVar.Analyze(analyzer, ExInfoFromParent.DefaultExInfo);

			callSignature.Analyze(analyzer, UnknownSignature.Default, info, false);

			return new Evaluation(this);
		}

		/// <include file='Doc/Nodes.xml' path='doc/method[@name="Emit"]/*'/>
		internal override PhpTypeCode Emit(CodeGenerator/*!*/ codeGenerator)
		{
			Statistics.AST.AddNode("StaticMethodCall.Indirect");

			PhpTypeCode result = codeGenerator.EmitRoutineOperatorCall(type, null, null, null, methodNameVar, callSignature, access);
            //EmitReturnValueCopy(codeGenerator.IL, result); // (J) already emitted by EmitRoutineOperatorCall

			// handles return value:
			codeGenerator.EmitReturnValueHandling(this, codeGenerator.ChainBuilder.LoadAddressOfFunctionReturnValue, ref result);

			return result;
		}

		internal override void DumpTo(AstVisitor/*!*/ visitor, TextWriter/*!*/ output)
		{
			if (isMemberOf != null)
			{
				isMemberOf.DumpTo(visitor, output);
				output.Write("->");
			}

            typeRef.DumpTo(visitor, output);
			output.Write("::");
			output.Write('{');
			methodNameVar.DumpTo(visitor, output);
			output.Write('}');
			DumpArguments(visitor, output);
			DumpAccess(output);
		}

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitIndirectStMtdCall(this);
        }
	}

	#endregion
}
