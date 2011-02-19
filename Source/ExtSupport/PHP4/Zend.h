//
// ExtManager - PHP extension manager, also #included in ExtSupport
//
// Zend.h
// - contains definition of macros and structures used when interacting with PHP
//   extensions (copied from PHP sources and slightly modified)
//

#pragma once

/* general return values */
#define SUCCESS		0
#define FAILURE		-1

/* hidden context-passing parameters */
#define TSRMLS_D	void ***tsrm_ls
#define TSRMLS_DC	, TSRMLS_D
#define TSRMLS_C	tsrm_ls
#define TSRMLS_CC	, TSRMLS_C

#define ZEND_PARSE_PARAMS_QUIET (1 << 1)

#define EMPTY_SWITCH_DEFAULT_CASE()		\
	default:							\
		__assume(0);					\
		break;

/* zval related macros */

#define Z_LVAL(zval)			(zval).value.lval
#define Z_BVAL(zval)			((zend_bool)(zval).value.lval)
#define Z_DVAL(zval)			(zval).value.dval
#define Z_STRVAL(zval)			(zval).value.str.val
#define Z_STRLEN(zval)			(zval).value.str.len
#define Z_ARRVAL(zval)			(zval).value.ht
#define Z_RESVAL(zval)			(zval).value.lval

#ifdef PHP4TS

#define Z_OBJ(zval)			(&(zval).value.obj)
#define Z_OBJPROP(zval)		(zval).value.obj.properties
#define Z_OBJCE(zval)		(zval).value.obj.ce

#define Z_OBJ_P(zval_p)			Z_OBJ(*zval_p)

#define Z_OBJ_PP(zval_pp)		Z_OBJ(**zval_pp)

#else

#define Z_OBJVAL(zval)			(zval).value.obj
#define Z_OBJ_HANDLE(zval)		Z_OBJVAL(zval).handle
#define Z_OBJ_HT(zval)			Z_OBJVAL(zval).handlers
#define Z_OBJCE(zval)			zend_get_class_entry(&(zval) TSRMLS_CC)
#define Z_OBJPROP(zval)			Z_OBJ_HT((zval))->get_properties(&(zval) TSRMLS_CC)
#define Z_OBJ_HANDLER(zval, hf) Z_OBJ_HT((zval))->hf


#define Z_OBJVAL_P(zval_p)      Z_OBJVAL(*zval_p)
#define Z_OBJ_HANDLE_P(zval_p)  Z_OBJ_HANDLE(*zval_p)
#define Z_OBJ_HT_P(zval_p)      Z_OBJ_HT(*zval_p)
#define Z_OBJ_HANDLER_P(zval_p, h) Z_OBJ_HANDLER(*zval_p, h)

#define Z_OBJVAL_PP(zval_pp)    Z_OBJVAL(**zval_pp)
#define Z_OBJ_HANDLE_PP(zval_p) Z_OBJ_HANDLE(**zval_p)
#define Z_OBJ_HT_PP(zval_p)     Z_OBJ_HT(**zval_p)
#define Z_OBJ_HANDLER_PP(zval_p, h) Z_OBJ_HANDLER(**zval_p, h)


#endif

#define Z_LVAL_P(zval_p)		Z_LVAL(*zval_p)
#define Z_BVAL_P(zval_p)		Z_BVAL(*zval_p)
#define Z_DVAL_P(zval_p)		Z_DVAL(*zval_p)
#define Z_STRVAL_P(zval_p)		Z_STRVAL(*zval_p)
#define Z_STRLEN_P(zval_p)		Z_STRLEN(*zval_p)
#define Z_ARRVAL_P(zval_p)		Z_ARRVAL(*zval_p)
#define Z_OBJPROP_P(zval_p)		Z_OBJPROP(*zval_p)
#define Z_OBJCE_P(zval_p)		Z_OBJCE(*zval_p)
#define Z_RESVAL_P(zval_p)		Z_RESVAL(*zval_p)

#define Z_LVAL_PP(zval_pp)		Z_LVAL(**zval_pp)
#define Z_BVAL_PP(zval_pp)		Z_BVAL(**zval_pp)
#define Z_DVAL_PP(zval_pp)		Z_DVAL(**zval_pp)
#define Z_STRVAL_PP(zval_pp)	Z_STRVAL(**zval_pp)
#define Z_STRLEN_PP(zval_pp)	Z_STRLEN(**zval_pp)
#define Z_ARRVAL_PP(zval_pp)	Z_ARRVAL(**zval_pp)
#define Z_OBJPROP_PP(zval_pp)	Z_OBJPROP(**zval_pp)
#define Z_OBJCE_PP(zval_pp)		Z_OBJCE(**zval_pp)
#define Z_RESVAL_PP(zval_pp)	Z_RESVAL(**zval_pp)

#define Z_TYPE(zval)		(zval).type
#define Z_TYPE_P(zval_p)	Z_TYPE(*zval_p)
#define Z_TYPE_PP(zval_pp)	Z_TYPE(**zval_pp)

