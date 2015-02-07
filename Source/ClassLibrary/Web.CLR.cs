/*

	Copyright (c) 2004006- Tomas Matousek, Jan Benda and Ladislav Prosek.  

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

TODO:
	Changed get_headers() to retrieve headers also from non-200 responses. (PHP 5.1.3) 
	Changed get_headers() to use the default context. (PHP 5.1.3) 
*/

using System;
using System.Text;
using System.IO;
using System.ComponentModel;
using System.Collections;
using System.Net;
using System.Text.RegularExpressions;
using System.Reflection.Emit;

using PHP.Core;
using System.Collections.Generic;

#if SILVERLIGHT
using PHP.CoreCLR;
#else
using System.Web;
using System.Collections.Specialized;
#endif

namespace PHP.Library
{
	/// <summary>
	/// Web related methods (URL, HTTP, cookies, headers, connection etc.).
	/// </summary>
	/// <threadsafety static="true"/>
	public static partial class Web
	{
		#region Helpers

		/// <summary>
		/// Ensures that current <see cref="RequestContext"/> associted with the thread is not a <B>null</B> reference.
		/// </summary>
		/// <param name="context">The current request context.</param>
		/// <returns>Whether the request context is available.</returns>
		/// <exception cref="PhpException">Web server variables are not available (Warning).</exception>
		internal static bool EnsureRequestContext(out RequestContext context)
		{
			context = RequestContext.CurrentContext;
			if (context == null)
			{
				PhpException.Throw(PhpError.Warning, LibResources.GetString("web_server_not_available"));
				return false;
			}
			return true;
		}

		/// <summary>
		/// Ensures that current <see cref="HttpContext"/> is not a <B>null</B> reference.
		/// </summary>
		/// <param name="context">The current context.</param>
		/// <returns>Whether the HTTP context is available.</returns>
		/// <exception cref="PhpException">Web server variables are not available (Warning).</exception>
		internal static bool EnsureHttpContext(out HttpContext context)
		{
			context = HttpContext.Current;
			if (context == null)
			{
				PhpException.Throw(PhpError.Warning, LibResources.GetString("web_server_not_available"));
				return false;
			}
			return true;
		}

		/// <summary>
		/// Ensures that current <see cref="HttpContext"/> is not a <B>null</B> reference.
		/// </summary>
		/// <returns>Whether the HTTP context is available.</returns>
		/// <exception cref="PhpException">Web server variables are not available (Warning).</exception>
		internal static bool EnsureHttpContext()
		{
			HttpContext context;
			return Web.EnsureHttpContext(out context);
		}

		/// <summary>
		/// Ensures that headers has not been sent.
		/// </summary>
		/// <param name="context">The current context.</param>
		/// <returns>Whether the HTTP context is available and headers has not been sent.</returns>
		/// <exception cref="PhpException">Web server variables are not available (Warning).</exception>
		/// <exception cref="PhpException">Headers has already been sent (Warning).</exception>
		internal static bool EnsureHeadersNotSent(out HttpContext context)
		{
			bool result = HeadersSent();
			
			if (result) PhpException.Throw(PhpError.Warning, LibResources.GetString("headers_has_been_sent"));

            context = HttpContext.Current;
            return !result && context != null;
		}

		/// <summary>
		/// Puts together elements of HTTP status line.
		/// </summary>
		internal static string MakeHttpStatusLine(Version version, int code, string message)
		{
			return String.Format("HTTP/{0}.{1} {2} {3}", version.Major, version.Minor, code, message);
		}

		#endregion

		#region setcookie, setrawcookie

		/// <summary>
		/// Sends a cookie with specified name.
		/// </summary>
		/// <param name="name">The name of the cookie to send.</param>
		/// <returns>Whether a cookie has been successfully send.</returns>
		[ImplementsFunction("setcookie")]
		public static bool SetCookie(string name)
		{
            return SetCookieInternal(name, null, 0, null, null, false, false, false);
		}

		/// <summary>
		/// Sends a cookie with specified name and value.
		/// </summary>
		/// <param name="name">The name of the cookie to send.</param>
		/// <param name="value">The value of the cookie. The value will be <see cref="UrlEncode"/>d.</param>
		/// <returns>Whether a cookie has been successfully send.</returns>
		[ImplementsFunction("setcookie")]
		public static bool SetCookie(string name, string value)
		{
            return SetCookieInternal(name, value, 0, null, null, false, false, false);
		}

		/// <summary>
		/// Sends a cookie with specified name, value and expiration timestamp.
		/// </summary>
		/// <param name="name">The name of the cookie to send.</param>
		/// <param name="value">The value of the cookie. The value will be <see cref="UrlEncode"/>d.</param>
		/// <param name="expire">The time (Unix timestamp) when the cookie expiers.</param>
		/// <returns>Whether a cookie has been successfully send.</returns>
		[ImplementsFunction("setcookie")]
		public static bool SetCookie(string name, string value, int expire)
		{
            return SetCookieInternal(name, value, expire, null, null, false, false, false);
		}

