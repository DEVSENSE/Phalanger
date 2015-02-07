/*

 Copyright (c) 2011 DEVSENSE.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/
using System;
using System.IO;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Microsoft.Build.Utilities;
using Microsoft.Build.Framework;
using System.Diagnostics;

using PHP.Core;
using System.Threading;

namespace PHP.VisualStudio.PhalangerTasks
{
	public class PhalangerCompilerTask : Task
	{
		#region Constructors

		/// <summary>
		/// Constructor. This is the constructor that will be used
		/// when the task run.
		/// </summary>
		public PhalangerCompilerTask()
		{
		}

		#endregion

		#region Public Properties

		[Required()]
		public string SourceRoot { get { return sourceRoot; } set { sourceRoot = value; } }
		private string sourceRoot;

		[Required()]
		public string[] SourceFiles { get { return sourceFiles; } set { sourceFiles = value; } }
		private string[] sourceFiles;

		[Required()]
		public string OutputType { get { return outputType; } set { outputType = value; } }
		private string outputType;

		[Required()]
		public string OutputAssembly { get { return outputAssembly; } set { outputAssembly = value; } }
		private string outputAssembly;

        /// <summary>
        /// Files referenced by the compiled task.
        /// </summary>
		public ITaskItem[] ReferencedAssemblies { get { return referencedAssemblies; } set { referencedAssemblies = value; } }
		private ITaskItem[] referencedAssemblies;

        /// <summary>
        /// Projects referenced by the compiled task.
        /// </summary>
        public ITaskItem[] ReferencedProjects { get { return referencedProjects; } set { referencedProjects = value; } }
        private ITaskItem[] referencedProjects;

        /// <summary>
        /// Resolved references (assemblies file name) of the compiled task. (contains project and file references together)
        /// </summary>
        public ITaskItem[] References { get { return references; } set { references = value; } }
        private ITaskItem[] references;

		public string[] ContentFiles { get { return contentFiles; } set { contentFiles = value; } }
		private string[] contentFiles;

		public bool Debug { get { return debug; } set { debug = value; } }
		private bool debug;

        public bool TreatWarningsAsErrors { get; set; }

		public string CompilationMode { get { return compilationMode; } set { compilationMode = value; } }
		private string compilationMode;

		public string StartupObject { get { return startupObject; } set { startupObject = value; } }
		private string startupObject;

		public string ApplicationIcon { get { return applicationIcon; } set { applicationIcon = value; } }
		private string applicationIcon;

		public string LanguageFeatures { get { return languageFeatures; } set { languageFeatures = value; } }
		private string languageFeatures;

		public string DisabledWarnings { get { return disabledWarnings; } set { disabledWarnings = value; } }
		private string disabledWarnings;

        public string KeyFile { get { return keyFile; } set { keyFile = value; } }
        private string keyFile;

		public string SomeTestingProperty { get { return someTestingProperty; } set { someTestingProperty = value; } }
		private string someTestingProperty;

        /// <summary>Contains value of the <see cref="ResourceFiles"/> property</summary>
        private ITaskItem[] resourceFiles = new ITaskItem[0];
        /// <summary>
        /// List of resource files
        /// </summary>
        public ITaskItem[] ResourceFiles {
            get { return resourceFiles; }
            set {
                if(value != null) {
                    resourceFiles = value;
                } else {
                    resourceFiles = new ITaskItem[0];
                }

            }
        }

		#endregion

		public override bool Execute()
		{
			Log.LogMessage(MessageImportance.Normal, "Phalanger Compilation Task");

			CompilationParameters ps = new CompilationParameters();

			// source root (project directory by default):
			ps.SourceRoot = new FullPath(sourceRoot);

			// target type:
			string assembly_extension;
            switch (outputType.ToLowerInvariant())
			{
				case "dll":
				case "library":
					ps.Target = ApplicationCompiler.Targets.Dll;
					assembly_extension = ".dll";
					break;

				case "exe":
				case "console":
					ps.Target = ApplicationCompiler.Targets.Console;
					assembly_extension = ".exe";
					break;

				case "winexe":
				case "winapp":
					ps.Target = ApplicationCompiler.Targets.WinApp;
					assembly_extension = ".exe";
					break;

				case "webapp":
                    ps.Target = ApplicationCompiler.Targets.Web;
                    assembly_extension = ".dll";
					// TODO: precompile option
					return true;

				default:
					Log.LogError("Invalid output type: '{0}'.", outputType);
					return false;
			}

			if (Path.GetExtension(outputAssembly) != assembly_extension)
			{
				Log.LogError("Output assembly extension doesn't match project type.");
				return false;
			}

			if (contentFiles != null)
			{
				foreach (string file in contentFiles)
				{
					if (String.Compare(Path.GetExtension(file), ".config", true) == 0)
					{
						ps.ConfigPaths.Add(new FullPath(file, ps.SourceRoot));
					}
				}
			}

			// debug symbols:
			ps.Debuggable = this.Debug;

            // compilation of executables in debug mode from VisualStudio/MSBuild will produce 32bit assembly to EE working properly
            ps.Force32Bit = this.Debug && assembly_extension.EqualsOrdinalIgnoreCase(".exe");

			// language features:
			ps.Pure = ApplicationCompiler.IsPureUnit(compilationMode);

			if (!String.IsNullOrEmpty(languageFeatures))
			{
				try
				{
					ps.LanguageFeatures = (Core.LanguageFeatures)Enum.Parse(typeof(Core.LanguageFeatures),
						languageFeatures, true);
				}
				catch (Exception)
				{
					Log.LogError("Invalid language features.");
					return false;
				}
			}
			else
			{
				ps.LanguageFeatures = (ps.Pure) ? Core.LanguageFeatures.PureModeDefault : Core.LanguageFeatures.Default;
			}

			// source paths:
			GetSourcePaths(ps.SourceRoot, ps.SourcePaths);

			// directories (TODO) 
			// ps.SourceDirs
			// extensions (TODO) 
			// ps.FileExtensions = null;

			if (ps.SourcePaths.Count == 0 && ps.SourceDirs.Count == 0)
			{
				Log.LogError("No source files to compile.");
				return false;
			}

			// out path:
			try
			{
				Directory.CreateDirectory(Path.GetDirectoryName(outputAssembly));
			}
			catch (Exception e)
			{
				Log.LogErrorFromException(e);
				return false;
			}

			ps.OutPath = new FullPath(outputAssembly);

			// doc path (TODO):
			ps.DocPath = FullPath.Empty;

			// startup file/class:
			ps.StartupFile = FullPath.Empty;
			// TODO: string startup_class = null;

			if (ps.Target == ApplicationCompiler.Targets.Console || ps.Target == ApplicationCompiler.Targets.WinApp)
			{
				if (ps.Pure)
				{
					if (!String.IsNullOrEmpty(startupObject))
					{
						// TODO: startup_class = startupObject;

						Log.LogWarning("Startup class is ignored -- the feature is not supported yet.");
						return false;
					}
					else
					{
						// TODO: startup_class = null;
					}
				}
				else
				{
					if (String.IsNullOrEmpty(startupObject))
					{
						if (ps.SourcePaths.Count > 1)
						{
							Log.LogError("The startup file must be specified in the project property pages.");
							return false;
						}
						else
						{
							ps.StartupFile = new FullPath(ps.SourcePaths[0], ps.SourceRoot);
						}
					}
					else
					{
						try
						{
							ps.StartupFile = new FullPath(startupObject, ps.SourceRoot);
						}
						catch (Exception e)
						{
							Log.LogErrorFromException(e);
							return false;
						}

						// startup file is not in the list of compiled files:
						if (ps.SourcePaths.IndexOf(ps.StartupFile) == -1)
						{
							Log.LogError("The startup file specified in the property pages must be included in the project.");
							return false;
						}
					}
				}
			}

			// icon:
			ps.Icon = null;
			try
			{
				if (applicationIcon != null)
					ps.Icon = new Win32IconResource(new FullPath(applicationIcon, ps.SourceRoot));
			}
			catch (Exception e)
			{
				Log.LogErrorFromException(e);
				return false;
			}

			// strong name, version (TODO):
            try
            {
                ps.Version = new Version(1, 0, 0, 0);
                ps.Key = null;
                if (!string.IsNullOrEmpty(keyFile))
                {
                    using (FileStream file = new FileStream(new FullPath(keyFile, ps.SourceRoot), FileMode.Open, FileAccess.Read))
                        ps.Key = new StrongNameKeyPair(file);
                }
            }
            catch (Exception e)
            {
                Log.LogErrorFromException(e);
                return false;
            }
            
            //Resources
            foreach(ITaskItem resource in this.ResourceFiles) {
                bool publicVisibility = true;
                string access = resource.GetMetadata("Access");
                if(String.CompareOrdinal("Private", access) == 0)
                    publicVisibility = false;
                string filename = resource.ItemSpec;
                string logicalName = resource.GetMetadata("LogicalName");
                if(String.IsNullOrEmpty(logicalName))
                    logicalName = Path.GetFileName(resource.ItemSpec);
                ps.Resources.Add(new ResourceFileReference(filename,logicalName,publicVisibility));
            }

			// referenced assemblies:

			//if (referencedAssemblies != null)
            if (references != null)
			{
                foreach (ITaskItem assemblyReference in references/*referencedAssemblies*/)
                {
                    // script library root:
                    var scriptLibraryRoot = assemblyReference.GetMetadata("MSARoot");

                    if (scriptLibraryRoot != null)
                        scriptLibraryRoot = scriptLibraryRoot.Trim();

                    if (string.IsNullOrEmpty(scriptLibraryRoot))
                        scriptLibraryRoot = null;

                    // add the reference to CompilationParameters:
                    ps.References.Add(new CompilationParameters.ReferenceItem()
                    {
                        Reference = assemblyReference.ItemSpec,
                        LibraryRoot = scriptLibraryRoot
                    });
                }
			}

			// errors, warnings:
			ErrorSink sink = new CompilerErrorSink(this.Log);

			if (!String.IsNullOrEmpty(disabledWarnings))
			{
				try
				{
					ps.DisableWarningNumbers = ConfigUtils.ParseIntegerList(disabledWarnings, ',', 1, 10000, null);
				}
				catch (Exception)
				{
					Log.LogError("Invalid list of disabled warnings.");
					return false;
				}
			}
			else
			{
				ps.DisableWarningNumbers = ArrayUtils.EmptyIntegers;
			}

            ps.EnableWarnings |= WarningGroups.DeferredToRuntime;   // enable deferred to runtime warnings
            
            ps.TreatWarningsAsErrors = this.TreatWarningsAsErrors;

            // compile

			try
			{
				//ApplicationCompiler.CompileInSeparateDomain(sink, ps);
				RemoteCompile(ref sink, ps);
			}
			catch (InvalidSourceException e)
			{
				e.Report(sink);
				return false;
			}
			catch (Exception e)
			{
				sink.AddInternalError(e);
				return false;
			}

			return !sink.AnyError;
		}

		public void RemoteCompile(ref ErrorSink/*!*/ errorSink, CompilationParameters/*!*/ ps)
		{
			lock (buildMutex) // TODO: do we need thread-safety (if yes, there is a better way)?
			{
				//if (++buildCounter % 10 == 0) // TODO: is it possible to estimate size of memory allocated by the domain?
				//{
				//  // if a referenced assembly gets updated then we should reload the domain as well
				//  AppDomain.Unload(remoteCompiler.Domain);
				//  remoteCompiler = null;
				//}

                if (remoteCompiler != null)
                    AppDomain.Unload(remoteCompiler.Domain);

				remoteCompiler = ApplicationCompiler.CreateRemoteCompiler();

				remoteCompiler.RemoteCompile(ref errorSink, ps);
			}
		}
		private static ApplicationCompiler/*!*/ remoteCompiler;
		//private static int buildCounter = 0;
		private static object/*!*/ buildMutex = new object();

		private void LogAllExceptions(Exception e)
		{
			while (e != null)
			{
				Log.LogErrorFromException(e);
				e = e.InnerException;
			}
		}

		private void GetSourcePaths(FullPath sourceRoot, List<FullPath>/*!*/ result)
		{
			System.Diagnostics.Debug.Assert(sourceFiles != null);

			result.Capacity = result.Count + sourceFiles.Length;

			foreach (string file in sourceFiles)
			{
				result.Add(new FullPath(file, sourceRoot));
			}
		}
	}
}