//
// ExtSupport.PHP4 - substitute for php4ts.dll
//
// Misc.h
// - contains declarations of miscellaneous exported functions
//

#pragma once

#include "stdafx.h"
#include "ExtSupport.h"


/* mode's for php_checkuid() */
#define CHECKUID_DISALLOW_FILE_NOT_EXISTS 0
#define CHECKUID_ALLOW_FILE_NOT_EXISTS 1
#define CHECKUID_CHECK_FILE_AND_DIR 2
#define CHECKUID_ALLOW_ONLY_DIR 3
#define CHECKUID_CHECK_MODE_PARAM 4
#define CHECKUID_ALLOW_ONLY_FILE 5

/* flags for php_checkuid_ex() */
#define CHECKUID_NO_ERRORS	0x01

//#define VCWD_GETCWD(buff, size) getcwd(buff, size)
//#define VCWD_FOPEN(path, mode)  crtx_fopen(path, mode)
//#define VCWD_OPEN(path, flags) crtx_open(path, flags)
//#define VCWD_OPEN_MODE(path, flags, mode) crtx_open(path, flags, mode)
//#define VCWD_CREAT(path, mode) crtx_creat(path, mode)
//#define VCWD_RENAME(oldname, newname) rename(oldname, newname)
//#define VCWD_CHDIR(path) chdir(path)
//#define VCWD_CHDIR_FILE(path) virtual_chdir_file(path, chdir)
//#define VCWD_GETWD(buf) getwd(buf)
//#define VCWD_STAT(path, buff) stat(path, buff)
//#define VCWD_LSTAT(path, buff) lstat(path, buff)
//#define VCWD_UNLINK(path) unlink(path)
//#define VCWD_MKDIR(pathname, mode) mkdir(pathname, mode)
//#define VCWD_RMDIR(pathname) rmdir(pathname)
//#define VCWD_OPENDIR(pathname) opendir(pathname)
//#define VCWD_POPEN(command, type) popen(command, type)
//#define VCWD_ACCESS(pathname, mode) tsrm_win32_access(pathname, mode)
//#define VCWD_REALPATH(path, real_path) strcpy(real_path, path)
//#define VCWD_CHMOD(path, mode) chmod(path, mode)

typedef unsigned short mode_t;

#define DEFAULT_SLASH '\\'
#define DEFAULT_DIR_SEPARATOR	';'
#define IS_SLASH(c)	((c) == '/' || (c) == '\\')
#define IS_SLASH_P(c)	(*(c) == '/' || \
        (*(c) == '\\' && !IsDBCSLeadByte(*(c-1))))

/* COPY_WHEN_ABSOLUTE is 2 under Win32 because by chance both regular absolute paths
   in the file system and UNC paths need copying of two characters */
#define COPY_WHEN_ABSOLUTE(path) 2
#define IS_UNC_PATH(path, len) \
	(len >= 2 && IS_SLASH(path[0]) && IS_SLASH(path[1]))
#define IS_ABSOLUTE_PATH(path, len) \
	(len >= 2 && ((isalpha(path[0]) && path[1] == ':') || IS_UNC_PATH(path, len)))

/* System Rand functions */
#ifndef RAND_MAX
#define RAND_MAX (1<<15)
#endif

#if HAVE_LRAND48
#define PHP_RAND_MAX 2147483647
#else
#define PHP_RAND_MAX RAND_MAX
#endif

#define RAND_RANGE(__n, __min, __max, __tmax) \
    (__n) = (__min) + (long) ((double) ((__max) - (__min) + 1.0) * ((__n) / ((__tmax) + 1.0)))

/* MT Rand */
#define PHP_MT_RAND_MAX ((long) (0x7FFFFFFF)) /* (1<<31) - 1 */ 

#define MT_N (624)

//#define php_rand_r rand_r

#define ZEND_SERVICE_MB_STYLE		(MB_TOPMOST /*| MB_SERVICE_NOTIFICATION*/)

