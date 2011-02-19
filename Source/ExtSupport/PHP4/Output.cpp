//
// ExtSupport.PHP4 - substitute for php4ts.dll
//
// Output.cpp
// - contains definitions of output related functions
//

#include "stdafx.h"
#include "Output.h"
#include "TsrmLs.h"
#include "Errors.h"
#include "Spprintf.h"
#include "Memory.h"
#include "Variables.h"
#include "Strings.h"
#include "Request.h"

using namespace PHP::ExtManager;

static int managed_zend_write(const char *str, uint str_length)
{
	if ( str_length == 0 )
		return 0;

	Request ^request = Request::GetCurrentRequest(false, false);
	if (request != nullptr && request->PhpInfoBuilder != nullptr)
	{
		request->PhpInfoBuilder->Append(gcnew String(str, 0, str_length,
			Request::AppConf->Globalization->PageEncoding));
	}
	else
	{
		IO::Stream ^out = PHP::Core::ScriptContext::CurrentContext->OutputStream;
		if (out != nullptr)
		{
			array<unsigned char> ^bytes = gcnew array<unsigned char>(str_length);
			Marshal::Copy(IntPtr(const_cast<char *>(str)), bytes, 0, str_length);

			out->Write(bytes, 0, str_length);
		}
	}
	return str_length;
}

ZEND_API int php_body_write(const char *str, uint str_length TSRMLS_DC)
{
	return zend_write(str, str_length);	
}

#pragma unmanaged

// copied from main.c and beautified
ZEND_API int php_printf(const char *format, ...)
{
	va_list args;
	int ret;
	char *buffer;
	int size;
	TSRMLS_FETCH();

	va_start(args, format);
	size = vspprintf(&buffer, 0, format, args);
	if (buffer)
	{
		ret = PHPWRITE(buffer, size);
		efree(buffer);
	}
	else
	{
		php_error_docref(NULL TSRMLS_CC, E_ERROR, "Out of memory");
		ret = 0;
	}
	va_end(args);
	
	return ret;
}

#pragma managed

zend_write_func_t zend_write = managed_zend_write;
int (*zend_printf)(const char *format, ...) = php_printf;

// copied from zend_variables.c
ZEND_API int zend_print_variable(zval *var) 
{
	return zend_print_zval(var, 0);
}

// copied from zend.c
ZEND_API int zend_print_zval(zval *expr, int indent)
{
	return zend_print_zval_ex(zend_write, expr, indent);
}

// copied from zend.c and beautified
ZEND_API int zend_print_zval_ex(zend_write_func_t write_func, zval *expr, int indent)
{
	zval expr_copy;
	int use_copy;

	zend_make_printable_zval(expr, &expr_copy, &use_copy);
	if (use_copy) expr = &expr_copy;

	if (expr->value.str.len == 0)
	{
		/* optimize away empty strings */
		if (use_copy) zval_dtor(expr);
		return 0;
	}

	write_func(expr->value.str.val, expr->value.str.len);
	if (use_copy) zval_dtor(expr);
	
	return expr->value.str.len;
}

// copied from zend.c
ZEND_API void zend_print_zval_r(zval *expr, int indent TSRMLS_DC_PHP5)
{
	zend_print_zval_r_ex(zend_write, expr, indent TSRMLS_CC_PHP5);
}

