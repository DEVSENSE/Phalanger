/*

 Copyright (c) 2004-2006 Tomas Matousek and Jan Benda.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

/*
 TODO:
 - implement all functions
 - Added (PHP 5.1.0):
   stream_socket_enable_crypto()
*/

using System;
using System.IO;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Collections;
using System.Threading;

using PHP.Core;

namespace PHP.Library
{
	/// <summary>
	/// Gives access to various network-based stream properties.
	/// </summary>
	/// <threadsafety static="true"/>
	public static class StreamSocket
	{
		#region Enums

		/// <summary>
		/// Options used for <see cref="StreamSocket.Connect"/>.
		/// </summary>
		public enum SocketOptions
		{
			/// <summary>
			/// Default option.
			/// </summary>
			None = 0,

			/// <summary>
			/// Client socket opened with <c>stream_socket_client</c> should remain persistent 
			/// between page loads.
			/// </summary>
			[ImplementsConstant("STREAM_CLIENT_PERSISTENT")]
			Persistent = 1,

			/// <summary>
			/// Open client socket asynchronously.
			/// </summary>
			[ImplementsConstant("STREAM_CLIENT_ASYNC_CONNECT")]
			Asynchronous = 2
		}

		public enum _AddressFamily
		{
			[ImplementsConstant("STREAM_PF_INET")]
			InterNetwork = AddressFamily.InterNetwork,
			[ImplementsConstant("STREAM_PF_INET6")]
			InterNetworkV6 = AddressFamily.InterNetworkV6,
			[ImplementsConstant("STREAM_PF_UNIX")]
			Unix = AddressFamily.Unix
		}

		public enum _SocketType
		{
			Unknown = SocketType.Unknown,
			[ImplementsConstant("STREAM_SOCK_STREAM")]
			Stream = SocketType.Stream,
			[ImplementsConstant("STREAM_SOCK_DGRAM")]
			Dgram = SocketType.Dgram,
			[ImplementsConstant("STREAM_SOCK_RAW")]
			Raw = SocketType.Raw,
			[ImplementsConstant("STREAM_SOCK_RDM")]
			Rdm = SocketType.Rdm,
			[ImplementsConstant("STREAM_SOCK_SEQPACKET")]
			Seqpacket = SocketType.Seqpacket,
		}

		public enum _ProtocolType
		{
			[ImplementsConstant("STREAM_IPPROTO_IP")]
			IP = ProtocolType.IP,
			[ImplementsConstant("STREAM_IPPROTO_ICMP")]
			Icmp = ProtocolType.Icmp,
			[ImplementsConstant("STREAM_IPPROTO_TCP")]
			Tcp = ProtocolType.Tcp,
			[ImplementsConstant("STREAM_IPPROTO_UDP")]
			Udp = ProtocolType.Udp,
			[ImplementsConstant("STREAM_IPPROTO_RAW")]
			Raw = ProtocolType.Raw
		}

		public enum SendReceiveOptions
		{
			None = 0,
			[ImplementsConstant("STREAM_OOB")]
			OutOfBand = 1,
			[ImplementsConstant("STREAM_PEEK")]
			Peek = 2
		}

		#endregion

		#region TODO: stream_get_transports, stream_socket_get_name

		/// <summary>Retrieve list of registered socket transports</summary>  
        [ImplementsFunction("stream_get_transports", FunctionImplOptions.NotSupported)]
		public static PhpArray GetTransports()
		{
			PhpException.FunctionNotSupported();
			return null;
		}

		/// <summary>
		/// Retrieve the name of the local or remote sockets.
		/// </summary>
        [ImplementsFunction("stream_socket_get_name", FunctionImplOptions.NotSupported)]
		public static string SocketGetName(PhpResource handle, bool wantPeer)
		{
			PhpException.FunctionNotSupported();
			return null;
		}

		#endregion

		#region TODO: stream_socket_client

		/// <summary>
		/// Open client socket.
		/// </summary>
		[ImplementsFunction("stream_socket_client")]
		public static PhpResource ConnectClient(string remoteSocket)
		{
			int errno;
			string errstr;
			return Connect(remoteSocket, 0, out errno, out errstr, Double.NaN, SocketOptions.None, StreamContext.Default);
		}

		/// <summary>
		/// Open client socket.
		/// </summary>
		[ImplementsFunction("stream_socket_client")]
		public static PhpResource ConnectClient(string remoteSocket, out int errno)
		{
			string errstr;
			return Connect(remoteSocket, 0, out errno, out errstr, Double.NaN, SocketOptions.None, StreamContext.Default);
		}

		/// <summary>
		/// Open client socket.
		/// </summary>
		[ImplementsFunction("stream_socket_client")]
		public static PhpResource ConnectClient(string remoteSocket, out int errno, out string errstr)
		{
			return Connect(remoteSocket, 0, out errno, out errstr, Double.NaN, SocketOptions.None, StreamContext.Default);
		}

