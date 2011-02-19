//
// ExtSupport.PHP4 - substitute for php4ts.dll
//
// ObjectHandlers.cpp
// - contains definitions of object handler related exported functions
// - copied directly from zend_object_handlers.c (except for noted modified functions)
//

#include "stdafx.h"
#include "Objects.h"
#include "Module.h"
#include "Hash.h"
#include "Memory.h"
#include "Variables.h"
#include "Strings.h"
#include "Errors.h"
#include "Parameters.h"
#include "Arrays.h"
#include "Request.h"
#include <assert.h>

#ifdef PHP5TS

using namespace PHP::Core;
using namespace PHP::ExtManager;

//Copied from zend_interfaces.c
ZEND_API zend_class_entry *zend_ce_arrayaccess;

//Copied from zend_object_handlers.h
#define IS_ZEND_STD_OBJECT(z)  (Z_TYPE(z) == IS_OBJECT && (Z_OBJ_HT((z))->get_class_entry != NULL))

//Copied from zend_API.c
ZEND_API int zend_copy_parameters_array(int param_count, zval *argument_array TSRMLS_DC)
{
	void **p;
	int arg_count;

	p = EG(argument_stack).top_element-2;
	arg_count = (int)(zend_uintptr_t) *p;

	if (param_count>arg_count) {
		return FAILURE;
	}

	while (param_count-->0) {
		zval **param = (zval **) p-(arg_count--);
		zval_add_ref(param);
		add_next_index_zval(argument_array, *param);
	}

	return SUCCESS;
}

//Copied from zend_execute.h
static inline int i_zend_is_true(zval *op)
{
	int result;

	switch (Z_TYPE_P(op)) {
		case IS_NULL:
			result = 0;
			break;
		case IS_LONG:
		case IS_BOOL:
		case IS_RESOURCE:
			result = (Z_LVAL_P(op)?1:0);
			break;
		case IS_DOUBLE:
			result = (Z_DVAL_P(op) ? 1 : 0);
			break;
		case IS_STRING:
			if (Z_STRLEN_P(op) == 0
				|| (Z_STRLEN_P(op)==1 && Z_STRVAL_P(op)[0]=='0')) {
				result = 0;
			} else {
				result = 1;
			}
			break;
		case IS_ARRAY:
			result = (zend_hash_num_elements(Z_ARRVAL_P(op))?1:0);
			break;
		case IS_OBJECT:
			if(IS_ZEND_STD_OBJECT(*op)) {
				TSRMLS_FETCH();

				if (Z_OBJ_HT_P(op)->cast_object) {
					zval tmp;
					if (Z_OBJ_HT_P(op)->cast_object(op, &tmp, IS_BOOL TSRMLS_CC) == SUCCESS) {
						result = Z_LVAL(tmp);
						break;
					}
				} else if (Z_OBJ_HT_P(op)->get) {
					zval *tmp = Z_OBJ_HT_P(op)->get(op TSRMLS_CC);
					if(Z_TYPE_P(tmp) != IS_OBJECT) {
						/* for safety - avoid loop */
						convert_to_boolean(tmp);
						result = Z_LVAL_P(tmp);
						zval_ptr_dtor(&tmp);
						break;
					}
				}
			
				if(EG(ze1_compatibility_mode)) {
					result = (zend_hash_num_elements(Z_OBJPROP_P(op))?1:0);
				} else {
					result = 1;
				}
			} else {
				result = 1;
			}
			break;
		default:
			result = 0;
			break;
	}
	return result;
}

//Copied from zend_compile.c
static int zend_strnlen(const char* s, int maxlen)
{
	int len = 0;
	while (*s++ && maxlen--) len++;
	return len;
}

//Copied from zend_compile.c
char *zend_visibility_string(zend_uint fn_flags)
{
	if (fn_flags & ZEND_ACC_PRIVATE) {
		return "private";
	}
	if (fn_flags & ZEND_ACC_PROTECTED) {
		return "protected";
	}
	if (fn_flags & ZEND_ACC_PUBLIC) {
		return "public";
	}
	return "";
}

//Copied from zend_compile.c
ZEND_API int zend_unmangle_property_name(char *mangled_property, int len, char **class_name, char **prop_name)
{
	int class_name_len;

	*class_name = NULL;

	if (mangled_property[0]!=0) {
		*prop_name = mangled_property;
		return SUCCESS;
	}
	if (len < 3 || mangled_property[1]==0) {
		zend_error(E_NOTICE, "Illegal member variable name");
		*prop_name = mangled_property;
		return FAILURE;
	}

	class_name_len = zend_strnlen(mangled_property+1, --len - 1) + 1;
	if (class_name_len >= len || mangled_property[class_name_len]!=0) {
		zend_error(E_NOTICE, "Corrupt member variable name");
		*prop_name = mangled_property;
		return FAILURE;
	}
	*class_name = mangled_property+1;
	*prop_name = (*class_name)+class_name_len;
	return SUCCESS;
}

//Copied from zend.h
#define SEPARATE_ARG_IF_REF(varptr) \
	if (PZVAL_IS_REF(varptr)) { \
		zval *original_var = varptr; \
		ALLOC_ZVAL(varptr); \
		varptr->value = original_var->value; \
		varptr->type = original_var->type; \
		varptr->is_ref = 0; \
		varptr->refcount = 1; \
		zval_copy_ctor(varptr); \
	} else { \
		varptr->refcount++; \
	}


#define Z_OBJ_P(zval_p) zend_objects_get_address(zval_p TSRMLS_CC)


/*
  __X accessors explanation:

  if we have __get and property that is not part of the properties array is
  requested, we call __get handler. If it fails, we return uninitialized.

  if we have __set and property that is not part of the properties array is
  set, we call __set handler. If it fails, we do not change the array.

  for both handlers above, when we are inside __get/__set, no further calls for
  __get/__set for this property of this object will be made, to prevent endless 
  recursion and enable accessors to change properties array.

  if we have __call and method which is not part of the class function table is
  called, we cal __call handler.
*/