int php_startup_ticks(TSRMLS_D);
void php_shutdown_ticks(TSRMLS_D);
void php_run_ticks(int count);
void lcg_seed(TSRMLS_D);

struct timezone
{
	int tz_minuteswest;
	int tz_dsttime;
};

int inet_aton(const char *cp, struct in_addr *inp);

#ifdef __cplusplus
extern "C"
{
#endif

extern ZEND_API HashTable module_registry;

extern ZEND_API void (*zend_ticks_function)(int ticks);
extern ZEND_API void (*zend_block_interruptions)(void);
extern ZEND_API void (*zend_unblock_interruptions)(void);

ZEND_API void php_add_tick_function(void (*func)(int));
ZEND_API void php_remove_tick_function(void (*func)(int));

ZEND_API int php_checkuid(const char *filename, char *fopen_mode, int mode);
ZEND_API int php_checkuid_ex(const char *filename, char *fopen_mode, int mode, int flags);
ZEND_API char *php_get_current_user(void);

ZEND_API FILE *popen_ex(const char *command, const char *type, const char *cwd, char *env);
ZEND_API FILE *popen(const char *command, const char *type);
ZEND_API int pclose(FILE *stream);

ZEND_API unsigned char *php_base64_encode(const unsigned char *, int, int *);
ZEND_API unsigned char *php_base64_decode(const unsigned char *, int, int *);

ZEND_API int php_rand_r(unsigned int *ctx);
ZEND_API void php_srand(long seed TSRMLS_DC);
ZEND_API long php_rand(TSRMLS_D);
ZEND_API void php_mt_srand(php_uint32 seed TSRMLS_DC);
ZEND_API php_uint32 php_mt_rand(TSRMLS_D);

ZEND_API char *realpath(char *orig_path, char *buffer);

ZEND_API ZEND_FUNCTION(display_disabled_function);
ZEND_API ZEND_FUNCTION(sql_regcase);

ZEND_API void dummy_indent();

ZEND_API int flock(int fd, int operation);
ZEND_API int php_flock(int fd, int operation);

ZEND_API char *get_zend_version();

ZEND_API int gettimeofday(struct timeval *time_Info, struct timezone *timezone_Info);

ZEND_API char *php_get_uname(char mode);

ZEND_API int php_mail(char *to, char *subject, char *message, char *headers, char *extra_cmd TSRMLS_DC);

ZEND_API char *php_ctime_r(const time_t *clock, char *buf);
ZEND_API char *php_asctime_r(const struct tm *tm, char *buf);
ZEND_API struct tm *php_gmtime_r(const time_t *const timep, struct tm *p_tm);
ZEND_API struct tm *php_localtime_r(const time_t *const timep, struct tm *p_tm);
ZEND_API char *php_strtok_r(char *s, const char *delim, char **last);

ZEND_API void zend_timeout(int dummy);
ZEND_API void zend_set_timeout(long seconds);
ZEND_API void zend_unset_timeout(TSRMLS_D);
ZEND_API int zend_set_memory_limit(unsigned int memory_limit);

ZEND_API int php_version_compare(const char *orig_ver1, const char *orig_ver2);
ZEND_API char *php_canonicalize_version(const char *version);

ZEND_API int php_lookup_hostname(const char *addr, struct in_addr *in);
ZEND_API int php_copy_file(char *src, char *dest TSRMLS_DC);
ZEND_API int php_mkdir(char *dir, long mode TSRMLS_DC);

ZEND_API double php_combined_lcg(TSRMLS_D);

ZEND_API char *_xml_zval_strdup(zval *val);
ZEND_API int php_sprintf (char*s, const char* format, ...);

ZEND_API char *php_escape_shell_cmd(char *str);
ZEND_API char *php_escape_shell_arg(char *str);

#ifdef __cplusplus
}
#endif
