//
// ExtSupport.PHP4 - substitute for php4ts.dll
//
// Memory.h
// - contains declarations of exported memory management functions
//

#pragma once

#include "stdafx.h"
#include "ExtSupport.h"


struct MEMORY_BLOCK_HEADER
{
	MEMORY_BLOCK_HEADER *next, *prev;
	
	unsigned int zvalsize : 1;
	unsigned int reserved : 31;
};

#define do_alloca(p) alloca(p)
#define free_alloca(p)

#ifdef __cplusplus
extern "C"
{
#endif

ZEND_API void *_ecalloc(size_t num, size_t size ZEND_FILE_LINE_DC ZEND_FILE_LINE_ORIG_DC);
ZEND_API void _efree(void *ptr ZEND_FILE_LINE_DC ZEND_FILE_LINE_ORIG_DC);
ZEND_API void *_emalloc(size_t size ZEND_FILE_LINE_DC ZEND_FILE_LINE_ORIG_DC);
ZEND_API void *_erealloc(void *ptr, size_t size, int allow_failure ZEND_FILE_LINE_DC ZEND_FILE_LINE_ORIG_DC);
ZEND_API char *_estrdup(const char *strSource ZEND_FILE_LINE_DC ZEND_FILE_LINE_ORIG_DC);
ZEND_API char *_estrndup(const char *strSource, size_t length ZEND_FILE_LINE_DC ZEND_FILE_LINE_ORIG_DC);

ZEND_API void *_safe_erealloc(void *ptr, size_t nmemb, size_t size, size_t offset ZEND_FILE_LINE_DC ZEND_FILE_LINE_ORIG_DC);
ZEND_API void *_safe_malloc(size_t nmemb, size_t size, size_t offset);
ZEND_API void *_safe_emalloc(size_t nmemb, size_t size, size_t offset ZEND_FILE_LINE_DC ZEND_FILE_LINE_ORIG_DC);

ZEND_API void free_estring(char **str_p);

#ifdef __cplusplus
}
#endif