/* Standard wrapper macros */
#define emalloc(size)						_emalloc((size) ZEND_FILE_LINE_CC ZEND_FILE_LINE_EMPTY_CC)
#define safe_emalloc(nmemb, size, offset)	_safe_emalloc((nmemb), (size), (offset) ZEND_FILE_LINE_CC ZEND_FILE_LINE_EMPTY_CC)
#define efree(ptr)							_efree((ptr) ZEND_FILE_LINE_CC ZEND_FILE_LINE_EMPTY_CC)
#define ecalloc(nmemb, size)				_ecalloc((nmemb), (size) ZEND_FILE_LINE_CC ZEND_FILE_LINE_EMPTY_CC)
#define erealloc(ptr, size)					_erealloc((ptr), (size), 0 ZEND_FILE_LINE_CC ZEND_FILE_LINE_EMPTY_CC)
#define erealloc_recoverable(ptr, size)		_erealloc((ptr), (size), 1 ZEND_FILE_LINE_CC ZEND_FILE_LINE_EMPTY_CC)
#define estrdup(s)							_estrdup((s) ZEND_FILE_LINE_CC ZEND_FILE_LINE_EMPTY_CC)
#define estrndup(s, length)					_estrndup((s), (length) ZEND_FILE_LINE_CC ZEND_FILE_LINE_EMPTY_CC)

/* Relay wrapper macros */
#define emalloc_rel(size)					_emalloc((size) ZEND_FILE_LINE_RELAY_CC ZEND_FILE_LINE_CC)
#define safe_emalloc_rel(nmemb, size, offset)	_safe_emalloc((nmemb), (size), (offset) ZEND_FILE_LINE_RELAY_CC ZEND_FILE_LINE_CC)
#define efree_rel(ptr)						_efree((ptr) ZEND_FILE_LINE_RELAY_CC ZEND_FILE_LINE_CC)
#define ecalloc_rel(nmemb, size)			_ecalloc((nmemb), (size) ZEND_FILE_LINE_RELAY_CC ZEND_FILE_LINE_CC)
#define erealloc_rel(ptr, size)				_erealloc((ptr), (size), 0 ZEND_FILE_LINE_RELAY_CC ZEND_FILE_LINE_CC)
#define erealloc_recoverable_rel(ptr, size)	_erealloc((ptr), (size), 1 ZEND_FILE_LINE_RELAY_CC ZEND_FILE_LINE_CC)
#define estrdup_rel(s)						_estrdup((s) ZEND_FILE_LINE_RELAY_CC ZEND_FILE_LINE_CC)
#define estrndup_rel(s, length)				_estrndup((s), (length) ZEND_FILE_LINE_RELAY_CC ZEND_FILE_LINE_CC)

/* Selective persistent/non persistent allocation macros */
#define pemalloc(size, persistent)			((persistent) ? malloc(size) : emalloc(size))
#define pefree(ptr, persistent)				((persistent) ? free(ptr) : efree(ptr))
#define pecalloc(nmemb, size, persistent)	((persistent) ? calloc((nmemb), (size)) : ecalloc((nmemb), (size)))
#define perealloc(ptr, size, persistent)	((persistent) ? realloc((ptr), (size)) : erealloc((ptr), (size)))
#define perealloc_recoverable(ptr, size, persistent) ((persistent) ? realloc((ptr), (size)) : erealloc_recoverable((ptr), (size)))
#define pestrdup(s, persistent)				((persistent) ? strdup(s) : estrdup(s))

#define ZVAL_CACHE_LIST			0

#define ZEND_FAST_ALLOC_REL(p, type, fc_type)	ZEND_FAST_ALLOC(p, type, fc_type)
#define ZEND_FAST_ALLOC(p, type, fc_type)		(p) = (type *) emalloc(sizeof(type))
#define ZEND_FAST_FREE(p, fc_type)				efree(p)
#define ZEND_FAST_FREE_REL(p, fc_type)			efree_rel(p)

#define ALLOC_ZVAL(z)			ZEND_FAST_ALLOC(z, zval, ZVAL_CACHE_LIST)
#define FREE_ZVAL(z)			ZEND_FAST_FREE(z, ZVAL_CACHE_LIST)

#define ALLOC_PERMANENT_ZVAL(z) (z) = (zval*)malloc(sizeof(zval));	/* TODO */


#define PZVAL_IS_REF(z)			((z)->is_ref)

#define DVAL_TO_LVAL(d, l) (l) = (d) > LONG_MAX ? (unsigned long) (d) : (long) (d)

#define SEPARATE_ZVAL(ppzv)					\
	{										\
		zval *orig_ptr = *(ppzv);			\
											\
		if (orig_ptr->refcount>1)			\
		{									\
			orig_ptr->refcount--;			\
			ALLOC_ZVAL(*(ppzv));			\
			**(ppzv) = *orig_ptr;			\
			zval_copy_ctor(*(ppzv));		\
			(*(ppzv))->refcount=1;			\
			(*(ppzv))->is_ref = 0;			\
		}									\
	}

#define SEPARATE_ZVAL_IF_NOT_REF(ppzv)		\
	if (!PZVAL_IS_REF(*ppzv))				\
	{										\
		SEPARATE_ZVAL(ppzv);				\
	}

#define ZVAL_RESOURCE(z, l)				\
	{									\
		(z)->type = IS_RESOURCE;        \
		(z)->value.lval = l;	        \
	}

