namespace MySql.Data.MySqlClient
{
    using MySql.Data.MySqlClient.Properties;
    using System;
    using System.IO;
    using System.Text;

    internal class MySqlStream
    {
        private MemoryStream bufferStream;
        private Stream inStream;
        private int maxBlockSize;
        private ulong maxPacketSize;
        private Stream outStream;
        private MySqlPacket packet;
        private byte[] packetHeader;
        private byte sequenceByte;
        private TimedStream timedStream;

        public MySqlStream(System.Text.Encoding encoding)
        {
            this.packetHeader = new byte[4];
            this.maxPacketSize = ulong.MaxValue;
            this.maxBlockSize = 0x7fffffff;
            this.packet = new MySqlPacket(encoding);
            this.bufferStream = new MemoryStream();
        }

        public MySqlStream(Stream baseStream, System.Text.Encoding encoding, bool compress) : this(encoding)
        {
            Stream timedStream;
            this.timedStream = new TimedStream(baseStream);
            if (compress)
            {
                timedStream = new CompressedStream(this.timedStream);
            }
            else
            {
                timedStream = this.timedStream;
            }
            this.inStream = new BufferedStream(timedStream);
            this.outStream = timedStream;
        }

        public void Close()
        {
            this.outStream.Close();
            this.inStream.Close();
            this.timedStream.Close();
        }

        public void LoadPacket()
        {
            try
            {
                int num2;
                this.packet.Length = 0;
                int offset = 0;
                do
                {
                    ReadFully(this.inStream, this.packetHeader, 0, 4);
                    this.sequenceByte = (byte) (this.packetHeader[3] + 1);
                    num2 = (this.packetHeader[0] + (this.packetHeader[1] << 8)) + (this.packetHeader[2] << 0x10);
                    this.packet.Length += num2;
                    ReadFully(this.inStream, this.packet.Buffer, offset, num2);
                    offset += num2;
                }
                while (num2 >= this.maxBlockSize);
                this.packet.Position = 0;
            }
            catch (IOException exception)
            {
                throw new MySqlException(Resources.ReadFromStreamFailed, true, exception);
            }
        }

        internal static void ReadFully(Stream stream, byte[] buffer, int offset, int count)
        {
            int num3;
            int num = 0;
            for (int i = count; i > 0; i -= num3)
            {
                num3 = stream.Read(buffer, offset + num, i);
                if (num3 == 0)
                {
                    throw new EndOfStreamException();
                }
                num += num3;
            }
        }

        public MySqlPacket ReadPacket()
        {
            this.LoadPacket();
            if (this.packet.Buffer[0] != 0xff)
            {
                return this.packet;
            }
            this.packet.ReadByte();
            int errno = this.packet.ReadInteger(2);
            string msg = this.packet.ReadString();
            if (msg.StartsWith("#"))
            {
                msg.Substring(1, 5);
                msg = msg.Substring(6);
            }
            throw new MySqlException(msg, errno);
        }

        public void ResetTimeout(int timeout)
        {
            this.timedStream.ResetTimeout(timeout);
        }

        public void SendEntirePacketDirectly(byte[] buffer, int count)
        {
            byte num;
            buffer[0] = (byte) (count & 0xff);
            buffer[1] = (byte) ((count >> 8) & 0xff);
            buffer[2] = (byte) ((count >> 0x10) & 0xff);
            this.sequenceByte = (byte) ((num = this.sequenceByte) + 1);
            buffer[3] = num;
            this.outStream.Write(buffer, 0, count + 4);
            this.outStream.Flush();
        }

        public void SendPacket(MySqlPacket packet)
        {
            int num3;
            byte[] buffer = packet.Buffer;
            int num = packet.Position - 4;
            if ((ulong)num > this.maxPacketSize)
            {
                throw new MySqlException(Resources.QueryTooLarge, 0x481);
            }
            for (int i = 0; num > 0; i += num3)
            {
                byte num4;
                num3 = (num > this.maxBlockSize) ? this.maxBlockSize : num;
                buffer[i] = (byte) (num3 & 0xff);
                buffer[i + 1] = (byte) ((num3 >> 8) & 0xff);
                buffer[i + 2] = (byte) ((num3 >> 0x10) & 0xff);
                this.sequenceByte = (byte) ((num4 = this.sequenceByte) + 1);
                buffer[i + 3] = num4;
                this.outStream.Write(buffer, i, num3 + 4);
                this.outStream.Flush();
                num -= num3;
            }
        }

        public System.Text.Encoding Encoding
        {
            get
            {
                return this.packet.Encoding;
            }
            set
            {
                this.packet.Encoding = value;
            }
        }

        public int MaxBlockSize
        {
            get
            {
                return this.maxBlockSize;
            }
            set
            {
                this.maxBlockSize = value;
            }
        }

        public ulong MaxPacketSize
        {
            get
            {
                return this.maxPacketSize;
            }
            set
            {
                this.maxPacketSize = value;
            }
        }

        public byte SequenceByte
        {
            get
            {
                return this.sequenceByte;
            }
            set
            {
                this.sequenceByte = value;
            }
        }
    }
}

