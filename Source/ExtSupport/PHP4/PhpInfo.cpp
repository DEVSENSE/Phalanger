//
// ExtSupport.PHP4 - substitute for php4ts.dll
//
// PhpInfo.cpp 
// - contains definitions of phpinfo() related functions
// - functions call back to PHP.NET core to output the information
//

#include "stdafx.h"
#include "PhpInfo.h"
#include "ExtSupport.h"
#include "Memory.h"
#include "Request.h"
#include "Output.h"

#include <stdio.h>
#include <stdarg.h>
#include <memory.h>
#include <string.h>

using namespace System;
using namespace System::Web;
using namespace System::Runtime::InteropServices;

using namespace PHP::Core;
using namespace PHP::ExtManager;


// Managed stub for php_info_print_table_header.
static void managed_print_header(int num_cols, char **cols)
{
	array <String ^> ^args = gcnew array<String ^>(num_cols);
	for (int i = 0; i < num_cols; i++) args[i] = gcnew String(cols[i]);

#ifdef DEBUG
	Debug::WriteLine("PHP4TS", "Calling back Core::PhpNetInfo::PrintTableHeader()");
#endif

	StringBuilder ^builder = Request::GetCurrentRequest()->PhpInfoBuilder;
	if (builder != nullptr) builder->Append(PhpNetInfo::PrintTableHeader(false, args));
	else PhpNetInfo::PrintTableHeader(true, args);
}

// Managed stub for php_info_print_table_row.
static void managed_print_row(int num_cols, char **cols)
{
	array<String ^> ^args = gcnew array<String ^>(num_cols);
	for (int i = 0; i < num_cols; i++) args[i] = gcnew String(cols[i]);

#ifdef DEBUG
	Debug::WriteLine("PHP4TS", "Calling back Core::PhpNetInfo::PrintTableRow()");
#endif

	StringBuilder ^builder = Request::GetCurrentRequest()->PhpInfoBuilder;
	if (builder != nullptr) builder->Append(PhpNetInfo::PrintTableRow(false, args));
	else PhpNetInfo::PrintTableRow(true, args);
}

/*****/

ZEND_API char *php_info_html_esc(char *string TSRMLS_DC)
{
	// encode the string
	String ^encoded = HttpUtility::HtmlEncode(
		gcnew String(string, 0, strlen(string), Request::AppConf->Globalization->PageEncoding));
	
	int length = encoded->Length;
	char *ansi = (char *)_emalloc(length + 1);

	// convert to (char *)
	Marshal::Copy(Request::AppConf->Globalization->PageEncoding->GetBytes(encoded), 0, IntPtr(ansi), length);
	Marshal::WriteByte(IntPtr(ansi + length), 0);

	return ansi;
}

ZEND_API void php_info_print_box_end(void)
{
#ifdef DEBUG
	Debug::WriteLine("PHP4TS", "Calling back Core::PhpNetInfo::PrintBoxEnd()");
#endif

	StringBuilder ^builder = Request::GetCurrentRequest()->PhpInfoBuilder;
	if (builder != nullptr) builder->Append(PhpNetInfo::PrintBoxEnd(false));
	else PhpNetInfo::PrintBoxEnd(true);
}

ZEND_API void php_info_print_box_start(int bg)
{
#ifdef DEBUG
	Debug::WriteLine("PHP4TS", "Calling back Core::PhpNetInfo::PrintBoxStart()");
#endif

	StringBuilder ^builder = Request::GetCurrentRequest()->PhpInfoBuilder;
	if (builder != nullptr) builder->Append(PhpNetInfo::PrintBoxStart(false, bg));
	else PhpNetInfo::PrintBoxStart(true, bg);
}

ZEND_API void php_info_print_css(void)
{
#ifdef DEBUG
	Debug::WriteLine("PHP4TS", "Calling back Core::PhpNetInfo::PrintCss()");
#endif

	StringBuilder ^builder = Request::GetCurrentRequest()->PhpInfoBuilder;
	if (builder != nullptr) builder->Append(PhpNetInfo::PrintCss(false));
	else PhpNetInfo::PrintCss(true);
}

