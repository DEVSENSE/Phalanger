//
// ExtSupport.PHP4 - substitute for php4ts.dll
//
// Variables.h
// - contains declarations of variables related exported functions
//

#pragma once

#include "stdafx.h"
#include "ExtSupport.h"

// copied from php.h and beautified
#define PHP_STRLCPY(dst, src, size, src_size)	\
	{											\
		size_t php_str_len;						\
												\
		if (src_size >= size)					\
			php_str_len = size - 1;				\
		else									\
			php_str_len = src_size;				\
		memcpy(dst, src, php_str_len);			\
		dst[php_str_len] = '\0';				\
	}

#define strlcpy php_strlcpy

#ifdef PHP4TS
#define zval_copy_ctor(zvalue) _zval_copy_ctor((zvalue) ZEND_FILE_LINE_CC)
#define zval_dtor(zvalue) _zval_dtor((zvalue) ZEND_FILE_LINE_CC)
#elif defined(PHP5TS)
#define zval_copy_ctor(zvalue) _zval_copy_ctor_func((zvalue) ZEND_FILE_LINE_CC)
#define zval_dtor(zvalue) _zval_dtor_func((zvalue) ZEND_FILE_LINE_CC)
#endif

#define zval_ptr_dtor(zval_ptr) _zval_ptr_dtor((zval_ptr) ZEND_FILE_LINE_CC)
#define zval_internal_dtor(zvalue) _zval_internal_dtor((zvalue) ZEND_FILE_LINE_CC)
#define zval_internal_ptr_dtor(zvalue) _zval_internal_ptr_dtor((zvalue) ZEND_FILE_LINE_CC)

int hash_zval_compare_function(const zval **z1, const zval **z2 TSRMLS_DC);

#define ZEND_NOP					0
									
#define ZEND_ADD					1
#define ZEND_SUB					2
#define ZEND_MUL					3
#define ZEND_DIV					4
#define ZEND_MOD					5
#define ZEND_SL						6
#define ZEND_SR						7
#define ZEND_CONCAT					8
#define ZEND_BW_OR					9
#define ZEND_BW_AND					10
#define ZEND_BW_XOR					11
#define ZEND_BW_NOT					12
#define ZEND_BOOL_NOT				13
#define ZEND_BOOL_XOR				14
#define ZEND_IS_IDENTICAL			15
#define ZEND_IS_NOT_IDENTICAL       16
#define ZEND_IS_EQUAL				17
#define ZEND_IS_NOT_EQUAL			18
#define ZEND_IS_SMALLER				19
#define ZEND_IS_SMALLER_OR_EQUAL	20
#define ZEND_CAST					21
#define ZEND_QM_ASSIGN				22

#define ZEND_ASSIGN_ADD				23
#define ZEND_ASSIGN_SUB				24
#define ZEND_ASSIGN_MUL				25
#define ZEND_ASSIGN_DIV				26
#define ZEND_ASSIGN_MOD				27
#define ZEND_ASSIGN_SL				28
#define ZEND_ASSIGN_SR				29
#define ZEND_ASSIGN_CONCAT			30
#define ZEND_ASSIGN_BW_OR			31
#define ZEND_ASSIGN_BW_AND			32
#define ZEND_ASSIGN_BW_XOR			33
									
#define ZEND_PRE_INC				34
#define ZEND_PRE_DEC				35
#define ZEND_POST_INC				36
#define ZEND_POST_DEC				37
								 	
#define ZEND_ASSIGN					38
#define ZEND_ASSIGN_REF				39

#define ZEND_ECHO					40
#define ZEND_PRINT					41

typedef int (*unary_op_type)(zval *, zval *);

BEGIN_EXTERN_C()

extern ZEND_API char *empty_string;
extern ZEND_API zval zval_used_for_init;
extern ZEND_API zend_utility_values zend_uv;

extern ZEND_API void (*php_import_environment_variables)(zval *array_ptr TSRMLS_DC);