#define ZVAL_BOOL(z, b)					\
	{									\
		(z)->type = IS_BOOL;	        \
		(z)->value.lval = b;	        \
	}

#define ZVAL_NULL(z)					\
	{									\
		(z)->type = IS_NULL;	        \
	}

#define ZVAL_LONG(z, l)					\
	{									\
		(z)->type = IS_LONG;	        \
		(z)->value.lval = l;	        \
	}

#define ZVAL_DOUBLE(z, d)				\
	{									\
		(z)->type = IS_DOUBLE;	        \
		(z)->value.dval = d;	        \
	}

#define ZVAL_STRING(z, s, duplicate)												\
	{																				\
		char *__s=(s);																\
		(z)->value.str.len = strlen(__s);											\
		(z)->value.str.val = (duplicate ? estrndup(__s, (z)->value.str.len) : __s);	\
		(z)->type = IS_STRING;														\
	}

#define ZVAL_STRINGL(z, s, l, duplicate)								\
	{																	\
		char *__s = (s); int __l = l;									\
		(z)->value.str.len = __l;										\
		(z)->value.str.val = (duplicate ? estrndup(__s, __l) : __s);	\
		(z)->type = IS_STRING;											\
	}

#define ZVAL_EMPTY_STRING(z)				\
	{										\
		(z)->value.str.len = 0;  			\
		(z)->value.str.val = empty_string;	\
		(z)->type = IS_STRING;				\
	}

#define ZVAL_ZVAL(z, zv, copy, dtor) {  \
		int is_ref, refcount;           \
		is_ref = (z)->is_ref;           \
		refcount = (z)->refcount;       \
		*(z) = *(zv);                   \
		if (copy) {                     \
			zval_copy_ctor(z);          \
	    }                               \
		if (dtor) {                     \
			if (!copy) {                \
				ZVAL_NULL(zv);          \
			}                           \
			zval_ptr_dtor(&zv);         \
	    }                               \
		(z)->is_ref = is_ref;           \
		(z)->refcount = refcount;       \
	}


#define ZVAL_FALSE(z)  					ZVAL_BOOL(z, 0)
#define ZVAL_TRUE(z)  					ZVAL_BOOL(z, 1)

#define INIT_ZVAL(z)					z = zval_used_for_init;

#define INIT_PZVAL(z)		\
		(z)->refcount = 1;	\
		(z)->is_ref = 0;	

#define MAKE_STD_ZVAL(zv)	\
		ALLOC_ZVAL(zv);		\
		INIT_PZVAL(zv);

#define ALLOC_INIT_ZVAL(zp)	\
	ALLOC_ZVAL(zp);			\
	INIT_ZVAL(*zp);

#define zval_copy_ctor_wrapper _zval_copy_ctor
#define zval_dtor_wrapper _zval_dtor
#define zval_ptr_dtor_wrapper _zval_ptr_dtor
#define zval_internal_dtor_wrapper _zval_internal_dtor
#define zval_internal_ptr_dtor_wrapper _zval_internal_ptr_dtor

#define ZVAL_DESTRUCTOR (void (*)(void *)) zval_dtor_wrapper
#define ZVAL_PTR_DTOR (void (*)(void *)) zval_ptr_dtor_wrapper
#define ZVAL_COPY_CTOR (void (*)(void *)) zval_copy_ctor_wrapper
#define ZVAL_INTERNAL_DTOR (void (*)(void *))zval_internal_dtor_wrapper
#define ZVAL_INTERNAL_PTR_DTOR (void (*)(void *))zval_internal_ptr_dtor_wrapper

#define COPY_PZVAL_TO_ZVAL(zv, pzv)				\
		(zv) = *(pzv);							\
		if ((pzv)->refcount>1)					\
		{										\
			zval_copy_ctor(&(zv));				\
			(pzv)->refcount--;					\
		}										\
		else FREE_ZVAL(pzv);					\
		INIT_PZVAL(&(zv));



#define MAX_LENGTH_OF_LONG 18
#define MAX_LENGTH_OF_DOUBLE 32

#define EG_precision 12
#define ZEND_ERROR_BUFFER_SIZE 1024

#define HANDLE_BLOCK_INTERRUPTIONS()
#define HANDLE_UNBLOCK_INTERRUPTIONS()

/* string related macros */
#define STR_FREE(ptr)		if (ptr && ptr != empty_string) { efree(ptr); }
#define STR_FREE_REL(ptr)	if (ptr && ptr != empty_string) { efree_rel(ptr); }

#define STR_REALLOC(ptr, size)										\
	if (ptr != empty_string) ptr = (char *) erealloc(ptr, size);	\
	else															\
	{																\
		ptr = (char *)emalloc(size);								\
		memset(ptr, 0, size);										\
	}

#define STR_EMPTY_ALLOC() estrndup("", sizeof("")-1)

