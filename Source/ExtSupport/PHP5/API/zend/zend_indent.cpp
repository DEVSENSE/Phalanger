//
// ExtSupport.PHP5 - substitute for php5ts.dll
//
// zend_indent.cpp
// - this is slightly modified zend_indent.c, originally PHP 5.3.3 source files
//


/*
   +----------------------------------------------------------------------+
   | Zend Engine                                                          |
   +----------------------------------------------------------------------+
   | Copyright (c) 1998-2010 Zend Technologies Ltd. (http://www.zend.com) |
   +----------------------------------------------------------------------+
   | This source file is subject to version 2.00 of the Zend license,     |
   | that is bundled with this package in the file LICENSE, and is        | 
   | available through the world-wide-web at the following url:           |
   | http://www.zend.com/license/2_00.txt.                                |
   | If you did not receive a copy of the Zend license and are unable to  |
   | obtain it through the world-wide-web, please send a note to          |
   | license@zend.com so we can mail you a copy immediately.              |
   +----------------------------------------------------------------------+
   | Authors: Andi Gutmans <andi@zend.com>                                |
   |          Zeev Suraski <zeev@zend.com>                                |
   +----------------------------------------------------------------------+
*/

/* $Id: zend_indent.c 296107 2010-03-12 10:28:59Z jani $ */

/* This indenter doesn't really work, it's here for no particular reason. */


#include "zend.h"
//#include "zend_language_parser.h"
//#include "zend_compile.h"
#include "../../Unsupported.h"
#include "zend_indent.h"



ZEND_API void zend_indent()
{
	UNSUPPORTED_MSG("zend_indent");
}

/*
 * Local variables:
 * tab-width: 4
 * c-basic-offset: 4
 * indent-tabs-mode: t
 * End:
 */

