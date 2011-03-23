//
// ExtSupport.PHP4 - substitute for php4ts.dll
//
// Functions.h
// - contains declarations of function related exported functions
//

#pragma once

#include "stdafx.h"
#include "ExtSupport.h"

#include "Hash.h"

#define ZEND_MAX_RESERVED_RESOURCES	4

#define ZEND_INTERNAL_FUNCTION				1
#define ZEND_USER_FUNCTION					2
#define ZEND_OVERLOADED_FUNCTION			3
#define	ZEND_EVAL_CODE						4
#define ZEND_OVERLOADED_FUNCTION_TEMPORARY	5

#define IS_CALLABLE_CHECK_SYNTAX_ONLY (1<<0)
#define IS_CALLABLE_CHECK_NO_ACCESS   (1<<1)
#define IS_CALLABLE_CHECK_IS_STATIC   (1<<2)

#define IS_CALLABLE_STRICT  (IS_CALLABLE_CHECK_IS_STATIC)

#define ZEND_FN_SCOPE_NAME(function)  ((function) && (function)->common.scope ? (function)->common.scope->name : "")

#ifdef PHP4TS
typedef struct _zend_arg_info
{
	char *name;
	zend_uint name_len;
	char *class_name;
	zend_uint class_name_len;
	zend_bool allow_null;
	zend_bool pass_by_reference;
}
zend_arg_info;
#elif defined(PHP5TS)
typedef struct _zend_arg_info {
	char *name;
	zend_uint name_len;
	char *class_name;
	zend_uint class_name_len;
	zend_bool array_type_hint;
	zend_bool allow_null;
	zend_bool pass_by_reference;
	zend_bool return_reference;
	int required_num_args;
} zend_arg_info;
#endif

struct _zend_class_entry;

typedef struct _zend_op_array
{
	/* Common elements */
	zend_uchar type;
	char *function_name;		
	struct _zend_class_entry *scope;
	zend_uint fn_flags;
	union _zend_function *prototype;
	zend_uint num_args;
	zend_arg_info *arg_info;
	zend_bool pass_rest_by_reference;
	/* END of common elements */

	zend_uint *refcount;

	/*zend_op*/void *opcodes;
	zend_uint last, size;

	zend_uint T;

	/*zend_brk_cont_element*/void *brk_cont_array;
	zend_uint last_brk_cont;
	zend_uint current_brk_cont;

	/* static variables support */
	HashTable *static_variables;

	/*zend_op*/void *start_op;
	int backpatch_count;

	zend_bool return_reference;
	zend_bool done_pass_two;
	zend_bool uses_this;

	char *filename;
	zend_uint line_start;
	zend_uint line_end;
	char *doc_comment;
	zend_uint doc_comment_len;

	void *reserved[ZEND_MAX_RESERVED_RESOURCES];
}
zend_op_array;

#ifdef PHP4TS
typedef struct _zend_internal_function
{
	/* Common elements */
	zend_uchar type;
	char *function_name;		
	struct _zend_class_entry *scope;
	zend_uint fn_flags;	
	union _zend_function *prototype;
	zend_uint num_args;
	zend_arg_info *arg_info;
	zend_bool pass_rest_by_reference;
	/* END of common elements */

	void (*handler)(INTERNAL_FUNCTION_PARAMETERS);
}
zend_internal_function;

typedef union _zend_function
{
	zend_uchar type;	/* MUST be the first element of this struct! */

	struct
	{
		zend_uchar type;  /* never used */
		char *function_name;
		struct _zend_class_entry *scope;
		zend_uint fn_flags;
		union _zend_function *prototype;
		zend_uint num_args;
		zend_arg_info *arg_info;
		zend_bool pass_rest_by_reference;
	}
	common;
	
	zend_op_array op_array;
	zend_internal_function internal_function;
}
zend_function;
#elif defined(PHP5TS)
#define ZEND_RETURN_VALUE				0
#define ZEND_RETURN_REFERENCE			1

typedef struct _zend_internal_function {
	/* Common elements */
	zend_uchar type;
	char * function_name;
	zend_class_entry *scope;
	zend_uint fn_flags;
	union _zend_function *prototype;
	zend_uint num_args;
	zend_uint required_num_args;
	zend_arg_info *arg_info;
	zend_bool pass_rest_by_reference;
	unsigned char return_reference;
	/* END of common elements */

	void (*handler)(INTERNAL_FUNCTION_PARAMETERS);
	struct _zend_module_entry *module;
} zend_internal_function;

typedef union _zend_function {
	zend_uchar type;	/* MUST be the first element of this struct! */

	struct {
		zend_uchar type;  /* never used */
		char *function_name;
		zend_class_entry *scope;
		zend_uint fn_flags;
		union _zend_function *prototype;
		zend_uint num_args;
		zend_uint required_num_args;
		zend_arg_info *arg_info;
		zend_bool pass_rest_by_reference;
		unsigned char return_reference;
	} common;

	zend_op_array op_array;
	zend_internal_function internal_function;
} zend_function;

struct _zend_serialize_data;
struct _zend_unserialize_data;

typedef struct _zend_serialize_data zend_serialize_data;
typedef struct _zend_unserialize_data zend_unserialize_data;

#endif

typedef struct _zend_fcall_info {
	size_t size;
	HashTable *function_table;
	zval *function_name;
	HashTable *symbol_table;
	zval **retval_ptr_ptr;
	zend_uint param_count;
	zval ***params;
	zval *object_ptr;
	zend_bool no_separation;
} zend_fcall_info;

typedef struct _zend_fcall_info_cache {
	zend_bool initialized;
	zend_function *function_handler;
	zend_class_entry *calling_scope;
	zend_class_entry *called_scope;
	zval *object_ptr;
} zend_fcall_info_cache;

//typedef struct _zend_fcall_info
//{
//	size_t size;
//	HashTable *function_table;
//	zval *function_name;
//	HashTable *symbol_table;
//	zval **retval_ptr_ptr;
//	zend_uint param_count;
//	zval ***params;
//	zval **object_pp;
//	zend_bool no_separation;
//}
//zend_fcall_info;
//
//typedef struct _zend_fcall_info_cache
//{
//	zend_bool initialized;
//	zend_function *function_handler;
//	struct _zend_class_entry *calling_scope;
//	zval **object_pp;
//}
//zend_fcall_info_cache;

typedef struct _zend_function_state
{
	HashTable *function_symbol_table;
	zend_function *function;
	void *reserved[ZEND_MAX_RESERVED_RESOURCES];
} zend_function_state;

BEGIN_EXTERN_C()

ZEND_API int call_user_function(HashTable *function_table, zval **object_pp, zval *function_name, zval *retval_ptr, int param_count, zval *params[] TSRMLS_DC);
ZEND_API int call_user_function_ex(HashTable *function_table, zval **object_pp, zval *function_name, zval **retval_ptr_ptr, int param_count, zval **params[], int no_separation, HashTable *symbol_table TSRMLS_DC);

ZEND_API int zend_call_function(zend_fcall_info *fci, zend_fcall_info_cache *fci_cache TSRMLS_DC);

ZEND_API void destroy_zend_function(zend_function *function TSRMLS_DC);

#ifdef PHP5TS
ZEND_API int zend_fcall_info_init(zval *callable, zend_fcall_info *fci, zend_fcall_info_cache *fcc TSRMLS_DC);

ZEND_API extern const zend_fcall_info empty_fcall_info;
ZEND_API extern const zend_fcall_info_cache empty_fcall_info_cache;

#endif

END_EXTERN_C()