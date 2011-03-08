namespace MySql.Data.Types
{
    using MySql.Data.MySqlClient;
    using System;
    using System.Data;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential)]
    internal struct MySqlUByte : IMySqlValue
    {
        private byte mValue;
        private bool isNull;
        public MySqlUByte(bool isNull)
        {
            this.isNull = isNull;
            this.mValue = 0;
        }

        public MySqlUByte(byte val)
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
                return MySqlDbType.UByte;
            }
        }
        DbType IMySqlValue.DbType
        {
            get
            {
                return DbType.Byte;
            }
        }
        object IMySqlValue.Value
        {
            get
            {
                return this.mValue;
            }
        }
        public byte Value
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
                return typeof(byte);
            }
        }
        string IMySqlValue.MySqlTypeName
        {
            get
            {
                return "TINYINT";
            }
        }
        void IMySqlValue.WriteValue(MySqlPacket packet, bool binary, object val, int length)
        {
            byte b = (val is byte) ? ((byte) val) : Convert.ToByte(val);
            if (binary)
            {
                packet.WriteByte(b);
            }
            else
            {
                packet.WriteStringNoNull(b.ToString());
            }
        }

        IMySqlValue IMySqlValue.ReadValue(MySqlPacket packet, long length, bool nullVal)
        {
            if (nullVal)
            {
                return new MySqlUByte(true);
            }
            if (length == -1L)
            {
                return new MySqlUByte(packet.ReadByte());
            }
            return new MySqlUByte(byte.Parse(packet.ReadString(length)));
        }

        void IMySqlValue.SkipValue(MySqlPacket packet)
        {
            packet.ReadByte();
        }

        internal static void SetDSInfo(DataTable dsTable)
        {
            DataRow row = dsTable.NewRow();
            row["TypeName"] = "TINY INT";
            row["ProviderDbType"] = MySqlDbType.UByte;
            row["ColumnSize"] = 0;
            row["CreateFormat"] = "TINYINT UNSIGNED";
            row["CreateParameters"] = null;
            row["DataType"] = "System.Byte";
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

