//
// ExtSupport.PHP4 - substitute for php4ts.dll
//
// Strings.h
// - contains declarations of string related exported functions
//

#pragma once

#include "stdafx.h"
#include "ExtSupport.h"

#define strlcat		php_strlcat
#define php_memnstr	zend_memnstr

// copied from zend_operators.h and beautified
static inline char *zend_memnstr(char *haystack, char *needle, int needle_len, char *end)
{
	char *p = haystack;
	char ne = needle[needle_len - 1];

	end -= needle_len;

	while (p <= end)
	{
		if ((p = (char *)memchr(p, *needle, (end-p+1))) && ne == p[needle_len-1])
		{
			if (!memcmp(needle, p, needle_len-1))
			{
				return p;
			}
		}
		
		if (p == NULL) return NULL;
		p++;
	}
	
	return NULL;
}

typedef struct
{
	char *c;
	size_t len;
	size_t a;
} smart_str;

#define smart_str_0(x) do {											\
	if ((x)->c) {													\
		(x)->c[(x)->len] = '\0';									\
	}																\
} while (0)

#ifndef SMART_STR_PREALLOC
#define SMART_STR_PREALLOC 128
#endif

#ifndef SMART_STR_START_SIZE
#define SMART_STR_START_SIZE 78
#endif

#ifdef SMART_STR_USE_REALLOC
#define SMART_STR_REALLOC(a,b,c) realloc((a),(b))
#else
#define SMART_STR_REALLOC(a,b,c) perealloc((a),(b),(c))
#endif

#define SMART_STR_DO_REALLOC(d, what) \
	(d)->c = (char *)SMART_STR_REALLOC((d)->c, (d)->a + 1, (what))

#define smart_str_alloc4(d, n, what, newlen) do {					\
	if (!(d)->c) {													\
		(d)->len = 0;												\
		newlen = (n);												\
		(d)->a = newlen < SMART_STR_START_SIZE 						\
				? SMART_STR_START_SIZE 								\
				: newlen + SMART_STR_PREALLOC;						\
		SMART_STR_DO_REALLOC(d, what);								\
	} else {														\
		newlen = (d)->len + (n);									\
		if (newlen >= (d)->a) {										\
			(d)->a = newlen + SMART_STR_PREALLOC;					\
			SMART_STR_DO_REALLOC(d, what);							\
		}															\
	}																\
} while (0)

#define smart_str_alloc(d, n, what) \
	smart_str_alloc4((d), (n), (what), newlen)

/* wrapper */

#define smart_str_appends_ex(dest, src, what) \
	smart_str_appendl_ex((dest), (src), strlen(src), (what))
#define smart_str_appends(dest, src) \
	smart_str_appendl((dest), (src), strlen(src))

#define smart_str_appendc(dest, c) \
	smart_str_appendc_ex((dest), (c), 0)
#define smart_str_free(s) \
	smart_str_free_ex((s), 0)
#define smart_str_appendl(dest, src, len) \
	smart_str_appendl_ex((dest), (src), (len), 0)
#define smart_str_append(dest, src) \
	smart_str_append_ex((dest), (src), 0)
#define smart_str_append_long(dest, val) \
	smart_str_append_long_ex((dest), (val), 0)
#define smart_str_append_off_t(dest, val) \
	smart_str_append_off_t_ex((dest), (val), 0)
#define smart_str_append_unsigned(dest, val) \
	smart_str_append_unsigned_ex((dest), (val), 0)

#define smart_str_appendc_ex(dest, ch, what) do {					\
	register size_t __nl;											\
	smart_str_alloc4((dest), 1, (what), __nl);						\
	(dest)->len = __nl;												\
	((unsigned char *) (dest)->c)[(dest)->len - 1] = (ch);			\
} while (0)

#define smart_str_free_ex(s, what) do {								\
	smart_str *__s = (smart_str *) (s);								\
	if (__s->c) {													\
		pefree(__s->c, what);										\
		__s->c = NULL;												\
	}																\
	__s->a = __s->len = 0;											\
} while (0)

#define smart_str_appendl_ex(dest, src, nlen, what) do {			\
	register size_t __nl;											\
	smart_str *__dest = (smart_str *) (dest);						\
																	\
	smart_str_alloc4(__dest, (nlen), (what), __nl);					\
	memcpy(__dest->c + __dest->len, (src), (nlen));					\
	__dest->len = __nl;												\
} while (0)

/* input: buf points to the END of the buffer */
#define smart_str_print_unsigned4(buf, num, vartype, result) do {	\
	char *__p = (buf);												\
	vartype __num = (num);											\
	*__p = '\0';													\
	do {															\
		*--__p = (char) (__num % 10) + '0';							\
		__num /= 10;												\
	} while (__num > 0);											\
	result = __p;													\
} while (0)

/* buf points to the END of the buffer */
#define smart_str_print_long4(buf, num, vartype, result) do {	\
	if (num < 0) {													\
		/* this might cause problems when dealing with LONG_MIN		\
		   and machines which don't support long long.  Works		\
		   flawlessly on 32bit x86 */								\
		smart_str_print_unsigned4((buf), -(num), vartype, (result));	\
		*--(result) = '-';											\
	} else {														\
		smart_str_print_unsigned4((buf), (num), vartype, (result));	\
	}																\
} while (0)

