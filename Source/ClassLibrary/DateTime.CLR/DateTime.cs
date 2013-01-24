
/*

 Copyright (c) 2004-2006 Tomas Matousek and Pavel Novak.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

TODO:
  - sunset/sunrise calculations changed (PHP 5.1.2)
  - Added support for exif date format in strtotime(). (PHP 5.1.3) 
*/

using System;
using System.IO;
using System.Text;
using System.Globalization;
using PHP.Core;
using PHP.Core.Reflection;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Runtime.InteropServices;

#if SILVERLIGHT
using PHP.CoreCLR;
#endif

namespace PHP.Library
{
    #region DateTime

    /// <summary>
    /// Representation of date and time.
    /// </summary>
#if !SILVERLIGHT
    [Serializable]
#endif
    [ImplementsType("DateTime")]
    public class __PHP__DateTime : PhpObject
    {
        #region Constants

        public const string ATOM = PhpDateTime.FormatAtom;// @"Y-m-d\TH:i:sP";
        public const string COOKIE = PhpDateTime.FormatCookie;// @"l, d-M-y H:i:s T";
        public const string ISO8601 = PhpDateTime.FormatISO8601;// @"Y-m-d\TH:i:sO";
        public const string RFC822 = PhpDateTime.FormatRFC822;// @"D, d M y H:i:s O";
        public const string RFC850 = PhpDateTime.FormatRFC850;// @"l, d-M-y H:i:s T";
        public const string RFC1036 = PhpDateTime.FormatRFC1036;// @"D, d M y H:i:s O";
        public const string RFC1123 = PhpDateTime.FormatRFC1123;// @"D, d M Y H:i:s O";
        public const string RFC2822 = PhpDateTime.FormatRFC2822;// @"D, d M Y H:i:s O";
        public const string RFC3339 = PhpDateTime.FormatRFC3339;// @"Y-m-d\TH:i:sP";
        public const string RSS = PhpDateTime.FormatRSS;// @"D, d M Y H:i:s O";
        public const string W3C = PhpDateTime.FormatW3C;// @"Y-m-d\TH:i:sP";

        #endregion

        #region Fields

        // dont see what these are for, no fields/props on php DateTime obj?
        //public PhpReference date = new PhpSmartReference();
        //public PhpReference timezone_type = new PhpSmartReference();
        //public PhpReference timezone = new PhpSmartReference();

        /// <summary>
        /// Get the date-time value, stored in UTC
        /// </summary>
        internal DateTime Time { get; private set; }

        /// <summary>
        /// Get the time zone for this DateTime object
        /// </summary>
        internal TimeZoneInfo TimeZone { get; private set; }

        #endregion

        #region Construction

        /// <summary>
        /// For internal purposes only.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public __PHP__DateTime(ScriptContext context, bool newInstance)
            : base(context, newInstance)
        { }

        /// <summary>
        /// For internal purposes only.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public __PHP__DateTime(ScriptContext context, DTypeDesc caller)
            : base(context, caller)
        { }

#if !SILVERLIGHT
        /// <summary>Deserializing constructor.</summary>
        protected __PHP__DateTime(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }
#endif

        #endregion

        #region Methods

        private static DateTime StrToTime(string timestr, DateTime time)

        {
            if (string.IsNullOrEmpty(timestr) || timestr.EqualsOrdinalIgnoreCase("now"))
            {
                return DateTime.UtcNow;
            }
            else
            {
                var result = PhpDateTime.StringToTime(timestr, DateTimeUtils.UtcToUnixTimeStamp(time));
                if (result is int)
                {
                    return DateTimeUtils.UnixTimeStampToUtc((int)result);
                }
                else
                {
                    return DateTime.UtcNow;
                }
            }
        }

        // public __construct ([ string $time = "now" [, DateTimeZone $timezone = NULL ]] )
        [ImplementsMethod]
        public object __construct(ScriptContext/*!*/context, [Optional]object time, [Optional]object timezone)
        {
            if (timezone == Arg.Default || timezone == null)
            {
                TimeZone = PhpTimeZone.CurrentTimeZone;
            }
            else
            {
                var datetimezone = timezone as DateTimeZone;
                if (datetimezone == null)
                {
                    PhpException.InvalidArgumentType("timezone", "DateTimeZone");
                    TimeZone = PhpTimeZone.CurrentTimeZone;
                }
                else
                {
                    TimeZone = datetimezone.timezone;
                }
            }

            if (TimeZone == null)
            {
                PhpException.InvalidArgument("timezone");
                return null;
            }

            var timestr = (time == Arg.Default) ? "now" : PHP.Core.Convert.ObjectToString(time);
            this.Time = StrToTime(timestr, DateTime.UtcNow);            

            //this.date.Value = this.Time.ToString("yyyy-mm-dd HH:mm:ss");
            //this.timezone_type.Value = 3;
            //this.timezone.Value = TimeZone.Id;

            return null;
        }
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object __construct(object instance, PhpStack stack)
        {
            var arg1 = stack.PeekValueOptional(1);
            var arg2 = stack.PeekValueOptional(2);
            stack.RemoveFrame();
            return ((__PHP__DateTime)instance).__construct(stack.Context, arg1, arg2);
        }

        [ImplementsMethod]
        public object setTimeZone(ScriptContext/*!*/context, object timezone)
        {
            if (timezone == null)
            {
                PhpException.ArgumentNull("timezone");
                return false;
            }

            var tz = timezone as DateTimeZone;
            if (tz == null)
            {
                PhpException.InvalidArgumentType("timezone", "DateTimeZone");
                return false;
            }

            this.TimeZone = tz.timezone;

            return this;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object setTimeZone(object instance, PhpStack stack)
        {
            var tz = stack.PeekValue(1);
            stack.RemoveFrame();

            return ((__PHP__DateTime)instance).setTimeZone(stack.Context, tz);
        }

        [ImplementsMethod]
        public object format(ScriptContext/*!*/context, object format)
        {
            if (format == null)
            {
                PhpException.ArgumentNull("format");
                return false;
            }

            string fm = format.ToString();
            if (string.IsNullOrEmpty(fm))
            {
                return false;
            }

            return PhpDateTime.FormatDate(fm, this.Time, this.TimeZone);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object format(object instance, PhpStack stack)
        {
            var format = stack.PeekValue(1);
            stack.RemoveFrame();

            return ((__PHP__DateTime)instance).format(stack.Context, format);
        }

        [ImplementsMethod]
        public object getOffset(ScriptContext/*!*/context)
        {
            if (this.TimeZone == null)
                return false;

            return (int)this.TimeZone.BaseUtcOffset.TotalSeconds;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object getOffset(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((__PHP__DateTime)instance).getOffset(stack.Context);
        }

        [ImplementsMethod]
        public object modify(ScriptContext/*!*/context, object modify)
        {
            if (modify == null)
            {
                PhpException.ArgumentNull("modify");
                return false;
            }

            string strtime = PHP.Core.Convert.ObjectToString(modify);
            this.Time = StrToTime(strtime, Time);

            return this;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object modify(object instance, PhpStack stack)
        {
            var modify = stack.PeekValue(1);
            stack.RemoveFrame();

            return ((__PHP__DateTime)instance).modify(stack.Context, modify);
        }

        [ImplementsMethod]
        public static object createFromFormat(ScriptContext/*!*/context, object format, object time, [Optional]object timezone)
        {
            // arguments
            var tz = (timezone is DateTimeZone) ? ((DateTimeZone)timezone).timezone : PhpTimeZone.CurrentTimeZone;
            string formatstr = PhpVariable.AsString(format);
            string timestr = PhpVariable.AsString(time);
            
            if (formatstr == null)
            {
                PhpException.InvalidArgument("format");
                return false;
            }

            if (timestr == null)
            {
                PhpException.InvalidArgument("time");
                return false;
            }

            // create DateTime from format+time
            int i = 0;  // position in <timestr>
            foreach (var c in formatstr)
            {
                switch (c)
                {
                    //case 'd':
                    //case 'j':
                    //    var day = PHP.Library.StrToTime.DateInfo.ParseUnsignedInt(timestr, ref i, 2);
                    //    // ... use day
                    //    break;
                    //case 'F':
                    //case 'M':
                    //    // parse  ...
                    //    break;
                    default:
                        if (i < timestr.Length && timestr[i] == c)
                        {
                            // match
                            i++;
                        }
                        else
                        {
                            // not match
                            PhpException.InvalidArgument("time");   // time not matching format
                            return false;
                        }
                        break;
                }
            }

            if (i < timestr.Length)
            {
                PhpException.InvalidArgument("time");   // time not matching format
                return false;
            }

            ////
            //return new __PHP__DateTime(context, true)
            //{
            //     //Time = new DateTime(year, month, day, hour, minute, second, millisecond),
            //     TimeZone = tz,
            //};

            throw new NotImplementedException();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object createFromFormat(object instance, PhpStack stack)
        {
            var format = stack.PeekValue(1);
            var time = stack.PeekValue(2);
            var timezone = stack.PeekValueOptional(3);
            stack.RemoveFrame();
            return createFromFormat(stack.Context, format, time, timezone);
        }

