/*

 Copyright (c) 2004-2006 Tomas Matousek, Ladislav Prosek, Vaclav Novak, and Martin Maly.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.IO;
using System.Diagnostics;
using System.Reflection.Emit;
using PHP.Core.Emit;
using PHP.Core.Parsers;

namespace PHP.Core.AST
{
	/// <summary>
	/// Access to an item of a structured variable by [] PHP operator.
	/// </summary>
	public sealed class ItemUse : CompoundVarUse
	{
		internal override Operations Operation { get { return Operations.ItemUse; } }

        /// <summary>
        /// Whether this represents function array dereferencing.
        /// </summary>
        public bool IsFunctionArrayDereferencing { get { return this.functionArrayDereferencing; } }
        private readonly bool functionArrayDereferencing = false;

		/// <summary>
		/// Variable used as an array identifier.
		/// </summary>
        public VarLikeConstructUse Array { get { return array; } set { array = value; } }
        private VarLikeConstructUse/*!*/ array;

		/// <summary>
		/// Expression used as an array index. 
		/// A <B>null</B> reference means key-less array operator (write context only).
		/// </summary>
		public Expression Index { get { return index; } }
		private Expression index;
		
		/// <summary>
		/// Set when the index is emitted.
		/// </summary>
		private PhpTypeCode indexTypeCode = PhpTypeCode.Unknown;

        public ItemUse(Position p, VarLikeConstructUse/*!*/ array, Expression index, bool functionArrayDereferencing = false)
			: base(p)
		{
			Debug.Assert(array != null);

			this.array = array;
			this.index = index;
            this.functionArrayDereferencing = functionArrayDereferencing;
		}

		/// <include file='Doc/Nodes.xml' path='doc/method[@name="Expression.Analyze"]/*'/>
		internal override Evaluation Analyze(Analyzer/*!*/ analyzer, ExInfoFromParent info)
		{
			access = info.Access;

			// checks for write context of key-less array operator ($a =& $x[] is ok):
			if (index == null
				&& (access == AccessType.Read
					|| access == AccessType.ReadAndWrite
					|| access == AccessType.ReadAndWriteAndReadRef
					|| access == AccessType.ReadAndWriteAndReadUnknown))
			{
				analyzer.ErrorSink.Add(Errors.EmptyIndexInReadContext, analyzer.SourceUnit, position);
				return new Evaluation(this);
			}

			base.Analyze(analyzer, info);

			ExInfoFromParent sinfo = new ExInfoFromParent(this);
			switch (info.Access)
			{
				case AccessType.Write:
				case AccessType.WriteRef:
				case AccessType.ReadRef: sinfo.Access = AccessType.Write; break;

				case AccessType.ReadAndWriteAndReadRef:
				case AccessType.WriteAndReadRef:
				case AccessType.ReadAndWrite: sinfo.Access = AccessType.ReadAndWrite; break;

				case AccessType.WriteAndReadUnknown:
				case AccessType.ReadAndWriteAndReadUnknown: sinfo.Access = info.Access; break;

				case AccessType.ReadUnknown: sinfo.Access = AccessType.ReadUnknown; break;
				default: sinfo.Access = AccessType.Read; break;
			}

			array.Analyze(analyzer, sinfo);

			if (index != null)
				index = index.Analyze(analyzer, ExInfoFromParent.DefaultExInfo).Literalize();

			return new Evaluation(this);
		}

		/// <include file='Doc/Nodes.xml' path='doc/method[@name="Emit"]/*'/>
		internal override PhpTypeCode Emit(CodeGenerator/*!*/ codeGenerator)
		{
			Statistics.AST.AddNode("ItemUse");
			PhpTypeCode result = PhpTypeCode.Invalid;

			switch (codeGenerator.SelectAccess(access))
			{
				case AccessType.None:
					result = EmitNodeRead(codeGenerator, Operators.GetItemKinds.Get);
					codeGenerator.IL.Emit(OpCodes.Pop);
					break;

				case AccessType.Read:
					result = EmitNodeRead(codeGenerator, Operators.GetItemKinds.Get);
					break;

				case AccessType.Write:
					// prepares for write:
					result = EmitNodeWrite(codeGenerator);
					break;

				case AccessType.ReadRef:
					// if the selector is set to the ReadRef, the chain is emitted as if it was written
					// (chained nodes are marked as ReadAndWrite):
					if (codeGenerator.AccessSelector == AccessType.ReadRef)
						codeGenerator.AccessSelector = AccessType.Write;

					result = EmitNodeReadRef(codeGenerator);
					break;

				case AccessType.ReadUnknown:
					result = EmitNodeReadUnknown(codeGenerator);
					break;

				case AccessType.WriteRef:
					// prepares for write:					
					result = EmitNodeWriteRef(codeGenerator);
					break;

				default:
					Debug.Fail();
					break;
			}

			return result;
		}

		/// <summary>
		/// Finishes the write operation starte by <see cref="Emit"/>.
		/// </summary>
		internal override PhpTypeCode EmitAssign(CodeGenerator/*!*/ codeGenerator)
		{
			ChainBuilder chain = codeGenerator.ChainBuilder;
			PhpTypeCode result;

			switch (access)
			{
				case AccessType.WriteAndReadRef:
				case AccessType.WriteAndReadUnknown:
				case AccessType.ReadAndWrite:
				case AccessType.ReadAndWriteAndReadRef:
				case AccessType.ReadAndWriteAndReadUnknown:
				case AccessType.Write:
				case AccessType.WriteRef:
					{
						bool reference = access == AccessType.WriteRef;

						// Note that some work was done in Emit() !
						// In cases 3, 4, 5 EmitAssign is not called

						if (isMemberOf != null ||
						   (isMemberOf == null && (array is DirectStFldUse || array is IndirectStFldUse || array is ItemUse)))
						{
							// 2, 6, 7
							chain.EmitSetArrayItem(indexTypeCode, index, reference);
							chain.End();
						}
						else
						{
							// Note: The value which should be stored is already loaded on the evaluation stack.
							//				Push the destination array and index and call the operator
							// 1: a_[x]_
							Debug.Assert(array is SimpleVarUse);
							chain.IsArrayItem = true;
							chain.IsLastMember = true;
							indexTypeCode = codeGenerator.EmitArrayKey(chain, index);
							array.Emit(codeGenerator);
							chain.EmitSetItem(indexTypeCode, index, reference);

							// Store the changed variable into table of variables (do nothing in optimalized functions)
							((SimpleVarUse)array).EmitLoadAddress_StoreBack(codeGenerator);
						}

						result = PhpTypeCode.Void;
						break;
					}

				case AccessType.None:
					// do nothing
					result = PhpTypeCode.Void;
					break;

				case AccessType.Read:
					// do nothing
					result = PhpTypeCode.Object;
					break;

				case AccessType.ReadRef:
					// Do nothing
					result = PhpTypeCode.PhpReference;
					break;

				default:
					Debug.Fail();
					result = PhpTypeCode.Invalid;
					break;
			}

			return result;
		}

		internal override void EmitUnset(CodeGenerator codeGenerator)
		{
			ChainBuilder chain = codeGenerator.ChainBuilder;

			// Template: "unset(x[y])"  Operators.UnsetItem(object obj,object index)

			// Case 3: a_[x]_[x] never reached
			Debug.Assert(chain.IsArrayItem == false);
			// Case 4,5 never reached
			// 4: a[x]->...
			// 5: ...->a[]->...
			Debug.Assert(chain.IsMember == false);

			chain.QuietRead = true;

			// 1, 2, 6, 7
			if (this.isMemberOf != null)
			{
				// 6 , 7: ...->a[]_[]_ , ...->a_[]_
				chain.Create();
				chain.Begin();
				chain.Lengthen(); // for hop over ->
				isMemberOf.Emit(codeGenerator);
				chain.IsArrayItem = true;
				chain.IsLastMember = false;
				chain.EmitUnsetItem(array, index);
				chain.IsArrayItem = false;
				chain.End();
				return;
			}
			// 1, 2
			if (array is ItemUse || array is DirectStFldUse || array is IndirectStFldUse /* ??? */)
			{
				// 2: a[]_[]_
				chain.Create();
				chain.Begin();
				chain.IsArrayItem = true;
				chain.IsLastMember = true;
				chain.EmitUnsetItem(array, index);
				chain.IsArrayItem = false;
				chain.End();
				return;
			}
			// 1: a_[x]_
			chain.IsArrayItem = true;
			chain.IsLastMember = true;
			chain.EmitUnsetItem(array, index);
			chain.IsArrayItem = false;
		}

		internal override PhpTypeCode EmitIsset(CodeGenerator codeGenerator, bool empty)
		{
			//Template:
			//		"isset(x[y])"  Operators.GetItem(x,y) != null      

			codeGenerator.ChainBuilder.QuietRead = true;

			// GetItem(x,y) ...
			this.EmitNodeRead(codeGenerator, (empty) ? Operators.GetItemKinds.Empty : Operators.GetItemKinds.Isset);
			return PhpTypeCode.Object;
		}

		/// <summary>
		/// Emits code to load variable onto the evaluation stack. Supports operators chaining.
		/// </summary>
		/// <param name="codeGenerator">A geenrator.</param>
		/// <param name="itemGetterKind">Whether to load for "get", "isset", or "empty".</param>
		private PhpTypeCode EmitNodeRead(CodeGenerator/*!*/ codeGenerator, Operators.GetItemKinds itemGetterKind)
		{
			ChainBuilder chain = codeGenerator.ChainBuilder;
			PhpTypeCode result;

			if (chain.IsArrayItem)
			{
				// we are in the array subchain //

				// 3: a_[x]_[x]
				chain.Lengthen(); // for []
				result = chain.EmitGetItem(array, index, itemGetterKind);
				return result;
			}

			// 1,2,4,5,6,7
			if (chain.IsMember)
			{
				// we are in the field chain //

				// 4, 5
				if (this.isMemberOf != null)
				{
					// we are in the middle of the field chain //

					// 5: ...->a[]->...

					// Lengthen chain for isMemberOf
					chain.Lengthen(); // for hop over ->

					isMemberOf.Emit(codeGenerator);

					// Lengthen chain for own []
					chain.Lengthen();

					chain.IsArrayItem = true;
					chain.IsLastMember = false;

					result = chain.EmitGetItem(array, index, itemGetterKind);

					chain.IsArrayItem = false;
					return result;
				}
				else
				{
					// we are at the beginning of the field chain //

					// 4: a[x]->...
					// Lengthen chain for itself
					chain.Lengthen(); // for own []
					chain.IsArrayItem = true;
					chain.IsLastMember = true;

					result = chain.EmitGetItem(array, index, itemGetterKind);

					chain.IsArrayItem = false;
					return result;
				}
			}

			// 1, 2, 6, 7
			if (this.isMemberOf != null)
			{
				// last node of the field chain //

				// 6 , 7: ...->a[]_[]_ , ...->a_[]_
				bool quiet_read = chain.QuietRead;
				chain.Create();
				chain.Begin();
				chain.QuietRead = quiet_read;
				chain.Lengthen(); // for hop over ->

				isMemberOf.Emit(codeGenerator);

				// let's emit the array subchain followed by the GetItem:
				chain.IsArrayItem = true;
				chain.IsLastMember = false;
				result = chain.EmitGetItem(array, index, itemGetterKind);
				chain.IsArrayItem = false;
				chain.End();
				return result;
			}

			// 1, 2
			if (array is ItemUse || array is DirectStFldUse || array is IndirectStFldUse)
			{
				// we are at the beginning of the field chain //

				// 2: a[]_[]_
				bool quiet_read = chain.QuietRead;
				chain.Create();
				chain.Begin();
				chain.QuietRead = quiet_read;
				chain.IsArrayItem = true;
				chain.IsLastMember = true;

				result = chain.EmitGetItem(array, index, itemGetterKind);

				chain.IsArrayItem = false;
				chain.End();
				return result;
			}

			// no chains //

			// 1: a_[x]_
			chain.IsArrayItem = true;
			chain.IsLastMember = true;
			result = chain.EmitGetItem(array, index, itemGetterKind);
			chain.IsArrayItem = false;
			return result;
		}

		/// <summary>
		/// Emits code to load a reference to a variable onto an evaluation stack.  Supports operators chaining.
		/// </summary>
		/// <param name="codeGenerator"></param>
		private PhpTypeCode EmitNodeReadRef(CodeGenerator codeGenerator)
		{
			ChainBuilder chain = codeGenerator.ChainBuilder;
			LocalBuilder local = codeGenerator.IL.DeclareLocal(typeof(object));

			// Case 3: a_[x]_[x] never reached
			Debug.Assert(chain.IsArrayItem == false, "ReadRef access shouldn't be set to array subchain nodes");

			// Case 4,5 never reached
			// 4: a[x]->...
			// 5: ...->a[]->...
			Debug.Assert(chain.IsMember == false);

			// 1, 2, 6, 7
			if (this.isMemberOf != null)
			{
				// last node of the field chain //

				// 6 , 7: ...->a[]_[]_ , ...->a_[]_
				chain.Create();
				chain.Begin();
				if (this.isMemberOf is FunctionCall)
					chain.LoadAddressOfFunctionReturnValue = true;

				chain.SetObjectForLazyEmit(this);

				// let's emit the array subchain followed by the GetArrayItemRef:
				chain.IsArrayItem = true;
				chain.IsLastMember = false;
				chain.Lengthen(); // for own []
				chain.EmitGetArrayItemRef(array, index);
				chain.IsArrayItem = false;
				chain.EndRef();
				return PhpTypeCode.PhpReference;
			}

			// 1, 2
			if (array is ItemUse || array is DirectStFldUse || array is IndirectStFldUse)
			{
				// we are at the beginning of the field chain //

				// 2: a[]_[]_
				chain.Create();
				chain.Begin();
				chain.IsArrayItem = true;
				chain.IsLastMember = true;
				chain.Lengthen();
				chain.EmitGetArrayItemRef(array, index);
				chain.IsArrayItem = false;
				chain.EndRef();
				return PhpTypeCode.PhpReference;
			}

			// no chains //

			// 1: a_[x]_
			return chain.EmitGetItemRef((SimpleVarUse)array, index);
		}

		/// <summary>
		/// Emits code to load <see cref="PhpRuntimeChain"/> onto an evaluation stack. Supports operators chaining.
		/// </summary>
		/// <param name="codeGenerator"></param>
		private PhpTypeCode EmitNodeReadUnknown(CodeGenerator codeGenerator)
		{
			ChainBuilder chain = codeGenerator.ChainBuilder;
			PhpTypeCode result = PhpTypeCode.PhpRuntimeChain;

			if (chain.IsArrayItem)
			{
				// 3: a_[x]_[x]
				chain.Lengthen(); // for []
				chain.EmitRTChainAddItem(this);
				return result;
			}
			// 1,2,4,5,6,7
			if (chain.IsMember)
			{
				// 4, 5
				if (this.isMemberOf != null)
				{
					// 5: ...->a[]->...

					// Lengthen chain for isMemberOf
					chain.Lengthen(); // for hop over ->
					PhpTypeCode res = isMemberOf.Emit(codeGenerator);
					if (res != PhpTypeCode.PhpRuntimeChain)
					{
						codeGenerator.EmitBoxing(res);
						chain.EmitCreateRTChain();
					}
					// Lengthen chain for own []
					chain.Lengthen();
					chain.IsArrayItem = true;
					chain.IsLastMember = false;
					chain.EmitRTChainAddItem(this);
					chain.IsArrayItem = false;
					return result;
				}
				// 4: a[x]->...
				// Lengthen chain for itself
				chain.Lengthen(); // for own []
				chain.IsArrayItem = true;
				chain.IsLastMember = true;
				chain.EmitRTChainAddItem(this);
				chain.IsArrayItem = false;
				return result;
			}
			// 1, 2, 6, 7
			if (this.isMemberOf != null)
			{
				// 6 , 7: ...->a[]_[]_ , ...->a_[]_
				bool quiet_read = chain.QuietRead;
				chain.Create();
				chain.Begin();
				chain.QuietRead = quiet_read;
				chain.Lengthen(); // for hop over ->
				PhpTypeCode res = isMemberOf.Emit(codeGenerator);
				if (res != PhpTypeCode.PhpRuntimeChain)
				{
					codeGenerator.EmitBoxing(res);
					chain.EmitCreateRTChain();
				}
				chain.IsArrayItem = true;
				chain.IsLastMember = false;
				chain.EmitRTChainAddItem(this);
				chain.IsArrayItem = false;
				chain.End();
				return result;
			}
			// 1, 2
			if (array is ItemUse || array is DirectStFldUse || array is IndirectStFldUse /* ??? */)
			{
				// 2: a[]_[]_
				bool quiet_read = chain.QuietRead;
				chain.Create();
				chain.Begin();
				chain.QuietRead = quiet_read;
				chain.IsArrayItem = true;
				chain.IsLastMember = true;
				chain.EmitRTChainAddItem(this);
				chain.IsArrayItem = false;
				chain.End();
				return result;
			}
			// 1: a_[x]_
			chain.IsArrayItem = true;
			chain.IsLastMember = true;
			chain.EmitRTChainAddItem(this);
			chain.IsArrayItem = false;
			return result;
		}

		/// <summary>
		/// Emits code to prepare an evaluation stack for storing a value into a variable.
		/// Supports operators chaining. Store is finished by calling <see cref="EmitAssign"/>.
		/// </summary>
		/// <param name="codeGenerator"></param>
		private PhpTypeCode EmitNodeWrite(CodeGenerator codeGenerator)
		{
			ChainBuilder chain = codeGenerator.ChainBuilder;

			if (chain.IsArrayItem)
			{
				// 3: a_[x]_[v]
				Debug.Assert(this.isMemberOf == null);
				return chain.EmitEnsureItem(array, index, true);
			}

			// 1, 2, 4, 5, 6, 7
			if (chain.IsMember)
			{
				// 4, 5
				if (this.isMemberOf != null)
				{
					// 5: ...->a[]->...
					// Store isMemberOf for lazy emit
					chain.SetObjectForLazyEmit(this);
					chain.IsArrayItem = true;
					chain.IsLastMember = false;
				}
				else
				{
					// 4: a_[x]_->c->..., a[x]_[x]_->c->...
					chain.IsArrayItem = true;
					chain.IsLastMember = true;
				}

				PhpTypeCode result = chain.EmitEnsureItem(array, index, false);
				chain.IsArrayItem = false;
				return result;
			}

			// 1, 2, 6, 7
			if (this.isMemberOf != null)
			{
				// 6, 7: ...->a[x]_[x]_
				chain.Create();
				chain.Begin();
				// Store isMemberOf for lazy emit
				chain.SetObjectForLazyEmit(this);
				chain.IsArrayItem = true;
				chain.IsLastMember = false;
				chain.Lengthen(); // for own []
				array.Emit(codeGenerator);
				indexTypeCode = codeGenerator.EmitArrayKey(chain, index);
														
				// Note that EmitAssign will finish the work (SetArrayItem or PhpArray.Add)
				return PhpTypeCode.Unknown;
			}
			// 1, 2
			Debug.Assert(this.isMemberOf == null);

			if (array is ItemUse || array is DirectStFldUse || array is IndirectStFldUse /* ??? */)
			{
				// 2: a[]_[]_
				chain.Create();
				chain.Begin();
				chain.IsArrayItem = true;
				chain.IsLastMember = true;
				array.Emit(codeGenerator);
				indexTypeCode = codeGenerator.EmitArrayKey(chain, index);
							
							
				// Note that further work will be done by EmitAssign (SetArrayItem or PhpArray.Add)
				return PhpTypeCode.Unknown;
			}

			// 1: a_[x]_
			// Do nothing now, let the work be done in EmitAssign()
			return PhpTypeCode.Unknown;
		}

		/// <summary>
		/// Emits code to prepare an evaluation stack for storing a reference into a variable.
		/// Supports operators chaining. Store is finished by calling <see cref="EmitAssign"/>.
		/// </summary>
		/// <param name="codeGenerator"></param>
		private PhpTypeCode EmitNodeWriteRef(CodeGenerator codeGenerator)
		{
			ChainBuilder chain = codeGenerator.ChainBuilder;

			// Case 3: a_[x]_[x] never reached
			Debug.Assert(chain.IsArrayItem == false);

			// Case 4,5 never reached
			// 4: a[x]->...
			// 5: ...->a[]->...
			Debug.Assert(chain.IsMember == false);

			// 1, 2, 6, 7
			if (this.isMemberOf != null)
			{
				// 6, 7: ...->a[x]_[x]_
				chain.Create();
				chain.Begin();
				// Store isMemberOf for lazy emit
				chain.SetObjectForLazyEmit(this);
				chain.IsArrayItem = true;
				chain.IsLastMember = false;
				chain.Lengthen(); // for own []
				array.Emit(codeGenerator);
				indexTypeCode = codeGenerator.EmitArrayKey(chain, index);
							
				// Note that EmitAssign will finish the work (SetArrayItem or PhpArray.Add)
			}
			else
			{
				// 1, 2
				Debug.Assert(this.isMemberOf == null);

				if (array is ItemUse || array is DirectStFldUse || array is IndirectStFldUse /* ??? */)
				{
					// 2: a[]_[]_
					chain.Create();
					chain.Begin();
					chain.IsArrayItem = true;
					chain.IsLastMember = true;
					array.Emit(codeGenerator);
					indexTypeCode = codeGenerator.EmitArrayKey(chain, index);
							
					// Note that further work will be done by EmitAssign (SetArrayItem or PhpArray.Add)
				}
				// 1: a_[x]_
				// Do nothing now, let the work be done in EmitAssign()
				// Note further work will be done by EmitAssign (either SetItem or SetItemRef);	
			}
			return PhpTypeCode.Unknown;
		}

		internal override void DumpTo(AstVisitor/*!*/ visitor, TextWriter/*!*/ output)
		{
			if (isMemberOf != null)
			{
				isMemberOf.DumpTo(visitor, output);
				output.Write("->");
			}

			array.DumpTo(visitor, output);

			output.Write('[');
			if (index != null) index.DumpTo(visitor, output);
			output.Write(']');
			DumpAccess(output);
		}

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitItemUse(this);
        }
	}
}
