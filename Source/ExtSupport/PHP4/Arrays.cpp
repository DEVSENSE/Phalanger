//
// ExtSupport.PHP4 - substitute for php4ts.dll
//
// Arrays.cpp 
// - contains definitions of arrays related functions
//

#include "stdafx.h"
#include "Arrays.h"
#include "ExtSupport.h"
#include "Hash.h"
#include "Memory.h"
#include "Variables.h"
#include "Errors.h"
#include "Objects.h"

#include <string.h>

using namespace System;

#pragma unmanaged

// copied from zend_API.c
ZEND_API int _array_init(zval *arg ZEND_FILE_LINE_DC)
{
	ALLOC_HASHTABLE_REL(arg->value.ht);

	zend_hash_init(arg->value.ht, 0, NULL, ZVAL_PTR_DTOR, 0);
	arg->type = IS_ARRAY;
	return SUCCESS;
}

// add_* functions copied from zend_API
ZEND_API int add_assoc_function(zval *arg, char *key, void (*function_ptr)(INTERNAL_FUNCTION_PARAMETERS))
{
	zend_error(E_WARNING, "add_assoc_function() is no longer supported");
	return FAILURE;
}

ZEND_API int add_assoc_long_ex(zval *arg, char *key, uint key_len, long n)
{
	zval *tmp;

	MAKE_STD_ZVAL(tmp);
	ZVAL_LONG(tmp, n);
	
	return zend_hash_update(Z_ARRVAL_P(arg), key, key_len, (void *) &tmp, sizeof(zval *), NULL);
}

ZEND_API int add_assoc_null_ex(zval *arg, char *key, uint key_len)
{
	zval *tmp;
	
	MAKE_STD_ZVAL(tmp);
	ZVAL_NULL(tmp);
	
	return zend_hash_update(Z_ARRVAL_P(arg), key, key_len, (void *) &tmp, sizeof(zval *), NULL);
}

ZEND_API int add_assoc_bool_ex(zval *arg, char *key, uint key_len, int b)
{
	zval *tmp;
	
	MAKE_STD_ZVAL(tmp);
	ZVAL_BOOL(tmp, b);

	return zend_hash_update(Z_ARRVAL_P(arg), key, key_len, (void *) &tmp, sizeof(zval *), NULL);
}

ZEND_API int add_assoc_resource_ex(zval *arg, char *key, uint key_len, int r)
{
	zval *tmp;
	
	MAKE_STD_ZVAL(tmp);
	ZVAL_RESOURCE(tmp, r);
	
	return zend_hash_update(Z_ARRVAL_P(arg), key, key_len, (void *) &tmp, sizeof(zval *), NULL);
}

ZEND_API int add_assoc_double_ex(zval *arg, char *key, uint key_len, double d)
{
	zval *tmp;
	
	MAKE_STD_ZVAL(tmp);
	ZVAL_DOUBLE(tmp, d);

	return zend_hash_update(Z_ARRVAL_P(arg), key, key_len, (void *) &tmp, sizeof(zval *), NULL);
}

ZEND_API int add_assoc_string_ex(zval *arg, char *key, uint key_len, char *str, int duplicate)
{
	zval *tmp;
	
	MAKE_STD_ZVAL(tmp);
	ZVAL_STRING(tmp, str, duplicate);

	return zend_hash_update(Z_ARRVAL_P(arg), key, key_len, (void *) &tmp, sizeof(zval *), NULL);
}

ZEND_API int add_assoc_stringl_ex(zval *arg, char *key, uint key_len, char *str, uint length, int duplicate)
{
	zval *tmp;
	
	MAKE_STD_ZVAL(tmp);
	ZVAL_STRINGL(tmp, str, length, duplicate);

	return zend_hash_update(Z_ARRVAL_P(arg), key, key_len, (void *) &tmp, sizeof(zval *), NULL);
}

ZEND_API int add_assoc_zval_ex(zval *arg, char *key, uint key_len, zval *value)
{
	return zend_hash_update(Z_ARRVAL_P(arg), key, key_len, (void *) &value, sizeof(zval *), NULL);
}

ZEND_API int add_index_long(zval *arg, uint index, long n)
{
	zval *tmp;

	MAKE_STD_ZVAL(tmp);
	ZVAL_LONG(tmp, n);

	return zend_hash_index_update(Z_ARRVAL_P(arg), index, (void *) &tmp, sizeof(zval *), NULL);
}

