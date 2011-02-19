//
// ExtSupport.PHP5 - substitute for php5ts.dll
//
// info.cpp
// - this is modified info.c, originally PHP 5.3.3 source files
// - functions call back to Phalanger core to output the information
//


/* 
   +----------------------------------------------------------------------+
   | PHP Version 5                                                        |
   +----------------------------------------------------------------------+
   | Copyright (c) 1997-2010 The PHP Group                                |
   +----------------------------------------------------------------------+
   | This source file is subject to version 3.01 of the PHP license,      |
   | that is bundled with this package in the file LICENSE, and is        |
   | available through the world-wide-web at the following url:           |
   | http://www.php.net/license/3_01.txt                                  |
   | If you did not receive a copy of the PHP license and are unable to   |
   | obtain it through the world-wide-web, please send a note to          |
   | license@php.net so we can mail you a copy immediately.               |
   +----------------------------------------------------------------------+
   | Authors: Rasmus Lerdorf <rasmus@php.net>                             |
   |          Zeev Suraski <zeev@zend.com>                                |
   |          Colin Viebrock <colin@easydns.com>                          |
   +----------------------------------------------------------------------+
*/

/* $Id: info.c 299960 2010-05-30 07:46:45Z pajoye $ */

#include "../../main/php.h"
#include "../../main/php_ini.h"
#include "../../main/php_globals.h"
#include "head.h"
#include "html.h"
#include "info.h"
#include "credits.h"
#include "css.h"
#include "SAPI.h"
#include <time.h>
#include "../../main/php_main.h"
#include "../../zend/zend_globals.h"		/* needs ELS */
#include "../../zend/zend_extensions.h"
#include "../../zend/zend_highlight.h"
#ifdef HAVE_SYS_UTSNAME_H
#include <sys/utsname.h>
#endif

#ifdef PHP_WIN32
typedef void (WINAPI *PGNSI)(LPSYSTEM_INFO);
typedef BOOL (WINAPI *PGPI)(DWORD, DWORD, DWORD, DWORD, PDWORD);

# include "winver.h"

#if _MSC_VER < 1300
# define OSVERSIONINFOEX php_win_OSVERSIONINFOEX
#endif

#endif

#if HAVE_MBSTRING
#include "ext/mbstring/mbstring.h"
ZEND_EXTERN_MODULE_GLOBALS(mbstring)
#endif

#if HAVE_ICONV
#include "ext/iconv/php_iconv.h"
ZEND_EXTERN_MODULE_GLOBALS(iconv)
#endif

#define SECTION(name)	if (!sapi_module.phpinfo_as_text) { \
							PUTS("<h2>" name "</h2>\n"); \
						} else { \
							php_info_print_table_start(); \
							php_info_print_table_header(1, name); \
							php_info_print_table_end(); \
						} \

PHPAPI extern char *php_ini_opened_path;
PHPAPI extern char *php_ini_scanned_path;
PHPAPI extern char *php_ini_scanned_files;
	
static int php_info_write_wrapper(const char *str, uint str_length)
{
	int new_len, written;
	char *elem_esc;

	TSRMLS_FETCH();

	elem_esc = php_escape_html_entities((unsigned char *)str, str_length, &new_len, 0, ENT_QUOTES, NULL TSRMLS_CC);

	written = php_body_write(elem_esc, new_len TSRMLS_CC);

	efree(elem_esc);

	return written;
}


PHPAPI void php_info_print_module(zend_module_entry *zend_module TSRMLS_DC) /* {{{ */
{
	if (zend_module->info_func || zend_module->version) {
		if (!sapi_module.phpinfo_as_text) {
			php_printf("<h2><a name=\"module_%s\">%s</a></h2>\n", zend_module->name, zend_module->name);
		} else {
			php_info_print_table_start();
			php_info_print_table_header(1, zend_module->name);
			php_info_print_table_end();
		}
		if (zend_module->info_func) {
			zend_module->info_func(zend_module TSRMLS_CC);
		} else {
			php_info_print_table_start();
			php_info_print_table_row(2, "Version", zend_module->version);
			php_info_print_table_end();
			DISPLAY_INI_ENTRIES();
		}
	} else {
		if (!sapi_module.phpinfo_as_text) {
			php_printf("<tr><td>%s</td></tr>\n", zend_module->name);
		} else {
			php_printf("%s\n", zend_module->name);
		}	
	}
}
/* }}} */

static int _display_module_info_func(zend_module_entry *module TSRMLS_DC) /* {{{ */
{
	if (module->info_func || module->version) {
		php_info_print_module(module TSRMLS_CC);
	}
	return ZEND_HASH_APPLY_KEEP;
}
/* }}} */

