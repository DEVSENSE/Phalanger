/*

 Copyright (c) 2004-2006 Tomas Matousek.  

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.IO;
using System.Web;
using System.Xml;
using System.Threading;
using System.Collections;
using System.ComponentModel;

using PHP;
using PHP.Core;
using Convert = PHP.Core.Convert;
using System.Web.Configuration;

namespace PHP.Library
{
	#region Enumerations

	/// <summary>
	/// Assertion options.
	/// </summary>
	public enum AssertOption
	{
		/// <summary>
		/// Whether assertions are evaluated.
		/// </summary>
		[ImplementsConstant("ASSERT_ACTIVE")]
		Active,

		/// <summary>
		/// Whether an error is reported if assertion fails.
		/// </summary>
		[ImplementsConstant("ASSERT_WARNING")]
		ReportWarning,

		/// <summary>
		/// Whether script execution is terminated if assertion fails.
		/// </summary>
		[ImplementsConstant("ASSERT_BAIL")]
		Terminate,

		/// <summary>
		/// Whether to disable error reporting during assertion evaluation.
		/// </summary>
		[ImplementsConstant("ASSERT_QUIET_EVAL")]
		Quiet,

		/// <summary>
		/// The user callback to be called if assertion fails. 
		/// Can be a <B>null</B> reference which means no function is called.
		/// </summary>
		[ImplementsConstant("ASSERT_CALLBACK")]
		Callback
	}

	#endregion

	/// <summary>
	/// Class manipulating PHP configuration. 
	/// The class is provided only for backward compatibility with PHP and 
	/// is intended to be used only by a compiler of PHP language.
	/// </summary>
	/// <threadsafety static="true"/>
	public static class PhpIni
	{
		#region Default values for Core options having no equivalent in configuration record

		/// <summary>
		/// Default value for "default_mimetype" PHP configuration option.
		/// </summary>
		public const string DefaultMimetype = "text/html";

		/// <summary>
		/// Default value for "default_charset" PHP configuration option.
		/// </summary>
		public static readonly string DefaultCharset = Configuration.Application.Globalization.PageEncoding.HeaderName;

		/// <summary>
		/// A value of "error_log" option meaning System log.
		/// </summary>
		public const string ErrorLogSysLog = "syslog";

		#endregion

		#region Core Options

		internal static object GetSetRestoreCoreOption(LocalConfiguration local, string option, object value, IniAction action)
		{
			LocalConfiguration @default = Configuration.DefaultLocal;
			GlobalConfiguration global = Configuration.Global;
			ApplicationConfiguration app = Configuration.Application;

			switch (option)
			{
				#region <paths>

				case "extension_dir": Debug.Assert(action == IniAction.Get); return app.Paths.ExtNatives;

				#endregion

				#region <compiler>

				case "short_open_tag": Debug.Assert(action == IniAction.Get); return app.Compiler.ShortOpenTags;
				case "asp_tags": Debug.Assert(action == IniAction.Get); return app.Compiler.AspTags;

				#endregion

				#region <error-control>

				case "html_errors": return GSR(ref local.ErrorControl.HtmlMessages, @default.ErrorControl.HtmlMessages, value, action);
				case "display_errors": return GSR(ref local.ErrorControl.DisplayErrors, @default.ErrorControl.DisplayErrors, value, action);
				case "error_append_string": return GSR(ref local.ErrorControl.ErrorAppendString, @default.ErrorControl.ErrorAppendString, value, action);
				case "error_prepend_string": return GSR(ref local.ErrorControl.ErrorPrependString, @default.ErrorControl.ErrorPrependString, value, action);
				case "log_errors": return GSR(ref local.ErrorControl.EnableLogging, @default.ErrorControl.EnableLogging, value, action);
				case "error_log": return GsrErrorLog(local, @default, value, action);
				case "error_reporting":
					switch (action)
					{
						case IniAction.Get: return ErrorReporting();
						case IniAction.Set: return ErrorReporting(Convert.ObjectToInteger(value));
						case IniAction.Restore: return ErrorReporting((int)@default.ErrorControl.ReportErrors);
					}
					break;

				#endregion

				#region <output-control>

				case "implicit_flush":
					Debug.Assert(action == IniAction.Get);
					return @default.OutputControl.ImplicitFlush;

				case "output_handler":
					Debug.Assert(action == IniAction.Get);
					IPhpConvertible handler = @default.OutputControl.OutputHandler;
					return (handler != null) ? handler.ToString() : null;

				case "output_buffering":
					Debug.Assert(action == IniAction.Get);
					return @default.OutputControl.OutputBuffering;

				#endregion

				#region <request-control>

				case "max_execution_time":
					{
						object result = GSR(ref local.RequestControl.ExecutionTimeout, @default.RequestControl.ExecutionTimeout, value, action);

						// applies the timeout:
						if (action != IniAction.Get)
							ScriptContext.CurrentContext.ApplyExecutionTimeout(local.RequestControl.ExecutionTimeout);

						return result;
					}

				case "ignore_user_abort":
					{
						object result = GSR(ref local.RequestControl.IgnoreUserAbort, @default.RequestControl.IgnoreUserAbort, value, action);

						// enables/disables disconnection tracking:
						if (action != IniAction.Get)
							RequestContext.CurrentContext.TrackClientDisconnection = !local.RequestControl.IgnoreUserAbort;

						return result;
					}

				#endregion

				#region <file-system>

				case "allow_url_fopen": return GSR(ref local.FileSystem.AllowUrlFopen, @default.FileSystem.AllowUrlFopen, value, action);
				case "user_agent": return GSR(ref local.FileSystem.UserAgent, @default.FileSystem.UserAgent, value, action);
				case "from": return GSR(ref local.FileSystem.AnonymousFtpPassword, @default.FileSystem.AnonymousFtpPassword, value, action);
				case "default_socket_timeout": return GSR(ref local.FileSystem.DefaultSocketTimeout, @default.FileSystem.DefaultSocketTimeout, value, action);
				case "include_path": return GSR(ref local.FileSystem.IncludePaths, @default.FileSystem.IncludePaths, value, action);

				#endregion

				#region <variables>

                case "zend.ze1_compatibility_mode": Debug.Assert(action != IniAction.Set || OptionValueToBoolean(value) == false); return false;// GSR(ref local.Variables.ZendEngineV1Compatible, @default.Variables.ZendEngineV1Compatible, value, action);
				case "magic_quotes_runtime": return GSR(ref local.Variables.QuoteRuntimeVariables, @default.Variables.QuoteRuntimeVariables, value, action);
                case "magic_quotes_sybase": Debug.Assert(action == IniAction.Get || OptionValueToBoolean(value) == local.Variables.QuoteInDbManner); return local.Variables.QuoteInDbManner; //GSR(ref local.Variables.QuoteInDbManner, @default.Variables.QuoteInDbManner, value, action);
                case "magic_quotes_gpc": Debug.Assert(action == IniAction.Get || OptionValueToBoolean(value) == global.GlobalVariables.QuoteGpcVariables); return global.GlobalVariables.QuoteGpcVariables;
				case "register_argc_argv": Debug.Assert(action == IniAction.Get); return global.GlobalVariables.RegisterArgcArgv;
				case "register_globals": Debug.Assert(action == IniAction.Get); return global.GlobalVariables.RegisterGlobals;
				case "register_long_arrays": Debug.Assert(action == IniAction.Get); return global.GlobalVariables.RegisterLongArrays;
				case "variables_order": return GsrVariablesOrder(local, @default, value, action);

				case "unserialize_callback_func":
					return GSR(ref local.Variables.DeserializationCallback, @default.Variables.DeserializationCallback, value, action);

                case "always_populate_raw_post_data":
                    switch (action)
                    {
                        case IniAction.Restore: local.Variables.AlwaysPopulateRawPostData = false; break;
                        case IniAction.Set: local.Variables.AlwaysPopulateRawPostData = Convert.ObjectToBoolean(value); break;
                    }
                    return local.Variables.AlwaysPopulateRawPostData;

				#endregion

				#region <posted-files>

				case "file_uploads": Debug.Assert(action == IniAction.Get); return global.PostedFiles.Accept;
				case "upload_tmp_dir": Debug.Assert(action == IniAction.Get); return global.PostedFiles.TempPath;

				case "post_max_size":
				case "upload_max_filesize":
					{
						Debug.Assert(action == IniAction.Get);

						HttpContext context;
						if (!Web.EnsureHttpContext(out context)) return null;

						HttpRuntimeSection http_runtime_section = (HttpRuntimeSection)context.GetSection("system.web/httpRuntime");
						return (http_runtime_section != null) ? http_runtime_section.MaxRequestLength * 1024 : 0;// values in config are in kB, PHP's in B
					}

                #endregion

				#region <assert>

				case "assert.active": return GSR(ref local.Assertion.Active, @default.Assertion.Active, value, action);
				case "assert.bail": return GSR(ref local.Assertion.Terminate, @default.Assertion.Terminate, value, action);
				case "assert.quiet_eval": return GSR(ref local.Assertion.Quiet, @default.Assertion.Quiet, value, action);
				case "assert.warning": return GSR(ref local.Assertion.ReportWarning, @default.Assertion.ReportWarning, value, action);
				case "assert.callback": return GSR(ref local.Assertion.Callback, @default.Assertion.Callback, value, action);

				#endregion

				#region <safe-mode>

				case "safe_mode": Debug.Assert(action == IniAction.Get); return global.SafeMode.Enabled;
				case "open_basedir": Debug.Assert(action == IniAction.Get); return global.SafeMode.GetAllowedPathPrefixesJoin();
				case "safe_mode_exec_dir": Debug.Assert(action == IniAction.Get); return global.SafeMode.ExecutionDirectory;

				#endregion

				#region <session>

				case "session.save_handler": return PhpSession.GsrHandler(local, @default, value, action);
				case "session.auto_start": Debug.Assert(action == IniAction.Get); return local.Session.AutoStart;
				case "session.name": Debug.Assert(action == IniAction.Get); return PhpSession.Name();

				#endregion

				#region others

				case "default_charset": return GsrDefaultCharset(value, action);
				case "default_mimetype": return GsrDefaultMimetype(value, action);
                case "memory_limit": return GsrMemoryLimit(value, action);
                case "disable_functions": return GsrDisableFunctions(value, action);

				#endregion
			}

			Debug.Fail("Option '" + option + "' is supported but not implemented.");
			return null;
		}

		/// <summary>
		/// Writes Core legacy options and their values to XML text stream.
		/// Skips options whose values are the same as default values of Phalanger.
		/// </summary>
		/// <param name="writer">XML writer.</param>
		/// <param name="options">A hashtable containing PHP names and option values. Consumed options are removed from the table.</param>
		/// <param name="writePhpNames">Whether to add "phpName" attribute to option nodes.</param>
		public static void CoreOptionsToXml(XmlTextWriter writer, Hashtable options, bool writePhpNames) // GENERICS: <string,string>
		{
			if (writer == null)
				throw new ArgumentNullException("writer");
			if (options == null)
				throw new ArgumentNullException("options");

			ApplicationConfiguration app = new ApplicationConfiguration();
			GlobalConfiguration global = new GlobalConfiguration();
			LocalConfiguration local = new LocalConfiguration();
			PhpIniXmlWriter ow = new PhpIniXmlWriter(writer, options, writePhpNames);

			ow.StartSection("compiler");
			ow.WriteOption("short_open_tag", "ShortOpenTag", true, app.Compiler.ShortOpenTags);
			ow.WriteOption("asp_tags", "AspTags", false, app.Compiler.AspTags);

			ow.StartSection("variables");
			//ow.WriteOption("zend.ze1_compatibility_mode", "ZendEngineV1Compatible", false, local.Variables.ZendEngineV1Compatible);
			ow.WriteOption("register_globals", "RegisterGlobals", false, global.GlobalVariables.RegisterGlobals);
			ow.WriteOption("register_argc_argv", "RegisterArgcArgv", true, global.GlobalVariables.RegisterArgcArgv);
			ow.WriteOption("register_long_arrays", "RegisterLongArrays", true, global.GlobalVariables.RegisterLongArrays);
			ow.WriteOption("variables_order", "RegisteringOrder", "EGPCS", local.Variables.RegisteringOrder);
			//ow.WriteOption("magic_quotes_gpc", "QuoteGpcVariables", true, global.GlobalVariables.QuoteGpcVariables);
			ow.WriteOption("magic_quotes_runtime", "QuoteRuntimeVariables", false, local.Variables.QuoteRuntimeVariables);
			//ow.WriteOption("magic_quotes_sybase", "QuoteInDbManner", false, local.Variables.QuoteInDbManner);
			ow.WriteOption("unserialize_callback_func", "DeserializationCallback", null, local.Variables.DeserializationCallback);

			ow.StartSection("output-control");
			ow.WriteOption("output_buffering", "OutputBuffering", false, local.OutputControl.OutputBuffering);
			ow.WriteOption("output_handler", "OutputHandler", null, local.OutputControl.OutputHandler);
			ow.WriteOption("implicit_flush", "ImplicitFlush", false, local.OutputControl.ImplicitFlush);
			ow.WriteOption("default_mimetype", "ContentType", "text/html", DefaultMimetype);
			ow.WriteOption("default_charset", "Charset", "", DefaultCharset);

			ow.StartSection("request-control");
			ow.WriteOption("max_execution_time", "ExecutionTimeout", 30, local.RequestControl.ExecutionTimeout);
			ow.WriteOption("ignore_user_abort", "IgnoreUserAbort", false, local.RequestControl.IgnoreUserAbort);

			ow.StartSection("error-control");
			ow.WriteEnumOption("error_reporting", "ReportErrors", (int)PhpErrorSet.AllButStrict, (int)local.ErrorControl.ReportErrors, typeof(PhpError));
			ow.WriteOption("display_errors", "DisplayErrors", true, local.ErrorControl.DisplayErrors);
			ow.WriteOption("html_errors", "HtmlMessages", true, local.ErrorControl.HtmlMessages);
			ow.WriteOption("docref_root", "DocRefRoot", null, local.ErrorControl.DocRefRoot.ToString());
			ow.WriteOption("docref_ext", "DocRefExtension", null, local.ErrorControl.DocRefExtension);
			ow.WriteErrorLog("error_log", null, local.ErrorControl.SysLog, local.ErrorControl.LogFile);
			ow.WriteOption("log_errors", "EnableLogging", false, local.ErrorControl.EnableLogging);
			ow.WriteOption("error_prepend_string", "ErrorPrependString", null, local.ErrorControl.ErrorPrependString);
			ow.WriteOption("error_append_string", "ErrorAppendString", null, local.ErrorControl.ErrorAppendString);

			ow.StartSection("session-control");
			ow.WriteOption("session.auto_start", "AutoStart", false, local.Session.AutoStart);
			ow.WriteOption("session.save_handler", "Handler", "files", local.Session.Handler.Name);

			ow.StartSection("assertion");
			ow.WriteOption("assert.active", "Active", true, local.Assertion.Active);
			ow.WriteOption("assert.warning", "ReportWarning", true, local.Assertion.ReportWarning);
			ow.WriteOption("assert.bail", "Terminate", false, local.Assertion.Terminate);
			ow.WriteOption("assert.quiet_eval", "Quiet", false, local.Assertion.Quiet);
			ow.WriteOption("assert.callback", "Callback", null, local.Assertion.Callback);

			ow.StartSection("safe-mode");
			ow.WriteOption("safe_mode", "Enabled", false, global.SafeMode.Enabled);
			ow.WriteOption("open_basedir", "AllowedPathPrefixes", null, global.SafeMode.GetAllowedPathPrefixesJoin());
			ow.WriteOption("safe_mode_exec_dir", "ExecutionDirectory", null, global.SafeMode.ExecutionDirectory);

			ow.StartSection("posted-files");
			ow.WriteOption("file_uploads", "Accept", true, global.PostedFiles.Accept);
			ow.WriteOption("upload_tmp_dir", "TempPath", null, global.PostedFiles.TempPath);

			ow.StartSection("file-system");
			ow.WriteOption("allow_url_fopen", "AllowUrlFopen", true, local.FileSystem.AllowUrlFopen);
			ow.WriteOption("default_socket_timeout", "DefaultSocketTimeout", 60, local.FileSystem.DefaultSocketTimeout);
			ow.WriteOption("user_agent", "UserAgent", null, local.FileSystem.UserAgent);
			ow.WriteOption("from", "AnonymousFtpPassword", null, local.FileSystem.AnonymousFtpPassword);
			ow.WriteOption("include_path", "IncludePaths", ".", local.FileSystem.IncludePaths);

			ow.WriteEnd();
		}

		#endregion

		#region Public GSRs

		internal static bool OptionValueToBoolean(object value)
		{
			string sval = value as string;
			if (sval != null)
			{
				switch (sval.ToLower(System.Globalization.CultureInfo.InvariantCulture)) // we dont need any unicode chars lowercased properly, CurrentCulture is slow
				{
					case "on":
					case "yes": return true;

					case "off":
					case "no":
					case "none": return false;
				}
			}
			return Convert.ObjectToBoolean(value);
		}

		/// <summary>
		/// Gets, sets or restores boolean option.
		/// </summary>
		public static object GSR(ref bool option, bool defaultValue, object value, IniAction action)
		{
			object result = option;
			switch (action)
			{
				case IniAction.Set: option = OptionValueToBoolean(value); break;
				case IniAction.Restore: option = defaultValue; break;
			}
			return result;
		}

		/// <summary>
		/// Gets, sets or restores integer option.
		/// </summary>
		public static object GSR(ref int option, int defaultValue, object value, IniAction action)
		{
			object result = option;
			switch (action)
			{
				case IniAction.Set: option = Convert.ObjectToInteger(value); break;
				case IniAction.Restore: option = defaultValue; break;
			}
			return result;
		}

		/// <summary>
		/// Gets, sets or restores double option.
		/// </summary>
		public static object GSR(ref double option, double defaultValue, object value, IniAction action)
		{
			object result = option;
			switch (action)
			{
				case IniAction.Set: option = Convert.ObjectToDouble(value); break;
				case IniAction.Restore: option = defaultValue; break;
			}
			return result;
		}

		/// <summary>
		/// Gets, sets or restores string option.
		/// </summary>
		public static object GSR(ref string option, string defaultValue, object value, IniAction action)
		{
			object result = option;
			switch (action)
			{
				case IniAction.Set: option = Convert.ObjectToString(value); break;
				case IniAction.Restore: option = defaultValue; break;
			}
			return result;
		}

		/// <summary>
		/// Gets, sets or restores callback option.
		/// </summary>
		public static object GSR(ref PhpCallback option, PhpCallback defaultValue, object value, IniAction action)
		{
			object result = option;
			switch (action)
			{
				case IniAction.Set: option = Convert.ObjectToCallback(value); break;
				case IniAction.Restore: option = defaultValue; break;
			}
			return result;
		}

		#endregion

		#region Special GSRs

		/// <summary>
		/// Gets, sets or restores "default_charset" option.
		/// </summary>
		private static object GsrDefaultCharset(object value, IniAction action)
		{
			HttpContext context;

			if (!Web.EnsureHttpContext(out context)) return null;

			object result = context.Response.Charset;
			switch (action)
			{
				case IniAction.Set: context.Response.Charset = Convert.ObjectToString(value); break;
				case IniAction.Restore: context.Response.Charset = DefaultCharset; break;
			}
			return result;
		}

		/// <summary>
		/// Gets, sets or restores "default_mimetype" option.
		/// </summary>
		private static object GsrDefaultMimetype(object value, IniAction action)
		{
			HttpContext context;

			if (!Web.EnsureHttpContext(out context)) return null;

			object result = context.Response.ContentType;
			switch (action)
			{
				case IniAction.Set: context.Response.ContentType = Convert.ObjectToString(value); break;
				case IniAction.Restore: context.Response.ContentType = DefaultMimetype; break;
			}
			return result;
		}

        /// <summary>
        /// Gets, sets or restores "memory_limit" option.
        /// </summary>
        private static object GsrMemoryLimit(object value, IniAction action)
        {
            object result = -1;
            switch (action)
            {
                case IniAction.Set:
                case IniAction.Restore:
                    PhpException.ArgumentValueNotSupported("memory_limit", action);
                    break;
            }
            return result;
        }

        /// <summary>
        /// Gets, sets or restores "disable_functions" option.
        /// </summary>
        private static object GsrDisableFunctions(object value, IniAction action)
        {
            object result = "";
            switch (action)
            {
                case IniAction.Set:
                case IniAction.Restore:
                    PhpException.ArgumentValueNotSupported("disable_functions", action);
                    break;
            }
            return result;
        }

		/// <summary>
		/// Gets, sets or restores "variables_order" option.
		/// </summary>
		private static object GsrVariablesOrder(LocalConfiguration local, LocalConfiguration @default, object value, IniAction action)
		{
			object result = local.Variables.RegisteringOrder;
			switch (action)
			{
				case IniAction.Set:
					string svalue = Convert.ObjectToString(value);

					if (!LocalConfiguration.VariablesSection.ValidateRegisteringOrder(svalue))
						PhpException.Throw(PhpError.Warning, CoreResources.GetString("invalid_registering_order"));
					else
						local.Variables.RegisteringOrder = svalue;
					break;

				case IniAction.Restore:
					local.Variables.RegisteringOrder = @default.Variables.RegisteringOrder;
					break;
			}
			return result;
		}

		/// <summary>
		/// Gets, sets or restores "error_log" option.
		/// </summary>
		private static object GsrErrorLog(LocalConfiguration local, LocalConfiguration @default, object value, IniAction action)
		{
			if (action == IniAction.Restore)
			{
				local.ErrorControl.LogFile = @default.ErrorControl.LogFile;
				local.ErrorControl.SysLog = @default.ErrorControl.SysLog;
				return null;
			}

			string result = (local.ErrorControl.SysLog) ? ErrorLogSysLog : local.ErrorControl.LogFile;

			if (action == IniAction.Set)
			{
				string svalue = Convert.ObjectToString(value);
				local.ErrorControl.SysLog = (string.Compare(svalue, ErrorLogSysLog, StringComparison.InvariantCultureIgnoreCase) == 0);
				local.ErrorControl.LogFile = (local.ErrorControl.SysLog) ? svalue : null;
			}
			return result;
		}

		#endregion

		#region ini_get, ini_set, ini_restore, get_cfg_var, ini_alter

		/// <summary>
		/// Gets the value of a configuration option.
		/// </summary>
		/// <param name="option">The option name (case sensitive).</param>
		/// <returns>The option old value conveted to string or <B>false</B> on error.</returns>
		/// <exception cref="PhpException">The option is not supported (Warning).</exception>
		[ImplementsFunction("ini_get")]
		public static object Get(string option)
		{
			bool error;
			object result = IniOptions.TryGetSetRestore(option, null, IniAction.Get, out error);
			if (error) return false;

			return Convert.ObjectToString(result);
		}

		/// <summary>
		/// Sets the value of a configuration option.
		/// </summary>
		/// <param name="option">The option name (case sensitive).</param>
		/// <param name="value">The option new value.</param>
		/// <returns>The option old value converted to string or <B>false</B> on error.</returns>
		/// <exception cref="PhpException">The option is not supported (Warning).</exception>
		/// <exception cref="PhpException">The option cannot be set by script (Warning).</exception>
		[ImplementsFunction("ini_set")]
		public static object Set(string option, object value)
		{
			bool error;
			object result = IniOptions.TryGetSetRestore(option, value, IniAction.Set, out error);
			if (error) return false;

			return Convert.ObjectToString(result);
		}

		/// <summary>
		/// Restores the value of a configuration option to its global value.
        /// No value is returned.
		/// </summary>
		/// <param name="option">The option name (case sensitive).</param>
		/// <exception cref="PhpException">The option is not supported (Warning).</exception>
		[ImplementsFunction("ini_restore")]
		public static void Restore(string option)
		{
			bool error;
			IniOptions.TryGetSetRestore(option, null, IniAction.Restore, out error);
		}

		/// <summary>
		/// Gets the value of a configuration option (alias for <see cref="Get"/>).
		/// </summary>
		/// <param name="option">The option name (case sensitive).</param>
		/// <returns>The option old value conveted to string or <B>false</B> on error.</returns>
		/// <exception cref="PhpException">The option is not supported (Warning).</exception>
		[ImplementsFunction("get_cfg_var")]
		public static object GetCfgVar(string option)
		{
			return Get(option);
		}

		/// <summary>
		/// Sets the value of a configuration option (alias for <see cref="Set"/>).
		/// </summary>
		/// <param name="option">The option name (case sensitive).</param>
		/// <param name="value">The option new value converted to string.</param>
		/// <returns>The option old value.</returns>
		/// <exception cref="PhpException">The option is not supported (Warning).</exception>
		/// <exception cref="PhpException">The option cannot be set by script (Warning).</exception>
		[ImplementsFunction("ini_alter")]
		public static object Alter(string option, object value)
		{
			return Set(option, value);
		}

		#endregion

		#region get_all

		/// <summary>
		/// Retrieves an array of all configuration entries.
		/// </summary>
		/// <seealso cref="GetAll(string)"/>
		[ImplementsFunction("ini_get_all")]
		public static PhpArray GetAll()
		{
			return (PhpArray)IniOptions.GetAllOptionStates(null, new PhpArray(0, IniOptions.Count));
		}

		/// <summary>
		/// Retrieves an array of configuration entries of a specified extension.
		/// </summary>
		/// <param name="extension">The PHP internal extension name.</param>
		/// <remarks>
		/// For each supported configuration option an entry is added to the resulting array.
		/// The key is the name of the option and the value is an array having three entries: 
		/// <list type="bullet">
		///   <item><c>global_value</c> - global value of the option</item>
		///   <item><c>local_value</c> - local value of the option</item>
		///   <item><c>access</c> - 7 (PHP_INI_ALL), 6 (PHP_INI_PERDIR | PHP_INI_SYSTEM) or 4 (PHP_INI_SYSTEM)</item>
		/// </list>
		/// </remarks>
		[ImplementsFunction("ini_get_all")]
		public static PhpArray GetAll(string extension)
		{
			PhpArray result = Externals.IniGetAll(extension);

			// adds options from managed libraries:
			IniOptions.GetAllOptionStates(extension, result);

			return result;
		}

		#endregion

		#region assert_options

		/// <summary>
		/// Gets a value of an assert option.
		/// </summary>
		/// <param name="option">The option which value to get.</param>
		/// <returns>The value of the option.</returns>
		[ImplementsFunction("assert_options")]
		public static object AssertOptions(AssertOption option)
		{
			return AssertOptions(option, null, IniAction.Get);
		}

		/// <summary>
		/// Sets a value of an assert option.
		/// </summary>
		/// <param name="option">The option which value to get.</param>
		/// <param name="value">The new value for the option.</param>
		/// <returns>The value of the option.</returns>
		[ImplementsFunction("assert_options")]
		public static object AssertOptions(AssertOption option, object value)
		{
			return AssertOptions(option, value, IniAction.Set);
		}

		/// <summary>
		/// Implementation of <see cref="AssertOptions(AssertOption)"/> and <see cref="AssertOptions(AssertOption,object)"/>.
		/// </summary>
		/// <remarks>Only gets/sets. No restore.</remarks>
		private static object AssertOptions(AssertOption option, object value, IniAction action)
		{
			LocalConfiguration config = Configuration.Local;

			switch (option)
			{
				case AssertOption.Active:
					return GSR(ref config.Assertion.Active, false, value, action);

				case AssertOption.Callback:
					return GSR(ref config.Assertion.Callback, null, value, action);

				case AssertOption.Quiet:
					return GSR(ref config.Assertion.Quiet, false, value, action);

				case AssertOption.Terminate:
					return GSR(ref config.Assertion.Terminate, false, value, action);

				case AssertOption.ReportWarning:
					return GSR(ref config.Assertion.ReportWarning, false, value, action);

				default:
					PhpException.InvalidArgument("option");
					return false;
			}
		}

		#endregion

		#region get_include_path, set_include_path, restore_include_path

		/// <summary>
		/// Gets a value of "include_path" option.
		/// </summary>
		/// <returns>The current value.</returns>
		[ImplementsFunction("get_include_path")]
		public static string GetIncludePath()
		{
			return Configuration.Local.FileSystem.IncludePaths;
		}

		/// <summary>
		/// Sets a new value of "include_path" option.
		/// </summary>
		/// <returns>A previous value.</returns>
		[ImplementsFunction("set_include_path")]
		public static string SetIncludePath(string value)
		{
			LocalConfiguration config = Configuration.Local;
			string result = config.FileSystem.IncludePaths;
			config.FileSystem.IncludePaths = value;
			return result;
		}

		/// <summary>
		/// Restores a value of "include_path" option from global configuration.
        /// No value is returned.
		/// </summary>
		[ImplementsFunction("restore_include_path")]
		public static void RestoreIncludePath()
		{
			Configuration.Local.FileSystem.IncludePaths = Configuration.DefaultLocal.FileSystem.IncludePaths;
		}

		#endregion

		#region get_magic_quotes_gpc, get_magic_quotes_runtime, set_magic_quotes_runtime

		/// <summary>
		/// Gets a value of "magic_quotes_gpc" option.
		/// </summary>
		/// <returns>The current value.</returns>
		[ImplementsFunction("get_magic_quotes_gpc")]
		public static bool GetMagicQuotesGPC()
		{
            return Configuration.Global.GlobalVariables.QuoteGpcVariables;
		}

		/// <summary>
		/// Gets a value of "magic_quotes_runtime" option.
		/// </summary>
		/// <returns>The current value.</returns>
		[ImplementsFunction("get_magic_quotes_runtime")]
		public static bool GetMagicQuotesRuntime()
		{
			return Configuration.Local.Variables.QuoteRuntimeVariables;
		}

		/// <summary>
		/// Sets a new value of "magic_quotes_runtime" option.
		/// </summary>
		/// <param name="value">The new value.</param>
		/// <returns>A previous value.</returns>
		[ImplementsFunction("set_magic_quotes_runtime")]
		public static bool SetMagicQuotesRuntime(bool value)
		{
			LocalConfiguration local = Configuration.Local;
			bool result = local.Variables.QuoteRuntimeVariables;
			local.Variables.QuoteRuntimeVariables = value;
			return result;
		}

		#endregion

		#region error_reporting, set_error_handler, restore_error_handler, set_exception_handler, restore_exception_handler

		/// <summary>
		/// Retrieves the current error reporting level.
		/// </summary>
		/// <returns>
		/// The bitmask of error types which are reported. Returns 0 if error reporting is disabled
		/// by means of @ operator.
		/// </returns>
		[ImplementsFunction("error_reporting")]
		public static int ErrorReporting()
		{
			return ScriptContext.CurrentContext.ErrorReportingLevel;
		}

		/// <summary>
		/// Sets a new level of error reporting.
		/// </summary>
		/// <param name="level">The new level.</param>
		/// <returns>The original level.</returns>
		[ImplementsFunction("error_reporting")]
		public static int ErrorReporting(int level)
		{
			if ((level & (int)PhpErrorSet.All) == 0 && level != 0)
				PhpException.InvalidArgument("level");

			ScriptContext context = ScriptContext.CurrentContext;
			int result = context.ErrorReportingLevel;
			context.Config.ErrorControl.ReportErrors = PhpErrorSet.All & (PhpErrorSet)level;
			return result;
		}

		/// <summary>
		/// Internal record in the error handler stack.
		/// </summary>
		private class ErrorHandlerRecord
		{
			/// <summary>
			/// Error handler callback.
			/// </summary>
			public PhpCallback ErrorHandler;

			/// <summary>
			/// Error types to be handled.
			/// </summary>
			public PhpError ErrorTypes;

			/// <summary>
			/// Public constructor of the class.
			/// </summary>
			/// <param name="handler">Error handler callback.</param>
			/// <param name="errors">Error types to be handled.</param>
			public ErrorHandlerRecord(PhpCallback handler, PhpError errors)
			{
				ErrorHandler = handler;
				ErrorTypes = errors;
			}
		}

		/// <summary>
		/// Stores user error handlers which has been rewritten by a new one.
		/// </summary>
		[ThreadStatic]
		private static Stack OldUserErrorHandlers;          // GENERICS: <ErrorHandlerRecord>

		/// <summary>
		/// Stores user exception handlers which has been rewritten by a new one.
		/// </summary>
		[ThreadStatic]
		private static Stack OldUserExceptionHandlers;          // GENERICS: <PhpCallback>

		/// <summary>
		/// Clears <see cref="OldUserErrorHandlers"/> and <see cref="OldUserExceptionHandlers"/> on request end.
		/// </summary>
		private static void ClearOldUserHandlers()
		{
			OldUserErrorHandlers = null;
			OldUserExceptionHandlers = null;
		}

		/// <summary>
		/// Sets user defined handler to handle errors.
		/// </summary>
        /// <param name="caller">The class context used to bind the callback.</param>
		/// <param name="newHandler">The user callback called to handle an error.</param>
		/// <returns>
		/// The PHP representation of previous user handler, <B>null</B> if there is no user one, or 
		/// <B>false</B> if <paramref name="newHandler"/> is invalid or empty.
		/// </returns>
		/// <remarks>
		/// Stores old user handlers on the stack so that it is possible to 
		/// go back to arbitrary previous user handler.
		/// </remarks>
		[ImplementsFunction("set_error_handler", FunctionImplOptions.NeedsClassContext)]
        public static object SetErrorHandler(PHP.Core.Reflection.DTypeDesc caller, PhpCallback newHandler)
		{
			return SetErrorHandler(caller, newHandler, (int)PhpErrorSet.Handleable);
		}

		/// <summary>
		/// Sets user defined handler to handle errors.
		/// </summary>
        /// <param name="caller">The class context used to bind the callback.</param>
        /// <param name="newHandler">The user callback called to handle an error.</param>
		/// <param name="errorTypes">Error types to be handled by the handler.</param>
		/// <returns>
		/// The PHP representation of previous user handler, <B>null</B> if there is no user one, or 
		/// <B>false</B> if <paramref name="newHandler"/> is invalid or empty.
		/// </returns>
		/// <remarks>
		/// Stores old user handlers on the stack so that it is possible to 
		/// go back to arbitrary previous user handler.
		/// </remarks>
		[ImplementsFunction("set_error_handler", FunctionImplOptions.NeedsClassContext)]
        public static object SetErrorHandler(PHP.Core.Reflection.DTypeDesc caller, PhpCallback newHandler, int errorTypes)
		{
			if (!PhpArgument.CheckCallback(newHandler, caller, "newHandler", 0, false)) return null;

			PhpCallback old_handler = Configuration.Local.ErrorControl.UserHandler;
			PhpError old_errors = Configuration.Local.ErrorControl.UserHandlerErrors;

			// previous handler was defined by user => store it into the stack:
			if (old_handler != null)
			{
				if (OldUserErrorHandlers == null)
				{
					OldUserErrorHandlers = new Stack(5);
                    RequestContext.RequestEnd += new Action(ClearOldUserHandlers);
				}
				OldUserErrorHandlers.Push(new ErrorHandlerRecord(old_handler, old_errors));
			}

			// sets the current handler:
			Configuration.Local.ErrorControl.UserHandler = newHandler;
			Configuration.Local.ErrorControl.UserHandlerErrors = (PhpError)errorTypes;

			// returns the previous handler:
			return (old_handler != null) ? old_handler.ToPhpRepresentation() : null;
		}

		/// <summary>
		/// Restores the previous user error handler if there was any.
		/// </summary>
		[ImplementsFunction("restore_error_handler")]
		public static bool RestoreErrorHandler()
		{
			// if some user handlers has been stored in the stack then restore the top-most, otherwise set to null:
			if (OldUserErrorHandlers != null && OldUserErrorHandlers.Count > 0)
			{
				ErrorHandlerRecord record = (ErrorHandlerRecord)OldUserErrorHandlers.Pop();

				Configuration.Local.ErrorControl.UserHandler = record.ErrorHandler;
				Configuration.Local.ErrorControl.UserHandlerErrors = record.ErrorTypes;
			}
			else
			{
				Configuration.Local.ErrorControl.UserHandler = null;
				Configuration.Local.ErrorControl.UserHandlerErrors = (PhpError)PhpErrorSet.None;
			}

			return true;
		}

		/// <summary>
		/// Sets user defined handler to handle exceptions.
		/// </summary>
        /// <param name="caller">The class context used to bind the callback.</param>
        /// <param name="newHandler">The user callback called to handle an exceptions.</param>
		/// <returns>
		/// The PHP representation of previous user handler, <B>null</B> if there is no user one, or 
		/// <B>false</B> if <paramref name="newHandler"/> is invalid or empty.
		/// </returns>
		/// <remarks>
		/// Stores old user handlers on the stack so that it is possible to 
		/// go back to arbitrary previous user handler.
		/// </remarks>
		[ImplementsFunction("set_exception_handler", FunctionImplOptions.NeedsClassContext)]
        public static object SetExceptionHandler(PHP.Core.Reflection.DTypeDesc caller, PhpCallback newHandler)
		{
			if (!PhpArgument.CheckCallback(newHandler, caller, "newHandler", 0, false)) return null;

			PhpCallback old_handler = Configuration.Local.ErrorControl.UserExceptionHandler;

			// previous handler was defined by user => store it into the stack:
			if (old_handler != null)
			{
				if (OldUserExceptionHandlers == null)
				{
					OldUserExceptionHandlers = new Stack(5);
                    RequestContext.RequestEnd += new Action(ClearOldUserHandlers);
				}
				OldUserExceptionHandlers.Push(old_handler);
			}

			// sets the current handler:
			Configuration.Local.ErrorControl.UserExceptionHandler = newHandler;

			// returns the previous handler:
			return (old_handler != null) ? old_handler.ToPhpRepresentation() : null;
		}

		/// <summary>
		/// Restores the previous user error handler if there was any.
		/// </summary>
		[ImplementsFunction("restore_exception_handler")]
		public static bool RestoreExceptionHandler()
		{
			if (OldUserExceptionHandlers != null && OldUserExceptionHandlers.Count > 0)
				Configuration.Local.ErrorControl.UserExceptionHandler = (PhpCallback)OldUserExceptionHandlers.Pop();
			else
				Configuration.Local.ErrorControl.UserExceptionHandler = null;

			return true;
		}

		#endregion

		#region set_time_limit, ignore_user_abort

		/// <summary>
		/// Sets the request time-out in seconds (configuration option "max_execution_time").
        /// No value is returned.
		/// </summary>
		/// <param name="seconds">The time-out setting for request.</param>
		[ImplementsFunction("set_time_limit")]
		public static void SetTimeLimit(int seconds)
		{
			ScriptContext.CurrentContext.ApplyExecutionTimeout(seconds);
		}


		/// <summary>
		/// Get a value of a configuration option "ignore_user_abort".
		/// </summary>
		/// <returns>The current value of the option.</returns>
		[ImplementsFunction("ignore_user_abort")]
		public static bool IgnoreUserAbort()
		{
			return Configuration.Local.RequestControl.IgnoreUserAbort;
		}

		/// <summary>
		/// Sets a value of a configuration option "ignore_user_abort".
		/// </summary>
		/// <param name="value">The new value of the option.</param>
		/// <returns>The previous value of the option.</returns>
		/// <exception cref="PhpException">Web request PHP context is not available (Warning).</exception>
		[ImplementsFunction("ignore_user_abort")]
		public static bool IgnoreUserAbort(bool value)
		{
			RequestContext context;
			if (!Web.EnsureRequestContext(out context)) return true;

			LocalConfiguration local = Configuration.Local;
			bool result = local.RequestControl.IgnoreUserAbort;
			local.RequestControl.IgnoreUserAbort = value;

			// enables/disables disconnection tracking:
			context.TrackClientDisconnection = !value;

			return result;
		}

		#endregion
	}

	#region PhpIniXmlWriter

	public sealed class PhpIniXmlWriter
	{
		private readonly XmlTextWriter writer;
		private readonly Hashtable options; // GENERICS: <string,string>
		private readonly bool writePhpNames;

		private string startSection;
		private bool sectionOpened;

		public PhpIniXmlWriter(XmlTextWriter writer, Hashtable options, bool writePhpNames)
		{
			this.writer = writer;
			this.options = options;
			this.writePhpNames = writePhpNames;
		}

		public void WriteEnd()
		{
			if (sectionOpened)
				writer.WriteEndElement();
		}

		public void StartSection(string name)
		{
			startSection = name;
		}

		private void StartElement()
		{
			if (startSection != null)
			{
				if (sectionOpened)
					writer.WriteEndElement();

				writer.WriteStartElement(startSection);
				startSection = null;
				sectionOpened = true;
			}
		}

		private void WriteSetNode(string phpName, string xmlName, string value)
		{
			StartElement();
			writer.WriteStartElement("set");

			writer.WriteAttributeString("name", xmlName);
			writer.WriteAttributeString("value", value);
			if (writePhpNames)
				writer.WriteAttributeString("phpName", phpName);

			writer.WriteEndElement();
		}

		private void WriteEnumSetNode(string phpName, string xmlName, int value, Type type)
		{
			StartElement();
			writer.WriteStartElement("set");

			writer.WriteAttributeString("name", xmlName);
			if (writePhpNames)
				writer.WriteAttributeString("phpName", phpName);

			writer.WriteStartElement("clear");
			writer.WriteEndElement();

			writer.WriteStartElement("add");
			writer.WriteAttributeString("value", Enum.Format(type, value, "G"));
			writer.WriteEndElement();

			writer.WriteEndElement();
		}

		public void WriteOption(string phpName, string xmlName, string phpValue, string defValue)
		{
			if (options.ContainsKey(phpName))
			{
				phpValue = (string)options[phpName];
				options.Remove(phpName);
			}

			if (phpValue == null) phpValue = "";
			if (defValue == null) defValue = "";

			if (phpValue != defValue)
				WriteSetNode(phpName, xmlName, phpValue);
		}

		public void WriteOption(string phpName, string xmlName, bool phpValue, bool defValue)
		{
			if (options.ContainsKey(phpName))
			{
				phpValue = Core.Convert.StringToBoolean((string)options[phpName]);
				options.Remove(phpName);
			}

			if (phpValue != defValue)
				WriteSetNode(phpName, xmlName, phpValue ? "true" : "false");
		}

		public void WriteByteSize(string phpName, string xmlName, int phpValue, int defValue)
		{
			if (options.ContainsKey(phpName))
			{
				phpValue = Core.Convert.StringByteSizeToInteger((string)options[phpName]);
				options.Remove(phpName);
			}

			if (phpValue != defValue)
				WriteSetNode(phpName, xmlName, phpValue.ToString());
		}

		public void WriteOption(string phpName, string xmlName, int phpValue, int defValue)
		{
			if (options.ContainsKey(phpName))
			{
				phpValue = Core.Convert.StringToInteger((string)options[phpName]);
				options.Remove(phpName);
			}

			if (phpValue != defValue)
				WriteSetNode(phpName, xmlName, phpValue.ToString());
		}

		public void WriteOption(string phpName, string xmlName, double phpValue, double defValue)
		{
			if (options.ContainsKey(phpName))
			{
				phpValue = Core.Convert.StringToDouble((string)options[phpName]);
				options.Remove(phpName);
			}

			if (phpValue != defValue)
				WriteSetNode(phpName, xmlName, phpValue.ToString());
		}

		public void WriteOption(string phpName, string xmlName, string phpValue, PhpCallback defValue)
		{
			WriteOption(phpName, xmlName, phpValue, (defValue != null) ? ((IPhpConvertible)defValue).ToString() : null);
		}

		internal void WriteErrorLog(string phpName, string phpValue, bool defSysLog, string defLogFile)
		{
			if (options.ContainsKey(phpName))
			{
				phpValue = (string)options[phpName];
				options.Remove(phpName);
			}

			bool phpSysLog = phpValue == PhpIni.ErrorLogSysLog;
			string phpLogFile = phpSysLog ? null : phpValue;

			if (phpLogFile == null) phpLogFile = "";
			if (defLogFile == null) defLogFile = "";

			if (phpLogFile != defLogFile)
				WriteSetNode(phpName, "LogFile", phpLogFile);

			if (phpSysLog != defSysLog)
				WriteSetNode(phpName, "SysLog", phpSysLog ? "true" : "false");
		}

		public void WriteEnumOption(string phpName, string xmlName, int phpValue, int defValue, Type type)
		{
			if (options.ContainsKey(phpName))
			{
				phpValue = Core.Convert.StringToInteger((string)options[phpName]);
				options.Remove(phpName);
			}

			if (phpValue != defValue)
				WriteEnumSetNode(phpName, xmlName, phpValue, type);
		}

	}

	#endregion
}
