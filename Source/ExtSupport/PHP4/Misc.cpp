//
// ExtSupport.PHP4 - substitute for php4ts.dll
//
// Misc.cpp
// - contains definitions of miscellaneous exported functions
//

#include "stdafx.h"
#include "Misc.h"
#include "Streams.h"
#include "TsrmLs.h"
#include "Variables.h"
#include "Errors.h"
#include "Helpers.h"
#include "Parameters.h"
#include "LinkedLists.h"
#include "Spprintf.h"
#include "Output.h"
#include "IniConfig.h"
#include "Unsupported.h"
#include "VirtualWorkingDir.h"
#include "StreamFopenWrappers.h"

#include <direct.h>
#include <fcntl.h>
#include <stdio.h>
#include <errno.h>
#include <time.h>

#pragma unmanaged

#undef errno
#define errno (*crtx_errno())

// loaded extensions
ZEND_API HashTable module_registry;

// copied from tsrm_win32.c and beautified
static HANDLE dupHandle(HANDLE fh, BOOL inherit)
{
	HANDLE copy, self = GetCurrentProcess();
	if (!DuplicateHandle(self, fh, self, &copy, 0, inherit, DUPLICATE_SAME_ACCESS|DUPLICATE_CLOSE_SOURCE))
	{
		return NULL;
	}
	return copy;
}

// copied from safe_mode.c and beaufied
/*
 * php_checkuid
 *
 * This function has six modes:
 * 
 * 0 - return invalid (0) if file does not exist
 * 1 - return valid (1)  if file does not exist
 * 2 - if file does not exist, check directory
 * 3 - only check directory (needed for mkdir)
 * 4 - check mode and param
 * 5 - only check file
 */
ZEND_API int php_checkuid_ex(const char *filename, char *fopen_mode, int mode, int flags)
{
	struct stat sb;
	int ret, nofile=0;
	long uid = 0L, gid = 0L, duid = 0L, dgid = 0L;
	char path[_MAX_PATH];
	char *s, filenamecopy[_MAX_PATH];
	php_stream_wrapper *wrapper = NULL;
	TSRMLS_FETCH();

	strlcpy(filenamecopy, filename, _MAX_PATH);
	filename = (char *)&filenamecopy;

	if (!filename)
	{
		return 0; /* path must be provided */
	}

	if (fopen_mode)
	{
		if (fopen_mode[0] == 'r') mode = CHECKUID_DISALLOW_FILE_NOT_EXISTS;
		else mode = CHECKUID_CHECK_FILE_AND_DIR;
	}

	/* 
	 * If given filepath is a URL, allow - safe mode stuff
	 * related to URL's is checked in individual functions
	 */
	wrapper = php_stream_locate_url_wrapper(filename, NULL, STREAM_LOCATE_WRAPPERS_ONLY TSRMLS_CC);
	if (wrapper != NULL) return 1;
		
	/* 
	 * First we see if the file is owned by the same user...
	 * If that fails, passthrough and check directory...
	 */
	if (mode != CHECKUID_ALLOW_ONLY_DIR)
	{
		VCWD_REALPATH(filename, path);
		ret = VCWD_STAT(path, &sb);
		if (ret < 0)
		{
			if (mode == CHECKUID_DISALLOW_FILE_NOT_EXISTS)
			{
				if ((flags & CHECKUID_NO_ERRORS) == 0)
				{
					php_error_docref(NULL TSRMLS_CC, E_WARNING, "Unable to access %s", filename);
				}
				return 0;
			}
			else if (mode == CHECKUID_ALLOW_FILE_NOT_EXISTS)
			{
				if ((flags & CHECKUID_NO_ERRORS) == 0)
				{
					php_error_docref(NULL TSRMLS_CC, E_WARNING, "Unable to access %s", filename);
				}
				return 1;
			} 
			nofile = 1;
		}
		else
		{
			uid = sb.st_uid;
			gid = sb.st_gid;
			if (uid == php_getuid()) return 1;
 			else if (PG(safe_mode_gid) && gid == php_getgid()) return 1;
		}

		/* Trim off filename */
		if ((s = strrchr(path, DEFAULT_SLASH)))
		{
			if (s == path) path[1] = '\0';
			else *s = '\0';
		}
	}
	else
	{
		/* CHECKUID_ALLOW_ONLY_DIR */
		s = strrchr(const_cast<char *>(filename), DEFAULT_SLASH);

		if (s == filename)
		{
			/* root dir */
			path[0] = DEFAULT_SLASH;
			path[1] = '\0';
		}
		else if (s)
		{
			*s = '\0';
			VCWD_REALPATH(filename, path);
			*s = DEFAULT_SLASH;
		}
		else
		{
			/* Under Solaris, getcwd() can fail if there are no
			 * read permissions on a component of the path, even
			 * though it has the required x permissions */
			path[0] = '.';
			path[1] = '\0';
			VCWD_GETCWD(path, sizeof(path));
 		}
	} /* end CHECKUID_ALLOW_ONLY_DIR */
	
	if (mode != CHECKUID_ALLOW_ONLY_FILE)
	{
		/* check directory */
		ret = VCWD_STAT(path, &sb);
		if (ret < 0)
		{
			if ((flags & CHECKUID_NO_ERRORS) == 0)
			{
				php_error_docref(NULL TSRMLS_CC, E_WARNING, "Unable to access %s", filename);
			}
			return 0;
		}
		duid = sb.st_uid;
		dgid = sb.st_gid;
		if (duid == php_getuid()) return 1;
 		else if (PG(safe_mode_gid) && dgid == php_getgid()) return 1;
		else
		{
			TSRMLS_FETCH();
/*
			if (SG(rfc1867_uploaded_files))
			{
				if (zend_hash_exists(SG(rfc1867_uploaded_files), (char *) filename, strlen(filename)+1))
				{
					return 1;
				}
			}
*/
		}
	}

	if (mode == CHECKUID_ALLOW_ONLY_DIR)
	{
		uid = duid;
		gid = dgid;
		if (s) *s = 0;
	}
	
	if (nofile)
	{
		uid = duid;
		gid = dgid;
		filename = path;
	}

	if ((flags & CHECKUID_NO_ERRORS) == 0)
	{
		if (PG(safe_mode_gid))
		{
			php_error_docref(NULL TSRMLS_CC, E_WARNING, "SAFE MODE Restriction in effect.  The script whose uid/gid is %ld/%ld is not allowed to access %s owned by uid/gid %ld/%ld", php_getuid(), php_getgid(), filename, uid, gid);
		}
		else
		{
			php_error_docref(NULL TSRMLS_CC, E_WARNING, "SAFE MODE Restriction in effect.  The script whose uid is %ld is not allowed to access %s owned by uid %ld", php_getuid(), filename, uid);
		}			
	}
	return 0;
}

// copied from safe_mode.c and beaufied
ZEND_API int php_checkuid(const char *filename, char *fopen_mode, int mode)
{
	return php_checkuid_ex(filename, fopen_mode, mode, 0);
}

// originally in safe_mode.c, changed
ZEND_API char *php_get_current_user()
{
	return empty_string;
}	

#pragma managed

