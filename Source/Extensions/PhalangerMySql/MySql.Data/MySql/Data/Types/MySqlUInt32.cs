namespace MySql.Data.Types
{
    using MySql.Data.MySqlClient;
    using System;
    using System.Data;
    using System.Globalization;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential)]
    internal struct MySqlUInt32 : IMySqlValue
    {
        private uint mValue;
        private bool isNull;
        private bool is24Bit;
        private MySqlUInt32(MySqlDbType type)
        {
            this.is24Bit = type == MySqlDbType.Int24;
            this.isNull = true;
            this.mValue = 0;
        }

        public MySqlUInt32(MySqlDbType type, bool isNull) : this(type)
        {
            this.isNull = isNull;
        }

        public MySqlUInt32(MySqlDbType type, uint val) : this(type)
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
                return MySqlDbType.UInt32;
            }
        }
        DbType IMySqlValue.DbType
        {
            get
            {
                return DbType.UInt32;
            }
        }
        object IMySqlValue.Value
        {
            get
            {
                return this.mValue;
            }
        }
        public uint Value
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
                return typeof(uint);
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
        void IMySqlValue.WriteValue(MySqlPacket packet, bool binary, object v, int length)
        {
            uint num = (v is uint) ? ((uint) v) : Convert.ToUInt32(v);
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
                return new MySqlUInt32(((IMySqlValue) this).MySqlDbType, true);
            }
            if (length == -1L)
            {
                return new MySqlUInt32(((IMySqlValue) this).MySqlDbType, (uint) packet.ReadInteger(4));
            }
            return new MySqlUInt32(((IMySqlValue) this).MySqlDbType, uint.Parse(packet.ReadString(length), NumberStyles.Any, CultureInfo.InvariantCulture));
        }

        void IMySqlValue.SkipValue(MySqlPacket packet)
        {
            packet.Position += 4;
        }

        internal static void SetDSInfo(DataTable dsTable)
        {
            string[] strArray = new string[] { "MEDIUMINT", "INT" };
            MySqlDbType[] typeArray = new MySqlDbType[] { MySqlDbType.UInt24, MySqlDbType.UInt32 };
            for (int i = 0; i < strArray.Length; i++)
            {
                DataRow row = dsTable.NewRow();
                row["TypeName"] = strArray[i];
                row["ProviderDbType"] = typeArray[i];
                row["ColumnSize"] = 0;
                row["CreateFormat"] = strArray[i] + " UNSIGNED";
                row["CreateParameters"] = null;
                row["DataType"] = "System.UInt32";
                row["IsAutoincrementable"] = true;
                row["IsBestMatch"] = true;
                row["IsCaseSensitive"] = false;
                row["IsFixedLength"] = true;
                row["IsFixedPrecisionScale"] = true;
                row["IsLong"] = false;
                row["IsNullable"] = true;
                row["IsSearchable"] = true;
                row["IsSearchableWithLike"] = false;
                row["IsUnsigned"] = true;
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

