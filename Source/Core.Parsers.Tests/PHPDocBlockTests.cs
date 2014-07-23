using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using PHP.Core.Text;

namespace PHP.Core.Parsers.Tests
{
    [TestClass]
    public class PHPDocBlockTests
    {
        [TestMethod]
        public void PHPDocSummaryTest()
        {
            PHPDocBlock phpdoc;

            phpdoc = NewPHPDoc(@"/** Short Singleline Summary */");
            Assert.AreEqual(phpdoc.Summary, "Short Singleline Summary");
            Assert.AreEqual(phpdoc.ShortDescription, "Short Singleline Summary");

            phpdoc = NewPHPDoc(@"/**
 * Short Summary
 *
 * Long Summary
 * On two lines.
 */");
            Assert.AreEqual(phpdoc.Summary, "Short Summary\nLong Summary\nOn two lines.");
            Assert.AreEqual(phpdoc.ShortDescription, "Short Summary");
            Assert.AreEqual(phpdoc.GetElement<PHPDocBlock.ShortDescriptionElement>().Span, new Span(8, 19));

            phpdoc = NewPHPDoc(@"/**
 * Short Summary.
 * Long Summary
 * On two lines.
 */");
            Assert.AreEqual(phpdoc.Summary, "Short Summary.\nLong Summary\nOn two lines.");
            Assert.AreEqual(phpdoc.ShortDescription, "Short Summary.");

            phpdoc = NewPHPDoc(@"/**
 * Short Summary
 * Long Summary
 * On three
 * lines.
 */");
            Assert.AreEqual(phpdoc.Summary, "Short Summary\nLong Summary\nOn three\nlines.");
            Assert.AreEqual(phpdoc.ShortDescription, "Short Summary");
        }

        [TestMethod]
        public void PHPDocIgnoreTest()
        {
            var phpdoc = NewPHPDoc(@"/**
 * Short summary
 * @ignore
 */");
            Assert.IsTrue(phpdoc.IsIgnored);
        }

        [TestMethod]
        public void PHPDocParamsTest()
        {
            var phpdoc = NewPHPDoc(@"/**
 * Short summary
 * @param A $a A description.
 * @param B $b B description.
 * @param C $c C description.
 * @return X Return description.
 */");
            var returntag = phpdoc.Returns;
            Assert.IsNotNull(returntag);
            Assert.AreEqual(returntag.Description, "Return description.");
            Assert.AreEqual(returntag.TypeNames, "X");
            Assert.AreEqual(returntag.TypeNamesSpan, new Span(127, 1));
            Assert.AreEqual(returntag.VariableName, null);

            var parameters = phpdoc.Params.ToArray();
            var expected = new char[] { 'a', 'b', 'c' };
            Assert.AreEqual(expected.Length, parameters.Length);
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.AreEqual("$" + expected[i], parameters[i].VariableName);
                Assert.AreEqual(expected[i].ToUpperAsciiInvariant().ToString(), parameters[i].TypeNames);
                Assert.AreEqual(expected[i].ToUpperAsciiInvariant() + " description.", parameters[i].Description);
                Assert.AreEqual(parameters[i].VariableNameSpan, new Span(35 + i*31, 2));
                Assert.AreEqual(parameters[i].TypeNamesSpan, new Span(33 + i * 31, 1));
            }
        }

        static PHPDocBlock NewPHPDoc(string phpdoc)
        {
            var linebreaks = LineBreaks.Create(phpdoc);
            return new PHPDocBlock(phpdoc, new TextSpan(linebreaks, 0, phpdoc.Length));
        }
    }
}
