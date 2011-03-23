//
// ExtSupport.PHP4 - substitute for php4ts.dll
//
// Objects.cpp
// - contains definitions of object related exported functions
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


using namespace PHP::Core;
using namespace PHP::ExtManager;


ZEND_API zend_class_entry zend_standard_class_def;

//copied from zend_objects_api.c
#define ZEND_OBJECTS_STORE_ADD_TO_FREE_LIST()																	\
			EG(objects_store).object_buckets[handle].bucket.free_list.next = EG(objects_store).free_list_head;	\
			EG(objects_store).free_list_head = handle;															\
			EG(objects_store).object_buckets[handle].valid = 0;

// copied from zend_API.c and beautified
/* If parent_ce is not NULL then it inherits from parent_ce
 * If parent_ce is NULL and parent_name isn't then it looks for the parent and inherits from it
 * If both parent_ce and parent_name are NULL it does a regular class registration
 * If parent_name is specified but not found NULL is returned
 */
ZEND_API zend_class_entry *zend_register_internal_class_ex(zend_class_entry *orig_class_entry, zend_class_entry *parent_ce,
														   char *parent_name TSRMLS_DC)
{
	PHP::ExtManager::Module ^module = PHP::ExtManager::Module::ModuleBoundContext;
	bool transient = false;

	if (module == nullptr)
	{
		transient = true;
		module = Request::GetCurrentRequest()->CurrentInvocationContext.Module;
	}
	if (module == nullptr)
	{
		throw gcnew InvalidOperationException("Could not register internal class without module and request context.");
	}

	if (!parent_ce && parent_name)
	{
		Class ^mng_parent_ce = module->GetClassByName(gcnew String(parent_name));
		if (mng_parent_ce == nullptr) return NULL;

		parent_ce = mng_parent_ce->GetClassEntry();
		if (parent_ce == NULL) return NULL;
	}

	String ^name = Class::GetZendClassName(orig_class_entry);
#ifdef DEBUG
	Debug::WriteLine("PHP4TS", String::Format("Registering internal class: {0}", name));
#endif

	PHP::ExtManager::Module ^mod = PHP::ExtManager::Module::GetCurrentModule();
	if (!transient) unmng_register_class(mod->GetModuleNumber(), orig_class_entry, parent_ce);

	Class ^cls = gcnew Class(mod, orig_class_entry, parent_ce);
	
	module->AddClass(name, cls, transient);

	return cls->GetClassEntry();
}

// copied from zend_API.c and beautified
ZEND_API zend_class_entry *zend_register_internal_class(zend_class_entry *class_entry TSRMLS_DC)
{
	return zend_register_internal_class_ex(class_entry, NULL, NULL TSRMLS_CC);
}

#ifdef PHP4TS
// copied from zend_opcode.c and bautified
ZEND_API void destroy_zend_class(zend_class_entry *ce)
{
	if (--(*ce->refcount) > 0) return;

	switch (ce->type)
	{
		case ZEND_USER_CLASS:
			efree(ce->name);
			efree(ce->refcount);
			zend_hash_destroy(&ce->function_table);
			zend_hash_destroy(&ce->default_properties);
			break;

		case ZEND_INTERNAL_CLASS:
			free(ce->name);
			free(ce->refcount);
			zend_hash_destroy(&ce->function_table);
			zend_hash_destroy(&ce->default_properties);
			break;
	}
}

// copied from zend.c
void register_standard_class(TSRMLS_D)
{
	zend_standard_class_def.type = ZEND_INTERNAL_CLASS;
	zend_standard_class_def.name_length = sizeof("stdClass") - 1;
	zend_standard_class_def.name = zend_strndup("stdClass", zend_standard_class_def.name_length);
	zend_standard_class_def.parent = NULL;
	zend_hash_init_ex(&zend_standard_class_def.default_properties, 0, NULL, ZVAL_PTR_DTOR, 1, 0);
	zend_hash_init_ex(&zend_standard_class_def.function_table, 0, NULL, ZEND_FUNCTION_DTOR, 1, 0);
	zend_standard_class_def.handle_function_call = NULL;
	zend_standard_class_def.handle_property_get = NULL;
	zend_standard_class_def.handle_property_set = NULL;
	zend_standard_class_def.refcount = (int *)malloc(sizeof(int));
	*zend_standard_class_def.refcount = 1;
//	zend_hash_add(GLOBAL_CLASS_TABLE, "stdclass", sizeof("stdclass"), &zend_standard_class_def, sizeof(zend_class_entry), NULL);
}
#elif defined(PHP5TS)
// copied from zend_opcode.c and bautified
ZEND_API void destroy_zend_class(zend_class_entry **pce)
{
	zend_class_entry *ce = *pce;
	
	if (--ce->refcount > 0) {
		return;
	}
	switch (ce->type) {
		case ZEND_USER_CLASS:
			zend_hash_destroy(&ce->default_properties);
			zend_hash_destroy(&ce->properties_info);
			zend_hash_destroy(&ce->default_static_members);
			efree(ce->name);
			zend_hash_destroy(&ce->function_table);
			zend_hash_destroy(&ce->constants_table);
			if (ce->num_interfaces > 0 && ce->interfaces) {
				efree(ce->interfaces);
			}
			if (ce->doc_comment) {
				efree(ce->doc_comment);
			}
			efree(ce);
			break;
		case ZEND_INTERNAL_CLASS:
			zend_hash_destroy(&ce->default_properties);
			zend_hash_destroy(&ce->properties_info);
			zend_hash_destroy(&ce->default_static_members);
			free(ce->name);
			zend_hash_destroy(&ce->function_table);
			zend_hash_destroy(&ce->constants_table);
			if (ce->num_interfaces > 0) {
				free(ce->interfaces);
			}
			if (ce->doc_comment) {
				free(ce->doc_comment);
			}
			free(ce);
			break;
	}
}

