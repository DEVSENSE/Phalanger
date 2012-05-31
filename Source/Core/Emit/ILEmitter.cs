/*

 Copyright (c) 2004-2006 Ladislav Prosek and Tomas Matousek.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/


using System;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Diagnostics.SymbolStore;

/*

  Designed and implemented by Ladislav Prosek and Tomas Matousek.

*/

namespace PHP.Core.Emit
{
	/// <summary>
	/// <see cref="ILGenerator"/> wrapper providing many useful services concerning MSIL emission.
	/// </summary>
	/// <threadsafety static="true" instance="false"/>
	[DebuggerNonUserCode]
	public sealed class ILEmitter
	{
		public enum Containers
		{
			MethodBuilder,
			ConstructorBuilder,
			DynamicMethod
		}

		public Containers Container { get { return container; } }
		private Containers container;

		/// <summary>
		/// Represents a distinguished position in the Microsoft intermediate language (MSIL) stream where a &quot;feature&quot;
		/// has been marked.
		/// </summary>
		private sealed class FeaturePoint
		{
			#region Fields and properties

			/// <summary>
			/// Determines whether the Microsoft intermediate language (MSIL) stream's current position is unconditionally
			/// preceded by the position where this <see cref="FeaturePoint"/> was defined.
			/// </summary>
			/// <remarks><seealso cref="ILEmitter.IsFeatureControlFlowPrecedent"/></remarks>
			public bool IsControlFlowPrecedent
			{
				get
				{
					// if there were no unresolved branch targets when this feature point was marked, then the marking
					// position was "unskippable" so no matter what happened after the feature point, the 'unconditionally
					// precedes' condition is satisfied
					if (unresolvedBranches.Count == 0) return true;

					// if a branch target was resolved (marked) - one that was unresolved when this feature point was marked -
					// then we have a possible forward branch pointing between marking position and current position so
					// the 'unconditionally precedes' condition is not satisfied
					if (forwardBranchResolved) return false;

					// finally check for possible backward branches
					return (branchTargets == null || branchTargets.Count == 0);
				}
			}

			/// <summary>
			/// Collection of <see cref="Label"/>s marked and not forgotten since this <see cref="FeaturePoint"/> was created
			/// (keyed by <see cref="Label"/>s, values unused).
			/// </summary>
			/// <remarks><seealso cref="MarkLabel"/></remarks>
			private Dictionary<Label, object> branchTargets;

			/// <summary>
			/// Collection of <see cref="Label"/>s - targets of branch instructions that had been emitted when this
			/// <see cref="FeaturePoint"/> was marked - that had not been marked at that time.
			/// </summary>
			/// <remarks>
			/// This is a snapshot of <see cref="ILEmitter.unresolvedBranches"/> at the time this <see cref="FeaturePoint"/>
			/// was marked.
			/// <seealso cref="Emit(OpCode,Label)"/><seealso cref="MarkLabel"/>
			/// </remarks>
			private Dictionary<Label, object> unresolvedBranches;

			/// <summary>
			/// <B>null</B> if a branch target was resolved (marked) - one that was unresolved when this <see cref="FeaturePoint"/>
			/// - was marked.
			/// </summary>
			private bool forwardBranchResolved;

			#endregion

			#region Construction

			/// <summary>
			/// Creates a new <see cref="FeaturePoint"/>.
			/// </summary>
			public FeaturePoint(Dictionary<Label, object> unresolvedBranches)
			{
				this.unresolvedBranches = new Dictionary<Label, object>(unresolvedBranches);
			}

			#endregion

			#region MarkLabel, ForgetLabel

			/// <summary>
			/// Notifies this <see cref="FeaturePoint"/> that a <see cref="Label"/> has been marked.
			/// </summary>
			/// <param name="loc">The label.</param>
			/// <remarks><seealso cref="ILEmitter.MarkLabel"/></remarks>
			public void MarkLabel(Label loc)
			{
				if (unresolvedBranches.ContainsKey(loc)) forwardBranchResolved = true;
				else
				{
					if (branchTargets == null) branchTargets = new Dictionary<Label, object>();
					branchTargets.Add(loc, null);
				}
			}

			/// <summary>
			/// Notifies this <see cref="FeaturePoint"/> that a <see cref="Label"/> has been forgotten.
			/// </summary>
			/// <param name="loc">The label.</param>
			/// <remarks><seealso cref="ILEmitter.ForgetLabel"/></remarks>
			public void ForgetLabel(Label loc)
			{
				if (branchTargets != null) branchTargets.Remove(loc);
			}

			#endregion
		}

		#region Fields and properties

		/// <summary>
		/// The <see cref="ILGenerator"/> wrapped by this instance;
		/// </summary>
		private ILGenerator/*!*/ il;

		/// <summary>
		/// GetUserEntryPoint whose body is being emitted by this emitter.
		/// </summary>
		public ConstructorBuilder ConstructorBuilder { get { return method as ConstructorBuilder; } }
		public MethodBuilder MethodBuilder { get { return method as MethodBuilder; } }
		public DynamicMethod DynamicMethod { get { return method as DynamicMethod; } }
		public MethodBase MethodBase { get { return method; } }
		private MethodBase/*!*/ method;

		public TypeBuilder TypeBuilder { get { return (TypeBuilder)method.DeclaringType; } }

        /// <summary>
        /// Gets the current offset, in bytes, in the Microsoft intermediate language (MSIL) stream
        /// that is being emitted by the System.Reflection.Emit.ILGenerator.
        /// Returns the offset in the MSIL stream at which the next instruction will be emitted.
        /// </summary>
        public int ILOffset { get { return il.ILOffset; } }

		/// <summary>
		/// The last <see cref="OpCode"/> emitted by this <see cref="ILEmitter"/>.
		/// </summary>
		private OpCode lastEmittedOpCode;

		/// <summary>
		/// <B>true</B> if a <see cref="Label"/> was marked at the current position, <B>false</B> otherwise.
		/// </summary>
		private bool isPositionLabeled;

		/// <summary>
		/// Collection of temporary local variables available for reuse (lazy init).
		/// </summary>
		private LinkedList<LocalBuilder> temporaryLocals;

		/// <summary>
		/// Collection of <see cref="FeaturePoint"/>s marked so far (keyed by <see cref="Object"/>s, lazy init).
		/// </summary>
		/// <remarks><seealso cref="MarkFeature"/></remarks>
		private Dictionary<object, FeaturePoint> featurePoints;

		/// <summary>
		/// Collection of <see cref="Label"/>s - targets of branch instructions emitted so far - that have not been
		/// marked yet.
		/// </summary>
		/// <remarks><seealso cref="Emit(OpCode,Label)"/><seealso cref="MarkLabel"/></remarks>
		private Dictionary<Label, object> unresolvedBranches;

		/// <summary>
		/// Collection of <see cref="Label"/>s marked and not forgotten so far (keyed by <see cref="Label"/>s, values unused).
		/// </summary>
		/// <remarks><seealso cref="MarkLabel"/></remarks>
		private Dictionary<Label, object> branchTargets;

#if DEBUG
		/// <summary>
		/// Collection of <see cref="Label"/>s forgotten so far (keyed by <see cref="Label"/>s, values unused).
		/// </summary>
		private Dictionary<Label, object> forgottenTargets;
#endif

