/*

 Copyright (c) 2004-2006 Tomas Matousek, Vaclav Novak and Martin Maly.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using PHP.Core.Emit;
using PHP.Core.Parsers;
using PHP.Core.Reflection;

namespace PHP.Core.AST
{
	#region IncludingEx

	/// <summary>
	/// Inclusion expression (include, require, synthetic auto-inclusion nodes).
	/// </summary>
	public sealed partial class IncludingEx : Expression
	{
		/// <summary>
		/// Static inclusion info or <B>null</B> reference if target cannot be determined statically.
		/// Set during inclusion graph building, before the analysis takes place.
		/// </summary>
		public StaticInclusion Inclusion { get { return inclusion; } internal /* CompilationUnit */ set { inclusion = value; } }
		private StaticInclusion inclusion;

		/// <summary>
		/// Set during inclusion graph building, before the analysis takes place.
		/// </summary>
		internal Characteristic Characteristic { get { return characteristic; } set { characteristic = value; } }
		private Characteristic characteristic;

		/// <include file='Doc/Nodes.xml' path='doc/method[@name="Expression.Analyze"]/*'/>
		internal override Evaluation Analyze(Analyzer/*!*/ analyzer, ExInfoFromParent info)
		{
			access = info.Access;

			// if the expression should be emitted:
			if (characteristic == Characteristic.Dynamic || characteristic == Characteristic.StaticArgEvaluated)
			{
				fileNameEx = fileNameEx.Analyze(analyzer, ExInfoFromParent.DefaultExInfo).Literalize();
			}

			analyzer.AddCurrentRoutineProperty(RoutineProperties.ContainsInclude);

			analyzer.CurrentScope = this.scope;

			return new Evaluation(this);
		}

		/// <include file='Doc/Nodes.xml' path='doc/method[@name="Emit"]/*'/>
		internal override PhpTypeCode Emit(CodeGenerator/*!*/ codeGenerator)
		{
			PhpTypeCode result;

			// emits inclusion and Main() call:
			if (inclusion != null)
				result = EmitStaticInclusion(codeGenerator);
			else
				result = EmitDynamicInclusion(codeGenerator);

			// return value conversion:
			codeGenerator.EmitReturnValueHandling(this, false, ref result);

			return result;
		}

		/// <summary>
		/// Emits a static inclusion.
		/// </summary>
		private PhpTypeCode EmitStaticInclusion(CodeGenerator/*!*/ codeGenerator)
		{
			ILEmitter il = codeGenerator.IL;
			Label endif_label = il.DefineLabel();
			Label else_label = il.DefineLabel();
			MethodInfo method;

			// if the expression should be emitted:
			if (characteristic == Characteristic.StaticArgEvaluated)
			{
                if (!(fileNameEx is StringLiteral || fileNameEx is BinaryStringLiteral))
                {
                    // emits expression evaluation and ignores the result:
                    fileNameEx.Emit(codeGenerator);
                    il.Emit(OpCodes.Pop);
                }
			}

			if (characteristic == Characteristic.StaticAutoInclusion)
			{
				// calls the Main routine only if this script is the main one:
				il.Ldarg(ScriptBuilder.ArgIsMain);
			}
			else
			{
                RelativePath relativePath = new RelativePath(inclusion.Includee.RelativeSourcePath);    // normalize the relative path

				// CALL context.StaticInclude(<relative included script source path>,<this script type>,<inclusion type>);
				codeGenerator.EmitLoadScriptContext();
                il.Emit(OpCodes.Ldc_I4, (int)relativePath.Level);
                il.Emit(OpCodes.Ldstr, relativePath.Path);
				il.Emit(OpCodes.Ldtoken, inclusion.Includee.ScriptClassType);
				il.LoadLiteral(inclusionType);
				il.Emit(OpCodes.Call, Methods.ScriptContext.StaticInclude);
			}

			// IF (STACK)
			il.Emit(OpCodes.Brfalse, else_label);
			if (true)
			{
				// emits a call to the main helper of the included script:
				method = inclusion.Includee.MainHelper;

				// CALL <Main>(context, variables, self, includer, false):
				codeGenerator.EmitLoadScriptContext();
				codeGenerator.EmitLoadRTVariablesTable();
				codeGenerator.EmitLoadSelf();
				codeGenerator.EmitLoadClassContext();
				il.Emit(OpCodes.Ldc_I4_0);
				il.Emit(OpCodes.Call, method);

				il.Emit(OpCodes.Br, endif_label);
			}

			// ELSE

			il.MarkLabel(else_label);
			if (true)
			{
				// LOAD <PhpScript.SkippedIncludeReturnValue>;                          
				il.LoadLiteral(ScriptModule.SkippedIncludeReturnValue);
				il.Emit(OpCodes.Box, ScriptModule.SkippedIncludeReturnValue.GetType());
			}

			il.MarkLabel(endif_label);
			// END IF 

			return PhpTypeCode.Object;
		}
	}

	#endregion
}