static void zend_destroy_property_info(zend_property_info *property_info)
{
	efree(property_info->name);
	if (property_info->doc_comment) {
		efree(property_info->doc_comment);
	}
}


static void zend_destroy_property_info_internal(zend_property_info *property_info)
{
	free(property_info->name);
}

ZEND_API void zend_initialize_class_data(zend_class_entry *ce, zend_bool nullify_handlers TSRMLS_DC)
{
	zend_bool persistent_hashes = (ce->type == ZEND_INTERNAL_CLASS) ? 1 : 0;
	dtor_func_t zval_ptr_dtor_func = ((persistent_hashes) ? ZVAL_INTERNAL_PTR_DTOR : ZVAL_PTR_DTOR);

	ce->refcount = 1;
	ce->constants_updated = 0;
	ce->ce_flags = 0;

	ce->doc_comment = NULL;
	ce->doc_comment_len = 0;

	zend_hash_init_ex(&ce->default_properties, 0, NULL, zval_ptr_dtor_func, persistent_hashes, 0);
	zend_hash_init_ex(&ce->properties_info, 0, NULL, (dtor_func_t) (persistent_hashes ? zend_destroy_property_info_internal : zend_destroy_property_info), persistent_hashes, 0);
	zend_hash_init_ex(&ce->default_static_members, 0, NULL, zval_ptr_dtor_func, persistent_hashes, 0);
	zend_hash_init_ex(&ce->constants_table, 0, NULL, zval_ptr_dtor_func, persistent_hashes, 0);
	zend_hash_init_ex(&ce->function_table, 0, NULL, ZEND_FUNCTION_DTOR, persistent_hashes, 0);

	if (ce->type == ZEND_INTERNAL_CLASS) {
#if 0 /* #if ZTS */ //TODO: Is there an equivalent of a compiler table in Phalanger? Is the code below needed?
		int n = zend_hash_num_elements(CG(class_table));

		if (CG(static_members) && n >= CG(last_static_member)) {
			/* Support for run-time declaration: dl() */
			CG(last_static_member) = n+1;
			CG(static_members) = realloc(CG(static_members), (n+1)*sizeof(HashTable*));
			CG(static_members)[n] = NULL;
		}
		ce->static_members = (HashTable*)(zend_intptr_t)n;
#else
		ce->static_members = NULL;
#endif
	} else {
		ce->static_members = &ce->default_static_members;
	}

	if (nullify_handlers) {
		ce->constructor = NULL;
		ce->destructor = NULL;
		ce->clone = NULL;
		ce->__get = NULL;
		ce->__set = NULL;
		ce->__unset = NULL;
		ce->__isset = NULL;
		ce->__call = NULL;
		ce->__callstatic = NULL;
		ce->__tostring = NULL;
		ce->create_object = NULL;
		ce->get_iterator = NULL;
		ce->iterator_funcs.funcs = NULL;
		ce->interface_gets_implemented = NULL;
		ce->get_static_method = NULL;
		ce->parent = NULL;
		ce->num_interfaces = 0;
		ce->interfaces = NULL;
		ce->module = NULL;
		ce->serialize = NULL;
		ce->unserialize = NULL;
		ce->serialize_func = NULL;
		ce->unserialize_func = NULL;
		ce->builtin_functions = NULL;
	}
}

void register_standard_class(TSRMLS_D)
{
	zend_standard_class_def.type = ZEND_INTERNAL_CLASS;
	zend_standard_class_def.name_length = sizeof("stdClass") - 1;
	zend_standard_class_def.name = zend_strndup("stdClass", zend_standard_class_def.name_length);
	zend_initialize_class_data(&zend_standard_class_def, 1 TSRMLS_CC);

//	zend_hash_add(CG(class_table), "stdclass", sizeof("stdclass"), &zend_standard_class_def, sizeof(zend_class_entry *), NULL);
}
#endif

