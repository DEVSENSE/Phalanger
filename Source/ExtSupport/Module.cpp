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
// Module.cpp
// - contains definition of Constant class
// - contains definition of Function class
// - contains definition of Method class
// - contains definition of CallerMethod class
// - contains definition of ConstructorMethod class
// - contains definition of GetterMethod class
// - contains definition of SetterMethod class
// - contains definition of Class class
// - contains definition of Module class
//

#include "stdafx.h"
#include <vector>

#include "Module.h"
#include "PhpMarshaler.h"
#include "Request.h"
#include "TsrmLs.h"
#include "Objects.h"
#include "Misc.h"
#include "WrapperGen.h"
#include "Variables.h"
#include "AssemblyInternals.h"
#include "Errors.h"
#include "IniConfig.h"
#include "VirtualWorkingDir.h"
#include "RemoteDispatcher.h"
#include "Zend.h"

#undef GetClassName

using namespace System;
using namespace System::Reflection;
using namespace System::Collections;

using namespace PHP::Core;

#ifdef DEBUG
#define Debug_Unindent() Debug::Unindent()
#define Debug_Indent() Debug::Indent()
#define Debug_WriteLine(info, msg) Debug::WriteLine(info, msg)
#define Debug_WriteLine_Conditional(cond, info, msg) if (cond) Debug::WriteLine(info, msg)
#else
#define Debug_Unindent()
#define Debug_Indent()
#define Debug_WriteLine(info, msg)
#define Debug_WriteLine_Conditional(cond, info, msg)
#endif

#pragma unmanaged

#ifdef PHP4TS

#define CALL_HANDLER(class_entry) (class_entry)->handle_function_call
#define GET_HANDLER(class_entry)  (class_entry)->handle_property_get
#define SET_HANDLER(class_entry)  (class_entry)->handle_property_set

#endif

/// <summary>
/// Global unique ID counter.
/// </summary>
/// <remarks>
/// Incremented every time an extension is loaded. The value of this field is written into its 
/// <c>moduleEntry-&gt;module_number</c>.
/// </remarks>
static int moduleCounter = 0;

/// <summary>
/// Global array of tables of class and constant entries registered by particular extensions.
/// Indexed by <c>module_number</c>.
/// <summary>
static std::vector<unmng_class_and_constant_info> unmng_module_entries;

/// <summary>
/// A mutex protecting <see cref="unmng_module_entries"/> and <see cref="unmng_module_entries_count"/>.
/// </summary>
MUTEX_T mx_module_entries;

#pragma managed

// Checks whether unmng_class_and_constant_info needs to be enlarged, and if so, reallocates it.
static void ummng_module_entries_enlarge(int module_number)
{
	static unmng_class_and_constant_info zero_entry = { NULL, NULL };

	tsrm_mutex_lock(mx_module_entries);
	try
	{
		if (module_number >= (int)unmng_module_entries.size())
		{
			int new_count = (module_number + 10) * 2;
			unmng_module_entries.resize(new_count, zero_entry);
		}
	}
	finally
	{
		tsrm_mutex_unlock(mx_module_entries);
	}
}

// Performs unmanaged class registration (necessary for reregistration in another AppDomain).
void unmng_register_class(int module_number, zend_class_entry *ce, zend_class_entry *parent_ce)
{
	tsrm_mutex_lock(mx_module_entries);
	try
	{
		ummng_module_entries_enlarge(module_number);

		unmng_class_and_constant_info *entry = &unmng_module_entries[module_number];
		if (entry->class_entries == NULL)
		{
			entry->class_entries = (HashTable *)malloc(sizeof(HashTable));
			zend_hash_init_ex(entry->class_entries, 20, NULL, NULL, 1, 0);
		}

		// make a copy of ce (OCI8 passes stack-allocated ce!)
		zend_class_entry *new_ce = (zend_class_entry *)malloc(sizeof(zend_class_entry));
		*new_ce = *ce;
		new_ce->name = _strdup(ce->name);

		// update parent_ce to point to the copy that has been made before
		unmng_class_entry *ocl_entry;
		zend_hash_internal_pointer_reset(entry->class_entries);
		while (zend_hash_get_current_data(entry->class_entries, (void **)&ocl_entry) == SUCCESS)
		{
			if (ocl_entry->original_entry == parent_ce)
			{
				parent_ce = ocl_entry->entry;
				break;
			}
			zend_hash_move_forward(entry->class_entries);
		}

		unmng_class_entry cl_entry = { ce, new_ce, parent_ce };
		zend_hash_next_index_insert(entry->class_entries, &cl_entry, sizeof(cl_entry), NULL);
	}
	finally
	{
		tsrm_mutex_unlock(mx_module_entries);
	}
}

// Performs unmanaged constant registration (necessary for reregistration in another AppDomain).
void unmng_register_constant(zend_constant *c)
{
	ummng_module_entries_enlarge(c->module_number);

	unmng_class_and_constant_info *entry = &unmng_module_entries[c->module_number];
	if (entry->constant_entries == NULL)
	{
		entry->constant_entries = (HashTable *)malloc(sizeof(HashTable));
		zend_hash_init_ex(entry->constant_entries, 20, NULL, NULL, 1, 0);
	}

	zend_hash_next_index_insert(entry->constant_entries, c, sizeof(zend_constant), NULL);
}

#ifdef _DEBUG
// Catches SEH exceptions and prints out EIP information.
static int _wrapper_filter(LPEXCEPTION_POINTERS pep)
{
	Debug_WriteLine("EXT SUP", "SEH EXCEPTION CAUGHT!");
	Debug_WriteLine("EXT SUP", String::Format("exception EIP = {0:x}", pep->ContextRecord->Eip));

	return EXCEPTION_EXECUTE_HANDLER;
}


// Wraps init calls.
static int _init_handler_wrapper(int (*handler)(INIT_FUNC_ARGS), INIT_FUNC_ARGS)
{
	__try
	{
		return handler(type, module_number, tsrm_ls);
	}
	__except (_wrapper_filter(GetExceptionInformation()))
	{
		return 0;
	}
}
#endif

// Wraps function calls.
static inline void _function_handler_wrapper(void (*handler)(INTERNAL_FUNCTION_PARAMETERS), INTERNAL_FUNCTION_PARAMETERS)
{
#ifdef DEBUG
	__try
	{
		handler(INTERNAL_FUNCTION_PARAM_PASSTHRU);
	}
	__except (_wrapper_filter(GetExceptionInformation()))
	{ }
#else
	handler(INTERNAL_FUNCTION_PARAM_PASSTHRU);
#endif
}

#ifdef PHP4TS
// Wraps function calls with a 'property reference'.
static inline void _function_handler_wrapper(void (*handler)(INTERNAL_FUNCTION_PARAMETERS, zend_property_reference *),
									  INTERNAL_FUNCTION_PARAMETERS, zend_property_reference *property_reference)
{
#ifdef DEBUG
	__try
	{
		handler(INTERNAL_FUNCTION_PARAM_PASSTHRU, property_reference);
	}
	__except (_wrapper_filter(GetExceptionInformation()))
	{ }
#else
	handler(INTERNAL_FUNCTION_PARAM_PASSTHRU, property_reference);
#endif
}
#endif

namespace PHP
{
	namespace ExtManager
	{
		// Constant implementation:

		// Adds this constant to a <see cref="IDictionary"/>.
		bool Constant::AddToDictionary(IDictionary ^dictionary, System::String ^name)
		{
			if (caseInsensitive) name = name->ToLower();
			try
			{
				dictionary->Add(name, this);
			}
			catch (ArgumentException ^)
			{
				return false;
			}
			return true;
		}

		// Retrievs a constant from a <see cref="IDictionary"/>.
		Constant ^Constant::RetrieveFromDictionary(IDictionary ^dictionary, System::String ^name)
		{
			Object ^obj = dictionary[name];

			Constant ^con = static_cast<Constant ^>(dictionary[name]);
			if (con != nullptr) return con;

			con = static_cast<Constant ^>(dictionary[name->ToLower()]);
			if (con != nullptr && con->caseInsensitive) return con;
			return nullptr;
		}

		// Registers this constant in the appropriate context (module or request).
		bool Constant::Register(bool persistent)
		{
			String ^name = GetName();
			Debug_WriteLine("EXT SUP", String::Concat("Registering ", 
				gcnew String(persistent ? "persistent" : "transient"), " constant: ", name));

			if (persistent)
			{
				if (Module::ModuleBoundContext != nullptr)
				{
					return Module::ModuleBoundContext->AddConstant(name, this);
				}

				// We are not within a module_startup - kind of strange situation - should we
				// allow the constant to be registered as persistent? No, not for now.
				// That would mean that managed wrappers generated within different request
				// might have different constants.
			}

			// Let's register a non-persistent constant. Note that if Module::ModuleBoundContext
			// is not NULL here, an exception will be thrown.
			Request ^request = Request::GetCurrentRequest();
			return AddToDictionary(request->TransientConstants, name);
		}

