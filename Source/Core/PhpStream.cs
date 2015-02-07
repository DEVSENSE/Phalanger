/*

 Copyright (c) 2004-2005 Jan Benda.
  
 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.IO;
using System.Text;
using System.Net;
using System.Collections;
using System.Diagnostics;

#if SILVERLIGHT
using PHP.CoreCLR;
#endif

namespace PHP.Core
{
	#region Stream Context

	/// <summary>
	/// Resource type used for associating additional options with stream wrappers.
	/// </summary>
	/// <remarks>
	/// Stream Contexts are stored in a Resource to save useless deep-copying
	/// of the contained constant array.
	/// </remarks>
	public class StreamContext : PhpResource
	{
        /// <summary>
        /// Default StreamContext. Cannot be null.
        /// </summary>
		public static readonly StreamContext/*!*/Default = new StreamContext();

		#region Properties

		/// <summary>
		/// The contained context array (2D associative array: first wrapper, then options).
		/// </summary>
		public PhpArray Data
		{
			get { return data; }
			set { data = value; }
		}
		protected PhpArray data;

		/// <summary>
		/// The additional parameters (currently only a notification callback).
		/// </summary>
		public PhpArray Parameters
		{
			get { return this.parameters; }
			set { this.parameters = value; }
		}
		protected PhpArray parameters;

		/// <summary>
		/// The type name displayed when printing a variable of type StreamContext.
		/// </summary>
		public const string StreamContextTypeName = "stream-context";

		#endregion

		#region Constructors

		/// <summary>
		/// Create an empty StreamContext (allows lazy PhpArray instantiation).
		/// </summary>
		public StreamContext() : this(null) { }

		/// <summary>
		/// Create a new context resource from an array of wrapper options.
		/// </summary>
		/// <param name="data">A 2-dimensional array of wrapper options</param>
		public StreamContext(PhpArray data)
			: base(StreamContextTypeName)
		{
			this.data = data;
		}

		#endregion

        /// <summary>
		/// Checks the context for validity, throws a warning it is not.
		/// </summary>
		/// <param name="context">Resource which should contain a StreamContext.</param>
		/// <returns>The given resource cast to <see cref="StreamContext"/> or <c>null</c> if invalid.</returns>
		/// <exception cref="PhpException">In case the context is invalid.</exception>
		public static StreamContext GetValid(PhpResource context)
        {
            return GetValid(context, false);
        }

		/// <summary>
		/// Checks the context for validity, throws a warning it is not.
		/// </summary>
		/// <param name="context">Resource which should contain a StreamContext.</param>
        /// <param name="allowNull"><c>True</c> to allow <c>NULL</c> context, that will be without any warning converted to Default <see cref="StreamContext"/>.</param>
        /// <returns>The given resource cast to <see cref="StreamContext"/> or <c>null</c> if invalid and <c>allowNull</c> is <c>false</c>.</returns>
		/// <exception cref="PhpException">In case the context is invalid.</exception>
		public static StreamContext GetValid(PhpResource context, bool allowNull)
		{
            // implicit default from NULL
            if (allowNull && context == null)
                return StreamContext.Default;

            // try to cast to StreamContext
			StreamContext result = context as StreamContext;
			if (result != null /* TODO: Why is default context disposed? && result.IsValid*/) return result;

			PhpException.Throw(PhpError.Warning, CoreResources.GetString("invalid_context_resource"));
			return null;
		}

		/// <summary>
		/// Gets a wrapper-specific option identified by the scheme and the option name.
		/// </summary>
		/// <param name="scheme">The target wrapper scheme.</param>
		/// <param name="option">The option name.</param>
		/// <returns>The specific option or <b>null</b> if no such option exists.</returns>
		public object GetOption(string scheme, string option)
		{
			if ((data != null) && (data.ContainsKey(scheme)))
			{
				PhpArray options = data[scheme] as PhpArray;
				if ((options != null) && (options.ContainsKey(option)))
					return options[option];
			}
			return null;
		}
	}

	#endregion

	/// <summary>
	/// Shortcuts for the short overload of PhpStream.Open
	/// </summary>
	[Flags]
	public enum StreamOpenMode
	{
		/// <summary>Open for reading</summary>
		Read = ReadText,
		/// <summary>Open for writing</summary>
		Write = WriteText,
		/// <summary>Open for reading (text mode)</summary>
		ReadText = 0,
		/// <summary>Open for writing (text mode)</summary>
		WriteText = 1,
		/// <summary>Open for reading (binary mode)</summary>
		ReadBinary = 2,
		/// <summary>Open for writing (binary mode)</summary>
		WriteBinary = 3
	}

	#region PhpStream Class

	#region Enumerations (StreamParameterOptions, CheckAccessMode, CheckAccessOptions)
	/// <summary>
	/// Parameter identifier for <see cref="PhpStream.SetParameter"/>.
	/// </summary>
	public enum StreamParameterOptions
	{
		/// <summary>Set the synchronous/asynchronous operation mode (<c>value</c> is <see cref="bool"/>.</summary>
		BlockingMode = 1,
		/// <summary>Set the read buffer size (<c>value</c> is <see cref="int"/>).</summary>
		ReadBufferSize = 2,
		/// <summary>Set the write buffer size (<c>value</c> is <see cref="int"/>).</summary>
		WriteBufferSize = 3,
		/// <summary>Set the read timeout in seconds (<c>value</c> is <see cref="double"/>).</summary>
		ReadTimeout = 4,
		/// <summary>Set the read chunk size (<c>value</c> is <see cref="int"/>).</summary>
		SetChunkSize = 5,
		/// <summary>Set file locking (<c>value</c> is <see cref="int"/>).</summary>
		Locking = 6,
		/// <summary>Set memory mapping. Unimplemented.</summary>
		MemoryMap = 9,
		/// <summary>Truncate the stream at the current position.</summary>
		Truncate = 10
	}

	/// <summary>
	/// Mode selector of <see cref="PhpStream.CheckAccess"/>.
	/// </summary>
	public enum CheckAccessMode
	{
		/// <summary>Return invalid <c>false</c> if file does not exist (<c>fopen()</c>).</summary>
		FileExists = 0,
		/// <summary>Return valid <c>true</c> if file does not exist (for example <c>rename()</c>.</summary>
		FileNotExists = 1,
		/// <summary>If file does not exist, check directory (for example <c>stat()</c>).</summary>
		FileOrDirectory = 2,
		/// <summary>Only check directory (needed for <c>mkdir</c>, <c>opendir</c>).</summary>
		Directory = 3,
		/// <summary>Only check file.</summary>
		FileMayExist = 5
	}

	/// <summary>
	/// Additional options for <see cref="PhpStream.CheckAccess"/>.
	/// </summary>
	public enum CheckAccessOptions
	{
		/// <summary>Empty option (default).</summary>
		Empty = 0,
		/// <summary>If <c>true</c> then the include paths are searched for the file too (1).</summary>
		UseIncludePath = StreamOptions.UseIncludePath,
		/// <summary>Suppress display of error messages (2).</summary>
		Quiet = StreamStatOptions.Quiet
	}

	#endregion


	/// <summary>
	/// Abstraction of streaming behavior for PHP.
	/// PhpStreams are opened by StreamWrappers on a call to fopen().
	/// </summary>
	/// <remarks>
	/// <para>
	/// PhpStream is a descendant of PhpResource,
	/// it contains a StreamContext (may be empty) and two ordered lists of StreamFilters
	/// (input and output filters).
	/// PhpStream may be cast to a .NET stream (using its RawStream property).
	/// </para>
	/// <para>
	/// Various stream types are defined by overriding the <c>Raw*</c> methods
	/// that provide direct access to the underlying physical stream.
	/// Corresponding public methods encapsulate these accessors with
	/// buffering and filtering. Raw stream access is performed at the <c>byte[]</c> level.
	/// ClassLibrary functions may use either the <c>Read/WriteBytes</c>
	/// or <c>Read/WriteString</c> depending on the nature of the PHP function.
	/// Data are converted using the <see cref="ApplicationConfiguration.GlobalizationSection.PageEncoding"/>
	/// as necessary.
	/// </para>
	/// <para>
	/// When reading from a stream, the stream data is read in binary format
	/// in chunks of predefined size (8kB). Stream filters (if any) are then applied
	/// in a cascade to the whole block. Filtered blocks are stored in a
	/// <see cref="Queue"/> of either strings or PhpBytes depending on the last
	/// filter output (note that after filtering not all blocks have necessarily
	/// the original chunk size; when appending a filter to the filter-chain
	/// all the buffered data is passed through this one too). The input queue is being 
	/// filled until the required data length is available. The <see cref="readPosition"/> 
	/// property holds the index into the first chunk of data. When this chunk is 
	/// entirely consumed it is dequeued.
	/// </para>
	/// <para>
	/// Writing to a stream is buffered too (unless it is disabled using <c>stream_set_write_buffer</c>). 
	/// When the data passes through the filter-chain it is appended to the 
	/// write buffer (using the <see cref="writePosition"/> property). 
	/// When the write buffer is full it is flushed to the underlying stream.
	/// </para>
	/// </remarks>
	public abstract partial class PhpStream : PhpResource
	{
		#region PhpStream Opening

		/// <summary>
		/// Simple version of the stream opening function
		/// </summary>
		/// <param name="path">URI or filename of the resource to be opened</param>
		/// <param name="mode">File access mode</param>
		/// <returns></returns>
		internal static PhpStream Open(string path, StreamOpenMode mode)
		{
			string modeStr = null;
			switch (mode)
			{
				case StreamOpenMode.ReadBinary: modeStr = "rb"; break;
				case StreamOpenMode.WriteBinary: modeStr = "wb"; break;
				case StreamOpenMode.ReadText: modeStr = "rt"; break;
				case StreamOpenMode.WriteText: modeStr = "wt"; break;
			}
			Debug.Assert(modeStr != null);
			return Open(path, modeStr, StreamOpenOptions.Empty, StreamContext.Default);
		}


		public static PhpStream Open(string path, string mode)
		{
			return Open(path, mode, StreamOpenOptions.Empty, StreamContext.Default);
		}

		public static PhpStream Open(string path, string mode, StreamOpenOptions options)
		{
			return Open(path, mode, options, StreamContext.Default);
		}


		/// <summary>
		/// Checks if the given path is a filesystem path or an URL and returns the corresponding scheme.
		/// </summary>
		/// <param name="path">The path to be canonicalized.</param>
		/// <param name="filename">The filesystem path before canonicalization (may be both relative or absolute).</param>
		/// <returns>The protocol portion of the given URL or <c>"file"</c>o for local files.</returns>
		internal static string GetSchemeInternal(string path, out string filename)
		{
			int colon_index = path.IndexOf(':');
			if (colon_index == -1)
			{
				// No scheme, no root directory, it's a relative path.
				filename = path;
				return "file";
			}

			if (Path.IsPathRooted(path))
			{
				// It already is an absolute path.
				filename = path;
				return "file";
			}

			if (path.Length < colon_index + 3 || path[colon_index + 1] != '/' || path[colon_index + 2] != '/')
			{
				// There is no "//" following the colon.
				filename = path;
				return "file";
			}

			// Otherwise it is an URL (including file://), set the filename and return the scheme.
			filename = path.Substring(colon_index + "://".Length);
			return path.Substring(0, colon_index);
		}


		/// <summary>
		/// Openes a PhpStream using the appropriate StreamWrapper.
		/// </summary>
		/// <param name="path">URI or filename of the resource to be opened.</param>
		/// <param name="mode">A file-access mode as passed to the PHP function.</param>
		/// <param name="options">A combination of <see cref="StreamOpenOptions"/>.</param>
		/// <param name="context">A valid StreamContext. Must not be <c>null</c>.</param>
		/// <returns></returns>
		public static PhpStream Open(string path, string mode, StreamOpenOptions options, StreamContext context)
		{
			if (context == null)
				throw new ArgumentNullException("context");

			StreamWrapper wrapper;
			if (!PhpStream.ResolvePath(ref path, out wrapper, CheckAccessMode.FileMayExist, (CheckAccessOptions)options))
				return null;

			return wrapper.Open(ref path, mode, options, context);

		}

		/// <summary>
		/// Flushes the stream before closing.
		/// </summary>
		~PhpStream()
		{
			if (!this.IsPersistent) this.Flush();
		}

		#endregion

		#region PhpResource override methods
		/// <summary>
		/// PhpStream is created by a StreamWrapper together with the
		/// encapsulated RawStream (the actual file opening is handled 
		/// by the wrapper).
		/// </summary>
		/// <remarks>
		/// This class newly implements the auto-remove behavior too
		/// (see <see cref="StreamAccessOptions.Temporary"/>).
		/// </remarks>
		/// <param name="openingWrapper">The parent instance.</param>
		/// <param name="accessOptions">The additional options parsed from the <c>fopen()</c> mode.</param>
		/// <param name="openedPath">The absolute path to the opened resource.</param>
		/// <param name="context">The stream context passed to fopen().</param>
		public PhpStream(StreamWrapper openingWrapper, StreamAccessOptions accessOptions, string openedPath, StreamContext context)
			: base(PhpStreamTypeName)
		{
			Debug.Assert(context != null);

			this.context = context;
			this.Wrapper = openingWrapper;
			this.OpenedPath = openedPath;

			// Stream modifiers (defined in open-time).
			this.Options = accessOptions;

			// Allocate the text conversion filters for this stream.
			if ((accessOptions & StreamAccessOptions.UseText) > 0)
			{
				if ((accessOptions & StreamAccessOptions.Read) > 0)
				{
					textReadFilter = new TextReadFilter();
				}
				if ((accessOptions & StreamAccessOptions.Write) > 0)
				{
					textWriteFilter = new TextWriteFilter();
				}
			}

			this.readTimeout = ScriptContext.CurrentContext.Config.FileSystem.DefaultSocketTimeout;
		}

		/// <summary>
		/// PhpResource.FreeManaged overridden to get rid of the contained context on Dispose.
		/// </summary>
		protected override void FreeManaged()
		{
			// Flush the underlying stream before closing.
			if ((writeFilters != null) && (writeFilters.Count > 0))
			{
				// Pass an empty data with closing == true through all the filters.
				WriteData(PhpBytes.Empty, true);
			}
			Flush();


			if (context != null)
			{
				context.Close();
				context = null;
			}

            writeBuffer = null; // http://phalanger.codeplex.com/workitem/31272
						
			base.FreeManaged();
		}

		/// <summary>
		/// PhpResource.FreeUnmanaged overridden to remove a temporary file on Dispose.
		/// </summary>
		protected override void FreeUnmanaged()
		{
			// Note: this method is called after FreeManaged, so the stream is already closed.
			base.FreeUnmanaged();
			if (this.IsTemporary)
			{
				try
				{
					this.Wrapper.Unlink(OpenedPath, StreamUnlinkOptions.Empty, StreamContext.Default);
					// File.Delete(this.OpenedPath);
				}
				catch (Exception)
				{
				}
			}
		}
		#endregion

		#region Raw byte access (mandatory)
		/// <include file='Doc/Streams.xml' path='docs/method[@name="RawRead"]/*'/>
		protected abstract int RawRead(byte[] buffer, int offset, int count);

		/// <include file='Doc/Streams.xml' path='docs/method[@name="RawWrite"]/*'/>
		protected abstract int RawWrite(byte[] buffer, int offset, int count);

		/// <include file='Doc/Streams.xml' path='docs/method[@name="RawFlush"]/*'/>
		protected abstract bool RawFlush();

		/// <include file='Doc/Streams.xml' path='docs/property[@name="RawEof"]/*'/>
		protected abstract bool RawEof { get; }

		#endregion

		#region Seeking (optional)
		/// <include file='Doc/Streams.xml' path='docs/property[@name="CanSeek"]/*'/>
		public virtual bool CanSeek { get { return false; } }

		/// <include file='Doc/Streams.xml' path='docs/method[@name="RawTell"]/*'/>
		protected virtual int RawTell()
		{
            PhpException.Throw(PhpError.Warning, CoreResources.GetString("wrapper_op_unsupported", "Seek"));
			return -1;
		}

		/// <include file='Doc/Streams.xml' path='docs/method[@name="RawSeek"]/*'/>
		protected virtual bool RawSeek(int offset, SeekOrigin whence)
		{
			PhpException.Throw(PhpError.Warning, CoreResources.GetString("wrapper_op_unsupported", "Seek"));
			return false;
		}

		/// <summary>
		/// Gets the length of the stream.
		/// </summary>
		/// <returns>Count of bytes in the stream or <c>-1</c> if seek is not supported.</returns>
		protected virtual int RawLength()
		{
			if (!CanSeek) return -1;
			int current = RawTell();
			if ((current < 0) || !RawSeek(0, SeekOrigin.End)) return -1;
			int rv = RawTell();
			if ((rv < 0) || !RawSeek(current, SeekOrigin.Begin)) return -1;
			return rv;
		}
		#endregion

		#region SetParameter (optional)
		/// <include file='Doc/Streams.xml' path='docs/method[@name="SetParameter"]/*'/>
		public virtual bool SetParameter(StreamParameterOptions option, object value)
		{
			// Do not display error messages here, the caller will.
			// EX: will have to distinguish between failed and unsupported.
			// (use additional message when fails)

			// Descendants may call this default implementation for unhandled options
			switch (option)
			{
				case StreamParameterOptions.BlockingMode:
					// Unimplemented in Win32 PHP.
					return false;

				case StreamParameterOptions.ReadBufferSize:
					// Unused option (only turns buffering off)
					return false;

				case StreamParameterOptions.WriteBufferSize:
					if (value is int)
					{
						// Let the write buffer reset on next write operation.
						FlushWriteBuffer();
						writeBuffer = null;
						// Set the new size (0 to disable write buffering).
						writeBufferSize = (int)value;
						if (writeBufferSize < 0) writeBufferSize = 0;
						return true;
					}
					return false;

				case StreamParameterOptions.ReadTimeout:
					// Set the read timeout for network-based streams (overrides DefaultTimeout).
					this.readTimeout = (double)value;
					return false;

				case StreamParameterOptions.SetChunkSize:
					if (value is int)
					{
						// This setting will affect reading after the buffers are emptied.
						readChunkSize = (int)value;
						if (readChunkSize < 1) readChunkSize = 1;
						return true;
					}
					return false;

				case StreamParameterOptions.Locking:
					return false;

				case StreamParameterOptions.MemoryMap:
					return false;

				case StreamParameterOptions.Truncate:
					// EX: [Truncate] Override SetParameter in NativeStream to truncate a local file.
					return false;

				default:
					Debug.Assert(false); // invalid option
					return false;
			}
		}
		#endregion

		#region High-level Stream Access (Buffering and Filtering)

		#region High-level Reading
		/// <include file='Doc/Streams.xml' path='docs/property[@name="RawEof"]/*'/>
		public bool Eof
		{
			get
			{
                // The raw stream reached EOF and all the data is processed.
				if (RawEof)
				{
					// Check the buffers as quickly as possible.
					if ((readBuffers == null) || (readBuffers.Count == 0)) return true;

					// There is at least one buffer, check position.
					int firstLength = GetDataLength(readBuffers.Peek());
					if (firstLength > readPosition) return false;

					if (ReadBufferLength == 0) return true;
				}
				return false;
			}
		}

		#region Buffered Reading

		private int ReadBufferScan(out int nlpos)
		{
			int total = 0;
			nlpos = -1;
			if (readBuffers == null) return 0;

			// Yields to 0 for empty readBuffers.
			foreach (object o in readBuffers)
			{
				string str = o as String;
				PhpBytes bin = o as PhpBytes;

				int read = 0;
				if (str != null) read = str.Length;
				else if (bin != null) read = bin.Length;
				else Debug.Assert(false);

				if ((nlpos == -1) && (total <= readPosition) && (total + read > readPosition))
				{
					// Find the first occurence of \n.
					nlpos = total + FindEoln(o, readPosition - total);
				}

				total += read;
			}

			// Substract the count of data already processed.
			total -= readPosition;
			return total;
		}

		/// <summary>
		/// Gets the number of <c>byte</c>s or <c>char</c>s available
		/// in the <see cref="readBuffers"/>.
		/// </summary>
		protected int ReadBufferLength
		{
			get
			{
				int nlpos;
				return ReadBufferScan(out nlpos);
			}
		}


		/// <summary>
		/// Fills the <see cref="readBuffers"/> with more data from the underlying stream
		/// passed through all the stream filters. 
		/// </summary>
		/// <param name="chunkSize">Maximum number of bytes to be read from the stream.</param>
		/// <returns>A <see cref="string"/> or <see cref="PhpBytes"/> containing the 
		/// data as returned from the last stream filter or <b>null</b> in case of an error or <c>EOF</c>.</returns>
		protected object ReadFiltered(int chunkSize)
		{
			byte[] chunk = new byte[chunkSize];
			object filtered = null;

			while (filtered == null)
			{
				// Read data until there is an output or error or EOF.
				if (RawEof) return null;
				int read = RawRead(chunk, 0, chunkSize);
				if (read <= 0)
				{
					// Error or EOF.
					return null;
				}

				if (read < chunkSize)
				{
					byte[] sub = new byte[read];
					Array.Copy(chunk, 0, sub, 0, read);
					chunk = sub;
				}
				filtered = new PhpBytes(chunk);

				bool closing = RawEof;

				if (textReadFilter != null)
				{
					// First use the text-input filter if any.
					filtered = textReadFilter.Filter(filtered, closing);
				}

				if (readFilters != null)
				{
					// After that apply the user-filters.
					foreach (IFilter f in readFilters)
					{
						if (filtered == null)
						{
							// This is the last chance to output something. Give chance to all filters.
							if (closing) filtered = PhpBytes.Empty;
							else break; // Continue with next RawRead()
						}
						filtered = f.Filter(filtered, closing);
					} // foreach
				} // if
			} // while 

			return filtered;
		}

		/// <summary>
		/// Put a buffer at the end of the <see cref="readBuffers"/>.
		/// </summary>
		/// <param name="data">The buffer to append.</param>
		internal void EnqueueReadBuffer(object data)
		{
			Debug.Assert((data is string) || (data is PhpBytes));

			// This may be the first access to the buffers.
			if (readBuffers == null)
				readBuffers = new Queue(2);

			// Append the filtered output to the buffers.
			readBuffers.Enqueue(data);
		}


		/// <summary>
		/// Remove the (entirely consumed) read buffer from the head of the read buffer queue.
		/// </summary>
		/// <returns><c>true</c> if there are more buffers in the queue.</returns>
		protected bool DropReadBuffer()
		{
			Debug.Assert(readBuffers != null);
			Debug.Assert(readBuffers.Count > 0);

			object data = readBuffers.Dequeue();
			int length = GetDataLength(data);
			Debug.Assert(length > 0);

			// Add the new offset to the total one.
			readOffset += length;

			readPosition = 0;
			return readBuffers.Count > 0;
		}


		/// <summary>
		/// Joins the read buffers to get at least <paramref name="length"/> characters
		/// in a <see cref="string"/>. 
		/// </summary>
		/// <remarks>
		/// It is assumed that there already is length bytes in the buffers.
		/// Otherwise an InvalidOperationException is raised.
		/// </remarks>
		/// <param name="length">The desired maximum result length.</param>
		/// <returns>A <see cref="string"/> dequeued from the buffer or <c>null</c> if the buffer is empty.</returns>
		/// <exception cref="InvalidOperationException">If the buffers don't contain enough data.</exception>
		protected string ReadTextBuffer(int length)
		{
			if (length == 0) return string.Empty;

			string peek = readBuffers.Peek() as string;
			if (peek == null) throw new InvalidOperationException(CoreResources.GetString("buffers_must_not_be_empty"));
			Debug.Assert(peek.Length >= readPosition);

			if (peek.Length - readPosition >= length)
			{
				// Great! We can just take a substring.
				string res = peek.Substring(readPosition, length);
				readPosition += length;

				if (peek.Length == readPosition)
				{
					// We just consumed the entire string. Dequeue it.
					DropReadBuffer();
				}
				return res;
			}
			else
			{
				// Start building the string from the remainder in the buffer.
				StringBuilder sb = new StringBuilder(peek, readPosition, peek.Length - readPosition, length);
				length -= peek.Length - readPosition;

				// We just consumed the entire string. Dequeue it.
				DropReadBuffer();

				while (length > 0)
				{
					peek = readBuffers.Peek() as string;
					if (peek == null) throw new InvalidOperationException(CoreResources.GetString("too_little_data_buffered"));
					if (peek.Length > length)
					{
						// This string is long enough. It is the last one.
						sb.Append(peek, 0, length);
						readPosition = length;
						length = 0;
						break;
					}
					else
					{
						// Append just another whole buffer to the StringBuilder.
						sb.Append(peek);
						length -= peek.Length;
						DropReadBuffer();

						// When this is the last buffer (it's probably an EOF), return.
						if (readBuffers.Count == 0)
							break;
					}
				} // while

				Debug.Assert(sb.Length > 0);
				return sb.ToString();
			} // else
		}


		/// <summary>
		/// Joins the read buffers to get at least <paramref name="length"/> bytes
		/// in a <see cref="PhpBytes"/>. 
		/// </summary>
		/// <param name="length">The desired maximum result length.</param>
		/// <returns>A <see cref="PhpBytes"/> dequeued from the buffer or <c>null</c> if the buffer is empty.</returns>
		protected PhpBytes ReadBinaryBuffer(int length)
		{
			if (length == 0) return PhpBytes.Empty;

			PhpBytes peek = (PhpBytes)readBuffers.Peek();
			Debug.Assert(peek.Length >= readPosition);

			if (peek.Length - readPosition >= length)
			{
				// Great! We can just take a sub-data.
				byte[] data = new byte[length];
                Array.Copy(peek.ReadonlyData, readPosition, data, 0, length);
				PhpBytes res = new PhpBytes(data);
				readPosition += length;

				if (peek.Length == readPosition)
				{
					// We just consumed the entire string. Dequeue it.
					DropReadBuffer();
				}
				return res;
			}
			else
			{
				// Start building the data from the remainder in the buffer.
				int buffered = this.ReadBufferLength;
				if (buffered < length) length = buffered;
				byte[] data = new byte[length];
				int copied = peek.Length - readPosition;
                Array.Copy(peek.ReadonlyData, readPosition, data, 0, copied); readPosition += copied;
				length -= copied;

				// We just consumed the entire data. Dequeue it.
				DropReadBuffer();

				while (length > 0)
				{
					peek = readBuffers.Peek() as PhpBytes;
					if (peek.Length > length)
					{
						// This data is long enough. It is the last one.
                        Array.Copy(peek.ReadonlyData, 0, data, copied, length);
						readPosition = length;
						length = 0;
						break;
					}
					else
					{
						// Append just another whole buffer to the array.
                        Array.Copy(peek.ReadonlyData, 0, data, copied, peek.Length);
						length -= peek.Length;
						copied += peek.Length;
						DropReadBuffer();

						// When this is the last buffer (it's probably an EOF), return.
						if (readBuffers.Count == 0)
							break;
					}
				} // while

				Debug.Assert(copied > 0);
				if (copied < length)
				{
					byte[] sub = new byte[copied];
					Array.Copy(data, 0, sub, 0, copied);
					return new PhpBytes(sub);
				}
				return new PhpBytes(data);
			} // else
		}

		#endregion

		#region Data Block Conversions

		/// <summary>
		/// Gets the length of a block of data (either a <see cref="String"/> or <see cref="PhpBytes"/>).
		/// </summary>
		/// <param name="data">A <see cref="String"/> or <see cref="PhpBytes"/> to be measured.</param>
		/// <returns>The length of the block or <c>-1</c> if the type is neither <see cref="String"/> nor <see cref="PhpBytes"/>.
		/// </returns>
		public static int GetDataLength(object data)
		{
			string str;
			PhpBytes bin;

            if ((str = data as string) != null) return str.Length;
            else if ((bin = data as PhpBytes) != null) return bin.Length;

			// Must be either 
			Debug.Assert(false);
			return -1;
		}

		/// <summary>
		/// Casts the input parameter as <see cref="PhpBytes"/>, converting it
		/// using the page encoding if necessary.
		/// </summary>
		/// <param name="input">The input passed to the filter. Must not be <c>null</c>.</param>
		/// <returns>The input cast to <see cref="PhpBytes"/> or <see cref="PhpBytes.Empty"/> for empty input.</returns>
		public static PhpBytes AsBinary(object input)
		{
			return Core.Convert.ObjectToPhpBytes(input);
		}


		/// <summary>
		/// Casts the input parameter as <see cref="string"/>, converting it
		/// using the page encoding if necessary.
		/// </summary>
		/// <param name="input">The input passed to the filter. Must not be <c>null</c>.</param>
		/// <param name="count">The maximum count of input entities to convert.</param>
		/// <returns>The input cast to <see cref="PhpBytes"/> or <see cref="PhpBytes.Empty"/> for empty input.</returns>
		public static PhpBytes AsBinary(object input, int count)
		{
			if (input == null) return PhpBytes.Empty;

			// Use only the necessary portion of the string
			string str = input as string;
			if (str != null)
			{
				if (count > str.Length)
					return new PhpBytes(Configuration.Application.Globalization.PageEncoding.GetBytes(str));

				byte[] sub = new byte[count];
				Configuration.Application.Globalization.PageEncoding.GetBytes(str, 0, count, sub, 0);
				return new PhpBytes(sub);
			}

			// All other types treat as one case.
			PhpBytes bin = Core.Convert.ObjectToPhpBytes(input);
			if (count >= bin.Length) return bin;
			byte[] sub2 = new byte[count];
			Array.Copy(bin.ReadonlyData, 0, sub2, 0, count);
			return new PhpBytes(sub2);
		}

		/// <summary>
		/// Casts the input parameter as <see cref="string"/>, converting it
		/// using the page encoding if necessary.
		/// </summary>
		/// <param name="input">The input passed to the filter.</param>
		/// <returns>The input cast to <see cref="string"/> or <see cref="string.Empty"/> for empty input.</returns>
		public static string AsText(object input)
		{
			return Core.Convert.ObjectToString(input);
		}

		/// <summary>
		/// Casts the input parameter as <see cref="string"/>, converting it
		/// using the page encoding if necessary.
		/// </summary>
		/// <param name="input">The input passed to the filter.</param>
		/// <param name="count">The count of input entities to convert.</param>
		/// <returns>The input cast to <see cref="string"/> or <see cref="string.Empty"/> for empty input.</returns>
		public static string AsText(object input, int count)
		{
			if (input == null) return string.Empty;

			// Use only the necessary portion of the PhpBytes
			PhpBytes bin = input as PhpBytes;
			if (bin != null)
			{
				if (count > bin.Length) count = bin.Length;
                return Configuration.Application.Globalization.PageEncoding.GetString(bin.ReadonlyData, 0, count);
			}

			string str = Core.Convert.ObjectToString(input);
			if (count >= str.Length) return str;
			return str.Substring(0, count);
		}

		#endregion



		#region Block Reading
		/// <summary>
		/// Reads a block of data from the stream up to <paramref name="length"/>
		/// characters or up to EOLN if <paramref name="length"/> is negative.
		/// </summary>
		/// <remarks>
		/// ReadData first looks for data into the <see cref="readBuffers"/>. 
		/// While <paramref name="length"/> is not satisfied, new data from the underlying stream are processed.
		/// The data is buffered as either <see cref="string"/> or <see cref="PhpBytes"/>
		/// but consistently. The type of the first buffer thus specifies the return type.
		/// </remarks>
		/// <param name="length">The number of bytes to return, when set to <c>-1</c>
		/// reading carries on up to EOLN or EOF.</param>
		/// <param name="ending">If <c>true</c>, the buffers are first searched for \n.</param>
		/// <returns>A <see cref="string"/> or <see cref="PhpBytes"/> containing the 
		/// data as returned from the last stream filter or <b>null</b> in case of an error or <c>EOF</c>.</returns>
		public object ReadData(int length, bool ending)
		{
			if (length == 0) return null;

			// Allow length to be -1 for ReadLine.
			Debug.Assert((length > 0) || ending);
			Debug.Assert(length >= -1);

			// Set file access to reading
			CurrentAccess = FileAccess.Read;
			if (!CanRead) return null;

			// If (length < 0) read up to \n, otherwise up to length bytes      
			// Unbuffered works only for Read not for ReadLine (blocks).
			if (!IsReadBuffered && (readBuffers == null))
			{
				// The stream is a "pure" unbuffered. Read just the first packet.
				object packet = null;
				bool done = false;
				while (!done)
				{
					int count = (length > 0) ? length : readChunkSize;
					packet = ReadFiltered(count);
					if (packet == null) return null;

					int filteredLength = GetDataLength(packet);
					done = filteredLength > 0;
					readFilteredCount += filteredLength;

					if (length < 0)
					{
						// If the data contains the EOLN, store the rest into the buffers, otherwise return the whole packet.
						int eoln = FindEoln(packet, 0);
						if (eoln > 0)
						{
							object rv, enq;
							SplitData(packet, eoln, out rv, out enq);
							if (enq != null) EnqueueReadBuffer(enq);
							return rv;
						}
					}
				}
				return packet;
			}

			// Try to fill the buffers with enough data (to satisfy length).
			int nlpos, buffered = ReadBufferScan(out nlpos), read = 0, newLength = length;
			object data = null;

			if (ending && (nlpos >= readPosition))
			{
				// Found a \n in the buffered data (return the line inluding the EOLN).
				// Network-based streams may be satisfied too.
				newLength = nlpos - readPosition + 1;
			}
			else if ((length > 0) && (buffered >= length))
			{
				// Great! Just take some of the data in the buffers.
				// NOP
			}
			else if (!IsReadBuffered && (buffered > 0))
			{
				// Use the first available packet for network-based streams.
				newLength = buffered;
			}
			else
			{
				// There is not enough data in the buffers, read more.
				for (; ; )
				{
					data = ReadFiltered(readChunkSize);
					if (data == null)
					{
						// There is an EOF, return as much data as possible.
						newLength = buffered;
						break;
					}
					read = GetDataLength(data);
					readFilteredCount += read;
					if (read > 0) EnqueueReadBuffer(data);
					buffered += read;

					// For unbuffered streams accept the first packet and go check for EOLN.
					if (!IsReadBuffered) newLength = buffered;

					// First check for satisfaciton of the ending.
					if (ending && (data != null))
					{
						// Find the EOLN in the most recently read buffer.
						int eoln = FindEoln(data, 0);
						if (eoln >= 0)
						{
							// Read all the data up to (and including) the EOLN.
							newLength = buffered - read + eoln + 1;
							break;
						}
					}

					// Check if there is enough data in the buffers (first packet etc).
					if (length > 0)
					{
						if (buffered >= length) break;
					}
				}
			}

			// Apply the restriction of available data size or newline position
			if ((newLength < length) || (length == -1)) length = newLength;

			// Eof?
			if ((readBuffers == null) || (readBuffers.Count == 0))
				return null;

			// Read the rest of the buffered data if no \n is found and there is an EOF.
			if (length < 0) length = buffered;

			if (this.IsText)
				return ReadTextBuffer(length);
			else
				return ReadBinaryBuffer(length);
			// Data may only be a string or PhpBytes (and consistently throughout all the buffers).
		}


		/// <summary>
		/// Reads binary data from the stream. First looks for data into the 
		/// <see cref="readBuffers"/>. When <paramref name="length"/> is not
		/// satisfied, new data from the underlying stream are processed.
		/// </summary>
		/// <param name="length">The number of bytes to return.</param>
		/// <returns><see cref="PhpBytes"/> containing the binary data read from the stream.</returns>
		public PhpBytes ReadBytes(int length)
		{
			Debug.Assert(this.IsBinary);
			// Data may only be a string or PhpBytes.
			object data = ReadData(length, false);
			if (data == null)
				return null;

			return AsBinary(data);
		}

		/// <summary>
		/// Reads text data from the stream. First looks for data into the 
		/// <see cref="readBuffers"/>. When <paramref name="length"/> is not
		/// satisfied, new data from the underlying stream are processed.
		/// </summary>
		/// <param name="length">The number of characters to return.</param>
		/// <returns><see cref="string"/> containing the text data read from the stream.</returns>
		public string ReadString(int length)
		{
			Debug.Assert(this.IsText);
			// Data may only be a string or PhpBytes.
			object data = ReadData(length, false);
			if (data == null)
				return null;

			return AsText(data);
		}

		/// <summary>
		/// Finds the '\n' in a string or PhpBytes and returns its offset or <c>-1</c>
		/// if not found.
		/// </summary>
		/// <param name="data">Data to scan.</param>
		/// <param name="from">Index of the first character to scan.</param>
		/// <returns></returns>
		private int FindEoln(object data, int from)
		{
			Debug.Assert(data != null);
			if (this.IsText)
			{
				string s = data as string;
				Debug.Assert(s != null);
				return s.IndexOf('\n', from);
			}
			else
			{
				PhpBytes bin = data as PhpBytes;
				Debug.Assert(bin != null);
                return ArrayUtils.IndexOf(bin.ReadonlyData, (byte)'\n', from);
				/*
				for (int i = from; i < bin.Data.Length; i++)
				{
				  if (bin.Data[i] == '\n') return i;
				}
				return -1;
				/**/
			}
		}

		/// <summary>
		/// Split a string or PhpBytes to "upto" bytes at left and the rest or null at right.
		/// </summary>
		/// <param name="data"></param>
		/// <param name="upto"></param>
		/// <param name="left"></param>
		/// <param name="right"></param>
		private void SplitData(object data, int upto, out object left, out object right)
		{
			Debug.Assert(data != null);
			Debug.Assert(upto >= 0);
			if (this.IsText)
			{
				string s = data as string;
				Debug.Assert(s != null);
				if (upto < s.Length - 1)
				{
					left = s.Substring(0, upto + 1);
					right = s.Substring(upto + 2);
				}
				else
				{
					left = s;
					right = null;
				}
			}
			else
			{
				PhpBytes bin = data as PhpBytes;
				Debug.Assert(bin != null);
				if (upto < bin.Length - 1)
				{
					byte[] l = new byte[upto + 1], r = new byte[bin.Length - upto - 1];
                    Array.Copy(bin.ReadonlyData, 0, l, 0, upto + 1);
                    Array.Copy(bin.ReadonlyData, upto + 1, r, 0, bin.Length - upto - 1);
					left = new PhpBytes(l);
					right = new PhpBytes(r);
				}
				else
				{
					left = bin;
					right = null;
				}
			}
		}

		#endregion

		#region Maximum Block Reading

		/// <summary>
		/// Gets the number of bytes or characters in the first read-buffer or next chunk size.
		/// </summary>
		/// <returns>The number of bytes or characters the next call to ReadMaximumData would return.</returns>
		public int GetNextDataLength()
		{
			if ((readBuffers != null) && (readBuffers.Count > 0))
			{
				return GetDataLength(readBuffers.Peek());
			}
			else return readChunkSize;
		}

		/// <summary>
		/// Most effecient access to the buffered stream consuming one whole buffer at a time.
		/// Performs no unnecessary conversions (although attached stream filters may do so).
		/// </summary>
		/// <remarks>
		/// Use the <see cref="readChunkSize"/> member to affect the amount of data returned at a time.
		/// </remarks>
		/// <returns>A <see cref="string"/> or <see cref="PhpBytes"/> containing data read from the stream.</returns>
		public object ReadMaximumData()
		{
			// Set file access to reading
			CurrentAccess = FileAccess.Read;
			if (!CanRead) return null;

			object data = null;
			if ((readBuffers == null) || (readBuffers.Count == 0))
			{
				// Read one block without storing it in the buffers.
				data = ReadFiltered(readChunkSize);
				int filteredLength = (data != null) ? GetDataLength(data) : 0;
				readFilteredCount += filteredLength;
			}
			else
			{
				// Dequeue one whole buffer.
				data = readBuffers.Peek();
				DropReadBuffer();
			}

			if (data == null) return null;
			Debug.Assert((data is string) || (data is PhpBytes));
			return data;
		}

		/// <summary>
		/// Effecient access to the buffered and filtered stream consuming one whole buffer at a time.
		/// </summary>
		/// <returns>A <see cref="PhpBytes"/> containing data read from the stream.</returns>
		public PhpBytes ReadMaximumBytes()
		{
			return AsBinary(ReadMaximumData());
		}

		/// <summary>
		/// Effecient access to the buffered and filtered stream consuming one whole buffer at a time.
		/// </summary>
		/// <returns>A <see cref="string"/> containing data read from the stream.</returns>
		public string ReadMaximumString()
		{
			return AsText(ReadMaximumData());
		}

		#endregion

		#region Entire Stream Reading

		public object ReadContents()
		{
			return ReadContents(-1, -1);
		}

		public object ReadContents(int maxLength)
		{
			return ReadContents(maxLength, -1);
		}

		public object ReadContents(int maxLength, int offset)
		{
			if (offset > -1 && !Seek(offset, SeekOrigin.Begin))
				return null;

			if (IsText)
				return ReadStringContents(maxLength);
			else
				return ReadBinaryContents(maxLength);
		}

		public string ReadStringContents(int maxLength)
		{
			if (!CanRead) return null;
			StringBuilder result = new StringBuilder();

			if (maxLength >= 0)
			{
				while (maxLength > 0 && !Eof)
				{
					string data = ReadString(maxLength);
					if (data == null && data.Length > 0) break; // EOF or error.
					maxLength -= data.Length;
					result.Append(data);
				}
			}
			else
			{
				while (!Eof)
				{
					string data = ReadMaximumString();
					if (data == null) break; // EOF or error.
					result.Append(data);
				}
			}

			return result.ToString();
		}

		public PhpBytes ReadBinaryContents(int maxLength)
		{
			if (!CanRead) return null;
			MemoryStream result = new MemoryStream();

			if (maxLength >= 0)
			{
				while (maxLength > 0 && !Eof)
				{
					PhpBytes data = ReadBytes(maxLength);
					if (data == null && data.Length > 0) break; // EOF or error.
					maxLength -= data.Length;
                    result.Write(data.ReadonlyData, 0, data.Length);
				}
			}
			else
			{
				while (!Eof)
				{
					PhpBytes data = ReadMaximumBytes();
					if (data == null) break; // EOF or error.
                    result.Write(data.ReadonlyData, 0, data.Length);
				}
			}
			return new PhpBytes(result.ToArray());
		}

		#endregion

		#region Parsed Reading (ReadLine)
		/// <summary>
		/// Reads one line (text ending with the <paramref name="ending"/> delimiter)
		/// from the stream up to <paramref name="length"/> characters long.
		/// </summary>
		/// <param name="length">Maximum length of the returned <see cref="string"/> or <c>-1</c> for unlimited reslut.</param>
		/// <param name="ending">Delimiter of the returned line or <b>null</b> to use the system default.</param>
		/// <returns>A <see cref="string"/> containing one line from the input stream.</returns>
		public string ReadLine(int length, string ending)
		{
			// A length has to be specified if we want to use the delimiter.
			Debug.Assert((length > 0) || (ending == null));

			object data = ReadData(length, ending == null); // null ending => use \n
			string str = AsText(data);

			if (ending != null)
			{
				int pos = (ending.Length == 1) ? str.IndexOf(ending[0]) : str.IndexOf(ending);
				if (pos >= 0)
				{
					object left, right;
					SplitData(str, pos + ending.Length - 1, out left, out right);
					Debug.Assert(left is string);
					Debug.Assert(right is string);
					int returnedLength = (right as string).Length;
					if (this.IsBinary) right = AsBinary(right);

					if (readBuffers.Count > 0)
					{
						// EX: Damn. Have to put the data to the front of the queue :((
						// Better first look into the buffers for the ending..
						Queue newBuffers = new Queue(readBuffers.Count + 2);
						newBuffers.Enqueue(right);
						foreach (object o in readBuffers)
						{
							newBuffers.Enqueue(o);
						}
						readBuffers = newBuffers;
					}
					else
					{
						readBuffers.Enqueue(right);
					}
					// Update the offset as the data gets back.
					readOffset -= returnedLength;
					return left as string;
				}
			}
			// ReadLine now works on binary files too but only for the \n ending.
			return str;
		}

		#endregion

		#region Filter Chains
		/// <summary>
		/// Adds a filter to one of the read or write filter chains.
		/// </summary>
		/// <param name="filter">The filter.</param>
		/// <param name="where">The position in the chain.</param>
		public void AddFilter(IFilter filter, FilterChainOptions where)
		{
			Debug.Assert((where & FilterChainOptions.ReadWrite) != FilterChainOptions.ReadWrite);
			ArrayList list = null;

			// Which chain.
			if ((where & FilterChainOptions.Read) > 0)
			{
				if (readFilters == null) readFilters = new ArrayList();
				list = readFilters;
			}
			else
			{
				if (writeFilters == null) writeFilters = new ArrayList();
				list = writeFilters;
			}

			// Position in the chain.
			if ((where & FilterChainOptions.Tail) > 0)
			{
				list.Add(filter);
				if ((list == readFilters) && (ReadBufferLength > 0))
				{
					// Process all the data in the read buffers.
					Queue q = new Queue();
					foreach (object o in readBuffers)
						q.Enqueue(filter.Filter(o, false));
					readBuffers = q;
				}
			}
			else
			{
				list.Insert(0, filter);
			}
		}

        /// <summary>
        /// Get enumerator of chained read/write filters.
        /// </summary>
        public System.Collections.Generic.IEnumerable<PhpFilter> StreamFilters
        {
            get
            {
                if (readFilters != null)
                    foreach (PhpFilter f in readFilters)
                        yield return f;

                if (writeFilters != null)
                    foreach (PhpFilter f in writeFilters)
                        yield return f;
            }
        }

		#endregion

		#endregion

		#region High-level Writing
		#region Buffered Writing
		/// <summary>
		/// Write all the output buffer to the underlying stream and flush it.
		/// </summary>
		/// <returns><c>true</c> on success, <c>false</c> on error.</returns>
		public bool Flush()
		{
			return FlushWriteBuffer() && RawFlush();
		}

		/// <summary>
		/// Writes all the output buffer to the underlying stream.
		/// </summary>
		/// <returns><c>true</c> on success, <c>false</c> on error.</returns>
		protected bool FlushWriteBuffer()
		{
			// Stream may not have been used for output yet.
			if ((writeBufferSize == 0) || (writeBuffer == null)) return true;

			int flushPosition = 0;
			while (flushPosition < writePosition)
			{
				// Send as much data as possible to the underlying stream.
				int written = RawWrite(writeBuffer, flushPosition, writePosition - flushPosition);

				if (written <= 0)
				{
					// An error occured. Clear flushed data and return.
					if (flushPosition > 0)
					{
						byte[] buf = new byte[writeBufferSize];
						Array.Copy(writeBuffer, flushPosition, buf, 0, writePosition - flushPosition);
						writeBuffer = buf;
					}

					PhpException.Throw(PhpError.Warning,
						CoreResources.GetString("stream_write_failed", flushPosition, writePosition));

					return false;
				}
				else
				{
					// Move for the next chunk.
					flushPosition += written;
					writeOffset += written;
				}
			}

			// All the data has been successfully flushed.
			writePosition = 0;
			return true;
		}
		#endregion

		#region Block Writing
		/// <summary>
		/// Passes the data through output filter-chain to the output buffer. 
		/// When the buffer is full or buffering is disabled, passes the data to the low-level stream.
		/// </summary>
		/// <param name="data">The data to store (filters will handle the type themselves).</param>
		/// <returns>Number of character entities successfully written or <c>-1</c> on an error.</returns>
		public int WriteData(object data)
		{
			return WriteData(data, false);
		}

		/// <summary>
		/// Apppends the binary data to the output buffer passing through the output filter-chain. 
		/// When the buffer is full or buffering is disabled, pass the data to the low-level stream.
		/// </summary>
		/// <param name="data">The <see cref="PhpBytes"/> to store.</param>
		/// <returns>Number of bytes successfully written or <c>-1</c> on an error.</returns>
		public int WriteBytes(PhpBytes data)
		{
			Debug.Assert(this.IsBinary);
			return WriteData(data, false);
		}

		/// <summary>
		/// Apppends the text data to the output buffer passing through the output filter-chain. 
		/// When the buffer is full or buffering is disabled, pass the data to the low-level stream.
		/// </summary>
		/// <param name="data">The <see cref="string"/> to store.</param>
		/// <returns>Number of characters successfully written or <c>-1</c> on an error.</returns>
		public int WriteString(string data)
		{
			Debug.Assert(this.IsText);
			return WriteData(data, false);
		}

		/// <summary>
		/// Passes the data through output filter-chain to the output buffer. 
		/// When the buffer is full or buffering is disabled, passes the data to the low-level stream.
		/// </summary>
		/// <param name="data">The data to store (filters will handle the type themselves).</param>
		/// <param name="closing"><c>true</c> when this method is called from <c>close()</c>
		/// to prune all the pending filters with closing set to <c>true</c>.</param>
		/// <returns>Number of character entities successfully written or <c>-1</c> on an error.</returns>
		protected int WriteData(object data, bool closing)
		{
			// Set file access to writing
			CurrentAccess = FileAccess.Write;
			if (!CanWrite) return -1;

			Debug.Assert((data is string) || (data is PhpBytes));

			int consumed = GetDataLength(data);
			writeFilteredCount += consumed;
			if (writeFilters != null)
			{
				// Process the data through the custom write filters first.
				foreach (IFilter f in writeFilters)
				{
					if (data == null)
					{
						// When closing, feed all the filters with data.
						if (closing) data = PhpBytes.Empty;
						else return consumed; // Eaten all
					}
					data = f.Filter(data, closing);
					if (closing) f.OnClose();
				}
			}

			if (textWriteFilter != null)
			{
				// Then pass it through the text-conversion filter if any.
				data = textWriteFilter.Filter(data, closing);
			}

			// From now on, the data is treated just as binary
            byte[] bin = AsBinary(data).ReadonlyData;
			if (bin.Length == 0)
				return consumed;

			// Append the resulting data to the output buffer if any.
			if (IsWriteBuffered)
			{
				// Is this the first access?
				if (writeBuffer == null)
				{
					writeBuffer = new byte[writeBufferSize];
					writePosition = 0;
				}

				// The whole binary data fits in the buffer, great!
				if (writeBufferSize - writePosition > bin.Length)
				{
					Array.Copy(bin, 0, writeBuffer, writePosition, bin.Length);
					writePosition += bin.Length;
					return consumed;
				}

				int copied = 0;

				// Use the buffer for small data only
				if (writeBufferSize > bin.Length)
				{
					// Otherwise fill the buffer and flush it.
					copied = writeBufferSize - writePosition;
					Array.Copy(bin, 0, writeBuffer, writePosition, copied);
					writePosition += copied;
				}

				// Flush the buffer
				if ((writePosition > 0) && (!FlushWriteBuffer()))
					return (copied > 0) ? copied : -1; // It is an error but still some output was written.

				if (bin.Length - copied >= writeBufferSize)
				{
					// If the binary data is really big, write it directly to stream.
					while (copied < bin.Length)
					{
						int written = RawWrite(bin, copied, bin.Length - copied);
						if (written <= 0)
						{
							PhpException.Throw(PhpError.Warning,
								CoreResources.GetString("stream_write_failed", copied, bin.Length));
							return (copied > 0) ? copied : -1; // It is an error but still some output was written.
						}
						copied += written;
						writeOffset += written;
					}
				}
				else
				{
					// Otherwise just start a new buffer with the rest of the data.
					Array.Copy(bin, copied, writeBuffer, 0, bin.Length - copied);
					writePosition = bin.Length - copied;
				}

				return consumed;
			}
			else
			{
				// No write buffer. Write the data directly.
				int copied = 0;
				while (copied < bin.Length)
				{
					int written = RawWrite(bin, copied, bin.Length - copied);
					if (written <= 0)
					{
						PhpException.Throw(PhpError.Warning,
							CoreResources.GetString("stream_write_failed", copied, bin.Length));
						return (copied > 0) ? copied : -1; // ERROR but maybe some was written.
					}
					copied += written;
					writeOffset += written;
				}

				return consumed;
			}
		}
		#endregion
		#endregion


		/// <summary>
		/// Sets the read/write pointer in the stream to a new position.
		/// </summary>
		/// <param name="offset">The offset from the position denoted by <paramref name="whence"/>.</param>
		/// <param name="whence">One of the <see cref="SeekOrigin"/> flags.</param>
		/// <returns><c>true</c> if the operation was successful.</returns>
		public bool Seek(int offset, SeekOrigin whence)
		{
			if (!CanSeek)
			{
				PhpException.Throw(PhpError.Warning, CoreResources.GetString("wrapper_op_unsupported", "Seek"));
				return false;
			}

			// This is supported by any stream.
			int current = Tell();
			int newpos = -1;
			if (whence == SeekOrigin.Begin) newpos = offset;
			else if (whence == SeekOrigin.Current) newpos = current + offset;
			else if (whence == SeekOrigin.End)
			{
				int len = RawLength();
				if (len >= 0) newpos = len + offset;
			}

			switch (CurrentAccess)
			{
				case FileAccess.ReadWrite:
					// Stream not R/W accessed yet. Prepare location and offset.
					return SeekInternal(offset, current, whence);

				case FileAccess.Read:
					// Maybe we will be able to seek inside the buffers.
					if ((newpos >= readOffset) && (newpos < readOffset + ReadBufferLength))
					{
						int streamPosition = readOffset + ReadPosition;
						if (newpos > streamPosition)
						{
							// Seek forward
							// This asserts that ReadBufferLength > 0.
							int len = GetDataLength(readBuffers.Peek());
							while (newpos - readOffset >= len)
							{
								DropReadBuffer();
								len = GetDataLength(readBuffers.Peek());
							}
							Debug.Assert(readBuffers.Count > 0);

							// All superfluous buffers are dropped, seek in the head one.
							readPosition = newpos - readOffset;
						}
						else if (newpos < streamPosition)
						{
							// The required position is still in the first buffer
							//. Debug.Assert(streamPosition == readOffset + readPosition);
							readPosition = newpos - readOffset;
						}
					}
					else
					{
						// Drop all the read buffers and proceed to the actual seeking.
						readBuffers = null;

						// Notice that for a filtered stream, seeking is not a good idea
						if (IsReadFiltered)
						{
							PhpException.Throw(PhpError.Notice,
								CoreResources.GetString("stream_seek_filtered", (textReadFilter != null) ? "text" : "filtered"));
						}
						return SeekInternal(offset, current, whence);
					}
					break;

				case FileAccess.Write:
                    // The following does not currently work since other methods do not take unempty writebuffer into account

                    //// Maybe we can seek inside of the buffer but we allow only backward skips.
                    //if ((newpos >= writeOffset) && (newpos < writeOffset + writePosition))
                    //{
                    //    // We are inside the current buffer, great.
                    //    writePosition = newpos - writeOffset;
                    //}
                    //else
                    //{

					// Flush write buffers and proceed to the default handling.
					FlushWriteBuffer();

					// Notice that for a filtered stream, seeking is not a good idea
					if (IsWriteFiltered)
					{
						PhpException.Throw(PhpError.Notice,
							CoreResources.GetString("stream_seek_filtered", (textWriteFilter != null) ? "text" : "filtered"));
					}
					return SeekInternal(offset, current, whence);
			}
			return true;
			// CHECKME: [PhpStream.Seek]
		}

		/// <summary>
		/// Perform the actual seek on the stream. Report errors.
		/// </summary>
		/// <param name="offset">New position in the stream.</param>
		/// <param name="current">Current position in the stream.</param>
		/// <param name="whence">Where to count from.</param>
		/// <returns><c>true</c> if successful</returns>
		/// <exception cref="PhpException">In case that Seek is not supported by this stream type.</exception>
		internal bool SeekInternal(int offset, int current, SeekOrigin whence)
		{
			try
			{
				if (!CanSeek)
				{
					PhpException.Throw(PhpError.Warning, CoreResources.GetString("wrapper_op_unsupported", "Seek"));
					return false;
				}

				if (!RawSeek(offset, whence)) return false;
				int expectedOffset = 0, absoluteOffset = RawTell();

				switch (whence)
				{
					case SeekOrigin.Begin:
						expectedOffset = offset;
						break;
					case SeekOrigin.Current:
						expectedOffset = current + offset;
						break;
					case SeekOrigin.End:
						expectedOffset = RawLength() + offset;
						break;
					default:
						PhpException.Throw(PhpError.Warning, CoreResources.GetString("invalid_argument_value", "whence", whence));
						return false;
				}

				readOffset = writeOffset = absoluteOffset;

				// No data should be buffered when seeking the underlying stream!
				Debug.Assert(readBuffers == null);
				Debug.Assert(writeBuffer == null || writePosition == 0);
				readPosition = writePosition = 0;

				// EX: This is inaccurate, but there is no better information avalable (w/o processing the whole stream)
				readFilteredCount = readOffset;
				writeFilteredCount = readOffset;

				return absoluteOffset == expectedOffset;
				// Seek is successful if the two values match.
			}
			catch (Exception)
			{
				PhpException.Throw(PhpError.Warning, CoreResources.GetString("wrapper_op_unsupported", "Seek"));
				return false;
			}
		}

		/// <summary>
		/// Gets the current position in the stream.
		/// </summary>
		/// <remarks>
		/// <newpara>
		/// The problem with tell() in PHP is that although the write offset 
		/// is calculated in the raw byte stream (just before buffering)
		/// the read one is calculated in the filtered string buffers.
		/// </newpara>
		/// <newpara>
		/// In other words the value returned by tell() for output streams
		/// is the real position in the raw stream but may differ from the
		/// number of characters written. On the other hand the value returned for
		/// input streams corresponds with the number of characters retreived 
		/// but not with the position in the raw stream. It is important
		/// to remember that seeking on a filtered stream (such as a file
		/// opened with a "rt" mode) has undefined behavior.
		/// </newpara>
		/// </remarks>
		/// <returns>The position in the filtered or raw stream depending on last 
		/// read or write access type respectively or -1 if the stream does not support seeking.</returns>
		public int Tell()
		{
			if (!CanSeek)
			{
				PhpException.Throw(PhpError.Warning, CoreResources.GetString("wrapper_op_unsupported", "Seek"));
				return -1;
			}
			switch (currentAccess)
			{
				default:
					// Stream not yet R/W accessed (but maybe with Seek).
					return readOffset;
				case FileAccess.Read:
					return ReadPosition;
				case FileAccess.Write:
					return WritePosition;
			}
		}
		#endregion

		#region Conversions

		/// <include file='Doc/Streams.xml' path='docs/property[@name="RawStream"]/*'/>
		/// <exception cref="InvalidCastException">When casting is not supported.</exception>
		public virtual Stream RawStream
		{
			get
			{
				throw new InvalidCastException(CoreResources.GetString("casting_to_stream_unsupported"));
			}
		}

		/// <summary>
		/// Check that the resource handle contains a valid
		/// PhpStream resource and cast the handle to PhpStream.
		/// </summary>
		/// <param name="handle">A PhpResource passed to the PHP function.</param>
		/// <returns>The handle cast to PhpStream.</returns>
		public static PhpStream GetValid(PhpResource handle)
		{
			PhpStream result = handle as PhpStream;
			if (result != null && result.IsValid) return result;

			PhpException.Throw(PhpError.Warning, CoreResources.GetString("invalid_stream_resource"));
			return null;
		}

		public static PhpStream GetValid(PhpResource handle, FileAccess desiredAccess)
		{
			PhpStream result = GetValid(handle);

			if (result != null)
			{
				if ((desiredAccess & FileAccess.Write) != 0 && !result.CanWrite)
				{
					PhpException.Throw(PhpError.Warning, CoreResources.GetString("stream_write_off"));
					return null;
				}

				if ((desiredAccess & FileAccess.Read) != 0 && !result.CanRead)
				{
					PhpException.Throw(PhpError.Warning, CoreResources.GetString("stream_read_off"));
					return null;
				}
			}
			return result;
		}

		#endregion

		#region Stream properties

		/// <summary>
		/// The stream context options resource.
		/// </summary>
		public StreamContext Context
		{
			get { return context; }
		}

		/// <summary>
		/// The stream context options resource.
		/// </summary>
		protected StreamContext context;

		/// <summary>
		/// Gets the Auto-remove option of this stream.
		/// </summary>
		public bool IsTemporary
		{
			get
			{
				return (Options & StreamAccessOptions.Temporary) > 0;
			}
		}

		/// <summary>
		/// Gets or sets the read fragmentation behavior.
		/// </summary>
		/// <remarks>
		/// <para>
		/// Network and console input streams return immediately after a nonempty data is read from the underlying stream.
		/// Buffered streams try to fill the whole given buffer while the underlying stream is providing data
		/// to satisfy the caller-specified length or <see cref="readChunkSize"/>.
		/// </para>
		/// <para>
		/// Still the input buffer may contain valid data even for unbuffered streams.
		/// This may happen for example when a <c>fgets</c> has to return unconsumed data
		/// (following the first <c>EOL</c>) back to the stream.
		/// </para>
		/// </remarks>
		public bool IsReadBuffered
		{
			get
			{
				return isReadBuffered;
			}
			set
			{
				isReadBuffered = value;
			}
		}

		/// <summary>
		/// Gets the write fragmentation behavior.
		/// </summary>
		/// <remarks>
		/// When the write is not buffered then all the fwrite calls
		/// pass the data immediately to the underlying stream.
		/// </remarks>
		public bool IsWriteBuffered
		{
			get
			{
				return writeBufferSize > 0;
			}
			set
			{
				if (value) writeBufferSize = DefaultBufferSize;
				else writeBufferSize = 0;
			}
		}

		/// <summary>
		/// Gets the filtering status of this stream. 
		/// <c>true</c> when there is at least one input filter on the stream.
		/// </summary>
		protected bool IsReadFiltered
		{
			get
			{
				return (((readFilters != null) && (readFilters.Count > 0))
					|| (textReadFilter != null));
			}
		}

		/// <summary>
		/// Gets the filtering status of this stream. 
		/// <c>true</c> when there is at least one output filter on the stream.
		/// </summary>
		protected bool IsWriteFiltered
		{
			get
			{
				return (((writeFilters != null) && (writeFilters.Count > 0))
					|| (textWriteFilter != null));
			}
		}

        /// <summary>Gets or sets the current Read/Write access mode.</summary>
		protected FileAccess CurrentAccess
		{
			get
			{
				return currentAccess;
			}
			set
			{
				switch (value)
				{
					case FileAccess.Read:
						if (!CanRead)
						{
							PhpException.Throw(PhpError.Warning, CoreResources.GetString("stream_read_off"));
							break;
						}
						if ((currentAccess == FileAccess.Write) && CanSeek)
						{
							// Flush the write buffers, switch to reading at the write position
							int offset = Tell();
							FlushWriteBuffer();
							writeOffset = writePosition = 0;
							currentAccess = value;
							Seek(offset, SeekOrigin.Begin);
						}
						currentAccess = value;
						break;

					case FileAccess.Write:
						if (!CanWrite)
						{
							PhpException.Throw(PhpError.Warning, CoreResources.GetString("stream_write_off"));
							break;
						}
						if ((currentAccess == FileAccess.Read) && CanSeek)
						{
							// Drop the read buffers, switch to writing at the read position
							int offset = Tell();
							//DropReadBuffer();
							readBuffers = null;
							readOffset = readPosition = 0;
							currentAccess = value;
							Seek(offset, SeekOrigin.Begin);
						}
						currentAccess = value;
						break;

					default:
						throw new ArgumentException();
				}
			}

			// CHECKME: [CurrentAccess]
		}

		/// <summary>Gets the writing pointer position in the buffered stream.</summary>
		public int WritePosition
		{
			get
			{
				if (CurrentAccess != FileAccess.Write) return -1;

				// Data passed via filters to output buffers (not filtered yet!)
                return writeFilteredCount;
                //try
                //{
                //  return RawTell() + this.writePosition;
                //}
                //catch (Exception)
                //{
                //  return this.writeOffset + this.writePosition;
                //}
			}
		}

		/// <summary>Gets the reading pointer position in the buffered stream.</summary>
		public int ReadPosition
		{
			get
			{
				if (CurrentAccess != FileAccess.Read) return -1;

				// Data physically read - data still in buffers
                return readFilteredCount - ReadBufferLength;
                //try
                //{
                //  return RawTell() - ReadBufferLength;
                //  // The position in the stream minus the data remaining in the buffers
                //}
                //catch (Exception)
                //{
                //  return this.readOffset + this.readPosition;
                //}
			}
		}

		/// <summary>The lists of StreamFilters associated with this stream.</summary>
		protected ArrayList readFilters = null, writeFilters = null;

		/// <summary>The text-mode conversion filter of this stream used for reading.</summary>
		protected IFilter textReadFilter = null;

		/// <summary>The text-mode conversion filter of this stream used for writing.</summary>
		protected IFilter textWriteFilter = null;

		/// <summary>
		/// The StreamWrapper responsible for opening this stream.
		/// </summary>
		/// <remarks>
		/// Used for example to access the correct section of context
		/// and for wrapper-notifications too.
		/// </remarks>
		public readonly StreamWrapper Wrapper;

        /// <summary>
        /// PHP wrapper specific data. See GetMetaData, wrapper_data array item.
        /// Can be null.
        /// </summary>
        public object WrapperSpecificData
        {
            get;
            internal set;
        }

		/// <summary>
		/// The absolute path to the resource.
		/// </summary>
		public readonly string OpenedPath;

		/// <summary>
		/// <c>true</c> if the stream was opened for writing.
		/// </summary>
		public bool CanWrite
		{
			get { return (Options & StreamAccessOptions.Write) > 0; }
		}

		/// <summary>
		/// <c>true</c> if the stream was opened for reading.
		/// </summary>
		public bool CanRead
		{
			get { return (Options & StreamAccessOptions.Read) > 0; }
		}

		/// <summary>
		/// <c>true</c> if the stream was opened in the text access-mode.
		/// </summary>
		public bool IsText
		{
			get { return (Options & StreamAccessOptions.UseText) > 0; }
		}

		/// <summary>
		/// <c>true</c> if the stream was opened in the binary access-mode.
		/// </summary>
		public bool IsBinary
		{
			get { return (Options & StreamAccessOptions.UseText) == 0; }
		}

		/// <summary>
		/// <c>true</c> if the stream persists accross multiple scripts.
		/// </summary>
		public bool IsPersistent
		{
			get { return (Options & StreamAccessOptions.Persistent) != 0; }
		}

		/// <summary>
		/// Additional stream options defined at open-time.
		/// </summary>
		public readonly StreamAccessOptions Options;

		/// <summary>
		/// Gets the type of last stream access (initialized to FileAccess.ReadWrite if not accessed yet).
		/// </summary>
		protected FileAccess currentAccess = FileAccess.ReadWrite;

		/// <summary>
		/// For <c>fgetss()</c> to handle multiline tags.
		/// </summary>
		public int StripTagsState
		{
			get { return fgetssState; }
			set { fgetssState = value; }
		}

		/// <summary>For <c>fgetss()</c> to handle multiline tags.</summary>
		protected int fgetssState = 0;

		/// <summary>For future use. Persistent streams are not implemented so far.</summary>
		protected bool isPersistent = false;

		/// <summary>The default size of read/write buffers.</summary>
		public const int DefaultBufferSize = 8 * 1024;

		/// <summary>The default size of a single read chunk in the readBuffers.</summary>
		protected int readChunkSize = DefaultBufferSize;

		/// <summary>Whether the read operations are interated for a single <c>fread</c> call.</summary>
		protected bool isReadBuffered = true;

		/// <summary>The maximum count of buffered output bytes. <c>0</c> to disable buffering.</summary>
		protected int writeBufferSize = DefaultBufferSize;

		/// <summary>Store the filtered input data queued as either <see cref="String"/>s or <see cref="PhpBytes"/>.</summary>
		protected Queue readBuffers = null;

		/// <summary>Store the filtered output data in a <c>byte[]</c> up to <see cref="writeBufferSize"/> bytes.</summary>
		protected byte[] writeBuffer = null;

		/// <summary>The offset from the beginning of the raw stream to the
		/// first byte stored in the <see cref="readBuffers"/>.</summary>
		/// <remarks>This offset is incremented when a consumed buffer is dropped.</remarks>
		protected int readOffset = 0;

		/// <summary>
		/// The offset from the beginning of the raw stream to the
		/// first byte of the <see cref="writeBuffer"/>.
		/// </summary>
		/// <remarks>
		/// This offset is incremented when the buffer is being flushed
		/// or the data is written to a non-buffered stream.
		/// </remarks>
		protected int writeOffset = 0;

		/// <summary>The position in the first buffer in the <see cref="readBuffers"/>.</summary>
		protected int readPosition = 0;

		/// <summary>Total bytes passed through the ReadData function (after input filtering)</summary>
		protected int readFilteredCount = 0;

		/// <summary>Total bytes passed through the WriteData function (before output filtering)</summary>
		protected int writeFilteredCount = 0;

		/// <summary>The actual write position in the <see cref="writeBuffer"/>.</summary>
		protected int writePosition = 0;

		/// <summary>Timeout for network-based streams in seconds.</summary>
		protected double readTimeout = 0;

		/// <summary>
		/// The type name displayed when printing a variable of type PhpStream.
		/// </summary>
		public const string PhpStreamTypeName = "stream";

		#endregion
	}

	#endregion

	#region NativeStream class

	/// <summary>
	/// An implementation of <see cref="PhpStream"/> as a simple
	/// encapsulation of a .NET <see cref="System.IO.Stream"/> class
	/// which is directly accessible via the RawStream property.
	/// </summary>
	public class NativeStream : PhpStream
	{
		#region PhpStream overrides

		public NativeStream(Stream nativeStream, StreamWrapper openingWrapper, StreamAccessOptions accessOptions, string openedPath, StreamContext context)
			: base(openingWrapper, accessOptions, openedPath, context)
		{
			Debug.Assert(nativeStream != null);
			this.stream = nativeStream;
		}

		/// <summary>
		/// PhpResource.FreeManaged overridden to get rid of the contained context on Dispose.
		/// </summary>
		protected override void FreeManaged()
		{
			base.FreeManaged();
			try
			{
				stream.Close();
			}
			catch (NotSupportedException)
			{
			}
			if ( Wrapper != null )	//Can be php://output
				Wrapper.Dispose();
			stream = null;
		}

		#endregion

		#region Raw byte access (mandatory)

		/// <include file='Doc/Streams.xml' path='docs/method[@name="RawRead"]/*'/>
		protected override int RawRead(byte[] buffer, int offset, int count)
		{
			try
			{
				int read = stream.Read(buffer, offset, count);
				if (read == 0) reportEof = true;
				return read;
			}
			catch (NotSupportedException)
			{
				PhpException.Throw(PhpError.Warning, CoreResources.GetString("wrapper_op_unsupported", "Read"));
				return -1;
			}
			catch (IOException e)
			{
				PhpException.Throw(PhpError.Warning, CoreResources.GetString("stream_read_io_error", e.Message));
				return -1;
			}
			catch (Exception e)
			{
				// For example WebException (timeout)
				PhpException.Throw(PhpError.Warning, CoreResources.GetString("stream_read_error", e.Message));
				return -1;
			}
		}

		/// <include file='Doc/Streams.xml' path='docs/method[@name="RawWrite"]/*'/>
		protected override int RawWrite(byte[] buffer, int offset, int count)
		{
			long position = stream.CanSeek ? stream.Position : -1;
			try
			{
				stream.Write(buffer, offset, count);
				return stream.CanSeek ? unchecked((int)(stream.Position - position)) : count;
			}
			catch (NotSupportedException)
			{
				PhpException.Throw(PhpError.Warning, CoreResources.GetString("wrapper_op_unsupported", "Write"));
				return -1;
			}
			catch (IOException e)
			{
				PhpException.Throw(PhpError.Warning, CoreResources.GetString("stream_write_io_error", e.Message));
				return -1;
			}
			catch (Exception e)
			{
				PhpException.Throw(PhpError.Warning, CoreResources.GetString("stream_write_error", e.Message));
				return -1;
			}
		}

		/// <include file='Doc/Streams.xml' path='docs/method[@name="RawFlush"]/*'/>
		protected override bool RawFlush()
		{
			if (stream.CanWrite) stream.Flush();
			return true;
		}

		/// <include file='Doc/Streams.xml' path='docs/property[@name="RawEof"]/*'/>
		protected override bool RawEof
		{
			get
			{
				if (stream.CanSeek) return stream.Position == stream.Length;
				else return reportEof;
				// Otherwise there is no apriori information - will be revealed at next read...
			}
		}

		/// <summary>EOF stored at the time of the last read.</summary>
		private bool reportEof = false;

		#endregion

		#region Raw Seeking (optional)

		/// <include file='Doc/Streams.xml' path='docs/property[@name="CanSeek"]/*'/>
		public override bool CanSeek { get { return stream.CanSeek; } }

		/// <include file='Doc/Streams.xml' path='docs/method[@name="RawTell"]/*'/>
		protected override int RawTell()
		{
			return unchecked((int)stream.Position);
		}

		/// <include file='Doc/Streams.xml' path='docs/method[@name="RawSeek"]/*'/>
		protected override bool RawSeek(int offset, SeekOrigin whence)
		{
			// Store the current position to be able to check for seek()'s success.
			long position = stream.Position;
			return stream.Seek(offset, (SeekOrigin)whence)
				== SeekExpects(position, stream.Length, offset, whence);
		}

		/// <summary>
		/// Returns the Length property of the underlying stream.
		/// </summary>
		/// <returns></returns>
		protected override int RawLength()
		{
			try
			{
				return unchecked((int)stream.Length);
			}
			catch (Exception)
			{
				PhpException.Throw(PhpError.Warning, CoreResources.GetString("wrapper_op_unsupported", "Seek"));
				return -1;
			}
		}


		/// <summary>
		/// Get the expected position in the stream to check for Seek() failure.
		/// </summary>
		/// <param name="position">Actual position in the stream.</param>
		/// <param name="length">The length of the stream.</param>
		/// <param name="offset">The offset for the seek() operation.</param>
		/// <param name="whence">Where to count the new position from.</param>
		/// <returns>The expected new position.</returns>
		protected long SeekExpects(long position, long length, long offset, SeekOrigin whence)
		{
			switch (whence)
			{
				case SeekOrigin.Begin:
					return offset;
				case SeekOrigin.Current:
					return position + offset;
				case SeekOrigin.End:
					return length + offset;
				default:
					return -1;
			}
		}

		#endregion

		#region Conversion to .NET native Stream
		//    /// <include file='Doc/Streams.xml' path='docs/property[@name="CanCast"]/*'/>
		//    public override bool CanCast { get { return true; } }

		/// <include file='Doc/Streams.xml' path='docs/property[@name="RawStream"]/*'/>
		public override Stream RawStream
		{
			get
			{
				return stream;
			}
		}
		#endregion

		#region NativeStream properties

		/// <summary>The encapsulated native stream.</summary>
		protected Stream stream;

		#endregion
	}

	#endregion

    #region PhpUserStream class

    /// <summary>
    /// An implementation of <see cref="PhpStream"/> as a simple
    /// encapsulation of a .NET <see cref="System.IO.Stream"/> class
    /// which is directly accessible via the RawStream property.
    /// </summary>
    public class PhpUserStream : PhpStream
    {
        #region names of user wrapper methods

        public const string USERSTREAM_OPEN = "stream_open";
        public const string USERSTREAM_CLOSE = "stream_close";
        public const string USERSTREAM_READ = "stream_read";
        public const string USERSTREAM_WRITE = "stream_write";
        public const string USERSTREAM_FLUSH = "stream_flush";
        public const string USERSTREAM_SEEK = "stream_seek";
        public const string USERSTREAM_TELL = "stream_tell";
        public const string USERSTREAM_EOF = "stream_eof";
        public const string USERSTREAM_STAT = "stream_stat";
        public const string USERSTREAM_STATURL = "url_stat";
        public const string USERSTREAM_UNLINK = "unlink";
        public const string USERSTREAM_RENAME = "rename";
        public const string USERSTREAM_MKDIR = "mkdir";
        public const string USERSTREAM_RMDIR = "rmdir";
        public const string USERSTREAM_DIR_OPEN = "dir_opendir";
        public const string USERSTREAM_DIR_READ = "dir_readdir";
        public const string USERSTREAM_DIR_REWIND = "dir_rewinddir";
        public const string USERSTREAM_DIR_CLOSE = "dir_closedir";
        public const string USERSTREAM_LOCK = "stream_lock";
        public const string USERSTREAM_CAST = "stream_cast";
        public const string USERSTREAM_SET_OPTION = "stream_set_option";
        public const string USERSTREAM_TRUNCATE = "stream_truncate";
        public const string USERSTREAM_METADATA = "stream_metadata";

        #endregion

        #region PhpStream overrides

        public PhpUserStream(UserStreamWrapper/*!*/openingWrapper, StreamAccessOptions accessOptions, string openedPath, StreamContext context)
            : base(openingWrapper, accessOptions, openedPath, context)
        {
        }

        /// <summary>
        /// PhpResource.FreeManaged overridden to get rid of the contained context on Dispose.
        /// </summary>
        protected override void FreeManaged()
        {
            // stream_close
            if (UserWrapper != null)
                UserWrapper.OnClose(this);

            // free
            base.FreeManaged();
            if (Wrapper != null)	//Can be php://output
                Wrapper.Dispose();
        }

        #endregion

        #region Raw byte access (mandatory)
        
        /// <include file='Doc/Streams.xml' path='docs/method[@name="RawRead"]/*'/>
        protected override int RawRead(byte[] buffer, int offset, int count)
        {
            // stream_read:
            object result = UserWrapper.InvokeWrapperMethod(USERSTREAM_READ, count);

            if (result != null)
            {
                PhpBytes bytes = Convert.ObjectToPhpBytes(result);
                int readbytes = bytes.Length;
                if (readbytes > count)
                {
                    //php_error_docref(NULL TSRMLS_CC, E_WARNING, "%s::" USERSTREAM_READ " - read %ld bytes more data than requested (%ld read, %ld max) - excess data will be lost",
                    //    us->wrapper->classname, (long)(didread - count), (long)didread, (long)count);
                    readbytes = count;
                }

                if (readbytes > 0)
                    Array.Copy(bytes.ReadonlyData, 0, buffer, offset, readbytes);
                
                return readbytes;
            }

            //
            return 0;
        }

        /// <include file='Doc/Streams.xml' path='docs/method[@name="RawWrite"]/*'/>
        protected override int RawWrite(byte[] buffer, int offset, int count)
        {
            PhpBytes bytes;
            if (count == 0)
            {
                bytes = PhpBytes.Empty;
            }
            if (offset == 0 && count == buffer.Length)
            {
                bytes = new PhpBytes(buffer);
            }
            else
            {
                byte[] data = new byte[count];
                Array.Copy(buffer, offset, data, 0, count);
                bytes = new PhpBytes(data);
            }

            object result = UserWrapper.InvokeWrapperMethod(USERSTREAM_WRITE, bytes);

            int byteswrote = Convert.ObjectToInteger(result);
            if (byteswrote > count)
            {
                //php_error_docref(NULL TSRMLS_CC, E_WARNING, "%s::" USERSTREAM_WRITE " wrote %ld bytes more data than requested (%ld written, %ld max)",
                //us->wrapper->classname, (long)(didwrite - count), (long)didwrite, (long)count);
                byteswrote = count;
            }

            return byteswrote;
        }

        /// <include file='Doc/Streams.xml' path='docs/method[@name="RawFlush"]/*'/>
        protected override bool RawFlush()
        {
            return Convert.ObjectToBoolean(UserWrapper.InvokeWrapperMethod(USERSTREAM_FLUSH));
        }

        /// <include file='Doc/Streams.xml' path='docs/property[@name="RawEof"]/*'/>
        protected override bool RawEof
        {
            get
            {
                // stream_eof:
                if (Convert.ObjectToBoolean(UserWrapper.InvokeWrapperMethod(USERSTREAM_EOF)))
                    return true;
                // TODO: if USERSTREAM_EOF not implemented, assume EOF too

                return false;
            }
        }

        #endregion

        #region Raw Seeking (optional)

        /// <include file='Doc/Streams.xml' path='docs/property[@name="CanSeek"]/*'/>
        public override bool CanSeek { get { return true; } }

        /// <include file='Doc/Streams.xml' path='docs/method[@name="RawTell"]/*'/>
        protected override int RawTell()
        {
            return Convert.ObjectToInteger(UserWrapper.InvokeWrapperMethod(USERSTREAM_TELL));
                
        }

        /// <include file='Doc/Streams.xml' path='docs/method[@name="RawSeek"]/*'/>
        protected override bool RawSeek(int offset, SeekOrigin whence)
        {
            // stream_seek:
            return Convert.ObjectToBoolean(UserWrapper.InvokeWrapperMethod(USERSTREAM_SEEK, offset, (int)whence));
        }

        /// <summary>
        /// Returns the Length property of the underlying stream.
        /// </summary>
        /// <returns></returns>
        protected override int RawLength()
        {
            try
            {
                return -1;
            }
            catch (Exception)
            {
                PhpException.Throw(PhpError.Warning, CoreResources.GetString("wrapper_op_unsupported", "Seek"));
                return -1;
            }
        }

        #endregion

        #region PhpUserStream properties

        /// <summary><see cref="UserStreamWrapper"/>.</summary>
        protected UserStreamWrapper/*!*/UserWrapper { get { return (UserStreamWrapper)Wrapper; } }

        #endregion
    }

    #endregion
}