        [ImplementsMethod]        
        public object setDate(ScriptContext/*!*/context, object year, object month, object day)
        {
            var y = PHP.Core.Convert.ObjectToInteger(year);
            var m = PHP.Core.Convert.ObjectToInteger(month);
            var d = PHP.Core.Convert.ObjectToInteger(day);
            try
            {
                var time = TimeZoneInfo.ConvertTimeFromUtc(Time, TimeZone);
                this.Time = TimeZoneInfo.ConvertTimeToUtc(
                    new DateTime(
                        y, m, d,
                        time.Hour, time.Minute, time.Second,
                        time.Millisecond
                    ),
                    TimeZone
                );
            }
            catch (ArgumentOutOfRangeException e)
            {
                throw new ArgumentOutOfRangeException(string.Format("The date {0}-{1}-{2} is not valid.", y, m, d), e);
            }


            return this;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object setDate(object instance, PhpStack stack)
        {
            var arg1 = stack.PeekValue(1);
            var arg2 = stack.PeekValue(2);
            var arg3 = stack.PeekValue(3);
            stack.RemoveFrame();
            return ((__PHP__DateTime)instance).setDate(stack.Context, arg1, arg2, arg3);
        }

        [ImplementsMethod]
        public object setTime(ScriptContext/*!*/context, object hour, object minute, object second)
        {
            var h = PHP.Core.Convert.ObjectToInteger(hour);
            var m = PHP.Core.Convert.ObjectToInteger(minute);
            var s = PHP.Core.Convert.ObjectToInteger(second);
            try
            {
                var time = TimeZoneInfo.ConvertTimeFromUtc(Time, TimeZone);
                this.Time = TimeZoneInfo.ConvertTimeToUtc(
                    new DateTime(time.Year, time.Month, time.Day, h, m, s),
                    TimeZone
                );
            }
            catch (ArgumentOutOfRangeException e)
            {
                throw new ArgumentOutOfRangeException(string.Format("The time {0}:{1}:{2} is not valid.", h, m, s), e);
            }


            return this;        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object setTime(object instance, PhpStack stack)
        {
            var arg1 = stack.PeekValue(1);
            var arg2 = stack.PeekValue(2);
            var arg3 = stack.PeekValue(3);
            stack.RemoveFrame();

            return ((__PHP__DateTime)instance).setTime(stack.Context, arg1, arg2, arg3);
        }

        [ImplementsMethod]
        public object getTimestamp(ScriptContext/*!*/context)
        {
            return DateTimeUtils.UtcToUnixTimeStamp(Time);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object getTimestamp(object instance, PhpStack stack)
        {
            stack.RemoveFrame();

            return ((__PHP__DateTime)instance).getTimestamp(stack.Context);
        }

        public override int CompareTo(object obj, System.Collections.IComparer comparer)
        {
            var other = obj as __PHP__DateTime;
            return other != null
                ? Time.CompareTo(other.Time)
                : base.CompareTo(obj, comparer);
        }

        #endregion
    }

    #endregion

	/// <summary>
	/// Functions for date and time manipulation.
	/// </summary>
	/// <threadsafety static="true"/>
	[ImplementsExtension(LibraryDescriptor.ExtDate)]
	public static class PhpDateTime
	{
		/// <summary>
		/// Gets the current local time with respect to the current PHP time zone. 
		/// </summary>
		public static DateTime Now
		{
			get
			{
                return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, PhpTimeZone.CurrentTimeZone);
			}
		}

		#region Constants

		[ImplementsConstant("DATE_ATOM")]
		public const string FormatAtom = @"Y-m-d\TH:i:sP";

		[ImplementsConstant("DATE_COOKIE")]
		public const string FormatCookie = @"D, d M Y H:i:s T";

		[ImplementsConstant("DATE_ISO8601")]
		public const string FormatISO8601 = @"Y-m-d\TH:i:sO";

		[ImplementsConstant("DATE_RFC822")]
        public const string FormatRFC822 = @"D, d M y H:i:s O";

		[ImplementsConstant("DATE_RFC850")]
		public const string FormatRFC850 = @"l, d-M-y H:i:s T";

		[ImplementsConstant("DATE_RFC1123")]
		public const string FormatRFC1123 = @"D, d M Y H:i:s T";

		[ImplementsConstant("DATE_RFC1036")]
		public const string FormatRFC1036 = @"l, d-M-y H:i:s T";

		[ImplementsConstant("DATE_RFC2822")]
		public const string FormatRFC2822 = @"D, d M Y H:i:s T";

        [ImplementsConstant("DATE_RFC3339")]
        public const string FormatRFC3339 = @"Y-m-d\TH:i:sP";

		[ImplementsConstant("DATE_RSS")]
		public const string FormatRSS = @"D, d M Y H:i:s T";

		[ImplementsConstant("DATE_W3C")]
		public const string FormatW3C = @"Y-m-d\TH:i:sO";

		#endregion

        #region date_format, date_create, date_offset_get,  date_modify

        [ImplementsFunction("date_format")]
        [return: CastToFalse]
        public static object DateFormat(__PHP__DateTime datetime, string format)
        {
            // TODO: format it properly
            return FormatDate(format, datetime.Time, datetime.TimeZone);
        }

        /// <summary>
        /// Alias of new <see cref="DateTime"/>
        /// </summary>
        [ImplementsFunction("date_create")]
        [return: CastToFalse]
        public static object DateCreate(ScriptContext/*!*/context)
        {
            return DateCreate(context, null, null);
        }

        /// <summary>
        /// Alias of new <see cref="DateTime"/>
        /// </summary>
        [ImplementsFunction("date_create")]
        [return: CastToFalse]
        public static object DateCreate(ScriptContext/*!*/context, string time)
        {
            return DateCreate(context, time, null);
        }

        /// <summary>
        /// Alias of new <see cref="DateTime"/>
        /// </summary>
        [ImplementsFunction("date_create")]
        [return: CastToFalse]
        public static object DateCreate(ScriptContext/*!*/context, string time, DateTimeZone timezone)
        {
            var dt = new __PHP__DateTime(context, true);
            dt.__construct(context, time, timezone);
            return dt;
        }

        /// <summary>
        /// Returns new DateTime object formatted according to the specified format.
        /// </summary>
        /// <param name="context"><see cref="ScriptContext"/> reference.</param>
        /// <param name="format">The format that the passed in string should be in.</param>
        /// <param name="time">String representing the time.</param>
        /// <returns></returns>
        [ImplementsFunction("date_create_from_format")]
        [return: CastToFalse]
        public static __PHP__DateTime DateCreateFromFormat(ScriptContext/*!*/context, string format, string time)
        {
            return __PHP__DateTime.createFromFormat(context, format, time, Arg.Default) as __PHP__DateTime;
        }

        /// <summary>
        /// Returns new DateTime object formatted according to the specified format.
        /// </summary>
        /// <param name="context"><see cref="ScriptContext"/> reference.</param>
        /// <param name="format">The format that the passed in string should be in.</param>
        /// <param name="time">String representing the time.</param>
        /// <param name="timezone">A DateTimeZone object representing the desired time zone.</param>
        /// <returns></returns>
        [ImplementsFunction("date_create_from_format")]
        [return:CastToFalse]
        public static __PHP__DateTime DateCreateFromFormat(ScriptContext/*!*/context, string format, string time, DateTimeZone timezone)
        {
            return __PHP__DateTime.createFromFormat(context, format, time, timezone) as __PHP__DateTime;
        }

        /// <summary>
        /// Alias of DateTime::getOffset().
        /// </summary>
        [ImplementsFunction("date_offset_get")]
        [return: CastToFalse]
        public static int DateOffsetGet(__PHP__DateTime datetime)
        {
            if (datetime == null)
            {
                PhpException.ArgumentNull("datetime");
                return -1;
            }

            if (datetime.TimeZone == null)
                return -1;

            return (int)datetime.TimeZone.BaseUtcOffset.TotalSeconds;
        }

        /// <summary>
        /// Alias of DateTime::modify().
        /// </summary>
        [ImplementsFunction("date_modify")]
        [return: CastToFalse]
        public static __PHP__DateTime DateModify(ScriptContext/*!*/context, __PHP__DateTime datetime, string modify)
        {
            return datetime.modify(context, modify) as __PHP__DateTime;
        }

        #endregion

        #region date, idate, gmdate

        /// <summary>
		/// Returns a string formatted according to the given format string using the current local time.
		/// </summary>
		/// <param name="format">Format definition for output.</param>
		/// <returns>Formatted string.</returns>
		[ImplementsFunction("date")]
		public static string FormatDate(string format)
		{
			return FormatDate(format, DateTime.UtcNow, PhpTimeZone.CurrentTimeZone);
		}

		/// <summary>
		/// Returns a string formatted according to the given format string using the given integer timestamp.
		/// </summary>
		/// <param name="format">Format definition for output.</param>
		/// <param name="timestamp">Nuber of seconds since 1970 specifying a date.</param>
		/// <returns>Formatted string.</returns>
		[ImplementsFunction("date")]
		public static string FormatDate(string format, int timestamp)
		{
			return FormatDate(format, DateTimeUtils.UnixTimeStampToUtc(timestamp), PhpTimeZone.CurrentTimeZone);
		}

		/// <summary>
		/// Identical to the date() function except that the time returned is Greenwich Mean Time (GMT)
		/// </summary>
		/// <param name="format">Format definition for output.</param>
		/// <returns>Formatted string.</returns>
		[ImplementsFunction("gmdate")]
		public static string FormatUtcDate(string format)
		{
			return FormatDate(format, DateTime.UtcNow, DateTimeUtils.UtcTimeZone);
		}

