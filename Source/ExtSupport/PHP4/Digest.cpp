//
// ExtSupport.PHP4 - substitute for php4ts.dll
//
// Digest.cpp
// - slightly modified md5.c and sha1.c, originally PHP source files
//

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
   | Author: Lachlan Roche                                                |
   +----------------------------------------------------------------------+
*/

/* $Id: Digest.cpp,v 1.1.2.2 2006/04/15 20:19:12 prosl0am Exp $ */

/* 
 * md5.c - Copyright 1997 Lachlan Roche 
 * md5_file() added by Alessandro Astarita <aleast@capri.it>
 */

#include "stdafx.h"
#include <stdio.h>
//#include "php.h"

#include "Digest.h"

#pragma unmanaged

ZEND_API void make_digest(char *md5str, unsigned char *digest)
{
	int i;

	for (i = 0; i < 16; i++) {
		sprintf(md5str, "%02x", digest[i]);
		md5str += 2;
	}

	*md5str = '\0';
}

/*
 * The remaining code is the reference MD5 code (md5c.c) from rfc1321
 */
/* MD5C.C - RSA Data Security, Inc., MD5 message-digest algorithm
 */

/* Copyright (C) 1991-2, RSA Data Security, Inc. Created 1991. All
   rights reserved.

   License to copy and use this software is granted provided that it
   is identified as the "RSA Data Security, Inc. MD5 Message-Digest
   Algorithm" in all material mentioning or referencing this software
   or this function.

   License is also granted to make and use derivative works provided
   that such works are identified as "derived from the RSA Data
   Security, Inc. MD5 Message-Digest Algorithm" in all material
   mentioning or referencing the derived work.

   RSA Data Security, Inc. makes no representations concerning either
   the merchantability of this software or the suitability of this
   software for any particular purpose. It is provided "as is"
   without express or implied warranty of any kind.

   These notices must be retained in any copies of any part of this
   documentation and/or software.
 */

/* Constants for MD5Transform routine.
 */


#define S11 7
#define S12 12
#define S13 17
#define S14 22
#define S21 5
#define S22 9
#define S23 14
#define S24 20
#define S31 4
#define S32 11
#define S33 16
#define S34 23
#define S41 6
#define S42 10
#define S43 15
#define S44 21

static void MD5Transform(php_uint32[4], const unsigned char[64]);
static void Encode(unsigned char *, php_uint32 *, unsigned int);
static void Decode(php_uint32 *, const unsigned char *, unsigned int);

static unsigned char PADDING[64] =
{
	0x80, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
	0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
	0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0
};

/* F, G, H and I are basic MD5 functions.
 */
#define F(x, y, z) (((x) & (y)) | ((~x) & (z)))
#define G(x, y, z) (((x) & (z)) | ((y) & (~z)))
#define H(x, y, z) ((x) ^ (y) ^ (z))
#define I(x, y, z) ((y) ^ ((x) | (~z)))

/* ROTATE_LEFT rotates x left n bits.
 */
#define ROTATE_LEFT(x, n) (((x) << (n)) | ((x) >> (32-(n))))

/* FF, GG, HH, and II transformations for rounds 1, 2, 3, and 4.
   Rotation is separate from addition to prevent recomputation.
 */
#define FF(a, b, c, d, x, s, ac) { \
 (a) += F ((b), (c), (d)) + (x) + (php_uint32)(ac); \
 (a) = ROTATE_LEFT ((a), (s)); \
 (a) += (b); \
  }
#define GG(a, b, c, d, x, s, ac) { \
 (a) += G ((b), (c), (d)) + (x) + (php_uint32)(ac); \
 (a) = ROTATE_LEFT ((a), (s)); \
 (a) += (b); \
  }
#define HH(a, b, c, d, x, s, ac) { \
 (a) += H ((b), (c), (d)) + (x) + (php_uint32)(ac); \
 (a) = ROTATE_LEFT ((a), (s)); \
 (a) += (b); \
  }
