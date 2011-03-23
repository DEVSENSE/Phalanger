//
// ExtSupport.PHP4 - substitute for php4ts.dll
//
// Variables.cpp 
// - contains definitions of variables related functions
//

#include "stdafx.h"
#include "Variables.h"
#include "Helpers.h"
#include "ExtSupport.h"
#include "Memory.h"
#include "Errors.h"
#include "Hash.h"
#include "Resources.h"
#include "TsrmLs.h"
#include "Request.h"
#include "Constants.h"
#include "Unsupported.h"
#include "Spprintf.h"
#include "Parameters.h"

#include <stdlib.h>
#include <stdio.h>
#include <limits.h>
#include <math.h>

using namespace System;

using namespace PHP::ExtManager;

#pragma unmanaged

static inline char *
zend_str_tolower_dup(const char *source, unsigned int length)
{
	return zend_str_tolower_copy((char *)emalloc(length+1), source, length);
}

char *empty_string = "";			/* in order to save emalloc() and efree() time for
									 * empty strings (usually used to denote empty
									 * return values in failed functions).
									 * The macro STR_FREE() will not efree() it.
									 */

#define LONG_SIGN_MASK (1L << (8*sizeof(long)-1))

#define zend_strtod(str, endptr) strtod(str, endptr)

zval zval_used_for_init = { NULL, IS_NULL, 0, 1 };

zend_utility_values zend_uv = { ".php", 4, 1 };

// copied from zend_API.c and slightly modified
#define zendi_convert_scalar_to_number(op, holder, result)			\
	if (op==result) {												\
		convert_scalar_to_number(op TSRMLS_CC);						\
	} else {														\
		switch ((op)->type) {										\
			case IS_STRING:											\
				{													\
					switch (((holder).type=is_numeric_string((op)->value.str.val, (op)->value.str.len, &(holder).value.lval, &(holder).value.dval, 1))) {	\
						case IS_DOUBLE:															\
						case IS_LONG:															\
							break;																\
						default:																\
							(holder).value.lval = strtol((op)->value.str.val, NULL, 10);		\
							(holder).type = IS_LONG;						\
							break;											\
					}														\
					(op) = &(holder);										\
					break;													\
				}															\
			case IS_BOOL:													\
			case IS_RESOURCE:												\
				(holder).value.lval = (op)->value.lval;						\
				(holder).type = IS_LONG;									\
				(op) = &(holder);											\
				break;														\
			case IS_NULL:													\
				(holder).value.lval = 0;									\
				(holder).type = IS_LONG;									\
				(op) = &(holder);											\
				break;														\
		}																	\
	}

// copied from zend_API.c
#define DVAL_TO_LVAL(d, l) (l) = (d) > LONG_MAX ? (unsigned long) (d) : (long) (d)

// copied from zend_API.c
#define zendi_convert_to_long(op, holder, result)					\
	if (op==result) {												\
		convert_to_long(op);										\
	} else if ((op)->type != IS_LONG) {								\
		switch ((op)->type) {										\
			case IS_NULL:											\
				(holder).value.lval = 0;							\
				break;												\
			case IS_DOUBLE:											\
				DVAL_TO_LVAL((op)->value.dval, (holder).value.lval);	\
				break;												\
			case IS_STRING:											\
				(holder).value.lval = strtol((op)->value.str.val, NULL, 10);					\
				break;												\
			case IS_ARRAY:											\
				(holder).value.lval = (zend_hash_num_elements((op)->value.ht)?1:0);				\
				break;												\
			case IS_OBJECT:											\
				(holder).value.lval = (zend_hash_num_elements(Z_OBJPROP_P(op))?1:0);	\
				break;												\
			case IS_BOOL:											\
			case IS_RESOURCE:										\
				(holder).value.lval = (op)->value.lval;				\
				break;												\
			default:												\
				zend_error(E_WARNING, "Cannot convert to ordinal value");						\
				(holder).value.lval = 0;							\
				break;												\
		}															\
		(holder).type = IS_LONG;									\
		(op) = &(holder);											\
	}

// copied from zend_API.c
#define zendi_convert_to_boolean(op, holder, result)				\
	if (op==result) {												\
		convert_to_boolean(op);										\
	} else if ((op)->type != IS_BOOL) {								\
		switch ((op)->type) {										\
			case IS_NULL:											\
				(holder).value.lval = 0;							\
				break;												\
			case IS_RESOURCE:										\
			case IS_LONG:											\
				(holder).value.lval = ((op)->value.lval ? 1 : 0);	\
				break;												\
			case IS_DOUBLE:											\
				(holder).value.lval = ((op)->value.dval ? 1 : 0);	\
				break;												\
			case IS_STRING:											\
				if ((op)->value.str.len == 0						\
					|| ((op)->value.str.len==1 && (op)->value.str.val[0]=='0')) {	\
					(holder).value.lval = 0;						\
				} else {											\
					(holder).value.lval = 1;						\
				}													\
				break;												\
			case IS_ARRAY:											\
				(holder).value.lval = (zend_hash_num_elements((op)->value.ht)?1:0);	\
				break;												\
			case IS_OBJECT:											\
				(holder).value.lval = (zend_hash_num_elements(Z_OBJPROP_P(op))?1:0);	\
				break;												\
			default:												\
				(holder).value.lval = 0;							\
				break;												\
		}															\
		(holder).type = IS_BOOL;									\
		(op) = &(holder);											\
	}

// copied from zend_API.c and beautified
ZEND_API char *zend_zval_type_name(zval *arg)
{
	switch (arg->type)
	{
		case IS_NULL:		return "null";
		case IS_LONG:		return "integer";
		case IS_DOUBLE:		return "double";
		case IS_STRING:		return "string";
		case IS_ARRAY:		return "array";
		case IS_OBJECT:		return "object";
		case IS_BOOL:		return "boolean";
		case IS_RESOURCE:	return "resource";

		default:			return "unknown";
	}
}

// copied from zend_variables.c, slightly modified and beautified
#ifdef PHP4TS
ZEND_API int _zval_copy_ctor(zval *zvalue ZEND_FILE_LINE_DC)
{
	switch (zvalue->type)
	{
		case IS_RESOURCE:	{
								TSRMLS_FETCH();
								_zend_list_addref(zvalue->value.lval, tsrm_ls);
							}
							break;

		case IS_BOOL:
		case IS_LONG:
		case IS_NULL:		break;

		case IS_CONSTANT:
		case IS_STRING:		if (zvalue->value.str.val)
							{
								if (zvalue->value.str.len==0)
								{
									zvalue->value.str.val = empty_string;
									return SUCCESS;
								}
							}
							CHECK_ZVAL_STRING_REL(zvalue);
							zvalue->value.str.val = (char *)estrndup_rel(zvalue->value.str.val, zvalue->value.str.len);
							break;

		case IS_ARRAY:
		case IS_CONSTANT_ARRAY: 
							{
								zval *tmp;
								HashTable *original_ht = zvalue->value.ht;

								TSRMLS_FETCH();
								if (zvalue->value.ht == &EG(symbol_table)) return SUCCESS;

								ALLOC_HASHTABLE_REL(zvalue->value.ht);
								zend_hash_init(zvalue->value.ht, 0, NULL, ZVAL_PTR_DTOR, 0);
								zend_hash_copy(zvalue->value.ht, original_ht, (copy_ctor_func_t)zval_add_ref, (void *)&tmp, sizeof(zval *));
							}
							break;

		case IS_OBJECT:		{
								zval *tmp;
								HashTable *original_ht = Z_OBJPROP_P(zvalue);
								TSRMLS_FETCH();

								if (original_ht == &EG(symbol_table)) return SUCCESS; /* do nothing */

								ALLOC_HASHTABLE_REL(Z_OBJPROP_P(zvalue));
								zend_hash_init(Z_OBJPROP_P(zvalue), 0, NULL, ZVAL_PTR_DTOR, 0);
								zend_hash_copy(Z_OBJPROP_P(zvalue), original_ht,
									(copy_ctor_func_t)zval_add_ref, (void *) &tmp, sizeof(zval *));
							}
							break;
	}
	return SUCCESS;
}
#elif defined(PHP5TS)
ZEND_API void _zval_copy_ctor_func(zval *zvalue ZEND_FILE_LINE_DC)
{
	switch (zvalue->type) {
		case IS_RESOURCE: {
				TSRMLS_FETCH();

				zend_list_addref(zvalue->value.lval);
			}
			break;
		case IS_BOOL:
		case IS_LONG:
		case IS_NULL:
			break;
		case IS_CONSTANT:
		case IS_STRING:
			CHECK_ZVAL_STRING_REL(zvalue);
			zvalue->value.str.val = (char *) estrndup_rel(zvalue->value.str.val, zvalue->value.str.len);
			break;
		case IS_ARRAY:
		case IS_CONSTANT_ARRAY: {
				zval *tmp;
				HashTable *original_ht = zvalue->value.ht;
				HashTable *tmp_ht = NULL;
				TSRMLS_FETCH();

				if (zvalue->value.ht == &EG(symbol_table)) {
					return; /* do nothing */
				}
				ALLOC_HASHTABLE_REL(tmp_ht);
				zend_hash_init(tmp_ht, zend_hash_num_elements(original_ht), NULL, ZVAL_PTR_DTOR, 0);
				zend_hash_copy(tmp_ht, original_ht, (copy_ctor_func_t) zval_add_ref, (void *) &tmp, sizeof(zval *));
				zvalue->value.ht = tmp_ht;
			}
			break;
		case IS_OBJECT:
			{
				TSRMLS_FETCH();
				Z_OBJ_HT_P(zvalue)->add_ref(zvalue TSRMLS_CC);
			}
			break;
	}
}
#endif
#pragma managed

// copied from zend_variables.c, slightly modified and beautified
#ifdef PHP4TS
ZEND_API void _zval_dtor(zval *zvalue ZEND_FILE_LINE_DC)
{
	if (zvalue->type == IS_LONG) return;

	switch (zvalue->type & ~IS_CONSTANT_INDEX)
	{
		case IS_STRING:
		case IS_CONSTANT:	CHECK_ZVAL_STRING_REL(zvalue);
							STR_FREE_REL(zvalue->value.str.val);
							break;

		case IS_ARRAY:
		case IS_CONSTANT_ARRAY:
							{
								TSRMLS_FETCH();
								if (zvalue->value.ht && (zvalue->value.ht != &EG(symbol_table)))
								{
									zend_hash_destroy(zvalue->value.ht);
									FREE_HASHTABLE(zvalue->value.ht);
								}
							}
							break;

		case IS_OBJECT:		{
								TSRMLS_FETCH();

								zend_hash_destroy(Z_OBJPROP_P(zvalue));
								FREE_HASHTABLE(Z_OBJPROP_P(zvalue));
							}
							break;

		case IS_RESOURCE:	{
								// NEVER DESTROY RESOURCES LIKE THIS - causes many problems
								// destroy resource
								//if (Request::GetCurrentRequest()->DontDestroyResources == false)
								//{
								//	TSRMLS_FETCH();
								//	_zend_list_delete(zvalue->value.lval, tsrm_ls);
								//}
							}
							break;

		case IS_LONG:	case IS_DOUBLE:
		case IS_BOOL:	case IS_NULL:
		default:
							return;
	}
}
#elif defined(PHP5TS)
ZEND_API void _zval_dtor_func(zval *zvalue ZEND_FILE_LINE_DC)
{
	switch (zvalue->type & ~IS_CONSTANT_INDEX) {
		case IS_STRING:
		case IS_CONSTANT:
			CHECK_ZVAL_STRING_REL(zvalue);
			STR_FREE_REL(zvalue->value.str.val);
			break;
		case IS_ARRAY:
		case IS_CONSTANT_ARRAY: {
				TSRMLS_FETCH();

				if (zvalue->value.ht && (zvalue->value.ht != &EG(symbol_table))) {
					zend_hash_destroy(zvalue->value.ht);
					FREE_HASHTABLE(zvalue->value.ht);
				}
			}
			break;
		case IS_OBJECT:
			{
				TSRMLS_FETCH();

				Z_OBJ_HT_P(zvalue)->del_ref(zvalue TSRMLS_CC);
			}
			break;
		case IS_RESOURCE:
			{
				// NEVER DESTROY RESOURCES LIKE THIS - causes many problems
				// destroy resource
				//if (Request::GetCurrentRequest()->DontDestroyResources == false)
				//{
				//TSRMLS_FETCH();
				///* destroy resource */
				//zend_list_delete(zvalue->value.lval);
				//}
			}
			break;
		case IS_LONG:
		case IS_DOUBLE:
		case IS_BOOL:
		case IS_NULL:
		default:
			return;
			break;
	}
}
#endif

#pragma unmanaged

// copied from zend_variables.c and beautified
ZEND_API void _zval_internal_dtor(zval *zvalue ZEND_FILE_LINE_DC)
{
	switch (zvalue->type & ~IS_CONSTANT_INDEX)
	{
		case IS_STRING:
		case IS_CONSTANT:
			CHECK_ZVAL_STRING_REL(zvalue);
			if (zvalue->value.str.val != empty_string) free(zvalue->value.str.val);
			break;

		case IS_ARRAY:
		case IS_CONSTANT_ARRAY:
		case IS_OBJECT:
		case IS_RESOURCE:
			zend_error(E_CORE_ERROR, "Internal zval's can't be arrays, objects or resources");
			break;

		case IS_LONG:
		case IS_DOUBLE:
		case IS_BOOL:
		case IS_NULL:
		default:
			break;
	}
}