ZEND_API void php_info_print_hr(void)
{
#ifdef DEBUG
	Debug::WriteLine("PHP4TS", "Calling back Core::PhpNetInfo::PrintHr()");
#endif

	StringBuilder ^builder = Request::GetCurrentRequest()->PhpInfoBuilder;
	if (builder != nullptr) builder->Append(PhpNetInfo::PrintHr(false));
	else PhpNetInfo::PrintHr(true);
}

ZEND_API void php_info_print_style(void)
{
#ifdef DEBUG
	Debug::WriteLine("PHP4TS", "Calling back Core::PhpNetInfo::PrintStyle()");
#endif

	StringBuilder ^builder = Request::GetCurrentRequest()->PhpInfoBuilder;
	if (builder != nullptr) builder->Append(PhpNetInfo::PrintStyle(false));
	else PhpNetInfo::PrintStyle(true);
}

ZEND_API void php_info_print_table_colspan_header(int num_cols, char *header)
{
#ifdef DEBUG
	Debug::WriteLine("PHP4TS", "Calling back Core::PhpNetInfo::PrintTableColspanHeader()");
#endif

	StringBuilder ^builder = Request::GetCurrentRequest()->PhpInfoBuilder;
	if (builder != nullptr) builder->Append(PhpNetInfo::PrintTableColspanHeader(false, num_cols, 
		gcnew String(header)));
	else PhpNetInfo::PrintTableColspanHeader(true, num_cols, gcnew String(header));
}

ZEND_API void php_info_print_table_end(void)
{
#ifdef DEBUG
	Debug::WriteLine("PHP4TS", "Calling back Core::PhpNetInfo::PrintTableEnd()");
#endif

	StringBuilder ^builder = Request::GetCurrentRequest()->PhpInfoBuilder;
	if (builder != nullptr) builder->Append(PhpNetInfo::PrintTableEnd(false));
	else PhpNetInfo::PrintTableEnd(true);
}

#pragma unmanaged

// Compiling varargs function as managed code is not yet implemented (VS.NET 2002),
// so let's help ourselves with a foxy dodge...
ZEND_API void php_info_print_table_header(int num_cols, ...)
{
	va_list marker;
	va_start(marker, num_cols);
	managed_print_header(num_cols, (char **)marker);
}

// Compiling varargs function as managed code is not yet implemented (VS.NET 2002),
// so let's help ourselves with a foxy dodge...
ZEND_API void php_info_print_table_row(int num_cols, ...)
{
	va_list marker;
	va_start(marker, num_cols);
	managed_print_row(num_cols, (char **)marker);
}

#pragma managed

ZEND_API void php_info_print_table_start(void)
{
#ifdef DEBUG
	Debug::WriteLine("PHP4TS", "Calling back Core::PhpNetInfo::PrintTableStart()");
#endif

	StringBuilder ^builder = Request::GetCurrentRequest()->PhpInfoBuilder;
	if (builder != nullptr) builder->Append(PhpNetInfo::PrintTableStart(false));
	else PhpNetInfo::PrintTableStart(true);
}

// copied from info.c
ZEND_API void php_print_info_htmlhead(TSRMLS_D)
{
	PUTS("<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Transitional//EN\" \"DTD/xhtml1-transitional.dtd\">\n");
	PUTS("<html>");
	PUTS("<head>\n");
	php_info_print_style();
	PUTS("<title>phpinfo()</title>");
	PUTS("</head>\n");
	PUTS("<body><div class=\"center\">\n");
}

// rewritten, originally in info.c
ZEND_API void php_print_info(int flag TSRMLS_DC)
{
	PHP::Core::PhpNetInfo::Write(static_cast<PHP::Core::PhpNetInfo::Sections>(flag),
		PHP::Core::ScriptContext::CurrentContext->Output);
}

// rewritten, originally in info.c
ZEND_API void php_print_credits(int flag)
{
	PHP::Core::PhpNetInfo::Write(PHP::Core::PhpNetInfo::Sections::Credits,
		PHP::Core::ScriptContext::CurrentContext->Output);
}
