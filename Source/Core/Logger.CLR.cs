/*

 Copyright (c) 2004-2006 Tomas Matousek.
  
 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.IO;
using System.Threading;
using System.Diagnostics;

namespace PHP.Core
{
	/// <summary>
	/// Hanldes logging to a file and event log.
	/// <threadsafety static="true"/>
	/// </summary>
	public sealed class Logger
	{
		private Logger() { }

		private const string mutexNamePrefix = "PhpNetErrorLogMutex_";
		private const int mutexTimeout = 1000;

		/// <summary>
		/// Appends a line into the specified log file synchronizing the access via a named mutex.
		/// </summary>
		/// <param name="fileName">The name of a log file.</param>
		/// <param name="message">The message to be appended.</param>
		/// <exception cref="IOException">An I/O error occurs.</exception>
		/// <remarks>
		/// <para>If the file doesn't exists it will be created.</para>
		/// </remarks>
		/// <exception cref="IOException">Unexpected IO error occured.</exception>
		public static void AppendLine(string fileName, string message)
		{
			message = DateTime.Now.ToString("[dd-MMM-yyyy HH:mm:ss] ") + message;

			bool mutex_created;
			using (Mutex m = new Mutex(true, mutexNamePrefix + fileName.GetHashCode(), out mutex_created))
			{
				// try to acquire the mutex if not acquired yet:
				if (!mutex_created && !m.WaitOne(mutexTimeout, false))
					throw new IOException();

				try
				{
					using (StreamWriter sw = File.AppendText(fileName))
						sw.WriteLine(message);
				}
				catch (Exception)
				{
					throw new IOException();
				}
				finally
				{
					m.ReleaseMutex();
				}
			}
		}

		/// <summary>
		/// Adds error message to system Event log.
		/// </summary>
		/// <param name="message"></param>
		public static void AddToEventLog(string message)
		{
			EventLog.WriteEntry("Phalanger", message, EventLogEntryType.Error);
		}
	}
}
