//
// ExtSupport.PHP4 - substitute for php4ts.dll
//
// Strings.cpp
// - contains definitions of string related exported functions
//

#include "stdafx.h"
#include "Strings.h"
#include "Errors.h"
#include "Memory.h"
#include "TsrmLs.h"
#include "Variables.h"
#include "Arrays.h"
#include "Misc.h"

#include <locale.h>

#pragma unmanaged

// copied from strlcat.c and beautified
/*
 * Appends src to string dst of size siz (unlike strncat, siz is the
 * full size of dst, not space left).  At most siz-1 characters
 * will be copied.  Always NUL terminates (unless siz == 0).
 * Returns strlen(src); if retval >= siz, truncation occurred.
 */
ZEND_API size_t php_strlcat(char *dst, const char *src, size_t siz)
{
	register char *d = dst;
	register const char *s = src;
	register size_t n = siz;
	size_t dlen;

	/* Find the end of dst and adjust bytes left but don't go past end */
	while (*d != '\0' && n-- != 0) d++;
	dlen = d - dst;
	n = siz - dlen;

	if (n == 0) return(dlen + strlen(s));
	while (*s != '\0')
	{
		if (n != 1)
		{
			*d++ = *s;
			n--;
		}
		s++;
	}
	*d = '\0';

	return dlen + (s - src);	/* count does not include NUL */
}

// copied from string.c and beautified
/* {{{ php_similar_str
 */
static void php_similar_str(const char *txt1, int len1, const char *txt2, int len2, int *pos1, int *pos2, int *max)
{
	char *p, *q;
	char *end1 = (char *) txt1 + len1;
	char *end2 = (char *) txt2 + len2;
	int l;
	
	*max = 0;
	for (p = (char *) txt1; p < end1; p++)
	{
		for (q = (char *) txt2; q < end2; q++)
		{
			for (l = 0; (p + l < end1) && (q + l < end2) && (p[l] == q[l]); l++);
			if (l > *max)
			{
				*max = l;
				*pos1 = p - txt1;
				*pos2 = q - txt2;
			}
		}
	}
}
/* }}} */

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
   | Authors: Rasmus Lerdorf <rasmus@php.net>                             |
   |          Stig Sather Bakken <ssb@php.net>                            |
   |          Zeev Suraski <zeev@zend.com>                                |
   +----------------------------------------------------------------------+
 */

/* $Id: Strings.cpp,v 1.1.2.2 2006/04/15 20:19:12 prosl0am Exp $ */

/* Synced with php 3.0 revision 1.193 1999-06-16 [ssb] */

#include <stdio.h>

#include <math.h>

#ifdef ZTS
//#include "TSRM.h"
#endif

#define STR_PAD_LEFT			0
#define STR_PAD_RIGHT			1
#define STR_PAD_BOTH			2
#define PHP_PATHINFO_DIRNAME 	1
#define PHP_PATHINFO_BASENAME 	2
#define PHP_PATHINFO_EXTENSION 	4
#define PHP_PATHINFO_ALL	(PHP_PATHINFO_DIRNAME | PHP_PATHINFO_BASENAME | PHP_PATHINFO_EXTENSION)

#define STR_STRSPN				0
#define STR_STRCSPN				1

int php_tag_find(char *tag, int len, char *set);

/* this is read-only, so it's ok */
static char hexconvtab[] = "0123456789abcdef";

/* localeconv mutex */
#ifdef ZTS
static MUTEX_T locale_mutex = NULL;
#endif

/* {{{ php_bin2hex
 */
static char *php_bin2hex(const unsigned char *old, const size_t oldlen, size_t *newlen)
{
	register unsigned char *result = NULL;
	size_t i, j;

	result = (unsigned char *) emalloc(oldlen * 2 * sizeof(char) + 1);
	
	for (i = j = 0; i < oldlen; i++) {
		result[j++] = hexconvtab[old[i] >> 4];
		result[j++] = hexconvtab[old[i] & 15];
	}
	result[j] = '\0';

	if (newlen) 
		*newlen = oldlen * 2 * sizeof(char);

	return (char *)result;
}
/* }}} */

/* {{{ php_charmask
 * Fills a 256-byte bytemask with input. You can specify a range like 'a..z',
 * it needs to be incrementing.  
 * Returns: FAILURE/SUCCESS wether the input was correct (i.e. no range errors)
 */
static inline int php_charmask(unsigned char *input, int len, char *mask TSRMLS_DC)
{
	unsigned char *end;
	unsigned char c;
	int result = SUCCESS;

	memset(mask, 0, 256);
	for (end = input+len; input < end; input++) {
		c=*input; 
		if ((input+3 < end) && input[1] == '.' && input[2] == '.' 
				&& input[3] >= c) {
			memset(mask+c, 1, input[3] - c + 1);
			input+=3;
		} else if ((input+1 < end) && input[0] == '.' && input[1] == '.') {
			/* Error, try to be as helpful as possible:
			   (a range ending/starting with '.' won't be captured here) */
			if (end-len >= input) { /* there was no 'left' char */
				php_error_docref(NULL TSRMLS_CC, E_WARNING, "Invalid '..'-range, no character to the left of '..'.");
				result = FAILURE;
				continue;
			}
			if (input+2 >= end) { /* there is no 'right' char */
				php_error_docref(NULL TSRMLS_CC, E_WARNING, "Invalid '..'-range, no character to the right of '..'.");
				result = FAILURE;
				continue;
			}
			if (input[-1] > input[2]) { /* wrong order */
				php_error_docref(NULL TSRMLS_CC, E_WARNING, "Invalid '..'-range, '..'-range needs to be incrementing.");
				result = FAILURE;
				continue;
			} 
			/* FIXME: better error (a..b..c is the only left possibility?) */
			php_error_docref(NULL TSRMLS_CC, E_WARNING, "Invalid '..'-range.");
			result = FAILURE;
			continue;
		} else {
			mask[c]=1;
		}
	}
	return result;
}
/* }}} */

/* {{{ php_trim()
 * mode 1 : trim left
 * mode 2 : trim right
 * mode 3 : trim left and right
 * what indicates which chars are to be trimmed. NULL->default (' \t\n\r\v\0')
 */
ZEND_API char *php_trim(char *c, int len, char *what, int what_len, zval *return_value, int mode TSRMLS_DC)
{
	register int i;
	int trimmed = 0;
	char mask[256];

	if (what) {
		php_charmask((unsigned char *)what, what_len, mask TSRMLS_CC);
	} else {
		php_charmask((unsigned char *)" \n\r\t\v\0", 6, mask TSRMLS_CC);
	}

	if (mode & 1) {
		for (i = 0; i < len; i++) {
			if (mask[(unsigned char)c[i]]) {
				trimmed++;
			} else {
				break;
			}
		}
		len -= trimmed;
		c += trimmed;
	}
	if (mode & 2) {
		for (i = len - 1; i >= 0; i--) {
			if (mask[(unsigned char)c[i]]) {
				len--;
			} else {
				break;
			}
		}
	}

	if (return_value) {
		RETVAL_STRINGL(c, len, 1);
	} else {
		return estrndup(c, len);
	}
	return "";
}
/* }}} */

/* {{{ php_explode
 */
ZEND_API void php_explode(zval *delim, zval *str, zval *return_value, int limit) 
{
	char *p1, *p2, *endp;

	endp = Z_STRVAL_P(str) + Z_STRLEN_P(str);

	p1 = Z_STRVAL_P(str);
	p2 = php_memnstr(Z_STRVAL_P(str), Z_STRVAL_P(delim), Z_STRLEN_P(delim), endp);

	if (p2 == NULL) {
		add_next_index_stringl(return_value, p1, Z_STRLEN_P(str), 1);
	} else {
		do {
			add_next_index_stringl(return_value, p1, p2 - p1, 1);
			p1 = p2 + Z_STRLEN_P(delim);
		} while ((p2 = php_memnstr(p1, Z_STRVAL_P(delim), Z_STRLEN_P(delim), endp)) != NULL &&
				 (limit == -1 || --limit > 1));

		if (p1 <= endp)
			add_next_index_stringl(return_value, p1, endp-p1, 1);
	}
}
/* }}} */

/* {{{ proto string join(array src, string glue)
   An alias for implode */
/* }}} */

/* {{{ php_implode
 */
ZEND_API void php_implode(zval *delim, zval *arr, zval *return_value) 
{
	zval         **tmp;
	HashPosition   pos;
	smart_str      implstr = {0};
	int            numelems, i = 0;

	numelems = zend_hash_num_elements(Z_ARRVAL_P(arr));

	if (numelems == 0) {
		RETURN_EMPTY_STRING();
	}

	zend_hash_internal_pointer_reset_ex(Z_ARRVAL_P(arr), &pos);

	while (zend_hash_get_current_data_ex(Z_ARRVAL_P(arr), (void **) &tmp, &pos) == SUCCESS) {
		if ((*tmp)->type != IS_STRING) {
			SEPARATE_ZVAL(tmp);
			convert_to_string(*tmp);
		} 
		
		smart_str_appendl(&implstr, Z_STRVAL_PP(tmp), Z_STRLEN_PP(tmp));
		if (++i != numelems) {
			smart_str_appendl(&implstr, Z_STRVAL_P(delim), Z_STRLEN_P(delim));
		}
		zend_hash_move_forward_ex(Z_ARRVAL_P(arr), &pos);
	}
	smart_str_0(&implstr);

	RETURN_STRINGL(implstr.c, implstr.len, 0);
}
/* }}} */

#define STRTOK_TABLE(p) BG(strtok_table)[(unsigned char) *p]	

/* {{{ php_strtoupper
 */
ZEND_API char *php_strtoupper(char *s, size_t len)
{
	unsigned char *c, *e;
	
	c = (unsigned char *)s;
	e = c+len;

	while (c < e) {
		*c = toupper(*c);
		c++;
	}
	return s;
}
/* }}} */

/* {{{ php_strtolower
 */
