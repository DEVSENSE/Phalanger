//
// ExtSupport - substitute for php4ts.dll/php5ts.dll
//
// Request.h
// - contains declaration of Request class
// - contains definition of VariableProxy class
//

#pragma once

#include "stdafx.h"
#include "ExtSupport.h"
#include "Module.h"
#include "Memory.h"
#include "TsrmLs.h"
#include "PhpMarshaler.h"
#include "Variables.h"
#include "AssemblyInternals.h"
#include <stdio.h>

using namespace System;
using namespace System::Text;
using namespace System::Threading;
using namespace System::Collections;
using namespace System::Runtime::Remoting::Lifetime;
using namespace System::Runtime::Remoting::Messaging;

using namespace PHP::Core;

/*

  Designed and implemented by Ladislav Prosek.
  
*/

namespace PHP
{
	namespace ExtManager
	{
		/// <summary>
		/// Marks a <see cref="MarshalByRefObject"/> whose lifetime should be bound to the lifetime of
		/// a <see cref="Request"/>.
		/// </summary>
		/// <remarks>
		/// Objects implementing this interface should register with the appropriate <see cref="Request"/>
		/// by calling its <see cref="Request.RegisterLifetimeBoundMBR"/>.
		/// </remarks>
		private interface class ILifetimeBoundMBR
		{
			/// <summary>
			/// Invoked by the <see cref="Request"/> the implementor is bound to, to signalize that
			/// its lifetime expires.
			/// </summary>
			void Expire();
		};