ZEND_API char *zend_zval_type_name(zval *arg);

#ifdef PHP4TS
ZEND_API int _zval_copy_ctor(zval *zvalue ZEND_FILE_LINE_DC);
ZEND_API void _zval_dtor(zval *zvalue ZEND_FILE_LINE_DC);
#elif defined(PHP5TS)
ZEND_API void _zval_copy_ctor_func(zval *zvalue ZEND_FILE_LINE_DC);
ZEND_API void _zval_dtor_func(zval *zvalue ZEND_FILE_LINE_DC);
#endif

ZEND_API void _zval_internal_dtor(zval *zvalue ZEND_FILE_LINE_DC);
ZEND_API void _zval_ptr_dtor(zval **zval_ptr ZEND_FILE_LINE_DC);
ZEND_API void _zval_internal_ptr_dtor(zval **zval_ptr ZEND_FILE_LINE_DC);
ZEND_API void zval_add_ref(zval **p);

ZEND_API void convert_scalar_to_number(zval *op TSRMLS_DC);
ZEND_API void convert_to_array(zval *op);
ZEND_API void convert_to_boolean(zval *op);
ZEND_API void convert_to_double(zval *op);
ZEND_API void convert_to_long(zval *op);
ZEND_API void convert_to_long_base(zval *op, int base);
ZEND_API void convert_to_null(zval *op);
ZEND_API void convert_to_object(zval *op);
ZEND_API void _convert_to_string(zval *op ZEND_FILE_LINE_DC);

ZEND_API void zend_str_tolower(char *str, unsigned int length);
ZEND_API char *zend_str_tolower_copy(char *dest, const char *source, unsigned int length);
ZEND_API int zend_binary_zval_strcmp(zval *s1, zval *s2);
ZEND_API int zend_binary_zval_strncmp(zval *s1, zval *s2, zval *s3);
ZEND_API int zend_binary_zval_strcasecmp(zval *s1, zval *s2);
ZEND_API int zend_binary_zval_strncasecmp(zval *s1, zval *s2, zval *s3);
ZEND_API int zend_binary_strcmp(char *s1, uint len1, char *s2, uint len2);
ZEND_API int zend_binary_strncmp(char *s1, uint len1, char *s2, uint len2, uint length);
ZEND_API int zend_binary_strcasecmp(char *s1, uint len1, char *s2, uint len2);
ZEND_API int zend_binary_strncasecmp(char *s1, uint len1, char *s2, uint len2, uint length);

ZEND_API void zendi_smart_strcmp(zval *result, zval *s1, zval *s2);
ZEND_API void zend_compare_symbol_tables(zval *result, HashTable *ht1, HashTable *ht2 TSRMLS_DC);
ZEND_API void zend_compare_arrays(zval *result, zval *a1, zval *a2 TSRMLS_DC);
ZEND_API void zend_compare_objects(zval *result, zval *o1, zval *o2 TSRMLS_DC);

ZEND_API int zend_atoi(const char *str, int str_len);
ZEND_API long zend_atol(const char *str, int str_len);

// copied from zend_strtod.h
ZEND_API double zend_hex_strtod(const char *str, char **endptr);
ZEND_API double zend_oct_strtod(const char *str, char **endptr);

ZEND_API int compare_function(zval *result, zval *op1, zval *op2 TSRMLS_DC);
ZEND_API int numeric_compare_function(zval *result, zval *op1, zval *op2 TSRMLS_DC);
ZEND_API int string_compare_function(zval *result, zval *op1, zval *op2 TSRMLS_DC);
ZEND_API int string_locale_compare_function(zval *result, zval *op1, zval *op2 TSRMLS_DC);

ZEND_API void zend_make_printable_zval(zval *expr, zval *expr_copy, int *use_copy);
ZEND_API void zend_locale_sprintf_double(zval *op ZEND_FILE_LINE_DC);

