//
// ExtSupport.PHP4 - substitute for php4ts.dll
//
// IniConfig.h
// - contains declarations of php.ini related exported functions
//

#pragma once

#include "stdafx.h"
#include "ExtSupport.h"
#include "Streams.h"

#define ZEND_INI_USER	(1 << 0)
#define ZEND_INI_PERDIR	(1 << 1)
#define ZEND_INI_SYSTEM	(1 << 2)
#define ZEND_INI_ALL	(ZEND_INI_USER | ZEND_INI_PERDIR | ZEND_INI_SYSTEM)

#define INI_INT(name) zend_ini_long((name), sizeof(name), 0)
#define INI_FLT(name) zend_ini_double((name), sizeof(name), 0)
#define INI_STR(name) zend_ini_string_ex((name), sizeof(name), 0, NULL)
#define INI_BOOL(name) ((zend_bool) INI_INT(name))

#define INI_ORIG_INT(name)	zend_ini_long((name), sizeof(name), 1)
#define INI_ORIG_FLT(name)	zend_ini_double((name), sizeof(name), 1)
#define INI_ORIG_STR(name)	zend_ini_string((name), sizeof(name), 1)
#define INI_ORIG_BOOL(name) ((zend_bool) INI_ORIG_INT(name))

#define XtOffsetOf(s_type, field) offsetof(s_type, field)

typedef struct _zend_ini_entry zend_ini_entry;

#define ZEND_INI_MH(name) int name(zend_ini_entry *entry, char *new_value, uint new_value_length, void *mh_arg1, void *mh_arg2, void *mh_arg3, int stage TSRMLS_DC)
#define ZEND_INI_DISP(name) void name(zend_ini_entry *ini_entry, int type)

struct _zend_ini_entry
{
	int module_number;
	int modifiable;
	char *name;
	unsigned int name_length;
	ZEND_INI_MH((*on_modify));
	void *mh_arg1;
	void *mh_arg2;
	void *mh_arg3;

	char *value;
	unsigned int value_length;

	char *orig_value;
	unsigned int orig_value_length;
	int modified;

	void (*displayer)(zend_ini_entry *ini_entry, int type);
};


#ifdef __cplusplus
extern "C"
{
#endif

void zend_ini_refresh_ini_entries_one_module(int module_number TSRMLS_DC);
void zend_ini_refresh_caches_one_module(int module_number TSRMLS_DC);
void zend_ini_update_configuration_one_module(int module_number TSRMLS_DC);
char *zend_ini_string_fixed(char *name, uint name_length, int orig);

ZEND_API int zend_ini_startup(TSRMLS_D);
ZEND_API int zend_ini_shutdown(TSRMLS_D);
ZEND_API int zend_ini_deactivate(TSRMLS_D);
ZEND_API int zend_ini_global_shutdown(TSRMLS_D);

ZEND_API int zend_copy_ini_directives(TSRMLS_D);

ZEND_API void zend_ini_sort_entries(TSRMLS_D);

ZEND_API int zend_register_ini_entries(zend_ini_entry *ini_entry, int module_number TSRMLS_DC);
ZEND_API void zend_unregister_ini_entries(int module_number TSRMLS_DC);
ZEND_API void zend_ini_refresh_caches(int stage TSRMLS_DC);
ZEND_API int zend_alter_ini_entry(char *name, uint name_length, char *new_value, uint new_value_length, int modify_type, int stage);
ZEND_API int zend_restore_ini_entry(char *name, uint name_length, int stage);
ZEND_API void display_ini_entries(zend_module_entry *module);

ZEND_API long zend_ini_long(char *name, uint name_length, int orig);
ZEND_API double zend_ini_double(char *name, uint name_length, int orig);
ZEND_API char *zend_ini_string(char *name, uint name_length, int orig);
ZEND_API char *zend_ini_string_ex(char *name, uint name_length, int orig, zend_bool *exists);

ZEND_API int zend_ini_register_displayer(char *name, uint name_length, void (*displayer)(zend_ini_entry *ini_entry, int type));

ZEND_API ZEND_INI_DISP(zend_ini_boolean_displayer_cb);
ZEND_API ZEND_INI_DISP(zend_ini_color_displayer_cb);
ZEND_API ZEND_INI_DISP(display_link_numbers);

/* Standard message handlers */
ZEND_API ZEND_INI_MH(OnUpdateBool);
ZEND_API ZEND_INI_MH(OnUpdateInt);
ZEND_API ZEND_INI_MH(OnUpdateLong);
ZEND_API ZEND_INI_MH(OnUpdateLongGEZero);
ZEND_API ZEND_INI_MH(OnUpdateReal);
ZEND_API ZEND_INI_MH(OnUpdateString);
ZEND_API ZEND_INI_MH(OnUpdateStringUnempty);


#define ZEND_INI_DISPLAY_ORIG	1
#define ZEND_INI_DISPLAY_ACTIVE	2

#define ZEND_INI_STAGE_STARTUP		(1 << 0)
#define ZEND_INI_STAGE_SHUTDOWN		(1 << 1)
#define ZEND_INI_STAGE_ACTIVATE		(1 << 2)
#define ZEND_INI_STAGE_DEACTIVATE	(1 << 3)
#define ZEND_INI_STAGE_RUNTIME		(1 << 4)

/* INI parsing engine */
typedef void (*zend_ini_parser_cb_t)(zval *arg1, zval *arg2, int callback_type, void *arg);
#define ZEND_INI_PARSER_ENTRY	1
#define ZEND_INI_PARSER_SECTION	2

typedef struct _zend_ini_parser_param
{
	zend_ini_parser_cb_t ini_parser_cb;
	void *arg;
} zend_ini_parser_param;

ZEND_API int zend_get_configuration_directive(char *name, uint name_length, zval *contents);
ZEND_API int cfg_get_long(char *varname, long *result);
ZEND_API int cfg_get_double(char *varname, double *result);
ZEND_API int cfg_get_string(char *varname, char **result);

#ifdef __cplusplus
}
#endif
