//
// ExtSupport.PHP4 - substitute for php4ts.dll
//
// Objects.h
// - contains declarations of object related exported functions
//

#pragma once

#include "stdafx.h"
#include "ExtSupport.h"
#include "Hash.h"
#include "Functions.h"
#include "LinkedLists.h"
#include "ObjectHandlers.h"

#define ZEND_INTERNAL_CLASS		1
#define ZEND_USER_CLASS			2

#define ZEND_FUNCTION_DTOR (void (*)(void *))destroy_zend_function
#define ZEND_CLASS_DTOR (void (*)(void *))destroy_zend_class

#define object_init(arg)		_object_init((arg) ZEND_FILE_LINE_CC TSRMLS_CC)
#define object_init_ex(arg, ce)	_object_init_ex((arg), (ce) ZEND_FILE_LINE_CC TSRMLS_CC)
#define object_and_properties_init(arg, ce, properties)	_object_and_properties_init((arg), (ce), (properties) ZEND_FILE_LINE_CC TSRMLS_CC)

typedef struct _zend_property_reference
{
	int type;  /* read, write or r/w */
	zval *object;
	zend_llist *elements_list;
}
zend_property_reference;

#ifdef PHP4TS
struct _zend_class_entry
{
	char type;
	char *name;
	uint name_length;
	struct _zend_class_entry *parent; 
	int *refcount;
	zend_bool constants_updated;

	HashTable function_table;
	HashTable default_properties;
	zend_function_entry *builtin_functions;

	/* handlers */
	void (*handle_function_call)(INTERNAL_FUNCTION_PARAMETERS, zend_property_reference *property_reference);
	zval (*handle_property_get)(zend_property_reference *property_reference);
	int (*handle_property_set)(zend_property_reference *property_reference, zval *value);
};

int zend_check_class(zval *obj, zend_class_entry *expected_ce);

#elif defined(PHP5TS)

/* method flags (types) */
#define ZEND_ACC_STATIC			0x01
#define ZEND_ACC_ABSTRACT		0x02
#define ZEND_ACC_FINAL			0x04
#define ZEND_ACC_IMPLEMENTED_ABSTRACT		0x08

/* class flags (types) */
/* ZEND_ACC_IMPLICIT_ABSTRACT_CLASS is used for abstract classes (since it is set by any abstract method even interfaces MAY have it set, too). */
/* ZEND_ACC_EXPLICIT_ABSTRACT_CLASS denotes that a class was explicitly defined as abstract by using the keyword. */
#define ZEND_ACC_IMPLICIT_ABSTRACT_CLASS	0x10
#define ZEND_ACC_EXPLICIT_ABSTRACT_CLASS	0x20
#define ZEND_ACC_FINAL_CLASS	            0x40
#define ZEND_ACC_INTERFACE		            0x80

/* op_array flags */
#define ZEND_ACC_INTERACTIVE				0x10

/* method flags (visibility) */
/* The order of those must be kept - public < protected < private */
#define ZEND_ACC_PUBLIC		0x100
#define ZEND_ACC_PROTECTED	0x200
#define ZEND_ACC_PRIVATE	0x400
#define ZEND_ACC_PPP_MASK  (ZEND_ACC_PUBLIC | ZEND_ACC_PROTECTED | ZEND_ACC_PRIVATE)

#define ZEND_ACC_CHANGED	0x800
#define ZEND_ACC_IMPLICIT_PUBLIC	0x1000

/* method flags (special method detection) */
#define ZEND_ACC_CTOR		0x2000
#define ZEND_ACC_DTOR		0x4000
#define ZEND_ACC_CLONE		0x8000

/* method flag (bc only), any method that has this flag can be used statically and non statically. */
#define ZEND_ACC_ALLOW_STATIC	0x10000

/* shadow of parent's private method/property */
#define ZEND_ACC_SHADOW 0x20000

/* deprecation flag */
#define ZEND_ACC_DEPRECATED 0x40000

/* var status for backpatching */
#define BP_VAR_R			0
#define BP_VAR_W			1
#define BP_VAR_RW			2
#define BP_VAR_IS			3
#define BP_VAR_NA			4	/* if not applicable */
#define BP_VAR_FUNC_ARG		5
#define BP_VAR_UNSET		6

