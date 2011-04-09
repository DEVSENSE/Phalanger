/*

 Copyright (c) 2004-2006 Tomas Matousek.  

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

	TODO:
		- Added a check for special characters in the session name. (PHP 5.1.3) 
        - Deprecated session_register(), session_unregister() and session_is_registered().
*/

using System;
using System.IO;
using System.Web;
using System.Web.SessionState;
using System.Collections;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Globalization;

using PHP.Core;
using PHP.Core.Reflection;

namespace PHP.Library
{
	#region PhpSessionHandlerBase

	/// <summary>
	/// A base class for PHP session handlers.
	/// </summary>
	public abstract class PhpSessionHandlerBase : SessionHandler
	{
		protected const string FilePrefix = "sess_";

		/// <summary>
		/// Loads serialized variables.
		/// </summary>
		/// <param name="savePath">A path where session files can be stored in.</param>
		/// <param name="sid">A session ID.</param>
		/// <returns>Variables in serialized form.</returns>
		protected abstract PhpBytes LoadSerializedVariables(string savePath, string sid);

		/// <summary>
		/// Stores serialized variables.
		/// </summary>
		/// <param name="savePath">A path where session files can be stored in.</param>
		/// <param name="sid">A session ID.</param>
		/// <param name="data">Variables in serialized form.</param>
		protected abstract void SaveSerializedVariables(string savePath, string sid, PhpBytes data);

		/// <summary>
		/// Collects old session data.
		/// </summary>
		/// <param name="savePath">A path where session files can be stored in.</param>
		/// <param name="sid">A session ID.</param>
		/// <param name="lifetime">A data lifetime in seconds.</param>
		protected abstract void Collect(string savePath, string sid, int lifetime);

		/// <summary>
		/// Loads session variables from persistent storage.
		/// </summary>
		/// <param name="context">The current script context.</param>
		/// <param name="httpContext">The current HTTP context.</param>
		/// <returns>An array of session variables. A <B>null</B> reference on error.</returns>
		protected sealed override PhpArray Load(ScriptContext context, HttpContext httpContext)
		{
			string sid = httpContext.Session.SessionID;
			LibraryConfiguration config = LibraryConfiguration.GetLocal(context);

			PhpBytes bytes = LoadSerializedVariables(config.Session.SavePath, sid);

			// deserialization:
			PhpArray result = null;
			if (bytes != null && bytes.Data.Length != 0)
			{
                PhpReference php_ref = PhpVariables.Unserialize(null/*Load method is not called from any class context*/, bytes);
				result = (php_ref != null) ? php_ref.Value as PhpArray : null;
			}

			// collection:
			if (DoCollection(config))
				Collect(config.Session.SavePath, sid, config.Session.GcMaxLifetime);

			return result;
		}

		/// <summary>
		/// Persists session variables to a file.
		/// </summary>
		/// <param name="variables">Variables to persist.</param>
		/// <param name="context">The current script context.</param>
		/// <param name="httpContext">The current HTTP context.</param>
		protected sealed override void Persist(PhpArray variables, ScriptContext context, HttpContext httpContext)
		{
			string sid = httpContext.Session.SessionID;
			LibraryConfiguration config = LibraryConfiguration.GetLocal(context);

			PhpBytes data = PhpVariables.Serialize(null/*Persist method is not called from any class context*/, variables);

			SaveSerializedVariables(config.Session.SavePath, sid, data);
		}

		/// <summary>
		/// Decides whether to perform collection or not.
		/// </summary>
		private static bool DoCollection(LibraryConfiguration config)
		{
			if (config.Session.GcProbability <= 0) return false;

			double rand = (double)config.Session.GcDivisor * PhpMath.Generator.NextDouble();
			return rand < config.Session.GcProbability;
		}

		/// <summary>
		/// Gets a name of the session file without path.
		/// </summary>
		protected static string GetSessionFileName(string sid)
		{
			return FilePrefix + sid;
		}

		/// <summary>
		/// Gets a full path to the session file.
		/// </summary>
		protected static string GetSessionFilePath(string savePath, string sid)
		{
			try
			{
				return Path.Combine(savePath, GetSessionFileName(sid));
			}
			catch (ArgumentException)
			{
				PhpException.Throw(PhpError.Warning, LibResources.GetString("invalid_session_save_path", savePath));
				return null;
			}
		}
	}

	#endregion

	#region PhpSessionHandler

	/// <summary>
	/// Handles PHP file backed sessions.
	/// </summary>
	public sealed class PhpSessionHandler : PhpSessionHandlerBase
	{
		private PhpSessionHandler() { }

		/// <summary>
		/// Singleton instance.
		/// </summary>
		public static readonly PhpSessionHandler Default = new PhpSessionHandler();

		/// <summary>
		/// Gets name of the handler used in configuration.
		/// </summary>
		public override string Name
		{
			get
			{
				return "files";
			}
		}

		/// <summary>
		/// Loads session data from the session file.
		/// </summary>
		protected override PhpBytes LoadSerializedVariables(string savePath, string sid)
		{
			PhpBytes result = null;

			using (PhpStream file = OpenSessionFile(savePath, sid, false))
			{
				if (file != null)
				{
					result = file.ReadMaximumBytes();
					file.Close();
				}
			}

			return result;
		}

		/// <summary>
		/// Stores serialied variables to the session file.
		/// </summary>
		protected override void SaveSerializedVariables(string savePath, string sid, PhpBytes data)
		{
			using (PhpStream file = OpenSessionFile(savePath, sid, true))
			{
				if (file != null)
				{
					file.WriteBytes(data);
					file.Close();
				}
			}
		}

