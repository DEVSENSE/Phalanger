//
// ExtSupport.PHP4 - substitute for php4ts.dll
//
// Resources.h
// - contains declarations of resources related exported functions
//

#pragma once

#include "stdafx.h"
#include "ExtSupport.h"
#include "Hash.h"

using namespace System;
using namespace System::Threading;

#define ZEND_RESOURCE_LIST_TYPE_STD	1
#define ZEND_RESOURCE_LIST_TYPE_EX	2

struct zend_rsrc_list_entry
{
	void *ptr;
	int type;
	int refcount;
};

#define list_entry			zend_rsrc_list_entry

typedef void (*rsrc_dtor_func_t)(zend_rsrc_list_entry *rsrc TSRMLS_DC);
#define ZEND_RSRC_DTOR_FUNC(name) void name(zend_rsrc_list_entry *rsrc TSRMLS_DC)

struct zend_rsrc_list_dtors_entry
{
	/* old style destructors */
	void (*list_dtor)(void *);
	void (*plist_dtor)(void *);

	/* new style destructors */
	rsrc_dtor_func_t list_dtor_ex;
	rsrc_dtor_func_t plist_dtor_ex;

	char *type_name;

	int module_number;
	int resource_id;
	unsigned char type;
};

#define ZEND_VERIFY_RESOURCE(rsrc)		\
	if (!rsrc)							\
	{									\
		RETURN_NULL();					\
	}

#define ZEND_FETCH_RESOURCE(rsrc, rsrc_type, passed_id, default_id, resource_type_name, resource_type)	\
	rsrc = (rsrc_type) zend_fetch_resource(passed_id TSRMLS_CC, default_id, resource_type_name, NULL, 1, resource_type);	\
	ZEND_VERIFY_RESOURCE(rsrc);

#define ZEND_FETCH_RESOURCE2(rsrc, rsrc_type, passed_id, default_id, resource_type_name, resource_type1, resource_type2)	\
	rsrc = (rsrc_type) zend_fetch_resource(passed_id TSRMLS_CC, default_id, resource_type_name, NULL, 2, resource_type1, resource_type2);	\
	ZEND_VERIFY_RESOURCE(rsrc);

#define ZEND_REGISTER_RESOURCE(rsrc_result, rsrc_pointer, rsrc_type)  \
    zend_register_resource(rsrc_result, rsrc_pointer, rsrc_type);

#define ZEND_GET_RESOURCE_TYPE_ID(le_id, le_type_name)	\
    if (le_id == 0)										\
	{													\
        le_id = zend_fetch_list_dtor_id(le_type_name);	\
	}

#define zend_list_addref(id)		_zend_list_addref(id TSRMLS_CC)
#define zend_list_delete(id)		_zend_list_delete(id TSRMLS_CC)
#define zend_list_find(id, type)	_zend_list_find(id, type TSRMLS_CC)

#ifdef __cplusplus
extern "C"
{
#endif

void zend_destroy_rsrc_list_dtors();
int zend_init_rsrc_list_dtors();
#ifdef PHP5TS
int zend_init_rsrc_list(HashTable *ht TSRMLS_DC);
void zend_destroy_rsrc_list(HashTable *ht TSRMLS_DC);
#else
int zend_init_rsrc_list(HashTable *ht);
void zend_destroy_rsrc_list(HashTable *ht);
#endif


extern ZEND_API int le_index_ptr;  /* list entry type for index pointers */

ZEND_API int zend_register_list_destructors(void (*ld)(void *), void (*pld)(void *), int module_number);
ZEND_API int zend_register_list_destructors_ex(rsrc_dtor_func_t ld, rsrc_dtor_func_t pld, char *type_name, int module_number);
ZEND_API int zend_list_insert(void *ptr, int type);
ZEND_API void *_zend_list_find(int id, int *type TSRMLS_DC);
ZEND_API int _zend_list_addref(int id TSRMLS_DC);
ZEND_API int _zend_list_delete(int id TSRMLS_DC);
ZEND_API char *zend_rsrc_list_get_rsrc_type(int resource TSRMLS_DC);
ZEND_API int zend_fetch_list_dtor_id(char *type_name);
ZEND_API int zend_register_resource(zval *rsrc_result, void *rsrc_pointer, int rsrc_type);
ZEND_API void *zend_fetch_resource(zval **passed_id TSRMLS_DC, int default_id, char *resource_type_name, int *found_resource_type, int num_resource_types, ...);

#ifdef __cplusplus
}
#endif


/* Persistent resources management */

