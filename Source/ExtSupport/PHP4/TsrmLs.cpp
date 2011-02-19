//
// ExtSupport.PHP4 - substitute for php4ts.dll
//
// TsrmLs.cpp
// - contains definitions of TLS related functions
//

#include "stdafx.h"
#include "TsrmLs.h"
#include "Request.h"
#include "IniConfig.h"
#include "Resources.h"
#include "Variables.h"
#include "Arrays.h"
#include "Constants.h"
#include "Objects.h"

#include <io.h>

using namespace PHP::ExtManager;

// IDs are exported so that extensions can use them.
ZEND_API int compiler_globals_id;
ZEND_API int executor_globals_id;
ZEND_API int alloc_globals_id;
ZEND_API int core_globals_id;
ZEND_API ts_rsrc_id ini_scanner_globals_id;
ZEND_API ts_rsrc_id language_scanner_globals_id;
ZEND_API int sapi_globals_id;
ZEND_API int file_globals_id;
ZEND_API int output_globals_id;
ZEND_API int php_win32_core_globals_id;

ZEND_API int com_globals_id = -1;
ZEND_API int ps_globals_id = -1;

int lcg_globals_id;

int le_stream_context = FAILURE;

// copied from file.c
ZEND_API int php_le_stream_context(void)
{
	return le_stream_context;
}

// copied from zend.c
#ifdef PHP4TS
// (called as part of new request initialization, i.e. when the new request makes
// TSRMLS_FETCH() for the first time)
void executor_globals_ctor(zend_executor_globals *executor_globals TSRMLS_DC)
{
#ifdef DEBUG
	Debug::WriteLine("PHP4TS", "executor_globals_ctor invoked.");
#endif

	memset(executor_globals, 0, sizeof(*executor_globals));

	// get a list of persistent resources
	PersistentResourcesManager::GetPersistentList(&executor_globals->persistent_list);

	executor_globals->active_symbol_table = (HashTable *)emalloc(sizeof(HashTable));
	zend_hash_init_ex(executor_globals->active_symbol_table, 0, NULL, ZVAL_PTR_DTOR, 1, 0);

	executor_globals->function_table = (HashTable *)emalloc(sizeof(HashTable));
	zend_hash_init_ex(executor_globals->function_table, 0, NULL, ZVAL_PTR_DTOR, 1, 0);

	executor_globals->class_table = (HashTable *)emalloc(sizeof(HashTable));
	zend_hash_init_ex(executor_globals->class_table, 0, NULL, ZVAL_PTR_DTOR, 1, 0);

	executor_globals->zend_constants = (HashTable *)emalloc(sizeof(HashTable));
	zend_hash_init_ex(executor_globals->zend_constants, 0, NULL, ZVAL_PTR_DTOR, 1, 0);
}

// copied from zend.c
// (called as part of request finalization)
void executor_globals_dtor(zend_executor_globals *executor_globals TSRMLS_DC)
{
#ifdef DEBUG
	Debug::WriteLine("PHP4TS", "executor_globals_dtor invoked.");
#endif

	// return the list of persistent resources to the "cache" for reuse
	PersistentResourcesManager::StorePersistentList(&EG(persistent_list));

	zend_hash_destroy(executor_globals->active_symbol_table);
	efree(executor_globals->active_symbol_table);

	zend_hash_destroy(executor_globals->function_table);
	efree(executor_globals->function_table);

	zend_hash_destroy(executor_globals->class_table);
	efree(executor_globals->class_table);

	zend_hash_destroy(executor_globals->zend_constants);
	efree(executor_globals->zend_constants);

	zend_ini_shutdown(TSRMLS_C);
}
#else
void executor_globals_ctor(zend_executor_globals *executor_globals TSRMLS_DC)
{
	zend_startup_constants(TSRMLS_C);

	//Phalanger TODO: What is the GLOBAL_CONSTANTS_TABLE equivalent in Phalanger?
//	zend_copy_constants(EG(zend_constants), GLOBAL_CONSTANTS_TABLE);
//	zend_init_rsrc_plist(TSRMLS_C); //Replaced by PersistentResourcesManager::GetPersistentList
	// get a list of persistent resources
	PersistentResourcesManager::GetPersistentList(&executor_globals->persistent_list);

	EG(lambda_count)=0;
	EG(user_error_handler) = NULL;
	EG(user_exception_handler) = NULL;
	EG(in_execution) = 0;
	EG(in_autoload) = NULL;
	EG(current_execute_data) = NULL;
	EG(current_module) = NULL;
	EG(exit_status) = 0;
	EG(active) = 0;

	//added for Phalanger
	zend_objects_store_init(&executor_globals->objects_store, 1024);
}


