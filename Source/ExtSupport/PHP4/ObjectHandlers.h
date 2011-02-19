//
// ExtSupport.PHP4 - substitute for php4ts.dll
//
// ObjectHandlers.h
// - contains declarations of object related exported functions
//

#pragma once

#include "stdafx.h"
#include "ExtSupport.h"
#include "Hash.h"
#include "Functions.h"
#include "LinkedLists.h"


/* Used to fetch property from the object, read-only */
typedef zval *(*zend_object_read_property_t)(zval *object, zval *member, int type TSRMLS_DC);

/* Used to fetch dimension from the object, read-only */
typedef zval *(*zend_object_read_dimension_t)(zval *object, zval *offset, int type TSRMLS_DC);

/* Used to set property of the object */
typedef void (*zend_object_write_property_t)(zval *object, zval *member, zval *value TSRMLS_DC);

/* Used to set dimension of the object */
typedef void (*zend_object_write_dimension_t)(zval *object, zval *offset, zval *value TSRMLS_DC);

/* Used to create pointer to the property of the object, for future direct r/w access */
typedef zval **(*zend_object_get_property_ptr_ptr_t)(zval *object, zval *member TSRMLS_DC);

/* Used to set object value (most probably used in combination with
 * typedef the result of the get_property_ptr)
 */
typedef void (*zend_object_set_t)(zval **property, zval *value TSRMLS_DC);

/* Used to get object value (most probably used in combination with
 * the result of the get_property_ptr or when converting object value to
 * one of the basic types)
 */
typedef zval* (*zend_object_get_t)(zval *property TSRMLS_DC);

/* Used to check if a property of the object exists */
typedef int (*zend_object_has_property_t)(zval *object, zval *member, int check_empty TSRMLS_DC);

/* Used to check if a dimension of the object exists */
typedef int (*zend_object_has_dimension_t)(zval *object, zval *member, int check_empty TSRMLS_DC);

/* Used to remove a property of the object */
typedef void (*zend_object_unset_property_t)(zval *object, zval *member TSRMLS_DC);

/* Used to remove a property of the object */
typedef void (*zend_object_unset_dimension_t)(zval *object, zval *offset TSRMLS_DC);

/* Used to get hash of the properties of the object, as hash of zval's */
typedef HashTable *(*zend_object_get_properties_t)(zval *object TSRMLS_DC);

/* Used to call methods */
/* args on stack! */
/* Andi - EX(fbc) (function being called) needs to be initialized already in the INIT fcall opcode so that the parameters can be parsed the right way. We need to add another callback for this.
 */
typedef int (*zend_object_call_method_t)(char *method, INTERNAL_FUNCTION_PARAMETERS);
typedef union _zend_function *(*zend_object_get_method_t)(zval **object_ptr, char *method_name, int method_len TSRMLS_DC);
typedef union _zend_function *(*zend_object_get_constructor_t)(zval *object TSRMLS_DC);

/* Object maintenance/destruction */
typedef void (*zend_object_add_ref_t)(zval *object TSRMLS_DC);
typedef void (*zend_object_del_ref_t)(zval *object TSRMLS_DC);
typedef void (*zend_object_delete_obj_t)(zval *object TSRMLS_DC);
typedef zend_object_value (*zend_object_clone_obj_t)(zval *object TSRMLS_DC);

typedef zend_class_entry *(*zend_object_get_class_entry_t)(zval *object TSRMLS_DC);
typedef int (*zend_object_get_class_name_t)(zval *object, char **class_name, zend_uint *class_name_len, int parent TSRMLS_DC);
typedef int (*zend_object_compare_t)(zval *object1, zval *object2 TSRMLS_DC);


/* updates *count to hold the number of elements present and returns SUCCESS.
 * Returns FAILURE if the object does not have any sense of overloaded dimensions */
typedef int (*zend_object_count_elements_t)(zval *object, long *count TSRMLS_DC);

typedef HashTable *(*zend_object_get_debug_info_t)(zval *object, int *is_temp TSRMLS_DC);

typedef int (*zend_object_get_closure_t)(zval *obj, zend_class_entry **ce_ptr, union _zend_function **fptr_ptr, zval **zobj_ptr TSRMLS_DC);

#ifdef PHP4TS

typedef int (*zend_object_cast_t)(zval *readobj, zval *writeobj, int type, int should_free TSRMLS_DC);

#elif defined PHP5TS

typedef int (*zend_object_cast_t)(zval *readobj, zval *retval, int type TSRMLS_DC);

#endif

struct _zend_object_handlers {
	/* general object functions */
	zend_object_add_ref_t					add_ref;
	zend_object_del_ref_t					del_ref;
	zend_object_clone_obj_t					clone_obj;
	/* individual object functions */
	zend_object_read_property_t				read_property;
	zend_object_write_property_t			write_property;
	zend_object_read_dimension_t			read_dimension;
	zend_object_write_dimension_t			write_dimension;
	zend_object_get_property_ptr_ptr_t		get_property_ptr_ptr;
	zend_object_get_t						get;
	zend_object_set_t						set;
	zend_object_has_property_t				has_property;
	zend_object_unset_property_t			unset_property;
	zend_object_has_dimension_t				has_dimension;
	zend_object_unset_dimension_t			unset_dimension;
	zend_object_get_properties_t			get_properties;
	zend_object_get_method_t				get_method;
	zend_object_call_method_t				call_method;
	zend_object_get_constructor_t			get_constructor;
	zend_object_get_class_entry_t			get_class_entry;
	zend_object_get_class_name_t			get_class_name;
	zend_object_compare_t					compare_objects;
	zend_object_cast_t						cast_object;
	zend_object_count_elements_t			count_elements;
	zend_object_get_debug_info_t			get_debug_info;
	zend_object_get_closure_t				get_closure;
};
typedef _zend_object_handlers zend_object_handlers;

extern ZEND_API zend_object_handlers std_object_handlers;

BEGIN_EXTERN_C()
ZEND_API int zend_check_protected(zend_class_entry *ce, zend_class_entry *scope);
ZEND_API int zend_check_private(zend_function *fbc, zend_class_entry *ce, char *function_name_strval, int function_name_strlen TSRMLS_DC);
ZEND_API int zend_std_cast_object_tostring(zval *readobj, zval *writeobj, int type TSRMLS_DC);
END_EXTERN_C()