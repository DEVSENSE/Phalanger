/*

 Copyright (c) 2005-2006 Tomas Matousek.  

 This software is distributed under GNU General Public License version 2.
 The use and distribution terms for this software are contained in the file named LICENSE, 
 which can be found in the same directory as this file. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Data;
using System.Collections;
using System.Text;
using MySql.Data.MySqlClient;

using PHP.Core;

namespace PHP.Library.Data
{
  /// <summary>
  /// Implements PHP functions provided by MySQL extension.
  /// </summary>
  public static class MySql
  {	
    private const string EquivalentNativeLibraryVersion = "5.1.3";
    private const string DefaultProtocolVersion = "10";
    private const string DefaultClientCharset = "latin1";
    
    #region Enums
	  
    /// <summary>
    /// Connection flags.
    /// </summary>
    [Flags]
    public enum ConnectFlags
    {
      /// <summary>
      /// No flags.
      /// </summary>
      None = 0,
      
      /// <summary>
      ///  Use compression protocol.
      /// </summary>
      [ImplementsConstant("MYSQL_CLIENT_COMPRESS")] Compress = 32,

      /// <summary>
      /// Allow space after function names.
      /// Not supported (ignored).
      /// </summary>
      [ImplementsConstant("MYSQL_CLIENT_IGNORE_SPACE")] IgnoreSpace = 256, 
      
      /// <summary>
      /// Allow interactive_timeout seconds (instead of wait_timeout) of inactivity before closing the connection.
      /// Not supported (ignored).
      /// </summary>
      [ImplementsConstant("MYSQL_CLIENT_INTERACTIVE")] Interactive = 1024,
      
      /// <summary>
      /// Use SSL encryption.
      /// </summary>
      [ImplementsConstant("MYSQL_CLIENT_SSL")] SSL = 2048
    }

    /// <summary>
    /// Query result array format.
    /// </summary>
    [Flags]
    public enum QueryResultKeys
    {
      /// <summary>
      /// Add items keyed by column names.
      /// </summary>
      [ImplementsConstant("MYSQL_ASSOC")] ColumnNames = 1,
  		
      /// <summary>
      /// Add items keyed by column indices.
      /// </summary>
      [ImplementsConstant("MYSQL_NUM")] Numbers = 2,
  		
  		/// <summary>
      /// Add both items keyed by column names and items keyd by column indices.
      /// </summary>
      [ImplementsConstant("MYSQL_BOTH")] Both = Numbers | ColumnNames
    }	
	  
    #endregion
		
    #region Thread Static Variables
    
    private static MySqlConnectionManager manager
    {
      get
      {
        if (_manager==null) _manager = new MySqlConnectionManager();
        return _manager;
      }
    }
    [ThreadStatic]
    private static MySqlConnectionManager _manager;
		
    [ThreadStatic]
    private static string failConnectErrorMessage = "";
		
    [ThreadStatic]
    private static int failConnectErrorNumber = 0;

    /// <summary>
    /// Clears thread static fields at the end of each request.
    /// </summary>
    private static void Clear()
    {
      _manager = null;
      failConnectErrorMessage = "";
      failConnectErrorNumber = 0;
    }

    static MySql()
    {
      RequestContext.RequestEnd += new Action(Clear);
    }
		
    private static void UpdateConnectErrorInfo(PhpMyDbConnection connection)
    {
      failConnectErrorMessage = connection.GetLastErrorMessage();
      failConnectErrorNumber = connection.GetLastErrorNumber();
    }

    #endregion		
		
    #region mysql_close
										  
    /// <summary>
    /// Close last connection.
    /// </summary>
    /// <returns><B>true</B> on success, <B>false</B> on failure.</returns>
    [ImplementsFunction("mysql_close")]
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
    [ImplementsFunction("mysql_close")]
    public static bool Close(PhpResource linkIdentifier)
    {
      PhpMyDbConnection connection = PhpMyDbConnection.ValidConnection(linkIdentifier);
      if (connection == null) return false;

      manager.RemoveConnection(connection);

      connection.Close();
      return true;
    }
		
    #endregion

    #region mysql_connect, NS: mysql_pconnect
		
    /// <summary>
    /// Establishes a new connection to MySQL server using default server, credentials, and flags.
    /// </summary>
    /// <returns>
    /// Resource representing the connection or a <B>null</B> reference (<B>false</B> in PHP) on failure.
    /// </returns>
    /// <remarks>
    /// Default values are taken from the configuration.
    /// </remarks>
    [ImplementsFunction("mysql_connect")]
    [return: CastToFalse]
    public static PhpResource Connect()
    {
      return Connect(null, null, null, false, ConnectFlags.None, false);
    }

    /// <summary>
    /// Establishes a new connection to MySQL server using a specified server and default credentials and flags.
    /// </summary>
    /// <param name="server">Server (host). A <b>null</b> reference means the default value.</param>
    /// <returns>
    /// Resource representing the connection or a <B>null</B> reference (<B>false</B> in PHP) on failure.
    /// </returns>
    /// <remarks>
    /// Default values are taken from the configuration.
    /// </remarks>		
    [ImplementsFunction("mysql_connect")]
    [return: CastToFalse]
    public static PhpResource Connect(string server)
    {
      return Connect(server, null, null, false, ConnectFlags.None, false);
    }

    /// <summary>
    /// Establishes a new connection to MySQL server using a specified server and user and default password and flags.
    /// </summary>
    /// <param name="server">Server (host). A <b>null</b> reference means the default value.</param>
    /// <param name="user">User name. A <b>null</b> reference means the default value.</param>
    /// <returns>
    /// Resource representing the connection or a <B>null</B> reference (<B>false</B> in PHP) on failure.
    /// </returns>
    /// <remarks>
    /// Default values are taken from the configuration.
    /// </remarks>		
    [ImplementsFunction("mysql_connect")]
    [return: CastToFalse]
    public static PhpResource Connect(string server, string user)
    {
      return Connect(server, user, null, false, ConnectFlags.None, false);
    }

    /// <summary>
    /// Establishes a new connection to MySQL server using a specified server, user, and password and default flags.
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
    [ImplementsFunction("mysql_connect")]
    [return: CastToFalse]
    public static PhpResource Connect(string server, string user, string password)
    {
      return Connect(server, user, password, false, ConnectFlags.None, false);
    }

    /// <summary>
    /// Establishes a connection to MySQL server using a specified server, user, and password and default flags.
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
    [ImplementsFunction("mysql_connect")]
    [return: CastToFalse]
    public static PhpResource Connect(string server, string user, string password, bool newLink)
    {
      return Connect(server, user, password, newLink, ConnectFlags.None, false);
    }
		
    /// <summary>
    /// Establishes a connection to MySQL server using a specified server, user, password, and flags.
    /// </summary>
    /// <param name="server">Server (host). A <b>null</b> reference means the default value.</param>
    /// <param name="user">User name. A <b>null</b> reference means the default value.</param>
    /// <param name="password">Password. A <b>null</b> reference means the default value.</param>
    /// <param name="newLink">Whether to create a new link (ignored by Phalanger).</param>
    /// <param name="flags">Connection flags.</param>
    /// <returns>
    /// Resource representing the connection or a <B>null</B> reference (<B>false</B> in PHP) on failure.
    /// </returns>
    /// <remarks>
    /// Default values are taken from the configuration.
    /// </remarks>		
    [ImplementsFunction("mysql_connect")]
    [return: CastToFalse]
    public static PhpResource Connect(string server, string user, string password, bool newLink, ConnectFlags flags)
    {
      return Connect(server, user, password, newLink, flags, false);
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
    [ImplementsFunction("mysql_pconnect")]
    [return: CastToFalse]
    public static PhpResource PersistentConnect()
    {
      return Connect(null, null, null, false, ConnectFlags.None, true);
    }

    /// <summary>
    /// Establishes a new connection to MySQL server using a specified server and default credentials and flags.
    /// </summary>
    /// <param name="server">Server (host). A <b>null</b> reference means the default value.</param>
    /// <returns>
    /// Resource representing the connection or a <B>null</B> reference (<B>false</B> in PHP) on failure.
    /// </returns>
    /// <remarks>
    /// Default values are taken from the configuration. 
    /// Creates a non-persistent connection. Persistent connections are not supported.
    /// </remarks>				
    [ImplementsFunction("mysql_pconnect")]
    [return: CastToFalse]
    public static PhpResource PersistentConnect(string server)
    {
      return Connect(server, null, null, false, ConnectFlags.None, true);
    }

    /// <summary>
    /// Establishes a new connection to MySQL server using a specified server and user and default password and flags.
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
    [ImplementsFunction("mysql_pconnect")]
    [return: CastToFalse]
    public static PhpResource PersistentConnect(string server, string user)
    {
      return Connect(server, user, null, false, ConnectFlags.None, true);
    }
    
    /// <summary>
    /// Establishes a new connection to MySQL server using a specified server, user, and password and default flags.
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
    [ImplementsFunction("mysql_pconnect")]
    [return: CastToFalse]
    public static PhpResource PersistentConnect(string server, string user, string password)
    {
      return Connect(server, user, password, false, ConnectFlags.None, true);
    }

    /// <summary>
    /// Establishes a connection to MySQL server using a specified server, user, and password and default flags.
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
    [ImplementsFunction("mysql_pconnect")]
    [return: CastToFalse]
    public static PhpResource PersistentConnect(string server, string user, string password, bool newLink)
    {
      return Connect(server, user, password, newLink, ConnectFlags.None, true);
    }
		
    /// <summary>
    /// Establishes a connection to MySQL server using a specified server, user, password, and flags.
    /// </summary>
    /// <param name="server">Server (host). A <b>null</b> reference means the default value.</param>
    /// <param name="user">User name. A <b>null</b> reference means the default value.</param>
    /// <param name="password">Password. A <b>null</b> reference means the default value.</param>
    /// <param name="newLink">Whether to create a new link (ignored by Phalanger).</param>
    /// <param name="flags">Connection flags.</param>
    /// <returns>
    /// Resource representing the connection or a <B>null</B> reference (<B>false</B> in PHP) on failure.
    /// </returns>
    /// <remarks>
    /// Default values are taken from the configuration.
    /// Creates a non-persistent connection. Persistent connections are not supported.
    /// </remarks>		
    [ImplementsFunction("mysql_pconnect")]
    [return: CastToFalse]
    public static PhpResource PersistentConnect(string server, string user, string password, bool newLink,ConnectFlags flags)
    {
      return Connect(server, user, password, newLink, flags, true);
    }

    private static PhpResource Connect(string server, string user, string password, bool newLink, ConnectFlags flags, bool persistent)
    {
        //// MYSQL_CLIENT_IGNORE_SPACE is not supported, throw a warning:
        //if ((flags & ConnectFlags.IgnoreSpace) != 0) PhpException.ArgumentValueNotSupported("flags");
        // persistent connections are treated as transient, a warning is issued:
        if (persistent) PhpException.FunctionNotSupported(PhpError.Notice);

        MySqlLocalConfig local = MySqlConfiguration.Local;
        MySqlGlobalConfig global = MySqlConfiguration.Global;

        string pipe_name = null;
        int port = -1;

        if (server != null)
            ParseServerName(ref server, out port, out pipe_name);
        else
            server = local.Server;

        if (port == -1) port = local.Port;
        if (user == null) user = local.User;
        if (password == null) password = local.Password;

        // build the connection string to be used with MySQL Connector/.NET
        // see http://dev.mysql.com/doc/refman/5.5/en/connector-net-connection-options.html
        string connection_string = PhpMyDbConnection.BuildConnectionString(
          server, user, password,

          String.Format("allowzerodatetime=true;allow user variables=true;connect timeout={0};Port={1};SSL Mode={2};Use Compression={3}{4}{5}",
            (local.ConnectTimeout > 0) ? local.ConnectTimeout : Int32.MaxValue,
            port,
            (flags & ConnectFlags.SSL) != 0 ? "Preferred" : "None",     // (since Connector 6.2.1.) ssl mode={None|Preferred|Required|VerifyCA|VerifyFull}   // (Jakub) use ssl={true|false} has been deprecated
            (flags & ConnectFlags.Compress) != 0 ? "true" : "false",    // Use Compression={true|false}
            (pipe_name != null) ? ";Pipe=" + pipe_name : string.Empty,  // Pipe={...}
            (flags & ConnectFlags.Interactive) != 0 ? ";Interactive=true" : string.Empty    // Interactive={true|false}
            )
        );

        bool success;
        PhpMyDbConnection connection = (PhpMyDbConnection)manager.OpenConnection(connection_string,
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

        connection.SetServer(server);
        return connection;
    }
    
    private static void ParseServerName(ref string/*!*/ server, out int port, out string socketPath)
    {
      port = -1;
      socketPath = null;
      
      int i = server.IndexOf(':');
      if (i == -1) return;
      
      string port_or_socket = server.Substring(i + 1);
      server = server.Substring(0,i);
      
      // socket path:
      if (port_or_socket.Length > 0 && port_or_socket[0] == '/')
      {
        socketPath = port_or_socket;
      }
      else
      {
        try
        {
          port = UInt16.Parse(port_or_socket);
        }
        catch
        {
          PhpException.Throw(PhpError.Notice, LibResources.GetString("invalid_port", port_or_socket));
        }  
      }  
    }

    #endregion

    #region mysql_free_result

    /// <summary>
    /// Releases a resource represening a query result.
    /// </summary>
    /// <param name="resultHandle">Query result resource.</param>
    /// <returns><B>true</B> on success, <B>false</B> on failure (invalid resource).</returns>
    [ImplementsFunction("mysql_free_result")]
    public static bool FreeResult(PhpResource resultHandle)
    {
      PhpMyDbResult result = PhpMyDbResult.ValidResult(resultHandle);
      if (result == null) return false;

      result.Close();
      return true;
    }

    #endregion

    #region mysql_select_db
		
    /// <summary>
    /// Selects the current DB for the last created connection.
    /// </summary>
    /// <param name="databaseName">Name of the database.</param>
    /// <returns><B>true</B> on success, <B>false</B> on failure.</returns>
    [ImplementsFunction("mysql_select_db")]
    public static bool SelectDb(string databaseName)
    {
      PhpDbConnection last_connection = manager.GetLastConnection();
      if (last_connection == null)
      {
        last_connection = (PhpDbConnection) Connect();
        if (last_connection==null) return false;
      }

      return SelectDb(databaseName,last_connection);
    }

    /// <summary>
    /// Selects the current DB for a specified connection.
    /// </summary>
    /// <param name="databaseName">Name of the database.</param>
    /// <param name="linkIdentifier">Connection resource.</param>
    /// <returns><B>true</B> on success, <B>false</B> on failure.</returns>
    [ImplementsFunction("mysql_select_db")]
    public static bool SelectDb(string databaseName, PhpResource linkIdentifier)
    {
      PhpMyDbConnection connection = PhpMyDbConnection.ValidConnection(linkIdentifier);
      if (connection == null) return false;

      return connection.SelectDb(databaseName);
    }
		
    #endregion
		
    #region mysql_query
		
    /// <summary>
    /// Sends a query to the current database associated with the last created connection.
    /// </summary>
    /// <param name="query">Query.</param>
    /// <returns>Query resource or a <B>null</B> reference (<B>null</B> in PHP) on failure.</returns>
    [ImplementsFunction("mysql_query")]
    [return: CastToFalse]
    public static PhpResource Query(string query)
    {
      PhpDbConnection last_connection = manager.GetLastConnection();

      if (last_connection == null)
        last_connection = (PhpDbConnection) Connect();

      return Query(query, last_connection);
    }

    /// <summary>
    /// Sends a query to the current database associated with a specified connection.
    /// </summary>
    /// <param name="query">Query.</param>
    /// <param name="linkIdentifier">Connection resource.</param>
    /// <returns>Query resource or a <B>null</B> reference (<B>null</B> in PHP) on failure.</returns>
    [ImplementsFunction("mysql_query")]
    [return: CastToFalse]
    public static PhpResource Query(string query, PhpResource linkIdentifier)
    {
      PhpMyDbConnection connection = PhpMyDbConnection.ValidConnection(linkIdentifier);
      if (query == null || connection == null) return null;

      return connection.ExecuteQuery(query.Trim(),true);
    }

    #endregion

    #region mysql_insert_id, mysql_thread_id

    /// <summary>
    /// Gets id generated by the previous insert operation.
    /// </summary>
    /// <returns>Id or 0 if the last command wasn't an insertion.</returns>
    [ImplementsFunction("mysql_insert_id")]
    public static int GetLastInsertedId()
    {
      PhpDbConnection last_connection = manager.GetLastConnection();

      if (last_connection == null)
        last_connection = (PhpDbConnection) Connect();

      return GetLastInsertedId(last_connection);
    }

    /// <summary>
    /// Gets id generated by the previous insert operation.
    /// </summary>
    /// <param name="linkIdentifier">Connection resource.</param>
    /// <returns>Id or 0 if the last command wasn't an insertion.</returns>
    [ImplementsFunction("mysql_insert_id")]
    public static int GetLastInsertedId(PhpResource linkIdentifier)
    {
      PhpMyDbConnection connection = PhpMyDbConnection.ValidConnection(linkIdentifier);
      if (connection == null) return 0;

      PhpDbResult result = connection.ExecuteQuery("SELECT LAST_INSERT_ID()",false);
      if (result==null || result.RowCount<1 || result.FieldCount<1) return 0;
      
      try
      {
        long id = System.Convert.ToInt64(result.GetFieldValue(0,0));
        return (id <= Int32.MaxValue) ? (int)id : Int32.MaxValue;
      }
      catch(InvalidCastException)
      {
        return -1;
      }  
    }
    
    /// <summary>
    /// Gets the current DB thread's id.
    /// </summary>
    /// <returns>The thread id.</returns>
    [ImplementsFunction("mysql_thread_id")]
    public static int GetThreadId()
    {
      PhpDbConnection last_connection = manager.GetLastConnection();

      if (last_connection == null)
        last_connection = (PhpDbConnection) Connect();

      return GetThreadId(last_connection);
    }

    /// <summary>
    /// Gets the current DB thread's id.
    /// </summary>
    /// <param name="linkIdentifier">Connection resource.</param>
    /// <returns>The thread id.</returns>
    [ImplementsFunction("mysql_thread_id")]
    public static int GetThreadId(PhpResource linkIdentifier)
    {
      PhpMyDbConnection connection = PhpMyDbConnection.ValidConnection(linkIdentifier);
      if (connection == null) return 0;

      return connection.Connection.ServerThread;
    }
    
    #endregion
		
    #region mysql_fetch_row, mysql_fetch_assoc, mysql_fetch_array, mysql_fetch_object
		
    /// <summary>
    /// Get a result row as an integer indexed array. 
    /// </summary>
    /// <param name="resultHandle">Query result resource.</param>
    /// <returns>Array indexed by integers starting from 0 containing values of the current row.</returns>
    [ImplementsFunction("mysql_fetch_row")]
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
    [ImplementsFunction("mysql_fetch_assoc")]
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
    [ImplementsFunction("mysql_fetch_array")]
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
    [ImplementsFunction("mysql_fetch_array")]
    [return: CastToFalse]
    public static PhpArray FetchArray(PhpResource resultHandle, QueryResultKeys resultType)
    {
      PhpMyDbResult result = PhpMyDbResult.ValidResult(resultHandle);
      if (result == null) return null;

      switch (resultType)
      {
        case QueryResultKeys.ColumnNames: return result.FetchArray(false,true);
        case QueryResultKeys.Numbers: return result.FetchArray(true,false);
        case QueryResultKeys.Both: return result.FetchArray(true,true);
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
    [ImplementsFunction("mysql_fetch_object")]
    [return: CastToFalse]
    public static PhpObject FetchObject(PhpResource resultHandle)
    {
      PhpMyDbResult result = PhpMyDbResult.ValidResult(resultHandle);
      if (result == null) return null;

      return result.FetchObject();
    }
										  
    #endregion
		
    #region mysql_affected_rows
		
    /// <summary>
    /// Get a number of affected rows in the previous operation.
    /// </summary>
    /// <returns>The number of affected rows or -1 if the last operation failed or the connection is invalid.</returns>
    [ImplementsFunction("mysql_affected_rows")]
    public static int GetLastAffectedRows()
    {
      PhpDbConnection last_connection = manager.GetLastConnection();

      if (last_connection == null)
        last_connection = (PhpDbConnection) Connect();

      return GetLastAffectedRows(last_connection);
    }

    /// <summary>
    /// Get a number of affected rows in the previous operation.
    /// </summary>
    /// <param name="linkIdentifier">Connection resource.</param>
    /// <returns>The number of affected rows or -1 if the last operation failed or the connection is invalid.</returns>
    [ImplementsFunction("mysql_affected_rows")]
    public static int GetLastAffectedRows(PhpResource linkIdentifier)
    {
      PhpMyDbConnection connection = PhpMyDbConnection.ValidConnection(linkIdentifier);
      if (connection == null || connection.LastException != null) return -1;
      
      return connection.LastAffectedRows;
    }
		
    #endregion
		
    #region mysql_num_fields, mysql_num_rows

    /// <summary>
    /// Get number of columns (fields) in a specified result.
    /// </summary>
    /// <param name="resultHandle">Query result resource.</param>
    /// <returns>Number of columns in the specified result or 0 if the result resource is invalid.</returns>
    [ImplementsFunction("mysql_num_fields")]
    public static int GetFieldCount(PhpResource resultHandle)
    {
      PhpMyDbResult result = PhpMyDbResult.ValidResult(resultHandle);
      if (result == null) return 0;

      return result.FieldCount;
    }
		
    /// <summary>
    /// Get number of rows in a specified result.
    /// </summary>
    /// <param name="resultHandle">Query result resource.</param>
    /// <returns>Number of rows in the specified result or 0 if the result resource is invalid.</returns>
    [ImplementsFunction("mysql_num_rows")]
    public static int GetRowCount(PhpResource resultHandle)
    {			
      PhpMyDbResult result = PhpMyDbResult.ValidResult(resultHandle);
      if (result == null) return 0;

      return result.RowCount;
    }
		
    #endregion		

    #region mysql_error, mysql_errno

    /// <summary>
    /// Returns the text of the error message from previous operation.
    /// </summary>
    /// <returns>Error message or empty string if no error occured.</returns>
    [ImplementsFunction("mysql_error")]
    public static string LastErrorMessage()
    {
      PhpDbConnection last_connection = manager.GetLastConnection();

      if (last_connection == null)
        return failConnectErrorMessage;

      return LastErrorMessage(last_connection);
    }

    /// <summary>
    /// Returns the text of the error message from previous operation.
    /// </summary>
    /// <param name="linkIdentifier">Connection resource.</param>
    /// <returns>
    /// Error message, empty string if no error occured, or a <B>null</B> reference 
    /// if the connection resource is invalid.
    /// </returns>
    [ImplementsFunction("mysql_error")]
    public static string LastErrorMessage(PhpResource linkIdentifier)
    {
      PhpMyDbConnection connection = PhpMyDbConnection.ValidConnection(linkIdentifier);
      if (connection == null) return null;

      return connection.GetLastErrorMessage();
    }

    /// <summary>
    /// Returns the number of the error from previous operation.
    /// </summary>
    /// <returns>Error number, 0 if no error occured, or -1 if the number cannot be retrieved.</returns>
    [ImplementsFunction("mysql_errno")]
    public static int LastErrorNumber()
    {
      PhpDbConnection last_connection = manager.GetLastConnection();

      if (last_connection == null)
        return failConnectErrorNumber;

      return LastErrorNumber(last_connection);
    }

    /// <summary>
    /// Returns the number of the error from previous operation.
    /// </summary>
    /// <param name="linkIdentifier">Connection resource.</param>
    /// <returns>Error number, 0 if no error occured, or -1 if the number cannot be retrieved.</returns>
    [ImplementsFunction("mysql_errno")]
    public static int LastErrorNumber(PhpResource linkIdentifier)
    {
      PhpMyDbConnection connection = PhpMyDbConnection.ValidConnection(linkIdentifier);
      if (connection == null) return -1;

      return connection.GetLastErrorNumber();
    }
				
    #endregion
		
    #region mysql_result
		
    /// <summary>
    /// Gets a contents of a specified cell of the current column from a specified query result resource.
    /// </summary>
    /// <param name="resultHandle">Query result resource.</param>
    /// <param name="rowIndex">Row index.</param>
    /// <returns>The value of the cell or a <B>null</B> reference (<B>false</B> in PHP) on failure (invalid resource or row index).</returns>
    [ImplementsFunction("mysql_result")]
    [return: CastToFalse]
    public static object GetField(PhpResource resultHandle,int rowIndex)
    {
      return GetField(resultHandle, rowIndex, null);
    }
		
    /// <summary>
    /// Gets a contents of a specified cell from a specified query result resource.
    /// </summary>
    /// <param name="resultHandle">Query result resource.</param>
    /// <param name="rowIndex">Row index.</param>
    /// <param name="field">Column (field) integer index or string name.</param>
    /// <returns>The value of the cell or a <B>null</B> reference (<B>false</B> in PHP) on failure (invalid resource or row/field index/name).</returns>
    /// <remarks>
    /// Result is affected by run-time quoting 
    /// (<see cref="LocalConfiguration.VariablesSection.QuoteRuntimeVariables"/>).
		/// </remarks>
		[ImplementsFunction("mysql_result")]
    [return: CastToFalse]
    public static object GetField(PhpResource resultHandle,int rowIndex,object field)
    {
      PhpMyDbResult result = PhpMyDbResult.ValidResult(resultHandle);
      ScriptContext context = ScriptContext.CurrentContext;
      if (result == null) return null;
			
      string field_name;
      object field_value;
      if (field == null)
      {
        field_value = result.GetFieldValue(rowIndex,result.CurrentFieldIndex);
      }
      else if ((field_name = PhpVariable.AsString(field))!=null)  
      {
        field_value = result.GetFieldValue(rowIndex,field_name);
      }
      else
      {
        field_value = result.GetFieldValue(rowIndex,Core.Convert.ObjectToInteger(field));
      }
			
      return Core.Convert.Quote(field_value, context);
    }
				
    #endregion

    #region mysql_field_name, mysql_field_type, mysql_field_len

    /// <summary>
    /// Gets a name of a specified column (field) in a result. 
    /// </summary>
    /// <param name="resultHandle">Query result resource.</param>
    /// <param name="fieldIndex">Column (field) index.</param>
    /// <returns>Name of the column or a <B>null</B> reference on failure (invalid resource or column index).</returns>
    [ImplementsFunction("mysql_field_name")]
    public static string FieldName(PhpResource resultHandle, int fieldIndex)
    {
      PhpMyDbResult result = PhpMyDbResult.ValidResult(resultHandle);
      if (result == null) return null;
			
      return result.GetFieldName(fieldIndex);
    }

    /// <summary>
    /// Gets a type of a specified column (field) in a result. 
    /// </summary>
    /// <param name="resultHandle">Query result resource.</param>
    /// <param name="fieldIndex">Column index.</param>
    /// <returns>MySQL type translated to PHP terminology.</returns>
    /// <remarks>
    /// Possible values are: "string", "int", "real", "year", "date", "timestamp", "datetime", "time", 
    /// "set", "enum", "blob", "bit" (Phalanger specific), "NULL", and "unknown".
    /// </remarks>
    [ImplementsFunction("mysql_field_type")]
    public static string FieldType(PhpResource resultHandle,int fieldIndex)
    {
      PhpMyDbResult result = PhpMyDbResult.ValidResult(resultHandle);
      if (result == null) return null;
			
      return result.GetPhpFieldType(fieldIndex);
    }

    /// <summary>
    /// Gets a length of a specified column (field) in a result. 
    /// </summary>
    /// <param name="resultHandle">Query result resource.</param>
    /// <param name="fieldIndex">Column index.</param>
    /// <returns>Length of the column or a -1 on failure (invalid resource or column index).</returns>
    [ImplementsFunction("mysql_field_len")]
    public static int FieldLength(PhpResource resultHandle, int fieldIndex)
    {
      PhpMyDbResult result = PhpMyDbResult.ValidResult(resultHandle);
      if (result == null) return -1;
			
      return result.GetFieldLength(fieldIndex);
    }
		
    #endregion
		
    #region mysql_field_seek, mysql_dataseek

    /// <summary>
    /// Sets the result resource's current column (field) offset.
    /// </summary>
    /// <param name="resultHandle">Query result resource.</param>
    /// <param name="fieldOffset">New column offset.</param>
    /// <returns><B>true</B> on success, <B>false</B> on failure (invalid resource or column offset).</returns>
    [ImplementsFunction("mysql_field_seek")]
    public static bool FieldSeek(PhpResource resultHandle,int fieldOffset)
    {
      PhpMyDbResult result = PhpMyDbResult.ValidResult(resultHandle);
      if (result == null) return false;
			
      return result.SeekField(fieldOffset);
    }

    /// <summary>
    /// Sets the result resource's current row index.
    /// </summary>
    /// <param name="resultHandle">Query result resource.</param>
    /// <param name="rowIndex">New row index.</param>
    /// <returns><B>true</B> on success, <B>false</B> on failure (invalid resource or row index).</returns>
    [ImplementsFunction("mysql_data_seek")]
    public static bool DataSeek(PhpResource resultHandle,int rowIndex)
    {
      PhpMyDbResult result = PhpMyDbResult.ValidResult(resultHandle);
      if (result == null) return false;

      return result.SeekRow(rowIndex);
    }
		
    #endregion
		
		
		
    #region mysql_field_table, mysql_field_flags, mysql_fetch_field, mysql_fetch_lengths

    /// <summary>
    /// Gets a base table of a specified field.
    /// </summary>
    /// <param name="resultHandle">Query result resource.</param>
    /// <param name="fieldIndex">Field index.</param>
    /// <returns>Name of the base table of the field.</returns>
    [ImplementsFunction("mysql_field_table")]
    public static string GetFieldTable(PhpResource resultHandle, int fieldIndex)
    {
      PhpMyDbResult result = PhpMyDbResult.ValidResult(resultHandle);
      if (result == null) return null;
			
      DataRow info = result.GetSchemaRowInfo(fieldIndex);
      if (info == null) return null;
			
      return (string)info["BaseTableName"];
    }
		
    /// <summary>
    /// Gets flags of a specified field.
    /// </summary>
    /// <param name="resultHandle">Query result resource.</param>
    /// <param name="fieldIndex">Field index.</param>
    /// <returns>Flags of the field.</returns>
    [ImplementsFunction("mysql_field_flags")]
    public static string GetFieldFlags(PhpResource resultHandle, int fieldIndex)
    {
      PhpMyDbResult result = PhpMyDbResult.ValidResult(resultHandle);
      if (result == null) return null;
			
      ColumnFlags flags = result.GetFieldFlags(fieldIndex);
      string type_name = result.GetPhpFieldType(fieldIndex);
			
      StringBuilder str_fields = new StringBuilder();
			
      if ((flags & ColumnFlags.NOT_NULL) != 0) 
        str_fields.Append("not_null ");

      if ((flags & ColumnFlags.PRIMARY_KEY) != 0) 
        str_fields.Append("primary_key ");
      
      if ((flags & ColumnFlags.UNIQUE_KEY) != 0) 
        str_fields.Append("unique_key ");
      
      if ((flags & ColumnFlags.MULTIPLE_KEY) != 0) 
        str_fields.Append("multiple_key ");
      
      if ((flags & ColumnFlags.BLOB) != 0) 
        str_fields.Append("blob ");
      
      if ((flags & ColumnFlags.UNSIGNED) != 0) 
        str_fields.Append("unsigned ");

      if ((flags & ColumnFlags.ZERO_FILL) != 0) 
        str_fields.Append("zerofill ");
      
      if ((flags & ColumnFlags.BINARY) != 0) 
        str_fields.Append("binary ");
      
      if ((flags & ColumnFlags.ENUM) != 0) 
        str_fields.Append("enum ");
      
      if ((flags & ColumnFlags.SET) != 0) 
        str_fields.Append("set ");

      if ((flags & ColumnFlags.AUTO_INCREMENT) != 0) 
        str_fields.Append("auto_increment ");

      if ((flags & ColumnFlags.TIMESTAMP) != 0) 
        str_fields.Append("timestamp ");

      if (str_fields.Length > 0)
        str_fields.Length = str_fields.Length - 1;
			
      return str_fields.ToString();
    }

    /// <summary>
    /// Gets a PHP object whose properties describes the last fetched field.
    /// </summary>
    /// <param name="resultHandle">Query result resource.</param>
    /// <returns>The PHP object.</returns>
    [ImplementsFunction("mysql_fetch_field")]
    public static PhpObject FetchField(PhpResource resultHandle)
    {
      PhpMyDbResult result = PhpMyDbResult.ValidResult(resultHandle);
      if (result == null) return null;
			
      return FetchFieldInternal(result, result.FetchNextField());
    }
    
    /// <summary>
    /// Gets a PHP object whose properties describes a specified field.
    /// </summary>
    /// <param name="resultHandle">Query result resource.</param>
    /// <param name="fieldIndex">Field index.</param>
    /// <returns>The PHP object.</returns>
    [ImplementsFunction("mysql_fetch_field")]
    public static PhpObject FetchField(PhpResource resultHandle, int fieldIndex)
    {
      PhpMyDbResult result = PhpMyDbResult.ValidResult(resultHandle);
      if (result == null) return null;

      return FetchFieldInternal(result, fieldIndex);
    }
    
    private static PhpObject FetchFieldInternal(PhpMyDbResult/*!*/ result, int fieldIndex)
    {
      DataRow info = result.GetSchemaRowInfo(fieldIndex);
      if (info == null) return null;
			
      string s;
      PhpObject obj = new stdClass();
      //ColumnFlags flags = result.GetFieldFlags(fieldIndex);
      string php_type = result.GetPhpFieldType(fieldIndex);
      
      obj.Add("name",result.GetFieldName(fieldIndex));
      obj.Add("table",(s = info["BaseTableName"] as string) != null ? s : "");
      obj.Add("def",""); // TODO
      obj.Add("max_length",result.GetFieldLength(fieldIndex));

      obj.Add("not_null",/*(flags & ColumnFlags.NOT_NULL) != 0*/ (!(bool)info["AllowDBNull"]) ? 1 : 0);
      obj.Add("primary_key",/*(flags & ColumnFlags.PRIMARY_KEY) != 0*/ ((bool)info["IsKey"]) ? 1 : 0);
      obj.Add("multiple_key",/*(flags & ColumnFlags.MULTIPLE_KEY) != 0*/ ((bool)info["IsMultipleKey"]) ? 1 : 0);
      obj.Add("unique_key",/*(flags & ColumnFlags.UNIQUE_KEY) != 0*/ ((bool)info["IsUnique"]) ? 1 : 0);
      obj.Add("numeric",result.IsNumericType(php_type) ? 1 : 0);
      obj.Add("blob",/*(flags & ColumnFlags.BLOB) != 0*/ ((bool)info["IsBlob"]) ? 1 : 0);
      
      obj.Add("type",php_type);
      obj.Add("unsigned",/*(flags & ColumnFlags.UNSIGNED) != 0*/ ((bool)info["IsUnsigned"]) ? 1 : 0);
      obj.Add("zerofill",/*(flags & ColumnFlags.ZERO_FILL) != 0*/ ((bool)info["ZeroFill"]) ? 1 : 0);
      
      return obj;
    }
    
    /// <summary>
    /// Gets an array of lengths of the values of the current row.
    /// </summary>
    /// <param name="resultHandle">Query result resource.</param>
    /// <returns>An array containing a length of each value of the current row.</returns>
    [ImplementsFunction("mysql_fetch_lengths")]
    [return: CastToFalse]
    public static PhpArray FetchLengths(PhpResource resultHandle)
    {
      PhpMyDbResult result = PhpMyDbResult.ValidResult(resultHandle);
      if (result == null) return null;
			
      int row_index = result.CurrentRowIndex;
      if (row_index < 0) return null;
      
      PhpArray array = new PhpArray(result.FieldCount,0);
      
      for (int i = 0; i < result.FieldCount; i++)
      {
        object value = result.GetFieldValue(row_index,i);
        
        PhpBytes bytes = value as PhpBytes;
        if (bytes != null)
          array.Add(bytes.Data.Length); 
        else if (value != null)
          array.Add(value.ToString().Length); 
        else
          array.Add(0);
      }  
      
      return array;
    }

    #endregion
		
    #region TODO: mysql_unbuffered_query
		
    /// <summary>
    /// Executes a query. 
    /// </summary>
    /// <param name="query">Query string.</param>
    /// <returns>Query result resource.</returns>
    /// <remarks>Equivalent to <see cref="Query(string)"/> (<c>mysql_query</c> in PHP). Unbuffered queries not supported.</remarks>
    [ImplementsFunction("mysql_unbuffered_query")]
    public static PhpResource QueryUnbuffered(string query)
    {
      PhpDbConnection last_connection = manager.GetLastConnection();

      if (last_connection == null)
        last_connection = (PhpDbConnection) Connect();

      return Query(query, last_connection);
    }

    /// <summary>
    /// Executes a query. 
    /// </summary>
    /// <param name="query">Query string.</param>
    /// <param name="linkIdentifier">Connection resource.</param>
    /// <returns>Query result resource.</returns>
    /// <remarks>Equivalent to <see cref="Query(string,PhpResource)"/> (<c>mysql_query</c> in PHP). Unbuffered queries not supported.</remarks>
    [ImplementsFunction("mysql_unbuffered_query")]
    public static PhpResource QueryUnbuffered(string query, PhpResource linkIdentifier)
    {
      return Query(query, linkIdentifier);
    }
		
    #endregion
		
    #region mysql_list_dbs, mysql_db_name
		
    /// <summary>
    /// Gets a query result containing a list of database.
    /// </summary>
    /// <returns>The result of "SHOW TABLES {database}" query or <B>null</B> (<B>false</B> in PHP) on failure.</returns>
    [ImplementsFunction("mysql_list_dbs")]
    public static PhpResource GetDatabaseList()
    {
      PhpDbConnection last_connection = manager.GetLastConnection();

      if (last_connection == null)
        last_connection = (PhpDbConnection) Connect();

      return GetDatabaseList(last_connection);
    }
		
    /// <summary>
    /// Gets a query result containing a list of databases.
    /// </summary>
    /// <param name="linkIdentifier">Connection resource.</param>
    /// <returns>The result of "SHOW DATABASES" query or <B>null</B> (<B>false</B> in PHP) on failure.</returns>
    [ImplementsFunction("mysql_list_dbs")]
    public static PhpResource GetDatabaseList(PhpResource linkIdentifier)
    {
      PhpMyDbConnection connection = PhpMyDbConnection.ValidConnection(linkIdentifier);
      if (connection == null) return null;
			
      return connection.ExecuteQuery("SHOW DATABASES",true);
    }
    		
     /// <summary>
    /// Gets a name of the table from a result resource returned by <see cref="GetDatabaseList()"/> (<c>mysql_list_dbs</c> in PHP).
    /// </summary>
    /// <param name="resultHandle">Result resource containing a list of databases.</param>
    /// <param name="rowIndex">Database index.</param>
    /// <returns>Database name.</returns>
    /// <remarks>
    /// Result is affected by run-time quoting 
    /// (<see cref="LocalConfiguration.VariablesSection.QuoteRuntimeVariables"/>).
		/// </remarks>
		[ImplementsFunction("mysql_db_name")]
    [return: CastToFalse]
    public static string GetDatabaseName(PhpResource resultHandle, int rowIndex)
    {		
      PhpMyDbResult result = PhpMyDbResult.ValidResult(resultHandle);
      if (result == null) return null;
			
      ScriptContext context = ScriptContext.CurrentContext;
      return Core.Convert.Quote(result.GetFieldValue(rowIndex,0) as string,context);
    }	
		
    #endregion

    #region mysql_list_tables, mysql_tablename
		
    /// <summary>
    /// Gets a query result containing a list of tables in a specified database.
    /// </summary>
    /// <param name="database">Database name.</param>
    /// <returns>The result of "SHOW TABLES {database}" query or <B>null</B> (<B>false</B> in PHP) on failure.</returns>
    [ImplementsFunction("mysql_list_tables")]
    [return: CastToFalse]
    public static PhpResource GetTablesList(string database)
    {
      PhpDbConnection last_connection = manager.GetLastConnection();

      if (last_connection == null)
        last_connection = (PhpDbConnection) Connect();

      return GetTablesList(database,last_connection);
    }

    /// <summary>
    /// Gets a query result containing a list of tables in a specified database.
    /// </summary>
    /// <param name="database">Database name.</param>
    /// <param name="linkIdentifier">Connection resource.</param>
    /// <returns>The result of "SHOW TABLES {database}" query or <B>null</B> (<B>false</B> in PHP) on failure.</returns>
    [ImplementsFunction("mysql_list_tables")]
    [return: CastToFalse]
    public static PhpResource GetTablesList(string database,PhpResource linkIdentifier)
    {
      PhpMyDbConnection connection = PhpMyDbConnection.ValidConnection(linkIdentifier);
      if (connection == null) return null;

      if (database==null || database=="") 
      {
        PhpException.InvalidArgument("database",LibResources.GetString("arg:null_or_empty"));
        return null;
      }  

      return connection.ExecuteQuery("SHOW TABLES FROM " + EscapeString(database),true);
    }
		
    /// <summary>
		/// Gets a name of the table from a result resource returned by <c>GetTablesList</c> (<c>mysql_list_tables</c> in PHP).
    /// </summary>
    /// <param name="resultHandle">Result resource containing a list of tables.</param>
    /// <param name="rowIndex">Table index.</param>
    /// <returns>Table name.</returns>
    /// <remarks>
    /// Result is affected by run-time quoting 
    /// (<see cref="LocalConfiguration.VariablesSection.QuoteRuntimeVariables"/>).
		/// </remarks>
		[ImplementsFunction("mysql_tablename")]
    public static string GetTableName(PhpResource resultHandle,int rowIndex)
    {
      PhpMyDbResult result = PhpMyDbResult.ValidResult(resultHandle);
      if (result == null) return null;
			
      ScriptContext context = ScriptContext.CurrentContext;
      return Core.Convert.Quote(result.GetFieldValue(rowIndex,0) as string,context);
    }
		
    #endregion
		
    #region mysql_list_processes
		
    /// <summary>
    /// Gets a query result containing a list of processes.
    /// </summary>
    /// <returns>The result of "SHOW PROCESSLIST" query.</returns>
    [ImplementsFunction("mysql_list_processes")]
    [return: CastToFalse]
    public static PhpResource ListProcesses()
    {
      PhpDbConnection last_connection = manager.GetLastConnection();

      if (last_connection == null)
        last_connection = (PhpDbConnection) Connect();

      return ListProcesses(last_connection);
    }

    /// <summary>
    /// Gets a query result containing a list of processes.
    /// </summary>
    /// <param name="linkIdentifier">Connection resource.</param>
    /// <returns>The result of "SHOW PROCESSLIST" query.</returns>
    [ImplementsFunction("mysql_list_processes")]
    [return: CastToFalse]
    public static PhpResource ListProcesses(PhpResource linkIdentifier)
    {
      PhpMyDbConnection connection = PhpMyDbConnection.ValidConnection(linkIdentifier);
      if (connection == null) return null;

      return connection.ExecuteQuery("SHOW PROCESSLIST",true);
    }
		
    #endregion
		
    #region mysql_real_escape_string, mysql_escape_string
		
    /// <summary>
    /// Escapes special characters in a string for use in a SQL statement.
    /// </summary>
    /// <param name="str">String to escape.</param>
    /// <returns>Escaped string.</returns>
    [ImplementsFunction("mysql_real_escape_string")]
    public static string RealEscapeString(string str)
    {
      PhpDbConnection last_connection = manager.GetLastConnection();

      if (last_connection == null)
        last_connection = (PhpDbConnection) Connect();

      return RealEscapeString(str,last_connection);
    }

    /// <summary>
    /// Escapes special characters in a string for use in a SQL statement.
    /// </summary>
    /// <param name="str">String to escape.</param>
    /// <param name="linkIdentifier">Connection resource.</param>
    /// <returns>Escaped string.</returns>
    [ImplementsFunction("mysql_real_escape_string")]
    public static string RealEscapeString(string str,PhpResource linkIdentifier)
    {
      PhpMyDbConnection connection = PhpMyDbConnection.ValidConnection(linkIdentifier);
      if (connection == null) return null;
      
      return EscapeString(str);
    }

    /// <summary>
    /// Escapes special characters in a string for use in a SQL statement.
    /// </summary>
    /// <param name="str">String to escape.</param>
    /// <returns>Escaped string.</returns>
    [ImplementsFunction("mysql_escape_string")]
    public static string EscapeString(string str)
    {
      StringBuilder sb = new StringBuilder();
      for(int i=0;i<str.Length;i++)
      {
        char c = str[i];
        switch (c)
        {
          case '\0': sb.Append(@"\0");break;
          case '\\': sb.Append(@"\\");break;
          case '\n': sb.Append(@"\n");break;
          case '\r': sb.Append(@"\r");break;
          case '\u001a': sb.Append(@"\Z");break;
          case '\'': sb.Append(@"\'");break;
          case '"' : sb.Append("\\\"");break;
          default: sb.Append(c);break;
        }
      }
      
      return sb.ToString();
    }
    		
    #endregion
		
    #region mysql_info (NS), mysql_stat
    
    /// <summary>
    /// Not supported.
    /// </summary>
    /// <returns><B>null</B>.</returns>
    [ImplementsFunction("mysql_info")]
    public static string GetInfo()
    {
      PhpDbConnection last_connection = manager.GetLastConnection();

      if (last_connection == null)
        last_connection = (PhpDbConnection) Connect();

      return GetInfo(last_connection);
    }
		
    /// <summary>
    /// Not supported.
    /// </summary>
    /// <param name="linkIdentifier">Connection resource.</param>
    /// <returns><B>null</B>.</returns>
    [ImplementsFunction("mysql_info")]
    public static string GetInfo(PhpResource linkIdentifier)
    {
      PhpMyDbConnection connection = PhpMyDbConnection.ValidConnection(linkIdentifier);
      if (connection == null) return null;
			
      PhpException.FunctionNotSupported();
      return null;
    }
    
    /// <summary>
    /// Gets server statistics.
    /// </summary>
    /// <returns>String of statistic data gathered from the server.</returns>
    [ImplementsFunction("mysql_stat")]
    public static string GetStatistics()
    {
      PhpDbConnection last_connection = manager.GetLastConnection();

      if (last_connection == null)
        last_connection = (PhpDbConnection) Connect();

      return GetStatistics(last_connection);
    }
		
    /// <summary>
    /// Gets server statistics.
    /// </summary>
    /// <param name="linkIdentifier">Connection resource.</param>
    /// <returns>String of statistic data gathered from the server.</returns>
    [ImplementsFunction("mysql_stat")]
    public static string GetStatistics(PhpResource linkIdentifier)
    {
      PhpMyDbConnection connection = PhpMyDbConnection.ValidConnection(linkIdentifier);
      if (connection == null) return null;
			
      PhpDbResult result = connection.ExecuteQuery("SHOW STATUS",true);
			
      string 
        uptime = null, threads = null, questions = null, slow_queries = null, 
        opens = null, flush_tables = null, open_tables = null;
			
      for (int i = 0; i < result.RowCount; i++)
      {
        switch (result.GetFieldValue(i,0) as string)
        {
          case "Uptime": uptime = result.GetFieldValue(i,1) as string; break;
          case "Threads_created": threads = result.GetFieldValue(i,1) as string; break;
          case "Questions": questions = result.GetFieldValue(i,1) as string; break;
          case "Slow_queries": slow_queries = result.GetFieldValue(i,1) as string; break;
          case "Flush_commands": flush_tables = result.GetFieldValue(i,1) as string; break;
          case "Open_tables": open_tables = result.GetFieldValue(i,1) as string; break;
        }  
      }
			
      double d_questions = 0.0, d_uptime = 0.0;
			
      try
      {
        d_uptime = (uptime!=null) ? Double.Parse(uptime) : 0;
        d_questions = (questions!=null) ? Double.Parse(questions) : 0;
      }
      catch (FormatException)
      {
      }
      catch (OverflowException)
      {
      }
			
      return String.Format(
        "Uptime: {0}  Threads: {1}  Questions: {2}  Slow queries: {3}  " + 
        "Opens: {4}  Flush tables: {5}  Open tables: {6}  Queries per second avg: {7:0.000}",
        uptime, threads, questions, slow_queries, opens, flush_tables, open_tables,
        d_questions/d_uptime);
    }
 
    #endregion

    #region mysql_get_client_info, mysql_get_server_info, mysql_get_host_info, mysql_get_proto_info
		
    /// <summary>
    /// Gets a version of the client library.
    /// </summary>
    /// <returns>Equivalent native library varsion.</returns>
    [ImplementsFunction("mysql_get_client_info")]
    public static string GetClientInfo()
    {
      return EquivalentNativeLibraryVersion;
    }
		
    /// <summary>
    /// Gets server version.
    /// </summary>
    /// <returns>Server version</returns>
    [ImplementsFunction("mysql_get_server_info")]
    public static string GetServerInfo()
    {
      PhpDbConnection last_connection = manager.GetLastConnection();

      if (last_connection == null)
        last_connection = (PhpDbConnection) Connect();

      return GetServerInfo(last_connection);
    }
		
    /// <summary>
    /// Gets server version.
    /// </summary>
    /// <returns>Server version</returns>
    [ImplementsFunction("mysql_get_server_info")]
    public static string GetServerInfo(PhpResource linkIdentifier)
    {
      PhpMyDbConnection connection = PhpMyDbConnection.ValidConnection(linkIdentifier);
      if (connection == null) return null;
			
      return connection.Connection.ServerVersion;
    }

    /// <summary>
    /// Gets information about the server.
    /// </summary>
    /// <returns>Server name and protocol type.</returns>
    [ImplementsFunction("mysql_get_host_info")]
    public static string GetHostInfo()
    {
      PhpDbConnection last_connection = manager.GetLastConnection();

      if (last_connection == null)
        last_connection = (PhpDbConnection) Connect();

      return GetHostInfo(last_connection);
    }

    /// <summary>
    /// Gets information about the server.
    /// </summary>
    /// <param name="linkIdentifier">Connection resource.</param>
    /// <returns>Server name and protocol type.</returns>
    [ImplementsFunction("mysql_get_host_info")]
    public static string GetHostInfo(PhpResource linkIdentifier)
    {
      PhpMyDbConnection connection = PhpMyDbConnection.ValidConnection(linkIdentifier);
      if (connection == null) return null;
			
      // TODO: how to get the protocol?
      return String.Concat(connection.Server," via TCP/IP");
    }
		
    /// <summary>
    /// Gets version of the protocol.
    /// </summary>
    /// <returns>Protocol version.</returns>
    [ImplementsFunction("mysql_get_proto_info")]
    public static string GetProtocolInfo()
    {
      PhpDbConnection last_connection = manager.GetLastConnection();

      if (last_connection == null)
        last_connection = (PhpDbConnection) Connect();

      return GetProtocolInfo(last_connection);
    }

    /// <summary>
    /// Gets version of the protocol.
    /// </summary>
    /// <param name="linkIdentifier">Connection resource.</param>
    /// <returns>Protocol version.</returns>
    [ImplementsFunction("mysql_get_proto_info")]
    public static string GetProtocolInfo(PhpResource linkIdentifier)
    {
      PhpMyDbConnection connection = PhpMyDbConnection.ValidConnection(linkIdentifier);
      if (connection == null) return null;

      object value = connection.QueryGlobalVariable("protocol_version");
      return (value!=null) ? value.ToString() : DefaultProtocolVersion;
    }
		
    #endregion
		
    #region mysql_client_encoding

    /// <summary>
    /// Gets the name of the client character set.
    /// </summary>
    /// <returns>Character set name.</returns>
    [ImplementsFunction("mysql_client_encoding")]
    public static string GetClientEncoding()
    {
      PhpDbConnection last_connection = manager.GetLastConnection();

      if (last_connection == null)
        last_connection = (PhpDbConnection) Connect();

      return GetClientEncoding(last_connection);
    }
		
    /// <summary>
    /// Gets the name of the client character set.
    /// </summary>
    /// <param name="linkIdentifier">Connection resource.</param>
    /// <returns>Character set name.</returns>
    [ImplementsFunction("mysql_client_encoding")]
    public static string GetClientEncoding(PhpResource linkIdentifier)
    {
      PhpMyDbConnection connection = PhpMyDbConnection.ValidConnection(linkIdentifier);
      if (connection == null) return null;
			
      object value = connection.QueryGlobalVariable("character_set_client");
      return (value!=null) ? value.ToString() : DefaultClientCharset;
    }
				
    #endregion
		
    #region mysql_ping

    /// <summary>
    /// Pings a server connection or reconnect if there is no connection. 
    /// </summary>
    /// <returns>Whether connection is established.</returns>
    [ImplementsFunction("mysql_ping")]
    public static bool Ping()
    {
      PhpDbConnection last_connection = manager.GetLastConnection();

      if (last_connection == null)
        last_connection = (PhpDbConnection) Connect();

      return Ping(last_connection);
    }
		
    /// <summary>
    /// Pings a server connection or reconnect if there is no connection. 
    /// </summary>
    /// <param name="linkIdentifier">Connection resource.</param>
    /// <returns>Whether connection is established.</returns>
    [ImplementsFunction("mysql_ping")]
    public static bool Ping(PhpResource linkIdentifier)
    {
      PhpMyDbConnection connection = PhpMyDbConnection.ValidConnection(linkIdentifier);
      if (connection == null) return false;
			
      try
      {
        return connection.Connection.Ping();
      }
      catch (Exception)
      {
        return false;
      } 
    }
				
    #endregion	
  }
}