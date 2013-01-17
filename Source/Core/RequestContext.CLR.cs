/*

 Copyright (c) 2005-2006 Tomas Matousek.
  
 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.IO;
using System.Web;
using System.Web.Caching;
using System.Web.SessionState;
using System.Text;
using System.Threading;
using System.Reflection;
using System.Diagnostics;
using System.Collections;
using System.Runtime.Remoting.Messaging;

using PHP.Core.Reflection;
using PHP.Core.Emit;
using System.Collections.Generic;

namespace PHP.Core
{
	/// <summary>
	/// Represents a set of data associated with the current web request targeting PHP scripts.
	/// </summary>
	public sealed partial class RequestContext : IDisposable, ILogicalThreadAffinative
	{
		#region Initialization etc.

		static RequestContext()
		{
			if (HttpContext.Current != null)
				Debug.WebInitialize();
		}

		/// <summary>
		/// The request context associated with the current thread executing the request. 
		/// Set by <see cref="Initialize"/> method when the request starts.
		/// Contains a <B>null</B> reference when the current thread is not executing any web request 
		/// (or prior to the call to <see cref="Initialize"/> method).
		/// </summary>
		public static RequestContext CurrentContext { get { return currentContext; } }
		[ThreadStatic]
		private static RequestContext currentContext = null;

		private RequestContext(HttpContext/*!*/ httpContext)
		{
			this.httpContext = httpContext;
			this.connectionAborted = false;
			this.responseFilter = null;

            // Initialized on the first use (work item 13528)
            // (because the HttpRequest object is not available until
            // the module is actually processing an event in the Request pipeline.)
            this.requestFile = null;
		}

        /// <summary>
        /// Source file targeted by the request.
        /// </summary>
        public PhpSourceFile/*!*/ RequestFile
        {
            get
            {
                // called by PHP.Core.RequestHandler.ProcessRequest
                if (requestFile == null)
                {
                    requestFile = new PhpSourceFile(
                         new FullPath(HttpRuntime.AppDomainAppPath, false),
                         new FullPath(httpContext.Request.PhysicalPath, false)
                         );
                }

                return requestFile;
            }
        }
        private PhpSourceFile/*!*/ requestFile;


		/// <summary>
		/// Current HTTP context.
		/// </summary>
		public HttpContext/*!*/ HttpContext { get { return httpContext; } }
		private HttpContext/*!*/ httpContext;

		#endregion

		#region Request Processing

		/// <summary>
		/// Initializes the context.
		/// </summary>
		private void Initialize(ApplicationContext/*!*/ appContext)
		{
			Debug.Assert(appContext != null);

			defaultResponseEncoding = httpContext.Response.ContentEncoding;

			scriptContext = ScriptContext.InitWebRequest(appContext, httpContext);
			TrackClientDisconnection = !scriptContext.Config.RequestControl.IgnoreUserAbort;

			if (RequestBegin != null) RequestBegin();
		}

		/// <summary>
		/// Creates and initializes request and script contexts associated with the current thread.
		/// </summary>
		/// <param name="appContext">Application context.</param>
		/// <param name="context">Current HTTP context.</param>
		/// <returns>The initialized request context.</returns>
		/// <remarks>
		/// <para>
		/// Request context provides PHP with the web server environment.
		/// It should be initialized before any PHP code is invoked within web server and finalized (disposed)
		/// at the end of the request. This method can be called for multiple times, however it creates and 
		/// initializes a new request context only once per HTTP request.
		/// </para>
		/// <para>
		/// The following steps take place during the initialization (in this order):
		/// <list type="number">
		///   <term>Configuration is loaded (if not loaded yet).</term>
		///   <term>A new instance of <see cref="RequestContext"/> is created and bound to the current thread.</term>
		///   <term>A new instance of <see cref="ScriptContext"/> is created and initialized.</term>
		///   <term>Event <see cref="RequestBegin"/> is fired.</term>
		///   <term>Session is started if session auto-start confgiuration option is switched on.</term>
		/// </list>
		/// </para>
		/// <para>
		/// The request context can be accessed via the returned instance or via <see cref="CurrentContext"/>
		/// thread static field anytime between the initialization and disposal.
		/// </para>
		/// </remarks>
		public static RequestContext/*!*/ Initialize(ApplicationContext/*!*/ appContext, HttpContext/*!*/ context)
		{
			if (appContext == null)
				throw new ArgumentNullException("appContext");
			if (context == null)
				throw new ArgumentNullException("context");

			RequestContext req_context = currentContext;

			// already initialized within the current request:
			if (req_context != null && req_context.httpContext.Timestamp == context.Timestamp)
				return req_context;

			Debug.WriteLine("REQUEST", "-- started ----------------------");

			req_context = new RequestContext(context);
			currentContext = req_context;

			req_context.Initialize(appContext);

			return req_context;
		}


		/// <summary>
		/// Finalizes (disposes) the current request context, if there is any.
		/// </summary>
		public static void FinalizeContext()
		{
			RequestContext req_context = currentContext;
			if (req_context != null) req_context.Dispose();
		}

		#endregion

		#region Temporary Per-Request Files

		/// <summary>
		/// A list of temporary files which was created during the request and should be deleted at its end.
		/// </summary>
		private List<string>/*!*/TemporaryFiles
		{
			get
			{
                if (this._temporaryFiles == null)
                    this._temporaryFiles = new List<string>();

                return this._temporaryFiles;
			}
		}
        private List<string> _temporaryFiles;

		/// <summary>
		/// Silently deletes all temporary files.
		/// </summary>
		private void DeleteTemporaryFiles()
		{
            if (this._temporaryFiles != null)
			{
                for (int i = 0; i < this._temporaryFiles.Count; i++)
				{
                    try
                    {
                        File.Delete(this._temporaryFiles[i]);
                    }
                    catch { }
				}

                this._temporaryFiles = null;
			}
		}

		/// <summary>
		/// Adds temporary file to current handler's temp files list.
		/// </summary>
		/// <param name="path">A path to the file.</param>
		internal void AddTemporaryFile(string path)
		{
			Debug.Assert(path != null);
			this.TemporaryFiles.Add(path);
		}

		/// <summary>
		/// Checks whether the given filename is a path to a temporary file
		/// (for example created using the filet upload mechanism).
		/// </summary>
		/// <remarks>
		/// The stored paths are checked case-insensitively.
		/// </remarks>
		/// <exception cref="ArgumentNullException">Argument is a <B>null</B> reference.</exception>
		public bool IsTemporaryFile(string path)
		{
			if (path == null) throw new ArgumentNullException("path");
            return this._temporaryFiles != null && this._temporaryFiles.IndexOf(path, FullPath.StringComparer) >= 0;
		}

		/// <summary>
		/// Removes a file from a list of temporary files.
		/// </summary>
		/// <param name="path">A full path to the file.</param>
		/// <exception cref="ArgumentNullException">Argument is a <B>null</B> reference.</exception>
		public bool RemoveTemporaryFile(string path)
		{
			if (path == null) throw new ArgumentNullException("path");
            if (this._temporaryFiles == null)
                return false;

            var index = this._temporaryFiles.IndexOf(path, FullPath.StringComparer);
            if (index >= 0)
            {
                this._temporaryFiles.RemoveAt(index);
                return true;
            }
            else
            {
                return false;
            }
		}

		#endregion

		#region Connection

		#region Nested Class: Response Filter

		/// <summary>
		/// A filter installed on the response. All data sent to the client go through this filter.
		/// The filter checks whether the client is connected or not while flushing the data. 
		/// If the state changes from connected to disconnected then a callback specified in the ctor is invoked.
		/// </summary>
		private class ResponseFilter : Stream
		{
            private Action clientDisconnected;
			private HttpResponse response;

			public Stream Sink { get { return sink; } }
			private Stream sink;

            public ResponseFilter(HttpResponse response, Action clientDisconnected)
			{
				this.sink = response.Filter;
				this.response = response;
				this.clientDisconnected = clientDisconnected;
			}

			public override void Flush()
			{
				if (clientDisconnected != null && !response.IsClientConnected)
				{
					// throws ScriptDiedException:
					clientDisconnected();
				}
				sink.Flush();
			}

			#region Pass thru

			public override bool CanRead { get { return sink.CanRead; } }
			public override bool CanSeek { get { return sink.CanSeek; } }
			public override bool CanWrite { get { return sink.CanWrite; } }

			public override void Write(byte[] buffer, int offset, int count)
			{
				sink.Write(buffer, offset, count);
			}

			public override long Length
			{
				get
				{
					return sink.Length;
				}
			}

			public override long Position
			{
				get
				{
					return sink.Position;
				}
				set
				{
					sink.Position = value;
				}
			}

			public override int Read(byte[] buffer, int offset, int count)
			{
				return sink.Read(buffer, offset, count);
			}

			public override long Seek(long offset, SeekOrigin origin)
			{
				return sink.Seek(offset, origin);
			}

			public override void SetLength(long value)
			{
				sink.SetLength(value);
			}

			#endregion
		}

		#endregion

		/// <summary>
		/// Gets whether the connection has been aborted due to client disconnection.
		/// It doesn't check, however, whether the client is connected right now.
		/// </summary>
		public bool ConnectionAborted { get { return connectionAborted; } }
		bool connectionAborted;

		/// <summary>
		/// Enables tracking for client disconnecion.
		/// </summary>
		public bool TrackClientDisconnection
		{
			get
			{
				return responseFilter != null && httpContext.Response.Filter == responseFilter;
			}
			set
			{
				// ignores the value if connection has been aborted or filtering is not supported:
				if (value && !connectionAborted && httpContext.Response.Filter != null)
				{
					if (responseFilter == null)
                        responseFilter = new ResponseFilter(httpContext.Response, new Action(ClientDisconnected));

					httpContext.Response.Filter = responseFilter;
				}
				else
				{
					if (responseFilter != null)
						httpContext.Response.Filter = responseFilter.Sink;
				}
			}
		}
		private ResponseFilter responseFilter;

		private void ClientDisconnected()
		{
			// switch off tracking; 
			// if connection has been aborted then we needn't to track it any more: 
			TrackClientDisconnection = false;

			connectionAborted = true;

			throw new ScriptDiedException();
		}

		#endregion

		#region Session

		/// <summary>
		/// Whether a session has been started (i.e. session variables has been loaded).
		/// </summary>
		public SessionStates SessionState { get { return sessionState; } }
		private SessionStates sessionState = SessionStates.Closed;

        /// <summary>
		/// Gets whether a session exists (i.e. has been started or is being closed).
		/// </summary>
		public bool SessionExists
		{
			get { return sessionState == SessionStates.Started || sessionState == SessionStates.Closing; }
		}
        
		/// <summary>
		/// Ensures that Session ID is set, so calls to Flush() don't cause issues
		/// (if flush() is called, session ID can't be set because cookie can't be created).
		/// </summary>
        private void EnsureSessionId()
		{
            Debug.Assert(httpContext != null);
            if (httpContext.Session != null && httpContext.Session.IsNewSession && httpContext.Session.Count == 0)
            {
                // Ensure the internal method SessionStateModule.DelayedGetSessionId() is called now,
                // not after the request is processed if no one uses SessionId during the request.
                // Otherwise it causes an attempt to save the Session ID when the response stream was already flushed.
                var ensureId = httpContext.Session.SessionID;

                System.Diagnostics.Debug.WriteLine("SessionId: " + ensureId);
            }
		}

        /// <summary>
        /// Adds/update a SID global PHP constant.
        /// </summary>
        /// <remarks>The constant is non-empty only for cookie-less sessions.</remarks>
        public void UpdateSID()
        {
            Debug.Assert(httpContext.Session != null);

            scriptContext.Constants["SID", false] = (httpContext.Session.IsCookieless) ? String.Concat(AspNetSessionHandler.AspNetSessionName, "=", httpContext.Session.SessionID) : String.Empty;
        }

		/// <summary>
		/// Starts session if not already started. Loads session variables from <c>HttpContext.Session</c>.
		/// </summary>
		/// <para>
		/// Session state (<c>HttpContext.Session</c>) has to be available at the time of the call. 
		/// Otherwise, an exception occurs.
		/// </para>
		/// <para>
		/// Starting the session inheres in importing session variables from the session data store.
		/// The store is specific to the current PHP session handler 
		/// defined by configuration option <see cref="LocalConfiguration.SessionSection.Handler"/>.
		/// In the case the ASP.NET handler is active, values from <c>HttpContext.Session</c> are imported to
		/// <c>$_SESSION</c> PHP auto-global variable. Hence, items added to the <c>HttpContext.Session</c> by 
		/// non-PHP code after the start of the session will not be visible to PHP code. The <c>$_SESSION</c> variable
		/// has to be updated directly (see <c>ScriptContext.AutoGlobals</c>) to make these items visible to PHP.
		/// </para>
		/// <exception cref="SessionException">Session state not available.</exception>
		public void StartSession()
		{
			// checks and changes session state:
			if (disposed || sessionState != SessionStates.Closed) return;
			sessionState = SessionStates.Starting;

            if (httpContext.Session == null)
				throw new SessionException(CoreResources.GetString("session_state_unavailable"));

            EnsureSessionId();

			GlobalConfiguration global = Configuration.Global;
			PhpArray variables = null;

            // removes dummy item keeping the session alive:
            if (httpContext.Session[AspNetSessionHandler.PhpNetSessionVars] as string == AspNetSessionHandler.DummySessionItem)
                httpContext.Session.Remove(AspNetSessionHandler.PhpNetSessionVars);

			// loads an array of session variables using the current session handler:
			variables = scriptContext.Config.Session.Handler.Load(scriptContext, httpContext);

			// variables cannot be null:
			if (variables == null)
				variables = new PhpArray();

			// sets the auto-global variable (the previous content of $_SESSION array is discarded):
			PhpReference.SetValue(ref scriptContext.AutoGlobals.Session, variables);

			// copies session variables to $GLOBALS array if necessary:
			if (global.GlobalVariables.RegisterGlobals)
				scriptContext.RegisterSessionGlobals();

			// adds a SID constant:
            UpdateSID();

			sessionState = SessionStates.Started;
		}

		/// <summary>
		/// Ends session, i.e. stores content of the $_SESSION array to the <c>HttpContext.Session</c> collection.
		/// </summary>
		/// <param name="abandon">Whether to abandon the session without persisting variables.</param>
		/// <exception cref="SessionException">Session state not available.</exception>
		public void EndSession(bool abandon)
		{
			// checks and changes session state:
			if (disposed || sessionState != SessionStates.Started) return;
			sessionState = SessionStates.Closing;

			if (httpContext.Session == null)
				throw new SessionException(CoreResources.GetString("session_state_unavailable"));

			GlobalConfiguration global = Configuration.Global;

			PhpArray variables = PhpReference.AsPhpArray(scriptContext.AutoGlobals.Session);
			if (variables == null) variables = new PhpArray();

			try
			{
				if (!abandon)
					scriptContext.Config.Session.Handler.Persist(variables, scriptContext, httpContext);
				else
					scriptContext.Config.Session.Handler.Abandoning(scriptContext, httpContext);
			}
			finally
			{
				if (!abandon)
				{
                    // if ASP.NET session state is empty then adds a dump item to preserve the session:
                    if (httpContext.Session.Count == 0)
                        httpContext.Session.Add(AspNetSessionHandler.PhpNetSessionVars, AspNetSessionHandler.DummySessionItem);
				}
				else
				{
					// abandons ASP.NET session:
					httpContext.Session.Abandon();
				}

				sessionState = SessionStates.Closed;
			}
		}

		/// <summary>
		/// Gets or sets a lifetime of the session cookie. 
		/// Cookie expiration is updated after the request using this value.
		/// Non-positive value means infinite.
		/// </summary>
		public int SessionCookieLifetime
		{
			get
			{
				HttpCookie cookie = AspNetSessionHandler.GetCookie(httpContext);

				if (cookie != null && cookie.Expires != DateTime.MinValue)
				{
					// expiration time has been set when the request has been processed by ASP.NET server;
					// that shouldn't take more than half a minute so the precision is enought:
					TimeSpan span = cookie.Expires - httpContext.Timestamp;
					return (span.Minutes < 0) ? 0 : span.Minutes;
				}
				else
				{
					return 0;
				}
			}
			set
			{
				sessionCookieLifetime = value;
				sessionCookieLifetimeSet = true;
			}
		}
		private int sessionCookieLifetime;
		private bool sessionCookieLifetimeSet = false;

		/// <summary>
		/// Updates the session cookie expiration time using <see cref="SessionCookieLifetime"/> field.
		/// Called at the end of the request.
		/// </summary>
		private void UpdateSessionCookieExpiration()
		{
			if (sessionCookieLifetimeSet)
			{
				HttpCookie cookie = AspNetSessionHandler.GetCookie(httpContext);
				if (cookie != null)
				{
					cookie.Expires = (sessionCookieLifetime <= 0) ? DateTime.MinValue : DateTime.Now.AddMinutes(sessionCookieLifetime);
				}
			}
		}

		#endregion

		#region Cleanup

		void TryDisposeBeforeFinalization()
		{
			scriptContext.GuardedCall<object,object>((_) => { EndSession(false); return null; }, null, false);
		}

		void TryDisposeAfterFinalization()
		{
			// flushes headers (if it wasn't done earlier)
            scriptContext.Headers.Flush(HttpContext);
		}

		void FinallyDispose()
		{
			DeleteTemporaryFiles();
			Externals.EndRequest();

			// updates session cookie expiration stamp:
			UpdateSessionCookieExpiration();

			this.httpContext = null;
			currentContext = null;
		}

		#endregion

		#region Compilation

		internal MultiScriptAssembly GetPrecompiledAssembly()
		{
            return scriptContext.ApplicationContext.RuntimeCompilerManager.GetPrecompiledAssembly();
		}

        /// <summary>
        /// Get the precompiled script from several locations - script library database, precompiled SSA, precompiled MSA (WebPages.dll).
        /// </summary>
        /// <param name="sourceFile">The source file of the script to retrieve.</param>
        /// <returns>The <see cref="ScriptInfo"/> of required script or null if such script cannot be obtained.</returns>
		internal ScriptInfo GetCompiledScript(PhpSourceFile/*!*/ sourceFile)
		{
            return scriptContext.ApplicationContext.RuntimeCompilerManager.GetCompiledScript(sourceFile, this);
		}

		#endregion
	}
}
