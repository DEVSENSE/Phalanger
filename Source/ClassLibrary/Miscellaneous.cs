/*

 Copyright (c) 2004-2006 Tomas Matousek.  

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

	TODO:
		- sleep() accepts negative values. (5.1.3) 
*/

using System;
//using System.Web;
using System.IO;
using System.Collections;
using System.ComponentModel;
using System.Threading;
using System.Reflection;
using PHP.Core;
using PHP.Core.Reflection;
using System.Collections.Generic;

#if SILVERLIGHT
using PHP.CoreCLR;
#endif

namespace PHP.Library
{
	#region Enumerations

	/// <exclude/>
	[Flags]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public enum PhpInfoSections
	{
		[ImplementsConstant("INFO_GENERAL")]
		General = PhpNetInfo.Sections.General,
		[ImplementsConstant("INFO_CREDITS")]
		Credits = PhpNetInfo.Sections.Credits,
		[ImplementsConstant("INFO_CONFIGURATION")]
		Configuration = PhpNetInfo.Sections.Configuration,
		[ImplementsConstant("INFO_MODULES")]
		Extensions = PhpNetInfo.Sections.Extensions,
		[ImplementsConstant("INFO_ENVIRONMENT")]
		Environment = PhpNetInfo.Sections.Environment,
		[ImplementsConstant("INFO_VARIABLES")]
		Variables = PhpNetInfo.Sections.Variables,
		[ImplementsConstant("INFO_LICENSE")]
		License = PhpNetInfo.Sections.License,
		[ImplementsConstant("INFO_ALL")]
		All = PhpNetInfo.Sections.All
	}

	/// <exclude/>
	[Flags]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public enum PhpCreditsSections
	{
		[ImplementsConstant("CREDITS_GROUP")]
		Group = 1,
		[ImplementsConstant("CREDITS_GENERAL")]
		General = 2,
		[ImplementsConstant("CREDITS_SAPI")]
		SAPI = 4,
		[ImplementsConstant("CREDITS_MODULES")]
		Modules = 8,
		[ImplementsConstant("CREDITS_DOCS")]
		Docs = 16,
		[ImplementsConstant("CREDITS_FULLPAGE")]
		Fullpage = 32,
		[ImplementsConstant("CREDITS_QA")]
		QA = 64,
		[ImplementsConstant("CREDITS_ALL")]
		All = -1
	}

	#endregion

	/// <summary>
	/// Miscellaneous functionality.
	/// </summary>
	/// <threadsafety static="true"/>
	public static class Misc
	{
		#region phpinfo, phpcredits, phpversion, version_compare, zend_version

		/// <summary>
		/// Shows all information about Phalanger.
		/// </summary>
		/// <returns>1.</returns>
		[ImplementsFunction("phpinfo")]
		public static int PhpInfo()
		{
			PhpNetInfo.Write(PhpNetInfo.Sections.All, ScriptContext.CurrentContext.Output);
			return 1;
		}

		/// <summary>
		/// Shows specific information about Phalanger.
		/// </summary>
		/// <param name="sections">A section to show.</param>
		/// <returns>1.</returns>
		[ImplementsFunction("phpinfo")]
		public static int PhpInfo(PhpInfoSections sections)
		{
			PhpNetInfo.Write((PhpNetInfo.Sections)sections, ScriptContext.CurrentContext.Output);
			return 1;
		}

		/// <summary>
		/// Shows all credits of Phalanger.
		/// </summary>
        /// <returns>True on success, or False on failure.</returns>
		[ImplementsFunction("phpcredits")]
		public static bool PhpCredits()
		{
			PhpNetInfo.Write(PhpNetInfo.Sections.Credits, ScriptContext.CurrentContext.Output);

            return true;
		}

		/// <summary>
		/// Shows all credits of Phalanger.
		/// </summary>
		/// <param name="sections">Ignored.</param>
        /// /// <returns>True on success, or False on failure.</returns>
		[ImplementsFunction("phpcredits")]
		public static bool PhpCredits(PhpCreditsSections sections)
		{
			PhpNetInfo.Write(PhpNetInfo.Sections.Credits, ScriptContext.CurrentContext.Output);

            return true;
		}