ZEND_API size_t php_strlcpy(char *dst, const char *src, size_t siz);

ZEND_API int bitwise_and_function(zval *result, zval *op1, zval *op2 TSRMLS_DC);
ZEND_API int bitwise_not_function(zval *result, zval *op1 TSRMLS_DC);
ZEND_API int bitwise_or_function(zval *result, zval *op1, zval *op2 TSRMLS_DC);
ZEND_API int bitwise_xor_function(zval *result, zval *op1, zval *op2 TSRMLS_DC);
ZEND_API int boolean_not_function(zval *result, zval *op1 TSRMLS_DC);
ZEND_API int boolean_xor_function(zval *result, zval *op1, zval *op2 TSRMLS_DC);

ZEND_API int add_char_to_string(zval *result, zval *op1, zval *op2);
ZEND_API int add_string_to_string(zval *result, zval *op1, zval *op2);

ZEND_API int zval_is_true(zval *op);
ZEND_API int zval_update_constant(zval **pp, void *arg TSRMLS_DC);

ZEND_API int zend_is_true(zval *op);

ZEND_API double zend_string_to_double(const char *number, zend_uint length);

ZEND_API int concat_function(zval *result, zval *op1, zval *op2 TSRMLS_DC);
ZEND_API int add_function(zval *result, zval *op1, zval *op2 TSRMLS_DC);
ZEND_API int sub_function(zval *result, zval *op1, zval *op2 TSRMLS_DC);
ZEND_API int decrement_function(zval *op1);
ZEND_API int increment_function(zval *op1);
ZEND_API int div_function(zval *result, zval *op1, zval *op2 TSRMLS_DC);
ZEND_API int mod_function(zval *result, zval *op1, zval *op2 TSRMLS_DC);
ZEND_API int mul_function(zval *result, zval *op1, zval *op2 TSRMLS_DC);
ZEND_API int shift_left_function(zval *result, zval *op1, zval *op2 TSRMLS_DC);
ZEND_API int shift_right_function(zval *result, zval *op1, zval *op2 TSRMLS_DC);

ZEND_API int is_identical_function(zval *result, zval *op1, zval *op2 TSRMLS_DC);
ZEND_API int is_not_identical_function(zval *result, zval *op1, zval *op2 TSRMLS_DC);
ZEND_API int is_equal_function(zval *result, zval *op1, zval *op2 TSRMLS_DC);
ZEND_API int is_not_equal_function(zval *result, zval *op1, zval *op2 TSRMLS_DC);
ZEND_API int is_smaller_function(zval *result, zval *op1, zval *op2 TSRMLS_DC);
ZEND_API int is_smaller_or_equal_function(zval *result, zval *op1, zval *op2 TSRMLS_DC);

ZEND_API void multi_convert_to_long_ex(int argc, ...);
ZEND_API void multi_convert_to_double_ex(int argc, ...);
ZEND_API void multi_convert_to_string_ex(int argc, ...);

ZEND_API void *get_binary_op(int opcode);
ZEND_API unary_op_type get_unary_op(int opcode);

ZEND_API zend_bool zend_is_callable(zval *callable, uint check_flags, char **callable_name TSRMLS_DC);

#ifdef PHP5TS
ZEND_API zend_bool zend_is_callable_ex(zval *callable, uint check_flags, char **callable_name, int *callable_name_len, zend_class_entry **ce_ptr, union _zend_function **fptr_ptr, zval ***zobj_ptr_ptr TSRMLS_DC);

ZEND_API zend_bool instanceof_function_ex(zend_class_entry *instance_ce, zend_class_entry *ce, zend_bool interfaces_only TSRMLS_DC);
ZEND_API zend_bool instanceof_function(zend_class_entry *instance_ce, zend_class_entry *ce TSRMLS_DC);

ZEND_API void zend_rebuild_symbol_table(TSRMLS_D);

#endif

END_EXTERN_C()