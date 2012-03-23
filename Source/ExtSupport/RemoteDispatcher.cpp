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
// RemoteDispatcher.cpp
// - contains definition of RemoteDispatcher class
//

#include "stdafx.h"

#include "RemoteDispatcher.h"
//#include "VirtualWorkingDir.h"
#include "StreamProxy.h"
#include "Module.h"
#include "Request.h"
#include "Errors.h"
#include "Hash.h"
#include "Strings.h"
#include "AssemblyInternals.h"

using namespace System;
using namespace System::Threading;
using namespace System::Runtime::Remoting;
using namespace System::Runtime::Remoting::Channels;
using namespace System::Runtime::Remoting::Channels::Tcp;

using namespace PHP::Core;

#if defined(SAPI_SUPPORT)
static int sapi_cli_ub_write(const char *str, uint str_length TSRMLS_DC)
{
	return str_length;
}


static void sapi_cli_flush(void *server_context)
{
}

static void sapi_cli_register_variables(zval *track_vars_array TSRMLS_DC)
{
}

static void sapi_cli_log_message(char *message)
{
	Debug::WriteLine("EXT SUP", gcnew String(message));
}

static int sapi_cli_deactivate(TSRMLS_D)
{
	return SUCCESS;
}

static char* sapi_cli_read_cookies(TSRMLS_D)
{
	return NULL;
}

static int sapi_cli_send_headers(sapi_headers_struct *sapi_headers TSRMLS_DC)
{
	return SAPI_HEADER_SENT_SUCCESSFULLY;
}

static void sapi_cli_send_header(sapi_header_struct *sapi_header, void *server_context TSRMLS_DC)
{
}

static int php_cli_startup(sapi_module_struct *sapi_module)
{
	if (php_module_startup(sapi_module, NULL, 0) == FAILURE)
	{
		return FAILURE;
	}
	return SUCCESS;
}

static void sapi_cli_ini_defaults(HashTable *configuration_hash)
{
}
#endif

namespace PHP
{
	namespace ExtManager
	{
		// Get external function proxy object used for direct invocation. 
		// The function cannot return null. In case of missing module or function, special wrapper throwing PHP warning is returned.
		IExternalFunction^/*!*/RemoteDispatcher::GetFunctionProxy(String ^moduleName, String ^className, String ^functionName)
		{
#define FULL_FUNCTION_NAME(className, functionName) ((className == nullptr) ? functionName : String::Format("{0}::{1}", className, functionName))

			//Request::EnsureRequestExists();

			// resolve module
			Module ^module = Module::GetModule(moduleName);
			if (module == nullptr)
				return gcnew InvalidExternalFunction(
					ExtResources::GetString("undefined_external_module_called", FULL_FUNCTION_NAME(className, functionName), moduleName));
			
			if (className == nullptr)
			{
				// resolve function
				Function ^function = module->GetFunctionByName(functionName);
				if (function == nullptr)
					return gcnew InvalidExternalFunction(ExtResources::GetString("undefined_external_function_called", functionName, moduleName));
				
				return function;
			}
			else
			{
				// resolve class
				Class ^cls = module->GetClassByName(className);
				if (cls == nullptr)
					return gcnew InvalidExternalFunction(ExtResources::GetString("undefined_external_class_called", FULL_FUNCTION_NAME(className, functionName), moduleName));
				
				// resolve method
				Method ^method = cls->GetMethodByName(functionName);
				if (method == nullptr)
					return gcnew InvalidExternalFunction(ExtResources::GetString("undefined_external_function_called", FULL_FUNCTION_NAME(className, functionName), moduleName));
				
				return method;
			}

#undef FULL_FUNCTION_NAME
		}

