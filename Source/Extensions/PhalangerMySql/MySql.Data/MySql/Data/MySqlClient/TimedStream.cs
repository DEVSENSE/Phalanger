namespace MySql.Data.MySqlClient
{
    using MySql.Data.Common;
    using System;
    using System.IO;

    internal class TimedStream : Stream
    {
        private Stream baseStream;
        private bool isClosed;
        private int lastReadTimeout;
        private int lastWriteTimeout;
        private LowResolutionStopwatch stopwatch;
        private int timeout;

        public TimedStream(Stream baseStream)
        {
            this.baseStream = baseStream;
            this.timeout = baseStream.ReadTimeout;
            this.isClosed = false;
            this.stopwatch = new LowResolutionStopwatch();
        }

        public override void Close()
        {
            if (!this.isClosed)
            {
                this.isClosed = true;
                this.baseStream.Close();
            }
        }

        public override void Flush()
        {
            try
            {
                this.StartTimer(IOKind.Write);
                this.baseStream.Flush();
                this.StopTimer();
            }
            catch (Exception exception)
            {
                this.HandleException(exception);
                throw;
            }
        }

        private void HandleException(Exception e)
        {
            this.stopwatch.Stop();
            this.ResetTimeout(-1);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int num2;
            try
            {
                this.StartTimer(IOKind.Read);
                int num = this.baseStream.Read(buffer, offset, count);
                this.StopTimer();
                num2 = num;
            }
            catch (Exception exception)
            {
                this.HandleException(exception);
                throw;
            }
            return num2;
        }

        public override int ReadByte()
        {
            int num2;
            try
            {
                this.StartTimer(IOKind.Read);
                int num = this.baseStream.ReadByte();
                this.StopTimer();
                num2 = num;
            }
            catch (Exception exception)
            {
                this.HandleException(exception);
                throw;
            }
            return num2;
        }

        public void ResetTimeout(int newTimeout)
        {
            if ((newTimeout == -1) || (newTimeout == 0))
            {
                this.timeout = -1;
            }
            else
            {
                this.timeout = newTimeout;
            }
            this.stopwatch.Reset();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return this.baseStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            this.baseStream.SetLength(value);
        }

        private bool ShouldResetStreamTimeout(int currentValue, int newValue)
        {
            return (((newValue == -1) && (currentValue != newValue)) || ((newValue > currentValue) || (currentValue >= (newValue + 100))));
        }

        private void StartTimer(IOKind op)
        {
            int num;
            if (this.timeout == -1)
            {
                num = -1;
            }
            else
            {
                num = this.timeout - ((int) this.stopwatch.ElapsedMilliseconds);
            }
            if (op == IOKind.Read)
            {
                if (this.ShouldResetStreamTimeout(this.lastReadTimeout, num))
                {
                    this.baseStream.ReadTimeout = num;
                    this.lastReadTimeout = num;
                }
            }
            else if (this.ShouldResetStreamTimeout(this.lastWriteTimeout, num))
            {
                this.baseStream.WriteTimeout = num;
                this.lastWriteTimeout = num;
            }
            if (this.timeout != -1)
            {
                this.stopwatch.Start();
            }
        }

        private void StopTimer()
        {
            if (this.timeout != -1)
            {
                this.stopwatch.Stop();
                if (this.stopwatch.ElapsedMilliseconds > this.timeout)
                {
                    this.ResetTimeout(-1);
                    throw new TimeoutException("Timeout in IO operation");
                }
            }
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            try
            {
                this.StartTimer(IOKind.Write);
                this.baseStream.Write(buffer, offset, count);
                this.StopTimer();
            }
            catch (Exception exception)
            {
                this.HandleException(exception);
                throw;
            }
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

        private enum IOKind
        {
            Read,
            Write
        }
    }
}

