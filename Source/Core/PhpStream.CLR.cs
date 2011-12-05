/*

 Copyright (c) 2004-2005 Jan Benda.
  
 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.IO;

namespace PHP.Core
{
	#region PhpStream (partial) class

	/// <summary>
	/// Abstraction of streaming behavior for PHP.
	/// PhpStreams are opened by StreamWrappers on a call to fopen().
	/// </summary>
	public abstract partial class PhpStream : PhpResource
	{
		#region Stat (optional)
		/// <include file='Doc/Streams.xml' path='docs/method[@name="Stat"]/*'/>
		public virtual StatStruct Stat()
		{
			if (this.Wrapper != null)
			{
				return this.Wrapper.Stat(OpenedPath, StreamStatOptions.Empty, StreamContext.Default, true);
			}

			PhpException.Throw(PhpError.Warning, CoreResources.GetString("wrapper_op_unsupported", "Stat"));
			StatStruct rv = new StatStruct();
			rv.st_size = -1;
			return rv;
		}
		#endregion

		#region Opening utilities
		
		/// <summary>
		/// Merges the path with the current working directory
		/// to get a canonicalized absolute pathname representing the same file.
		/// </summary>
		/// <remarks>
		/// This method is an analogy of <c>main/safe_mode.c: php_checkuid</c>.
		/// Looks for the file in the <c>include_path</c> and checks for <c>open_basedir</c> restrictions.
		/// </remarks>
		/// <param name="path">An absolute or relative path to a file.</param>
		/// <param name="wrapper">The wrapper found for the specified file or <c>null</c> if the path resolution fails.</param>
		/// <param name="mode">The checking mode of the <see cref="CheckAccess"/> method (file, directory etc.).</param>
		/// <param name="options">Additional options for the <see cref="CheckAccess"/> method.</param>
		/// <returns><c>true</c> if all the resolution and checking passed without an error, <b>false</b> otherwise.</returns>
		/// <exception cref="PhpException">Security violation - when the target file 
		/// lays outside the tree defined by <c>open_basedir</c> configuration option.</exception>
		public static bool ResolvePath(ref string path, out StreamWrapper wrapper, CheckAccessMode mode, CheckAccessOptions options)
		{
			// Path will contain the absolute path without file:// or the complete URL; filename is the relative path.
			string filename, scheme = GetSchemeInternal(path, out filename);
			wrapper = StreamWrapper.GetWrapper(scheme, (StreamOptions)options);
			if (wrapper == null) return false;

			if (wrapper.IsUrl)
			{
				// Note: path contains the whole URL, filename the same without the scheme:// portion.
				// What to check more?
			}
			else if (scheme != "php")
			{
				try
				{
					// Filename contains the original path without the scheme:// portion, check for include path.
					bool isInclude = false;
					if ((options & CheckAccessOptions.UseIncludePath) > 0)
					{
						isInclude = CheckIncludePath(filename, ref path);
					}

					// Path will now contain an absolute path (either to an include or actual directory).
					if (!isInclude)
					{
						path = Path.GetFullPath(Path.Combine(ScriptContext.CurrentContext.WorkingDirectory, filename));
					}
				}
				catch (Exception)
				{
                    if ((options & CheckAccessOptions.Quiet) == 0)
					PhpException.Throw(PhpError.Warning, CoreResources.GetString("stream_filename_invalid",
						FileSystemUtils.StripPassword(path)));
					return false;
				}

				GlobalConfiguration global_config = Configuration.Global;

				// Note: extensions check open_basedir too -> double check..
				if (!global_config.SafeMode.IsPathAllowed(path))
				{
					if ((options & CheckAccessOptions.Quiet) == 0)
						PhpException.Throw(PhpError.Warning, CoreResources.GetString("open_basedir_effect",
							path, global_config.SafeMode.GetAllowedPathPrefixesJoin()));
					return false;
				}

				// Replace all '/' with '\'.
				// path = path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
                Debug.Assert(
                    path.IndexOf(Path.AltDirectorySeparatorChar) == -1 ||
                    (Path.AltDirectorySeparatorChar == Path.DirectorySeparatorChar),    // on Mono, so ignore it
                    string.Format("'{0}' should not contain '{1}' char.", path, Path.AltDirectorySeparatorChar));

				// The file wrapper expects an absolute path w/o the scheme, others expect the scheme://url.
				if (scheme != "file")
				{
					path = String.Format("{0}://{1}", scheme, path);
				}
			}

			return true;
		}

		/// <summary>
		/// Check if the path lays inside of the directory tree specified 
		/// by the <c>open_basedir</c> configuration option and return the resulting <paramref name="absolutePath"/>.
		/// </summary>
		/// <param name="relativePath">The filename to search for.</param>
		/// <param name="absolutePath">The combined absolute path (either in the working directory 
		/// or in an include path wherever it has been found first).</param>
		/// <returns><c>true</c> if the file was found in an include path.</returns>
		private static bool CheckIncludePath(string relativePath, ref string absolutePath)
		{
			// Note: If the absolutePath exists, it overtakse the include_path search.
			if (Path.IsPathRooted(relativePath)) return false;
			if (File.Exists(absolutePath)) return false;

			string paths = ScriptContext.CurrentContext.Config.FileSystem.IncludePaths;
			if (paths == null) return false;

			foreach (string s in paths.Split(new char[] { Path.PathSeparator }))
			{
				if ((s == null) || (s == string.Empty)) continue;
				string abs = Path.GetFullPath(Path.Combine(s, relativePath));
				if (File.Exists(abs))
				{
					absolutePath = abs;
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Performs all checks on a path passed to a PHP function.
		/// </summary>
		/// <remarks>
		/// <para>
		/// This method performs a check similar to <c>safe_mode.c: php_checkuid_ex()</c>
		/// together with <c>open_basedir</c> check.
		/// </para>
		/// <para>
		/// The <paramref name="filename"/> may be one of the following:
		/// <list type="bullet">
		/// <item>A relative path. The path is resolved regarding the <c>include_path</c> too if required
		/// and checking continues as in the next case.</item>
		/// <item>An absolute path. The file or directory is checked for existence and for access permissions<sup>1</sup>
		/// according to the given <paramref name="mode"/>.</item>
		/// </list>
		/// <sup>1</sup> Regarding the <c>open_basedir</c> configuration option. 
		/// File access permissions are checked at the time of file manipulation
		/// (opening, copying etc.).
		/// </para>
		/// </remarks>
		/// <param name="filename">A resolved path. Must be an absolute path to a local file.</param>
		/// <param name="mode">One of the <see cref="CheckAccessMode"/>.</param>
		/// <param name="options"><c>true</c> to suppress error messages.</param>
		/// <returns><c>true</c> if the function may continue with file access,
		/// <c>false</c>to fail.</returns>
		/// <exception cref="PhpException">If the file can not be accessed
		/// and the <see cref="CheckAccessOptions.Quiet"/> is not set.</exception>
		public static bool CheckAccess(string filename, CheckAccessMode mode, CheckAccessOptions options)
		{
			Debug.Assert(Path.IsPathRooted(filename));
			string url = FileSystemUtils.StripPassword(filename);
			bool quiet = (options & CheckAccessOptions.Quiet) > 0;

			switch (mode)
			{
				case CheckAccessMode.FileMayExist:
					break;

				case CheckAccessMode.FileExists:
					if (!File.Exists(filename))
					{
						if (!quiet) PhpException.Throw(PhpError.Warning, CoreResources.GetString("stream_file_not_exists", url));
						return false;
					}
					break;

				case CheckAccessMode.FileNotExists:
					if (File.Exists(filename))
					{
						if (!quiet) PhpException.Throw(PhpError.Warning, CoreResources.GetString("stream_file_exists", url));
						return false;
					}
					break;

				case CheckAccessMode.FileOrDirectory:
					if ((!Directory.Exists(filename)) && (!File.Exists(filename)))
					{
						if (!quiet) PhpException.Throw(PhpError.Warning, CoreResources.GetString("stream_path_not_exists", url));
						return false;
					}
					break;

				case CheckAccessMode.Directory:
					if (!Directory.Exists(filename))
					{
						if (!quiet) PhpException.Throw(PhpError.Warning, CoreResources.GetString("stream_directory_not_exists", url));
						return false;
					}
					break;

				default:
					Debug.Assert(false);
					return false;
			}

			return true;
		}

		#endregion
	}

	#endregion

	#region ExternalStream class
	/// <summary>
	/// Represents a native PHP stream that lives in <c>ExtManager</c>.
	/// </summary>
	public class ExternalStream : PhpStream
	{
		/// <summary>A MBRO proxy of the (remote) native stream.</summary>
		private IExternalStream proxy;

		/// <summary>
		/// Creates a new <see cref="ExternalStream"/> with the given proxy object.
		/// </summary>
		/// <param name="proxy">Instance of a class derived from <see cref="MarshalByRefObject"/> that should
		/// serve as a proxy for this <see cref="ExternalStream"/>.</param>
		/// <param name="openingWrapper">The parent instance.</param>
		/// <param name="accessOptions">The additional options parsed from the <c>fopen()</c> mode.</param>
		/// <param name="openedPath">The absolute path to the opened resource.</param>
		/// <param name="context">The stream context passed to fopen().</param>
		internal ExternalStream(IExternalStream proxy, StreamWrapper openingWrapper, StreamAccessOptions accessOptions, string openedPath, StreamContext context)
			: base(openingWrapper, accessOptions, openedPath, context)
		{
			this.proxy = proxy;
		}

		/// <include file='Doc/Streams.xml' path='docs/method[@name="RawRead"]/*'/>
		protected override int RawRead(byte[] buffer, int offset, int count)
		{
			byte[] new_buffer = buffer;
			int ret = proxy.Read(ref new_buffer, offset, count);

			// the copying is necessary if proxy.Read was a remote call
			if (!Object.ReferenceEquals(buffer, new_buffer))
			{
				new_buffer.CopyTo(buffer, 0);
			}

			return ret;
		}

		/// <include file='Doc/Streams.xml' path='docs/method[@name="RawWrite"]/*'/>
		protected override int RawWrite(byte[] buffer, int offset, int count)
		{
			return proxy.Write(buffer, offset, count);
		}

		/// <summary>
		/// Closes the stream.
		/// </summary>
		protected override void FreeManaged()
		{
			// Call the base implementation to flush the output buffers if any.
			base.FreeManaged();
			proxy.Close();
		}

		/// <include file='Doc/Streams.xml' path='docs/method[@name="RawFlush"]/*'/>
		protected override bool RawFlush()
		{
			return proxy.Flush();
		}

		/// <include file='Doc/Streams.xml' path='docs/property[@name="RawEof"]/*'/>
		protected override bool RawEof
		{
			get
			{
				return proxy.Eof();
			}
		}

		/// <summary>
		/// Returns the status array for the stram.
		/// </summary>
		/// <returns>A stat array describing the stream.</returns>
		public override StatStruct Stat()
		{
			return proxy.Stat();
		}

		#region Seeking
		/// <include file='Doc/Streams.xml' path='docs/property[@name="CanSeek"]/*'/>
		public override bool CanSeek
		{
			get
			{
				return true;
			}
		}


		/// <include file='Doc/Streams.xml' path='docs/method[@name="RawSeek"]/*'/>
		protected override bool RawSeek(int offset, SeekOrigin whence)
		{
			return proxy.Seek(offset, whence);
		}

		/// <summary>
		/// Gets the current position in the stream.
		/// </summary>
		/// <returns>The position or <c>-1</c> on error.</returns>
		protected override int RawTell()
		{
			return proxy.Tell();
		}
		#endregion
	}
	#endregion

	#region SocketStream class

	/// <summary>
	/// An implementation of <see cref="PhpStream"/> as an encapsulation 
	/// of System.Net.Socket transports.
	/// </summary>
	public class SocketStream : PhpStream
	{
		/// <summary>
		/// The encapsulated network socket.
		/// </summary>
		protected Socket socket;

		/// <summary>
		/// Result of the last read/write operation.
		/// </summary>
		protected bool eof;

		#region PhpStream overrides

		public SocketStream(Socket socket, string openedPath, StreamContext context)
			: base(null, StreamAccessOptions.Read | StreamAccessOptions.Write, openedPath, context)
		{
			Debug.Assert(socket != null);
			this.socket = socket;
			this.IsWriteBuffered = false;
			this.eof = false;
		}

		/// <summary>
		/// PhpResource.FreeManaged overridden to get rid of the contained context on Dispose.
		/// </summary>
		protected override void FreeManaged()
		{
			base.FreeManaged();
			socket.Close();
			socket = null;
		}

		#endregion

		#region Raw byte access (mandatory)

		/// <include file='Doc/Streams.xml' path='docs/method[@name="RawRead"]/*'/>
		protected override int RawRead(byte[] buffer, int offset, int count)
		{
			try
			{
				int rv = socket.Receive(buffer, offset, count, SocketFlags.None);
				eof = rv == 0;
				return rv;
			}
			catch (Exception e)
			{
				PhpException.Throw(PhpError.Warning, CoreResources.GetString("stream_socket_error", e.Message));
				return 0;
			}
		}

		/// <include file='Doc/Streams.xml' path='docs/method[@name="RawWrite"]/*'/>
		protected override int RawWrite(byte[] buffer, int offset, int count)
		{
			try
			{
				int rv = socket.Send(buffer, offset, count, SocketFlags.None);
				eof = rv == 0;
				return rv;
			}
			catch (Exception e)
			{
				PhpException.Throw(PhpError.Warning, CoreResources.GetString("stream_socket_error", e.Message));
				return 0;
			}
		}

		/// <include file='Doc/Streams.xml' path='docs/method[@name="RawFlush"]/*'/>
		protected override bool RawFlush()
		{
			return true;
		}

		/// <include file='Doc/Streams.xml' path='docs/property[@name="RawEof"]/*'/>
		protected override bool RawEof
		{
			get
			{
				return eof;
			}
		}

		public override bool SetParameter(StreamParameterOptions option, object value)
		{
			if (option == StreamParameterOptions.ReadTimeout)
			{
				int timeout = Convert.ObjectToInteger(value);
				socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, timeout);
				socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, timeout);
				return true;
			}
			return base.SetParameter(option, value);
		}

		#endregion

		#region Conversion to .NET native Stream (NS)

		/// <include file='Doc/Streams.xml' path='docs/property[@name="RawStream"]/*'/>
		public override Stream RawStream
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		#endregion

		new public static SocketStream GetValid(PhpResource handle)
		{
			SocketStream result = handle as SocketStream;
			if (result == null)
				PhpException.Throw(PhpError.Warning, CoreResources.GetString("invalid_socket_stream_resource"));
			return result;
		}
	}

	#endregion
}
