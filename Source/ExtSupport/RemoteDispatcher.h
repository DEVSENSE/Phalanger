//
// ExtSupport - substitute for php4ts.dll/php5ts.dll
//
// RemoteDispatcher.h
// - contains declaration of RemoteDispatcher class
//

#pragma once

#include "stdafx.h"
#include "Request.h"
#include "TsrmLs.h"
#include "IniConfig.h"
#include "Misc.h"

using namespace System;
using namespace System::Collections;
using namespace System::Runtime::Remoting::Channels;
using namespace System::Runtime::Remoting::Lifetime;

/*

  Designed and implemented by Ladislav Prosek.
  
*/

namespace PHP
{
	namespace ExtManager
	{
		/// <summary>
		/// Implementation of the <c>IExternals</c> interface. Dispatches external calls.
		/// </summary>
		/// <remarks>
		/// A singleton instance of this class is constructed by .NET Remoting and called
		/// through a transparent proxy from Core (for extensions configured as isolated),
		/// or by the Core itself and called directly (for extensions configured as
		/// collocated).
		/// </remarks>
		public ref class RemoteDispatcher : public MarshalByRefObject, public IExternals
		{
		public:
#ifdef DEBUG
			/// <summary>
			/// Creates a new <see cref="RemoteDispatcher"/>.
			/// </summary>
			RemoteDispatcher()
			{
				Debug::WriteLine("EXT SUP", "RemoteDispatcher singleton constructed");
			}
			/// <summary>
			/// Finalizes this <see cref="RemoteDispatcher"/>.
			/// </summary>
			~RemoteDispatcher()
			{
				// should never get here unless the hosting process is terminating
				Debug::WriteLine("EXT SUP", "RemoteDispatcher singleton finalized");
			}
#endif

			/// <summary>
			/// Obtains a lifetime service object to control the lifetime policy for this instance.
			/// </summary>
			/// <returns>An object of type <see cref="ILease"/> used to control the lifetime policy for this
			/// instance.</returns>
			/// <remarks>
			/// Returns null lifetime service, which means that the singleton should never be
			/// finalized. See <a href="http://www.ingorammer.com/RemotingFAQ/SINGLETON_IS_DYING.html">
			/// http://www.ingorammer.com/RemotingFAQ/SINGLETON_IS_DYING.html</a>.
			/// </remarks>
			virtual Object ^InitializeLifetimeService() override
			{
				return nullptr;
			}

			// IExternals implementation

			/// <summary>
			/// Get external function proxy object used for direct invocation. 
			/// </summary>
			/// <remarks>Cannot return null. In case of missing module or function, special wrapper throwing PHP warning is returned.</remarks>
			virtual IExternalFunction^/*!*/GetFunctionProxy(String ^moduleName, String ^className, String ^functionName);

			/// <summary>
			/// Invokes an external function.
			/// </summary>
			/// <include file='../Core/Doc/Externals.xml' path='doc/method[@name="InvokeFunction"]/*'/>
			virtual Object ^InvokeFunction(String ^moduleName, String ^functionName,
				array<Object ^> ^%args, array<int> ^refInfo, String ^workingDir);

			/// <summary>
			/// Invokes an external method.
			/// </summary>
			/// <include file='../Core/Doc/Externals.xml' path='doc/method[@name="InvokeMethod"]/*'/>
			virtual Object ^InvokeMethod(String ^moduleName, String ^className, String ^methodName,
				PhpObject ^%self, array<Object ^> ^%args, array<int> ^refInfo, String ^workingDir);

			/// <summary>
			/// Returns a proxy of a variable (<c>zval</c>) that lives in <c>ExtManager</c> as one of the last function/method
			/// invocation parameters.
			/// </summary>
			/// <include file='../Core/Doc/Externals.xml' path='doc/method[@name="BindParameter"]/*'/>
			virtual IExternalVariable ^BindParameter(int paramIndex);

			/// <summary>
			/// Returns a proxy of a native PHP stream wrapper that lives in <c>ExtManager</c>.
			/// </summary>
			/// <include file='../Core/Doc/Externals.xml' path='doc/method[@name="GetStreamWrapper"]/*'/>
			virtual IExternalStreamWrapper ^GetStreamWrapper(String ^scheme);

			/// <summary>
			/// Returns an <see cref="ICollection"/> of schemes of all available external stream wrappers.
			/// </summary>
			/// <include file='../Core/Doc/Externals.xml' path='doc/method[@name="GetStreamWrapperSchemes"]/*'/>
			virtual ICollection ^GetStreamWrapperSchemes();

			/// <summary>
			/// Returns an <see cref="ICollection"/> of error messages.
			/// </summary>
			/// <include file='Doc/Externals.xml' path='doc/method[@name="GetStartupErrors"]/*'/>
			virtual ICollection ^GetStartupErrors();

			/// <summary>
			/// Gathers information about loaded extensions.
			/// </summary>
			/// <include file='../Core/Doc/Externals.xml' path='doc/method[@name="PhpInfo"]/*'/>
			virtual String ^PhpInfo();

			/// <summary>
			/// Returns an <see cref="ICollection"/> of names of extensions that are currently loaded.
			/// </summary>
			/// <include file='../Core/Doc/Externals.xml' path='doc/method[@name="GetModules"]/*'/>
			virtual ICollection ^GetModules(bool internalNames);

			/// <summary>
			/// Checks whether a given extension is currently loaded.
			/// </summary>
			/// <include file='../Core/Doc/Externals.xml' path='doc/method[@name="GetModuleVersion"]/*'/>
			virtual String ^GetModuleVersion(String ^moduleName, bool internalName, bool %loaded);

			/// <summary>
			/// Returns an <see cref="ICollection"/> of names of functions in a given extension.
			/// </summary>
			/// <include file='../Core/Doc/Externals.xml' path='doc/method[@name="GetFunctionsByModule"]/*'/>
			virtual ICollection ^GetFunctionsByModule(String ^moduleName, bool internalName);

			/// <summary>
			/// Returns an <see cref="ICollection"/> of names of classes registered by a given extension.
			/// </summary>
			/// <include file='../Core/Doc/Externals.xml' path='doc/method[@name="GetClassesByModule"]/*'/>
			virtual ICollection ^GetClassesByModule(String ^moduleName, bool internalName);

			/// <summary>
			/// Generates the managed wrapper for a given extension.
			/// </summary>
			/// <include file='../Core/Doc/Externals.xml' path='doc/method[@name="GenerateManagedWrapper"]/*'/>
			virtual String ^GenerateManagedWrapper(String ^moduleName);

			/// <summary>
			/// Instructs the <c>ExtManager</c> to load an extension.
			/// </summary>
			/// <include file='Doc/Externals.xml' path='doc/method[@name="LoadExtensions"]/*'/>
			virtual bool LoadExtension(ExtensionLibraryDescriptor ^descriptor);

			/// <summary>
			/// Associates calling <see cref="Thread"/> with a new request.
			/// <seealso cref="IRequestTerminator"/>
			/// </summary>
			/// <include file='../Core/Doc/Externals.xml' path='doc/method[@name="BeginRequest"]/*'/>
			virtual void BeginRequest()
			{
#ifdef DEBUG
				Debug::WriteLine("EXT SUP", "ExtManager::BeginRequest");
#endif
				gcnew Request();
			}

			/// <summary>
			/// Terminates the request currently associated with calling <see cref="Thread"/>.
			/// <seealso cref="IRequestTerminator"/>
			/// </summary>
			/// <include file='../Core/Doc/Externals.xml' path='doc/method[@name="EndRequest"]/*'/>
			virtual void EndRequest()
			{
#ifdef DEBUG
				try
				{
					Debug::WriteLine("EXT SUP", "ExtManager::EndRequest");
#endif
					Request::Destroy();
#ifdef DEBUG
				}
				catch (Exception ^e)
				{
					Debug::WriteLine("EXT SUP", e->ToString());
					throw e;
				}
#endif
			}

			/// <summary>
			/// Returns the private URL of the current <c>ExtManager</c>.
			/// </summary>
			/// <include file='../Core/Doc/Externals.xml' path='doc/method[@name="GetInstanceUrl"]/*'/>
			virtual String ^GetInstanceUrl(String ^generalUrl, ApplicationConfiguration ^appConfig, scg::Dictionary<String^, ExtensionLibraryDescriptor^> ^extConfig);
			
			/// <summary>
			/// Instructs the <c>ExtManager</c> to shut down gracefully.
			/// </summary>
			/// <include file='../Core/Doc/Externals.xml' path='doc/method[@name="GracefulShutdown"]/*'/>
			virtual void GracefulShutdown();

			/// <summary>
			/// Sets an INI value that might have an effect on an extension.
			/// </summary>
			/// <include file='../Core/Doc/Externals.xml' path='doc/method[@name="IniSet"]/*'/>
			virtual bool IniSet(String ^varName, String ^newValue, String ^%oldValue);

			/// <summary>
			/// Gets an INI value related to extensions.
			/// </summary>
			/// <include file='../Core/Doc/Externals.xml' path='doc/method[@name="IniGet"]/*'/>
			virtual bool IniGet(String ^varName, String ^%value);

			/// <summary>
			/// Restores an INI value related to extensions.
			/// </summary>
			/// <include file='../Core/Doc/Externals.xml' path='doc/method[@name="IniRestore"]/*'/>
			virtual bool IniRestore(String ^varName);
			
			/// <summary>
			/// Gets all INI entry names and values.
			/// </summary>
			/// <include file='../Core/Doc/Externals.xml' path='doc/method[@name="IniGetAll"]/*'/>
			virtual PhpArray ^IniGetAll(String ^extension);

			/// <summary>
			/// Determines whether a given extension registered a given INI entry name.
			/// </summary>
			/// <include file='../Core/Doc/Externals.xml' path='doc/method[@name="IniOptionExists"]/*'/>
			virtual bool IniOptionExists(String ^moduleName, String ^varName);

		private:
			
			// static members
		public:

			/// <summary>
			/// Performs original PHP engine components initialization.
			/// </summary>
			static void ExtSupportInit();

			/// <summary>
			/// Instructs the <c>ExtManager</c> to shut down gracefully.
			/// </summary>
			static void StaticGracefulShutdown();

			/// <summary>
			/// Returns number of requests that are currently active.
			/// </summary>
			/// <returns>The number of requests that are currently active.</returns>
			static int GetNumberOfRequests()
			{
				return Request::GetNumberOfRequests();
			}

			/// <summary>
			/// Remoting channel.
			/// </summary>
			static IChannel ^Channel;

			/// <summary>
			/// Remoting channel specific for this <c>ExtManager</c> instance.
			/// </summary>
			static IChannel ^InstanceChannel;

			/// <summary>
			/// If non-zero, this <c>ExtManager</c> instance is shutting down (does not accept new requests).
			/// </summary>
			static int ShuttingDown = 0;

			/// <summary>
			/// If set, this <c>ExtManager</c> instance is shutting down (does not accept new requests).
			/// </summary>
			static AutoResetEvent ^ShuttingDownEvent = gcnew AutoResetEvent(false);

			/// <summary>
			/// Name of the well-known <c>ExtManager</c> Remoting endpoint.
			/// </summary>
			static String ^ExtManEndPoint  = "ExtManager";

		internal:
			/// <summary>
			/// Throws a <c>PhpException</c> in an appropriate way so that it propagates back to Core.
			/// </summary>
			/// <param name="errType">The error severity.</param>
			/// <param name="message">The error message.</param>
			static void ThrowException(PhpError errType, String ^message);

			/// <summary>
			/// Represents invalid(missing) function. 
			/// </summary>
			/// <remarks>
			/// This proxy is used just to throw specified PHP error.
			/// </remarks>
			ref class InvalidExternalFunction : public MarshalByRefObject, public PHP::Core::IExternalFunction
			{
			internal:
				InvalidExternalFunction(String^ message)
				{
					this->message = message;
				}
			public:

				virtual Object^ Invoke(PhpObject^ self, array<Object ^> ^%args, array<int> ^refInfo, String ^workingDir)
				{
					RemoteDispatcher::ThrowException(PhpError::Error, message);

					args = nullptr;
					return nullptr;
				}

				virtual property IExternals^ ExtManager{IExternals^ get(){return nullptr;}}

			private:
				String^ message;
			};

		private:
			// Like zend_ini_string but returns NULL when the entry could not be found.
			static char *zend_ini_string_fixed(String ^name, bool originalValue);
		};
	}
}
