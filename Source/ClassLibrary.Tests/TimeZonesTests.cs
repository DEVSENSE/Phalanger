using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PHP.Library;

namespace PHP.Library.Tests
{
    [TestClass]
    public class TimeZonesTests
    {

        //        //[Test(true)]
        //        static void SortZones()
        //        {
        //            InitTables();

        //#if !SILVERLIGHT
        //            Array.Sort(zones, CaseInsensitiveComparer.DefaultInvariant);
        //#else
        //            Array.Sort(zones, StringComparer.InvariantCultureIgnoreCase);
        //#endif


        //            Console.WriteLine();
        //            foreach (string z in zones)
        //            {
        //                Console.WriteLine("\"{0}\",", z);
        //            }
        //        }

        //        [Test]
        //        static void TestSorted()
        //        {
        //            InitTables();
        //            string[] sorted = (string[])zones.Clone();

        //#if !SILVERLIGHT
        //            Array.Sort(sorted, CaseInsensitiveComparer.DefaultInvariant);
        //#else
        //            Array.Sort(sorted, StringComparer.InvariantCultureIgnoreCase);
        //#endif

        //            for (int i = 0; i < zones.Length; i++)
        //                Debug.Assert(sorted[i] == zones[i]);
        //        }

        [TestMethod]
        public void TestGetTimeZone()
        {
            TimeZoneInfo zone;

            zone = PhpTimeZone.GetTimeZone("Europe/Prague");
            Assert.IsTrue(zone != null && zone.Id == "Europe/Prague");

            zone = PhpTimeZone.GetTimeZone("europe/prague");
            Assert.IsTrue(zone != null && zone.Id == "Europe/Prague");

            zone = PhpTimeZone.GetTimeZone("foo");
            Assert.IsNull(zone);
        }
    }
}