void executor_globals_dtor(zend_executor_globals *executor_globals TSRMLS_DC)
{
	// return the list of persistent resources to the "cache" for reuse
	PersistentResourcesManager::StorePersistentList(&EG(persistent_list));

	zend_ini_shutdown(TSRMLS_C);
//	Phalanger TODO: Not sure if we need the code below, seems to be taken care of by PersistentResourcesManager 
//	if (&executor_globals->persistent_list != global_persistent_list) {
//		zend_destroy_rsrc_list(&executor_globals->persistent_list TSRMLS_CC);
//	}

//	Phalanger TODO: We likely won't need to check if it's = GLOBAL_CONSTANTS_TABLE, as Phalanger will copy the global constants instead of setting the pointer =
//	if (executor_globals->zend_constants != GLOBAL_CONSTANTS_TABLE) {
		zend_hash_destroy(executor_globals->zend_constants);
		free(executor_globals->zend_constants);
//	}
}
#endif

// copied from zend.c
// (called as part of new request initialization, i.e. when the new request makes
// TSRMLS_FETCH() for the first time)
void zend_new_thread_end_handler(THREAD_T thread_id TSRMLS_DC)
{
#ifdef DEBUG
	Debug::WriteLine("PHP4TS", "zend_new_thread_end_handler invoked.");
#endif

	zend_copy_ini_directives(TSRMLS_C);
	zend_ini_refresh_caches(ZEND_INI_STAGE_STARTUP TSRMLS_CC);
}

// copied from main.c
// (called as part of new request initialization, i.e. when the new request makes
// TSRMLS_FETCH() for the first time)
void core_globals_ctor(php_core_globals *core_globals TSRMLS_DC)
{
	memset(core_globals, 0, sizeof(*core_globals));

	core_globals->y2k_compliance = 1;

	core_globals->html_errors = true; //global->ErrorControl.HtmlMessages;

	core_globals->magic_quotes_gpc = false; //global->GlobalVariables->QuoteGpcVariables;
	core_globals->magic_quotes_runtime = false; //global->GlobalVariables.QuoteRuntimeVariables;
	core_globals->magic_quotes_sybase = false; //global->GlobalVariables.QuoteInDbManner;
	
	//Debug::WriteLine("PHP4TS", "XXXXXXXXXX:");
	//Debug::WriteLine("PHP4TS", __box((char *)&core_globals->http_globals[TRACK_VARS_ENV] - (char *)core_globals)->ToString());
	// Note: had to make core_globals #pragma pack(2) to keep the layout (or at the offset of least http_globals)
	// in sync with Zend

	// create the argv array
	zval *arr;
	ALLOC_ZVAL(arr);
	array_init(arr);
	arr->is_ref = 0;
	arr->refcount = 0;

	PhpMarshaler ^marshaler = PhpMarshaler::GetInstance(nullptr);
	array<String ^> ^args = Environment::GetCommandLineArgs();
	for (int i = 0; i < args->Length; i++)
	{
		zval *tmp = (zval *)marshaler->MarshalManagedToNative(args[i]).ToPointer();
		if (zend_hash_next_index_insert(Z_ARRVAL_P(arr), &tmp, sizeof(zval *), NULL) == FAILURE)
		{
			marshaler->CleanUpNativeData(IntPtr(tmp));
		}
	}

	// create the argc integer
	zval *argc;
	ALLOC_ZVAL(argc);
	Z_LVAL_P(argc) = args->Length;
	Z_TYPE_P(argc) = IS_LONG;
	argc->is_ref = 0;
	argc->refcount = 0;

	MAKE_STD_ZVAL(core_globals->http_globals[TRACK_VARS_ENV]);
	array_init(core_globals->http_globals[TRACK_VARS_ENV]);

	// update PG(http_globals)
	arr->refcount++;
	argc->refcount++;
	zend_hash_update(Z_ARRVAL_P(core_globals->http_globals[TRACK_VARS_ENV]), "argv", sizeof("argv"), &arr, sizeof(zval *), NULL);
	zend_hash_update(Z_ARRVAL_P(core_globals->http_globals[TRACK_VARS_ENV]), "argc", sizeof("argc"), &argc, sizeof(zval *), NULL);
}

