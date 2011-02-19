//
// ExtSupport.PHP4 - substitute for php4ts.dll
//
// Images.h
// - contains declarations of image related exported functions
//

#pragma once

#include "stdafx.h"
#include "ExtSupport.h"
#include "Streams.h"

#ifdef __cplusplus
extern "C"
{
#endif

ZEND_API extern char php_sig_gif[3];
ZEND_API extern char php_sig_psd[4];
ZEND_API extern char php_sig_bmp[2];
ZEND_API extern char php_sig_swf[3];
ZEND_API extern char php_sig_swc[3];
ZEND_API extern char php_sig_jpg[3];
ZEND_API extern char php_sig_png[8];
ZEND_API extern char php_sig_tif_ii[4];
ZEND_API extern char php_sig_tif_mm[4];
ZEND_API extern char php_sig_jpc[3];
ZEND_API extern char php_sig_jp2[12];
ZEND_API extern char php_sig_iff[4];

ZEND_API int php_tiff_bytes_per_format[];

ZEND_API const char *php_image_type_to_mime_type(int image_type);

typedef enum
{
	IMAGE_FILETYPE_UNKNOWN = 0,
	IMAGE_FILETYPE_GIF = 1,
	IMAGE_FILETYPE_JPEG,
	IMAGE_FILETYPE_PNG,
	IMAGE_FILETYPE_SWF,
	IMAGE_FILETYPE_PSD,
	IMAGE_FILETYPE_BMP,
	IMAGE_FILETYPE_TIFF_II, /* intel */
	IMAGE_FILETYPE_TIFF_MM, /* motorola */
	IMAGE_FILETYPE_JPC,
	IMAGE_FILETYPE_JP2,
	IMAGE_FILETYPE_JPX,
	IMAGE_FILETYPE_JB2,
	IMAGE_FILETYPE_SWC,
	IMAGE_FILETYPE_IFF,
	IMAGE_FILETYPE_WBMP,
	/* IMAGE_FILETYPE_JPEG2000 is a userland alias for IMAGE_FILETYPE_JPC */
	IMAGE_FILETYPE_XBM
	/* WHEN EXTENDING: PLEASE ALSO REGISTER IN image.c:PHP_MINIT_FUNCTION(imagetypes) */
}
image_filetype;

ZEND_API int php_getimagetype(php_stream *stream, char *filetype TSRMLS_DC);

#ifdef __cplusplus
}
#endif
