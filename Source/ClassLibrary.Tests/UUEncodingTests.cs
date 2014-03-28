using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PHP.Core;
using System.Text;

namespace PHP.Library.Tests
{
    [TestClass]
    public class UUEncodingTests
    {
        [TestMethod]
        public void TestUUEncodeDecode()
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
                string encoded = UUEncoder.Encode(new PhpBytes(Encoding.Default.GetBytes(cases[i, 0])));

                if (encoded != cases[i, 1].Replace("\r", ""))
                {
                    Console.WriteLine();
                    Console.WriteLine(encoded);
                    Console.WriteLine(StringUtils.FirstDifferent(encoded, cases[i, 1], false));
                    Assert.Fail(null);
                }

                byte[] bytes = UUEncoder.Decode(encoded).ReadonlyData;
                string decoded = Encoding.Default.GetString(bytes, 0, bytes.Length);
                Assert.AreEqual(decoded, cases[i, 0]);
            }
        }
    }
}