		private int uniqueIndex = 0;

		public int GetNextUniqueIndex()
		{
			return ++uniqueIndex;
		}

		public int GetCurrentUniqueIndex()
		{
			return uniqueIndex;
		}

		/// <summary>
		/// Returns the last <see cref="OpCode"/> emitted by this <see cref="ILEmitter"/>.
		/// </summary>
		public OpCode LastEmittedOpCode
		{
			get
			{ return lastEmittedOpCode; }
		}

		/// <summary>
		/// Returns <B>true</B> if a <see cref="Label"/> was marked at the current position, <B>false</B> otherwise.
		/// </summary>
		public bool IsPositionLabeled
		{
			get
			{ return isPositionLabeled; }
		}

		#endregion

		#region Construction

		/// <summary>
		/// Creates a new <see cref="ILEmitter"/> by wrapping an <see cref="ILGenerator"/>
		/// </summary>
		private ILEmitter(ILGenerator/*!*/ il, Containers container)
		{
			this.il = il;
			this.container = container;
			this.branchTargets = new Dictionary<Label, object>();
			this.unresolvedBranches = new Dictionary<Label, object>();

#if DEBUG
			forgottenTargets = new Dictionary<Label, object>();
#endif
		}

		/// <summary>
		/// Creates a new <see cref="ILEmitter"/> for a given <see cref="MethodBuilder"/>.
		/// </summary>
		/// <param name="methodBuilder">The <see cref="MethodBuilder"/> to emit to.</param>
		public ILEmitter(MethodBuilder/*!*/ methodBuilder)
			: this(methodBuilder.GetILGenerator(), Containers.MethodBuilder)
		{
			this.method = methodBuilder;
		}

		/// <summary>
		/// Creates a new <see cref="ILEmitter"/> for a given <see cref="ConstructorBuilder"/>.
		/// </summary>
		/// <param name="constructorBuilder">The <see cref="ConstructorBuilder"/> to emit to.</param>
		public ILEmitter(ConstructorBuilder/*!*/ constructorBuilder)
			: this(constructorBuilder.GetILGenerator(), Containers.ConstructorBuilder)
		{
			this.method = constructorBuilder;
		}

		/// <summary>
		/// Creates a new <see cref="ILEmitter"/> for a given <see cref="DynamicMethod"/>.
		/// </summary>
		/// <param name="dynamicMethod">The <see cref="DynamicMethod"/> to emit to.</param>
		public ILEmitter(DynamicMethod/*!*/ dynamicMethod)
			: this(dynamicMethod.GetILGenerator(), Containers.DynamicMethod)
		{
			this.method = dynamicMethod;
		}

		/// <summary>
		/// Creates a new <see cref="ILEmitter"/> for a given <see cref="MethodBuilder"/> or <see cref="DynamicMethod"/>.
		/// </summary>
		/// <param name="method">The <see cref="MethodBuilder"/> or <see cref="DynamicMethod"/> to emit to.</param>
		public ILEmitter(MethodInfo/*!*/ method)
			: this(
				(method is MethodBuilder) ? ((MethodBuilder)method).GetILGenerator() : ((DynamicMethod)method).GetILGenerator(),
				(method is MethodBuilder) ? Containers.MethodBuilder : Containers.DynamicMethod)
		{
			this.method = method;
		}

		#endregion

		#region Pass-thru methods

		/// <summary>
		/// Begins a catch block.
		/// </summary>
		public void BeginCatchBlock(Type exceptionType)
		{ il.BeginCatchBlock(exceptionType); }

		/// <summary>
		/// Begins an exception block for a filtered exception.
		/// </summary>
		public void BeginExceptFilterBlock()
		{ il.BeginExceptFilterBlock(); }

		/// <summary>
		/// Begins an exception block for a non-filtered exception.
		/// </summary>
		public Label BeginExceptionBlock()
		{ return il.BeginExceptionBlock(); }

		/// <summary>
		/// Begins an exception fault block in the Microsoft intermediate language (MSIL) stream.
		/// </summary>
		public void BeginFaultBlock()
		{ il.BeginFaultBlock(); }

		/// <summary>
		/// Begins a finally block in the Microsoft intermediate language (MSIL) instruction stream.
		/// </summary>
		public void BeginFinallyBlock()
		{ il.BeginFinallyBlock(); }

		/// <summary>
		/// Emits an instruction to throw an exception.
		/// </summary>
		public void ThrowException(Type type)
		{ il.ThrowException(type); }

		/// <summary>
		/// Begins a lexical scope.
		/// </summary>
		public void BeginScope()
		{ il.BeginScope(); }

		/// <summary>
		/// Declares a local variable.
		/// </summary>
		public LocalBuilder DeclareLocal(Type localType)
		{ return il.DeclareLocal(localType); }

		/// <summary>
		/// Declares a new label.
		/// </summary>
		public Label DefineLabel()
		{ return il.DefineLabel(); }

		/// <summary>
		/// Ends an exception block.
		/// </summary>
		public void EndExceptionBlock()
		{ il.EndExceptionBlock(); }

		/// <summary>
		/// Ends a lexical scope.
		/// </summary>
		public void EndScope()
		{ il.EndScope(); }

		/// <summary>
		/// Marks a sequence point in the Microsoft intermediate language (MSIL) stream.
		/// </summary>
		public void MarkSequencePoint(ISymbolDocumentWriter document, int startLine, int startColumn, int endLine,
			int endColumn)
		{ il.MarkSequencePoint(document, startLine, startColumn, endLine, endColumn); }

		/// <summary>
		/// Specifies the namespace to be used in evaluating locals and watches for the current active lexical scope.
		/// </summary>
		public void UsingNamespace(string usingNamespace)
		{ il.UsingNamespace(usingNamespace); }

		#endregion

		#region Emits

		/// <summary>
		/// Puts the specified instruction onto the stream of instructions.
		/// </summary>
		public void Emit(OpCode opcode)
		{
			if (InterceptEmit(opcode, null)) il.Emit(opcode);
		}

		/// <summary>
		/// Puts the specified instruction and character argument onto the Microsoft intermediate language (MSIL)
		/// stream of instructions.
		/// </summary>
		public void Emit(OpCode opcode, byte arg)
		{
			if (InterceptEmit(opcode, arg)) il.Emit(opcode, arg);
		}

		/// <summary>
		/// Puts the specified instruction and metadata token for the specified constructor onto the Microsoft
		/// intermediate language (MSIL) stream of instructions.
		/// </summary>
		public void Emit(OpCode opcode, ConstructorInfo con)
		{
			if (InterceptEmit(opcode, con)) il.Emit(opcode, con);
		}

		/// <summary>
		/// Puts the specified instruction and numerical argument onto the Microsoft intermediate language (MSIL)
		/// stream of instructions.
		/// </summary>
		public void Emit(OpCode opcode, double arg)
		{
			if (InterceptEmit(opcode, arg)) il.Emit(opcode, arg);
		}

		/// <summary>
		/// Puts the specified instruction and metadata token for the specified field onto the Microsoft intermediate
		/// language (MSIL) stream of instructions.
		/// </summary>
		public void Emit(OpCode opcode, FieldInfo field)
		{
			if (InterceptEmit(opcode, field)) il.Emit(opcode, field);
		}