static int _display_module_info_def(zend_module_entry *module TSRMLS_DC) /* {{{ */
{
	if (!module->info_func && !module->version) {
		php_info_print_module(module TSRMLS_CC);
	}
	return ZEND_HASH_APPLY_KEEP;
}
/* }}} */

/* {{{ php_print_gpcse_array
 */
static void php_print_gpcse_array(char *name, uint name_length TSRMLS_DC)
{
	zval **data, **tmp, tmp2;
	char *string_key;
	uint string_len;
	ulong num_key;

	zend_is_auto_global(name, name_length TSRMLS_CC);

	if (zend_hash_find(&EG(symbol_table), name, name_length+1, (void **) &data)!=FAILURE
		&& (Z_TYPE_PP(data)==IS_ARRAY)) {
		zend_hash_internal_pointer_reset(Z_ARRVAL_PP(data));
		while (zend_hash_get_current_data(Z_ARRVAL_PP(data), (void **) &tmp) == SUCCESS) {
			if (!sapi_module.phpinfo_as_text) {
				PUTS("<tr>");
				PUTS("<td class=\"e\">");

			}

			PUTS(name);
			PUTS("[\"");
			
			switch (zend_hash_get_current_key_ex(Z_ARRVAL_PP(data), &string_key, &string_len, &num_key, 0, NULL)) {
				case HASH_KEY_IS_STRING:
					if (!sapi_module.phpinfo_as_text) {
						php_info_html_esc_write(string_key, string_len - 1 TSRMLS_CC);
					} else {
						PHPWRITE(string_key, string_len - 1);
					}	
					break;
				case HASH_KEY_IS_LONG:
					php_printf("%ld", num_key);
					break;
			}
			PUTS("\"]");
			if (!sapi_module.phpinfo_as_text) {
				PUTS("</td><td class=\"v\">");
			} else {
				PUTS(" => ");
			}
			if (Z_TYPE_PP(tmp) == IS_ARRAY) {
				if (!sapi_module.phpinfo_as_text) {
					PUTS("<pre>");
					zend_print_zval_r_ex((zend_write_func_t) php_info_write_wrapper, *tmp, 0 TSRMLS_CC);
					PUTS("</pre>");
				} else {
					zend_print_zval_r(*tmp, 0 TSRMLS_CC);
				}
			} else if (Z_TYPE_PP(tmp) != IS_STRING) {
				tmp2 = **tmp;
				zval_copy_ctor(&tmp2);
				convert_to_string(&tmp2);
				if (!sapi_module.phpinfo_as_text) {
					if (Z_STRLEN(tmp2) == 0) {
						PUTS("<i>no value</i>");
					} else {
						php_info_html_esc_write(Z_STRVAL(tmp2), Z_STRLEN(tmp2) TSRMLS_CC);
					} 
				} else {
					PHPWRITE(Z_STRVAL(tmp2), Z_STRLEN(tmp2));
				}	
				zval_dtor(&tmp2);
			} else {
				if (!sapi_module.phpinfo_as_text) {
					if (Z_STRLEN_PP(tmp) == 0) {
						PUTS("<i>no value</i>");
					} else {
						php_info_html_esc_write(Z_STRVAL_PP(tmp), Z_STRLEN_PP(tmp) TSRMLS_CC);
					}
				} else {
					PHPWRITE(Z_STRVAL_PP(tmp), Z_STRLEN_PP(tmp));
				}	
			}
			if (!sapi_module.phpinfo_as_text) {
				PUTS("</td></tr>\n");
			} else {
				PUTS("\n");
			}	
			zend_hash_move_forward(Z_ARRVAL_PP(data));
		}
	}
}
/* }}} */

/* {{{ php_info_print_style
 */
void php_info_print_style(TSRMLS_D)
{
	php_printf("<style type=\"text/css\">\n");
	php_info_print_css(TSRMLS_C);
	php_printf("</style>\n");
}
/* }}} */

/* {{{ php_info_html_esc_write
 */
PHPAPI void php_info_html_esc_write(char *string, int str_len TSRMLS_DC)
{
	int new_len;
	char *ret = php_escape_html_entities((unsigned char *)string, str_len, &new_len, 0, ENT_QUOTES, NULL TSRMLS_CC);

	PHPWRITE(ret, new_len);
	efree(ret);
}
/* }}} */

/* {{{ php_info_html_esc
 */
PHPAPI char *php_info_html_esc(char *string TSRMLS_DC)
{
	int new_len;
	return php_escape_html_entities((unsigned char *)string, strlen(string), &new_len, 0, ENT_QUOTES, NULL TSRMLS_CC);
}
/* }}} */


