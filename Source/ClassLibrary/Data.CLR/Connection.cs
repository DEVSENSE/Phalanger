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
using System.Collections;
using System.Collections.Generic;

using PHP.Core;

namespace PHP.Library.Data
{
	/// <summary>
	/// Abstract class implementing common functionality of PHP connection resources.
	/// </summary>
	public abstract class PhpDbConnection : PhpResource
	{
		#region Fields & Properties

		/// <summary>
		/// Connection string.
		/// </summary>
		public string/*!*/ ConnectionString { get { return connectionString; } }
		private string/*!*/ connectionString;

		/// <summary>
		/// Underlying database connection.
		/// </summary>
		protected IDbConnection/*!*/ connection;

		/// <summary>
		/// A result associated with this connection that possibly has not been closed yet.
		/// </summary>
		protected IDataReader pendingReader;

		/// <summary>
		/// Last result resource.
		/// </summary>
		public PhpDbResult LastResult { get { return lastResult; } }
		private PhpDbResult lastResult;

		/// <summary>
		/// Gets an exception thrown by last performed operation or a <B>null</B> reference 
		/// if that operation succeeded.
		/// </summary>
		public Exception LastException { get { return lastException; } }
		private Exception lastException;

		/// <summary>
		/// Gets the number of rows affected by the last query executed on this connection.
		/// </summary>
		public int LastAffectedRows
		{
			get
			{
				if (lastResult == null) return -1;

				// SELECT gives -1, UPDATE/INSERT gives the number:
				return (lastResult.RecordsAffected >= 0) ? lastResult.RecordsAffected : lastResult.RowCount;
			}
		}

		#endregion

		/// <summary>
		/// Creates a new instance of <see cref="PhpDbConnection"/> with a specified connection.
		/// </summary>
		/// <param name="connectionString">Connection string.</param>
		/// <param name="connection">Database connection.</param>
		/// <param name="name">Connection resource name.</param>
		/// <exception cref="ArgumentNullException"><paramref name="connection"/> is a <B>null</B> reference.</exception>
		protected PhpDbConnection(string/*!*/ connectionString, IDbConnection/*!*/ connection, string/*!*/ name)
			: base(name)
		{
			if (connection == null)
				throw new ArgumentNullException("connection");
			if (connectionString == null)
				throw new ArgumentNullException("connectionString");

			this.connection = connection;
			this.connectionString = connectionString;
		}

		/// <summary>
		/// Gets a query result resource.
		/// </summary>
		/// <param name="connection">Database connection.</param>
		/// <param name="reader">Data reader to be used for result resource population.</param>
		/// <param name="convertTypes">Whether to convert data types to PHP ones.</param>
		/// <returns>Result resource holding all resulting data of the query.</returns>
		protected abstract PhpDbResult GetResult(PhpDbConnection/*!*/ connection, IDataReader/*!*/ reader, bool convertTypes);

		/// <summary>
		/// Creates a command instance.
		/// </summary>
		/// <returns>Instance of command specific for the database provider.</returns>
		protected abstract IDbCommand/*!*/ CreateCommand();

		/// <summary>
		/// Builds a connection string.
		/// </summary>
		public static string/*!*/ BuildConnectionString(string server, string user, string password, string additionalSettings)
		{
			StringBuilder result = new StringBuilder(8);
			result.Append("server=");
			result.Append(server);
			//			result.Append(";database=");
			//			result.Append(database);
			result.Append(";user id=");
			result.Append(user);
			result.Append(";password=");
			result.Append(password);

			if (!String.IsNullOrEmpty(additionalSettings))
			{
				result.Append(';');
				result.AppendFormat(additionalSettings);
			}

			return result.ToString();
		}

		/// <summary>
		/// Opens a database connection if it has not been opened yet.
		/// </summary>
		/// <returns><B>true</B> if successful.</returns>
		/// <exception cref="PhpException">Attempt to connect the database failed (Warning).</exception>
		/// <remarks>
		/// Sets <see cref="LastException"/> to <B>null</B> (on success) or to the exception object (on failure).
		/// </remarks>
		public bool Connect()
		{
			Debug.Assert(connection != null);

			if (connection.State == ConnectionState.Open) return true;

			connection.ConnectionString = this.ConnectionString;
			try
			{
				connection.Open();
				lastException = null;
			}
			catch (Exception e)
			{
				lastException = e;
				PhpException.Throw(PhpError.Warning, LibResources.GetString("cannot_open_connection",
				  GetExceptionMessage(e)));

				return false;
			}

			return true;
		}

		/// <summary>
		/// Closes connection and releases the resource.
		/// </summary>
		protected override void FreeManaged()
		{
			base.FreeManaged();

			ClosePendingReader();

			try
			{
				if (connection != null)
				{
					connection.Close();
				}
				lastException = null;
			}
			catch (Exception e)
			{
				lastException = e;
				PhpException.Throw(PhpError.Warning, LibResources.GetString("error_closing_connection",
				  GetExceptionMessage(e)));
			}
			connection = null;
		}

		/// <summary>
		/// Closes pending reader.
		/// </summary>
		public void ClosePendingReader()
		{
			if (pendingReader != null && !pendingReader.IsClosed)
				pendingReader.Close();

			pendingReader = null;
		}

