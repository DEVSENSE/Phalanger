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
using System.Threading;

namespace PHP.CoreCLR
{
    /// <summary>
    /// Represents a time zone. 
    /// </summary>
    /// <remarks>
    /// There isn't this class in Silverlight, so this is our implementantion. It's not 100% compatible!
    /// </remarks>
    public abstract class TimeZone
    {

        private static TimeZone currentTimeZone = null;

        private static Object internalSyncObject;
        private static Object InternalSyncObject
        {
            get
            {
                if (internalSyncObject == null)
                {
                    Object o = new Object();
                    Interlocked.CompareExchange(ref internalSyncObject, o, null);
                }
                return internalSyncObject;
            }
        }

        /// <summary>
        /// Initializes a new instance of the TimeZone class. 
        /// </summary>
        protected TimeZone()
        {
        }

        /// <summary>
        /// Gets the standard time zone name. 
        /// </summary>
        public abstract String StandardName
        {
            get;
        }

        /// <summary>
        /// Gets the daylight saving time zone name. 
        /// </summary>
        public abstract String DaylightName
        {
            get;
        }


        /// <summary>
        /// Returns the Coordinated Universal Time (UTC) offset for the specified local time. 
        /// </summary>
        /// <param name="time">A date and time value.</param>
        /// <returns>The Coordinated Universal Time (UTC) offset from Time. </returns>
        public abstract TimeSpan GetUtcOffset(DateTime time);


        /// <summary>
        /// Returns the daylight saving time period for a particular year. 
        /// </summary>
        /// <param name="year">The year that the daylight saving time period applies to. </param>
        /// <returns>A DaylightTime object that contains the start and end date for daylight saving time in year.</returns>
        public abstract DaylightTime GetDaylightChanges(int year);

        /// <summary>
        /// Returns a value indicating whether the specified date and time is within a daylight saving time period. 
        /// </summary>
        /// <param name="time">A date and time.</param>
        /// <returns></returns>
        public virtual bool IsDaylightSavingTime(DateTime time)
        {
            return TimeZoneInfo.Local.IsDaylightSavingTime(time);
        }

        /// <summary>
        /// Gets the time zone of the current computer. 
        /// </summary>
        public static TimeZone CurrentTimeZone
        {
            get
            {
                TimeZone tz = currentTimeZone;
                if (tz == null)
                {
                    lock (InternalSyncObject)
                    {
                        if (currentTimeZone == null)
                        {
                            currentTimeZone = new CurrentSystemTimeZone();
                        }
                        tz = currentTimeZone;
                    }
                }
                return (tz);
            }
        }

        /// <summary>
        /// Returns the Coordinated Universal Time (UTC) that corresponds to a specified time. 
        /// </summary>
        public virtual DateTime ToUniversalTime(DateTime time)
        {
            DateTime actTime = time;

            if (time.Kind == DateTimeKind.Unspecified)
                actTime = new DateTime(time.Ticks, DateTimeKind.Utc); // TimeZone and TimeZoneInfo have different assumption about DateTimeKind.Unspecified

            return new DateTime(TimeZoneInfo.ConvertTime(actTime, TimeZoneInfo.Utc).Ticks, DateTimeKind.Utc);
        }

        //
        // Convert the specified datetime value from UTC to the local time based on the time zone.
        //
        public virtual DateTime ToLocalTime(DateTime time)
        {
            DateTime actTime = time;

            if (time.Kind == DateTimeKind.Unspecified)
                actTime = new DateTime(time.Ticks, DateTimeKind.Utc); // TimeZone and TimeZoneInfo have different assumption about DateTimeKind.Unspecified

            return new DateTime(TimeZoneInfo.ConvertTime(actTime, TimeZoneInfo.Local).Ticks, DateTimeKind.Local);
        }

    }

}
