namespace MySql.Data.Common
{
    using MySql.Data.MySqlClient;
    using System;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Threading;

    internal class SharedMemoryStream : Stream
    {
        private const int BUFFERLENGTH = 0x3e84;
        private int bytesLeft;
        private EventWaitHandle clientRead;
        private EventWaitHandle clientWrote;
        private EventWaitHandle connectionClosed;
        private int connectNumber;
        private SharedMemory data;
        private string memoryName;
        private int position;
        private int readTimeout = -1;
        private EventWaitHandle serverRead;
        private EventWaitHandle serverWrote;
        private int writeTimeout = -1;

        public SharedMemoryStream(string memName)
        {
            this.memoryName = memName;
        }

        public override void Close()
        {
            if (this.connectionClosed != null)
            {
                if (!this.connectionClosed.WaitOne(0))
                {
                    this.connectionClosed.Set();
                    this.connectionClosed.Close();
                }
                this.connectionClosed = null;
                EventWaitHandle[] handleArray = new EventWaitHandle[] { this.serverRead, this.serverWrote, this.clientRead, this.clientWrote };
                for (int i = 0; i < handleArray.Length; i++)
                {
                    if (handleArray[i] != null)
                    {
                        handleArray[i].Close();
                    }
                }
                if (this.data != null)
                {
                    this.data.Dispose();
                    this.data = null;
                }
            }
        }

        public override void Flush()
        {
        }

        private void GetConnectNumber(uint timeOut)
        {
            EventWaitHandle handle;
            try
            {
                handle = EventWaitHandle.OpenExisting(this.memoryName + "_CONNECT_REQUEST");
            }
            catch (Exception)
            {
                string str = @"Global\" + this.memoryName;
                handle = EventWaitHandle.OpenExisting(str + "_CONNECT_REQUEST");
                this.memoryName = str;
            }
            EventWaitHandle handle2 = EventWaitHandle.OpenExisting(this.memoryName + "_CONNECT_ANSWER");
            using (SharedMemory memory = new SharedMemory(this.memoryName + "_CONNECT_DATA", (IntPtr) 4))
            {
                if (!handle.Set())
                {
                    throw new MySqlException("Failed to open shared memory connection");
                }
                if (!handle2.WaitOne((int) (timeOut * 0x3e8), false))
                {
                    throw new MySqlException("Timeout during connection");
                }
                this.connectNumber = Marshal.ReadInt32(memory.View);
            }
        }

        public void Open(uint timeOut)
        {
            EventWaitHandle connectionClosed = this.connectionClosed;
            this.GetConnectNumber(timeOut);
            this.SetupEvents();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int readTimeout = this.readTimeout;
            WaitHandle[] waitHandles = new WaitHandle[] { this.serverWrote, this.connectionClosed };
            LowResolutionStopwatch stopwatch = new LowResolutionStopwatch();
            while (this.bytesLeft == 0)
            {
                stopwatch.Start();
                int index = WaitHandle.WaitAny(waitHandles, readTimeout);
                stopwatch.Stop();
                if (index == 0x102)
                {
                    throw new TimeoutException("Timeout when reading from shared memory");
                }
                if (waitHandles[index] == this.connectionClosed)
                {
                    throw new MySqlException("Connection to server lost", true, null);
                }
                if (this.readTimeout != -1)
                {
                    readTimeout = this.readTimeout - ((int) stopwatch.ElapsedMilliseconds);
                    if (readTimeout < 0)
                    {
                        throw new TimeoutException("Timeout when reading from shared memory");
                    }
                }
                this.bytesLeft = Marshal.ReadInt32(this.data.View);
                this.position = 4;
            }
            int num3 = Math.Min(count, this.bytesLeft);
            long num4 = this.data.View.ToInt64() + this.position;
            int num5 = 0;
            while (num5 < num3)
            {
                buffer[offset + num5] = Marshal.ReadByte((IntPtr) (num4 + num5));
                num5++;
                this.position++;
            }
            this.bytesLeft -= num3;
            if (this.bytesLeft == 0)
            {
                this.clientRead.Set();
            }
            return num3;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException("SharedMemoryStream does not support seeking");
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException("SharedMemoryStream does not support seeking");
        }

        private void SetupEvents()
        {
            string str = this.memoryName + "_" + this.connectNumber;
            this.data = new SharedMemory(str + "_DATA", (IntPtr) 0x3e84);
            this.serverWrote = EventWaitHandle.OpenExisting(str + "_SERVER_WROTE");
            this.serverRead = EventWaitHandle.OpenExisting(str + "_SERVER_READ");
            this.clientWrote = EventWaitHandle.OpenExisting(str + "_CLIENT_WROTE");
            this.clientRead = EventWaitHandle.OpenExisting(str + "_CLIENT_READ");
            this.connectionClosed = EventWaitHandle.OpenExisting(str + "_CONNECTION_CLOSED");
            this.serverRead.Set();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            int num = count;
            int startIndex = offset;
            WaitHandle[] waitHandles = new WaitHandle[] { this.serverRead, this.connectionClosed };
            LowResolutionStopwatch stopwatch = new LowResolutionStopwatch();
            int writeTimeout = this.writeTimeout;
            while (num > 0)
            {
                stopwatch.Start();
                int index = WaitHandle.WaitAny(waitHandles, writeTimeout);
                stopwatch.Stop();
                if (waitHandles[index] == this.connectionClosed)
                {
                    throw new MySqlException("Connection to server lost", true, null);
                }
                if (index == 0x102)
                {
                    throw new TimeoutException("Timeout when reading from shared memory");
                }
                if (this.writeTimeout != -1)
                {
                    writeTimeout = this.writeTimeout - ((int) stopwatch.ElapsedMilliseconds);
                    if (writeTimeout < 0)
                    {
                        throw new TimeoutException("Timeout when writing to shared memory");
                    }
                }
                int val = Math.Min(num, 0x3e84);
                long num6 = this.data.View.ToInt64() + 4L;
                Marshal.WriteInt32(this.data.View, val);
                Marshal.Copy(buffer, startIndex, (IntPtr) num6, val);
                startIndex += val;
                num -= val;
                if (!this.clientWrote.Set())
                {
                    throw new MySqlException("Writing to shared memory failed");
                }
            }
        }

        public override bool CanRead
        {
            get
            {
                return true;
            }
        }

        public override bool CanSeek
        {
            get
            {
                return false;
            }
        }

        public override bool CanTimeout
        {
            get
            {
                return true;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return true;
            }
        }

        public override long Length
        {
            get
            {
                throw new NotSupportedException("SharedMemoryStream does not support seeking - length");
            }
        }

        public override long Position
        {
            get
            {
                throw new NotSupportedException("SharedMemoryStream does not support seeking - position");
            }
            set
            {
            }
        }

        public override int ReadTimeout
        {
            get
            {
                return this.readTimeout;
            }
            set
            {
                this.readTimeout = value;
            }
        }

        public override int WriteTimeout
        {
            get
            {
                return this.writeTimeout;
            }
            set
            {
                this.writeTimeout = value;
            }
        }
    }
}

