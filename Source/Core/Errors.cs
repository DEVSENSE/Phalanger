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
using System.Runtime.Serialization;
using System.Configuration;
using System.Xml;
using System.Text;
using System.Resources;
using System.Globalization;
using System.Diagnostics;
using PHP.Core.Reflection;

#if SILVERLIGHT
using PHP.CoreCLR;
#else
using System.Web; // ReportError(config, HttpContext.Current.Response.Output, error, id, info, message);
#endif

namespace PHP.Core
{
	#region Enumerations

	/// <summary>
	/// Types of errors caused by PHP class library functions.
	/// </summary>
	[Flags]
	public enum PhpError : int
	{
		/// <summary>Error.</summary>
		Error = 1,
		/// <summary>Warning.</summary>
		Warning = 2,
		/// <summary>Notice.</summary>
		Notice = 8,

		/// <summary>User error.</summary>
		UserError = 256,
		/// <summary>User warning.</summary>
		UserWarning = 512,
		/// <summary>User notice.</summary>
		UserNotice = 1024,

		/// <summary>Parse error.</summary>
		ParseError = 4,

		/// <summary>Core error.</summary>
		CoreError = 16,
		/// <summary>Core warning.</summary>
		CoreWarning = 32,

		/// <summary>Compile error.</summary>
		CompileError = 64,
		/// <summary>Compile warning.</summary>
		CompileWarning = 128,

		/// <summary>Strict notice (PHP 5.0+).</summary>
		Strict = 2048,

        /// <summary>PHP 5.2+</summary>
        RecoverableError = 4096,

        /// <summary>Deprecated (PHP 5.3+)</summary>
        Deprecated = 8192,
        UserDeprecated = 16384,
	}

	/// <summary>
	/// Sets of error types.
	/// </summary>
	[Flags]
	public enum PhpErrorSet : int
	{
		/// <summary>Empty error set.</summary>
		None = 0,

		/// <summary>Standard errors used by Core and Class Library.</summary>
		Standard = PhpError.Error | PhpError.Warning | PhpError.Notice | PhpError.Deprecated,

		/// <summary>User triggered errors.</summary>
		User = PhpError.UserError | PhpError.UserWarning | PhpError.UserNotice | PhpError.UserDeprecated,

		/// <summary>Core system errors.</summary>
        System = PhpError.ParseError | PhpError.CoreError | PhpError.CoreWarning | PhpError.CompileError | PhpError.CompileWarning | PhpError.RecoverableError,

		/// <summary>All possible errors except for the strict ones.</summary>
		AllButStrict = Standard | User | System,

        /// <summary>All possible errors. 30719 in PHP 5.3</summary>
		All = AllButStrict | PhpError.Strict,

		/// <summary>Errors which can be handled by the user defined routine.</summary>
		Handleable = (User | Standard) & ~PhpError.Error,

		/// <summary>Errors which causes termination of a running script.</summary>
		Fatal = PhpError.Error | PhpError.CompileError | PhpError.CoreError | PhpError.UserError
	}

	/// <summary>
	/// Type of action being performed when PhpException static handlers (Throw, InvalidArgument, ...) are called. 
	/// </summary>
	public enum PhpErrorAction
	{
		/// <summary>An action specified by the current configuration is taken.</summary>
		Default,
		/// <summary>An exception is thrown.</summary>
		Throw,
		/// <summary>Do nothing but setting the flag.</summary>
		None
	}

	#endregion

	/// <summary>
	/// Represents information about an error got from the stack.
	/// </summary>
	public struct ErrorStackInfo
	{
        /// <summary>
		/// The name of the source file.
		/// </summary>
		public string File;

		/// <summary>
		/// The name of the PHP function which caused an error.
		/// </summary>
		public string Caller;

		/// <summary>
		/// Whether a caller is a library function.
		/// </summary>
		public bool LibraryCaller;

		/// <summary>
		/// A number of a line in a source file where an error occured.
		/// </summary>
		public int Line;

		/// <summary>
		/// A number of a column in a source file where an error occured.
		/// </summary>
		public int Column;

        /// <summary>
        /// Initializes <see cref="ErrorStackInfo"/> by given values.
        /// </summary>
        /// <param name="file">Full path to a source file.</param>
        /// <param name="caller">Name of a calling PHP funcion.</param>
        /// <param name="line">Line in a source file.</param>
        /// <param name="column">Column in a source file.</param>
        /// <param name="libraryCaller">Whether a caller is a library function.</param>
        public ErrorStackInfo(string file, string caller, int line, int column, bool libraryCaller)
        {
            File = file;
            Caller = caller;
            Line = line;
            Column = column;
            LibraryCaller = libraryCaller;
        }
	}

