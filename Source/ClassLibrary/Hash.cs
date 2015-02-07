﻿/*

 Copyright (c) 2004-2006 Tomas Matousek and Ladislav Prosek.  
 Copyright (c) 2010 Jakub Misek

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

/*

 TODO: (PHP 5.2 hash functions)
    * hash_algos  — Return a list of registered hashing algorithms
    * hash_copy — Copy hashing context
    * hash_file — Generate a hash value using the contents of a given file
    * hash_final — Finalize an incremental hash and return resulting digest
    * hash_hmac_file — Generate a keyed hash value using the HMAC method and the contents of a given file
    * hash_hmac — Generate a keyed hash value using the HMAC method
    * hash_init — Initialize an incremental hashing context
    * hash_update_file — Pump data into an active hashing context from a file
    * hash_update_stream — Pump data into an active hashing context from an open stream
    * hash_update — Pump data into an active hashing context
    * hash — Generate a hash value (message digest)

*/

using System;
using PHP.Core;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.ComponentModel;
using CRYPT = System.Security.Cryptography;

#if SILVERLIGHT
using PHP.CoreCLR;
using System.Windows.Browser;
#else
using System.Web;
#endif

namespace PHP.Library
{
    /// <summary>
    /// PHP hash functions support.
    /// </summary>
    [ImplementsExtension(LibraryDescriptor.ExtHash)]
    public static class PhpHash
    {
        #region crc32, (Only CLR:) md5, md5_file, sha1, sha1_file, sha256, sha256_file

        /// <summary>
        /// Calculates the crc32 polynomial of a string of bytes.
        /// </summary>
        /// <param name="bytes">The string of bytes to compute crc32 of.</param>
        /// <returns>The CRC32 of <paramref name="bytes"/>.</returns>
        [ImplementsFunction("crc32")]
        [PureFunction]
        public static int CRC32(PhpBytes bytes)
        {
            return BitConverter.ToInt32(new CRC32().ComputeHash(bytes.ReadonlyData), 0);
        }

#if !SILVERLIGHT

        /// <summary>
        /// Calculate the md5 hash of a string of bytes.
        /// </summary>
        /// <param name="bytes">The string of bytes to compute md5 of.</param>
        /// <returns>MD5 of <paramref name="bytes"/> as a sequence of hexadecimal numbers.</returns>
        [ImplementsFunction("md5")]
        [PureFunction]
        public static string MD5(PhpBytes bytes)
        {
            return (string)MD5(bytes, false);
        }

        /// <summary>
        /// Calculate the md5 hash of a string of bytes.
        /// </summary>
        /// <param name="bytes">The string of bytes to compute md5 of.</param>
        /// <param name="rawOutput">If true, returns raw binary hash, otherwise returns hash as 
        /// a sequence of hexadecimal numbers.</param>
        /// <returns>md5 of <paramref name="bytes"/>.</returns>
        [ImplementsFunction("md5")]
        [PureFunction]
        public static object MD5(PhpBytes bytes, bool rawOutput)
        {
            return Hash(new CRYPT.MD5CryptoServiceProvider(), bytes, rawOutput);
        }

        /// <summary>
        /// Calculates the md5 hash of a given file.
        /// </summary>
        /// <param name="fileName">The file name.</param>
        /// <returns>MD5 of given <paramref name="fileName"/> content.</returns>
        [ImplementsFunction("md5_file")]
        public static string MD5FromFile(string fileName)
        {
            return (string)MD5FromFile(fileName, false);
        }

        /// <summary>
        /// Calculates the md5 hash of a given file.
        /// </summary>
        /// <param name="fileName">The file name.</param>
        /// <param name="rawOutput">If <B>true</B>, returns raw binary hash, otherwise returns hash as 
        /// a sequence of hexadecimal numbers.</param>
        /// <returns>MD5 of given <paramref name="fileName"/> content.</returns>
        [ImplementsFunction("md5_file")]
        public static object MD5FromFile(string fileName, bool rawOutput)
        {
            return HashFromFile(new CRYPT.MD5CryptoServiceProvider(), fileName, rawOutput);
        }

        /// <summary>
        /// Calculate the sha1 hash of a string of bytes.
        /// </summary>
        /// <param name="bytes">The string of bytes to compute SHA1 of.</param>
        /// <returns>SHA1 of <paramref name="bytes"/> as a sequence of hexadecimal numbers.</returns>
        [ImplementsFunction("sha1")]
        [PureFunction]
        public static string SHA1(PhpBytes bytes)
        {
            return (string)SHA1(bytes, false);
        }

        /// <summary>
        /// Calculate the SHA1 hash of a string of bytes.
        /// </summary>
        /// <param name="bytes">The string of bytes to compute SHA1 of.</param>
        /// <param name="rawOutput">If <B>true</B>, returns raw binary hash, otherwise returns hash as 
        /// a sequence of hexadecimal numbers.</param>
        /// <returns>md5 of <paramref name="bytes"/>.</returns>
        [ImplementsFunction("sha1")]
        [PureFunction]
        public static object SHA1(PhpBytes bytes, bool rawOutput)
        {
            return Hash(new CRYPT.SHA1CryptoServiceProvider(), bytes, rawOutput);
        }

        /// <summary>
        /// Calculates the SHA1 hash of a given file.
        /// </summary>
        /// <param name="fileName">The file name.</param>
        /// <returns>SHA1 of given <paramref name="fileName"/> content.</returns>
        [ImplementsFunction("sha1_file")]
        public static string SHA1FromFile(string fileName)
        {
            return (string)SHA1FromFile(fileName, false);
        }

        /// <summary>
        /// Calculates the SHA1 hash of a given file.
        /// </summary>
        /// <param name="fileName">The file name.</param>
        /// <param name="rawOutput">If <B>true</B>, returns raw binary hash, otherwise returns hash as 
        /// a sequence of hexadecimal numbers.</param>
        /// <returns>SHA1 of <paramref name="fileName"/> content.</returns>
        [ImplementsFunction("sha1_file")]
        public static object SHA1FromFile(string fileName, bool rawOutput)
        {
            return HashFromFile(new CRYPT.SHA1CryptoServiceProvider(), fileName, rawOutput);
        }
#endif

        #region not implemented in PHP
        ///// <summary>
        ///// Calculate the sha256 hash of a string of bytes.
        ///// </summary>
        ///// <param name="bytes">The string of bytes to compute SHA256 of.</param>
        ///// <returns>SHA256 of <paramref name="bytes"/> as a sequence of hexadecimal numbers.</returns>
        //[ImplementsFunction("sha256")]
        //public static string SHA256(PhpBytes bytes)
        //{
        //    return (string)SHA256(bytes, false);
        //}

        ///// <summary>
        ///// Calculate the SHA256 hash of a string of bytes.
        ///// </summary>
        ///// <param name="bytes">The string of bytes to compute SHA256 of.</param>
        ///// <param name="rawOutput">If <B>true</B>, returns raw binary hash, otherwise returns hash as 
        ///// a sequence of hexadecimal numbers.</param>
        ///// <returns>md5 of <paramref name="str"/>.</returns>
        //[ImplementsFunction("sha256")]
        //public static object SHA256(PhpBytes bytes, bool rawOutput)
        //{
        //    return Hash(new CRYPT.SHA256Managed(), bytes, rawOutput);
        //}

        ///// <summary>
        ///// Calculates the SHA256 hash of a given file.
        ///// </summary>
        ///// <param name="fileName">The file name.</param>
        ///// <returns>SHA256 of <paramref name="str"/> as a sequence of hexadecimal numbers.</returns>
        //[ImplementsFunction("sha256_file")]
        //public static string SHA256FromFile(string fileName)
        //{
        //    return (string)SHA256FromFile(fileName, false);
        //}

        ///// <summary>
        ///// Calculates the SHA256 hash of a given file.
        ///// </summary>
        ///// <param name="fileName">The file name.</param>
        ///// <param name="rawOutput">If <B>true</B>, returns raw binary hash, otherwise returns hash as 
        ///// a sequence of hexadecimal numbers.</param>
        ///// <returns>SHA256 of <paramref name="str"/>.</returns>
        //[ImplementsFunction("sha256_file")]
        //public static object SHA256FromFile(string fileName, bool rawOutput)
        //{
        //    return HashFromFile(new CRYPT.SHA256Managed(), fileName, rawOutput);
        //}
        #endregion


        /// <summary>
        /// Computes a hash of a string of bytes using specified algorithm.
        /// </summary>
        private static object Hash(CRYPT.HashAlgorithm/*!*/ algorithm, PhpBytes bytes, bool rawOutput)
        {
            if (bytes == null) return null;

            byte[] hash = algorithm.ComputeHash(bytes.ReadonlyData);

            if (rawOutput)
                return new PhpBytes(hash);
            else
                return StringUtils.BinToHex(hash, null);
        }

        /// <summary>
        /// Computes a hash of a file using specified algorithm.
        /// </summary>
        private static object HashFromFile(CRYPT.HashAlgorithm/*!*/ algorithm, string fileName, bool rawOutput)
        {
            byte[] hash;

            try
            {
                using (PhpStream stream = PhpStream.Open(fileName, "rb", StreamOpenOptions.Empty, StreamContext.Default))
                //using (FileStream file = new FileStream(fileName, FileMode.Open, FileAccess.Read))
                {
                    if (stream == null)
                        return null;

                    object data = stream.ReadContents();
			        if (data == null)
				        return null;

			        PhpBytes bytes = PhpStream.AsBinary(data);
                    if (bytes == null)
                        return null;

                    hash = algorithm.ComputeHash(bytes.ReadonlyData);
                }
            }
            catch (SystemException)
            {
                return null;
            }

            if (rawOutput)
                return new PhpBytes(hash);
            else
                return StringUtils.BinToHex(hash, null);
        }

        #endregion

        #region HashPhpResource

        /// <summary>
        /// The Hashing Context PHP Resource.
        /// </summary>
        public abstract class HashPhpResource : PhpResource
        {
            #region HashPhpResource base ctor

            /// <summary>
            /// hash_init
            /// </summary>
            protected HashPhpResource()
                : base("Hash Context")
            {
                Init();
            }

            #endregion

            #region HashPhpResource interface

            /// <summary>
            /// hash_copy
            /// </summary>
            /// <returns></returns>
            public abstract HashPhpResource Clone();

            /// <summary>
            /// hash_init
            /// Restart the hashing algorithm.
            /// </summary>
            public abstract void Init();

            /// <summary>
            /// hash_update
            /// Push more data into the algorithm, incremental hashing.
            /// </summary>
            /// <param name="data"></param>
            /// <returns></returns>
            public abstract bool Update(byte[] data);

            /// <summary>
            /// hash_final
            /// Finalize the algorithm. Get the result.
            /// </summary>
            /// <returns></returns>
            public abstract byte[] Final();

            /// <summary>
            /// HMAC key max size.
            /// </summary>
            public abstract int BlockSize { get; }

            #endregion

            #region hash_init state

            internal HashInitOptions options = HashInitOptions.HASH_DEFAULT;
            internal byte[] HMACkey = null;

            protected void CloneHashState(HashPhpResource clone)
            {
                clone.options = this.options;
                clone.HMACkey = (this.HMACkey != null) ? (byte[])this.HMACkey.Clone() : null;
            }

