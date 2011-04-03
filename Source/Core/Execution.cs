/*

 Copyright (c) 2004-2006 Tomas Matousek.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Diagnostics;
using System.Security;

#if !SILVERLIGHT
using System.Web.Configuration;
#endif

namespace PHP.Core
{
	/// <summary>
	/// Provides functionality related to process execution.
	/// </summary>
	public class Execution
	{
		private Execution() { }

		/// <summary>
		/// How to handle external process output.
		/// </summary>
		public enum OutputHandling
		{
			/// <summary>
			/// Split the result into lines and add them to the specified collection.
			/// </summary>
			ArrayOfLines,

			/// <summary>
			/// Return entire output as a string.
			/// </summary>
			String,

			/// <summary>
			/// Write each line to the current output and flush the output after each line.
			/// </summary>
			FlushLinesToScriptOutput,

			/// <summary>
			/// Redirect all output to binary sink of the current output.
			/// </summary>
			RedirectToScriptOutput
		}

#if !SILVERLIGHT
		/// <summary>
		/// Executes a <c>cmd.exe</c> and passes it a specified command.
		/// </summary>
		/// <param name="command">The command to be passed.</param>
		/// <returns>A string containing the entire output.</returns>
		/// <remarks>Implements backticks operator (i.e. <code>`command`</code>).</remarks>
		[Emitted]
		public static string ShellExec(string command)
		{
			string result;
			ShellExec(command, OutputHandling.String, null, out result);
			return result;
		}


		/// <summary>
		/// Executes a <c>cmd.exe</c> and passes it a specified command.
		/// </summary>
		/// <param name="command">The command to be passed.</param>
		/// <param name="handling">How to handle the output.</param>
		/// <param name="arrayOutput">
		/// A list where output lines will be added if <paramref name="handling"/> is <see cref="OutputHandling.ArrayOfLines"/>.
		/// </param>
		/// <param name="stringOutput">
		/// A string containing the entire output in if <paramref name="handling"/> is <see cref="OutputHandling.String"/>
		/// or the last line of the output if <paramref name="handling"/> is <see cref="OutputHandling.ArrayOfLines"/> or
		/// <see cref="OutputHandling.FlushLinesToScriptOutput"/>. 
		/// </param>
		/// <returns>Exit code of the process.</returns>
		public static int ShellExec(string command, OutputHandling handling, IList arrayOutput, out string stringOutput)
		{
			if (!MakeCommandSafe(ref command))
			{
				stringOutput = "";
				return -1;
			}

			using (Process p = new Process())
			{
                IdentitySection identityConfig = null;

                try { identityConfig = WebConfigurationManager.GetSection("system.web/identity") as IdentitySection; }
                catch { }

                if (identityConfig != null)
                {
                    p.StartInfo.UserName = identityConfig.UserName;
                    if (identityConfig.Password != null)
                    {
                        p.StartInfo.Password = new SecureString();
                        foreach (char c in identityConfig.Password) p.StartInfo.Password.AppendChar(c);
                        p.StartInfo.Password.MakeReadOnly();
                    }                    
                }

				p.StartInfo.FileName = "cmd.exe";
				p.StartInfo.Arguments = "/c " + command;
				p.StartInfo.UseShellExecute = false;
				p.StartInfo.CreateNoWindow = true;
				p.StartInfo.RedirectStandardOutput = true;
				p.Start();

				stringOutput = null;
				switch (handling)
				{
					case OutputHandling.String:
						stringOutput = p.StandardOutput.ReadToEnd();
						break;

					case OutputHandling.ArrayOfLines:
						{
							string line;
							while ((line = p.StandardOutput.ReadLine()) != null)
							{
								stringOutput = line;
								if (arrayOutput != null) arrayOutput.Add(line);
							}
							break;
						}

					case OutputHandling.FlushLinesToScriptOutput:
						{
							ScriptContext context = ScriptContext.CurrentContext;

							string line;
							while ((line = p.StandardOutput.ReadLine()) != null)
							{
								stringOutput = line;
								context.Output.WriteLine(line);
								context.Output.Flush();
							}
							break;
						}

					case OutputHandling.RedirectToScriptOutput:
						{
							ScriptContext context = ScriptContext.CurrentContext;

							byte[] buffer = new byte[1024];
							int count;
							while ((count = p.StandardOutput.BaseStream.Read(buffer, 0, buffer.Length)) > 0)
							{
								context.OutputStream.Write(buffer, 0, count);
							}
							break;
						}
				}

				p.WaitForExit();

				return p.ExitCode;
			}
		}
#endif

		/// <summary>
		/// Escape shell metacharacters in a specified shell command.
		/// </summary>
		/// <param name="command">The command to excape.</param>
		/// <para>
		/// On Windows platform, each occurance of a character that might be used to trick a shell command
		/// is replaced with space. These characters are 
		/// <c>", ', #, &amp;, ;, `, |, *, ?, ~, &lt;, &gt;, ^, (, ), [, ], {, }, $, \, \u000A, \u00FF, %</c>.
		/// </para>
		public static string EscapeCommand(string command)
		{
			if (command == null) return String.Empty;

			StringBuilder sb = new StringBuilder(command);

			// GENERICS:
			//			if (Environment.OSVersion.Platform!=PlatformID.Unix)
			{
				for (int i = 0; i < sb.Length; i++)
				{
					switch (sb[i])
					{
						case '"':
						case '\'':
						case '#':
						case '&':
						case ';':
						case '`':
						case '|':
						case '*':
						case '?':
						case '~':
						case '<':
						case '>':
						case '^':
						case '(':
						case ')':
						case '[':
						case ']':
						case '{':
						case '}':
						case '$':
						case '\\':
						case '\u000A':
						case '\u00FF':
						case '%':
							sb[i] = ' ';
							break;
					}
				}
			}
			//      else
			//      {
			//        // ???
			//        PhpException.FunctionNotSupported();
			//      } 

			return sb.ToString();
		}

		/// <summary>
		/// Makes command safe in similar way PHP does.
		/// </summary>
		/// <param name="command">Potentially unsafe command.</param>
		/// <returns>Safe command.</returns>
		/// <remarks>
		/// If safe mode is enabled, command is split by the first space into target path 
		/// and arguments (optionally) components. The target path must not contain '..' substring.
		/// A file name is extracted from the target path and combined with 
		/// <see cref="GlobalConfiguration.SafeModeSection.ExecutionDirectory"/>.
		/// The resulting path is checked for invalid path characters (Phalanger specific).
		/// Finally, arguments are escaped by <see cref="EscapeCommand"/> and appended to the path.
		/// If safe mode is disabled, the command remains unchanged.
		/// </remarks>
		public static bool MakeCommandSafe(ref string command)
		{
			if (command == null) return false;
#if SILVERLIGHT
			return true;
#else
			GlobalConfiguration global = Configuration.Global;

			if (!global.SafeMode.Enabled) return true;

			int first_space = command.IndexOf(' ');
			if (first_space == -1) first_space = command.Length;

			if (command.IndexOf("..", 0, first_space) >= 0)
			{
				PhpException.Throw(PhpError.Warning, "dotdot_not_allowed_in_path");
				return false;
			}

			try
			{
				string file_name = Path.GetFileName(command.Substring(0, first_space));
				string target_path = Path.Combine(global.SafeMode.ExecutionDirectory, file_name);

				// <execution directory>/<file name> <escaped arguments>
				command = String.Concat(target_path, EscapeCommand(command.Substring(first_space)));
			}
			catch (ArgumentException)
			{
				PhpException.Throw(PhpError.Warning, "path_contains_invalid_characters");
				return false;
			}

			return true;
#endif
		}
	}
}
