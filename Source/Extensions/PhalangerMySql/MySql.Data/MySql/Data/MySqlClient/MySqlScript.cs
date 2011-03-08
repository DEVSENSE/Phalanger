namespace MySql.Data.MySqlClient
{
    using MySql.Data.MySqlClient.Properties;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Globalization;
    using System.IO;
    using System.Threading;

    public class MySqlScript
    {
        private MySqlConnection connection;
        private string delimiter;
        private MySqlScriptErrorEventHandler _Error;
        private string query;
        private EventHandler _ScriptCompleted;
        private MySqlStatementExecutedEventHandler _StatementExecuted;

        public event MySqlScriptErrorEventHandler Error
        {
            add
            {
                MySqlScriptErrorEventHandler handler2;
                MySqlScriptErrorEventHandler error = this._Error;
                do
                {
                    handler2 = error;
                    MySqlScriptErrorEventHandler handler3 = (MySqlScriptErrorEventHandler) Delegate.Combine(handler2, value);
                    error = Interlocked.CompareExchange<MySqlScriptErrorEventHandler>(ref this._Error, handler3, handler2);
                }
                while (error != handler2);
            }
            remove
            {
                MySqlScriptErrorEventHandler handler2;
                MySqlScriptErrorEventHandler error = this._Error;
                do
                {
                    handler2 = error;
                    MySqlScriptErrorEventHandler handler3 = (MySqlScriptErrorEventHandler) Delegate.Remove(handler2, value);
                    error = Interlocked.CompareExchange<MySqlScriptErrorEventHandler>(ref this._Error, handler3, handler2);
                }
                while (error != handler2);
            }
        }

        public event EventHandler ScriptCompleted
        {
            add
            {
                EventHandler handler2;
                EventHandler scriptCompleted = this._ScriptCompleted;
                do
                {
                    handler2 = scriptCompleted;
                    EventHandler handler3 = (EventHandler) Delegate.Combine(handler2, value);
                    scriptCompleted = Interlocked.CompareExchange<EventHandler>(ref this._ScriptCompleted, handler3, handler2);
                }
                while (scriptCompleted != handler2);
            }
            remove
            {
                EventHandler handler2;
                EventHandler scriptCompleted = this._ScriptCompleted;
                do
                {
                    handler2 = scriptCompleted;
                    EventHandler handler3 = (EventHandler) Delegate.Remove(handler2, value);
                    scriptCompleted = Interlocked.CompareExchange<EventHandler>(ref this._ScriptCompleted, handler3, handler2);
                }
                while (scriptCompleted != handler2);
            }
        }

        public event MySqlStatementExecutedEventHandler StatementExecuted
        {
            add
            {
                MySqlStatementExecutedEventHandler handler2;
                MySqlStatementExecutedEventHandler statementExecuted = this._StatementExecuted;
                do
                {
                    handler2 = statementExecuted;
                    MySqlStatementExecutedEventHandler handler3 = (MySqlStatementExecutedEventHandler) Delegate.Combine(handler2, value);
                    statementExecuted = Interlocked.CompareExchange<MySqlStatementExecutedEventHandler>(ref this._StatementExecuted, handler3, handler2);
                }
                while (statementExecuted != handler2);
            }
            remove
            {
                MySqlStatementExecutedEventHandler handler2;
                MySqlStatementExecutedEventHandler statementExecuted = this._StatementExecuted;
                do
                {
                    handler2 = statementExecuted;
                    MySqlStatementExecutedEventHandler handler3 = (MySqlStatementExecutedEventHandler) Delegate.Remove(handler2, value);
                    statementExecuted = Interlocked.CompareExchange<MySqlStatementExecutedEventHandler>(ref this._StatementExecuted, handler3, handler2);
                }
                while (statementExecuted != handler2);
            }
        }

        public MySqlScript()
        {
            this.Delimiter = ";";
        }

        public MySqlScript(MySqlConnection connection) : this()
        {
            this.connection = connection;
        }

        public MySqlScript(string query) : this()
        {
            this.query = query;
        }

        public MySqlScript(MySqlConnection connection, string query) : this()
        {
            this.connection = connection;
            this.query = query;
        }

        private void AdjustDelimiterEnd(MySqlTokenizer tokenizer)
        {
            int stopIndex = tokenizer.StopIndex;
            for (char ch = this.query[stopIndex]; !char.IsWhiteSpace(ch) && (stopIndex < (this.query.Length - 1)); ch = this.query[++stopIndex])
            {
            }
            tokenizer.StopIndex = stopIndex;
            tokenizer.Position = stopIndex;
        }

        private List<ScriptStatement> BreakIntoStatements(bool ansiQuotes, bool noBackslashEscapes)
        {
            string delimiter = this.Delimiter;
            int startIndex = 0;
            List<ScriptStatement> list = new List<ScriptStatement>();
            List<int> lineNumbers = this.BreakScriptIntoLines();
            MySqlTokenizer tokenizer = new MySqlTokenizer(this.query);
            tokenizer.AnsiQuotes = ansiQuotes;
            tokenizer.BackslashEscapes = !noBackslashEscapes;
            for (string str2 = tokenizer.NextToken(); str2 != null; str2 = tokenizer.NextToken())
            {
                if (!tokenizer.Quoted)
                {
                    if (str2.ToLower(CultureInfo.InvariantCulture) == "delimiter")
                    {
                        tokenizer.NextToken();
                        this.AdjustDelimiterEnd(tokenizer);
                        delimiter = this.query.Substring(tokenizer.StartIndex, (tokenizer.StopIndex - tokenizer.StartIndex) + 1).Trim();
                        startIndex = tokenizer.StopIndex;
                    }
                    else
                    {
                        if ((delimiter.StartsWith(str2) && ((tokenizer.StartIndex + delimiter.Length) <= this.query.Length)) && (this.query.Substring(tokenizer.StartIndex, delimiter.Length) == delimiter))
                        {
                            str2 = delimiter;
                            tokenizer.Position = tokenizer.StartIndex + delimiter.Length;
                            tokenizer.StopIndex = tokenizer.Position;
                        }
                        int index = str2.IndexOf(delimiter, StringComparison.InvariantCultureIgnoreCase);
                        if (index != -1)
                        {
                            int num3 = (tokenizer.StopIndex - str2.Length) + index;
                            if (tokenizer.StopIndex == (this.query.Length - 1))
                            {
                                num3++;
                            }
                            string str3 = this.query.Substring(startIndex, num3 - startIndex);
                            ScriptStatement item = new ScriptStatement();
                            item.text = str3.Trim();
                            item.line = FindLineNumber(startIndex, lineNumbers);
                            item.position = startIndex - lineNumbers[item.line];
                            list.Add(item);
                            startIndex = num3 + delimiter.Length;
                        }
                    }
                }
            }
            if (startIndex < (this.query.Length - 1))
            {
                string str4 = this.query.Substring(startIndex).Trim();
                if (!string.IsNullOrEmpty(str4))
                {
                    ScriptStatement statement2 = new ScriptStatement();
                    statement2.text = str4;
                    statement2.line = FindLineNumber(startIndex, lineNumbers);
                    statement2.position = startIndex - lineNumbers[statement2.line];
                    list.Add(statement2);
                }
            }
            return list;
        }

        private List<int> BreakScriptIntoLines()
        {
            List<int> list = new List<int>();
            StringReader reader = new StringReader(this.query);
            string str = reader.ReadLine();
            int item = 0;
            while (str != null)
            {
                list.Add(item);
                item += str.Length;
                str = reader.ReadLine();
            }
            return list;
        }

        public int Execute()
        {
            int num2;
            bool flag = false;
            if (this.connection == null)
            {
                throw new InvalidOperationException(Resources.ConnectionNotSet);
            }
            if ((this.query == null) || (this.query.Length == 0))
            {
                return 0;
            }
            if (this.connection.State != ConnectionState.Open)
            {
                flag = true;
                this.connection.Open();
            }
            bool allowUserVariables = this.connection.Settings.AllowUserVariables;
            this.connection.Settings.AllowUserVariables = true;
            try
            {
                string str = this.connection.driver.Property("sql_mode").ToUpper(CultureInfo.InvariantCulture);
                bool ansiQuotes = str.IndexOf("ANSI_QUOTES") != -1;
                bool noBackslashEscapes = str.IndexOf("NO_BACKSLASH_ESCAPES") != -1;
                List<ScriptStatement> list = this.BreakIntoStatements(ansiQuotes, noBackslashEscapes);
                int num = 0;
                MySqlCommand command = new MySqlCommand(null, this.connection);
                foreach (ScriptStatement statement in list)
                {
                    if (!string.IsNullOrEmpty(statement.text))
                    {
                        command.CommandText = statement.text;
                        try
                        {
                            command.ExecuteNonQuery();
                            num++;
                            this.OnQueryExecuted(statement);
                            continue;
                        }
                        catch (Exception exception)
                        {
                            if (this._Error == null)
                            {
                                throw;
                            }
                            if (!this.OnScriptError(exception))
                            {
                                break;
                            }
                            continue;
                        }
                    }
                }
                this.OnScriptCompleted();
                num2 = num;
            }
            finally
            {
                this.connection.Settings.AllowUserVariables = allowUserVariables;
                if (flag)
                {
                    this.connection.Close();
                }
            }
            return num2;
        }

        private static int FindLineNumber(int position, List<int> lineNumbers)
        {
            int num = 0;
            while ((num < lineNumbers.Count) && (position < lineNumbers[num]))
            {
                num++;
            }
            return num;
        }

        private void OnQueryExecuted(ScriptStatement statement)
        {
            if (this._StatementExecuted != null)
            {
                MySqlScriptEventArgs args = new MySqlScriptEventArgs();
                args.Statement = statement;
                this._StatementExecuted(this, args);
            }
        }

        private void OnScriptCompleted()
        {
            if (this._ScriptCompleted != null)
            {
                this._ScriptCompleted(this, EventArgs.Empty);
            }
        }

        private bool OnScriptError(Exception ex)
        {
            if (this._Error != null)
            {
                MySqlScriptErrorEventArgs args = new MySqlScriptErrorEventArgs(ex);
                this._Error(this, args);
                return args.Ignore;
            }
            return false;
        }

        public MySqlConnection Connection
        {
            get
            {
                return this.connection;
            }
            set
            {
                this.connection = value;
            }
        }

        public string Delimiter
        {
            get
            {
                return this.delimiter;
            }
            set
            {
                this.delimiter = value;
            }
        }

        public string Query
        {
            get
            {
                return this.query;
            }
            set
            {
                this.query = value;
            }
        }
    }
}

