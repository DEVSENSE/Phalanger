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
            string error = null;
            return Open(filename, DEFAULT_FILE_MODE, ref error, false);
        }


        [ImplementsFunction("sqlite_open")]
        [return: CastToFalse]
        public static PhpResource Open(string filename, int mode)
        {
            string error = null;
            return Open(filename, mode, ref error, false);
        }

        [ImplementsFunction("sqlite_open")]
        [return: CastToFalse]
        public static PhpResource Open(string filename, int mode, ref string error)
        {
            return Open(filename, mode, ref error, false);
        }
        #endregion

        [ImplementsFunction("sqlite_popen")]
        [return: CastToFalse]
        public static PhpResource POpen(string filename, int mode, ref string error)
        {
            return Open(filename, mode, ref error, true);
        }

        private static PhpResource Open(string filename, int mode, ref string error, bool persistent)
        {
            if (persistent) PhpException.FunctionNotSupported(PhpError.Notice);

            SQLiteConnectionStringBuilder csb = new SQLiteConnectionStringBuilder();
            csb.DataSource = filename;
            csb.Version = 3;

            PhpSQLiteDbConnection connection = new PhpSQLiteDbConnection(csb.ConnectionString);
            return connection;
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

        //[ImplementsFunction("sqlite_query")]
        //[return: CastToFalse]
        //public static PhpResource Query(PhpResource linkIdentifier, object query, object result_type, ref object error_message)
        //{
        //    PhpSQLiteDbConnection connection = PhpSQLiteDbConnection.ValidConnection(linkIdentifier);
        //    if (query == null || connection == null) return null;

        //    return connection.ExecuteQuery(PHP.Core.Convert.ObjectToString(query), true);
        //}

        //[ImplementsFunction("sqlite_query")]
        //[return: CastToFalse]
        //public static PhpResource Query(PhpResource linkIdentifier, object query)
        //{
        //    PhpSQLiteDbConnection connection = PhpSQLiteDbConnection.ValidConnection(linkIdentifier);
        //    if (query == null || connection == null) return null;

        //    return connection.ExecuteQuery(PHP.Core.Convert.ObjectToString(query), true);
        //}

        //[ImplementsFunction("sqlite_query")]
        //[return: CastToFalse]
        //public static PhpResource Query(object query, PhpResource linkIdentifier)
        //{
        //    PhpSQLiteDbConnection connection = PhpSQLiteDbConnection.ValidConnection(linkIdentifier);
        //    if (query == null || connection == null) return null;

        //    return connection.ExecuteQuery(PHP.Core.Convert.ObjectToString(query), true);
        //}

        [ImplementsFunction("sqlite_query")]
        [return: CastToFalse]
        public static PhpResource Query(object arg1, object arg2)
        {
            object error_msg = null;
            return Query(arg1, arg2, QueryResultKeys.Both, ref error_msg);
        }

        [ImplementsFunction("sqlite_query")]
        [return: CastToFalse]
        public static PhpResource Query(object arg1, object arg2, QueryResultKeys result_type)
        {
            object error_msg = null;
            return Query(arg1, arg2, result_type, ref error_msg);
        }

        [ImplementsFunction("sqlite_query")]
        [return: CastToFalse]
        public static PhpResource Query(object arg1, object arg2, QueryResultKeys result_type, ref object error_msg)
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

            return connection.ExecuteQuery(query, true);
        }

        [ImplementsFunction("sqlite_exec")]
        public static bool Exec(PhpResource linkIdentifier, object query, ref object error_message)
        {
            PhpSQLiteDbConnection connection = PhpSQLiteDbConnection.ValidConnection(linkIdentifier);
            if (query == null || connection == null) return false;

            string strQuery = PHP.Core.Convert.ObjectToString(query);
            connection.ExecuteCommand(strQuery, System.Data.CommandType.Text, true, null, true);
            return true;
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
    }
}
