namespace MySql.Data.MySqlClient
{
    using MySql.Data.Common;
    using MySql.Data.MySqlClient.Properties;
    using System;
    using System.IO;
    using System.Text;

    internal class MySqlPacket
    {
        private MemoryStream buffer;
        private System.Text.Encoding encoding;
        private byte[] tempBuffer;
        private DBVersion version;

        private MySqlPacket()
        {
            this.tempBuffer = new byte[0x100];
            this.buffer = new MemoryStream(5);
            this.Clear();
        }

        public MySqlPacket(MemoryStream stream) : this()
        {
            this.buffer = stream;
        }

        public MySqlPacket(System.Text.Encoding enc) : this()
        {
            this.Encoding = enc;
        }

        public void Clear()
        {
            this.Position = 4;
        }

        public int Read(byte[] byteBuffer, int offset, int count)
        {
            return this.buffer.Read(byteBuffer, offset, count);
        }

        public int Read3ByteInt()
        {
            int num = 0;
            int position = (int) this.buffer.Position;
            byte[] buffer = this.buffer.GetBuffer();
            int num3 = 0;
            for (int i = 0; i < 3; i++)
            {
                num |= buffer[position++] << num3;
                num3 += 8;
            }
            this.buffer.Position += 3L;
            return num;
        }

        public string ReadAsciiString(long length)
        {
            if (length == 0L)
            {
                return string.Empty;
            }
            this.Read(this.tempBuffer, 0, (int) length);
            return System.Text.Encoding.ASCII.GetString(this.tempBuffer, 0, (int) length);
        }

        public ulong ReadBitValue(int numbytes)
        {
            ulong num = 0L;
            int position = (int) this.buffer.Position;
            byte[] buffer = this.buffer.GetBuffer();
            int num3 = 0;
            for (int i = 0; i < numbytes; i++)
            {
                num = num << num3;
                num |= buffer[position++];
                num3 = 8;
            }
            this.buffer.Position += numbytes;
            return num;
        }

        public byte ReadByte()
        {
            return (byte) this.buffer.ReadByte();
        }

        public int ReadFieldLength()
        {
            byte num = this.ReadByte();
            switch (num)
            {
                case 0xfb:
                    return -1;

                case 0xfc:
                    return this.ReadInteger(2);

                case 0xfd:
                    return this.ReadInteger(3);

                case 0xfe:
                    return this.ReadInteger(8);
            }
            return num;
        }

        public int ReadInteger(int numbytes)
        {
            if (numbytes == 3)
            {
                return this.Read3ByteInt();
            }
            return (int) this.ReadLong(numbytes);
        }

        public string ReadLenString()
        {
            long length = this.ReadPackedInteger();
            return this.ReadString(length);
        }

        public long ReadLong(int numbytes)
        {
            byte[] buffer = this.buffer.GetBuffer();
            int position = (int) this.buffer.Position;
            this.buffer.Position += numbytes;
            switch (numbytes)
            {
                case 2:
                    return (long) BitConverter.ToUInt16(buffer, position);

                case 4:
                    return (long) BitConverter.ToUInt32(buffer, position);

                case 8:
                    return BitConverter.ToInt64(buffer, position);
            }
            throw new NotSupportedException("Only byte lengths of 2, 4, or 8 are supported");
        }

        public int ReadNBytes()
        {
            byte numbytes = this.ReadByte();
            if ((numbytes < 1) || (numbytes > 4))
            {
                throw new MySqlException(Resources.IncorrectTransmission);
            }
            return this.ReadInteger(numbytes);
        }

        public int ReadPackedInteger()
        {
            byte num = this.ReadByte();
            switch (num)
            {
                case 0xfb:
                    return -1;

                case 0xfc:
                    return this.ReadInteger(2);

                case 0xfd:
                    return this.ReadInteger(3);

                case 0xfe:
                    return this.ReadInteger(4);
            }
            return num;
        }

