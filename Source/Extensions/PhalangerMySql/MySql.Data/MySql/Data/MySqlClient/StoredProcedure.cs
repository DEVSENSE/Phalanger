namespace MySql.Data.MySqlClient
{
    using MySql.Data.MySqlClient.Properties;
    using MySql.Data.Types;
    using System;
    using System.Data;
    using System.Globalization;
    using System.Runtime.InteropServices;
    using System.Text;

    internal class StoredProcedure : PreparableStatement
    {
        private string outSelect;
        internal const string ParameterPrefix = "_cnet_param_";
        private DataTable parametersTable;
        private string resolvedCommandText;

        public StoredProcedure(MySqlCommand cmd, string text) : base(cmd, text)
        {
        }

        public override void Close(MySqlDataReader reader)
        {
            base.Close(reader);
            ResultSet resultSet = reader.ResultSet;
            if ((resultSet == null) || !resultSet.IsOutputParameters)
            {
                MySqlDataReader hackedOuputParameters = this.GetHackedOuputParameters();
                if (hackedOuputParameters == null)
                {
                    return;
                }
                reader = hackedOuputParameters;
            }
            using (reader)
            {
                string str = "@_cnet_param_";
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    string name = reader.GetName(i);
                    if (name.StartsWith(str))
                    {
                        name = name.Remove(0, str.Length);
                    }
                    base.Parameters.GetParameterFlexible(name, true).Value = reader.GetValue(i);
                }
                reader.Close();
            }
        }

        private string FixProcedureName(string name)
        {
            string[] strArray = name.Split(new char[] { '.' });
            for (int i = 0; i < strArray.Length; i++)
            {
                if (!strArray[i].StartsWith("`"))
                {
                    strArray[i] = string.Format("`{0}`", strArray[i]);
                }
            }
            if (strArray.Length == 1)
            {
                return strArray[0];
            }
            return string.Format("{0}.{1}", strArray[0], strArray[1]);
        }

        private MySqlParameter GetAndFixParameter(DataRow param, bool realAsFloat, string returnParameter)
        {
            string text1 = (string) param["PARAMETER_MODE"];
            string parameterName = (string) param["PARAMETER_NAME"];
            if (param["ORDINAL_POSITION"].Equals(0))
            {
                parameterName = returnParameter;
            }
            if (parameterName == null)
            {
                return null;
            }
            MySqlParameter parameterFlexible = base.command.Parameters.GetParameterFlexible(parameterName, true);
            if (!parameterFlexible.TypeHasBeenSet)
            {
                string typeName = (string) param["DATA_TYPE"];
                bool unsigned = GetFlags(param["DTD_IDENTIFIER"].ToString()).IndexOf("UNSIGNED") != -1;
                parameterFlexible.MySqlDbType = MetaData.NameToType(typeName, unsigned, realAsFloat, base.Connection);
            }
            return parameterFlexible;
        }

        internal string GetCacheKey(string spName)
        {
            string str = string.Empty;
            StringBuilder builder = new StringBuilder(spName);
            builder.Append("(");
            string str2 = "";
            foreach (MySqlParameter parameter in base.command.Parameters)
            {
                if (parameter.Direction == ParameterDirection.ReturnValue)
                {
                    str = "?=";
                }
                else
                {
                    builder.AppendFormat(CultureInfo.InvariantCulture, "{0}?", new object[] { str2 });
                    str2 = ",";
                }
            }
            builder.Append(")");
            return (str + builder.ToString());
        }

        public static string GetFlags(string dtd)
        {
            int startIndex = dtd.Length - 1;
            while ((startIndex > 0) && (char.IsLetterOrDigit(dtd[startIndex]) || (dtd[startIndex] == ' ')))
            {
                startIndex--;
            }
            return dtd.Substring(startIndex).ToUpper(CultureInfo.InvariantCulture);
        }

        private MySqlDataReader GetHackedOuputParameters()
        {
            if (this.outSelect.Length == 0)
            {
                return null;
            }
            MySqlDataReader reader = new MySqlCommand("SELECT " + this.outSelect, base.Connection).ExecuteReader();
            ResultSet resultSet = reader.ResultSet;
            for (int i = 0; i < reader.FieldCount; i++)
            {
                string parameterName = reader.GetName(i).Remove(0, "_cnet_param_".Length + 1);
                IMySqlValue iMySqlValue = MySqlField.GetIMySqlValue(base.Parameters.GetParameterFlexible(parameterName, true).MySqlDbType);
                if (iMySqlValue is MySqlBit)
                {
                    MySqlBit valueObject = (MySqlBit) iMySqlValue;
                    valueObject.ReadAsString = true;
                    resultSet.SetValueObject(i, valueObject);
                }
                else
                {
                    resultSet.SetValueObject(i, iMySqlValue);
                }
            }
            if (!reader.Read())
            {
                reader.Close();
                return null;
            }
            return reader;
        }

        private void GetParameters(string procName, out DataTable proceduresTable, out DataTable parametersTable)
        {
            string cacheKey = this.GetCacheKey(procName);
            DataSet set = base.Connection.ProcedureCache.GetProcedure(base.Connection, procName, cacheKey);
            if ((set.Tables.Count == 2) && base.Connection.Settings.UseProcedureBodies)
            {
                lock (set)
                {
                    proceduresTable = set.Tables["procedures"];
                    parametersTable = set.Tables["procedure parameters"];
                    return;
                }
            }
            lock (set)
            {
                proceduresTable = set.Tables["procedures"];
            }
            parametersTable = new ISSchemaProvider(base.Connection).CreateParametersTable();
            int num = 1;
            foreach (MySqlParameter parameter in base.command.Parameters)
            {
                if (!parameter.TypeHasBeenSet)
                {
                    throw new InvalidOperationException(Resources.NoBodiesAndTypeNotSet);
                }
                DataRow row = parametersTable.NewRow();
                row["PARAMETER_NAME"] = parameter.ParameterName;
                row["PARAMETER_MODE"] = "IN";
                if (parameter.Direction == ParameterDirection.InputOutput)
                {
                    row["PARAMETER_MODE"] = "INOUT";
                }
                else if (parameter.Direction == ParameterDirection.Output)
                {
                    row["PARAMETER_MODE"] = "OUT";
                }
                else if (parameter.Direction == ParameterDirection.ReturnValue)
                {
                    row["PARAMETER_MODE"] = "OUT";
                    row["ORDINAL_POSITION"] = 0;
                }
                else
                {
                    row["ORDINAL_POSITION"] = num++;
                }
                parametersTable.Rows.Add(row);
            }
            if (base.Connection.Settings.UseProcedureBodies)
            {
                lock (set)
                {
                    if (set.Tables.Contains("Procedure Parameters"))
                    {
                        set.Tables.Remove("Procedure Parameters");
                    }
                    set.Tables.Add(parametersTable);
                }
            }
        }

        private string GetReturnParameter()
        {
            if (base.Parameters != null)
            {
                foreach (MySqlParameter parameter in base.Parameters)
                {
                    if (parameter.Direction == ParameterDirection.ReturnValue)
                    {
                        return parameter.ParameterName.Substring(1);
                    }
                }
            }
            return null;
        }

        public override void Resolve(bool preparing)
        {
            if (this.resolvedCommandText == null)
            {
                DataTable table;
                string commandText = base.commandText;
                if ((commandText.IndexOf(".") == -1) && !string.IsNullOrEmpty(base.Connection.Database))
                {
                    commandText = base.Connection.Database + "." + commandText;
                }
                commandText = this.FixProcedureName(commandText);
                this.GetParameters(commandText, out table, out this.parametersTable);
                if (table.Rows.Count == 0)
                {
                    throw new InvalidOperationException(string.Format(Resources.RoutineNotFound, commandText));
                }
                bool realAsFloat = table.Rows[0]["SQL_MODE"].ToString().IndexOf("REAL_AS_FLOAT") != -1;
                StringBuilder builder = new StringBuilder();
                StringBuilder builder2 = new StringBuilder();
                string str2 = "";
                string str3 = "";
                string returnParameter = this.GetReturnParameter();
                foreach (DataRow row in this.parametersTable.Rows)
                {
                    MySqlParameter parameter = this.GetAndFixParameter(row, realAsFloat, returnParameter);
                    if ((parameter != null) && !row["ORDINAL_POSITION"].Equals(0))
                    {
                        string parameterName = parameter.ParameterName;
                        string str6 = parameterName;
                        if (parameterName.StartsWith("@") || parameterName.StartsWith("?"))
                        {
                            parameterName = parameterName.Substring(1);
                        }
                        else
                        {
                            str6 = "@" + str6;
                        }
                        string str7 = str6;
                        if (((parameter.Direction != ParameterDirection.Input) && !base.Connection.driver.SupportsOutputParameters) && !preparing)
                        {
                            MySqlCommand command = new MySqlCommand(string.Format("SET @{0}{1}={2}", "_cnet_param_", parameterName, str6), base.Connection);
                            command.Parameters.Add(parameter);
                            command.ExecuteNonQuery();
                            str7 = string.Format("@{0}{1}", "_cnet_param_", parameterName);
                            builder2.AppendFormat(CultureInfo.InvariantCulture, "{0}{1}", new object[] { str3, str7 });
                            str3 = ", ";
                        }
                        builder.AppendFormat(CultureInfo.InvariantCulture, "{0}{1}", new object[] { str2, str7 });
                        str2 = ", ";
                    }
                }
                string str9 = builder.ToString().TrimEnd(new char[] { ' ', ',' });
                this.outSelect = builder2.ToString().TrimEnd(new char[] { ' ', ',' });
                if (table.Rows[0]["ROUTINE_TYPE"].Equals("PROCEDURE"))
                {
                    str9 = string.Format("call {0} ({1})", commandText, str9);
                }
                else
                {
                    if (returnParameter == null)
                    {
                        returnParameter = "_cnet_param_dummy";
                    }
                    else
                    {
                        this.outSelect = string.Format("@{0}{1}", "_cnet_param_", returnParameter);
                    }
                    str9 = string.Format("SET @{0}{1}={2}({3})", new object[] { "_cnet_param_", returnParameter, commandText, str9 });
                }
                this.resolvedCommandText = str9;
            }
        }

        public override string ResolvedCommandText
        {
            get
            {
                return this.resolvedCommandText;
            }
        }
    }
}