		/// <summary>
		/// Sends a cookie with specified name, value and expiration timestamp.
		/// </summary>
		/// <param name="name">The name of the cookie to send.</param>
		/// <param name="value">The value of the cookie. The value will be <see cref="UrlEncode"/>d.</param>
		/// <param name="expire">The time (Unix timestamp) when the cookie expiers.</param>
		/// <param name="path">The virtual path on server in which context is the cookie valid.</param>
		/// <returns>Whether a cookie has been successfully send.</returns>
		[ImplementsFunction("setcookie")]
		public static bool SetCookie(string name, string value, int expire, string path)
		{
            return SetCookieInternal(name, value, expire, path, null, false, false, false);
		}

		/// <summary>
		/// Sends a cookie with specified name, value and expiration timestamp.
		/// </summary>
		/// <param name="name">The name of the cookie to send.</param>
		/// <param name="value">The value of the cookie. The value will be <see cref="UrlEncode"/>d.</param>
		/// <param name="expire">The time (Unix timestamp) when the cookie expiers.</param>
		/// <param name="path">The virtual path on server in which is the cookie valid.</param>
		/// <param name="domain">The domain where the cookie is valid.</param>
		/// <returns>Whether a cookie has been successfully send.</returns>
		[ImplementsFunction("setcookie")]
		public static bool SetCookie(string name, string value, int expire, string path, string domain)
		{
            return SetCookieInternal(name, value, expire, path, domain, false, false, false);
		}

		/// <summary>
		/// Sends a cookie with specified name, value and expiration timestamp.
		/// </summary>
		/// <param name="name">The name of the cookie to send.</param>
		/// <param name="value">The value of the cookie. The value will be <see cref="UrlEncode"/>d.</param>
		/// <param name="expire">The time (Unix timestamp) when the cookie expires.</param>
		/// <param name="path">The virtual path on server in which is the cookie valid.</param>
		/// <param name="domain">The domain where the cookie is valid.</param>
		/// <param name="secure">Whether to transmit the cookie securely (that is, over HTTPS only).</param>
		/// <returns>Whether a cookie has been successfully send.</returns>
		[ImplementsFunction("setcookie")]
		public static bool SetCookie(string name, string value, int expire, string path, string domain, bool secure)
		{
            return SetCookieInternal(name, value, expire, path, domain, secure, false, false);
		}

        /// <summary>
        /// Sends a cookie with specified name, value and expiration timestamp.
        /// </summary>
        /// <param name="name">The name of the cookie to send.</param>
        /// <param name="value">The value of the cookie. The value will be <see cref="UrlEncode"/>d.</param>
        /// <param name="expire">The time (Unix timestamp) when the cookie expires.</param>
        /// <param name="path">The virtual path on server in which is the cookie valid.</param>
        /// <param name="domain">The domain where the cookie is valid.</param>
        /// <param name="secure">Whether to transmit the cookie securely (that is, over HTTPS only).</param>
        /// <param name="httponly">When TRUE the cookie will be made accessible only through the HTTP protocol.
        /// This means that the cookie won't be accessible by scripting languages, such as JavaScript.
        /// This setting can effectively help to reduce identity theft through XSS attacks
        /// (although it is not supported by all browsers).</param>
        /// <returns>Whether a cookie has been successfully send.</returns>
        [ImplementsFunction("setcookie")]
        public static bool SetCookie(string name, string value, int expire, string path, string domain, bool secure, bool httponly)
        {
            return SetCookieInternal(name, value, expire, path, domain, secure, httponly, false);
        }

		/// <summary>
		/// The same as <see cref="SetCookie(string)"/> except for that value is not <see cref="UrlEncode"/>d.
		/// </summary>
		[ImplementsFunction("setrawcookie")]
		public static bool SetRawCookie(string name)
		{
            return SetCookieInternal(name, null, 0, null, null, false, false, true);
		}

		/// <summary>
		/// The same as <see cref="SetCookie(string,string)"/> except for that value is not <see cref="UrlEncode"/>d.
		/// </summary>
		[ImplementsFunction("setrawcookie")]
		public static bool SetRawCookie(string name, string value)
		{
            return SetCookieInternal(name, value, 0, null, null, false, false, true);
		}

		/// <summary>
		/// The same as <see cref="SetCookie(string,string,int)"/> except for that value is not <see cref="UrlEncode"/>d.
		/// </summary>
		[ImplementsFunction("setrawcookie")]
		public static bool SetRawCookie(string name, string value, int expire)
		{
            return SetCookieInternal(name, value, expire, null, null, false, false, true);
		}

		/// <summary>
		/// The same as <see cref="SetCookie(string,string,int,string)"/> except for that value is not <see cref="UrlEncode"/>d.
		/// </summary>
		[ImplementsFunction("setrawcookie")]
		public static bool SetRawCookie(string name, string value, int expire, string path)
		{
            return SetCookieInternal(name, value, expire, path, null, false, false, true);
		}

		/// <summary>
		/// The same as <see cref="SetCookie(string,string,int,string,string)"/> except for that value is not <see cref="UrlEncode"/>d.
		/// </summary>
		[ImplementsFunction("setrawcookie")]
		public static bool SetRawCookie(string name, string value, int expire, string path, string domain)
		{
            return SetCookieInternal(name, value, expire, path, domain, false, false, true);
		}

