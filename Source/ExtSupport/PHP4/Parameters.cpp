//
// ExtSupport.PHP4 - substitute for php4ts.dll
//
// Parameters.cpp 
// - contains declarations of parameter handling functions
// - input parameters are not passed to external functions directly; functions
//   use zend_parse_parameters and zend_parse_parameters_ex to retrieve them
//

#include "stdafx.h"
#include "Parameters.h"
#include "Variables.h"
#include "ExtSupport.h"
#include "Memory.h"
#include "Helpers.h"
#include "Errors.h"
#include "PhpMarshaler.h"
#include "Request.h"
#include "Objects.h"

#include <stdarg.h>
#include <stdlib.h>
#include <stdio.h>

using namespace System;

using namespace PHP::ExtManager;

ZEND_API unsigned char first_arg_force_ref[] = { 1, BYREF_FORCE };
ZEND_API unsigned char second_arg_force_ref[] = { 2, BYREF_NONE, BYREF_FORCE };
ZEND_API unsigned char third_arg_force_ref[] = { 3, BYREF_NONE, BYREF_NONE, BYREF_FORCE };


// request = a request (usually current request)
// i = argument number, zero-based
// returns the argument in native form
static zval **zend_managed_get_arg(Request ^request, int i)
{
	return &(request->CurrentInvocationContext.FunctionPhpArgs[i]);
}

ZEND_API int zend_lookup_class(char *name, int name_length, zend_class_entry ***ce TSRMLS_DC)
{
	//TODO: This should be done via managed classes
	throw gcnew InvalidOperationException("Classes are not registered in Phalanger.");
}			

#pragma unmanaged 