// copied from zend_execute.h, slightly modified and beautified
static inline void safe_free_zval_ptr(zval *p)
{
	TSRMLS_FETCH();

	if (p != EG(uninitialized_zval_ptr))
	{
		FREE_ZVAL(p);
	}
}

// copied from zend_execute_API.c, slightly modified and beautified
ZEND_API void _zval_ptr_dtor(zval **zval_ptr ZEND_FILE_LINE_DC)
{
	(*zval_ptr)->refcount--;
	if ((*zval_ptr)->refcount == 0)
	{
		zval_dtor(*zval_ptr);
		safe_free_zval_ptr(*zval_ptr);
	} 
	else if (((*zval_ptr)->refcount == 1) && ((*zval_ptr)->type != IS_OBJECT))
	{
		(*zval_ptr)->is_ref = 0;
	}
}

// copied from zend_execute_API.c, slightly modified and beautified
ZEND_API void _zval_internal_ptr_dtor(zval **zval_ptr ZEND_FILE_LINE_DC)
{
	(*zval_ptr)->refcount--;
	if ((*zval_ptr)->refcount == 0)
	{
		zval_internal_dtor(*zval_ptr);
		free(*zval_ptr);
	}
	else if ((*zval_ptr)->refcount == 1) (*zval_ptr)->is_ref = 0;
}

// copied from zend_variables.c
ZEND_API void zval_add_ref(zval **p)
{
	(*p)->refcount++;
}

// copied from zend_operators.c, slightly modified and beautified
#ifdef PHP4TS
ZEND_API void convert_scalar_to_number(zval *op TSRMLS_DC)
{
	switch (op->type) 
	{
		case IS_STRING:
			{
				char *strval;

				strval = op->value.str.val;
				switch ((op->type = is_numeric_string(strval, op->value.str.len, &op->value.lval, &op->value.dval, 1)))
				{
					case IS_DOUBLE:
					case IS_LONG:
						break;

					default:
						op->value.lval = strtol(op->value.str.val, NULL, 10);
						op->type = IS_LONG;
						break;
				}
				STR_FREE(strval);
				break;
			}

		case IS_BOOL:		op->type = IS_LONG;
							break;

		case IS_RESOURCE:	_zend_list_delete(op->value.lval, tsrm_ls);
							op->type = IS_LONG;
							break;

		case IS_NULL:		op->type = IS_LONG;
							op->value.lval = 0;
							break;
	}
}

// copied from zend_operators.c and beautified
static void convert_scalar_to_array(zval *op, int type)
{
	zval *entry;
	
	ALLOC_ZVAL(entry);
	*entry = *op;
	INIT_PZVAL(entry);
	
	switch (type)
	{
		case IS_ARRAY:
			ALLOC_HASHTABLE(op->value.ht);
			zend_hash_init(op->value.ht, 0, NULL, ZVAL_PTR_DTOR, 0);
			zend_hash_index_update(op->value.ht, 0, (void *) &entry, sizeof(zval *), NULL);
			op->type = IS_ARRAY;
			break;

		case IS_OBJECT:
			ALLOC_HASHTABLE(Z_OBJPROP_P(op));
			zend_hash_init(Z_OBJPROP_P(op), 0, NULL, ZVAL_PTR_DTOR, 0);
			zend_hash_update(Z_OBJPROP_P(op), "scalar", sizeof("scalar"), (void *)&entry, sizeof(zval *), NULL);
			Z_OBJCE_P(op) = &zend_standard_class_def;
			Z_TYPE_P(op) = IS_OBJECT;
			break;
	}
}

// copied from zend_operators.c, slightly modified and beautified
ZEND_API void convert_to_array(zval *op)
{
	TSRMLS_FETCH();

	switch(op->type) 
	{
		case IS_ARRAY:	return;

		/* OBJECTS_OPTIMIZE */
		case IS_OBJECT:
			op->type = IS_ARRAY;
			op->value.ht = Z_OBJPROP_P(op);
			return;

		case IS_NULL:	ALLOC_HASHTABLE(op->value.ht);
						zend_hash_init(op->value.ht, 0, NULL, ZVAL_PTR_DTOR, 0);
						op->type = IS_ARRAY;
						break;

		default:		convert_scalar_to_array(op, IS_ARRAY);
						break;
	}
}

// copied from zend_operators.c and beautified
ZEND_API void convert_to_boolean(zval *op)
{
	char *strval;
	int tmp;

	switch (op->type)
	{
		case IS_BOOL:
			break;

		case IS_NULL:
			op->value.lval = 0;
			break;

		case IS_RESOURCE:
			{
				TSRMLS_FETCH();
				zend_list_delete(op->value.lval);
			}
			/* break missing intentionally */

		case IS_LONG:
			op->value.lval = (op->value.lval ? 1 : 0);
			break;

		case IS_DOUBLE:
			op->value.lval = (op->value.dval ? 1 : 0);
			break;

		case IS_STRING:
			strval = op->value.str.val;

			if (op->value.str.len == 0 || (op->value.str.len == 1 && op->value.str.val[0] == '0')) op->value.lval = 0;
			else op->value.lval = 1;
			STR_FREE(strval);
			break;

		case IS_ARRAY:
			tmp = (zend_hash_num_elements(op->value.ht) ? 1 : 0);
			zval_dtor(op);
			op->value.lval = tmp;
			break;

		case IS_OBJECT:
			tmp = (zend_hash_num_elements(Z_OBJPROP_P(op)) ? 1 : 0);
			zval_dtor(op);
			op->value.lval = tmp;
			break;

		default:
			zval_dtor(op);
			op->value.lval = 0;
			break;
	}
	op->type = IS_BOOL;
}

// copied from zend_operators.c and beautified
ZEND_API void convert_to_double(zval *op)
{
	char *strval;
	double tmp;

	switch (op->type)
	{
		case IS_NULL:
			op->value.dval = 0.0;
			break;

		case IS_RESOURCE:
			{
				TSRMLS_FETCH();
				zend_list_delete(op->value.lval);
			}
			/* break missing intentionally */

		case IS_BOOL:
		case IS_LONG:
			op->value.dval = (double)op->value.lval;
			break;

		case IS_DOUBLE:
			break;

		case IS_STRING:
			strval = op->value.str.val;
			op->value.dval = strtod(strval, NULL);
			STR_FREE(strval);
			break;

		case IS_ARRAY:
			tmp = (zend_hash_num_elements(op->value.ht) ? 1 : 0);
			zval_dtor(op);
			op->value.dval = tmp;
			break;

		case IS_OBJECT:
			tmp = (zend_hash_num_elements(Z_OBJPROP_P(op))? 1 : 0);
			zval_dtor(op);
			op->value.dval = tmp;
			break;

		default:
			zend_error(E_WARNING, "Cannot convert to real value (type=%d)", op->type);
			zval_dtor(op);
			op->value.dval = 0;
			break;
	}
	op->type = IS_DOUBLE;
}

// copied from zend_operators.c and beautified
ZEND_API void convert_to_long(zval *op)
{
	convert_to_long_base(op, 10);
}

// copied from zend_operators.c and beautified
ZEND_API void convert_to_long_base(zval *op, int base)
{
	char *strval;
	long tmp;

	switch (op->type)
	{
		case IS_NULL:
			op->value.lval = 0;
			break;
		case IS_RESOURCE:
			{
				TSRMLS_FETCH();
				zend_list_delete(op->value.lval);
			}
			/* break missing intentionally */

		case IS_BOOL:
		case IS_LONG:
			break;

		case IS_DOUBLE:
			DVAL_TO_LVAL(op->value.dval, op->value.lval);
			break;

		case IS_STRING:
			strval = op->value.str.val;
			op->value.lval = strtol(strval, NULL, base);
			STR_FREE(strval);
			break;

		case IS_ARRAY:
			tmp = (zend_hash_num_elements(op->value.ht) ? 1 : 0);
			zval_dtor(op);
			op->value.lval = tmp;
			break;

		case IS_OBJECT:
			tmp = (zend_hash_num_elements(Z_OBJPROP_P(op))?1:0);
			zval_dtor(op);
			op->value.lval = tmp;
			break;

		default:
			zend_error(E_WARNING, "Cannot convert to ordinal value");
			zval_dtor(op);
			op->value.lval = 0;
			break;
	}

	op->type = IS_LONG;
}

// copied from zend_operators.c and beautified
ZEND_API void convert_to_null(zval *op)
{
	zval_dtor(op);
	op->type = IS_NULL;
}

// copied from zend_operators.c, slightly modified and beautified
ZEND_API void convert_to_object(zval *op)
{
	switch (op->type)
	{
		/* OBJECTS_FIXME */
		case IS_ARRAY:
			Z_TYPE_P(op) = IS_OBJECT;
			Z_OBJPROP_P(op) = op->value.ht;
			Z_OBJCE_P(op) = &zend_standard_class_def;
			return;

		case IS_OBJECT:
			return;

/* OBJECTS_FIXME */
		case IS_NULL:
			ALLOC_HASHTABLE(Z_OBJPROP_P(op));
			zend_hash_init(Z_OBJPROP_P(op), 0, NULL, ZVAL_PTR_DTOR, 0);
			Z_OBJCE_P(op) = &zend_standard_class_def;
			Z_TYPE_P(op) = IS_OBJECT;
			break;

		default:
			convert_scalar_to_array(op, IS_OBJECT);
			break;
	}
}

// copied from zend_operators.c, slightly modified and beautified
ZEND_API void _convert_to_string(zval *op ZEND_FILE_LINE_DC)
{
	long lval;
	double dval;
	TSRMLS_FETCH();

	switch (op->type)
	{
		case IS_NULL:	op->value.str.val = empty_string;
						op->value.str.len = 0;
						break;

		case IS_STRING:	break;

		case IS_BOOL:	if (op->value.lval)
						{
							op->value.str.val = estrndup_rel("1", 1);
							op->value.str.len = 1;
						} 
						else
						{
							op->value.str.val = empty_string;
							op->value.str.len = 0;
						}
						break;

		case IS_RESOURCE: 
						{
							long tmp = op->value.lval;

							_zend_list_delete(op->value.lval, tsrm_ls);
							op->value.str.val = (char *)emalloc(sizeof("Resource id #") - 1 + MAX_LENGTH_OF_LONG);
							op->value.str.len = sprintf(op->value.str.val, "Resource id #%ld", tmp);
							break;
						}

		case IS_LONG:	lval = op->value.lval;
						op->value.str.val = (char *) emalloc_rel(MAX_LENGTH_OF_LONG + 1);
						op->value.str.len = zend_sprintf(op->value.str.val, "%ld", lval);  /* SAFE */
						break;

		case IS_DOUBLE: {
							dval = op->value.dval;
							op->value.str.val = (char *)emalloc_rel(MAX_LENGTH_OF_DOUBLE + EG_precision + 1);
							op->value.str.len = zend_sprintf(op->value.str.val, "%.*G", (int)EG_precision, dval);  /* SAFE */
							/* %G already handles removing trailing zeros from the fractional part, yay */
							break;
						}

		case IS_ARRAY:	zval_dtor(op);
						op->value.str.val = estrndup_rel("Array", sizeof("Array") - 1);
						op->value.str.len = sizeof("Array") - 1;
						zend_error(E_NOTICE, "Array to string conversion");
						break;

		case IS_OBJECT:	zval_dtor(op);
						op->value.str.val = estrndup_rel("Object", sizeof("Object") - 1);
						op->value.str.len = sizeof("Object") - 1;
						zend_error(E_NOTICE, "Object to string conversion");
						break;

		default:		zval_dtor(op);
						ZVAL_BOOL(op, 0);
						break;
	}
	op->type = IS_STRING;
}
#elif defined(PHP5TS)

#define convert_object_to_type(op, ctype, conv_func)										\
	if (Z_OBJ_HT_P(op)->cast_object) {														\
		zval dst;																			\
		if (Z_OBJ_HT_P(op)->cast_object(op, &dst, ctype TSRMLS_CC) == FAILURE) {			\
			zend_error(E_RECOVERABLE_ERROR, 												\
			"Object of class %s could not be converted to %s", Z_OBJCE_P(op)->name,			\
			zend_get_type_by_const(ctype));													\
		} else {																			\
			zval_dtor(op);																	\
			Z_TYPE_P(op) = ctype;															\
			op->value = dst.value;															\
		}																					\
	} else {																				\
		if(Z_OBJ_HT_P(op)->get) {															\
			zval *newop = Z_OBJ_HT_P(op)->get(op TSRMLS_CC);								\
			if(Z_TYPE_P(newop) != IS_OBJECT) {												\
				/* for safety - avoid loop */												\
				zval_dtor(op);																\
				*op = *newop;																\
				FREE_ZVAL(newop);															\
				conv_func(op);																\
			}																				\
		}																					\
	}

/* Argument parsing API -- andrei */

ZEND_API char *zend_get_type_by_const(int type) 
{
	switch(type) {
		case IS_BOOL:
			return "boolean";
		case IS_LONG:
			return "integer";
		case IS_DOUBLE:
			return "double";
		case IS_STRING:
			return "string";
		case IS_OBJECT:
			return "object";
		case IS_RESOURCE:
			return "resource";
		case IS_NULL:
			return "null";
		case IS_ARRAY:
			return "array";
		default:
			return "unknown";
	}
}

ZEND_API void convert_to_long(zval *op)
{
	if ((op)->type != IS_LONG) {
		convert_to_long_base(op, 10);
	}
}