		/// <summary>
		/// Retrieves a string version of PHP language which features is supported by the Phalanger.
		/// </summary>
		/// <returns>PHP language version.</returns>
		[ImplementsFunction("phpversion"/*, FunctionImplOptions.Special*/)]
        [PureFunction]
        public static string PhpVersion()
		{
			return Core.PhpVersion.Current;
		}

#if !SILVERLIGHT
		/// <summary>
		/// Retrieves a string version of a specified extension.
		/// </summary>
		/// <returns>Version of the extension or <b>null</b> if it cannot be retrieved.</returns>
		[ImplementsFunction("phpversion")]
		[return: CastToFalse]
		public static string PhpVersion(string extensionName)
		{
			bool dummy;
			return Externals.GetModuleVersion(extensionName, true, out dummy);
		}
#endif

		/// <summary>
		/// Compares PHP versions.
		/// </summary>
		/// <param name="ver1">The first version.</param>
		/// <param name="ver2">The second version.</param>
		/// <returns>The result of comparison (-1,0,+1).</returns>
		[ImplementsFunction("version_compare"/*, FunctionImplOptions.Special*/)]
        [PureFunction]
        public static int VersionCompare(string ver1, string ver2)
		{
			return Core.PhpVersion.Compare(ver1, ver2);
		}

		/// <summary>
		/// Compares PHP versions using a specified operators.
		/// </summary>
		/// <param name="ver1">The first version.</param>
		/// <param name="ver2">The second version.</param>
		/// <param name="op">The operator to be used.</param>
		/// <returns>A boolean result of comparison or a <B>null</B> reference if the operator is invalid.</returns>
		[ImplementsFunction("version_compare"/*, FunctionImplOptions.Special*/)]
        [PureFunction]
        public static object VersionCompare(string ver1, string ver2, string op)
		{
			return Core.PhpVersion.Compare(ver1, ver2, op);
		}

		/// <summary>
		/// Gets the current version of Zend engine as it is defined in the currently supported PHP.
		/// </summary>
		/// <returns>The version.</returns>
		[ImplementsFunction("zend_version")]
        [PureFunction]
        public static string ZendVersion()
		{
			return Core.PhpVersion.Zend;
		}

		#endregion

        #region gethostname, php_uname, memory_get_usage, php_sapi_name

        /// <summary>
        /// gethostname() gets the standard host name for the local machine. 
        /// </summary>
        /// <returns>Returns a string with the hostname on success, otherwise FALSE is returned. </returns>
        [ImplementsFunction("gethostname")]
        [return: CastToFalse]
        public static string GetHostName()
        {
            string host = null;

#if !SILVERLIGHT
            try { host = System.Net.Dns.GetHostName(); }
            catch { }
#endif
            return host;
        }

        /// <summary>
		/// Retrieves full version information about OS.
		/// </summary>
		/// <returns>OS version.</returns>
		[ImplementsFunction("php_uname")]
		public static string PhpUName()
		{
			//return String.Concat(Environment.OSVersion,", CLR ",Environment.Version);
			return PhpUName(null);
		}

