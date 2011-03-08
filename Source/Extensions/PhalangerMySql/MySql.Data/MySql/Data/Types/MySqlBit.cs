namespace MySql.Data.Types
{
    using MySql.Data.MySqlClient;
    using System;
    using System.Data;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential)]
    internal struct MySqlBit : IMySqlValue
    {
        private ulong mValue;
        private bool isNull;
        private bool readAsString;
        public MySqlBit(bool isnull)
        {
            this.mValue = 0L;
            this.isNull = isnull;
            this.readAsString = false;
        }

        public bool ReadAsString
        {
            get
            {
                return this.readAsString;
            }
            set
            {
                this.readAsString = value;
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
                return MySqlDbType.Bit;
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
                return "BIT";
            }
        }
        public void WriteValue(MySqlPacket packet, bool binary, object value, int length)
        {
            ulong num = (value is ulong) ? ((ulong) value) : Convert.ToUInt64(value);
            if (binary)
            {
                packet.WriteInteger((long) num, 8);
            }
            else
            {
                packet.WriteStringNoNull(num.ToString());
            }
        }

        public IMySqlValue ReadValue(MySqlPacket packet, long length, bool isNull)
        {
            this.isNull = isNull;
            if (!isNull)
            {
                if (length == -1L)
                {
                    length = packet.ReadFieldLength();
                }
                if (this.ReadAsString)
                {
                    this.mValue = ulong.Parse(packet.ReadString(length));
                }
                else
                {
                    this.mValue = packet.ReadBitValue((int) length);
                }
            }
            return this;
        }

        public void SkipValue(MySqlPacket packet)
        {
            int num = packet.ReadFieldLength();
            packet.Position += num;
        }

        public static void SetDSInfo(DataTable dsTable)
        {
            DataRow row = dsTable.NewRow();
            row["TypeName"] = "BIT";
            row["ProviderDbType"] = MySqlDbType.Bit;
            row["ColumnSize"] = 0x40;
            row["CreateFormat"] = "BIT";
            row["CreateParameters"] = DBNull.Value;
            row["DataType"] = typeof(ulong).ToString();
            row["IsAutoincrementable"] = false;
            row["IsBestMatch"] = true;
            row["IsCaseSensitive"] = false;
            row["IsFixedLength"] = false;
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
            row["LiteralPrefix"] = DBNull.Value;
            row["LiteralSuffix"] = DBNull.Value;
            row["NativeDataType"] = DBNull.Value;
            dsTable.Rows.Add(row);
        }
    }
}

