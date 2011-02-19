/*

 Copyright (c) 2005-2006 Tomas Matousek.  

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Text;
using System.Collections;
using System.Threading;

using PHP.Core;

namespace PHP.Library.Data
{
	/// <summary>
	/// Abstract base class for database connection managers.
	/// </summary>
	public abstract class ConnectionManager
	{
		/// <summary>
		/// Connection factory.
		/// </summary>
		/// <param name="connectionString">Connection string.</param>
		/// <returns>Connection.</returns>
		protected abstract PhpDbConnection CreateConnection(string/*!*/ connectionString);

		/// <summary>
		/// List of connections established by the manager.
		/// </summary>
		private ArrayList connections = new ArrayList();

		/// <summary>
		/// Number of all connections established by the application.
		/// </summary>
		private static int AppConnectionCount = 0;

		/// <summary>
		/// Establishes a connection if a connection with the same connection string doesn't exist yet.
		/// </summary>
		/// <param name="connectionString">Connection string.</param>
		/// <param name="newConnection">Whether to create a new connection even if there exists one with same string.</param>
		/// <param name="limit">Maximal number of connections. Negative value means no limit.</param>
		/// <param name="success"><B>true</B> on success, <B>false</B> on failure.</param>
		/// <returns>The connection (opened or not) or a <B>null</B> reference on failure.</returns>
		public PhpDbConnection OpenConnection(string/*!*/ connectionString, bool newConnection, int limit, out bool success)
		{
			if (connectionString == null)
				throw new ArgumentNullException("connectionString");

			PhpDbConnection connection;

			if (!newConnection)
			{
				connection = GetConnectionByString(connectionString);
				if (connection != null)
				{
					success = true;
					return connection;
				}
			}

			int count = Interlocked.Increment(ref AppConnectionCount);

			if (limit >= 0 && count > limit)
			{
				Interlocked.Decrement(ref AppConnectionCount);

				PhpException.Throw(PhpError.Warning, LibResources.GetString("connection_limit_reached", limit));
				success = false;
				return null;
			}

			connection = CreateConnection(connectionString);
			if (!connection.Connect())
			{
				success = false;
				return connection;
			}

			connections.Add(connection);
			success = true;
			return connection;
		}

		private PhpDbConnection GetConnectionByString(string connectionString)
		{
			foreach (PhpDbConnection connection in connections)
			{
				if (connection.ConnectionString == connectionString)
					return connection;
			}
			return null;
		}

		/// <summary>
		/// Removes last used connection from the list of active Connections.
		/// </summary>
		public void RemoveConnection()
		{
			if (connections.Count > 0)
			{
				connections.RemoveAt(connections.Count - 1);
				Interlocked.Decrement(ref AppConnectionCount);
			}
		}

		/// <summary>
		/// Removes specified connection from the list of active connections.
		/// </summary>
		/// <param name="connection">The connection to be removed.</param>
		public void RemoveConnection(PhpDbConnection/*!*/ connection)
		{
			if (connection == null)
				throw new ArgumentNullException("connection");

			if (connections.Count > 0)
			{
				connections.Remove(connection);
				Interlocked.Decrement(ref AppConnectionCount);
			}
		}

		/// <summary>
		/// Returns last opened connection.
		/// </summary>
		/// <returns></returns>
		public PhpDbConnection GetLastConnection()
		{
			if (connections.Count == 0) return null;
			return (PhpDbConnection)connections[connections.Count - 1];
		}
	}
}
