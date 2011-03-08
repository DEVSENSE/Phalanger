namespace MySql.Data.MySqlClient
{
    using MySql.Data.Common;
    using MySql.Data.MySqlClient.Properties;
    using System;
    using System.ComponentModel;
    using System.Data;
    using System.Data.Common;
    using System.Drawing;
    using System.Drawing.Design;
    using System.Threading;
    using System.Transactions;

    [ToolboxBitmap(typeof(MySqlConnection), "MySqlClient.resources.connection.bmp"), ToolboxItem(true), DesignerCategory("Code")]
    public sealed class MySqlConnection : DbConnection, ICloneable
    {
        private int commandTimeout;
        internal ConnectionState connectionState;
        private static Cache<string, MySqlConnectionStringBuilder> connectionStringCache = new Cache<string, MySqlConnectionStringBuilder>(0, 0x19);
        private string database;
        internal Driver driver;
        private bool hasBeenOpen;
        private MySqlInfoMessageEventHandler _InfoMessage;
        private bool isKillQueryConnection;
        private PerformanceMonitor perfMonitor;
        private MySql.Data.MySqlClient.ProcedureCache procedureCache;
        private MySqlConnectionStringBuilder settings;
        private SchemaProvider schemaProvider;

        public event MySqlInfoMessageEventHandler InfoMessage
        {
            add
            {
                MySqlInfoMessageEventHandler handler2;
                MySqlInfoMessageEventHandler infoMessage = this._InfoMessage;
                do
                {
                    handler2 = infoMessage;
                    MySqlInfoMessageEventHandler handler3 = (MySqlInfoMessageEventHandler) Delegate.Combine(handler2, value);
                    infoMessage = Interlocked.CompareExchange<MySqlInfoMessageEventHandler>(ref this._InfoMessage, handler3, handler2);
                }
                while (infoMessage != handler2);
            }
            remove
            {
                MySqlInfoMessageEventHandler handler2;
                MySqlInfoMessageEventHandler infoMessage = this._InfoMessage;
                do
                {
                    handler2 = infoMessage;
                    MySqlInfoMessageEventHandler handler3 = (MySqlInfoMessageEventHandler) Delegate.Remove(handler2, value);
                    infoMessage = Interlocked.CompareExchange<MySqlInfoMessageEventHandler>(ref this._InfoMessage, handler3, handler2);
                }
                while (infoMessage != handler2);
            }
        }

        public MySqlConnection()
        {
            this.settings = new MySqlConnectionStringBuilder();
            this.database = string.Empty;
        }

        public MySqlConnection(string connectionString) : this()
        {
            this.ConnectionString = connectionString;
        }

        internal void Abort()
        {
            try
            {
                this.driver.Close();
            }
            catch (Exception exception)
            {
                MySqlTrace.LogWarning(this.ServerThread, "Error occurred aborting the connection. Exception was: " + exception.Message);
            }
            this.SetState(ConnectionState.Closed, true);
        }

        protected override DbTransaction BeginDbTransaction(System.Data.IsolationLevel isolationLevel)
        {
            if (isolationLevel == System.Data.IsolationLevel.Unspecified)
            {
                return this.BeginTransaction();
            }
            return this.BeginTransaction(isolationLevel);
        }

        public MySqlTransaction BeginTransaction()
        {
            return this.BeginTransaction(System.Data.IsolationLevel.RepeatableRead);
        }

        public MySqlTransaction BeginTransaction(System.Data.IsolationLevel iso)
        {
            if (this.State != ConnectionState.Open)
            {
                throw new InvalidOperationException(Resources.ConnectionNotOpen);
            }
            if (this.driver.HasStatus(ServerStatusFlags.InTransaction))
            {
                throw new InvalidOperationException(Resources.NoNestedTransactions);
            }
            MySqlTransaction transaction = new MySqlTransaction(this, iso);
            MySqlCommand command = new MySqlCommand("", this);
            command.CommandText = "SET SESSION TRANSACTION ISOLATION LEVEL ";
            switch (iso)
            {
                case System.Data.IsolationLevel.Chaos:
                    throw new NotSupportedException(Resources.ChaosNotSupported);

                case System.Data.IsolationLevel.ReadUncommitted:
                    command.CommandText = command.CommandText + "READ UNCOMMITTED";
                    goto Label_00F0;

                case System.Data.IsolationLevel.ReadCommitted:
                    command.CommandText = command.CommandText + "READ COMMITTED";
                    break;

                case System.Data.IsolationLevel.RepeatableRead:
                    command.CommandText = command.CommandText + "REPEATABLE READ";
                    break;

                case System.Data.IsolationLevel.Serializable:
                    command.CommandText = command.CommandText + "SERIALIZABLE";
                    break;
            }
        Label_00F0:
            command.ExecuteNonQuery();
            command.CommandText = "BEGIN";
            command.ExecuteNonQuery();
            return transaction;
        }

        public void CancelQuery(int timeout)
        {
            MySqlConnectionStringBuilder builder = new MySqlConnectionStringBuilder(this.Settings.ConnectionString);
            builder.Pooling = false;
            builder.ConnectionTimeout = (uint) timeout;
            using (MySqlConnection connection = new MySqlConnection(builder.ConnectionString))
            {
                connection.isKillQueryConnection = true;
                connection.Open();
                MySqlCommand command = new MySqlCommand("KILL QUERY " + this.ServerThread, connection);
                command.CommandTimeout = timeout;
                command.ExecuteNonQuery();
            }
        }

        public static void ClearAllPools()
        {
            MySqlPoolManager.ClearAllPools();
        }

        internal void ClearCommandTimeout()
        {
            if (this.hasBeenOpen)
            {
                this.commandTimeout = 0;
                if (this.driver != null)
                {
                    this.driver.ResetTimeout(0);
                }
            }
        }

        public static void ClearPool(MySqlConnection connection)
        {
            MySqlPoolManager.ClearPool(connection.Settings);
        }

        public MySqlConnection Clone()
        {
            MySqlConnection connection = new MySqlConnection();
            string connectionString = this.settings.ConnectionString;
            if (connectionString != null)
            {
                connection.ConnectionString = connectionString;
            }
            return connection;
        }

        public override void Close()
        {
            if (this.State != ConnectionState.Closed)
            {
                if (this.Reader != null)
                {
                    this.Reader.Close();
                }
                if (this.driver != null)
                {
                    if (this.driver.CurrentTransaction == null)
                    {
                        this.CloseFully();
                    }
                    else
                    {
                        this.driver.IsInActiveUse = false;
                    }
                }
                this.SetState(ConnectionState.Closed, true);
            }
        }

        internal void CloseFully()
        {
            if (this.settings.Pooling && this.driver.IsOpen)
            {
                if (this.driver.HasStatus(ServerStatusFlags.InTransaction))
                {
                    new MySqlTransaction(this, System.Data.IsolationLevel.Unspecified).Rollback();
                }
                MySqlPoolManager.ReleaseConnection(this.driver);
            }
            else
            {
                this.driver.Close();
            }
            this.driver = null;
        }

        public MySqlCommand CreateCommand()
        {
            MySqlCommand command = new MySqlCommand();
            command.Connection = this;
            return command;
        }

        protected override DbCommand CreateDbCommand()
        {
            return this.CreateCommand();
        }

        internal string CurrentDatabase()
        {
            if ((this.Database != null) && (this.Database.Length > 0))
            {
                return this.Database;
            }
            MySqlCommand command = new MySqlCommand("SELECT database()", this);
            return command.ExecuteScalar().ToString();
        }

        protected override void Dispose(bool disposing)
        {
            if (this.State == ConnectionState.Open)
            {
                this.Close();
            }
            base.Dispose(disposing);
        }

        public override void EnlistTransaction(Transaction transaction)
        {
            if (transaction != null)
            {
                if (this.driver.CurrentTransaction != null)
                {
                    if (this.driver.CurrentTransaction.BaseTransaction != transaction)
                    {
                        throw new MySqlException("Already enlisted");
                    }
                }
                else
                {
                    Driver driverInTransaction = DriverTransactionManager.GetDriverInTransaction(transaction);
                    if (driverInTransaction != null)
                    {
                        if (driverInTransaction.IsInActiveUse)
                        {
                            throw new NotSupportedException(Resources.MultipleConnectionsInTransactionNotSupported);
                        }
                        string connectionString = driverInTransaction.Settings.ConnectionString;
                        string strB = this.Settings.ConnectionString;
                        if (string.Compare(connectionString, strB, true) != 0)
                        {
                            throw new NotSupportedException(Resources.MultipleConnectionsInTransactionNotSupported);
                        }
                        this.CloseFully();
                        this.driver = driverInTransaction;
                    }
                    if (this.driver.CurrentTransaction == null)
                    {
                        MySqlPromotableTransaction promotableSinglePhaseNotification = new MySqlPromotableTransaction(this, transaction);
                        if (!transaction.EnlistPromotableSinglePhase(promotableSinglePhaseNotification))
                        {
                            throw new NotSupportedException(Resources.DistributedTxnNotSupported);
                        }
                        this.driver.CurrentTransaction = promotableSinglePhaseNotification;
                        DriverTransactionManager.SetDriverInTransaction(this.driver);
                        this.driver.IsInActiveUse = true;
                    }
                }
            }
        }

        public override DataTable GetSchema()
        {
            return this.GetSchema(null);
        }

        public override DataTable GetSchema(string collectionName)
        {
            if (collectionName == null)
            {
                collectionName = SchemaProvider.MetaCollection;
            }
            return this.GetSchema(collectionName, null);
        }

        public override DataTable GetSchema(string collectionName, string[] restrictionValues)
        {
            if (collectionName == null)
            {
                collectionName = SchemaProvider.MetaCollection;
            }
            string[] restrictions = this.schemaProvider.CleanRestrictions(restrictionValues);
            return this.schemaProvider.GetSchema(collectionName, restrictions);
        }

        internal void HandleTimeoutOrThreadAbort(Exception ex)
        {
            bool isFatal = false;
            if (this.isKillQueryConnection)
            {
                this.Abort();
                if (ex is TimeoutException)
                {
                    throw new MySqlException(Resources.Timeout, true, ex);
                }
            }
            else
            {
                try
                {
                    this.CancelQuery(5);
                    this.driver.ResetTimeout(0x1388);
                    if (this.Reader != null)
                    {
                        this.Reader.Close();
                        this.Reader = null;
                    }
                }
                catch (Exception exception)
                {
                    MySqlTrace.LogWarning(this.ServerThread, "Could not kill query,  aborting connection. Exception was " + exception.Message);
                    this.Abort();
                    isFatal = true;
                }
                if (ex is TimeoutException)
                {
                    throw new MySqlException(Resources.Timeout, isFatal, ex);
                }
            }
        }

        public override void ChangeDatabase(string databaseName)
        {
            if ((databaseName == null) || (databaseName.Trim().Length == 0))
            {
                throw new ArgumentException(Resources.ParameterIsInvalid, "databaseName");
            }
            if (this.State != ConnectionState.Open)
            {
                throw new InvalidOperationException(Resources.ConnectionNotOpen);
            }
            lock (this.driver)
            {
                if ((Transaction.Current != null) && (Transaction.Current.TransactionInformation.Status == TransactionStatus.Aborted))
                {
                    throw new TransactionAbortedException();
                }
                using (new CommandTimer(this, (int) this.Settings.DefaultCommandTimeout))
                {
                    this.driver.SetDatabase(databaseName);
                }
            }
            this.database = databaseName;
        }

        internal void OnInfoMessage(MySqlInfoMessageEventArgs args)
        {
            if (this._InfoMessage != null)
            {
                this._InfoMessage(this, args);
            }
        }

        public override void Open()
        {
            if (this.State == ConnectionState.Open)
            {
                throw new InvalidOperationException(Resources.ConnectionAlreadyOpen);
            }
            this.SetState(ConnectionState.Connecting, true);
            if (this.settings.AutoEnlist && (Transaction.Current != null))
            {
                this.driver = DriverTransactionManager.GetDriverInTransaction(Transaction.Current);
                if ((this.driver != null) && (this.driver.IsInActiveUse || !this.driver.Settings.EquivalentTo(this.Settings)))
                {
                    throw new NotSupportedException(Resources.MultipleConnectionsInTransactionNotSupported);
                }
            }
            try
            {
                if (this.settings.Pooling)
                {
                    MySqlPool pool = MySqlPoolManager.GetPool(this.settings);
                    if (this.driver == null)
                    {
                        this.driver = pool.GetConnection();
                    }
                    this.procedureCache = pool.ProcedureCache;
                }
                else
                {
                    if (this.driver == null)
                    {
                        this.driver = Driver.Create(this.settings);
                    }
                    this.procedureCache = new MySql.Data.MySqlClient.ProcedureCache((int) this.settings.ProcedureCacheSize);
                }
            }
            catch (Exception)
            {
                this.SetState(ConnectionState.Closed, true);
                throw;
            }
            if (this.driver.Settings.UseOldSyntax)
            {
                MySqlTrace.LogWarning(this.ServerThread, "You are using old syntax that will be removed in future versions");
            }
            this.SetState(ConnectionState.Open, false);
            this.driver.Configure(this);
            if ((this.settings.Database != null) && (this.settings.Database != string.Empty))
            {
                this.ChangeDatabase(this.settings.Database);
            }
            this.schemaProvider = new ISSchemaProvider(this);
            this.perfMonitor = new PerformanceMonitor(this);
            if ((Transaction.Current != null) && this.settings.AutoEnlist)
            {
                this.EnlistTransaction(Transaction.Current);
            }
            this.hasBeenOpen = true;
            this.SetState(ConnectionState.Open, true);
        }

        public bool Ping()
        {
            if (this.Reader != null)
            {
                throw new MySqlException(Resources.DataReaderOpen);
            }
            if ((this.driver != null) && this.driver.Ping())
            {
                return true;
            }
            this.driver = null;
            this.SetState(ConnectionState.Closed, true);
            return false;
        }

        internal bool SetCommandTimeout(int value)
        {
            if (!this.hasBeenOpen)
            {
                return false;
            }
            if (this.commandTimeout != 0)
            {
                return false;
            }
            if (this.driver == null)
            {
                return false;
            }
            this.commandTimeout = value;
            this.driver.ResetTimeout(this.commandTimeout * 0x3e8);
            return true;
        }

        internal void SetState(ConnectionState newConnectionState, bool broadcast)
        {
            if ((newConnectionState != this.connectionState) || broadcast)
            {
                ConnectionState connectionState = this.connectionState;
                this.connectionState = newConnectionState;
                if (broadcast)
                {
                    this.OnStateChange(new StateChangeEventArgs(connectionState, this.connectionState));
                }
            }
        }

        object ICloneable.Clone()
        {
            return this.Clone();
        }

        [Category("Data"), Browsable(true), Editor("MySql.Data.MySqlClient.Design.ConnectionStringTypeEditor,MySqlClient.Design", typeof(UITypeEditor)), Description("Information used to connect to a DataSource, such as 'Server=xxx;UserId=yyy;Password=zzz;Database=dbdb'.")]
        public override string ConnectionString
        {
            get
            {
                return this.settings.GetConnectionString(!this.hasBeenOpen || this.settings.PersistSecurityInfo);
            }
            set
            {
                MySqlConnectionStringBuilder builder;
                if (this.State != ConnectionState.Closed)
                {
                    throw new MySqlException("Not allowed to change the 'ConnectionString' property while the connection (state=" + this.State + ").");
                }
                lock (connectionStringCache)
                {
                    if (value == null)
                    {
                        builder = new MySqlConnectionStringBuilder();
                    }
                    else
                    {
                        builder = connectionStringCache[value];
                        if (builder == null)
                        {
                            builder = new MySqlConnectionStringBuilder(value);
                            connectionStringCache.Add(value, builder);
                        }
                    }
                }
                this.settings = builder;
                if ((this.settings.Database != null) && (this.settings.Database.Length > 0))
                {
                    this.database = this.settings.Database;
                }
                if (this.driver != null)
                {
                    this.driver.Settings = builder;
                }
            }
        }

        [Browsable(true)]
        public override int ConnectionTimeout
        {
            get
            {
                return (int) this.settings.ConnectionTimeout;
            }
        }

        [Browsable(true)]
        public override string Database
        {
            get
            {
                return this.database;
            }
        }

        [Browsable(true)]
        public override string DataSource
        {
            get
            {
                return this.settings.Server;
            }
        }

        protected override System.Data.Common.DbProviderFactory DbProviderFactory
        {
            get
            {
                return MySqlClientFactory.Instance;
            }
        }

        internal PerformanceMonitor PerfMonitor
        {
            get
            {
                return this.perfMonitor;
            }
        }

        internal MySql.Data.MySqlClient.ProcedureCache ProcedureCache
        {
            get
            {
                return this.procedureCache;
            }
        }

        internal MySqlDataReader Reader
        {
            get
            {
                if (this.driver == null)
                {
                    return null;
                }
                return this.driver.reader;
            }
            set
            {
                this.driver.reader = value;
            }
        }

        [Browsable(false)]
        public int ServerThread
        {
            get
            {
                return this.driver.ThreadID;
            }
        }

        [Browsable(false)]
        public override string ServerVersion
        {
            get
            {
                return this.driver.Version.ToString();
            }
        }

        internal MySqlConnectionStringBuilder Settings
        {
            get
            {
                return this.settings;
            }
        }

        internal bool SoftClosed
        {
            get
            {
                return (((this.State == ConnectionState.Closed) && (this.driver != null)) && (this.driver.CurrentTransaction != null));
            }
        }

        [Browsable(false)]
        public override ConnectionState State
        {
            get
            {
                return this.connectionState;
            }
        }

        [Browsable(false)]
        public bool UseCompression
        {
            get
            {
                return this.settings.UseCompression;
            }
        }
    }
}

