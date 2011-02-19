//
// ExtSupport.PHP4 - substitute for php4ts.dll
//
// IniConfig.cpp 
// - contains definitions of php.ini related functions
//

#include "stdafx.h"

#include <stdlib.h>

#include "IniConfig.h"
#include "Hash.h"
#include "QSort.h"
#include "TsrmLs.h"
#include "Memory.h"
#include "Variables.h"
#include "Output.h"
#include "Module.h"
#include "Request.h"
#include "PhpInfo.h"
#include "ThreadSafeHash.h"

using namespace System::Threading;
using namespace System::Collections;
using namespace PHP::ExtManager;

// this global table holds directives registered by extensions
static TsHashTable *registered_zend_ini_directives = NULL;

/// <summary>
/// Holds the per-AppDomain extension configuration.
/// </summary>
private ref class Directives
{
public:
	static Directives()
	{
		singleton = gcnew Directives();
	}

private:
	/// <summary>
	/// Creates the table.
	/// </summary>
	Directives()
	{
		table = (HashTable *)malloc(sizeof(HashTable));
		zend_hash_init_ex(table, 100, NULL, NULL, 1, 0);
	}

public:
	/// <summary>
	/// Destroyes the table when AppDomain is torn down.
	/// </summary>
	~Directives()
	{
		if (table != NULL)
		{
			zend_hash_destroy(table);
			free(table);
			table = NULL;
		}
	}

	static property HashTable *Table
	{
		static HashTable *get()
		{
			return table;
		}
	}

private:
	static Directives ^singleton;
	static HashTable *table;
};

#define NO_VALUE_PLAINTEXT		"no value"
#define NO_VALUE_HTML			"<i>no value</i>"

/* hash_apply functions */

// copied from zend_ini.c, slightly modified and beautified
static int zend_remove_ini_entries(zend_ini_entry *ini_entry, int *module_number TSRMLS_DC)
{
	return (ini_entry->module_number == *module_number);
}

// copied from zend_ini.c and beautified
static int zend_restore_ini_entry_cb(zend_ini_entry *ini_entry, int stage TSRMLS_DC)
{
	if (ini_entry->modified)
	{
		if (ini_entry->on_modify) 
		{
			ini_entry->on_modify(ini_entry, ini_entry->orig_value, ini_entry->orig_value_length, 
				ini_entry->mh_arg1, ini_entry->mh_arg2, ini_entry->mh_arg3, stage TSRMLS_CC);
		}
		efree(ini_entry->value);
		ini_entry->value = ini_entry->orig_value;
		ini_entry->value_length = ini_entry->orig_value_length;
		ini_entry->modified = 0;
		ini_entry->orig_value = NULL;
		ini_entry->orig_value_length = 0;
	}
	return 0;
}

/* Startup / shutdown */

// copied from zend_ini.c, slightly modified and beautified
ZEND_API int zend_ini_startup(TSRMLS_D)
{
	registered_zend_ini_directives = (TsHashTable *)malloc(sizeof(TsHashTable));

	//EG(ini_directives) = registered_zend_ini_directives;
	return zend_ts_hash_init_ex(registered_zend_ini_directives, 100, NULL, NULL, 1, 0);
}

// copied from zend_ini.c
ZEND_API int zend_ini_shutdown(TSRMLS_D)
{
	zend_hash_destroy(EG(ini_directives));
	free(EG(ini_directives));

	return SUCCESS;
}

// copied from zend_ini.c and beautified
ZEND_API int zend_ini_deactivate(TSRMLS_D)
{
	zend_hash_apply_with_argument(EG(ini_directives), (apply_func_arg_t)zend_restore_ini_entry_cb, 
		(void *)ZEND_INI_STAGE_DEACTIVATE TSRMLS_CC);
	return SUCCESS;
}

// copied from zend_ini.c (new to PHP5)
ZEND_API int zend_ini_global_shutdown(TSRMLS_D)
{
	zend_ts_hash_destroy(registered_zend_ini_directives);
	free(registered_zend_ini_directives);
	return SUCCESS;
}