// Provides file handle to process handle mapping.
// Just a cast wrapper around a hashtable.
private ref class StreamToProcessMap
{
private:
	StreamToProcessMap()
	{ }

public:
	// Adds file to process mapping.
	static void AddHandleStreamPair(FILE *stream, HANDLE handle)
	{
		map->Add(IntPtr(stream), IntPtr(handle));
	}

	// Gets process by file handle and removes the mapping.
	static HANDLE GetProcessHandleByStream(FILE *stream)
	{
		Object ^obj = map[IntPtr(stream)];
		if (obj != nullptr)
		{
			map->Remove(IntPtr(stream));
			return (HANDLE)(static_cast<IntPtr ^>(obj)->ToPointer());
		}
		return NULL;
	}

private:
	static Hashtable ^map = Hashtable::Synchronized(gcnew Hashtable());
};

// copied from tsrm_win32.c
ZEND_API FILE *popen(const char *command, const char *type)
{
	return popen_ex(command, type, NULL, NULL);
}

// copied from tsrm_win32.c and beautified
ZEND_API FILE *popen_ex(const char *command, const char *type, const char *cwd, char *env)
{
	FILE *stream = NULL;
	int fno, str_len = strlen(type), read, mode;
	STARTUPINFOA startup;
	PROCESS_INFORMATION process;
	SECURITY_ATTRIBUTES security;
	HANDLE in, out;
	char *cmd, *comspec = "cmd.exe";
	TSRMLS_FETCH();

	security.nLength				= sizeof(SECURITY_ATTRIBUTES);
	security.bInheritHandle			= TRUE;
	security.lpSecurityDescriptor	= NULL;

	if (!str_len || !CreatePipe(&in, &out, &security, 2048L)) return NULL;
	
	memset(&startup, 0, sizeof(STARTUPINFOA));
	memset(&process, 0, sizeof(PROCESS_INFORMATION));

	startup.cb			= sizeof(STARTUPINFOA);
	startup.dwFlags		= STARTF_USESTDHANDLES;
	startup.hStdError	= GetStdHandle(STD_ERROR_HANDLE);

	read = (type[0] == 'r') ? TRUE : FALSE;
	mode = ((str_len == 2) && (type[1] == 'b')) ? O_BINARY : O_TEXT;

	if (read)
	{
		in = dupHandle(in, FALSE);
		startup.hStdInput  = GetStdHandle(STD_INPUT_HANDLE);
		startup.hStdOutput = out;
	}
	else
	{
		out = dupHandle(out, FALSE);
		startup.hStdInput  = in;
		startup.hStdOutput = GetStdHandle(STD_OUTPUT_HANDLE);
	}

	cmd = (char*)malloc(strlen(command) + strlen(comspec) + sizeof(" /c "));
	sprintf(cmd, "%s /c %s", comspec, command);
	if (!CreateProcessA(NULL, cmd, &security, &security, security.bInheritHandle, 
		NORMAL_PRIORITY_CLASS, env, cwd, &startup, &process))
	{
		return NULL;
	}
	free(cmd);

	CloseHandle(process.hThread);

	if (read)
	{
		fno = crtx_open_osfhandle((long)in, _O_RDONLY | mode);
		CloseHandle(out);
	}
	else
	{
		fno = crtx_open_osfhandle((long)out, _O_WRONLY | mode);
		CloseHandle(in);
	}

	stream = _fdopen(fno, type);
	StreamToProcessMap::AddHandleStreamPair(stream, process.hProcess);
	return stream;}

// copied from tsrm_win32.c and beautified
ZEND_API int pclose(FILE *stream)
{
	DWORD termstat = 0;
	TSRMLS_FETCH();

	HANDLE hProcess = StreamToProcessMap::GetProcessHandleByStream(stream);

	if (hProcess == NULL) return 0;

	crtx_fflush(stream);
    crtx_fclose(stream);

	WaitForSingleObject(hProcess, INFINITE);
	GetExitCodeProcess(hProcess, &termstat);
	CloseHandle(hProcess);

	return termstat;
}

#pragma unmanaged

// copied from base64.c
static const char base64_table[] =
	{ 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M',
	  'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z',
	  'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm',
	  'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z',
	  '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '+', '/', '\0'
	};

// copied from base64.c
static const char base64_pad = '=';

// copied from base64.c
static const short base64_reverse_table[256] = {
	-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
	-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
	-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 62, -1, -1, -1, 63,
	52, 53, 54, 55, 56, 57, 58, 59, 60, 61, -1, -1, -1, -1, -1, -1,
	-1,  0,  1,  2,  3,  4,  5,  6,  7,  8,  9, 10, 11, 12, 13, 14,
	15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, -1, -1, -1, -1, -1,
	-1, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40,
	41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, -1, -1, -1, -1, -1,
	-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
	-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
	-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
	-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
	-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
	-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
	-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
	-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1
};
/* }}} */

// copied from base64.c and beautified
/* {{{ php_base64_encode */
ZEND_API unsigned char *php_base64_encode(const unsigned char *str, int length, int *ret_length)
{
	const unsigned char *current = str;
	unsigned char *p;
	unsigned char *result;

	if ((length + 2) < 0 || ((length + 2) / 3) >= (1 << (sizeof(int) * 8 - 2)))
	{
		if (ret_length != NULL) *ret_length = 0;
		return NULL;
	}

	result = (unsigned char *)safe_emalloc(((length + 2) / 3) * 4, sizeof(char), 1);
	p = result;

	while (length > 2)
	{ /* keep going until we have less than 24 bits */
		*p++ = base64_table[current[0] >> 2];
		*p++ = base64_table[((current[0] & 0x03) << 4) + (current[1] >> 4)];
		*p++ = base64_table[((current[1] & 0x0f) << 2) + (current[2] >> 6)];
		*p++ = base64_table[current[2] & 0x3f];

		current += 3;
		length -= 3; /* we just handle 3 octets of data */
	}

	/* now deal with the tail end of things */
	if (length != 0)
	{
		*p++ = base64_table[current[0] >> 2];
		if (length > 1)
		{
			*p++ = base64_table[((current[0] & 0x03) << 4) + (current[1] >> 4)];
			*p++ = base64_table[(current[1] & 0x0f) << 2];
			*p++ = base64_pad;
		}
		else
		{
			*p++ = base64_table[(current[0] & 0x03) << 4];
			*p++ = base64_pad;
			*p++ = base64_pad;
		}
	}
	if (ret_length != NULL) *ret_length = (int)(p - result);
	*p = '\0';
	return result;
}
/* }}} */

