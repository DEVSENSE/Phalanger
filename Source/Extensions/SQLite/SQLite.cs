using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PHP.Core;
using System.Data.SQLite;

namespace PHP.Library.Data
{
    public static partial class SQLite
    {
        public const int DEFAULT_FILE_MODE = 438; //666 octal

        #region sqlite_open
        [ImplementsFunction("sqlite_open")]
        [return: CastToFalse]
        public static PhpResource Open(string filename)
        {
            return Open(filename, DEFAULT_FILE_MODE, null, false);
        }


        [ImplementsFunction("sqlite_open")]
        [return: CastToFalse]
        public static PhpResource Open(string filename, int mode)
        {
            return Open(filename, mode, null, false);
        }

        [ImplementsFunction("sqlite_open")]
        [return: CastToFalse]
        public static PhpResource Open(string filename, int mode, PhpReference error)
        {
            return Open(filename, mode, error, false);
        }
        #endregion

        [ImplementsFunction("sqlite_popen")]
        [return: CastToFalse]
        public static PhpResource POpen(string filename, int mode, PhpReference error)
        {
            return Open(filename, mode, error, true);
        }

        private static PhpResource Open(string filename, int mode, PhpReference error, bool persistent)
        {
            if (persistent) PhpException.FunctionNotSupported(PhpError.Notice);

            SQLiteConnectionStringBuilder csb = new SQLiteConnectionStringBuilder();
            csb.DataSource = filename;
            csb.Version = 3;

            try
            {
                PhpSQLiteDbConnection connection = new PhpSQLiteDbConnection(csb.ConnectionString);
                if (error != null)
                {
                    error.Value = null;
                }
                return connection;
            }
            catch (Exception ex)
            {
                if (error != null)
                {
                    error.Value = ex.Message;
                }
                return null;
            }
        }

        /// <summary>
        /// Closes a specified connection.
        /// </summary>
        /// <param name="linkIdentifier">The connection resource.</param>
        /// <returns><B>true</B> on success, <B>false</B> on failure.</returns>
        [ImplementsFunction("sqlite_close")]
        public static void Close(PhpResource linkIdentifier)
        {
            PhpSQLiteDbConnection connection = PhpSQLiteDbConnection.ValidConnection(linkIdentifier);
            if (connection != null)
            {
                connection.Close();
            }
        }

        #region sqlite_query
        [ImplementsFunction("sqlite_query")]
        public static PhpResource Query(object arg1, object arg2)
        {
            return Query(arg1, arg2, QueryResultKeys.Both, null);
        }

        [ImplementsFunction("sqlite_query")]
        public static PhpResource Query(object arg1, object arg2, QueryResultKeys result_type)
        {
            return Query(arg1, arg2, result_type, null);
        }

        [ImplementsFunction("sqlite_query")]
        public static PhpResource Query(object arg1, object arg2, QueryResultKeys result_type, PhpReference error_msg)
        {
            PhpSQLiteDbConnection connection = PhpSQLiteDbConnection.ValidConnection(arg1 as PhpResource);
            string query;
            if (connection == null)
            {
                connection = PhpSQLiteDbConnection.ValidConnection(arg2 as PhpResource);
                query = PHP.Core.Convert.ObjectToString(arg1);
            }
            else
            {
                query = PHP.Core.Convert.ObjectToString(arg2);
            }

            if (query == null || connection == null)
                return null;

            try
            {
                var result = connection.ExecuteQuery(query, true);
                if (error_msg != null)
                {
                    error_msg.Value = null;
                }
                return result;
            }
            catch (Exception ex)
            {
                if (error_msg != null)
                {
                    error_msg.Value = ex.Message;
                }
                return null;
            }
        }
        #endregion

        #region sqlite_exec
        [ImplementsFunction("sqlite_exec")]
        public static bool Exec(object arg1, object arg2)
        {
            return Exec(arg1, arg2, null);
        }

        [ImplementsFunction("sqlite_exec")]
        public static bool Exec(object arg1, object arg2, PhpReference error_message)
        {
            PhpSQLiteDbConnection connection = PhpSQLiteDbConnection.ValidConnection(arg1 as PhpResource);
            string query;
            if (connection == null)
            {
                connection = PhpSQLiteDbConnection.ValidConnection(arg2 as PhpResource);
                query = PHP.Core.Convert.ObjectToString(arg1);
            }
            else
            {
                query = PHP.Core.Convert.ObjectToString(arg2);
            }

            if (query == null || connection == null)
                return false;

            try
            {
                connection.ExecuteCommand(query, System.Data.CommandType.Text, true, null, true);
                if (error_message != null)
                {
                    error_message.Value = null;
                }
                return true;
            }
            catch (Exception ex)
            {
                if (error_message != null)
                {
                    error_message.Value = ex.Message;
                }
                return false;
            }
        }
        #endregion