// copied from zend_ini.c and beautified (called as part of request initialization)
ZEND_API int zend_copy_ini_directives(TSRMLS_D)
{
	zend_ini_entry ini_entry;

	EG(ini_directives) = (HashTable *)malloc(sizeof(HashTable));
	
	if (zend_hash_init_ex(EG(ini_directives), Directives::Table->nNumOfElements,
		NULL, NULL, 1, 0) == FAILURE) return FAILURE;

	zend_hash_copy(EG(ini_directives), Directives::Table, NULL, &ini_entry, sizeof(zend_ini_entry));

	// should not be necessary as no modules are started by now anyway
	//zend_ini_refresh_caches(ZEND_INI_STAGE_STARTUP TSRMLS_CC);

	/*
	HashPosition pos;
	zval **elem;
	char *string_key;
	unsigned string_key_len;
	unsigned long num_key;
	HashTable *ht = registered_zend_ini_directives;

#ifdef DEBUG
	Debug::WriteLine("PHP4TS", "<registered INI entries>");
#endif
	zend_hash_internal_pointer_reset_ex(ht, &pos);
	while (zend_hash_get_current_data_ex(ht, (void **)&elem, &pos) == SUCCESS)
	{
		if (zend_hash_get_current_key_ex(ht, &string_key, &string_key_len, 
			&num_key, 0, &pos) == HASH_KEY_IS_LONG)
		{
			// key is an integer
#ifdef DEBUG
			Debug::WriteLine("PHP4TS", __box(num_key));
#endif
		}
		else
		{
			// key is a string
#ifdef DEBUG
			Debug::WriteLine("PHP4TS", gcnew String(string_key, 0, string_key_len));
#endif
		}

		zend_hash_move_forward_ex(ht, &pos);
	}
#ifdef DEBUG
	Debug::WriteLine("PHP4TS", "</registered INI entries>");
#endif
	*/

	return SUCCESS;
}

// copied from zend_ini.c and beautified
static int ini_key_compare(const void *a, const void *b TSRMLS_DC)
{
	Bucket *f;
	Bucket *s;
 
	f = *((Bucket **)a);
	s = *((Bucket **)b);

	if (f->nKeyLength == 0 && s->nKeyLength == 0)
	{ 
		/* both numeric */
		return ZEND_NORMALIZE_BOOL(f->nKeyLength - s->nKeyLength);
	} 
	else if (f->nKeyLength == 0)
	{ 
		/* f is numeric, s is not */
		return -1;
	} 
	else if (s->nKeyLength == 0)
	{
		/* s is numeric, f is not */
		return 1;
	} 
	else 
	{	
		/* both strings */
		return zend_binary_strcasecmp(f->arKey, f->nKeyLength, s->arKey, s->nKeyLength);
	}
}

// copied from zend_ini.c
ZEND_API void zend_ini_sort_entries(TSRMLS_D)
{
	zend_hash_sort(EG(ini_directives), zend_qsort, ini_key_compare, 0 TSRMLS_CC);
}

/* Registration / unregistration */

// copied from zend_ini.c and beautified
ZEND_API int zend_register_ini_entries(zend_ini_entry *ini_entry, int module_number TSRMLS_DC)
{
#ifdef DEBUG
	Debug::WriteLine("PHP4TS", "zend_register_ini_entries");
#endif

	zend_ini_entry *p = ini_entry;
	zend_ini_entry *hashed_ini_entry;
	zval default_value;

	while (p->name)
	{
		p->module_number = module_number;

		if (zend_ts_hash_add(registered_zend_ini_directives, p->name, p->name_length, p, sizeof(zend_ini_entry), 
			(void **)&hashed_ini_entry) == FAILURE)
		{
			zend_unregister_ini_entries(module_number TSRMLS_CC);
			return FAILURE;
		}

		if ((zend_get_configuration_directive(p->name, p->name_length, &default_value)) == SUCCESS)
		{
			if (!hashed_ini_entry->on_modify ||
				hashed_ini_entry->on_modify(hashed_ini_entry, default_value.value.str.val, 
				default_value.value.str.len, hashed_ini_entry->mh_arg1, hashed_ini_entry->mh_arg2, 
				hashed_ini_entry->mh_arg3, ZEND_INI_STAGE_STARTUP TSRMLS_CC) == SUCCESS)
			{
				hashed_ini_entry->value = default_value.value.str.val;
				hashed_ini_entry->value_length = default_value.value.str.len;
			}
		}
		else
		{
			if (hashed_ini_entry->on_modify)
			{
				hashed_ini_entry->on_modify(hashed_ini_entry, hashed_ini_entry->value, hashed_ini_entry->value_length,
					hashed_ini_entry->mh_arg1, hashed_ini_entry->mh_arg2, hashed_ini_entry->mh_arg3, 
					ZEND_INI_STAGE_STARTUP TSRMLS_CC);
			}
		}
		p++;
	}
	return SUCCESS;
}