#define II(a, b, c, d, x, s, ac) { \
 (a) += I ((b), (c), (d)) + (x) + (php_uint32)(ac); \
 (a) = ROTATE_LEFT ((a), (s)); \
 (a) += (b); \
  }

/* {{{ PHP_MD5Init
 * MD5 initialization. Begins an MD5 operation, writing a new context.
 */
ZEND_API void PHP_MD5Init(PHP_MD5_CTX * context)
{
	context->count[0] = context->count[1] = 0;
	/* Load magic initialization constants.
	 */
	context->state[0] = 0x67452301;
	context->state[1] = 0xefcdab89;
	context->state[2] = 0x98badcfe;
	context->state[3] = 0x10325476;
}
/* }}} */

/* {{{ PHP_MD5Update
   MD5 block update operation. Continues an MD5 message-digest
   operation, processing another message block, and updating the
   context.
 */
ZEND_API void PHP_MD5Update(PHP_MD5_CTX * context, const unsigned char *input,
			   unsigned int inputLen)
{
	unsigned int i, index, partLen;

	/* Compute number of bytes mod 64 */
	index = (unsigned int) ((context->count[0] >> 3) & 0x3F);

	/* Update number of bits */
	if ((context->count[0] += ((php_uint32) inputLen << 3))
		< ((php_uint32) inputLen << 3))
		context->count[1]++;
	context->count[1] += ((php_uint32) inputLen >> 29);

	partLen = 64 - index;

	/* Transform as many times as possible.
	 */
	if (inputLen >= partLen) {
		memcpy
			((unsigned char*) & context->buffer[index], (unsigned char*) input, partLen);
		MD5Transform(context->state, context->buffer);

		for (i = partLen; i + 63 < inputLen; i += 64)
			MD5Transform(context->state, &input[i]);

		index = 0;
	} else
		i = 0;

	/* Buffer remaining input */
	memcpy
		((unsigned char*) & context->buffer[index], (unsigned char*) & input[i],
		 inputLen - i);
}
/* }}} */

/* {{{ PHP_MD5Final
   MD5 finalization. Ends an MD5 message-digest operation, writing the
   the message digest and zeroizing the context.
 */
ZEND_API void PHP_MD5Final(unsigned char digest[16], PHP_MD5_CTX * context)
{
	unsigned char bits[8];
	unsigned int index, padLen;

	/* Save number of bits */
	Encode(bits, context->count, 8);

	/* Pad out to 56 mod 64.
	 */
	index = (unsigned int) ((context->count[0] >> 3) & 0x3f);
	padLen = (index < 56) ? (56 - index) : (120 - index);
	PHP_MD5Update(context, PADDING, padLen);

	/* Append length (before padding) */
	PHP_MD5Update(context, bits, 8);

	/* Store state in digest */
	Encode(digest, context->state, 16);

	/* Zeroize sensitive information.
	 */
	memset((unsigned char*) context, 0, sizeof(*context));
}
/* }}} */

/* {{{ MD5Transform
 * MD5 basic transformation. Transforms state based on block.
 */
