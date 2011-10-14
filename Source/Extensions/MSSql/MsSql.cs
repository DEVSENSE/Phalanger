/*

 Copyright (c) 2005-2006 Tomas Matousek and Martin Maly.  

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Data;
using System.Collections;
using System.Text;
using System.Data.SqlClient;

using PHP.Core;

namespace PHP.Library.Data
{
	/// <summary>
	/// Implements PHP functions provided by MSSQL extension.
	/// </summary>
	public static class MsSql
	{
		#region Enums

		/// <summary>
		/// Query result array format.
		/// </summary>
		[Flags]
		public enum QueryResultKeys
		{
			/// <summary>
			/// Add items keyed by column names.
			/// </summary>
			[ImplementsConstant("MSSQL_ASSOC")]
			ColumnNames = 1,

			/// <summary>
			/// Add items keyed by column indices.
			/// </summary>
			[ImplementsConstant("MSSQL_NUM")]
			Numbers = 2,

			/// <summary>
			/// Add both items keyed by column names and items keyd by column indices.
			/// </summary>
			[ImplementsConstant("MSSQL_BOTH")]
			Both = ColumnNames | Numbers
		}

		/// <summary>
		/// Types of variables bound to stored procedure parameters.
		/// </summary>
		public enum VariableType
		{
			[ImplementsConstant("SQLBIT")]
			Bit = 50,

			[ImplementsConstant("SQLTEXT")]
			Text = 35,
			[ImplementsConstant("SQLVARCHAR")]
			VarChar = 39,
			[ImplementsConstant("SQLCHAR")]
			Char = 47,

			[ImplementsConstant("SQLINT1")]
			Int8 = 48,
			[ImplementsConstant("SQLINT2")]
			Int16 = 52,
			[ImplementsConstant("SQLINT4")]
			Int32 = 56,

			[ImplementsConstant("SQLFLT4")]
			Float = 59,
			[ImplementsConstant("SQLFLT8")]
			Double = 62,
			[ImplementsConstant("SQLFLTN")]
			FloatN = 109
		}

		#endregion

		#region Thread Static Variables

		private static SqlConnectionManager manager
		{
			get
			{
				if (_manager == null) _manager = new SqlConnectionManager();
				return _manager;
			}
		}
		[ThreadStatic]
		private static SqlConnectionManager _manager;

		[ThreadStatic]
		private static string failConnectErrorMessage = "";

		/// <summary>
		/// Clears thread static fields at the end of each request.
		/// </summary>
		private static void Clear()
		{
			_manager = null;
			failConnectErrorMessage = "";
		}

		static MsSql()
		{
			RequestContext.RequestEnd += new Action(Clear);
		}

		private static void UpdateConnectErrorInfo(PhpSqlDbConnection connection)
		{
			failConnectErrorMessage = connection.GetLastErrorMessage();
		}

		#endregion

		#region mssql_close

		/// <summary>
		/// Close last connection.
		/// </summary>
		/// <returns><B>true</B> on success, <B>false</B> on failure.</returns>
		[ImplementsFunction("mssql_close")]
		public static bool Close()
		{
			PhpDbConnection last_connection = manager.GetLastConnection();

			if (last_connection == null) return false;

			manager.RemoveConnection(last_connection);

			last_connection.Close();
			last_connection = null;
			return true;
		}

		/// <summary>
		/// Closes a specified connection.
		/// </summary>
		/// <param name="linkIdentifier">The connection resource.</param>
		/// <returns><B>true</B> on success, <B>false</B> on failure.</returns>
		[ImplementsFunction("mssql_close")]
		public static bool Close(PhpResource linkIdentifier)
		{
			PhpSqlDbConnection connection = PhpSqlDbConnection.ValidConnection(linkIdentifier);
			if (connection == null) return false;

			manager.RemoveConnection(connection);

			connection.Close();
			return true;
		}

		#endregion

		#region mssql_connect, NS: mssql_pconnect

		/// <summary>
		/// Establishes a new connection to SQL server using default server, credentials, and flags.
		/// </summary>
		/// <returns>
		/// Resource representing the connection or a <B>null</B> reference (<B>false</B> in PHP) on failure.
		/// </returns>
		/// <remarks>
		/// Default values are taken from the configuration.
		/// </remarks>
		[ImplementsFunction("mssql_connect")]
		[return: CastToFalse]
		public static PhpResource Connect()
		{
			return Connect(null, null, null, false, false);
		}

		/// <summary>
		/// Establishes a new connection to SQL server using a specified server and default credentials and flags.
		/// </summary>
		/// <param name="server">Server (host). A <b>null</b> reference means the default value.</param>
		/// <returns>
		/// Resource representing the connection or a <B>null</B> reference (<B>false</B> in PHP) on failure.
		/// </returns>
		/// <remarks>
		/// Default values are taken from the configuration.
		/// </remarks>		
		[ImplementsFunction("mssql_connect")]
		[return: CastToFalse]
		public static PhpResource Connect(string server)
		{
			return Connect(server, null, null, false, false);
		}

		/// <summary>
		/// Establishes a new connection to SQL server using a specified server and user and default password and flags.
		/// </summary>
		/// <param name="server">Server (host). A <b>null</b> reference means the default value.</param>
		/// <param name="user">User name. A <b>null</b> reference means the default value.</param>
		/// <returns>
		/// Resource representing the connection or a <B>null</B> reference (<B>false</B> in PHP) on failure.
		/// </returns>
		/// <remarks>
		/// Default values are taken from the configuration.
		/// </remarks>		
		[ImplementsFunction("mssql_connect")]
		[return: CastToFalse]
		public static PhpResource Connect(string server, string user)
		{
			return Connect(server, user, null, false, false);
		}

		/// <summary>
		/// Establishes a new connection to SQL server using a specified server, user, and password and default flags.
		/// </summary>
		/// <param name="server">Server (host). A <b>null</b> reference means the default value.</param>
		/// <param name="user">User name. A <b>null</b> reference means the default value.</param>
		/// <param name="password">Password. A <b>null</b> reference means the default value.</param>
		/// <returns>
		/// Resource representing the connection or a <B>null</B> reference (<B>false</B> in PHP) on failure.
		/// </returns>
		/// <remarks>
		/// Default values are taken from the configuration.
		/// </remarks>		
		[ImplementsFunction("mssql_connect")]
		[return: CastToFalse]
		public static PhpResource Connect(string server, string user, string password)
		{
			return Connect(server, user, password, false, false);
		}

		/// <summary>
		/// Establishes a connection to SQL server using a specified server, user, and password and default flags.
		/// </summary>
		/// <param name="server">Server (host). A <b>null</b> reference means the default value.</param>
		/// <param name="user">User name. A <b>null</b> reference means the default value.</param>
		/// <param name="password">Password. A <b>null</b> reference means the default value.</param>
		/// <param name="newLink">Whether to create a new link (ignored by Phalanger).</param>
		/// <returns>
		/// Resource representing the connection or a <B>null</B> reference (<B>false</B> in PHP) on failure.
		/// </returns>
		/// <remarks>
		/// Default values are taken from the configuration.
		/// </remarks>		
		[ImplementsFunction("mssql_connect")]
		[return: CastToFalse]
		public static PhpResource Connect(string server, string user, string password, bool newLink)
		{
			return Connect(server, user, password, newLink, false);
		}


		/// <summary>
		/// Establishes a new connection to MySQL server using default server, credentials, and flags.
		/// </summary>
		/// <returns>
		/// Resource representing the connection or a <B>null</B> reference (<B>false</B> in PHP) on failure.
		/// </returns>
		/// <remarks>
		/// Default values are taken from the configuration.
		/// </remarks>		
		[ImplementsFunction("mssql_pconnect")]
		[return: CastToFalse]
		public static PhpResource PersistentConnect()
		{
			return Connect(null, null, null, false, true);
		}

		/// <summary>
		/// Establishes a new connection to SQL server using default server, credentials, and flags.
		/// </summary>
		/// <returns>
		/// Resource representing the connection or a <B>null</B> reference (<B>false</B> in PHP) on failure.
		/// </returns>
		/// <remarks>
		/// Default values are taken from the configuration.
		/// </remarks>		
		[ImplementsFunction("mssql_pconnect")]
		[return: CastToFalse]
		public static PhpResource PersistentConnect(string server)
		{
			return Connect(server, null, null, false, true);
		}

		/// <summary>
		/// Establishes a new connection to SQL server using a specified server and user and default password and flags.
		/// </summary>
		/// <param name="server">Server (host). A <b>null</b> reference means the default value.</param>
		/// <param name="user">User name. A <b>null</b> reference means the default value.</param>
		/// <returns>
		/// Resource representing the connection or a <B>null</B> reference (<B>false</B> in PHP) on failure.
		/// </returns>
		/// <remarks>
		/// Default values are taken from the configuration.
		/// Creates a non-persistent connection. Persistent connections are not supported.
		/// </remarks>			
		[ImplementsFunction("mssql_pconnect")]
		[return: CastToFalse]
		public static PhpResource PersistentConnect(string server, string user)
		{
			return Connect(server, user, null, false, true);
		}

		/// <summary>
		/// Establishes a new connection to SQL server using a specified server, user, and password and default flags.
		/// </summary>
		/// <param name="server">Server (host). A <b>null</b> reference means the default value.</param>
		/// <param name="user">User name. A <b>null</b> reference means the default value.</param>
		/// <param name="password">Password. A <b>null</b> reference means the default value.</param>
		/// <returns>
		/// Resource representing the connection or a <B>null</B> reference (<B>false</B> in PHP) on failure.
		/// </returns>
		/// <remarks>
		/// Default values are taken from the configuration.
		/// Creates a non-persistent connection. Persistent connections are not supported.
		/// </remarks>			
		[ImplementsFunction("mssql_pconnect")]
		[return: CastToFalse]
		public static PhpResource PersistentConnect(string server, string user, string password)
		{
			return Connect(server, user, password, false, true);
		}

		/// <summary>
		/// Establishes a connection to SQL server using a specified server, user, and password and default flags.
		/// </summary>
		/// <param name="server">Server (host). A <b>null</b> reference means the default value.</param>
		/// <param name="user">User name. A <b>null</b> reference means the default value.</param>
		/// <param name="password">Password. A <b>null</b> reference means the default value.</param>
		/// <param name="newLink">Whether to create a new link (ignored by Phalanger).</param>
		/// <returns>
		/// Resource representing the connection or a <B>null</B> reference (<B>false</B> in PHP) on failure.
		/// </returns>
		/// <remarks>
		/// Default values are taken from the configuration.
		/// Creates a non-persistent connection. Persistent connections are not supported.
		/// </remarks>		
		[ImplementsFunction("mssql_pconnect")]
		[return: CastToFalse]
		public static PhpResource PersistentConnect(string server, string user, string password, bool newLink)
		{
			return Connect(server, user, password, newLink, true);
		}

		private static PhpResource Connect(string server, string user, string password, bool newLink, bool persistent)
		{
			// persistent connections are treated as transient, a warning is issued:
			if (persistent)
				PhpException.FunctionNotSupported(PhpError.Notice);

			MsSqlLocalConfig local = MsSqlConfiguration.Local;
			MsSqlGlobalConfig global = MsSqlConfiguration.Global;

			StringBuilder opts = new StringBuilder();

			if (local.ConnectTimeout > 0)
				opts.AppendFormat("Connect Timeout={0}", local.ConnectTimeout);

			if (global.NTAuthentication)
			{
				if (opts.Length > 0) opts.Append(';');
				user = password = null;
				opts.Append("Integrated Security=true");
			}

			string connection_string = PhpSqlDbConnection.BuildConnectionString(server, user, password, opts.ToString());

			bool success;
			PhpSqlDbConnection connection = (PhpSqlDbConnection)manager.OpenConnection(connection_string,
			  newLink, global.MaxConnections, out success);

			if (!success)
			{
				if (connection != null)
				{
					UpdateConnectErrorInfo(connection);
					connection = null;
				}
				return null;
			}

			return connection;
		}

		#endregion

		#region mssql_free_result

		/// <summary>
		/// Releases a resource represening a query result.
		/// </summary>
		/// <param name="resultHandle">Query result resource.</param>
		/// <returns><B>true</B> on success, <B>false</B> on failure (invalid resource).</returns>
		[ImplementsFunction("mssql_free_result")]
		public static bool FreeResult(PhpResource resultHandle)
		{
			PhpSqlDbResult result = PhpSqlDbResult.ValidResult(resultHandle);
			if (result == null) return false;

			result.Close();
			return true;
		}

		#endregion

		#region mssql_select_db

		/// <summary>
		/// Selects the current DB for the last created connection.
		/// </summary>
		/// <param name="databaseName">Name of the database.</param>
		/// <returns><B>true</B> on success, <B>false</B> on failure.</returns>
		[ImplementsFunction("mssql_select_db")]
		public static bool SelectDb(string databaseName)
		{
			PhpDbConnection last_connection = manager.GetLastConnection();
			if (last_connection == null)
			{
				last_connection = (PhpDbConnection)Connect();
				if (last_connection == null) return false;
			}

			return SelectDb(databaseName, last_connection);
		}

		/// <summary>
		/// Selects the current DB for a specified connection.
		/// </summary>
		/// <param name="databaseName">Name of the database.</param>
		/// <param name="linkIdentifier">Connection resource.</param>
		/// <returns><B>true</B> on success, <B>false</B> on failure.</returns>
		[ImplementsFunction("mssql_select_db")]
		public static bool SelectDb(string databaseName, PhpResource linkIdentifier)
		{
			PhpSqlDbConnection connection = PhpSqlDbConnection.ValidConnection(linkIdentifier);
			if (connection == null) return false;

			return connection.SelectDb(databaseName);
		}

		#endregion

		#region mssql_query

		/// <summary>
		/// Sends a query to the current database associated with the last created connection.
		/// </summary>
		/// <param name="query">Query.</param>
		/// <returns>Query resource or a <B>null</B> reference (<B>null</B> in PHP) on failure.</returns>
		[ImplementsFunction("mssql_query")]
		[return: CastToFalse]
		public static PhpResource Query(string query)
		{
			PhpDbConnection last_connection = manager.GetLastConnection();

			if (last_connection == null)
				last_connection = (PhpDbConnection)Connect();

			return Query(query, last_connection);
		}

		/// <summary>
		/// Sends a query to the current database associated with a specified connection.
		/// </summary>
		/// <param name="query">Query.</param>
		/// <param name="linkIdentifier">Connection resource.</param>
		/// <returns>Query resource or a <B>null</B> reference (<B>null</B> in PHP) on failure.</returns>
		[ImplementsFunction("mssql_query")]
		[return: CastToFalse]
		public static PhpResource Query(string query, PhpResource linkIdentifier)
		{
			MsSqlLocalConfig local = MsSqlConfiguration.Local;
			return Query(query, linkIdentifier, local.BatchSize);
		}

		/// <summary>
		/// Sends a query to the current database associated with a specified connection.
		/// </summary>
		/// <param name="query">Query.</param>
		/// <param name="linkIdentifier">Connection resource.</param>
		/// <param name="batchSize">Connection resource.</param>
		/// <returns>Query resource or a <B>null</B> reference (<B>null</B> in PHP) on failure.</returns>
		[ImplementsFunction("mssql_query")]
		[return: CastToFalse]
		public static PhpResource Query(string query, PhpResource linkIdentifier, int batchSize)
		{
			PhpSqlDbConnection connection = PhpSqlDbConnection.ValidConnection(linkIdentifier);
			if (query == null || connection == null) return null;

			PhpSqlDbResult result = (PhpSqlDbResult)connection.ExecuteQuery(query.Trim(), true);
			if (result == null) return null;

			result.BatchSize = batchSize;
			return result;
		}

		#endregion

		#region mssql_fetch_row, mssql_fetch_assoc, mssql_fetch_array, mssql_fetch_object

		/// <summary>
		/// Get a result row as an integer indexed array. 
		/// </summary>
		/// <param name="resultHandle">Query result resource.</param>
		/// <returns>Array indexed by integers starting from 0 containing values of the current row.</returns>
		[ImplementsFunction("mssql_fetch_row")]
		[return: CastToFalse]
		public static PhpArray FetchRow(PhpResource resultHandle)
		{
			return FetchArray(resultHandle, QueryResultKeys.Numbers);
		}

		/// <summary>
		/// Get a result row as an associative array. 
		/// </summary>
		/// <param name="resultHandle">Query result resource.</param>
		/// <returns>Array indexed by column names containing values of the current row.</returns>			
		[ImplementsFunction("mssql_fetch_assoc")]
		public static PhpArray FetchAssoc(PhpResource resultHandle)
		{
			return FetchArray(resultHandle, QueryResultKeys.ColumnNames);
		}

		/// <summary>
		/// Get a result row as an associative array combined with integer-indexed array. 
		/// </summary>
		/// <param name="resultHandle">Query result resource.</param>
		/// <returns>
		/// Array indexed by both column names and indices containing values of the current row.
		/// Each value is contained twice - once with column name key and once with column index.
		/// </returns>
		[ImplementsFunction("mssql_fetch_array")]
		[return: CastToFalse]
		public static PhpArray FetchArray(PhpResource resultHandle)
		{
			return FetchArray(resultHandle, QueryResultKeys.Both);
		}

		/// <summary>
		/// Get a result row as an array with a specified key format. 
		/// </summary>
		/// <param name="resultHandle">Query result resource.</param>
		/// <param name="resultType">Type(s) of keys in the resulting array.</param>
		/// <returns>
		/// Array containing values of the rows indexed by column keys and/or column indices depending 
		/// on value of <paramref name="resultType"/>.
		/// </returns>
		[ImplementsFunction("mssql_fetch_array")]
		[return: CastToFalse]
		public static PhpArray FetchArray(PhpResource resultHandle, QueryResultKeys resultType)
		{
			PhpSqlDbResult result = PhpSqlDbResult.ValidResult(resultHandle);
			if (result == null) return null;

			switch (resultType)
			{
				case QueryResultKeys.ColumnNames: return result.FetchArray(false, true);
				case QueryResultKeys.Numbers: return result.FetchArray(true, false);
				case QueryResultKeys.Both: return result.FetchArray(true, true);
			}

			return null;
		}

		/// <summary>
		/// Get a result row as an object. 
		/// </summary>
		/// <param name="resultHandle">Query result resource.</param>
		/// <returns>
		/// Object whose fields contain values from the current row. 
		/// Field names corresponds to the column names.
		/// </returns>
		[ImplementsFunction("mssql_fetch_object")]
		[return: CastToFalse]
		public static PhpObject FetchObject(PhpResource resultHandle)
		{
			PhpSqlDbResult result = PhpSqlDbResult.ValidResult(resultHandle);
			if (result == null) return null;

			return result.FetchObject();
		}

		#endregion

		#region mssql_rows_affected

		/// <summary>
		/// Get a number of affected rows in the previous operation.
		/// </summary>
		/// <returns>The number of affected rows or -1 if the last operation failed or the connection is invalid.</returns>
		[ImplementsFunction("mssql_rows_affected")]
		public static int GetLastAffectedRows()
		{
			PhpDbConnection last_connection = manager.GetLastConnection();

			if (last_connection == null)
				last_connection = (PhpDbConnection)Connect();

			return GetLastAffectedRows(last_connection);
		}

		/// <summary>
		/// Get a number of affected rows in the previous operation.
		/// </summary>
		/// <param name="linkIdentifier">Connection resource.</param>
		/// <returns>The number of affected rows or -1 if the last operation failed or the connection is invalid.</returns>
		[ImplementsFunction("mssql_rows_affected")]
		public static int GetLastAffectedRows(PhpResource linkIdentifier)
		{
			PhpSqlDbConnection connection = PhpSqlDbConnection.ValidConnection(linkIdentifier);
			if (connection == null) return -1;

			return connection.LastAffectedRows;
		}

		#endregion

		#region mssql_num_fields, mssql_num_rows

		/// <summary>
		/// Get number of columns (fields) in a specified result.
		/// </summary>
		/// <param name="resultHandle">Query result resource.</param>
		/// <returns>Number of columns in the specified result or 0 if the result resource is invalid.</returns>
		[ImplementsFunction("mssql_num_fields")]
		public static int GetFieldCount(PhpResource resultHandle)
		{
			PhpSqlDbResult result = PhpSqlDbResult.ValidResult(resultHandle);
			if (result == null) return 0;

			return result.FieldCount;
		}

		/// <summary>
		/// Get number of rows in a specified result.
		/// </summary>
		/// <param name="resultHandle">Query result resource.</param>
		/// <returns>Number of rows in the specified result or 0 if the result resource is invalid.</returns>
		[ImplementsFunction("mssql_num_rows")]
		public static int GetRowCount(PhpResource resultHandle)
		{
			PhpSqlDbResult result = PhpSqlDbResult.ValidResult(resultHandle);
			if (result == null) return 0;

			return result.RowCount;
		}

		#endregion

		#region mssql_get_last_message, NS: mssql_min_error_severity, mssql_min_message_severity

		/// <summary>
		/// Gets last error message.
		/// </summary>
		/// <returns>The message sent by server.</returns>
		[ImplementsFunction("mssql_get_last_message")]
		public static string GetLastMessage()
		{
			PhpSqlDbConnection last_connection = (PhpSqlDbConnection)manager.GetLastConnection();

			if (last_connection == null)
				return failConnectErrorMessage;

			return last_connection.GetLastErrorMessage();
		}

		/// <summary>
		/// Sets a threshold for displaying errors sent by server. Not supported.
		/// </summary>
		/// <param name="severity">Severity threshold.</param>
		[ImplementsFunction("mssql_min_error_severity")]
		public static void MinErrorSeverity(int severity)
		{
			PhpException.FunctionNotSupported(PhpError.Notice);
		}

		/// <summary>
		/// Sets a threshold for displaying messages sent by server. Not supported.
		/// </summary>
		/// <param name="severity">Severity threshold.</param>
		[ImplementsFunction("mssql_min_message_severity")]
		public static void MinMessageSeverity(int severity)
		{
			PhpException.FunctionNotSupported(PhpError.Notice);
		}

		#endregion

		#region mssql_result

		/// <summary>
		/// Gets a contents of a specified cell from a specified query result resource.
		/// </summary>
		/// <param name="resultHandle">Query result resource.</param>
		/// <param name="rowIndex">Row index.</param>
		/// <param name="field">Column (field) integer index or string name.</param>
		/// <returns>The value of the cell or a <B>null</B> reference (<B>false</B> in PHP) on failure (invalid resource or row index).</returns>
		[ImplementsFunction("mssql_result")]
		[return: CastToFalse]
		public static object GetField(PhpResource resultHandle, int rowIndex, object field)
		{
			PhpSqlDbResult result = PhpSqlDbResult.ValidResult(resultHandle);
			ScriptContext context = ScriptContext.CurrentContext;
			if (result == null) return null;

			string field_name;
			object field_value;
			if (field == null)
			{
				field_value = result.GetFieldValue(rowIndex, result.CurrentFieldIndex);
			}
			else if ((field_name = PhpVariable.AsString(field)) != null)
			{
				field_value = result.GetFieldValue(rowIndex, field_name);
			}
			else
			{
				field_value = result.GetFieldValue(rowIndex, Core.Convert.ObjectToInteger(field));
			}

			return Core.Convert.Quote(field_value, context);
		}

		#endregion

		#region mssql_next_result

		/// <summary>
		/// Fetches the next result set if the query returned multiple result sets.
		/// </summary>
		/// <param name="resultHandle">Query result resource.</param>
		/// <returns>Whether the next result set is available.</returns>
		[ImplementsFunction("mssql_next_result")]
		public static bool NextResult(PhpResource resultHandle)
		{
			PhpSqlDbResult result = PhpSqlDbResult.ValidResult(resultHandle);
			if (result == null) return false;

			return result.NextResultSet();
		}

		#endregion

		#region mssql_field_name, mssql_field_type, mssql_field_length

		/// <summary>
		/// Gets a name of the current column (field) in a result. 
		/// </summary>
		/// <param name="resultHandle">Query result resource.</param>
		/// <returns>Name of the column or a <B>null</B> reference on failure (invalid resource or column index).</returns>
		[ImplementsFunction("mssql_field_name")]
		public static string FieldName(PhpResource resultHandle)
		{
			PhpSqlDbResult result = PhpSqlDbResult.ValidResult(resultHandle);
			if (result == null) return null;

			return result.GetFieldName();
		}

		/// <summary>
		/// Gets a name of a specified column (field) in a result. 
		/// </summary>
		/// <param name="resultHandle">Query result resource.</param>
		/// <param name="fieldIndex">Column (field) index.</param>
		/// <returns>Name of the column or a <B>null</B> reference on failure (invalid resource or column index).</returns>
		[ImplementsFunction("mssql_field_name")]
		public static string FieldName(PhpResource resultHandle, int fieldIndex)
		{
			PhpSqlDbResult result = PhpSqlDbResult.ValidResult(resultHandle);
			if (result == null) return null;

			return result.GetFieldName(fieldIndex);
		}

		/// <summary>
		/// Gets a type of the current column (field) in a result. 
		/// </summary>
		/// <param name="resultHandle">Query result resource.</param>
		/// <returns>MSSQL type translated to PHP terminology.</returns>
		/// <remarks>
		/// Possible values are: TODO.
		/// </remarks>   
		[ImplementsFunction("mssql_field_type")]
		public static string FieldType(PhpResource resultHandle)
		{
			PhpSqlDbResult result = PhpSqlDbResult.ValidResult(resultHandle);
			if (result == null) return null;

			return result.GetPhpFieldType();
		}

		/// <summary>
		/// Gets a type of a specified column (field) in a result. 
		/// </summary>
		/// <param name="resultHandle">Query result resource.</param>
		/// <param name="fieldIndex">Column index.</param>
		/// <returns>MSSQL type translated to PHP terminology.</returns>
		/// <remarks>
		/// Possible values are: TODO.
		/// </remarks>   
		[ImplementsFunction("mssql_field_type")]
		public static string FieldType(PhpResource resultHandle, int fieldIndex)
		{
			PhpSqlDbResult result = PhpSqlDbResult.ValidResult(resultHandle);
			if (result == null) return null;

			return result.GetPhpFieldType(fieldIndex);
		}

		/// <summary>
		/// Gets a length of the current column (field) in a result. 
		/// </summary>
		/// <param name="resultHandle">Query result resource.</param>
		/// <returns>Length of the column or a -1 on failure (invalid resource or column index).</returns>
		[ImplementsFunction("mssql_field_length")]
		public static int FieldLength(PhpResource resultHandle)
		{
			PhpSqlDbResult result = PhpSqlDbResult.ValidResult(resultHandle);
			if (result == null) return -1;

			return result.GetFieldLength();
		}

		/// <summary>
		/// Gets a length of a specified column (field) in a result. 
		/// </summary>
		/// <param name="resultHandle">Query result resource.</param>
		/// <param name="fieldIndex">Column index.</param>
		/// <returns>Length of the column or a -1 on failure (invalid resource or column index).</returns>
		[ImplementsFunction("mssql_field_length")]
		public static int FieldLength(PhpResource resultHandle, int fieldIndex)
		{
			PhpSqlDbResult result = PhpSqlDbResult.ValidResult(resultHandle);
			if (result == null) return -1;

			return result.GetFieldLength(fieldIndex);
		}

		#endregion

		#region mssql_field_seek, mssql_dataseek

		/// <summary>
		/// Sets the result resource's current column (field) offset.
		/// </summary>
		/// <param name="resultHandle">Query result resource.</param>
		/// <param name="fieldOffset">New column offset.</param>
		/// <returns><B>true</B> on success, <B>false</B> on failure (invalid resource or column offset).</returns>
		[ImplementsFunction("mssql_field_seek")]
		public static bool FieldSeek(PhpResource resultHandle, int fieldOffset)
		{
			PhpSqlDbResult result = PhpSqlDbResult.ValidResult(resultHandle);
			if (result == null) return false;

			return result.SeekField(fieldOffset);
		}

		/// <summary>
		/// Sets the result resource's current row index.
		/// </summary>
		/// <param name="resultHandle">Query result resource.</param>
		/// <param name="rowIndex">New row index.</param>
		/// <returns><B>true</B> on success, <B>false</B> on failure (invalid resource or row index).</returns>
		[ImplementsFunction("mssql_data_seek")]
		public static bool DataSeek(PhpResource resultHandle, int rowIndex)
		{
			PhpSqlDbResult result = PhpSqlDbResult.ValidResult(resultHandle);
			if (result == null) return false;

			return result.SeekRow(rowIndex);
		}

		#endregion

		#region mssql_fetch_field

		/// <summary>
		/// Gets a PHP object whose properties describes the last fetched field.
		/// </summary>
		/// <param name="resultHandle">Query result resource.</param>
		/// <returns>The PHP object.</returns>
		[ImplementsFunction("mssql_fetch_field")]
		public static PhpObject FetchField(PhpResource resultHandle)
		{
			PhpSqlDbResult result = PhpSqlDbResult.ValidResult(resultHandle);
			if (result == null) return null;

			return FetchFieldInternal(result, result.FetchNextField());
		}

		/// <summary>
		/// Gets a PHP object whose properties describes a specified field.
		/// </summary>
		/// <param name="resultHandle">Query result resource.</param>
		/// <param name="fieldIndex">Field index.</param>
		/// <returns>The PHP object.</returns>
		[ImplementsFunction("mssql_fetch_field")]
		public static PhpObject FetchField(PhpResource resultHandle, int fieldIndex)
		{
			PhpSqlDbResult result = PhpSqlDbResult.ValidResult(resultHandle);
			if (result == null) return null;

			return FetchFieldInternal(result, fieldIndex);
		}

		private static PhpObject FetchFieldInternal(PhpSqlDbResult/*!*/ result, int fieldIndex)
		{
			DataRow info = result.GetSchemaRowInfo(fieldIndex);
			if (info == null) return null;

			string s;
			PhpObject obj = new stdClass();
			string php_type = result.GetPhpFieldType(fieldIndex);

			obj.Add("name", result.GetFieldName(fieldIndex));
			obj.Add("column_source", (s = info["BaseColumnName"] as string) != null ? s : "");
			obj.Add("max_length", result.GetFieldLength(fieldIndex));
			obj.Add("numeric", result.IsNumericType(php_type) ? 1 : 0);
			obj.Add("type", php_type);

			return obj;
		}

		#endregion

		#region NS: mssql_fetch_batch

		/// <summary>
		/// Not supported.
		/// </summary>
        [ImplementsFunction("mssql_fetch_batch", FunctionImplOptions.NotSupported)]
		public static PhpArray FetchBatch(PhpResource resultHandle)
		{
			PhpException.FunctionNotSupported();
			return null;
		}

		#endregion

		// Stored Procedures //

		#region mssql_init, mssql_free_statement

		/// <summary>
		/// Inicializes a stored procedure of a given name.
		/// </summary>
		/// <param name="procedureName">Name of the stored procedure.</param>
		/// <returns>Statement resource representing the procedure.</returns>
		[ImplementsFunction("mssql_init")]
		[return: CastToFalse]
		public static PhpResource CreateProcedure(string procedureName)
		{
			PhpDbConnection last_connection = manager.GetLastConnection();

			if (last_connection == null)
				last_connection = (PhpDbConnection)Connect();

			return CreateProcedure(procedureName, last_connection);
		}

		/// <summary>
		/// Inicializes a stored procedure of a given name.
		/// </summary>
		/// <param name="procedureName">Name of the stored procedure.</param>
		/// <param name="linkIdentifier">Connection resource.</param>
		/// <returns>Statement resource representing the procedure.</returns>
		[ImplementsFunction("mssql_init")]
		[return: CastToFalse]
		public static PhpResource CreateProcedure(string procedureName, PhpResource linkIdentifier)
		{
			PhpSqlDbConnection connection = PhpSqlDbConnection.ValidConnection(linkIdentifier);
			if (connection == null) return null;

			if (procedureName == null)
			{
				PhpException.ArgumentNull("procedureName");
				return null;
			}

			return new PhpSqlDbProcedure(connection, procedureName);
		}

		/// <summary>
		/// Releases a resource represening a statement.
		/// </summary>
		/// <param name="statement">Statement resource.</param>
		/// <returns><B>true</B> on success, <B>false</B> on failure (invalid resource).</returns>
		[ImplementsFunction("mssql_free_statement")]
		public static bool FreeStatement(PhpResource statement)
		{
			PhpSqlDbProcedure procedure = PhpSqlDbProcedure.ValidProcedure(statement);
			if (procedure == null) return false;

			procedure.Close();
			return true;
		}

		#endregion

		#region mssql_bind

		/// <summary>
		/// Binds a PHP variable to an SQL input parameter of a statement.
		/// </summary>
		/// <param name="statement">Statement resource.</param>
		/// <param name="parameterName">Parameter name starting with '@' character.</param>
		/// <param name="variable">PHP variable to bind to the parameter.</param>
		/// <param name="type">SQL type of the parameter.</param>
		/// <returns>Whether binding succeeded.</returns>
		[ImplementsFunction("mssql_bind")]
		public static bool Bind(PhpResource statement, string parameterName, PhpReference variable, VariableType type)
		{
			return Bind(statement, parameterName, variable, type, false, false, -1);
		}

		/// <summary>
		/// Binds a PHP variable to an SQL parameter of a statement.
		/// </summary>
		/// <param name="statement">Statement resource.</param>
		/// <param name="parameterName">Parameter name starting with '@' character.</param>
		/// <param name="variable">PHP variable to bind to the parameter.</param>
		/// <param name="type">SQL type of the parameter.</param>
		/// <param name="isOutput">Whether the parameter is an output parameter.</param>
		/// <returns>Whether binding succeeded.</returns>
		[ImplementsFunction("mssql_bind")]
		public static bool Bind(PhpResource statement, string parameterName, PhpReference variable, VariableType type,
		  bool isOutput)
		{
			return Bind(statement, parameterName, variable, type, isOutput, false, -1);
		}

		/// <summary>
		/// Binds a PHP variable to an SQL parameter of a statement.
		/// </summary>
		/// <param name="statement">Statement resource.</param>
		/// <param name="parameterName">Parameter name starting with '@' character.</param>
		/// <param name="variable">PHP variable to bind to the parameter.</param>
		/// <param name="type">SQL type of the parameter.</param>
		/// <param name="isOutput">Whether the parameter is an output parameter.</param>
		/// <param name="isNullable">Whether the parameter accepts <B>null</B> values.</param>
		/// <returns>Whether binding succeeded.</returns>
		[ImplementsFunction("mssql_bind")]
		public static bool Bind(PhpResource statement, string parameterName, PhpReference variable, VariableType type,
		  bool isOutput, bool isNullable)
		{
			return Bind(statement, parameterName, variable, type, isOutput, isNullable, -1);
		}

		/// <summary>
		/// Binds a PHP variable to an SQL parameter of a statement.
		/// </summary>
		/// <param name="statement">Statement resource.</param>
		/// <param name="parameterName">Parameter name starting with '@' character.</param>
		/// <param name="variable">PHP variable to bind to the parameter.</param>
		/// <param name="type">SQL type of the parameter.</param>
		/// <param name="isOutput">Whether the parameter is an output parameter.</param>
		/// <param name="isNullable">Whether the parameter accepts <B>null</B> values.</param>
		/// <param name="maxLength">Maximum size of input data.</param>
		/// <returns>Whether binding succeeded.</returns>
		[ImplementsFunction("mssql_bind")]
		public static bool Bind(PhpResource statement, string parameterName, PhpReference variable, VariableType type,
		  bool isOutput, bool isNullable, int maxLength)
		{
			PhpSqlDbProcedure procedure = PhpSqlDbProcedure.ValidProcedure(statement);
			if (procedure == null) return false;

			if (parameterName == null)
			{
				PhpException.ArgumentNull("parameterName");
				return false;
			}

			PhpSqlDbProcedure.ParameterType param_type = PhpSqlDbProcedure.VariableTypeToParamType(type);
			if (param_type == PhpSqlDbProcedure.ParameterType.Invalid)
			{
				PhpException.ArgumentValueNotSupported("type", (int)type);
				return false;
			}

			SqlParameter parameter = new SqlParameter();
			parameter.ParameterName = parameterName;

			// it is necessary to set size for in-out params as the results are truncated to this size;
			// 8000 is maximal size of the data according to the doc:
			if (maxLength >= 0)
				parameter.Size = maxLength;
			else
				parameter.Size = 8000;

			if (String.Compare(parameterName, "RETVAL", true) == 0)
				parameter.Direction = ParameterDirection.ReturnValue;
			else if (isOutput)
				parameter.Direction = ParameterDirection.InputOutput;
			else
				parameter.Direction = ParameterDirection.Input;

			if (!procedure.AddBinding(parameter, variable, param_type))
			{
				PhpException.Throw(PhpError.Notice, LibResources.GetString("parameter_already_bound", parameterName));
				return false;
			}

			return true;
		}

		#endregion

		#region mssql_execute

		/// <summary>
		/// Executes a specified stored procedure statement.
		/// </summary>
		/// <param name="statement">Statement resource (stored procedure).</param>
		/// <returns>
		/// Result resource containing procedure output, 
		/// <B>true</B> if the procedure succeeded yet doesn't return any value, or
		/// <B>false</B> on failure.
		/// </returns>
		[ImplementsFunction("mssql_execute")]
		public static object Execute(PhpResource statement)
		{
			return Execute(statement, false);
		}

		/// <summary>
		/// Executes a specified stored procedure statement.
		/// </summary>
		/// <param name="statement">Statement resource (stored procedure).</param>
		/// <param name="skipResults">Whether to retrieve and return procedure output.</param>
		/// <returns>
		/// Result resource containing procedure output, 
		/// <B>true</B> if the procedure succeeded yet doesn't return any value, or
		/// <B>false</B> on failure.
		/// </returns>
		[ImplementsFunction("mssql_execute")]
		public static object Execute(PhpResource statement, bool skipResults)
		{
			PhpSqlDbProcedure procedure = PhpSqlDbProcedure.ValidProcedure(statement);
			if (procedure == null) return false;

			bool success;
			PhpSqlDbResult result = procedure.Execute(skipResults, out success);

			if (!success) return false;
			if (skipResults) return true;
			return result;
		}

		#endregion

		// Others //

		#region mssql_guid_string

		/// <summary>
		/// Converts 16 bytes to a string representation of a GUID.
		/// </summary>
		/// <param name="binary">Binary representation of a GUID.</param>
		/// <returns>String representation of a GUID.</returns>
		[ImplementsFunction("mssql_guid_string")]
		public static string GuidToString(PhpBytes binary)
		{
			return GuidToString(binary, false);
		}

		/// <summary>
		/// Converts 16 bytes to a string representation of a GUID.
		/// </summary>
		/// <param name="binary">Binary representation of a GUID.</param>
		/// <param name="shortFormat">Whether to return a short format.</param>
		/// <returns>String representation of a GUID.</returns>
		[ImplementsFunction("mssql_guid_string")]
		public static string GuidToString(PhpBytes binary, bool shortFormat)
		{
			if (binary == null || binary.Length == 0)
				return String.Empty;

			if (binary.Length != 16)
			{
				PhpException.InvalidArgument("binary", LibResources.GetString("arg:invalid_length"));
				return null;
			}

			if (shortFormat)
                return new Guid(binary.ReadonlyData).ToString("D").ToUpper();
			else
                return PHP.Core.StringUtils.BinToHex(binary.ReadonlyData, null).ToUpper();
		}

		#endregion
	}
}