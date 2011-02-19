//
// ExtSupport.PHP4 - substitute for php4ts.dll
//
// Urls.h
// - slightly modified url.h, originally PHP source file
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
/* $Id: Urls.h,v 1.1.2.2 2006/04/15 20:19:12 prosl0am Exp $ */

#ifndef URL_H
#define URL_H

typedef struct php_url {
	char *scheme;
	char *user;
	char *pass;
	char *host;
	unsigned short port;
	char *path;
	char *query;
	char *fragment;
} php_url;

#ifdef __cplusplus
extern "C"
{
#endif

ZEND_API char *php_replace_controlchars(char *str);
ZEND_API char *php_replace_controlchars_ex(char *str, int len);
ZEND_API void php_url_free(php_url *theurl);
ZEND_API php_url *php_url_parse(const char *str);
ZEND_API php_url *php_url_parse_ex(const char *str, int length);
ZEND_API int php_url_decode(char *str, int len); /* return value: length of decoded string */
ZEND_API int php_raw_url_decode(char *str, int len); /* return value: length of decoded string */
ZEND_API char *php_url_encode(char *s, int len, int *new_length);
ZEND_API char *php_raw_url_encode(char *s, int len, int *new_length);

#ifdef __cplusplus
}
#endif

/*
PHP_FUNCTION(parse_url);
PHP_FUNCTION(urlencode);
PHP_FUNCTION(urldecode);
PHP_FUNCTION(rawurlencode);
PHP_FUNCTION(rawurldecode);
PHP_FUNCTION(get_headers);
*/

#endif /* URL_H */
