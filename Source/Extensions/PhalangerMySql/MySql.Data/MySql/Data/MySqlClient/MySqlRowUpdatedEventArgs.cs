namespace MySql.Data.MySqlClient
{
    using System;
    using System.Data;
    using System.Data.Common;

    public sealed class MySqlRowUpdatedEventArgs : RowUpdatedEventArgs
    {
        public MySqlRowUpdatedEventArgs(DataRow row, IDbCommand command, StatementType statementType, DataTableMapping tableMapping) : base(row, command, statementType, tableMapping)
        {
        }

        public MySqlCommand Command
        {
            get
            {
                return (MySqlCommand) base.Command;
            }
        }
    }
}