		/// <summary>
		/// Looks up a constant based on its name.
		/// </summary>
		/// <param name="name">The name of the constant.</param>
		/// <returns>The constant or <B>null</B> if not found.</returns>
		Constant ^Constant::Lookup(String ^name)
		{
			// check this module's constants first
			Constant ^constant = Module::GetCurrentModule()->GetConstantByName(name);

			if (constant == nullptr)
			{
				// now this request's constants
				Request ^request = Request::GetCurrentRequest(false, false);
				if (request != nullptr)
				{
					constant = Constant::RetrieveFromDictionary(request->TransientConstants, name);
				}
			}

			return constant;
		}
		
		Object^ Function::Invoke(PhpObject^ self, array<Object ^> ^%args, array<int> ^refInfo, String ^workingDir)
		{
			Request^ request = Request::EnsureRequestExists();

			// update CWD if necessary
			if (request->currentWorkingDirectory != workingDir)
			{
				UpdateVirtualWorkingDir(workingDir);
				request->currentWorkingDirectory = workingDir;
			}

			// request startup the module
			if (containingModule->IsEarlyInit() == false &&
				request->StartedModules->ContainsKey(containingModule->GetFileName()) == false)
			{
				// no function from the containing module has been called on behalf
				// of the current request
				containingModule->RequestStartup(request);
				request->StartedModules->Add(containingModule->GetFileName(), containingModule);
			}

			// invoke the function
			Object^ ret = Invoke(request, true, self, args, refInfo, NULL);

#ifdef DEBUG
			Debug::WriteLine("EXT SUP", String::Concat("# return type == ", ret == nullptr ? "NULL" : ret->GetType()->Name));
#endif

			if (refInfo == nullptr)
				args = nullptr;	// do not pass arguments back through the remote channel, they did not change

			return ret;
		}

		// Function implementation:

		// Invokes this function with given parameters.
		Object ^Function::Invoke(Request ^request, bool retValueUsed, PhpObject ^self, array<Object ^> ^args, array<int> ^refInfo, zend_property_reference *propertyReference)
		{
			//
			int num_args = (args == nullptr ? 0 : args->Length);
			
			// get marshaler instance
			PhpMarshaler ^marshaler = PhpMarshaler::GetInstance(containingModule);

			// create placeholder for return value
			IntPtr ret_val = marshaler->MarshalManagedToNative(nullptr);
			zval * ret_new_val = NULL;
			Object ^ret = nullptr;

			// marshal $this if non-NULL
			IntPtr self_native;
			if (self != nullptr) self_native = marshaler->MarshalManagedToNative(self);
			else self_native = IntPtr::Zero;

			Request::InvocationContext old_invocation_context;
			try
			{
				// remember the previous invocation context
				old_invocation_context = request->CurrentInvocationContext;

				if (ValidateParameters(request, num_args, refInfo) == false) return nullptr;

				// save arguments and function name into current context
				request->CurrentInvocationContext.FunctionPhpArgs = (zval **)ecalloc(num_args, sizeof(zval *));
				request->CurrentInvocationContext.FunctionArgCount = num_args;

				request->CurrentInvocationContext.FunctionName = (functionEntry == NULL ? "" : functionEntry->fname);
				request->CurrentInvocationContext.FunctionArgs = args;
				request->CurrentInvocationContext.Module = containingModule;
				
				request->CurrentInvocationContext.FunctionType = ZEND_INTERNAL_FUNCTION;

				try
				{
					TSRMLS_FETCH();

					//Phalanger TODO: set the class scope of the caller
					//EG(scope) = self

					// marshal arguments
					for (int i = 0; i < num_args; i++)
					{
						request->CurrentInvocationContext.FunctionPhpArgs[i] = (zval *)
							(marshaler->MarshalManagedToNative(request->CurrentInvocationContext.FunctionArgs[i]).ToPointer());

						// check for argument passed by reference
						if (request->CurrentInvocationContext.FunctionRefArray != nullptr &&
							request->CurrentInvocationContext.FunctionRefArray[i])
						{
							request->CurrentInvocationContext.FunctionPhpArgs[i]->is_ref = 1;
						}
					}

					// CALL!
					Debug_WriteLine("EXT SUP", String::Format("Function ptr = {0:x}", (unsigned)GetFunctionPtr()));

#ifdef PHP4TS
					if (propertyReference != NULL)
					{
						// used when invoking __call handler
						propertyReference->object = (zval *)self_native.ToPointer();

						_function_handler_wrapper((void (*)(INTERNAL_FUNCTION_PARAMETERS, zend_property_reference *))GetFunctionPtr(),
										num_args,                        // number of arguments
										(zval *)ret_val.ToPointer(),      // return value
										(zval *)self_native.ToPointer(),  // $this ptr
										retValueUsed,                     // return value used
										tsrm_ls,                          // TSRM ptr
										propertyReference
										); 
					}
					else
#endif
					{
						_function_handler_wrapper(GetFunctionPtr(),
										num_args,                         // number of arguments
										(zval *)ret_val.ToPointer(),      // return value
#if defined(PHP5TS)
										&ret_new_val,                             // return value points to new value
#endif
										(zval *)self_native.ToPointer(),  // $this ptr
										retValueUsed,                     // return value used
										tsrm_ls                         // TSRM ptr
										);
					}

					// unmarshal arguments that were passed by reference
					if (request->CurrentInvocationContext.FunctionRefArray != nullptr)
					{
						for (int i = 0; i < num_args; i++)
						{
							if (request->CurrentInvocationContext.FunctionRefArray[i] == true && 
								request->CurrentInvocationContext.FunctionPhpArgs[i] != NULL)
							{
								args[i] = marshaler->MarshalNativeToManaged(IntPtr(request->CurrentInvocationContext.FunctionPhpArgs[i]));
							}
						}
					}

					// unmarshal $this
					if (self_native != IntPtr::Zero) marshaler->IncarnateNativeToManaged(self_native, self);

					// unmarshal return value
					if ( ret_new_val != NULL )
						ret = marshaler->MarshalNativeToManaged(IntPtr(ret_new_val));
					else
						ret = marshaler->MarshalNativeToManaged(ret_val);

				}
				finally
				{
					// free the arguments

					// Avoid deleting resources implicitly by destroying zvals.
					request->DontDestroyResources = true;
					for (int i = 0; i < num_args; i++)
					{
						if (request->CurrentInvocationContext.FunctionPhpArgs[i] != NULL)
						{
							marshaler->CleanUpNativeData(IntPtr(request->CurrentInvocationContext.FunctionPhpArgs[i]));
							
							// do not nullify FunctionPhpArgs here!
							// (they should be still accessible via LastFunctionPhpArgs)
						}
					}
					request->DontDestroyResources = false;

					// CurrentModule
					// CurrentInvocationContext.FunctionPhpArgs
					// CurrentInvocationContext.FunctionArgCount are retained

					if (request->LastFunctionPhpArgs != NULL) efree(request->LastFunctionPhpArgs);
				
					request->LastModule = request->CurrentInvocationContext.Module;
					request->LastFunctionPhpArgs = request->CurrentInvocationContext.FunctionPhpArgs;
					request->LastFunctionArgCount = request->CurrentInvocationContext.FunctionArgCount;

					request->CurrentInvocationContext = old_invocation_context;


				}
			}
			finally
			{
				request->DontDestroyResources = true;

				// free $this
				if (self_native != IntPtr::Zero) 
				{
					marshaler->CleanUpNativeData(self_native);
				}

				// free the return value
				marshaler->CleanUpNativeData(ret_val);



				if ( ret_new_val != NULL )
					marshaler->CleanUpNativeData(IntPtr(ret_new_val));

				request->DontDestroyResources = false;

			}

			return ret;
		}