#ifdef PHP_WIN32
/* {{{  */
//modified
char* php_get_windows_name()
{
	OSVERSIONINFOEX osvi;
	SYSTEM_INFO si;
	PGNSI pGNSI;
	PGPI pGPI;
	BOOL bOsVersionInfoEx;
	DWORD dwType;
	char *major = NULL, *sub = NULL, *retval;

	ZeroMemory(&si, sizeof(SYSTEM_INFO));
	ZeroMemory(&osvi, sizeof(OSVERSIONINFOEX));
	osvi.dwOSVersionInfoSize = sizeof(OSVERSIONINFOEX);

	if (!(bOsVersionInfoEx = GetVersionEx ((OSVERSIONINFO *) &osvi))) {
		return NULL;
	}

	pGNSI = (PGNSI) GetProcAddress(GetModuleHandle(L"kernel32.dll"), "GetNativeSystemInfo");
	if(NULL != pGNSI) {
		pGNSI(&si);
	} else {
		GetSystemInfo(&si);
	}

	if (VER_PLATFORM_WIN32_NT==osvi.dwPlatformId && osvi.dwMajorVersion > 4 ) {
		if (osvi.dwMajorVersion == 6) {
			if( osvi.dwMinorVersion == 0 ) {
				if( osvi.wProductType == VER_NT_WORKSTATION ) {
					major = "Windows Vista";
				} else {
					major = "Windows Server 2008";
				}
			} else
			if ( osvi.dwMinorVersion == 2 ) {
				if( osvi.wProductType == VER_NT_WORKSTATION )  {
					major = "Windows 7";
				} else {
					major = "Windows Server 2008 R2";
				}
			} else {
				major = "Unknow Windows version";
			}

			pGPI = (PGPI) GetProcAddress(GetModuleHandle(L"kernel32.dll"), "GetProductInfo");
			pGPI(6, 0, 0, 0, &dwType);

			switch (dwType) {
				case PRODUCT_ULTIMATE:
					sub = "Ultimate Edition";
					break;
				case PRODUCT_HOME_PREMIUM:
					sub = "Home Premium Edition";
					break;
				case PRODUCT_HOME_BASIC:
					sub = "Home Basic Edition";
					break;
				case PRODUCT_ENTERPRISE:
					sub = "Enterprise Edition";
					break;
				case PRODUCT_BUSINESS:
					sub = "Business Edition";
					break;
				case PRODUCT_STARTER:
					sub = "Starter Edition";
					break;
				case PRODUCT_CLUSTER_SERVER:
					sub = "Cluster Server Edition";
					break;
				case PRODUCT_DATACENTER_SERVER:
					sub = "Datacenter Edition";
					break;
				case PRODUCT_DATACENTER_SERVER_CORE:
					sub = "Datacenter Edition (core installation)";
					break;
				case PRODUCT_ENTERPRISE_SERVER:
					sub = "Enterprise Edition";
					break;
				case PRODUCT_ENTERPRISE_SERVER_CORE:
					sub = "Enterprise Edition (core installation)";
					break;
				case PRODUCT_ENTERPRISE_SERVER_IA64:
					sub = "Enterprise Edition for Itanium-based Systems";
					break;
				case PRODUCT_SMALLBUSINESS_SERVER:
					sub = "Small Business Server";
					break;
				case PRODUCT_SMALLBUSINESS_SERVER_PREMIUM:
					sub = "Small Business Server Premium Edition";
					break;
				case PRODUCT_STANDARD_SERVER:
					sub = "Standard Edition";
					break;
				case PRODUCT_STANDARD_SERVER_CORE:
					sub = "Standard Edition (core installation)";
					break;
				case PRODUCT_WEB_SERVER:
					sub = "Web Server Edition";
					break;
			}
		}

		if ( osvi.dwMajorVersion == 5 && osvi.dwMinorVersion == 2 )	{
			if (GetSystemMetrics(SM_SERVERR2))
				major = "Windows Server 2003 R2";
			else if (osvi.wSuiteMask == VER_SUITE_STORAGE_SERVER)
				major = "Windows Storage Server 2003";
			else if (osvi.wSuiteMask == VER_SUITE_WH_SERVER)
				major = "Windows Home Server";
			else if (osvi.wProductType == VER_NT_WORKSTATION &&
				si.wProcessorArchitecture==PROCESSOR_ARCHITECTURE_AMD64) {
				major = "Windows XP Professional x64 Edition";
			} else {
				major = "Windows Server 2003";
			}

			/* Test for the server type. */
			if ( osvi.wProductType != VER_NT_WORKSTATION ) {
				if ( si.wProcessorArchitecture==PROCESSOR_ARCHITECTURE_IA64 ) {
					if( osvi.wSuiteMask & VER_SUITE_DATACENTER )
						sub = "Datacenter Edition for Itanium-based Systems";
					else if( osvi.wSuiteMask & VER_SUITE_ENTERPRISE )
						sub = "Enterprise Edition for Itanium-based Systems";
				}

				else if ( si.wProcessorArchitecture==PROCESSOR_ARCHITECTURE_AMD64 ) {
					if( osvi.wSuiteMask & VER_SUITE_DATACENTER )
						sub = "Datacenter x64 Edition";
					else if( osvi.wSuiteMask & VER_SUITE_ENTERPRISE )
						sub = "Enterprise x64 Edition";
					else sub = "Standard x64 Edition";
				} else {
					if ( osvi.wSuiteMask & VER_SUITE_COMPUTE_SERVER )
						sub = "Compute Cluster Edition";
					else if( osvi.wSuiteMask & VER_SUITE_DATACENTER )
						sub = "Datacenter Edition";
					else if( osvi.wSuiteMask & VER_SUITE_ENTERPRISE )
						sub = "Enterprise Edition";
					else if ( osvi.wSuiteMask & VER_SUITE_BLADE )
						sub = "Web Edition";
					else sub = "Standard Edition";
				}
			} 
		}

		if ( osvi.dwMajorVersion == 5 && osvi.dwMinorVersion == 1 )	{
			major = "Windows XP";
			if( osvi.wSuiteMask & VER_SUITE_PERSONAL )
				sub = "Home Edition";
			else sub = "Professional";
		}

		if ( osvi.dwMajorVersion == 5 && osvi.dwMinorVersion == 0 ) {
			major = "Windows 2000";

			if (osvi.wProductType == VER_NT_WORKSTATION ) {
				sub = "Professional";
			} else {
				if( osvi.wSuiteMask & VER_SUITE_DATACENTER )
					sub = "Datacenter Server";
				else if( osvi.wSuiteMask & VER_SUITE_ENTERPRISE )
					sub = "Advanced Server";
				else sub = "Server";
			}
		}
	} else {
		return NULL;
	}

	spprintf(&retval, 0, "%s%s%s%s%s", major, sub?" ":"", sub?sub:"", osvi.szCSDVersion[0] != '\0'?" ":"", osvi.szCSDVersion);
	return retval;
}
/* }}}  */