static HashTable *zend_std_get_properties(zval *object TSRMLS_DC)
{
	zend_object *zobj;
	zobj = Z_OBJ_P(object);
	return zobj->properties;
}

static zval *zend_std_call_getter(zval *object, zval *member TSRMLS_DC)
{
	zval *retval = NULL;
	zend_class_entry *ce = Z_OBJCE_P(object);

	/* __get handler is called with one argument:
	      property name

	   it should return whether the call was successfull or not
	*/

	SEPARATE_ARG_IF_REF(member);

	zend_call_method_with_1_params(&object, ce, &ce->__get, ZEND_GET_FUNC_NAME, &retval, member);

	zval_ptr_dtor(&member);

	if (retval) {
		retval->refcount--;
	}

	return retval;
}

static int zend_std_call_setter(zval *object, zval *member, zval *value TSRMLS_DC)
{
	zval *retval = NULL;
	int result;
	zend_class_entry *ce = Z_OBJCE_P(object);

	SEPARATE_ARG_IF_REF(member);
	value->refcount++;

	/* __set handler is called with two arguments:
	     property name
	     value to be set

	   it should return whether the call was successfull or not
	*/
	zend_call_method_with_2_params(&object, ce, &ce->__set, ZEND_SET_FUNC_NAME, &retval, member, value);

	zval_ptr_dtor(&member);
	zval_ptr_dtor(&value);

	if (retval) {
		result = i_zend_is_true(retval) ? SUCCESS : FAILURE;
		zval_ptr_dtor(&retval);
		return result;
	} else {
		return FAILURE;
	}
}

static void zend_std_call_unsetter(zval *object, zval *member TSRMLS_DC)
{
	zend_class_entry *ce = Z_OBJCE_P(object);

	/* __unset handler is called with one argument:
	      property name
	*/

	SEPARATE_ARG_IF_REF(member);

	zend_call_method_with_1_params(&object, ce, &ce->__unset, ZEND_UNSET_FUNC_NAME, NULL, member);

	zval_ptr_dtor(&member);
}

static zval *zend_std_call_issetter(zval *object, zval *member TSRMLS_DC)
{
	zval *retval = NULL;
	zend_class_entry *ce = Z_OBJCE_P(object);

	/* __isset handler is called with one argument:
	      property name

	   it should return whether the property is set or not
	*/

	SEPARATE_ARG_IF_REF(member);

	zend_call_method_with_1_params(&object, ce, &ce->__isset, ZEND_ISSET_FUNC_NAME, &retval, member);

	zval_ptr_dtor(&member);

	return retval;
}

static int zend_verify_property_access(zend_property_info *property_info, zend_class_entry *ce TSRMLS_DC)
{
	switch (property_info->flags & ZEND_ACC_PPP_MASK) {
		case ZEND_ACC_PUBLIC:
			return 1;
		case ZEND_ACC_PROTECTED:
			return zend_check_protected(property_info->ce, EG(scope));
		case ZEND_ACC_PRIVATE:
			if ((ce==EG(scope) || property_info->ce == EG(scope)) && EG(scope)) {
				return 1;
			} else {
				return 0;
			}
			break;
	}
	return 0;
}

static inline zend_bool is_derived_class(zend_class_entry *child_class, zend_class_entry *parent_class)
{
	child_class = child_class->parent;
	while (child_class) {
		if (child_class == parent_class) {
			return 1;
		}
		child_class = child_class->parent;
	}

	return 0;
}

ZEND_API struct _zend_property_info *zend_get_property_info(zend_class_entry *ce, zval *member, int silent TSRMLS_DC)
{
	zend_property_info *property_info = NULL;
	zend_property_info *scope_property_info;
	zend_bool denied_access = 0;
	ulong h;

	if (Z_STRVAL_P(member)[0] == '\0') {
		if (!silent) {
			if (Z_STRLEN_P(member) == 0) {
				zend_error(E_ERROR, "Cannot access empty property");
			} else {
				zend_error(E_ERROR, "Cannot access property started with '\\0'");
			}
		}
		return NULL;
	}
	h = zend_get_hash_value(Z_STRVAL_P(member), Z_STRLEN_P(member) + 1);
	if (zend_hash_quick_find(&ce->properties_info, Z_STRVAL_P(member), Z_STRLEN_P(member)+1, h, (void **) &property_info)==SUCCESS) {
		if(property_info->flags & ZEND_ACC_SHADOW) {
			/* if it's a shadow - go to access it's private */
			property_info = NULL;
		} else {
			if (zend_verify_property_access(property_info, ce TSRMLS_CC)) {
				if (property_info->flags & ZEND_ACC_CHANGED
					&& !(property_info->flags & ZEND_ACC_PRIVATE)) {
					/* We still need to make sure that we're not in a context
					 * where the right property is a different 'statically linked' private
					 * continue checking below...
					 */
				} else {
					if (!silent && (property_info->flags & ZEND_ACC_STATIC)) {
						zend_error(E_STRICT, "Accessing static property %s::$%s as non static", ce->name, Z_STRVAL_P(member));
					}
					return property_info;
				}
			} else {
				/* Try to look in the scope instead */
				denied_access = 1;
			}
		}
	}
	if (EG(scope) != ce
		&& is_derived_class(ce, EG(scope))
		&& EG(scope)
		&& zend_hash_quick_find(&EG(scope)->properties_info, Z_STRVAL_P(member), Z_STRLEN_P(member)+1, h, (void **) &scope_property_info)==SUCCESS
		&& scope_property_info->flags & ZEND_ACC_PRIVATE) {
		return scope_property_info;
	} else if (property_info) {
		if (denied_access) {
			/* Information was available, but we were denied access.  Error out. */
			if (silent) {
				return NULL;
			}
			zend_error(E_ERROR, "Cannot access %s property %s::$%s", zend_visibility_string(property_info->flags), ce->name, Z_STRVAL_P(member));
		} else {
			/* fall through, return property_info... */
		}
	} else {
		EG(std_property_info).flags = ZEND_ACC_PUBLIC;
		EG(std_property_info).name = Z_STRVAL_P(member);
		EG(std_property_info).name_length = Z_STRLEN_P(member);
		EG(std_property_info).h = h;
		EG(std_property_info).ce = ce;
		property_info = &EG(std_property_info);
	}
	return property_info;
}


