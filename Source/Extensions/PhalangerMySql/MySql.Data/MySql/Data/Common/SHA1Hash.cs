namespace MySql.Data.Common
{
    using System;

    internal class SHA1Hash
    {
        private bool computed;
        private uint[] intermediateHash = new uint[5];
        private static uint[] K = new uint[] { 0x5a827999, 0x6ed9eba1, 0x8f1bbcdc, 0xca62c1d6 };
        private ulong length;
        private byte[] messageBlock = new byte[0x40];
        private short messageBlockIndex;
        private static uint[] sha_const_key = new uint[] { 0x67452301, 0xefcdab89, 0x98badcfe, 0x10325476, 0xc3d2e1f0 };
        private const int SHA1_HASH_SIZE = 20;

        public SHA1Hash()
        {
            this.Reset();
        }

        private static uint CircularShift(int bits, uint word)
        {
            return ((word << bits) | (word >> (0x20 - bits)));
        }

        public byte[] ComputeHash(byte[] buffer)
        {
            this.Reset();
            this.Input(buffer, 0, buffer.Length);
            return this.Result();
        }

        public void Input(byte[] buffer, int index, int bufLen)
        {
            if ((buffer != null) && (bufLen != 0))
            {
                if ((index < 0) || (index > (buffer.Length - 1)))
                {
                    throw new ArgumentException("Index must be a value between 0 and buffer.Length-1", "index");
                }
                if (bufLen < 0)
                {
                    throw new ArgumentException("Length must be a value > 0", "length");
                }
                if ((bufLen + index) > buffer.Length)
                {
                    throw new ArgumentException("Length + index would extend past the end of buffer", "length");
                }
                while (bufLen-- > 0)
                {
                    short num;
                    this.messageBlockIndex = (short) ((num = this.messageBlockIndex) + 1);
                    this.messageBlock[num] = (byte) (buffer[index++] & 0xff);
                    this.length += (ulong) 8L;
                    if (this.messageBlockIndex == 0x40)
                    {
                        this.ProcessMessageBlock();
                    }
                }
            }
        }

        private void PadMessage()
        {
            int messageBlockIndex = this.messageBlockIndex;
            if (messageBlockIndex > 0x37)
            {
                this.messageBlock[messageBlockIndex++] = 0x80;
                Array.Clear(this.messageBlock, messageBlockIndex, 0x40 - messageBlockIndex);
                this.messageBlockIndex = 0x40;
                this.ProcessMessageBlock();
                Array.Clear(this.messageBlock, 0, 0x38);
                this.messageBlockIndex = 0x38;
            }
            else
            {
                this.messageBlock[messageBlockIndex++] = 0x80;
                Array.Clear(this.messageBlock, messageBlockIndex, 0x38 - messageBlockIndex);
                this.messageBlockIndex = 0x38;
            }
            this.messageBlock[0x38] = (byte) (this.length >> 0x38);
            this.messageBlock[0x39] = (byte) (this.length >> 0x30);
            this.messageBlock[0x3a] = (byte) (this.length >> 40);
            this.messageBlock[0x3b] = (byte) (this.length >> 0x20);
            this.messageBlock[60] = (byte) (this.length >> 0x18);
            this.messageBlock[0x3d] = (byte) (this.length >> 0x10);
            this.messageBlock[0x3e] = (byte) (this.length >> 8);
            this.messageBlock[0x3f] = (byte) this.length;
            this.ProcessMessageBlock();
        }

        private void ProcessMessageBlock()
        {
            uint num;
            uint[] numArray = new uint[80];
            for (int i = 0; i < 0x10; i++)
            {
                int index = i * 4;
                numArray[i] = (uint) (this.messageBlock[index] << 0x18);
                numArray[i] |= (uint) (this.messageBlock[index + 1] << 0x10);
                numArray[i] |= (uint) (this.messageBlock[index + 2] << 8);
                numArray[i] |= this.messageBlock[index + 3];
            }
            for (int j = 0x10; j < 80; j++)
            {
                numArray[j] = CircularShift(1, ((numArray[j - 3] ^ numArray[j - 8]) ^ numArray[j - 14]) ^ numArray[j - 0x10]);
            }
            uint word = this.intermediateHash[0];
            uint num3 = this.intermediateHash[1];
            uint num4 = this.intermediateHash[2];
            uint num5 = this.intermediateHash[3];
            uint num6 = this.intermediateHash[4];
            for (int k = 0; k < 20; k++)
            {
                num = (((CircularShift(5, word) + ((num3 & num4) | (~num3 & num5))) + num6) + numArray[k]) + K[0];
                num6 = num5;
                num5 = num4;
                num4 = CircularShift(30, num3);
                num3 = word;
                word = num;
            }
            for (int m = 20; m < 40; m++)
            {
                num = (((CircularShift(5, word) + ((num3 ^ num4) ^ num5)) + num6) + numArray[m]) + K[1];
                num6 = num5;
                num5 = num4;
                num4 = CircularShift(30, num3);
                num3 = word;
                word = num;
            }
            for (int n = 40; n < 60; n++)
            {
                num = (((CircularShift(5, word) + (((num3 & num4) | (num3 & num5)) | (num4 & num5))) + num6) + numArray[n]) + K[2];
                num6 = num5;
                num5 = num4;
                num4 = CircularShift(30, num3);
                num3 = word;
                word = num;
            }
            for (int num13 = 60; num13 < 80; num13++)
            {
                num = (((CircularShift(5, word) + ((num3 ^ num4) ^ num5)) + num6) + numArray[num13]) + K[3];
                num6 = num5;
                num5 = num4;
                num4 = CircularShift(30, num3);
                num3 = word;
                word = num;
            }
            this.intermediateHash[0] += word;
            this.intermediateHash[1] += num3;
            this.intermediateHash[2] += num4;
            this.intermediateHash[3] += num5;
            this.intermediateHash[4] += num6;
            this.messageBlockIndex = 0;
        }

        public void Reset()
        {
            this.length = 0L;
            this.messageBlockIndex = 0;
            this.intermediateHash[0] = sha_const_key[0];
            this.intermediateHash[1] = sha_const_key[1];
            this.intermediateHash[2] = sha_const_key[2];
            this.intermediateHash[3] = sha_const_key[3];
            this.intermediateHash[4] = sha_const_key[4];
            this.computed = false;
        }

        public byte[] Result()
        {
            if (!this.computed)
            {
                this.PadMessage();
                Array.Clear(this.messageBlock, 0, 0x40);
                this.length = 0L;
                this.computed = true;
            }
            byte[] buffer = new byte[20];
            for (int i = 0; i < 20; i++)
            {
                buffer[i] = (byte) (this.intermediateHash[i >> 2] >> (8 * (3 - (i & 3))));
            }
            return buffer;
        }
    }
}