static void MD5Transform(php_uint32 state[4], const unsigned char block[64])
{
	php_uint32 a = state[0], b = state[1], c = state[2], d = state[3], x[16];

	Decode(x, block, 64);

	/* Round 1 */
	FF(a, b, c, d, x[0], S11, 0xd76aa478);	/* 1 */
	FF(d, a, b, c, x[1], S12, 0xe8c7b756);	/* 2 */
	FF(c, d, a, b, x[2], S13, 0x242070db);	/* 3 */
	FF(b, c, d, a, x[3], S14, 0xc1bdceee);	/* 4 */
	FF(a, b, c, d, x[4], S11, 0xf57c0faf);	/* 5 */
	FF(d, a, b, c, x[5], S12, 0x4787c62a);	/* 6 */
	FF(c, d, a, b, x[6], S13, 0xa8304613);	/* 7 */
	FF(b, c, d, a, x[7], S14, 0xfd469501);	/* 8 */
	FF(a, b, c, d, x[8], S11, 0x698098d8);	/* 9 */
	FF(d, a, b, c, x[9], S12, 0x8b44f7af);	/* 10 */
	FF(c, d, a, b, x[10], S13, 0xffff5bb1);		/* 11 */
	FF(b, c, d, a, x[11], S14, 0x895cd7be);		/* 12 */
	FF(a, b, c, d, x[12], S11, 0x6b901122);		/* 13 */
	FF(d, a, b, c, x[13], S12, 0xfd987193);		/* 14 */
	FF(c, d, a, b, x[14], S13, 0xa679438e);		/* 15 */
	FF(b, c, d, a, x[15], S14, 0x49b40821);		/* 16 */

	/* Round 2 */
	GG(a, b, c, d, x[1], S21, 0xf61e2562);	/* 17 */
	GG(d, a, b, c, x[6], S22, 0xc040b340);	/* 18 */
	GG(c, d, a, b, x[11], S23, 0x265e5a51);		/* 19 */
	GG(b, c, d, a, x[0], S24, 0xe9b6c7aa);	/* 20 */
	GG(a, b, c, d, x[5], S21, 0xd62f105d);	/* 21 */
	GG(d, a, b, c, x[10], S22, 0x2441453);	/* 22 */
	GG(c, d, a, b, x[15], S23, 0xd8a1e681);		/* 23 */
	GG(b, c, d, a, x[4], S24, 0xe7d3fbc8);	/* 24 */
	GG(a, b, c, d, x[9], S21, 0x21e1cde6);	/* 25 */
	GG(d, a, b, c, x[14], S22, 0xc33707d6);		/* 26 */
	GG(c, d, a, b, x[3], S23, 0xf4d50d87);	/* 27 */
	GG(b, c, d, a, x[8], S24, 0x455a14ed);	/* 28 */
	GG(a, b, c, d, x[13], S21, 0xa9e3e905);		/* 29 */
	GG(d, a, b, c, x[2], S22, 0xfcefa3f8);	/* 30 */
	GG(c, d, a, b, x[7], S23, 0x676f02d9);	/* 31 */
	GG(b, c, d, a, x[12], S24, 0x8d2a4c8a);		/* 32 */

	/* Round 3 */
	HH(a, b, c, d, x[5], S31, 0xfffa3942);	/* 33 */
	HH(d, a, b, c, x[8], S32, 0x8771f681);	/* 34 */
	HH(c, d, a, b, x[11], S33, 0x6d9d6122);		/* 35 */
	HH(b, c, d, a, x[14], S34, 0xfde5380c);		/* 36 */
	HH(a, b, c, d, x[1], S31, 0xa4beea44);	/* 37 */
	HH(d, a, b, c, x[4], S32, 0x4bdecfa9);	/* 38 */
	HH(c, d, a, b, x[7], S33, 0xf6bb4b60);	/* 39 */
	HH(b, c, d, a, x[10], S34, 0xbebfbc70);		/* 40 */
	HH(a, b, c, d, x[13], S31, 0x289b7ec6);		/* 41 */
	HH(d, a, b, c, x[0], S32, 0xeaa127fa);	/* 42 */
	HH(c, d, a, b, x[3], S33, 0xd4ef3085);	/* 43 */
	HH(b, c, d, a, x[6], S34, 0x4881d05);	/* 44 */
	HH(a, b, c, d, x[9], S31, 0xd9d4d039);	/* 45 */
	HH(d, a, b, c, x[12], S32, 0xe6db99e5);		/* 46 */
	HH(c, d, a, b, x[15], S33, 0x1fa27cf8);		/* 47 */
	HH(b, c, d, a, x[2], S34, 0xc4ac5665);	/* 48 */

	/* Round 4 */
	II(a, b, c, d, x[0], S41, 0xf4292244);	/* 49 */
	II(d, a, b, c, x[7], S42, 0x432aff97);	/* 50 */
	II(c, d, a, b, x[14], S43, 0xab9423a7);		/* 51 */
	II(b, c, d, a, x[5], S44, 0xfc93a039);	/* 52 */
	II(a, b, c, d, x[12], S41, 0x655b59c3);		/* 53 */
	II(d, a, b, c, x[3], S42, 0x8f0ccc92);	/* 54 */
	II(c, d, a, b, x[10], S43, 0xffeff47d);		/* 55 */
	II(b, c, d, a, x[1], S44, 0x85845dd1);	/* 56 */
	II(a, b, c, d, x[8], S41, 0x6fa87e4f);	/* 57 */
	II(d, a, b, c, x[15], S42, 0xfe2ce6e0);		/* 58 */
	II(c, d, a, b, x[6], S43, 0xa3014314);	/* 59 */
	II(b, c, d, a, x[13], S44, 0x4e0811a1);		/* 60 */
	II(a, b, c, d, x[4], S41, 0xf7537e82);	/* 61 */
	II(d, a, b, c, x[11], S42, 0xbd3af235);		/* 62 */
	II(c, d, a, b, x[2], S43, 0x2ad7d2bb);	/* 63 */
	II(b, c, d, a, x[9], S44, 0xeb86d391);	/* 64 */

	state[0] += a;
	state[1] += b;
	state[2] += c;
	state[3] += d;

	/* Zeroize sensitive information. */
	memset((unsigned char*) x, 0, sizeof(x));
}
/* }}} */