		/// <summary>
		/// Retrieves specific version information about OS.
		/// </summary>
		/// <param name="mode">
		/// <list type="bullet">
		/// <term>'a'</term><description>This is the default. Contains all modes in the sequence "s n r v m".</description>
		/// <term>'s'</term><description>Operating system name, e.g. "Windows NT", "Windows 9x".</description>
		/// <term>'n'</term><description>Host name, e.g. "www.php-compiler.net".</description>
		/// <term>'r'</term><description>Release name, e.g. "5.1".</description>
		/// <term>'v'</term><description>Version information. Varies a lot between operating systems, e.g. "build 2600".</description>
		/// <term>'m'</term><description>Machine type. eg. "i586".</description>
		/// </list>
		/// </param>
		/// <returns>OS version.</returns>
		[ImplementsFunction("php_uname")]
		public static string PhpUName(string mode)
		{
			string system, host, release, version, machine;

			switch (Environment.OSVersion.Platform)
			{
				case PlatformID.Win32NT: system = "Windows NT"; break;
				case PlatformID.Win32Windows: system = "Windows 9x"; break;
				case PlatformID.Win32S: system = "Win32S"; break;
				case PlatformID.WinCE: system = "Windows CE"; break;
				default: system = "Unix"; break;        // TODO
			}

#if !SILVERLIGHT
			host = System.Net.Dns.GetHostName();

			machine = Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE");
			if (machine == "x86") machine = "i586";    // TODO
#endif
			release = String.Concat(Environment.OSVersion.Version.Major, ".", Environment.OSVersion.Version.Minor);
			version = String.Concat("build ", Environment.OSVersion.Version.Build);

			if (mode != null && mode != "")
			{
				switch (mode[0])
				{
					case 's': return system;
					case 'r': return release;
					case 'v': return version;
#if !SILVERLIGHT
					case 'm': return machine;
					case 'n': return host;
#endif
				}
			}
#if !SILVERLIGHT
			return String.Format("{0} {1} {2} {3} {4}", system, host, release, version, machine);
#else
			return String.Format("{0} {1} {2}", system, release, version);
#endif
		}

#if !SILVERLIGHT
		/// <summary>
		/// Retrieves the size of the current process working set in bytes.
        /// (In PHP, Returns the amount of memory, in bytes, that's currently being allocated to your PHP script.)
		/// </summary>
		/// <returns>The size.</returns>
		[ImplementsFunction("memory_get_usage")]
		public static int MemoryGetUsage()
		{
            return MemoryGetUsage(false);
		}

        /// <summary>
        /// Retrieves the size of the current process working set in bytes.
        /// </summary>
        /// <param name="real_usage">
        /// "Set this to TRUE to get the real size of memory allocated from system.
        /// If not set or FALSE only the memory used by emalloc() is reported."</param>
        /// <returns>The size.</returns>
        [ImplementsFunction("memory_get_usage")]
        public static int MemoryGetUsage(bool real_usage)
        {
            //if (real_usage == false)// TODO: real_usage = false
            //    PhpException.ArgumentValueNotSupported("real_usage");

            long ws = System.Diagnostics.Process.GetCurrentProcess().WorkingSet64;
            if (ws > Int32.MaxValue) return Int32.MaxValue;
            return (int)ws;
        }


        /// <summary>
        /// Returns the peak of memory, in bytes, that's been allocated to the PHP script.
        /// </summary>
        /// <returns>The size.</returns>
        [ImplementsFunction("memory_get_peak_usage", FunctionImplOptions.NotSupported)]
        public static int MemoryGetPeakUsage()
        {
            return MemoryGetPeakUsage(false);
        }

        /// <summary>
        /// Returns the peak of memory, in bytes, that's been allocated to the PHP script.
        /// </summary>
        /// <param name="real_usage">
        /// Set this to TRUE to get the real size of memory allocated from system.
        /// If not set or FALSE only the memory used by emalloc() is reported.</param>
        /// <returns>The size.</returns>
        [ImplementsFunction("memory_get_peak_usage", FunctionImplOptions.NotSupported)]
        public static int MemoryGetPeakUsage(bool real_usage)
        {
            //if (real_usage == false)// TODO: real_usage = false
            //    PhpException.ArgumentValueNotSupported("real_usage");

            long ws = System.Diagnostics.Process.GetCurrentProcess().NonpagedSystemMemorySize64;    // can't get current thread's memory
            if (ws > Int32.MaxValue) return Int32.MaxValue;
            return (int)ws;
        }

		/// <summary>
		/// Returns the type of interface between web server and Phalanger. 
		/// </summary>
		/// <returns>The "isapi" string if runned under webserver (ASP.NET works via ISAPI) or "cli" otherwise.</returns>
		[ImplementsFunction("php_sapi_name")]
		public static string PhpSapiName()
		{
			return (System.Web.HttpContext.Current == null) ? "cli" : "isapi";
		}
#endif

		#endregion

		#region getmypid, getlastmod, get_current_user, (UNIX) getmyuid

#if !SILVERLIGHT
		/// <summary>
		/// Returns the PID of the current process. 
		/// </summary>
		/// <returns>The PID.</returns>
		[ImplementsFunction("getmypid")]
		public static int GetCurrentProcessId()
		{
			return System.Diagnostics.Process.GetCurrentProcess().Id;
		}