            #endregion

            #region Helper methods

            #region Buffering for blocks of input data

            private byte[] buffer = null;
            private int bufferUsage = 0;

            /// <summary>
            /// Returns blocks of data, using buffered data stored before.
            /// Provided data can be too small to fit the block, so they are buffered and processed when more data comes.
            /// </summary>
            /// <param name="newData">New pack of data to be appended to the buffered ones.</param>
            /// <param name="blockSize">Block size, when buffered data fits this, they are returned.</param>
            /// <returns>Packs of block, as a pair of byte array and index of first element.</returns>
            internal IEnumerable<Tuple<byte[], int>> ProcessBlocked(byte[]/*!*/newData, int blockSize)
            {
                Debug.Assert(newData != null);
                Debug.Assert(blockSize > 0);

                int index = 0;  // index of first byte in the newData to be used as a block start

                // fill the buffer / used buffered data if it fits the block size
                if (bufferUsage > 0)
                {
                    Debug.Assert(buffer != null);
                    Debug.Assert(buffer.Length == blockSize);

                    int bytesToFitBuffer = blockSize - bufferUsage; // bytes needed to fill the whole buffer

                    if (newData.Length < bytesToFitBuffer)
                    {
                        Array.Copy(newData, 0, buffer, bufferUsage, newData.Length);
                        bufferUsage += newData.Length;
                        yield break;
                    }

                    Array.Copy(newData, 0, buffer, bufferUsage, bytesToFitBuffer);
                    yield return new Tuple<byte[], int>(buffer, 0); // use the data from buffer

                    bufferUsage = 0;            // buffer is empty now
                    index += bytesToFitBuffer;  // part of newData was used
                }

                // returns blocks from the newData
                while (index + blockSize <= newData.Length)
                {
                    yield return new Tuple<byte[], int>(newData, index);
                    index += blockSize;
                }

                // put the rest of newData into the buffer
                int remainingBytes = newData.Length - index;
                if (remainingBytes > 0)
                {
                    if (buffer == null) buffer = new byte[blockSize];
                    Debug.Assert(remainingBytes < blockSize);
                    Debug.Assert(buffer.Length == blockSize);

                    Array.Copy(newData, index, buffer, 0, remainingBytes);
                    bufferUsage = remainingBytes;
                }
            }

            /// <summary>
            /// Returns the buffered bytes not processed yet.
            /// </summary>
            /// <param name="length">Amount of used bytes in the buffer.</param>
            /// <returns>Buffer, can be null.</returns>
            internal byte[] GetBufferedBlock(out int length)
            {
                length = bufferUsage;
                return buffer;
            }

            internal void ClearBufferedBlock()
            {
                bufferUsage = 0;
                buffer = null;
            }

            /// <summary>
            /// Copies the buffered information from this instance into the clone.
            /// </summary>
            /// <param name="clone">Copy buffered info here.</param>
            internal void CloneBufferedBlock(HashPhpResource clone)
            {
                if (bufferUsage > 0)
                {
                    clone.bufferUsage = this.bufferUsage;
                    clone.buffer = new byte[this.buffer.Length];
                    this.buffer.CopyTo(clone.buffer, 0);
                }
                else
                {
                    clone.bufferUsage = 0;
                    clone.buffer = null;
                }
            }

            #endregion

            #region Conversion

            protected static void DWORDToBigEndian(byte[] block, uint[] x, int digits)
            {
                int index = 0;
                for (int i = 0; index < digits; i += 4)
                {
                    block[i] = (byte)((x[index] >> 0x18) & 0xff);
                    block[i + 1] = (byte)((x[index] >> 0x10) & 0xff);
                    block[i + 2] = (byte)((x[index] >> 8) & 0xff);
                    block[i + 3] = (byte)(x[index] & 0xff);
                    index++;
                }
            }

            protected static void DWORDFromBigEndian(uint[] x, int digits, byte[] block)
            {
                int index = 0;
                for (int i = 0; index < digits; i += 4)
                {
                    x[index] = ((((uint)block[i] << 0x18) | (uint)(block[i + 1] << 0x10)) | (uint)(block[i + 2] << 8)) | (uint)block[i + 3];
                    index++;
                }
            }


            #endregion

            /// <summary>
            /// Simply compute hash on existing HashPhpResource instance.
            /// No HMAC.
            /// The algorithm is reinitialized.
            /// </summary>
            /// <param name="data"></param>
            /// <returns></returns>
            public byte[] ComputeHash(byte[] data)
            {
                this.Init();

                if (!this.Update(data))
                    return null;

                return this.Final();
            }

            #endregion

            #region hash algorithms implementation

            #region list of available algorithms

            internal delegate HashPhpResource HashAlgFactory();

            internal static Dictionary<string, HashAlgFactory> _HashAlgorithms = null;
            internal static Dictionary<string, HashAlgFactory> HashAlgorithms
            {
                get
                {
                    if (_HashAlgorithms == null)
                    {
                        var algs = new Dictionary<string, HashAlgFactory>(25, StringComparer.OrdinalIgnoreCase);

                        //
                        // note: use lower case as algorithms name
                        //

                        algs["crc32"] = () => new CRC32();
                        algs["crc32b"] = () => new CRC32B();

                        algs["md2"] = () => new MD2();
                        algs["md4"] = () => new MD4();
                        algs["md5"] = () => new MD5();

                        //algs["haval256,3"] = () => new HAVAL256();
                        //algs["haval224,3"] = () => new HAVAL224();
                        //algs["haval192,3"] = () => new HAVAL192();
                        //algs["haval160,3"] = () => new HAVAL160();
                        //algs["haval128,3"] = () => new HAVAL128();

                        //algs["tiger192,3"] = () => new TIGER();
                        //algs["tiger128,3"] = () => new TIGER128();
                        //algs["tiger160,3"] = () => new TIGER160();
                        
                        //algs["gost"] = () => new GOST();
                        
                        algs["adler32"] = () => new ADLER32();
                        
                        algs["sha1"] = () => new SHA1();
                        //algs["sha224"] = () => new SHA224();
                        algs["sha256"] = () => new SHA256();
                        //algs["sha384"] = () => new SHA384();
                        //algs["sha512"] = () => new SHA512();
                        
                        //algs["whirlpool"] = () => new WHIRLPOOL();
                        
                        //algs["ripemd160"] = () => new RIPEMD160();
                        //algs["ripemd128"] = () => new RIPEMD128();
                        //algs["ripemd256"] = () => new RIPEMD256();
                        //algs["ripemd320"] = () => new RIPEMD320();
                        
                        //algs["snefru256"] = () => new SNEFRU256();
                        
                        _HashAlgorithms = algs;
                    }

                    return _HashAlgorithms;
                }
            }

            #endregion

            public sealed class ADLER32 : HashPhpResource
            {
                private uint state;

                public override HashPhpResource Clone()
                {
                    var clone = new ADLER32()
                    {
                        state = this.state
                    };
                    CloneHashState(clone);
                    return clone;
                }
                public override void Init()
                {
                    state = 1;
                }
                public override bool Update(byte[] data)
                {
                    uint s0, s1;

                    s0 = state & 0xffff;
                    s1 = (state >> 16) & 0xffff;
                    foreach (byte b in data)
                    {
                        s0 = (s0 + b) % 65521;
                        s1 = (s1 + s0) % 65521;
                    }
                    state = s0 + (s1 << 16);

                    return true;
                }
                public override byte[] Final()
                {
                    byte[] bytes = BitConverter.GetBytes((uint)state);
                    Array.Reverse(bytes);

                    state = 0;
                    return bytes;
                }
                public override int BlockSize { get { return 4; } }
            }
            public sealed class CRC32 : HashPhpResource
            {
                private uint state;

                private static uint[] crc32_table = { 0x0,
	0x04c11db7, 0x09823b6e, 0x0d4326d9, 0x130476dc, 0x17c56b6b,
	0x1a864db2, 0x1e475005, 0x2608edb8, 0x22c9f00f, 0x2f8ad6d6,
	0x2b4bcb61, 0x350c9b64, 0x31cd86d3, 0x3c8ea00a, 0x384fbdbd,
	0x4c11db70, 0x48d0c6c7, 0x4593e01e, 0x4152fda9, 0x5f15adac,
	0x5bd4b01b, 0x569796c2, 0x52568b75, 0x6a1936c8, 0x6ed82b7f,
	0x639b0da6, 0x675a1011, 0x791d4014, 0x7ddc5da3, 0x709f7b7a,
	0x745e66cd, 0x9823b6e0, 0x9ce2ab57, 0x91a18d8e, 0x95609039,
	0x8b27c03c, 0x8fe6dd8b, 0x82a5fb52, 0x8664e6e5, 0xbe2b5b58,
	0xbaea46ef, 0xb7a96036, 0xb3687d81, 0xad2f2d84, 0xa9ee3033,
	0xa4ad16ea, 0xa06c0b5d, 0xd4326d90, 0xd0f37027, 0xddb056fe,
	0xd9714b49, 0xc7361b4c, 0xc3f706fb, 0xceb42022, 0xca753d95,
	0xf23a8028, 0xf6fb9d9f, 0xfbb8bb46, 0xff79a6f1, 0xe13ef6f4,
	0xe5ffeb43, 0xe8bccd9a, 0xec7dd02d, 0x34867077, 0x30476dc0,
	0x3d044b19, 0x39c556ae, 0x278206ab, 0x23431b1c, 0x2e003dc5,
	0x2ac12072, 0x128e9dcf, 0x164f8078, 0x1b0ca6a1, 0x1fcdbb16,
	0x018aeb13, 0x054bf6a4, 0x0808d07d, 0x0cc9cdca, 0x7897ab07,
	0x7c56b6b0, 0x71159069, 0x75d48dde, 0x6b93dddb, 0x6f52c06c,
	0x6211e6b5, 0x66d0fb02, 0x5e9f46bf, 0x5a5e5b08, 0x571d7dd1,
	0x53dc6066, 0x4d9b3063, 0x495a2dd4, 0x44190b0d, 0x40d816ba,
	0xaca5c697, 0xa864db20, 0xa527fdf9, 0xa1e6e04e, 0xbfa1b04b,
	0xbb60adfc, 0xb6238b25, 0xb2e29692, 0x8aad2b2f, 0x8e6c3698,
	0x832f1041, 0x87ee0df6, 0x99a95df3, 0x9d684044, 0x902b669d,
	0x94ea7b2a, 0xe0b41de7, 0xe4750050, 0xe9362689, 0xedf73b3e,
	0xf3b06b3b, 0xf771768c, 0xfa325055, 0xfef34de2, 0xc6bcf05f,
	0xc27dede8, 0xcf3ecb31, 0xcbffd686, 0xd5b88683, 0xd1799b34,
	0xdc3abded, 0xd8fba05a, 0x690ce0ee, 0x6dcdfd59, 0x608edb80,
	0x644fc637, 0x7a089632, 0x7ec98b85, 0x738aad5c, 0x774bb0eb,
	0x4f040d56, 0x4bc510e1, 0x46863638, 0x42472b8f, 0x5c007b8a,
	0x58c1663d, 0x558240e4, 0x51435d53, 0x251d3b9e, 0x21dc2629,
	0x2c9f00f0, 0x285e1d47, 0x36194d42, 0x32d850f5, 0x3f9b762c,
	0x3b5a6b9b, 0x0315d626, 0x07d4cb91, 0x0a97ed48, 0x0e56f0ff,
	0x1011a0fa, 0x14d0bd4d, 0x19939b94, 0x1d528623, 0xf12f560e,
	0xf5ee4bb9, 0xf8ad6d60, 0xfc6c70d7, 0xe22b20d2, 0xe6ea3d65,
	0xeba91bbc, 0xef68060b, 0xd727bbb6, 0xd3e6a601, 0xdea580d8,
	0xda649d6f, 0xc423cd6a, 0xc0e2d0dd, 0xcda1f604, 0xc960ebb3,
	0xbd3e8d7e, 0xb9ff90c9, 0xb4bcb610, 0xb07daba7, 0xae3afba2,
	0xaafbe615, 0xa7b8c0cc, 0xa379dd7b, 0x9b3660c6, 0x9ff77d71,
	0x92b45ba8, 0x9675461f, 0x8832161a, 0x8cf30bad, 0x81b02d74,
	0x857130c3, 0x5d8a9099, 0x594b8d2e, 0x5408abf7, 0x50c9b640,
	0x4e8ee645, 0x4a4ffbf2, 0x470cdd2b, 0x43cdc09c, 0x7b827d21,
	0x7f436096, 0x7200464f, 0x76c15bf8, 0x68860bfd, 0x6c47164a,
	0x61043093, 0x65c52d24, 0x119b4be9, 0x155a565e, 0x18197087,
	0x1cd86d30, 0x029f3d35, 0x065e2082, 0x0b1d065b, 0x0fdc1bec,
	0x3793a651, 0x3352bbe6, 0x3e119d3f, 0x3ad08088, 0x2497d08d,
	0x2056cd3a, 0x2d15ebe3, 0x29d4f654, 0xc5a92679, 0xc1683bce,
	0xcc2b1d17, 0xc8ea00a0, 0xd6ad50a5, 0xd26c4d12, 0xdf2f6bcb,
	0xdbee767c, 0xe3a1cbc1, 0xe760d676, 0xea23f0af, 0xeee2ed18,
	0xf0a5bd1d, 0xf464a0aa, 0xf9278673, 0xfde69bc4, 0x89b8fd09,
	0x8d79e0be, 0x803ac667, 0x84fbdbd0, 0x9abc8bd5, 0x9e7d9662,
	0x933eb0bb, 0x97ffad0c, 0xafb010b1, 0xab710d06, 0xa6322bdf,
	0xa2f33668, 0xbcb4666d, 0xb8757bda, 0xb5365d03, 0xb1f740b4
                                                    };

