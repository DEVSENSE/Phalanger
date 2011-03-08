namespace MySql.Data.MySqlClient
{
    using System;
    using System.Data;
    using System.Text;

    public sealed class MySqlHelper
    {
        private static CharClass[] charClassArray = makeCharClassArray();
        private static string stringOfBackslashChars = "\\\x00a5Š₩∖﹨＼";
        private static string stringOfQuoteChars = "'`\x00b4ʹʺʻʼˈˊˋ˙̀́‘’‚′‵❛❜＇";

        private MySqlHelper()
        {
        }

        public static string DoubleQuoteString(string value)
        {
            if (!needsQuoting(value))
            {
                return value;
            }
            StringBuilder builder = new StringBuilder();
            foreach (char ch in value)
            {
                switch (charClassArray[ch])
                {
                    case CharClass.Quote:
                        builder.Append(ch);
                        break;

                    case CharClass.Backslash:
                        builder.Append(@"\");
                        break;
                }
                builder.Append(ch);
            }
            return builder.ToString();
        }

        public static string EscapeString(string value)
        {
            if (!needsQuoting(value))
            {
                return value;
            }
            StringBuilder builder = new StringBuilder();
            foreach (char ch in value)
            {
                if (charClassArray[ch] != CharClass.None)
                {
                    builder.Append(@"\");
                }
                builder.Append(ch);
            }
            return builder.ToString();
        }

        public static DataRow ExecuteDataRow(string connectionString, string commandText, params MySqlParameter[] parms)
        {
            DataSet set = ExecuteDataset(connectionString, commandText, parms);
            if (set == null)
            {
                return null;
            }
            if (set.Tables.Count == 0)
            {
                return null;
            }
            if (set.Tables[0].Rows.Count == 0)
            {
                return null;
            }
            return set.Tables[0].Rows[0];
        }

        public static DataSet ExecuteDataset(MySqlConnection connection, string commandText)
        {
            return ExecuteDataset(connection, commandText, null);
        }

        public static DataSet ExecuteDataset(string connectionString, string commandText)
        {
            return ExecuteDataset(connectionString, commandText, null);
        }

        public static DataSet ExecuteDataset(MySqlConnection connection, string commandText, params MySqlParameter[] commandParameters)
        {
            MySqlCommand selectCommand = new MySqlCommand();
            selectCommand.Connection = connection;
            selectCommand.CommandText = commandText;
            selectCommand.CommandType = CommandType.Text;
            if (commandParameters != null)
            {
                foreach (MySqlParameter parameter in commandParameters)
                {
                    selectCommand.Parameters.Add(parameter);
                }
            }
            MySqlDataAdapter adapter = new MySqlDataAdapter(selectCommand);
            DataSet dataSet = new DataSet();
            adapter.Fill(dataSet);
            selectCommand.Parameters.Clear();
            return dataSet;
        }

        public static DataSet ExecuteDataset(string connectionString, string commandText, params MySqlParameter[] commandParameters)
        {
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                return ExecuteDataset(connection, commandText, commandParameters);
            }
        }

        public static int ExecuteNonQuery(MySqlConnection connection, string commandText, params MySqlParameter[] commandParameters)
        {
            MySqlCommand command = new MySqlCommand();
            command.Connection = connection;
            command.CommandText = commandText;
            command.CommandType = CommandType.Text;
            if (commandParameters != null)
            {
                foreach (MySqlParameter parameter in commandParameters)
                {
                    command.Parameters.Add(parameter);
                }
            }
            int num = command.ExecuteNonQuery();
            command.Parameters.Clear();
            return num;
        }

        public static int ExecuteNonQuery(string connectionString, string commandText, params MySqlParameter[] parms)
        {
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                return ExecuteNonQuery(connection, commandText, parms);
            }
        }

        public static MySqlDataReader ExecuteReader(MySqlConnection connection, string commandText)
        {
            return ExecuteReader(connection, null, commandText, null, true);
        }

        public static MySqlDataReader ExecuteReader(string connectionString, string commandText)
        {
            return ExecuteReader(connectionString, commandText, null);
        }

        public static MySqlDataReader ExecuteReader(MySqlConnection connection, string commandText, params MySqlParameter[] commandParameters)
        {
            return ExecuteReader(connection, null, commandText, commandParameters, true);
        }

        public static MySqlDataReader ExecuteReader(string connectionString, string commandText, params MySqlParameter[] commandParameters)
        {
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                return ExecuteReader(connection, null, commandText, commandParameters, false);
            }
        }

        private static MySqlDataReader ExecuteReader(MySqlConnection connection, MySqlTransaction transaction, string commandText, MySqlParameter[] commandParameters, bool ExternalConn)
        {
            MySqlDataReader reader;
            MySqlCommand command = new MySqlCommand();
            command.Connection = connection;
            command.Transaction = transaction;
            command.CommandText = commandText;
            command.CommandType = CommandType.Text;
            if (commandParameters != null)
            {
                foreach (MySqlParameter parameter in commandParameters)
                {
                    command.Parameters.Add(parameter);
                }
            }
            if (ExternalConn)
            {
                reader = command.ExecuteReader();
            }
            else
            {
                reader = command.ExecuteReader(CommandBehavior.CloseConnection);
            }
            command.Parameters.Clear();
            return reader;
        }

        public static object ExecuteScalar(MySqlConnection connection, string commandText)
        {
            return ExecuteScalar(connection, commandText, null);
        }

        public static object ExecuteScalar(string connectionString, string commandText)
        {
            return ExecuteScalar(connectionString, commandText, null);
        }

        public static object ExecuteScalar(MySqlConnection connection, string commandText, params MySqlParameter[] commandParameters)
        {
            MySqlCommand command = new MySqlCommand();
            command.Connection = connection;
            command.CommandText = commandText;
            command.CommandType = CommandType.Text;
            if (commandParameters != null)
            {
                foreach (MySqlParameter parameter in commandParameters)
                {
                    command.Parameters.Add(parameter);
                }
            }
            object obj2 = command.ExecuteScalar();
            command.Parameters.Clear();
            return obj2;
        }

        public static object ExecuteScalar(string connectionString, string commandText, params MySqlParameter[] commandParameters)
        {
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                return ExecuteScalar(connection, commandText, commandParameters);
            }
        }

        private static CharClass[] makeCharClassArray()
        {
            CharClass[] classArray = new CharClass[0x10000];
            foreach (char ch in stringOfBackslashChars)
            {
                classArray[ch] = CharClass.Backslash;
            }
            foreach (char ch2 in stringOfQuoteChars)
            {
                classArray[ch2] = CharClass.Quote;
            }
            return classArray;
        }

        private static bool needsQuoting(string s)
        {
            foreach (char ch in s)
            {
                if (charClassArray[ch] != CharClass.None)
                {
                    return true;
                }
            }
            return false;
        }

        public static void UpdateDataSet(string connectionString, string commandText, DataSet ds, string tablename)
        {
            MySqlConnection connection = new MySqlConnection(connectionString);
            connection.Open();
            MySqlDataAdapter adapter = new MySqlDataAdapter(commandText, connection);
            new MySqlCommandBuilder(adapter).ToString();
            adapter.Update(ds, tablename);
            connection.Close();
        }

        private enum CharClass : byte
        {
            Backslash = 2,
            None = 0,
            Quote = 1
        }
    }
}