		// Invokes an external function.
		Object ^RemoteDispatcher::InvokeFunction(String ^moduleName, String ^functionName,
			array<Object ^> ^%args, array<int> ^refInfo, String ^workingDir)
		{
#ifdef DEBUG
			Debug::WriteLine("EXT SUP", String::Format("# about to call {0} in module {1} with {2} arguments",
				functionName, moduleName, args == nullptr ? 0 : args->Length));
			Debug::Indent();

			try
			{
#endif
				IExternalFunction^ function = GetFunctionProxy(moduleName, nullptr, functionName);
				
#ifdef DEBUG
				try
				{
#endif
					// CALL!
					return function->Invoke(nullptr, args, refInfo, workingDir);
#ifdef DEBUG
				}
				finally
				{
					Debug::Unindent();
				}
			}
			catch (Exception ^e)
			{
				Debug::WriteLine("EXT SUP", "# exception caught (to be rethrown):");
				Debug::WriteLine("EXT SUP", e->ToString());
				throw e;
			}
#endif
		}

		// Invokes an external method.
		Object ^RemoteDispatcher::InvokeMethod(String ^moduleName, String ^className,
			String ^methodName, PhpObject ^%self, array<Object ^> ^%args, array<int> ^refInfo,
			String ^workingDir)
		{
#ifdef DEBUG
			Debug::WriteLine("EXT SUP", String::Format("# about to call {0} in module {1} with {2} arguments",
				String::Format("{0}::{1}", className, methodName), moduleName, args == nullptr ? 0 :
				args->Length));
			Debug::Indent();
			
			try
			{
#endif
				IExternalFunction^ method = GetFunctionProxy(moduleName, className, methodName);
#ifdef DEBUG
				try
				{
#endif
					// CALL!
					return method->Invoke(self, args, refInfo, workingDir);
#ifdef DEBUG
				}
				finally
				{
					Debug::Unindent();
				}
			}
			catch (Exception ^e)
			{
				Debug::WriteLine("EXT SUP", "# exception caught (to be rethrown):");
				Debug::WriteLine("EXT SUP", e->ToString());
				throw e;
			}
#endif
		}

		// Returns a proxy of a variable (<c>zval</c>) that lives in <c>ExtManager</c> as one of the last function/method
		// invocation parameters.
		IExternalVariable ^RemoteDispatcher::BindParameter(int paramIndex)
		{
			Request ^request = Request::EnsureRequestExists();
			if (request->LastFunctionPhpArgs == NULL || paramIndex >= request->LastFunctionArgCount) return nullptr;

			zval *var = request->LastFunctionPhpArgs[paramIndex];
			if (var == NULL) return nullptr;

			return gcnew VariableProxy(var, request->LastModule);
		}

		// Returns a proxy of a native PHP stream wrapper that lives in <c>ExtManager</c>.
		IExternalStreamWrapper ^RemoteDispatcher::GetStreamWrapper(String ^scheme)
		{
#ifdef DEBUG
			Debug::WriteLine("EXT SUP", String::Format("RemoteDispatcher::GetStreamWrapper(\"{0}\")", scheme));
#endif
			Request::EnsureRequestExists();
			return StreamWrapperProxy::CreateWrapperProxy(scheme);
		}

		// Returns an <see cref="ICollection"/> of schemes of all available external stream wrappers.
		ICollection ^RemoteDispatcher::GetStreamWrapperSchemes()
		{
#ifdef DEBUG
			Debug::WriteLine("EXT SUP", "RemoteDispatcher::GetStreamWrapperSchemes()");
#endif
			if (Module::GetEarlyInitModuleCount() > 0) Request::EnsureRequestExists();
			return StreamWrapperProxy::GetWrapperSchemes();
		}

		// Returns an <see cref="ICollection"/> of error messages.
		ICollection ^RemoteDispatcher::GetStartupErrors()
		{
			if (Module::GetEarlyInitModuleCount() > 0) Request::EnsureRequestExists();
			return StartupHelper::StartupErrors;
		}