		/// <summary>
		/// Puts the specified instruction and numerical argument onto the Microsoft intermediate language (MSIL)
		/// stream of instructions.
		/// </summary>
		public void Emit(OpCode opcode, short arg)
		{
			if (InterceptEmit(opcode, arg)) il.Emit(opcode, arg);
		}

		/// <summary>
		/// Puts the specified instruction and numerical argument onto the Microsoft intermediate language (MSIL)
		/// stream of instructions.
		/// </summary>
		public void Emit(OpCode opcode, int arg)
		{
			if (InterceptEmit(opcode, arg)) il.Emit(opcode, arg);
		}

		/// <summary>
		/// Puts the specified instruction and numerical argument onto the Microsoft intermediate language (MSIL)
		/// stream of instructions.
		/// </summary>
		public void Emit(OpCode opcode, long arg)
		{
			if (InterceptEmit(opcode, arg)) il.Emit(opcode, arg);
		}

		/// <summary>
		/// Puts the specified instruction onto the Microsoft intermediate language (MSIL) stream and leaves space
		/// to include a label when fixes are done.
		/// </summary>
		public void Emit(OpCode opcode, Label label)
		{
			if (InterceptEmit(opcode, label)) il.Emit(opcode, label);
		}

		/// <summary>
		/// Puts the specified instruction onto the Microsoft intermediate language (MSIL) stream and leaves space
		/// to include a label when fixes are done.
		/// </summary>
		public void Emit(OpCode opcode, Label[] labels)
		{
			if (InterceptEmit(opcode, labels)) il.Emit(opcode, labels);
		}

		/// <summary>
		/// Puts the specified instruction onto the Microsoft intermediate language (MSIL) stream followed by the
		/// index of the given local variable.
		/// </summary>
		public void Emit(OpCode opcode, LocalBuilder local)
		{
			if (InterceptEmit(opcode, local)) il.Emit(opcode, local);
		}

		/// <summary>
		/// Puts the specified instruction onto the Microsoft intermediate language (MSIL) stream followed by the
		/// metadata token for the given method.
		/// </summary>
		public void Emit(OpCode opcode, MethodInfo meth)
		{
            //Debug.Assert(opcode != OpCodes.Callvirt || meth.IsVirtual, "Non-Virtual method called as virtual!");
            if (opcode == OpCodes.Callvirt && !meth.IsVirtual)
                opcode = OpCodes.Call;
            
			if (InterceptEmit(opcode, meth)) il.Emit(opcode, meth);
		}

		/// <summary>
		/// Puts the specified instruction and character argument onto the Microsoft intermediate language (MSIL)
		/// stream of instructions.
		/// </summary>
		public void Emit(OpCode opcode, sbyte arg)
		{
			if (InterceptEmit(opcode, arg)) il.Emit(opcode, arg);
		}

#if !SILVERLIGHT
		/// <summary>
		/// Puts the specified instruction and a signature token onto the Microsoft intermediate language (MSIL)
		/// stream of instructions.
		/// </summary>
		public void Emit(OpCode opcode, SignatureHelper signature)
		{
			if (InterceptEmit(opcode, signature)) il.Emit(opcode, signature);
		}
#endif

		/// <summary>
		/// Puts the specified instruction and numerical argument onto the Microsoft intermediate language (MSIL)
		/// stream of instructions.
		/// </summary>
		public void Emit(OpCode opcode, float arg)
		{
			if (InterceptEmit(opcode, arg)) il.Emit(opcode, arg);
		}

		/// <summary>
		/// Puts the specified instruction onto the Microsoft intermediate language (MSIL) stream followed by the
		/// metadata token for the given string.
		/// </summary>
		public void Emit(OpCode opcode, string str)
		{
			if (InterceptEmit(opcode, str)) il.Emit(opcode, str);
		}

		/// <summary>
		/// Puts the specified instruction onto the Microsoft intermediate language (MSIL) stream followed by the
		/// metadata token for the given type.
		/// </summary>
		public void Emit(OpCode opcode, Type cls)
		{
			if (InterceptEmit(opcode, cls)) il.Emit(opcode, cls);
		}

		/// <summary>
		/// Puts a call or callvirt instruction onto the Microsoft intermediate language (MSIL) stream.
		/// </summary>
		public void EmitCall(OpCode opcode, MethodInfo methodInfo, Type[] optionalParameterTypes)
		{
			if (InterceptEmit(opcode, methodInfo)) il.EmitCall(opcode, methodInfo, optionalParameterTypes);
		}

#if !SILVERLIGHT
		/// <summary>
		/// Puts a Calli instruction onto the Microsoft intermediate language (MSIL) stream, specifying an unmanaged
		/// calling convention for the indirect call.
		/// </summary>
		public void EmitCalli(OpCode opcode, CallingConvention unmanagedCallConv, Type returnType, Type[] parameterTypes)
		{
			if (InterceptEmit(opcode, null)) il.EmitCalli(opcode, unmanagedCallConv, returnType, parameterTypes);
		}

		/// <summary>
		/// Puts a Calli instruction onto the Microsoft intermediate language (MSIL) stream, specifying a managed calling
		/// convention for the indirect call.
		/// </summary>
		public void EmitCalli(OpCode opcode, CallingConventions callingConvention, Type returnType, Type[] parameterTypes,
			Type[] optionalParameterTypes)
		{
			if (InterceptEmit(opcode, null)) il.EmitCalli(opcode, callingConvention, returnType, parameterTypes,
												 optionalParameterTypes);
		}
#endif

		#endregion

		#region MarkLabel, ForgetLabel, InterceptEmit

		/// <summary>
		/// Marks the Microsoft intermediate language (MSIL) stream's current position with the given label.
		/// </summary>
		/// <param name="loc">The label for which to set an index.</param>
		/// <param name="forget"><B>true</B> if the label should be forgotten right away, <B>false</B> otherwise.</param>
		/// <remarks>
		/// Intercepting the <see cref="ILGenerator.MarkLabel"/> call is important for control flow analysis.
		/// </remarks>
		public void MarkLabel(Label loc, bool forget)
		{
			isPositionLabeled = true;
			il.MarkLabel(loc);

			if (!forget) branchTargets.Add(loc, null);
			unresolvedBranches.Remove(loc);

			if (featurePoints != null)
			{
				foreach (FeaturePoint point in featurePoints.Values)
				{
					point.MarkLabel(loc);
					if (forget) point.ForgetLabel(loc);
				}
			}

#if DEBUG
			if (forget) forgottenTargets[loc] = null;
#endif
		}

		/// <summary>
		/// Marks the Microsoft intermediate language (MSIL) stream's current position with the given label.
		/// </summary>
		/// <param name="loc">The label for which to set an index.</param>
		/// <remarks>
		/// Intercepting the <see cref="ILGenerator.MarkLabel"/> call is important for control flow analysis.
		/// </remarks>
		public void MarkLabel(Label loc)
		{
			MarkLabel(loc, false);
		}

