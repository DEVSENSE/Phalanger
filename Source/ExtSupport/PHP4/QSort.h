//
// ExtSupport.PHP4 - substitute for php4ts.dll
//
// QSort.h
// - slightly modified zend_qsort.h, originally PHP source file
//

#pragma once

#include "stdafx.h"
#include "ExtSupport.h"

/*
   +----------------------------------------------------------------------+
   | Zend Engine                                                          |
   +----------------------------------------------------------------------+
   | Copyright (c) 1998-2003 Zend Technologies Ltd. (http://www.zend.com) |
   +----------------------------------------------------------------------+
   | This source file is subject to version 2.00 of the Zend license,     |
   | that is bundled with this package in the file LICENSE, and is        | 
   | available at through the world-wide-web at                           |
   | http://www.zend.com/license/2_00.txt.                                |
   | If you did not receive a copy of the Zend license and are unable to  |
   | obtain it through the world-wide-web, please send a note to          |
   | license@zend.com so we can mail you a copy immediately.              |
   +----------------------------------------------------------------------+
   | Authors: Sterling Hughes <sterling@php.net>                          |
   +----------------------------------------------------------------------+
*/

#ifdef __cplusplus
extern "C"
{
#endif

ZEND_API void zend_qsort(void *base, size_t nmemb, size_t siz, compare_func_t compare TSRMLS_DC);

#ifdef __cplusplus
}
#endif
