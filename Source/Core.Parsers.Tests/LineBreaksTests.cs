using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PHP.Core.Text;

namespace Core.Parsers.Tests
{
    [TestClass]
    public class LineBreaksTests
    {
        [TestMethod]
        public void LineBreaksTest()
        {
            string sample = "Hello World";
            foreach (var nl in new[] { "\r", "\r\n", "\n", "\u0085", "\u2028", "\u2029" })
            {
                for (int breakscount = 0; breakscount < 512; breakscount += 17)
                {
                    // construct sample text with {linecount} lines
                    string text = string.Empty;
                    for (var line = 0; line < breakscount; line++)
                        text += sample + nl;
                    text += sample;

                    // test LineBreaks
                    var linebreaks = LineBreaks.Create(text);
                    Assert.AreEqual(linebreaks.LinesCount, breakscount + 1);
                    for (int i = 0; i <= text.Length; i += 7)
                        Assert.AreEqual(linebreaks.GetLineFromPosition(i), (i / (sample.Length + nl.Length)));
                }
            }
        }
    }
}
