namespace MySql.Data.MySqlClient
{
    using System;

    public class MySqlError
    {
        private int code;
        private string level;
        private string message;

        public MySqlError(string level, int code, string message)
        {
            this.level = level;
            this.code = code;
            this.message = message;
        }

        public int Code
        {
            get
            {
                return this.code;
            }
        }

        public string Level
        {
            get
            {
                return this.level;
            }
        }

        public string Message
        {
            get
            {
                return this.message;
            }
        }
    }
}

