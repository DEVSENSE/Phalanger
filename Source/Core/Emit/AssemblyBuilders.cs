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
#if SILVERLIGHT
using PHP.CoreCLR;
#endif

namespace PHP.Core.Emit
{
	#region AssemblyKinds

	public enum AssemblyKinds
	{
		ConsoleApplication,
		WindowApplication,
		WebPage,
		Library
	}

	public static partial class Enums
	{
		/// <summary>
		/// Converts assembly kind to <see cref="PEFileKinds"/>.
		/// </summary>
		public static PEFileKinds ToPEFileKind(AssemblyKinds kind)
		{
			switch (kind)
			{
				case AssemblyKinds.ConsoleApplication: return PEFileKinds.ConsoleApplication;
				case AssemblyKinds.WindowApplication: return PEFileKinds.WindowApplication;
				default: return PEFileKinds.Dll;
			}
		}
	}

	#endregion

	#region PhpAssemblyBuilderBase

	public abstract class PhpAssemblyBuilderBase : AST.IPhpCustomAttributeProvider
	{
		public abstract bool IsExported { get; }
		public abstract bool IsPure { get; }
		public abstract bool IsTransient { get; }

		public AssemblyBuilder/*!*/ RealAssemblyBuilder { get { return (AssemblyBuilder)assembly.RealAssembly; } }
		public ModuleBuilder/*!*/ RealModuleBuilder { get { return (ModuleBuilder)assembly.RealModule; } }
        internal DelegateBuilder/*!*/ DelegateBuilder { get { return deleateBuilder ?? (deleateBuilder = new DelegateBuilder(RealModuleBuilder)); } }
        private DelegateBuilder deleateBuilder;

        /// <summary>
        /// Prevent calling of ReflectionUtils.CreateGlobalType(RealModuleBuilder) more than once.
        /// </summary>
        private bool globalTypeCreated = false;

		public PhpAssembly/*!*/ Assembly { get { return assembly; } }
		protected readonly PhpAssembly/*!*/ assembly;

		/// <summary>
		/// Global type emitter. Emits naming context definitions, entry-point, etc.
		/// (in transient module it is used for naming contexts only)
		/// </summary>
		/// <remarks>
		/// Non-null after 'DefineGlobalType' is called.
		/// </remarks>
		public ILEmitter/*!*/ GlobalTypeEmitter { get { return globalTypeEmitter; } }
		protected ILEmitter/*!*/ globalTypeEmitter;

		public PhpAssemblyBuilderBase(PhpAssembly/*!*/ assembly)
		{
			this.assembly = assembly;
		}

		/// <summary>
		/// Initializes the 'GlobalTypeEmitter' field.
		/// </summary>
		/// <param name="moduleBuilder"></param>
		protected void DefineGlobalType(ModuleBuilder/*!*/ moduleBuilder)
		{
			TypeBuilder builder = moduleBuilder.DefineType(GenerateGlobalTypeName(),
                TypeAttributes.Class | TypeAttributes.NotPublic | TypeAttributes.Sealed);

			// empty default ctor:
			builder.DefineDefaultConstructor(MethodAttributes.PrivateScope);
            
			// cctor:
            ConstructorBuilder cctor_builder = builder.DefineTypeInitializer();

            this.globalTypeEmitter = new ILEmitter(cctor_builder);
		}

		/// <summary>
		/// Generates name for the 'global' type that stores information like
		/// entry-points, naming contexts etc. In transient assembly, we need to generate
		/// unique name for every loaded 'Transient' compilation unit.
		/// </summary>
		/// <returns></returns>
		protected abstract string GenerateGlobalTypeName();

		protected void BakeGlobals()
		{
			// finish initializer:
            globalTypeEmitter.Emit(OpCodes.Ret);

			// bake type:
			globalTypeEmitter.TypeBuilder.CreateType();

            // bake global functions and <Global Fields> type, only once:
            if (!globalTypeCreated)
            {
#if !SILVERLIGHT
                // Bake global CLR type:

                // Throws No Debug Module if the globals have been already created
                // This is checked in the condition above, the try/catch block is actually not needed.

                try
                { ReflectionUtils.CreateGlobalType(RealModuleBuilder); }
                catch (Exception) { Debug.Fail(); }
#else
			    // TODO: .. this is some hack.. 
#endif
                globalTypeCreated = true;
            }
		}

		#region Attributes

		public AST.PhpAttributeTargets AttributeTarget { get { return AST.PhpAttributeTargets.Assembly; } }

		public abstract AttributeTargets AcceptsTargets { get; }
		public abstract void ApplyCustomAttribute(AST.SpecialAttributes kind, Attribute attribute, AST.CustomAttribute.TargetSelectors selector);
		public abstract void CustomAttributeDefined(ErrorSink errors, AST.CustomAttribute customAttribute);
		public abstract void EmitCustomAttribute(CustomAttributeBuilder builder, AST.CustomAttribute.TargetSelectors selector);
		public abstract int GetAttributeUsageCount(DType type, AST.CustomAttribute.TargetSelectors selector);

		#endregion