		/// <summary>              
		/// Identical to the date() function except that the time returned is Greenwich Mean Time (GMT)
		/// </summary>
		/// <param name="format">Format definition for output.</param>
		/// <param name="timestamp">Nuber of seconds since 1970 specifying a date.</param>
		/// <returns>Formatted string.</returns>
		[ImplementsFunction("gmdate")]
		public static string FormatUtcDate(string format, int timestamp)
		{
			return FormatDate(format, DateTimeUtils.UnixTimeStampToUtc(timestamp), DateTimeUtils.UtcTimeZone);
		}

		/// <summary>
		/// Returns a part of current time.
		/// </summary>
		/// <param name="format">Format definition for output.</param>
		/// <returns>Part of the date, e.g. month or hours.</returns>
		[ImplementsFunction("idate")]
		public static int GetDatePart(string format)
		{
			if (format == null || format.Length != 1)
				PhpException.InvalidArgument("format");

			return GetDatePart(format[0], DateTime.UtcNow, PhpTimeZone.CurrentTimeZone);
		}

		/// <summary>
		/// Returns a part of a specified timestamp.
		/// </summary>
		/// <param name="format">Format definition for output.</param>
		/// <param name="timestamp">Nuber of seconds since 1970 specifying a date.</param>
		/// <returns>Part of the date, e.g. month or hours.</returns>
		[ImplementsFunction("idate")]
		public static int GetDatePart(string format, int timestamp)
		{
			if (format == null || format.Length != 1)
				PhpException.InvalidArgument("format");

			return GetDatePart(format[0], DateTimeUtils.UnixTimeStampToUtc(timestamp), PhpTimeZone.CurrentTimeZone);
		}

		private static int GetDatePart(char format, DateTime utc, TimeZoneInfo/*!*/ zone)
		{
            DateTime local = TimeZoneInfo.ConvertTimeFromUtc(utc, zone);// zone.ToLocalTime(utc);

			switch (format)
			{
				case 'B':
					// Swatch Beat (Internet Time) - 000 through 999 
					return GetSwatchBeat(utc);

				case 'd':
					// Day of the month
					return local.Day;

				case 'g':
				case 'h':
					// 12-hour format:
					return (local.Hour == 12) ? 12 : local.Hour % 12;

				case 'G':
				case 'H':
					// 24-hour format:
					return local.Hour;

				case 'i':
					return local.Minute;

				case 'I':
					return zone.IsDaylightSavingTime(local) ? 1 : 0;

				case 'j':
					goto case 'd';

				case 'L':
					return DateTime.IsLeapYear(local.Year) ? 1 : 0;

				case 'm':
					return local.Month;

				case 'n':
					goto case 'm';

				case 's':
					return local.Second;

				case 't':
					return DateTime.DaysInMonth(local.Year, local.Month);

				case 'U':
					return DateTimeUtils.UtcToUnixTimeStamp(utc);

				case 'w':
					// day of the week - 0 (for Sunday) through 6 (for Saturday)
					return (int)local.DayOfWeek;

				case 'W':
					{
						// ISO-8601 week number of year, weeks starting on Monday:
						int week, year;
						GetIsoWeekAndYear(local, out week, out year);
						return week;
					}

				case 'y':
					return local.Year % 100;

				case 'Y':
					return local.Year;

				case 'z':
					return local.DayOfYear - 1;

				case 'Z':
					return (int)zone.GetUtcOffset(local).TotalSeconds;

				default:
					PhpException.InvalidArgument("format");
					return 0;
			}
		}

        internal static string FormatDate(string format, DateTime utc, TimeZoneInfo zone)
        {
            Debug.Assert(zone != null);

            if (format == null)
                return string.Empty;

            DateTime local = TimeZoneInfo.ConvertTimeFromUtc(utc, zone);

            // here we are creating output string
            StringBuilder result = new StringBuilder();
            bool escape = false;

            foreach (char ch in format)
            {
                if (escape)
                {
                    result.Append(ch);
                    escape = false;
                    continue;
                }

                switch (ch)
                {
                    case 'a':
                        // Lowercase Ante meridiem and Post meridiem - am or pm
                        result.Append(local.ToString("tt", DateTimeFormatInfo.InvariantInfo).ToLowerInvariant());
                        break;

                    case 'A':
                        // Uppercase Ante meridiem and Post meridiem - AM or PM 
                        result.Append(local.ToString("tt", DateTimeFormatInfo.InvariantInfo));
                        break;

                    case 'B':
                        // Swatch Beat (Internet Time) - 000 through 999 
                        result.AppendFormat("{0:000}", GetSwatchBeat(utc));
                        break;

                    case 'c':
                        {
                            // ISO 8601 date (added in PHP 5) 2004-02-12T15:19:21+00:00 
                            result.Append(local.ToString("yyyy-MM-dd'T'HH:mm:ss", DateTimeFormatInfo.InvariantInfo));

                            TimeSpan offset = zone.GetUtcOffset(local);
                            result.AppendFormat("{0}{1:00}:{2:00}", (offset.Ticks < 0) ? ""/*offset.Hours already < 0*/ : "+", offset.Hours, offset.Minutes);
                            break;
                        }

                    case 'd':
                        // Day of the month, 2 digits with leading zeros - 01 to 31
                        result.Append(local.ToString("dd", DateTimeFormatInfo.InvariantInfo));
                        break;

                    case 'e':
                        // Timezone identifier (added in PHP 5.1.0)
                        result.Append(zone.Id);
                        break;

                    case 'D':
                        // A textual representation of a day, three letters - Mon through Sun
                        result.Append(local.ToString("ddd", DateTimeFormatInfo.InvariantInfo));
                        break;

                    case 'F':
                        // A full textual representation of a month, such as January or March - January through December 
                        result.Append(local.ToString("MMMM", DateTimeFormatInfo.InvariantInfo));
                        break;

                    case 'g':
                        // 12-hour format of an hour without leading zeros - 1 through 12
                        result.Append(local.ToString("%h", DateTimeFormatInfo.InvariantInfo));
                        break;

                    case 'G':
                        // 24-hour format of an hour without leading zeros - 0 through 23
                        result.Append(local.ToString("%H", DateTimeFormatInfo.InvariantInfo));
                        break;

                    case 'h':
                        // 12-hour format of an hour with leading zeros - 01 through 12
                        result.Append(local.ToString("hh", DateTimeFormatInfo.InvariantInfo));
                        break;

                    case 'H':
                        // 24-hour format of an hour with leading zeros - 00 through 23
                        result.Append(local.ToString("HH", DateTimeFormatInfo.InvariantInfo));
                        break;

                    case 'i':
                        // Minutes with leading zeros - 00 to 59
                        result.Append(local.ToString("mm", DateTimeFormatInfo.InvariantInfo));
                        break;

                    case 'I':
                        // Whether or not the date is in daylights savings time - 1 if Daylight Savings Time, 0 otherwise.
                        result.Append(zone.IsDaylightSavingTime(local) ? "1" : "0");
                        break;

                    case 'j':
                        // Day of the month without leading zeros - 1 to 31
                        result.Append(local.ToString("%d", DateTimeFormatInfo.InvariantInfo));
                        break;

                    case 'l':
                        // A full textual representation of the day of the week - Sunday through Saturday
                        result.Append(local.ToString("dddd", DateTimeFormatInfo.InvariantInfo));
                        break;

                    case 'L':
                        // Whether it's a leap year - 1 if it is a leap year, 0 otherwise.
                        result.Append(DateTime.IsLeapYear(local.Year) ? "1" : "0");
                        break;

                    case 'm':
                        // Numeric representation of a month, with leading zeros - 01 through 12
                        result.Append(local.ToString("MM", DateTimeFormatInfo.InvariantInfo));
                        break;

                    case 'M':
                        // A short textual representation of a month, three letters - Jan through Dec
                        result.Append(local.ToString("MMM", DateTimeFormatInfo.InvariantInfo));
                        break;

                    case 'n':
                        // Numeric representation of a month, without leading zeros - 1 through 12
                        result.Append(local.ToString("%M", DateTimeFormatInfo.InvariantInfo));
                        break;

                    case 'N':
                        // ISO-8601 numeric representation of the day of the week (added in PHP 5.1.0)
                        int day_of_week = (int)local.DayOfWeek;
                        result.Append(day_of_week == 0 ? 7 : day_of_week);
                        break;

                    case 'o':
                        {
                            // ISO-8601 year number. This has the same value as Y, except that if the ISO
                            // week number (W) belongs to the previous or next year, that year is used instead.
                            // (added in PHP 5.1.0)
                            int week, year;
                            GetIsoWeekAndYear(local, out week, out year);
                            result.Append(year);
                            break;
                        }

                    case 'O':
                        {
                            // Difference to Greenwich time (GMT) in hours Example: +0200
                            TimeSpan offset = zone.GetUtcOffset(local);
                            string sign = (offset.Ticks < 0) ? ((offset.Hours < 0) ? string.Empty : "-") : "+";
                            result.AppendFormat("{0}{1:00}{2:00}", sign, offset.Hours, offset.Minutes);
                            break;
                        }

                    case 'P':
                        {
                            // same as 'O' but with the extra colon between hours and minutes
                            // Difference to Greenwich time (GMT) in hours Example: +02:00
                            TimeSpan offset = zone.GetUtcOffset(local);
                            string sign = (offset.Ticks < 0) ? ((offset.Hours < 0) ? string.Empty : "-") : "+";
                            result.AppendFormat("{0}{1:00}:{2:00}", sign, offset.Hours, offset.Minutes);
                            break;
                        }

                    case 'r':
                        // RFC 822 formatted date Example: Thu, 21 Dec 2000 16:01:07 +0200
                        result.Append(local.ToString("ddd, dd MMM yyyy H:mm:ss ", DateTimeFormatInfo.InvariantInfo));
                        goto case 'O';

                    case 's':
                        // Seconds, with leading zeros - 00 through 59
                        result.Append(local.ToString("ss", DateTimeFormatInfo.InvariantInfo));
                        break;

                    case 'S':
                        result.Append(GetDayNumberSuffix(local.Day));
                        break;

                    case 't':
                        // Number of days in the given month 28 through 31
                        result.Append(DateTime.DaysInMonth(local.Year, local.Month));
                        break;

                    case 'T':
                        // Timezone setting of this machine Examples: EST, MDT ...
                        result.Append(zone.IsDaylightSavingTime(local) ? zone.DaylightName : zone.StandardName);
                        break;

                    case 'U':
                        // Seconds since the Unix Epoch (January 1 1970 00:00:00 GMT)
                        result.Append(DateTimeUtils.UtcToUnixTimeStamp(utc));
                        break;

                    case 'u':
                        // Microseconds (added in PHP 5.2.2)
                        result.Append((utc.Millisecond / 1000).ToString("D6"));
                        break;

                    case 'w':
                        // Numeric representation of the day of the week - 0 (for Sunday) through 6 (for Saturday)
                        result.Append((int)local.DayOfWeek);
                        break;

                    case 'W':
                        {
                            // ISO-8601 week number of year, weeks starting on Monday (added in PHP 4.1.0) Example: 42 (the 42nd week in the year)
                            int week, year;
                            GetIsoWeekAndYear(local, out week, out year);
                            result.Append(week);
                            break;
                        }

                    case 'y':
                        // A two digit representation of a year Examples: 99 or 03
                        result.Append(local.ToString("yy", DateTimeFormatInfo.InvariantInfo));
                        break;

                    case 'Y':
                        // A full numeric representation of a year, 4 digits Examples: 1999 or 2003
                        result.Append(local.ToString("yyyy", DateTimeFormatInfo.InvariantInfo));
                        break;

                    case 'z':
                        // The day of the year starting from 0
                        result.Append(local.DayOfYear - 1);
                        break;

                    case 'Z':
                        // TimeZone offset in seconds:
                        result.Append((int)zone.GetUtcOffset(local).TotalSeconds);
                        break;

                    case '\\':
                        // Escape char. Output next character directly to the result.
                        escape = true;
                        break;

                    default:
                        // unrecognized character, print it as-is.
                        result.Append(ch);
                        break;
                }
            }

            if (escape)
                result.Append('\\');

            return result.ToString();
        }