// copied from zend_API.c and beautified
static char *zend_parse_arg_impl(int arg_num, zval **arg, va_list *va, char **spec TSRMLS_DC)
{
	char *spec_walk = *spec;
	char c = *spec_walk++;
	int return_null = 0;

	while (*spec_walk == '/' || *spec_walk == '!')
	{
		if (*spec_walk == '/') 
		{
			SEPARATE_ZVAL_IF_NOT_REF(arg);
		}
		else if (*spec_walk == '!' && Z_TYPE_PP(arg) == IS_NULL) return_null = 1;

		spec_walk++;
	}

	switch (c) 
	{
		// caller expects a long
		case 'l':
			{
				long *p = va_arg(*va, long *);
				switch (Z_TYPE_PP(arg))
				{
					case IS_STRING:
						{
							double d;
							int type;

							if ((type = is_numeric_string(Z_STRVAL_PP(arg), Z_STRLEN_PP(arg), p, &d, 0)) == 0) return "long";
							else if (type == IS_DOUBLE) *p = (long)d;
						}
						break;

					case IS_NULL:	case IS_LONG: 
					case IS_DOUBLE: case IS_BOOL:
						convert_to_long_ex(arg);
						*p = Z_LVAL_PP(arg);
						break;

					case IS_ARRAY:	case IS_OBJECT:
					case IS_RESOURCE:
					default:
						return "long";
				}
			}
			break;

		// caller expects a double
		case 'd':
			{
				double *p = va_arg(*va, double *);
				switch (Z_TYPE_PP(arg))
				{
					case IS_STRING:
						{
							long l;
							int type;

							if ((type = is_numeric_string(Z_STRVAL_PP(arg), Z_STRLEN_PP(arg), &l, p, 0)) == 0) return "double";
							else if (type == IS_LONG) *p = (double)l;
						}
						break;

					case IS_NULL:	case IS_LONG:
					case IS_DOUBLE:	case IS_BOOL:
						convert_to_double_ex(arg);
						*p = Z_DVAL_PP(arg);
						break;

					case IS_ARRAY:	case IS_OBJECT:
					case IS_RESOURCE:
					default:
						return "double";
				}
			}
			break;

		// caller expects a string
		case 's':
			{
				char **p = va_arg(*va, char **);
				int *pl = va_arg(*va, int *);
				switch (Z_TYPE_PP(arg))
				{
					case IS_NULL:
						if (return_null)
						{
							*p = NULL;
							*pl = 0;
							break;
						}
						/* break omitted intentionally */

					case IS_STRING:	case IS_LONG:
					case IS_DOUBLE:	case IS_BOOL:
						convert_to_string_ex(arg);
						*p = Z_STRVAL_PP(arg);
						*pl = Z_STRLEN_PP(arg);
						break;

					case IS_ARRAY:	case IS_OBJECT:
					case IS_RESOURCE:
					default:
						return "string";
				}
			}
			break;

		// caller expects a boolean
		case 'b':
			{
				zend_bool *p = va_arg(*va, zend_bool *);
				switch (Z_TYPE_PP(arg)) 
				{
					case IS_NULL:	case IS_STRING:
					case IS_LONG:	case IS_DOUBLE:
					case IS_BOOL:
						convert_to_boolean_ex(arg);
						*p = Z_BVAL_PP(arg);
						break;

					case IS_ARRAY:	case IS_OBJECT:
					case IS_RESOURCE:
					default:
						return "boolean";
				}
			}
			break;

		// caller expects a hashtable?
		case 'h':
			{
				HashTable **p = va_arg(*va, HashTable **);
				if (Z_TYPE_PP(arg) != IS_ARRAY) {
					if (Z_TYPE_PP(arg) == IS_NULL && return_null) {
						*p = NULL;
					} else {
						return "array";
					}
				} else {
					*p = Z_ARRVAL_PP(arg);
				}
			}
			break;

		// caller expects a resource
		case 'r':
			{
				zval **p = va_arg(*va, zval **);
				if (Z_TYPE_PP(arg) != IS_RESOURCE)
				{
					if (Z_TYPE_PP(arg) == IS_NULL && return_null) *p = NULL;
					else return "resource";
				} 
				else *p = *arg;
			}
			break;

		// caller expects an array
		case 'a':
			{
				zval **p = va_arg(*va, zval **);
				if (Z_TYPE_PP(arg) != IS_ARRAY) 
				{
					if (Z_TYPE_PP(arg) == IS_NULL && return_null) *p = NULL;
					else return "array";
				}
				else *p = *arg;
			}
			break;

		// caller expects an object (of any class)
		case 'o':
			{
				zval **p = va_arg(*va, zval **);
				if (Z_TYPE_PP(arg) != IS_OBJECT)
				{
					if (Z_TYPE_PP(arg) == IS_NULL && return_null) *p = NULL;
					else return "object";
				}
				else *p = *arg;
			}
			break;

		// caller expects an object (of class specified by class entry)
		case 'O':
		{
			zval **p = va_arg(*va, zval **);
			zend_class_entry *ce = va_arg(*va, zend_class_entry *);

			if (Z_TYPE_PP(arg) == IS_OBJECT &&
#ifdef PHP5TS
					(!ce || instanceof_function(Z_OBJCE_PP(arg), ce TSRMLS_CC))
#else
					zend_check_class(*arg, ce)
#endif
				) {
				*p = *arg;
			} else {
				if (Z_TYPE_PP(arg) == IS_NULL && return_null) {
					*p = NULL;
				} else if (ce) {
					return ce->name;
				} else {
					return "object";
				}
			}
		}
		break;

#ifdef PHP5TS
		case 'C':
			{
				zend_class_entry **lookup, **pce = va_arg(*va, zend_class_entry **);
				zend_class_entry *ce_base = *pce;

				if (return_null && Z_TYPE_PP(arg) == IS_NULL) {
					*pce = NULL;
					break;
				}
				convert_to_string_ex(arg);
				if (zend_lookup_class(Z_STRVAL_PP(arg), Z_STRLEN_PP(arg), &lookup TSRMLS_CC) == FAILURE) {
					*pce = NULL;
				} else {
					*pce = *lookup;
				}
				if (ce_base) {
					if ((!*pce || !instanceof_function(*pce, ce_base TSRMLS_CC)) && !return_null) {
						char *space;
						char *class_name = get_active_class_name(&space TSRMLS_CC);
						zend_error(E_WARNING, "%s%s%s() expects parameter %d to be a class name derived from %s, '%s' given",
							   class_name, space, get_active_function_name(TSRMLS_C),
							   arg_num, ce_base->name, Z_STRVAL_PP(arg));
						*pce = NULL;
						return "";
					}
				}
				if (!*pce) {
					char *space;
					char *class_name = get_active_class_name(&space TSRMLS_CC);
					zend_error(E_WARNING, "%s%s%s() expects parameter %d to be a valid class name, '%s' given",
						   class_name, space, get_active_function_name(TSRMLS_C),
						   arg_num, Z_STRVAL_PP(arg));
					return "";
				}

			}
			break;

		case 'f':
			{
				zend_fcall_info       *fci = va_arg(*va, zend_fcall_info *);
				zend_fcall_info_cache *fcc = va_arg(*va, zend_fcall_info_cache *);

				if (zend_fcall_info_init(*arg, fci, fcc TSRMLS_CC) == SUCCESS) {
					break;
				} else if (return_null) {
					fci->size = 0;
					fcc->initialized = 0;
					break;
				} else {
					return "function";
				}

			}
#endif
		case 'z':
			{
				zval **p = va_arg(*va, zval **);
				if (Z_TYPE_PP(arg) == IS_NULL && return_null) {
					*p = NULL;
				} else {
					*p = *arg;
				}
			}
			break;
		case 'Z':
			{
				zval ***p = va_arg(*va, zval ***);
				if (Z_TYPE_PP(arg) == IS_NULL && return_null) {
					*p = NULL;
				} else {
					*p = arg;
				}
			}
			break;

		default:
			return "unknown";
	}

	*spec = spec_walk;

	return NULL;
}