	/// <summary>
	/// Represents exceptions thrown by PHP class library functions.
	/// </summary>
	[Serializable]
	[DebuggerNonUserCode]
	public class PhpException : System.Exception
	{
		#region Frequently reported errors

		/// <summary>
		/// Invalid argument error.
		/// </summary>
		/// <param name="argument">The name of the argument being invalid.</param>
		public static void InvalidArgument(string argument)
		{
			Throw(PhpError.Warning, CoreResources.GetString("invalid_argument", argument));
		}

		/// <summary>
		/// Invalid argument error with a description of a reason. 
		/// </summary>
		/// <param name="argument">The name of the argument being invalid.</param>
		/// <param name="message">The message - what is wrong with the argument. Must contain "{0}" which is replaced by argument's name.
		/// </param>
		public static void InvalidArgument(string argument, string message)
		{
			Throw(PhpError.Warning, String.Format(CoreResources.GetString("invalid_argument_with_message") + message, argument));
		}

		/// <summary>
		/// Argument null error. Thrown when argument can't be null but it is.
		/// </summary>
		/// <param name="argument">The name of the argument.</param>
		public static void ArgumentNull(string argument)
		{
			Throw(PhpError.Warning, CoreResources.GetString("argument_null", argument));
		}

		/// <summary>
		/// Reference argument null error. Thrown when argument which is passed by reference is null.
		/// </summary>
		/// <param name="argument">The name of the argument.</param>
		public static void ReferenceNull(string argument)
		{
			Throw(PhpError.Error, CoreResources.GetString("reference_null", argument));
		}

		/// <summary>
		/// Called library function is not supported.
		/// </summary>
		public static void FunctionNotSupported()
		{
			Throw(PhpError.Warning, CoreResources.GetString("function_not_supported"));
		}

        /// <summary>
		/// Called library function is not supported.
		/// </summary>
        /// <param name="function">Not supported function name.</param>
        [Emitted]
		public static void FunctionNotSupported(string/*!*/function)
		{
            Debug.Assert(!string.IsNullOrEmpty(function));

            Throw(PhpError.Warning, CoreResources.GetString("notsupported_function_called", function));
		}

        /// <summary>
		/// Calles library function is not supported.
		/// </summary>
		/// <param name="severity">A severity of the error.</param>
		public static void FunctionNotSupported(PhpError severity)
		{
			Throw(severity, CoreResources.GetString("function_not_supported"));
		}

        ///// <summary>
        ///// Called library function is deprecated.
        ///// </summary>
        //public static void FunctionDeprecated()
        //{
        //    ErrorStackInfo info = PhpStackTrace.TraceErrorFrame(ScriptContext.CurrentContext);
        //    FunctionDeprecated(info.LibraryCaller ? info.Caller : null);
        //}

        /// <summary>
        /// Called library function is deprecated.
        /// </summary>
        public static void FunctionDeprecated(string functionName)
        {
            Throw(PhpError.Deprecated, CoreResources.GetString("function_is_deprecated", functionName));
        }

        /// <summary>
		/// Calls by the Class Library methods which need variables but get a <b>null</b> reference.
		/// </summary>
		public static void NeedsVariables()
		{
			Throw(PhpError.Warning, CoreResources.GetString("function_needs_variables"));
		}

		/// <summary>
		/// The value of an argument is not invalid but unsupported.
		/// </summary>
		/// <param name="argument">The argument which value is unsupported.</param>
        /// <param name="value">The value which is unsupported.</param>
		public static void ArgumentValueNotSupported(string argument, object value)
		{
			Throw(PhpError.Warning, CoreResources.GetString("argument_value_not_supported", value, argument));
		}

		/// <summary>
		/// Throw by <see cref="PhpStack"/> when a peeked argument should be passed by reference but it is not.
		/// </summary>
		/// <param name="index">An index of the argument.</param>
		/// <param name="calleeName">A name of the function or method being called. Can be a <B>null</B> reference.</param>
		public static void ArgumentNotPassedByRef(int index, string calleeName)
		{
			if (calleeName != null)
				Throw(PhpError.Error, CoreResources.GetString("argument_not_passed_byref_to", index, calleeName));
			else
				Throw(PhpError.Error, CoreResources.GetString("argument_not_passed_byref", index));
		}

		/// <summary>
		/// Emitted to a user function/method call which has less actual arguments than it's expected to have.
		/// </summary>
		/// <param name="index">An index of the parameter.</param>
		/// <param name="calleeName">A name of the function or method being called. Can be a <B>null</B> reference.</param>
		[Emitted]
		public static void MissingArgument(int index, string calleeName)
		{
			if (calleeName != null)
				Throw(PhpError.Warning, CoreResources.GetString("missing_argument_for", index, calleeName));
			else
				Throw(PhpError.Warning, CoreResources.GetString("missing_argument", index));
		}