void core_globals_dtor(php_core_globals *core_globals TSRMLS_DC)
{
	zval_ptr_dtor(&core_globals->http_globals[TRACK_VARS_ENV]);
}

// copied from file.c
// (called as part of new request initialization, i.e. when the new request makes
// TSRMLS_FETCH() for the first time)
void file_globals_ctor(php_file_globals *file_globals_p TSRMLS_DC)
{
	file_globals_p->pclose_ret = 0;
	file_globals_p->user_stream_current_filename = NULL;
	file_globals_p->def_chunk_size = /*PHP_SOCK_CHUNK_SIZE*/8192;

	file_globals_p->default_socket_timeout = 60; //global->FileSystem.DefaultSocketTimeout;
	file_globals_p->user_agent = "Phalanger 1.0"; //PhpMarshaler::MarshalManagedStringToNativeString(global->FileSystem.UserAgent);
}

// copied from file.c
// (called as part of request finalization)
void file_globals_dtor(php_file_globals *file_globals_p TSRMLS_DC)
{
}

void sapi_globals_ctor(sapi_globals_struct *sapi_globals TSRMLS_DC)
{
	memset(sapi_globals, 0, sizeof(sapi_globals_struct));
}

void compiler_globals_ctor(zend_compiler_globals *compiler_globals TSRMLS_DC)
{
	memset(compiler_globals, 0, sizeof(zend_compiler_globals));
}

void scanner_globals_ctor(zend_scanner_globals *scanner_globals TSRMLS_DC)
{
	memset(scanner_globals, 0, sizeof(zend_scanner_globals));
}

void alloc_globals_ctor(zend_alloc_globals *alloc_globals TSRMLS_DC)
{
	alloc_globals->MemBlocks = NULL;
	alloc_globals->ZvalCache = NULL;
}

void alloc_globals_dtor(zend_alloc_globals *alloc_globals TSRMLS_DC)
{
	// free memory blocks that are still allocated
	while (alloc_globals->MemBlocks)
	{
		MEMORY_BLOCK_HEADER *block = alloc_globals->MemBlocks;
		alloc_globals->MemBlocks = alloc_globals->MemBlocks->next;

#ifdef DEBUG
		Debug::WriteLine("PHP4TS", "Freeing a leaked memory block!");
#endif
		free(block);
	}

	// free "zval lookaside buffer"
	while (alloc_globals->ZvalCache)
	{
		MEMORY_BLOCK_HEADER *block = alloc_globals->ZvalCache;
		alloc_globals->ZvalCache = alloc_globals->ZvalCache->next;

		free(block);
	}
}

void php_win32_core_globals_ctor(php_win32_core_globals *win32_globals TSRMLS_DC)
{
	memset(win32_globals, 0, sizeof(win32_globals));
}