//#pragma unmanaged

// copied from zend_API.c
ZEND_API int _object_init_ex(zval *arg, zend_class_entry *class_type ZEND_FILE_LINE_DC TSRMLS_DC)
{
	return _object_and_properties_init(arg, class_type, NULL ZEND_FILE_LINE_CC TSRMLS_CC);
}

// copied from zend_API.c
ZEND_API int _object_init(zval *arg ZEND_FILE_LINE_DC TSRMLS_DC)
{
	return _object_init_ex(arg, &zend_standard_class_def ZEND_FILE_LINE_CC TSRMLS_CC);
}

// copied from zend_API.c and beautified
#ifdef PHP4TS
ZEND_API int _object_and_properties_init(zval *arg, zend_class_entry *class_type, HashTable *properties ZEND_FILE_LINE_DC TSRMLS_DC)
{
	zval *tmp;

	if (!class_type->constants_updated)
	{
		zend_hash_apply_with_argument(&class_type->default_properties,
			(apply_func_arg_t)zval_update_constant, (void *)1 TSRMLS_CC);
		class_type->constants_updated = 1;
	}
	
	if (properties) arg->value.obj.properties = properties;
	else
	{
		ALLOC_HASHTABLE_REL(arg->value.obj.properties);
		zend_hash_init(arg->value.obj.properties, 0, NULL, ZVAL_PTR_DTOR, 0);
		zend_hash_copy(arg->value.obj.properties, &class_type->default_properties,
			(copy_ctor_func_t)zval_add_ref, (void *)&tmp, sizeof(zval *));
	}
	arg->type = IS_OBJECT;
	arg->value.obj.ce = class_type;
	return SUCCESS;
}

int zend_check_class(zval *obj, zend_class_entry *expected_ce)
{
	zend_class_entry *ce;

	if (Z_TYPE_P(obj) != IS_OBJECT) return 0;

	for (ce = Z_OBJCE_P(obj); ce != NULL; ce = ce->parent)
	{
		/*
		 * C'est une UGLY HACK.
		 */
		// LP: IMHO PHP is nothing but one UGLY HACK... :-) and BTW I am using the same trick
		// in Request::logicalThreadId :-O
		if (ce->refcount == expected_ce->refcount) return 1;
	}

	return 0;
}
#elif defined(PHP5TS)
ZEND_API void zend_objects_destroy_object(zend_object *object, zend_object_handle handle TSRMLS_DC)
{
	zend_function *destructor = object->ce->destructor;

	if (destructor) {
		zval *obj;
		zval *old_exception;

		if (destructor->op_array.fn_flags & (ZEND_ACC_PRIVATE|ZEND_ACC_PROTECTED)) {
			if (destructor->op_array.fn_flags & ZEND_ACC_PRIVATE) {
				/* Ensure that if we're calling a private function, we're allowed to do so.
				 */
				if (object->ce != EG(scope)) {
					zend_class_entry *ce = object->ce;

					zend_error(EG(in_execution) ? E_ERROR : E_WARNING, 
						"Call to private %s::__destruct() from context '%s'%s", 
						ce->name, 
						EG(scope) ? EG(scope)->name : "", 
						EG(in_execution) ? "" : " during shutdown ignored");
					return;
				}
			} else {
				/* Ensure that if we're calling a protected function, we're allowed to do so.
				 */
				if (!zend_check_protected(destructor->common.scope, EG(scope))) {
					zend_class_entry *ce = object->ce;

					zend_error(EG(in_execution) ? E_ERROR : E_WARNING, 
						"Call to protected %s::__destruct() from context '%s'%s", 
						ce->name, 
						EG(scope) ? EG(scope)->name : "", 
						EG(in_execution) ? "" : " during shutdown ignored");
					return;
				}
			}
		}

		MAKE_STD_ZVAL(obj);
		Z_TYPE_P(obj) = IS_OBJECT;
		Z_OBJ_HANDLE_P(obj) = handle;
		/* TODO: We cannot set proper handlers. */
		Z_OBJ_HT_P(obj) = &std_object_handlers; 
		zval_copy_ctor(obj);

		/* Make sure that destructors are protected from previously thrown exceptions.
		 * For example, if an exception was thrown in a function and when the function's
		 * local variable destruction results in a destructor being called.
		 */
		old_exception = EG(exception);
		EG(exception) = NULL;
#if 0 //Phalanger TODO: Do any of these need to get called? The object we have a handle of is a "fake", created solely to pass into the zend extension
		zend_call_method_with_0_params(&obj, object->ce, &destructor, ZEND_DESTRUCTOR_FUNC_NAME, NULL);
		if (old_exception) {
			if (EG(exception)) {
				zend_class_entry *default_exception_ce = zend_exception_get_default(TSRMLS_C);
				zval *file = zend_read_property(default_exception_ce, old_exception, "file", sizeof("file")-1, 1 TSRMLS_CC);
				zval *line = zend_read_property(default_exception_ce, old_exception, "line", sizeof("line")-1, 1 TSRMLS_CC);

				zval_ptr_dtor(&obj);
				zval_ptr_dtor(&EG(exception));
				EG(exception) = old_exception;
				zend_error(E_ERROR, "Ignoring exception from %s::__destruct() while an exception is already active (Uncaught %s in %s on line %ld)", 
					object->ce->name, Z_OBJCE_P(old_exception)->name, Z_STRVAL_P(file), Z_LVAL_P(line));
			}
			EG(exception) = old_exception;
		}
#endif
		zval_ptr_dtor(&obj);
	}
}