		/// <summary>
		/// Emitted to a user function/method call which has less actual type arguments than it's expected to have.
		/// </summary>
		/// <param name="index">An index of the type parameter.</param>
		/// <param name="calleeName">A name of the function or method being called. Can be a <B>null</B> reference.</param>
		[Emitted]
		public static void MissingTypeArgument(int index, string calleeName)
		{
			if (calleeName != null)
				Throw(PhpError.Warning, CoreResources.GetString("missing_type_argument_for", index, calleeName));
			else
				Throw(PhpError.Warning, CoreResources.GetString("missing_type_argument", index));
		}

		[Emitted]
		public static void MissingArguments(string typeName, string methodName, int actual, int required)
		{
			if (typeName != null)
			{
				if (methodName != null)
					Throw(PhpError.Warning, CoreResources.GetString("too_few_method_params", typeName, methodName, required, actual));
				else
					Throw(PhpError.Warning, CoreResources.GetString("too_few_ctor_params", typeName, required, actual));
			}
			else
				Throw(PhpError.Warning, CoreResources.GetString("too_few_function_params", methodName, required, actual));
		}

        public static void UnsupportedOperandTypes()
        {
            PhpException.Throw(PhpError.Error, CoreResources.GetString("unsupported_operand_types"));
        }

		/// <summary>
		/// Emitted to a library function call which has invalid actual argument count.
		/// </summary>
		[Emitted]
		public static void InvalidArgumentCount(string typeName, string methodName)
		{
			if (methodName != null)
			{
				if (typeName != null)
					Throw(PhpError.Warning, CoreResources.GetString("invalid_argument_count_for_method", typeName, methodName));
				else
					Throw(PhpError.Warning, CoreResources.GetString("invalid_argument_count_for_function", methodName));
			}
			else
				Throw(PhpError.Warning, CoreResources.GetString("invalid_argument_count"));
		}

		/// <summary>
		/// Emitted to the foreach statement if the variable to be enumerated doesn't implement 
		/// the <see cref="IPhpEnumerable"/> interface.
		/// </summary>
		[Emitted]
		public static void InvalidForeachArgument()
		{
			Throw(PhpError.Warning, CoreResources.GetString("invalid_foreach_argument"));
		}

		/// <summary>
		/// Emitted to the function call if an argument cannot be implicitly casted.
		/// </summary>
		/// <param name="argument">The argument which is casted.</param>
		/// <param name="targetType">The type to which is casted.</param>
		/// <param name="functionName">The name of the function called.</param>
		[Emitted]
		public static void InvalidImplicitCast(object argument, string targetType, string functionName)
		{
			Throw(PhpError.Warning, CoreResources.GetString("invalid_implicit_cast",
			  PhpVariable.GetTypeName(argument),
			  targetType,
			  functionName));
		}

		/// <summary>
		/// Emitted to the code on the places where invalid number of breaking levels is used.
		/// </summary>
		/// <param name="levelCount">The number of levels.</param>
		[Emitted]
		public static void InvalidBreakLevelCount(int levelCount)
		{
			Throw(PhpError.Error, CoreResources.GetString("invalid_break_level_count", levelCount));
		}

		/// <summary>
		/// Reported by operators when they found that a undefined variable is acceesed.
		/// </summary>
		/// <param name="name">The name of the variable.</param>
		[Emitted]
		public static void UndefinedVariable(string name)
		{
			Throw(PhpError.Notice, CoreResources.GetString("undefined_variable", name));
		}

		/// <summary>
		/// Emitted instead of the assignment of to the "$this" variable.
		/// </summary>
		[Emitted]
		public static void CannotReassignThis()
		{
			Throw(PhpError.Error, CoreResources.GetString("cannot_reassign_this"));
		}

		/// <summary>
		/// An argument violates a type hint.
		/// </summary>
		/// <param name="argName">The name of the argument.</param>
		/// <param name="typeName">The name of the hinted type.</param>
		[Emitted]
		public static void InvalidArgumentType(string argName, string typeName)
		{
			Throw(PhpError.Error, CoreResources.GetString("invalid_argument_type", argName, typeName));
		}

		/// <summary>
		/// Array operators reports this error if an value of illegal type is used for indexation.
		/// </summary>
		public static void IllegalOffsetType()
		{
			Throw(PhpError.Warning, CoreResources.GetString("illegal_offset_type"));
		}

        /// <summary>
        /// Array does not contain given <paramref name="key"/>.
        /// </summary>
        /// <param name="key">Key which was not found in the array.</param>
        public static void UndefinedOffset(object key)
        {
            Throw(PhpError.Notice, CoreResources.GetString("undefined_offset", key));
        }

