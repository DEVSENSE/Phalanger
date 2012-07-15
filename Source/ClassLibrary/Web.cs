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
using System.Runtime.CompilerServices;

using PHP.Core;
using System.Collections.Generic;

#if SILVERLIGHT
using PHP.CoreCLR;
using System.Windows.Browser;
#else
using System.Web;
#endif

namespace PHP.Library
{
	/// <summary>
	/// Web related methods (URL, HTTP, cookies, headers, connection etc.).
	/// </summary>
	/// <threadsafety static="true"/>
	public static partial class Web
	{
		public enum UrlComponent
		{
			[ImplementsConstant("PHP_URL_SCHEME")]
			Scheme = 0,
			[ImplementsConstant("PHP_URL_HOST")]
			Host = 1,
			[ImplementsConstant("PHP_URL_PORT")]
			Port = 2,
			[ImplementsConstant("PHP_URL_USER")]
			User = 3,
			[ImplementsConstant("PHP_URL_PASS")]
			Password = 4,
			[ImplementsConstant("PHP_URL_PATH")]
			Path = 5,
			[ImplementsConstant("PHP_URL_QUERY")]
			Query = 6,
			[ImplementsConstant("PHP_URL_FRAGMENT")]
			Fragment = 7
		}

		#region Enumerations

		/// <summary>
		/// Connection status.
		/// </summary>
		[Flags]
		public enum ConnectionStatus
		{
			[ImplementsConstant("CONNECTION_NORMAL")]
			Normal = 0,
			[ImplementsConstant("CONNECTION_ABORTED")]
			Aborted = 1,
			[ImplementsConstant("CONNECTION_TIMEOUT")]
			Timeout = 2
		}

		#endregion

		#region base64_decode, base64_encode

		[ImplementsFunction("base64_decode"), EditorBrowsable(EditorBrowsableState.Never)]
		[return: CastToFalse]
		public static PhpBytes DecodeBase64(string encoded_data)
		{
            return DecodeBase64(encoded_data, false);
		}

        [ImplementsFunction("base64_decode"), EditorBrowsable(EditorBrowsableState.Never)]
        [return: CastToFalse]
        public static PhpBytes DecodeBase64(string encoded_data, bool strict /* = false*/)
        {
            if (encoded_data == null) return null;
            try
            {
                return new PhpBytes(System.Convert.FromBase64String(encoded_data));
            }
            catch (FormatException)
            {
                PhpException.Throw(PhpError.Warning, LibResources.GetString("invalid_base64_encoded_data"));
                return null;
            }
        }

		[ImplementsFunction("base64_encode"), EditorBrowsable(EditorBrowsableState.Never)]
		public static string EncodeBase64(PhpBytes data_to_encode)
		{
			if (data_to_encode == null) return null;
            return System.Convert.ToBase64String(data_to_encode.ReadonlyData);
		}

		#endregion

		#region parse_url, parse_str

        #region Helper parse_url() methods

        internal static class ParseUrlMethods
        {
            /// <summary>
            /// Regular expression for parsing URLs (via parse_url())
            /// </summary>
            public static Regex ParseUrlRegEx
            {
                get
                {
                    return
                        (_parseUrlRegEx) ??
                        (_parseUrlRegEx = new Regex(@"^((?<scheme>[^:]+):(?<scheme_separator>/{0,2}))?((?<user>[^:@/?#\[\]]*)(:(?<pass>[^@/?#\[\]]*))?@)?(?<host>([^/:?#\[\]]+)|(\[[^\[\]]+\]))?(:(?<port>[0-9]*))?(?<path>/[^\?#]*)?(\?(?<query>[^#]+)?)?(#(?<fragment>.*))?$", 
#if !SILVERLIGHT
                            RegexOptions.Compiled |
#endif
                            RegexOptions.Singleline | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase

                            ));
                }
            }
            private static Regex _parseUrlRegEx = null;

            /// <summary>
            /// Determines matched group value or null if the group was not matched.
            /// </summary>
            /// <param name="g"></param>
            /// <returns></returns>
            public static string MatchedString(Group/*!*/g)
            {
                Debug.Assert(g != null);

                return (g.Success && g.Value.Length > 0) ? g.Value : null;
            }