ZEND_API void convert_to_long_base(zval *op, int base)
{
	char *strval;
	long tmp;

	switch (op->type) {
		case IS_NULL:
			op->value.lval = 0;
			break;
		case IS_RESOURCE: {
				TSRMLS_FETCH();

				zend_list_delete(op->value.lval);
			}
			/* break missing intentionally */
		case IS_BOOL:
		case IS_LONG:
			break;
		case IS_DOUBLE:
			DVAL_TO_LVAL(op->value.dval, op->value.lval);
			break;
		case IS_STRING:
			strval = op->value.str.val;
			op->value.lval = strtol(strval, NULL, base);
			STR_FREE(strval);
			break;
		case IS_ARRAY:
			tmp = (zend_hash_num_elements(op->value.ht)?1:0);
			zval_dtor(op);
			op->value.lval = tmp;
			break;
		case IS_OBJECT:
			{
				int retval = 1;
				TSRMLS_FETCH();

				convert_object_to_type(op, IS_LONG, convert_to_long);

				if (op->type == IS_LONG) {
					return;
				}

				if (EG(ze1_compatibility_mode)) {
					HashTable *ht = Z_OBJPROP_P(op);
					if (ht) {
						retval = (zend_hash_num_elements(ht)?1:0);
					}
				} else {
					zend_error(E_NOTICE, "Object of class %s could not be converted to int", Z_OBJCE_P(op)->name);
				}
				zval_dtor(op);
				ZVAL_LONG(op, retval);
				return;
			}
		default:
			zend_error(E_WARNING, "Cannot convert to ordinal value");
			zval_dtor(op);
			op->value.lval = 0;
			break;
	}

	op->type = IS_LONG;
}


ZEND_API void convert_to_double(zval *op)
{
	char *strval;
	double tmp;

	switch (op->type) {
		case IS_NULL:
			op->value.dval = 0.0;
			break;
		case IS_RESOURCE: {
				TSRMLS_FETCH();

				zend_list_delete(op->value.lval);
			}
			/* break missing intentionally */
		case IS_BOOL:
		case IS_LONG:
			op->value.dval = (double) op->value.lval;
			break;
		case IS_DOUBLE:
			break;
		case IS_STRING:
			strval = op->value.str.val;

			op->value.dval = zend_strtod(strval, NULL);
			STR_FREE(strval);
			break;
		case IS_ARRAY:
			tmp = (zend_hash_num_elements(op->value.ht)?1:0);
			zval_dtor(op);
			op->value.dval = tmp;
			break;
		case IS_OBJECT:
			{
				double retval = 1.0;
				TSRMLS_FETCH();
				
				convert_object_to_type(op, IS_DOUBLE, convert_to_double);

				if (op->type == IS_DOUBLE) {
					return;
				}

				if (EG(ze1_compatibility_mode)) {
					HashTable *ht = Z_OBJPROP_P(op);
					if (ht) {
						retval = (zend_hash_num_elements(ht)?1.0:0.0);
					}
				} else {
					zend_error(E_NOTICE, "Object of class %s could not be converted to double", Z_OBJCE_P(op)->name);
				}

				zval_dtor(op);
				ZVAL_DOUBLE(op, retval);
				break;
			}	
		default:
			zend_error(E_WARNING, "Cannot convert to real value (type=%d)", op->type);
			zval_dtor(op);
			op->value.dval = 0;
			break;
	}
	op->type = IS_DOUBLE;
}


ZEND_API void convert_to_null(zval *op)
{
	if (Z_TYPE_P(op) == IS_OBJECT) {
		if (Z_OBJ_HT_P(op)->cast_object) {
			zval *org;
			TSRMLS_FETCH();

			ALLOC_ZVAL(org);
			*org = *op;
			if (Z_OBJ_HT_P(op)->cast_object(org, op, IS_NULL TSRMLS_CC) == SUCCESS) {
				zval_dtor(org);
				return;
			}
			*op = *org;
			FREE_ZVAL(org);
		}
	}

	zval_dtor(op);
	Z_TYPE_P(op) = IS_NULL;
}


ZEND_API void convert_to_boolean(zval *op)
{
	char *strval;
	int tmp;

	switch (op->type) {
		case IS_BOOL:
			break;
		case IS_NULL:
			op->value.lval = 0;
			break;
		case IS_RESOURCE: {
				TSRMLS_FETCH();

				zend_list_delete(op->value.lval);
			}
			/* break missing intentionally */
		case IS_LONG:
			op->value.lval = (op->value.lval ? 1 : 0);
			break;
		case IS_DOUBLE:
			op->value.lval = (op->value.dval ? 1 : 0);
			break;
		case IS_STRING:
			strval = op->value.str.val;

			if (op->value.str.len == 0
				|| (op->value.str.len==1 && op->value.str.val[0]=='0')) {
				op->value.lval = 0;
			} else {
				op->value.lval = 1;
			}
			STR_FREE(strval);
			break;
		case IS_ARRAY:
			tmp = (zend_hash_num_elements(op->value.ht)?1:0);
			zval_dtor(op);
			op->value.lval = tmp;
			break;
		case IS_OBJECT:
			{
				zend_bool retval = 1;
				TSRMLS_FETCH();

				convert_object_to_type(op, IS_BOOL, convert_to_boolean);

				if (op->type == IS_BOOL) {
					return;
				}
					
				if (EG(ze1_compatibility_mode)) {
					HashTable *ht = Z_OBJPROP_P(op);
					if (ht) {
						retval = (zend_hash_num_elements(ht)?1:0);
					}
				}
				
				zval_dtor(op);
				ZVAL_BOOL(op, retval);
				break;
			}
		default:
			zval_dtor(op);
			op->value.lval = 0;
			break;
	}
	op->type = IS_BOOL;
}

ZEND_API void _convert_to_string(zval *op ZEND_FILE_LINE_DC)
{
	long lval;
	double dval;

	switch (op->type) {
		case IS_NULL:
			op->value.str.val = STR_EMPTY_ALLOC();
			op->value.str.len = 0;
			break;
		case IS_STRING:
			break;
		case IS_BOOL:
			if (op->value.lval) {
				op->value.str.val = estrndup_rel("1", 1);
				op->value.str.len = 1;
			} else {
				op->value.str.val = STR_EMPTY_ALLOC();
				op->value.str.len = 0;
			}
			break;
		case IS_RESOURCE: {
			long tmp = op->value.lval;
			TSRMLS_FETCH();

			zend_list_delete(op->value.lval);
			op->value.str.len = zend_spprintf(&op->value.str.val, 0, "Resource id #%ld", tmp);
			break;
		}
		case IS_LONG:
			lval = op->value.lval;

			op->value.str.len = zend_spprintf(&op->value.str.val, 0, "%ld", lval);  /* SAFE */
			break;
		case IS_DOUBLE: {
			TSRMLS_FETCH();
			dval = op->value.dval;
			op->value.str.len = zend_spprintf(&op->value.str.val, 0, "%.*G", (int) EG(precision), dval);  /* SAFE */
			/* %G already handles removing trailing zeros from the fractional part, yay */
			break;
		}
		case IS_ARRAY:
			zend_error(E_NOTICE, "Array to string conversion");
			zval_dtor(op);
			op->value.str.val = estrndup_rel("Array", sizeof("Array")-1);
			op->value.str.len = sizeof("Array")-1;
			break;
		case IS_OBJECT: {
			TSRMLS_FETCH();
			
			convert_object_to_type(op, IS_STRING, convert_to_string);

			if (op->type == IS_STRING) {
				return;
			}

			zend_error(E_NOTICE, "Object of class %s to string conversion", Z_OBJCE_P(op)->name);
			zval_dtor(op);
			op->value.str.val = estrndup_rel("Object", sizeof("Object")-1);
			op->value.str.len = sizeof("Object")-1;
			break;
		}
		default:
			zval_dtor(op);
			ZVAL_BOOL(op, 0);
			break;
	}
	op->type = IS_STRING;
}


static void convert_scalar_to_array(zval *op, int type)
{
	zval *entry;
	
	ALLOC_ZVAL(entry);
	*entry = *op;
	INIT_PZVAL(entry);
	
	switch (type) {
		case IS_ARRAY:
			ALLOC_HASHTABLE(op->value.ht);
			zend_hash_init(op->value.ht, 0, NULL, ZVAL_PTR_DTOR, 0);
			zend_hash_index_update(op->value.ht, 0, (void *) &entry, sizeof(zval *), NULL);
			op->type = IS_ARRAY;
			break;
		case IS_OBJECT:
			{
				/* OBJECTS_OPTIMIZE */
				TSRMLS_FETCH();

				object_init(op);
				zend_hash_update(Z_OBJPROP_P(op), "scalar", sizeof("scalar"), (void *) &entry, sizeof(zval *), NULL);
			}
			break;
	}
}


ZEND_API void convert_to_array(zval *op)
{
	TSRMLS_FETCH();

	switch (op->type) {
		case IS_ARRAY:
			return;
			break;
/* OBJECTS_OPTIMIZE */
		case IS_OBJECT:
			{
				zval *tmp;
				HashTable *ht;

				ALLOC_HASHTABLE(ht);
				zend_hash_init(ht, 0, NULL, ZVAL_PTR_DTOR, 0);
				if (Z_OBJ_HT_P(op)->get_properties) {
					HashTable *obj_ht = Z_OBJ_HT_P(op)->get_properties(op TSRMLS_CC);
					if(obj_ht) {
						zend_hash_copy(ht, obj_ht, (copy_ctor_func_t) zval_add_ref, (void *) &tmp, sizeof(zval *));
					}
				} else {
					convert_object_to_type(op, IS_ARRAY, convert_to_array);

					if (op->type == IS_ARRAY) {
						zend_hash_destroy(ht);
						FREE_HASHTABLE(ht);
						return;
					}
				}
				zval_dtor(op);
				op->type = IS_ARRAY;
				op->value.ht = ht;
			}
			return;
		case IS_NULL:
			ALLOC_HASHTABLE(op->value.ht);
			zend_hash_init(op->value.ht, 0, NULL, ZVAL_PTR_DTOR, 0);
			op->type = IS_ARRAY;
			break;
		default:
			convert_scalar_to_array(op, IS_ARRAY);
			break;
	}
}


ZEND_API void convert_to_object(zval *op)
{
	switch (op->type) {
		case IS_ARRAY:
			{
				/* OBJECTS_OPTIMIZE */
				TSRMLS_FETCH();

				object_and_properties_init(op, &zend_standard_class_def, op->value.ht);
				return;
				break;
			}
		case IS_OBJECT:
			return;
		case IS_NULL:
			{
				/* OBJECTS_OPTIMIZE */
				TSRMLS_FETCH();

				object_init(op);
				break;
			}
		default:
			convert_scalar_to_array(op, IS_OBJECT);
			break;
	}
}

ZEND_API void convert_scalar_to_number(zval *op TSRMLS_DC)
{
	switch (op->type) {
		case IS_STRING:
			{
				char *strval;

				strval = op->value.str.val;
				if ((op->type=is_numeric_string(strval, op->value.str.len, &op->value.lval, &op->value.dval, 1)) == 0) {
					op->value.lval = 0;
					op->type = IS_LONG;
				}
				STR_FREE(strval);
				break;
			}
		case IS_BOOL:
			op->type = IS_LONG;
			break;
		case IS_RESOURCE:
			zend_list_delete(op->value.lval);
			op->type = IS_LONG;
			break;
		case IS_OBJECT:
			convert_to_long_base(op, 10);
			break;
		case IS_NULL:
			op->type = IS_LONG;
			op->value.lval = 0;
			break;
	}
}
#endif
// copied from zend_strtod.c
ZEND_API double zend_hex_strtod(const char *str, char **endptr)
{
	const char *s = str;
	char c;
	int any = 0;
	double value = 0;

	if (*s == '0' && (s[1] == 'x' || s[1] == 'X')) {
		s += 2;
	}

	while ((c = *s++)) {
		if (c >= '0' && c <= '9') {
			c -= '0';
		} else if (c >= 'A' && c <= 'F') {
			c -= 'A' - 10;
		} else if (c >= 'a' && c <= 'f') {
			c -= 'a' - 10;
		} else {
			break;
		}

		any = 1;
		value = value * 16 + c;
	}

	if (endptr != NULL) {
		*endptr = (char *)(any ? s - 1 : str);
	}

	return value;
}

ZEND_API double zend_oct_strtod(const char *str, char **endptr)
{
	const char *s = str;
	char c;
	double value = 0;
	int any = 0;

	/* skip leading zero */
	s++;

	while ((c = *s++)) {
		if (c < '0' || c > '7') {
			/* break and return the current value if the number is not well-formed
			* that's what Linux strtol() does 
			*/
			break;
		}
		value = value * 8 + c - '0';
		any = 1;
	}

	if (endptr != NULL) {
		*endptr = (char *)(any ? s - 1 : str);
	}

	return value;
}


// copied from zend_operators.c and beautified
ZEND_API long zend_atol(const char *str, int str_len)
{
	long retval;

	if (!str_len) {
		str_len = strlen(str);
	}
	retval = strtol(str, NULL, 0);
	if (str_len>0) {
		switch (str[str_len-1]) {
			case 'g':
			case 'G':
				retval *= 1024;
				/* break intentionally missing */
			case 'm':
			case 'M':
				retval *= 1024;
				/* break intentionally missing */
			case 'k':
			case 'K':
				retval *= 1024;
				break;
		}
	}
	return retval;
}
ZEND_API int zend_atoi(const char *str, int str_len)
{
	int retval;

	if (!str_len) str_len = strlen(str);

	retval = strtol(str, NULL, 0);
	if (str_len > 0)
	{
		switch (str[str_len-1])
		{
			case 'k':
			case 'K':	retval *= 1024;
						break;
			case 'm':
			case 'M':	retval *= 1048576;
						break;
		}
	}
	return retval;
}

