/*

 Copyright (c) 2004-2006 Tomas Matousek.  

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/
using System;
using System.Reflection;
using System.Collections;
using System.Text;
using System.ComponentModel;
using PHP.Core;

#if SILVERLIGHT
using PHP.CoreCLR;
#endif

namespace PHP.Library
{
	/// <summary>
	/// Implementation of PHP error control functions.
	/// </summary>
	/// <threadsafety static="true"/>
    [ImplementsExtension(LibraryDescriptor.ExtCore)]
	public static class Errors
	{
		#region Enumerations

		/// <summary>
		/// Set of error types.
		/// </summary>
		[Flags, EditorBrowsable(EditorBrowsableState.Never)]
		public enum _PhpErrorSet
		{
			/// <summary>Error.</summary>
			[ImplementsConstant("E_ERROR")]
			E_ERROR = PhpError.Error,
			/// <summary>Warning.</summary>
			[ImplementsConstant("E_WARNING")]
			E_WARNING = PhpError.Warning,
			/// <summary>Parse error.</summary>
			[ImplementsConstant("E_PARSE")]
			E_PARSE = PhpError.ParseError,
			/// <summary>Notice.</summary>
			[ImplementsConstant("E_NOTICE")]
			E_NOTICE = PhpError.Notice,
			/// <summary>Core error.</summary>
			[ImplementsConstant("E_CORE_ERROR")]
			E_CORE_ERROR = PhpError.CoreError,
			/// <summary>Core warning.</summary>
			[ImplementsConstant("E_CORE_WARNING")]
			E_CORE_WARNING = PhpError.CoreWarning,
			/// <summary>Compile error.</summary>
			[ImplementsConstant("E_COMPILE_ERROR")]
			E_COMPILE_ERROR = PhpError.CompileError,
			/// <summary>Compile warning.</summary>
			[ImplementsConstant("E_COMPILE_WARNING")]
			E_COMPILE_WARNING = PhpError.CompileWarning,
			/// <summary>User error.</summary>
			[ImplementsConstant("E_USER_ERROR")]
			E_USER_ERROR = PhpError.UserError,
			/// <summary>User warning.</summary>
			[ImplementsConstant("E_USER_WARNING")]
			E_USER_WARNING = PhpError.UserWarning,
			/// <summary>User notice.</summary>
			[ImplementsConstant("E_USER_NOTICE")]
			E_USER_NOTICE = PhpError.UserNotice,
			/// <summary>All errors but strict.</summary>
			[ImplementsConstant("E_ALL")]
			E_ALL = PhpErrorSet.AllButStrict,
			/// <summary>Strict error.</summary>
			[ImplementsConstant("E_STRICT")]
			E_STRICT = PhpError.Strict,
            /// <summary>E_RECOVERABLE_ERROR error.</summary>
            [ImplementsConstant("E_RECOVERABLE_ERROR")]
            E_RECOVERABLE_ERROR = PhpError.RecoverableError,
			/// <summary>Deprecated error.</summary>
			[ImplementsConstant("E_DEPRECATED")]
			E_DEPRECATED = PhpError.Deprecated,
            /// <summary>Deprecated error.</summary>
            [ImplementsConstant("E_USER_DEPRECATED ")]
            E_USER_DEPRECATED = PhpError.UserDeprecated,
            
		}

		/// <summary>
		/// An action performed by the <see cref="Log"/> method.
		/// </summary>
		public enum LogAction
		{
			/// <summary>
			/// A message to be logged is appended to log file or sent to system log depending on the 
			/// current value of <see cref="LocalConfiguration.ErrorControl"/>.
			/// </summary>
			Default,

			/// <summary>
			/// A message is sent by an e-mail.
			/// </summary>
			SendByEmail,

			/// <summary>
			/// Not supported.
			/// </summary>
			ToDebuggingConnection,

			/// <summary>
			/// A message is appended to a specified file.
			/// </summary>
			AppendToFile
		}

		#endregion

		#region error_log (CLR only)

#if !SILVERLIGHT
		/// <summary>
		/// Logs a message to a log file or the system event log.
		/// </summary>
		/// <param name="message">The message to be logged.</param>
		/// <returns>Whether the message has been logged successfully.</returns>
		[ImplementsFunction("error_log")]
		public static bool Log(string message)
		{
			return Log(message, 0);
		}

		/// <summary>
		/// Performs specific <see cref="LogAction"/> with a given message and default options.
		/// </summary>
		/// <param name="message">The message to be logged.</param>
		/// <param name="action">The <see cref="LogAction"/> to be performed.</param>
		/// <returns>Whether the message has been logged successfully.</returns>
		[ImplementsFunction("error_log")]
		public static bool Log(string message, LogAction action)
		{
			return Log(message, action, null, null);
		}

		/// <summary>
		/// Performs specific <see cref="LogAction"/> with a given message and name of the log file.
		/// </summary>
		/// <param name="message">The message to be logged.</param>
		/// <param name="destination">The name of the log file.</param>
		/// <param name="action">The <see cref="LogAction"/> to be performed.</param>
		/// <returns>Whether the message has been logged successfully.</returns>
		[ImplementsFunction("error_log")]
		public static bool Log(string message, LogAction action, string destination)
		{
			return Log(message, action, destination, null);
		}

		/// <summary>
		/// Performs specific <see cref="LogAction"/> with a given message, name of the log file and additional headers.
		/// </summary>
		/// <param name="message">The message to be logged.</param>
		/// <param name="destination">The name of the log file.</param>
		/// <param name="extraHeaders">Additional headers which are sent along the e-mail.</param>
		/// <param name="action">The <see cref="LogAction"/> to be performed.</param>
		/// <returns>Whether the message has been logged successfully.</returns>
		[ImplementsFunction("error_log")]
		public static bool Log(string message, LogAction action, string destination, string extraHeaders)
		{
			switch (action)
			{
				case LogAction.Default:

					bool result = true;

					LocalConfiguration config = Configuration.Local;

					// adds a message to the default log file:
					if (config.ErrorControl.LogFile != null)
						try { Logger.AppendLine(config.ErrorControl.LogFile, message); }
						catch (System.Exception) { result = false; }

					// adds a message to an event log:
					if (config.ErrorControl.SysLog)
						try { Logger.AddToEventLog(message); }
						catch (System.Exception) { result = false; }

					return result;

				case LogAction.SendByEmail:
					Mailer.Mail(destination, LibResources.GetString("error_report"), message, extraHeaders);
					return true;

				case LogAction.ToDebuggingConnection:
					PhpException.ArgumentValueNotSupported("action", (int)action);
					return false;

				case LogAction.AppendToFile:
					try
					{
						PHP.Core.Logger.AppendLine(destination, message);
					}
					catch (System.Exception)
					{
						return false;
					}
					return true;

				default:
					PhpException.InvalidArgument("action");
					return false;
			}
		}
#endif

		#endregion

        #region error_get_last

        [ImplementsFunction("error_get_last")]
        public static PhpArray GetLastError(ScriptContext/*!*/context)
        {
            Debug.Assert(context != null);

            if (context.LastErrorType != 0)
            {
                PhpArray result = new PhpArray(0, 5);
                result.Add("type", (int)context.LastErrorType);
                result.Add("message", context.LastErrorMessage);
                result.Add("file", context.LastErrorFile);
                result.Add("line", context.LastErrorLine);
                //result.Add("column", context.LastErrorColumn);
                return result;
            }

            return null;
        }

        #endregion

        #region trigger_error, user_error

        /// <summary>
		/// Triggers user notice with a specified message.
		/// </summary>
		/// <param name="message">The message.</param>
		[ImplementsFunction("trigger_error")]
		public static bool TriggerError(string message)
		{
			PhpException.Throw(PhpError.UserNotice, message);
            return true;
		}


        /// <summary>
		/// Triggers user error of an arbitrary type and specified message.
		/// </summary>
		/// <param name="message">The message.</param>
		/// <param name="error">The type of an error. The type should be one of the user error types.</param>
		/// <exception cref="PhpException">User specified error.</exception>
		/// <exception cref="PhpException">The <paramref name="error"/> argument has an invalid value.</exception>
		[ImplementsFunction("trigger_error")]
		public static bool TriggerError(string message, PhpError error)
		{
            if (((PhpErrorSet)error & PhpErrorSet.User) == 0)
                return false;//    PhpException.InvalidArgument("error");

            PhpException.Throw(error, message);

            return true;
		}


		/// <summary>
        /// Alias of trigger_error().
		/// </summary>
		/// <param name="message">The message.</param>
		[ImplementsFunction("user_error")]
		public static bool UserError(string message)
		{
			PhpException.Throw(PhpError.UserNotice, message);
            return true;
		}

		/// <summary>
        /// Alias of trigger_error().
		/// </summary>
		/// <param name="message">The message.</param>
		/// <param name="error">The type of an error. The type should be one of the user error types.</param>
		/// <exception cref="PhpException">User specified error.</exception>
		/// <exception cref="PhpException">The <paramref name="error"/> argument has an invalid value.</exception>
		[ImplementsFunction("user_error")]
		public static bool UserError(string message, PhpError error)
		{
            if (((PhpErrorSet)error & PhpErrorSet.User) == 0)
                return false;//    PhpException.InvalidArgument("error");

			return TriggerError(message, error);
		}

		#endregion

		#region debug_backtrace, debug_print_backtrace

		/// <summary>
		/// Returns array containing current stack state. Each item is an array representing one stack frame.
		/// </summary>
		/// <returns>The stack trace.</returns>
		/// <remarks>
		/// The resulting array contains the following items (their keys are stated):
		/// <list type="bullet">
		/// <item><c>"file"</c> - a source file where the function/method has been called</item>
		/// <item><c>"line"</c> - a line in a source code where the function/method has been called</item>
		/// <item><c>"column"</c> - a column in a source code where the function/method has been called</item>
		/// <item><c>"function"</c> - a name of the function/method</item> 
		/// <item><c>"class"</c> - a name of a class where the method is declared (if any)</item>
        /// <item><c>"object"</c> - an object which metod has been called</item>
		/// <item><c>"type"</c> - either "::" for static methods or "->" for instance methods</item>
		/// </list>
        /// PHP adds one more item - "args" containing values of arguments and object which metod has been called. This is not supported.
		/// </remarks>
		[ImplementsFunction("debug_backtrace")]
		public static PhpArray Backtrace()
		{
			return new PhpStackTrace(ScriptContext.CurrentContext, 1).GetUserTrace();
		}

        /// <summary>
        /// Returns array containing current stack state. Each item is an array representing one stack frame.
        /// </summary>
        /// <returns>The stack trace.</returns>
        /// <remarks>
        /// The resulting array contains the following items (their keys are stated):
        /// <list type="bullet">
        /// <item><c>"file"</c> - a source file where the function/method has been called</item>
        /// <item><c>"line"</c> - a line in a source code where the function/method has been called</item>
        /// <item><c>"column"</c> - a column in a source code where the function/method has been called</item>
        /// <item><c>"function"</c> - a name of the function/method</item> 
        /// <item><c>"class"</c> - a name of a class where the method is declared (if any)</item>
        /// <item><c>"object"</c> - an object which metod has been called</item>
        /// <item><c>"type"</c> - either "::" for static methods or "->" for instance methods</item>
        /// </list>
        /// PHP adds two more item - "args" containing values of arguments and object which metod has been called. This is not supported.
        /// </remarks>
        /// <param name="provideObject">Recipient e-mail address.</param>
        /// <exception cref="PhpException"><paramref name="provideObject"/> has an invalid or unsupported value. (Warning)</exception>
        [ImplementsFunction("debug_backtrace")]
        public static PhpArray Backtrace(bool provideObject)
        {
            if (provideObject == true)
                PhpException.ArgumentValueNotSupported("provideObject", provideObject);
            
            return Backtrace();
        }

		/// <summary>
		/// Prints string representation of the stack trace.
        /// No value is returned.
		/// </summary>
		[ImplementsFunction("debug_print_backtrace")]
		public static void PrintBacktrace()
		{
			ScriptContext context = ScriptContext.CurrentContext;
			context.Output.Write(new PhpStackTrace(context, 1).FormatUserTrace());
		}

        /// <summary>
        /// Prints string representation of the stack trace.
        /// No value is returned.
        /// </summary>
        [ImplementsFunction("debug_print_backtrace")]
        public static void PrintBacktrace(bool provideObject)
        {
            if (provideObject == true)
                PhpException.ArgumentValueNotSupported("provideObject", provideObject);

            ScriptContext context = ScriptContext.CurrentContext;
            context.Output.Write(new PhpStackTrace(context, 1).FormatUserTrace());
        }

		#endregion

	}

	#region NS: PhpLogger
	/*
	
	  /// <summary>
	  /// Not supported.
	  /// </summary>
	/// <threadsafety static="true"/>
	  [EditorBrowsable(EditorBrowsableState.Never)]
	  public sealed class PhpLogger
	  {
		/// <summary>Prevents from creating instances of this class.</summary>
	  private PhpLogger() { }
	  
	  /// <summary>
	  /// Not supported.
	  /// </summary>
	  public enum Facility
	  {
		[ImplementsConstant("LOG_KERN")] Kern = 0,
		[ImplementsConstant("LOG_USER")] User = 8,
		[ImplementsConstant("LOG_MAIL")] Mail = 16,
		[ImplementsConstant("LOG_DAEMON")] Daemon = 24,
		[ImplementsConstant("LOG_AUTH")] Auth = 32,
		[ImplementsConstant("LOG_SYSLOG")] SysLog = 40,
		[ImplementsConstant("LOG_LPR")] Lpr = 48,
		[ImplementsConstant("LOG_NEWS")] News = 56,
		[ImplementsConstant("LOG_UUCP")] Uucp = 64,
		[ImplementsConstant("LOG_CRON")] Cron = 72,
		[ImplementsConstant("LOG_AUTHPRIV")] AuthPriv = 80
	  }
    
	  /// <summary>
	  /// Not supported.
	  /// </summary>
	  public enum Options
	  {
		[ImplementsConstant("LOG_PID")] PID = 1,
		[ImplementsConstant("LOG_CONS")] Console = 2,
		[ImplementsConstant("LOG_ODELAY")] Delay = 4,
		[ImplementsConstant("LOG_NDELAY")] NoDelay = 8,
		[ImplementsConstant("LOG_NOWAIT")] NoWait = 16,
		[ImplementsConstant("LOG_PERROR")] PrintError = 32,
	  }
      
	  /// <summary>
	  /// Not supported.
	  /// </summary>
	  public enum Priority
	  {
		[ImplementsConstant("LOG_EMERG")] Emergency = 1,
		[ImplementsConstant("LOG_ALERT")] Alert = 1,
		[ImplementsConstant("LOG_CRIT")] Critical = 1,
		[ImplementsConstant("LOG_ERR")] Error = 4,
		[ImplementsConstant("LOG_WARNING")] Warning = 5,
		[ImplementsConstant("LOG_NOTICE")] Notice = 6,
		[ImplementsConstant("LOG_INFO")] Info = 6,
		[ImplementsConstant("LOG_DEBUG")] Debug = 6
	  }
    
	  /// <summary>
	  /// Not supported.
	  /// </summary>
	  [ImplementsFunction("closelog")]
	  public static int CloseLog()
	  {
		return 0;
	  }
    
	  /// <summary>
	  /// Not supported.
	  /// </summary>
	  [ImplementsFunction("define_syslog_variables",FunctionImplOptions.NotSupported)]
	  [EditorBrowsable(EditorBrowsableState.Never)]
	  public static void DefineSyslogVariables()
	  {
		PhpException.FunctionNotSupported();
	  }
    
	  /// <summary>
	  /// Not supported.
	  /// </summary>
	  [ImplementsFunction("openlog",FunctionImplOptions.NotSupported)]
	  [EditorBrowsable(EditorBrowsableState.Never)]
	  public static int OpenLog(string prefix,Options option,Facility facility)
	  {
		PhpException.FunctionNotSupported();
		return 0;
	  }
    
	  /// <summary>
	  /// Not supported.
	  /// </summary>
	  [ImplementsFunction("syslog",FunctionImplOptions.NotSupported)]
	  [EditorBrowsable(EditorBrowsableState.Never)]
	  public static int SysLog(Priority priority,string message)
	  {
		PhpException.FunctionNotSupported();
		return 0;
	  }
	  }

	  */
	#endregion
}
