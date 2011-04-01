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
using Microsoft.Win32;
using PHP.Core;

#if SILVERLIGHT
using PHP.CoreCLR;
#endif

namespace PHP.Library
{
	/// <summary>
	/// Provides timezone information for PHP functions.
	/// </summary>
	[ImplementsExtension(LibraryDescriptor.ExtDate)]
	public static class PhpTimeZone
	{
		private const string EnvVariableName = "TZ";

		/// <summary>
		/// Registers <see cref="Clear"/> called on request end.
		/// </summary>
		static PhpTimeZone()
		{
			RequestContext.RequestEnd += new Action(Clear);
		}

		/// <summary>
		/// Gets the current time zone for PHP date-time library functions. Associated with the current thread.
		/// </summary>
        /// <remarks>It returns the time zone set by date_default_timezone_set PHP function.
        /// If no time zone was set, the time zone is determined in following order:
        /// 1. the time zone set in configuration
        /// 2. the time zone of the current system
        /// 3. default UTC time zone</remarks>
		public static TimeZone CurrentTimeZone
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
		private static TimeZone _default;

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
            internal CurrentTimeZoneCache(TimeZone timezone)
            {
                this._timeZone = timezone;
                this._changedFunc = (_) => false;
            }
#endif

            /// <summary>
            /// Get the TimeZone set by the current process. Depends on environment variable, or local configuration, or system time zone.
            /// </summary>
            public TimeZone TimeZone
            {
                get
                {
                    if (_timeZone == null || _changedFunc == null || _changedFunc(_timeZone) == true)
                        _timeZone = DetermineTimeZone(out _changedFunc);    // get the current timezone, update the function that determines, if the timezone has to be rechecked.

                    return _timeZone;
                }
            }

            private TimeZone _timeZone;

            /// <summary>
            /// Function that determines if the current timezone should be rechecked.
            /// </summary>
            private Func<TimeZone/*!*/, bool> _changedFunc;