// copied from file.c
ZEND_RSRC_DTOR_FUNC(file_context_dtor)
{
	php_stream_context *context = (php_stream_context*)rsrc->ptr;
	if (context->options)
	{
		zval_ptr_dtor(&context->options);
		context->options = NULL;
	}
	php_stream_context_free(context);
}

// copied from lcg.c
void lcg_init_globals(php_lcg_globals *lcg_globals_p TSRMLS_DC)
{
	lcg_globals_p->seeded = 0;
}

// copied from output.c
static inline int php_default_output_func(const char *str, uint str_len TSRMLS_DC)
{
	crtx_fwrite(str, 1, str_len, stderr);
	return str_len;
}

// copied from output.c
void php_output_init_globals(php_output_globals *output_globals_p TSRMLS_DC)
{
 	output_globals_p->php_body_write = php_default_output_func;
	output_globals_p->php_header_write = php_default_output_func;
	output_globals_p->implicit_flush = 0;
	output_globals_p->output_start_filename = NULL;
	output_globals_p->output_start_lineno = 0;
}

// copied from TSRM.c and beautified
#define TSRM_ERROR(args)
#define TSRM_SAFE_RETURN_RSRC(array, offset, range)		\
	if (offset == 0) return &array;						\
	else return array[TSRM_UNSHUFFLE_RSRC_ID(offset)];


/* New thread handlers */
static tsrm_thread_begin_func_t tsrm_new_thread_begin_handler;
static tsrm_thread_end_func_t tsrm_new_thread_end_handler;

/* The memory manager table */
static tsrm_tls_entry	**tsrm_tls_table = NULL;
static int				tsrm_tls_table_size;
static ts_rsrc_id		id_count;

/* The resource sizes table */
static tsrm_resource_type	*resource_types_table = NULL;
static int					resource_types_table_size;

static MUTEX_T tsmm_mutex;	/* thread-safe memory manager mutex */

static DWORD tls_key;

// copied from TSRM.c, modified and beautified
/* Startup TSRM (call once for the entire process) */
ZEND_API int tsrm_startup(int expected_threads, int expected_resources, int debug_level, char *debug_filename)
{
	tls_key = TlsAlloc();

	tsrm_error_set(debug_level, debug_filename);
	tsrm_tls_table_size = expected_threads;

	tsrm_tls_table = (tsrm_tls_entry **)calloc(tsrm_tls_table_size, sizeof(tsrm_tls_entry *));

	if (!tsrm_tls_table)
	{
		TSRM_ERROR((TSRM_ERROR_LEVEL_ERROR, "Unable to allocate TLS table"));
		return 0;
	}

	id_count = 0;

	resource_types_table_size = expected_resources;
	resource_types_table = (tsrm_resource_type *)calloc(resource_types_table_size, sizeof(tsrm_resource_type));

	if (!resource_types_table)
	{
		TSRM_ERROR((TSRM_ERROR_LEVEL_ERROR, "Unable to allocate resource types table"));
		free(tsrm_tls_table);
		tsrm_tls_table = NULL;
		return 0;
	}

	tsmm_mutex = tsrm_mutex_alloc();

	tsrm_new_thread_begin_handler = tsrm_new_thread_end_handler = NULL;

	TSRM_ERROR((TSRM_ERROR_LEVEL_CORE, "Started up TSRM, %d expected threads, %d expected resources", 
		expected_threads, expected_resources));
	return 1;
}

// copied from TSRM.c, modified and beautified
/* Shutdown TSRM (call once for the entire process) */
ZEND_API void tsrm_shutdown(void)
{
	int i;

	if (tsrm_tls_table)
	{
		for (i = 0; i < tsrm_tls_table_size; i++)
		{
			tsrm_tls_entry *p = tsrm_tls_table[i], *next_p;

			while (p)
			{
				int j;

				next_p = p->next;
				for (j = 0; j < id_count; j++) free(p->storage[j]);
				free(p->storage);
				free(p);
				p = next_p;
			}
		}
		free(tsrm_tls_table);
		tsrm_tls_table = NULL;
	}

	if (resource_types_table)
	{
		free(resource_types_table);
		resource_types_table=NULL;
	}
	tsrm_mutex_free(tsmm_mutex);
	tsmm_mutex = NULL;
	TSRM_ERROR((TSRM_ERROR_LEVEL_CORE, "Shutdown TSRM"));
}