		/// <summary>
		/// Converts a given <see cref="DateTime"/> to the ISO week of year number and ISO year number.
		/// </summary>
		/// <param name="dt">The <see cref="DateTime"/>.</param>
		/// <param name="week">The ISO week of year number.</param>
		/// <param name="year">The ISO year number.</param>
		private static void GetIsoWeekAndYear(DateTime dt, out int week, out int year)
		{
			int weekDay = (int)dt.DayOfWeek - 1; // Day of week (0 for Monday .. 6 for Sunday)
			int yearDay = dt.DayOfYear;     // Days since January 1st (1 .. 367)
			int firstDay = (7 + weekDay - yearDay % 7 + 1) % 7;	// Weekday of 1st January

			if ((yearDay <= 7 - firstDay) && firstDay > 3)
			{
				// Week is a last year week (52 or 53)
				week = (firstDay == 4 || (firstDay == 5 && DateTime.IsLeapYear(dt.Year - 1))) ? 53 : 52;
				year = dt.Year - 1;
			}
			else if ((DateTime.IsLeapYear(dt.Year) ? 366 : 365) - yearDay < 3 - weekDay)
			{
				// Week is a next year week (1)
				week = 1;
				year = dt.Year + 1;
			}
			else
			{
				// Normal week
				week = (yearDay + 6 - weekDay + firstDay) / 7;
				if (firstDay > 3) week--;
				year = dt.Year;
			}
		}

		private static int GetSwatchBeat(DateTime utc)
		{
			int seconds = DateTimeUtils.UtcToUnixTimeStamp(utc);
			int beat = (int)(((seconds - (seconds - ((seconds % 86400) + 3600))) * 10) / 864) % 1000;
			return (beat < 0) ? beat + 1000 : beat;
		}

        /// <summary>
        /// English ordinal suffix for the day of the month, 2 characters - st, nd, rd or th.
        /// </summary>
        /// <param name="DayNumber">Number of the day. In [1..31].</param>
        /// <returns>st, nd, rd or th</returns>
        private static string GetDayNumberSuffix(int DayNumber /* = 1..31 */)
        {
            Debug.Assert(DayNumber >= 1 && DayNumber <= 31);

            int DayNumber10 = DayNumber % 10;

            if (DayNumber10 == 1) { if (DayNumber/*%100*/ != 11) return "st"; }
            else if (DayNumber10 == 2) { if (DayNumber/*%100*/ != 12) return "nd"; }
            else if (DayNumber10 == 3) { if (DayNumber/*%100*/ != 13) return "rd"; }
            
            return "th";
        }

		#endregion

        #region strftime, gmstrftime

        /// <summary>
		/// Returns a string formatted according to the given format string using the current local time.
		/// </summary>
		/// <param name="format">Format of the string.</param>
		/// <returns>Formatted string representing date and time.</returns>
		[ImplementsFunction("strftime")]
		public static string FormatTime(string format)
		{
			return FormatTime(format, DateTime.UtcNow, PhpTimeZone.CurrentTimeZone);
		}

		/// <summary>
		/// Returns a string formatted according to the given format string using the given timestamp.
		/// </summary>
		/// <param name="format">Format of the string.</param>
		/// <param name="timestamp">Number of seconds since 1970 representing the time to format.</param>
		/// <returns>Formatted string representing date and time.</returns>
		[ImplementsFunction("strftime")]
		public static string FormatTime(string format, int timestamp)
		{
			return FormatTime(format, DateTimeUtils.UnixTimeStampToUtc(timestamp), PhpTimeZone.CurrentTimeZone);
		}

		/// <summary>
		/// Behaves the same as <c>strftime</c> except that the time returned is Greenwich Mean Time (GMT).
		/// </summary>
		/// <param name="format">Format of the string.</param>
		/// <returns>Formatted string representing date and time.</returns>
		[ImplementsFunction("gmstrftime")]
		public static string FormatUtcTime(string format)
		{
			return FormatTime(format, DateTime.UtcNow, DateTimeUtils.UtcTimeZone);
		}

		/// <summary>
		/// Behaves the same as <c>strftime</c> except that the time returned is Greenwich Mean Time (GMT).
		/// </summary>
		/// <param name="format">Format of the string.</param>
		/// <param name="timestamp">Number of seconds since 1970 representing the time to format.</param>
		/// <returns>Formatted string representing date and time.</returns>
		[ImplementsFunction("gmstrftime")]
		public static string FormatUtcTime(string format, int timestamp)
		{
			return FormatTime(format, DateTimeUtils.UnixTimeStampToUtc(timestamp), DateTimeUtils.UtcTimeZone);
		}

