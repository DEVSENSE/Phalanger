namespace MySql.Data.MySqlClient
{
    using MySql.Data.Common;
    using MySql.Data.MySqlClient.Properties;
    using System;
    using System.Globalization;
    using System.Text;

    internal class Crypt
    {
        private Crypt()
        {
        }

        public static string EncryptPassword(string password, string seed, bool new_ver)
        {
            long max = 0x3fffffffL;
            if (!new_ver)
            {
                max = 0x1ffffffL;
            }
            if ((password == null) || (password.Length == 0))
            {
                return password;
            }
            long[] numArray = Hash(seed);
            long[] numArray2 = Hash(password);
            long num2 = (numArray[0] ^ numArray2[0]) % max;
            long num3 = (numArray[1] ^ numArray2[1]) % max;
            if (!new_ver)
            {
                num3 = num2 / 2L;
            }
            char[] chArray = new char[seed.Length];
            for (int i = 0; i < seed.Length; i++)
            {
                double num5 = rand(ref num2, ref num3, max);
                chArray[i] = (char) ((ushort) (Math.Floor((double) (num5 * 31.0)) + 64.0));
            }
            if (new_ver)
            {
                char ch = (char) ((ushort) Math.Floor((double) (rand(ref num2, ref num3, max) * 31.0)));
                for (int j = 0; j < chArray.Length; j++)
                {
                    chArray[j] = (char) (chArray[j] ^ ch);
                }
            }
            return new string(chArray);
        }

        public static byte[] Get410Password(string password, byte[] seedBytes)
        {
            SHA1Hash hash = new SHA1Hash();
            password = password.Replace(" ", "").Replace("\t", "");
            byte[] bytes = Encoding.Default.GetBytes(password);
            byte[] sourceArray = hash.ComputeHash(bytes);
            byte[] destinationArray = new byte[0x18];
            Array.Copy(seedBytes, 0, destinationArray, 0, 4);
            Array.Copy(sourceArray, 0, destinationArray, 4, 20);
            byte[] buffer4 = hash.ComputeHash(destinationArray);
            byte[] to = new byte[20];
            XorScramble(seedBytes, 4, to, 0, buffer4, 20);
            byte[] buffer6 = new byte[20];
            XorScramble(to, 0, buffer6, 0, sourceArray, 20);
            return buffer6;
        }

        public static byte[] Get411Password(string password, string seed)
        {
            if (password.Length == 0)
            {
                return new byte[1];
            }
            SHA1Hash hash = new SHA1Hash();
            byte[] buffer = hash.ComputeHash(Encoding.Default.GetBytes(password));
            byte[] sourceArray = hash.ComputeHash(buffer);
            byte[] bytes = Encoding.Default.GetBytes(seed);
            byte[] destinationArray = new byte[bytes.Length + sourceArray.Length];
            Array.Copy(bytes, 0, destinationArray, 0, bytes.Length);
            Array.Copy(sourceArray, 0, destinationArray, bytes.Length, sourceArray.Length);
            byte[] buffer5 = hash.ComputeHash(destinationArray);
            byte[] buffer6 = new byte[buffer5.Length + 1];
            buffer6[0] = 20;
            Array.Copy(buffer5, 0, buffer6, 1, buffer5.Length);
            for (int i = 1; i < buffer6.Length; i++)
            {
                buffer6[i] = (byte) (buffer6[i] ^ buffer[i - 1]);
            }
            return buffer6;
        }

        public static byte[] GetOld410Password(string password, byte[] seedBytes)
        {
            long[] numArray = Hash(password);
            int[] numArray2 = getSaltFromPassword(string.Format(CultureInfo.InvariantCulture, "{0,8:X}{1,8:X}", new object[] { numArray[0], numArray[1] }));
            byte[] src = new byte[20];
            int num = 0;
            for (int i = 0; i < 2; i++)
            {
                int num3 = numArray2[i];
                for (int k = 3; k >= 0; k--)
                {
                    src[k + num] = (byte) (num3 % 0x100);
                    num3 = num3 >> 8;
                }
                num += 4;
            }
            SHA1Hash hash = new SHA1Hash();
            byte[] dst = new byte[8];
            Buffer.BlockCopy(src, 0, dst, 0, 8);
            byte[] buffer3 = hash.ComputeHash(dst);
            byte[] to = new byte[20];
            XorScramble(seedBytes, 4, to, 0, buffer3, 20);
            string p = Encoding.Default.GetString(to, 0, to.Length).Substring(0, 8);
            long[] numArray3 = Hash(password);
            long[] numArray4 = Hash(p);
            long max = 0x3fffffffL;
            byte[] buffer5 = new byte[20];
            int num6 = 0;
            int length = p.Length;
            int num8 = 0;
            long num9 = (numArray3[0] ^ numArray4[0]) % max;
            long num10 = (numArray3[1] ^ numArray4[1]) % max;
            while (num6++ < length)
            {
                buffer5[num8++] = (byte) (Math.Floor((double) (rand(ref num9, ref num10, max) * 31.0)) + 64.0);
            }
            byte num11 = (byte) Math.Floor((double) (rand(ref num9, ref num10, max) * 31.0));
            for (int j = 0; j < 8; j++)
            {
                buffer5[j] = (byte) (buffer5[j] ^ num11);
            }
            return buffer5;
        }

        private static int[] getSaltFromPassword(string password)
        {
            int[] numArray = new int[6];
            if ((password != null) && (password.Length != 0))
            {
                int num = 0;
                int num2 = 0;
                while (num2 < password.Length)
                {
                    int num3 = 0;
                    for (int i = 0; i < 8; i++)
                    {
                        num3 = (num3 << 4) + HexValue(password[num2++]);
                    }
                    numArray[num++] = num3;
                }
            }
            return numArray;
        }

        private static long[] Hash(string P)
        {
            long num = 0x50305735L;
            long num2 = 0x12345671L;
            long num3 = 7L;
            for (int i = 0; i < P.Length; i++)
            {
                if ((P[i] != ' ') && (P[i] != '\t'))
                {
                    long num5 = '\x00ff' & P[i];
                    num ^= (((num & 0x3fL) + num3) * num5) + (num << 8);
                    num2 += (num2 << 8) ^ num;
                    num3 += num5;
                }
            }
            return new long[] { (num & 0x7fffffffL), (num2 & 0x7fffffffL) };
        }

        private static int HexValue(char c)
        {
            if ((c >= 'A') && (c <= 'Z'))
            {
                return ((c - 'A') + 10);
            }
            if ((c >= 'a') && (c <= 'z'))
            {
                return ((c - 'a') + 10);
            }
            return (c - '0');
        }

        private static double rand(ref long seed1, ref long seed2, long max)
        {
            seed1 = (seed1 * 3L) + seed2;
            seed1 = seed1 % max;
            seed2 = ((seed1 + seed2) + 0x21L) % max;
            return (((double) seed1) / ((double) max));
        }

        private static void XorScramble(byte[] from, int fromIndex, byte[] to, int toIndex, byte[] password, int length)
        {
            if ((fromIndex < 0) || (fromIndex >= from.Length))
            {
                throw new ArgumentException(Resources.IndexMustBeValid, "fromIndex");
            }
            if ((fromIndex + length) > from.Length)
            {
                throw new ArgumentException(Resources.FromAndLengthTooBig, "fromIndex");
            }
            if (from == null)
            {
                throw new ArgumentException(Resources.BufferCannotBeNull, "from");
            }
            if (to == null)
            {
                throw new ArgumentException(Resources.BufferCannotBeNull, "to");
            }
            if ((toIndex < 0) || (toIndex >= to.Length))
            {
                throw new ArgumentException(Resources.IndexMustBeValid, "toIndex");
            }
            if ((toIndex + length) > to.Length)
            {
                throw new ArgumentException(Resources.IndexAndLengthTooBig, "toIndex");
            }
            if ((password == null) || (password.Length < length))
            {
                throw new ArgumentException(Resources.PasswordMustHaveLegalChars, "password");
            }
            if (length < 0)
            {
                throw new ArgumentException(Resources.ParameterCannotBeNegative, "count");
            }
            for (int i = 0; i < length; i++)
            {
                to[toIndex++] = (byte) (from[fromIndex++] ^ password[i]);
            }
        }
    }
}

