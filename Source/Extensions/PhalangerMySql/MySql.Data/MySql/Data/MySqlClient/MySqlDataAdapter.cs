namespace MySql.Data.MySqlClient
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Data;
    using System.Data.Common;
    using System.Drawing;
    using System.Threading;

    [ToolboxBitmap(typeof(MySqlDataAdapter), "MySqlClient.resources.dataadapter.bmp"), Designer("MySql.Data.MySqlClient.Design.MySqlDataAdapterDesigner,MySqlClient.Design"), DesignerCategory("Code")]
    public sealed class MySqlDataAdapter : DbDataAdapter, IDbDataAdapter, IDataAdapter, ICloneable
    {
        private List<IDbCommand> commandBatch;
        private bool loadingDefaults;
        private MySqlRowUpdatedEventHandler _RowUpdated;
        private MySqlRowUpdatingEventHandler _RowUpdating;
        private int updateBatchSize;

        public event MySqlRowUpdatedEventHandler RowUpdated
        {
            add
            {
                MySqlRowUpdatedEventHandler handler2;
                MySqlRowUpdatedEventHandler rowUpdated = this._RowUpdated;
                do
                {
                    handler2 = rowUpdated;
                    MySqlRowUpdatedEventHandler handler3 = (MySqlRowUpdatedEventHandler) Delegate.Combine(handler2, value);
                    rowUpdated = Interlocked.CompareExchange<MySqlRowUpdatedEventHandler>(ref this._RowUpdated, handler3, handler2);
                }
                while (rowUpdated != handler2);
            }
            remove
            {
                MySqlRowUpdatedEventHandler handler2;
                MySqlRowUpdatedEventHandler rowUpdated = this._RowUpdated;
                do
                {
                    handler2 = rowUpdated;
                    MySqlRowUpdatedEventHandler handler3 = (MySqlRowUpdatedEventHandler) Delegate.Remove(handler2, value);
                    rowUpdated = Interlocked.CompareExchange<MySqlRowUpdatedEventHandler>(ref this._RowUpdated, handler3, handler2);
                }
                while (rowUpdated != handler2);
            }
        }

        public event MySqlRowUpdatingEventHandler RowUpdating
        {
            add
            {
                MySqlRowUpdatingEventHandler handler2;
                MySqlRowUpdatingEventHandler rowUpdating = this._RowUpdating;
                do
                {
                    handler2 = rowUpdating;
                    MySqlRowUpdatingEventHandler handler3 = (MySqlRowUpdatingEventHandler) Delegate.Combine(handler2, value);
                    rowUpdating = Interlocked.CompareExchange<MySqlRowUpdatingEventHandler>(ref this._RowUpdating, handler3, handler2);
                }
                while (rowUpdating != handler2);
            }
            remove
            {
                MySqlRowUpdatingEventHandler handler2;
                MySqlRowUpdatingEventHandler rowUpdating = this._RowUpdating;
                do
                {
                    handler2 = rowUpdating;
                    MySqlRowUpdatingEventHandler handler3 = (MySqlRowUpdatingEventHandler) Delegate.Remove(handler2, value);
                    rowUpdating = Interlocked.CompareExchange<MySqlRowUpdatingEventHandler>(ref this._RowUpdating, handler3, handler2);
                }
                while (rowUpdating != handler2);
            }
        }

        public MySqlDataAdapter()
        {
            this.loadingDefaults = true;
            this.updateBatchSize = 1;
        }

        public MySqlDataAdapter(MySqlCommand selectCommand) : this()
        {
            this.SelectCommand = selectCommand;
        }

        public MySqlDataAdapter(string selectCommandText, MySqlConnection connection) : this()
        {
            this.SelectCommand = new MySqlCommand(selectCommandText, connection);
        }

        public MySqlDataAdapter(string selectCommandText, string selectConnString) : this()
        {
            this.SelectCommand = new MySqlCommand(selectCommandText, new MySqlConnection(selectConnString));
        }

        protected override int AddToBatch(IDbCommand command)
        {
            MySqlCommand command2 = (MySqlCommand) command;
            if (command2.BatchableCommandText == null)
            {
                command2.GetCommandTextForBatching();
            }
            IDbCommand item = (IDbCommand) ((ICloneable) command).Clone();
            this.commandBatch.Add(item);
            return (this.commandBatch.Count - 1);
        }

        protected override void ClearBatch()
        {
            if (this.commandBatch.Count > 0)
            {
                MySqlCommand command = (MySqlCommand) this.commandBatch[0];
                if (command.Batch != null)
                {
                    command.Batch.Clear();
                }
            }
            this.commandBatch.Clear();
        }

        protected override RowUpdatedEventArgs CreateRowUpdatedEvent(DataRow dataRow, IDbCommand command, StatementType statementType, DataTableMapping tableMapping)
        {
            return new MySqlRowUpdatedEventArgs(dataRow, command, statementType, tableMapping);
        }

        protected override RowUpdatingEventArgs CreateRowUpdatingEvent(DataRow dataRow, IDbCommand command, StatementType statementType, DataTableMapping tableMapping)
        {
            return new MySqlRowUpdatingEventArgs(dataRow, command, statementType, tableMapping);
        }

        protected override int ExecuteBatch()
        {
            int num = 0;
            int num2 = 0;
            while (num2 < this.commandBatch.Count)
            {
                MySqlCommand command = (MySqlCommand) this.commandBatch[num2++];
                int num3 = num2;
                while (num3 < this.commandBatch.Count)
                {
                    MySqlCommand command2 = (MySqlCommand) this.commandBatch[num3];
                    if ((command2.BatchableCommandText == null) || (command2.CommandText != command.CommandText))
                    {
                        break;
                    }
                    command.AddToBatch(command2);
                    num3++;
                    num2++;
                }
                num += command.ExecuteNonQuery();
            }
            return num;
        }

        protected override IDataParameter GetBatchedParameter(int commandIdentifier, int parameterIndex)
        {
            return (IDataParameter) this.commandBatch[commandIdentifier].Parameters[parameterIndex];
        }

        protected override void InitializeBatching()
        {
            this.commandBatch = new List<IDbCommand>();
        }

        protected override void OnRowUpdated(RowUpdatedEventArgs value)
        {
            if (this._RowUpdated != null)
            {
                this._RowUpdated(this, value as MySqlRowUpdatedEventArgs);
            }
        }

        protected override void OnRowUpdating(RowUpdatingEventArgs value)
        {
            if (this._RowUpdating != null)
            {
                this._RowUpdating(this, value as MySqlRowUpdatingEventArgs);
            }
        }

        private void OpenConnectionIfClosed(DataRowState state, List<MySqlConnection> openedConnections)
        {
            MySqlCommand deleteCommand = null;
            DataRowState state2 = state;
            if (state2 != DataRowState.Added)
            {
                if (state2 != DataRowState.Deleted)
                {
                    if (state2 != DataRowState.Modified)
                    {
                        return;
                    }
                    deleteCommand = this.UpdateCommand;
                    goto Label_002B;
                }
            }
            else
            {
                deleteCommand = this.InsertCommand;
                goto Label_002B;
            }
            deleteCommand = this.DeleteCommand;
        Label_002B:
            if (((deleteCommand != null) && (deleteCommand.Connection != null)) && (deleteCommand.Connection.connectionState == ConnectionState.Closed))
            {
                deleteCommand.Connection.Open();
                openedConnections.Add(deleteCommand.Connection);
            }
        }

        protected override void TerminateBatching()
        {
            this.ClearBatch();
            this.commandBatch = null;
        }

        protected override int Update(DataRow[] dataRows, DataTableMapping tableMapping)
        {
            int num2;
            List<MySqlConnection> openedConnections = new List<MySqlConnection>();
            try
            {
                foreach (DataRow row in dataRows)
                {
                    this.OpenConnectionIfClosed(row.RowState, openedConnections);
                }
                int num = base.Update(dataRows, tableMapping);
                foreach (DataRow row2 in dataRows)
                {
                    if ((row2.RowState != DataRowState.Unchanged) && (row2.RowState != DataRowState.Detached))
                    {
                        row2.AcceptChanges();
                    }
                }
                num2 = num;
            }
            finally
            {
                foreach (MySqlConnection connection in openedConnections)
                {
                    connection.Close();
                }
            }
            return num2;
        }

        [Description("Used during Update for deleted rows in Dataset.")]
        public MySqlCommand DeleteCommand
        {
            get
            {
                return (MySqlCommand) base.DeleteCommand;
            }
            set
            {
                base.DeleteCommand = value;
            }
        }

        [Description("Used during Update for new rows in Dataset.")]
        public MySqlCommand InsertCommand
        {
            get
            {
                return (MySqlCommand) base.InsertCommand;
            }
            set
            {
                base.InsertCommand = value;
            }
        }

        internal bool LoadDefaults
        {
            get
            {
                return this.loadingDefaults;
            }
            set
            {
                this.loadingDefaults = value;
            }
        }

        [Description("Used during Fill/FillSchema"), Category("Fill")]
        public MySqlCommand SelectCommand
        {
            get
            {
                return (MySqlCommand) base.SelectCommand;
            }
            set
            {
                base.SelectCommand = value;
            }
        }

        public override int UpdateBatchSize
        {
            get
            {
                return this.updateBatchSize;
            }
            set
            {
                this.updateBatchSize = value;
            }
        }

        [Description("Used during Update for modified rows in Dataset.")]
        public MySqlCommand UpdateCommand
        {
            get
            {
                return (MySqlCommand) base.UpdateCommand;
            }
            set
            {
                base.UpdateCommand = value;
            }
        }
    }
}