/* {{{  */
void php_get_windows_cpu(char *buf, int bufsize)
{
	SYSTEM_INFO SysInfo;
	GetSystemInfo(&SysInfo);
	switch (SysInfo.wProcessorArchitecture) {
		case PROCESSOR_ARCHITECTURE_INTEL :
			snprintf(buf, bufsize, "i%d", SysInfo.dwProcessorType);
			break;
		case PROCESSOR_ARCHITECTURE_MIPS :
			snprintf(buf, bufsize, "MIPS R%d000", SysInfo.wProcessorLevel);
			break;
		case PROCESSOR_ARCHITECTURE_ALPHA :
			snprintf(buf, bufsize, "Alpha %d", SysInfo.wProcessorLevel);
			break;
		case PROCESSOR_ARCHITECTURE_PPC :
			snprintf(buf, bufsize, "PPC 6%02d", SysInfo.wProcessorLevel);
			break;
		case PROCESSOR_ARCHITECTURE_IA64 :
			snprintf(buf, bufsize,  "IA64");
			break;
#if defined(PROCESSOR_ARCHITECTURE_IA32_ON_WIN64)
		case PROCESSOR_ARCHITECTURE_IA32_ON_WIN64 :
			snprintf(buf, bufsize, "IA32");
			break;
#endif
#if defined(PROCESSOR_ARCHITECTURE_AMD64)
		case PROCESSOR_ARCHITECTURE_AMD64 :
			snprintf(buf, bufsize, "AMD64");
			break;
#endif
		case PROCESSOR_ARCHITECTURE_UNKNOWN :
		default:
			snprintf(buf, bufsize, "Unknown");
			break;
	}
}
/* }}}  */
#endif

//modified
/* {{{ php_get_uname
 */
