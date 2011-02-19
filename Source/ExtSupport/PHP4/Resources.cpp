//
// ExtSupport.PHP4 - substitute for php4ts.dll
//
// Resources.cpp 
// - contains definitions of resources related functions
//

#include "stdafx.h"
#include "Resources.h"
#include "Request.h"
#include "Errors.h"
#include "Hash.h"
#include "ThreadSafeHash.h"
#include "Parameters.h"
#include "TsrmLs.h"

using namespace System;
using namespace System::Collections;

using namespace PHP::ExtManager;


int le_index_ptr;  /* list entry type for index pointers */

static TsHashTable list_destructors;

// copied from zend_list.c and beautified
static void list_entry_destructor(void *param)
{
	zend_rsrc_list_dtors_entry *ld;
	zend_rsrc_list_entry *le = (zend_rsrc_list_entry *)param;

	TSRMLS_FETCH();

	if (zend_ts_hash_index_find(&list_destructors, le->type, (void **)&ld) == SUCCESS)
	{
		switch (ld->type) 
		{
			case ZEND_RESOURCE_LIST_TYPE_STD:
				if (ld->list_dtor) ld->list_dtor(le->ptr);
				break;

			case ZEND_RESOURCE_LIST_TYPE_EX:
				if (ld->list_dtor_ex) ld->list_dtor_ex(le TSRMLS_CC);
				break;

			EMPTY_SWITCH_DEFAULT_CASE();
		}
	} 
	else zend_error(E_WARNING, "Unknown list entry type in request shutdown (%d)", le->type);
}

// copied from zend_list.c and beautified
void plist_entry_destructor(void *ptr)
{
	zend_rsrc_list_dtors_entry *ld;
	zend_rsrc_list_entry *le = (zend_rsrc_list_entry *)ptr;

	TSRMLS_FETCH();

	if (zend_ts_hash_index_find(&list_destructors, le->type, (void **)&ld) == SUCCESS)
	{
		switch (ld->type)
		{
			case ZEND_RESOURCE_LIST_TYPE_STD:
				if (ld->plist_dtor) ld->plist_dtor(le->ptr);
				break;

			case ZEND_RESOURCE_LIST_TYPE_EX:
				if (ld->plist_dtor_ex) ld->plist_dtor_ex(le TSRMLS_CC);
				break;

			EMPTY_SWITCH_DEFAULT_CASE()
		}
	} 
	else zend_error(E_WARNING, "Unknown persistent list entry type in module shutdown (%d)", le->type);
}

// copied from zend_list.c and beautified
#ifdef PHP5TS
int zend_init_rsrc_list(HashTable *ht TSRMLS_DC)
{
	if (zend_hash_init(ht, 0, NULL, list_entry_destructor, 0) == SUCCESS)
	{
		ht->nNextFreeElement = 1;	/* we don't want resource id 0 */
		return SUCCESS;
	} 
	else return FAILURE;
}
#else
int zend_init_rsrc_list(HashTable *ht)
{
	if (zend_hash_init(ht, 0, NULL, list_entry_destructor, 0) == SUCCESS)
	{
		ht->nNextFreeElement = 1;	/* we don't want resource id 0 */
		return SUCCESS;
	} 
	else return FAILURE;
}
#endif

// copied from zend_list.c and beautified
int zend_init_rsrc_list_dtors()
{
	int retval;

	retval = zend_ts_hash_init(&list_destructors, 50, NULL, NULL, 1);
	list_destructors.hash.nNextFreeElement = 1;	/* we don't want resource type 0 */

	return retval;
}

// copied from zend_list.c and beautified
void zend_destroy_rsrc_list_dtors()
{
	zend_ts_hash_destroy(&list_destructors);
}

#ifdef PHP5TS
// copied from zend_list.c
void zend_destroy_rsrc_list(HashTable *ht TSRMLS_DC)
{
	zend_hash_graceful_reverse_destroy(ht);
}
#else
// copied from zend_list.c
void zend_destroy_rsrc_list(HashTable *ht)
{
	zend_hash_graceful_reverse_destroy(ht);
}
#endif

