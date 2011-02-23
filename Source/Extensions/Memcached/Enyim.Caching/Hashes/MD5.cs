using System;
using System.Security.Cryptography;

namespace Enyim.Hashes
{
    internal class Hash_MD5 : HashAlgorithm
    {
        private byte[] hashed = null;

        private static MD5 md5instance = MD5.Create();

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        public Hash_MD5()
        {
            this.Initialize();
        }

        /// <summary>
        /// Initializes an instance.
        /// </summary>
        public override void Initialize()
        {
            hashed = null;
        }

        protected override void HashCore(byte[] array, int ibStart, int cbSize)
        {
            hashed = md5instance.ComputeHash(array, ibStart, cbSize);
            base.HashSizeValue = hashed.Length * 8;
        }

        /// <summary>
        /// Returns the computed <see cref="T:OneAtATime" /> hash value after all data has been written to the object.
        /// </summary>
        /// <returns>The computed hash code.</returns>
        protected override byte[] HashFinal()
        {
            return hashed;
        }
    }
}