		/// <summary>
		/// Open client socket.
		/// </summary>
		[ImplementsFunction("stream_socket_client")]
		public static PhpResource ConnectClient(string remoteSocket, out int errno, out string errstr,
		  double timeout)
		{
			return Connect(remoteSocket, 0, out errno, out errstr, timeout, SocketOptions.None, StreamContext.Default);
		}

		/// <summary>
		/// Open client socket.
		/// </summary>
		[ImplementsFunction("stream_socket_client")]
		public static PhpResource ConnectClient(string remoteSocket, out int errno, out string errstr,
		  double timeout, SocketOptions flags)
		{
			return Connect(remoteSocket, 0, out errno, out errstr, timeout, flags, StreamContext.Default);
		}

		/// <summary>
		/// Open client socket.
		/// </summary>
		[ImplementsFunction("stream_socket_client")]
		public static PhpResource ConnectClient(string remoteSocket, out int errno, out string errstr,
		  double timeout, SocketOptions flags, PhpResource context)
		{
			StreamContext sc = StreamContext.GetValid(context);
			if (sc == null)
			{
				errno = -1;
				errstr = null;
				return null;
			}

			return Connect(remoteSocket, 0, out errno, out errstr, timeout, flags, sc);
		}

		#endregion

		#region TODO: stream_socket_server

		/// <summary>
		/// Open client socket.
		/// </summary>
		[ImplementsFunction("stream_socket_server")]
		public static PhpResource ConnectServer(string localSocket)
		{
			int errno;
			string errstr;
			return Connect(localSocket, 0, out errno, out errstr, Double.NaN, SocketOptions.None, StreamContext.Default);
		}

		/// <summary>
		/// Open client socket.
		/// </summary>
		[ImplementsFunction("stream_socket_server")]
		public static PhpResource ConnectServer(string localSocket, out int errno)
		{
			string errstr;
			return Connect(localSocket, 0, out errno, out errstr, Double.NaN, SocketOptions.None, StreamContext.Default);
		}

		/// <summary>
		/// Open client socket.
		/// </summary>
		[ImplementsFunction("stream_socket_server")]
		public static PhpResource ConnectServer(string localSocket, out int errno, out string errstr)
		{
			return Connect(localSocket, 0, out errno, out errstr, Double.NaN, SocketOptions.None, StreamContext.Default);
		}

		/// <summary>
		/// Open client socket.
		/// </summary>
		[ImplementsFunction("stream_socket_server")]
		public static PhpResource ConnectServer(string localSocket, out int errno, out string errstr,
		  double timeout)
		{
			return Connect(localSocket, 0, out errno, out errstr, timeout, SocketOptions.None, StreamContext.Default);
		}

		/// <summary>
		/// Open client socket.
		/// </summary>
		[ImplementsFunction("stream_socket_server")]
		public static PhpResource ConnectServer(string localSocket, out int errno, out string errstr,
		  double timeout, SocketOptions flags)
		{
			return Connect(localSocket, 0, out errno, out errstr, timeout, flags, StreamContext.Default);
		}

		/// <summary>
		/// Open client socket.
		/// </summary>
		[ImplementsFunction("stream_socket_server")]
		public static PhpResource ConnectServer(string localSocket, out int errno, out string errstr,
		  double timeout, SocketOptions flags, PhpResource context)
		{
			StreamContext sc = StreamContext.GetValid(context);
			if (sc == null)
			{
				errno = -1;
				errstr = null;
				return null;
			}

			return Connect(localSocket, 0, out errno, out errstr, timeout, flags, sc);
		}

		#endregion

		#region TODO: stream_socket_accept

		/// <summary>
		/// Accepts a connection on a server socket.
		/// </summary>
        [ImplementsFunction("stream_socket_accept", FunctionImplOptions.NotSupported)]
		public static bool Accept(PhpResource serverSocket)
		{
			string peerName;
			return Accept(serverSocket, Configuration.Local.FileSystem.DefaultSocketTimeout, out peerName);
		}

		/// <summary>
		/// Accepts a connection on a server socket.
		/// </summary>
        [ImplementsFunction("stream_socket_accept", FunctionImplOptions.NotSupported)]
		public static bool Accept(PhpResource serverSocket, int timeout)
		{
			string peerName;
			return Accept(serverSocket, timeout, out peerName);
		}

		/// <summary>
		/// Accepts a connection on a server socket.
		/// </summary>
        [ImplementsFunction("stream_socket_accept", FunctionImplOptions.NotSupported)]
		public static bool Accept(PhpResource serverSocket, int timeout, out string peerName)
		{
			peerName = "";

			SocketStream stream = SocketStream.GetValid(serverSocket);
			if (stream == null) return false;

			PhpException.FunctionNotSupported();
			return false;
		}