		// Construct a bool array marking parameters to be passed by reference and check whether all
		// parameters that are forced to be passed by reference actually are.
		bool Function::ValidateParameters(Request ^request, int numArgs, array<int> ^refInfo)
		{
			// construct bool array marking arguments to be passed by reference
			if (refInfo != nullptr)
			{
				request->CurrentInvocationContext.FunctionRefArray = gcnew array<System::Boolean>(numArgs);
				int num_refs = refInfo->Length;

				for (int i = 0; i < num_refs; ++i)
				{
					int ref_arg = refInfo[i];
					if (ref_arg >= 0 && ref_arg < numArgs)
					{
						// one argument marked as ref
						request->CurrentInvocationContext.FunctionRefArray[ref_arg] = true;
					}

					if (ref_arg == -1)
					{
						// all remaining arguments marked as ref
						for (int j = refInfo[(i > 0) ? (i - 1) : 0]; j < numArgs; ++j)
						{
							request->CurrentInvocationContext.FunctionRefArray[j] = true;
						}
						break;
					}
				}
			}
			else request->CurrentInvocationContext.FunctionRefArray = nullptr;

			if (functionEntry != NULL)
			{
#if defined(PHP4TS)
				// check whether all arguments that are forced to be passed by reference actually are
				unsigned char *arg_types = functionEntry->func_arg_types;
				if (arg_types != NULL)
				{
					unsigned char arg_types_len = arg_types[0];
					if (numArgs < arg_types_len) arg_types_len = numArgs;

					for (unsigned char k = 0; k < arg_types_len; k++)
					{
						if (arg_types[k + 1] == BYREF_FORCE)
						{
							// this arg is forced by ref
							if (request->CurrentInvocationContext.FunctionRefArray == nullptr ||
								request->CurrentInvocationContext.FunctionRefArray[k] == false)
							{
								RemoteDispatcher::ThrowException(PhpError::Error,
									ExtResources::GetString("parameter_must_be_byref", k + 1));
								return false;
							}
						}

						if (arg_types[k + 1] == BYREF_FORCE_REST)
						{
							// all remaining args are forced by ref
							for (unsigned char g = k; g < numArgs; g++)
							{
								if (request->CurrentInvocationContext.FunctionRefArray == nullptr ||
									request->CurrentInvocationContext.FunctionRefArray[g] == false)
								{
									RemoteDispatcher::ThrowException(PhpError::Error,
										ExtResources::GetString("parameter_must_be_byref", g + 1));
									return false;
								}
							}
							break;
						}
					}
				}
#elif defined(PHP5TS)
				// check whether all arguments that are forced to be passed by reference actually are
				_zend_arg_info *info = functionEntry->arg_info;
				if (info != NULL)
				{
					int info_len = functionEntry->num_args;
					if (numArgs < info_len) info_len = numArgs;

					for (int k = 0; k < info_len; ++k)
					{
						if (info[k + 1].pass_by_reference)
						{
							// this arg is forced by ref
							if (request->CurrentInvocationContext.FunctionRefArray == nullptr ||
								request->CurrentInvocationContext.FunctionRefArray[k] == false)
							{
								RemoteDispatcher::ThrowException(PhpError::Error,
									ExtResources::GetString("parameter_must_be_byref", k + 1));
								return false;
							}
						}
					}

					if (info[0].pass_by_reference)
					{
						// all remaining args are forced by ref
						for (int k = info_len; k < numArgs; ++k)
						{
							if (request->CurrentInvocationContext.FunctionRefArray == nullptr ||
								request->CurrentInvocationContext.FunctionRefArray[k] == false)
							{
								RemoteDispatcher::ThrowException(PhpError::Error,
									ExtResources::GetString("parameter_must_be_byref", k + 1));
								return false;
							}
						}
					}
				}
#else
				Debug::Assert(false);
#endif
			}
			return true;
		}


		// Method implementation

		// Creates a new <see cref="Method"/>.
		Method::Method(Class ^cls, zend_function_entry *func)
			: Function(cls->GetContainingModule(), func)
		{
			declaringClass = cls;
		}

#if defined(PHP5TS)
		// Allocates a <c>zend_function</c> describing this instance.
		zend_function *Method::CreateZendFunction()
		{
			zend_class_entry *class_entry = declaringClass->GetClassEntry();

			zend_function *zf = (zend_function *)malloc(sizeof(zend_function));
			zend_internal_function& func = zf->internal_function;

			func.type = ZEND_INTERNAL_FUNCTION;
			func.handler = functionEntry->handler;
			func.function_name = functionEntry->fname;
			func.scope = class_entry;
			func.prototype = NULL;
			if (functionEntry->arg_info)
			{
				func.arg_info = functionEntry->arg_info + 1;
				func.num_args = functionEntry->num_args;

				/* Currently you cannot denote that the function can accept less arguments than num_args */
				if (functionEntry->arg_info[0].required_num_args == -1) func.required_num_args = functionEntry->num_args;
				else func.required_num_args = functionEntry->arg_info[0].required_num_args;
				func.pass_rest_by_reference = functionEntry->arg_info[0].pass_by_reference;
				func.return_reference = functionEntry->arg_info[0].return_reference;
			}
			else
			{
				func.arg_info = NULL;
				func.num_args = 0;
				func.required_num_args = 0;
				func.pass_rest_by_reference = 0;
				func.return_reference = 0;
			}

			// add this zend_function to the declaring class's function table so that is is properly destroyed
			zend_hash_update(&class_entry->function_table, func.function_name, strlen(func.function_name) + 1,
				zf, sizeof(zend_function), NULL);

			return zf;
		}
#endif

		// Invokes this method with given parameters.
		Object ^Method::Invoke(Request ^request, bool retValueUsed, PhpObject ^self, array<Object ^> ^args, array<int> ^refInfo, zend_property_reference *propertyReference)
		{
			Type ^declaring_type = declaringClass->GetWrappingType();

			if (self != nullptr)
			{
				Core::Reflection::DTypeDesc ^type_desc = self->TypeDesc;
				while (1)
				{
					if (type_desc->RealType->Equals(declaring_type)) break;
					if ((type_desc = type_desc->Base) == nullptr)
					{
						throw gcnew ArgumentException(ExtResources::GetString("this_type_mismatch"), "self");
					}
				}
			}

			Object ^ret_val = Function::Invoke(request, retValueUsed, self, args, refInfo, propertyReference);

			// check for extension errors
			//if (*self == NULL)
			//{
			//	RemoteDispatcher::ThrowException(PhpError::Error, ExtResources::GetString("new_this_is_null"));
			//	return NULL;
			//}

			if (self != nullptr)
			{
				Core::Reflection::DTypeDesc ^type_desc = self->TypeDesc;
				while (1)
				{
					if (type_desc->RealType->Equals(declaring_type)) break;
					if ((type_desc = type_desc->Base) == nullptr)
					{
						RemoteDispatcher::ThrowException(PhpError::Error, ExtResources::GetString("new_this_type_mismatch"));
						return nullptr;
					}
				}
			}

			return ret_val;
		}

		// CallerMethod implementation
#ifdef PHP4TS
		// Invokes the handler.
		Object ^CallerMethod::InvokeHandler(Request ^request, bool retValueUsed, PHP::Core::PhpObject ^self, array<Object ^> ^args,
			array<int> ^refInfo, String ^methodName)
		{
			// extract method name
			zval *method_name = (zval *)PhpMarshaler::GetInstance(nullptr)->
				MarshalManagedToNative(methodName->ToLower()).ToPointer();

			// prepare property_reference
			zend_property_reference property_reference = { 0, NULL, (zend_llist *)emalloc(sizeof(zend_llist)) };
			zend_llist_init(property_reference.elements_list, sizeof(zend_overloaded_element), NULL, 0);


			zend_overloaded_element *element = (zend_overloaded_element *)emalloc(sizeof(zend_overloaded_element));
			element->type = OE_IS_METHOD; //OE_IS_OBJECT;
			element->element = *method_name;

			zend_llist_add_element(property_reference.elements_list, element);

			try
			{
				return Method::Invoke(request, retValueUsed, self, args, refInfo, &property_reference);
			}
			finally
			{
				efree(method_name); // the zval is relased by callee
				efree(element);
				zend_llist_destroy(property_reference.elements_list);
			}
		}
#elif defined(PHP5TS)
		// Invokes the handler.
		Object ^CallerMethod::InvokeHandler(Request ^request, bool retValueUsed, PHP::Core::PhpObject ^self, array<Object ^> ^args,
			array<int> ^refInfo, String ^methodName)
		{
			return Method::Invoke(request, retValueUsed, self, args, refInfo, NULL);
		}
#endif
#if defined(PHP4TS) || defined(PHP5TS)
		//CallerMethod implementation