		/// <summary>
		/// Deletes session files older than <paramref name="lifetime"/>.
		/// </summary>
		protected override void Collect(string savePath, string sid, int lifetime)
		{
			using (PhpResource dir = PhpDirectory.Open(savePath))
			{
				if (dir == null) return;

				int threshold = DateTimeUtils.UtcToUnixTimeStamp(DateTime.Now.ToUniversalTime().AddSeconds(-lifetime));

				string file_name;
				while ((file_name = PhpDirectory.Read(dir)) != null)
				{
					if (file_name.Length >= FilePrefix.Length && file_name.Substring(0, FilePrefix.Length) == FilePrefix)
					{
						string full_path = Path.Combine(savePath, file_name);
						int time = PhpFile.GetAccessTime(full_path);

						if (time < threshold)
						{
							Debug.WriteLine(String.Format("Collecting file {0} atime: {1} threshold: {2}",
							  full_path,
							  DateTimeUtils.UnixTimeStampToUtc(time).ToLongTimeString(),
							  DateTimeUtils.UnixTimeStampToUtc(threshold).ToLongTimeString()),
							  "PhpSessionHandler");

							PhpFile.Delete(full_path);
						}
					}
				}
			}
		}

		/// <summary>
		/// Called immediately before the session is abandoned.
		/// </summary>
		/// <param name="context">A current script context.</param>
		/// <param name="httpContext">A current HTTP context.</param>
		protected override void Abandoning(ScriptContext context, HttpContext httpContext)
		{
			LibraryConfiguration config = LibraryConfiguration.GetLocal(context);

			string file_name = GetSessionFilePath(config.Session.SavePath, httpContext.Session.SessionID);
			Debug.WriteLine("Abandoning file " + file_name, "PhpSessionHandler.CollectOldFiles");
			PhpFile.Delete(file_name);
		}

		/// <summary>
		/// Opens a session file for reading or writing.
		/// </summary>
		/// <param name="savePath">A save path in the configuration.</param>
		/// <param name="sid">The SID.</param>
		/// <param name="write">Whether to open the file for writing.</param>
		private static PhpStream OpenSessionFile(string savePath, string sid, bool write)
		{
			string file_path = GetSessionFilePath(savePath, sid);

			if (file_path != null)
			{
				if (write)
				{
					Debug.WriteLine("Write open file " + file_path, "PhpSessionHandler");
					return PhpStream.Open(file_path, "wb", StreamOpenOptions.Empty, StreamContext.Default);
				}
				else if (PhpFile.Exists(file_path))
				{
					Debug.WriteLine("Read open file " + file_path, "PhpSessionHandler");
					return PhpStream.Open(file_path, "rb", StreamOpenOptions.Empty, StreamContext.Default);
				}
			}
			return null;
		}

		#region Unit Testing
#if DEBUG

		public static void Test_CollectOldFiles()
		{
			string path = Path.Combine(Path.GetTempPath(), "Session");
			System.IO.Directory.CreateDirectory(path);
			const int lifetime = 200;
			const int count = 20;

			//      System.Diagnostics.Debug.Listeners.Clear();
			//      System.Diagnostics.Debug.Listeners.Add(new TextWriterTraceListener(Console.Out));

			for (int i = 0; i < count; i++)
			{
				string file = Path.Combine(path, "sess_" + i);
				Console.WriteLine("create file '{0}'", file);
				File.CreateText(file).Close();
				File.SetLastAccessTime(file, DateTime.Now.AddSeconds(-lifetime + count / 2 - i));
			}

			Console.WriteLine("collecting...");
			Default.Collect(path, "", lifetime);
			Console.WriteLine("done.");
		}

#endif
		#endregion
	}

	#endregion

	#region PhpUserSessionHandler

	/// <summary>
	/// Handles PHP sessions via user handlers.
	/// </summary>
	public sealed class PhpUserSessionHandler : PhpSessionHandlerBase
	{
		private PhpUserSessionHandler() { }

		#region Nested class: User Handlers

		/// <summary>
		/// A set of session callbacks. 
		/// </summary>
		internal class Handlers
		{
			public PhpCallback Open;
			public PhpCallback Close;
			public PhpCallback Read;
			public PhpCallback Write;
			public PhpCallback Destroy;
			public PhpCallback Collect;

			/// <summary>
			/// Clears thread static field. Called on request end.
			/// </summary>
			private static void Clear()
			{
				_current = null;
			}

			/// <summary>
			/// Registeres <see cref="Clear"/> called on request end.
			/// </summary>
			static Handlers()
			{
                RequestContext.RequestEnd += new Action(Clear);
			}

			/// <summary>
			/// Gets the current set of handlers.
			/// </summary>
			internal static Handlers Current
			{
				get
				{
					if (_current == null) _current = new Handlers();
					return _current;
				}
			}
			[ThreadStatic]
			private static Handlers _current = null;
		}

		#endregion

		/// <summary>
		/// Singleton instance.
		/// </summary>
		public static readonly PhpUserSessionHandler Default = new PhpUserSessionHandler();

		/// <summary>
		/// Gets name of the handler used in configuration.
		/// </summary>
		public override string Name
		{
			get
			{
				return "user";
			}
		}