		/// <summary>
		/// Implementation of <see cref="FormatTime(string,int)"/> function.
		/// </summary>
		private static string FormatTime(string format, DateTime utc, TimeZoneInfo/*!*/ zone)
		{
			// Possibly bug in framework? "h" and "hh" just after midnight shows 12, not 0
			if (format == null) return "";

            DateTime local = TimeZoneInfo.ConvertTimeFromUtc(utc, zone);// zone.ToLocalTime(utc);
			DateTimeFormatInfo info = Locale.GetCulture(Locale.Category.Time).DateTimeFormat;

			StringBuilder result = new StringBuilder();

			bool specialChar = false;

			foreach (char ch in format)
			{
				if (!specialChar)
				{
					if (ch == '%')
						specialChar = true;
					else
						result.Append(ch);

					continue;
				}

				// we have special character
				switch (ch)
				{
					case 'a':
						// abbreviated weekday name according to the current locale
						result.Append(local.ToString("ddd", info));
						break;

					case 'A':
						// full weekday name according to the current locale
						result.Append(local.ToString("dddd", info));
						break;

					case 'b':
						// abbreviated month name according to the current locale
						result.Append(local.ToString("MMM", info));
						break;

					case 'B':
						// full month name according to the current locale
						result.Append(local.ToString("MMMM", info));
						break;

					case 'c':
						// preferred date and time representation for the current locale
						result.Append(local.ToString(info));
						break;

					case 'C':
						// century number (the year divided by 100 and truncated to an integer, range 00 to 99)
						result.Append(local.Year / 100);
						break;

					case 'd':
						// day of the month as a decimal number (range 01 to 31)
						result.Append(local.ToString("dd", info));
						break;

					case 'D':
						// same as %m/%d/%y
						result.Append(local.ToString("MM\\/dd\\/yy", info));
						break;

					case 'e':
						// day of the month as a decimal number, a single digit is preceded by a space (range ' 1' to '31')
						result.AppendFormat("{0,2}", local.Day);
						break;

					case 'g':
						{
							// like %G, but without the century.
							int week, year;
							GetIsoWeekAndYear(local, out week, out year);
							result.AppendFormat("{0:00}", year % 100);
							break;
						}

					case 'G': // The 4-digit year corresponding to the ISO week number.
						{
							int week, year;
							GetIsoWeekAndYear(local, out week, out year);
							result.AppendFormat("{0:0000}", year);
							break;
						}

					case 'h':
						// same as %b
						goto case 'b';

					case 'H':
						// hour as a decimal number using a 24-hour clock (range 00 to 23)
						result.Append(local.ToString("HH", info));
						break;

					case 'I':
						// hour as a decimal number using a 12-hour clock (range 01 to 12)
						result.Append(local.ToString("hh", info));
						break;

					case 'j':
						// day of the year as a decimal number (range 001 to 366)
						result.AppendFormat("{0:000}", local.DayOfYear);
						break;

					case 'm':
						// month as a decimal number (range 01 to 12)
						result.Append(local.ToString("MM", info));
						break;

					case 'M':
						// minute as a decimal number
						result.Append(local.ToString("mm", info));
						break;

					case 'n':
						// newline character
						result.Append('\n');
						break;

					case 'p':
						// either `am' or `pm' according to the given time value,
						// or the corresponding strings for the current locale
						result.Append(local.ToString("tt", info));
						break;

					case 'r':
						// time in a.m. and p.m. notation
						result.Append(local.ToString("hh:mm:ss tt", info));
						break;

					case 'R':
						// time in 24 hour notation
						result.Append(local.ToString("H:mm:ss", info));
						break;

					case 'S':
						// second as a decimal number
						result.Append(local.ToString("ss", info));
						break;

					case 't':
						// tab character
						result.Append('\t');
						break;

					case 'T':
						// current time, equal to %H:%M:%S
						result.Append(local.ToString("HH:mm:ss", info));
						break;

					case 'u':
						// weekday as a decimal number [1,7], with 1 representing Monday
						result.Append(((int)local.DayOfWeek + 5) % 7 + 1);
						break;

					case 'U':
						// week number of the current year as a decimal number, starting with the first 
						// Sunday as the first day of the first week (formula taken from GlibC 2.3.5):
						result.AppendFormat("{0:00}", (local.DayOfYear - 1 - (int)local.DayOfWeek + 7) / 7);
						break;

					case 'V':
						{
							// The ISO 8601:1988 week number of the current year 
							int week, year;
							GetIsoWeekAndYear(local, out week, out year);
							result.AppendFormat("{0:00}", week);
							break;
						}

					case 'w':
						// day of the week as a decimal, Sunday being 0
						result.Append((int)local.DayOfWeek);
						break;

					case 'W':
						// week number of the current year as a decimal number, starting with the first 
						// Monday as the first day of the first week (formula taken from GlibC 2.3.5):
						result.AppendFormat("{0:00}", (local.DayOfYear - 1 - ((int)local.DayOfWeek - 1 + 7) % 7 + 7) / 7);
						break;

					case 'x':
						// preferred date representation for the current locale without the time
						result.Append(local.ToString("d", info));
						break;

					case 'X':
						// preferred time representation for the current locale without the date
						result.Append(local.ToString("T", info));
						break;

					case 'y':
						// year as a decimal number without a century (range 00 to 99)
						result.Append(local.ToString("yy", info));
						break;

					case 'Y':
						// year as a decimal number including the century
						result.Append(local.ToString("yyyy", info));
						break;

					case 'z':
					case 'Z':
						result.Append(zone.IsDaylightSavingTime(local) ? zone.DaylightName : zone.StandardName);
						break;

					case '%':
						result.Append('%');
						break;
				}
				specialChar = false;
			}

			if (specialChar)
				result.Append('%');

			return result.ToString();
		}

		#endregion

		#region NS: strptime

		//		[ImplementsFunction("strptime")]
		//		public static PhpArray StringToTime(string datetime,string format)
		//		{
		//		  
		//		}

		#endregion

		#region gmmktime

		[ImplementsFunction("gmmktime")]
		public static int MakeUtcTime()
		{
			DateTime utc_now = DateTime.UtcNow;
			return MakeUtcTime(utc_now.Hour, utc_now.Minute, utc_now.Second, utc_now.Month, utc_now.Day, utc_now.Year);
		}

		[ImplementsFunction("gmmktime")]
		public static int MakeUtcTime(int hour)
		{
			DateTime utc_now = DateTime.UtcNow;
			return MakeUtcTime(hour, utc_now.Minute, utc_now.Second, utc_now.Month, utc_now.Day, utc_now.Year);
		}

		[ImplementsFunction("gmmktime")]
		public static int MakeUtcTime(int hour, int minute)
		{
			DateTime utc_now = DateTime.UtcNow;
			return MakeUtcTime(hour, minute, utc_now.Second, utc_now.Month, utc_now.Day, utc_now.Year);
		}

		[ImplementsFunction("gmmktime")]
		public static int MakeUtcTime(int hour, int minute, int second)
		{
			DateTime utc_now = DateTime.UtcNow;
			return MakeUtcTime(hour, minute, second, utc_now.Month, utc_now.Day, utc_now.Year);
		}

		[ImplementsFunction("gmmktime")]
		public static int MakeUtcTime(int hour, int minute, int second, int month)
		{
			DateTime utc_now = DateTime.UtcNow;
			return MakeUtcTime(hour, minute, second, month, utc_now.Day, utc_now.Year);
		}

		[ImplementsFunction("gmmktime")]
		public static int MakeUtcTime(int hour, int minute, int second, int month, int day)
		{
			DateTime utc_now = DateTime.UtcNow;
			return MakeUtcTime(hour, minute, second, month, day, utc_now.Year);
		}

		[ImplementsFunction("gmmktime")]
		public static int MakeUtcTime(int hour, int minute, int second, int month, int day, int year, int dummy)
		{
			// According to PHP manual daylight savings time parameter ignored
			return MakeUtcTime(hour, minute, second, month, day, year);
		}

		[ImplementsFunction("gmmktime")]
		public static int MakeUtcTime(int hour, int minute, int second, int month, int day, int year)
		{
			return DateTimeUtils.UtcToUnixTimeStamp(MakeDateTime(hour, minute, second, month, day, year));
		}

		#endregion

		#region mktime

		/// <summary>
		/// Returns the Unix timestamp for current time.
		/// </summary>
		/// <returns>Unix timestamp.</returns>
		[ImplementsFunction("mktime")]
		public static int MakeTime()
		{
			DateTime now = Now;
			return MakeTime(now.Hour, now.Minute, now.Second, now.Month, now.Day, now.Year, -1);
		}

		/// <summary>
		/// Returns the Unix timestamp for a time compound of an hour which is specified and a minute, a second,
		/// a month, a day and a year which are taken from the current date values.
		/// </summary>
		/// <param name="hour">The hour.</param>
		/// <returns>Unix timestamp.</returns>
		[ImplementsFunction("mktime")]
		public static int MakeTime(int hour)
		{
			DateTime now = Now;
			return MakeTime(hour, now.Minute, now.Second, now.Month, now.Day, now.Year, -1);
		}

		/// <summary>
		/// Returns the Unix timestamp for a time compound of an hour and a minute which are specified and a second,
		/// a month, a day and a year which are taken from the current date values.
		/// </summary>
		/// <param name="hour">The hour.</param>
		/// <param name="minute">The minute.</param>
		/// <returns>Unix timestamp.</returns>
		[ImplementsFunction("mktime")]
		public static int MakeTime(int hour, int minute)
		{
			DateTime now = Now;
			return MakeTime(hour, minute, now.Second, now.Month, now.Day, now.Year, -1);
		}

		/// <summary>
		/// Returns the Unix timestamp for a time compound of an hour, a minute and a second which are specified and 
		/// a month, a day and a year which are taken from the current date values.
		/// </summary>
		/// <param name="hour">The hour.</param>
		/// <param name="minute">The minute.</param>
		/// <param name="second">The second.</param>
		/// <returns>Unix timestamp.</returns>
		[ImplementsFunction("mktime")]
		public static int MakeTime(int hour, int minute, int second)
		{
			DateTime now = Now;
			return MakeTime(hour, minute, second, now.Month, now.Day, now.Year, -1);
		}

		/// <summary>
		/// Returns the Unix timestamp for a time compound of an hour, a minute, a second and a month which are specified and 
		/// a day and a year which are taken from the current date values.
		/// </summary>
		/// <param name="hour">The hour.</param>
		/// <param name="minute">The minute.</param>
		/// <param name="second">The second.</param>
		/// <param name="month">The month.</param>
		/// <returns>Unix timestamp.</returns>
		[ImplementsFunction("mktime")]
		public static int MakeTime(int hour, int minute, int second, int month)
		{
			DateTime now = Now;
			return MakeTime(hour, minute, second, month, now.Day, now.Year, -1);
		}

		/// <summary>
		/// Returns the Unix timestamp for a time compound of an hour, a minute, a second, a month and a day 
		/// which are specified and a year which is taken from the current date values.
		/// </summary>
		/// <param name="hour">The hour.</param>
		/// <param name="minute">The minute.</param>
		/// <param name="second">The second.</param>
		/// <param name="month">The month.</param>
		/// <param name="day">The day.</param>
		/// <returns>Unix timestamp.</returns>
		[ImplementsFunction("mktime")]
		public static int MakeTime(int hour, int minute, int second, int month, int day)
		{
			DateTime now = Now;
			return MakeTime(hour, minute, second, month, day, now.Year, -1);
		}