		// Gathers information about loaded extensions.
		String ^RemoteDispatcher::PhpInfo()
		{			
			Request ^request;

#ifdef DEBUG
			Debug::WriteLine("EXT SUP", "RemoteDispatcher::PhpInfo");
			Debug::Indent();
#endif
			try
			{
				request = Request::EnsureRequestExists();
				request->PhpInfoBuilder = gcnew StringBuilder(1024);

				int count = Module::GetModuleCount();
				ArrayList ^additional_modules = gcnew ArrayList();

				for (int i = 0; i < count; i++)
				{
					Module ^mod = Module::GetModule(i);

					// skip stream wrappers and extensions that are currently not configured
					if (String::Compare(mod->GetModuleName(), Externals::BuiltInStreamWrappersExtensionName, true) == 0 ||
						request->ExtConfig->ContainsKey(mod->GetFileName()) == false) continue;
					
#ifdef DEBUG
					Debug::WriteLine("EXT SUP", mod->GetFileName());
					Debug::Indent();

					try
					{
#endif
						if (mod->IsEarlyInit() == false && request->StartedModules->ContainsKey(mod->GetFileName()) == false)
						{
							// no function from this module has been called on behalf of the current request
							try
							{
								mod->RequestStartup(request);
								request->StartedModules->Add(mod->GetFileName(), mod);
							}
#ifdef DEBUG
							catch (Exception ^e)
							{
								Debug::WriteLine("EXT SUP", e->ToString());
							}
#else
							catch (Exception ^) { }
#endif
						}

#ifdef DEBUG
						try
						{
#endif
							// if there is no info_func handler, add the module to additional modules
							if (mod->PhpInfo() == false) additional_modules->Add(mod->GetModuleName());
#ifdef DEBUG
						}
						catch (Exception ^e)
						{
							Debug::WriteLine("EXT SUP", e->ToString());
							throw e;
						}
					}
					finally
					{
						Debug::Unindent();
					}
#endif
				}

				count = additional_modules->Count;
				if (count > 0)
				{
					// we have some modules without info_func handler
					request->PhpInfoBuilder->Append(PhpNetInfo::PrintSectionCaption(false, "Additional Modules"));
					request->PhpInfoBuilder->Append(PhpNetInfo::PrintTableStart(false));

					array<String ^> ^args = gcnew array<String ^>(1);
					args[0] = "Module Name";

					// append table 'Additional Modules'
					request->PhpInfoBuilder->Append(PhpNetInfo::PrintTableHeader(false, args));
					
					for (int i = 0; i < count; i++)
					{
						args[0] = dynamic_cast<String ^>(additional_modules[i]);
						request->PhpInfoBuilder->Append(PhpNetInfo::PrintTableRow(false, args));
					}

					request->PhpInfoBuilder->Append(PhpNetInfo::PrintTableEnd(false));
				}

				String ^result = request->PhpInfoBuilder->ToString();
				return result;
			}
			finally
			{
				request->PhpInfoBuilder = nullptr;
#ifdef DEBUG
				Debug::Unindent();
#endif
			}
		}

		// Returns an <see cref="ICollection"/> of names of extensions that are currently loaded.
		ICollection ^RemoteDispatcher::GetModules(bool internalNames)
		{
			ICollection ^col = internalNames ? Module::GetInternalModuleNames() : Module::GetModuleNames();
			array<String ^> ^names = gcnew array<String ^>(col->Count);

			col->CopyTo(names, 0);
			return static_cast<System::Array ^>(names);
		}

		// Checks whether a given extension is currently loaded.
		String ^RemoteDispatcher::GetModuleVersion(String ^moduleName, bool internalName, bool %loaded)
		{
			// resolve module
			Module ^module = internalName ? Module::GetModuleByInternalName(moduleName) : Module::GetModule(moduleName);
			if (module == nullptr)
			{
				loaded = false;
				return nullptr;
			}
			else
			{
				loaded = true;
				return module->GetVersion();
			}
		}

