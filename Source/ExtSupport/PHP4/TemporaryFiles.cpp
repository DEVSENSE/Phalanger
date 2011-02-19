//
// ExtSupport.PHP4 - substitute for php4ts.dll
//
// TemporaryFiles.cpp 
// - slightly modified php_open_temporary_file.c, originally PHP source file
//

#include "stdafx.h"
#include "TemporaryFiles.h"
#include "Memory.h"
#include "Misc.h"
#include "VirtualWorkingDir.h"

#include <stdio.h>
#include <io.h>

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
   | Author: Zeev Suraski <zeev@zend.com>                                 |
   +----------------------------------------------------------------------+
 */

/* $Id: TemporaryFiles.cpp,v 1.1.2.2 2006/04/15 20:19:12 prosl0am Exp $ */

#include <errno.h>
#include <fcntl.h>

#undef errno
#define errno (*crtx_errno())

#pragma unmanaged

/* {{{ php_do_open_temporary_file */

/* Loosely based on a tempnam() implementation by UCLA */

/*
 * Copyright (c) 1988, 1993
 *      The Regents of the University of California.  All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions
 * are met:
 * 1. Redistributions of source code must retain the above copyright
 *    notice, this list of conditions and the following disclaimer.
 * 2. Redistributions in binary form must reproduce the above copyright
 *    notice, this list of conditions and the following disclaimer in the
 *    documentation and/or other materials provided with the distribution.
 * 3. All advertising materials mentioning features or use of this software
 *    must display the following acknowledgement:
 *      This product includes software developed by the University of
 *      California, Berkeley and its contributors.
 * 4. Neither the name of the University nor the names of its contributors
 *    may be used to endorse or promote products derived from this software
 *    without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE REGENTS AND CONTRIBUTORS ``AS IS'' AND
 * ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED.  IN NO EVENT SHALL THE REGENTS OR CONTRIBUTORS BE LIABLE
 * FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
 * DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS
 * OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION)
 * HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT
 * LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY
 * OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF
 * SUCH DAMAGE.
 */

static int php_do_open_temporary_file(const char *path, const char *pfx, char **opened_path_p TSRMLS_DC)
{
	char *trailing_slash;
	char *opened_path;
	int fd = -1;
#ifndef HAVE_MKSTEMP
	int open_flags = O_CREAT | O_TRUNC | O_RDWR
#ifdef PHP_WIN32
		| _O_BINARY
#endif
		;
#endif
#ifdef NETWARE
    char *file_path = NULL;
#endif

	if (!path) {
		return -1;
	}

	if (!(opened_path = (char *)emalloc(MAXPATHLEN))) {
		return -1;
	}

	if (IS_SLASH(path[strlen(path)-1])) {
		trailing_slash = "";
	} else {
		trailing_slash = "/";
	}

	(void)snprintf(opened_path, MAXPATHLEN, "%s%s%sXXXXXX", path, trailing_slash, pfx);

#ifdef PHP_WIN32
	if (GetTempFileNameA(path, pfx, 0, opened_path)) {
		/* Some versions of windows set the temp file to be read-only,
		 * which means that opening it will fail... */
		VCWD_CHMOD(opened_path, 0600);
		fd = VCWD_OPEN_MODE(opened_path, open_flags, 0600);
	}
#elif defined(NETWARE)
	/* Using standard mktemp() implementation for NetWare */
	file_path = mktemp(opened_path);
	if (file_path) {
		fd = VCWD_OPEN(file_path, open_flags);
	}
#elif defined(HAVE_MKSTEMP)
	fd = mkstemp(opened_path);
#else
	if (mktemp(opened_path)) {
		fd = VCWD_OPEN(opened_path, open_flags);
	}
#endif
	if (fd == -1 || !opened_path_p) {
		efree(opened_path);
	} else {
		*opened_path_p = opened_path;
	}
	return fd;
}
/* }}} */

/*
 *  Determine where to place temporary files.
 */
