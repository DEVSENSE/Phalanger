/*

 Copyright (c) 2004-2006 Ladislav Prosek. Inspired by PipeChannel and MS Shared Source CLI.
 
 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.IO;
using System.Threading;
using System.Reflection;
using System.Collections;
using System.Diagnostics;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Messaging;

namespace PHP.Core
{
	/// <summary>
	/// Shared memory remoting channel, the client end.
	/// </summary>
	public class ShmClientChannel : IChannelSender
	{
		#region Fields

		/// <summary>
		/// Default channel priority.
		/// </summary>
		private const int defaultChannelPriority = 1;

		/// <summary>
		/// Current channel priority.
		/// </summary>
		private int channelPriority = defaultChannelPriority;

		/// <summary>
		/// Channel name.
		/// </summary>
		private string channelName;

		/// <summary>
		/// Client sink chain provider.
		/// </summary>
		private IClientChannelSinkProvider clientSinkProvider;

		#endregion

		#region Construction and initialization

		/// <summary>
		/// Creates a new <see cref="ShmClientChannel"/>. Parameterless constructor sets no properties and
		/// chains no additional sinks.
		/// </summary>
		public ShmClientChannel()
		{
			InitProperties(null);
			InitProviders(null);
		}

		/// <summary>
		/// Creates a new <see cref="ShmClientChannel"/> with given properties and sinks.
		/// </summary>
		/// <param name="properties">Properties of the channel.</param>
		/// <param name="clientProviderChain">Sink providers for the channel.</param>
		public ShmClientChannel(IDictionary properties, IClientChannelSinkProvider clientProviderChain)
		{
			InitProperties(properties);
			InitProviders(clientProviderChain);
		}

		/// <summary>
		/// Initializes members of the instance according to given properties.
		/// </summary>
		/// <param name="properties">The properties.</param>
		private void InitProperties(IDictionary properties)
		{
			if (properties != null)
			{
				foreach (DictionaryEntry entry in properties)
				{
					switch ((string)entry.Key)
					{
						case "name":
						channelName = (string)entry.Value;
						break;

						case "priority":
						channelPriority = Convert.ToInt32(entry.Value);
						break;
					}
				}
			}
		}

		/// <summary>
		/// Inititializes provider sink chain.
		/// </summary>
		/// <param name="clientProviderChain">Additional sinks to be used with this channel.</param>
		/// <remarks>
		/// The resulting chain is <see cref="BinaryClientFormatterSink"/> -&gt; <see cref="ShmClientTransportSink"/>
		/// if no user sinks are given, <c>[user sinks]</c> -&gt; <see cref="ShmClientTransportSink"/> otherwise.
		/// Therefore, if you pass a non-null <paramref name="clientProviderChain"/> to this method, you are
		/// responsible for creating and chaining a formatter!
		/// </remarks>
		private void InitProviders(IClientChannelSinkProvider clientProviderChain)
		{
			clientSinkProvider = clientProviderChain;

			if (clientSinkProvider == null)
			{
				// we need at least a formatter
				clientSinkProvider = new BinaryClientFormatterSinkProvider();
			}

			IClientChannelSinkProvider temp_sink_provider = clientSinkProvider;

			// move to the end of provider list
			while (temp_sink_provider.Next != null) temp_sink_provider = temp_sink_provider.Next;

			// append transport sink provider to the end
			temp_sink_provider.Next = new ShmClientTransportSinkProvider();
		}

		#endregion

		#region IChannel implementation

		/// <summary>
		/// Gets the name of the channel.
		/// </summary>
		public string ChannelName
		{
			get { return channelName; }
		}

		/// <summary>
		/// Gets the priority of the channel.
		/// </summary>
		public int ChannelPriority
		{
			get { return channelPriority; }
		}

		/// <summary>
		/// Returns the current channel URI and places object URI into out parameter.
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
		/// <param name="data">The channel data object of the remote host to which 
		/// the new sink will deliver messages. Can be a <B>null</B> reference.</param>
		/// <param name="objUri">When this method returns, contains a URI of the new channel 
		/// message sink that delivers messages to the specified URL or channel data object.</param>
		/// <returns>A channel message sink that delivers messages to the specified URL or channel 
		/// data object, or a <B>null</B> reference if the channel cannot connect to the given endpoint.
		/// </returns>
		public IMessageSink CreateMessageSink(string url, object data, out string objUri)
		{
			// set the out parameters
			objUri = null;
			string chan_uri = null;

			if (url != null) // is this a well known object?
			{
				// Parse returns null if this is not one of the shm channel URLs
				chan_uri = Parse(url, out objUri);
			}
			else if (data != null)
			{
				IChannelDataStore cds = data as IChannelDataStore;

				if (cds != null)
				{
					Debug.WriteLineIf(ShmChannel.verbose, "ChannelUris[0] = " + cds.ChannelUris[0]);

					chan_uri = Parse(cds.ChannelUris[0], out objUri);

					Debug.WriteLineIf(ShmChannel.verbose, "CreateMessageSink: chanUri = " + chan_uri +
						", objUri = " + objUri);
					if (chan_uri != null) url = cds.ChannelUris[0];
				}
			}

			if (chan_uri != null)
			{
				if (url == null) url = chan_uri;

				Debug.WriteLineIf(ShmChannel.verbose, "CreateMessageSink: delegating w/ url = " + url);
				return (IMessageSink)clientSinkProvider.CreateSink(this, url, data);
			}

			Debug.WriteLineIf(ShmChannel.verbose, "CreateMessageSink: ignoring request");
			return null;
		}

		#endregion
	}

	/// <summary>
	/// Provides client transport sink for <see cref="ShmClientChannel"/>.
	/// </summary>
	internal sealed class ShmClientTransportSinkProvider : IClientChannelSinkProvider
	{
		#region Construction

		/// <summary>
		/// Creates a new <see cref="ShmClientTransportSinkProvider"/>.
		/// </summary>
		internal ShmClientTransportSinkProvider()
		{ }

		#endregion

		#region IClientChannelSinkProvider implementation

		/// <summary>
		/// Creates a sink chain.
		/// </summary>
		/// <param name="channel">Channel for which the current sink chain is being constructed.</param>
		/// <param name="url">The URL of the object to connect to.</param>
		/// <param name="data">A channel data object describing a channel on the remote server.</param>
		/// <returns>The first sink of the newly formed channel sink chain, or a <B>null</B> reference 
		/// indicating that this provider will not or cannot provide a connection for this endpoint.
		/// </returns>
		public IClientChannelSink CreateSink(IChannelSender channel, string url, object data)
		{
			// this is the transport sink and therefore always last in the chain
			return new ShmClientTransportSink(url);
		}

		/// <summary>
		/// Gets or sets the next sink provider in the channel sink provider chain.
		/// </summary>
		/// <exception cref="NotSupportedException">When this property is set.</exception>
		public IClientChannelSinkProvider Next
		{
			// this is the transport sink and therefore always last in the chain
			get { return null; }
			set { throw new NotSupportedException(ShmResources.GetString("next_provider_unsupported")); }
		}

		#endregion
	}

	/// <summary>
	/// Client transport sink for <see cref="ShmClientChannel"/>.
	/// </summary>
	internal sealed class ShmClientTransportSink : IClientChannelSink, IDisposable
	{
		#region Fields

		/// <summary>
		/// Name of the file mapping (section) object this transport sink is going to connect to.
		/// </summary>
		private string sectionName;

		/// <summary>
		/// Pool of previously created connections ready for reuse.
		/// </summary>
		private ShmConnectionPool shmConnectionPool;

		/// <summary>
		/// Number of retries when sending a message.
		/// </summary>
		private const int defaultRetryCount = 3;

		/// <summary>
		/// A callback for handling asynchronous calls. <seealso cref="AsyncProcessRequest"/>
		/// </summary>
		private WaitCallback callback;

		#endregion

		#region Construction

		/// <summary>
		/// Creates a new <see cref="ShmClientTransportSink"/>.
		/// </summary>
		/// <param name="url">The URL of the object to connect to.</param>
		internal ShmClientTransportSink(string url)
		{
			string obj_uri;
			sectionName = ShmConnection.Parse(url, out obj_uri);

			Debug.WriteLineIf(ShmChannel.verbose, "ShmClientTransportSink: creating shm on URI: " +
				sectionName);

			// get/create a connection pool
			shmConnectionPool = ShmConnectionPoolManager.LookupPool(sectionName);

			callback = new WaitCallback(this.ReceiveCallback);
		}

		#endregion

		#region IChannelSinkBase implementation

		/// <summary>
		/// Gets a dictionary through which properties on the sink can be accessed.
		/// </summary>
		public IDictionary Properties
		{
			get { return null; }
		}

		#endregion

		#region IClientChannelSink implementation

		/// <summary>
		/// Requests asynchronous processing of a method call on the current sink.
		/// </summary>
		/// <param name="stack">A stack of channel sinks that called this sink.</param>
		/// <param name="msg">The message to process.</param>
		/// <param name="headers">The headers to add to the outgoing message heading to the server.</param>
		/// <param name="stream">The stream headed to the transport sink.</param>
		public void AsyncProcessRequest(IClientChannelSinkStack stack, IMessage msg,
			ITransportHeaders headers, Stream stream)
		{
			Debug.WriteLineIf(ShmChannel.verbose, "AsyncProcessRequest.");

			ShmConnection shm = SendWithRetry(msg, headers, stream);

			IMethodCallMessage mcm = (IMethodCallMessage)msg;
			MethodBase method_base = mcm.MethodBase;

			if (RemotingServices.IsOneWay(method_base))
			{
				shmConnectionPool.ReturnToPool(shm);
				shm = null;
			}
			else
			{
				ShmConnectionCookie cookie = new ShmConnectionCookie();

				cookie.Connection = shm;
				cookie.SinkStack = stack;

				// wait for reply in another thread
				ThreadPool.QueueUserWorkItem(callback, cookie);
			}
		}

		/// <summary>
		/// Requests asynchronous processing of a response to a method call on the current sink.
		/// </summary>
		/// <param name="stack">A stack of sinks that called this sink.</param>
		/// <param name="obj">Information generated on the request side that is associated with 
		/// this sink.</param>
		/// <param name="headers">The headers retrieved from the server response stream.</param>
		/// <param name="stream">The stream coming back from the transport sink.</param>
		/// <exception cref="NotSupportedException">Always, because it is no use calling
		/// <see cref="AsyncProcessResponse"/> on a client transport sink.</exception>
		public void AsyncProcessResponse(IClientResponseChannelSinkStack stack,
			object obj, ITransportHeaders headers, Stream stream)
		{
			throw new NotSupportedException(ShmResources.GetString("asyncprocessresponse_unsupported"));
		}

		/// <summary>
		/// Returns the <see cref="Stream"/> onto which the provided message is to be serialized.
		/// </summary>
		/// <param name="msg">The <see cref="IMethodCallMessage"/> containing details about the method call.
		/// </param>
		/// <param name="headers">The headers to add to the outgoing message heading to the server.
		/// </param>
		/// <returns>The <see cref="Stream"/> onto which the provided message is to be serialized.</returns>
		public Stream GetRequestStream(IMessage msg, ITransportHeaders headers)
		{
			// we don't do any serialization here
			return null;
		}

		/// <summary>
		/// Requests message processing from the current sink.
		/// </summary>
		/// <param name="msg">The message to process.</param>
		/// <param name="reqHead">The headers to add to the outgoing message heading to the server.
		/// </param>
		/// <param name="reqStm">The stream headed to the transport sink.</param>
		/// <param name="respHead">When this method returns, contains an <see cref="ITransportHeaders"/>
		/// interface that holds the headers that the server returned.</param>
		/// <param name="respStm">When this method returns, contains a <see cref="Stream"/> coming back from
		/// the transport sink.</param>
		/// <exception cref="ShmIOException">Failed to process the message.</exception>
		public void ProcessMessage(IMessage msg, ITransportHeaders reqHead, Stream reqStm,
			out ITransportHeaders respHead, out Stream respStm)
		{
			int try_count;
			long request_stream_pos;

			if (reqStm.CanSeek)
			{
				request_stream_pos = reqStm.Position;
				try_count = defaultRetryCount;
			}
			else
			{
				request_stream_pos = -1;
				try_count = 1;
			}

			ShmIOException exception = null;

			while (try_count > 0)
			{
				// send the message across the shm
				ShmConnection shm = SendWithRetry(msg, reqHead, reqStm);

				// read response
				try
				{
					if (!shm.BeginReadMessage())
					{
						exception = new ShmIOException(ShmResources.GetString("timeout_waiting_for_client_event"));
					}
					else
					{
						respHead = shm.ReadHeaders();
						respStm = shm.ReadStream();
						shm.EndReadMessage();

						shmConnectionPool.ReturnToPool(shm);
						return;
					}
				}
				catch (ShmIOException e)
				{
					Debug.WriteLineIf(ShmChannel.verbose, "Exception caught in ShmClientTransportSink.ProcessMessage: " +
						e.ToString());

					exception = e;
				}

				shm.Dispose();

				// clear connection cache
				shmConnectionPool.CloseAllConnections();

				try_count--;

				// rewind the request stream
				reqStm.Position = request_stream_pos;
			}

			throw new ShmIOException(ShmResources.GetString("processmessage_failed"), exception);
		}

		/// <summary>
		/// Gets the next client channel sink in the client sink chain.
		/// </summary>
		IClientChannelSink IClientChannelSink.NextChannelSink
		{
			// this is always the last sink
			get { return null; }
		}

		#endregion

		#region IDisposable implementation and related members

		/// <summary>
		/// Disposes of the connection pool.
		/// </summary>
		public void Dispose()
		{
			shmConnectionPool.Dispose();
		}

		#endregion

		#region Helper methods containing most of the functionality

		/// <summary>
		/// Waits for a reply when processing asynchronous method call request. This method
		/// runs in a separate worker thread.
		/// </summary>
		/// <param name="state">Reference to a <see cref="ShmConnectionCookie"/> describing the original
		/// request.</param>
		private void ReceiveCallback(object state)
		{
			ShmConnectionCookie cookie = (ShmConnectionCookie)state;

			ShmConnection shm = cookie.Connection;
			IClientChannelSinkStack sink_stack = cookie.SinkStack;

			Exception exception;
			try
			{
				// read response
				if (shm.BeginReadMessage())
				{
					ITransportHeaders response_headers = shm.ReadHeaders();
					Stream response_stream = shm.ReadStream();
					shm.EndReadMessage();

					shmConnectionPool.ReturnToPool(shm);

					sink_stack.AsyncProcessResponse(response_headers, response_stream);
					return;
				}
				else
				{
					exception = new ShmIOException(ShmResources.GetString("timeout_waiting_for_client_event"));
				}
			}
			catch (Exception e)
			{
				Debug.WriteLineIf(ShmChannel.verbose, "Exception caught in ShmClientTransportSink.ReceiveCallback: " +
					e.ToString());
				exception = e;
			}

			// clear connection cache
			shmConnectionPool.CloseAllConnections();

			try
			{
				// exceptions are dispatched back to the original caller!
				if (sink_stack != null) sink_stack.DispatchException(exception);
			}
			catch (Exception)
			{
				// fatal error -> ignore :-)
			}
		}

		/// <summary>
		/// Sends a message across the apropriate shared memory connection.
		/// </summary>
		/// <param name="msg">The message to send.</param>
		/// <param name="reqHead">The headers to add to the outgoing message.</param>
		/// <param name="reqStm">The stream headed to the transport sink.</param>
		/// <returns>The shared memory connection through which the message was transmitted.</returns>
		/// <exception cref="ShmIOException">Failed to send the message.</exception>
		private ShmConnection SendWithRetry(IMessage msg, ITransportHeaders reqHead, Stream reqStm)
		{
			IMethodCallMessage mcm = (IMethodCallMessage)msg;
			string uri = mcm.Uri;

			int try_count;
			long request_stream_pos;

			if (reqStm.CanSeek)
			{
				request_stream_pos = reqStm.Position;
				try_count = defaultRetryCount;
			}
			else
			{
				request_stream_pos = -1;
				try_count = 1;
			}

			ShmConnection shm = null;
			ShmClientConnector connector = null;

			ShmIOException exception = null;

			while (try_count > 0)
			{
				try
				{
					// try to reuse a connection if possible
					shm = shmConnectionPool.Obtain();
					if (shm != null)
					{
						Debug.WriteLineIf(ShmChannel.verbose, "Shm connection gotten from the pool.");
					}
					else
					{
						// otherwise create a new connection

						// obtain a private section name (private connection) using a
						// connector on the shared section name (shared connection)
						//
						// shared sections are only used for obtaining private section
						// names through connectors

						connector = new ShmClientConnector(sectionName);
						string private_section_name = connector.Connect();
						connector.Dispose();
						connector = null;

						shm = new ShmConnection(private_section_name, false);
					}

					// send with retry
					shm.BeginWriteMessage();
					shm.WriteHeaders(uri, reqHead);
					shm.Write(reqStm);
					shm.EndWriteMessage();

					return shm;
				}
				catch (ShmIOException e)
				{
					Debug.WriteLineIf(ShmChannel.verbose, "Exception caught in ShmClientTransportSink.SendWithRetry: "
						+ e.ToString());

					if (shm != null)
					{
						shm.Dispose();
						shm = null;
					}
					if (connector != null) connector.Dispose();

					// clear connection cache
					shmConnectionPool.CloseAllConnections();

					try_count--;

					// rewind the request stream
					reqStm.Position = request_stream_pos;

					exception = e;
				}
			}

			throw new ShmIOException(ShmResources.GetString("sendmessage_failed"), exception);
		}

		#endregion
	}
}
