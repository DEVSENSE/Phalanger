using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text;
using System.IO;
using PHP.Core.Parsers;

namespace Core.Parsers.Tests
{
    [TestClass]
    public class TokenizerTests
    {
        [TestMethod]
        static void Test1()
        {
            Tokenizer tokenizer = new Tokenizer(TextReader.Null);

            //tokenizer.Initialize(new StringReader("EOT;\r\n"), LexicalStates.ST_HEREDOC, true);
            //tokenizer.hereDocLabel = "EOT";

            //tokenizer.Initialize(new StringReader("/*\r\n*/"), LexicalStates.ST_IN_SCRIPTING, true);
            //tokenizer.hereDocLabel = null;

            //tokenizer.Initialize(new StringReader("$x = 1; ###\r\n $y = 2;"), LexicalStates.ST_IN_SCRIPTING, true);
            //tokenizer.hereDocLabel = null;

            //tokenizer.Initialize(new StringReader("<? $x = array(); ?>"), LexicalStates.INITIAL, true);
            //tokenizer.hereDocLabel = null;

            //                    111111111
            //          0123456789012345678
            //string s = "echo 'aě'.'řa'.'x';";
            string s = "echo 'asdě' . 'řčřžý' . 'موقع للأخبا' . 'האתר' . 'as';";
            //string s = "echo 'abřc'.'e'/*xx\n\nyy\nxxx */;";

            byte[] buffer = new byte[1000];

            byte[] b = Encoding.UTF8.GetBytes(s);
            Stream stream = new MemoryStream(b);
            tokenizer.Initialize(new StreamReader(stream), PHP.Core.Parsers.Lexer.LexicalStates.ST_IN_SCRIPTING, true);
            
            //int b_start = 0;
            //int b_end = -1;
            //int b_length = 0;

            Tokens token;
            for (; ; )
            {
                token = tokenizer.GetNextToken();

                // check byte position matches:
                //b_length = tokenizer.GetTokenByteLength(Encoding.UTF8);
                //b_start = b_end + 1;
                //b_end += b_length;

                //// check binary positions:
                //long pos = stream.Position;
                //stream.Seek(b_start, SeekOrigin.Begin);
                //stream.Read(buffer, 0, b_length);
                //stream.Seek(pos, SeekOrigin.Begin);

                //Debug.Assert(String.CompareOrdinal(Encoding.UTF8.GetString(buffer, 0, b_length), tokenizer.TokenText) == 0);

                if (token == Tokens.EOF) break;

                //// check position:
                //Assert.AreEqual(s.Substring(tokenizer.token_start_pos.Char, tokenizer.TokenLength), tokenizer.TokenText);

                ////
                //Console.WriteLine("{0} '{1}' ({2}..{3}]", token, tokenizer.TokenText, tokenizer.token_start_pos.Char, tokenizer.token_end_pos.Char);
            }
        }
    }
}
