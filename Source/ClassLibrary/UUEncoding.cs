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

		#region Unit Test
#if DEBUG && !SILVERLIGHT

		[Test]
		static void TestUUEncodeDecode()
		{
			string[,] cases = 
		  { 
		    {"x", "!>```\n`\n"},
		    {"xx", "\">'@`\n`\n"},       
		    {"xxx", "#>'AX\n`\n"},
		    {"test\ntext text\r\n", "0=&5S=`IT97AT('1E>'0-\"@``\n`\n"},
		    {"The algorithm that shall be used for lines in","M5&AE(&%L9V]R:71H;2!T:&%T('-H86QL(&)E('5S960@9F]R(&QI;F5S(&EN\n`\n"},
		    {"The algorithm that shall be used for lines i","L5&AE(&%L9V]R:71H;2!T:&%T('-H86QL(&)E('5S960@9F]R(&QI;F5S(&D`\n`\n"},
		    {"The algorithm that shall be used for lines in ","M5&AE(&%L9V]R:71H;2!T:&%T('-H86QL(&)E('5S960@9F]R(&QI;F5S(&EN\n!(```\n`\n"},
		    {"",""},
		    {@"The algorithm that shall be used for lines in between begin and end takes three octets as input and writes four characters of output by splitting the input at six-bit intervals into four octets, containing data in the lower six bits only. These octets shall be converted to characters by adding a value of 0x20 to each octet, so that each octet is in the range [0x20,0x5f], and then it shall be assumed to represent a printable character in the ISO/IEC 646:1991 standard encoded character set. It then shall be translated into the corresponding character codes for the codeset in use in the current locale. (For example, the octet 0x41, representing 'A', would be translated to 'A' in the current codeset, such as 0xc1 if it were EBCDIC.)
Where the bits of two octets are combined, the least significant bits of the first octet shall be shifted left and combined with the most significant bits of the second octet shifted right. Thus the three octets A, B, C shall be converted into the four octets:
These octets then shall be translated into the local character set.
Each encoded line contains a length character, equal to the number of characters to be decoded plus 0x20 translated to the local character set as described above, followed by the encoded characters. The maximum number of octets to be encoded on each line shall be 45.",
@"M5&AE(&%L9V]R:71H;2!T:&%T('-H86QL(&)E('5S960@9F]R(&QI;F5S(&EN
M(&)E='=E96X@8F5G:6X@86YD(&5N9""!T86ME<R!T:')E92!O8W1E=',@87,@
M:6YP=70@86YD('=R:71E<R!F;W5R(&-H87)A8W1E<G,@;V8@;W5T<'5T(&)Y
M('-P;&ET=&EN9R!T:&4@:6YP=70@870@<VEX+6)I=""!I;G1E<G9A;',@:6YT
M;R!F;W5R(&]C=&5T<RP@8V]N=&%I;FEN9R!D871A(&EN('1H92!L;W=E<B!S
M:7@@8FET<R!O;FQY+B!4:&5S92!O8W1E=',@<VAA;&P@8F4@8V]N=F5R=&5D
M('1O(&-H87)A8W1E<G,@8GD@861D:6YG(&$@=F%L=64@;V8@,'@R,""!T;R!E
M86-H(&]C=&5T+""!S;R!T:&%T(&5A8V@@;V-T970@:7,@:6X@=&AE(')A;F=E
M(%LP>#(P+#!X-69=+""!A;F0@=&AE;B!I=""!S:&%L;""!B92!A<W-U;65D('1O
M(')E<')E<V5N=""!A('!R:6YT86)L92!C:&%R86-T97(@:6X@=&AE($E33R])
M14,@-C0V.C$Y.3$@<W1A;F1A<F0@96YC;V1E9""!C:&%R86-T97(@<V5T+B!)
M=""!T:&5N('-H86QL(&)E('1R86YS;&%T960@:6YT;R!T:&4@8V]R<F5S<&]N
M9&EN9R!C:&%R86-T97(@8V]D97,@9F]R('1H92!C;V1E<V5T(&EN('5S92!I
M;B!T:&4@8W5R<F5N=""!L;V-A;&4N(""A&;W(@97AA;7!L92P@=&AE(&]C=&5T
M(#!X-#$L(')E<')E<V5N=&EN9R`G02<L('=O=6QD(&)E('1R86YS;&%T960@
M=&\@)T$G(&EN('1H92!C=7)R96YT(&-O9&5S970L('-U8V@@87,@,'AC,2!I
M9B!I=""!W97)E($5""0T1)0RXI#0I7:&5R92!T:&4@8FET<R!O9B!T=V\@;V-T
M971S(&%R92!C;VUB:6YE9""P@=&AE(&QE87-T('-I9VYI9FEC86YT(&)I=',@
M;V8@=&AE(&9I<G-T(&]C=&5T('-H86QL(&)E('-H:69T960@;&5F=""!A;F0@
M8V]M8FEN960@=VET:""!T:&4@;6]S=""!S:6=N:69I8V%N=""!B:71S(&]F('1H
M92!S96-O;F0@;V-T970@<VAI9G1E9""!R:6=H=""X@5&AU<R!T:&4@=&AR964@
M;V-T971S($$L($(L($,@<VAA;&P@8F4@8V]N=F5R=&5D(&EN=&\@=&AE(&9O
M=7(@;V-T971S.@T*5&AE<V4@;V-T971S('1H96X@<VAA;&P@8F4@=')A;G-L
M871E9""!I;G1O('1H92!L;V-A;""!C:&%R86-T97(@<V5T+@T*16%C:""!E;F-O
M9&5D(&QI;F4@8V]N=&%I;G,@82!L96YG=&@@8VAA<F%C=&5R+""!E<75A;""!T
M;R!T:&4@;G5M8F5R(&]F(&-H87)A8W1E<G,@=&\@8F4@9&5C;V1E9""!P;'5S
M(#!X,C`@=')A;G-L871E9""!T;R!T:&4@;&]C86P@8VAA<F%C=&5R('-E=""!A
M<R!D97-C<FEB960@86)O=F4L(&9O;&QO=V5D(&)Y('1H92!E;F-O9&5D(&-H
M87)A8W1E<G,N(%1H92!M87AI;75M(&YU;6)E<B!O9B!O8W1E=',@=&\@8F4@
A96YC;V1E9""!O;B!E86-H(&QI;F4@<VAA;&P@8F4@-#4N
`
"}
		  };

			for (int i = 0; i < cases.GetLength(0); i++)
			{
				string encoded = Encode(new PhpBytes(Encoding.Default.GetBytes(cases[i, 0])));

				if (encoded != cases[i, 1].Replace("\r", ""))
				{
					Console.WriteLine();
					Console.WriteLine(encoded);
					Console.WriteLine(StringUtils.FirstDifferent(encoded, cases[i, 1], false));
					Debug.Fail();
				}

                byte[] bytes = Decode(encoded).ReadonlyData;
				string decoded = Encoding.Default.GetString(bytes, 0, bytes.Length);
				if (decoded != cases[i, 0])
					Debug.Fail();
			}
		}

#endif
		#endregion
	}
}
