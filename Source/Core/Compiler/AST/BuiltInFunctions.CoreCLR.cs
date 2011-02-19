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
		/// <include file='Doc/Nodes.xml' path='doc/method[@name="Expression.Analyze"]/*'/>
		internal override Evaluation Analyze(Analyzer/*!*/ analyzer, ExInfoFromParent info)
		{
			access = info.Access;
			fileNameEx = fileNameEx.Analyze(analyzer, ExInfoFromParent.DefaultExInfo).Literalize();
			analyzer.AddCurrentRoutineProperty(RoutineProperties.ContainsInclude);
			analyzer.CurrentScope = this.scope;
			return new Evaluation(this);
		}

		/// <include file='Doc/Nodes.xml' path='doc/method[@name="Emit"]/*'/>
		internal override PhpTypeCode Emit(CodeGenerator/*!*/ codeGenerator)
		{
			PhpTypeCode result;

			// emits inclusion and Main() call:
			result = EmitDynamicInclusion(codeGenerator);

			// return value conversion:
			codeGenerator.EmitReturnValueHandling(this, false, ref result);
			return result;
		}
	}

	#endregion
}