// copied from zend_API.c, slightly modified and beautified
static int zend_parse_arg(int arg_num, zval **arg, va_list *va, char **spec, int quiet TSRMLS_DC)
{
	char *expected_type = NULL;

	expected_type = zend_parse_arg_impl(arg_num, arg, va, spec TSRMLS_CC);
	if (expected_type) 
	{
		if (!quiet) 
		{
			zend_error(E_WARNING, "%s() expects parameter %d to be %s, %s given",
					get_active_function_name(TSRMLS_C), arg_num, expected_type, zend_zval_type_name(*arg));
		}
		return FAILURE;
	}
	return SUCCESS;
}

#pragma managed

// copied from zend_API.c, slightly modified and beautified
static int zend_parse_va_args(int num_args, char *type_spec, va_list *va, int flags TSRMLS_DC)
{
#ifdef DEBUG
	Debug::WriteLine("PHP4TS", "Entering zend_parse_va_arg");
#endif

	int c, i, arg_count, min_num_args = -1, max_num_args = 0;
	int quiet = flags & ZEND_PARSE_PARAMS_QUIET;
	char *spec_walk, buf[1024];
	zval **arg;

	for (spec_walk = type_spec; *spec_walk; spec_walk++)
	{
		c = *spec_walk;
		switch (c) {
			case 'l': case 'd': case 's': case 'b': case 'r': case 'a':	case 'o': case 'O':	case 'z':
				max_num_args++;
				break;

			case '|':
				min_num_args = max_num_args;
				break;

			case '/': case '!':
				/* Pass */
				break;

			default:
				if (!quiet)
				{
					zend_error(E_WARNING, "%s(): bad type specifier while parsing parameters", 
						get_active_function_name(TSRMLS_C));
				}
				return FAILURE;
		}
	}

	if (min_num_args < 0) min_num_args = max_num_args;

	if (num_args < min_num_args || num_args > max_num_args)
	{
		if (!quiet)
		{
			sprintf(buf, "%s() expects %s %d parameter%s, %d given",
					get_active_function_name(TSRMLS_C),
					min_num_args == max_num_args ? "exactly" : num_args < min_num_args ? "at least" : "at most",
					num_args < min_num_args ? min_num_args : max_num_args,
					(num_args < min_num_args ? min_num_args : max_num_args) == 1 ? "" : "s",
					num_args);
			zend_error(E_WARNING, buf);
		}
		return FAILURE;
	}

	// get number of arguments from the current request
	Request ^request = Request::GetCurrentRequest();
	arg_count = (request->CurrentInvocationContext.FunctionArgs == nullptr ? 0 : request->CurrentInvocationContext.FunctionArgs->Length);

	if (num_args > arg_count)
	{
		zend_error(E_WARNING, "%s(): could not obtain parameters for parsing",
				   get_active_function_name(TSRMLS_C));
		return FAILURE;
	}

	i = 0;

	while (num_args-- > 0) 
	{
		arg = zend_managed_get_arg(request, i);

		if (*type_spec == '|') type_spec++;
		if (zend_parse_arg(i + 1, arg, va, &type_spec, quiet TSRMLS_CC) == FAILURE) return FAILURE;
		i++;
	}
	return SUCCESS;
}

