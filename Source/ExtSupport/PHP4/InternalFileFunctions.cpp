//
// ExtSupport.PHP4 - substitute for php4ts.dll
//
// InternalFileFunctions.cpp
// - contains definitions of "zif" exported file functions
//

#include "stdafx.h"
#include "InternalFileFunctions.h"
#include "Streams.h"
#include "Parameters.h"
#include "Memory.h"
#include "Errors.h"
#include "Resources.h"
#include "Variables.h"
#include "Strings.h"
#include "TsrmLs.h"
#include "Streams.h"

#include <stdio.h>
#include <io.h>

#pragma unmanaged

// copied from file.c and beautified
/* {{{ proto bool fclose(resource fp)
   Close an open file pointer */
ZEND_API ZEND_FUNCTION(fclose)
{
	zval **arg1;
	php_stream *stream;

	if (ZEND_NUM_ARGS() != 1 || zend_get_parameters_ex(1, &arg1) == FAILURE) WRONG_PARAM_COUNT;

	php_stream_from_zval(stream, arg1);
	if (!stream->is_persistent)	zend_list_delete(stream->rsrc_id);
	else php_stream_pclose(stream);

	RETURN_TRUE;
}

// copied from file.c and beautified
/* {{{ proto bool feof(resource fp)
   Test for end-of-file on a file pointer */
ZEND_API ZEND_FUNCTION(feof)
{
	zval **arg1;
	php_stream *stream;

	if (ZEND_NUM_ARGS() != 1 || zend_get_parameters_ex(1, &arg1) == FAILURE) WRONG_PARAM_COUNT;

	php_stream_from_zval(stream, arg1);

	if (php_stream_eof(stream))
	{
		RETURN_TRUE;
	}
	else RETURN_FALSE;
}
/* }}} */

#pragma managed

// copied from file.c and beautified
/* {{{ proto string fgets(resource fp[, int length])
   Get a line from file pointer */
ZEND_API ZEND_FUNCTION(fgets)
{
	zval **arg1, **arg2;
	int len = 1024;
	char *buf = NULL;
	int argc = ZEND_NUM_ARGS();
	size_t line_len = 0;
	php_stream *stream;

	if (argc<1 || argc>2 || zend_get_parameters_ex(argc, &arg1, &arg2) == FAILURE) WRONG_PARAM_COUNT;

	php_stream_from_zval(stream, arg1);

	if (argc == 1)
	{
		/* ask streams to give us a buffer of an appropriate size */
		buf = php_stream_get_line(stream, NULL, 0, &line_len);
		if (buf == NULL) goto exit_failed;
	}
	else if (argc > 1)
	{
		convert_to_long_ex(arg2);
		len = Z_LVAL_PP(arg2);

		if (len < 0)
		{
			php_error_docref(NULL TSRMLS_CC, E_WARNING, "Length parameter may not be negative");
			RETURN_FALSE;
		}

		buf = (char *)ecalloc(len + 1, sizeof(char));
		if (php_stream_get_line(stream, buf, len, &line_len) == NULL) goto exit_failed;
	}
	
	//if (PG(magic_quotes_runtime))
	if (PHP::Core::ScriptContext::CurrentContext->Config->Variables->QuoteRuntimeVariables)
	{
		Z_STRVAL_P(return_value) = php_addslashes(buf, line_len, &Z_STRLEN_P(return_value), 1 TSRMLS_CC);
		Z_TYPE_P(return_value) = IS_STRING;
	}
	//else
	//{
	//	ZVAL_STRINGL(return_value, buf, line_len, 0);
	//	/* resize buffer if it's much larger than the result.
	//	 * Only needed if the user requested a buffer size. */
	//	if (argc > 1 && Z_STRLEN_P(return_value) < len / 2)
	//	{
	//		Z_STRVAL_P(return_value) = (char *)erealloc(buf, line_len + 1);
	//	}
	//}
	return;

exit_failed:
	RETVAL_FALSE;
	if (buf) efree(buf);
}
/* }}} */