ZEND_API int zend_check_property_access(zend_object *zobj, char *prop_info_name, int prop_info_name_len TSRMLS_DC)
{
	zend_property_info *property_info;
	char *class_name, *prop_name;
	zval member;

	zend_unmangle_property_name(prop_info_name, prop_info_name_len, &class_name, &prop_name);
	ZVAL_STRING(&member, prop_name, 0);
	property_info = zend_get_property_info(zobj->ce, &member, 1 TSRMLS_CC);
	if (!property_info) {
		return FAILURE;
	}
	if (prop_info_name[0] == '\0' && prop_info_name[1] != '*') {
		if (!(property_info->flags & ZEND_ACC_PRIVATE)) {
			/* we we're looking for a private prop but found a non private one of the same name */
			return FAILURE;
		} else if (strcmp(prop_info_name+1, property_info->name+1)) {
			/* we we're looking for a private prop but found a private one of the same name but another class */
			return FAILURE;
		}
	}
	return zend_verify_property_access(property_info, zobj->ce TSRMLS_CC) ? SUCCESS : FAILURE;
}

static int zend_get_property_guard(zend_object *zobj, zend_property_info *property_info, zval *member, zend_guard **pguard)
{
	zend_property_info info;
	zend_guard stub;

	if (!property_info) {
		property_info = &info;
		info.name = Z_STRVAL_P(member);
		info.name_length = Z_STRLEN_P(member);
		info.h = zend_get_hash_value(Z_STRVAL_P(member), Z_STRLEN_P(member) + 1);
	}
	if (!zobj->guards) {
		ALLOC_HASHTABLE(zobj->guards);
		zend_hash_init(zobj->guards, 0, NULL, NULL, 0);
	} else if (zend_hash_quick_find(zobj->guards, property_info->name, property_info->name_length+1, property_info->h, (void **) pguard) == SUCCESS) {
		return SUCCESS;
	}
	stub.in_get = 0;
	stub.in_set = 0;
	stub.in_unset = 0;
	stub.in_isset = 0;
	return zend_hash_quick_add(zobj->guards, property_info->name, property_info->name_length+1, property_info->h, (void**)&stub, sizeof(stub), (void**) pguard);
}

zval *zend_std_read_property(zval *object, zval *member, int type TSRMLS_DC)
{
	zend_object *zobj;
	zval *tmp_member = NULL;
	zval **retval;
	zval *rv = NULL;
	zend_property_info *property_info;
	int silent;

	silent = (type == BP_VAR_IS);
	zobj = Z_OBJ_P(object);

 	if (member->type != IS_STRING) {
 		ALLOC_ZVAL(tmp_member);
		*tmp_member = *member;
		INIT_PZVAL(tmp_member);
		zval_copy_ctor(tmp_member);
		convert_to_string(tmp_member);
		member = tmp_member;
	}

#if DEBUG_OBJECT_HANDLERS
	fprintf(stderr, "Read object #%d property: %s\n", Z_OBJ_HANDLE_P(object), Z_STRVAL_P(member));
#endif			

	/* make zend_get_property_info silent if we have getter - we may want to use it */
	property_info = zend_get_property_info(zobj->ce, member, (zobj->ce->__get != NULL) TSRMLS_CC);

	if (!property_info || zend_hash_quick_find(zobj->properties, property_info->name, property_info->name_length+1, property_info->h, (void **) &retval) == FAILURE) {
		zend_guard *guard;

		if (zobj->ce->__get &&
		    zend_get_property_guard(zobj, property_info, member, &guard) == SUCCESS &&
		    !guard->in_get) {
			/* have getter - try with it! */
			guard->in_get = 1; /* prevent circular getting */
			rv = zend_std_call_getter(object, member TSRMLS_CC);
			guard->in_get = 0;

			if (rv) {
				retval = &rv;
				if (!rv->is_ref &&
				    (type == BP_VAR_W || type == BP_VAR_RW  || type == BP_VAR_UNSET)) {
					if (rv->refcount > 0) {
						zval *tmp = rv;

						ALLOC_ZVAL(rv);
						*rv = *tmp;
						zval_copy_ctor(rv);
						rv->is_ref = 0;
						rv->refcount = 0;
					}
					if (Z_TYPE_P(rv) != IS_OBJECT) {
						zend_error(E_NOTICE, "Indirect modification of overloaded property %s::$%s has no effect", zobj->ce->name, Z_STRVAL_P(member));
					}
				}
			} else {
				retval = &EG(uninitialized_zval_ptr);
			}
		} else {
			if (!silent) {
				zend_error(E_NOTICE,"Undefined property:  %s::$%s", zobj->ce->name, Z_STRVAL_P(member));
			}
			retval = &EG(uninitialized_zval_ptr);
		}
	}
	if (tmp_member) {
		(*retval)->refcount++;
		zval_ptr_dtor(&tmp_member);
		(*retval)->refcount--;
	}
	return *retval;
}