		// Invokes the handler as a <c>__call</c> method - name of the method to invoke is expected in argument 0.
		Object ^CallerMethod::Invoke(Request ^request, bool retValueUsed, PHP::Core::PhpObject ^self, array<Object ^> ^args,
			array<int> ^refInfo, zend_property_reference *propertyReference)
		{
			if (args->Length != 2) return nullptr;

			// extract method name from the args array
			String ^method_name = dynamic_cast<String ^>(args[0]);
			if (method_name == nullptr) return nullptr;

			// extract parameters array from the args array
			PhpArray ^params = dynamic_cast<PhpArray ^>(args[1]);
			if (params == nullptr) return nullptr;

			// dereference references and build ref_info array
			array<Object ^> ^new_args = gcnew array<Object ^>(params->IntegerCount);
			ArrayList ^refs = gcnew ArrayList();
			for (int i = 0; i < params->IntegerCount; i++)
			{
				Object ^obj = params[i];
				PhpReference ^ref = dynamic_cast<PhpReference ^>(obj);
				if (ref != nullptr)
				{
					refs->Add(i);
					obj = ref->value;
				}
				new_args[i] = obj;
			}

			// invoke the handler
			array<int> ^ref_info = static_cast<array<int> ^>(refs->ToArray(int::typeid));
			Object ^ret_val = InvokeHandler(request, retValueUsed, self, new_args, ref_info, 
				PHP::Core::Convert::ObjectToString(args[0]));

			// put arguments back to the original array
			for (int i = 0; i < refs->Count; i++)
			{
				int idx = ref_info[i];
				params[idx] = new_args[idx];
			}

			return ret_val;
		}
#endif

#ifdef PHP4TS

		// ConstructorMethod implementation

		// Returns the name of this function.
		String ^ConstructorMethod::GetFunctionName()
		{
			return GetDeclaringClass()->GetClassName();
		}

		// Invokes the associated <see cref="CallerMethod"/> with given parameters.
		Object ^ConstructorMethod::Invoke(Request ^request, bool retValueUsed, PHP::Core::PhpObject ^self, array<Object ^> ^args,
			array<int> ^refInfo, zend_property_reference *propertyReference)
		{
			return callerMethod->InvokeHandler(request, retValueUsed, self, args, refInfo, GetFunctionName());
		}


		// GetterMethod implementation

		// Invokes the handler as a <c>__get</c> method - name of the field to retrieve is expected in argument 0.
		Object ^GetterMethod::Invoke(Request ^request, bool retValueUsed, PHP::Core::PhpObject ^self, array<Object ^> ^args,
			array<int> ^refInfo, zend_property_reference *propertyReference)
		{
			if (args == nullptr || args->Length == 0) return nullptr;
			PhpMarshaler ^marshaler = PhpMarshaler::GetInstance(GetContainingModule());
			//Request ^request = Request::GetCurrentRequest();

			zval *property_name = NULL;

			zval ret_val;
			INIT_ZVAL(ret_val);
			Object ^ret = nullptr;

			// marshal $this if non-NULL
			IntPtr self_native;
			if (self != nullptr) self_native = marshaler->MarshalManagedToNative(self);
			else self_native = IntPtr::Zero;

			try
			{
				// prepare property_reference
				zend_property_reference property_reference = { 0, (zval *)self_native.ToPointer(),
					(zend_llist *)emalloc(sizeof(zend_llist)) };
				zend_llist_init(property_reference.elements_list, sizeof(zend_overloaded_element), NULL, 0);

				zend_overloaded_element *element = (zend_overloaded_element *)emalloc(sizeof(zend_overloaded_element));
				element->type = OE_IS_OBJECT;

				// extract property name
				property_name = (zval *)marshaler->MarshalManagedToNative(args[0]).ToPointer();
				element->element = *property_name;
				zend_llist_add_element(property_reference.elements_list, element);

				// GTK asks for handle_property_set being non-NULL
				// (we do not do this kind of "inheritance" - see zend_do_inheritance)
				Z_OBJCE_P(property_reference.object)->handle_property_get = handler;

				try
				{
					ret_val = handler(&property_reference);
				}
				finally
				{
					efree(element);
					zend_llist_destroy(property_reference.elements_list);
				}

				// unmarshal $this
				if (self_native != IntPtr::Zero) marshaler->IncarnateNativeToManaged(self_native, self);

				// unmarshal return value
				ret = marshaler->MarshalNativeToManaged(IntPtr(&ret_val));
			}
			finally
			{
				// free property name
				if (property_name != NULL) efree(property_name); // the zval is relased by callee

				// we are freeing our temp native peers - do a careful clean up
				request->DontDestroyResources = true;
				try
				{
					// free $this
					if (self_native != IntPtr::Zero) marshaler->CleanUpNativeData(self_native);

					// free the return value
					zval_dtor(&ret_val);
				}
				finally
				{
					request->DontDestroyResources = false;
				}
			}

			return ret;
		}


		// SetterMethod implementation

		// Invokes the handler as a <c>__set</c> method - name of the field to set or an array of RuntimeChainElements
		// is expected in argument 0, the new field value is expected in argument 1.
		Object ^SetterMethod::Invoke(Request ^request, bool retValueUsed, PHP::Core::PhpObject ^self, array<Object ^> ^args,
			array<int> ^refInfo, zend_property_reference *propertyReference)
		{
			if (args == nullptr || args->Length < 2) return nullptr;
			PhpMarshaler ^marshaler = PhpMarshaler::GetInstance(GetContainingModule());
			//Request ^request = Request::GetCurrentRequest();

			int ret_val = FAILURE;
			zval *property_name = NULL;
			zval *property_value = NULL;

			// marshal $this if non-NULL
			IntPtr self_native;
			if (self != nullptr) self_native = marshaler->MarshalManagedToNative(self);
			else self_native = IntPtr::Zero;

			try
			{
				// extract property value
				property_value = (zval *)marshaler->MarshalManagedToNative(args[1]).ToPointer();

				// prepare property_reference
				zend_property_reference property_reference = { 0, (zval *)self_native.ToPointer(),
					(zend_llist *)emalloc(sizeof(zend_llist)) };
				zend_llist_init(property_reference.elements_list, sizeof(zend_overloaded_element), NULL, 0);

				RuntimeChainElement ^rce = dynamic_cast<RuntimeChainElement ^>(args[0]);
				if (rce != nullptr)
				{
					// we have a chain of field/item names
					do
					{
						zend_overloaded_element element;
						
						if (istypeof<RuntimeChainProperty>(rce)) element.type = OE_IS_OBJECT;
						else if (istypeof<RuntimeChainItem>(rce)) element.type = OE_IS_ARRAY;
						else Debug::Assert(false, "Invalid setter chain element type.");

						element.element = *(zval *)marshaler->MarshalManagedToNative(rce->Name).ToPointer();
						zend_llist_add_element(property_reference.elements_list, &element);

						rce = rce->Next;
					}
					while (rce != nullptr);
				}
				else
				{
					zend_overloaded_element element;
					
					element.type = OE_IS_OBJECT;
					element.element = *(zval *)marshaler->MarshalManagedToNative(args[0]).ToPointer(),
					zend_llist_add_element(property_reference.elements_list, &element);
				}

				// GTK asks for handle_property_set being non-NULL
				// (we do not do this kind of "inheritance" - see zend_do_inheritance)
				Z_OBJCE_P(property_reference.object)->handle_property_set = handler;

				try
				{
					ret_val = handler(&property_reference, property_value);
				}
				finally
				{
					zend_llist_destroy(property_reference.elements_list);
				}

				if (ret_val == SUCCESS)
				{
					// unmarshal $this
					if (self_native != IntPtr::Zero) marshaler->IncarnateNativeToManaged(self_native, self);
				}
			}
			finally
			{
				if (property_name != NULL) efree(property_name); // the zval is relased by callee

				// we are freeing our temp native peers - do a careful clean up
				request->DontDestroyResources = true;
				try
				{
					// free $this
					if (self_native != IntPtr::Zero) marshaler->CleanUpNativeData(self_native);

					// free property value and name
					if (property_value != NULL) marshaler->CleanUpNativeData(IntPtr(property_value));
				}
				finally
				{
					request->DontDestroyResources = false;
				}
			}

			return nullptr;
		}
#endif

		//
		// Class implementation
		//

		// wrapper for PHP::Core::Name::Value to suppress warning message when accessing read-only predefined Names.
		String^ Value(PHP::Core::Name name){return name.Value;}

