/*

 Copyright (c) 2004-2006 Tomas Matousek, Ladislav Prosek, Vaclav Novak and Martin Maly.

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
	#region VariableUse

	/// <summary>
	/// Base class for variable uses.
	/// </summary>
	public abstract class VariableUse : VarLikeConstructUse
	{
		protected VariableUse(Position p) : base(p) { }

		internal abstract PhpTypeCode EmitAssign(CodeGenerator codeGenerator);
		internal abstract PhpTypeCode EmitIsset(CodeGenerator codeGenerator, bool empty);
		internal abstract void EmitUnset(CodeGenerator codeGenerator);
	}

	#endregion

	// possible access values for all VariableUse subclasses: 
	// Read, Write, ReadRef, ReadUnknown, WriteRef, None

	#region CompoundVarUse

	/// <summary>
	/// Base class for compound variable uses.
	/// </summary>
	public abstract class CompoundVarUse : VariableUse
	{
		protected CompoundVarUse(Position p) : base(p) { }
	}

	#endregion

	#region SimpleVarUse

	/// <summary>
	/// Base class for simple variable uses.
	/// </summary>
	public abstract class SimpleVarUse : CompoundVarUse
	{
		/// <summary>
		/// Points to a method that emits code to be placed after the new instance field value has
		/// been loaded onto the evaluation stack.
		/// </summary>
		internal AssignmentCallback assignmentCallback;

		protected SimpleVarUse(Position p) : base(p) { this.TabledLocalAddressStorage = null; }

		/// <summary>
		/// A holder of a temporary local variable which is used to obtain address of a variable
		/// stored in runtime variables table when methods from <see cref="PHP.Core.Operators"/>
		/// expecting a ref argument are invoked.
		/// </summary>
		/// <remarks>After the operator is invoked, the result should be stored back to table and this
		/// holder may be released. This holder does not take place in optimalized user functions and methods of
		/// user classes as the address of a variable shoul be obtained directly (variables are defined as
		/// locals).</remarks>
		protected LocalBuilder TabledLocalAddressStorage;

		internal abstract void EmitName(CodeGenerator codeGenerator);
		internal abstract PhpTypeCode EmitLoad(CodeGenerator codeGenerator);
		internal abstract void EmitLoadAddress(CodeGenerator codeGenerator);
		internal abstract void EmitLoadAddress_StoreBack(CodeGenerator codeGenerator);
		internal abstract void EmitLoadAddress_StoreBack(CodeGenerator codeGenerator, bool duplicate_value);
		internal abstract void EmitLoadRef(CodeGenerator codeGenerator);
		internal abstract void EmitStorePrepare(CodeGenerator codeGenerator);
		internal abstract void EmitStoreAssign(CodeGenerator codeGenerator);
		internal abstract void EmitStoreRefPrepare(CodeGenerator codeGenerator);
		internal abstract void EmitStoreRefAssign(CodeGenerator codeGenerator);

		/// <summary>
		/// Loads the value represented by this object from the runtime variables table,
		/// stores it to a local variable and loads the address of this local.
		/// </summary>
		/// <remarks>This method is used only in non-optimized user functions and global code.
		/// Specified local variable is obtained from current <see cref="ILEmitter"/> by
		/// <see cref="ILEmitter.GetTemporaryLocal"/> and stored to <see cref="TabledLocalAddressStorage"/>
		/// for later use. Once the local become useless, <see cref="ILEmitter.ReturnTemporaryLocal"/>
		/// should be called.
		/// </remarks>
		/// <param name="codeGenerator">Currently used <see cref="CodeGenerator"/>.</param>
		internal virtual void LoadTabledVariableAddress(CodeGenerator codeGenerator)
		{
			// This function should be call only once on every SimpleVarUse object
			// TODO: ASSERTION FAILS (e.g. PhpMyAdmin, common.lib.php)
			// Debug.Assert(this.TabledLocalAddressStorage == null);
			ILEmitter il = codeGenerator.IL;

			// Load the value represented by this node from the runtime variables table

			// LOAD Operators.GetVariableUnchecked(<script context>, <local variables table>, <variable name>);
			codeGenerator.EmitLoadScriptContext();
			codeGenerator.EmitLoadRTVariablesTable();
			EmitName(codeGenerator);
			codeGenerator.IL.Emit(OpCodes.Call, Methods.Operators.GetVariableUnchecked);

			// Get local from ILEmitter
			this.TabledLocalAddressStorage = il.GetTemporaryLocal(Types.Object[0]);
			// Store the value
			il.Stloc(this.TabledLocalAddressStorage);
			// Load the address
			il.Ldloca(this.TabledLocalAddressStorage);
		}

		/// <summary>
		/// Stores the value represented by <see cref="TabledLocalAddressStorage"/> to the runtime variables table and 
		/// returns the <see cref="TabledLocalAddressStorage"/> back to <see cref="ILEmitter.temporaryLocals"/>.
		/// Duplicates the value if requested.
		/// </summary>
		/// <param name="codeGenerator">Currently used <see cref="CodeGenerator"/>.</param>
		/// <param name="duplicate_value">If <c>true</c>, the value of specified local is left on the evaluation stack.
		/// </param>
		internal virtual void StoreTabledVariableBack(CodeGenerator codeGenerator, bool duplicate_value)
		{
			ILEmitter il = codeGenerator.IL;

			// CALL Operators.SetVariable(<local variables table>,<name>,<TabledLocalAddressStorage>);
			codeGenerator.EmitLoadScriptContext();
			codeGenerator.EmitLoadRTVariablesTable();
			EmitName(codeGenerator);
			il.Ldloc(TabledLocalAddressStorage);
			il.Emit(OpCodes.Call, Methods.Operators.SetVariable);

			// If requested, load the changed value on the evaluation stack
			if (duplicate_value)
				il.Ldloc(this.TabledLocalAddressStorage);

			// Release temporary local
			il.ReturnTemporaryLocal(this.TabledLocalAddressStorage);
		}


		/// <summary>
		/// Emits IL instructions that read the value of an instance field.
		/// </summary>
		/// <param name="codeGenerator">The current <see cref="CodeGenerator"/>.</param>
		/// <param name="wantRef">If <B>false</B> the field value should be left on the evaluation stack,
		/// if <B>true</B> the <see cref="PhpReference"/> should be left on the evaluation stack.</param>
		/// <returns>
		/// Nothing is expected on the evaluation stack. A <see cref="PhpReference"/> (if <paramref name="wantRef"/>
		/// is <B>true</B>) or the field value itself (if <paramref name="wantRef"/> is <B>false</B>) is left on the
		/// evaluation stack.
		/// </returns>
		internal virtual PhpTypeCode EmitReadField(CodeGenerator codeGenerator, bool wantRef)
		{
			ILEmitter il = codeGenerator.IL;

			DirectVarUse direct_instance = isMemberOf as DirectVarUse;
			if (direct_instance != null && direct_instance.IsMemberOf == null && direct_instance.VarName.IsThisVariableName)
			{
				return EmitReadFieldOfThis(codeGenerator, wantRef);
			}
            		

			if (!wantRef)
			{
                //codeGenerator.ChainBuilder.Lengthen();
                //PhpTypeCode type_code = isMemberOf.Emit(codeGenerator);
                //Debug.Assert(type_code == PhpTypeCode.Object || type_code == PhpTypeCode.DObject);

                //// CALL Operators.GetProperty(STACK,<field name>,<type desc>,<quiet>);
                //EmitName(codeGenerator);
                //codeGenerator.EmitLoadClassContext();
                //il.LoadBool(codeGenerator.ChainBuilder.QuietRead);
                //il.Emit(OpCodes.Call, Methods.Operators.GetProperty);
                //return PhpTypeCode.Object;

                string fieldName = (this is DirectVarUse) ? ((DirectVarUse)this).VarName.Value : null;
                Expression fieldNameExpr = (this is IndirectVarUse) ? ((IndirectVarUse)this).VarNameEx : null;
                bool quietRead = wantRef ? false : codeGenerator.ChainBuilder.QuietRead;
                return codeGenerator.CallSitesBuilder.EmitGetProperty(
                    codeGenerator, wantRef,
                    isMemberOf, null, null,
                    null,
                    fieldName, fieldNameExpr,
                    quietRead);
			}

            // call GetProperty/GetObjectPropertyRef
			codeGenerator.ChainBuilder.Lengthen();
            // loads the variable which field is gotten:
            PhpTypeCode var_type_code = isMemberOf.Emit(codeGenerator);

			if (codeGenerator.ChainBuilder.Exists)
			{
				Debug.Assert(var_type_code == PhpTypeCode.DObject);

				// CALL Operators.GetObjectPropertyRef(STACK,<field name>,<type desc>);
				EmitName(codeGenerator);
				codeGenerator.EmitLoadClassContext();
				il.Emit(OpCodes.Call, Methods.Operators.GetObjectPropertyRef);
			}
			else
			{
				Debug.Assert(var_type_code == PhpTypeCode.ObjectAddress);

				// CALL Operators.GetPropertyRef(ref STACK,<field name>,<type desc>,<script context>);
				EmitName(codeGenerator);
				codeGenerator.EmitLoadClassContext();
				codeGenerator.EmitLoadScriptContext();
				il.Emit(OpCodes.Call, Methods.Operators.GetPropertyRef);

				// stores the value of variable back:
				SimpleVarUse simple_var = isMemberOf as SimpleVarUse;
				if (simple_var != null)
					simple_var.EmitLoadAddress_StoreBack(codeGenerator);
			}

			return PhpTypeCode.PhpReference;
		}

		/// <summary>
		/// Emits IL instructions that read the value of a field of $this instance.
		/// </summary>
		/// <param name="codeGenerator">The current <see cref="CodeGenerator"/>.</param>
		/// <param name="wantRef">If <B>false</B> the field value should be left on the evaluation stack,
		/// if <B>true</B> the <see cref="PhpReference"/> should be left on the evaluation stack.</param>
		/// <returns></returns>
		private PhpTypeCode EmitReadFieldOfThis(CodeGenerator/*!*/ codeGenerator, bool wantRef)
		{
			ILEmitter il = codeGenerator.IL;

			// $this->a
			switch (codeGenerator.LocationStack.LocationType)
			{
				case LocationTypes.GlobalCode:
					{
						// load $this from one of Main's arguments and check for null
						Label this_non_null = il.DefineLabel();
						Label reading_over = il.DefineLabel();

						codeGenerator.EmitLoadSelf();
						il.Emit(OpCodes.Brtrue_S, this_non_null);
						EmitThisUsedOutOfObjectThrow(codeGenerator, wantRef);
						il.Emit(OpCodes.Br, reading_over);
						il.MarkLabel(this_non_null, true);

						// call GetObjectProperty/GetObjectPropertyRef
						EmitGetFieldOfPlace(codeGenerator.SelfPlace, codeGenerator, wantRef);

						il.MarkLabel(reading_over, true);

						break;
					}

				case LocationTypes.FunctionDecl:
					{
						EmitThisUsedOutOfObjectThrow(codeGenerator, wantRef);
						break;
					}

				case LocationTypes.MethodDecl:
					{
						CompilerLocationStack.MethodDeclContext context = codeGenerator.LocationStack.PeekMethodDecl();
						if (context.Method.IsStatic)
						{
							EmitThisUsedOutOfObjectThrow(codeGenerator, wantRef);
							break;
						}

						// attempt direct field reading (DirectVarUse only)
						return EmitReadFieldOfThisInInstanceMethod(codeGenerator, wantRef);
					}
			}

			return wantRef ? PhpTypeCode.PhpReference : PhpTypeCode.Object;
		}

		/// <summary>
		/// Emits IL instructions that read the value of a field of $this instance when we know that we
		/// are in an instance method and hence there's a chance of actually resolving the field being read.
		/// </summary>
		/// <param name="codeGenerator">The current <see cref="CodeGenerator"/>.</param>
		/// <param name="wantRef">If <B>false</B> the field value should be left on the evaluation stack,
		/// if <B>true</B> the <see cref="PhpReference"/> should be left on the evaluation stack.</param>
		internal virtual PhpTypeCode EmitReadFieldOfThisInInstanceMethod(CodeGenerator/*!*/ codeGenerator, bool wantRef)
		{
			// the override in DirectVarUse is a bit more sophisticated ;)
			return EmitGetFieldOfPlace(codeGenerator.SelfPlace, codeGenerator, wantRef);
		}

		/// <summary>
		/// Emits error reporting call when "this" variable is used out of object context.
		/// </summary>
		private static void EmitThisUsedOutOfObjectThrow(CodeGenerator/*!*/ codeGenerator, bool wantRef)
		{
			codeGenerator.EmitPhpException(Methods.PhpException.ThisUsedOutOfObjectContext);
			if (wantRef)
				codeGenerator.IL.Emit(OpCodes.Newobj, Constructors.PhpReference_Void);
			else
				codeGenerator.IL.Emit(OpCodes.Ldnull);
		}

		/// <summary>
		/// Emits <see cref="Operators.GetObjectProperty"/> or <see cref="Operators.GetObjectPropertyRef"/>
		/// on a specified argument variable.
		/// </summary>
		private PhpTypeCode EmitGetFieldOfPlace(IPlace/*!*/ arg, CodeGenerator/*!*/ codeGenerator, bool wantRef)
		{
            //ILEmitter il = codeGenerator.IL;

            //arg.EmitLoad(il);
            //EmitName(codeGenerator);
            //codeGenerator.EmitLoadClassContext();

            //if (wantRef)
            //{
            //    il.Emit(OpCodes.Call, Methods.Operators.GetObjectPropertyRef);
            //    return PhpTypeCode.PhpReference;
            //}
            //else
            //{
            //    il.LoadBool(codeGenerator.ChainBuilder.QuietRead);
            //    il.Emit(OpCodes.Call, Methods.Operators.GetObjectProperty);
            //    return PhpTypeCode.Object;
            //}

            string fieldName = (this is DirectVarUse) ? ((DirectVarUse)this).VarName.Value : null;
            Expression fieldNameExpr = (this is IndirectVarUse) ? ((IndirectVarUse)this).VarNameEx : null;
            bool quietRead = wantRef ? false : codeGenerator.ChainBuilder.QuietRead;

            return codeGenerator.CallSitesBuilder.EmitGetProperty(
                codeGenerator, wantRef,
                null, arg, null,
                null,
                fieldName, fieldNameExpr,
                quietRead);
		}

		private static void EmitCallSetObjectField(CodeGenerator/*!*/ codeGenerator, PhpTypeCode stackTypeCode)
		{
			// CALL Operators.SetObjectProperty(<STACK:instance>,<STACK:field name>,<STACK:field value>, <type desc>)
			codeGenerator.EmitLoadClassContext();

			codeGenerator.IL.Emit(OpCodes.Call, Methods.Operators.SetObjectProperty);
            
            //always when function with void return argument is called it's necesarry to add nop instruction due to debugger
            if (codeGenerator.Context.Config.Compiler.Debug)
            {
                codeGenerator.IL.Emit(OpCodes.Nop);
            }
		}

		private static void EmitPopValue(CodeGenerator/*!*/ codeGenerator, PhpTypeCode stackTypeCode)
		{
			// just pop the value that was meant to be written
			codeGenerator.IL.Emit(OpCodes.Pop);
		}

		/// <summary>
		/// Emits IL instructions that write a value to an instance field.
		/// </summary>
		/// <param name="codeGenerator">The current <see cref="CodeGenerator"/>.</param>
		/// <param name="writeRef">If <B>true</B> the value being written is a <see cref="PhpReference"/>
		/// instance, if <B>false</B> it is an <see cref="Object"/> instance.</param>
		/// <returns>Delegate to a method that emits code to be executed when the actual value has been
		/// loaded on the evaluation stack.</returns>
		/// <remarks>
        /// If the field could be resolved at compile time (because <see cref="VarLikeConstructUse.isMemberOf"/> is <c>$this</c> or a
		/// variable is proved to be of a certain type by type analysis), direct field writing code is emitted.
		/// Otherwise, <see cref="Operators.SetProperty"/> or <see cref="Operators.SetObjectProperty"/> call is emitted.
		/// </remarks>
		internal virtual AssignmentCallback EmitWriteField(CodeGenerator/*!*/ codeGenerator, bool writeRef)
		{
			ILEmitter il = codeGenerator.IL;

			DirectVarUse direct_instance = isMemberOf as DirectVarUse;
			if (direct_instance != null && direct_instance.IsMemberOf == null && direct_instance.VarName.IsThisVariableName)
			{
				return EmitWriteFieldOfThis(codeGenerator, writeRef);
			}

			if (isMemberOf is ItemUse || isMemberOf is StaticFieldUse || isMemberOf.IsMemberOf != null)
			{
				// we are part of a chain
				// Lengthen for hop over ->
				codeGenerator.ChainBuilder.Lengthen();
				FunctionCall funcCall = isMemberOf as FunctionCall;
				if (funcCall == null)
				{
					isMemberOf.Emit(codeGenerator);
					EmitName(codeGenerator);
				}
				else
				{
					codeGenerator.ChainBuilder.LoadAddressOfFunctionReturnValue = true;
					isMemberOf.Emit(codeGenerator);
					codeGenerator.ChainBuilder.RecastValueReturnedByFunctionCall();

					EmitName(codeGenerator);
				}
				return new AssignmentCallback(EmitCallSetObjectField);
			}
			else
			{
				return delegate(CodeGenerator codeGen, PhpTypeCode stackTypeCode)
				{
					codeGen.ChainBuilder.Lengthen();

					// CALL Operators.SetProperty(STACK,ref <instance>,<field name>,<handle>,<script context>);
					isMemberOf.Emit(codeGen);
					EmitName(codeGen);
					codeGen.EmitLoadClassContext();
					codeGen.EmitLoadScriptContext();

					// invoke the operator
					codeGen.IL.Emit(OpCodes.Call, Methods.Operators.SetProperty);
				};
			}
		}

		/// <summary>
		/// Emits IL instructions that prepare a field of $this for writing.
		/// </summary>
		private AssignmentCallback EmitWriteFieldOfThis(CodeGenerator/*!*/ codeGenerator, bool writeRef)
		{
			ILEmitter il = codeGenerator.IL;

			// $this->a
			switch (codeGenerator.LocationStack.LocationType)
			{
				case LocationTypes.GlobalCode:
					{
						// load $this from one of Main's arguments and check for null
						Label this_non_null = il.DefineLabel();

						codeGenerator.EmitLoadSelf();
						il.Emit(OpCodes.Brtrue_S, this_non_null);
						codeGenerator.EmitPhpException(Methods.PhpException.ThisUsedOutOfObjectContext);
						il.Emit(OpCodes.Br, codeGenerator.ChainBuilder.ErrorLabel);
						il.MarkLabel(this_non_null, true);

						// prepare the stack for SetObjectProperty call
						codeGenerator.EmitLoadSelf();
						EmitName(codeGenerator);

						return new AssignmentCallback(EmitCallSetObjectField);
					}

				case LocationTypes.FunctionDecl:
					{
						// always throws error
						codeGenerator.EmitPhpException(Methods.PhpException.ThisUsedOutOfObjectContext);
						return new AssignmentCallback(EmitPopValue);
					}

				case LocationTypes.MethodDecl:
					{
						CompilerLocationStack.MethodDeclContext context = codeGenerator.LocationStack.PeekMethodDecl();
						if (context.Method.IsStatic)
						{
							// always throws error
							codeGenerator.EmitPhpException(Methods.PhpException.ThisUsedOutOfObjectContext);
							return new AssignmentCallback(EmitPopValue);
						}

						// attempt direct field writing (DirectVarUse only)
						return EmitWriteFieldOfThisInInstanceMethod(codeGenerator, writeRef);
					}
			}

			Debug.Fail("Invalid lcoation type.");
			return null;
		}

		/// <summary>
		/// Emits IL instructions that write the value of a field of $this instance when we know that we
		/// are in an instance method and hence there's a chance of actually resolving the field being written.
		/// </summary>
		/// <param name="codeGenerator">The current <see cref="CodeGenerator"/>.</param>
		/// <param name="writeRef">If <B>true</B> the value being written is a <see cref="PhpReference"/>; if
		/// <B>false</B> the value being written is an <see cref="Object"/>.</param>
		/// <returns></returns>
		internal virtual AssignmentCallback EmitWriteFieldOfThisInInstanceMethod(CodeGenerator/*!*/ codeGenerator, bool writeRef)
		{
			// prepare for SetObjectProperty call
			codeGenerator.EmitLoadSelf();
			EmitName(codeGenerator);

			return new AssignmentCallback(EmitCallSetObjectField);
		}
	}

	#endregion
}