		/// <summary>
		/// The same as <see cref="SetCookie(string,string,int,string,string,bool)"/> except for that value is not <see cref="UrlEncode"/>d.
		/// </summary>
		[ImplementsFunction("setrawcookie")]
		public static bool SetRawCookie(string name, string value, int expire, string path, string domain, bool secure)
		{
            return SetCookieInternal(name, value, expire, path, domain, secure, false, true);
		}

        /// <summary>
        /// The same as <see cref="SetCookie(string,string,int,string,string,bool)"/> except for that value is not <see cref="UrlEncode"/>d.
        /// </summary>
        [ImplementsFunction("setrawcookie")]
        public static bool SetRawCookie(string name, string value, int expire, string path, string domain, bool secure, bool httponly)
        {
            return SetCookieInternal(name, value, expire, path, domain, secure, httponly, true);
        }

		/// <summary>
		/// Internal version common for <see cref="SetCookie"/> and <see cref="SetRawCookie"/>.
		/// </summary>
		internal static bool SetCookieInternal(string name, string value, int expire, string path, string domain, bool secure, bool httponly, bool raw)
		{
			HttpContext context;
			if (!EnsureHeadersNotSent(out context)) return false;

			HttpCookie cookie = new HttpCookie(name, raw ? value : UrlEncode(value));
			if (expire > 0)
			{
				cookie.Expires = DateTimeUtils.UnixTimeStampToUtc(expire).ToLocalTime();
			}
			cookie.Path = path;
			cookie.Domain = domain;
			cookie.Secure = secure;
            cookie.HttpOnly = httponly;

			context.Response.Cookies.Add(cookie);

			return true;
		}

		#endregion

        #region header, header_remove

        /// <summary>
		/// Adds a specified header to the current response.
		/// </summary>
		/// <param name="str">The header to be added.</param>
		/// <exception cref="PhpException">Web server variables are not available (Warning).</exception>
		/// <exception cref="PhpException">Headers has already been sent (Warning).</exception>
		[ImplementsFunction("header")]
		public static void Header(string str)
		{
			Header(str, false, 0);
		}

		/// <summary>
		/// Adds a specified header to the current response.
		/// </summary>
		/// <param name="str">The header to be added.</param>
		/// <param name="replace">Whether the header should be replaced if there is already one with the same name (ignored since 5.1.2)</param>
		/// <exception cref="PhpException">Web server variables are not available (Warning).</exception>
		/// <exception cref="PhpException">Headers has already been sent (Warning).</exception>
		/// <exception cref="PhpException"><paramref name="replace"/> is <B>true</B> (Warning).</exception>
		[ImplementsFunction("header")]
		public static void Header(string str, bool replace)
		{
			Header(str, replace, 0);
		}

		/// <summary>
		/// Adds a specified header to the current response.
		/// </summary>
		/// <param name="str">The header to be added.</param>
		/// <param name="replace">Whether the header should be replaced if there is already one with the same name. 
		/// Replacement not supported (ignored since 5.1.2)</param>
		/// <param name="httpResponseCode">Sets the response status code.</param>
		/// <remarks>
		/// <para>
		/// If <paramref name="httpResponseCode"/> is positive than the response status code is set to this value.
		/// Otherwise, if <paramref name="str"/> has format "{spaces}HTTP/{no spaces} {response code}{whatever}" 
		/// then the response code is set to the {responce code} and the method returns.
		/// </para>
		/// <para>
		/// If <paramref name="str"/> has format "{name}:{value}" then the respective header is set (both name and value 
		/// are trimmed) and an appropriate action associated with this header by ASP.NET is performed.
		/// </para>
		/// <para>
		/// Not:  Since PHP 4.4.2 and PHP 5.1.2 this function prevents more than one header to be sent at once as 
		/// a protection against header injection attacks (which means that header is always replaced).
		/// </para>
		/// </remarks>
		/// <exception cref="PhpException">Web server variables are not available (Warning).</exception>
		/// <exception cref="PhpException">Headers has already been sent (Warning).</exception>
		/// <exception cref="PhpException"><paramref name="replace"/> is <B>true</B> (Warning).</exception>
		/// <exception cref="PhpException"><paramref name="str"/> has invalid format (Warning).</exception>
		[ImplementsFunction("header")]
		public static void Header(string str, bool replace, int httpResponseCode)
		{
			if (str == null) return;

			HttpContext context;
			if (!EnsureHeadersNotSent(out context)) return;

			// response code is not forced => checks for initial HTTP/ and the status code in "str":  
			if (httpResponseCode <= 0)
			{
				Match m = Regex.Match(str, "[ ]*HTTP/[^ ]* ([0-9]{1,3}).*", RegexOptions.IgnoreCase);
				if (m.Success)
				{
					context.Response.StatusCode = Int32.Parse(m.Groups[1].Value);
					return;
				}
			}
			else
			{
				// sets response status code:
				context.Response.StatusCode = httpResponseCode;
			}

			// adds a header if it has a correct form (i.e. "name: value"):
			// store header in collection associated with current context - headers can be
			// replaced and are flushed automatically (in BeforeHeadersSent event :-)) on IIS Classic Mode.
			HttpHeaders headers = ScriptContext.CurrentContext.Headers;
			int i = str.IndexOf(':');
			if (i > 0)
			{
				string name = str.Substring(0, i).Trim();
				if (!string.IsNullOrEmpty(name))
					headers[name] = str.Substring(i + 1).Trim();
			}
		}