//copied from zend_objects.c
static void zval_add_ref_or_clone(zval **p)
{
	if (Z_TYPE_PP(p) == IS_OBJECT && !PZVAL_IS_REF(*p)) {
		TSRMLS_FETCH();

		if (Z_OBJ_HANDLER_PP(p, clone_obj) == NULL) {
			zend_error(E_ERROR, "Trying to clone an uncloneable object of class %s",  Z_OBJCE_PP(p)->name);
		} else {
			zval *orig = *p;

			ALLOC_ZVAL(*p);
			**p = *orig;
			INIT_PZVAL(*p);
			(*p)->value.obj = Z_OBJ_HT_PP(p)->clone_obj(orig TSRMLS_CC);
		}
	} else {
		(*p)->refcount++;
	}
}

//copied from zend_objects.c
ZEND_API void zend_object_std_init(zend_object *object, zend_class_entry *ce TSRMLS_DC)
{
	ALLOC_HASHTABLE(object->properties);
	zend_hash_init(object->properties, 0, NULL, ZVAL_PTR_DTOR, 0);

	object->ce = ce;	
	object->guards = NULL;
}
//copied from zend_objects.c
ZEND_API void zend_object_std_dtor(zend_object *object TSRMLS_DC)
{
	if (object->guards) {
		zend_hash_destroy(object->guards);
		FREE_HASHTABLE(object->guards);		
	}
	if (object->properties) {
		zend_hash_destroy(object->properties);
		FREE_HASHTABLE(object->properties);
	}
}

//copied from zend_objects.c
ZEND_API void zend_objects_clone_members(zend_object *new_object, zend_object_value new_obj_val, zend_object *old_object, zend_object_handle handle TSRMLS_DC)
{
	if (EG(ze1_compatibility_mode)) {
		zend_hash_copy(new_object->properties, old_object->properties, (copy_ctor_func_t) zval_add_ref_or_clone, (void *) NULL /* Not used anymore */, sizeof(zval *));
	} else {
		zend_hash_copy(new_object->properties, old_object->properties, (copy_ctor_func_t) zval_add_ref, (void *) NULL /* Not used anymore */, sizeof(zval *));
	}
	if (old_object->ce->clone) {
		zval *new_obj;

		MAKE_STD_ZVAL(new_obj);
		new_obj->type = IS_OBJECT;
		new_obj->value.obj = new_obj_val;
		zval_copy_ctor(new_obj);

		zend_call_method_with_0_params(&new_obj, old_object->ce, &old_object->ce->clone, ZEND_CLONE_FUNC_NAME, NULL);

		zval_ptr_dtor(&new_obj);
	}
}

ZEND_API void zend_objects_free_object_storage(zend_object *object TSRMLS_DC)
{
	zend_object_std_dtor(object TSRMLS_CC);
	efree(object);
}


ZEND_API zend_object_value zend_objects_new(zend_object **object, zend_class_entry *class_type TSRMLS_DC)
{	
	zend_object_value retval;

	*object = (zend_object *)emalloc(sizeof(zend_object));
	(*object)->ce = class_type;
	retval.handle = zend_objects_store_put(*object, (zend_objects_store_dtor_t) zend_objects_destroy_object, (zend_objects_free_object_storage_t) zend_objects_free_object_storage, NULL TSRMLS_CC);
	retval.handlers = &std_object_handlers;
	(*object)->guards = NULL;
	return retval;
}

