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
using System.Reflection.Emit;
using System.Diagnostics;
using PHP.Core.Emit;
using PHP.Core.Parsers;

namespace PHP.Core.AST
{
	/// <summary>
	/// Represents <c>array</c> constructor.
	/// </summary>
	public sealed class ArrayEx : Expression
	{
		internal override Operations Operation { get { return Operations.Array; } }

		private readonly List<Item>/*!*/ items;
        public List<Item>/*!*/ Items{get{return items;}}

		public ArrayEx(Position position, List<Item>/*!*/ items)
			: base(position)
		{
			Debug.Assert(items != null);
			this.items = items;
		}

		#region Analysis

		internal override Evaluation Analyze(Analyzer/*!*/ analyzer, ExInfoFromParent info)
		{
			access = info.Access;

			foreach (Item i in items)
				if (i != null) i.Analyze(analyzer);

			return new Evaluation(this);
		}

		#endregion

		#region Code Emission

		/// <include file='Doc/Nodes.xml' path='doc/method[@name="IsDeeplyCopied"]/*'/>
		/// <returns>It suffice to make a copy only if assignment nesting level is 1 or above (we are starting from 0).</returns>
		internal override bool IsDeeplyCopied(CopyReason reason, int nestingLevel)
		{
			return nestingLevel > 0;
		}

		/// <include file='Doc/Nodes.xml' path='doc/method[@name="Emit"]/*'/>
		internal override PhpTypeCode Emit(CodeGenerator codeGenerator)
		{
			Debug.Assert(access == AccessType.Read || access == AccessType.None);
			ILEmitter il = codeGenerator.IL;

			// count integer and string keys:
			int int_count = 0;
			int string_count = 0;
            DetermineCapacities(out int_count, out string_count);

			// array = new PhpArray(<int_count>, <string_count>);
			il.Emit(OpCodes.Ldc_I4, int_count);
			il.Emit(OpCodes.Ldc_I4, string_count);
			il.Emit(OpCodes.Newobj, Constructors.PhpArray.Int32_Int32);

            if (codeGenerator.Context.Config.Compiler.Debug)
            {
                il.Emit(OpCodes.Nop);
                il.Emit(OpCodes.Nop);
                il.Emit(OpCodes.Nop);
            }

			foreach (Item item in items)
			{
				// CALL array.SetArrayItemRef(z, p);
				// CALL array.SetArrayItem(x, PhpVariable.Copy(y, CopyReason.Assigned));
				// CALL array.SetArrayItem(PhpVariable.Copy(x, CopyReason.Assigned))
                // CALL array.AddToEnd(x)

                il.Emit(OpCodes.Dup);
				PhpTypeCode index_type_code = item.EmitIndex(codeGenerator);
				item.EmitValue(codeGenerator);
				codeGenerator.EmitSetArrayItem(index_type_code, item.Index, item is RefItem, true);
			}

			switch (this.access)
			{
				case AccessType.Read:
					// keep array on the stack
					return PhpTypeCode.PhpArray;

				case AccessType.None:
					// pop array from the stack
                    il.Emit(OpCodes.Pop);
					return PhpTypeCode.Void;
			}

            Debug.Fail();
			return PhpTypeCode.Invalid;
		}

        private void DetermineCapacities(out int intCount, out int stringCount)
		{
			intCount = 0;
			stringCount = 0;
            
			foreach (Item item in items)
			{
				if (item.HasKey)
				{
            		if (item.IsIndexStringLiteral)
						stringCount++;
					else
						intCount++; // Item is IntLiteral, Variable, Constant, etc.
				}
				else
					intCount++;
			}
		}

		#endregion

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitArrayEx(this);
        }
	}

	#region Item

	/// <summary>
	/// Base class for item of an array defined by <c>array</c> constructor.
	/// </summary>
	public abstract class Item : AstNode
	{
		public Expression Index { get { return index; } }
		protected Expression index; // can be null

		protected Item(Expression index)
		{
			this.index = index;
		}

		internal bool HasKey { get { return (index != null); } }
		internal bool IsIndexLiteral { get { return index is Literal; } }
		internal bool IsIndexIntLiteral { get { return index is IntLiteral; } }
		internal bool IsIndexStringLiteral { get { return index is StringLiteral; } }

		internal virtual void Analyze(Analyzer/*!*/ analyzer)
		{
			if (index != null)
				index = index.Analyze(analyzer, ExInfoFromParent.DefaultExInfo).Literalize();
		}

		/// <summary>
		/// Emit IL instructions that load the value of array index at the stack.
		/// </summary>
		internal PhpTypeCode EmitIndex(CodeGenerator/*!*/ codeGenerator)
		{
			return codeGenerator.EmitArrayKey(null, index);
		}

		internal abstract PhpTypeCode EmitValue(CodeGenerator/*!*/ codeGenerator);
	}

	#endregion

	#region ValueItem

	/// <summary>
	/// Expression for the value of an array item defined by <c>array</c> constructor.
	/// </summary>
	public sealed class ValueItem : Item
	{
		private Expression valueExpr;
        /// <summary>Value of array item</summary>
        public Expression ValueExpr{get{return valueExpr;}}

		public ValueItem(Expression index, Expression/*!*/ valueExpr)
			: base(index)
		{
			Debug.Assert(valueExpr != null);
			this.valueExpr = valueExpr;
		}

		internal override void Analyze(Analyzer/*!*/ analyzer)
		{
			base.Analyze(analyzer);
			valueExpr = valueExpr.Analyze(analyzer, ExInfoFromParent.DefaultExInfo).Literalize();
		}

		/// <summary>
		/// Emit IL instructions that load the value of array item at the stack and make a copy 
		/// of it if necessary.
		/// </summary>
		internal override PhpTypeCode EmitValue(CodeGenerator/*!*/ codeGenerator)
		{
			Debug.Assert(valueExpr != null);
			Statistics.AST.AddNode("Array.ValueItem");

			codeGenerator.EmitBoxing(valueExpr.Emit(codeGenerator));
			codeGenerator.EmitVariableCopy(CopyReason.Assigned, valueExpr);

			return PhpTypeCode.Object;
		}
	}

	#endregion

	#region RefItem

	/// <summary>
	/// Reference to a variable containing the value of an array item defined by <c>array</c> constructor.
	/// </summary>
	public sealed class RefItem : Item
	{
		private readonly VariableUse/*!*/refToGet;
        /// <summary>Object to obtain reference of</summary>
        public VariableUse/*!*/RefToGet { get { return this.refToGet; } }

		public RefItem(Expression index, VariableUse refToGet)
			: base(index)
		{
            Debug.Assert(refToGet != null);
            this.refToGet = refToGet;
		}

		internal override void Analyze(Analyzer analyzer)
		{
			ExInfoFromParent info = new ExInfoFromParent(this);
			info.Access = AccessType.ReadRef;
			refToGet.Analyze(analyzer, info);
			base.Analyze(analyzer);
		}

		/// <summary>
		/// Emit IL instructions that load the value of array item at the stack and make a copy 
		/// of it if necessary.
		/// </summary>
		/// <param name="codeGenerator"></param>
		/// <returns></returns>
		/// <remarks>This node represents the item x=>&amp;y in PHP notation. See <see cref="PHP.Core.AST.ArrayEx"/>
		///  for more details.</remarks>
		internal override PhpTypeCode EmitValue(CodeGenerator codeGenerator)
		{
			Debug.Assert(refToGet != null);
			Statistics.AST.AddNode("Array.RefItem");

			// Emit refToGet
			return refToGet.Emit(codeGenerator);
		}
	}

	#endregion

}