/* {{{ Encode
   Encodes input (php_uint32) into output (unsigned char). Assumes len is
   a multiple of 4.
 */
static void Encode(unsigned char *output, php_uint32 *input, unsigned int len)
{
	unsigned int i, j;

	for (i = 0, j = 0; j < len; i++, j += 4) {
		output[j] = (unsigned char) (input[i] & 0xff);
		output[j + 1] = (unsigned char) ((input[i] >> 8) & 0xff);
		output[j + 2] = (unsigned char) ((input[i] >> 16) & 0xff);
		output[j + 3] = (unsigned char) ((input[i] >> 24) & 0xff);
	}
}
/* }}} */

/* {{{ Decode
   Decodes input (unsigned char) into output (php_uint32). Assumes len is
   a multiple of 4.
 */
static void Decode(php_uint32 *output, const unsigned char *input, unsigned int len)
{
	unsigned int i, j;

	for (i = 0, j = 0; j < len; i++, j += 4)
		output[i] = ((php_uint32) input[j]) | (((php_uint32) input[j + 1]) << 8) |
			(((php_uint32) input[j + 2]) << 16) | (((php_uint32) input[j + 3]) << 24);
}
/* }}} */

#undef F
#undef G
#undef H
#undef I
#undef FF
#undef GG
#undef HH
#undef II

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
   | Author: Stefan Esser <sesser@php.net>                                |
   +----------------------------------------------------------------------+
*/

/* $Id: Digest.cpp,v 1.1.2.2 2006/04/15 20:19:12 prosl0am Exp $ */

#include <stdio.h>
//#include "php.h"

/* This code is heavily based on the PHP md5 implementation */ 

//#include "sha1.h"

ZEND_API void make_sha1_digest(char *sha1str, unsigned char *digest)
{
	int i;

	for (i = 0; i < 20; i++) {
		sprintf(sha1str, "%02x", digest[i]);
		sha1str += 2;
	}

	*sha1str = '\0';
}

static void SHA1Transform(php_uint32[5], const unsigned char[64]);
static void SHA1Encode(unsigned char *, php_uint32 *, unsigned int);
static void SHA1Decode(php_uint32 *, const unsigned char *, unsigned int);

static unsigned char PADDING_[64] =
{
	0x80, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
	0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
	0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0
};

/* F, G, H and I are basic SHA1 functions.
 */
#define F(x, y, z) ((z) ^ ((x) & ((y) ^ (z))))
#define G(x, y, z) ((x) ^ (y) ^ (z))
#define H(x, y, z) (((x) & (y)) | ((z) & ((x) | (y))))
#define I(x, y, z) ((x) ^ (y) ^ (z))

/* ROTATE_LEFT rotates x left n bits.
 */