/* */
#define ZEND_FILE_LINE_D
#define ZEND_FILE_LINE_DC
#define ZEND_FILE_LINE_ORIG_D
#define ZEND_FILE_LINE_ORIG_DC
#define ZEND_FILE_LINE_RELAY_C
#define ZEND_FILE_LINE_RELAY_CC
#define ZEND_FILE_LINE_C
#define ZEND_FILE_LINE_CC
#define ZEND_FILE_LINE_EMPTY_C
#define ZEND_FILE_LINE_EMPTY_CC
#define ZEND_FILE_LINE_ORIG_RELAY_C
#define ZEND_FILE_LINE_ORIG_RELAY_CC

#define CHECK_ZVAL_STRING(z)
#define CHECK_ZVAL_STRING_REL(z)

#define TSRMLS_FETCH()				void ***tsrm_ls = (void ***) ts_resource_ex(0, NULL)

/* Ugly hack to support constants as static array indices */
#define IS_CONSTANT_INDEX	0x80

/* errors */
#define E_ERROR				(1<<0L)
#define E_WARNING			(1<<1L)
#define E_PARSE				(1<<2L)
#define E_NOTICE			(1<<3L)
#define E_CORE_ERROR		(1<<4L)
#define E_CORE_WARNING		(1<<5L)
#define E_COMPILE_ERROR		(1<<6L)
#define E_COMPILE_WARNING	(1<<7L)
#define E_USER_ERROR		(1<<8L)
#define E_USER_WARNING		(1<<9L)
#define E_USER_NOTICE		(1<<10L)
#define E_STRICT			(1<<11L)
#define E_RECOVERABLE_ERROR	(1<<12L)

#ifdef PHP4TS
#define E_ALL (E_ERROR | E_WARNING | E_PARSE | E_NOTICE | E_CORE_ERROR | E_CORE_WARNING | E_COMPILE_ERROR | E_COMPILE_WARNING | E_USER_ERROR | E_USER_WARNING | E_USER_NOTICE)
#elif PHP5TS
#define E_ALL (E_ERROR | E_WARNING | E_PARSE | E_NOTICE | E_CORE_ERROR | E_CORE_WARNING | E_COMPILE_ERROR | E_COMPILE_WARNING | E_USER_ERROR | E_USER_WARNING | E_USER_NOTICE | E_RECOVERABLE_ERROR)
#endif

#define php_error zend_error

/* basic properties of a module and of the hosting environment (ExtManager + ExtSupport) */
#define ZEND_DEBUG			0
#define USING_ZTS			1
#ifdef PHP4TS
#define ZEND_MODULE_API_NO	20020429
#elif defined(PHP5TS)
#define ZEND_MODULE_API_NO	20090626
#endif
/* module persistence */
#define MODULE_PERSISTENT	1	// loaded in php.ini
#define MODULE_TEMPORARY	2	// loaded using dl()

/* function parameters */
#define INIT_FUNC_ARGS					int type, int module_number TSRMLS_DC
#define SHUTDOWN_FUNC_ARGS				int type, int module_number TSRMLS_DC
#define ZEND_MODULE_INFO_FUNC_ARGS		zend_module_entry *zend_module TSRMLS_DC
#ifdef PHP4TS
#define INTERNAL_FUNCTION_PARAMETERS	int ht, zval *return_value, zval *this_ptr, \
										int return_value_used TSRMLS_DC
#define INTERNAL_FUNCTION_PARAM_PASSTHRU ht, return_value, this_ptr, return_value_used TSRMLS_CC

#elif defined(PHP5TS)
#define INTERNAL_FUNCTION_PARAMETERS	int ht, zval *return_value, zval **return_value_ptr, zval *this_ptr, \
										int return_value_used TSRMLS_DC

#define INTERNAL_FUNCTION_PARAM_PASSTHRU ht, return_value, return_value_ptr, this_ptr, return_value_used TSRMLS_CC
#endif

/* output support */
#define ZEND_WRITE(str, str_len)		zend_write((str), (str_len))
#define ZEND_WRITE_EX(str, str_len)		zend_write((str), (str_len))
#define ZEND_PUTS(str)					zend_write((str), strlen((str)))
#define ZEND_PUTS_EX(str)				zend_write((str), strlen((str)))
#define ZEND_PUTC(c)					zend_write(&(c), 1), (c)

/* data types */
#ifdef PHP4TS

#define IS_NULL		0
#define IS_LONG		1
#define IS_DOUBLE	2
#define IS_STRING	3
#define IS_ARRAY	4
#define IS_OBJECT	5
#define IS_BOOL		6
#define IS_RESOURCE	7
#define IS_CONSTANT	8
#define IS_CONSTANT_ARRAY	9

#elif defined (PHP5TS)

/* data types */
/* All data types <= IS_BOOL have their constructor/destructors skipped */
#define IS_NULL		0
#define IS_LONG		1
#define IS_DOUBLE	2
#define IS_BOOL		3
#define IS_ARRAY	4
#define IS_OBJECT	5
#define IS_STRING	6
#define IS_RESOURCE	7
#define IS_CONSTANT	8
#define IS_CONSTANT_ARRAY	9

#endif

/* Zend data types */
typedef unsigned char zend_bool;
typedef unsigned char zend_uchar;
typedef unsigned int zend_uint;
typedef unsigned long zend_ulong;
typedef unsigned short zend_ushort;

typedef unsigned int uint;
typedef unsigned long ulong;

