//
// ExtSupport.PHP4 - substitute for php4ts.dll
//
// PhpInfo.h
// - contains declarations of phpinfo() related exported functions
//

#pragma once

#include "stdafx.h"
#include "ExtSupport.h"

#ifdef __cplusplus
extern "C"
{
#endif

ZEND_API char *php_info_html_esc(char *string TSRMLS_DC);
ZEND_API void php_info_print_box_end(void);
ZEND_API void php_info_print_box_start(int bg);
ZEND_API void php_info_print_css(void);
ZEND_API void php_info_print_hr(void);
ZEND_API void php_info_print_style(void);
ZEND_API void php_info_print_table_colspan_header(int num_cols, char *header);
ZEND_API void php_info_print_table_end(void);
ZEND_API void php_info_print_table_header(int num_cols, ...);
ZEND_API void php_info_print_table_row(int num_cols, ...);
ZEND_API void php_info_print_table_start(void);

ZEND_API void php_print_info_htmlhead(TSRMLS_D);
ZEND_API void php_print_info(int flag TSRMLS_DC);
ZEND_API void php_print_credits(int flag);

#ifdef __cplusplus
}
#endif