// copied from TSRM.c, modified and beautified
/* allocates a new thread-safe-resource id */
ZEND_API ts_rsrc_id ts_allocate_id(ts_rsrc_id *rsrc_id, size_t size, ts_allocate_ctor ctor, ts_allocate_dtor dtor)
{
	int i;

#ifdef DEBUG
	Debug::WriteLine("PHP4TS", String::Format("Obtaining a new resource id, {0} bytes", size));
#endif
	TSRM_ERROR((TSRM_ERROR_LEVEL_CORE, "Obtaining a new resource id, %d bytes", size));

	tsrm_mutex_lock(tsmm_mutex);

	/* obtain a resource id */
	*rsrc_id = TSRM_SHUFFLE_RSRC_ID(id_count++);
	TSRM_ERROR((TSRM_ERROR_LEVEL_CORE, "Obtained resource id %d", *rsrc_id));

	/* store the new resource type in the resource sizes table */
	if (resource_types_table_size < id_count)
	{
		resource_types_table = (tsrm_resource_type *)realloc(resource_types_table, sizeof(tsrm_resource_type) * id_count);
		if (!resource_types_table)
		{
			tsrm_mutex_unlock(tsmm_mutex);
			TSRM_ERROR((TSRM_ERROR_LEVEL_ERROR, "Unable to allocate storage for resource"));
			*rsrc_id = 0;
			return 0;
		}
		resource_types_table_size = id_count;
	}
	resource_types_table[TSRM_UNSHUFFLE_RSRC_ID(*rsrc_id)].size = size;
	resource_types_table[TSRM_UNSHUFFLE_RSRC_ID(*rsrc_id)].ctor = ctor;
	resource_types_table[TSRM_UNSHUFFLE_RSRC_ID(*rsrc_id)].dtor = dtor;

	/* enlarge the arrays for the already active threads */
	for (i = 0; i < tsrm_tls_table_size; i++)
	{
		tsrm_tls_entry *p = tsrm_tls_table[i];

		while (p)
		{
			if (p->count < id_count)
			{
				int j;

				p->storage = (void **)realloc(p->storage, sizeof(void *) * id_count);
				for (j = p->count; j < id_count; j++)
				{
					p->storage[j] = (void *)malloc(resource_types_table[j].size);
					if (resource_types_table[j].ctor)
					{
						resource_types_table[j].ctor(p->storage[j], &p->storage);
					}
				}
				p->count = id_count;
			}
			p = p->next;
		}
	}
	tsrm_mutex_unlock(tsmm_mutex);

	TSRM_ERROR((TSRM_ERROR_LEVEL_CORE, "Successfully allocated new resource id %d", *rsrc_id));
	return *rsrc_id;
}

// copied from TSRM.c, modified and beautified
static void allocate_new_resource(tsrm_tls_entry **thread_resources_ptr, THREAD_T thread_id)
{
	int i;

	TSRM_ERROR((TSRM_ERROR_LEVEL_CORE, "Creating data structures for thread %x", thread_id));
	(*thread_resources_ptr) = (tsrm_tls_entry *)malloc(sizeof(tsrm_tls_entry));
	(*thread_resources_ptr)->storage = (void **)malloc(sizeof(void *) * id_count);
	(*thread_resources_ptr)->count = id_count;
	(*thread_resources_ptr)->thread_id = thread_id;
	(*thread_resources_ptr)->next = NULL;

	Request::SetThreadStorage(*thread_resources_ptr); 
	TlsSetValue(tls_key, (void *)*thread_resources_ptr);

	if (tsrm_new_thread_begin_handler) tsrm_new_thread_begin_handler(thread_id, &((*thread_resources_ptr)->storage));

	for (i = 0; i < id_count; i++)
	{
		(*thread_resources_ptr)->storage[i] = (void *)malloc(resource_types_table[i].size);
		if (resource_types_table[i].ctor)
		{
			resource_types_table[i].ctor((*thread_resources_ptr)->storage[i], &(*thread_resources_ptr)->storage);
		}
	}

	tsrm_mutex_unlock(tsmm_mutex);

	if (tsrm_new_thread_end_handler) tsrm_new_thread_end_handler(thread_id, &((*thread_resources_ptr)->storage));
}