        /// <summary>
        /// RemoveRemoves an HTTP header previously set using header().
        /// </summary>
        [ImplementsFunction("header_remove")]
        public static void HeaderRemove()
        {
            // remove all headers
            HeaderRemove(null);
        }

        /// <summary>
        /// Removes an HTTP header previously set using header().
        /// </summary>
        /// <param name="name">The header name to be removed.
        /// Note: This parameter is case-insensitive. 
        /// </param>
        /// <remarks>Caution: This function will remove all headers set by PHP, including cookies, session and the X-Powered-By headers.</remarks>
        [ImplementsFunction("header_remove")]
        public static void HeaderRemove(string name)
        {
            if (name == null)
                ScriptContext.CurrentContext.Headers.Clear();
            else
                ScriptContext.CurrentContext.Headers[name] = null;
            //PhpException.FunctionNotSupported();    // see remarks, remove specified header (can be cookie, content-type, content-encoding or any other header)

            // TODO: cookies, session
        }

		#endregion

		#region get_headers

		/// <summary>
		/// Fetches headers sent by the server in response to a HTTP request.
		/// </summary>
		/// <param name="url">The URL where to send a request (e.g. http://www.mff.cuni.cz). </param>
		/// <returns>The same as <see cref="GetHeaders(string,bool)"/> where <c>format</c> is <B>false</B>.</returns>
		[ImplementsFunction("get_headers")]
		public static PhpArray GetHeaders(string url)
		{
			return GetHeaders(url, false);
		}

		/// <summary>
		/// Fetches headers sent by the server in response to a HTTP request.
		/// </summary>
		/// <param name="url">The URL where to send a request (e.g. http://www.mff.cuni.cz). </param>
		/// <param name="format">Whether to parse a response and set the result's keys to header names.</param>
		/// <returns>
		/// Either an array with integer keys indexed from 0 and values set to raw headers
		/// (<paramref name="format"/> is <B>false</B>). Or an array which each key is a name of a header and 
		/// a value is either an appropriate header's value or an array of values if the header has more than one 
		/// value. In both cases the first item (always with key 0) will be the HTTP response status line.
		/// </returns>
		[ImplementsFunction("get_headers")]
		public static PhpArray GetHeaders(string url, bool format)
		{
			HttpWebRequest request;
			HttpWebResponse response;

			// creates a HTTP request:
			try
			{
				request = (HttpWebRequest)WebRequest.Create(url);
			}
			catch (System.Exception)
			{
				PhpException.Throw(PhpError.Warning, LibResources.GetString("invalid_url",
					FileSystemUtils.StripPassword(url)));
				return null;
			}

			// fetches response:
			try
			{
				response = (HttpWebResponse)request.GetResponse();
			}
			catch (WebException e)
			{
				response = (HttpWebResponse)e.Response;
				PhpException.Throw(PhpError.Warning, LibResources.GetString("http_request_failed",
					(response != null) ? String.Format("({0}) {1}", (int)response.StatusCode, response.StatusDescription) : null));
				return null;
			}
			catch (System.Exception)
			{
				PhpException.Throw(PhpError.Warning, LibResources.GetString("host_not_responding"));
				return null;
			}

			WebHeaderCollection headers = response.Headers;
			PhpArray result = (format) ? new PhpArray(1, headers.Count) : new PhpArray(headers.Count + 1, 0);

			// adds the first entry (0 => HTTP status line):
			result.Add(0, MakeHttpStatusLine(response.ProtocolVersion, (int)response.StatusCode, response.StatusDescription));

			// creates an array {<header> => <value>}:
			if (format)
			{
				foreach (string header in headers)
				{
					// gets all values with specified header:
					string[] values = headers.GetValues(header);

					// puts values into an subarray if header has more than one value:
					if (values.Length > 1)
					{
						PhpArray values_array = new PhpArray(values.Length, 0);

						// fills subarray with values:
						for (int i = 0; i < values.Length; i++)
							values_array.Add(values[i]);

						result.Add(header, values_array);
					}
					else
					{
						result.Add(header, values[0]);
					}
				}
			}
			else
			// create an array {<index> => <header>: <value>}:
			{
				foreach (string header in headers)
					result.Add(String.Concat(header, ": ", headers[header]));
			}

			return result;
		}

		#endregion

		#region headers_sent, headers_list

		/// <summary>
		/// Checks whether all headers has been sent.
		/// </summary>
		/// <returns>Whether headers has already been sent.</returns>
		/// <exception cref="PhpException">Web server variables are not available (Warning).</exception>
		[ImplementsFunction("headers_sent")]
		public static bool HeadersSent()
		{
			HttpContext context;
			if (!EnsureHttpContext(out context)) return false;

			try
			{
				// a trick (StatusCodes's setter checks whether or not headers has been sent):
				context.Response.StatusCode = context.Response.StatusCode;
			}
			catch (HttpException)
			{
				return true;
			}

			return false;
		}

