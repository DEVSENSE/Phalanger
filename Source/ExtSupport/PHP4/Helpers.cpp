//
// ExtSupport.PHP4 - substitute for php4ts.dll
//
// Helpers.cpp 
// - contains definitions of helper functions that are not part of the Zend API and are
//   not related to only one group of Zend API functions
//

#include "stdafx.h"
#include "Helpers.h"
#include "Variables.h"

#include <stdlib.h>
#include <errno.h>
#include <float.h>
#include <io.h>

#undef errno
#define errno (*crtx_errno())

using namespace System;

#pragma unmanaged

// copied from zend_operators.h, slightly modified and beautified
int is_numeric_string(char *str, int length, long *lval, double *dval, zend_bool allow_errors)
{
	long local_lval;
	double local_dval;
	char *end_ptr_long, *end_ptr_double;
	int conv_base = 10;

	if (!length) return 0;
	
	/* handle hex numbers */
	if (length >= 2 && str[0 ]== '0' && (str[1] == 'x' || str[1] == 'X')) conv_base = 16;
	
	errno = 0;
	local_lval = strtol(str, &end_ptr_long, conv_base);

	if (errno != ERANGE)
	{
		if (end_ptr_long == str + length) /* integer string */
		{
			if (lval) *lval = local_lval;
			return IS_LONG;
		}
	} else end_ptr_long = NULL;

	/* hex string, under UNIX strtod() messes it up		*/
	/* UNIX? AND WHO CARES? (interpreter's remark :-)	*/
	if (conv_base == 16) return 0;

	errno = 0;
	local_dval = strtod(str, &end_ptr_double);
	if (errno != ERANGE)
	{
		if (end_ptr_double == str + length) /* floating point string */
		{
			if (!zend_finite(local_dval))
			{
				/* "inf", "nan" and maybe other weird ones */
				return 0;
			}

			if (dval) *dval = local_dval;
			return IS_DOUBLE;
		}
	} 
	else end_ptr_double = NULL;

	if (allow_errors)
	{
		if (end_ptr_double > end_ptr_long && dval)
		{
			*dval = local_dval;
			return IS_DOUBLE;
		} 
		else if (end_ptr_long && lval)
		{
			*lval = local_lval;
			return IS_LONG;
		}
	}
	return 0;
}

// copied from flock.c and beautified
//int flock(int fd, int op)
//{
//	HANDLE hdl = (HANDLE)crtx_get_osfhandle(fd);
//	DWORD low = 1, high = 0;
//	OVERLAPPED offset =	{0, 0, 0, 0, NULL};
//
//	if (hdl < 0) return -1;			/* error in file descriptor */
//	/* bug for bug compatible with Unix */
//	UnlockFileEx(hdl, 0, low, high, &offset);
//
//	switch (op & ~LOCK_NB)
//	{								/* translate to LockFileEx() op */
//		case LOCK_EX:				/* exclusive */
//			if (LockFileEx(hdl, LOCKFILE_EXCLUSIVE_LOCK +
//				((op & LOCK_NB) ? LOCKFILE_FAIL_IMMEDIATELY : 0), 0, low, high, &offset))
//				return 0;
//			break;
//
//		case LOCK_SH:				/* shared */
//			if (LockFileEx(hdl, ((op & LOCK_NB) ? LOCKFILE_FAIL_IMMEDIATELY : 0),
//				0, low, high, &offset)) return 0;
//			break;
//
//		case LOCK_UN:				/* unlock */
//			return 0;				/* always succeeds */
//
//		default:					/* default */
//			break;
//	}
//	errno = EINVAL;				/* bad call */
//	return -1;
//}

// copied from readdir.c and beautified
ZEND_API DIR *opendir(const char *dir)
{
	DIR *dp;
	char *filespec;
	long handle;
	int index;

	filespec = (char *)malloc(strlen(dir) + 2 + 1);
	strcpy(filespec, dir);
	index = strlen(filespec) - 1;
	if (index >= 0 && (filespec[index] == '/' || (filespec[index] == '\\' && !IsDBCSLeadByte(filespec[index-1]))))
	{
		filespec[index] = '\0';
	}
	strcat(filespec, "/*");

	dp = (DIR *)malloc(sizeof(DIR));
	dp->offset = 0;
	dp->finished = 0;
	dp->dir = _strdup(dir);

	if ((handle = _findfirst(filespec, &(dp->fileinfo))) < 0)
	{
		if (errno == ENOENT) dp->finished = 1;
		else return NULL;
	}
	dp->handle = handle;
	free(filespec);

	return dp;
}

// copied from readdir.c and beautified
ZEND_API struct dirent *readdir(DIR *dp)
{
	if (!dp || dp->finished) return NULL;

	if (dp->offset != 0)
	{
		if (_findnext(dp->handle, &(dp->fileinfo)) < 0)
		{
			dp->finished = 1;
			return NULL;
		}
	}
	dp->offset++;

	strlcpy(dp->dent.d_name, dp->fileinfo.name, _MAX_FNAME + 1);
	dp->dent.d_ino = 1;
	dp->dent.d_reclen = strlen(dp->dent.d_name);
	dp->dent.d_off = dp->offset;

	return &(dp->dent);
}

// copied from readdir.c and beautified
ZEND_API int readdir_r(DIR *dp, struct dirent *entry, struct dirent **result)
{
	if (!dp || dp->finished)
	{
		*result = NULL;
		return 0;
	}

	if (dp->offset != 0)
	{
		if (_findnext(dp->handle, &(dp->fileinfo)) < 0)
		{
			dp->finished = 1;
			*result = NULL;
			return 0;
		}
	}
	dp->offset++;

	strlcpy(dp->dent.d_name, dp->fileinfo.name, _MAX_FNAME + 1);
	dp->dent.d_ino = 1;
	dp->dent.d_reclen = strlen(dp->dent.d_name);
	dp->dent.d_off = dp->offset;

	memcpy(entry, &dp->dent, sizeof(*entry));

	*result = &dp->dent;

	return 0;
}

// copied from readdir.c and beautified
ZEND_API int closedir(DIR *dp)
{
	if (!dp) return 0;
	_findclose(dp->handle);

	if (dp->dir) free(dp->dir);
	if (dp) free(dp);

	return 0;
}

// copied from readdir.c and beautified
ZEND_API int rewinddir(DIR *dp)
{
	/* Re-set to the beginning */
	char *filespec;
	long handle;
	int index;

	_findclose(dp->handle);

	dp->offset = 0;
	dp->finished = 0;

	filespec = (char *)malloc(strlen(dp->dir) + 2 + 1);
	strcpy(filespec, dp->dir);
	index = strlen(filespec) - 1;
	if (index >= 0 && (filespec[index] == '/' || filespec[index] == '\\'))
	{
		filespec[index] = '\0';
	}
	strcat(filespec, "/*");

	if ((handle = _findfirst(filespec, &(dp->fileinfo))) < 0)
	{
		if (errno == ENOENT) dp->finished = 1;
	}
	dp->handle = handle;
	free(filespec);

	return 0;
}

long php_getuid(void)
{
	return 1;
}

long php_getgid(void)
{
	return 1;
}

int php_win32_check_trailing_space(const char * path, const int path_len) {
	if (path_len < 1) {
		return 1;
	}
	if (path) {
		if (path[0] == ' ' || path[path_len - 1] == ' ') {
			return 0;
		} else {
			return 1;
		}
	} else {
		return 0;
	}
}