//
// ExtSupport.PHP4 - substitute for php4ts.dll
//
// Errors.cpp 
// - contains definitions of error handling related functions
//

#include "stdafx.h"
#include "ExtSupport.h"
#include "Errors.h"
#include "Request.h"
#include "Spprintf.h"
#include "Memory.h"
#include "Parameters.h"
#include "Misc.h"
#include "RemoteDispatcher.h"

#include <stdio.h>
#include <stdarg.h>
#include <string.h>

using namespace System;

using namespace PHP::Core;
using namespace PHP::ExtManager;


// functions with ellipsis (...) cannot be compiled to IL managed
static void managed_zend_error(int type, const char *format, va_list arg)
{
	char buffer[ZEND_ERROR_BUFFER_SIZE];

	_vsnprintf(buffer, ZEND_ERROR_BUFFER_SIZE, format, arg);
	buffer[ZEND_ERROR_BUFFER_SIZE - 1] = 0;

	RemoteDispatcher::ThrowException(static_cast<PhpError>(type), gcnew String(buffer));
}

// functions with ellipsis (...) cannot be compiled to IL managed
static void managed_zend_output_debug_string(zend_bool trigger_break, char *format, va_list arg)
{
	char buffer[ZEND_ERROR_BUFFER_SIZE];

	_vsnprintf(buffer, ZEND_ERROR_BUFFER_SIZE, format, arg);
	buffer[ZEND_ERROR_BUFFER_SIZE - 1] = 0;

#ifdef DEBUG
	Debug::WriteLine("PHP4TS", String::Concat("zend_output_debug_string: ", gcnew String(buffer)));
#endif
	if (trigger_break) System::Diagnostics::Debugger::Break();
}

#pragma unmanaged

// originally in zend.c, rewritten
ZEND_API void zend_error(int type, const char *format, ...)
{
	va_list marker;

	va_start(marker, format);
	managed_zend_error(type, format, marker);
	va_end(marker);
}

#pragma managed

// copied from main.c, modified and beautified
static void php_error_cb(int type, const char *error_filename, const uint error_lineno, const char *format, va_list args)
{
	char *buffer;
	int buffer_len;

	TSRMLS_FETCH();

	buffer_len = vspprintf(&buffer, 1024, format, args);
	RemoteDispatcher::ThrowException(static_cast<PhpError>(type), gcnew String(buffer));
	efree(buffer);
}

ZEND_API void (*zend_error_cb)(int type, const char *error_filename, const uint error_lineno, const char *format, va_list args);

// copied from main.c, modified and beautified
ZEND_API void php_verror(const char *docref, const char *params, int type, const char *format, va_list args TSRMLS_DC)
{
	char *buffer = NULL;
	int buffer_len = 0;
	
	if (params && *params)
	{
		buffer_len = vspprintf(&buffer, 0, format, args);
		if (buffer)	php_error(type, "(%s): %s", params, buffer);
		else php_error(E_ERROR, "%s(%s): Out of memory", get_active_function_name(TSRMLS_C), params);
	}
	else managed_zend_error(type, format, args);
}

#pragma unmanaged

// copied from main.c
ZEND_API void php_error_docref0(const char *docref TSRMLS_DC, int type, const char *format, ...)
{
	va_list args;

	va_start(args, format);
	php_verror(docref, "", type, format, args TSRMLS_CC);
	va_end(args);
}

// copied from main.c
ZEND_API void php_error_docref1(const char *docref TSRMLS_DC, const char *param1, int type, const char *format, ...)
{
	va_list args;
	
	va_start(args, format);
	php_verror(docref, param1, type, format, args TSRMLS_CC);
	va_end(args);
}

// copied from main.c
ZEND_API void php_error_docref2(const char *docref TSRMLS_DC, const char *param1, const char *param2, int type, const char *format, ...)
{
	char *params;
	va_list args;
	
	spprintf(&params, 0, "%s,%s", param1, param2);
	va_start(args, format);
	php_verror(docref, params ? params : "...", type, format, args TSRMLS_CC);
	va_end(args);
	if (params) efree(params);
}