static void zend_std_write_property(zval *object, zval *member, zval *value TSRMLS_DC)
{
	zend_object *zobj;
	zval *tmp_member = NULL;
	zval **variable_ptr;
	zend_property_info *property_info;

	zobj = Z_OBJ_P(object);

 	if (member->type != IS_STRING) {
 		ALLOC_ZVAL(tmp_member);
		*tmp_member = *member;
		INIT_PZVAL(tmp_member);
		zval_copy_ctor(tmp_member);
		convert_to_string(tmp_member);
		member = tmp_member;
	}

	property_info = zend_get_property_info(zobj->ce, member, (zobj->ce->__set != NULL) TSRMLS_CC);

	if (property_info && zend_hash_quick_find(zobj->properties, property_info->name, property_info->name_length+1, property_info->h, (void **) &variable_ptr) == SUCCESS) {
		/* if we already have this value there, we don't actually need to do anything */
		if (*variable_ptr != value) {
			/* if we are assigning reference, we shouldn't move it, but instead assign variable
			   to the same pointer */
			if (PZVAL_IS_REF(*variable_ptr)) {
				zval garbage = **variable_ptr; /* old value should be destroyed */

				/* To check: can't *variable_ptr be some system variable like error_zval here? */
				(*variable_ptr)->type = value->type;
				(*variable_ptr)->value = value->value;
				if (value->refcount>0) {
					zval_copy_ctor(*variable_ptr);
				}
				zval_dtor(&garbage);
			} else {
				zval *garbage = *variable_ptr;

				/* if we assign referenced variable, we should separate it */
				value->refcount++;
				if (PZVAL_IS_REF(value)) {
					SEPARATE_ZVAL(&value);
				}
				*variable_ptr = value;
				zval_ptr_dtor(&garbage);
			}
		}
	} else {
		int setter_done = 0;
		zend_guard *guard;

		if (zobj->ce->__set &&
		    zend_get_property_guard(zobj, property_info, member, &guard) == SUCCESS &&
		    !guard->in_set) {
			guard->in_set = 1; /* prevent circular setting */
			if (zend_std_call_setter(object, member, value TSRMLS_CC) != SUCCESS) {
				/* for now, just ignore it - __set should take care of warnings, etc. */
			}
			setter_done = 1;
			guard->in_set = 0;
		}
		if (!setter_done && property_info) {
			zval **foo;

			/* if we assign referenced variable, we should separate it */
			value->refcount++;
			if (PZVAL_IS_REF(value)) {
				SEPARATE_ZVAL(&value);
			}
			zend_hash_quick_update(zobj->properties, property_info->name, property_info->name_length+1, property_info->h, &value, sizeof(zval *), (void **) &foo);
		}
	}

	if (tmp_member) {
		zval_ptr_dtor(&tmp_member);
	}
}

zval *zend_std_read_dimension(zval *object, zval *offset, int type TSRMLS_DC)
{
	zend_class_entry *ce = Z_OBJCE_P(object);
	zval *retval;

	if (instanceof_function_ex(ce, zend_ce_arrayaccess, 1 TSRMLS_CC)) {
		if(offset == NULL) {
			/* [] construct */
			ALLOC_INIT_ZVAL(offset);
		} else {
			SEPARATE_ARG_IF_REF(offset);
		}
		zend_call_method_with_1_params(&object, ce, NULL, "offsetget", &retval, offset);

		zval_ptr_dtor(&offset);

		if (!retval) {
			if (!EG(exception)) {
				zend_error(E_ERROR, "Undefined offset for object of type %s used as array", ce->name);
			}
			return 0;
		}

		/* Undo PZVAL_LOCK() */
		retval->refcount--;

		return retval;
	} else {
		zend_error(E_ERROR, "Cannot use object of type %s as array", ce->name);
		return 0;
	}
}


static void zend_std_write_dimension(zval *object, zval *offset, zval *value TSRMLS_DC)
{
	zend_class_entry *ce = Z_OBJCE_P(object);

	if (instanceof_function_ex(ce, zend_ce_arrayaccess, 1 TSRMLS_CC)) {
		if (!offset) {
			ALLOC_INIT_ZVAL(offset);
		} else {
			SEPARATE_ARG_IF_REF(offset);
		}
		zend_call_method_with_2_params(&object, ce, NULL, "offsetset", NULL, offset, value);
		zval_ptr_dtor(&offset);
	} else {
		zend_error(E_ERROR, "Cannot use object of type %s as array", ce->name);
	}
}


static int zend_std_has_dimension(zval *object, zval *offset, int check_empty TSRMLS_DC)
{
	zend_class_entry *ce = Z_OBJCE_P(object);
	zval *retval;
	int result;

	if (instanceof_function_ex(ce, zend_ce_arrayaccess, 1 TSRMLS_CC)) {
		SEPARATE_ARG_IF_REF(offset);
		zend_call_method_with_1_params(&object, ce, NULL, "offsetexists", &retval, offset);
		if (retval) {
			result = i_zend_is_true(retval);
			zval_ptr_dtor(&retval);
			if (check_empty && result && !EG(exception)) {
				zend_call_method_with_1_params(&object, ce, NULL, "offsetget", &retval, offset);
				if (retval) {
					result = i_zend_is_true(retval);
					zval_ptr_dtor(&retval);
				}
			}
		} else {
			result = 0;
		}
		zval_ptr_dtor(&offset);
	} else {
		zend_error(E_ERROR, "Cannot use object of type %s as array", ce->name);
		return 0;
	}
	return result;
}