// copied from zend_ini.c and beautified
ZEND_API void zend_unregister_ini_entries(int module_number TSRMLS_DC)
{
	zend_ts_hash_apply_with_argument(registered_zend_ini_directives, (apply_func_arg_t)zend_remove_ini_entries, 
		(void *)&module_number TSRMLS_CC);
}

// copied from zend_ini.c, modified and beautified
static int zend_ini_refresh_cache(zend_ini_entry *p, int stage TSRMLS_DC)
{
	if (p->on_modify) 
	{
		// call on_modify handler only if the module that registered the directive was
		// started for the current request

		bool call = false;
		Request ^request = Request::GetCurrentRequest(false, false);

		if (request == nullptr) call = true;
		else
		{
			IDictionaryEnumerator ^enumerator = request->StartedModules->GetEnumerator();
			while (enumerator->MoveNext() == true)
			{
				if ((static_cast<Module ^>(enumerator->Value))->GetModuleNumber() == p->module_number)
				{
					call = true;
					break;
				}
			}
		}

		if (call == true)
		{
#ifdef DEBUG
			Debug::WriteLine("PHP4TS", "zend_ini_refresh_cache: about to call on_modify handler");
#endif
			p->on_modify(p, p->value, p->value_length, p->mh_arg1, p->mh_arg2, p->mh_arg3, stage TSRMLS_CC);
		}
	}
	return 0;
}

// written from scratch
static int zend_ini_refresh_cache_one_module(zend_ini_entry *p, int module_number TSRMLS_DC)
{
	if (p->on_modify) 
	{
		// call on_modify handler only if the directive was registered by given module

		if (p->module_number == module_number)
		{
#ifdef DEBUG
			Debug::WriteLine("PHP4TS", "zend_ini_refresh_cache_one_module: about to call on_modify handler");
#endif
			p->on_modify(p, p->value, p->value_length, p->mh_arg1, p->mh_arg2, p->mh_arg3, ZEND_INI_STAGE_STARTUP TSRMLS_CC);
		}
	}
	return 0;
}

// written from scratch
static int zend_ini_refresh_ini_entry_one_module(zend_ini_entry *p, int module_number TSRMLS_DC)
{
	if (p->module_number == module_number)
	{
		zend_ini_entry *hashed_ini_entry;
		zend_hash_add(Directives::Table, p->name, p->name_length, p, sizeof(zend_ini_entry), 
			(void **)&hashed_ini_entry);
	}
	return 0;
}

// written from scratch
static int zend_ini_update_config_one_module(zend_ini_entry *p, int module_number TSRMLS_DC)
{
	if (p->module_number == module_number)
	{
		zval default_value;
		if ((zend_get_configuration_directive(p->name, p->name_length, &default_value)) == SUCCESS)
		{
			p->value = default_value.value.str.val;
			p->value_length = default_value.value.str.len;
		}
	}
	return 0;
}

// copied from php_ini.c, modified and beautified
static void php_ini_displayer_cb(zend_ini_entry *ini_entry, int type)
{
	if (ini_entry->displayer) ini_entry->displayer(ini_entry, type);
	else
	{
		char *display_string;
		uint display_string_length, esc_html = 1;
		TSRMLS_FETCH();

		if (type == ZEND_INI_DISPLAY_ORIG && ini_entry->modified)
		{
			if (ini_entry->orig_value && ini_entry->orig_value[0])
			{
				display_string = ini_entry->orig_value;
				display_string_length = ini_entry->orig_value_length;
				//esc_html = !sapi_module.phpinfo_as_text;
			}
			else
			{
				//if (!sapi_module.phpinfo_as_text)
				{
					display_string = "<i>no value</i>";
					display_string_length = sizeof("<i>no value</i>") - 1;
				}
				//else
				//{
				//	display_string = "no value";
				//	display_string_length = sizeof("no value") - 1;
				//}	
			}
		}
		else if (ini_entry->value && ini_entry->value[0])
		{
			display_string = ini_entry->value;
			display_string_length = ini_entry->value_length;
			//esc_html = !sapi_module.phpinfo_as_text;
		}
		else
		{
			//if (!sapi_module.phpinfo_as_text)
			{
				display_string = "<i>no value</i>";
				display_string_length = sizeof("<i>no value</i>") - 1;
			}
			//else
			//{
			//	display_string = "no value";
			//	display_string_length = sizeof("no value") - 1;
			//}	
		}

		if (esc_html) php_html_puts(display_string, display_string_length TSRMLS_CC);
		else PHPWRITE(display_string, display_string_length);
	}
}

