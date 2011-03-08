namespace MySql.Data.Common
{
    using Microsoft.Win32.SafeHandles;
    using MySql.Data.MySqlClient.Properties;
    using System;
    using System.ComponentModel;
    using System.IO;
    using System.Runtime.InteropServices;

    internal class NamedPipeStream : Stream
    {
        private const int ERROR_PIPE_BUSY = 0xe7;
        private const int ERROR_SEM_TIMEOUT = 0x79;
        private Stream fileStream;
        private SafeFileHandle handle;
        private int readTimeout = -1;
        private int writeTimeout = -1;

        public NamedPipeStream(string path, FileAccess mode, uint timeout)
        {
            this.Open(path, mode, timeout);
        }

        private void CancelIo()
        {
            if (!MySql.Data.Common.NativeMethods.CancelIo(this.handle.DangerousGetHandle()))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
        }

        public override void Close()
        {
            if (((this.handle != null) && !this.handle.IsInvalid) && !this.handle.IsClosed)
            {
                this.fileStream.Close();
                try
                {
                    this.handle.Close();
                }
                catch (Exception)
                {
                }
            }
        }

        internal static Stream Create(string pipeName, string hostname, uint timeout)
        {
            string str;
            if (string.Compare(hostname, "localhost", true) == 0)
            {
                str = @"\\.\pipe\" + pipeName;
            }
            else
            {
                str = string.Format(@"\\{0}\pipe\{1}", hostname, pipeName);
            }
            return new NamedPipeStream(str, FileAccess.ReadWrite, timeout);
        }

        public override void Flush()
        {
            this.fileStream.Flush();
        }

        public void Open(string path, FileAccess mode, uint timeout)
        {
            IntPtr ptr;
            while (true)
            {
                ptr = MySql.Data.Common.NativeMethods.CreateFile(path, 0xc0000000, 0, null, 3, 0x40000000, 0);
                if (ptr != IntPtr.Zero)
                {
                    break;
                }
                if (Marshal.GetLastWin32Error() != 0xe7)
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "Error opening pipe");
                }
                LowResolutionStopwatch stopwatch = LowResolutionStopwatch.StartNew();
                bool flag = MySql.Data.Common.NativeMethods.WaitNamedPipe(path, timeout);
                stopwatch.Stop();
                if (!flag)
                {
                    if ((timeout >= stopwatch.ElapsedMilliseconds) && (Marshal.GetLastWin32Error() != 0x79))
                    {
                        throw new Win32Exception(Marshal.GetLastWin32Error(), "Error waiting for pipe");
                    }
                    throw new TimeoutException("Timeout waiting for named pipe");
                }
                timeout -= (uint) stopwatch.ElapsedMilliseconds;
            }
            this.handle = new SafeFileHandle(ptr, true);
            this.fileStream = new FileStream(this.handle, mode, 0x1000, true);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (this.readTimeout == -1)
            {
                return this.fileStream.Read(buffer, offset, count);
            }
            IAsyncResult asyncResult = this.fileStream.BeginRead(buffer, offset, count, null, null);
            if (!asyncResult.CompletedSynchronously && !asyncResult.AsyncWaitHandle.WaitOne(this.readTimeout))
            {
                this.CancelIo();
                throw new TimeoutException("Timeout in named pipe read");
            }
            return this.fileStream.EndRead(asyncResult);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException(Resources.NamedPipeNoSeek);
        }

        public override void SetLength(long length)
        {
            throw new NotSupportedException(Resources.NamedPipeNoSetLength);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (this.writeTimeout == -1)
            {
                this.fileStream.Write(buffer, offset, count);
            }
            else
            {
                IAsyncResult asyncResult = this.fileStream.BeginWrite(buffer, offset, count, null, null);
                if (asyncResult.CompletedSynchronously)
                {
                    this.fileStream.EndWrite(asyncResult);
                }
                if (!asyncResult.AsyncWaitHandle.WaitOne(this.readTimeout))
                {
                    this.CancelIo();
                    throw new TimeoutException("Timeout in named pipe write");
                }
                this.fileStream.EndWrite(asyncResult);
            }
        }

        public override bool CanRead
        {
            get
            {
                return this.fileStream.CanRead;
            }
        }

        public override bool CanSeek
        {
            get
            {
                throw new NotSupportedException(Resources.NamedPipeNoSeek);
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
                return this.fileStream.CanWrite;
            }
        }

        public override long Length
        {
            get
            {
                throw new NotSupportedException(Resources.NamedPipeNoSeek);
            }
        }

        public override long Position
        {
            get
            {
                throw new NotSupportedException(Resources.NamedPipeNoSeek);
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