		/// <summary>
		/// Checks whether all headers has been sent.
		/// </summary>
		/// <param name="file">The name of a source file which has sent headers or an empty string 
		/// headers has not been sent yet. Not supported.</param>
		/// <returns>Whether headers has already been sent.</returns>
		/// <exception cref="PhpException">Web server variables are not available (Warning).</exception>
		/// <exception cref="PhpException">Function is not supported in this version (Warning).</exception>
		[ImplementsFunction("headers_sent")]
		public static bool HeadersSent(out string file)
		{
			PhpException.FunctionNotSupported();
			file = String.Empty;
			return HeadersSent();
		}

		/// <summary>
		/// Checks whether all headers has been sent.
		/// </summary>
		/// <param name="file">The name of a source file which has sent headers or an empty string  if
		/// headers has not been sent yet. Not supported.</param>
		/// <param name="line">The line in a source file where headers has been sent or 0 if 
		/// headers has not been sent yet. Not supported.</param>
		/// <returns>Whether headers has already been sent.</returns>
		/// <exception cref="PhpException">Web server variables are not available (Warning).</exception>
		/// <exception cref="PhpException">Function is not supported in this version (Warning).</exception>
		[ImplementsFunction("headers_sent")]
		public static bool HeadersSent(out string file, out int line)
		{
			PhpException.FunctionNotSupported();
			file = String.Empty;
			line = 0;
			return HeadersSent();
		}

		/// <summary>
        /// headers_list() will return a list of headers to be sent to the browser / client.
        /// To determine whether or not these headers have been sent yet, use headers_sent(). 
		/// </summary>
		[ImplementsFunction("headers_list")]
		public static PhpArray HeadersList()
		{
			HttpContext context;
            if (!EnsureHttpContext(out context))
                return null;

            var list = new PhpArray();
            
            foreach (var x in ScriptContext.CurrentContext.Headers)
                list.Add(x.Key + ": " + x.Value);

            /*foreach (var x in context.Response.Cookies.AllKeys)
            {
                var cookie = context.Response.Cookies[x];
                list.Add("set-cookie: " + cookie.Name + "=" + cookie.Value);    // TODO: full cookie spec
            }*/

            // TODO: cookies, session

            return list;
		}

		#endregion

		#region http_build_query, get_browser

        /// <summary>
        /// Generates a URL-encoded query string from the associative (or indexed) array provided. 
        /// </summary>
        /// <param name="formData">
        /// The array form may be a simple one-dimensional structure, or an array of arrays
        /// (who in turn may contain other arrays). 
        /// </param>
        /// <returns>Returns a URL-encoded string.</returns>
		[ImplementsFunction("http_build_query")]
		public static string HttpBuildQuery(PhpArray formData)
		{
            return PHP.Library.Web.HttpBuildQuery(formData, null, "&", null);
		}

        /// <summary>
        /// Generates a URL-encoded query string from the associative (or indexed) array provided. 
        /// </summary>
        /// <param name="formData">
        /// The array form may be a simple one-dimensional structure, or an array of arrays
        /// (who in turn may contain other arrays). 
        /// </param>
        /// <param name="numericPrefix">
        /// If numeric indices are used in the base array and this parameter is provided,
        /// it will be prepended to the numeric index for elements in the base array only.
        /// This is meant to allow for legal variable names when the data is decoded by PHP
        /// or another CGI application later on.
        /// </param>
        /// <returns>Returns a URL-encoded string.</returns>
		[ImplementsFunction("http_build_query")]
		public static string HttpBuildQuery(PhpArray formData, string numericPrefix)
		{
            return PHP.Library.Web.HttpBuildQuery(formData, numericPrefix, "&", null);
		}

        /// <summary>
        /// Generates a URL-encoded query string from the associative (or indexed) array provided. 
        /// </summary>
        /// <param name="formData">
        /// The array form may be a simple one-dimensional structure, or an array of arrays
        /// (who in turn may contain other arrays). 
        /// </param>
        /// <param name="numericPrefix">
        /// If numeric indices are used in the base array and this parameter is provided,
        /// it will be prepended to the numeric index for elements in the base array only.
        /// This is meant to allow for legal variable names when the data is decoded by PHP
        /// or another CGI application later on.
        /// </param>
        /// <param name="argSeparator">
        /// arg_separator.output is used to separate arguments, unless this parameter is
        /// specified, and is then used. 
        /// </param>
        /// <returns>Returns a URL-encoded string </returns>
		[ImplementsFunction("http_build_query")]
		public static string HttpBuildQuery(PhpArray formData, string numericPrefix, string argSeparator)
		{
            return HttpBuildQuery(formData, numericPrefix, argSeparator, null);
		}

