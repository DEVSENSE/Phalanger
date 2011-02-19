//
// ExtSupport.PHP4 - substitute for php4ts.dll
//
// TsrmLs.h
// - contains declarations of TLS related macros and exported functions
//

#pragma once

#include "stdafx.h"
#include "ExtSupport.h"
#include "LinkedLists.h"
#include "Stacks.h"
#include "Streams.h"
#include "Resources.h"
#include "Functions.h"
#include "Objects.h"

#include <setjmp.h>

#define tsrm_do_alloca(p) alloca(p)
#define tsrm_free_alloca(p)

#define TSRM_SHUFFLE_RSRC_ID(rsrc_id)		((rsrc_id) + 1)
#define TSRM_UNSHUFFLE_RSRC_ID(rsrc_id)		((rsrc_id) - 1)

// TSRMLS_FETCH, TSRMLS_D, TSRMLS_DC, TSRMLS_C, TSRMLS_CC are defined in Zend.h
#define TSRMG(id, type, element)	(((type) (*((void ***) tsrm_ls))[TSRM_UNSHUFFLE_RSRC_ID(id)])->element)

#define CG(v)	TSRMG(compiler_globals_id, zend_compiler_globals *, v)
#define EG(v)	TSRMG(executor_globals_id, zend_executor_globals *, v)
#define AG(v)	TSRMG(alloc_globals_id, zend_alloc_globals *, v)
#define PG(v)	TSRMG(core_globals_id, php_core_globals *, v)
#define FG(v)	TSRMG(file_globals_id, php_file_globals *, v)
#define SG(v)	TSRMG(sapi_globals_id, sapi_globals_struct *, v)
#define OG(v)	TSRMG(output_globals_id, php_output_globals *, v)
#define PS(v)	TSRMG(ps_globals_id, php_ps_globals *, v)
#define LCG(v)	TSRMG(lcg_globals_id, php_lcg_globals *, v)
#define COMG(v)	TSRMG(com_globals_id, zend_com_globals *, v)

#define LANG_SCNG(v)	TSRMG(language_scanner_globals_id, zend_scanner_globals *, v)
#define INI_SCNG(v)		TSRMG(ini_scanner_globals_id, zend_scanner_globals *, v)


#define SYMTABLE_CACHE_SIZE 32
#define ZEND_MAX_RESERVED_RESOURCES	4

#define ts_resource(id)			ts_resource_ex(id, NULL)

#define THREAD_T				DWORD
#define MUTEX_T					CRITICAL_SECTION *
#define THREAD_HASH_OF(thr,ts)  (unsigned long)thr%(unsigned long)ts

/* Debug support */
#define TSRM_ERROR_LEVEL_ERROR	1
#define TSRM_ERROR_LEVEL_CORE	2
#define TSRM_ERROR_LEVEL_INFO	3

typedef int ts_rsrc_id;

typedef void (*ts_allocate_ctor)(void *, void ***);
typedef void (*ts_allocate_dtor)(void *, void ***);

typedef void (*tsrm_thread_begin_func_t)(THREAD_T thread_id, void ***tsrm_ls);
typedef void (*tsrm_thread_end_func_t)(THREAD_T thread_id, void ***tsrm_ls);

// There is one instance of the following structure per thread. Fields are usually accessed using
// the EG macro.

#ifdef PHP4TS
struct _zend_executor_globals
{
	zval **return_value_ptr_ptr;

	zval uninitialized_zval;
	zval *uninitialized_zval_ptr;

	zval error_zval;
	zval *error_zval_ptr;

	zend_function_state *function_state_ptr;
	zend_ptr_stack arg_types_stack;

	/* symbol table cache */
	HashTable *symtable_cache[SYMTABLE_CACHE_SIZE];
	HashTable **symtable_cache_limit;
	HashTable **symtable_cache_ptr;

	/*zend_op*/void **opline_ptr;

	/*zend_execute_data*/void *current_execute_data;
    
	HashTable *active_symbol_table;
	HashTable symbol_table;		/* main symbol table */

	HashTable included_files;	/* files already included */

	jmp_buf bailout;