		#endregion

		#region TODO: stream_socket_recvfrom

        [ImplementsFunction("stream_socket_recvfrom", FunctionImplOptions.NotSupported)]
		public static string ReceiveFrom(PhpResource socket, int length)
		{
			string address;
			return ReceiveFrom(socket, length, SendReceiveOptions.None, out address);
		}

        [ImplementsFunction("stream_socket_recvfrom", FunctionImplOptions.NotSupported)]
		public static string ReceiveFrom(PhpResource socket, int length, SendReceiveOptions flags)
		{
			string address;
			return ReceiveFrom(socket, length, flags, out address);
		}

        [ImplementsFunction("stream_socket_recvfrom", FunctionImplOptions.NotSupported)]
		public static string ReceiveFrom(PhpResource socket, int length, SendReceiveOptions flags,
		  out string address)
		{
			address = null;

			SocketStream stream = SocketStream.GetValid(socket);
			if (stream == null) return null;

			PhpException.FunctionNotSupported();
			return null;
		}

		#endregion

		#region TODO: stream_socket_sendto

        [ImplementsFunction("stream_socket_sendto", FunctionImplOptions.NotSupported)]
		public static int SendTo(PhpResource socket, string data)
		{
			return SendTo(socket, data, SendReceiveOptions.None, null);
		}

        [ImplementsFunction("stream_socket_sendto", FunctionImplOptions.NotSupported)]
		public static int SendTo(PhpResource socket, string data, SendReceiveOptions flags)
		{
			return SendTo(socket, data, flags, null);
		}

        [ImplementsFunction("stream_socket_sendto", FunctionImplOptions.NotSupported)]
		public static int SendTo(PhpResource socket, string data, SendReceiveOptions flags,
		  string address)
		{
			SocketStream stream = SocketStream.GetValid(socket);
			if (stream == null) return -1;

			PhpException.FunctionNotSupported();
			return -1;
		}

		#endregion

		#region TODO: stream_socket_pair

        //[ImplementsFunction("stream_socket_pair", FunctionImplOptions.NotSupported)]
		public static PhpArray CreatePair(ProtocolFamily protocolFamily, SocketType type, ProtocolType protocol)
		{
			PhpException.FunctionNotSupported();
			return null;
		}

		#endregion

		#region Connect

		/// <summary>
		/// Opens a new SocketStream
		/// </summary>
		internal static SocketStream Connect(string remoteSocket, int port, out int errno, out string errstr,
		  double timeout, SocketOptions flags, StreamContext/*!*/ context)
		{
			errno = 0;
			errstr = null;

			if (remoteSocket == null)
			{
				PhpException.ArgumentNull("remoteSocket");
				return null;
			}

			// TODO: extract schema (tcp://, udp://) and port from remoteSocket
			// Uri uri = Uri.TryCreate(remoteSocket);
			ProtocolType protocol = ProtocolType.Tcp;

			if (Double.IsNaN(timeout))
				timeout = Configuration.Local.FileSystem.DefaultSocketTimeout;

			// TODO:
			if (flags != SocketOptions.None)
				PhpException.ArgumentValueNotSupported("flags", (int)flags);

			try
			{
                // workitem 299181; for remoteSocket as IPv4 address it results in IPv6 address
                //IPAddress address = System.Net.Dns.GetHostEntry(remoteSocket).AddressList[0];
                
                IPAddress address;
                if (!IPAddress.TryParse(remoteSocket, out address)) // if remoteSocket is not a valid IP address then lookup the DNS
                    address = System.Net.Dns.GetHostEntry(remoteSocket).AddressList[0];

				Socket socket = new Socket(address.AddressFamily, SocketType.Stream, protocol);

				IAsyncResult res = socket.BeginConnect(
				  new IPEndPoint(address, port),
				  new AsyncCallback(StreamSocket.ConnectResultCallback),
				  socket);

				int msec = 0;
				while (!res.IsCompleted)
				{
					Thread.Sleep(100);
					msec += 100;
					if (msec / 1000.0 > timeout)
					{
						PhpException.Throw(PhpError.Warning, LibResources.GetString("socket_open_timeout",
						  FileSystemUtils.StripPassword(remoteSocket)));
						return null;
					}
				}
				socket.EndConnect(res);

				//        socket.Connect(new IPEndPoint(address, port));
				return new SocketStream(socket, remoteSocket, context);
			}
			catch (SocketException e)
			{
				errno = e.ErrorCode;
				errstr = e.Message;
			}
			catch (System.Exception e)
			{
				errno = -1;
				errstr = e.Message;
			}

			PhpException.Throw(PhpError.Warning, LibResources.GetString("socket_open_error",
			  FileSystemUtils.StripPassword(remoteSocket), errstr));
			return null;
		}

		private static void ConnectResultCallback(IAsyncResult res)
		{
		}

		#endregion
	}
}