		/// <summary>
		/// Gets time of last page modification. 
		/// </summary>
		/// <returns>The UNIX timestamp or -1 on error.</returns>
		[ImplementsFunction("getlastmod")]
		public static int GetLastModification()
		{
			try
			{
				PhpSourceFile file = ScriptContext.CurrentContext.MainScriptFile;
				if (file == null) return -1;

				return DateTimeUtils.UtcToUnixTimeStamp(File.GetLastWriteTime(file.FullPath).ToUniversalTime());
			}
			catch (System.Exception)
			{
				return -1;
			}
		}


		/// <summary>
		/// Gets the name of the current user.
		/// </summary>
		/// <returns>The name of the current user.</returns>
		[ImplementsFunction("get_current_user")]
		public static string GetCurrentUser()
		{
			return Environment.UserName;
		}
#endif

		/// <summary>
		/// Not supported.
		/// </summary>
		/// <returns>Zero.</returns>
        [ImplementsFunction("getmyuid", FunctionImplOptions.NotSupported)]
		public static int GetCurrentUserId()
		{
			PhpException.FunctionNotSupported();
			return 0;
		}

		#endregion

		#region sleep, usleep, (UNIX) time_sleep_until, (UNIX) time_nanosleep

		/// <summary>
		/// Sleeps the current thread for a specified amount of time.
		/// </summary>
		/// <param name="seconds">The number of seconds to sleep.</param>
        /// <returns>Zero on success, or FALSE if negative argument is passed.</returns>
		[ImplementsFunction("sleep")]
        [return: CastToFalse]
		public static int sleep(int seconds)
		{
            if (seconds < 0)
            {
                PhpException.Throw(PhpError.Warning, LibResources.GetString("sleep_seconds_less_zero"));
                return -1;
            }

			Thread.Sleep(((long)seconds * 1000 > int.MaxValue) ? seconds = Timeout.Infinite : seconds * 1000);

            return 0;
		}

		/// <summary>
		/// Sleeps the current thread for a specified amount of time.
        /// No value is returned.
		/// </summary>
		/// <param name="microSeconds">The number of microseconds to sleep.</param>
		[ImplementsFunction("usleep")]
		public static void usleep(int microSeconds)
		{
			if (microSeconds < 0) microSeconds = 0;
			Thread.Sleep(microSeconds / 1000);
		}


		#endregion

		#region dl, extension_loaded, get_loaded_extensions, get_extension_funcs

		/// <summary>
		/// Not supported.
		/// </summary>
		/// <param name="library">Ignored.</param>
		/// <returns><B>false</B></returns>
		[ImplementsFunction("dl")]
		public static bool LoadExtension(string library)
		{
			PhpException.Throw(PhpError.Warning, LibResources.GetString("dl_not_supported"));
			return false;
		}

		/// <summary>
		/// Determines whether a native extension is loaded.
		/// </summary>
		/// <param name="extension">Internal extension name (e.g. <c>sockets</c>).</param>
		/// <returns><B>true</B> if the <paramref name="extension"/> is loaded, <B>false</B> otherwise.</returns>
		[ImplementsFunction("extension_loaded")]
        [PureFunction(typeof(Misc), "ExtensionLoaded_Analyze")]
        public static bool ExtensionLoaded(string extension)
		{
			return ScriptContext.CurrentContext.ApplicationContext.GetExtensionImplementor(extension) != null;
        }

        #region analyzer of extension_loaded

        public static bool ExtensionLoaded_Analyze(Analyzer/*!*/analyzer, string extension)
        {
            Debug.Assert(analyzer != null);

            foreach (var loadedExtension in analyzer.Context.ApplicationContext.GetLoadedExtensions())
                if (String.Compare(loadedExtension, extension, StringComparison.CurrentCultureIgnoreCase) == 0)
                    return true;

            return false;
        }

        #endregion

        /// <summary>
		/// Returns an array with names of all loaded native extensions.
		/// </summary>
		/// <returns>The array of extension names.</returns>
		[ImplementsFunction("get_loaded_extensions")]
		public static PhpArray GetLoadedExtensions()
		{
			PhpArray result = new PhpArray();

			foreach (string extension_name in ScriptContext.CurrentContext.ApplicationContext.GetLoadedExtensions())
				result.Add(extension_name);

			return result;
		}