	int error_reporting;
	int orig_error_reporting;
	int exit_status;

	zend_op_array *active_op_array;

	HashTable *function_table;	/* function symbol table */
	HashTable *class_table;		/* class table */
	HashTable *zend_constants;	/* constants table */

	long precision;

	int ticks_count;

	zend_bool in_execution;
	zend_bool bailout_set;
	zend_bool full_tables_cleanup;

	/* for extended information support */
	zend_bool no_extensions;

#ifdef ZEND_WIN32
	zend_bool timed_out;
#endif

	HashTable regular_list;
	HashTable persistent_list;

	zend_ptr_stack argument_stack;
	int free_op1, free_op2;
	int (*unary_op)(zval *result, zval *op1);
	int (*binary_op)(zval *result, zval *op1, zval *op2 TSRMLS_DC);

	zval *garbage[2];
	int garbage_ptr;

	zval *user_error_handler;
	zend_ptr_stack user_error_handlers;

	/* timeout support */
	int timeout_seconds;

	int lambda_count;

	HashTable *ini_directives;

	/* locale stuff */
	char float_separator[1];

	void *reserved[ZEND_MAX_RESERVED_RESOURCES];
};
#else
struct _zend_executor_globals {
	zval **return_value_ptr_ptr;

	zval uninitialized_zval;
	zval *uninitialized_zval_ptr;

	zval error_zval;
	zval *error_zval_ptr;

	zend_function_state *function_state_ptr;
	zend_ptr_stack arg_types_stack;

	/* symbol table cache */
	HashTable *symtable_cache[SYMTABLE_CACHE_SIZE];
	HashTable **symtable_cache_limit;
	HashTable **symtable_cache_ptr;

	struct zend_op **opline_ptr;

	HashTable *active_symbol_table;
	HashTable symbol_table;		/* main symbol table */

	HashTable included_files;	/* files already included */

	jmp_buf *bailout;

	int error_reporting;
	int orig_error_reporting;
	int exit_status;

	zend_op_array *active_op_array;

	HashTable *function_table;	/* function symbol table */
	HashTable *class_table;		/* class table */
	HashTable *zend_constants;	/* constants table */

	zend_class_entry *scope;

	zval *This;

	long precision;

	int ticks_count;

	zend_bool in_execution;
	HashTable *in_autoload;
	zend_function *autoload_func;
	zend_bool full_tables_cleanup;
	zend_bool ze1_compatibility_mode;

	/* for extended information support */
	zend_bool no_extensions;

#ifdef ZEND_WIN32
	zend_bool timed_out;
#endif

	HashTable regular_list;
	HashTable persistent_list;

	zend_ptr_stack argument_stack;

	int user_error_handler_error_reporting;
	zval *user_error_handler;
	zval *user_exception_handler;
	zend_stack user_error_handlers_error_reporting;
	zend_ptr_stack user_error_handlers;
	zend_ptr_stack user_exception_handlers;

	/* timeout support */
	int timeout_seconds;

	int lambda_count;

	HashTable *ini_directives;
	HashTable *modified_ini_directives;

	zend_objects_store objects_store;
	zval *exception;
	struct zend_op *opline_before_exception;

	struct _zend_execute_data *current_execute_data;

	struct _zend_module_entry *current_module;

	zend_property_info std_property_info;

	zend_bool active; 

	void *reserved[ZEND_MAX_RESERVED_RESOURCES];
};
#endif

typedef struct _zend_executor_globals zend_executor_globals;

typedef struct _arg_separators
{
	char *output;
	char *input;
} arg_separators;

typedef enum
{
	EH_NORMAL = 0,
	EH_SUPPRESS,
	EH_THROW
} error_handling_t;

#pragma pack (push)
#pragma pack (2)

struct _php_core_globals {
	zend_bool magic_quotes_gpc;
	zend_bool magic_quotes_runtime;
	zend_bool magic_quotes_sybase;

	zend_bool safe_mode;

	zend_bool allow_call_time_pass_reference;
	zend_bool implicit_flush;

	long output_buffering;

