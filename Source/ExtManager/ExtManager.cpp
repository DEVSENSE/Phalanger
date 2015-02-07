/*

 Copyright (c) 2004-2006 Ladislav Prosek.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

//
// ExtManager - PHP extension manager
//
// This program and the PHP extensions optionally reside in a separate process.
// Communication between ExtManager and Phalanger Core is then done via Remoting.
//
// ExtManager.cpp 
// - contains entry point of the application
// - sets up remoting to handle incoming remote calls
// - provides a simple lifetime control for this application
//

#include <windows.h>
#include <tchar.h>
#include <stdio.h>

#include "ExtManager.h"

using namespace System;
using namespace System::IO;
using namespace System::Text;
using namespace System::Threading;
using namespace System::Reflection;
using namespace System::Collections;
using namespace System::Configuration;
using namespace System::Runtime::Remoting;
using namespace System::Runtime::Remoting::Lifetime;
using namespace System::Runtime::Remoting::Messaging;
using namespace System::Runtime::Remoting::Channels;

using namespace PHP::Core;

/*

  Designed and implemented by Ladislav Prosek.
  
*/

namespace PHP
{
	namespace ExtManager
	{
		/// <summary>
		/// Provides simple lifetime control of the ExtManager process.
		/// </summary>
		/// <remarks>
		/// Number of active <c>Request</c>s and number of ongoing calls are monitored in order to decide
		/// when it is safe to shut this instance of <c>ExtManager</c> down.
		/// </remarks>
		private ref class LifeTime
		{
		private:
			/// <summary>The timeout counter.</summary>
			static int counter = 0;

			/// <summary>The number of active (ongoing) remote calls.</summary>
			static int numberOfActiveCalls = 0;

		public:
			/// <summary>Resets the timeout counter.</summary>
			static void ResetTimeout()
			{
				Interlocked::Exchange(counter, 0);
			}

			/// <summary>Increments the number of active calls.</summary>
			static void IncrementActiveCalls()
			{
				Interlocked::Increment(numberOfActiveCalls);
			}

			/// <summary>Decrements the number of active calls.</summary>
			static void DecrementActiveCalls()
			{
				Interlocked::Decrement(numberOfActiveCalls);
			}

			/// <summary>Returns the number of remote calls that are currently active.</summary>
			/// <returns>The number of active (ongoing) remote calls.</returns>
			static int GetNumberOfActiveCalls()
			{
				return Interlocked::CompareExchange(numberOfActiveCalls, -1, -1);
			}

			/// <summary>
			/// Sleeps until the timeout counter reaches the given value or until <c>ExtManager</c>
			/// is requested to shut down.
			/// </summary>
			/// <param name="timeout">Number of minutes of inactivity that must elapse until this
			/// method returns.</param>
			static void Wait(int timeout)
			{
				TimeSpan timeSpan = TimeSpan::FromMinutes(1);

				while (timeout == -1 || Interlocked::Increment(counter) <= timeout)
				{
					if (RemoteDispatcher::ShuttingDownEvent->WaitOne(timeSpan, false) == true)
					{
						ResetTimeout();
						timeSpan = TimeSpan::FromSeconds(15);
					}
					
					if (Interlocked::CompareExchange(RemoteDispatcher::ShuttingDown, -1, -1) != 0)
					{
#ifdef DEBUG
						Debug::WriteLine("EXT MGR", "Number of requests: {0}  number of active calls: {1}",
							RemoteDispatcher::GetNumberOfRequests(), GetNumberOfActiveCalls());
#endif

						// wait until there are no active requests
						if (RemoteDispatcher::GetNumberOfRequests() == 0 &&	GetNumberOfActiveCalls() == 0) break;
					}
				}
			}
		};