        public string ReadString()
        {
            byte[] bytes = this.buffer.GetBuffer();
            int position = (int) this.buffer.Position;
            while (((position < ((int) this.buffer.Length)) && (bytes[position] != 0)) && (bytes[position] != -1))
            {
                position++;
            }
            string str = this.encoding.GetString(bytes, (int) this.buffer.Position, position - ((int) this.buffer.Position));
            this.buffer.Position = position + 1;
            return str;
        }

        public string ReadString(long length)
        {
            if (length == 0L)
            {
                return string.Empty;
            }
            if ((this.tempBuffer == null) || (length > this.tempBuffer.Length))
            {
                this.tempBuffer = new byte[length];
            }
            this.Read(this.tempBuffer, 0, (int) length);
            return this.encoding.GetString(this.tempBuffer, 0, (int) length);
        }

        public ulong ReadULong(int numbytes)
        {
            byte[] buffer = this.buffer.GetBuffer();
            int position = (int) this.buffer.Position;
            this.buffer.Position += numbytes;
            switch (numbytes)
            {
                case 2:
                    return (ulong) BitConverter.ToUInt16(buffer, position);

                case 4:
                    return (ulong) BitConverter.ToUInt32(buffer, position);

                case 8:
                    return BitConverter.ToUInt64(buffer, position);
            }
            throw new NotSupportedException("Only byte lengths of 2, 4, or 8 are supported");
        }

        public void Write(byte[] bytesToWrite)
        {
            this.Write(bytesToWrite, 0, bytesToWrite.Length);
        }

        public void Write(byte[] bytesToWrite, int offset, int countToWrite)
        {
            this.buffer.Write(bytesToWrite, offset, countToWrite);
        }

        public void WriteByte(byte b)
        {
            this.buffer.WriteByte(b);
        }

        public void WriteInteger(long v, int numbytes)
        {
            long num = v;
            for (int i = 0; i < numbytes; i++)
            {
                this.tempBuffer[i] = (byte) (num & 0xffL);
                num = num >> 8;
            }
            this.Write(this.tempBuffer, 0, numbytes);
        }

        public void WriteLength(long length)
        {
            if (length < 0xfbL)
            {
                this.WriteByte((byte) length);
            }
            else if (length < 0x10000L)
            {
                this.WriteByte(0xfc);
                this.WriteInteger(length, 2);
            }
            else if (length < 0x1000000L)
            {
                this.WriteByte(0xfd);
                this.WriteInteger(length, 3);
            }
            else
            {
                this.WriteByte(0xfe);
                this.WriteInteger(length, 4);
            }
        }

        public void WriteLenString(string s)
        {
            byte[] bytes = this.encoding.GetBytes(s);
            this.WriteLength((long) bytes.Length);
            this.Write(bytes, 0, bytes.Length);
        }

        public void WriteString(string v)
        {
            this.WriteStringNoNull(v);
            this.WriteByte(0);
        }

        public void WriteStringNoNull(string v)
        {
            byte[] bytes = this.encoding.GetBytes(v);
            this.Write(bytes, 0, bytes.Length);
        }

        public byte[] Buffer
        {
            get
            {
                return this.buffer.GetBuffer();
            }
        }

        public System.Text.Encoding Encoding
        {
            get
            {
                return this.encoding;
            }
            set
            {
                this.encoding = value;
            }
        }

        public bool HasMoreData
        {
            get
            {
                return (this.buffer.Position < this.buffer.Length);
            }
        }

        public bool IsLastPacket
        {
            get
            {
                return ((this.buffer.GetBuffer()[0] == 0xfe) && (this.Length <= 5));
            }
        }

        public int Length
        {
            get
            {
                return (int) this.buffer.Length;
            }
            set
            {
                this.buffer.SetLength((long) value);
            }
        }

        public int Position
        {
            get
            {
                return (int) this.buffer.Position;
            }
            set
            {
                this.buffer.Position = value;
            }
        }

        public DBVersion Version
        {
            get
            {
                return this.version;
            }
            set
            {
                this.version = value;
            }
        }
    }
}