// copied from main.c
#define PHP_WIN32_ERROR_MSG_BUFFER_SIZE 512
ZEND_API void php_win32_docref2_from_error(DWORD error, const char *param1, const char *param2 TSRMLS_DC) {
	if (error == 0) {
		php_error_docref2(NULL TSRMLS_CC, param1, param2, E_WARNING, "%s", strerror(errno));
	} else {
		char buf[PHP_WIN32_ERROR_MSG_BUFFER_SIZE + 1];
		int buf_len;

		FormatMessageA(FORMAT_MESSAGE_FROM_SYSTEM, NULL, error, 0, buf, PHP_WIN32_ERROR_MSG_BUFFER_SIZE, NULL);
		buf_len = strlen(buf);
		if (buf_len >= 2) {
			buf[buf_len - 1] = '\0';
			buf[buf_len - 2] = '\0';
		}
		php_error_docref2(NULL TSRMLS_CC, param1, param2, E_WARNING, "%s (code: %lu)", (char *)buf, error);
	}
}
#undef PHP_WIN32_ERROR_MSG_BUFFER_SIZE
// copied from zend_API.c
ZEND_API void zend_wrong_param_count(TSRMLS_D)
{
	zend_error(E_WARNING, "Wrong parameter count for %s()", get_active_function_name(TSRMLS_C));
}

// originally in zend.c, rewritten
ZEND_API void zend_output_debug_string(zend_bool trigger_break, char *format, ...)
{
#ifdef _DEBUG
	va_list args;

	va_start(args, format);
	managed_zend_output_debug_string(trigger_break, format, args);
	va_end(args);
#endif
}

// copied from zend.c, slightly modified and beautified
ZEND_API void _zend_bailout(char *filename, uint lineno)
{
	TSRMLS_FETCH();

	if (!EG(bailout))
	{
		zend_output_debug_string(1, "%s(%d) : Bailed out without a bailout address!", filename, lineno);
		//exit(-1);
		//throw gcnew ExtensionException("_zend_bailout was called by an extension.");
	}
//	CG(unclean_shutdown) = 1;
	/*CG(in_compilation) =*/ EG(in_execution) = 0;
	EG(current_execute_data) = NULL;
#ifdef PHP4TS
	longjmp(EG(bailout), FAILURE);
#elif defined(PHP5TS)
	longjmp(*EG(bailout), FAILURE);
#endif
}

// copied from basic_functions.c, slightly modified and beautified
ZEND_API int _php_error_log(int opt_err, char *message, char *opt, char *headers TSRMLS_DC)
{
	php_stream *stream = NULL;

	switch (opt_err)
	{
		case 1:		/*send an email */
			{
				if (!php_mail(opt, "PHP error_log message", message, headers, NULL TSRMLS_CC))
				{
					return FAILURE;
				}
			}
			break;

		case 2:		/*send to an address */
			php_error_docref(NULL TSRMLS_CC, E_WARNING, "TCP/IP option not available!");
			return FAILURE;
			break;

		case 3:		/*save to a file */
			stream = php_stream_open_wrapper(opt, "a", IGNORE_URL | ENFORCE_SAFE_MODE | REPORT_ERRORS, NULL);
			if (!stream) return FAILURE;
			php_stream_write(stream, message, strlen(message));
			php_stream_close(stream);
			break;

		default:
			php_log_err(message TSRMLS_CC);
			break;
	}
	return SUCCESS;
}

// copied from winutil.c and beautified
ZEND_API char *php_win_err(int error)
{
	char *buf;

	FormatMessage(FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_FROM_SYSTEM |	FORMAT_MESSAGE_IGNORE_INSERTS,
		NULL, error, MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT),	(LPTSTR)&buf, 0, NULL);

	return buf ? buf : "";
}

#pragma managed

// rewritten, originally in main.c
ZEND_API void php_log_err(char *log_message TSRMLS_DC)
{
	System::Diagnostics::EventLog::WriteEntry("Phalanger Extension Manager", gcnew String(log_message),
		System::Diagnostics::EventLogEntryType::Information);
}