// copied from base64.c and beautified
/* {{{ php_base64_decode */
/* as above, but backwards. :) */
ZEND_API unsigned char *php_base64_decode(const unsigned char *str, int length, int *ret_length)
{
	const unsigned char *current = str;
	int ch, i = 0, j = 0, k;
	/* this sucks for threaded environments */
	unsigned char *result;
	
	result = (unsigned char *)emalloc(length + 1);
	if (result == NULL)	return NULL;

	/* run through the whole string, converting as we go */
	while ((ch = *current++) != '\0' && length-- > 0)
	{
		if (ch == base64_pad) break;

	    /* When Base64 gets POSTed, all pluses are interpreted as spaces.
		   This line changes them back.  It's not exactly the Base64 spec,
		   but it is completely compatible with it (the spec says that
		   spaces are invalid).  This will also save many people considerable
		   headache.  - Turadg Aleahmad <turadg@wise.berkeley.edu>
	    */

		if (ch == ' ') ch = '+'; 

		ch = base64_reverse_table[ch];
		if (ch < 0) continue;

		switch(i % 4)
		{
			case 0:
				result[j] = ch << 2;
				break;
			case 1:
				result[j++] |= ch >> 4;
				result[j] = (ch & 0x0f) << 4;
				break;
			case 2:
				result[j++] |= ch >>2;
				result[j] = (ch & 0x03) << 6;
				break;
			case 3:
				result[j++] |= ch;
				break;
		}
		i++;
	}

	k = j;
	/* mop things up if we ended on a boundary */
	if (ch == base64_pad)
	{
		switch(i % 4)
		{
			case 0:
			case 1:
				efree(result);
				return NULL;
			case 2:
				k++;
			case 3:
				result[k++] = 0;
		}
	}
	if(ret_length) *ret_length = j;
	result[j] = '\0';
	return result;
}
/* }}} */

#pragma managed

/* SYSTEM RAND FUNCTIONS */

private ref class StateHolder
{
private:
	unsigned int *value;

public:
	StateHolder(int size)
	{
		value = new unsigned int[size];
	}

	~StateHolder()
	{
		delete[] value;
	}

	property unsigned int *Value
	{
		unsigned int *get()
		{
			return value;
		}
	}
};
// Random generator per-thread context.
private ref class RandomContext
{
private:
	[ThreadStatic]
	static StateHolder ^state;

public:
	static property StateHolder^ State
	{
		static StateHolder ^get()
		{
			if (state == nullptr) state = gcnew StateHolder(MT_N + 1);
			return state;
		}
	}
		
	[ThreadStatic]
	static unsigned int Seed;

	[ThreadStatic]
	static unsigned int Next;

	[ThreadStatic]
	static int Left;
};

#pragma unmanaged

// copied from reentrancy.c
static int do_rand(unsigned long *ctx)
{
	return ((*ctx = *ctx * 1103515245 + 12345) % ((u_long)RAND_MAX + 1));
}

// copied from reentrancy.c
ZEND_API int php_rand_r(unsigned int *ctx)
{
	u_long val = (u_long) *ctx;
	*ctx = do_rand(&val);
	return (int) *ctx;
}

#pragma managed

// copied from rand.c, modified and beautified
/* {{{ php_srand
 */
ZEND_API void php_srand(long seed TSRMLS_DC)
{
	RandomContext::Seed = (unsigned int)seed;
}
/* }}} */

// copied from rand.c, modified and beautified
/* {{{ php_rand
 */
ZEND_API long php_rand(TSRMLS_D)
{
	long ret;
	unsigned int _seed = RandomContext::Seed;

	ret = php_rand_r(&_seed);
	RandomContext::Seed = _seed;

	return ret;
}
/* }}} */


/* MT RAND FUNCTIONS */

/*
   This is the ``Mersenne Twister'' random number generator MT19937, which
   generates pseudorandom integers uniformly distributed in 0..(2^32 - 1)
   starting from any odd seed in 0..(2^32 - 1).  This version is a recode
   by Shawn Cokus (Cokus@math.washington.edu) on March 8, 1998 of a version by
   Takuji Nishimura (who had suggestions from Topher Cooper and Marc Rieffel in
   July-August 1997).
  
   Effectiveness of the recoding (on Goedel2.math.washington.edu, a DEC Alpha
   running OSF/1) using GCC -O3 as a compiler: before recoding: 51.6 sec. to
   generate 300 million random numbers; after recoding: 24.0 sec. for the same
   (i.e., 46.5% of original time), so speed is now about 12.5 million random
   number generations per second on this machine.
  
   According to the URL <http://www.math.keio.ac.jp/~matumoto/emt.html>
   (and paraphrasing a bit in places), the Mersenne Twister is ``designed
   with consideration of the flaws of various existing generators,'' has
   a period of 2^19937 - 1, gives a sequence that is 623-dimensionally
   equidistributed, and ``has passed many stringent tests, including the
   die-hard test of G. Marsaglia and the load test of P. Hellekalek and
   S. Wegenkittl.''  It is efficient in memory usage (typically using 2506
   to 5012 bytes of static data, depending on data type sizes, and the code
   is quite short as well).  It generates random numbers in batches of 624
   at a time, so the caching and pipelining of modern systems is exploited.
   It is also divide- and mod-free.
  
   This library is free software; you can redistribute it and/or modify it
   under the terms of the GNU Library General Public License as published by
   the Free Software Foundation (either version 2 of the License or, at your
   option, any later version).  This library is distributed in the hope that
   it will be useful, but WITHOUT ANY WARRANTY, without even the implied
   warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See
   the GNU Library General Public License for more details.  You should have
   received a copy of the GNU Library General Public License along with this
   library; if not, write to the Free Software Foundation, Inc., 59 Temple
   Place, Suite 330, Boston, MA 02111-1307, USA.
  
   The code as Shawn received it included the following notice:
  
     Copyright (C) 1997 Makoto Matsumoto and Takuji Nishimura.  When
     you use this, send an e-mail to <matumoto@math.keio.ac.jp> with
     an appropriate reference to your work.
  
   It would be nice to CC: <Cokus@math.washington.edu> when you write.
  

  
   php_uint32 must be an unsigned integer type capable of holding at least 32
   bits; exactly 32 should be fastest, but 64 is better on an Alpha with
   GCC at -O3 optimization so try your options and see what's best for you

   Melo: we should put some ifdefs here to catch those alphas...
*/
#define N             MT_N                 /* length of state vector */
#define M             (397)                /* a period parameter */
#define K             (0x9908B0DFU)        /* a magic constant */
#define hiBit(u)      ((u) & 0x80000000U)  /* mask all but highest   bit of u */
#define loBit(u)      ((u) & 0x00000001U)  /* mask all but lowest    bit of u */
#define loBits(u)     ((u) & 0x7FFFFFFFU)  /* mask     the highest   bit of u */
#define mixBits(u, v) (hiBit(u)|loBits(v)) /* move hi bit of u to hi bit of v */

// copied from rand.c, modified and beautified
/* {{{ php_mt_srand
 */