// copied from zend.c and beautied
#ifdef PHP4TS
static void print_hash(HashTable *ht, int indent)
{
	zval **tmp;
	char *string_key;
	HashPosition iterator;
	ulong num_key;
	uint str_len;
	int i;

	for (i = 0; i < indent; i++) ZEND_PUTS(" ");

	ZEND_PUTS("(\n");
	indent += PRINT_ZVAL_INDENT;
	zend_hash_internal_pointer_reset_ex(ht, &iterator);
	while (zend_hash_get_current_data_ex(ht, (void **)&tmp, &iterator) == SUCCESS)
	{
		for (i = 0; i < indent; i++) ZEND_PUTS(" ");

		ZEND_PUTS("[");
		switch (zend_hash_get_current_key_ex(ht, &string_key, &str_len, &num_key, 0, &iterator))
		{
			case HASH_KEY_IS_STRING:
				ZEND_WRITE(string_key, str_len-1);
				break;

			case HASH_KEY_IS_LONG:
				zend_printf("%ld", num_key);
				break;
		}
		ZEND_PUTS("] => ");
		zend_print_zval_r(*tmp, indent + PRINT_ZVAL_INDENT TSRMLS_CC_PHP5);
		ZEND_PUTS("\n");
		zend_hash_move_forward_ex(ht, &iterator);
	}
	indent -= PRINT_ZVAL_INDENT;
	for (i = 0; i < indent; i++) ZEND_PUTS(" ");

	ZEND_PUTS(")\n");
}
#elif defined(PHP5TS)
static void print_hash(zend_write_func_t write_func, HashTable *ht, int indent, zend_bool is_object TSRMLS_DC)
{
	zval **tmp;
	char *string_key;
	HashPosition iterator;
	ulong num_key;
	uint str_len;
	int i;

	for (i=0; i<indent; i++) {
		ZEND_PUTS_EX(" ");
	}
	ZEND_PUTS_EX("(\n");
	indent += PRINT_ZVAL_INDENT;
	zend_hash_internal_pointer_reset_ex(ht, &iterator);
	while (zend_hash_get_current_data_ex(ht, (void **) &tmp, &iterator) == SUCCESS) {
		for (i=0; i<indent; i++) {
			ZEND_PUTS_EX(" ");
		}
		ZEND_PUTS_EX("[");
		switch (zend_hash_get_current_key_ex(ht, &string_key, &str_len, &num_key, 0, &iterator)) {
			case HASH_KEY_IS_STRING:
				if (is_object) {
					char *prop_name, *class_name;

					int mangled = zend_unmangle_property_name(string_key, str_len-1, &class_name, &prop_name);
					ZEND_PUTS_EX(prop_name);
					if (class_name && mangled == SUCCESS) {
						if (class_name[0]=='*') {
							ZEND_PUTS_EX(":protected");
						} else {
							ZEND_PUTS_EX(":private");
						}
					}
				} else {
					ZEND_WRITE_EX(string_key, str_len-1);
				}
				break;
			case HASH_KEY_IS_LONG:
				{
					char key[25];
					snprintf(key, sizeof(key), "%ld", num_key);
					ZEND_PUTS_EX(key);
				}
				break;
		}
		ZEND_PUTS_EX("] => ");
		zend_print_zval_r_ex(write_func, *tmp, indent+PRINT_ZVAL_INDENT TSRMLS_CC);
		ZEND_PUTS_EX("\n");
		zend_hash_move_forward_ex(ht, &iterator);
	}
	indent -= PRINT_ZVAL_INDENT;
	for (i=0; i<indent; i++) {
		ZEND_PUTS_EX(" ");
	}
	ZEND_PUTS_EX(")\n");
}

static void print_flat_hash(HashTable *ht TSRMLS_DC)
{
    zval **tmp;
    char *string_key;
    HashPosition iterator;
    ulong num_key;
    uint str_len;
    int i = 0;

    zend_hash_internal_pointer_reset_ex(ht, &iterator);
    while (zend_hash_get_current_data_ex(ht, (void **) &tmp, &iterator) == SUCCESS) {
	if (i++ > 0) {
	    ZEND_PUTS(",");
	}
	ZEND_PUTS("[");
	switch (zend_hash_get_current_key_ex(ht, &string_key, &str_len, &num_key, 0, &iterator)) {
	    case HASH_KEY_IS_STRING:
		ZEND_PUTS(string_key);
		break;
	    case HASH_KEY_IS_LONG:
		zend_printf("%ld", num_key);
		break;
	}
	ZEND_PUTS("] => ");
	zend_print_flat_zval_r(*tmp TSRMLS_CC);
	zend_hash_move_forward_ex(ht, &iterator);
    }
}