// copied from zend_operators.c and beautified
ZEND_API void zend_str_tolower(char *str, unsigned int length)
{
	register char *p = str, *end = p + length;
	
	while (p < end)
	{
		if (*p >= 'A' && *p <= 'Z') *p = (*p)+32;
		p++;
	}
}

// copied from zend_operators.c and beautified
ZEND_API char *zend_str_tolower_copy(char *dest, const char *source, unsigned int length)
{
	register unsigned char *str = (unsigned char *)source;
	register unsigned char *result = (unsigned char *)dest;
	register unsigned char *end = str + length;

	while (str < end) *result++ = tolower((int)*str++);
	*result = *end;

	return dest;
}

// copied from zend_operators.c and beautified
ZEND_API int zend_binary_strcmp(char *s1, uint len1, char *s2, uint len2)
{
	int retval;
	
	retval = memcmp(s1, s2, MIN(len1, len2));
	if (!retval) return (len1 - len2);
	else return retval;
}

// copied from zend_operators.c and beautified
ZEND_API int zend_binary_strncmp(char *s1, uint len1, char *s2, uint len2, uint length)
{
	int retval;
	
	retval = memcmp(s1, s2, MIN(length, MIN(len1, len2)));
	if (!retval) return (MIN(length, len1) - MIN(length, len2));
	else return retval;
}

// copied from zend_operators.c and beautified
ZEND_API int zend_binary_strcasecmp(char *s1, uint len1, char *s2, uint len2)
{
	int len;
	int c1, c2;

	len = MIN(len1, len2);

	while (len--)
	{
		c1 = tolower(*s1++);
		c2 = tolower(*s2++);
		if (c1 != c2) return c1 - c2;
	}
	return len1 - len2;
}

// copied from zend_operators.c and beautified
ZEND_API int zend_binary_strncasecmp(char *s1, uint len1, char *s2, uint len2, uint length)
{
	int len;
	int c1, c2;

	len = MIN(length, MIN(len1, len2));

	while (len--)
	{
		c1 = tolower(*s1++);
		c2 = tolower(*s2++);
		if (c1 != c2) return c1 - c2;
	}
	return MIN(length, len1) - MIN(length, len2);
}

// copied from zend_operators.c and beautified
ZEND_API int zend_binary_zval_strcmp(zval *s1, zval *s2)
{
	return zend_binary_strcmp(s1->value.str.val, s1->value.str.len, s2->value.str.val, s2->value.str.len);
}

// copied from zend_operators.c and beautified
ZEND_API int zend_binary_zval_strncmp(zval *s1, zval *s2, zval *s3)
{
	return zend_binary_strncmp(s1->value.str.val, s1->value.str.len, s2->value.str.val, s2->value.str.len, s3->value.lval);
}

// copied from zend_operators.c and beautified
ZEND_API int zend_binary_zval_strcasecmp(zval *s1, zval *s2)
{
	return zend_binary_strcasecmp(s1->value.str.val, s1->value.str.len, s2->value.str.val, s2->value.str.len);
}

// copied from zend_operators.c and beautified
ZEND_API int zend_binary_zval_strncasecmp(zval *s1, zval *s2, zval *s3)
{
	return zend_binary_strncasecmp(s1->value.str.val, s1->value.str.len, s2->value.str.val, s2->value.str.len, s3->value.lval);
}

// copied from zend_operators.c, slightly modified and beautified
ZEND_API void zendi_smart_strcmp(zval *result, zval *s1, zval *s2)
{
	int ret1, ret2;
	long lval1, lval2;
	double dval1, dval2;
	
	if ((ret1 = is_numeric_string(s1->value.str.val, s1->value.str.len, &lval1, &dval1, 0)) &&
		(ret2 = is_numeric_string(s2->value.str.val, s2->value.str.len, &lval2, &dval2, 0)))
	{
		if (ret1 == IS_DOUBLE || ret2 == IS_DOUBLE)
		{
			if (ret1 != IS_DOUBLE) dval1 = strtod(s1->value.str.val, NULL);
			else if (ret2 != IS_DOUBLE) dval2 = strtod(s2->value.str.val, NULL);
	
			result->value.dval = dval1 - dval2;
			result->value.lval = ZEND_NORMALIZE_BOOL(result->value.dval);
			result->type = IS_LONG;
		}
		else
		{ 
			/* they both have to be long's */
			result->value.lval = lval1 - lval2;
			result->value.lval = ZEND_NORMALIZE_BOOL(result->value.lval);
			result->type = IS_LONG;
		}
	}
	else
	{
		result->value.lval = zend_binary_zval_strcmp(s1, s2);
		result->value.lval = ZEND_NORMALIZE_BOOL(result->value.lval);
		result->type = IS_LONG;
	}
	return;	
}

// copied from zend_operators.c, slightly modified and beautified
int hash_zval_compare_function(const zval **z1, const zval **z2 TSRMLS_DC)
{
	zval result;

	if (compare_function(&result, (zval *)*z1, (zval *)*z2 TSRMLS_CC) == FAILURE) return 1;
	return result.value.lval;
}

// copied from zend_operators.c and beautified
ZEND_API void zend_compare_symbol_tables(zval *result, HashTable *ht1, HashTable *ht2 TSRMLS_DC)
{
	result->type = IS_LONG;
	result->value.lval = zend_hash_compare(ht1, ht2, (compare_func_t)hash_zval_compare_function, 0 TSRMLS_CC);
}

// copied from zend_operators.c and beautified
ZEND_API void zend_compare_arrays(zval *result, zval *a1, zval *a2 TSRMLS_DC)
{
	zend_compare_symbol_tables(result, a1->value.ht, a2->value.ht TSRMLS_CC);
}

// copied from zend_operators.c
ZEND_API int zend_compare_symbol_tables_i(HashTable *ht1, HashTable *ht2 TSRMLS_DC)
{
	return zend_hash_compare(ht1, ht2, (compare_func_t) hash_zval_compare_function, 0 TSRMLS_CC);
}

// copied from zend_operators.c and beautified
ZEND_API void zend_compare_objects(zval *result, zval *o1, zval *o2 TSRMLS_DC)
{
/* OBJECTS_FIXME */
	if (Z_OBJCE_P(o1) != Z_OBJCE_P(o2))
	{
		result->value.lval = 1;	/* Comparing objects of different types is pretty much meaningless */
		result->type = IS_LONG;
		return;
	}
	zend_compare_symbol_tables(result, Z_OBJPROP_P(o1), Z_OBJPROP_P(o2) TSRMLS_CC);
}

// copied from zend_operators.c and beautified
ZEND_API int string_compare_function(zval *result, zval *op1, zval *op2 TSRMLS_DC)
{
	zval op1_copy, op2_copy;
	int use_copy1, use_copy2;

	zend_make_printable_zval(op1, &op1_copy, &use_copy1);
	zend_make_printable_zval(op2, &op2_copy, &use_copy2);

	if (use_copy1) op1 = &op1_copy;
	if (use_copy2) op2 = &op2_copy;

	result->value.lval = zend_binary_zval_strcmp(op1, op2);
	result->type = IS_LONG;

	if (use_copy1) zval_dtor(op1);
	if (use_copy2) zval_dtor(op2);

	return SUCCESS;
}

// copied from zend_operators.c and beautified
ZEND_API int string_locale_compare_function(zval *result, zval *op1, zval *op2 TSRMLS_DC)
{
	zval op1_copy, op2_copy;
	int use_copy1, use_copy2;

	zend_make_printable_zval(op1, &op1_copy, &use_copy1);
	zend_make_printable_zval(op2, &op2_copy, &use_copy2);

	if (use_copy1) op1 = &op1_copy;
	if (use_copy2) op2 = &op2_copy;

	result->value.lval = strcoll(op1->value.str.val, op2->value.str.val);
	result->type = IS_LONG;

	if (use_copy1) zval_dtor(op1);
	if (use_copy2) zval_dtor(op2);

	return SUCCESS;
}

// copied from zend_operators.c and beautified
ZEND_API int numeric_compare_function(zval *result, zval *op1, zval *op2 TSRMLS_DC)
{
	zval op1_copy, op2_copy;

	op1_copy = *op1;
	zval_copy_ctor(&op1_copy);

	op2_copy = *op2;
	zval_copy_ctor(&op2_copy);

	convert_to_double(&op1_copy);
	convert_to_double(&op2_copy);

	result->value.lval = ZEND_NORMALIZE_BOOL(op1_copy.value.dval - op2_copy.value.dval);
	result->type = IS_LONG;

	return SUCCESS;
}

// copied from zend_operators.c and beautified
ZEND_API int compare_function(zval *result, zval *op1, zval *op2 TSRMLS_DC)
{
	zval op1_copy, op2_copy;

	if ((op1->type == IS_NULL && op2->type == IS_STRING) ||
		(op2->type == IS_NULL && op1->type == IS_STRING))
	{
		if (op1->type == IS_NULL)
		{
			result->type = IS_LONG;
			result->value.lval = zend_binary_strcmp("", 0, op2->value.str.val, op2->value.str.len);
			return SUCCESS;
		}
		else
		{
			result->type = IS_LONG;
			result->value.lval = zend_binary_strcmp(op1->value.str.val, op1->value.str.len, "", 0);
			return SUCCESS;
		}
	}
		
	if (op1->type == IS_STRING && op2->type == IS_STRING)
	{
		zendi_smart_strcmp(result, op1, op2);
		return SUCCESS;
	}
	
	if (op1->type == IS_BOOL || op2->type == IS_BOOL ||
		op1->type == IS_NULL || op2->type == IS_NULL)
	{
		zendi_convert_to_boolean(op1, op1_copy, result);
		zendi_convert_to_boolean(op2, op2_copy, result);
		result->type = IS_LONG;
		result->value.lval = ZEND_NORMALIZE_BOOL(op1->value.lval - op2->value.lval);
		return SUCCESS;
	}

	zendi_convert_scalar_to_number(op1, op1_copy, result);
	zendi_convert_scalar_to_number(op2, op2_copy, result);

	if (op1->type == IS_LONG && op2->type == IS_LONG) {
		result->type = IS_LONG;
		result->value.lval = op1->value.lval > op2->value.lval ? 1 : (op1->value.lval < op2->value.lval ? -1 : 0);
		return SUCCESS;
	}
	if ((op1->type == IS_DOUBLE || op1->type == IS_LONG) &&
		(op2->type == IS_DOUBLE || op2->type == IS_LONG))
	{
		result->value.dval = (op1->type == IS_LONG ? (double) op1->value.lval : op1->value.dval) - 
			(op2->type == IS_LONG ? (double) op2->value.lval : op2->value.dval);
		result->value.lval = ZEND_NORMALIZE_BOOL(result->value.dval);
		result->type = IS_LONG;
		return SUCCESS;
	}
	if (op1->type == IS_ARRAY && op2->type == IS_ARRAY)
	{
		zend_compare_arrays(result, op1, op2 TSRMLS_CC);
		return SUCCESS;
	}

	if (op1->type == IS_OBJECT && op2->type == IS_OBJECT)
	{
		zend_compare_objects(result, op1, op2 TSRMLS_CC);
		return SUCCESS;
	}

	if (op1->type == IS_ARRAY)
	{
		result->value.lval = 1;
		result->type = IS_LONG;
		return SUCCESS;
	}
	if (op2->type == IS_ARRAY)
	{
		result->value.lval = -1;
		result->type = IS_LONG;
		return SUCCESS;
	}
	if (op1->type == IS_OBJECT)
	{
		result->value.lval = 1;
		result->type = IS_LONG;
		return SUCCESS;
	}
	if (op2->type == IS_OBJECT)
	{
		result->value.lval = -1;
		result->type = IS_LONG;
		return SUCCESS;
	}

	ZVAL_BOOL(result, 0);
	return FAILURE;
}

// copied from zend_operators.c and beautified
ZEND_API void zend_make_printable_zval(zval *expr, zval *expr_copy, int *use_copy)
{
	if (expr->type == IS_STRING)
	{
		*use_copy = 0;
		return;
	}
	switch (expr->type)
	{
		case IS_NULL:
			expr_copy->value.str.len = 0;
			expr_copy->value.str.val = empty_string;
			break;

		case IS_BOOL:
			if (expr->value.lval)
			{
				expr_copy->value.str.len = 1;
				expr_copy->value.str.val = estrndup("1", 1);
			}
			else
			{
				expr_copy->value.str.len = 0;
				expr_copy->value.str.val = empty_string;
			}
			break;

		case IS_RESOURCE:
			expr_copy->value.str.val = (char *)emalloc(sizeof("Resource id #") -1 + MAX_LENGTH_OF_LONG);
			expr_copy->value.str.len = sprintf(expr_copy->value.str.val, "Resource id #%ld", expr->value.lval);
			break;

		case IS_ARRAY:
			expr_copy->value.str.len = sizeof("Array") - 1;
			expr_copy->value.str.val = estrndup("Array", expr_copy->value.str.len);
			break;

		case IS_OBJECT:
			expr_copy->value.str.len = sizeof("Object") - 1;
			expr_copy->value.str.val = estrndup("Object", expr_copy->value.str.len);
			break;

		case IS_DOUBLE: 
			*expr_copy = *expr;
			zval_copy_ctor(expr_copy);
			zend_locale_sprintf_double(expr_copy ZEND_FILE_LINE_CC);
			break;	

		default:
			*expr_copy = *expr;
			zval_copy_ctor(expr_copy);
			convert_to_string(expr_copy);
			break;
	}
	expr_copy->type = IS_STRING;
	*use_copy = 1;
}

