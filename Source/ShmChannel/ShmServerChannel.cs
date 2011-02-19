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
using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Messaging;

namespace PHP.Core
{
	/// <summary>
	/// Shared memory remoting channel, the server end.
	/// </summary>
	public class ShmServerChannel : IChannelReceiver, IDisposable
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
		/// This mutex prevents more than one server channel from listening on one section.
		/// </summary>
		private IntPtr listenMutex;

		/// <summary>
		/// Setting this manual reset event causes all server threads waiting for clients'
		/// requests to exit immediatelly.
		/// </summary>
		internal IntPtr stopListeningEventHandle = ShmNative.CreateEvent(IntPtr.Zero, true, true, null);

		/// <summary>
		/// Channel name.
		/// </summary>
		private string channelName;

		/// <summary>
		/// Name of the public (connector) section object.
		/// </summary>
		private string sectionName;

		/// <summary>
		/// Name of the private (connection) section object.
		/// </summary>
		private string privateSectionName;

		/// <summary>
		/// A thread that listens for incoming connections made through the <see cref="ShmServerConnector"/>.
		/// </summary>
		private Thread listener;

		/// <summary>
		/// A helper event that serializes <see cref="ShmConnection"/> creation.
		/// </summary>
		private AutoResetEvent autoEvent;

		/// <summary>
		/// Server sink chain provider.
		/// </summary>
		private IServerChannelSinkProvider serverSinkProvider;

		/// <summary>
		/// The server transport sink.
		/// </summary>
		private ShmServerTransportSink transportSink;

		/// <summary>
		/// The <see cref="ShmServerConnector"/> incoming connections are made through.
		/// </summary>
		private ShmServerConnector connector;

		/// <summary>
		/// Recently created <see cref="ShmConnection"/>. Used to pass <see cref="ShmConnection"/> reference to
		/// connection server threads.
		/// </summary>
		private volatile ShmConnection shm;

		/// <summary>
		/// Stores channel-specific data.
		/// </summary>
		private ChannelDataStore data;

		#endregion

		#region Construction and initialization

		/// <summary>
		/// Creates a new <see cref="ShmServerChannel"/>.
		/// </summary>
		/// <param name="name">Name of the shared (connector) section to listen on.</param>
		/// <remarks>
		/// The connector section name is an analogy of the port number in
		/// <see cref="System.Runtime.Remoting.Channels.Tcp.TcpServerChannel"/>.
		/// </remarks>
		public ShmServerChannel(string name)
		{
			sectionName = name;

			InitProperties(null);
			InitProviders(null);
		}