//copied from zend_api.c, removed references to compiler globals (CG)
ZEND_API void zend_update_class_constants(zend_class_entry *class_type TSRMLS_DC)
{
	if (!class_type->constants_updated || !CE_STATIC_MEMBERS(class_type)) {
		zend_class_entry **scope = &EG(scope); //EG(in_execution)?&EG(scope):&CG(active_class_entry); //Phalanger removed
		zend_class_entry *old_scope = *scope;

		*scope = class_type;
		zend_hash_apply_with_argument(&class_type->constants_table, (apply_func_arg_t) zval_update_constant, (void*)1 TSRMLS_CC);
		zend_hash_apply_with_argument(&class_type->default_properties, (apply_func_arg_t) zval_update_constant, (void *) 1 TSRMLS_CC);

		if (!CE_STATIC_MEMBERS(class_type)) {
			HashPosition pos;
			zval **p;

			if (class_type->parent) {
				zend_update_class_constants(class_type->parent TSRMLS_CC);
			}
#if 0 /* #if ZTS */
			ALLOC_HASHTABLE(CG(static_members)[(zend_intptr_t)(class_type->static_members)]);
#else
			ALLOC_HASHTABLE(class_type->static_members);
#endif
			zend_hash_init(CE_STATIC_MEMBERS(class_type), zend_hash_num_elements(&class_type->default_static_members), NULL, ZVAL_PTR_DTOR, 0);

			zend_hash_internal_pointer_reset_ex(&class_type->default_static_members, &pos);
			while (zend_hash_get_current_data_ex(&class_type->default_static_members, (void**)&p, &pos) == SUCCESS) {
				char *str_index;
				uint str_length;
				ulong num_index;
				zval **q;

				zend_hash_get_current_key_ex(&class_type->default_static_members, &str_index, &str_length, &num_index, 0, &pos);
				if ((*p)->is_ref &&
				    class_type->parent &&
				    zend_hash_find(&class_type->parent->default_static_members, str_index, str_length, (void**)&q) == SUCCESS &&
				    *p == *q &&
				    zend_hash_find(CE_STATIC_MEMBERS(class_type->parent), str_index, str_length, (void**)&q) == SUCCESS) {
					(*q)->refcount++;
					(*q)->is_ref = 1;
					zend_hash_add(CE_STATIC_MEMBERS(class_type), str_index, str_length, (void**)q, sizeof(zval*), NULL);
				} else {
					zval *q;

					ALLOC_ZVAL(q);
					*q = **p;
					INIT_PZVAL(q);
					zval_copy_ctor(q);
					zend_hash_add(CE_STATIC_MEMBERS(class_type), str_index, str_length, (void**)&q, sizeof(zval*), NULL);
				}
				zend_hash_move_forward_ex(&class_type->default_static_members, &pos);
			}
		}
		zend_hash_apply_with_argument(CE_STATIC_MEMBERS(class_type), (apply_func_arg_t) zval_update_constant, (void *) 1 TSRMLS_CC);

		*scope = old_scope;
		class_type->constants_updated = 1;
	}
}

/* This function requires 'properties' to contain all props declared in the
 * class and all props being public. If only a subset is given or the class
 * has protected members then you need to merge the properties seperately by
 * calling zend_merge_properties(). */
ZEND_API int _object_and_properties_init(zval *arg, zend_class_entry *class_type, HashTable *properties ZEND_FILE_LINE_DC TSRMLS_DC)
{
	zval *tmp;
	zend_object *object;

	if (class_type->ce_flags & (ZEND_ACC_INTERFACE|ZEND_ACC_IMPLICIT_ABSTRACT_CLASS|ZEND_ACC_EXPLICIT_ABSTRACT_CLASS)) {
		char *what = class_type->ce_flags & ZEND_ACC_INTERFACE ? "interface" : "abstract class";
		zend_error(E_ERROR, "Cannot instantiate %s %s", what, class_type->name);
	}

	zend_update_class_constants(class_type TSRMLS_CC);

	Z_TYPE_P(arg) = IS_OBJECT;
	if (class_type->create_object == NULL) {
		Z_OBJVAL_P(arg) = zend_objects_new(&object, class_type TSRMLS_CC);
		if (properties) {
			object->properties = properties;
		} else {
			ALLOC_HASHTABLE_REL(object->properties);
			zend_hash_init(object->properties, zend_hash_num_elements(&class_type->default_properties), NULL, ZVAL_PTR_DTOR, 0);
			zend_hash_copy(object->properties, &class_type->default_properties, (copy_ctor_func_t) zval_add_ref, (void *) &tmp, sizeof(zval *));
		}
	} else {
		Z_OBJVAL_P(arg) = class_type->create_object(class_type TSRMLS_CC);
	}
	return SUCCESS;
}

ZEND_API zend_class_entry *zend_get_class_entry(zval *zobject TSRMLS_DC)
{
	if (Z_OBJ_HT_P(zobject)->get_class_entry) {
		return Z_OBJ_HT_P(zobject)->get_class_entry(zobject TSRMLS_CC);
	} else {
		zend_error(E_ERROR, "Class entry requested for an object without PHP class");
		return NULL;
	}
}

/* returns 1 if you need to copy result, 0 if it's already a copy */
ZEND_API int zend_get_object_classname(zval *object, char **class_name, zend_uint *class_name_len TSRMLS_DC)
{
	if (Z_OBJ_HT_P(object)->get_class_name == NULL ||
		Z_OBJ_HT_P(object)->get_class_name(object, class_name, class_name_len, 0 TSRMLS_CC) != SUCCESS) {
		zend_class_entry *ce = Z_OBJCE_P(object);

		*class_name = ce->name;
		*class_name_len = ce->name_length;
		return 1;
	}
	return 0;
}