// copied from zend_operators.c and beautified
#ifdef PHP4TS
ZEND_API void zend_locale_sprintf_double(zval *op ZEND_FILE_LINE_DC)
{
	double dval = op->value.dval;
	
	TSRMLS_FETCH();
	
	op->value.str.val = (char *)emalloc_rel(MAX_LENGTH_OF_DOUBLE + EG(precision) + 1);
	sprintf(op->value.str.val, "%.*G", (int)EG(precision), dval);
	op->value.str.len = strlen(op->value.str.val);

	if (EG(float_separator)[0] != '.')
	{ 
		char *p = op->value.str.val;
		if ((p = strchr(p, '.'))) *p = EG(float_separator)[0];
	}
}
#elif defined(PHP5TS)
ZEND_API void zend_locale_sprintf_double(zval *op ZEND_FILE_LINE_DC)
{
	TSRMLS_FETCH();

	op->value.str.len = zend_spprintf(&op->value.str.val, 0, "%.*G", (int) EG(precision), (double)op->value.dval);
}
#endif

// copied from strlcpy.c and slightly modified
/*
 * Copyright (c) 1998 Todd C. Miller <Todd.Miller@courtesan.com>
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions
 * are met:
 * 1. Redistributions of source code must retain the above copyright
 *    notice, this list of conditions and the following disclaimer.
 * 2. Redistributions in binary form must reproduce the above copyright
 *    notice, this list of conditions and the following disclaimer in the
 *    documentation and/or other materials provided with the distribution.
 * 3. The name of the author may not be used to endorse or promote products
 *    derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED ``AS IS'' AND ANY EXPRESS OR IMPLIED WARRANTIES,
 * INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY
 * AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED.  IN NO EVENT SHALL
 * THE AUTHOR BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
 * EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
 * PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS;
 * OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY,
 * WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR
 * OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
 * ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

/*
 * Copy src to string dst of size siz.  At most siz-1 characters
 * will be copied.  Always NUL terminates (unless siz == 0).
 * Returns strlen(src); if retval >= siz, truncation occurred.
 */
ZEND_API size_t php_strlcpy(char *dst, const char *src, size_t siz)
{
	register char *d = dst;
	register const char *s = src;
	register size_t n = siz;

	/* Copy as many bytes as will fit */
	if (n != 0 && --n != 0) {
		do {
			if ((*d++ = *s++) == 0)
				break;
		} while (--n != 0);
	}

	/* Not enough room in dst, add NUL and traverse rest of src */
	if (n == 0) {
		if (siz != 0)
			*d = '\0';		/* NUL-terminate dst */
		while (*s++)
			;
	}

	return(s - src - 1);	/* count does not include NUL */
}

// copied from zend_operators.c and beautified
ZEND_API int bitwise_and_function(zval *result, zval *op1, zval *op2 TSRMLS_DC)
{
	zval op1_copy, op2_copy;
	
	if (op1->type == IS_STRING && op2->type == IS_STRING)
	{
		zval *longer, *shorter;
		char *result_str;
		int i, result_len;

		if (op1->value.str.len >= op2->value.str.len)
		{
			longer = op1;
			shorter = op2;
		}
		else
		{
			longer = op2;
			shorter = op1;
		}

		result->type = IS_STRING;
		result_len = shorter->value.str.len;
		result_str = estrndup(shorter->value.str.val, shorter->value.str.len);
		for (i = 0; i < shorter->value.str.len; i++) 
		{
			result_str[i] &= longer->value.str.val[i];
		}
		if (result == op1)
		{
			STR_FREE(result->value.str.val);
		}
		result->value.str.val = result_str;
		result->value.str.len = result_len;
		return SUCCESS;
	}
	
	zendi_convert_to_long(op1, op1_copy, result);
	zendi_convert_to_long(op2, op2_copy, result);

	result->type = IS_LONG;
	result->value.lval = op1->value.lval & op2->value.lval;
	return SUCCESS;
}

// copied from zend_operators.c and beautified
ZEND_API int bitwise_not_function(zval *result, zval *op1 TSRMLS_DC)
{
	zval op1_copy = *op1;
	
	op1 = &op1_copy;
	
	if (op1->type == IS_DOUBLE)
	{
		op1->value.lval = (long)op1->value.dval;
		op1->type = IS_LONG;
	}
	if (op1->type == IS_LONG)
	{
		result->value.lval = ~op1->value.lval;
		result->type = IS_LONG;
		return SUCCESS;
	}
	if (op1->type == IS_STRING)
	{
		int i;

		result->type = IS_STRING;
		result->value.str.val = estrndup(op1->value.str.val, op1->value.str.len);
		result->value.str.len = op1->value.str.len;
		for (i = 0; i < op1->value.str.len; i++)
		{
			result->value.str.val[i] = ~op1->value.str.val[i];
		}
		return SUCCESS;
	}
	zend_error(E_ERROR, "Unsupported operand types");
	return FAILURE;				/* unknown datatype */
}

// copied from zend_operators.c and beautified
ZEND_API int bitwise_or_function(zval *result, zval *op1, zval *op2 TSRMLS_DC)
{
	zval op1_copy, op2_copy;
	
	if (op1->type == IS_STRING && op2->type == IS_STRING)
	{
		zval *longer, *shorter;
		char *result_str;
		int i, result_len;

		if (op1->value.str.len >= op2->value.str.len)
		{
			longer = op1;
			shorter = op2;
		}
		else
		{
			longer = op2;
			shorter = op1;
		}

		result->type = IS_STRING;
		result_len = longer->value.str.len;
		result_str = estrndup(longer->value.str.val, longer->value.str.len);
		for (i = 0; i < shorter->value.str.len; i++)
		{
			result_str[i] |= shorter->value.str.val[i];
		}
		if (result == op1)
		{
			STR_FREE(result->value.str.val);
		}
		result->value.str.val = result_str;
		result->value.str.len = result_len;
		return SUCCESS;
	}
	zendi_convert_to_long(op1, op1_copy, result);
	zendi_convert_to_long(op2, op2_copy, result);

	result->type = IS_LONG;
	result->value.lval = op1->value.lval | op2->value.lval;
	return SUCCESS;
}

// copied from zend_operators.c and beautified
ZEND_API int bitwise_xor_function(zval *result, zval *op1, zval *op2 TSRMLS_DC)
{
	zval op1_copy, op2_copy;
	
	if (op1->type == IS_STRING && op2->type == IS_STRING)
	{
		zval *longer, *shorter;
		char *result_str;
		int i, result_len;

		if (op1->value.str.len >= op2->value.str.len)
		{
			longer = op1;
			shorter = op2;
		}
		else
		{
			longer = op2;
			shorter = op1;
		}

		result->type = IS_STRING;
		result_len = shorter->value.str.len;
		result_str = estrndup(shorter->value.str.val, shorter->value.str.len);
		for (i = 0; i < shorter->value.str.len; i++)
		{
			result_str[i] ^= longer->value.str.val[i];
		}
		if (result == op1)
		{
			STR_FREE(result->value.str.val);
		}
		result->value.str.val = result_str;
		result->value.str.len = result_len;
		return SUCCESS;
	}

	zendi_convert_to_long(op1, op1_copy, result);
	zendi_convert_to_long(op2, op2_copy, result);	

	result->type = IS_LONG;
	result->value.lval = op1->value.lval ^ op2->value.lval;
	return SUCCESS;
}

// copied from zend_operators.c
ZEND_API int boolean_not_function(zval *result, zval *op1 TSRMLS_DC)
{
	zval op1_copy;
	
	zendi_convert_to_boolean(op1, op1_copy, result);

	result->type = IS_BOOL;
	result->value.lval = !op1->value.lval;
	return SUCCESS;
}

// copied from zend_operators.c
ZEND_API int boolean_xor_function(zval *result, zval *op1, zval *op2 TSRMLS_DC)
{
	zval op1_copy, op2_copy;
	
	result->type = IS_BOOL;

	zendi_convert_to_boolean(op1, op1_copy, result);
	zendi_convert_to_boolean(op2, op2_copy, result);
	result->value.lval = op1->value.lval ^ op2->value.lval;
	return SUCCESS;
}

// copied from zend_operators.c and beautified
ZEND_API int add_char_to_string(zval *result, zval *op1, zval *op2)
{
	result->value.str.len = op1->value.str.len + 1;
	result->value.str.val = (char *)erealloc(op1->value.str.val, result->value.str.len+1);
    result->value.str.val[result->value.str.len - 1] = (char)op2->value.lval;
	result->value.str.val[result->value.str.len] = 0;
	result->type = IS_STRING;
	return SUCCESS;
}

// copied from zend_operators.c and beautified
ZEND_API int add_string_to_string(zval *result, zval *op1, zval *op2)
{
	int length = op1->value.str.len + op2->value.str.len;
	if (op1->value.str.val == empty_string)
	{
		result->value.str.val = (char *)emalloc(length+1);
	}
	else
	{
		result->value.str.val = (char *)erealloc(op1->value.str.val, length+1);
	}
    memcpy(result->value.str.val + op1->value.str.len, op2->value.str.val, op2->value.str.len);
    result->value.str.val[length] = 0;
	result->value.str.len = length;
	result->type = IS_STRING;
	return SUCCESS;
}

// copied from zend_operators.c
ZEND_API int zval_is_true(zval *op)
{
	convert_to_boolean(op);
	return (op->value.lval ? 1 : 0);
}

// copied from zend_execute_API.c and beautified
ZEND_API int zval_update_constant(zval **pp, void *arg TSRMLS_DC)
{
	zval *p = *pp;
	zend_bool inline_change = (zend_bool)(unsigned long)arg;
	zval const_value;

	if (p->type == IS_CONSTANT)
	{
		int refcount;

		SEPARATE_ZVAL(pp);
		p = *pp;

		refcount = p->refcount;

		if (!zend_get_constant(p->value.str.val, p->value.str.len, &const_value TSRMLS_CC))
		{
			zend_error(E_NOTICE, "Use of undefined constant %s - assumed '%s'",
				p->value.str.val, p->value.str.val);

			p->type = IS_STRING;
			if (!inline_change) zval_copy_ctor(p);
		}
		else
		{
			if (inline_change) STR_FREE(p->value.str.val);
			*p = const_value;
		}

		INIT_PZVAL(p);
		p->refcount = refcount;
	}
	else if (p->type == IS_CONSTANT_ARRAY)
	{
		zval **element, *new_val;
		char *str_index;
		uint str_index_len;
		ulong num_index;

		SEPARATE_ZVAL(pp);
		p = *pp;
		p->type = IS_ARRAY;
		
		/* First go over the array and see if there are any constant indices */
		zend_hash_internal_pointer_reset(p->value.ht);
		while (zend_hash_get_current_data(p->value.ht, (void **) &element) == SUCCESS)
		{
			if (!(Z_TYPE_PP(element) & IS_CONSTANT_INDEX))
			{
				zend_hash_move_forward(p->value.ht);
				continue;
			}
			Z_TYPE_PP(element) &= ~IS_CONSTANT_INDEX;
			if (zend_hash_get_current_key_ex(p->value.ht, &str_index, &str_index_len, &num_index,
				0, NULL) != HASH_KEY_IS_STRING)
			{
				zend_hash_move_forward(p->value.ht);
				continue;
			}
			if (!zend_get_constant(str_index, str_index_len-1, &const_value TSRMLS_CC))
			{
				zend_error(E_NOTICE, "Use of undefined constant %s - assumed '%s'",	str_index, str_index);
				zend_hash_move_forward(p->value.ht);
				continue;
			}
			
			if (const_value.type == IS_STRING && const_value.value.str.len == str_index_len-1 &&
			   !strncmp(const_value.value.str.val, str_index, str_index_len))
			{
				/* constant value is the same as its name */
				zval_dtor(&const_value);
				zend_hash_move_forward(p->value.ht);
				continue;
			}

			ALLOC_ZVAL(new_val);
			*new_val = **element;
			zval_copy_ctor(new_val);
			new_val->refcount = 1;
			new_val->is_ref = 0;
			
			/* preserve this bit for inheritance */
			Z_TYPE_PP(element) |= IS_CONSTANT_INDEX;
			
			switch (const_value.type)
			{
				case IS_STRING:
					zend_hash_update(p->value.ht, const_value.value.str.val, const_value.value.str.len+1,
						&new_val, sizeof(zval *), NULL);
					break;

				case IS_LONG:
					zend_hash_index_update(p->value.ht, const_value.value.lval, &new_val, sizeof(zval *), NULL);
					break;
			}
			zend_hash_del(p->value.ht, str_index, str_index_len);
			zval_dtor(&const_value);
		}
		zend_hash_apply_with_argument(p->value.ht, (apply_func_arg_t)zval_update_constant, (void *)1 TSRMLS_CC);
	}
	return 0;
}

// copied from zend_execute.h and beautified
static inline int i_zend_is_true(zval *op)
{
	int result;

	switch (op->type)
	{
		case IS_NULL:
			result = 0;
			break;
		
		case IS_LONG:
		case IS_BOOL:
		case IS_RESOURCE:
			result = (op->value.lval ? 1 : 0);
			break;
		
		case IS_DOUBLE:
			result = (op->value.dval ? 1 : 0);
			break;

		case IS_STRING:
			if (op->value.str.len == 0 || (op->value.str.len == 1 && op->value.str.val[0] == '0'))
			{
				result = 0;
			}
			else
			{
				result = 1;
			}
			break;

		case IS_ARRAY:
			result = (zend_hash_num_elements(op->value.ht) ? 1 : 0);
			break;

		case IS_OBJECT:
			/* OBJ-TBI */
			result = 1;
			break;

		default:
			result = 0;
			break;
	}
	return result;
}

