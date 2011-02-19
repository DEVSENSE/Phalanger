//
// ExtSupport.PHP4 - substitute for php4ts.dll
//
// Unsupported.h
// - contains declarations of exported Zend API functions that are not supported by PHP.NET
//

#pragma once

#include "stdafx.h"
#include "ExtSupport.h"
#include "LinkedLists.h"
#include "Strings.h"

#define SAPI_POST_READER_FUNC(post_reader) void post_reader(TSRMLS_D)

typedef struct
{
	char *header;
	uint header_len;
	zend_bool replace;
} sapi_header_struct;

typedef struct
{
	zend_llist headers;
	int http_response_code;
	unsigned char send_default_content_type;
	char *mimetype;
	char *http_status_line;
} sapi_headers_struct;

typedef struct _sapi_module_struct
{
	char *name;
	char *pretty_name;

	int (*startup)(struct _sapi_module_struct *sapi_module);
	int (*shutdown)(struct _sapi_module_struct *sapi_module);

	int (*activate)(TSRMLS_D);
	int (*deactivate)(TSRMLS_D);

	int (*ub_write)(const char *str, unsigned int str_length TSRMLS_DC);
	void (*flush)(void *server_context);
	struct stat *(*get_stat)(TSRMLS_D);
	char *(*getenv)(char *name, size_t name_len TSRMLS_DC);

	void (*sapi_error)(int type, const char *error_msg, ...);

	int (*header_handler)(sapi_header_struct *sapi_header, sapi_headers_struct *sapi_headers TSRMLS_DC);
	int (*send_headers)(sapi_headers_struct *sapi_headers TSRMLS_DC);
	void (*send_header)(sapi_header_struct *sapi_header, void *server_context TSRMLS_DC);

	int (*read_post)(char *buffer, uint count_bytes TSRMLS_DC);
	char *(*read_cookies)(TSRMLS_D);

	void (*register_server_variables)(zval *track_vars_array TSRMLS_DC);
	void (*log_message)(char *message);

	char *php_ini_path_override;

	void (*block_interruptions)(void);
	void (*unblock_interruptions)(void);

	void (*default_post_reader)(TSRMLS_D);
	void (*treat_data)(int arg, char *str, zval *destArray TSRMLS_DC);
	char *executable_location;

	int php_ini_ignore;

	int (*get_fd)(int *fd TSRMLS_DC);

	int (*force_http_10)(TSRMLS_D);

	int (*get_target_uid)(uid_t * TSRMLS_DC);
	int (*get_target_gid)(gid_t * TSRMLS_DC);

	unsigned int (*input_filter)(int arg, char *var, char **val, unsigned int val_len TSRMLS_DC);
	
	void (*ini_defaults)(HashTable *configuration_hash);
	int phpinfo_as_text;
} sapi_module_struct;

typedef enum
{								/* Parameter: 			*/
	SAPI_HEADER_REPLACE,		/* sapi_header_line* 	*/
	SAPI_HEADER_ADD,			/* sapi_header_line* 	*/
	SAPI_HEADER_SET_STATUS		/* int 					*/
} sapi_header_op_enum;

typedef struct _sapi_post_entry
{
	char *content_type;
	uint content_type_len;
	void (*post_reader)(TSRMLS_D);
	void (*post_handler)(char *content_type_dup, void *arg TSRMLS_DC);
} sapi_post_entry;