ZEND_API void php_mt_srand(php_uint32 seed TSRMLS_DC)
{
	/*
	   We initialize state[0..(N-1)] via the generator

	     x_new = (69069 * x_old) mod 2^32

	   from Line 15 of Table 1, p. 106, Sec. 3.3.4 of Knuth's
	   _The Art of Computer Programming_, Volume 2, 3rd ed.

	   Notes (SJC): I do not know what the initial state requirements
	   of the Mersenne Twister are, but it seems this seeding generator
	   could be better.  It achieves the maximum period for its modulus
	   (2^30) iff x_initial is odd (p. 20-21, Sec. 3.2.1.2, Knuth); if
	   x_initial can be even, you have sequences like 0, 0, 0, ...;
	   2^31, 2^31, 2^31, ...; 2^30, 2^30, 2^30, ...; 2^29, 2^29 + 2^31,
	   2^29, 2^29 + 2^31, ..., etc. so I force seed to be odd below.

	   
	   Even if x_initial is odd, if x_initial is 1 mod 4 then

	     the          lowest bit of x is always 1,
	     the  next-to-lowest bit of x is always 0,
	     the 2nd-from-lowest bit of x alternates      ... 0 1 0 1 0 1 0 1 ... ,
	     the 3rd-from-lowest bit of x 4-cycles        ... 0 1 1 0 0 1 1 0 ... ,
	     the 4th-from-lowest bit of x has the 8-cycle ... 0 0 0 1 1 1 1 0 ... ,
	      ...

	   and if x_initial is 3 mod 4 then

	     the          lowest bit of x is always 1,
	     the  next-to-lowest bit of x is always 1,
	     the 2nd-from-lowest bit of x alternates      ... 0 1 0 1 0 1 0 1 ... ,
	     the 3rd-from-lowest bit of x 4-cycles        ... 0 0 1 1 0 0 1 1 ... ,
	     the 4th-from-lowest bit of x has the 8-cycle ... 0 0 1 1 1 1 0 0 ... ,
	      ...

	   The generator's potency (min. s>=0 with (69069-1)^s = 0 mod 2^32) is
	   16, which seems to be alright by p. 25, Sec. 3.2.1.3 of Knuth.  It
	   also does well in the dimension 2..5 spectral tests, but it could be
	   better in dimension 6 (Line 15, Table 1, p. 106, Sec. 3.3.4, Knuth).

	   Note that the random number user does not see the values generated
	   here directly since reloadMT() will always munge them first, so maybe
	   none of all of this matters.  In fact, the seed values made here could
	   even be extra-special desirable if the Mersenne Twister theory says
	   so-- that's why the only change I made is to restrict to odd seeds.
	*/

	register php_uint32 x = (seed | 1U) & 0xFFFFFFFFU;
	php_uint32 *s = RandomContext::State->Value;
	register int j;
	
	for (RandomContext::Left = 0, *s++ = x, j = N; --j; *s++ = (x *= 69069U) & 0xFFFFFFFFU);
}
/* }}} */

// copied from rand.c, modified and beautified
/* {{{ php_mt_reload
 */
static php_uint32 php_mt_reload(TSRMLS_D)
{
	php_uint32 *s = RandomContext::State->Value;

	php_uint32 *p0 = s;
	php_uint32 *p2 = s + 2;
	php_uint32 *pM = s + M;
	php_uint32 s0, s1;
	register int j;

	if (RandomContext::Left < -1) php_mt_srand(4357U TSRMLS_CC);

	RandomContext::Left = N - 1, RandomContext::Next = 1;

	for (s0 = s[0], s1 = s[1], j = N - M + 1; --j; s0 = s1, s1 = *p2++)
		*p0++ = *pM++ ^ (mixBits(s0, s1) >> 1) ^ (loBit(s1) ? K : 0U);

	for (pM = s, j = M; --j; s0 = s1, s1 = *p2++)
		*p0++ = *pM++ ^ (mixBits(s0, s1) >> 1) ^ (loBit(s1) ? K : 0U);

	s1 = s[0], *p0 = *pM ^ (mixBits(s0, s1) >> 1) ^ (loBit(s1) ? K : 0U);
	s1 ^= (s1 >> 11);
	s1 ^= (s1 <<  7) & 0x9D2C5680U;
	s1 ^= (s1 << 15) & 0xEFC60000U;

	return s1 ^ (s1 >> 18);
}
/* }}} */

// copied from rand.c, modified and beautified
/* {{{ php_mt_rand
 */
ZEND_API php_uint32 php_mt_rand(TSRMLS_D)
{
	php_uint32 y;

	if (--RandomContext::Left < 0)	return php_mt_reload(TSRMLS_C);

	y  = *(RandomContext::Next++ + RandomContext::State->Value);
	y ^= (y >> 11);
	y ^= (y <<  7) & 0x9D2C5680U;
	y ^= (y << 15) & 0xEFC60000U;

	return y ^ (y >> 18);
}
/* }}} */

#define GENERATE_SEED() ((long) (time(0) * GetCurrentProcessId() * 1000000 * php_combined_lcg(TSRMLS_C)))

#pragma unmanaged

// copied from tsrm_win32.c and beautified
ZEND_API char *realpath(char *orig_path, char *buffer)
{
	int ret = GetFullPathNameA(orig_path, _MAX_PATH, buffer, NULL);
	if(!ret || ret > _MAX_PATH) return NULL;
	
	return buffer;
}

// copied from zend_API.c
ZEND_API ZEND_FUNCTION(display_disabled_function)
{
	zend_error(E_WARNING, "%s() has been disabled for security reasons", get_active_function_name(TSRMLS_C));
}

// copied from reg.c
/* {{{ proto string sql_regcase(string string)
   Make regular expression for case insensitive match */
ZEND_API ZEND_FUNCTION(sql_regcase)
{
	zval **string;
	char *tmp;
	unsigned char c;
	register int i, j;
	
	if (ZEND_NUM_ARGS() != 1 || zend_get_parameters_ex(1, &string) == FAILURE) WRONG_PARAM_COUNT;

	convert_to_string_ex(string);
	
	tmp = (char *)safe_emalloc(Z_STRLEN_PP(string), 4, 1);
	
	for (i = j = 0; i < Z_STRLEN_PP(string); i++)
	{
		c = (unsigned char) Z_STRVAL_PP(string)[i];
		if (isalpha(c))
		{
			tmp[j++] = '[';
			tmp[j++] = toupper(c);
			tmp[j++] = tolower(c);
			tmp[j++] = ']';
		}
		else tmp[j++] = c;
	}
	tmp[j] = 0;

	RETVAL_STRINGL(tmp, j, 1);
	efree(tmp);
}
/* }}} */

// copied from php_ticks.c
static void php_tick_iterator(void *data, void *arg TSRMLS_DC)
{
	void (*func)(int);

	memcpy(&func, data, sizeof(void(*)(int)));
	func(*((int *)arg));
}

// copied from php_ticks.c
void php_run_ticks(int count)
{
	TSRMLS_FETCH();
	zend_llist_apply_with_argument(&PG(tick_functions), (llist_apply_with_arg_func_t) php_tick_iterator, &count TSRMLS_CC);
}

#pragma managed

private ref class MutexContainer
{
public:
	static Object ^Mutex = gcnew Object();
};

static void block_interruptions()
{
	Monitor::Enter(MutexContainer::Mutex);	
}

static void unblock_interruptions()
{
	Monitor::Exit(MutexContainer::Mutex);
}

#pragma unmanaged

ZEND_API void (*zend_ticks_function)(int ticks) = php_run_ticks;
ZEND_API void (*zend_block_interruptions)(void) = block_interruptions;
ZEND_API void (*zend_unblock_interruptions)(void) = unblock_interruptions;

// copied from php_ticks.c
int php_startup_ticks(TSRMLS_D)
{
	zend_llist_init(&PG(tick_functions), sizeof(void(*)(int)), NULL, 1);
	return SUCCESS;
}

// copied from php_ticks.c
void php_shutdown_ticks(TSRMLS_D)
{
	zend_llist_destroy(&PG(tick_functions));
}

// copied from php_ticks.c and beautified
static int php_compare_tick_functions(void *elem1, void *elem2)
{
	void (*func1)(int);
	void (*func2)(int);
	memcpy(&func1, elem1, sizeof(void(*)(int)));
	memcpy(&func2, elem2, sizeof(void(*)(int)));
	return (func1 == func2);
}