// copied from php_ini.c and beautified
static int php_ini_displayer(zend_ini_entry *ini_entry, int module_number TSRMLS_DC)
{
	if (ini_entry->module_number != module_number) return 0;

	//if (!sapi_module.phpinfo_as_text)
	{
		PUTS("<tr>");
		PUTS("<td class=\"e\">");
		PHPWRITE(ini_entry->name, ini_entry->name_length - 1);
		PUTS("</td><td class=\"v\">");
		php_ini_displayer_cb(ini_entry, ZEND_INI_DISPLAY_ACTIVE);
		PUTS("</td><td class=\"v\">");
		php_ini_displayer_cb(ini_entry, ZEND_INI_DISPLAY_ORIG);
		PUTS("</td></tr>\n");
	}
	//else
	//{
	//	PHPWRITE(ini_entry->name, ini_entry->name_length - 1);
	//	PUTS(" => ");
	//	php_ini_displayer_cb(ini_entry, ZEND_INI_DISPLAY_ACTIVE);
	//	PUTS(" => ");
	//	php_ini_displayer_cb(ini_entry, ZEND_INI_DISPLAY_ORIG);
	//	PUTS("\n");
	//}	
	return 0;
}

// copied from zend_ini.c and beautified
ZEND_API void zend_ini_refresh_caches(int stage TSRMLS_DC)
{
	zend_hash_apply_with_argument(EG(ini_directives), (apply_func_arg_t)zend_ini_refresh_cache,
		(void *)(long)stage TSRMLS_CC);
}

// written from scratch
void zend_ini_refresh_caches_one_module(int module_number TSRMLS_DC)
{
	zend_hash_apply_with_argument(EG(ini_directives), (apply_func_arg_t)zend_ini_refresh_cache_one_module,
		(void *)(long)module_number TSRMLS_CC);
}

// written from scratch
void zend_ini_refresh_ini_entries_one_module(int module_number TSRMLS_DC)
{
	zend_ts_hash_apply_with_argument(registered_zend_ini_directives, (apply_func_arg_t)zend_ini_refresh_ini_entry_one_module,
		(void *)(long)module_number TSRMLS_CC);
}

// written from scratch
void zend_ini_update_configuration_one_module(int module_number TSRMLS_DC)
{
	zend_hash_apply_with_argument(EG(ini_directives), (apply_func_arg_t)zend_ini_update_config_one_module,
		(void *)(long)module_number TSRMLS_CC);
}

// copied from zend_ini.c and beautified
ZEND_API int zend_alter_ini_entry(char *name, uint name_length, char *new_value, uint new_value_length, int modify_type, int stage)
{
	zend_ini_entry *ini_entry;
	char *duplicate;
	TSRMLS_FETCH();

#ifdef DEBUG
	Debug::WriteLine("PHP4TS", "zend_alter_ini_entry");
#endif

	if (zend_hash_find(EG(ini_directives), name, name_length, (void **)&ini_entry) == FAILURE) return FAILURE;

	if (!(ini_entry->modifiable & modify_type)) return FAILURE;

	duplicate = estrndup(new_value, new_value_length);
	
	if (!ini_entry->on_modify || ini_entry->on_modify(ini_entry, duplicate, new_value_length, ini_entry->mh_arg1,
		ini_entry->mh_arg2, ini_entry->mh_arg3, stage TSRMLS_CC) == SUCCESS)
	{
		if (!ini_entry->modified)
		{
			ini_entry->orig_value = ini_entry->value;
			ini_entry->orig_value_length = ini_entry->value_length;
		}
		else
		{
			/* we already changed the value, free the changed value */
			efree(ini_entry->value);
		}
		ini_entry->value = duplicate;
		ini_entry->value_length = new_value_length;
		ini_entry->modified = 1;
	} 
	else efree(duplicate);

	return SUCCESS;
}