		/// <summary>
		/// Calls "open" and "read" user handlers if not empty.
		/// </summary>
		protected override PhpBytes LoadSerializedVariables(string savePath, string sid)
		{
			Handlers handlers = Handlers.Current;
			PhpBytes result = null;

			if (handlers.Open != null)
			{
				if (!Core.Convert.ObjectToBoolean(handlers.Open.Invoke(savePath, GetSessionFileName(sid))))
				{
					ReportError("open", savePath, sid);
					return null;
				}
			}

			if (handlers.Read != null)
			{
				result = Core.Convert.ObjectToPhpBytes(handlers.Read.Invoke(sid));

				// error (empty string of bytes):
				if (result != null && result.Data.Length == 0)
				{
					ReportError("read", savePath, sid);
					return null;
				}
			}

			return result;
		}

		/// <summary>
		/// Calls "write" and "close" user handlers if not empty.
		/// </summary>
		protected override void SaveSerializedVariables(string savePath, string sid, PhpBytes data)
		{
			Handlers handlers = Handlers.Current;

			if (handlers.Write != null && !Core.Convert.ObjectToBoolean(handlers.Write.Invoke(sid, data)))
			{
				ReportError("write", savePath, sid);
				return;
			}

			if (handlers.Close != null && !Core.Convert.ObjectToBoolean(handlers.Close.Invoke()))
			{
				ReportError("close", savePath, sid);
				return;
			}
		}

		/// <summary>
		/// Calls "gc" user handler if not empty.
		/// </summary>
		protected override void Collect(string savePath, string sid, int lifetime)
		{
			Handlers handlers = Handlers.Current;

			if (handlers.Collect != null)
				if (!Core.Convert.ObjectToBoolean(handlers.Collect.Invoke(lifetime)))
					ReportError("gc", savePath, sid);
		}

		/// <summary>
		/// Calls "destroy" user handler if not empty.
		/// </summary>
		protected override void Abandoning(ScriptContext context, HttpContext httpContext)
		{
			string sid = httpContext.Session.SessionID;
			LibraryConfiguration config = LibraryConfiguration.GetLocal(context);
			Handlers handlers = Handlers.Current;

			if (handlers.Destroy != null)
				if (!Core.Convert.ObjectToBoolean(handlers.Destroy.Invoke(sid)))
					ReportError("destroy", config.Session.SavePath, sid);
		}

		/// <summary>
		/// Reports an error when the user handler has failed.
		/// </summary>
		private void ReportError(string operation, string savePath, string sid)
		{
			PhpException.Throw(PhpError.Warning, LibResources.GetString("user_session_handler_failed",
			  operation, sid, savePath));
		}
	}

	#endregion

	/// <summary>
	/// PHP session handling functions.
	/// </summary>
	/// <threadsafety static="true"/>
	[ImplementsExtension(LibraryDescriptor.ExtSession)]
	public static class PhpSession
	{
		#region GSRs

		/// <summary>
		/// Default value for "session.cache_expire" PHP configuration option.
		/// </summary>
		public const int DefaultCacheExpire = 180;

		/// <summary>
		/// Default value for "session.cache_limiter" PHP configuration option.
		/// </summary>
		public const string DefaultCacheLimiter = "public";

		/// <summary>
		/// Default value for "session.cookie_lifetime" PHP configuration option.
		/// </summary>
		public const int DefaultCookieLifetime = 0;

		/// <summary>
		/// Default value for "session.cookie_path" PHP configuration option.
		/// </summary>
		public const string DefaultCookiePath = "/";

		/// <summary>
		/// Default value for "session.cookie_domain" PHP configuration option.
		/// </summary>
		public const string DefaultCookieDomain = null;

		/// <summary>
		/// Default value for "session.cookie_secure" PHP configuration option.
		/// </summary>
		public const bool DefaultCookieSecure = false;

		/// <summary>
		/// GSR routine for "session.serialize_handler" configuration option.
		/// </summary>
		internal static object GsrSerializer(LibraryConfiguration/*!*/ local, LibraryConfiguration/*!*/ @default, object value, IniAction action)
		{
			string result = local.Session.Serializer.Name;

			switch (action)
			{
				case IniAction.Set:
					{
						string name = Core.Convert.ObjectToString(value);
						Serializer serializer = Serializers.GetSerializer(name);

						if (serializer == null)
						{
							PhpException.Throw(PhpError.Warning, LibResources.GetString("unknown_serializer", name));
						}
						else
						{
							local.Session.Serializer = serializer;
						}
						break;
					}

				case IniAction.Restore:
					local.Session.Serializer = @default.Session.Serializer;
					break;
			}

			return result;
		}

		/// <summary>
		/// Gets, sets or restores "session.save_handler" option.
		/// </summary>
		internal static object GsrHandler(LocalConfiguration local, LocalConfiguration @default, object value, IniAction action)
		{
			string result = local.Session.Handler.Name;

			switch (action)
			{
				case IniAction.Set:
					{
						string name = Core.Convert.ObjectToString(value);
						SessionHandler handler = SessionHandlers.GetHandler(name);

						if (handler == null)
						{
							PhpException.Throw(PhpError.Warning,
							  PhpException.ToErrorMessage(CoreResources.GetString("unknown_session_handler", name)));
						}
						else
						{
							local.Session.Handler = handler;
						}

						break;
					}
				case IniAction.Restore:
					local.Session.Handler = @default.Session.Handler;
					break;
			}

			return result;
		}

		/// <summary>
		/// GSR routine used for configuration.
		/// </summary>
		internal static object GsrCacheExpire(object value, IniAction action)
		{
			switch (action)
			{
				case IniAction.Get: return CacheExpire();
				case IniAction.Set: return CacheExpire(Core.Convert.ObjectToInteger(value));
				case IniAction.Restore: CacheExpire(PhpSession.DefaultCacheExpire); return null;
			}
			return null;
		}

