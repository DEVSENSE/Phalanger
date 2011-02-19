//
// ExtSupport.PHP4 - substitute for php4ts.dll
//
// Errors.h
// - contains declarations of error handling related exported functions
//

#pragma once

#include "stdafx.h"
#include "ExtSupport.h"
#include <stdarg.h>

#define zend_bailout()		_zend_bailout(__FILE__, __LINE__)

#ifdef __cplusplus
extern "C"
{
#endif

extern ZEND_API void (*zend_error_cb)(int type, const char *error_filename, const uint error_lineno,
									  const char *format, va_list args);

ZEND_API void zend_error(int type, const char *format, ...);
ZEND_API void php_verror(const char *docref, const char *params, int type, const char *format, va_list args TSRMLS_DC);

ZEND_API void php_error_docref0(const char *docref TSRMLS_DC, int type, const char *format, ...);
ZEND_API void php_error_docref1(const char *docref TSRMLS_DC, const char *param1, int type, 
								const char *format, ...);
ZEND_API void php_error_docref2(const char *docref TSRMLS_DC, const char *param1, const char *param2, 
								int type, const char *format, ...);
ZEND_API void php_win32_docref2_from_error(DWORD error, const char *param1, const char *param2 TSRMLS_DC);
ZEND_API void zend_wrong_param_count(TSRMLS_D);

ZEND_API void _zend_bailout(char *filename, uint lineno);

ZEND_API void zend_output_debug_string(zend_bool trigger_break, char *format, ...);

ZEND_API int _php_error_log(int opt_err, char *message, char *opt, char *headers TSRMLS_DC);
ZEND_API void php_log_err(char *log_message TSRMLS_DC);
ZEND_API char *php_win_err(int error);

#ifdef __cplusplus
}
#endif