		/// <summary>
		/// Returns the Unix timestamp for a time compound of an hour, a minute, a second, a month, a day and a year.
		/// </summary>
		/// <param name="hour">The hour.</param>
		/// <param name="minute">The minute.</param>
		/// <param name="second">The second.</param>
		/// <param name="month">The month.</param>
		/// <param name="day">The day.</param>
		/// <param name="year">The year.</param>
		/// <returns>Unix timestamp.</returns>
		[ImplementsFunction("mktime")]
		public static int MakeTime(int hour, int minute, int second, int month, int day, int year)
		{
			return MakeTime(hour, minute, second, month, day, year, -1);
		}

		/// <summary>
		/// Returns the Unix timestamp for a time compound of an hour, a minute, a second, a month, a day and a year.
		/// </summary>
		/// <param name="hour">The hour.</param>
		/// <param name="minute">The minute.</param>
		/// <param name="second">The second.</param>
		/// <param name="month">The month.</param>
		/// <param name="day">The day.</param>
		/// <param name="year">The year.</param>
		/// <param name="daylightSaving">Daylight savings time.</param>
		/// <returns>Unix timestamp.</returns>
		[ImplementsFunction("mktime")]
		public static int MakeTime(int hour, int minute, int second, int month, int day, int year, int daylightSaving)
		{
			var zone = PhpTimeZone.CurrentTimeZone;
			DateTime local = MakeDateTime(hour, minute, second, month, day, year);
            
			switch (daylightSaving)
			{
				case -1: // detect, whether the date is during DST:
					if (zone.IsDaylightSavingTime(local))
						local.AddHours(-1);
					break;

				case 0: // not dst
					break;

				case 1: // dst
					local.AddHours(-1);
					break;

				default:
                    PhpException.ArgumentValueNotSupported("daylightSaving", daylightSaving);
					break;
			}
            return DateTimeUtils.UtcToUnixTimeStamp(TimeZoneInfo.ConvertTimeToUtc(local, zone));
		}

		#endregion

		#region MakeDateTime

		internal static DateTime MakeDateTime(int hour, int minute, int second, int month, int day, int year)
		{
			if (year >= 0 && year < 70) year += 2000;
			else if (year >= 70 && year <= 110) year += 1900;

			// TODO (better)

			DateTime dt = new DateTime(0);
			int i = 0;

			try
			{
				// first add positive values, than negative to not throw exception
				// if there will be negative values first.
				// This works bad for upper limit, if there are big positive values that
				// exceeds DateTime.MaxValue and big negative that will substract it to
				// less value, this returns simply MaxValue - it is big enough, so it
				// should not occur in real life. Algorithm handling this correctly will
				// be much more complicated.
				for (i = 1; i <= 2; i++)
				{
					if (i == 1 && year >= 0)
						dt = dt.AddYears(year - 1);
					else if (i == 2 && year < 0)
						dt = dt.AddYears(year - 1);

					if (i == 1 && month >= 0)
						dt = dt.AddMonths(month - 1);
					else if (i == 2 && month < 0)
						dt = dt.AddMonths(month - 1);

					if (i == 1 && day >= 0)
						dt = dt.AddDays(day - 1);
					else if (i == 2 && day < 0)
						dt = dt.AddDays(day - 1);

					if (i == 1 && hour >= 0)
						dt = dt.AddHours(hour);
					else if (i == 2 && hour < 0)
						dt = dt.AddHours(hour);

					if (i == 1 && minute >= 0)
						dt = dt.AddMinutes(minute);
					else if (i == 2 && minute < 0)
						dt = dt.AddMinutes(minute);

					if (i == 1 && second >= 0)
						dt = dt.AddSeconds(second);
					else if (i == 2 && second < 0)
						dt = dt.AddSeconds(second);
				}
			}
			catch (ArgumentOutOfRangeException)
			{
				if (i == 1) // exception while adding positive values
					dt = DateTime.MaxValue;
				else // exception while substracting
					dt = DateTime.MinValue;
			}

			return dt;
		}

		#endregion

		#region checkdate

		/// <summary>
		/// Returns TRUE if the date given is valid; otherwise returns FALSE.
		/// Checks the validity of the date formed by the arguments.
		/// </summary>
		/// <remarks>
		/// A date is considered valid if:
		/// <list type="bullet">
		/// <item>year is between 1 and 32767 inclusive</item>
		/// <item>month is between 1 and 12 inclusive</item>
		/// <item>day is within the allowed number of days for the given month. Leap years are taken into consideration.</item>
		/// </list>		
		/// </remarks>
		/// <param name="month">Month</param>
		/// <param name="day">Day</param>
		/// <param name="year">Year</param>
		/// <returns>True if the date is valid, false otherwise.</returns>
		[ImplementsFunction("checkdate")]
		public static bool CheckDate(int month, int day, int year)
		{
			return month >= 1 && month <= 12
				&& year >= 1 && year <= 32767
				&& day >= 1 && day <= DateTime.DaysInMonth(year, month); // this works also with leap years
		}

		#endregion

		#region getdate

		/// <summary>
		/// Returns an associative array containing the date information of the current local time.
		/// </summary>
		/// <returns>Associative array with date information.</returns>
		[ImplementsFunction("getdate")]
		public static PhpArray GetDate()
		{
			return GetDate(DateTime.UtcNow);
		}

		/// <summary>
		/// Returns an associative array containing the date information of the timestamp.
		/// </summary>
		/// <param name="timestamp">Number of seconds since 1970.</param>
		/// <returns>Associative array with date information.</returns>
		[ImplementsFunction("getdate")]
		public static PhpArray GetDate(int timestamp)
		{
			return GetDate(DateTimeUtils.UnixTimeStampToUtc(timestamp));
		}

		/// <summary>
		/// Returns an associative array containing the date information.
		/// </summary>
		/// <param name="utc">UTC date time.</param>
		/// <returns>Associative array with date information.</returns>
		public static PhpArray GetDate(DateTime utc)
		{
			PhpArray result = new PhpArray(1, 10);

			var zone = PhpTimeZone.CurrentTimeZone;
            DateTime local = TimeZoneInfo.ConvertTimeFromUtc(utc, zone);

			result.Add("seconds", local.Second);
			result.Add("minutes", local.Minute);
			result.Add("hours", local.Hour);
			result.Add("mday", local.Day);
			result.Add("wday", (int)local.DayOfWeek);
			result.Add("mon", local.Month);
			result.Add("year", local.Year);
			result.Add("yday", local.DayOfYear - 1); // PHP: zero based day count
			result.Add("weekday", local.DayOfWeek.ToString());
			result.Add("month", local.ToString("MMMM", DateTimeFormatInfo.InvariantInfo));
			result.Add(0, DateTimeUtils.UtcToUnixTimeStamp(utc));

			return result;
		}

		#endregion

		#region gettimeofday

		/// <summary>
		/// Gets time information.
		/// </summary>
		/// <remarks>
		/// It returns <see cref="PhpArray"/> containing the following 4 entries:
		/// <list type="table">
		/// <item><term><c>"sec"</c></term><description>Unix timestamp (seconds since the Unix Epoch)</description></item>
		/// <item><term><c>"usec"</c></term><description>microseconds</description></item>
		/// <item><term><c>"minuteswest"</c></term><description>minutes west of Greenwich (doesn't take daylight savings time in consideration)</description></item>
		/// <item><term><c>"dsttime"</c></term><description>type of DST correction (+1 or 0, determined only by the current time zone not by the time)</description></item>
		/// </list>
		/// </remarks>
		/// <returns>Associative array</returns>
		[ImplementsFunction("gettimeofday")]
		public static PhpArray GetTimeOfDay()
		{
			return GetTimeOfDay(DateTime.UtcNow, PhpTimeZone.CurrentTimeZone);
		}

		[ImplementsFunction("gettimeofday")]
		public static object GetTimeOfDay(bool returnDouble)
		{
			if (returnDouble)
			{
				return (Now - DateTimeUtils.UtcStartOfUnixEpoch).TotalSeconds;
			}
			else
			{
				return GetTimeOfDay();
			}
		}

		private static PhpArray GetTimeOfDay(DateTime utc, TimeZoneInfo/*!*/ zone)
		{
			PhpArray result = new PhpArray(0, 4);

            DateTime local = TimeZoneInfo.ConvertTimeFromUtc(utc, zone);
            
			int current_dst = 0;
            if (zone.IsDaylightSavingTime(local))
            {
                var rules = zone.GetAdjustmentRules();
                for (int i = 0; i < rules.Length; i++)
                {
                    if (rules[i].DateStart <= local && rules[i].DateEnd >= local)
                    {
                        current_dst = (int)rules[i].DaylightDelta.TotalHours;
                        break;
                    }
                }
            }			

			const int ticks_per_microsecond = (int)TimeSpan.TicksPerMillisecond / 1000;

			result["sec"] = DateTimeUtils.UtcToUnixTimeStamp(utc);
			result["usec"] = (int)(local.Ticks % TimeSpan.TicksPerSecond) / ticks_per_microsecond;
			result["minuteswest"] = (int)(utc - local).TotalMinutes;
            result["dsttime"] = current_dst;

			return result;
		}

#if DEBUG

