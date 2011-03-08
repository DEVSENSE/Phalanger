namespace MySql.Data.MySqlClient
{
    using System;

    public class MySqlScriptErrorEventArgs : MySqlScriptEventArgs
    {
        private System.Exception exception;
        private bool ignore;

        public MySqlScriptErrorEventArgs(System.Exception exception)
        {
            this.exception = exception;
        }

        public System.Exception Exception
        {
            get
            {
                return this.exception;
            }
        }

        public bool Ignore
        {
            get
            {
                return this.ignore;
            }
            set
            {
                this.ignore = value;
            }
        }
    }
}