		// Creates a new <see cref="Class"/>.
		Class::Class(Module ^mod, zend_class_entry *entry, zend_class_entry *parentEntry)
		{
			isOriginalClassEntryValid = false;
			originalClassEntry = entry;
			containingModule = mod;

			// make a copy of the supplied class entry
			classEntry = (zend_class_entry *)malloc(sizeof(zend_class_entry));
			*classEntry = *originalClassEntry;
			classEntry->name = _strdup(entry->name);

			classEntry->type = ZEND_INTERNAL_CLASS;
			classEntry->parent = parentEntry;
#if defined(PHP4TS)
			classEntry->refcount = (int *)malloc(sizeof(int));
			*classEntry->refcount = 1;
			classEntry->constants_updated = 0;
			zend_hash_init(&classEntry->default_properties, 0, NULL, ZVAL_PTR_DTOR, 1);
			zend_hash_init(&classEntry->function_table, 0, NULL, ZEND_FUNCTION_DTOR, 1);
#elif defined(PHP5TS)
			TSRMLS_FETCH();
			zend_initialize_class_data(classEntry, 0 TSRMLS_CC);
#endif

			// enumerate methods
			methods = gcnew Hashtable();
			zend_function_entry *func = classEntry->builtin_functions;

			Debug_Indent();
			try
			{
				if (func)
				{
					while (func->fname)
					{
						Debug_WriteLine("EXT SUP", System::String::Concat("Registering method: ", gcnew String(func->fname)));

						// some extensions (GTK) register one method more than once (?)
						Method ^method = gcnew Method(this, func);
						methods[(gcnew String(func->fname))->ToLower()] = method;

#if defined(PHP5TS)
						if (::_stricmp(func->fname, ZEND_CONSTRUCTOR_FUNC_NAME) == 0)     classEntry->constructor = method->CreateZendFunction();
						else if (::_stricmp(func->fname, ZEND_DESTRUCTOR_FUNC_NAME) == 0) classEntry->destructor = method->CreateZendFunction();
						else if (::_stricmp(func->fname, ZEND_CLONE_FUNC_NAME) == 0)    classEntry->clone = method->CreateZendFunction();
						else if (::_stricmp(func->fname, ZEND_GET_FUNC_NAME) == 0)      classEntry->__get = method->CreateZendFunction();
						else if (::_stricmp(func->fname, ZEND_SET_FUNC_NAME) == 0)      classEntry->__set = method->CreateZendFunction();
						else if (::_stricmp(func->fname, ZEND_UNSET_FUNC_NAME) == 0)    classEntry->__unset = method->CreateZendFunction();
						else if (::_stricmp(func->fname, ZEND_ISSET_FUNC_NAME) == 0)    classEntry->__isset = method->CreateZendFunction();
						else if (::_stricmp(func->fname, ZEND_CALL_FUNC_NAME) == 0)     classEntry->__call = method->CreateZendFunction();
#endif

						func++;
					}
				}

#if defined(PHP4TS)
#pragma warning (push)
#pragma warning (disable: 4395)
				if (CALL_HANDLER(classEntry) != NULL)
				{
					// create an artificial __call method
					CallerMethod ^caller_method = gcnew CallerMethod(this, CALL_HANDLER(classEntry));
					methods->Add(PHP::Core::PhpObject::SpecialMethodNames::Call.Value, caller_method);

					if (!methods->ContainsKey(PHP::Core::PhpObject::SpecialMethodNames::Construct.Value) &&
						!methods->ContainsKey(GetClassName()->ToLower()))
					{
						// there is no explicit constructor -> create an artificial constructor that calls __call
						methods->Add(GetClassName()->ToLower(), gcnew ConstructorMethod(caller_method));
					}
				}

				if (GET_HANDLER(classEntry) != NULL)
				{
					// create an artificial __get method
					GetterMethod ^getter_method = gcnew GetterMethod(this, GET_HANDLER(classEntry));
					methods->Add(PHP::Core::PhpObject::SpecialMethodNames::Get.Value, getter_method);
				}

				if (SET_HANDLER(classEntry) != NULL)
				{
					// create an artificial __set method
					SetterMethod ^setter_method = gcnew SetterMethod(this, SET_HANDLER(classEntry));
					methods->Add(PHP::Core::PhpObject::SpecialMethodNames::Set.Value, setter_method);
				}
#pragma warning (pop)
#elif defined (PHP5TS)

				_zend_function* func;

				if (((func = classEntry->__call) != NULL) && (func->type == ZEND_INTERNAL_FUNCTION))
				{
					// create an artificial __call method
					CallerMethod ^caller_method = gcnew CallerMethod(this, func->internal_function.handler);
					methods->Add(caller_method->GetFunctionName(), caller_method);
				}

				if (((func = classEntry->__get) != NULL) && (func->type == ZEND_INTERNAL_FUNCTION))
				{
					// create an artificial __get method
					SpecialMethod ^method = gcnew SpecialMethod(Value(PhpObject::SpecialMethodNames::Get), this, func->internal_function.handler);
					methods->Add(method->GetFunctionName(), method);
				}

				if (((func = classEntry->__set) != NULL) && (func->type == ZEND_INTERNAL_FUNCTION))
				{
					// create an artificial __set method
					SpecialMethod ^method = gcnew SpecialMethod(Value(PhpObject::SpecialMethodNames::Set), this, func->internal_function.handler);
					methods->Add(method->GetFunctionName(), method);
				}

				if (((func = classEntry->constructor) != NULL) && (func->type == ZEND_INTERNAL_FUNCTION) &&
					!methods->ContainsKey(Value(PhpObject::SpecialMethodNames::Construct)) &&
					!methods->ContainsKey(GetClassName()->ToLower()))
				{
					// create an artificial construct method
					SpecialMethod ^method = gcnew SpecialMethod(Value(PhpObject::SpecialMethodNames::Construct), this, func->internal_function.handler);
					methods->Add(method->GetFunctionName(), method);
				}

				if (((func = classEntry->destructor) != NULL) && (func->type == ZEND_INTERNAL_FUNCTION) && 
					!methods->ContainsKey(Value(PhpObject::SpecialMethodNames::Destruct)))
				{
					// create an artificial destruct method
					SpecialMethod ^method = gcnew SpecialMethod(Value(PhpObject::SpecialMethodNames::Destruct), this, func->internal_function.handler);
					methods->Add(method->GetFunctionName(), method);
				}

				if (((func = classEntry->clone) != NULL) && (func->type == ZEND_INTERNAL_FUNCTION))
				{
					// create an artificial destruct method
					SpecialMethod ^method = gcnew SpecialMethod(Value(PhpObject::SpecialMethodNames::Clone), this, func->internal_function.handler);
					methods->Add(method->GetFunctionName(), method);
				}

				if (((func = classEntry->__unset) != NULL) && (func->type == ZEND_INTERNAL_FUNCTION))
				{
					// create an artificial destruct method
					SpecialMethod ^method = gcnew SpecialMethod(Value(PhpObject::SpecialMethodNames::Unset), this, func->internal_function.handler);
					methods->Add(method->GetFunctionName(), method);
				}

				if (((func = classEntry->__isset) != NULL) && (func->type == ZEND_INTERNAL_FUNCTION))
				{
					// create an artificial destruct method
					SpecialMethod ^method = gcnew SpecialMethod(Value(PhpObject::SpecialMethodNames::Isset), this, func->internal_function.handler);
					methods->Add(method->GetFunctionName(), method);
				}

				if (((func = classEntry->__tostring) != NULL) && (func->type == ZEND_INTERNAL_FUNCTION))
				{
					// create an artificial destruct method
					SpecialMethod ^method = gcnew SpecialMethod(Value(PhpObject::SpecialMethodNames::Tostring), this, func->internal_function.handler);
					methods->Add(method->GetFunctionName(), method);
				}

				//TODO: serialize_func, unserialize_func
#endif
			}
			finally
			{
				Debug_Unindent();
			}

			if (containingModule != nullptr)
			{
				Assembly ^ass = containingModule->GetWrappingAssembly();
				if (ass != nullptr)
				{
					try
					{
						wrappingType = ass->GetType(String::Concat(Namespaces::Library, ".", GetClassName()), false, true);
					}
					catch (TypeLoadException ^)
					{
						wrappingType = nullptr;
					}

					if (wrappingType != nullptr &&
						wrappingType->IsSubclassOf(PhpObject::typeid) == false) wrappingType = nullptr;
				}
			}
		}

