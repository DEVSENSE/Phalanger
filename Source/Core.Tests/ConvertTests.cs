using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PHP.Core.Tests
{
    [TestClass]
    public class ConvertTests
    {
        struct TestCase
        {
            public string s;
            public bool isnum;
            public int p, i, l, d;
            public int iv;
            public long lv;
            public double dv;

            public TestCase(string s, bool isnum, int p, int i, int l, int d, int iv, long lv, double dv)
            {
                this.s = s;
                this.isnum = isnum;
                this.p = p;
                this.i = i;
                this.l = l;
                this.d = d;
                this.iv = iv;
                this.lv = lv;
                this.dv = dv;
            }
        }

        static int MaxInt = Int32.MaxValue;
        static int MinInt = Int32.MinValue;
        static long MinLong = Int64.MinValue;
        static long MaxLong = Int64.MaxValue;
        static string LongOvf = "1250456465465412504564654654";
        static string IntOvf = "12504564654654";
        static long IntOvfL = long.Parse(IntOvf);
        static double IntOvfD = double.Parse(IntOvf);
        static string LongHOvf = "0x09213921739830924323423";

        static TestCase[] cases = new TestCase[]
			{
				//           string                 number?    p   i   l   d       iv       lv  dv
                new TestCase("0",                     true,    1,  1,  1,  1,       0,       0,  0.0),
                new TestCase("0x",                    true,    2,  2,  2,  1,       0,       0,  0.0),
                new TestCase("0X",                    true,    2,  2,  2,  1,       0,       0,  0.0),
                new TestCase("00x1",                 false,    2,  2,  2,  2,       0,       0,  0.0),
                new TestCase("0x10",                  true,    4,  4,  4,  1,      16,      16,  16.0),  // dv changed in v2
                new TestCase("-0xf",                  true,    4,  4,  4,  2,     -15,     -15,  -15.0), // dv changed in v2
                new TestCase("00000000013",           true,   11, 11, 11, 11,      13,      13,  13.0),
                new TestCase("00000000",              true,    8,  8,  8,  8,       0,       0,  0.0),
                new TestCase("1",                     true,    1,  1,  1,  1,       1,       1,  1.0),
                new TestCase("0",                     true,    1,  1,  1,  1,       0,       0,  0.0),
                new TestCase("00008",                 true,    5,  5,  5,  5,       8,       8,  8.0),
                new TestCase(IntOvf,                  true,   14, 10, 14, 14,  MaxInt, IntOvfL,  IntOvfD),
                new TestCase(LongOvf,                 true,   LongOvf.Length,  10, 19, LongOvf.Length, MaxInt, MaxLong,  Double.NaN),
                new TestCase(LongHOvf,                true,   LongHOvf.Length, 17, 24, 1, MaxInt, MaxLong, Double.NaN),
                new TestCase(MaxInt.ToString(),       true,   10, 10, 10, 10,  MaxInt,  MaxInt,  MaxInt),
                new TestCase(MinInt.ToString(),       true,   11, 11, 11, 11,  MinInt,  MinInt,  MinInt),
                new TestCase(MinLong.ToString(),      true,   20, 11, 20, 20,  MinInt,  MinLong,  MinLong),
                new TestCase(MaxLong.ToString(),      true,   19, 10, 19, 19,  MaxInt,  MaxLong,  MaxLong),
				new TestCase("0.587e5",               true,    7,  1,  1,  7,       0,       0,  58700.0),
				new TestCase("10dfd",                false,    2,  2,  2,  2,      10,      10,  10.0),
				new TestCase("10efd",                false,    2,  2,  2,  2,      10,      10,  10.0),
				new TestCase("10d",                  false,    2,  2,  2,  2,      10,      10,  10.0),
				new TestCase("10e",                  false,    2,  2,  2,  2,      10,      10,  10.0),
				new TestCase("-.14",                  true,    4,  1,  1,  4,       0,       0, -0.14),
				new TestCase(".14",                   true,    3,  0,  0,  3,       0,       0,  0.14),
				new TestCase("+.e2",                 false,    4,  1,  1,  2,       0,       0,  0.0),
				new TestCase("1e10xy",               false,    4,  1,  1,  4,       1,       1,  10000000000.0),
				new TestCase("   ",                  false,    3,  3,  3,  3,       0,       0,  0.0),
				new TestCase("     -",               false,    6,  6,  6,  6,       0,       0,  0.0),
				new TestCase("       d",             false,    7,  7,  7,  7,       0,       0,  0.0),
				new TestCase("  0  ",                false,    3,  3,  3,  3,       0,       0,  0.0),
				new TestCase(" 2545as fsdf",         false,    5,  5,  5,  5,    2545,    2545,  2545.0),
				new TestCase(" 54.dadasdasd",        false,    4,  3,  3,  4,      54,      54,  54.0),
				new TestCase("54. ",                 false,    3,  2,  2,  3,      54,      54,  54.0),
				new TestCase("2.",                    true,    2,  1,  1,  2,       2,       2,  2.0),
				new TestCase("2.e",                  false,    2,  1,  1,  2,       2,       2,  2.0),
				new TestCase("2.e+",                 false,    2,  1,  1,  2,       2,       2,  2.0),
				new TestCase(".",                    false,    1,  0,  0,  1,       0,       0,  0.0),
				new TestCase("+.",                   false,    2,  1,  1,  2,       0,       0,  0.0),
				new TestCase("-.",                   false,    2,  1,  1,  2,       0,       0,  0.0),
				new TestCase("-",                    false,    1,  1,  1,  1,       0,       0,  0.0),
				new TestCase("+",                    false,    1,  1,  1,  1,       0,       0,  0.0),
				new TestCase("",                     false,    0,  0,  0,  0,       0,       0,  0.0),
				new TestCase(null,                   false,    0,  0,  0,  0,       0,       0,  0.0),
				new TestCase("10e1111111111111111",   true,    6,  2,  2, 19,      10,      10,  Double.PositiveInfinity),
				new TestCase("10e-1111111111111111",  true,   20,  2,  2, 20,      10,      10,  0.0),
				new TestCase("0e-1111111111111111",   true,   19,  1,  1, 19,       0,       0,  0.0),
                new TestCase("89.99",                 true,    5,  2,  2,  5,      89,      89,  89.99),
                new TestCase("-12.3",                 true,    5,  3,  3,  5,     -12,     -12, -12.3),
                new TestCase("0.12345678901234567890123456789",true,   31, 1,   1, 31,       0,       0,  0.12345678901234567890123456789),
			    new TestCase("0.00000000000034567890123456789",true,   31, 1,   1, 31,       0,       0,  0.00000000000034567890123456789),
                new TestCase("1.89",                  true,    4,  1,  1,  4,       1,       1,  1.89),
			};

        [TestMethod]
        static void TestIsNumber()
        {
            foreach (TestCase c in cases)
            {
                int d, l, iv, p = 0;
                double dv;
                long lv;
                var info = PHP.Core.Convert.IsNumber(c.s, (c.s != null) ? c.s.Length : 0, p, out l, out d, out iv, out lv, out dv);

                Assert.IsTrue(c.isnum == ((info & PHP.Core.Convert.NumberInfo.IsNumber) != 0));
                //Assert.AreEqual(c.p, p);
                //Assert.AreEqual(c.i, i);
                Assert.AreEqual(c.l, l);
                Assert.AreEqual(c.d, d);
                Assert.AreEqual(c.iv, iv);
                Assert.IsTrue(Double.IsNaN(c.dv) || c.dv == dv);
                Assert.AreEqual(c.lv, lv);
            }
        }
    }
}
