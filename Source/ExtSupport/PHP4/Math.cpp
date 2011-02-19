//
// ExtSupport.PHP4 - substitute for php4ts.dll
//
// Math.cpp 
// - contains definitions of math exported functions
//

#include "stdafx.h"

#include <math.h>
#include <float.h>

#include "Math.h"
#include "Errors.h"
#include "TsrmLs.h"
#include "Variables.h"
#include "Spprintf.h"

#pragma unmanaged

#define PHP_ROUND_FUZZ 0.5

// copied from math.c and beautified
#define PHP_ROUND_WITH_FUZZ(val, places)					\
	{														\
	double tmp_val = val, f = pow(10.0, (double)places);	\
	tmp_val *= f;											\
	if (tmp_val >= 0.0)										\
	{														\
		tmp_val = floor(tmp_val + PHP_ROUND_FUZZ);			\
	}														\
	else													\
	{														\
		tmp_val = ceil(tmp_val - PHP_ROUND_FUZZ);			\
	}														\
	tmp_val /= f;											\
	val = !zend_isnan(tmp_val) ? tmp_val : val;				\
}

// copied from math.c and beautified
/*
 * Convert a string representation of a base(2-36) number to a long.
 */
ZEND_API long _php_math_basetolong(zval *arg, int base)
{
	long num = 0, digit, onum;
	int i;
	char c, *s;

	if (Z_TYPE_P(arg) != IS_STRING || base < 2 || base > 36) return 0;

	s = Z_STRVAL_P(arg);

	for (i = Z_STRLEN_P(arg); i > 0; i--)
	{
		c = *s++;

		digit = (c >= '0' && c <= '9') ? c - '0'
			: (c >= 'A' && c <= 'Z') ? c - 'A' + 10
			: (c >= 'a' && c <= 'z') ? c - 'a' + 10
			: base;

		if (digit >= base) continue;

		onum = num;
		num = num * base + digit;
		if (num > onum) continue;

		{
			TSRMLS_FETCH();

			php_error_docref(NULL TSRMLS_CC, E_WARNING, "Number '%s' is too big to fit in long", s);
			return LONG_MAX;
		}
	}

	return num;
}

// copied from math.c and beautified
/*
 * Convert a string representation of a base(2-36) number to a zval.
 */
ZEND_API int _php_math_basetozval(zval *arg, int base, zval *ret)
{
	int i, cutlim, mode = 0;
	long cutoff, num = 0;
	double fnum = 0;
	char c, *s;

	if (Z_TYPE_P(arg) != IS_STRING || base < 2 || base > 36) return FAILURE;

	s = Z_STRVAL_P(arg);

	cutoff = LONG_MAX / base;
	cutlim = LONG_MAX % base;

	for (i = Z_STRLEN_P(arg); i > 0; i--)
	{
		c = *s++;

		/* might not work for EBCDIC */
		if (c >= '0' && c <= '9') c -= '0';
		else if (c >= 'A' && c <= 'Z') c -= 'A' - 10;
		else if (c >= 'a' && c <= 'z') c -= 'a' - 10;
		else continue;

		if (c >= base) continue;

		switch (mode)
		{
			case 0: /* Integer */
				if (num < cutoff || (num == cutoff && c <= cutlim))
				{
					num = num * base + c;
					break;
				}
				else
				{
					fnum = num;
					mode = 1;
				}
				/* fall-through */
			case 1: /* Float */
				fnum = fnum * base + c;
		}
	}

	if (mode == 1) 
	{
		ZVAL_DOUBLE(ret, fnum);
	}
	else ZVAL_LONG(ret, num);

	return SUCCESS;
}

// copied from math.c and beautified
/*
 * Convert a long to a string containing a base(2-36) representation of
 * the number.
 */
ZEND_API char *_php_math_longtobase(zval *arg, int base)
{
	static char digits[] = "0123456789abcdefghijklmnopqrstuvwxyz";
	char buf[(sizeof(unsigned long) << 3) + 1], *ptr, *end;
	unsigned long value;

	if (Z_TYPE_P(arg) != IS_LONG || base < 2 || base > 36) return empty_string;

	value = Z_LVAL_P(arg);

	end = ptr = buf + sizeof(buf) - 1;
	*ptr = '\0';

	do
	{
		*--ptr = digits[value % base];
		value /= base;
	}
	while (ptr > buf && value);

	return estrndup(ptr, end - ptr);
}