            /// <summary>
            /// Replace all the occurrences of control characters (see iscntrl() C++ function) with the specified character.
            /// </summary>
            /// <param name="str"></param>
            /// <param name="newChar"></param>
            /// <returns></returns>
            public static string ReplaceControlCharset(string/*!*/str, char newChar)
            {
                Debug.Assert(str != null);

                string result = str;

                int i = 0;
                foreach (char c in str)
                {
                    byte b = (byte)c;

                    if (b <= 0x1F || b == 0x7F)
                        result = result.Remove(i) + newChar + result.Substring(i + 1);

                    ++i;
                }

                return result;
            }
        }

        #endregion

        /// <summary>
		/// Parses an URL and returns its components.
		/// </summary>
		/// <param name="url">
		/// The URL string with format 
        /// <c>{scheme}://{user}:{pass}@{host}:{port}{path}?{query}#{fragment}</c>
		/// or <c>{schema}:{path}?{query}#{fragment}</c>.
		/// </param>
		/// <returns>
		/// An array which keys are names of components (stated in URL string format in curly braces, e.g."schema")
		/// and values are components themselves.
		/// </returns>
		[ImplementsFunction("parse_url")]
		public static PhpArray ParseUrl(string url)
		{
            Match match = ParseUrlMethods.ParseUrlRegEx.Match(url ?? string.Empty);

            if (match == null || !match.Success || match.Groups["port"].Value.Length > 5)   // not matching or port number too long
            {
                PhpException.Throw(PhpError.Warning, LibResources.GetString("invalid_url", FileSystemUtils.StripPassword(url)));
                return null;
            }

            string scheme = ParseUrlMethods.MatchedString(match.Groups["scheme"]);
            string user = ParseUrlMethods.MatchedString(match.Groups["user"]);
            string pass = ParseUrlMethods.MatchedString(match.Groups["pass"]);
            string host = ParseUrlMethods.MatchedString(match.Groups["host"]);
            string port = ParseUrlMethods.MatchedString(match.Groups["port"]);
            string path = ParseUrlMethods.MatchedString(match.Groups["path"]);
            string query = ParseUrlMethods.MatchedString(match.Groups["query"]);
            string fragment = ParseUrlMethods.MatchedString(match.Groups["fragment"]);

            string scheme_separator = match.Groups["scheme_separator"].Value;   // cannot be null

            int tmp;

            // some exceptions
            if (host != null && scheme != null && scheme_separator.Length == 0 && int.TryParse(host, out tmp))
            {   // domain:port/path
                port = host;
                host = scheme;
                scheme = null;
            }
            else if (scheme_separator.Length != 2 && host != null)
            {   // mailto:user@host
                // st:xx/zzz
                // mydomain.com/path
                // mydomain.com:port/path

                // dismiss user and pass
                if (user != null || pass != null)
                {
                    if (pass != null) user = user + ":" + pass;
                    host = user + "@" + host;

                    user = null;
                    pass = null;
                }

                // dismiss port
                if (port != null)
                {
                    host += ":" + port;
                    port = null;
                }

                // everything as a path
                path = scheme_separator + host + path;
                host = null;
            }
            
			PhpArray result = new PhpArray(0, 8);

            const char neutralChar = '_';

            // store segments into the array (same order as it is in PHP)
            if (scheme != null) result["scheme"] = ParseUrlMethods.ReplaceControlCharset(scheme, neutralChar);
            if (host != null) result["host"] = ParseUrlMethods.ReplaceControlCharset(host, neutralChar);
            if (port != null) result["port"] = (int)unchecked((ushort)uint.Parse(port)); // PHP overflows in this way
            if (user != null) result["user"] = ParseUrlMethods.ReplaceControlCharset(user, neutralChar);
            if (pass != null) result["pass"] = ParseUrlMethods.ReplaceControlCharset(pass, neutralChar);
            if (path != null) result["path"] = ParseUrlMethods.ReplaceControlCharset(path, neutralChar);
            if (query != null) result["query"] = ParseUrlMethods.ReplaceControlCharset(query, neutralChar);
            if (fragment != null) result["fragment"] = ParseUrlMethods.ReplaceControlCharset(fragment, neutralChar);

            return result;
		}