		// Returns an <see cref="ICollection"/> of names of functions in a given extension.
		ICollection ^RemoteDispatcher::GetFunctionsByModule(String ^moduleName, bool internalName)
		{
			if (Module::GetEarlyInitModuleCount() > 0) Request::EnsureRequestExists();

			// resolve module
			Module ^module = internalName ? Module::GetModuleByInternalName(moduleName) : Module::GetModule(moduleName);
			if (module == nullptr) return nullptr;

			ICollection ^col = module->GetFunctionNames();
			array<String ^> ^names = gcnew array<String ^>(col->Count);

			col->CopyTo(names, 0);
			return static_cast<System::Array ^>(names);
		}

		// Returns an <see cref="ICollection"/> of names of classes registered by a given extension.
		ICollection ^RemoteDispatcher::GetClassesByModule(String ^moduleName, bool internalName)
		{
			if (Module::GetEarlyInitModuleCount() > 0) Request::EnsureRequestExists();

			// resolve module
			Module ^module = internalName ? Module::GetModuleByInternalName(moduleName) : Module::GetModule(moduleName);
			if (module == nullptr) return nullptr;

			ICollection ^col = module->GetClassNames();
			array<String ^> ^names = gcnew array<String ^>(col->Count);

			col->CopyTo(names, 0);
			return static_cast<System::Array ^>(names);
		}

		// Generates the managed wrapper for a given extension.
		String ^RemoteDispatcher::GenerateManagedWrapper(String ^moduleName)
		{
			// resolve module
			Module ^module = Module::GetModule(moduleName);

			if (module == nullptr)
			{
				try
				{
					// load with default path and with earlyInit
					module = gcnew DynamicModule(
						PHP::Core::Configuration::Application->Paths->ExtNatives,
						moduleName,
						true);

					module->RequestStartup(Request::GetCurrentRequest());
					String ^res = module->GenerateManagedWrapper();

					module->ModuleShutdown();
					return res;
				}
				catch (CouldNotLoadExtensionException ^e)
				{
					return e->Message;
				}
			}
			else
			{
				if (module->IsEarlyInit()) Request::EnsureRequestExists();
				return module->GenerateManagedWrapper();
			}
		}

		// Instructs the <c>ExtManager</c> to load an extension.
		bool RemoteDispatcher::LoadExtension(ExtensionLibraryDescriptor ^descriptor)
		{
			tsrm_update_native_tls(Request::GetThreadStorage());

			// skip earlyInit collocated extensions if running under IIS
			if (
				//StartupHelper::IsCollocated &&
				descriptor->EarlyInit)					
			{
				RequestContext ^req_context = RequestContext::CurrentContext;
				if (req_context != nullptr) return false;
			}

			Monitor::Enter(Module::typeid);
			try
			{
				if (Module::GetModule(descriptor->FileName) == nullptr)
				{
					try
					{
						PHP::ExtManager::DynamicModule::LoadDynamicModule(descriptor);
					}
					catch (Exception ^e)
					{
			#ifdef DEBUG
						Debug::WriteLine("EXT SUP", System::String::Format("Loading failed ({0})", e->ToString()));
			#endif
						StartupHelper::StartupErrors->Add(/*ExtResources::GetString("error_during_extension_loading",
							descriptor->FileName, e->Message)*/e->StackTrace);
					}
					return true;
				}
			}
			finally
			{
				Monitor::Exit(Module::typeid);
			}
			return false;
		}