const char* get_temporary_directory()
{
	/* Cache the chosen temporary directory. */
	static char* temporary_directory;

	/* Did we determine the temporary directory already? */
	if (temporary_directory) {
		return temporary_directory;
	}

#ifdef PHP_WIN32
	/* We can't count on the environment variables TEMP or TMP,
	 * and so must make the Win32 API call to get the default
	 * directory for temporary files.  Note this call checks
	 * the environment values TMP and TEMP (in order) first.
	 */
	{
		char sTemp[MAX_PATH];
		DWORD n = GetTempPathA(sizeof(sTemp),sTemp);
		assert(0 < n);  /* should *never* fail! */
		temporary_directory = _strdup(sTemp);
		return temporary_directory;
	}
#else
	/* On Unix use the (usual) TMPDIR environment variable. */
	{
		char* s = getenv("TMPDIR");
		if (s) {
			temporary_directory = strdup(s);
			return temporary_directory;
		}
	}
#ifdef P_tmpdir
	/* Use the standard default temporary directory. */
	if (P_tmpdir) {
		temporary_directory = P_tmpdir;
		return temporary_directory;
	}
#endif
	/* Shouldn't ever(!) end up here ... last ditch default. */
	temporary_directory = "/tmp";
	return temporary_directory;
#endif
}

/* {{{ php_open_temporary_file
 *
 * Unlike tempnam(), the supplied dir argument takes precedence
 * over the TMPDIR environment variable
 * This function should do its best to return a file pointer to a newly created
 * unique file, on every platform.
 */
ZEND_API int php_open_temporary_fd(const char *dir, const char *pfx, char **opened_path_p TSRMLS_DC)
{
	int fd;

	if (!pfx) {
		pfx = "tmp.";
	}
	if (opened_path_p) {
		*opened_path_p = NULL;
	}

	/* Try the directory given as parameter. */
	fd = php_do_open_temporary_file(dir, pfx, opened_path_p TSRMLS_CC);
	if (fd == -1) {
		/* Use default temporary directory. */
		fd = php_do_open_temporary_file(get_temporary_directory(), pfx, opened_path_p TSRMLS_CC);
	}
	return fd;
}

ZEND_API FILE *php_open_temporary_file(const char *dir, const char *pfx, char **opened_path_p TSRMLS_DC)
{
	FILE *fp;
	int fd = php_open_temporary_fd(dir, pfx, opened_path_p TSRMLS_CC);

	if (fd == -1) {
		return NULL;
	}
	
	fp = crtx_fdopen(fd, "r+b");
	if (fp == NULL) {
		crtx_close(fd);
	}
	
	return fp;
}
/* }}} */

ZEND_API const char* php_get_temporary_directory(void)
{
	/* Cache the chosen temporary directory. */
	static char* temporary_directory;

	/* Did we determine the temporary directory already? */
	if (temporary_directory) {
		return temporary_directory;
	}

#ifdef PHP_WIN32
	/* We can't count on the environment variables TEMP or TMP,
	 * and so must make the Win32 API call to get the default
	 * directory for temporary files.  Note this call checks
	 * the environment values TMP and TEMP (in order) first.
	 */
	{
		char sTemp[MAX_PATH];
		DWORD n = GetTempPathA(sizeof(sTemp),sTemp);
		assert(0 < n);  /* should *never* fail! */
		temporary_directory = _strdup(sTemp);
		return temporary_directory;
	}
#else
	/* On Unix use the (usual) TMPDIR environment variable. */
	{
		char* s = getenv("TMPDIR");
		if (s) {
			temporary_directory = strdup(s);
			return temporary_directory;
		}
	}
#ifdef P_tmpdir
	/* Use the standard default temporary directory. */
	if (P_tmpdir) {
		temporary_directory = P_tmpdir;
		return temporary_directory;
	}
#endif
	/* Shouldn't ever(!) end up here ... last ditch default. */
	temporary_directory = "/tmp";
	return temporary_directory;
#endif
}
