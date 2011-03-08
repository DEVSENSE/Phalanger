namespace MySql.Data.MySqlClient
{
    using MySql.Data.Common;
    using MySql.Data.MySqlClient.Properties;
    using MySql.Data.Types;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text;

    internal class Driver : IDisposable
    {
        protected MySqlConnectionStringBuilder connectionString;
        protected DateTime creationTime;
        protected MySqlPromotableTransaction currentTransaction;
        protected System.Text.Encoding encoding = System.Text.Encoding.GetEncoding(0x4e4);
        private bool firstResult;
        protected IDriver handler;
        protected Hashtable charSets;
        private DateTime idleSince;
        protected bool inActiveUse;
        protected bool isOpen;
        protected long maxPacketSize;
        protected MySqlPool pool;
        internal MySqlDataReader reader;
        protected ClientFlags serverCaps;
        protected string serverCharSet;
        protected int serverCharSetIndex;
        protected Hashtable serverProps;

        public Driver(MySqlConnectionStringBuilder settings)
        {
            if (this.encoding == null)
            {
                throw new MySqlException(Resources.DefaultEncodingNotFound);
            }
            this.connectionString = settings;
            this.serverCharSet = "latin1";
            this.serverCharSetIndex = -1;
            this.maxPacketSize = 0x400L;
            this.handler = new NativeDriver(this);
        }

        public virtual void Close()
        {
            this.Dispose();
        }

        public virtual void CloseQuery(MySqlConnection connection, int statementId)
        {
            if (this.handler.WarningCount > 0)
            {
                this.ReportWarnings(connection);
            }
        }

        public virtual void CloseStatement(int id)
        {
            this.handler.CloseStatement(id);
        }

        public virtual void Configure(MySqlConnection connection)
        {
            bool flag = false;
            if (this.serverProps == null)
            {
                flag = true;
                this.serverProps = new Hashtable();
                MySqlDataReader reader = new MySqlCommand("SHOW VARIABLES", connection).ExecuteReader();
                try
                {
                    while (reader.Read())
                    {
                        string str = reader.GetString(0);
                        string str2 = reader.GetString(1);
                        this.serverProps[str] = str2;
                    }
                }
                catch (Exception exception)
                {
                    MySqlTrace.LogError(this.ThreadID, exception.Message);
                    throw;
                }
                finally
                {
                    if (reader != null)
                    {
                        reader.Dispose();
                    }
                }
                if (this.serverProps.Contains("max_allowed_packet"))
                {
                    this.maxPacketSize = Convert.ToInt64(this.serverProps["max_allowed_packet"]);
                }
                this.LoadCharacterSets(connection);
            }
            if (this.Settings.ConnectionReset || flag)
            {
                string characterSet = this.connectionString.CharacterSet;
                if ((characterSet == null) || (characterSet.Length == 0))
                {
                    if (this.serverCharSetIndex >= 0)
                    {
                        characterSet = (string) this.charSets[this.serverCharSetIndex];
                    }
                    else
                    {
                        characterSet = this.serverCharSet;
                    }
                }
                MySqlCommand command2 = new MySqlCommand("SET character_set_results=NULL", connection);
                object obj2 = this.serverProps["character_set_client"];
                object obj3 = this.serverProps["character_set_connection"];
                if (((obj2 != null) && (obj2.ToString() != characterSet)) || ((obj3 != null) && (obj3.ToString() != characterSet)))
                {
                    new MySqlCommand("SET NAMES " + characterSet, connection).ExecuteNonQuery();
                }
                command2.ExecuteNonQuery();
                if (characterSet != null)
                {
                    this.Encoding = CharSetMap.GetEncoding(this.Version, characterSet);
                }
                else
                {
                    this.Encoding = CharSetMap.GetEncoding(this.Version, "latin1");
                }
                this.handler.Configure();
            }
        }

        public bool ConnectionLifetimeExpired()
        {
            TimeSpan span = DateTime.Now.Subtract(this.creationTime);
            return ((this.Settings.ConnectionLifeTime != 0) && (span.TotalSeconds > this.Settings.ConnectionLifeTime));
        }

        public static Driver Create(MySqlConnectionStringBuilder settings)
        {
            Driver driver = null;
            if ((settings.Logging || settings.UseUsageAdvisor) || MySqlTrace.QueryAnalysisEnabled)
            {
                driver = new TracingDriver(settings);
            }
            else
            {
                driver = new Driver(settings);
            }
            driver.Open();
            return driver;
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            try
            {
                this.ResetTimeout(0x3e8);
                if (disposing)
                {
                    this.handler.Close(this.isOpen);
                }
                if (this.connectionString.Pooling)
                {
                    MySqlPoolManager.RemoveConnection(this);
                }
            }
            catch (Exception)
            {
                if (disposing)
                {
                    throw;
                }
            }
            finally
            {
                this.reader = null;
                this.isOpen = false;
            }
        }

        public virtual void ExecuteDirect(string sql)
        {
            MySqlPacket p = new MySqlPacket(this.Encoding);
            p.WriteString(sql);
            this.SendQuery(p);
            this.NextResult(0);
        }

        public virtual void ExecuteStatement(MySqlPacket packetToExecute)
        {
            this.handler.ExecuteStatement(packetToExecute);
        }

        public virtual bool FetchDataRow(int statementId, int columns)
        {
            return this.handler.FetchDataRow(statementId, columns);
        }

        ~Driver()
        {
            this.Dispose(false);
        }

        public MySqlField[] GetColumns(int count)
        {
            MySqlField[] columns = new MySqlField[count];
            for (int i = 0; i < count; i++)
            {
                columns[i] = new MySqlField(this);
            }
            this.handler.GetColumnsData(columns);
            return columns;
        }

        protected virtual int GetResult(int statementId, ref int affectedRows, ref int insertedId)
        {
            return this.handler.GetResult(ref affectedRows, ref insertedId);
        }

        public bool HasStatus(ServerStatusFlags flag)
        {
            return ((this.handler.ServerStatus & flag) != 0);
        }

        private void LoadCharacterSets(MySqlConnection connection)
        {
            MySqlCommand command = new MySqlCommand("SHOW COLLATION", connection);
            try
            {
                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    this.charSets = new Hashtable();
                    while (reader.Read())
                    {
                        this.charSets[Convert.ToInt32(reader["id"], NumberFormatInfo.InvariantInfo)] = reader.GetString(reader.GetOrdinal("charset"));
                    }
                }
            }
            catch (Exception exception)
            {
                MySqlTrace.LogError(this.ThreadID, exception.Message);
                throw;
            }
        }

        public virtual ResultSet NextResult(int statementId)
        {
            if (!this.firstResult && !this.HasStatus(ServerStatusFlags.AnotherQuery | ServerStatusFlags.MoreResults))
            {
                return null;
            }
            this.firstResult = false;
            int affectedRows = -1;
            int insertedId = -1;
            int numCols = this.GetResult(statementId, ref affectedRows, ref insertedId);
            if (numCols == -1)
            {
                return null;
            }
            if (numCols > 0)
            {
                return new ResultSet(this, statementId, numCols);
            }
            return new ResultSet(affectedRows, insertedId);
        }

        public virtual void Open()
        {
            this.creationTime = DateTime.Now;
            this.handler.Open();
            this.isOpen = true;
        }

        public bool Ping()
        {
            return this.handler.Ping();
        }

        public virtual int PrepareStatement(string sql, ref MySqlField[] parameters)
        {
            return this.handler.PrepareStatement(sql, ref parameters);
        }

        public string Property(string key)
        {
            return (string) this.serverProps[key];
        }

        public IMySqlValue ReadColumnValue(int index, MySqlField field, IMySqlValue value)
        {
            return this.handler.ReadColumnValue(index, field, value);
        }

        public virtual List<MySqlError> ReportWarnings(MySqlConnection connection)
        {
            List<MySqlError> list = new List<MySqlError>();
            MySqlCommand command = new MySqlCommand("SHOW WARNINGS", connection);
            using (MySqlDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    list.Add(new MySqlError(reader.GetString(0), reader.GetInt32(1), reader.GetString(2)));
                }
            }
            MySqlInfoMessageEventArgs args = new MySqlInfoMessageEventArgs();
            args.errors = list.ToArray();
            if (connection != null)
            {
                connection.OnInfoMessage(args);
            }
            return list;
        }

        public virtual void Reset()
        {
            this.handler.Reset();
        }

        public void ResetTimeout(int timeoutMilliseconds)
        {
            this.handler.ResetTimeout(timeoutMilliseconds);
        }

        public virtual void SendQuery(MySqlPacket p)
        {
            this.handler.SendQuery(p);
            this.firstResult = true;
        }

        public virtual void SetDatabase(string dbName)
        {
            this.handler.SetDatabase(dbName);
        }

        public void SkipColumnValue(IMySqlValue valObject)
        {
            this.handler.SkipColumnValue(valObject);
        }

        public virtual bool SkipDataRow()
        {
            return this.FetchDataRow(-1, 0);
        }

        internal int ConnectionCharSetIndex
        {
            get
            {
                return this.serverCharSetIndex;
            }
            set
            {
                this.serverCharSetIndex = value;
            }
        }

        public MySqlPromotableTransaction CurrentTransaction
        {
            get
            {
                return this.currentTransaction;
            }
            set
            {
                this.currentTransaction = value;
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

        internal Hashtable CharacterSets
        {
            get
            {
                return this.charSets;
            }
        }

        public DateTime IdleSince
        {
            get
            {
                return this.idleSince;
            }
            set
            {
                this.idleSince = value;
            }
        }

        public bool IsInActiveUse
        {
            get
            {
                return this.inActiveUse;
            }
            set
            {
                this.inActiveUse = value;
            }
        }

        public bool IsOpen
        {
            get
            {
                return this.isOpen;
            }
        }

        public long MaxPacketSize
        {
            get
            {
                return this.maxPacketSize;
            }
        }

        public MySqlPool Pool
        {
            get
            {
                return this.pool;
            }
            set
            {
                this.pool = value;
            }
        }

        public MySqlConnectionStringBuilder Settings
        {
            get
            {
                return this.connectionString;
            }
            set
            {
                this.connectionString = value;
            }
        }

        public bool SupportsBatch
        {
            get
            {
                return ((this.handler.Flags & ClientFlags.MULTI_STATEMENTS) != ((ClientFlags) 0L));
            }
        }

        public bool SupportsOutputParameters
        {
            get
            {
                return this.Version.isAtLeast(6, 0, 8);
            }
        }

        public int ThreadID
        {
            get
            {
                return this.handler.ThreadId;
            }
        }

        public DBVersion Version
        {
            get
            {
                return this.handler.Version;
            }
        }
    }
}

