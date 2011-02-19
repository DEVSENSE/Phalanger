/*

 Copyright (c) 2007 Tomas Petricek

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Text;

using PHP.Core.Parsers;
using PHP.Core.Emit;


namespace PHP.Core.Reflection
{
	#region SourceCodeUnit

	/// <summary>
	/// Represents a source code that is stored in a string, but contains
	/// a complete PHP script file including the initial marks
	/// </summary>
	public sealed class ClientSourceCodeUnit : SourceCodeUnit
	{
		public ClientSourceCodeUnit(TransientCompilationUnit/*!*/ compilationUnit, string/*!*/ code, PhpSourceFile/*!*/ sourceFile,
			Encoding/*!*/ encoding, int line, int column)
			: base(compilationUnit, code, sourceFile, encoding, line, column)
		{
			this.initialState = Lexer.LexicalStates.INITIAL;
		}
	}

	#endregion
}