static zval **zend_std_get_property_ptr_ptr(zval *object, zval *member TSRMLS_DC)
{
	zend_object *zobj;
	zval tmp_member;
	zval **retval;
	zend_property_info *property_info;

	zobj = Z_OBJ_P(object);

 	if (member->type != IS_STRING) {
		tmp_member = *member;
		zval_copy_ctor(&tmp_member);
		convert_to_string(&tmp_member);
		member = &tmp_member;
	}

#if DEBUG_OBJECT_HANDLERS
	fprintf(stderr, "Ptr object #%d property: %s\n", Z_OBJ_HANDLE_P(object), Z_STRVAL_P(member));
#endif

	property_info = zend_get_property_info(zobj->ce, member, (zobj->ce->__get != NULL) TSRMLS_CC);

	if (!property_info || zend_hash_quick_find(zobj->properties, property_info->name, property_info->name_length+1, property_info->h, (void **) &retval) == FAILURE) {
		zval *new_zval;
		zend_guard *guard;

		if (!zobj->ce->__get ||
		    zend_get_property_guard(zobj, property_info, member, &guard) != SUCCESS ||
		    guard->in_get) {
			/* we don't have access controls - will just add it */
			new_zval = &EG(uninitialized_zval);

/* 			zend_error(E_NOTICE, "Undefined property: %s", Z_STRVAL_P(member)); */
			new_zval->refcount++;
			zend_hash_quick_update(zobj->properties, property_info->name, property_info->name_length+1, property_info->h, &new_zval, sizeof(zval *), (void **) &retval);
		} else {
			/* we do have getter - fail and let it try again with usual get/set */
			retval = NULL;
		}
	}
	if (member == &tmp_member) {
		zval_dtor(member);
	}
	return retval;
}


static void zend_std_unset_property(zval *object, zval *member TSRMLS_DC)
{
	zend_object *zobj;
	zval *tmp_member = NULL;
	zend_property_info *property_info;

	zobj = Z_OBJ_P(object);

 	if (member->type != IS_STRING) {
 		ALLOC_ZVAL(tmp_member);
		*tmp_member = *member;
		INIT_PZVAL(tmp_member);
		zval_copy_ctor(tmp_member);
		convert_to_string(tmp_member);
		member = tmp_member;
	}

	property_info = zend_get_property_info(zobj->ce, member, (zobj->ce->__unset != NULL) TSRMLS_CC);

	if (!property_info || zend_hash_del(zobj->properties, property_info->name, property_info->name_length+1) == FAILURE) {
		zend_guard *guard;

		if (zobj->ce->__unset &&
		    zend_get_property_guard(zobj, property_info, member, &guard) == SUCCESS &&
		    !guard->in_unset) {
			/* have unseter - try with it! */
			guard->in_unset = 1; /* prevent circular unsetting */
			zend_std_call_unsetter(object, member TSRMLS_CC);
			guard->in_unset = 0;
		}
	}

	if (tmp_member) {
		zval_ptr_dtor(&tmp_member);
	}
}


static void zend_std_unset_dimension(zval *object, zval *offset TSRMLS_DC)
{
	zend_class_entry *ce = Z_OBJCE_P(object);

	if (instanceof_function_ex(ce, zend_ce_arrayaccess, 1 TSRMLS_CC)) {
		SEPARATE_ARG_IF_REF(offset);
		zend_call_method_with_1_params(&object, ce, NULL, "offsetunset", NULL, offset);
		zval_ptr_dtor(&offset);
	} else {
		zend_error(E_ERROR, "Cannot use object of type %s as array", ce->name);
	}
}


ZEND_API void zend_std_call_user_call(INTERNAL_FUNCTION_PARAMETERS)
{
	zend_internal_function *func = (zend_internal_function *)EG(function_state_ptr)->function;
	zval *method_name_ptr, *method_args_ptr;
	zval *method_result_ptr = NULL;
	zend_class_entry *ce = Z_OBJCE_P(this_ptr);

	ALLOC_ZVAL(method_args_ptr);
	INIT_PZVAL(method_args_ptr);
	array_init(method_args_ptr);

	if (zend_copy_parameters_array(ZEND_NUM_ARGS(), method_args_ptr TSRMLS_CC) == FAILURE) {
		zval_dtor(method_args_ptr);
		zend_error(E_ERROR, "Cannot get arguments for __call");
		RETURN_FALSE;
	}

	ALLOC_ZVAL(method_name_ptr);
	INIT_PZVAL(method_name_ptr);
	ZVAL_STRING(method_name_ptr, func->function_name, 0); /* no dup - it's a copy */

	/* __call handler is called with two arguments:
	   method name
	   array of method parameters

	*/
	zend_call_method_with_2_params(&this_ptr, ce, &ce->__call, ZEND_CALL_FUNC_NAME, &method_result_ptr, method_name_ptr, method_args_ptr);

	if (method_result_ptr) {
		if (method_result_ptr->is_ref || method_result_ptr->refcount > 1) {
			RETVAL_ZVAL(method_result_ptr, 1, 1);
		} else {
			RETVAL_ZVAL(method_result_ptr, 0, 1);
		}
	}

	/* now destruct all auxiliaries */
	zval_ptr_dtor(&method_args_ptr);
	zval_ptr_dtor(&method_name_ptr);

	/* destruct the function also, then - we have allocated it in get_method */
	efree(func);
}

/* Ensures that we're allowed to call a private method.
 * Returns the function address that should be called, or NULL
 * if no such function exists.
 */
static inline zend_function *zend_check_private_int(zend_function *fbc, zend_class_entry *ce, char *function_name_strval, int function_name_strlen TSRMLS_DC)
{
	if (!ce) {
		return 0;
	}

	/* We may call a private function if:
	 * 1.  The class of our object is the same as the scope, and the private
	 *     function (EX(fbc)) has the same scope.
	 * 2.  One of our parent classes are the same as the scope, and it contains
	 *     a private function with the same name that has the same scope.
	 */
	if (fbc->common.scope == ce && EG(scope) == ce) {
		/* rule #1 checks out ok, allow the function call */
		return fbc;
	}


	/* Check rule #2 */
	ce = ce->parent;
	while (ce) {
		if (ce == EG(scope)) {
			if (zend_hash_find(&ce->function_table, function_name_strval, function_name_strlen+1, (void **) &fbc)==SUCCESS
				&& fbc->op_array.fn_flags & ZEND_ACC_PRIVATE
				&& fbc->common.scope == EG(scope)) {
				return fbc;
			}
			break;
		}
		ce = ce->parent;
	}
	return NULL;
}


