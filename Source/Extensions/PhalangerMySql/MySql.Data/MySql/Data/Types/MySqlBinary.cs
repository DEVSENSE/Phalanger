namespace MySql.Data.Types
{
    using MySql.Data.MySqlClient;
    using System;
    using System.Data;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential)]
    internal struct MySqlBinary : IMySqlValue
    {
        private MySqlDbType type;
        private byte[] mValue;
        private bool isNull;
        public MySqlBinary(MySqlDbType type, bool isNull)
        {
            this.type = type;
            this.isNull = isNull;
            this.mValue = null;
        }

        public MySqlBinary(MySqlDbType type, byte[] val)
        {
            this.type = type;
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
                return this.type;
            }
        }
        DbType IMySqlValue.DbType
        {
            get
            {
                return DbType.Binary;
            }
        }
        object IMySqlValue.Value
        {
            get
            {
                return this.mValue;
            }
        }
        public byte[] Value
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
                return typeof(byte[]);
            }
        }
        string IMySqlValue.MySqlTypeName
        {
            get
            {
                switch (this.type)
                {
                    case MySqlDbType.TinyBlob:
                        return "TINY_BLOB";

                    case MySqlDbType.MediumBlob:
                        return "MEDIUM_BLOB";

                    case MySqlDbType.LongBlob:
                        return "LONG_BLOB";
                }
                return "BLOB";
            }
        }
        void IMySqlValue.WriteValue(MySqlPacket packet, bool binary, object val, int length)
        {
            byte[] bytesToWrite = val as byte[];
            if (bytesToWrite == null)
            {
                char[] chars = val as char[];
                if (chars != null)
                {
                    bytesToWrite = packet.Encoding.GetBytes(chars);
                }
                else
                {
                    string s = val.ToString();
                    if (length == 0)
                    {
                        length = s.Length;
                    }
                    else
                    {
                        s = s.Substring(0, length);
                    }
                    bytesToWrite = packet.Encoding.GetBytes(s);
                }
            }
            if (length == 0)
            {
                length = bytesToWrite.Length;
            }
            if (bytesToWrite == null)
            {
                throw new MySqlException("Only byte arrays and strings can be serialized by MySqlBinary");
            }
            if (binary)
            {
                packet.WriteLength((long) length);
                packet.Write(bytesToWrite, 0, length);
            }
            else
            {
                packet.WriteStringNoNull("_binary ");
                packet.WriteByte(0x27);
                EscapeByteArray(bytesToWrite, length, packet);
                packet.WriteByte(0x27);
            }
        }

        private static void EscapeByteArray(byte[] bytes, int length, MySqlPacket packet)
        {
            for (int i = 0; i < length; i++)
            {
                byte b = bytes[i];
                switch (b)
                {
                    case 0:
                        packet.WriteByte(0x5c);
                        packet.WriteByte(0x30);
                        break;

                    case 0x5c:
                    case 0x27:
                    case 0x22:
                        packet.WriteByte(0x5c);
                        packet.WriteByte(b);
                        break;

                    default:
                        packet.WriteByte(b);
                        break;
                }
            }
        }

        IMySqlValue IMySqlValue.ReadValue(MySqlPacket packet, long length, bool nullVal)
        {
            if (nullVal)
            {
                return new MySqlBinary(this.type, true);
            }
            if (length == -1L)
            {
                length = packet.ReadFieldLength();
            }
            byte[] byteBuffer = new byte[length];
            packet.Read(byteBuffer, 0, (int) length);
            return new MySqlBinary(this.type, byteBuffer);
        }

        void IMySqlValue.SkipValue(MySqlPacket packet)
        {
            int num = packet.ReadFieldLength();
            packet.Position += num;
        }

        public static void SetDSInfo(DataTable dsTable)
        {
            string[] strArray = new string[] { "BLOB", "TINYBLOB", "MEDIUMBLOB", "LONGBLOB", "BINARY", "VARBINARY" };
            MySqlDbType[] typeArray = new MySqlDbType[] { MySqlDbType.Blob, MySqlDbType.TinyBlob, MySqlDbType.MediumBlob, MySqlDbType.LongBlob, MySqlDbType.Binary, MySqlDbType.VarBinary };
            long[] numArray = new long[] { 0xffffL, 0xffL, 0xffffffL, 0xffffffffL, 0xffL, 0xffffL };
            string[] strArray5 = new string[6];
            strArray5[4] = "binary({0})";
            strArray5[5] = "varbinary({0})";
            string[] strArray2 = strArray5;
            string[] strArray6 = new string[6];
            strArray6[4] = "length";
            strArray6[5] = "length";
            string[] strArray3 = strArray6;
            for (int i = 0; i < strArray.Length; i++)
            {
                DataRow row = dsTable.NewRow();
                row["TypeName"] = strArray[i];
                row["ProviderDbType"] = typeArray[i];
                row["ColumnSize"] = numArray[i];
                row["CreateFormat"] = strArray2[i];
                row["CreateParameters"] = strArray3[i];
                row["DataType"] = "System.Byte[]";
                row["IsAutoincrementable"] = false;
                row["IsBestMatch"] = true;
                row["IsCaseSensitive"] = false;
                row["IsFixedLength"] = i >= 4;
                row["IsFixedPrecisionScale"] = false;
                row["IsLong"] = numArray[i] > 0xffL;
                row["IsNullable"] = true;
                row["IsSearchable"] = false;
                row["IsSearchableWithLike"] = false;
                row["IsUnsigned"] = DBNull.Value;
                row["MaximumScale"] = DBNull.Value;
                row["MinimumScale"] = DBNull.Value;
                row["IsConcurrencyType"] = DBNull.Value;
                row["IsLiteralSupported"] = false;
                row["LiteralPrefix"] = "0x";
                row["LiteralSuffix"] = DBNull.Value;
                row["NativeDataType"] = DBNull.Value;
                dsTable.Rows.Add(row);
            }
        }
    }
}