// registers a new global destructor list
ZEND_API int zend_register_list_destructors(void (*ld)(void *), void (*pld)(void *), int module_number)
{
#ifdef DEBUG
	Debug::WriteLine("PHP4TS", "Registering list destructors");
#endif

	begin_write(&list_destructors);
	try
	{
		zend_rsrc_list_dtors_entry entry;
		entry.list_dtor     = (void (*)(void *))ld;
		entry.plist_dtor    = (void (*)(void *))pld;
		entry.list_dtor_ex  = NULL;
		entry.plist_dtor_ex	= NULL;
		entry.module_number	= module_number;
		entry.type          = ZEND_RESOURCE_LIST_TYPE_STD;
		entry.type_name     = NULL;
		entry.resource_id   = list_destructors.hash.nNextFreeElement;

		if (zend_hash_next_index_insert(&list_destructors.hash, (void *)&entry, sizeof(zend_rsrc_list_dtors_entry), NULL) == FAILURE)
		{
			return FAILURE;
		}
		return list_destructors.hash.nNextFreeElement - 1;
	}
	finally
	{
		end_write(&list_destructors);
	}
}

// registers a new global destructor list
ZEND_API int zend_register_list_destructors_ex(rsrc_dtor_func_t ld, rsrc_dtor_func_t pld, char *type_name, int module_number)
{
#ifdef DEBUG
	Debug::WriteLine("PHP4TS", String::Concat("Registering list destructors for resource type: ", 
		(type_name ? gcnew String(type_name) : "(unknown)")));
#endif

	begin_write(&list_destructors);
	try
	{
		zend_rsrc_list_dtors_entry entry;
		entry.list_dtor     = NULL;
		entry.plist_dtor    = NULL;
		entry.list_dtor_ex  = ld;
		entry.plist_dtor_ex = pld;
		entry.module_number = module_number;
		entry.type          = ZEND_RESOURCE_LIST_TYPE_EX;
		entry.type_name     = type_name;
		entry.resource_id   = list_destructors.hash.nNextFreeElement;

		if (zend_hash_next_index_insert(&list_destructors.hash, (void *)&entry, sizeof(zend_rsrc_list_dtors_entry), NULL) == FAILURE)
		{
			return FAILURE;
		}
		return list_destructors.hash.nNextFreeElement - 1;
	}
	finally
	{
		end_write(&list_destructors);
	}
}

// copied from zend_list.c, modified and beautified
ZEND_API int zend_list_insert(void *ptr, int type)
{
	Request ^request = Request::GetCurrentRequest();

#ifdef DEBUG
	Debug::WriteLine("PHP4TS", String::Format("zend_list_insert invoked for type {0}", type));
	Debug::WriteLine("PHP4TS", String::Format("inserting {0:x}", (unsigned)ptr));
#endif

	zend_rsrc_list_entry le;
	le.ptr		= ptr;
	le.type		= type;
	le.refcount	= 1;

	int index = zend_hash_next_free_element(request->Resources);
	zend_hash_index_update(request->Resources, index, (void *)&le, sizeof(zend_rsrc_list_entry), NULL);
	return index;
}

// copied from zend_list.c, modified and beautified
ZEND_API void *_zend_list_find(int id, int *type TSRMLS_DC)
{
#ifdef DEBUG
	Debug::WriteLine("PHP4TS", String::Format("_zend_list_find invoked for id {0}", id));
#endif

	Request ^request = Request::GetCurrentRequest();
	zend_rsrc_list_entry *le;

	if (zend_hash_index_find(request->Resources, id, (void **)&le) == SUCCESS)
	{
		*type = le->type;
#ifdef DEBUG
		Debug::WriteLine("PHP4TS", String::Format("returning {0:x}", (unsigned)le->ptr));
#endif
		return le->ptr;
	} 
	else 
	{
		*type = -1;
		return NULL;
	}
}

// copied from zend_list.c, modified and beautified
ZEND_API int _zend_list_addref(int id TSRMLS_DC)
{
#ifdef DEBUG
	Debug::WriteLine("PHP4TS", "_zend_list_addref invoked.");
#endif

	zend_rsrc_list_entry *le;
	Request ^request = Request::GetCurrentRequest();

	if (zend_hash_index_find(request->Resources, id, (void **)&le) == SUCCESS)
	{
		le->refcount++;
		return SUCCESS;
	} 
	else return FAILURE;
}