ZEND_API int add_index_null(zval *arg, uint index)
{
	zval *tmp;

	MAKE_STD_ZVAL(tmp);
	ZVAL_NULL(tmp);

	return zend_hash_index_update(Z_ARRVAL_P(arg), index, (void *) &tmp, sizeof(zval *), NULL);
}

ZEND_API int add_index_bool(zval *arg, uint index, int b)
{
	zval *tmp;
	
	MAKE_STD_ZVAL(tmp);
	ZVAL_BOOL(tmp, b);
	
	return zend_hash_index_update(Z_ARRVAL_P(arg), index, (void *) &tmp, sizeof(zval *), NULL);
}

ZEND_API int add_index_resource(zval *arg, uint index, int r)
{
	zval *tmp;
	
	MAKE_STD_ZVAL(tmp);
	ZVAL_RESOURCE(tmp, r);
	
	return zend_hash_index_update(Z_ARRVAL_P(arg), index, (void *) &tmp, sizeof(zval *), NULL);
}

ZEND_API int add_index_double(zval *arg, uint index, double d)
{
	zval *tmp;
	
	MAKE_STD_ZVAL(tmp);
	ZVAL_DOUBLE(tmp, d);
	
	return zend_hash_index_update(Z_ARRVAL_P(arg), index, (void *) &tmp, sizeof(zval *), NULL);
}

ZEND_API int add_index_string(zval *arg, uint index, char *str, int duplicate)
{
	zval *tmp;
	
	MAKE_STD_ZVAL(tmp);
	ZVAL_STRING(tmp, str, duplicate);

	return zend_hash_index_update(Z_ARRVAL_P(arg), index, (void *) &tmp, sizeof(zval *), NULL);
}

ZEND_API int add_index_stringl(zval *arg, uint index, char *str, uint length, int duplicate)
{
	zval *tmp;
	
	MAKE_STD_ZVAL(tmp);
	ZVAL_STRINGL(tmp, str, length, duplicate);
	
	return zend_hash_index_update(Z_ARRVAL_P(arg), index, (void *) &tmp, sizeof(zval *), NULL);
}

ZEND_API int add_index_zval(zval *arg, uint index, zval *value)
{
	return zend_hash_index_update(Z_ARRVAL_P(arg), index, (void *) &value, sizeof(zval *), NULL);
}

ZEND_API int add_next_index_long(zval *arg, long n)
{
	zval *tmp;
	
	MAKE_STD_ZVAL(tmp);
	ZVAL_LONG(tmp, n);
	
	return zend_hash_next_index_insert(Z_ARRVAL_P(arg), &tmp, sizeof(zval *), NULL);
}

ZEND_API int add_next_index_null(zval *arg)
{
	zval *tmp;
	
	MAKE_STD_ZVAL(tmp);
	ZVAL_NULL(tmp);
	
	return zend_hash_next_index_insert(Z_ARRVAL_P(arg), &tmp, sizeof(zval *), NULL);
}

ZEND_API int add_next_index_bool(zval *arg, int b)
{
	zval *tmp;
	
	MAKE_STD_ZVAL(tmp);
	ZVAL_BOOL(tmp, b);
	
	return zend_hash_next_index_insert(Z_ARRVAL_P(arg), &tmp, sizeof(zval *), NULL);
}

ZEND_API int add_next_index_resource(zval *arg, int r)
{
	zval *tmp;
	
	MAKE_STD_ZVAL(tmp);
	ZVAL_RESOURCE(tmp, r);
	
	return zend_hash_next_index_insert(Z_ARRVAL_P(arg), &tmp, sizeof(zval *), NULL);
}

ZEND_API int add_next_index_double(zval *arg, double d)
{
	zval *tmp;
	
	MAKE_STD_ZVAL(tmp);
	ZVAL_DOUBLE(tmp, d);
	
	return zend_hash_next_index_insert(Z_ARRVAL_P(arg), &tmp, sizeof(zval *), NULL);
}

ZEND_API int add_next_index_string(zval *arg, char *str, int duplicate)
{
	zval *tmp;

	MAKE_STD_ZVAL(tmp);
	ZVAL_STRING(tmp, str, duplicate);

	return zend_hash_next_index_insert(Z_ARRVAL_P(arg), &tmp, sizeof(zval *), NULL);
}

ZEND_API int add_next_index_stringl(zval *arg, char *str, uint length, int duplicate)
{
	zval *tmp;

	MAKE_STD_ZVAL(tmp);
	ZVAL_STRINGL(tmp, str, length, duplicate);

	return zend_hash_next_index_insert(Z_ARRVAL_P(arg), &tmp, sizeof(zval *), NULL);
}

