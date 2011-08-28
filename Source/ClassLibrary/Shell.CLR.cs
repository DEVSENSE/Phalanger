/*

 Copyright (c) 2004-2006 Tomas Matousek.  

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Web;
using System.Text;
using System.ComponentModel;

using PHP.Core;
using System.Diagnostics;

namespace PHP.Library
{
	/// <summary>
	/// Implements program execution functions defined by PHP.
	/// </summary>
	/// <threadsafety static="true"/>
	public static class Shell
	{
		#region getenv, putenv

		/// <summary>
		/// Gets a value of an environment variable associated with a current process.
		/// </summary>
		/// <param name="name">A name of the variable.</param>
		/// <returns>Current value of the variable.</returns>
		[ImplementsFunction("getenv")]
		[return: CastToFalse]
		public static string GetEnvironmentVariable(string name)
		{
			if (string.IsNullOrEmpty(name)) return null;

            var servervar = ScriptContext.CurrentContext.AutoGlobals.Server.Value as PhpArray;
            if (servervar != null)
            {
                object value;
                if (servervar.TryGetValue(name, out value))
                    return PHP.Core.Convert.ObjectToString(PhpVariable.Dereference(value));
            }

            return Environment.GetEnvironmentVariable(name);
		}

		/// <summary>
		/// Sets an environment variable of the current process.
		/// </summary>
		/// <param name="setting">String in format "{name}={value}".</param>
		[ImplementsFunction("putenv")]
		public static bool SetEnvironmentVariable(string setting)
		{
			if (HttpContext.Current != null)
			{
				PhpException.Throw(PhpError.Warning, LibResources.GetString("function_disallowed_in_web_context"));
				return false;
			}

			if (String.IsNullOrEmpty(setting))
			{
				PhpException.InvalidArgument("setting", LibResources.GetString("arg:null_or_empty"));
				return false;
			}

			int separator_pos = setting.IndexOf('=');
			if (separator_pos == -1)
			{
				PhpException.Throw(PhpError.Warning, LibResources.GetString("arg:invalid_value", "setting", setting));
				return false;
			}

			string name = setting.Substring(0, separator_pos);
			string value = setting.Substring(separator_pos + 1);

			try
			{
				Environment.SetEnvironmentVariable(name, value);
			}
			catch (Exception e)
			{
				PhpException.Throw(PhpError.Warning, e.Message);
				return false;
			}

			return true;
		}

		#endregion

		#region escapeshellarg, escapeshellcmd

		/// <summary>
		/// Escapes argument to be passed to shell command.
		/// </summary>
		/// <param name="arg">The argument to excape.</param>
		/// <returns>
		/// <para>
		/// On Windows platform, each occurance of double quote (") and ampersand (&amp;) 
		/// is replaced with a single space. The resulting string is then put into double quotes.
		/// </para>
		/// <para>
		/// On Unix platform, each occurance of single quote (')
		/// is replaced with characters '\'''. The resulting string is then put into single quotes.
		/// </para>
		/// </returns>
		[ImplementsFunction("escapeshellarg")]
		public static string EscapeShellArg(string arg)
		{
			if (arg == null || arg.Length == 0) return String.Empty;

			StringBuilder sb;

			if (Environment.OSVersion.Platform != PlatformID.Unix)
			{
				sb = new StringBuilder(arg.Length + 2);
				sb.Append(' ');
				sb.Append(arg);
				sb.Replace('"', ' ');
				sb.Replace('%', ' ');
				sb.Append('"');
				sb[0] = '\'';
			}
			else
			{
				sb = new StringBuilder(arg.Length + 2);
				sb.Append(' ');
				sb.Append(arg);
				sb.Replace("'", @"'\''");
				sb.Append('\'');
				sb[0] = '\'';
			}

			return sb.ToString();
		}

		/// <summary>
		/// Escape shell metacharacters in a specified shell command.
		/// </summary>
		/// <param name="command">The command to excape.</param>
		/// <para>
		/// On Windows platform, each occurance of a character that might be used to trick a shell command
		/// is replaced with space. These characters are 
		/// <c>", ', #, &amp;, ;, `, |, *, ?, ~, &lt;, &gt;, ^, (, ), [, ], {, }, $, \, \u000A, \u00FF, %</c>.
		/// </para>
		[ImplementsFunction("escapeshellcmd")]
		public static string EscapeShellCmd(string command)
		{
			return Execution.EscapeCommand(command);
		}

		#endregion

		#region exec

		/// <summary>
		/// Executes a shell command.
		/// </summary>
		/// <param name="command">The command to execute.</param>
		/// <returns>The last line of the output.</returns>
		[ImplementsFunction("exec")]
		public static string Exec(string command)
		{
			string result;

			Execution.ShellExec(command, Execution.OutputHandling.ArrayOfLines, null, out result);
			return Core.Convert.Quote(result, ScriptContext.CurrentContext);
		}

		/// <summary>
		/// Executes a shell command.
		/// </summary>
		/// <param name="command">The command to execute.</param>
		/// <param name="output">An array where to add items of output. One item per each line of the output.</param>
		/// <returns>The last line of the output.</returns>
		[ImplementsFunction("exec")]
		public static string Exec(string command, ref PhpArray output)
		{
			int exit_code;
			return Exec(command, ref output, out exit_code);
		}

		/// <summary>
		/// Executes a shell command.
		/// </summary>
		/// <param name="command">The command to execute.</param>
		/// <param name="output">An array where to add items of output. One item per each line of the output.</param>
		/// <param name="exitCode">Exit code of the process.</param>
		/// <returns>The last line of the output.</returns>
		[ImplementsFunction("exec")]
		public static string Exec(string command, ref PhpArray output, out int exitCode)
		{
			// creates a new array if user specified variable not containing one:
			if (output == null) output = new PhpArray();

			string result;
			exitCode = Execution.ShellExec(command, Execution.OutputHandling.ArrayOfLines, output, out result);

			return Core.Convert.Quote(result, ScriptContext.CurrentContext);
		}

		#endregion

		#region pasthru

		/// <summary>
		/// Executes a command and writes raw output to the output sink set on the current script context.
		/// </summary>
		/// <param name="command">The command.</param>
		[ImplementsFunction("passthru")]
		public static void PassThru(string command)
		{
			string dummy;
			Execution.ShellExec(command, Execution.OutputHandling.RedirectToScriptOutput, null, out dummy);
		}

		/// <summary>
		/// Executes a command and writes raw output to the output sink set on the current script context.
		/// </summary>
		/// <param name="command">The command.</param>
		/// <param name="exitCode">An exit code of the process.</param>
		[ImplementsFunction("passthru")]
		public static void PassThru(string command, out int exitCode)
		{
			string dummy;
			exitCode = Execution.ShellExec(command, Execution.OutputHandling.RedirectToScriptOutput, null, out dummy);
		}

		#endregion

		#region system

		/// <summary>
		/// Executes a command and writes output line by line to the output sink set on the current script context.
		/// Flushes output after each written line.
		/// </summary>
		/// <param name="command">The command.</param>
		/// <returns>
		/// Either the last line of the output or a <B>null</B> reference if the command fails (returns non-zero exit code).
		/// </returns>
		[ImplementsFunction("system")]
		[return: CastToFalse]
		public static string System(string command)
		{
			int exit_code;
			return System(command, out exit_code);
		}

		/// <summary>
		/// Executes a command and writes output line by line to the output sink set on the current script context.
		/// Flushes output after each written line.
		/// </summary>
		/// <param name="command">The command.</param>
		/// <param name="exitCode">An exit code of the process.</param>
		/// <returns>
		/// Either the last line of the output or a <B>null</B> reference if the command fails (returns non-zero exit code).
		/// </returns>
		[ImplementsFunction("system")]
		[return: CastToFalse]
		public static string System(string command, out int exitCode)
		{
			string result;
			exitCode = Execution.ShellExec(command, Execution.OutputHandling.FlushLinesToScriptOutput, null, out result);
			return (exitCode == 0) ? result : null;
		}

		#endregion

		#region shell_exec

		[ImplementsFunction("shell_exec")]
		public static string ShellExec(string command)
		{
			string result;
			Execution.ShellExec(command, Execution.OutputHandling.String, null, out result);
			return result;
		}

		#endregion

        #region getopt

        [ImplementsFunction("getopt")]
        [return: CastToFalse]
        public static PhpArray GetOptions(string options)
        {
            return GetOptions(options, null);
        }

        /// <summary>
        /// Gets options from the command line argument list.
        /// </summary>
        /// <param name="options">Each character in this string will be used as option characters and matched against options passed to the script starting with a single hyphen (-).   For example, an option string "x" recognizes an option -x.   Only a-z, A-Z and 0-9 are allowed. </param>
        /// <param name="longopts">An array of options. Each element in this array will be used as option strings and matched against options passed to the script starting with two hyphens (--).   For example, an longopts element "opt" recognizes an option --opt. </param>
        /// <returns>This function will return an array of option / argument pairs or FALSE  on failure. </returns>
        [ImplementsFunction("getopt")]
        [return:CastToFalse]
        public static PhpArray GetOptions(string options, PhpArray longopts)
        {
            var args = Environment.GetCommandLineArgs();

            PhpArray result = new PhpArray();

            // process single char options
            if (options != null)
                for (int i = 0; i < options.Length; ++i )
                {
                    char opt = options[i];
                    if (!char.IsLetterOrDigit(opt))
                        break;
                    
                    int ncolons = 0;
                    if (i+1<options.Length && options[i+1] == ':'){++ncolons;++i;}    // require value
                    if (i+1<options.Length && options[i+1] == ':'){++ncolons;++i;}    // optional value

                    object value = ParseOption(opt.ToString(), false, ncolons == 1, ncolons == 2, args);
                    if (value != null)
                        result.Add(opt.ToString(), value);
                }

            // process long options
            if (longopts != null)
                foreach (var opt in longopts)
                {
                    string str = PhpVariable.AsString(opt.Value);
                    if (str == null) continue;

                    int ncolons = 0;
                    if (str.EndsWith(":")) ncolons = (str.EndsWith("::")) ? 2 : 1;
                    str = str.Substring(0, str.Length - ncolons);// remove colons

                    object value = ParseOption(str, true, ncolons == 1, ncolons == 2, args);
                    if (value != null)
                        result.Add(str, value);
                }

            return result;
        }

        private static object ParseOption(string option, bool longOpt, bool valueRequired, bool valueOptional, string[] args)
        {
            string prefix = (longOpt ? "--" : "-") + option;
            bool noValue = (!valueOptional && !valueRequired);

            // find matching arg
            for (int a = 1; a < args.Length; ++a)
            {
                string arg = args[a];
                if (arg.StartsWith(prefix))
                {
                    if (noValue)
                    {
                        if (arg.Length == prefix.Length) return false;   // OK, no value
                        if (longOpt) continue; // try another arg
                        return null;    // invalid arg
                    }

                    // value is optional or required
                    // try value after the prefix
                    string value = arg.Substring(prefix.Length);

                    if (value.Length > 0)
                    {
                        bool eq = (value[0] == '=');    // '=' follows
                        if (longOpt && !eq)continue;    // long options can have value only after =
                        if (eq) value = value.Substring(1); // remove the '=' char
                        return value;   // value resolved (optional or required)
                    }

                    if (valueOptional) return false;

                    // value required
                    if (a + 1 >= args.Length) return null;  // missing value
                    return args[a + 1];
                }
            }

            // not found
            return null;
        }

        #endregion
    }
}