ZEND_API char *php_strtolower(char *s, size_t len)
{
	unsigned char *c, *e;
	
	c = (unsigned char *)s;
	e = c+len;

	while (c < e) {
		*c = tolower(*c);
		c++;
	}
	return s;
}
/* }}} */

/* {{{ php_basename
 */
void php_basename_ZendV2(char *s, size_t len, char *suffix, size_t sufflen, char **p_ret, size_t *p_len TSRMLS_DC)
{
	char *ret = NULL, *c, *comp, *cend;
	size_t inc_len, cnt;
	int state;

	c = comp = cend = s;
	cnt = len;
	state = 0;
	while (cnt > 0) {
		inc_len = (*c == '\0' ? 1: php_mblen(c, cnt));

		switch (inc_len) {
			case -2:
			case -1:
				inc_len = 1;
				php_mblen(NULL, 0);
				break;
			case 0:
				goto quit_loop;
			case 1:
#ifdef PHP_WIN32
				if (*c == '/' || *c == '\\') {
#else
				if (*c == '/') {
#endif
					if (state == 1) {
						state = 0;
						cend = c;
					}
				} else {
					if (state == 0) {
						comp = c;
						state = 1;
					}
				}
			default:
				break;
		}
		c += inc_len;
		cnt -= inc_len;
	}

quit_loop:
	if (state == 1) {
		cend = c;
	}
	if (suffix != NULL && sufflen < (size_t)(cend - comp) &&
			memcmp(cend - sufflen, suffix, sufflen) == 0) {
		cend -= sufflen;
	}

	len = cend - comp;
	ret = (char *)emalloc(len + 1);
	memcpy(ret, comp, len);
	ret[len] = '\0';

	if (p_ret) {
		*p_ret = ret;
	}
	if (p_len) {
		*p_len = len;
	}
}
/* }}} */

/* {{{ php_basename
 */
ZEND_API char *php_basename(char *s, size_t len, char *suffix, size_t sufflen)
{
	char *ret=NULL, *c, *p=NULL, buf='\0', *p2=NULL, buf2='\0';
	c = s + len - 1;	

	/* do suffix removal as the unix command does */
	if (suffix && (len > sufflen)) {
		if (!strncmp(suffix, c-sufflen+1, sufflen)) {
			c -= sufflen; 
			buf2 = *(c + 1); /* Save overwritten char */
			*(c + 1) = '\0'; /* overwrite char */
			p2 = c + 1;      /* Save pointer to overwritten char */
		}
	}


	/* strip trailing slashes */
	while (*c == '/'
#ifdef PHP_WIN32
		   || (*c == '\\' && !IsDBCSLeadByte(*(c-1)))
#endif
		)
		c--;
	if (c < s+len-1) {
		buf = *(c + 1);  /* Save overwritten char */
		*(c + 1) = '\0'; /* overwrite char */
		p = c + 1;       /* Save pointer to overwritten char */
	}

#ifdef PHP_WIN32
	if ((c = strrchr(s, '/')) || ((c = strrchr(s, '\\')) && !IsDBCSLeadByte(*(c-1)))) {
		if (*c == '/') {
			char *c2 = strrchr(s, '\\');
			if (c2 && !IsDBCSLeadByte(*(c2-1)) && c2 > c) {
				c = c2;
			}
		}
#else 
	if ((c = strrchr(s, '/'))) {
#endif
		ret = estrdup(c + 1);
	} else {
		ret = estrdup(s);
	}
	if (buf) *p = buf;
	if (buf2) *p2 = buf2;
	return (ret);
}
/* }}} */

/* {{{ php_dirname
   Returns directory name component of path */
ZEND_API size_t php_dirname(char *path, size_t len)
{
	register char *end = path + len - 1;
	unsigned int len_adjust = 0;

#ifdef PHP_WIN32
	/* Note that on Win32 CWD is per drive (heritage from CP/M).
	 * This means dirname("c:foo") maps to "c:." or "c:" - which means CWD on C: drive.
	 */
	if ((2 <= len) && isalpha((int)((unsigned char *)path)[0]) && (':' == path[1])) {
		/* Skip over the drive spec (if any) so as not to change */
		path += 2;
		len_adjust += 2;
		if (2 == len) {
			/* Return "c:" on Win32 for dirname("c:").
			 * It would be more consistent to return "c:." 
			 * but that would require making the string *longer*.
			 */
			return len;
		}
	}
#endif

	if (len == 0) {
		/* Illegal use of this function */
		return 0;
	}

	/* Strip trailing slashes */
	while (end >= path && IS_SLASH_P(end)) {
		end--;
	}
	if (end < path) {
		/* The path only contained slashes */
		path[0] = DEFAULT_SLASH;
		path[1] = '\0';
		return 1 + len_adjust;
	}

	/* Strip filename */
	while (end >= path && !IS_SLASH_P(end)) {
		end--;
	}
	if (end < path) {
		/* No slash found, therefore return '.' */
		path[0] = '.';
		path[1] = '\0';
		return 1 + len_adjust;
	}

	/* Strip slashes which came before the file name */
	while (end >= path && IS_SLASH_P(end)) {
		end--;
	}
	if (end < path) {
		path[0] = DEFAULT_SLASH;
		path[1] = '\0';
		return 1 + len_adjust;
	}
	*(end+1) = '\0';

	return (size_t)(end + 1 - path) + len_adjust;
}
/* }}} */

/* {{{ php_stristr
   case insensitve strstr */
ZEND_API char *php_stristr(unsigned char *s, unsigned char *t, size_t s_len, size_t t_len)
{
	php_strtolower((char *)s, s_len);
	php_strtolower((char *)t, t_len);
	return php_memnstr((char *)s, (char *)t, t_len, (char *)(s + s_len));
}
/* }}} */

/* {{{ php_strspn
 */
ZEND_API size_t php_strspn(char *s1, char *s2, char *s1_end, char *s2_end)
{
	register const char *p = s1, *spanp;
	register char c = *p;

cont:
	for (spanp = s2; p != s1_end && spanp != s2_end;) {
		if (*spanp++ == c) {
			c = *(++p);
			goto cont;
		}
	}
	return (p - s1);
}
/* }}} */

/* {{{ php_strcspn
 */
ZEND_API size_t php_strcspn(char *s1, char *s2, char *s1_end, char *s2_end)
{
	register const char *p, *spanp;
	register char c = *s1;

	for (p = s1;;) {
		spanp = s2;
		do {
			if (*spanp == c || p == s1_end) {
				return p - s1;
			}
		} while (spanp++ < s2_end);
		c = *++p;
	}
	/* NOTREACHED */
}
/* }}} */

/* {{{ php_chunk_split
 */
static char *php_chunk_split(char *src, int srclen, char *end, int endlen, int chunklen, int *destlen)
{
	char *dest;
	char *p, *q;
	int chunks; /* complete chunks! */
	int restlen;

	chunks = srclen / chunklen;
	restlen = srclen - chunks * chunklen; /* srclen % chunklen */

	dest = (char *)emalloc((srclen + (chunks + 1) * endlen + 1) * sizeof(char));

	for (p = src, q = dest; p < (src + srclen - chunklen + 1); ) {
		memcpy(q, p, chunklen);
		q += chunklen;
		memcpy(q, end, endlen);
		q += endlen;
		p += chunklen;
	}

	if (restlen) {
		memcpy(q, p, restlen);
		q += restlen;
		memcpy(q, end, endlen);
		q += endlen;
	}

	*q = '\0';
	if (destlen) {
		*destlen = q - dest;
	}

	return(dest);
}
/* }}} */

/* {{{ php_strtr
 */
ZEND_API char *php_strtr(char *str, int len, char *str_from, char *str_to, int trlen)
{
	int i;
	unsigned char xlat[256];

	if ((trlen < 1) || (len < 1)) {
		return str;
	}

	for (i = 0; i < 256; xlat[i] = i, i++);

	for (i = 0; i < trlen; i++) {
		xlat[(unsigned char) str_from[i]] = str_to[i];
	}

	for (i = 0; i < len; i++) {
		str[i] = xlat[(unsigned char) str[i]];
	}

	return str;
}
/* }}} */

/* {{{ php_similar_char
 */
static int php_similar_char(const char *txt1, int len1, const char *txt2, int len2)
{
	int sum;
	int pos1, pos2, max;

	php_similar_str(txt1, len1, txt2, len2, &pos1, &pos2, &max);
	if ((sum = max)) {
		if (pos1 && pos2) {
			sum += php_similar_char(txt1, pos1, 
									txt2, pos2);
		}
		if ((pos1 + max < len1) && (pos2 + max < len2)) {
			sum += php_similar_char(txt1 + pos1 + max, len1 - pos1 - max, 
									txt2 + pos2 + max, len2 - pos2 - max);
		}
	}

	return sum;
}
/* }}} */

/* {{{ php_stripslashes
 *
 * be careful, this edits the string in-place */
ZEND_API void php_stripslashes(char *str, int *len TSRMLS_DC)
{
	char *s, *t;
	int l;

	if (len != NULL) {
		l = *len;
	} else {
		l = strlen(str);
	}
	s = str;
	t = str;

	if (PG(magic_quotes_sybase)) {
		while (l > 0) {
			if (*t == '\'') {
				if ((l > 0) && (t[1] == '\'')) {
					t++;
					if (len != NULL) {
						(*len)--;
					}
					l--;
				}
				*s++ = *t++;
			} else if (*t == '\\' && t[1] == '0' && l > 0) {
				*s++='\0';
				t+=2;
				if (len != NULL) {
					(*len)--;
				}
				l--;
			} else {
				*s++ = *t++;
			}
			l--;
		}
		*s = '\0';
		
		return;
	}

	while (l > 0) {
		if (*t == '\\') {
			t++;				/* skip the slash */
			if (len != NULL) {
				(*len)--;
			}
			l--;
			if (l > 0) {
				if (*t == '0') {
					*s++='\0';
					t++;
				} else {
					*s++ = *t++;	/* preserve the next character */
				}
				l--;
			}
		} else {
			*s++ = *t++;
			l--;
		}
	}
	if (s != t) {
		*s = '\0';
	}
}
/* }}} */

/* {{{ php_stripcslashes
 */
ZEND_API void php_stripcslashes(char *str, int *len)
{
	char *source, *target, *end;
	int  nlen = *len, i;
	char numtmp[4];

	for (source=str, end=str+nlen, target=str; source < end; source++) {
		if (*source == '\\' && source+1 < end) {
			source++;
			switch (*source) {
				case 'n':  *target++='\n'; nlen--; break;
				case 'r':  *target++='\r'; nlen--; break;
				case 'a':  *target++='\a'; nlen--; break;
				case 't':  *target++='\t'; nlen--; break;
				case 'v':  *target++='\v'; nlen--; break;
				case 'b':  *target++='\b'; nlen--; break;
				case 'f':  *target++='\f'; nlen--; break;
				case '\\': *target++='\\'; nlen--; break;
				case 'x':
					if (source+1 < end && isxdigit((int)(*(source+1)))) {
						numtmp[0] = *++source;
						if (source+1 < end && isxdigit((int)(*(source+1)))) {
							numtmp[1] = *++source;
							numtmp[2] = '\0';
							nlen-=3;
						} else {
							numtmp[1] = '\0';
							nlen-=2;
						}
						*target++=(char)strtol(numtmp, NULL, 16);
						break;
					}
					/* break is left intentionally */
				default: 
					i=0; 
					while (source < end && *source >= '0' && *source <= '7' && i<3) {
						numtmp[i++] = *source++;
					}
					if (i) {
						numtmp[i]='\0';
						*target++=(char)strtol(numtmp, NULL, 8);
						nlen-=i;
						source--;
					} else {
						*target++=*source;
						nlen--;
					}
			}
		} else {
			*target++=*source;
		}
	}

	if (nlen != 0) {
		*target='\0';
	}

	*len = nlen;
}
/* }}} */
			
/* {{{ php_addcslashes
 */
ZEND_API char *php_addcslashes(char *str, int length, int *new_length, int should_free, char *what, int wlength TSRMLS_DC)
{
	char flags[256];
	char *new_str = (char *)emalloc((length?length:(length=strlen(str)))*4+1); 
	char *source, *target;
	char *end;
	char c;
	int  newlen;

	if (!wlength) {
		wlength = strlen(what);
	}

	if (!length) {
		length = strlen(str);
	}

	php_charmask((unsigned char *)what, wlength, flags TSRMLS_CC);

	for (source = str, end = source + length, target = new_str; (c = *source) || (source < end); source++) {
		if (flags[(unsigned char)c]) {
			if ((unsigned char) c < 32 || (unsigned char) c > 126) {
				*target++ = '\\';
				switch (c) {
					case '\n': *target++ = 'n'; break;
					case '\t': *target++ = 't'; break;
					case '\r': *target++ = 'r'; break;
					case '\a': *target++ = 'a'; break;
					case '\v': *target++ = 'v'; break;
					case '\b': *target++ = 'b'; break;
					case '\f': *target++ = 'f'; break;
					default: target += sprintf(target, "%03o", (unsigned char) c);
				}
				continue;
			} 
			*target++ = '\\';
		}
		*target++ = c;
	}
	*target = 0;
	newlen = target - new_str;
	if (target - new_str < length * 4) {
		new_str = (char *)erealloc(new_str, newlen + 1);
	}
	if (new_length) {
		*new_length = newlen;
	}
	if (should_free) {
		STR_FREE(str);
	}
	return new_str;
}
/* }}} */

/* {{{ php_addslashes
 */
ZEND_API char *php_addslashes(char *str, int length, int *new_length, int should_free TSRMLS_DC)
{
	return php_addslashes_ex(str, length, new_length, should_free, 0 TSRMLS_CC);
}
/* }}} */

/* true static */
const unsigned char php_esc_list[256] = {2,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,0,0,0,0,3,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0};

/* {{{ php_addslashes_ex
 */
ZEND_API char *php_addslashes_ex(char *str, int length, int *new_length, int should_free, int ignore_sybase TSRMLS_DC)
{
	char *e = str + (length ? length : (length = strlen(str)));
	char *p = str;
	char *new_str, *ps;
	int local_new_length = length;
	int type = (!ignore_sybase && PG(magic_quotes_sybase)) ? 1 : 0;

	if (!new_length) {
		new_length = &local_new_length;
	}

	if (!str) {
		*new_length = 0;
		return str;
	}

	/* determine the number of the characters that need to be escaped */
	while (p < e) {
		if (php_esc_list[(int)(unsigned char)*p++] > type) {
			local_new_length++;
		}
	}

	/* string does not have any escapable characters */
	if (local_new_length == length) {
		new_str = estrndup(str, length);
		goto done;
	}

	/* create escaped string */
	ps = new_str = (char *)emalloc(local_new_length + 1);
	p = str;
	if (!type) {
		while (p < e) {
			int c = php_esc_list[(int)(unsigned char)*p];
			if (c == 2) {
				*ps++ = '\\';
				*ps++ = '0';
				p++;
				continue;
			} else if (c) {
				*ps++ = '\\';
			}
			*ps++ = *p++;
		}
	} else {
		while (p < e) {
			switch (php_esc_list[(int)(unsigned char)*p]) {
				case 2:
					*ps++ = '\\';
					*ps++ = '0';
					p++;
					break;
			
				case 3:
					*ps++ = '\'';
					*ps++ = '\'';
					p++;
					break;

				default:
					*ps++ = *p++;
					break;
			}
		}
	}
	*ps = '\0';

done:
	if (should_free) {
		STR_FREE(str);
	}
	*new_length = local_new_length;

	return new_str;
}
/* }}} */

#define _HEB_BLOCK_TYPE_ENG 1
#define _HEB_BLOCK_TYPE_HEB 2
#define isheb(c)      (((((unsigned char) c) >= 224) && (((unsigned char) c) <= 250)) ? 1 : 0)
#define _isblank(c)   (((((unsigned char) c) == ' '  || ((unsigned char) c) == '\t')) ? 1 : 0)
#define _isnewline(c) (((((unsigned char) c) == '\n' || ((unsigned char) c) == '\r')) ? 1 : 0)

/* {{{ php_char_to_str
 */
ZEND_API int php_char_to_str(char *str, uint len, char from, char *to, int to_len, zval *result)
{
	int char_count = 0;
	int replaced = 0;
	char *source, *target, *tmp, *source_end=str+len, *tmp_end = NULL;
	
	for (source = str; source < source_end; source++) {
		if (*source == from) {
			char_count++;
		}
	}

	if (char_count == 0) {
		ZVAL_STRINGL(result, str, len, 1);
		return 0;
	}
	
	Z_STRLEN_P(result) = len + (char_count * (to_len - 1));
	Z_STRVAL_P(result) = target = (char *)emalloc(Z_STRLEN_P(result) + 1);
	Z_TYPE_P(result) = IS_STRING;
	
	for (source = str; source < source_end; source++) {
		if (*source == from) {
			replaced = 1;
			for (tmp = to, tmp_end = tmp+to_len; tmp < tmp_end; tmp++) {
				*target = *tmp;
				target++;
			}
		} else {
			*target = *source;
			target++;
		}
	}
	*target = 0;
	return replaced;
}
/* }}} */

/* {{{ php_str_to_str_ex
 */
ZEND_API char *php_str_to_str_ex(char *haystack, int length, 
	char *needle, int needle_len, char *str, int str_len, int *_new_length, int case_sensitivity, int *replace_count)
{
	char *new_str;

	if (needle_len < length) {
		char *end, *haystack_dup, *needle_dup;
		char *e, *s, *p, *r;

		if (needle_len == str_len) {
			new_str = estrndup(haystack, length);
			*_new_length = length;

			if (case_sensitivity) {
				end = new_str + length;
				for (p = new_str; (r = php_memnstr(p, needle, needle_len, end)); p = r + needle_len) {
					memcpy(r, str, str_len);
					if (replace_count) {
						(*replace_count)++;
					}
				}
			} else {
				haystack_dup = estrndup(haystack, length);
				needle_dup = estrndup(needle, needle_len);
				php_strtolower(haystack_dup, length);
				php_strtolower(needle_dup, needle_len);
				end = haystack_dup + length;
				for (p = haystack_dup; (r = php_memnstr(p, needle_dup, needle_len, end)); p = r + needle_len) {
					memcpy(new_str + (r - haystack_dup), str, str_len);
					if (replace_count) {
						(*replace_count)++;
					}
				}
				efree(haystack_dup);
				efree(needle_dup);
			}
			return new_str;
		} else {
			if (str_len < needle_len) {
				new_str = (char *)emalloc(length + 1);
			} else {
				new_str = (char *)emalloc((length / needle_len + 1) * str_len);
			}

			e = s = new_str;

			if (case_sensitivity) {
				end = haystack + length;
				for (p = haystack; (r = php_memnstr(p, needle, needle_len, end)); p = r + needle_len) {
					memcpy(e, p, r - p);
					e += r - p;
					memcpy(e, str, str_len);
					e += str_len;
					if (replace_count) {
						(*replace_count)++;
					}
				}

				if (p < end) {
					memcpy(e, p, end - p);
					e += end - p;
				}
			} else {
				haystack_dup = estrndup(haystack, length);
				needle_dup = estrndup(needle, needle_len);
				php_strtolower(haystack_dup, length);
				php_strtolower(needle_dup, needle_len);

				end = haystack_dup + length;

				for (p = haystack_dup; (r = php_memnstr(p, needle_dup, needle_len, end)); p = r + needle_len) {
					memcpy(e, haystack + (p - haystack_dup), r - p);
					e += r - p;
					memcpy(e, str, str_len);
					e += str_len;
					if (replace_count) {
						(*replace_count)++;
					}
				}

				if (p < end) {
					memcpy(e, haystack + (p - haystack_dup), end - p);
					e += end - p;
				}
				efree(haystack_dup);
				efree(needle_dup);
			}

			*e = '\0';
			*_new_length = e - s;

			new_str = (char *)erealloc(new_str, *_new_length + 1);
			return new_str;
		}
	} else if (needle_len > length) {
nothing_todo:
		*_new_length = length;
		new_str = estrndup(haystack, length);
		return new_str;
	} else {
		if (case_sensitivity ? strncmp(haystack, needle, length) : strncasecmp(haystack, needle, length)) {
			goto nothing_todo;
		} else {
			*_new_length = str_len;
			new_str = estrndup(str, str_len);
			if (replace_count) {
				(*replace_count)++;
			}
			return new_str;
		}
	}

}
/* }}} */

/* {{{ php_str_to_str
 */
ZEND_API char *php_str_to_str(char *haystack, int length, 
	char *needle, int needle_len, char *str, int str_len, int *_new_length)
{
	return php_str_to_str_ex(haystack, length, needle, needle_len, str, str_len, _new_length, 1, NULL);
} 
/* }}}
 */

/* {{{ php_str_replace_in_subject
 */
static void php_str_replace_in_subject(zval *search, zval *replace, zval **subject, zval *result, int case_sensitivity, int *replace_count)
{
	zval		**search_entry,
				**replace_entry = NULL,
				  temp_result;
	char		*replace_value = NULL;
	int			 replace_len = 0;

	/* Make sure we're dealing with strings. */	
	convert_to_string_ex(subject);
	Z_TYPE_P(result) = IS_STRING;
	if (Z_STRLEN_PP(subject) == 0) {
		ZVAL_STRINGL(result, empty_string, 0, 1);
		return;
	}
	
	/* If search is an array */
	if (Z_TYPE_P(search) == IS_ARRAY) {
		/* Duplicate subject string for repeated replacement */
		*result = **subject;
		zval_copy_ctor(result);
		INIT_PZVAL(result);
		
		zend_hash_internal_pointer_reset(Z_ARRVAL_P(search));

		if (Z_TYPE_P(replace) == IS_ARRAY) {
			zend_hash_internal_pointer_reset(Z_ARRVAL_P(replace));
		} else {
			/* Set replacement value to the passed one */
			replace_value = Z_STRVAL_P(replace);
			replace_len = Z_STRLEN_P(replace);
		}

		/* For each entry in the search array, get the entry */
		while (zend_hash_get_current_data(Z_ARRVAL_P(search), (void **) &search_entry) == SUCCESS) {
			/* Make sure we're dealing with strings. */	
			SEPARATE_ZVAL(search_entry);
			convert_to_string(*search_entry);
			if (Z_STRLEN_PP(search_entry) == 0) {
				zend_hash_move_forward(Z_ARRVAL_P(search));
				if (Z_TYPE_P(replace) == IS_ARRAY) {
					zend_hash_move_forward(Z_ARRVAL_P(replace));
				}
				continue;
			}

			/* If replace is an array. */
			if (Z_TYPE_P(replace) == IS_ARRAY) {
				/* Get current entry */
				if (zend_hash_get_current_data(Z_ARRVAL_P(replace), (void **)&replace_entry) == SUCCESS) {
					/* Make sure we're dealing with strings. */	
					convert_to_string_ex(replace_entry);
					
					/* Set replacement value to the one we got from array */
					replace_value = Z_STRVAL_PP(replace_entry);
					replace_len = Z_STRLEN_PP(replace_entry);

					zend_hash_move_forward(Z_ARRVAL_P(replace));
				} else {
					/* We've run out of replacement strings, so use an empty one. */
					replace_value = empty_string;
					replace_len = 0;
				}
			}
			
			if (Z_STRLEN_PP(search_entry) == 1) {
				php_char_to_str(Z_STRVAL_P(result),
								Z_STRLEN_P(result),
								Z_STRVAL_PP(search_entry)[0],
								replace_value,
								replace_len,
								&temp_result);
			} else if (Z_STRLEN_PP(search_entry) > 1) {
				Z_STRVAL(temp_result) = php_str_to_str_ex(Z_STRVAL_P(result), Z_STRLEN_P(result),
														   Z_STRVAL_PP(search_entry), Z_STRLEN_PP(search_entry),
														   replace_value, replace_len, &Z_STRLEN(temp_result), case_sensitivity, replace_count);
			}

			efree(Z_STRVAL_P(result));
			Z_STRVAL_P(result) = Z_STRVAL(temp_result);
			Z_STRLEN_P(result) = Z_STRLEN(temp_result);

			if (Z_STRLEN_P(result) == 0) {
				return;
			}

			zend_hash_move_forward(Z_ARRVAL_P(search));
		}
	} else {
		if (Z_STRLEN_P(search) == 1) {
			php_char_to_str(Z_STRVAL_PP(subject),
							Z_STRLEN_PP(subject),
							Z_STRVAL_P(search)[0],
							Z_STRVAL_P(replace),
							Z_STRLEN_P(replace),
							result);
		} else if (Z_STRLEN_P(search) > 1) {
			Z_STRVAL_P(result) = php_str_to_str_ex(Z_STRVAL_PP(subject), Z_STRLEN_PP(subject),
													Z_STRVAL_P(search), Z_STRLEN_P(search),
													Z_STRVAL_P(replace), Z_STRLEN_P(replace), &Z_STRLEN_P(result), case_sensitivity, replace_count);
		} else {
			*result = **subject;
			zval_copy_ctor(result);
			INIT_PZVAL(result);
		}
	}
}
/* }}} */

#define PHP_TAG_BUF_SIZE 1023

/* {{{ php_tag_find
 *
 * Check if tag is in a set of tags 
 *
 * states:
 * 
 * 0 start tag
 * 1 first non-whitespace char seen
 */
int php_tag_find(char *tag, int len, char *set) {
	char c, *n, *t;
	int state=0, done=0;
	char *norm = (char *)emalloc(len+1);

	n = norm;
	t = tag;
	c = tolower(*t);
	/* 
	   normalize the tag removing leading and trailing whitespace
	   and turn any <a whatever...> into just <a> and any </tag>
	   into <tag>
	*/
	if (!len) {
		return 0;
	}
	while (!done) {
		switch (c) {
			case '<':
				*(n++) = c;
				break;
			case '>':
				done =1;
				break;
			default:
				if (!isspace((int)c)) {
					if (state == 0) {
						state=1;
						if (c != '/')
							*(n++) = c;
					} else {
						*(n++) = c;
					}
				} else {
					if (state == 1)
						done=1;
				}
				break;
		}
		c = tolower(*(++t));
	}  
	*(n++) = '>';
	*n = '\0'; 
	if (strstr(set, norm)) {
		done=1;
	} else {
		done=0;
	}
	efree(norm);
	return done;
}
/* }}} */

/* {{{ php_strip_tags
 
	A simple little state-machine to strip out html and php tags 
	
	State 0 is the output state, State 1 means we are inside a
	normal html tag and state 2 means we are inside a php tag.

	The state variable is passed in to allow a function like fgetss
	to maintain state across calls to the function.

	lc holds the last significant character read and br is a bracket
	counter.

	When an allow string is passed in we keep track of the string
	in state 1 and when the tag is closed check it against the
	allow string to see if we should allow it.

	swm: Added ability to strip <?xml tags without assuming it PHP
	code.
*/
ZEND_API size_t php_strip_tags(char *rbuf, int len, int *stateptr, char *allow, int allow_len)
{
	char *tbuf, *buf, *p, *tp, *rp, c, lc;
	int br, i=0, depth=0;
	int state = 0;

	if (stateptr)
		state = *stateptr;

	buf = estrndup(rbuf, len);
	c = *buf;
	lc = '\0';
	p = buf;
	rp = rbuf;
	br = 0;
	if (allow) {
		php_strtolower(allow, allow_len);
		tbuf = (char *)emalloc(PHP_TAG_BUF_SIZE+1);
		tp = tbuf;
	} else {
		tbuf = tp = NULL;
	}

	while (i < len) {
		switch (c) {
			case '<':
				if (isspace(*(p + 1))) {
					goto reg_char;
				}
				if (state == 0) {
					lc = '<';
					state = 1;
					if (allow) {
						*(tp++) = '<';
					}
				} else if (state == 1) {
					depth++;
				}
				break;

			case '(':
				if (state == 2) {
					if (lc != '"' && lc != '\'') {
						lc = '(';
						br++;
					}
				} else if (allow && state == 1) {
					*(tp++) = c;
				} else if (state == 0) {
					*(rp++) = c;
				}
				break;	

			case ')':
				if (state == 2) {
					if (lc != '"' && lc != '\'') {
						lc = ')';
						br--;
					}
				} else if (allow && state == 1) {
					*(tp++) = c;
				} else if (state == 0) {
					*(rp++) = c;
				}
				break;	

			case '>':
				if (depth) {
					depth--;
					break;
				}
			
				switch (state) {
					case 1: /* HTML/XML */
						lc = '>';
						state = 0;
						if (allow) {
							*(tp++) = '>';
							*tp='\0';
							if (php_tag_find(tbuf, tp-tbuf, allow)) {
								memcpy(rp, tbuf, tp-tbuf);
								rp += tp-tbuf;
							}
							tp = tbuf;
						}
						break;
						
					case 2: /* PHP */
						if (!br && lc != '\"' && *(p-1) == '?') {
							state = 0;
							tp = tbuf;
						}
						break;
						
					case 3:
						state = 0;
						tp = tbuf;
						break;

					case 4: /* JavaScript/CSS/etc... */
						if (p >= buf + 2 && *(p-1) == '-' && *(p-2) == '-') {
							state = 0;
							tp = tbuf;
						}
						break;

					default:
						*(rp++) = c;
						break;
				}
				break;

			case '"':
			case '\'':
				if (state == 2 && *(p-1) != '\\') {
					if (lc == c) {
						lc = '\0';
					} else if (lc != '\\') {
						lc = c;
					}
				} else if (state == 0) {
					*(rp++) = c;
				} else if (allow && state == 1) {
					*(tp++) = c;
				}
				break;
			
			case '!': 
				/* JavaScript & Other HTML scripting languages */
				if (state == 1 && *(p-1) == '<') { 
					state = 3;
					lc = c;
				} else {
					if (state == 0) {
						*(rp++) = c;
					} else if (allow && state == 1) {
						*(tp++) = c;
						if ( (tp-tbuf) >= PHP_TAG_BUF_SIZE ) {
							/* prevent buffer overflows */
							tp = tbuf;
						}
					}
				}
				break;

			case '-':
				if (state == 3 && p >= buf + 2 && *(p-1) == '-' && *(p-2) == '!') {
					state = 4;
				} else {
					goto reg_char;
				}
				break;

			case '?':

				if (state == 1 && *(p-1)=='<') { 
					br=0;
					state=2;
					break;
				}

			case 'E':
			case 'e':
				/* !DOCTYPE exception */
				if (state==3 && p > buf+6
						     && tolower(*(p-1)) == 'p'
					         && tolower(*(p-2)) == 'y'
						     && tolower(*(p-3)) == 't'
						     && tolower(*(p-4)) == 'c'
						     && tolower(*(p-5)) == 'o'
						     && tolower(*(p-6)) == 'd') {
					state = 1;
					break;
				}
				/* fall-through */

			case 'l':

				/* swm: If we encounter '<?xml' then we shouldn't be in
				 * state == 2 (PHP). Switch back to HTML.
				 */

				if (state == 2 && p > buf+2 && *(p-1) == 'm' && *(p-2) == 'x') {
					state = 1;
					break;
				}

				/* fall-through */
			default:
reg_char:
				if (state == 0) {
					*(rp++) = c;
				} else if (allow && state == 1) {
					*(tp++) = c;
					if ( (tp-tbuf) >= PHP_TAG_BUF_SIZE ) { /* no buffer overflows */
						tp = tbuf;
					}
				} 
				break;
		}
		c = *(++p);
		i++;
	}	
	if (rp < rbuf + len) {
		*rp = '\0';
	}
	efree(buf);
	if (allow)
		efree(tbuf);
	if (stateptr)
		*stateptr = state;

	return (size_t)(rp - rbuf);
}
/* }}} */

static char rot13_from[] = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
static char rot13_to[] = "nopqrstuvwxyzabcdefghijklmNOPQRSTUVWXYZABCDEFGHIJKLM";

static void php_string_shuffle(char *str, long len TSRMLS_DC)
{
	long n_elems, rnd_idx, n_left;
	char temp;
	/* The implementation is stolen from array_data_shuffle       */
	/* Thus the characteristics of the randomization are the same */
	n_elems = len;
	
	if (n_elems <= 1) {
		return;
	}

	n_left = n_elems;
	
	while (--n_left) {
		rnd_idx = php_rand(TSRMLS_C);
		RAND_RANGE(rnd_idx, 0, n_left, PHP_RAND_MAX);
		if (rnd_idx != n_left) {
			temp = str[n_left];
			str[n_left] = str[rnd_idx];
			str[rnd_idx] = temp;
		}
	}
}

// copied from zend_alloc.c and beautified
ZEND_API char *zend_strndup(const char *s, uint length)
{
	char *p;

	p = (char *)malloc(length + 1);
	if (!p) return NULL;

	if (length) memcpy(p, s, length);

	p[length] = 0;
	return p;
}

// copied from strnatcmp.c and beautified
static int compare_right(char const **a, char const *aend, char const **b, char const *bend)
{
	int bias = 0;

	/* The longest run of digits wins.  That aside, the greatest
	   value wins, but we can't know that it will until we've scanned
	   both numbers to know that they have the same magnitude, so we
	   remember it in BIAS. */
	for(;; (*a)++, (*b)++)
	{
		if ((*a == aend || !isdigit((int)(unsigned char)**a)) &&
			(*b == bend || !isdigit((int)(unsigned char)**b))) return bias;
		else if (*a == aend || !isdigit((int)(unsigned char)**a)) return -1;
		else if (*b == bend || !isdigit((int)(unsigned char)**b)) return +1;
		else if (**a < **b)
		{
			if (!bias) bias = -1;
		}
		else if (**a > **b)
		{
			if (!bias) bias = +1;
		}
     }

     return 0;
}

// copied from strnatcmp.c and beautified
static int compare_left(char const **a, char const *aend, char const **b, char const *bend)
{
     /* Compare two left-aligned numbers: the first to have a
        different value wins. */
	for(;; (*a)++, (*b)++)
	{
		if ((*a == aend || !isdigit((int)(unsigned char)**a)) &&
			(*b == bend || !isdigit((int)(unsigned char)**b))) return 0;
		else if (*a == aend || !isdigit((int)(unsigned char)**a)) return -1;
		else if (*b == bend || !isdigit((int)(unsigned char)**b)) return +1;
		else if (**a < **b) return -1;
		else if (**a > **b) return +1;
	}
	  
	return 0;
}

// copied from strnatcmp.c and beautified
ZEND_API int strnatcmp_ex(char const *a, size_t a_len, char const *b, size_t b_len, int fold_case)
{
	char ca, cb;
	char const *ap, *bp;
	char const *aend = a + a_len, *bend = b + b_len;
	int fractional, result;

	if (a_len == 0 || b_len == 0) return a_len - b_len;

	ap = a;
	bp = b;
	while (1)
	{
		ca = *ap; cb = *bp;

		/* skip over leading spaces or zeros */
		while (isspace((int)(unsigned char)ca)) ca = *++ap;
		while (isspace((int)(unsigned char)cb)) cb = *++bp;

		/* process run of digits */
		if (isdigit((int)(unsigned char)ca)  &&  isdigit((int)(unsigned char)cb))
		{
			fractional = (ca == '0' || cb == '0');

			if (fractional) result = compare_left(&ap, aend, &bp, bend);
			else result = compare_right(&ap, aend, &bp, bend);

			if (result != 0) return result;
			else if (ap == aend && bp == bend)
			{
				/* End of the strings. Let caller sort them out. */
				return 0;
			}
			else
			{
				/* Keep on comparing from the current point. */
				ca = *ap; cb = *bp;
			}
		}

		if (fold_case)
		{
			ca = toupper((int)(unsigned char)ca);
			cb = toupper((int)(unsigned char)cb);
		}

		if (ca < cb) return -1;
		else if (ca > cb) return +1;

		++ap; ++bp;
		if (ap == aend && bp == bend)
		{
			/* The strings compare the same.  Perhaps the caller
			   will want to call strcmp to break the tie. */
			return 0;
		}
		else if (ap == aend) return -1;
		else if (bp == bend) return 1;
	}
}

static const struct
{
	const char *codeset;
	enum entity_charset charset;
}
charset_map[] = {
	{ "ISO-8859-1", 	cs_8859_1 },
	{ "ISO8859-1",	 	cs_8859_1 },
	{ "ISO-8859-15", 	cs_8859_15 },
	{ "ISO8859-15", 	cs_8859_15 },
	{ "utf-8", 			cs_utf_8 },
	{ "cp1252", 		cs_cp1252 },
	{ "Windows-1252", 	cs_cp1252 },
	{ "1252",           cs_cp1252 }, 
	{ "BIG5",			cs_big5 },
	{ "950",            cs_big5 },
	{ "GB2312",			cs_gb2312 },
	{ "936",            cs_gb2312 },
	{ "BIG5-HKSCS",		cs_big5hkscs },
	{ "Shift_JIS",		cs_sjis },
	{ "SJIS",   		cs_sjis },
	{ "932",            cs_sjis },
	{ "EUCJP",   		cs_eucjp },
	{ "EUC-JP",   		cs_eucjp },
	{ "KOI8-R",         cs_koi8r },
	{ "koi8-ru",        cs_koi8r },
	{ "koi8r",          cs_koi8r },
	{ "cp1251",         cs_cp1251 },
	{ "Windows-1251",   cs_cp1251 },
	{ "win-1251",       cs_cp1251 },
	{ "iso8859-5",      cs_8859_5 },
	{ "iso-8859-5",     cs_8859_5 },
	{ "cp866",          cs_cp866 },
	{ "866",            cs_cp866 },    
	{ "ibm866",         cs_cp866 },
	{ NULL }
};

typedef const char *entity_table_t;

/* codepage 1252 is a Windows extension to iso-8859-1. */
static entity_table_t ent_cp_1252[] = {
	"euro", NULL, "sbquo", "fnof", "bdquo", "hellip", "dagger",
	"Dagger", "circ", "permil", "Scaron", "lsaquo", "OElig",
	NULL, NULL, NULL, NULL, "lsquo", "rsquo", "ldquo", "rdquo",
	"bull", "ndash", "mdash", "tilde", "trade", "scaron", "rsaquo",
	"oelig", NULL, NULL, "Yuml" 
};

static entity_table_t ent_iso_8859_1[] = {
	"nbsp", "iexcl", "cent", "pound", "curren", "yen", "brvbar",
	"sect", "uml", "copy", "ordf", "laquo", "not", "shy", "reg",
	"macr", "deg", "plusmn", "sup2", "sup3", "acute", "micro",
	"para", "middot", "cedil", "sup1", "ordm", "raquo", "frac14",
	"frac12", "frac34", "iquest", "Agrave", "Aacute", "Acirc",
	"Atilde", "Auml", "Aring", "AElig", "Ccedil", "Egrave",
	"Eacute", "Ecirc", "Euml", "Igrave", "Iacute", "Icirc",
	"Iuml", "ETH", "Ntilde", "Ograve", "Oacute", "Ocirc", "Otilde",
	"Ouml", "times", "Oslash", "Ugrave", "Uacute", "Ucirc", "Uuml",
	"Yacute", "THORN", "szlig", "agrave", "aacute", "acirc",
	"atilde", "auml", "aring", "aelig", "ccedil", "egrave",
	"eacute", "ecirc", "euml", "igrave", "iacute", "icirc",
	"iuml", "eth", "ntilde", "ograve", "oacute", "ocirc", "otilde",
	"ouml", "divide", "oslash", "ugrave", "uacute", "ucirc",
	"uuml", "yacute", "thorn", "yuml"
};

static entity_table_t ent_iso_8859_15[] = {
	"nbsp", "iexcl", "cent", "pound", "euro", "yen", "Scaron",
	"sect", "scaron", "copy", "ordf", "laquo", "not", "shy", "reg",
	"macr", "deg", "plusmn", "sup2", "sup3", NULL, /* Zcaron */
	"micro", "para", "middot", NULL, /* zcaron */ "sup1", "ordm",
	"raquo", "OElig", "oelig", "Yuml", "iquest", "Agrave", "Aacute",
	"Acirc", "Atilde", "Auml", "Aring", "AElig", "Ccedil", "Egrave",
	"Eacute", "Ecirc", "Euml", "Igrave", "Iacute", "Icirc",
	"Iuml", "ETH", "Ntilde", "Ograve", "Oacute", "Ocirc", "Otilde",
	"Ouml", "times", "Oslash", "Ugrave", "Uacute", "Ucirc", "Uuml",
	"Yacute", "THORN", "szlig", "agrave", "aacute", "acirc",
	"atilde", "auml", "aring", "aelig", "ccedil", "egrave",
	"eacute", "ecirc", "euml", "igrave", "iacute", "icirc",
	"iuml", "eth", "ntilde", "ograve", "oacute", "ocirc", "otilde",
	"ouml", "divide", "oslash", "ugrave", "uacute", "ucirc",
	"uuml", "yacute", "thorn", "yuml"
};

static entity_table_t ent_uni_338_402[] = {
	/* 338 */
	"OElig", "oelig", NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL,
	NULL, NULL, NULL, NULL,
	/* 352 */
	"Scaron", "scaron",
	/* 354 - 375 */
	NULL, NULL, NULL, NULL, NULL, NULL, NULL,
	NULL, NULL, NULL, NULL, NULL, NULL, NULL,
	NULL, NULL, NULL, NULL, NULL, NULL, NULL,
	/* 376 */
	"Yuml",
	/* 377 - 401 */
	NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL,
	NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL,
	NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL,
	/* 402 */
	"fnof"
};

static entity_table_t ent_uni_spacing[] = {
	/* 710 */
	"circ",
	/* 711 - 731 */
	NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL,
	NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL,
	/* 732 */
	"tilde",
};

static entity_table_t ent_uni_greek[] = {
	/* 913 */
	"Alpha", "Beta", "Gamma", "Delta", "Epsilon", "Zeta", "Eta", "Theta",
	"Iota", "Kappa", "Lambda", "Mu", "Nu", "X1", "Omicron", "P1", "Rho",
	NULL, "Sigma", "Tau", "Upsilon", "Ph1", "Ch1", "Ps1", "Omega",
	/* 938 - 944 are not mapped */
	NULL, NULL, NULL, NULL, NULL, NULL, NULL,
	"alpha", "beta", "gamma", "delta", "epsilon", "zeta", "eta", "theta",
	"iota", "kappa", "lambda", "mu", "nu", "x1", "omicron", "p1", "rho",
	"sigmaf", "sigma", "tau", "upsilon", "ph1", "ch1", "ps1", "omega",
	/* 970 - 976 are not mapped */
	NULL, NULL, NULL, NULL, NULL, NULL, NULL,
	"thetasym", "ups1h",
	NULL, NULL, NULL,
	"p1v" 
};

static entity_table_t ent_uni_punct[] = {
	/* 8194 */
	"ensp", "emsp", NULL, NULL, NULL, NULL, NULL,
	"thinsp", NULL, NULL, "zwnj", "zwj", "lrm", "rlm",
	NULL, NULL, NULL, "ndash", "mdash", NULL, NULL, NULL,
	"lsquo", "rsquo", "sbquo", NULL, "ldquo", "rdquo", "bdquo",
	"dagger", "Dagger",	"bull", NULL, NULL, NULL, "hellip",
	NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, "permil", NULL,
	"prime", "Prime", NULL, NULL, NULL, NULL, NULL, "lsaquo", "rsaquo",
	NULL, NULL, NULL, "oline", NULL, NULL, NULL, NULL, NULL,
	"frasl"
};

static entity_table_t ent_uni_euro[] = {
	"euro"
};

static entity_table_t ent_uni_8465_8501[] = {
	/* 8465 */
	"image", NULL, NULL, NULL, NULL, NULL, NULL,
	/* 8472 */
	"weierp", NULL, NULL, NULL,
	/* 8476 */
	"real", NULL, NULL, NULL, NULL, NULL,
	/* 8482 */
	"trade", NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL,
	NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL,
	/* 8501 */
	"alefsym",
};

static entity_table_t ent_uni_8592_9002[] = {
	/* 8592 (0x2190) */
	"larr", "uarr", "rarr", "darr", "harr", NULL, NULL, NULL,
	NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL,
	/* 8608 (0x21a0) */
	NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL,
	NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL,
	/* 8624 (0x21b0) */
	NULL, NULL, NULL, NULL, "crarr", NULL, NULL, NULL,
	NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL,
	/* 8640 (0x21c0) */
	NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL,
	NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL,
	/* 8656 (0x21d0) */
	"lArr", "uArr", "rArr", "dArr", "hArr", "vArr", NULL, NULL,
	NULL, NULL, "lAarr", "rAarr", NULL, "rarrw", NULL, NULL,
	/* 8672 (0x21e0) */
	NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL,
	NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL,
	NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL,
	NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL,
	/* 8704 (0x2200) */
	"forall", "comp", "part", "exist", "nexist", "empty", NULL, "nabla",
	"isin", "notin", "epsis", NULL, "ni", "bepsi", NULL, "prod",
	/* 8720 (0x2210) */
	"coprod", "sum", "minus", "mnplus", "plusdo", NULL, "setmn", NULL,
	"compfn", NULL, "radic", NULL, NULL, "prop", "infin", "ang90",
	/* 8736 (0x2220) */
	"ang", "angmsd", "angsph", "mid", "nmid", "par", "npar", "and",
	"or", "cap", "cup", "int", NULL, NULL, "conint", NULL,
	/* 8752 (0x2230) */
	NULL, NULL, NULL, NULL, "there4", "becaus", NULL, NULL,
	NULL, NULL, NULL, NULL, "sim", "bsim", NULL, NULL,
	/* 8768 (0x2240) */
	"wreath", "nsim", NULL, "sime", "nsime", "cong", NULL, "ncong",
	"ap", "nap", "ape", NULL, "bcong", "asymp", "bump", "bumpe",
	/* 8784 (0x2250) */
	NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL,
	NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL,
	/* 8800 (0x2260) */
	"ne", "equiv", NULL, NULL, "le", "ge", "lE", "gE",
	"lnE", "gnE", "Lt", "Gt", "twixt", NULL, "nlt", "ngt",
	/* 8816 (0x2270) */
	"nles", "nges", "lsim", "gsim", NULL, NULL, "lg", "gl",
	NULL, NULL, "pr", "sc", "cupre", "sscue", "prsim", "scsim",
	/* 8832 (0x2280) */
	"npr", "nsc", "sub", "sup", "nsub", "nsup", "sube", "supe",
	/* 8840 - 8852 */
	NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL,
	/* 8853 */
	"oplus", NULL, "otimes",
	/* 8856 - 8868 */
	NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL,
	/* 8869 */
	"perp",
	/* 8870 - 8901 */
	NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL,
	NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL,
	NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL,
	NULL,
	/* 8901 */
	"sdot",
	/* 8902 - 8967 */
	NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL,
	NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL,
	NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL,
	NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL,
	NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL,
	NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL,
	NULL, NULL, NULL, NULL, NULL,
	/* 8968 */
	"lceil", "rceil", "lfloor", "rfloor",
	/* 8969 - 9000 */
	NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL,
	NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL,
	NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL,
	NULL,
	/* 9001 */
	"lang", "rang",
};

static entity_table_t ent_uni_9674[] = {
	/* 9674 */
	"loz"
};

static entity_table_t ent_uni_9824_9830[] = {
	/* 9824 */
	"spades", NULL, NULL, "clubs", NULL, "hearts", "diams"
};

static entity_table_t ent_koi8r[] = {
	"#1105", /* "jo "*/
	NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 
	NULL, NULL, NULL, NULL, NULL, "#1025", /* "JO" */
	NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 
	"#1102", "#1072", "#1073", "#1094", "#1076", "#1077", "#1092", 
	"#1075", "#1093", "#1080", "#1081", "#1082", "#1083", "#1084", 
	"#1085", "#1086", "#1087", "#1103", "#1088", "#1089", "#1090", 
	"#1091", "#1078", "#1074", "#1100", "#1099", "#1079", "#1096", 
	"#1101", "#1097", "#1095", "#1098", "#1070", "#1040", "#1041", 
	"#1062", "#1044", "#1045", "#1060", "#1043", "#1061", "#1048", 
	"#1049", "#1050", "#1051", "#1052", "#1053", "#1054", "#1055", 
	"#1071", "#1056", "#1057", "#1058", "#1059", "#1046", "#1042",
	"#1068", "#1067", "#1047", "#1064", "#1069", "#1065", "#1063", 
	"#1066"
};

static entity_table_t ent_cp_1251[] = {
	"#1026", "#1027", "#8218", "#1107", "#8222", "hellip", "dagger",
	"Dagger", "euro", "permil", "#1033", "#8249", "#1034", "#1036",
	"#1035", "#1039", "#1106", "#8216", "#8217", "#8219", "#8220",
	"bull", "ndash", "mdash", NULL, "trade", "#1113", "#8250",
	"#1114", "#1116", "#1115", "#1119", "nbsp", "#1038", "#1118",
	"#1032", "curren", "#1168", "brvbar", "sect", "#1025", "copy",
	"#1028", "laquo", "not", "shy", "reg", "#1031", "deg", "plusmn",
	"#1030", "#1110", "#1169", "micro", "para", "middot", "#1105",
	"#8470", "#1108", "raquo", "#1112", "#1029", "#1109", "#1111",
	"#1040", "#1041", "#1042", "#1043", "#1044", "#1045", "#1046",
	"#1047", "#1048", "#1049", "#1050", "#1051", "#1052", "#1053",
	"#1054", "#1055", "#1056", "#1057", "#1058", "#1059", "#1060",
	"#1061", "#1062", "#1063", "#1064", "#1065", "#1066", "#1067",
	"#1068", "#1069", "#1070", "#1071", "#1072", "#1073", "#1074",
	"#1075", "#1076", "#1077", "#1078", "#1079", "#1080", "#1081",
	"#1082", "#1083", "#1084", "#1085", "#1086", "#1087", "#1088",
	"#1089", "#1090", "#1091", "#1092", "#1093", "#1094", "#1095",
	"#1096", "#1097", "#1098", "#1099", "#1100", "#1101", "#1102",
	"#1103"
};

static entity_table_t ent_iso_8859_5[] = {
	"#1056", "#1057", "#1058", "#1059", "#1060", "#1061", "#1062",
	"#1063", "#1064", "#1065", "#1066", "#1067", "#1068", "#1069",
	"#1070", "#1071", "#1072", "#1073", "#1074", "#1075", "#1076",
	"#1077", "#1078", "#1079", "#1080", "#1081", "#1082", "#1083",
	"#1084", "#1085", "#1086", "#1087", "#1088", "#1089", "#1090",
	"#1091", "#1092", "#1093", "#1094", "#1095", "#1096", "#1097",
	"#1098", "#1099", "#1100", "#1101", "#1102", "#1103", "#1104",
	"#1105", "#1106", "#1107", "#1108", "#1109", "#1110", "#1111",
	"#1112", "#1113", "#1114", "#1115", "#1116", "#1117", "#1118",
	"#1119"
};

static entity_table_t ent_cp_866[] = {

	"#9492", "#9524", "#9516", "#9500", "#9472", "#9532", "#9566", 
	"#9567", "#9562", "#9556", "#9577", "#9574", "#9568", "#9552", 
	"#9580", "#9575", "#9576", "#9572", "#9573", "#9561", "#9560", 
	"#9554", "#9555", "#9579", "#9578", "#9496", "#9484", "#9608", 
	"#9604", "#9612", "#9616", "#9600", "#1088", "#1089", "#1090", 
	"#1091", "#1092", "#1093", "#1094", "#1095", "#1096", "#1097", 
	"#1098", "#1099", "#1100", "#1101", "#1102", "#1103", "#1025", 
	"#1105", "#1028", "#1108", "#1031", "#1111", "#1038", "#1118", 
	"#176", "#8729", "#183", "#8730", "#8470", "#164",  "#9632", 
	"#160"
};


struct html_entity_map {
	enum entity_charset charset;	/* charset identifier */
	unsigned short basechar;			/* char code at start of table */
	unsigned short endchar;			/* last char code in the table */
	entity_table_t *table;			/* the table of mappings */
};

static const struct html_entity_map entity_map[] = {
	{ cs_cp1252, 		0x80, 0x9f, ent_cp_1252 },
	{ cs_cp1252, 		0xa0, 0xff, ent_iso_8859_1 },
	{ cs_8859_1, 		0xa0, 0xff, ent_iso_8859_1 },
	{ cs_8859_15, 		0xa0, 0xff, ent_iso_8859_15 },
	{ cs_utf_8, 		0xa0, 0xff, ent_iso_8859_1 },
	{ cs_utf_8, 		338,  402,  ent_uni_338_402 },
	{ cs_utf_8, 		710,  732,  ent_uni_spacing },
	{ cs_utf_8, 		913,  982,  ent_uni_greek },
	{ cs_utf_8, 		8194, 8260, ent_uni_punct },
	{ cs_utf_8, 		8364, 8364, ent_uni_euro }, 
	{ cs_utf_8, 		8465, 8501, ent_uni_8465_8501 },
	{ cs_utf_8, 		8592, 9002, ent_uni_8592_9002 },
	{ cs_utf_8, 		9674, 9674, ent_uni_9674 },
	{ cs_utf_8, 		9824, 9830, ent_uni_9824_9830 },
	{ cs_big5, 			0xa0, 0xff, ent_iso_8859_1 },
	{ cs_gb2312, 		0xa0, 0xff, ent_iso_8859_1 },
	{ cs_big5hkscs, 	0xa0, 0xff, ent_iso_8859_1 },
 	{ cs_sjis,			0xa0, 0xff, ent_iso_8859_1 },
 	{ cs_eucjp,			0xa0, 0xff, ent_iso_8859_1 },
	{ cs_koi8r,		    0xa3, 0xff, ent_koi8r },
	{ cs_cp1251,		0x80, 0xff, ent_cp_1251 },
	{ cs_8859_5,		0xc0, 0xff, ent_iso_8859_5 },
	{ cs_cp866,		    0xc0, 0xff, ent_cp_866 },
	{ cs_terminator }
};

static const struct
{
	unsigned short charcode;
	char *entity;
	int entitylen;
	int flags;
}
basic_entities[] = {
	{ '"',	"&quot;",	6,	ENT_HTML_QUOTE_DOUBLE },
	{ '\'',	"&#039;",	6,	ENT_HTML_QUOTE_SINGLE },
	{ '\'',	"&#39;",	5,	ENT_HTML_QUOTE_SINGLE },
	{ '<',	"&lt;",		4,	0 },
	{ '>',	"&gt;",		4,	0 },
	{ 0, NULL, 0, 0 }
};

#define MB_RETURN					\
		{							\
			*newpos = pos;			\
		  	mbseq[mbpos] = '\0';	\
		  	*mbseqlen = mbpos;		\
		  	return this_char;		\
		}
					
#define MB_WRITE(mbchar)				\
		{								\
			mbspace--;					\
			if (mbspace == 0)			\
			{							\
				MB_RETURN;				\
			}							\
			mbseq[mbpos++] = (mbchar);	\
		}

// copied from html.c and beautified
inline static unsigned short get_next_char(enum entity_charset charset, unsigned char *str, int *newpos,
		unsigned char *mbseq, int *mbseqlen)
{
	int pos = *newpos;
	int mbpos = 0;
	int mbspace = *mbseqlen;
	unsigned short this_char = str[pos++];
	
	if (mbspace <= 0)
	{
		*mbseqlen = 0;
		return this_char;
	}
	
	MB_WRITE((unsigned char)this_char);
	
	switch (charset)
	{
		case cs_utf_8:
			{
				unsigned long utf = 0;
				int stat = 0;
				int more = 1;

				/* unpack utf-8 encoding into a wide char.
				 * Code stolen from the mbstring extension */

				do
				{
					if (this_char < 0x80)
					{
						more = 0;
						break;
					}
					else if (this_char < 0xc0)
					{
						switch (stat)
						{
							case 0x10:	/* 2, 2nd */
							case 0x21:	/* 3, 3rd */
							case 0x32:	/* 4, 4th */
							case 0x43:	/* 5, 5th */
							case 0x54:	/* 6, 6th */
								/* last byte in sequence */
								more = 0;
								utf |= (this_char & 0x3f);
								this_char = (unsigned short)utf;
								break;

							case 0x20:	/* 3, 2nd */
							case 0x31:	/* 4, 3rd */
							case 0x42:	/* 5, 4th */
							case 0x53:	/* 6, 5th */
								/* penultimate char */
								utf |= ((this_char & 0x3f) << 6);
								stat++;
								break;

							case 0x30:	/* 4, 2nd */
							case 0x41:	/* 5, 3rd */
							case 0x52:	/* 6, 4th */
								utf |= ((this_char & 0x3f) << 12);
								stat++;
								break;

							case 0x40:	/* 5, 2nd */
							case 0x51:
								utf |= ((this_char & 0x3f) << 18);
								stat++;
								break;

							case 0x50:	/* 6, 2nd */
								utf |= ((this_char & 0x3f) << 24);
								stat++;

							default:
								/* invalid */
								more = 0;
						}
					}
					/* lead byte */
					else if (this_char < 0xe0)
					{
						stat = 0x10;	/* 2 byte */
						utf = (this_char & 0x1f) << 6;
					}
					else if (this_char < 0xf0)
					{
						stat = 0x20;	/* 3 byte */
						utf = (this_char & 0xf) << 12;
					}
					else if (this_char < 0xf8)
					{
						stat = 0x30;	/* 4 byte */
						utf = (this_char & 0x7) << 18;
					}
					else if (this_char < 0xfc)
					{
						stat = 0x40;	/* 5 byte */
						utf = (this_char & 0x3) << 24;
					}
					else if (this_char < 0xfe)
					{
						stat = 0x50;	/* 6 byte */
						utf = (this_char & 0x1) << 30;
					}
					else
					{
						/* invalid; bail */
						more = 0;
						break;
					}

					if (more)
					{
						this_char = str[pos++];
						MB_WRITE((unsigned char)this_char);
					}
				} while (more);
			}
			break;

		case cs_big5:
		case cs_gb2312:
		case cs_big5hkscs:
			{
				/* check if this is the first of a 2-byte sequence */
				if (this_char >= 0xa1 && this_char <= 0xf9)
				{
					/* peek at the next char */
					unsigned char next_char = str[pos];
					if ((next_char >= 0x40 && next_char <= 0x73) ||
							(next_char >= 0xa1 && next_char <= 0xfe))
					{
						/* yes, this a wide char */
						this_char <<= 8;
						MB_WRITE(next_char);
						this_char |= next_char;
						pos++;
					}
					
				}
				break;
			}

		case cs_sjis:
			{
				/* check if this is the first of a 2-byte sequence */
				if ( (this_char >= 0x81 && this_char <= 0x9f) ||
					 (this_char >= 0xe0 && this_char <= 0xef)
					)
				{
					/* peek at the next char */
					unsigned char next_char = str[pos];
					if ((next_char >= 0x40 && next_char <= 0x7e) ||
						(next_char >= 0x80 && next_char <= 0xfc))
					{
						/* yes, this a wide char */
						this_char <<= 8;
						MB_WRITE(next_char);
						this_char |= next_char;
						pos++;
					}
					
				}
				break;
			}

		case cs_eucjp:
			{
				/* check if this is the first of a multi-byte sequence */
				if (this_char >= 0xa1 && this_char <= 0xfe)
				{
					/* peek at the next char */
					unsigned char next_char = str[pos];
					if (next_char >= 0xa1 && next_char <= 0xfe)
					{
						/* yes, this a jis kanji char */
						this_char <<= 8;
						MB_WRITE(next_char);
						this_char |= next_char;
						pos++;
					}
					
				}
				else if (this_char == 0x8e)
				{
					/* peek at the next char */
					unsigned char next_char = str[pos];
					if (next_char >= 0xa1 && next_char <= 0xdf)
					{
						/* JIS X 0201 kana */
						this_char <<= 8;
						MB_WRITE(next_char);
						this_char |= next_char;
						pos++;
					}
					
				} else if (this_char == 0x8f)
				{
					/* peek at the next two char */
					unsigned char next_char = str[pos];
					unsigned char next2_char = str[pos+1];
					if ((next_char >= 0xa1 && next_char <= 0xfe) &&
						(next2_char >= 0xa1 && next2_char <= 0xfe))
					{
						/* JIS X 0212 hojo-kanji */
						this_char <<= 8;
						MB_WRITE(next_char);
						this_char |= next_char;
						pos++;
						this_char <<= 8;
						MB_WRITE(next2_char);
						this_char |= next2_char;
						pos++;
					}
					
				}
				break;
			}

		default:
			break;
	}
	MB_RETURN;
}

// copied from html.c, slightly modified and beautified
/* {{{ entity_charset determine_charset
 * returns the charset identifier based on current locale or a hint.
 * defaults to iso-8859-1 */
static enum entity_charset determine_charset(char *charset_hint TSRMLS_DC)
{
	int i;
	enum entity_charset charset = cs_8859_1;
	int len = 0;
	zval *uf_result = NULL;

	/* Guarantee default behaviour for backwards compatibility */
	if (charset_hint == NULL) return cs_8859_1;

	if ((len = strlen(charset_hint)) != 0) goto det_charset;
/*
	charset_hint = SG(default_charset);
	if (charset_hint != NULL && (len=strlen(charset_hint)) != 0) {
		goto det_charset;
	}
*/
	/* try to detect the charset for the locale */
/*
#if HAVE_NL_LANGINFO && HAVE_LOCALE_H && defined(CODESET)
	charset_hint = nl_langinfo(CODESET);
	if (charset_hint != NULL && (len=strlen(charset_hint)) != 0) {
		goto det_charset;
	}
#endif
*/
//#if HAVE_LOCALE_H
	/* try to figure out the charset from the locale */
	{
		char *localename;
		char *dot, *at;

		/* lang[_territory][.codeset][@modifier] */
		localename = setlocale(LC_CTYPE, NULL);

		dot = strchr(localename, '.');
		if (dot)
		{
			dot++;
			/* locale specifies a codeset */
			at = strchr(dot, '@');
			if (at)	len = at - dot;
			else len = strlen(dot);
			charset_hint = dot;
		}
		else
		{
			/* no explicit name; see if the name itself
			 * is the charset */
			charset_hint = localename;
			len = strlen(charset_hint);
		}
	}
//#endif

det_charset:

	if (charset_hint)
	{
		int found = 0;
		
		/* now walk the charset map and look for the codeset */
		for (i = 0; charset_map[i].codeset; i++)
		{
			if (strncasecmp(charset_hint, charset_map[i].codeset, len) == 0)
			{
				charset = charset_map[i].charset;
				found = 1;
				break;
			}
		}
		if (!found)
		{
			php_error_docref(NULL TSRMLS_CC, E_WARNING, "charset `%s' not supported, assuming iso-8859-1",
					charset_hint);
		}
	}
	if (uf_result != NULL) zval_ptr_dtor(&uf_result);
	return charset;
}

// copied from html.c and beautified
ZEND_API char *php_escape_html_entities(unsigned char *old, int oldlen, int *newlen, int all,
										int quote_style, char *hint_charset TSRMLS_DC)
{
	int i, j, maxlen, len;
	char *replaced;
	enum entity_charset charset = determine_charset(hint_charset TSRMLS_CC);
	int matches_map;

	maxlen = 2 * oldlen;
	if (maxlen < 128) maxlen = 128;
	replaced = (char *)emalloc (maxlen);
	len = 0;

	i = 0;
	while (i < oldlen)
	{
		unsigned char mbsequence[16];	/* allow up to 15 characters in a multibyte sequence */
		int mbseqlen = sizeof(mbsequence);
		unsigned short this_char = get_next_char(charset, old, &i, mbsequence, &mbseqlen);

		matches_map = 0;

		if (len + 9 > maxlen) replaced = (char *)erealloc(replaced, maxlen += 128);

		if (all)
		{
			/* look for a match in the maps for this charset */
			unsigned char *rep = NULL;

			for (j = 0; entity_map[j].charset != cs_terminator; j++)
			{
				if (entity_map[j].charset == charset
						&& this_char >= entity_map[j].basechar
						&& this_char <= entity_map[j].endchar)
				{
					rep = (unsigned char*)entity_map[j].table[this_char - entity_map[j].basechar];
					if (rep == NULL)
					{
						/* there is no entity for this position; fall through and
						 * just output the character itself */
						break;
					}

					matches_map = 1;
					break;
				}
			}

			if (matches_map)
			{
				replaced[len++] = '&';
				strcpy(replaced + len, (const char *)rep);
				len += strlen((const char *)rep);
				replaced[len++] = ';';
			}
		}
		if (!matches_map)
		{	
			int is_basic = 0;

			if (this_char == '&') {
				memcpy(replaced + len, "&amp;", sizeof("&amp;") - 1);
				len += sizeof("&amp;") - 1;
				is_basic = 1;
			}
			else
			{
				for (j = 0; basic_entities[j].charcode != 0; j++)
				{
					if ((basic_entities[j].charcode != this_char) ||
							(basic_entities[j].flags &&
							(quote_style & basic_entities[j].flags) == 0))
					{
						continue;
					}

					memcpy(replaced + len, basic_entities[j].entity, basic_entities[j].entitylen);
					len += basic_entities[j].entitylen;
		
					is_basic = 1;
					break;
				}
			}

			if (!is_basic)
			{
				/* a wide char without a named entity; pass through the original sequence */
				if (mbseqlen > 1)
				{
					memcpy(replaced + len, mbsequence, mbseqlen);
					len += mbseqlen;
				}
				else replaced[len++] = (unsigned char)this_char;
			}
		}
	}
	replaced[len] = '\0';
	*newlen = len;

	return replaced;
}

ZEND_API char *php_unescape_html_entities(unsigned char *old, int oldlen, int *newlen, int all, int quote_style,
										  char *hint_charset TSRMLS_DC)
{
	int retlen;
	int j, k;
	char *replaced, *ret;
	enum entity_charset charset = determine_charset(hint_charset TSRMLS_CC);
	unsigned char replacement[15];
	
	ret = estrdup((char *)old);
	retlen = oldlen;
	if (!retlen) goto empty_source;
	
	if (all)
	{
		/* look for a match in the maps for this charset */
		for (j = 0; entity_map[j].charset != cs_terminator; j++)
		{
			if (entity_map[j].charset != charset) continue;

			for (k = entity_map[j].basechar; k <= entity_map[j].endchar; k++)
			{
				unsigned char entity[32];
				int entity_length = 0;

				if (entity_map[j].table[k - entity_map[j].basechar] == NULL) continue;
				
				entity[0] = '&';
				entity_length = strlen(entity_map[j].table[k - entity_map[j].basechar]);
				strncpy((char *)&entity[1], entity_map[j].table[k - entity_map[j].basechar], sizeof(entity) - 2);
				entity[entity_length+1] = ';';
				entity[entity_length+2] = '\0';
				entity_length += 2;

				/* When we have MBCS entities in the tables above, this will need to handle it */
				if (k > 0xff) zend_error(E_WARNING, "cannot yet handle MBCS in html_entity_decode()!");

				replacement[0] = k;
				replacement[1] = '\0';

				replaced = php_str_to_str(ret, retlen, (char *)entity, entity_length, (char *)replacement, 1, &retlen);
				efree(ret);
				ret = replaced;
			}
		}
	}

	for (j = 0; basic_entities[j].charcode != 0; j++)
	{
		if (basic_entities[j].flags && (quote_style & basic_entities[j].flags) == 0) continue;
		
		replacement[0] = (unsigned char)basic_entities[j].charcode;
		replacement[1] = '\0';
		
		replaced = php_str_to_str(ret, retlen, basic_entities[j].entity, basic_entities[j].entitylen,
			(char *)replacement, 1, &retlen);
		efree(ret);
		ret = replaced;
	}

empty_source:	
	*newlen = retlen;
	return ret;
}

/* {{{ localeconv_r
 * glibc's localeconv is not reentrant, so lets make it so ... sorta */
ZEND_API struct lconv *localeconv_r(struct lconv *out)
{
	struct lconv *res;

	tsrm_mutex_lock(locale_mutex);

	/* localeconv doesn't return an error condition */
	res = localeconv();
	*out = *res;

	tsrm_mutex_unlock(locale_mutex);

	return out;
}

void localeconv_init()
{
	locale_mutex = tsrm_mutex_alloc();
}

void localeconv_shutdown()
{
	tsrm_mutex_free(locale_mutex);
}
