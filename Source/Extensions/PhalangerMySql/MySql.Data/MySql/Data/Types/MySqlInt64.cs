namespace MySql.Data.Types
{
    using MySql.Data.MySqlClient;
    using System;
    using System.Data;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential)]
    internal struct MySqlInt64 : IMySqlValue
    {
        private long mValue;
        private bool isNull;
        public MySqlInt64(bool isNull)
        {
            this.isNull = isNull;
            this.mValue = 0L;
        }

        public MySqlInt64(long val)
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
                return MySqlDbType.Int64;
            }
        }
        DbType IMySqlValue.DbType
        {
            get
            {
                return DbType.Int64;
            }
        }
        object IMySqlValue.Value
        {
            get
            {
                return this.mValue;
            }
        }
        public long Value
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
                return typeof(long);
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
            long v = (val is long) ? ((long) val) : Convert.ToInt64(val);
            if (binary)
            {
                packet.WriteInteger(v, 8);
            }
            else
            {
                packet.WriteStringNoNull(v.ToString());
            }
        }

        IMySqlValue IMySqlValue.ReadValue(MySqlPacket packet, long length, bool nullVal)
        {
            if (nullVal)
            {
                return new MySqlInt64(true);
            }
            if (length == -1L)
            {
                return new MySqlInt64((long) packet.ReadULong(8));
            }
            return new MySqlInt64(long.Parse(packet.ReadString(length)));
        }

        void IMySqlValue.SkipValue(MySqlPacket packet)
        {
            packet.Position += 8;
        }

        internal static void SetDSInfo(DataTable dsTable)
        {
            DataRow row = dsTable.NewRow();
            row["TypeName"] = "BIGINT";
            row["ProviderDbType"] = MySqlDbType.Int64;
            row["ColumnSize"] = 0;
            row["CreateFormat"] = "BIGINT";
            row["CreateParameters"] = null;
            row["DataType"] = "System.Int64";
            row["IsAutoincrementable"] = true;
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