        /// <summary>
        /// Generates a URL-encoded query string from the associative (or indexed) array provided. 
        /// </summary>
        /// <param name="formData">
        /// The array form may be a simple one-dimensional structure, or an array of arrays
        /// (who in turn may contain other arrays). 
        /// </param>
        /// <param name="numericPrefix">
        /// If numeric indices are used in the base array and this parameter is provided,
        /// it will be prepended to the numeric index for elements in the base array only.
        /// This is meant to allow for legal variable names when the data is decoded by PHP
        /// or another CGI application later on.
        /// </param>
        /// <param name="argSeparator">
        /// arg_separator.output is used to separate arguments, unless this parameter is
        /// specified, and is then used. 
        /// </param>
        /// <param name="indexerPrefix">Default is null, otherwise it is a name of the array to be used instead
        /// of regular parameter name. Then the parameter name is used as an index.
        /// 
        /// This argument must be URL encoded.</param>
        /// <returns>Returns a URL-encoded string </returns>
        private static string HttpBuildQuery(PhpArray formData, string numericPrefix, string argSeparator, string indexerPrefix)
        {
            StringBuilder str_builder = new StringBuilder(64);  // statistically the length of the result
            StringWriter result = new StringWriter(str_builder);

            bool isNotFirst = false;

            foreach (KeyValuePair<IntStringKey, object> item in formData)
            {
                // the query parameter name (key name)
                // the parameter name is URL encoded
                string keyName = null;

                if (item.Key.IsInteger)
                    keyName = UrlEncode(numericPrefix) + item.Key.Integer.ToString();
                else
                    keyName = UrlEncode(item.Key.String);

                if (indexerPrefix != null)
                {
                    keyName = indexerPrefix + "%5B" + keyName + "%5D";  // == prefix[key] (url encoded brackets)
                }
                
                // write the query element

                PhpArray valueArray = item.Value as PhpArray;

                if (valueArray != null)
                {
                    // value is an array, emit query recursively, use current keyName as an array variable name

                    string queryStr = HttpBuildQuery(valueArray, null, argSeparator, keyName);  // emit the query recursively

                    if (queryStr != null && queryStr.Length > 0)
                    {
                        if (isNotFirst)
                            result.Write(argSeparator);

                        result.Write(queryStr);
                    }
                }
                else
                {
                    // simple value, emit query in a form of (key=value), URL encoded !

                    if (isNotFirst)
                        result.Write(argSeparator);

                    if (item.Value != null)
                    {
                        result.Write(keyName + "=" + UrlEncode(PHP.Core.Convert.ObjectToString(item.Value)));    // == "keyName=keyValue"
                    }
                    else
                    {
                        result.Write(keyName + "=");    // == "keyName="
                    }
                }

                // separator will be used in next loop
                isNotFirst = true;
            }

            result.Flush();

            return str_builder.ToString();
        }


        /// <summary>
        /// Attempts to determine the capabilities of the user's browser, by looking up the browser's information in the browscap.ini  file.
        /// </summary>
        /// <returns>
        ///  The information is returned in an object or an array which will contain various data elements representing,
        ///  for instance, the browser's major and minor version numbers and ID string; TRUE/FALSE  values for features
        ///  such as frames, JavaScript, and cookies; and so forth.
        ///  The cookies value simply means that the browser itself is capable of accepting cookies and does not mean
        ///  the user has enabled the browser to accept cookies or not. The only way to test if cookies are accepted is
        ///  to set one with setcookie(), reload, and check for the value. 
        /// </returns>
        [ImplementsFunction("get_browser")]
        public static object GetBrowser()
		{
            return GetBrowser(null, false);
		}

        /// <summary>
        /// Attempts to determine the capabilities of the user's browser, by looking up the browser's information in the browscap.ini  file.
        /// </summary>
        /// <param name="user_agent">
        /// The User Agent to be analyzed. By default, the value of HTTP User-Agent header is used; however, you can alter this (i.e., look up another browser's info) by passing this parameter.
        /// You can bypass this parameter with a NULL value.
        /// </param>
        /// <returns>
        ///  The information is returned in an object or an array which will contain various data elements representing,
        ///  for instance, the browser's major and minor version numbers and ID string; TRUE/FALSE  values for features
        ///  such as frames, JavaScript, and cookies; and so forth.
        ///  The cookies value simply means that the browser itself is capable of accepting cookies and does not mean
        ///  the user has enabled the browser to accept cookies or not. The only way to test if cookies are accepted is
        ///  to set one with setcookie(), reload, and check for the value. 
        /// </returns>
        [ImplementsFunction("get_browser")]
        public static object GetBrowser(string user_agent)
		{
            return GetBrowser(user_agent, false);
		}

        /// <summary>
        /// Attempts to determine the capabilities of the user's browser, by looking up the browser's information in the browscap.ini  file.
        /// </summary>
        /// <param name="user_agent">
        /// The User Agent to be analyzed. By default, the value of HTTP User-Agent header is used; however, you can alter this (i.e., look up another browser's info) by passing this parameter.
        /// You can bypass this parameter with a NULL value.
        /// </param>
        /// <param name="return_array">If set to TRUE, this function will return an array instead of an object . </param>
        /// <returns>
        ///  The information is returned in an object or an array which will contain various data elements representing,
        ///  for instance, the browser's major and minor version numbers and ID string; TRUE/FALSE  values for features
        ///  such as frames, JavaScript, and cookies; and so forth.
        ///  The cookies value simply means that the browser itself is capable of accepting cookies and does not mean
        ///  the user has enabled the browser to accept cookies or not. The only way to test if cookies are accepted is
        ///  to set one with setcookie(), reload, and check for the value. 
        /// </returns>
        [ImplementsFunction("get_browser")]
        public static object GetBrowser(string user_agent, bool return_array /*= false*/)
        {
            HttpBrowserCapabilities browserCaps = GetBrowserCaps(user_agent);    // this is container for information given from Request and browscap.ini, which is placed in Win systems by default