/*
 * these could be replaced using a braced-group inside an expression
 * for GCC compatible compilers, e.g.
 *
 * #define f(..) ({char *r;..;__r;})
 */  
 
static inline char *smart_str_print_long(char *buf, long num) {
	char *r; 
	smart_str_print_long4(buf, num, unsigned long, r); 
	return r;
}

static inline char *smart_str_print_unsigned(char *buf, long num) {
	char *r; 
	smart_str_print_unsigned4(buf, num, unsigned long, r); 
	return r;
}

#define smart_str_append_generic_ex(dest, num, type, vartype, func) do {	\
	char __b[32];															\
	char *__t;																\
   	smart_str_print##func##4 (__b + sizeof(__b) - 1, (num), vartype, __t);	\
	smart_str_appendl_ex((dest), __t, __b + sizeof(__b) - 1 - __t, (type));	\
} while (0)
	
#define smart_str_append_unsigned_ex(dest, num, type) \
	smart_str_append_generic_ex((dest), (num), (type), unsigned long, _unsigned)

#define smart_str_append_long_ex(dest, num, type) \
	smart_str_append_generic_ex((dest), (num), (type), unsigned long, _long)

#define smart_str_append_off_t_ex(dest, num, type) \
	smart_str_append_generic_ex((dest), (num), (type), off_t, _long)

#define smart_str_append_ex(dest, src, what) \
	smart_str_appendl_ex((dest), ((smart_str *)(src))->c, \
		((smart_str *)(src))->len, (what));


#define smart_str_setl(dest, src, nlen) do {						\
	(dest)->len = (nlen);											\
	(dest)->a = (nlen) + 1;											\
	(dest)->c = (char *) (src);										\
} while (0)

#define smart_str_sets(dest, src) \
	smart_str_setl((dest), (src), strlen(src));

#define php_mblen(ptr, len) mblen(ptr, len)

enum entity_charset { cs_terminator, cs_8859_1, cs_cp1252,
					  cs_8859_15, cs_utf_8, cs_big5, cs_gb2312, 
					  cs_big5hkscs, cs_sjis, cs_eucjp, cs_koi8r,
					  cs_cp1251, cs_8859_5, cs_cp866
					};

#define ENT_HTML_QUOTE_NONE		0
#define ENT_HTML_QUOTE_SINGLE	1
#define ENT_HTML_QUOTE_DOUBLE	2

void localeconv_init();
void localeconv_shutdown();

BEGIN_EXTERN_C()

void php_basename_ZendV2(char *s, size_t len, char *suffix, size_t sufflen, char **p_ret, size_t *p_len TSRMLS_DC);

ZEND_API size_t php_strlcat(char *dst, const char *src, size_t siz);

ZEND_API char *php_strtoupper(char *s, size_t len);
ZEND_API char *php_strtolower(char *s, size_t len);
ZEND_API char *php_strtr(char *str, int len, char *str_from, char *str_to, int trlen);
ZEND_API char *php_addslashes(char *str, int length, int *new_length, int should_free TSRMLS_DC);
ZEND_API char *php_addslashes_ex(char *str, int length, int *new_length, int should_free, int ignore_sybase TSRMLS_DC);
ZEND_API char *php_addcslashes(char *str, int length, int *new_length, int freeit, char *what, int wlength TSRMLS_DC);
ZEND_API void php_stripslashes(char *str, int *len TSRMLS_DC);
ZEND_API void php_stripcslashes(char *str, int *len);
ZEND_API char *php_basename(char *s, size_t len, char *suffix, size_t sufflen);
ZEND_API size_t php_dirname(char *str, size_t len);
ZEND_API char *php_stristr(unsigned char *s, unsigned char *t, size_t s_len, size_t t_len);
ZEND_API char *php_str_to_str_ex(char *haystack, int length, char *needle,
		int needle_len, char *str, int str_len, int *_new_length, int case_sensitivity, int *replace_count);
ZEND_API char *php_str_to_str(char *haystack, int length, char *needle,
		int needle_len, char *str, int str_len, int *_new_length);
ZEND_API char *php_trim(char *c, int len, char *what, int what_len, zval *return_value, int mode TSRMLS_DC);
ZEND_API size_t php_strip_tags(char *rbuf, int len, int *state, char *allow, int allow_len);
ZEND_API int php_char_to_str(char *str, uint len, char from, char *to, int to_len, pval *result);
ZEND_API void php_implode(zval *delim, zval *arr, zval *return_value);
ZEND_API void php_explode(zval *delim, zval *str, zval *return_value, int limit);

ZEND_API size_t php_strspn(char *s1, char *s2, char *s1_end, char *s2_end); 
ZEND_API size_t php_strcspn(char *s1, char *s2, char *s1_end, char *s2_end); 

ZEND_API char *zend_strndup(const char *s, uint length);

ZEND_API int strnatcmp_ex(char const *a, size_t a_len, char const *b, size_t b_len, int fold_case);
ZEND_API char *php_escape_html_entities(unsigned char *old, int oldlen, int *newlen, int all,
										int quote_style, char *hint_charset TSRMLS_DC);
ZEND_API char *php_unescape_html_entities(unsigned char *old, int oldlen, int *newlen, int all, int quote_style,
										  char *hint_charset TSRMLS_DC);
ZEND_API struct lconv *localeconv_r(struct lconv *out);

END_EXTERN_C()