#pragma unmanaged

// copied from zend_API.c
ZEND_API int zend_parse_parameters(int num_args TSRMLS_DC, char *type_spec, ...)
{
	va_list va;
	int retval;
	
	va_start(va, type_spec);
	retval = zend_parse_va_args(num_args, type_spec, &va, 0 TSRMLS_CC);
	va_end(va);

	return retval;
}

// copied from zend_API.c
ZEND_API int zend_parse_parameters_ex(int flags, int num_args TSRMLS_DC, char *type_spec, ...)
{
	va_list va;
	int retval;
	
	va_start(va, type_spec);
	retval = zend_parse_va_args(num_args, type_spec, &va, flags TSRMLS_CC);
	va_end(va);

	return retval;
}

#pragma managed

// rewritten
ZEND_API char *get_active_function_name(TSRMLS_D)
{
	return Request::GetCurrentRequest()->CurrentInvocationContext.FunctionName;
}

/* return class name and "::" or "". */
#ifdef PHP5TS
ZEND_API char *get_active_class_name(char **space TSRMLS_DC)
{
	switch (Request::GetCurrentRequest()->CurrentInvocationContext.FunctionType) {
		case ZEND_USER_FUNCTION:
		case ZEND_INTERNAL_FUNCTION:
		{
			zend_class_entry *ce = EG(scope);

			if (space) {
				*space = ce ? "::" : "";
			}
			return ce ? ce->name : "";
		}
		default:
			if (space) {
				*space = "";
			}
			return "";
	}
}
#endif

static int _zend_get_parameters(int ht, int param_count, va_list ptr)
{
	int arg_count;
	zval **param, *param_ptr;

	// get number of arguments from the current request
	Request ^request = Request::GetCurrentRequest();
	arg_count = (request->CurrentInvocationContext.FunctionArgs == nullptr ? 0 : request->CurrentInvocationContext.FunctionArgs->Length);

	if (param_count > arg_count) return FAILURE;
	arg_count = 0;

	while (param_count-- > 0)
	{
		// cannot use va_arg here, because it expands to some assembler, which is
		// not allowed in managed functions
		param = /*va_arg(ptr, zval **)*/ *((zval ***)ptr);
		ptr += 4;

		param_ptr = *zend_managed_get_arg(request, arg_count);

		if (!PZVAL_IS_REF(param_ptr) && param_ptr->refcount > 1)
		{
			zval *new_tmp;

			ALLOC_ZVAL(new_tmp);
			*new_tmp = *param_ptr;
			zval_copy_ctor(new_tmp);
			INIT_PZVAL(new_tmp);
			param_ptr = new_tmp;

			zval **_tmp = zend_managed_get_arg(request, arg_count);
			(*_tmp)->refcount--;
			*_tmp = param_ptr;
		}

		*param = param_ptr;
		arg_count++;
	}

	return SUCCESS;
}