		[ImplementsFunction("parse_url")]
		public static object ParseUrl(string url, UrlComponent component)
		{
			PhpArray array = ParseUrl(url);
			if (array == null) return null;

			switch (component)
			{
				case UrlComponent.Fragment: return (string)array["fragment"];
				case UrlComponent.Host: return (string)array["host"];
				case UrlComponent.Password: return (string)array["pass"];
				case UrlComponent.Path: return (string)array["path"];
				case UrlComponent.Port: object port = array["port"]; if (port != null) return (int)port; else return null;
				case UrlComponent.Query: return (string)array["query"];
				case UrlComponent.Scheme: return (string)array["scheme"];
				case UrlComponent.User: return (string)array["user"];

				default:
					PhpException.Throw(PhpError.Warning, LibResources.GetString("arg:invalid_value", "component", component));
					return null;
			}
		}

		/// <summary>
		/// Parses a string as if it were the query string passed via an URL.
		/// </summary>
		/// <param name="definedVariables">Only to comply with Phalanger Class Library rules - all overloads of the same 
		/// function has to have the same implementation options. Can be <B>null</B> reference.</param>
		/// <param name="str">The string to parse.</param>
		/// <param name="result">The array to store the variable found in <paramref name="str"/> to.</param>
		[ImplementsFunction("parse_str", FunctionImplOptions.NeedsVariables), EditorBrowsable(EditorBrowsableState.Never)]
		public static void ParseUrlQuery(Dictionary<string, object> definedVariables, string str, out PhpArray result)
		{
			result = new PhpArray();

			Dictionary<string, object> temp = new Dictionary<string, object>();

			ParseUrlQuery(temp, str);

			foreach(string key in temp.Keys)
			{
				result.Add(key, temp[key]);
			}
		}

		/// <summary>
		/// Parses a string as if it were the query string passed via an URL and sets variables in the
		/// current scope.
		/// </summary>
		/// <param name="localVariables">The <see cref="IDictionary"/> where to store variables and its values.</param>
		/// <param name="str">The string to parse.</param>
		[ImplementsFunction("parse_str", FunctionImplOptions.NeedsVariables)]
		public static void ParseUrlQuery(Dictionary<string, object> localVariables, string str)
		{
			if (str == null) return;

			PhpArray globals = (localVariables != null) ? null : ScriptContext.CurrentContext.GlobalVariables;

			int index = -1, lastAmp = -1, lastEq = -1;
			char[] eqAmp = new char[] { '&', '=' };
			string key = null, val = null;

			// search for = and & if = has not been found yet, or for & if = has already been found
			while ((index = (lastEq > -1 ? str.IndexOf('&', index + 1) : str.IndexOfAny(eqAmp, index + 1))) > -1)
			{
				if (str[index] == '=')
				{
					key = str.Substring(lastAmp + 1, index - lastAmp - 1);
					lastEq = index;
				}
				else
				{
					if (lastEq > -1)
						val = str.Substring(lastEq + 1, index - lastEq - 1);
					else
						key = str.Substring(lastAmp + 1, index - lastAmp - 1);

					if (key.Length > 0)
					{
						// if no variable value is specified (no = or nothing after =),
						// an empty string is used as the value:
						ParseUrlQuery_InitVariable(globals, localVariables, HttpUtility.UrlDecode(key),
							(val == null) ? String.Empty : HttpUtility.UrlDecode(val));
					}

					lastAmp = index;
					lastEq = -1;
					key = val = null;
				}
			}

			// process the rest of the string (after last = and &)
			if (lastEq > -1)
				val = str.Substring(lastEq + 1, str.Length - lastEq - 1);
			else
				key = str.Substring(lastAmp + 1, str.Length - lastAmp - 1);

			if (key.Length > 0)
			{
				ParseUrlQuery_InitVariable(globals, localVariables, HttpUtility.UrlDecode(key),
					(val == null) ? String.Empty : HttpUtility.UrlDecode(val));
			}
		}

