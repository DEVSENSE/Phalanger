//
// ExtSupport.PHP4 - substitute for php4ts.dll
//
// Images.cpp 
// - contains definitions of image related functions
//

#include "stdafx.h"
#include "Images.h"
#include "Streams.h"
#include "Errors.h"
#include "Memory.h"

#include <stdio.h>

#pragma unmanaged

ZEND_API char php_sig_gif[3] =		{'G', 'I', 'F'};
ZEND_API char php_sig_psd[4] =		{'8', 'B', 'P', 'S'};
ZEND_API char php_sig_bmp[2] =		{'B', 'M'};
ZEND_API char php_sig_swf[3] =		{'F', 'W', 'S'};
ZEND_API char php_sig_swc[3] =		{'C', 'W', 'S'};
ZEND_API char php_sig_jpg[3] =		{(char) 0xff, (char) 0xd8, (char) 0xff};
ZEND_API char php_sig_png[8] =		{(char) 0x89, (char) 0x50, (char) 0x4e, (char) 0x47,
									(char) 0x0d, (char) 0x0a, (char) 0x1a, (char) 0x0a};
ZEND_API char php_sig_tif_ii[4] =	{'I','I', (char)0x2A, (char)0x00};
ZEND_API char php_sig_tif_mm[4] =	{'M','M', (char)0x00, (char)0x2A};
ZEND_API char php_sig_jpc[3]  =		{(char)0xff, (char)0x4f, (char)0xff};
ZEND_API char php_sig_jp2[12] =		{(char)0x00, (char)0x00, (char)0x00, (char)0x0c,
									(char)0x6a, (char)0x50, (char)0x20, (char)0x20,
									(char)0x0d, (char)0x0a, (char)0x87, (char)0x0a};
ZEND_API char php_sig_iff[4] =		{'F','O','R','M'};

ZEND_API int php_tiff_bytes_per_format[] = {0, 1, 1, 2, 4, 8, 1, 1, 2, 4, 8, 4, 8};


struct gfxinfo {
	unsigned int width;
	unsigned int height;
	unsigned int bits;
	unsigned int channels;
};

// copied from image.c and beautified
/* {{{ php_get_wbmp
 * int WBMP file format type
 * byte Header Type
 *	byte Extended Header
 *		byte Header Data (type 00 = multibyte)
 *		byte Header Data (type 11 = name/pairs)
 * int Number of columns
 * int Number of rows
 */
static int php_get_wbmp(php_stream *stream, struct gfxinfo **result, int check TSRMLS_DC)
{
	int i, width = 0, height = 0;

	if (php_stream_rewind(stream)) return 0;

	/* get type */
	if (php_stream_getc(stream) != 0) return 0;

	/* skip header */
	do
	{
		i = php_stream_getc(stream);
		if (i < 0) return 0;
	}
	while (i & 0x80);

	/* get width */
	do
	{
		i = php_stream_getc(stream);
		if (i < 0) return 0;
		width = (width << 7) | (i & 0x7f);
	}
	while (i & 0x80);
	
	/* get height */
	do
	{
		i = php_stream_getc(stream);
		if (i < 0) return 0;
		height = (height << 7) | (i & 0x7f);
	}
	while (i & 0x80);
	
	if (!check)
	{
		(*result)->width = width;
		(*result)->height = height;
	}

	return IMAGE_FILETYPE_WBMP;
}

// copied from image.c and beautified
static int php_get_xbm(php_stream *stream, struct gfxinfo **result TSRMLS_DC)
{
    char *fline;
    char *iname;
    char *type;
    int value;
    unsigned int width = 0, height = 0;

	if (result) *result = NULL;
	if (php_stream_rewind(stream)) return 0;

	while ((fline = php_stream_gets(stream, NULL, 0)) != NULL)
	{
		iname = estrdup(fline); /* simple way to get necessary buffer of required size */
		if (sscanf(fline, "#define %s %d", iname, &value) == 2)
		{
			if (!(type = strrchr(iname, '_'))) type = iname;
			else type++;
	
			if (!strcmp("width", type))
			{
				width = (unsigned int)value;
				if (height)
				{
					efree(iname);
					break;
				}
			}
			if (!strcmp("height", type))
			{
				height = (unsigned int)value;
				if (width)
				{
					efree(iname);
					break;
				}
			}
		}
		efree(fline);
		efree(iname);
	}
	if (fline) efree(fline);

	if (width && height)\
	{
		if (result)
		{
			*result = (struct gfxinfo *)ecalloc(1, sizeof(struct gfxinfo));
			(*result)->width = width;
			(*result)->height = height;
		}
		return IMAGE_FILETYPE_XBM;
	}

	return 0;
}