		/// <summary>
		/// Notifies this <see cref="ILEmitter"/> that there will be no more branches to the given label.
		/// </summary>
		/// <param name="loc">The label that is surely not going to be the target of any consequent branch instruction.
		/// </param>
		public void ForgetLabel(Label loc)
		{
			branchTargets.Remove(loc);

			if (featurePoints != null)
			{
				foreach (FeaturePoint point in featurePoints.Values) point.ForgetLabel(loc);
			}

#if DEBUG
			forgottenTargets[loc] = null;
#endif
		}

		/// <summary>
		/// Intercepts opcode emission.
		/// </summary>
		/// <param name="opcode">The <see cref="OpCode"/> being emitted.</param>
		/// <param name="arg">The operand.</param>
		/// <returns><B>null</B> if the <paramref name="opcode"/> should be emitted, <B>false</B> otherwise.</returns>
		/// <remarks>
		/// This method is consulted before emitting any IL instruction.
		/// </remarks>
		private bool InterceptEmit(OpCode opcode, object arg)
		{
			//Debug.WriteLine("{0} {1}", opcode.ToString(), arg == null ? String.Empty : arg.ToString());

			lastEmittedOpCode = opcode;
			isPositionLabeled = false;

			if (opcode.FlowControl == FlowControl.Branch || opcode.FlowControl == FlowControl.Cond_Branch)
			{
				// the argument is either Label or an array of Labels
				if (arg is Label) InterceptBranch(opcode, (Label)arg);
				else
				{
					Label[] targets = (Label[])arg;
					for (int i = 0; i < targets.Length; i++) InterceptBranch(opcode, targets[i]);
				}
			}

			return true;
		}

		private void InterceptBranch(OpCode opcode, Label target)
		{
#if DEBUG
			Debug.Assert(!forgottenTargets.ContainsKey(target), "Attempt to branch to a forgotten label. " +
				"Invalid control-flow hint was given.");
#endif

			// if the branch target is unknown, remember it
			if (!branchTargets.ContainsKey(target)) unresolvedBranches[target] = null;

			if (OpCodes.Leave.Equals(opcode) || OpCodes.Leave_S.Equals(opcode))
			{
				// we do not do detailed exception handling control flow analysis now
				if (featurePoints != null) featurePoints.Clear();
			}
		}

		#endregion

		#region MarkFeature, IsFeatureControlFlowPrecedent

		/// <summary>
		/// Marks the Microsoft intermediate language (MSIL) stream's current position as a distinguished control flow point
		/// related to a &quot;feature&quot;.
		/// </summary>
		/// <param name="featureId">An arbitrary non-<B>null</B> reference.</param>
		/// <remarks><seealso cref="IsFeatureControlFlowPrecedent"/></remarks>
		public void MarkFeature(object featureId)
		{
			if (featureId == null) throw new ArgumentNullException("featureId");

			if (featurePoints == null) featurePoints = new Dictionary<object, FeaturePoint>();
			featurePoints[featureId] = new FeaturePoint(unresolvedBranches);
		}

		/// <summary>
		/// Determines whether the Microsoft intermediate language (MSIL) stream's current position is unconditionally
		/// preceded by a previously marked &quot;feature&quot;.
		/// </summary>
		/// <param name="featureId">The feature ID passed earlier to <see cref="MarkFeature"/> (non-<B>null</B>).</param>
		/// <returns><B>null</B> if current position surely uncoditionally follows the position identified by
		/// <paramref name="featureId"/>, <B>false</B> otherwise.</returns>
		/// <remarks>
		/// <para>
		/// This method returns <B>null</B> if it is possible to prove that whenever IL execution reaches current position,
		/// it has already reached the position marked by <see cref="MarkFeature"/>(<paramref name="featureId"/>).
		/// </para>
		/// <para>
		/// These are the sufficient conditions used by this implementation (inclusive or):
		/// <list type="bullet">
		/// <item>All branch instructions before the feature point have their targets before the feature point, and there
		/// is no <see cref="OpCodes.Leave"/> instruction between the feature point and current position.</item>
		/// <item>There is no branch target between the feature point and current position that is referenced before the
		/// feature point or left open (unforgotten) at current positionn and there is no <see cref="OpCodes.Leave"/>
		/// instruction between the feature point and current position.</item>
		/// </list>
		/// </para>
		/// </remarks>
		public bool IsFeatureControlFlowPrecedent(object featureId)
		{
			if (featureId == null) throw new ArgumentNullException("featureId");

			if (featurePoints == null) return false;

			FeaturePoint point;
			return (featurePoints.TryGetValue(featureId, out point) && point.IsControlFlowPrecedent);
		}

		#endregion

		#region GetTemporaryLocal

		/// <summary>
		/// Returns a <see cref="LocalBuilder"/> of a temporary local variable of a specified <see cref="Type"/>.
		/// </summary>
		/// <param name="type">The requested <see cref="Type"/> of the local.</param>
		/// <param name="immediateReturn"><B>True</B> to immediately return the local builder to the pool of locals
		/// available for reuse (no need to call <see cref="ReturnTemporaryLocal"/>).</param>
		/// <returns>The <see cref="LocalBuilder"/>.</returns>
		/// <remarks>
		/// If a <see cref="LocalBuilder"/> of the given <see cref="Type"/> has already been declared and returned
		/// to the pool, this local is reused. Otherwise, a new local is declared. Use this method to obtain a
		/// short-lived temporary local. If <paramref name="immediateReturn"/> is <B>false</B>, return the local
		/// to the pool of locals available for reuse by calling <see cref="ReturnTemporaryLocal"/>.
		/// </remarks>
		public LocalBuilder/*!*/ GetTemporaryLocal(Type/*!*/ type, bool immediateReturn)
		{
			if (temporaryLocals != null)
				for (LinkedListNode<LocalBuilder> node = temporaryLocals.First; node != null; node = node.Next)
					if (node.Value.LocalType == type)
					{
						if (!immediateReturn) temporaryLocals.Remove(node);
						return node.Value;
					}
			
			LocalBuilder builder = DeclareLocal(type);
			if (immediateReturn) ReturnTemporaryLocal(builder);

			return builder;
		}

		/// <summary>
		/// Returns a <see cref="LocalBuilder"/> of a temporary local variable of a specified <see cref="Type"/>.
		/// </summary>
		/// <param name="type">The requested <see cref="Type"/> of the local.</param>
		/// <returns>The <see cref="LocalBuilder"/>.</returns>
		/// <remarks>
		/// If a <see cref="LocalBuilder"/> of the given <see cref="Type"/> has already been declared and returned
		/// to the pool, this local is reused. Otherwise, a new local is declared. Use this method to obtain a
		/// short-lived temporary local. Return the local to the pool of locals available for reuse by calling
		/// <see cref="ReturnTemporaryLocal"/>.
		/// </remarks>
		public LocalBuilder/*!*/ GetTemporaryLocal(Type/*!*/ type)
		{
			return GetTemporaryLocal(type, false);
		}

		/// <summary>
		/// Returns a <see cref="LocalBuilder"/> previously obtained from <see cref="GetTemporaryLocal"/> to the
		/// pool of locals available for reuse.
		/// </summary>
		/// <param name="builder">The <see cref="LocalBuilder"/> to return to the pool.</param>
		public void ReturnTemporaryLocal(LocalBuilder/*!*/ builder)
		{
			Debug.Assert(builder != null);

			if (temporaryLocals == null) temporaryLocals = new LinkedList<LocalBuilder>();

			// try to do it LIFO 
			temporaryLocals.AddFirst(builder);
		}