PHPAPI char *php_get_uname(char mode)
{
	char *php_uname;
	char tmp_uname[256];
#ifdef PHP_WIN32
	DWORD dwBuild=0;
	DWORD dwVersion = GetVersion();
	DWORD dwWindowsMajorVersion =  (DWORD)(LOBYTE(LOWORD(dwVersion)));
	DWORD dwWindowsMinorVersion =  (DWORD)(HIBYTE(LOWORD(dwVersion)));
	DWORD dwSize = MAX_COMPUTERNAME_LENGTH + 1;
	char ComputerName[MAX_COMPUTERNAME_LENGTH + 1];
	
	GetComputerName((LPWSTR)ComputerName, &dwSize);

	if (mode == 's') {
		if (dwVersion < 0x80000000) {
			php_uname = "Windows NT";
		} else {
			php_uname = "Windows 9x";
		}
	} else if (mode == 'r') {
		snprintf(tmp_uname, sizeof(tmp_uname), "%d.%d", dwWindowsMajorVersion, dwWindowsMinorVersion);
		php_uname = tmp_uname;
	} else if (mode == 'n') {
		php_uname = ComputerName;
	} else if (mode == 'v') {
		char *winver = php_get_windows_name();
		dwBuild = (DWORD)(HIWORD(dwVersion));
		if(winver == NULL) {
			snprintf(tmp_uname, sizeof(tmp_uname), "build %d", dwBuild);
		} else {
			snprintf(tmp_uname, sizeof(tmp_uname), "build %d (%s)", dwBuild, winver);
		}
		php_uname = tmp_uname;
		if(winver) {
			efree(winver);
		}
	} else if (mode == 'm') {
		php_get_windows_cpu(tmp_uname, sizeof(tmp_uname));
		php_uname = tmp_uname;
	} else { /* assume mode == 'a' */
		/* Get build numbers for Windows NT or Win95 */
		if (dwVersion < 0x80000000){
			char *winver = php_get_windows_name();
			char wincpu[20];

			php_get_windows_cpu(wincpu, sizeof(wincpu));
			dwBuild = (DWORD)(HIWORD(dwVersion));
			snprintf(tmp_uname, sizeof(tmp_uname), "%s %s %d.%d build %d (%s) %s",
					 "Windows NT", ComputerName,
					 dwWindowsMajorVersion, dwWindowsMinorVersion, dwBuild, winver?winver:"unknown", wincpu);
			if(winver) {
				efree(winver);
			}
		} else {
			snprintf(tmp_uname, sizeof(tmp_uname), "%s %s %d.%d",
					 "Windows 9x", ComputerName,
					 dwWindowsMajorVersion, dwWindowsMinorVersion);
		}
		php_uname = tmp_uname;
	}
#else
#ifdef HAVE_SYS_UTSNAME_H
	struct utsname buf;
	if (uname((struct utsname *)&buf) == -1) {
		php_uname = PHP_UNAME;
	} else {
#ifdef NETWARE
		if (mode == 's') {
			php_uname = buf.sysname;
		} else if (mode == 'r') {
			snprintf(tmp_uname, sizeof(tmp_uname), "%d.%d.%d", 
					 buf.netware_major, buf.netware_minor, buf.netware_revision);
			php_uname = tmp_uname;
		} else if (mode == 'n') {
			php_uname = buf.servername;
		} else if (mode == 'v') {
			snprintf(tmp_uname, sizeof(tmp_uname), "libc-%d.%d.%d #%d",
					 buf.libmajor, buf.libminor, buf.librevision, buf.libthreshold);
			php_uname = tmp_uname;
		} else if (mode == 'm') {
			php_uname = buf.machine;
		} else { /* assume mode == 'a' */
			snprintf(tmp_uname, sizeof(tmp_uname), "%s %s %d.%d.%d libc-%d.%d.%d #%d %s",
					 buf.sysname, buf.servername,
					 buf.netware_major, buf.netware_minor, buf.netware_revision,
					 buf.libmajor, buf.libminor, buf.librevision, buf.libthreshold,
					 buf.machine);
			php_uname = tmp_uname;
		}
#else
		if (mode == 's') {
			php_uname = buf.sysname;
		} else if (mode == 'r') {
			php_uname = buf.release;
		} else if (mode == 'n') {
			php_uname = buf.nodename;
		} else if (mode == 'v') {
			php_uname = buf.version;
		} else if (mode == 'm') {
			php_uname = buf.machine;
		} else { /* assume mode == 'a' */
			snprintf(tmp_uname, sizeof(tmp_uname), "%s %s %s %s %s",
					 buf.sysname, buf.nodename, buf.release, buf.version,
					 buf.machine);
			php_uname = tmp_uname;
		}
#endif /* NETWARE */
	}
#else
	php_uname = PHP_UNAME;
#endif
#endif
	return estrdup(php_uname);
}
/* }}} */


/* {{{ php_print_info_htmlhead
 */
PHPAPI void php_print_info_htmlhead(TSRMLS_D)
{

	PUTS("<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Transitional//EN\" \"DTD/xhtml1-transitional.dtd\">\n");
	PUTS("<html>");
	PUTS("<head>\n");
	php_info_print_style(TSRMLS_C);
	PUTS("<title>phpinfo()</title>");
	PUTS("<meta name=\"ROBOTS\" content=\"NOINDEX,NOFOLLOW,NOARCHIVE\" />");
/*
	php_printf("<meta http-equiv=\"Content-Type\" content=\"text/html; charset=%s\" />\n", charset);
*/
	PUTS("</head>\n");
	PUTS("<body><div class=\"center\">\n");
}
/* }}} */