// copied from image.c and beautified
ZEND_API const char *php_image_type_to_mime_type(int image_type)
{
	switch (image_type)
	{
		case IMAGE_FILETYPE_GIF:		return "image/gif";
		case IMAGE_FILETYPE_JPEG:		return "image/jpeg";
		case IMAGE_FILETYPE_PNG:		return "image/png";
		case IMAGE_FILETYPE_SWF:
		case IMAGE_FILETYPE_SWC:		return "application/x-shockwave-flash";
		case IMAGE_FILETYPE_PSD:		return "image/psd";
		case IMAGE_FILETYPE_BMP:		return "image/bmp";
		case IMAGE_FILETYPE_TIFF_II:
		case IMAGE_FILETYPE_TIFF_MM:	return "image/tiff";
		case IMAGE_FILETYPE_IFF:		return "image/iff";
		case IMAGE_FILETYPE_WBMP:		return "image/vnd.wap.wbmp";
		case IMAGE_FILETYPE_JPC:		return "application/octet-stream";
		case IMAGE_FILETYPE_JP2:		return "image/jp2";
		case IMAGE_FILETYPE_XBM:		return "image/xbm";
		default:
		case IMAGE_FILETYPE_UNKNOWN:	return "application/octet-stream"; /* suppose binary format */
	}
}

/* detect filetype from first bytes */
// copied from image.c and beautified
ZEND_API int php_getimagetype(php_stream *stream, char *filetype TSRMLS_DC)
{
	char tmp[12];

	if (!filetype) filetype = tmp;
	if ((php_stream_read(stream, filetype, 3)) != 3)
	{
		php_error_docref(NULL TSRMLS_CC, E_WARNING, "Read error!");
		return IMAGE_FILETYPE_UNKNOWN;
	}

	/* BYTES READ: 3 */
	if (!memcmp(filetype, php_sig_gif, 3)) return IMAGE_FILETYPE_GIF;
	else if (!memcmp(filetype, php_sig_jpg, 3)) return IMAGE_FILETYPE_JPEG;
	else if (!memcmp(filetype, php_sig_png, 3))
	{
		if (php_stream_read(stream, filetype + 3, 5) != 5)
		{
			php_error_docref(NULL TSRMLS_CC, E_WARNING, "Read error!");
			return IMAGE_FILETYPE_UNKNOWN;
		}
		if (!memcmp(filetype, php_sig_png, 8)) return IMAGE_FILETYPE_PNG;
		else
		{
			php_error_docref(NULL TSRMLS_CC, E_WARNING, "PNG file corrupted by ASCII conversion");
			return IMAGE_FILETYPE_UNKNOWN;
		}
	}
	else if (!memcmp(filetype, php_sig_swf, 3)) return IMAGE_FILETYPE_SWF;
	else if (!memcmp(filetype, php_sig_swc, 3)) return IMAGE_FILETYPE_SWC;
	else if (!memcmp(filetype, php_sig_psd, 3)) return IMAGE_FILETYPE_PSD;
	else if (!memcmp(filetype, php_sig_bmp, 2)) return IMAGE_FILETYPE_BMP;
	else if (!memcmp(filetype, php_sig_jpc, 3)) return IMAGE_FILETYPE_JPC;
	
	if (php_stream_read(stream, filetype + 3, 1) != 1)
	{
		php_error_docref(NULL TSRMLS_CC, E_WARNING, "Read error!");
		return IMAGE_FILETYPE_UNKNOWN;
	}

	/* BYTES READ: 4 */
	if (!memcmp(filetype, php_sig_tif_ii, 4)) return IMAGE_FILETYPE_TIFF_II;
	else if (!memcmp(filetype, php_sig_tif_mm, 4)) return IMAGE_FILETYPE_TIFF_MM;
	else if (!memcmp(filetype, php_sig_iff, 4)) return IMAGE_FILETYPE_IFF;

	if (php_stream_read(stream, filetype + 4, 8) != 8)
	{
		php_error_docref(NULL TSRMLS_CC, E_WARNING, "Read error!");
		return IMAGE_FILETYPE_UNKNOWN;
	}

	/* BYTES READ: 12 */
   	if (!memcmp(filetype, php_sig_jp2, 12)) return IMAGE_FILETYPE_JP2;

	/* AFTER ALL ABOVE FAILED */
	if (php_get_wbmp(stream, NULL, 1 TSRMLS_CC)) return IMAGE_FILETYPE_WBMP;
	if (php_get_xbm(stream, NULL TSRMLS_CC)) return IMAGE_FILETYPE_XBM;

	return IMAGE_FILETYPE_UNKNOWN;
}
