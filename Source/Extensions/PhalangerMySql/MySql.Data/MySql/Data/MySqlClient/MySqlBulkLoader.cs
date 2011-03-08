namespace MySql.Data.MySqlClient
{
    using MySql.Data.MySqlClient.Properties;
    using System;
    using System.Collections.Specialized;
    using System.Data;
    using System.IO;
    using System.Text;

    public class MySqlBulkLoader
    {
        private StringCollection columns;
        private MySqlBulkLoaderConflictOption conflictOption;
        private MySqlConnection connection;
        private const char defaultEscapeCharacter = '\\';
        private const string defaultFieldTerminator = "\t";
        private const string defaultLineTerminator = "\n";
        private char escapeChar;
        private StringCollection expressions;
        private char fieldQuotationCharacter;
        private bool fieldQuotationOptional;
        private string fieldTerminator;
        private string filename;
        private string charSet;
        private string linePrefix;
        private string lineTerminator;
        private bool local;
        private int numLinesToIgnore;
        private MySqlBulkLoaderPriority priority;
        private string tableName;
        private int timeout;

        public MySqlBulkLoader(MySqlConnection connection)
        {
            this.Connection = connection;
            this.Local = true;
            this.FieldTerminator = "\t";
            this.LineTerminator = "\n";
            this.FieldQuotationCharacter = '\0';
            this.ConflictOption = MySqlBulkLoaderConflictOption.None;
            this.columns = new StringCollection();
            this.expressions = new StringCollection();
        }

        private string BuildSqlCommand()
        {
            StringBuilder builder = new StringBuilder("LOAD DATA ");
            if (this.Priority == MySqlBulkLoaderPriority.Low)
            {
                builder.Append("LOW_PRIORITY ");
            }
            else if (this.Priority == MySqlBulkLoaderPriority.Concurrent)
            {
                builder.Append("CONCURRENT ");
            }
            if (this.Local)
            {
                builder.Append("LOCAL ");
            }
            builder.Append("INFILE ");
            if (Path.DirectorySeparatorChar == '\\')
            {
                builder.AppendFormat("'{0}' ", this.FileName.Replace(@"\", @"\\"));
            }
            else
            {
                builder.AppendFormat("'{0}' ", this.FileName);
            }
            if (this.ConflictOption == MySqlBulkLoaderConflictOption.Ignore)
            {
                builder.Append("IGNORE ");
            }
            else if (this.ConflictOption == MySqlBulkLoaderConflictOption.Replace)
            {
                builder.Append("REPLACE ");
            }
            builder.AppendFormat("INTO TABLE {0} ", this.TableName);
            if (this.CharacterSet != null)
            {
                builder.AppendFormat("CHARACTER SET {0} ", this.CharacterSet);
            }
            StringBuilder builder2 = new StringBuilder(string.Empty);
            if (this.FieldTerminator != "\t")
            {
                builder2.AppendFormat("TERMINATED BY '{0}' ", this.FieldTerminator);
            }
            if (this.FieldQuotationCharacter != '\0')
            {
                builder2.AppendFormat("{0} ENCLOSED BY '{1}' ", this.FieldQuotationOptional ? "OPTIONALLY" : "", this.FieldQuotationCharacter);
            }
            if ((this.EscapeCharacter != '\\') && (this.EscapeCharacter != '\0'))
            {
                builder2.AppendFormat("ESCAPED BY '{0}' ", this.EscapeCharacter);
            }
            if (builder2.Length > 0)
            {
                builder.AppendFormat("FIELDS {0}", builder2.ToString());
            }
            builder2 = new StringBuilder(string.Empty);
            if ((this.LinePrefix != null) && (this.LinePrefix.Length > 0))
            {
                builder2.AppendFormat("STARTING BY '{0}' ", this.LinePrefix);
            }
            if (this.LineTerminator != "\n")
            {
                builder2.AppendFormat("TERMINATED BY '{0}' ", this.LineTerminator);
            }
            if (builder2.Length > 0)
            {
                builder.AppendFormat("LINES {0}", builder2.ToString());
            }
            if (this.NumberOfLinesToSkip > 0)
            {
                builder.AppendFormat("IGNORE {0} LINES ", this.NumberOfLinesToSkip);
            }
            if (this.Columns.Count > 0)
            {
                builder.Append("(");
                builder.Append(this.Columns[0]);
                for (int i = 1; i < this.Columns.Count; i++)
                {
                    builder.AppendFormat(",{0}", this.Columns[i]);
                }
                builder.Append(") ");
            }
            if (this.Expressions.Count > 0)
            {
                builder.Append("SET ");
                builder.Append(this.Expressions[0]);
                for (int j = 1; j < this.Expressions.Count; j++)
                {
                    builder.AppendFormat(",{0}", this.Expressions[j]);
                }
            }
            return builder.ToString();
        }

        public int Load()
        {
            int num;
            bool flag = false;
            if (this.Connection == null)
            {
                throw new InvalidOperationException(Resources.ConnectionNotSet);
            }
            if (this.connection.State != ConnectionState.Open)
            {
                flag = true;
                this.connection.Open();
            }
            try
            {
                MySqlCommand command = new MySqlCommand(this.BuildSqlCommand(), this.Connection);
                command.CommandTimeout = this.Timeout;
                num = command.ExecuteNonQuery();
            }
            finally
            {
                if (flag)
                {
                    this.connection.Close();
                }
            }
            return num;
        }

        public StringCollection Columns
        {
            get
            {
                return this.columns;
            }
        }

        public MySqlBulkLoaderConflictOption ConflictOption
        {
            get
            {
                return this.conflictOption;
            }
            set
            {
                this.conflictOption = value;
            }
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

        public char EscapeCharacter
        {
            get
            {
                return this.escapeChar;
            }
            set
            {
                this.escapeChar = value;
            }
        }

        public StringCollection Expressions
        {
            get
            {
                return this.expressions;
            }
        }

        public char FieldQuotationCharacter
        {
            get
            {
                return this.fieldQuotationCharacter;
            }
            set
            {
                this.fieldQuotationCharacter = value;
            }
        }

        public bool FieldQuotationOptional
        {
            get
            {
                return this.fieldQuotationOptional;
            }
            set
            {
                this.fieldQuotationOptional = value;
            }
        }

        public string FieldTerminator
        {
            get
            {
                return this.fieldTerminator;
            }
            set
            {
                this.fieldTerminator = value;
            }
        }

        public string FileName
        {
            get
            {
                return this.filename;
            }
            set
            {
                this.filename = value;
            }
        }

        public string CharacterSet
        {
            get
            {
                return this.charSet;
            }
            set
            {
                this.charSet = value;
            }
        }

        public string LinePrefix
        {
            get
            {
                return this.linePrefix;
            }
            set
            {
                this.linePrefix = value;
            }
        }

        public string LineTerminator
        {
            get
            {
                return this.lineTerminator;
            }
            set
            {
                this.lineTerminator = value;
            }
        }

        public bool Local
        {
            get
            {
                return this.local;
            }
            set
            {
                this.local = value;
            }
        }

        public int NumberOfLinesToSkip
        {
            get
            {
                return this.numLinesToIgnore;
            }
            set
            {
                this.numLinesToIgnore = value;
            }
        }

        public MySqlBulkLoaderPriority Priority
        {
            get
            {
                return this.priority;
            }
            set
            {
                this.priority = value;
            }
        }

        public string TableName
        {
            get
            {
                return this.tableName;
            }
            set
            {
                this.tableName = value;
            }
        }

        public int Timeout
        {
            get
            {
                return this.timeout;
            }
            set
            {
                this.timeout = value;
            }
        }
    }
}