		/// <summary>
		/// GSR routine used for configuration.
		/// </summary>
		internal static object GsrCacheLimiter(object value, IniAction action)
		{
			switch (action)
			{
				case IniAction.Get: return CacheLimiter();
				case IniAction.Set: return CacheLimiter(Core.Convert.ObjectToString(value));
				case IniAction.Restore: CacheLimiter(PhpSession.DefaultCacheLimiter); return null;
			}
			return null;
		}

		/// <summary>
		/// GSR routine used for configuration.
		/// </summary>
		internal static object GsrCookieLifetime(object value, IniAction action)
		{
			RequestContext context;
			HttpCookie cookie;
			if (!GetCookie(out cookie, out context)) return DefaultCookieLifetime;

			int result = context.SessionCookieLifetime;
			switch (action)
			{
				case IniAction.Set:
					context.SessionCookieLifetime = Core.Convert.ObjectToInteger(value);
					break;

				case IniAction.Restore:
					context.SessionCookieLifetime = DefaultCookieLifetime;
					break;
			}
			return result;
		}

		/// <summary>
		/// GSR routine used for configuration.
		/// </summary>
		internal static object GsrCookieSecure(object value, IniAction action)
		{
			RequestContext context;
			HttpCookie cookie;
			if (!GetCookie(out cookie, out context)) return DefaultCookieSecure;

			bool result = cookie.Secure;
			switch (action)
			{
				case IniAction.Set:
					cookie.Secure = PhpIni.OptionValueToBoolean(value);
					break;

				case IniAction.Restore:
					cookie.Secure = DefaultCookieSecure;
					break;
			}
			return result;
		}

		/// <summary>
		/// GSR routine used for configuration.
		/// </summary>
		internal static object GsrCookiePath(object value, IniAction action)
		{
			RequestContext context;
			HttpCookie cookie;
			if (!GetCookie(out cookie, out context)) return DefaultCookiePath;

			string result = cookie.Path;
			switch (action)
			{
				case IniAction.Set:
					cookie.Path = Core.Convert.ObjectToString(value);
					break;

				case IniAction.Restore:
					cookie.Path = DefaultCookiePath;
					break;
			}
			return result;
		}

		/// <summary>
		/// GSR routine used for configuration.
		/// </summary>
		internal static object GsrCookieDomain(object value, IniAction action)
		{
			RequestContext context;
			HttpCookie cookie;
			if (!GetCookie(out cookie, out context)) return DefaultCookieDomain;

			string result = cookie.Domain;
			switch (action)
			{
				case IniAction.Set:
					cookie.Domain = Core.Convert.ObjectToString(value);
					break;

				case IniAction.Restore:
					cookie.Domain = DefaultCookieDomain;
					break;
			}
			return result;
		}

		#endregion

        #region SessionId

        internal class SessionId
        {
            /// <summary>
            /// Singleton SessionIDManager instance.
            /// </summary>
            internal static SessionIDManager Manager
            {
                get
                {
                    if (manager == null)
                    {
                        var newManager = new SessionIDManager();
                        newManager.Initialize();

                        return (manager = newManager);
                    }

                    return manager;
                }
            }
            private static SessionIDManager manager = null;

            /// <summary>
            /// Set new SessionId string. Resets the Session object.
            /// </summary>
            /// <param name="request_context">Current RequestContext. Cannot be null.</param>
            /// <param name="session_id">New SessionId string.</param>
            internal static void SetNewSessionId(RequestContext/*!*/request_context, string session_id)
            {
                Debug.Assert(request_context != null);

                // currently this method does not work properly with ASP.NET handler, because
                // there is already created InProcDataStore associated with old SessionId,
                // this old SessionId is saved in private field SessionStateModule._rqId (and others)
                // and an attempt to store the data at the end of the request by ASP runtime with
                // new SessionId will silently fail (probably).
                //
                // Need to implement own SessionStateModule ?
                Debug.Assert(request_context.ScriptContext.Config.Session.Handler.Name != AspNetSessionHandler.Default.Name);


                if (!Manager.Validate(session_id))
                    throw new ArgumentException(null, "session_id");

                var session = request_context.HttpContext.Session;

                //var x = HttpRuntime.CacheInternal.Get("j" + session.SessionID);

                // drop previous HttpContext.Session
                System.Web.SessionState.SessionStateUtility.RemoveHttpSessionStateFromContext(
                    request_context.HttpContext);

                // assign new HttpContext.Session
                System.Web.SessionState.SessionStateUtility.AddHttpSessionStateToContext(
                    request_context.HttpContext,
                    new System.Web.SessionState.HttpSessionStateContainer(
                        session_id,
                        CloneSessionStateCollection(session),   // in PHP, session variables are not cleaned when new SessionId has been set
                        session.StaticObjects,// new HttpStaticObjectsCollection(),
                        session.Timeout,
                        true,
                        session.CookieMode,
                        session.Mode,
                        false)
                    );

                // save session id cookie, update SID

                //request_context.HttpContext.Response.Cookies[]
                SessionId.Manager.RemoveSessionID(request_context.HttpContext);
                bool redirected, cookieAdded;
                SessionId.Manager.SaveSessionID(request_context.HttpContext, session_id, out redirected, out cookieAdded);

                // set new SID constant
                RequestContext.UpdateSID(request_context.ScriptContext, request_context.HttpContext);
            }

