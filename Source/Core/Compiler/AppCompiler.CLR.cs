/*

 Copyright (c) 2004-2006 Tomas Matousek.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.IO;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Emit;
using PHP.Core.Emit;
using System.Linq;

namespace PHP.Core
{
	using Targets = ApplicationCompiler.Targets;
	using System.Text;
	using System.Configuration;

	#region InvalidCommandLineArgumentException

	public sealed class InvalidCommandLineArgumentException : Exception
	{
		public string Name { get { return name; } }
		private readonly string name;

		public string/*!*/ Value { get { return value; } }
		private readonly string/*!*/ value;

		internal InvalidCommandLineArgumentException(string name, string/*!*/ value, Exception/*!*/ inner)
			: base(inner.Message, inner)
		{
			this.name = name;
			this.value = value;
		}

		internal InvalidCommandLineArgumentException(string name, string/*!*/ value, string/*!*/ message)
			: base(message)
		{
			this.name = name;
			this.value = value;
		}

		public void Report(ErrorSink/*!*/ sink)
		{
			if (sink == null)
				throw new ArgumentNullException("sink");
				
			if (name != null)
				sink.Add(FatalErrors.InvalidCommandLineArgument, null, Parsers.Position.Invalid, name, Message);
			else
				sink.Add(FatalErrors.InvalidCommandLineArgumentNoName, null, Parsers.Position.Invalid, Message);
		}
	}
	
	#endregion

	#region CompilationParameters
	
	[Serializable]
	public sealed class CompilationParameters
    {
        #region Nested struct: ReferenceItem

        /// <summary>
        /// Represents referenced assembly.
        /// </summary>
        [Serializable]
        public struct ReferenceItem
        {
            public string Reference;
            public string LibraryRoot;
        }

        #endregion

        #region Properties

        /// <summary>
		/// Targets. Valid targets from command line are "dll", "console", "web" and "winexe" (in future versions).
		/// </summary>
		public Targets Target { get { return target; } set { target = value; } }
		private Targets target = Targets.None;

		/// <summary>
		/// Full paths to source files to be compiled.
		/// </summary>
		public List<FullPath> SourcePaths { get { return sourcePaths; } }
		private List<FullPath> sourcePaths = new List<FullPath>();

		/// <summary>
		/// Full paths to directories to be recursively searched for files.
		/// </summary>
		public List<FullPath>/*!*/ SourceDirs { get { return sourceDirs; } }
        private List<FullPath>/*!*/ sourceDirs = new List<FullPath>();

        /// <summary>
        /// Full paths to directories or files which should be skipped during compilation.
        /// </summary>
        public List<string>/*!*/ SkipPaths { get { return skipPaths; } }
        private List<string>/*!*/ skipPaths = new List<string>();

		/// <summary>
		/// Paths to configuration files.
		/// </summary>
		public List<FullPath>/*!*/ ConfigPaths { get { return configPaths; } }
		private List<FullPath>/*!*/ configPaths = new List<FullPath>();

		/// <summary>
		/// Full path to the output file (for applications targets) or directory (for web targets).
		/// </summary>
		public FullPath OutPath { get { return outPath; } set { outPath = value; } }
		private FullPath outPath;

		/// <summary>
		/// Full path to the documentation file that should be generated.
		/// </summary>
		public FullPath DocPath { get { return docPath; } set { docPath = value; } }
		private FullPath docPath = FullPath.Empty;

		/// <summary>
		/// Full path to application root directory. All source files paths are relative to this path.
		/// </summary>
		public FullPath SourceRoot { get { return sourceRoot; } set { sourceRoot = value; } }
		private FullPath sourceRoot;

		/// <summary>
		/// Full path of the script which contains entry point 
		/// (only for <see cref="Targets.Console"/> and <see cref="Targets.WinApp"/>).
		/// </summary>
		public FullPath StartupFile { get { return startupFile; } set { startupFile = value; } }
		private FullPath startupFile;

		public QualifiedName? StartupFunction { get { return startupFunction; } set { startupFunction = value; } }
		private QualifiedName? startupFunction;
		
		/// <summary>
		/// Whether to generate debugging information (.pdb files).
		/// </summary>
		public bool? Debuggable { get { return debuggable; } set { debuggable = value; } }
		private bool? debuggable;

        /// <summary>
        /// Whether to force saving the resulting assembly with 32BIT+ flag.
        /// </summary>
        /// <remarks>This is useful for debuggers or deployment when 64bit execution is not supported.</remarks>
        public bool Force32Bit { get; set; }

		public bool? StaticInclusions { get { return staticInclusions; } set { staticInclusions = value; } }
		private bool? staticInclusions;

		public bool Pure { get { return pure; } set { pure = value; } }
		private bool pure;

		public bool IsMTA { get { return isMTA; } set { isMTA = value; } }
		private bool isMTA;
		
		public LanguageFeatures? LanguageFeatures { get { return languageFeatures; } set { languageFeatures = value; } }
		private LanguageFeatures? languageFeatures;

		/// <summary>
		/// Encoding for script files.
		/// </summary>
		public Encoding Encoding { get { return encoding; } set { encoding = value; } }
		private Encoding encoding;

		/// <summary>
		/// A key to sign the assembly with or a <B>null</B> reference.
		/// </summary>
		public StrongNameKeyPair Key { get { return key; } set { key = value; } }
		private StrongNameKeyPair key;

		/// <summary>
		/// Version of the resuting assembly.
		/// </summary>
		public Version Version { get { return version; } set { version = value; } }
		private Version version = new Version(1, 0, 0, 0);

		/// <summary>
		/// Win32 icon to include in the resulting assembly.
		/// </summary>
		public Win32IconResource Icon { get { return icon; } set { icon = value; } }
		private Win32IconResource icon = null;

		/// <summary>
		/// Extensions (e.g. php,inc) of files included in the web application.
		/// Empty list means all extensions.
		/// </summary>
		public List<string>/*!*/ FileExtensions { get { return fileExtensions; } }
		private List<string>/*!*/ fileExtensions = new List<string>();

		/// <summary>
		/// Full paths to referenced assemblies.
		/// </summary>
        public List<ReferenceItem>/*!*/ References { get { return references; } }
        private List<ReferenceItem>/*!*/ references = new List<ReferenceItem>();

		/// <summary>
		/// Full paths to referenced resources.
		/// </summary>
        /// <remarks>If you want more control over resources, put <see cref="ResourceFileReference"/> here</remarks>
        public List<FileReference> Resources { get { return resources; } }
        private List<FileReference> resources = new List<FileReference>();

		public WarningGroups DisableWarnings { get { return disableWarnings; } set { disableWarnings = value; } }
		private WarningGroups disableWarnings = WarningGroups.None;

		public WarningGroups EnableWarnings { get { return enableWarnings; } set { enableWarnings = value; } }
		private WarningGroups enableWarnings = WarningGroups.None;

		public int[]/*!*/ DisableWarningNumbers 
		{ 
			get { return disableWarningNumbers; } 
			set { if (value == null) throw new ArgumentNullException("value"); disableWarningNumbers = value; } 
		}
		private int[]/*!*/ disableWarningNumbers = ArrayUtils.EmptyIntegers;

        /// <summary>
        /// Whether warnings will reported as errors, so they will cause compilation process to not finish.
        /// </summary>
        public bool TreatWarningsAsErrors { get; set; }
	
		#endregion
	
		#region ApplyToConfiguration

		public void ApplyToConfiguration(CompilerConfiguration/*!*/ compilerConfig)
		{
			if (compilerConfig == null)
				throw new ArgumentNullException("compilerConfig");

			if (languageFeatures.HasValue)
			{
				compilerConfig.Compiler.LanguageFeatures = languageFeatures.Value;
			}
			else
			{
				// sets the default language features for pure mode if not set yet:
				if (pure && !compilerConfig.Compiler.LanguageFeaturesSet)
					compilerConfig.Compiler.LanguageFeatures = Core.LanguageFeatures.PureModeDefault;
			}

			// cmd line:
			if (debuggable.HasValue)
				compilerConfig.Compiler.Debug = (bool)debuggable;

			if (staticInclusions.HasValue)
				compilerConfig.Compiler.EnableStaticInclusions = staticInclusions;

            // paths skipped during compilation are also forced dynamic inclusion paths (otherwise static inclusion could force compilation of these sources files)
            foreach (string path in skipPaths)
            {
                compilerConfig.Compiler.ForcedDynamicInclusionPaths.Add(path);
            }

			// static inclusion will be enabled for non-web applications if not set:
			if (!compilerConfig.Compiler.EnableStaticInclusions.HasValue && target != Targets.Web)
				compilerConfig.Compiler.EnableStaticInclusions = true;

			if (encoding != null)
				compilerConfig.Globalization.PageEncoding = encoding;

			// enable all warnings in pure mode by default:
			if (pure)
				compilerConfig.Compiler.DisabledWarnings = WarningGroups.None;

			// disableWarnings and enableWarnings sets are disjoint:
			compilerConfig.Compiler.DisabledWarnings |= disableWarnings;
			compilerConfig.Compiler.DisabledWarnings &= ~enableWarnings;
            compilerConfig.Compiler.DisabledWarningNumbers = compilerConfig.Compiler.DisabledWarningNumbers.Concat(disableWarningNumbers).Distinct().ToArray();

            // Treat Warnings as Errors
            compilerConfig.Compiler.TreatWarningsAsErrors = this.TreatWarningsAsErrors;

			// sets source root (overrides any config setting):
			compilerConfig.Compiler.SourceRoot = new FullPath(sourceRoot);
		}
		
		#endregion


		internal void Validate()
		{
			// comparing value type with null //if (outPath == null) throw new ArgumentNullException("outPath");// 
			if (sourcePaths == null) throw new ArgumentNullException("sourcePaths");
			if (resources == null) throw new ArgumentNullException("resources");
			if (sourceDirs == null) throw new ArgumentNullException("sourceDirs");
			if (sourcePaths.Count == 0 && sourceDirs.Count == 0) throw new ArgumentException("sourcePaths");
			if (!pure && (target == Targets.Console || target == Targets.WinApp)) startupFile.EnsureNonEmpty("startupFile");
		}
    }
    #region Resources
    /// <summary>General file reference</summary>
    [Serializable]
    public class FileReference {
        /// <summary>Contains value of the <see cref="Path"/> property</summary>
        private readonly FullPath path;
        /// <summary>Full path of file</summary>
        public FullPath Path { get { return path; } }
        /// <summary>
		/// Creates file reference from arbitraray path <see cref="System.IO.Path.GetFullPath"/>.
		/// </summary>
		/// <param name="arbitraryPath">Arbitrary path.</param>
		/// <exception cref="ArgumentNullException"><paramref name="arbitraryPath"/> is a <B>null</B> reference.</exception>
		/// <exception cref="ArgumentException">Invalid path. Inner exception specifies details (see <see cref="System.IO.Path.GetFullPath"/>).</exception>
		public FileReference(string arbitraryPath){
            this.path = new FullPath(arbitraryPath);
		}
        /// <summary>CTor from <see cref="FullPath"/></summary>
        /// <param name="fullPath"><see cref="FullPath"/> to reference</param>
        public FileReference(FullPath fullPath) {
            this.path = fullPath;
        }

		/// <summary>
		/// Creates file reference from relative path using <see cref="System.IO.Path.GetFullPath"/>.
		/// </summary>
		/// <param name="relativePath">Arbitrary path.</param>
		/// <param name="root">Root for the path.</param>
		/// <exception cref="ArgumentNullException"><paramref name="relativePath"/> is a <B>null</B> reference.</exception>
		/// <exception cref="ArgumentException">Invalid path. Inner exception specifies details (see <see cref="System.IO.Path.GetFullPath"/>).</exception>
        public FileReference(string/*!*/ relativePath, FullPath root){
			this.path=new FullPath(relativePath,root);
		}
    }
    /// <summary>Points to file that contains resource for assembly</summary>
    [Serializable]
    public sealed class ResourceFileReference:FileReference{
        /// <summary>
        /// Creates resource reference from arbitrary path using <see cref="System.IO.Path.GetFullPath"/>.
        /// </summary>
        /// <param name="arbitraryPath">Arbitrary path.</param>
        /// <remarks>Created resource is public and has same name is is name of file</remarks>
        /// <exception cref="ArgumentNullException"><paramref name="arbitraryPath"/> is a <B>null</B> reference.</exception>
        /// <exception cref="ArgumentException">Invalid path. Inner exception specifies details (see <see cref="System.IO.Path.GetFullPath"/>).</exception>
        /// <exception cref="FileNotFoundException">File represented by current path does not exist (or it is directory)</exception>
        public ResourceFileReference(string arbitraryPath)
            :base(arbitraryPath) {
            if(!this.Path.FileExists) throw new FileNotFoundException("ResourceFileReference can be constructed only from existing file.");
            this.name = base.Path.FileName;
        }
        /// <summary>CTor from <see cref="FullPath"/></summary>
        /// <param name="fullPath"><see cref="FullPath"/> to reference</param>
        /// <remarks>Created resource is public and has same name is is name of file</remarks>
        /// <exception cref="FileNotFoundException">File represented by current path does not exist (or it is directory)</exception>
        public ResourceFileReference(FullPath fullPath) :base(fullPath) {
            if(!this.Path.FileExists) throw new FileNotFoundException("ResourceFileReference can be constructed only from existing file.");
            this.name = base.Path.FileName;
        }
        /// <summary>
        /// Creates resource reference path from relative path using <see cref="System.IO.Path.GetFullPath"/>.
        /// </summary>
        /// <param name="relativePath">Arbitrary path.</param>
        /// <param name="root">Root for the path.</param>
        /// <remarks>Created resource is public and has same name is is name of file</remarks>
        /// <exception cref="ArgumentNullException"><paramref name="relativePath"/> is a <B>null</B> reference.</exception>
        /// <exception cref="ArgumentException">Invalid path. Inner exception specifies details (see <see cref="System.IO.Path.GetFullPath"/>).</exception>
        /// <exception cref="FileNotFoundException">File represented by current path does not exist (or it is directory)</exception>
        public ResourceFileReference(string/*!*/ relativePath, FullPath root)
            : base(relativePath, root) {
            if(!this.Path.FileExists) throw new FileNotFoundException("ResourceFileReference can be constructed only from existing file.");
            this.name = base.Path.FileName;
        }
        /// <summary>
        /// Creates resource reference from arbitrary path using <see cref="System.IO.Path.GetFullPath"/>.
        /// With given name and visibility.
        /// </summary>
        /// <param name="arbitraryPath">Arbitrary path.</param>
        /// <param name="isPublic">Indicates if resource is public</param>
        /// <param name="name">Name of resource</param>
        /// <exception cref="ArgumentNullException"><paramref name="arbitraryPath"/> is a <B>null</B> reference. -or- <paramref name="name"/> is null</exception>
        /// <exception cref="ArgumentException">Invalid path. Inner exception specifies details (see <see cref="System.IO.Path.GetFullPath"/>).</exception>
        /// <exception cref="FileNotFoundException">File represented by current path does not exist (or it is directory)</exception>
        public ResourceFileReference(string arbitraryPath, string name, bool isPublic)
            :base(arbitraryPath){
            if(!this.Path.FileExists) throw new FileNotFoundException("ResourceFileReference can be constructed only from existing file.");
            if(name == null) throw new ArgumentNullException("name");
            this.name = name;
            this.isPublic = isPublic;
        }
        /// <summary>
        /// Creates resource reference path from relative path using <see cref="System.IO.Path.GetFullPath"/>.
        /// </summary>
        /// <param name="relativePath">Arbitrary path.</param>
        /// <param name="root">Root for the path.</param>
        /// <param name="isPublic">Indicates if resource is public</param>
        /// <param name="name">Name of resource</param>
        /// <exception cref="ArgumentNullException"><paramref name="relativePath"/> is a <B>null</B> reference. -or- <paramref name="name"/> is null</exception>
        /// <exception cref="ArgumentException">Invalid path. Inner exception specifies details (see <see cref="System.IO.Path.GetFullPath"/>).</exception>
        /// <exception cref="FileNotFoundException">File represented by current path does not exist (or it is directory)</exception>
        public ResourceFileReference(string/*!*/ relativePath, FullPath root, string name, bool isPublic)
            : base(relativePath, root) {
            if(!this.Path.FileExists) throw new FileNotFoundException("ResourceFileReference can be constructed only from existing file.");
            if(name == null) throw new ArgumentNullException("name");
            this.name = name;
            this.isPublic = isPublic;
        }
        /// <summary>CTor from <see cref="FullPath"/> and attributes</summary>
        /// <param name="fullPath"><see cref="FullPath"/> to reference</param>
        /// <exception cref="FileNotFoundException">File represented by current path does not exist (or it is directory)</exception>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is null</exception>
        /// <param name="isPublic">Indicates if resource is public</param>
        /// <param name="name">Name of resource</param>
        public ResourceFileReference(FullPath fullPath, string name, bool isPublic)
            : base(fullPath) {
            if(!this.Path.FileExists) throw new FileNotFoundException("ResourceFileReference can be constructed only from existing file.");
            if(name == null) throw new ArgumentNullException("name");
            this.name = name;
            this.isPublic = isPublic;
        }
        /// <summary>Contains value of the <see cref="Name"/> property</summary>
        private string name;
        /// <summary>Contains value of the <see cref="IsPublic"/> property</summary>
        private bool isPublic = true;
        /// <summary>Gets name of the resource</summary>
        public string Name { get { return name; } }
        /// <summary>Gets value indicating is this resource is public</summary>
        public bool IsPublic { get { return isPublic; } }
        /// <summary>Converts list of files and directories to list of resource files</summary>
        /// <param name="files">Files and directories. Directories are parsed recursivelly. Files are added immediatelly. If any item is <see cref="ResourceFileReference"/> it is preserved with no changed.</param>
        /// <returns><see cref="List&lt;ResourceFileReference>"/></returns>
        /// <seealso cref="FileSystemUtils.GetAllFiles"/>
        public static List<ResourceFileReference> FromFiles(IEnumerable<FileReference> files) {
            List<FileReference> allfiles = FileSystemUtils.GetAllFiles(files);
            List<ResourceFileReference> ret = new List<ResourceFileReference>(allfiles.Count);
            foreach (FileReference file in allfiles)
            {
                if(file is ResourceFileReference)
                    ret.Add((ResourceFileReference)file);
                else
                    ret.Add(new ResourceFileReference(file.Path));
            }
            return ret;
        }
    }
    #endregion
    #endregion
	
	#region CommandLineParser

	public sealed class CommandLineParser
	{
		#region Construction

		public CommandLineParser(CompilationParameters/*!*/ ps)
		{
			if (ps == null)
				throw new ArgumentException("ps");
		
			this.ps = ps;
#if DEBUG
			verbose = true;
#else
			verbose = false;
#endif
		}

		public CommandLineParser()
			: this(new CompilationParameters())
		{
			ps.SourceRoot = FullPath.GetCurrentDirectory();	
		}

		#endregion

		public CompilationParameters/*!*/ Parameters { get { return ps; } }
		private CompilationParameters/*!*/ ps;
		
		/// <summary>
		/// Level of verbosity.
		/// </summary>
		public bool Verbose { get { return verbose; } set { verbose = value; } }
		private bool verbose;

		public bool ShowHelp { get { return showHelp; } set { showHelp = value; } }
		private bool showHelp = false;

		public bool DrawInclusionGraph { get { return drawInclusionGraph; } set { drawInclusionGraph = value; } }
		private bool drawInclusionGraph = false;

		public bool RedirectErrors { get { return redirectErrors; } set { redirectErrors = value; } }
		private bool redirectErrors = false;

		public bool Quiet { get { return quiet; } set { quiet = value; } }
		private bool quiet = false;

		#region Options

		private static string[] options;

		/// <summary>
		/// Displays a help.
		/// </summary>
		public static KeyValuePair<string, string>[]/*!*/ GetSupportedOptions()
		{
			if (options == null)
			{
				options = new string[]
				{
					"/help", "help",
					"/debug[+|-]", "debug",
					"/target:web", "target_web",
					"/target:exe", "target_exe",
					"/target:dll", "target_dll",
					"/target:winexe", "target_winexe",
					"/pure[+|-]", "pure",
					"/lang:[PHP4|PHP5|PHP6|CLR]", "lang",
					"/out:<path>", "out_path",
					// TODO:
					// "/doc","doc",
					// "/doc:<path>","doc_path",
					"/root:<path>", "root_path",
					"/config:<path>", "config_path",
					"/r[eference]:<path>", "reference_path",
					"/entrypoint:<script>", "entrypoint",
					"/static[+|-]", "static",
					"/skip:<path>", "skip",
					"/recurse:<dir path>", "recurse",
					"/ext:<extension list>", "ext",
					"/encoding:<encoding>", "encoding",
					"/key:<snk file>", "key",
					"/version:#.#.#.#", "version",
					"/win32icon:<file>", "win32icon",
					"/mta[+|-]", "mta",
					"/res[ource]:<path>[,name[,public|private]]", "resource",
					"/nowarn:<warning numbers>", "nowarn_warning_numbers",
					"/dw:DeferredToRuntime", "dw_DeferredToRuntime",
					"/dw:InclusionsMapping", "dw_InclusionsMapping",
					"/dw:CompilerStrict", "dw_CompilerStrict",
					"/dw:AmpModifiers", "dw_AmpModifiers",
					"/ew:<warning>", "ew",
					"/verbose[+|-]", "verbose",
					"/quiet", "quiet",
					"@<path>", "at_path",
				};

				Debug.Assert(options.Length % 2 == 0);

				for (int i = 1; i < options.Length; i += 2)
					options[i] = CoreResources.GetString("phpc_arg_" + options[i]);
			}

			KeyValuePair<string, string>[] result = new KeyValuePair<string, string>[options.Length / 2];
			for (int i = 0, j = 0; i < options.Length; i += 2, j++)
				result[j] = new KeyValuePair<string, string>(options[i], options[i + 1]);

			return result;
		}

		#endregion

		#region Parsing

		/// <summary>
		/// Currently analyzed option.
		/// </summary>
		private static string currentOption;

		/// <summary>
		/// Currently analyzed option's value.
		/// </summary>
		private static string currentValue;

		private InvalidCommandLineArgumentException/*!*/ InvalidValue()
		{
			return new InvalidCommandLineArgumentException(currentOption, currentValue, CoreResources.GetString("invalid_value"));
		}

		private InvalidCommandLineArgumentException/*!*/ Exception(string/*!*/ message)
		{
			return new InvalidCommandLineArgumentException(currentOption, currentValue, message);
		}

		/// <summary>
		/// Processes command line arguments.
		/// </summary>
		/// <param name="args">The command line arguments. The list of arguments may be extended (see @ argument).</param>
		/// <returns>Whether all arguments were processed.</returns>
		public bool Parse(List<string>/*!*/ args)
		{
			if (args == null)
				throw new ArgumentException("args");

			string option_out = null;
			string option_entry_point = null;
			List<string> source_paths = new List<string>();
			List<string> source_dirs = new List<string>();
            List<string> skip_paths = new List<string>();
			List<string> config_paths = new List<string>();
			//bool default_doc_path = false;

			int i = 0;

			try
			{
				while (i < args.Count)
				{
					if (String.IsNullOrEmpty(args[i]))
					{
						i++;
					}
					else if (args[i][0] == '@')
					{
						// replaces the i-th argument by a list of arguments loaded from the file:
						InsertArgumentsFromFile(args, i);
					}
					else if (args[i][0] == '/')
					{
						int colon = args[i].IndexOf(':');

						if (colon >= 0)
						{
							// option having format "/name:value"
							currentOption = args[i].Substring(1, colon - 1).Trim();
							currentValue = args[i].Substring(colon + 1).Trim();

							switch (currentOption.ToLowerInvariant())
							{
								case "out":
									option_out = currentValue;
									break;

								// TODO:
								//case "doc":
								//  // TODO: doesn't consider source root

								//  docPath = new FullPath(currentValue);
								//  default_doc_path = false;

								//  Directory.CreateDirectory(Path.GetDirectoryName(docPath));
								//  break;

								case "root":
									ps.SourceRoot = new FullPath(currentValue);
									if (!ps.SourceRoot.DirectoryExists)
										throw Exception(CoreResources.GetString("directory_not_found", currentValue));
									break;

								case "entrypoint":
									option_entry_point = currentValue;
									break;

								case "target":
									switch (currentValue)
									{
										case "exe": ps.Target = Targets.Console; break;
										case "winexe": ps.Target = Targets.WinApp; break;
										case "dll": ps.Target = Targets.Dll; break;
										case "web": ps.Target = Targets.Web; break;

										default:
											throw InvalidValue();
									}
									break;

								case "dw":
								case "ew":
									try
									{
										WarningGroups wg = (WarningGroups)Enum.Parse(typeof(WarningGroups), currentValue, true);

										if (currentOption == "dw")
										{
											ps.DisableWarnings |= wg;
											ps.EnableWarnings &= ~wg;
										}
										else
										{
											ps.DisableWarnings &= ~wg;
											ps.EnableWarnings |= wg;
										}
									}
									catch (ArgumentException)
									{
										throw InvalidValue();
									}
									break;

								case "nowarn":
									try
									{
										ps.DisableWarningNumbers = ConfigUtils.ParseIntegerList(currentValue, ',', 1, 10000, null);
									}
									catch (ConfigurationErrorsException)
									{
										throw InvalidValue();
									}
									break;

								case "config":
									config_paths.Add(currentValue);
									break;

								case "encoding":
									try
									{
										ps.Encoding = Encoding.GetEncoding(currentValue);
									}
									catch (NotSupportedException)
									{
										throw InvalidValue();
									}
									break;

								case "ext":
									ps.FileExtensions.AddRange(currentValue.Split(',', ';'));
									break;

								case "reference":
								case "r":
                                    ps.References.Add(new CompilationParameters.ReferenceItem() { Reference = currentValue });
									break;

								case "resource":
								case "res":{
								    // TODO: doesn't consider source root
                                    //TODO: Test commandline resources
                                    List<int> commas = new List<int>();
                                    for(int j = 0;j<currentValue.Length;j++)
                                        if(currentValue[j]==',') commas.Add(j);
                                    FileReference res;
                                    switch(commas.Count){
                                        case 0://Path only
                                            res = new FileReference(currentValue);
                                        break;
                                        case 1://Path and name
                                            res = new ResourceFileReference(currentValue.Substring(0, commas[0]), currentValue.Substring(commas[0] + 1),true);
                                        break;
                                        default://Anything,separated,by,commas,name,visibility
                                            bool @public;
                                            switch(currentValue.Substring(commas[commas.Count - 1] + 1).ToLower()) {
                                                case "private": @public=false;
                                                break;
                                                case "public": @public=true;
                                                break;
                                                default:
                                                    throw new ApplicationException(CoreResources.GetString("invalid_resource_visibility"));
                                            }
                                            string name = currentValue.Substring(commas[commas.Count - 2] + 1, commas[commas.Count - 1] - commas[commas.Count - 2]);
                                            string path = currentValue.Substring(0, commas[commas.Count - 2]);
                                            res = new ResourceFileReference(path, name, @public);
                                        break;
                                    }
								    ps.Resources.Add(res);
								    break;
								}

								case "recurse":
									source_dirs.Add(currentValue);
									break;

                                case "skip":
                                    skip_paths.Add(currentValue);
                                    break;

								case "version":
									ps.Version = new Version(currentValue);
									break;

								case "key":
									using (FileStream file = new FileStream(currentValue, FileMode.Open))
										ps.Key = new StrongNameKeyPair(file);
									break;

								case "win32icon":
									ps.Icon = new Win32IconResource(currentValue);
									break;

								case "lang":
								case "language":

                                    Core.LanguageFeatures features = (LanguageFeatures)0;
                                    foreach (var value in currentValue.ToUpperInvariant().Split(new char[]{'|'}, StringSplitOptions.RemoveEmptyEntries))
                                        switch (value)
                                        {
                                            case "4":
                                            case "PHP4":
                                                features |= Core.LanguageFeatures.Php4;
                                                break;

                                            case "5":
                                            case "PHP5":
                                                features |= Core.LanguageFeatures.Php5;
                                                break;

                                            case "PHP/CLR":
                                            case "PHPCLR":
                                            case "CLR":
                                                features |= Core.LanguageFeatures.PhpClr;
                                                break;
                                            default:
                                                features |= (LanguageFeatures)Enum.Parse(typeof(LanguageFeatures), value, true);
                                                break;
                                        }

                                    if (features != 0)
                                        ps.LanguageFeatures = features;

									break;

								default:
									throw Exception(CoreResources.GetString("invalid_option", currentOption));
							}
						}
						else
						{
							// option having format "/name"
							currentOption = args[i].Substring(1).Trim();

							switch (currentOption.ToLower())
							{
								case "debug":
								case "debug+": ps.Debuggable = true; break;
								case "debug-": ps.Debuggable = false; break;

								case "static":
								case "static+": ps.StaticInclusions = true; break;
								case "static-": ps.StaticInclusions = false; break;

								case "verbose":
								case "verbose+": verbose = true; break;
								case "verbose-": verbose = false; break;

								case "pure":
								case "pure+": ps.Pure = true; break;
								case "pure-": ps.Pure = false; break;

								//case "doc":
								//case "doc+": default_doc_path = true; break;
								//case "doc-": default_doc_path = false; break;

								case "quiet": quiet = true; break;
								
								case "mta":
								case "mta+": ps.IsMTA = true; break;
								case "mta-": ps.IsMTA = false; break;
									
								case "ig": drawInclusionGraph = true; break;
								case "errors-to-stdout": redirectErrors = true; break;

								case "?":
								case "help":
									showHelp = true;
									return false;

								default:
									throw Exception(CoreResources.GetString("invalid_option", currentOption));
							}
						}

						i++;
					}
					else
					{
						// source files:

						currentOption = null;
						source_paths.Add(args[i]);

						i++;
					}
				}

				// target not specified => assume console application:
				if (ps.Target == Targets.None) ps.Target = Targets.Console;

				// script source paths:
				ProcessPaths(source_paths, source_dirs, skip_paths, config_paths);

				// output directory:
				ProcessOutOption(option_out);

				//// sets default doc path:
				//if (default_doc_path)
				//  ps.DocPath = new FullPath(Path.ChangeExtension(ps.OutPath, ".xml"));

				// entry point:
				if (ps.Target != Targets.Dll && ps.Target != Targets.Web)
				{
					ProcessEntryPointOption(option_entry_point);
				}

				// file extensions:
				ProcessFileExtensions();

				return true;
			}
			catch (InvalidCommandLineArgumentException)
			{
				throw;
			}
			catch (Exception e)
			{
				throw new InvalidCommandLineArgumentException(currentOption, currentValue, e);
			}
		}

		private void InsertArgumentsFromFile(List<string>/*!*/ args, int index)
		{
			Debug.Assert(args[index][0] == '@');

			string extra_args = File.ReadAllText(args[index].Substring(1));

			// parse the contents of the file
			List<string> arg_list = StringToArgumentList(extra_args);

			args.RemoveAt(index);
			args.InsertRange(index, arg_list);
		}

		/// <summary>
		/// Checks whether there are files to be compiled and modifies their paths to make them absolute.
		/// Doesn't check existance of that files since this is done be compiler.
		/// </summary>
		/// <exception cref="ApplicationException">Error.</exception>
		private void ProcessPaths(List<string>/*!*/ files, List<string>/*!*/ dirs, List<string> skipPaths, List<string>/*!*/ configs)
		{
			ps.SourceDirs.Capacity = ps.SourceDirs.Count + dirs.Count;
			ps.SourcePaths.Capacity = ps.SourcePaths.Count + files.Count;
			ps.ConfigPaths.Capacity = ps.ConfigPaths.Count + configs.Count;
			
			currentOption = null;

			foreach (string file in files)
			{
				currentValue = file;
				FullPath p = new FullPath(file, ps.SourceRoot);

				if (!p.FileExists)
					throw Exception(CoreResources.GetString("source_file_not_found", currentValue));

				ps.SourcePaths.Add(p);
			}

			currentOption = "recurse";

			foreach (string dir in dirs)
			{
				currentValue = dir;
				FullPath p = new FullPath(dir, ps.SourceRoot);

				if (!p.DirectoryExists)
					throw Exception(CoreResources.GetString("directory_not_found", currentValue));

				ps.SourceDirs.Add(p);
			}

            currentOption = "skip";

            foreach (string path in skipPaths)
            {
                ps.SkipPaths.Add(path);
            }

			currentOption = "config";

			foreach (string file in configs)
			{
				currentValue = file;

				FullPath p = new FullPath(file, ps.SourceRoot);
				
				if (!p.FileExists)
					throw Exception(CoreResources.GetString("configuration_file_not_found", currentValue));

				ps.ConfigPaths.Add(p);
			}

			// no source files nor directories specified => adds default (web) or report error:
			if (ps.SourceDirs.Count == 0 && ps.SourcePaths.Count == 0)
			{
				if (ps.Target == Targets.Web)
				{
					ps.SourceDirs.Add(ps.SourceRoot);
					return;
				}
				else
				{
					throw Exception(CoreResources.GetString("no_source_files_to_compile"));
				}
			}
			
			// default config file in source root:
			if (ps.ConfigPaths.Count == 0)
			{
				FullPath default_config = new FullPath(
					(ps.Target == Targets.Web) ? ApplicationCompiler.WebConfigFile : ApplicationCompiler.AppConfigFile, ps.SourceRoot);

				if (default_config.FileExists)
					ps.ConfigPaths.Add(default_config);
			}
		}

		/// <summary>
		/// Gets a default output file extension depending on value of <see cref="CompilationParameters.Target"/>.
		/// </summary>
		/// <returns>The extension, e.g. ".exe".</returns>
		private string GetDefaultOutputFileExtension()
		{
			switch (ps.Target)
			{
				case Targets.Console:
				case Targets.WinApp: return ".exe";
				case Targets.Web:
				case Targets.Dll: return ".dll";
				default:
					Debug.Fail("Invalid target");
					return null;
			}
		}

		/// <summary>
		/// Gets a default output file name depending on the values of <see cref="CompilationParameters.Target"/> and 
		/// <see cref="CompilationParameters.SourcePaths"/>.
		/// </summary>
		/// <returns>The file name with extension (e.g. File.exe).</returns>
		private string GetDefaultOutputFile()
		{
			if (ps.Target != Targets.Web)
			{
				Debug.Assert(ps.SourcePaths != null && ps.SourceDirs != null && (ps.SourcePaths.Count > 0 || ps.SourceDirs.Count > 0));

				if (ps.SourcePaths.Count != 0)
					return Path.GetFileNameWithoutExtension(ps.SourcePaths[0]) + GetDefaultOutputFileExtension();
				else
					return Path.GetFileName(ps.SourceDirs[0]) + GetDefaultOutputFileExtension();
			}
			else
			{
				return PhpScript.CompiledWebAppAssemblyName;
			}
		}
        
		/// <summary>
		/// Processes "out" option and sets <see cref="CompilationParameters.OutPath"/> accordingly.
		/// </summary>
		/// <param name="value">The option's value</param>
		private void ProcessOutOption(string value)
		{
			Debug.Assert(ps.SourcePaths != null);

			currentOption = "out";
			currentValue = value;

			string dir;
			string out_path;

			// default values:
			if (String.IsNullOrEmpty(value))
			{
				// do not override output path if it has been set pior parsing:
				if (!ps.OutPath.IsEmpty) return;
				
				dir = "bin";

				if (ps.Target == Targets.Web)
					dir = Path.Combine(ps.SourceRoot, dir);

				out_path = Path.Combine(dir, GetDefaultOutputFile());
			}
			else if (ps.Target == Targets.Web)
			{
				// web applications expects a directory name:
				if (!Path.IsPathRooted(value))
					value = Path.Combine(ps.SourceRoot, value);

				out_path = Path.Combine(value, PhpScript.CompiledWebAppAssemblyName);
				dir = value;
			}
			else
			{
				// other targets expects a file name:

				if (!Path.IsPathRooted(value))
					value = Path.Combine(ps.SourceRoot, value);

				// extracts directory from the value:
				dir = Path.GetDirectoryName(value);
				if (dir == null)                             // "C:\"
					dir = Path.GetPathRoot(value);

				if (Path.GetFileName(value) == "")
				{
					// if value doesn't specify a file name (e.g. "C:\dir\"):
					out_path = Path.Combine(value, GetDefaultOutputFile());
				}
				else if (Path.GetExtension(value) == "")
				{
					// if value doesn't specify an extension (e.g. "C:\dir\f")
					out_path = value + GetDefaultOutputFileExtension();
				}
				else
				{
					out_path = value;
				}
			}

			// checks path and makes it full:
			ps.OutPath = new FullPath(out_path);
		}

		/// <summary>
		/// Processes "entrypoint" option and sets <see cref="CompilationParameters.StartupFile"/> accordingly.
		/// </summary>
		/// <param name="value">The option's value</param>
		private void ProcessEntryPointOption(string value)
		{
			// comparing value type with null //Debug.Assert(ps.SourceRoot != null && ps.SourcePaths != null);

			currentOption = "entrypoint";
			currentValue = value;

			if (!String.IsNullOrEmpty(value))
			{
				ps.StartupFile = new FullPath(value, ps.SourceRoot);
				ps.SourcePaths.Insert(0, ps.StartupFile);
			}
			else if (!ps.StartupFile.IsEmpty)
			{
				// nop, startup file has already been set prior parsing
			}
			else if (ps.SourcePaths.Count != 0)
			{
				// default value - the first source full path:
				ps.StartupFile = ps.SourcePaths[0];
			}
			else
			{
				throw Exception(CoreResources.GetString("entrypoint_not_specified"));
			}
		}

		/// <summary>
		/// Checks whether file extensions are valid.
		/// </summary>
		private void ProcessFileExtensions()
		{
			foreach (string ext in ps.FileExtensions)
			{
				if (ext.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
					throw Exception(CoreResources.GetString("invalid_file_extension", ext));
			}
			
			if (ps.FileExtensions.Count == 0)
			{
				ps.FileExtensions.Add("php");
				ps.FileExtensions.Add("inc");
			}
		}

		#endregion

		#region Utils
		
		public static List<string>/*!*/ StringToArgumentList(string/*!*/ str)
		{
			if (str == null)
				throw new ArgumentNullException("str");

			List<string> arg_list = new List<string>();

			StringBuilder sb = new StringBuilder();
			bool in_quotes = false;

			for (int i = 0; i < str.Length; i++)
			{
				if (str[i] == '"')
				{
					in_quotes = !in_quotes;
				}
				else
				{
					if (in_quotes || !Char.IsWhiteSpace(str, i))
					{
						sb.Append(str[i]);
					}
					else
					{
						if (sb.Length > 0)
						{
							arg_list.Add(sb.ToString());
							sb.Length = 0;
						}
					}
				}
			}

			if (sb.Length > 0) arg_list.Add(sb.ToString());

			return arg_list;
		}

		#endregion
	}
	
	#endregion

	#region ApplicationCompiler
	
	/// <summary>
	/// PHP application compiler.
	/// </summary>
	public sealed class ApplicationCompiler : MarshalByRefObject
    {
        #region Constants

        /// <summary>
        /// app.config file name.
        /// </summary>
        public const string AppConfigFile = "App.config";

        /// <summary>
        /// web.config file name.
        /// </summary>
		public const string WebConfigFile = "Web.config";
		
		/// <summary>
        /// Compilation target.
        /// </summary>
        public enum Targets
        {
            None,
            Dll,
            Console,
            WinApp,
            Web,
            Eval
        }

        #endregion

        #region Properties

        /// <summary>
        /// Current AppDomain. The AppDomain in which the ApplicationCompiler was created.
        /// </summary>
        public AppDomain/*!*/ Domain { get { return domain; } }
        private readonly AppDomain/*!*/ domain;

        #endregion

        #region Constructor

        public ApplicationCompiler()
		{
			this.domain = AppDomain.CurrentDomain;
        }

        #endregion

        public static bool IsPureUnit(string/*!*/ value)
		{
			return value != null && value.ToLower() == "pure";
        }

        #region Compile

        /// <summary>
		/// Compiles an application.
		/// </summary>
		/// <param name="applicationContext">Application context.</param>
		/// <param name="config">Compiler configuration record.</param>
		/// <param name="errorSink">Error sink.</param>
		/// <param name="ps">Parameters.</param>
		/// <exception cref="InvalidSourceException">Cannot read a source file/directory. See the inner exception for details.</exception>
		public void Compile(
			ApplicationContext/*!*/ applicationContext,
			CompilerConfiguration/*!*/ config,
			ErrorSink/*!*/ errorSink,
			CompilationParameters/*!*/ ps)
		{
			if (applicationContext == null) throw new ArgumentNullException("applicationContext");
			if (config == null) throw new ArgumentNullException("config");
			if (errorSink == null) throw new ArgumentNullException("errorSink");
			ps.Validate();

			PhpSourceFile entry_point_file = (ps.StartupFile != null) ? new PhpSourceFile(config.Compiler.SourceRoot, ps.StartupFile) : null;
			List<ResourceFileReference> resource_files = ResourceFileReference.FromFiles(ps.Resources);

			// creates directory if not exists:
			try
			{
				Directory.CreateDirectory(Path.GetDirectoryName(ps.OutPath));
			}
			catch (Exception ex)
			{
				errorSink.Add(FatalErrors.ErrorCreatingFile, null, ErrorPosition.Invalid, ps.OutPath, ex.Message);
			}	
				
			AssemblyKinds kind;

			switch (ps.Target)
			{
				case Targets.Dll:
					kind = AssemblyKinds.Library;
					entry_point_file = null;
					break;

				case Targets.Console:
					kind = AssemblyKinds.ConsoleApplication;
					break;

				case Targets.WinApp:
					kind = AssemblyKinds.WindowApplication;
					break;

				case Targets.Web:
					kind = AssemblyKinds.WebPage;
					entry_point_file = null;
					break;

				default:
					Debug.Fail();
					throw null;
			}

			PhpAssemblyBuilder assembly_builder = PhpAssemblyBuilder.Create(applicationContext, kind, ps.Pure, ps.OutPath,
				ps.DocPath, entry_point_file, ps.Version, ps.Key, ps.Icon, resource_files, config.Compiler.Debug, ps.Force32Bit);

			assembly_builder.IsMTA = ps.IsMTA;
			
			Statistics.CompilationStarted();

			ICompilerManager manager = (!ps.Pure) ? new ApplicationCompilerManager(applicationContext, assembly_builder) : null;

            try
            {
                CompilationContext context = new CompilationContext(applicationContext, manager, config, errorSink, config.Compiler.SourceRoot);

                assembly_builder.Build(EnumerateScripts(ps.SourcePaths, ps.SourceDirs, ps.FileExtensions, context), context);

                if (!context.Errors.AnyError && (ps.Target == Targets.Console || ps.Target == Targets.WinApp))
                    CopyApplicationConfigFile(config.Compiler.SourceRoot, ps.OutPath);
            }
            catch (CompilerException e)
            {
                errorSink.Add(e.ErrorInfo, null, ErrorPosition.Invalid, e.ErrorParams);
            }
            catch (InvalidSourceException e)
            {
                e.Report(errorSink);
            }
            catch (Exception e)
            {
#if DEBUG
                //Console.WriteLine("Unexpected error: {0}", e.ToString());// removed, exception added into the error sink, so it's displayed in the VS Integration too
#endif
                errorSink.AddInternalError(e);  // compilation will fail, error will be displayed in Errors by VS Integration               
            }
			finally
			{
#if DEBUG
				Console.WriteLine();
				Console.WriteLine("Statistics:");
				Statistics.Dump(Console.Out, Path.GetDirectoryName(ps.OutPath));
				Console.WriteLine();
#endif
			}
        }

        /// <summary>
        /// Copies App.config files to the target bin directory (desktop apps only).
        /// </summary>
        private static bool CopyApplicationConfigFile(FullPath sourceRoot, FullPath assemblyPath)
        {
            string app_config_src = Path.Combine(sourceRoot, AppConfigFile);
            string app_config_dst = assemblyPath + ".config";
            try
            {
                if (File.Exists(app_config_src))
                {
                    File.Copy(app_config_src, app_config_dst, true);
                    //message = CoreResources.GetString("application_config_file_copied");
                }
            }
            catch (SystemException)
            {
                //message = CoreResources.GetString("cannot_create_config_file", app_config_dst, e.Message);
                return false;
            }
            return true;
        }

        #endregion

        #region Scripts enumeration

        private static IEnumerable<PhpSourceFile>/*!*/ EnumerateScripts(
			ICollection<FullPath>/*!*/ sourcePaths, ICollection<FullPath>/*!*/ sourceDirs,
			ICollection<string>/*!*/ fileExtensions, CompilationContext/*!*/ context)
		{
			Debug.Assert(sourcePaths != null && sourceDirs != null && fileExtensions != null && context != null);
			
			// enumerate listed source files:
            foreach (PhpSourceFile file in EnumerateScriptsInDirectory(sourcePaths, ArrayUtils.EmptyStrings, context))
				yield return file;

			// enumerate source files recursively located in specified directories:
			foreach (FullPath dir in sourceDirs)
			{
                foreach (PhpSourceFile file in EnumerateScriptsRecursive(dir, fileExtensions, context))
					yield return file;
			}
		}

		/// <summary>
		/// Compiles a specified collection of scripts within the given compilation context.
		/// </summary>
		private static IEnumerable<PhpSourceFile>/*!*/ EnumerateScriptsInDirectory(IEnumerable<FullPath>/*!*/ sourcePaths,
            ICollection<string>/*!*/ fileExtensions, CompilationContext/*!*/ context)
		{
			Debug.Assert(sourcePaths != null && fileExtensions != null && context != null);

			FullPath source_root = context.Config.Compiler.SourceRoot;

			foreach (FullPath path in sourcePaths)
			{
				if (fileExtensions.Count != 0 && !path.HasAnyExtension(fileExtensions))
					continue;

                string pathString = path;
                bool skip = false;

                foreach (string skipPath in context.Config.Compiler.ForcedDynamicInclusionTranslatedFullPaths)
                    if (pathString.StartsWith(skipPath))
                    {
                        skip = true;
                        break;
                    }
                
                if (skip) continue;

				yield return new PhpSourceFile(source_root, path);
			}
		}

		/// <summary>
		/// Recursively searches a directory for all files matching the web script file pattern.
		/// </summary>
		/// <exception cref="InvalidSourceException">Error reading the directory.</exception>
		private static IEnumerable<PhpSourceFile>/*!*/ EnumerateScriptsRecursive(FullPath directory,
            ICollection<string>/*!*/ fileExtensions, CompilationContext/*!*/ context)
		{
			Debug.Assert(fileExtensions != null && context != null);
			Debug.Assert(!directory.IsEmpty);

            string pathString = directory;

            foreach (string skipPath in context.Config.Compiler.ForcedDynamicInclusionTranslatedFullPaths)
                if (pathString.StartsWith(skipPath))
                    yield break;
            
			FullPath[] files, directories;
			
			try
			{
				files = directory.GetFiles();
				directories = directory.GetDirectories();
			}
			catch (Exception e)
			{
				throw new InvalidSourceException(directory, e);
			}

			// compiles scripts in the current directory:
			foreach (PhpSourceFile file in EnumerateScriptsInDirectory(files, fileExtensions, context))
				yield return file;

			// processes subdirectories:
			foreach (FullPath dir in directories)
			{
                foreach (PhpSourceFile file in EnumerateScriptsRecursive(dir, fileExtensions, context))
					yield return file;
			}
        }

        #endregion

        #region Configuration loading

        /// <summary>
        /// Loads configuration from Machine.config, phpc.exe.config, from files specified by command line arguments,
        /// and from command line arguments themselves.
        /// </summary>
        /// <exception cref="ConfigurationErrorsException">An error occured while loading the configuration.</exception>
        public static CompilerConfiguration/*!*/ LoadConfiguration(
            ApplicationContext/*!*/ appContext, List<FullPath>/*!*/ paths, TextWriter output)
        {
            Configuration.IsBuildTime = true;

            Configuration.Reload(appContext, true);

            // Machine.config, phpc.exe.config:
            CompilerConfiguration result = new CompilerConfiguration(Configuration.Application);

            // explicitly specified or default configs:
            foreach (FullPath path in paths)
            {
                if (output != null) output.WriteLine(path);
                result.LoadFromFile(appContext, path);
            }

            // load libraries lazily
            result.LoadLibraries(appContext);

            //
            return result;
        }

        #endregion

        #region MarshalByRefObject

        [System.Security.SecurityCritical]
        public override object InitializeLifetimeService()
		{
			return null;
        }

        #endregion

        #region Remote Compile

        //public static void CompileInSeparateDomain(ErrorSink/*!*/ errorSink, CompilationParameters/*!*/ ps)
        //{
        //    if (ps == null) throw new ArgumentNullException("ps");
        //    if (errorSink == null) throw new ArgumentNullException("errorSink");
        //    ps.Validate();

        //    ApplicationCompiler remote = CreateRemoteCompiler();
        //    try
        //    {
        //        remote.RemoteCompile(ref errorSink, ps);
        //    }
        //    catch (TargetInvocationException e)
        //    {
        //        throw e.InnerException;
        //    }

        //    AppDomain.Unload(remote.Domain);  // this may cause an exception in the AppDomain thread
        //}

        public static ApplicationCompiler/*!*/ CreateRemoteCompiler()
		{
            // setup new AppDomain
            // the AppDomain has ShadowCopyFiles enabled to not lock loaded assemblies
            AppDomainSetup setup = new AppDomainSetup();
            setup.ShadowCopyFiles = "true";
            
            // create AppDomain
            AppDomain domain = AppDomain.CreateDomain("PhalangerCompilationDomain_" + Guid.NewGuid().ToString(), null, setup);

            // Wrap the instance of ApplicationCompiler
			Type type = typeof(ApplicationCompiler);

			return (ApplicationCompiler)domain.CreateInstanceAndUnwrap(type.Assembly.FullName, type.FullName);
		}

        public void RemoteCompile(
			ref ErrorSink/*!*/ errorSink,
			CompilationParameters/*!*/ ps)
		{
			errorSink = new PassthroughErrorSink(errorSink);

			ApplicationContext.DefineDefaultContext(false, true, false);
			ApplicationContext app_context = ApplicationContext.Default;
			
			CompilerConfiguration compiler_config;

			// loads entire configuration:
			try
			{
				compiler_config = LoadConfiguration(app_context, ps.ConfigPaths, null);			
			}
			catch (ConfigurationErrorsException e)
			{
				errorSink.AddConfigurationError(e);
				return;
			}

			ps.ApplyToConfiguration(compiler_config);

			// load referenced assemblies:
			try
			{
				app_context.AssemblyLoader.Load(ps.References);
			}
			catch (ConfigurationErrorsException e)
			{
				errorSink.AddConfigurationError(e);
				return;
			}
			
			try
			{
				errorSink.DisabledGroups = compiler_config.Compiler.DisabledWarnings;
				errorSink.DisabledWarnings = compiler_config.Compiler.DisabledWarningNumbers;
                errorSink.TreatWarningsAsErrors = compiler_config.Compiler.TreatWarningsAsErrors;
			
				// initializes log:
				Debug.ConsoleInitialize(Path.GetDirectoryName(ps.OutPath));

				Compile(app_context, compiler_config, errorSink, ps);
			}
			catch (InvalidSourceException e)
			{
				e.Report(errorSink);
				return;
			}
			catch (Exception e)
			{
				errorSink.AddInternalError(e);
				return;
			}
        }

        #endregion
	}
	
	#endregion
}
