using System;
using System.Security.Cryptography;

namespace Enyim.Hashes
{
    public class Hash_OneAtATime : HashAlgorithm
    {
        private uint currentHashValue;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        public Hash_OneAtATime()
        {
            base.HashSizeValue = 32;

            this.Initialize();
        }

        /// <summary>
        /// Initializes an instance.
        /// </summary>
        public override void Initialize()
        {
            this.currentHashValue = 0;
        }

        protected override void HashCore(byte[] array, int ibStart, int cbSize)
        {
            int end = ibStart + cbSize;

            for (int i = ibStart; i < end; i++)
            {
                uint val = (uint)array[i];
                currentHashValue += val;
                currentHashValue += (currentHashValue << 10);
                currentHashValue ^= (currentHashValue >> 6);
            }
        }

        /// <summary>
        /// Returns the computed <see cref="T:OneAtATime" /> hash value after all data has been written to the object.
        /// </summary>
        /// <returns>The computed hash code.</returns>
        protected override byte[] HashFinal()
        {
            uint value = currentHashValue;

            value += (value << 3);
            value ^= (value >> 11);
            value += (value << 15);

            return BitConverter.GetBytes(value);
        }
    }
}