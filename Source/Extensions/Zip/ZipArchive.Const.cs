using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PHP.Core;

namespace PHP.Library.Zip
{
    partial class ZipArchive
    {
        [ImplementsConstant("CREATE")]
        public const int CREATE = 1;
        [ImplementsConstant("OVERWRITE")]
        public const int OVERWRITE = 2;
        [ImplementsConstant("EXCL")]
        public const int EXCL = 4;
        [ImplementsConstant("CHECKCONS")]
        public const int CHECKCONS = 8;
        [ImplementsConstant("FL_NOCASE")]
        public const int FL_NOCASE = 16;
        [ImplementsConstant("FL_NODIR")]
        public const int FL_NODIR = 32;
        [ImplementsConstant("FL_COMPRESSED")]
        public const int FL_COMPRESSED = 64;
        [ImplementsConstant("FL_UNCHANGED")]
        public const int FL_UNCHANGED = 128;
        [ImplementsConstant("CM_DEFAULT")]
        public const int CM_DEFAULT = 0;
        [ImplementsConstant("CM_STORE")]
        public const int CM_STORE = 1;
        [ImplementsConstant("CM_SHRINK")]
        public const int CM_SHRINK = 2;
        [ImplementsConstant("CM_REDUCE_1")]
        public const int CM_REDUCE_1 = 3;
        [ImplementsConstant("CM_REDUCE_2")]
        public const int CM_REDUCE_2 = 4;
        [ImplementsConstant("CM_REDUCE_3")]
        public const int CM_REDUCE_3 = 5;
        [ImplementsConstant("CM_REDUCE_4")]
        public const int CM_REDUCE_4 = 6;
        [ImplementsConstant("CM_IMPLODE")]
        public const int CM_IMPLODE = 7;
        [ImplementsConstant("CM_DEFLATE")]
        public const int CM_DEFLATE = 8;
        [ImplementsConstant("CM_DEFLATE64")]
        public const int CM_DEFLATE64 = 9;
        [ImplementsConstant("CM_PKWARE_IMPLODE")]
        public const int CM_PKWARE_IMPLODE = 10;
        [ImplementsConstant("CRCREATEEATE")]
        public const int CM_BZIP2 = 11;

        [ImplementsConstant("ER_CLOSE")]
        public const int ER_CLOSE = 3;  /* S Closing zip archive failed */
        [ImplementsConstant("ER_SEEK")]
        public const int ER_SEEK = 4;  /* S Seek error */
        [ImplementsConstant("ER_READ")]
        public const int ER_READ = 5;  /* S Read error */
        [ImplementsConstant("ER_WRITE")]
        public const int ER_WRITE = 6;  /* S Write error */
        [ImplementsConstant("ER_CRC")]
        public const int ER_CRC = 7;  /* N CRC error */
        [ImplementsConstant("ER_ZIPCLOSED")]
        public const int ER_ZIPCLOSED = 8;  /* N Containing zip archive was closed */
        [ImplementsConstant("ER_NOENT")]
        public const int ER_NOENT = 9;  /* N No such file */
        [ImplementsConstant("ER_EXISTS")]
        public const int ER_EXISTS = 10;  /* N File already exists */
        [ImplementsConstant("ER_OPEN")]
        public const int ER_OPEN = 11;  /* S Can't open file */
        [ImplementsConstant("ER_TMPOPEN")]
        public const int ER_TMPOPEN = 12;  /* S Failure to create temporary file */
        [ImplementsConstant("ER_ZLIB")]
        public const int ER_ZLIB = 13;  /* Z Zlib error */
        [ImplementsConstant("ER_MEMORY")]
        public const int ER_MEMORY = 14;  /* N Malloc failure */
        [ImplementsConstant("ER_CHANGED")]
        public const int ER_CHANGED = 15;  /* N Entry has been changed */
        [ImplementsConstant("ER_COMPNOTSUPP")]
        public const int ER_COMPNOTSUPP = 16;  /* N Compression method not supported */
        [ImplementsConstant("ER_EOF")]
        public const int ER_EOF = 17;  /* N Premature EOF */
        [ImplementsConstant("ER_INVAL")]
        public const int ER_INVAL = 18;  /* N Invalid argument */
        [ImplementsConstant("ER_NOZIP")]
        public const int ER_NOZIP = 19;  /* N Not a zip archive */
        [ImplementsConstant("ER_INTERNAL")]
        public const int ER_INTERNAL = 20;  /* N Internal error */
        [ImplementsConstant("ER_INCONS")]
        public const int ER_INCONS = 21;  /* N Zip archive inconsistent */
        [ImplementsConstant("ER_REMOVE")]
        public const int ER_REMOVE = 22;  /* S Can't remove file */
        [ImplementsConstant("ER_DELETED")]
        public const int ER_DELETED = 23;  /* N Entry has been deleted */
    }
}