// copied from zend_list.c, modified and beautified
ZEND_API int _zend_list_delete(int id TSRMLS_DC)
{
#ifdef DEBUG
	Debug::WriteLine("PHP4TS", "_zend_list_delete invoked.");
#endif

	Request ^request = Request::GetCurrentRequest();
	zend_rsrc_list_entry *le;
	
	if (zend_hash_index_find(request->Resources, id, (void **)&le) == SUCCESS) 
	{
		if (--le->refcount <= 0) return zend_hash_index_del(request->Resources, id);
		else return SUCCESS;
	} 
	else return FAILURE;
}

// copied from zend_list.c, modified and beautified
ZEND_API char *zend_rsrc_list_get_rsrc_type(int resource TSRMLS_DC)
{
	zend_rsrc_list_dtors_entry *lde;
	int rsrc_type;

	if (!_zend_list_find(resource, &rsrc_type, tsrm_ls)) return NULL;

	if (zend_ts_hash_index_find(&list_destructors, rsrc_type, (void **)&lde) == SUCCESS)
	{
		return lde->type_name;
	}
	else return NULL;
}

// copied from zend_list.c, modified and beautified
ZEND_API int zend_fetch_list_dtor_id(char *type_name)
{
	zend_rsrc_list_dtors_entry *lde;
	HashPosition pos;

	begin_write(&list_destructors);
	try
	{
		zend_hash_internal_pointer_reset_ex(&list_destructors.hash, &pos);
		while (zend_hash_get_current_data_ex(&list_destructors.hash, (void **)&lde, &pos) == SUCCESS)
		{
			if (lde->type_name && (strcmp(type_name, lde->type_name) == 0)) return lde->resource_id;
			zend_hash_move_forward_ex(&list_destructors.hash, &pos);
		}
	}
	finally
	{
		end_write(&list_destructors);
	}

	return 0;
}

// copied from zend_list.c and beautified
ZEND_API int zend_register_resource(zval *rsrc_result, void *rsrc_pointer, int rsrc_type)
{
	int rsrc_id;

	rsrc_id = zend_list_insert(rsrc_pointer, rsrc_type);
	
	if (rsrc_result)
	{
		rsrc_result->value.lval = rsrc_id;
		rsrc_result->type = IS_RESOURCE;
	}

	return rsrc_id;
}

#pragma unmanaged

// copied from zend_list.c and beautified
ZEND_API void *zend_fetch_resource(zval **passed_id TSRMLS_DC, int default_id, char *resource_type_name,
								   int *found_resource_type, int num_resource_types, ...)
{
	int id, actual_resource_type, i;
	void *resource;
	va_list resource_types;

	if (default_id == -1) /* use id */
	{
		if (!passed_id)
		{
			if (resource_type_name) 
			{
				zend_error(E_WARNING, "%s(): no %s resource supplied", get_active_function_name(TSRMLS_C), resource_type_name);
			}
			return NULL;
		} 
		else if ((*passed_id)->type != IS_RESOURCE) 
		{
			if (resource_type_name) 
			{
				zend_error(E_WARNING, "%s(): supplied argument is not a valid %s resource", get_active_function_name(TSRMLS_C), 
					resource_type_name);
			}
			return NULL;
		}
		id = (*passed_id)->value.lval;
	} 
	else id = default_id;

	resource = _zend_list_find(id, &actual_resource_type, tsrm_ls);
	if (!resource) 
	{
		if (resource_type_name) 
		{
			zend_error(E_WARNING, "%s(): %d is not a valid %s resource", get_active_function_name(TSRMLS_C), 
				id, resource_type_name);
		}
		return NULL;
	}

	va_start(resource_types, num_resource_types);
	for (i = 0; i < num_resource_types; i++)
	{
		if (actual_resource_type == va_arg(resource_types, int))
		{
			va_end(resource_types);
			if (found_resource_type) *found_resource_type = actual_resource_type;
			return resource;
		}
	}
	va_end(resource_types);

	if (resource_type_name)
	{
		zend_error(E_WARNING, "%s(): supplied resource is not a valid %s resource",
			get_active_function_name(TSRMLS_C), resource_type_name);
	}

	return NULL;
}
