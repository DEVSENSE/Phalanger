//
// ExtSupport.PHP4 - substitute for php4ts.dll
//
// Output.h
// - contains declarations of output related exported symbols
//

#pragma once

#include "stdafx.h"
#include "ExtSupport.h"

#define PRINT_ZVAL_INDENT 4
#define PHPWRITE(str, str_len)		php_body_write((str), (str_len) TSRMLS_CC)
#define COMMON						((*struc)->is_ref ? "&" : "")
#define STR_PRINT(str)				((str) ? (str) : "")

typedef int (*zend_write_func_t)(const char *str, uint str_length);

#ifdef __cplusplus
extern "C"
{
#endif

#ifdef PHP5TS
	#define TSRMLS_DC_PHP5	TSRMLS_DC
	#define TSRMLS_CC_PHP5	TSRMLS_CC
#else
	#define TSRMLS_DC_PHP5
	#define TSRMLS_CC_PHP5
#endif

extern ZEND_API zend_write_func_t zend_write;
extern ZEND_API int (*zend_printf)(const char *format, ...);

ZEND_API int php_body_write(const char *str, uint str_length TSRMLS_DC);
ZEND_API int php_printf(const char *format, ...);

ZEND_API int zend_print_variable(zval *var);
ZEND_API int zend_print_zval(zval *expr, int indent);
ZEND_API int zend_print_zval_ex(zend_write_func_t write_func, zval *expr, int indent);
ZEND_API void zend_print_zval_r(zval *expr, int indent TSRMLS_DC_PHP5);
ZEND_API void zend_print_zval_r_ex(zend_write_func_t write_func, zval *expr, int indent TSRMLS_DC_PHP5);

#ifdef PHP5TS
ZEND_API void zend_print_flat_zval_r(zval *expr TSRMLS_DC);
#endif

ZEND_API int php_write(void *buf, uint size TSRMLS_DC);
ZEND_API void php_var_dump(zval **struc, int level TSRMLS_DC);

ZEND_API void zend_html_putc(char c);
ZEND_API void zend_html_puts(const char *s, uint len TSRMLS_DC);
ZEND_API void php_html_puts(const char *str, uint size TSRMLS_DC);

ZEND_API void php_var_export(zval **struc, int level TSRMLS_DC);
ZEND_API void php_debug_zval_dump(zval **struc, int level TSRMLS_DC);

ZEND_API int zend_unmangle_property_name(char *mangled_property, int mangled_property_len, char **prop_name, char **class_name);

#ifdef __cplusplus
}
#endif