		/// <summary>
		/// Executes a query on the connection.
		/// </summary>
		/// <param name="query">The query.</param>
		/// <param name="convertTypes">Whether to convert data types to PHP ones.</param>
		/// <returns>PhpDbResult class representing the data read from database.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="query"/> is a <B>null</B> reference.</exception>
		/// <exception cref="PhpException">Query execution failed (Warning).</exception>
		public PhpDbResult ExecuteQuery(string/*!*/ query, bool convertTypes)
		{
			if (query == null)
				throw new ArgumentNullException("query");

			return ExecuteCommand(query, CommandType.Text, convertTypes, null, false);
		}

		/// <summary>
		/// Executes a stored procedure on the connection.
		/// </summary>
		/// <param name="procedureName">Procedure name.</param>
		/// <param name="parameters">Parameters.</param>
		/// <param name="skipResults">Whether to load results.</param>
		/// <returns>PhpDbResult class representing the data read from database.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="procedureName"/> is a <B>null</B> reference.</exception>
		/// <exception cref="PhpException">Procedure execution failed (Warning).</exception>
		public PhpDbResult ExecuteProcedure(string/*!*/ procedureName, IEnumerable<IDataParameter> parameters, bool skipResults)
		{
			if (procedureName == null)
				throw new ArgumentNullException("procedureName");

			return ExecuteCommand(procedureName, CommandType.StoredProcedure, true, parameters, skipResults);
		}

		/// <summary>
		/// Executes a command on the connection.
		/// </summary>
		/// <param name="commandText">Command text.</param>
		/// <param name="convertTypes">Whether to convert data types to PHP ones.</param>
		/// <param name="commandType">Command type.</param>
		/// <param name="parameters">Parameters.</param>
		/// <param name="skipResults">Whether to load results.</param>
		/// <returns>PhpDbResult class representing the data read from database.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="commandText"/> is a <B>null</B> reference.</exception>
		/// <exception cref="PhpException">Command execution failed (Warning).</exception>
		public PhpDbResult ExecuteCommand(string/*!*/ commandText, CommandType commandType, bool convertTypes,
		  IEnumerable<IDataParameter> parameters, bool skipResults)
		{
			if (commandText == null)
				throw new ArgumentNullException("commandText");

			if (!Connect()) return null;

			ClosePendingReader();

			IDbCommand command = CreateCommand();

			command.Connection = connection;
			command.CommandText = commandText;
			command.CommandType = commandType;

			if (parameters != null)
			{
				command.Parameters.Clear();
				foreach (IDataParameter parameter in parameters)
					command.Parameters.Add(parameter);
			}

			try
			{
				if (skipResults)
				{
					pendingReader = command.ExecuteReader();

					// reads all data:
					do { while (pendingReader.Read()); } while (pendingReader.NextResult());

					lastException = null;
					return null;
				}
				else
				{
					lastResult = null;

					pendingReader = command.ExecuteReader();
					PhpDbResult result = GetResult(this, pendingReader, convertTypes);
					result.command = command;

					lastException = null;
					lastResult = result;
					return result;
				}
			}
			catch (Exception e)
			{
				lastException = e;
				PhpException.Throw(PhpError.Warning, LibResources.GetString("command_execution_failed",
					GetExceptionMessage(e)));
				return null;
			}
		}

		/// <summary>
		/// Reexecutes a command associated with a specified result resource to get schema of the command result.
		/// </summary>
		/// <param name="result">The result resource.</param>
		internal void ReexecuteSchemaQuery(PhpDbResult/*!*/ result)
		{
			if (!Connect() || result.Command == null) return;

			ClosePendingReader();

			try
			{
				result.Reader = pendingReader = result.Command.ExecuteReader(CommandBehavior.KeyInfo | CommandBehavior.SchemaOnly);
			}
			catch (Exception e)
			{
				lastException = e;
				PhpException.Throw(PhpError.Warning, LibResources.GetString("command_execution_failed",
					GetExceptionMessage(e)));
			}
		}

		/// <summary>
		/// Changes the active database on opened connection.
		/// </summary>
		/// <param name="databaseName"></param>
		/// <returns>true if databse was changed; otherwise returns false</returns>
		public bool SelectDb(string databaseName)
		{
            ClosePendingReader();

			try
			{
				if (this.connection.State == ConnectionState.Open)
				{
					connection.ChangeDatabase(databaseName);
					lastException = null;
					return true;
				}
			}
			catch (Exception e)
			{
				lastException = e;
				PhpException.Throw(PhpError.Warning, LibResources.GetString("database_selection_failed",
				  GetExceptionMessage(e)));
			}

			return false;
		}

		/// <summary>
		/// Gets a message from an exception raised by the connector.
		/// Removes the ending dot.
		/// </summary>
		/// <param name="e">Exception.</param>
		/// <returns>The message.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="e"/> is a <B>null</B> reference.</exception>
		public virtual string GetExceptionMessage(Exception/*!*/ e)
		{
			if (e == null) throw new ArgumentNullException("e");
			return PhpException.ToErrorMessage(e.Message);
		}

		/// <summary>
		/// Gets the last error message.
		/// </summary>
		/// <returns>The message or an empty string if no error occured.</returns>
		public virtual string GetLastErrorMessage()
		{
			return (LastException != null) ? LastException.Message : String.Empty;
		}

		/// <summary>
		/// Gets the last error number.
		/// </summary>
		/// <returns>-1 on error, zero otherwise.</returns>
		/// <remarks>Should be implemented by the subclass if the respective provider supports error numbers.</remarks>
		public virtual int GetLastErrorNumber()
		{
			return (LastException != null) ? -1 : 0;
		}
	}
}