                public override HashPhpResource Clone()
                {
                    var clone = new CRC32()
                    {
                        state = this.state
                    };
                    CloneHashState(clone);
                    return clone;
                }
                public override void Init()
                {
                    state = ~(uint)0;
                }
                public override bool Update(byte[] data)
                {
                    foreach (byte b in data)
                    {
                        state = (state << 8) ^ crc32_table[(state >> 24) ^ (b & 0xff)];
                    }

                    return true;
                }
                public override byte[] Final()
                {
                    state = ~state;
                    var h = BitConverter.GetBytes((uint)state);

                    state = 0;
                    return h;
                }
                public override int BlockSize { get { return 4; } }
            }
            public sealed class CRC32B : HashPhpResource
            {
                private uint state;

                private static uint[] crc32b_table = {
	0x00000000, 0x77073096, 0xee0e612c, 0x990951ba,
	0x076dc419, 0x706af48f, 0xe963a535, 0x9e6495a3,
	0x0edb8832, 0x79dcb8a4, 0xe0d5e91e, 0x97d2d988,
	0x09b64c2b, 0x7eb17cbd, 0xe7b82d07, 0x90bf1d91,
	0x1db71064, 0x6ab020f2, 0xf3b97148, 0x84be41de,
	0x1adad47d, 0x6ddde4eb, 0xf4d4b551, 0x83d385c7,
	0x136c9856, 0x646ba8c0, 0xfd62f97a, 0x8a65c9ec,
	0x14015c4f, 0x63066cd9, 0xfa0f3d63, 0x8d080df5,
	0x3b6e20c8, 0x4c69105e, 0xd56041e4, 0xa2677172,
	0x3c03e4d1, 0x4b04d447, 0xd20d85fd, 0xa50ab56b,
	0x35b5a8fa, 0x42b2986c, 0xdbbbc9d6, 0xacbcf940,
	0x32d86ce3, 0x45df5c75, 0xdcd60dcf, 0xabd13d59,
	0x26d930ac, 0x51de003a, 0xc8d75180, 0xbfd06116,
	0x21b4f4b5, 0x56b3c423, 0xcfba9599, 0xb8bda50f,
	0x2802b89e, 0x5f058808, 0xc60cd9b2, 0xb10be924,
	0x2f6f7c87, 0x58684c11, 0xc1611dab, 0xb6662d3d,
	0x76dc4190, 0x01db7106, 0x98d220bc, 0xefd5102a,
	0x71b18589, 0x06b6b51f, 0x9fbfe4a5, 0xe8b8d433,
	0x7807c9a2, 0x0f00f934, 0x9609a88e, 0xe10e9818,
	0x7f6a0dbb, 0x086d3d2d, 0x91646c97, 0xe6635c01,
	0x6b6b51f4, 0x1c6c6162, 0x856530d8, 0xf262004e,
	0x6c0695ed, 0x1b01a57b, 0x8208f4c1, 0xf50fc457,
	0x65b0d9c6, 0x12b7e950, 0x8bbeb8ea, 0xfcb9887c,
	0x62dd1ddf, 0x15da2d49, 0x8cd37cf3, 0xfbd44c65,
	0x4db26158, 0x3ab551ce, 0xa3bc0074, 0xd4bb30e2,
	0x4adfa541, 0x3dd895d7, 0xa4d1c46d, 0xd3d6f4fb,
	0x4369e96a, 0x346ed9fc, 0xad678846, 0xda60b8d0,
	0x44042d73, 0x33031de5, 0xaa0a4c5f, 0xdd0d7cc9,
	0x5005713c, 0x270241aa, 0xbe0b1010, 0xc90c2086,
	0x5768b525, 0x206f85b3, 0xb966d409, 0xce61e49f,
	0x5edef90e, 0x29d9c998, 0xb0d09822, 0xc7d7a8b4,
	0x59b33d17, 0x2eb40d81, 0xb7bd5c3b, 0xc0ba6cad,
	0xedb88320, 0x9abfb3b6, 0x03b6e20c, 0x74b1d29a,
	0xead54739, 0x9dd277af, 0x04db2615, 0x73dc1683,
	0xe3630b12, 0x94643b84, 0x0d6d6a3e, 0x7a6a5aa8,
	0xe40ecf0b, 0x9309ff9d, 0x0a00ae27, 0x7d079eb1,
	0xf00f9344, 0x8708a3d2, 0x1e01f268, 0x6906c2fe,
	0xf762575d, 0x806567cb, 0x196c3671, 0x6e6b06e7,
	0xfed41b76, 0x89d32be0, 0x10da7a5a, 0x67dd4acc,
	0xf9b9df6f, 0x8ebeeff9, 0x17b7be43, 0x60b08ed5,
	0xd6d6a3e8, 0xa1d1937e, 0x38d8c2c4, 0x4fdff252,
	0xd1bb67f1, 0xa6bc5767, 0x3fb506dd, 0x48b2364b,
	0xd80d2bda, 0xaf0a1b4c, 0x36034af6, 0x41047a60,
	0xdf60efc3, 0xa867df55, 0x316e8eef, 0x4669be79,
	0xcb61b38c, 0xbc66831a, 0x256fd2a0, 0x5268e236,
	0xcc0c7795, 0xbb0b4703, 0x220216b9, 0x5505262f,
	0xc5ba3bbe, 0xb2bd0b28, 0x2bb45a92, 0x5cb36a04,
	0xc2d7ffa7, 0xb5d0cf31, 0x2cd99e8b, 0x5bdeae1d,
	0x9b64c2b0, 0xec63f226, 0x756aa39c, 0x026d930a,
	0x9c0906a9, 0xeb0e363f, 0x72076785, 0x05005713,
	0x95bf4a82, 0xe2b87a14, 0x7bb12bae, 0x0cb61b38,
	0x92d28e9b, 0xe5d5be0d, 0x7cdcefb7, 0x0bdbdf21,
	0x86d3d2d4, 0xf1d4e242, 0x68ddb3f8, 0x1fda836e,
	0x81be16cd, 0xf6b9265b, 0x6fb077e1, 0x18b74777,
	0x88085ae6, 0xff0f6a70, 0x66063bca, 0x11010b5c,
	0x8f659eff, 0xf862ae69, 0x616bffd3, 0x166ccf45,
	0xa00ae278, 0xd70dd2ee, 0x4e048354, 0x3903b3c2,
	0xa7672661, 0xd06016f7, 0x4969474d, 0x3e6e77db,
	0xaed16a4a, 0xd9d65adc, 0x40df0b66, 0x37d83bf0,
	0xa9bcae53, 0xdebb9ec5, 0x47b2cf7f, 0x30b5ffe9,
	0xbdbdf21c, 0xcabac28a, 0x53b39330, 0x24b4a3a6,
	0xbad03605, 0xcdd70693, 0x54de5729, 0x23d967bf,
	0xb3667a2e, 0xc4614ab8, 0x5d681b02, 0x2a6f2b94,
	0xb40bbe37, 0xc30c8ea1, 0x5a05df1b, 0x2d02ef8d,
};

                public override HashPhpResource Clone()
                {
                    var clone = new CRC32B()
                    {
                        state = this.state
                    };
                    CloneHashState(clone);
                    return clone;
                }
                public override void Init()
                {
                    state = ~(uint)0;
                }
                public override bool Update(byte[] data)
                {
                    foreach (byte b in data)
                    {
                        state = (state >> 8) ^ crc32b_table[(state ^ b) & 0xff];
                    }

                    return true;
                }
                public override byte[] Final()
                {
                    state = ~state;

                    byte[] bytes = BitConverter.GetBytes((uint)state);
                    Array.Reverse(bytes);

                    state = 0;
                    return bytes;
                }
                public override int BlockSize { get { return 4; } }
            }
            public sealed class MD2 : HashPhpResource
            {
                private static byte[] MD2_S = {
	 41,  46,  67, 201, 162, 216, 124,   1,  61,  54,  84, 161, 236, 240,   6,  19,
	 98, 167,   5, 243, 192, 199, 115, 140, 152, 147,  43, 217, 188,  76, 130, 202,
	 30, 155,  87,  60, 253, 212, 224,  22, 103,  66, 111,  24, 138,  23, 229,  18,
	190,  78, 196, 214, 218, 158, 222,  73, 160, 251, 245, 142, 187,  47, 238, 122,
	169, 104, 121, 145,  21, 178,   7,  63, 148, 194,  16, 137,  11,  34,  95,  33,
	128, 127,  93, 154,  90, 144,  50,  39,  53,  62, 204, 231, 191, 247, 151,   3,
	255,  25,  48, 179,  72, 165, 181, 209, 215,  94, 146,  42, 172,  86, 170, 198,
	 79, 184,  56, 210, 150, 164, 125, 182, 118, 252, 107, 226, 156, 116,   4, 241,
	 69, 157, 112,  89, 100, 113, 135,  32, 134,  91, 207, 101, 230,  45, 168,   2,
	 27,  96,  37, 173, 174, 176, 185, 246,  28,  70,  97, 105,  52,  64, 126,  15,
	 85,  71, 163,  35, 221,  81, 175,  58, 195,  92, 249, 206, 186, 197, 234,  38,
	 44,  83,  13, 110, 133,  40, 132,   9, 211, 223, 205, 244,  65, 129,  77,  82,
	106, 220,  55, 200, 108, 193, 171, 250,  36, 225, 123,   8,  12, 189, 177,  74,
	120, 136, 149, 139, 227,  99, 232, 109, 233, 203, 213, 254,  59,   0,  29,  57,
	242, 239, 183,  14, 102,  88, 208, 228, 166, 119, 114, 248, 235, 117,  75,  10,
	 49,  68,  80, 180, 143, 237,  31,  26, 219, 153, 141,  51, 159,  17, 131,  20 };