ZEND_API int zend_check_private(zend_function *fbc, zend_class_entry *ce, char *function_name_strval, int function_name_strlen TSRMLS_DC)
{
	return zend_check_private_int(fbc, ce, function_name_strval, function_name_strlen TSRMLS_CC) != NULL;
}


/* Ensures that we're allowed to call a protected method.
 */
ZEND_API int zend_check_protected(zend_class_entry *ce, zend_class_entry *scope)
{
	zend_class_entry *fbc_scope = ce;

	/* Is the context that's calling the function, the same as one of
	 * the function's parents?
	 */
	while (fbc_scope) {
		if (fbc_scope==scope) {
			return 1;
		}
		fbc_scope = fbc_scope->parent;
	}

	/* Is the function's scope the same as our current object context,
	 * or any of the parents of our context?
	 */
	while (scope) {
		if (scope==ce) {
			return 1;
		}
		scope = scope->parent;
	}
	return 0;
}


static inline zend_class_entry * zend_get_function_root_class(zend_function *fbc)
{
	return fbc->common.prototype ? fbc->common.prototype->common.scope : fbc->common.scope;
}


static union _zend_function *zend_std_get_method(zval **object_ptr, char *method_name, int method_len TSRMLS_DC)
{
	zend_object *zobj;
	zend_function *fbc;
	char *lc_method_name;
	zval *object = *object_ptr;
	
	lc_method_name = (char *)do_alloca(method_len+1);
	/* Create a zend_copy_str_tolower(dest, src, src_length); */
	zend_str_tolower_copy(lc_method_name, method_name, method_len);
		
	zobj = Z_OBJ_P(object);
	if (zend_hash_find(&zobj->ce->function_table, lc_method_name, method_len+1, (void **)&fbc) == FAILURE) {
		free_alloca(lc_method_name);
		if (zobj->ce->__call) {
			zend_internal_function *call_user_call = (zend_internal_function * )emalloc(sizeof(zend_internal_function));
			call_user_call->type = ZEND_INTERNAL_FUNCTION;
			call_user_call->module = zobj->ce->module;
			call_user_call->handler = zend_std_call_user_call;
			call_user_call->arg_info = NULL;
			call_user_call->num_args = 0;
			call_user_call->scope = zobj->ce;
			call_user_call->fn_flags = 0;
			call_user_call->function_name = estrndup(method_name, method_len);
			call_user_call->pass_rest_by_reference = 0;
			call_user_call->return_reference = ZEND_RETURN_VALUE;

			return (union _zend_function *)call_user_call;
		} else {
			return NULL;
		}
	}

	/* Check access level */
	if (fbc->op_array.fn_flags & ZEND_ACC_PRIVATE) {
		zend_function *updated_fbc;

		/* Ensure that if we're calling a private function, we're allowed to do so.
		 */
		updated_fbc = zend_check_private_int(fbc, Z_OBJ_HANDLER_P(object, get_class_entry)(object TSRMLS_CC), lc_method_name, method_len TSRMLS_CC);
		if (!updated_fbc) {
			zend_error(E_ERROR, "Call to %s method %s::%s() from context '%s'", zend_visibility_string(fbc->common.fn_flags), ZEND_FN_SCOPE_NAME(fbc), method_name, EG(scope) ? EG(scope)->name : "");
		}
		fbc = updated_fbc;
	} else {
		/* Ensure that we haven't overridden a private function and end up calling
		 * the overriding public function...
		 */
		if (EG(scope) &&
		    is_derived_class(fbc->common.scope, EG(scope)) &&
		    fbc->op_array.fn_flags & ZEND_ACC_CHANGED) {
			zend_function *priv_fbc;

			if (zend_hash_find(&EG(scope)->function_table, lc_method_name, method_len+1, (void **) &priv_fbc)==SUCCESS
				&& priv_fbc->common.fn_flags & ZEND_ACC_PRIVATE
				&& priv_fbc->common.scope == EG(scope)) {
				fbc = priv_fbc;
			}
		}
		if ((fbc->common.fn_flags & ZEND_ACC_PROTECTED)) {
			/* Ensure that if we're calling a protected function, we're allowed to do so.
			 */
			if (!zend_check_protected(zend_get_function_root_class(fbc), EG(scope))) {
				zend_error(E_ERROR, "Call to %s method %s::%s() from context '%s'", zend_visibility_string(fbc->common.fn_flags), ZEND_FN_SCOPE_NAME(fbc), method_name, EG(scope) ? EG(scope)->name : "");
			}
		}
	}

	free_alloca(lc_method_name);
	return fbc;
}


/* This is not (yet?) in the API, but it belongs in the built-in objects callbacks */
ZEND_API zend_function *zend_std_get_static_method(zend_class_entry *ce, char *function_name_strval, int function_name_strlen TSRMLS_DC)
{
	zend_function *fbc;

	if (zend_hash_find(&ce->function_table, function_name_strval, function_name_strlen+1, (void **) &fbc)==FAILURE) {
		char *class_name = ce->name;

		if (!class_name) {
			class_name = "";
		}
		zend_error(E_ERROR, "Call to undefined method %s::%s()", class_name, function_name_strval);
	}
	if (fbc->op_array.fn_flags & ZEND_ACC_PUBLIC) {
		/* No further checks necessary, most common case */
	} else if (fbc->op_array.fn_flags & ZEND_ACC_PRIVATE) {
		zend_function *updated_fbc;

		/* Ensure that if we're calling a private function, we're allowed to do so.
		 */
		updated_fbc = zend_check_private_int(fbc, EG(scope), function_name_strval, function_name_strlen TSRMLS_CC);
		if (!updated_fbc) {
			zend_error(E_ERROR, "Call to %s method %s::%s() from context '%s'", zend_visibility_string(fbc->common.fn_flags), ZEND_FN_SCOPE_NAME(fbc), function_name_strval, EG(scope) ? EG(scope)->name : "");
		}
		fbc = updated_fbc;
	} else if ((fbc->common.fn_flags & ZEND_ACC_PROTECTED)) {
		/* Ensure that if we're calling a protected function, we're allowed to do so.
		 */
		if (!zend_check_protected(zend_get_function_root_class(fbc), EG(scope))) {
			zend_error(E_ERROR, "Call to %s method %s::%s() from context '%s'", zend_visibility_string(fbc->common.fn_flags), ZEND_FN_SCOPE_NAME(fbc), function_name_strval, EG(scope) ? EG(scope)->name : "");
		}
	}

	return fbc;
}