// copied from zend_execute_API.c
ZEND_API int zend_is_true(zval *op)
{
	return i_zend_is_true(op);
}

// copied from zend_operators.c and beautified
ZEND_API double zend_string_to_double(const char *number, zend_uint length)
{
	double divisor = 10.0;
	double result = 0.0;
	double exponent;
	const char *end = number + length;
	const char *digit = number;

	if (!length) return result;

	while (digit < end)
	{
		if (*digit <= '9' && *digit >= '0')
		{
			result *= 10;
			result += *digit - '0';
		}
		else if (*digit == '.')
		{
			digit++;
			break;
		}
		else if (toupper(*digit) == 'E')
		{
			exponent = (double)atoi(digit + 1);
			result *= pow(10.0, exponent);
			return result;
		}
		else return result;
		digit++;
	}

	while (digit < end)
	{
		if (*digit <= '9' && *digit >= '0')
		{
			result += (*digit - '0') / divisor;
			divisor *= 10;
		}
		else if (toupper(*digit) == 'E')
		{
			exponent = (double)atoi(digit + 1);
			result *= pow(10.0, exponent);
			return result;
		}
		else return result;
		digit++;
	}
	return result;
}

// copied from zend_operators.c and beautified
ZEND_API int concat_function(zval *result, zval *op1, zval *op2 TSRMLS_DC)
{
	zval op1_copy, op2_copy;
	int use_copy1, use_copy2;

	zend_make_printable_zval(op1, &op1_copy, &use_copy1);
	zend_make_printable_zval(op2, &op2_copy, &use_copy2);

	if (use_copy1) op1 = &op1_copy;
	if (use_copy2) op2 = &op2_copy;

	if (result == op1)
	{
		/* special case, perform operations on result */
		uint res_len = op1->value.str.len + op2->value.str.len;
		
		if (result->value.str.len == 0)
		{
			/* handle empty_string */
			STR_FREE(result->value.str.val);
			result->value.str.val = (char *)emalloc(res_len + 1);
		}
		else result->value.str.val = (char *)erealloc(result->value.str.val, res_len + 1);

		memcpy(result->value.str.val + result->value.str.len, op2->value.str.val, op2->value.str.len);
		result->value.str.val[res_len] = 0;
		result->value.str.len = res_len;
	}
	else
	{
		result->value.str.len = op1->value.str.len + op2->value.str.len;
		result->value.str.val = (char *)emalloc(result->value.str.len + 1);
		memcpy(result->value.str.val, op1->value.str.val, op1->value.str.len);
		memcpy(result->value.str.val + op1->value.str.len, op2->value.str.val, op2->value.str.len);
		result->value.str.val[result->value.str.len] = 0;
		result->type = IS_STRING;
	}
	if (use_copy1) zval_dtor(op1);
	if (use_copy2) zval_dtor(op2);

	return SUCCESS;
}

// copied from zend_operators.c and beautified
ZEND_API int add_function(zval *result, zval *op1, zval *op2 TSRMLS_DC)
{
	zval op1_copy, op2_copy;

	if (op1->type == IS_ARRAY && op2->type == IS_ARRAY)
	{
		zval *tmp;

		if (result == op1 && result == op2)
		{
			/* $a += $a */
			return SUCCESS;
		}
		if (result != op1)
		{
			*result = *op1;
			zval_copy_ctor(result);
		}
		zend_hash_merge(result->value.ht, op2->value.ht, (void (*)(void *pData))zval_add_ref,
			(void *)&tmp, sizeof(zval *), 0);
		return SUCCESS;
	}
	zendi_convert_scalar_to_number(op1, op1_copy, result);
	zendi_convert_scalar_to_number(op2, op2_copy, result);


	if (op1->type == IS_LONG && op2->type == IS_LONG)
	{
		long lval = op1->value.lval + op2->value.lval;
		
		/* check for overflow by comparing sign bits */
		if ((op1->value.lval & LONG_SIGN_MASK) == (op2->value.lval & LONG_SIGN_MASK) 
			&& (op1->value.lval & LONG_SIGN_MASK) != (lval & LONG_SIGN_MASK))
		{
			result->value.dval = (double)op1->value.lval + (double)op2->value.lval;
			result->type = IS_DOUBLE;
		}
		else
		{
			result->value.lval = lval;
			result->type = IS_LONG;
		}
		return SUCCESS;
	}
	if ((op1->type == IS_DOUBLE && op2->type == IS_LONG)
		|| (op1->type == IS_LONG && op2->type == IS_DOUBLE))
	{
		result->value.dval = (op1->type == IS_LONG ?
						 (((double) op1->value.lval) + op2->value.dval) :
						 (op1->value.dval + ((double) op2->value.lval)));
		result->type = IS_DOUBLE;
		return SUCCESS;
	}
	if (op1->type == IS_DOUBLE && op2->type == IS_DOUBLE)
	{
		result->type = IS_DOUBLE;
		result->value.dval = op1->value.dval + op2->value.dval;
		return SUCCESS;
	}
	zend_error(E_ERROR, "Unsupported operand types");
	return FAILURE;				/* unknown datatype */
}

// copied from zend_operators.c and beautified
ZEND_API int sub_function(zval *result, zval *op1, zval *op2 TSRMLS_DC)
{
	zval op1_copy, op2_copy;
	
	zendi_convert_scalar_to_number(op1, op1_copy, result);
	zendi_convert_scalar_to_number(op2, op2_copy, result);

	if (op1->type == IS_LONG && op2->type == IS_LONG)
	{
		long lval = op1->value.lval - op2->value.lval;
		
		/* check for overflow by comparing sign bits */
		if ((op1->value.lval & LONG_SIGN_MASK) != (op2->value.lval & LONG_SIGN_MASK) 
			&& (op1->value.lval & LONG_SIGN_MASK) != (lval & LONG_SIGN_MASK))
		{
			result->value.dval = (double)op1->value.lval - (double)op2->value.lval;
			result->type = IS_DOUBLE;
		}
		else
		{
			result->value.lval = lval;
			result->type = IS_LONG;
		}
		return SUCCESS;
	}
	if ((op1->type == IS_DOUBLE && op2->type == IS_LONG)
		|| (op1->type == IS_LONG && op2->type == IS_DOUBLE))
	{
		result->value.dval = (op1->type == IS_LONG ?
						 (((double) op1->value.lval) - op2->value.dval) :
						 (op1->value.dval - ((double) op2->value.lval)));
		result->type = IS_DOUBLE;
		return SUCCESS;
	}
	if (op1->type == IS_DOUBLE && op2->type == IS_DOUBLE)
	{
		result->type = IS_DOUBLE;
		result->value.dval = op1->value.dval - op2->value.dval;
		return SUCCESS;
	}
	zend_error(E_ERROR, "Unsupported operand types");
	return FAILURE;				/* unknown datatype */
}

// copied from zend_operators.c and beautified
ZEND_API int decrement_function(zval *op1)
{
	long lval;
	double dval;
	
	switch (op1->type)
	{
		case IS_LONG:
			if (op1->value.lval == LONG_MIN)
			{
				double d = (double)op1->value.lval;
				ZVAL_DOUBLE(op1, d-1);
			}
			else op1->value.lval--;
			break;

		case IS_DOUBLE:
			op1->value.dval = op1->value.dval - 1;
			break;

		case IS_STRING:		/* Like perl we only support string increment */
			if (op1->value.str.len == 0)
			{
				/* consider as 0 */
				STR_FREE(op1->value.str.val);
				op1->value.lval = -1;
				op1->type = IS_LONG;
				break;
			}
			switch (is_numeric_string(op1->value.str.val, op1->value.str.len, &lval, &dval, 0))
			{
				case IS_LONG:
					STR_FREE(op1->value.str.val);
					if (lval == LONG_MIN)
					{
						double d = (double)lval;
						ZVAL_DOUBLE(op1, d - 1);
					}
					else
					{
						op1->value.lval = lval - 1;
						op1->type = IS_LONG;
					}
					break;

				case IS_DOUBLE:
					STR_FREE(op1->value.str.val);
					op1->value.dval = dval - 1;
					op1->type = IS_DOUBLE;
					break;
			}
			break;

		default:
			return FAILURE;
	}

	return SUCCESS;
}

#define LOWER_CASE 1
#define UPPER_CASE 2
#define NUMERIC 3

// copied from zend_operators.c and beautified
static void increment_string(zval *str)
{
    int carry = 0;
    int pos = str->value.str.len - 1;
    char *s = str->value.str.val;
    char *t;
    int last = 0; /* Shut up the compiler warning */
    int ch;
    
	if (str->value.str.len == 0)
	{
		STR_FREE(str->value.str.val);
		str->value.str.val = estrndup("1", sizeof("1") - 1);
		str->value.str.len = 1;
		return;
	}

	while (pos >= 0)
	{
        ch = s[pos];
        if (ch >= 'a' && ch <= 'z')
		{
            if (ch == 'z')
			{
                s[pos] = 'a';
                carry = 1;
            }
			else
			{
                s[pos]++;
                carry = 0;
            }
            last = LOWER_CASE;
        }
		else if (ch >= 'A' && ch <= 'Z')
		{
            if (ch == 'Z')
			{
                s[pos] = 'A';
                carry = 1;
            }
			else
			{
                s[pos]++;
                carry = 0;
            }
            last = UPPER_CASE;
        }
		else if (ch >= '0' && ch <= '9')
		{
            if (ch == '9')
			{
                s[pos] = '0';
                carry = 1;
            }
			else
			{
                s[pos]++;
                carry = 0;
            }
            last = NUMERIC;
        }
		else
		{
            carry = 0;
            break;
        }
        if (carry == 0) break;
        pos--;
    }

    if (carry)
	{
        t = (char *)emalloc(str->value.str.len + 1 + 1);
        memcpy(t + 1, str->value.str.val, str->value.str.len);
        str->value.str.len++;
        t[str->value.str.len] = '\0';
        switch (last)
		{
            case NUMERIC:
            	t[0] = '1';
            	break;

            case UPPER_CASE:
            	t[0] = 'A';
            	break;

            case LOWER_CASE:
            	t[0] = 'a';
            	break;
        }
        STR_FREE(str->value.str.val);
        str->value.str.val = t;
    }
}

// copied from zend_operators.c and beautified
ZEND_API int increment_function(zval *op1)
{
	switch (op1->type)
	{
		case IS_LONG:
			if (op1->value.lval == LONG_MAX)
			{
				/* switch to double */
				double d = (double)op1->value.lval;
				ZVAL_DOUBLE(op1, d + 1);
			}
			else op1->value.lval++;
			break;

		case IS_DOUBLE:
			op1->value.dval = op1->value.dval + 1;
			break;

		case IS_NULL:
			op1->value.lval = 1;
			op1->type = IS_LONG;
			break;

		case IS_STRING:
			{
				long lval;
				double dval;
				char *strval = op1->value.str.val;

				switch (is_numeric_string(strval, op1->value.str.len, &lval, &dval, 0))
				{
					case IS_LONG:
						if (lval == LONG_MAX)
						{
							/* switch to double */
							double d = (double)lval;
							ZVAL_DOUBLE(op1, d+1);
						}
						else
						{
							op1->value.lval = lval + 1;
							op1->type = IS_LONG;
						}
						efree(strval); /* should never be empty_string */
						break;

					case IS_DOUBLE:
						op1->value.dval = dval + 1;
						op1->type = IS_DOUBLE;
						efree(strval); /* should never be empty_string */
						break;

					default:
						/* Perl style string increment */
						increment_string(op1);
						break;
				}
			}
			break;

		default:
			return FAILURE;
	}
	return SUCCESS;
}

// copied from zend_operators.c and beautified
ZEND_API int div_function(zval *result, zval *op1, zval *op2 TSRMLS_DC)
{
	zval op1_copy, op2_copy;
	
	zendi_convert_scalar_to_number(op1, op1_copy, result);
	zendi_convert_scalar_to_number(op2, op2_copy, result);

	if ((op2->type == IS_LONG && op2->value.lval == 0) || (op2->type == IS_DOUBLE && op2->value.dval == 0.0))
	{
		zend_error(E_WARNING, "Division by zero");
		ZVAL_BOOL(result, 0);
		return FAILURE;			/* division by zero */
	}
	if (op1->type == IS_LONG && op2->type == IS_LONG)
	{
		if (op1->value.lval % op2->value.lval == 0)
		{
			/* integer */
			result->type = IS_LONG;
			result->value.lval = op1->value.lval / op2->value.lval;
		}
		else
		{
			result->type = IS_DOUBLE;
			result->value.dval = ((double)op1->value.lval) / op2->value.lval;
		}
		return SUCCESS;
	}
	if ((op1->type == IS_DOUBLE && op2->type == IS_LONG)
		|| (op1->type == IS_LONG && op2->type == IS_DOUBLE))
	{
		result->value.dval = (op1->type == IS_LONG ?
						 (((double)op1->value.lval) / op2->value.dval) :
						 (op1->value.dval / ((double)op2->value.lval)));
		result->type = IS_DOUBLE;
		return SUCCESS;
	}
	if (op1->type == IS_DOUBLE && op2->type == IS_DOUBLE)
	{
		result->type = IS_DOUBLE;
		result->value.dval = op1->value.dval / op2->value.dval;
		return SUCCESS;
	}
	zend_error(E_ERROR, "Unsupported operand types");
	return FAILURE;				/* unknown datatype */
}

