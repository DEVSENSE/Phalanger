namespace MySql.Data.MySqlClient
{
    using MySql.Data.MySqlClient.Properties;
    using MySql.Data.Types;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Data;
    using System.Data.Common;
    using System.Globalization;
    using System.Text;

    [DesignerCategory("Code"), ToolboxItem(false)]
    public sealed class MySqlCommandBuilder : DbCommandBuilder
    {
        public MySqlCommandBuilder()
        {
            this.QuotePrefix = this.QuoteSuffix = "`";
        }

        public MySqlCommandBuilder(MySqlDataAdapter adapter) : this()
        {
            this.DataAdapter = adapter;
        }

        protected override void ApplyParameterInfo(DbParameter parameter, DataRow row, StatementType statementType, bool whereClause)
        {
            ((MySqlParameter) parameter).MySqlDbType = (MySqlDbType) row["ProviderType"];
        }

        public static void DeriveParameters(MySqlCommand command)
        {
            string commandText = command.CommandText;
            if (commandText.IndexOf(".") == -1)
            {
                commandText = command.Connection.Database + "." + commandText;
            }
            DataSet set = command.Connection.ProcedureCache.GetProcedure(command.Connection, commandText, null);
            if (!set.Tables.Contains("Procedure Parameters"))
            {
                throw new MySqlException(Resources.UnableToDeriveParameters);
            }
            DataTable table = set.Tables["Procedure Parameters"];
            DataTable table2 = set.Tables["Procedures"];
            command.Parameters.Clear();
            foreach (DataRow row in table.Rows)
            {
                MySqlParameter parameter = new MySqlParameter();
                parameter.ParameterName = string.Format("@{0}", row["PARAMETER_NAME"]);
                if (row["ORDINAL_POSITION"].Equals(0) && (parameter.ParameterName == "@"))
                {
                    parameter.ParameterName = "@RETURN_VALUE";
                }
                parameter.Direction = GetDirection(row);
                bool unsigned = StoredProcedure.GetFlags(row["DTD_IDENTIFIER"].ToString()).IndexOf("UNSIGNED") != -1;
                bool realAsFloat = table2.Rows[0]["SQL_MODE"].ToString().IndexOf("REAL_AS_FLOAT") != -1;
                parameter.MySqlDbType = MetaData.NameToType(row["DATA_TYPE"].ToString(), unsigned, realAsFloat, command.Connection);
                if (!row["CHARACTER_MAXIMUM_LENGTH"].Equals(DBNull.Value))
                {
                    parameter.Size = (int) row["CHARACTER_MAXIMUM_LENGTH"];
                }
                if (!row["NUMERIC_PRECISION"].Equals(DBNull.Value))
                {
                    parameter.Precision = Convert.ToByte(row["NUMERIC_PRECISION"]);
                }
                if (!row["NUMERIC_SCALE"].Equals(DBNull.Value))
                {
                    parameter.Scale = Convert.ToByte(row["NUMERIC_SCALE"]);
                }
                if ((parameter.MySqlDbType == MySqlDbType.Set) || (parameter.MySqlDbType == MySqlDbType.Enum))
                {
                    parameter.PossibleValues = GetPossibleValues(row);
                }
                command.Parameters.Add(parameter);
            }
        }

        public MySqlCommand GetDeleteCommand()
        {
            return (MySqlCommand) base.GetDeleteCommand();
        }

        private static ParameterDirection GetDirection(DataRow row)
        {
            string str = row["PARAMETER_MODE"].ToString();
            if (Convert.ToInt32(row["ORDINAL_POSITION"]) == 0)
            {
                return ParameterDirection.ReturnValue;
            }
            switch (str)
            {
                case "IN":
                    return ParameterDirection.Input;

                case "OUT":
                    return ParameterDirection.Output;
            }
            return ParameterDirection.InputOutput;
        }

        public MySqlCommand GetInsertCommand()
        {
            return (MySqlCommand) base.GetInsertCommand(false);
        }

        protected override string GetParameterName(int parameterOrdinal)
        {
            return string.Format("@p{0}", parameterOrdinal.ToString(CultureInfo.InvariantCulture));
        }

        protected override string GetParameterName(string parameterName)
        {
            StringBuilder builder = new StringBuilder(parameterName);
            builder.Replace(" ", "");
            builder.Replace("/", "_per_");
            builder.Replace("-", "_");
            builder.Replace(")", "_cb_");
            builder.Replace("(", "_ob_");
            builder.Replace("%", "_pct_");
            builder.Replace("<", "_lt_");
            builder.Replace(">", "_gt_");
            builder.Replace(".", "_pt_");
            return string.Format("@{0}", builder.ToString());
        }

        protected override string GetParameterPlaceholder(int parameterOrdinal)
        {
            return string.Format("@p{0}", parameterOrdinal.ToString(CultureInfo.InvariantCulture));
        }

        private static List<string> GetPossibleValues(DataRow row)
        {
            string[] strArray = new string[] { "ENUM", "SET" };
            string input = row["DTD_IDENTIFIER"].ToString().Trim();
            int index = 0;
            while (index < 2)
            {
                if (input.StartsWith(strArray[index], StringComparison.InvariantCultureIgnoreCase))
                {
                    break;
                }
                index++;
            }
            if (index == 2)
            {
                return null;
            }
            input = input.Substring(strArray[index].Length).Trim().Trim(new char[] { '(', ')' }).Trim();
            List<string> list = new List<string>();
            MySqlTokenizer tokenizer = new MySqlTokenizer(input);
            string str2 = tokenizer.NextToken();
            int startIndex = tokenizer.StartIndex;
        Label_00A7:
            switch (str2)
            {
                case null:
                case ",":
                {
                    int num3 = input.Length - 1;
                    if (str2 == ",")
                    {
                        num3 = tokenizer.StartIndex;
                    }
                    string item = input.Substring(startIndex, num3 - startIndex).Trim(new char[] { '\'', '"' }).Trim();
                    list.Add(item);
                    startIndex = tokenizer.StopIndex;
                    break;
                }
            }
            if (str2 != null)
            {
                str2 = tokenizer.NextToken();
                goto Label_00A7;
            }
            return list;
        }

        protected override DataTable GetSchemaTable(DbCommand sourceCommand)
        {
            DataTable schemaTable = base.GetSchemaTable(sourceCommand);
            foreach (DataRow row in schemaTable.Rows)
            {
                if (row["BaseSchemaName"].Equals(sourceCommand.Connection.Database))
                {
                    row["BaseSchemaName"] = null;
                }
            }
            return schemaTable;
        }

        public MySqlCommand GetUpdateCommand()
        {
            return (MySqlCommand) base.GetUpdateCommand();
        }

        public override string QuoteIdentifier(string unquotedIdentifier)
        {
            if (unquotedIdentifier == null)
            {
                throw new ArgumentNullException("unquotedIdentifier");
            }
            if (unquotedIdentifier.StartsWith(this.QuotePrefix) && unquotedIdentifier.EndsWith(this.QuoteSuffix))
            {
                return unquotedIdentifier;
            }
            unquotedIdentifier = unquotedIdentifier.Replace(this.QuotePrefix, this.QuotePrefix + this.QuotePrefix);
            return string.Format("{0}{1}{2}", this.QuotePrefix, unquotedIdentifier, this.QuoteSuffix);
        }

        private void RowUpdating(object sender, MySqlRowUpdatingEventArgs args)
        {
            base.RowUpdatingHandler(args);
        }

        protected override void SetRowUpdatingHandler(DbDataAdapter adapter)
        {
            MySqlDataAdapter adapter2 = adapter as MySqlDataAdapter;
            if (adapter != base.DataAdapter)
            {
                adapter2.RowUpdating += new MySqlRowUpdatingEventHandler(this.RowUpdating);
            }
            else
            {
                adapter2.RowUpdating -= new MySqlRowUpdatingEventHandler(this.RowUpdating);
            }
        }

        public override string UnquoteIdentifier(string quotedIdentifier)
        {
            if (quotedIdentifier == null)
            {
                throw new ArgumentNullException("quotedIdentifier");
            }
            if (quotedIdentifier.StartsWith(this.QuotePrefix) && quotedIdentifier.EndsWith(this.QuoteSuffix))
            {
                if (quotedIdentifier.StartsWith(this.QuotePrefix))
                {
                    quotedIdentifier = quotedIdentifier.Substring(1);
                }
                if (quotedIdentifier.EndsWith(this.QuoteSuffix))
                {
                    quotedIdentifier = quotedIdentifier.Substring(0, quotedIdentifier.Length - 1);
                }
                quotedIdentifier = quotedIdentifier.Replace(this.QuotePrefix + this.QuotePrefix, this.QuotePrefix);
            }
            return quotedIdentifier;
        }

        public MySqlDataAdapter DataAdapter
        {
            get
            {
                return (MySqlDataAdapter) base.DataAdapter;
            }
            set
            {
                base.DataAdapter = value;
            }
        }
    }
}

