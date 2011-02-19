//
// ExtSupport.PHP4 - substitute for php4ts.dll
//
// InternalFileFunctions.h
// - contains declarations of "zif" exported file functions
//

#pragma once

#include "stdafx.h"
#include "ExtSupport.h"

#include "Streams.h"

#ifdef __cplusplus
extern "C"
{
#endif

ZEND_API ZEND_FUNCTION(fclose);
ZEND_API ZEND_FUNCTION(feof);
ZEND_API ZEND_FUNCTION(fgets);
ZEND_API ZEND_FUNCTION(fgetc);
ZEND_API ZEND_FUNCTION(fgetss);
ZEND_API ZEND_FUNCTION(fwrite);
ZEND_API ZEND_FUNCTION(fflush);
ZEND_API ZEND_FUNCTION(rewind);
ZEND_API ZEND_FUNCTION(ftell);
ZEND_API ZEND_FUNCTION(fseek);
ZEND_API ZEND_FUNCTION(fpassthru);
ZEND_API ZEND_FUNCTION(fread);

extern ZEND_API FILE *(*zend_fopen)(const char *filename, char **opened_path);
extern ZEND_API zend_bool (*zend_open)(const char *filename, zend_file_handle *);

ZEND_API void zend_file_handle_dtor(zend_file_handle *fh);

#ifdef __cplusplus
}
#endif
