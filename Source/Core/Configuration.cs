/*

 Copyright (c) 2004-2006 Tomas Matousek.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/
using System;
using System.Threading;
using System.Text;
using System.Diagnostics;

namespace PHP.Core
{
	#region Language Features Enum

	/// <summary>
	/// PHP language features supported by Phalanger.
	/// </summary>
	[Flags]
	public enum LanguageFeatures
	{
		/// <summary>
		/// Basic features - always present.
		/// </summary>
		Basic = 0,

		/// <summary>
		/// Allows using short open tags in the script.
		/// </summary>
		ShortOpenTags = 1,

		/// <summary>
		/// Allows using ASP tags.
		/// </summary>
		AspTags = 2,

		/// <summary>
        /// Enables PHP5 keywords such as <c>private</c>, <c>protected</c>, <c>public</c>, <c>clone</c>, <c>goto</c>, , etc.
        /// Enables namespaces.
		/// </summary>
		V5Keywords = 4,

		/// <summary>
		/// Enables primitive type keywords <c>bool</c>, <c>int</c>, <c>int64</c>, <c>double</c>, <c>string</c>,
		/// <c>object</c>, <c>resource</c>.
		/// </summary>
		TypeKeywords = 8,

		/// <summary>
		/// Enables LINQ <c>from</c> keyword and LINQ context keywords <c>where</c>, <c>orderby</c>, <c>ascending</c>, 
		/// <c>descending</c>, <c>select</c>, <c>group</c>, <c>by</c>, <c>in</c>).
		/// </summary>
		Linq = 16,

        /// <summary>
		/// Enables Unicode escapes in strings (\U, \u, \C).
		/// </summary>
		UnicodeSemantics = 32,

		/// <summary>
		/// Allows to treat values of PHP types as CLR objects (e.g. $s = "string"; $s->GetHashCode()).
		/// </summary>
		ClrSemantics = 64,

        /// <summary>
        /// Enables PHP keywords that may be used in C# as class or namespace name, to be used in PHP code too.
        /// E.g. "List", "Array", "Abstract", ... would not be treated as syntax error when used as a <c>namespace_name_identifier</c> token.
        /// </summary>
        CSharpTypeNames = 128,

		/// <summary>
		/// Features enabled by default in the standard mode. Corresponds to the currently supported version of PHP 
		/// <seealso cref="PhpVersion.Current"/>.
		/// </summary>
		Default = Php5,

		/// <summary>
		/// Features enabled by default in the pure mode. Corresponds to the PHP/CLR language.
		/// </summary>
		PureModeDefault = PhpClr,

		Php4 = ShortOpenTags,
		Php5 = Php4 | V5Keywords,
        PhpClr = Php5 | UnicodeSemantics | TypeKeywords | Linq | ClrSemantics | AspTags | CSharpTypeNames
	}

	#endregion

	// library configuration

	#region Library Configuration Interface

	/// <summary>
	/// Interface implemented by each configuration record.
	/// </summary>
	public interface IPhpConfiguration
	{
		/// <summary>
		/// Makes a deep copy of the configuration record. 
		/// </summary>
		/// <returns>The deep copy.</returns>
		/// <remarks>
        /// Immutable fields such are those of types <see cref="string"/>, <see cref="System.Text.RegularExpressions.Regex"/>, etc.
		/// needn't to be copied deeply, of course. If your configuration record contains immutable 
		/// fields only you may implement this method simply by <see cref="Object.MemberwiseClone"/> which 
		/// is the usual case.
		/// </remarks>
		IPhpConfiguration DeepCopy();
	}

	#endregion

	#region Library Configurations Section

#if !SILVERLIGHT
	[Serializable]
#endif
    [DebuggerNonUserCode]
    internal sealed class LibraryConfigurationsSection
	{
		public bool IsConfigurationLoaded { get { return _configurations != null; } }

		/// <summary>
		/// A list of library configurations. Can contain <B>null</B> references. Initialized after configuration is read.
		/// </summary>
		private IPhpConfiguration[] _configurations = null;

		internal void SetConfigurations(IPhpConfiguration[]/*!*/ configurations)
		{
			Debug.Assert(!IsConfigurationLoaded && configurations != null);
			this._configurations = configurations;
		}

		/// <summary>
		/// Gets a configuration record for a specified library.
		/// </summary>
		/// <param name="descriptor">The descriptor of the library.</param>
		/// <returns>The configuration record or <B>null</B> if there is no record for the library.</returns>
		/// <exception cref="InvalidOperationException">Configuration not loaded.</exception>
		public IPhpConfiguration GetConfig(PhpLibraryDescriptor/*!*/ descriptor)
		{
			if (!IsConfigurationLoaded)
				throw new InvalidOperationException(CoreResources.GetString("configuration_not_loaded"));

			// libraries loaded after closing the configuration have greater unique indices:
			return (descriptor.UniqueIndex < _configurations.Length) ? _configurations[descriptor.UniqueIndex] : null;
		}

		/// <summary>
		/// Makes a deep copy of all library configurations.
		/// </summary>
		public LibraryConfigurationsSection DeepCopy()
		{
			LibraryConfigurationsSection result = new LibraryConfigurationsSection();

			if (IsConfigurationLoaded)
			{
				result._configurations = new IPhpConfiguration[_configurations.Length];
				for (int i = 0; i < _configurations.Length; i++)
				{
					if (this._configurations[i] != null)
						result._configurations[i] = _configurations[i].DeepCopy();
				}
			}
			return result;
		}
	}

	#endregion

	// configuration (shared funcs.) - loading is in separate files

	#region Local Configuration

	/// <summary>
	/// The configuration record containing the configuration applicable by user code (PhpPages,ClassLibrary).
	/// </summary>  
    [DebuggerNonUserCode]
    public sealed partial class LocalConfiguration : IPhpConfiguration
	{
		#region Output Control

		/// <summary>
		/// Output control options.
		/// </summary>
		public sealed partial class OutputControlSection : IPhpConfigurationSection
		{
			/// <summary>
			/// Whether to start output buffering on the beginning of each script execution. 
			/// </summary>
			public bool OutputBuffering { get { return outputBuffering; } }
			private bool outputBuffering = false;

			/// <summary>
			/// A user function which will filter buffered output.
			/// </summary>
			public PhpCallback OutputHandler { get { return outputHandler; } }
			private PhpCallback outputHandler = null;

			/// <summary>
			/// Whether to send output to a client after a call of any function which generates output.
			/// </summary>
			public bool ImplicitFlush { get { return implicitFlush; } }
			public bool implicitFlush = false;

            /// <summary>
            /// Overrides <see cref="System.Web.HttpResponse.Charset"/> if not <c>null</c>.
            /// </summary>
            public string CharSet { get { return this.charSet; } }
            private string charSet = null;

            /// <summary>
            /// Overrides <see cref="System.Web.HttpResponse.ContentType"/> if not <c>null</c>.
            /// </summary>
            public string ContentType { get { return this.contentType; } }
            private string contentType = null;

			internal OutputControlSection DeepCopy()
			{
				return (OutputControlSection)MemberwiseClone();
			}
		}

		#endregion

		#region Error Control

		/// <summary>
		/// Error control options.
		/// </summary>
        [DebuggerNonUserCode]
        public sealed partial class ErrorControlSection : IPhpConfigurationSection
		{
			/// <summary>
			/// Whether to display errors as a part of the output.
			/// </summary>
			public bool DisplayErrors = true;

			/// <summary>
			/// Which errors are reported.
			/// </summary>
			public PhpErrorSet ReportErrors { get { return reportErrors; } set { reportErrors = value; } }
			private PhpErrorSet reportErrors = PhpErrorSet.All;

			/// <summary>
			/// Whether to ignore operator @. <B>true</B> will cause to report errors regardless of the operator @.
			/// </summary>
			public bool IgnoreAtOperator = false;

			/// <summary>
			/// Whether error messages are reported in HTML format or plain text.
			/// </summary>
			public bool HtmlMessages = true;

			/// <summary>
			/// <see cref="Uri"/> specifying the root of PHP manual used in error messages.
			/// </summary>
			public Uri DocRefRoot = new Uri("http://www.php.net/manual");

			/// <summary>
			/// An extension of PHP manual documents (should start with a dot character '.').
			/// </summary>
			public string DocRefExtension = ".php";

			/// <summary>
			/// User defined callback which is called to handle an error.
			/// </summary>
			public PhpCallback UserHandler = null;

			/// <summary>
			/// Error which would cause calling user-defined error handler.
			/// </summary>
			public PhpError UserHandlerErrors = (PhpError)PhpErrorSet.None;

			/// <summary>
			/// User defined callback which is called to handle an exception.
			/// </summary>
			public PhpCallback UserExceptionHandler = null;

			/// <summary>
			/// Whether to log errors to the system Event Log if logging is enabled.
			/// </summary>
			public bool SysLog = false;

			/// <summary>
			/// A file where to log errors if logging is enabled. Empty value means errors are not logged into a file.
			/// </summary>
            public string LogFile = null;

            /// <summary>
            /// Ensures the path is rooted.
            /// </summary>
            /// <param name="value">LogFile value.</param>
            /// <param name="node">Configuration element.</param>
            private static string AbsolutizeLogFile(string value, System.Xml.XmlNode/*!*/node)
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    return null;
                }
                else
                {
                    if (System.IO.Path.IsPathRooted(value))
                    {
                        return value;
                    }
                    else
                    {
                        // relative path provided, make it rooted to config directory
                        return System.IO.Path.Combine(System.IO.Path.GetDirectoryName(ConfigUtils.GetConfigXmlPath(node.OwnerDocument)), value);
                    }
                }
            }

			/// <summary>
			/// Whether to log errors.
			/// </summary>
			public bool EnableLogging = false;

			/// <summary>
			/// String which will be prepended to an error message if it is about to be displayed.
			/// </summary>
			public string ErrorPrependString = null;

			/// <summary>
			/// String which will be appended to an error message if it is about to be displayed.
			/// </summary>
			public string ErrorAppendString = null;

			/// <summary>
			/// Copies values to the target structure.
			/// </summary>
			internal ErrorControlSection DeepCopy()
			{
				return (ErrorControlSection)this.MemberwiseClone();
			}
		}

		#endregion

		#region Request Control

		/// <summary>
		/// Request control options.
		/// </summary>
		public sealed partial class RequestControlSection : IPhpConfigurationSection
		{
			/// <summary>
			/// Timeout in seconds for each phase of user code execution. There are up to three phases: 
			/// script main execution, shutdown callbacks execution, and session close handler execution.
			/// Each phase is aborted if the specified time elapses. The next phase is executed then (if any).
			/// Works also for console and Windows applications. 
			/// Non-positive values are treated as no timeout (<see cref="Int32.MaxValue"/>).
			/// </summary>
#if SILVERLIGHT
			public int ExecutionTimeout = 0;
#else
			public int ExecutionTimeout = (System.Web.HttpContext.Current != null) ? 30 : 0;
#endif

			/// <summary>
			/// Gets execution timeout in microseconds.
			/// </summary>
			internal long ExecutionTimeoutForTimer
			{
				get { return (ExecutionTimeout <= 0) ? Timeout.Infinite : (long)ExecutionTimeout * 1000; }
			}

			/// <summary>
			/// Whether not to abort on client disconnection.
			/// </summary>
			public bool IgnoreUserAbort = true;


			internal RequestControlSection DeepCopy()
			{
				return (RequestControlSection)MemberwiseClone();
			}
		}

		#endregion

		#region Assertion

		/// <summary>
		/// Assertion options.
		/// </summary>
		public sealed partial class AssertionSection : IPhpConfigurationSection
		{
			/// <summary>
			/// Whether to evaluate assertions.
			/// </summary>
			public bool Active = true;

			/// <summary>
			/// Whether a warning should be reported on failed assertion.
			/// </summary>
			public bool ReportWarning = true;

			/// <summary>
			/// Whether to terminate script execution on failed assertion.
			/// </summary>
			public bool Terminate = false;

			/// <summary>
			/// Whether assertion evaluation should report errors (including parse errors).
			/// </summary>
			public bool Quiet = false;

			/// <summary>
			/// User callback called on failed assertion. Should have 3 parameters. Can be a <B>null</B> reference.
			/// </summary>
			public PhpCallback Callback = null;

			/// <summary>
			/// Copies values to the target structure.
			/// </summary>
			internal AssertionSection DeepCopy()
			{
				return (AssertionSection)this.MemberwiseClone();
			}
		}

		#endregion

		#region Variables

		/// <summary>
		/// Variables handling options.
		/// </summary>
		public sealed partial class VariablesSection : IPhpConfigurationSection
		{
            ///// <summary>
            ///// Whether to emulate Zend Engine 1 behavior.
            ///// </summary>
            //public bool ZendEngineV1Compatible = false;

			/// <summary>
			/// Whether to quote values returned from some PHP functions.
			/// </summary>
			public bool QuoteRuntimeVariables = false;

			/// <summary>
			/// Whether to quote values in Sybase DB manner, i.e. using '' instead of \'.
			/// </summary>
			public readonly bool QuoteInDbManner = false;

			/// <summary>
			/// User callback called on failed serialization. Can be empty.
			/// </summary>
			public PhpCallback DeserializationCallback = null;

            /// <summary>
            /// Always populate the $HTTP_RAW_POST_DATA containing the raw POST data.
            /// However, the preferred method for accessing the raw POST data is php://input.
            /// $HTTP_RAW_POST_DATA is not available with enctype="multipart/form-data". 
            /// </summary>
            public bool AlwaysPopulateRawPostData = false;

			/// <summary>
			/// The order in which global will be added to <see cref="AutoGlobals.Globals"/> and 
			/// <see cref="AutoGlobals.Request"/> arrays. Can contain only a permutation of "EGPCS" string.
			/// </summary>
			public string/*!*/ RegisteringOrder
			{
				get { return registeringOrder; }
				set { if (ValidateRegisteringOrder(value)) registeringOrder = value; }
			}
			private string/*!*/ registeringOrder = "EGPCS";

            /// <summary>
			/// Checks whether a specified value is global valid variables registering order.
			/// </summary>
			/// <param name="value">The value.</param>
			/// <returns>Whether <paramref name="value"/> contains a permutation of "EGPCS".</returns>
			public static bool ValidateRegisteringOrder(string value)
			{
				if (value == null || value.Length != AutoGlobals.EgpcsCount) return false;

				int present = 0;
				for (int i = 0; i < value.Length; i++)
				{
					switch (value[i])
					{
						case 'E': if ((present & 1) != 0) return false; present |= 1; break;
						case 'G': if ((present & 2) != 0) return false; present |= 2; break;
						case 'P': if ((present & 4) != 0) return false; present |= 4; break;
						case 'C': if ((present & 8) != 0) return false; present |= 8; break;
						case 'S': if ((present & 16) != 0) return false; present |= 16; break;
						default: return false;
					}
				}
				return true;
			}

			/// <summary>
			/// Copies values to the target structure.
			/// </summary>
			internal VariablesSection DeepCopy()
			{
				return (VariablesSection)this.MemberwiseClone();
			}
		}

		#endregion

		#region File System

		/// <summary>
		/// File system functions options.
		/// </summary>
		public sealed partial class FileSystemSection : IPhpConfigurationSection
		{
			/// <summary>
			/// Whether file names can be specified as URL (and thus allows to use streams).
			/// </summary>
			public bool AllowUrlFopen = true;

			/// <summary>
			/// A user agent to send when communicating as client over HTTP.
			/// </summary>
			public string UserAgent = null;

			/// <summary>
			/// Default timeout for socket based streams.
			/// </summary>
			public int DefaultSocketTimeout = 60;

			/// <summary>
			/// A default file open mode used when it is not specified in <c>fopen</c> function explicitly. 
			/// You can specify either "b" for binary mode or "t" for text mode. Any other value is treated as
			/// if there is no default value.
			/// </summary>
			public string DefaultFileOpenMode = "b";

			/// <summary>
			/// A password used when logging to FTP server as an anonymous client.
			/// </summary>
			public string AnonymousFtpPassword = null;

			/// <summary>
			/// A list of semicolon-separated separated by ';' where file system functions and dynamic 
			/// inclusion constructs searches for files. A <B>null</B> or an empty string means empty list.
			/// </summary>
			public string IncludePaths = ".";

			/// <summary>
			/// Copies values to the target structure.
			/// </summary>
			internal FileSystemSection DeepCopy()
			{
				return (FileSystemSection)this.MemberwiseClone();
			}
		}

		#endregion

		#region Session

		/// <summary>
		/// Session management configuration independent of a particular session handler.
		/// </summary>
		public sealed partial class SessionSection : IPhpConfigurationSection
		{
			/// <summary>
			/// Whether a session is started automatically before script execution.
			/// </summary>
			/// <remarks>
			/// This value should rather be a part of the global configuration since it
			/// can't be changed BCL functions. Its potential change by another code 
			/// have no effect either. The value is stated here to all session handling
			/// settings together.
			/// </remarks>
			public bool AutoStart = false;

			/// <summary>
			/// Makes a deep copy.
			/// </summary>
			internal SessionSection DeepCopy()
			{
				return (SessionSection)MemberwiseClone();
			}
		}

		#endregion

		#region Library Configuration

		/// <summary>
		/// Gets a configuration associated with the specified library.
		/// </summary>
		/// <param name="descriptor">The library descriptor.</param>
		/// <returns>The configuration.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="descriptor"/> is a <B>null</B> reference.</exception>
		/// <exception cref="InvalidOperationException">Configuration has not been initialized yet.</exception>
		public IPhpConfiguration GetLibraryConfig(PhpLibraryDescriptor descriptor)
		{
			if (descriptor == null)
				throw new ArgumentNullException("descriptor");

			return Library.GetConfig(descriptor);
		}

		#endregion

		#region Construction, Copying, Validation

		public readonly OutputControlSection OutputControl;
		public readonly ErrorControlSection ErrorControl;
		public readonly RequestControlSection RequestControl;
		public readonly FileSystemSection FileSystem;
		public readonly AssertionSection Assertion;
		public readonly VariablesSection Variables;
		public readonly SessionSection Session;
		internal readonly LibraryConfigurationsSection Library;

		/// <summary>
		/// Creates an instance of <see cref="LocalConfiguration"/> initialized by default values.
		/// </summary>
		public LocalConfiguration()
		{
			OutputControl = new OutputControlSection();
			ErrorControl = new ErrorControlSection();
			RequestControl = new RequestControlSection();
			FileSystem = new FileSystemSection();
			Assertion = new AssertionSection();
			Variables = new VariablesSection();
			Session = new SessionSection();
			Library = new LibraryConfigurationsSection();

            LastConfigurationModificationTime = DateTime.MinValue;
		}

		/// <summary>
		/// Creates an instance of <see cref="LocalConfiguration"/> initialized by values 
		/// copied from the specified instance.
		/// </summary>
		/// <param name="source"></param>
		private LocalConfiguration(LocalConfiguration source)
		{
			this.OutputControl = source.OutputControl.DeepCopy();
			this.ErrorControl = source.ErrorControl.DeepCopy();
			this.RequestControl = source.RequestControl.DeepCopy();
			this.FileSystem = source.FileSystem.DeepCopy();
			this.Assertion = source.Assertion.DeepCopy();
			this.Variables = source.Variables.DeepCopy();
			this.Session = source.Session.DeepCopy();
			this.Library = source.Library.DeepCopy();

            LastConfigurationModificationTime = source.LastConfigurationModificationTime;
		}

		/// <summary>
		/// Creates a copy of the configuration.
		/// </summary>
		/// <returns>The copy.</returns>
		public IPhpConfiguration DeepCopy()
		{
			return new LocalConfiguration(this);
		}

		public void Validate()
		{
		}

		#endregion

        #region Properties

        /// <summary>
        /// .config file (set of .config files) latest modification time.
        /// If it cannot be determined, it is equal to <see cref="DateTime.MinValue"/>.
        /// </summary>
        public DateTime LastConfigurationModificationTime { get; internal set; }

        #endregion
	}

	#endregion

	#region Compiler Configuration

	/// <summary>
	/// Groups configuration related to the compiler. 
	/// Includes <see cref="ApplicationConfiguration.CompilerSection"/> and 
	/// <see cref="ApplicationConfiguration.GlobalizationSection"/>
	/// sections of global configuration record. 
	/// Used for passing configuration for the purpose of compilation.
	/// </summary>
	public sealed partial class CompilerConfiguration
	{
		#region Fields & Constructor

		/// <summary>
		/// Compiler section.
		/// </summary>
		public readonly ApplicationConfiguration.CompilerSection/*!*/ Compiler;

		/// <summary>
		/// Globalization section.
		/// </summary>
		public readonly ApplicationConfiguration.GlobalizationSection/*!*/ Globalization;

#if !SILVERLIGHT
		/// <summary>
		/// Paths section. Not modified.
		/// </summary>
		private readonly ApplicationConfiguration.PathsSection/*!*/ Paths;
#endif

		/// <summary>
		/// Creates a new compiler configuration as a shallow copy of 
		/// the relevant sections of the global configuration record.
		/// </summary>
		/// <param name="app">Application configuration record.</param>
		/// <exception cref="ArgumentNullException"><paramref name="app"/> is a <B>null</B> reference.</exception>
		public CompilerConfiguration(ApplicationConfiguration/*!*/ app)
		{
			if (app == null)
				throw new ArgumentNullException("app");

			this.Compiler = app.Compiler;
			this.Globalization = app.Globalization;
#if !SILVERLIGHT
			this.Paths = app.Paths;
#endif
		}

		#endregion

		#region Validation

		/// <summary>
		/// Checks whether the configuration data are valid and complete and fills missing information 
		/// by defaults or throws an exception.
		/// </summary>
        /// <exception cref="System.Configuration.ConfigurationErrorsException">Configuration is invalid or incomplete.</exception>
		public void Validate()
		{
			Compiler.Validate();
			Globalization.Validate();
		}

		#endregion
	}

	#endregion

	#region Application Configuration

	/// <summary>
	/// The configuration containing per-application configuration. 
	/// The confguration can be defined only in Machine.config and 
	/// some can be changed also in Web.config files in the appliciation root directory or above.
	/// </summary>
    [DebuggerNonUserCode]
    public sealed partial class ApplicationConfiguration
	{
		#region Compiler

		/// <summary>
		/// Compiler options.
		/// </summary>
        [DebuggerNonUserCode]
        public sealed partial class CompilerSection : IPhpConfigurationSection
		{
			#region Debug

			/// <summary>
			/// Whether to compile scripts in debug mode.
			/// </summary>
			public bool Debug 
			{ 
				get { return debug; } 
				set 
				{ 
					debug = value; 
#if !SILVERLIGHT
					dirty = true;
#endif
				} 
			}
			internal bool debug;

			#endregion

			#region Inclusions

			/// <summary>
			/// Gets a path to source root directory (cannot be empty). 
			/// A directory to which relative source paths hardcoded into the application IL 
			/// are absolutized. An application virtual directory in case of Web applications.
			/// The directory can be set in a console application configuration if the user
			/// wants to display another source path then the one used for compilation.
			/// </summary>
			/// <exception cref="Exception">A value being set is not a valid path.</exception>
			public FullPath SourceRoot
			{
				get
				{
					return sourceRoot;
				}
				set
				{
					if (value.IsEmpty)
						throw new ArgumentException("value");     // TODO

					SourceRootSet = true;
					sourceRoot = value;
				}
			}
			private FullPath sourceRoot;             // GENERICS: non-nullable

			/// <summary>
			/// Whether source root was set (otherwise it has a default value).
			/// </summary>
			internal bool SourceRootSet;

			/// <summary>
			/// Whether static inclusions are enabled.
			/// </summary>
			public bool? EnableStaticInclusions { get { return enableStaticInclusions; } set { enableStaticInclusions = value; } }
			private bool? enableStaticInclusions;

            #endregion

			#region Simple Options

			/// <summary>
			/// Gets whether <see cref="PHP.Core.LanguageFeatures.ShortOpenTags"/> feature is enabled.
			/// </summary>
			public bool ShortOpenTags { get { return (_languageFeatures & LanguageFeatures.ShortOpenTags) != 0; } }

			/// <summary>
			/// Gets whether <see cref="PHP.Core.LanguageFeatures.AspTags"/> feature is enabled.
			/// </summary>
			public bool AspTags { get { return (_languageFeatures & LanguageFeatures.AspTags) != 0; } }

			/// <summary>
			/// Gets whether <see cref="PHP.Core.LanguageFeatures.V5Keywords"/> feature is enabled.
			/// </summary>
			public bool V5Keywords { get { return (_languageFeatures & LanguageFeatures.V5Keywords) != 0; } }

            /// <summary>
			/// Gets whether <see cref="PHP.Core.LanguageFeatures.UnicodeSemantics"/> feature is enabled.
			/// </summary>
			public bool UnicodeSemantics { get { return (_languageFeatures & LanguageFeatures.UnicodeSemantics) != 0; } }

			/// <summary>
			/// Gets whether <see cref="PHP.Core.LanguageFeatures.TypeKeywords"/> feature is enabled.
			/// </summary>
			public bool TypeKeywords { get { return (_languageFeatures & LanguageFeatures.TypeKeywords) != 0; } }

			/// <summary>
			/// Gets whether <see cref="PHP.Core.LanguageFeatures.Linq"/> feature is enabled.
			/// </summary>
			public bool Linq { get { return (_languageFeatures & LanguageFeatures.Linq) != 0; } }

			/// <summary>
			/// Gets whether <see cref="PHP.Core.LanguageFeatures.Linq"/> feature is enabled.
			/// </summary>
			public bool ClrSemantics { get { return (_languageFeatures & LanguageFeatures.ClrSemantics) != 0; } }

			/// <summary>
			/// Enabled PHP language features.
			/// </summary>
			public LanguageFeatures LanguageFeatures
			{
				get { return _languageFeatures; }
				set { _languageFeatures = value; languageFeaturesSet = true; }
			}
			private LanguageFeatures _languageFeatures = LanguageFeatures.Default;

			/// <summary>
			/// Whether the <see cref="LanguageFeatures"/> has been set after initialized to the default value.
			/// </summary>
			public bool LanguageFeaturesSet { get { return languageFeaturesSet; } }
			private bool languageFeaturesSet = false;

			/// <summary>
			/// Compiler warnings which should not be reported.
			/// </summary>
			public WarningGroups DisabledWarnings { get { return disabledWarnings; } set { disabledWarnings = value; } }
			private WarningGroups disabledWarnings;

			/// <summary>
			/// Numbers of disabled warnings.
			/// </summary>
			public int[]/*!*/ DisabledWarningNumbers { get { return disabledWarningNumbers; } set { disabledWarningNumbers = value; } }
			private int[]/*!*/ disabledWarningNumbers;

            /// <summary>
            /// Whether to treat warnings as errors, so code containing warnings won't be allowed to be compiled or executed.
            /// </summary>
            public bool TreatWarningsAsErrors { get; set; }

			#endregion

			#region Validation

			/// <summary>
			/// Validates configuration. Throws an exception if any option is invalid.
			/// </summary>
			internal void Validate()
			{
			}

			#endregion
		}

		#endregion

		#region Globalization

		/// <summary>
		/// Configuration related to culture.
		/// </summary>
		public sealed partial class GlobalizationSection : IPhpConfigurationSection
		{
			#region Fields & Validation

			/// <summary>
			/// Source code encoding.
			/// </summary>
			public Encoding PageEncoding { get { return pageEncoding; } set { pageEncoding = value; } }
			private Encoding pageEncoding;

			internal void Validate()
			{
			}

			#endregion
		}

		#endregion

		#region Construction and Validation

		/// <summary>
		/// Creates a new instance.
		/// </summary>
		public ApplicationConfiguration()
		{
#if !SILVERLIGHT
			Paths = new PathsSection();
#endif
			Compiler = new CompilerSection();
			Globalization = new GlobalizationSection();
		}

		/// <summary>
		/// If extensions are installed all paths should be defined.
		/// </summary>
		internal void ValidateNoLock()
		{
			// validate only if not yet validated:
			if (!isLoaded)
			{
#if !SILVERLIGHT
				Paths.Validate();
#endif
				Compiler.Validate();
				Globalization.Validate();
				isLoaded = true;
			}
		}

		#endregion

		#region Loading

		/// <summary>
		/// Whether application configuration record has been loaded.
		/// </summary>
		public bool IsLoaded { get { return isLoaded; } }
		private volatile bool isLoaded = false;

		#endregion

        #region Properties

        public readonly CompilerSection Compiler;
        public readonly GlobalizationSection Globalization;
#if !SILVERLIGHT
        public readonly PathsSection Paths;
#endif

        /// <summary>
        /// .config file (set of .config files) latest modification time.
        /// If it cannot be determined, it is equal to <see cref="DateTime.MinValue"/>.
        /// </summary>
        public DateTime LastConfigurationModificationTime
        {
            get
            {
                return Paths.LastConfigurationModificationTime;
            }
        }

        #endregion
    }

	#endregion

	#region Global Configuration

	/// <summary>
	/// The configuration containing script independent configuration options.
	/// Options are directory dependent - each application subdirectory can define settings applicable for its content.
	/// </summary>
	public sealed partial class GlobalConfiguration : IPhpConfiguration
	{
		#region GlobalVariables

		/// <summary>
		/// Global variables handling options.
		/// </summary>
		public sealed partial class GlobalVariablesSection : IPhpConfigurationSection
		{
			/// <summary>
			/// Whether or not to register the EGPCS variables as global variables.
			/// </summary>
			public bool RegisterGlobals = false;

			/// <summary>
			/// Whether or not to register the "argc" and "argv" variables as global variables.
			/// </summary>
			public bool RegisterArgcArgv = false;

			/// <summary>
			/// Whether or not to register the "HTTP_*_VARS" arrays as global variables.
			/// </summary>
			public bool RegisterLongArrays = false;

            /// <summary>
            /// Whether to quote GET/POST/Cookie variables' values when they are added to respective global arrays.
            /// </summary>
            public readonly bool QuoteGpcVariables = false;

			internal GlobalVariablesSection DeepCopy()
			{
				return (GlobalVariablesSection)MemberwiseClone();
			}
		}

		#endregion

		#region Library Configuration

		/// <summary>
		/// Gets a configuration associated with the specified library.
		/// </summary>
		/// <param name="descriptor">The library descriptor.</param>
		/// <returns>The configuration.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="descriptor"/> is a <B>null</B> reference.</exception>
		/// <exception cref="InvalidOperationException">Configuration has not been initialized yet.</exception>
		public IPhpConfiguration GetLibraryConfig(PhpLibraryDescriptor/*!*/ descriptor)
		{
			if (descriptor == null)
				throw new ArgumentNullException("descriptor");

			return Library.GetConfig(descriptor);
		}

		#endregion

		#region Construction, Copying, Validation

		public readonly GlobalVariablesSection GlobalVariables;
		internal readonly LibraryConfigurationsSection Library;

#if !SILVERLIGHT
		public readonly PostedFilesSection PostedFiles;
		public readonly SafeModeSection SafeMode;
#endif

		/// <summary>
		/// Creates an instance of <see cref="GlobalConfiguration"/> initialized by default values.
		/// </summary>		
		public GlobalConfiguration()
		{
			GlobalVariables = new GlobalVariablesSection();
			Library = new LibraryConfigurationsSection();
#if !SILVERLIGHT
			PostedFiles = new PostedFilesSection();
			SafeMode = new SafeModeSection();
#endif
            this.LastConfigurationModificationTime = DateTime.MinValue;
		}

		/// <summary>
		/// Creates an instance of <see cref="GlobalConfiguration"/> initialized by values 
		/// copied from the specified instance.
		/// </summary>
		/// <param name="source">The configuration from which to copy values.</param>
		private GlobalConfiguration(GlobalConfiguration/*!*/ source)
		{
			Debug.Assert(source != null);

			this.GlobalVariables = source.GlobalVariables.DeepCopy();
			this.Library = source.Library.DeepCopy();
#if !SILVERLIGHT
			this.PostedFiles = source.PostedFiles.DeepCopy();
			this.SafeMode = source.SafeMode.DeepCopy();
#endif
            this.LastConfigurationModificationTime = source.LastConfigurationModificationTime;
		}

		/// <summary>
		/// Creates a copy of the configuration.
		/// </summary>
		/// <returns>The copy.</returns>
		public IPhpConfiguration/*!*/ DeepCopy()
		{
			return new GlobalConfiguration(this);
		}

		/// <summary>
		/// Checks whether the configuration data are valid and complete and fills missing information 
		/// by defaults or throws an exception.
		/// </summary>
        /// <exception cref="System.Configuration.ConfigurationErrorsException">Configuration is invalid or incomplete.</exception>
		internal void Validate()
		{
		}

		#endregion

        #region Properties

        /// <summary>
        /// .config file (set of .config files) latest modification time.
        /// If it cannot be determined, it is equal to <see cref="DateTime.MinValue"/>.
        /// </summary>
        public DateTime LastConfigurationModificationTime { get; internal set; }

        #endregion
    }

	#endregion
}
