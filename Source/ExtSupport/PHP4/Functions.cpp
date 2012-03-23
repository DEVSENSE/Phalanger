//
// ExtSupport.PHP4 - substitute for php4ts.dll
//
// Functions.cpp
// - contains definitions of function related exported functions
//

#include "stdafx.h"
#include "Functions.h"
#include "Memory.h"
#include "Variables.h"
#include "Errors.h"
#include "PhpMarshaler.h"
#include "Request.h"
#include "Unsupported.h"

using namespace PHP::ExtManager;

#ifdef PHP5TS

ZEND_API const zend_fcall_info empty_fcall_info = { 0, NULL, NULL, NULL, NULL, 0, NULL, NULL, 0 };
ZEND_API const zend_fcall_info_cache empty_fcall_info_cache = { 0, NULL, NULL, NULL, NULL };

// Copied from zend_api.c
ZEND_API int zend_fcall_info_init(zval *callable, zend_fcall_info *fci, zend_fcall_info_cache *fcc TSRMLS_DC)
{
	zend_class_entry *ce;
	zend_function    *func;
	zval             **obj;

	if (!zend_is_callable_ex(callable, IS_CALLABLE_STRICT, NULL, NULL, &ce, &func, &obj TSRMLS_CC)) {
		return FAILURE;
	}

	fci->size = sizeof(*fci);
	fci->function_table = ce ? &ce->function_table : EG(function_table);
	fci->object_ptr = *obj;
	fci->function_name = callable;
	fci->retval_ptr_ptr = NULL;
	fci->param_count = 0;
	fci->params = NULL;
	fci->no_separation = 1;
	fci->symbol_table = NULL;

        if (strlen(func->common.function_name) == sizeof(ZEND_CALL_FUNC_NAME) - 1 && !memcmp(func->common.function_name, ZEND_CALL_FUNC_NAME, sizeof(ZEND_CALL_FUNC_NAME))) {
		fcc->initialized = 0;
		fcc->function_handler = NULL;
		fcc->calling_scope = NULL;
		fcc->called_scope = NULL;
		fcc->object_ptr = NULL;
	} else {
		fcc->initialized = 1;
		fcc->function_handler = func;
		fcc->calling_scope = ce;
		fcc->object_ptr = *obj;
	}

	return SUCCESS;
}
#endif

// copied from zend_execute_API.c and beautified
ZEND_API int call_user_function(HashTable *function_table, zval **object_pp, zval *function_name, zval *retval_ptr, int param_count, zval *params[] TSRMLS_DC)
{
	zval ***params_array = (zval ***)emalloc(sizeof(zval **)*param_count);
	int i;
	int ex_retval;
	zval *local_retval_ptr;

	for (i = 0; i < param_count; i++) params_array[i] = &params[i];

	ex_retval = call_user_function_ex(function_table, object_pp, function_name, &local_retval_ptr, param_count, 
		params_array, 1, NULL TSRMLS_CC);

	if (local_retval_ptr)
	{
		COPY_PZVAL_TO_ZVAL(*retval_ptr, local_retval_ptr);
	}
	else INIT_ZVAL(*retval_ptr);

	efree(params_array);
	return ex_retval;
}

// copied from zend_execute_API.c and beautified
ZEND_API int call_user_function_ex(HashTable *function_table, zval **object_pp, zval *function_name,
								   zval **retval_ptr_ptr, int param_count, zval **params[], int no_separation,
								   HashTable *symbol_table TSRMLS_DC)
{
	zend_fcall_info fci;

	fci.size = sizeof(fci);
	fci.function_table = function_table;
	fci.object_ptr = object_pp ? *object_pp : NULL;
	fci.function_name = function_name;
	fci.retval_ptr_ptr = retval_ptr_ptr;
	fci.param_count = param_count;
	fci.params = params;
	fci.no_separation = (zend_bool) no_separation;
	fci.symbol_table = symbol_table;

	return zend_call_function(&fci, NULL TSRMLS_CC);
}