ZEND_API void zend_print_flat_zval_r(zval *expr TSRMLS_DC)
{
    switch (expr->type) {
	case IS_ARRAY:
	    ZEND_PUTS("Array (");
	    if (++expr->value.ht->nApplyCount>1) {
		ZEND_PUTS(" *RECURSION*");
		expr->value.ht->nApplyCount--;
		return;
	    }
	    print_flat_hash(expr->value.ht TSRMLS_CC);
	    ZEND_PUTS(")");
	    expr->value.ht->nApplyCount--;
	    break;
	case IS_OBJECT:
	    {
			HashTable *properties = NULL;
			char *class_name = NULL;
			zend_uint clen;
			
			if (Z_OBJ_HANDLER_P(expr, get_class_name)) {
				Z_OBJ_HANDLER_P(expr, get_class_name)(expr, &class_name, &clen, 0 TSRMLS_CC);
			}
			zend_printf("%s Object (", class_name?class_name:"Unknown Class");
			if (class_name) {
				efree(class_name);
			}
			if (Z_OBJ_HANDLER_P(expr, get_properties)) {
				properties = Z_OBJPROP_P(expr);
			}
			if (properties) {
				if (++properties->nApplyCount>1) {
					ZEND_PUTS(" *RECURSION*");
					properties->nApplyCount--;
					return;
				}
				print_flat_hash(properties TSRMLS_CC);
				properties->nApplyCount--;
			}
			ZEND_PUTS(")");
			break;
	    }
	default:
	    zend_print_variable(expr);
	    break;
    }
}