	char *safe_mode_include_dir;
	zend_bool safe_mode_gid;
	zend_bool sql_safe_mode;
	zend_bool enable_dl;

	char *output_handler;

	char *unserialize_callback_func;

	char *safe_mode_exec_dir;

	long memory_limit;
	long max_input_time;

	zend_bool track_errors;
	zend_bool display_errors;
	zend_bool display_startup_errors;
	zend_bool log_errors;
	long      log_errors_max_len;
	zend_bool ignore_repeated_errors;
	zend_bool ignore_repeated_source;
	zend_bool report_memleaks;
	char *error_log;

	char *doc_root;
	char *user_dir;
	char *include_path;
	char *open_basedir;
	char *extension_dir;

	char *upload_tmp_dir;
	long upload_max_filesize;
	
	char *error_append_string;
	char *error_prepend_string;

	char *auto_prepend_file;
	char *auto_append_file;

	arg_separators arg_separator;

	char *gpc_order;
	char *variables_order;

	HashTable rfc1867_protected_variables;

	short connection_status;
	short ignore_user_abort;

	unsigned char header_is_being_sent;

	zend_llist tick_functions;

	zval *http_globals[6];

	zend_bool expose_php;

	zend_bool register_globals;
	zend_bool register_argc_argv;

	zend_bool y2k_compliance;

	char *docref_root;
	char *docref_ext;

	zend_bool html_errors;
	zend_bool xmlrpc_errors;

	long xmlrpc_error_number;

	zend_bool modules_activated;
	zend_bool file_uploads;
	zend_bool during_request_startup;
	zend_bool allow_url_fopen;
	zend_bool always_populate_raw_post_data;
	zend_bool report_zend_debug;

	char *last_error_message;
	char *last_error_file;
	int  last_error_lineno;
	error_handling_t  error_handling;
	zend_class_entry *exception_class;
};

#pragma pack (pop)

typedef struct _php_core_globals php_core_globals;

typedef struct
{
  	int pclose_ret;
	size_t def_chunk_size;
	long auto_detect_line_endings;
	long default_socket_timeout;
	char *user_agent;
	char *user_stream_current_filename; /* for simple recursion protection */
	php_stream_context *default_context;
}
php_file_globals;

typedef struct
{
	php_int32 s1;
	php_int32 s2;
	int seeded;
}
php_lcg_globals;

typedef struct _php_ob_buffer
{
	char *buffer;
	uint size;
	uint text_length;
	int block_size;
	uint chunk_size;
	int status;
	zval *output_handler;
	/*php_output_handler_func_t*/void *internal_output_handler;
	char *internal_output_handler_buffer;
	uint internal_output_handler_buffer_size;
	char *handler_name;
	zend_bool erase;
}
php_ob_buffer;

typedef struct _php_output_globals
{
	int (*php_body_write)(const char *str, uint str_length TSRMLS_DC);		/* string output */
	int (*php_header_write)(const char *str, uint str_length TSRMLS_DC);	/* unbuffer string output */
	php_ob_buffer active_ob_buffer;
	unsigned char implicit_flush;
	char *output_start_filename;
	int output_start_lineno;
	zend_stack ob_buffers;
	int ob_nesting_level;
	zend_bool ob_lock;
	zend_bool disable_output;
}
php_output_globals;

typedef struct _zend_compiler_globals
{
	char _pad[512];
}
zend_compiler_globals;

typedef struct _zend_scanner_globals
{
	char _pad[128];
}
zend_scanner_globals;

typedef struct _sapi_globals_struct
{
	char _pad[512];
}
sapi_globals_struct;

// modified:
typedef struct _zend_alloc_globals
{
	MEMORY_BLOCK_HEADER *MemBlocks;
	MEMORY_BLOCK_HEADER *ZvalCache;
}
zend_alloc_globals;

typedef struct _php_win32_core_globals {
	char _pad[96];
}
php_win32_core_globals;

typedef struct _tsrm_tls_entry tsrm_tls_entry;
struct _tsrm_tls_entry
{
	void **storage;
	int count;
	THREAD_T thread_id;
	tsrm_tls_entry *next;
};