#pragma unmanaged

// copied from file.c and beautified
/* {{{ proto string fgetc(resource fp)
   Get a character from file pointer */
ZEND_API ZEND_FUNCTION(fgetc)
{
	zval **arg1;
	char *buf;
	int result;
	php_stream *stream;

	if (ZEND_NUM_ARGS() != 1 || zend_get_parameters_ex(1, &arg1) == FAILURE) WRONG_PARAM_COUNT;

	php_stream_from_zval(stream, arg1);

	buf = (char *)safe_emalloc(2, sizeof(char), 0);

	result = php_stream_getc(stream);

	if (result == EOF)
	{
		efree(buf);
		RETVAL_FALSE;
	}
	else
	{
		buf[0] = result;
		buf[1] = '\0';

		RETURN_STRINGL(buf, 1, 0);
	}
}
/* }}} */

// copied from file.c and beautified
/* {{{ proto string fgetss(resource fp [, int length, string allowable_tags])
   Get a line from file pointer and strip HTML tags */
ZEND_API ZEND_FUNCTION(fgetss)
{
	zval **fd, **bytes = NULL, **allow = NULL;
	size_t len = 0;
	size_t actual_len, retval_len;
	char *buf = NULL, *retval;
	php_stream *stream;
	char *allowed_tags=NULL;
	int allowed_tags_len=0;

	switch (ZEND_NUM_ARGS())
	{
		case 1:
			if (zend_get_parameters_ex(1, &fd) == FAILURE) RETURN_FALSE;
			break;

		case 2:
			if (zend_get_parameters_ex(2, &fd, &bytes) == FAILURE) RETURN_FALSE;
			break;

		case 3:
			if (zend_get_parameters_ex(3, &fd, &bytes, &allow) == FAILURE) RETURN_FALSE;
			convert_to_string_ex(allow);
			allowed_tags = Z_STRVAL_PP(allow);
			allowed_tags_len = Z_STRLEN_PP(allow);
			break;

		default:
			WRONG_PARAM_COUNT;
			/* NOTREACHED */
			break;
	}

	php_stream_from_zval(stream, fd);

	if (bytes != NULL)
	{
		convert_to_long_ex(bytes);
		if (Z_LVAL_PP(bytes) < 0)
		{
			php_error_docref(NULL TSRMLS_CC, E_WARNING, "Length parameter may not be negative");
			RETURN_FALSE;
		}

		len = (size_t) Z_LVAL_PP(bytes);
		buf = (char *)safe_emalloc(sizeof(char), (len + 1), 0);
		/*needed because recv doesnt set null char at end*/
		memset(buf, 0, len + 1);
	}

	if ((retval = php_stream_get_line(stream, buf, len, &actual_len)) == NULL)
	{
		if (buf != NULL) efree(buf);
		RETURN_FALSE;
	}

	retval_len = php_strip_tags(retval, actual_len, &stream->fgetss_state, allowed_tags, allowed_tags_len);

	RETURN_STRINGL(retval, retval_len, 0);
}
/* }}} */

#pragma managed

// copied from file.c and beautified
/* {{{ proto int fwrite(resource fp, string str [, int length])
   Binary-safe file write */
