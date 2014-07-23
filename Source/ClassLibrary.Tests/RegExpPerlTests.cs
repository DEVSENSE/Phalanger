using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PHP.Core;
using System.Text;
using System.Text.RegularExpressions;
using PHP.Library;
using System.Collections.Generic;

namespace PHP.Library.Tests
{
    [TestClass]
    public class RegExpPerlTests
    {
        [TestMethod]
        public void TestUnicodeMatch()
        {
            int m;

            m = PerlRegExp.Match
            (
              new PhpBytes(Encoding.UTF8.GetBytes("/[ř]/u")),
              new PhpBytes(Encoding.UTF8.GetBytes("12ščř45"))
            );
            Assert.AreEqual(m, 1);

            Encoding enc = Configuration.Application.Globalization.PageEncoding;

            m = PerlRegExp.Match
            (
              new PhpBytes(enc.GetBytes("/[ř]/")),
              new PhpBytes("12ščř45")
            );
            Assert.AreEqual(m, 1);

            // binary cache test:
            m = PerlRegExp.Match
            (
              new PhpBytes(enc.GetBytes("/[ř]/")),
              new PhpBytes("12ščř45")
            );
            Assert.AreEqual(m, 1);

            int count;
            object r = PerlRegExp.Replace
            (
            ScriptContext.CurrentContext,
                null,
                null,
              new PhpBytes(Encoding.UTF8.GetBytes("/[řš]+/u")),
              "|žýř|",
              new PhpBytes(Encoding.UTF8.GetBytes("Hešovářřřříčkořš hxx")),
              1000,
              out count
            );

            Assert.AreEqual(r as string, "He|žýř|ová|žýř|íčko|žýř| hxx");
            Assert.AreEqual(count, 3);
        }

        [TestMethod]
        public void TestConvertRegex()
        {
            IEnumerable<Tuple<string,string,PerlRegexOptions>> tests = new Tuple<string,string,PerlRegexOptions>[]
            {
                new Tuple<string,string,PerlRegexOptions>( @"?a+sa?s (?:{1,2})", "??a+?sa??s (?:{1,2}?)", PerlRegexOptions.Ungreedy),
                new Tuple<string,string,PerlRegexOptions>( @"(X+)(?:\|(.+?))?]](.*)$", @"(X+?)(?:\|(.+))??]](.*?)$", PerlRegexOptions.Ungreedy),
                new Tuple<string,string,PerlRegexOptions>( @"([X$]+)$", @"([X$]+)\z", PerlRegexOptions.DollarMatchesEndOfStringOnly)
            };

            foreach (var test in tests)
            {
                var result = PerlRegExpConverter.ConvertRegex(test.Item1, test.Item3, Encoding.UTF8);
                Assert.AreEqual(result, test.Item2);
            }
        }
    }
}
