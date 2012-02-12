/*

 Copyright (c) 2004-2006 Tomas Matousek.  

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/
using System;
using System.Reflection;
using System.ComponentModel;
using System.Collections.Generic;
using PHP.Core;
using PHP.Core.Reflection;

#if SILVERLIGHT
using PHP.CoreCLR;
#endif

namespace PHP.Library
{
	/// <summary>
	/// Provides means for PHP functions handling.
	/// </summary>
	/// <threadsafety static="true"/>
	public static class PhpFunctions
	{
		#region call_user_func, call_user_func_array, create_function

		/// <summary>
		/// Calls a function or a method defined by callback with given arguments.
		/// </summary>
        /// <param name="caller">DTypeDesc of the caller's class context. Can be UnknownTypeDesc.</param>
        /// <param name="function">The function or metod designation.</param>
		/// <param name="args">The arguments.</param>
		/// <returns>The return value.</returns>
		[ImplementsFunction("call_user_func", FunctionImplOptions.NeedsClassContext)]
		public static object CallUserFunction(DTypeDesc caller, PhpCallback function, params object[] args)
		{
			if (function == null)
			{
				PhpException.ArgumentNull("function");
				return null;
			}
			if (function.IsInvalid) return null;

			// invoke the callback:
			return PhpVariable.Dereference(function.Invoke(caller, args));
		}

		/// <summary>
		/// Calls a function or a method defined by callback with arguments stored in an array.
		/// </summary>
        /// <param name="caller">DTypeDesc of the caller's class context. Can be UnknownTypeDesc.</param>
        /// <param name="function">The function or method designation.</param>
		/// <param name="args">The arguments. Can be null.</param>
		/// <returns>The returned value.</returns>
        [ImplementsFunction("call_user_func_array", FunctionImplOptions.NeedsClassContext)]
        public static object CallUserFunctionArray(DTypeDesc caller, PhpCallback function, PhpArray args)
		{
			object[] args_array;

            if (args != null)
            {
                args_array = new object[args.Count];
                args.CopyValuesTo(args_array, 0);
            }
            else
            {
                args_array = ArrayUtils.EmptyObjects;
            }

			return CallUserFunction(caller, function, args_array);
		}

		/// <summary>
		/// Creates a new lambda function given its arguments and body.
		/// </summary>
		/// <param name="args">A source code defining function signature, e.g. "$a,MyClass $b,$c = null".</param>
		/// <param name="body">A source code defining function body, e.g. "return $a + $b->x + $c;"</param>
		/// <returns>A name of the created function.</returns>
		[ImplementsFunction("create_function", FunctionImplOptions.CaptureEvalInfo /*| FunctionImplOptions.Special*/)]
        [PureFunction(typeof(PhpFunctions), "CreateFunction_Analyze")]
		public static string CreateFunction(string args, string body)
		{
			ScriptContext context = ScriptContext.CurrentContext;
			return DynamicCode.CreateLambdaFunction(args, body, context, context.GetCapturedSourceCodeDescriptor());
        }

        #region analyzer of create_function

        public static PHP.Core.AST.DirectFcnCall.EvaluateInfo CreateFunction_Analyze(
            Analyzer analyzer,
            PHP.Core.AST.CallSignature callSignature,
            string args, string body)
        {
            if (analyzer.IsInsideIncompleteClass())
                return null;  // in this case, the DirectFnCall will not be Emitted. Therefore the lambda routine will not be declared and compilation will fail when emitting not fully declared lambda FunctionDecl.

            // has to be a valid identifier:
            // actually this name is never used then
            string function_name = "__" + Guid.NewGuid().ToString().Replace('-', '_'); //DynamicCode.GenerateLambdaName(args, body);

            string prefix1, prefix2;
            DynamicCode.GetLamdaFunctionCodePrefixes(function_name, args, out prefix1, out prefix2);

            PHP.Core.Parsers.Position pos_args = callSignature.Parameters[0].Position;
            PHP.Core.Parsers.Position pos_body = callSignature.Parameters[1].Position;

            // function __XXXXXX(<args>){<fill><body>}
            string fill = GetInlinedLambdaCodeFill(pos_args, pos_body);
            string code = String.Concat(prefix2, fill, body, "}");

            // the position of the first character of the parsed code:
            // (note that escaped characters distort position a little bit, which cannot be eliminated so easily)
            PHP.Core.Parsers.Position pos = PHP.Core.Parsers.Position.Initial;
            pos.FirstOffset = pos_args.FirstOffset - prefix1.Length + 1;
            pos.FirstColumn = pos_args.FirstColumn - prefix1.Length + 1;
            pos.FirstLine = pos_args.FirstLine;

            // parses function source code:
            var counter = new PHP.Core.Parsers.Parser.ReductionsCounter();
            var ast = analyzer.BuildAst(pos, code, counter);
            if (ast == null || ast.Statements == null)
                return null;   // the function cannot be parsed

            Debug.Assert(counter.FunctionCount == 1);

            var decl_node = (PHP.Core.AST.FunctionDecl)ast.Statements[0];

            // adds declaration to the end of the global code statement list:
            analyzer.AddLambdaFcnDeclaration(decl_node);

            //
            return new PHP.Core.AST.DirectFcnCall.EvaluateInfo()
            {
                //.inlined = InlinedFunction.CreateFunction;
                emitDeclareLamdaFunction = true,

                // modify declaration:
                newRoutine = decl_node.ConvertToLambda(analyzer),
            };
        }

        /// <summary>
        /// Gets a string which is used as a fill in the code to be parsed in order to maintain
        /// correct token positioning.
        /// </summary>
        /// <param name="args">A position of string literal holding source code for lambda function arguments.</param>
        /// <param name="body">A position of string literal holding source code for the body.</param>
        /// <returns>A string containing spaces and end-of-line characters '\n'.</returns>
        private static string GetInlinedLambdaCodeFill(PHP.Core.Parsers.Position args, PHP.Core.Parsers.Position body)
        {
            int delta_lines = body.FirstLine - args.LastLine;

            if (delta_lines == 0)
            {
                // ....args.......'_____,_______________'.......body.....
                // ...............)_________fill________{................
                return new String(' ', body.FirstColumn - args.LastColumn - 1);
            }
            else
            {
                // source:
                // .....args.....'_____\r\n
                // _________,_____\r\n
                // ____________'......body..... 

                // code to parse:
                // .....args....'\n
                // \n
                // ____fill____{.....body......

                // the same number of lines as it is in the source file + leading columns:
                return new System.Text.StringBuilder(delta_lines + body.FirstColumn).
                  Append('\n', delta_lines).Append(' ', body.FirstColumn).ToString();
            }
        }

        #endregion

        #endregion

        #region func_num_args, func_get_arg, func_get_args

        /// <summary>
		/// Retrieves the number of arguments passed to the current user-function.
		/// </summary>
		/// <remarks><seealso cref="PhpStack.GetArgCount"/></remarks>
		[ImplementsFunction("func_num_args", FunctionImplOptions.NeedsFunctionArguments)]
		public static int GetArgsNumber()
		{
			int arg_count, type_arg_count;
			return ScriptContext.CurrentContext.Stack.GetArgCount(out arg_count, out type_arg_count) ? arg_count : -1;
		}

		/// <summary>
		/// Retrieves an argument passed to the current user-function.
		/// </summary>
		/// <remarks><seealso cref="PhpStack.GetArgument"/></remarks>
		[ImplementsFunction("func_get_arg", FunctionImplOptions.NeedsFunctionArguments)]
		[return: PhpDeepCopy]
		public static object GetArg(int index)
		{
			return ScriptContext.CurrentContext.Stack.GetArgument(index);
		}

		/// <summary>
		/// Returns an array of arguments of the current user-defined function. 
		/// </summary>
		/// <remarks><seealso cref="PhpStack.GetArguments"/>
        /// Also throws warning if called from global scope.</remarks>
		[ImplementsFunction("func_get_args", FunctionImplOptions.NeedsFunctionArguments)]
		[return: PhpDeepCopy]
        [return: CastToFalse]
		public static PhpArray GetArgs()
		{
			PhpArray result = ScriptContext.CurrentContext.Stack.GetArguments();
            if (result != null)
            {
                result.InplaceCopyOnReturn = true;
            }

			return result;
		}

		#endregion

		#region func_num_generic_args, func_get_generic_arg, func_get_generic_args (PHP/CLR)

		/// <summary>
		/// Retrieves the number of generic type arguments passed to the current user-function.
		/// </summary>
		[ImplementsFunction("func_num_generic_args", FunctionImplOptions.NeedsFunctionArguments)]
		public static int GetGenericArgsNumber()
		{
			int arg_count, type_arg_count;
			return ScriptContext.CurrentContext.Stack.GetArgCount(out arg_count, out type_arg_count) ? type_arg_count : -1;
		}

		/// <summary>
		/// Retrieves a fully qualified name of the generic type argument passed to the current user-function.
		/// </summary>
		[ImplementsFunction("func_get_generic_arg", FunctionImplOptions.NeedsFunctionArguments)]
		[return: PhpDeepCopy]
		public static string GetGenericArg(int index)
		{
			DTypeDesc type_desc = ScriptContext.CurrentContext.Stack.GetTypeArgument(index);
			return (type_desc != null) ? type_desc.MakeFullName() : null;
		}

		/// <summary>
		/// Returns an array of names of generic type arguments of the current user-defined function. 
		/// </summary>
		[ImplementsFunction("func_get_generic_args", FunctionImplOptions.NeedsFunctionArguments)]
		[return: PhpDeepCopy]
		public static PhpArray GetGenericArgs()
		{
			DTypeDesc[] type_descs = ScriptContext.CurrentContext.Stack.GetTypeArguments();
			if (type_descs == null) return null;

			PhpArray result = new PhpArray(type_descs.Length, 0);

			foreach (DTypeDesc type_desc in type_descs)
				result.Add(type_desc.MakeFullName());

			result.InplaceCopyOnReturn = true;
			return result;
		}

		#endregion

		#region function_exists, get_defined_functions

		/// <summary>
		/// Determines whether a function with a specified name exists.
		/// </summary>
		/// <param name="name">The name of the function.</param>
		/// <returns>Wheter the function exists.</returns>
		/// <remarks>User functions which are declared conditionally and was not declared yet is considered as not existent.</remarks>
		[ImplementsFunction("function_exists"/*, FunctionImplOptions.Special*/)]
        [PureFunction(typeof(PhpFunctions), "Exists_Analyze")]
        public static bool Exists(string name)
		{
			return ScriptContext.CurrentContext.ResolveFunction(name, null, true) != null;
        }

        #region analyzer of function_exists

        public static PHP.Core.AST.DirectFcnCall.EvaluateInfo Exists_Analyze(Analyzer analyzer, string name)
        {
            QualifiedName? alias;

            DRoutine routine = analyzer.SourceUnit.ResolveFunctionName(
                new QualifiedName(new Name(name)),
                analyzer.CurrentScope,
                out alias,
                null,
                PHP.Core.Parsers.Position.Invalid,
                false);

            if (routine == null || routine.IsUnknown)
                return null;  // function is not known at the compilation time. However it can be defined at the runtime (dynamic include, script library, etc).

            return new PHP.Core.AST.DirectFcnCall.EvaluateInfo()
            {
                value = true    // function is definitely known the the compilation time
            };
        }

        #endregion

        /// <summary>
		/// Retrieves defined functions.
		/// </summary>
		/// <returns>
		/// The <see cref="PhpArray"/> containing two entries with keys "internal" and "user".
		/// The former's value is a <see cref="PhpArray"/> containing PHP library functions as values.
		/// The latter's value is a <see cref="PhpArray"/> containing user defined functions as values.
		/// Keys of both these arrays are integers starting from 0.
		/// </returns>
		/// <remarks>User functions which are declared conditionally and was not declared yet is considered as not existent.</remarks>
		[ImplementsFunction("get_defined_functions")]
		public static PhpArray GetDefinedFunctions()
		{
			PhpArray result = new PhpArray(0, 2);
			PhpArray library = new PhpArray(0, 500);
			PhpArray user = new PhpArray();

			ScriptContext.CurrentContext.GetDeclaredFunctions(user, library);

			result["internal"] = library;
			result["user"] = user;
			return result;
		}

		#endregion

		#region register_shutdown_function

		/// <summary>
		/// Registers callback which will be called when script processing is complete but before the request
		/// has been complete.
        /// Function has no return value.
		/// </summary>
		/// <param name="function">The function which is called after main code of the script is finishes execution.</param>
		/// <param name="parameters">Parameters for the function.</param>
		/// <remarks>
		/// Although, there is explicitly written in the PHP manual that it is not possible 
		/// to send an output to a browser via echo or another output handling functions you can actually do so.
		/// There is no such limitation with Phalanger.
		/// </remarks>
		[ImplementsFunction("register_shutdown_function")]
		public static void RegisterShutdownFunction(PhpCallback/*!*/ function, params object[] parameters)
		{
			if (function == null)
			{
				PhpException.ArgumentNull("function");
				return;
			}

			ScriptContext.CurrentContext.RegisterShutdownCallback(function, parameters);
		}

		#endregion

		#region NS: register_tick_function, unregister_tick_function

		/// <summary>
		/// Not supported.
		/// </summary>
		[ImplementsFunction("register_tick_function", FunctionImplOptions.NotSupported)]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static void RegisterTickFunction(PhpCallback function)
		{
			PhpException.FunctionNotSupported();
		}

		/// <summary>
		/// Not supported.
		/// </summary>
		[ImplementsFunction("register_tick_function", FunctionImplOptions.NotSupported)]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static void RegisterTickFunction(PhpCallback function, object arg)
		{
			PhpException.FunctionNotSupported();
		}

		/// <summary>
		/// Not supported.
		/// </summary>
		[ImplementsFunction("unregister_tick_function", FunctionImplOptions.NotSupported)]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static void UnregisterTickFunction(PhpCallback function)
		{
			PhpException.FunctionNotSupported();
		}

		#endregion
	}
}
