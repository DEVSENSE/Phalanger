//
// ExtSupport.PHP4 - substitute for php4ts.dll
//
// Math.h
// - contains declarations of math exported functions
//

#pragma once

#include "stdafx.h"
#include "ExtSupport.h"

static inline double _huge_val()
{
	static __int64 i64 = 9218868437227405312L;
	static double *pd = (double *)&i64;

	return *pd;
}

#undef HUGE_VAL
#define HUGE_VAL _huge_val()

#ifdef __cplusplus
extern "C"
{
#endif

ZEND_API long _php_math_basetolong(zval *arg, int base);
ZEND_API int _php_math_basetozval(zval *arg, int base, zval *ret);
ZEND_API char *_php_math_longtobase(zval *arg, int base);
ZEND_API char *_php_math_zvaltobase(zval *arg, int base TSRMLS_DC);
ZEND_API char *_php_math_number_format(double d, int dec, char dec_point, char thousand_sep);

ZEND_API double php_get_inf(void);
ZEND_API double php_get_nan(void);

#ifdef __cplusplus
}
#endif