ZEND_API ZEND_FUNCTION(fwrite)
{
	zval **arg1, **arg2, **arg3=NULL;
	int ret;
	int num_bytes;
	char *buffer = NULL;
	php_stream *stream;

	switch (ZEND_NUM_ARGS())
	{
		case 2:
			if (zend_get_parameters_ex(2, &arg1, &arg2) == FAILURE) RETURN_FALSE;
			convert_to_string_ex(arg2);
			num_bytes = Z_STRLEN_PP(arg2);
			break;

		case 3:
			if (zend_get_parameters_ex(3, &arg1, &arg2, &arg3) == FAILURE) RETURN_FALSE;
			convert_to_string_ex(arg2);
			convert_to_long_ex(arg3);
			num_bytes = MIN(Z_LVAL_PP(arg3), Z_STRLEN_PP(arg2));
			break;

		default:
			WRONG_PARAM_COUNT;
			/* NOTREACHED */
			break;
	}

	php_stream_from_zval(stream, arg1);

	//if (!arg3 && PG(magic_quotes_runtime))
	if (!arg3 && PHP::Core::ScriptContext::CurrentContext->Config->Variables->QuoteRuntimeVariables)
	{
		buffer = estrndup(Z_STRVAL_PP(arg2), Z_STRLEN_PP(arg2));
		php_stripslashes(buffer, &num_bytes TSRMLS_CC);
	}

	ret = php_stream_write(stream, buffer ? buffer : Z_STRVAL_PP(arg2), num_bytes);
	if (buffer) efree(buffer);

	RETURN_LONG(ret);
}
/* }}} */

#pragma unmanaged

// copied from file.c and beautified
/* {{{ proto bool fflush(resource fp)
   Flushes output */
ZEND_API ZEND_FUNCTION(fflush)
{
	zval **arg1;
	int ret;
	php_stream *stream;

	if (ZEND_NUM_ARGS() != 1 || zend_get_parameters_ex(1, &arg1) == FAILURE) WRONG_PARAM_COUNT;

	php_stream_from_zval(stream, arg1);

	ret = php_stream_flush(stream);
	if (ret) RETURN_FALSE;

	RETURN_TRUE;
}
/* }}} */

// copied from file.c and beautified
/* {{{ proto bool rewind(resource fp)
   Rewind the position of a file pointer */
ZEND_API ZEND_FUNCTION(rewind)
{
	zval **arg1;
	php_stream *stream;

	if (ZEND_NUM_ARGS() != 1 || zend_get_parameters_ex(1, &arg1) == FAILURE) WRONG_PARAM_COUNT;

	php_stream_from_zval(stream, arg1);

	if (-1 == php_stream_rewind(stream)) RETURN_FALSE;
	RETURN_TRUE;
}
/* }}} */

// copied from file.c and beautified
/* {{{ proto int ftell(resource fp)
   Get file pointer's read/write position */
ZEND_API ZEND_FUNCTION(ftell)
{
	zval **arg1;
	long ret;
	php_stream *stream;

	if (ZEND_NUM_ARGS() != 1 || zend_get_parameters_ex(1, &arg1) == FAILURE) WRONG_PARAM_COUNT;

	php_stream_from_zval(stream, arg1);

	ret = php_stream_tell(stream);
	if (ret == -1) RETURN_FALSE;

	RETURN_LONG(ret);
}
/* }}} */

// copied from file.c and beautified
/* {{{ proto int fseek(resource fp, int offset [, int whence])
   Seek on a file pointer */
ZEND_API ZEND_FUNCTION(fseek)
{
	zval **arg1, **arg2, **arg3;
	int argcount = ZEND_NUM_ARGS(), whence = SEEK_SET;
	php_stream *stream;

	if (argcount < 2 || argcount > 3 || zend_get_parameters_ex(argcount, &arg1, &arg2, &arg3) == FAILURE)
	{
		WRONG_PARAM_COUNT;
	}

	php_stream_from_zval(stream, arg1);

	convert_to_long_ex(arg2);
	if (argcount > 2)
	{
		convert_to_long_ex(arg3);
		whence = Z_LVAL_PP(arg3);
	}

	RETURN_LONG(php_stream_seek(stream, Z_LVAL_PP(arg2), whence));
}

// copied from file.c and beautified
/* {{{ proto int fpassthru(resource fp)
   Output all remaining data from a file pointer */