                #region state

                private readonly byte[] state = new byte[48];
                private readonly byte[] checksum = new byte[16];
                
                #endregion

                public override HashPhpResource Clone()
                {
                    var clone = new MD2();

                    this.CloneHashState(clone);
                    this.CloneBufferedBlock(clone);
                    this.state.CopyTo(clone.state, 0);
                    this.checksum.CopyTo(clone.checksum, 0);
                                        
                    return clone;
                }

                public override void Init()
                {
                    this.ClearBufferedBlock();
                    Array.Clear(this.state, 0, this.state.Length);
                    Array.Clear(this.checksum, 0, this.checksum.Length);                    
                }

                private void TransformBlock(byte[]/*!*//*byte[16+startIndex]*/block, int startIndex)
                {
                    Debug.Assert(block != null);
                    Debug.Assert(block.Length >= 16 + startIndex);

                    byte i, j, t = 0;

                    for (i = 0; i < 16; i++)
                    {
                        state[16 + i] = block[i + startIndex];
                        state[32 + i] = (byte)(state[16 + i] ^ state[i]);
                    }

                    for (i = 0; i < 18; i++)
                    {
                        for (j = 0; j < 48; j++)
                        {
                            t = state[j] = (byte)(state[j] ^ MD2_S[t]);
                        }
                        unchecked 
                        {
                            t += i;
                        }
                    }

                    /* Update checksum -- must be after transform to avoid fouling up last message block */
                    t = checksum[15];
                    for (i = 0; i < 16; i++)
                    {
                        t = checksum[i] ^= MD2_S[block[i + startIndex] ^ t];
                    }
                }

                public override bool Update(byte[] data)
                {
                    foreach (var block in ProcessBlocked(data, 16))
                        TransformBlock(block.Item1, block.Item2);
                    
                    return true;
                }

                public override byte[] Final()
                {
                    // take the remaining buffer
                    // fill the rest up to 16 bytes with "remainingBytes" value
                    int bufferUsage;
                    byte[] buffer = GetBufferedBlock(out bufferUsage);
                    if (buffer == null) buffer = new byte[16];
                    Debug.Assert(buffer.Length == 16);
                    Debug.Assert(bufferUsage < 16);
                    byte remainingBytes = (byte)(16 - bufferUsage);
                    for (int i = bufferUsage; i < 16; ++i)
                        buffer[i] = remainingBytes;
                    
                    //
                    TransformBlock(buffer, 0);
                    TransformBlock(checksum, 0);

                    //
                    byte[] hash = new byte[16];
                    Array.Copy(this.state, 0, hash, 0, 16);
                    return hash;
                }

                public override int BlockSize { get { return 16; } }
            }
            public sealed class MD4 : HashPhpResource
            {
                #region state

                private uint[] state = new uint[4];
                private uint[] count = new uint[2];

                #endregion

                #region MD4 transformation
                private static uint MD4_F(uint x, uint y, uint z) { return ((z) ^ ((x) & ((y) ^ (z)))); }
                private static uint MD4_G(uint x, uint y, uint z) { return (((x) & ((y) | (z))) | ((y) & (z))); }
                private static uint MD4_H(uint x, uint y, uint z) { return ((x) ^ (y) ^ (z)); }

                private static uint ROTL32(byte s, uint v) { return (((v) << (s)) | ((v) >> (32 - (s)))); }

                private static void MD4_R1(ref uint a, uint b, uint c, uint d, uint xk, byte s) { unchecked { a = ROTL32(s, a + MD4_F(b, c, d) + xk); } }
                private static void MD4_R2(ref uint a, uint b, uint c, uint d, uint xk, byte s) { unchecked { a = ROTL32(s, a + MD4_G(b, c, d) + xk + 0x5A827999); } }
                private static void MD4_R3(ref uint a, uint b, uint c, uint d, uint xk, byte s) { unchecked { a = ROTL32(s, a + MD4_H(b, c, d) + xk + 0x6ED9EBA1); } }
                private static uint[] Decode(byte[] block, int startIndex, int bytesCount)
                {
                    Debug.Assert(bytesCount > 0);
                    Debug.Assert((bytesCount % 4) == 0);

                    uint[] result = new uint[bytesCount / 4];
                    int index = 0;
                    while (bytesCount > 0)
                    {
                        result[index++] = BitConverter.ToUInt32(block, startIndex);
                        startIndex += 4;
                        bytesCount -= 4;
                    }

                    return result;
                }
                private static byte[] Encode(uint[] nums, int startIndex, int bytesCount)
                {
                    Debug.Assert(bytesCount > 0);
                    Debug.Assert((bytesCount % 4) == 0);

                    byte[] result = new byte[bytesCount];

                    int index = 0;
                    while (index < bytesCount)
                    {
                        Array.Copy(BitConverter.GetBytes(nums[startIndex++]), 0, result, index, 4);
                        index += 4;
                    }

                    return result;
                }
                private void MD4Transform(byte[] block, int startIndex)
                {
                    uint a = state[0], b = state[1], c = state[2], d = state[3];
                    uint[] x = Decode(block, startIndex, 64);


                    /* Round 1 */
                    MD4_R1(ref a, b, c, d, x[0], 3);
                    MD4_R1(ref d, a, b, c, x[1], 7);
                    MD4_R1(ref c, d, a, b, x[2], 11);
                    MD4_R1(ref b, c, d, a, x[3], 19);
                    MD4_R1(ref a, b, c, d, x[4], 3);
                    MD4_R1(ref d, a, b, c, x[5], 7);
                    MD4_R1(ref c, d, a, b, x[6], 11);
                    MD4_R1(ref b, c, d, a, x[7], 19);
                    MD4_R1(ref a, b, c, d, x[8], 3);
                    MD4_R1(ref d, a, b, c, x[9], 7);
                    MD4_R1(ref c, d, a, b, x[10], 11);
                    MD4_R1(ref b, c, d, a, x[11], 19);
                    MD4_R1(ref a, b, c, d, x[12], 3);
                    MD4_R1(ref d, a, b, c, x[13], 7);
                    MD4_R1(ref c, d, a, b, x[14], 11);
                    MD4_R1(ref b, c, d, a, x[15], 19);

                    /* Round 2 */
                    MD4_R2(ref a, b, c, d, x[0], 3);
                    MD4_R2(ref d, a, b, c, x[4], 5);
                    MD4_R2(ref c, d, a, b, x[8], 9);
                    MD4_R2(ref b, c, d, a, x[12], 13);
                    MD4_R2(ref a, b, c, d, x[1], 3);
                    MD4_R2(ref d, a, b, c, x[5], 5);
                    MD4_R2(ref c, d, a, b, x[9], 9);
                    MD4_R2(ref b, c, d, a, x[13], 13);
                    MD4_R2(ref a, b, c, d, x[2], 3);
                    MD4_R2(ref d, a, b, c, x[6], 5);
                    MD4_R2(ref c, d, a, b, x[10], 9);
                    MD4_R2(ref b, c, d, a, x[14], 13);
                    MD4_R2(ref a, b, c, d, x[3], 3);
                    MD4_R2(ref d, a, b, c, x[7], 5);
                    MD4_R2(ref c, d, a, b, x[11], 9);
                    MD4_R2(ref b, c, d, a, x[15], 13);

                    /* Round 3 */
                    MD4_R3(ref a, b, c, d, x[0], 3);
                    MD4_R3(ref d, a, b, c, x[8], 9);
                    MD4_R3(ref c, d, a, b, x[4], 11);
                    MD4_R3(ref b, c, d, a, x[12], 15);
                    MD4_R3(ref a, b, c, d, x[2], 3);
                    MD4_R3(ref d, a, b, c, x[10], 9);
                    MD4_R3(ref c, d, a, b, x[6], 11);
                    MD4_R3(ref b, c, d, a, x[14], 15);
                    MD4_R3(ref a, b, c, d, x[1], 3);
                    MD4_R3(ref d, a, b, c, x[9], 9);
                    MD4_R3(ref c, d, a, b, x[5], 11);
                    MD4_R3(ref b, c, d, a, x[13], 15);
                    MD4_R3(ref a, b, c, d, x[3], 3);
                    MD4_R3(ref d, a, b, c, x[11], 9);
                    MD4_R3(ref c, d, a, b, x[7], 11);
                    MD4_R3(ref b, c, d, a, x[15], 15);

                    unchecked
                    {
                        state[0] += a;
                        state[1] += b;
                        state[2] += c;
                        state[3] += d;
                    }
                }
                #endregion

                #region HashPhpResource

                public override HashPhpResource Clone()
                {
                    var clone = new MD4();
                    this.CloneHashState(clone);
                    this.CloneBufferedBlock(clone);
                    this.state.CopyTo(clone.state, 0);
                    this.count.CopyTo(clone.count, 0);

                    return clone;
                }
                public override void Init()
                {
                    this.ClearBufferedBlock();

                    count[0] = count[1] = 0;
                    /* Load magic initialization constants.
                     */
                    state[0] = 0x67452301;
                    state[1] = 0xefcdab89;
                    state[2] = 0x98badcfe;
                    state[3] = 0x10325476;
                }
                public override bool Update(byte[] data)
                {
                    /* Update number of bits */
                    if ((count[0] += ((uint)data.Length << 3)) < ((uint)data.Length << 3))
                        ++count[1];
                    count[1] += ((uint)data.Length >> 29);

                    foreach (var block in ProcessBlocked(data, 64))
                        MD4Transform(block.Item1, block.Item2);
                    
                    return true;
                }
                public override byte[] Final()
                {
                    // save length
                    byte[] bits = Encode(count, 0, 8);

                    // padd to 56 mod 64
                    int bufferUsage;
                    byte[] buffer = GetBufferedBlock(out bufferUsage);
                    if (buffer == null) buffer = new byte[64];
                    Debug.Assert(buffer.Length == 64);
                    int padLen = (bufferUsage < 56) ? (56 - bufferUsage) : (120 - bufferUsage);
                    if (padLen > 0)
                    {
                        byte[] padding = new byte[padLen];
                        padding[0] = 0x80;
                        Update(padding);
                    }

                    Update(bits);

                    byte[] result = Encode(state, 0, 16);

                    // cleanup sensitive data
                    Array.Clear(state, 0, state.Length);
                    Array.Clear(count, 0, count.Length);

                    // done
                    return result;
                }
                public override int BlockSize { get { return 64; } }