typedef struct _zend_property_info {
	zend_uint flags;
	char *name;
	int name_length;
	ulong h;
	char *doc_comment;
	int doc_comment_len;
	zend_class_entry *ce;
} zend_property_info;

typedef struct _zend_object_iterator zend_object_iterator;

typedef struct _zend_object_iterator_funcs {
	/* release all resources associated with this iterator instance */
	void (*dtor)(zend_object_iterator *iter TSRMLS_DC);

	/* check for end of iteration (FAILURE or SUCCESS if data is valid) */
	int (*valid)(zend_object_iterator *iter TSRMLS_DC);

	/* fetch the item data for the current element */
	void (*get_current_data)(zend_object_iterator *iter, zval ***data TSRMLS_DC);

	/* fetch the key for the current element (return HASH_KEY_IS_STRING or HASH_KEY_IS_LONG) (optional, may be NULL) */
	int (*get_current_key)(zend_object_iterator *iter, char **str_key, uint *str_key_len, ulong *int_key TSRMLS_DC);

	/* step forwards to next element */
	void (*move_forward)(zend_object_iterator *iter TSRMLS_DC);

	/* rewind to start of data (optional, may be NULL) */
	void (*rewind)(zend_object_iterator *iter TSRMLS_DC);

	/* invalidate current value/key (optional, may be NULL) */
	void (*invalidate_current)(zend_object_iterator *iter TSRMLS_DC);
} zend_object_iterator_funcs;

struct _zend_object_iterator {
	void *data;
	zend_object_iterator_funcs *funcs;
	ulong index; /* private to fe_reset/fe_fetch opcodes */
};

typedef struct _zend_class_iterator_funcs {
	zend_object_iterator_funcs  *funcs;
	union _zend_function *zf_new_iterator;
	union _zend_function *zf_valid;
	union _zend_function *zf_current;
	union _zend_function *zf_key;
	union _zend_function *zf_next;
	union _zend_function *zf_rewind;
} zend_class_iterator_funcs;

struct _zend_class_entry {
	char type;
	char *name;
	zend_uint name_length;
	struct _zend_class_entry *parent;
	int refcount;
	zend_bool constants_updated;
	zend_uint ce_flags;

	HashTable function_table;
	HashTable default_properties;
	HashTable properties_info;
	HashTable default_static_members;
	HashTable *static_members;
	HashTable constants_table;
	zend_function_entry *builtin_functions;

	union _zend_function *constructor;
	union _zend_function *destructor;
	union _zend_function *clone;
	union _zend_function *__get;
	union _zend_function *__set;
	union _zend_function *__unset;
	union _zend_function *__isset;
	union _zend_function *__call;
	union _zend_function *__callstatic;
	union _zend_function *__tostring;
	union _zend_function *serialize_func;
	union _zend_function *unserialize_func;

	zend_class_iterator_funcs iterator_funcs;

	/* handlers */
	zend_object_value (*create_object)(zend_class_entry *class_type TSRMLS_DC);
	zend_object_iterator *(*get_iterator)(zend_class_entry *ce, zval *object, int by_ref TSRMLS_DC);
	int (*interface_gets_implemented)(zend_class_entry *iface, zend_class_entry *class_type TSRMLS_DC); /* a class implements this interface */
	union _zend_function *(*get_static_method)(zend_class_entry *ce, char* method, int method_len TSRMLS_DC);

	/* serializer callbacks */
	int (*serialize)(zval *object, unsigned char **buffer, zend_uint *buf_len, zend_serialize_data *data TSRMLS_DC);
	int (*unserialize)(zval **object, zend_class_entry *ce, const unsigned char *buf, zend_uint buf_len, zend_unserialize_data *data TSRMLS_DC);

	zend_class_entry **interfaces;
	zend_uint num_interfaces;

	char *filename;
	zend_uint line_start;
	zend_uint line_end;
	char *doc_comment;
	zend_uint doc_comment_len;

	struct _zend_module_entry *module;
};

ZEND_API void zend_initialize_class_data(zend_class_entry *ce, zend_bool nullify_handlers TSRMLS_DC);
ZEND_API zend_class_entry *zend_get_class_entry(zval *zobject TSRMLS_DC);
#endif

void register_standard_class(TSRMLS_D);

#define zend_stdClass_ptr &zend_standard_class_def