		/// <summary>
		/// Returns an array with names of the functions of a native extension.
		/// </summary>
		/// <param name="extension">Internal extension name (e.g. <c>sockets</c>).</param>
		/// <returns>The array of function names or <B>null</B> if the <paramref name="extension"/> is not loaded.</returns>
		[ImplementsFunction("get_extension_funcs")]
		public static PhpArray GetExtensionFunctions(string extension)
		{
            if (extension == "zend")    // since PHP 5.0
            {
                PhpException.ArgumentValueNotSupported("extension", extension); // TODO: functions in the module zend (php functions in PhpNetCore ?)
                // ...
            }

			ApplicationContext app_context = ScriptContext.CurrentContext.ApplicationContext;

			PhpLibraryDescriptor desc = app_context.GetExtensionImplementor(extension);
			if (desc == null) return null;

			PhpArray result = new PhpArray();

			foreach (KeyValuePair<string, DRoutineDesc> function in app_context.Functions)
			{
				if (function.Value.DeclaringType.DeclaringModule == desc.Module)
				{
					result.Add(function.Key);
				}
			}

			return result;
		}

		/// <summary>
		/// A callback used by <see cref="GetExtensionFunctions"/> method. Adds a function to the resulting array as a key.
		/// </summary>
		private static bool AddFunctionToHashtable(MethodInfo info, ImplementsFunctionAttribute ifa, object result)
		{
			if ((ifa.Options & FunctionImplOptions.NotSupported) == 0)
				((Hashtable)result)[ifa.Name] = null;

			return true;
		}

		#endregion

		#region get_required_files, get_included_files

		/// <summary>
		/// Returns an array of included file paths.
		/// </summary>
		/// <returns>The array of paths to included files (without duplicates).</returns>
		[ImplementsFunction("get_required_files")]
		public static PhpArray GetRequiredFiles()
		{
			return GetIncludedFiles();
		}

		/// <summary>
		/// Returns an array of included file paths.
		/// </summary>
		/// <returns>The array of paths to included files (without duplicates).</returns>
		[ImplementsFunction("get_included_files")]
		public static PhpArray GetIncludedFiles()
		{
			PhpArray result = new PhpArray();

			foreach (var source_file in ScriptContext.CurrentContext.GetIncludedScripts())
			{
				result.Add(source_file/*.FullPath.ToString()*/);
			}
			return result;
		}

		#endregion

		#region NS: zend_logo_guid, php_logo_guid, (UNIX) getmygid, (UNIX) getmyinode

		/// <summary>
		/// Not supported.
		/// </summary>
		[ImplementsFunction("_mime_content_type", FunctionImplOptions.NotSupported)]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static string GetMimeContentType(string fileName)
		{
			PhpException.FunctionNotSupported();
			return "text/plain";
		}

		/// <summary>
		/// Not supported.
		/// </summary>
		[ImplementsFunction("zend_logo_guid", FunctionImplOptions.NotSupported)]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static string ZendLogoGuid()
		{
			PhpException.FunctionNotSupported();
			return null;
		}

		/// <summary>
		/// Not supported.
		/// </summary>
		[ImplementsFunction("php_logo_guid", FunctionImplOptions.NotSupported)]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static string PhpLogoGuid()
		{
			PhpException.FunctionNotSupported();
			return null;
		}

		/// <summary>
		/// Not supported.
		/// </summary>
		[ImplementsFunction("getmygid", FunctionImplOptions.NotSupported)]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static int GetMyGid()
		{
			PhpException.FunctionNotSupported();
			return 0;
		}

		/// <summary>
		/// Not supported.
		/// </summary>
		[ImplementsFunction("getmyinode", FunctionImplOptions.NotSupported)]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static int GetMyINode()
		{
			PhpException.FunctionNotSupported();
			return 0;
		}

		#endregion

        #region gc_enabled

        [ImplementsFunction("gc_enabled")]
        public static bool gc_enabled()
        {
            return true;    // status of the circular reference collector
        }

        #endregion
    }
}
