namespace MySql.Data.Common
{
    using MySql.Data.MySqlClient.Properties;
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;
    using System.Reflection;

    internal class StreamCreator
    {
        private string hostList;
        private uint keepalive;
        private string pipeName;
        private uint port;
        private uint timeOut;

        public StreamCreator(string hosts, uint port, string pipeName, uint keepalive)
        {
            this.hostList = hosts;
            if ((this.hostList == null) || (this.hostList.Length == 0))
            {
                this.hostList = "localhost";
            }
            this.port = port;
            this.pipeName = pipeName;
            this.keepalive = keepalive;
        }

        private Stream CreateSocketStream(IPAddress ip, bool unix)
        {
            EndPoint point;
            if (!Platform.IsWindows() && unix)
            {
                point = CreateUnixEndPoint(this.hostList);
            }
            else
            {
                point = new IPEndPoint(ip, (int) this.port);
            }
            Socket s = unix ? new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.IP) : new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            if (this.keepalive > 0)
            {
                SetKeepAlive(s, this.keepalive);
            }
            IAsyncResult asyncResult = s.BeginConnect(point, null, null);
            if (!asyncResult.AsyncWaitHandle.WaitOne((int) (this.timeOut * 0x3e8), false))
            {
                s.Close();
                return null;
            }
            try
            {
                s.EndConnect(asyncResult);
            }
            catch (Exception)
            {
                s.Close();
                throw;
            }
            MyNetworkStream stream = new MyNetworkStream(s, true);
            GC.SuppressFinalize(s);
            GC.SuppressFinalize(stream);
            return stream;
        }

        private static EndPoint CreateUnixEndPoint(string host)
        {
            return (EndPoint) Assembly.Load("Mono.Posix").CreateInstance("Mono.Posix.UnixEndPoint", false, BindingFlags.CreateInstance, null, new object[] { host }, null, null);
        }

        private IPHostEntry GetDnsHostEntry(string hostname)
        {
            IPHostEntry hostEntry;
            LowResolutionStopwatch stopwatch = new LowResolutionStopwatch();
            try
            {
                stopwatch.Start();
                hostEntry = Dns.GetHostEntry(hostname);
            }
            catch (SocketException exception)
            {
                throw new Exception(string.Format(Resources.GetHostEntryFailed, new object[] { stopwatch.Elapsed, hostname, exception.SocketErrorCode, exception.ErrorCode, exception.NativeErrorCode }), exception);
            }
            finally
            {
                stopwatch.Stop();
            }
            return hostEntry;
        }

        private IPHostEntry GetHostEntry(string hostname)
        {
            IPHostEntry entry = this.ParseIPAddress(hostname);
            if (entry != null)
            {
                return entry;
            }
            return this.GetDnsHostEntry(hostname);
        }

        public Stream GetStream(uint timeout)
        {
            this.timeOut = timeout;
            if (this.hostList.StartsWith("/", StringComparison.OrdinalIgnoreCase))
            {
                return this.CreateSocketStream(null, true);
            }
            string[] strArray = this.hostList.Split(new char[] { '&' });
            int index = new Random((int) DateTime.Now.Ticks).Next(strArray.Length);
            int num2 = 0;
            bool flag = (this.pipeName != null) && (this.pipeName.Length != 0);
            Stream stream = null;
            while (num2 < strArray.Length)
            {
                try
                {
                    if (flag)
                    {
                        stream = NamedPipeStream.Create(this.pipeName, strArray[index], timeout);
                    }
                    else
                    {
                        foreach (IPAddress address in this.GetHostEntry(strArray[index]).AddressList)
                        {
                            if (address.AddressFamily != AddressFamily.InterNetworkV6)
                            {
                                stream = this.CreateSocketStream(address, false);
                                if (stream != null)
                                {
                                    break;
                                }
                            }
                        }
                    }
                    if (stream != null)
                    {
                        return stream;
                    }
                    index++;
                    if (index == strArray.Length)
                    {
                        index = 0;
                    }
                    num2++;
                    continue;
                }
                catch (Exception)
                {
                    if (num2 >= (strArray.Length - 1))
                    {
                        throw;
                    }
                    continue;
                }
            }
            return stream;
        }

        private IPHostEntry ParseIPAddress(string hostname)
        {
            IPHostEntry entry = null;
            IPAddress address;
            if (IPAddress.TryParse(hostname, out address))
            {
                entry = new IPHostEntry();
                entry.AddressList = new IPAddress[] { address };
            }
            return entry;
        }

        private static void SetKeepAlive(Socket s, uint time)
        {
            uint num = 1;
            uint num2 = 0x3e8;
            byte[] array = new byte[12];
            BitConverter.GetBytes(num).CopyTo(array, 0);
            BitConverter.GetBytes(time).CopyTo(array, 4);
            BitConverter.GetBytes(num2).CopyTo(array, 8);
            try
            {
                s.IOControl(IOControlCode.KeepAliveValues, array, null);
            }
            catch (NotImplementedException)
            {
            }
            return;
            s.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
        }
    }
}