// copied from php_ticks.c
ZEND_API void php_add_tick_function(void (*func)(int))
{
	TSRMLS_FETCH();
	zend_llist_add_element(&PG(tick_functions), (void *)&func);
}

// copied from php_ticks.c
ZEND_API void php_remove_tick_function(void (*func)(int))
{
	TSRMLS_FETCH();
	zend_llist_del_element(&PG(tick_functions), (void *)func,
						   (int(*)(void*, void*))php_compare_tick_functions);
}

// copied from main.c
ZEND_API void dummy_indent()
{
	zend_indent();
}

// copied from flock_compat.c
ZEND_API int flock(int fd, int operation)
{
	return php_flock(fd, operation);
}

// copied from flock_compat.c and beautified
ZEND_API int php_flock(int fd, int operation)
/*
 * Program:   Unix compatibility routines
 *
 * Author:  Mark Crispin
 *      Networks and Distributed Computing
 *      Computing & Communications
 *      University of Washington
 *      Administration Building, AG-44
 *      Seattle, WA  98195
 *      Internet: MRC@CAC.Washington.EDU
 *
 * Date:    14 September 1996
 * Last Edited: 14 August 1997
 *
 * Copyright 1997 by the University of Washington
 *
 *  Permission to use, copy, modify, and distribute this software and its
 * documentation for any purpose and without fee is hereby granted, provided
 * that the above copyright notice appears in all copies and that both the
 * above copyright notice and this permission notice appear in supporting
 * documentation, and that the name of the University of Washington not be
 * used in advertising or publicity pertaining to distribution of the software
 * without specific, written prior permission.  This software is made available
 * "as is", and
 * THE UNIVERSITY OF WASHINGTON DISCLAIMS ALL WARRANTIES, EXPRESS OR IMPLIED,
 * WITH REGARD TO THIS SOFTWARE, INCLUDING WITHOUT LIMITATION ALL IMPLIED * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE, AND IN
 * NO EVENT SHALL THE UNIVERSITY OF WASHINGTON BE LIABLE FOR ANY SPECIAL,
 * INDIRECT OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING FROM
 * LOSS OF USE, DATA OR PROFITS, WHETHER IN AN ACTION OF CONTRACT, TORT
 * (INCLUDING NEGLIGENCE) OR STRICT LIABILITY, ARISING OUT OF OR IN CONNECTION
 * WITH THE USE OR PERFORMANCE OF THIS SOFTWARE.
 *
 */
/*              DEDICATION

 *  This file is dedicated to my dog, Unix, also known as Yun-chan and
 * Unix J. Terwilliker Jehosophat Aloysius Monstrosity Animal Beast.  Unix
 * passed away at the age of 11 1/2 on September 14, 1996, 12:18 PM PDT, after
 * a two-month bout with cirrhosis of the liver.
 *
 *  He was a dear friend, and I miss him terribly.
 *
 *  Lift a leg, Yunie.  Luv ya forever!!!!
 */
{
    HANDLE hdl = (HANDLE)crtx_get_osfhandle(fd);
    DWORD low = 1, high = 0;
    OVERLAPPED offset = {0, 0, 0, 0, NULL};
    
	if (hdl < 0) return -1;              /* error in file descriptor */
    /* bug for bug compatible with Unix */
    
	UnlockFileEx(hdl, 0, low, high, &offset);
    switch (operation & ~LOCK_NB)	/* translate to LockFileEx() op */
	{
		case LOCK_EX:				/* exclusive */
            if (LockFileEx(hdl, LOCKFILE_EXCLUSIVE_LOCK + ((operation & LOCK_NB) ? LOCKFILE_FAIL_IMMEDIATELY : 0),
                           0, low, high, &offset))
			{
                return 0;
			}
            break;

        case LOCK_SH:				/* shared */
            if (LockFileEx(hdl, ((operation & LOCK_NB) ? LOCKFILE_FAIL_IMMEDIATELY : 0),
                           0, low, high, &offset))
			{            
				return 0;
			}
            break;

		case LOCK_UN:				/* unlock */
            return 0;			    /* always succeeds */

        default:					/* default */
            break;
    }
	/* Under Win32 MT library, errno is not a variable but a function call,
	 * which cannot be assigned to.
	 */
	// LP: Bullshit. errno is #defined as (*_errno()) so that it CAN be assigned to.
//#if !defined(PHP_WIN32)
    errno = EINVAL;					/* bad call */
//#endif
    return -1;
}

// copied from zend.c and modified
ZEND_API char *get_zend_version()
{
	return "Zend Engine v2.0.0-dev, Copyright (c) 1998-2003 Zend Technologies\nModified by the PHP.NET Team 2004\n";
}

// copied from time.c and beautified
static int getfilesystemtime(struct timeval *time_Info) 
{
	FILETIME ft;
	__int64 ff;

    GetSystemTimeAsFileTime(&ft);   /* 100 ns blocks since 01-Jan-1641 */
                                    /* resolution seems to be 0.01 sec */ 
    ff = *(__int64*)(&ft);
    time_Info->tv_sec = (int)(ff / (__int64)10000000 - (__int64)11644473600);
    time_Info->tv_usec = (int)(ff % 10000000) / 10;
    return 0;
}

#pragma managed

// copied from time.c and beautified
ZEND_API int gettimeofday(struct timeval *time_Info, struct timezone *timezone_Info)
{
	static struct timeval starttime = { 0, 0 };
	static __int64 lasttime = 0;
	static __int64 freq = 0;
	__int64 timer;
	LARGE_INTEGER li;
	BOOL b;
	double dt;

	/* Get the time, if they want it */
	if (time_Info != NULL)
	{
		if (starttime.tv_sec == 0)
		{
            b = QueryPerformanceFrequency(&li);
            if (!b) starttime.tv_sec = -1;
            else
			{
                freq = li.QuadPart;
                b = QueryPerformanceCounter(&li);
                if (!b) starttime.tv_sec = -1;
                else
				{
                    getfilesystemtime(&starttime);
                    timer = li.QuadPart;
                    dt = (double)timer / freq;
                    starttime.tv_usec -= (int)((dt - (int)dt) * 1000000);
                    if (starttime.tv_usec < 0)
					{
                        starttime.tv_usec += 1000000;
                        --starttime.tv_sec;
                    }
                    starttime.tv_sec -= (int)dt;
                }
            }
        }
        if (starttime.tv_sec > 0)
		{
            b = QueryPerformanceCounter(&li);
            if (!b) starttime.tv_sec = -1;
            else
			{
                timer = li.QuadPart;
                if (timer < lasttime)
				{
                    getfilesystemtime(time_Info);
                    dt = (double)timer / freq;
                    starttime = *time_Info;
                    starttime.tv_usec -= (int)((dt - (int)dt) * 1000000);
                    if (starttime.tv_usec < 0)
					{
                        starttime.tv_usec += 1000000;
                        --starttime.tv_sec;
                    }
                    starttime.tv_sec -= (int)dt;
                }
                else
				{
                    lasttime = timer;
                    dt = (double)timer / freq;
                    time_Info->tv_sec = starttime.tv_sec + (int)dt;
                    time_Info->tv_usec = starttime.tv_usec + (int)((dt - (int)dt) * 1000000);
                    if (time_Info->tv_usec > 1000000)
					{
                        time_Info->tv_usec -= 1000000;
                        ++time_Info->tv_sec;
                    }
                }
            }
        }
        if (starttime.tv_sec < 0) getfilesystemtime(time_Info);
	}
	/* Get the timezone, if they want it */
	if (timezone_Info != NULL)
	{
		TimeZone ^zone = TimeZone::CurrentTimeZone;
		DateTime now = DateTime::Now;

		timezone_Info->tz_minuteswest = (int)((zone->ToUniversalTime(now) - now).TotalMinutes);
		timezone_Info->tz_dsttime = zone->IsDaylightSavingTime(now) ? 1 : 0;
	}
	/* And return */
	return 0;
}