// rewritten, calls back to Core to invoke a (mostly user) function or method
ZEND_API int zend_call_function(zend_fcall_info *fci, zend_fcall_info_cache *fci_cache TSRMLS_DC)
{
	if (fci->size != sizeof(zend_fcall_info))
	{
		zend_error(E_ERROR, "Corrupted fcall_info provided to zend_call_function()");
		return FAILURE;
	}

	if (fci->function_name->type == IS_ARRAY)
	{
		/* assume array($obj, $name) couple */
		zval **tmp_object_ptr, **tmp_real_function_name;

		if (zend_hash_index_find(fci->function_name->value.ht, 0, (void **)&tmp_object_ptr) == FAILURE) return FAILURE;
		if (zend_hash_index_find(fci->function_name->value.ht, 1, (void **)&tmp_real_function_name) == FAILURE) return FAILURE;

		fci->function_name = *tmp_real_function_name;
		SEPARATE_ZVAL_IF_NOT_REF(tmp_object_ptr);
		fci->object_ptr = *tmp_object_ptr;
		fci->object_ptr->is_ref = 1;
	}
	//if (fci->object_pp && !*fci->object_pp) fci->object_pp = NULL;

	Request ^request = Request::GetCurrentRequest();
	PhpMarshaler ^marshaler = PhpMarshaler::GetInstance(request->CurrentInvocationContext.Module);

	// marshal function name and create PhpCallback
	Object ^target_obj = (fci->object_ptr == NULL ? nullptr : marshaler->MarshalNativeToManaged(IntPtr(fci->object_ptr)));
	Object ^func_name = marshaler->MarshalNativeToManaged(IntPtr(fci->function_name));

	PhpCallback ^callback;
	if (target_obj != nullptr)
	{
		PhpArray ^tmp = gcnew PhpArray(2, 0);
		tmp->Add(target_obj);
		tmp->Add(func_name);

		callback = PHP::Core::Convert::ObjectToCallback(tmp);
	}
	else callback = PHP::Core::Convert::ObjectToCallback(func_name);

	if (callback == nullptr || callback->IsInvalid) return FAILURE;

	array<Object ^> ^mng_args = gcnew array<Object ^>(fci->param_count);

	// marshal function arguments
	for (unsigned i = 0; i < fci->param_count; i++)
	{
		mng_args[i] = marshaler->MarshalNativeToManaged(IntPtr(*fci->params[i]));
	}

	Object ^ret_val;

	//RequestCookie ^cookie = request->GetCookie();
	//if (cookie != nullptr)
	//{
	//	// second arg passed by ref!
	//	ret_val = cookie->CallFunction(callback, mng_args);
	//}
	//else
	{
		ret_val = RequestCookie::CallFunctionDirect(callback, mng_args);
	}

	// unmarshal byref parameters
	for (unsigned i = 0; i < fci->param_count; i++)
	{
		if ((*fci->params[i])->is_ref)
		{
			marshaler->CleanUpNativeData(IntPtr(*fci->params[i]));
			*fci->params[i] = (zval *)marshaler->MarshalManagedToNative(mng_args[i]).ToPointer();
		}
	}

	// unmarshal self (hope it's legal to update object_pp this way)
	/*
	if (callback->TargetInstance != NULL && fci->object_pp != NULL)
	{
		zval *new_self = (zval *)marshaler->MarshalManagedToNative(callback->TargetInstance).ToPointer();
		marshaler->CleanUpNativeData(IntPtr(*fci->object_pp));
		*fci->object_pp = new_self;
	}
	*/

	// unmarshal return value
	*fci->retval_ptr_ptr = (zval *)marshaler->MarshalManagedToNative(ret_val).ToPointer();

	return SUCCESS;
}

#pragma unmanaged

// copied from zend_opcode.c and beautified
ZEND_API void destroy_zend_function(zend_function *function TSRMLS_DC)
{
	switch (function->type)
	{
		case ZEND_USER_FUNCTION:
			destroy_op_array((zend_op_array *)function TSRMLS_CC);
			break;

		case ZEND_INTERNAL_FUNCTION:
			/* do nothing */
			break;
	}
}