// copied from zend_ini.c and beautified
ZEND_API int zend_restore_ini_entry(char *name, uint name_length, int stage)
{
	zend_ini_entry *ini_entry;
	TSRMLS_FETCH();

	if (zend_hash_find(EG(ini_directives), name, name_length, (void **)&ini_entry) == FAILURE) return FAILURE;
	Request::EnsureModuleStarted(Module::GetModuleByModuleNumber(ini_entry->module_number));

	zend_restore_ini_entry_cb(ini_entry, stage TSRMLS_CC);
	return SUCCESS;
}

// copied from php_ini.c and beautified
ZEND_API void display_ini_entries(zend_module_entry *module)
{
	int module_number;
	TSRMLS_FETCH();

	if (module) module_number = module->module_number;
	else module_number = 0;

	php_info_print_table_start();
	php_info_print_table_header(3, "Directive", "Local Value", "Master Value");
	zend_hash_apply_with_argument(EG(ini_directives), (apply_func_arg_t)php_ini_displayer,
		(void *)(long) module_number TSRMLS_CC);
	php_info_print_table_end();
}

// copied from zend_ini.c and beautified
ZEND_API int zend_ini_register_displayer(char *name, uint name_length, void (*displayer)(zend_ini_entry *ini_entry, int type))
{
	zend_ini_entry *ini_entry;

	if (zend_hash_find(Directives::Table, name, name_length, 
		(void **)&ini_entry) == FAILURE) return FAILURE;

	ini_entry->displayer = displayer;
	return SUCCESS;
}

#pragma unmanaged

/* Data retrieval */

// copied from zend_ini.c and beautified
ZEND_API long zend_ini_long(char *name, uint name_length, int orig)
{
	zend_ini_entry *ini_entry;
	TSRMLS_FETCH();

	if (zend_hash_find(EG(ini_directives), name, name_length, (void **)&ini_entry) == SUCCESS)
	{
		if (orig && ini_entry->modified)
		{
			return (ini_entry->orig_value ? strtol(ini_entry->orig_value, NULL, 0) : 0);
		} 
		else if (ini_entry->value) return strtol(ini_entry->value, NULL, 0);
	}

	return 0;
}

// copied from zend_ini.c and beautified
ZEND_API double zend_ini_double(char *name, uint name_length, int orig)
{
	zend_ini_entry *ini_entry;
	TSRMLS_FETCH();

	if (zend_hash_find(EG(ini_directives), name, name_length, (void **)&ini_entry) == SUCCESS)
	{
		if (orig && ini_entry->modified)
		{
			return (double)(ini_entry->orig_value ? strtod(ini_entry->orig_value, NULL) : 0.0);
		} 
		else if (ini_entry->value) return (double)strtod(ini_entry->value, NULL);
	}

	return 0.0;
}

// copied from zend_ini.c
ZEND_API char *zend_ini_string_ex(char *name, uint name_length, int orig, zend_bool *exists) /* {{{ */
{
	zend_ini_entry *ini_entry;
	TSRMLS_FETCH();

	if (zend_hash_find(EG(ini_directives), name, name_length, (void **) &ini_entry) == SUCCESS) {
		if (exists) {
			*exists = 1;
		}

		if (orig && ini_entry->modified) {
			return ini_entry->orig_value;
		} else {
			return ini_entry->value;
		}
	} else {
		if (exists) {
			*exists = 0;
		}
		return NULL;
	}
}
/* }}} */

ZEND_API char *zend_ini_string(char *name, uint name_length, int orig) /* {{{ */
{
	zend_bool exists = 1;
	char *return_value;

	return_value = zend_ini_string_ex(name, name_length, orig, &exists);
	if (!exists) {
		return NULL;
	} else if (!return_value) {
		return_value = "";
	}
	return return_value;
}
/* }}} */