		// TODO:
		//    private void AddResources(ModuleBuilder/*!*/ builder, ICollection resourcePaths, string outDir)
		//    {
		//		  if (resourcePaths != null && resourcePaths.Count > 0)
		//		  {
		//  		  Environment.CurrentDirectory = outDir;
		//		    int i = 0;
		//		    foreach (FullPath path in resourcePaths)
		//		    {
		//		      using (FileStream fs = new FileStream(path, FileMode.Open))
		//		      {
		//		        byte[] buffer = new byte[fs.Length];
		//		        fs.Read(buffer, 0, (int)fs.Length);
		//		        
		//		        IResourceWriter rw = builder.DefineResource("x.resource","blah");
		//		        rw.AddResource(path, "blah");
		//		      }  
		//		    }  
		//		  }
		//    }  
	}

	#endregion

	#region TransientAssemblyBuilder

	[DebuggerNonUserCode]
	public sealed class TransientAssemblyBuilder : PhpAssemblyBuilderBase
	{
		#region Properties

		public override bool IsExported { get { return false; } }
		public override bool IsPure { get { return false; } }
		public override bool IsTransient { get { return true; } }

		private object initializationMutex = new object();
		private volatile bool initialized = false;
		private bool debuggable = false;

		#endregion

		#region Construction

		public TransientAssemblyBuilder(ApplicationContext/*!*/ applicationContext)
			: base(new TransientAssembly(applicationContext))
		{
		}

		/// <summary>
		/// The argument <paramref name="completeSource" /> determines whether the source code
		/// is complete PHP script file, which is a case in dynamic includ in Silverlight
		/// </summary>
		public TransientCompilationUnit Build(string/*!*/ sourceCode, SourceCodeDescriptor descriptor,
			EvalKinds kind, CompilationContext/*!*/ context, ScriptContext/*!*/ scriptContext,
			DTypeDesc referringType, NamingContext namingContext, bool completeSource)
		{
			PhpSourceFile source_file = new PhpSourceFile(context.Config.Compiler.SourceRoot,
				RelativePath.ParseCanonical(descriptor.ContainingSourcePath));

			Encoding encoding = context.Config.Globalization.PageEncoding;

			TransientCompilationUnit result = new TransientCompilationUnit
				(sourceCode, source_file, encoding, namingContext, descriptor.Line, descriptor.Column, completeSource);
			if (!result.PreCompile(context, scriptContext, descriptor, kind, referringType)) return null;

			DefineGlobalType(((TransientModuleBuilder)result.ModuleBuilder).AssemblyBuilder.RealModuleBuilder);
			if (!result.Compile(context, kind)) return null;

			BakeGlobals();
			result.PostCompile(descriptor);
			return result;
		}

		private void InitializeRealAssembly(bool debuggable)
		{
			if (!initialized)
			{
				lock (initializationMutex)
				{
					if (!initialized)
					{
						AssemblyName assembly_name = new AssemblyName(TransientAssembly.RealAssemblyName);

						// TODO: do we need sync?
						AssemblyBuilder assembly_builder = AppDomain.CurrentDomain.DefineDynamicAssembly
							(assembly_name, AssemblyBuilderAccess.Run);
						ModuleBuilder module_builder = assembly_builder.DefineDynamicModule(TransientAssembly.RealModuleName, debuggable);

						assembly.WriteUp(module_builder, null);

						this.debuggable = debuggable;
						this.initialized = true;
					}
				}
			}
		}

		#endregion

		public TransientAssembly TransientAssembly { get { return (TransientAssembly)assembly; } }

		public TransientModuleBuilder/*!*/ DefineModule(TransientCompilationUnit/*!*/ compilationUnit, bool debuggable,
			int containerId, EvalKinds kind, string sourcePath)
		{
			InitializeRealAssembly(debuggable);
			Debug.Assert(this.debuggable == debuggable);

			return TransientAssembly.DefineModule(this, compilationUnit, containerId, kind, sourcePath);
		}

		internal bool IsTransientRealType(Type/*!*/ realType)
		{
			return initialized && realType.Assembly.Equals(RealAssemblyBuilder);
		}

		// Global type counter
		private static int globalTypeIndex = 0;

		/// <summary>
		/// Returns name for the global type.
		/// </summary>
		protected override string GenerateGlobalTypeName()
		{
            return string.Format("<TransientGlobal_{0}>", System.Threading.Interlocked.Increment(ref globalTypeIndex));
		}

		#region CustomAttributes

		public override AttributeTargets AcceptsTargets { get { return 0; } }

		public override void ApplyCustomAttribute(AST.SpecialAttributes kind, Attribute attribute, AST.CustomAttribute.TargetSelectors selector)
		{
			Debug.Fail("Custom attributes cannot be defined on transient assemblies or modules.");
			throw null;
		}

		public override void CustomAttributeDefined(ErrorSink errors, AST.CustomAttribute customAttribute)
		{
			Debug.Fail("Custom attributes cannot be defined on transient assemblies or modules.");
			throw null;
		}

		public override void EmitCustomAttribute(CustomAttributeBuilder builder, AST.CustomAttribute.TargetSelectors selector)
		{
			Debug.Fail("Custom attributes cannot be defined on transient assemblies or modules.");
			throw null;
		}

		public override int GetAttributeUsageCount(DType type, AST.CustomAttribute.TargetSelectors selector)
		{
			Debug.Fail("Custom attributes cannot be defined on transient assemblies or modules.");
			throw null;
		}

		#endregion

	}


	#endregion
}
