using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PHP.Core;

namespace PHP.Library.Tests
{
    [TestClass]
    public class ArraysTests
    {
        [TestMethod]
        public void TestRandomKeys()
        {
            PhpArray a = PhpArray.Keyed("Server1", 1, "Server2", 2, "Server3", 3);
            PhpVariable.Dump(a);
            string result = PhpArrays.RandomKeys(a) as string;
            Assert.IsTrue(result == "Server1" || result == "Server2" || result == "Server3");
        }
    }
}
