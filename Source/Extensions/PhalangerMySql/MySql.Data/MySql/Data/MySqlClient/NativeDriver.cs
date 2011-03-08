namespace MySql.Data.MySqlClient
{
    using MySql.Data.Common;
    using MySql.Data.MySqlClient.Properties;
    using MySql.Data.Types;
    using System;
    using System.Collections;
    using System.IO;
    using System.Net.Security;
    using System.Security.Authentication;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;

    internal class NativeDriver : IDriver
    {
        protected Stream baseStream;
        private ClientFlags connectionFlags;
        protected string encryptionSeed;
        private BitArray nullMap;
        private Driver owner;
        private MySqlPacket packet;
        protected ServerStatusFlags serverStatus;
        protected MySqlStream stream;
        private int threadId;
        private DBVersion version;
        private int warnings;

        public NativeDriver(Driver owner)
        {
            this.owner = owner;
            this.threadId = -1;
        }

        public void Authenticate()
        {
            this.packet.WriteString(this.Settings.UserID);
            this.AuthenticateNew();
        }

        private void AuthenticateNew()
        {
            if ((this.connectionFlags & ClientFlags.SECURE_CONNECTION) == ((ClientFlags) 0L))
            {
                this.AuthenticateOld();
            }
            this.packet.Write(Crypt.Get411Password(this.Settings.Password, this.encryptionSeed));
            if (((this.connectionFlags & ClientFlags.CONNECT_WITH_DB) != ((ClientFlags) 0L)) && (this.Settings.Database != null))
            {
                this.packet.WriteString(this.Settings.Database);
            }
            else
            {
                this.packet.WriteString("");
            }
            this.stream.SendPacket(this.packet);
            this.packet = this.stream.ReadPacket();
            if (this.packet.IsLastPacket)
            {
                this.packet.Clear();
                this.packet.WriteString(Crypt.EncryptPassword(this.Settings.Password, this.encryptionSeed.Substring(0, 8), true));
                this.stream.SendPacket(this.packet);
                this.ReadOk(true);
            }
            else
            {
                this.ReadOk(false);
            }
        }

        private void AuthenticateOld()
        {
            this.packet.WriteString(Crypt.EncryptPassword(this.Settings.Password, this.encryptionSeed, true));
            if (((this.connectionFlags & ClientFlags.CONNECT_WITH_DB) != ((ClientFlags) 0L)) && (this.Settings.Database != null))
            {
                this.packet.WriteString(this.Settings.Database);
            }
            this.stream.SendPacket(this.packet);
            this.ReadOk(true);
        }

        public void Close(bool isOpen)
        {
            try
            {
                if (isOpen)
                {
                    try
                    {
                        this.packet.Clear();
                        this.packet.WriteByte(1);
                        this.ExecutePacket(this.packet);
                    }
                    catch (Exception)
                    {
                    }
                }
                if (this.stream != null)
                {
                    this.stream.Close();
                }
                this.stream = null;
            }
            catch (Exception)
            {
            }
        }

        public void CloseStatement(int statementId)
        {
            this.packet.Clear();
            this.packet.WriteByte(0x19);
            this.packet.WriteInteger((long) statementId, 4);
            this.stream.SequenceByte = 0;
            this.stream.SendPacket(this.packet);
        }

        public void Configure()
        {
            this.stream.MaxPacketSize = (ulong) this.owner.MaxPacketSize;
            this.stream.Encoding = this.Encoding;
        }

        private void ExecutePacket(MySqlPacket packetToExecute)
        {
            try
            {
                this.warnings = 0;
                this.stream.SequenceByte = 0;
                this.stream.SendPacket(packetToExecute);
            }
            catch (MySqlException exception)
            {
                this.HandleException(exception);
                throw;
            }
        }

        public void ExecuteStatement(MySqlPacket packetToExecute)
        {
            this.warnings = 0;
            packetToExecute.Buffer[4] = 0x17;
            this.ExecutePacket(packetToExecute);
            this.serverStatus |= ServerStatusFlags.AnotherQuery;
        }

        public bool FetchDataRow(int statementId, int columns)
        {
            this.packet = this.stream.ReadPacket();
            if (this.packet.IsLastPacket)
            {
                this.CheckEOF();
                return false;
            }
            this.nullMap = null;
            if (statementId > 0)
            {
                this.ReadNullMap(columns);
            }
            return true;
        }

        private X509CertificateCollection GetClientCertificates()
        {
            X509CertificateCollection certificates = new X509CertificateCollection();
            if (this.Settings.CertificateFile != null)
            {
                X509Certificate2 certificate = new X509Certificate2(this.Settings.CertificateFile, this.Settings.CertificatePassword);
                certificates.Add(certificate);
                return certificates;
            }
            if (this.Settings.CertificateStoreLocation != MySqlCertificateStoreLocation.None)
            {
                StoreLocation storeLocation = (this.Settings.CertificateStoreLocation == MySqlCertificateStoreLocation.CurrentUser) ? StoreLocation.CurrentUser : StoreLocation.LocalMachine;
                X509Store store = new X509Store(StoreName.My, storeLocation);
                store.Open(OpenFlags.OpenExistingOnly);
                if (this.Settings.CertificateThumbprint == null)
                {
                    certificates.AddRange(store.Certificates);
                    return certificates;
                }
                certificates.AddRange(store.Certificates.Find(X509FindType.FindByThumbprint, this.Settings.CertificateThumbprint, true));
                if (certificates.Count == 0)
                {
                    throw new MySqlException("Certificate with Thumbprint " + this.Settings.CertificateThumbprint + " not found");
                }
            }
            return certificates;
        }

        private void GetColumnData(MySqlField field)
        {
            ColumnFlags flags;
            this.stream.Encoding = this.Encoding;
            this.packet = this.stream.ReadPacket();
            field.Encoding = this.Encoding;
            field.CatalogName = this.packet.ReadLenString();
            field.DatabaseName = this.packet.ReadLenString();
            field.TableName = this.packet.ReadLenString();
            field.RealTableName = this.packet.ReadLenString();
            field.ColumnName = this.packet.ReadLenString();
            field.OriginalColumnName = this.packet.ReadLenString();
            this.packet.ReadByte();
            field.CharacterSetIndex = this.packet.ReadInteger(2);
            field.ColumnLength = this.packet.ReadInteger(4);
            MySqlDbType type = (MySqlDbType) this.packet.ReadByte();
            if ((this.connectionFlags & ClientFlags.LONG_FLAG) != ((ClientFlags) 0L))
            {
                flags = (ColumnFlags) this.packet.ReadInteger(2);
            }
            else
            {
                flags = (ColumnFlags) this.packet.ReadByte();
            }
            field.Scale = this.packet.ReadByte();
            if (this.packet.HasMoreData)
            {
                this.packet.ReadInteger(2);
            }
            switch (type)
            {
                case MySqlDbType.Decimal:
                case MySqlDbType.NewDecimal:
                    field.Precision = (byte) (field.ColumnLength - field.Scale);
                    if ((flags & ColumnFlags.UNSIGNED) != ((ColumnFlags) 0))
                    {
                        field.Precision = (byte) (field.Precision + 1);
                    }
                    break;
            }
            field.SetTypeAndFlags(type, flags);
        }

        public void GetColumnsData(MySqlField[] columns)
        {
            for (int i = 0; i < columns.Length; i++)
            {
                this.GetColumnData(columns[i]);
            }
            this.ReadEOF();
        }

        public int GetResult(ref int affectedRow, ref int insertedId)
        {
            try
            {
                this.packet = this.stream.ReadPacket();
            }
            catch (TimeoutException)
            {
                throw;
            }
            catch (Exception)
            {
                this.serverStatus = 0;
                throw;
            }
            int num = this.packet.ReadFieldLength();
            if (-1 == num)
            {
                string filename = this.packet.ReadString();
                this.SendFileToServer(filename);
                return this.GetResult(ref affectedRow, ref insertedId);
            }
            if (num == 0)
            {
                this.serverStatus &= ~(ServerStatusFlags.AnotherQuery | ServerStatusFlags.MoreResults);
                affectedRow = this.packet.ReadFieldLength();
                insertedId = this.packet.ReadFieldLength();
                this.serverStatus = (ServerStatusFlags) this.packet.ReadInteger(2);
                this.warnings += this.packet.ReadInteger(2);
                if (this.packet.HasMoreData)
                {
                    this.packet.ReadLenString();
                }
            }
            return num;
        }

        private void HandleException(MySqlException ex)
        {
            if (ex.IsFatal)
            {
                this.owner.Close();
            }
        }

        private void CheckEOF()
        {
            if (!this.packet.IsLastPacket)
            {
                throw new MySqlException("Expected end of data packet");
            }
            this.packet.ReadByte();
            if (this.packet.HasMoreData)
            {
                this.warnings += this.packet.ReadInteger(2);
                this.serverStatus = (ServerStatusFlags) this.packet.ReadInteger(2);
            }
        }

        public void Open()
        {
            try
            {
                if (this.Settings.ConnectionProtocol == MySqlConnectionProtocol.SharedMemory)
                {
                    SharedMemoryStream stream = new SharedMemoryStream(this.Settings.SharedMemoryName);
                    stream.Open(this.Settings.ConnectionTimeout);
                    this.baseStream = stream;
                }
                else
                {
                    string pipeName = this.Settings.PipeName;
                    if (this.Settings.ConnectionProtocol != MySqlConnectionProtocol.Pipe)
                    {
                        pipeName = null;
                    }
                    this.baseStream = new StreamCreator(this.Settings.Server, this.Settings.Port, pipeName, this.Settings.Keepalive).GetStream(this.Settings.ConnectionTimeout);
                }
            }
            catch (Exception exception)
            {
                throw new MySqlException(Resources.UnableToConnectToHost, 0x412, exception);
            }
            if (this.baseStream == null)
            {
                throw new MySqlException(Resources.UnableToConnectToHost, 0x412);
            }
            int num = 0xfd02ff;
            this.stream = new MySqlStream(this.baseStream, this.Encoding, false);
            this.stream.ResetTimeout((int) (this.Settings.ConnectionTimeout * 0x3e8));
            this.packet = this.stream.ReadPacket();
            this.packet.ReadByte();
            string versionString = this.packet.ReadString();
            this.version = DBVersion.Parse(versionString);
            if (!this.version.isAtLeast(5, 0, 0))
            {
                throw new NotSupportedException(Resources.ServerTooOld);
            }
            this.threadId = this.packet.ReadInteger(4);
            this.encryptionSeed = this.packet.ReadString();
            num = 0xffffff;
            ClientFlags serverCaps = 0L;
            if (this.packet.HasMoreData)
            {
                serverCaps = (ClientFlags) this.packet.ReadInteger(2);
            }
            this.owner.ConnectionCharSetIndex = this.packet.ReadByte();
            this.serverStatus = (ServerStatusFlags) this.packet.ReadInteger(2);
            this.packet.Position += 13;
            string str3 = this.packet.ReadString();
            this.encryptionSeed = this.encryptionSeed + str3;
            this.SetConnectionFlags(serverCaps);
            this.packet.Clear();
            this.packet.WriteInteger((long) ((int) this.connectionFlags), 4);
            if ((serverCaps & ClientFlags.SSL) == ((ClientFlags) 0L))
            {
                if ((this.Settings.SslMode != MySqlSslMode.None) && (this.Settings.SslMode != MySqlSslMode.Preferred))
                {
                    throw new MySqlException(string.Format(Resources.NoServerSSLSupport, this.Settings.Server));
                }
            }
            else if (this.Settings.SslMode != MySqlSslMode.None)
            {
                this.stream.SendPacket(this.packet);
                this.StartSSL();
                this.packet.Clear();
                this.packet.WriteInteger((long) ((int) this.connectionFlags), 4);
            }
            this.packet.WriteInteger((long) num, 4);
            this.packet.WriteByte(8);
            this.packet.Write(new byte[0x17]);
            this.Authenticate();
            if ((this.connectionFlags & ClientFlags.COMPRESS) != ((ClientFlags) 0L))
            {
                this.stream = new MySqlStream(this.baseStream, this.Encoding, true);
            }
            this.packet.Version = this.version;
            this.stream.MaxBlockSize = num;
        }

        public bool Ping()
        {
            try
            {
                this.packet.Clear();
                this.packet.WriteByte(14);
                this.ExecutePacket(this.packet);
                this.ReadOk(true);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public int PrepareStatement(string sql, ref MySqlField[] parameters)
        {
            this.packet.Length = (sql.Length * 4) + 5;
            byte[] buffer = this.packet.Buffer;
            int num = this.Encoding.GetBytes(sql, 0, sql.Length, this.packet.Buffer, 5);
            this.packet.Position = num + 5;
            buffer[4] = 0x16;
            this.ExecutePacket(this.packet);
            this.packet = this.stream.ReadPacket();
            if (this.packet.ReadByte() != 0)
            {
                throw new MySqlException("Expected prepared statement marker");
            }
            int num3 = this.packet.ReadInteger(4);
            int num4 = this.packet.ReadInteger(2);
            int count = this.packet.ReadInteger(2);
            this.packet.ReadInteger(3);
            if (count > 0)
            {
                parameters = this.owner.GetColumns(count);
                for (int i = 0; i < parameters.Length; i++)
                {
                    parameters[i].Encoding = this.Encoding;
                }
            }
            if (num4 > 0)
            {
                while (num4-- > 0)
                {
                    this.packet = this.stream.ReadPacket();
                }
                this.ReadEOF();
            }
            return num3;
        }

        public IMySqlValue ReadColumnValue(int index, MySqlField field, IMySqlValue valObject)
        {
            bool flag;
            long length = -1L;
            if (this.nullMap != null)
            {
                flag = this.nullMap[index + 2];
            }
            else
            {
                length = this.packet.ReadFieldLength();
                flag = length == -1L;
            }
            this.packet.Encoding = field.Encoding;
            this.packet.Version = this.version;
            return valObject.ReadValue(this.packet, length, flag);
        }

        private void ReadEOF()
        {
            this.packet = this.stream.ReadPacket();
            this.CheckEOF();
        }

        private void ReadNullMap(int fieldCount)
        {
            this.nullMap = null;
            byte[] byteBuffer = new byte[(fieldCount + 9) / 8];
            this.packet.ReadByte();
            this.packet.Read(byteBuffer, 0, byteBuffer.Length);
            this.nullMap = new BitArray(byteBuffer);
        }

        private void ReadOk(bool read)
        {
            try
            {
                if (read)
                {
                    this.packet = this.stream.ReadPacket();
                }
                if (this.packet.ReadByte() != 0)
                {
                    throw new MySqlException("Out of sync with server", true, null);
                }
                this.packet.ReadFieldLength();
                this.packet.ReadFieldLength();
                if (this.packet.HasMoreData)
                {
                    this.serverStatus = (ServerStatusFlags) this.packet.ReadInteger(2);
                    this.packet.ReadInteger(2);
                    if (this.packet.HasMoreData)
                    {
                        this.packet.ReadLenString();
                    }
                }
            }
            catch (MySqlException exception)
            {
                this.HandleException(exception);
                throw;
            }
        }

        public void Reset()
        {
            this.warnings = 0;
            this.stream.Encoding = this.Encoding;
            this.stream.SequenceByte = 0;
            this.packet.Clear();
            this.packet.WriteByte(0x11);
            this.Authenticate();
        }

        public void ResetTimeout(int timeout)
        {
            if (this.stream != null)
            {
                this.stream.ResetTimeout(timeout);
            }
        }

        private void SendFileToServer(string filename)
        {
            byte[] buffer = new byte[0x2004];
            long num = 0L;
            try
            {
                using (FileStream stream = new FileStream(filename, FileMode.Open, FileAccess.Read))
                {
                    int num2;
                    for (num = stream.Length; num > 0L; num -= num2)
                    {
                        num2 = stream.Read(buffer, 4, (num > 0x2000L) ? ((int) 0x2000L) : ((int) num));
                        this.stream.SendEntirePacketDirectly(buffer, num2);
                    }
                    this.stream.SendEntirePacketDirectly(buffer, 0);
                }
            }
            catch (Exception exception)
            {
                throw new MySqlException("Error during LOAD DATA LOCAL INFILE", exception);
            }
        }

        public void SendQuery(MySqlPacket queryPacket)
        {
            this.warnings = 0;
            queryPacket.Buffer[4] = 3;
            this.ExecutePacket(queryPacket);
            this.serverStatus |= ServerStatusFlags.AnotherQuery;
        }

        private bool ServerCheckValidation(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return ((sslPolicyErrors == SslPolicyErrors.None) || (((this.Settings.SslMode == MySqlSslMode.Preferred) || (this.Settings.SslMode == MySqlSslMode.Required)) || ((this.Settings.SslMode == MySqlSslMode.VerifyCA) && (sslPolicyErrors == SslPolicyErrors.RemoteCertificateNameMismatch))));
        }

        private void SetConnectionFlags(ClientFlags serverCaps)
        {
            ClientFlags flags = ClientFlags.LOCAL_FILES;
            if (!this.Settings.UseAffectedRows)
            {
                flags |= ClientFlags.FOUND_ROWS;
            }
            flags |= ClientFlags.PROTOCOL_41;
            flags |= ClientFlags.TRANSACTIONS;
            if (this.Settings.AllowBatch)
            {
                flags |= ClientFlags.MULTI_STATEMENTS;
            }
            flags |= ClientFlags.MULTI_RESULTS;
            if ((serverCaps & ClientFlags.LONG_FLAG) != ((ClientFlags) 0L))
            {
                flags |= ClientFlags.LONG_FLAG;
            }
            if (((serverCaps & ClientFlags.COMPRESS) != ((ClientFlags) 0L)) && this.Settings.UseCompression)
            {
                flags |= ClientFlags.COMPRESS;
            }
            flags |= ClientFlags.LONG_PASSWORD;
            if (this.Settings.InteractiveSession)
            {
                flags |= ClientFlags.INTERACTIVE;
            }
            if ((((serverCaps & ClientFlags.CONNECT_WITH_DB) != ((ClientFlags) 0L)) && (this.Settings.Database != null)) && (this.Settings.Database.Length > 0))
            {
                flags |= ClientFlags.CONNECT_WITH_DB;
            }
            if ((serverCaps & ClientFlags.SECURE_CONNECTION) != ((ClientFlags) 0L))
            {
                flags |= ClientFlags.SECURE_CONNECTION;
            }
            if (((serverCaps & ClientFlags.SSL) != ((ClientFlags) 0L)) && (this.Settings.SslMode != MySqlSslMode.None))
            {
                flags |= ClientFlags.SSL;
            }
            flags |= ClientFlags.PS_MULTI_RESULTS;
            this.connectionFlags = flags;
        }

        public void SetDatabase(string dbName)
        {
            byte[] bytes = this.Encoding.GetBytes(dbName);
            this.packet.Clear();
            this.packet.WriteByte(2);
            this.packet.Write(bytes);
            this.ExecutePacket(this.packet);
            this.ReadOk(true);
        }

        public void SkipColumnValue(IMySqlValue valObject)
        {
            int num = -1;
            if (this.nullMap == null)
            {
                num = this.packet.ReadFieldLength();
                if (num == -1)
                {
                    return;
                }
            }
            if (num > -1)
            {
                this.packet.Position += num;
            }
            else
            {
                valObject.SkipValue(this.packet);
            }
        }

        private void StartSSL()
        {
            RemoteCertificateValidationCallback userCertificateValidationCallback = new RemoteCertificateValidationCallback(this.ServerCheckValidation);
            SslStream baseStream = new SslStream(this.baseStream, true, userCertificateValidationCallback, null);
            X509CertificateCollection clientCertificates = this.GetClientCertificates();
            baseStream.AuthenticateAsClient(this.Settings.Server, clientCertificates, SslProtocols.Default, false);
            this.baseStream = baseStream;
            this.stream = new MySqlStream(baseStream, this.Encoding, false);
            this.stream.SequenceByte = 2;
        }

        private System.Text.Encoding Encoding
        {
            get
            {
                return this.owner.Encoding;
            }
        }

        public ClientFlags Flags
        {
            get
            {
                return this.connectionFlags;
            }
        }

        public MySqlPacket Packet
        {
            get
            {
                return this.packet;
            }
        }

        public ServerStatusFlags ServerStatus
        {
            get
            {
                return this.serverStatus;
            }
        }

        private MySqlConnectionStringBuilder Settings
        {
            get
            {
                return this.owner.Settings;
            }
        }

        public int ThreadId
        {
            get
            {
                return this.threadId;
            }
        }

        public DBVersion Version
        {
            get
            {
                return this.version;
            }
        }

        public int WarningCount
        {
            get
            {
                return this.warnings;
            }
        }
    }
}

