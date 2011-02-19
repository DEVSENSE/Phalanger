//
// ExtSupport.PHP4 - substitute for php4ts.dll
//
// Memory.cpp 
// - contains definitions of exported memory management functions
//

#include "stdafx.h"
#include "ExtSupport.h"
#include "Memory.h"
#include "Request.h"
#include "Errors.h"

#include <string.h>
#include <malloc.h>
#include <limits.h>

using namespace System;

using namespace PHP::ExtManager;

ZEND_API void *_ecalloc(size_t num, size_t size ZEND_FILE_LINE_DC ZEND_FILE_LINE_ORIG_DC)
{
	void *ptr;
	size_t total = num * size;

	// check caller's sanity
	if (num && (total / num != size)) return NULL;

	if (!(ptr = _emalloc(total))) return NULL;

	memset(ptr, 0, total);
	return ptr;
}

ZEND_API void _efree(void *ptr ZEND_FILE_LINE_DC ZEND_FILE_LINE_ORIG_DC)
{
	MEMORY_BLOCK_HEADER *mem = (MEMORY_BLOCK_HEADER *)ptr - 1;

	//if (mem->next == (MEMORY_BLOCK_HEADER *)-1 &&
	//	mem->prev == (MEMORY_BLOCK_HEADER *)-1)
	//{
	//	// this block was not chained
	//	free(mem);
	//	return;
	//}
	
	void ***tsrm_ls = NULL;

	// delete this block from request's block chain
	if (mem->next) mem->next->prev = mem->prev;
	if (mem->prev) mem->prev->next = mem->next; 
	else 
	{
		tsrm_ls = (void ***)ts_resource_ex(0, NULL);
		AG(MemBlocks) = mem->next;
	}

	if (mem->zvalsize)
	{
		// put the block to zval lookaside buffer
		if (tsrm_ls == NULL) tsrm_ls = (void ***)ts_resource_ex(0, NULL);
		mem->next = AG(ZvalCache);
		AG(ZvalCache) = mem;
	}
	else free(mem);
}

ZEND_API void *_emalloc(size_t size ZEND_FILE_LINE_DC ZEND_FILE_LINE_ORIG_DC)
{
	MEMORY_BLOCK_HEADER *mem;

	TSRMLS_FETCH();

	if (size <= sizeof(zval) && AG(ZvalCache) != NULL)
	{
		// get the block from zval lookaside buffer
		mem = AG(ZvalCache);
		AG(ZvalCache) = AG(ZvalCache)->next;
	}
	else
	{
		mem = (MEMORY_BLOCK_HEADER *)malloc(sizeof(MEMORY_BLOCK_HEADER) + size);
		if (!mem) return NULL;

		mem->zvalsize = (size == sizeof(zval));
	}

	//if (request == NULL)
	//{
	//	mem->next = (MEMORY_BLOCK_HEADER *)-1;
	//	mem->prev = (MEMORY_BLOCK_HEADER *)-1;
	//	return mem + 1;
	//}

	// add this \block to request's block chain
	mem->next = (MEMORY_BLOCK_HEADER *)AG(MemBlocks);
	mem->prev = NULL;
	if (mem->next) mem->next->prev = mem;
	AG(MemBlocks) = mem;

	return mem + 1;
}

ZEND_API void *_erealloc(void *ptr, size_t size, int allow_failure ZEND_FILE_LINE_DC ZEND_FILE_LINE_ORIG_DC)
{
	MEMORY_BLOCK_HEADER *mem = (MEMORY_BLOCK_HEADER *)ptr - 1;
	if (!ptr) return _emalloc(size);

	mem = (MEMORY_BLOCK_HEADER *)realloc(mem, sizeof(MEMORY_BLOCK_HEADER) + size);
	if (!mem) return NULL;

	mem->zvalsize = (size == sizeof(zval));

	//if (mem->next == (MEMORY_BLOCK_HEADER *)-1 &&
	//	mem->prev == (MEMORY_BLOCK_HEADER *)-1)
	//{
	//	// this block was not chained
	//	return mem + 1;
	//}

	// update next's prev and prev's next
	if (mem->next) mem->next->prev = mem;
	if (mem->prev) mem->prev->next = mem; 
	else 
	{
		TSRMLS_FETCH();
		AG(MemBlocks) = mem;
	}

	return mem + 1;
}

// Copy a string into a new one, allocate using _emalloc.
ZEND_API char *_estrdup(const char *strSource ZEND_FILE_LINE_DC ZEND_FILE_LINE_ORIG_DC)
{
	// Exif calls this function with strSource == NULL
	if (strSource == NULL) strSource = "";

	size_t length;
	char *strDest;

	length = strlen(strSource) + 1;
	strDest = (char *)_emalloc(length);
	if (!strDest) return NULL;

	memcpy(strDest, strSource, length);
	return strDest;
}

// Copy a string into a new one, allocate using _emalloc. (Max)length given.
ZEND_API char *_estrndup(const char *strSource, size_t length ZEND_FILE_LINE_DC ZEND_FILE_LINE_ORIG_DC)
{
	char *strDest;

	strDest = (char *)_emalloc(length + 1);
	if (!strDest) return NULL;

	memcpy(strDest, strSource, length);
	strDest[length] = 0;
	return strDest;
}


//#define LONG_MAX 2147483647L
//#define LONG_MIN (- LONG_MAX - 1)

// copied from zend_multiply.h and bautified
#define ZEND_SIGNED_MULTIPLY_LONG(a, b, lval, dval, usedval) do		\
{																	\
	double __tmpvar = (double)(a) * (double)(b);					\
																	\
	if (__tmpvar >= LONG_MAX || __tmpvar <= LONG_MIN)				\
	{																\
		(dval) = __tmpvar;											\
		(usedval) = 1;												\
	}																\
	else															\
	{																\
		(lval) = (a) * (b);											\
		(usedval) = 0;												\
	}																\
} while (0)

// copied from zend_alloc.c and bautified
static inline size_t safe_address(size_t nmemb, size_t size, size_t offset)
{
	size_t res = nmemb * size + offset;
	double _d  = (double)nmemb * (double)size + (double)offset;
	double _delta = (double)res - _d;

	if (/*UNEXPECTED*/((_d + _delta ) != _d)) {
		zend_error(E_ERROR, "Possible integer overflow in memory allocation (%zu * %zu + %zu)", nmemb, size, offset);
		return 0;
	}
	return res;
}

ZEND_API void *_safe_erealloc(void *ptr, size_t nmemb, size_t size, size_t offset ZEND_FILE_LINE_DC ZEND_FILE_LINE_ORIG_DC)
{
	return erealloc_rel(ptr, safe_address(nmemb, size, offset));
}
ZEND_API void *_safe_malloc(size_t nmemb, size_t size, size_t offset)
{
	return pemalloc(safe_address(nmemb, size, offset), 1);
}
ZEND_API void *_safe_emalloc(size_t nmemb, size_t size, size_t offset ZEND_FILE_LINE_DC ZEND_FILE_LINE_ORIG_DC)
{
	return emalloc_rel(safe_address(nmemb, size, offset));
}

// copied from zend.c
ZEND_API void free_estring(char **str_p)
{
	efree(*str_p);
}