#define convert_to_explicit_type(pzv, type)		\
    do {										\
		switch (type) {							\
			case IS_NULL:						\
				convert_to_null(pzv);			\
				break;							\
			case IS_LONG:						\
				convert_to_long(pzv);			\
				break;							\
			case IS_DOUBLE: 					\
				convert_to_double(pzv); 		\
				break; 							\
			case IS_BOOL: 						\
				convert_to_boolean(pzv); 		\
				break; 							\
			case IS_ARRAY: 						\
				convert_to_array(pzv); 			\
				break; 							\
			case IS_OBJECT: 					\
				convert_to_object(pzv); 		\
				break; 							\
			case IS_STRING: 					\
				convert_to_string(pzv); 		\
				break; 							\
			default: 							\
				assert(0); 						\
				break; 							\
		}										\
	} while (0);								\

#define convert_to_explicit_type_ex(ppzv, str_type)	\
	if (Z_TYPE_PP(ppzv) != str_type) {				\
		SEPARATE_ZVAL_IF_NOT_REF(ppzv);				\
		convert_to_explicit_type(*ppzv, str_type);	\
	}


#ifdef PHP5TS
typedef void (*zend_objects_store_dtor_t)(void *object, zend_object_handle handle TSRMLS_DC);
typedef void (*zend_objects_free_object_storage_t)(void *object TSRMLS_DC);
typedef void (*zend_objects_store_clone_t)(void *object, void **object_clone TSRMLS_DC);

typedef struct _zend_object_store_bucket {
	zend_bool destructor_called;
	zend_bool valid;
	union _store_bucket {
		struct _store_object {
			void *object;
			zend_objects_store_dtor_t dtor;
			zend_objects_free_object_storage_t free_storage;
			zend_objects_store_clone_t clone;
			zend_uint refcount;
		} obj;
		struct {
			int next;
		} free_list;
	} bucket;
} zend_object_store_bucket;

typedef struct _zend_objects_store {
	zend_object_store_bucket *object_buckets;
	zend_uint top;
	zend_uint size;
	int free_list_head;
} zend_objects_store;


#if 0 /* defined(ZTS) */
#	define CE_STATIC_MEMBERS(ce) (((ce)->type==ZEND_USER_CLASS)?(ce)->static_members:CG(static_members)[(zend_intptr_t)(ce)->static_members])
#else
#	define CE_STATIC_MEMBERS(ce) ((ce)->static_members)
#endif

#endif

BEGIN_EXTERN_C()

ZEND_API void zend_object_std_init(zend_object *object, zend_class_entry *ce TSRMLS_DC);
ZEND_API void zend_object_std_dtor(zend_object *object TSRMLS_DC);
ZEND_API zend_object_value zend_objects_new(zend_object **object, zend_class_entry *class_type TSRMLS_DC);
ZEND_API void zend_objects_destroy_object(zend_object *object, zend_object_handle handle TSRMLS_DC);
ZEND_API zend_object *zend_objects_get_address(zval *object TSRMLS_DC);
ZEND_API void zend_objects_clone_members(zend_object *new_object, zend_object_value new_obj_val, zend_object *old_object, zend_object_handle handle TSRMLS_DC);
ZEND_API zend_object_value zend_objects_clone_obj(zval *object TSRMLS_DC);
ZEND_API void zend_objects_free_object_storage(zend_object *object TSRMLS_DC);

extern ZEND_API zend_class_entry zend_standard_class_def;

ZEND_API zend_class_entry *zend_register_internal_class(zend_class_entry *class_entry TSRMLS_DC);
ZEND_API zend_class_entry *zend_register_internal_class_ex(zend_class_entry *class_entry, zend_class_entry *parent_ce, char *parent_name TSRMLS_DC);

ZEND_API int _object_init(zval *arg ZEND_FILE_LINE_DC TSRMLS_DC);
ZEND_API int _object_init_ex(zval *arg, zend_class_entry *class_type ZEND_FILE_LINE_DC TSRMLS_DC);
ZEND_API int _object_and_properties_init(zval *arg, zend_class_entry *class_type, HashTable *properties ZEND_FILE_LINE_DC TSRMLS_DC);