		#endregion

		#region Patterns

		/// <summary>
		/// Emits the most efficient form of the <c>ldc_i4</c> IL instruction.
		/// </summary>
		/// <param name="i">The constant to be loaded.</param>
		public void LdcI4(int i)
		{
			switch (i)
			{
				case -1: Emit(OpCodes.Ldc_I4_M1); break;
				case 0: Emit(OpCodes.Ldc_I4_0); break;
				case 1: Emit(OpCodes.Ldc_I4_1); break;
				case 2: Emit(OpCodes.Ldc_I4_2); break;
				case 3: Emit(OpCodes.Ldc_I4_3); break;
				case 4: Emit(OpCodes.Ldc_I4_4); break;
				case 5: Emit(OpCodes.Ldc_I4_5); break;
				case 6: Emit(OpCodes.Ldc_I4_6); break;
				case 7: Emit(OpCodes.Ldc_I4_7); break;
				case 8: Emit(OpCodes.Ldc_I4_8); break;
				default:
					if (i >= SByte.MinValue && i <= SByte.MaxValue)
						Emit(OpCodes.Ldc_I4_S, (sbyte)i);
					else
						Emit(OpCodes.Ldc_I4, i);
					break;
			}
		}

		/// <summary>
		/// Emits the most efficient form of loading 64bit integer on evaluation stack.
		/// </summary>
		public void LdcI8(long i)
		{
			if (i >= int.MinValue && i <= int.MaxValue)
			{
				LdcI4((int)i);
				Emit(OpCodes.Conv_I8);
			}
			else
				Emit(OpCodes.Ldc_I8, i);
		}

		/// <summary>
		/// Emits the most efficient form of <c>ldloc</c> IL instruction.
		/// </summary>
		/// <param name="i">The index of a local variable to be loaded.</param>
		public void Ldloc(int i)
		{
			switch (i)
			{
				case 0: Emit(OpCodes.Ldloc_0); break;
				case 1: Emit(OpCodes.Ldloc_1); break;
				case 2: Emit(OpCodes.Ldloc_2); break;
				case 3: Emit(OpCodes.Ldloc_3); break;
				default:
					if (i >= SByte.MinValue && i <= SByte.MaxValue)
						Emit(OpCodes.Ldloc_S, (sbyte)i);
					else
						Emit(OpCodes.Ldloc, (short)i);
					break;
			}
		}

		/// <summary>
		/// Emits the most efficient form of <c>ldloc</c> IL instruction.
		/// </summary>
		/// <param name="local">The local variable to be loaded.</param>
		public void Ldloc(LocalBuilder/*!*/ local)
		{
			Ldloc(local.LocalIndex);
		}

		/// <summary>
		/// Emits the most efficient form of the <c>ldloca</c> IL instruction.
		/// </summary>
		/// <param name="i">The index of a local variable whose address to load.</param>
		public void Ldloca(int i)
		{
			if (i >= Byte.MinValue && i <= Byte.MaxValue)
				Emit(OpCodes.Ldloca_S, (byte)i);
			else
				Emit(OpCodes.Ldloca, (short)i);
		}

		/// <summary>
		/// Emits the most efficient form of the <c>ldloca</c> IL instruction.
		/// </summary>
		/// <param name="local">The local variable whose address to load.</param>
		public void Ldloca(LocalBuilder/*!*/ local)
		{
			Ldloca(local.LocalIndex);
		}

		/// <summary>
		/// Emits the most efficient form of the <c>ldarg</c> IL instruction.
		/// </summary>
		/// <param name="i">The index of an argument to be loaded.</param>
		public void Ldarg(int i)
		{
			switch (i)
			{
				case 0: Emit(OpCodes.Ldarg_0); break;
				case 1: Emit(OpCodes.Ldarg_1); break;
				case 2: Emit(OpCodes.Ldarg_2); break;
				case 3: Emit(OpCodes.Ldarg_3); break;
				default:
					if (i >= SByte.MinValue && i <= SByte.MaxValue)
						Emit(OpCodes.Ldarg_S, (sbyte)i);
					else
						Emit(OpCodes.Ldarg, (short)i);
					break;
			}
		}

		/// <summary>
		/// Emits the most efficient form of the <c>ldarga</c> IL instruction.
		/// </summary>
		/// <param name="i">The index of an argument whose address to load.</param>
		public void Ldarga(int i)
		{
			if (i >= Byte.MinValue && i <= Byte.MaxValue)
				Emit(OpCodes.Ldarga_S, (byte)i);
			else
				Emit(OpCodes.Ldarga, (short)i);
		}

		/// <summary>
		/// Emits the most efficient form of the <c>starg</c> IL instruction.
		/// </summary>
		/// <param name="i">The index of argument to be stored.</param>
		public void Starg(int i)
		{
			if (i >= Byte.MinValue && i <= Byte.MaxValue)
				Emit(OpCodes.Starg_S, (byte)i);
			else
				Emit(OpCodes.Starg, (short)i);
		}

		/// <summary>
		/// Emits the most efficient form of the <c>stloc</c> IL instruction.
		/// </summary>
		/// <param name="i">The index of a local variable to be stored.</param>
		public void Stloc(int i)
		{
			switch (i)
			{
				case 0: Emit(OpCodes.Stloc_0); break;
				case 1: Emit(OpCodes.Stloc_1); break;
				case 2: Emit(OpCodes.Stloc_2); break;
				case 3: Emit(OpCodes.Stloc_3); break;
				default:
					if (i >= SByte.MinValue && i <= SByte.MaxValue)
						Emit(OpCodes.Stloc_S, (sbyte)i);
					else
						Emit(OpCodes.Stloc, (short)i);
					break;
			}
		}

		/// <summary>
		/// Emits the most efficient form of the <c>stloc</c> IL instruction.
		/// </summary>
		/// <param name="local">The local variable to be stored.</param>
		public void Stloc(LocalBuilder/*!*/ local)
		{
			Stloc(local.LocalIndex);
		}

		/// <summary>
		/// Dereferences an address on the stack by emitting one of <c>Ldind</c> or <c>Ldobj</c>.
		/// </summary>
		/// <param name="type">Type of the referenced value.</param>
		public void Ldind(Type/*!*/ type)
		{
			Debug.Assert(!type.IsByRef);

			if (type.IsGenericParameter)
			{
				il.Emit(OpCodes.Ldobj, type);
			}
			else if (type.IsValueType)
			{
				switch (Type.GetTypeCode(type))
				{
					// signed ints:
					case TypeCode.SByte:
					case TypeCode.Boolean: Emit(OpCodes.Ldind_I1); break;

					case TypeCode.Int16: Emit(OpCodes.Ldind_I2); break;

					case TypeCode.Int32: Emit(OpCodes.Ldind_I4); break;

					case TypeCode.Int64:
					case TypeCode.UInt64: Emit(OpCodes.Ldind_I8); break;

					// unsigned ints:
					case TypeCode.Byte: Emit(OpCodes.Ldind_U1); break;

					case TypeCode.UInt16:
					case TypeCode.Char: Emit(OpCodes.Ldind_U2); break;

					case TypeCode.UInt32: Emit(OpCodes.Ldind_U4); break;

					// floats:
					case TypeCode.Single: Emit(OpCodes.Ldind_R4); break;

					case TypeCode.Double: Emit(OpCodes.Ldind_R8); break;

					// other value types:
					default:
						{
							if (type == typeof(IntPtr)) Emit(OpCodes.Ldind_I);
							else Emit(OpCodes.Ldobj, type);
							break;
						}
				}
			}
			else Emit(OpCodes.Ldind_Ref);
		}

