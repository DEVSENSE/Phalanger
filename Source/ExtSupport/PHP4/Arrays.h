//
// ExtSupport.PHP4 - substitute for php4ts.dll
//
// Arrays.h
// - contains declarations of arrays related exported functions
//

#pragma once

#include "stdafx.h"
#include "ExtSupport.h"

#define array_init(arg)			_array_init((arg) ZEND_FILE_LINE_CC)

#ifdef __cplusplus
extern "C"
{
#endif

ZEND_API int _array_init(zval *arg ZEND_FILE_LINE_DC);

ZEND_API int add_assoc_function(zval *arg, char *key, void (*function_ptr)(INTERNAL_FUNCTION_PARAMETERS));
ZEND_API int add_assoc_long_ex(zval *arg, char *key, uint key_len, long n);
ZEND_API int add_assoc_null_ex(zval *arg, char *key, uint key_len);
ZEND_API int add_assoc_bool_ex(zval *arg, char *key, uint key_len, int b);
ZEND_API int add_assoc_resource_ex(zval *arg, char *key, uint key_len, int r);
ZEND_API int add_assoc_double_ex(zval *arg, char *key, uint key_len, double d);
ZEND_API int add_assoc_string_ex(zval *arg, char *key, uint key_len, char *str, int duplicate);
ZEND_API int add_assoc_stringl_ex(zval *arg, char *key, uint key_len, char *str, uint length, int duplicate);
ZEND_API int add_assoc_zval_ex(zval *arg, char *key, uint key_len, zval *value);
ZEND_API int add_index_long(zval *arg, uint index, long n);
ZEND_API int add_index_null(zval *arg, uint index);
ZEND_API int add_index_bool(zval *arg, uint index, int b);
ZEND_API int add_index_resource(zval *arg, uint index, int r);
ZEND_API int add_index_double(zval *arg, uint index, double d);
ZEND_API int add_index_string(zval *arg, uint index, char *str, int duplicate);
ZEND_API int add_index_stringl(zval *arg, uint index, char *str, uint length, int duplicate);
ZEND_API int add_index_zval(zval *arg, uint index, zval *value);
ZEND_API int add_next_index_long(zval *arg, long n);
ZEND_API int add_next_index_null(zval *arg);
ZEND_API int add_next_index_bool(zval *arg, int b);
ZEND_API int add_next_index_resource(zval *arg, int r);
ZEND_API int add_next_index_double(zval *arg, double d);
ZEND_API int add_next_index_string(zval *arg, char *str, int duplicate);
ZEND_API int add_next_index_stringl(zval *arg, char *str, uint length, int duplicate);
ZEND_API int add_next_index_zval(zval *arg, zval *value);
ZEND_API int add_get_assoc_string_ex(zval *arg, char *key, uint key_len, char *str, void **dest, int duplicate);
ZEND_API int add_get_assoc_stringl_ex(zval *arg, char *key, uint key_len, char *str, uint length, void **dest, int duplicate);
ZEND_API int add_get_index_long(zval *arg, uint index, long l, void **dest);
ZEND_API int add_get_index_double(zval *arg, uint index, double d, void **dest);
ZEND_API int add_get_index_string(zval *arg, uint index, char *str, void **dest, int duplicate);
ZEND_API int add_get_index_stringl(zval *arg, uint index, char *str, uint length, void **dest, int duplicate);
ZEND_API int add_property_long_ex(zval *arg, char *key, uint key_len, long n);
ZEND_API int add_property_bool_ex(zval *arg, char *key, uint key_len, int b);
ZEND_API int add_property_null_ex(zval *arg, char *key, uint key_len);
ZEND_API int add_property_resource_ex(zval *arg, char *key, uint key_len, long n);
ZEND_API int add_property_double_ex(zval *arg, char *key, uint key_len, double d);
ZEND_API int add_property_string_ex(zval *arg, char *key, uint key_len, char *str, int duplicate);
ZEND_API int add_property_stringl_ex(zval *arg, char *key, uint key_len, char *str, uint length, int duplicate);
ZEND_API int add_property_zval_ex(zval *arg, char *key, uint key_len, zval *value);

#define add_assoc_long(__arg, __key, __n) add_assoc_long_ex(__arg, __key, strlen(__key)+1, __n)
#define add_assoc_null(__arg, __key) add_assoc_null_ex(__arg, __key, strlen(__key) + 1)
#define add_assoc_bool(__arg, __key, __b) add_assoc_bool_ex(__arg, __key, strlen(__key)+1, __b)
#define add_assoc_resource(__arg, __key, __r) add_assoc_resource_ex(__arg, __key, strlen(__key)+1, __r)
#define add_assoc_double(__arg, __key, __d) add_assoc_double_ex(__arg, __key, strlen(__key)+1, __d)
#define add_assoc_string(__arg, __key, __str, __duplicate) add_assoc_string_ex(__arg, __key, strlen(__key)+1, __str, __duplicate)
#define add_assoc_stringl(__arg, __key, __str, __length, __duplicate) add_assoc_stringl_ex(__arg, __key, strlen(__key)+1, __str, __length, __duplicate)
#define add_assoc_zval(__arg, __key, __value) add_assoc_zval_ex(__arg, __key, strlen(__key)+1, __value)

/* unset() functions are only suported for legacy modules and null() functions should be used */
#define add_assoc_unset(__arg, __key) add_assoc_null_ex(__arg, __key, strlen(__key) + 1)
#define add_index_unset(__arg, __key) add_index_null(__arg, __key)
#define add_next_index_unset(__arg) add_next_index_null(__arg)
#define add_property_unset(__arg, __key) add_property_null(__arg, __key)

ZEND_API int php_array_merge(HashTable *dest, HashTable *src, int recursive TSRMLS_DC);
ZEND_API int zend_set_hash_symbol(zval *symbol, char *name, int name_length, zend_bool is_ref, int num_symbol_tables, ...);

#ifdef __cplusplus
}
#endif
