using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PHP.Core;

namespace PHP.Library.Tests
{
    [TestClass]
    public class PhpDateTimeTests
    {
        private static TimeZoneInfo/*!*/NepalTimeZone { get { return PhpTimeZone.GetTimeZone("Asia/Katmandu"); } }// = TimeZoneInfo.FindSystemTimeZoneById("Nepal Standard Time");// new _NepalTimeZone();
        private static TimeZoneInfo/*!*/PacificTimeZone { get { return PhpTimeZone.GetTimeZone("America/Los_Angeles"); } }//  = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");//new _PacificTimeZone();
        private static TimeZoneInfo/*!*/GmtTimeZone { get { return PhpTimeZone.GetTimeZone("Etc/GMT"); } }//  = TimeZoneInfo.FindSystemTimeZoneById("GTM");

        [TestMethod]
        public void TestGetTimeOfDay()
        {
            PhpArray result;

            result = PhpDateTime.GetTimeOfDay(new DateTime(2005, 10, 1), PacificTimeZone);
            Assert.AreEqual((int)result["minuteswest"], 480);
            Assert.AreEqual((int)result["dsttime"], 1);

            result = PhpDateTime.GetTimeOfDay(new DateTime(2005, 11, 1), PacificTimeZone);
            Assert.AreEqual((int)result["minuteswest"], 480);
            Assert.AreEqual((int)result["dsttime"], 1);

            result = PhpDateTime.GetTimeOfDay(new DateTime(2005, 11, 1), NepalTimeZone);
            Assert.AreEqual((int)result["minuteswest"], -345);
            Assert.AreEqual((int)result["dsttime"], 0);

            result = PhpDateTime.GetTimeOfDay(new DateTime(2005, 10, 1), GmtTimeZone);
            Assert.AreEqual((int)result["minuteswest"], 0);
            Assert.AreEqual((int)result["dsttime"], 1);

            result = PhpDateTime.GetTimeOfDay(new DateTime(2005, 11, 1), GmtTimeZone);
            Assert.AreEqual((int)result["minuteswest"], 0);
            Assert.AreEqual((int)result["dsttime"], 1);

            result = PhpDateTime.GetTimeOfDay(new DateTime(2005, 11, 1), DateTimeUtils.UtcTimeZone);
            Assert.AreEqual((int)result["minuteswest"], 0);
            Assert.AreEqual((int)result["dsttime"], 0);
        }

        [TestMethod]
        public void TestGetLocalTime()
        {
#if DEBUG
            PhpArray result1, result2;
            PhpTimeZone.CurrentTimeZone = PhpTimeZone.GetTimeZone("UTC");
            DateTime dt = new DateTime(2005, 11, 4, 5, 4, 3, 132);

            result1 = PhpDateTime.GetLocalTime(dt, false);
            result2 = PhpDateTime.GetLocalTime(dt, true);
            Assert.AreEqual((int)result1[0], 3);
            Assert.AreEqual((int)result1[1], 4);
            Assert.AreEqual((int)result1[2], 5);
            Assert.AreEqual((int)result1[3], 4);
            Assert.AreEqual((int)result1[4], 10);
            Assert.AreEqual((int)result1[5], 105);
            Assert.AreEqual((int)result1[6], 5);
            Assert.AreEqual((int)result1[7], 307);
            Assert.AreEqual((int)result1[8], 0);

            Assert.AreEqual((int)result1[0], (int)result2["tm_sec"]);
            Assert.AreEqual((int)result1[1], (int)result2["tm_min"]);
            Assert.AreEqual((int)result1[2], (int)result2["tm_hour"]);
            Assert.AreEqual((int)result1[3], (int)result2["tm_mday"]);
            Assert.AreEqual((int)result1[4], (int)result2["tm_mon"]);
            Assert.AreEqual((int)result1[5], (int)result2["tm_year"]);
            Assert.AreEqual((int)result1[6], (int)result2["tm_wday"]);
            Assert.AreEqual((int)result1[7], (int)result2["tm_yday"]);
            Assert.AreEqual((int)result1[8], (int)result2["tm_isdst"]);
#endif
        }

