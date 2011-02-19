using System;
using System.Security.Cryptography;

namespace Enyim.Hashes
{
    public class Hash_HSIEH : HashAlgorithm
    {
        /* 
            'm' and 'r' are mixing constants generated offline.  They're not
            really 'magic', they just happen to work well.
        */
        const uint m= 0x5bd1e995;
        const int r= 24;

        private uint hash;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        public Hash_HSIEH()
        {
            base.HashSizeValue = 32;

            this.Initialize();
        }

        /// <summary>
        /// Initializes an instance.
        /// </summary>
        public override void Initialize()
        {
            this.hash = 0;
        }

        protected override void HashCore(byte[] array, int ibStart, int cbSize)
        {
            int end = ibStart + cbSize;
            uint tmp;
            
            int i = ibStart;

            while (i + 4 < end) // at least 4 bytes available
            {
                hash += (uint)BitConverter.ToUInt16(array, i);
                tmp = (((uint)BitConverter.ToUInt16(array, i + 2)) << 11) ^ hash;
                hash = (hash << 16) ^ tmp;
                hash += hash >> 11; 

                //
                i += 4;
            }

            // Handle the last few bytes of the input array
            int remains = end - i;
            switch (remains)
            {
                case 3:
                    hash += (uint)BitConverter.ToUInt16(array, i);
                    hash ^= hash << 16;
                    hash ^= (uint)array[i + 2] << 18;
                    hash += hash >> 11;
                    break;
                case 2:
                    hash += (uint)BitConverter.ToUInt16(array, i);
                    hash ^= hash << 11;
                    hash += hash >> 17;
                    break;
                case 1:
                    hash += (uint)array[i];
                    hash ^= hash << 10;
                    hash += hash >> 1;
                    break;
            }
            
        }

        /// <summary>
        /// Returns the computed <see cref="T:OneAtATime" /> hash value after all data has been written to the object.
        /// </summary>
        /// <returns>The computed hash code.</returns>
        protected override byte[] HashFinal()
        {
            /* Force "avalanching" of final 127 bits */
            hash ^= hash << 3;
            hash += hash >> 5;
            hash ^= hash << 4;
            hash += hash >> 17;
            hash ^= hash << 25;
            hash += hash >> 6;

            return BitConverter.GetBytes(hash);
        }
    }
}