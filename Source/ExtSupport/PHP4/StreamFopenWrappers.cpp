//
// ExtSupport.PHP4 - substitute for php4ts.dll
//
// StreamFopenWrappers.cpp 
// - modified fopen_wrappers.c, originally PHP source file
//

#include "stdafx.h"
#include "Streams.h"
#include "StreamFopenWrappers.h"
#include "Variables.h"
#include "Misc.h"
#include "TsrmLs.h"
#include "VirtualWorkingDir.h"

#include <direct.h>

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
   | Authors: Rasmus Lerdorf <rasmus@lerdorf.on.ca>                       |
   |          Jim Winstead <jimw@php.net>                                 |
   +----------------------------------------------------------------------+
 */

/* $Id: StreamFopenWrappers.cpp,v 1.1.2.2 2006/04/15 20:19:12 prosl0am Exp $ */

/* {{{ includes
 */
#include <stdio.h>
#include <stdlib.h>
#include <errno.h>
#include <fcntl.h>

#define O_RDONLY _O_RDONLY

#ifndef S_ISREG
#define S_ISREG(mode)	(((mode) & S_IFMT) == S_IFREG)
#endif

#undef errno
#define errno (*crtx_errno())

//#include <winsock2.h>

#pragma unmanaged

/* }}} */

/* {{{ php_check_specific_open_basedir
	When open_basedir is not NULL, check if the given filename is located in
	open_basedir. Returns -1 if error or not in the open_basedir, else 0
	
	When open_basedir is NULL, always return 0
*/
ZEND_API int php_check_specific_open_basedir(const char *basedir, const char *path TSRMLS_DC)
{
	return 0;
}
/* }}} */

ZEND_API int php_check_open_basedir(const char *path TSRMLS_DC)
{
	return php_check_open_basedir_ex(path, 1 TSRMLS_CC);
}

/* {{{ php_check_open_basedir
 */
ZEND_API int php_check_open_basedir_ex(const char *path, int warn TSRMLS_DC)
{
	/* Nothing to check... */
	return 0;
}
/* }}} */

/* {{{ php_check_safe_mode_include_dir
 */
ZEND_API int php_check_safe_mode_include_dir(char *path TSRMLS_DC)
{
	/* Nothing to check... */
	return 0;
}
/* }}} */

/* {{{ php_fopen_and_set_opened_path
 */
static FILE *php_fopen_and_set_opened_path(const char *path, char *mode, char **opened_path TSRMLS_DC)
{
	FILE *fp;

	if (php_check_open_basedir((char *)path TSRMLS_CC)) {
		return NULL;
	}
	fp = VCWD_FOPEN(path, mode);
	if (fp && opened_path) {
		*opened_path = expand_filepath(path, NULL TSRMLS_CC);
	}
	return fp;
}
/* }}} */

/* {{{ php_fopen_primary_script
 */
ZEND_API int php_fopen_primary_script(zend_file_handle *file_handle TSRMLS_DC)
{
	return FAILURE;
}
/* }}} */

/* {{{ php_fopen_with_path
 * Tries to open a file with a PATH-style list of directories.
 * If the filename starts with "." or "/", the path is ignored.
 */