        struct StringToTimeCase
        {
            public string String;
            public int StartTime;
            public string Result;
            public TimeZoneInfo[] Zones;

            public StringToTimeCase(string str, int start, string result, TimeZoneInfo[] zones)
            {
                this.String = str;
                this.StartTime = start;
                this.Result = result;
                this.Zones = zones;
            }

            public StringToTimeCase(string str, string result, TimeZoneInfo[] zones)
                : this(str, 0, result, zones)
            {
            }

            public StringToTimeCase(string str, int locMonth, int locDay, int locYear, TimeZoneInfo zone, string result, TimeZoneInfo[] zones)
                : this(str, DateTimeUtils.UtcToUnixTimeStamp(TimeZoneInfo.ConvertTimeToUtc(new DateTime(locYear, locMonth, locDay), zone)),
                  result, zones)
            {
            }

            public StringToTimeCase(string str, DateTime local, TimeZoneInfo zone, string result, TimeZoneInfo[] zones)
                : this(str, DateTimeUtils.UtcToUnixTimeStamp(TimeZoneInfo.ConvertTimeToUtc(local, zone)),
                  result, zones)
            {
            }
        }

        [TestMethod]
        public void TestStringToTime()
        {
            TimeZoneInfo[] all_zones =
		  {
		    NepalTimeZone,
		    PacificTimeZone,
		    GmtTimeZone
		  };

            var utc_zone = DateTimeUtils.UtcTimeZone;
            var nep_zone = NepalTimeZone;
            var pac_zone = PacificTimeZone;

            TimeZoneInfo[] utc_zones = { utc_zone };
            TimeZoneInfo[] nep_zones = { nep_zone };
            TimeZoneInfo[] pac_zones = { pac_zone };

            DateTime time1 = new DateTime(2005, 11, 13, 17, 41, 43);
            // mktime(17,41,43,11,13,2005);

            StringToTimeCase[] cases =
		  {
		    new StringToTimeCase("10 September 2000",                time1,pac_zone, "", pac_zones),
		    new StringToTimeCase("+0545",                            time1,pac_zone, "", pac_zones),

//		    new StringToTimeCase("+0545",                            time1,pac_zone, "11:56:43 11/13/2005", pac_zones),
//		    new StringToTimeCase("11/31/2005",                       time1,pac_zone, "17:41:43 11/13/2005", pac_zones),
//		    new StringToTimeCase("-1 month +0545",11,01,2005,pac_zone, "20:15:00 09-30-2005", pac_zones),
//        new StringToTimeCase("@-1519789808",null,pac_zones),
//		    new StringToTimeCase( "1/1/1900",                                        null, all_zones),
//		    new StringToTimeCase("11/1/2005",                       "00:00:00 11/01/2005", nep_zones),
//
//		    // note: goes over daylight savings change date:
//		    new StringToTimeCase( "+1 month",  10,01,2005,utc_zone, "00:00:00 11/01/2005", utc_zones),
//		    new StringToTimeCase( "+1 month",  10,01,2005,nep_zone, "00:00:00 11/01/2005", nep_zones),
//		    new StringToTimeCase( "+1 month",  10,01,2005,pac_zone, "00:00:00 11/01/2005", pac_zones),
//		    new StringToTimeCase( "-1 month",  11,01,2005,utc_zone, "00:00:00 10/01/2005", utc_zones),
//		    new StringToTimeCase( "-1 month",  11,01,2005,nep_zone, "00:00:00 10/01/2005", nep_zones),
//		    new StringToTimeCase( "-1 month",  11,01,2005,pac_zone, "00:00:00 10/01/2005", pac_zones),
//
//        new StringToTimeCase("now",                              time1,pac_zone, "17:41:43 11/13/2005", pac_zones),
//        new StringToTimeCase("10 September 2000",                time1,pac_zone, "00:00:00 09/10/2000", pac_zones),
//        new StringToTimeCase("+1 day",                           time1,pac_zone, "17:41:43 11/14/2005", pac_zones),
//        new StringToTimeCase("+1 week",                          time1,pac_zone, "17:41:43 11/20/2005", pac_zones),
//        new StringToTimeCase("+1 week 2 days 4 hours 2 seconds", time1,pac_zone, "21:41:45 11/22/2005", pac_zones),
//        new StringToTimeCase("next Thursday",                    time1,pac_zone, "00:00:00 11/17/2005", pac_zones),
//        new StringToTimeCase("last Monday",                      time1,pac_zone, "00:00:00 11/07/2005", pac_zones),
//        new StringToTimeCase("2004-12-31",                       time1,pac_zone, "00:00:00 12/31/2004", pac_zones),
//        new StringToTimeCase("2005-04-15",                       time1,pac_zone, "00:00:00 04/15/2005", pac_zones),
//        new StringToTimeCase("last Wednesday",                   time1,pac_zone, "00:00:00 11/09/2005", pac_zones),
//        new StringToTimeCase("04/05/2005",                       time1,pac_zone, "00:00:00 04/05/2005", pac_zones),
//        new StringToTimeCase("Thu, 31 Jul 2003 13:02:39 -0700",  time1,pac_zone, "13:02:39 07/31/2003", pac_zones),
//        new StringToTimeCase("today 00:00:00",                   time1,pac_zone, "00:00:00 11/13/2005", pac_zones),
//        new StringToTimeCase("last Friday",                      time1,pac_zone, "00:00:00 11/11/2005", pac_zones),
//        new StringToTimeCase("2004-12-01",                       time1,pac_zone, "00:00:00 12/01/2004", pac_zones),
//        new StringToTimeCase("- 1week",                          time1,pac_zone, "16:00:00 12/31/1969", pac_zones),
//        new StringToTimeCase("2004-06-13 09:20:00.0",            time1,pac_zone, "09:20:00 06/13/2004", pac_zones),
//        new StringToTimeCase("+10 seconds",                      time1,pac_zone, "17:41:53 11/13/2005", pac_zones),
//        new StringToTimeCase("2004-04-04 02:00:00 GMT",          time1,pac_zone, "18:00:00 04/03/2004", pac_zones),
//        new StringToTimeCase("2004-04-04 01:59:59 UTC",          time1,pac_zone, "16:00:00 12/31/1969", pac_zones),
//        new StringToTimeCase("2004-06-13 09:20:00.0",            time1,pac_zone, "09:20:00 06/13/2004", pac_zones),
//        new StringToTimeCase("2004-04-04 02:00:00",              time1,pac_zone, "03:00:00 04/04/2004", pac_zones),
//        new StringToTimeCase("last sunday 12:00:00",             time1,pac_zone, "12:00:00 11/06/2005", pac_zones),
//        new StringToTimeCase("last sunday",                      time1,pac_zone, "00:00:00 11/06/2005", pac_zones),
//        new StringToTimeCase("01-jan-70 01:00",                  time1,pac_zone, "01:00:00 01/01/1970", pac_zones),
//        new StringToTimeCase("01-jan-70 02:00",                  time1,pac_zone, "02:00:00 01/01/1970", pac_zones),
		  };

            foreach (StringToTimeCase c in cases)
            {
                foreach (var zone in c.Zones)
                {
                    //DateTimeUtils.SetCurrentTimeZone(zone);

                    object timestamp = PhpDateTime.StringToTime(c.String, c.StartTime);

                    //          string str = null;
                    //          if (timestamp is int)
                    //            str = FormatDate("H:i:s m/d/Y",(int)timestamp); else
                    //            Debug.Assert(!(bool)timestamp);
                    //
                    //          if (str!=c.Result)
                    //            Debug.Fail();
                }
            }
        }
    }
}
