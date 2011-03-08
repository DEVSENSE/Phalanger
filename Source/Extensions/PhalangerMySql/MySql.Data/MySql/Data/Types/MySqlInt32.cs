namespace MySql.Data.Types
{
    using MySql.Data.MySqlClient;
    using System;
    using System.Data;
    using System.Globalization;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential)]
    internal struct MySqlInt32 : IMySqlValue
    {
        private int mValue;
        private bool isNull;
        private bool is24Bit;
        private MySqlInt32(MySqlDbType type)
        {
            this.is24Bit = type == MySqlDbType.Int24;
            this.isNull = true;
            this.mValue = 0;
        }

        public MySqlInt32(MySqlDbType type, bool isNull) : this(type)
        {
            this.isNull = isNull;
        }

        public MySqlInt32(MySqlDbType type, int val) : this(type)
        {
            this.isNull = false;
            this.mValue = val;
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
                return MySqlDbType.Int32;
            }
        }
        DbType IMySqlValue.DbType
        {
            get
            {
                return DbType.Int32;
            }
        }
        object IMySqlValue.Value
        {
            get
            {
                return this.mValue;
            }
        }
        public int Value
        {
            get
            {
                return this.mValue;
            }
        }
        Type IMySqlValue.SystemType
        {
            get
            {
                return typeof(int);
            }
        }
        string IMySqlValue.MySqlTypeName
        {
            get
            {
                if (!this.is24Bit)
                {
                    return "INT";
                }
                return "MEDIUMINT";
            }
        }
        void IMySqlValue.WriteValue(MySqlPacket packet, bool binary, object val, int length)
        {
            int num = (val is int) ? ((int) val) : Convert.ToInt32(val);
            if (binary)
            {
                packet.WriteInteger((long) num, this.is24Bit ? 3 : 4);
            }
            else
            {
                packet.WriteStringNoNull(num.ToString());
            }
        }

        IMySqlValue IMySqlValue.ReadValue(MySqlPacket packet, long length, bool nullVal)
        {
            if (nullVal)
            {
                return new MySqlInt32(((IMySqlValue) this).MySqlDbType, true);
            }
            if (length == -1L)
            {
                return new MySqlInt32(((IMySqlValue) this).MySqlDbType, packet.ReadInteger(4));
            }
            return new MySqlInt32(((IMySqlValue) this).MySqlDbType, int.Parse(packet.ReadString(length), CultureInfo.InvariantCulture));
        }

        void IMySqlValue.SkipValue(MySqlPacket packet)
        {
            packet.Position += 4;
        }

        internal static void SetDSInfo(DataTable dsTable)
        {
            string[] strArray = new string[] { "INT", "YEAR", "MEDIUMINT" };
            MySqlDbType[] typeArray = new MySqlDbType[] { MySqlDbType.Int32, MySqlDbType.Year, MySqlDbType.Int24 };
            for (int i = 0; i < strArray.Length; i++)
            {
                DataRow row = dsTable.NewRow();
                row["TypeName"] = strArray[i];
                row["ProviderDbType"] = typeArray[i];
                row["ColumnSize"] = 0;
                row["CreateFormat"] = strArray[i];
                row["CreateParameters"] = null;
                row["DataType"] = "System.Int32";
                row["IsAutoincrementable"] = typeArray[i] != MySqlDbType.Year;
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
    }
}

