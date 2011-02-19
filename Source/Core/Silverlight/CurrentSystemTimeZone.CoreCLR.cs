using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace PHP.CoreCLR
{
    internal class CurrentSystemTimeZone : TimeZone
    {

        internal CurrentSystemTimeZone()
        {
        }

        public override String StandardName
        {
            get
            {
                return TimeZoneInfo.Local.StandardName;
            }
        }

        public override String DaylightName
        {
            get
            {
                return TimeZoneInfo.Local.DaylightName;
            }
        }

        public override DaylightTime GetDaylightChanges(int year)
        {
            throw new InvalidOperationException("CurrentSystemTimeZone.GetDaylightChanges method isn't supported on Phalanger Silverlight.");
        }

        public override TimeSpan GetUtcOffset(DateTime time)
        {
            return TimeZoneInfo.Local.GetUtcOffset(time);
        }

    }
}