#pragma unmanaged

// copied from info.c, slightly modified and beautified
ZEND_API char *php_get_uname(char mode)
{
	char *php_uname;
	char tmp_uname[256];
	DWORD dwBuild = 0;
	DWORD dwVersion = GetVersion();
	DWORD dwWindowsMajorVersion = (DWORD)(LOBYTE(LOWORD(dwVersion)));
	DWORD dwWindowsMinorVersion = (DWORD)(HIBYTE(LOWORD(dwVersion)));
	DWORD dwSize = MAX_COMPUTERNAME_LENGTH + 1;
	char ComputerName[MAX_COMPUTERNAME_LENGTH + 1];
	SYSTEM_INFO SysInfo;

	GetComputerNameA(ComputerName, &dwSize);
	GetSystemInfo(&SysInfo);

	if (mode == 's')
	{
		if (dwVersion < 0x80000000) php_uname = "Windows NT";
		else php_uname = "Windows 9x";
	}
	else if (mode == 'r')
	{
		snprintf(tmp_uname, sizeof(tmp_uname), "%d.%d", dwWindowsMajorVersion, dwWindowsMinorVersion);
		php_uname = tmp_uname;
	}
	else if (mode == 'n') php_uname = ComputerName;
	else if (mode == 'v')
	{
		dwBuild = (DWORD)(HIWORD(dwVersion));
		snprintf(tmp_uname, sizeof(tmp_uname), "build %d", dwBuild);
		php_uname = tmp_uname;
	}
	else if (mode == 'm')
	{
		switch (SysInfo.wProcessorArchitecture)
		{
			case PROCESSOR_ARCHITECTURE_INTEL:
				snprintf(tmp_uname, sizeof(tmp_uname), "i%d", SysInfo.dwProcessorType);
				php_uname = tmp_uname;
				break;

			case PROCESSOR_ARCHITECTURE_MIPS:
				php_uname = "MIPS R4000";
				php_uname = tmp_uname;
				break;

			case PROCESSOR_ARCHITECTURE_ALPHA:
				snprintf(tmp_uname, sizeof(tmp_uname), "Alpha %d", SysInfo.wProcessorLevel);
				php_uname = tmp_uname;
				break;

			case PROCESSOR_ARCHITECTURE_PPC:
				snprintf(tmp_uname, sizeof(tmp_uname), "PPC 6%02d", SysInfo.wProcessorLevel);
				php_uname = tmp_uname;
				break;

			case PROCESSOR_ARCHITECTURE_IA64:
				php_uname = "IA64";
				break;

#if defined(PROCESSOR_ARCHITECTURE_IA32_ON_WIN64)
			case PROCESSOR_ARCHITECTURE_IA32_ON_WIN64:
				php_uname = "IA32";
				break;
#endif
#if defined(PROCESSOR_ARCHITECTURE_AMD64)
			case PROCESSOR_ARCHITECTURE_AMD64:
				php_uname = "AMD64";
				break;
#endif

			case PROCESSOR_ARCHITECTURE_UNKNOWN:
			default:
				php_uname = "Unknown";
				break;
		}
	}
	else
	{
		/* assume mode == 'a' */
		/* Get build numbers for Windows NT or Win95 */
		if (dwVersion < 0x80000000)
		{
			dwBuild = (DWORD)(HIWORD(dwVersion));
			snprintf(tmp_uname, sizeof(tmp_uname), "%s %s %d.%d build %d",
					 "Windows NT", ComputerName,
					 dwWindowsMajorVersion, dwWindowsMinorVersion, dwBuild);
		}
		else
		{
			snprintf(tmp_uname, sizeof(tmp_uname), "%s %s %d.%d",
					 "Windows 9x", ComputerName,
					 dwWindowsMajorVersion, dwWindowsMinorVersion);
		}
		php_uname = tmp_uname;
	}
	return estrdup(php_uname);
}

#pragma managed

// originally in mail.c
ZEND_API int php_mail(char *to, char *subject, char *message, char *headers, char *extra_cmd TSRMLS_DC)
{
#pragma warning (disable: 4996)
	return PHP::Library::Mailer::Mail(gcnew String(to), gcnew String(subject), gcnew String(message),
		gcnew String(headers));
#pragma warning (default: 4996)
}

private ref class MiscMutexContainer
{
public:
	static Object ^Php_ctime_r_Mutex = gcnew Object();
	static Object ^Php_asctime_r_Mutex = gcnew Object();
	static Object ^Php_gmtime_r_Mutex = gcnew Object();
	static Object ^Php_localtime_r_Mutex = gcnew Object();
};

// copied from reentrancy.c and modified
ZEND_API char *php_ctime_r(const time_t *clock, char *buf)
{
	char *tmp;
	
	Threading::Monitor::Enter(MiscMutexContainer::Php_ctime_r_Mutex);
	try
	{
		tmp = ctime(clock);
		strcpy(buf, tmp);
	}
	finally
	{
		Threading::Monitor::Exit(MiscMutexContainer::Php_ctime_r_Mutex);
	}
	return buf;
}

// copied from reentrancy.c and modified
ZEND_API char *php_asctime_r(const struct tm *tm, char *buf)
{
	char *tmp;
	
	Threading::Monitor::Enter(MiscMutexContainer::Php_asctime_r_Mutex);
	try
	{
		tmp = asctime(tm);
		strcpy(buf, tmp);
	}
	finally
	{
		Threading::Monitor::Exit(MiscMutexContainer::Php_asctime_r_Mutex);
	}
	return buf;
}

// copied from reentrancy.c and modified
ZEND_API struct tm *php_gmtime_r(const time_t *const timep, struct tm *p_tm)
{
	struct tm *tmp;
	
	Threading::Monitor::Enter(MiscMutexContainer::Php_gmtime_r_Mutex);
	try
	{
		tmp = gmtime(timep);
		if (tmp)
		{
			memcpy(p_tm, tmp, sizeof(struct tm));
			tmp = p_tm;
		}
	}
	finally
	{
		Threading::Monitor::Exit(MiscMutexContainer::Php_gmtime_r_Mutex);
	}
	return tmp;
}

// copied from reentrancy.c and modified
ZEND_API struct tm *php_localtime_r(const time_t *const timep, struct tm *p_tm)
{
	struct tm *tmp;
	
	Threading::Monitor::Enter(MiscMutexContainer::Php_localtime_r_Mutex);
	try
	{
		tmp = localtime(timep);
		if (tmp)
		{
			memcpy(p_tm, tmp, sizeof(struct tm));
			tmp = p_tm;
		}
	}
	finally
	{
		Threading::Monitor::Exit(MiscMutexContainer::Php_localtime_r_Mutex);
	}
	return tmp;
}