		/// <summary>
		/// Creates a new <see cref="ShmServerChannel"/> with given properties and sinks.
		/// </summary>
		/// <param name="properties">Properties of the channel.</param>
		/// <param name="serverProviderChain">Sink providers for the channel.</param>
		public ShmServerChannel(IDictionary properties, IServerChannelSinkProvider serverProviderChain)
		{
			InitProperties(properties);
			InitProviders(serverProviderChain);
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

						case "section":
						sectionName = (string)entry.Value;
						break;
					}
				}
			}

			// if section name was not set so far, generate a globally unique one
			if (sectionName == null || sectionName.Length == 0)
			{
				sectionName = Guid.NewGuid().ToString();
			}
		}

		/// <summary>
		/// Inititializes the provider sink chain.
		/// </summary>
		/// <param name="serverProviderChain">Additional sinks to be used with this channel.</param>
		private void InitProviders(IServerChannelSinkProvider serverProviderChain)
		{
			listener = null;
			autoEvent = new AutoResetEvent(false);

			data = new ChannelDataStore(null);
			data.ChannelUris = new String[1];
			data.ChannelUris[0] = ShmChannel.channelScheme + "://" + sectionName;

			serverSinkProvider = serverProviderChain;

			// create the default sink chain if one was not passed in
			if (serverSinkProvider == null)
			{
				serverSinkProvider = CreateDefaultServerProviderChain();
			}

			// collect the rest of the channel data
			IServerChannelSinkProvider provider = serverSinkProvider;
			while (provider != null)
			{
				provider.GetChannelData(data);
				provider = provider.Next;
			}

			IServerChannelSink next = ChannelServices.CreateServerChannelSinkChain(serverSinkProvider, this);
			transportSink = new ShmServerTransportSink(next);

			StartListening(null);
		}

		/// <summary>
		/// Creates the default server sink provider chain consisting of the 
		/// <see cref="BinaryServerFormatterSinkProvider"/>.
		/// </summary>
		/// <returns>The sink provider chain.</returns>
		private IServerChannelSinkProvider CreateDefaultServerProviderChain()
		{
			return new BinaryServerFormatterSinkProvider();
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
		/// <param name="objectUri">When this method returns, it contains a string that holds 
		/// the object URI.</param>
		/// <returns>The URI of the current channel, or a <B>null</B> reference if the URI does 
		/// not belong to this channel.</returns>
		public string Parse(string url, out string objectUri)
		{
			return ShmConnection.Parse(url, out objectUri);
		}

		#endregion

		#region IChannelReceiver implementation

		/// <summary>
		/// Returns an array of all the URLs for a URI.
		/// </summary>
		/// <param name="objUri">The URI for which URLs are required.</param>
		/// <returns>An array of the URLs.</returns>
		public string[] GetUrlsForUri(string objUri)
		{
			Debug.WriteLineIf(ShmChannel.verbose, "GetUrlsForUri: Looking up URL for URI = " + objUri);
			string[] arr = new string[1];

			if (!objUri.StartsWith("/")) objUri = "/" + objUri;
			arr[0] = ShmChannel.channelScheme + "://" + sectionName + objUri;

			return arr;
		}

		/// <summary>
		/// Instructs the current channel to start listening for requests.
		/// </summary>
		/// <param name="data">Optional initialization information.</param>
		/// <exception cref="ShmIOException">Another server channel already listens on the given
		/// <see cref="sectionName"/>.
		/// </exception>
		public void StartListening(object data)
		{
			listenMutex = ShmNative.CreateNamedMutex("Global\\ShmChannel_listenmutex_" + sectionName, false);
			if (Marshal.GetLastWin32Error() == ShmNative.ERROR_ALREADY_EXISTS)
			{
				// another guy already listens on this section
				ShmNative.CloseHandleOnce(ref listenMutex);
				throw new ShmIOException(ShmResources.GetString("section_already_exists"));
			}

			Debug.WriteLineIf(ShmChannel.verbose, "Starting to listen...");

			// create the connector
			connector = new ShmServerConnector(sectionName);

			ShmNative.ResetEvent(stopListeningEventHandle);

			// start up a listening thread
			listener = new Thread(new ThreadStart(this.ListenerMain));
			listener.IsBackground = true;
			listener.Start();
		}

		/// <summary>
		/// Instructs the current channel to stop listening for requests.
		/// </summary>
		/// <param name="data">Optional state information for the channel.</param>
		public void StopListening(object data)
		{
			// this will abort all threads listening for remote calls
			ShmNative.SetEvent(stopListeningEventHandle);

			if (listener != null)
			{
				Debug.WriteLineIf(ShmChannel.verbose, "Stopping the listening thread...");
				listener.Join();
				listener = null;

				connector.Dispose();
				connector = null;
			}

			ShmNative.CloseHandleOnce(ref listenMutex);
		}

		/// <summary>
		/// Gets the channel-specific data.
		/// </summary>
		public object ChannelData
		{
			get
			{
				// return a blob that can be use to reconnect
				return data;
			}
		}

		#endregion

		#region Helper methods containing most of the functionality

		/// <summary>
		/// Listens for incoming connections and starts server threads.
		/// </summary>
		private void ListenerMain()
		{
			// common ThreadStart delegate
			ThreadStart ts = new ThreadStart(this.ServerMain);
			Thread.CurrentThread.IsBackground = true;

			while (true)
			{
				try
				{
					// wait for a client to connect
					if (!connector.WaitForConnect(stopListeningEventHandle)) return;

					// generate a globally unique name for the new private section
					privateSectionName = Guid.NewGuid().ToString();

					// create a new connection
					shm = new ShmConnection(privateSectionName, true, stopListeningEventHandle);

					// start a new thread to handle this connection
					Thread server = new Thread(ts);
					server.IsBackground = true;
					server.Start();

					// wait for the handler to spin up
					autoEvent.WaitOne();
				}
				catch (Exception e)
				{
					Debug.WriteLineIf(ShmChannel.verbose, "Exception caught in ShmServerChannel.ListenerMain: " +
						e.ToString());
				}
			}
		}

		/// <summary>
		/// Processes clients' method call requests.
		/// </summary>
		private void ServerMain()
		{
			ShmConnection connected_shm = shm;
			shm = null;

			// acknowledge the client
			connector.ConfirmConnect(privateSectionName);

			// signal the listener thread to start waiting again
			autoEvent.Set();

			try
			{
				while (connected_shm.BeginReadMessage())
				{
					// read the request
					ITransportHeaders headers = connected_shm.ReadHeaders();
					Stream request = connected_shm.ReadStream();
					connected_shm.EndReadMessage();

					ServerChannelSinkStack stack = new ServerChannelSinkStack();
					stack.Push(transportSink, null);

					IMessage response_msg;
					ITransportHeaders response_headers;
					Stream response_stream;

					// pass the exception-handling-behaviour header to formatter
					headers["__CustomErrorsEnabled"] = RemotingConfiguration.CustomErrorsEnabled(true);

					ServerProcessing processing = transportSink.NextChannelSink.ProcessMessage(
						stack,
						null,
						headers,
						request,
						out response_msg,
						out response_headers,
						out response_stream);

					// handle response
					switch (processing)
					{
						case ServerProcessing.Complete:
						// send the response, call completed synchronously
						stack.Pop(transportSink);
						WriteClientResponse(connected_shm, response_headers, response_stream);
						break;

						case ServerProcessing.OneWay:
						break;

						case ServerProcessing.Async:
						stack.StoreAndDispatch(transportSink, null);
						break;
					}
				}
			}
			catch (ShmIOException e)
			{
				Debug.WriteLineIf(ShmChannel.verbose, "Exception caught in ShmServerChannel.ServerMain: " +
					e.ToString());
			}

			connected_shm.Dispose();
		}

		/// <summary>
		/// Processes clients' method call responses.
		/// </summary>
		/// <param name="connectedShm">The connection.</param>
		/// <param name="headers">Transport headers.</param>
		/// <param name="responseStream">Response stream.</param>
		private void WriteClientResponse(ShmConnection connectedShm, ITransportHeaders headers,
			Stream responseStream)
		{
			object uri_obj = headers[CommonTransportKeys.RequestUri];
			string uri = (uri_obj == null ? String.Empty : uri_obj.ToString());

			// send the reply
			connectedShm.BeginWriteMessage();
			connectedShm.WriteHeaders(uri, headers);

			connectedShm.Write(responseStream);
			connectedShm.EndWriteMessage();
		}

		#endregion

		#region IDisposable implementation and related members

		/// <summary>
		/// Tracks whether <see cref="Dispose(bool)"/> has been called.
		/// </summary>
		private int disposed;

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
			if (Interlocked.Exchange(ref disposed, 1) == 0)
			{
				if (disposing)
				{
					StopListening(null);
					if (shm != null)
					{
						shm.Dispose();
						shm = null;
					}
				}

				ShmNative.CloseHandleOnce(ref stopListeningEventHandle);
			}
		}

		/// <summary>
		/// Destructor that will run only if the <see cref="Dispose()"/> method does not get called.
		/// </summary>
		~ShmServerChannel()
		{
			Dispose(false);
		}

		#endregion
	}

	/// <summary>
	/// Server transport sink.
	/// </summary>
	internal class ShmServerTransportSink : IServerChannelSink
	{
		/// <summary>
		/// Next sink in the sink chain.
		/// </summary>
		private IServerChannelSink next;

		/// <summary>
		/// Creates a new ShmServerTransportSink.
		/// </summary>
		/// <param name="next">Next sink in the sink chain.</param>
		public ShmServerTransportSink(IServerChannelSink next)
		{
			this.next = next;
		}

		#region IChannelSinkBase implementation

		/// <summary>
		/// Gets a dictionary through which properties on the sink can be accessed.
		/// </summary>
		public IDictionary Properties
		{
			get { return null; }
		}

		#endregion

		#region IServerChannelSink implementation

		/// <summary>
		/// Requests message processing from the current sink.
		/// </summary>
		/// <param name="sinkStack">A stack of channel sinks that called the current sink.</param>
		/// <param name="requestMsg">The message that contains the request.</param>
		/// <param name="requestHeaders">Headers retrieved from the incoming message from the client.
		/// </param>
		/// <param name="requestStream">The stream that needs to be to processed and passed on to the 
		/// deserialization sink.</param>
		/// <param name="msg">When this method returns, contains an <see cref="IMessage"/> that holds the
		/// response message.</param>
		/// <param name="responseHeaders">When this method returns, contains an <see cref="ITransportHeaders"/>
		/// that holds the headers that are to be added to return message heading to the client.</param>
		/// <param name="responseStream">When this method returns, contains a <see cref="Stream"/> that is
		/// heading back to the transport sink.</param>
		/// <returns>A <see cref="ServerProcessing"/> status value that provides information about how
		/// message was processed.</returns>
		/// <exception cref="NotSupportedException">Always, because it is no use calling
		/// <see cref="ProcessMessage"/> on a server transport sink.</exception>
		public ServerProcessing ProcessMessage(IServerChannelSinkStack sinkStack, IMessage requestMsg,
			ITransportHeaders requestHeaders, Stream requestStream, out IMessage msg,
			out ITransportHeaders responseHeaders, out Stream responseStream)
		{
			// note: this doesn't have to be implemented because the server transport sink is always first
			throw new NotSupportedException(ShmResources.GetString("processmessage_unsupported"));
		}

		/// <summary>
		/// Requests processing from the current sink of the response from a method call sent
		/// asynchronously.
		/// </summary>
		/// <param name="sinkStack">A stack of sinks leading back to the server transport sink.
		/// </param>
		/// <param name="state">Information generated on the request side that is associated with this 
		/// sink.</param>
		/// <param name="msg">The response message.</param>
		/// <param name="headers">The headers to add to the return message heading to the client.
		/// </param>
		/// <param name="stream">The stream heading back to the transport sink.</param>
		/// <exception cref="NotSupportedException">Always, because it is no use calling
		/// <see cref="AsyncProcessResponse"/> on a server transport sink.</exception>
		public void AsyncProcessResponse(IServerResponseChannelSinkStack sinkStack, object state,
			IMessage msg, ITransportHeaders headers, Stream stream)
		{
			throw new NotSupportedException(ShmResources.GetString("asyncprocessmessage_unsupported"));
		}

		/// <summary>
		/// Returns the <see cref="Stream"/> onto which the provided response message is to be serialized.
		/// </summary>
		/// <param name="sinkStack">A stack of sinks leading back to the server transport sink.
		/// </param>
		/// <param name="state">The state that has been pushed to the stack by this sink.</param>
		/// <param name="msg">The response message to serialize.</param>
		/// <param name="headers">headers to put in the response stream to the client.</param>
		/// <returns>The <see cref="Stream"/> onto which the provided response message is to be serialized.
		/// </returns>
		public Stream GetResponseStream(IServerResponseChannelSinkStack sinkStack, object state,
			IMessage msg, ITransportHeaders headers)
		{
			// we always want a stream to read from
			return null;
		}

		/// <summary>
		/// Gets the next server channel sink in the server sink chain.
		/// </summary>
		public IServerChannelSink NextChannelSink
		{
			get { return next; }
		}

		#endregion
	}
}
