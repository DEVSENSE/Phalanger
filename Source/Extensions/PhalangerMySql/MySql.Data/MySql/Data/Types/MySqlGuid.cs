namespace MySql.Data.Types
{
    using MySql.Data.MySqlClient;
    using MySql.Data.MySqlClient.Properties;
    using System;
    using System.Data;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential)]
    internal struct MySqlGuid : IMySqlValue
    {
        private Guid mValue;
        private bool isNull;
        private byte[] bytes;
        private bool oldGuids;
        public MySqlGuid(byte[] buff)
        {
            this.oldGuids = false;
            this.mValue = new Guid(buff);
            this.isNull = false;
            this.bytes = buff;
        }

        public byte[] Bytes
        {
            get
            {
                return this.bytes;
            }
        }
        public bool OldGuids
        {
            get
            {
                return this.oldGuids;
            }
            set
            {
                this.oldGuids = value;
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
                return MySqlDbType.Guid;
            }
        }
        DbType IMySqlValue.DbType
        {
            get
            {
                return DbType.Guid;
            }
        }
        object IMySqlValue.Value
        {
            get
            {
                return this.mValue;
            }
        }
        public Guid Value
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
                return typeof(Guid);
            }
        }
        string IMySqlValue.MySqlTypeName
        {
            get
            {
                if (!this.OldGuids)
                {
                    return "CHAR(36)";
                }
                return "BINARY(16)";
            }
        }
        void IMySqlValue.WriteValue(MySqlPacket packet, bool binary, object val, int length)
        {
            Guid empty = Guid.Empty;
            string g = val as string;
            byte[] b = val as byte[];
            if (val is Guid)
            {
                empty = (Guid) val;
            }
            else
            {
                try
                {
                    if (g != null)
                    {
                        empty = new Guid(g);
                    }
                    else if (b != null)
                    {
                        empty = new Guid(b);
                    }
                }
                catch (Exception exception)
                {
                    throw new MySqlException(Resources.DataNotInSupportedFormat, exception);
                }
            }
            if (this.OldGuids)
            {
                this.WriteOldGuid(packet, empty, binary);
            }
            else
            {
                empty.ToString("D");
                if (binary)
                {
                    packet.WriteLenString(empty.ToString("D"));
                }
                else
                {
                    packet.WriteStringNoNull("'" + MySqlHelper.EscapeString(empty.ToString("D")) + "'");
                }
            }
        }

        private void WriteOldGuid(MySqlPacket packet, Guid guid, bool binary)
        {
            byte[] bytesToWrite = guid.ToByteArray();
            if (binary)
            {
                packet.WriteLength((long) bytesToWrite.Length);
                packet.Write(bytesToWrite);
            }
            else
            {
                packet.WriteStringNoNull("_binary ");
                packet.WriteByte(0x27);
                EscapeByteArray(bytesToWrite, bytesToWrite.Length, packet);
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

        private MySqlGuid ReadOldGuid(MySqlPacket packet, long length)
        {
            if (length == -1L)
            {
                length = packet.ReadFieldLength();
            }
            byte[] byteBuffer = new byte[length];
            packet.Read(byteBuffer, 0, (int) length);
            MySqlGuid guid = new MySqlGuid(byteBuffer);
            guid.OldGuids = this.OldGuids;
            return guid;
        }

        IMySqlValue IMySqlValue.ReadValue(MySqlPacket packet, long length, bool nullVal)
        {
            MySqlGuid guid = new MySqlGuid();
            guid.isNull = true;
            guid.OldGuids = this.OldGuids;
            if (!nullVal)
            {
                if (this.OldGuids)
                {
                    return this.ReadOldGuid(packet, length);
                }
                string g = string.Empty;
                if (length == -1L)
                {
                    g = packet.ReadLenString();
                }
                else
                {
                    g = packet.ReadString(length);
                }
                guid.mValue = new Guid(g);
                guid.isNull = false;
            }
            return guid;
        }

        void IMySqlValue.SkipValue(MySqlPacket packet)
        {
            int num = packet.ReadFieldLength();
            packet.Position += num;
        }

        public static void SetDSInfo(DataTable dsTable)
        {
            DataRow row = dsTable.NewRow();
            row["TypeName"] = "GUID";
            row["ProviderDbType"] = MySqlDbType.Guid;
            row["ColumnSize"] = 0;
            row["CreateFormat"] = "BINARY(16)";
            row["CreateParameters"] = null;
            row["DataType"] = "System.Guid";
            row["IsAutoincrementable"] = false;
            row["IsBestMatch"] = true;
            row["IsCaseSensitive"] = false;
            row["IsFixedLength"] = true;
            row["IsFixedPrecisionScale"] = true;
            row["IsLong"] = false;
            row["IsNullable"] = true;
            row["IsSearchable"] = false;
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

