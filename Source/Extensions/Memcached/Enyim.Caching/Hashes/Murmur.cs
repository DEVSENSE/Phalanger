using System;
using System.Security.Cryptography;

namespace Enyim.Hashes
{
    public class Hash_Murmur : HashAlgorithm
    {
        /* 
            'm' and 'r' are mixing constants generated offline.  They're not
            really 'magic', they just happen to work well.
        */
        const uint m= 0x5bd1e995;
        const int r= 24;

        private uint currentHashValue;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        public Hash_Murmur()
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

            uint seed = (0xdeadbeef * (uint)cbSize);
            uint h = seed ^ (uint)cbSize;

            int i = ibStart;

            while (i + 4 < end) // at least 4 bytes available
            {
                uint k = BitConverter.ToUInt32(array, i);

                k *= m;
                k ^= k >> r;
                k *= m;

                h *= m;
                h ^= k;

                //
                i += 4;
            }

            // Handle the last few bytes of the input array
            int remains = end - i;
            if (remains >= 1)
            {
                h ^= (uint)array[i + 0] <<  0;

                if (remains >= 2)
                {
                    h ^= (uint)array[i + 1] << 8;

                    if (remains >= 3)
                    {
                        h ^= (uint)array[i + 2] << 16;
                    }
                }

                h *= m;
            }

            currentHashValue = h;
        }

        /// <summary>
        /// Returns the computed <see cref="T:OneAtATime" /> hash value after all data has been written to the object.
        /// </summary>
        /// <returns>The computed hash code.</returns>
        protected override byte[] HashFinal()
        {
            uint h = currentHashValue;

            h ^= h >> 13;
            h *= m;
            h ^= h >> 15;

            return BitConverter.GetBytes(h);
        }
    }
}