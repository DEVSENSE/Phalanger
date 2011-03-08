namespace MySql.Data.Types
{
    using MySql.Data.MySqlClient;
    using System;
    using System.Data;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential)]
    internal struct MySqlUInt64 : IMySqlValue
    {
        private ulong mValue;
        private bool isNull;
        public MySqlUInt64(bool isNull)
        {
            this.isNull = isNull;
            this.mValue = 0L;
        }

        public MySqlUInt64(ulong val)
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
                return MySqlDbType.UInt64;
            }
        }
        DbType IMySqlValue.DbType
        {
            get
            {
                return DbType.UInt64;
            }
        }
        object IMySqlValue.Value
        {
            get
            {
                return this.mValue;
            }
        }
        public ulong Value
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
                return typeof(ulong);
            }
        }
        string IMySqlValue.MySqlTypeName
        {
            get
            {
                return "BIGINT";
            }
        }
        void IMySqlValue.WriteValue(MySqlPacket packet, bool binary, object val, int length)
        {
            ulong num = (val is ulong) ? ((ulong) val) : Convert.ToUInt64(val);
            if (binary)
            {
                packet.WriteInteger((long) num, 8);
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
                return new MySqlUInt64(true);
            }
            if (length == -1L)
            {
                return new MySqlUInt64(packet.ReadULong(8));
            }
            return new MySqlUInt64(ulong.Parse(packet.ReadString(length)));
        }

        void IMySqlValue.SkipValue(MySqlPacket packet)
        {
            packet.Position += 8;
        }

        internal static void SetDSInfo(DataTable dsTable)
        {
            DataRow row = dsTable.NewRow();
            row["TypeName"] = "BIGINT";
            row["ProviderDbType"] = MySqlDbType.UInt64;
            row["ColumnSize"] = 0;
            row["CreateFormat"] = "BIGINT UNSIGNED";
            row["CreateParameters"] = null;
            row["DataType"] = "System.UInt64";
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