// copied from zend_API.c
ZEND_API int zend_declare_class_constant(zend_class_entry *ce, const char *name, size_t name_length, zval *value TSRMLS_DC) /* {{{ */
{
	return zend_hash_update(&ce->constants_table, (char*)name, name_length+1, &value, sizeof(zval *), NULL);
}
ZEND_API int zend_declare_class_constant_null(zend_class_entry *ce, const char *name, size_t name_length TSRMLS_DC) /* {{{ */
{
	zval *constant;

	if (ce->type & ZEND_INTERNAL_CLASS) {
		ALLOC_PERMANENT_ZVAL(constant);
	} else {
		ALLOC_ZVAL(constant);
	}
	ZVAL_NULL(constant);
	INIT_PZVAL(constant);
	return zend_declare_class_constant(ce, name, name_length, constant TSRMLS_CC);
}
ZEND_API int zend_declare_class_constant_long(zend_class_entry *ce, const char *name, size_t name_length, long value TSRMLS_DC) /* {{{ */
{
	zval *constant;

	if (ce->type & ZEND_INTERNAL_CLASS) {
		ALLOC_PERMANENT_ZVAL(constant);
	} else {
		ALLOC_ZVAL(constant);
	}
	ZVAL_LONG(constant, value);
	INIT_PZVAL(constant);
	return zend_declare_class_constant(ce, name, name_length, constant TSRMLS_CC);
}
ZEND_API int zend_declare_class_constant_bool(zend_class_entry *ce, const char *name, size_t name_length, zend_bool value TSRMLS_DC) /* {{{ */
{
	zval *constant;

	if (ce->type & ZEND_INTERNAL_CLASS) {
		ALLOC_PERMANENT_ZVAL(constant);
	} else {
		ALLOC_ZVAL(constant);
	}
	ZVAL_BOOL(constant, value);
	INIT_PZVAL(constant);
	return zend_declare_class_constant(ce, name, name_length, constant TSRMLS_CC);
}
ZEND_API int zend_declare_class_constant_double(zend_class_entry *ce, const char *name, size_t name_length, double value TSRMLS_DC) /* {{{ */
{
	zval *constant;

	if (ce->type & ZEND_INTERNAL_CLASS) {
		ALLOC_PERMANENT_ZVAL(constant);
	} else {
		ALLOC_ZVAL(constant);
	}
	ZVAL_DOUBLE(constant, value);
	INIT_PZVAL(constant);
	return zend_declare_class_constant(ce, name, name_length, constant TSRMLS_CC);
}
ZEND_API int zend_declare_class_constant_stringl(zend_class_entry *ce, const char *name, size_t name_length, const char *value, size_t value_length TSRMLS_DC) /* {{{ */
{
	zval *constant;

	if (ce->type & ZEND_INTERNAL_CLASS) {
		ALLOC_PERMANENT_ZVAL(constant);
		ZVAL_STRINGL(constant, zend_strndup(value, value_length), value_length, 0);
	} else {
		ALLOC_ZVAL(constant);
		ZVAL_STRINGL(constant, (char*)value, value_length, 1);
	}
	INIT_PZVAL(constant);
	return zend_declare_class_constant(ce, name, name_length, constant TSRMLS_CC);
}
ZEND_API int zend_declare_class_constant_string(zend_class_entry *ce, const char *name, size_t name_length, const char *value TSRMLS_DC) /* {{{ */
{
	return zend_declare_class_constant_stringl(ce, name, name_length, value, strlen(value) TSRMLS_CC);
}

ZEND_API zval *zend_read_property(zend_class_entry *scope, zval *object, char *name, int name_length, zend_bool silent TSRMLS_DC)
{
	zval *property, *value;
	zend_class_entry *old_scope = EG(scope);

	EG(scope) = scope;

	if (!Z_OBJ_HT_P(object)->read_property) {
		char *class_name;
		zend_uint class_name_len;

		zend_get_object_classname(object, &class_name, &class_name_len TSRMLS_CC);
		zend_error(E_CORE_ERROR, "Property %s of class %s cannot be read", name, class_name);
	}

	MAKE_STD_ZVAL(property);
	ZVAL_STRINGL(property, name, name_length, 1);
	value = Z_OBJ_HT_P(object)->read_property(object, property, silent?BP_VAR_IS:BP_VAR_R TSRMLS_CC);
	zval_ptr_dtor(&property);

	EG(scope) = old_scope;
	return value;
}

