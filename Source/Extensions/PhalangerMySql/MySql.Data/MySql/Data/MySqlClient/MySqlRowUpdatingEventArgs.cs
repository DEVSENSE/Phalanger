namespace MySql.Data.MySqlClient
{
    using System;
    using System.Data;
    using System.Data.Common;

    public sealed class MySqlRowUpdatingEventArgs : RowUpdatingEventArgs
    {
        public MySqlRowUpdatingEventArgs(DataRow row, IDbCommand command, StatementType statementType, DataTableMapping tableMapping) : base(row, command, statementType, tableMapping)
        {
        }

        public MySqlCommand Command
        {
            get
            {
                return (MySqlCommand) base.Command;
            }
            set
            {
                base.Command = value;
            }
        }
    }
}

