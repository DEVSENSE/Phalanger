//
// ExtSupport.PHP4 - substitute for php4ts.dll
//
// Strtod.h
// - slightly modified zend_strtod.h, originally PHP source file
//

#pragma once

#include "stdafx.h"
#include "ExtSupport.h"

/*
   +----------------------------------------------------------------------+
   | Zend Engine                                                          |
   +----------------------------------------------------------------------+
   | Copyright (c) 1998-2004 Zend Technologies Ltd. (http://www.zend.com) |
   +----------------------------------------------------------------------+
   | This source file is subject to version 2.00 of the Zend license,     |
   | that is bundled with this package in the file LICENSE, and is        | 
   | available through the world-wide-web at the following url:           |
   | http://www.zend.com/license/2_00.txt.                                |
   | If you did not receive a copy of the Zend license and are unable to  |
   | obtain it through the world-wide-web, please send a note to          |
   | license@zend.com so we can mail you a copy immediately.              |
   +----------------------------------------------------------------------+
   | Authors: Derick Rethans <derick@php.net>                             |
   +----------------------------------------------------------------------+
*/

/* $Id: Strtod.h,v 1.1.2.2 2006/04/15 20:19:12 prosl0am Exp $ */

/* This is a header file for the strtod implementation by David M. Gay which
 * can be found in zend_strtod.c */
#ifndef ZEND_STRTOD_H
#define ZEND_STRTOD_H
//#include <zend.h>

BEGIN_EXTERN_C()
ZEND_API double zend_strtod(const char *s00, char **se);
END_EXTERN_C()

#endif