		/// <summary>
		/// Emitted to the script's Main() routine. Thrown when an unexpected exception is catched.
		/// </summary>
		/// <param name="e">The catched exception.</param>
		public static void InternalError(Exception e)
		{
			throw new PhpNetInternalException(e.Message, e);
		}

		/// <summary>
		/// Reports an error when a variable should be PHP array but it is not.
		/// </summary>
		/// <param name="reference">Whether a reference modifier (=&amp;) is used.</param>
		/// <param name="var">The variable which was misused.</param>
		/// <exception cref="PhpException"><paramref name="var"/> is <see cref="PhpArray"/> (Warning).</exception>
		/// <exception cref="PhpException"><paramref name="var"/> is scalar type (Warning).</exception>
		/// <exception cref="PhpException"><paramref name="var"/> is a string (Warning).</exception>
		public static void VariableMisusedAsArray(object var, bool reference)
		{
			Debug.Assert(var != null);

			DObject obj;

			if ((obj = var as DObject) != null)
			{
				PhpException.Throw(PhpError.Warning, CoreResources.GetString("object_used_as_array", obj.TypeName));
			}
			else if (PhpVariable.IsString(var))
			{
				PhpException.Throw(PhpError.Warning, CoreResources.GetString(reference ? "string_item_used_as_reference" : "string_used_as_array"));
			}
			else
			{
				PhpException.Throw(PhpError.Warning, CoreResources.GetString("scalar_used_as_array", PhpVariable.GetTypeName(var)));
			}
		}

		/// <summary>
		/// Reports an error when a variable should be PHP object but it is not.
		/// </summary>
		/// <param name="reference">Whether a reference modifier (=&amp;) is used.</param>
		/// <param name="var">The variable which was misused.</param>
		/// <exception cref="PhpException"><paramref name="var"/> is <see cref="PhpArray"/> (Warning).</exception>
		/// <exception cref="PhpException"><paramref name="var"/> is scalar type (Warning).</exception>
		/// <exception cref="PhpException"><paramref name="var"/> is a string (Warning).</exception>
		public static void VariableMisusedAsObject(object var, bool reference)
		{
			Debug.Assert(var != null);

			if (var is PhpArray)
			{
				PhpException.Throw(PhpError.Warning, CoreResources.GetString("array_used_as_object"));
			}
			else if (PhpVariable.IsString(var))
			{
				PhpException.Throw(PhpError.Warning, CoreResources.GetString(reference ? "string_item_used_as_reference" : "string_used_as_object"));
			}
			else
			{
				PhpException.Throw(PhpError.Warning, CoreResources.GetString("scalar_used_as_object", PhpVariable.GetTypeName(var)));
			}
		}

		/// <summary>
		/// Thrown when "this" special variable is used out of class.
		/// </summary>
		[Emitted]
		public static void ThisUsedOutOfObjectContext()
		{
			PhpException.Throw(PhpError.Error, CoreResources.GetString("this_used_out_of_object"));
		}

		public static void UndeclaredStaticProperty(string className, string fieldName)
		{
			PhpException.Throw(PhpError.Error, CoreResources.GetString("undeclared_static_property_accessed", className, fieldName));
		}

		[Emitted]
		public static void StaticPropertyUnset(string className, string fieldName)
		{
			PhpException.Throw(PhpError.Error, CoreResources.GetString("static_property_unset", className, fieldName));
		}

        [Emitted]
		public static void UndefinedMethodCalled(string className, string methodName)
		{
			PhpException.Throw(PhpError.Error, CoreResources.GetString("undefined_method_called", className, methodName));
		}

		public static void AbstractMethodCalled(string className, string methodName)
		{
			PhpException.Throw(PhpError.Error, CoreResources.GetString("abstract_method_called", className, methodName));
		}

		public static void ConstantNotAccessible(string className, string constName, string context, bool isProtected)
		{
			PhpException.Throw(PhpError.Error, CoreResources.GetString(
					  isProtected ? "protected_constant_accessed" : "private_constant_accessed", className, constName, context));
		}

		public static void PropertyNotAccessible(string className, string fieldName, string context, bool isProtected)
		{
			PhpException.Throw(PhpError.Error, CoreResources.GetString(
					  isProtected ? "protected_property_accessed" : "private_property_accessed", className, fieldName, context));
		}

		public static void MethodNotAccessible(string className, string methodName, string context, bool isProtected)
		{
			PhpException.Throw(PhpError.Error, CoreResources.GetString(
					  isProtected ? "protected_method_called" : "private_method_called", className, methodName, context));
		}

		public static void CannotInstantiateType(string typeName, bool isInterface)
		{
			PhpException.Throw(PhpError.Error, CoreResources.GetString(
			  isInterface ? "interface_instantiated" : "abstract_class_instantiated", typeName));
		}