#pragma managed 

#include "../../../ExtSupport.h"
#include "../../../Request.h"

using namespace System;
using namespace System::Text;
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
	Debug::WriteLine("PHP5TS", "Calling back Core::PhpNetInfo::PrintTableHeader()");
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
	Debug::WriteLine("PHP5TS", "Calling back Core::PhpNetInfo::PrintTableRow()");
#endif

	StringBuilder ^builder = Request::GetCurrentRequest()->PhpInfoBuilder;
	if (builder != nullptr) builder->Append(PhpNetInfo::PrintTableRow(false, args));
	else PhpNetInfo::PrintTableRow(true, args);
}

/* {{{ php_print_info
 */
// rewritten
PHPAPI void php_print_info(int flag TSRMLS_DC)
{
	PHP::Core::PhpNetInfo::Write(static_cast<PHP::Core::PhpNetInfo::Sections>(flag),
		PHP::Core::ScriptContext::CurrentContext->Output);
}
/* }}} */


PHPAPI void php_info_print_table_start(void)
{
#ifdef DEBUG
	Debug::WriteLine("PHP5TS", "Calling back Core::PhpNetInfo::PrintTableStart()");
#endif

	StringBuilder ^builder = Request::GetCurrentRequest()->PhpInfoBuilder;
	if (builder != nullptr) builder->Append(PhpNetInfo::PrintTableStart(false));
	else PhpNetInfo::PrintTableStart(true);
}

PHPAPI void php_info_print_table_end(void)
{
#ifdef DEBUG
	Debug::WriteLine("PHP5TS", "Calling back Core::PhpNetInfo::PrintTableEnd()");
#endif

	StringBuilder ^builder = Request::GetCurrentRequest()->PhpInfoBuilder;
	if (builder != nullptr) builder->Append(PhpNetInfo::PrintTableEnd(false));
	else PhpNetInfo::PrintTableEnd(true);
}

PHPAPI void php_info_print_box_start(int bg)
{
#ifdef DEBUG
	Debug::WriteLine("PHP5TS", "Calling back Core::PhpNetInfo::PrintBoxStart()");
#endif

	StringBuilder ^builder = Request::GetCurrentRequest()->PhpInfoBuilder;
	if (builder != nullptr) builder->Append(PhpNetInfo::PrintBoxStart(false, bg));
	else PhpNetInfo::PrintBoxStart(true, bg);
}

PHPAPI void php_info_print_box_end(void)
{
#ifdef DEBUG
	Debug::WriteLine("PHP5TS", "Calling back Core::PhpNetInfo::PrintBoxEnd()");
#endif

	StringBuilder ^builder = Request::GetCurrentRequest()->PhpInfoBuilder;
	if (builder != nullptr) builder->Append(PhpNetInfo::PrintBoxEnd(false));
	else PhpNetInfo::PrintBoxEnd(true);
}

PHPAPI void php_info_print_hr(void)
{
#ifdef DEBUG
	Debug::WriteLine("PHP5TS", "Calling back Core::PhpNetInfo::PrintHr()");
#endif

	StringBuilder ^builder = Request::GetCurrentRequest()->PhpInfoBuilder;
	if (builder != nullptr) builder->Append(PhpNetInfo::PrintHr(false));
	else PhpNetInfo::PrintHr(true);
}


PHPAPI void php_info_print_table_colspan_header(int num_cols, char *header)
{
#ifdef DEBUG
	Debug::WriteLine("PHP5TS", "Calling back Core::PhpNetInfo::PrintTableColspanHeader()");
#endif

	StringBuilder ^builder = Request::GetCurrentRequest()->PhpInfoBuilder;
	if (builder != nullptr) builder->Append(PhpNetInfo::PrintTableColspanHeader(false, num_cols, 
		gcnew String(header)));
	else PhpNetInfo::PrintTableColspanHeader(true, num_cols, gcnew String(header));
}

/* {{{ php_info_print_table_header
 */
PHPAPI void php_info_print_table_header(int num_cols, ...)
{
	va_list marker;
	va_start(marker, num_cols);

	managed_print_header(num_cols, (char **)marker);
}
/* }}} */

PHPAPI void php_info_print_table_row(int num_cols, ...)
{
	va_list marker;
	va_start(marker, num_cols);
	managed_print_row(num_cols, (char **)marker);
}
/* }}} */

/* {{{ php_info_print_table_row_ex
 */