#ifdef PHP4TS
ZEND_API void destroy_zend_class(zend_class_entry *ce);
#elif defined(PHP5TS)
ZEND_API void destroy_zend_class(zend_class_entry **pce);

// copied from zend_API.h
ZEND_API int zend_declare_class_constant(zend_class_entry *ce, const char *name, size_t name_length, zval *value TSRMLS_DC);
ZEND_API int zend_declare_class_constant_null(zend_class_entry *ce, const char *name, size_t name_length TSRMLS_DC);
ZEND_API int zend_declare_class_constant_long(zend_class_entry *ce, const char *name, size_t name_length, long value TSRMLS_DC);
ZEND_API int zend_declare_class_constant_bool(zend_class_entry *ce, const char *name, size_t name_length, zend_bool value TSRMLS_DC);
ZEND_API int zend_declare_class_constant_double(zend_class_entry *ce, const char *name, size_t name_length, double value TSRMLS_DC);
ZEND_API int zend_declare_class_constant_stringl(zend_class_entry *ce, const char *name, size_t name_length, const char *value, size_t value_length TSRMLS_DC);
ZEND_API int zend_declare_class_constant_string(zend_class_entry *ce, const char *name, size_t name_length, const char *value TSRMLS_DC);

ZEND_API zval *zend_read_property(zend_class_entry *scope, zval *object, char *name, int name_length, zend_bool silent TSRMLS_DC);

/* Store API functions */
ZEND_API zend_object_handle zend_objects_store_put(void *object, zend_objects_store_dtor_t dtor, zend_objects_free_object_storage_t storage, zend_objects_store_clone_t clone TSRMLS_DC);


ZEND_API void zend_objects_store_init(zend_objects_store *objects, zend_uint init_size);
ZEND_API void zend_objects_store_add_ref(zval *object TSRMLS_DC);
ZEND_API void zend_objects_store_del_ref(zval *object TSRMLS_DC);
ZEND_API void zend_objects_store_add_ref_by_handle(zend_object_handle handle TSRMLS_DC);
ZEND_API void zend_objects_store_del_ref_by_handle(zend_object_handle handle TSRMLS_DC);
ZEND_API zend_uint zend_objects_store_get_refcount(zval *object TSRMLS_DC);
ZEND_API zend_object_value zend_objects_store_clone_obj(zval *object TSRMLS_DC);
ZEND_API void *zend_object_store_get_object(zval *object TSRMLS_DC);
ZEND_API void *zend_object_store_get_object_by_handle(zend_object_handle handle TSRMLS_DC);
/* See comment in zend_objects_API.c before you use this */
ZEND_API void zend_object_store_set_object(zval *zobject, void *object TSRMLS_DC);
ZEND_API void zend_object_store_ctor_failed(zval *zobject TSRMLS_DC);

ZEND_API void zend_objects_store_free_object_storage(zend_objects_store *objects TSRMLS_DC);

ZEND_API zval* zend_call_method(zval **object_pp, zend_class_entry *obj_ce, zend_function **fn_proxy, char *function_name, int function_name_len, zval **retval_ptr_ptr, int param_count, zval* arg1, zval* arg2 TSRMLS_DC);

#define zend_call_method_with_0_params(obj, obj_ce, fn_proxy, function_name, retval) \
	zend_call_method(obj, obj_ce, fn_proxy, function_name, sizeof(function_name)-1, retval, 0, NULL, NULL TSRMLS_CC)

#define zend_call_method_with_1_params(obj, obj_ce, fn_proxy, function_name, retval, arg1) \
	zend_call_method(obj, obj_ce, fn_proxy, function_name, sizeof(function_name)-1, retval, 1, arg1, NULL TSRMLS_CC)

#define zend_call_method_with_2_params(obj, obj_ce, fn_proxy, function_name, retval, arg1, arg2) \
	zend_call_method(obj, obj_ce, fn_proxy, function_name, sizeof(function_name)-1, retval, 2, arg1, arg2 TSRMLS_CC)

#define ZEND_OBJECTS_STORE_HANDLERS zend_objects_store_add_ref, zend_objects_store_del_ref, zend_objects_store_clone_obj

ZEND_API void zend_update_class_constants(zend_class_entry *class_type TSRMLS_DC);
ZEND_API zend_object_handlers *zend_get_std_object_handlers(void);
#endif

END_EXTERN_C()