// copied from math.c and beautified
/*
 * Convert a zval to a string containing a base(2-36) representation of
 * the number.
 */
ZEND_API char *_php_math_zvaltobase(zval *arg, int base TSRMLS_DC)
{
	static char digits[] = "0123456789abcdefghijklmnopqrstuvwxyz";

	if ((Z_TYPE_P(arg) != IS_LONG && Z_TYPE_P(arg) != IS_DOUBLE) || base < 2 || base > 36) return empty_string;

	if (Z_TYPE_P(arg) == IS_DOUBLE)
	{
		double fvalue = floor(Z_DVAL_P(arg)); /* floor it just in case */
		char buf[(sizeof(double) << 3) + 1];
		char *ptr, *end;

		/* Don't try to convert +/- infinity */
		if (fvalue == HUGE_VAL || fvalue == -HUGE_VAL)
		{
			php_error_docref(NULL TSRMLS_CC, E_WARNING, "Number too large");
			return empty_string;
		}

		end = ptr = buf + sizeof(buf) - 1;
		*ptr = '\0';

		do
		{
			*--ptr = digits[(int) fmod(fvalue, base)];
			fvalue /= base;
		}
		while (ptr > buf && fabs(fvalue) >= 1);

		return estrndup(ptr, end - ptr);
	}

	return _php_math_longtobase(arg, base);
}

// copied from math.c and beautified
ZEND_API char *_php_math_number_format(double d, int dec, char dec_point, char thousand_sep)
{
	char *tmpbuf = NULL, *resbuf, *dp, *s, *t;  /* source, target */
	int integral, tmplen, reslen = 0, count = 0, is_negative = 0;

	if (d < 0)
	{
		is_negative = 1;
		d = -d;
	}
	dec = MAX(0, dec);

	PHP_ROUND_WITH_FUZZ(d, dec);

	tmplen = spprintf(&tmpbuf, 0, "%.*f", dec, d);

	if (tmpbuf == NULL || !isdigit((int)tmpbuf[0])) return tmpbuf;

	/* calculate the length of the return buffer */
	dp = strchr(tmpbuf, '.');
	if (dp) integral = dp - tmpbuf;
	else
	{
		/* no decimal point was found */
		integral = tmplen;
	}

	/* allow for thousand separators */
	if (thousand_sep) integral += (integral-1) / 3;

	reslen = integral;

	if (dec) reslen += 1 + dec;

	/* add a byte for minus sign */
	if (is_negative) reslen++;
	resbuf = (char *) emalloc(reslen+1); /* +1 for NUL terminator */

	s = tmpbuf + tmplen - 1;
	t = resbuf + reslen;
	*t-- = '\0';

	/* copy the decimal places.
	 * Take care, as the sprintf implementation may return less places than
	 * we requested due to internal buffer limitations */
	if (dec)
	{
		int declen = dp ? strlen(dp + 1) : 0;
		int topad = declen > 0 ? dec - declen : 0;

		/* pad with '0's */

		while (topad--) *t-- = '0';

		if (dp)
		{
			/* now copy the chars after the point */
			memcpy(t - declen + 1, dp + 1, declen);

			t -= declen;
			s -= declen;
		}

		/* add decimal point */
		*t-- = dec_point;
		s--;
	}

	/* copy the numbers before the decimal place, adding thousand
	 * separator every three digits */
	while (s >= tmpbuf)
	{
		*t-- = *s--;
		if (thousand_sep && (++count % 3) == 0 && s >= tmpbuf)
		{
			*t-- = thousand_sep;
		}
	}

	/* and a minus sign, if needed */
	if (is_negative) *t-- = '-';

	efree(tmpbuf);
	return resbuf;
}

// copied from basic_functions.c
ZEND_API double php_get_inf(void)
{
	return HUGE_VAL;
}

// copied from basic_functions.c
ZEND_API double php_get_nan(void)
{
	return HUGE_VAL + -HUGE_VAL;
}
