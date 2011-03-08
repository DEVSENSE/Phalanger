namespace MySql.Data.Types
{
    using MySql.Data.MySqlClient;
    using System;
    using System.Data;
    using System.Globalization;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential)]
    internal struct MySqlDouble : IMySqlValue
    {
        private double mValue;
        private bool isNull;
        public MySqlDouble(bool isNull)
        {
            this.isNull = isNull;
            this.mValue = 0.0;
        }

        public MySqlDouble(double val)
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
                return MySqlDbType.Double;
            }
        }
        DbType IMySqlValue.DbType
        {
            get
            {
                return DbType.Double;
            }
        }
        object IMySqlValue.Value
        {
            get
            {
                return this.mValue;
            }
        }
        public double Value
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
                return typeof(double);
            }
        }
        string IMySqlValue.MySqlTypeName
        {
            get
            {
                return "DOUBLE";
            }
        }
        void IMySqlValue.WriteValue(MySqlPacket packet, bool binary, object val, int length)
        {
            double num = (val is double) ? ((double) val) : Convert.ToDouble(val);
            if (binary)
            {
                packet.Write(BitConverter.GetBytes(num));
            }
            else
            {
                packet.WriteStringNoNull(num.ToString("R", CultureInfo.InvariantCulture));
            }
        }

        IMySqlValue IMySqlValue.ReadValue(MySqlPacket packet, long length, bool nullVal)
        {
            double minValue;
            if (nullVal)
            {
                return new MySqlDouble(true);
            }
            if (length == -1L)
            {
                byte[] byteBuffer = new byte[8];
                packet.Read(byteBuffer, 0, 8);
                return new MySqlDouble(BitConverter.ToDouble(byteBuffer, 0));
            }
            string s = packet.ReadString(length);
            try
            {
                minValue = double.Parse(s, CultureInfo.InvariantCulture);
            }
            catch (OverflowException)
            {
                if (s.StartsWith("-"))
                {
                    minValue = double.MinValue;
                }
                else
                {
                    minValue = double.MaxValue;
                }
            }
            return new MySqlDouble(minValue);
        }

        void IMySqlValue.SkipValue(MySqlPacket packet)
        {
            packet.Position += 8;
        }

        internal static void SetDSInfo(DataTable dsTable)
        {
            DataRow row = dsTable.NewRow();
            row["TypeName"] = "DOUBLE";
            row["ProviderDbType"] = MySqlDbType.Double;
            row["ColumnSize"] = 0;
            row["CreateFormat"] = "DOUBLE";
            row["CreateParameters"] = null;
            row["DataType"] = "System.Double";
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

