//
// ExtSupport.PHP4 - substitute for php4ts.dll
//
// Parameters.h
// - contains declarations of parameter handling exported functions
// - input parameters are not passed to external functions directly; functions
//   use zend_parse_parameters and zend_parse_parameters_ex to retrieve them
//

#pragma once

#include "stdafx.h"
#include "ExtSupport.h"

#define zend_get_parameters_array(ht, param_count, argument_array)			\
	_zend_get_parameters_array(ht, param_count, argument_array TSRMLS_CC)
#define zend_get_parameters_array_ex(param_count, argument_array)			\
	_zend_get_parameters_array_ex(param_count, argument_array TSRMLS_CC)

#ifdef __cplusplus
extern "C"
{
#endif

ZEND_API extern unsigned char first_arg_force_ref[];
ZEND_API extern unsigned char second_arg_force_ref[];
ZEND_API extern unsigned char third_arg_force_ref[];

ZEND_API int zend_parse_parameters(int num_args TSRMLS_DC, char *type_spec, ...);
ZEND_API int zend_parse_parameters_ex(int flags, int num_args TSRMLS_DC, char *type_spec, ...);
ZEND_API char *get_active_function_name(TSRMLS_D);

#ifdef PHP5TS
ZEND_API char *get_active_class_name(char **space TSRMLS_DC);
ZEND_API int zend_lookup_class(char *name, int name_length, zend_class_entry ***ce TSRMLS_DC);
#endif

ZEND_API int zend_get_parameters(int ht, int param_count, ...);
ZEND_API int _zend_get_parameters_array(int ht, int param_count, zval **argument_array TSRMLS_DC);
ZEND_API int zend_get_parameters_ex(int param_count, ...);
ZEND_API int _zend_get_parameters_array_ex(int param_count, zval ***argument_array TSRMLS_DC);

#ifdef __cplusplus
}
#endif