		// Returns the private URL of the current <c>ExtManager</c>.
		String ^RemoteDispatcher::GetInstanceUrl(String ^generalUrl, ApplicationConfiguration ^appConfig,
			scg::Dictionary<String ^, ExtensionLibraryDescriptor^> ^extConfig)
		{
#ifdef DEBUG
			Debug::WriteLine("EXT SUP", "RemoteDispatcher::GetInstanceUrl");
#endif
			Debug::Assert(appConfig != nullptr && extConfig != nullptr);

			// start a new request now!
			// (so that client can be sure that we don't time out after publishing our instance URL)
			Request ^request = Request::EnsureRequestExists();
			request->AppConfig = appConfig;
			request->ExtConfig = extConfig;

			bool loaded = false;
			for each (scg::KeyValuePair<String ^, ExtensionLibraryDescriptor^> entry in extConfig)
			{
				if (LoadExtension(entry.Value)) loaded = true;
			}

			// if at least one extension was loaded, recreate the Request
			if (loaded)
			{
				request->Terminate();
				request = gcnew Request();
				request->AppConfig = appConfig;
				request->ExtConfig = extConfig;
			}

			IChannelReceiver ^recChan = static_cast<IChannelReceiver ^>(RemoteDispatcher::InstanceChannel);
			array<String ^> ^urls = recChan->GetUrlsForUri(ExtManEndPoint);

			// The following code handles the situation when TCP channel is used - it is necessary
			// to pick the right local IP address so that we are reachable for the client.
			
			int longest = 0, length = 0, indOfColon = generalUrl->IndexOf(':');
			if (indOfColon == -1) return urls[0];

			for (int i = urls->Length - 1; i >= 0; i--)
			{
				int index = urls[i]->IndexOf(':');
				if (index >= 0 && String::Compare(generalUrl, 0, urls[i], 0, index) == 0)
				{
					if (index > length)
					{
						length = index;
						longest = i;
					}
				}
			}

			return urls[longest];
		}

		// Instructs the <c>ExtManager</c> to shut down gracefully.
		void RemoteDispatcher::GracefulShutdown()
		{
			StaticGracefulShutdown();
		}

		// Instructs the <c>ExtManager</c> to shut down gracefully.
		void RemoteDispatcher::StaticGracefulShutdown()
		{
			if (Interlocked::CompareExchange(ShuttingDown, -1, -1) == 0)
			{
#ifdef DEBUG
				Debug::WriteLine("EXT SUP", "RemoteDispatcher::GracefulShutdown");
#endif

				IChannelReceiver ^rec_chan = static_cast<IChannelReceiver ^>(Channel);
				rec_chan->StopListening(nullptr);
				ChannelServices::UnregisterChannel(Channel);

				System::Threading::Interlocked::Exchange(ShuttingDown, 1);
				ShuttingDownEvent->Set();
			}
		}

		// Sets an INI value that might have an effect on an extension.
		bool RemoteDispatcher::IniSet(String ^varName, String ^newValue, String ^%oldValue)
		{
#ifdef DEBUG
			Debug::WriteLine("EXT SUP", String::Concat("RemoteDispatcher::IniSet(\"", varName,
				"\", \"", newValue, "\")"));
#endif

			// make sure EG(ini_directives) is initialized
			Request::EnsureRequestExists();

			char *ntv_old_value = zend_ini_string_fixed(varName, 0);
			if (ntv_old_value == NULL) return false;
			
			oldValue = gcnew String(ntv_old_value, 0, strlen(ntv_old_value),
				Request::AppConf->Globalization->PageEncoding);

			char *ntv_new_value = PhpMarshaler::MarshalManagedStringToNativeString(newValue);
			char *ntv_var_name  = PhpMarshaler::MarshalManagedStringToNativeString(varName);
			try
			{
				return (zend_alter_ini_entry(ntv_var_name, varName->Length + 1, ntv_new_value,
					newValue->Length, ZEND_INI_USER, ZEND_INI_STAGE_RUNTIME) == SUCCESS);
			}
			finally
			{
				efree(ntv_var_name);
				efree(ntv_new_value);
			}
		}

