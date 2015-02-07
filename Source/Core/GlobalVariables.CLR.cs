/*

 Copyright (c) 2004-2006 Tomas Matousek.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Net;
using System.IO;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Web;
using System.Web.SessionState;
using System.Collections.Specialized;
using System.Reflection;
using PHP.Core.Emit;

namespace PHP.Core
{
	/// <summary>
	/// Declares auto-global variables stored in the script context.
	/// </summary>
	public sealed class AutoGlobals
	{
		internal const int EgpcsCount = 5;
		internal const int MaxCount = 9;
		internal const int EstimatedUserGlobalVariableCount = 15;

		#region Enumeration

		/// <summary>
		/// File upload errors.
		/// </summary>
		public enum PostedFileError
		{
			/// <summary>
			/// No error.
			/// </summary>
			None,

			/// <summary>
			/// The uploaded file exceeds the "upload_max_filesize" configuration option. Not supported.
			/// Request is not processed when exceeding maximal size of posted file set in ASP.NET config.
			/// </summary>
			SizeExceededOnServer,

			/// <summary>
			/// The uploaded file exceeds the "MAX_FILE_SIZE" value specified in the form. Not supported.
			/// </summary>
			SizeExceededOnClient,

			/// <summary>
			/// The uploaded file was only partially uploaded. Not supported.
			/// </summary>
			Partial,

			/// <summary>
			/// No file was uploaded.
			/// </summary>
			NoFile
		}

		#endregion

		#region Fields

		/// <summary>
		/// <para>
		/// If server context is available contains server variables ($_SERVER).
		/// Moreover, it contains <c>PHP_SELF</c> - a virtual path to the executing script and
		/// if <see cref="GlobalConfiguration.GlobalVariablesSection.RegisterArgcArgv"/> is set it contains also
		/// <c>argv</c> (an array containing a query string as its one and only element) and 
		/// <c>argc</c> which is set to zero.
		/// </para>
		/// <para>
		/// If server context is not available contains empty array (unlike PHP which does fill it with <see cref="Env"/>
		/// and then adds some empty items).
		/// </para>
		/// </summary>
		public PhpReference/*!*/ Server = new PhpReference();
		public const string ServerName = "_SERVER";

		/// <summary>
		/// Environment variables ($_ENV).
		/// </summary>
		public PhpReference/*!*/ Env = new PhpReference();
		public const string EnvName = "_ENV";

		/// <summary>
		/// Global variables ($GLOBALS). 
		/// </summary>
		public PhpReference/*!*/ Globals = new PhpReference();
		public const string GlobalsName = "GLOBALS";

		/// <summary>
		/// Request variables ($_REQUEST) copied from $_GET, $_POST and $_COOKIE arrays.
		/// </summary>
		public PhpReference/*!*/ Request = new PhpReference();
		public const string RequestName = "_REQUEST";

		/// <summary>
		/// Variables passed by HTTP GET method ($_GET).
		/// </summary>
		public PhpReference/*!*/ Get = new PhpReference();
		public const string GetName = "_GET";

		/// <summary>
		/// Variables passed by HTTP POST method ($_POST).
		/// </summary>
		public PhpReference/*!*/ Post = new PhpReference();
		public const string PostName = "_POST";

		/// <summary>
		/// Cookies ($_COOKIE).
		/// </summary>
		public PhpReference/*!*/ Cookie = new PhpReference();
		public const string CookieName = "_COOKIE";

        /// <summary>
        /// Raw POST data ($HTTP_RAW_POST_DTA). Equivalent to file_get_contents("php://input").
        /// </summary>
        public PhpReference/*!*/ HttpRawPostData = new PhpReference();
        public const string HttpRawPostDataName = "HTTP_RAW_POST_DATA";

		/// <summary>
		/// Uploaded files information ($_FILES).
		/// </summary>
		public PhpReference/*!*/ Files = new PhpReference();
		public const string FilesName = "_FILES";

		/// <summary>
		/// Session variables ($_SESSION). Initialized on session start.
		/// </summary>
		public PhpReference/*!*/ Session = new PhpReference();
		public const string SessionName = "_SESSION";

		#endregion

		#region IsAutoGlobal

		/// <summary>
		/// Checks whether a specified name is the name of an auto-global variable.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <returns>Whether <paramref name="name"/> is auto-global.</returns>
		public static bool IsAutoGlobal(string name)
		{
			switch (name)
			{
                case GlobalsName:
                case ServerName:
                case EnvName:
                case CookieName:
                case HttpRawPostDataName:
                case FilesName:
                case RequestName:
                case GetName:
                case PostName:
                case SessionName:
					return true;

                default:
                    return false;
			}
		}

		#endregion

		#region Variable Addition

		/// <summary>
		/// Adds a variable to auto-global array.
		/// </summary>
		/// <param name="array">The array.</param>
		/// <param name="name">A unparsed name of variable.</param>
		/// <param name="value">A value to be added.</param>
		/// <param name="subname">A name of intermediate array inserted before the value.</param>
		private static void AddVariable(
		  PhpArray/*!*/ array,
		  string name,
		  object value,
		  string subname)
		{
			if (array == null)
				throw new ArgumentNullException("array");
			if (name == null)
				name = String.Empty;

            string key;

			// current left and right square brace positions:
			int left, right;

			// checks pattern {var_name}[{key1}][{key2}]...[{keyn}] where var_name is [^[]* and keys are [^]]*:
			left = name.IndexOf('[');
			if (left > 0 && left < name.Length - 1 && (right = name.IndexOf(']', left + 1)) >= 0)
			{
				// the variable name is a key to the "array", dots are replaced by underscores in top-level name:
                key = EncodeTopLevelName(name.Substring(0, left));

				// ensures that all [] operators in the chain except for the last one are applied on an array:
				for (;;)
				{
					// adds a level keyed by "key":
					array = Operators.EnsureItemIsArraySimple(array, key);

					// adds a level keyed by "subname" (once only):
					if (subname != null)
					{
						array = Operators.EnsureItemIsArraySimple(array, subname);
						subname = null;
					}

					// next key:
					key = name.Substring(left + 1, right - left - 1);

					// breaks if ']' is not followed by '[':
					left = right + 1;
					if (left == name.Length || name[left] != '[') break;

					// the next right brace:
					right = name.IndexOf(']', left + 1);
				}

				if (key.Length > 0)
					array.SetArrayItem(key, value);
				else
					array.Add(value);
			}
			else
			{
				// no array pattern in variable name, "name" is a top-level key:
                name = EncodeTopLevelName(name);

				// inserts a subname on the next level:
				if (subname != null)
					Operators.EnsureItemIsArraySimple(array, name)[subname] = value;
				else
					array[name] = value;
			}
		}

        /// <summary>
        /// Fixes top level variable name to not contain spaces and dots (as it is in PHP);
        /// </summary>
        private static string EncodeTopLevelName(string/*!*/name)
        {
            Debug.Assert(name != null);

            return name.Replace('.', '_').Replace(' ', '_');
        }

        /// <summary>
        /// Returns <see cref="HttpUtility.UrlDecode"/>  of <paramref name="value"/> if it is a string.
        /// </summary>
        private static string UrlDecodeValue(string value)
        {
            return HttpUtility.UrlDecode(value, Configuration.Application.Globalization.PageEncoding);
        }

        //private static object GpcEncodeValue(object value, LocalConfiguration config)
        //{
        //    if (value != null && value.GetType() == typeof(string))
        //    {
        //         // url-decodes the values: (COOKIES ONLY)
        //        string svalue = HttpUtility.UrlDecode((string)value, Configuration.Application.Globalization.PageEncoding);

        //        // quotes the values:
        //        if (Configuration.Global.GlobalVariables.QuoteGpcVariables)
        //        {
        //            if (config.Variables.QuoteInDbManner)
        //                svalue = StringUtils.AddDbSlashes(svalue);
        //            svalue = StringUtils.AddCSlashes(svalue, true, true);
        //        }

        //        //
        //        value = svalue;
        //    }

        //    return value;
        //}

		/// <summary>
		/// Adds variables from one auto-global array to another.
		/// </summary>
		/// <param name="dst">The target array.</param>
		/// <param name="src">The source array.</param>
		/// <remarks>Variable values are deeply copied.</remarks>
		/// <exception cref="ArgumentNullException"><paramref name="dst"/> is a <B>null</B> reference.</exception>
		/// <exception cref="ArgumentNullException"><paramref name="src"/> is a <B>null</B> reference.</exception>
		private static void AddVariables(PhpArray/*!*/ dst, PhpArray/*!*/ src)
		{
			Debug.Assert(dst != null && src != null);

			foreach (KeyValuePair<IntStringKey, object> entry in src)
				dst[entry.Key] = PhpVariable.DeepCopy(entry.Value);
		}

		/// <summary>
		/// Adds variables from one auto-global array to another.
		/// </summary>
		/// <param name="dst">A PHP reference to the target array.</param>
		/// <param name="src">A PHP reference to the source array.</param>
		/// <remarks>
		/// Variable values are deeply copied. 
		/// If either reference is a <B>null</B> reference or doesn't contain an array, no copying takes place.
		/// </remarks>
		internal static void AddVariables(PhpReference/*!*/ dst, PhpReference/*!*/ src)
		{
			if (dst != null && src != null)
			{
				PhpArray adst = dst.Value as PhpArray;
				PhpArray asrc = src.Value as PhpArray;
				if (adst != null && asrc != null)
					AddVariables(adst, asrc);
			}
		}

		/// <summary>
		/// Loads variables from a collection.
		/// </summary>
		/// <param name="result">An array where to add variables stored in the collection.</param>
		/// <param name="collection">The collection.</param>
		private static void LoadFromCollection(PhpArray result, NameValueCollection collection)
		{
			foreach (string name in collection)
			{
				// gets all values associated with the name:
				string[] values = collection.GetValues(name);

                if (values == null)
                    continue;   // http://phalanger.codeplex.com/workitem/30132

				// adds all items:
				if (name != null)
				{
					foreach (string value in values)
                        AddVariable(result, name, value, null);
				}
				else
				{
					// if name is null, only name of the variable is stated:
					// e.g. for GET variables, URL looks like this: ...&test&...
					// we add the name of the variable and an emtpy string to get what PHP gets:
                    foreach (string value in values)
                        AddVariable(result, value, String.Empty, null);
				}
			}
		}

		#endregion

		#region Initialization

		/// <summary>
		/// Initializes all auto-global variables.
		/// </summary>
		internal void Initialize(LocalConfiguration config/*!*/, HttpContext context)
		{
			Debug.Assert(config != null);
			HttpRequest request = (context != null) ? context.Request : null;

			// $_ENV:
			InitializeEnvironmentVariables(config);

			// $_SERVER:
			InitializeServerVariables(config, context);

			// $_GET, $_POST, $_COOKIE, $_REQUEST, $HTTP_RAW_POST_DATA:
			InitializeGetPostCookieRequestVariables(config, request);

			// $_SESSION (initialized by session_start)

			// $_FILE:
			InitializeFileVariables(config, request, context);

			// $GLOBALS:
			InitializeGlobals(config, request);
		}

		/// <summary>
		/// Loads $_ENV from Environment.GetEnvironmentVariables().
		/// </summary>
		private void InitializeEnvironmentVariables(LocalConfiguration/*!*/ config)
		{
			Debug.Assert(config != null);

			IDictionary env_vars = Environment.GetEnvironmentVariables();
			PhpArray array = new PhpArray(0, env_vars.Count);

			foreach (DictionaryEntry entry in env_vars)
				AddVariable(array, entry.Key as string, entry.Value as string, null);

			Env.Value = array;
		}

		/// <summary>
		/// Loads $_SERVER from HttpRequest.ServerVariables.
		/// </summary>
		private void InitializeServerVariables(LocalConfiguration/*!*/ config, HttpContext context)
		{
            if (context == null)
            {
                Server.Value = new PhpArray();
                return;
            }

            Debug.Assert(config != null);            

			PhpArray array, argv;

            var request = context.Request;
            var serverVariables = request.ServerVariables;

            Server.Value = array = new PhpArray(0, /*serverVariables.Count*/64);

			// adds variables defined by ASP.NET and IIS:
            LoadFromCollection(array, serverVariables);

			// adds argv, argc variables:
			if (Configuration.Global.GlobalVariables.RegisterArgcArgv)
			{
                array["argv"] = argv = new PhpArray(1) { request.QueryString };
                array["argc"] = 0;
			}

			// additional variables defined in PHP manual:
            array["PHP_SELF"] = request.Path;

			try
			{
                array["DOCUMENT_ROOT"] = request.MapPath("/"); // throws exception under mod_aspdotnet
			}
			catch
			{
				array["DOCUMENT_ROOT"] = null;
			}

            array["SERVER_ADDR"] = serverVariables["LOCAL_ADDR"];
            array["REQUEST_URI"] = request.RawUrl;
			array["REQUEST_TIME"] = DateTimeUtils.UtcToUnixTimeStamp(context.Timestamp.ToUniversalTime());
            array["SCRIPT_FILENAME"] = request.PhysicalPath;

			//IPv6 is the default in IIS7, convert to an IPv4 address (store the IPv6 as well)
            if (request.UserHostAddress.Contains(":"))
			{
                array["REMOTE_ADDR_IPV6"] = request.UserHostAddress;

                if (request.UserHostAddress == "::1")
                {
                    array["REMOTE_ADDR"] = array["SERVER_ADDR"] = "127.0.0.1";
                }
                else foreach (IPAddress IPA in Dns.GetHostAddresses(request.UserHostAddress))
                    {
                        if (IPA.AddressFamily.ToString() == "InterNetwork")
                        {
                            array["REMOTE_ADDR"] = IPA.ToString();
                            break;
                        }
                    }
			}

            // PATH_INFO
            // should contain partial path information only
            // note: IIS has AllowPathInfoForScriptMappings property that do the thing ... but ISAPI does not work then
            // hence it must be done here manually

            if (array.ContainsKey("PATH_INFO"))
            {
                string path_info = (string)array["PATH_INFO"];
                string script_name = (string)array["SCRIPT_NAME"];
                    
                // 'ORIG_PATH_INFO'
                // Original version of 'PATH_INFO' before processed by PHP. 
                array["ORIG_PATH_INFO"] = path_info;
                    
                // 'PHP_INFO'
                // Contains any client-provided pathname information trailing the actual script filename
                // but preceding the query string, if available. For instance, if the current script was
                // accessed via the URL http://www.example.com/php/path_info.php/some/stuff?foo=bar,
                // then $_SERVER['PATH_INFO'] would contain /some/stuff. 
                    
                // php-5.3.2\sapi\isapi\php5isapi.c:
                // 
                // strncpy(path_info_buf, static_variable_buf + scriptname_len - 1, sizeof(path_info_buf) - 1);    // PATH_INFO = PATH_INFO.SubString(SCRIPT_NAME.Length);


                array["PATH_INFO"] = (script_name.Length <= path_info.Length) ? path_info.Substring(script_name.Length) : string.Empty;
            }
		}


		/// <summary>
        /// Loads $_GET, $_POST, $_COOKIE, $HTTP_RAW_POST_DATA, and $_REQUEST arrays.
		/// </summary>
		private void InitializeGetPostCookieRequestVariables(LocalConfiguration/*!*/ config, HttpRequest request)
		{
			Debug.Assert(config != null);

			PhpArray get_array, post_array, cookie_array, request_array;
            string httprawpostdata_bytes;

            InitializeGetPostVariables(config, request, out get_array, out post_array, out httprawpostdata_bytes);
			InitializeCookieVariables(config, request, out cookie_array);
			InitializeRequestVariables(request, config.Variables.RegisteringOrder,
			  get_array, post_array, cookie_array, out request_array);

			Get.Value = get_array;
			Post.Value = post_array;
			Cookie.Value = cookie_array;
			Request.Value = request_array;
            HttpRawPostData.Value = httprawpostdata_bytes;
		}

		/// <summary>
		/// Loads $_GET, $_POST arrays from HttpRequest.QueryString and HttpRequest.Form.
		/// </summary>
		/// <param name="config">Local configuration.</param>
		/// <param name="request">HTTP request instance or a <B>null</B> reference.</param>
		/// <param name="getArray">Resulting $_GET array.</param>
		/// <param name="postArray">Resulting $_POST array.</param>
        /// <param name="httprawpostdataBytes">$HTTP_RAW_POST_DATA variable.</param>
		/// <exception cref="ArgumentNullException"><paranref name="config"/> is a <B>null</B> reference.</exception>
		public static void InitializeGetPostVariables(LocalConfiguration/*!*/ config, HttpRequest request,
          out PhpArray getArray, out PhpArray postArray, out string httprawpostdataBytes)
		{
			if (config == null)
				throw new ArgumentNullException("config");

            if (request != null)
            {
                if (request.RequestType == "GET")
                {
                    getArray = new PhpArray(0, request.QueryString.Count + request.Form.Count);
                    postArray = new PhpArray(0, 0);

                    // loads Form variables to GET array:
                    LoadFromCollection(getArray, request.Form);
                }
                else
                {
                    getArray = new PhpArray(0, request.QueryString.Count);
                    postArray = new PhpArray(0, request.Form.Count);

                    // loads Form variables to POST array:
                    LoadFromCollection(postArray, request.Form);
                }

                // loads Query variables to GET array:
                LoadFromCollection(getArray, request.QueryString);

                // HTTP_RAW_POST_DATA   // when always_populate_raw_post_data option is TRUE, however using "php://input" is preferred. For "multipart/form-data" it is not available.
                try
                {
                    httprawpostdataBytes =
                       (config.Variables.AlwaysPopulateRawPostData && !request.ContentType.StartsWith("multipart/form-data")) ?
                       new StreamReader(request.InputStream).ReadToEnd() :
                       null;
                }
                catch
                {
                    httprawpostdataBytes = null;    // unable to read the input stream, unreachable
                }
            }
            else
            {
                getArray = new PhpArray(0, 0);
                postArray = new PhpArray(0, 0);
                httprawpostdataBytes = null;
            }
		}


		/// <summary>
		/// Loads $_COOKIE arrays from HttpRequest.Cookies.
		/// </summary>
		/// <param name="config">Local configuration.</param>
		/// <param name="request">HTTP request instance or a <B>null</B> reference.</param>
		/// <param name="cookieArray">Resulting $_COOKIE array.</param>
		/// <exception cref="ArgumentNullException"><paranref name="config"/> is a <B>null</B> reference.</exception>
		public static void InitializeCookieVariables(LocalConfiguration/*!*/ config, HttpRequest request,
		  out PhpArray cookieArray)
		{
            Debug.Assert(config != null);

			if (request != null)
			{
                var cookies = request.Cookies;
                Debug.Assert(cookies != null, "cookies == null");

                int count = cookies.Count;
				cookieArray = new PhpArray(0, count);

                for (int i = 0; i < count; i++)
                {
                    HttpCookie cookie = cookies.Get(i);
					AddVariable(cookieArray, cookie.Name, UrlDecodeValue(cookie.Value), null);

					// adds a copy of cookie with the same key as the session name;
					// the name gets encoded and so $_COOKIE[session_name()] doesn't work then:
					if (cookie.Name == AspNetSessionHandler.AspNetSessionName)
						cookieArray[AspNetSessionHandler.AspNetSessionName] = UrlDecodeValue(cookie.Value);
				}
			}
			else
			{
				cookieArray = new PhpArray(0, 0);
			}
		}

		/// <summary>
		/// Loads $_REQUEST from $_GET, $_POST and $_COOKIE arrays.
		/// </summary>
		private static void InitializeRequestVariables(HttpRequest request, string/*!*/ gpcOrder,
		  PhpArray/*!*/ getArray, PhpArray/*!*/ postArray, PhpArray/*!*/ cookieArray, out PhpArray requestArray)
		{
			Debug.Assert(gpcOrder != null && getArray != null && postArray != null && cookieArray != null);

			if (request != null)
			{
				requestArray = new PhpArray(0, getArray.Count + postArray.Count + cookieArray.Count);

				// adds items from GET, POST, COOKIE arrays in the order specified by RegisteringOrder config option:
				for (int i = 0; i < gpcOrder.Length; i++)
				{
					switch (Char.ToUpperInvariant(gpcOrder[i]))
					{
						case 'G': AddVariables(requestArray, getArray); break;
						case 'P': AddVariables(requestArray, postArray); break;
						case 'C': AddVariables(requestArray, cookieArray); break;
					}
				}
			}
			else
			{
				requestArray = new PhpArray(0, 0);
			}
		}

        /// <summary>
		/// Loads $_FILES from HttpRequest.Files.
		/// </summary>
		/// <remarks>
		/// <list type="bullet">
		///   <item>$_FILES[{var_name}]['name'] - The original name of the file on the client machine.</item>
		///   <item>$_FILES[{var_name}]['type'] - The mime type of the file, if the browser provided this information. An example would be "image/gif".</item>
		///   <item>$_FILES[{var_name}]['size'] - The size, in bytes, of the uploaded file.</item> 
		///   <item>$_FILES[{var_name}]['tmp_name'] - The temporary filename of the file in which the uploaded file was stored on the server.</item>
		///   <item>$_FILES[{var_name}]['error'] - The error code associated with this file upload.</item> 
		/// </list>
		/// </remarks>
        private void InitializeFileVariables(LocalConfiguration/*!*/ config, HttpRequest request, HttpContext context)
		{
			Debug.Assert(config != null);
			PhpArray files;
			int count;

			GlobalConfiguration global_config = Configuration.Global;

			if (request != null && global_config.PostedFiles.Accept && (count = request.Files.Count) > 0)
			{
                Debug.Assert(context != null);
                Debug.Assert(RequestContext.CurrentContext != null, "PHP.Core.RequestContext not initialized!");

				files = new PhpArray(0, count);

				// gets a path where temporary files are stored:
				var temppath = global_config.PostedFiles.GetTempPath(global_config.SafeMode);
                // temporary file name (first part)
                var basetempfilename = string.Concat("php_", context.Timestamp.Ticks.ToString("x"), "-");
                var basetempfileid = this.GetHashCode();

				for (int i = 0; i < count; i++)
				{
					string name = request.Files.GetKey(i);
					string file_path, type, file_name;
					HttpPostedFile file = request.Files[i];
					PostedFileError error = PostedFileError.None;

					if (!string.IsNullOrEmpty(file.FileName))
					{
						type = file.ContentType;

                        var tempfilename = string.Concat(basetempfilename, (basetempfileid++).ToString("X"), ".tmp");
                        file_path = Path.Combine(temppath, tempfilename);
						file_name = Path.GetFileName(file.FileName);

						// registers the temporary file for deletion at request end:
						RequestContext.CurrentContext.AddTemporaryFile(file_path);

						// saves uploaded content to the temporary file:
						file.SaveAs(file_path);
					}
					else
					{
						file_path = type = file_name = String.Empty;
						error = PostedFileError.NoFile;
					}

					AddVariable(files, name, file_name, "name");
					AddVariable(files, name, type, "type");
					AddVariable(files, name, file_path, "tmp_name");
					AddVariable(files, name, (int)error, "error");
					AddVariable(files, name, file.ContentLength, "size");
				}
			}
			else
			{
				files = new PhpArray(0, 0);
			}

			Files.Value = files;
		}

		/// <summary>
		/// Adds file variables from $_FILE array to $GLOBALS array.
		/// </summary>
		/// <param name="globals">$GLOBALS array.</param>
		/// <param name="files">$_FILES array.</param>
		private void AddFileVariablesToGlobals(PhpArray/*!*/ globals, PhpArray/*!*/ files)
		{
			foreach (KeyValuePair<IntStringKey, object> entry in files)
			{
				PhpArray file_info = (PhpArray)entry.Value;

				globals[entry.Key] = file_info["tmp_name"];
				globals[entry.Key.ToString() + "_name"] = file_info["name"];
				globals[entry.Key.ToString() + "_type"] = file_info["type"];
				globals[entry.Key.ToString() + "_size"] = file_info["size"];
			}
		}

		/// <summary>
		/// Loads $GLOBALS from $_ENV, $_REQUEST, $_SERVER and $_FILES.
		/// </summary>
		private void InitializeGlobals(LocalConfiguration/*!*/ config, HttpRequest/*!*/ request)
		{
			Debug.Assert(config != null && Request.Value != null && Env.Value != null && Server.Value != null && Files.Value != null);

			PhpArray globals;
			GlobalConfiguration global = Configuration.Global;

			// estimates the initial capacity of $GLOBALS array:
			int count = EstimatedUserGlobalVariableCount + AutoGlobals.MaxCount;
			if (global.GlobalVariables.RegisterLongArrays) count += AutoGlobals.MaxCount;

			// adds EGPCS variables as globals:
			if (global.GlobalVariables.RegisterGlobals)
			{
				PhpArray env_array = (PhpArray)Env.Value;
				PhpArray get_array = (PhpArray)Get.Value;
				PhpArray post_array = (PhpArray)Post.Value;
				PhpArray files_array = (PhpArray)Files.Value;
				PhpArray cookie_array = (PhpArray)Cookie.Value;
				PhpArray server_array = (PhpArray)Server.Value;
				PhpArray request_array = (PhpArray)Request.Value;

				if (request != null)
				{
					globals = new PhpArray(0, count + env_array.Count + request_array.Count + server_array.Count + files_array.Count * 4);

					// adds items in the order specified by RegisteringOrder config option (overwrites existing):
					string order = config.Variables.RegisteringOrder;
					for (int i = 0; i < order.Length; i++)
					{
						switch (order[i])
						{
							case 'E': AddVariables(globals, env_array); break;
							case 'G': AddVariables(globals, get_array); break;

							case 'P':
								AddVariables(globals, post_array);
								AddFileVariablesToGlobals(globals, files_array);
								break;

							case 'C': AddVariables(globals, cookie_array); break;
							case 'S': AddVariables(globals, server_array); break;
						}
					}
				}
				else
				{
					globals = new PhpArray(0, count + env_array.Count);
					AddVariables(globals, env_array);
				}
			}
			else
			{
				globals = new PhpArray(0, count);
			}

			// command line argc, argv:
			if (request == null)
			{
				string[] args = Environment.GetCommandLineArgs();
				PhpArray argv = new PhpArray(0, args.Length);

				// adds all arguments to the array (the 0-th argument is not '-' as in PHP but the program file):
				for (int i = 0; i < args.Length; i++)
					argv.Add(i, args[i]);

				globals["argv"] = argv;
				globals["argc"] = args.Length;
			}

			// adds auto-global variables (overwrites potential existing variables in $GLOBALS):
			globals[GlobalsName] = Globals;
			globals[EnvName] = Env;
			globals[GetName] = Get;
			globals[PostName] = Post;
			globals[CookieName] = Cookie;
			globals[RequestName] = Request;
			globals[ServerName] = Server;
			globals[FilesName] = Files;
			globals[SessionName] = Session;
            globals[HttpRawPostDataName] = HttpRawPostData;

			// adds long arrays:
			if (Configuration.Global.GlobalVariables.RegisterLongArrays)
			{
				globals.Add("HTTP_ENV_VARS", new PhpReference(((PhpArray)Env.Value).DeepCopy()));
				globals.Add("HTTP_GET_VARS", new PhpReference(((PhpArray)Get.Value).DeepCopy()));
				globals.Add("HTTP_POST_VARS", new PhpReference(((PhpArray)Post.Value).DeepCopy()));
				globals.Add("HTTP_COOKIE_VARS", new PhpReference(((PhpArray)Cookie.Value).DeepCopy()));
				globals.Add("HTTP_SERVER_VARS", new PhpReference(((PhpArray)Server.Value).DeepCopy()));
				globals.Add("HTTP_POST_FILES", new PhpReference(((PhpArray)Files.Value).DeepCopy()));

				// both session array references the same array:
				globals.Add("HTTP_SESSION_VARS", Session);
			}

			Globals.Value = globals;
		}

		#endregion

		#region Emit Support

		/// <summary>
		/// Returns 'FieldInfo' representing field in AutoGlobals for given global variable name.
		/// </summary>
		internal static FieldInfo GetFieldForVariable(VariableName name)
		{
			switch (name.ToString())
			{
				case AutoGlobals.CookieName:
					return Fields.AutoGlobals.Cookie;
				case AutoGlobals.EnvName:
					return Fields.AutoGlobals.Env;
				case AutoGlobals.FilesName:
					return Fields.AutoGlobals.Files;
				case AutoGlobals.GetName:
					return Fields.AutoGlobals.Get;
				case AutoGlobals.GlobalsName:
					return Fields.AutoGlobals.Globals;
				case AutoGlobals.PostName:
					return Fields.AutoGlobals.Post;
				case AutoGlobals.RequestName:
					return Fields.AutoGlobals.Request;
				case AutoGlobals.ServerName:
					return Fields.AutoGlobals.Server;
				case AutoGlobals.SessionName:
					return Fields.AutoGlobals.Session;
                case AutoGlobals.HttpRawPostDataName:
                    return Fields.AutoGlobals.HttpRawPostData;
				default:
					return null;
			}
		}

		#endregion
	}
}
