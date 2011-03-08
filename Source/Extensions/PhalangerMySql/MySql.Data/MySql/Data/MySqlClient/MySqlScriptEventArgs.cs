namespace MySql.Data.MySqlClient
{
    using System;

    public class MySqlScriptEventArgs : EventArgs
    {
        private ScriptStatement statement;

        public int Line
        {
            get
            {
                return this.statement.line;
            }
        }

        public int Position
        {
            get
            {
                return this.statement.position;
            }
        }

        internal ScriptStatement Statement
        {
            set
            {
                this.statement = value;
            }
        }

        public string StatementText
        {
            get
            {
                return this.statement.text;
            }
        }
    }
}