		// Returns a <see cref="Constants"/> with a given name that was defined in this class.
		Constant ^Class::GetConstantByName(String ^name)
		{
#if defined(PHP4TS)
			return nullptr;
#elif defined(PHP5TS)
			char *ntv_name = PhpMarshaler::MarshalManagedStringToNativeString(name);

			zval **value;
			try
			{
				if (zend_hash_find(&classEntry->constants_table, ntv_name, name->Length + 1, (void **)&value) != SUCCESS)
				{
					return nullptr;
				}
			}
			finally
			{
				efree(ntv_name);
			}
			
			PhpMarshaler ^marshaler = PhpMarshaler::GetInstance(containingModule);
			Object ^mng_value = marshaler->MarshalNativeToManaged((IntPtr)*value);

			return gcnew Constant(containingModule, name, mng_value, false);
#endif
		}

		// Returns an <see cref="IDictionaryEnumerator"/> to be used for constant enumeration.
		IDictionaryEnumerator ^Class::GetConstantEnumerator()
		{
#if defined(PHP4TS)
			return (gcnew Hashtable())->GetEnumerator();
#elif defined(PHP5TS)
			Hashtable ^constants = gcnew Hashtable(classEntry->constants_table.nNumOfElements);
			PhpMarshaler ^marshaler = PhpMarshaler::GetInstance(containingModule);
			
			zval **value;

			zend_hash_internal_pointer_reset(&classEntry->constants_table);
			while (zend_hash_get_current_data(&classEntry->constants_table, (void **)&value) == SUCCESS)
			{
				Object ^mng_value = marshaler->MarshalNativeToManaged((IntPtr)*value);
				
				char *name;
				ulong index;
				if (zend_hash_get_current_key(&classEntry->constants_table, &name, &index, 0) == HASH_KEY_IS_STRING)
				{
					String ^mng_name = gcnew String(name);
					constants->Add(mng_name, gcnew Constant(containingModule, mng_name, mng_value, false));
				}

				zend_hash_move_forward(&classEntry->constants_table);
			}

			return constants->GetEnumerator();
#endif
		}


		// Module implementation:

		// Returns a <see cref="Constant"/> with given name that was defined by this extension.
		Constant ^Module::GetConstantByName(System::String ^name)
		{
			return Constant::RetrieveFromDictionary(constants, name);
		}

		// Returns a <see cref="Function"/> with given name that was defined by this extension.
		Function ^Module::GetFunctionByName(System::String ^name)
		{
			return static_cast<Function ^>(functions[name]);
		}

		// Returns a <see cref="Class"/> with given name that was defined by this extension.
		Class ^Module::GetClassByName(System::String ^name)
		{
			String ^lower_name = name->ToLower();
			
			Class ^cl = static_cast<Class ^>(classes[lower_name]);
			if (cl == nullptr)
			{
				Request ^request = Request::GetCurrentRequest(false, false);
				if (request != nullptr)
				{
					cl = static_cast<Class ^>(request->TransientClasses[lower_name]);
					//if (cl != nullptr && cl->GetContainingModule() != this) cl = nullptr;
				}
			}
			return cl;
		}

		// Adds a persistent constant to the table of constants defined by this extension.
		bool Module::AddConstant(System::String ^name, Constant ^constant)
		{
			return constant->AddToDictionary(constants, name);
		}

		// Adds a class to the table of classes defined by this extension.
		void Module::AddClass(System::String ^name, Class ^entry, bool transient)
		{
			if (transient) Request::GetCurrentRequest()->TransientClasses->Add(name->ToLower(), entry);
			else classes->Add(name->ToLower(), entry);
		}

		// Determines whether the parameter represents a method table of a Zend class registered by this extension.
		bool Module::IsNativeMethodTable(HashTable *ht)
		{
			for each (scg::KeyValuePair<String ^, Object^> entry in classes)
			{
				Class ^cl = static_cast<Class ^>(entry.Value);
				if (&cl->GetClassEntry()->function_table == ht) return true;
			}

			Request ^request = Request::GetCurrentRequest(false, false);
			if (request != nullptr)
			{
				for each (scg::KeyValuePair<String ^, Object^> entry in request->TransientClasses)
				{
					Class ^cl = static_cast<Class ^>(entry.Value);
					if (&cl->GetClassEntry()->function_table == ht) return true;
				}
			}

			return false;
		}

		// Returns an <see cref="ICollection"/> of names of functions defined by this extension.
		ICollection ^Module::GetFunctionNames()
		{
			array<String ^> ^fn  = gcnew array<String ^>(functions->Count);

			int i = 0;
			IDictionaryEnumerator ^enumerator = functions->GetEnumerator();
			while (enumerator->MoveNext())
			{
				fn[i++] = static_cast<Function ^>(enumerator->Value)->GetFunctionName();
			}
			return static_cast<Array ^>(fn);
		}

		// Returns an <see cref="ICollection"/> of names of classes defined by this extension.
		ICollection ^Module::GetClassNames()
		{
			ArrayList ^cls = gcnew ArrayList(classes->Count);

			for each (scg::KeyValuePair<String ^, Object^> entry in classes)
			{
				if (entry.Key->CompareTo("stdclass") != 0)
				{
					cls->Add(static_cast<Class ^>(entry.Value)->GetClassName());
				}
			}

			Request ^request = Request::GetCurrentRequest(false, false);
			if (request != nullptr)
			{
				for each (scg::KeyValuePair<String ^, Object^> entry in request->TransientClasses)
				{
					Class ^cl = static_cast<Class ^>(entry.Value);
					if (cl->GetContainingModule() == this) cls->Add(cl->GetClassName());
				}
			}

			return cls;
		}

		// Returns an <see cref="IniEntry"/> given the key (INI entry name).
		IniEntry ^Module::GetConfigEntry(String ^key)
		{
			// configuration is now a request-sensitive piece of data!
			Request ^request = Request::GetCurrentRequest(false, false);
			if (request == nullptr || request->ExtConfig == nullptr) return nullptr;

			ExtensionLibraryDescriptor ^descriptor;
			if (request->ExtConfig->TryGetValue(GetFileName(), descriptor) == false  || descriptor == nullptr )
				return nullptr;
			
			if (descriptor->LocalConfig == nullptr ||
				!descriptor->LocalConfig->Options->ContainsKey(key)) return nullptr;
			Object ^item = descriptor->LocalConfig->Options[key];
			
			// convert the items to IniEntries lazily
			IniEntry ^entry = dynamic_cast<IniEntry ^>(item);
			
			if (entry == nullptr)
			{
				entry = gcnew IniEntry(dynamic_cast<String ^>(item));
				descriptor->LocalConfig->Options[key] =  entry;
			}
			return entry;
		}

		// Returns reference to the <see cref="Module/> that contains currently executing function.
		Module ^Module::GetCurrentModule()
		{
			if (ModuleBoundContext != nullptr) return ModuleBoundContext;
			else return Request::GetCurrentRequest(false, true)->CurrentInvocationContext.Module;
		}


		// DynamicModule implementation:

		// Creates and loads a new <see cref="Module"/>.
		DynamicModule::DynamicModule(String ^_path, String ^_fileName, bool _earlyInit)
		{
			initialized = false;
			fileName = _fileName;
			earlyInit = _earlyInit;

			// load the module
			String ^file_path;
			try
			{
				file_path = Path::Combine(_path, String::Concat(fileName, ".dll"));
			}
			catch (Exception ^)
			{
				throw gcnew CouldNotLoadExtensionException(fileName, ExtResources::GetString("invalid_extension_path",
					fileName));
			}

			Debug_WriteLine("EXT SUP", String::Concat("Loading module: ", file_path));
			Debug_Indent();

			try
			{
				// load the dynamic library, search dependencies in the ExtNatives directory:
				hLib = LoadLibraryEx(file_path, IntPtr::Zero, LOAD_WITH_ALTERED_SEARCH_PATH);
				
				if (hLib == NULL)
				{
					System::ComponentModel::Win32Exception ^ex = gcnew System::ComponentModel::Win32Exception();
					throw gcnew CouldNotLoadExtensionException(fileName, ex->Message);
				}
				
				// get get_module
				GetModuleProto get_module = (GetModuleProto)GetProcAddress(hLib, "get_module");
				if (get_module == NULL)
				{
					FreeLibrary(hLib);
					throw gcnew CouldNotLoadExtensionException(fileName, ExtResources::GetString("get_module_not_found"));
				}

				// get module entry
				try
				{
					moduleEntry = get_module();
				}
				catch (Exception ^e)
				{
					FreeLibrary(hLib);
					throw e;
				}

				if (moduleEntry == NULL)
				{
					FreeLibrary(hLib);
					throw gcnew CouldNotLoadExtensionException(fileName, ExtResources::GetString("get_module_returned_null"));
				}

				// check version
				if (moduleEntry->zend_debug != ZEND_DEBUG || moduleEntry->zts != USING_ZTS ||
					moduleEntry->zend_api != ZEND_MODULE_API_NO)
				{	
					array<Object ^> ^args = gcnew array<Object ^>(6);
					
					args[0] = moduleEntry->zend_debug;
					args[1] = moduleEntry->zts;
					args[2] = moduleEntry->zend_api;
					args[3] = static_cast<Object ^>(ZEND_DEBUG);
					args[4] = static_cast<Object ^>(USING_ZTS);
					args[5] = static_cast<Object ^>(ZEND_MODULE_API_NO);

					FreeLibrary(hLib);
					throw gcnew CouldNotLoadExtensionException(fileName,
						ExtResources::GetString("extension_version_mismatch", args));
				}

				constants = gcnew Hashtable(10);
				classes = gcnew OrderedHashtable<String ^>();
				AddClass("stdClass", Class::StdClass, false);

				TSRMLS_FETCH();

				// call module startup
				if (Interlocked::Increment(moduleEntry->module_started) == 1)
				{
					// get next module number
					moduleEntry->module_number = Interlocked::Increment(moduleCounter);

					if (moduleEntry->module_startup_func != NULL)
					{
						// store and change current directory to ExtNatives dir so that dependencies could be found 
						// in the case the extension loads them by LoadLibrary:
						String ^old_current_dir = Environment::CurrentDirectory;
						Environment::CurrentDirectory = PHP::Core::Configuration::Application->Paths->ExtNatives;
						
						ModuleBoundContext = this;

						try
						{
							if (moduleEntry->module_startup_func(MODULE_PERSISTENT, 
								moduleEntry->module_number, tsrm_ls) != SUCCESS)
							{
								Interlocked::Decrement(moduleEntry->module_started);
								throw gcnew CouldNotLoadExtensionException(fileName,
									ExtResources::GetString("module_startup_func_failed"));
							}
						}
						catch (Exception ^e)
						{
							Debug_WriteLine("EXT SUP", e->ToString());

							Interlocked::Decrement(moduleEntry->module_started);
							FreeLibrary(hLib);
							throw e;
						}
						finally
						{
							ModuleBoundContext = nullptr;

							// restores directory:
							Environment::CurrentDirectory = old_current_dir;
						}
					}
				}
				else
				{
					// module startup has already been called before, so only reregister classes and constants
					tsrm_mutex_lock(mx_module_entries);
					try
					{
						if (moduleEntry->module_number < (int)unmng_module_entries.size())
						{
							// reregister classes
							HashTable *table = unmng_module_entries[moduleEntry->module_number].class_entries;
							if (table != NULL)
							{
								unmng_class_entry *entry;
								zend_hash_internal_pointer_reset(table);
								while (zend_hash_get_current_data(table, (void **)&entry) == SUCCESS)
								{
									AddClass(Class::GetZendClassName(entry->entry), gcnew Class(this, entry->entry,
										entry->parent), false);
									zend_hash_move_forward(table);
								}
							}

							// reregister constants
							table = unmng_module_entries[moduleEntry->module_number].constant_entries;

							if (table != NULL)
							{
								PhpMarshaler ^marshaler = PhpMarshaler::GetInstance(nullptr);

								zend_constant *cnst;
								zend_hash_internal_pointer_reset(table);
								while (zend_hash_get_current_data(table, (void **)&cnst) == SUCCESS)
								{
									Object ^value = marshaler->MarshalNativeToManaged(IntPtr(&cnst->value));
									
									Constant ^constant = gcnew Constant(this, gcnew String(cnst->name, 0,
										cnst->name_len - 1), value, !(cnst->flags & CONST_CS));
									AddConstant(constant->GetName(), constant);
									zend_hash_move_forward(table);
								}
							}
						}
					}
					finally
					{
						tsrm_mutex_unlock(mx_module_entries);
					}
				}

				// refresh ini directives
				ModuleBoundContext = this;
				try
				{
					zend_ini_refresh_ini_entries_one_module(moduleEntry->module_number, tsrm_ls);
				}
				finally
				{
					ModuleBoundContext = nullptr;
				}
			
				// enumerate functions
				functions = gcnew Hashtable(30);
				zend_function_entry *func = moduleEntry->functions;

				if (func)
				{
					while (func->fname)
					{
						Debug_WriteLine("EXT SUP", System::String::Concat("Registering function: ", gcnew System::String(func->fname)));

						functions->Add((gcnew String(func->fname))->ToLower(), gcnew Function(this, func));
						func++;
					}
				}
			}
			finally
			{
				Debug_Unindent();
			}

			String ^mod_name = GetModuleName();
			char *lcname = PhpMarshaler::MarshalManagedStringToNativeString(mod_name->ToLower());
			try
			{
				zend_module_entry *module_ptr;
				zend_hash_add(&module_registry, lcname, mod_name->Length + 1, (void *)moduleEntry,
					sizeof(zend_module_entry), (void **)&module_ptr);
			}
			finally
			{
				efree(lcname);
			}

			initialized = true;
		}

		// Unloads this <see cref="Module"/>.
		void DynamicModule::ModuleShutdown()
		{
			if (!initialized) return;
			initialized = false;

			Debug_WriteLine("EXT SUP", String::Concat("Unloading module: ", GetFileName()));

			String ^mod_name = GetModuleName();
			char *lcname = PhpMarshaler::MarshalManagedStringToNativeString(mod_name->ToLower());
			try
			{
				zend_hash_del_key_or_index(&module_registry, lcname, mod_name->Length + 1, 0, HASH_DEL_KEY);
			}
			finally
			{
				efree(lcname);
			}

			Exception ^exception = nullptr;
			
			// call shutdown function
			if (Interlocked::Decrement(moduleEntry->module_started) == 0)
			{
				if (moduleEntry->module_shutdown_func)
				{
					ModuleBoundContext = this;
					try
					{
						TSRMLS_FETCH();

						if (moduleEntry->module_shutdown_func(MODULE_PERSISTENT, 
							moduleEntry->module_number, tsrm_ls) != SUCCESS)
						{
							exception = gcnew ExtensionException(ExtResources::GetString("module_shutdown_func_failed"));
						}
					}
					catch (Exception ^e)
					{ 
						exception = e;
					}
					finally
					{
						ModuleBoundContext = nullptr;
					}
				}
			}
			
			// an exception can occur during the call to lib's DllMain()
			try
			{
				FreeLibrary(hLib);
			}
			catch (Exception ^e)
			{
				exception = e;
			}

			 Debug_WriteLine_Conditional((exception != nullptr), "EXT SUP", String::Format("{0}: misbehaviour while unloading ({1})", fileName,
				exception->Message));
		}

		// Calls extension's <c>info_func</c> handler.
		bool DynamicModule::PhpInfo()
		{
			if (moduleEntry->info_func)
			{
				Request ^request = Request::GetCurrentRequest();
				StringBuilder ^builder = request->PhpInfoBuilder;

				if (builder != nullptr)
				{
					builder->Append(PHP::Core::PhpNetInfo::PrintSectionCaption(false, GetModuleName()));
				}
				else
				{
					Debug_WriteLine("EXT SUP", "Module::PhpInfo: current request has null PhpInfoBuilder");

					return false;
				}

				Module ^old_module = request->CurrentInvocationContext.Module;
				request->CurrentInvocationContext.Module = this;
				try
				{
					TSRMLS_FETCH();
					moduleEntry->info_func(moduleEntry, tsrm_ls);
				}
				finally
				{
					request->CurrentInvocationContext.Module = old_module;
				}
				return true;
			}
			else return false;
		}

		// Calls extension's <c>request_startup_func</c> handler.
		void DynamicModule::RequestStartup(Request ^request)
		{
			TSRMLS_FETCH();

			Module ^old_module = request->CurrentInvocationContext.Module;
			request->CurrentInvocationContext.Module = this;
			try
			{
				zend_ini_update_configuration_one_module(moduleEntry->module_number, tsrm_ls);
				zend_ini_refresh_caches_one_module(moduleEntry->module_number, tsrm_ls);
			}
			finally
			{
				request->CurrentInvocationContext.Module = old_module;
			}


			Debug_WriteLine("EXT SUP", System::String::Concat(fileName, "->request_startup_func"));

			if (moduleEntry->request_startup_func)
			{
				Request ^request;
				Module ^old_module;

				request = Request::GetCurrentRequest();

				String ^old_current_dir;
				if (earlyInit)
				{
					// store and change current directory to ExtNatives dir so that dependencies could be found 
					// in the case the extension loads them by LoadLibrary:
					old_current_dir = Environment::CurrentDirectory;
					Environment::CurrentDirectory = PHP::Core::Configuration::Application->Paths->ExtNatives;
				}

				old_module = request->CurrentInvocationContext.Module;
				request->CurrentInvocationContext.Module = this;

				try
				{
#ifdef _DEBUG
					if (_init_handler_wrapper(moduleEntry->request_startup_func, MODULE_PERSISTENT, 
						moduleEntry->module_number, tsrm_ls) != SUCCESS)
#else
					if (moduleEntry->request_startup_func(MODULE_PERSISTENT, 
						moduleEntry->module_number, tsrm_ls) != SUCCESS)
#endif
					{
						RemoteDispatcher::ThrowException(PhpError::Error,
							ExtResources::GetString("request_startup_func_failed", fileName));
						return;
					}
				}
				finally
				{
					request->CurrentInvocationContext.Module = old_module;

					if (earlyInit)
					{
						// restores directory:
						Environment::CurrentDirectory = old_current_dir;
					}
				}
			}
		}