// copied from zend_ini.c and beautified
static void zend_ini_displayer_cb(zend_ini_entry *ini_entry, int type)
{
	if (ini_entry->displayer) ini_entry->displayer(ini_entry, type);
	else
	{
		char *display_string;
		uint display_string_length;

		if (type == ZEND_INI_DISPLAY_ORIG && ini_entry->modified)
		{
			if (ini_entry->orig_value)
			{
				display_string = ini_entry->orig_value;
				display_string_length = ini_entry->orig_value_length;
			}
			else
			{
				if (zend_uv.html_errors)
				{
					display_string = NO_VALUE_HTML;
					display_string_length = sizeof(NO_VALUE_HTML) - 1;
				}
				else
				{
					display_string = NO_VALUE_PLAINTEXT;
					display_string_length = sizeof(NO_VALUE_PLAINTEXT) - 1;
				}	
			}
		}
		else if (ini_entry->value && ini_entry->value[0])
		{
			display_string = ini_entry->value;
			display_string_length = ini_entry->value_length;
		}
		else
		{
			if (zend_uv.html_errors)
			{
				display_string = NO_VALUE_HTML;
				display_string_length = sizeof(NO_VALUE_HTML) - 1;
			} 
			else
			{
				display_string = NO_VALUE_PLAINTEXT;
				display_string_length = sizeof(NO_VALUE_PLAINTEXT) - 1;
			}	
		}
		ZEND_WRITE(display_string, display_string_length);
	}
}

// copied from zend_ini.c and beautified
ZEND_INI_DISP(zend_ini_boolean_displayer_cb)
{
	int value;

	if (type == ZEND_INI_DISPLAY_ORIG && ini_entry->modified)
	{
		value = (ini_entry->orig_value ? atoi(ini_entry->orig_value) : 0);
	} 
	else if (ini_entry->value) value = atoi(ini_entry->value);
	else value = 0;

	if (value) ZEND_PUTS("On");
	else ZEND_PUTS("Off");
}

// copied from zend_ini.c and beautified
ZEND_INI_DISP(zend_ini_color_displayer_cb)
{
	char *value;

	if (type == ZEND_INI_DISPLAY_ORIG && ini_entry->modified) value = ini_entry->orig_value;
	else if (ini_entry->value) value = ini_entry->value;
	else value = NULL;

	if (value)
	{
		if (zend_uv.html_errors) zend_printf("<font style=\"color: %s\">%s</font>", value, value);
		else ZEND_PUTS(value);
	}
	else
	{
		if (zend_uv.html_errors) ZEND_PUTS(NO_VALUE_HTML);
		else ZEND_PUTS(NO_VALUE_PLAINTEXT);
	}
}

// copied from zend_ini.c and beautified
ZEND_INI_DISP(display_link_numbers)
{
	char *value;

	if (type == ZEND_INI_DISPLAY_ORIG && ini_entry->modified) value = ini_entry->orig_value;
	else if (ini_entry->value) value = ini_entry->value;
	else value = NULL;

	if (value)
	{
		if (atoi(value) == -1) ZEND_PUTS("Unlimited");
		else zend_printf("%s", value);
	}
}

/* Standard message handlers */

// copied from zend_ini.c, slightly modified and beautified
ZEND_API ZEND_INI_MH(OnUpdateBool)
{
	zend_bool *p;
	char *base;

	base = (char *)ts_resource(*((int *)mh_arg2));

	p = (zend_bool *)(base + (size_t)mh_arg1);

	if (strncasecmp("on", new_value, sizeof("on"))) *p = (zend_bool)atoi(new_value);
	else *p = (zend_bool)1;

	return SUCCESS;
}

// copied from zend_ini.c
ZEND_API ZEND_INI_MH(OnUpdateLong) /* {{{ */
{
	long *p;
#ifndef ZTS
	char *base = (char *) mh_arg2;
#else
	char *base;

	base = (char *) ts_resource(*((int *) mh_arg2));
#endif

	p = (long *) (base+(size_t) mh_arg1);

	*p = zend_atol(new_value, new_value_length);
	return SUCCESS;
}
/* }}} */

// copied from zend_ini.c
ZEND_API ZEND_INI_MH(OnUpdateLongGEZero) /* {{{ */
{
	long *p, tmp;
#ifndef ZTS
	char *base = (char *) mh_arg2;
#else
	char *base;

	base = (char *) ts_resource(*((int *) mh_arg2));
#endif

	tmp = zend_atol(new_value, new_value_length);
	if (tmp < 0) {
		return FAILURE;
	}

	p = (long *) (base+(size_t) mh_arg1);
	*p = tmp;

	return SUCCESS;
}
/* }}} */

