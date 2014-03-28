using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PHP.Core;

namespace Core.Parsers.Tests
{
    [TestClass]
    public class StringUtilsTests
    {
        /// <summary>
        /// Unit test.
        /// </summary>
        [TestMethod]
        public void TestIncrement()
        {
            string[] cases = new string[] { null, "", "z", "ZZ[Z9ZzZ", "ZZz" };
            string[] results = new string[] { "0", "1", "aa", "ZZ[A0AaA", "AAAa" };

            for (int i = 0; i < cases.Length; i++)
                Assert.AreEqual(StringUtils.Increment(cases[i]), results[i]);
        }
    }
}