            private static ISessionStateItemCollection CloneSessionStateCollection(HttpSessionState session)
            {
                var collection = new System.Web.SessionState.SessionStateItemCollection();

                for (int i = 0; i < session.Count; ++i)
                    collection[session.Keys[i]] = session[i];

                return collection;
            }
        }

        #endregion

		#region session_start, session_destroy, session_write_close, session_commit

		/// <summary>
		/// Starts session. Loads session variables to <c>$_SESSION</c> and optionally to <c>$GLOBALS</c> arrays.
		/// </summary>
		/// <returns><B>true</B> on success.</returns>
		/// <exception cref="PhpException">Web server variables are not available (Warning).</exception>
		[ImplementsFunction("session_start")]
		public static bool Start()
		{
			RequestContext request_context;
			if (!Web.EnsureRequestContext(out request_context)) return false;

			request_context.StartSession();
			return true;
		}

		/// <summary>
		/// Closes session and deletes all the data associated with it.
		/// </summary>
		/// <returns><B>true</B> on success.</returns>
		[ImplementsFunction("session_destroy")]
		public static bool Destroy()
		{
			RequestContext request_context;
			if (!Web.EnsureRequestContext(out request_context)) return false;

			request_context.EndSession(true);
			return true;
		}

		/// <summary>
		/// Persists session data and closes the session. See <see cref="Commit"/> for details.
        /// No value is returned.
		/// </summary>
		[ImplementsFunction("session_write_close")]
		public static void WriteClose()
		{
			Commit();
		}

		/// <summary>
		/// Persists session data and closes the session.
		/// </summary>
		[ImplementsFunction("session_commit")]
		public static void Commit()
		{
			RequestContext request_context;
			if (!Web.EnsureRequestContext(out request_context)) return;

			request_context.EndSession(false);
		}

		#endregion

		#region session_register, session_is_registered, session_unregister, session_unset

		[ImplementsFunction("session_register")]
		public static bool RegisterVariable(params object[] names)
		{
			if (names == null)
			{
				PhpException.ArgumentNull("names");
				return false;
			}

			RequestContext request_context;
			if (!Web.EnsureRequestContext(out request_context)) return false;

			// starts session if not started yet:
			if (request_context.SessionState == SessionStates.Closed)
				request_context.StartSession();

			ScriptContext context = request_context.ScriptContext;

			// gets $GLOBALS array if exists:
			PhpArray globals = context.GlobalVariables;

			// gets $_SESSION array (creates a new one if not exists):
			PhpArray session = context.SessionVariables;

			PhpReference reference;
			bool result = true;

			// sets $_SESSION items using $GLOBALS array:
			for (int i = 0; i < names.Length; i++)
			{
				string name;
				PhpArray array;

				if ((array = names[i] as PhpArray) != null)
				{
					// recursively searches for string variable names:
					using (PhpHashtable.RecursiveEnumerator iterator = array.GetRecursiveEnumerator())
					{
						while (iterator.MoveNext())
						{
							name = PHP.Core.Convert.ObjectToString(iterator.Current.Value);
							reference = globals.GetArrayItemRef(iterator.Current.Value);

							// skips resources:
							if (!(reference.value is PhpResource)) session[name] = reference; else result = false;
						}
					}
				}
				else
				{
					name = PHP.Core.Convert.ObjectToString(names[i]);
					reference = globals.GetArrayItemRef(names[i]);

					// skips resources:
					if (!(reference.value is PhpResource)) session[name] = reference; else result = false;
				}
			}
			return result;
		}

		[ImplementsFunction("session_is_registered")]
		public static bool IsVariableRegistered(string sessionName)
		{
			PhpArray session = PhpReference.AsPhpArray(ScriptContext.CurrentContext.AutoGlobals.Session);
			return (session != null) && session.ContainsKey(sessionName);
		}

		[ImplementsFunction("session_unregister")]
		public static bool UnregisterVariable(string name)
		{
			PhpArray session = PhpReference.AsPhpArray(ScriptContext.CurrentContext.AutoGlobals.Session);
			if (session == null) return false;

			bool result = session.ContainsKey(name);
			session.Remove(name);
			return result;
		}

		[ImplementsFunction("session_unset")]
		public static void UnsetVariable()
		{
			PhpArray session = PhpReference.AsPhpArray(ScriptContext.CurrentContext.AutoGlobals.Session);
			if (session != null)
				session.Clear();
		}

		#endregion

		#region session_cache_expire, session_cache_limiter

		/// <summary>
		/// Gets a session cache expiration timeout.
		/// </summary>
		/// <returns>The timeout in minutes.</returns>
		[ImplementsFunction("session_cache_expire")]
		public static int CacheExpire()
		{
			HttpContext http_context;
			if (!Web.EnsureHttpContext(out http_context)) return 0;

			return http_context.Response.Expires;
		}

		/// <summary>
		/// Sets a session cache expiration timeout.
		/// </summary>
		/// <param name="newValue">A new value (in minutes).</param>
		/// <returns>An old value (in minutes).</returns>
		/// <exception cref="PhpException"><paramref name="newValue"/> is not positive. (Warning)</exception>
		[ImplementsFunction("session_cache_expire")]
		public static int CacheExpire(int newValue)
		{
			HttpContext http_context;
			if (!Web.EnsureHttpContext(out http_context)) return 0;

			int result = http_context.Response.Expires;

			if (newValue > 0)
			{
				http_context.Response.Expires = newValue;
			}
			else
			{
				PhpException.InvalidArgument("newValue", LibResources.GetString("arg:negative_or_zero"));
			}

			return result;
		}

