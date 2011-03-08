namespace MySql.Data.Types
{
    using MySql.Data.MySqlClient;
    using System;
    using System.Data;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential)]
    internal struct MySqlInt16 : IMySqlValue
    {
        private short mValue;
        private bool isNull;
        public MySqlInt16(bool isNull)
        {
            this.isNull = isNull;
            this.mValue = 0;
        }

        public MySqlInt16(short val)
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
                return MySqlDbType.Int16;
            }
        }
        DbType IMySqlValue.DbType
        {
            get
            {
                return DbType.Int16;
            }
        }
        object IMySqlValue.Value
        {
            get
            {
                return this.mValue;
            }
        }
        public short Value
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
                return typeof(short);
            }
        }
        string IMySqlValue.MySqlTypeName
        {
            get
            {
                return "SMALLINT";
            }
        }
        void IMySqlValue.WriteValue(MySqlPacket packet, bool binary, object val, int length)
        {
            int num = (val is int) ? ((int) val) : Convert.ToInt32(val);
            if (binary)
            {
                packet.WriteInteger((long) num, 2);
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
                return new MySqlInt16(true);
            }
            if (length == -1L)
            {
                return new MySqlInt16((short) packet.ReadInteger(2));
            }
            return new MySqlInt16(short.Parse(packet.ReadString(length)));
        }

        void IMySqlValue.SkipValue(MySqlPacket packet)
        {
            packet.Position += 2;
        }

        internal static void SetDSInfo(DataTable dsTable)
        {
            DataRow row = dsTable.NewRow();
            row["TypeName"] = "SMALLINT";
            row["ProviderDbType"] = MySqlDbType.Int16;
            row["ColumnSize"] = 0;
            row["CreateFormat"] = "SMALLINT";
            row["CreateParameters"] = null;
            row["DataType"] = "System.Int16";
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