//Copied from zend_objects.c
ZEND_API zend_object_value zend_objects_clone_obj(zval *zobject TSRMLS_DC)
{
	zend_object_value new_obj_val;
	zend_object *old_object;
	zend_object *new_object;
	zend_object_handle handle = Z_OBJ_HANDLE_P(zobject);

	/* assume that create isn't overwritten, so when clone depends on the 
	 * overwritten one then it must itself be overwritten */
	old_object = zend_objects_get_address(zobject TSRMLS_CC);
	new_obj_val = zend_objects_new(&new_object, old_object->ce TSRMLS_CC);

	ALLOC_HASHTABLE(new_object->properties);
	zend_hash_init(new_object->properties, 0, NULL, ZVAL_PTR_DTOR, 0);

	zend_objects_clone_members(new_object, new_obj_val, old_object, handle TSRMLS_CC);

	return new_obj_val;
}

/* Store objects API */

//copied from zend_objects_api.c
ZEND_API void zend_objects_store_init(zend_objects_store *objects, zend_uint init_size)
{
	objects->object_buckets = (zend_object_store_bucket *) emalloc(init_size * sizeof(zend_object_store_bucket));
	objects->top = 1; /* Skip 0 so that handles are true */
	objects->size = init_size;
	objects->free_list_head = -1;
	memset(&objects->object_buckets[0], 0, sizeof(zend_object_store_bucket));
}

//copied from zend_objects_api.c
ZEND_API void zend_objects_store_destroy(zend_objects_store *objects)
{
	efree(objects->object_buckets);
	objects->object_buckets = NULL;
}

//copied from zend_objects.c
ZEND_API zend_object_handle zend_objects_store_put(void *object, zend_objects_store_dtor_t dtor, zend_objects_free_object_storage_t free_storage, zend_objects_store_clone_t clone TSRMLS_DC)
{
	zend_object_handle handle;
	_zend_object_store_bucket::_store_bucket::_store_object *obj;

	if (EG(objects_store).free_list_head != -1) {
		handle = EG(objects_store).free_list_head;
		EG(objects_store).free_list_head = EG(objects_store).object_buckets[handle].bucket.free_list.next;
	} else {
		if (EG(objects_store).top == EG(objects_store).size) {
			EG(objects_store).size <<= 1;
			EG(objects_store).object_buckets = (zend_object_store_bucket *) erealloc(EG(objects_store).object_buckets, EG(objects_store).size * sizeof(zend_object_store_bucket));
		}
		handle = EG(objects_store).top++;
	}
	obj = &EG(objects_store).object_buckets[handle].bucket.obj;
	EG(objects_store).object_buckets[handle].destructor_called = 0;
	EG(objects_store).object_buckets[handle].valid = 1;

	obj->refcount = 1;
	obj->object = object;
	obj->dtor = dtor?dtor:(zend_objects_store_dtor_t)zend_objects_destroy_object;
	obj->free_storage = free_storage;

	obj->clone = clone;

#if ZEND_DEBUG_OBJECTS
	fprintf(stderr, "Allocated object id #%d\n", handle);
#endif
	return handle;
}

ZEND_API zend_uint zend_objects_store_get_refcount(zval *object TSRMLS_DC)
{
	zend_object_handle handle = Z_OBJ_HANDLE_P(object);

	return EG(objects_store).object_buckets[handle].bucket.obj.refcount;
}

ZEND_API void zend_objects_store_add_ref(zval *object TSRMLS_DC)
{
	zend_object_handle handle = Z_OBJ_HANDLE_P(object);

	EG(objects_store).object_buckets[handle].bucket.obj.refcount++;
#if ZEND_DEBUG_OBJECTS
	fprintf(stderr, "Increased refcount of object id #%d\n", handle);
#endif
}

//copied from zend_objects_api.c
ZEND_API void zend_objects_store_del_ref(zval *zobject TSRMLS_DC)
{
	zend_object_handle handle;

	handle = Z_OBJ_HANDLE_P(zobject);

	zobject->refcount++;
	zend_objects_store_del_ref_by_handle(handle TSRMLS_CC);
	zobject->refcount--;
}

/*
 * Delete a reference to an objects store entry given the object handle.
 */