		/// <summary>
		/// Gets a current value of cache control limiter.
		/// </summary>
		/// <returns>The cache control limiter ("private", "public", "no-cache").</returns>
		[ImplementsFunction("session_cache_limiter")]
		public static string CacheLimiter()
		{
			HttpContext http_context;
			if (!Web.EnsureHttpContext(out http_context)) return null;

			return http_context.Response.CacheControl;
		}

		/// <summary>
		/// Sets cache control limiter.
		/// </summary>
		/// <param name="newLimiter">
		/// A new value - should be one of "private", "private_no_expire", "public", "nocache", or "no-cache".
		/// Letter case is ignored. In PHP the value can contain other colon-separated values.
		/// </param>
		/// <returns>An old value ("private", "public", or "no-cache").</returns>
		/// <exception cref="PhpException"><paramref name="newLimiter"/> has invalid value. (Notice)</exception>
		[ImplementsFunction("session_cache_limiter")]
		public static string CacheLimiter(string newLimiter)
		{
			HttpContext http_context;
			if (!Web.EnsureHttpContext(out http_context)) return null;

            string result = http_context.Response.CacheControl;

            if (string.IsNullOrEmpty(newLimiter))
                return result;

            if (newLimiter.IndexOf(',') < 0)
            {
                CacheLimiterInternal(http_context, newLimiter);
            }
            else
            {
                foreach (var singleLimiter in newLimiter.Split(','))
                    CacheLimiterInternal(http_context, singleLimiter);
            }

			return result;
		}

        /// <summary>
        /// Updates the cache control of the given HttpContext.
        /// </summary>
        /// <param name="http_context">Current HttpContext instance.</param>
        /// <param name="singleLimiter">Cache limiter passed to the session_cache_limiter() PHP function.</param>
        private static void CacheLimiterInternal(HttpContext http_context, string/*!*/singleLimiter)
        {
            Debug.Assert(singleLimiter != null);

            singleLimiter = singleLimiter.Trim();

            if (singleLimiter.Length == 0)
                return;

            var compareInfo = CultureInfo.CurrentCulture.CompareInfo;

            if (compareInfo.Compare(singleLimiter, "private", CompareOptions.IgnoreCase) == 0)
                http_context.Response.CacheControl = "private";
            else if (compareInfo.Compare(singleLimiter, "public", CompareOptions.IgnoreCase) == 0)
                http_context.Response.CacheControl = "public";
            else if (compareInfo.Compare(singleLimiter, "no-cache", CompareOptions.IgnoreCase) == 0)
                http_context.Response.CacheControl = "no-cache";
            else if (compareInfo.Compare(singleLimiter, "private_no_expire", CompareOptions.IgnoreCase) == 0)
                http_context.Response.CacheControl = "private";
            else if (compareInfo.Compare(singleLimiter, "nocache", CompareOptions.IgnoreCase) == 0)
                http_context.Response.CacheControl = "no-cache";
            else if (compareInfo.Compare(singleLimiter, "must-revalidate", CompareOptions.IgnoreCase) == 0)
                http_context.Response.Cache.SetRevalidation(HttpCacheRevalidation.AllCaches);
            else
                PhpException.Throw(PhpError.Notice, LibResources.GetString("invalid_cache_limiter", singleLimiter));
        }

		#endregion

		#region session_save_path, session_name, session_id, (NS) session_regenerate_id

		/// <summary>
		/// Gets a path where sessions are stored.
		/// </summary>
		[ImplementsFunction("session_save_path")]
		public static string SavePath()
		{
			HttpContext context;
			if (!Web.EnsureHttpContext(out context)) return null;

			return LibraryConfiguration.Local.Session.SavePath;
		}

		/// <summary>
		/// Sets a path where sessions are stored (see 'session.save_path' configuration option).
		/// </summary>
		/// <param name="newPath">The new path to set.</param>
		/// <remarks>A previous value of the path.</remarks>
		[ImplementsFunction("session_save_path")]
		public static string SavePath(string newPath)
		{
			HttpContext context;
			if (!Web.EnsureHttpContext(out context)) return null;

			string result = LibraryConfiguration.Local.Session.SavePath;
			LibraryConfiguration.Local.Session.SavePath = newPath;
			return result;
		}

		/// <summary>
		/// Gets the current session name. 
		/// </summary>
		/// <returns>A session name (<c>"ASP.NET_SessionId"</c>).</returns>
		[ImplementsFunction("session_name")]
		public static string Name()
		{
			HttpContext context;
			if (!Web.EnsureHttpContext(out context)) return null;

			return AspNetSessionHandler.AspNetSessionName;
		}

		/// <summary>
		/// Sets the current session name. Not supported. Doesn't change session name. 
		/// </summary>
		/// <param name="newName">A new name.</param>
		/// <returns>An old name.</returns>
		[ImplementsFunction("session_name")]
		public static string Name(string newName)
		{
			HttpContext context;
			if (!Web.EnsureHttpContext(out context)) return null;

			PhpException.FunctionNotSupported(PhpError.Notice);
			return AspNetSessionHandler.AspNetSessionName;
		}

		/// <summary>
		/// Gets the current session id.
		/// </summary>
		/// <returns>The session id.</returns>
		[ImplementsFunction("session_id")]
		public static string Id()
		{
			RequestContext request_context;
			if (!Web.EnsureRequestContext(out request_context)) return null;

            // in Phalanger:
            // id is returned only if sessions are active

            return (request_context.SessionState == SessionStates.Started) ?
              request_context.HttpContext.Session.SessionID : String.Empty;

            // in PHP:
            // id is initially ""
            // session_id() returns current SessionId even if session was not started yet
            // id is cleared after destroying the session
            
            // return request_context.HttpContext.Session.SessionID;
		}