#define ROTATE_LEFT(x, n) (((x) << (n)) | ((x) >> (32-(n))))

/* W[i]
 */
#define W(i) ( tmp=x[(i-3)&15]^x[(i-8)&15]^x[(i-14)&15]^x[i&15], \
	(x[i&15]=ROTATE_LEFT(tmp, 1)) )  

/* FF, GG, HH, and II transformations for rounds 1, 2, 3, and 4.
 */
#define FF(a, b, c, d, e, w) { \
 (e) += F ((b), (c), (d)) + (w) + (php_uint32)(0x5A827999); \
 (e) += ROTATE_LEFT ((a), 5); \
 (b) = ROTATE_LEFT((b), 30); \
  }
#define GG(a, b, c, d, e, w) { \
 (e) += G ((b), (c), (d)) + (w) + (php_uint32)(0x6ED9EBA1); \
 (e) += ROTATE_LEFT ((a), 5); \
 (b) = ROTATE_LEFT((b), 30); \
  }
#define HH(a, b, c, d, e, w) { \
 (e) += H ((b), (c), (d)) + (w) + (php_uint32)(0x8F1BBCDC); \
 (e) += ROTATE_LEFT ((a), 5); \
 (b) = ROTATE_LEFT((b), 30); \
  }
#define II(a, b, c, d, e, w) { \
 (e) += I ((b), (c), (d)) + (w) + (php_uint32)(0xCA62C1D6); \
 (e) += ROTATE_LEFT ((a), 5); \
 (b) = ROTATE_LEFT((b), 30); \
  }
			                    

/* {{{ PHP_SHA1Init
 * SHA1 initialization. Begins an SHA1 operation, writing a new context.
 */
ZEND_API void PHP_SHA1Init(PHP_SHA1_CTX * context)
{
	context->count[0] = context->count[1] = 0;
	/* Load magic initialization constants.
	 */
	context->state[0] = 0x67452301;
	context->state[1] = 0xefcdab89;
	context->state[2] = 0x98badcfe;
	context->state[3] = 0x10325476;
	context->state[4] = 0xc3d2e1f0;
}
/* }}} */

/* {{{ PHP_SHA1Update
   SHA1 block update operation. Continues an SHA1 message-digest
   operation, processing another message block, and updating the
   context.
 */
ZEND_API void PHP_SHA1Update(PHP_SHA1_CTX * context, const unsigned char *input,
			   unsigned int inputLen)
{
	unsigned int i, index, partLen;

	/* Compute number of bytes mod 64 */
	index = (unsigned int) ((context->count[0] >> 3) & 0x3F);

	/* Update number of bits */
	if ((context->count[0] += ((php_uint32) inputLen << 3))
		< ((php_uint32) inputLen << 3))
		context->count[1]++;
	context->count[1] += ((php_uint32) inputLen >> 29);

	partLen = 64 - index;

	/* Transform as many times as possible.
	 */
	if (inputLen >= partLen) {
		memcpy
			((unsigned char*) & context->buffer[index], (unsigned char*) input, partLen);
		SHA1Transform(context->state, context->buffer);

		for (i = partLen; i + 63 < inputLen; i += 64)
			SHA1Transform(context->state, &input[i]);

		index = 0;
	} else
		i = 0;

	/* Buffer remaining input */
	memcpy
		((unsigned char*) & context->buffer[index], (unsigned char*) & input[i],
		 inputLen - i);
}
/* }}} */

/* {{{ PHP_SHA1Final
   SHA1 finalization. Ends an SHA1 message-digest operation, writing the
   the message digest and zeroizing the context.
 */