            if (browserCaps == null)
                return null;

            // some special fields
            /*if (browserCaps.Browsers != null)
                for (int ib = 0; ib < browserCaps.Browsers.Count; ++ib)
                    if (browserCaps.Browsers[ib].ToString().ToLower() == browserCaps.Browser.ToLower())
                    {
                        if (ib > 0) caps["parent"] = browserCaps.Browsers[ib - 1].ToString();
                        break;
                    }*/

            // create an array of browser capabilities:
            var caps = new PhpArray(browserCaps.Capabilities.Count);

            foreach (var x in browserCaps.Capabilities.Keys)
                caps.Add(x, browserCaps.Capabilities[x]);

            if (return_array)
                return caps;

            // create an object of browser capabilities:
            return new stdClass()
            {
                RuntimeFields = caps
            };
        }

        private static HttpBrowserCapabilities GetBrowserCaps(string user_agent)
        {
            if (String.IsNullOrEmpty(user_agent))
            {
                HttpContext context;
                if (!EnsureHttpContext(out context))
                    return null;

                return context.Request.Browser;
            }
            else
            {
                NameValueCollection headers = new NameValueCollection();
                headers["User-Agent"] = user_agent;

                HttpBrowserCapabilities browserCaps = new HttpBrowserCapabilities();
                Hashtable hashtable = new Hashtable(180, StringComparer.OrdinalIgnoreCase);
                hashtable[string.Empty] = user_agent; // The actual method uses client target   
                browserCaps.Capabilities = hashtable;

                //var capsFactory = new System.Web.Configuration.BrowserCapabilitiesFactory();
                //capsFactory.ConfigureBrowserCapabilities(headers, browserCaps);
                //capsFactory.ConfigureCustomCapabilities(headers, browserCaps);

                // use System.Web.Configuration.BrowserCapabilitiesFactory dynamically since Mono does not have this defined
                // Following code emits DynamicMethod delegate lazily and performs code commented above.
                
                // Note: absolutely no performance overhead.
                // This should be removed when the type will be defined on Mono.
                var configureCapsMethod = ConfigureCapsMethod;
                if (configureCapsMethod != null)
                    configureCapsMethod(headers, browserCaps);

                return browserCaps;
            }
        }

        #region System.Web.Configuration.BrowserCapabilitiesFactory

        /// <summary>
        /// Get DynamicMethod that configures capabilities on systems, where System.Web.Configuration.BrowserCapabilitiesFactory is defined.
        /// </summary>
        /// <remarks>
        /// The method performs following code:
        /// {
        ///     var capsFactory = new System.Web.Configuration.BrowserCapabilitiesFactory();
        ///     capsFactory.ConfigureBrowserCapabilities(headers, browserCaps);
        ///     capsFactory.ConfigureCustomCapabilities(headers, browserCaps);
        /// }
        /// </remarks>
        private static Action<NameValueCollection, HttpBrowserCapabilities> ConfigureCapsMethod
        {
            get
            {
                if (configureCapsMethod == null && configureCapsMethodAvailable)
                    lock (configureCapsLocker)  // double checked lock
                        if (configureCapsMethod == null && configureCapsMethodAvailable)
                        {
                            // find the type dynamically
                            Type browserCapabilitiesFactoryType = null;
                            foreach (var ass in AppDomain.CurrentDomain.GetAssemblies())
                                if ((browserCapabilitiesFactoryType = ass.GetType("System.Web.Configuration.BrowserCapabilitiesFactory")) != null)
                                    break;

                            //Type browserCapabilitiesFactoryType = Type.GetType("System.Web.Configuration.BrowserCapabilitiesFactory", false);
                            if (browserCapabilitiesFactoryType != null)
                                configureCapsMethod = BuildConfigureCapsMethod(browserCapabilitiesFactoryType);
                            else
                                configureCapsMethodAvailable = false;   // TODO: declare another Type with required methods and BuildConfigureCapsMethod() with this Type
                        }
                    
                return configureCapsMethod;
            }
        }
        private static Action<NameValueCollection, HttpBrowserCapabilities> configureCapsMethod = null;
        private static bool configureCapsMethodAvailable = true;
        private static object configureCapsLocker = new object();