		/// <summary>
		/// Changes session id. Not supported. Doesn't change session id. 
		/// </summary>
		/// <param name="id">A new id value.</param>
		/// <returns>A session id.</returns>
		[ImplementsFunction("session_id")]
		public static string Id(string id)
		{
            string oldId = Id();

            if (!string.IsNullOrEmpty(id))
            {
                RequestContext request_context;
                if (!Web.EnsureRequestContext(out request_context))
                    return null;

                if (request_context.ScriptContext.Config.Session.Handler.Name != AspNetSessionHandler.Default.Name)
                {
                    SessionId.SetNewSessionId(request_context, id);
                }
                else
                {
                    // TODO: throw warning, unsupported using Asp Sessions
                    PhpException.ArgumentValueNotSupported("session_id");
                }
            }

            return oldId;
		}

		/// <summary>
		/// Update the current session id with a newly generated one.
		/// </summary>
		/// <returns>Returns TRUE on success or FALSE on failure.</returns>
		[ImplementsFunction("session_regenerate_id")]
		public static bool RegenerateId()
		{
            return RegenerateId(false);
		}


		/// <summary>
		/// Update the current session id with a newly generated one. Not supported. 
		/// </summary>
        /// <param name="delete_old_session">Whether to delete the old associated session file or not.</param>
		/// <returns>Returns TRUE on success or FALSE on failure.   </returns>
        [ImplementsFunction("session_regenerate_id")]
        public static bool RegenerateId(bool delete_old_session)
        {
            RequestContext request_context;
            if (!Web.EnsureRequestContext(out request_context)) return false;

            if (request_context.SessionState != SessionStates.Started)
                return false;

            if (delete_old_session)
            {
                PhpArray session = PhpReference.AsPhpArray(request_context.ScriptContext.AutoGlobals.Session);
                if (session != null)
                    session.Clear();
            }

            if (request_context.ScriptContext.Config.Session.Handler.Name != AspNetSessionHandler.Default.Name)
            {
                // regenerate SessionID
                string session_id = SessionId.Manager.CreateSessionID(request_context.HttpContext/*not used*/);
                SessionId.SetNewSessionId(request_context, session_id);
            }
            else
            {
                // TODO: throw warning, unsupported using Asp Sessions
                PhpException.FunctionNotSupported();
            }

            return true;
        }

		#endregion

		#region session_decode, session_encode

		/// <summary>
		/// Deserializes data serialized by PHP session serializer and registers them into the $_SESSION
		/// and $GLOBAL (if register globals configuration option is on) variables.
		/// </summary>
		/// <param name="data">A string of bytes to deserialize.</param>
		/// <returns>Whether deserialization was successful.</returns>
		/// <exception cref="PhpException">Out of HTTP server context (Warning).</exception>
		/// <exception cref="PhpException">Session doesn't not exist (Notice).</exception>
		/// <exception cref="PhpException">Deserialization failed (Notice).</exception>
		[ImplementsFunction("session_decode")]
		public static bool DecodeVariables(PhpBytes data)
		{
			RequestContext request_context;
			if (!Web.EnsureRequestContext(out request_context)) return false;

			if (!request_context.SessionExists)
			{
				PhpException.Throw(PhpError.Notice, LibResources.GetString("session_not_exists"));
				return false;
			}

			ScriptContext context = request_context.ScriptContext;
			LibraryConfiguration config = LibraryConfiguration.GetLocal(context);
			GlobalConfiguration global = Configuration.Global;

            PhpReference php_ref = config.Session.Serializer.Deserialize(data, UnknownTypeDesc.Singleton);
			if (php_ref == null) return false;

			context.AutoGlobals.Session = php_ref;

			// copies session variables to $GLOBALS array if necessary:
			if (global.GlobalVariables.RegisterGlobals)
				context.RegisterSessionGlobals();

			return true;
		}

		/// <summary>
		/// Serializes session variables.
		/// </summary>
		/// <returns>
		/// Session variables serialized by the current session serializer. 
		/// Returns a <B>null</B> reference on failure.
		/// </returns>
		/// <exception cref="PhpException">Out of HTTP server context (Warning).</exception>
		/// <exception cref="PhpException">Session doesn't not exist (Notice).</exception>
		/// <exception cref="PhpException">Serialization failed (Notice).</exception>
		[ImplementsFunction("session_encode")]
		public static PhpBytes EncodeVariables()
		{
			RequestContext request_context;
			if (!Web.EnsureRequestContext(out request_context)) return null;

			if (!request_context.SessionExists)
			{
				PhpException.Throw(PhpError.Notice, LibResources.GetString("session_not_exists"));
				return null;
			}

			ScriptContext context = request_context.ScriptContext;
			LibraryConfiguration config = LibraryConfiguration.GetLocal(context);

            return config.Session.Serializer.Serialize(PhpReference.AsPhpArray(context.AutoGlobals.Session), UnknownTypeDesc.Singleton);
		}

		#endregion

		#region session_get_cookie_params, session_set_cookie_params

		/// <summary>
		/// Gets the cookie created for the session by ASP.NET server.
		/// </summary>
		private static bool GetCookie(out HttpCookie cookie, out RequestContext context)
		{
			if (!Web.EnsureRequestContext(out context))
			{
				context = null;
				cookie = null;
				return false;
			}

			cookie = AspNetSessionHandler.GetCookie(context.HttpContext);
			return cookie != null;
		}