        [ImplementsFunction("sqlite_fetch_array")]
        [return: CastToFalse]
        public static PhpArray FetchArray(PhpResource resultIdentifier)
        {
            return FetchArray(resultIdentifier, QueryResultKeys.Both, true);
        }

        [ImplementsFunction("sqlite_fetch_array")]
        [return: CastToFalse]
        public static PhpArray FetchArray(PhpResource resultIdentifier, object result_type)
        {
            return FetchArray(resultIdentifier, result_type, true);
        }

        [ImplementsFunction("sqlite_fetch_array")]
        [return: CastToFalse]
        public static PhpArray FetchArray(PhpResource resultIdentifier, object result_type, object decode_binary)
        {
            PhpSQLiteDbResult res = PhpSQLiteDbResult.ValidResult(resultIdentifier);
            if (res == null)
            {
                return null;
            }
            SQLite.QueryResultKeys resType = QueryResultKeys.Both;
            if (result_type != null)
            {
                int val = PHP.Core.Convert.ObjectToInteger(result_type);
                if (val != 0)
                {
                    resType = (SQLite.QueryResultKeys)val;
                }
            }
            bool intKey = (resType & SQLite.QueryResultKeys.Numbers) == SQLite.QueryResultKeys.Numbers;
            bool strKey = (resType & SQLite.QueryResultKeys.ColumnNames) == SQLite.QueryResultKeys.ColumnNames;
            PhpArray arr = res.FetchArray(intKey, strKey);
            return arr;
        }

        #region fetch_all
        [ImplementsFunction("sqlite_fetch_all")]
        [return: CastToFalse]
        public static PhpArray FetchAll(PhpResource resultIdentifier)
        {
            return FetchAll(resultIdentifier, null, true);
        }

        [ImplementsFunction("sqlite_fetch_all")]
        [return: CastToFalse]
        public static PhpArray FetchAll(PhpResource resultIdentifier, object result_type)
        {
            return FetchAll(resultIdentifier, result_type, true);
        }

        [ImplementsFunction("sqlite_fetch_all")]
        [return: CastToFalse]
        public static PhpArray FetchAll(PhpResource resultIdentifier, object result_type, object decode_binary)
        {
            PhpSQLiteDbResult res = PhpSQLiteDbResult.ValidResult(resultIdentifier);
            if (res == null)
            {
                return null;
            }
            SQLite.QueryResultKeys resType = QueryResultKeys.Both;
            if (result_type != null)
            {
                int val = PHP.Core.Convert.ObjectToInteger(result_type);
                if (val != 0)
                {
                    resType = (SQLite.QueryResultKeys)val;
                }
            }
            bool intKey = (resType & SQLite.QueryResultKeys.Numbers) == SQLite.QueryResultKeys.Numbers;
            bool strKey = (resType & SQLite.QueryResultKeys.ColumnNames) == SQLite.QueryResultKeys.ColumnNames;

            PhpArray arr = new PhpArray();
            PhpArray line = null;
            while ((line = res.FetchArray(intKey, strKey)) != null)
            {
                arr.AddToEnd(line);
            }

            if (arr.Count == 0)
            {
                return null;
            }
            return arr;
        }
        #endregion

        #region sqlite_create_function
        [ImplementsFunction("sqlite_create_function")]
        [PhpVisible]
        public static void CreateFunction(PhpResource dbhandle, string function_name, string callback)
        {
            CreateFunction(dbhandle, function_name, callback, -1);
        }
        [ImplementsFunction("sqlite_create_function")]
        [PhpVisible]
        public static void CreateFunction(PhpResource dbhandle, string function_name, object callback, int num_args)
        {
            PhpSQLiteDbConnection connection = PhpSQLiteDbConnection.ValidConnection(dbhandle);
            PhpCallback cb = PHP.Core.Convert.ObjectToCallback(callback);

            int paramCount = cb.TargetRoutine.PhpFunction.ArgFullInfo.GetParameters().Length;
            Delegate d = null;
            connection.Connection.RegisterFunction(function_name, paramCount, FunctionType.Scalar, d);
            //SQLiteFunction.RegisterFunction()
        }
        #endregion
    }
}