typedef struct
{
	size_t size;
	ts_allocate_ctor ctor;
	ts_allocate_dtor dtor;
} tsrm_resource_type;


void executor_globals_ctor(zend_executor_globals *executor_globals TSRMLS_DC);
void executor_globals_dtor(zend_executor_globals *executor_globals TSRMLS_DC);
void zend_new_thread_end_handler(THREAD_T thread_id TSRMLS_DC);
void core_globals_ctor(php_core_globals *core_globals TSRMLS_DC);
void core_globals_dtor(php_core_globals *core_globals TSRMLS_DC);
void sapi_globals_ctor(sapi_globals_struct *sapi_globals TSRMLS_DC);
void file_globals_ctor(php_file_globals *file_globals_p TSRMLS_DC);
void file_globals_dtor(php_file_globals *file_globals_p TSRMLS_DC);
void lcg_init_globals(php_lcg_globals *lcg_globals_p TSRMLS_DC);
void php_output_init_globals(php_output_globals *output_globals_p TSRMLS_DC);
void compiler_globals_ctor(zend_compiler_globals *compiler_globals TSRMLS_DC);
void scanner_globals_ctor(zend_scanner_globals *scanner_globals TSRMLS_DC);
void alloc_globals_ctor(zend_alloc_globals *alloc_globals TSRMLS_DC);
void alloc_globals_dtor(zend_alloc_globals *alloc_globals TSRMLS_DC);
void php_win32_core_globals_ctor(php_win32_core_globals *win32_globals TSRMLS_DC);

void tsrm_update_native_tls(void *value);

ZEND_RSRC_DTOR_FUNC(file_context_dtor);
extern int le_stream_context;
extern int lcg_globals_id;


#ifdef __cplusplus
extern "C"
{
#endif

extern ZEND_API int compiler_globals_id;
extern ZEND_API int executor_globals_id;
extern ZEND_API int alloc_globals_id;
extern ZEND_API int core_globals_id;
extern ZEND_API ts_rsrc_id ini_scanner_globals_id;
extern ZEND_API ts_rsrc_id language_scanner_globals_id;
extern ZEND_API int sapi_globals_id;
extern ZEND_API int file_globals_id;
extern ZEND_API int output_globals_id;
extern ZEND_API int php_win32_core_globals_id;
extern ZEND_API int com_globals_id;
extern ZEND_API int ps_globals_id;

ZEND_API int php_le_stream_context(void);

/* startup/shutdown */
ZEND_API int tsrm_startup(int expected_threads, int expected_resources, int debug_level, char *debug_filename);
ZEND_API void tsrm_shutdown(void);

/* allocates a new thread-safe-resource id */
ZEND_API ts_rsrc_id ts_allocate_id(ts_rsrc_id *rsrc_id, size_t size, ts_allocate_ctor ctor, ts_allocate_dtor dtor);

/* fetches the requested resource for the current thread */
ZEND_API void *ts_resource_ex(ts_rsrc_id id, THREAD_T *th_id);

/* frees all resources allocated for the current thread */
ZEND_API void ts_free_thread(void);

/* deallocates all occurrences of a given id */
ZEND_API void ts_free_id(ts_rsrc_id id);

//ZEND_API int tsrm_error(int level, const char *format, ...);
ZEND_API void tsrm_error_set(int level, char *debug_filename);

/* utility functions */
ZEND_API THREAD_T tsrm_thread_id(void);
ZEND_API MUTEX_T tsrm_mutex_alloc(void);
ZEND_API void tsrm_mutex_free(MUTEX_T mutexp);
ZEND_API int tsrm_mutex_lock(MUTEX_T mutexp);
ZEND_API int tsrm_mutex_unlock(MUTEX_T mutexp);

ZEND_API void *tsrm_set_new_thread_begin_handler(tsrm_thread_begin_func_t new_thread_begin_handler);
ZEND_API void *tsrm_set_new_thread_end_handler(tsrm_thread_end_func_t new_thread_end_handler);

ZEND_API int tsrm_win32_access(const char *pathname, int mode);

#ifdef __cplusplus
}
#endif
