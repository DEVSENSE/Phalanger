using System;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Web;
using System.Diagnostics;
using System.Collections;
using System.Threading;
using PHP.Core;
using PHP;

/*
  
  Designed by ...
  Implemented by ...
  
*/

namespace PHP.Core
{ /// <summary>
	/// PHP source code compiler
	/// </summary>
	public class SimpleCompiler
	{
		public SimpleCompiler() { }

		public void Reset() { }

		/// <summary>
		/// Emits constructors.
		/// </summary>
		private void EmitPageConstructors(TypeBuilder type)
		{
			ConstructorBuilder ctor;
			//      ILGenerator il;
			ConstructorInfo ci;

			//public instance void .ctor()
			//{
			//  ldarg.0
			//  call instance void [PhpNetCore]PHP.Core.PhpPage::.ctor()
			//  ret
			//} 

			ctor = type.DefineDefaultConstructor(MethodAttributes.Public);

			ci = typeof(PhpPage).GetConstructor(
			  BindingFlags.NonPublic | BindingFlags.Instance, // flags: instance family    
			  null,                                           // binder
			  new Type[] { },                                  // params
			  null);                                          // modifiers

			/* obsolete
			//public instance void .ctor(class [PhpNetCore]PHP.Core.ConfigurationRecord)
			//{
			//  ldarg.0
			//  ldarg.1
			//  call instance void [PhpNetCore]PHP.Core.PhpPage::.ctor(class [PhpNetCore]PHP.Core.ConfigurationRecord)
			//  ret
			//} 

			ctor = type.DefineConstructor(
			  MethodAttributes.Public,                         // flags
			  CallingConventions.Standard,                     // calling convention
			  new Type[] {typeof(ConfigurationRecord)});       // params
      
			ci = typeof(PhpPage).GetConstructor(
			  BindingFlags.NonPublic | BindingFlags.Instance,  // flags: instance family    
			  null,                                            // binder
			  new Type[] {typeof(ConfigurationRecord)},        // params
			  null);                                           // modifiers
      
			il = ctor.GetILGenerator();

			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldarg_1);
			il.Emit(OpCodes.Call,ci);
			il.Emit(OpCodes.Ret);
			*/
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="module"></param>
		/// <param name="sourceFilePath"></param>
		public void GeneratePageClass(ModuleBuilder module, string sourceFilePath)
		{
			ILGenerator il;
			//      ParsedScript script = (ParsedScript)context.Scripts[sourceFilePath];

			// Defines the page class:
			TypeBuilder type = module.DefineType(
			  "PhpNetPage",
			  TypeAttributes.Public | TypeAttributes.Class,
			  typeof(PhpPage));

			// Emits constructors:
			EmitPageConstructors(type);

			// Declares protected override void IntitializeGlobals() method:
			MethodBuilder method_InitializeGlobals = type.DefineMethod(
			  "InitializeGlobals",
			  MethodAttributes.Family | MethodAttributes.Virtual | MethodAttributes.HideBySig,
			  null,
			  null);

			il = method_InitializeGlobals.GetILGenerator();
			il.Emit(OpCodes.Ret);

			// Declares public override object Main(<ref global code vars>) method:
			MethodBuilder method_Main1 = type.DefineMethod(
			  "Main",
			  MethodAttributes.Public | MethodAttributes.HideBySig,
			  typeof(object),
			  null);

			il = method_Main1.GetILGenerator();

			Debug.Write("Loading PhpNetClassLibrary.dll ... ");
			try
			{
				AssemblyName assembly_name = new AssemblyName();
				assembly_name.Name = "PhpNetClassLibrary";
				assembly_name.SetPublicKeyToken(new byte[] { 0x4a, 0xf3, 0x7a, 0xfe, 0x3c, 0xde, 0x05, 0xfb });
				assembly_name.CultureInfo = new System.Globalization.CultureInfo("");
				assembly_name.Version = new Version(1, 0, 0, 0);

				Assembly ass = Assembly.Load(assembly_name);
				Debug.WriteLine("OK.");
				Debug.WriteLine(ass);

				Debug.Write("Searching for method PHP.PhpArray.Fill ... ");

				Type t = ass.GetType("PHP.PhpArrays");
				MethodInfo mi = t.GetMethod("Fill", BindingFlags.Static | BindingFlags.Public);
				Debug.WriteLine("OK.");
				Debug.WriteLine(mi);

				il.Emit(OpCodes.Ldc_I4_1);
				il.Emit(OpCodes.Ldc_I4_0);
				il.Emit(OpCodes.Ldnull);
				il.Emit(OpCodes.Call, mi);
				il.Emit(OpCodes.Pop);
			}
			catch (Exception e)
			{
				Debug.WriteLine("Exception: " + e);
			}

			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldfld, typeof(PhpPage).GetField("context", BindingFlags.Instance | BindingFlags.NonPublic));
			il.Emit(OpCodes.Callvirt, typeof(ScriptContext).GetMethod("get_Output"));
			il.Emit(OpCodes.Ldstr, "This is " + sourceFilePath);
			il.Emit(OpCodes.Callvirt, typeof(TextWriter).GetMethod("Write", new Type[] { typeof(string) }));

			il.Emit(OpCodes.Ldnull);
			il.Emit(OpCodes.Ret);

			// Declares protected override void Main() method:
			MethodBuilder method_Main2 = type.DefineMethod(
			  "Main",
			  MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig,
			  null,
			  null);

			il = method_Main2.GetILGenerator();
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Call, method_Main1);
			il.Emit(OpCodes.Pop);
			il.Emit(OpCodes.Ret);

			// Creates the page class:
			type.CreateType();
		}

	}
}