                #endregion
            }
            public sealed class MD5 : HashPhpResource
            {
                #region state

                private uint[] state = new uint[4];
                private uint[] count = new uint[2];

                #endregion

                #region MD5 transformation

                /// <summary>
                /// MD5 Transformation constants.
                /// </summary>
                enum MD5Consts : byte
                {
                    S11 = 7,
                    S12 = 12,
                    S13 = 17,
                    S14 = 22,
                    S21 = 5,
                    S22 = 9,
                    S23 = 14,
                    S24 = 20,
                    S31 = 4,
                    S32 = 11,
                    S33 = 16,
                    S34 = 23,
                    S41 = 6,
                    S42 = 10,
                    S43 = 15,
                    S44 = 21,
                }
               
                /// F, G, H and I are basic MD5 functions.
                private static uint F(uint x, uint y, uint z) { return (((x) & (y)) | ((~x) & (z))); }
                private static uint G(uint x, uint y, uint z) { return (((x) & (z)) | ((y) & (~z))); }
                private static uint H(uint x, uint y, uint z) { return ((x) ^ (y) ^ (z)); }
                private static uint I(uint x, uint y, uint z) { return ((y) ^ ((x) | (~z))); }

                /// <summary>
                /// ROTATE_LEFT rotates x left n bits.
                /// </summary>
                /// <param name="x"></param>
                /// <param name="n"></param>
                /// <returns></returns>
                private static uint ROTATE_LEFT(uint x, byte n) { return (((x) << (n)) | ((x) >> (32 - (n)))); }

                /// FF, GG, HH, and II transformations for rounds 1, 2, 3, and 4.
                /// Rotation is separate from addition to prevent re-computation.
                private static void FF(ref uint a, uint b, uint c, uint d, uint x, byte s, uint ac)
                {
                    unchecked
                    {
                        (a) += F((b), (c), (d)) + (x) + (ac);
                        (a) = ROTATE_LEFT((a), (s));
                        (a) += (b);
                    }
                }
                private static void GG(ref uint a, uint b, uint c, uint d, uint x, byte s, uint ac)
                {
                    unchecked
                    {
                        (a) += G((b), (c), (d)) + (x) + (ac);
                        (a) = ROTATE_LEFT((a), (s));
                        (a) += (b);
                    }
                }
                private static void HH(ref uint a, uint b, uint c, uint d, uint x, byte s, uint ac)
                {
                    unchecked
                    {
                        (a) += H((b), (c), (d)) + (x) + (ac);
                        (a) = ROTATE_LEFT((a), (s));
                        (a) += (b);
                    }
                }
                private static void II(ref uint a, uint b, uint c, uint d, uint x, byte s, uint ac)
                {
                    unchecked
                    {
                        (a) += I((b), (c), (d)) + (x) + (ac);
                        (a) = ROTATE_LEFT((a), (s));
                        (a) += (b);
                    }
                }

                private static uint[] Decode(byte[] block, int startIndex, int bytesCount)
                {
                    Debug.Assert(bytesCount > 0);
                    Debug.Assert((bytesCount % 4) == 0);

                    uint[] result = new uint[bytesCount / 4];
                    int index = 0;
                    while (bytesCount > 0)
                    {
                        result[index++] = BitConverter.ToUInt32(block, startIndex);
                        startIndex += 4;
                        bytesCount -= 4;
                    }

                    return result;
                }
                private static byte[] Encode(uint[] nums, int startIndex, int bytesCount)
                {
                    Debug.Assert(bytesCount > 0);
                    Debug.Assert((bytesCount % 4) == 0);

                    byte[] result = new byte[bytesCount];

                    int index = 0;
                    while (index < bytesCount)
                    {
                        Array.Copy(BitConverter.GetBytes(nums[startIndex++]), 0, result, index, 4);
                        index += 4;
                    }

                    return result;
                }
                
                /// <summary>
                /// MD5 basic transformation. Transforms state based on block.
                /// </summary>
                /// <param name="block"></param>
                /// <param name="startIndex"></param>
                private void MD5Transform(byte[]/*[64]*/block, int startIndex)
                {
                    uint a = state[0], b = state[1], c = state[2], d = state[3];

                    uint[] x = Decode(block, startIndex, 64);   // [16]
                    Debug.Assert(x.Length == 16);

                    /* Round 1 */
                    FF(ref a, b, c, d, x[0], (byte)MD5Consts.S11, 0xd76aa478);	/* 1 */
                    FF(ref d, a, b, c, x[1], (byte)MD5Consts.S12, 0xe8c7b756);	/* 2 */
                    FF(ref c, d, a, b, x[2], (byte)MD5Consts.S13, 0x242070db);	/* 3 */
                    FF(ref b, c, d, a, x[3], (byte)MD5Consts.S14, 0xc1bdceee);	/* 4 */
                    FF(ref a, b, c, d, x[4], (byte)MD5Consts.S11, 0xf57c0faf);	/* 5 */
                    FF(ref d, a, b, c, x[5], (byte)MD5Consts.S12, 0x4787c62a);	/* 6 */
                    FF(ref c, d, a, b, x[6], (byte)MD5Consts.S13, 0xa8304613);	/* 7 */
                    FF(ref b, c, d, a, x[7], (byte)MD5Consts.S14, 0xfd469501);	/* 8 */
                    FF(ref a, b, c, d, x[8], (byte)MD5Consts.S11, 0x698098d8);	/* 9 */
                    FF(ref d, a, b, c, x[9], (byte)MD5Consts.S12, 0x8b44f7af);	/* 10 */
                    FF(ref c, d, a, b, x[10], (byte)MD5Consts.S13, 0xffff5bb1);		/* 11 */
                    FF(ref b, c, d, a, x[11], (byte)MD5Consts.S14, 0x895cd7be);		/* 12 */
                    FF(ref a, b, c, d, x[12], (byte)MD5Consts.S11, 0x6b901122);		/* 13 */
                    FF(ref d, a, b, c, x[13], (byte)MD5Consts.S12, 0xfd987193);		/* 14 */
                    FF(ref c, d, a, b, x[14], (byte)MD5Consts.S13, 0xa679438e);		/* 15 */
                    FF(ref b, c, d, a, x[15], (byte)MD5Consts.S14, 0x49b40821);		/* 16 */

                    /* Round 2 */
                    GG(ref a, b, c, d, x[1], (byte)MD5Consts.S21, 0xf61e2562);	/* 17 */
                    GG(ref d, a, b, c, x[6], (byte)MD5Consts.S22, 0xc040b340);	/* 18 */
                    GG(ref c, d, a, b, x[11], (byte)MD5Consts.S23, 0x265e5a51);		/* 19 */
                    GG(ref b, c, d, a, x[0], (byte)MD5Consts.S24, 0xe9b6c7aa);	/* 20 */
                    GG(ref a, b, c, d, x[5], (byte)MD5Consts.S21, 0xd62f105d);	/* 21 */
                    GG(ref d, a, b, c, x[10], (byte)MD5Consts.S22, 0x2441453);	/* 22 */
                    GG(ref c, d, a, b, x[15], (byte)MD5Consts.S23, 0xd8a1e681);		/* 23 */
                    GG(ref b, c, d, a, x[4], (byte)MD5Consts.S24, 0xe7d3fbc8);	/* 24 */
                    GG(ref a, b, c, d, x[9], (byte)MD5Consts.S21, 0x21e1cde6);	/* 25 */
                    GG(ref d, a, b, c, x[14], (byte)MD5Consts.S22, 0xc33707d6);		/* 26 */
                    GG(ref c, d, a, b, x[3], (byte)MD5Consts.S23, 0xf4d50d87);	/* 27 */
                    GG(ref b, c, d, a, x[8], (byte)MD5Consts.S24, 0x455a14ed);	/* 28 */
                    GG(ref a, b, c, d, x[13], (byte)MD5Consts.S21, 0xa9e3e905);		/* 29 */
                    GG(ref d, a, b, c, x[2], (byte)MD5Consts.S22, 0xfcefa3f8);	/* 30 */
                    GG(ref c, d, a, b, x[7], (byte)MD5Consts.S23, 0x676f02d9);	/* 31 */
                    GG(ref b, c, d, a, x[12], (byte)MD5Consts.S24, 0x8d2a4c8a);		/* 32 */

                    /* Round 3 */
                    HH(ref a, b, c, d, x[5], (byte)MD5Consts.S31, 0xfffa3942);	/* 33 */
                    HH(ref d, a, b, c, x[8], (byte)MD5Consts.S32, 0x8771f681);	/* 34 */
                    HH(ref c, d, a, b, x[11], (byte)MD5Consts.S33, 0x6d9d6122);		/* 35 */
                    HH(ref b, c, d, a, x[14], (byte)MD5Consts.S34, 0xfde5380c);		/* 36 */
                    HH(ref a, b, c, d, x[1], (byte)MD5Consts.S31, 0xa4beea44);	/* 37 */
                    HH(ref d, a, b, c, x[4], (byte)MD5Consts.S32, 0x4bdecfa9);	/* 38 */
                    HH(ref c, d, a, b, x[7], (byte)MD5Consts.S33, 0xf6bb4b60);	/* 39 */
                    HH(ref b, c, d, a, x[10], (byte)MD5Consts.S34, 0xbebfbc70);		/* 40 */
                    HH(ref a, b, c, d, x[13], (byte)MD5Consts.S31, 0x289b7ec6);		/* 41 */
                    HH(ref d, a, b, c, x[0], (byte)MD5Consts.S32, 0xeaa127fa);	/* 42 */
                    HH(ref c, d, a, b, x[3], (byte)MD5Consts.S33, 0xd4ef3085);	/* 43 */
                    HH(ref b, c, d, a, x[6], (byte)MD5Consts.S34, 0x4881d05);	/* 44 */
                    HH(ref a, b, c, d, x[9], (byte)MD5Consts.S31, 0xd9d4d039);	/* 45 */
                    HH(ref d, a, b, c, x[12], (byte)MD5Consts.S32, 0xe6db99e5);		/* 46 */
                    HH(ref c, d, a, b, x[15], (byte)MD5Consts.S33, 0x1fa27cf8);		/* 47 */
                    HH(ref b, c, d, a, x[2], (byte)MD5Consts.S34, 0xc4ac5665);	/* 48 */

                    /* Round 4 */
                    II(ref a, b, c, d, x[0], (byte)MD5Consts.S41, 0xf4292244);	/* 49 */
                    II(ref d, a, b, c, x[7], (byte)MD5Consts.S42, 0x432aff97);	/* 50 */
                    II(ref c, d, a, b, x[14], (byte)MD5Consts.S43, 0xab9423a7);		/* 51 */
                    II(ref b, c, d, a, x[5], (byte)MD5Consts.S44, 0xfc93a039);	/* 52 */
                    II(ref a, b, c, d, x[12], (byte)MD5Consts.S41, 0x655b59c3);		/* 53 */
                    II(ref d, a, b, c, x[3], (byte)MD5Consts.S42, 0x8f0ccc92);	/* 54 */
                    II(ref c, d, a, b, x[10], (byte)MD5Consts.S43, 0xffeff47d);		/* 55 */
                    II(ref b, c, d, a, x[1], (byte)MD5Consts.S44, 0x85845dd1);	/* 56 */
                    II(ref a, b, c, d, x[8], (byte)MD5Consts.S41, 0x6fa87e4f);	/* 57 */
                    II(ref d, a, b, c, x[15], (byte)MD5Consts.S42, 0xfe2ce6e0);		/* 58 */
                    II(ref c, d, a, b, x[6], (byte)MD5Consts.S43, 0xa3014314);	/* 59 */
                    II(ref b, c, d, a, x[13], (byte)MD5Consts.S44, 0x4e0811a1);		/* 60 */
                    II(ref a, b, c, d, x[4], (byte)MD5Consts.S41, 0xf7537e82);	/* 61 */
                    II(ref d, a, b, c, x[11], (byte)MD5Consts.S42, 0xbd3af235);		/* 62 */
                    II(ref c, d, a, b, x[2], (byte)MD5Consts.S43, 0x2ad7d2bb);	/* 63 */
                    II(ref b, c, d, a, x[9], (byte)MD5Consts.S44, 0xeb86d391);	/* 64 */

                    unchecked
                    {
                        state[0] += a;
                        state[1] += b;
                        state[2] += c;
                        state[3] += d;
                    }

                    Array.Clear(x, 0, 16);
                }