ZEND_API FILE *php_fopen_with_path(char *filename, char *mode, char *path, char **opened_path TSRMLS_DC)
{
	char *pathbuf, *ptr, *end;
	//char *exec_fname;
	char trypath[MAXPATHLEN];
	struct stat sb;
	FILE *fp;
	//int path_length;
	int filename_length;
	//int exec_fname_length;

	if (opened_path) {
		*opened_path = NULL;
	}
	
	if(!filename) {
		return NULL;
	}

	filename_length = strlen(filename);
	
	/* Relative path open */
	if (*filename == '.') {
		if (PG(safe_mode) && (!php_checkuid(filename, mode, CHECKUID_CHECK_MODE_PARAM))) {
			return NULL;
		}
		return php_fopen_and_set_opened_path(filename, mode, opened_path TSRMLS_CC);
	}
	
	/*
	 * files in safe_mode_include_dir (or subdir) are excluded from
	 * safe mode GID/UID checks
	 */
	
	/* Absolute path open */
	if (IS_ABSOLUTE_PATH(filename, filename_length)) {
		if ((php_check_safe_mode_include_dir(filename TSRMLS_CC)) == 0)
			/* filename is in safe_mode_include_dir (or subdir) */
			return php_fopen_and_set_opened_path(filename, mode, opened_path TSRMLS_CC);
			
		if (PG(safe_mode) && (!php_checkuid(filename, mode, CHECKUID_CHECK_MODE_PARAM)))
			return NULL;

		return php_fopen_and_set_opened_path(filename, mode, opened_path TSRMLS_CC);
	}

	if (!path || (path && !*path)) {
		if (PG(safe_mode) && (!php_checkuid(filename, mode, CHECKUID_CHECK_MODE_PARAM))) {
			return NULL;
		}
		return php_fopen_and_set_opened_path(filename, mode, opened_path TSRMLS_CC);
	}

	/* check in provided path */
	/* append the calling scripts' current working directory
	 * as a fall back case
	 */
	//if (zend_is_executing(TSRMLS_C)) {
	//	exec_fname = zend_get_executed_filename(TSRMLS_C);
	//	exec_fname_length = strlen(exec_fname);
	//	path_length = strlen(path);

	//	while ((--exec_fname_length >= 0) && !IS_SLASH(exec_fname[exec_fname_length]));
	//	if ((exec_fname && exec_fname[0] == '[')
	//		|| exec_fname_length<=0) {
	//		/* [no active file] or no path */
	//		pathbuf = estrdup(path);
	//	} else {		
	//		pathbuf = (char *) emalloc(exec_fname_length + path_length +1 +1);
	//		memcpy(pathbuf, path, path_length);
	//		pathbuf[path_length] = DEFAULT_DIR_SEPARATOR;
	//		memcpy(pathbuf+path_length+1, exec_fname, exec_fname_length);
	//		pathbuf[path_length + exec_fname_length +1] = '\0';
	//	}
	//} else {
		pathbuf = estrdup(path);
	//}

	ptr = pathbuf;

	while (ptr && *ptr) {
		end = strchr(ptr, DEFAULT_DIR_SEPARATOR);
		if (end != NULL) {
			*end = '\0';
			end++;
		}
		snprintf(trypath, MAXPATHLEN, "%s/%s", ptr, filename);
		if (PG(safe_mode)) {
			if (VCWD_STAT(trypath, &sb) == 0) {
				/* file exists ... check permission */
				if ((php_check_safe_mode_include_dir(trypath TSRMLS_CC) == 0) ||
						php_checkuid(trypath, mode, CHECKUID_CHECK_MODE_PARAM))
					/* UID ok, or trypath is in safe_mode_include_dir */
					fp = php_fopen_and_set_opened_path(trypath, mode, opened_path TSRMLS_CC);
				else
					fp = NULL;

				efree(pathbuf);
				return fp;
			}
		}
		fp = php_fopen_and_set_opened_path(trypath, mode, opened_path TSRMLS_CC);
		if (fp) {
			efree(pathbuf);
			return fp;
		}
		ptr = end;
	} /* end provided path */

	efree(pathbuf);
	return NULL;
}
/* }}} */
 
/* {{{ php_strip_url_passwd
 */
ZEND_API char *php_strip_url_passwd(char *url)
{
	register char *p, *url_start;
	
	if (url == NULL) {
		return "";
	}
	
	p = url;
	
	while (*p) {
		if (*p==':' && *(p+1)=='/' && *(p+2)=='/') {
			/* found protocol */
			url_start = p = p+3;
			
			while (*p) {
				if (*p=='@') {
					int i;
					
					for (i=0; i<3 && url_start<p; i++, url_start++) {
						*url_start = '.';
					}
					for (; *p; p++) {
						*url_start++ = *p;
					}
					*url_start=0;
					break;
				}
				p++;
			}
			return url;
		}
		p++;
	}
	return url;
}
/* }}} */

/* {{{ expand_filepath
 */
ZEND_API char *expand_filepath(const char *filepath, char *real_path TSRMLS_DC)
{
	cwd_state new_state;
	char cwd[MAXPATHLEN];
	char *result;

	result = VCWD_GETCWD(cwd, MAXPATHLEN);	
	if (!result) {
		cwd[0] = '\0';
	}

	new_state.cwd = _strdup(cwd);
	new_state.cwd_length = strlen(cwd);

	if (virtual_file_ex(&new_state, filepath, NULL, 1))
	{
		free(new_state.cwd);
		return NULL;
	}

	if(real_path) {
		int copy_len = new_state.cwd_length>MAXPATHLEN-1?MAXPATHLEN-1:new_state.cwd_length;
		memcpy(real_path, new_state.cwd, copy_len);
		real_path[copy_len]='\0';
	} else {
		real_path = estrndup(new_state.cwd, new_state.cwd_length);
	}
	free(new_state.cwd);

	return real_path;
}
/* }}} */
