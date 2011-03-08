namespace MySql.Data.MySqlClient
{
    using System;

    public class MySqlInfoMessageEventArgs : EventArgs
    {
        public MySqlError[] errors;
    }
}

