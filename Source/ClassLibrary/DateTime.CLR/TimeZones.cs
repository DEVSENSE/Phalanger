/*

 Copyright (c) 2005-2006 Tomas Matousek.  

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Globalization;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Win32;
using System.Xml;

using System.ComponentModel;
using System.Runtime.Serialization;
using System.Runtime.InteropServices;

using PHP.Core;
using PHP.Core.Reflection;

#if SILVERLIGHT
using PHP.CoreCLR;
#endif

namespace PHP.Library
{
    #region DateTimeZone

    /// <summary>
    /// Representation of time zone.
    /// </summary>
#if !SILVERLIGHT
    [Serializable]
#endif
    [ImplementsType]
    public class DateTimeZone : PhpObject
    {
        internal TimeZoneInfo timezone;

        #region Construction

        public DateTimeZone(ScriptContext/*!*/context, TimeZoneInfo/*!*/resolvedTimeZone)
            : this(context, true)
        {
            Debug.Assert(context != null);
            Debug.Assert(resolvedTimeZone != null);

            this.timezone = resolvedTimeZone;
        }

        /// <summary>
        /// For internal purposes only.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public DateTimeZone(ScriptContext context, bool newInstance)
            : base(context, newInstance)
        { }

        /// <summary>
        /// For internal purposes only.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public DateTimeZone(ScriptContext context, DTypeDesc caller)
            : base(context, caller)
        { }

#if !SILVERLIGHT
        /// <summary>Deserializing constructor.</summary>
        protected DateTimeZone(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }
#endif

        #endregion

        #region Methods

        // public __construct ( string $timezone )
        [ImplementsMethod]
        public object __construct(ScriptContext/*!*/context, object timezone_name)
        {
            if (timezone_name != null)
            {
                var zoneName = PHP.Core.Convert.ObjectToString(timezone_name);
                this.timezone = PhpTimeZone.GetTimeZone(zoneName);

                if (this.timezone == null)
                    PhpException.Throw(PhpError.Notice, LibResources.GetString("unknown_timezone", zoneName));
            }
            else
            {
                this.timezone = PhpTimeZone.CurrentTimeZone;
            }

            return null;
        }
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object __construct(object instance, PhpStack stack)
        {
            var arg1 = stack.PeekValue(1);
            stack.RemoveFrame();
            return ((DateTimeZone)instance).__construct(stack.Context, arg1);
        }

        //public array getLocation ( void )
        [ImplementsMethod]
        public object getLocation(ScriptContext/*!*/context)
        {
            throw new NotImplementedException();
        }
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object getLocation(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((DateTimeZone)instance).getLocation(stack.Context);
        }

        //public string getName ( void )
        [ImplementsMethod]
        public object getName(ScriptContext/*!*/context)
        {
            return (timezone != null) ? timezone.Id : null;
        }
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object getName(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((DateTimeZone)instance).getName(stack.Context);
        }

        //public int getOffset ( DateTime $datetime )
        [ImplementsMethod]
        public object getOffset(ScriptContext/*!*/context, object datetime)
        {
            if (this.timezone == null)
                return false;

            if (datetime == null)
            {
                PhpException.ArgumentNull("datetime");
                return false;
            }

            var dt = datetime as __PHP__DateTime;
            if (dt == null)
            {
                PhpException.InvalidArgumentType("datetime", "DateTime");
                return false;
            }

            return (int)this.timezone.BaseUtcOffset.TotalSeconds + (this.timezone.IsDaylightSavingTime(dt.Time) ? 3600 : 0);
        }
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object getOffset(object instance, PhpStack stack)
        {
            var arg1 = stack.PeekValue(1);
            stack.RemoveFrame();
            return ((DateTimeZone)instance).getOffset(stack.Context, arg1);
        }

        //public array getTransitions ([ int $timestamp_begin [, int $timestamp_end ]] )
        [ImplementsMethod]
        public object getTransitions(ScriptContext/*!*/context, [Optional]object timestamp_begin, [Optional]object timestamp_end)
        {
            // TODO: timestamp_begin, timestamp_end

            var rules = this.timezone.GetAdjustmentRules();
            var array = new PhpArray(rules.Length);

            //var now = DateTime.UtcNow;
            for (int i = 0; i < rules.Length; i++)
            {
                var rule = rules[i];

                // TODO: timezone transitions
                //if (rule.DateStart > now || rule.DateEnd < now) continue;
                //var transition = new PhpArray(5);
                //transition["ts"] = (int)(new DateTime(now.Year, rule.DaylightTransitionStart.Month, rule.DaylightTransitionStart.Day) - DateTimeUtils.UtcStartOfUnixEpoch).TotalSeconds;
                ////transition["time"] = ;
                ////transition["offset"] = ;
                //transition["isdst"] = 1;
                ////transition["abbr"] = ;

                //array.Add(transition);
            }

            return array;
        }
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object getTransitions(object instance, PhpStack stack)
        {
            var arg1 = stack.PeekValueOptional(1);
            var arg2 = stack.PeekValueOptional(2);
            stack.RemoveFrame();
            return ((DateTimeZone)instance).getTransitions(stack.Context, arg1, arg2);
        }

        //public static array listAbbreviations ( void )
        [ImplementsMethod]
        public object listAbbreviations(ScriptContext/*!*/context)
        {
            throw new NotImplementedException();
        }
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object listAbbreviations(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((DateTimeZone)instance).listAbbreviations(stack.Context);
        }

        //public static array listIdentifiers ([ int $what = DateTimeZone::ALL [, string $country = NULL ]] )
        [ImplementsMethod]
        public static object listIdentifiers(ScriptContext/*!*/context, [Optional]object what, [Optional]object country)
        {
            throw new NotImplementedException();
        }
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object listIdentifiers(object instance, PhpStack stack)
        {
            var arg1 = stack.PeekValueOptional(1);
            var arg2 = stack.PeekValueOptional(2);
            stack.RemoveFrame();
            return listIdentifiers(stack.Context, arg1, arg2);
        }

        #endregion
    }

    #endregion

	/// <summary>
	/// Provides timezone information for PHP functions.
	/// </summary>
    [ImplementsExtension(LibraryDescriptor.ExtDate)]
    public static class PhpTimeZone
    {
        private const string EnvVariableName = "TZ";

        private struct TimeZoneInfoItem
        {
            /// <summary>
            /// Comparer of <see cref="TimeZoneInfoItem"/>, comparing its <see cref="TimeZoneInfoItem.PhpName"/>.
            /// </summary>
            public class Comparer : IComparer<TimeZoneInfoItem>
            {
                public int Compare(TimeZoneInfoItem x, TimeZoneInfoItem y)
                {
                    return StringComparer.OrdinalIgnoreCase.Compare(x.PhpName, y.PhpName);
                }
            }

            /// <summary>
            /// PHP time zone name.
            /// </summary>
            public readonly string PhpName;

            /// <summary>
            /// Actual <see cref="TimeZoneInfo"/> from .NET.
            /// </summary>
            public readonly TimeZoneInfo Info;

            /// <summary>
            /// An abbrevation, not supported.
            /// </summary>
            public readonly string Abbrevation;

            /// <summary>
            /// Not listed item used only as an alias for another time zone.
            /// </summary>
            public readonly bool IsAlias;

            internal TimeZoneInfoItem(string/*!*/phpName, TimeZoneInfo/*!*/info, string abbrevation, bool isAlias)
            {
                // alter the ID with php-like name
                if (!phpName.Equals(info.Id, StringComparison.OrdinalIgnoreCase))
                    info = TimeZoneInfo.CreateCustomTimeZone(phpName, info.BaseUtcOffset, info.DisplayName, info.StandardName, info.DaylightName, info.GetAdjustmentRules());

                //
                this.PhpName = phpName;
                this.Info = info;
                this.Abbrevation = abbrevation;
                this.IsAlias = isAlias;
            }
        }

        /// <summary>
        /// Registers <see cref="Clear"/> called on request end.
        /// </summary>
        static PhpTimeZone()
        {
            RequestContext.RequestEnd += new Action(Clear);

            // initialize tz database (from system time zone database)
            timezones = InitializeTimeZones();
        }

        #region timezones

        /// <summary>
        /// PHP time zone database.
        /// </summary>
        private readonly static TimeZoneInfoItem[]/*!!*/timezones;

        private static TimeZoneInfoItem[]/*!!*/InitializeTimeZones()
        {
            // read list of initial timezones
            var sortedTZ = new SortedSet<TimeZoneInfoItem>(
                EnvironmentUtils.IsWindows ? InitialTimeZones_Windows() : InitialTimeZones_Mono(),
                new TimeZoneInfoItem.Comparer());

            // add additional time zones:
            sortedTZ.Add(new TimeZoneInfoItem("UTC", TimeZoneInfo.Utc, null, false));
            sortedTZ.Add(new TimeZoneInfoItem("Etc/UTC", TimeZoneInfo.Utc, null, true));
            sortedTZ.Add(new TimeZoneInfoItem("Etc/GMT-0", TimeZoneInfo.Utc, null, true));
            sortedTZ.Add(new TimeZoneInfoItem("GMT", TimeZoneInfo.Utc, null, true));
            sortedTZ.Add(new TimeZoneInfoItem("GMT0", TimeZoneInfo.Utc, null, true));
            sortedTZ.Add(new TimeZoneInfoItem("UCT", TimeZoneInfo.Utc, null, true));
            sortedTZ.Add(new TimeZoneInfoItem("Universal", TimeZoneInfo.Utc, null, true));
            sortedTZ.Add(new TimeZoneInfoItem("Zulu", TimeZoneInfo.Utc, null, true));
            sortedTZ.Add(new TimeZoneInfoItem("MET", sortedTZ.First(t => t.PhpName == "Europe/Rome").Info, null, true));
            sortedTZ.Add(new TimeZoneInfoItem("WET", sortedTZ.First(t => t.PhpName == "Europe/Berlin").Info, null, true));     
            //{ "PRC"              
            //{ "ROC"              
            //{ "ROK"   
            // W-SU = 
            //{ "Poland"           
            //{ "Portugal"         
            //{ "PRC"              
            //{ "ROC"              
            //{ "ROK"              
            //{ "Singapore"      = Asia/Singapore  
            //{ "Turkey"  

            //
            return sortedTZ.ToArray();
        }

        private static IEnumerable<TimeZoneInfoItem>/*!!*/InitialTimeZones_Windows()
        {
            Debug.Assert(EnvironmentUtils.IsWindows);

            // time zone cache:
            var tzcache = new Dictionary<string, TimeZoneInfo>(128, StringComparer.OrdinalIgnoreCase);
            Func<string, TimeZoneInfo> cachelookup = (id) =>
            {
                TimeZoneInfo tz;
                if (!tzcache.TryGetValue(id, out tz))
                {
                    TimeZoneInfo winTZ = null;
                    try
                    {
                        winTZ = TimeZoneInfo.FindSystemTimeZoneById(id);
                    }
                    catch { }

                    tzcache[id] = tz = winTZ;   // null in case "id" is not defined in Windows registry (probably missing Windows Update)
                }

                return tz;
            };

            // collect php time zone names and match them with Windows TZ IDs:
            var tzdoc = new XmlDocument();
            tzdoc.LoadXml(Strings.WindowsTZ);
            foreach (XmlNode tz in tzdoc.DocumentElement.SelectNodes(@"//windowsZones/mapTimezones/mapZone"))
            {
                // <mapZone other="Dateline Standard Time" type="Etc/GMT+12"/>
                // @other = Windows TZ ID
                // @type = PHP TZ names, separated by space

                var windowsId = tz.Attributes["other"].Value;
                var phpIds = tz.Attributes["type"].Value;

                var windowsTZ = cachelookup(windowsId);
                if (windowsTZ != null)  // TZ not defined in Windows registry, ignore such time zone // TODO: show a warning
                    foreach (var phpTzName in phpIds.Split(' '))
                    {
                        Debug.Assert(!string.IsNullOrWhiteSpace(phpTzName));

                        bool isAlias = !phpTzName.Contains('/') || phpTzName.Contains("GMT");   // whether to display such tz within timezone_identifiers_list()
                        yield return new TimeZoneInfoItem(phpTzName, windowsTZ, null, isAlias);
                    }
            }

            //
            //{ "US/Alaska"        
            //{ "US/Aleutian"      
            //{ "US/Arizona"       
            yield return new TimeZoneInfoItem("US/Central", cachelookup("Central Standard Time"), null, true);
            //{ "US/East-Indiana"  
            //{ "US/Eastern"       
            yield return new TimeZoneInfoItem("US/Hawaii", cachelookup("Hawaiian Standard Time"), null, true);
            //{ "US/Indiana-Starke"
            //{ "US/Michigan"      
            //{ "US/Mountain"      
            //{ "US/Pacific"       
            //{ "US/Pacific-New"   
            //{ "US/Samoa"   
        }

        private static IEnumerable<TimeZoneInfoItem>/*!!*/InitialTimeZones_Mono()
        {
            Debug.Assert(!EnvironmentUtils.IsDotNetFramework);

            var tzns = TimeZoneInfo.GetSystemTimeZones();
            if (tzns == null)
                yield break;

            foreach (var x in tzns)
            {
                bool isAlias = !x.Id.Contains('/') || x.Id.Contains("GMT");   // whether to display such tz within timezone_identifiers_list()                    
                yield return new TimeZoneInfoItem(x.Id, x, null, isAlias);
            }
        }

        #endregion

        /// <summary>
        /// Gets the current time zone for PHP date-time library functions. Associated with the current thread.
        /// </summary>
        /// <remarks>It returns the time zone set by date_default_timezone_set PHP function.
        /// If no time zone was set, the time zone is determined in following order:
        /// 1. the time zone set in configuration
        /// 2. the time zone of the current system
        /// 3. default UTC time zone</remarks>
        public static TimeZoneInfo CurrentTimeZone
        {
            get
            {
                // timezone is set by date_default_timezone_set(), return this one
                if (_default != null)
                    return _default;

                // default timezone was not set, use & cache the current timezone
                return (_current ?? (_current = new CurrentTimeZoneCache())).TimeZone;
            }
#if DEBUG   // for unit tests only
            internal set
            {
                _current = new CurrentTimeZoneCache(value);
            }
#endif
        }

        /// <summary>
        /// Time zone set as current. <B>null</B> initially.
        /// </summary>
#if !SILVERLIGHT
        [ThreadStatic]
#endif
        private static TimeZoneInfo _default;

        /// <summary>
        /// Time zone set as current. <B>null</B> initially.
        /// </summary>
#if !SILVERLIGHT
        [ThreadStatic]
#endif
        private static CurrentTimeZoneCache _current;

        #region CurrentTimeZoneCache

        /// <summary>
        /// Cache of current TimeZone with auto-update ability.
        /// </summary>
        private class CurrentTimeZoneCache
        {
            public CurrentTimeZoneCache()
            {
            }
#if DEBUG
            internal CurrentTimeZoneCache(TimeZoneInfo timezone)
            {
                this._timeZone = timezone;
                this._changedFunc = (_) => false;
            }
#endif

            /// <summary>
            /// Get the TimeZone set by the current process. Depends on environment variable, or local configuration, or system time zone.
            /// </summary>
            public TimeZoneInfo TimeZone
            {
                get
                {
                    if (_timeZone == null || _changedFunc == null || _changedFunc(_timeZone) == true)
                        _timeZone = DetermineTimeZone(out _changedFunc);    // get the current timezone, update the function that determines, if the timezone has to be rechecked.

                    return _timeZone;
                }
            }

            private TimeZoneInfo _timeZone;

            /// <summary>
            /// Function that determines if the current timezone should be rechecked.
            /// </summary>
            private Func<TimeZoneInfo/*!*/, bool> _changedFunc;

            /// <summary>
            /// Finds out the time zone in the way how PHP does.
            /// </summary>
            private static TimeZoneInfo DetermineTimeZone(out Func<TimeZoneInfo, bool> changedFunc)
            {
                TimeZoneInfo result;

                // check environment variable:
#if !SILVERLIGHT
                string env_tz = Environment.GetEnvironmentVariable(EnvVariableName);
                if (!String.IsNullOrEmpty(env_tz))
                {
                    result = GetTimeZone(env_tz);
                    if (result != null)
                    {
                        // recheck the timezone only if the environment variable changes
                        changedFunc = (timezone) => !String.Equals(timezone.StandardName, Environment.GetEnvironmentVariable(EnvVariableName), StringComparison.OrdinalIgnoreCase);
                        // return the timezone set in environment
                        return result;
                    }

                    PhpException.Throw(PhpError.Notice, LibResources.GetString("unknown_timezone_env", env_tz));
                }
#endif

                // check configuration:
                LibraryConfiguration config = LibraryConfiguration.Local;
                if (config.Date.TimeZone != null)
                {
                    // recheck the timezone only if the local configuration changes, ignore the environment variable from this point at all
                    changedFunc = (timezone) => LibraryConfiguration.Local.Date.TimeZone != timezone;
                    return config.Date.TimeZone;
                }

                // convert current system time zone to PHP zone:
                result = SystemToPhpTimeZone(TimeZoneInfo.Local);
                
                // UTC:
                if (result == null)
                    result = DateTimeUtils.UtcTimeZone;// GetTimeZone("UTC");

                PhpException.Throw(PhpError.Strict, LibResources.GetString("using_implicit_timezone", result.Id));

                // recheck the timezone when the TimeZone in local configuration is set
                changedFunc = (timezone) => LibraryConfiguration.Local.Date.TimeZone != null;
                return result;
            }

        }

        #endregion

        /// <summary>
        /// Clears thread static field. Called on request end.
        /// </summary>
        private static void Clear()
        {
            _current = null;
            _default = null;
        }

#if !SILVERLIGHT
        /// <summary>
        /// Gets/sets/resets legacy configuration setting "date.timezone".
        /// </summary>
        internal static object GsrTimeZone(LibraryConfiguration/*!*/ local, LibraryConfiguration/*!*/ @default, object value, IniAction action)
        {
            string result = (local.Date.TimeZone != null) ? local.Date.TimeZone.StandardName : null;

            switch (action)
            {
                case IniAction.Set:
                    {
                        string name = Core.Convert.ObjectToString(value);
                        TimeZoneInfo zone = GetTimeZone(name);

                        if (zone == null)
                        {
                            PhpException.Throw(PhpError.Warning, LibResources.GetString("unknown_timezone", name));
                        }
                        else
                        {
                            local.Date.TimeZone = zone;
                        }
                        break;
                    }

                case IniAction.Restore:
                    local.Date.TimeZone = @default.Date.TimeZone;
                    break;
            }
            return result;
        }
#endif

        /// <summary>
        /// Gets an instance of <see cref="TimeZone"/> corresponding to specified PHP name for time zone.
        /// </summary>
        /// <param name="phpName">PHP time zone name.</param>
        /// <returns>The time zone or a <B>null</B> reference.</returns>
        public static TimeZoneInfo GetTimeZone(string/*!*/ phpName)
        {
            if (string.IsNullOrEmpty(phpName))
                return null;

            // simple binary search (not the Array.BinarySearch)
            var timezones = PhpTimeZone.timezones;
            int a = 0, b = timezones.Length - 1;
            while (a <= b)
            {
                int x = (a + b) >> 1;
                int comparison = StringComparer.OrdinalIgnoreCase.Compare(timezones[x].PhpName, phpName);
                if (comparison == 0)
                    return timezones[x].Info;
                
                if (comparison < 0)
                    a = x + 1;
                else //if (comparison > 0)
                    b = x - 1;
            }

            return null;
        }

        /// <summary>
        /// Tries to match given <paramref name="systemTimeZone"/> to our fixed <see cref="timezones"/>.
        /// </summary>
        private static TimeZoneInfo SystemToPhpTimeZone(TimeZoneInfo systemTimeZone)
        {
            if (systemTimeZone == null)
                return null;

            var tzns = timezones;
            for (int i = 0; i < tzns.Length; i++)
            {
                var tz = tzns[i].Info;
                if (tz != null && tz.DisplayName.EqualsOrdinalIgnoreCase(systemTimeZone.DisplayName) && tz.HasSameRules(systemTimeZone))
                    return tz;
            }

            return null;
        }

#if DEBUG

        internal static TimeZoneInfo/*!*/NepalTimeZone { get { return GetTimeZone("Asia/Katmandu"); } }// = TimeZoneInfo.FindSystemTimeZoneById("Nepal Standard Time");// new _NepalTimeZone();
        internal static TimeZoneInfo/*!*/PacificTimeZone { get { return GetTimeZone("America/Los_Angeles"); } }//  = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");//new _PacificTimeZone();
        internal static TimeZoneInfo/*!*/GmtTimeZone { get { return GetTimeZone("Etc/GMT"); } }//  = TimeZoneInfo.FindSystemTimeZoneById("GTM");
#endif

        #region date_default_timezone_get, date_default_timezone_set

        [ImplementsFunction("date_default_timezone_set")]
        public static bool SetCurrentTimeZone(string zoneName)
        {
            var zone = GetTimeZone(zoneName);
            if (zone == null)
            {
                PhpException.Throw(PhpError.Notice, LibResources.GetString("unknown_timezone", zoneName));
                return false;
            }
            _default = zone;
            return true;
        }

        [ImplementsFunction("date_default_timezone_get")]
        public static string GetCurrentTimeZone()
        {
            var timezone = CurrentTimeZone;

            return (timezone != null) ? timezone.Id : null;
        }

        #endregion

        #region timezone_identifiers_list, timezone_version_get

        [ImplementsFunction("timezone_identifiers_list")]
        public static PhpArray IdentifierList()
        {
            var timezones = PhpTimeZone.timezones;

            // copy names to PHP array:
            var array = new PhpArray(timezones.Length);
            for (int i = 0; i < timezones.Length; i++)
                if (!timezones[i].IsAlias)
                    array.AddToEnd(timezones[i].PhpName);

            //
            return array;
        }

        /// <summary>
        /// Gets the version of used the time zone database.
        /// </summary>
        [ImplementsFunction("timezone_version_get")]
        public static string GetTZVersion()
        {
            try
            {
                using (var reg = Microsoft.Win32.Registry.LocalMachine.CreateSubKey(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Time Zones"))
                    return reg.GetValue("TzVersion", 0).ToString() + ".system";
            }
            catch { }

            // no windows update installed
            return "0.system";
        }

        #endregion

        #region timezone_open, timezone_offset_get

        /// <summary>
        /// Alias of new <see cref="DateTimeZone"/>
        /// </summary>
        [ImplementsFunction("timezone_open")]
        [return: CastToFalse]
        public static object TimeZoneOpen(ScriptContext/*!*/context, string timezone)
        {
            var tz = GetTimeZone(timezone);
            if (tz == null)
                return null;

            return new DateTimeZone(context, tz);
        }

        /// <summary>
        /// Alias of <see cref="DateTimeZone.getOffset"/>
        /// </summary>
        [ImplementsFunction("timezone_offset_get")]
        [return: CastToFalse]
        public static int TimeZoneOffsetGet(ScriptContext context, DateTimeZone timezone, __PHP__DateTime datetime)
        {
            if (timezone == null)
                return -1;

            var result = timezone.getOffset(context, datetime);
            if (result == null)
                return -1;

            return PHP.Core.Convert.ObjectToInteger(timezone.getOffset(context, datetime));
        }

        [ImplementsFunction("timezone_transitions_get")]
        [return: CastToFalse]
        public static PhpArray TimeZoneGetTransitions(ScriptContext context, DateTimeZone timezone)
        {
            if (timezone == null)
                return null;

            return (PhpArray)timezone.getTransitions(context, Arg.Default, Arg.Default);
        }

        #endregion

        #region Unit Testing
#if DEBUG

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

        [Test]
        static void TestGetTimeZone()
        {
            TimeZoneInfo zone;

            zone = GetTimeZone("Europe/Prague");
            Debug.Assert(zone != null && zone.Id == "Europe/Prague");

            zone = GetTimeZone("europe/prague");
            Debug.Assert(zone != null && zone.Id == "Europe/Prague");

            zone = GetTimeZone("foo");
            Debug.Assert(zone == null);
        }

#endif
        #endregion
    }
}