		// Gets an INI value related to extensions.
		bool RemoteDispatcher::IniGet(String ^varName, String ^%value)
		{
#ifdef DEBUG
			Debug::WriteLine("EXT SUP", String::Format("RemoteDispatcher::IniGet(\"{0}\")", varName));
#endif

			// make sure EG(ini_directives) is initialized
			Request::EnsureRequestExists();

			char *str = zend_ini_string_fixed(varName, 0);
			
			if (str == NULL) return false;
			else
			{
				value = gcnew String(str, 0, strlen(str),
					Request::AppConf->Globalization->PageEncoding);
				return true;
			}
		}

		// Restores an INI value related to extensions.
		bool RemoteDispatcher::IniRestore(String ^varName)
		{
#ifdef DEBUG
			Debug::WriteLine("EXT SUP", String::Format("RemoteDispatcher::IniRestore(\"{0}\")", varName));
#endif

			// make sure EG(ini_directives) is initialized
			Request::EnsureRequestExists();

			char *ntv_var_name = PhpMarshaler::MarshalManagedStringToNativeString(varName);
			try
			{
				return (zend_restore_ini_entry(ntv_var_name, varName->Length + 1, ZEND_INI_STAGE_RUNTIME) == SUCCESS);
			}
			finally
			{
				efree(ntv_var_name);
			}
		}
			
		// Gets all INI entry names and values.
		PhpArray ^RemoteDispatcher::IniGetAll(String ^extension)
		{
#ifdef DEBUG
			Debug::WriteLine("EXT SUP", String::Format("RemoteDispatcher::IniGetAll(\"{0}\")", extension ?
				extension : "NULL"));
#endif

			// make sure EG(ini_directives) is initialized
			Request::EnsureRequestExists();

			int ext_number = 0;
			if (extension != nullptr)
			{
				// Note that we call Module::GetModuleByInternalName and not Module::GetModule which means
				// that the parameter is extension's name and not file name.
				PHP::ExtManager::Module ^module = Module::GetModuleByInternalName(extension);
				if (module == nullptr) return nullptr;

				ext_number = module->GetModuleNumber();
				Request::EnsureModuleStarted(module);
			}
			else
			{
				int count = Module::GetModuleCount();
				for (int i = 0; i < count; i++) Request::EnsureModuleStarted(Module::GetModule(i));
			}

			// walk EG(ini_directives) and convert items into managed form
			PhpArray ^array = gcnew PhpArray();
			HashPosition pos;
			zval **elem;
			char *string_key;
			unsigned string_key_len;
			unsigned long num_key;

			TSRMLS_FETCH();

			zend_hash_internal_pointer_reset_ex(EG(ini_directives), &pos);
			while (zend_hash_get_current_data_ex(EG(ini_directives), (void **)&elem, &pos) == SUCCESS)
			{
				if (zend_hash_get_current_key_ex(EG(ini_directives), &string_key, &string_key_len, 
					&num_key, 0, &pos) == HASH_KEY_IS_LONG)
				{
					// key is int, should not happen -> do nothing
				}
				else
				{
					// key is a string
					zend_ini_entry *ini_entry = (zend_ini_entry *)elem;
					
					if (ext_number == 0 || ext_number == ini_entry->module_number)
					{
						PhpArray ^triple = gcnew PhpArray(0, 3);

						// add "global_value"
						String ^key = "global_value";
						if (ini_entry->orig_value)
						{
							triple->Add(key, gcnew String(ini_entry->orig_value, 0, ini_entry->orig_value_length));
						}
						else if (ini_entry->value)
						{
							triple->Add(key, gcnew String(ini_entry->value, 0, ini_entry->value_length));
						} 
						else triple->Add(key, nullptr);

						// add "local_value"
						triple->Add("local_value", gcnew String(ini_entry->value, 0, ini_entry->value_length));

						// add "access"
						triple->Add("access", ini_entry->modifiable);

						// add the triple to the outer array
						array->Add(gcnew String(string_key, 0, string_key_len - 1), triple);
					}
				}

				zend_hash_move_forward_ex(EG(ini_directives), &pos);
			}

			return array;
		}