		/// <summary>
		/// Stores a value to an address on the stack by emitting one of <c>Stind</c> or <c>Stobj</c>.
		/// </summary>
		/// <param name="type">Type of the referenced value.</param>
		public void Stind(Type/*!*/ type)
		{
			Debug.Assert(!type.IsByRef);

			if (type.IsGenericParameter)
			{
				Emit(OpCodes.Stobj, type);
			}
			else if (type.IsValueType)
			{
				switch (Type.GetTypeCode(type))
				{
					// ints:
					case TypeCode.Byte:
					case TypeCode.SByte:
					case TypeCode.Boolean: Emit(OpCodes.Stind_I1); break;

					case TypeCode.Int16:
					case TypeCode.Char: Emit(OpCodes.Stind_I2); break;

					case TypeCode.Int32:
					case TypeCode.UInt32: Emit(OpCodes.Stind_I4); break;

					case TypeCode.Int64:
					case TypeCode.UInt64: Emit(OpCodes.Stind_I8); break;

					// floats:
					case TypeCode.Single: Emit(OpCodes.Stind_R4); break;

					case TypeCode.Double: Emit(OpCodes.Stind_R8); break;

					// other value types:
					default:
						{
							if (type == typeof(IntPtr)) Emit(OpCodes.Stind_I);
							else Emit(OpCodes.Stobj, type);
							break;
						}
				}
			}
			else Emit(OpCodes.Stind_Ref);
		}

		/// <summary>
		/// Converts value on the stack to the provided <paramref name="type"/> using a <c>conv</c> or
		/// <c>conv.ovf</c> instruction variant.
		/// </summary>
		/// <param name="type">The target type.</param>
		/// <param name="overflow"><B>True</B> if a <c>ovf</c> variant should be used.</param>
		public void Conv(Type/*!*/ type, bool overflow)
		{
			switch (Type.GetTypeCode(type))
			{
				case TypeCode.Boolean:
				case TypeCode.Byte: Emit(overflow ? OpCodes.Conv_Ovf_I1 : OpCodes.Conv_I1); break;
				case TypeCode.SByte: Emit(overflow ? OpCodes.Conv_Ovf_U1 : OpCodes.Conv_U1); break;

				case TypeCode.Char:
				case TypeCode.Int16: Emit(overflow ? OpCodes.Conv_Ovf_I2 : OpCodes.Conv_I2); break;
				case TypeCode.UInt16: Emit(overflow ? OpCodes.Conv_Ovf_U2 : OpCodes.Conv_U2); break;

				case TypeCode.Int32: Emit(overflow ? OpCodes.Conv_Ovf_I4 : OpCodes.Conv_I4); break;
				case TypeCode.UInt32: Emit(overflow ? OpCodes.Conv_Ovf_U4 : OpCodes.Conv_U4); break;

				case TypeCode.Int64: Emit(overflow ? OpCodes.Conv_Ovf_I8 : OpCodes.Conv_I8); break;
				case TypeCode.UInt64: Emit(overflow ? OpCodes.Conv_Ovf_U8 : OpCodes.Conv_U8); break;

				case TypeCode.Single: Emit(OpCodes.Conv_R4); break;
				case TypeCode.Double: Emit(OpCodes.Conv_R8); break;

				default:
					{
						if (type == typeof(IntPtr)) Emit(overflow ? OpCodes.Conv_Ovf_I : OpCodes.Conv_I);
						else Debug.Fail();
						break;
					}
			}
		}

		/// <summary>
		/// Loads a literal to the evaluation stack.
		/// </summary>
		/// <param name="value">The value to be loaded. Should be of literal type.</param>
		/// <returns>The type of the <paramref name="value"/>.</returns>
		/// <exception cref="ArgumentException">Invalid <paramref name="value"/> type.</exception>
		public Type LoadLiteral(object value)
		{
			return LoadLiteral(value, false);
		}

		public Type LoadLiteralBox(object value)
		{
			return LoadLiteral(value, true);
		}

		public Type LoadLiteral(object value, bool box)
		{
			if (value == null)
			{
				Emit(OpCodes.Ldnull);
				return typeof(object);
			}

			Type type = value.GetType();
			switch (Type.GetTypeCode(type))
			{
				case TypeCode.SByte: LdcI4((sbyte)value); break;
				case TypeCode.Byte: LdcI4((byte)value); break;
				case TypeCode.Int16: LdcI8((short)value); break;
				case TypeCode.UInt16: LdcI8((ushort)value); break;
				case TypeCode.Char: LdcI4((int)(char)value); break;
				case TypeCode.Int32: LdcI4((int)value); break;
				case TypeCode.UInt32: LdcI4(unchecked((int)(uint)value)); break;
				case TypeCode.Int64: LdcI8((long)value); break;
				case TypeCode.UInt64: LdcI8(unchecked((long)(ulong)value)); break;
				case TypeCode.Boolean: LoadBool((bool)value); break;
				case TypeCode.Double: Emit(OpCodes.Ldc_R8, (double)value); break;
				case TypeCode.Single: Emit(OpCodes.Ldc_R4, (float)value); break;
				case TypeCode.String:
                    if (PluginHandler.StringLiteralEmitter != null) PluginHandler.StringLiteralEmitter(this, (string)value);
                    else Emit(OpCodes.Ldstr, (string)value);
                    break;
				case TypeCode.Object:
					{
						PhpBytes bytes = value as PhpBytes;
						if (bytes != null)
						{
							EmitLoadPhpBytes(bytes);
							break;
						}
						goto default;
					}

				default:
					throw new ArgumentException("value");
			}

			if (type.IsValueType && box)
				il.Emit(OpCodes.Box, type);

			return type;
		}

