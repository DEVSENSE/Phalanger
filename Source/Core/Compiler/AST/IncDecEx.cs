/*

 Copyright (c) 2004-2006 Tomas Matousek, Vaclav Novak, and Martin Maly.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Diagnostics;
using System.Reflection.Emit;
using PHP.Core.Emit;
using PHP.Core.Parsers;

namespace PHP.Core.AST
{
	/// <summary>
	/// Post/pre increment/decrement expression.
	/// </summary>
	public sealed class IncDecEx : Expression
	{
		internal override Operations Operation { get { return Operations.IncDec; } }

		private bool inc;
        /// <summary>Indicates incrementation.</summary>
        public bool Inc { get { return inc; } }
		private bool post;
        /// <summary>Indicates post-incrementation or post-decrementation</summary>
        public bool Post { get { return post; } }

		private VariableUse/*!*/ variable;
        /// <summary>Variable being incremented/decremented</summary>
        public VariableUse /*!*/ Variable { get { return variable; } }

		public IncDecEx(Position position, bool inc, bool post, VariableUse/*!*/ variable)
			: base(position)
		{
			this.variable = variable;
			this.inc = inc;
			this.post = post;
		}

		internal override Evaluation Analyze(Analyzer/*!*/ analyzer, ExInfoFromParent info)
		{
			access = info.Access;
			ExInfoFromParent var_info = new ExInfoFromParent(this);
			var_info.Access = AccessType.ReadAndWrite;

			variable.Analyze(analyzer, var_info);

			return new Evaluation(this);
		}

		#region Code generation

		/// <include file='Doc/Nodes.xml' path='doc/method[@name="Emit"]/*'/>
		internal override PhpTypeCode Emit(CodeGenerator codeGenerator)
		{
			Statistics.AST.AddNode("IncDecEx");
			Debug.Assert(access == AccessType.Read || access == AccessType.None);

            AccessType old_selector = codeGenerator.AccessSelector;

			PhpTypeCode returned_typecode = PhpTypeCode.Void;

			codeGenerator.AccessSelector = AccessType.Write;
			codeGenerator.ChainBuilder.Create();
			variable.Emit(codeGenerator);
			codeGenerator.AccessSelector = AccessType.Read;
			codeGenerator.ChainBuilder.Create();
			variable.Emit(codeGenerator);
			codeGenerator.ChainBuilder.End();

			LocalBuilder old_value = null;

			if (access == AccessType.Read && post)
			{
				old_value = codeGenerator.IL.DeclareLocal(Types.Object[0]);
				// Save variable's value for later use
				codeGenerator.IL.Emit(OpCodes.Dup);
				codeGenerator.IL.Stloc(old_value);
			}

			if (this.inc)
			{
				// Increment
				codeGenerator.IL.Emit(OpCodes.Call, Methods.Operators.Increment);
			}
			else
			{
				// Decrement
				codeGenerator.IL.Emit(OpCodes.Call, Methods.Operators.Decrement);
			}

			codeGenerator.AccessSelector = AccessType.Write;
			if (access == AccessType.Read)
			{
				if (post)
				{
					variable.EmitAssign(codeGenerator);
					// Load original value (as was before operation)
					codeGenerator.IL.Ldloc(old_value);
				}
				else
				{
					old_value = codeGenerator.IL.DeclareLocal(Types.Object[0]);
					// pre-incrementation
					// Load variable's value after operation
					codeGenerator.IL.Emit(OpCodes.Dup);
					codeGenerator.IL.Stloc(old_value);
					variable.EmitAssign(codeGenerator);
					codeGenerator.IL.Ldloc(old_value);
				}

				returned_typecode = PhpTypeCode.Object;
			}
			else
			{
				variable.EmitAssign(codeGenerator);
			}
            codeGenerator.AccessSelector = old_selector;

			codeGenerator.ChainBuilder.End();

			return returned_typecode;
		}
		#endregion

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitIncDecEx(this);
        }
	}
}
