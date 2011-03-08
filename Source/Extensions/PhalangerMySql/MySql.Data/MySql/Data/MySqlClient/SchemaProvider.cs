namespace MySql.Data.MySqlClient
{
    using MySql.Data.Common;
    using MySql.Data.MySqlClient.Properties;
    using MySql.Data.Types;
    using System;
    using System.Collections;
    using System.Data;
    using System.Data.Common;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using System.Text;
    using System.Text.RegularExpressions;

    internal class SchemaProvider
    {
        protected MySqlConnection connection;
        public static string MetaCollection = "MetaDataCollections";

        public SchemaProvider(MySqlConnection connectionToUse)
        {
            this.connection = connectionToUse;
        }

        internal string[] CleanRestrictions(string[] restrictionValues)
        {
            string[] strArray = null;
            if (restrictionValues != null)
            {
                strArray = (string[]) restrictionValues.Clone();
                for (int i = 0; i < strArray.Length; i++)
                {
                    string str = strArray[i];
                    if (str != null)
                    {
                        strArray[i] = str.Trim(new char[] { '`' });
                    }
                }
            }
            return strArray;
        }

        protected static void FillTable(DataTable dt, object[][] data)
        {
            foreach (object[] objArray in data)
            {
                DataRow row = dt.NewRow();
                for (int i = 0; i < objArray.Length; i++)
                {
                    row[i] = objArray[i];
                }
                dt.Rows.Add(row);
            }
        }

        private void FindTables(DataTable schemaTable, string[] restrictions)
        {
            StringBuilder builder = new StringBuilder();
            StringBuilder builder2 = new StringBuilder();
            builder.AppendFormat(CultureInfo.InvariantCulture, "SHOW TABLE STATUS FROM `{0}`", new object[] { restrictions[1] });
            if (((restrictions != null) && (restrictions.Length >= 3)) && (restrictions[2] != null))
            {
                builder2.AppendFormat(CultureInfo.InvariantCulture, " LIKE '{0}'", new object[] { restrictions[2] });
            }
            builder.Append(builder2.ToString());
            string str = (restrictions[1].ToLower() == "information_schema") ? "SYSTEM VIEW" : "BASE TABLE";
            MySqlCommand command = new MySqlCommand(builder.ToString(), this.connection);
            using (MySqlDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    DataRow row = schemaTable.NewRow();
                    row["TABLE_CATALOG"] = null;
                    row["TABLE_SCHEMA"] = restrictions[1];
                    row["TABLE_NAME"] = reader.GetString(0);
                    row["TABLE_TYPE"] = str;
                    row["ENGINE"] = GetString(reader, 1);
                    row["VERSION"] = reader.GetValue(2);
                    row["ROW_FORMAT"] = GetString(reader, 3);
                    row["TABLE_ROWS"] = reader.GetValue(4);
                    row["AVG_ROW_LENGTH"] = reader.GetValue(5);
                    row["DATA_LENGTH"] = reader.GetValue(6);
                    row["MAX_DATA_LENGTH"] = reader.GetValue(7);
                    row["INDEX_LENGTH"] = reader.GetValue(8);
                    row["DATA_FREE"] = reader.GetValue(9);
                    row["AUTO_INCREMENT"] = reader.GetValue(10);
                    row["CREATE_TIME"] = reader.GetValue(11);
                    row["UPDATE_TIME"] = reader.GetValue(12);
                    row["CHECK_TIME"] = reader.GetValue(13);
                    row["TABLE_COLLATION"] = GetString(reader, 14);
                    row["CHECKSUM"] = reader.GetValue(15);
                    row["CREATE_OPTIONS"] = GetString(reader, 0x10);
                    row["TABLE_COMMENT"] = GetString(reader, 0x11);
                    schemaTable.Rows.Add(row);
                }
            }
        }

        protected virtual DataTable GetCollections()
        {
            object[][] data = new object[][] { new object[] { "MetaDataCollections", 0, 0 }, new object[] { "DataSourceInformation", 0, 0 }, new object[] { "DataTypes", 0, 0 }, new object[] { "Restrictions", 0, 0 }, new object[] { "ReservedWords", 0, 0 }, new object[] { "Databases", 1, 1 }, new object[] { "Tables", 4, 2 }, new object[] { "Columns", 4, 4 }, new object[] { "Users", 1, 1 }, new object[] { "Foreign Keys", 4, 3 }, new object[] { "IndexColumns", 5, 4 }, new object[] { "Indexes", 4, 3 }, new object[] { "Foreign Key Columns", 4, 3 }, new object[] { "UDF", 1, 1 } };
            DataTable dt = new DataTable("MetaDataCollections");
            dt.Columns.Add(new DataColumn("CollectionName", typeof(string)));
            dt.Columns.Add(new DataColumn("NumberOfRestrictions", typeof(int)));
            dt.Columns.Add(new DataColumn("NumberOfIdentifierParts", typeof(int)));
            FillTable(dt, data);
            return dt;
        }

        public virtual DataTable GetColumns(string[] restrictions)
        {
            DataTable dt = new DataTable("Columns");
            dt.Columns.Add("TABLE_CATALOG", typeof(string));
            dt.Columns.Add("TABLE_SCHEMA", typeof(string));
            dt.Columns.Add("TABLE_NAME", typeof(string));
            dt.Columns.Add("COLUMN_NAME", typeof(string));
            dt.Columns.Add("ORDINAL_POSITION", typeof(ulong));
            dt.Columns.Add("COLUMN_DEFAULT", typeof(string));
            dt.Columns.Add("IS_NULLABLE", typeof(string));
            dt.Columns.Add("DATA_TYPE", typeof(string));
            dt.Columns.Add("CHARACTER_MAXIMUM_LENGTH", typeof(ulong));
            dt.Columns.Add("CHARACTER_OCTET_LENGTH", typeof(ulong));
            dt.Columns.Add("NUMERIC_PRECISION", typeof(ulong));
            dt.Columns.Add("NUMERIC_SCALE", typeof(ulong));
            dt.Columns.Add("CHARACTER_SET_NAME", typeof(string));
            dt.Columns.Add("COLLATION_NAME", typeof(string));
            dt.Columns.Add("COLUMN_TYPE", typeof(string));
            dt.Columns.Add("COLUMN_KEY", typeof(string));
            dt.Columns.Add("EXTRA", typeof(string));
            dt.Columns.Add("PRIVILEGES", typeof(string));
            dt.Columns.Add("COLUMN_COMMENT", typeof(string));
            string columnRestriction = null;
            if ((restrictions != null) && (restrictions.Length == 4))
            {
                columnRestriction = restrictions[3];
                restrictions[3] = null;
            }
            foreach (DataRow row in this.GetTables(restrictions).Rows)
            {
                this.LoadTableColumns(dt, row["TABLE_SCHEMA"].ToString(), row["TABLE_NAME"].ToString(), columnRestriction);
            }
            return dt;
        }

        public virtual DataTable GetDatabases(string[] restrictions)
        {
            Regex regex = null;
            int num = int.Parse(this.connection.driver.Property("lower_case_table_names"));
            string selectCommandText = "SHOW DATABASES";
            if (num == 0)
            {
                if ((restrictions != null) && (restrictions.Length >= 1))
                {
                    selectCommandText = selectCommandText + " LIKE '" + restrictions[0] + "'";
                }
            }
            else if (((restrictions != null) && (restrictions.Length >= 1)) && (restrictions[0] != null))
            {
                regex = new Regex(restrictions[0], RegexOptions.IgnoreCase);
            }
            MySqlDataAdapter adapter = new MySqlDataAdapter(selectCommandText, this.connection);
            DataTable dataTable = new DataTable();
            adapter.Fill(dataTable);
            DataTable table2 = new DataTable("Databases");
            table2.Columns.Add("CATALOG_NAME", typeof(string));
            table2.Columns.Add("SCHEMA_NAME", typeof(string));
            foreach (DataRow row in dataTable.Rows)
            {
                if (((num == 0) || (regex == null)) || regex.Match(row[0].ToString()).Success)
                {
                    DataRow row2 = table2.NewRow();
                    row2[1] = row[0];
                    table2.Rows.Add(row2);
                }
            }
            return table2;
        }

        private DataTable GetDataSourceInformation()
        {
            DataTable table = new DataTable("DataSourceInformation");
            table.Columns.Add("CompositeIdentifierSeparatorPattern", typeof(string));
            table.Columns.Add("DataSourceProductName", typeof(string));
            table.Columns.Add("DataSourceProductVersion", typeof(string));
            table.Columns.Add("DataSourceProductVersionNormalized", typeof(string));
            table.Columns.Add("GroupByBehavior", typeof(GroupByBehavior));
            table.Columns.Add("IdentifierPattern", typeof(string));
            table.Columns.Add("IdentifierCase", typeof(IdentifierCase));
            table.Columns.Add("OrderByColumnsInSelect", typeof(bool));
            table.Columns.Add("ParameterMarkerFormat", typeof(string));
            table.Columns.Add("ParameterMarkerPattern", typeof(string));
            table.Columns.Add("ParameterNameMaxLength", typeof(int));
            table.Columns.Add("ParameterNamePattern", typeof(string));
            table.Columns.Add("QuotedIdentifierPattern", typeof(string));
            table.Columns.Add("QuotedIdentifierCase", typeof(IdentifierCase));
            table.Columns.Add("StatementSeparatorPattern", typeof(string));
            table.Columns.Add("StringLiteralPattern", typeof(string));
            table.Columns.Add("SupportedJoinOperators", typeof(SupportedJoinOperators));
            DBVersion version = this.connection.driver.Version;
            string str = string.Format("{0:0}.{1:0}.{2:0}", version.Major, version.Minor, version.Build);
            DataRow row = table.NewRow();
            row["CompositeIdentifierSeparatorPattern"] = @"\.";
            row["DataSourceProductName"] = "MySQL";
            row["DataSourceProductVersion"] = this.connection.ServerVersion;
            row["DataSourceProductVersionNormalized"] = str;
            row["GroupByBehavior"] = GroupByBehavior.Unrelated;
            row["IdentifierPattern"] = "(^\\`\\p{Lo}\\p{Lu}\\p{Ll}_@#][\\p{Lo}\\p{Lu}\\p{Ll}\\p{Nd}@$#_]*$)|(^\\`[^\\`\\0]|\\`\\`+\\`$)|(^\\\" + [^\\\"\\0]|\\\"\\\"+\\\"$)";
            row["IdentifierCase"] = IdentifierCase.Insensitive;
            row["OrderByColumnsInSelect"] = false;
            row["ParameterMarkerFormat"] = "{0}";
            row["ParameterMarkerPattern"] = "(@[A-Za-z0-9_$#]*)";
            row["ParameterNameMaxLength"] = 0x80;
            row["ParameterNamePattern"] = @"^[\p{Lo}\p{Lu}\p{Ll}\p{Lm}_@#][\p{Lo}\p{Lu}\p{Ll}\p{Lm}\p{Nd}\uff3f_@#\$]*(?=\s+|$)";
            row["QuotedIdentifierPattern"] = @"(([^\`]|\`\`)*)";
            row["QuotedIdentifierCase"] = IdentifierCase.Sensitive;
            row["StatementSeparatorPattern"] = ";";
            row["StringLiteralPattern"] = "'(([^']|'')*)'";
            row["SupportedJoinOperators"] = 15;
            table.Rows.Add(row);
            return table;
        }

        private static DataTable GetDataTypes()
        {
            DataTable dsTable = new DataTable("DataTypes");
            dsTable.Columns.Add(new DataColumn("TypeName", typeof(string)));
            dsTable.Columns.Add(new DataColumn("ProviderDbType", typeof(int)));
            dsTable.Columns.Add(new DataColumn("ColumnSize", typeof(long)));
            dsTable.Columns.Add(new DataColumn("CreateFormat", typeof(string)));
            dsTable.Columns.Add(new DataColumn("CreateParameters", typeof(string)));
            dsTable.Columns.Add(new DataColumn("DataType", typeof(string)));
            dsTable.Columns.Add(new DataColumn("IsAutoincrementable", typeof(bool)));
            dsTable.Columns.Add(new DataColumn("IsBestMatch", typeof(bool)));
            dsTable.Columns.Add(new DataColumn("IsCaseSensitive", typeof(bool)));
            dsTable.Columns.Add(new DataColumn("IsFixedLength", typeof(bool)));
            dsTable.Columns.Add(new DataColumn("IsFixedPrecisionScale", typeof(bool)));
            dsTable.Columns.Add(new DataColumn("IsLong", typeof(bool)));
            dsTable.Columns.Add(new DataColumn("IsNullable", typeof(bool)));
            dsTable.Columns.Add(new DataColumn("IsSearchable", typeof(bool)));
            dsTable.Columns.Add(new DataColumn("IsSearchableWithLike", typeof(bool)));
            dsTable.Columns.Add(new DataColumn("IsUnsigned", typeof(bool)));
            dsTable.Columns.Add(new DataColumn("MaximumScale", typeof(short)));
            dsTable.Columns.Add(new DataColumn("MinimumScale", typeof(short)));
            dsTable.Columns.Add(new DataColumn("IsConcurrencyType", typeof(bool)));
            dsTable.Columns.Add(new DataColumn("IsLiteralSupported", typeof(bool)));
            dsTable.Columns.Add(new DataColumn("LiteralPrefix", typeof(string)));
            dsTable.Columns.Add(new DataColumn("LiteralSuffix", typeof(string)));
            dsTable.Columns.Add(new DataColumn("NativeDataType", typeof(string)));
            MySqlBit.SetDSInfo(dsTable);
            MySqlBinary.SetDSInfo(dsTable);
            MySqlDateTime.SetDSInfo(dsTable);
            MySqlTimeSpan.SetDSInfo(dsTable);
            MySqlString.SetDSInfo(dsTable);
            MySqlDouble.SetDSInfo(dsTable);
            MySqlSingle.SetDSInfo(dsTable);
            MySqlByte.SetDSInfo(dsTable);
            MySqlInt16.SetDSInfo(dsTable);
            MySqlInt32.SetDSInfo(dsTable);
            MySqlInt64.SetDSInfo(dsTable);
            MySqlDecimal.SetDSInfo(dsTable);
            MySqlUByte.SetDSInfo(dsTable);
            MySqlUInt16.SetDSInfo(dsTable);
            MySqlUInt32.SetDSInfo(dsTable);
            MySqlUInt64.SetDSInfo(dsTable);
            return dsTable;
        }

        public virtual DataTable GetForeignKeyColumns(string[] restrictions)
        {
            DataTable fkTable = new DataTable("Foreign Keys");
            fkTable.Columns.Add("CONSTRAINT_CATALOG", typeof(string));
            fkTable.Columns.Add("CONSTRAINT_SCHEMA", typeof(string));
            fkTable.Columns.Add("CONSTRAINT_NAME", typeof(string));
            fkTable.Columns.Add("TABLE_CATALOG", typeof(string));
            fkTable.Columns.Add("TABLE_SCHEMA", typeof(string));
            fkTable.Columns.Add("TABLE_NAME", typeof(string));
            fkTable.Columns.Add("COLUMN_NAME", typeof(string));
            fkTable.Columns.Add("ORDINAL_POSITION", typeof(int));
            fkTable.Columns.Add("REFERENCED_TABLE_CATALOG", typeof(string));
            fkTable.Columns.Add("REFERENCED_TABLE_SCHEMA", typeof(string));
            fkTable.Columns.Add("REFERENCED_TABLE_NAME", typeof(string));
            fkTable.Columns.Add("REFERENCED_COLUMN_NAME", typeof(string));
            string filterName = null;
            if ((restrictions != null) && (restrictions.Length >= 4))
            {
                filterName = restrictions[3];
                restrictions[3] = null;
            }
            foreach (DataRow row in this.GetTables(restrictions).Rows)
            {
                this.GetForeignKeysOnTable(fkTable, row, filterName, true);
            }
            return fkTable;
        }

        public virtual DataTable GetForeignKeys(string[] restrictions)
        {
            DataTable fkTable = new DataTable("Foreign Keys");
            fkTable.Columns.Add("CONSTRAINT_CATALOG", typeof(string));
            fkTable.Columns.Add("CONSTRAINT_SCHEMA", typeof(string));
            fkTable.Columns.Add("CONSTRAINT_NAME", typeof(string));
            fkTable.Columns.Add("TABLE_CATALOG", typeof(string));
            fkTable.Columns.Add("TABLE_SCHEMA", typeof(string));
            fkTable.Columns.Add("TABLE_NAME", typeof(string));
            fkTable.Columns.Add("MATCH_OPTION", typeof(string));
            fkTable.Columns.Add("UPDATE_RULE", typeof(string));
            fkTable.Columns.Add("DELETE_RULE", typeof(string));
            fkTable.Columns.Add("REFERENCED_TABLE_CATALOG", typeof(string));
            fkTable.Columns.Add("REFERENCED_TABLE_SCHEMA", typeof(string));
            fkTable.Columns.Add("REFERENCED_TABLE_NAME", typeof(string));
            string filterName = null;
            if ((restrictions != null) && (restrictions.Length >= 4))
            {
                filterName = restrictions[3];
                restrictions[3] = null;
            }
            foreach (DataRow row in this.GetTables(restrictions).Rows)
            {
                this.GetForeignKeysOnTable(fkTable, row, filterName, false);
            }
            return fkTable;
        }

        private void GetForeignKeysOnTable(DataTable fkTable, DataRow tableToParse, string filterName, bool includeColumns)
        {
            string sqlMode = this.GetSqlMode();
            if (filterName != null)
            {
                filterName = filterName.ToLower(CultureInfo.InvariantCulture);
            }
            string cmdText = string.Format("SHOW CREATE TABLE `{0}`.`{1}`", tableToParse["TABLE_SCHEMA"], tableToParse["TABLE_NAME"]);
            string input = null;
            MySqlCommand command = new MySqlCommand(cmdText, this.connection);
            using (MySqlDataReader reader = command.ExecuteReader())
            {
                reader.Read();
                input = reader.GetString(1).ToLower(CultureInfo.InvariantCulture);
            }
            MySqlTokenizer tokenizer = new MySqlTokenizer(input);
            tokenizer.AnsiQuotes = sqlMode.IndexOf("ANSI_QUOTES") != -1;
            tokenizer.BackslashEscapes = sqlMode.IndexOf("NO_BACKSLASH_ESCAPES") != -1;
            while (true)
            {
                string str5 = tokenizer.NextToken();
                while ((str5 != null) && ((str5 != "constraint") || tokenizer.Quoted))
                {
                    str5 = tokenizer.NextToken();
                }
                if (str5 == null)
                {
                    return;
                }
                ParseConstraint(fkTable, tableToParse, tokenizer, includeColumns);
            }
        }

        public virtual DataTable GetIndexColumns(string[] restrictions)
        {
            DataTable table = new DataTable("IndexColumns");
            table.Columns.Add("INDEX_CATALOG", typeof(string));
            table.Columns.Add("INDEX_SCHEMA", typeof(string));
            table.Columns.Add("INDEX_NAME", typeof(string));
            table.Columns.Add("TABLE_NAME", typeof(string));
            table.Columns.Add("COLUMN_NAME", typeof(string));
            table.Columns.Add("ORDINAL_POSITION", typeof(int));
            table.Columns.Add("SORT_ORDER", typeof(string));
            int num = (restrictions == null) ? 4 : restrictions.Length;
            string[] array = new string[Math.Max(num, 4)];
            if (restrictions != null)
            {
                restrictions.CopyTo(array, 0);
            }
            array[3] = "BASE TABLE";
            foreach (DataRow row in this.GetTables(array).Rows)
            {
                MySqlCommand command = new MySqlCommand(string.Format("SHOW INDEX FROM `{0}`.`{1}`", row["TABLE_SCHEMA"], row["TABLE_NAME"]), this.connection);
                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string str2 = GetString(reader, reader.GetOrdinal("KEY_NAME"));
                        string str3 = GetString(reader, reader.GetOrdinal("COLUMN_NAME"));
                        if ((restrictions == null) || ((((restrictions.Length < 4) || (restrictions[3] == null)) || (str2 == restrictions[3])) && (((restrictions.Length < 5) || (restrictions[4] == null)) || (str3 == restrictions[4]))))
                        {
                            DataRow row2 = table.NewRow();
                            row2["INDEX_CATALOG"] = null;
                            row2["INDEX_SCHEMA"] = row["TABLE_SCHEMA"];
                            row2["INDEX_NAME"] = str2;
                            row2["TABLE_NAME"] = GetString(reader, reader.GetOrdinal("TABLE"));
                            row2["COLUMN_NAME"] = str3;
                            row2["ORDINAL_POSITION"] = reader.GetValue(reader.GetOrdinal("SEQ_IN_INDEX"));
                            row2["SORT_ORDER"] = reader.GetString("COLLATION");
                            table.Rows.Add(row2);
                        }
                    }
                    continue;
                }
            }
            return table;
        }

        public virtual DataTable GetIndexes(string[] restrictions)
        {
            DataTable table = new DataTable("Indexes");
            table.Columns.Add("INDEX_CATALOG", typeof(string));
            table.Columns.Add("INDEX_SCHEMA", typeof(string));
            table.Columns.Add("INDEX_NAME", typeof(string));
            table.Columns.Add("TABLE_NAME", typeof(string));
            table.Columns.Add("UNIQUE", typeof(bool));
            table.Columns.Add("PRIMARY", typeof(bool));
            table.Columns.Add("TYPE", typeof(string));
            table.Columns.Add("COMMENT", typeof(string));
            int num = (restrictions == null) ? 4 : restrictions.Length;
            string[] array = new string[Math.Max(num, 4)];
            if (restrictions != null)
            {
                restrictions.CopyTo(array, 0);
            }
            array[3] = "BASE TABLE";
            foreach (DataRow row in this.GetTables(array).Rows)
            {
                MySqlDataAdapter adapter = new MySqlDataAdapter(string.Format("SHOW INDEX FROM `{0}`.`{1}`", MySqlHelper.DoubleQuoteString((string) row["TABLE_SCHEMA"]), MySqlHelper.DoubleQuoteString((string) row["TABLE_NAME"])), this.connection);
                DataTable dataTable = new DataTable();
                adapter.Fill(dataTable);
                foreach (DataRow row2 in dataTable.Rows)
                {
                    long num2 = (long) row2["SEQ_IN_INDEX"];
                    if ((num2 == 1L) && (((restrictions == null) || (restrictions.Length != 4)) || ((restrictions[3] == null) || row2["KEY_NAME"].Equals(restrictions[3]))))
                    {
                        DataRow row3 = table.NewRow();
                        row3["INDEX_CATALOG"] = null;
                        row3["INDEX_SCHEMA"] = row["TABLE_SCHEMA"];
                        row3["INDEX_NAME"] = row2["KEY_NAME"];
                        row3["TABLE_NAME"] = row2["TABLE"];
                        row3["UNIQUE"] = ((long) row2["NON_UNIQUE"]) == 0L;
                        row3["PRIMARY"] = row2["KEY_NAME"].Equals("PRIMARY");
                        row3["TYPE"] = row2["INDEX_TYPE"];
                        row3["COMMENT"] = row2["COMMENT"];
                        table.Rows.Add(row3);
                    }
                }
            }
            return table;
        }

        public virtual DataTable GetProcedures(string[] restrictions)
        {
            DataTable table = new DataTable("Procedures");
            table.Columns.Add(new DataColumn("SPECIFIC_NAME", typeof(string)));
            table.Columns.Add(new DataColumn("ROUTINE_CATALOG", typeof(string)));
            table.Columns.Add(new DataColumn("ROUTINE_SCHEMA", typeof(string)));
            table.Columns.Add(new DataColumn("ROUTINE_NAME", typeof(string)));
            table.Columns.Add(new DataColumn("ROUTINE_TYPE", typeof(string)));
            table.Columns.Add(new DataColumn("DTD_IDENTIFIER", typeof(string)));
            table.Columns.Add(new DataColumn("ROUTINE_BODY", typeof(string)));
            table.Columns.Add(new DataColumn("ROUTINE_DEFINITION", typeof(string)));
            table.Columns.Add(new DataColumn("EXTERNAL_NAME", typeof(string)));
            table.Columns.Add(new DataColumn("EXTERNAL_LANGUAGE", typeof(string)));
            table.Columns.Add(new DataColumn("PARAMETER_STYLE", typeof(string)));
            table.Columns.Add(new DataColumn("IS_DETERMINISTIC", typeof(string)));
            table.Columns.Add(new DataColumn("SQL_DATA_ACCESS", typeof(string)));
            table.Columns.Add(new DataColumn("SQL_PATH", typeof(string)));
            table.Columns.Add(new DataColumn("SECURITY_TYPE", typeof(string)));
            table.Columns.Add(new DataColumn("CREATED", typeof(DateTime)));
            table.Columns.Add(new DataColumn("LAST_ALTERED", typeof(DateTime)));
            table.Columns.Add(new DataColumn("SQL_MODE", typeof(string)));
            table.Columns.Add(new DataColumn("ROUTINE_COMMENT", typeof(string)));
            table.Columns.Add(new DataColumn("DEFINER", typeof(string)));
            StringBuilder builder = new StringBuilder("SELECT * FROM mysql.proc WHERE 1=1");
            if (restrictions != null)
            {
                if ((restrictions.Length >= 2) && (restrictions[1] != null))
                {
                    builder.AppendFormat(CultureInfo.InvariantCulture, " AND db LIKE '{0}'", new object[] { restrictions[1] });
                }
                if ((restrictions.Length >= 3) && (restrictions[2] != null))
                {
                    builder.AppendFormat(CultureInfo.InvariantCulture, " AND name LIKE '{0}'", new object[] { restrictions[2] });
                }
                if ((restrictions.Length >= 4) && (restrictions[3] != null))
                {
                    builder.AppendFormat(CultureInfo.InvariantCulture, " AND type LIKE '{0}'", new object[] { restrictions[3] });
                }
            }
            MySqlCommand command = new MySqlCommand(builder.ToString(), this.connection);
            using (MySqlDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    DataRow row = table.NewRow();
                    row["SPECIFIC_NAME"] = reader.GetString("specific_name");
                    row["ROUTINE_CATALOG"] = DBNull.Value;
                    row["ROUTINE_SCHEMA"] = reader.GetString("db");
                    row["ROUTINE_NAME"] = reader.GetString("name");
                    string str = reader.GetString("type");
                    row["ROUTINE_TYPE"] = str;
                    row["DTD_IDENTIFIER"] = (str.ToLower(CultureInfo.InvariantCulture) == "function") ? ((object) reader.GetString("returns")) : ((object) DBNull.Value);
                    row["ROUTINE_BODY"] = "SQL";
                    row["ROUTINE_DEFINITION"] = reader.GetString("body");
                    row["EXTERNAL_NAME"] = DBNull.Value;
                    row["EXTERNAL_LANGUAGE"] = DBNull.Value;
                    row["PARAMETER_STYLE"] = "SQL";
                    row["IS_DETERMINISTIC"] = reader.GetString("is_deterministic");
                    row["SQL_DATA_ACCESS"] = reader.GetString("sql_data_access");
                    row["SQL_PATH"] = DBNull.Value;
                    row["SECURITY_TYPE"] = reader.GetString("security_type");
                    row["CREATED"] = reader.GetDateTime("created");
                    row["LAST_ALTERED"] = reader.GetDateTime("modified");
                    row["SQL_MODE"] = reader.GetString("sql_mode");
                    row["ROUTINE_COMMENT"] = reader.GetString("comment");
                    row["DEFINER"] = reader.GetString("definer");
                    table.Rows.Add(row);
                }
            }
            return table;
        }

        private static DataTable GetReservedWords()
        {
            DataTable table = new DataTable("ReservedWords");
            table.Columns.Add(new DataColumn(DbMetaDataColumnNames.ReservedWord, typeof(string)));
            Stream manifestResourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("MySql.Data.MySqlClient.Properties.ReservedWords.txt");
            StreamReader reader = new StreamReader(manifestResourceStream);
            for (string str = reader.ReadLine(); str != null; str = reader.ReadLine())
            {
                foreach (string str2 in str.Split(new char[] { ' ' }))
                {
                    if (!string.IsNullOrEmpty(str2))
                    {
                        DataRow row = table.NewRow();
                        row[0] = str2;
                        table.Rows.Add(row);
                    }
                }
            }
            reader.Close();
            manifestResourceStream.Close();
            return table;
        }

        protected virtual DataTable GetRestrictions()
        {
            object[][] data = new object[][] { 
                new object[] { "Users", "Name", "", 0 }, new object[] { "Databases", "Name", "", 0 }, new object[] { "Tables", "Database", "", 0 }, new object[] { "Tables", "Schema", "", 1 }, new object[] { "Tables", "Table", "", 2 }, new object[] { "Tables", "TableType", "", 3 }, new object[] { "Columns", "Database", "", 0 }, new object[] { "Columns", "Schema", "", 1 }, new object[] { "Columns", "Table", "", 2 }, new object[] { "Columns", "Column", "", 3 }, new object[] { "Indexes", "Database", "", 0 }, new object[] { "Indexes", "Schema", "", 1 }, new object[] { "Indexes", "Table", "", 2 }, new object[] { "Indexes", "Name", "", 3 }, new object[] { "IndexColumns", "Database", "", 0 }, new object[] { "IndexColumns", "Schema", "", 1 }, 
                new object[] { "IndexColumns", "Table", "", 2 }, new object[] { "IndexColumns", "ConstraintName", "", 3 }, new object[] { "IndexColumns", "Column", "", 4 }, new object[] { "Foreign Keys", "Database", "", 0 }, new object[] { "Foreign Keys", "Schema", "", 1 }, new object[] { "Foreign Keys", "Table", "", 2 }, new object[] { "Foreign Keys", "Constraint Name", "", 3 }, new object[] { "Foreign Key Columns", "Catalog", "", 0 }, new object[] { "Foreign Key Columns", "Schema", "", 1 }, new object[] { "Foreign Key Columns", "Table", "", 2 }, new object[] { "Foreign Key Columns", "Constraint Name", "", 3 }, new object[] { "UDF", "Name", "", 0 }
             };
            DataTable dt = new DataTable("Restrictions");
            dt.Columns.Add(new DataColumn("CollectionName", typeof(string)));
            dt.Columns.Add(new DataColumn("RestrictionName", typeof(string)));
            dt.Columns.Add(new DataColumn("RestrictionDefault", typeof(string)));
            dt.Columns.Add(new DataColumn("RestrictionNumber", typeof(int)));
            FillTable(dt, data);
            return dt;
        }

        public virtual DataTable GetSchema(string collection, string[] restrictions)
        {
            if (this.connection.State != ConnectionState.Open)
            {
                throw new MySqlException("GetSchema can only be called on an open connection.");
            }
            collection = collection.ToUpper(CultureInfo.InvariantCulture);
            DataTable schemaInternal = this.GetSchemaInternal(collection, restrictions);
            if (schemaInternal == null)
            {
                throw new MySqlException("Invalid collection name");
            }
            return schemaInternal;
        }

        protected virtual DataTable GetSchemaInternal(string collection, string[] restrictions)
        {
            switch (collection)
            {
                case "METADATACOLLECTIONS":
                    return this.GetCollections();

                case "DATASOURCEINFORMATION":
                    return this.GetDataSourceInformation();

                case "DATATYPES":
                    return GetDataTypes();

                case "RESTRICTIONS":
                    return this.GetRestrictions();

                case "RESERVEDWORDS":
                    return GetReservedWords();

                case "USERS":
                    return this.GetUsers(restrictions);

                case "DATABASES":
                    return this.GetDatabases(restrictions);

                case "UDF":
                    return this.GetUDF(restrictions);
            }
            if (restrictions == null)
            {
                restrictions = new string[2];
            }
            if ((((this.connection != null) && (this.connection.Database != null)) && ((this.connection.Database.Length > 0) && (restrictions.Length > 1))) && (restrictions[1] == null))
            {
                restrictions[1] = this.connection.Database;
            }
            switch (collection)
            {
                case "TABLES":
                    return this.GetTables(restrictions);

                case "COLUMNS":
                    return this.GetColumns(restrictions);

                case "INDEXES":
                    return this.GetIndexes(restrictions);

                case "INDEXCOLUMNS":
                    return this.GetIndexColumns(restrictions);

                case "FOREIGN KEYS":
                    return this.GetForeignKeys(restrictions);

                case "FOREIGN KEY COLUMNS":
                    return this.GetForeignKeyColumns(restrictions);
            }
            return null;
        }

        private string GetSqlMode()
        {
            MySqlCommand command = new MySqlCommand("SELECT @@SQL_MODE", this.connection);
            return command.ExecuteScalar().ToString();
        }

        private static string GetString(MySqlDataReader reader, int index)
        {
            if (reader.IsDBNull(index))
            {
                return null;
            }
            return reader.GetString(index);
        }

        public virtual DataTable GetTables(string[] restrictions)
        {
            DataTable schemaTable = new DataTable("Tables");
            schemaTable.Columns.Add("TABLE_CATALOG", typeof(string));
            schemaTable.Columns.Add("TABLE_SCHEMA", typeof(string));
            schemaTable.Columns.Add("TABLE_NAME", typeof(string));
            schemaTable.Columns.Add("TABLE_TYPE", typeof(string));
            schemaTable.Columns.Add("ENGINE", typeof(string));
            schemaTable.Columns.Add("VERSION", typeof(ulong));
            schemaTable.Columns.Add("ROW_FORMAT", typeof(string));
            schemaTable.Columns.Add("TABLE_ROWS", typeof(ulong));
            schemaTable.Columns.Add("AVG_ROW_LENGTH", typeof(ulong));
            schemaTable.Columns.Add("DATA_LENGTH", typeof(ulong));
            schemaTable.Columns.Add("MAX_DATA_LENGTH", typeof(ulong));
            schemaTable.Columns.Add("INDEX_LENGTH", typeof(ulong));
            schemaTable.Columns.Add("DATA_FREE", typeof(ulong));
            schemaTable.Columns.Add("AUTO_INCREMENT", typeof(ulong));
            schemaTable.Columns.Add("CREATE_TIME", typeof(DateTime));
            schemaTable.Columns.Add("UPDATE_TIME", typeof(DateTime));
            schemaTable.Columns.Add("CHECK_TIME", typeof(DateTime));
            schemaTable.Columns.Add("TABLE_COLLATION", typeof(string));
            schemaTable.Columns.Add("CHECKSUM", typeof(ulong));
            schemaTable.Columns.Add("CREATE_OPTIONS", typeof(string));
            schemaTable.Columns.Add("TABLE_COMMENT", typeof(string));
            string[] strArray = new string[4];
            if ((restrictions != null) && (restrictions.Length >= 2))
            {
                strArray[0] = restrictions[1];
            }
            DataTable databases = this.GetDatabases(strArray);
            if (restrictions != null)
            {
                Array.Copy(restrictions, strArray, Math.Min(strArray.Length, restrictions.Length));
            }
            foreach (DataRow row in databases.Rows)
            {
                strArray[1] = row["SCHEMA_NAME"].ToString();
                this.FindTables(schemaTable, strArray);
            }
            return schemaTable;
        }

        public virtual DataTable GetUDF(string[] restrictions)
        {
            string cmdText = "SELECT name,ret,dl FROM mysql.func";
            if (((restrictions != null) && (restrictions.Length >= 1)) && !string.IsNullOrEmpty(restrictions[0]))
            {
                cmdText = cmdText + string.Format(" WHERE name LIKE '{0}'", restrictions[0]);
            }
            DataTable table = new DataTable("User-defined Functions");
            table.Columns.Add("NAME", typeof(string));
            table.Columns.Add("RETURN_TYPE", typeof(int));
            table.Columns.Add("LIBRARY_NAME", typeof(string));
            MySqlCommand command = new MySqlCommand(cmdText, this.connection);
            try
            {
                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        DataRow row = table.NewRow();
                        row[0] = reader.GetString(0);
                        row[1] = reader.GetInt32(1);
                        row[2] = reader.GetString(2);
                        table.Rows.Add(row);
                    }
                }
            }
            catch (MySqlException exception)
            {
                if (exception.Number != 0x476)
                {
                    throw;
                }
                throw new MySqlException(Resources.UnableToEnumerateUDF, exception);
            }
            return table;
        }

        public virtual DataTable GetUsers(string[] restrictions)
        {
            StringBuilder builder = new StringBuilder("SELECT Host, User FROM mysql.user");
            if ((restrictions != null) && (restrictions.Length > 0))
            {
                builder.AppendFormat(CultureInfo.InvariantCulture, " WHERE User LIKE '{0}'", new object[] { restrictions[0] });
            }
            MySqlDataAdapter adapter = new MySqlDataAdapter(builder.ToString(), this.connection);
            DataTable dataTable = new DataTable();
            adapter.Fill(dataTable);
            dataTable.TableName = "Users";
            dataTable.Columns[0].ColumnName = "HOST";
            dataTable.Columns[1].ColumnName = "USERNAME";
            return dataTable;
        }

        private void LoadTableColumns(DataTable dt, string schema, string tableName, string columnRestriction)
        {
            MySqlCommand command = new MySqlCommand(string.Format("SHOW FULL COLUMNS FROM `{0}`.`{1}`", schema, tableName), this.connection);
            int num = 1;
            using (MySqlDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    string str2 = reader.GetString(0);
                    if ((columnRestriction == null) || (str2 == columnRestriction))
                    {
                        DataRow row = dt.NewRow();
                        row["TABLE_CATALOG"] = DBNull.Value;
                        row["TABLE_SCHEMA"] = schema;
                        row["TABLE_NAME"] = tableName;
                        row["COLUMN_NAME"] = str2;
                        row["ORDINAL_POSITION"] = num++;
                        row["COLUMN_DEFAULT"] = reader.GetValue(5);
                        row["IS_NULLABLE"] = reader.GetString(3);
                        row["DATA_TYPE"] = reader.GetString(1);
                        row["CHARACTER_MAXIMUM_LENGTH"] = DBNull.Value;
                        row["CHARACTER_OCTET_LENGTH"] = DBNull.Value;
                        row["NUMERIC_PRECISION"] = DBNull.Value;
                        row["NUMERIC_SCALE"] = DBNull.Value;
                        row["CHARACTER_SET_NAME"] = reader.GetValue(2);
                        row["COLLATION_NAME"] = row["CHARACTER_SET_NAME"];
                        row["COLUMN_TYPE"] = reader.GetString(1);
                        row["COLUMN_KEY"] = reader.GetString(4);
                        row["EXTRA"] = reader.GetString(6);
                        row["PRIVILEGES"] = reader.GetString(7);
                        row["COLUMN_COMMENT"] = reader.GetString(8);
                        ParseColumnRow(row);
                        dt.Rows.Add(row);
                    }
                }
            }
        }

        private static void ParseColumnRow(DataRow row)
        {
            string str = row["CHARACTER_SET_NAME"].ToString();
            int index = str.IndexOf('_');
            if (index != -1)
            {
                row["CHARACTER_SET_NAME"] = str.Substring(0, index);
            }
            string str2 = row["DATA_TYPE"].ToString();
            index = str2.IndexOf('(');
            if (index != -1)
            {
                row["DATA_TYPE"] = str2.Substring(0, index);
                int num2 = str2.IndexOf(')', index);
                string str3 = str2.Substring(index + 1, num2 - (index + 1));
                switch (row["DATA_TYPE"].ToString().ToLower())
                {
                    case "char":
                    case "varchar":
                        row["CHARACTER_MAXIMUM_LENGTH"] = str3;
                        return;

                    case "real":
                    case "decimal":
                    {
                        string[] strArray = str3.Split(new char[] { ',' });
                        row["NUMERIC_PRECISION"] = strArray[0];
                        if (strArray.Length == 2)
                        {
                            row["NUMERIC_SCALE"] = strArray[1];
                        }
                        break;
                    }
                }
            }
        }

        private static ArrayList ParseColumns(MySqlTokenizer tokenizer)
        {
            ArrayList list = new ArrayList();
            for (string str = tokenizer.NextToken(); str != ")"; str = tokenizer.NextToken())
            {
                if (str != ",")
                {
                    list.Add(str);
                }
            }
            return list;
        }

        private static void ParseConstraint(DataTable fkTable, DataRow table, MySqlTokenizer tokenizer, bool includeColumns)
        {
            string str = tokenizer.NextToken();
            DataRow row = fkTable.NewRow();
            string str2 = tokenizer.NextToken();
            if ((str2 == "foreign") && !tokenizer.Quoted)
            {
                tokenizer.NextToken();
                tokenizer.NextToken();
                row["CONSTRAINT_CATALOG"] = table["TABLE_CATALOG"];
                row["CONSTRAINT_SCHEMA"] = table["TABLE_SCHEMA"];
                row["TABLE_CATALOG"] = table["TABLE_CATALOG"];
                row["TABLE_SCHEMA"] = table["TABLE_SCHEMA"];
                row["TABLE_NAME"] = table["TABLE_NAME"];
                row["REFERENCED_TABLE_CATALOG"] = null;
                row["CONSTRAINT_NAME"] = str.Trim(new char[] { '\'', '`' });
                ArrayList srcColumns = includeColumns ? ParseColumns(tokenizer) : null;
                while ((str2 != "references") || tokenizer.Quoted)
                {
                    str2 = tokenizer.NextToken();
                }
                string str3 = tokenizer.NextToken();
                string str4 = tokenizer.NextToken();
                if (str4.StartsWith("."))
                {
                    row["REFERENCED_TABLE_SCHEMA"] = str3;
                    row["REFERENCED_TABLE_NAME"] = str4.Substring(1).Trim(new char[] { '\'', '`' });
                    tokenizer.NextToken();
                }
                else
                {
                    row["REFERENCED_TABLE_SCHEMA"] = table["TABLE_SCHEMA"];
                    row["REFERENCED_TABLE_NAME"] = str3.Substring(1).Trim(new char[] { '\'', '`' });
                }
                ArrayList targetColumns = includeColumns ? ParseColumns(tokenizer) : null;
                if (includeColumns)
                {
                    ProcessColumns(fkTable, row, srcColumns, targetColumns);
                }
                else
                {
                    fkTable.Rows.Add(row);
                }
            }
        }

        private static void ProcessColumns(DataTable fkTable, DataRow row, ArrayList srcColumns, ArrayList targetColumns)
        {
            for (int i = 0; i < srcColumns.Count; i++)
            {
                DataRow row2 = fkTable.NewRow();
                row2.ItemArray = row.ItemArray;
                row2["COLUMN_NAME"] = (string) srcColumns[i];
                row2["ORDINAL_POSITION"] = i;
                row2["REFERENCED_COLUMN_NAME"] = (string) targetColumns[i];
                fkTable.Rows.Add(row2);
            }
        }
    }
}