ZEND_API int add_next_index_zval(zval *arg, zval *value)
{
	return zend_hash_next_index_insert(Z_ARRVAL_P(arg), &value, sizeof(zval *), NULL);
}

ZEND_API int add_get_assoc_string_ex(zval *arg, char *key, uint key_len, char *str, void **dest, int duplicate)
{
	zval *tmp;
	
	MAKE_STD_ZVAL(tmp);
	ZVAL_STRING(tmp, str, duplicate);
	
	return zend_hash_update(Z_ARRVAL_P(arg), key, key_len, (void *) &tmp, sizeof(zval *), dest);
}

ZEND_API int add_get_assoc_stringl_ex(zval *arg, char *key, uint key_len, char *str, uint length, void **dest, int duplicate)
{
	zval *tmp;
	
	MAKE_STD_ZVAL(tmp);
	ZVAL_STRINGL(tmp, str, length, duplicate);

	return zend_hash_update(Z_ARRVAL_P(arg), key, key_len, (void *) &tmp, sizeof(zval *), dest);
}

ZEND_API int add_get_index_long(zval *arg, uint index, long l, void **dest)
{
	zval *tmp;
	
	MAKE_STD_ZVAL(tmp);
	ZVAL_LONG(tmp, l);
	
	return zend_hash_index_update(Z_ARRVAL_P(arg), index, (void *) &tmp, sizeof(zval *), dest);
}

ZEND_API int add_get_index_double(zval *arg, uint index, double d, void **dest)
{
	zval *tmp;
	
	MAKE_STD_ZVAL(tmp);
	ZVAL_DOUBLE(tmp, d);
	
	return zend_hash_index_update(Z_ARRVAL_P(arg), index, (void *) &tmp, sizeof(zval *), dest);
}

ZEND_API int add_get_index_string(zval *arg, uint index, char *str, void **dest, int duplicate)
{
	zval *tmp;
	
	MAKE_STD_ZVAL(tmp);
	ZVAL_STRING(tmp, str, duplicate);
	
	return zend_hash_index_update(Z_ARRVAL_P(arg), index, (void *) &tmp, sizeof(zval *), dest);
}

ZEND_API int add_get_index_stringl(zval *arg, uint index, char *str, uint length, void **dest, int duplicate)
{
	zval *tmp;
	
	MAKE_STD_ZVAL(tmp);
	ZVAL_STRINGL(tmp, str, length, duplicate);
	
	return zend_hash_index_update(Z_ARRVAL_P(arg), index, (void *) &tmp, sizeof(zval *), dest);
}

#ifdef PHP4TS
ZEND_API int add_property_long_ex(zval *arg, char *key, uint key_len, long n)
{
	zval *tmp;

	MAKE_STD_ZVAL(tmp);
	ZVAL_LONG(tmp, n);

	return zend_hash_update(Z_OBJPROP_P(arg), key, key_len, (void *) &tmp, sizeof(zval *), NULL);
}

ZEND_API int add_property_bool_ex(zval *arg, char *key, uint key_len, int b)
{
	zval *tmp;

	MAKE_STD_ZVAL(tmp);
	ZVAL_BOOL(tmp, b);

	return zend_hash_update(Z_OBJPROP_P(arg), key, key_len, (void *) &tmp, sizeof(zval *), NULL);
}

ZEND_API int add_property_null_ex(zval *arg, char *key, uint key_len)
{
	zval *tmp;
	
	MAKE_STD_ZVAL(tmp);
	ZVAL_NULL(tmp);
	
	return zend_hash_update(Z_OBJPROP_P(arg), key, key_len, (void *) &tmp, sizeof(zval *), NULL);
}

ZEND_API int add_property_resource_ex(zval *arg, char *key, uint key_len, long n)
{
	zval *tmp;
	
	MAKE_STD_ZVAL(tmp);
	ZVAL_RESOURCE(tmp, n);
	
	return zend_hash_update(Z_OBJPROP_P(arg), key, key_len, (void *) &tmp, sizeof(zval *), NULL);
}


ZEND_API int add_property_double_ex(zval *arg, char *key, uint key_len, double d)
{
	zval *tmp;
	
	MAKE_STD_ZVAL(tmp);
	ZVAL_DOUBLE(tmp, d);
	
	return zend_hash_update(Z_OBJPROP_P(arg), key, key_len, (void *) &tmp, sizeof(zval *), NULL);
}


