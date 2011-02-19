/*

 Copyright (c) 2004-2006 Tomas Matousek.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;

namespace PHP.Core
{
	/// <summary>
	/// Common superclass for <see cref="Analyzer"/> and <see cref="CodeGenerator"/>.
	/// </summary>
	public abstract class AstVisitor
	{
		public abstract CompilationContext Context { get; }
	}
}