/* 
	As far as I know, in PHP, EG(persistent_list) remains untouched as long as a thread exists.
	Then, executor_globals_dtor is called and persistent resources associated with the thread 
	are destroyed. Lists are never copied or merged.
	I'll try to introduce a better persistent resources handling.
*/

void plist_entry_destructor(void *ptr);

//
// PersistentListWrapper
//
// Wraps a hashtable containing persistent resources.
//
private ref class PersistentListWrapper
{
public:
	// Constructs a new wrapper around given ht. Attaches timestamp.
	PersistentListWrapper(const HashTable *ht)
	{
		persistent_resources = new HashTable(*ht);

#ifdef DEBUG
		Debug::WriteLine("PHP4TS", String::Format("constructing PersistentListWrapper with {0} resources", ht->nNumOfElements));
#endif

		lastUse = DateTime::Now;
	}

	// Destructs the wrapper.
	~PersistentListWrapper()
	{
		delete persistent_resources;
	}

	// Returns the timestamp.
	DateTime GetLastUseDateTime()
	{
		return lastUse;
	}

	// Returns the ht.
	void GetHashTable(HashTable *ht)
	{
		memcpy(ht, persistent_resources, sizeof(HashTable));
	}

	// Merges wrapper given as parameter with this wrapper (this wrapper is target).
	void Merge(PersistentListWrapper ^source)
	{
		if (source->persistent_resources->nNumOfElements > 0)
		{
			zend_rsrc_list_entry tmp;

			HashTable *source_ht = source->persistent_resources;
			HashTable *target_ht = this->persistent_resources;

			zend_hash_merge(target_ht, source_ht, NULL, &tmp, sizeof(zend_rsrc_list_entry), 1);
		}
	}

private:
	HashTable *persistent_resources;
	DateTime lastUse;
};

//
// PersistentResourcesManager
//
// Manages list (stack) of persistent resource lists.
//
private ref class PersistentResourcesManager
{
public:
	// Gets a persistent list - typically when a new request is started.
	static void GetPersistentList(HashTable *ht)
	{
		Monitor::Enter(lists);
		try
		{
			// is there a persistent list in our stack?
			int count = lists->Count;
			if (count > 0)
			{
				if (++getCounter > INSPECT_GET_COUNT)
				{
					if (count > 1)
					{
						// inspect the stack and merge lists that have not been used for a long time
						DateTime now = DateTime::Now;
						scg::Stack<PersistentListWrapper ^> ^stack = gcnew scg::Stack<PersistentListWrapper ^>(count);

						for (int i = 0; i < count; i++)
						{
							PersistentListWrapper ^item = lists->Pop();

							if ((now - item->GetLastUseDateTime()) > MAX_LIST_AGE && i > 0)
							{
								// this list is too old -> merge it with the preceding one
								stack->Peek()->Merge(item);
							}
							else stack->Push(item);
						}
						//lists = gcnew scg::Stack<PersistentListWrapper ^>(stack);

						// lists object must not be changed (it has pending lock!!)
						lists->Clear();
						scg::IEnumerator<PersistentListWrapper ^> ^enumerator = stack->GetEnumerator();
						while (enumerator->MoveNext()) lists->Push(enumerator->Current);
					}

					getCounter = 0;
				}

				lists->Pop()->GetHashTable(ht);
				return;
			}
		}
#ifdef DEBUG
		catch (Exception^ex)
		{
			Debug::WriteLine("ExtSupport.PHP4", String::Format("PersistentResourcesManager::GetPersistentList exception: {0}", ex->Message));
			throw;
		}
#endif
		finally
		{
			Monitor::Exit(lists);
		}

		// create a new empty persistent list
		zend_hash_init_ex(ht, 0, NULL, plist_entry_destructor, 1, 0);
	}
	
	// Returns a persistent list to the "cache" - typically when a request is finished.
	static void StorePersistentList(const HashTable *ht)
	{
		Monitor::Enter(lists);
		try
		{
			// push the persistent list to our stack
			lists->Push(gcnew PersistentListWrapper(ht));

#ifdef DEBUG
			Debug::WriteLine("PHP4TS", String::Format("PersistentResourcesManager::StorePersistentList: {0} lists on stack",
				lists->Count));
#endif
		}
		finally
		{
			Monitor::Exit(lists);
		}
	}

private:
	static scg::Stack<PersistentListWrapper ^> ^lists = gcnew scg::Stack<PersistentListWrapper ^>();
	static int getCounter = 0;

	static const int        INSPECT_GET_COUNT = 1000;
	static const TimeSpan   MAX_LIST_AGE = TimeSpan::FromMinutes(10);
};