		private static void ParseUrlQuery_InitVariable(PhpArray globals, Dictionary<string, object> localVariables, string key, object value)
		{
			if (key.EndsWith("[]"))
			{
				key = key.Substring(0, key.Length - 2);

				object ov;

				if (PhpArray.TryGetValue(globals, localVariables, key, out ov))
				{
					if (ov is PhpArray)
					{
						PhpArray a = (PhpArray)ov;

						a.Add(value);
					}
					else
					{
						PhpArray.Set(globals, localVariables, HttpUtility.UrlDecode(key), PhpArray.New(ov, value));
					}
				}
				else
				{
					PhpArray.Set(globals, localVariables, HttpUtility.UrlDecode(key), PhpArray.New(value));
				}
			}
			else
			{
				PhpArray.Set(globals, localVariables, HttpUtility.UrlDecode(key), value);
			}
		}

		#endregion

		#region rawurlencode, rawurldecode, urlencode, urldecode

		/// <summary>
		/// Decode URL-encoded strings
		/// </summary>
		/// <param name="str">The URL string (e.g. "hello%20from%20foo%40bar").</param>
		/// <returns>Decoded string (e.g. "hello from foo@bar")</returns>
		[ImplementsFunction("rawurldecode")]
		public static string RawUrlDecode(string str)
		{
			if (str == null) return null;
            return HttpUtility.UrlDecode(str.Replace("+", "%2B"));  // preserve '+'
        }

		/// <summary>
		/// Encodes a URL string keeping spaces in it. Spaces are encoded as '%20'.
		/// </summary>  
		/// <param name="str">The string to be encoded.</param>
		/// <returns>The encoded string.</returns>
		[ImplementsFunction("rawurlencode")]
		public static string RawUrlEncode(string str)
		{
			if (str == null) return null;
            return UpperCaseEncodedChars(HttpUtility.UrlEncode(str)).Replace("+", "%20");   // ' ' => '+' => '%20'
		}

		/// <summary>
		/// Decodes a URL string.
		/// </summary>  
		[ImplementsFunction("urldecode")]
		public static string UrlDecode(string str)
		{
            return HttpUtility.UrlDecode(str);
		}

		/// <summary>
        /// Encodes a URL string. Spaces are encoded as '+'.
		/// </summary>  
		[ImplementsFunction("urlencode")]
		public static string UrlEncode(string str)
		{
            return UpperCaseEncodedChars(HttpUtility.UrlEncode(str));
		}

        private static string UpperCaseEncodedChars(string encoded)
        {
            char[] temp = encoded.ToCharArray();
            for (int i = 0; i < temp.Length - 2; i++)
            {
                if (temp[i] == '%')
                {
                    temp[i + 1] = temp[i + 1].ToUpperAsciiInvariant();
                    temp[i + 2] = temp[i + 2].ToUpperAsciiInvariant();
                }
            }
            return new string(temp);
        }

//#if !SILVERLIGHT
//        /// <summary>
//        /// Encodes a Unicode URL string.
//        /// </summary>  
//        [ImplementsFunction("urlencode_unicode")]
//        public static string UrlEncodeUnicode(string str)
//        {
//            return HttpUtility.UrlEncodeUnicode(str);//TODO: implement this in PhpHttpUtility
//        }
//#endif

		#endregion

		#region get_meta_tags

		/// <summary>
		/// Lazily initialized &lt;meta&gt; tag regex.
		/// </summary>
		private static volatile Regex getMetaTagsRegex = null;

		/// <summary>
		/// Extracts all meta tag content attributes from a file and returns an array.
		/// </summary>
		/// <param name="fileName">The file to search for meta tags in.</param>
		/// <returns>Array with keys set to values of the name property and values set to values of the
		/// content property.</returns>
		/// <remarks>The parsing stops at the &lt;/head&gt; tag.</remarks>
		[ImplementsFunction("get_meta_tags")]
		[return: CastToFalse]
		public static PhpArray GetMetaTags(string fileName)
		{
			return GetMetaTags(fileName, FileOpenOptions.Empty);
		}

