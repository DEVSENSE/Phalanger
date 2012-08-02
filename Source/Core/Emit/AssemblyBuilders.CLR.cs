/*

 Copyright (c) 2006 Tomas Matousek.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.IO;
using System.Diagnostics;
using System.Collections;
using System.Reflection;
using System.Reflection.Emit;

using PHP.Core;
using PHP.Core.Reflection;
using System.Collections.Generic;
using System.Text;
using System.Resources;

namespace PHP.Core.Emit
{
	#region PhpAssemblyBuilder

	public abstract partial class PhpAssemblyBuilder : PhpAssemblyBuilderBase
	{
		#region Properties

		/// <summary>
		/// A kind of assembly file.
		/// </summary>
		public AssemblyKinds Kind { get { return kind; } }
		private readonly AssemblyKinds kind;

		public bool IsExecutable
		{
			get { return kind == AssemblyKinds.ConsoleApplication || kind == AssemblyKinds.WindowApplication; }
		}

		public override bool IsTransient { get { return false; } }

		/// <summary>
		/// Whether the assembly contains debug information.
		/// </summary>
		public bool Debuggable { get { return debuggable; } }
		private readonly bool debuggable;

        /// <summary>
        /// Whether saved assembly should be executed as 32-bit process on 64-bit environments.
        /// </summary>
        public bool Force32Bit { get; private set; }

		public string/*!*/ Directory { get { return directory; } }
		private readonly string/*!*/ directory;

		public string/*!*/ FileName { get { return fileName; } }
		private readonly string/*!*/ fileName;

		public Win32IconResource Icon { get { return icon; } }
		private readonly Win32IconResource icon;

        /// <summary>Contains value of the <see cref="Resources"/> property</summary>
        private ICollection<ResourceFileReference> resources;
        /// <summary>Resources to emed</summary>
        public ICollection<ResourceFileReference> Resources { get { return resources; } }

		/// <summary>
		/// Multi-threaded apartment.
		/// </summary>
		public bool IsMTA { get { return isMTA; } set { isMTA = value; } }
		private bool isMTA;

		protected abstract void SetAttributes();
		protected abstract void EmitEntryPoint(MethodBuilder/*!*/ builder);
        
        protected virtual void EmitAndBakeHelpers() { }
		public abstract IPhpModuleBuilder/*!*/ DefineModule(CompilationUnitBase/*!*/ compilationUnit);

		internal ExportAttribute ExportInfo { get { return exportInfo; } }
		private ExportAttribute exportInfo;

		public override bool IsExported { get { return exportInfo != null; } }

		#endregion

		#region Construction

		protected PhpAssemblyBuilder(PhpAssembly/*!*/ assembly, AssemblyName assemblyName, string moduleName,
            string directory, string fileName, AssemblyKinds kind, ICollection<ResourceFileReference> resources, bool debug,
            bool force32bit, bool saveOnlyAssembly, Win32IconResource icon)
			: base(assembly)
		{
			this.kind = kind;
			this.debuggable = debug;
            this.Force32Bit = force32bit;
			this.fileName = fileName;
			this.directory = directory;
			this.icon = icon;
            this.resources = resources;

#if SILVERLIGHT
			AssemblyBuilder assembly_builder = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
			ModuleBuilder module_builder = (ModuleBuilder)assembly_builder.ManifestModule; // SILVERLIGHT: hack? http://silverlight.org/forums/p/1444/3919.aspx#3919
#else

            AssemblyBuilder assembly_builder = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, saveOnlyAssembly ? AssemblyBuilderAccess.Save : AssemblyBuilderAccess.RunAndSave, directory);
			ModuleBuilder module_builder = assembly_builder.DefineDynamicModule(moduleName, fileName, debug);
#endif
			DefineGlobalType(module_builder);
			assembly.WriteUp(module_builder, Path.Combine(directory, fileName)); // TODO: Combine can be avoided (pass path instead of directory + fileName)
		}

		#endregion

		#region Emission

#if !SILVERLIGHT
		public abstract bool Build(IEnumerable<PhpSourceFile>/*!!*/ sourceFiles, CompilationContext/*!*/ context);