ZEND_API void PHP_SHA1Final(unsigned char digest[20], PHP_SHA1_CTX * context)
{
	unsigned char bits[8];
	unsigned int index, padLen;

	/* Save number of bits */
	bits[7] = context->count[0] & 0xFF;
	bits[6] = (context->count[0] >> 8) & 0xFF;
	bits[5] = (context->count[0] >> 16) & 0xFF;
	bits[4] = (context->count[0] >> 24) & 0xFF;
	bits[3] = context->count[1] & 0xFF;
	bits[2] = (context->count[1] >> 8) & 0xFF;
	bits[1] = (context->count[1] >> 16) & 0xFF;
	bits[0] = (context->count[1] >> 24) & 0xFF;
	
	/* Pad out to 56 mod 64.
	 */
	index = (unsigned int) ((context->count[0] >> 3) & 0x3f);
	padLen = (index < 56) ? (56 - index) : (120 - index);
	PHP_SHA1Update(context, PADDING_, padLen);

	/* Append length (before padding) */
	PHP_SHA1Update(context, bits, 8);

	/* Store state in digest */
	SHA1Encode(digest, context->state, 20);

	/* Zeroize sensitive information.
	 */
	memset((unsigned char*) context, 0, sizeof(*context));
}
/* }}} */

/* {{{ SHA1Transform
 * SHA1 basic transformation. Transforms state based on block.
 */
