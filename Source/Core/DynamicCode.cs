/*

 Copyright (c) 2004-2006 Tomas Matousek.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.IO;
using System.Threading;
using System.Reflection;
using System.Globalization;
using System.Reflection.Emit;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using PHP.Core.Emit;

namespace PHP.Core
{
	#region TypesProvider

	using PHP.Core.Reflection;
	using ProvidedType = KeyValuePair<string, PHP.Core.Reflection.DTypeDesc>;

	/// <summary>
	/// Provides access to class and interface declarators and 
	/// remembers which of them were provided and so the user depends on them.
	/// </summary>
	public sealed class TypesProvider
	{
		/// <summary>
		/// A current script context in which the declarations are provided.
		/// </summary>
		private readonly ScriptContext context;

		/// <summary>
		/// A current type context in which the declarations are provided.
		/// </summary>
		private readonly DTypeDesc caller;

		/// <summary>
		/// A hybrid dictionary of types that has been provided since since the creation of the provider.
		/// </summary>
		public List<ProvidedType> ProvidedTypes { get { return providedTypes; } }
		private List<ProvidedType> providedTypes;

		/// <param name="context">A script context to get declarators from.</param>
		/// <param name="caller">A current type context.</param>
		public TypesProvider(ScriptContext/*!*/ context, DTypeDesc/*!*/ caller)
		{
			Debug.Assert(context != null && caller != null);

			this.context = context;
			this.caller = caller;
			Debug.WriteLine("PROVIDER", "created");
		}

		/// <summary>
		/// Provides the caller a type of a specified name. The caller is made dependent on the requested type.
		/// Important: the caller is made dependent also on non-existing type (i.e. if the method returns <B>null</B>)! 
		/// </summary>
		/// <param name="name">The name of requested file (case insensitive).</param>
		/// <returns>The requested type or <B>null</B> if the type has not been declared on the associated context.</returns>
		public DTypeDesc ProvideType(string name)
		{
			DTypeDesc type = context.DeclaredTypes[name];

			// remembers phantoms - the types which are not contained in the declarators table:
			if (providedTypes == null)
				providedTypes = new List<ProvidedType>();

			providedTypes.Add(new ProvidedType(name, type));

			Debug.WriteLine("PROVIDER", "Added: {0} ({1})", name, type.RealType.FullName);

			return type;
		}

		/// <summary>
		/// Determines whether a type has been declared on the current context.
		/// Doesn't make the caller dependent on the existance of the declarator!
		/// The caller mustn't use this methods if its result is used in a way establishing a dependency.
		/// </summary>
		/// <param name="name">The type name.</param>
		/// <returns></returns>
		public bool IsTypeDeclared(string name)
		{
			return context.DeclaredTypes.ContainsKey(name);
		}

		/// <summary>
		/// Searches for type using <see cref="ScriptContext.ResolveType"/>. If the type is found 
		/// and has a declarator (is a user type) then the caller is made dependent on it.
		/// </summary>
		/// <returns>The type.</returns>
		public DTypeDesc FindAndProvideType(string name)
		{
			// finds a type - searches in script context and in libraries, may also run __autoload:
			DTypeDesc type = context.ResolveType(name, null, caller, null, ResolveTypeFlags.UseAutoload);

			// if the type is a user type then does provide the type:
			if (type != null && context.DeclaredTypes.ContainsKey(name))
			{
				if (providedTypes == null)
					providedTypes = new List<ProvidedType>();

				providedTypes.Add(new ProvidedType(name, type));

				Debug.WriteLine("PROVIDER", "Added: {0} ({1})", name, type.RealType.FullName);
			}

			return type;
		}

		/// <summary>
        /// Try to load all <paramref name="providedTypes"/>. This can invoke autoloading if necessary.
        /// Check if they were not modified, so calling compilation unit has to be invalidated and recompiled.
        /// </summary>
		/// <param name="providedTypes">a list of provided type declarators. Can be a <B>null</B> reference.</param>
		/// <param name="target">The script context to be checked.</param>
        /// <param name="caller">Current class context.</param>
		/// <returns><paramref name="providedTypes"/> are loadable and match in context of <paramref name="target"/>.</returns>
        public static bool LoadAndMatch(List<ProvidedType> providedTypes, ScriptContext/*!*/ target, DTypeDesc caller)
		{
			Debug.Assert(target != null);

            if (providedTypes != null && providedTypes.Count > 0)
			{
                //// there is less declarators than we require:
                //if (target.DeclaredTypes.Count < providedTypes.Count) return false;

				// looks up each provided declarator in the target context:
				foreach (ProvidedType declarator in providedTypes)
				{
                    //DTypeDesc decl_type;
                    //target.DeclaredTypes.TryGetValue(declarator.Key, out decl_type);

                    // When class is compiled in runtime, autoload is invoked on base class (if isn't already declared). 
                    // We have to call autoload on the base class also in transient assembly
                    var decl_type = target.ResolveType(declarator.Key, null, caller, null, ResolveTypeFlags.UseAutoload);
                    if (decl_type == null || decl_type.RealType != declarator.Value.RealType)
						return false;
				}
			}

			return true;
		}
	}

	#endregion

	/// <summary>
	/// Kinds of eval.
	/// </summary>
	public enum EvalKinds
	{
		Unknown,
		ExplicitEval,
		SyntheticEval,
		LambdaFunction,
		Assert,

		DynamicInclude // used on Silverlight
	}

	#region Dynamic Code

	/// <summary>
	/// Provides means for executing code (evals, asserts, lambda functions) dynamically.
	/// </summary>
	[DebuggerNonUserCode]
	public static class DynamicCode
	{
		internal const string LambdaFunctionName = "Lambda_";
		internal const string InlinedLambdaFunctionName = "InlinedLambda";
		internal static readonly Type/*!*/ DynamicMethodType = typeof(DynamicCode);

        /// <summary>
        /// An index used in lambda function name. Common for all threads, increased interlocked.
        /// </summary>
        private static int LambdaFunctionIndex = 0;

		/// <summary>
		/// Generates a name for a lambda function.
		/// </summary>
		/// <returns>The name.</returns>
        internal static string/*!*/ GenerateLambdaName()
		{
            uint index = (uint)Interlocked.Increment(ref LambdaFunctionIndex);
            return LambdaFunctionName + index;
		}

        /// <summary>
        /// Generates a name for a lambda function.
        /// </summary>
        /// <param name="parameters">Parameters string.</param>
        /// <param name="body">Function body string.</param>
        /// <returns>The name.</returns>
        public static string/*!*/ GenerateLambdaName(string/*!*/ parameters, string/*!*/ body)
        {
            return LambdaFunctionName + 
                unchecked((uint)body.GetHashCode()).ToString() + "_" + 
                unchecked((uint)parameters.GetHashCode()).ToString() + 
                (parameters.Length + body.Length).ToString();
        }

		/// <summary>
		/// Gets prefixes of lambda function source code.
		/// First prefix is "function {name}(", the second is "function {name}({parameters}){".
		/// </summary>
		public static void GetLamdaFunctionCodePrefixes(string name, string parameters, out string prefix1, out string prefix2)
		{
			prefix1 = String.Concat("function ", name, "(");
			prefix2 = String.Concat(prefix1, parameters, "){");
		}

		/// <summary>
		/// Compiles a function with a specified parameters and body and adds it to dynamic module. 
		/// </summary>
		/// <param name="parameters">The function's parameters (e.g. <c>"$x, $y = 1, &amp;$z"</c>).</param>
		/// <param name="body">The function's body.</param>
		/// <param name="context">A script context.</param>
		/// <param name="descriptor"></param>
		/// <returns>A name of the created function.</returns>
		/// <exception cref="ArgumentNullException">Any parameter is a <B>null</B> reference.</exception>
		public static string CreateLambdaFunction(string/*!*/ parameters, string/*!*/ body, ScriptContext/*!*/ context,
			SourceCodeDescriptor descriptor)
		{
			if (parameters == null)
				throw new ArgumentNullException("parameters");
			if (body == null)
				throw new ArgumentNullException("body");
			if (context == null)
				throw new ArgumentNullException("context");

			string name = GenerateLambdaName(parameters, body);
            if (context.DeclaredFunctions != null && context.DeclaredFunctions.ContainsKey(name))
                return name;

			string prefix1, prefix2;
			GetLamdaFunctionCodePrefixes(name, parameters, out prefix1, out prefix2);

			context.ClearCapturedSourceCodeDescriptor();
			EvalInternal(prefix2, body, "}", EvalKinds.LambdaFunction, context, null, null, null, descriptor, false, null); // TODO: naming context in lambda function??

			return name;
		}

		/// <summary>
		/// Implements PHP <c>assert</c> construct. 
		/// </summary>
		/// <param name="assertion">The condition to be checked.</param>
		/// <param name="context">The current script context.</param>
		/// <param name="definedVariables">Current scope run-time variables.</param>
		/// <param name="self">The current <see cref="PhpObject"/> in which method the eval is called. Can be a <B>null</B> reference.</param>
		/// <param name="includer">A type desc of the current type in where eval is called in its method.</param>
		/// <param name="containingSourcePath">Relative path to the source file of the calling script (used to evaluate __FILE__ and format error messages).</param>
		/// <param name="line">The line where eval is called. Used only when error is reported in debug mode.</param>
		/// <param name="column">The column where eval is called. Used only when error is reported in debug mode.</param>
		/// <param name="containerId">Id of the containing transient module.</param>
        /// <param name="namingContext"></param>
		/// <returns>Whether the <paramref name="assertion"/> doesn't evaluate to <B>false</B>.</returns>
		/// <remarks>
		/// <paramref name="assertion"/> is converted to string by <see cref="PHP.Core.Convert.ObjectToString"/>,
		/// evaluated by <see cref="Eval"/> and finally compared by == operator (see <see cref="PhpComparer.Default"/>)
		/// for equality with <B>false</B>. Actions taken before evaluation as well as if assertion fails
		/// are defined by the <see cref="Configuration.Global"/>, section "assertion".
		/// </remarks>
		[Emitted]
		public static bool Assert(
			object assertion,
			ScriptContext context,
			Dictionary<string, object> definedVariables,
			DObject self,
			DTypeDesc includer,
			string containingSourcePath,
			int line,
			int column,
			int containerId,
			NamingContext namingContext)
		{
			object result;
			string code;

			// skips asserts if not active:
			if (!context.Config.Assertion.Active) return true;

			if ((code = PhpVariable.AsString(assertion)) != null)
			{
				// disables error reporting if eval should be quite:
				if (context.Config.Assertion.Quiet) context.DisableErrorReporting();

				SourceCodeDescriptor descriptor = new SourceCodeDescriptor(containingSourcePath, containerId, line, column);

				// evaluates the expression:
				result = EvalInternal("return ", code, ";", EvalKinds.Assert, context, definedVariables, self, includer, descriptor, false, namingContext);

				// restores error reporting if eval have been quite: 
				if (context.Config.Assertion.Quiet) context.EnableErrorReporting();
			}
			else
			{
				result = assertion;
			}

			// checks the result of assertion:
			return CheckAssertion(result, code, context, containingSourcePath, line, column, namingContext);
		}

		[Emitted]
		public static bool PreAssert(ScriptContext context)
		{
			if (!context.Config.Assertion.Active) return false;

			// disables error reporting if eval should be quite:
			if (context.Config.Assertion.Quiet) context.DisableErrorReporting();

			return true;
		}

		[Emitted]
		public static void PostAssert(ScriptContext context)
		{
			// restores error reporting if eval have been quite: 
			if (context.Config.Assertion.Quiet) context.EnableErrorReporting();
		}

		/// <summary>
		/// Ckecks the value of an assertion and performs and according action.
		/// </summary>
		/// <param name="assertion">The value of assertion.</param>
		/// <param name="code">The assertion condition source code.</param>
		/// <param name="context">A script context.</param>
		/// <param name="callerRelativeSourcePath">A relative path to the source file where the assertion is stated.</param>
		/// <param name="line">The line where the assertion is stated.</param>
		/// <param name="column">The column where the assertion is stated.</param>
        /// <param name="namingContext"></param>
		/// <returns>Whether the assertion succeeded.</returns>
		[Emitted]
		public static bool CheckAssertion(
			object assertion,
			string code,
			ScriptContext context,
			string callerRelativeSourcePath,
			int line,
			int column,
			NamingContext namingContext)
		{
			// checks assertion:
			if (assertion != null && !PhpComparer./*Default.*/CompareEq(assertion, false))
				return true;

			// calls user callback:
			if (context.Config.Assertion.Callback != null)
			{
				ApplicationConfiguration app_config = Configuration.Application;
				FullPath full_path = new FullPath(callerRelativeSourcePath, app_config.Compiler.SourceRoot);
				context.Config.Assertion.Callback.Invoke(full_path, line, code);
			}

			// reports a warning if required:
			if (context.Config.Assertion.ReportWarning)
				PhpException.Throw(PhpError.Warning, CoreResources.GetString("assertion_failed", code));

			// terminates script execution if required:
			if (context.Config.Assertion.Terminate)
				throw new ScriptDiedException(0);

			// assertion failed:
			return false;
		}

		/// <summary>
		/// Implements PHP <c>eval</c> construct. 
		/// </summary>
		/// <param name="code">A code to be evaluated.</param>
		/// <param name="synthetic">Whether the eval is synthetic.</param>
		/// <param name="context">The current script context.</param>
		/// <param name="definedVariables">Currently defined variables.</param>
		/// <param name="self">The current <see cref="PhpObject"/> in which method the eval is called. Can be a <B>null</B> reference.</param>
		/// <param name="referringType">A type desc of the type whose method is calling the eval.</param>
		/// <param name="callerRelativeSourcePath">
		/// Relative path to the source file of a calling script with respect to the source root 
		/// (used to evaluate __FILE__ and format error messages).
		/// </param>
		/// <param name="line">The line where eval is called. Used only when error is reported in debug mode.</param>
		/// <param name="column">The column where eval is called. Used only when error is reported in debug mode.</param>
		/// <param name="containerId">Id of the containing transient module.</param>
        /// <param name="namingContext">Naming context of the caller.</param>
		/// <returns>The result of evaluation.</returns>
		[Emitted]
		public static object Eval(
			string code,
			bool synthetic,
			ScriptContext context,
			Dictionary<string, object> definedVariables,
			DObject self,
			DTypeDesc referringType,
			string callerRelativeSourcePath,
			int line,
			int column,
			int containerId,
			NamingContext namingContext)
		{
			EvalKinds kind = synthetic ? EvalKinds.SyntheticEval : EvalKinds.ExplicitEval;
                return EvalInternal("", code, "", kind, context, definedVariables, self, referringType,
                    new SourceCodeDescriptor(callerRelativeSourcePath, containerId, line, column), false, namingContext);
		}

#if SILVERLIGHT
		/// <summary>
		/// Evaluates an entire PHP script file
		/// </summary>
		public static object EvalFile(
			string code,
			ScriptContext context,
			Dictionary<string, object> definedVariables,
			DObject self,
			DTypeDesc referringType,
			string callerRelativeSourcePath,
			int line,
			int column,
			int containerId)
		{
			return EvalInternal("", code, "", EvalKinds.DynamicInclude, context, definedVariables, self, referringType,
				new SourceCodeDescriptor(callerRelativeSourcePath, containerId, line, column), true, null);
		}
#endif
		/// <summary>
		/// Implements PHP <c>eval</c> construct with given code prefix and suffix. 
		/// A result of concatanation prefix + code + suffix is compiled.
		/// Prefix should contain no new line characters.
		/// </summary>
		internal static object EvalInternal(
			string prefix,
			string code,
			string suffix,
			EvalKinds kind,
			ScriptContext/*!*/ scriptContext,
			Dictionary<string, object> localVariables,
			DObject self,
			DTypeDesc referringType,
			SourceCodeDescriptor descriptor,
			bool entireFile, 
			NamingContext namingContext)
		{
			Debug.Assert(prefix != null && suffix != null);

			// composes code to be compiled:
			code = String.Concat(prefix, code, suffix);

			TransientAssemblyBuilder assembly_builder = scriptContext.ApplicationContext.TransientAssemblyBuilder;

			// looks up the cache:
			TransientModule module = assembly_builder.TransientAssembly.GetModule(scriptContext, referringType, code, descriptor);

            if (module == null)
                // double checked lock,
                // if module != null, it is definitely completed
                // since module is added into TransientAssembly at the end
                // of assembly_builder.Build
                lock (assembly_builder.TransientAssembly)
                {
                    // lookup again, since it could be added into TransientAssembly while lock
                    module = assembly_builder.TransientAssembly.GetModule(scriptContext, referringType, code, descriptor);

                    if (module == null)
                    {
                        if (kind == EvalKinds.SyntheticEval)
                            Debug.WriteLine("SYN EVAL", "Eval cache missed: '{0}'", code.Substring(0, Math.Max(code.IndexOf('{'), 0)).TrimEnd());
                        else
                            Debug.WriteLine("EVAL", "Eval cache missed: '{0}'({1},{2})", descriptor.ContainingSourcePath, descriptor.Line, descriptor.Column);

                        CompilerConfiguration config = new CompilerConfiguration(Configuration.Application);

                        CompilationContext context = new CompilationContext(scriptContext.ApplicationContext, null, config,
                            new EvalErrorSink(-prefix.Length, config.Compiler.DisabledWarnings, config.Compiler.DisabledWarningNumbers),
                            scriptContext.WorkingDirectory);


                        TransientCompilationUnit unit = assembly_builder.Build(code, descriptor, kind, context,
                            scriptContext, referringType, namingContext, entireFile);

                        // compilation failed:
                        if (unit == null) return false;
                        module = unit.TransientModule;
                    }
                }
			
			// activates unconditionally declared types, functions and constants: 
            module.TransientCompilationUnit.Declare(scriptContext);

			return module.Main(scriptContext, localVariables, self, referringType, true);
		}

		/// <summary>
		/// Dumps content of transient assembly.
		/// </summary>
		[Conditional("DEBUG")]
		public static void Dump(ScriptContext/*!*/ context, TextWriter output)
		{
			context.ApplicationContext.TransientAssemblyBuilder.TransientAssembly.Dump(output);
		}
	}

	#endregion
}
