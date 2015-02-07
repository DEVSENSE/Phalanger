/*

 Copyright (c) 2004-2006 Ladislav Prosek.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

//
// ExtSupport - substitute for php4ts.dll/php5ts.dll
//
// Request.cpp
// - contains definition of Request class
//

#include "stdafx.h"

#include "Request.h"
#include "Memory.h"
#include "Module.h"
#include "Resources.h"
#include "IniConfig.h"
#include "Misc.h"
#include "Variables.h"
#include "Errors.h"
#include "RemoteDispatcher.h"
#include "Objects.h"

#include <malloc.h>

using namespace System;
using namespace System::Threading;
using namespace System::Collections;

using namespace PHP::Core;
using namespace PHP::ExtManager;

namespace PHP
{
	namespace ExtManager
	{
		// Creates a new <see cref="Request"/> and associates it with current thread.
		Request::Request()
		{
#ifdef DEBUG
			Debug::WriteLine("EXT SUP", "Request::Request");
#endif
			destroyed = 1;
			
			// it is not allowed to have >1 request in one thread
			Request ^request = GetCurrentRequest(false, false);
			if (request != nullptr)
			{
				//throw gcnew InvalidOperationException(ExtResources::GetString("thread_already_associated_with_request"));
				request->Terminate();
			}

			//// if we are not collocated, obtain current RequestCookie
			//if (StartupHelper::IsCollocated == false)
			//{
			//	cookie = RequestCookie::GetCurrentThreadCookie();
			//	if (cookie == nullptr)
			//	{
			//		throw gcnew InvalidOperationException(ExtResources::GetString("thread_not_associated_with_cookie"));
			//	}
			//	else
			//	{
			//		// set the method to be called when the client side knows for sure that there
			//		// will not be more external function calls on behalf of this request
			//		cookie->Terminator = static_cast<IRequestTerminator ^>(this);
			//	}
			//}
			//else
			{
				// if we are collocated reference the config directly
				AppConfig = PHP::Core::Configuration::Application;
				ExtConfig = ExtensionLibraryDescriptor::CollocatedExtensions;
			}

			CallContext::SetData(RequestThreadSlotName, gcnew ObjectWrapper(this));

			// throw all errors that occured when ExtManager was starting up
			IEnumerator ^enumerator = StartupHelper::StartupErrors->GetEnumerator();
			while (enumerator->MoveNext())
			{
				RemoteDispatcher::ThrowException(PhpError::Error, static_cast<String ^>(enumerator->Current));
			}

			destroyed = 0;

			// init fields
			CurrentInvocationContext.FunctionArgCount = 0;
			CurrentInvocationContext.FunctionPhpArgs = NULL;
			CurrentInvocationContext.FunctionArgs = nullptr;
			CurrentInvocationContext.FunctionName = NULL;
			CurrentInvocationContext.FunctionType = 0;
			PhpInfoBuilder = nullptr;
			StartedModules = gcnew Hashtable(10);
			TransientConstants = gcnew Hashtable();
			TransientClasses = gcnew PHP::Core::OrderedHashtable<String ^>();
#ifdef PHP5TS
			ZendObjectHandles = gcnew scg::Dictionary<PhpObject ^, IntPtr>();
#endif
			DontDestroyResources = false;
			HttpResponseHeader = NULL;

			// create unique logical thread ID
			logicalThreadId = (DWORD)malloc(1);
			tsrmThreadStorage = NULL;
			tsrm_update_native_tls(tsrmThreadStorage);

			// let the TSRM know that we have a new request - a new logical thread
			TSRMLS_FETCH();
			Resources = &(EG(regular_list));

#if defined(PHP4TS)
			zend_init_rsrc_list(Resources);
#elif defined(PHP5TS)
			zend_init_rsrc_list(Resources TSRMLS_CC);
#else
			Debug::Assert(false);
#endif

#if defined(PHP4TS) || defined(PHP5TS)
			php_startup_ticks(TSRMLS_C);
#endif

			PG(last_error_message) = NULL;
			PG(last_error_file) = NULL;
			PG(last_error_lineno) = 0;
			PG(error_handling) = EH_NORMAL;

#if defined(PHP4TS) || defined(PHP5TS)
			if (!LCG(seeded)) lcg_seed(TSRMLS_C);
#endif

			Interlocked::Increment(numberOfRequests);

			// start early init extensions
			int count = Module::GetEarlyInitModuleCount();
			for (int i = 0; i < count; i++)
			{
				Module ^mod = Module::GetEarlyInitModule(i);
				mod->RequestStartup(this);
				StartedModules->Add(mod->GetFileName(), mod);
			}
		}

		// Finalizer cancels the association with current thread and releases unmanaged resources
		// allocated by this request: memory, Zend resources, etc.
		Request::~Request()
		{
			// has this 'destructor' already been called?
			if (Interlocked::Exchange(destroyed, 1)) return;

			try
			{
				GC::SuppressFinalize(this);

#ifdef DEBUG
				Debug::WriteLine("EXT SUP", "Request::~Request");
#endif

				// Set current request - this is necessary because the object can be finalized by
				// an arbitrary thread (by GC thread or more likely by lifetime services thread if 
				// called from Renewal method).
				CallContext::SetData(RequestThreadSlotName, gcnew ObjectWrapper(this));
				tsrm_update_native_tls(tsrmThreadStorage);

				// notify lifetime bound objects
				try
				{
					if (lifetimeBoundMBRs != nullptr)
					{
						IDictionaryEnumerator ^enumerator = static_cast<Hashtable ^>
							(lifetimeBoundMBRs->Clone())->GetEnumerator();
						while (enumerator->MoveNext())
						{
							(static_cast<ILifetimeBoundMBR ^>(enumerator->Key))->Expire();
						}
					}
				}
				catch (Exception ^)
				{ }

				// shutdown modules
				try
				{
					IDictionaryEnumerator ^enumerator = StartedModules->GetEnumerator();
					while (enumerator->MoveNext() == true)
					{
						Module ^module = static_cast<Module ^>(enumerator->Value);
						module->RequestShutdown(this);
					}
				}
				catch (Exception ^)
				{ }

				TSRMLS_FETCH();

#ifdef PHP5TS
				//remove object handle dictionary
				for each (IntPtr varptr in ZendObjectHandles->Values)
				{
					zval *var = (zval *)varptr.ToPointer();
					zval_ptr_dtor(&var);
				}
				ZendObjectHandles->Clear();
#endif

#if defined(PHP4TS) || defined(PHP5TS)
				request_shutdown_streams(TSRMLS_C);
#endif

				// free HttpResponseHeader
				if (HttpResponseHeader != NULL)
				{
					zval_dtor(*HttpResponseHeader);
					efree(HttpResponseHeader);
				}

				// free	CurrentInvocationContext.FunctionPhpArgs
				if (LastFunctionPhpArgs != NULL) efree(LastFunctionPhpArgs);

				// free resources
#if defined(PHP4TS)
				zend_destroy_rsrc_list(Resources);
#elif defined(PHP5TS)
				zend_destroy_rsrc_list(Resources TSRMLS_CC);
#else
				Debug::Assert(false);
#endif

				ts_free_thread();

				// free logical thread ID
				free((void *)logicalThreadId);

				CallContext::FreeNamedDataSlot(RequestThreadSlotName);
			}
			finally
			{
				Interlocked::Decrement(numberOfRequests);
			}
		}
	}
}
