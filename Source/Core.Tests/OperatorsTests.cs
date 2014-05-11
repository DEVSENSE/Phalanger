using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PHP.Core;

namespace PHP.Core.Tests
{
    [TestClass]
    public class OperatorsTests
    {
        [TestMethod]
        static void TestAddition()
        {
            object[,] cases = 
			{
			{ 1, 2, 3 },
			{ Int32.MaxValue-10, "10dfghgfh", Int32.MaxValue },
			{ "-100", "+100", 0 },
			{ "100", "100.0000000001", 200.0000000001 },
			};

            for (int i = 0; i < cases.GetLength(1); i++)
            {
                object result = Operators.Add(cases[i, 0], cases[i, 1]);
                Assert.Equals(result, cases[i, 2]);
            }

            PhpArray a = Operators.Add(
                PhpArray.New(new object[] { "a", 5, 7 }),
                PhpArray.New(new object[] { "8q", 1 })
            ) as PhpArray;

            Assert.IsTrue(a != null && a.Count == 3 && (string)a[0] == "a" && (int)a[1] == 5 && (int)a[2] == 7);
        }

        [TestMethod]
        static void TestShiftLeft()
        {
            object[,] cases = 
			{
			{ "1.5xxx", -35, 536870912 },
			{ "1.5xxx",   0, 1 },
			{ "1.5xxx",  34, 17179869184L } // 64bit behaviour
			};

            for (int i = 0; i < cases.GetLength(1); i++)
            {
                object result = Operators.ShiftLeft(cases[i, 0], cases[i, 1]);
                Assert.AreEqual(result, cases[i, 2]);
            }
        }

        [TestMethod]
        static void Concat()
        {
            PhpBytes a = new PhpBytes(new byte[] { 61, 62, 63 });
            string b = "-hello-";
            PhpBytes c = new PhpBytes(new byte[] { 61, 61, 61 });
            string d = "-bye-";

            object result = Operators.Concat(a, b, c, d);
            Assert.IsTrue(Operators.StrictEquality(result, "=>?-hello-===-bye-"));
        }
    }
}
