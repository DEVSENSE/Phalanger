/*

 Copyright (c) 2005-2006 Tomas Matousek.  

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.IO;
using System.Text;
using PHP.Core;

#if SILVERLIGHT
using MathEx = PHP.CoreCLR.MathEx;
#else
using MathEx = System.Math;
using System.Diagnostics;
#endif

namespace PHP.Library
{
	/// <summary>
	/// Provides methods for strings UU-encoding and UU-decoding.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Uuencode repeatedly takes in a group of three bytes, adding trailing zeros if there are fewer 
	/// than three bytes left. These 24 bits are split into four groups of six which are treated as 
	/// numbers between 0 and 63. Decimal 32 is added to each number and they are output as ASCII 
	/// characters which will lie in the range 32 (space) to 32+63 = 95 (underscore). ASCII characters 
	/// greater than 95 may also be used; however, only the six right-most bits are relevant.
	/// </para>
	/// <para>
	/// Each group of sixty output characters (corresponding to 45 input bytes) is output as a separate 
	/// line preceded by an encoded character giving the number of encoded bytes on that line. 
	/// For all lines except the last, this will be the character 'M' (ASCII code 77 = 32+45). 
	/// If the input is not evenly divisible by 45, the last line will contain the remaining N 
	/// output characters, preceded by the character whose code is 32+N. Finally, a line containing just 
	/// a single space (or grave character) is output, followed by one line containing the string "end".
	/// </para>
	/// <para>
	/// Sometimes each data line has extra dummy characters (often the grave accent) added to avoid 
	/// problems with mailers that strip trailing spaces. These characters are ignored by uudecode. 
	/// The grave accent ('`') is used in place of a space character. 
	/// When stripped of their high bits they both decode to 100000.
	/// </para>
	/// </remarks>
	public static class UUEncoder
	{
		private const char UUEncodeZero = '`';

		/// <summary>
		/// Encodes an array of bytes using UU-encode algorithm.
		/// </summary>
		/// <param name="input">Array of bytes to be encoded.</param>
		/// <param name="output">Encoded output writer.</param>
		public static void Encode(byte[]/*!*/ input, TextWriter/*!*/ output)
		{
			if (input == null) throw new ArgumentNullException("input");
			if (output == null) throw new ArgumentNullException("output");

			if (input.Length == 0) return;

			const int max_bytes_per_line = 45;

			int remains;
			int full_lines = MathEx.DivRem(input.Length, max_bytes_per_line, out remains);
			int input_offset = 0;

			// encode full lines:
			for (int i = 0; i < full_lines; i++)
			{
				output.Write(EncodeByte(max_bytes_per_line));

				for (int j = 0; j < max_bytes_per_line / 3; j++)
				{
					EncodeWriteTriplet(output, input[input_offset], input[input_offset + 1], input[input_offset + 2]);
					input_offset += 3;
				}

				output.Write('\n');
			}

			// encode remaining bytes (if any):
			if (remains > 0)
			{
				output.Write(EncodeByte(remains));

				// ceil(remains/3)*4
				int full_triplets = MathEx.DivRem(remains, 3, out remains);

				// full triplets:
				for (int i = 0; i < full_triplets; i++)
				{
					EncodeWriteTriplet(output, input[input_offset], input[input_offset + 1], input[input_offset + 2]);
					input_offset += 3;
				}

				// remaining bytes:
				if (remains == 1)
				{
					EncodeWriteTriplet(output, input[input_offset], 0, 0);
				}
				else if (remains == 2)
				{
					EncodeWriteTriplet(output, input[input_offset], input[input_offset + 1], 0);
				}

				output.Write('\n');
			}

			output.Write('`');
			output.Write('\n');
		}

		private static char EncodeByte(int b)
		{
			Debug.Assert(b <= 0x3f);
			return (b == 0) ? '`' : (char)(0x20 + b);
		}

		private static byte DecodeChar(int c)
		{
			return (byte)((c - 0x20) & 0x3f);
		}

		private static void EncodeWriteTriplet(TextWriter/*!*/ output, int a, int b, int c)
		{
			output.Write(EncodeByte(a >> 2));
			output.Write(EncodeByte(((a << 4) | (b >> 4)) & 0x3f));
			output.Write(EncodeByte(((b << 2) | (c >> 6)) & 0x3f));
			output.Write(EncodeByte(c & 0x3f));
		}

		/// <summary>
		/// Decodes textual data using UU-encode algorithm.
		/// </summary>
		/// <param name="input">Textual data reader.</param>
		/// <param name="output">Binary output writer.</param>
		/// <remarks>Whether input data has correct format.</remarks>
		public static bool Decode(TextReader/*!*/ input, MemoryStream/*!*/ output)
		{
			if (input == null) throw new ArgumentNullException("input");
			if (output == null) throw new ArgumentNullException("output");

			// empty input:
			if (input.Peek() == -1) return true;

			for (; ; )
			{
				int line_length = input.Read();
				if (line_length == -1) return false;

				line_length = DecodeChar((char)line_length);

				// stopped by '`' on the last line:
				if (line_length == 0)
					return input.Read() == (int)'\n';

				int remains;
				int full_triplets = MathEx.DivRem(line_length, 3, out remains);

				for (int i = 0; i < full_triplets; i++)
				{
					int a = DecodeChar(input.Read());
					int b = DecodeChar(input.Read());
					int c = DecodeChar(input.Read());
					int d = input.Read();
					if (d == -1) return false;
					d = DecodeChar(d);

					output.WriteByte((byte)((a << 2 | b >> 4) & 0xff));
					output.WriteByte((byte)((b << 4 | c >> 2) & 0xff));
					output.WriteByte((byte)((c << 6 | d) & 0xff));
				}

				if (remains > 0)
				{
					int a = DecodeChar(input.Read());
					int b = DecodeChar(input.Read());
					int c = DecodeChar(input.Read());
					int d = input.Read();
					if (d == -1) return false;
					d = DecodeChar(d);

					output.WriteByte((byte)(a << 2 | b >> 4));

					if (remains == 2)
						output.WriteByte((byte)(b << 4 | c >> 2));
				}

				if (input.Read() != (int)'\n')
					return false;
			}
		}

		/// <summary>
		/// Encodes a string using UU-encode algorithm.
		/// </summary>
		/// <param name="bytes">String of bytes to be encoded.</param>
		/// <returns>The encoded string.</returns>
		[ImplementsFunction("convert_uuencode")]
		public static string Encode(PhpBytes bytes)
		{
            byte[] data = (bytes != null) ? bytes.ReadonlyData : ArrayUtils.EmptyBytes;
			StringBuilder result = new StringBuilder((int)(data.Length * 1.38 + data.Length + 1));

			Encode(data, new StringWriter(result));

			return result.ToString();
		}

		/// <summary>
		/// Decodes a uu-encoded string.
		/// </summary>
		/// <param name="data">Data to be decoded.</param>
		/// <returns>Decoded bytes.</returns>
		[ImplementsFunction("convert_uudecode")]
		public static PhpBytes Decode(string data)
		{
			if (data == null) data = "";

			MemoryStream result = new MemoryStream((int)(data.Length * 0.75) + 2);

			if (!Decode(new StringReader(data), result))
				PhpException.Throw(PhpError.Warning, LibResources.GetString("invalid_uuencoded_string"));

			return new PhpBytes(result.ToArray());
		}
	}
}