		/// <summary>
		/// Request context holder. Marshaled by reference.
		/// </summary>
		/// <remarks>
		/// Instances of this class represent per-request context. References are stored in
		/// <see cref="CallContext"/> and implementing <see cref="ILogicalThreadAffinative"/> marker
		/// interface ensures that the references are preserved across remote calls.
		/// </remarks>
		private ref class Request : public MarshalByRefObject, public ILogicalThreadAffinative, public IRequestTerminator
		{
		public:
			/// <summary>
			/// Encapsulates information about the current function/method invocation.
			/// </summary>
			/// <remarks>
			/// There is one-to-many relalation between a request and this structure because callbacks issued by
			/// external functions may invoke another external function. Stack of <see cref="InvocationContext"/>s
			/// is maintained implicitly along with call stack (<see cref="Function.Invoke"/> stores previous
			/// content of <see cref="Request.CurrentInvocationContext"/>).
			/// </remarks>
			value struct InvocationContext
			{
				/// <summary>
				/// Number of arguments of the currently executing external function.
				/// </summary>
				int FunctionArgCount;

				/// <summary>
				/// Arguments of the currently executing external function in original managed form.
				/// </summary>
				array<Object ^> ^FunctionArgs;

				/// <summary>
				/// Arguments of the recently executing external function in the marshaled unmanaged form.
				/// </summary>
				/// <remarks>
				/// <para>
				/// When an external function is invoked, <see cref="CurrentInvocationContext.FunctionArgs"/> is initialized to the actual
				/// parameters and items of <see cref="CurrentInvocationContext.FunctionPhpArgs"/> are all set to zero. Managed to native
				/// marshaling is lazy - that means that it is performed when the function asks for its parameters.
				/// </para>
				/// <para>
				/// <see cref="CurrentInvocationContext.FunctionPhpArgs"/> is not freed until the next external function is called, in order to
				/// be able to perform the variable binding (see <see cref="RemoteDispatcher.BindParameter"/>).
				/// </para>
				/// </remarks>
				zval **FunctionPhpArgs;

				/// <summary>
				/// Marks arguments of the currently executing external function that should be passed by reference.
				/// </summary>
				/// <remarks>
				/// <B>true</B> means that the corrensponding argument should be passed by reference.
				/// </remarks>
				array<bool> ^FunctionRefArray;
				
				/// <summary>Name of the currently executing external function.</summary>
				char *FunctionName;

				/// <summary>The module that contains currently executing external function.</summary>
				Module ^Module;

				/// <summary>The type of the currently executing external function (at the moment always ZEND_INTERNAL_FUNCTION).</summary>
				int FunctionType;

			};

			/// <summary>
			/// Creates a new <see cref="Request"/> and associates it with current thread.
			/// </summary>
			Request();

			/// <summary>
			/// Finalizer cancels the association with current thread and releases unmanaged resources
			/// allocated by this request: memory, Zend resources, etc.
			/// </summary>
			~Request();

			/// <summary>
			/// Just a remotely callable wrapper around destructor (<see cref="Object.Finalize"/>) that also
			/// destroys lifetime bound <see cref="MarshalByRefObject"/>s.
			/// </summary>
			/// <remarks>
			/// This method is called by <c>RequestCookie</c> when the request ends or the client thread dies
			/// (<c>RequestCookie</c> is finalized).
			/// </remarks>
			virtual void Terminate()
			{
#ifdef DEBUG
				Debug::WriteLine("EXT SUP", "Request::Terminate");
#endif

				delete this;
			}

			/// <summary>
			/// Obtains a lifetime service object to control the lifetime policy for this instance.
			/// </summary>
			/// <returns>An object of type <see cref="ILease"/> used to control the lifetime policy for this
			/// instance.</returns>
			/// <remarks>
			/// <see cref="MarshalByRefObject.InitializeLifetimeService"/> is overriden in order to be able to
			/// register a sponsor. The associated <c>RequestCookie</c> becomes this object's lifetime sponsor.
			/// <seealso cref="ISponsor"/>
			/// </remarks>
			virtual Object ^InitializeLifetimeService() override
			{
				ILease ^lease = static_cast<ILease ^>(MarshalByRefObject::InitializeLifetimeService());
//#pragma warning (push)
//#pragma warning (disable: 4538)
//				if (cookie != nullptr) lease->Register(cookie);
//#pragma warning (pop)
				return lease;
			}

			/// <summary>
			/// Registers a <see cref="MarshalByRefObject"/> implementing the <see cref="ILifetimeBoundMBR"/> interface
			/// as a lifetime bound object.
			/// </summary>
			/// <param name="mbr">The <see cref="MarshalByRefObject"/> whose lifetime should be bound to the lifetime
			/// of this <see cref="Request"/>.</param>
			void RegisterLifetimeBoundMBR(ILifetimeBoundMBR ^mbr)
			{
				Monitor::Enter(this);
				try
				{
					if (lifetimeBoundMBRs == nullptr) lifetimeBoundMBRs = gcnew Hashtable();
					lifetimeBoundMBRs->Add(mbr, nullptr);
				}
				finally
				{
					Monitor::Exit(this);
				}
			}

			/// <summary>
			/// Removes a <see cref="MarshalByRefObject"/> implementing the <see cref="ILifetimeBoundMBR"/> interface
			/// from the list of lifetime bound objects.
			/// </summary>
			/// <param name="mbr">The <see cref="MarshalByRefObject"/> whose lifetime should not be bound to the lifetime
			/// of this <see cref="Request"/> anymore.</param>
			void DeregisterLifetimeBoundMBR(ILifetimeBoundMBR ^mbr)
			{
				Monitor::Enter(this);
				try
				{
					if (lifetimeBoundMBRs != nullptr) lifetimeBoundMBRs->Remove(mbr);
				}
				finally
				{
					Monitor::Exit(this);
				}
			}

			/*/// <summary>
			/// Read-only access to the request cookie.
			/// </summary>
			/// <remarks>
			/// Returns <c>RequestCookie</c> associated with this request or <B>null</B> when this <c>ExtManager</c>
			/// is collocated.
			/// </remarks>
			PHP::Core::RequestCookie ^GetCookie()
			{
				return cookie;
			}*/

			/// <summary>
			/// Information about the current function/method invocation.
			/// </summary>
			InvocationContext CurrentInvocationContext;

			/// <summary>The module that contains recently executing external function.</summary>
			Module ^LastModule;

			/// <summary>Number of arguments of the recently executing external function.</summary>
			int LastFunctionArgCount;

			/// <summary>
			/// Arguments of the recently executing external function in the marshaled unmanaged form.
			/// </summary>
			zval **LastFunctionPhpArgs;

			// Moved to _zend_alloc_globals for performance reasons.
			/// <summary>Head of a doubly linked list of memory blocks allocated by this request.</summary>
			//MEMORY_BLOCK_HEADER *MemBlocks;
			/// <summary>Stores free memory blocks of <c>sizeof(zval)</c> length for faster allocation.</summary>
			//MEMORY_BLOCK_HEADER *ZvalBlocks;

			/// <summary>
			/// Modules that have been started (their <c>request_startup</c> has been called) during
			/// processing of this request.
			/// </summary>
			/// <remarks>
			/// Modules are started in a lazy manner so we have to keep track of the modules that actually
			/// are aware of this <see cref="Request"/>.
			/// </remarks>
			Hashtable ^StartedModules;

			/// <summary>
			///	Constants that have been registered without the <c>CONST_PERSISTENT</c> flag during
			/// processing of this request.
			/// </summary>
			Hashtable ^TransientConstants;

			/// <summary>
			///	Classes that have been registered during processing of this request.
			/// </summary>
			PHP::Core::OrderedHashtable<String ^> ^TransientClasses;	// GENERIC scg::Dictionary<String^, Class^>

#ifdef PHP5TS
			/// <summary>
			/// Stores object store handles of PhpObjects, which represent Zend-handled objects.
			/// </summary>
			scg::Dictionary<PhpObject ^, IntPtr> ^ZendObjectHandles;
#endif

			/// <summary>Zend resources registered during processing of this request.</summary>
			HashTable *Resources;

			/// <summary>A string to be returned from <c>phpinfo</c>.</summary>
			StringBuilder ^PhpInfoBuilder;

			/// <summary>
			/// If <B>true</B>, <c>zval_dtor</c> doesn't call <c>zend_list_delete</c>.
			/// </summary>
			/// <remarks>
			/// If this field is set to <B>true</B> Zend resources held by this <see cref="Request"/>
			/// are not destroyed implicitly by destroying <c>zval</c>s.
			/// </remarks>
			bool DontDestroyResources;

			/// <summary>
			/// Used in HttpWrapper.cpp.
			/// </summary>
			zval **HttpResponseHeader;

			/// <summary>
			/// Globals configuration that should be used during this request.
			/// </summary>
			/// <remarks>
			/// If we are running collocated, this is just a reference to the true global configuration
			/// living in Core and is set in this class's constructor. If we are isolated it is reference
			/// to a copy of global configuration passed to us via <see cref="RemoteDispatcher.GetInstanceUrl"/>.
			/// </remarks>
			ApplicationConfiguration ^AppConfig;
			
			/// <summary>
			/// Extension configuration that should be used during this request (keys are extension names,
			/// values are <c>ExtensionLibraryDescriptor</c>s).
			/// </summary>
			/// <remarks>
			/// If we are running collocated, this is just a reference to the <c>collocatedExtensions</c> collection
			/// living in Core and is set in this class's constructor. If we are isolated it is reference
			/// to a copy of the configuration passed to us via <see cref="RemoteDispatcher.GetInstanceUrl"/>.
			/// </remarks>
			scg::Dictionary<String ^, ExtensionLibraryDescriptor^> ^ExtConfig;

		private:
			/// <summary>Non-zero if this request has already been destroyed.</summary>
			int destroyed;

			/// <summary>
			/// List of <see cref="MarshalByRefObject"/>s implementing the <see cref="ILifetimeBoundMBR"/> interface
			/// whose lifetime is bound to the lifetime of this instance.
			/// </summary>
			Hashtable ^lifetimeBoundMBRs;

			/*/// <summary>
			/// Reference to the <c>RequestCookie</c> associated with this request. This cookie lives\
			/// on the client side. One of its purposes is to terminate the request when the client thread dies.
			/// It also provides an empty <c>Ping</c> method which is (remotely) called to make sure that the client
			/// thread is still alive. See <see cref="MarshalByRefObject"/>, <see cref="ILease"/>, <see cref="ISponsor"/>.
			PHP::Core::RequestCookie ^cookie;*/

			/// <summary>Unique logical thread ID (used by the Zend TSRM).</summary>
			DWORD logicalThreadId;

			/// <summary>Thread local storage used by the Zend TSRM (see <c>TsrmLs.cpp</c>).</summary>
			tsrm_tls_entry *tsrmThreadStorage;

			// Static members
		public:

			/// <summary>
			/// Returns unique logical thread ID. Used by the Zend TSRM.
			/// </summary>
			/// <returns>The unique logical thread ID.</returns>
			static DWORD GetLogicalThreadId()
			{
				Request ^request = GetCurrentRequest(false, false);
				return (request == nullptr ? 1 : request->logicalThreadId);
			}

			/// <summary>
			/// Gets TLS value for the current logical thread. Used by the Zend TSRM.
			/// </summary>
			/// <returns>The TLS pointer. <seealso cref="tsrmThreadStorage"/>.</returns>
			static tsrm_tls_entry *GetThreadStorage()
			{
				Request ^request = GetCurrentRequest(false, false);

				// no request -> return a special thread storage
				return (request == nullptr ? generalThreadStorage : request->tsrmThreadStorage);
			}

			/// <summary>
			/// Sets TLS value for the current logical thread. Used by the Zend TSRM.<summary>
			/// </summary>
			/// <param name="entry">The new TLS value.</param>
			static void SetThreadStorage(tsrm_tls_entry *entry)
			{
				Request ^request = GetCurrentRequest(false, false);

				// no request -> set our special thread storage
				if (request == nullptr) generalThreadStorage = entry;
				else request->tsrmThreadStorage = entry;
			}

			/// <summary>
			/// Gets the request associated with current logical thread.
			/// </summary>
			/// <returns>The request associated with current logical thread.</returns>
			/// <remarks>
			/// Creates new if one does not exist for current logical thread. Throws an exception if running
			/// in 'module context'.
			/// </remarks>
			static Request ^GetCurrentRequest()
			{
				return GetCurrentRequest(true, true);
			}

			/// <summary>
			/// Gets the request associated with current logical thread.
			/// </summary>
			/// <param name="wantException"/>Throws an exception if current thread is running in 'module context'
			/// (as opposed to 'request context').</param>
			/// <param name="createNew"/>Creates the request if one does not exist for current logical thread.
			/// </param>
			/// <returns>The request associated with current logical thread.</returns>
			static Request ^GetCurrentRequest(bool wantException, bool createNew)
			{
				if (Module::ModuleBoundContext != nullptr)
				{
					if (wantException)
					{
						throw gcnew InvalidOperationException(ExtResources::GetString("invalid_module_context"));
					}
					else return nullptr;
				}
				
				ObjectWrapper ^requestwrap = static_cast<ObjectWrapper ^>(CallContext::GetData(RequestThreadSlotName));
				if (requestwrap != nullptr)
				{
					return static_cast<Request ^>(requestwrap->Object);
				}
				else
				{
					return createNew ? gcnew Request() : nullptr;
				}
				//if (requestwrap == nullptr && createNew) return gcnew Request();
				//return request;
			}

			/// <summary
			/// Determines whether current logical thread is associated with a request.
			/// </summary>
			/// <returns><B>true</B> if the current thread is associated with a request, <B>false</B> otherwise.
			/// </returns>
			static bool RequestsExists()
			{
				return (CallContext::GetData(RequestThreadSlotName) != nullptr);
			}

			/// <summary>Ensures that a request is associated with current logical thread.</summary>
			/// <returns>The request.</returns>
			static Request ^EnsureRequestExists()
			{
				Request ^request = GetCurrentRequest(true, true);
				tsrm_update_native_tls(request->tsrmThreadStorage);
				return request;
			}

			/// <summary>
			/// Ensures that a given <see cref="Module"/> is started (its <c>request_startup</c> handler has
			/// been called) for the current request.
			/// </summary>
			/// <param name="module">The module.</param>
			static void EnsureModuleStarted(Module ^module)
			{
				if (module == nullptr) return;
				
				Request ^request = Request::GetCurrentRequest();
				if (request->StartedModules->ContainsKey(module->GetFileName()) == false)
				{
					module->RequestStartup(request);
					request->StartedModules->Add(module->GetFileName(), module);
				}
			}

			/// <summary>Destroys the request associated with current logical thread.</summary>
			static void Destroy()
			{
				Request ^request = GetCurrentRequest(false, false);
				if (request != nullptr) request->Terminate();
			}

			/// <summary>
			/// Returns number of requests that are currently active.
			/// </summary>
			/// <returns>The number of requests that are currently active.</returns>
			static int GetNumberOfRequests()
			{
				return Interlocked::CompareExchange(numberOfRequests, -1, -1);
			}

		private:
			/// <summary>Number of requests that are currently active.</summary>
            static int numberOfRequests = 0;

			/// <summary>TLS value for the Zend TSRM when running in module context.</summary>
			static tsrm_tls_entry *generalThreadStorage = NULL;

		public:
			/// <summary>
			/// Name of the thread slot where <see cref="Request"/> instances are stored.
			/// </summary>
			/// <remarks>
			/// Eventually assigned to by <see cref="StartupHelper.Collocate"/>.
			/// </remarks>
#if defined(PHP4TS)
			static String ^RequestThreadSlotName = "ExtManager4:Request";
#elif defined(PHP5TS)
			static String ^RequestThreadSlotName = "ExtManager5:Request";
#endif

			static property ApplicationConfiguration ^AppConf
			{
				static ApplicationConfiguration ^get()
				{
					Request ^request = GetCurrentRequest(false, false);
					if (request != nullptr && request->AppConfig != nullptr) return request->AppConfig;

					return PHP::Core::Configuration::Application;
				}
			}

		internal:
			String^ currentWorkingDirectory;
		};