#ifdef _WIN64
typedef __int64 zend_intptr_t;
typedef unsigned __int64 zend_uintptr_t;
#else
typedef long zend_intptr_t;
typedef unsigned long zend_uintptr_t;
#endif

/* */
typedef struct _zend_utility_values
{
	char *import_use_extension;
	uint import_use_extension_length;
	zend_bool html_errors;
} zend_utility_values;

#define snprintf _snprintf
#define strcasecmp(s1, s2) stricmp(s1, s2)
#define strncasecmp(s1, s2, n) _strnicmp(s1, s2, n)

// VS 2008 has this already defined
// #define vsnprintf _vsnprintf

#define zend_isinf(a)	((_fpclass(a) == _FPCLASS_PINF) || (_fpclass(a) == _FPCLASS_NINF))
#define zend_finite(x)	_finite(x)
#define zend_isnan(x)	_isnan(x)
#define zend_sprintf sprintf
#define zend_finite(dval)	_finite(dval)

/* variables conversions */
#define convert_to_ex_master(ppzv, lower_type, upper_type)	\
	if ((*ppzv)->type!=IS_##upper_type)						\
	{														\
		if (!(*ppzv)->is_ref) SEPARATE_ZVAL(ppzv);			\
		convert_to_##lower_type(*ppzv);						\
	}

#define convert_to_writable_ex_master(ppzv, lower_type, upper_type)	\
	if ((*ppzv)->type!=IS_##upper_type)								\
	{																\
		SEPARATE_ZVAL(ppzv);										\
		convert_to_##lower_type(*ppzv);								\
	}

#define ZEND_NORMALIZE_BOOL(n)		((n) ? (((n) > 0) ? 1 : -1) : 0)

#define convert_to_string(op)		_convert_to_string((op) ZEND_FILE_LINE_CC)

#define convert_to_boolean_ex(ppzv)	convert_to_ex_master(ppzv, boolean, BOOL)
#define convert_to_long_ex(ppzv)	convert_to_ex_master(ppzv, long, LONG)
#define convert_to_double_ex(ppzv)	convert_to_ex_master(ppzv, double, DOUBLE)
#define convert_to_string_ex(ppzv)	convert_to_ex_master(ppzv, string, STRING)
#define convert_to_array_ex(ppzv)	convert_to_ex_master(ppzv, array, ARRAY)
#define convert_to_object_ex(ppzv)	convert_to_ex_master(ppzv, object, OBJECT)
#define convert_to_null_ex(ppzv)	convert_to_ex_master(ppzv, null, NULL)

#define convert_to_writable_boolean_ex(ppzv)	convert_to_writable_ex_master(ppzv, boolean, BOOL)
#define convert_to_writable_long_ex(ppzv)		convert_to_writable_ex_master(ppzv, long, LONG)
#define convert_to_writable_double_ex(ppzv)		convert_to_writable_ex_master(ppzv, double, DOUBLE)
#define convert_to_writable_string_ex(ppzv)		convert_to_writable_ex_master(ppzv, string, STRING)
#define convert_to_writable_array_ex(ppzv)		convert_to_writable_ex_master(ppzv, array, ARRAY)
#define convert_to_writable_object_ex(ppzv)		convert_to_writable_ex_master(ppzv, object, OBJECT)
#define convert_to_writable_null_ex(ppzv)		convert_to_writable_ex_master(ppzv, null, NULL)

#define convert_scalar_to_number_ex(ppzv)							\
	if ((*ppzv)->type!=IS_LONG && (*ppzv)->type!=IS_DOUBLE)			\
	{																\
		if (!(*ppzv)->is_ref) SEPARATE_ZVAL(ppzv);					\
		convert_scalar_to_number(*ppzv TSRMLS_CC);					\
	}

// hashtable macros and typedefs
#define HASH_KEY_IS_STRING 1
#define HASH_KEY_IS_LONG 2
#define HASH_KEY_NON_EXISTANT 3

#define HASH_UPDATE 		(1<<0)
#define HASH_ADD			(1<<1)
#define HASH_NEXT_INSERT	(1<<2)

#define HASH_DEL_KEY 0
#define HASH_DEL_INDEX 1

#define ALLOC_HASHTABLE(ht)		ZEND_FAST_ALLOC(ht, HashTable, HASHTABLE_CACHE_LIST)
#define FREE_HASHTABLE(ht)		ZEND_FAST_FREE(ht, HASHTABLE_CACHE_LIST)
#define ALLOC_HASHTABLE_REL(ht)	ZEND_FAST_ALLOC_REL(ht, HashTable, HASHTABLE_CACHE_LIST)
#define FREE_HASHTABLE_REL(ht)	ZEND_FAST_FREE_REL(ht, HASHTABLE_CACHE_LIST)

typedef unsigned long (*hash_func_t)(char *arKey, unsigned nKeyLength);
typedef int  (*compare_func_t)(const void *, const void * TSRMLS_DC);
typedef void (*sort_func_t)(void *, size_t, register size_t, compare_func_t TSRMLS_DC);
typedef void (*dtor_func_t)(void *pDest);
typedef void (*copy_ctor_func_t)(void *pElement);

// hashtable bucket
typedef struct bucket {
	ulong h;						/* Used for numeric indexing */
	uint nKeyLength;
	void *pData;
	void *pDataPtr;
	struct bucket *pListNext;
	struct bucket *pListLast;
	struct bucket *pNext;
	struct bucket *pLast;
	char arKey[1]; /* Must be last element */
} Bucket;

// hashtable - PHP array
typedef struct _hashtable {
	uint nTableSize;
	uint nTableMask;
	uint nNumOfElements;
	ulong nNextFreeElement;
	Bucket *pInternalPointer;	/* Used for element traversal */
	Bucket *pListHead;
	Bucket *pListTail;
	Bucket **arBuckets;
	dtor_func_t pDestructor;
	zend_bool persistent;
	unsigned char nApplyCount;
	zend_bool bApplyProtection;
#if 0//ZEND_DEBUG
	int inconsistent;
#endif
} HashTable;

typedef Bucket* HashPosition;

struct _zend_class_entry;
typedef _zend_class_entry zend_class_entry;
struct zval;

typedef struct _zend_object_value zend_object_value;

#ifdef PHP4TS
// Represents a PHP object.
struct zend_object
{
	zend_class_entry *ce;
	HashTable *properties;
//	unsigned int in_get:1;
//	unsigned int in_set:1;
};

#elif defined(PHP5TS)

typedef struct _zend_guard {
	zend_bool in_get;
	zend_bool in_set;
	zend_bool in_unset;
	zend_bool in_isset;
	zend_bool dummy; /* sizeof(zend_guard) must not be equal to sizeof(void*) */
} zend_guard;

typedef struct _zend_object {
	zend_class_entry *ce;
	HashTable *properties;
	HashTable *guards; /* protects from __get/__set ... recursion */
} zend_object;

#endif

typedef unsigned int zend_object_handle;

typedef struct _zend_object_handlers zend_object_handlers;

struct _zend_object_value
{
	zend_object_handle handle;
	zend_object_handlers *handlers;
};

// Represents a PHP variable value.
union zvalue_value
{
	long lval;					/* long value	*/
	double dval;				/* double value	*/
	struct 
	{
		char *val;
		int len;
	} str;
	HashTable *ht;				/* hash table value - PHP array	*/
#ifdef PHP4TS
	zend_object obj;			/* object value - PHP object	*/
#elif defined(PHP5TS)
	zend_object_value obj;
#endif
};

// Represents a PHP variable.
//God only knows why the PHP folks shuffled this structure around from PHP4 to PHP5
#ifdef PHP4TS

struct zval
{
	/* Variable information */
	zvalue_value value;			/* value */
	unsigned char type;			/* active type */
	unsigned char is_ref;
	unsigned short refcount;
};

#elif defined (PHP5TS)

struct zval
{
	/* Variable information */
	zvalue_value value;		/* value */
	zend_uint refcount;
	zend_uchar type;	/* active type */
	zend_uchar is_ref;
};

#endif

#define CONST_CS				(1<<0)				/* Case Sensitive */
#define CONST_PERSISTENT		(1<<1)				/* Persistent */

// Represents a PHP constant.
struct zend_constant
{
	zval value;
	int flags;
	char *name;
	uint name_len;
	int module_number;
};

/* Argument passing types */
#define BYREF_NONE			0
#define BYREF_FORCE			1
#define BYREF_ALLOW			2
#define BYREF_FORCE_REST	3

#ifdef PHP4TS
// Represents a function in an extension.
struct zend_function_entry {
	char *fname;
	void (*handler)(INTERNAL_FUNCTION_PARAMETERS);
	unsigned char *func_arg_types;
};

// Represents an extension.
struct zend_module_entry
{
    unsigned short size;
	unsigned int zend_api;
	unsigned char zend_debug;
	unsigned char zts;
	char *name;
	zend_function_entry *functions;
	int (*module_startup_func)(INIT_FUNC_ARGS);
	int (*module_shutdown_func)(SHUTDOWN_FUNC_ARGS);
	int (*request_startup_func)(INIT_FUNC_ARGS);
	int (*request_shutdown_func)(SHUTDOWN_FUNC_ARGS);
	void (*info_func)(ZEND_MODULE_INFO_FUNC_ARGS);
	char *version;

	// seems like these global_ functions are not used in PHP at all
	int (*global_startup_func)(void);
	int (*global_shutdown_func)(void);

	int globals_id;
	int module_started;
	unsigned char type;
	void *handle;
	int module_number;
};

// Represents an extensions in PHP 4.1.0-
struct pre_4_1_0_module_entry 
{
	char *name;
	zend_function_entry *functions;
	int (*module_startup_func)(INIT_FUNC_ARGS);
	int (*module_shutdown_func)(SHUTDOWN_FUNC_ARGS);
	int (*request_startup_func)(INIT_FUNC_ARGS);
	int (*request_shutdown_func)(SHUTDOWN_FUNC_ARGS);
	void (*info_func)(ZEND_MODULE_INFO_FUNC_ARGS);
	
	// seems like these global_ functions are not used in PHP at all
	int (*global_startup_func)(void);
	int (*global_shutdown_func)(void);
	
	int globals_id;
	int module_started;
	unsigned char type;
	void *handle;
	int module_number;
	unsigned char zend_debug;
	unsigned char zts;
	unsigned int zend_api;
};
#elif defined(PHP5TS)

struct zend_function_entry {
	char *fname;
	void (*handler)(INTERNAL_FUNCTION_PARAMETERS);
	struct _zend_arg_info *arg_info;
	zend_uint num_args;
	zend_uint flags;
};

// Represents an extension.
struct zend_module_entry {
	unsigned short size;
	unsigned int zend_api;
	unsigned char zend_debug;
	unsigned char zts;
	struct _zend_ini_entry *ini_entry;
	struct _zend_module_dep *deps;
	char *name;
	zend_function_entry *functions;
	int (*module_startup_func)(INIT_FUNC_ARGS);
	int (*module_shutdown_func)(SHUTDOWN_FUNC_ARGS);
	int (*request_startup_func)(INIT_FUNC_ARGS);
	int (*request_shutdown_func)(SHUTDOWN_FUNC_ARGS);
	void (*info_func)(ZEND_MODULE_INFO_FUNC_ARGS);
	char *version;
	size_t globals_size;

	void* globals_ptr;
	void (*globals_ctor)(void *global TSRMLS_DC);
	void (*globals_dtor)(void *global TSRMLS_DC);
	int (*post_deactivate_func)(void);
	int module_started;
	unsigned char type;
	void *handle;
	int module_number;
	char*build_id;
};
#endif

#undef MIN
#undef MAX
#define MAX(a, b)  (((a) > (b)) ? (a) : (b))
#define MIN(a, b)  (((a) < (b)) ? (a) : (b))

/* output support */
#define PUTS(str)	do													\
					{													\
						const char *__str = (str);						\
						php_body_write(__str, strlen(__str) TSRMLS_CC);	\
					} while (0)

#define PHPWRITE(str, str_len)		php_body_write((str), (str_len) TSRMLS_CC)
#define PUTC(c)						(php_body_write(&(c), 1 TSRMLS_CC), (c))
#define PHPWRITE_H(str, str_len)	php_header_write((str), (str_len) TSRMLS_CC)
#define PUTC_H(c)					(php_header_write(&(c), 1 TSRMLS_CC), (c))

#define PUTS_H(str)	do														\
					{														\
						const char *__str = (str);							\
						php_header_write(__str, strlen(__str) TSRMLS_CC);	\
					} while (0)

#define php_error_docref php_error_docref0

typedef int uid_t;
typedef int gid_t;

/* Name macros */
#define ZEND_MODULE_STARTUP_N(module)       zm_startup_##module
#define ZEND_MODULE_SHUTDOWN_N(module)		zm_shutdown_##module
#define ZEND_MODULE_ACTIVATE_N(module)		zm_activate_##module
#define ZEND_MODULE_DEACTIVATE_N(module)	zm_deactivate_##module
#define ZEND_MODULE_INFO_N(module)			zm_info_##module

/* Declaration macros */
#define ZEND_MODULE_STARTUP_D(module)		int ZEND_MODULE_STARTUP_N(module)(INIT_FUNC_ARGS)
#define ZEND_MODULE_SHUTDOWN_D(module)		int ZEND_MODULE_SHUTDOWN_N(module)(SHUTDOWN_FUNC_ARGS)
#define ZEND_MODULE_ACTIVATE_D(module)		int ZEND_MODULE_ACTIVATE_N(module)(INIT_FUNC_ARGS)
#define ZEND_MODULE_DEACTIVATE_D(module)	int ZEND_MODULE_DEACTIVATE_N(module)(SHUTDOWN_FUNC_ARGS)
#define ZEND_MODULE_INFO_D(module)			void ZEND_MODULE_INFO_N(module)(ZEND_MODULE_INFO_FUNC_ARGS)

#define PHP_WIN32
#define TSRM_WIN32
#define ZEND_WIN32

#define MAXPATHLEN _MAX_PATH
#define PHP_DIR_SEPARATOR '\\'

#define ZTS

#define RETVAL_RESOURCE(l)				ZVAL_RESOURCE(return_value, l)
#define RETVAL_BOOL(b)					ZVAL_BOOL(return_value, b)
#define RETVAL_NULL() 					ZVAL_NULL(return_value)
#define RETVAL_LONG(l) 					ZVAL_LONG(return_value, l)
#define RETVAL_DOUBLE(d) 				ZVAL_DOUBLE(return_value, d)
#define RETVAL_STRING(s, duplicate) 		ZVAL_STRING(return_value, s, duplicate)
#define RETVAL_STRINGL(s, l, duplicate) 	ZVAL_STRINGL(return_value, s, l, duplicate)
#define RETVAL_EMPTY_STRING() 			ZVAL_EMPTY_STRING(return_value)
#define RETVAL_ZVAL(zv, copy, dtor)		ZVAL_ZVAL(return_value, zv, copy, dtor)
#define RETVAL_FALSE  					ZVAL_BOOL(return_value, 0)
#define RETVAL_TRUE   					ZVAL_BOOL(return_value, 1)

#define RETURN_RESOURCE(l) 				{ RETVAL_RESOURCE(l); return; }
#define RETURN_BOOL(b) 					{ RETVAL_BOOL(b); return; }
#define RETURN_NULL() 					{ RETVAL_NULL(); return;}
#define RETURN_LONG(l) 					{ RETVAL_LONG(l); return; }
#define RETURN_DOUBLE(d) 				{ RETVAL_DOUBLE(d); return; }
#define RETURN_STRING(s, duplicate) 	{ RETVAL_STRING(s, duplicate); return; }
#define RETURN_STRINGL(s, l, duplicate) { RETVAL_STRINGL(s, l, duplicate); return; }
#define RETURN_EMPTY_STRING() 			{ RETVAL_EMPTY_STRING(); return; }
#define RETURN_ZVAL(zv, copy, dtor)		{ RETVAL_ZVAL(zv, copy, dtor); return; }
#define RETURN_FALSE  					{ RETVAL_FALSE; return; }
#define RETURN_TRUE   					{ RETVAL_TRUE; return; }

typedef zval pval;
typedef unsigned int php_uint32;
typedef signed int php_int32;

#define ZEND_HANDLE_FILENAME		0
#define ZEND_HANDLE_FD				1
#define ZEND_HANDLE_FP				2
#define ZEND_HANDLE_STDIOSTREAM		3
#define ZEND_HANDLE_FSTREAM			4
#define ZEND_HANDLE_STREAM			5
#define ZEND_HANDLE_SOCKET_FD		5

#define HAVE_UTIME	1

#define WRONG_PARAM_COUNT					ZEND_WRONG_PARAM_COUNT()
#define WRONG_PARAM_COUNT_WITH_RETVAL(ret)	ZEND_WRONG_PARAM_COUNT_WITH_RETVAL(ret)
#define ARG_COUNT(dummy)	(ht)
#define ZEND_NUM_ARGS()		(ht)
#define ZEND_WRONG_PARAM_COUNT()					{ zend_wrong_param_count(TSRMLS_C); return; }
#define ZEND_WRONG_PARAM_COUNT_WITH_RETVAL(ret)		{ zend_wrong_param_count(TSRMLS_C); return ret; }

#define ZEND_FN(name)				zif_##name
#define ZEND_NAMED_FUNCTION(name)	void name(INTERNAL_FUNCTION_PARAMETERS)
#define ZEND_FUNCTION(name)			ZEND_NAMED_FUNCTION(ZEND_FN(name))

#define DL_HANDLE					HMODULE

#define ZEND_SET_SYMBOL(symtable, name, var)										\
	{																				\
		char *_name = (name);														\
																					\
		ZEND_SET_SYMBOL_WITH_LENGTH(symtable, _name, strlen(_name)+1, var, 1, 0);	\
	}

#define ZEND_SET_SYMBOL_WITH_LENGTH(symtable, name, name_length, var, _refcount, _is_ref)				\
	{																									\
		zval **orig_var;																				\
																										\
		if (zend_hash_find(symtable, (name), (name_length), (void **) &orig_var)==SUCCESS				\
			&& PZVAL_IS_REF(*orig_var)) {																\
			(var)->refcount = (*orig_var)->refcount;													\
			(var)->is_ref = 1;																			\
																										\
			if (_refcount) {																			\
				(var)->refcount += _refcount-1;															\
			}																							\
			zval_dtor(*orig_var);																		\
			**orig_var = *(var);																		\
			FREE_ZVAL(var);																					\
		} else {																						\
			(var)->is_ref = _is_ref;																	\
			if (_refcount) {																			\
				(var)->refcount = _refcount;															\
			}																							\
			zend_hash_update(symtable, (name), (name_length), &(var), sizeof(zval *), NULL);			\
		}																								\
	}

typedef struct _zend_overloaded_element
{
	int type;		/* array offset or object proprety */
	zval element;
}
zend_overloaded_element;

#define OE_IS_ARRAY   (1<<0)
#define OE_IS_OBJECT  (1<<1)
#define OE_IS_METHOD  (1<<2)

#define TRACK_VARS_POST		0
#define TRACK_VARS_GET		1
#define TRACK_VARS_COOKIE	2
#define TRACK_VARS_SERVER	3
#define TRACK_VARS_ENV		4
#define TRACK_VARS_FILES	5


#ifdef __cplusplus
#define BEGIN_EXTERN_C() extern "C" {
#define END_EXTERN_C() }
#else
#define BEGIN_EXTERN_C()
#define END_EXTERN_C()
#endif


#define zend_vspprintf vspprintf
#define zend_spprintf spprintf


#define ZEND_CLONE_FUNC_NAME		"__clone"
#define ZEND_CONSTRUCTOR_FUNC_NAME	"__construct"
#define ZEND_DESTRUCTOR_FUNC_NAME	"__destruct"
#define ZEND_GET_FUNC_NAME          "__get"
#define ZEND_SET_FUNC_NAME          "__set"
#define ZEND_UNSET_FUNC_NAME        "__unset"
#define ZEND_ISSET_FUNC_NAME        "__isset"
#define ZEND_CALL_FUNC_NAME         "__call"
#define ZEND_TOSTRING_FUNC_NAME     "__tostring"
#define ZEND_AUTOLOAD_FUNC_NAME     "__autoload"