static void SHA1Transform(php_uint32 state[5], const unsigned char block[64])
{
	php_uint32 a = state[0], b = state[1], c = state[2];
	php_uint32 d = state[3], e = state[4], x[16], tmp;

	SHA1Decode(x, block, 64);

	/* Round 1 */
	FF(a, b, c, d, e, x[0]);   /* 1 */
	FF(e, a, b, c, d, x[1]);   /* 2 */
	FF(d, e, a, b, c, x[2]);   /* 3 */
	FF(c, d, e, a, b, x[3]);   /* 4 */
	FF(b, c, d, e, a, x[4]);   /* 5 */
	FF(a, b, c, d, e, x[5]);   /* 6 */
	FF(e, a, b, c, d, x[6]);   /* 7 */
	FF(d, e, a, b, c, x[7]);   /* 8 */
	FF(c, d, e, a, b, x[8]);   /* 9 */
	FF(b, c, d, e, a, x[9]);   /* 10 */
	FF(a, b, c, d, e, x[10]);  /* 11 */
	FF(e, a, b, c, d, x[11]);  /* 12 */
	FF(d, e, a, b, c, x[12]);  /* 13 */
	FF(c, d, e, a, b, x[13]);  /* 14 */
	FF(b, c, d, e, a, x[14]);  /* 15 */
	FF(a, b, c, d, e, x[15]);  /* 16 */
	FF(e, a, b, c, d, W(16));  /* 17 */
	FF(d, e, a, b, c, W(17));  /* 18 */
	FF(c, d, e, a, b, W(18));  /* 19 */
	FF(b, c, d, e, a, W(19));  /* 20 */

	/* Round 2 */
	GG(a, b, c, d, e, W(20));  /* 21 */
	GG(e, a, b, c, d, W(21));  /* 22 */
	GG(d, e, a, b, c, W(22));  /* 23 */
	GG(c, d, e, a, b, W(23));  /* 24 */
	GG(b, c, d, e, a, W(24));  /* 25 */
	GG(a, b, c, d, e, W(25));  /* 26 */
	GG(e, a, b, c, d, W(26));  /* 27 */
	GG(d, e, a, b, c, W(27));  /* 28 */
	GG(c, d, e, a, b, W(28));  /* 29 */
	GG(b, c, d, e, a, W(29));  /* 30 */
	GG(a, b, c, d, e, W(30));  /* 31 */
	GG(e, a, b, c, d, W(31));  /* 32 */
	GG(d, e, a, b, c, W(32));  /* 33 */
	GG(c, d, e, a, b, W(33));  /* 34 */
	GG(b, c, d, e, a, W(34));  /* 35 */
	GG(a, b, c, d, e, W(35));  /* 36 */
	GG(e, a, b, c, d, W(36));  /* 37 */
	GG(d, e, a, b, c, W(37));  /* 38 */
	GG(c, d, e, a, b, W(38));  /* 39 */
	GG(b, c, d, e, a, W(39));  /* 40 */

	/* Round 3 */
	HH(a, b, c, d, e, W(40));  /* 41 */
	HH(e, a, b, c, d, W(41));  /* 42 */
	HH(d, e, a, b, c, W(42));  /* 43 */
	HH(c, d, e, a, b, W(43));  /* 44 */
	HH(b, c, d, e, a, W(44));  /* 45 */
	HH(a, b, c, d, e, W(45));  /* 46 */
	HH(e, a, b, c, d, W(46));  /* 47 */
	HH(d, e, a, b, c, W(47));  /* 48 */
	HH(c, d, e, a, b, W(48));  /* 49 */
	HH(b, c, d, e, a, W(49));  /* 50 */
	HH(a, b, c, d, e, W(50));  /* 51 */
	HH(e, a, b, c, d, W(51));  /* 52 */
	HH(d, e, a, b, c, W(52));  /* 53 */
	HH(c, d, e, a, b, W(53));  /* 54 */
	HH(b, c, d, e, a, W(54));  /* 55 */
	HH(a, b, c, d, e, W(55));  /* 56 */
	HH(e, a, b, c, d, W(56));  /* 57 */
	HH(d, e, a, b, c, W(57));  /* 58 */
	HH(c, d, e, a, b, W(58));  /* 59 */
	HH(b, c, d, e, a, W(59));  /* 60 */

	/* Round 4 */
	II(a, b, c, d, e, W(60));  /* 61 */
	II(e, a, b, c, d, W(61));  /* 62 */
	II(d, e, a, b, c, W(62));  /* 63 */
	II(c, d, e, a, b, W(63));  /* 64 */
	II(b, c, d, e, a, W(64));  /* 65 */
	II(a, b, c, d, e, W(65));  /* 66 */
	II(e, a, b, c, d, W(66));  /* 67 */
	II(d, e, a, b, c, W(67));  /* 68 */
	II(c, d, e, a, b, W(68));  /* 69 */
	II(b, c, d, e, a, W(69));  /* 70 */
	II(a, b, c, d, e, W(70));  /* 71 */
	II(e, a, b, c, d, W(71));  /* 72 */
	II(d, e, a, b, c, W(72));  /* 73 */
	II(c, d, e, a, b, W(73));  /* 74 */
	II(b, c, d, e, a, W(74));  /* 75 */
	II(a, b, c, d, e, W(75));  /* 76 */
	II(e, a, b, c, d, W(76));  /* 77 */
	II(d, e, a, b, c, W(77));  /* 78 */
	II(c, d, e, a, b, W(78));  /* 79 */
	II(b, c, d, e, a, W(79));  /* 80 */

	state[0] += a;
	state[1] += b;
	state[2] += c;
	state[3] += d;
	state[4] += e;

	/* Zeroize sensitive information. */
	memset((unsigned char*) x, 0, sizeof(x));
}
/* }}} */

/* {{{ SHA1Encode
   Encodes input (php_uint32) into output (unsigned char). Assumes len is
   a multiple of 4.
 */
static void SHA1Encode(unsigned char *output, php_uint32 *input, unsigned int len)
{
	unsigned int i, j;

	for (i = 0, j = 0; j < len; i++, j += 4) {
		output[j] = (unsigned char) ((input[i] >> 24) & 0xff);
		output[j + 1] = (unsigned char) ((input[i] >> 16) & 0xff);
		output[j + 2] = (unsigned char) ((input[i] >> 8) & 0xff);
		output[j + 3] = (unsigned char) (input[i] & 0xff);
	}
}
/* }}} */

/* {{{ SHA1Decode
   Decodes input (unsigned char) into output (php_uint32). Assumes len is
   a multiple of 4.
 */
static void SHA1Decode(php_uint32 *output, const unsigned char *input, unsigned int len)
{
	unsigned int i, j;

	for (i = 0, j = 0; j < len; i++, j += 4)
		output[i] = ((php_uint32) input[j + 3]) | (((php_uint32) input[j + 2]) << 8) |
			(((php_uint32) input[j + 1]) << 16) | (((php_uint32) input[j]) << 24);
}
/* }}} */