//Copied from zend_objects.c, modified to be C++ compliant, removed zend_try/catch/end_try
ZEND_API void zend_objects_store_del_ref_by_handle(zend_object_handle handle TSRMLS_DC)
{
	_zend_object_store_bucket::_store_bucket::_store_object *obj;
	int failure = 0;

	if (!EG(objects_store).object_buckets) {
		return;
	}

	obj = &EG(objects_store).object_buckets[handle].bucket.obj;

	/*	Make sure we hold a reference count during the destructor call
		otherwise, when the destructor ends the storage might be freed
		when the refcount reaches 0 a second time
	 */
	if (EG(objects_store).object_buckets[handle].valid) {
		if (obj->refcount == 1) {
			if (!EG(objects_store).object_buckets[handle].destructor_called) {
				EG(objects_store).object_buckets[handle].destructor_called = 1;

				if (obj->dtor) {
					//zend_try {
						obj->dtor(obj->object, handle TSRMLS_CC);
					//} zend_catch {
					//	failure = 1;
					//} zend_end_try();
				}
			}
			if (obj->refcount == 1) {
				if (obj->free_storage) {
					//zend_try {
						obj->free_storage(obj->object TSRMLS_CC);
					//} zend_catch {
					//	failure = 1;
					//} zend_end_try();
				}
				ZEND_OBJECTS_STORE_ADD_TO_FREE_LIST();
			}
		}
	}

	obj->refcount--;

#if ZEND_DEBUG_OBJECTS
	if (obj->refcount == 0) {
		fprintf(stderr, "Deallocated object id #%d\n", handle);
	} else {
		fprintf(stderr, "Decreased refcount of object id #%d\n", handle);
	}
#endif
	if (failure) {
		zend_bailout();
	}
}

//Copied from zend_objects_api.c
ZEND_API void *zend_object_store_get_object(zval *zobject TSRMLS_DC)
{
	zend_object_handle handle = Z_OBJ_HANDLE_P(zobject);

	return EG(objects_store).object_buckets[handle].bucket.obj.object;
}

//Copied from zend_objects_api.c
ZEND_API void *zend_object_store_get_object_by_handle(zend_object_handle handle TSRMLS_DC)
{
	return EG(objects_store).object_buckets[handle].bucket.obj.object;
}

//Copied from zend_objects.c
ZEND_API zend_object *zend_objects_get_address(zval *zobject TSRMLS_DC)
{
	return (zend_object *)zend_object_store_get_object(zobject TSRMLS_CC);
}

//Copied from zend_interfaces.c
/* {{{ zend_call_method
 Only returns the returned zval if retval_ptr != NULL */
ZEND_API zval* zend_call_method(zval **object_pp, zend_class_entry *obj_ce, zend_function **fn_proxy, char *function_name, int function_name_len, zval **retval_ptr_ptr, int param_count, zval* arg1, zval* arg2 TSRMLS_DC)
{
	int result;
	zend_fcall_info fci;
	zval z_fname;
	zval *retval;
	HashTable *function_table;

	zval **params[2];

	params[0] = &arg1;
	params[1] = &arg2;

	fci.size = sizeof(fci);
	/*fci.function_table = NULL; will be read form zend_class_entry of object if needed */
	fci.object_ptr = object_pp ? *object_pp : NULL;
	fci.function_name = &z_fname;
	fci.retval_ptr_ptr = retval_ptr_ptr ? retval_ptr_ptr : &retval;
	fci.param_count = param_count;
	fci.params = params;
	fci.no_separation = 1;
	fci.symbol_table = NULL;

	if (!fn_proxy && !obj_ce) {
		/* no interest in caching and no information already present that is
		 * needed later inside zend_call_function. */
		ZVAL_STRINGL(&z_fname, function_name, function_name_len, 0);
		fci.function_table = !object_pp ? EG(function_table) : NULL;
		result = zend_call_function(&fci, NULL TSRMLS_CC);
	} else {
		zend_fcall_info_cache fcic;

		fcic.initialized = 1;
		if (!obj_ce) {
			obj_ce = object_pp ? Z_OBJCE_PP(object_pp) : NULL;
		}
		if (obj_ce) {
			function_table = &obj_ce->function_table;
		} else {
			function_table = EG(function_table);
		}
		if (!fn_proxy || !*fn_proxy) {
			if (zend_hash_find(function_table, function_name, function_name_len+1, (void **) &fcic.function_handler) == FAILURE) {
				/* error at c-level */
				zend_error(E_CORE_ERROR, "Couldn't find implementation for method %s%s%s", obj_ce ? obj_ce->name : "", obj_ce ? "::" : "", function_name);
			}
			if (fn_proxy) {
				*fn_proxy = fcic.function_handler;
			}
		} else {
			fcic.function_handler = *fn_proxy;
		}
		fcic.calling_scope = obj_ce;
		fcic.object_ptr = object_pp ? *object_pp : NULL;
		result = zend_call_function(&fci, &fcic TSRMLS_CC);
	}
	if (result == FAILURE) {
		/* error at c-level */
		if (!obj_ce) {
			obj_ce = object_pp ? Z_OBJCE_PP(object_pp) : NULL;
		}
		if (!EG(exception)) {
			zend_error(E_CORE_ERROR, "Couldn't execute method %s%s%s", obj_ce ? obj_ce->name : "", obj_ce ? "::" : "", function_name);
		}
	}
	if (!retval_ptr_ptr) {
		if (retval) {
			zval_ptr_dtor(&retval);
		}
		return NULL;
	}
	return *retval_ptr_ptr;
}
/* }}} */
ZEND_API zend_object_handlers *zend_get_std_object_handlers(void)
{
	return &std_object_handlers;
}

#endif