		[Emitted]
		public static void NoSuitableOverload(string className, string/*!*/ methodName)
		{
			PhpException.Throw(PhpError.Error, CoreResources.GetString(
					  (className != null) ? "no_suitable_method_overload" : "no_suitable_function_overload",
					  className, methodName));
		}

		[Emitted]
		public static void PropertyTypeMismatch(string/*!*/ className, string/*!*/ propertyName)
		{
			PhpException.Throw(PhpError.Error, CoreResources.GetString("property_type_mismatch",
				className, propertyName));
		}

		#endregion

        #region Error handling stuff

        /// <summary>
        /// Delegate used to catch any thrown PHP exception. Used in compile time to catch PHP runtime exceptions.
        /// </summary>
        [ThreadStatic]
        internal static Action<PhpError, string> ThrowCallbackOverride = null;

        /// <summary>
		/// Reports a PHP error. 
		/// </summary>
		/// <param name="error">The error type</param>
		/// <param name="message">The error message.</param>    
		public static void Throw(PhpError error, string message)
		{
            if (ThrowCallbackOverride != null)
            {
                ThrowCallbackOverride(error, message);
                return;
            }

			ErrorStackInfo info = new ErrorStackInfo();
			bool info_loaded = false;

			// gets the current script context and config:
			ScriptContext context = ScriptContext.CurrentContext;
			LocalConfiguration config = context.Config;

			// determines whether the error will be reported and whether it is handleable:
			bool is_error_reported = ((PhpErrorSet)error & config.ErrorControl.ReportErrors) != 0 && !context.ErrorReportingDisabled;
			bool is_error_handleable = ((PhpErrorSet)error & PhpErrorSet.Handleable & (PhpErrorSet)config.ErrorControl.UserHandlerErrors) != 0;
			bool is_error_fatal = ((PhpErrorSet)error & PhpErrorSet.Fatal) != 0;
			bool do_report = true;

            // remember last error info
            context.LastErrorType = error;
            context.LastErrorMessage = message;
            context.LastErrorFile = null;   // only if we are getting ErrorStackInfo, see PhpStackTrace.TraceErrorFrame
            context.LastErrorLine = 0;     // only if we are getting ErrorStackInfo, see PhpStackTrace.TraceErrorFrame

			// calls a user defined handler if available:
			if (is_error_handleable && config.ErrorControl.UserHandler != null)
			{
				// loads stack info:
				if (!info_loaded) { info = PhpStackTrace.TraceErrorFrame(context); info_loaded = true; }

				do_report = CallUserErrorHandler(context, error, info, message);
			}

			// reports error to output and logs:
            if (do_report && is_error_reported &&
                (config.ErrorControl.DisplayErrors || config.ErrorControl.EnableLogging))   // check if the error will be displayed to avoid stack trace loading
			{
				// loads stack info:
				if (!info_loaded) { info = PhpStackTrace.TraceErrorFrame(context); info_loaded = true; }

				ReportError(config, context.Output, error, -1, info, message);
			}

			// Throws an exception if the error is fatal and throwing is enabled.
			// PhpError.UserError is also fatal, but can be cancelled by user handler => handler call must precede this line.
			// Error displaying must also precede this line because the error should be displayed before an exception is thrown.
			if (is_error_fatal && context.ThrowExceptionOnError)
			{
				// loads stack info:
				if (!info_loaded) { info = PhpStackTrace.TraceErrorFrame(context); info_loaded = true; }

				throw new PhpException(error, message, info);
			}
		}

		/// <summary>
		/// Reports an error to log file, event log and to output (as configured).
		/// </summary>
		private static void ReportError(LocalConfiguration config, TextWriter output, PhpError error, int id,
			ErrorStackInfo info, string message)
		{
            string formatted_message = FormatErrorMessageOutput(config, error, id, info, message);

			// logs error if logging is enabled:
			if (config.ErrorControl.EnableLogging)
			{
#if SILVERLIGHT
				throw new NotSupportedException("Logging is not supported on Silverlight. Set EnableLogging to false.");
#else
				// adds a message to log file:
				if (config.ErrorControl.LogFile != null)
                    try
                    {
                        // <error>: <caller>(): <message> in <file> on line <line>
                        string caller = (info.Caller != null) ? (info.Caller + "(): ") : null;
                        string place = (info.Line > 0 && info.Column > 0) ? CoreResources.GetString("error_place", info.File, info.Line, info.Column) : null;

                        Logger.AppendLine(config.ErrorControl.LogFile, string.Concat(error, ": ", caller, message, place));
                    }
                    catch (Exception) { }

				// adds a message to event log:
				if (config.ErrorControl.SysLog)
					try { Logger.AddToEventLog(message); }
					catch (Exception) { }
#endif
			}

			// displays an error message if desired:
			if (config.ErrorControl.DisplayErrors)
			{
				output.Write(config.ErrorControl.ErrorPrependString);
				output.Write(formatted_message);
				output.Write(config.ErrorControl.ErrorAppendString);
			}
		}

