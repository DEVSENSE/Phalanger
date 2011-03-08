namespace zlib
{
    using System;
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;
    using System.Text;

    internal class SupportClass
    {
        public static object Deserialize(BinaryReader binaryReader)
        {
            BinaryFormatter formatter = new BinaryFormatter();
            return formatter.Deserialize(binaryReader.BaseStream);
        }

        public static double Identity(double literal)
        {
            return literal;
        }

        public static long Identity(long literal)
        {
            return literal;
        }

        public static float Identity(float literal)
        {
            return literal;
        }

        public static ulong Identity(ulong literal)
        {
            return literal;
        }

        public static int ReadInput(Stream sourceStream, byte[] target, int start, int count)
        {
            if (target.Length == 0)
            {
                return 0;
            }
            byte[] buffer = new byte[target.Length];
            int num = sourceStream.Read(buffer, start, count);
            if (num == 0)
            {
                return -1;
            }
            for (int i = start; i < (start + num); i++)
            {
                target[i] = buffer[i];
            }
            return num;
        }

        public static int ReadInput(TextReader sourceTextReader, byte[] target, int start, int count)
        {
            if (target.Length == 0)
            {
                return 0;
            }
            char[] buffer = new char[target.Length];
            int num = sourceTextReader.Read(buffer, start, count);
            if (num == 0)
            {
                return -1;
            }
            for (int i = start; i < (start + num); i++)
            {
                target[i] = (byte) buffer[i];
            }
            return num;
        }

        public static void Serialize(BinaryWriter binaryWriter, object objectToSend)
        {
            new BinaryFormatter().Serialize(binaryWriter.BaseStream, objectToSend);
        }

        public static void Serialize(Stream stream, object objectToSend)
        {
            new BinaryFormatter().Serialize(stream, objectToSend);
        }

        public static byte[] ToByteArray(string sourceString)
        {
            return Encoding.UTF8.GetBytes(sourceString);
        }

        public static char[] ToCharArray(byte[] byteArray)
        {
            return Encoding.UTF8.GetChars(byteArray);
        }

        public static int URShift(int number, int bits)
        {
            if (number >= 0)
            {
                return (number >> bits);
            }
            return ((number >> bits) + (((int) 2) << ~bits));
        }

        public static int URShift(int number, long bits)
        {
            return URShift(number, (int) bits);
        }

        public static long URShift(long number, int bits)
        {
            if (number >= 0L)
            {
                return (number >> bits);
            }
            return ((number >> bits) + (((long) 2L) << ~bits));
        }

        public static long URShift(long number, long bits)
        {
            return URShift(number, (int) bits);
        }

        public static void WriteStackTrace(Exception throwable, TextWriter stream)
        {
            stream.Write(throwable.StackTrace);
            stream.Flush();
        }
    }
}

