namespace MySql.Data.Types
{
    using MySql.Data.Common;
    using MySql.Data.MySqlClient;
    using System;
    using System.Data;
    using System.Globalization;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential)]
    public struct MySqlDateTime : IMySqlValue, IConvertible, IComparable
    {
        private bool isNull;
        private MySqlDbType type;
        private int year;
        private int month;
        private int day;
        private int hour;
        private int minute;
        private int second;
        private int millisecond;
        public MySqlDateTime(int year, int month, int day, int hour, int minute, int second) : this(MySqlDbType.DateTime, year, month, day, hour, minute, second)
        {
        }

        public MySqlDateTime(DateTime dt) : this(MySqlDbType.DateTime, dt)
        {
        }

        public MySqlDateTime(MySqlDateTime mdt)
        {
            this.year = mdt.Year;
            this.month = mdt.Month;
            this.day = mdt.Day;
            this.hour = mdt.Hour;
            this.minute = mdt.Minute;
            this.second = mdt.Second;
            this.millisecond = 0;
            this.type = MySqlDbType.DateTime;
            this.isNull = false;
        }

        public MySqlDateTime(string dateTime) : this(Parse(dateTime))
        {
        }

        internal MySqlDateTime(MySqlDbType type, int year, int month, int day, int hour, int minute, int second)
        {
            this.isNull = false;
            this.type = type;
            this.year = year;
            this.month = month;
            this.day = day;
            this.hour = hour;
            this.minute = minute;
            this.second = second;
            this.millisecond = 0;
        }

        internal MySqlDateTime(MySqlDbType type, bool isNull) : this(type, 0, 0, 0, 0, 0, 0)
        {
            this.isNull = isNull;
        }

        internal MySqlDateTime(MySqlDbType type, DateTime val) : this(type, 0, 0, 0, 0, 0, 0)
        {
            this.isNull = false;
            this.year = val.Year;
            this.month = val.Month;
            this.day = val.Day;
            this.hour = val.Hour;
            this.minute = val.Minute;
            this.second = val.Second;
            this.millisecond = val.Millisecond;
        }

        public bool IsValidDateTime
        {
            get
            {
                return (((this.year != 0) && (this.month != 0)) && (this.day != 0));
            }
        }
        public int Year
        {
            get
            {
                return this.year;
            }
            set
            {
                this.year = value;
            }
        }
        public int Month
        {
            get
            {
                return this.month;
            }
            set
            {
                this.month = value;
            }
        }
        public int Day
        {
            get
            {
                return this.day;
            }
            set
            {
                this.day = value;
            }
        }
        public int Hour
        {
            get
            {
                return this.hour;
            }
            set
            {
                this.hour = value;
            }
        }
        public int Minute
        {
            get
            {
                return this.minute;
            }
            set
            {
                this.minute = value;
            }
        }
        public int Second
        {
            get
            {
                return this.second;
            }
            set
            {
                this.second = value;
            }
        }
        public int Millisecond
        {
            get
            {
                return this.millisecond;
            }
            set
            {
                this.millisecond = value;
            }
        }
        public bool IsNull
        {
            get
            {
                return this.isNull;
            }
        }
        MySqlDbType IMySqlValue.MySqlDbType
        {
            get
            {
                return this.type;
            }
        }
        DbType IMySqlValue.DbType
        {
            get
            {
                if ((this.type != MySqlDbType.Date) && (this.type != MySqlDbType.Newdate))
                {
                    return DbType.DateTime;
                }
                return DbType.Date;
            }
        }
        object IMySqlValue.Value
        {
            get
            {
                return this.GetDateTime();
            }
        }
        public DateTime Value
        {
            get
            {
                return this.GetDateTime();
            }
        }
        Type IMySqlValue.SystemType
        {
            get
            {
                return typeof(DateTime);
            }
        }
        string IMySqlValue.MySqlTypeName
        {
            get
            {
                MySqlDbType type = this.type;
                if (type == MySqlDbType.Timestamp)
                {
                    return "TIMESTAMP";
                }
                if (type != MySqlDbType.Date)
                {
                    if (type == MySqlDbType.Newdate)
                    {
                        return "NEWDATE";
                    }
                    return "DATETIME";
                }
                return "DATE";
            }
        }
        private void SerializeText(MySqlPacket packet, MySqlDateTime value)
        {
            string str = string.Empty;
            str = string.Format("{0:0000}-{1:00}-{2:00}", value.Year, value.Month, value.Day);
            if (this.type != MySqlDbType.Date)
            {
                str = string.Format("{0} {1:00}:{2:00}:{3:00}", new object[] { str, value.Hour, value.Minute, value.Second });
            }
            packet.WriteStringNoNull("'" + str + "'");
        }

        void IMySqlValue.WriteValue(MySqlPacket packet, bool binary, object value, int length)
        {
            MySqlDateTime time;
            string s = value as string;
            if (value is DateTime)
            {
                time = new MySqlDateTime(this.type, (DateTime) value);
            }
            else if (s != null)
            {
                time = new MySqlDateTime(this.type, DateTime.Parse(s, CultureInfo.CurrentCulture));
            }
            else
            {
                if (!(value is MySqlDateTime))
                {
                    throw new MySqlException("Unable to serialize date/time value.");
                }
                time = (MySqlDateTime) value;
            }
            if (!binary)
            {
                this.SerializeText(packet, time);
            }
            else
            {
                if (this.type == MySqlDbType.Timestamp)
                {
                    packet.WriteByte(11);
                }
                else
                {
                    packet.WriteByte(7);
                }
                packet.WriteInteger((long) time.Year, 2);
                packet.WriteByte((byte) time.Month);
                packet.WriteByte((byte) time.Day);
                if (this.type == MySqlDbType.Date)
                {
                    packet.WriteByte(0);
                    packet.WriteByte(0);
                    packet.WriteByte(0);
                }
                else
                {
                    packet.WriteByte((byte) time.Hour);
                    packet.WriteByte((byte) time.Minute);
                    packet.WriteByte((byte) time.Second);
                }
                if (this.type == MySqlDbType.Timestamp)
                {
                    packet.WriteInteger((long) time.Millisecond, 4);
                }
            }
        }

        internal static MySqlDateTime Parse(string s)
        {
            MySqlDateTime time = new MySqlDateTime();
            return time.ParseMySql(s);
        }

        internal static MySqlDateTime Parse(string s, DBVersion version)
        {
            MySqlDateTime time = new MySqlDateTime();
            return time.ParseMySql(s);
        }

        private MySqlDateTime ParseMySql(string s)
        {
            string[] strArray = s.Split(new char[] { '-', ' ', ':', '/' });
            int year = int.Parse(strArray[0]);
            int month = int.Parse(strArray[1]);
            int day = int.Parse(strArray[2]);
            int hour = 0;
            int minute = 0;
            int second = 0;
            if (strArray.Length > 3)
            {
                hour = int.Parse(strArray[3]);
                minute = int.Parse(strArray[4]);
                second = int.Parse(strArray[5]);
            }
            return new MySqlDateTime(this.type, year, month, day, hour, minute, second);
        }

        IMySqlValue IMySqlValue.ReadValue(MySqlPacket packet, long length, bool nullVal)
        {
            if (nullVal)
            {
                return new MySqlDateTime(this.type, true);
            }
            if (length >= 0L)
            {
                string s = packet.ReadString(length);
                return this.ParseMySql(s);
            }
            long num = packet.ReadByte();
            int year = 0;
            int month = 0;
            int day = 0;
            int hour = 0;
            int minute = 0;
            int second = 0;
            if (num >= 4L)
            {
                year = packet.ReadInteger(2);
                month = packet.ReadByte();
                day = packet.ReadByte();
            }
            if (num > 4L)
            {
                hour = packet.ReadByte();
                minute = packet.ReadByte();
                second = packet.ReadByte();
            }
            if (num > 7L)
            {
                packet.ReadInteger(4);
            }
            return new MySqlDateTime(this.type, year, month, day, hour, minute, second);
        }

        void IMySqlValue.SkipValue(MySqlPacket packet)
        {
            int num = packet.ReadByte();
            packet.Position += num;
        }

        public DateTime GetDateTime()
        {
            if (!this.IsValidDateTime)
            {
                throw new MySqlConversionException("Unable to convert MySQL date/time value to System.DateTime");
            }
            return new DateTime(this.year, this.month, this.day, this.hour, this.minute, this.second);
        }

        private static string FormatDateCustom(string format, int monthVal, int dayVal, int yearVal)
        {
            format = format.Replace("MM", "{0:00}");
            format = format.Replace("M", "{0}");
            format = format.Replace("dd", "{1:00}");
            format = format.Replace("d", "{1}");
            format = format.Replace("yyyy", "{2:0000}");
            format = format.Replace("yy", "{3:00}");
            format = format.Replace("y", "{4:0}");
            int num = yearVal - ((yearVal / 0x3e8) * 0x3e8);
            num -= (num / 100) * 100;
            int num2 = num - ((num / 10) * 10);
            return string.Format(format, new object[] { monthVal, dayVal, yearVal, num, num2 });
        }

        public override string ToString()
        {
            if (this.IsValidDateTime)
            {
                DateTime time = new DateTime(this.year, this.month, this.day, this.hour, this.minute, this.second);
                if (this.type != MySqlDbType.Date)
                {
                    return time.ToString();
                }
                return time.ToString("d");
            }
            string str = FormatDateCustom(CultureInfo.CurrentUICulture.DateTimeFormat.ShortDatePattern, this.month, this.day, this.year);
            if (this.type == MySqlDbType.Date)
            {
                return str;
            }
            DateTime time2 = new DateTime(1, 2, 3, this.hour, this.minute, this.second);
            return string.Format("{0} {1}", str, time2.ToLongTimeString());
        }

        public static explicit operator DateTime(MySqlDateTime val)
        {
            if (!val.IsValidDateTime)
            {
                return DateTime.MinValue;
            }
            return val.GetDateTime();
        }

        internal static void SetDSInfo(DataTable dsTable)
        {
            string[] strArray = new string[] { "DATE", "DATETIME", "TIMESTAMP" };
            MySqlDbType[] typeArray = new MySqlDbType[] { MySqlDbType.Date, MySqlDbType.DateTime, MySqlDbType.Timestamp };
            for (int i = 0; i < strArray.Length; i++)
            {
                DataRow row = dsTable.NewRow();
                row["TypeName"] = strArray[i];
                row["ProviderDbType"] = typeArray[i];
                row["ColumnSize"] = 0;
                row["CreateFormat"] = strArray[i];
                row["CreateParameters"] = null;
                row["DataType"] = "System.DateTime";
                row["IsAutoincrementable"] = false;
                row["IsBestMatch"] = true;
                row["IsCaseSensitive"] = false;
                row["IsFixedLength"] = true;
                row["IsFixedPrecisionScale"] = true;
                row["IsLong"] = false;
                row["IsNullable"] = true;
                row["IsSearchable"] = true;
                row["IsSearchableWithLike"] = false;
                row["IsUnsigned"] = false;
                row["MaximumScale"] = 0;
                row["MinimumScale"] = 0;
                row["IsConcurrencyType"] = DBNull.Value;
                row["IsLiteralSupported"] = false;
                row["LiteralPrefix"] = null;
                row["LiteralSuffix"] = null;
                row["NativeDataType"] = null;
                dsTable.Rows.Add(row);
            }
        }

        ulong IConvertible.ToUInt64(IFormatProvider provider)
        {
            return 0L;
        }

        sbyte IConvertible.ToSByte(IFormatProvider provider)
        {
            return 0;
        }

        double IConvertible.ToDouble(IFormatProvider provider)
        {
            return 0.0;
        }

        DateTime IConvertible.ToDateTime(IFormatProvider provider)
        {
            return this.GetDateTime();
        }

        float IConvertible.ToSingle(IFormatProvider provider)
        {
            return 0f;
        }

        bool IConvertible.ToBoolean(IFormatProvider provider)
        {
            return false;
        }

        int IConvertible.ToInt32(IFormatProvider provider)
        {
            return 0;
        }

        ushort IConvertible.ToUInt16(IFormatProvider provider)
        {
            return 0;
        }

        short IConvertible.ToInt16(IFormatProvider provider)
        {
            return 0;
        }

        string IConvertible.ToString(IFormatProvider provider)
        {
            return null;
        }

        byte IConvertible.ToByte(IFormatProvider provider)
        {
            return 0;
        }

        char IConvertible.ToChar(IFormatProvider provider)
        {
            return '\0';
        }

        long IConvertible.ToInt64(IFormatProvider provider)
        {
            return 0L;
        }

        TypeCode IConvertible.GetTypeCode()
        {
            return TypeCode.Empty;
        }

        decimal IConvertible.ToDecimal(IFormatProvider provider)
        {
            return 0M;
        }

        object IConvertible.ToType(Type conversionType, IFormatProvider provider)
        {
            return null;
        }

        uint IConvertible.ToUInt32(IFormatProvider provider)
        {
            return 0;
        }

        int IComparable.CompareTo(object obj)
        {
            MySqlDateTime time = (MySqlDateTime) obj;
            if (this.Year < time.Year)
            {
                return -1;
            }
            if (this.Year > time.Year)
            {
                return 1;
            }
            if (this.Month < time.Month)
            {
                return -1;
            }
            if (this.Month > time.Month)
            {
                return 1;
            }
            if (this.Day < time.Day)
            {
                return -1;
            }
            if (this.Day > time.Day)
            {
                return 1;
            }
            if (this.Hour < time.Hour)
            {
                return -1;
            }
            if (this.Hour > time.Hour)
            {
                return 1;
            }
            if (this.Minute < time.Minute)
            {
                return -1;
            }
            if (this.Minute > time.Minute)
            {
                return 1;
            }
            if (this.Second < time.Second)
            {
                return -1;
            }
            if (this.Second > time.Second)
            {
                return 1;
            }
            if (this.Millisecond < time.Millisecond)
            {
                return -1;
            }
            if (this.Millisecond > time.Millisecond)
            {
                return 1;
            }
            return 0;
        }
    }
}