// copied from zend_operators.c and beautified
ZEND_API int mod_function(zval *result, zval *op1, zval *op2 TSRMLS_DC)
{
	zval op1_copy, op2_copy;
	
	zendi_convert_to_long(op1, op1_copy, result);
	zendi_convert_to_long(op2, op2_copy, result);

	if (op2->value.lval == 0)
	{
		ZVAL_BOOL(result, 0);
		return FAILURE;			/* modulus by zero */
	}

	result->type = IS_LONG;
	result->value.lval = op1->value.lval % op2->value.lval;
	return SUCCESS;
}

// copied from zend_operators.c and beautified
ZEND_API int mul_function(zval *result, zval *op1, zval *op2 TSRMLS_DC)
{
	zval op1_copy, op2_copy;
	
	zendi_convert_scalar_to_number(op1, op1_copy, result);
	zendi_convert_scalar_to_number(op2, op2_copy, result);

	if (op1->type == IS_LONG && op2->type == IS_LONG)
	{
		long lval = op1->value.lval * op2->value.lval;
		
		/* check for overflow by applying the reverse calculation */
		if (op1->value.lval != 0 && lval / op1->value.lval != op2->value.lval)
		{
			result->value.dval = (double) op1->value.lval * (double) op2->value.lval;
			result->type = IS_DOUBLE;
		}
		else
		{
			result->value.lval = lval;
			result->type = IS_LONG;
		}
		return SUCCESS;
	}
	if ((op1->type == IS_DOUBLE && op2->type == IS_LONG)
		|| (op1->type == IS_LONG && op2->type == IS_DOUBLE))
	{
		result->value.dval = (op1->type == IS_LONG ?
						 (((double) op1->value.lval) * op2->value.dval) :
						 (op1->value.dval * ((double) op2->value.lval)));
		result->type = IS_DOUBLE;
		return SUCCESS;
	}
	if (op1->type == IS_DOUBLE && op2->type == IS_DOUBLE)
	{
		result->type = IS_DOUBLE;
		result->value.dval = op1->value.dval * op2->value.dval;
		return SUCCESS;
	}
	zend_error(E_ERROR, "Unsupported operand types");
	return FAILURE;				/* unknown datatype */
}

// copied from zend_operators.c
ZEND_API int shift_left_function(zval *result, zval *op1, zval *op2 TSRMLS_DC)
{
	zval op1_copy, op2_copy;
	
	zendi_convert_to_long(op1, op1_copy, result);
	zendi_convert_to_long(op2, op2_copy, result);
	result->value.lval = op1->value.lval << op2->value.lval;
	result->type = IS_LONG;
	return SUCCESS;
}

// copied from zend_operators.c
ZEND_API int shift_right_function(zval *result, zval *op1, zval *op2 TSRMLS_DC)
{
	zval op1_copy, op2_copy;
	
	zendi_convert_to_long(op1, op1_copy, result);
	zendi_convert_to_long(op2, op2_copy, result);
	result->value.lval = op1->value.lval >> op2->value.lval;
	result->type = IS_LONG;
	return SUCCESS;
}

// copied from zend_operators.c and beautified
static int hash_zval_identical_function(const zval **z1, const zval **z2)
{
	zval result;
	TSRMLS_FETCH();

	/* is_identical_function() returns 1 in case of identity and 0 in case
	 * of a difference;
	 * whereas this comparison function is expected to return 0 on identity,
	 * and non zero otherwise.
	 */
	if (is_identical_function(&result, (zval *)*z1, (zval *)*z2 TSRMLS_CC) == FAILURE)
	{
		return 1;
	}
	return !result.value.lval;
}

// copied from zend_operators.c and beautified
ZEND_API int is_identical_function(zval *result, zval *op1, zval *op2 TSRMLS_DC)
{
	result->type = IS_BOOL;
	if (op1->type != op2->type)
	{
		result->value.lval = 0;
		return SUCCESS;
	}
	switch (op1->type)
	{
		case IS_NULL:
			result->value.lval = (op2->type==IS_NULL);
			break;

		case IS_BOOL:
		case IS_LONG:
		case IS_RESOURCE:
			result->value.lval = (op1->value.lval == op2->value.lval);
			break;

		case IS_DOUBLE:
			result->value.lval = (op1->value.dval == op2->value.dval);
			break;

		case IS_STRING:
			if ((op1->value.str.len == op2->value.str.len)
				&& (!memcmp(op1->value.str.val, op2->value.str.val, op1->value.str.len)))
			{
				result->value.lval = 1;
			}
			else result->value.lval = 0;
			break;

		case IS_ARRAY:
			if (zend_hash_compare(op1->value.ht, op2->value.ht,
				(compare_func_t)hash_zval_identical_function, 1 TSRMLS_CC) == 0)
			{
				result->value.lval = 1;
			}
			else result->value.lval = 0;
			break;

		case IS_OBJECT:
			/* OBJECTS_FIXME */
			if (Z_OBJCE_P(op1) != Z_OBJCE_P(op2)) result->value.lval = 0;
			else
			{
				if (zend_hash_compare(Z_OBJPROP_P(op1), Z_OBJPROP_P(op2),
					(compare_func_t)hash_zval_identical_function, 1 TSRMLS_CC) == 0) result->value.lval = 1;
				else result->value.lval = 0;
			}
			break;

		default:
			ZVAL_BOOL(result, 0);
			return FAILURE;
	}
	return SUCCESS;
}

// copied from zend_operators.c and beautified
ZEND_API int is_not_identical_function(zval *result, zval *op1, zval *op2 TSRMLS_DC)
{
   result->type = IS_BOOL;
   if (is_identical_function(result, op1, op2 TSRMLS_CC) == FAILURE) return FAILURE;
   result->value.lval = !result->value.lval;
   return SUCCESS;
}

// copied from zend_operators.c and beautified
ZEND_API int is_equal_function(zval *result, zval *op1, zval *op2 TSRMLS_DC)
{
	if (compare_function(result, op1, op2 TSRMLS_CC) == FAILURE) return FAILURE;

	convert_to_boolean(result);
	if (result->value.lval == 0) result->value.lval = 1;
	else result->value.lval = 0;

	return SUCCESS;
}

// copied from zend_operators.c and beautified
ZEND_API int is_not_equal_function(zval *result, zval *op1, zval *op2 TSRMLS_DC)
{
	if (compare_function(result, op1, op2 TSRMLS_CC) == FAILURE) return FAILURE;

	convert_to_boolean(result);
	if (result->value.lval) result->value.lval = 1;
	else result->value.lval = 0;

	return SUCCESS;
}

// copied from zend_operators.c and beautified
ZEND_API int is_smaller_function(zval *result, zval *op1, zval *op2 TSRMLS_DC)
{
	if (compare_function(result, op1, op2 TSRMLS_CC) == FAILURE) return FAILURE;

	if (result->type == IS_LONG)
	{
		result->type = IS_BOOL;
		if (result->value.lval < 0) result->value.lval = 1;
		else result->value.lval = 0;

		return SUCCESS;
	}
	if (result->type == IS_DOUBLE)
	{
		result->type = IS_BOOL;
		if (result->value.dval < 0) result->value.lval = 1;
		else result->value.lval = 0;

		return SUCCESS;
	}
	zend_error(E_ERROR, "Unsupported operand types");
	return FAILURE;
}

// copied from zend_operators.c and beautified
ZEND_API int is_smaller_or_equal_function(zval *result, zval *op1, zval *op2 TSRMLS_DC)
{
	if (compare_function(result, op1, op2 TSRMLS_CC) == FAILURE) return FAILURE;

	if (result->type == IS_LONG)
	{
		result->type = IS_BOOL;
		if (result->value.lval <= 0) result->value.lval = 1;
		else result->value.lval = 0;

		return SUCCESS;
	}
	if (result->type == IS_DOUBLE)
	{
		result->type = IS_BOOL;
		if (result->value.dval <= 0) result->value.lval = 1;
		else result->value.lval = 0;

		return SUCCESS;
	}
	zend_error(E_ERROR, "Unsupported operand types");
	return FAILURE;
}

// copied from zend_operators.c and beautified
ZEND_API void multi_convert_to_long_ex(int argc, ...)
{
	zval **arg;
	va_list ap;
	
	va_start(ap, argc);

	while (argc--)
	{
		arg = va_arg(ap, zval **);
		convert_to_long_ex(arg);
	}
	
	va_end(ap);
}

// copied from zend_operators.c and beautified
ZEND_API void multi_convert_to_double_ex(int argc, ...)
{
	zval **arg;
	va_list ap;
	
	va_start(ap, argc);

	while (argc--)
	{
		arg = va_arg(ap, zval **);
		convert_to_double_ex(arg);
	}
	
	va_end(ap);
}

// copied from zend_operators.c and beautified
ZEND_API void multi_convert_to_string_ex(int argc, ...)
{
	zval **arg;
	va_list ap;
	
	va_start(ap, argc);

	while (argc--)
	{
		arg = va_arg(ap, zval **);
		convert_to_string_ex(arg);
	}
	
	va_end(ap);
}

// copied from zend_opcode.c and beautified
ZEND_API void *get_binary_op(int opcode)
{
	switch (opcode)
	{
		case ZEND_ADD:
		case ZEND_ASSIGN_ADD:
			return (void *)add_function;
			break;

		case ZEND_SUB:
		case ZEND_ASSIGN_SUB:
			return (void *)sub_function;
			break;

		case ZEND_MUL:
		case ZEND_ASSIGN_MUL:
			return (void *)mul_function;
			break;

		case ZEND_DIV:
		case ZEND_ASSIGN_DIV:
			return (void *)div_function;
			break;

		case ZEND_MOD:
		case ZEND_ASSIGN_MOD:
			return (void *)mod_function;
			break;

		case ZEND_SL:
		case ZEND_ASSIGN_SL:
			return (void *)shift_left_function;
			break;

		case ZEND_SR:
		case ZEND_ASSIGN_SR:
			return (void *)shift_right_function;
			break;

		case ZEND_CONCAT:
		case ZEND_ASSIGN_CONCAT:
			return (void *)concat_function;
			break;

		case ZEND_IS_IDENTICAL:
			return (void *)is_identical_function;
			break;

		case ZEND_IS_NOT_IDENTICAL:
			return (void *)is_not_identical_function;
			break;

		case ZEND_IS_EQUAL:
			return (void *)is_equal_function;
			break;

		case ZEND_IS_NOT_EQUAL:
			return (void *)is_not_equal_function;
			break;

		case ZEND_IS_SMALLER:
			return (void *)is_smaller_function;
			break;

		case ZEND_IS_SMALLER_OR_EQUAL:
			return (void *)is_smaller_or_equal_function;
			break;

		case ZEND_BW_OR:
		case ZEND_ASSIGN_BW_OR:
			return (void *)bitwise_or_function;
			break;

		case ZEND_BW_AND:
		case ZEND_ASSIGN_BW_AND:
			return (void *)bitwise_and_function;
			break;

		case ZEND_BW_XOR:
		case ZEND_ASSIGN_BW_XOR:
			return (void *)bitwise_xor_function;
			break;

		default:
			return NULL;
			break;
	}
}

// copied from zend_opcode.c and beautified
ZEND_API unary_op_type get_unary_op(int opcode)
{
	switch (opcode)
	{
		case ZEND_BW_NOT:
			return (unary_op_type)bitwise_not_function;
			break;

		case ZEND_BOOL_NOT:
			return (unary_op_type)boolean_not_function;
			break;

		default:
			return (unary_op_type) NULL;
			break;
	}
}

// copied from php_variables.c and beautified
void _php_import_environment_variables(zval *array_ptr TSRMLS_DC)
{
	//char **env, *p, *t;

	//for (env = environ; env != NULL && *env != NULL; env++)
	//{
	//	p = strchr(*env, '=');
	//	if (!p)	continue; 		/* malformed entry? */

	//	t = estrndup(*env, p - *env);
	//	php_register_variable(t, p + 1, array_ptr TSRMLS_CC);
	//	efree(t);
	//}
}

ZEND_API void (*php_import_environment_variables)(zval *array_ptr TSRMLS_DC) = _php_import_environment_variables;