//Copied from zend_compile.c
static inline int zend_strnlen(const char* s, int maxlen)
{
	int len = 0;
	while (*s++ && maxlen--) len++;
	return len;
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
#endif

// copied from zend.c and beautified
ZEND_API void zend_print_zval_r_ex(zend_write_func_t write_func, zval *expr, int indent TSRMLS_DC_PHP5) 
{
	switch (expr->type)
	{
		case IS_ARRAY:
			ZEND_PUTS("Array\n");
			if (++expr->value.ht->nApplyCount > 1)
			{
				ZEND_PUTS(" *RECURSION*");
				expr->value.ht->nApplyCount--;
				return;
			}
#ifdef PHP4TS
			print_hash(expr->value.ht, indent TSRMLS_CC_PHP5);
#elif defined(PHP5TS)
			print_hash(write_func, expr->value.ht, indent, 0 TSRMLS_CC);
#endif
			expr->value.ht->nApplyCount--;
			break;

		case IS_OBJECT:
#ifdef PHP4TS
			{
				zend_object *object = Z_OBJ_P(expr);

				if (++object->properties->nApplyCount > 1)
				{
					ZEND_PUTS(" *RECURSION*");
					object->properties->nApplyCount--;
					return;
				}
				zend_printf("%s Object\n", object->ce->name);
				print_hash(object->properties, indent);
				object->properties->nApplyCount--;
				break;
			}
#elif defined PHP5TS
			{
				HashTable *properties = NULL;
				char *class_name = NULL;
				zend_uint clen;
				
				if (Z_OBJ_HANDLER_P(expr, get_class_name)) {
					Z_OBJ_HANDLER_P(expr, get_class_name)(expr, &class_name, &clen, 0 TSRMLS_CC);
				}
				if (class_name) {
					ZEND_PUTS_EX(class_name);
				} else {
					ZEND_PUTS_EX("Unknown Class");
				}
				ZEND_PUTS_EX(" Object\n");
				if (class_name) {
					efree(class_name);
				}
				if (Z_OBJ_HANDLER_P(expr, get_properties)) {
					properties = Z_OBJPROP_P(expr);
				}
				if (properties) {
					if (++properties->nApplyCount>1) {
						ZEND_PUTS_EX(" *RECURSION*");
						properties->nApplyCount--;
						return;
					}
					print_hash(write_func, properties, indent, 1 TSRMLS_CC);
					properties->nApplyCount--;
				}
				break;
			}
#endif

		default:
			zend_print_variable(expr);
			break;
	}
}

// copied from main.c
ZEND_API int php_write(void *buf, uint size TSRMLS_DC)
{
	return PHPWRITE((char *)buf, size);
}

#pragma unmanaged

// copied from var.c and beautified
static int php_array_element_dump(zval **zv, int num_args, va_list args, zend_hash_key *hash_key)
{
	int level;
	TSRMLS_FETCH();

	level = va_arg(args, int);

	if (hash_key->nKeyLength == 0)
	{
		/* numeric key */
		php_printf("%*c[%ld]=>\n", level + 1, ' ', hash_key->h);
	}
	else
	{
		/* string key */
		php_printf("%*c[\"%s\"]=>\n", level + 1, ' ', hash_key->arKey);
	}
	php_var_dump(zv, level + 2 TSRMLS_CC);
	return 0;
}

// copied from var.c
static int php_object_property_dump(zval **zv, int num_args, va_list args, zend_hash_key *hash_key)
{
	int level;
	char *prop_name, *class_name;
	TSRMLS_FETCH();

	level = va_arg(args, int);

	if (hash_key->nKeyLength ==0 ) { /* numeric key */
		php_printf("%*c[%ld]=>\n", level + 1, ' ', hash_key->h);
	} else { /* string key */
		int unmangle = zend_unmangle_property_name(hash_key->arKey, hash_key->nKeyLength-1, &class_name, &prop_name);
		if (class_name && unmangle == SUCCESS) {
			php_printf("%*c[\"%s", level + 1, ' ', prop_name);
			if (class_name[0]=='*') {
				ZEND_PUTS(":protected");
			} else {
				ZEND_PUTS(":private");
			}
		} else {
			php_printf("%*c[\"", level + 1, ' ');
			PHPWRITE(hash_key->arKey, hash_key->nKeyLength - 1);
#ifdef ANDREY_0
			ZEND_PUTS(":public");
#endif
		}
		ZEND_PUTS("\"]=>\n");
	}
	php_var_dump(zv, level + 2 TSRMLS_CC);
	return 0;
}

#pragma managed

// copied from var.c and beautified
ZEND_API void php_var_dump(zval **struc, int level TSRMLS_DC)
{
	HashTable *myht = NULL;

	if (level > 1) php_printf("%*c", level - 1, ' ');

	switch (Z_TYPE_PP(struc))
	{
		case IS_BOOL:
			php_printf("%sbool(%s)\n", COMMON, Z_LVAL_PP(struc) ? "true" : "false");
			break;

		case IS_NULL:
			php_printf("%sNULL\n", COMMON);
			break;

		case IS_LONG:
			php_printf("%sint(%ld)\n", COMMON, Z_LVAL_PP(struc));
			break;

		case IS_DOUBLE:
			php_printf("%sfloat(%.*G)\n", COMMON, (int) EG(precision), Z_DVAL_PP(struc));
			break;

		case IS_STRING:
			php_printf("%sstring(%d) \"", COMMON, Z_STRLEN_PP(struc));
			PHPWRITE(Z_STRVAL_PP(struc), Z_STRLEN_PP(struc));
			PUTS("\"\n");
			break;

		case IS_ARRAY:
			myht = Z_ARRVAL_PP(struc);
			if (myht->nApplyCount > 1)
			{
				PUTS("*RECURSION*\n");
				return;
			}
			php_printf("%sarray(%d) {\n", COMMON, zend_hash_num_elements(myht));
			goto head_done;

		case IS_OBJECT:
		{
			myht = Z_OBJPROP_PP(struc);
			if (myht->nApplyCount > 1)
			{
				PUTS("*RECURSION*\n");
				return;
			}
			apply_func_args_t php_element_dump_func;
#ifdef PHP4TS
			zend_object *object = NULL;
			object = Z_OBJ_PP(struc);
			
			php_printf("%sobject(%s)(%d) {\n", COMMON, Z_OBJCE_PP(struc)->name, zend_hash_num_elements(myht));
			php_element_dump_func = (apply_func_args_t)php_array_element_dump;
#else
			char *class_name;
			zend_uint class_name_len = 0;
			Z_OBJ_HANDLER(**struc, get_class_name)(*struc, &class_name, &class_name_len, 0 TSRMLS_CC);
			php_printf("%sobject(%s)#%d (%d) {\n", COMMON, class_name, Z_OBJ_HANDLE_PP(struc), myht ? zend_hash_num_elements(myht) : 0);
			efree(class_name);
			php_element_dump_func = (apply_func_args_t)php_object_property_dump;
#endif
	head_done:
			zend_hash_apply_with_arguments(myht, php_element_dump_func, 1, level);
			if (level > 1) php_printf("%*c", level-1, ' ');
				PUTS("}\n");
		}
			break;

		case IS_RESOURCE:
			{
				char *type_name;

				type_name = zend_rsrc_list_get_rsrc_type(Z_LVAL_PP(struc) TSRMLS_CC);
				php_printf("%sresource(%ld) of type (%s)\n", COMMON, Z_LVAL_PP(struc), type_name ? type_name : "Unknown");
				break;
			}

		default:
			php_printf("%sUNKNOWN:0\n", COMMON);
			break;
	}
}

// copied from zend_highlight.c and beautified
ZEND_API void zend_html_putc(char c)
{
	switch (c)
	{
		case '\n':
			ZEND_PUTS("<br />");
			break;

		case '<':
			ZEND_PUTS("&lt;");
			break;

		case '>':
			ZEND_PUTS("&gt;");
			break;

		case '&':
			ZEND_PUTS("&amp;");
			break;

		case ' ':
			ZEND_PUTS("&nbsp;");
			break;

		case '\t':
			ZEND_PUTS("&nbsp;&nbsp;&nbsp;&nbsp;");
			break;

		default:
			ZEND_PUTC(c);
			break;
	}
}

// copied from zend_highlight.c and beautified
ZEND_API void zend_html_puts(const char *s, uint len TSRMLS_DC)
{
	const char *ptr=s, *end=s+len;

	while (ptr < end)
	{
		if (*ptr==' ')
		{
			/* Series of spaces should be displayed as &nbsp;'s
			 * whereas single spaces should be displayed as a space
			 */
			if ((ptr + 1) < end && *(ptr + 1)==' ')
			{
				do
				{
					zend_html_putc(*ptr);
				}
				while ((++ptr < end) && (*ptr==' '));
			}
			else
			{
				ZEND_PUTC(*ptr);
				ptr++;
			}
		}
		else zend_html_putc(*ptr++);
	}
}

// copied from main.c
ZEND_API void php_html_puts(const char *str, uint size TSRMLS_DC)
{
	zend_html_puts(str, size TSRMLS_CC);
}

#pragma unmanaged

// copied from var.c and beautified
static int php_array_element_export(zval **zv, int num_args, va_list args, zend_hash_key *hash_key)
{
	int level;
	TSRMLS_FETCH();

	level = va_arg(args, int);

	if (hash_key->nKeyLength == 0)
	{
		/* numeric key */
		php_printf("%*c%ld => ", level + 1, ' ', hash_key->h);
	}
	else
	{
		/* string key */
		char *key;
		int key_len;
		key = php_addcslashes(hash_key->arKey, hash_key->nKeyLength - 1, &key_len, 0, "'\\", 2 TSRMLS_CC);
		php_printf("%*c'", level + 1, ' ');
		PHPWRITE(key, key_len);
		php_printf("' => ");
		efree(key);
	}
	php_var_export(zv, level + 2 TSRMLS_CC);
	PUTS(",\n");
	return 0;
}


// copied from var.c and beautified
static int php_object_element_export(zval **zv, int num_args, va_list args, zend_hash_key *hash_key)
{
	int level;
	TSRMLS_FETCH();

	level = va_arg(args, int);

	if (hash_key->nKeyLength != 0)
	{
		php_printf("%*cvar $%s = ", level + 1, ' ', hash_key->arKey);
		php_var_export(zv, level + 2 TSRMLS_CC);
		PUTS (";\n");
	}
	return 0;
}

#pragma managed

// copied from var.c and beautified
ZEND_API void php_var_export(zval **struc, int level TSRMLS_DC)
{
	HashTable *myht;
	char* tmp_str;
	int tmp_len;

	switch (Z_TYPE_PP(struc))
	{
		case IS_BOOL:
			php_printf("%s", Z_LVAL_PP(struc) ? "true" : "false");
			break;

		case IS_NULL:
			php_printf("NULL");
			break;

		case IS_LONG:
			php_printf("%ld", Z_LVAL_PP(struc));
			break;

		case IS_DOUBLE:
			php_printf("%.*G", (int) EG(precision), Z_DVAL_PP(struc));
			break;

		case IS_STRING:
			tmp_str = php_addcslashes(Z_STRVAL_PP(struc), Z_STRLEN_PP(struc), &tmp_len, 0, "'\\", 2 TSRMLS_CC);
			PUTS ("'");
			PHPWRITE(tmp_str, tmp_len);
			PUTS ("'");
			efree (tmp_str);
			break;

		case IS_ARRAY:
			myht = Z_ARRVAL_PP(struc);
			if (level > 1) php_printf("\n%*c", level - 1, ' ');
			PUTS ("array (\n");
			zend_hash_apply_with_arguments(myht, (apply_func_args_t)php_array_element_export, 1, level);
			if (level > 1) php_printf("%*c", level - 1, ' ');
			PUTS(")");
			break;

		case IS_OBJECT:
			myht = Z_OBJPROP_PP(struc);
			if (level > 1) php_printf("\n%*c", level - 1, ' ');
			php_printf("class %s {\n", Z_OBJCE_PP(struc)->name);
			zend_hash_apply_with_arguments(myht, (apply_func_args_t)php_object_element_export, 1, level);
			if (level > 1) php_printf("%*c", level - 1, ' ');
			PUTS("}");
			break;

		default:
			PUTS ("NULL");
			break;
	}
}

#define Z_REFCOUNT_PP(a) ((*a)->refcount)

#pragma unmanaged

// copied from var.c and beautified
static int zval_array_element_dump(zval **zv, int num_args, va_list args, zend_hash_key *hash_key)
{
	int level;
	TSRMLS_FETCH();

	level = va_arg(args, int);

	if (hash_key->nKeyLength == 0)
	{ /* numeric key */
		php_printf("%*c[%ld]=>\n", level + 1, ' ', hash_key->h);
	}
	else
	{ /* string key */
		php_printf("%*c[\"", level + 1, ' ');
		PHPWRITE(hash_key->arKey, hash_key->nKeyLength - 1);
		php_printf("\"]=>\n");
	}
	php_debug_zval_dump(zv, level + 2 TSRMLS_CC);
	return 0;
}

#pragma managed

// copied from var.c and beautified
ZEND_API void php_debug_zval_dump(zval **struc, int level TSRMLS_DC)
{
	HashTable *myht = NULL;

	if (level > 1) php_printf("%*c", level - 1, ' ');

	switch (Z_TYPE_PP(struc))
	{
		case IS_BOOL:
			php_printf("%sbool(%s) refcount(%u)\n", COMMON, Z_LVAL_PP(struc) ? "true" : "false", Z_REFCOUNT_PP(struc));
			break;

		case IS_NULL:
			php_printf("%sNULL refcount(%u)\n", COMMON, Z_REFCOUNT_PP(struc));
			break;

		case IS_LONG:
			php_printf("%slong(%ld) refcount(%u)\n", COMMON, Z_LVAL_PP(struc), Z_REFCOUNT_PP(struc));
			break;

		case IS_DOUBLE:
			php_printf("%sdouble(%.*G) refcount(%u)\n", COMMON, (int)EG(precision), Z_DVAL_PP(struc), Z_REFCOUNT_PP(struc));
			break;

		case IS_STRING:
			php_printf("%sstring(%d) \"", COMMON, Z_STRLEN_PP(struc));
			PHPWRITE(Z_STRVAL_PP(struc), Z_STRLEN_PP(struc));
			php_printf("\" refcount(%u)\n", Z_REFCOUNT_PP(struc));
			break;

		case IS_ARRAY:
			myht = Z_ARRVAL_PP(struc);
			php_printf("%sarray(%d) refcount(%u){\n", COMMON, zend_hash_num_elements(myht), Z_REFCOUNT_PP(struc));
			goto head_done;

		case IS_OBJECT:
			myht = Z_OBJPROP_PP(struc);
			php_printf("%sobject(%s)(%d) refcount(%u){\n", COMMON, Z_OBJCE_PP(struc)->name, zend_hash_num_elements(myht),
				Z_REFCOUNT_PP(struc));
	head_done:
			zend_hash_apply_with_arguments(myht, (apply_func_args_t)zval_array_element_dump, 1, level);
			if (level > 1) php_printf("%*c", level-1, ' ');
			PUTS("}\n");
			break;

		case IS_RESOURCE:
		{
			char *type_name;

			type_name = zend_rsrc_list_get_rsrc_type(Z_LVAL_PP(struc) TSRMLS_CC);
			php_printf("%sresource(%ld) of type (%s) refcount(%u)\n", COMMON, Z_LVAL_PP(struc),
				type_name ? type_name : "Unknown", Z_REFCOUNT_PP(struc));
			break;
		}

		default:
			php_printf("%sUNKNOWN:0\n", COMMON);
			break;
	}
}
