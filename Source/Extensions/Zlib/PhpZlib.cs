/*

 Copyright (c) 2005-2011 DEVSENSE s.r.o.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Data;
using System.Collections;
using System.Text;
using System.Data.SqlClient;

using PHP.Core;
using System.Collections.Generic;

using ComponentAce.Compression.Libs.zlib;

namespace PHP.Library.Zlib
{
    [Serializable]
    public class PhpZlibResource : PhpResource
    {
        public PhpZlibResource()
            :base("zlib") // TODO
        { }
    }

	/// <summary>
    /// Implements PHP functions provided by multi-byte-string extension.
	/// </summary>
    public static class Zlib
	{
        /// <summary>
        /// Zlib force* constants.
        /// </summary>
        public enum ForceConstants
        {
            [ImplementsConstant("FORCE_GZIP")]
            FORCE_GZIP = 1,

            [ImplementsConstant("FORCE_DEFLATE")]
            FORCE_DEFLATE = 2,
        }

        internal static readonly byte[] GZIP_HEADER = new byte[] { 0x1f, 0x8b};
        internal const byte GZIP_HEADER_EXTRAFIELD = 4;
        internal const byte GZIP_HEADER_FILENAME = 8;
        internal const byte GZIP_HEADER_COMMENT = 16;
        internal const byte GZIP_HEADER_CRC = 2;
        internal const byte Z_DEFLATED = 8;
        internal const byte GZIP_HEADER_RESERVED_FLAGS = 0xe0;
        internal const byte OS_CODE = 0x03;
        internal const int MAX_WBITS = 15;
        internal const int PHP_ZLIB_MODIFIER = 100;

        internal const int GZIP_HEADER_LENGTH = 10;
        internal const int GZIP_FOOTER_LENGTH = 8;

        internal static string zError(int status)
        {
            return Deflate.z_errmsg[zlibConst.Z_NEED_DICT - status];
        }

        #region gzclose, gzopen

        /// <summary>
        /// Closes the given gz-file pointer.
        /// </summary>
        /// <param name="zp">The gz-file pointer. It must be valid, and must point to a file successfully opened by gzopen().</param>
        /// <returns>Returns TRUE on success or FALSE on failure.</returns>
        [ImplementsFunction("gzclose")]
        public static bool GzClose(PhpResource zp)
        {
            return PhpFile.Close(zp);
        }

        /// <summary>
        /// Opens a gzip (.gz) file for reading or writing. 
        /// </summary> 
        /// <param name="filename">The file name.</param>
        /// <param name="mode">
        ///     As in fopen() (rb or wb) but can also include a compression level (wb9) or a strategy: f for filtered data as
        ///     in wb6f, h for Huffman only compression as in wb1h.
        /// </param>
        /// <returns>
        ///     <para>Returns a file pointer to the file opened, after that, everything you read from this file descriptor will be 
        ///     transparently decompressed and what you write gets compressed.</para>
        ///     <para>If the open fails, the function returns FALSE.</para>
        /// </returns>
        [ImplementsFunction("gzopen")]
        public static PhpResource GzOpen(string filename, string mode)
        {
            return GzOpen(filename, mode, 0);
        }

        /// <summary>
        /// Opens a gzip (.gz) file for reading or writing.
        /// </summary>
        /// <param name="filename">The file name.</param>
        /// <param name="mode">
        ///     As in fopen() (rb or wb) but can also include a compression level (wb9) or a strategy: f for filtered data as
        ///     in wb6f, h for Huffman only compression as in wb1h.
        /// </param>
        /// <param name="use_include_path">
        ///     You can set this optional parameter to 1, if you want to search for the file in the include_path too.
        /// </param>
        /// <returns>
        ///     <para>Returns a file pointer to the file opened, after that, everything you read from this file descriptor will be 
        ///     transparently decompressed and what you write gets compressed.</para>
        ///     <para>If the open fails, the function returns FALSE.</para>
        /// </returns>
        [ImplementsFunction("gzopen")]
        public static PhpResource GzOpen(string filename, string mode, int use_include_path)
        {
            return new ZlibStreamWrapper().Open(
                ref filename,
                mode,
                use_include_path == 1 ? StreamOpenOptions.UseIncludePath : StreamOpenOptions.Empty,
                null);
        }

        #endregion

        #region gzcompress, gzuncompress

        /// <summary>
        /// This function compresses the given string using the ZLIB data format.
        /// </summary>
        /// <param name="data">The data to compress.</param>
        /// <returns>The compressed string or FALSE if an error occurred.</returns>
        [ImplementsFunction("gzcompress")]
        [return: CastToFalse]
        public static PhpBytes GzCompress(PhpBytes data)
        {
            return GzCompress(data, -1);
        }

        /// <summary>
        /// This function compress the given string using the ZLIB data format.
        /// </summary> 
        /// <param name="data">The data to compress.</param>
        /// <param name="level">The level of compression. Can be given as 0 for no compression up to 9 for maximum compression.</param>
        /// <returns>The compressed string or FALSE if an error occurred.</returns>
        [ImplementsFunction("gzcompress")]
        [return: CastToFalse]
        public static PhpBytes GzCompress(PhpBytes data, int level)
        {
            if ((level < -1) || (level > 9)) {
		        PhpException.Throw(PhpError.Warning, String.Format("compression level ({0}) must be within -1..9", level));
		        return null;
	        }

            int length_bound = data.Length + (data.Length / PHP_ZLIB_MODIFIER) + 15 + 1;

            byte[] output;

            try
            {
                output = new byte[length_bound];
            }
            catch (OutOfMemoryException)
            {
                return null;
            }

            int status;

            status = ZlibCompress(ref output, data.ReadonlyData, level);

            if (status == zlibConst.Z_OK)
            {
                return new PhpBytes(output);
            }
            else
            {
                PhpException.Throw(PhpError.Warning, zError(status));
                return null;
            }
        }

        /// <summary>
        /// This function uncompress a compressed string.
        /// </summary>
        /// <param name="data">The data compressed by gzcompress().</param>
        /// <returns>
        ///     <para>
        ///         The original uncompressed data or FALSE on error.
        ///     </para>
        ///     <para>
        ///         The function will return an error if the uncompressed data is more than 32768 times the length of the compressed
        ///         input data or more than the optional parameter length.
        ///     </para>
        /// </returns>
        [ImplementsFunction("gzuncompress")]
        [return: CastToFalse]
        public static PhpBytes GzUncompress(PhpBytes data)
        {
            return GzUncompress(data, 0);
        }

        /// <summary>
        /// This function uncompress a compressed string.
        /// </summary>
        /// <param name="data">The data compressed by gzcompress().</param>
        /// <param name="length">The maximum length of data to decode.</param>
        /// <returns>
        ///     <para>
        ///         The original uncompressed data or FALSE on error.
        ///     </para>
        ///     <para>
        ///         The function will return an error if the uncompressed data is more than 32768 times the length of the compressed
        ///         input data or more than the optional parameter length.
        ///     </para>
        /// </returns>
        [ImplementsFunction("gzuncompress")]
        [return:CastToFalse]
        public static PhpBytes GzUncompress(PhpBytes data, int length)
        {
            if (length   < 0)
            {
		        PhpException.Throw(PhpError.Warning, String.Format("length {0} must be greater or equal zero", length));
                return null;
	        }

            int ilength;
            int factor = 1, maxfactor = 16;
            byte[] output;
            int status;

            do
            {
                ilength = length != 0 ? length : (data.Length * (1 << factor++));

                try
                {
                    output = new byte[ilength];
                }
                catch (OutOfMemoryException)
                {
                    return null;
                }

                status = ZlibUncompress(ref output, data.ReadonlyData);
            }
            while ((status == zlibConst.Z_BUF_ERROR) && (length == 0) && (factor < maxfactor));

            if (status == zlibConst.Z_OK)
            {
                return new PhpBytes(output);
            }
            else
            {
                PhpException.Throw(PhpError.Warning, zError(status));
                return null;
            }
        }

        /// <summary>
        /// Reimplements function from zlib (compress2) that is not present in ZLIB.NET.
        /// </summary>
        /// <param name="dest">Destination array of bytes. May be trimmed if necessary.</param>
        /// <param name="source">Source array of bytes.</param>
        /// <param name="level">Level of compression.</param>
        /// <returns>Zlib status code.</returns>
        private static int ZlibCompress(ref byte[] dest, byte[] source, int level)
        {
            ZStream stream = new ZStream();
            int err;

            stream.next_in = source;
            stream.avail_in = source.Length;
            stream.next_out = dest;
            stream.avail_out = dest.Length;

            err = stream.deflateInit(level);
            if (err != zlibConst.Z_OK) return err;

            err = stream.deflate(zlibConst.Z_FINISH);
            if (err != zlibConst.Z_STREAM_END)
            {
                stream.deflateEnd();
                return err == zlibConst.Z_OK ? zlibConst.Z_BUF_ERROR : err;
            }

            if (stream.total_out != dest.Length)
            {
                byte[] output = new byte[stream.total_out];
                Buffer.BlockCopy(stream.next_out, 0, output, 0, (int)stream.total_out);
                dest = output;
            }

            return stream.deflateEnd();
        }

        /// <summary>
        /// Reimplements function from zlib (uncompress) that is not present in ZLIB.NET.
        /// </summary>
        /// <param name="dest">Destination array of bytes. May be trimmed if necessary.</param>
        /// <param name="source">Source array of bytes.</param>
        /// <returns>Zlib status code.</returns>
        private static int ZlibUncompress(ref byte[] dest, byte[] source)
        {
            ZStream stream = new ZStream();
            int err;

            stream.next_in = source;
            stream.avail_in = source.Length;
            stream.next_out = dest;
            stream.avail_out = dest.Length;

            err = stream.inflateInit();
            if (err != zlibConst.Z_OK) return err;

            err = stream.inflate(zlibConst.Z_FINISH);
            if (err != zlibConst.Z_STREAM_END)
            {
                stream.inflateEnd();
                return err == zlibConst.Z_OK ? zlibConst.Z_BUF_ERROR : err;
            }

            if (stream.total_out != dest.Length)
            {
                byte[] output = new byte[stream.total_out];
                Buffer.BlockCopy(stream.next_out, 0, output, 0, (int)stream.total_out);
                dest = output;
            }

            return stream.inflateEnd();
        }

        #endregion

        #region gzdeflate, gzinflate

        /// <summary>
        /// This function compress the given string using the DEFLATE data format.
        /// </summary>
        /// <param name="data">The data to deflate.</param>
        /// <returns>The deflated string or FALSE if an error occurred.</returns>
        [ImplementsFunction("gzdeflate")]
        [return: CastToFalse]
        public static PhpBytes GzDeflate(PhpBytes data)
        {
            return GzDeflate(data, -1);
        }

        /// <summary>
        /// This function compress the given string using the DEFLATE data format.
        /// </summary>
        /// <param name="data">The data to deflate.</param>
        /// <param name="level">
        ///     The level of compression. Can be given as 0 for no compression up to 9 for maximum compression.
        ///     If not given, the default compression level will be the default compression level of the zlib library.
        /// </param>
        /// <returns>The deflated string or FALSE if an error occurred.</returns>
        [ImplementsFunction("gzdeflate")]
        [return: CastToFalse]
        public static PhpBytes GzDeflate(PhpBytes data, int level)
        {
            if ((level < -1) || (level > 9))
            {
                PhpException.Throw(PhpError.Warning, String.Format("compression level ({0}) must be within -1..9", level));
                return null;
            }

            ZStream zs = new ZStream();

            zs.next_in = data.ReadonlyData;
            zs.avail_in = data.Length;

            // heuristic for max data length
            zs.avail_out = data.Length + data.Length / PHP_ZLIB_MODIFIER + 15 + 1;
            zs.next_out = new byte[zs.avail_out];

            // -15 omits the header (undocumented feature of zlib)
            int status = zs.deflateInit(level, -MAX_WBITS);

            if (status == zlibConst.Z_OK)
            {
                status = zs.deflate(zlibConst.Z_FINISH);
                if (status != zlibConst.Z_STREAM_END)
                {
                    zs.deflateEnd();
                    if (status == zlibConst.Z_OK)
                    {
                        status = zlibConst.Z_BUF_ERROR;
                    }
                }
                else
                {
                    status = zs.deflateEnd();
                }
            }

            if (status == zlibConst.Z_OK)
            {
                byte[] result = new byte[zs.total_out];
                Buffer.BlockCopy(zs.next_out, 0, result, 0, (int)zs.total_out);
                return new PhpBytes(result);
            }
            else
            {
                PhpException.Throw(PhpError.Warning, zError(status));
                return null;
            }
        }

        /// <summary>
        /// This function inflate a deflated string.
        /// </summary> 
        /// <param name="data">The data compressed by gzdeflate().</param>
        /// <returns>
        ///     <para>
        ///         The original uncompressed data or FALSE on error.
        ///     </para>
        ///     <para>
        ///         The function will return an error if the uncompressed data is more than 32768 times the length of 
        ///         the compressed input data or more than the optional parameter length.
        ///     </para>
        /// </returns>
        [ImplementsFunction("gzinflate")]
        [return: CastToFalse]
        public static PhpBytes GzInflate(PhpBytes data)
        {
            return GzInflate(data, 0);
        }

        /// <summary>
        /// This function inflate a deflated string.
        /// </summary> 
        /// <param name="data">The data compressed by gzdeflate().</param>
        /// <param name="length">The maximum length of data to decode.</param>
        /// <returns>
        ///     <para>
        ///         The original uncompressed data or FALSE on error.
        ///     </para>
        ///     <para>
        ///         The function will return an error if the uncompressed data is more than 32768 times the length of 
        ///         the compressed input data or more than the optional parameter length.
        ///     </para>
        /// </returns>
        [ImplementsFunction("gzinflate")]
        [return: CastToFalse]
        public static PhpBytes GzInflate(PhpBytes data, long length)
        {
            uint factor=1, maxfactor=16;
	        long ilength;

            ZStream zs = new ZStream();

            zs.avail_in = data.Length;
            zs.next_in = data.ReadonlyData;
            zs.total_out = 0;

            // -15 omits the header (undocumented feature of zlib)
            int status = zs.inflateInit(-15);

            if (status != zlibConst.Z_OK)
            {
                PhpException.Throw(PhpError.Warning, zError(status));
                return null;
            }

            do
            {
                ilength = length != 0 ? length : data.Length * (1 << (int)(factor++));

                try
                {
                    byte[] newOutput = new byte[ilength];

                    if (zs.next_out != null)
                    {
                        Buffer.BlockCopy(zs.next_out, 0, newOutput, 0, zs.next_out.Length);
                    }

                    zs.next_out = newOutput;
                }
                catch (OutOfMemoryException)
                {
                    zs.inflateEnd();
                    return null;
                }

                zs.next_out_index = (int)zs.total_out;
                zs.avail_out = unchecked((int)(ilength - zs.total_out));
                status = zs.inflate(zlibConst.Z_NO_FLUSH);
            }
            while ((status == zlibConst.Z_BUF_ERROR || (status == zlibConst.Z_OK && (zs.avail_in != 0 || zs.avail_out == 0))) && length == 0 && factor < maxfactor);

            zs.inflateEnd();

            if ((length != 0 && status == zlibConst.Z_OK) || factor >= maxfactor)
            {
                status = zlibConst.Z_MEM_ERROR;
            }

            if (status == zlibConst.Z_STREAM_END || status == zlibConst.Z_OK)
            {
                byte[] result = new byte[zs.total_out];
                Buffer.BlockCopy(zs.next_out, 0, result, 0, (int)zs.total_out);
                return new PhpBytes(result);
            }
            else
            {
                PhpException.Throw(PhpError.Warning, zError(status));
                return null;
            }
        }

        #endregion

        #region gzdecode, gzencode

        /// <summary>
        /// This function returns a decoded version of the input data.
        /// </summary>
        /// <param name="data">The data to decode, encoded by gzencode().</param>
        /// <returns>The decoded string, or FALSE if an error occurred.</returns>
        [ImplementsFunction("gzdecode", FunctionImplOptions.NotSupported)]
        [return: CastToFalse]
        public static PhpBytes GzDecode(PhpBytes data)
        {
            return GzDecode(data, 0);
        }

        /// <summary>
        /// This function returns a decoded version of the input data.
        /// </summary>
        /// <param name="data">The data to decode, encoded by gzencode().</param>
        /// <param name="length">The maximum length of data to decode.</param>
        /// <returns>The decoded string, or FALSE if an error occurred.</returns>
        [ImplementsFunction("gzdecode", FunctionImplOptions.NotSupported)]
        [return: CastToFalse]
        public static PhpBytes GzDecode(PhpBytes data, int length)
        {
            PhpException.FunctionNotSupported(PhpError.Warning);
            return null;
        }

        /// <summary>
        /// This function returns a compressed version of the input data compatible with the output of the gzip program.
        /// </summary>
        /// <param name="data">The data to encode.</param>
        /// <returns>The encoded string, or FALSE if an error occurred.</returns>
        [ImplementsFunction("gzencode")]
        [return: CastToFalse]
        public static PhpBytes GzEncode(PhpBytes data)
        {
            return GzEncode(data, -1, (int)ForceConstants.FORCE_GZIP);
        }

        /// <summary>
        /// This function returns a compressed version of the input data compatible with the output of the gzip program.
        /// </summary>
        /// <param name="data">The data to encode.</param>
        /// <param name="level">
        ///     The level of compression. Can be given as 0 for no compression up to 9 for maximum compression. If not 
        ///     given, the default compression level will be the default compression level of the zlib library.
        /// </param>
        /// <returns>The encoded string, or FALSE if an error occurred.</returns>
        [ImplementsFunction("gzencode")]
        [return: CastToFalse]
        public static PhpBytes GzEncode(PhpBytes data, int level)
        {
            return GzEncode(data, level, (int)ForceConstants.FORCE_GZIP);
        }

        /// <summary>
        /// This function returns a compressed version of the input data compatible with the output of the gzip program.
        /// </summary>
        /// <param name="data">The data to encode.</param>
        /// <param name="level">
        ///     The level of compression. Can be given as 0 for no compression up to 9 for maximum compression. If not 
        ///     given, the default compression level will be the default compression level of the zlib library.
        /// </param>
        /// <param name="encoding_mode">
        ///     <para>The encoding mode. Can be FORCE_GZIP (the default) or FORCE_DEFLATE.</para>
        ///     <para>
        ///         If you use FORCE_DEFLATE, you get a standard zlib deflated string (inclusive zlib headers) after 
        ///         the gzip file header but without the trailing crc32 checksum.
        ///     </para>
        /// </param>
        /// <returns>The encoded string, or FALSE if an error occurred.</returns>
        [ImplementsFunction("gzencode")]
        [return: CastToFalse]
        public static PhpBytes GzEncode(PhpBytes data, int level, int encoding_mode)
        {
            if ((level < -1) || (level > 9))
            {
                PhpException.Throw(PhpError.Warning, String.Format("compression level ({0}) must be within -1..9", level));
                return null;
            }

            ZStream zs = new ZStream();
            int status = zlibConst.Z_OK;

            zs.next_in = data.ReadonlyData;
            zs.avail_in = data.Length;

            // heuristic for max data length
            zs.avail_out = data.Length + data.Length / Zlib.PHP_ZLIB_MODIFIER + 15 + 1;
            zs.next_out = new byte[zs.avail_out];

            switch (encoding_mode)
            {
                case (int)ForceConstants.FORCE_GZIP:
                    if ((status = zs.deflateInit(level, -MAX_WBITS)) != zlibConst.Z_OK) 
                    {
                        PhpException.Throw(PhpError.Warning, zError(status));
                        return null;
			        }
                    break;
                case (int)ForceConstants.FORCE_DEFLATE:
                    if ((status = zs.deflateInit(level)) != zlibConst.Z_OK)
                    {
                        PhpException.Throw(PhpError.Warning, zError(status));
                        return null;
                    }
                    break;
            }

            status = zs.deflate(zlibConst.Z_FINISH);

            if (status != zlibConst.Z_STREAM_END)
            {
                zs.deflateEnd();

                if (status == zlibConst.Z_OK)
                {
                    status = zlibConst.Z_STREAM_ERROR;
                }
            }
            else
            {
                status = zs.deflateEnd();
            }

            if (status == zlibConst.Z_OK)
            {
                long output_length = zs.total_out + (encoding_mode == (int)ForceConstants.FORCE_GZIP ? GZIP_HEADER_LENGTH + GZIP_FOOTER_LENGTH : GZIP_HEADER_LENGTH);
                long output_offset = GZIP_HEADER_LENGTH;

                byte[] output = new byte[output_length];
                Buffer.BlockCopy(zs.next_out, 0, output, (int)output_offset, (int)zs.total_out);

                // fill the header
                output[0] = GZIP_HEADER[0];
                output[1] = GZIP_HEADER[1];
                output[2] = Z_DEFLATED; // zlib constant (private in ZLIB.NET)
                output[3] = 0; // reserved flag bits (this function puts invalid flags in here)
                // 4-8 represent time and are set to zero
                output[9] = OS_CODE; // php constant

                if (encoding_mode == (int)ForceConstants.FORCE_GZIP)
                {
                    var crc_algo = new PHP.Library.CRC32();
                    byte[] crc = crc_algo.ComputeHash(data.ReadonlyData, 0, data.Length);
                    crc_algo.Dispose();

                    output[output_length - 8] = crc[0];
                    output[output_length - 7] = crc[1];
                    output[output_length - 6] = crc[2];
                    output[output_length - 5] = crc[3];
                    output[output_length - 4] = (byte)(zs.total_in & 0xFF);
                    output[output_length - 3] = (byte)((zs.total_in >> 8) & 0xFF);
                    output[output_length - 2] = (byte)((zs.total_in >> 16) & 0xFF);
                    output[output_length - 1] = (byte)((zs.total_in >> 24) & 0xFF);
                }

                return new PhpBytes(output);
            }
            else
            {
                PhpException.Throw(PhpError.Warning, zError(status));
                return null;
            }
        }

        #endregion

        #region gzeof, gzrewind, gzseek, gztell

        /// <summary>
        /// Tests the given GZ file pointer for EOF.
        /// </summary>
        /// <param name="zp">The gz-file pointer. It must be valid, and must point to a file successfully opened by gzopen().</param>
        /// <returns>Returns TRUE if the gz-file pointer is at EOF or an error occurs; otherwise returns FALSE.</returns>
        [ImplementsFunction("gzeof")]
        public static bool GzEof(PhpResource zp)
        {
            return PhpFile.Eof(zp);
        }

        /// <summary>
        /// Sets the file position indicator of the given gz-file pointer to the beginning of the file stream.
        /// </summary>
        /// <param name="zp">The gz-file pointer. It must be valid, and must point to a file successfully opened by gzopen().</param>
        /// <returns>Returns TRUE on success or FALSE on failure.</returns>
        [ImplementsFunction("gzrewind", FunctionImplOptions.NotSupported)]
        public static bool GzRewind(PhpResource zp)
        {
            return PhpFile.Rewind(zp);
        }

        /// <summary>
        ///     <para>
        ///         Sets the file position indicator for the given file pointer to the given offset byte into the file stream. Equivalent
        ///         to calling (in C) gzseek(zp, offset, SEEK_SET).
        ///     </para>
        ///     <para>
        ///         If the file is opened for reading, this function is emulated but can be extremely slow. If the file is opened for 
        ///         writing, only forward seeks are supported; gzseek() then compresses a sequence of zeroes up to the new starting position. 
        ///     </para>
        /// </summary>
        /// <param name="zp">The gz-file pointer. It must be valid, and must point to a file successfully opened by gzopen().</param>
        /// <param name="offset">The seeked offset.</param>
        /// <returns>Upon success, returns 0; otherwise, returns -1. Note that seeking past EOF is not considered an error.</returns>
        [ImplementsFunction("gzseek", FunctionImplOptions.NotSupported)]
        public static int GzSeek(PhpResource zp, int offset)
        {
            return GzSeek(zp, offset, (int)SeekOptions.Set);
        }

        /// <summary>
        ///     <para>
        ///         Sets the file position indicator for the given file pointer to the given offset byte into the file stream. Equivalent
        ///         to calling (in C) gzseek(zp, offset, SEEK_SET).
        ///     </para>
        ///     <para>
        ///         If the file is opened for reading, this function is emulated but can be extremely slow. If the file is opened for 
        ///         writing, only forward seeks are supported; gzseek() then compresses a sequence of zeroes up to the new starting position. 
        ///     </para>
        /// </summary>
        /// <param name="zp">The gz-file pointer. It must be valid, and must point to a file successfully opened by gzopen().</param>
        /// <param name="offset">The seeked offset.</param>
        /// <param name="whence">
        ///     whence values are: SEEK_SET (relative to origin), SEEK_CUR (relative to current position).
        /// </param>
        /// <returns>Upon success, returns 0; otherwise, returns -1. Note that seeking past EOF is not considered an error.</returns>
        [ImplementsFunction("gzseek", FunctionImplOptions.NotSupported)]
        public static int GzSeek(PhpResource zp, int offset, int whence)
        {
            return PhpFile.Seek(zp, offset, whence);
        }

        /// <summary>
        /// Gets the position of the given file pointer; i.e., its offset into the uncompressed file stream.
        /// </summary>
        /// <param name="zp">The gz-file pointer. It must be valid, and must point to a file successfully opened by gzopen().</param>
        /// <returns>The position of the file pointer or FALSE if an error occurs.</returns>
        [ImplementsFunction("gztell", FunctionImplOptions.NotSupported)]
        public static object GzTell(PhpResource zp)
        {
            return PhpFile.Tell(zp);
        }

        #endregion

        #region gzfile



        /// <summary>
        /// This function is identical to readgzfile(), except that it returns the file in an array.
        /// </summary>
        /// <param name="context">Current script context, passed automatically by the caller.</param>
        /// <param name="filename">The file name.</param>
        /// <returns>An array containing the file, one line per cell.</returns>
        [ImplementsFunction("gzfile")]
        [return: CastToFalse]
        public static PhpArray GzFile(ScriptContext context, string filename)
        {
            return GzFile(context, filename, 0);
        }

        /// <summary>
        /// This function is identical to readgzfile(), except that it returns the file in an array.
        /// </summary>
        /// <param name="context">Current script context, passed automatically by the caller.</param>
        /// <param name="filename">The file name.</param>
        /// <param name="use_include_path">
        ///     You can set this optional parameter to 1, if you want to search for the file in the include_path too.
        /// </param>
        /// <returns>An array containing the file, one line per cell.</returns>
        [ImplementsFunction("gzfile")]
        [return: CastToFalse]
        public static PhpArray GzFile(ScriptContext context, string filename, int use_include_path)
        {
            PhpStream fs = (PhpStream)GzOpen(filename, "r", use_include_path);

            if (fs == null) return null;

            PhpArray returnValue = new PhpArray();
            int blockLength = 8192;

            while (!fs.Eof)
            {
                string value = PhpStream.AsText(fs.ReadData(blockLength, true));

                returnValue.Add(Core.Convert.Quote(value, context));
            }

            return returnValue;
        }

        #endregion

        #region gzgetc, gzgets, gzgetss

        /// <summary>
        /// Returns a string containing a single (uncompressed) character read from the given gz-file pointer.
        /// </summary>
        /// <param name="zp">The gz-file pointer. It must be valid, and must point to a file successfully opened by gzopen().</param>
        /// <returns>The uncompressed character or FALSE on EOF (unlike gzeof()).</returns>
        [ImplementsFunction("gzgetc")]
        [return:CastToFalse]
        public static object GzGetChar(PhpResource zp)
        {
            return PhpFile.ReadChar(zp);
        }

        /// <summary>
        /// Gets a (uncompressed) string of up to length - 1 bytes read from the given file pointer. Reading ends when length - 1 bytes 
        /// have been read, on a newline, or on EOF (whichever comes first). 
        /// </summary>
        /// <param name="zp">The gz-file pointer. It must be valid, and must point to a file successfully opened by gzopen().</param>
        /// <param name="length">The length of data to get.</param>
        /// <returns>The uncompressed string, or FALSE on error.</returns>
        [ImplementsFunction("gzgets")]
        [return: CastToFalse]
        public static object GzGetString(PhpResource zp, int length)
        {
            return PhpFile.ReadLine(zp, length);
        }

        /// <summary>
        /// Identical to gzgets(), except that gzgetss() attempts to strip any HTML and PHP tags from the text it reads.
        /// </summary>
        /// <param name="zp">The gz-file pointer. It must be valid, and must point to a file successfully opened by gzopen().</param>
        /// <param name="length">The length of data to get.</param>
        /// <returns>The uncompressed and striped string, or FALSE on error.</returns>
        [ImplementsFunction("gzgetss")]
        [return: CastToFalse]
        public static object GzGetStringStripped(PhpResource zp, int length)
        {
            return GzGetStringStripped(zp, length, PhpBytes.Empty);
        }

        /// <summary>
        /// Identical to gzgets(), except that gzgetss() attempts to strip any HTML and PHP tags from the text it reads.
        /// </summary>
        /// <param name="zp">The gz-file pointer. It must be valid, and must point to a file successfully opened by gzopen().</param>
        /// <param name="length">The length of data to get.</param>
        /// <param name="allowable_tags">You can use this optional parameter to specify tags which should not be stripped.</param>
        /// <returns>The uncompressed and striped string, or FALSE on error.</returns>
        [ImplementsFunction("gzgetss")]
        [return: CastToFalse]
        public static object GzGetStringStripped(PhpResource zp, int length, PhpBytes allowable_tags)
        {
            return PhpFile.ReadLineStripTags(zp, length, allowable_tags.ToString());
        }

        #endregion

        #region gzpassthru, gzputs

        /// <summary>
        /// Reads to EOF on the given gz-file pointer from the current position and writes the (uncompressed) results to standard output.
        /// </summary>
        /// <param name="zp">The gz-file pointer. It must be valid, and must point to a file successfully opened by gzopen().</param>
        /// <returns>The number of uncompressed characters read from gz and passed through to the input, or FALSE on error.</returns>
        [ImplementsFunction("gzpassthru")]
        public static int GzPassthru(PhpResource zp)
        {
            return PhpFile.PassThrough(zp);
        }

        /// <summary>
        /// This function is an alias of gzwrite(), which writes the contents of string to the given gz-file.
        /// </summary>
        /// <param name="zp">The gz-file pointer. It must be valid, and must point to a file successfully opened by gzopen().</param>
        /// <param name="str">The string to write.</param>
        /// <returns>Returns the number of (uncompressed) bytes written to the given gz-file stream.</returns>
        [ImplementsFunction("gzputs")]
        public static int GzPutString(PhpResource zp, PhpBytes str)
        {
            return GzPutString(zp, str, -1);
        }

        /// <summary>
        /// This function is an alias of gzwrite(), which writes the contents of string to the given gz-file.
        /// </summary>
        /// <param name="zp">The gz-file pointer. It must be valid, and must point to a file successfully opened by gzopen().</param>
        /// <param name="str">The string to write.</param>
        /// <param name="length">
        ///     The number of uncompressed bytes to write. If supplied, writing will stop after length (uncompressed) bytes have been 
        ///     written or the end of string is reached, whichever comes first.
        /// </param>
        /// <returns>Returns the number of (uncompressed) bytes written to the given gz-file stream.</returns>
        [ImplementsFunction("gzputs")]
        public static int GzPutString(PhpResource zp, PhpBytes str, int length)
        {
            return GzWrite(zp, str, length);
        }

        #endregion

        #region gzread, gzwrite

        /// <summary>
        /// Reads up to length bytes from the given gz-file pointer. Reading stops when length (uncompressed) bytes 
        /// have been read or EOF is reached, whichever comes first.
        /// </summary>
        /// <param name="zp">The gz-file pointer. It must be valid, and must point to a file successfully opened by gzopen().</param>
        /// <param name="length">The number of bytes to read.</param>
        /// <returns>The data that have been read.</returns>
        [ImplementsFunction("gzread")]
        public static object GzRead(PhpResource zp, int length)
        {
            return PhpFile.Read(zp, length);
        }

        /// <summary>
        /// Writes the contents of string to the given gz-file.
        /// </summary>
        /// <param name="zp">The gz-file pointer. It must be valid, and must point to a file successfully opened by gzopen().</param>
        /// <param name="str">The string to write.</param>
        /// <returns>Returns the number of (uncompressed) bytes written to the given gz-file stream.</returns>
        [ImplementsFunction("gzwrite")]
        public static int GzWrite(PhpResource zp, PhpBytes str)
        {
            return GzWrite(zp, str, -1);
        }

        /// <summary>
        /// Writes the contents of string to the given gz-file.
        /// </summary>
        /// <param name="zp">The gz-file pointer. It must be valid, and must point to a file successfully opened by gzopen().</param>
        /// <param name="str">The string to write.</param>
        /// <param name="length">
        ///     The number of uncompressed bytes to write. If supplied, writing will stop after length (uncompressed) bytes have been 
        ///     written or the end of string is reached, whichever comes first.
        /// </param>
        /// <returns>Returns the number of (uncompressed) bytes written to the given gz-file stream.</returns>
        [ImplementsFunction("gzwrite")]
        public static int GzWrite(PhpResource zp, PhpBytes str, int length)
        {
            return PhpFile.Write(zp, str, length);
        }

        #endregion

        #region readgzfile

        /// <summary>
        /// Reads a file, decompresses it and writes it to standard output.
        /// </summary> 
        /// <param name="filename">
        ///     The file name. This file will be opened from the filesystem and its contents written to standard output.
        /// </param>
        /// <returns>
        ///     Returns the number of (uncompressed) bytes read from the file. If an error occurs, FALSE is returned and 
        ///     unless the function was called as @readgzfile, an error message is printed.
        /// </returns>
        [ImplementsFunction("readgzfile")]
        public static int ReadGzFile(string filename)
        {
            return ReadGzFile(filename, 0);
        }

        /// <summary>
        /// Reads a file, decompresses it and writes it to standard output.
        /// </summary> 
        /// <param name="filename">
        ///     The file name. This file will be opened from the filesystem and its contents written to standard output.
        /// </param>
        /// <param name="use_include_path">
        ///     You can set this optional parameter to 1, if you want to search for the file in the include_path too.
        /// </param>
        /// <returns>
        ///     Returns the number of (uncompressed) bytes read from the file. If an error occurs, FALSE is returned and 
        ///     unless the function was called as @readgzfile, an error message is printed.
        /// </returns>
        [ImplementsFunction("readgzfile")]
        public static int ReadGzFile(string filename, int use_include_path)
        {
            PhpStream fs = (PhpStream)GzOpen(filename, "r", use_include_path);

            return PhpStreams.Copy(fs, InputOutputStreamWrapper.ScriptOutput);
        }

        #endregion

        #region zlib_get_coding_type

        /// <summary>
        /// Returns the coding type used for output compression.
        /// </summary>
        /// <returns>Possible return values are gzip, deflate, or FALSE.</returns>
        [ImplementsFunction("zlib_get_coding_type", FunctionImplOptions.NotSupported)]
        public static PhpBytes ZlibGetCodingType()
        {
            PhpException.FunctionNotSupported(PhpError.Warning);
            return null;
        }

        #endregion
    }
}