#endif

		/// <summary>
		/// Returns name for the global type.
		/// </summary>
		protected override string GenerateGlobalTypeName()
		{
			return QualifiedName.Global.Name.Value;
		}

		#endregion

		#region IPhpCustomAttributeProvider Members

		private AST.CustomAttributes attributes = new AST.CustomAttributes(new List<AST.CustomAttribute>());

		public override void CustomAttributeDefined(ErrorSink errors, AST.CustomAttribute/*!*/ customAttribute)
		{
			attributes.Attributes.Add(customAttribute);
		}

		public override AttributeTargets AcceptsTargets
		{
			get { return AttributeTargets.Assembly | AttributeTargets.Module; }
		}

		public override int GetAttributeUsageCount(DType/*!*/ type, AST.CustomAttribute.TargetSelectors selector)
		{
			return attributes.Count(type, selector);
		}

		public override void ApplyCustomAttribute(AST.SpecialAttributes kind, Attribute attribute, AST.CustomAttribute.TargetSelectors selector)
		{
			switch (kind)
			{
				case AST.SpecialAttributes.Export:
					this.exportInfo = (ExportAttribute)attribute;
					break;

				default:
					Debug.Fail("N/A");
					break;
			}
		}

		public override void EmitCustomAttribute(CustomAttributeBuilder/*!*/ builder, AST.CustomAttribute.TargetSelectors selector)
		{
			switch (selector)
			{
				case AST.CustomAttribute.TargetSelectors.Assembly:
					RealAssemblyBuilder.SetCustomAttribute(builder);
					break;

				case AST.CustomAttribute.TargetSelectors.Module:
					RealModuleBuilder.SetCustomAttribute(builder);
					break;
			}
		}

		#endregion

		#region Create, Save, etc..

		public static PhpAssemblyBuilder/*!*/ Create(ApplicationContext/*!*/ applicationContext, AssemblyKinds kind,
			bool pure, FullPath outPath, FullPath docPath, PhpSourceFile entryPoint, Version version,
            StrongNameKeyPair key, Win32IconResource icon, ICollection<ResourceFileReference> resources, bool debug, bool force32bit)
		{
			string out_dir = Path.GetDirectoryName(outPath);
			string out_file = Path.GetFileName(outPath);

			AssemblyName assembly_name = new AssemblyName();
			assembly_name.Name = Path.GetFileNameWithoutExtension(outPath);
			assembly_name.Version = version;
			assembly_name.KeyPair = key;

            if (pure)
            {
                return new PureAssemblyBuilder(applicationContext, assembly_name, out_dir, out_file,
                    kind, resources, debug, force32bit, icon);
            }
            else
            {
                return new MultiScriptAssemblyBuilder(applicationContext, assembly_name, out_dir, out_file,
                    kind, resources, debug, force32bit, icon, entryPoint);
            }
		}


		public void Save()
		{
			AssemblyBuilder builder = RealAssemblyBuilder;

			EmitAndBakeHelpers();

			// defines and emits the entry point helper:
			if (IsExecutable)
				CreateEntryPoint();

			BakeGlobals();

			// annotates the assembly with Debuggable attribute:
			if (debuggable)
			{
				builder.SetCustomAttribute(new CustomAttributeBuilder(Constructors.Debuggable,
					new object[] { true, true }));
			}

            //// annotates the assembly with TargetFramework attribute:
            ////[assembly: TargetFramework(".NETFramework,Version=v4.0", FrameworkDisplayName = ".NET Framework 4")]
            //builder.SetCustomAttribute(new CustomAttributeBuilder(Constructors.TargetFramework,
            //    new object[] { ".NETFramework,Version=v4.0" },
            //    new PropertyInfo[] { Properties.TargetFrameworkAttribute_FrameworkDisplayName }, new object[] { ".NET Framework 4" }));

			// adds builder-specific attributes:
			SetAttributes();

			string res_file_path = (icon != null) ? Path.GetTempFileName() : null;

            try {
                //adds resources
                try {
                    if(resources != null)
                        foreach(ResourceFileReference resource in resources)
                            AddResourceFile(RealModuleBuilder,resource.Name, resource.Path, resource.IsPublic ? ResourceAttributes.Public : ResourceAttributes.Private);
                } catch(Exception ex) {
                    throw new CompilerException(FatalErrors.ErrorCreatingFile, ex, Path.Combine(directory, fileName), ex.Message);
                }

                try {
                    // adds the icon:
                    if(icon != null)
                        icon.DefineIconResource(builder, res_file_path);

                    builder.Save(
                        fileName,
                        (Force32Bit) ? (PortableExecutableKinds.ILOnly | PortableExecutableKinds.Required32Bit) : (PortableExecutableKinds.ILOnly),
                        ImageFileMachine.I386);

                } catch(IOException e) {
                    throw new CompilerException(FatalErrors.ErrorCreatingFile, e, Path.Combine(directory, fileName), e.Message);
                }
            } finally {
                if(res_file_path != null)
                    File.Delete(res_file_path);
            }
		}

        /// <summary>Enbdeds resource into assembly</summary>
        /// <param name="builder"><see cref="ModuleBuilder"/> to embede resource in</param>
        /// <param name="name">Name of the resource</param>
        /// <param name="path">File to obtain resource from</param>
        /// <param name="attributes">Defines resource visibility</param>

        //DefineResource
        // Exceptions:
        //   System.ArgumentException:
        //     name has been previously defined or if there is another file in the assembly
        //     named fileName.-or- The length of name is zero.-or- The length of fileName
        //     is zero.-or- fileName includes a path.
        //
        //   System.ArgumentNullException:
        //     name or fileName is null.
        //
        //   System.Security.SecurityException:
        //     The caller does not have the required permission.

        //ResourceReader
        // Exceptions:
        //   System.ArgumentException:
        //     The stream is not readable.
        //
        //   System.ArgumentNullException:
        //     The stream parameter is null.
        //
        //   System.IO.IOException:
        //     An I/O error has occurred while accessing stream.

        //AddResource
        // Exceptions:
        //   System.ArgumentNullException:
        //     The name parameter is null.

        //ReadAllBytes
        // Exceptions:
        //   System.ArgumentException:
        //     path is a zero-length string, contains only white space, or contains one
        //     or more invalid characters as defined by System.IO.Path.InvalidPathChars.
        //
        //   System.ArgumentNullException:
        //     path is null.
        //
        //   System.IO.PathTooLongException:
        //     The specified path, file name, or both exceed the system-defined maximum
        //     length. For example, on Windows-based platforms, paths must be less than
        //     248 characters, and file names must be less than 260 characters.
        //
        //   System.IO.DirectoryNotFoundException:
        //     The specified path is invalid (for example, it is on an unmapped drive).
        //
        //   System.IO.IOException:
        //     An I/O error occurred while opening the file.
        //
        //   System.UnauthorizedAccessException:
        //     path specified a file that is read-only.-or- This operation is not supported
        //     on the current platform.-or- path specified a directory.-or- The caller does
        //     not have the required permission.
        //
        //   System.IO.FileNotFoundException:
        //     The file specified in path was not found.
        //
        //   System.NotSupportedException:
        //     path is in an invalid format.
        //
        //   System.Security.SecurityException:
        //     The caller does not have the required permission.
        private void AddResourceFile(ModuleBuilder builder,string name, FullPath path, ResourceAttributes attributes) {
            IResourceWriter rw = builder.DefineResource(path.FileName, name, attributes);
            string ext = path.Extension.ToLower();
            if(ext == ".resources") {
                ResourceReader rr = new ResourceReader(path);
                using(rr) {
                    System.Collections.IDictionaryEnumerator de = rr.GetEnumerator();
                    while(de.MoveNext()) {
                        string key = de.Key as string;
                        rw.AddResource(key, de.Value);
                    }
                }
            } else {
                rw.AddResource(name, File.ReadAllBytes(path));
            }              
        }

		/// <summary>
		/// Adds an entry point as a global method.
		/// </summary>
		/// <returns>A method info representing the entry point.</returns>
		private MethodInfo CreateEntryPoint()
		{
			// public static void Run([string[] args]);
			MethodBuilder entry_method = globalTypeEmitter.TypeBuilder.DefineMethod(
				ScriptAssembly.EntryPointHelperName,
				MethodAttributes.Public | MethodAttributes.Static,
				Types.Void,
				(kind == AssemblyKinds.ConsoleApplication) ? new Type[] { typeof(string[]) } : Type.EmptyTypes);


			// marks entry point as STA/MTA to enable using COM:
			entry_method.SetCustomAttribute(new CustomAttributeBuilder(
				isMTA ? Constructors.MTAThread : Constructors.STAThread, ArrayUtils.EmptyStrings));

			EmitEntryPoint(entry_method);

			// sets assembly entry point:
			RealAssemblyBuilder.SetEntryPoint(entry_method, Enums.ToPEFileKind(kind));

			// user entry point can be defined only on module which is in debug mode:
			if (debuggable)
				ReflectionUtils.SetUserEntryPoint(RealModuleBuilder, GetUserEntryPointMethod());

			return entry_method;
		}

        protected abstract MethodInfo GetUserEntryPointMethod();

		#endregion

	}

	#endregion

	#region PureAssemblyBuilder

	public sealed class PureAssemblyBuilder : PhpAssemblyBuilder
	{
		public PureAssembly/*!*/ PureAssembly { get { return (PureAssembly)assembly; } }
		public PureModuleBuilder/*!*/ PureModuleBuilder { get { return (PureModuleBuilder)PureAssembly.Module; } }

		public override bool IsPure { get { return true; } }

		public PureAssemblyBuilder(ApplicationContext/*!*/ applicationContext, AssemblyName assemblyName,
            string directory, string fileName, AssemblyKinds kind, ICollection<ResourceFileReference> resources, bool debug, bool force32bit, Win32IconResource icon)
			: base(new PureAssembly(applicationContext), assemblyName, PureAssembly.ModuleName, directory,
					fileName, kind, resources, debug, force32bit, false, icon)
		{
		}

#if !SILVERLIGHT
		public override bool Build(IEnumerable<PhpSourceFile>/*!*/ sourceFiles, CompilationContext/*!*/ context)
		{
			PureCompilationUnit unit = new PureCompilationUnit(false, false);

			bool success = unit.Compile(sourceFiles, this, context, context.Config.Globalization.PageEncoding);

			if (success) Save();

			return success;
		}
#endif

		public PureModuleBuilder/*!*/ DefineModule(PureCompilationUnit/*!*/ compilationUnit)
		{
			PureModuleBuilder builder = new PureModuleBuilder(compilationUnit, this);
			PureAssembly.Module = builder;
			return builder;
		}

		public override IPhpModuleBuilder/*!*/ DefineModule(CompilationUnitBase/*!*/ compilationUnit)
		{
			return DefineModule((PureCompilationUnit)compilationUnit);
		}

		protected override void SetAttributes()
		{
			AssemblyBuilder builder = (AssemblyBuilder)RealModuleBuilder.Assembly;

            // try to find any other DAssemblyAttribute that was defined manually in the code;
            // in such case, PurePhpAssemblyAttribute cannot be added (there can be just one DAssemblyAttribute)
            object[] attrs = builder.GetCustomAttributes(typeof(DAssemblyAttribute), false);
            if (attrs != null && attrs.Length > 0)
                return;
            
			// stores a list of assemblies whose entries are present in the compiler's tables and thus
			// are expected to be dynamically accessible at run-time:

			List<DAssembly> assemblies = assembly.ApplicationContext.GetLoadedAssemblies();
			string[] names = new string[assemblies.Count];

			for (int i = 0; i < assemblies.Count; i++)
				names[i] = assemblies[i].RealAssembly.FullName;

			builder.SetCustomAttribute(new CustomAttributeBuilder(Constructors.PurePhpAssembly,
				new object[] { names }));
		}

		protected override void EmitAndBakeHelpers()
		{
			PureModuleBuilder.EmitHelpers();
		}

		protected override void EmitEntryPoint(MethodBuilder/*!*/ builder)
		{
			PureCompilationUnit unit = PureModuleBuilder.PureCompilationUnit;

			Debug.Assert(unit.EntryPoint != null);

			ILEmitter il = new ILEmitter(builder);

			// LOAD new RoutineDelegate(<main PHP method>);
			il.Emit(OpCodes.Ldnull);
			il.Emit(OpCodes.Ldftn, unit.EntryPoint.ArgLessInfo);
			il.Emit(OpCodes.Newobj, Constructors.RoutineDelegate);

			// ScriptContext.RunApplication(<main helper delegate>, null, null);
			il.Emit(OpCodes.Ldnull);
			il.Emit(OpCodes.Ldnull);
			il.Emit(OpCodes.Call, Methods.ScriptContext.RunApplication);

			// RETURN;
			il.Emit(OpCodes.Ret);
		}

        protected override MethodInfo GetUserEntryPointMethod()
        {
            PureCompilationUnit unit = PureModuleBuilder.PureCompilationUnit;
            Debug.Assert(unit.EntryPoint != null);
            return unit.EntryPoint.ArgFullInfo;
        }
    }

	#endregion

	#region ScriptAssemblyBuilder

	/// <summary>
	/// Provides a functionality common to script assembly builders.
	/// </summary>
	public abstract class ScriptAssemblyBuilder : PhpAssemblyBuilder
	{
		public override bool IsPure { get { return false; } }
		public ScriptAssembly/*!*/ ScriptAssembly { get { return (ScriptAssembly)assembly; } }

		protected ScriptAssemblyBuilder(ScriptAssembly/*!*/ assembly, AssemblyName assemblyName, string directory,
            string fileName, AssemblyKinds kind, ICollection<ResourceFileReference> resources, bool debug,
            bool force32bit, bool saveOnlyAssembly, Win32IconResource icon)
			: base(assembly, assemblyName, ScriptAssembly.RealModuleName, directory, fileName, kind,resources, debug, force32bit, saveOnlyAssembly, icon)
		{

		}

#if !SILVERLIGHT
		public override bool Build(IEnumerable<PhpSourceFile>/*!!*/ sourceFiles, CompilationContext/*!*/ context)
		{
			return CompileScripts(sourceFiles, context);
		}
#endif

		public static bool CompileScripts(IEnumerable<PhpSourceFile>/*!!*/ sourceFiles, CompilationContext/*!*/ context)
		{
			bool success = true;
			InclusionGraphBuilder graph_builder = null;

			Debug.WriteLine("SAB", "CompileScripts()");

			try
			{
				graph_builder = new InclusionGraphBuilder(context);

				foreach (PhpSourceFile source_file in sourceFiles)
					success &= graph_builder.AnalyzeDfsTree(source_file);

				if (success)
					graph_builder.EmitAllUnits(new CodeGenerator(context));
			}
			catch (Exception)
			{
				success = false;
				throw;
			}
			finally
			{
				if (graph_builder != null)
				{
					graph_builder.CleanAllUnits(context, success);
					graph_builder.Dispose();
				}

				context.Manager.Finish(success);
			}

			return success;
		}

    	protected override void SetAttributes()
		{
			AssemblyBuilder builder = (AssemblyBuilder)RealModuleBuilder.Assembly;

            var ssabuilder = this as SingleScriptAssemblyBuilder;
            var scriptType = (ssabuilder != null) ? ssabuilder.ModuleBuilder.ScriptType : Types.Void; // SAVE THIS TO THE ATTRIBUTE

			builder.SetCustomAttribute(new CustomAttributeBuilder(Constructors.ScriptAssembly,
                new object[] { ScriptAssembly.IsMultiScript, scriptType }));
		}

		protected abstract ScriptBuilder GetEntryScriptBuilder();

        protected override void EmitEntryPoint(MethodBuilder/*!*/ methodBuilder)
        {
            ScriptBuilder script_builder = GetEntryScriptBuilder();
            Debug.Assert(script_builder.CompilationUnit is ScriptCompilationUnit);

            if (script_builder == null)
                throw new InvalidOperationException(CoreResources.GetString("entrypoint_not_specified"));

            PhpSourceFile entry_file = ((ScriptCompilationUnit)script_builder.CompilationUnit).SourceUnit.SourceFile;

            ILEmitter il = new ILEmitter(methodBuilder);

            // LOAD new PhpScript.MainHelperDelegate(Default.Main);
            il.Emit(OpCodes.Ldnull);
            il.Emit(OpCodes.Ldftn, script_builder.MainHelper);
            il.Emit(OpCodes.Newobj, Constructors.MainHelperDelegate);

            // LOAD <source name>
            il.Emit(OpCodes.Ldstr, entry_file.RelativePath.ToString());

            // LOAD Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location)
            il.Emit(OpCodes.Call, Methods.Assembly.GetEntryAssembly);
            il.Emit(OpCodes.Callvirt, Properties.Assembly_Location.GetGetMethod());
            il.Emit(OpCodes.Call, Methods.Path.GetDirectoryName);

            // ScriptContext.RunApplication(<main helper delegate>, <source name>, <entry assembly directory> );
            il.Emit(OpCodes.Call, Methods.ScriptContext.RunApplication);

            // RETURN;
            il.Emit(OpCodes.Ret);
        }

        protected override MethodInfo GetUserEntryPointMethod()
        {
            ScriptBuilder script_builder = GetEntryScriptBuilder();

            if (script_builder == null)
                throw new InvalidOperationException(CoreResources.GetString("entrypoint_not_specified"));

            return script_builder.MainHelper;
        }
	}

	#endregion

	#region SingleScriptAssemblyBuilder

	/// <summary>
	/// A builder of a script assembly which comprises of a single script module.
	/// </summary>
	internal class SingleScriptAssemblyBuilder : ScriptAssemblyBuilder
	{
		public SingleScriptAssembly/*!*/ SingleScriptAssembly { get { return (SingleScriptAssembly)assembly; } }
		public ScriptBuilder ModuleBuilder { get { return (ScriptBuilder)SingleScriptAssembly.Module; } }

		/// <summary>
		/// Creates an instance of of single-script assembly builder.
		/// </summary>
		/// <param name="applicationContext">Application context.</param>
		/// <param name="assemblyName">Name of the assembly.</param>
		/// <param name="directory">Directory where assembly will be stored.</param>
		/// <param name="fileName">Name of the assembly file including an extension.</param>
		/// <param name="kind">Assembly kind.</param>
		/// <param name="debug">Whether to include debug information.</param>
        /// <param name="force32bit">Whether to force 32bit execution of generated assembly.</param>
        /// <param name="saveOnlyAssembly">Whether to not load the assembly into memory.</param>
        /// <param name="icon">Icon resource or a <B>null</B> reference.</param>
        /// <param name="resources">Resources to embed</param>
		public SingleScriptAssemblyBuilder(ApplicationContext/*!*/ applicationContext, AssemblyName assemblyName, string directory, string fileName,
            AssemblyKinds kind, ICollection<ResourceFileReference> resources, bool debug, bool force32bit, bool saveOnlyAssembly, Win32IconResource icon)
            : base(new SingleScriptAssembly(applicationContext), assemblyName, directory, fileName, kind, resources, debug, force32bit, saveOnlyAssembly, icon)
		{
		}
        /// <summary>
        /// Creates an instance of of single-script assembly builder (without resources).
        /// </summary>
        /// <param name="applicationContext">Application context.</param>
        /// <param name="assemblyName">Name of the assembly.</param>
        /// <param name="directory">Directory where assembly will be stored.</param>
        /// <param name="fileName">Name of the assembly file including an extension.</param>
        /// <param name="kind">Assembly kind.</param>
        /// <param name="debug">Whether to include debug information.</param>
        /// <param name="force32bit">Whether to force 32bit execution of generated assembly.</param>
        /// <param name="saveOnlyAssembly">Whether to not load the assembly into memory.</param>
        /// <param name="icon">Icon resource or a <B>null</B> reference.</param>
        public SingleScriptAssemblyBuilder(ApplicationContext/*!*/ applicationContext, AssemblyName assemblyName, string directory, string fileName,
            AssemblyKinds kind, bool debug, bool force32bit, bool saveOnlyAssembly, Win32IconResource icon)
            : base(new SingleScriptAssembly(applicationContext), assemblyName, directory, fileName, kind, null, debug, force32bit, saveOnlyAssembly, icon)
        {
        }

		/// <summary>
		/// Defines one and only script belonging to the assembly builder.
		/// </summary>
		public ScriptBuilder/*!*/ DefineScript(ScriptCompilationUnit/*!*/ compilationUnit)
		{
			// defines a new script:
            string subnamespace = ScriptModule.GetSubnamespace(compilationUnit.SourceUnit.SourceFile.RelativePath, true);
            ScriptBuilder sb = new ScriptBuilder(compilationUnit, this, subnamespace);

			// adds the script into script assembly builder:
			this.SingleScriptAssembly.Module = sb;

			return sb;
		}

		public override IPhpModuleBuilder DefineModule(CompilationUnitBase/*!*/ compilationUnit)
		{
			return DefineModule((CompilationUnit)compilationUnit);
		}

		protected override ScriptBuilder GetEntryScriptBuilder()
		{
			return ModuleBuilder;
		}

		protected override void EmitAndBakeHelpers()
		{
            //// information is needed when loading SSA from ASP.NET temporary files
            //if (this.Kind == AssemblyKinds.WebPage)
            //    ModuleBuilder.SetScriptAttribute(ScriptBuilder.ScriptAttributeType.RelativeSourceAndIncludees);
            ModuleBuilder.SetScriptAttribute(ScriptBuilder.ScriptAttributes.All);

			ModuleBuilder.EmitHelpers();
			ModuleBuilder.Bake();
		}
	}

	#endregion

	#region MultiScriptAssemblyBuilder

	/// <summary>
	/// A builder of a script assembly which comprises of multiple script modules.
	/// </summary>
	internal class MultiScriptAssemblyBuilder : ScriptAssemblyBuilder
	{
		public MultiScriptAssembly/*!*/ MultiScriptAssembly { get { return (MultiScriptAssembly)assembly; } }

		public PhpSourceFile EntryPoint { get { return entryPoint; } }
		private PhpSourceFile entryPoint;

		/// <summary>
		/// Creates an instance of of multi-script assembly builder.
		/// </summary>
		/// <param name="applicationContext">Application context.</param>
		/// <param name="assemblyName">Name of the assembly.</param>
		/// <param name="directory">Directory where assembly will be stored.</param>
		/// <param name="fileName">Name of the assembly file including an extension.</param>
        /// <param name="kind">Assembly file kind.</param>
		/// <param name="debug">Whether to include debug information.</param>
        /// <param name="force32bit">Whether to force 32bit execution of generated assembly.</param>
		/// <param name="entryPoint">Entry point.</param>
		/// <param name="icon">Icon.</param>
        /// <param name="resources">Resources to embed</param>
        public MultiScriptAssemblyBuilder(ApplicationContext/*!*/ applicationContext, AssemblyName assemblyName,
            string directory, string fileName, AssemblyKinds kind, ICollection<ResourceFileReference> resources,
                        bool debug, bool force32bit, Win32IconResource icon, PhpSourceFile entryPoint)
            : base(new MultiScriptAssembly(applicationContext), assemblyName, directory, fileName, kind, resources, debug, force32bit, false, icon)
        {
            this.entryPoint = entryPoint;
        }

		/// <summary>
		/// Defines a new script belonging to the multiscript assembly builder.
		/// </summary>
		public ScriptBuilder/*!*/ DefineModule(ScriptCompilationUnit/*!*/ compilationUnit)
		{
			string subnamespace = ScriptModule.GetSubnamespace(compilationUnit.SourceUnit.SourceFile.RelativePath, true);
			ScriptBuilder sb = new ScriptBuilder(compilationUnit, this, subnamespace);
			MultiScriptAssembly.AddScriptModule(compilationUnit.SourceUnit.SourceFile, sb);
			return sb;
		}

		public override IPhpModuleBuilder DefineModule(CompilationUnitBase/*!*/ compilationUnit)
		{
			return DefineModule((ScriptCompilationUnit)compilationUnit);
		}

		protected override ScriptBuilder GetEntryScriptBuilder()
		{
			return (ScriptBuilder)MultiScriptAssembly.GetModule(entryPoint);
		}

		protected override void EmitAndBakeHelpers()
		{
			foreach (ScriptBuilder sb in MultiScriptAssembly.Modules.Values)
			{
                //if (this.Kind == AssemblyKinds.WebPage)
                //    sb.SetScriptAttribute(ScriptBuilder.ScriptAttributeType.Includers);
                //else if (this.Kind == AssemblyKinds.Library)
                //    sb.SetScriptAttribute(ScriptBuilder.ScriptAttributeType.RelativeSourceAndIncludees);
                sb.SetScriptAttribute(ScriptBuilder.ScriptAttributes.All);

				sb.EmitHelpers();
				sb.Bake();
			}
			
			// add a dummy type with CLS compliant name to enable static references to the assembly from other languages
			// (see ScriptContext.IncludeScript):
			this.RealModuleBuilder.DefineType(StringUtils.ToClsCompliantIdentifier(Path.ChangeExtension(this.FileName, "")), 
				TypeAttributes.Class | TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.Public).CreateType();
		}
	}

	#endregion
}