ZEND_API int add_property_string_ex(zval *arg, char *key, uint key_len, char *str, int duplicate)
{
	zval *tmp;

	MAKE_STD_ZVAL(tmp);
	ZVAL_STRING(tmp, str, duplicate);

	return zend_hash_update(Z_OBJPROP_P(arg), key, key_len, (void *) &tmp, sizeof(zval *), NULL);
}

ZEND_API int add_property_stringl_ex(zval *arg, char *key, uint key_len, char *str, uint length, int duplicate)
{
	zval *tmp;

	MAKE_STD_ZVAL(tmp);
	ZVAL_STRINGL(tmp, str, length, duplicate);

	return zend_hash_update(Z_OBJPROP_P(arg), key, key_len, (void *) &tmp, sizeof(zval *), NULL);
}

ZEND_API int add_property_zval_ex(zval *arg, char *key, uint key_len, zval *value)
{
	return zend_hash_update(Z_OBJPROP_P(arg), key, key_len, (void *) &value, sizeof(zval *), NULL);
}
#elif defined(PHP5TS)
ZEND_API int add_property_long_ex(zval *arg, char *key, uint key_len, long n TSRMLS_DC)
{
	zval *tmp;
	zval *z_key;

	MAKE_STD_ZVAL(tmp);
	ZVAL_LONG(tmp, n);
	
	MAKE_STD_ZVAL(z_key);
	ZVAL_STRINGL(z_key, key, key_len-1, 1);

	Z_OBJ_HANDLER_P(arg, write_property)(arg, z_key, tmp TSRMLS_CC);
	zval_ptr_dtor(&tmp); /* write_property will add 1 to refcount */
	zval_ptr_dtor(&z_key);
	return SUCCESS;
}

ZEND_API int add_property_bool_ex(zval *arg, char *key, uint key_len, int b TSRMLS_DC)
{
	zval *tmp;
	zval *z_key;

	MAKE_STD_ZVAL(tmp);
	ZVAL_BOOL(tmp, b);

	MAKE_STD_ZVAL(z_key);
	ZVAL_STRINGL(z_key, key, key_len-1, 1);

	Z_OBJ_HANDLER_P(arg, write_property)(arg, z_key, tmp TSRMLS_CC);
	zval_ptr_dtor(&tmp); /* write_property will add 1 to refcount */
	zval_ptr_dtor(&z_key);
	return SUCCESS;
}

ZEND_API int add_property_null_ex(zval *arg, char *key, uint key_len TSRMLS_DC)
{
	zval *tmp;
	zval *z_key;
	
	MAKE_STD_ZVAL(tmp);
	ZVAL_NULL(tmp);
	
	MAKE_STD_ZVAL(z_key);
	ZVAL_STRINGL(z_key, key, key_len-1, 1);

	Z_OBJ_HANDLER_P(arg, write_property)(arg, z_key, tmp TSRMLS_CC);
	zval_ptr_dtor(&tmp); /* write_property will add 1 to refcount */
	zval_ptr_dtor(&z_key);
	return SUCCESS;
}

ZEND_API int add_property_resource_ex(zval *arg, char *key, uint key_len, long n TSRMLS_DC)
{
	zval *tmp;
	zval *z_key;
	
	MAKE_STD_ZVAL(tmp);
	ZVAL_RESOURCE(tmp, n);

	MAKE_STD_ZVAL(z_key);
	ZVAL_STRINGL(z_key, key, key_len-1, 1);

	Z_OBJ_HANDLER_P(arg, write_property)(arg, z_key, tmp TSRMLS_CC);
	zval_ptr_dtor(&tmp); /* write_property will add 1 to refcount */
	zval_ptr_dtor(&z_key);
	return SUCCESS;
}


ZEND_API int add_property_double_ex(zval *arg, char *key, uint key_len, double d TSRMLS_DC)
{
	zval *tmp;
	zval *z_key;

	MAKE_STD_ZVAL(tmp);
	ZVAL_DOUBLE(tmp, d);
	
	MAKE_STD_ZVAL(z_key);
	ZVAL_STRINGL(z_key, key, key_len-1, 1);

	Z_OBJ_HANDLER_P(arg, write_property)(arg, z_key, tmp TSRMLS_CC);
	zval_ptr_dtor(&tmp); /* write_property will add 1 to refcount */
	zval_ptr_dtor(&z_key);
	return SUCCESS;
}


