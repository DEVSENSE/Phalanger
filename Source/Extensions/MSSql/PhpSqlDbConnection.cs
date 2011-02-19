/*

 Copyright (c) 2005-2006 Tomas Matousek and Martin Maly.  

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;

using PHP.Core;

namespace PHP.Library.Data
{
	internal sealed class SqlConnectionManager : ConnectionManager
	{
		protected override PhpDbConnection CreateConnection(string/*!*/ connectionString)
		{
			return new PhpSqlDbConnection(connectionString, ScriptContext.CurrentContext);
		}
	}

	/// <summary>
	/// SQL connection resource.
	/// </summary>
	public sealed class PhpSqlDbConnection : PhpDbConnection
	{
		internal SqlConnection Connection { get { return (SqlConnection)this.connection; } }

		private readonly ScriptContext/*!*/ context;

		/// <summary>
		/// Creates a new connection resource.
		/// </summary>
		/// <param name="connectionString">Connection string.</param>
		/// <param name="context">Script context associated with the connection.</param>
		public PhpSqlDbConnection(string/*!*/ connectionString, ScriptContext/*!*/ context)
			: base(connectionString, new SqlConnection(), "mssql connection")
		{
			if (context == null)
				throw new ArgumentNullException("context");

			this.context = context;
			// TODO: Connection.InfoMessage += new SqlInfoMessageEventHandler(InfoMessage);
		}

		/// <summary>
		/// Validates whether the specified handler is instance of PhpDbConnection type.
		/// </summary>
		/// <param name="handle"></param>
		/// <returns></returns>
		internal static PhpSqlDbConnection ValidConnection(PhpResource handle)
		{
			PhpSqlDbConnection connection = handle as PhpSqlDbConnection;
			if (connection != null && connection.IsValid) return connection;

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
		protected override PhpDbResult/*!*/ GetResult(PhpDbConnection/*!*/ connection, IDataReader/*!*/ reader, bool convertTypes)
		{
			return new PhpSqlDbResult(connection, reader, convertTypes);
		}

		/// <summary>
		/// Command factory.
		/// </summary>
		/// <returns>An empty instance of <see cref="SqlCommand"/>.</returns>
		protected override IDbCommand/*!*/ CreateCommand()
		{
			SqlCommand command = new SqlCommand();
			MsSqlLocalConfig local = MsSqlConfiguration.GetLocal(context);
			command.CommandTimeout = (local.Timeout > 0) ? local.Timeout : 0;
			return command;
		}
	}
}