                #endregion

                #region HashPhpResource

                public override HashPhpResource Clone()
                {
                    var clone = new MD5();
                    this.CloneHashState(clone);
                    this.CloneBufferedBlock(clone);
                    this.state.CopyTo(clone.state, 0);
                    this.count.CopyTo(clone.count, 0);

                    return clone;
                }
                public override void Init()
                {
                    count[0] = count[1] = 0;
                    /* Load magic initialization constants.
                     */
                    state[0] = 0x67452301;
                    state[1] = 0xefcdab89;
                    state[2] = 0x98badcfe;
                    state[3] = 0x10325476;
                }
                public override bool Update(byte[]/*!*/data)
                {
                    Debug.Assert(data != null);

                    // Update number of bits
                    if ((count[0] += ((uint)data.Length << 3)) < ((uint)data.Length << 3)) count[1]++;
                    count[1] += ((uint)data.Length >> 29);

                    // Transform blocks of 64 bytes
                    foreach (var block in ProcessBlocked(data, 64))
                        MD5Transform(block.Item1, block.Item2);

                    return true;
                }
                public override byte[] Final()
                {
                    // save length
                    byte[] bits = Encode(count, 0, 8);

                    // padd to 56 mod 64
                    int bufferUsage;
                    byte[] buffer = GetBufferedBlock(out bufferUsage);
                    if (buffer == null) buffer = new byte[64];
                    Debug.Assert(buffer.Length == 64);
                    int padLen = (bufferUsage < 56) ? (56 - bufferUsage) : (120 - bufferUsage);
                    if (padLen > 0)
                    {
                        byte[] padding = new byte[padLen];
                        padding[0] = 0x80;
                        Update(padding);
                    }

                    Update(bits);

                    byte[] result = Encode(state, 0, 16);

                    // cleanup sensitive data
                    Array.Clear(state, 0, state.Length);
                    Array.Clear(count, 0, count.Length);

                    // done
                    return result;
                }
                public override int BlockSize
                {
                    get { return 64; }
                }

                #endregion
            }
            /// <summary>
            /// Base class for SHA based hashing algorithms.
            /// </summary>
            /// <typeparam name="T">Actual type of SHA class. Used to instantiate new one in <see cref="Clone"/> method.</typeparam>
            public abstract class SHA<T> : HashPhpResource where T: SHA<T>, new()
            {
                #region state

                /// <summary>
                /// Internal buffer holding SHA results.
                /// </summary>
                protected readonly byte[]/*!*/_buffer;
                /// <summary>
                /// Amount of chars encoded.
                /// </summary>
                protected long _count;
                /// <summary>
                /// Temporary buffer used internally by <see cref="_HashData"/> method.
                /// </summary>
                protected readonly uint[]/*!*/_tmp;
                /// <summary>
                /// Current hash state.
                /// </summary>
                protected readonly uint[]/*!*/_state;

                #endregion

                #region Constructor

                public SHA(int bufferSize, int tmpSize, int stateSize)
                {
                    Debug.Assert(bufferSize > 0);
                    Debug.Assert(tmpSize > 0);
                    Debug.Assert(stateSize > 0);

                    this._buffer = new byte[bufferSize];
                    this._tmp = new uint[tmpSize];
                    this._state = new uint[stateSize];

                    // initialize the state:
                    this.InitializeState();
                }

                #endregion

                #region SHA (to be implemented in derived class)

                /// <summary>
                /// Set <see cref="_count"/> and <see cref="_state"/> to their initial state.
                /// </summary>
                protected abstract void InitializeState();

                /// <summary>
                /// Finalize the hash.
                /// </summary>
                /// <returns>Resulting hash.</returns>
                protected abstract byte[] _EndHash();

                /// <summary>
                /// Pump more data into the hashing algorithm.
                /// </summary>
                /// <param name="partIn">Array of data to be hashed.</param>
                /// <param name="ibStart">Index where to start reading from <paramref name="partIn"/>.</param>
                /// <param name="cbSize">Amount of bytes to read from <paramref name="partIn"/>.</param>
                /// <returns><c>true</c> if hashing succeeded.</returns>
                protected abstract bool _HashData(byte[] partIn, int ibStart, int cbSize);

                #endregion

                #region HashPhpResource

                public override void Init()
                {
                    if (_state != null /*&& _buffer != null && _tmp != null*/) // iff we are initialized already (base .ctor which calls Init is called before this .ctor, so these arrays may not be initialized yet)
                    {
                        this.InitializeState();
                        Array.Clear(this._buffer, 0, this._buffer.Length);
                        Array.Clear(this._tmp, 0, this._tmp.Length);
                    }
                }

                public override bool Update(byte[]/*!*/data)
                {
                    Debug.Assert(data != null);

                    return _HashData(data, 0, data.Length);
                }

                public override byte[] Final()
                {
                    try
                    {
                        return _EndHash();
                    }
                    finally
                    {
                        InitializeState();  // clear the state
                    }
                }

                public override HashPhpResource Clone()
                {
                    var clone = new T() { _count = this._count };
                    this.CloneHashState(clone);

                    this._buffer.CopyTo(clone._buffer, 0);
                    this._tmp.CopyTo(clone._tmp, 0);
                    this._state.CopyTo(clone._state, 0);

                    return clone;
                }

                public override int BlockSize
                {
                    get { return _buffer.Length; }
                }

                #endregion
            }
            public sealed class SHA1 : SHA<SHA1>
            {
                #region SHA1 hashing internals

                public SHA1()
                    :base(64,80,5)
                {
                }

                protected override void InitializeState()
                {
                    this._count = 0L;
                    this._state[0] = 0x67452301;
                    this._state[1] = 0xefcdab89;
                    this._state[2] = 0x98badcfe;
                    this._state[3] = 0x10325476;
                    this._state[4] = 0xc3d2e1f0;
                }

                /// <summary>
                /// Finalize the hash.
                /// </summary>
                /// <returns>Hashed bytes.</returns>
                protected override byte[] _EndHash()
                {
                    byte[] block = new byte[20];
                    int num = 0x40 - ((int)(this._count & 0x3fL));
                    if (num <= 8)
                        num += 0x40;

                    byte[] partIn = new byte[num];
                    partIn[0] = 0x80;
                    long num2 = this._count * 8L;
                    partIn[num - 8] = (byte)((num2 >> 0x38) & 0xffL);
                    partIn[num - 7] = (byte)((num2 >> 0x30) & 0xffL);
                    partIn[num - 6] = (byte)((num2 >> 40) & 0xffL);
                    partIn[num - 5] = (byte)((num2 >> 0x20) & 0xffL);
                    partIn[num - 4] = (byte)((num2 >> 0x18) & 0xffL);
                    partIn[num - 3] = (byte)((num2 >> 0x10) & 0xffL);
                    partIn[num - 2] = (byte)((num2 >> 8) & 0xffL);
                    partIn[num - 1] = (byte)(num2 & 0xffL);
                    this._HashData(partIn, 0, partIn.Length);
                    DWORDToBigEndian(block, this._state, 5);
                    //base.HashValue = block;
                    return block;
                }

                protected override bool _HashData(byte[] partIn, int ibStart, int cbSize)
                {
                    unchecked
                    {
                        int byteCount = cbSize;
                        int srcOffsetBytes = ibStart;
                        int dstOffsetBytes = (int)(this._count & 0x3fL);
                        this._count += byteCount;

                        if ((dstOffsetBytes > 0) && ((dstOffsetBytes + byteCount) >= 0x40))
                        {
                            Buffer.BlockCopy(partIn, srcOffsetBytes, this._buffer, dstOffsetBytes, 0x40 - dstOffsetBytes);
                            srcOffsetBytes += 0x40 - dstOffsetBytes;
                            byteCount -= 0x40 - dstOffsetBytes;
                            SHATransform(_tmp, _state, _buffer);
                            dstOffsetBytes = 0;
                        }
                        while (byteCount >= 0x40)
                        {
                            Buffer.BlockCopy(partIn, srcOffsetBytes, this._buffer, 0, 0x40);
                            srcOffsetBytes += 0x40;
                            byteCount -= 0x40;
                            SHATransform(_tmp, _state, _buffer);
                        }
                        if (byteCount > 0)
                        {
                            Buffer.BlockCopy(partIn, srcOffsetBytes, this._buffer, dstOffsetBytes, byteCount);
                        }
                    }

                    return true;
                }

                private static void SHAExpand(uint[] x)
                {
                    unchecked
                    {
                        for (int i = 0x10; i < 80; i++)
                        {
                            uint num2 = ((x[i - 3] ^ x[i - 8]) ^ x[i - 14]) ^ x[i - 0x10];
                            x[i] = (num2 << 1) | (num2 >> 0x1f);
                        }
                    }
                }