        /// <summary>
        /// Create DynamicMethod that configures capabilities using System.Web.Configuration.BrowserCapabilitiesFactory (or similar) type.
        /// </summary>
        /// <param name="BrowserCapabilitiesFactoryType">Type with ConfigureBrowserCapabilities and ConfigureCustomCapabilities methods.</param>
        /// <remarks>
        /// Generated method performs following code:
        /// {
        ///     var capsFactory = new System.Web.Configuration.BrowserCapabilitiesFactory();
        ///     capsFactory.ConfigureBrowserCapabilities(headers, browserCaps);
        ///     capsFactory.ConfigureCustomCapabilities(headers, browserCaps);
        /// }
        /// </remarks>
        private static Action<NameValueCollection, HttpBrowserCapabilities> BuildConfigureCapsMethod(Type/*!*/BrowserCapabilitiesFactoryType)
        {
            Debug.Assert(BrowserCapabilitiesFactoryType != null);

            var method_ctor = BrowserCapabilitiesFactoryType.GetConstructor(Type.EmptyTypes);
            var method_ConfigureBrowserCapabilities = BrowserCapabilitiesFactoryType.GetMethod("ConfigureBrowserCapabilities");
            var method_ConfigureCustomCapabilities = BrowserCapabilitiesFactoryType.GetMethod("ConfigureCustomCapabilities");

            if (method_ctor == null) throw new InvalidOperationException(string.Format("{0} does not implement .ctor.", BrowserCapabilitiesFactoryType.ToString()));
            if (method_ConfigureBrowserCapabilities == null) throw new InvalidOperationException(string.Format("{0} does not implement {1}.", BrowserCapabilitiesFactoryType.ToString(), "ConfigureBrowserCapabilities"));
            if (method_ConfigureCustomCapabilities == null) throw new InvalidOperationException(string.Format("{0} does not implement {1}.", BrowserCapabilitiesFactoryType.ToString(), "ConfigureCustomCapabilities"));

            var method = new DynamicMethod("<dynamic>.BrowserCapabilitiesFactory", typeof(void), new Type[] { typeof(NameValueCollection), typeof(HttpBrowserCapabilities) });
            var il = new PHP.Core.Emit.ILEmitter(method);

            method.DefineParameter(1, System.Reflection.ParameterAttributes.None, "headers");
            method.DefineParameter(2, System.Reflection.ParameterAttributes.None, "browserCaps");

            // var capsFactory = new System.Web.Configuration.BrowserCapabilitiesFactory();
            var loc_factory = il.DeclareLocal(BrowserCapabilitiesFactoryType);
            il.Emit(OpCodes.Newobj, method_ctor);
            il.Stloc(loc_factory);

            // capsFactory.ConfigureBrowserCapabilities(headers, browserCaps);
            il.Ldloc(loc_factory);
            il.Ldarg(0);
            il.Ldarg(1);
            il.Emit(OpCodes.Callvirt, method_ConfigureBrowserCapabilities);

            // capsFactory.ConfigureCustomCapabilities(headers, browserCaps);
            il.Ldloc(loc_factory);
            il.Ldarg(0);
            il.Ldarg(1);
            il.Emit(OpCodes.Callvirt, method_ConfigureCustomCapabilities);

            // ret
            il.Emit(OpCodes.Ret);

            // done
            return (Action<NameValueCollection, HttpBrowserCapabilities>)method.CreateDelegate(typeof(Action<NameValueCollection, HttpBrowserCapabilities>));
        }

        #endregion

        #endregion

        #region connection_aborted, connection_timeout, connection_status

        /// <summary>
		/// Checks whether a client is still connected.
		/// </summary>
		/// <returns>Whether a client is still connected.</returns>
		[ImplementsFunction("connection_aborted")]
		public static bool IsClientDisconnected()
		{
			HttpContext context;
			if (!EnsureHttpContext(out context)) return false;

			// we needn't to check for abortion because the abortion implies disconnection:
			return !context.Response.IsClientConnected;
		}

		/// <summary>
		/// Checks whether a client is still connected.
		/// </summary>
		/// <returns>Whether a client is still connected.</returns>
		[ImplementsFunction("connection_timeout")]
		public static bool ConnectionTimeout()
		{
			return ScriptContext.CurrentContext.ExecutionTimedOut;
		}

		/// <summary>
		/// Retrieves a connection status. 
		/// </summary>
		/// <returns>The connection status bitfield.</returns>
		/// <remarks>
		/// Works also out of HTTP context (i.e. in console and windows apps). 
		/// In that cases, only <see cref="ConnectionStatus.Timeout"/> flag is relevant.
		/// </remarks>
		[ImplementsFunction("connection_status")]
		public static int GetConnectionStatus()
		{
			ConnectionStatus result = ConnectionStatus.Normal;

			if (ScriptContext.CurrentContext.ExecutionTimedOut)
				result |= ConnectionStatus.Timeout;

			HttpContext context = HttpContext.Current;
			if (context != null && !context.Response.IsClientConnected)
				result |= ConnectionStatus.Aborted;

			return (int)result;
		}

		#endregion

		#region is_uploaded_file, move_uploaded_file

		/// <summary>
		/// Tells whether the file was uploaded via HTTP POST.
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		[ImplementsFunction("is_uploaded_file")]
		public static bool IsUploadedFile(string path)
		{
			if (path == null) return false;
			return RequestContext.CurrentContext.IsTemporaryFile(path);
		}


		/// <summary>
		/// Moves an uploaded file to a new location.
		/// </summary>
		/// <param name="path"></param>
		/// <param name="destination"></param>
		/// <returns></returns>
		[ImplementsFunction("move_uploaded_file")]
		public static bool MoveUploadedFile(string path, string destination)
		{
			RequestContext context = RequestContext.CurrentContext;
			if (path == null || !context.IsTemporaryFile(path)) return false;

			if (PhpFile.Exists(destination))
				PhpFile.Delete(destination);

			if (!PhpFile.Rename(path, destination))
				return false;

			context.RemoveTemporaryFile(path);
			return true;
		}

		#endregion
	}
}  