		[Test]
		private static void TestGetTimeOfDay()
		{
			PhpArray result;

			result = GetTimeOfDay(new DateTime(2005, 10, 1), PhpTimeZone.PacificTimeZone);
			Debug.Assert((int)result["minuteswest"] == 480);
			Debug.Assert((int)result["dsttime"] == 1);

			result = GetTimeOfDay(new DateTime(2005, 11, 1), PhpTimeZone.PacificTimeZone);
			Debug.Assert((int)result["minuteswest"] == 480);
			Debug.Assert((int)result["dsttime"] == 1);

			result = GetTimeOfDay(new DateTime(2005, 11, 1), PhpTimeZone.NepalTimeZone);
			Debug.Assert((int)result["minuteswest"] == -345);
			Debug.Assert((int)result["dsttime"] == 0);

            result = GetTimeOfDay(new DateTime(2005, 10, 1), PhpTimeZone.GmtTimeZone);
			Debug.Assert((int)result["minuteswest"] == 0);
			Debug.Assert((int)result["dsttime"] == 1);

            result = GetTimeOfDay(new DateTime(2005, 11, 1), PhpTimeZone.GmtTimeZone);
			Debug.Assert((int)result["minuteswest"] == 0);
			Debug.Assert((int)result["dsttime"] == 1);

			result = GetTimeOfDay(new DateTime(2005, 11, 1), DateTimeUtils.UtcTimeZone);
			Debug.Assert((int)result["minuteswest"] == 0);
			Debug.Assert((int)result["dsttime"] == 0);
		}

#endif

		#endregion

		#region localtime

		/// <summary>
		/// The localtime() function returns an array identical to that of the structure returned by the C function call.
		/// Current time is used, regular numericaly indexed array is returned.
		/// </summary>
		/// <returns>Array containing values specifying the date and time.</returns>
		[ImplementsFunction("localtime")]
		public static PhpArray GetLocalTime()
		{
			return GetLocalTime(DateTime.UtcNow, false);
		}

		/// <summary>
		/// The localtime() function returns an array identical to that of the structure returned by the C function call.
		/// Time specified by the parameter timestamp is used, regular numericaly indexed array is returned.
		/// </summary>
		/// <param name="timestamp">Number of seconds since 1970.</param>
		/// <returns>Array containing values specifying the date and time.</returns>
		[ImplementsFunction("localtime")]
		public static PhpArray GetLocalTime(int timestamp)
		{
			return GetLocalTime(DateTimeUtils.UnixTimeStampToUtc(timestamp), false);
		}

		/// <summary>
		/// The localtime() function returns an array identical to that of the structure returned by the C function call.
		/// The first argument to localtime() is the timestamp. The second argument to the localtime() is
		/// the isAssociative, if this is set to <c>false</c> than the array is returned as a regular, numerically indexed array.
		/// If the argument is set to <c>true</c> then localtime() is an associative array containing all the different
		/// elements of the structure returned by the C function call to localtime. 
		/// </summary>
		/// <remarks>
		/// Returned array contains these elements if isAssociative is set to true:
		/// <list type="bullet">
		/// <term><c>"tm_sec"</c></term><description>seconds</description> 
		/// <term><c>"tm_min"</c></term><description>minutes</description> 
		/// <term><c>"tm_hour"</c></term><description>hour</description>
		///	<term><c>"tm_mday"</c></term><description>day of the month</description>
		///	<term><c>"tm_mon"</c></term><description>month of the year, starting with 0 for January</description>
		/// <term><c>"tm_year"</c></term><description>Years since 1900</description>
		/// <term><c>"tm_wday"</c></term><description>Day of the week</description>
		/// <term><c>"tm_yday"</c></term><description>Day of the year</description>
		/// <term><c>"tm_isdst"</c></term><description>Is daylight savings time in effect</description>
		/// </list>
		/// </remarks>
		/// <param name="timestamp"></param>
		/// <param name="returnAssociative"></param>
		/// <returns></returns>
		[ImplementsFunction("localtime")]
		public static PhpArray GetLocalTime(int timestamp, bool returnAssociative)
		{
			return GetLocalTime(DateTimeUtils.UnixTimeStampToUtc(timestamp), returnAssociative);
		}

		private static PhpArray GetLocalTime(DateTime utc, bool returnAssociative)
		{
			PhpArray result;

            DateTime local = TimeZoneInfo.ConvertTimeFromUtc(utc, PhpTimeZone.CurrentTimeZone);

			if (returnAssociative)
			{
				result = new PhpArray(0, 9);
				result["tm_sec"] = local.Second;
				result["tm_min"] = local.Minute;
				result["tm_hour"] = local.Hour;
				result["tm_mday"] = local.Day;
				result["tm_mon"] = local.Month - 1;
				result["tm_year"] = local.Year - 1900;
				result["tm_wday"] = (int)local.DayOfWeek;
				result["tm_yday"] = local.DayOfYear - 1;
				result["tm_isdst"] = PhpTimeZone.CurrentTimeZone.IsDaylightSavingTime(local) ? 1 : 0;
			}
			else
			{
				result = new PhpArray(9, 0);
				result[0] = local.Second;
				result[1] = local.Minute;
				result[2] = local.Hour;
				result[3] = local.Day;
				result[4] = local.Month - 1;
				result[5] = local.Year - 1900;
				result[6] = (int)local.DayOfWeek;
				result[7] = local.DayOfYear - 1;
				result[8] = PhpTimeZone.CurrentTimeZone.IsDaylightSavingTime(local) ? 1 : 0;
			}

			return result;
		}

#if DEBUG

		[Test]
		private static void TestGetLocalTime()
		{
			PhpArray result1, result2;

			PhpTimeZone.CurrentTimeZone = PhpTimeZone.GetTimeZone("UTC");
			DateTime dt = new DateTime(2005, 11, 4, 5, 4, 3, 132);

			result1 = GetLocalTime(dt, false);
			result2 = GetLocalTime(dt, true);
			Debug.Assert((int)result1[0] == 3);
			Debug.Assert((int)result1[1] == 4);
			Debug.Assert((int)result1[2] == 5);
			Debug.Assert((int)result1[3] == 4);
			Debug.Assert((int)result1[4] == 10);
			Debug.Assert((int)result1[5] == 105);
			Debug.Assert((int)result1[6] == 5);
			Debug.Assert((int)result1[7] == 307);
			Debug.Assert((int)result1[8] == 0);

			Debug.Assert((int)result1[0] == (int)result2["tm_sec"]);
			Debug.Assert((int)result1[1] == (int)result2["tm_min"]);
			Debug.Assert((int)result1[2] == (int)result2["tm_hour"]);
			Debug.Assert((int)result1[3] == (int)result2["tm_mday"]);
			Debug.Assert((int)result1[4] == (int)result2["tm_mon"]);
			Debug.Assert((int)result1[5] == (int)result2["tm_year"]);
			Debug.Assert((int)result1[6] == (int)result2["tm_wday"]);
			Debug.Assert((int)result1[7] == (int)result2["tm_yday"]);
			Debug.Assert((int)result1[8] == (int)result2["tm_isdst"]);
		}

#endif

		#endregion

		#region microtime

		/// <summary>
		/// Returns the string "msec sec" where sec is the current time measured in the number of seconds
		/// since the Unix Epoch (0:00:00 January 1, 1970 GMT), and msec is the microseconds part.
		/// </summary>
		/// <returns>String containing number of miliseconds, space and number of seconds.</returns>
		[ImplementsFunction("microtime")]
		public static string MicroTime()
		{
			// time from 1970
			TimeSpan fromUnixEpoch = DateTime.UtcNow - DateTimeUtils.UtcStartOfUnixEpoch;

			// seconds part to return
			long seconds = (long)fromUnixEpoch.TotalSeconds;

			// only remaining time less than one second
			TimeSpan mSec = fromUnixEpoch.Subtract(new TimeSpan(seconds * 10000000)); // convert seconds to 100 ns
			double remaining = ((double)mSec.Ticks) / 10000000; // convert from 100ns to seconds

			return String.Format("{0} {1}", remaining, seconds);
		}

		/// <summary>
		/// Returns the fractional time in seconds from the start of the UNIX epoch.
		/// </summary>
		/// <param name="returnDouble"><c>true</c> to return the double, <c>false</c> to return string.</param>
		/// <returns><see cref="String"/> containing number of miliseconds, space and number of seconds
		/// if <paramref name="returnDouble"/> is <c>false</c> and <see cref="double"/> 
		/// containing the fractional count of seconds otherwise.</returns>
		[ImplementsFunction("microtime")]
		public static object MicroTime(bool returnDouble)
		{
			if (returnDouble)
				return (DateTime.UtcNow - DateTimeUtils.UtcStartOfUnixEpoch).TotalSeconds;
			else
				return MicroTime();
		}

		#endregion

		#region strtotime

		/// <summary>
		/// Parses a string containing an English date format into a UNIX timestamp relative to the current time.
		/// </summary>
		/// <param name="time">String containing time definition</param>
		/// <returns>Number of seconds since 1/1/1970 or -1 on failure.</returns>
		[ImplementsFunction("strtotime")]
		public static object StringToTime(string time)
		{
			return StringToTime(time, DateTime.UtcNow);
		}

		/// <summary>
		/// Parses a string containing an English date format into a UNIX timestamp relative to a specified time.
		/// </summary>
		/// <param name="time">String containing time definition.</param>
		/// <param name="start">Timestamp (seconds from 1970) to which is the new timestamp counted.</param>
		/// <returns>Number of seconds since 1/1/1970 or -1 on failure.</returns>
		[ImplementsFunction("strtotime")]
		public static object StringToTime(string time, int start)
		{
			return StringToTime(time, DateTimeUtils.UnixTimeStampToUtc(start));
		}

		/// <summary>
		/// Implementation of <see cref="StringToTime(string,int)"/> function.
		/// </summary>
		private static object StringToTime(string time, DateTime startUtc)
		{
			if (time == null) return false;
			time = time.Trim();
			if (time == "") return false;

