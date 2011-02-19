//
// ExtSupport.PHP4 - substitute for php4ts.dll
//
// Helpers.h
// - contains declarations of helper functions that are not part of the Zend API are
//   not related to only one group of Zend API functions
//

#pragma once

#include "ExtSupport.h"
#include "stdafx.h"

#include <io.h>

int is_numeric_string(char *str, int length, long *lval, double *dval, zend_bool allow_errors);

#define LOCK_SH 1
#define LOCK_EX 2
#define LOCK_NB 4
#define LOCK_UN 8

#define fsync _commit
#define ftruncate chsize

//int flock(int fd, int op);

/* struct dirent - same as Unix */

struct dirent
{
	long d_ino;						/* inode (always 1 in WIN32) */
	off_t d_off;					/* offset to this dirent */
	unsigned short d_reclen;		/* length of d_name */
	char d_name[_MAX_FNAME + 1];	/* filename (null terminated) */
};


/* typedef DIR - not the same as Unix */
typedef struct
{
	long handle;					/* _findfirst/_findnext handle */
	short offset;					/* offset into directory */
	short finished;					/* 1 if there are not more files */
	struct _finddata_t fileinfo;	/* from _findfirst/_findnext */
	char *dir;						/* the dir we are reading */
	struct dirent dent;				/* the dirent to return */
} DIR;

#define php_readdir_r readdir_r

/* Function prototypes */
#ifdef __cplusplus
extern "C" 
{
#endif

ZEND_API DIR *opendir(const char *);
ZEND_API struct dirent *readdir(DIR *);
ZEND_API int readdir_r(DIR *, struct dirent *, struct dirent **);
ZEND_API int closedir(DIR *);
ZEND_API int rewinddir(DIR *);

#ifdef __cplusplus
}
#endif

long php_getuid(void);
long php_getgid(void);

int php_win32_check_trailing_space(const char * path, const int path_len);