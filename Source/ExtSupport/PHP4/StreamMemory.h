//
// ExtSupport.PHP4 - substitute for php4ts.dll
//
// StreamMemory.h
// - slightly modified php_memory_streams.h, originally PHP source file
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
   | Author: Marcus Boerger <helly@php.net>                               |
   +----------------------------------------------------------------------+
 */

/* $Id: StreamMemory.h,v 1.1.2.2 2006/04/15 20:19:12 prosl0am Exp $ */

#ifndef PHP_MEMORY_STREAM_H
#define PHP_MEMORY_STREAM_H

#include "Streams.h"

#define PHP_STREAM_MAX_MEM	2 * 1024 * 1024

#define TEMP_STREAM_DEFAULT  0
#define TEMP_STREAM_READONLY 1

#define php_stream_memory_create(mode) _php_stream_memory_create((mode) STREAMS_CC TSRMLS_CC)
#define php_stream_memory_create_rel(mode) _php_stream_memory_create((mode) STREAMS_REL_CC TSRMLS_CC)
#define php_stream_memory_open(mode, buf, length) _php_stream_memory_open((mode), (buf), (length) STREAMS_CC TSRMLS_CC)
#define php_stream_memory_get_buffer(stream, length) _php_stream_memory_get_buffer((stream), (length) STREAMS_CC TSRMLS_CC)

#define php_stream_temp_new() php_stream_temp_create(TEMP_STREAM_DEFAULT, PHP_STREAM_MAX_MEM)
#define php_stream_temp_create(mode, max_memory_usage) _php_stream_temp_create((mode), (max_memory_usage) STREAMS_CC TSRMLS_CC)
#define php_stream_temp_create_rel(mode, max_memory_usage) _php_stream_temp_create((mode), (max_memory_usage) STREAMS_REL_CC TSRMLS_CC)
#define php_stream_temp_open(mode, max_memory_usage, buf, length) _php_stream_temp_open((mode), (max_memory_usage), (buf), (length) STREAMS_CC TSRMLS_CC)

#ifdef __cplusplus
extern "C"
{
#endif

ZEND_API php_stream *_php_stream_memory_create(int mode STREAMS_DC TSRMLS_DC);
ZEND_API php_stream *_php_stream_memory_open(int mode, char *buf, size_t length STREAMS_DC TSRMLS_DC);
ZEND_API char *_php_stream_memory_get_buffer(php_stream *stream, size_t *length STREAMS_DC TSRMLS_DC);

ZEND_API php_stream *_php_stream_temp_create(int mode, size_t max_memory_usage STREAMS_DC TSRMLS_DC);
ZEND_API php_stream *_php_stream_temp_open(int mode, size_t max_memory_usage, char *buf, size_t length STREAMS_DC TSRMLS_DC);

extern php_stream_ops php_stream_memory_ops;
extern php_stream_ops php_stream_temp_ops;

#ifdef __cplusplus
}
#endif

#define PHP_STREAM_IS_MEMORY &php_stream_memory_ops
#define PHP_STREAM_IS_TEMP   &php_stream_temp_ops

#endif
