namespace MySql.Data.MySqlClient
{
    using System;

    internal class CommandTimer : IDisposable
    {
        private MySqlConnection connection;
        private bool timeoutSet;

        public CommandTimer(MySqlConnection connection, int timeout)
        {
            this.connection = connection;
            if (connection != null)
            {
                this.timeoutSet = connection.SetCommandTimeout(timeout);
            }
        }

        public void Dispose()
        {
            if (this.timeoutSet)
            {
                this.timeoutSet = false;
                this.connection.ClearCommandTimeout();
                this.connection = null;
            }
        }
    }
}