		/// <summary>
		/// Provides a server channel sink that resets the timeout <see cref="LifeTime.counter"/> every time
		/// a Remoting message s processed and monitors number of active calls.
		/// </summary>
		private ref class TimeoutServerChannelSinkProvider : public IServerChannelSinkProvider
		{
		private:
			/// <summary>Next channel sink provider in the chain.</summary>
			IServerChannelSinkProvider ^_next;

			/// <summary>
			/// This is a pass-thru server sink that just resets timeout <see cref="LifeTime.counter"/> every time
			/// a message is processed and also monitors number of active calls.
			/// </summary>
			ref class TimeoutServerChannelSink : public IServerChannelSink
			{
			private:
				/// <summary>Next channel sink in the sink chain.</summary>
				IServerChannelSink ^_nextSink;

			public:
				/// <summary>
				/// Creates a new <see cref="TimeoutServerChannelSink"/>.
				/// </summary>
				/// <param name="nextSink">Next channel sink in the sink chain.</param>
				TimeoutServerChannelSink(IServerChannelSink ^nextSink) : _nextSink(nextSink)
				{ }

				/// <summary>
				/// Gets the next server channel sink in the server sink chain.
				/// </summary>
				virtual property IServerChannelSink ^NextChannelSink
				{
					IServerChannelSink ^get()
					{
						return _nextSink;
					}
				}

				/// <summary>
				/// Gets a dictionary through which properties on the sink can be accessed.
				/// </summary>
				virtual property IDictionary ^Properties
				{
					IDictionary ^get()
					{
						return (_nextSink != nullptr) ? _nextSink->Properties : nullptr;
					}
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
				virtual void AsyncProcessResponse(IServerResponseChannelSinkStack ^sinkStack, Object ^state,
					IMessage ^msg, ITransportHeaders ^headers, Stream ^stream)
				{
					LifeTime::ResetTimeout();
					if (_nextSink != nullptr) 
					{
						_nextSink->AsyncProcessResponse(sinkStack, state, msg, headers, stream);
					}
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
				virtual Stream ^GetResponseStream(IServerResponseChannelSinkStack ^sinkStack, Object ^state,
					IMessage ^msg, ITransportHeaders ^headers)
				{
					return (_nextSink != nullptr) ? 
						_nextSink->GetResponseStream(sinkStack, state, msg, headers) : nullptr;
				}

				/// <summary>
				/// Requests message processing from the current sink.
				/// </summary>
				/// <param name="sinkStack">A stack of channel sinks that called the current sink.</param>
				/// <param name="requestMsg">The message that contains the request.</param>
				/// <param name="requestHeaders">Headers retrieved from the incoming message from the client.
				/// </param>
				/// <param name="requestStream">The stream that needs to be to processed and passed on to the 
				/// deserialization sink.</param>
				/// <param name="responseMsg">When this method returns, contains an <see cref="IMessage"/> that holds the
				/// response message.</param>
				/// <param name="responseHeaders">When this method returns, contains an <see cref="ITransportHeaders"/>
				/// that holds the headers that are to be added to return message heading to the client.</param>
				/// <param name="responseStream">When this method returns, contains a <see cref="Stream"/> that is
				/// heading back to the transport sink.</param>
				/// <returns>A <see cref="ServerProcessing"/> status value that provides information about how
				/// message was processed.</returns>
				virtual ServerProcessing ProcessMessage(IServerChannelSinkStack ^sinkStack, IMessage ^requestMsg,
					ITransportHeaders ^requestHeaders, Stream ^requestStream, IMessage ^%responseMsg,
					ITransportHeaders ^%responseHeaders, Stream ^%responseStream)
				{
					LifeTime::ResetTimeout();
					LifeTime::IncrementActiveCalls();
					try
					{
						return (_nextSink != nullptr) ?
							_nextSink->ProcessMessage(sinkStack, requestMsg, requestHeaders, requestStream,
							responseMsg, responseHeaders, responseStream) : ServerProcessing::Complete;
					}
					finally
					{
						LifeTime::DecrementActiveCalls();
					}
				}
			};

		public:
			/// <summary>Gets or sets the next sink provider in the channel sink provider chain.</summary>
			virtual property IServerChannelSinkProvider ^Next
			{
				IServerChannelSinkProvider ^get()
				{
					return _next;
				}
				void set(IServerChannelSinkProvider ^value)
				{
					_next = value;
				}
			}

			/// <summary>
			/// Creates a sink chain.
			/// </summary>
			/// <param name="channel">The channel for which to create the channel sink chain.</param>
			/// <returns>The first sink of the newly formed channel sink chain.</returns>
			virtual IServerChannelSink ^CreateSink(IChannelReceiver ^channel)
			{
				IServerChannelSink ^nextSink = nullptr;
				if (_next != nullptr) nextSink = _next->CreateSink(channel);

				return gcnew TimeoutServerChannelSink(nextSink);
			}

			/// <summary>
			/// Returns the channel data for the channel that the current sink is associated with.
			/// </summary>
			/// <param name="channelData">An <see cref="IChannelDataStore"/> object in which the channel data
			/// is to be returned.</param>
			virtual void GetChannelData(IChannelDataStore ^channelData)
			{
				if (_next != nullptr) _next->GetChannelData(channelData);
			}
		};

		/// <summary>
		/// Provides a client channel sink that resets the timeout <see cref="LifeTime.counter"/> every time
		/// a Remoting message s processed and monitors number of active calls.
		/// </summary>
		private ref class TimeoutClientChannelSinkProvider : public IClientChannelSinkProvider
		{
		private:
			/// <summary>Next channel sink provider in the chain.</summary>
			IClientChannelSinkProvider ^_next;
			
			/// <summary>
			/// This is a pass-thru client sink that just resets timeout <see cref="LifeTime.counter"/> every time
			/// a message is processed and also monitors number of active calls.
			/// </summary>
			ref class TimeoutClientChannelSink : public IClientChannelSink
			{
			private:
				/// <summary>Next channel sink in the sink chain.</summary>
				IClientChannelSink ^_nextSink;

			public:
				/// <summary>
				/// Creates a new <see cref="TimeoutClientChannelSink"/>.
				/// </summary>
				/// <param name="nextSink">Next channel sink in the sink chain.</param>
				TimeoutClientChannelSink(IClientChannelSink ^nextSink) : _nextSink(nextSink)
				{ }

				/// <summary>Gets the next client channel sink in the server sink chain.</summary>
				virtual property IClientChannelSink ^NextChannelSink
				{
					IClientChannelSink ^get()
					{
						return _nextSink;
					}
				}

				/// <summary>Gets a dictionary through which properties on the sink can be accessed.</summary>
				virtual property IDictionary ^Properties
				{
					IDictionary ^get()
					{
						return _nextSink == nullptr ? nullptr : _nextSink->Properties;
					}
				}

				/// <summary>
				/// Returns the <see cref="Stream"/> onto which the provided message is to be serialized.
				/// </summary>
				/// <param name="msg">The <see cref="IMethodCallMessage"/> containing details about the method call.
				/// </param>
				/// <param name="headers">The headers to add to the outgoing message heading to the server.
				/// </param>
				/// <returns>The <see cref="Stream"/> onto which the provided message is to be serialized.</returns>
				virtual Stream ^GetRequestStream(IMessage ^msg, ITransportHeaders ^headers)
				{
					if (_nextSink != nullptr)
					{
						return _nextSink->GetRequestStream(msg, headers);
					} 
					else return nullptr;
				}

				/// <summary>
				/// Requests asynchronous processing of a method call on the current sink.
				/// </summary>
				/// <param name="sinkStack">A stack of channel sinks that called this sink.</param>
				/// <param name="msg">The message to process.</param>
				/// <param name="headers">The headers to add to the outgoing message heading to the server.</param>
				/// <param name="stream">The stream headed to the transport sink.</param>
				virtual void AsyncProcessRequest(IClientChannelSinkStack ^sinkStack, IMessage ^msg, 
					ITransportHeaders ^headers, Stream ^stream)
				{
					LifeTime::ResetTimeout();
					if (_nextSink != nullptr) 
					{
						_nextSink->AsyncProcessRequest(sinkStack, msg, headers, stream);
					}
				}

				/// <summary>
				/// Requests asynchronous processing of a response to a method call on the current sink.
				/// </summary>
				/// <param name="sinkStack">A stack of sinks that called this sink.</param>
				/// <param name="state">Information generated on the request side that is associated with 
				/// this sink.</param>
				/// <param name="headers">The headers retrieved from the server response stream.</param>
				/// <param name="stream">The stream coming back from the transport sink.</param>
				virtual void AsyncProcessResponse(IClientResponseChannelSinkStack ^sinkStack,
					Object ^state, ITransportHeaders ^headers, Stream ^stream)
				{
					LifeTime::ResetTimeout();
					// unsupported by the underlying transports sink anyway
					if (_nextSink != nullptr)
					{
						_nextSink->AsyncProcessResponse(sinkStack, state, headers, stream);
					}
				}

				/// <summary>
				/// Requests message processing from the current sink.
				/// </summary>
				/// <param name="msg">The message to process.</param>
				/// <param name="requestHeaders">The headers to add to the outgoing message heading to the server.
				/// </param>
				/// <param name="requestStream">The stream headed to the transport sink.</param>
				/// <param name="responseHeaders">When this method returns, contains an <see cref="ITransportHeaders"/>
				/// interface that holds the headers that the server returned.</param>
				/// <param name="responseStream">When this method returns, contains a <see cref="Stream"/> coming back from
				/// the transport sink.</param>
				virtual void ProcessMessage(IMessage ^msg, ITransportHeaders ^requestHeaders,
					Stream ^requestStream, ITransportHeaders ^%responseHeaders, Stream ^%responseStream)
				{
					LifeTime::ResetTimeout();
					LifeTime::IncrementActiveCalls();
					try
					{
						if (_nextSink != nullptr)
						{
							_nextSink->ProcessMessage(msg, requestHeaders, requestStream,
								responseHeaders, responseStream);
						}
					}
					finally
					{
						LifeTime::DecrementActiveCalls();
					}
				}
			};

		public:
			/// <summary>Gets or sets the next sink provider in the channel sink provider chain.</summary>
			virtual property IClientChannelSinkProvider ^Next
			{
				IClientChannelSinkProvider ^get()
				{
					return _next;
				}
				void set(IClientChannelSinkProvider ^value)
				{
					_next = value;
				}
			}
	
			/// <summary>
			/// Creates a sink chain.
			/// </summary>
			/// <param name="channel">Channel for which the current sink chain is being constructed.</param>
			/// <param name="url">The URL of the object to connect to.</param>
			/// <param name="remoteChannelData">A channel data object describing a channel on the remote server.</param>
			/// <returns>The first sink of the newly formed channel sink chain, or a null reference 
			/// indicating that this provider will not or cannot provide a connection for this endpoint.
			/// </returns>
			virtual IClientChannelSink ^CreateSink(IChannelSender ^channel, String ^url, 
				Object ^remoteChannelData)
			{
				IClientChannelSink ^nextSink = nullptr;
				if (_next != nullptr) nextSink = _next->CreateSink(channel, url, remoteChannelData);

				return gcnew TimeoutClientChannelSink(nextSink);
			}
		};
	}
}

// Types used in php4ts.dll (ExtSupport) initializing/finalizing.
typedef void (__stdcall *pfnEnsureInit)(void);
typedef void (__stdcall *pfnForceTerm)(void);

using namespace PHP::ExtManager;

// This is the entry point for this application.
// Note: different function names and prototypes have to be used for Debug and Release build.
#ifdef DEBUG

int _tmain(int argc, _TCHAR *argv[], _TCHAR *envp[])
{
	_TCHAR *argv1 = _T("");
	if (argc > 1) argv1 = argv[1];

#else

int WINAPI WinMain(
	HINSTANCE hInstance,      // handle to current instance
	HINSTANCE hPrevInstance,  // handle to previous instance
	LPSTR lpCmdLine,          // command line
	int nCmdShow)             // show state
{
	// convert command line argument into Unicode
	_TCHAR argv1[128];
	int _ret = MultiByteToWideChar(CP_ACP, MB_PRECOMPOSED, lpCmdLine, -1, argv1, sizeof(argv1));
	if (_ret > 0) argv1[_ret - 1] = 0; else argv1[0] = 0;

#endif

	// no 'error' message boxes
	SetErrorMode(SEM_NOOPENFILEERRORBOX | SEM_FAILCRITICALERRORS);

#ifdef DEBUG
	System::Diagnostics::Debug::Listeners->Add(gcnew System::Diagnostics::TextWriterTraceListener(Console::Out));
	System::Diagnostics::Debug::AutoFlush = true;
	Debug::WriteLine("EXT MGR", "Phalanger Extension Manager starting...");
#endif

	PHP::Core::ExtensionLibraryDescriptor::ServerMode = true;
	PHP::Core::Configuration::Load(PHP::Core::ApplicationContext::Default);

	// lifetime (of Request instances)
	LifetimeServices::LeaseTime             = TimeSpan::FromSeconds(15); // initial lease time
	LifetimeServices::LeaseManagerPollTime  = TimeSpan::FromSeconds(10); // lease manager act. interval
	LifetimeServices::RenewOnCallTime       = TimeSpan::FromSeconds(10); // lease extension on call
	LifetimeServices::SponsorshipTimeout    = TimeSpan::FromSeconds(5);  // sponsor timeout

	// create server formatters
	BinaryServerFormatterSinkProvider ^server_formatter_provider =
		gcnew BinaryServerFormatterSinkProvider();
	server_formatter_provider->TypeFilterLevel =
		System::Runtime::Serialization::Formatters::TypeFilterLevel::Full;
	server_formatter_provider->Next = gcnew TimeoutServerChannelSinkProvider();

	BinaryServerFormatterSinkProvider ^server_formatter_provider_inst =
		gcnew BinaryServerFormatterSinkProvider();
	server_formatter_provider_inst->TypeFilterLevel =
		System::Runtime::Serialization::Formatters::TypeFilterLevel::Full;
	server_formatter_provider_inst->Next = gcnew TimeoutServerChannelSinkProvider();
		
	// create client formatters
	BinaryClientFormatterSinkProvider ^client_formatter_provider =
		gcnew BinaryClientFormatterSinkProvider();
	client_formatter_provider->Next = gcnew TimeoutClientChannelSinkProvider();
		
	BinaryClientFormatterSinkProvider ^client_formatter_provider_inst =
		gcnew BinaryClientFormatterSinkProvider();
	client_formatter_provider_inst->Next = gcnew TimeoutClientChannelSinkProvider();
		
	Hashtable ^properties = gcnew Hashtable(3);

	// Create channels and register them.
	// Note: two channels are created. One of them is a channel with a well-known section name.
	// The other one has a unique section name. Remote dispatcher can be called through both of them.
	// However, if you use the well-known one, you might be talking to different instances of ExtManager
	// every time you make a call. This is alright when there is no request context. If you are operating
	// in a request context, all calls must be served by the same instance of ExtManager. Therefore, you
	// should first call RemoteDispatcher::GetInstanceUrl to obtain a URL through which you then connect
	// to RemoteDispatcher again. But with this connection, you can be sure that as long as your request
	// is active, "your" ExtManager doesn't terminate (unless something terrible happens, of course).
	// ExtManager can be gracefully terminated by calling RemoteDispatcher::GracefulShutdown. Usually
	// you will do this after you have detected a change in configuration.

	try
	{
		properties["section"]  = "phalanger";
		properties["priority"] = 10;
		properties["name"]     = "shm1";

		// Binary formatter -> Timeout sink -> Transport channel
		RemoteDispatcher::Channel = gcnew ShmChannel(properties, client_formatter_provider,
			server_formatter_provider);
		ChannelServices::RegisterChannel(RemoteDispatcher::Channel, false);

		// now create and register the "instance" channel

		String ^section = Guid::NewGuid().ToString();
		properties["section"]  = section;
		
		// setting higher priority for the instance channel than for the general channel ensures
		// that when a reference to a MarshalByRef object residing in ExtSupport is created, its
		// URL is set to the one handled by instance channel (this is especially important when
		// dealing with IRequestTerminator / RequestCookie.Terminator)
		properties["priority"] = 20;
		properties["name"]     = "shm2";

		// Binary formatter -> Timeout sink -> Transport channel
		RemoteDispatcher::InstanceChannel = gcnew ShmChannel(properties, client_formatter_provider_inst, 
			server_formatter_provider_inst);
		ChannelServices::RegisterChannel(RemoteDispatcher::InstanceChannel, false);
	}
#ifdef DEBUG
	catch (System::Exception ^e)
	{
		Debug::WriteLine("EXT MGR", e->ToString());
		
		// unable to start listening => another instance of ExtManager running?
		Debug::WriteLine("EXT MGR", "Another instance running?");
#else
	catch (System::Exception ^)
	{
#endif

		// if an event name was given as a command line parameter, signal that event,
		// so that the parent process won't wait for nothing...
		if (argv1[0] != 0)
		{
			HANDLE event = OpenEvent(EVENT_MODIFY_STATE, false, argv1);
			if (event != NULL)
			{
				SetEvent(event);
				CloseHandle(event);
			}
		}
		return -1;
	}

	// initialize winsock
	WORD version_requested = MAKEWORD(2, 0);
	WSADATA wsa_data;

	int err_code = WSAStartup(version_requested, &wsa_data);
	if (err_code != 0) throw gcnew System::Net::Sockets::SocketException(err_code);

	StartupHelper::InitializeCRTX();
	RemoteDispatcher::ExtSupportInit();
	
	Type ^class_type =
		Type::GetType("PHP.ExtManager.RemoteDispatcher,php4ts,Version=2.2.0.0,Culture=neutral,PublicKeyToken=43b6773fb05dc4f0");
			
	// register our well-known type and tell the server to connect the type to the endpoint 
	RemotingConfiguration::RegisterWellKnownServiceType(
		class_type, 
		RemoteDispatcher::ExtManEndPoint,
		WellKnownObjectMode::Singleton);
	
	// if an event name was given as a command line parameter, signal that event to indicate
	// that we are ready to handle incoming calls
	if (argv1[0] != 0)
	{
		HANDLE event = OpenEvent(EVENT_MODIFY_STATE, false, argv1);
		if (event != NULL)
		{
			SetEvent(event);
			CloseHandle(event);
		}
	}

	// read processLifetime configuration item
	int process_lifetime = -1;
	Specialized::NameValueCollection ^nvc = dynamic_cast<Specialized::NameValueCollection ^>
		(ConfigurationSettings::AppSettings);
	if (nvc != nullptr)
	{
		try
		{
			process_lifetime = Int32::Parse(nvc["processLifetime"]);
		}
		catch (Exception ^)
		{ }
	}
	
	// now serve!
#ifdef DEBUG
	Debug::WriteLine("EXT MGR","");
#endif
	LifeTime::Wait(process_lifetime);

	// no request are active now -> shutdown all loaded modules
	StartupHelper::UnloadExtensions();
	StartupHelper::ShutdownCRTX();

	WSACleanup();

	return 0;
}
