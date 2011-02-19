//
// ExtSupport.PHP4 - substitute for php4ts.dll
//
// Constants.h
// - contains declarations of constants related exported functions
//

#pragma once

#include "stdafx.h"
#include "ExtSupport.h"

BEGIN_EXTERN_C()
#ifdef PHP5TS
int zend_startup_constants(TSRMLS_D);
#endif

void free_zend_constant(zend_constant *c);

ZEND_API void zend_register_long_constant(char *name, uint name_len, long lval, int flags, int module_number TSRMLS_DC);
ZEND_API void zend_register_double_constant(char *name, uint name_len, double dval, int flags, int module_number TSRMLS_DC);
ZEND_API void zend_register_stringl_constant(char *name, uint name_len, char *strval, uint strlen, int flags, 
											 int module_number TSRMLS_DC);
ZEND_API void zend_register_string_constant(char *name, uint name_len, char *strval, int flags, int module_number TSRMLS_DC);
ZEND_API int zend_get_constant(char *name, uint name_len, zval *result TSRMLS_DC);
ZEND_API int zend_register_constant(zend_constant *c TSRMLS_DC);

#define ZEND_CONSTANT_DTOR (void (*)(void *)) free_zend_constant
END_EXTERN_C()