ZEND_API zval **zend_std_get_static_property(zend_class_entry *ce, char *property_name, int property_name_len, zend_bool silent TSRMLS_DC)
{
	zval **retval = NULL;
	zend_class_entry *tmp_ce = ce;
	zend_property_info *property_info;
	zend_property_info std_property_info;

	if (zend_hash_find(&ce->properties_info, property_name, property_name_len+1, (void **) &property_info)==FAILURE || (property_info->flags & ZEND_ACC_SHADOW)) {
		std_property_info.flags = ZEND_ACC_PUBLIC;
		std_property_info.name = property_name;
		std_property_info.name_length = property_name_len;
		std_property_info.h = zend_get_hash_value(std_property_info.name, std_property_info.name_length+1);
		std_property_info.ce = ce;
		property_info = &std_property_info;
	}

#if DEBUG_OBJECT_HANDLERS
	zend_printf("Access type for %s::%s is %s\n", ce->name, property_name, zend_visibility_string(property_info->flags));
#endif

	if (!zend_verify_property_access(property_info, ce TSRMLS_CC)) {
		if (!silent) {
			zend_error(E_ERROR, "Cannot access %s property %s::$%s", zend_visibility_string(property_info->flags), ce->name, property_name);
		}
		return NULL;
	}

	zend_update_class_constants(tmp_ce TSRMLS_CC);

	zend_hash_quick_find(CE_STATIC_MEMBERS(tmp_ce), property_info->name, property_info->name_length+1, property_info->h, (void **) &retval);

	if (!retval) {
		if (silent) {
			return NULL;
		} else {
			zend_error(E_ERROR, "Access to undeclared static property:  %s::$%s", ce->name, property_name);
		}
	}

	return retval;
}


ZEND_API zend_bool zend_std_unset_static_property(zend_class_entry *ce, char *property_name, int property_name_len TSRMLS_DC)
{
	zend_error(E_ERROR, "Attempt to unset static property %s::$%s", ce->name, property_name);
	return 0;
}


ZEND_API union _zend_function *zend_std_get_constructor(zval *object TSRMLS_DC)
{
	zend_object *zobj = Z_OBJ_P(object);
	zend_function *constructor = zobj->ce->constructor;

	if (constructor) {
		if (constructor->op_array.fn_flags & ZEND_ACC_PUBLIC) {
			/* No further checks necessary */
		} else if (constructor->op_array.fn_flags & ZEND_ACC_PRIVATE) {
			/* Ensure that if we're calling a private function, we're allowed to do so.
			 */
			if (Z_OBJ_HANDLER_P(object, get_class_entry)(object TSRMLS_CC) != EG(scope)) {
				if (EG(scope)) {
					zend_error(E_ERROR, "Call to private %s::%s() from context '%s'", constructor->common.scope->name, constructor->common.function_name, EG(scope)->name);
				} else {
					zend_error(E_ERROR, "Call to private %s::%s() from invalid context", constructor->common.scope->name, constructor->common.function_name);
				}
			}
		} else if ((constructor->common.fn_flags & ZEND_ACC_PROTECTED)) {
			/* Ensure that if we're calling a protected function, we're allowed to do so.
			 */
			if (!zend_check_protected(zend_get_function_root_class(constructor), EG(scope))) {
				if (EG(scope)) {
					zend_error(E_ERROR, "Call to protected %s::%s() from context '%s'", constructor->common.scope->name, constructor->common.function_name, EG(scope)->name);
				} else {
					zend_error(E_ERROR, "Call to protected %s::%s() from invalid context", constructor->common.scope->name, constructor->common.function_name);
				}
			}
		}
	}

	return constructor;
}


int zend_compare_symbol_tables_i(HashTable *ht1, HashTable *ht2 TSRMLS_DC);


static int zend_std_compare_objects(zval *o1, zval *o2 TSRMLS_DC)
{
	zend_object *zobj1, *zobj2;

	zobj1 = Z_OBJ_P(o1);
	zobj2 = Z_OBJ_P(o2);

	if (zobj1->ce != zobj2->ce) {
		return 1; /* different classes */
	}
	return zend_compare_symbol_tables_i(zobj1->properties, zobj2->properties TSRMLS_CC);
}