			string error = null;
			int result = StrToTime.DateInfo.Parse(time, startUtc, out error);

			if (error != null)
			{
				PhpException.Throw(PhpError.Warning, error);
				return false;
			}

			return result;
		}

		#endregion

		#region time

		/// <summary>
		/// Returns the current time measured in the number of seconds since the Unix Epoch (January 1 1970 00:00:00 GMT).
		/// </summary>
		/// <returns>Number of seconds since 1970.</returns>
		[ImplementsFunction("time")]
		public static int Time()
		{
			return DateTimeUtils.UtcToUnixTimeStamp(DateTime.UtcNow);
		}

		#endregion

		#region date_sunrise, date_sunset

		public enum TimeFormats
		{
			[ImplementsConstant("SUNFUNCS_RET_TIMESTAMP")]
			Integer = 0,
			[ImplementsConstant("SUNFUNCS_RET_STRING")]
			String = 1,
			[ImplementsConstant("SUNFUNCS_RET_DOUBLE")]
			Double = 2
		}

		[ImplementsFunction("date_sunrise")]
		public static object GetSunriseTime(int timestamp)
		{
			return GetSunTime(timestamp, TimeFormats.String, Double.NaN, Double.NaN, Double.NaN, Double.NaN, true);
		}

		[ImplementsFunction("date_sunrise")]
		public static object GetSunriseTime(int timestamp, TimeFormats format)
		{
			return GetSunTime(timestamp, format, Double.NaN, Double.NaN, Double.NaN, Double.NaN, true);
		}

		[ImplementsFunction("date_sunrise")]
		public static object GetSunriseTime(int timestamp, TimeFormats format, double latitude)
		{
			return GetSunTime(timestamp, format, latitude, Double.NaN, Double.NaN, Double.NaN, true);
		}

		[ImplementsFunction("date_sunrise")]
		public static object GetSunriseTime(int timestamp, TimeFormats format, double latitude, double longitude)
		{
			return GetSunTime(timestamp, format, latitude, longitude, Double.NaN, Double.NaN, true);
		}

		[ImplementsFunction("date_sunrise")]
		public static object GetSunriseTime(int timestamp, TimeFormats format, double latitude, double longitude, double zenith)
		{
			return GetSunTime(timestamp, format, latitude, longitude, zenith, Double.NaN, true);
		}

		[ImplementsFunction("date_sunrise")]
		public static object GetSunriseTime(int timestamp, TimeFormats format, double latitude, double longitude, double zenith, double offset)
		{
			return GetSunTime(timestamp, format, latitude, longitude, zenith, offset, true);
		}

		[ImplementsFunction("date_sunset")]
		public static object GetSunsetTime(int timestamp)
		{
			return GetSunTime(timestamp, TimeFormats.String, Double.NaN, Double.NaN, Double.NaN, Double.NaN, false);
		}

		[ImplementsFunction("date_sunset")]
		public static object GetSunsetTime(int timestamp, TimeFormats format)
		{
			return GetSunTime(timestamp, format, Double.NaN, Double.NaN, Double.NaN, Double.NaN, false);
		}

		[ImplementsFunction("date_sunset")]
		public static object GetSunsetTime(int timestamp, TimeFormats format, double latitude)
		{
			return GetSunTime(timestamp, format, latitude, Double.NaN, Double.NaN, Double.NaN, false);
		}

		[ImplementsFunction("date_sunset")]
		public static object GetSunsetTime(int timestamp, TimeFormats format, double latitude, double longitude)
		{
			return GetSunTime(timestamp, format, latitude, longitude, Double.NaN, Double.NaN, false);
		}

		[ImplementsFunction("date_sunset")]
		public static object GetSunsetTime(int timestamp, TimeFormats format, double latitude, double longitude, double zenith)
		{
			return GetSunTime(timestamp, format, latitude, longitude, zenith, Double.NaN, false);
		}

		[ImplementsFunction("date_sunset")]
		public static object GetSunsetTime(int timestamp, TimeFormats format, double latitude, double longitude, double zenith, double offset)
		{
			return GetSunTime(timestamp, format, latitude, longitude, zenith, offset, false);
		}


		public static object GetSunTime(int timestamp, TimeFormats format, double latitude, double longitude, double zenith, double offset, bool getSunrise)
		{
			var zone = PhpTimeZone.CurrentTimeZone;
			DateTime utc = DateTimeUtils.UnixTimeStampToUtc(timestamp);
            DateTime local = TimeZoneInfo.ConvertTimeFromUtc(utc, zone);

			if (Double.IsNaN(latitude) || Double.IsNaN(longitude) || Double.IsNaN(zenith))
			{
				LibraryConfiguration config = LibraryConfiguration.GetLocal(ScriptContext.CurrentContext);

				if (Double.IsNaN(latitude))
					latitude = config.Date.Latitude;
				if (Double.IsNaN(longitude))
					longitude = config.Date.Longitude;
				if (Double.IsNaN(zenith))
					zenith = (getSunrise) ? config.Date.SunriseZenith : config.Date.SunsetZenith;
			}

			if (Double.IsNaN(offset))
				offset = zone.GetUtcOffset(local).TotalHours;

			double result_utc = CalculateSunTime(local.DayOfYear, latitude, longitude, zenith, getSunrise);
			double result = result_utc + offset;

			switch (format)
			{
				case TimeFormats.Integer:
					return (timestamp - (timestamp % (24 * 3600))) + (int)(3600 * result);

				case TimeFormats.String:
					return String.Format("{0:00}:{1:00}", (int)result, (int)(60 * (result - (double)(int)result)));

				case TimeFormats.Double:
					return result;

				default:
					PhpException.InvalidArgument("format");
					return null;
			}
		}

		private static double ToRadians(double degrees) { return degrees * Math.PI / 180; }
		private static double ToDegrees(double radians) { return radians * 180 / Math.PI; }

		/// <summary>
		/// Calculates sunrise or sunset. Adopted PHP implementation by Moshe Doron (mosdoron@netvision.net.il).
		/// Returns UTC time.
		/// </summary>
		private static double CalculateSunTime(int day, double latitude, double longitude, double zenith, bool getSunrise)
		{
			double lngHour, t, M, L, Lx, RA, RAx, Lquadrant, RAquadrant, sinDec, cosDec, cosH, H, T, UT, UTx;

			// convert the longitude to hour value and calculate an approximate time
			lngHour = longitude / 15;

			if (getSunrise)
				t = (double)day + ((6 - lngHour) / 24);
			else
				t = (double)day + ((18 - lngHour) / 24);

			// calculate the sun's mean anomaly:
			M = (0.9856 * t) - 3.289;

			// step 4: calculate the sun's true longitude:
			L = M + (1.916 * Math.Sin(ToRadians(M))) + (0.020 * Math.Sin(ToRadians(2 * M))) + 282.634;

			while (L < 0)
			{
				Lx = L + 360;
				Debug.Assert(Lx != L);
				L = Lx;
			}

			while (L >= 360)
			{
				Lx = L - 360;
				Debug.Assert(Lx != L);
				L = Lx;
			}

			// calculate the sun's right ascension:
			RA = ToDegrees(Math.Atan(0.91764 * Math.Tan(ToRadians(L))));

			while (RA < 0)
			{
				RAx = RA + 360;
				Debug.Assert(RAx != RA);
				RA = RAx;
			}

			while (RA >= 360)
			{
				RAx = RA - 360;
				Debug.Assert(RAx != RA);
				RA = RAx;
			}

			// right ascension value needs to be in the same quadrant as L:
			Lquadrant = Math.Floor(L / 90) * 90;
			RAquadrant = Math.Floor(RA / 90) * 90;
			RA = RA + (Lquadrant - RAquadrant);

			// right ascension value needs to be converted into hours:
			RA /= 15;

			// calculate the sun's declination:
			sinDec = 0.39782 * Math.Sin(ToRadians(L));
			cosDec = Math.Cos(Math.Asin(sinDec));

			// calculate the sun's local hour angle:
			cosH = (Math.Cos(ToRadians(zenith)) - (sinDec * Math.Sin(ToRadians(latitude)))) / (cosDec * Math.Cos(ToRadians(latitude)));

			// finish calculating H and convert into hours:
			if (getSunrise)
				H = 360 - ToDegrees(Math.Acos(cosH));
			else
				H = ToDegrees(Math.Acos(cosH));

			H = H / 15;

			// calculate local mean time:
			T = H + RA - (0.06571 * t) - 6.622;

			// convert to UTC:
			UT = T - lngHour;

			while (UT < 0)
			{
				UTx = UT + 24;
				Debug.Assert(UTx != UT);
				UT = UTx;
			}

			while (UT >= 24)
			{
				UTx = UT - 24;
				Debug.Assert(UTx != UT);
				UT = UTx;
			}

			return UT;
		}

		#endregion

		#region Unit Tests

#if DEBUG

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

		[Test]
		static void TestStringToTime()
		{
			TimeZoneInfo[] all_zones = 
		  {
		    PhpTimeZone.NepalTimeZone,
		    PhpTimeZone.PacificTimeZone,
		    PhpTimeZone.GmtTimeZone
		  };

			var utc_zone = DateTimeUtils.UtcTimeZone;
            var nep_zone = PhpTimeZone.NepalTimeZone;
            var pac_zone = PhpTimeZone.PacificTimeZone;

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

					object timestamp = StringToTime(c.String, c.StartTime);

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

#endif
		#endregion
    }
}
