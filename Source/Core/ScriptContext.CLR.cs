/*

 Copyright (c) 2004-2006 Tomas Matousek and Ladislav Prosek.
  
 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.IO;
using System.Web;
//using System.Web.SessionState;
using System.Text;
using System.Threading;
using System.Reflection;
using System.Diagnostics;
using System.Globalization;
using System.Collections;
//using System.Collections.Specialized;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.ComponentModel;
using PHP.Core.Emit;
using PHP.Core.Reflection;
using System.Configuration;

namespace PHP.Core
{
	/// <summary>
	/// The context of an executing script. Contains data associated with a request.
	/// </summary>
	[DebuggerTypeProxy(typeof(ScriptContext.DebugView))]
	public sealed partial class ScriptContext : MarshalByRefObject, ILogicalThreadAffinative
	{
		#region Initialization of requests and applications

		/// <summary>
		/// Initializes the script context for a web request.
		/// </summary>
		/// <param name="appContext">Application context.</param>
		/// <param name="context">HTTP context of the request.</param>
		/// <returns>A instance of <see cref="ScriptContext"/> to be used by the request.</returns>
		/// <exception cref="System.Configuration.ConfigurationErrorsException">
		/// Web configuration is invalid. The context is not initialized then.
		/// </exception>
		internal static ScriptContext/*!*/ InitWebRequest(ApplicationContext/*!*/ appContext, HttpContext/*!*/ context)
		{
			Debug.Assert(appContext != null && context != null);

			// reloads configuration of the current thread from ASP.NET caches or web.config files;
			// cached configuration is reused;
			Configuration.Reload(appContext, false);

			// takes a writable copy of a global configuration (may throw ConfigurationErrorsException):
			LocalConfiguration config = (LocalConfiguration)Configuration.DefaultLocal.DeepCopy();

            // following initialization statements shouldn't throw an exception:    // can throw on Integrated Pipeline, events must be attached within HttpApplication.Init()

			ScriptContext result = new ScriptContext(appContext, config, context.Response.Output, context.Response.OutputStream);

			result.IsOutputBuffered = config.OutputControl.OutputBuffering;
			result.ThrowExceptionOnError = true;
			result.WorkingDirectory = Path.GetDirectoryName(context.Request.PhysicalPath);
            if (config.OutputControl.ContentType != null) context.Response.ContentType = config.OutputControl.ContentType;
            if (config.OutputControl.CharSet != null) context.Response.Charset = config.OutputControl.CharSet;

			result.AutoGlobals.Initialize(config, context);

			ScriptContext.CurrentContext = result;

			Externals.BeginRequest();

			return result;
		}

        /// <summary>
        /// Creates a new script context and runs the application in it. For internal use only.
        /// </summary>
        /// <param name="mainRoutine">The script's main helper routine.</param>
        /// <param name="relativeSourcePath">A path to the main script source file.</param>
        /// <param name="sourceRoot">A source root within which an application has been compiler.</param>
        [Emitted, EditorBrowsable(EditorBrowsableState.Never)]
        public static void RunApplication(Delegate/*!*/ mainRoutine, string relativeSourcePath, string sourceRoot)
        {
            bool is_pure = mainRoutine is RoutineDelegate;

            ApplicationContext app_context = ApplicationContext.Default;

            // default culture:
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            // try to preload configuration (to prevent exceptions during InitApplication:
            try
            {
                Configuration.Load(app_context);
            }
            catch (ConfigurationErrorsException e)
            {
                Console.WriteLine(e.Message);
                return;
            }

            ApplicationConfiguration app_config = Configuration.Application;

            if (is_pure && !app_config.Compiler.LanguageFeaturesSet)
                app_config.Compiler.LanguageFeatures = LanguageFeatures.PureModeDefault;

            // environment settings; modifies the PATH variable to fix LoadLibrary called by native extensions:
            if (EnvironmentUtils.IsDotNetFramework)
            {
                string path = Environment.GetEnvironmentVariable("PATH");
                path = String.Concat(path, Path.PathSeparator, app_config.Paths.ExtNatives);

                Environment.SetEnvironmentVariable("PATH", path);
            }

            Type main_script;
            if (is_pure)
            {
                // loads the calling assembly:
                app_context.AssemblyLoader.Load(mainRoutine.Method.Module.Assembly, null);
                main_script = null;
            }
            else
            {
                main_script = mainRoutine.Method.DeclaringType;
                app_context.AssemblyLoader.LoadScriptLibrary(System.Reflection.Assembly.GetEntryAssembly(), ".");
            }

            ScriptContext context = InitApplication(app_context, main_script, relativeSourcePath, sourceRoot);

            try
            {
                context.GuardedCall<object, object>(context.GuardedMain, mainRoutine, true);
                context.GuardedCall<object, object>(context.FinalizeBufferedOutput, null, false);
                context.GuardedCall<object, object>(context.ProcessShutdownCallbacks, null, false);
                context.GuardedCall<object, object>(context.FinalizePhpObjects, null, false);
            }
            finally
            {
                Externals.EndRequest();
            }
        }

        /// <summary>
        /// Initializes the script context for a PHP console application.
        /// </summary>
        /// <param name="appContext">Application context.</param>
        /// <param name="mainScript">The main script's type or a <B>null</B> reference for a pure application.</param>
        /// <param name="relativeSourcePath">A path to the main script source file.</param>
        /// <param name="sourceRoot">A source root within which an application has been compiler.</param>
        /// <returns>
        /// A new instance of <see cref="ScriptContext"/> with its own copy of local configuration 
        /// to be used by the application.
        /// </returns>
        /// <exception cref="System.Configuration.ConfigurationErrorsException">
        /// Web configuration is invalid. The context is not initialized then.
        /// </exception>
        /// <remarks>
        /// Use this method if you want to initialize application in the same way the PHP console/Windows 
        /// application is initialized. The returned script context is initialized as follows:
        /// <list type="bullet">
        ///   <term>The application's source root is set.</term>
        ///   <term>The main script of the application is defined.</term>
        ///   <term>Output and input streams are set to standard output and input, respectively.</term>
        ///   <term>Current culture it set to <see cref="CultureInfo.InvariantCulture"/>.</term>
        ///   <term>Auto-global variables ($_GET, $_SET, etc.) are initialized.</term>
        ///   <term>Working directory is set tothe current working directory.</term>
        /// </list>
        /// </remarks>
        public static ScriptContext/*!*/ InitApplication(ApplicationContext/*!*/ appContext, Type mainScript,
            string relativeSourcePath, string sourceRoot)
        {
            // loads configuration into the given application context 
            // (applies only if the config has not been loaded yet by the current thread):
            Configuration.Load(appContext);

            ApplicationConfiguration app_config = Configuration.Application;

            if (mainScript != null)
            {
                if (relativeSourcePath == null)
                    throw new ArgumentNullException("relativeSourcePath");

                if (sourceRoot == null)
                    throw new ArgumentNullException("sourceRoot");

                // overrides source root configuration if not explicitly specified in config file:
                if (!app_config.Compiler.SourceRootSet)
                    app_config.Compiler.SourceRoot = new FullPath(sourceRoot);
            }

            // takes a writable copy of a global configuration:
            LocalConfiguration config = (LocalConfiguration)Configuration.DefaultLocal.DeepCopy();

            // sets invariant culture as a default one:
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            ScriptContext result = new ScriptContext(appContext, config, Console.Out, Console.OpenStandardOutput());

            result.IsOutputBuffered = result.config.OutputControl.OutputBuffering;
            result.AutoGlobals.Initialize(config, null);
            result.WorkingDirectory = Directory.GetCurrentDirectory();
            result.ThrowExceptionOnError = true;
            result.config.ErrorControl.HtmlMessages = false;

            if (mainScript != null)
            {
                // converts relative path of the script source to full canonical path using source root from the configuration:
                PhpSourceFile main_source_file = new PhpSourceFile(
                    app_config.Compiler.SourceRoot,
                    new FullPath(relativeSourcePath, app_config.Compiler.SourceRoot)
                );

                result.DefineMainScript(new ScriptInfo(mainScript), main_source_file);
            }

            ScriptContext.CurrentContext = result;

            Externals.BeginRequest();

            return result;
        }

		#endregion

		#region Constants

		private void InitConstants(DualDictionary<string, object> _constants)
		{
            // Thease constants are here, because they are environment dependent
            // When the code is compiled and assembly is run on another platforms they could be different

            _constants.Add("PHALANGER", PhalangerVersion.Current, false);
            _constants.Add("PHP_VERSION", PhpVersion.Current, false);
            _constants.Add("PHP_MAJOR_VERSION", PhpVersion.Major, false);
            _constants.Add("PHP_MINOR_VERSION", PhpVersion.Minor, false);
            _constants.Add("PHP_RELEASE_VERSION", PhpVersion.Release, false);
            _constants.Add("PHP_VERSION_ID", PhpVersion.Major * 10000 + PhpVersion.Minor * 100 + PhpVersion.Release, false);
            _constants.Add("PHP_EXTRA_VERSION", PhpVersion.Extra, false);
            _constants.Add("PHP_OS", Environment.OSVersion.Platform == PlatformID.Win32NT ? "WINNT" : "WIN32", false); // TODO: GENERICS (Unix)
            _constants.Add("PHP_SAPI", (System.Web.HttpContext.Current == null) ? "cli" : "isapi", false);
            _constants.Add("DIRECTORY_SEPARATOR", FullPath.DirectorySeparatorString, false);
            _constants.Add("PATH_SEPARATOR", Path.PathSeparator.ToString(), false);

            //TODO: should be specified elsewhere (app context??)
            _constants.Add("PHP_EOL", System.Environment.NewLine, false);

            //TODO: this is a bit pesimistic, as this value is a bit higher on Vista and on other filesystems and OSes
            //      sadly .NET does not specify value of MAXPATH constant
            _constants.Add("PHP_MAXPATHLEN", 255, false);

            if (HttpContext.Current == null)
            {
                _constants.Add("STDIN", InputOutputStreamWrapper.In, false);
                _constants.Add("STDOUT", InputOutputStreamWrapper.Out, false);
                _constants.Add("STDERR", InputOutputStreamWrapper.Error, false);
            }
		}

		#endregion

		#region Current Context

		/// <summary>
		/// Call context name for the current <see cref="ScriptContext"/>.
		/// </summary>
		private const string callContextSlotName = "PhpNet:ScriptContext";

		/// <summary>
		/// The instance of <see cref="ScriptContext"/> associated with the current logical thread.
		/// </summary>
		/// <remarks>
		/// If no instance is associated with the current logical thread
		/// a new one is created, added to call context and returned. 
		/// The slot allocated by some instance is freed
		/// by setting this property to a <B>null</B> reference.
		/// </remarks>
        [DebuggerNonUserCode]
		public static ScriptContext CurrentContext
		{
			[Emitted]
			get
			{
				// try to get script context from call context:
                // ScriptContext is ILogicalThreadAffinative, LogicalCallContext is used.
				try
				{
                    return ((ScriptContext)CallContext.GetData(callContextSlotName)) ?? CreateDefaultScriptContext();   // on Mono, .GetData must be used (GetLogicalData is not implemented)
				}
				catch (InvalidCastException)
				{
					throw new InvalidCallContextDataException(callContextSlotName);
				}

				//return result.AttachToHttpApplication();
			}
			set
			{
				if (value == null)
					CallContext.FreeNamedDataSlot(callContextSlotName);
				else
                    CallContext.SetData(callContextSlotName, value);            // on Mono, .SetData must be used (SetLogicalData is not implemented)
			}
		}

        /// <summary>
        /// Initialize new ScriptContext and store it into the LogicalCallContext.
        /// </summary>
        /// <returns>Newly created ScriptContext.</returns>
        private static ScriptContext CreateDefaultScriptContext()
        {
            ScriptContext result;
            HttpContext context;

            if ((context = HttpContext.Current) != null)
                result = RequestContext.Initialize(ApplicationContext.Default, context).ScriptContext;
            else
                result = new ScriptContext(ApplicationContext.Default);

            ScriptContext.CurrentContext = result;

            return result;
        }

		#endregion

		#region Variables

		public PhpArray/*!*/ SessionVariables
		{
			get
			{
				PhpArray result = AutoGlobals.Session.Value as PhpArray;
				if (result == null)
					AutoGlobals.Session.Value = result = new PhpArray();

				return result;
			}
		}

		#endregion

		#region Inclusions

        /// <summary>
        /// Includes a specific script using current configuration.
        /// </summary>
        /// <param name="relativeSourcePath">Source root relative path to the script.</param>
        /// <param name="once">Specifies whether script should be included only once.</param>
        /// <returns>The value returned by the global code of the target script.</returns>
        public object Include(string/*!*/ relativeSourcePath, bool once)
        {
            ApplicationConfiguration app_config = Configuration.Application;

            // searches for file:
            FullPath included_full_path = SearchForIncludedFile(PhpError.Error, relativeSourcePath, FullPath.Empty);
            if (included_full_path.IsEmpty) return false;

            ScriptInfo info;
            bool already_included = scripts.TryGetValue(included_full_path.ToString(), out info);

            // skips inclusion if script has already been included and inclusion's type is "once":
            if (already_included)
            {
                if(once)
                    return ScriptModule.SkippedIncludeReturnValue;

                // script type loaded, info cannot be null
            }
            else
            {
                PhpSourceFile included_source_file = new PhpSourceFile(app_config.Compiler.SourceRoot, included_full_path);
                
                // loads script type:
                info = LoadDynamicScriptType(included_source_file);

                // script not found:
                if (info == null)
                    return false;

                if (MainScriptFile == null)
                    // the first script becomes the main one:
                    DefineMainScript(info, included_source_file);
                else
                    // adds included file into the script list
                    scripts.Add(included_full_path.ToString(), info);
            }

            Debug.Assert(info != null);

            return GuardedCall((ScriptInfo scriptInfo) =>
            {
                //return PhpScript.InvokeMainHelper(
                //    (Type)scriptType,
                return scriptInfo.Main(
                    this,
                    null,  // no local variables
                    null,  // no object context
                    null,  // no class context
                    true);
            }, info, true);
        }

		/// <summary>
		/// Performs PHP inclusion on a specified script. 
		/// </summary>
		/// <param name="relativeSourcePath">
		/// Path to the target script source file relative to the application source root 
		/// (see <c>Configuration.Application.Compiler.SourceRoot</c>.
		/// </param>
        /// <param name="script">
		/// Script type (i.e. type called <c>Default</c> representing the target script) or any type from 
		/// the assembly where the target script is contained (useful for multi-script assemblies, where script types are 
		/// not directly available from C# as they have mangled names). In the latter case, the script type is searched in the 
		/// assembly using value of <paramref name="relativeSourcePath"/>.
		/// </param>
		/// <returns>The value returned by the global code of the target script.</returns>
		/// <remarks>
		/// <para>
		/// The inclusion inheres in adding the target to the list of included scripts on the current script context
		/// (see <c>ScriptContext.Scripts</c> and in a call to the global code of the target script.
		/// </para>
		/// </remarks>
		/// <exception cref="InvalidOperationException">Request context has been disposed.</exception>
		/// <exception cref="ArgumentNullException"><paramref name="relativeSourcePath"/> or <paramref name="script"/> are <B>null</B> references.</exception>
		/// <exception cref="ArgumentException">Script type cannot be resolved.</exception>
		/// <exception cref="InvalidScriptAssemblyException">The target assembly is not a valid Phalanger compiled assembly.</exception>
		internal object IncludeScript(string/*!*/ relativeSourcePath, ScriptInfo/*!*/ script)
		{
            //if (type == null)
            //    throw new ArgumentNullException("type");
            if (relativeSourcePath == null)
				throw new ArgumentNullException("relativeSourcePath");
            if (script == null)
                throw new ArgumentException("script");

			FullPath source_root = Configuration.Application.Compiler.SourceRoot;
			PhpSourceFile source_file = new PhpSourceFile(
				new FullPath(source_root),
				new FullPath(Path.Combine(source_root, relativeSourcePath)));

            // the first script becomes the main one:
			if (MainScriptFile == null)
				DefineMainScript(script, source_file);

            return GuardedCall((ScriptInfo scriptInfo) =>
            {
                //return PhpScript.InvokeMainHelper(
                //    (Type)scriptType,
                return scriptInfo.Main(
                    this,
                    null,  // no local variables
                    null,  // no object context
                    null,  // no class context
                    true);
            }, script, true);
		}

        ///// <summary>
        ///// Resolves the script type using the given <see cref="ApplicationContext"/>.
        ///// </summary>
        //private static Type ResolveScriptType(ApplicationContext/*!*/ applicationContext, PhpSourceFile/*!*/ sourceFile, Type/*!*/ type)
        //{
        //    if (PhpScript.IsScriptType(type))
        //        return type;

        //    ScriptAssembly sa = ScriptAssembly.LoadFromAssembly(applicationContext, type.Assembly);

        //    return sa.GetScriptType(sourceFile);
        //}

		/// <summary>
		/// Called in place where a script is statically included. For internal purposes only.
		/// </summary>
        /// <param name="level">RelativePath.level; <paramref name="relativeSourcePath"/>.</param>
		/// <param name="relativeSourcePath">RelativePath.path; A path to the included script's source file relative to source root.</param>
		/// <param name="includee">A type handle of the included script.</param>
		/// <param name="inclusionType">A type of an inclusion.</param>
		/// <returns>Whether to process inclusion. If <B>false</B>, inclusion should be ignored.</returns>
		[Emitted, EditorBrowsable(EditorBrowsableState.Never)]
		public bool StaticInclude(int level, string relativeSourcePath, RuntimeTypeHandle includee, InclusionTypes inclusionType)
		{
			ApplicationConfiguration app_config = Configuration.Application;

            var included_full_path =
            //PhpSourceFile source_file = new PhpSourceFile(
            //	app_config.Compiler.SourceRoot,
                new FullPath(app_config.Compiler.SourceRoot, new RelativePath((sbyte)level, relativeSourcePath));
			//);

            if (scripts.ContainsKey(included_full_path.ToString()))
			{
				// the script has been included => returns whether it should be included again:
				return !InclusionTypesEnum.IsOnceInclusion(inclusionType);
			}
			else
			{
				// the script has not been included yet:
                scripts.Add(included_full_path.ToString(), new ScriptInfo(Type.GetTypeFromHandle(includee)));

				// the script should be included:
				return true;
			}
		}

		/// <summary>
		/// Called in place where a script is dynamically included. For internal purposes only.
		/// </summary>
		/// <param name="includedFilePath">A source path to the included script.</param>
		/// <param name="includerFileRelPath">A source path to the script issuing the inclusion relative to the source root.</param>
		/// <param name="variables">A run-time variables table.</param>
		/// <param name="self">A current object in which method an include is called (if applicable).</param>
		/// <param name="includer">A current class type desc in which method an include is called (if applicable).</param>
		/// <param name="inclusionType">A type of an inclusion.</param>
		/// <returns>A result of the Main() method call.</returns>
		[Emitted, EditorBrowsable(EditorBrowsableState.Never)]
		public object DynamicInclude(
			string includedFilePath,
			string includerFileRelPath,
			Dictionary<string, object> variables,
			DObject self,
			DTypeDesc includer,
			InclusionTypes inclusionType)
		{
			ApplicationConfiguration app_config = Configuration.Application;

			// determines inclusion behavior:
			FullPath includer_full_path = new FullPath(includerFileRelPath, app_config.Compiler.SourceRoot);

			// searches for file:
			FullPath included_full_path = SearchForIncludedFile(
                InclusionTypesEnum.IsMustInclusion(inclusionType) ? PhpError.Error : PhpError.Warning,
                includedFilePath, includer_full_path);

			if (included_full_path.IsEmpty) return false;

			ScriptInfo info;
            bool already_included = scripts.TryGetValue(included_full_path.ToString(), out info);

			// skips inclusion if script has already been included and inclusion's type is "once":
            if (already_included && InclusionTypesEnum.IsOnceInclusion(inclusionType))
				return ScriptModule.SkippedIncludeReturnValue;

			if (!already_included)
			{
				// loads script type:
                info = LoadDynamicScriptType(new PhpSourceFile(app_config.Compiler.SourceRoot, included_full_path));

				// script not found:
				if (info == null) return false;

				// adds included file into the script list
                scripts.Add(included_full_path.ToString(), info/* = new ScriptInfo(script)*/);
			}

			return info.Main(this, variables, self, includer, false);
		}

        /// <summary>
		/// Searches for a file in the script library, current directory, included paths, and web application root respectively.
		/// </summary>
		/// <param name="errorSeverity">A severity of an error (if occures).</param>
		/// <param name="includedPath">A source path to the included script.</param>
		/// <param name="includerFullPath">Full source path to the including script.</param>
		/// <returns>Full path to the file or <B>null</B> path if not found.</returns>
		private FullPath SearchForIncludedFile(PhpError errorSeverity, string includedPath, FullPath includerFullPath)
		{
            FullPath result;

			string message;
            
            //
            // construct the delegate checking the script existance
            //

            var file_exists = applicationContext.BuildFileExistsDelegate();            

            //
            // try to find the script
            //

            if (file_exists != null)
            {
                string includer_directory = includerFullPath.IsEmpty ? WorkingDirectory : Path.GetDirectoryName(includerFullPath);
                
                // searches for file in the following order: 
                // - incomplete absolute path => combines with RootOf(WorkingDirectory)
                // - relative path => searches in FileSystem.IncludePaths then in the includer source directory
                result = PhpScript.FindInclusionTargetPath(
                    new InclusionResolutionContext(
                        applicationContext,
                        includer_directory,
                        WorkingDirectory,
                        config.FileSystem.IncludePaths
                        ),
                    includedPath,
                    file_exists,
                    out message);
            }
            else
            {
                message = "Script cannot be included with current configuration.";   // there is no precompiled MSA available on non-web application
                result = FullPath.Empty;
            }

			// failure:
			if (result.IsEmpty)
			{
				PhpException.Throw(errorSeverity,
					CoreResources.GetString("script_inclusion_failed",
					includedPath,
					message,
					config.FileSystem.IncludePaths,
					WorkingDirectory));
			}

			return result;
		}

		/// <summary>
		/// Loads a script type dynamically.
		/// </summary>
		/// <param name="sourceFile">Script's source file.</param>
		private ScriptInfo LoadDynamicScriptType(PhpSourceFile/*!*/ sourceFile)
		{
            Debug.WriteLine("SC", "LoadDynamicScriptType: '{0}'", sourceFile);

            // runtime compiler manages:
            // - 1. script library
            // - 2. optionally bin/WebPages.dll
            // - 3. compiles file from file system if allowed

            return this.ApplicationContext.RuntimeCompilerManager.GetCompiledScript(sourceFile, RequestContext.CurrentContext);
		}

		#endregion

		#region Platform Dependent

		/// <summary>
		/// Stores HttpHeaders locally so PHP apps can change them (by default you can't change value 
		/// of already set http header, but this is possible in PHP)
		/// </summary>
		private HttpHeaders httpHeaders;
		public HttpHeaders Headers { get { return httpHeaders; } }

		void InitPlatformSpecific()
		{
            // HTTP headers implementation
			this.httpHeaders = HttpHeaders.Create();
		}

		#endregion

		#region Session Handling

		/// <summary>
		/// Adds session variables aliases to global variables.
		/// </summary>
		public void RegisterSessionGlobals()
		{
			PhpArray globals, session;

			// do not create session variables table if not exists:
			if ((session = PhpReference.AsPhpArray(AutoGlobals.Session)) == null)
				return;

			// creates globals array if it doesn't exists:
			globals = GlobalVariables;

			// iterates using unbreakable enumerator:
			foreach (KeyValuePair<IntStringKey, object> entry in session)
			{
				PhpReference php_ref = entry.Value as PhpReference;

				// converts the session variable to a reference if it is not one ("no duplicate pointers" rule preserved):
				if (php_ref == null)
					session[entry.Key] = php_ref = new PhpReference(entry.Value);

				// adds alias to the globals:
				globals[entry.Key] = php_ref;
			}
		}

		#endregion
	}
}