static int zend_std_has_property(zval *object, zval *member, int has_set_exists TSRMLS_DC)
{
	zend_object *zobj;
	int result;
	zval **value;
	zval *tmp_member = NULL;
	zend_property_info *property_info;

	zobj = Z_OBJ_P(object);

	if (member->type != IS_STRING) {
		ALLOC_ZVAL(tmp_member);
		*tmp_member = *member;
		INIT_PZVAL(tmp_member);
		zval_copy_ctor(tmp_member);
		convert_to_string(tmp_member);
		member = tmp_member;
	}

#if DEBUG_OBJECT_HANDLERS
	fprintf(stderr, "Read object #%d property: %s\n", Z_OBJ_HANDLE_P(object), Z_STRVAL_P(member));
#endif

	property_info = zend_get_property_info(zobj->ce, member, 1 TSRMLS_CC);

	if (!property_info || zend_hash_quick_find(zobj->properties, property_info->name, property_info->name_length+1, property_info->h, (void **) &value) == FAILURE) {
		zend_guard *guard;

		result = 0;
		if ((has_set_exists != 2) &&
		    zobj->ce->__isset &&
		    zend_get_property_guard(zobj, property_info, member, &guard) == SUCCESS &&
		    !guard->in_isset) {
			zval *rv;

			/* have issetter - try with it! */
			guard->in_isset = 1; /* prevent circular getting */
			rv = zend_std_call_issetter(object, member TSRMLS_CC);
			if (rv) {
				result = zend_is_true(rv);
				zval_ptr_dtor(&rv);
				if (has_set_exists && result && !EG(exception) && zobj->ce->__get && !guard->in_get) {
					guard->in_get = 1;
					rv = zend_std_call_getter(object, member TSRMLS_CC);
					guard->in_get = 0;
					if (rv) {
						rv->refcount++;
						result = i_zend_is_true(rv);
						zval_ptr_dtor(&rv);
					}
				}
			}
			guard->in_isset = 0;
		}
	} else {
		switch (has_set_exists) {
		case 0:
			result = (Z_TYPE_PP(value) != IS_NULL);
			break;
		default:
			result = zend_is_true(*value);
			break;
		case 2:
			result = 1;
			break;
		}
	}

	if (tmp_member) {
		zval_ptr_dtor(&tmp_member);
	}
	return result;
}


zend_class_entry *zend_std_object_get_class(zval *object TSRMLS_DC)
{
	zend_object *zobj;
	zobj = Z_OBJ_P(object);

	return zobj->ce;
}

int zend_std_object_get_class_name(zval *object, char **class_name, zend_uint *class_name_len, int parent TSRMLS_DC)
{
	zend_object *zobj;
	zend_class_entry *ce;
	zobj = Z_OBJ_P(object);

	if (parent) {
		if (!zobj->ce->parent) {
			return FAILURE;
		}
		ce = zobj->ce->parent;
	} else {
		ce = zobj->ce;
	}

	*class_name_len = ce->name_length;
	*class_name = estrndup(ce->name, ce->name_length);
	return SUCCESS;
}

int zend_std_get_closure(zval *obj, zend_class_entry **ce_ptr, zend_function **fptr_ptr, zval **zobj_ptr TSRMLS_DC)
{
	throw gcnew System::NotImplementedException("zend_std_get_closure");
}

#pragma managed
//Copied from zend_object_handlers.c, heavily modified to call __toString
ZEND_API int zend_std_cast_object_tostring(zval *readobj, zval *writeobj, int type TSRMLS_DC)
{
	zval *retval;
	zend_class_entry *ce;

	switch (type) {
		case IS_STRING:
			{
				ce = Z_OBJCE_P(readobj);

				PhpMarshaler ^ marshaler = PhpMarshaler::GetInstance(PHP::ExtManager::Module::ModuleBoundContext);

				// transform parameters (as if they were 'out' parameters)
				Object^ arg = marshaler->MarshalNativeToManaged((IntPtr)readobj);
				retval = (zval *)marshaler->MarshalManagedToNative(RequestCookie::ConvertObjectToString(arg)).ToPointer();
				
				if (Z_TYPE_P(retval) == IS_STRING) {
					INIT_PZVAL(writeobj);
					ZVAL_ZVAL(writeobj, retval, 1, 1);
					if (Z_TYPE_P(writeobj) != type) {
						convert_to_explicit_type(writeobj, type);
					}
					return SUCCESS;
				} else {
					zval_ptr_dtor(&retval);
					INIT_PZVAL(writeobj);
					ZVAL_EMPTY_STRING(writeobj);
					zend_error(E_RECOVERABLE_ERROR, "Method %s::__toString() must return a string value", ce->name);
					return SUCCESS;
				}
			}
			return FAILURE;
		case IS_BOOL:
			INIT_PZVAL(writeobj);
			ZVAL_BOOL(writeobj, 1);
			return SUCCESS;
		case IS_LONG:
			ce = Z_OBJCE_P(readobj);
			zend_error(E_NOTICE, "Object of class %s could not be converted to int", ce->name);
			INIT_PZVAL(writeobj);
			ZVAL_LONG(writeobj, 1);
			return SUCCESS;
		case IS_DOUBLE:
			ce = Z_OBJCE_P(readobj);
			zend_error(E_NOTICE, "Object of class %s could not be converted to double", ce->name);
			INIT_PZVAL(writeobj);
			ZVAL_DOUBLE(writeobj, 1);
			return SUCCESS;
		default:
			break;
	}
	return FAILURE;
}

ZEND_API zend_object_handlers std_object_handlers = {
	zend_objects_store_add_ref,				/* add_ref */
	zend_objects_store_del_ref,				/* del_ref */
	zend_objects_clone_obj,					/* clone_obj */

	zend_std_read_property,					/* read_property */
	zend_std_write_property,				/* write_property */
	zend_std_read_dimension,				/* read_dimension */
	zend_std_write_dimension,				/* write_dimension */
	zend_std_get_property_ptr_ptr,			/* get_property_ptr_ptr */
	NULL,									/* get */
	NULL,									/* set */
	zend_std_has_property,					/* has_property */
	zend_std_unset_property,				/* unset_property */
	zend_std_has_dimension,					/* has_dimension */
	zend_std_unset_dimension,				/* unset_dimension */
	zend_std_get_properties,				/* get_properties */
	zend_std_get_method,					/* get_method */
	NULL,									/* call_method */
	zend_std_get_constructor,				/* get_constructor */
	zend_std_object_get_class,				/* get_class_entry */
	zend_std_object_get_class_name,			/* get_class_name */
	zend_std_compare_objects,				/* compare_objects */
	zend_std_cast_object_tostring,			/* cast_object */
	NULL,									/* count_elements */
	NULL,									/* get_debug_info */
	zend_std_get_closure,					/* get_closure */
};

#endif