		/// <summary>
		/// Extracts all meta tag content attributes from a file and returns an array.
		/// </summary>
		/// <param name="fileName">The file to search for meta tags in.</param>
		/// <param name="flags">If true, the file specified by <paramref name="fileName"/> should be sought
		/// for along the standard include path.</param>
		/// <returns>Array with keys set to values of the name property and values set to values of the
		/// content property.</returns>
		/// <remarks>The parsing stops at the &lt;/head&gt; tag.</remarks>
		[ImplementsFunction("get_meta_tags")]
		[return: CastToFalse]
		public static PhpArray GetMetaTags(string fileName, FileOpenOptions flags)
		{
			PhpArray result = new PhpArray();
			ScriptContext context = ScriptContext.CurrentContext;

			if (getMetaTagsRegex == null)
			{
				getMetaTagsRegex = new Regex(@"^meta\s+name\s*=\s*(?:(\w*)|'([^']*)'|\u0022([^\u0022]*)\u0022)\s+" +
					@"content\s*=\s*(?:(\w*)|'([^']*)'|\u0022([^\u0022]*)\u0022)\s*/?$",
					RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
			}

			try
			{
				PhpStream stream = PhpStream.Open(fileName, "rt", PhpFile.ProcessOptions(flags));
				StringBuilder tag = new StringBuilder();

				bool in_brackets = false;
				int in_quotes = 0; // 1 = ', 2 = "
				int in_comment = 0; // 1 = <, 2 = <!, 3 = <!-, 4 = <!--, 5 = <!-- -, 6  <!-- --

				while (!stream.Eof)
				{
					int start_index = 0;

					string line = stream.ReadLine(-1, null);
					for (int i = 0; i < line.Length; i++)
					{
						switch (line[i])
						{
							case '<':
								{
									if (!in_brackets && in_quotes == 0 && in_comment == 0)
									{
										in_brackets = true;
										in_comment = 1;

										start_index = i + 1;
									}
									break;
								}

							case '>':
								{
									if (in_brackets && in_quotes == 0 && in_comment != 4 && in_comment != 5)
									{
										in_brackets = false;
										in_comment = 0;

										if (start_index < i) tag.Append(line, start_index, i - start_index);

										string str = tag.ToString();
										tag.Length = 0;

										// did we reach the end of <head>?
										if (str.Equals("/head", StringComparison.InvariantCultureIgnoreCase)) return result;

										// try to match the tag with the <meta> regex
										Match match = getMetaTagsRegex.Match(str);
										if (match.Success)
										{
											string name = null, value = null;
											for (int j = 1; j <= 3; j++)
												if (match.Groups[j].Success)
												{
													name = match.Groups[j].Value;
													break;
												}

											if (name != null)
											{
												for (int j = 4; j <= 6; j++)
													if (match.Groups[j].Success)
													{
														value = match.Groups[j].Value;
														break;
													}

												result[name] = (value == null ? String.Empty : Core.Convert.Quote(value, context));
											}
										}
									}
									break;
								}

							case '\'':
								{
									if (in_quotes == 0) in_quotes = 1;
									else if (in_quotes == 1) in_quotes = 0;
									break;
								}

							case '"':
								{
									if (in_quotes == 0) in_quotes = 2;
									else if (in_quotes == 2) in_quotes = 0;
									break;
								}

							case '!': if (in_comment == 1) in_comment = 2; break;
							case '-': if (in_comment >= 2 && in_comment < 6) in_comment++; break;

							default:
								{
									// reset comment state machine
									if (in_comment < 4) in_comment = 0;
									if (in_comment > 4) in_comment = 4;
									break;
								}
						}
					}

					if (in_brackets && start_index < line.Length) tag.Append(line, start_index, line.Length - start_index);
				}
			}
			catch (IOException)
			{
				return null;
			}

			return result;
		}

		#endregion
    }
}  