		/// <summary>
		/// Calls user error handler. 
		/// </summary>
		/// <returns>Whether to report error by default handler (determined by handler's return value).</returns>
		/// <exception cref="ScriptDiedException">Error handler dies.</exception>
		private static bool CallUserErrorHandler(ScriptContext context, PhpError error, ErrorStackInfo info, string message)
		{
			LocalConfiguration config = context.Config;

			try
			{
				object result = PhpVariable.Dereference(config.ErrorControl.UserHandler.Invoke(new PhpReference[] 
        { 
          new PhpReference((int)error),
          new PhpReference(message),
          new PhpReference(info.File),
          new PhpReference(info.Line),
          new PhpReference() // global variables list is not supported
        }));

				// since PHP5 an error is reported by default error handler if user handler returns false:
				return result is bool && (bool)result == false;
			}
			catch (ScriptDiedException)
			{
				// user handler has cancelled the error via script termination:
				throw;
			}
			catch (PhpUserException)
			{
				// rethrow user exceptions:
				throw;
			}
			catch (Exception)
			{
			}
			return false;
		}

		/// <summary>
		/// Reports error thrown from inside eval.
		/// </summary>
		internal static void ThrowByEval(PhpError error, string sourceFile, int line, int column, string message)
		{
			// obsolete:
			//      ErrorStackInfo info = new ErrorStackInfo(sourceFile,null,line,column,false);
			//      
			//      if (ScriptContext.CurrentContext.Config.ErrorControl.HtmlMessages)
			//        message = CoreResources.GetString("error_message_html_eval",message,info.Line,info.Column); else
			//        message = CoreResources.GetString("error_message_plain_eval",message,info.Line,info.Column);

			Throw(error, message);
		}

		/// <summary>
		/// Reports error thrown by compiler.
		/// </summary>
		internal static void ThrowByWebCompiler(PhpError error, int id, string sourceFile, int line, int column, string message)
		{
			ErrorStackInfo info = new ErrorStackInfo(sourceFile, null, line, column, false);

			// gets the current script context and config:
			LocalConfiguration config = Configuration.Local;

#if !SILVERLIGHT
			ReportError(config, HttpContext.Current.Response.Output, error, id, info, message);
#else
			ReportError(config, new StreamWriter(ScriptContext.CurrentContext.OutputStream), error, id, info, message);
#endif

			if (((PhpErrorSet)error & PhpErrorSet.Fatal) != 0)
				throw new PhpException(error, message, info);
		}