		/// <summary>
		/// Get the session cookie parameters.
		/// </summary>
		[ImplementsFunction("session_get_cookie_params")]
		public static PhpArray GetCookieParameters()
		{
			RequestContext context;
			HttpCookie cookie;
			if (!GetCookie(out cookie, out context)) return null;

			PhpArray result = new PhpArray(0, 4);

			result.Add("secure", cookie.Secure);
			result.Add("domain", cookie.Domain);
			result.Add("path", cookie.Path);
			result.Add("lifetime", context.SessionCookieLifetime);

			return result;
		}

		/// <summary>
		/// Set the session cookie parameters.
		/// </summary>
		[ImplementsFunction("session_set_cookie_params")]
		public static void SetCookieParameters(int lifetime)
		{
			RequestContext context;
			HttpCookie cookie;
			if (!GetCookie(out cookie, out context)) return;

			context.SessionCookieLifetime = lifetime;
		}

		/// <summary>
		/// Set the session cookie parameters.
		/// </summary>
		[ImplementsFunction("session_set_cookie_params")]
		public static void SetCookieParameters(int lifetime, string path)
		{
			RequestContext context;
			HttpCookie cookie;
			if (!GetCookie(out cookie, out context)) return;

			context.SessionCookieLifetime = lifetime;
			cookie.Path = path;
		}

		/// <summary>
		/// Set the session cookie parameters.
		/// </summary>
		[ImplementsFunction("session_set_cookie_params")]
		public static void SetCookieParameters(int lifetime, string path, string domain)
		{
            SetCookieParameters(lifetime, path, domain, false, false);
		}

		/// <summary>
		/// Set the session cookie parameters.
		/// </summary>
		[ImplementsFunction("session_set_cookie_params")]
		public static void SetCookieParameters(int lifetime, string path, string domain, bool secure)
		{
            SetCookieParameters(lifetime, path, domain, secure, false);
		}

        /// <summary>
        /// Set the session cookie parameters.
        /// </summary>
        [ImplementsFunction("session_set_cookie_params")]
        public static void SetCookieParameters(int lifetime, string path, string domain, bool secure, bool httponly)
        {
            RequestContext context;
            HttpCookie cookie;
            if (!GetCookie(out cookie, out context)) return;

            context.SessionCookieLifetime = lifetime;
            cookie.Path = path;
            cookie.Domain = domain;
            cookie.Secure = secure;
            cookie.HttpOnly = httponly;
        }

		#endregion

		#region session_set_save_handler, session_module_name

		/// <summary>
		/// Sets handlers for session managing. 
		/// </summary>
		/// <remarks>
		/// Only those callbacks which are non-null are set others are left their previous values. 
		/// If any non-null callback binding fails none are set.
		/// </remarks>
		/// <returns>Whether all non-null callbacks were successfully set.</returns>
		/// <exception cref="PhpException">Web server is not available (Warning).</exception>
		[ImplementsFunction("session_set_save_handler")]
		public static bool SetHandlers(
			PhpCallback open,
			PhpCallback close,
			PhpCallback read,
			PhpCallback write,
			PhpCallback destroy,
			PhpCallback gc)
		{
			if (!Web.EnsureHttpContext()) return false;

			// binds all non-null callbacks (reports all errors due to bitwise or):
			if (!PhpArgument.CheckCallback(open, "open", 0, true) |
			  !PhpArgument.CheckCallback(close, "close", 0, true) |
			  !PhpArgument.CheckCallback(read, "read", 0, true) |
			  !PhpArgument.CheckCallback(write, "write", 0, true) |
			  !PhpArgument.CheckCallback(destroy, "destroy", 0, true) |
			  !PhpArgument.CheckCallback(gc, "gc", 0, true))
			{
				return false;
			}

			PhpUserSessionHandler.Handlers handlers = PhpUserSessionHandler.Handlers.Current;

			// sets current handlers:
			if (open != null) handlers.Open = open;
			if (close != null) handlers.Close = close;
			if (read != null) handlers.Read = read;
			if (write != null) handlers.Write = write;
			if (destroy != null) handlers.Destroy = destroy;
			if (gc != null) handlers.Collect = gc;

			Configuration.Local.Session.Handler = PhpUserSessionHandler.Default;

			return true;
		}

		/// <summary>
		/// Gets the current session handler name.
		/// </summary>
		/// <returns>The name of the current session handler.</returns>
		/// <exception cref="PhpException">Web server is not available (Warning).</exception>
		[ImplementsFunction("session_module_name")]
		public static string HandlerName()
		{
			if (!Web.EnsureHttpContext()) return null;

			return Configuration.Local.Session.Handler.Name;
		}

		/// <summary>
		/// Sets the current session module.
		/// </summary>
		/// <param name="name">A name of the new handler.</param>
		/// <returns>The name of the current session handler.</returns>
		/// <exception cref="PhpException">Web server is not available (Warning).</exception>
		/// <exception cref="PhpException"><paramref name="name"/> is a <B>null</B> reference.</exception>
		[ImplementsFunction("session_module_name")]
		public static string HandlerName(string name)
		{
			if (!Web.EnsureHttpContext()) return null;
			if (name == null)
			{
				PhpException.ArgumentNull("name");
				return Configuration.Local.Session.Handler.Name;
			}

			return (string)GsrHandler(Configuration.Local, null, name, IniAction.Set);
		}

		#endregion
	}
}
