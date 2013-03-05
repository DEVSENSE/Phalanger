using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Resources;
using System.Reflection;

using PHP.Core;

namespace PHP.Library.Gd2
{
    internal static class Utils
    {
        /// <summary>
        /// Assembly's resources.
        /// </summary>
        internal static readonly ResourceManager Resources = new ResourceManager("PHP.Library.Gd2.Properties.Resources", Assembly.GetExecutingAssembly());

        /// <summary>
        /// Open stream using working directory and PHP include directories.
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        internal static System.IO.Stream OpenStream(string filename)
        {
            PhpStream stream = PhpStream.Open(filename, "rb", StreamOpenOptions.Empty, StreamContext.Default);
            if (stream == null)
                return null;

            return stream.RawStream;
        }

        /// <summary>
        /// Reads PhpBytes from file using the PhpStream.
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        internal static PhpBytes ReadPhpBytes(string filename)
        {
            PhpBytes bytes;

            using (PhpStream stream = PhpStream.Open(filename, "rb", StreamOpenOptions.Empty, StreamContext.Default))
            {
                if (stream == null)
                    return null;

                try
                {
                    bytes = PhpStream.AsBinary(stream.ReadContents());
                    if (bytes == null || bytes.IsEmpty())
                        return null;
                }
                catch
                {
                    return null;
                }
            }

            return bytes;
        }

        /// <summary>
        /// Tests if specified portions of two byte arrays are equal
        /// </summary>
        /// <param name="array1">First array. Cannot be <c>null</c> reference.</param>
        /// <param name="array2">Second array. Cannot be <c>null</c> reference.</param>
        /// <param name="length">Amount of bytes to compare.</param>
        /// <returns>returns true if both arrays are equal</returns>
        internal static bool ByteArrayCompare(byte[]/*!*/array1, byte[]/*!*/array2, int length)
        {
            //int max = (array1.Length > array2.Length) ? array1.Length : array2.Length;

            Debug.Assert(array1 != null);
            Debug.Assert(array2 != null);

            if (array1.Length < length || array2.Length < length)
            {
                return false;
            }

            try
            {
                for (int i = 0; i < length; i++)
                {
                    if (array1[i] != array2[i])
                        return false;
                }
            }
            catch
            {
                return false;
            }

            return true;
        }
    }
}