                private static void SHATransform(uint[] tmp, uint[] state, byte[] block)
                {
                    Debug.Assert(tmp != null && tmp.Length == 80);

                    uint num = state[0];
                    uint num2 = state[1];
                    uint num3 = state[2];
                    uint num4 = state[3];
                    uint num5 = state[4];
                    DWORDFromBigEndian(tmp, 0x10, block);
                    SHAExpand(tmp);
                    int index = 0;

                    unchecked
                    {

                        while (index < 20)
                        {
                            num5 += ((((num << 5) | (num >> 0x1b)) + (num4 ^ (num2 & (num3 ^ num4)))) + tmp[index]) + 0x5a827999;
                            num2 = (num2 << 30) | (num2 >> 2);
                            num4 += ((((num5 << 5) | (num5 >> 0x1b)) + (num3 ^ (num & (num2 ^ num3)))) + tmp[index + 1]) + 0x5a827999;
                            num = (num << 30) | (num >> 2);
                            num3 += ((((num4 << 5) | (num4 >> 0x1b)) + (num2 ^ (num5 & (num ^ num2)))) + tmp[index + 2]) + 0x5a827999;
                            num5 = (num5 << 30) | (num5 >> 2);
                            num2 += ((((num3 << 5) | (num3 >> 0x1b)) + (num ^ (num4 & (num5 ^ num)))) + tmp[index + 3]) + 0x5a827999;
                            num4 = (num4 << 30) | (num4 >> 2);
                            num += ((((num2 << 5) | (num2 >> 0x1b)) + (num5 ^ (num3 & (num4 ^ num5)))) + tmp[index + 4]) + 0x5a827999;
                            num3 = (num3 << 30) | (num3 >> 2);
                            index += 5;
                        }
                        while (index < 40)
                        {
                            num5 += ((((num << 5) | (num >> 0x1b)) + ((num2 ^ num3) ^ num4)) + tmp[index]) + 0x6ed9eba1;
                            num2 = (num2 << 30) | (num2 >> 2);
                            num4 += ((((num5 << 5) | (num5 >> 0x1b)) + ((num ^ num2) ^ num3)) + tmp[index + 1]) + 0x6ed9eba1;
                            num = (num << 30) | (num >> 2);
                            num3 += ((((num4 << 5) | (num4 >> 0x1b)) + ((num5 ^ num) ^ num2)) + tmp[index + 2]) + 0x6ed9eba1;
                            num5 = (num5 << 30) | (num5 >> 2);
                            num2 += ((((num3 << 5) | (num3 >> 0x1b)) + ((num4 ^ num5) ^ num)) + tmp[index + 3]) + 0x6ed9eba1;
                            num4 = (num4 << 30) | (num4 >> 2);
                            num += ((((num2 << 5) | (num2 >> 0x1b)) + ((num3 ^ num4) ^ num5)) + tmp[index + 4]) + 0x6ed9eba1;
                            num3 = (num3 << 30) | (num3 >> 2);
                            index += 5;
                        }
                        while (index < 60)
                        {
                            num5 += ((((num << 5) | (num >> 0x1b)) + ((num2 & num3) | (num4 & (num2 | num3)))) + tmp[index]) + 0x8f1bbcdc;
                            num2 = (num2 << 30) | (num2 >> 2);
                            num4 += ((((num5 << 5) | (num5 >> 0x1b)) + ((num & num2) | (num3 & (num | num2)))) + tmp[index + 1]) + 0x8f1bbcdc;
                            num = (num << 30) | (num >> 2);
                            num3 += ((((num4 << 5) | (num4 >> 0x1b)) + ((num5 & num) | (num2 & (num5 | num)))) + tmp[index + 2]) + 0x8f1bbcdc;
                            num5 = (num5 << 30) | (num5 >> 2);
                            num2 += ((((num3 << 5) | (num3 >> 0x1b)) + ((num4 & num5) | (num & (num4 | num5)))) + tmp[index + 3]) + 0x8f1bbcdc;
                            num4 = (num4 << 30) | (num4 >> 2);
                            num += ((((num2 << 5) | (num2 >> 0x1b)) + ((num3 & num4) | (num5 & (num3 | num4)))) + tmp[index + 4]) + 0x8f1bbcdc;
                            num3 = (num3 << 30) | (num3 >> 2);
                            index += 5;
                        }
                        while (index < 80)
                        {
                            num5 += ((((num << 5) | (num >> 0x1b)) + ((num2 ^ num3) ^ num4)) + tmp[index]) + 0xca62c1d6;
                            num2 = (num2 << 30) | (num2 >> 2);
                            num4 += ((((num5 << 5) | (num5 >> 0x1b)) + ((num ^ num2) ^ num3)) + tmp[index + 1]) + 0xca62c1d6;
                            num = (num << 30) | (num >> 2);
                            num3 += ((((num4 << 5) | (num4 >> 0x1b)) + ((num5 ^ num) ^ num2)) + tmp[index + 2]) + 0xca62c1d6;
                            num5 = (num5 << 30) | (num5 >> 2);
                            num2 += ((((num3 << 5) | (num3 >> 0x1b)) + ((num4 ^ num5) ^ num)) + tmp[index + 3]) + 0xca62c1d6;
                            num4 = (num4 << 30) | (num4 >> 2);
                            num += ((((num2 << 5) | (num2 >> 0x1b)) + ((num3 ^ num4) ^ num5)) + tmp[index + 4]) + 0xca62c1d6;
                            num3 = (num3 << 30) | (num3 >> 2);
                            index += 5;
                        }
                        state[0] += num;
                        state[1] += num2;
                        state[2] += num3;
                        state[3] += num4;
                        state[4] += num5;

                    }
                }

                #endregion
            }
            public sealed class SHA256 : SHA<SHA256>
            {
                #region SHA256 hashing internals

                public SHA256()
                    :base(64,64,8)
                {                    
                }

                protected override void InitializeState()
                {
                    this._count = 0L;

                    // initialize the state:
                    this._state[0] = 0x6a09e667;
                    this._state[1] = 0xbb67ae85;
                    this._state[2] = 0x3c6ef372;
                    this._state[3] = 0xa54ff53a;
                    this._state[4] = 0x510e527f;
                    this._state[5] = 0x9b05688c;
                    this._state[6] = 0x1f83d9ab;
                    this._state[7] = 0x5be0cd19;
                }

                /// <summary>
                /// Finalize the hash.
                /// </summary>
                /// <returns>Hashed bytes.</returns>
                protected override byte[] _EndHash()
                {
                    byte[] block = new byte[32];

                    int num = 0x40 - ((int)(this._count & 0x3fL));
                    if (num <= 8)
                        num += 0x40;

                    byte[] partIn = new byte[num];
                    partIn[0] = 0x80;
                    long num2 = this._count * 8L;
                    partIn[num - 8] = (byte)((num2 >> 0x38) & 0xffL);
                    partIn[num - 7] = (byte)((num2 >> 0x30) & 0xffL);
                    partIn[num - 6] = (byte)((num2 >> 40) & 0xffL);
                    partIn[num - 5] = (byte)((num2 >> 0x20) & 0xffL);
                    partIn[num - 4] = (byte)((num2 >> 0x18) & 0xffL);
                    partIn[num - 3] = (byte)((num2 >> 0x10) & 0xffL);
                    partIn[num - 2] = (byte)((num2 >> 8) & 0xffL);
                    partIn[num - 1] = (byte)(num2 & 0xffL);
                    this._HashData(partIn, 0, partIn.Length);
                    DWORDToBigEndian(block, this._state, 8);
                    //base.HashValue = block;
                    return block;
                }

                protected override bool _HashData(byte[] partIn, int ibStart, int cbSize)
                {
                    unchecked
                    {
                        int byteCount = cbSize;
                        int srcOffsetBytes = ibStart;
                        int dstOffsetBytes = (int)(this._count & 0x3fL);
                        this._count += byteCount;

                        if ((dstOffsetBytes > 0) && ((dstOffsetBytes + byteCount) >= 0x40))
                        {
                            Buffer.BlockCopy(partIn, srcOffsetBytes, this._buffer, dstOffsetBytes, 0x40 - dstOffsetBytes);
                            srcOffsetBytes += 0x40 - dstOffsetBytes;
                            byteCount -= 0x40 - dstOffsetBytes;
                            SHATransform(_tmp, _state, _buffer);
                            dstOffsetBytes = 0;
                        }
                        while (byteCount >= 0x40)
                        {
                            Buffer.BlockCopy(partIn, srcOffsetBytes, this._buffer, 0, 0x40);
                            srcOffsetBytes += 0x40;
                            byteCount -= 0x40;
                            SHATransform(_tmp, _state, _buffer);
                        }
                        if (byteCount > 0)
                        {
                            Buffer.BlockCopy(partIn, srcOffsetBytes, this._buffer, dstOffsetBytes, byteCount);
                        }
                    }

                    return true;
                }

                #region SHA256 internals
                private static uint ROTR32(int b, uint x) { return unchecked((x >> b) | (x << (32 - b))); }
                private static uint SHR(int b, uint x) { return unchecked(x >> b); }

                private static uint SHA256_F0(uint x, uint y, uint z) { return unchecked(((x) & (y)) ^ ((~(x)) & (z))); }
                private static uint SHA256_F1(uint x, uint y, uint z) { return unchecked(((x) & (y)) ^ ((x) & (z)) ^ ((y) & (z))); }
                private static uint SHA256_F2(uint x) { return unchecked(ROTR32(2, (x)) ^ ROTR32(13, (x)) ^ ROTR32(22, (x))); }
                private static uint SHA256_F3(uint x) { return unchecked(ROTR32(6, (x)) ^ ROTR32(11, (x)) ^ ROTR32(25, (x))); }
                private static uint SHA256_F4(uint x) { return unchecked(ROTR32(7, (x)) ^ ROTR32(18, (x)) ^ SHR(3, (x))); }
                private static uint SHA256_F5(uint x) { return unchecked(ROTR32(17, (x)) ^ ROTR32(19, (x)) ^ SHR(10, (x))); }
                static readonly uint[] SHA256_K = new uint[]{
	                0x428a2f98, 0x71374491, 0xb5c0fbcf, 0xe9b5dba5, 0x3956c25b, 0x59f111f1, 0x923f82a4, 0xab1c5ed5,
	                0xd807aa98, 0x12835b01, 0x243185be, 0x550c7dc3, 0x72be5d74, 0x80deb1fe, 0x9bdc06a7, 0xc19bf174,
	                0xe49b69c1, 0xefbe4786, 0x0fc19dc6, 0x240ca1cc, 0x2de92c6f, 0x4a7484aa, 0x5cb0a9dc, 0x76f988da,
	                0x983e5152, 0xa831c66d, 0xb00327c8, 0xbf597fc7, 0xc6e00bf3, 0xd5a79147, 0x06ca6351, 0x14292967,
	                0x27b70a85, 0x2e1b2138, 0x4d2c6dfc, 0x53380d13, 0x650a7354, 0x766a0abb, 0x81c2c92e, 0x92722c85,
	                0xa2bfe8a1, 0xa81a664b, 0xc24b8b70, 0xc76c51a3, 0xd192e819, 0xd6990624, 0xf40e3585, 0x106aa070,
	                0x19a4c116, 0x1e376c08, 0x2748774c, 0x34b0bcb5, 0x391c0cb3, 0x4ed8aa4a, 0x5b9cca4f, 0x682e6ff3,
	                0x748f82ee, 0x78a5636f, 0x84c87814, 0x8cc70208, 0x90befffa, 0xa4506ceb, 0xbef9a3f7, 0xc67178f2
                };
                #endregion