#ifdef __cplusplus
extern "C"
{
#endif

extern ZEND_API /*zend_op_array*/void *(*zend_compile_file)(/*zend_file_handle*/void *file_handle, int type TSRMLS_DC);
extern ZEND_API void (*zend_execute)(/*zend_op_array*/void *op_array TSRMLS_DC);
extern ZEND_API void (*zend_execute_internal)(/*zend_execute_data*/void *execute_data_ptr, int return_value_used TSRMLS_DC);

ZEND_API extern sapi_module_struct sapi_module;
ZEND_API extern zend_llist zend_extensions;

ZEND_API extern char *php_ini_opened_path;
ZEND_API extern char *php_ini_scanned_files;

ZEND_API void sapi_activate(TSRMLS_D);
ZEND_API void sapi_activate_headers_only(TSRMLS_D);
ZEND_API int sapi_add_header_ex(char *header_line, uint header_line_len, zend_bool duplicate, zend_bool replace TSRMLS_DC);
ZEND_API size_t sapi_apply_default_charset(char **mimetype, size_t len TSRMLS_DC);
ZEND_API void sapi_deactivate(TSRMLS_D);
ZEND_API int sapi_flush(TSRMLS_D);
ZEND_API int sapi_force_http_10(TSRMLS_D);
ZEND_API void sapi_free_header(sapi_header_struct *sapi_header);
ZEND_API char *sapi_get_default_content_type(TSRMLS_D);
ZEND_API void sapi_get_default_content_type_header(sapi_header_struct *default_header TSRMLS_DC);
ZEND_API int sapi_get_fd(int *fd TSRMLS_DC);
ZEND_API struct stat *sapi_get_stat(TSRMLS_D);
ZEND_API int sapi_get_target_gid(gid_t * TSRMLS_DC);
ZEND_API int sapi_get_target_uid(uid_t * TSRMLS_DC);
ZEND_API char *sapi_getenv(char *name, size_t name_len TSRMLS_DC);
ZEND_API void sapi_handle_post(void *arg TSRMLS_DC);
ZEND_API int sapi_header_op(sapi_header_op_enum op, void *arg TSRMLS_DC);
ZEND_API void sapi_initialize_empty_request(TSRMLS_D);
ZEND_API SAPI_POST_READER_FUNC(sapi_read_standard_form_data);
ZEND_API int sapi_register_default_post_reader(void (*default_post_reader)(TSRMLS_D));
ZEND_API int sapi_register_post_entries(sapi_post_entry *post_entry);
ZEND_API int sapi_register_post_entry(sapi_post_entry *post_entry);
ZEND_API int sapi_register_treat_data(void (*treat_data)(int arg, char *str, zval *destArray TSRMLS_DC));
ZEND_API int sapi_send_headers(TSRMLS_D);
ZEND_API void sapi_shutdown(void);
ZEND_API void sapi_startup(sapi_module_struct *sf);
ZEND_API void sapi_unregister_post_entry(sapi_post_entry *post_entry);
ZEND_API int sapi_register_input_filter(unsigned int (*input_filter)(int arg, char *var, char **val, unsigned int val_len TSRMLS_DC));

ZEND_API void php_COM_addref(void);
ZEND_API void php_COM_call_function_handler(void);
ZEND_API void php_COM_clone(void);
ZEND_API void php_COM_destruct(void);
ZEND_API void php_COM_error_message(void);
ZEND_API void php_COM_export_as_sink(void);
ZEND_API void php_COM_export_object(void);
ZEND_API void php_COM_get_ids_of_names(void);
ZEND_API void php_COM_get_le_comval(void);
ZEND_API void php_COM_get_property_handler(void);
ZEND_API void php_COM_invoke(void);
ZEND_API void php_COM_load_typelib(void);
ZEND_API void php_COM_object_from_dispatch(void);
ZEND_API void php_COM_release(void);
ZEND_API void php_COM_set(void);
ZEND_API void php_COM_set_property_handler(void);

ZEND_API zend_bool zend_is_auto_global(char *name, uint name_len TSRMLS_DC);

ZEND_API void zend_strip(TSRMLS_D);
ZEND_API void zend_highlight(/*zend_syntax_highlighter_ini*/void *syntax_highlighter_ini TSRMLS_DC);
ZEND_API void zend_indent();
ZEND_API /*zend_op_array*/void *compile_file(/*zend_file_handle*/void *file_handle, int type TSRMLS_DC);
ZEND_API /*zend_op_array*/void *compile_filename(int type, zval *filename TSRMLS_DC);
ZEND_API /*zend_op_array*/void *compile_string(zval *source_string, char *filename TSRMLS_DC);
ZEND_API void init_op_array(/*zend_op_array*/void *op_array, zend_uchar type, int initial_ops_size TSRMLS_DC);
ZEND_API void destroy_op_array(/*zend_op_array*/void *op_array TSRMLS_DC);

ZEND_API void execute(/*zend_op_array*/void *op_array TSRMLS_DC);
ZEND_API void execute_internal(/*zend_execute_data*/void *execute_data_ptr, int return_value_used TSRMLS_DC);
ZEND_API void function_add_ref(/*zend_function*/void *function);

ZEND_API int highlight_file(char *filename, /*zend_syntax_highlighter_ini*/void *syntax_highlighter_ini TSRMLS_DC);
ZEND_API int highlight_string(zval *str, /*zend_syntax_highlighter_ini*/void *syntax_highlighter_ini,
							  char *str_name TSRMLS_DC);
ZEND_API int lex_scan(zval *zendlval TSRMLS_DC);

ZEND_API char *zend_set_compiled_filename(char *new_compiled_filename TSRMLS_DC);
ZEND_API void zend_restore_compiled_filename(char *original_compiled_filename TSRMLS_DC);
ZEND_API char *zend_get_compiled_filename(TSRMLS_D);
ZEND_API int zend_get_compiled_lineno(TSRMLS_D);
ZEND_API zend_bool zend_is_compiling(TSRMLS_D);
ZEND_API char *zend_get_executed_filename(TSRMLS_D);
ZEND_API uint zend_get_executed_lineno(TSRMLS_D);
ZEND_API zend_bool zend_is_executing(TSRMLS_D);
ZEND_API int zend_eval_string(char *str, zval *retval_ptr, char *string_name TSRMLS_DC);
ZEND_API int zend_eval_stringl(char *str, int str_len, zval *retval_ptr, char *string_name TSRMLS_DC);
ZEND_API int zend_execute_scripts(int type TSRMLS_DC, zval **retval, int file_count, ...);
ZEND_API int php_execute_script(/*zend_file_handle*/void *primary_file TSRMLS_DC);
ZEND_API void zend_save_lexical_state(/*zend_lex_state*/void *lex_state TSRMLS_DC);
ZEND_API void zend_restore_lexical_state(/*zend_lex_state*/void *lex_state TSRMLS_DC);
ZEND_API int zend_prepare_string_for_scanning(zval *str, char *filename TSRMLS_DC);
ZEND_API char *zend_make_compiled_string_description(char *name TSRMLS_DC);
ZEND_API int zend_get_resource_handle(/*zend_extension*/void *extension);
ZEND_API int zend_disable_function(char *function_name, uint function_name_length TSRMLS_DC);
ZEND_API int zend_disable_class(char *class_name, uint class_name_length TSRMLS_DC);
ZEND_API void zend_destroy_file_handle(/*zend_file_handle*/void *file_handle TSRMLS_DC);

ZEND_API int zend_startup_module(zend_module_entry *module);
ZEND_API int zend_register_module(zend_module_entry *module);
ZEND_API int zend_register_extension(/*zend_extension*/void *new_extension, DL_HANDLE handle);
ZEND_API int zend_load_extension(char *path);
ZEND_API char *zend_get_module_version(char *module_name);
ZEND_API zend_module_entry *zend_get_module(int module_number);
ZEND_API /*zend_extension*/void *zend_get_extension(char *extension_name);
ZEND_API void zend_extension_dispatch_message(int message, void *arg);
ZEND_API void zend_message_dispatcher(long message, void *data);
ZEND_API int php_startup_extensions(zend_module_entry **ptr, int count);

ZEND_API void var_replace(/*php_unserialize_data_t*/void *var_hashx, zval *ozval, zval **nzval);
ZEND_API void var_destroy(/*php_unserialize_data_t*/void *var_hashx);

ZEND_API void tsrm_win32_startup(void);
ZEND_API void tsrm_win32_shutdown(void);
ZEND_API void start_memory_manager(TSRMLS_D);
ZEND_API void shutdown_memory_manager(int silent, int clean_cache TSRMLS_DC);

ZEND_API void session_adapt_url(const char *url, size_t urllen, char **_new, size_t *newlen TSRMLS_DC);
ZEND_API void rfc1867_post_handler(char *content_type_dup, void *arg TSRMLS_DC);
ZEND_API void php_std_post_handler(char *content_type_dup, void *arg TSRMLS_DC);

ZEND_API void php_pval_to_variant(pval *pval_arg, VARIANT *var_arg, int codepage TSRMLS_DC);
ZEND_API void php_pval_to_variant_ex(pval *pval_arg, VARIANT *var_arg, pval *pval_type, int codepage TSRMLS_DC);
ZEND_API void php_pval_to_variant_ex2(pval *pval_arg, VARIANT *var_arg, VARTYPE type, int codepage TSRMLS_DC);
ZEND_API int php_variant_to_pval(VARIANT *var_arg, pval *pval_arg, int codepage TSRMLS_DC);
ZEND_API OLECHAR *php_char_to_OLECHAR(char *C_str, uint strlen, int codepage TSRMLS_DC);
ZEND_API char *php_OLECHAR_to_char(OLECHAR *unicode_str, uint *out_length, int codepage TSRMLS_DC);

ZEND_API void php_var_serialize(smart_str *buf, zval **struc, HashTable *var_hash TSRMLS_DC);
ZEND_API int php_var_unserialize(zval **rval, const char **p, const char *max, /*php_unserialize_data_t*/void *var_hash TSRMLS_DC);

ZEND_API int php_register_info_logo(char *logo_string, char *mimetype, unsigned char *data, int size);
ZEND_API int php_unregister_info_logo(char *logo_string);

ZEND_API void php_statpage(TSRMLS_D);

ZEND_API int php_start_ob_buffer(zval *output_handler, uint chunk_size, zend_bool erase TSRMLS_DC);
ZEND_API int php_start_ob_buffer_named(const char *output_handler_name, uint chunk_size, zend_bool erase TSRMLS_DC);
ZEND_API void php_end_ob_buffer(zend_bool send_buffer, zend_bool just_flush TSRMLS_DC);
ZEND_API void php_end_ob_buffers(zend_bool send_buffer TSRMLS_DC);
ZEND_API void php_start_implicit_flush(TSRMLS_D);
ZEND_API void php_end_implicit_flush(TSRMLS_D);
ZEND_API void php_ob_set_internal_handler(/*php_output_handler_func_t*/void *internal_output_handler,
										  uint buffer_size, char *handler_name, zend_bool erase TSRMLS_DC);
ZEND_API int php_ob_init_conflict(char *handler_new, char *handler_set TSRMLS_DC);
ZEND_API int php_ob_handler_used(char *handler_name TSRMLS_DC);
ZEND_API int php_ob_get_buffer(zval *p TSRMLS_DC);
ZEND_API int php_ob_get_length(zval *p TSRMLS_DC);
ZEND_API char *php_get_output_start_filename(TSRMLS_D);
ZEND_API int php_get_output_start_lineno(TSRMLS_D);

ZEND_API int php_setcookie(char *name, int name_len, char *value, int value_len, time_t expires, char *path,
						   int path_len, char *domain, int domain_len, int secure TSRMLS_DC);
ZEND_API int php_session_register_module(/*ps_module*/void *);
ZEND_API int php_session_register_serializer(const char *name, int (*encode)(/*PS_SERIALIZER_ENCODE_ARGS*/),
	        int (*decode)(/*PS_SERIALIZER_DECODE_ARGS*/));
ZEND_API void php_session_set_id(char *id TSRMLS_DC);
ZEND_API void php_session_start(TSRMLS_D);

ZEND_API int php_request_startup(TSRMLS_D);
ZEND_API void php_request_shutdown(void *dummy);
ZEND_API void php_request_shutdown_for_exec(void *dummy);

ZEND_API void php_register_variable(char *var, char *strval, zval *track_vars_array TSRMLS_DC);
ZEND_API void php_register_variable_ex(char *var, zval *val, pval *track_vars_array TSRMLS_DC);
ZEND_API void php_register_variable_safe(char *var, char *strval, int str_len, zval *track_vars_array TSRMLS_DC);

ZEND_API char *php_reg_replace(const char *pattern, const char *replace, const char *string, int icase, int extended);

ZEND_API void php_output_startup(void);
ZEND_API void php_output_set_status(zend_bool status TSRMLS_DC);
ZEND_API void php_output_register_constants(TSRMLS_D);
ZEND_API void php_output_activate(TSRMLS_D);

ZEND_API int php_module_startup(/*sapi_module_struct*/void *sf, zend_module_entry *additional_modules,
								uint num_additional_modules);
ZEND_API int php_module_shutdown_wrapper(/*sapi_module_struct*/void *sapi_globals);
ZEND_API void php_module_shutdown_for_exec();
ZEND_API void php_module_shutdown(TSRMLS_D);

ZEND_API int php_lint_script(/*zend_file_handle*/void *file TSRMLS_DC);

ZEND_API int php_header_write(const char *str, uint str_length TSRMLS_DC);
ZEND_API int php_header();
ZEND_API int php_handle_special_queries(TSRMLS_D);
ZEND_API int php_handle_auth_data(const char *auth TSRMLS_DC);
ZEND_API void php_handle_aborted_connection(void);
ZEND_API long php_getlastmod(TSRMLS_D);

ZEND_API void php_get_highlight_struct(/*zend_syntax_highlighter_ini*/void *syntax_highlighter_ini);
ZEND_API void php_default_treat_data(int arg, char *str, zval* destArray TSRMLS_DC);
ZEND_API void php_default_post_reader(TSRMLS_D);

ZEND_API int open_file_for_scanning(/*zend_file_handle*/void *file_handle TSRMLS_DC);
ZEND_API int do_bind_function_or_class(/*zend_op*/void *opline, HashTable *function_table, HashTable *class_table,
									   int compile_time);
ZEND_API /*pcre*/void *pcre_get_compiled_regex(char *regex, /*pcre_extra*/void **extra, int *preg_options);
ZEND_API char *php_pcre_replace(char *regex, int regex_len, char *subject, int subject_len, zval *replace_val,
								int is_callable_replace, int *result_len, int limit TSRMLS_DC);

ZEND_API int php_VARIANT_get_le_variant();

ZEND_API int zend_parse_ini_file(/*zend_file_handle*/void *fh, zend_bool unbuffered_errors,
								 /*zend_ini_parser_cb_t*/void *ini_parser_cb, void *arg);

ZEND_API void xml_utf8_decode(void);

ZEND_API int TSendMail(char *host, int *error, char **error_message, char *headers, char *Subject,
					   char *mailTo, char *data, char *mailCc, char *mailBcc, char *mailRPath);
ZEND_API char *GetSMErrorText(int index);
ZEND_API void TSMClose();

ZEND_API int zend_register_auto_global(char *name, uint name_len TSRMLS_DC);

#ifdef __cplusplus
}
#endif