// copied from TSRM.c, modified and beautified
/* fetches the requested resource for the current thread */
ZEND_API void *ts_resource_ex(ts_rsrc_id id, THREAD_T *th_id)
{
	THREAD_T thread_id;
	int hash_value;
	tsrm_tls_entry *thread_resources;

	if (!th_id)
	{
		//thread_resources = Request::GetThreadStorage();
		thread_resources = (tsrm_tls_entry *)TlsGetValue(tls_key);
		if (thread_resources)
		{
#ifdef DEBUG
			Debug::WriteLine("PHP4TS", String::Format("Fetching resource id {0} for current thread", id));
#endif
			TSRM_ERROR((TSRM_ERROR_LEVEL_INFO, "Fetching resource id %d for current thread %d", id, 
				(long)thread_resources->thread_id));
			/* Read a specific resource from the thread's resources.
			 * This is called outside of a mutex, so have to be aware about external
			 * changes to the structure as we read it.
			 */
			TSRM_SAFE_RETURN_RSRC(thread_resources->storage, id, thread_resources->count);
		}
		thread_id = tsrm_thread_id();
	} 
	else thread_id = *th_id;

#ifdef DEBUG
	Debug::WriteLine("PHP4TS", String::Format("Fetching resource id {0} for thread {1}", id, (long)thread_id));
#endif
	TSRM_ERROR((TSRM_ERROR_LEVEL_INFO, "Fetching resource id %d for thread %ld", id, (long)thread_id));
	tsrm_mutex_lock(tsmm_mutex);

	hash_value = THREAD_HASH_OF(thread_id, tsrm_tls_table_size);
	thread_resources = tsrm_tls_table[hash_value];

	if (!thread_resources)
	{
		allocate_new_resource(&tsrm_tls_table[hash_value], thread_id);
		return ts_resource_ex(id, &thread_id);
	}
	else
	{
		 do
		 {
			if (thread_resources->thread_id == thread_id) break;
			if (thread_resources->next) thread_resources = thread_resources->next;
			else
			{
				allocate_new_resource(&thread_resources->next, thread_id);
				return ts_resource_ex(id, &thread_id);
				/*
				 * thread_resources = thread_resources->next;
				 * break;
				 */
			}
		 } while (thread_resources);
	}
	tsrm_mutex_unlock(tsmm_mutex);
	/* Read a specific resource from the thread's resources.
	 * This is called outside of a mutex, so have to be aware about external
	 * changes to the structure as we read it.
	 */
	TSRM_SAFE_RETURN_RSRC(thread_resources->storage, id, thread_resources->count);
}