#pragma unmanaged

// copied from zend_API, modified and beautified
/*  this function doesn't check for too many parameters */
ZEND_API int zend_get_parameters(int ht, int param_count, ...)
{
	va_list ptr;

	va_start(ptr, param_count);
	int ret = _zend_get_parameters(ht, param_count, ptr);
	va_end(ptr);

	return ret;
}

#pragma managed

// copied from zend_API, modified and beautified
ZEND_API int _zend_get_parameters_array(int ht, int param_count, zval **argument_array TSRMLS_DC)
{
	int arg_count;
	zval *param_ptr;

	// get number of arguments from the current request
	Request ^request = Request::GetCurrentRequest();
	arg_count = (request->CurrentInvocationContext.FunctionArgs == nullptr ? 0 : request->CurrentInvocationContext.FunctionArgs->Length);

	if (param_count > arg_count) return FAILURE;

	arg_count = 0;
	while (param_count-- > 0)
	{
		param_ptr = *zend_managed_get_arg(request, arg_count);
		if (!PZVAL_IS_REF(param_ptr) && param_ptr->refcount > 1)
		{
			zval *new_tmp;

			ALLOC_ZVAL(new_tmp);
			*new_tmp = *param_ptr;
			zval_copy_ctor(new_tmp);
			INIT_PZVAL(new_tmp);
			param_ptr = new_tmp;

			zval **_tmp = zend_managed_get_arg(request, arg_count);
			(*_tmp)->refcount--;
			*_tmp = param_ptr;
		}
		*(argument_array++) = param_ptr;
		arg_count++;
	}

	return SUCCESS;
}

static int _zend_get_parameters_ex(int param_count, va_list ptr)
{
	int arg_count;
	zval ***param;

	// get number of arguments from the current request
	Request ^request = Request::GetCurrentRequest();
	arg_count = (request->CurrentInvocationContext.FunctionArgs == nullptr ? 0 : request->CurrentInvocationContext.FunctionArgs->Length);

	if (param_count > arg_count) return FAILURE;
	arg_count = 0;

	while (param_count-- > 0)
	{
		// cannot use va_arg here, because it expands to some assembler, which is
		// not allowed in managed functions
		param = /*va_arg(ptr, zval ***)*/ *((zval ****)ptr);
		ptr += 4;
		
		*param = zend_managed_get_arg(request, arg_count++);
	}

	return SUCCESS;
}

#pragma unmanaged

// copied from zend_API, modified and beautified
/* Zend-optimized Extended functions */
/* this function doesn't check for too many parameters */
ZEND_API int zend_get_parameters_ex(int param_count, ...)
{
	va_list ptr;

	va_start(ptr, param_count);
	int ret = _zend_get_parameters_ex(param_count, ptr);
	va_end(ptr);

	return ret;
}

#pragma managed

// copied from zend_API, modified and beautified
ZEND_API int _zend_get_parameters_array_ex(int param_count, zval ***argument_array TSRMLS_DC)
{
	int arg_count;

	// get number of arguments from the current request
	Request ^request = Request::GetCurrentRequest();
	arg_count = (request->CurrentInvocationContext.FunctionArgs == nullptr ? 0 : request->CurrentInvocationContext.FunctionArgs->Length);

	if (param_count > arg_count) return FAILURE;
	arg_count = 0;

	while (param_count-- > 0)
	{
		*(argument_array++) = zend_managed_get_arg(request, arg_count++);
	}

	return SUCCESS;
}
