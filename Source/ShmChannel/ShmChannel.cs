/*

 Copyright (c) 2004-2006 Ladislav Prosek. Inspired by PipeChannel and MS Shared Source CLI.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Collections;
using System.Runtime.Serialization;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Messaging;

namespace PHP.Core
{
	/// <summary>
	/// Shared memory remoting channel.
	/// </summary>
	public class ShmChannel : IChannelSender, IChannelReceiver, IDisposable
	{
		/// <summary>
		/// Specifies whether the channel should output verbose debug messages using the
		/// <see cref="System.Diagnostics.Debug"/> class.
		/// </summary>
		internal const bool verbose = false;

		/// <summary>
		/// URL prefix for this channel (scheme).
		/// </summary>
		internal const string channelScheme = "shm";

		/// <summary>
		/// Client side of the channel.
		/// </summary>
		private ShmClientChannel clientChannel = null;

		/// <summary>
		/// Server side of the channel.
		/// </summary>
		private ShmServerChannel serverChannel = null;

		/// <summary>
		/// Parameterless constructor creates only the client channel. Use this constructor in
		/// the clients if you do not intend to receive callbacks.
		/// </summary>
		public ShmChannel()
		{
			clientChannel = new ShmClientChannel();
		}

		/// <summary>
		/// Creates both the client and the server channel.
		/// </summary>
		/// <param name="section">Name of the section object used for communication. If null, a unique 
		/// name is generated, which is useful if you need the server channel only for callbacks.</param>
		public ShmChannel(string section)
		{
			clientChannel = new ShmClientChannel();
			serverChannel = new ShmServerChannel(section);
		}

		/// <summary>
		/// Creates both the client and the server channel. Properties and additional sink providers
		/// can be specified.
		/// </summary>
		/// <param name="properties">Properties of the channel.</param>
		/// <param name="clientSinkProvider">Sink providers for the client channel.</param>
		/// <param name="serverSinkProvider">Sink providers for the server channel.</param>
		public ShmChannel(IDictionary properties, IClientChannelSinkProvider clientSinkProvider,
			IServerChannelSinkProvider serverSinkProvider)
		{
			clientChannel = new ShmClientChannel(properties, clientSinkProvider);
			serverChannel = new ShmServerChannel(properties, serverSinkProvider);
		}

		#region IChannel implementation

		/// <summary>
		/// Gets the name of the channel.
		/// </summary>
		public string ChannelName
		{
			get { return clientChannel.ChannelName; }
		}

		/// <summary>
		/// Gets the priority of the channel.
		/// </summary>
		public int ChannelPriority
		{
			get { return clientChannel.ChannelPriority; }
		}

		/// <summary>
		/// Returns channel URI and places object URI into the <paramref name="objectUri"/> out parameter.
		/// </summary>
		/// <param name="url">The URL of the object.</param>
		/// <param name="objectUri">When this method returns, contains a string that holds 
		/// the object URI.</param>
		/// <returns>The URI of the current channel, or a <B>null</B> reference if the URI does 
		/// not belong to this channel.</returns>
		public string Parse(string url, out string objectUri)
		{
			return ShmConnection.Parse(url, out objectUri);
		}

		#endregion

		#region IChannelSender implementation

		/// <summary>
		/// Returns a channel message sink that delivers messages to the specified URL or channel 
		/// data object.
		/// </summary>
		/// <param name="url">The URL to which the new sink will deliver messages. Can be a <B>null</B>
		/// reference.</param>
		/// <param name="remoteChannelData">The channel data object of the remote host to which 
		/// the new sink will deliver messages. Can be a <B>null</B> reference.</param>
		/// <param name="objectUri">When this method returns, contains a URI of the new channel 
		/// message sink that delivers messages to the specified URL or channel data object.</param>
		/// <returns>A channel message sink that delivers messages to the specified URL or channel 
		/// data object, or a <B>null</B> reference if the channel cannot connect to the given endpoint.
		/// </returns>
		public IMessageSink CreateMessageSink(string url, object remoteChannelData,
			out string objectUri)
		{
			return clientChannel.CreateMessageSink(url, remoteChannelData, out objectUri);
		}

		#endregion

		#region IChannelReceiver implementation

		/// <summary>
		/// Gets the channel-specific data.
		/// </summary>
		public object ChannelData
		{
			get
			{
				if (serverChannel != null) return serverChannel.ChannelData;
				else return null;
			}
		}

		/// <summary>
		/// Returns an array of all the URLs for a URI.
		/// </summary>
		/// <param name="objectURI">The URI for which URLs are required.</param>
		/// <returns>An array of the URLs.</returns>
		public string[] GetUrlsForUri(string objectURI)
		{
			if (serverChannel != null) return serverChannel.GetUrlsForUri(objectURI);
			else return null;
		}

		/// <summary>
		/// Instructs the current channel to start listening for requests.
		/// </summary>
		/// <param name="data">Optional initialization information.</param>
		public void StartListening(object data)
		{
			if (serverChannel != null) serverChannel.StartListening(data);
		}

		/// <summary>
		/// Instructs the current channel to stop listening for requests.
		/// </summary>
		/// <param name="data">Optional state information for the channel.</param>
		public void StopListening(object data)
		{
			if (serverChannel != null) serverChannel.StopListening(data);
		}

		#endregion

		#region IDisposable implementation

		/// <summary>
		/// Propagates the <see cref="IDisposable.Dispose"/> call through the containment hierarchy.
		/// </summary>
		public void Dispose()
		{
			if (serverChannel != null) serverChannel.Dispose();
		}

		#endregion
	}

	/// <summary>
	/// Shared memory IO exception. Thrown if something goes wrong with shm communication,
	/// e.g. timeout expires when waiting for an event etc.
	/// </summary>
	[Serializable]
	internal class ShmIOException : RemotingException
	{
		/// <summary>
		/// Creates a new <see cref="ShmIOException"/>.
		/// </summary>
		/// <param name="text">The message that describes the error.</param>
		public ShmIOException(string text)
			: base(text)
		{ }

		/// <summary>
		/// Creates a new <see cref="ShmIOException"/> after a P/Invoke call failure.
		/// </summary>
		/// <param name="text">The message that describes the error.</param>
		/// <param name="errorCode">Win32 error code. Usually the result of
		/// <see cref="System.Runtime.InteropServices.Marshal.GetLastWin32Error"/>.
		/// </param>
		public ShmIOException(string text, int errorCode)
			: base(text + "\n" + ShmNative.GetErrorString(errorCode))
		{ }

		/// <summary>
		/// Creates a new instance of the <see cref="ShmIOException"/> class with a specified error message
		/// and a reference to the inner exception that is the cause of this exception.
		/// </summary>
		/// <param name="message">The error message that explains why the exception occurred.</param>
		/// <param name="innerException">The exception that is the cause of the current exception.</param>
		public ShmIOException(string message, Exception innerException)
			: base(message, innerException)
		{ }

		/// <summary>
		/// Initializes a new instance of the <see cref="ShmIOException"/> from serialized data.
		/// </summary>
		/// <param name="info">The object that holds the serialized object data.</param>
		/// <param name="context">The contextual information about the source or destination of the exception.</param>
		protected ShmIOException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{ }
	}
}