ZEND_API int add_property_string_ex(zval *arg, char *key, uint key_len, char *str, int duplicate TSRMLS_DC)
{
	zval *tmp;
	zval *z_key;

	MAKE_STD_ZVAL(tmp);
	ZVAL_STRING(tmp, str, duplicate);

	MAKE_STD_ZVAL(z_key);
	ZVAL_STRINGL(z_key, key, key_len-1, 1);

	Z_OBJ_HANDLER_P(arg, write_property)(arg, z_key, tmp TSRMLS_CC);
	zval_ptr_dtor(&tmp); /* write_property will add 1 to refcount */
	zval_ptr_dtor(&z_key);
	return SUCCESS;
}

ZEND_API int add_property_stringl_ex(zval *arg, char *key, uint key_len, char *str, uint length, int duplicate TSRMLS_DC)
{
	zval *tmp;
	zval *z_key;

	MAKE_STD_ZVAL(tmp);
	ZVAL_STRINGL(tmp, str, length, duplicate);

	MAKE_STD_ZVAL(z_key);
	ZVAL_STRINGL(z_key, key, key_len-1, 1);

	Z_OBJ_HANDLER_P(arg, write_property)(arg, z_key, tmp TSRMLS_CC);
	zval_ptr_dtor(&tmp); /* write_property will add 1 to refcount */
	zval_ptr_dtor(&z_key);
	return SUCCESS;
}

ZEND_API int add_property_zval_ex(zval *arg, char *key, uint key_len, zval *value TSRMLS_DC)
{
	zval *z_key;

	MAKE_STD_ZVAL(z_key);
	ZVAL_STRINGL(z_key, key, key_len-1, 1);

	Z_OBJ_HANDLER_P(arg, write_property)(arg, z_key, value TSRMLS_CC);
	zval_ptr_dtor(&z_key);
	return SUCCESS;
}
#endif

// copied from array.c and beautified
ZEND_API int php_array_merge(HashTable *dest, HashTable *src, int recursive TSRMLS_DC)
{
	zval **src_entry, **dest_entry;
	char *string_key;
	uint string_key_len;
	ulong num_key;
	HashPosition pos;

	zend_hash_internal_pointer_reset_ex(src, &pos);
	while (zend_hash_get_current_data_ex(src, (void **)&src_entry, &pos) == SUCCESS)
	{
		switch (zend_hash_get_current_key_ex(src, &string_key, &string_key_len, &num_key, 0, &pos))
		{
			case HASH_KEY_IS_STRING:
				if (recursive &&
					zend_hash_find(dest, string_key, string_key_len, (void **)&dest_entry) == SUCCESS)
				{
					if (*src_entry == *dest_entry && ((*dest_entry)->refcount % 2))
					{
						php_error_docref(NULL TSRMLS_CC, E_WARNING, "recursion detected");
						return 0;
					}
					SEPARATE_ZVAL(dest_entry);
					SEPARATE_ZVAL(src_entry);
					
					convert_to_array_ex(dest_entry);
					convert_to_array_ex(src_entry);
					if (!php_array_merge(Z_ARRVAL_PP(dest_entry), Z_ARRVAL_PP(src_entry), recursive TSRMLS_CC))
					{
						return 0;
					}
				}
				else
				{
					(*src_entry)->refcount++;

					zend_hash_update(dest, string_key, strlen(string_key)+1, src_entry, sizeof(zval *), NULL);
				}
				break;

			case HASH_KEY_IS_LONG:
				(*src_entry)->refcount++;
				zend_hash_next_index_insert(dest, src_entry, sizeof(zval *), NULL);
				break;
		}

		zend_hash_move_forward_ex(src, &pos);
	}
	return 1;
}

// copied from zend_API.c
ZEND_API int zend_set_hash_symbol(zval *symbol, char *name, int name_length, zend_bool is_ref, int num_symbol_tables, ...)
{
    HashTable  *symbol_table;
    va_list symbol_table_list;

    if (num_symbol_tables <= 0) return FAILURE;

    symbol->is_ref = is_ref;

    va_start(symbol_table_list, num_symbol_tables);
    while (num_symbol_tables-- > 0)
	{
        symbol_table = va_arg(symbol_table_list, HashTable *);
        zend_hash_update(symbol_table, name, name_length + 1, &symbol, sizeof(zval *), NULL);
        zval_add_ref(&symbol);
    }
    va_end(symbol_table_list);
    return SUCCESS;
}
