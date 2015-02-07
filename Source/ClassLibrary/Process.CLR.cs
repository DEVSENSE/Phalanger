/*

 Copyright (c) 2005-2006 Tomas Matousek.  

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.IO;
using System.Threading;
using System.Collections;
using Process = System.Diagnostics.Process;

using PHP.Core;
using PHP.Core.Reflection;

namespace PHP.Library
{
	#region PhpProcessHandle

	public class PhpProcessHandle : PhpResource
	{
		public Process/*!*/ Process { get { return process; } }
		private Process/*!*/ process;

		public string/*!*/ Command { get { return command; } }
		private string/*!*/ command;

		internal PhpProcessHandle(Process/*!*/ process, string/*!*/ command)
			: base("process")
		{
			Debug.Assert(process != null && command != null);
			this.process = process;
			this.command = command;
		}

		protected override void FreeManaged()
		{
			process.Close();
			base.FreeManaged();
		}

		internal static PhpProcessHandle Validate(PhpResource resource)
		{
			PhpProcessHandle result = resource as PhpProcessHandle;

			if (result == null || !result.IsValid)
			{
				PhpException.Throw(PhpError.Warning, LibResources.GetString("invalid_process_resource"));
				return null;
			}

			return result;
		}
	}

	#endregion

	public static class Processes
	{
		#region popen, pclose

		private sealed class ProcessWrapper : StreamWrapper
		{
			public Process/*!*/ process;

			public ProcessWrapper(Process/*!*/ process)
			{
				this.process = process;
			}

			public override bool IsUrl { get { return false; } }
			public override string Label { get { return null; } }
			public override string Scheme { get { return null; } }

			public override PhpStream Open(ref string path, string mode, StreamOpenOptions options, StreamContext context)
			{
				return null;
			}
		}

		/// <summary>
		/// Starts a process and creates a pipe to its standard input or output.
		/// </summary>
		/// <param name="command">The command.</param>
		/// <param name="mode">Pipe open mode (<c>"r"</c> or <c>"w"</c>).</param>
		/// <returns>Opened pipe or <B>null</B> on error.</returns>
		[ImplementsFunction("popen")]
		public static PhpResource OpenPipe(string command, string mode)
		{
			if (String.IsNullOrEmpty(mode))
			{
				PhpException.Throw(PhpError.Warning, CoreResources.GetString("invalid_file_mode", mode));
				return null;
			}

			bool read = mode[0] == 'r';
			bool write = mode[0] == 'w' || mode[0] == 'a' || mode[0] == 'x';

			if (!read && !write)
			{
				PhpException.Throw(PhpError.Warning, CoreResources.GetString("invalid_file_mode", mode));
				return null;
			}

			Process process = CreateProcessExecutingCommand(ref command, false);
			if (process == null) return null;

			process.StartInfo.RedirectStandardOutput = read;
			process.StartInfo.RedirectStandardInput = write;

			if (!StartProcess(process, true))
				return null;

			Stream stream = (read) ? process.StandardOutput.BaseStream : process.StandardInput.BaseStream;
			StreamAccessOptions access = (read) ? StreamAccessOptions.Read : StreamAccessOptions.Write;
			ProcessWrapper wrapper = new ProcessWrapper(process);
			PhpStream php_stream = new NativeStream(stream, wrapper, access, String.Empty, StreamContext.Default);

			return php_stream;
		}

		/// <summary>
		/// Closes a pipe and a process opened by <see cref="OpenPipe"/>.
		/// </summary>
		/// <param name="pipeHandle">The pipe handle returned by <see cref="OpenPipe"/>.</param>
		/// <returns>An exit code of the process.</returns>
		[ImplementsFunction("pclose")]
		public static int ClosePipe(PhpResource pipeHandle)
		{
			PhpStream php_stream = PhpStream.GetValid(pipeHandle);
			if (php_stream == null) return -1;

			ProcessWrapper wrapper = php_stream.Wrapper as ProcessWrapper;
			if (wrapper == null) return -1;

			var code = CloseProcess(wrapper.process);
            php_stream.Close();
            return code;
		}

		#endregion

		#region proc_open

		/// <summary>
		/// Opens a process.
		/// </summary>
		[ImplementsFunction("proc_open")]
		public static PhpResource Open(string command, PhpArray descriptorSpec, out PhpArray pipes)
		{
			return Open(command, descriptorSpec, out pipes, null, null, null);
		}

		/// <summary>
		/// Opens a process.
		/// </summary>
		[ImplementsFunction("proc_open")]
		public static PhpResource Open(string command, PhpArray descriptorSpec, out PhpArray pipes,
		  string workingDirectory)
		{
			return Open(command, descriptorSpec, out pipes, workingDirectory, null, null);
		}

		/// <summary>
		/// Opens a process.
		/// </summary>
		[ImplementsFunction("proc_open")]
		public static PhpResource Open(string command, PhpArray descriptorSpec, out PhpArray pipes,
		  string workingDirectory, PhpArray envVariables)
		{
			return Open(command, descriptorSpec, out pipes, workingDirectory, envVariables, null);
		}

		/// <summary>
		/// Starts a process and otpionally redirects its input/output/error streams to specified PHP streams.
		/// </summary>
		/// <param name="command"></param>
		/// <param name="descriptors"></param>
		/// Indexed array where the key represents the descriptor number (0 for STDIN, 1 for STDOUT, 2 for STDERR)
		/// and the value represents how to pass that descriptor to the child process. 
		/// A descriptor is either an opened file resources or an integer indexed arrays 
		/// containing descriptor name followed by options. Supported descriptors:
		/// <list type="bullet">
		/// <term><c>array("pipe",{mode})</c></term><description>Pipe is opened in the specified mode .</description>
		/// <term><c>array("file",{path},{mode})</c></term><description>The file is opened in the specified mode.</description>
		/// </list>
		/// <param name="pipes">
		/// Set to indexed array of file resources corresponding to the current process's ends of created pipes.
		/// </param>
		/// <param name="workingDirectory">
		/// Working directory.
		/// </param>
		/// <param name="envVariables"></param>
		/// <param name="options">
		/// Associative array containing following key-value pairs.
		///   <list type="bullet">
		///     <term>"suppress_errors"</term><description></description>
		///   </list>
		/// </param>
		/// <returns>
		/// Resource representing the process.
		/// </returns>
		[ImplementsFunction("proc_open")]
		public static PhpResource Open(string command, PhpArray descriptors, out PhpArray pipes,
		  string workingDirectory, PhpArray envVariables, PhpArray options)
		{
			if (descriptors == null)
			{
				PhpException.ArgumentNull("descriptors");
				pipes = null;
				return null;
			}

			pipes = new PhpArray();
			PhpResource result = Open(command, descriptors, pipes, workingDirectory, envVariables, options);
			return result;
		}

		/// <summary>
		/// Opens a process.
		/// </summary>
		private static PhpResource Open(string command, PhpArray/*!*/ descriptors, PhpArray/*!*/ pipes,
          string workingDirectory, PhpArray envVariables, PhpArray options)
		{
			if (descriptors == null)
				throw new ArgumentNullException("descriptors");
			if (pipes == null)
				throw new ArgumentNullException("pipes");

            bool bypass_shell = options != null && Core.Convert.ObjectToBoolean(options["bypass_shell"]);   // quiet

            Process process = CreateProcessExecutingCommand(ref command, bypass_shell);
			if (process == null)
				return null;

			if (!SetupStreams(process, descriptors))
				return null;

			if (envVariables != null)
				SetupEnvironment(process, envVariables);

			if (workingDirectory != null)
				process.StartInfo.WorkingDirectory = workingDirectory;

			bool suppress_errors = false;

			if (options != null)
			{
				suppress_errors = Core.Convert.ObjectToBoolean(options["suppress_errors"]);
			}

			if (!StartProcess(process, !suppress_errors))
				return null;

			if (!RedirectStreams(process, descriptors, pipes))
				return null;

			return new PhpProcessHandle(process, command);
		}

        private const string CommandLineSplitterPattern = @"(?<filename>^""[^""]*""|\S*) *(?<arguments>.*)?";
        private static readonly System.Text.RegularExpressions.Regex/*!*/CommandLineSplitter = new System.Text.RegularExpressions.Regex(CommandLineSplitterPattern, System.Text.RegularExpressions.RegexOptions.Singleline);

        private static Process CreateProcessExecutingCommand(ref string command, bool bypass_shell)
		{
			if (!Execution.MakeCommandSafe(ref command))
				return null;

			Process process = new Process();

            if (bypass_shell)
            {
                var match = CommandLineSplitter.Match(command);
                if (match == null || !match.Success)
                {
                    PhpException.InvalidArgument("command");
                    return null;
                }
                
                process.StartInfo.FileName = match.Groups["filename"].Value;
                process.StartInfo.Arguments = match.Groups["arguments"].Value;
            }
            else
            {
                process.StartInfo.FileName = (Environment.OSVersion.Platform != PlatformID.Win32Windows) ? "cmd.exe" : "command.com";
                process.StartInfo.Arguments = "/c " + command;
            }
			process.StartInfo.UseShellExecute = false;
            process.StartInfo.WorkingDirectory = ScriptContext.CurrentContext.WorkingDirectory;

			return process;
		}

		private static bool StartProcess(Process/*!*/ process, bool reportError)
		{
			try
			{
				process.Start();
				return true;
			}
			catch (Exception e)
			{
				if (reportError)
					PhpException.Throw(PhpError.Warning, LibResources.GetString("error_starting_process", e.Message));
				return false;
			}
		}

		private static void SetupEnvironment(Process/*!*/ process, IDictionary/*!*/ envVariables)
		{
			foreach (DictionaryEntry entry in envVariables)
			{
				string s = entry.Key as string;
				if (s != null)
					process.StartInfo.EnvironmentVariables.Add(s, Core.Convert.ObjectToString(entry.Value));
			}
		}

		private static bool SetupStreams(Process/*!*/ process, IDictionary/*!*/ descriptors)
		{
			foreach (DictionaryEntry entry in descriptors)
			{
				// key must be an integer:
				if (!(entry.Key is int))
				{
					PhpException.Throw(PhpError.Warning, LibResources.GetString("argument_not_integer_indexed_array", "descriptors"));
					return false;
				}

				int desc_no = (int)entry.Key;

				switch (desc_no)
				{
					case 0: process.StartInfo.RedirectStandardInput = true; break;
					case 1: process.StartInfo.RedirectStandardOutput = true; break;
					case 2: process.StartInfo.RedirectStandardError = true; break;
					default:
						PhpException.Throw(PhpError.Warning, LibResources.GetString("descriptor_unsupported", desc_no));
						return false;
				}
			}
			return true;
		}

        private static bool RedirectStreams(Process/*!*/ process, PhpArray/*!*/ descriptors, PhpArray/*!*/ pipes)
		{
			using (var descriptors_enum = descriptors.GetFastEnumerator())
            while (descriptors_enum.MoveNext())
			{
                int desc_no = descriptors_enum.CurrentKey.Integer;

				StreamAccessOptions access;
				Stream stream;
				switch (desc_no)
				{
					case 0: stream = process.StandardInput.BaseStream; access = StreamAccessOptions.Write; break;
					case 1: stream = process.StandardOutput.BaseStream; access = StreamAccessOptions.Read; break;
					case 2: stream = process.StandardError.BaseStream; access = StreamAccessOptions.Read; break;
					default: Debug.Fail(); return false;
				}

                object value = PhpVariable.Dereference(descriptors_enum.CurrentValue);
                PhpResource resource;
				PhpArray array;
                
                if ((array = PhpArray.AsPhpArray(value)) != null)
				{
					if (!array.Contains(0))
					{
						// value must be either a resource or an array:
						PhpException.Throw(PhpError.Warning, LibResources.GetString("descriptor_item_missing_qualifier", desc_no));
						return false;
					}

					string qualifier = Core.Convert.ObjectToString(array[0]);

					switch (qualifier)
					{
						case "pipe":
							{
								// mode is ignored (it's determined by the stream):
								PhpStream php_stream = new NativeStream(stream, null, access, String.Empty, StreamContext.Default);
								pipes.Add(desc_no, php_stream);
								break;
							}

						case "file":
							{
								if (!array.Contains(1))
								{
									PhpException.Throw(PhpError.Warning, LibResources.GetString("descriptor_item_missing_file_name", desc_no));
									return false;
								}

								if (!array.Contains(2))
								{
									PhpException.Throw(PhpError.Warning, LibResources.GetString("descriptor_item_missing_mode", desc_no));
									return false;
								}

								string path = Core.Convert.ObjectToString(array[1]);
								string mode = Core.Convert.ObjectToString(array[2]);

								PhpStream php_stream = PhpStream.Open(path, mode, StreamOpenOptions.Empty, StreamContext.Default);
								if (php_stream == null)
									return false;

								if (!ActivePipe.BeginIO(stream, php_stream, access, desc_no)) return false;
								break;
							}

						default:
							PhpException.Throw(PhpError.Warning, LibResources.GetString("invalid_handle_qualifier", qualifier));
							return false;
					}
				}
                else if ((resource = value as PhpResource) != null)
				{
					PhpStream php_stream = PhpStream.GetValid(resource);
					if (php_stream == null) return false;

					if (!ActivePipe.BeginIO(stream, php_stream, access, desc_no)) return false;
				}
				else
				{
					// value must be either a resource or an array:
					PhpException.Throw(PhpError.Warning, LibResources.GetString("descriptor_item_not_array_nor_resource", desc_no));
					return false;
				}
			}

			return true;
		}

		private sealed class ActivePipe
		{
			private const int BufferSize = 1024;

			Stream stream;
			StreamAccessOptions access;
			PhpStream phpStream;
			public AsyncCallback callback;
			public PhpBytes buffer;

			public static bool BeginIO(Stream stream, PhpStream phpStream, StreamAccessOptions access, int desc_no)
			{
				if (access == StreamAccessOptions.Read && !phpStream.CanWrite ||
				  access == StreamAccessOptions.Write && !phpStream.CanRead)
				{
					PhpException.Throw(PhpError.Warning, LibResources.GetString("descriptor_item_invalid_mode", desc_no));
					return false;
				}

				ActivePipe pipe = new ActivePipe();
				pipe.stream = stream;
				pipe.phpStream = phpStream;
				pipe.access = access;
				pipe.callback = new AsyncCallback(pipe.Callback);

				if (access == StreamAccessOptions.Read)
				{
                    var buffer = new byte[BufferSize];
                    stream.BeginRead(buffer, 0, buffer.Length, pipe.callback, null);
                    pipe.buffer = new PhpBytes(buffer);
				}
				else
				{
					pipe.buffer = phpStream.ReadBytes(BufferSize);
					if (pipe.buffer != null)
						stream.BeginWrite(pipe.buffer.ReadonlyData, 0, pipe.buffer.Length, pipe.callback, null);
					else
						stream.Close();
				}

				return true;
			}

			private void Callback(IAsyncResult ar)
			{
				if (access == StreamAccessOptions.Read)
				{
					int count = stream.EndRead(ar);
					if (count > 0)
					{
						if (count != buffer.Length)
						{
							// TODO: improve streams
							var buf = new byte[count];
							Buffer.BlockCopy(buffer.ReadonlyData, 0, buf, 0, count);
							phpStream.WriteBytes(new PhpBytes(buf));
						}
						else
						{
							phpStream.WriteBytes(buffer);
						}

						stream.BeginRead(buffer.Data, 0, buffer.Length, callback, ar.AsyncState);
					}
					else
					{
						stream.Close();
					}
				}
				else
				{
					buffer = phpStream.ReadBytes(BufferSize);
					if (buffer != null)
					{
                        stream.BeginWrite(buffer.ReadonlyData, 0, buffer.Length, callback, ar.AsyncState);
					}
					else
					{
						stream.EndWrite(ar);
						stream.Close();
					}
				}
			}
		}

		#endregion

		#region proc_close, proc_get_status, proc_terminate

		[ImplementsFunction("proc_close")]
		public static int Close(PhpResource process)
		{
			PhpProcessHandle handle = PhpProcessHandle.Validate(process);
			if (handle == null) return -1;

            var code = CloseProcess(handle.Process);
            handle.Close();
            return code;
		}

		private static int CloseProcess(Process/*!*/ process)
		{
			try
			{
				process.WaitForExit();
			}
			catch (Exception e)
			{
				PhpException.Throw(PhpError.Warning, LibResources.GetString("error_waiting_for_process_exit", e.Message));
				return -1;
			}

			return process.ExitCode;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="process"></param>
		/// <returns>
		/// <list type="bullet">
		/// <term>"command"</term><description>The command string that was passed to proc_open()</description> 
		/// <term>"pid"</term><description>process id</description>
		/// <term>"running"</term><description>TRUE if the process is still running, FALSE if it has terminated</description>  
		/// <term>"signaled"</term><description>TRUE if the child process has been terminated by an uncaught signal. Always set to FALSE on Windows.</description>
		/// <term>"stopped"</term><description>TRUE if the child process has been stopped by a signal. Always set to FALSE on Windows.</description>  
		/// <term>"exitcode"</term><description>the exit code returned by the process (which is only meaningful if running is FALSE)</description>  
		/// <term>"termsig"</term><description>the number of the signal that caused the child process to terminate its execution (only meaningful if signaled is TRUE)</description>  
		/// <term>"stopsig"</term><description>the number of the signal that caused the child process to stop its execution (only meaningful if stopped is TRUE)</description>  
		/// </list>
		/// </returns>
		[ImplementsFunction("proc_get_status")]
		public static PhpArray GetStatus(PhpResource process)
		{
			PhpProcessHandle handle = PhpProcessHandle.Validate(process);
			if (handle == null) return null;

			PhpArray result = new PhpArray(0, 8);

			result.Add("command", handle.Command);
			result.Add("pid", handle.Process.Id);
			result.Add("running", !handle.Process.HasExited);
			result.Add("signaled", false); // UNIX
			result.Add("stopped", false);  // UNIX
			result.Add("exitcode", handle.Process.HasExited ? handle.Process.ExitCode : -1);
			result.Add("termsig", 0);      // UNIX
			result.Add("stopsig", 0);      // UNIX

			return result;
		}

		[ImplementsFunction("proc_terminate")]
		public static int Terminate(PhpResource process)
		{
			return Terminate(process, 255);
		}

		[ImplementsFunction("proc_terminate")]
		public static int Terminate(PhpResource process, int signal)
		{
			PhpProcessHandle handle = PhpProcessHandle.Validate(process);
			if (handle == null) return -1;

			try
			{
				handle.Process.Kill();
			}
			catch (Exception e)
			{
				PhpException.Throw(PhpError.Warning, LibResources.GetString("error_terminating_process",
				  handle.Process.ProcessName, handle.Process.Id, e.Message));
				return -1;
			}
			return handle.Process.ExitCode;
		}

		#endregion

		#region NS: proc_nice

		[ImplementsFunction("proc_nice", FunctionImplOptions.NotSupported)]
		public static bool SetPriority(int priority)
		{
			PhpException.FunctionNotSupported();    // even in PHP for Windows, it is not available
			return false;
		}

		#endregion
	}

}