		/// <summary>
		/// Class of remotable, marshaled by reference objects that serve as proxies of native PHP variables (<c>zval<//c>s).
		/// </summary>
		private ref class VariableProxy : public MarshalByRefObject, public ISponsor, public ILifetimeBoundMBR,
											public IExternalVariable
		{
		public:
			/// <summary>
			/// Creates a new <see cref="VariableProxy"/> wrapping a given <c>zval</c>.
			/// </summary>
			/// <param name="variable">The <c>zval</c> to wrap.</param>
			/// <param name="module">The <see cref="Module"/> that holds the variable.</param>
			VariableProxy(zval *variable, Module ^module)
			{
				zval_add_ref(&variable);
				this->variable = variable;
				this->isExpired = false;
				this->module = module;

				Request ^request = Request::GetCurrentRequest();
				request->RegisterLifetimeBoundMBR(this);
			}

			/// <summary>
			/// Obtains a lifetime service object to control the lifetime policy for this instance.
			/// </summary>
			/// <returns>An object of type <see cref="ILease"/> used to control the lifetime policy for this
			/// instance.</returns>
			/// <remarks>
			/// <see cref="MarshalByRefObject.InitializeLifetimeService"/> is overriden in order to be able to
			/// register a sponsor. This very object becomes its own sponsor. <seealso cref="ISponsor"/>
			/// </remarks>
			virtual Object ^InitializeLifetimeService() override
			{
				ILease ^lease = static_cast<ILease ^>(MarshalByRefObject::InitializeLifetimeService());
				lease->Register(this);
				return lease;
			}

			// ISponsor implementation

			/// <summary>
			/// Requests a sponsoring client to renew the lease for this object.
			/// </summary>
			/// <param name="lease">The lifetime lease of the object that requires lease renewal.</param>
			/// <returns>The additional lease time for the specified object.</returns>
			virtual TimeSpan Renewal(ILease ^lease)
			{
#ifdef DEBUG
				Debug::WriteLine("EXT SUP", "VariableProxy::Renewal");
#endif

				if (isExpired) return TimeSpan::Zero;
				else return lease->RenewOnCallTime;
			}

			// ILifetimeBoundMBR implementation

			// Invoked by the <see cref="Request"/> this instance is bound to, to signalize that
			// the lifetime expires.
			virtual void Expire()
			{
				Unbind();
				isExpired = true;
			}

			// IExternalVariable implementation

			/// <summary>
			/// Retrieves the underlying variable's value.
			/// </summary>
			/// <returns>The value.</returns>
			virtual Object ^GetValue()
			{
				if (variable == NULL) return nullptr;

				tsrm_update_native_tls(Request::GetThreadStorage());
				return PhpMarshaler::GetInstance(module)->MarshalNativeToManaged(IntPtr(variable));
			}

			/// <summary>
			/// Sets the underlying variable's value.
			/// </summary>
			/// <param name="value">The value.</param>
			virtual void SetValue(Object ^value)
			{
				if (variable == NULL) return;

				zval *unmng_value = (zval *)PhpMarshaler::GetInstance(module)->MarshalManagedToNative(value).ToPointer();

				Request ^request = Request::EnsureRequestExists();
				
				request->DontDestroyResources = true;
				zval_dtor(variable);
				request->DontDestroyResources = false;

				variable->type = unmng_value->type;
				variable->value = unmng_value->value;

				efree(unmng_value);
			}

			/// <summary>
			/// Unbinds this variable so that this instance can be discarded and the underlying <c>zval</c> released.
			/// </summary>
			virtual void Unbind()
			{
				if (variable == NULL) return;

				Request ^request = Request::EnsureRequestExists();

				zval *var = variable;
				zval_ptr_dtor(&var);
				variable = NULL;

				request->DeregisterLifetimeBoundMBR(this);
				isExpired = true;
			}

		private:
			/// <summary>Pointer to the underlying native PHP variable (<c>zval</c>).</summary>
			zval *variable;

			/// <summary>
			/// <B>true</B> if this instance has expired and should no longer be retained, <B>false</B>
			/// otherwise.
			/// </summary>
			bool isExpired;

			/// <summary>
			/// The <see cref="Module"/> for which this variable should be marshaled (i.e. be given as a parameter
			/// to <see cref="PhpMarshaler.GetInstance"/>).
			/// </summary>
			Module ^module;
		};
	}
}