        /// <summary>
        /// Get the error type text, to be displayed on output.
        /// </summary>
        /// <param name="error"></param>
        /// <param name="id"></param>
        /// <returns>Error text.</returns>
        internal static string PhpErrorText(PhpError error, int id)
        {
            if (id > 0)
            {
                return String.Format("{0} ({1})", error, id);
            }
            else
            {
                switch (error)
                {
                    // errors with spaces in the name
                    case PhpError.Strict:
                        return "Strict Standards";

                    // user errors reported as normal errors (without "User")
                    case PhpError.UserNotice:
                        return PhpError.Notice.ToString();
                    case PhpError.UserError:
                        return PhpError.Error.ToString();
                    case PhpError.UserWarning:
                        return PhpError.Warning.ToString();

                    // error string as it is
                    default:
                        return error.ToString(); ;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="info"></param>
        /// <param name="config"></param>
        /// <returns>Returns caller name with () or null. Formatted for the current output capabilities.</returns>
        internal static string FormatErrorCallerName(ErrorStackInfo info, LocalConfiguration config)
        {
            if (info.Caller == null)
                return null;

            if (config.ErrorControl.HtmlMessages && config.ErrorControl.DocRefRoot != null && info.LibraryCaller)
            {   // able to display HTML
                return String.Format("<a href='{0}/function.{1}{2}'>{3}()</a>",
                      config.ErrorControl.DocRefRoot,
                      info.Caller.Replace('_', '-').ToLower(),
                      config.ErrorControl.DocRefExtension,
                      info.Caller);
            }
            else
            {
                return info.Caller + "()";
            }
        }

        /// <summary>
        /// Modifies the error message and caller display text, depends on error type.
        /// In case of different PHP behavior.
        /// </summary>
        /// <param name="error">error type.</param>
        /// <param name="message">Error message, in default without any change.</param>
        /// <param name="caller">Caller text, in default will be modified to "foo(): ".</param>
        internal static void FormatErrorMessageText(PhpError error, ref string message, ref string caller)
        {
            switch (error)
            {
                case PhpError.Deprecated:
                case PhpError.UserNotice:
                    caller = null;  // the caller is not displayed in PHP
                    return;
                default:
                    if (caller != null)
                        caller += ": ";
                    return;
            }
        }

        /// <summary>
		/// Formats error message.
		/// </summary>
		/// <param name="config">A configuration.</param>
		/// <param name="error">A type of the error.</param>
		/// <param name="id">Error id or -1.</param>
		/// <param name="info">A stack information about the error.</param>
		/// <param name="message">A message.</param>
		/// <returns>A formatted plain text or HTML message depending on settings in <paramref name="config"/>.</returns>
		/// <exception cref="ArgumentNullException"><paramren name="config"/> is a <B>null</B> reference.</exception>
		public static string FormatErrorMessageOutput(LocalConfiguration config, PhpError error, int id, ErrorStackInfo info, string message)
		{
			if (config == null)
				throw new ArgumentNullException("config");

            string error_str = PhpErrorText(error, id); // the error type (Warning, Error, ...)
            bool show_place = info.Line > 0 && info.Column > 0; // we are able to report error position
            string caller = FormatErrorCallerName(info, config);    // current function name "foo()" or null

            // change the message or caller, based on the error type
            FormatErrorMessageText(error, ref message, ref caller);

            // error message
            string ErrorFormatString =
                config.ErrorControl.HtmlMessages ?
                (show_place ? CoreResources.error_message_html_debug : CoreResources.error_message_html) :
                (show_place ? CoreResources.error_message_plain_debug : CoreResources.error_message_plain);

			if (show_place)
                return string.Format(ErrorFormatString,
					error_str, caller, message, info.File, info.Line, info.Column);
			else
                return string.Format(ErrorFormatString,
                    error_str, caller, message);
		}

		/// <summary>
		/// Converts exception message (ending by dot) to error message (not ending by a dot).
		/// </summary>
		/// <param name="exceptionMessage">The exception message.</param>
		/// <returns>The error message.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="exceptionMessage"/> is a <B>null</B> reference.</exception>
		public static string ToErrorMessage(string exceptionMessage)
		{
			if (exceptionMessage == null) throw new ArgumentNullException("exceptionMessage");
			return exceptionMessage.TrimEnd(new char[] { '.' });
		}

		#endregion

		#region Exception handling stuff

		/// <summary>
		/// Exception constructor.
		/// </summary>
		internal PhpException()
		{
		}

		/// <summary>
		/// Exception constructor.
		/// </summary>
		/// <param name="error">The type of PHP error.</param>
		/// <param name="message">The error message.</param>
		/// <param name="info">Information about an error gained from a stack.</param>
		private PhpException(PhpError error, string message, ErrorStackInfo info)
			: base(message)
		{
			this.info = info;
			this.error = error;
		}

		/// <summary>
		/// Error seriousness.
		/// </summary>
		public PhpError Error { get { return error; } }
		private PhpError error;

		/// <summary>
		/// Error debug info (caller, source file, line and column).
		/// </summary>
		public ErrorStackInfo DebugInfo { get { return info; } }
		private ErrorStackInfo info;

		/// <summary>
		/// Converts the exception to a string message.
		/// </summary>
		/// <returns>The formatted message.</returns>
		public override string ToString()
		{
			return FormatErrorMessageOutput(ScriptContext.CurrentContext.Config, error, -1, info, Message);
		}

		#endregion

		#region Serialization (CLR only)
#if !SILVERLIGHT

		/// <summary>
		/// Initializes a new instance of the PhpException class with serialized data. This constructor is used
		/// when an exception is thrown in a remotely called method. Such an exceptions needs to be serialized,
		/// transferred back to the caller and then rethrown using this constructor.
		/// </summary>
		/// <param name="info">The SerializationInfo that holds the serialized object data about the exception 
		/// being thrown.</param>
		/// <param name="context">The StreamingContext that contains contextual information about the source or 
		/// destination.</param>
		protected PhpException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
			this.error = (PhpError)info.GetValue("error", typeof(PhpError));
			this.info = new ErrorStackInfo(
			  (string)info.GetString("file"),
              (string)info.GetString("caller"),
              (int)info.GetInt32("line"),
			  (int)info.GetInt32("column"),
			  (bool)info.GetBoolean("libraryCaller"));
		}


		/// <summary>
		/// Sets the SerializationInfo with information about the exception. This method is called when a skeleton
		/// catches PhpException thrown in a remotely called method.
		/// </summary>
		/// <param name="info">The SerializationInfo that holds the serialized object data.</param>
		/// <param name="context">The StreamingContext that contains contextual information about the source or 
		/// destination.</param>
        [System.Security.SecurityCritical]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData(info, context);
			info.AddValue("error", error);
			info.AddValue("caller", this.info.Caller);
			info.AddValue("file", this.info.File);
			info.AddValue("line", this.info.Line);
			info.AddValue("column", this.info.Column);
		}
#endif

		#endregion
	}