// copied from zend_ini.c, slightly modified and beautified
ZEND_API ZEND_INI_MH(OnUpdateInt)
{
	long *p;
	char *base;

	base = (char *)ts_resource(*((int *)mh_arg2));

	p = (long *)(base + (size_t)mh_arg1);

	*p = zend_atoi(new_value, new_value_length);
	return SUCCESS;
}

// copied from zend_ini.c, slightly modified and beautified
ZEND_API ZEND_INI_MH(OnUpdateReal)
{
	double *p;
	char *base;

	base = (char *)ts_resource(*((int *)mh_arg2));

	p = (double *)(base + (size_t) mh_arg1);

	*p = strtod(new_value, NULL);
	return SUCCESS;
}

// copied from zend_ini.c, slightly modified and beautified
ZEND_API ZEND_INI_MH(OnUpdateString)
{
	char **p;
	char *base;

	base = (char *)ts_resource(*((int *)mh_arg2));

	p = (char **)(base + (size_t)mh_arg1);

	*p = new_value;
	return SUCCESS;
}

// copied from zend_ini.c, slightly modified and beautified
ZEND_API ZEND_INI_MH(OnUpdateStringUnempty)
{
	char **p;
	char *base;

	base = (char *)ts_resource(*((int *)mh_arg2));

	if (new_value && !new_value[0]) return FAILURE;

	p = (char **)(base + (size_t)mh_arg1);

	*p = new_value;
	return SUCCESS;
}

/******************************************************************************/

#pragma managed

// rewritten
ZEND_API int zend_get_configuration_directive(char *name, uint name_length, zval *contents)
{
#ifdef DEBUG
	Debug::WriteLine("PHP4TS", String::Format("zend_get_configuration_directive: {0}", gcnew String(name, 0, name_length)));
#endif

	Module ^module = Module::GetCurrentModule();
	if (module != nullptr)
	{
		IniEntry ^entry = module->GetConfigEntry(gcnew String(name, 0, name_length - 1));

		if (entry != nullptr)
		{
			*contents = *entry->GetNativeValue();
			return SUCCESS;
		}
	}
	return FAILURE;
}

// rewritten
ZEND_API int cfg_get_long(char *varname, long *result)
{
	Module ^module = Module::GetCurrentModule();
	if (module != nullptr)
	{
		IniEntry ^entry = module->GetConfigEntry(gcnew String(varname));

		if (entry != nullptr)
		{
			try
			{
				*result = Int32::Parse(entry->MngValue);
			}
			catch (Exception ^)
			{
				*result = 0;
			}
			return SUCCESS;
		}
	}
	*result = 0;	
	return FAILURE;
}

// rewritten
ZEND_API int cfg_get_double(char *varname, double *result)
{
	Module ^module = Module::GetCurrentModule();
	if (module != nullptr)
	{
		IniEntry ^entry = module->GetConfigEntry(gcnew String(varname));

		if (entry != nullptr)
		{
			*result = Double::Parse(entry->MngValue);
			return SUCCESS;
		}
	}
	*result = 0.0;
	return FAILURE;
}

private ref class cfg_get_string_MutexHolder
{
public:
	static Object ^Mutex = gcnew Object();
};

// rewritten
ZEND_API int cfg_get_string(char *varname, char **result)
{
	static volatile char *extension_dir = NULL;

	Module ^module = Module::GetCurrentModule();
	if (module != nullptr)
	{
		IniEntry ^entry = module->GetConfigEntry(gcnew String(varname));

		if (entry != nullptr)
		{
			*result = entry->GetNativeValue()->value.str.val;
			return SUCCESS;
		}
		else if (strcmp(varname, "extension_dir") == 0)
		{
			// some extensions ask for this configuration option (GTK)
			if (extension_dir == NULL)
			{
				Monitor::Enter(cfg_get_string_MutexHolder::Mutex);
				try
				{
					if (extension_dir == NULL)
					{
						extension_dir = PhpMarshaler::MarshalManagedStringToNativeStringPersistent(
							PHP::Core::Configuration::Application->Paths->ExtNatives);
					}
				}
				finally
				{
					Monitor::Exit(cfg_get_string_MutexHolder::Mutex);
				}
			}
			*result = const_cast<char *>(extension_dir);
			return SUCCESS;
		}
	}
	*result = NULL;
	return FAILURE;
}
