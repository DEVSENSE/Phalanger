/*

 Copyright (c) 2003-2006 Martin Maly, Tomas Matousek, Ladislav Prosek.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Diagnostics;
using System.Reflection.Emit;
using System.Reflection;
using System.Collections;

using PHP.Core;
using PHP.Core.AST;
using PHP.Core.Emit;
using PHP.Core.Reflection;

#if SILVERLIGHT
using PHP.CoreCLR;
#endif

namespace PHP.Core
{
	/// <summary>
	/// Provides means for building operator chains.
	/// </summary>
	internal sealed class ChainBuilder
	{
		private CodeGenerator codeGenerator;

		public ChainBuilder(CodeGenerator codeGenerator)
		{
			this.codeGenerator = codeGenerator;
		}

		#region Nested Class: Chain (an item of the stack)

		/// <summary>
		/// Represents a single chain being built. Those chains are items of the stack.
		/// </summary>
		private class Chain
		{
			/// <summary>
			/// Initializes a new empty instance of the <see cref="Chain"/>.
			/// </summary>
			/// <param name="il">An IL emitter.</param>
			public Chain(ILEmitter il)
			{
				isChainMember = false;
				isArrayItem = false;
				IsLastChainMember = false;
				length = 0;
				errorLabelHasValue = false;
				this.il = il;
				this.QuietRead = false;
			}

			/// <summary>
			/// Gets or sets a flag indicating whether the variable being actually emitted 
			/// is a member of operator's chain.
			/// </summary>
			public bool IsChainMember { get { return isChainMember; } set { isChainMember = value; } }
			private bool isChainMember;

			/// <summary>
			/// Gets or sets a flag indicating whether the variable being actually emitted 
			/// is an array (see <see cref="ItemUse"/>).
			/// </summary>
			public bool IsArrayItem { get { return isArrayItem; } set { isArrayItem = value; } }
			private bool isArrayItem;

			/// <summary>
			/// Gets or sets a flag indicating whether the the bottom most variable in AST 
			/// of actually emitted array (see <see cref="ItemUse"/>) is the last member of chain.
			/// </summary>
			public bool IsLastChainMember { get { return isLastChainMember; } set { isLastChainMember = value; } }
			private bool isLastChainMember;

			/// <summary>
			/// Gets or sets a flag indicating whether to force the FunctionCall to place the address of return value
			/// in the evaluation stack. The address is consumed by an operator. E.g.:
			/// <code>
			/// Chain: $z =&amp; $x->f()->y;
			/// AST:
			///                                             [RAE]
			///                                            /  |
			///                                         [DVU] |
			///                                               |
			/// [DVU] +- member of -- [DFC] +- member of -- [DVU]
			/// </code>
			/// Property set since <see cref="Operators.GetObjectPropertyRef"/> requires the parameter (DFC) to be an address.
			/// </summary>
			public bool LoadAddressOfFunctionReturnValue
			{
				get { return loadAddressOfFunctionReturnValue; }
				set { loadAddressOfFunctionReturnValue = value; }
			}
			private bool loadAddressOfFunctionReturnValue;

			/// <summary>
			/// A value that represents the number of -> and [] operators in the operators chain.
			/// </summary>
			private int length;

			/// <summary>
			/// Gets or sets the target <see cref="Label"/> where the control is transfered if an error
			/// occure during operators chain processing at runtime. Supports lazy intialization.
			/// </summary>
			public Label ErrorLabel           // GENERICS: Label? nullable type
			{
				get
				{
					if (!errorLabelHasValue)
					{
						errorLabel = il.DefineLabel();
						errorLabelHasValue = true;
					}
					return errorLabel;
				}
				set
				{
					errorLabel = value;
					errorLabelHasValue = true;
				}
			}
			private Label errorLabel;
			private bool errorLabelHasValue; // makes errorLabel a nullable structure

			/// <summary>
			/// IL emitter used to generate code for actual operators chain.
			/// </summary>
			private ILEmitter il;

			/// <summary>
			/// If set to <c>true</c>, no errors are genereted while emitting isset and unset.
			/// </summary>
			public bool QuietRead;

			// obsolete:
			//			private RefErrorLabelInfo refErrorLabelInfo;

			/// <summary>
			/// Increases the length of the operators chain by one.
			/// </summary>
			public void Lengthen()
			{
				length++;
			}

			/// <summary>
			/// Checks whether operators chain exists (its length is greater than one).
			/// </summary>
			public bool Exist
			{
				get { return length > 1; }
			}

			/// <summary>
			/// Begins a new operators chain. Handles the case when the chain is written or read.
			/// </summary>
			public void Begin()
			{
				isChainMember = true;
				isArrayItem = false;
				isLastChainMember = false;
				loadAddressOfFunctionReturnValue = false;
				// SHOULD BE 0 !
				length = 0;
			}

			// obsolete:
			//			/// <summary>
			//			/// Begins a new operators chain. Handles the case when the chain is read as a reference.
			//			/// </summary>
			//			public void BeginRef(RefErrorLabelInfo errorInfo)
			//			{
			//				this.Begin();
			//				this.ErrorLabel = errorInfo.ErrorLabel;
			//				this.refErrorLabelInfo = errorInfo;
			//			}

			/// <summary>
			/// Ends the actuall operators chain. Handles the case when the chain is read and written.
			/// </summary>
			public void End()
			{
				isChainMember = false;
				if (errorLabelHasValue)
				{
					il.MarkLabel(errorLabel, true);
					errorLabelHasValue = false;
				}
			}

			/// <summary>
			/// Ends the actuall operators chain. Handles the case when the chain is read as a reference.
			/// </summary>
			public void EndRef(/* TODO: ??? ref RefErrorLabelInfo errorInfo*/)
			{
				isChainMember = false;
				if (errorLabelHasValue)
				{
					Label temp = il.DefineLabel();
					il.Emit(OpCodes.Br, temp);
					il.MarkLabel(errorLabel, true);
					il.Emit(OpCodes.Newobj, Constructors.PhpReference_Void);
					il.MarkLabel(temp, true);
					errorLabelHasValue = false;
				}
			}
		}

		#endregion

		#region Chain Stack

		/// <summary>
		/// The stack of <see cref="Chain"/> elements which represent contexts for
		/// operator's chains code emmitting.
		/// </summary>
		private Stack stack = new Stack(); // GENERICS: Stack<Chain>

		/// <summary>
		/// Gets a top item of the <see cref="stack"/>.
		/// </summary>
		private Chain TopChain { get { return (Chain)stack.Peek(); } }

		public bool QuietRead
		{
			get { return TopChain.QuietRead; }
			set { TopChain.QuietRead = value; }
		}

		/// <summary>
		/// Gets a <B>bool</B> value indicating whether the variable being actually emitted 
		/// is a member of operator's chain.
		/// </summary>
		public bool IsMember
		{
			get { return TopChain.IsChainMember; }
		}

		/// <summary>
		/// Whether item of the chain is contained in the array subchain.
		/// <code>
		/// -- isMemberOf -- [IU] -- isMemberOf
		///                   /\
		///                [..] key 
		///                 
		/// </code>
		/// All nodes attached to the ItemUse node as children has this flag set to <B>true</B>.
		/// </summary>
		public bool IsArrayItem
		{
			get { return TopChain.IsArrayItem; }
			set { TopChain.IsArrayItem = value; }
		}

		/// <summary>
		/// Gets or sets a <B>bool</B> value indicating whether the the most bottom variable in AST 
		/// of actually emitted array (see <see cref="ItemUse"/>) is the last member of chain.
		/// </summary>
		public bool IsLastMember
		{
			get { return TopChain.IsLastChainMember; }
			set { TopChain.IsLastChainMember = value; }
		}

		/// <summary>
		/// Gets a <B>bool</B> value indicating whether operators chain exists (its length is greater than one).
		/// </summary>
		public bool Exists
		{
			get { return TopChain.Exist; }
		}

		/// <summary>
		/// Returns the error label of the current operator chain.
		/// </summary>
		public Label ErrorLabel
		{
			get { return TopChain.ErrorLabel; }
		}

		/// <summary>
		/// Gets or sets a flag indicating whether to force the FunctionCall to place the address of return value
		/// in the evaluation stack. 
		/// </summary>
		public bool LoadAddressOfFunctionReturnValue
		{
			get { return TopChain.LoadAddressOfFunctionReturnValue; }
			set { TopChain.LoadAddressOfFunctionReturnValue = value; }
		}

		/// <summary>
		/// Increases the length of the operators chain by one.
		/// </summary>
		public void Lengthen()
		{
			TopChain.Lengthen();
		}

		#endregion

		#region Create, Begin, End, EmitErrorCheck

		/// <summary>
		/// Creates a <see cref="Chain"/> item for a new operators chain and pushes it on the stack.
		/// </summary>
		public void Create()
		{
			stack.Push(new Chain(codeGenerator.IL));
		}

		/// <summary>
		/// Begins operators chainining.
		/// </summary>
		/// <remarks>
		/// This method should precede by a call to <see cref="Create"/> method.
		/// </remarks>
		public void Begin()
		{
			TopChain.Begin();
		}

		/// <summary>
		/// Ends current operator's chain and remove current chain context.
		/// </summary>
		public void End()
		{
			// terminate current chain
			TopChain.End();

			// remove the chain from chain stack
			stack.Pop();
		}

		/// <summary>
		/// Ends current operator's chain and remove current chain context. Handles the case when operator's chain
		/// is read as a reference.
		/// </summary>
		public void EndRef()
		{
			// Terminate current chain
			TopChain.EndRef(/*TODO: ref this.refErrorLabelInfo*/);

			// Remove the chain from chain stack
			stack.Pop();
		}

		/// <summary>
		/// Emits IL instructions to handle the error check after evaluating the chain operator returning 
		/// <see cref="PhpArray"/>.
		/// </summary>           
		/// <param name="isArray">
		/// Whether the operator's result is of type <see cref="PhpArray"/> (or <see cref="PhpObject"/>).
		/// </param>
		/// <remarks>
		/// The result value of chain operator method is expected on evaluation stack.
		/// The value is either left on the evaluation stack or the control is branched to error label.
		/// </remarks>
		private void EmitErrorCheck(bool isArray)
		{
			ILEmitter il = codeGenerator.IL;
			LocalBuilder temp = il.GetTemporaryLocal(isArray ? Types.PhpArray[0] : Types.DObject[0], true);

			il.Stloc(temp);
			il.Ldloc(temp);
			il.Emit(OpCodes.Brfalse, TopChain.ErrorLabel);
			il.Ldloc(temp);
		}

		#endregion

		#region Nested Class: ObjectFieldLazyEmitInfo

		/// <summary>
		/// <see cref="ObjectFieldLazyEmitInfo"/> class holds data controling lazy code generation of the
		/// <see cref="PHP.Core.AST.ItemUse"/><c>.IsMemberOf</c> object.
		/// </summary>
		/// <remarks>
		/// Lazy code generation takes place if <see cref="PHP.Core.AST.ItemUse"/><c>.IsMemberOf</c>
		/// is NOT equal to <B>null</B> which means that the most bottom node in <see cref="ItemUse.array"/>
		/// objects path should emit <see cref="PHP.Core.Operators.EnsurePropertyIsArray"/> operator call.
		/// </remarks>
		internal class ObjectFieldLazyEmitInfo
		{
			private VarLikeConstructUse objectForLazyEmit;
			public bool Old_IsArrayItem;
			public bool Old_IsLastMember;

			internal VarLikeConstructUse ObjectForLazyEmit
			{
				get
				{
					Debug.Assert(objectForLazyEmit != null);
					return objectForLazyEmit;
				}
				set
				{
					Debug.Assert(value != null);
					objectForLazyEmit = value;
				}
			}

			public ObjectFieldLazyEmitInfo(VarLikeConstructUse var_object, bool old_IsArrayItem, bool old_IsLastMember)
			{
				this.objectForLazyEmit = var_object;
				this.Old_IsArrayItem = old_IsArrayItem;
				this.Old_IsLastMember = old_IsLastMember;
			}

			public void Reset()
			{
				objectForLazyEmit = null;
			}
		}

		#endregion

		#region Nested Class: ObjectFieldLazyEmitInfoCache

		/// <summary>
		/// <see cref="ObjectFieldLazyEmitInfoCache"/> class provides the service for reusing <see cref="ObjectFieldLazyEmitInfo"/> objects once 
		/// instantialized.
		/// </summary>
		private class ObjectFieldLazyEmitInfoCache
		{
			private Stack stack;

			public ObjectFieldLazyEmitInfoCache()
			{
				stack = new Stack();
			}

			/// <summary>
			/// Gets the <see cref="ObjectFieldLazyEmitInfo"/> object stored in collection.
			/// </summary>
			/// <param name="var_object"></param>
			/// <param name="old_IsArrayItem"></param>
			/// <param name="old_IsLastMember"></param>
			/// <returns></returns>
			/// <remarks>
			/// This method should only be called from <see cref="ChainBuilder.GetObjectForLazyEmit"/> method.
			/// </remarks>
			public ObjectFieldLazyEmitInfo GetItem(VarLikeConstructUse var_object, bool old_IsArrayItem, bool old_IsLastMember)
			{
				if (stack.Count != 0)
				{
					ObjectFieldLazyEmitInfo info = (ObjectFieldLazyEmitInfo)stack.Pop();
					Debug.Assert(info != null);
					info.ObjectForLazyEmit = var_object;
					info.Old_IsArrayItem = old_IsArrayItem;
					info.Old_IsLastMember = old_IsLastMember;
				}
				return new ObjectFieldLazyEmitInfo(var_object, old_IsArrayItem, old_IsLastMember);
			}

			/// <summary>
			/// Sets the <see cref="ObjectFieldLazyEmitInfo"/> object to the collection.
			/// </summary>
			/// <param name="info"></param>
			/// <remarks>
			/// This method should only be called from <see cref="ChainBuilder.ReleaseObjectForLazyEmit"/> method.
			/// </remarks>
			public void SetItem(ObjectFieldLazyEmitInfo info)
			{
				Debug.Assert(stack != null);
				info.Reset();
				stack.Push(info);
			}
		}

		#endregion

		#region Object Field Lazy Emit

		private ObjectFieldLazyEmitInfoCache objectFieldLazyEmitInfoCache;
		private ObjectFieldLazyEmitInfo objectFieldLazyEmitInfo;

		internal void SetObjectForLazyEmit(VariableUse object_holder)
		{
			Debug.Assert(object_holder is ItemUse);
			if (objectFieldLazyEmitInfoCache == null) objectFieldLazyEmitInfoCache = new ObjectFieldLazyEmitInfoCache();
			objectFieldLazyEmitInfo = objectFieldLazyEmitInfoCache.GetItem(object_holder.IsMemberOf, this.IsArrayItem, this.IsLastMember);
		}

		internal ObjectFieldLazyEmitInfo GetObjectForLazyEmit()
		{
			Debug.Assert(objectFieldLazyEmitInfo != null);
			ObjectFieldLazyEmitInfo info = objectFieldLazyEmitInfo;
			objectFieldLazyEmitInfo = null;
			this.IsArrayItem = info.Old_IsArrayItem;
			this.IsLastMember = info.Old_IsLastMember;
			return info;
		}

		internal void ReleaseObjectForLazyEmit(ObjectFieldLazyEmitInfo info)
		{
			objectFieldLazyEmitInfoCache.SetItem(info);
		}

		#endregion


		#region Reading...

		/// <summary>
		/// Emits IL instructions that load a value of the specified property of an object.
		/// </summary>
		/// <param name="variable"><see cref="PHP.Core.AST.VariableUse"/> class determining the name of the field.</param>
		/// <remarks>Expects that the <see cref="PHP.Core.Reflection.DObject"/> whose property value should be obtained
		/// is loaded on the evaluation stack. The value of the property is left on the evaluation stack.</remarks>
		public PhpTypeCode EmitGetProperty(SimpleVarUse variable)
		{
			Debug.Assert(variable is DirectVarUse || variable is IndirectVarUse);

            var il = codeGenerator.IL;

            // we already have the instance value on top of the stack,
            // it must be stored in local variable first so we can call
            // call CallSite normally.
            
            // <memberOf> = <STACK:variable>:
            var memberOf = il.GetTemporaryLocal(Types.Object[0]);
            il.Stloc(memberOf);

            // create and call the CallSite:
            string fieldName = (variable is DirectVarUse) ? ((DirectVarUse)variable).VarName.Value : null;
            Expression fieldNameExpr = (variable is IndirectVarUse) ? ((IndirectVarUse)variable).VarNameEx : null;
            
            var result = codeGenerator.CallSitesBuilder.EmitGetProperty(codeGenerator, false,
                null, null, new IndexedPlace(memberOf),
                null,
                fieldName, fieldNameExpr,
                QuietRead);

            // return temporary variable:
            il.ReturnTemporaryLocal(memberOf);

            //
            return result;

            //// CALL object Operators.GetProperty(<STACK:variable>,<field name>,<type desc>);
            //variable.EmitName(codeGenerator);
            //codeGenerator.EmitLoadClassContext();
            //codeGenerator.IL.LoadBool(QuietRead);
            //codeGenerator.IL.Emit(OpCodes.Call, Methods.Operators.GetProperty);

			//return PhpTypeCode.Object;
		}

		/// <summary>
		/// Emits IL instructions that load the value of an item of given array.
		/// </summary>
		/// <param name="array"><see cref="Expression"/> determining the array.</param>
		/// <param name="index"><see cref="Expression"/> determining the index whose value 
		/// should be obtained from the array.</param>
		/// <param name="kind">A kind of getter.</param>
		/// <remarks>Nothing is supposed on the evaluation stack. The value of the item is left
		/// on the evaluation stack.</remarks>
		public PhpTypeCode EmitGetItem(Expression/*!*/ array, Expression/*!*/ index, Operators.GetItemKinds kind)
		{
			ILEmitter il = codeGenerator.IL;
			
			// array:
			array.Emit(codeGenerator);

			// index:
			PhpTypeCode index_type_code = codeGenerator.EmitArrayKey(this, index);

			// kind:
			if (kind == Operators.GetItemKinds.Get && QuietRead)
				kind = Operators.GetItemKinds.QuietGet;
			il.LdcI4((int)kind);

			// CALL Operators.GetItem(<array>, <index>, <kind>)
			codeGenerator.EmitGetItem(index_type_code, index, false);
			return PhpTypeCode.Object;
		}

		/// <summary>
		/// Emits IL instructions that loads the value of array's item as a <see cref="PHP.Core.PhpReference"/>.
		/// </summary>
		/// <param name="variable">A simple variable.</param>
		/// <param name="index"><see cref="Expression"/> determining the index.</param> 
		public PhpTypeCode EmitGetItemRef(SimpleVarUse/*!*/ variable, Expression index)
		{
			IsArrayItem = true;
			IsLastMember = true;
			
			PhpTypeCode index_type_code = PhpTypeCode.Invalid;

			// index:
			if (index != null)
				index_type_code = codeGenerator.EmitArrayKey(this, index);

			// array:
			variable.Emit(codeGenerator);

			// LOAD Operators.GetItemRef([<index>], ref <variable>)
			codeGenerator.EmitGetItem(index_type_code, index, true);
			
			// store the changed variable into table of variables (do nothing in optimalized functions)
			variable.EmitLoadAddress_StoreBack(codeGenerator);

			IsArrayItem = false;

			return PhpTypeCode.PhpReference;
		}

		#endregion

		#region Writing...

		#region Single node (no chains)

		/// <summary>
		/// Emits IL instructions that sets the value to an <see cref="PHP.Core.PhpArray"/> item.
		/// </summary>
		/// <remarks>This method is used to set the item of an array having the index determined (not <B>null</B>).
		/// This method is called only in simple cases when operators chained are <b>not</b> involved.
		/// See short example of PHP code below.
		/// Expects that the reference to the object whose item should be set, the index and the value
		/// are loaded on the evaluation stack. Nothing is left on the evaluation stack.
		/// stack. 
		/// </remarks>
		/// <example>$a[3] = "foo"; $a[] = "foo";</example>
		/// <example>$x[y] =&amp; p; $x[] =&amp; p;</example>
		public void EmitSetItem(PhpTypeCode keyTypeCode, Expression index, bool reference)
		{
			codeGenerator.EmitSetItem(keyTypeCode, index, reference);
		}

		#endregion

		#region First operator (EnsureVariableIsArray, EnsureVariableIsObject, EnsureStaticPropertyIsArray, EnsureStaticPropertyIsObject)

		/// <summary>
		/// Emits IL instructions that ensure that the specified variable is an instance of <see cref="PhpArray"/>. 
		/// </summary>
		/// <param name="variable">Variable's name-index to a table of identifiers.</param>
		/// <remarks>
		/// This method is used in operators chains. Nothing is expected on the evaluation stack. 
		/// If the specified variable is an instance of <see cref="PhpArray"/>
		/// it is left on the evaluation stack. Otherwise the control is transfered to the end of 
		/// the chain.
		/// </remarks>
		public void EmitEnsureVariableIsArray(SimpleVarUse variable)
		{
			// Template: PhpArray     EnsureVariableIsArray(ref object)

			// Load variable's address
			//this.EmitVariableLoadAddress(variable);
			variable.EmitLoadAddress(codeGenerator);
			codeGenerator.IL.Emit(OpCodes.Call, Methods.Operators.EnsureVariableIsArray);
			// Store the changed destVar into table of variables (do nothing in optimalized functions)
			variable.EmitLoadAddress_StoreBack(codeGenerator);
			EmitErrorCheck(true);
		}

		/// <summary>
		/// Emits IL instructions that ensure that the specified variable is an instance of <see cref="PhpObject"/>. 
		/// </summary>
		/// <param name="variable">Variable that should be examined.</param>
		/// <remarks>
		/// This method is used in operators chains. Nothing is expected on the evaluation stack. 
		/// If the specified variable is an instance of <see cref="PhpObject"/>
		/// it is left on the evaluation stack. Otherwise the control is transfered to the end of 
		/// the chain.
		/// </remarks>
		public void EmitEnsureVariableIsObject(SimpleVarUse variable)
		{
			ILEmitter il = codeGenerator.IL;

			DirectVarUse direct_variable = variable as DirectVarUse;
			if (direct_variable != null && direct_variable.VarName.IsThisVariableName)
			{
				// special treatment of $this
				switch (codeGenerator.LocationStack.LocationType)
				{
					case LocationTypes.GlobalCode:
						{
							// load $this from one of Main's arguments and check for null
							Label this_non_null = il.DefineLabel();

							codeGenerator.EmitLoadSelf();
							il.Emit(OpCodes.Brtrue_S, this_non_null);

							codeGenerator.EmitPhpException(Methods.PhpException.ThisUsedOutOfObjectContext);

							il.Emit(OpCodes.Br, TopChain.ErrorLabel);
							il.MarkLabel(this_non_null, true);
							codeGenerator.EmitLoadSelf();
							break;
						}

					case LocationTypes.FunctionDecl:
						{
							// always throws error
							codeGenerator.EmitPhpException(Methods.PhpException.ThisUsedOutOfObjectContext);
							il.Emit(OpCodes.Br, TopChain.ErrorLabel);
							break;
						}

					case LocationTypes.MethodDecl:
						{
							CompilerLocationStack.MethodDeclContext context = codeGenerator.LocationStack.PeekMethodDecl();
							if (context.Method.IsStatic)
							{
								// always throws error
								codeGenerator.EmitPhpException(Methods.PhpException.ThisUsedOutOfObjectContext);
								il.Emit(OpCodes.Br, TopChain.ErrorLabel);
							}
							else
							{
								// arg0 or <proxy> in instance methods
								codeGenerator.EmitLoadSelf();
							}
							break;
						}

					default:
						Debug.Assert(false, "Invalid location type.");
						break;
				}
			}
			else
			{
				// Template: PhpObject    EnsureVariableIsObject(ref object,ScriptContext)

				// Load variable's address
				//				if (variable is FunctionCall)
				//				{
				//					variable.Emit(this);
				//					EmitLoadScriptContext();
				//					il.Emit(OpCodes.Call, Methods.Operators.EnsureVariableIsObject);
				//					// Store the changed destVar into table of variables (do nothing in optimalized functions)
				//				}
				//				else
				//				{
				variable.EmitLoadAddress(codeGenerator);
				codeGenerator.EmitLoadScriptContext();
				il.Emit(OpCodes.Call, Methods.Operators.EnsureVariableIsObject);
				// Store the changed destVar into table of variables (do nothing in optimalized functions)
				variable.EmitLoadAddress_StoreBack(codeGenerator);
				//				}
				EmitErrorCheck(false);
			}
		}

		/// <summary>
		/// Emits IL instructions that ensure that the variable on the top of the evaluation stack is an instance of <see cref="PhpObject"/>. 
		/// </summary>
		/// <remarks>
		/// This method is used in operators chains to ensure that the variable placed on the top of the evaluation stack by 
		/// a function call is of type PhpObject. The address ot the variable to check is expected on the evaluation stack. 
		/// If the specified variable is an instance of <see cref="PhpObject"/>
		/// it is left on the evaluation stack. Otherwise the control is transfered to the end of 
		/// the chain.
		/// </remarks>
		public void EmitEnsureVariableIsObject()
		{
			codeGenerator.EmitLoadScriptContext();
			codeGenerator.IL.Emit(OpCodes.Call, Methods.Operators.EnsureVariableIsObject);
			// Store the changed destVar into table of variables (do nothing in optimalized functions)
			EmitErrorCheck(false);
		}

		/// <summary>
		/// Emits IL instructions that ensure that a static field is of <see cref="PhpObject"/> or <see cref="PhpArray"/>
		/// type. Handles the case when field name is unknown at compile time (see <see cref="AST.IndirectStFldUse"/>).
		/// </summary>
        /// <param name="typeRef">The class name (identifier index).</param>
		/// <param name="propertyName">The property name.</param>
		/// <param name="propertyNameExpr">The expression that evaluates to property name.</param>
		/// <param name="ensureArray">Whether to ensure that static field is an array (or an object).</param>
		/// <remarks>
		/// Nothing is expected on the evaluation stack. A <see cref="PhpArray"/> or <see cref="DObject"/> is left on the
		/// evaluation stack.
		/// </remarks>
		public PhpTypeCode EmitEnsureStaticProperty(TypeRef typeRef, VariableName? propertyName,
			Expression propertyNameExpr, bool ensureArray)
		{
			Debug.Assert(propertyName != null ^ propertyNameExpr != null);

			ResolveTypeFlags flags = ResolveTypeFlags.UseAutoload | ResolveTypeFlags.ThrowErrors;

			// LOAD Operators.EnsureStaticFieldIs[Object|Array](<type desc>, <field name>, <type desc>, <context>)
            typeRef.EmitLoadTypeDesc(codeGenerator, flags);

			if (propertyNameExpr != null)
				codeGenerator.EmitBoxing(propertyNameExpr.Emit(codeGenerator));
			else
				codeGenerator.IL.Emit(OpCodes.Ldstr, propertyName.Value.ToString());

			codeGenerator.EmitLoadClassContext();
			codeGenerator.EmitLoadScriptContext();

			if (ensureArray)
				codeGenerator.IL.Emit(OpCodes.Call, Methods.Operators.EnsureStaticPropertyIsArray);
			else
				codeGenerator.IL.Emit(OpCodes.Call, Methods.Operators.EnsureStaticPropertyIsObject);

			EmitErrorCheck(ensureArray);
			return (ensureArray) ? PhpTypeCode.PhpArray : PhpTypeCode.DObject;
		}

		/// <summary>
		/// Emits IL instructions that ensure that a static field is of <see cref="PhpObject"/> or <see cref="PhpArray"/>
		/// type. Handles the case when field name is known at compile time (see <see cref="AST.DirectStFldUse"/>).
		/// </summary>
		/// <param name="property">The corresponding <see cref="DProperty"/> or <B>null</B>.</param>
        /// <param name="typeRef">The class type reference (identifier index).</param>
		/// <param name="fieldName">The field name (identifier index).</param>
		/// <param name="ensureArray">Whether to ensure that static field is an array (or an object).</param>
		/// <remarks>
		/// Nothing is expected on the evaluation stack. A <see cref="PhpObject"/> or <see cref="DObject"/> is left
		/// on the evaluation stack or the last emitted instruction is unconditional branch to <see cref="Chain.ErrorLabel"/>.
		/// </remarks>
		public PhpTypeCode EmitEnsureStaticProperty(DProperty property, TypeRef typeRef,
			VariableName fieldName, bool ensureArray)
		{
			ILEmitter il = codeGenerator.IL;

			PhpField php_field = property as PhpField;
			if (php_field != null)
			{
				// HACK HACK
				EmitEnsureStaticPhpFieldDirect(php_field, ensureArray);

				EmitErrorCheck(ensureArray);
				return (ensureArray) ? PhpTypeCode.PhpArray : PhpTypeCode.DObject;
			}
            else return EmitEnsureStaticProperty(typeRef, fieldName, null, ensureArray);
		}

		/// <summary>
		/// I do not like PHP-specific access code emission here. TODO: Move to PhpField
		/// </summary>
		/// <param name="phpField"></param>
		/// <param name="ensureArray"></param>
		void EmitEnsureStaticPhpFieldDirect(PhpField phpField, bool ensureArray)
		{
			ILEmitter il = codeGenerator.IL;
			MethodInfo static_init_info = ((PhpType)phpField.Implementor).StaticFieldInitMethodInfo;

			// ensure that the field has been initialized for this request by invoking __InitializeStaticFields
			if (static_init_info != null && !il.IsFeatureControlFlowPrecedent(phpField.Implementor))
			{
				codeGenerator.EmitLoadScriptContext();
				il.Emit(OpCodes.Call, static_init_info);

				// remember that we have just initialized class_entry's static fields
				il.MarkFeature(phpField.Implementor);
			}

			// LOAD EnsureVariableIs[Array|Object](ref <field>.value,[<context>]);
			il.Emit(OpCodes.Ldsfld, phpField.RealField);
			il.Emit(OpCodes.Ldflda, Fields.PhpReference_Value);

			if (ensureArray)
			{
				il.Emit(OpCodes.Call, Methods.Operators.EnsureVariableIsArray);
			}
			else
			{
				codeGenerator.EmitLoadScriptContext();
				il.Emit(OpCodes.Call, Methods.Operators.EnsureVariableIsObject);
			}
		}

		#endregion

		#region Middle operators (EnsureItemIsArray, EnsureItemIsObject, EnsurePropertyIsArray, EnsurePropertyIsObject)

		/// <summary>
		/// Emits IL instructions that ensure that the specified item of specified array is of
		/// type <see cref="PhpArray"/>.
		/// </summary>
		/// <param name="array">Array which item is examined.</param>
		/// <param name="index">Index determining the item which should be examined (can be <B>null</B>).</param>
		/// <param name="ensureArray">Whether to ensure that static field is an array (or an object).</param>
		/// <remarks>
		/// This method is used in operators chains. Nothing is expected on the evaluation stack. 
		/// If the item is of type <see cref="PhpArray"/> and <see cref="PhpObject"/> (respectively) 
		/// it is left on the evaluation stack. Otherwise the control is transfered to the end of chain.
		/// </remarks>
		public PhpTypeCode EmitEnsureItem(Expression/*!*/ array, Expression index, bool ensureArray)
		{
			if (!ensureArray) Lengthen();

			array.Emit(codeGenerator);

			if (index != null)
			{
				// keyed item:
				Create();
				codeGenerator.EmitBoxing(index.Emit(codeGenerator));
				End();

				if (ensureArray)
				{
					codeGenerator.IL.Emit(OpCodes.Callvirt, Methods.PhpArray.EnsureItemIsArray_Object);
				}
				else
				{
					codeGenerator.EmitLoadScriptContext();
					codeGenerator.IL.Emit(OpCodes.Callvirt, Methods.PhpArray.EnsureItemIsObject_Object);
				}
			}
			else
			{
				// key-less item:
				if (ensureArray)
				{
					codeGenerator.IL.Emit(OpCodes.Callvirt, Methods.PhpArray.EnsureItemIsArray);
				}
				else
				{
					codeGenerator.EmitLoadScriptContext();
					codeGenerator.IL.Emit(OpCodes.Callvirt, Methods.PhpArray.EnsureItemIsObject);
				}
			}
			EmitErrorCheck(ensureArray);
			return (ensureArray) ? PhpTypeCode.PhpArray : PhpTypeCode.DObject;
		}

		/// <summary>
		/// Emits IL instructions that ensure that the specified property of an object is
		/// of the <see cref="PhpArray"/> type.
		/// </summary>
		/// <param name="varObject">Represents the instance whose property should be examined.</param>
		/// <param name="fieldName">A <see cref="SimpleVarUse"/> that evaluates to the property name.</param>
		/// <param name="ensureArray">Whether to ensure that static property is an array (or an object).</param>
		/// <remarks>Nothing is expected on the evaluation stack. If the property is of <see cref="PhpArray"/> type
		/// it is left on the evaluation stack. Otherwise the control is transfered to the end of chain.</remarks>
		public PhpTypeCode EmitEnsureProperty(VarLikeConstructUse/*!*/ varObject, SimpleVarUse/*!*/ fieldName, bool ensureArray)
		{
			// Template: PhpArray EnsurePropertyIsArray(DObject,field,DTypeDesc)
			Debug.Assert(varObject != null && fieldName != null);
			Debug.Assert(fieldName is DirectVarUse || fieldName is IndirectVarUse);

			LocationTypes location;
			DProperty property = ResolveProperty(varObject, fieldName, out location);

			ILEmitter il = codeGenerator.IL;

			PhpField php_field = property as PhpField;
			if (php_field != null) // we can emit code that manipulates the property directly
			{
				// HACK HACK
				EmitEnsurePhpFieldDirect(php_field, fieldName, ensureArray);
			}
			else
			{
				switch (location)
				{
					case LocationTypes.GlobalCode:
						{
							// call EnsurePropertyIsArray
							codeGenerator.EmitLoadSelf();
							fieldName.EmitName(codeGenerator);
							codeGenerator.EmitLoadClassContext();

							if (ensureArray)
							{
								il.Emit(OpCodes.Call, Methods.Operators.EnsurePropertyIsArray);
							}
							else
							{
								codeGenerator.EmitLoadScriptContext();
								il.Emit(OpCodes.Call, Methods.Operators.EnsurePropertyIsObject);
							}
							break;
						}

					case LocationTypes.MethodDecl:
						{
							if (ensureArray) this.Lengthen(); // for hop over ->
							FunctionCall func = varObject as FunctionCall;
							if (func == null)
							{
								varObject.Emit(codeGenerator);
							}
							else
							{
								this.LoadAddressOfFunctionReturnValue = true;
								func.Emit(codeGenerator);
								RecastValueReturnedByFunctionCall();
							}
							fieldName.EmitName(codeGenerator);
							codeGenerator.EmitLoadClassContext();

							if (ensureArray)
							{
								il.Emit(OpCodes.Call, Methods.Operators.EnsurePropertyIsArray);
							}
							else
							{
								codeGenerator.EmitLoadScriptContext();
								il.Emit(OpCodes.Call, Methods.Operators.EnsurePropertyIsObject);
							}
							EmitErrorCheck(ensureArray);
							break;
						}

					// if the location was FunctionDecl, appropriate code was already generated by GetDProperty
				}
			}
			return (ensureArray ? PhpTypeCode.PhpArray : PhpTypeCode.DObject);
		}

		/// <summary>
		/// I do not like PHP-specific access code emission here. TODO: Move to PhpField
		/// </summary>
		/// <param name="field"></param>
		/// <param name="fieldName"></param>
		/// <param name="ensureArray"></param>
		private void EmitEnsurePhpFieldDirect(PhpField/*!*/ field, SimpleVarUse/*!*/ fieldName, bool ensureArray)
		{
			ILEmitter il = codeGenerator.IL;

			// check whether the field is set
			il.Ldarg(FunctionBuilder.ArgThis);
			il.Emit(OpCodes.Ldfld, field.RealField);

			Label direct_ensure = il.DefineLabel();
			Label ensuring_over = il.DefineLabel();

			// test whether it is set
			il.Emit(OpCodes.Callvirt, Properties.PhpReference_IsSet.GetGetMethod());
			il.Emit(OpCodes.Brtrue, direct_ensure);

			// the field has been unset -> must call operator that handles __get/__set
			if (ensureArray) this.Lengthen();  // TODO: ???
			codeGenerator.EmitLoadSelf();
			fieldName.EmitName(codeGenerator);
			codeGenerator.EmitLoadClassContext();
			if (ensureArray)
			{
				il.Emit(OpCodes.Call, Methods.Operators.EnsurePropertyIsArray);
			}
			else
			{
				codeGenerator.EmitLoadScriptContext();
				il.Emit(OpCodes.Call, Methods.Operators.EnsurePropertyIsObject);
			}
			il.Emit(OpCodes.Br, ensuring_over);

			// read the field again and call EnsureVariableIsArray
			il.MarkLabel(direct_ensure, true);
			il.Ldarg(FunctionBuilder.ArgThis);
			il.Emit(OpCodes.Ldfld, field.RealField);
			il.Emit(OpCodes.Ldflda, Fields.PhpReference_Value);

			if (ensureArray)
			{
				il.Emit(OpCodes.Call, Methods.Operators.EnsureVariableIsArray);
			}
			else
			{
				codeGenerator.EmitLoadScriptContext();
				il.Emit(OpCodes.Call, Methods.Operators.EnsureVariableIsObject);
			}

			il.MarkLabel(ensuring_over, true);
			EmitErrorCheck(ensureArray);
		}

		/// <summary>
		/// Tries to find an instance of <see cref="DProperty"/> that corresponds to an instance property given by
		/// <paramref name="varObject"/> and <paramref name="fieldName"/>. Currently it is possible only if
		/// <paramref name="varObject"/> represents <B>$this</B> and <paramref name="fieldName"/> is a compile time
		/// known instance property, which is surely accessible from current location.
		/// </summary>
		/// <param name="varObject">Represents the left side of <B>-&gt;</B>.</param>
		/// <param name="fieldName">Represents the right side of <B>-&gt;</B>.</param>
		/// <param name="location">Current location, valid only if the return value is <B>null</B>. Used by the caller to
		/// decide what kind of run time access should be emitted.</param>
		/// <returns>A valid non-<B>null</B> <see cref="PhpField"/> if the field was found, <B>null</B> otherwise.</returns>
		internal DProperty ResolveProperty(VarLikeConstructUse varObject, SimpleVarUse fieldName, out LocationTypes location)
		{
			DirectVarUse direct_var = varObject as DirectVarUse;
			DirectVarUse direct_field_name;

			if (direct_var != null && (direct_field_name = fieldName as DirectVarUse) != null &&
				direct_var.IsMemberOf == null && direct_var.VarName.IsThisVariableName)
			{
				ILEmitter il = codeGenerator.IL;
				location = codeGenerator.LocationStack.LocationType;
				switch (location)
				{
					case LocationTypes.GlobalCode:
						{
							// load $this from one of Main's arguments and check for null
							Label this_non_null = il.DefineLabel();

							codeGenerator.EmitLoadSelf();
							il.Emit(OpCodes.Brtrue_S, this_non_null);
							codeGenerator.EmitPhpException(Methods.PhpException.ThisUsedOutOfObjectContext);
							il.Emit(OpCodes.Br, TopChain.ErrorLabel);
							il.MarkLabel(this_non_null, true);

							return null;
						}

					case LocationTypes.FunctionDecl:
						{
							// always throws error
							codeGenerator.EmitPhpException(Methods.PhpException.ThisUsedOutOfObjectContext);
							il.Emit(OpCodes.Br, TopChain.ErrorLabel);

							return null;
						}

					case LocationTypes.MethodDecl:
						{
							CompilerLocationStack.MethodDeclContext context = codeGenerator.LocationStack.PeekMethodDecl();
							if (context.Method.IsStatic)
							{
								// always throws error
								codeGenerator.EmitPhpException(Methods.PhpException.ThisUsedOutOfObjectContext);
								il.Emit(OpCodes.Br, TopChain.ErrorLabel);

								location = LocationTypes.FunctionDecl;
								return null;
							}
							else
							{
								DProperty property;
								if (context.Type.GetProperty(direct_field_name.VarName, context.Type, out property)
									== GetMemberResult.OK && !property.IsStatic)
								{
									return property;
								}
							}
							break;
						}
				}
			}

			location = LocationTypes.MethodDecl;
			return null;
		}

		internal void RecastValueReturnedByFunctionCall()
		{
			this.EmitEnsureVariableIsObject();
		}

		#endregion

		#region Last operator (GetArrayItem, GetArrayItemRef, GetObjectProperty, GetObjectPropertyRef, SetArrayItem, SetObjectProperty, a function/method call)

		public void EmitGetArrayItemRef(Expression/*!*/ array, Expression index)
		{
			array.Emit(codeGenerator);

			PhpTypeCode index_type_code = PhpTypeCode.Invalid;

			if (index != null)
				index_type_code = codeGenerator.EmitArrayKey(this, index);

			codeGenerator.EmitGetArrayItem(index_type_code, index, true);
		}

		public void EmitSetArrayItem(PhpTypeCode keyTypeCode, Expression index, bool reference)
		{
			codeGenerator.EmitSetArrayItem(keyTypeCode, index, reference, false);
		}

		public void EmitSetObjectField()
		{
			// CALL Operators.SetObjectProperty(STACK:obj,STACK:name,STACK:value,<type desc>);
			codeGenerator.EmitLoadClassContext();
			codeGenerator.IL.Emit(OpCodes.Call, Methods.Operators.SetObjectProperty);

            //always when function with void return argument is called it's necesarry to add nop instruction due to debugger
            if (codeGenerator.Context.Config.Compiler.Debug)
            {
                codeGenerator.IL.Emit(OpCodes.Nop);
            }
		}

		#endregion

		#endregion

		#region Runtime Chains

		/// <summary>
		/// Emits IL instructions that create a new empty <see cref="PhpRuntimeChain"/>.
		/// </summary>
		/// <remarks>
		/// Nothing is expected on the evaluation stack, a reference to <see cref="PhpRuntimeChain"/>
		/// is left on the evaluation stack.
		/// </remarks>
		public void EmitCreateRTChain()
		{
			codeGenerator.EmitLoadClassContext();
			codeGenerator.IL.Emit(OpCodes.Newobj, Constructors.PhpRuntimeChain_Object_DTypeDesc);
		}

		/// <summary>
		/// Emits IL instructions that add an object field access to the current <see cref="PhpRuntimeChain"/>.
		/// </summary>
		/// <param name="varUse">AST node representing the field access.</param>
		/// <remarks>
		/// A reference to <see cref="PhpRuntimeChain"/> is expected and left on the evaluation stack.
		/// </remarks>
		public void EmitRTChainAddField(SimpleVarUse varUse)
		{
			codeGenerator.IL.Emit(OpCodes.Dup);
			varUse.EmitName(codeGenerator);
			codeGenerator.IL.EmitCall(OpCodes.Call, Methods.PhpRuntimeChain.AddField, null);
		}

		/// <summary>
		/// Emits IL instructions that add an array item access to the current <see cref="PhpRuntimeChain"/>.
		/// </summary>
		/// <param name="itemUse">AST node representing the item access.</param>
		/// <remarks>
		/// A reference to <see cref="PhpRuntimeChain"/> is expected and left on the evaluation stack.
		/// </remarks>
		public void EmitRTChainAddItem(ItemUse itemUse)
		{
			PhpTypeCode res = itemUse.Array.Emit(codeGenerator);

			// create the runtime chain if it has not been done so
			if (res != PhpTypeCode.PhpRuntimeChain)
			{
				codeGenerator.EmitBoxing(res);
				EmitCreateRTChain();
			}

			codeGenerator.IL.Emit(OpCodes.Dup);

			if (itemUse.Index != null)
			{
				// [x]
				Create();
				codeGenerator.EmitBoxing(itemUse.Index.Emit(codeGenerator));
				End();
				codeGenerator.IL.EmitCall(OpCodes.Call, Methods.PhpRuntimeChain.AddItem_Object, null);
			}
			else
			{
				// []
				codeGenerator.IL.EmitCall(OpCodes.Call, Methods.PhpRuntimeChain.AddItem_Void, null);
			}
		}

		#endregion

		#region Unset

		public void EmitUnsetItem(Expression array, Expression index)
		{
			// Template:
			//		void UnsetItem(object var,object index)

			array.Emit(codeGenerator);

			Debug.Assert(index != null);

			Create();
			codeGenerator.EmitBoxing(index.Emit(codeGenerator));
			End();

			codeGenerator.IL.Emit(OpCodes.Call, Methods.Operators.UnsetItem);
		}

		#endregion
	}
}