#pragma unmanaged

// copied from reentrancy.c and beautified
/*
 * Copyright (c) 1998 Softweyr LLC.  All rights reserved.
 *
 * strtok_r, from Berkeley strtok
 * Oct 13, 1998 by Wes Peters <wes@softweyr.com>
 *
 * Copyright (c) 1988, 1993
 *	The Regents of the University of California.  All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions
 * are met:
 *
 * 1. Redistributions of source code must retain the above copyright
 *    notices, this list of conditions and the following disclaimer.
 * 
 * 2. Redistributions in binary form must reproduce the above copyright
 *    notices, this list of conditions and the following disclaimer in the
 *    documentation and/or other materials provided with the distribution.
 * 
 * 3. All advertising materials mentioning features or use of this software
 *    must display the following acknowledgement:
 *
 *	This product includes software developed by Softweyr LLC, the
 *      University of California, Berkeley, and its contributors.
 *
 * 4. Neither the name of the University nor the names of its contributors
 *    may be used to endorse or promote products derived from this software
 *    without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY SOFTWEYR LLC, THE REGENTS AND CONTRIBUTORS
 * ``AS IS'' AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
 * LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A
 * PARTICULAR PURPOSE ARE DISCLAIMED.  IN NO EVENT SHALL SOFTWEYR LLC, THE
 * REGENTS, OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED
 * TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
 * PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
 * LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
 * NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

ZEND_API char *php_strtok_r(char *s, const char *delim, char **last)
{
    char *spanp;
    int c, sc;
    char *tok;

    if (s == NULL && (s = *last) == NULL) return NULL;

    /*
     * Skip (span) leading delimiters (s += strspn(s, delim), sort of).
     */
cont:
    c = *s++;
    for (spanp = (char *)delim; (sc = *spanp++) != 0;)
    {
		if (c == sc) goto cont;
    }

    if (c == 0)		/* no non-delimiter characters */
    {
		*last = NULL;
		return NULL;
    }
    tok = s - 1;

    /*
     * Scan token (scan for delimiters: s += strcspn(s, delim), sort of).
     * Note that delim must have one NUL; we stop if we see that, too.
     */
    for (;;)
    {
		c = *s++;
		spanp = (char *)delim;
		do
		{
			if ((sc = *spanp++) == c)
			{
				if (c == 0)
				{
					s = NULL;
				}
				else
				{
					char *w = s - 1;
					*w = '\0';
				}
				*last = s;
				return tok;
			}
		}
		while (sc != 0);
    }
    /* NOTREACHED */
}

// originally in zend_execute_API.c
ZEND_API void zend_timeout(int dummy)
{
	// do nothing
}

// originally in zend_execute_API.c
ZEND_API void zend_set_timeout(long seconds)
{
	// do nothing
}

// originally in zend_execute_API.c
ZEND_API void zend_unset_timeout(TSRMLS_D)
{
	// do nothing
}

// originally in zend_alloc.c
ZEND_API int zend_set_memory_limit(unsigned int memory_limit)
{
	// do nothing
	return SUCCESS;
}

#define sign(n) ((n) < 0 ? -1 : ((n) > 0 ? 1 : 0))

typedef struct
{
	const char *name;
	int order;
}
special_forms_t;

// copied from versioning.c and beautified
static int compare_special_version_forms(char *form1, char *form2)
{
	int found1 = -1, found2 = -1;
	special_forms_t special_forms[10] =
	{	{"dev", 0},		{"alpha", 1},		{"a", 1},		{"beta", 2},		{"b", 2},
		{"RC", 3},		{"#", 4},			{"pl", 5},		{"p", 5},			{NULL, 0} };

	special_forms_t *pp;

	for (pp = special_forms; pp && pp->name; pp++)
	{
		if (strncmp(form1, pp->name, strlen(pp->name)) == 0)
		{
			found1 = pp->order;
			break;
		}
	}
	for (pp = special_forms; pp && pp->name; pp++)
	{
		if (strncmp(form2, pp->name, strlen(pp->name)) == 0)
		{
			found2 = pp->order;
			break;
		}
	}
	return sign(found1 - found2);
}

// copied from versioning.c and beautified
ZEND_API int php_version_compare(const char *orig_ver1, const char *orig_ver2)
{
	char *p1, *p2, *n1, *n2, *ver1, *ver2;
	int compare = 0;
	long l1, l2;

	if (!*orig_ver1 || !*orig_ver2)
	{
		if (!*orig_ver1 && !*orig_ver2) return 0;
		else return *orig_ver1 ? 1 : -1;
	}

	if (orig_ver1[0] == '#') ver1 = estrdup(orig_ver1);
	else ver1 = php_canonicalize_version(orig_ver1);

	if (orig_ver2[0] == '#') ver2 = estrdup(orig_ver2);
	else ver2 = php_canonicalize_version(orig_ver2);

	p1 = n1 = ver1;
	p2 = n2 = ver2;
	while (*p1 && *p2 && n1 && n2)
	{
		if ((n1 = strchr(p1, '.')) != NULL) *n1 = '\0';
		if ((n2 = strchr(p2, '.')) != NULL) *n2 = '\0';

		if (isdigit(*p1) && isdigit(*p2))
		{
			/* compare element numerically */
			l1 = strtol(p1, NULL, 10);
			l2 = strtol(p2, NULL, 10);
			compare = sign(l1 - l2);
		}
		else if (!isdigit(*p1) && !isdigit(*p2))
		{
			/* compare element names */
			compare = compare_special_version_forms(p1, p2);
		}
		else
		{
			/* mix of names and numbers */
			if (isdigit(*p1)) compare = compare_special_version_forms("#N#", p2);
			else compare = compare_special_version_forms(p1, "#N#");
		}
		if (compare != 0) break;

		if (n1 != NULL) p1 = n1 + 1;
		if (n2 != NULL) p2 = n2 + 1;
	}
	if (compare == 0)
	{
		if (n1 != NULL)
		{
			if (isdigit(*p1)) compare = 1;
			else compare = php_version_compare(p1, "#N#");
		}
		else if (n2 != NULL)
		{
			if (isdigit(*p2)) compare = -1;
			else compare = php_version_compare("#N#", p2);
		}
	}
	efree(ver1);
	efree(ver2);
	return compare;
}

// copied from versioning.c and beautified
ZEND_API char *php_canonicalize_version(const char *version)
{
    int len = strlen(version);
    char *buf = (char *)emalloc(len * 2 + 1), *q, lp, lq;
    const char *p;

    if (len == 0)
	{
        *buf = '\0';
        return buf;
    }

    p = version;
    q = buf;
    *q++ = lp = *p++;
    lq = '\0';
    while (*p) {
/*  s/[-_+]/./g;
 *  s/([^\d\.])([^\D\.])/$1.$2/g;
 *  s/([^\D\.])([^\d\.])/$1.$2/g;
 */
#define isdig(x) (isdigit(x)&&(x)!='.')
#define isndig(x) (!isdigit(x)&&(x)!='.')
#define isspecialver(x) ((x)=='-'||(x)=='_'||(x)=='+')

        lq = *(q - 1);
		if (isspecialver(*p))
		{
			if (lq != '.') lq = *q++ = '.';
		}
		else if ((isndig(lp) && isdig(*p)) || (isdig(lp) && isndig(*p)))
		{
			if (lq != '.') *q++ = '.';
			lq = *q++ = *p;
		}
		else if (!isalnum(*p))
		{
			if (lq != '.') lq = *q++ = '.';
		}
		else lq = *q++ = *p;
		lp = *p++;
    }
    *q++ = '\0';
    return buf;
}