            /// <summary>
            /// Finds out the time zone in the way how PHP does.
            /// </summary>
            private static TimeZone DetermineTimeZone(out Func<TimeZone, bool> changedFunc)
            {
                TimeZone result;

                // check environment variable:
#if !SILVERLIGHT
                string env_tz = Environment.GetEnvironmentVariable(EnvVariableName);
                if (!String.IsNullOrEmpty(env_tz))
                {
                    result = GetTimeZone(env_tz);
                    if (result != null)
                    {
                        // recheck the timezone only if the environment variable changes
                        changedFunc = (timezone) => String.Compare(timezone.StandardName, Environment.GetEnvironmentVariable(EnvVariableName), true) != 0;
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
                    changedFunc = (timezone) => config.Date.TimeZone != timezone;
                    return config.Date.TimeZone;
                }

                // convert current system time zone to PHP zone:
                result = SystemToPhpTimeZone(TimeZone.CurrentTimeZone);

                // UTC:
                if (result == null)
                    result = GetTimeZone("UTC");

                PhpException.Throw(PhpError.Strict, LibResources.GetString("using_implicit_timezone", result.StandardName));

                // recheck the timezone when the TimeZone in local configuration is set
                changedFunc = (timezone) => config.Date.TimeZone != null;
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
						TimeZone zone = GetTimeZone(name);

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
		public static TimeZone GetTimeZone(string/*!*/ phpName)
		{
			if (phpName == null) return null;

			if (zones == null) InitTables();
            
#if !SILVERLIGHT
			int zone_idx = Array.BinarySearch(zones, 0, zones.Length, phpName, CaseInsensitiveComparer.DefaultInvariant);
#else
            int zone_idx = Array.BinarySearch(zones, 0, zones.Length, phpName, StringComparer.InvariantCultureIgnoreCase);
#endif

			if (zone_idx < 0) return null;

			return GetTimeZoneByIndex(zone_idx);
		}

		/// <summary>
		/// Creates a time zone given the index in <see cref="zones"/> array.
		/// </summary>
		private static TimeZone GetTimeZoneByIndex(int zoneIndex)
		{
			Debug.Assert(zones != null && data != null);

			string php_name = zones[zoneIndex];
			int dst_idx = data[zoneIndex, 1];

			return new CustomTimeZone(
			  php_name,
			  php_name,
			  data[zoneIndex, 0] * 15,
			  daylightChanges[dst_idx, 0],
			  daylightChanges[dst_idx, 1],
			  daylightChanges[dst_idx, 2],
			  daylightChanges[dst_idx, 3]);
		}

		/// <summary>
		/// Converts a specified zone to the PHP zone. Searches <see cref="data"/> array for zone offset.
		/// </summary>
		private static TimeZone SystemToPhpTimeZone(TimeZone/*!*/ zone)
		{
			DateTime now = DateTime.Now;
			DaylightTime dst = zone.GetDaylightChanges(now.Year);

           


			// calculates offset in quarters of hour:
			int qoffset = (int)DateTimeUtils.GetStandardUtcOffset(zone).TotalMinutes / 15;

			if (zones == null) InitTables();

			int index = -1;

			for (int i = 0; i < data.GetLength(0); i++)
			{
				if (data[i, 0] == qoffset)
				{
					// partial match
					if (index < 0) index = i;

					int dst_idx = data[i, 1];
					if ((dst_idx == 0 && dst.Delta.Ticks == 0) ||
						(daylightChanges[dst_idx, 0] == dst.Start.Month &&
						daylightChanges[dst_idx, 1] == dst.Start.Day &&
						daylightChanges[dst_idx, 2] == dst.End.Month &&
						daylightChanges[dst_idx, 3] == dst.End.Day))
					{
						// exact match
						return GetTimeZoneByIndex(i);
					}
				}
			}

			return (index >= 0 ? GetTimeZoneByIndex(index) : null);
		}

		#region Predefined Time Zones

		/// <summary>
		/// Custom time zone.
		/// </summary>
		public sealed class CustomTimeZone : CustomTimeZoneBase
		{
			public override string DaylightName { get { return daylightName; } }
			private readonly string daylightName;

			public override string StandardName { get { return standardName; } }
			private readonly string standardName;

			private readonly int delta;
			private readonly int offset;

			private readonly int dstStartMonth;
			private readonly int dstStartDay;
			private readonly int dstEndMonth;
			private readonly int dstEndDay;

			public override TimeSpan GetUtcOffset(DateTime time)
			{
				return this.IsDaylightSavingTime(time) ?
				  TimeSpan.FromMinutes(offset + 60 * delta) :
				  TimeSpan.FromMinutes(offset);
			}

			public override DaylightTime GetDaylightChanges(int year)
			{
				return new DaylightTime
				(
				  (dstStartMonth >= 0) ? new DateTime(year, dstStartMonth, dstStartDay) : DateTime.MinValue,
				  (dstStartMonth >= 0) ? new DateTime(year, dstEndMonth, dstEndDay) : DateTime.MinValue,
				  TimeSpan.FromHours(delta)
				);
			}

			public CustomTimeZone(string daylightName, string standardName, int offset,
			  int dstStartMonth, int dstStartDay, int dstEndMonth, int dstEndDay)
			{
				this.daylightName = daylightName;
				this.standardName = standardName;
				this.offset = offset;
				this.dstStartMonth = dstStartMonth;
				this.dstStartDay = dstStartDay;
				this.dstEndMonth = dstEndMonth;
				this.dstEndDay = dstEndDay;
				this.delta = dstStartMonth >= 0 ? 1 : 0;
			}
		}

#if DEBUG

		/// <summary>
		/// Nepal time zone (+05:45 UTC) for debugging purposes.
		/// </summary>
		private sealed class _NepalTimeZone : CustomTimeZoneBase
		{
			public override string DaylightName { get { return "Nepal Standard Time"; } }
			public override string StandardName { get { return "Nepal Standard Time"; } }

			public override TimeSpan GetUtcOffset(DateTime time)
			{
				return new TimeSpan(0, 5, 45, 0, 0);
			}

			public override DaylightTime GetDaylightChanges(int year)
			{
				return new DaylightTime
				(
				  new DateTime(0),
				  new DateTime(0),
				  new TimeSpan(0)
				);
			}
		}

		/// <summary>
		/// Pacific time zone (-08:00 UTC) for debugging purposes.
		/// </summary>
		private sealed class _PacificTimeZone : CustomTimeZoneBase
		{
			public override string DaylightName { get { return "Pacific Daylight Time"; } }
			public override string StandardName { get { return "Pacific Standard Time"; } }

			public override TimeSpan GetUtcOffset(DateTime time)
			{
				return this.IsDaylightSavingTime(time) ?
				  new TimeSpan(0, -7, 0, 0, 0) : new TimeSpan(0, -8, 0, 0, 0);
			}

			public override DaylightTime GetDaylightChanges(int year)
			{
				return new DaylightTime
				(
				  new DateTime(year, 4, 3),
				  new DateTime(year, 10, 30),
				  new TimeSpan(0, 1, 0, 0, 0)
				);
			}
		}

		internal static readonly TimeZone NepalTimeZone = new _NepalTimeZone();
		internal static readonly TimeZone PacificTimeZone = new _PacificTimeZone();

#endif

		#endregion

		#region date_default_timezone_get, date_default_timezone_set

		[ImplementsFunction("date_default_timezone_set")]
		public static bool SetCurrentTimeZone(string zoneName)
		{
			TimeZone zone = GetTimeZone(zoneName);
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

            return (timezone != null) ? timezone.StandardName : null;
		}

		#endregion

		#region PHP Time Zone Tables

		/// <summary>
		/// Sorted list of PHP zone names.
		/// </summary>
		private static string[] zones;

		/// <summary>
		/// Each zone is defined by two numbers: 
		/// 1) number of quarters of hour from GMT (without DST)
		/// 2) daylight changes index
		/// </summary>
		private static sbyte[,] data;

		/// <summary>
		/// Daylight changes { month-start, day-start, month-end, day-end }. 
		/// </summary>
		private static int[,] daylightChanges;

		internal static void InitTables()
		{

			// generated by TimeZones.php script //

			#region Generated

			zones = new string[]
			{
			  "Africa/Abidjan",
			  "Africa/Accra",
			  "Africa/Addis_Ababa",
			  "Africa/Algiers",
			  "Africa/Asmera",
			  "Africa/Bamako",
			  "Africa/Bangui",
			  "Africa/Banjul",
			  "Africa/Bissau",
			  "Africa/Blantyre",
			  "Africa/Brazzaville",
			  "Africa/Bujumbura",
			  "Africa/Cairo",
			  "Africa/Casablanca",
			  "Africa/Ceuta",
			  "Africa/Conakry",
			  "Africa/Dakar",
			  "Africa/Dar_es_Salaam",
			  "Africa/Djibouti",
			  "Africa/Douala",
			  "Africa/El_Aaiun",
			  "Africa/Freetown",
			  "Africa/Gaborone",
			  "Africa/Harare",
			  "Africa/Johannesburg",
			  "Africa/Kampala",
			  "Africa/Khartoum",
			  "Africa/Kigali",
			  "Africa/Kinshasa",
			  "Africa/Lagos",
			  "Africa/Libreville",
			  "Africa/Lome",
			  "Africa/Luanda",
			  "Africa/Lubumbashi",
			  "Africa/Lusaka",
			  "Africa/Malabo",
			  "Africa/Maputo",
			  "Africa/Maseru",
			  "Africa/Mbabane",
			  "Africa/Mogadishu",
			  "Africa/Monrovia",
			  "Africa/Nairobi",
			  "Africa/Ndjamena",
			  "Africa/Niamey",
			  "Africa/Nouakchott",
			  "Africa/Ouagadougou",
			  "Africa/Porto-Novo",
			  "Africa/Sao_Tome",
			  "Africa/Timbuktu",
			  "Africa/Tripoli",
			  "Africa/Tunis",
			  "Africa/Windhoek",
			  "America/Adak",
			  "America/Anchorage",
			  "America/Anguilla",
			  "America/Antigua",
			  "America/Araguaina",
			  "America/Argentina/Buenos_Aires",
			  "America/Argentina/Catamarca",
			  "America/Argentina/ComodRivadavia",
			  "America/Argentina/Cordoba",
			  "America/Argentina/Jujuy",
			  "America/Argentina/La_Rioja",
			  "America/Argentina/Mendoza",
			  "America/Argentina/Rio_Gallegos",
			  "America/Argentina/San_Juan",
			  "America/Argentina/Tucuman",
			  "America/Argentina/Ushuaia",
			  "America/Aruba",
			  "America/Asuncion",
			  "America/Atka",
			  "America/Bahia",
			  "America/Barbados",
			  "America/Belem",
			  "America/Belize",
			  "America/Boa_Vista",
			  "America/Bogota",
			  "America/Boise",
			  "America/Buenos_Aires",
			  "America/Cambridge_Bay",
			  "America/Campo_Grande",
			  "America/Cancun",
			  "America/Caracas",
			  "America/Catamarca",
			  "America/Cayenne",
			  "America/Cayman",
			  "America/Chicago",
			  "America/Chihuahua",
			  "America/Coral_Harbour",
			  "America/Cordoba",
			  "America/Costa_Rica",
			  "America/Cuiaba",
			  "America/Curacao",
			  "America/Danmarkshavn",
			  "America/Dawson",
			  "America/Dawson_Creek",
			  "America/Denver",
			  "America/Detroit",
			  "America/Dominica",
			  "America/Edmonton",
			  "America/Eirunepe",
			  "America/El_Salvador",
			  "America/Ensenada",
			  "America/Fort_Wayne",
			  "America/Fortaleza",
			  "America/Glace_Bay",
			  "America/Godthab",
			  "America/Goose_Bay",
			  "America/Grand_Turk",
			  "America/Grenada",
			  "America/Guadeloupe",
			  "America/Guatemala",
			  "America/Guayaquil",
			  "America/Guyana",
			  "America/Halifax",
			  "America/Havana",
			  "America/Hermosillo",
			  "America/Indiana/Indianapolis",
			  "America/Indiana/Knox",
			  "America/Indiana/Marengo",
			  "America/Indiana/Vevay",
			  "America/Indianapolis",
			  "America/Inuvik",
			  "America/Iqaluit",
			  "America/Jamaica",
			  "America/Jujuy",
			  "America/Juneau",
			  "America/Kentucky/Louisville",
			  "America/Kentucky/Monticello",
			  "America/Knox_IN",
			  "America/La_Paz",
			  "America/Lima",
			  "America/Los_Angeles",
			  "America/Louisville",
			  "America/Maceio",
			  "America/Managua",
			  "America/Manaus",
			  "America/Martinique",
			  "America/Mazatlan",
			  "America/Mendoza",
			  "America/Menominee",
			  "America/Merida",
			  "America/Mexico_City",
			  "America/Miquelon",
			  "America/Monterrey",
			  "America/Montevideo",
			  "America/Montreal",
			  "America/Montserrat",
			  "America/Nassau",
			  "America/New_York",
			  "America/Nipigon",
			  "America/Nome",
			  "America/Noronha",
			  "America/North_Dakota/Center",
			  "America/Panama",
			  "America/Pangnirtung",
			  "America/Paramaribo",
			  "America/Phoenix",
			  "America/Port_of_Spain",
			  "America/Port-au-Prince",
			  "America/Porto_Acre",
			  "America/Porto_Velho",
			  "America/Puerto_Rico",
			  "America/Rainy_River",
			  "America/Rankin_Inlet",
			  "America/Recife",
			  "America/Regina",
			  "America/Rio_Branco",
			  "America/Rosario",
			  "America/Santiago",
			  "America/Santo_Domingo",
			  "America/Sao_Paulo",
			  "America/Scoresbysund",
			  "America/Shiprock",
			  "America/St_Johns",
			  "America/St_Kitts",
			  "America/St_Lucia",
			  "America/St_Thomas",
			  "America/St_Vincent",
			  "America/Swift_Current",
			  "America/Tegucigalpa",
			  "America/Thule",
			  "America/Thunder_Bay",
			  "America/Tijuana",
			  "America/Toronto",
			  "America/Tortola",
			  "America/Vancouver",
			  "America/Virgin",
			  "America/Whitehorse",
			  "America/Winnipeg",
			  "America/Yakutat",
			  "America/Yellowknife",
			  "Antarctica/Casey",
			  "Antarctica/Davis",
			  "Antarctica/DumontDUrville",
			  "Antarctica/Mawson",
			  "Antarctica/McMurdo",
			  "Antarctica/Palmer",
			  "Antarctica/Rothera",
			  "Antarctica/South_Pole",
			  "Antarctica/Syowa",
			  "Antarctica/Vostok",
			  "Arctic/Longyearbyen",
			  "Asia/Aden",
			  "Asia/Almaty",
			  "Asia/Amman",
			  "Asia/Anadyr",
			  "Asia/Aqtau",
			  "Asia/Aqtobe",
			  "Asia/Ashgabat",
			  "Asia/Ashkhabad",
			  "Asia/Baghdad",
			  "Asia/Bahrain",
			  "Asia/Baku",
			  "Asia/Bangkok",
			  "Asia/Beirut",
			  "Asia/Bishkek",
			  "Asia/Brunei",
			  "Asia/Calcutta",
			  "Asia/Choibalsan",
			  "Asia/Chongqing",
			  "Asia/Chungking",
			  "Asia/Colombo",
			  "Asia/Dacca",
			  "Asia/Damascus",
			  "Asia/Dhaka",
			  "Asia/Dili",
			  "Asia/Dubai",
			  "Asia/Dushanbe",
			  "Asia/Gaza",
			  "Asia/Harbin",
			  "Asia/Hong_Kong",
			  "Asia/Hovd",
			  "Asia/Irkutsk",
			  "Asia/Istanbul",
			  "Asia/Jakarta",
			  "Asia/Jayapura",
			  "Asia/Jerusalem",
			  "Asia/Kabul",
			  "Asia/Kamchatka",
			  "Asia/Karachi",
			  "Asia/Kashgar",
			  "Asia/Katmandu",
			  "Asia/Krasnoyarsk",
			  "Asia/Kuala_Lumpur",
			  "Asia/Kuching",
			  "Asia/Kuwait",
			  "Asia/Macao",
			  "Asia/Macau",
			  "Asia/Magadan",
			  "Asia/Makassar",
			  "Asia/Manila",
			  "Asia/Muscat",
			  "Asia/Nicosia",
			  "Asia/Novosibirsk",
			  "Asia/Omsk",
			  "Asia/Oral",
			  "Asia/Phnom_Penh",
			  "Asia/Pontianak",
			  "Asia/Pyongyang",
			  "Asia/Qatar",
			  "Asia/Qyzylorda",
			  "Asia/Rangoon",
			  "Asia/Riyadh",
			  "Asia/Saigon",
			  "Asia/Sakhalin",
			  "Asia/Samarkand",
			  "Asia/Seoul",
			  "Asia/Shanghai",
			  "Asia/Singapore",
			  "Asia/Taipei",
			  "Asia/Tashkent",
			  "Asia/Tbilisi",
			  "Asia/Tehran",
			  "Asia/Tel_Aviv",
			  "Asia/Thimbu",
			  "Asia/Thimphu",
			  "Asia/Tokyo",
			  "Asia/Ujung_Pandang",
			  "Asia/Ulaanbaatar",
			  "Asia/Ulan_Bator",
			  "Asia/Urumqi",
			  "Asia/Vientiane",
			  "Asia/Vladivostok",
			  "Asia/Yakutsk",
			  "Asia/Yekaterinburg",
			  "Asia/Yerevan",
			  "Atlantic/Azores",
			  "Atlantic/Azores",
			  "Atlantic/Bermuda",
			  "Atlantic/Bermuda",
			  "Atlantic/Canary",
			  "Atlantic/Canary",
			  "Atlantic/Cape_Verde",
			  "Atlantic/Cape_Verde",
			  "Atlantic/Faeroe",
			  "Atlantic/Faeroe",
			  "Atlantic/Jan_Mayen",
			  "Atlantic/Jan_Mayen",
			  "Atlantic/Madeira",
			  "Atlantic/Madeira",
			  "Atlantic/Reykjavik",
			  "Atlantic/Reykjavik",
			  "Atlantic/South_Georgia",
			  "Atlantic/South_Georgia",
			  "Atlantic/St_Helena",
			  "Atlantic/St_Helena",
			  "Atlantic/Stanley",
			  "Atlantic/Stanley",
			  "Brazil/Acre",
			  "Brazil/DeNoronha",
			  "Brazil/East",
			  "Brazil/West",
			  "Canada/Atlantic",
			  "Canada/Central",
			  "Canada/Eastern",
			  "Canada/East-Saskatchewan",
			  "Canada/Mountain",
			  "Canada/Newfoundland",
			  "Canada/Pacific",
			  "Canada/Saskatchewan",
			  "Canada/Yukon",
			  "CET",
			  "Chile/Continental",
			  "Chile/EasterIsland",
			  "CST6CDT",
			  "Cuba",
			  "EET",
			  "Egypt",
			  "Eire",
			  "EST",
			  "EST5EDT",
			  "Etc/GMT",
			  "Etc/GMT+0",
			  "Etc/GMT+1",
			  "Etc/GMT+10",
			  "Etc/GMT+11",
			  "Etc/GMT+12",
			  "Etc/GMT+2",
			  "Etc/GMT+3",
			  "Etc/GMT+4",
			  "Etc/GMT+5",
			  "Etc/GMT+6",
			  "Etc/GMT+7",
			  "Etc/GMT+8",
			  "Etc/GMT+9",
			  "Etc/GMT0",
			  "Etc/GMT-0",
			  "Etc/GMT-1",
			  "Etc/GMT-10",
			  "Etc/GMT-11",
			  "Etc/GMT-12",
			  "Etc/GMT-13",
			  "Etc/GMT-14",
			  "Etc/GMT-2",
			  "Etc/GMT-3",
			  "Etc/GMT-4",
			  "Etc/GMT-5",
			  "Etc/GMT-6",
			  "Etc/GMT-7",
			  "Etc/GMT-8",
			  "Etc/GMT-9",
			  "Etc/Greenwich",
			  "Etc/UCT",
			  "Etc/Universal",
			  "Etc/UTC",
			  "Etc/Zulu",
			  "Europe/Amsterdam",
			  "Europe/Andorra",
			  "Europe/Athens",
			  "Europe/Belfast",
			  "Europe/Belgrade",
			  "Europe/Berlin",
			  "Europe/Bratislava",
			  "Europe/Brussels",
			  "Europe/Bucharest",
			  "Europe/Budapest",
			  "Europe/Chisinau",
			  "Europe/Copenhagen",
			  "Europe/Dublin",
			  "Europe/Gibraltar",
			  "Europe/Helsinki",
			  "Europe/Istanbul",
			  "Europe/Kaliningrad",
			  "Europe/Kiev",
			  "Europe/Lisbon",
			  "Europe/Ljubljana",
			  "Europe/London",
			  "Europe/Luxembourg",
			  "Europe/Madrid",
			  "Europe/Malta",
			  "Europe/Mariehamn",
			  "Europe/Minsk",
			  "Europe/Monaco",
			  "Europe/Moscow",
			  "Europe/Nicosia",
			  "Europe/Oslo",
			  "Europe/Paris",
			  "Europe/Prague",
			  "Europe/Riga",
			  "Europe/Rome",
			  "Europe/Samara",
			  "Europe/San_Marino",
			  "Europe/Sarajevo",
			  "Europe/Simferopol",
			  "Europe/Skopje",
			  "Europe/Sofia",
			  "Europe/Stockholm",
			  "Europe/Tallinn",
			  "Europe/Tirane",
			  "Europe/Tiraspol",
			  "Europe/Uzhgorod",
			  "Europe/Vaduz",
			  "Europe/Vatican",
			  "Europe/Vienna",
			  "Europe/Vilnius",
			  "Europe/Warsaw",
			  "Europe/Zagreb",
			  "Europe/Zaporozhye",
			  "Europe/Zurich",
			  "Factory",
			  "GB",
			  "GB-Eire",
			  "GMT",
			  "GMT+0",
			  "GMT0",
			  "GMT-0",
			  "Greenwich",
			  "Hongkong",
			  "HST",
			  "Iceland",
			  "Indian/Antananarivo",
			  "Indian/Chagos",
			  "Indian/Christmas",
			  "Indian/Cocos",
			  "Indian/Comoro",
			  "Indian/Kerguelen",
			  "Indian/Mahe",
			  "Indian/Maldives",
			  "Indian/Mauritius",
			  "Indian/Mayotte",
			  "Indian/Reunion",
			  "Iran",
			  "Israel",
			  "Jamaica",
			  "Japan",
			  "Kwajalein",
			  "Libya",
			  "MET",
			  "Mexico/BajaNorte",
			  "Mexico/BajaSur",
			  "Mexico/General",
			  "MST",
			  "MST7MDT",
			  "Navajo",
			  "NZ",
			  "NZ-CHAT",
			  "Pacific/Apia",
			  "Pacific/Auckland",
			  "Pacific/Chatham",
			  "Pacific/Easter",
			  "Pacific/Efate",
			  "Pacific/Enderbury",
			  "Pacific/Fakaofo",
			  "Pacific/Fiji",
			  "Pacific/Funafuti",
			  "Pacific/Galapagos",
			  "Pacific/Gambier",
			  "Pacific/Guadalcanal",
			  "Pacific/Guam",
			  "Pacific/Honolulu",
			  "Pacific/Johnston",
			  "Pacific/Kiritimati",
			  "Pacific/Kosrae",
			  "Pacific/Kwajalein",
			  "Pacific/Majuro",
			  "Pacific/Marquesas",
			  "Pacific/Midway",
			  "Pacific/Nauru",
			  "Pacific/Niue",
			  "Pacific/Norfolk",
			  "Pacific/Noumea",
			  "Pacific/Pago_Pago",
			  "Pacific/Palau",
			  "Pacific/Pitcairn",
			  "Pacific/Ponape",
			  "Pacific/Port_Moresby",
			  "Pacific/Rarotonga",
			  "Pacific/Saipan",
			  "Pacific/Samoa",
			  "Pacific/Tahiti",
			  "Pacific/Tarawa",
			  "Pacific/Tongatapu",
			  "Pacific/Truk",
			  "Pacific/Wake",
			  "Pacific/Wallis",
			  "Pacific/Yap",
			  "Poland",
			  "Portugal",
			  "PRC",
			  "PST8PDT",
			  "ROC",
			  "ROK",
			  "Singapore",
			  "Turkey",
			  "UCT",
			  "Universal",
			  "US/Alaska",
			  "US/Aleutian",
			  "US/Arizona",
			  "US/Central",
			  "US/Eastern",
			  "US/East-Indiana",
			  "US/Hawaii",
			  "US/Indiana-Starke",
			  "US/Michigan",
			  "US/Mountain",
			  "US/Pacific",
			  "US/Pacific-New",
			  "US/Samoa",
			  "UTC",
			  "WET",
			  "W-SU",
			  "Zulu",
			};

			data = new sbyte[,]
			{
			  {  0, 0}, // Africa/Abidjan
			  {  0, 0}, // Africa/Accra
			  { 12, 0}, // Africa/Addis_Ababa
			  {  4, 0}, // Africa/Algiers
			  { 12, 0}, // Africa/Asmera
			  {  0, 0}, // Africa/Bamako
			  {  4, 0}, // Africa/Bangui
			  {  0, 0}, // Africa/Banjul
			  {  0, 0}, // Africa/Bissau
			  {  8, 0}, // Africa/Blantyre
			  {  4, 0}, // Africa/Brazzaville
			  {  8, 0}, // Africa/Bujumbura
			  {  8, 1}, // Africa/Cairo
			  {  0, 0}, // Africa/Casablanca
			  {  4, 2}, // Africa/Ceuta
			  {  0, 0}, // Africa/Conakry
			  {  0, 0}, // Africa/Dakar
			  { 12, 0}, // Africa/Dar_es_Salaam
			  { 12, 0}, // Africa/Djibouti
			  {  4, 0}, // Africa/Douala
			  {  0, 0}, // Africa/El_Aaiun
			  {  0, 0}, // Africa/Freetown
			  {  8, 0}, // Africa/Gaborone
			  {  8, 0}, // Africa/Harare
			  {  8, 0}, // Africa/Johannesburg
			  { 12, 0}, // Africa/Kampala
			  { 12, 0}, // Africa/Khartoum
			  {  8, 0}, // Africa/Kigali
			  {  4, 0}, // Africa/Kinshasa
			  {  4, 0}, // Africa/Lagos
			  {  4, 0}, // Africa/Libreville
			  {  0, 0}, // Africa/Lome
			  {  4, 0}, // Africa/Luanda
			  {  8, 0}, // Africa/Lubumbashi
			  {  8, 0}, // Africa/Lusaka
			  {  4, 0}, // Africa/Malabo
			  {  8, 0}, // Africa/Maputo
			  {  8, 0}, // Africa/Maseru
			  {  8, 0}, // Africa/Mbabane
			  { 12, 0}, // Africa/Mogadishu
			  {  0, 0}, // Africa/Monrovia
			  { 12, 0}, // Africa/Nairobi
			  {  4, 0}, // Africa/Ndjamena
			  {  4, 0}, // Africa/Niamey
			  {  0, 0}, // Africa/Nouakchott
			  {  0, 0}, // Africa/Ouagadougou
			  {  4, 0}, // Africa/Porto-Novo
			  {  0, 0}, // Africa/Sao_Tome
			  {  0, 0}, // Africa/Timbuktu
			  {  8, 0}, // Africa/Tripoli
			  {  4, 0}, // Africa/Tunis
			  {  4, 3}, // Africa/Windhoek
			  {-40, 4}, // America/Adak
			  {-36, 4}, // America/Anchorage
			  {-16, 0}, // America/Anguilla
			  {-16, 0}, // America/Antigua
			  {-12, 5}, // America/Araguaina
			  {-12, 0}, // America/Argentina/Buenos_Aires
			  {-12, 0}, // America/Argentina/Catamarca
			  {-12, 0}, // America/Argentina/ComodRivadavia
			  {-12, 0}, // America/Argentina/Cordoba
			  {-12, 0}, // America/Argentina/Jujuy
			  {-12, 0}, // America/Argentina/La_Rioja
			  {-12, 0}, // America/Argentina/Mendoza
			  {-12, 0}, // America/Argentina/Rio_Gallegos
			  {-12, 0}, // America/Argentina/San_Juan
			  {-12, 0}, // America/Argentina/Tucuman
			  {-12, 0}, // America/Argentina/Ushuaia
			  {-16, 0}, // America/Aruba
			  {-16, 6}, // America/Asuncion
			  {-40, 4}, // America/Atka
			  {-12, 5}, // America/Bahia
			  {-16, 0}, // America/Barbados
			  {-12, 0}, // America/Belem
			  {-24, 0}, // America/Belize
			  {-16, 0}, // America/Boa_Vista
			  {-20, 0}, // America/Bogota
			  {-28, 4}, // America/Boise
			  {-12, 0}, // America/Buenos_Aires
			  {-28, 4}, // America/Cambridge_Bay
			  {-16, 5}, // America/Campo_Grande
			  {-24, 7}, // America/Cancun
			  {-16, 0}, // America/Caracas
			  {-12, 0}, // America/Catamarca
			  {-12, 0}, // America/Cayenne
			  {-20, 0}, // America/Cayman
			  {-24, 4}, // America/Chicago
			  {-28, 7}, // America/Chihuahua
			  {-20, 0}, // America/Coral_Harbour
			  {-12, 0}, // America/Cordoba
			  {-24, 0}, // America/Costa_Rica
			  {-16, 5}, // America/Cuiaba
			  {-16, 0}, // America/Curacao
			  {  0, 0}, // America/Danmarkshavn
			  {-32, 4}, // America/Dawson
			  {-28, 0}, // America/Dawson_Creek
			  {-28, 4}, // America/Denver
			  {-20, 4}, // America/Detroit
			  {-16, 0}, // America/Dominica
			  {-28, 4}, // America/Edmonton
			  {-20, 0}, // America/Eirunepe
			  {-24, 0}, // America/El_Salvador
			  {-32, 4}, // America/Ensenada
			  {-20, 0}, // America/Fort_Wayne
			  {-12, 8}, // America/Fortaleza
			  {-16, 4}, // America/Glace_Bay
			  {-12, 9}, // America/Godthab
			  {-16, 4}, // America/Goose_Bay
			  {-20,10}, // America/Grand_Turk
			  {-16, 0}, // America/Grenada
			  {-16, 0}, // America/Guadeloupe
			  {-24, 0}, // America/Guatemala
			  {-20, 0}, // America/Guayaquil
			  {-16, 0}, // America/Guyana
			  {-16, 4}, // America/Halifax
			  {-20,11}, // America/Havana
			  {-28, 0}, // America/Hermosillo
			  {-20, 0}, // America/Indiana/Indianapolis
			  {-24, 0}, // America/Indiana/Knox
			  {-20, 0}, // America/Indiana/Marengo
			  {-20, 0}, // America/Indiana/Vevay
			  {-20, 0}, // America/Indianapolis
			  {-28, 4}, // America/Inuvik
			  {-20, 4}, // America/Iqaluit
			  {-20, 0}, // America/Jamaica
			  {-12, 0}, // America/Jujuy
			  {-36, 4}, // America/Juneau
			  {-20, 4}, // America/Kentucky/Louisville
			  {-20, 4}, // America/Kentucky/Monticello
			  {-24, 0}, // America/Knox_IN
			  {-16, 0}, // America/La_Paz
			  {-20, 0}, // America/Lima
			  {-32, 4}, // America/Los_Angeles
			  {-20, 4}, // America/Louisville
			  {-12, 8}, // America/Maceio
			  {-24, 0}, // America/Managua
			  {-16, 0}, // America/Manaus
			  {-16, 0}, // America/Martinique
			  {-28, 7}, // America/Mazatlan
			  {-12, 0}, // America/Mendoza
			  {-24, 4}, // America/Menominee
			  {-24, 7}, // America/Merida
			  {-24, 7}, // America/Mexico_City
			  {-12, 4}, // America/Miquelon
			  {-24, 7}, // America/Monterrey
			  {-12, 0}, // America/Montevideo
			  {-20, 4}, // America/Montreal
			  {-16, 0}, // America/Montserrat
			  {-20, 4}, // America/Nassau
			  {-20, 4}, // America/New_York
			  {-20, 4}, // America/Nipigon
			  {-36, 4}, // America/Nome
			  { -8, 8}, // America/Noronha
			  {-24, 4}, // America/North_Dakota/Center
			  {-20, 0}, // America/Panama
			  {-20, 4}, // America/Pangnirtung
			  {-12, 0}, // America/Paramaribo
			  {-28, 0}, // America/Phoenix
			  {-16, 0}, // America/Port_of_Spain
			  {-20, 0}, // America/Port-au-Prince
			  {-20, 0}, // America/Porto_Acre
			  {-16, 0}, // America/Porto_Velho
			  {-16, 0}, // America/Puerto_Rico
			  {-24, 4}, // America/Rainy_River
			  {-24, 4}, // America/Rankin_Inlet
			  {-12, 8}, // America/Recife
			  {-24, 0}, // America/Regina
			  {-20, 0}, // America/Rio_Branco
			  {-12, 0}, // America/Rosario
			  {-16,12}, // America/Santiago
			  {-16, 0}, // America/Santo_Domingo
			  {-12, 5}, // America/Sao_Paulo
			  { -4,13}, // America/Scoresbysund
			  {-28, 4}, // America/Shiprock
			  {-14, 4}, // America/St_Johns
			  {-16, 0}, // America/St_Kitts
			  {-16, 0}, // America/St_Lucia
			  {-16, 0}, // America/St_Thomas
			  {-16, 0}, // America/St_Vincent
			  {-24, 0}, // America/Swift_Current
			  {-24, 0}, // America/Tegucigalpa
			  {-16, 4}, // America/Thule
			  {-20, 4}, // America/Thunder_Bay
			  {-32, 4}, // America/Tijuana
			  {-20, 4}, // America/Toronto
			  {-16, 0}, // America/Tortola
			  {-32, 4}, // America/Vancouver
			  {-16, 0}, // America/Virgin
			  {-32, 4}, // America/Whitehorse
			  {-24, 4}, // America/Winnipeg
			  {-36, 4}, // America/Yakutat
			  {-28, 4}, // America/Yellowknife
			  { 32, 0}, // Antarctica/Casey
			  { 28, 0}, // Antarctica/Davis
			  { 40, 0}, // Antarctica/DumontDUrville
			  { 24, 0}, // Antarctica/Mawson
			  { 48,14}, // Antarctica/McMurdo
			  {-16,12}, // Antarctica/Palmer
			  {-12, 0}, // Antarctica/Rothera
			  { 48,14}, // Antarctica/South_Pole
			  { 12, 0}, // Antarctica/Syowa
			  { 24, 0}, // Antarctica/Vostok
			  {  4, 2}, // Arctic/Longyearbyen
			  { 12, 0}, // Asia/Aden
			  { 24, 2}, // Asia/Almaty
			  {  8,15}, // Asia/Amman
			  { 48, 2}, // Asia/Anadyr
			  { 20, 2}, // Asia/Aqtau
			  { 20, 2}, // Asia/Aqtobe
			  { 20, 0}, // Asia/Ashgabat
			  { 20, 0}, // Asia/Ashkhabad
			  { 12,16}, // Asia/Baghdad
			  { 12, 0}, // Asia/Bahrain
			  { 16, 2}, // Asia/Baku
			  { 28, 0}, // Asia/Bangkok
			  {  8, 9}, // Asia/Beirut
			  { 24, 2}, // Asia/Bishkek
			  { 32, 0}, // Asia/Brunei
			  { 22, 0}, // Asia/Calcutta
			  { 36,17}, // Asia/Choibalsan
			  { 32, 0}, // Asia/Chongqing
			  { 32, 0}, // Asia/Chungking
			  { 24, 0}, // Asia/Colombo
			  { 24, 0}, // Asia/Dacca
			  {  8,18}, // Asia/Damascus
			  { 24, 0}, // Asia/Dhaka
			  { 36, 0}, // Asia/Dili
			  { 16, 0}, // Asia/Dubai
			  { 20, 0}, // Asia/Dushanbe
			  {  8,19}, // Asia/Gaza
			  { 32, 0}, // Asia/Harbin
			  { 32, 0}, // Asia/Hong_Kong
			  { 28,17}, // Asia/Hovd
			  { 32, 2}, // Asia/Irkutsk
			  {  8, 2}, // Asia/Istanbul
			  { 28, 0}, // Asia/Jakarta
			  { 36, 0}, // Asia/Jayapura
			  {  8,20}, // Asia/Jerusalem
			  { 18, 0}, // Asia/Kabul
			  { 48, 2}, // Asia/Kamchatka
			  { 20, 0}, // Asia/Karachi
			  { 32, 0}, // Asia/Kashgar
			  { 23, 0}, // Asia/Katmandu
			  { 28, 2}, // Asia/Krasnoyarsk
			  { 32, 0}, // Asia/Kuala_Lumpur
			  { 32, 0}, // Asia/Kuching
			  { 12, 0}, // Asia/Kuwait
			  { 32, 0}, // Asia/Macao
			  { 32, 0}, // Asia/Macau
			  { 44, 2}, // Asia/Magadan
			  { 32, 0}, // Asia/Makassar
			  { 32, 0}, // Asia/Manila
			  { 16, 0}, // Asia/Muscat
			  {  8, 2}, // Asia/Nicosia
			  { 24, 2}, // Asia/Novosibirsk
			  { 24, 2}, // Asia/Omsk
			  { 20, 2}, // Asia/Oral
			  { 28, 0}, // Asia/Phnom_Penh
			  { 28, 0}, // Asia/Pontianak
			  { 36, 0}, // Asia/Pyongyang
			  { 12, 0}, // Asia/Qatar
			  { 24, 2}, // Asia/Qyzylorda
			  { 26, 0}, // Asia/Rangoon
			  { 12, 0}, // Asia/Riyadh
			  { 28, 0}, // Asia/Saigon
			  { 40, 2}, // Asia/Sakhalin
			  { 20, 0}, // Asia/Samarkand
			  { 36, 0}, // Asia/Seoul
			  { 32, 0}, // Asia/Shanghai
			  { 32, 0}, // Asia/Singapore
			  { 32, 0}, // Asia/Taipei
			  { 20, 0}, // Asia/Tashkent
			  { 16, 9}, // Asia/Tbilisi
			  { 14,21}, // Asia/Tehran
			  {  8,20}, // Asia/Tel_Aviv
			  { 24, 0}, // Asia/Thimbu
			  { 24, 0}, // Asia/Thimphu
			  { 36, 0}, // Asia/Tokyo
			  { 32, 0}, // Asia/Ujung_Pandang
			  { 32,17}, // Asia/Ulaanbaatar
			  { 32,17}, // Asia/Ulan_Bator
			  { 32, 0}, // Asia/Urumqi
			  { 28, 0}, // Asia/Vientiane
			  { 40, 2}, // Asia/Vladivostok
			  { 36, 2}, // Asia/Yakutsk
			  { 20, 2}, // Asia/Yekaterinburg
			  { 16, 2}, // Asia/Yerevan
			  { -4,13}, // Atlantic/Azores
			  { -4,13}, // Atlantic/Azores
			  {-16, 4}, // Atlantic/Bermuda
			  {-16, 4}, // Atlantic/Bermuda
			  {  0, 2}, // Atlantic/Canary
			  {  0, 2}, // Atlantic/Canary
			  { -4, 0}, // Atlantic/Cape_Verde
			  { -4, 0}, // Atlantic/Cape_Verde
			  {  0, 2}, // Atlantic/Faeroe
			  {  0, 2}, // Atlantic/Faeroe
			  {  4, 2}, // Atlantic/Jan_Mayen
			  {  4, 2}, // Atlantic/Jan_Mayen
			  {  0, 2}, // Atlantic/Madeira
			  {  0, 2}, // Atlantic/Madeira
			  {  0, 0}, // Atlantic/Reykjavik
			  {  0, 0}, // Atlantic/Reykjavik
			  { -8, 0}, // Atlantic/South_Georgia
			  { -8, 0}, // Atlantic/South_Georgia
			  {  0, 0}, // Atlantic/St_Helena
			  {  0, 0}, // Atlantic/St_Helena
			  {-16,22}, // Atlantic/Stanley
			  {-16,22}, // Atlantic/Stanley
			  {-20, 0}, // Brazil/Acre
			  { -8, 8}, // Brazil/DeNoronha
			  {-12, 5}, // Brazil/East
			  {-16, 0}, // Brazil/West
			  {-16, 4}, // Canada/Atlantic
			  {-24, 4}, // Canada/Central
			  {-20, 4}, // Canada/Eastern
			  {-24, 0}, // Canada/East-Saskatchewan
			  {-28, 4}, // Canada/Mountain
			  {-14, 4}, // Canada/Newfoundland
			  {-32, 4}, // Canada/Pacific
			  {-24, 0}, // Canada/Saskatchewan
			  {-32, 4}, // Canada/Yukon
			  {  4, 2}, // CET
			  {-16,12}, // Chile/Continental
			  {-24,12}, // Chile/EasterIsland
			  {-24, 4}, // CST6CDT
			  {-20,11}, // Cuba
			  {  8, 2}, // EET
			  {  8, 1}, // Egypt
			  {  0, 2}, // Eire
			  {-20, 0}, // EST
			  {-20, 4}, // EST5EDT
			  {  0, 0}, // Etc/GMT
			  {  0, 0}, // Etc/GMT+0
			  { -4, 0}, // Etc/GMT+1
			  {-40, 0}, // Etc/GMT+10
			  {-44, 0}, // Etc/GMT+11
			  {-48, 0}, // Etc/GMT+12
			  { -8, 0}, // Etc/GMT+2
			  {-12, 0}, // Etc/GMT+3
			  {-16, 0}, // Etc/GMT+4
			  {-20, 0}, // Etc/GMT+5
			  {-24, 0}, // Etc/GMT+6
			  {-28, 0}, // Etc/GMT+7
			  {-32, 0}, // Etc/GMT+8
			  {-36, 0}, // Etc/GMT+9
			  {  0, 0}, // Etc/GMT0
			  {  0, 0}, // Etc/GMT-0
			  {  4, 0}, // Etc/GMT-1
			  { 40, 0}, // Etc/GMT-10
			  { 44, 0}, // Etc/GMT-11
			  { 48, 0}, // Etc/GMT-12
			  { 52, 0}, // Etc/GMT-13
			  { 56, 0}, // Etc/GMT-14
			  {  8, 0}, // Etc/GMT-2
			  { 12, 0}, // Etc/GMT-3
			  { 16, 0}, // Etc/GMT-4
			  { 20, 0}, // Etc/GMT-5
			  { 24, 0}, // Etc/GMT-6
			  { 28, 0}, // Etc/GMT-7
			  { 32, 0}, // Etc/GMT-8
			  { 36, 0}, // Etc/GMT-9
			  {  0, 0}, // Etc/Greenwich
			  {  0, 0}, // Etc/UCT
			  {  0, 0}, // Etc/Universal
			  {  0, 0}, // Etc/UTC
			  {  0, 0}, // Etc/Zulu
			  {  4, 2}, // Europe/Amsterdam
			  {  4, 2}, // Europe/Andorra
			  {  8, 2}, // Europe/Athens
			  {  0, 2}, // Europe/Belfast
			  {  4, 2}, // Europe/Belgrade
			  {  4, 2}, // Europe/Berlin
			  {  4, 2}, // Europe/Bratislava
			  {  4, 2}, // Europe/Brussels
			  {  8, 2}, // Europe/Bucharest
			  {  4, 2}, // Europe/Budapest
			  {  8, 2}, // Europe/Chisinau
			  {  4, 2}, // Europe/Copenhagen
			  {  0, 2}, // Europe/Dublin
			  {  4, 2}, // Europe/Gibraltar
			  {  8, 2}, // Europe/Helsinki
			  {  8, 2}, // Europe/Istanbul
			  {  8, 2}, // Europe/Kaliningrad
			  {  8, 2}, // Europe/Kiev
			  {  0, 2}, // Europe/Lisbon
			  {  4, 2}, // Europe/Ljubljana
			  {  0, 2}, // Europe/London
			  {  4, 2}, // Europe/Luxembourg
			  {  4, 2}, // Europe/Madrid
			  {  4, 2}, // Europe/Malta
			  {  8, 2}, // Europe/Mariehamn
			  {  8, 2}, // Europe/Minsk
			  {  4, 2}, // Europe/Monaco
			  { 12, 2}, // Europe/Moscow
			  {  8, 2}, // Europe/Nicosia
			  {  4, 2}, // Europe/Oslo
			  {  4, 2}, // Europe/Paris
			  {  4, 2}, // Europe/Prague
			  {  8, 2}, // Europe/Riga
			  {  4, 2}, // Europe/Rome
			  { 16, 2}, // Europe/Samara
			  {  4, 2}, // Europe/San_Marino
			  {  4, 2}, // Europe/Sarajevo
			  {  8, 2}, // Europe/Simferopol
			  {  4, 2}, // Europe/Skopje
			  {  8, 2}, // Europe/Sofia
			  {  4, 2}, // Europe/Stockholm
			  {  8, 0}, // Europe/Tallinn
			  {  4, 2}, // Europe/Tirane
			  {  8, 2}, // Europe/Tiraspol
			  {  8, 2}, // Europe/Uzhgorod
			  {  4, 2}, // Europe/Vaduz
			  {  4, 2}, // Europe/Vatican
			  {  4, 2}, // Europe/Vienna
			  {  8, 0}, // Europe/Vilnius
			  {  4, 2}, // Europe/Warsaw
			  {  4, 2}, // Europe/Zagreb
			  {  8, 2}, // Europe/Zaporozhye
			  {  4, 2}, // Europe/Zurich
			  {  0, 0}, // Factory
			  {  0, 2}, // GB
			  {  0, 2}, // GB-Eire
			  {  0, 0}, // GMT
			  {  0, 0}, // GMT+0
			  {  0, 0}, // GMT0
			  {  0, 0}, // GMT-0
			  {  0, 0}, // Greenwich
			  { 32, 0}, // Hongkong
			  {-40, 0}, // HST
			  {  0, 0}, // Iceland
			  { 12, 0}, // Indian/Antananarivo
			  { 24, 0}, // Indian/Chagos
			  { 28, 0}, // Indian/Christmas
			  { 26, 0}, // Indian/Cocos
			  { 12, 0}, // Indian/Comoro
			  { 20, 0}, // Indian/Kerguelen
			  { 16, 0}, // Indian/Mahe
			  { 20, 0}, // Indian/Maldives
			  { 16, 0}, // Indian/Mauritius
			  { 12, 0}, // Indian/Mayotte
			  { 16, 0}, // Indian/Reunion
			  { 14,21}, // Iran
			  {  8,20}, // Israel
			  {-20, 0}, // Jamaica
			  { 36, 0}, // Japan
			  { 48, 0}, // Kwajalein
			  {  8, 0}, // Libya
			  {  4, 2}, // MET
			  {-32, 4}, // Mexico/BajaNorte
			  {-28, 7}, // Mexico/BajaSur
			  {-24, 7}, // Mexico/General
			  {-28, 0}, // MST
			  {-28, 4}, // MST7MDT
			  {-28, 4}, // Navajo
			  { 48,14}, // NZ
			  { 51,14}, // NZ-CHAT
			  {-44, 0}, // Pacific/Apia
			  { 48,14}, // Pacific/Auckland
			  { 51,14}, // Pacific/Chatham
			  {-24,12}, // Pacific/Easter
			  { 44, 0}, // Pacific/Efate
			  { 52, 0}, // Pacific/Enderbury
			  {-40, 0}, // Pacific/Fakaofo
			  { 48, 0}, // Pacific/Fiji
			  { 48, 0}, // Pacific/Funafuti
			  {-24, 0}, // Pacific/Galapagos
			  {-36, 0}, // Pacific/Gambier
			  { 44, 0}, // Pacific/Guadalcanal
			  { 40, 0}, // Pacific/Guam
			  {-40, 0}, // Pacific/Honolulu
			  {-40, 0}, // Pacific/Johnston
			  { 56, 0}, // Pacific/Kiritimati
			  { 44, 0}, // Pacific/Kosrae
			  { 48, 0}, // Pacific/Kwajalein
			  { 48, 0}, // Pacific/Majuro
			  {-38, 0}, // Pacific/Marquesas
			  {-44, 0}, // Pacific/Midway
			  { 48, 0}, // Pacific/Nauru
			  {-44, 0}, // Pacific/Niue
			  { 46, 0}, // Pacific/Norfolk
			  { 44, 0}, // Pacific/Noumea
			  {-44, 0}, // Pacific/Pago_Pago
			  { 36, 0}, // Pacific/Palau
			  {-32, 0}, // Pacific/Pitcairn
			  { 44, 0}, // Pacific/Ponape
			  { 40, 0}, // Pacific/Port_Moresby
			  {-40, 0}, // Pacific/Rarotonga
			  { 40, 0}, // Pacific/Saipan
			  {-44, 0}, // Pacific/Samoa
			  {-40, 0}, // Pacific/Tahiti
			  { 48, 0}, // Pacific/Tarawa
			  { 52,23}, // Pacific/Tongatapu
			  { 40, 0}, // Pacific/Truk
			  { 48, 0}, // Pacific/Wake
			  { 48, 0}, // Pacific/Wallis
			  { 40, 0}, // Pacific/Yap
			  {  4, 2}, // Poland
			  {  0, 2}, // Portugal
			  { 32, 0}, // PRC
			  {-32, 4}, // PST8PDT
			  { 32, 0}, // ROC
			  { 36, 0}, // ROK
			  { 32, 0}, // Singapore
			  {  8, 2}, // Turkey
			  {  0, 0}, // UCT
			  {  0, 0}, // Universal
			  {-36, 4}, // US/Alaska
			  {-40, 4}, // US/Aleutian
			  {-28, 0}, // US/Arizona
			  {-24, 4}, // US/Central
			  {-20, 4}, // US/Eastern
			  {-20, 0}, // US/East-Indiana
			  {-40, 0}, // US/Hawaii
			  {-24, 0}, // US/Indiana-Starke
			  {-20, 4}, // US/Michigan
			  {-28, 4}, // US/Mountain
			  {-32, 4}, // US/Pacific
			  {-32, 4}, // US/Pacific-New
			  {-44, 0}, // US/Samoa
			  {  0, 0}, // UTC
			  {  0, 2}, // WET
			  { 12, 2}, // W-SU
			  {  0, 0}, // Zulu
			};

			daylightChanges = new int[,] {
				{ -1, -1, -1, -1 }, // 0
				{  4, 27,  9, 28 }, // 1
				{  3, 26, 10, 29 }, // 2
				{  9,  3,  4,  2 }, // 3
				{  4,  2, 10, 29 }, // 4
				{ 10, 14,  2, 18 }, // 5
				{ 10,  7,  3,  4 }, // 6
				{  5,  7, 10,  1 }, // 7
				{ 10, 14, 12, 31 }, // 8
				{  3, 25, 10, 28 }, // 9
				{  4,  1, 10, 28 }, // 10
				{  4,  1, 10, 29 }, // 11
				{ 10, 14,  3, 11 }, // 12
				{  3, 25, 10, 29 }, // 13
				{ 10,  8,  3, 19 }, // 14
				{  3, 29,  9, 27 }, // 15
				{  4,  2, 10,  2 }, // 16
				{  4, 29,  9, 30 }, // 17
				{  4,  1, 10,  1 }, // 18
				{  4, 20, 10, 19 }, // 19
				{  4, 10,  9, 24 }, // 20
				{  3, 22,  9, 22 }, // 21
				{  9,  3,  4, 16 }, // 22
				{ 11,  5,  1, 29 }, // 23
				};

			#endregion

		}

		#endregion

		#region Unit Testing
#if DEBUG

		//[Test(true)]
		static void SortZones()
		{
			InitTables();

#if !SILVERLIGHT
			Array.Sort(zones, CaseInsensitiveComparer.DefaultInvariant);
#else
            Array.Sort(zones, StringComparer.InvariantCultureIgnoreCase);
#endif


			Console.WriteLine();
			foreach (string z in zones)
			{
				Console.WriteLine("\"{0}\",", z);
			}
		}

		[Test]
		static void TestSorted()
		{
			InitTables();
			string[] sorted = (string[])zones.Clone();

#if !SILVERLIGHT
			Array.Sort(sorted, CaseInsensitiveComparer.DefaultInvariant);
#else
            Array.Sort(sorted, StringComparer.InvariantCultureIgnoreCase);
#endif

            for (int i = 0; i < zones.Length; i++)
				Debug.Assert(sorted[i] == zones[i]);
		}

		[Test]
		static void TestGetTimeZone()
		{
			TimeZone zone;

			zone = GetTimeZone("Europe/Prague");
			Debug.Assert(zone != null && zone.StandardName == "Europe/Prague");

			zone = GetTimeZone("europe/prague");
			Debug.Assert(zone != null && zone.StandardName == "Europe/Prague");

			zone = GetTimeZone("foo");
			Debug.Assert(zone == null);
		}

#endif
		#endregion
	}
}
