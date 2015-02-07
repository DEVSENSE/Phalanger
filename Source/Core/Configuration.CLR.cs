/*

 Copyright (c) 2004-2006 Tomas Matousek.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/
using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Specialized;
using System.Configuration;
using System.Diagnostics;
using System.Threading;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.IO;
using System.Xml;
using System.Web;
using System.Web.Configuration;
using PHP.Core;
using System.Collections.Generic;
using PHP.Core.Reflection;

namespace PHP.Core
{
	// library configuration

	#region LibraryConfigStore

	/// <summary>
	/// Class that abstracts library configuration - in CLR it is wrapper over
	/// XmlAttributeCollection.
	/// </summary>
	public class LibraryConfigStore
	{
		public LibraryConfigStore(XmlNode node)
		{
			if (node != null) _attributes = node.Attributes;
		}

		XmlAttributeCollection _attributes;
		public XmlAttributeCollection Attributes { get { return _attributes; } }
	}

	#endregion

	#region Library Configuration Interface

	/// <summary>
	/// Interface implemented by all configuration sections loaded from XML config file.
	/// </summary>
	public interface IPhpConfigurationSection
	{
		/// <summary>
		/// Parses a configuration section node having attributes "name" and "value".
		/// </summary>
		/// <param name="name">A value of the "name" attribute.</param>
		/// <param name="value">A value of the "value" attribute.</param>
		/// <param name="node">The node.</param>
		/// <returns>
		/// Whether the node has been processed by implementor. Depends usually on the <paramref name="name"/> value.
		/// </returns>
		/// <exception cref="ConfigurationErrorsException">The value of <paramref name="value"/> is not valid.</exception>
		bool Parse(string name, string value, XmlNode node);
	}

	/// <summary>
	/// A base class for configuration contexts.
	/// </summary>
	public class ConfigContextBase       // GENERICS: <L,G> where L : IPhpConfiguration, new(), G: new()
	{
		/// <summary>
		/// Local configuration record or a <B>null</B> if not used by the library.
		/// </summary>
		public IPhpConfiguration Local;    // can be null

		/// <summary>
		/// Global configuration record or a <B>null</B> if not used by the library.
		/// </summary>
		public IPhpConfiguration Global;   // can be null

		/// <summary>
		/// Creates a configuration context.
		/// </summary>
		/// <param name="local">Local configuration record or a <B>null</B> reference.</param>
		/// <param name="global">Local configuration record or a <B>null</B> reference.</param>
		public ConfigContextBase(IPhpConfiguration local, IPhpConfiguration global)
		{
			this.Local = local;
			this.Global = global;
		}
	}

	#endregion

	// partial classes - loading (on CLR)

	#region Local Configuration

	/// <summary>
	/// The configuration record containing the configuration applicable by user code (PhpPages,ClassLibrary).
	/// </summary>  
	[Serializable]
	public sealed partial class LocalConfiguration : IPhpConfiguration
	{
		#region Output Control

		/// <summary>
		/// Output control options.
		/// </summary>
		[Serializable]
		public sealed partial class OutputControlSection : IPhpConfigurationSection
		{
			/// <summary>
			/// Loads configuration from XML node.
			/// </summary>
			public bool Parse(string name, string value, XmlNode node)
			{
				switch (name)
				{
					case "OutputBuffering":
                        {
                            int ivalue;
                            if (int.TryParse(value, out ivalue))
                                outputBuffering = ivalue != 0;
						    else
                                outputBuffering = (value == "true");
                        }
						break;

					case "OutputHandler":
						outputHandler = (value != "") ? new PhpCallback(value) : null;
						break;

					case "ImplicitFlush":
						implicitFlush = value == "true";
						break;

					case "ContentType":
                        this.contentType = (value != "") ? value : null;
						break;

					case "Charset":
						this.charSet = (value != "") ? value : null;
						break;

					default:
						return false;
				}
				return true;
			}
		}

		#endregion

		#region Error Control

		/// <summary>
		/// Error control options.
		/// </summary>
		[Serializable]
		public sealed partial class ErrorControlSection : IPhpConfigurationSection
		{
			/// <summary>
			/// Loads configuration from XML node.
			/// </summary>
			public bool Parse(string name, string value, XmlNode node)
			{
				bool t = value == "true";

				switch (name)
				{
					case "ReportErrors":
						ReportErrors = (PhpErrorSet)ConfigUtils.ParseFlags(node, (int)ReportErrors, typeof(PhpError));
						break;

					case "UserHandler": UserHandler = (value != String.Empty) ? new PhpCallback(value) : null; break;
					case "UserExceptionHandler": UserExceptionHandler = (value != String.Empty) ? new PhpCallback(value) : null; break;
					case "DisplayErrors": DisplayErrors = t; break;
					case "LogFile": LogFile = AbsolutizeLogFile(value, node); break;
					case "EnableLogging": EnableLogging = t; break;
					case "SysLog": SysLog = t; break;
					case "ErrorPrependString": ErrorPrependString = value; break;
					case "ErrorAppendString": ErrorAppendString = value; break;
					case "HtmlMessages": HtmlMessages = t; break;
					case "IgnoreAtOperator": IgnoreAtOperator = t; break;
					case "DocRefExtension": DocRefExtension = value; break;

					case "DocRefRoot":
						try
						{
							DocRefRoot = (value != "") ? new Uri(value) : null;
						}
						catch (UriFormatException e)
						{
							throw new ConfigurationErrorsException(e.Message, node);
						}
						break;

					default:
						return false;
				}
				return true;
			}
		}

		#endregion

		#region Request Control

		/// <summary>
		/// Request control options.
		/// </summary>
		[Serializable]
		public sealed partial class RequestControlSection : IPhpConfigurationSection
		{
			/// <summary>
			/// Loads configuration from XML node.
			/// </summary>
			public bool Parse(string name, string value, XmlNode node)
			{
				switch (name)
				{
					case "ExecutionTimeout":
						ExecutionTimeout = ConfigUtils.ParseInteger(value, 0, Int32.MaxValue, node);
						break;

					case "IgnoreUserAbort":
						IgnoreUserAbort = value == "true";
						break;

					default:
						return false;
				}
				return true;
			}
		}

		#endregion

		#region Assertion

		/// <summary>
		/// Assertion options.
		/// </summary>
		[Serializable]
		public sealed partial class AssertionSection : IPhpConfigurationSection
		{
			/// <summary>
			/// Loads configuration from XML node.
			/// </summary>
			public bool Parse(string name, string value, XmlNode node)
			{
				switch (name)
				{
					case "Callback": Callback = (value != String.Empty) ? new PhpCallback(value) : null; break;
					case "Active": Active = value == "true"; break;
					case "ReportWarning": ReportWarning = value == "true"; break;
					case "Terminate": Terminate = value == "true"; break;
					case "Quiet": Quiet = value == "true"; break;

					default:
						return false;
				}
				return true;
			}
		}

		#endregion

		#region Variables

		/// <summary>
		/// Variables handling options.
		/// </summary>
		[Serializable]
		public sealed partial class VariablesSection : IPhpConfigurationSection
		{
			/// <summary>
			/// Loads configuration from XML node.
			/// </summary>
			public bool Parse(string name, string value, XmlNode node)
			{
				switch (name)
				{
                    case "ZendEngineV1Compatible": throw new NotSupportedException(name); // ZendEngineV1Compatible = (value == "true"); break;
					case "QuoteRuntimeVariables": QuoteRuntimeVariables = (value == "true"); break;
                    case "QuoteInDbManner": /*QuoteInDbManner =*/ if (value == "true") throw new ConfigUtils.InvalidAttributeValueException(node, "value"); break;
					case "DeserializationCallback": DeserializationCallback = (value != String.Empty) ? new PhpCallback(value) : null; break;
                    case "AlwaysPopulateRawPostData": AlwaysPopulateRawPostData = (value == "true"); break;

					case "RegisteringOrder":

						if (!ValidateRegisteringOrder(value))
							throw new ConfigurationErrorsException(CoreResources.GetString("invalid_registering_order"), node);

						RegisteringOrder = value;
						break;

					default:
						return false;
				}
				return true;
			}
		}

		#endregion

		#region File System

		/// <summary>
		/// File system functions options.
		/// </summary>
		[Serializable]
		public sealed partial class FileSystemSection : IPhpConfigurationSection
		{
			/// <summary>
			/// Loads configuration from XML node.
			/// </summary>
			public bool Parse(string name, string value, XmlNode node)
			{
				switch (name)
				{
					case "AllowUrlFopen":
						AllowUrlFopen = value == "true";
						break;

					case "UserAgent":
						UserAgent = value;
						break;

					case "AnonymousFtpPassword":
						AnonymousFtpPassword = value;
						break;

					case "IncludePaths":
						IncludePaths = value;
						break;

					case "DefaultSocketTimeout":
						DefaultSocketTimeout = ConfigUtils.ParseInteger(value, 0, Int32.MaxValue, node);
						break;

					case "DefaultFileOpenMode":
						switch (value)
						{
							case "b":
							case "t": DefaultFileOpenMode = value; break;
							case "": DefaultFileOpenMode = null; break;
							default:
								throw new ConfigUtils.InvalidAttributeValueException(node, name);
						}
						break;

					default:
						return false;
				}
				return true;
			}
		}
		
		#endregion

		#region Session

		/// <summary>
		/// Session management configuration independent of a particular session handler.
		/// </summary>
		[Serializable]
		public sealed partial class SessionSection : IPhpConfigurationSection
		{
			/// <summary>
			/// A handler providing persistence for session variables.
			/// Can't contain a <B>null</B> reference. Setting the <B>null</B> reference will assign the default handler 
			/// (<see cref="AspNetSessionHandler.Default"/>).
			/// </summary>
			public SessionHandler/*!*/Handler
			{
				get
				{
                    if (handler == null)
                    {
                        // lazily initialize the session handler:
                        if (handler_getter != null)
                        {
                            handler = handler_getter() ?? handler;    // keep old one if handler_getter fails and handler is not null
                            handler_getter = null;  // drop the getter closure
                        }

                        if (handler == null)
                            handler = AspNetSessionHandler.Default;
                    }

                    return handler;
				}
				set
				{
					handler = value;
                    handler_getter = null;
				}
			}
			private SessionHandler handler = null;

            /// <summary>
            /// One-time handler initializer function.
            /// </summary>
            private Func<SessionHandler> handler_getter = null;

            /// <summary>
            /// url_rewriter.tags specifies which HTML tags are rewritten to include session id
            /// if transparent sid support is enabled.
            /// Defaults to a=href,area=href,frame=src,input=src,form=,fieldset=  
            ///  
            /// The Dictionary contains the pair of ("HTML Element", "Attribute name").
            /// Keys and values are in lower case.
            /// 
            /// Cannot be null.
            /// </summary>
            public Dictionary<string,string[]>/*!!*/UrlRewriterTags
            {
                get
                {
                    if (urlRewriterTags == null)
                    {
                        urlRewriterTags = new Dictionary<string, string[]>()
                        {
                            { "a", new string[]{"href"}},
                            { "area", new string[]{"href"}},
                            { "frame", new string[]{"src"}},
                            { "input", new string[]{"src"}},
                            { "form", ArrayUtils.EmptyStrings},
                            { "fieldset", ArrayUtils.EmptyStrings}
                        };
                    }
                    return urlRewriterTags;
                }
                set
                {
                    urlRewriterTags = value;
                }
            }

            private Dictionary<string, string[]> urlRewriterTags = null;

			/// <summary>
			/// Loads configuration from XML node.
			/// </summary>
			public bool Parse(string name, string value, XmlNode node)
			{
				switch (name)
				{
					case "AutoStart":
						AutoStart = value == "true";
						return true;

					case "Handler":
						{
                            // postpone this step until sessions are actually used, also not all the handlers are loaded yet
                            this.handler_getter = () =>
                                {
                                    SessionHandler handler = SessionHandlers.GetHandler(value);
                                    if (handler == null)
                                        PhpException.Throw(PhpError.Warning, string.Format(CoreResources.unknown_session_handler, value));
                                    
                                    return handler;
                                };
							return true;
						}
                    case "UrlRewriterTags":
                        {
                            var newUrlRewriterTags = new Dictionary<string, string[]>();

                            // value = "a=href,area=href,frame=src,input=src,form=,fieldset="
                            string[] tags = value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                            // enumerate list of tags
                            foreach (string tag in tags)
                            {
                                int ass = tag.IndexOf('='); // find the assignment

                                if (ass >= 1)   // there is at least one character before the assignment
                                {
                                    string tagName = tag.Remove(ass).ToLower();
                                    string tagValue = tag.Substring(ass + 1).ToLower();

                                    string[] attrs = null;
                                    newUrlRewriterTags.TryGetValue(tagName, out attrs);
                                    newUrlRewriterTags[tagName] = ArrayUtils.Concat(attrs, tagValue);
                                }
                            }

                            this.UrlRewriterTags = newUrlRewriterTags;
                            
                            return true;
                        }
				}
				return false;
			}
		}

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
	[Serializable]
	public sealed partial class CompilerConfiguration
	{
		#region Loading

        /// <summary>
        /// Class libraries collected while parsing configuration files.
        /// </summary>
        private LibrariesConfigurationList/*!*/addedLibraries = new LibrariesConfigurationList();

        /// <summary>
        /// Load class libraries collected while parsing configuration files.
        /// </summary>
        /// <param name="appContext"></param>
        internal void LoadLibraries(ApplicationContext/*!*/ appContext)
        {
            addedLibraries.LoadLibrariesNoLock(
                    (_assemblyName, _assemblyUrl, _sectionName, /*!*/ _node) =>
                    {
                        appContext.AssemblyLoader.Load(_assemblyName, _assemblyUrl, new LibraryConfigStore(_node));
                        return true;
                    },
                    null // ignore class library sections
                    );
        }

		/// <summary>
		/// Parses a XML node and loads the configuration values from it.
		/// </summary>
		/// <param name="applicationContext">Context where to load libraries.</param>
		/// <param name="section">The "phpNet" section node.</param>
        /// <param name="addedLibraries">List of class libraries to be loaded lazily.</param>
        internal void Parse(ApplicationContext/*!*/ applicationContext, XmlNode/*!*/ section, LibrariesConfigurationList/*!*/addedLibraries)
		{
			// parses XML tree:
			foreach (XmlNode node in section.ChildNodes)
			{
				if (node.NodeType == XmlNodeType.Element)
				{
					switch (node.Name)
					{
						case ConfigurationSectionHandler.NodeClassLibrary:
							ConfigUtils.ParseLibraryAssemblyList(
                                node,
                                addedLibraries,
								Paths.ExtWrappers,
								Paths.Libraries);
							break;

                        case ConfigurationSectionHandler.NodeScriptLibrary:
                            ConfigUtils.ParseScriptLibraryAssemblyList(node, applicationContext.ScriptLibraryDatabase);
                            break;

						case ConfigurationSectionHandler.NodeCompiler:
							ConfigUtils.ParseNameValueList(node, null, Compiler);
							break;

						case ConfigurationSectionHandler.NodeGlobalization:
							ConfigUtils.ParseNameValueList(node, null, Globalization);
							break;
					}
				}
			}
		}

		private void ParseSystemWebSection(XmlNode/*!*/ section)
		{
			foreach (XmlNode node in section.ChildNodes)
			{
				if (node.NodeType == XmlNodeType.Element)
				{
					switch (node.Name)
					{
						case "globalization":
							string value = ConfigUtils.OptionalAttribute(node, "fileEncoding");

							if (value != null)
							{
								try
								{
									Globalization.PageEncoding = (value != String.Empty) ? Encoding.GetEncoding(value) : Encoding.Default;
								}
								catch (Exception e)
								{
									throw new ConfigurationErrorsException(e.Message, node);
								}
							}
							break;
					}
				}
			}
		}


		/// <summary>
		/// Loads compiler configuration values from a specified .config file into a given record.
		/// </summary>
		/// <param name="appContext">Application context where to load libraries.</param>
		/// <param name="path">A full path to the .config file.</param>
		/// <returns>The new configuration record.</returns>
		/// <exception cref="ConfigurationErrorsException">An error in configuration.</exception>
        public void LoadFromFile(ApplicationContext/*!*/ appContext, FullPath path)
		{
			if (appContext == null)
				throw new ArgumentNullException("appContext");

			path.EnsureNonEmpty("path");

			ConfigXmlDocument doc = new ConfigXmlDocument();

			try
			{
				doc.Load(path);
			}
			catch (XmlException e)
			{
				throw new ConfigurationErrorsException(e.Message);
			}

			XmlNode root = doc.DocumentElement;
            if (root.Name == "configuration")
            {
                ProcessNodes(appContext, root, addedLibraries);
            }
		}

        /// <summary>
        /// Recursively handles loading of the configuration file sections, to handle the inheritance properly
        /// </summary>
        /// <param name="appContext">Application context where to load libraries.</param>
        /// <param name="root">Root to parse child nodes from</param>
        /// <param name="addedLibraries">List of class libraries that are collected while parsing configuration node.</param>
        private void ProcessNodes(ApplicationContext appContext, XmlNode root, LibrariesConfigurationList/*!*/addedLibraries)
        {
            foreach (XmlNode node in root.ChildNodes) {
                if (node.NodeType == XmlNodeType.Element) {
                    switch (node.Name) {
                        case Configuration.SectionName:
                            Parse(appContext, node, addedLibraries);
                            break;

                        case Configuration.LocationName:
                            // Recursively parse the Web.config file to include everything in the <location> element
                            ProcessNodes(appContext, node, addedLibraries);
                            break;

                        case "system.web":
                            ParseSystemWebSection(node);
                            break;
                    }
                }
            }
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
	[Serializable]
	public sealed partial class ApplicationConfiguration
	{
		#region Compiler

		/// <summary>
		/// Compiler options.
		/// </summary>
		[Serializable]
		public sealed partial class CompilerSection : IPhpConfigurationSection
		{
			#region CLR only configuration (inclusions, prepend/append)

			/// <summary>
			/// Whether to watch source code for changes. Applicable only on web applications.
			/// </summary>
            public bool WatchSourceChanges { get { return watchSourceChanges; } }
			private bool watchSourceChanges = true;

            /// <summary>
			/// Whether to allow SSAs. Otherwise Phalanger will ignore physical script files completely.
			/// </summary>
            public bool OnlyPrecompiledCode { get { return onlyPrecompiledCode; } }
            private bool onlyPrecompiledCode = false;

			/// <summary>
			/// Paths searched for statically evaluated inclusion targets.
			/// </summary>
            public string StaticIncludePaths { get { return staticIncludePaths; } set { staticIncludePaths = value; } }
			private string staticIncludePaths = string.Empty;

            /// <summary>
            /// Paths to script files or directories inclusions of which will be forces to be static.
            /// </summary>
            public List<string>/*!*/ForcedDynamicInclusionPaths { get { return forcedDynamicInclusionPaths ?? (forcedDynamicInclusionPaths = new List<string>()); } }
            private List<string> forcedDynamicInclusionPaths = null;

            /// <summary>
            /// ForcedDynamicInclusionPaths translated into FullPath using current SourceRoot.
            /// Only existing items are included in the list.
            /// Can be <c>null</c> if no paths are skipped.
            /// </summary>
            public IList<string>/*!*/ForcedDynamicInclusionTranslatedFullPaths
            {
                get
                {
                    if (forcedDynamicInclusionTranslatedFullPaths == null && forcedDynamicInclusionPaths != null)
                    {
                        forcedDynamicInclusionTranslatedFullPaths = new List<string>(forcedDynamicInclusionPaths.Count);

                        // add only existing files/directories
                        foreach (var skippath in forcedDynamicInclusionPaths)
                        {
                            var fullSkipPath = new FullPath(skippath, this.SourceRoot);

                            if (fullSkipPath.DirectoryExists || fullSkipPath.FileExists)
                                forcedDynamicInclusionTranslatedFullPaths.Add(fullSkipPath.FullFileName);
                        }
                        
                        // forcedDynamicInclusionPaths should not be changed from now
                    }

                    // return read only collection of items
                    // if there are no items, use empty string list
                    return (forcedDynamicInclusionTranslatedFullPaths != null) ?
                        forcedDynamicInclusionTranslatedFullPaths.AsReadOnly() :
                        (IList<string>)ArrayUtils.EmptyStrings;
                }
            }
            private List<string> forcedDynamicInclusionTranslatedFullPaths = null;

			/// <summary>
			/// List of regular expressions and replacements to use when converting include expressions.
			/// </summary>
            public List<InclusionMapping>/*!*/ InclusionMappings { get { return inclusionMappings ?? (inclusionMappings = new List<InclusionMapping>()); } }
			private List<InclusionMapping> inclusionMappings = null;

			/// <summary>
			/// Adds an inclusion mapping.
			/// </summary>
			/// <param name="pattern">A pattern. Should be valid regular expression pattern.</param>
			/// <param name="replacement">A replacement.</param>
			/// <param name="name">An optional name.</param>
			/// <exception cref="ArgumentException"><paramref name="pattern"/> is not a valid regular expression pattern.</exception>
			/// <exception cref="ArgumentNullException"><paramref name="pattern"/> or <paramref name="replacement"/> is <B>null</B>.</exception>
			public void AddInclusionMapping(string/*!*/ pattern, string/*!*/ replacement, string name)
			{
                InclusionMappings.Add(new InclusionMapping(pattern, replacement, name));
				dirty = true;
			}

			/// <summary>
			/// Removes all inclusion mappings having a specified name.
			/// </summary>
			/// <param name="name">A pattern to remove.</param>
			/// <exception cref="ArgumentNullException"><paramref name="name"/> is <B>null</B>.</exception>
			/// <returns>The number of mappings that has been found and removed.</returns>
			public int RemoveInclusionMappingByName(string/*!*/ name)
			{
				if (name == null)
					throw new ArgumentNullException("name");

				int remains_idx = 0;
                for (int i = 0; i < InclusionMappings.Count; i++)
				{
                    if (InclusionMappings[i].Name != name)
                        InclusionMappings[remains_idx++] = InclusionMappings[i];
				}

                int result = InclusionMappings.Count - remains_idx;
				if (result > 0)
				{
                    InclusionMappings.RemoveRange(remains_idx, result);
					dirty = true;
				}
				return result;
			}

			/// <summary>
			/// Clears the list of inclusion mappings.
			/// </summary>
			public void ClearInclusionMappings()
			{
                InclusionMappings.Clear();
			}

			/// <summary>
			/// File to be once-included before a script.
			/// </summary>
			public string PrependFile { get { return prependFile; } }
			private string prependFile;

			/// <summary>
			/// File to be once-included after a script.
			/// </summary>
			public string AppendFile { get { return appendFile; } }
			private string appendFile;

			#endregion

			#region Significant Options Hash Code

			/// <summary>
			/// Whether significant options has been changed since last hash computation.
			/// </summary>
			private bool dirty;

			/// <summary>
			/// Gets a hash code of options significant for compilation.
			/// </summary>
			internal int HashCode
			{
				get
				{
					if (dirty)
					{
						hashcode = 0;

						unchecked
						{
                            foreach (InclusionMapping mapping in InclusionMappings)
							{
								hashcode += mapping.Pattern.ToString().GetHashCode();
								hashcode += mapping.Replacement.GetHashCode();
							}
							hashcode += debug ? 897987897 : 12;
						}

						dirty = false;
					}
					return hashcode;
				}
			}
			private int hashcode;
	
			#endregion

			#region Defaults

			/// <summary>
			/// Initializes fields by default values.
			/// </summary>
			internal CompilerSection()
			{
				dirty = true;
				hashcode = 0;
				debug = true;
				disabledWarnings = WarningGroups.DeferredToRuntime | WarningGroups.CompilerStrict;
				disabledWarningNumbers = ArrayUtils.EmptyIntegers;

				if (HttpContext.Current != null)
				{
					sourceRoot = new FullPath(HttpRuntime.AppDomainAppPath, false);
				}
				else
				{
					// the value should be rewritten by the command line compiler:
					sourceRoot = new FullPath(Directory.GetCurrentDirectory(), false);
				}
				SourceRootSet = false;
			}

			#endregion

			#region XML Parsing

			/// <summary>
			/// Loads configuration from XML node.
			/// </summary>
			public bool Parse(string name, string value, XmlNode node)
			{
				switch (name)
				{
					case "LanguageFeatures":
						LanguageFeatures = (LanguageFeatures)ConfigUtils.ParseFlags(node, (int)LanguageFeatures, typeof(LanguageFeatures));
						return true;

                    case "EnableStaticInclusions":
						{
							if (Configuration.IsValidInCurrentScope(node))
								EnableStaticInclusions = value == "true";
							return true;
						}

					case "StaticIncludePaths":
						StaticIncludePaths = value;
						return true;

                    case "ForcedDynamicInclusionPaths":
                        foreach(XmlNode child in node.ChildNodes)
                        {
                            switch (child.Name)
							{
								case "add":
                                    ForcedDynamicInclusionPaths.Add(ConfigUtils.MandatoryAttribute(child, "value"));
                                    break;
                                case "remove":
                                    ForcedDynamicInclusionPaths.Remove(ConfigUtils.MandatoryAttribute(child, "value"));
                                    break;
                                case "clear":
                                    ForcedDynamicInclusionPaths.Clear();
                                    break;
                                default:
									if (child.NodeType == XmlNodeType.Element)
										throw new ConfigUtils.InvalidNodeException(child);
									break;
                            }
                        }

                        return true;

					case "InclusionMappings":

						foreach (XmlNode child in node.ChildNodes)
						{
							switch (child.Name)
							{
								case "add":
									{
										try
										{
											AddInclusionMapping(
												ConfigUtils.MandatoryAttribute(child, "pattern"),
												ConfigUtils.MandatoryAttribute(child, "value"),
												ConfigUtils.OptionalAttribute(child, "name"));
										}
										catch (ArgumentException)
										{
											throw new ConfigurationErrorsException(CoreResources.GetString("invalid_regular_expression"), child);
										}
										break;
									}

								case "remove":
									RemoveInclusionMappingByName(ConfigUtils.MandatoryAttribute(child, "name"));
									break;

								case "clear":
									ClearInclusionMappings();
									break;

								default:
									if (child.NodeType == XmlNodeType.Element)
										throw new ConfigUtils.InvalidNodeException(child);
									break;
							}
						}
						return true;

					case "Debug":
						debug = (value == "true");
						return true;

					case "WatchSourceChanges":
						{
							// applicable only in run-time:
							if (Configuration.IsBuildTime)
								return true;

							if (HttpContext.Current == null)
								throw new ConfigurationErrorsException(CoreResources.GetString("web_only_option"), node);

                            watchSourceChanges = value == "true" && !OnlyPrecompiledCode;   // OnlyPrecompiledCode => !WatchSourceChanges
                            
							return true;
						}

                    case "OnlyPrecompiledCode":
                        {
                            // applicable only in run-time:
                            if (Configuration.IsBuildTime)
                                return true;

                            if (HttpContext.Current == null)
                                throw new ConfigurationErrorsException(CoreResources.GetString("web_only_option"), node);

                            onlyPrecompiledCode = value == "true";
                            if (OnlyPrecompiledCode) watchSourceChanges = false;    // OnlyPrecompiledCode => !WatchSourceChanges

                            return true;
                        }

					case "DisabledWarnings":
						{
							if (Configuration.IsValidInCurrentScope(node))
							{
								disabledWarnings = (WarningGroups)ConfigUtils.ParseFlags(node, (int)disabledWarnings, typeof(WarningGroups));

								string numbers = ConfigUtils.OptionalAttribute(node, "numbers");
								if (!String.IsNullOrEmpty(numbers))
									disabledWarningNumbers = ConfigUtils.ParseIntegerList(numbers, ',', 1, 10000, node);
							}
							return true;
						}

					// TODO: disabled warning numbers

					case "SourceRoot":
						{
							// applicable only in run-time:
							if (Configuration.IsBuildTime)
								return true;

							// source root option is allowed only in console application configuration:
							if (HttpContext.Current != null)
								throw new ConfigurationErrorsException(CoreResources.GetString("console_only_option"), node);

							try
							{
								SourceRoot = new FullPath(value);
							}
							catch (Exception e)
							{
								throw new ConfigurationErrorsException(e.Message, node);
							}
							return true;
						}

					case "PrependFile":
						prependFile = (value != "") ? value : null;
						return true;

					case "AppendFile":
						appendFile = (value != "") ? value : null;
						return true;
				}
				return false;
			}

			#endregion
		}

		#endregion

		#region Globalization

		/// <summary>
		/// Configuration related to culture.
		/// </summary>
		[Serializable]
		public sealed partial class GlobalizationSection : IPhpConfigurationSection
		{
			#region Loading

			internal GlobalizationSection()
			{
                System.Web.Configuration.GlobalizationSection system_glob = null;

                try
                {
                    system_glob =
                    WebConfigurationManager.GetSection("system.web/globalization") as System.Web.Configuration.GlobalizationSection;
                }
                catch (Exception)
                { }
				
				pageEncoding = (system_glob != null) ? system_glob.FileEncoding : Encoding.Default;
			}

			/// <summary>
			/// Loads configuration from XML node.
			/// </summary>
			public bool Parse(string name, string value, XmlNode node)
			{
				switch (name)
				{
					case "PageEncoding":
						try
						{
							PageEncoding = (value != String.Empty) ? Encoding.GetEncoding(value) : Encoding.Default;
						}
						catch (Exception e)
						{
							throw new ConfigurationErrorsException(e.Message, node);
						}
						return true;
				}
				return false;
			}

			#endregion
		}

		#endregion

		#region Paths

		/// <summary>
		/// Paths to Phalanger directories and tools.
		/// </summary>
		[Serializable]
		public sealed class PathsSection : IPhpConfigurationSection
		{
			/// <summary>
			/// Directory path where dynamic wrappers are stored. 
			/// </summary>
            public FullPath DynamicWrappers { get { return dynamicWrappers; } }
			private FullPath dynamicWrappers;

			/// <summary>
			/// Directory path where managed libraries are stored. 
			/// </summary>
            public FullPath Libraries { get { return libraries; } }
			private FullPath libraries;

            ///// <summary>
            ///// Path to Extensions Manager root.
            ///// </summary>
            //public FullPath ExtManager { get { return manager; } }
            //private FullPath manager;

			/// <summary>
			/// Path to PHP native extensions directory.
			/// </summary>
			public FullPath ExtNatives { get { return natives; } }
			private FullPath natives;

			/// <summary>
			/// Path to PHP extensions wrappers directory.
			/// </summary>
			public FullPath ExtWrappers { get { return wrappers; } }
			private FullPath wrappers;

			/// <summary>
			/// Directory path where type definitions of extensions are stored. 
			/// </summary>
			public FullPath ExtTypeDefs { get { return typeDefs; } }
			private FullPath typeDefs;

            /// <summary>
            /// Last determined modification time. Used to invalidate assemblies compiled before this time.
            /// </summary>
            public DateTime LastConfigurationModificationTime { get; private set; }

            public PathsSection()
            {
                var http_context = HttpContext.Current;

                // default paths, used when the configuration does not set its own:

                string current_app_dir;
                try { current_app_dir = (http_context != null) ? http_context.Server.MapPath("/bin") : "."; }  // this can throw on Mono
                catch (InvalidOperationException) { current_app_dir = "bin"; }

                libraries = /*manager =*/ natives = wrappers = typeDefs = new FullPath(current_app_dir);

                string dynamic_path = (http_context != null) ? current_app_dir : Path.GetTempPath();
                dynamicWrappers = new FullPath(dynamic_path);
            }

			/// <summary>
			/// Loads paths from XML configuration node.
			/// </summary>
			public bool Parse(string name, string value, XmlNode node)
			{
                // determine last configuration modification time:
                this.LastConfigurationModificationTime = ConfigUtils.GetConfigModificationTime(node, this.LastConfigurationModificationTime);

                switch (name)
				{
					case "DynamicWrappers": dynamicWrappers = CheckedPath(value, node); return true;
					case "Libraries": libraries = CheckedPath(value, node); return true;
					case "ExtWrappers": wrappers = CheckedPath(value, node); return true;
					case "ExtTypeDefs": typeDefs = CheckedPath(value, node); return true;
					case "ExtNatives": natives = CheckedPath(value, node); return true;
					case "ExtManager": /*manager = CheckedPath(value, node); // DEPRECATED: will be removed in future versions */  return true;
				}
				return false;
			}

			private FullPath CheckedPath(string value, XmlNode/*!*/node)
			{
                Debug.Assert(node != null);

				FullPath result;

				// checks path correctness:
				try
				{
                    string root = ConfigUtils.GetConfigXmlPath(node.OwnerDocument);
                    if (root != null) value = Path.Combine(Path.GetDirectoryName(root), value);

					result = new FullPath(value);
				}
				catch (ArgumentException e)
				{
					throw new ConfigurationErrorsException(e.Message, node);
				}

				// checks directory existance:
				if (!result.IsEmpty && !Directory.Exists(result))
					throw new ConfigurationErrorsException(CoreResources.GetString("directory_not_exists", result), node);

				return result;
			}

            internal void Validate()
            {
                
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
	[Serializable]
	public sealed partial class GlobalConfiguration : IPhpConfiguration
	{
		#region GlobalVariables

		/// <summary>
		/// Global variables handling options.
		/// </summary>
		[Serializable]
		public sealed partial class GlobalVariablesSection : IPhpConfigurationSection
		{
			/// <summary>
			/// Loads configuration from XML node.
			/// </summary>
			public bool Parse(string name, string value, XmlNode node)
			{
				bool t = value == "true";

				switch (name)
				{
					case "RegisterGlobals": RegisterGlobals = t; break;
					case "RegisterArgcArgv": RegisterArgcArgv = t; break;
					case "RegisterLongArrays": RegisterLongArrays = t; break;
                    case "QuoteGpcVariables": /*QuoteGpcVariables = t;*/ if (t) throw new ConfigUtils.InvalidAttributeValueException(node, "value"); break;
					default:
						return false;
				}
				return true;
			}
		}

		#endregion

		#region PostedFiles

		/// <summary>
		/// Options influencing posting files via HTTP.
		/// </summary>
		[Serializable]
		public sealed class PostedFilesSection : IPhpConfigurationSection
		{
			/// <summary>
			/// Whether to accept HTTP posted files.
			/// </summary>
			public bool Accept = true;

			/// <summary>
			/// Path where to store uploaded files to make them accessible to scripts.
			/// Can be a <B>null</B> reference or empty string which means that default path is used.
			/// </summary>
			public string TempPath = null;

			public string GetTempPath(SafeModeSection/*!*/ safeModeConfig)
			{
				string result = (TempPath != "") ? TempPath : null;

				try
				{
					if (result != null)
					{
						if (safeModeConfig.IsPathAllowed(result))
							Directory.CreateDirectory(result);
						else
							result = null;
					}
				}
				catch (SystemException)
				{
					result = null;
				}

				if (result == null)
				{
					result = Path.Combine(HttpRuntime.CodegenDir, "Posted Files");
					Directory.CreateDirectory(result);
				}

				return result;
			}

			/// <summary>
			/// Loads configuration from XML node.
			/// </summary>
			public bool Parse(string name, string value, XmlNode node)
			{
				switch (name)
				{
					case "Accept":
						Accept = value == "true";
						break;

					case "TempPath":
						try
						{
							TempPath = (value == String.Empty) ? null : Path.GetFullPath(value);
						}
						catch (Exception e)
						{
							throw new ConfigurationErrorsException(e.Message, node);
						}
						break;

					default:
						return false;
				}
				return true;
			}

			internal PostedFilesSection DeepCopy()
			{
				return (PostedFilesSection)MemberwiseClone();
			}
		}

		#endregion

		#region SafeMode

		/// <summary>
		/// Configuration related to the safe mode.
		/// </summary>
		[Serializable]
		public sealed partial class SafeModeSection : IPhpConfigurationSection
		{
			/// <summary>
			/// Whether safe mode is enabled.
			/// </summary>
			public bool Enabled = false;

			/// <summary>
			/// Directory where programs to be executed are searched in.
			/// Applies only if <see cref="Enabled"/> is <B>true</B>.
			/// </summary>
			public string ExecutionDirectory = null;

			/// <summary>
			/// List of directory path prefixes where file system functions can open files.
			/// A <B>null</B> reference mean all paths are allowed.
			/// Not affected by value of <see cref="Enabled"/>.
			/// </summary>
			public string[] AllowedPathPrefixes = null;

			public string/*!*/ GetAllowedPathPrefixesJoin()
			{
				return (AllowedPathPrefixes != null) ? String.Join(Path.DirectorySeparatorChar.ToString(), AllowedPathPrefixes) : "";
			}

			/// <summary>
			/// Checks whether a path is allowed to be used by the script.
			/// </summary>
			/// <param name="path">The non-empty path to be checked.</param>
			/// <returns>
			/// <B>true</B> if the path targets a subdirectory of any directory specified in the 
			/// <see cref="AllowedPathPrefixes"/> or the list is empty.
			/// </returns>
			/// <exception cref="ArgumentNullException"><paramref name="path"/> si a <B>null</B> reference.</exception>
			public bool IsPathAllowed(string path)
			{
				if (path == null)
					throw new ArgumentNullException("path");

				if (AllowedPathPrefixes == null) return true;

				foreach (string prefix in AllowedPathPrefixes)
				{
					if (!String.IsNullOrEmpty(prefix))
					{
						if (StringUtils.FirstDifferent(path, prefix, true) == prefix.Length)    // GENERICS: StartsWith
							return true;
					}
				}
				return false;
			}

			/// <summary>
			/// Loads configuration from XML node.
			/// </summary>
			public bool Parse(string/*!*/ name, string/*!*/ value, XmlNode/*!*/ node)
			{
				switch (name)
				{
					case "Enabled":
						Enabled = value == "true";
						break;

					case "AllowedPathPrefixes":
						AllowedPathPrefixes = (value != "") ? value.Split(new char[] { Path.PathSeparator, Path.AltDirectorySeparatorChar }) : null;
						break;

					case "ExecutionDirectory":
						ExecutionDirectory = value;
						break;

					default:
						return false;
				}
				return true;
			}

			internal SafeModeSection DeepCopy()
			{
				return (SafeModeSection)MemberwiseClone();
			}
		}

		#endregion
	}

	#endregion

	// other stuff..

	#region Configuration Context

    public sealed class LibrariesConfigurationList
    {
        /// <summary>
        /// Information about library being load to be loaded lazily.
        /// </summary>
        private class AddLibraryInfo
        {
            public AddLibraryInfo(string assemblyName, Uri assemblyUrl, string sectionName, XmlNode/*!*/ node)
            {
                Debug.Assert(node != null && (assemblyName != null ^ assemblyUrl != null));

                this.assemblyName = assemblyName;
                this.assemblyUrl = assemblyUrl;
                this.sectionName = sectionName;
                this.node = node;
            }

            public readonly string assemblyName;
            public readonly Uri assemblyUrl;
            public readonly string sectionName;
            public readonly XmlNode/*!*/ node;
        }

        /// <summary>
        /// Libraries to be loaded lazily at the and of parsing of all the configuration sections.
        /// Checks for duplicities, loads libraries using the same configuration after processing all sub-configurations.
        /// </summary>
        private Dictionary<string, AddLibraryInfo> addedLibraries = null;

        /// <summary>
        /// List of class library sections to be parsed when class libraries are loaded.
        /// </summary>
        private List<XmlNode> sections = null;

        /// <summary>
        /// Add class library configuration section to the list to be processed once libraries are loaded.
        /// </summary>
        /// <param name="sectionNode"></param>
        public void AddSection(XmlNode/*!*/sectionNode)
        {
            Debug.Assert(sectionNode != null);
            if (this.sections == null)
                this.sections = new List<XmlNode>();

            this.sections.Add(sectionNode);
        }

        /// <summary>
        /// Adds the library to the list of libraries to be loaded lazily.
        /// </summary>
        public bool AddLibrary(string assemblyName, Uri assemblyUrl, string sectionName, XmlNode/*!*/ node)
        {
            if (addedLibraries == null)
                addedLibraries = new Dictionary<string, AddLibraryInfo>();

            // add the library to be loaded lazily, avoids duplicities in config sections by overwriting previously added library
            addedLibraries[LibraryKey(assemblyName, assemblyUrl)] = new AddLibraryInfo(assemblyName, assemblyUrl, sectionName, node);
            return true;
        }

        /// <summary>
        /// Remove given library from the list of libraries that will be loaded.
        /// </summary>
        public bool RemoveLibrary(string assemblyName, Uri assemblyUrl)
        {
            return addedLibraries != null && addedLibraries.Remove(LibraryKey(assemblyName, assemblyUrl));
        }

        /// <summary>
        /// Clear the list of libraries that will be loaded.
        /// </summary>
        public void ClearLibraries()
        {
            addedLibraries = null;
        }

        private string LibraryKey(string assemblyName, Uri assemblyUrl)
        {
            return string.Format("{0}^{1}",
                (assemblyName != null) ? assemblyName.ToLowerInvariant() : string.Empty,
                (assemblyUrl != null) ? assemblyUrl.ToString().ToLowerInvariant() : string.Empty);
        }

        /// <summary>
        /// Load libraries specified by <see cref="AddLibrary"/> lazily. Also parses postponed sections.
        /// </summary>
        public void LoadLibrariesNoLock(Func<string,Uri,string,XmlNode,bool>/*!*/callback, Action<XmlNode> parseSectionCallback)
        {
            if (addedLibraries != null)
            {
                foreach (var lib in addedLibraries.Values)
                    callback(lib.assemblyName, lib.assemblyUrl, lib.sectionName, lib.node);

                addedLibraries = null;
            }

            if (sections != null)
            {
                if (parseSectionCallback != null)
                    foreach (var section in sections)
                        parseSectionCallback(section);

                sections = null;
            }
        }
    }

	/// <summary>
	/// Configuration context used when loading configuration from XML files.
	/// </summary>
	public sealed class PhpConfigurationContext
	{
		/// <summary>
		/// Collection of defined sections. Sections can be defined only on the application level or above.
		/// Thus it is not necessary to make copies of this table.
		/// </summary>
		private Dictionary<string, LibrarySection>/*!*/ sections;

		/// <summary>
		/// Collection of final sections. Final sections can be defined only on the web application directory level or above.
		/// Thus it is not necessary to make copies of this table. 
		/// </summary>
		private Dictionary<string, string>/*!*/ sealedSections;

		/// <summary>
		/// Path to a directory containing the <c>Web.config</c> file or a <B>null</B> reference 
		/// meaning <c>Machine.config</c>.
		/// </summary>
		public string VirtualPath { get { return virtualPath; } }
		private string virtualPath;

		/// <summary>
		/// Local configuration being currently loaded.
		/// </summary>
		internal LocalConfiguration Local { get { return local; } }
		private LocalConfiguration local;

		/// <summary>
		/// Global configuration being currently loaded.
		/// </summary>
		internal GlobalConfiguration Global { get { return global; } }
		private GlobalConfiguration global;

        private readonly ApplicationContext/*!*/ applicationContext;

        /// <summary>
		/// Creates an empty configuration context used as a root context.
		/// </summary>
		internal PhpConfigurationContext(ApplicationContext/*!*/ applicationContext, string virtualPath)
		{
			Debug.Assert(applicationContext != null);
			this.virtualPath = virtualPath;
			this.applicationContext = applicationContext;

			this.sections = new Dictionary<string, LibrarySection>();
			this.sealedSections = new Dictionary<string, string>();
            this.librariesList = new LibrariesConfigurationList();

			this.local = new LocalConfiguration();
			this.global = new GlobalConfiguration();
		}

		/// <summary>
		/// Makes a copy (child) of this instance (parent) deeply copying the confgiuration fields.
		/// </summary>
		internal PhpConfigurationContext(ApplicationContext/*!*/ applicationContext, string virtualPath, 
			PhpConfigurationContext parent)
		{
			Debug.Assert(applicationContext != null);
			this.virtualPath = virtualPath;
			this.applicationContext = applicationContext;

			// section tables are shared:
			this.sections = parent.sections;
			this.sealedSections = parent.sealedSections;
            this.librariesList = parent.librariesList;

			// configuration records are copied:
			this.local = (LocalConfiguration)parent.local.DeepCopy();
			this.global = (GlobalConfiguration)parent.global.DeepCopy();
		}

        #region Libraries loading

        internal LibrariesConfigurationList/*!*/librariesList;

        /// <summary>
        /// Actually loads libraries specified in <see cref="librariesList"/>.
        /// </summary>
        internal void LoadLibrariesNoLock()
        {
            this.librariesList.LoadLibrariesNoLock(this.LoadLibrary, this.ParseSection);
        }

        /// <summary>
        /// Loads a library and adds a new section to the list of sections if available.
        /// </summary>
        private bool LoadLibrary(string assemblyName, Uri assemblyUrl, string sectionName, XmlNode/*!*/ node)
        {
            // load the assembly:
            DAssembly assembly = applicationContext.AssemblyLoader.Load(assemblyName, assemblyUrl, new LibraryConfigStore(node));
            PhpLibraryAssembly lib_assembly = assembly as PhpLibraryAssembly;

            // not a PHP library or the library is loaded for reflection only:
            if (lib_assembly == null)
                return true;

            PhpLibraryDescriptor descriptor = lib_assembly.Descriptor;

            // section name not stated or the descriptor is not available (reflected-only assembly):
            if (sectionName == null || descriptor == null)
                return true;

            if (descriptor.ConfigurationSectionName == sectionName)
            {
                // an assembly has already been assigned a section? => ok
                if (sections.ContainsKey(sectionName)) return true;

                // TODO (TP): Consider whether this is correct behavior?
                //       This occurs under stress test, because ASP.NET calls 
                //       ConfigurationSectionHandler.Create even though we already loaded assemblies
                Debug.WriteLine("CONFIG", "WARNING: Loading configuration for section '{0}'. " +
                    "Library has been loaded, but the section is missing.", sectionName);
            }
            else if (descriptor.ConfigurationSectionName != null)
            {
                // an assembly has already been loaded with another section name => error:
                throw new ConfigurationErrorsException(CoreResources.GetString("cannot_change_library_section",
                    descriptor.RealAssembly.FullName, descriptor.ConfigurationSectionName), node);
            }

            // checks whether the section has not been used yet:
            LibrarySection existing_section;
            if (sections.TryGetValue(sectionName, out existing_section))
            {
                Assembly conflicting_assembly = existing_section.Descriptor.RealAssembly;
                throw new ConfigurationErrorsException(CoreResources.GetString("library_section_redeclared",
                        sectionName, conflicting_assembly.FullName), node);
            }

            // maps section name to the library descriptor:
            descriptor.WriteConfigurationUp(sectionName);
            sections.Add(sectionName, new LibrarySection(descriptor));

            return true;
        }

        #endregion

        /// <summary>
		/// Processes library configuration section.
		/// </summary>
		/// <param name="node">Configuration node of the section.</param>
		internal void ParseSection(XmlNode/*!*/ node)
		{
			Debug.Assert(node != null, "ParseSection precondition failed.");

			// section not defined by the library => skip:
			LibrarySection section;
			if (!sections.TryGetValue(node.Name, out section))
				return;

			try
			{
				// one iteration of extraction:
				section.UserContext = section.Descriptor.ParseConfig(section.UserContext, this, node);
			}
			catch (ConfigurationErrorsException)
			{
				throw;
			}
			catch (Exception e)
			{
				string lib_name = section.Descriptor.RealAssembly.FullName;
				throw new ConfigurationErrorsException(CoreResources.GetString("library_config_handler_failed", lib_name), e);
			}
		}

        /// <summary>
		/// Finishes and validates the configuration. 
		/// Creates an array of library configurations and stores it to local and global config records.
		/// The first validated configuration is the global one, local ones follows in the order in which 
		/// the respective libraries has been loaded.
		/// </summary>
		/// <exception cref="ConfigurationErrorsException">Configuration is invalid.</exception>
		internal void ValidateNoLock()
		{
			List<PhpLibraryAssembly> libraries = new List<PhpLibraryAssembly>(applicationContext.GetLoadedLibraries());

			Debug.WriteLine("CONFIG", "Context.Validate: #desc = {0}", libraries.Count);

			// request can use only some of the libraries but we need to allocate space for all to allow
			// indexing by unique indices:
			IPhpConfiguration[] local_configs = new IPhpConfiguration[libraries.Count];
			IPhpConfiguration[] global_configs = new IPhpConfiguration[libraries.Count];

			if (!this.applicationContext.AssemblyLoader.ReflectionOnly)
			{
				foreach (PhpLibraryAssembly library in libraries)
				{
					PhpLibraryDescriptor descriptor = library.Descriptor;
					ConfigContextBase cfg_context;

					if (descriptor.ConfigurationSectionName != null)
					{
						Debug.Assert(sections.ContainsKey(descriptor.ConfigurationSectionName));
						cfg_context = sections[descriptor.ConfigurationSectionName].UserContext;
					}
					else
					{
						// creates a configuration records with default values or 
						// empty context if the library doesn't use configuration:
						cfg_context = descriptor.CreateConfigContext();
					}

					descriptor.Validate(cfg_context);

					local_configs[descriptor.UniqueIndex] = cfg_context.Local;
					global_configs[descriptor.UniqueIndex] = cfg_context.Global;
				}
			}
			
			global.Library.SetConfigurations(global_configs);
			local.Library.SetConfigurations(local_configs);

			global.Validate();
			local.Validate();
		}

		/// <summary>
		/// Makes a specified option sealed (which prevents it to be modified in lower-level Web.config files).
		/// </summary>
		/// <param name="name">A name of the option.</param>
		public void SealOption(string/*!*/ name)
		{
			if (name == null)
				throw new ArgumentNullException("name");

			sealedSections.Add(name, virtualPath);
		}

		/// <summary>
		/// Checks whether a specified option ha been sealed.
		/// </summary>
		/// <param name="name">A name of the option.</param>
		/// <returns>Whether it has been sealed.</returns>
		public bool IsOptionSealed(string/*!*/ name)
		{
			if (name == null)
				throw new ArgumentNullException("name");
			return sealedSections.ContainsKey(name);
		}

		/// <summary>
		/// Gets a virtual path to configuration file where a specified option has been sealed.
		/// </summary>
		/// <param name="name">A name of the option.</param>
		/// <returns>A virtual path to the Web.config file, "Machine.config" string, or a <B>null</B> reference if 
		/// the option hasn't been sealed yet.</returns>
		public string GetSealingLocation(string/*!*/ name)
		{
			if (name == null)
				throw new ArgumentNullException("name");

			string result;
			return (sealedSections.TryGetValue(name, out result)) ? result + "Web.config" : "Machine.config";
		}

		/// <summary>
		/// Checks whether the context is associated with Web.config file located lower in hierarchy
		/// than one on the application level (in the web application virtual directory).
		/// </summary>
		/// <returns>Whether the configuration is specific to an application subdirectory.</returns>
		public bool IsSubApplicationConfig()
		{
            string appdomainVirtualPath;

            return
                virtualPath != null && (appdomainVirtualPath = HttpRuntime.AppDomainAppVirtualPath) != null &&
                virtualPath.Length > appdomainVirtualPath.Length;
		}

		/// <summary>
		/// Checks whether the context is associated with the Machine.config file.
		/// </summary>
		/// <returns>Whether the configuration is machine wide.</returns>
		public bool IsMachineConfig()
		{
			return virtualPath == null;
		}

		/// <summary>
		/// Ensures that the configuration is stated on at least application level since it cannot be 
		/// used in sub-application one.
		/// </summary>
		/// <param name="node">The node.</param>
		/// <exception cref="ConfigurationErrorsException">We are on sub-application level.</exception>
		public void EnsureApplicationConfig(XmlNode node)
		{
			if (IsSubApplicationConfig())
			{
				throw new ConfigurationErrorsException(CoreResources.GetString("invalid_node_location", virtualPath,
					CoreResources.GetString("defines_app_wide_config")), node);
			}
		}

		/// <summary>
		/// Ensures that the configuration is stated on the machine level. 
		/// </summary>
		/// <param name="node">The node.</param>
		/// <exception cref="ConfigurationErrorsException">We are not on machine level.</exception>
		public void EnsureMachineConfig(XmlNode node)
		{
			if (!IsMachineConfig())
			{
				throw new ConfigurationErrorsException(CoreResources.GetString("invalid_node_location", virtualPath,
					CoreResources.GetString("defines_app_wide_config")), node);
			}
		}
	}

	/// <summary>
	/// Represents a configuration section defined in a config file. 
	/// </summary>
	/// <remarks>
	/// The section holds the descriptor of the library and it's configuration context.
	/// The in-process configuration records are contained in the context.
	/// </remarks>
	internal sealed class LibrarySection           // GENERICS: struct
	{
		/// <summary>
		/// A descriptor of the library owning the section.
		/// </summary>
		public PhpLibraryDescriptor/*!*/ Descriptor;

		/// <summary>
		/// User configuration context containing values stored in the section.
		/// </summary>
		public ConfigContextBase UserContext;

		public LibrarySection(PhpLibraryDescriptor/*!*/ descriptor)
		{
			Debug.Assert(descriptor != null);

			this.Descriptor = descriptor;
			this.UserContext = descriptor.CreateConfigContext();
		}
	}

	#endregion

	#region Configuration Handler

	/// <summary>
	/// The configuration handler used by configuration system to parse 
	/// the phpNet section of Machine.config and Web.config files.
	/// </summary>
	public sealed class ConfigurationSectionHandler : IConfigurationSectionHandler
	{
		/// <summary>
		/// Names of the top-level nodes.
		/// </summary>
		internal const string NodePaths = "paths";
		internal const string NodeClassLibrary = "classLibrary";
        internal const string NodeScriptLibrary = "scriptLibrary";
        internal const string NodeCompiler = "compiler";
		internal const string NodeGlobalization = "globalization";
		internal const string NodeVariables = "variables";
		internal const string NodeSafeMode = "safe-mode";
		internal const string NodePostedFiles = "posted-files";
		internal const string NodeAssertion = "assertion";
		internal const string NodeFileSystem = "file-system";
		internal const string NodeSessionControl = "session-control";
		internal const string NodeErrorControl = "error-control";
		internal const string NodeRequestControl = "request-control";
		internal const string NodeOutputControl = "output-control";

		private static object/*!*/ loadMutex = new Object();
		private static int stamp = 0;
		private static ApplicationContext applicationContext;

		/// <summary>
		/// Gets a configuration context from the ASP.NET cache.
		/// </summary>
		internal static PhpConfigurationContext GetConfig(ApplicationContext/*!*/ appContext, string/*!*/ sectionName)
		{
			Debug.Assert(appContext != null);
			
			PhpConfigurationContext context;

			lock (loadMutex)
			{
				applicationContext = appContext;
				
				int old_stamp = stamp;

				// loads configuration from all relevant .config files using our Configuration Section Handler;
				// although this way of loading configuration is considered deprecated, the new one is not feasible:
#pragma warning disable 618
                context = (PhpConfigurationContext)ConfigurationManager.GetSection(sectionName);  //ConfigurationSettings.GetConfig(sectionName);
#pragma warning restore 618

				int new_stamp = stamp;

				if (new_stamp != old_stamp)
				{
					// a new context has been loaded from .config file //

					// fills in missing configuration and checks whether the configuration has been loaded properly:
                    if (context != null)
                    {
                        context.LoadLibrariesNoLock();
                        context.ValidateNoLock();
                    }

					// validates application configuration if it has not been validated yet;
					// the application configuration is shared among all requests (threads); 
					// therefore only the first one should validate it:
					Configuration.application.ValidateNoLock();
				}
			}
			return context;
		}

		/// <summary>
		/// GetUserEntryPoint is called by .NET config system when a configuration is needed to be extracted from a XML config file.
		/// </summary>
		/// <param name="parent">
		/// The configuration settings in the parent configuration section. 
		/// Contains config data from already parsed sections.
		/// </param>
		/// <param name="configContext">
		/// An <see cref="HttpConfigurationContext"/> when called from the ASP.NET config. Otherwise, a null reference. 
		/// </param>
		/// <param name="section">Provides direct access to the XML contents of the configuration section. </param>
		/// <returns>Returns LocalConfiguration object with fields set.</returns>
		public object Create(object parent, object configContext, XmlNode/*!*/ section)
		{
			Interlocked.Increment(ref stamp);

			try
			{
				return Create((PhpConfigurationContext)parent, configContext as HttpConfigurationContext, section);
			}
			catch (ConfigurationErrorsException)
			{
				throw;
			}
			catch (Exception /*e*/)
			{
				// HACK: Retry Create, since sometimes loading failed
				try
				{
					return Create((PhpConfigurationContext)parent, configContext as HttpConfigurationContext, section);
				}
				catch (Exception e2)
				{
					throw new ConfigurationErrorsException(e2.Message, section);
				}
			}
		}

		private PhpConfigurationContext Create(PhpConfigurationContext parent, HttpConfigurationContext context,
			XmlNode/*!*/ section)
		{
			PhpConfigurationContext result;

			// determines virtual path to the .config file (null means Machine.config or not applicable):
			string virtual_path = (context != null) ? context.VirtualPath : null;

			Debug.WriteLine("CONFIG", "Parsing configuration in '{0}'. Parent config is '{1}'",
				virtual_path ?? "Machine.config", (parent != null) ? parent.VirtualPath : "null");

			// initialization:
			if (parent != null)
			{
				result = new PhpConfigurationContext(applicationContext, virtual_path, parent);
			}
			else
			{
				result = new PhpConfigurationContext(applicationContext, virtual_path);
			}

			GlobalConfiguration global = result.Global;
			LocalConfiguration local = result.Local;

			// configuration loading is assumed to be synchronized:
			ApplicationConfiguration app = Configuration.application;

            // same with script libraries - these need to be parsed after <sourceRoot>
            XmlNode node_ScriptLibrary = null;

            // determine configuration modification time:
            result.Global.LastConfigurationModificationTime = ConfigUtils.GetConfigModificationTime(section, result.Global.LastConfigurationModificationTime);
            
			// parses XML tree:
			foreach (XmlNode node in section.ChildNodes)
			{
				if (node.NodeType == XmlNodeType.Element)
				{
					switch (node.Name)
					{
						case NodePaths:
							// options can be specified only in application root config and above:
							result.EnsureApplicationConfig(node);

							ConfigUtils.ParseNameValueList(node, result, app.Paths);
							break;

						case NodeClassLibrary:
							// libraries can be loaded only in application root config and above:
							result.EnsureApplicationConfig(node);

                            // parses and loads libraries contained in the list (lazy):
                            ConfigUtils.ParseLibraryAssemblyList(
                                node,
                                result.librariesList,
                                app.Paths.ExtWrappers,
                                app.Paths.Libraries);
                            break;

                        case NodeScriptLibrary:
                            // script libraries can be loaded only in application root config and above:
                            result.EnsureApplicationConfig(node);

                            node_ScriptLibrary = node;

                            break;
						case NodeCompiler:
							// options can be specified only in application root:
							result.EnsureApplicationConfig(node);

							ConfigUtils.ParseNameValueList(node, result, app.Compiler);
							break;

						case NodeGlobalization:
							// options can be specified only in application root:
							result.EnsureApplicationConfig(node);

							ConfigUtils.ParseNameValueList(node, result, app.Globalization);
							break;

						case NodeOutputControl:
							ConfigUtils.ParseNameValueList(node, result, local.OutputControl);
							break;

						case NodeRequestControl:
							ConfigUtils.ParseNameValueList(node, result, local.RequestControl);
							break;

						case NodeErrorControl:
							ConfigUtils.ParseNameValueList(node, result, local.ErrorControl);
							break;

						case NodeSessionControl:
							ConfigUtils.ParseNameValueList(node, result, local.Session);
							break;

						case NodeFileSystem:
							ConfigUtils.ParseNameValueList(node, result, local.FileSystem);
							break;

						case NodeAssertion:
							ConfigUtils.ParseNameValueList(node, result, local.Assertion);
							break;

						case NodeVariables:
							ConfigUtils.ParseNameValueList(node, result, local.Variables, global.GlobalVariables);
							break;

						case NodePostedFiles:
							ConfigUtils.ParseNameValueList(node, result, global.PostedFiles);
							break;

						case NodeSafeMode:
							ConfigUtils.ParseNameValueList(node, result, global.SafeMode);
							break;

						default:
							// processes library section:
                            result.librariesList.AddSection(node);
							break;
					}
				}
			}

            // and script library after that
            if (node_ScriptLibrary != null)
            {
                ConfigUtils.ParseScriptLibraryAssemblyList(node_ScriptLibrary, applicationContext.ScriptLibraryDatabase);
            }

			return result;
		}
	}

	#endregion

	// Access to config (on CLR)

	#region Configuration

	/// <summary>
	/// Provides access to the current configuration records.
	/// </summary>
	[DebuggerNonUserCode]
	public sealed class Configuration
	{
		public const string SectionName = "phpNet";
        public const string LocationName = "location";

		private readonly GlobalConfiguration/*!*/ global;
		private readonly LocalConfiguration/*!*/ defaultLocal;

		[ThreadStatic]
		private static Configuration current = null;

		[ThreadStatic]
		private static bool isBeingLoadedToCurrentThread = false;

        private Configuration(GlobalConfiguration/*!*/ global, LocalConfiguration/*!*/ defaultLocal)
        {
            this.global = global;
            this.defaultLocal = defaultLocal;
        }

        /// <summary>
        /// Loads configuration using default <see cref="ApplicationContext"/>.
        /// </summary>
        private static void LoadDefault()
        {
            Load(ApplicationContext.Default);
        }

		/// <summary>
		/// Loads configuration and returns configuration record.
		/// </summary>
		/// <exception cref="ConfigurationErrorsException">Configuration is invalid or incomplete.</exception>
		public static void Load(ApplicationContext/*!*/ appContext)
		{
			if (current == null)
			{
				Debug.Assert(!isBeingLoadedToCurrentThread, "Configuration loader triggered next configuration load");
				isBeingLoadedToCurrentThread = true;

				try
				{
					PhpConfigurationContext context = ConfigurationSectionHandler.GetConfig(appContext, SectionName);

					if (context != null)
					{
						current = new Configuration(context.Global, context.Local);
					}
					else
					{
						// no configuration loaded from .config files:
						current = new Configuration(new GlobalConfiguration(), new LocalConfiguration());
					}
				}
				finally
				{
					isBeingLoadedToCurrentThread = false;
				}
			}
		}

		/// <summary>
		/// Drops the configuration associated with the current thread and loads a new one.
		/// Doesn't reload XML data from file (cached configuration records are reused).
		/// The libraries listed in the <c>classLibrary</c> section are therefore not loaded into the context.
		/// </summary>
		/// <remarks>
		/// The current thread may have been reused to serve a different request with different configuration context.
		/// Therefore, the configuration associated with the thread needs to be dropped and a new one to be loaded.
		/// </remarks>
		public static void Reload(ApplicationContext/*!*/ appContext, bool reloadFromFile)
		{
			current = null;
			
			if (reloadFromFile) 
				ConfigurationManager.RefreshSection(SectionName);
				
			Load(appContext);
		}

		/// <summary>
		/// Gets application configuration record.
		/// The record is shared among all requests (threads) of the application.
		/// </summary>
		public static ApplicationConfiguration Application
		{
			get
			{
				// note: more threads can start loading the configuration, but that ok:
                if (!application.IsLoaded) LoadDefault();
				return application;
			}
		}
		internal static ApplicationConfiguration application = new ApplicationConfiguration();

		/// <summary>
		/// We need the paths during configuration load (e.g. in dynamic wrapper generator).
		/// </summary>
		internal static ApplicationConfiguration.PathsSection/*!*/ GetPathsNoLoad()
		{
			Debug.Assert(current != null || isBeingLoadedToCurrentThread);
			return application.Paths;
		}

		/// <summary>
		/// Global (script independent) configuration.
		/// Different requsts (threads) may have different global configurations as it depends on the 
		/// directory the request is targetting. Requests to the same directory share the same record.
		/// </summary>
		public static GlobalConfiguration Global
		{
			get
			{
                LoadDefault();
				Debug.Assert(current != null);
				return current.global;
			}
		}

		/// <summary>
		/// Default values for local (script dependent) configuration.
		/// Different requsts (threads) may have different global configurations as it depends on the 
		/// directory the request is targetting. Requests to the same directory share the same record.
		/// </summary>
		public static LocalConfiguration DefaultLocal
		{
			get
			{
                LoadDefault();
				Debug.Assert(current != null);
				return current.defaultLocal;
			}
		}

		/// <summary>
		/// Gets whether the current thread has loaded entire relevant configuration.
		/// </summary>
		public static bool IsLoaded
		{
			get
			{
				return current != null;
			}
		}

        /// <summary>
        /// Get whether we are loading configuration right not, but it is not loaded yet.
        /// </summary>
        internal static bool IsBeingLoaded
        {
            get
            {
                return isBeingLoadedToCurrentThread;
            }
        }

		/// <summary>
		/// Gets script local configuration record, which is unique per request.
		/// </summary>
		public static LocalConfiguration Local
		{
			get
			{
				return ScriptContext.CurrentContext.Config;
			}
		}

		/// <summary>
		/// Whether the application being run is a command line compiler.
		/// Influences a scope of configuration options during their load.
		/// </summary>
		public static bool IsBuildTime = false;

		/// <summary>
		/// Returns whether specified node's scope corresponds to the scope defined by <see cref="IsBuildTime"/>.
		/// </summary>
		/// <param name="node">The node.</param>
		/// <returns>
		/// <B>True</B>, if scope is "Any" or is missing, 
		/// <see cref="IsBuildTime"/> is <B>true</B> and scope is "Build", or
		/// <see cref="IsBuildTime"/> is <B>false</B> and scope is "Runtime". 
		/// </returns>
		/// <exception cref="ConfigurationErrorsException">If scope has invalid value.</exception>
		internal static bool IsValidInCurrentScope(XmlNode/*!*/ node)
		{
			switch (ConfigUtils.OptionalAttribute(node, "scope"))
			{
				case null:
				case "Any": return true;
				case "Build": return IsBuildTime;
				case "Runtime": return !IsBuildTime;

				default:
					throw new ConfigUtils.InvalidAttributeValueException(node, "scope");
			}
		}

        /// <summary>
        /// Latest configuration modification time.
        /// If it cannot be determined, it is equal to <see cref="DateTime.MinValue"/>.
        /// </summary>
        public static DateTime LastConfigurationModificationTime
        {
            get
            {
                return DateTimeUtils.Max(
                    Application.LastConfigurationModificationTime,
                    Global.LastConfigurationModificationTime,
                    Local.LastConfigurationModificationTime);
            }
        }
	}

	#endregion
}