                private static void SHATransform(uint[] tmp, uint[] state, byte[] block)
                {
                    Debug.Assert(tmp != null && tmp.Length == 64);

                    unchecked
                    {
                        uint a = state[0], b = state[1], c = state[2], d = state[3], e = state[4], f = state[5], g = state[6], h = state[7];

                        DWORDFromBigEndian(tmp, 0x10, block);

                        for (int i = 16; i < 64; i++)
                            tmp[i] = SHA256_F5(tmp[i - 2]) + tmp[i - 7] + SHA256_F4(tmp[i - 15]) + tmp[i - 16];

                        for (int i = 0; i < 64; i++)
                        {
                            uint T1 = h + SHA256_F3(e) + SHA256_F0(e, f, g) + SHA256_K[i] + tmp[i];
                            uint T2 = SHA256_F2(a) + SHA256_F1(a, b, c);

                            h = g;
                            g = f;
                            f = e;
                            e = d + T1;
                            d = c;
                            c = b;
                            b = a;
                            a = T1 + T2;
                        }

                        state[0] += a;
                        state[1] += b;
                        state[2] += c;
                        state[3] += d;
                        state[4] += e;
                        state[5] += f;
                        state[6] += g;
                        state[7] += h;
                    }
                }


                #endregion
            }
 
            #endregion
        }

        #endregion

        #region hash_algos

        /// <summary>
        /// Get an array of available hashing algorithms. These names can be used in hash* functions, as the "algo" argument.
        /// </summary>
        /// <returns>Zero-based indexed array of names of hashing algorithms.</returns>
        [ImplementsFunction("hash_algos")]
        public static PhpArray GetHashAlgorithms()
        {
            var algos = HashPhpResource.HashAlgorithms;
            var result = new PhpArray(algos.Count, 0);

            foreach (var x in algos)
                result.Add(x.Key);

            return result;
        }

        #endregion

        #region options

        /// <summary>
        /// hash_init() options
        /// </summary>
        [Flags]
        public enum HashInitOptions
        {
            /// <summary>
            /// No options.
            /// Default.
            /// </summary>
            [ImplementsConstant("HASH_DEFAULT")]
            HASH_DEFAULT = 0,

            /// <summary>
            /// Use HMAC. The key must be provided.
            /// </summary>
            [ImplementsConstant("HASH_HMAC")]
            HASH_HMAC = 1,
        }

        #endregion

        #region hash_copy, hash_init, hash_update, hash_final, hash_update_file, hash_update_stream

        [ImplementsFunction("hash_copy")]
        public static PhpResource HashCopy(PhpResource context)
        {
            HashPhpResource h = context as HashPhpResource;

            if (h == null)
            {
                PhpException.InvalidArgumentType("context", PhpResource.PhpTypeName);
                return null;
            }

            return h.Clone();
        }

        [ImplementsFunction("hash_init")]
        public static PhpResource HashInit(string algo)
        {
            return HashInit(algo, HashInitOptions.HASH_DEFAULT, null);
        }
        [ImplementsFunction("hash_init")]
        public static PhpResource HashInit(string algo, HashInitOptions options)
        {
            return HashInit(algo, options, null);
        }
        [ImplementsFunction("hash_init")]
        public static PhpResource HashInit(string algo, HashInitOptions options, PhpBytes key)
        {
            bool hmac = (options & HashInitOptions.HASH_HMAC) != 0;

            if (hmac && (key == null || key.Length == 0))
            {
                PhpException.Throw(PhpError.Warning, "HMAC requested without a key");   // TODO: to resources
                return null;
            }

            HashPhpResource.HashAlgFactory algFactory;
            if (!HashPhpResource.HashAlgorithms.TryGetValue(algo, out algFactory))
            {
                PhpException.Throw(PhpError.Warning, "Unknown hashing algorithm: " + algo);   // TODO: to resources
                return null;
            }

            //
            // create the hashing algorithm context
            //
            HashPhpResource h = algFactory();
            h.options = options;

            //
            // HMAC
            //
            if (hmac)
            {
                // Take the given key and hash it in the context of newly created hashing context.

                Debug.Assert(h.BlockSize > 0);
                byte[] K = key.ReadonlyData;

                if (K.Length > h.BlockSize)
                {
                    // provided key is too long, hash it to obtain shorter key
                    h.Update(K);
                    K = h.Final();
                    h.Init();// restart the algorithm
                }
                else
                {
                    K = (byte[])K.Clone();
                }

                if (K.Length != h.BlockSize)
                {
                    Debug.Assert(K.Length < h.BlockSize);
                    byte[] KAligned = new byte[h.BlockSize];
                    K.CopyTo(KAligned, 0);
                    K = KAligned;
                }

                for (int i = 0; i < K.Length; ++i)
                    K[i] ^= 0x36;

                h.Update(K);
                h.HMACkey = K;
            }

            return h;
        }

        [ImplementsFunction("hash_update")]
        public static bool HashUpdate(PhpResource context, PhpBytes data)
        {
            HashPhpResource h = context as HashPhpResource;

            if (h == null)
            {
                PhpException.InvalidArgumentType("context", PhpResource.PhpTypeName);
                return false;
            }

            h.Update(data.ReadonlyData);

            return true;
        }

        [ImplementsFunction("hash_final")]
        public static object HashFinal(PhpResource context)
        {
            return HashFinal(context, false);
        }
        [ImplementsFunction("hash_final")]
        public static object HashFinal(PhpResource context, bool raw_output)
        {
            HashPhpResource h = context as HashPhpResource;

            if (h == null)
            {
                PhpException.InvalidArgumentType("context", PhpResource.PhpTypeName);
                return null;
            }

            byte[] hash = h.Final();

            //
            // HMAC
            //
            if (/*(h.options & HashInitOptions.HASH_HMAC) != 0 &&*/ h.HMACkey != null)
            {
                /* Convert K to opad -- 0x6A = 0x36 ^ 0x5C */
                byte[] K = h.HMACkey;
                for (int i = 0; i < K.Length; ++i)
                    K[i] ^= 0x6A;

                /* Feed this result into the outter hash */
                h.Init();
                h.Update(K);
                h.Update(hash);
                hash = h.Final();

                /* Zero the key */
                //Array.Clear(K, 0, K.Length);
                h.HMACkey = null;
            }

            //
            // output
            //
            if (raw_output)
                return new PhpBytes(hash);
            else
                return StringUtils.BinToHex(hash, null);
        }

        [ImplementsFunction("hash_update_file")]
        public static bool HashUpdateFromFile(PhpResource context, string filename)
        {
            return HashUpdateFromFile(context, filename, StreamContext.Default);
        }
        [ImplementsFunction("hash_update_file")]
        public static bool HashUpdateFromFile(PhpResource context, string filename, PhpResource stream_context)
        {
            // hashing context
            HashPhpResource h = context as HashPhpResource;

            if (h == null)
            {
                PhpException.InvalidArgumentType("context", PhpResource.PhpTypeName);
                return false;
            }

            // stream context
            StreamContext sc = StreamContext.GetValid(stream_context);
            if (sc == null)
                return false;

            // read data from file (or URL)
            using (PhpStream stream = PhpStream.Open(filename, "rb", StreamOpenOptions.Empty, sc))
            {
                if (stream == null)
                    return false;

                if (HashUpdateFromStream(h, stream, -1) < 0)
                    return false;
            }

            //
            return true;
        }

        [ImplementsFunction("hash_update_stream")]
        [return: CastToFalse]
        public static int HashUpdateFromStream(PhpResource context, PhpResource handle)
        {
            return HashUpdateFromStream(context, handle, -1);
        }
        [ImplementsFunction("hash_update_stream")]
        [return: CastToFalse]
        public static int HashUpdateFromStream(PhpResource context, PhpResource handle, int length/*=-1*/)
        {
            // hashing context
            HashPhpResource h = context as HashPhpResource;

            if (h == null)
            {
                PhpException.InvalidArgumentType("context", PhpResource.PhpTypeName);
                return -1;
            }

            PhpStream stream = PhpStream.GetValid(handle);
            if (stream == null)
                return -1;

            // read data from stream, return number of used bytes
            return HashUpdateFromStream(h, stream, length);
        }

        /// <summary>
        /// Pump data from valid PHP stream into the hashing incremental algorithm.
        /// </summary>
        /// <param name="context">Hash resource to be updated from given <paramref name="stream"/>. Cannot be null.</param>
        /// <param name="stream">The <see cref="PhpStream"/> to read from. Cannot be null.</param>
        /// <param name="length">Maximum number of bytes to read from <paramref name="stream"/>. Or <c>-1</c> to read entire stream.</param>
        /// <returns>Number of bytes read from given <paramref name="stream"/>.</returns>
        private static int HashUpdateFromStream(HashPhpResource/*!*/context, PhpStream/*!*/stream, int length/*=-1*/)
        {
            Debug.Assert(context != null);
            Debug.Assert(stream != null);

            int n = 0;
            bool done = false;

            const int buffsize = 4096;

            do
            {
                // read data from stream sub-sequentially to lower memory consumption
                int bytestoread = (length < 0) ? buffsize : Math.Min(length - n, buffsize);   // read <buffsize> bytes, or up to <length> bytes
                if (bytestoread == 0)
                    break;

                PhpBytes bytes = stream.ReadBytes(bytestoread);
                if (bytes == null)
                    break;

                // update the incremental hash
                context.Update(bytes.ReadonlyData);

                n += bytes.Length;
                done = (bytes.Length < bytestoread);
            } while (!done);

            return n;
        }

        #endregion

        #region hash, hash_file

        [ImplementsFunction("hash")]
        [PureFunction]
        public static object DoHash(string algo, PhpBytes data)
        {
            return DoHash(algo, data, false);
        }
        [ImplementsFunction("hash")]
        [PureFunction]
        public static object DoHash(string algo, PhpBytes data, bool raw_output)
        {
            var h = HashInit(algo);
            if (h == null)
                return false;

            if (!HashUpdate(h, data))
                return false;

            return HashFinal(h, raw_output);
        }

        [ImplementsFunction("hash_file")]
        public static object DoHashFile(string algo, string filename)
        {
            return DoHashFile(algo, filename, false);
        }
        [ImplementsFunction("hash_file")]
        public static object DoHashFile(string algo, string filename, bool raw_output)
        {
            var h = HashInit(algo);
            if (h == null)
                return false;

            if (!HashUpdateFromFile(h, filename))
                return false;

            return HashFinal(h, raw_output);
        }

        #endregion

        #region hash_hmac, hash_hmac_file

        [ImplementsFunction("hash_hmac")]
        [PureFunction]
        public static object DoHashHMAC(string algo, PhpBytes data, PhpBytes key)
        {
            return DoHashHMAC(algo, data, key, false);
        }
        [ImplementsFunction("hash_hmac")]
        [PureFunction]
        public static object DoHashHMAC(string algo, PhpBytes data, PhpBytes key, bool raw_output)
        {
            var h = HashInit(algo, HashInitOptions.HASH_HMAC, key);
            if (h == null)
                return false;

            if (!HashUpdate(h, data))
                return false;

            return HashFinal(h, raw_output);
        }

        [ImplementsFunction("hash_hmac_file")]
        public static object DoHashHMACFile(string algo, string filename, PhpBytes key)
        {
            return DoHashHMACFile(algo, filename, key, false);
        }
        [ImplementsFunction("hash_hmac_file")]
        public static object DoHashHMACFile(string algo, string filename, PhpBytes key, bool raw_output)
        {
            var h = HashInit(algo, HashInitOptions.HASH_HMAC, key);
            if (h == null)
                return false;

            if (!HashUpdateFromFile(h, filename))
                return false;

            return HashFinal(h, raw_output);
        }

        #endregion
    }
}