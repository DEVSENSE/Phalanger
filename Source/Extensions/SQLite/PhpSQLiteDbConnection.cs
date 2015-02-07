using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SQLite;
using PHP.Core;
using System.Data;

namespace PHP.Library.Data
{
    public sealed class PhpSQLiteDbConnection : PhpDbConnection
    {
        internal SQLiteConnection Connection { get { return (SQLiteConnection)connection; } }

        /// <summary>
        /// Server.
        /// </summary>
        public string/*!*/ Server { get { return server; } }
        private string/*!*/ server;
        internal void SetServer(string/*!*/ value) { server = value; }

        /// <summary>
        /// Creates a connection resource.
        /// </summary>
        public PhpSQLiteDbConnection(string/*!*/ connectionString)
            : base(connectionString, new SQLiteConnection(), "sqlite connection")
        {
        }

        internal static PhpSQLiteDbConnection ValidConnection(PhpResource handle)
        {
            PhpSQLiteDbConnection connection;

            if (handle != null && handle.GetType() == typeof(PhpSQLiteDbConnection))
                connection = (PhpSQLiteDbConnection)handle;
            else
                connection = null;

            if (connection != null && connection.IsValid)
                return connection;

            PhpException.Throw(PhpError.Warning, LibResources.GetString("invalid_connection_resource"));
            return null;
        }

        /// <summary>
        /// Gets a query result resource.
        /// </summary>
        /// <param name="connection">Database connection.</param>
        /// <param name="reader">Data reader to be used for result resource population.</param>
        /// <param name="convertTypes">Whether to convert data types to PHP ones.</param>
        /// <returns>Result resource holding all resulting data of the query.</returns>
        protected override PhpDbResult GetResult(PhpDbConnection/*!*/ connection, IDataReader/*!*/ reader, bool convertTypes)
        {
            return new PhpSQLiteDbResult(connection, reader, convertTypes);
        }

        /// <summary>
        /// Command factory.
        /// </summary>
        /// <returns>An empty instance of <see cref="SQLiteCommand"/>.</returns>
        protected override IDbCommand/*!*/ CreateCommand()
        {
            return new SQLiteCommand();
        }

        /// <summary>
        /// Gets last error number.
        /// </summary>
        /// <returns>The error number it is known, -1 if unknown error occured, or zero on success.</returns>
        public override int GetLastErrorNumber()
        {
            if (LastException == null) return 0;

            SQLiteException e = LastException as SQLiteException;
            return (e != null) ? (int)e.ErrorCode : -1;
        }

        /// <summary>
        /// Gets the last error message.
        /// </summary>
        /// <returns>The message or an empty string if no error occured.</returns>
        public override string GetLastErrorMessage()
        {
            return StripErrorNumber(base.GetLastErrorMessage());
        }


        /// <summary>
        /// Gets a message from an exception raised by the connector.
        /// Removes the initial #{number} and the ending dot.
        /// </summary>
        /// <param name="e">Exception.</param>
        /// <returns>The message.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="e"/> is a <B>null</B> reference.</exception>
        public override string GetExceptionMessage(Exception/*!*/ e)
        {
            if (e == null) throw new ArgumentNullException("e");

            SQLiteException mye = e as SQLiteException;
            if (mye == null || mye.Message.Length == 0) return e.Message;

            string msg = StripErrorNumber(mye.Message);

            // skip last dot:
            int j = msg.Length;
            if (msg[j - 1] == '.') j--;

            return String.Format("{0} (error {1})", msg.Substring(0, j), mye.ErrorCode);
        }

        private string StripErrorNumber(string msg)
        {
            // find first non-digit:
            if (msg.Length > 0 && msg[0] == '#')
            {
                int i = 1;
                while (i < msg.Length && msg[i] >= '0' && msg[i] <= '9') i++;
                return msg.Substring(i);
            }
            else
            {
                return msg;
            }
        }

        /// <summary>
        /// Queries server for a value of a global variable.
        /// </summary>
        /// <param name="name">Global variable name.</param>
        /// <returns>Global variable value (converted).</returns>
        internal object QueryGlobalVariable(string name)
        {
            // TODO: better query:

            PhpDbResult result = ExecuteQuery("SHOW GLOBAL VARIABLES LIKE '" + name + "'", true);

            // default value
            if (result.FieldCount != 2 || result.RowCount != 1)
                return null;

            return result.GetFieldValue(0, 1);
        }
    }
}