// copied from php_sockets_win.c and beautified
int inet_aton(const char *cp, struct in_addr *inp)
{
	inp->s_addr = inet_addr(cp);
	if (inp->s_addr == INADDR_NONE)  return 0;

	return 1;
}

// copied from fsock.c and beautified
/*
 * Converts a host name to an IP address.
 * T-O-D-O: This looks like unused code suitable for nuking.
 */
ZEND_API int php_lookup_hostname(const char *addr, struct in_addr *in)
{
	struct hostent *host_info;

	if (!inet_aton(addr, in))
	{
		/* XXX NOT THREAD SAFE */
		host_info = gethostbyname(addr);
		if (host_info == 0)
		{
			/* Error: unknown host */
			return -1;
		}
		*in = *((struct in_addr *)host_info->h_addr);
	}
	return 0;
}

// copied from file.c and beautified
ZEND_API int php_copy_file(char *src, char *dest TSRMLS_DC)
{
	php_stream *srcstream = NULL, *deststream = NULL;
	int ret = FAILURE;

	srcstream = php_stream_open_wrapper(src, "rb", STREAM_DISABLE_OPEN_BASEDIR | REPORT_ERRORS, NULL);
	if (!srcstream) return ret;

	deststream = php_stream_open_wrapper(dest, "wb", ENFORCE_SAFE_MODE | REPORT_ERRORS, NULL);

	if (srcstream && deststream)
	{
		ret = php_stream_copy_to_stream(srcstream, deststream, PHP_STREAM_COPY_ALL) == 0 ? FAILURE : SUCCESS;
	}

	if (srcstream) php_stream_close(srcstream);
	if (deststream) php_stream_close(deststream);

	return ret;
}


// copied from file.c
/* DEPRECATED APIs: Use php_stream_mkdir() instead */
ZEND_API int php_mkdir_ex(char *dir, long mode, int options TSRMLS_DC)
{
	int ret;

	if (PG(safe_mode) && (!php_checkuid(dir, NULL, CHECKUID_CHECK_FILE_AND_DIR))) {
		return -1;
	}

	if (php_check_open_basedir(dir TSRMLS_CC)) {
		return -1;
	}

	if ((ret = VCWD_MKDIR(dir, (mode_t)mode)) < 0 && (options & REPORT_ERRORS)) {
		php_error_docref(NULL TSRMLS_CC, E_WARNING, "%s", strerror(errno));
	}

	return ret;
}

ZEND_API int php_mkdir(char *dir, long mode TSRMLS_DC)
{
	return php_mkdir_ex(dir, mode, REPORT_ERRORS TSRMLS_CC);
}

/* }}} */
// copied from lcg.c and beautified
void lcg_seed(TSRMLS_D)
{
	struct timeval tv;

	if (gettimeofday(&tv, NULL) == 0) LCG(s1) = tv.tv_sec ^ (~tv.tv_usec);
	else LCG(s1) = 1;

	LCG(s2) = (long) tsrm_thread_id();
	LCG(seeded) = 1;
}

#define MODMULT(a, b, c, m, s) q = s / a; s = b * (s - a * q) - c * q; if (s < 0) s += m

// copied from lcg.c and beautified
ZEND_API double php_combined_lcg(TSRMLS_D)
{
	php_int32 q;
	php_int32 z;
	
	if (!LCG(seeded)) lcg_seed(TSRMLS_C);

	MODMULT(53668, 40014, 12211, 2147483563L, LCG(s1));
	MODMULT(52774, 40692, 3791, 2147483399L, LCG(s2));

	z = LCG(s1) - LCG(s2);
	if (z < 1) z += 2147483562;

	return z * 4.656613e-10;
}

ZEND_API char *_xml_zval_strdup(zval *val)
{
	if (Z_TYPE_P(val) == IS_STRING)
	{
		char *buf = (char *)emalloc(Z_STRLEN_P(val) + 1);
		memcpy(buf, Z_STRVAL_P(val), Z_STRLEN_P(val));
		buf[Z_STRLEN_P(val)] = '\0';
		return buf;
	}
	return NULL;
}

ZEND_API int php_sprintf (char*s, const char* format, ...)
{
	va_list args;
	int ret;

	va_start(args, format);
	s[0] = '\0';
	ret = vsprintf(s, format, args);
	va_end(args);
	if (!ret) return -1;
	return strlen(s);
}

// copied from exec.c and beautified
ZEND_API char *php_escape_shell_cmd(char *str)
{
	register int x, y, l;
	char *cmd;
	char *p = NULL;

	l = strlen(str);
	cmd = (char *)emalloc(2 * l + 1);
	
	for (x = 0, y = 0; x < l; x++)
	{
		switch (str[x])
		{
			case '"':
			case '\'':
#ifndef PHP_WIN32
				if (!p && (p = memchr(str + x + 1, str[x], l - x - 1)))
				{
					/* noop */
				}
				else if (p && *p == str[x]) p = NULL;
				else cmd[y++] = '\\';
				cmd[y++] = str[x];
				break;
#endif
			case '#': /* This is character-set independent */
			case '&':
			case ';':
			case '`':
			case '|':
			case '*':
			case '?':
			case '~':
			case '<':
			case '>':
			case '^':
			case '(':
			case ')':
			case '[':
			case ']':
			case '{':
			case '}':
			case '$':
			case '\\':
			case '\x0A': /* excluding these two */
			case '\xFF':
#ifdef PHP_WIN32
			/* since Windows does not allow us to escape these chars, just remove them */
			case '%':
				cmd[y++] = ' ';
				break;
#endif
				cmd[y++] = '\\';
				/* fall-through */
			default:
				cmd[y++] = str[x];

		}
	}
	cmd[y] = '\0';
	return cmd;
}
/* }}} */

/* {{{ php_escape_shell_arg
 */
ZEND_API char *php_escape_shell_arg(char *str)
{
	int x, y, l;
	char *cmd;

	y = 0;
	l = strlen(str);
	
	cmd = (char *)emalloc(4 * l + 3); /* worst case */
#ifdef PHP_WIN32
	cmd[y++] = '"';
#else
	cmd[y++] = '\'';
#endif
	
	for (x = 0; x < l; x++)
	{
		switch (str[x])
		{
#ifdef PHP_WIN32
		case '"':
		case '%':
			cmd[y++] = ' ';
			break;
#else
		case '\'':
			cmd[y++] = '\'';
			cmd[y++] = '\\';
			cmd[y++] = '\'';
#endif
			/* fall-through */
		default:
			cmd[y++] = str[x];
		}
	}
#ifdef PHP_WIN32
	cmd[y++] = '"';
#else
	cmd[y++] = '\'';
#endif
	cmd[y] = '\0';
	return cmd;
}
/* }}} */
