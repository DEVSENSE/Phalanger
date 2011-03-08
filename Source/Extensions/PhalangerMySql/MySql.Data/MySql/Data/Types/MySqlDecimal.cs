namespace MySql.Data.Types
{
    using MySql.Data.MySqlClient;
    using System;
    using System.Data;
    using System.Globalization;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential)]
    public struct MySqlDecimal : IMySqlValue
    {
        private byte precision;
        private byte scale;
        private string mValue;
        private bool isNull;
        internal MySqlDecimal(bool isNull)
        {
            this.isNull = isNull;
            this.mValue = null;
            this.precision = (byte) (this.scale = 0);
        }

        internal MySqlDecimal(string val)
        {
            this.isNull = false;
            this.precision = (byte) (this.scale = 0);
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
                return MySqlDbType.Decimal;
            }
        }
        public byte Precision
        {
            get
            {
                return this.precision;
            }
            set
            {
                this.precision = value;
            }
        }
        public byte Scale
        {
            get
            {
                return this.scale;
            }
            set
            {
                this.scale = value;
            }
        }
        DbType IMySqlValue.DbType
        {
            get
            {
                return DbType.Decimal;
            }
        }
        object IMySqlValue.Value
        {
            get
            {
                return this.Value;
            }
        }
        public decimal Value
        {
            get
            {
                return Convert.ToDecimal(this.mValue, CultureInfo.InvariantCulture);
            }
        }
        public double ToDouble()
        {
            return double.Parse(this.mValue);
        }

        public override string ToString()
        {
            return this.mValue;
        }

        Type IMySqlValue.SystemType
        {
            get
            {
                return typeof(decimal);
            }
        }
        string IMySqlValue.MySqlTypeName
        {
            get
            {
                return "DECIMAL";
            }
        }
        void IMySqlValue.WriteValue(MySqlPacket packet, bool binary, object val, int length)
        {
            string s = ((val is decimal) ? ((decimal) val) : Convert.ToDecimal(val)).ToString(CultureInfo.InvariantCulture);
            if (binary)
            {
                packet.WriteLenString(s);
            }
            else
            {
                packet.WriteStringNoNull(s);
            }
        }

        IMySqlValue IMySqlValue.ReadValue(MySqlPacket packet, long length, bool nullVal)
        {
            if (nullVal)
            {
                return new MySqlDecimal(true);
            }
            string val = string.Empty;
            if (length == -1L)
            {
                val = packet.ReadLenString();
            }
            else
            {
                val = packet.ReadString(length);
            }
            return new MySqlDecimal(val);
        }

        void IMySqlValue.SkipValue(MySqlPacket packet)
        {
            int num = packet.ReadFieldLength();
            packet.Position += num;
        }

        internal static void SetDSInfo(DataTable dsTable)
        {
            DataRow row = dsTable.NewRow();
            row["TypeName"] = "DECIMAL";
            row["ProviderDbType"] = MySqlDbType.NewDecimal;
            row["ColumnSize"] = 0;
            row["CreateFormat"] = "DECIMAL({0},{1})";
            row["CreateParameters"] = "precision,scale";
            row["DataType"] = "System.Decimal";
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
}

