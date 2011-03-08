namespace MySql.Data.MySqlClient
{
    using MySql.Data.MySqlClient.Properties;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Data;
    using System.Data.Common;
    using System.Drawing;
    using System.Drawing.Design;
    using System.Globalization;
    using System.IO;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Transactions;

    [DesignerCategory("Code"), ToolboxBitmap(typeof(MySqlCommand), "MySqlClient.resources.command.bmp")]
    public sealed class MySqlCommand : DbCommand, ICloneable
    {
        private IAsyncResult asyncResult;
        private List<MySqlCommand> batch;
        private string batchableCommandText;
        internal AsyncDelegate caller;
        private string cmdText;
        private System.Data.CommandType cmdType;
        private int commandTimeout;
        private CommandTimer commandTimer;
        private MySqlConnection connection;
        private MySqlTransaction curTransaction;
        private bool designTimeVisible;
        internal long lastInsertedId;
        private MySqlParameterCollection parameters;
        private bool resetSqlSelect;
        private PreparableStatement statement;
        internal Exception thrownException;
        private long updatedRowCount;
        private UpdateRowSource updatedRowSource;
        private bool useDefaultTimeout;

        public MySqlCommand()
        {
            this.designTimeVisible = true;
            this.cmdType = System.Data.CommandType.Text;
            this.parameters = new MySqlParameterCollection(this);
            this.updatedRowSource = UpdateRowSource.Both;
            this.cmdText = string.Empty;
            this.useDefaultTimeout = true;
        }

        public MySqlCommand(string cmdText) : this()
        {
            this.CommandText = cmdText;
        }

        public MySqlCommand(string cmdText, MySqlConnection connection) : this(cmdText)
        {
            this.Connection = connection;
        }

        public MySqlCommand(string cmdText, MySqlConnection connection, MySqlTransaction transaction) : this(cmdText, connection)
        {
            this.curTransaction = transaction;
        }

        internal void AddToBatch(MySqlCommand command)
        {
            if (this.batch == null)
            {
                this.batch = new List<MySqlCommand>();
            }
            this.batch.Add(command);
        }

        internal object AsyncExecuteWrapper(int type, CommandBehavior behavior)
        {
            this.thrownException = null;
            try
            {
                if (type == 1)
                {
                    return this.ExecuteReader(behavior);
                }
                return this.ExecuteNonQuery();
            }
            catch (Exception exception)
            {
                this.thrownException = exception;
            }
            return null;
        }

        public IAsyncResult BeginExecuteNonQuery()
        {
            if (this.caller != null)
            {
                throw new MySqlException(Resources.UnableToStartSecondAsyncOp);
            }
            this.caller = new AsyncDelegate(this.AsyncExecuteWrapper);
            this.asyncResult = this.caller.BeginInvoke(2, CommandBehavior.Default, null, null);
            return this.asyncResult;
        }

        public IAsyncResult BeginExecuteNonQuery(AsyncCallback callback, object stateObject)
        {
            if (this.caller != null)
            {
                throw new MySqlException(Resources.UnableToStartSecondAsyncOp);
            }
            this.caller = new AsyncDelegate(this.AsyncExecuteWrapper);
            this.asyncResult = this.caller.BeginInvoke(2, CommandBehavior.Default, callback, stateObject);
            return this.asyncResult;
        }

        public IAsyncResult BeginExecuteReader()
        {
            return this.BeginExecuteReader(CommandBehavior.Default);
        }

        public IAsyncResult BeginExecuteReader(CommandBehavior behavior)
        {
            if (this.caller != null)
            {
                throw new MySqlException(Resources.UnableToStartSecondAsyncOp);
            }
            this.caller = new AsyncDelegate(this.AsyncExecuteWrapper);
            this.asyncResult = this.caller.BeginInvoke(1, behavior, null, null);
            return this.asyncResult;
        }

        public override void Cancel()
        {
            this.connection.CancelQuery(this.connection.ConnectionTimeout);
        }

        internal void ClearCommandTimer()
        {
            if (this.commandTimer != null)
            {
                this.commandTimer.Dispose();
                this.commandTimer = null;
            }
        }

        public MySqlCommand Clone()
        {
            MySqlCommand command = new MySqlCommand(this.cmdText, this.connection, this.curTransaction);
            command.CommandType = this.CommandType;
            command.commandTimeout = this.commandTimeout;
            command.useDefaultTimeout = this.useDefaultTimeout;
            command.batchableCommandText = this.batchableCommandText;
            command.UpdatedRowSource = this.UpdatedRowSource;
            foreach (MySqlParameter parameter in this.parameters)
            {
                command.Parameters.Add(parameter.Clone());
            }
            return command;
        }

        internal void Close(MySqlDataReader reader)
        {
            if (this.statement != null)
            {
                this.statement.Close(reader);
            }
            this.ResetSqlSelectLimit();
            if (((this.statement != null) && (this.connection != null)) && (this.connection.driver != null))
            {
                this.connection.driver.CloseQuery(this.connection, this.statement.StatementId);
            }
            this.ClearCommandTimer();
        }

        protected override DbParameter CreateDbParameter()
        {
            return new MySqlParameter();
        }

        public MySqlParameter CreateParameter()
        {
            return (MySqlParameter) this.CreateDbParameter();
        }

        protected override void Dispose(bool disposing)
        {
            if ((disposing && (this.statement != null)) && this.statement.IsPrepared)
            {
                this.statement.CloseStatement();
            }
            base.Dispose(disposing);
        }

        public int EndExecuteNonQuery(IAsyncResult asyncResult)
        {
            asyncResult.AsyncWaitHandle.WaitOne();
            AsyncDelegate caller = this.caller;
            this.caller = null;
            if (this.thrownException != null)
            {
                throw this.thrownException;
            }
            return (int) caller.EndInvoke(asyncResult);
        }

        public MySqlDataReader EndExecuteReader(IAsyncResult result)
        {
            result.AsyncWaitHandle.WaitOne();
            AsyncDelegate caller = this.caller;
            this.caller = null;
            if (this.thrownException != null)
            {
                throw this.thrownException;
            }
            return (MySqlDataReader) caller.EndInvoke(result);
        }

        internal long EstimatedSize()
        {
            long length = this.CommandText.Length;
            foreach (MySqlParameter parameter in this.Parameters)
            {
                length += parameter.EstimatedSize();
            }
            return length;
        }

        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
        {
            return this.ExecuteReader(behavior);
        }

        public override int ExecuteNonQuery()
        {
            using (MySqlDataReader reader = this.ExecuteReader())
            {
                reader.Close();
                return reader.RecordsAffected;
            }
        }

        public MySqlDataReader ExecuteReader()
        {
            return this.ExecuteReader(CommandBehavior.Default);
        }

        public MySqlDataReader ExecuteReader(CommandBehavior behavior)
        {
            MySqlDataReader reader2;
            bool flag = false;
            this.CheckState();
            Driver driver = this.connection.driver;
            lock (driver)
            {
                if (this.connection.Reader != null)
                {
                    throw new MySqlException(Resources.DataReaderOpen);
                }
                System.Transactions.Transaction current = System.Transactions.Transaction.Current;
                if (current != null)
                {
                    bool inRollback = false;
                    if (driver.CurrentTransaction != null)
                    {
                        inRollback = driver.CurrentTransaction.InRollback;
                    }
                    if (!inRollback)
                    {
                        TransactionStatus inDoubt = TransactionStatus.InDoubt;
                        try
                        {
                            inDoubt = current.TransactionInformation.Status;
                        }
                        catch (TransactionException)
                        {
                        }
                        if (inDoubt == TransactionStatus.Aborted)
                        {
                            throw new TransactionAbortedException();
                        }
                    }
                }
                this.commandTimer = new CommandTimer(this.connection, this.CommandTimeout);
                this.lastInsertedId = -1L;
                if ((this.cmdText == null) || (this.cmdText.Trim().Length == 0))
                {
                    throw new InvalidOperationException(Resources.CommandTextNotInitialized);
                }
                string text = TrimSemicolons(this.cmdText);
                if (this.CommandType == System.Data.CommandType.TableDirect)
                {
                    text = "SELECT * FROM " + text;
                }
                if ((this.statement == null) || !this.statement.IsPrepared)
                {
                    if (this.CommandType == System.Data.CommandType.StoredProcedure)
                    {
                        this.statement = new StoredProcedure(this, text);
                    }
                    else
                    {
                        this.statement = new PreparableStatement(this, text);
                    }
                }
                this.statement.Resolve(false);
                this.HandleCommandBehaviors(behavior);
                this.updatedRowCount = -1L;
                try
                {
                    MySqlDataReader reader = new MySqlDataReader(this, this.statement, behavior);
                    this.connection.Reader = reader;
                    this.statement.Execute();
                    reader.NextResult();
                    flag = true;
                    reader2 = reader;
                }
                catch (TimeoutException exception)
                {
                    this.connection.HandleTimeoutOrThreadAbort(exception);
                    throw;
                }
                catch (ThreadAbortException exception2)
                {
                    this.connection.HandleTimeoutOrThreadAbort(exception2);
                    throw;
                }
                catch (IOException exception3)
                {
                    this.connection.Abort();
                    throw new MySqlException(Resources.FatalErrorDuringExecute, exception3);
                }
                catch (MySqlException exception4)
                {
                    if (exception4.InnerException is TimeoutException)
                    {
                        throw;
                    }
                    try
                    {
                        this.ResetReader();
                        this.ResetSqlSelectLimit();
                    }
                    catch (Exception)
                    {
                        this.Connection.Abort();
                        throw new MySqlException(exception4.Message, true, exception4);
                    }
                    if (exception4.IsFatal)
                    {
                        this.Connection.Close();
                    }
                    if (exception4.Number == 0)
                    {
                        throw new MySqlException(Resources.FatalErrorDuringExecute, exception4);
                    }
                    throw;
                }
                finally
                {
                    if (this.connection != null)
                    {
                        if (this.connection.Reader == null)
                        {
                            this.ClearCommandTimer();
                        }
                        if (!flag)
                        {
                            this.ResetReader();
                        }
                    }
                }
            }
            return reader2;
        }

        public override object ExecuteScalar()
        {
            this.lastInsertedId = -1L;
            object obj2 = null;
            using (MySqlDataReader reader = this.ExecuteReader())
            {
                if (reader.Read())
                {
                    obj2 = reader.GetValue(0);
                }
            }
            return obj2;
        }

        internal string GetCommandTextForBatching()
        {
            if ((this.batchableCommandText == null) && (string.Compare(this.CommandText.Substring(0, 6), "INSERT", true) == 0))
            {
                MySqlCommand command = new MySqlCommand("SELECT @@sql_mode", this.Connection);
                string str = command.ExecuteScalar().ToString().ToUpper(CultureInfo.InvariantCulture);
                MySqlTokenizer tokenizer = new MySqlTokenizer(this.CommandText);
                tokenizer.AnsiQuotes = str.IndexOf("ANSI_QUOTES") != -1;
                tokenizer.BackslashEscapes = str.IndexOf("NO_BACKSLASH_ESCAPES") == -1;
                for (string str2 = tokenizer.NextToken().ToLower(CultureInfo.InvariantCulture); str2 != null; str2 = tokenizer.NextToken())
                {
                    if ((str2.ToUpper(CultureInfo.InvariantCulture) == "VALUES") && !tokenizer.Quoted)
                    {
                        str2 = tokenizer.NextToken();
                        int num = 1;
                        while (str2 != null)
                        {
                            this.batchableCommandText = this.batchableCommandText + str2;
                            str2 = tokenizer.NextToken();
                            if (str2 == "(")
                            {
                                num++;
                            }
                            else if (str2 == ")")
                            {
                                num--;
                            }
                            if (num == 0)
                            {
                                break;
                            }
                        }
                        if (str2 != null)
                        {
                            this.batchableCommandText = this.batchableCommandText + str2;
                        }
                        str2 = tokenizer.NextToken();
                        if ((str2 != null) && ((str2 == ",") || (str2.ToUpper(CultureInfo.InvariantCulture) == "ON")))
                        {
                            this.batchableCommandText = null;
                            break;
                        }
                    }
                }
            }
            return this.batchableCommandText;
        }

        private void HandleCommandBehaviors(CommandBehavior behavior)
        {
            if ((behavior & CommandBehavior.SchemaOnly) != CommandBehavior.Default)
            {
                new MySqlCommand("SET SQL_SELECT_LIMIT=0", this.connection).ExecuteNonQuery();
                this.resetSqlSelect = true;
            }
            else if ((behavior & CommandBehavior.SingleRow) != CommandBehavior.Default)
            {
                new MySqlCommand("SET SQL_SELECT_LIMIT=1", this.connection).ExecuteNonQuery();
                this.resetSqlSelect = true;
            }
        }

        private void CheckState()
        {
            if (this.connection == null)
            {
                throw new InvalidOperationException("Connection must be valid and open.");
            }
            if ((this.connection.State != ConnectionState.Open) && !this.connection.SoftClosed)
            {
                throw new InvalidOperationException("Connection must be valid and open.");
            }
            if (this.connection.Reader != null)
            {
                throw new MySqlException("There is already an open DataReader associated with this Connection which must be closed first.");
            }
        }

        public override void Prepare()
        {
            if (this.connection == null)
            {
                throw new InvalidOperationException("The connection property has not been set.");
            }
            if (this.connection.State != ConnectionState.Open)
            {
                throw new InvalidOperationException("The connection is not open.");
            }
            if (!this.connection.Settings.IgnorePrepare)
            {
                this.Prepare(0);
            }
        }

        private void Prepare(int cursorPageSize)
        {
            using (new CommandTimer(this.Connection, this.CommandTimeout))
            {
                string commandText = this.CommandText;
                if ((commandText != null) && (commandText.Trim().Length != 0))
                {
                    if (this.CommandType == System.Data.CommandType.StoredProcedure)
                    {
                        this.statement = new StoredProcedure(this, this.CommandText);
                    }
                    else
                    {
                        this.statement = new PreparableStatement(this, this.CommandText);
                    }
                    this.statement.Resolve(true);
                    this.statement.Prepare();
                }
            }
        }

        private void ResetReader()
        {
            if ((this.connection != null) && (this.connection.Reader != null))
            {
                this.connection.Reader.Close();
                this.connection.Reader = null;
            }
        }

        internal void ResetSqlSelectLimit()
        {
            if (this.resetSqlSelect)
            {
                this.resetSqlSelect = false;
                new MySqlCommand("SET SQL_SELECT_LIMIT=DEFAULT", this.connection).ExecuteNonQuery();
            }
        }

        object ICloneable.Clone()
        {
            return this.Clone();
        }

        private static string TrimSemicolons(string sql)
        {
            int startIndex = 0;
            while (sql[startIndex] == ';')
            {
                startIndex++;
            }
            int num2 = sql.Length - 1;
            while (sql[num2] == ';')
            {
                num2--;
            }
            return sql.Substring(startIndex, (num2 - startIndex) + 1);
        }

        internal List<MySqlCommand> Batch
        {
            get
            {
                return this.batch;
            }
        }

        internal string BatchableCommandText
        {
            get
            {
                return this.batchableCommandText;
            }
        }

        [Category("Data"), Editor("MySql.Data.Common.Design.SqlCommandTextEditor,MySqlClient.Design", typeof(UITypeEditor)), Description("Command text to execute")]
        public override string CommandText
        {
            get
            {
                return this.cmdText;
            }
            set
            {
                this.cmdText = value;
                this.statement = null;
                this.batchableCommandText = null;
                if ((this.cmdText != null) && this.cmdText.EndsWith("DEFAULT VALUES"))
                {
                    this.cmdText = this.cmdText.Substring(0, this.cmdText.Length - 14);
                    this.cmdText = this.cmdText + "() VALUES ()";
                }
            }
        }

        [Category("Misc"), DefaultValue(30), Description("Time to wait for command to execute")]
        public override int CommandTimeout
        {
            get
            {
                if (!this.useDefaultTimeout)
                {
                    return this.commandTimeout;
                }
                return 30;
            }
            set
            {
                if (this.commandTimeout < 0)
                {
                    throw new ArgumentException("Command timeout must not be negative");
                }
                int num = Math.Min(value, 0x20c49b);
                if (num != value)
                {
                    MySqlTrace.LogWarning(this.connection.ServerThread, string.Concat(new object[] { "Command timeout value too large (", value, " seconds). Changed to max. possible value (", num, " seconds)" }));
                }
                this.commandTimeout = num;
                this.useDefaultTimeout = false;
            }
        }

        [Category("Data")]
        public override System.Data.CommandType CommandType
        {
            get
            {
                return this.cmdType;
            }
            set
            {
                this.cmdType = value;
            }
        }

        [Category("Behavior"), Description("Connection used by the command")]
        public MySqlConnection Connection
        {
            get
            {
                return this.connection;
            }
            set
            {
                if (this.connection != value)
                {
                    this.Transaction = null;
                }
                this.connection = value;
                if ((this.connection != null) && this.useDefaultTimeout)
                {
                    this.commandTimeout = (int) this.connection.Settings.DefaultCommandTimeout;
                    this.useDefaultTimeout = false;
                }
            }
        }

        protected override System.Data.Common.DbConnection DbConnection
        {
            get
            {
                return this.Connection;
            }
            set
            {
                this.Connection = (MySqlConnection) value;
            }
        }

        protected override System.Data.Common.DbParameterCollection DbParameterCollection
        {
            get
            {
                return this.Parameters;
            }
        }

        protected override System.Data.Common.DbTransaction DbTransaction
        {
            get
            {
                return this.Transaction;
            }
            set
            {
                this.Transaction = (MySqlTransaction) value;
            }
        }

        [Browsable(false)]
        public override bool DesignTimeVisible
        {
            get
            {
                return this.designTimeVisible;
            }
            set
            {
                this.designTimeVisible = value;
            }
        }

        [Browsable(false)]
        public bool IsPrepared
        {
            get
            {
                return ((this.statement != null) && this.statement.IsPrepared);
            }
        }

        [Browsable(false)]
        public long LastInsertedId
        {
            get
            {
                return this.lastInsertedId;
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content), Description("The parameters collection"), Category("Data")]
        public MySqlParameterCollection Parameters
        {
            get
            {
                return this.parameters;
            }
        }

        [Browsable(false)]
        public MySqlTransaction Transaction
        {
            get
            {
                return this.curTransaction;
            }
            set
            {
                this.curTransaction = value;
            }
        }

        public override UpdateRowSource UpdatedRowSource
        {
            get
            {
                return this.updatedRowSource;
            }
            set
            {
                this.updatedRowSource = value;
            }
        }

        internal delegate object AsyncDelegate(int type, CommandBehavior behavior);
    }
}