        internal FieldBuilder/*!*/DefineInitializedData(string name, byte[] data, FieldAttributes attributes)
        {
            // TODO: cache values, reuse existing PhpBytes or datafld
            
            // regular function, we have a type builder:
            if (TypeBuilder != null)
                return TypeBuilder.DefineInitializedData(name, data, attributes);

            // global function in pure mode:
            var moduleBuilder = this.MethodBuilder.Module as ModuleBuilder;
            if (moduleBuilder != null)
                return moduleBuilder.DefineInitializedData(name, data, attributes);

            //
            throw new NotImplementedException();
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <remarks>TODO: move to CodeGenerator.</remarks>
		internal void EmitLoadPhpBytes(PhpBytes/*!*/ value)
		{
            Debug.Assert(value != null);

            // create array of bytes
            LdcI4(value.Length);
            Emit(OpCodes.Newarr, typeof(byte));

            if (value.Length > 0)   // not valid for zero-length byte arrays
            {
                FieldBuilder datafld = this.DefineInitializedData(
                        string.Concat("byte'", value.ReadonlyData.Length.ToString("x"), "'", value.ReadonlyData.GetHashCode().ToString()),
                        value.ReadonlyData,
                        FieldAttributes.Assembly | FieldAttributes.Static);

                Emit(OpCodes.Dup);
                Emit(OpCodes.Ldtoken, datafld);
                Emit(OpCodes.Call, Methods.InitializeArray);
            }

			Emit(OpCodes.Newobj, Constructors.PhpBytes_ByteArray);
		}

		/// <summary>
		/// Loads a bool literal on the evaluation stack.
		/// </summary>
		/// <param name="value">The literal to load.</param>
		public void LoadBool(bool value)
		{
			Emit(value ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
		}

		/// <summary>
		/// Loads local variable, field, place or emits parameterless opcode.
		/// </summary>
		/// <param name="source">
		/// An instance of <see cref="LocalBuilder"/>, <see cref="FieldInfo"/> or <see cref="IPlace"/>.
		/// </param>
		public void Load(object source)
		{
			LocalBuilder local;
			FieldInfo field;
			IPlace place;
			MethodInfo method;
			PropertyInfo property;
			//Type type;

			if ((local = source as LocalBuilder) != null)
			{
				Ldloc(local);
			}
			else if ((field = source as FieldInfo) != null)
			{
				Emit(field.IsStatic ? OpCodes.Ldsfld : OpCodes.Ldfld, field);
			}
			else if ((place = source as IPlace) != null)
			{
				place.EmitLoad(this);
			}
			else if ((method = source as MethodInfo) != null)
			{
				Debug.Assert(method.GetParameters().Length == 0 && method.IsStatic);
				Emit(OpCodes.Call, method);
			}
			else if ((property = source as PropertyInfo) != null)
			{
				method = property.GetGetMethod();
				Emit(method.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, method);
			}
			else
			{
				throw new ArgumentException("source");
			}
		}

		/// <summary>
		/// Loads address of local variable, field or place.
		/// </summary>
		/// <param name="source">
		/// An instance of <see cref="LocalBuilder"/>, <see cref="FieldInfo"/> or <see cref="IPlace"/>.
		/// </param>
		public void LoadAddress(object source)
		{
			LocalBuilder local;
			FieldInfo field;
			IPlace place;

			if ((local = source as LocalBuilder) != null)
			{
				Ldloca(local);
			}
			else
				if ((field = source as FieldInfo) != null)
				{
					Emit(field.IsStatic ? OpCodes.Ldsflda : OpCodes.Ldflda, field);
				}
				else
					if ((place = source as IPlace) != null)
					{
						place.EmitLoadAddress(this);
					}
					else
					{
						throw new ArgumentException("source");
					}
		}

		/// <summary>
		/// Gets whether the place has an address.
		/// </summary>
		/// <param name="source">
		/// An instance of <see cref="LocalBuilder"/>, <see cref="FieldInfo"/> or <see cref="IPlace"/>.
		/// </param>
		/// <returns>Whether the place is addressable.</returns>
		public static bool HasAddress(object source)
		{
			IPlace place;

			if (source is LocalBuilder || source is FieldInfo)
			{
				return true;
			}
			else if ((place = source as IPlace) != null)
			{
				return place.HasAddress;
			}
			else
			{
				return false;
			}
		}

		/// <summary>
		/// Stores local variable, field, place or emits paramereless opcode.
		/// </summary>
		/// <param name="destination">
		/// An instance of <see cref="LocalBuilder"/>, <see cref="FieldInfo"/> or <see cref="IPlace"/>.
		/// </param>
		public void Store(object destination)
		{
			LocalBuilder local;
			FieldInfo field;
			IPlace place;

			if ((local = destination as LocalBuilder) != null)
			{
				Stloc(local);
			}
			else
				if ((field = destination as FieldInfo) != null)
				{
					Emit(field.IsStatic ? OpCodes.Stsfld : OpCodes.Stfld, field);
				}
				else
					if ((place = destination as IPlace) != null)
					{
						place.EmitStore(this);
					}
					else
					{
						throw new ArgumentException("destination");
					}
		}

		/// <summary>
		/// Emits oprator ++ on local variable.
		/// </summary>
		/// <param name="local">The local variable builder.</param>
		/// <remarks>The original value of the local variable remains on the stack.</remarks>
		public void IncLoc(LocalBuilder local)
		{
			Ldloc(local);
			Emit(OpCodes.Dup);
			Emit(OpCodes.Ldc_I4_1);
			Emit(OpCodes.Add);
			Stloc(local);
		}

		/// <summary>
		/// Emits comparison of the top of the stack (TOS) with <B>null</B>.
		/// </summary>
		/// <remarks>
		/// Loads a boolean <B>true</B> (TOS is not equal to <B>null</B>) or <B>false</B> 
		/// (TOS is equal to <B>null</B>) on the top of the stack.
		/// </remarks>
		public void CmpNotNull()
		{
			il.Emit(OpCodes.Ldnull);
			il.Emit(OpCodes.Ceq);
			il.Emit(OpCodes.Ldc_I4_0);
			il.Emit(OpCodes.Ceq);
		}

		/// <summary>
		/// Stores an element into the array which should be on the top of the evaluation stack.
		/// </summary>
		/// <param name="type">The type of the element.</param>
		/// <exception cref="ArgumentException">Invalid <paramref name="type"/>.</exception>
		public void Stelem(Type/*!*/ type)
		{
			if (type.IsGenericParameter)
			{
				Emit(OpCodes.Stelem);
			}
			else
			{
				if (type.IsValueType)
				{
					switch (Type.GetTypeCode(type))
					{
						case TypeCode.Byte:
						case TypeCode.SByte: Emit(OpCodes.Stelem_I1); break;
						case TypeCode.Char:
						case TypeCode.UInt16:
						case TypeCode.Int16: Emit(OpCodes.Stelem_I2); break;
						case TypeCode.UInt32:
						case TypeCode.Int32: Emit(OpCodes.Stelem_I4); break;
						case TypeCode.UInt64:
						case TypeCode.Int64: Emit(OpCodes.Stelem_I8); break;
						case TypeCode.Double: Emit(OpCodes.Stelem_R8); break;
						case TypeCode.Single: Emit(OpCodes.Stelem_R4); break;
						default:
							throw new ArgumentException("type");
					}
				}
				else
				{
					Emit(OpCodes.Stelem_Ref);
				}
			}
		}

		/// <summary>
		/// Loads a value from a specified place on the evaluation stack and boxes it if it is of a value type.
		/// </summary>
		/// <param name="place">The place where to load a value from.</param>
		public void LoadBoxed(IPlace/*!*/ place)
		{
			Type type = place.PlaceType;

			place.EmitLoad(this);
			if (type.IsValueType)
				il.Emit(OpCodes.Box, type);
		}

		/// <summary>
		/// Emits a new vector creation and initialization. The resulting array is pushed onto the top of evaluation stack.
		/// </summary>
		/// <param name="elementType">Element type.</param>
		/// <param name="length">Array length.</param>
		/// <param name="emitItem">Action emitting an array item.</param>
		public void EmitInitializedArray(Type/*!*/ elementType, int length, Action<ILEmitter, int>/*!*/ emitItem)
		{
			Debug.Assert(elementType != null && emitItem != null);

			//LocalBuilder tmp = GetTemporaryLocal(elementType.MakeArrayType());

			// tmp = new string[<length>];
			LdcI4(length);
			Emit(OpCodes.Newarr, elementType);
			//Stloc(tmp);

			for (int i = 0; i < length; ++i)
			{
				// tmp[<i>] = <item[i]>;
                il.Emit(OpCodes.Dup);//Ldloc(tmp);
				LdcI4(i);
				emitItem(this, i);
				Stelem(elementType);
			}

			//return tmp;
		}
        /// <summary>
        /// Emits a new vector creation and initialization. The resulting array is on the top of the stack. Not stored in any local variable!
        /// </summary>
        /// <param name="elementType">Element type.</param>
        /// <param name="length">Array length.</param>
        /// <param name="emitItem">Action emitting an array item.</param>
        public void EmitLoadInitializedArray(Type/*!*/ elementType, int length, Action<ILEmitter, int>/*!*/ emitItem)
        {
            Debug.Assert(elementType != null && emitItem != null);

            // new type[<length>];
            LdcI4(length);
            Emit(OpCodes.Newarr, elementType);
            
            for (int i = 0; i < length; ++i)
            {
                // tmp[<i>] = <item[i]>;
                Emit(OpCodes.Dup);
                LdcI4(i);
                emitItem(this, i);
                Stelem(elementType);
            }
        }

        /*
        /// <summary>
        /// Emits a new vector of byte creation and initialization. The resulting array is pushed onto the top of the evaluation stack.
        /// </summary>
        /// <param name="data">source array to be used</param>
        /// <returns>The resulting array is pushed onto the top of the evaluation stack.</returns>
        public void EmitInitializedArray(byte[] data)
        {
            Debug.Assert(data != null);

            //LocalBuilder tmp = GetTemporaryLocal(typeof(byte[]));

            // tmp = new byte[data.length];
            LdcI4(data.Length);
            Emit(OpCodes.Newarr, typeof(byte));
            //Stloc(tmp);

            for (int i = 0; i < data.Length; ++i)
            {
                // tmp[<i>] = data[i];
                il.Emit(OpCodes.Dup);//Ldloc(tmp);
                LdcI4(i);
                LdcI4(data[i]);
                Emit(OpCodes.Stelem_I1);    
            }

            //return tmp;
        }*/

		/// <summary>
		/// Emits either an array of items or the items themselves depending on their number.
		/// Useful for emitting arguments of optimized overloads.
		/// </summary>
		/// <param name="elementType">Arguments type.</param>
		/// <param name="argCount">Number of actual arguments to be emitted.</param>
		/// <param name="explicitOverloads">Maximal number of arguments for which an explicit overload exists.</param>
		/// <param name="emitArg">Argument emitter.</param>
		public void EmitOverloadedArgs(Type/*!*/ elementType, int argCount, int explicitOverloads,
			Action<ILEmitter, int>/*!*/ emitArg)
		{
			Debug.Assert(elementType != null && emitArg != null);

			if (argCount > explicitOverloads)
			{
				// emit array:
				EmitInitializedArray(elementType, argCount, emitArg);
			}
			else
			{
				// emit separate arguments:
				for (int i = 0; i < argCount; i++)
				{
					emitArg(this, i);
				}
			}
		}

		#endregion

		#region GetAssignmentLocal

		/// <summary>
		/// Holds a <see cref="LocalBuilder"/> used as a storage for a source value of an assignment.
		/// </summary>
		private LocalBuilder assignmentLocalBuilder;
		private LocalBuilder assignmentLocalBuilderRef;

		/// <summary>
		/// Returns a <see cref="LocalBuilder"/> used as a storage for a source value of an assignment.
		/// </summary>
		/// <returns>The <see cref="LocalBuilder"/>.</returns>
		/// <remarks>
		/// Returns a <see cref="LocalBuilder"/> of type <see cref="System.Object"/>. The local is declared
		/// with the first call of this method.
		/// </remarks>
		public LocalBuilder GetAssignmentLocal()
		{
			if (this.assignmentLocalBuilder == null)
				assignmentLocalBuilder = il.DeclareLocal(typeof(object));
			return assignmentLocalBuilder;
		}

		public LocalBuilder GetAssignmentLocalRef()
		{
			if (this.assignmentLocalBuilderRef == null)
				assignmentLocalBuilderRef = il.DeclareLocal(typeof(PhpReference));
			return assignmentLocalBuilderRef;
		}

		#endregion

		#region GetAddressStorageLocal

		// Obsolete:
		// <summary>
		// A <see cref="Stack{T}"/> class that stores temporarily used variables. See Remarks for more information.
		// </summary>
		// <remarks>This stack stores locals that are used to obtain address of a variable stored in
		// a runtime variables table while calling methods from <see cref="PHP.Core.Operators"/> having
		// a <c>ref</c> argument. Those variables are not so short-live to be obtained by <see cref="GetTemporaryLocal"/>,
		// but can be reused within a defining scope under certain circumstances.
		// When <see cref="GetAddressStorageLocal"/> method is called, the temporary
		// local is either popped from the cache or a new local is defined if the cache is empty.
		// If the variable become useless, <see cref="ReturnAddressStorageLocal"/> method should
		// be called to push the variable back to cache. Once the variable is returned to cache it must not have
		// been used unless it is obtained again by <see cref="GetAddressStorageLocal"/> method.
		// The cache is created when the first local is returned.
		// </remarks>

		#endregion

		#region PHP Specific

		internal void EmitBoxing(PhpTypeCode type)
		{
			switch (type)
			{
				case PhpTypeCode.Integer:
					il.Emit(OpCodes.Box, typeof(Int32));
					break;

				case PhpTypeCode.LongInteger:
					il.Emit(OpCodes.Box, typeof(Int64));
					break;

				case PhpTypeCode.Double:
					il.Emit(OpCodes.Box, typeof(Double));
					break;

				case PhpTypeCode.Boolean:
					il.Emit(OpCodes.Box, typeof(Boolean));
					break;

				case PhpTypeCode.Void:
					il.Emit(OpCodes.Ldnull);
					break;
			}
		}

        /// <summary>
        /// UnBox object containing value-type.
        /// </summary>
        /// <param name="type">Type of object to UnBox.</param>
        /// <remarks>Extracts the value contained within obj (of type O), it is equivalent to unbox followed by ldobj.</remarks>
        internal void EmitUnboxingForArg(PhpTypeCode type)
        {
            switch (type)
            {
                case PhpTypeCode.Integer:
                    il.Emit(OpCodes.Unbox_Any, typeof(Int32));
                    break;

                case PhpTypeCode.LongInteger:
                    il.Emit(OpCodes.Unbox_Any, typeof(Int64));
                    break;

                case PhpTypeCode.Double:
                    il.Emit(OpCodes.Unbox_Any, typeof(Double));
                    break;

                case PhpTypeCode.Boolean:
                    il.Emit(OpCodes.Unbox_Any, typeof(Boolean));
                    break;

                case PhpTypeCode.Void:
                    il.Emit(OpCodes.Pop);
                    break;
            }
        }

		#endregion
	}
}
