using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PHP.Core;

namespace PHP.Library.Zip
{
    public static class ZipStatic
    {
        [ImplementsFunction("zip_close")]
        public static void zip_close(object zip)
        {
            throw new NotImplementedException();
        }

        [ImplementsFunction("zip_entry_close")]
        public static bool zip_entry_close(object zip_entry)
        {
            throw new NotImplementedException();
        }

        [ImplementsFunction("zip_entry_compressedsize")]
        public static int zip_entry_compressedsize(object zip_entry)
        {
            throw new NotImplementedException();
        }

        [ImplementsFunction("zip_entry_compressionmethod")]
        public static string zip_entry_compressionmethod(object zip_entry)
        {
            throw new NotImplementedException();
        }

        [ImplementsFunction("zip_entry_filesize")]
        public static int zip_entry_filesize(object zip_entry)
        {
            throw new NotImplementedException();
        }

        [ImplementsFunction("zip_entry_name")]
        public static string zip_entry_name(object zip_entry)
        {
            throw new NotImplementedException();
        }

        [ImplementsFunction("zip_entry_open")]
        public static bool zip_entry_open(object zip, object zip_entry)
        {
            return zip_entry_open(zip, zip_entry, null);
        }

        [ImplementsFunction("zip_entry_open")]
        public static bool zip_entry_open(object zip, object zip_entry, string mode)
        {
            throw new NotImplementedException();
        }

        [ImplementsFunction("zip_entry_read")]
        public static string zip_entry_read(object zip_entry)
        {
            return zip_entry_read(zip_entry, 0);
        }

        [ImplementsFunction("zip_entry_read")]
        public static string zip_entry_read(object zip_entry, int length)
        {
            throw new NotImplementedException();
        }

        [ImplementsFunction("zip_open")]
        public static object zip_open(string filename)
        {
            throw new NotImplementedException();
        }

        [ImplementsFunction("zip_read")]
        public static object zip_read(object zip)
        {
            throw new NotImplementedException();
        }

        #region const
        [ImplementsConstant("ZIP_ER_OK")]
        public const int ZIP_ER_OK = 0;  /* N No error */
        [ImplementsConstant("ZIP_ER_MULTIDISK")]
        public const int ZIP_ER_MULTIDISK = 1;  /* N Multi-disk zip archives not supported */
        [ImplementsConstant("ZIP_ER_RENAME")]
        public const int ZIP_ER_RENAME = 2;  /* S Renaming temporary file failed */
        [ImplementsConstant("ZIP_ER_CLOSE")]
        public const int ZIP_ER_CLOSE = 3;  /* S Closing zip archive failed */
        [ImplementsConstant("ZIP_ER_SEEK")]
        public const int ZIP_ER_SEEK = 4;  /* S Seek error */
        [ImplementsConstant("ZIP_ER_READ")]
        public const int ZIP_ER_READ = 5;  /* S Read error */
        [ImplementsConstant("ZIP_ER_WRITE")]
        public const int ZIP_ER_WRITE = 6;  /* S Write error */
        [ImplementsConstant("ZIP_ER_CRC")]
        public const int ZIP_ER_CRC = 7;  /* N CRC error */
        [ImplementsConstant("ZIP_ER_ZIPCLOSED")]
        public const int ZIP_ER_ZIPCLOSED = 8;  /* N Containing zip archive was closed */
        [ImplementsConstant("ZIP_ER_NOENT")]
        public const int ZIP_ER_NOENT = 9;  /* N No such file */
        [ImplementsConstant("ZIP_ER_EXISTS")]
        public const int ZIP_ER_EXISTS = 10;  /* N File already exists */
        [ImplementsConstant("ZIP_ER_OPEN")]
        public const int ZIP_ER_OPEN = 11;  /* S Can't open file */
        [ImplementsConstant("ZIP_ER_TMPOPEN")]
        public const int ZIP_ER_TMPOPEN = 12;  /* S Failure to create temporary file */
        [ImplementsConstant("ZIP_ER_ZLIB")]
        public const int ZIP_ER_ZLIB = 13;  /* Z Zlib error */
        [ImplementsConstant("ZIP_ER_MEMORY")]
        public const int ZIP_ER_MEMORY = 14;  /* N Malloc failure */
        [ImplementsConstant("ZIP_ER_CHANGED")]
        public const int ZIP_ER_CHANGED = 15;  /* N Entry has been changed */
        [ImplementsConstant("ZIP_ER_COMPNOTSUPP")]
        public const int ZIP_ER_COMPNOTSUPP = 16;  /* N Compression method not supported */
        [ImplementsConstant("ZIP_ER_EOF")]
        public const int ZIP_ER_EOF = 17;  /* N Premature EOF */
        [ImplementsConstant("ZIP_ER_INVAL")]
        public const int ZIP_ER_INVAL = 18;  /* N Invalid argument */
        [ImplementsConstant("ZIP_ER_NOZIP")]
        public const int ZIP_ER_NOZIP = 19;  /* N Not a zip archive */
        [ImplementsConstant("ZIP_ER_INTERNAL")]
        public const int ZIP_ER_INTERNAL = 20;  /* N Internal error */
        [ImplementsConstant("ZIP_ER_INCONS")]
        public const int ZIP_ER_INCONS = 21;  /* N Zip archive inconsistent */
        [ImplementsConstant("ZIP_ER_REMOVE")]
        public const int ZIP_ER_REMOVE = 22;  /* S Can't remove file */
        [ImplementsConstant("ZIP_ER_DELETED")]
        public const int ZIP_ER_DELETED = 23;  /* N Entry has been deleted */
        #endregion
    }
}