		// Calls extensions's <c>request_shutdown_func</c> handler.
		void DynamicModule::RequestShutdown(Request ^request)
		{
#ifdef DEBUG
			Debug::WriteLine("EXT SUP", System::String::Concat(fileName, "->request_shutdown_func"));
#endif
			if (moduleEntry->request_shutdown_func)
			{
				Module ^old_module;

				old_module = request->CurrentInvocationContext.Module;
				request->CurrentInvocationContext.Module = this;

				try
				{
					TSRMLS_FETCH();

					if (moduleEntry->request_shutdown_func(MODULE_PERSISTENT, 
						moduleEntry->module_number, tsrm_ls) != SUCCESS)
					{
						RemoteDispatcher::ThrowException(PhpError::Error,
							ExtResources::GetString("request_shutdown_func_failed", fileName));
						return;
					}
				}
				finally
				{
					request->CurrentInvocationContext.Module = old_module;
				}
			}
		}

		// Generates managed wrapper for this extension.
		String ^DynamicModule::GenerateManagedWrapper()
		{
			Request ^request = Request::GetCurrentRequest(false, false);

			PHP::Core::OrderedHashtable<String ^> ^cls;

			// merge resident and transient classes if necessary
			if (request != nullptr && request->TransientClasses->Count > 0)
			{
				cls = gcnew PHP::Core::OrderedHashtable<String ^>(classes->Count);

				for each (scg::KeyValuePair<String ^, Object^> entry in classes)
				{
					cls->Add(entry.Key, entry.Value);
				}

				for each (scg::KeyValuePair<String ^, Object^> entry in request->TransientClasses)
				{
					Class ^cl = static_cast<Class ^>(entry.Value);
					if (cl != nullptr && cl->GetContainingModule() == this) cls->Add(entry.Key, cl);
				}
			}
			else cls = classes;

			Hashtable ^cns;

			// merge resident and transient constants if necessary
			if (request != nullptr && request->TransientConstants->Count > 0)
			{
				cns = gcnew Hashtable(constants->Count);

				IDictionaryEnumerator ^enumerator = constants->GetEnumerator();
				while (enumerator->MoveNext()) cns->Add(enumerator->Key, enumerator->Value);

				enumerator = request->TransientConstants->GetEnumerator();
				while (enumerator->MoveNext())
				{
					Constant ^cn = static_cast<Constant ^>(enumerator->Value);
					if (cn != nullptr && cn->GetContainingModule() == this) cns->Add(enumerator->Key, cn);
				}
			}
			else cns = constants;
	
			WrapperGen ^wrapper_gen = gcnew WrapperGen(fileName, GetModuleName(), cns, functions, cls);
			return wrapper_gen->GenerateManagedWrapper();
		}

		// Returns reference to the corresponding managed wrapper assembly.
		Assembly ^DynamicModule::GetWrappingAssembly()
		{
			if (wrappingAssembly != nullptr) return wrappingAssembly;

			String ^ass_name = String::Concat(fileName, Externals::WrapperAssemblySuffix);

			// search in loaded assemblies
			array<Assembly ^> ^asses = AppDomain::CurrentDomain->GetAssemblies();
			for (int i = 0; i < asses->Length; i++)
			{
				if (String::Compare(asses[i]->GetName()->Name, ass_name, true) == 0)
				{
					wrappingAssembly = asses[i];
					return wrappingAssembly;
				}
			}

			// not found in loaded assemblies => load it!
			String ^wrappers_path = PHP::Core::Configuration::Application->Paths->ExtWrappers;

			String ^ass_path = nullptr;
			try
			{
				if (wrappers_path != nullptr)
				{
					ass_path = Path::Combine(wrappers_path, String::Concat(fileName, Externals::WrapperAssemblySuffix, ".dll"));

					// The managed wrapper must be loaded in order to have managed versions of the classes
					// defined in this application domain (Remoting would not be able to deserialize the type).
					// However, if we map the *.mng.dll (for example with Assembly.LoadFile), it would not be
					// possible to regenerate the wrapper. That's why the assembly is loaded kindda indirectly.

					FileStream ^stream = gcnew FileStream(ass_path, FileMode::Open,	FileAccess::Read,
						FileShare::Read);
					try
					{
						array<unsigned char> ^ass_bytes = gcnew array<unsigned char>((int)stream->Length);
						if (stream->Read(ass_bytes, 0, ass_bytes->Length) == ass_bytes->Length)
						{
							wrappingAssembly = Assembly::Load(ass_bytes);

							Debug_WriteLine("EXT SUP", String::Format("Assembly '{0}' loaded", ass_path));
						}
					}
					finally
					{
						delete stream;
					}
				}
				else
				{
					AssemblyName ^name	= gcnew AssemblyName();
					name->Name    = String::Concat(fileName, Externals::WrapperAssemblySuffix);
					name->Version = WrapperGen::wrapperVersion;

					// If the wrapper assembly is supposed to be in GAC, load it using Load
					wrappingAssembly = Assembly::Load(name);
				}
			}
			catch (FileNotFoundException ^)
			{ }
#ifdef DEBUG
			catch (Exception ^e)
			{
				Debug::WriteLine("EXT SUP", String::Format("Exception caught when trying to load assembly {0}.\n{1}",
					ass_path == nullptr ? fileName : ass_path, e->ToString()));
			}
#else
			catch (Exception ^)
			{ }
#endif

			return wrappingAssembly;
		}

		// Loads a specified extension.
		DynamicModule ^DynamicModule::LoadDynamicModule(ExtensionLibraryDescriptor ^descriptor)
		{
			DynamicModule ^mod = gcnew DynamicModule(
				descriptor->ExtensionPath.ToString(),
				descriptor->FileName,
				descriptor->EarlyInit);
			
			modules->Add(descriptor->FileName, mod);
			modulesByInternalName->Add(gcnew System::String(mod->moduleEntry->name), mod);

			if (descriptor->EarlyInit) earlyInitModules->Add(mod);

			// can't synchronize this one as it is published as HashTable through ZEND_API
			zend_hash_add(&module_registry, mod->moduleEntry->name, strlen(mod->moduleEntry->name) + 1,
				(void *)mod->moduleEntry, sizeof(zend_module_entry), NULL);

			return mod;
		}


		// InternalStreamWrappers implementation:

		// Registers internal stream wrappers.
		InternalStreamWrappers::InternalStreamWrappers()
		{
			functions = gcnew Hashtable();
			constants = gcnew Hashtable();
			classes = gcnew OrderedHashtable<String ^>();

			Module::ModuleBoundContext = this;
			try
			{
				TSRMLS_FETCH();
#if defined(PHP4TS) || defined(PHP5TS)
				php_register_url_stream_wrapper("http", &php_stream_http_wrapper TSRMLS_CC);
				php_register_url_stream_wrapper("ftp", &php_stream_ftp_wrapper TSRMLS_CC);
#endif
			}
			finally
			{
				Module::ModuleBoundContext = nullptr;
			}
		}

		// Unloads this <see cref="Module"/>.
		void InternalStreamWrappers::ModuleShutdown()
		{
			TSRMLS_FETCH();
			php_unregister_url_stream_wrapper("http" TSRMLS_CC);
			php_unregister_url_stream_wrapper("ftp" TSRMLS_CC);
		}

		// Loads this extension.
		void InternalStreamWrappers::LoadModule()
		{
			InternalStreamWrappers ^mod = gcnew InternalStreamWrappers();

			modules->Add(mod->GetFileName(), mod);
			modulesByInternalName->Add(mod->GetModuleName(), mod);
		}
	}
}
