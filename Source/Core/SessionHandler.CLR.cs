/*

 Copyright (c) 2004-2006 Tomas Matousek.
  
 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.IO;
using System.Web;
using System.Web.SessionState;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Collections;
using System.Collections.Generic;

namespace PHP.Core
{
	/// <summary>
	/// A session state.
	/// </summary>
	public enum SessionStates
	{
		/// <summary>
		/// Session is being started. Session handler's 
		/// <see cref="SessionHandler.Load"/> method is called during this phase.
		/// </summary>
		Starting,

		/// <summary>
		/// Session has been started. 
		/// </summary>
		Started,

		/// <summary>
		/// Session is being closed. Session handler's 
		/// <see cref="SessionHandler.Persist"/> method is called during this phase.
		/// </summary>
		Closing,

		/// <summary>
		/// Session has been closed.
		/// </summary>
		Closed
	}

	/// <summary>
	/// Exception thrown by a Phalanger session manager.
	/// </summary>
	public sealed class SessionException : Exception
	{
		internal SessionException(string message)
			: base(message)
		{
		}
	}

	#region SessionHandler

	/// <summary>
	/// Base abstract class for custom session handlers.
	/// </summary>
	public abstract class SessionHandler : MarshalByRefObject
	{
		/// <summary>
		/// Gets a name of the handler.
		/// </summary>
		public abstract string Name { get; }

		/// <summary>
		/// Loads variables stored in the session.
		/// </summary>
		/// <returns>The array containing session variables. Can return a <B>null</B> reference.</returns>
		/// <param name="context">A current script context. Can't be a <B>null</B> reference.</param>
		/// <param name="httpContext">A current HTTP context. Can't be a <B>null</B> reference.</param>
		internal protected abstract PhpArray Load(ScriptContext context, HttpContext httpContext);

		/// <summary>
		/// Persists session variables.
		/// </summary>
		/// <param name="variables">Session variables to be persisted.</param>
		/// <param name="context">A current script context. Can't be a <B>null</B> reference.</param>
		/// <param name="httpContext">A current HTTP context. Can't be a <B>null</B> reference.</param>
		internal protected abstract void Persist(PhpArray variables, ScriptContext context, HttpContext httpContext);

		/// <summary>
		/// Called immediately before the session is abandoned.
		/// </summary>
		/// <param name="context">A current script context. Can't be a <B>null</B> reference.</param>
		/// <param name="httpContext">A current HTTP context. Can't be a <B>null</B> reference.</param>
		internal protected abstract void Abandoning(ScriptContext context, HttpContext httpContext);

		/// <summary>
		/// Keeps the object living forever.
		/// </summary>
        [System.Security.SecurityCritical]
        public override object InitializeLifetimeService()
		{
			return null;
		}
	}

	#endregion

	#region AspNetSessionHandler

	/// <summary>
	/// Session handler based of ASP.NET sessions.
	/// </summary>
	public sealed class AspNetSessionHandler : SessionHandler
	{
		private AspNetSessionHandler() { }

		public const string AspNetSessionName = "ASP.NET_SessionId";
		public const string PhpNetSessionVars = "Phalanger.SessionVars";
		internal const string DummySessionItem = "Phalanger_DummySessionKeepAliveItem(\uffff)";

		/// <summary>
		/// Singleton instance.
		/// </summary>
		public static readonly AspNetSessionHandler Default = new AspNetSessionHandler();

		/// <summary>
		/// Gets a string representation.
		/// </summary>
		/// <returns>The name of the handler.</returns>
		public override string ToString()
		{
			return Name;
		}

		/// <summary>
		/// Gets a name of the handler used in the configuration.
		/// </summary>
		public override string Name
		{
			get { return "aspnet"; }
		}

		/// <summary>
		/// Loads variables from ASP.NET session to an array.
		/// </summary>
		internal protected override PhpArray Load(ScriptContext context, HttpContext httpContext)
		{
			HttpSessionState state = httpContext.Session;

			PhpArray result = null;

			if (state.Mode == SessionStateMode.InProc)
			{
				result = new PhpArray();

				foreach (string name in state)
				{
					result[name] = state[name];
				}

				context.AcquireArray(result);
			}
			else
			{
				byte[] data = state[PhpNetSessionVars] as byte[];

				if (data != null)
				{
					MemoryStream stream = new MemoryStream(data);
					BinaryFormatter formatter = new BinaryFormatter(null,
						new StreamingContext(StreamingContextStates.Persistence));

					result = formatter.Deserialize(stream) as PhpArray;
				}
			}

			return (result != null) ? result : new PhpArray();
		}

		/// <summary>
		/// Stores session variables to ASP.NET session.
		/// </summary>
		internal protected override void Persist(PhpArray variables, ScriptContext context, HttpContext httpContext)
		{
			HttpSessionState state = httpContext.Session;

			// removes all items (some could be changed or removed in PHP):
            // TODO: some session variables could be added in ASP.NET application
			state.Clear();

			if (state.Mode == SessionStateMode.InProc)
			{
				context.ReleaseArray(variables);

				// populates session collection from variables:
				foreach (KeyValuePair<IntStringKey, object> entry in variables)
				{
					// skips resources:
					if (!(entry.Value is PhpResource))
						state.Add(entry.Key.ToString(), entry.Value);
				}
			}
			else
			{
				// if the session is maintained out-of-process, serialize the entire $_SESSION autoglobal
				MemoryStream stream = new MemoryStream();
				BinaryFormatter formatter = new BinaryFormatter(null,
					new StreamingContext(StreamingContextStates.Persistence));

				formatter.Serialize(stream, variables);

				// add the serialized $_SESSION to ASP.NET session:
				state.Add(PhpNetSessionVars, stream.ToArray());
			}
		}

		/// <summary>
		/// Called immediately before the session is abandoned.
		/// </summary>
		internal protected override void Abandoning(ScriptContext context, HttpContext httpContext)
		{

		}

		/// <summary>
		/// Gets session cookie associated with a specified HTTP context.
		/// </summary>
		/// <param name="context">The context.</param>
		/// <returns>The cookie.</returns>
		public static HttpCookie GetCookie(HttpContext/*!*/ context)
		{
			if (context == null) throw new ArgumentNullException("context");

			// no cookies available:
			if (context.Session == null || context.Session.IsCookieless) return null;

			// gets cookie from request:
			return context.Request.Cookies[AspNetSessionName];
		}
	}

	#endregion

	#region SessionHandlers

	/// <summary>
	/// Maintains known session handler set.
	/// </summary>
	/// <threadsafety static="true"/>
	public sealed class SessionHandlers
	{
		/// <summary>
		/// Registered handlers.
		/// </summary>
        private static Dictionary<string, SessionHandler>/*!!*/handlers;

		/// <summary>
		/// Initializes static list of handlers to contain an ASP.NET handler.
		/// </summary>
		static SessionHandlers()
		{
            handlers = new Dictionary<string, SessionHandler>(3);
			RegisterHandler(AspNetSessionHandler.Default);
		}

		/// <summary>
		/// Registeres a new session handler.
		/// </summary>
		/// <param name="handler">The handler.</param>
		/// <returns>Whether handler has been successfuly registered. Two handlers with the same names can't be registered.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="handler"/> is a <B>null</B> reference.</exception>
		public static bool RegisterHandler(SessionHandler handler)
		{
			if (handler == null) throw new ArgumentNullException("handler");
			if (handler.Name == null) return false;

			lock (handlers)
			{
				if (handlers.ContainsKey(handler.Name))
					return false;

				handlers.Add(handler.Name, handler);
			}

			return true;
		}

		/// <summary>
		/// Gets a session handler by specified name.
		/// </summary>
		/// <param name="name">The name of the handler.</param>
		/// <returns>The handler or <B>null</B> reference if such handler has not been registered.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="name"/> is a <B>null</B> reference.</exception>
		public static SessionHandler GetHandler(string name)
		{
			if (name == null) throw new ArgumentNullException("name");

			SessionHandler value;
            lock (handlers)
                handlers.TryGetValue(name, out value);

            return value;
		}
	}

	#endregion
}
