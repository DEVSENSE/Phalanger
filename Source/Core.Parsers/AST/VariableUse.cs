/*

 Copyright (c) 2006- DEVSENSE
 Copyright (c) 2004-2006 Tomas Matousek, Ladislav Prosek, Vaclav Novak and Martin Maly.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Diagnostics;
using PHP.Core.Parsers;

namespace PHP.Core.AST
{
	#region VariableUse

	/// <summary>
	/// Base class for variable uses.
	/// </summary>
    [Serializable]
	public abstract class VariableUse : VarLikeConstructUse
	{
		protected VariableUse(Position p) : base(p) { }
	}

	#endregion

	#region CompoundVarUse

	/// <summary>
	/// Base class for compound variable uses.
	/// </summary>
    [Serializable]
    public abstract class CompoundVarUse : VariableUse
	{
		protected CompoundVarUse(Position p) : base(p) { }
	}

	#endregion

	#region SimpleVarUse

	/// <summary>
	/// Base class for simple variable uses.
	/// </summary>
    [Serializable]
    public abstract class SimpleVarUse : CompoundVarUse
	{
		protected SimpleVarUse(Position p) : base(p) { }
	}

	#endregion
}