	// TODO:
	internal sealed class EvalErrorSink : ErrorSink
	{
		private readonly int firstLineColumnDisplacement;

		public EvalErrorSink(int firstLineColumnDisplacement, WarningGroups disabledGroups, int[]/*!*/ disabledWarnings)
			: base(disabledGroups, disabledWarnings)
		{
			this.firstLineColumnDisplacement = firstLineColumnDisplacement;
		}

		protected override bool Add(int id, string message, ErrorSeverity severity, int group, string/*!*/ fullPath,
			ErrorPosition pos)
		{
			Debug.Assert(fullPath != null);

			// first line column adjustment:
			if (pos.FirstLine == 1) pos.FirstColumn += firstLineColumnDisplacement;

			Debug.WriteLine("!!!3", message);
			
			PhpException.ThrowByEval(severity.ToPhpCompileError(), fullPath, pos.FirstLine, pos.FirstColumn, message);

			return true;
		}
	}

	internal sealed class WebErrorSink : ErrorSink
	{
		public WebErrorSink(WarningGroups disabledGroups, int[]/*!*/ disabledWarnings)
			: base(disabledGroups, disabledWarnings)
		{

		}
		
		protected override bool Add(int id, string message, ErrorSeverity severity, int group, string/*!*/ fullPath,
			ErrorPosition pos)
		{
			Debug.Assert(fullPath != null);
			PhpException.ThrowByWebCompiler(severity.ToPhpCompileError(), id, fullPath, pos.FirstLine, pos.FirstColumn, message);
			return true;
		}
	}

	/// <summary>
	/// Thrown when data are not found found in call context or are not valid.
	/// </summary>
	[Serializable]
	public class InvalidCallContextDataException : ApplicationException
	{
		internal InvalidCallContextDataException(string slot)
			: base(CoreResources.GetString("invalid_call_context_data", slot)) { }
	}

	/// <summary>
	/// Thrown by exit/die language constructs to cause immediate termination of a script being executed.
	/// </summary>
	[Serializable]
	public class ScriptDiedException : ApplicationException
	{
		internal ScriptDiedException(object status)
		{
			this.status = status;
		}

		internal ScriptDiedException() : this(255) { }

		public object Status { get { return status; } set { status = value; } }
		private object status;

        #region Serializable

#if !SILVERLIGHT

        public ScriptDiedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            if (info != null)
            {
                this.Status = info.GetValue("Status", typeof(object));
            }

        }

        [System.Security.SecurityCritical]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);

            if (info != null)
            {
                info.AddValue("Status", Status);
            }
        }
#endif

        #endregion

    }

	/// <summary>
	/// Thrown when user attempts to create two types with same name in one assembly.
	/// </summary>
	internal class DuplicateTypeNames : ApplicationException
	{
		public DuplicateTypeNames(string name)
		{
			this.name = name;
		}

		public readonly string name;
	}

	/// <summary>
	/// Thrown when an unexpected exception is thrown during a script execution.
	/// </summary>
	[Serializable]
	public class PhpNetInternalException : ApplicationException
	{
		internal PhpNetInternalException(string message, Exception inner) : base(message, inner) { }
#if !SILVERLIGHT
		protected PhpNetInternalException(SerializationInfo info, StreamingContext context) : base(info, context) { }
#endif

        /// <summary>
        /// Exception details. Contains also details of <see cref="Exception.InnerException"/> to pass this into event logs.
        /// </summary>
        public override string Message
        {
            get
            {
                StringBuilder result = new StringBuilder(base.Message);

                //for (var ex = this.InnerException; ex != null; ex = ex.InnerException)
                var ex = this.InnerException;
                if (ex != null)
                {
                    result.AppendLine();
                    result.AppendFormat("InnerException: {0}\nat {1}\n", ex.Message, ex.StackTrace);
                }

                return result.ToString();
            }
        }
	}

	/// <summary>
	/// Holder for an instance of <see cref="Library.SPL.Exception"/>.
	/// For internal purposes only.
	/// </summary>
	public class PhpUserException : ApplicationException
	{
		public readonly Library.SPL.Exception UserException;

		public PhpUserException(Library.SPL.Exception inner) : base(Convert.ObjectToString(inner.getMessage(ScriptContext.CurrentContext)))
		{
			UserException = inner;
		}
	}

	/// <summary>
	/// An implementation of a method doesn't behave correctly.
	/// </summary>
	public class InvalidMethodImplementationException : ApplicationException
	{
		public InvalidMethodImplementationException(string methodName)
			: base(CoreResources.GetString("invalid_method_implementation", methodName))
		{ }
	}

} 
