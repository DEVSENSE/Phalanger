/*

 Copyright (c) 2004-2006 Tomas Matousek.  

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Collections;
using System.Collections.Generic;

using PHP.Core;
using Convert = PHP.Core.Convert;

namespace PHP.Library
{
	public delegate object GetSetRestoreDelegate(LocalConfiguration config, string option, object value, IniAction action);

	/// <summary>
	/// An action which can be performed on option.
	/// </summary>
	public enum IniAction
	{
		Restore, Set, Get
	}

	[Flags]
	public enum IniFlags : byte
	{
		Unsupported = 0,
		Global = 0,

		Supported = 1,
		Local = 2,
		Http = 4,
	}

	[Flags]
	public enum IniAccessability
	{
		User = 1,
		PerDirectory = 2,
		System = 4,
		All = 7,

		Global = PerDirectory | System,
		Local = All
	}

	public static class IniOptions
	{
		#region Options Table

		/// <summary>
		/// Holds information about the option.
		/// </summary>
		public class OptionDefinition // GENERICS: struct
		{
			public readonly IniFlags Flags;
			public readonly GetSetRestoreDelegate Gsr;
			public readonly string Extension;

			internal OptionDefinition(IniFlags flags, GetSetRestoreDelegate gsr, string extension)
			{
				this.Flags = flags;
				this.Gsr = gsr;
				this.Extension = extension;
			}
		}

		private static Dictionary<string, OptionDefinition> options;
		private static GetSetRestoreDelegate GsrCoreOption = new GetSetRestoreDelegate(PhpIni.GetSetRestoreCoreOption);

		/// <summary>
		/// Gets a number of currently registered options.
		/// </summary>
		public static int Count { get { return options.Count; } }

		/// <summary>
		/// Gets an option by name.
		/// </summary>
		/// <param name="name">The name of the option.</param>
		/// <returns>Information about the option or a <B>null</B> reference if it has not been registered yet.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="name"/> is a <B>null</B> reference.</exception>
		/// <remarks>Shouldn't be called before or during option registration (not thread safe for writes).</remarks>
		public static OptionDefinition GetOption(string name)
		{
			if (name == null)
				throw new ArgumentNullException("name");

			OptionDefinition value;
			return options.TryGetValue(name, out value) ? value : null;
		}

		/// <summary>
		/// Registeres a legacy configuration option. Not thread safe.
		/// </summary>
		/// <param name="name">A case-sensitive unique option name.</param>
		/// <param name="flags">Flags.</param>
		/// <param name="gsr">A delegate pointing to a method which will perform option's value getting, setting, and restoring.</param>
		/// <param name="extension">A case-sensitive name of the extension which the option belongs to. Can be a <B>null</B> reference.</param>
		/// <remarks>
		/// Registered options are known to <c>ini_get</c>, <c>ini_set</c>, and <c>ini_restore</c> PHP functions.
		/// </remarks>
		/// <exception cref="ArgumentNullException"><paramref name="name"/> is a <B>null</B> reference.</exception>
		/// <exception cref="ArgumentNullException"><paramref name="gsr"/> is a <B>null</B> reference.</exception>
		/// <exception cref="ArgumentException">An option with specified name has already been registered.</exception>
		public static void Register(string name, IniFlags flags, GetSetRestoreDelegate gsr, string extension)
		{
			if (name == null)
				throw new ArgumentNullException("name");
			if (gsr == null)
				throw new ArgumentNullException("gsr");
			if (options.ContainsKey(name))
				throw new ArgumentException(LibResources.GetString("option_already_registered", name));

			options.Add(name, new OptionDefinition(flags, gsr, extension));
		}

		/// <summary>
		/// Registeres a Core option.
		/// </summary>
		private static void RegisterCoreOption(string name, IniFlags flags)
		{
			options.Add(name, new OptionDefinition(flags, GsrCoreOption, null));
		}

		#endregion

		#region Initialization

		static IniOptions()
		{
			options = new Dictionary<string, OptionDefinition>(150);
			RegisterCoreOption("allow_call_time_pass_reference", IniFlags.Unsupported | IniFlags.Global);
			RegisterCoreOption("allow_url_fopen", IniFlags.Supported | IniFlags.Local);
			RegisterCoreOption("allow_webdav_methods", IniFlags.Unsupported | IniFlags.Global);
			RegisterCoreOption("always_populate_raw_post_data", IniFlags.Supported | IniFlags.Local);
			RegisterCoreOption("arg_separator.input", IniFlags.Unsupported | IniFlags.Global);
			RegisterCoreOption("arg_separator.output", IniFlags.Unsupported | IniFlags.Local);
			RegisterCoreOption("asp_tags", IniFlags.Supported | IniFlags.Global);
			RegisterCoreOption("assert.active", IniFlags.Supported | IniFlags.Local);
			RegisterCoreOption("assert.bail", IniFlags.Supported | IniFlags.Local);
			RegisterCoreOption("assert.callback", IniFlags.Supported | IniFlags.Local);
			RegisterCoreOption("assert.quiet_eval", IniFlags.Supported | IniFlags.Local);
			RegisterCoreOption("assert.warning", IniFlags.Supported | IniFlags.Local);
			RegisterCoreOption("async_send", IniFlags.Unsupported | IniFlags.Local);
			RegisterCoreOption("auto_append_file", IniFlags.Unsupported | IniFlags.Global);
			RegisterCoreOption("auto_detect_line_endings", IniFlags.Unsupported | IniFlags.Local);
			RegisterCoreOption("auto_prepend_file", IniFlags.Unsupported | IniFlags.Global);
			RegisterCoreOption("browscap", IniFlags.Unsupported | IniFlags.Global);
			RegisterCoreOption("cgi.force_redirect", IniFlags.Unsupported | IniFlags.Global);
			RegisterCoreOption("cgi.redirect_status_env", IniFlags.Unsupported | IniFlags.Global);
			RegisterCoreOption("cgi.rfc2616_headers", IniFlags.Unsupported | IniFlags.Global);
			RegisterCoreOption("child_terminate", IniFlags.Unsupported | IniFlags.Local);
			RegisterCoreOption("debugger.enabled", IniFlags.Unsupported | IniFlags.Global);
			RegisterCoreOption("debugger.host", IniFlags.Unsupported | IniFlags.Global);
			RegisterCoreOption("debugger.port", IniFlags.Unsupported | IniFlags.Global);
			RegisterCoreOption("default_charset", IniFlags.Supported | IniFlags.Local | IniFlags.Http);
			RegisterCoreOption("default_mimetype", IniFlags.Supported | IniFlags.Local | IniFlags.Http);
			RegisterCoreOption("default_socket_timeout", IniFlags.Supported | IniFlags.Local);
			RegisterCoreOption("define_syslog_variables", IniFlags.Unsupported | IniFlags.Local);
			RegisterCoreOption("disable_classes", IniFlags.Unsupported | IniFlags.Global);
			RegisterCoreOption("disable_functions", IniFlags.Supported | IniFlags.Global);
			RegisterCoreOption("display_errors", IniFlags.Supported | IniFlags.Local);
			RegisterCoreOption("display_startup_errors", IniFlags.Unsupported | IniFlags.Local);
			RegisterCoreOption("doc_root", IniFlags.Unsupported | IniFlags.Global);
			RegisterCoreOption("enable_dl", IniFlags.Unsupported | IniFlags.Global);
			RegisterCoreOption("engine", IniFlags.Unsupported | IniFlags.Local);
			RegisterCoreOption("error_append_string", IniFlags.Supported | IniFlags.Local);
			RegisterCoreOption("error_log", IniFlags.Supported | IniFlags.Local);
			RegisterCoreOption("error_prepend_string", IniFlags.Supported | IniFlags.Local);
			RegisterCoreOption("error_reporting", IniFlags.Supported | IniFlags.Local);
			RegisterCoreOption("expose_php", IniFlags.Unsupported | IniFlags.Global);
			RegisterCoreOption("extension_dir", IniFlags.Supported | IniFlags.Global);
			RegisterCoreOption("fastcgi.impersonate", IniFlags.Unsupported | IniFlags.Global);
			RegisterCoreOption("file_uploads", IniFlags.Supported | IniFlags.Global);
			RegisterCoreOption("from", IniFlags.Supported | IniFlags.Local);
			RegisterCoreOption("gpc_order", IniFlags.Unsupported | IniFlags.Local);
			RegisterCoreOption("html_errors", IniFlags.Supported | IniFlags.Local);
			RegisterCoreOption("ignore_repeated_errors", IniFlags.Unsupported | IniFlags.Local);
			RegisterCoreOption("ignore_repeated_source", IniFlags.Unsupported | IniFlags.Local);
			RegisterCoreOption("ignore_user_abort", IniFlags.Supported | IniFlags.Local | IniFlags.Http);
			RegisterCoreOption("implicit_flush", IniFlags.Supported | IniFlags.Global);
			RegisterCoreOption("include_path", IniFlags.Supported | IniFlags.Local);
			RegisterCoreOption("last_modified", IniFlags.Unsupported | IniFlags.Local);
			RegisterCoreOption("log_errors", IniFlags.Supported | IniFlags.Local);
			RegisterCoreOption("log_errors_max_len", IniFlags.Unsupported | IniFlags.Local);
			RegisterCoreOption("magic_quotes_gpc", IniFlags.Supported | IniFlags.Global);
			RegisterCoreOption("magic_quotes_runtime", IniFlags.Supported | IniFlags.Local);
			RegisterCoreOption("magic_quotes_sybase", IniFlags.Supported | IniFlags.Local);
			RegisterCoreOption("max_execution_time", IniFlags.Supported | IniFlags.Local);
			RegisterCoreOption("max_input_time", IniFlags.Unsupported | IniFlags.Global);
            RegisterCoreOption("memory_limit", IniFlags.Supported | IniFlags.Local);
			RegisterCoreOption("mime_magic.magicfile", IniFlags.Unsupported | IniFlags.Global);
			RegisterCoreOption("open_basedir", IniFlags.Supported | IniFlags.Global);
			RegisterCoreOption("output_buffering", IniFlags.Supported | IniFlags.Global);
			RegisterCoreOption("output_handler", IniFlags.Supported | IniFlags.Global);
			RegisterCoreOption("post_max_size", IniFlags.Supported | IniFlags.Global);
			RegisterCoreOption("precision", IniFlags.Unsupported | IniFlags.Local);
			RegisterCoreOption("register_argc_argv", IniFlags.Supported | IniFlags.Global);
			RegisterCoreOption("register_globals", IniFlags.Supported | IniFlags.Global);
			RegisterCoreOption("register_long_arrays", IniFlags.Supported | IniFlags.Global);
			RegisterCoreOption("report_memleaks", IniFlags.Unsupported | IniFlags.Global);
			RegisterCoreOption("safe_mode", IniFlags.Supported | IniFlags.Global);
			RegisterCoreOption("safe_mode_allowed_env_vars", IniFlags.Unsupported | IniFlags.Global);
			RegisterCoreOption("safe_mode_exec_dir", IniFlags.Supported | IniFlags.Global);
			RegisterCoreOption("safe_mode_gid", IniFlags.Unsupported | IniFlags.Global);
			RegisterCoreOption("safe_mode_include_dir", IniFlags.Unsupported | IniFlags.Global);
			RegisterCoreOption("safe_mode_protected_env_vars", IniFlags.Unsupported | IniFlags.Global);
			RegisterCoreOption("session.auto_start", IniFlags.Supported | IniFlags.Global | IniFlags.Http);
			RegisterCoreOption("session.save_handler", IniFlags.Supported | IniFlags.Local | IniFlags.Http);
			RegisterCoreOption("session.name", IniFlags.Supported | IniFlags.Global | IniFlags.Http);
			RegisterCoreOption("short_open_tag", IniFlags.Supported | IniFlags.Global);
			RegisterCoreOption("sql.safe_mode", IniFlags.Unsupported | IniFlags.Global);
			RegisterCoreOption("track_errors", IniFlags.Unsupported | IniFlags.Local);
			RegisterCoreOption("unserialize_callback_func", IniFlags.Supported | IniFlags.Local);
			RegisterCoreOption("upload_max_filesize", IniFlags.Supported | IniFlags.Global);
			RegisterCoreOption("upload_tmp_dir", IniFlags.Supported | IniFlags.Global);
			RegisterCoreOption("url_rewriter.tags", IniFlags.Unsupported | IniFlags.Local);
			RegisterCoreOption("user_agent", IniFlags.Supported | IniFlags.Local);
			RegisterCoreOption("user_dir", IniFlags.Unsupported | IniFlags.Global);
			RegisterCoreOption("variables_order", IniFlags.Supported | IniFlags.Local);
			RegisterCoreOption("warn_plus_overloading", IniFlags.Unsupported | IniFlags.Global);
			RegisterCoreOption("xbithack", IniFlags.Unsupported | IniFlags.Local);
			RegisterCoreOption("y2k_compliance", IniFlags.Unsupported | IniFlags.Local);
			RegisterCoreOption("zend.ze1_compatibility_mode", IniFlags.Supported | IniFlags.Local);
		}

		#endregion

		/// <summary>
		/// Tries to get, set, or restore an option given its PHP name and value.
		/// </summary>
		/// <param name="name">The option name.</param>
		/// <param name="value">The option new value if applicable.</param>
		/// <param name="action">The action to be taken.</param>
		/// <param name="error"><B>true</B>, on failure.</param>
		/// <returns>The option old value.</returns>
		/// <exception cref="PhpException">The option not supported (Warning).</exception>
		/// <exception cref="PhpException">The option is read only but action demands write access (Warning).</exception>
		internal static object TryGetSetRestore(string name, object value, IniAction action, out bool error)
		{
			Debug.Assert(name != null);
			error = true;

			IniOptions.OptionDefinition option = GetOption(name);

			// option not found:
			if (option == null)
			{
				// check for options in native extensions:
				string result = null;
				switch (action)
				{
					case IniAction.Get: error = !Externals.IniGet(name, out result); break;
					case IniAction.Set: error = !Externals.IniSet(name, Convert.ObjectToString(value), out result); break;
					case IniAction.Restore: error = !Externals.IniRestore(name); break;
				}
				if (error) PhpException.Throw(PhpError.Warning, LibResources.GetString("unknown_option", name));
				return result;
			}

			// the option is known but not supported:
			if ((option.Flags & IniFlags.Supported) == 0)
			{
				PhpException.Throw(PhpError.Warning, LibResources.GetString("option_not_supported", name));
				return null;
			}

			// the option is global thus cannot be changed:
			if ((option.Flags & IniFlags.Local) == 0 && action != IniAction.Get)
			{
				PhpException.Throw(PhpError.Warning, LibResources.GetString("option_readonly", name));
				return null;
			}

			error = false;
			return option.Gsr(Configuration.Local, name, value, action);
		}

		/// <summary>
		/// Formats a state of the specified option into <see cref="PhpArray"/>.
		/// </summary>
		/// <param name="flags">The option's flag.</param>
		/// <param name="defaultValue">A default value of the option.</param>
		/// <param name="localValue">A script local value of the option.</param>
		/// <returns>An array containig keys <c>"global_value"</c>, <c>"local_value"</c>, <c>"access"</c>.</returns>
		private static PhpArray FormatOptionState(IniFlags flags, object defaultValue, object localValue)
		{
			PhpArray result = new PhpArray(0, 3);

			// default value:
			result.Add("global_value", Convert.ObjectToString(defaultValue));

			// local value:
			result.Add("local_value", Convert.ObjectToString(localValue));

			// accessibility:
			result.Add("access", (int)((flags & IniFlags.Local) != 0 ? IniAccessability.Local : IniAccessability.Global));

			return result;
		}

		/// <summary>
		/// Gets an array of options states formatted by <see cref="FormatOptionState"/>.
		/// </summary>
		/// <param name="extension">An extension which options to retrieve.</param>
		/// <param name="result">A dictionary where to add options.</param>
		/// <returns>An array of option states.</returns>
		/// <remarks>Options already contained in <paramref name="result"/> are overwritten.</remarks>
		internal static IDictionary GetAllOptionStates(string extension, IDictionary result)
		{
			Debug.Assert(result != null);

			LocalConfiguration local = Configuration.Local;
			LocalConfiguration @default = Configuration.DefaultLocal;

			foreach (KeyValuePair<string, OptionDefinition> entry in options)
			{
				string name = entry.Key;
				OptionDefinition def = entry.Value;

				// skips configuration which don't belong to the specified extension:
				if ((extension == null || extension == def.Extension))
				{
					if ((def.Flags & IniFlags.Supported) == 0)
					{
						result[name] = FormatOptionState(
							def.Flags,
							"Not Supported",
							"Not Supported");
					}
					else if ((def.Flags & IniFlags.Http) != 0 && System.Web.HttpContext.Current == null)
					{
						result[name] = FormatOptionState(
							def.Flags,
							"Http Context Required",
							"Http Context Required");
					}
					else
					{
						result[name] = FormatOptionState(
							def.Flags,
							def.Gsr(@default, name, null, IniAction.Get),
							def.Gsr(local, name, null, IniAction.Get));
					}
				}
			}
			return result;
		}
	}
}