		// Determines whether a given extension registered a given INI entry name.
		bool RemoteDispatcher::IniOptionExists(String ^moduleName, String ^varName)
		{
#ifdef DEBUG
			Debug::WriteLine("EXT SUP", String::Format("RemoteDispatcher::IniOptionExists(\"{0}, {1}\")", moduleName, varName));
#endif

			// make sure this module is loaded
			//LoadExtension(moduleName, true);

			// resolve module
			Module ^module = Module::GetModule(moduleName);
			if (module == nullptr) return false;

			// make sure EG(ini_directives) is initialized
			Request::EnsureRequestExists();

			TSRMLS_FETCH();
			zend_copy_ini_directives(tsrm_ls);

			char *ntv_var_name = PhpMarshaler::MarshalManagedStringToNativeString(varName);

			try
			{
				zend_ini_entry *ini_entry;
				if (zend_hash_find(EG(ini_directives), ntv_var_name, varName->Length + 1, (void **)&ini_entry) == SUCCESS)
				{
					return (module->GetModuleNumber() == ini_entry->module_number);
				}
			}
			finally
			{
				efree(ntv_var_name);
			}

			return false;
		}

		// Performs original PHP engine components initialization.
		void RemoteDispatcher::ExtSupportInit()
		{
#ifdef DEBUG
			Debug::WriteLine("EXT SUP", "ExtSupportInit invoked.");
#endif
			mx_module_entries = tsrm_mutex_alloc();
			tsrm_startup(128, 20, 0, NULL);

			ts_allocate_id(&alloc_globals_id, sizeof(zend_alloc_globals), (ts_allocate_ctor)alloc_globals_ctor,
				(ts_allocate_dtor)alloc_globals_dtor);

			zend_ini_startup(NULL);

			zend_init_rsrc_list_dtors();
			le_index_ptr = zend_register_list_destructors_ex(NULL, NULL, "index pointer", 0);

			le_stream_context = zend_register_list_destructors_ex(file_context_dtor, NULL, "stream-context",
				/*Module::moduleCounter*/0);

			zend_hash_init_ex(&module_registry, 50, NULL, /*ZEND_MODULE_DTOR*/NULL, 1, 0);

			ts_allocate_id(&compiler_globals_id, sizeof(zend_compiler_globals), (ts_allocate_ctor)compiler_globals_ctor, NULL);
			ts_allocate_id(&executor_globals_id, sizeof(zend_executor_globals), (ts_allocate_ctor)executor_globals_ctor, 
				(ts_allocate_dtor)executor_globals_dtor);
			ts_allocate_id(&language_scanner_globals_id, sizeof(zend_scanner_globals),
				(ts_allocate_ctor)scanner_globals_ctor, NULL);
			ts_allocate_id(&ini_scanner_globals_id, sizeof(zend_scanner_globals), (ts_allocate_ctor)scanner_globals_ctor, NULL);
			ts_allocate_id(&core_globals_id, sizeof(php_core_globals), (ts_allocate_ctor)core_globals_ctor,
				(ts_allocate_dtor)core_globals_dtor);
			ts_allocate_id(&sapi_globals_id, sizeof(sapi_globals_struct), (ts_allocate_ctor)sapi_globals_ctor, NULL);
			ts_allocate_id(&file_globals_id, sizeof(php_file_globals), (ts_allocate_ctor)file_globals_ctor,
				(ts_allocate_dtor)file_globals_dtor);
			ts_allocate_id(&lcg_globals_id, sizeof(php_lcg_globals), (ts_allocate_ctor)lcg_init_globals, NULL);
			ts_allocate_id(&output_globals_id, sizeof(php_output_globals), (ts_allocate_ctor)php_output_init_globals, NULL);
			ts_allocate_id(&php_win32_core_globals_id, sizeof(php_win32_core_globals), (ts_allocate_ctor)php_win32_core_globals_ctor,
				NULL);

			tsrm_set_new_thread_end_handler(zend_new_thread_end_handler);

			virtual_cwd_startup();
			php_init_stream_wrappers(0, (void ***)ts_resource(0));

			register_standard_class((void ***)ts_resource(0));
			localeconv_init();
//			zend_stdClass_ptr = (zend_class_entry *)calloc(1, sizeof(zend_class_entry));

			InternalStreamWrappers::LoadModule();

#ifdef SAPI

			static sapi_module_struct cli_sapi_module = {
				"cli",							/* name */
				"Command Line Interface",    	/* pretty name */

				php_cli_startup,				/* startup */
				php_module_shutdown_wrapper,	/* shutdown */

				NULL,							/* activate */
				sapi_cli_deactivate,			/* deactivate */

				sapi_cli_ub_write,		    	/* unbuffered write */
				sapi_cli_flush,				    /* flush */
				NULL,							/* get uid */
				NULL,							/* getenv */

				php_error,						/* error handler */

				NULL,							/* header handler */
				sapi_cli_send_headers,			/* send headers handler */
				sapi_cli_send_header,			/* send header handler */

				NULL,				            /* read POST data */
				sapi_cli_read_cookies,          /* read Cookies */

				sapi_cli_register_variables,	/* register server variables */
				sapi_cli_log_message,			/* Log message */
				NULL,							/* Get request time */

				STANDARD_SAPI_MODULE_PROPERTIES
			};

			sapi_startup(&cli_sapi_module);

			if (php_module_startup(&cli_sapi_module, NULL, 0) == FAILURE)
			{
				throw gcnew ExtensionException(ExtResources::GetString("could_not_initialize_phpts", 5));
			}

#endif
		}

