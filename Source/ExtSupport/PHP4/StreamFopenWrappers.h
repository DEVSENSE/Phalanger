//
// ExtSupport.PHP4 - substitute for php4ts.dll
//
// StreamFopenWrappers.h
// - slightly modified fopen_wrappers.h, originally PHP source file
//

#pragma once

#include "stdafx.h"
#include "ExtSupport.h"

/*
   +----------------------------------------------------------------------+
   | PHP Version 4                                                        |
   +----------------------------------------------------------------------+
   | Copyright (c) 1997-2003 The PHP Group                                |
   +----------------------------------------------------------------------+
   | This source file is subject to version 3.0 of the PHP license,       |
   | that is bundled with this package in the file LICENSE, and is        |
   | available through the world-wide-web at the following url:           |
   | http://www.php.net/license/3_0.txt.                                  |
   | If you did not receive a copy of the PHP license and are unable to   |
   | obtain it through the world-wide-web, please send a note to          |
   | license@php.net so we can mail you a copy immediately.               |
   +----------------------------------------------------------------------+
   | Author: Jim Winstead <jimw@php.net>                                  |
   +----------------------------------------------------------------------+
 */

/* $Id: StreamFopenWrappers.h,v 1.1.2.2 2006/04/15 20:19:12 prosl0am Exp $ */

#ifndef FOPEN_WRAPPERS_H
#define FOPEN_WRAPPERS_H

#define getcwd(a, b)		_getcwd(a, b)

//typedef struct _cwd_state
//{
//	char *cwd;
//	int cwd_length;
//}
//cwd_state;

#ifdef __cplusplus
extern "C"
{
#endif

ZEND_API int php_fopen_primary_script(zend_file_handle *file_handle TSRMLS_DC);
ZEND_API char *expand_filepath(const char *filepath, char *real_path TSRMLS_DC);

ZEND_API int php_check_open_basedir(const char *path TSRMLS_DC);
ZEND_API int php_check_open_basedir_ex(const char *path, int warn TSRMLS_DC);
ZEND_API int php_check_specific_open_basedir(const char *basedir, const char *path TSRMLS_DC);

ZEND_API int php_check_safe_mode_include_dir(char *path TSRMLS_DC);

ZEND_API FILE *php_fopen_with_path(char *filename, char *mode, char *path, char **opened_path TSRMLS_DC);

ZEND_API int php_is_url(char *path);
ZEND_API char *php_strip_url_passwd(char *path);

#ifdef __cplusplus
}
#endif

#endif