// copied from TSRM.c, modified and beautified
/* frees all resources allocated for the current thread */
ZEND_API void ts_free_thread(void)
{
	tsrm_tls_entry *thread_resources;
	int i;
	THREAD_T thread_id = tsrm_thread_id();
	int hash_value;
	tsrm_tls_entry *last = NULL;

	tsrm_mutex_lock(tsmm_mutex);
	hash_value = THREAD_HASH_OF(thread_id, tsrm_tls_table_size);
	thread_resources = tsrm_tls_table[hash_value];

	while (thread_resources)
	{
		if (thread_resources->thread_id == thread_id)
		{
			// call the dtors in reverse order!
			for (i = thread_resources->count - 1; i >= 0; i--)
			{
				if (resource_types_table[i].dtor)
				{
					resource_types_table[i].dtor(thread_resources->storage[i], &thread_resources->storage);
				}
			}
			for (i = 0; i < thread_resources->count; i++) free(thread_resources->storage[i]);

			free(thread_resources->storage);
			if (last) last->next = thread_resources->next;
			else tsrm_tls_table[hash_value] = thread_resources->next;

			free(thread_resources);
			break;
		}
		if (thread_resources->next) last = thread_resources;
		thread_resources = thread_resources->next;
	}

	Request::SetThreadStorage(NULL); 
	TlsSetValue(tls_key, 0);

	tsrm_mutex_unlock(tsmm_mutex);
}

// copied from TSRM.c
/* deallocates all occurrences of a given id */
ZEND_API void ts_free_id(ts_rsrc_id id)
{
	// a joke?
}

/*
 * Utility Functions
 */

// copied from TSRM.c, modified and beautified
/* Obtain the current thread id */
ZEND_API THREAD_T tsrm_thread_id(void)
{
	return Request::GetLogicalThreadId();
}

// copied from TSRM.c, modified and beautified
/* Allocate a mutex */
ZEND_API MUTEX_T tsrm_mutex_alloc(void)
{
	MUTEX_T mutexp;

	mutexp = (CRITICAL_SECTION *)malloc(sizeof(CRITICAL_SECTION));
	InitializeCriticalSection(mutexp);

	return mutexp;
}

// copied from TSRM.c, modified and beautified
/* Free a mutex */
ZEND_API void tsrm_mutex_free(MUTEX_T mutexp)
{
	if (mutexp) DeleteCriticalSection(mutexp);
}

// copied from TSRM.c, modified and beautified
/* Lock a mutex */
ZEND_API int tsrm_mutex_lock(MUTEX_T mutexp)
{
	TSRM_ERROR((TSRM_ERROR_LEVEL_INFO, "Mutex locked thread: %ld", tsrm_thread_id()));

	EnterCriticalSection(mutexp);
	return 1;
}

// copied from TSRM.c, modified and beautified
/* Unlock a mutex */
ZEND_API int tsrm_mutex_unlock(MUTEX_T mutexp)
{
	TSRM_ERROR((TSRM_ERROR_LEVEL_INFO, "Mutex unlocked thread: %ld", tsrm_thread_id()));

	LeaveCriticalSection(mutexp);
	return 1;
}

// copied from TSRM.c
ZEND_API void *tsrm_set_new_thread_begin_handler(tsrm_thread_begin_func_t new_thread_begin_handler)
{
	void *retval = (void *)tsrm_new_thread_begin_handler;

	tsrm_new_thread_begin_handler = new_thread_begin_handler;
	return retval;
}

// copied from TSRM.c
ZEND_API void *tsrm_set_new_thread_end_handler(tsrm_thread_end_func_t new_thread_end_handler)
{
	void *retval = (void *)tsrm_new_thread_end_handler;

	tsrm_new_thread_end_handler = new_thread_end_handler;
	return retval;
}

// copied from tsrm_win32.c and beautified
ZEND_API int tsrm_win32_access(const char *pathname, int mode)
{
	SHFILEINFOA sfi;

	if (mode == 1 /*X_OK*/)
	{
		return _access(pathname, 0) == 0 && 
			SHGetFileInfoA(pathname, 0, &sfi, sizeof(SHFILEINFOA), SHGFI_EXETYPE) != 0 ? 0 : -1;
	}
	else return _access(pathname, mode);
}

/*
 * Debug support
 */

// intentionally empty
ZEND_API void tsrm_error_set(int level, char *debug_filename)
{
}

void tsrm_update_native_tls(void *value)
{
	TlsSetValue(tls_key, value);
}