ZEND_API ZEND_FUNCTION(fpassthru)
{
	zval **arg1;
	int size;
	php_stream *stream;

	if (ZEND_NUM_ARGS() != 1 || zend_get_parameters_ex(1, &arg1) == FAILURE) WRONG_PARAM_COUNT;

	php_stream_from_zval(stream, arg1);

	size = php_stream_passthru(stream);
	RETURN_LONG(size);
}
/* }}} */

#pragma managed

// copied from file.c and beautified
/* {{{ proto string fread(resource fp, int length)
   Binary-safe file read */
ZEND_API ZEND_FUNCTION(fread)
{
	zval **arg1, **arg2;
	int len;
	php_stream *stream;

	if (ZEND_NUM_ARGS() != 2 || zend_get_parameters_ex(2, &arg1, &arg2) == FAILURE) WRONG_PARAM_COUNT;

	php_stream_from_zval(stream, arg1);

	convert_to_long_ex(arg2);
	len = Z_LVAL_PP(arg2);
	if (len < 0)
	{
		php_error_docref(NULL TSRMLS_CC, E_WARNING, "Length parameter may not be negative");
		RETURN_FALSE;
	}

	Z_STRVAL_P(return_value) = (char *)emalloc(len + 1);
	Z_STRLEN_P(return_value) = php_stream_read(stream, Z_STRVAL_P(return_value), len);

	/* needed because recv/read/gzread doesnt put a null at the end*/
	Z_STRVAL_P(return_value)[Z_STRLEN_P(return_value)] = 0;

	//if (PG(magic_quotes_runtime))
	if (PHP::Core::ScriptContext::CurrentContext->Config->Variables->QuoteRuntimeVariables)
	{
		Z_STRVAL_P(return_value) = php_addslashes(Z_STRVAL_P(return_value), 
				Z_STRLEN_P(return_value), &Z_STRLEN_P(return_value), 1 TSRMLS_CC);
	}
	Z_TYPE_P(return_value) = IS_STRING;
}
/* }}} */

#pragma unmanaged

// copied from main.c and beautified
static FILE *php_fopen_wrapper_for_zend(const char *filename, char **opened_path)
{
	TSRMLS_FETCH();
	return php_stream_open_wrapper_as_file((char *)filename, "rb", 
		ENFORCE_SAFE_MODE | USE_PATH | IGNORE_URL_WIN | REPORT_ERRORS | STREAM_OPEN_FOR_INCLUDE, opened_path);
}

// copied from main.c and beautified
static zend_bool php_open_wrapper_for_zend(const char *filename, struct _zend_file_handle *fh)
{
	TSRMLS_FETCH();
	return php_stream_open_wrapper_as_file_handle((char *)filename, "rb",
		ENFORCE_SAFE_MODE | USE_PATH | IGNORE_URL_WIN | REPORT_ERRORS | STREAM_OPEN_FOR_INCLUDE, fh);
}

ZEND_API FILE *(*zend_fopen)(const char *filename, char **opened_path) = php_fopen_wrapper_for_zend;
ZEND_API zend_bool (*zend_open)(const char *filename, zend_file_handle *) = php_open_wrapper_for_zend;

// copied from zend_language_scanner.c and beautified
ZEND_API void zend_file_handle_dtor(zend_file_handle *fh)
{
	switch (fh->type) {
		case ZEND_HANDLE_SOCKET_FD:
#ifdef ZEND_WIN32
			closesocket(fh->handle.fd);
			break;
#endif
		/* fall-through */ 
		case ZEND_HANDLE_FD:
			crtx_close(fh->handle.fd);
			break;
		case ZEND_HANDLE_FP:
			crtx_fclose(fh->handle.fp);
			break;
		case ZEND_HANDLE_FILENAME:
			/* We're only supposed to get here when destructing the used_files hash,
			 * which doesn't really contain open files, but references to their names/paths
			 */
			break;
	}
	if (fh->opened_path) {
		efree(fh->opened_path);
		fh->opened_path = NULL;
	}
	if (fh->free_filename && fh->filename) {
		efree(fh->filename);
		fh->filename = NULL;
	}
}
