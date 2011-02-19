/*

 Copyright (c) 2004-2006 Ladislav Prosek. Inspired by PipeChannel and MS Shared Source CLI.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;

namespace PHP.Core
{
	/// <summary>
	/// Context that is passed to worker threads when waiting for asynchronous call completion.
	/// <seealso cref="ShmClientTransportSink.ReceiveCallback"/>
	/// </summary>
	internal sealed class ShmConnectionCookie
	{
		/// <summary>
		/// The <see cref="ShmConnection"/> through which the call was made.
		/// </summary>
		public ShmConnection Connection;

		/// <summary>
		/// The stack of channel sinks that called <see cref="ShmClientTransportSink"/> to process the call.
		/// </summary>
		public IClientChannelSinkStack SinkStack;
	}

	/// <summary>
	/// Shared memory connection. This class represents one of the two ends of a shared memory
	/// connection. Whether it is the server end or the client end depends on a constructor
	/// parameter.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The communication protocol is as follows:<br/>
	/// Sender waits for his write event, copies data to the shared memory block and finally
	/// signals read event of the other side.
	/// Receiver waits for his read event, copies data from the shared memory block, and
	/// signals write event of the other side.
	/// </para>
	/// <para>
	/// Shared memory block layout:
	/// <list type="table">
	/// <item>
	/// <term>4 bytes</term>
	/// <description>total length of the message from server to client</description>
	/// </item>
	/// <item>
	/// <term><see cref="MESSAGE_BLOCK_SIZE"/> bytes</term>
	/// <description>the message from server to client (or part of it)</description>
	/// </item>
	/// <item>
	/// <term>4 bytes</term>
	/// <description>total length of the message from client to server</description>
	/// </item>
	/// <item>
	/// <term><see cref="MESSAGE_BLOCK_SIZE"/> bytes</term>
	/// <description>the message from client to server (or part of it)</description>
	/// </item>
	/// </list>
	/// </para>
	/// </remarks>
	internal sealed class ShmConnection : IDisposable
	{
		#region Fields

		/// <summary>
		/// Specifies whether this is the server end of the connection.
		/// </summary>
		private bool serverSide;

		/// <summary>
		/// Name of the file mapping (section) object.
		/// </summary>
		private string sectionName;


		/// <summary>Handle of the section object.</summary>
		private IntPtr sectionHandle;

		/// <summary>Handle of the server write event object.</summary>
		private IntPtr serverWrEventHandle;

		/// <summary>Handle of the client write event object.</summary>
		private IntPtr clientWrEventHandle;

		/// <summary>Handle of the server read event object.</summary>
		private IntPtr serverRdEventHandle;

		/// <summary>Handle of the client read event object.</summary>
		private IntPtr clientRdEventHandle;

		/// <summary>Handle of the thread death mutex object.</summary>
		private IntPtr threadMutexHandle;


		/// <summary>Size of the shared memory block.</summary>
		private const uint SECTION_SIZE = 32768;

		/// <summary>Size of each of the two transfer blocks.</summary>
		private const uint TRANSFER_BLOCK_SIZE = SECTION_SIZE / 2;

		/// <summary>Size of each of the two message blocks.</summary>
		private const uint MESSAGE_BLOCK_SIZE = TRANSFER_BLOCK_SIZE - 4;


		/// <summary>Address in virtual address space where the block of shared memory begins.</summary>
		private IntPtr viewAddr;

		/// <summary>Address in virtual address space where the write transfer block begins.</summary>
		private IntPtr writeSizeAddr;

		/// <summary>Address in virtual address space where the read transfer block begins.</summary>
		private IntPtr readSizeAddr;

		/// <summary>Address in virtual address space where the write message block begins.</summary>
		private IntPtr writeMsgAddr;

		/// <summary>Address in virtual address space where the read message block begins.</summary>
		private IntPtr readMsgAddr;

		/// <summary>
		/// The handles to apply <see cref="ShmNative.WaitForMultipleObjects"/> on when waiting
		/// for incoming data at the client end.
		/// </summary>
		/// <remarks>
		/// Index 0 should contain handle to the client read event, index 1 handle to the mutex
		/// owned by the server thread that serves this client thread. Valid only for clients, 
		/// who pass this array to <see cref="ShmNative.WaitForMultipleObjects"/>.
		/// </remarks>
		private IntPtr[] clientReadHandles = new IntPtr[2];

		/// <summary>
		/// The handles to apply <see cref="ShmNative.WaitForMultipleObjects"/> on when waiting
		/// for incoming data at the server end.
		/// </summary>
		/// <remarks>
		/// Index 0 should contain handle to the server read event, index 1 handle to stop
		/// listening event. Valid only for servers, who pass this array to
		/// <see cref="ShmNative.WaitForMultipleObjects"/>.
		/// </remarks>>
		private IntPtr[] serverReadHandles = new IntPtr[2];

		/// <summary>Timeout when waiting for various synchronization objects.</summary>
		private const uint waitTimeout = 5000;


		/// <summary>A <see cref="Stream"/> containing the incoming/outgoing data.</summary>
		private MemoryStream stream;

		/// <summary>A writer connected to <see cref="stream"/>.</summary>
		private BinaryWriter writer;

		/// <summary>A reader connected to <see cref="stream"/>.</summary>
		private BinaryReader reader;


		/// <summary>Common prefix of <see cref="sectionHandle"/> names.</summary>
		private const string fileMappingPrefix = "Global\\ShmChannel_section_";

		/// <summary>Common prefix of <see cref="serverRdEventHandle"/> names.</summary>
		private const string srvrRdEventPrefix = "Global\\ShmChannel_srvrrdevent_";

		/// <summary>Common prefix of <see cref="serverWrEventHandle"/> names.</summary>
		private const string srvrWrEventPrefix = "Global\\ShmChannel_srvrwrevent_";

		/// <summary>Common prefix of <see cref="clientRdEventHandle"/> names.</summary>
		private const string clntRdEventPrefix = "Global\\ShmChannel_clntrdevent_";

		/// <summary>Common prefix of <see cref="clientWrEventHandle"/> names.</summary>
		private const string clntWrEventPrefix = "Global\\ShmChannel_clntwrevent_";

		/// <summary>Common prefix of <see cref="threadMutexHandle"/> names.</summary>
		private const string threadMutexPrefix = "Global\\ShmChannel_thrdmutex_";

		#endregion

		#region Construction and initialization

		/// <summary>
		/// Constructs a new shared memory connection object.
		/// </summary>
		/// <param name="sectionName">File mapping (section) name.</param>
		/// <param name="create">Specifies whether the section should be created.</param>
		public ShmConnection(string sectionName, bool create)
		{
			this.sectionName = sectionName;

			// if we are requested to create the section, we are the server
			serverSide = create;

			if (create) Create();
			else Connect();
		}

		/// <summary>
		/// Constructs a new shared memory connection object.
		/// </summary>
		/// <param name="sectionName">File mapping (section) name.</param>
		/// <param name="create">Specifies whether the section should be created.</param>
		/// <param name="stopListeningEventHandle">Handle of the stop listening event.</param>
		public ShmConnection(string sectionName, bool create, IntPtr stopListeningEventHandle)
			: this(sectionName, create)
		{
			serverReadHandles[0] = serverRdEventHandle;
			serverReadHandles[1] = stopListeningEventHandle;
		}

		/// <summary>
		/// Creates a new file mapping (section) object and maps a view of it to the virtual
		/// address space.
		/// </summary>
		/// <exception cref="ShmIOException">Creation of unmanaged resources failed.</exception>
		private void Create()
		{
			Debug.WriteLineIf(ShmChannel.verbose, "About to create connection section " + sectionName);

			string file_mapping_name = fileMappingPrefix + sectionName;

			// create file mapping object
			sectionHandle = ShmNative.CreateFileMapping(
				ShmNative.INVALID_HANDLE_VALUE,
				ShmNative.securityAttributes,
				ShmNative.PAGE_READWRITE | ShmNative.SEC_COMMIT,
				0,
				SECTION_SIZE,
				file_mapping_name);

			if (sectionHandle == IntPtr.Zero)
			{
				throw new ShmIOException(ShmResources.GetString("creating_file_mapping_failed", file_mapping_name),
					Marshal.GetLastWin32Error());
			}

			// map it into our address space
			viewAddr = ShmNative.MapViewOfFile(sectionHandle, ShmNative.FILE_MAP_WRITE, 0, 0, SECTION_SIZE);

			if (viewAddr == IntPtr.Zero)
			{
				throw new ShmIOException(ShmResources.GetString("mapping_view_failed", file_mapping_name),
					Marshal.GetLastWin32Error());
			}

			// create events
			serverRdEventHandle = ShmNative.CreateNamedEvent(srvrRdEventPrefix + sectionName, false);
			serverWrEventHandle = ShmNative.CreateNamedEvent(srvrWrEventPrefix + sectionName, true);
			clientRdEventHandle = ShmNative.CreateNamedEvent(clntRdEventPrefix + sectionName, false);
			clientWrEventHandle = ShmNative.CreateNamedEvent(clntWrEventPrefix + sectionName, true);

			// create mutex
			threadMutexHandle = ShmNative.CreateNamedMutex(threadMutexPrefix + sectionName, true);

			if (Marshal.GetLastWin32Error() == ShmNative.ERROR_ALREADY_EXISTS)
			{
				ShmNative.WaitForSingleObject(threadMutexHandle, ShmNative.INFINITE);
			}

			// set pointers
			writeSizeAddr = viewAddr;
			writeMsgAddr = (IntPtr)((uint)writeSizeAddr + 4);
			readSizeAddr = (IntPtr)((uint)viewAddr + TRANSFER_BLOCK_SIZE);
			readMsgAddr = (IntPtr)((uint)readSizeAddr + 4);

			Debug.WriteLineIf(ShmChannel.verbose, "created OK");
		}

		/// <summary>
		/// Opens an existing file mapping (section) object and maps a view of it to the virtual
		/// address space.
		/// </summary>
		/// <exception cref="ShmIOException">Creation of unmanaged resources failed.</exception>
		private void Connect()
		{
			Debug.WriteLineIf(ShmChannel.verbose, "About to connect to connection section " + sectionName);

			string file_mapping_name = fileMappingPrefix + sectionName;

			// open file mapping object
			sectionHandle = ShmNative.OpenFileMapping(
				ShmNative.FILE_MAP_READ | ShmNative.FILE_MAP_WRITE,
				false,
				file_mapping_name);

			if (sectionHandle == IntPtr.Zero)
			{
				throw new ShmIOException(ShmResources.GetString("opening_file_mapping_failed", file_mapping_name),
					Marshal.GetLastWin32Error());
			}

			// map it into our address space
			viewAddr = ShmNative.MapViewOfFile(sectionHandle, ShmNative.FILE_MAP_WRITE, 0, 0, SECTION_SIZE);

			if (viewAddr == IntPtr.Zero)
			{
				throw new ShmIOException(ShmResources.GetString("mapping_view_failed", file_mapping_name),
					Marshal.GetLastWin32Error());
			}

			// open events
			serverRdEventHandle = ShmNative.OpenNamedEvent(srvrRdEventPrefix + sectionName);
			serverWrEventHandle = ShmNative.OpenNamedEvent(srvrWrEventPrefix + sectionName);
			clientRdEventHandle = ShmNative.OpenNamedEvent(clntRdEventPrefix + sectionName);
			clientWrEventHandle = ShmNative.OpenNamedEvent(clntWrEventPrefix + sectionName);

			// open mutex
			threadMutexHandle = ShmNative.CreateNamedMutex(threadMutexPrefix + sectionName, true);

			// set pointers
			readSizeAddr = viewAddr;
			readMsgAddr = (IntPtr)((uint)readSizeAddr + 4);
			writeSizeAddr = (IntPtr)((uint)viewAddr + TRANSFER_BLOCK_SIZE);
			writeMsgAddr = (IntPtr)((uint)writeSizeAddr + 4);

			clientReadHandles[0] = clientRdEventHandle;
			clientReadHandles[1] = threadMutexHandle;

			Debug.WriteLineIf(ShmChannel.verbose, "connected OK");
		}

		#endregion

		#region Connection ageing support

		/// <summary>
		/// Number of milliseconds of inactivity that must elapse to consider this connection stale.
		/// </summary>
		internal const int defaultAgeLastTimeAccessed = 10000;

		/// <summary>
		/// The value of <see cref="Environment.TickCount"/> last time an activity was recorded.
		/// </summary>
		private int lastTimeAccessed = -1;

		/// <summary>
		/// Updates <see cref="lastTimeAccessed"/>.
		/// </summary>
		internal void UpdateLastTimeAccessed()
		{
			lastTimeAccessed = Environment.TickCount;
		}

		/// <summary>
		/// Determines whether this connection is alive or stale.
		/// </summary>
		/// <returns><B>true</B> if this connection if stale, <B>false</B> otherwise.</returns>
		public bool IsConnectionStale()
		{
			long now = Environment.TickCount;
			long result = now - lastTimeAccessed;

			// did we wrap 24.9 days?
			if (result < 0) result = (Int32.MaxValue - lastTimeAccessed) + now;

			return (result > defaultAgeLastTimeAccessed);
		}

		#endregion

		#region Buffer writing helpers

		/// <summary>
		/// Writes a byte array into the outgoing buffer.
		/// </summary>
		/// <param name="buffer">The byte array to save.</param>
		public void Write(byte[] buffer)
		{
			Write(buffer, buffer.Length);
		}

		/// <summary>
		/// Writes a byte array into the outgoing buffer.
		/// </summary>
		/// <param name="buffer">The byte array to save.</param>
		/// <param name="length">Length of the byte array.</param>
		public void Write(byte[] buffer, int length)
		{
			writer.Write(length);
			writer.Write(buffer);
		}

		/// <summary>
		/// Writes an <c>ushort</c> into the outgoing buffer.
		/// </summary>
		/// <param name="val">The <c>ushort</c> to save.</param>
		public void Write(ushort val)
		{
			writer.Write(val);
		}

		/// <summary>
		/// Writes an <c>int</c> into the outgoing buffer.
		/// </summary>
		/// <param name="val">The <c>int</c> to save.</param>
		public void Write(int val)
		{
			writer.Write(val);
		}

		/// <summary>
		/// Writes a <see cref="String"/> into the outgoing buffer.
		/// </summary>
		/// <param name="str">The <see cref="String"/> to save.</param>
		public void Write(string str)
		{
			if (str == null) str = String.Empty;
			writer.Write(str);
		}

		/// <summary>
		/// Writes a <see cref="Stream"/> into the outgoing buffer.
		/// </summary>
		/// <param name="str">The <see cref="Stream"/> to save.</param>
		public void Write(Stream str)
		{
			const int chunk = 512;
			byte[] buffer = new byte[chunk];

			writer.Write((int)str.Length);

			int len;
			while ((len = str.Read(buffer, 0, chunk)) > 0)
			{
				writer.Write(buffer);
			}
		}

		#endregion

		#region Buffer reading helpers

		/// <summary>
		/// Reads a <see cref="Stream"/> from the incoming buffer.
		/// </summary>
		/// <returns>The <see cref="Stream"/>.</returns>
		public Stream ReadStream()
		{
			int length = reader.ReadInt32();
			byte[] buffer = reader.ReadBytes(length);

			return new MemoryStream(buffer, false);
		}

		/// <summary>
		/// Reads a byte array from the incoming buffer.
		/// </summary>
		/// <param name="length">Length of the array.</param>
		/// <returns>The byte array.</returns>
		public byte[] ReadBytes(int length)
		{
			return reader.ReadBytes(length);
		}

		/// <summary>
		/// Reads an <c>ushort</c> from the incoming buffer.
		/// </summary>
		/// <returns>The <c>ushort</c>.</returns>
		public ushort ReadUShort()
		{
			return reader.ReadUInt16();
		}

		/// <summary>
		/// Reads an <c>int</c> from the incoming buffer.
		/// </summary>
		/// <returns>The <c>int</c>.</returns>
		public int ReadInt()
		{
			return reader.ReadInt32();
		}

		/// <summary>
		/// Reads a <see cref="String"/> from the incoming buffer.
		/// </summary>
		/// <returns>The <see cref="String"/>.</returns>
		public string ReadString()
		{
			return reader.ReadString();
		}

		#endregion

		#region BeginWriteMessage and EndWriteMessage

		/// <summary>
		/// Prepares buffers for the outgoing message.
		/// </summary>
		public void BeginWriteMessage()
		{
			stream = new MemoryStream(128);
			writer = new BinaryWriter(stream, Encoding.UTF8);
		}

		/// <summary>
		/// Transmits the message stored in <see cref="stream"/> through the shared memory.
		/// </summary>
		/// <exception cref="ShmIOException">Waiting for a synchronization object failed.</exception>
		public void EndWriteMessage()
		{
			uint bytes_to_write = (uint)stream.Length;
			uint src_index = 0;
			byte[] msg_bytes = stream.GetBuffer();

			writer.Flush();

			do
			{
				// wait until it is safe to write
				switch (ShmNative.WaitForSingleObject(
					serverSide ? serverWrEventHandle : clientWrEventHandle,
					waitTimeout))
				{
					case ShmNative.WAIT_TIMEOUT:
					{
						throw new ShmIOException(ShmResources.GetString(serverSide ?
							"timeout_waiting_for_server_event" : "timeout_waiting_for_client_event"));
					}

					case ShmNative.WAIT_FAILED:
					{
						throw new ShmIOException(ShmResources.GetString(serverSide ?
							"waiting_for_server_event_failed" : "waiting_for_client_event_failed",
							Marshal.GetLastWin32Error()));
					}
				}

				Marshal.WriteInt32(writeSizeAddr, (int)bytes_to_write);
				uint chunk_size = Math.Min(bytes_to_write, MESSAGE_BLOCK_SIZE);

				Marshal.Copy(msg_bytes, (int)src_index, writeMsgAddr, (int)chunk_size);

				// indicate to the other side that it is safe to read
				ShmNative.SetEvent(serverSide ? clientRdEventHandle : serverRdEventHandle);

				bytes_to_write -= chunk_size;
				src_index += chunk_size;

			}
			while (bytes_to_write > 0);

			stream = null;
			writer = null;
		}

		#endregion

		#region BeginReadMessage and EndReadMessage

		/// <summary>
		/// Receives the message from shared memory into <see cref="stream"/>.
		/// </summary>
		/// <exception cref="ShmIOException">Waiting for a synchronization object failed.</exception>
		public bool BeginReadMessage()
		{
			uint bytes_to_read = 0, dst_index = 0;
			byte[] msg_bytes = null;

			do
			{
				uint wait_result;
				if (serverSide)
				{
					// timeout is 3 times longer than for the client, so the server side of the
					// connection should never be disposed of before the client side is
					wait_result = ShmNative.WaitForMultipleObjects(
						2,
						serverReadHandles,
						false,
						(uint)(defaultAgeLastTimeAccessed * 3));
				}
				else
				{
					// client is waiting for server's reply... or server thread's death
					wait_result = ShmNative.WaitForMultipleObjects(
						2,
						clientReadHandles,
						false,
						ShmNative.INFINITE);
				}

				switch (wait_result)
				{
					case ShmNative.WAIT_TIMEOUT:
					case ShmNative.WAIT_OBJECT_0 + 1:
					case ShmNative.WAIT_ABANDONED_0 + 1:
					{
						ShmNative.ReleaseMutex(threadMutexHandle);

						// timeout
						return false;
					}

					case ShmNative.WAIT_FAILED:
					{
						throw new ShmIOException(ShmResources.GetString(serverSide ?
							"waiting_for_server_event_failed" : "waiting_for_client_event_failed",
							Marshal.GetLastWin32Error()));
					}
				}

				// if this is the first chunk, determine the length of the message
				if (dst_index == 0)
				{
					bytes_to_read = (uint)Marshal.ReadInt32(readSizeAddr);
					msg_bytes = new byte[bytes_to_read];
				}
				uint chunk_size = Math.Min(bytes_to_read, MESSAGE_BLOCK_SIZE);

				Marshal.Copy(readMsgAddr, msg_bytes, (int)dst_index, (int)chunk_size);

				// indicate to the other side that it is safe to write
				ShmNative.SetEvent(serverSide ? clientWrEventHandle : serverWrEventHandle);

				bytes_to_read -= chunk_size;
				dst_index += chunk_size;

			}
			while (bytes_to_read > 0);

			stream = new MemoryStream(msg_bytes, false);
			reader = new BinaryReader(stream, Encoding.UTF8);

			return true;
		}

		/// <summary>
		/// Resets the incoming buffer.
		/// </summary>
		public void EndReadMessage()
		{
			stream = null;
			reader = null;
		}

		#endregion

		#region WriteHeaders and ReadHeaders

		/// <summary>Header delimiter - more headers follow.</summary>
		private const ushort headerMarker = 0xFFA1;

		/// <summary>Header delimiter - no more headers.</summary>
		private const ushort headerEndMarker = 0xFFA2;

		/// <summary>
		/// Writes transport headers into the outgoing buffer.
		/// </summary>
		/// <param name="uri">The request URI.</param>
		/// <param name="headers">The headers to write.</param>
		public void WriteHeaders(string uri, ITransportHeaders headers)
		{
			Write(uri); // Write the URI

			// since we cannot count the headers, just begin writing counted strings
			// we'll write a terminator marker at the end
			foreach (DictionaryEntry header in headers)
			{
				string header_name = (string)header.Key;

				// ignore headers beginning with "__"
				if (header_name.Length < 2 || header_name[0] != '_' || header_name[1] != '_')
				{
					Write(headerMarker);

					Write(header_name);
					Write(header.Value.ToString());
				}
				else Debug.WriteLineIf(ShmChannel.verbose, "ShmConnection.WriteHeaders: Ignoring header " + header_name);
			}

			Write(headerEndMarker);
		}

		/// <summary>
		/// Reads transport headers from the incoming buffer.
		/// </summary>
		/// <returns>The headers.</returns>
		public ITransportHeaders ReadHeaders()
		{
			TransportHeaders headers = new TransportHeaders();

			// read uri (and make sure that no channel specific data is present)
			string uri = ReadString();

			if (uri != null && uri != "")
			{
				string obj_uri;
				string chan_uri = ShmConnection.Parse(uri, out obj_uri);
				if (chan_uri == null) obj_uri = uri;

				headers[CommonTransportKeys.RequestUri] = obj_uri;
			}

			// read to end of headers
			ushort marker = ReadUShort();
			while (marker == headerMarker)
			{
				string hname = ReadString();
				string hvalue = ReadString();

				headers[hname] = hvalue;

				marker = ReadUShort();
			}

			return headers;
		}

		#endregion

		#region Parse

		/// <summary>
		/// Returns the object URI as an out parameter, and the URI of the current channel as the return value.
		/// </summary>
		/// <param name="url">The URL of the object.</param>
		/// <param name="objUri">When this method returns, contains a <see cref="String"/> that holds 
		/// the object URI.</param>
		/// <returns>The URI of the current channel, or a <B>null</B> reference if the URI does not belong to
		/// this channel.</returns>
		internal static string Parse(string url, out string objUri)
		{
			// format is shm://sectionname/objname

			string shm_name = null;
			Debug.WriteLineIf(ShmChannel.verbose, "Parse: IN: url = " + url);

			string url_compare = url.ToLower();

			// starts with shm:// ?
			string prefix = ShmChannel.channelScheme + "://";
			if (!url_compare.StartsWith(prefix))
			{
				objUri = null;
				return null;
			}

			// skip the shm://
			int start = prefix.Length;
			int end = url.IndexOf("/", start);

			if (end > start)
			{
				shm_name = url.Substring(start, end - start);
				objUri = url.Substring(end + 1);
			}
			else
			{
				shm_name = url.Substring(start);
				objUri = null;
			}

			Debug.WriteLineIf(ShmChannel.verbose, "Parse: OUT: shmName = " + shm_name + ", objUri = " + objUri);

			return shm_name;
		}

		#endregion

		#region IDisposable implementation and related members

		/// <summary>
		/// Standard <see cref="IDisposable.Dispose"/> implementation.
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Disposes of unmanaged and optionally also managed resources.
		/// </summary>
		/// <param name="disposing">If <B>true</B>, both managed and unmanaged resources should be released.
		/// If <B>false</B> only unmanaged resources should be released.</param>
		private void Dispose(bool disposing)
		{
			Debug.WriteLineIf(ShmChannel.verbose, "Disposing " + (serverSide ? "server" : "client") +
				" connection.");

			if (viewAddr != IntPtr.Zero)
			{
				ShmNative.UnmapViewOfFile(viewAddr);
				viewAddr = IntPtr.Zero;
			}

			ShmNative.CloseHandleOnce(ref threadMutexHandle);
			ShmNative.CloseHandleOnce(ref sectionHandle);

			ShmNative.CloseHandleOnce(ref serverRdEventHandle);
			ShmNative.CloseHandleOnce(ref serverWrEventHandle);
			ShmNative.CloseHandleOnce(ref clientRdEventHandle);
			ShmNative.CloseHandleOnce(ref clientWrEventHandle);
		}

		/// <summary>
		/// Destructor that will run only if the <see cref="Dispose()"/> method does not get called.
		/// </summary>
		~ShmConnection()
		{
			Dispose(false);
		}

		#endregion
	}

	#region Connection pooling support

	/// <summary>
	/// Connection pool manager. Manages <see cref="ShmConnectionPool"/> instances.
	/// Note that there is a separate connection pool for each section name.
	/// </summary>
	internal sealed class ShmConnectionPoolManager
	{
		/// <summary>A timer that is used to periodically check for stale connections.</summary>
		private static Timer timer;

		/// <summary>Connection pool collection.</summary>
		private static Hashtable poolInstances = new Hashtable();

		/// <summary>
		/// Private constructor to prevent instantiation.
		/// </summary>
		private ShmConnectionPoolManager()
		{ }

		/// <summary>
		/// Static constructor. Initializes the <see cref="timer"/>.
		/// </summary>
		static ShmConnectionPoolManager()
		{
			// setup the timer
			TimerCallback timer_delegate = new TimerCallback(ShmConnectionPoolManagerCallback);
			timer = new Timer(
				timer_delegate,
				null,
				ShmConnection.defaultAgeLastTimeAccessed,
				ShmConnection.defaultAgeLastTimeAccessed);
		}

		/// <summary>
		/// Cleans up static members.
		/// </summary>
		public static void Cleanup()
		{
			// stop the timer
			timer.Dispose();
		}

		/// <summary>
		/// Timer callback.
		/// </summary>
		/// <param name="state">Not used.</param>
		private static void ShmConnectionPoolManagerCallback(object state)
		{
			lock (poolInstances)
			{
				foreach (DictionaryEntry entry in poolInstances)
				{
					ShmConnectionPool pool = (ShmConnectionPool)entry.Value;
					pool.CloseStaleConnections();
				}
			}
		}

		/// <summary>
		/// Looks up an <see cref="ShmConnectionPool"/> according to a given section name.
		/// </summary>
		/// <param name="sectionName">The section name.</param>
		/// <returns>The <see cref="ShmConnectionPool"/> containing zero or more 
		/// <see cref="ShmConnection"/>s that are connected to <paramref name="sectionName"/>.</returns>
		public static ShmConnectionPool LookupPool(string sectionName)
		{
			ShmConnectionPool pool;

			lock (poolInstances)
			{
				pool = (ShmConnectionPool)poolInstances[sectionName];

				if (pool == null)
				{
					pool = new ShmConnectionPool();
					poolInstances[sectionName] = pool;
				}
			}
			return pool;
		}
	}

	/// <summary>
	/// Connection pool. Stores <see cref="ShmConnection"/> instances that are ready for reuse.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Whenever an <see cref="ShmConnection"/> instance is needed, the <see cref="Obtain"/> method
	/// of the corresponding <see cref="ShmConnectionPool"/> should be invoked to check whether there
	/// is a cached connection waiting to be reused. When a connection is no longer needed (could be
	/// a connection gotten from the pool as well as a fresh connection created with <c>new</c>), it
	/// should be returned to the pool using the <see cref="ReturnToPool"/> method.
	/// </para>
	/// <para>
	/// Connections are stored in a <see cref="Stack"/>.
	/// </para>
	/// </remarks>
	internal sealed class ShmConnectionPool : IDisposable
	{
		/// <summary>
		/// The stack of cached connections.
		/// </summary>
		private Stack connectionStack;

		/// <summary>
		/// Creates a new <see cref="ShmConnectionPool"/>.
		/// </summary>
		public ShmConnectionPool()
		{
			connectionStack = new Stack();
		}

		/// <summary>
		/// Closes all connections.
		/// </summary>
		public void Dispose()
		{
			CloseAllConnections();
		}

		/// <summary>
		/// Closes connections that have not been used for a long time.
		/// </summary>
		public void CloseStaleConnections()
		{
			Stack active_connections = new Stack();

			lock (connectionStack)
			{
				foreach (ShmConnection connection in connectionStack)
				{
					if (connection.IsConnectionStale()) connection.Dispose();
					else active_connections.Push(connection);
				}

				connectionStack = active_connections;
			}
		}

		/// <summary>
		/// Closes all connections.
		/// </summary>
		public void CloseAllConnections()
		{
			lock (connectionStack)
			{
				foreach (ShmConnection connection in connectionStack)
				{
					connection.Dispose();
				}
				connectionStack.Clear();
			}
		}

		/// <summary>
		/// Tries to obtain a connection from the pool.
		/// </summary>
		/// <returns>The <see cref="ShmConnection"/> or <B>null</B> if there no connections in this pool.</returns>
		public ShmConnection Obtain()
		{
			ShmConnection connection = null;

			lock (connectionStack)
			{
				int count = connectionStack.Count;
				if (count > 0) connection = (ShmConnection)connectionStack.Pop();
			}

			return connection;
		}

		/// <summary>
		/// Puts a <see cref="ShmConnection"/> to the pool for reuse.
		/// </summary>
		/// <param name="connection">The connection.</param>
		public void ReturnToPool(ShmConnection connection)
		{
			lock (connectionStack)
			{
				connection.UpdateLastTimeAccessed();
				connectionStack.Push(connection);
			}
		}
	}

	#endregion
}
