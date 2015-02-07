/*

 Copyright (c) 2003-2006 Martin Maly, Ladislav Prosek, Tomas Matousek.

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
using System.Collections.Generic;

#if SILVERLIGHT
using PHP.CoreCLR;
#endif

namespace PHP.Core
{
	/// <summary>Defines relevant location types.</summary>
	public enum LocationTypes { GlobalCode, FunctionDecl, TypeDecl, MethodDecl };

	/// <summary>
	/// Manages a stack of source code locations along with a user-defined context.
	/// </summary>
	internal class LocationStack
	{
		#region Nested Class: Location

		/// <summary>
		/// Class of objects stored in the <see cref="stack"/>.
		/// </summary>
		protected class Location
		{
			/// <summary>
			/// Location type.
			/// </summary>
			public LocationTypes Type;

			/// <summary>
			/// User-defined context associated with this stack element.
			/// </summary>
			public object Context;

			/// <summary>
			/// Creates a new <see cref="Location"/>.
			/// </summary>
			/// <param name="type">A location type of the new <see cref="Location"/>.</param>
			/// <param name="context">A context of the new <see cref="Location"/>.</param>
			public Location(LocationTypes type, object context)
			{
				this.Type = type;
				this.Context = context;
			}
		}

		#endregion

		#region Construction

		/// <summary>
		/// Creates a new empty <see cref="LocationStack"/>.
		/// </summary>
		public LocationStack()
		{
			stack = new Stack();
		}

		#endregion

		#region Properties

		/// <summary>
		/// The underlying <see cref="Stack"/> data structure.
		/// </summary>
		protected Stack stack;

		/// <summary>
		/// Returns <B>true</B> if the element atop the stack is marked with <see cref="LocationTypes.GlobalCode"/>,
		/// <B>false</B> otherwise.
		/// </summary>
		public bool InGlobalCode
		{
			get
			{
				if (stack.Count == 0) return false;
				return ((Location)stack.Peek()).Type == LocationTypes.GlobalCode;
			}
		}

		/// <summary>
		/// Returns <B>true</B> if the element atop the stack is marked with <see cref="LocationTypes.FunctionDecl"/>,
		/// <B>false</B> otherwise.
		/// </summary>
		public bool InFunctionDecl
		{
			get
			{
				if (stack.Count == 0) return false;
				return ((Location)stack.Peek()).Type == LocationTypes.FunctionDecl;
			}
		}

		/// <summary>
		/// Returns <B>true</B> if the element atop the stack is marked with <see cref="LocationTypes.TypeDecl"/>,
		/// <B>false</B> otherwise.
		/// </summary>
		public bool InClassDecl
		{
			get
			{
				if (stack.Count == 0) return false;
				return ((Location)stack.Peek()).Type == LocationTypes.TypeDecl;
			}
		}

		/// <summary>
		/// Returns <B>true</B> if the element atop the stack is marked with <see cref="LocationTypes.MethodDecl"/>,
		/// <B>false</B> otherwise.
		/// </summary>
		public bool InMethodDecl
		{
			get
			{
				if (stack.Count == 0) return false;
				return ((Location)stack.Peek()).Type == LocationTypes.MethodDecl;
			}
		}

		/// <summary>
		/// Returns <B>true</B> if the <see cref="stack"/> is empty, <B>false</B> otherwise.
		/// </summary>
		public bool IsEmpty
		{
			get
			{
				return stack.Count == 0;
			}
		}

		/// <summary>
		/// Returns one of the <see cref="LocationTypes"/> of current stack top or <see cref="LocationTypes.GlobalCode"/>
		/// if the stack is empty.
		/// </summary>
		public LocationTypes LocationType
		{
			get
			{
				if (stack.Count == 0) return LocationTypes.GlobalCode;
				return ((Location)stack.Peek()).Type;
			}
		}

		#endregion

		#region Push, Pop, Peek

		/// <summary>
		/// Pushes a new <see cref="Location"/> to the stack.
		/// </summary>
		/// <param name="type">A location type of the new <see cref="Location"/>.</param>
		/// <param name="context">A context of the new <see cref="Location"/>.</param>
		public void Push(LocationTypes type, object context)
		{
			stack.Push(new Location(type, context));
		}

		/// <summary>
		/// Pops a location context from the top of the stack.
		/// </summary>
		/// <returns>The <see cref="Location.Context"/> of the element atop the stack.</returns>
		/// <exception cref="InvalidOperationException">The stack is empty.</exception>
		public object Pop()
		{
			return ((Location)stack.Pop()).Context;
		}

		/// <summary>
		/// Peeks a location context at the top of the stack.
		/// </summary>
		/// <returns>The <see cref="Location.Context"/> of the element atop the stack.</returns>
		/// <exception cref="InvalidOperationException">The stack is empty.</exception>
		public object Peek()
		{
			return ((Location)stack.Peek()).Context;
		}

		#endregion
	}

	/// <summary>
	/// Extends the <see cref="LocationStack"/> with functionality that is specific for the code generator.
	/// </summary>
	internal class CompilerLocationStack : LocationStack
	{
		#region Nested Classes: GlobalCodeContext, ClassDeclContext, MethodDeclContext, FunctionDeclContext

		/// <summary>
		/// Routine declaration context. A routine may be either global code, user function, or user method.
		/// </summary>
		public abstract class ProcedureDeclContext
		{
			/// <summary>
			/// IL emitter.
			/// </summary>
			public ILEmitter IL;

			/// <summary>
			/// Place storing the current script context.
			/// </summary>
			public IPlace ScriptContextPlace;

			/// <summary>
			/// Place where run-time local variables table is stored.
			/// </summary>
			public IPlace RTVariablesTablePlace;

			/// <summary>
			/// Place where type desc is stored.
			/// </summary>
			public IPlace ClassContextPlace;

			/// <summary>
			/// Place where self object is stored.
			/// </summary>
			public IPlace SelfPlace;

            /// <summary>
            /// Place where calling type is stored.
            /// </summary>
            public IPlace LateStaticBindTypePlace;

			/// <summary>
			/// Place where result of the routine is stored when returning value from the routine.
			/// </summary>
			public IPlace ResultPlace;

			/// <summary>
			/// Label where return statements branch to.
			/// </summary>
			public Label ReturnLabel;

			/// <summary>
			/// Table of local compile-time variables.
			/// </summary>
			public VariablesTable CurrentVariablesTable;

			/// <summary>
			/// Table of local compile-time variables.
			/// </summary>
			public Dictionary<VariableName, Statement> CurrentLabels;

			/// <summary>
			/// Whether local variables are optimized.
			/// <B>false</B> for global code.
			/// </summary>
			public bool OptimizedLocals;

			/// <summary>
			/// Whether the routine returns by reference.
			/// <B>false</B> for global code.
			/// </summary>
			public bool ReturnsPhpReference;

			/// <summary>
			/// Number of exception nested blocks (both try and catch blocks). 
			/// </summary>
			public int ExceptionBlockNestingLevel;

            /// <summary>
            /// The PhpRoutine of the current location.
            /// Used for obtaining indexes of type arguments (generic functions/methods).
            /// Can be null in case of lambda function.
            /// </summary>
            public PhpRoutine PhpRoutine;
		}

		/// <summary>
		/// Global code context.
		/// </summary>
		public sealed class GlobalCodeContext : ProcedureDeclContext
		{
		}

		/// <summary>
		/// Class declaration context.
		/// </summary>
		/// <remarks>
		/// Contains information that need to be kept when generating a class declaration.
		/// </remarks>
		public sealed class TypeDeclContext
		{
			public PhpType Type;
			public IPlace TypeContextPlace;

            /// <summary>
            /// CallSite manager and emitter.
            /// </summary>
            public PHP.Core.Compiler.CodeGenerator.CallSitesBuilder CallSites;
		}

		/// <summary>
		/// GetUserEntryPoint declaration context.
		/// </summary>
		/// <remarks>
		/// Contains information that need to be kept when generating a method.
		/// </remarks>
		public sealed class MethodDeclContext : ProcedureDeclContext
		{
			public PhpType Type;
			public PhpMethod Method;
		}

		/// <summary>
		/// Function declaration context.
		/// </summary>
		/// <remarks>
		/// Contains information that need to be kept when generating a function.
		/// </remarks>
		public sealed class FunctionDeclContext : ProcedureDeclContext
		{
			public QualifiedName Name;

            /// <summary>
            /// CallSite manager and emitter.
            /// </summary>
            public PHP.Core.Compiler.CodeGenerator.CallSitesBuilder CallSites;
		}

		#endregion

		#region Push

		/// <summary>
		/// Pushes a <see cref="TypeDeclContext"/> to the stack.
		/// </summary>
		/// <param name="context">The context.</param>
		public void PushTypeDecl(TypeDeclContext context)
		{
			Push(LocationTypes.TypeDecl, context);
		}

		/// <summary>
		/// Pushes a <see cref="GlobalCodeContext"/> to the stack.
		/// </summary>
		/// <param name="context">The context.</param>
		public void PushGlobalCode(GlobalCodeContext context)
		{
			Push(LocationTypes.GlobalCode, context);
		}

		/// <summary>
		/// Pushes a <see cref="MethodDeclContext"/> to the stack.
		/// </summary>
		/// <param name="context">The context.</param>
		public void PushMethodDecl(MethodDeclContext context)
		{
			Push(LocationTypes.MethodDecl, context);
		}

		/// <summary>
		/// Pushes a <see cref="FunctionDeclContext"/> to the stack.
		/// </summary>
		/// <param name="context">The context.</param>
		public void PushFunctionDecl(FunctionDeclContext context)
		{
			Push(LocationTypes.FunctionDecl, context);
		}

		#endregion

		#region Peek

		/// <summary>
		/// Peeks a <see cref="TypeDeclContext"/> at the top of the stack.
		/// </summary>
		/// <returns>The context.</returns>
		/// <exception cref="InvalidOperationException">The stack is empty.</exception>
		public TypeDeclContext PeekTypeDecl()
		{
			Debug.Assert(InClassDecl);
			return (TypeDeclContext)Peek();
		}

		/// <summary>
		/// Peeks a <see cref="GlobalCodeContext"/> at the top of the stack.
		/// </summary>
		/// <returns>The context.</returns>
		/// <exception cref="InvalidOperationException">The stack is empty.</exception>
		public GlobalCodeContext PeekGlobalCode()
		{
			Debug.Assert(InGlobalCode);
			return (GlobalCodeContext)Peek();
		}

		/// <summary>
		/// Peeks a <see cref="MethodDeclContext"/> at the top of the stack.
		/// </summary>
		/// <returns>The context.</returns>
		/// <exception cref="InvalidOperationException">The stack is empty.</exception>
		public MethodDeclContext PeekMethodDecl()
		{
			Debug.Assert(InMethodDecl);
			return (MethodDeclContext)Peek();
		}

		/// <summary>
		/// Peeks a <see cref="FunctionDeclContext"/> at the top of the stack.
		/// </summary>
		/// <returns>The context.</returns>
		/// <exception cref="InvalidOperationException">The stack is empty.</exception>
		public FunctionDeclContext PeekFunctionDecl()
		{
			Debug.Assert(InFunctionDecl);
			return (FunctionDeclContext)Peek();
		}

		#endregion
	}

	/// <summary>
	/// A stack used by <B>continue</B> and <B>break</B> statements code generation to track 
	/// loops and switch statements. Inner class of <see cref="CodeGenerator"/>.
	/// </summary>
	internal class BranchingStack
	{
		/// <summary>
		/// Creates an instance of the stack associated with a specified generator.
		/// </summary>
		/// <param name="codeGenerator">The code generator.</param>
		public BranchingStack(CodeGenerator codeGenerator)
		{
			this.codeGenerator = codeGenerator;
		}

		/// <summary>
		/// Owning generator.
		/// </summary>
		private CodeGenerator codeGenerator;

		/// <summary>
		/// Items of the stack. We need to enumerate the stack in well defined way, so
		/// we cannot use <see cref="Stack"/> class as it didn't ensure the order.
		/// </summary>
		private ArrayList stack = new ArrayList(); // GENERICS <StackItem>

		#region Nested Class: StackItem

		/// <summary>
		/// Represents an element of stack collection. Holds <see cref="System.Reflection.Emit.Label"/> items
		/// to manage the code generation of PHP <B>break</B> and <B>continue</B> statements inside loops and switch statement.
		/// </summary>
		private class StackItem
		{
			/// <summary>
			/// A target label to which the control is transfered by <B>continue</B> statement.
			/// </summary>
			public Label ConditionLabel { get { return this.conditionLabel; } }
			private Label conditionLabel; // mark the condition (used for continue)

			/// <summary>
			/// Gets a target label to which the control is transfered by <B>break</B> statement.
			/// </summary>
			public Label ExitLabel { get { return this.exitLabel; } }
			private Label exitLabel;  // mark the end of loop (used for break)

			/// <summary>
			/// A level of exception block nesting where the loop/switch is located.
			/// When enitting <c>break</c> or <c>continue</c> jump, we need to know
			/// the nesting level of the branch target which is either begin or 
			/// end of the loop or end of the switch.
			/// </summary>
			public int ExceptionBlockNestingLevel { get { return exceptionBlockNestingLevel; } }
			private int exceptionBlockNestingLevel;

			/// <summary>
			/// Initializes a new instance of <see cref="StackItem"/>.
			/// </summary>
			/// <param name="conditionLabel">
			/// The target <see cref="Label"/> for <B>continue</B> statement code generation.
			/// </param>
			/// <param name="exitLabel">
			/// The target <see cref="Label"/> for <B>break</B> statement code generation.
			/// </param>
			/// <param name="exceptionBlockNestingLevel">
			/// Level of exception block nesting.
			/// </param>
			public StackItem(Label conditionLabel, Label exitLabel, int exceptionBlockNestingLevel)
			{
				this.conditionLabel = conditionLabel;
				this.exitLabel = exitLabel;
				this.exceptionBlockNestingLevel = exceptionBlockNestingLevel;
			}
		}

		#endregion

		#region BeginLoop, EndLoop

		/// <summary>
		/// Begins a new scope for loop (<B>for</B> and <B>while</B>) and switch statement code generation.
		/// </summary>
		/// <param name="conditionLabel">
		/// The target <see cref="Label"/> for <B>continue</B> statement code generation.
		/// </param>
		/// <param name="exitLabel">
		/// The target <see cref="Label"/> for <B>break</B> statement code generation.
		/// </param>
		/// <param name="exceptionBlockNestingLevel">
		/// Level of exception block nesting.
		/// </param>
		/// <remarks>
		/// This function should be called once at the beginning of each <B>for</B>, <B>while</B> or 
		/// <B>switch</B> statement. It adds a new <see cref="StackItem"/> item to the stack.
		/// </remarks>
		public void BeginLoop(Label conditionLabel, Label exitLabel, int exceptionBlockNestingLevel)
		{
			stack.Add(new StackItem(conditionLabel, exitLabel, exceptionBlockNestingLevel));
		}

		/// <summary>
		/// Ends a scope for loop (<B>for</B> and <B>while</B>) and switch statement code generation.
		/// </summary>
		/// <remarks>
		/// This function should be called once at the end of each <B>for</B>, <B>while</B> 
		/// or <B>switch</B> statement. 
		/// </remarks>
		public void EndLoop()
		{
			Debug.Assert(stack.Count > 0);

			// Remove last item
			stack.RemoveAt(stack.Count - 1);
		}

		#endregion

		#region EmitBreak, EmitBreakRuntime

		private void EmitBranch(Label target, int targetExceptionLevel)
		{
			if (codeGenerator.ExceptionBlockNestingLevel != targetExceptionLevel)
				codeGenerator.IL.Emit(OpCodes.Leave, target);
			else
				codeGenerator.IL.Emit(OpCodes.Br, target);
		}

		private void EmitBranchToExit(StackItem item)
		{
			EmitBranch(item.ExitLabel, item.ExceptionBlockNestingLevel);
		}

		private void EmitBranchToCondition(StackItem item)
		{
			EmitBranch(item.ConditionLabel, item.ExceptionBlockNestingLevel);
		}

		/// <summary>
		/// Emits IL instructions that transfer the control to the target label for parametherless <B>break</B> statement.
		/// </summary>
		/// <remarks>This function is used to generate code for <B>break;</B> statement.</remarks>
		public void EmitBreak()
		{
			Debug.Assert(stack.Count > 0);

			// get the top item
			StackItem item = (StackItem)stack[stack.Count - 1];

			Debug.Assert(item != null);

			EmitBranchToExit(item);
		}

		/// <summary>
		/// Emits IL instructions that transfer the control to the target label for <B>break</B> statement having one <see cref="Literal"/> parameter.
		/// </summary>
		public void EmitBreak(int loopsToSkip)
		{
			if (loopsToSkip == 0)
				loopsToSkip = 1;

			Debug.Assert(stack.Count >= loopsToSkip);

			// Get the item to whitch we want to jump
			StackItem item = (StackItem)stack[stack.Count - loopsToSkip];
			Debug.Assert(item != null);

			EmitBranchToExit(item);
		}

		/// <summary>
		/// Emits IL instructions that transfer the control to the target label for <B>break</B> statement 
		/// having parameter that cannot be evaluated at compile time.
		/// </summary>
		/// <remarks>This function is used to generate code for <B>break v;</B> where <i>v</i> is a variable.</remarks>
		public void EmitBreakRuntime()
		{
			int i;
			ILEmitter il = codeGenerator.IL;
			Label[] jumpTable = new Label[stack.Count + 1];
			Label exitLabel = il.DefineLabel();

			Debug.Assert(stack.Count != 0);

			for (i = 0; i <= stack.Count; i++)
			{
				jumpTable[i] = il.DefineLabel();
			}

			// The value according to we switch is already present on the evaluation stack
			LocalBuilder break_level_count = il.DeclareLocal(typeof(Int32));
			il.Emit(OpCodes.Dup);
			il.Stloc(break_level_count);
			il.Emit(OpCodes.Switch, jumpTable);

			// Default case
			il.Ldloc(break_level_count);
			codeGenerator.EmitPhpException(Methods.PhpException.InvalidBreakLevelCount);
			il.Emit(OpCodes.Br, exitLabel);

			il.MarkLabel(jumpTable[0]);
			EmitBranchToExit((StackItem)stack[stack.Count - 1]);

			for (i = 1; i <= stack.Count; i++)
			{
				il.MarkLabel(jumpTable[i]);
				EmitBranchToExit((StackItem)stack[stack.Count - i]);
			}

			il.MarkLabel(exitLabel);
		}

		#endregion

		#region EmitContinue, EmitContinueRuntime

		/// <summary>
		/// Emits IL instructions that transfer the control to the target label for parametherless <B>continue</B> statement.
		/// </summary>
		/// <remarks>This function is used to generate code for <B>continue;</B> statement.</remarks>
		public void EmitContinue()
		{
			Debug.Assert(stack.Count != 0);

			// Get the top item
			StackItem item = (StackItem)stack[stack.Count - 1];

			Debug.Assert(item != null);

			EmitBranchToCondition(item);
		}

		/// <summary>
		/// Emits IL instructions that transfer the control to the target label for <B>continue</B> statement
		/// having one <see cref="Literal"/> parameter.
		/// </summary>
		public void EmitContinue(int loopsToSkip)
		{
			if (loopsToSkip == 0)
				loopsToSkip = 1;

			Debug.Assert(stack.Count >= loopsToSkip);

			// Get the item to whitch we want to jump
			StackItem item = (StackItem)stack[stack.Count - loopsToSkip];

			Debug.Assert(item != null);

			EmitBranchToCondition(item);
		}

		/// <summary>
		/// Emits IL instructions that transfer the control to the target label for <B>continue</B> statement 
		/// having parameter that cannot be evaluated at compile time.
		/// </summary>
		/// <remarks>This function is used to generate code for <B>continue v;</B> where <i>v</i> is a variable.</remarks>
		public void EmitContinueRuntime()
		{
			int i;
			ILEmitter il = codeGenerator.IL;
			Label[] jumpTable = new Label[stack.Count + 1];
			Label exitLabel = il.DefineLabel();

			Debug.Assert(stack.Count != 0);

			for (i = 0; i <= stack.Count; i++)
			{
				jumpTable[i] = il.DefineLabel();
			}

			// The value accotding to we switch is already present at IL stack
			LocalBuilder continue_level_count = il.DeclareLocal(typeof(Int32));
			il.Emit(OpCodes.Dup);
			il.Stloc(continue_level_count);
			il.Emit(OpCodes.Switch, jumpTable);

			// Default case
			il.Ldloc(continue_level_count);
			codeGenerator.EmitPhpException(Methods.PhpException.InvalidBreakLevelCount);
			il.Emit(OpCodes.Br, exitLabel);
			EmitBranchToCondition((StackItem)stack[stack.Count - 1]);
			il.MarkLabel(jumpTable[0]);

			for (i = 1; i <= stack.Count; i++)
			{
				il.MarkLabel(jumpTable[i]);
				EmitBranchToCondition((StackItem)stack[stack.Count - i]);
			}

			il.MarkLabel(exitLabel);
		}

		#endregion
	}
}	
