namespace MySql.Data.MySqlClient
{
    using MySql.Data.Common;
    using MySql.Data.MySqlClient.Properties;
    using System;
    using System.IO;
    using zlib;

    internal class CompressedStream : Stream
    {
        private Stream baseStream;
        private MemoryStream cache;
        private byte[] inBuffer;
        private WeakReference inBufferRef;
        private int inPos;
        private byte[] lengthBytes;
        private byte[] localByte;
        private int maxInPos;
        private ZInputStream zInStream;

        public CompressedStream(Stream baseStream)
        {
            this.baseStream = baseStream;
            this.localByte = new byte[1];
            this.lengthBytes = new byte[7];
            this.cache = new MemoryStream();
            this.inBufferRef = new WeakReference(this.inBuffer, false);
        }

        public override void Close()
        {
            this.baseStream.Close();
            base.Close();
        }

        private void CompressAndSendCache()
        {
            long num;
            long num2;
            MemoryStream cache;
            byte[] buffer = this.cache.GetBuffer();
            byte num3 = buffer[3];
            buffer[3] = 0;
            MemoryStream stream = this.CompressCache();
            if (stream == null)
            {
                num = this.cache.Length;
                num2 = 0L;
                cache = this.cache;
            }
            else
            {
                num = stream.Length;
                num2 = this.cache.Length;
                cache = stream;
            }
            long length = cache.Length;
            int count = ((int) length) + 7;
            cache.SetLength((long) count);
            byte[] sourceArray = cache.GetBuffer();
            Array.Copy(sourceArray, 0, sourceArray, 7, (int) length);
            sourceArray[0] = (byte) (num & 0xffL);
            sourceArray[1] = (byte) ((num >> 8) & 0xffL);
            sourceArray[2] = (byte) ((num >> 0x10) & 0xffL);
            sourceArray[3] = num3;
            sourceArray[4] = (byte) (num2 & 0xffL);
            sourceArray[5] = (byte) ((num2 >> 8) & 0xffL);
            sourceArray[6] = (byte) ((num2 >> 0x10) & 0xffL);
            this.baseStream.Write(sourceArray, 0, count);
            this.baseStream.Flush();
            this.cache.SetLength(0L);
        }

        private MemoryStream CompressCache()
        {
            if (this.cache.Length < 50L)
            {
                return null;
            }
            byte[] buffer = this.cache.GetBuffer();
            MemoryStream stream = new MemoryStream();
            ZOutputStream stream2 = new ZOutputStream(stream, -1);
            stream2.Write(buffer, 0, (int) this.cache.Length);
            stream2.finish();
            if (stream.Length >= this.cache.Length)
            {
                return null;
            }
            return stream;
        }

        public override void Flush()
        {
            if (this.InputDone())
            {
                this.CompressAndSendCache();
            }
        }

        private bool InputDone()
        {
            if (this.cache.Length < 4L)
            {
                return false;
            }
            byte[] buffer = this.cache.GetBuffer();
            int num = (buffer[0] + (buffer[1] << 8)) + (buffer[2] << 0x10);
            if (this.cache.Length < (num + 4))
            {
                return false;
            }
            return true;
        }

        private void PrepareNextPacket()
        {
            MySqlStream.ReadFully(this.baseStream, this.lengthBytes, 0, 7);
            int len = (this.lengthBytes[0] + (this.lengthBytes[1] << 8)) + (this.lengthBytes[2] << 0x10);
            int num2 = (this.lengthBytes[4] + (this.lengthBytes[5] << 8)) + (this.lengthBytes[6] << 0x10);
            if (num2 == 0)
            {
                num2 = len;
                this.zInStream = null;
            }
            else
            {
                this.ReadNextPacket(len);
                MemoryStream stream = new MemoryStream(this.inBuffer);
                this.zInStream = new ZInputStream(stream);
                this.zInStream.maxInput = len;
            }
            this.inPos = 0;
            this.maxInPos = num2;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int num2;
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer", Resources.BufferCannotBeNull);
            }
            if ((offset < 0) || (offset >= buffer.Length))
            {
                throw new ArgumentOutOfRangeException("offset", Resources.OffsetMustBeValid);
            }
            if ((offset + count) > buffer.Length)
            {
                throw new ArgumentException(Resources.BufferNotLargeEnough, "buffer");
            }
            if (this.inPos == this.maxInPos)
            {
                this.PrepareNextPacket();
            }
            int len = Math.Min(count, this.maxInPos - this.inPos);
            if (this.zInStream != null)
            {
                num2 = this.zInStream.read(buffer, offset, len);
            }
            else
            {
                num2 = this.baseStream.Read(buffer, offset, len);
            }
            this.inPos += num2;
            if (this.inPos == this.maxInPos)
            {
                this.zInStream = null;
                if (!Platform.IsMono())
                {
                    this.inBufferRef = new WeakReference(this.inBuffer, false);
                    this.inBuffer = null;
                }
            }
            return num2;
        }

        public override int ReadByte()
        {
            try
            {
                this.Read(this.localByte, 0, 1);
                return this.localByte[0];
            }
            catch (EndOfStreamException)
            {
                return -1;
            }
        }

        private void ReadNextPacket(int len)
        {
            if (!Platform.IsMono())
            {
                this.inBuffer = this.inBufferRef.Target as byte[];
            }
            if ((this.inBuffer == null) || (this.inBuffer.Length < len))
            {
                this.inBuffer = new byte[len];
            }
            MySqlStream.ReadFully(this.baseStream, this.inBuffer, 0, len);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return this.baseStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException(Resources.CSNoSetLength);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            this.cache.Write(buffer, offset, count);
        }

        public override void WriteByte(byte value)
        {
            this.cache.WriteByte(value);
        }

        public override bool CanRead
        {
            get
            {
                return this.baseStream.CanRead;
            }
        }

        public override bool CanSeek
        {
            get
            {
                return this.baseStream.CanSeek;
            }
        }

        public override bool CanTimeout
        {
            get
            {
                return this.baseStream.CanTimeout;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return this.baseStream.CanWrite;
            }
        }

        public override long Length
        {
            get
            {
                return this.baseStream.Length;
            }
        }

        public override long Position
        {
            get
            {
                return this.baseStream.Position;
            }
            set
            {
                this.baseStream.Position = value;
            }
        }

        public override int ReadTimeout
        {
            get
            {
                return this.baseStream.ReadTimeout;
            }
            set
            {
                this.baseStream.ReadTimeout = value;
            }
        }

        public override int WriteTimeout
        {
            get
            {
                return this.baseStream.WriteTimeout;
            }
            set
            {
                this.baseStream.WriteTimeout = value;
            }
        }
    }
}