PHPAPI void php_info_print_table_row_ex(int num_cols, const char *value_class, 
		...)//TODO: there is slight difference between this one and php_info_print_table_row
{
	va_list marker;
	va_start(marker, num_cols);
	managed_print_row(num_cols, (char **)marker);
}
/* }}} */
//
///* {{{ register_phpinfo_constants
// */
//void register_phpinfo_constants(INIT_FUNC_ARGS)
//{
//	REGISTER_LONG_CONSTANT("INFO_GENERAL", PHP_INFO_GENERAL, CONST_PERSISTENT|CONST_CS);
//	REGISTER_LONG_CONSTANT("INFO_CREDITS", PHP_INFO_CREDITS, CONST_PERSISTENT|CONST_CS);
//	REGISTER_LONG_CONSTANT("INFO_CONFIGURATION", PHP_INFO_CONFIGURATION, CONST_PERSISTENT|CONST_CS);
//	REGISTER_LONG_CONSTANT("INFO_MODULES", PHP_INFO_MODULES, CONST_PERSISTENT|CONST_CS);
//	REGISTER_LONG_CONSTANT("INFO_ENVIRONMENT", PHP_INFO_ENVIRONMENT, CONST_PERSISTENT|CONST_CS);
//	REGISTER_LONG_CONSTANT("INFO_VARIABLES", PHP_INFO_VARIABLES, CONST_PERSISTENT|CONST_CS);
//	REGISTER_LONG_CONSTANT("INFO_LICENSE", PHP_INFO_LICENSE, CONST_PERSISTENT|CONST_CS);
//	REGISTER_LONG_CONSTANT("INFO_ALL", PHP_INFO_ALL, CONST_PERSISTENT|CONST_CS);
//	REGISTER_LONG_CONSTANT("CREDITS_GROUP",	PHP_CREDITS_GROUP, CONST_PERSISTENT|CONST_CS);
//	REGISTER_LONG_CONSTANT("CREDITS_GENERAL",	PHP_CREDITS_GENERAL, CONST_PERSISTENT|CONST_CS);
//	REGISTER_LONG_CONSTANT("CREDITS_SAPI",	PHP_CREDITS_SAPI, CONST_PERSISTENT|CONST_CS);
//	REGISTER_LONG_CONSTANT("CREDITS_MODULES",	PHP_CREDITS_MODULES, CONST_PERSISTENT|CONST_CS);
//	REGISTER_LONG_CONSTANT("CREDITS_DOCS",	PHP_CREDITS_DOCS, CONST_PERSISTENT|CONST_CS);
//	REGISTER_LONG_CONSTANT("CREDITS_FULLPAGE",	PHP_CREDITS_FULLPAGE, CONST_PERSISTENT|CONST_CS);
//	REGISTER_LONG_CONSTANT("CREDITS_QA",	PHP_CREDITS_QA, CONST_PERSISTENT|CONST_CS);
//	REGISTER_LONG_CONSTANT("CREDITS_ALL",	PHP_CREDITS_ALL, CONST_PERSISTENT|CONST_CS);
//}
///* }}} */
//
///* {{{ proto void phpinfo([int what])
//   Output a page of useful information about PHP and the current request */
//PHP_FUNCTION(phpinfo)
//{
//	long flag = PHP_INFO_ALL;
//
//	if (zend_parse_parameters(ZEND_NUM_ARGS() TSRMLS_CC, "|l", &flag) == FAILURE) {
//		return;
//	}
//
//	/* Andale!  Andale!  Yee-Hah! */
//	php_start_ob_buffer(NULL, 4096, 0 TSRMLS_CC);
//	php_print_info(flag TSRMLS_CC);
//	php_end_ob_buffer(1, 0 TSRMLS_CC);
//
//	RETURN_TRUE;
//}
//
///* }}} */
//
///* {{{ proto string phpversion([string extension])
//   Return the current PHP version */
//PHP_FUNCTION(phpversion)
//{
//	zval **arg;
//	const char *version;
//	int argc = ZEND_NUM_ARGS();
//
//	if (argc == 0) {
//		RETURN_STRING(PHP_VERSION, 1);
//	} else {
//		if (zend_parse_parameters(argc TSRMLS_CC, "Z", &arg) == FAILURE) {
//			return;
//		}
//			
//		convert_to_string_ex(arg);
//		version = zend_get_module_version(Z_STRVAL_PP(arg));
//		
//		if (version == NULL) {
//			RETURN_FALSE;
//		}
//		RETURN_STRING(version, 1);
//	}
//}
///* }}} */
//
///* {{{ proto void phpcredits([int flag])
//   Prints the list of people who've contributed to the PHP project */
//PHP_FUNCTION(phpcredits)
//{
//	long flag = PHP_CREDITS_ALL;
//
//	if (zend_parse_parameters(ZEND_NUM_ARGS() TSRMLS_CC, "|l", &flag) == FAILURE) {
//		return;
//	}
//
//	php_print_credits(flag TSRMLS_CC);
//	RETURN_TRUE;
//}
///* }}} */
//
//
///* {{{ php_logo_guid
// */
//PHPAPI char *php_logo_guid(void)
//{
//	char *logo_guid;
//
//	time_t the_time;
//	struct tm *ta, tmbuf;
//
//	the_time = time(NULL);
//	ta = php_localtime_r(&the_time, &tmbuf);
//
//	if (ta && (ta->tm_mon==3) && (ta->tm_mday==1)) {
//		logo_guid = PHP_EGG_LOGO_GUID;
//	} else {
//		logo_guid = PHP_LOGO_GUID;
//	}
//
//	return estrdup(logo_guid);
//
//}
///* }}} */
//
///* {{{ proto string php_logo_guid(void)
//   Return the special ID used to request the PHP logo in phpinfo screens*/
//PHP_FUNCTION(php_logo_guid)
//{
//
//	if (zend_parse_parameters_none() == FAILURE) {
//		return;
//	}
//
//	RETURN_STRING(php_logo_guid(), 0);
//}
///* }}} */
//
///* {{{ proto string php_real_logo_guid(void)
//   Return the special ID used to request the PHP logo in phpinfo screens*/
//PHP_FUNCTION(php_real_logo_guid)
//{
//
//	if (zend_parse_parameters_none() == FAILURE) {
//		return;
//	}
//
//	RETURN_STRINGL(PHP_LOGO_GUID, sizeof(PHP_LOGO_GUID)-1, 1);
//}
///* }}} */
//
///* {{{ proto string php_egg_logo_guid(void)
//   Return the special ID used to request the PHP logo in phpinfo screens*/
//PHP_FUNCTION(php_egg_logo_guid)
//{
//	if (zend_parse_parameters_none() == FAILURE) {
//		return;
//	}
//
//	RETURN_STRINGL(PHP_EGG_LOGO_GUID, sizeof(PHP_EGG_LOGO_GUID)-1, 1);
//}
///* }}} */
//
///* {{{ proto string zend_logo_guid(void)
//   Return the special ID used to request the Zend logo in phpinfo screens*/
//PHP_FUNCTION(zend_logo_guid)
//{
//	if (zend_parse_parameters_none() == FAILURE) {
//		return;
//	}
//
//	RETURN_STRINGL(ZEND_LOGO_GUID, sizeof(ZEND_LOGO_GUID)-1, 1);
//}
///* }}} */
//
///* {{{ proto string php_sapi_name(void)
//   Return the current SAPI module name */
//PHP_FUNCTION(php_sapi_name)
//{
//	if (zend_parse_parameters_none() == FAILURE) {
//		return;
//	}
//
//	if (sapi_module.name) {
//		RETURN_STRING(sapi_module.name, 1);
//	} else {
//		RETURN_FALSE;
//	}
//}
//
///* }}} */
//
///* {{{ proto string php_uname(void)
//   Return information about the system PHP was built on */
//PHP_FUNCTION(php_uname)
//{
//	char *mode = "a";
//	int modelen = sizeof("a")-1;
//	if (zend_parse_parameters(ZEND_NUM_ARGS() TSRMLS_CC, "|s", &mode, &modelen) == FAILURE) {
//		return;
//	}
//	RETURN_STRING(php_get_uname(*mode), 0);
//}
//
///* }}} */
//
///* {{{ proto string php_ini_scanned_files(void)
//   Return comma-separated string of .ini files parsed from the additional ini dir */
//PHP_FUNCTION(php_ini_scanned_files)
//{
//	if (zend_parse_parameters_none() == FAILURE) {
//		return;
//	}
//	
//	if (strlen(PHP_CONFIG_FILE_SCAN_DIR) && php_ini_scanned_files) {
//		RETURN_STRING(php_ini_scanned_files, 1);
//	} else {
//		RETURN_FALSE;
//	}
//}
///* }}} */
//
///* {{{ proto string php_ini_loaded_file(void)
//   Return the actual loaded ini filename */
//PHP_FUNCTION(php_ini_loaded_file)
//{
//	if (zend_parse_parameters_none() == FAILURE) {
//		return;
//	}
//	
//	if (php_ini_opened_path) {
//		RETURN_STRING(php_ini_opened_path, 1);
//	} else {
//		RETURN_FALSE;
//	}
//}
///* }}} */
//
///*
// * Local variables:
// * tab-width: 4
// * c-basic-offset: 4
// * End:
// * vim600: sw=4 ts=4 fdm=marker
// * vim<600: sw=4 ts=4
// */
//