		// Throws a <c>PhpException</c> in an appropriate way so that it propagates back to Core.
		void RemoteDispatcher::ThrowException(PhpError errType, String ^message)
		{
			if (Module::ModuleBoundContext != nullptr)
			{
#ifdef DEBUG
				Debug::WriteLine("EXT SUP", String::Format("managed_zend_error in module context \"{0}\"", message));
#endif
				
				String ^fmsg = String::Format("{0}: {1}",
					Module::ModuleBoundContext->GetFileName(), message);

				StartupHelper::StartupErrors->Add(fmsg);

				if (errType == PhpError::Error) throw gcnew InvalidOperationException(fmsg);
				Console::WriteLine(fmsg);		
			}
			else
			{
//				RequestCookie ^cookie = Request::GetCurrentRequest()->GetCookie();
//
//				if (cookie != nullptr)
//				{
//					// extensions are isolated
//#ifdef DEBUG
//					Debug::WriteLine("EXT SUP", "managed_zend_error: about to call back RequestCookie::ExceptionCallback");
//#endif
//
//					// call core to handle the error (output the message etc.)
//					cookie->ExceptionCallback(errType, message);
//				}
//				else
				{
					// extensions are collocated
#ifdef DEBUG
					Debug::WriteLine("EXT SUP", "managed_zend_error: about to call back PhpException::Throw");
#endif

					// call core to handle the error (output the message etc.)
					PHP::Core::PhpException::Throw(errType, message);
				}
			}
		}

		// Like zend_ini_string but returns NULL when the entry could not be found.
		char *RemoteDispatcher::zend_ini_string_fixed(String ^name, bool originalValue)
		{
			zend_ini_entry *ini_entry;
			TSRMLS_FETCH();

			char *ntv_name = PhpMarshaler::MarshalManagedStringToNativeString(name);

			try
			{
				if (zend_hash_find(EG(ini_directives), ntv_name, name->Length + 1, (void **)&ini_entry) == SUCCESS)
				{
					Request::EnsureModuleStarted(Module::GetModuleByModuleNumber(ini_entry->module_number));
					if (originalValue && ini_entry->modified) return ini_entry->orig_value;
					else return ini_entry->value;
				}

				return NULL;
			}
			finally
			{
				efree(ntv_name);
			}
		}
	}
}