#ifdef PHP4TS
// copied from zend_operators.c
ZEND_API zend_bool zend_is_callable(zval *callable, zend_bool syntax_only, char **callable_name)
{
	char *lcname;
	int retval = 0;
	TSRMLS_FETCH();

	switch (Z_TYPE_P(callable))
	{
		case IS_STRING:
		{
			if (callable_name) *callable_name = estrndup(Z_STRVAL_P(callable), Z_STRLEN_P(callable));

			if (syntax_only) return 1;

			lcname = estrndup(Z_STRVAL_P(callable), Z_STRLEN_P(callable));
			zend_str_tolower(lcname, Z_STRLEN_P(callable));
			if (zend_hash_exists(EG(function_table), lcname, Z_STRLEN_P(callable) + 1)) retval = 1;
			efree(lcname);
			break;
		}

		case IS_ARRAY:
			{
				zval **method;
				zval **obj;
				zend_class_entry *ce = NULL;
				char callable_name_len;
				
				if (zend_hash_num_elements(Z_ARRVAL_P(callable)) == 2 &&
					zend_hash_index_find(Z_ARRVAL_P(callable), 0, (void **) &obj) == SUCCESS &&
					zend_hash_index_find(Z_ARRVAL_P(callable), 1, (void **) &method) == SUCCESS &&
					(Z_TYPE_PP(obj) == IS_OBJECT || Z_TYPE_PP(obj) == IS_STRING) &&
					Z_TYPE_PP(method) == IS_STRING)
				{
					if (Z_TYPE_PP(obj) == IS_STRING)
					{
						if (callable_name)
						{
							char *ptr;

							callable_name_len = Z_STRLEN_PP(obj) + Z_STRLEN_PP(method) + sizeof("::");
							ptr = *callable_name = (char *)emalloc(callable_name_len);
							memcpy(ptr, Z_STRVAL_PP(obj), Z_STRLEN_PP(obj));
							ptr += Z_STRLEN_PP(obj);
							memcpy(ptr, "::", sizeof("::") - 1);
							ptr += sizeof("::") - 1;
							memcpy(ptr, Z_STRVAL_PP(method), Z_STRLEN_PP(method) + 1);
						}

						if (syntax_only) return 1;

						lcname = estrndup(Z_STRVAL_PP(obj), Z_STRLEN_PP(obj));
						zend_str_tolower(lcname, Z_STRLEN_PP(obj));
						zend_hash_find(EG(class_table), lcname, Z_STRLEN_PP(obj) + 1, (void**)&ce);
						efree(lcname);
					}
					else
					{
						ce = Z_OBJCE_PP(obj);

						if (callable_name)
						{
							char *ptr;

							callable_name_len = ce->name_length + Z_STRLEN_PP(method) + sizeof("::");
							ptr = *callable_name = (char *)emalloc(callable_name_len);
							memcpy(ptr, ce->name, ce->name_length);
							ptr += ce->name_length;
							memcpy(ptr, "::", sizeof("::") - 1);
							ptr += sizeof("::") - 1;
							memcpy(ptr, Z_STRVAL_PP(method), Z_STRLEN_PP(method) + 1);
						}

						if (syntax_only) return 1;
					}

					if (ce)
					{
						lcname = estrndup(Z_STRVAL_PP(method), Z_STRLEN_PP(method));
						zend_str_tolower(lcname, Z_STRLEN_PP(method));
						if (zend_hash_exists(&ce->function_table, lcname, Z_STRLEN_PP(method) + 1))
						{
							retval = 1;
						}
						efree(lcname);
					}
				}
				else if (callable_name) *callable_name = estrndup("Array", sizeof("Array")-1);
			}
			break;

		default:
			if (callable_name)
			{
				zval expr_copy;
				int use_copy;

				zend_make_printable_zval(callable, &expr_copy, &use_copy);
				*callable_name = estrndup(Z_STRVAL(expr_copy), Z_STRLEN(expr_copy));
				zval_dtor(&expr_copy);
			}
			break;
	}

	return retval;
}
#elif defined PHP5TS

//Copied from zend_api.c
static int zend_is_callable_check_func(int check_flags, zval ***zobj_ptr_ptr, zend_class_entry *ce_org, zval *callable, zend_class_entry **ce_ptr, zend_function **fptr_ptr TSRMLS_DC)
{
	int retval;
	char *lcname, *lmname, *colon;
	int clen, mlen;
	zend_function *fptr;
	zend_class_entry **pce;
	HashTable *ftable;

	*ce_ptr = NULL;
	*fptr_ptr = NULL;

	if ((colon = strstr(Z_STRVAL_P(callable), "::")) != NULL) {
		clen = colon - Z_STRVAL_P(callable);
		mlen = Z_STRLEN_P(callable) - clen - 2;
		lcname = zend_str_tolower_dup(Z_STRVAL_P(callable), clen);
		/* caution: lcname is not '\0' terminated */
		if (clen == sizeof("self") - 1 && memcmp(lcname, "self", sizeof("self") - 1) == 0) {
			*ce_ptr = EG(scope);
		} else if (clen == sizeof("parent") - 1 && memcmp(lcname, "parent", sizeof("parent") - 1) == 0 && EG(active_op_array)->scope) {
			*ce_ptr = EG(scope) ? EG(scope)->parent : NULL;
		} else if (zend_lookup_class(Z_STRVAL_P(callable), clen, &pce TSRMLS_CC) == SUCCESS) {
			*ce_ptr = *pce;
		}
		efree(lcname);
		if (!*ce_ptr) {
			return 0;
		}
		ftable = &(*ce_ptr)->function_table;
		if (ce_org && !instanceof_function(ce_org, *ce_ptr TSRMLS_CC)) {
			return 0;
		}
		lmname = zend_str_tolower_dup(Z_STRVAL_P(callable) + clen + 2, mlen);
	} else {
		mlen = Z_STRLEN_P(callable);
		lmname = zend_str_tolower_dup(Z_STRVAL_P(callable), mlen);
		if (ce_org) {
			ftable = &ce_org->function_table;
			*ce_ptr = ce_org;
		} else {
			ftable = EG(function_table);
		}
	}

	retval = zend_hash_find(ftable, lmname, mlen+1, (void**)&fptr) == SUCCESS ? 1 : 0;

	if (!retval) {
		if (*zobj_ptr_ptr && *ce_ptr && (*ce_ptr)->__call != 0) {
			retval = (*ce_ptr)->__call != NULL;
			*fptr_ptr = (*ce_ptr)->__call;
		}
	} else {
		*fptr_ptr = fptr;
		if (*ce_ptr) {
			if (!*zobj_ptr_ptr && !(fptr->common.fn_flags & ZEND_ACC_STATIC)) {
				if ((check_flags & IS_CALLABLE_CHECK_IS_STATIC) != 0) {
					retval = 0;
				} else {
					if (EG(This) && instanceof_function(Z_OBJCE_P(EG(This)), *ce_ptr TSRMLS_CC)) {
						*zobj_ptr_ptr = &EG(This);
						zend_error(E_STRICT, "Non-static method %s::%s() cannot be called statically, assuming $this from compatible context %s", (*ce_ptr)->name, fptr->common.function_name, Z_OBJCE_P(EG(This))->name);
					} else {
						zend_error(E_STRICT, "Non-static method %s::%s() cannot be called statically", (*ce_ptr)->name, fptr->common.function_name);
					}
				}
			}
			if (retval && (check_flags & IS_CALLABLE_CHECK_NO_ACCESS) == 0) {
				if (fptr->op_array.fn_flags & ZEND_ACC_PRIVATE) {
					if (!zend_check_private(fptr, *zobj_ptr_ptr ? Z_OBJCE_PP(*zobj_ptr_ptr) : EG(scope), lmname, mlen TSRMLS_CC)) {
						retval = 0;
					}
				} else if ((fptr->common.fn_flags & ZEND_ACC_PROTECTED)) {
					if (!zend_check_protected(fptr->common.scope, EG(scope))) {
						retval = 0;
					}
				}
			}
		}
	}
	efree(lmname);
	return retval;
}

//Copied from zend_api.c (php4ts)
ZEND_API zend_bool zend_is_callable_ex(zval *callable, uint check_flags, char **callable_name, int *callable_name_len, zend_class_entry **ce_ptr, zend_function **fptr_ptr, zval ***zobj_ptr_ptr TSRMLS_DC)
{
	char *lcname;
	zend_bool retval = 0; 
	int callable_name_len_local;
	zend_class_entry *ce_local, **pce;
	zend_function *fptr_local;
	zval **zobj_ptr_local;

	if (callable_name) {
		*callable_name = NULL;
	}
	if (callable_name_len == NULL) {
		callable_name_len = &callable_name_len_local;
	}
	if (ce_ptr == NULL) {
		ce_ptr = &ce_local;
	}
	if (fptr_ptr == NULL) {
		fptr_ptr = &fptr_local;
	}
	if (zobj_ptr_ptr == NULL) {
		zobj_ptr_ptr = &zobj_ptr_local;
	}
	*ce_ptr = NULL;
	*fptr_ptr = NULL;
	*zobj_ptr_ptr = NULL;

	switch (Z_TYPE_P(callable)) {
		case IS_STRING:
			if (callable_name) {
				*callable_name = estrndup(Z_STRVAL_P(callable), Z_STRLEN_P(callable));
				*callable_name_len = Z_STRLEN_P(callable);
			}
			if (check_flags & IS_CALLABLE_CHECK_SYNTAX_ONLY) {
				return 1;
			}
			
			retval = zend_is_callable_check_func(check_flags|IS_CALLABLE_CHECK_IS_STATIC, zobj_ptr_ptr, NULL, callable, ce_ptr, fptr_ptr TSRMLS_CC);
			break;

		case IS_ARRAY:
			{
				zend_class_entry *ce = NULL;
				zval **method;
				zval **obj;

				if (zend_hash_num_elements(Z_ARRVAL_P(callable)) == 2 &&
					zend_hash_index_find(Z_ARRVAL_P(callable), 0, (void **) &obj) == SUCCESS &&
					zend_hash_index_find(Z_ARRVAL_P(callable), 1, (void **) &method) == SUCCESS &&
					(Z_TYPE_PP(obj) == IS_OBJECT || Z_TYPE_PP(obj) == IS_STRING) &&
					Z_TYPE_PP(method) == IS_STRING) {

					if (Z_TYPE_PP(obj) == IS_STRING) {
						if (callable_name) {
							char *ptr;

							*callable_name_len = Z_STRLEN_PP(obj) + Z_STRLEN_PP(method) + sizeof("::") - 1;
							ptr = *callable_name = (char *)emalloc(*callable_name_len + 1);
							memcpy(ptr, Z_STRVAL_PP(obj), Z_STRLEN_PP(obj));
							ptr += Z_STRLEN_PP(obj);
							memcpy(ptr, "::", sizeof("::") - 1);
							ptr += sizeof("::") - 1;
							memcpy(ptr, Z_STRVAL_PP(method), Z_STRLEN_PP(method) + 1);
						}

						if (check_flags & IS_CALLABLE_CHECK_SYNTAX_ONLY) {
							return 1;
						}

						lcname = zend_str_tolower_dup(Z_STRVAL_PP(obj), Z_STRLEN_PP(obj));
						if (Z_STRLEN_PP(obj) == sizeof("self") - 1 && memcmp(lcname, "self", sizeof("self")) == 0 && EG(active_op_array)) {
							ce = EG(active_op_array)->scope;
						} else if (Z_STRLEN_PP(obj) == sizeof("parent") - 1 && memcmp(lcname, "parent", sizeof("parent")) == 0 && EG(active_op_array) && EG(active_op_array)->scope) {
							ce = EG(active_op_array)->scope->parent;
						} else if (zend_lookup_class(Z_STRVAL_PP(obj), Z_STRLEN_PP(obj), &pce TSRMLS_CC) == SUCCESS) {
							ce = *pce;
						}
						efree(lcname);
					} else {
						ce = Z_OBJCE_PP(obj); /* TBFixed: what if it's overloaded? */

						*zobj_ptr_ptr = obj;

						if (callable_name) {
							char *ptr;

							*callable_name_len = ce->name_length + Z_STRLEN_PP(method) + sizeof("::") - 1;
							ptr = *callable_name = (char *)emalloc(*callable_name_len + 1);
							memcpy(ptr, ce->name, ce->name_length);
							ptr += ce->name_length;
							memcpy(ptr, "::", sizeof("::") - 1);
							ptr += sizeof("::") - 1;
							memcpy(ptr, Z_STRVAL_PP(method), Z_STRLEN_PP(method) + 1);
						}

						if (check_flags & IS_CALLABLE_CHECK_SYNTAX_ONLY) {
							*ce_ptr = ce;
							return 1;
						}
					}

					if (ce) {
						retval = zend_is_callable_check_func(check_flags, zobj_ptr_ptr, ce, *method, ce_ptr, fptr_ptr TSRMLS_CC);
					}
				} else if (callable_name) {
					*callable_name = estrndup("Array", sizeof("Array")-1);
					*callable_name_len = sizeof("Array") - 1;
				}
				*ce_ptr = ce;
			}
			break;

		default:
			if (callable_name) {
				zval expr_copy;
				int use_copy;

				zend_make_printable_zval(callable, &expr_copy, &use_copy);
				*callable_name = estrndup(Z_STRVAL(expr_copy), Z_STRLEN(expr_copy));
				*callable_name_len = Z_STRLEN(expr_copy);
				zval_dtor(&expr_copy);
			}
			break;
	}

	return retval;
}


ZEND_API zend_bool zend_is_callable(zval *callable, uint check_flags, char **callable_name TSRMLS_DC)
{
	return zend_is_callable_ex(callable, check_flags, callable_name, NULL, NULL, NULL, NULL TSRMLS_CC);
}

// copied from zend_operators.c
ZEND_API zend_bool instanceof_function_ex(zend_class_entry *instance_ce, zend_class_entry *ce, zend_bool interfaces_only TSRMLS_DC)
{
	zend_uint i;

	for (i=0; i<instance_ce->num_interfaces; i++) {
		if (instanceof_function(instance_ce->interfaces[i], ce TSRMLS_CC)) {
			return 1;
		}
	}
	if (!interfaces_only) {
		while (instance_ce) {
			if (instance_ce == ce) {
				return 1;
			}
			instance_ce = instance_ce->parent;
		}
	}

	return 0;
}

// copied from zend_operators.c
ZEND_API zend_bool instanceof_function(zend_class_entry *instance_ce, zend_class_entry *ce TSRMLS_DC)
{
	return instanceof_function_ex(instance_ce, ce, 0 TSRMLS_CC);
}

// copied from zend_execute_api.c
ZEND_API void zend_rebuild_symbol_table(TSRMLS_D)
{
}

#endif