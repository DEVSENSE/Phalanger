/*

  Copyright (c) 2003-2006 Tomas Matousek, Ladislav Prosek 
  Copyright (c) 2003-2004 Martin Maly.

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
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.SymbolStore;
using PHP.Core.AST;
using PHP.Core.Emit;
using PHP.Core.Parsers;
using PHP.Core.Reflection;

namespace PHP.Core
{
	internal delegate void AssignmentCallback(CodeGenerator/*!*/ codeGenerator, PhpTypeCode stackTypeCode);

	/// <summary>
	/// Manage the process of code generation.
	/// </summary>
	internal sealed class CodeGenerator : AstVisitor
	{
		#region Fields and Properties

		/// <summary>
		/// <see cref="PHP.Core.Emit.ILEmitter"/> currently used to emit IL instructions.
		/// </summary>
		public ILEmitter IL { get { return il; } set { il = value; } }
		private ILEmitter il;

		public LinqBuilder LinqBuilder { get { return linqBuilder; } set { linqBuilder = value; } }
		private LinqBuilder linqBuilder;

		/// <summary>
		/// The current compilation context.
		/// </summary>
		public override CompilationContext Context { get { return context; } }
		private CompilationContext context;

		/// <summary>
		/// Current source unit. Switched by <see cref="GlobalCode"/>.
        /// Internally modifiable in order to change the sourceUnit during the emission of methods/fields in partial classes.
		/// </summary>
        public SourceUnit SourceUnit { get { return sourceUnit; } internal set { sourceUnit = value; } }
		private SourceUnit sourceUnit;

		public CompilationUnitBase CompilationUnit { get { return sourceUnit.CompilationUnit; } }

		/// <summary>
		/// Gets a stack that stores information that has to be shared by more AST nodes.
		/// </summary>
		internal CompilerLocationStack LocationStack { get { return locationStack; } }
		private CompilerLocationStack locationStack;

		/// <summary>
		/// Gets a stack used for branching statements (<B>break</B>, <B>continue</B>).
		/// </summary>
		internal BranchingStack BranchingStack { get { return branchingStack; } }
		private BranchingStack branchingStack;

		/// <summary>
		/// A builder used for generating chained operators.
		/// </summary>
		internal ChainBuilder ChainBuilder { get { return chainBuilder; } }
		private ChainBuilder chainBuilder;

		/// <summary>
		/// Whether the current function returns by reference.
		/// Used by return statement. Set in enter/leave declaration.
		/// </summary>
		internal bool ReturnsPhpReference;

		/// <summary>
		/// Number of nested exception blocks (try and catch blocks are not distinguished).
		/// </summary>
		internal int ExceptionBlockNestingLevel;

		/// <summary>
		/// Gets or sets <see cref="PHP.Core.VariablesTable"/> currently used during code emission. 
		/// Holds variables defined in the current local scope.
		/// </summary>
		public VariablesTable CurrentVariablesTable { get { return currentVariablesTable; } }
		private VariablesTable currentVariablesTable;

		public Dictionary<VariableName, Statement> CurrentLabels { get { return currentLabels; } }
		private Dictionary<VariableName, Statement> currentLabels;

		/// <summary>
		/// Gets or sets the type of access operation to be performed on nodes that has multiple access types.
		/// </summary>
		public AccessType AccessSelector { get { return accessSelector; } set { accessSelector = value; } }
		private AccessType accessSelector = AccessType.None;

		/// <summary>
		/// Selects actual access type using the current value of <see cref="AccessSelector"/>.
		/// </summary>
		internal AccessType SelectAccess(AccessType access)
		{
			switch (access)
			{
				case AccessType.ReadAndWrite:
					if (accessSelector == AccessType.Read) return AccessType.Read;
					if (accessSelector == AccessType.Write) return AccessType.Write;
					break;

				case AccessType.WriteAndReadRef:
					if (accessSelector == AccessType.Write) return AccessType.Write;
					if (accessSelector == AccessType.ReadRef) return AccessType.ReadRef;
					break;

				case AccessType.WriteAndReadUnknown:
					if (accessSelector == AccessType.Write) return AccessType.Write;
					if (accessSelector == AccessType.ReadUnknown) return AccessType.ReadUnknown;
					break;

				case AccessType.ReadAndWriteAndReadRef:
					if (accessSelector == AccessType.Read) return AccessType.Read;
					if (accessSelector == AccessType.Write) return AccessType.Write;
					if (accessSelector == AccessType.ReadRef) return AccessType.ReadRef;
					break;

				case AccessType.ReadAndWriteAndReadUnknown:
					if (accessSelector == AccessType.Read) return AccessType.Read;
					if (accessSelector == AccessType.Write) return AccessType.Write;
					if (accessSelector == AccessType.ReadUnknown) return AccessType.ReadUnknown;
					break;

				default:
					return access;
			}
			Debug.Fail("Invalid access selector");
			return AccessType.None;
		}

		#endregion

		#region Places: ScriptContext, Self, ClassContext, RTVariablesTable, Result

		/// <summary>
		/// A place to load <see cref="PHP.Core.ScriptContext"/> from.
		/// </summary>
		public IPlace/*!*/ ScriptContextPlace;
		public void EmitLoadScriptContext() { ScriptContextPlace.EmitLoad(il); }

		/// <summary>
		/// A place to load <see cref="DTypeDesc"/> representing current class context from.
		/// </summary>
		public IPlace/*!*/ TypeContextPlace;
		public void EmitLoadClassContext() { TypeContextPlace.EmitLoad(il); }

        /// <summary>
        /// A place to load late static bind type from.
        /// It may be a local variable with value copied from <see cref="PhpStack.LateStaticBindType"/>.
        /// </summary>
        public IPlace LateStaticBindTypePlace;

		/// <summary>
		/// A place to load <see cref="NamingContext"/> representing current name context from.
		/// </summary>
		public void EmitLoadNamingContext()
		{
			if (SourceUnit.NamingContextFieldBuilder != null)
				il.Emit(OpCodes.Ldsfld, SourceUnit.NamingContextFieldBuilder);
			else
				il.Emit(OpCodes.Ldnull);
		}

		/// <summary>
		/// A place to load local run-time variables table from.
		/// </summary>
		public IPlace/*!*/ RTVariablesTablePlace;
		public void EmitLoadRTVariablesTable() { RTVariablesTablePlace.EmitLoad(il); }

		/// <summary>
		/// A place to load <see cref="PhpObject"/> representing current object context from.
		/// </summary>
		public IPlace/*!*/ SelfPlace;
		public void EmitLoadSelf() { SelfPlace.EmitLoad(il); }

		/// <summary>
		/// Place to store routine result to.
		/// </summary>
		private IPlace ResultPlace;

		/// <summary>
		/// Label where return statements jumps. 
		/// Defined lazily in the time when result place is defined.
		/// </summary>
		private Label ReturnLabel;

		#endregion

        #region CallSites

        /// <summary>
        /// Current scope CallSites.
        /// Will not be null within the GlobalCode and its sub-tree.
        /// </summary>
        private PHP.Core.Compiler.CodeGenerator.CallSitesBuilder callSites = null;

        /// <summary>
        /// Current scope CallSitesBuilder.
        /// </summary>
        public PHP.Core.Compiler.CodeGenerator.CallSitesBuilder CallSitesBuilder
        {
            get
            {
                Debug.Assert(callSites != null);    // only null outside of the global code
                return callSites;
            }
        }

        #endregion

        #region Constructors

        /// <summary>
		/// Initializes a new instance of the <see cref="PHP.Core.CodeGenerator"/> class.
		/// </summary>
		public CodeGenerator(CompilationContext/*!*/ context)
		{
			ScriptContextPlace = new IndexedPlace(PlaceHolder.Argument, ScriptBuilder.ArgContext);
			TypeContextPlace = new IndexedPlace(PlaceHolder.Argument, ScriptBuilder.ArgIncluder);

			this.context = context;

			this.il = null;
			this.currentVariablesTable = null;
			this.currentLabels = null;
			this.locationStack = new CompilerLocationStack();
			this.branchingStack = new BranchingStack(this);
			this.chainBuilder = new ChainBuilder(this);
		}

		#endregion

		#region Conversions, Boxing, Copying (Tomas Matousek)

		/// <summary>
		/// Emits IL instructions that convert the top of evaluation stack to a specified type.
		/// </summary>
		/// <remarks>
		/// Emits a call to one of <see cref="PHP.Core.Convert"/> methods to do the conversion.
		/// The method result is left on the evaluation stack.
		/// </remarks>
		internal void EmitConversion(Expression/*!*/ expression, PhpTypeCode dst)
		{
			// expression is evaluable:
			if (expression.HasValue)
			{
				switch (dst)
				{
					case PhpTypeCode.String:
						il.Emit(OpCodes.Ldstr, PHP.Core.Convert.ObjectToString(expression.Value));
						break;

					case PhpTypeCode.Boolean:
						il.LdcI4(PHP.Core.Convert.ObjectToBoolean(expression.Value) ? 1 : 0);
						break;

					case PhpTypeCode.Integer:
						il.LdcI4(PHP.Core.Convert.ObjectToInteger(expression.Value));
						break;

					case PhpTypeCode.Double:
						il.Emit(OpCodes.Ldc_R8, PHP.Core.Convert.ObjectToDouble(expression.Value));
						break;

					case PhpTypeCode.Object:
						il.LoadLiteral(expression.Value);
						break;

					default:
						Debug.Fail("Conversion not implemented.");
						break;
				}
			}
			else
			{
				// emits the expression:
				PhpTypeCode src = expression.Emit(this);

				// emits no conversion if types are the same:
				if (src == dst) return;

				// emits boxing if needed (conversion methods takes object):
				EmitBoxing(src);

				switch (dst)
				{
					case PhpTypeCode.String:
						il.Emit(OpCodes.Call, Methods.Convert.ObjectToString);
						break;

					case PhpTypeCode.Boolean:
						il.Emit(OpCodes.Call, Methods.Convert.ObjectToBoolean);
						break;

					case PhpTypeCode.Integer:
						il.Emit(OpCodes.Call, Methods.Convert.ObjectToBoolean);
						break;

					case PhpTypeCode.Double:
						il.Emit(OpCodes.Call, Methods.Convert.ObjectToDouble);
						break;

					case PhpTypeCode.Object:
						// nop //
						break;

					case PhpTypeCode.LinqSource:
						// LOAD Convert.ObjectToLinqSource(<variable>, <type desc>);
                        TypeContextPlace.EmitLoad(il);
                        il.Emit(OpCodes.Call, Methods.Convert.ObjectToLinqSource);
						
						// nop //

						break;

					default:
						Debug.Fail("Conversion is not implemented.");
						break;
				}
			}
		}

		/// <summary>
		/// Emits IL instructions that box a literal value into its object representation.
		/// </summary>
		/// <param name="type"><see cref="PhpTypeCode"/> of the top item placed on evaluation stack.</param>
		/// <returns>The type code of an item on the top of evaluatuion stack after call.</returns>
		/// <remarks>
		/// The <see cref="PHP.Core.AST.Literal"/> to be boxed is expected on the evaluation stack.
		/// The boxed value is left on the evaluation stack.
		/// </remarks>
		public void EmitBoxing(PhpTypeCode type)
		{
			il.EmitBoxing(type);
		}

		/// <summary>
		/// Emits IL instructions that makes a copy of variable placed on the top of evaluation stack.
		/// </summary>
		/// <param name="reason">Reason of the copy.</param>
		/// <param name="expression">The <see cref="PHP.Core.AST.Expression"/> to be copied.</param>
		/// <remarks>
		/// The variable's value is expected on the top of evaluation stack.
		/// Calls <see cref="PHP.Core.PhpVariable.Copy"/> method to process the value copying.
		/// The result is left on the evaluation stack.
		/// </remarks>
		public void EmitVariableCopy(CopyReason reason, Expression expression)
		{
			// checks whether to make a deep copy; starts with level of nesting set to 0:
			if (expression == null || expression.IsDeeplyCopied(reason, 0))
			{
				// First parameter should already be placed on the evaluation stack
				il.LdcI4((int)reason);
				il.Emit(OpCodes.Call, Methods.PhpVariable.Copy);
			}
		}

		#endregion

		#region InstanceOf, TypeOf, New Direct Code Emission

		/// <summary>
		/// Emits IL instructions that check whether a value on stack is an instance of the provided
		/// <see cref="DType"/>.
		/// </summary>
		/// <param name="dtype">The <see cref="DType"/> to check for.</param>
		/// <remarks>
		/// A value is expected on the top of the evaluation stack. Boolean value is left on the stack.
		/// </remarks>
		public void EmitDirectInstanceOf(DType/*!*/ dtype)
		{
			Debug.Assert(dtype != null);
			
			bool is_wrapped;

			PhpType php_type = dtype as PhpType;

			if (php_type != null) is_wrapped = !(php_type.Root is PhpType);
			else
			{
				// determine the root ad-hoc
				DType root = dtype;
				while (root.Base != null) root = root.Base;

				is_wrapped = !(root is PhpType);
			}

			if (!is_wrapped)
			{
				// we can do a direct isinst here only if we know that the instance is not wrapped!
				il.Emit(OpCodes.Isinst, dtype.RealType);
			}
			else
			{
				Label instanceof_over = il.DefineLabel();

				il.Emit(OpCodes.Isinst, Types.DObject[0]);
				il.Emit(OpCodes.Dup);
				il.Emit(OpCodes.Brfalse_S, instanceof_over);
				il.Emit(OpCodes.Callvirt, Properties.DObject_RealObject.GetGetMethod());
				il.Emit(OpCodes.Isinst, dtype.RealType);
				il.MarkLabel(instanceof_over);
			}

			il.Emit(OpCodes.Ldnull);
			il.Emit(OpCodes.Cgt_Un);
		}

		public void EmitDirectTypeOf(DType/*!*/ dtype)
		{
			Debug.Assert(dtype != null);
			
			il.Emit(OpCodes.Ldtoken, dtype.RealType);
			il.Emit(OpCodes.Call, Methods.GetTypeFromHandle);
			il.Emit(OpCodes.Call, Methods.ClrObject_WrapRealObject);
		}

		#endregion

		#region Enter/Leave Function and GlobalCode declaration (Martin Maly, Tomas Matousek)

		/// <summary>
		/// Called when a <see cref="PHP.Core.AST.GlobalCode"/> AST node is entered during the emit phase.
		/// </summary>
		public void EnterGlobalCodeDeclaration(VariablesTable variablesTable,
			Dictionary<VariableName, Statement> labels, SourceUnit/*!*/ sourceUnit)
		{
			CompilerLocationStack.GlobalCodeContext gc_context = new CompilerLocationStack.GlobalCodeContext();

			// no need to backup current source unit as it is no longer needed:
			this.sourceUnit = sourceUnit;

			// set whether access to variables should be generated via locals or table
			gc_context.OptimizedLocals = this.OptimizedLocals;
			this.OptimizedLocals = false;

			// global code returns object
			gc_context.ReturnsPhpReference = this.ReturnsPhpReference;
			this.ReturnsPhpReference = false;

            // CallSites
            Debug.Assert(this.callSites == null, "Unclosed CallSite!");
            this.callSites = new Compiler.CodeGenerator.CallSitesBuilder(
                sourceUnit.CompilationUnit.Module.GlobalType.RealModuleBuilder,
                sourceUnit.SourceFile.RelativePath.ToString(),
                null/*Unknown at compile time*/);

			// set ILEmitter for global code
			gc_context.IL = il;
			il = CompilationUnit.ModuleBuilder.CreateGlobalCodeEmitter();

			// set current variables table (at codeGenerator)
			gc_context.CurrentVariablesTable = currentVariablesTable;
			currentVariablesTable = variablesTable;

			// set current labels table (at codeGenerator)
			gc_context.CurrentLabels = currentLabels;
			currentLabels = labels;

			// set OpCode for loading hashtable with variables at runtime
			gc_context.RTVariablesTablePlace = RTVariablesTablePlace;
			RTVariablesTablePlace = new IndexedPlace(PlaceHolder.Argument, 1);

			// set Script Context place
			gc_context.ScriptContextPlace = ScriptContextPlace;
			ScriptContextPlace = new IndexedPlace(PlaceHolder.Argument, ScriptBuilder.ArgContext);

			// set Class Context place
			gc_context.ClassContextPlace = TypeContextPlace;
			TypeContextPlace = new IndexedPlace(PlaceHolder.Argument, ScriptBuilder.ArgIncluder);

			// set Self place
			gc_context.SelfPlace = SelfPlace;
			SelfPlace = new IndexedPlace(PlaceHolder.Argument, ScriptBuilder.ArgSelf);

            // set late static bind place
            gc_context.LateStaticBindTypePlace = LateStaticBindTypePlace;
            LateStaticBindTypePlace = null;

			// set Result place and return label
			gc_context.ResultPlace = ResultPlace;
			gc_context.ReturnLabel = ReturnLabel;
			ResultPlace = null;

			// set exception block nesting:
			gc_context.ExceptionBlockNestingLevel = ExceptionBlockNestingLevel;
			ExceptionBlockNestingLevel = 0;

			locationStack.PushGlobalCode(gc_context);
		}

		/// <summary>
		/// Called when a <see cref="PHP.Core.AST.GlobalCode"/> AST node is left during the emit phase.
		/// </summary>
		public void LeaveGlobalCodeDeclaration()
		{
			CompilerLocationStack.GlobalCodeContext gc_context = locationStack.PeekGlobalCode();
			locationStack.Pop();

			// clear (for convenience):
			this.sourceUnit = null;

            // close CallSites:
            this.callSites.Bake();

			// restore:
            this.callSites = null;
			this.il = gc_context.IL;
			this.ScriptContextPlace = gc_context.ScriptContextPlace;
			this.TypeContextPlace = gc_context.ClassContextPlace;
            this.LateStaticBindTypePlace = null;
			this.SelfPlace = gc_context.SelfPlace;
			this.ResultPlace = gc_context.ResultPlace;
			this.ReturnLabel = gc_context.ReturnLabel;
			this.currentVariablesTable = gc_context.CurrentVariablesTable;
			this.currentLabels = gc_context.CurrentLabels;
			this.RTVariablesTablePlace = gc_context.RTVariablesTablePlace;
			this.OptimizedLocals = gc_context.OptimizedLocals;
			this.ReturnsPhpReference = gc_context.ReturnsPhpReference;
			this.ExceptionBlockNestingLevel = gc_context.ExceptionBlockNestingLevel;
		}

		/// <summary>
		/// Called when a <see cref="PHP.Core.AST.FunctionDecl"/> AST node is entered during the emit phase.
		/// </summary>
		/// <param name="function">The function to enter.</param>
		/// <returns><B>true</B> if the function should be emitted, <B>false</B> if it should not be emitted
		/// (an error was emitted instead due to the incorrect declaration).</returns>
        public bool EnterFunctionDeclaration(PhpFunction/*!*/ function)
        {
            return EnterFunctionDeclarationInternal(function, function.QualifiedName);
        }

        public bool EnterFunctionDeclaration(PhpLambdaFunction/*!*/ function)
        {
            return EnterFunctionDeclarationInternal(function, new QualifiedName(function.Name));
        }

        private bool EnterFunctionDeclarationInternal(PhpRoutine/*!*/ function, QualifiedName qualifiedName)
		{
            Debug.Assert(function.IsFunction);

			bool is_optimized = (function.Properties & RoutineProperties.HasUnoptimizedLocals) == 0;
			bool indirect_local_access = (function.Properties & RoutineProperties.IndirectLocalAccess) != 0;

			CompilerLocationStack.FunctionDeclContext fd_context = new CompilerLocationStack.FunctionDeclContext();
            fd_context.Name = qualifiedName;

			// Set whether access to variables should be generated via locals or table
			fd_context.OptimizedLocals = this.OptimizedLocals;
			this.OptimizedLocals = is_optimized;

			// Set the valid method to emit the "return" statement
			fd_context.ReturnsPhpReference = this.ReturnsPhpReference;
			this.ReturnsPhpReference = function.Signature.AliasReturn;

            // CallSites
            fd_context.CallSites = null;//fd_context.CallSites = callSites;
            //this.callSites = new Compiler.CodeGenerator.CallSitesBuilder(
            //    sourceUnit.CompilationUnit.Module.GlobalType.RealModuleBuilder,
            //    fd_context.Name.ToString(),
            //    LiteralPlace.Null);
            // keep current site container, just change the class context (to avoid of creating and baking so many types)
            this.callSites.PushClassContext(LiteralPlace.Null, null);
            
            // Set ILEmitter to function's body
			fd_context.IL = this.il;
			this.il = new ILEmitter(function.ArgFullInfo);

			// Set current variables table (at codeGenerator)
			fd_context.CurrentVariablesTable = this.currentVariablesTable;
			this.currentVariablesTable = function.Builder.LocalVariables;

			// Set current variables table (at codeGenerator)
			fd_context.CurrentLabels = this.currentLabels;
			this.currentLabels = function.Builder.Labels;

			// Set place for loading hashtable with variables at runtime
			fd_context.RTVariablesTablePlace = this.RTVariablesTablePlace;

			if (indirect_local_access || !is_optimized)
			{
				LocalBuilder var_table_local = il.DeclareLocal(PhpVariable.RTVariablesTableType);
				if (sourceUnit.SymbolDocumentWriter != null)
					var_table_local.SetLocalSymInfo("<locals>");
				this.RTVariablesTablePlace = new Place(var_table_local);
			}
			else
				this.RTVariablesTablePlace = LiteralPlace.Null;

			// Set ScriptContext
			fd_context.ScriptContextPlace = this.ScriptContextPlace;
			this.ScriptContextPlace = new IndexedPlace(PlaceHolder.Argument, FunctionBuilder.ArgContext);

			// Set Class context
			fd_context.ClassContextPlace = this.TypeContextPlace;
			this.TypeContextPlace = LiteralPlace.Null;

			// Set Self
			fd_context.SelfPlace = this.SelfPlace;
			this.SelfPlace = LiteralPlace.Null;

            // set Result place
			fd_context.ResultPlace = this.ResultPlace;
			fd_context.ReturnLabel = this.ReturnLabel;
			this.ResultPlace = null;
            this.LateStaticBindTypePlace = null;

			// set exception block nesting:
			fd_context.ExceptionBlockNestingLevel = this.ExceptionBlockNestingLevel;
			this.ExceptionBlockNestingLevel = 0;

            // set current PhpRoutine
            fd_context.PhpRoutine = function;

            //
			locationStack.PushFunctionDecl(fd_context);
			return true;
		}


		/// <summary>
		/// Called when a <see cref="PHP.Core.AST.FunctionDecl"/> AST node is left during the emit phase.
		/// </summary>
		public void LeaveFunctionDeclaration()
		{
			CompilerLocationStack.FunctionDeclContext fd_context = locationStack.PeekFunctionDecl();
			locationStack.Pop();

            // close CallSites:
            //this.callSites.Bake();
            
			// restore:
            this.callSites.PopClassContext();//this.callSites = fd_context.CallSites;
            this.il = fd_context.IL;
			this.ScriptContextPlace = fd_context.ScriptContextPlace;
			this.TypeContextPlace = fd_context.ClassContextPlace;
            this.LateStaticBindTypePlace = fd_context.LateStaticBindTypePlace;
			this.SelfPlace = fd_context.SelfPlace;
			this.ResultPlace = fd_context.ResultPlace;
			this.ReturnLabel = fd_context.ReturnLabel;
			this.currentVariablesTable = fd_context.CurrentVariablesTable;
			this.currentLabels = fd_context.CurrentLabels;
			this.RTVariablesTablePlace = fd_context.RTVariablesTablePlace;
			this.OptimizedLocals = fd_context.OptimizedLocals;
			this.ReturnsPhpReference = fd_context.ReturnsPhpReference;
			this.ExceptionBlockNestingLevel = fd_context.ExceptionBlockNestingLevel;
		}

		public bool EnterLambdaDeclaration(ILEmitter il, bool aliasReturn, IPlace/*!*/ rtVariablesTablePlace,
			IPlace/*!*/ scriptContextPlace, IPlace/*!*/ classContextPlace, IPlace/*!*/ selfPlace)
		{
			CompilerLocationStack.FunctionDeclContext fd_context = new CompilerLocationStack.FunctionDeclContext();
			fd_context.Name = QualifiedName.Lambda;

			// Set whether access to variables should be generated via locals or table
			fd_context.OptimizedLocals = this.OptimizedLocals;
			this.OptimizedLocals = false;

			// Set the valid method to emit the "return" statement
			fd_context.ReturnsPhpReference = this.ReturnsPhpReference;
			this.ReturnsPhpReference = aliasReturn;

            // CallSites
            fd_context.CallSites = null;
            //this.callSites = new Compiler.CodeGenerator.CallSitesBuilder(
            //    sourceUnit.CompilationUnit.Module.GlobalType.RealModuleBuilder,
            //    fd_context.Name.ToString(),
            //    null/*class_context = Unknown (at compile time)*/);
            // keep current site container, to be compatible with LeaveFunctionDeclaration
            this.callSites.PushClassContext(null, null);
            
            // Set ILEmitter to function's body
			fd_context.IL = this.il;
			this.il = il;

			// current variables table remains unchanged:
			fd_context.CurrentVariablesTable = this.currentVariablesTable;

			// current variables table remains unchanged:
			fd_context.CurrentLabels = this.currentLabels;

			// Set place for loading hashtable with variables at runtime
			fd_context.RTVariablesTablePlace = this.RTVariablesTablePlace;
			this.RTVariablesTablePlace = rtVariablesTablePlace;

			// Set ScriptContext
			fd_context.ScriptContextPlace = this.ScriptContextPlace;
			this.ScriptContextPlace = scriptContextPlace;

			// Set Class context
			fd_context.ClassContextPlace = this.TypeContextPlace;
			this.TypeContextPlace = classContextPlace;

			// Set Self
			fd_context.SelfPlace = this.SelfPlace;
			this.SelfPlace = selfPlace;

			// set Result place
			fd_context.ResultPlace = this.ResultPlace;
			fd_context.ReturnLabel = this.ReturnLabel;
            fd_context.LateStaticBindTypePlace = this.LateStaticBindTypePlace;
			this.ResultPlace = null;
            this.LateStaticBindTypePlace = null;

			// set exception block nesting:
			fd_context.ExceptionBlockNestingLevel = this.ExceptionBlockNestingLevel;
			this.ExceptionBlockNestingLevel = 0;

            // set current PhpRoutine
            fd_context.PhpRoutine = null;

            //
			locationStack.PushFunctionDecl(fd_context);
			return true;
		}

		#endregion

		#region Class, GetUserEntryPoint, Field and Class Constant declaration (Ladislav Prosek)

		/// <summary>
		/// Called when a <see cref="PHP.Core.AST.TypeDecl"/> AST node is entered during the emit phase.
		/// </summary>
		public void EnterTypeDeclaration(PhpType/*!*/ type)
		{
			CompilerLocationStack.TypeDeclContext cd_context = new CompilerLocationStack.TypeDeclContext();
			cd_context.Type = type;

			cd_context.TypeContextPlace = TypeContextPlace;
			TypeContextPlace = new Place(null, type.TypeDescFieldInfo);

            // CallSites
            cd_context.CallSites = callSites;
            this.callSites = new Compiler.CodeGenerator.CallSitesBuilder(
                sourceUnit.CompilationUnit.Module.GlobalType.RealModuleBuilder,
                type.QualifiedName.ToString(),
                TypeContextPlace, /*class_context = TypeContextPlace, can be used in .cctor of call sites container*/
                type);

            //
            locationStack.PushTypeDecl(cd_context);
		}

		/// <summary>
		/// Called when a <see cref="PHP.Core.AST.TypeDecl"/> AST node is left during the emit phase.
		/// </summary>
		public void LeaveTypeDeclaration()
		{
			CompilerLocationStack.TypeDeclContext context = locationStack.PeekTypeDecl();
            locationStack.Pop();

            // close CallSites:
            this.callSites.Bake();

            // restore:
            this.callSites = context.CallSites;
            this.TypeContextPlace = context.TypeContextPlace;			
		}


		/// <summary>
		/// Called when a <see cref="PHP.Core.AST.MethodDecl"/> AST node is entered during the emit phase.
		/// </summary>
		public void EnterMethodDeclaration(PhpMethod/*!*/ method)
		{
            bool is_optimized = (method.Properties & RoutineProperties.HasUnoptimizedLocals) == 0;
			bool rt_variables_table = (method.Properties & RoutineProperties.HasRTVariablesTable) != 0;

			CompilerLocationStack.TypeDeclContext class_context = locationStack.PeekTypeDecl();

			CompilerLocationStack.MethodDeclContext md_context = new CompilerLocationStack.MethodDeclContext();
			md_context.Type = class_context.Type;
			md_context.Method = method;

			// Set whether access to variables should be generated via locals or table
			md_context.OptimizedLocals = this.OptimizedLocals;
			OptimizedLocals = is_optimized;

			// set compile-time variables table:
			md_context.CurrentVariablesTable = this.currentVariablesTable;
			currentVariablesTable = method.Builder.LocalVariables;

			// set compile-time variables table:
			md_context.CurrentLabels = this.currentLabels;
			currentLabels = method.Builder.Labels;

			// Set the valid method to emit the "return" statement
			md_context.ReturnsPhpReference = this.ReturnsPhpReference;
			this.ReturnsPhpReference = method.Signature.AliasReturn;

            // CallSites (same as in TypeDecl, not changed):
            //md_context.CallSites = callSites;
            //this.callSites = new Compiler.CodeGenerator.CallSites(/*class_context = TypeContextPlace*/);

            // create new IL emitter for the method:
            md_context.IL = il;
			il = new ILEmitter(method.ArgFullInfo);

			// set RT variables table place:
			md_context.RTVariablesTablePlace = RTVariablesTablePlace;
			if (rt_variables_table)
			{
				LocalBuilder var_table_local = il.DeclareLocal(PhpVariable.RTVariablesTableType);
				if (sourceUnit.SymbolDocumentWriter != null)
					var_table_local.SetLocalSymInfo("<locals>");
				RTVariablesTablePlace = new Place(var_table_local);
			}
			else
				RTVariablesTablePlace = LiteralPlace.Null;

			// sets ScriptContext and Self places appropriately:
			md_context.ClassContextPlace = TypeContextPlace;
			md_context.ScriptContextPlace = ScriptContextPlace;
			md_context.SelfPlace = SelfPlace;
            md_context.LateStaticBindTypePlace = LateStaticBindTypePlace;

			if (method.IsStatic)
			{
				ScriptContextPlace = new IndexedPlace(PlaceHolder.Argument, FunctionBuilder.ArgContextStatic);
				SelfPlace = LiteralPlace.Null;
			}
			else
			{
				ScriptContextPlace = new IndexedPlace(PlaceHolder.Argument, FunctionBuilder.ArgContextInstance);
				if (method.DeclaringPhpType.ProxyFieldInfo != null)
				{
					// the real this is not a DObject
					SelfPlace = new Place(IndexedPlace.ThisArg, method.DeclaringPhpType.ProxyFieldInfo);
				}
				else
				{
					// the real this is a DObject
					SelfPlace = IndexedPlace.ThisArg;
				}
			}

			// set Result place and return label:
			md_context.ResultPlace = ResultPlace;
			md_context.ReturnLabel = ReturnLabel;
			ResultPlace = null;
            LateStaticBindTypePlace = null;

			// set exception block nesting:
			md_context.ExceptionBlockNestingLevel = ExceptionBlockNestingLevel;
			ExceptionBlockNestingLevel = 0;

            // set current PhpRoutine
            md_context.PhpRoutine = method;

            //
			locationStack.PushMethodDecl(md_context);
		}

		/// <summary>
		/// Called when a <see cref="PHP.Core.AST.MethodDecl"/> AST node is left during the emit phase.
		/// </summary>
		public void LeaveMethodDeclaration()
		{
			CompilerLocationStack.MethodDeclContext md_context = locationStack.PeekMethodDecl();
			locationStack.Pop();

			// restore:
            //this.callSites = md_context.CallSite; // the same
			this.il = md_context.IL;
			this.ScriptContextPlace = md_context.ScriptContextPlace;
			this.TypeContextPlace = md_context.ClassContextPlace;
            this.LateStaticBindTypePlace = md_context.LateStaticBindTypePlace;
			this.SelfPlace = md_context.SelfPlace;
			this.ResultPlace = md_context.ResultPlace;
			this.ReturnLabel = md_context.ReturnLabel;
			this.currentVariablesTable = md_context.CurrentVariablesTable;
			this.currentLabels = md_context.CurrentLabels;
			this.RTVariablesTablePlace = md_context.RTVariablesTablePlace;
			this.OptimizedLocals = md_context.OptimizedLocals;
			this.ReturnsPhpReference = md_context.ReturnsPhpReference;
			this.ExceptionBlockNestingLevel = md_context.ExceptionBlockNestingLevel;
		}

		/// <summary>
		/// Called when a <see cref="PHP.Core.AST.ConstantDecl"/> AST node is visited during the emit phase.
		/// </summary>
		/// <param name="constant">The constant.</param>
		/// <remarks>
		/// Even interface constants are permitted in PHP. These are implemented by <B>static</B> <B>initonly</B>
		/// fields in the interface, which causes some complaints in the .NET Framework 1.1 verifier.
		/// However it is rather a verifier bug - .NET Framework 2.0 verifier is fixed and verifies it OK.
		/// </remarks>
		public void InitializeClassConstant(ClassConstant/*!*/ constant)
		{
			Debug.Assert(constant != null);

            // real constant definition
            if (constant.RealField.IsLiteral)
            {
                Debug.Assert(constant.RealFieldBuilder != null);
                constant.RealFieldBuilder.SetConstant(constant.Value);
                return;
            }

			// class constant initialization is emitted into the static constructor
			ILEmitter old_il = il;
			IPlace old_sc_emitter = ScriptContextPlace;
			try
			{
				// set il and SC-emitter appropriately
				
				if (constant.HasValue)
				{
					il = constant.DeclaringPhpType.Builder.StaticCtorEmitter;
                    il.LoadLiteralBox(constant.Value);
				}
				else
				{
                    il = new ILEmitter(constant.DeclaringPhpType.StaticFieldInitMethodBuilder);
                    ScriptContextPlace = new IndexedPlace(PlaceHolder.Argument, ScriptBuilder.ArgContext);

					// emit the expression evaluating code
					EmitBoxing(constant.Node.Initializer.Emit(this));
				}

				// store it in the field
				il.Emit(OpCodes.Stsfld, constant.RealField);
			}
			finally
			{
				// restore the saved il and SC-emitter
				il = old_il;
				ScriptContextPlace = old_sc_emitter;
			}
		}

		/// <summary>
		/// Called when a <see cref="PHP.Core.AST.FieldDecl"/> AST node is visited during the emit phase.
		/// </summary>
		public void InitializeField(PhpField/*!*/ field, AST.Expression initVal)
		{
			ILEmitter cil;
			IPlace sc_place;
            
			if (field.IsStatic)
			{
                // (J) even overiding static field is created again in derivating type
				// there is no initialization taking place if the implementing CLI field does not live in current class
				//if (field.Overrides != null) return;

				if (field.IsAppStatic)
				{
					// app-static field initialization is emitted into the static ctor
					cil = field.DeclaringPhpType.Builder.StaticCtorEmitter;

					sc_place = new LazyLoadSCPlace();
				}
				else
				{
					// thread-static field initialization is emitted into the __InitializeStaticFields method
					cil = new ILEmitter(field.DeclaringPhpType.StaticFieldInitMethodBuilder);

					sc_place = new IndexedPlace(PlaceHolder.Argument, ScriptBuilder.ArgContext);
				}
			}
			else
			{
                if (initVal == null && field.Implementor != field.DeclaringType)
                    return;

				// instance field initialization is emitted into the <InitializeInstanceFields> method
				cil = field.DeclaringPhpType.Builder.InstanceFieldInitEmitter;

				sc_place = new IndexedPlace(PlaceHolder.Argument, FunctionBuilder.ArgContextInstance);

				cil.Ldarg(FunctionBuilder.ArgThis);
			}

			if (initVal != null)
			{
				// emit the expression evaluating code
				ILEmitter old_il = il;
				IPlace old_sc_place = ScriptContextPlace;

				try
				{
					// set il and SC-emitter appropriately
					il = cil;
					ScriptContextPlace = sc_place;

					EmitBoxing(initVal.Emit(this));
				}
				finally
				{
					// restore the saved il and SC-emitter
					il = old_il;
					ScriptContextPlace = old_sc_place;
				}

				cil.Emit(OpCodes.Newobj, Constructors.PhpSmartReference.Object);
			}
			else cil.Emit(OpCodes.Newobj, Constructors.PhpSmartReference.Void);

			// store it in the field
            Debug.Assert(field.IsStatic == field.RealField.IsStatic);
			cil.Emit(field.IsStatic ? OpCodes.Stsfld : OpCodes.Stfld, field.RealField);
		}

		public void EmitConstantExportStub(ClassConstant/*!*/ constant, PropertyBuilder/*!*/ property)
		{
			Debug.Assert(constant != null && constant.IsExported && property != null);

			MethodBuilder getter = (MethodBuilder)property.GetGetMethod(true);

			// emit getter:
			if (getter != null)
			{
				EmissionContext emission_context = SetupStubPlaces(constant.DeclaringPhpType, getter.IsStatic);
				il = new ILEmitter(getter);

				try
				{
					// read the field
					PhpTypeCode type_code = constant.EmitGet(this, null, false, null);

					// convert it to the return type
					//ClrOverloadBuilder.EmitConvertToClr(
					//    il,
					//    type_code,
					//    getter.ReturnType);

					il.EmitBoxing(type_code);

					il.Emit(OpCodes.Ret);
				}
				finally
				{
					RestorePlaces(emission_context);
				}
			}
		}

		public void EmitFieldExportGetter(PhpField/*!*/ field, PropertyBuilder/*!*/ property, MethodBuilder/*!*/ getter)
		{
			IPlace instance_place = (field.IsStatic ? null : IndexedPlace.ThisArg);

			EmissionContext emission_context = SetupStubPlaces(field.DeclaringPhpType, getter.IsStatic);

			il = new ILEmitter(getter);
			try
			{
				// read the field
				PhpTypeCode type_code = field.EmitGet(this, instance_place, false, null, false);

				// convert it to the return type
				ClrOverloadBuilder.EmitConvertToClr(
					il,
					type_code,
					property.PropertyType);

				il.Emit(OpCodes.Ret);
			}
			finally
			{
				RestorePlaces(emission_context);
			}
		}

		public void EmitFieldExportSetter(PhpField/*!*/ field, PropertyBuilder/*!*/ property, MethodBuilder/*!*/ setter)
		{
			IPlace instance_place = (field.IsStatic ? null : IndexedPlace.ThisArg);

			EmissionContext emission_context = SetupStubPlaces(field.DeclaringPhpType, setter.IsStatic);

			il = new ILEmitter(setter);
			try
			{
				// prepare the field for writing
				AssignmentCallback callback = field.EmitSet(this, instance_place, false, null, false);

				// load and convert the argument
				il.Ldarg(setter.IsStatic ? 0 : 1);
				PhpTypeCode type_code = ClrOverloadBuilder.EmitConvertToPhp(
					il,
					property.PropertyType/*,
					ScriptContextPlace*/);

				EmitBoxing(type_code);

				// write the field
				callback(this, PhpTypeCode.Object);

				il.Emit(OpCodes.Ret);
			}
			finally
			{
				RestorePlaces(emission_context);
			}
		}

		public void EmitFieldExportStubs(PhpField/*!*/ field, PropertyBuilder/*!*/ property)
		{
			Debug.Assert(field != null && property != null);

			MethodBuilder getter = (MethodBuilder)property.GetGetMethod(true);
			MethodBuilder setter = (MethodBuilder)property.GetSetMethod(true);

			// emit getter:
			if (getter != null) EmitFieldExportGetter(field, property, getter);

			// emit setter:
			if (setter != null) EmitFieldExportSetter(field, property, setter);
		}

		#endregion

		private struct EmissionContext
		{
			public EmissionContext(IPlace scriptContextPlace, IPlace selfPlace, ILEmitter il)
			{
				this.ScriptContextPlace = scriptContextPlace;
				this.SelfPlace = selfPlace;
				this.IL = il;
			}

			public IPlace ScriptContextPlace;
			public IPlace SelfPlace;
			public ILEmitter IL;
		}

		private EmissionContext SetupStubPlaces(PhpType/*!*/ type, bool stubIsStatic)
		{
			EmissionContext context = new EmissionContext(ScriptContextPlace, SelfPlace, il);

			ScriptContextPlace = new LazyLoadSCPlace();
			if (stubIsStatic)
			{
				SelfPlace = LiteralPlace.Null;
			}
			else
			{
				if (type.ProxyFieldInfo != null)
				{
					// the real this is not a DObject
					SelfPlace = new Place(IndexedPlace.ThisArg, type.ProxyFieldInfo);
				}
				else
				{
					// the real this is a DObject
					SelfPlace = IndexedPlace.ThisArg;
				}
			}

			return context;
		}

		private void RestorePlaces(EmissionContext emissionContext)
		{
			ScriptContextPlace = emissionContext.ScriptContextPlace;
			SelfPlace = emissionContext.SelfPlace;
			il = emissionContext.IL;
		}

		#region Echo and Print

        ///// <summary>
        ///// Emits IL instructions for calling the best overload of <see cref="PHP.Core.ScriptContext.Echo"/> method.
        ///// </summary>
        ///// <param name="typecode"><see cref="PHP.Core.PhpTypeCode"/> of the parameter.</param>
        ///// <remarks>GetUserEntryPoint parameters are expected on the evaluation stack. Nothing is left on the evaluation stack.</remarks>
        //private void EmitEchoCall(PhpTypeCode typecode)
        //{
        //    switch (typecode)
        //    {
        //        case PhpTypeCode.Object:
        //            il.Emit(OpCodes.Call, Methods.ScriptContext.Echo.Object);
        //            break;

        //        case PhpTypeCode.String:
        //            il.Emit(OpCodes.Call, Methods.ScriptContext.Echo.String);
        //            break;

        //        case PhpTypeCode.PhpBytes:
        //            il.Emit(OpCodes.Call, Methods.ScriptContext.Echo.PhpBytes);
        //            break;

        //        case PhpTypeCode.Integer:
        //            il.Emit(OpCodes.Call, Methods.ScriptContext.Echo.Int);
        //            break;

        //        case PhpTypeCode.LongInteger:
        //            il.Emit(OpCodes.Call, Methods.ScriptContext.Echo.LongInt);
        //            break;

        //        case PhpTypeCode.Double:
        //            il.Emit(OpCodes.Call, Methods.ScriptContext.Echo.Double);
        //            break;

        //        case PhpTypeCode.Boolean:
        //            il.Emit(OpCodes.Call, Methods.ScriptContext.Echo.Bool);
        //            break;

        //        default:
        //            il.Emit(OpCodes.Call, Methods.ScriptContext.Echo.Object);
        //            break;
        //    }
        //}

        /// <summary>
        /// Emits IL instructions for calling the best overload of <see cref="PHP.Core.ScriptContext.Echo"/> static method.
        /// </summary>
        /// <param name="typecode"><see cref="PHP.Core.PhpTypeCode"/> of the parameter.</param>
        /// <remarks>Nothing is left on the evaluation stack. Emitted method call expects two parameters on the evaluation stack: (value, ScriptContext).</remarks>
        private void EmitEchoStaticCall(PhpTypeCode typecode)
        {
            switch (typecode)
            {
                case PhpTypeCode.Object:
                    il.Emit(OpCodes.Call, Methods.ScriptContext.EchoStatic.Object);
                    break;

                case PhpTypeCode.String:
                    il.Emit(OpCodes.Call, Methods.ScriptContext.EchoStatic.String);
                    break;

                case PhpTypeCode.PhpBytes:
                    il.Emit(OpCodes.Call, Methods.ScriptContext.EchoStatic.PhpBytes);
                    break;

                case PhpTypeCode.Integer:
                    il.Emit(OpCodes.Call, Methods.ScriptContext.EchoStatic.Int);
                    break;

                case PhpTypeCode.LongInteger:
                    il.Emit(OpCodes.Call, Methods.ScriptContext.EchoStatic.LongInt);
                    break;

                case PhpTypeCode.Double:
                    il.Emit(OpCodes.Call, Methods.ScriptContext.EchoStatic.Double);
                    break;

                case PhpTypeCode.Boolean:
                    il.Emit(OpCodes.Call, Methods.ScriptContext.EchoStatic.Bool);
                    break;

                default:
                    il.Emit(OpCodes.Call, Methods.ScriptContext.EchoStatic.Object);
                    break;
            }
        }

        /// <summary>
        /// Emits IL instructions to process the <B>echo</B> and <B>print</B> commands.
        /// </summary>
        /// <param name="expressions">List of expressions to be echoed. They will be evaluated first. The list cannot be null and it must contain at least one element.</param>
        public void EmitEcho(List<Expression>/*!*/expressions)
        {
            Debug.Assert(expressions != null);
            Debug.Assert(expressions.Count > 0);

            // known types of resulting values
            PhpTypeCode[] types = new PhpTypeCode[expressions.Count];

            // construct the array with values
            // to preserve the proper order of evaluation and output
            il.LdcI4(expressions.Count);
            il.Emit(OpCodes.Newarr, typeof(object));

            for (int i = 0; i < expressions.Count; ++i)
            {
                // array[<i>] = <expressions[i]>;
                il.Emit(OpCodes.Dup);
                il.LdcI4(i);
                EmitBoxing(types[i] = expressions[i].Emit(this));
                il.Emit(OpCodes.Stelem_Ref);
            }

            // echo the values
            for (int i = 0; i < expressions.Count; ++i)
            {
                il.Emit(OpCodes.Dup);   // array
                il.LdcI4(i);            // <i>
                il.Emit(OpCodes.Ldelem_Ref);    // object array[<i>]
                il.EmitUnboxingForArg(types[i]);  // UnBox value type, if value-type was boxed here, prepared for method call argument
                // convert object to string or PhpBytes to hold the right type on the stack (valid IL)
                if (types[i] == PhpTypeCode.PhpBytes)   il.Emit(OpCodes.Castclass, Types.PhpBytes[0]);
                else if (types[i] == PhpTypeCode.String) il.Emit(OpCodes.Castclass, Types.String[0]);
                EmitLoadScriptContext();
                // CALL ScriptContext.Echo(<obj>, <ScriptContext>)
                EmitEchoStaticCall(types[i]);
            }

            il.Emit(OpCodes.Pop);// remove the array from the stack
        }

		/// <summary>
		/// Emits IL instructions to process the <B>echo</B> and <B>print</B> commands.
		/// </summary>
		/// <param name="parameter">Expression to be sent to output.</param>
		public void EmitEcho(Expression parameter)
		{
			// Template:
			//	context.Echo(value);

			ConcatEx concat;
			//BinaryEx binary_expr;

			if ((concat = parameter as ConcatEx) != null && concat.Expressions.Count > 1)
			{
                //foreach (Expression expr in concat.Expressions)
                //{
                //    EmitLoadScriptContext();
                //    EmitEchoCall(expr.Emit(this));
                //}

				// obsolete: (but expressions must be first emitted and processed, then echoed)
                // array = new object[] { expr1, expr2, ..., exprn };
                //LocalBuilder array = EmitObjectArrayPopulation(concat.Expressions, null);

                //// context.Echo(array);
                //EmitLoadScriptContext();
                //il.Ldloc(array);
                //il.Emit(OpCodes.Call, Methods.ScriptContext.Echo.ObjectArray);

                EmitEcho(concat.Expressions);
			}
            //// obsolete: wrong order of expressions execution (evaluate first, then echo!)
            //else if ((binary_expr = parameter as BinaryEx) != null && binary_expr.Operation == Operations.Concat)
            //{
            //    // context.Echo(<left>)
            //    EmitLoadScriptContext();
            //    EmitEchoCall(binary_expr.LeftExpr.Emit(this));

            //    // context.Echo(<right>)
            //    EmitLoadScriptContext();
            //    EmitEchoCall(binary_expr.RightExpr.Emit(this));
            //}
			else
			{
                var typecode = parameter.Emit(this);
				EmitLoadScriptContext();
                // CALL ScriptContext.Echo(<parameter>, <context>)
                EmitEchoStaticCall(typecode);
			}
		}

		#endregion

		#region Load(Ref)/Store(Ref)/Variable support

		/// <summary>
		/// If set to <B>true</B> the optimized access to locals is emitted. If set to <B>false</B>
		/// the access via variables table is emitted. This flag is controled by in 
		/// <see cref="EnterGlobalCodeDeclaration"/> and <see cref="EnterFunctionDeclaration"/> methods.
		/// </summary>
		public bool OptimizedLocals;

		#region Auto-global variables

		private readonly OpCode AutoGlobalLoadOpCode = OpCodes.Ldfld;
		private readonly OpCode AutoGlobalStoreOpCode = OpCodes.Stfld;

		internal bool VariableIsAutoGlobal(VariableName name)
		{
			if (!currentVariablesTable.Contains(name))
			{
				if (name.IsAutoGlobal)
				{
					return true;
				}
				Debug.Fail("DirectVarUse not in table and not auto-global!");
			}
			return false;
		}

		/// <summary>
		/// Emits an operation on auto-global variable with a specified name.
		/// </summary>
		/// <param name="name">The name of the auto-global variable.</param>
		/// <param name="opCode">The operation.</param>
		private void EmitAutoGlobalOperation(VariableName name, OpCode opCode)
		{
			FieldInfo fld = AutoGlobals.GetFieldForVariable(name);
			if (fld != null)
				il.Emit(opCode, fld);
			else
				Debug.Fail("Unsupported auto-global variable.");
		}

		private void EmitLoadAutoGlobals()
		{
			EmitLoadScriptContext();
			il.Emit(OpCodes.Ldfld, Fields.ScriptContext_AutoGlobals);
		}

		internal void EmitAutoGlobalLoad(VariableName variableName)
		{
			EmitLoadAutoGlobals();
			EmitAutoGlobalOperation(variableName, AutoGlobalLoadOpCode);
			il.Emit(OpCodes.Ldfld, Fields.PhpReference_Value);
		}

		internal void EmitAutoGlobalLoadAddress(VariableName variableName)
		{
			EmitLoadAutoGlobals();
			EmitAutoGlobalOperation(variableName, AutoGlobalLoadOpCode);
			il.Emit(OpCodes.Ldflda, Fields.PhpReference_Value);
		}

		internal void EmitAutoGlobalLoadRef(VariableName variableName)
		{
			EmitLoadAutoGlobals();
			EmitAutoGlobalOperation(variableName, AutoGlobalLoadOpCode);
		}

		internal void EmitAutoGlobalStorePrepare(VariableName variableName)
		{
			EmitLoadAutoGlobals();
			EmitAutoGlobalOperation(variableName, AutoGlobalLoadOpCode);
		}

		internal void EmitAutoGlobalStoreAssign()
		{
			il.Emit(OpCodes.Stfld, Fields.PhpReference_Value);
		}

		internal void EmitAutoGlobalStoreRefPrepare(VariableName variableName)
		{
			EmitLoadAutoGlobals();
		}

		internal void EmitAutoGlobalStoreRefAssign(VariableName variableName)
		{
			EmitAutoGlobalOperation(variableName, AutoGlobalStoreOpCode);
		}

		#endregion

		#region Support for optimized functions

		private void EmitVariableIssetOptimized(SimpleVarUse variable)
		{
			// Template: for DirectVarUse

			//	ISSET($x)
			//	 ldloc local
			// *** if local is of type PhpReference
			// 	 ldfld PhpReference.Value
			// ***
			//	 ldnull
			//	 ceq
			//	 ldc.i4.0
			//	 ceq           
			DirectVarUse direct = variable as DirectVarUse;
			if (direct != null)
			{
				VariablesTable.Entry entry = currentVariablesTable[direct.VarName];

				entry.Variable.EmitLoad(il);
				if (entry.IsPhpReference)
				{
					il.Emit(OpCodes.Ldfld, Fields.PhpReference_Value);
				}
				il.Emit(OpCodes.Ldnull);
				il.Emit(OpCodes.Ceq);
				il.Emit(OpCodes.Ldc_I4_0);
				il.Emit(OpCodes.Ceq);
				return;
			}

			IndirectVarUse indirect_var = (IndirectVarUse)variable;
			indirect_var.EmitSwitch(this, new IndirectVarUse.SwitchMethod(indirect_var.LoadLocal));
			il.Emit(OpCodes.Ldnull);
			il.Emit(OpCodes.Ceq);
			il.Emit(OpCodes.Ldc_I4_0);
			il.Emit(OpCodes.Ceq);
		}

		#endregion

		#endregion

		#region Call, New, InstanceOf, Static Property, Constant Operators Emission

		// warning: this EmitName does not emit conversion to string
		internal void EmitName(string fullName, Expression nameExpr, bool createChain)
		{
			EmitName(fullName, nameExpr, createChain, PhpTypeCode.Object);
		}

        internal void EmitName(string fullName, Expression nameExpr, bool createChain, PhpTypeCode dstType)
        {
            Debug.Assert(fullName != null ^ nameExpr != null);

            if (fullName != null)
            {
                il.Emit(OpCodes.Ldstr, fullName);
            }
            else
            {
                if (createChain) ChainBuilder.Create();
                EmitConversion(nameExpr, dstType);
                if (createChain) ChainBuilder.End();
            }
        }

		/// <summary>
		/// Emits a call to a routine with specified name using an operator.
		/// </summary>
        internal PhpTypeCode EmitRoutineOperatorCall(DType type, Expression targetExpr,
            string routineFullName, string fallbackRoutineFullname, Expression routineNameExpr, CallSignature callSignature, AccessType access)
        {
            Debug.Assert(routineFullName != null ^ routineNameExpr != null);

            MethodInfo operator_method;
            PhpTypeCode return_type_code;

            // (J) use call sites to call the method:
            if (targetExpr != null /*|| type != null*/)
            {
                Debug.Assert(fallbackRoutineFullname == null);

                return this.CallSitesBuilder.EmitMethodCall(this, Compiler.CodeGenerator.CallSitesBuilder.AccessToReturnType(access), targetExpr, type, routineFullName, routineNameExpr, callSignature);
            }
            else if (targetExpr != null)
            {
                Debug.Assert(fallbackRoutineFullname == null);

                // LOAD Operators.InvokeMethod(<target>, <method name>, <type desc>, <context>);

                // start a new operators chain (as the rest of chain is read)
                this.ChainBuilder.Create();
                this.ChainBuilder.Begin();
                this.ChainBuilder.Lengthen(); // for hop over ->

                // prepare for operator invocation
                this.EmitBoxing(targetExpr.Emit(this));
                this.ChainBuilder.End();

                this.EmitName(routineFullName, routineNameExpr, true);
                this.EmitLoadClassContext();
                this.EmitLoadScriptContext();

                if (routineFullName != null)
                    operator_method = Methods.Operators.InvokeMethodStr;
                else
                    operator_method = Methods.Operators.InvokeMethodObj;

                return_type_code = PhpTypeCode.PhpReference;
            }
            else if (type != null)
            {
                Debug.Assert(fallbackRoutineFullname == null);

                // LOAD Operators.InvokeStaticMethod(<type desc>, <method name>, <self>, <type desc>, context);
                type.EmitLoadTypeDesc(this, ResolveTypeFlags.UseAutoload | ResolveTypeFlags.ThrowErrors);

                this.EmitName(routineFullName, routineNameExpr, true);

                this.EmitLoadSelf();
                this.EmitLoadClassContext();
                this.EmitLoadScriptContext();

                operator_method = Methods.Operators.InvokeStaticMethod;
                return_type_code = PhpTypeCode.PhpReference;
            }
            else
            {
                Debug.Assert(routineNameExpr == null || fallbackRoutineFullname == null);   // (routineNameExpr != null) => (fallbackRoutineFullName == null)

                // LOAD ScriptContext.Call{|Void|Value}(<local variables>, <naming context>, <function name>, ref <hint>, context);
                this.EmitLoadRTVariablesTable();
                this.EmitLoadNamingContext();
                this.EmitName(routineFullName, routineNameExpr, true);
                if (fallbackRoutineFullname != null) il.Emit(OpCodes.Ldstr, fallbackRoutineFullname); else il.Emit(OpCodes.Ldnull); // fallback fcn name
                il.Emit(OpCodes.Ldsflda, il.TypeBuilder.DefineField("<callHint>'" + (routineFullName ?? "indirect"), typeof(PHP.Core.Reflection.DRoutineDesc), FieldAttributes.Static | FieldAttributes.Private));
                this.EmitLoadScriptContext();

                // (J) only necessary copying, dereferencing or reference making:
                if (access == AccessType.None)
                {
                    operator_method = Methods.ScriptContext.CallVoid;
                    return_type_code = PhpTypeCode.Void;
                }
                else if (access == AccessType.Read)
                {
                    operator_method = Methods.ScriptContext.CallValue;
                    return_type_code = PhpTypeCode.Object;
                }
                else
                {
                    operator_method = Methods.ScriptContext.Call;
                    return_type_code = PhpTypeCode.PhpReference;
                }
            }

            // emits load of parameters to the PHP stack:
            callSignature.EmitLoadOnPhpStack(this);

            // marks transient sequence point just before the call:
            this.MarkTransientSequencePoint();

            il.Emit(OpCodes.Call, operator_method);

            // marks transient sequence point just after the call:
            this.MarkTransientSequencePoint();

            return return_type_code;
        }

		private void EmitLoadTypeDesc(string typeFullName, TypeRef typeNameRef, DType type, ResolveTypeFlags flags)
		{
			Debug.AssertNonNull(1, typeFullName, typeNameRef, type);

			if (typeFullName != null)
				EmitLoadTypeDescOperator(typeFullName, null, flags);
			else if (typeNameRef != null)
				typeNameRef.EmitLoadTypeDesc(this, flags);
			else
				type.EmitLoadTypeDesc(this, flags);
		}

		internal void EmitLoadTypeDesc(Type/*!*/ realType)
		{
			Debug.Assert(realType != null);

			// TODO: keep in an internal field? cache the result in a local?
			// fields for all types whose type-descs are needed in a method
			// initialize at the entry to the method (or lazily...)
			il.Emit(OpCodes.Ldtoken, realType);
			il.Emit(OpCodes.Call, Methods.DTypeDesc_Create);
		}

        /// <summary>
        /// Pushes an array of genericArgs (object[]genericArgs) onto the evaluation stack. Can pushes null only.
        /// The array contains generic arguments of current PhpRoutine.
        /// </summary>
        private void EmitLoadGenericArgsArray()
        {
            var procedure = locationStack.Peek() as CompilerLocationStack.ProcedureDeclContext;
            if (procedure == null || procedure.PhpRoutine == null || procedure.PhpRoutine.Signature.GenericParamCount == 0)
            {
                il.Emit(OpCodes.Ldnull);
            }
            else
            {
                PhpRoutineSignature signature = procedure.PhpRoutine.Signature;
                il.EmitLoadInitializedArray(typeof(object), procedure.PhpRoutine.Signature.GenericParamCount * 2, delegate(ILEmitter _il, int _i)
                {
                    int genericParamIndex = _i / 2;
                    if ((_i&1) == 0)
                    {   // arg name
                        il.Emit(OpCodes.Ldstr, signature.GenericParams[genericParamIndex].Name.LowercaseValue);
                    }
                    else
                    {   // DTypeDesc
                        signature.GenericParams[genericParamIndex].EmitLoadTypeDesc(this, ResolveTypeFlags.None/*not used*/ );
                    }
                });
            }
        }

		internal void EmitLoadTypeDescOperator(string typeFullName, Expression typeNameExpr, ResolveTypeFlags flags)
		{
			Debug.Assert(typeFullName != null ^ typeNameExpr != null);

			// LOAD Convert.ObjectToTypeDesc(<name>, <use autoload>, <context type desc>, <script context>, <naming context>/*, <locals>*/);
			EmitName(typeFullName, typeNameExpr, false);
			il.LdcI4((int)flags);
			EmitLoadClassContext();
			EmitLoadScriptContext();
			EmitLoadNamingContext();
            EmitLoadGenericArgsArray();
            il.Emit(OpCodes.Call, (typeFullName != null) ? Methods.Convert.StringToTypeDesc : Methods.Convert.ObjectToTypeDesc);
		}

        /// <summary>
        /// Loads <see cref="DTypeDesc"/> of runtime type of current method.
        /// </summary>
        /// <param name="flags">Ignored.</param>
        internal void EmitLoadStaticTypeDesc(ResolveTypeFlags flags)
        {
            // if we have a type place, use it:
            if (this.LateStaticBindTypePlace != null)
            {
                this.LateStaticBindTypePlace.EmitLoad(this.il);
            }
            else
            {
                // not handled yet
                throw new NotImplementedException();
            }
        }

		internal void EmitNewOperator(string typeFullName, TypeRef typeNameRef, DType type, CallSignature callSignature)
		{
			Debug.AssertNonNull(1, typeFullName, typeNameRef, type);

			// prepare stack frame for the constructor:
			callSignature.EmitLoadOnPhpStack(this);

			// CALL Operators.New(<type desc>, <context type desc>, <context>);
			EmitLoadTypeDesc(typeFullName, typeNameRef, type, ResolveTypeFlags.UseAutoload | ResolveTypeFlags.ThrowErrors);
			this.EmitLoadClassContext();
			this.EmitLoadScriptContext();
			this.EmitLoadNamingContext();
			il.Emit(OpCodes.Call, Methods.Operators.New);
		}

		internal void EmitInstanceOfOperator(string typeFullName, TypeRef typeNameRef, DType type)
		{
			Debug.AssertNonNull(1, typeFullName, typeNameRef, type);

			// LOAD Operators.InstanceOf(STACK, <type desc>);
			EmitLoadTypeDesc(typeFullName, typeNameRef, type, ResolveTypeFlags.None);
			il.Emit(OpCodes.Call, Methods.Operators.InstanceOf);
		}

		internal void EmitTypeOfOperator(string typeFullName, TypeRef typeNameRef, DType type)
		{
			Debug.AssertNonNull(1, typeFullName, typeNameRef, type);

			// LOAD Operators.InstanceOf(STACK, <type desc>, <context type desc>, <script context>);
			EmitLoadTypeDesc(typeFullName, typeNameRef, type, ResolveTypeFlags.UseAutoload | ResolveTypeFlags.ThrowErrors);
			il.Emit(OpCodes.Call, Methods.Operators.TypeOf);
		}

		internal PhpTypeCode EmitGetStaticPropertyOperator(DType/*!*/ type,
			string propertyFullName, Expression propertyNameExpr, bool getReference)
		{
			Debug.Assert(type != null && (propertyFullName != null ^ propertyNameExpr != null));

			// LOAD GetStaticProperty[Ref](<type name>, <field name>, <type desc>, <context>, [quiet]);
			type.EmitLoadTypeDesc(this, ResolveTypeFlags.UseAutoload | ResolveTypeFlags.ThrowErrors);
			EmitName(propertyFullName, propertyNameExpr, false);
			EmitLoadClassContext();
			EmitLoadScriptContext();

			// invoke the operator
			if (getReference)
			{
				il.Emit(OpCodes.Call, Methods.Operators.GetStaticPropertyRef);
				return PhpTypeCode.PhpReference;
			}
			else
			{
				il.LdcI4(this.ChainBuilder.QuietRead ? 1 : 0);
				il.Emit(OpCodes.Call, Methods.Operators.GetStaticProperty);
				return PhpTypeCode.Object;
			}
		}

		internal AssignmentCallback/*!*/ EmitSetStaticPropertyOperator(DType/*!*/ type,
			string propertyFullName, Expression propertyNameExpr, bool setReference)
		{
			Debug.Assert(type != null && (propertyFullName != null ^ propertyNameExpr != null));

			// we need to check the visibility => invoke the operator:
			type.EmitLoadTypeDesc(this, ResolveTypeFlags.UseAutoload | ResolveTypeFlags.ThrowErrors);
			EmitName(propertyFullName, propertyNameExpr, false);

			return delegate(CodeGenerator/*!*/ codeGen, PhpTypeCode stackTypeCode)
			{
				codeGen.EmitLoadClassContext();
				codeGen.EmitLoadScriptContext();

				// invoke the operator
				codeGen.IL.Emit(OpCodes.Call, Methods.Operators.SetStaticProperty);
			};
		}

		internal void EmitUnsetStaticPropertyOperator(DType/*!*/ type, string propertyFullName,
			Expression propertyNameExpr)
		{
			Debug.Assert(type != null && (propertyFullName != null ^ propertyNameExpr != null));

			// CALL Operators.UnsetStaticProperty(<type desc>, <field name>, <type desc>, <context>) 
			type.EmitLoadTypeDesc(this, ResolveTypeFlags.UseAutoload | ResolveTypeFlags.ThrowErrors);

			EmitName(propertyFullName, propertyNameExpr, false);
			EmitLoadClassContext();
			EmitLoadScriptContext();

			il.Emit(OpCodes.Call, Methods.Operators.UnsetStaticProperty);
		}

		internal void EmitGetConstantValueOperator(DType type, string/*!*/ constantFullName, string constantFallbackName)
		{
			if (type != null)
			{
                Debug.Assert(constantFallbackName == null);

				// CALL Operators.GetClassConstant(<type desc>, <constant name>, <type context>, <script context>);
				type.EmitLoadTypeDesc(this, ResolveTypeFlags.UseAutoload | ResolveTypeFlags.ThrowErrors);

				il.Emit(OpCodes.Ldstr, constantFullName);

				EmitLoadClassContext();
				EmitLoadScriptContext();

				il.EmitCall(OpCodes.Call, Methods.Operators.GetClassConstant, null);
			}
			else
			{
				// CALL context.GetConstantValue(name);
				EmitLoadScriptContext();
				il.Emit(OpCodes.Ldstr, constantFullName);
                if (constantFallbackName != null) il.Emit(OpCodes.Ldstr, constantFallbackName); else il.Emit(OpCodes.Ldnull);
				il.Emit(OpCodes.Call, Methods.ScriptContext.GetConstantValue);
			}
		}

		#endregion


		#region Routines Body Emission (Tomas Matousek)

		/// <summary>
		/// Emits a body of an arg-full function or method overload.
		/// </summary>
		public void EmitArgfullOverloadBody(PhpRoutine/*!*/ routine, List<Statement>/*!*/ body, Position entirePosition, ShortPosition declarationBodyPosition)
		{
			Debug.Assert(!routine.IsAbstract);

            if (context.Config.Compiler.Debug)
            {
                if (!routine.IsLambda)
                {
                    MarkSequencePoint(declarationBodyPosition.Line, declarationBodyPosition.Column,
                        declarationBodyPosition.Line, declarationBodyPosition.Column + 1);
                }
                il.Emit(OpCodes.Nop);

                EmitArgsAwareCheck(routine);
            }

			// declares and initializes real locals (should be before args init):
			EmitArgfullLocalsInitialization(routine);

			// initializes locals (from arguments or by empty value):
			EmitArgfullArgsInitialization(routine);

            // remember late static bind type from <stack>
            EmitArgfullLateStaticBindTypeInitialization(routine);

            // custom body prolog emittion:
            PluginHandler.EmitBeforeBody(il, body);

			// define user labels:
			DefineLabels(routine.Builder.Labels);

			// emits function's body:
			foreach (Statement statement in body)
				statement.Emit(this);

			// marks ending "}" as the last sequence point of the routine:
			// (do not mark it in lambda functions as they are created from source code without braces);
			if (!routine.IsLambda)
			{
				MarkSequencePoint(entirePosition.LastLine, entirePosition.LastColumn,
					entirePosition.LastLine, entirePosition.LastColumn + 1);
			}else if (context.Config.Compiler.Debug)
            {
                il.Emit(OpCodes.Nop);
            }

			EmitRoutineEpilogue(null, false);
		}

        /// <summary>
        /// Emit check whether the argsaware routine was called properly with <see cref="PhpStack"/> initialized.
        /// </summary>
        /// <remarks>Emitted code is equivalent to <code>context.Stack.ThrowIfNotArgsaware(routine.Name.Value);</code></remarks>
        private void EmitArgsAwareCheck(PhpRoutine/*!*/ routine)
        {
            if (routine.IsArgsAware)
            {
                //  <context>.Stack.ThrowIfNotArgsaware(routine.Name.Value)
                this.EmitLoadScriptContext();   // <context>
                this.IL.Emit(OpCodes.Ldfld, Fields.ScriptContext_Stack);    // .Stack
                this.IL.Emit(OpCodes.Ldstr, routine.Name.Value);    // routine.Name.Value
                this.IL.Emit(OpCodes.Call, Methods.PhpStack.ThrowIfNotArgsaware);   // .call ThrowIfNotArgsaware
            }
        }

		/// <summary>
		/// Declares all locals used in a function.
		/// </summary>
		private void EmitArgfullLocalsInitialization(PhpRoutine/*!*/ routine)
		{
			bool optimized = (routine.Properties & RoutineProperties.HasUnoptimizedLocals) == 0;
			bool rt_var_table = (routine.Properties & RoutineProperties.HasRTVariablesTable) != 0;

			// TODO: MarkSequencePoint(0xFeeFee, 0xFeeFee, 0xFeeFee, 0xFeeFee);

			// emits creation of a new table of variables if it will be used in a function:
			if (rt_var_table)
			{
				il.LdcI4(routine.Builder.LocalVariables.Count);
				il.Emit(OpCodes.Newobj, PhpVariable.RTVariablesTableCtor);
				RTVariablesTablePlace.EmitStore(il);
			}

			if (optimized)
			{
				// declares and initializes real locals (skips arguments):
				foreach (VariablesTable.Entry entry in routine.Builder.LocalVariables)
				{
					if (!entry.IsParameter)
					{
						LocalBuilder local;

						if (entry.IsPhpReference)
						{
							local = il.DeclareLocal(Types.PhpReference[0]);

							// local = new PhpReference();
							il.Emit(OpCodes.Newobj, Constructors.PhpReference_Void);
							il.Stloc(local);
						}
						else
						{
							local = il.DeclareLocal(Types.Object[0]);
						}

						// stores local to table:
						entry.Variable = new Place(local);

						// gives locals names (if they are not parameters):
						if (sourceUnit.SymbolDocumentWriter != null)
							local.SetLocalSymInfo(entry.VariableName.Value);
					}
				}
			}
		}

		/// <summary>
		/// Emits initialization of arg-full argument variables.
		/// </summary>
		private void EmitArgfullArgsInitialization(PhpRoutine/*!*/ routine)
		{
			bool optimized = (routine.Properties & RoutineProperties.HasUnoptimizedLocals) == 0;
			bool indirect_local_access = (routine.Properties & RoutineProperties.IndirectLocalAccess) != 0;

			int real_index = routine.FirstPseudoGenericParameterIndex;
			int index = 0;

			foreach (GenericParameter param in routine.Signature.GenericParams)
			{
				if (param.DefaultType != null)
				{
					Label endif_label = il.DefineLabel();

					// IF ARG[real_index] == Arg.DefaultType) THEN
					il.Ldarg(real_index);
					il.Emit(OpCodes.Ldsfld, Fields.Arg_DefaultType);
					il.Emit(OpCodes.Bne_Un, endif_label);

					// ARG[real_index] = <typedesc(param.DefaultType)>;
					param.DefaultType.EmitLoadTypeDesc(this, ResolveTypeFlags.UseAutoload | ResolveTypeFlags.ThrowErrors);
					il.Starg(real_index);

					// ENDIF;
					il.MarkLabel(endif_label);
				}

                // add the DTypeDesc into locals
                /*{
                    EmitLoadRTVariablesTable();
                    il.Emit(OpCodes.Ldstr, "!" + param.Name.ToString().ToLower());

                    // LOAD ARG[arg_idx];
                    il.Ldarg(real_index);

                    // stores argument to table:
                    il.Emit(OpCodes.Callvirt, PhpVariable.RTVariablesTableAdder);
                }*/

				real_index++;
				index++;
			}

			real_index = routine.FirstPhpParameterIndex;
			index = 0;

			foreach (FormalParam param in routine.Builder.Signature.FormalParams)
			{
				// sets variables place in the table:
				VariablesTable.Entry entry = routine.Builder.LocalVariables[param.Name];

                bool optional = index >= routine.Signature.MandatoryParamCount;

				// only variables accessible by function's code are initialized;
				// these are 
				// - all if function has unoptimized locals or contains indirect access 
				//   (which doesn't imply unoptimized locals)
				// - those which are directly used 
				if (!optimized || indirect_local_access || entry.IsDirectlyUsed)
				{
					// if the argument is reference => the local should also be a reference:
					Debug.Assert(!param.PassedByRef || entry.IsPhpReference);

					// marks a sequence point if a parameter is initialized or type hinted:
					if (optional || param.TypeHint != null)
					{
						this.MarkSequencePoint(
							param.Position.FirstLine,
							param.Position.FirstColumn,
							param.Position.LastLine,
							param.Position.LastColumn + 2);
					}

					if (optional)
					{
						Label end_label = il.DefineLabel();
						Label else_label = il.DefineLabel();

						// IF (ARG[arg_idx]!=Arg.Default) THEN
						il.Ldarg(real_index);
						il.Emit(OpCodes.Ldsfld, Fields.Arg_Default);
						il.Emit(OpCodes.Beq_S, else_label);

						// emits deep copying (if not reference):
						EmitArgumentCopying(real_index, param);

						// ELSE;
						il.Emit(OpCodes.Br, end_label);
						il.MarkLabel(else_label);

						// ARG[arg_idx] = <default value>;
						EmitLoadArgumentDefaultValue(index, param, routine.FullName);
						il.Starg(real_index);

						// END IF;
						il.MarkLabel(end_label);
					}
					else
					{
						// emits deep copying (if not reference):
						EmitArgumentCopying(real_index, param);
					}

					// emits type hint test (if specified):
					param.EmitTypeHintTest(this);

					// stores argument value to the local variable or to the table //

					// prepares evaluation stack for call to <variables_table>.Add(<name>,ARG[arg_idx]):
					if (!optimized)
					{
						EmitLoadRTVariablesTable();
						il.Emit(OpCodes.Ldstr, param.Name.ToString());

						// LOAD ARG[arg_idx];
						il.Ldarg(real_index);

                        // "box" to reference (if the local is a reference and argument is not a reference):
						if (entry.IsPhpReference && !param.PassedByRef)
							il.Emit(OpCodes.Newobj, Constructors.PhpReference_Object);

						// stores argument to table:
						il.Emit(OpCodes.Callvirt, PhpVariable.RTVariablesTableAdder);
					}
					else if (entry.IsPhpReference && !param.PassedByRef)
					{
						// local variable is stored in a new reference local:
						LocalBuilder local = il.DeclareLocal(typeof(PhpReference));
						entry.Variable = new Place(local);

						// "box" to reference (if the local is a reference and argument is not a reference):
						il.Ldarg(real_index);
						il.Emit(OpCodes.Newobj, Constructors.PhpReference_Object);
						il.Stloc(local);
					}
					else
					{
						// local variable is stored in the argument:
						entry.Variable = new IndexedPlace(PlaceHolder.Argument, real_index);
					}
				}
				else
				{
                    if (param.TypeHint != null)
                    {
                        if (optional)
                        {
                            // convert Arg.Default to proper default value (so TypeHint test will check the proper value):

                            Label is_default = il.DefineLabel();
                            Label end_if = il.DefineLabel();

                            // if (ARG[arg_idx] = Arg.Default) THEN
                            il.Ldarg(real_index);
                            il.Emit(OpCodes.Ldsfld, Fields.Arg_Default);
                            il.Emit(OpCodes.Beq_S, is_default);
                            il.Emit(OpCodes.Br_S, end_if);
                            {
                                // ARG[arg_idx] = <default value>;
                                il.MarkLabel(is_default);
                                EmitLoadArgumentDefaultValue(index, param, routine.FullName);
                                il.Starg(real_index);
                            }
                            il.MarkLabel(end_if);
                        }

                        // emits type hint test (if specified):
                        param.EmitTypeHintTest(this);
                    }
				}

				real_index++;
				index++;
			}
		}

        /// <summary>
        /// Stores late static binding type information if necessary.
        /// </summary>
        private void EmitArgfullLateStaticBindTypeInitialization(PhpRoutine/*!*/routine)
        {
            if (routine == null || !routine.UsesLateStaticBinding)
                return;

            if (routine.IsMethod)
            {
                if (routine.IsStatic)
                {
                    // static method,
                    // reads <context>.Stack.LateStaticBindType,
                    // saves it into a local variable:

                    // <context>.Stack.LateStaticBindType
                    this.EmitLoadScriptContext();
                    this.il.Emit(OpCodes.Ldfld, Fields.ScriptContext_Stack);
                    this.il.Emit(OpCodes.Ldfld, Fields.PhpStack_LateStaticBindType);

                    // DTypeDesc <loc_lsb> =
                    this.LateStaticBindTypePlace = new IndexedPlace(il.DeclareLocal(Types.DTypeDesc[0]));
                    this.LateStaticBindTypePlace.EmitStore(il);
                }
                else
                {
                    // instance method,
                    // uses ((DObject)this).TypeDesc
                    
                    Debug.Assert(this.SelfPlace != null && this.SelfPlace != LiteralPlace.Null, "SelfPlace expected to be non-NULL");
                    this.LateStaticBindTypePlace = new MethodCallPlace(Properties.DObject_TypeDesc.GetGetMethod(), false, this.SelfPlace);
                }
            }
            else
            {
                this.LateStaticBindTypePlace = LiteralPlace.Null;
            }

            
        }

		/// <summary>
		/// Emits non-reference argument deep copying.
		/// </summary>
		private void EmitArgumentCopying(int idx, FormalParam param)
		{
			if (!param.PassedByRef)
			{
				// ARG[idx] = PhpVariable.Copy(ARG[idx],CopyReason.PassedByCopy);
				il.Ldarg(idx);
				il.LdcI4((int)CopyReason.PassedByCopy);
				il.Emit(OpCodes.Call, Methods.PhpVariable.Copy);
				il.Starg(idx);
			}
		}

		/// <summary>
		/// Emits a default value load of a specified optional argument. 
		/// </summary>
		/// <param name="realIndex">A real index of the argument starting from 0.</param>
		/// <param name="param">The parameter.</param>
		/// <param name="calleeFullName">A name of the target function or method.</param>
		private void EmitLoadArgumentDefaultValue(int realIndex, FormalParam param, string calleeFullName)
		{
			// optional argument with a default value:
			if (param.InitValue != null)
			{
				EmitBoxing(param.InitValue.Emit(this));
			}
			else
			{
				// optional argument without a default value

				// CALL PhpException.MissingArgument(<realIndex+1>,<calleeName>);
				il.LdcI4(realIndex + 1);
				il.Emit(OpCodes.Ldstr, calleeFullName);
				EmitPhpException(Methods.PhpException.MissingArgument);

				// LOAD null;
				il.Emit(OpCodes.Ldnull);
			}

			// if the param is a references => "box" into a reference:
			if (param.PassedByRef)
				il.Emit(OpCodes.Newobj, Constructors.PhpReference_Object);
		}

		/// <summary>
		/// Emits instructions to conform a required access type.
		/// </summary>
		/// <param name="callExpression">Expression emitting the call.</param>
		/// <param name="loadAddress">Whether to load an address of the return value.</param>
		/// <param name="result">The type code of a top item of the evaluation stack.</param>
		public void EmitReturnValueHandling(Expression/*!*/ callExpression, bool loadAddress, ref PhpTypeCode result)
		{
			Debug.Assert(callExpression != null);

			if (loadAddress)
			{
                if (result == PhpTypeCode.PhpReference)
				{
					// LOADADDR STACK.Value;
					il.Emit(OpCodes.Ldflda, Fields.PhpReference_Value);
				}
				else
				{
                    if (result == PhpTypeCode.Void)
                        il.Emit(OpCodes.Ldnull);
                    
                    // local = STACK; 
					// LOADADDR local;
					LocalBuilder local = il.GetTemporaryLocal(PhpTypeCodeEnum.ToType(result), true);
					il.Stloc(local);
					il.Ldloca(local);
				}
				result = PhpTypeCode.ObjectAddress;
				return;
			}

			switch (callExpression.Access)
			{
				case AccessType.None:

					// return value is discarded:
					if (result != PhpTypeCode.Void)
					{
						il.Emit(OpCodes.Pop);
						result = PhpTypeCode.Void;
					}
					break;

				case AccessType.ReadUnknown:
				case AccessType.ReadRef:

					if (result != PhpTypeCode.PhpReference)
					{
						// return value is "boxed" to PhpReference:
						if (result != PhpTypeCode.Void)
						{
							EmitBoxing(result);
							EmitVariableCopy(CopyReason.ReturnedByCopy, callExpression);
							il.Emit(OpCodes.Newobj, Constructors.PhpReference_Object);
						}
						else
						{
							il.Emit(OpCodes.Newobj, Constructors.PhpReference_Void);
						}

						result = PhpTypeCode.PhpReference;
					}
					break;

				case AccessType.Read:

					if (result == PhpTypeCode.PhpReference)
					{
						// return value is dereferenced:
						il.Emit(OpCodes.Ldfld, Fields.PhpReference_Value);
						result = PhpTypeCode.Object;
					}
					else if (result == PhpTypeCode.Void)
					{
						// null value is loaded as a result:
						il.Emit(OpCodes.Ldnull);
						result = PhpTypeCode.Object;
					}
					break;

				default:
					Debug.Fail();
					break;
			}
		}

		/// <summary>
		/// Emits a load of the value stored to the result place if available.
		/// </summary>
		public void EmitRoutineEpilogue(GlobalCode globalCode, bool transient)
		{
            if (globalCode != null && globalCode.AppendedInclusion != null)
			{
				// marks the return label, however return value is ignored since it is 
				// overriden by appended script's return value (TODO: HOW DOES PHP BEHAVE?):
				if (ResultPlace != null)
					il.MarkLabel(ReturnLabel);

				// IF (<is main script>) LOAD <appended file script>.Main(...):
				globalCode.AppendedInclusion.Emit(this);

				// returns the value retured by the appended script:
				il.Emit(OpCodes.Ret);
			}
			else
			{
				if (globalCode != null && !transient)
				{
					// returns default value of the Main routine:
					il.LoadLiteral(ScriptModule.DefaultMainReturnValue);
					il.Emit(OpCodes.Box, ScriptModule.DefaultMainReturnValue.GetType());
				}
				else
				{
					// function or method contains no return statement:
					if (ReturnsPhpReference)
						il.Emit(OpCodes.Newobj, Constructors.PhpReference_Void);
					else
						il.Emit(OpCodes.Ldnull);
				}

				il.Emit(OpCodes.Ret);

				if (ResultPlace != null)
				{
					// there is a return statement branching to the ReturnLabel and returning value
					// stored in the ReturnPlace:
					il.MarkLabel(ReturnLabel);
					ResultPlace.EmitLoad(il);
					il.Emit(OpCodes.Ret);
				}
			}
		}

		/// <summary>
		/// Emits a store of the value on the top of the eval. stack to the result place.
		/// Creates a local variable backing the result place if it hasn't been created yet.
		/// </summary>
		public void EmitReturnBranch()
		{
			// no return branch has been emitted yet -> declare result and return label:
			if (ResultPlace == null)
			{
				LocalBuilder result_local;
				if (ReturnsPhpReference)
					result_local = il.DeclareLocal(typeof(PhpReference));
				else
					result_local = il.DeclareLocal(typeof(object));

				ResultPlace = new Place(result_local);
				ReturnLabel = il.DefineLabel();
			}

			// stores top of the stack to the result place:
			ResultPlace.EmitStore(il);

			// emit branch or leave:
			if (ExceptionBlockNestingLevel > 0)
				il.Emit(OpCodes.Leave, ReturnLabel);
			else
				il.Emit(OpCodes.Br, ReturnLabel);
		}

		internal void DefineLabels(Dictionary<VariableName, Statement>/*!*/ labels)
		{
			foreach (Statement stmt in labels.Values)
				((LabelStmt)stmt).Label = il.DefineLabel();
		}

		#endregion


		#region Ghost GetUserEntryPoint/Property Implement Stub Emission (Ladislav Prosek)

		/// <summary>
		/// Emits ghost stubs for methods and properties that are declared by a <paramref name="type"/>'s
		/// base type but need to be adapted to a particular CLR signature because of implementing an
		/// interface by the <paramref name="type"/>.
		/// </summary>
		/// <param name="type">The <see cref="PhpType"/> that possibly released ghosts by implementing an interface.
		/// </param>
		public void EmitGhostStubs(PhpType/*!*/ type)
		{
			List<KeyValuePair<DMemberRef, DMemberRef>> ghosts = type.Builder.GhostImplementations;
			if (ghosts != null)
			{
				Dictionary<string, MethodBuilder> m_stubs = new Dictionary<string, MethodBuilder>();
				Dictionary<Type, PropertyBuilder> f_stubs = new Dictionary<Type, PropertyBuilder>();

				for (int i = 0; i < ghosts.Count; i++)
				{
					PhpMethod impl_method;
					PhpField impl_field;

					if ((impl_method = ghosts[i].Value.Member as PhpMethod) != null)
					{
						// emit ghost method stub
						EmitOverrideStubs(m_stubs, impl_method, ghosts[i].Value.Type, type, ghosts[i].Key, true);
					}
					else if ((impl_field = ghosts[i].Value.Member as PhpField) != null)
					{
						// emit ghost property stub
						EmitOverrideStubs(f_stubs, impl_field, type, ghosts[i].Key, true);
					}
				}
			}
		}

		#endregion

		#region Property Override/Implement/Export Stub Emission (Ladislav Prosek)

		/// <summary>
		/// Emits stubs for overriden/implemented properties and explicit export stubs.
		/// </summary>
		/// <param name="field">The overriding/implementing/exported field.</param>
		public void EmitOverrideAndExportStubs(PhpField/*!*/ field)
		{
			// keep track of property types that have already been generated
			Dictionary<Type, PropertyBuilder> stubs = null;

			// emit explicit export stub
			// (note: the property builder is already defined - needed for custom attributes usage)

			if (field.IsExported)
			{
				Debug.Assert(field.ExportedPropertyBuilder != null);

				PropertyBuilder prop_builder = field.ExportedPropertyBuilder;
				EmitFieldExportStubs(field, prop_builder);

				stubs = new Dictionary<Type, PropertyBuilder>();
				stubs.Add(prop_builder.PropertyType, prop_builder);
			}
			// emit stubs for overriden property

			if (field.Overrides != null)
			{
				if (stubs == null) stubs = new Dictionary<Type, PropertyBuilder>();
				EmitOverrideStubs(stubs, field, field.DeclaringPhpType, field.Overrides, false);
			}

			if (field.Implements != null)
			{
				// emit stubs(s) for implemented property/ies

				for (int i = 0; i < field.Implements.Count; i++)
				{
					if (stubs == null) stubs = new Dictionary<Type, PropertyBuilder>();
					EmitOverrideStubs(stubs, field, field.DeclaringPhpType, field.Implements[i], true);
				}
			}
		}

		/// <summary>
		/// Emits property stubs for a overriden or implemented CLR property.
		/// </summary>
		/// <param name="stubs">Already generated stubs.</param>
		/// <param name="target">The overriding/implementing field.</param>
		/// <param name="declaringType">The type where the stubs should be emitted.</param>
		/// <param name="template">The property being overriden/implemented.</param>
		/// <param name="newSlot"><B>True</B> if the stub should be assigned a new vtable slot,
		/// <B>false</B> otherwise.</param>
		private void EmitOverrideStubs(IDictionary<Type, PropertyBuilder>/*!*/ stubs, PhpField/*!*/ target,
			PhpType/*!*/ declaringType, DMemberRef/*!*/ template, bool newSlot)
		{
			ClrProperty clr_template = template.Member as ClrProperty;
			if (clr_template == null) return;

			MethodInfo getter = clr_template.Getter;
			MethodInfo setter = clr_template.Setter;

			// we're only interested in non-final virtual getters/setters
			if (getter != null && (!getter.IsVirtual || getter.IsFinal)) getter = null;
			if (setter != null && (!setter.IsVirtual || setter.IsFinal)) setter = null;

			ConstructedType constructed_type = template.Type as ConstructedType;

			// map property type according to constructed type
			Type property_type = clr_template.RealProperty.PropertyType;
			if (constructed_type != null) property_type = constructed_type.MapRealType(property_type);

			// do we already have getter/setter of this type?
			PropertyBuilder prop_builder;
			if (stubs.TryGetValue(property_type, out prop_builder))
			{
				if (prop_builder.GetGetMethod(true) != null) getter = null;
				if (prop_builder.GetSetMethod(true) != null) setter = null;
			}

			if (getter != null || setter != null)
			{
				if (prop_builder == null)
				{
					// the property might already exist - we could be just adding an accessor
					TypeBuilder type_builder = declaringType.RealTypeBuilder;

					prop_builder = type_builder.DefineProperty(
						clr_template.Name.ToString(),
						Reflection.Enums.ToPropertyAttributes(target.MemberDesc.MemberAttributes),
						property_type,
						Type.EmptyTypes);

					stubs.Add(property_type, prop_builder);
				}

				if (getter != null)
				{
					// add getter
					MethodBuilder getter_builder = DefineOverrideAccessor(
						declaringType,
						target,
						getter,
						newSlot,
						property_type,
						Type.EmptyTypes);

					prop_builder.SetGetMethod(getter_builder);

					EmitFieldExportGetter(target, prop_builder, getter_builder);
				}

				if (setter != null)
				{
					// add setter
					MethodBuilder setter_builder = DefineOverrideAccessor(
						declaringType,
						target,
						setter,
						newSlot,
						Types.Void,
						new Type[] { property_type });

					prop_builder.SetSetMethod(setter_builder);

					EmitFieldExportSetter(target, prop_builder, setter_builder);
				}
			}
		}

		/// <summary>
		/// Defines a property accessor method and installs an explicit override if necessary.
		/// </summary>
		private MethodBuilder/*!*/ DefineOverrideAccessor(PhpType/*!*/ declaringType, PhpField/*!*/ target,
			MethodInfo/*!*/ template, bool newSlot, Type/*!*/ returnType, Type[]/*!!*/ paramTypes)
		{
			bool changed;
			string name = ClrStubBuilder.GetNonConflictingMethodName(declaringType.TypeDesc, template.Name, out changed);

			MethodAttributes attr;

			if (changed) attr = MethodAttributes.PrivateScope;
			else attr = Reflection.Enums.ToMethodAttributes(target.MemberDesc.MemberAttributes);

			attr |= (MethodAttributes.Virtual | MethodAttributes.HideBySig);
			if (newSlot) attr |= MethodAttributes.NewSlot;

			MethodBuilder method_builder = declaringType.RealTypeBuilder.DefineMethod(
				name,
				attr,
				returnType,
				paramTypes);

			if (changed)
			{
				declaringType.RealTypeBuilder.DefineMethodOverride(
					method_builder,
					template);
			}

			return method_builder;
		}

		#endregion

		#region GetUserEntryPoint Override/Implement/Export Stub Emission (Ladislav Prosek)

		/// <summary>
		/// Emits stubs for overridden/implemented methods and explicit export stubs.
		/// </summary>
		/// <param name="method">The overriding/implementing/exported method.</param>
		/// <remarks>
		/// If the <paramref name="method"/> implements or overrides a CLR method (or methods),
		/// appropriate stub(s) are generated and emitted to its declaring type. In addition,
		/// if the method is exported using the <c>Export</c> pseudo-custom attribute, stub(s)
		/// created according to its signature (type hints, default params, etc.) are also
		/// generated.
		/// </remarks>
		public void EmitOverrideAndExportStubs(PhpMethod/*!*/ method)
		{
			// keep track of the signatures that have already been generated
			Dictionary<string, MethodBuilder> stubs = null;

			// emit stub(s) for overridden method(s)

			if (method.Overrides != null)
			{
				stubs = new Dictionary<string, MethodBuilder>();
				EmitOverrideStubs(stubs, method, method.DeclaringPhpType, method.DeclaringPhpType,
					method.Overrides, false);
			}

			if (method.Implements != null)
			{
				// emit stub(s) for implemented method(s)

				for (int i = 0; i < method.Implements.Count; i++)
				{
					if (stubs == null) stubs = new Dictionary<string, MethodBuilder>();
					EmitOverrideStubs(stubs, method, method.DeclaringPhpType, method.DeclaringPhpType,
						method.Implements[i], true);
				}
			}

			// emit explicit export stubs

			if (method.IsExported)
			{
				if (stubs == null) stubs = new Dictionary<string, MethodBuilder>();
				EmitExportStubs(stubs, method);
			}
		}

		/// <summary>
		/// Emits stubs for all overloads of one overridden or implemented method.
		/// </summary>
		/// <param name="stubs">Already generated stubs.</param>
		/// <param name="target">The overriding/implementing method.</param>
		/// <param name="targetType">The type (perhaps constructed) that declared <paramref name="target"/>.</param>
		/// <param name="declaringType">The type where the stubs should be emitted.</param>
		/// <param name="template">The method being overridden/implemented.</param>
		/// <param name="newSlot"><B>True</B> if the stub should be assigned a new vtable slot,
		/// <B>false</B> otherwise.</param>
		private void EmitOverrideStubs(IDictionary<string, MethodBuilder>/*!*/ stubs, PhpMethod/*!*/ target,
			DType/*!*/ targetType, PhpType/*!*/ declaringType, DMemberRef/*!*/ template, bool newSlot)
		{
			ClrMethod clr_template = template.Member as ClrMethod;
			if (clr_template == null)
			{
                if (!target.IsStatic)
				    EmitOverrideStubsForPhpTemplate(stubs, target, targetType, declaringType, template, newSlot);

				return;
			}

            //
            // following code emits stubs in case of CLR base method
            //

			ConstructedType constructed_type = template.Type as ConstructedType;
			TypeBuilder type_builder = declaringType.RealTypeBuilder;

			// override all virtual non-final overloads
			foreach (ClrMethod.Overload overload in clr_template.Overloads)
			{
				if (overload.Method.IsVirtual && !overload.Method.IsFinal)
				{
					// map generic type parameters according to the constructed type
					Type constructed_return_type;
					ParameterInfo[] constructed_params = overload.MakeConstructed(constructed_type, out constructed_return_type);

					// check whether we have not generated this signature before
					string clr_sig = ClrMethod.Overload.ClrSignatureToString(
						overload.GenericParamCount,
						constructed_params,
						constructed_return_type);

					if (stubs.ContainsKey(clr_sig)) continue;

					Type[] param_types = new Type[constructed_params.Length];

					for (int j = 0; j < param_types.Length; j++)
					{
						param_types[j] = constructed_params[j].ParameterType;
					}

					// determine the stub attributes
					MethodAttributes attr;
					string name;

					name = overload.Method.Name;
					attr = Reflection.Enums.ToMethodAttributes(target.MemberDesc.MemberAttributes);
					attr |= (MethodAttributes.Virtual | MethodAttributes.HideBySig);

					if (newSlot) attr |= MethodAttributes.NewSlot;

					MethodBuilder overload_builder = type_builder.DefineMethod(name, attr);

					if (overload.MandatoryGenericParamCount > 0)
					{
						// define the same generic parameters that are defined for the overridden method
						// (the same constraints but possibly having different names)
						ClrStubBuilder.DefineStubGenericParameters(
							overload_builder,
							overload.GenericParameters,
							target.Signature,
							param_types);
					}

					overload_builder.SetReturnType(constructed_return_type);
					overload_builder.SetParameters(param_types);

					// set parameter names and attributes
					ClrStubBuilder.DefineStubParameters(overload_builder,
						target.Builder.Signature.FormalParams, constructed_params);

					if (!overload_builder.IsAbstract)
					{
						EmissionContext emission_context = SetupStubPlaces(target.DeclaringPhpType, false);

						try
						{
							// convert parameters and invoke the target
							ClrStubBuilder.EmitMethodStubBody(
								new ILEmitter(overload_builder),
								ScriptContextPlace,
								constructed_params,
								overload.GenericParameters,
								constructed_return_type,
								target,
								targetType);
						}
						finally
						{
							RestorePlaces(emission_context);
						}
					}

					stubs.Add(clr_sig, overload_builder);
				}
			}
		}

		/// <summary>
		/// Emits stubs for one overridden or implemented PHP method.
		/// </summary>
		/// <param name="stubs">Already generated stubs.</param>
		/// <param name="target">The overriding/implementing method.</param>
		/// <param name="targetType">The type (perhaps constructed) that declared <paramref name="target"/>.</param>
		/// <param name="declaringType">The type where the stubs should be emitted.</param>
		/// <param name="template">The method being overridden/implemented (surely PHP).</param>
		/// <param name="newSlot"><B>True</B> if the stub should be assigned a new vtable slot,
		/// <B>false</B> otherwise.</param>
		/// <remarks>
		/// This method handles situations where method overriding/implementing does not work by itself because of
		/// the fact that method names in PHP are case insensitive.
		/// </remarks>
		private void EmitOverrideStubsForPhpTemplate(IDictionary<string, MethodBuilder>/*!*/ stubs,
			PhpMethod/*!*/ target, DType/*!*/ targetType, PhpType/*!*/ declaringType, DMemberRef/*!*/ template,
			bool newSlot)
		{
            PhpMethod php_template = (PhpMethod)template.Member;

            // Emit method stub if needed here ... (resolve .NET incompatibility of base method and overriding method)
            // 
            // Until now, several possible cases or their combination are known:
            // - base and overriding methods match, but their name letter-casing don't (need to define override explicitly to properly Bake the type)
            // - base and overriding methods name match exactly, but overriding methods has additional arguments (with default values) (in PHP it is allowed) (stub needed)
            // - ghost stub, where B extends A implements I, where A contains definition of method in I and casing does not match
            //
            // if signatures don't match, virtual sealed stub must be created, it only calls the target method
            // if signatures match, only explicit override must be stated

            if (target.Name.ToString() != php_template.Name.ToString() ||           // the names differ (perhaps only in casing)
                target.Signature.ParamCount != php_template.Signature.ParamCount    // signature was extended (additional arguments added, with implicit value only)
                )
			{
				MethodInfo target_argfull = DType.MakeConstructed(target.ArgFullInfo, targetType as ConstructedType);
				TypeBuilder type_builder = declaringType.RealTypeBuilder;

				// we have to generate a pass-thru override stub that overrides the template based on
				// name since it is impossible to install an explicit override of a method declared by
				// a generic type in v2.0 SRE (feedback ID=97425)
				bool sre_bug_workaround = (template.Type is ConstructedType);

                if (target.DeclaringType == declaringType && !sre_bug_workaround && target.Signature.ParamCount == php_template.Signature.ParamCount)
				{
                    // signatures match, just install an explicit override if possible
					type_builder.DefineMethodOverride(target_argfull,
						DType.MakeConstructed(php_template.ArgFullInfo, template.Type as ConstructedType));
				}
				else
				{
					string stubs_key = null;
					MethodAttributes attrs;

                    if (sre_bug_workaround)
                    {
                        // check whether we have generated a stub having the template name before
                        if (stubs.ContainsKey(stubs_key = "," + php_template.ArgFullInfo.Name)) return;

                        attrs = php_template.ArgFullInfo.Attributes & ~MethodAttributes.Abstract;
                    }
                    else
                    {
                        attrs = MethodAttributes.PrivateScope | MethodAttributes.Virtual;
                    }

                    if (newSlot) attrs |= MethodAttributes.NewSlot; 
					else attrs &= ~MethodAttributes.NewSlot;

					// determine stub return and parameters type
					Type return_type;
					Type[] param_types = php_template.Signature.ToArgfullSignature(1, out return_type);
                    param_types[0] = Types.ScriptContext[0];

					MethodBuilder override_stub = type_builder.DefineMethod(
                        (sre_bug_workaround ? php_template.ArgFullInfo.Name : "<Override>"),
						attrs, return_type, param_types);

					ILEmitter il = new ILEmitter(override_stub);

                    //
                    // return target( [arg1, ...[, default, ...]] );
                    //

					// pass-thru all arguments, including this (arg0)
                    int pass_args = Math.Min(param_types.Length, target.Signature.ParamCount + 1);
					for (int i = 0; i <= pass_args; ++i) il.Ldarg(i);  // this, param1, ....
                    for (int i = pass_args; i <= target.Signature.ParamCount; ++i)
                    {
                        // ... // PhpException.MissingArgument(i, target.FullName); // but in some override it can be optional argument 
                        il.Emit(OpCodes.Ldsfld, PHP.Core.Emit.Fields.Arg_Default);  // paramN
                    }
                    il.Emit(OpCodes.Callvirt, target_argfull);
					il.Emit(OpCodes.Ret);

					if (sre_bug_workaround)
					{
						stubs.Add(stubs_key, override_stub);
					}
					else
					{
                        if (!php_template.ArgFullInfo.IsVirtual)
                            throw new InvalidOperationException(string.Format("Cannot override non-virtual method '{0}'!", php_template.ArgFullInfo.Name));

						type_builder.DefineMethodOverride(override_stub,
							DType.MakeConstructed(php_template.ArgFullInfo, template.Type as ConstructedType));
					}
				}
			}
		}

		/// <summary>
		/// Emits stubs for all overloads of one exported method.
		/// </summary>
		/// <param name="stubs">Already generated stubs.</param>
		/// <param name="target">The exported method.</param>
		private void EmitExportStubs(IDictionary<string, MethodBuilder>/*!*/ stubs, PhpMethod/*!*/ target)
		{
			Debug.Assert(target.IsExported);

			string clr_sig = null;
			MethodAttributes attributes = Reflection.Enums.ToMethodAttributes(target.MemberDesc.MemberAttributes);
			attributes |= MethodAttributes.HideBySig;

			foreach (StubInfo stub in ClrStubBuilder.DefineMethodExportStubs(
				target,
				attributes,
				false,
				delegate(string[] genericParamNames, object[] parameterTypes, object returnType)
				{
					// check whether we have not generated this signature before
					clr_sig = ClrMethod.Overload.ClrSignatureToString(genericParamNames.Length, parameterTypes, returnType);
					return !stubs.ContainsKey(clr_sig);
				}))
			{
				// set parameter names and attributes
				ClrStubBuilder.DefineStubParameters(stub.MethodBuilder, null, stub.Parameters);

				if (!stub.MethodBuilder.IsAbstract)
				{
					EmissionContext emission_context = SetupStubPlaces(target.DeclaringPhpType, stub.MethodBuilder.IsStatic);
					try
					{
						// stub body
						ClrStubBuilder.EmitMethodStubBody(
							new ILEmitter(stub.MethodBuilder),
							ScriptContextPlace,
							stub.Parameters,
							stub.TypeParameters,
							stub.ReturnType,
							target,
							target.DeclaringType);
					}
					finally
					{
						RestorePlaces(emission_context);
					}
				}

				stubs.Add(clr_sig, stub.MethodBuilder);
			}
		}

		#endregion
        
		#region EmitPhpException, EmitThrow (Tomas Matousek)

		/// <summary>
		/// Emits call to error reporting method.
		/// </summary>
		/// <param name="method">The error reporting method.</param>
		internal void EmitPhpException(MethodInfo/*!*/ method)
		{
			EmitPhpException(il, method);
		}

		internal void EmitPhpException(ILEmitter/*!*/ il, MethodInfo/*!*/ method)
		{
			// emits call to a method which reports an error:
			il.Emit(OpCodes.Call, method);

			// emits nop which makes sequence points working well in stack trace:
			if (context.Config.Compiler.Debug)
				il.Emit(OpCodes.Nop);
		}

		#endregion

		#region Script and Debugging Information Emission (Tomas Matousek)

		private int LastTransientLine = -1;
		private int LastTransientColumn = -1;

		/// <summary>
		/// Marks a sequence point in generated code if symbol writer is defined.
		/// In transient scripts, a code setting special fields of <see cref="ScriptContext"/> is emmitted.
		/// </summary>
		/// <param name="startLine">Real first line of the point.</param>
		/// <param name="startColumn">Real first column of the point.</param>
		/// <param name="endLine">Real last line of the point.</param>
		/// <param name="endColumn">Real last column of the point.</param>
		internal void MarkSequencePoint(int startLine, int startColumn, int endLine, int endColumn)
		{
			if (context.Config.Compiler.Debug)
			{
				// ignores #pragma inside the code span:
				ISymbolDocumentWriter symbol_writer = sourceUnit.GetMappedSymbolDocumentWriter(startLine);
				startLine = sourceUnit.GetMappedLine(startLine);

				if (symbol_writer != null && startLine >= 0)
				{
					Debug.Assert(startLine >= 0 && startColumn >= 0 && endLine >= 0 && endColumn >= 0, "Invalid position values.");
					il.MarkSequencePoint(symbol_writer, startLine, startColumn, endLine, endColumn);
				}

				if (CompilationUnit.IsTransient)
				{
					EmitEvalInfoCapture(startLine, startColumn, true);
					LastTransientLine = startLine;
					LastTransientColumn = startColumn;
				}
			}
		}

        /// <summary>
        /// Marks a sequence point (see <see cref="MarkSequencePoint"/>) using position of given <paramref name="expression"/>.
        /// </summary>
        /// <param name="expression">Expression which position is used to mark sequence point.</param>
        internal void MarkSequencePoint(Expression/*!*/expression)
        {
            Debug.Assert(expression != null);
            MarkSequencePoint(
                expression.Position.FirstLine,
                expression.Position.FirstColumn,
                expression.Position.LastLine,
                expression.Position.LastColumn + 1);
        }

		internal void MarkTransientSequencePoint()
		{
			if (context.Config.Compiler.Debug && CompilationUnit.IsTransient)
			{
				Debug.Assert(LastTransientLine != -1 && LastTransientColumn != -1);
				EmitEvalInfoCapture(LastTransientLine, LastTransientColumn, true);
			}
		}

		internal void EmitEvalInfoCapture(int line, int column, bool positionOnly)
		{
			EmitLoadScriptContext();
			il.LdcI4(line);
			il.Emit(OpCodes.Stfld, Fields.ScriptContext_EvalLine);

			EmitLoadScriptContext();
			il.LdcI4(column);
			il.Emit(OpCodes.Stfld, Fields.ScriptContext_EvalColumn);

			if (!positionOnly)
			{
				EmitLoadScriptContext();
				il.LdcI4(sourceUnit.CompilationUnit.TransientId);
				il.Emit(OpCodes.Stfld, Fields.ScriptContext_EvalId);

				EmitLoadScriptContext();
				il.LoadLiteral(sourceUnit.SourceFile.RelativePath.ToString());
				il.Emit(OpCodes.Stfld, Fields.ScriptContext_EvalRelativeSourcePath);
			}
		}

		internal void EmitEvalInfoPass(int line, int column)
		{
			il.LoadLiteral(sourceUnit.SourceFile.RelativePath.ToString());
			il.LdcI4(line);
			il.LdcI4(column);
			il.LdcI4(sourceUnit.CompilationUnit.TransientId);
		}


		#endregion

		#region Other stuff

		/// <summary>
		/// Emits code which populates an array with values of specified expressions.
		/// </summary>
		/// <param name="expressions">A list of expressions.</param>
		/// <param name="result">
		/// A local variable where to store the resulting array. 
		/// If <B>null</B> then a new local variable is defined.
		/// </param>
		/// <returns>The local variable where the resulting array is stored.</returns>
		public LocalBuilder EmitObjectArrayPopulation(List<Expression>/*!*/ expressions, LocalBuilder result)
		{
            // constructs the array and pushes it onto the top of the evaluation stack
            EmitObjectArrayPopulation(expressions);

            // stores the array into the <result> variable
			if (result == null)
                result = il.DeclareLocal(typeof(object[]));

            // <result> = array
            il.Stloc(result);

            //
            return result;
		}

        /// <summary>
        /// Emits code which populates an array with values of specified expressions.
        /// PUshes the array onto the top of the evaluation stack.
        /// </summary>
        /// <param name="expressions">A list of expressions.</param>
        /// <remarks>PUshes the resulting array onto the top of the evaluation stack.</remarks>
        public void EmitObjectArrayPopulation(List<Expression>/*!*/ expressions)
        {
            Debug.Assert(expressions != null);

            il.LdcI4(expressions.Count);
            il.Emit(OpCodes.Newarr, typeof(object));

            for (int i = 0; i < expressions.Count; i++)
            {
                // array[<i>] = <expressions[i]>;
                il.Emit(OpCodes.Dup);
                il.LdcI4(i);
                EmitBoxing(expressions[i].Emit(this));
                il.Emit(OpCodes.Stelem_Ref);
            }
        }

		/// <summary>
		/// Returns a string that uniquely identifies current function, method or class, or returns <B>null</B>
		/// if global code is currently emitted.
		/// </summary>
		/// <returns>String ID or <B>null</B>.</returns>
		public string GetLocationId()
		{
			switch (locationStack.LocationType)
			{
				case LocationTypes.FunctionDecl:
					return "F:" + locationStack.PeekFunctionDecl().Name;

				case LocationTypes.TypeDecl:
					return "C:" + locationStack.PeekTypeDecl().Type.QualifiedName;

				case LocationTypes.MethodDecl:
					{
						CompilerLocationStack.MethodDeclContext method = locationStack.PeekMethodDecl();
						return String.Format("M:{0}${1}", method.Type.QualifiedName, method.Method.FullName);
					}
			}
			return null;
		}

		/// <summary>
		/// Emits call to a method.
		/// </summary>
		/// <param name="method">A <see cref="MethodInfo"/> of the method to be called.</param>
		/// <returns>A type code of return value.</returns>
		/// <remarks>
		/// Use if it is hard to keep track of types returned by emitted methods (e.g. in operators).
		/// Do not waste cycles when it is clear what type code the emitted method returns.
		/// </remarks>
		internal PhpTypeCode EmitMethodCall(MethodInfo method)
		{
			il.Emit(OpCodes.Call, method);
			return PhpTypeCodeEnum.FromType(method.ReturnType);
		}

        /// <summary>
		/// Emits call to <see cref="ScriptContext.DeclareFunction"/>.
		/// </summary>
        internal void EmitDeclareFunction(PhpFunction/*!*/ function)
        {
            EmitDeclareFunction(il, ScriptContextPlace, function);
        }

		/// <summary>
		/// Emits call to <see cref="ScriptContext.DeclareFunction"/>.
		/// </summary>
		internal static void EmitDeclareFunction(ILEmitter/*!*/il, IPlace/*!*/scriptContextPlace, PhpFunction/*!*/ function)
        {
            Label lbl_fieldinitialized = il.DefineLabel();

            // private static PhpRoutine <routine>'function = null;
            var attrs = FieldAttributes.Static | FieldAttributes.Private;
            var field = il.TypeBuilder.DefineField(string.Format("<routine>'{0}", function.FullName), typeof(PhpRoutineDesc), attrs);

            // if (<field> == null)
            il.Emit(OpCodes.Ldsfld, field);
            il.Emit(OpCodes.Brtrue, lbl_fieldinitialized);
            {
                // <field> = new PhpRoutineDesc(<attributes>, new RoutineDelegate(null, <delegate>))

                // LOAD <attributes>;
                il.LdcI4((int)function.MemberDesc.MemberAttributes);

                // new RoutineDelegate(null, <delegate>, true)
                il.Emit(OpCodes.Ldnull);
                il.Emit(OpCodes.Ldftn, function.ArgLessInfo);
                il.Emit(OpCodes.Newobj, Constructors.RoutineDelegate);
                il.LoadBool(true);

                // new PhpRoutineDesc:
                il.Emit(OpCodes.Newobj, Constructors.PhpRoutineDesc_Attr_Delegate_Bool);

                // <field> = <STACK>
                il.Emit(OpCodes.Stsfld, field);

                // new PurePhpFunction(<field>, fullName, argfull);   // writes desc.Member
                il.Emit(OpCodes.Ldsfld, field);
                il.Emit(OpCodes.Ldstr, function.FullName);
                CodeGenerator.EmitLoadMethodInfo(il, function.ArgFullInfo/*, AssemblyBuilder.DelegateBuilder*/);
                il.Emit(OpCodes.Newobj, Constructors.PurePhpFunction);
                il.Emit(OpCodes.Pop);
                
            }
            il.MarkLabel(lbl_fieldinitialized);

            // CALL ScriptContent.DeclareFunction(<field>, <name>);
            scriptContextPlace.EmitLoad(il);
            
            // LOAD <field>
            il.Emit(OpCodes.Ldsfld, field);            

            // LOAD <fullName>
            il.Emit(OpCodes.Ldstr, function.FullName);

            //
            il.Emit(OpCodes.Call, Methods.ScriptContext.DeclareFunction);
        }

        /// <summary>
		/// Emits call to <see cref="ScriptContext.DeclareLambda"/>.
		/// </summary>
		/// <param name="info">A method info.</param>
		internal void EmitDeclareLamdaFunction(MethodInfo/*!*/ info)
		{
			Debug.Assert(info != null);

			// LOAD ScriptContext.DeclareLamda(<delegate>);
			EmitLoadScriptContext();

			il.Emit(OpCodes.Ldnull);
			il.Emit(OpCodes.Ldftn, info);
			il.Emit(OpCodes.Newobj, Constructors.RoutineDelegate);

			il.Emit(OpCodes.Call, Methods.ScriptContext.DeclareLambda);
		}

		internal void EmitReferenceDereference(ref PhpTypeCode typeCode, bool wantRef)
		{
			if (wantRef)
			{
				// make reference:
				if (typeCode != PhpTypeCode.PhpReference)
				{
					EmitBoxing(typeCode);
					il.Emit(OpCodes.Newobj, Constructors.PhpReference_Object);
				}

				typeCode = PhpTypeCode.PhpReference;
			}
			else
			{
				// dereference:
				if (typeCode == PhpTypeCode.PhpReference)
				{
					il.Emit(OpCodes.Ldfld, Fields.PhpReference_Value);
					typeCode = PhpTypeCode.Object;
				}
			}
		}

        /// <summary>
        /// Emits load of <see cref="MethodInfo"/> onto the top of evaluation stack.
        /// </summary>
        /// <param name="il"></param>
        /// <param name="mi"></param>
        internal static void EmitLoadMethodInfo(ILEmitter/*!*/il, MethodInfo/*!*/mi/*, DelegateBuilder dbuild*/)
        {
            if (mi == null)
                throw new ArgumentNullException("mi");

            if (!mi.IsStatic)
                throw new NotSupportedException();

            // following code uses hack, where we can create delegate in "compile time", and then takes its MethodInfo property.
            // new Func<...>( null, <mi> ).Method

            //// construct the type
            ////var miArgs = mi.GetParameters();    // THIS FAILS WHEN <mi> IS NOT BAKED YET
            ////Type[] delegateArgs = new Type[1 + miArgs.Length];
            ////delegateArgs[0] = mi.ReturnType;
            ////for (int i = 0; i < miArgs.Length; i++) delegateArgs[i + 1] = miArgs[i].ParameterType;
            //var delegateCtor = DelegateBuilder.GetDelegateCtor(dbuild.GetDelegateType(delegateArgs, il.GetNextUniqueIndex()));
            var delegateCtor = DelegateBuilder.GetDelegateCtor(Types.Action[0]); // NOT NICE

            //.ldnull
            //.ldftn <mi>
            //.newobj instance void Action::.ctor(object, native int)
            //.call get_Method

            il.Emit(OpCodes.Ldnull);
            il.Emit(OpCodes.Ldftn, mi);
            il.Emit(OpCodes.Newobj, delegateCtor);
            il.Emit(OpCodes.Call, Properties.Delegate_Method.GetGetMethod());
        }

        /// <summary>
        /// Emit call to <see cref="DynamicCode.Assert"/> or <see cref="DynamicCode.Eval"/>.
        /// </summary>
        internal PhpTypeCode EmitEval(EvalKinds kind, Expression/*!*/code, Position position, QualifiedName? currentNamespace, Dictionary<string, QualifiedName> currentAliases)
        {
            Debug.Assert(code != null);

            // LOAD DynamicCode.<Eval | Assert>(<code>, context, definedVariables, self, includer, source, line, column, evalId, naming)
            if (kind == EvalKinds.Assert)
            {
                // an argument of the assert is boxed:
                EmitBoxing(code.Emit(this));
            }
            else if (kind == EvalKinds.SyntheticEval)
            {
                Debug.Assert(code.HasValue);
                Debug.Assert(code.Value is string);

                // an argument of the eval is converted to a string:
                il.Emit(OpCodes.Ldstr, (string)code.Value);
                il.Emit(OpCodes.Ldc_I4_1);
            }
            else
            {
                // an argument of the eval is converted to a string:
                EmitConversion(code, PhpTypeCode.String);
                il.Emit(OpCodes.Ldc_I4_0);
            }

            EmitLoadScriptContext();
            EmitLoadRTVariablesTable();
            EmitLoadSelf();
            EmitLoadClassContext();
            EmitEvalInfoPass(position.FirstLine, position.FirstColumn);
            EmitNamingContext(currentNamespace, currentAliases, position);

            il.Emit(OpCodes.Call, (kind == EvalKinds.Assert) ? Methods.DynamicCode.Assert : Methods.DynamicCode.Eval);

            return (kind == EvalKinds.Assert) ? PhpTypeCode.Boolean : PhpTypeCode.Object;
        }

        /// <summary>
        /// Loads (cached) instance of given state of <see cref="NamingContext"/> onto the evaluation stack.
        /// </summary>
        internal void EmitNamingContext(QualifiedName? currentNamespace, Dictionary<string, QualifiedName> currentAliases, Position position)
        {
            ILEmitter il = this.IL;

            if (NamingContext.NeedsNamingContext(currentNamespace, currentAliases))
            {
                // private static NamingContext <id> = null;
                string fname = (this.SourceUnit != null) ? this.SourceUnit.SourceFile.ToString() : string.Empty;
                string id = String.Format("<namingContext>{0}${1}${2}", unchecked((uint)fname.GetHashCode()), position.FirstLine, position.FirstColumn);

                // create static field for static local index: static int <id>;
                Debug.Assert(il.TypeBuilder != null, "The method does not have declaring type! (global code in pure mode?)");
                var fld = il.TypeBuilder.DefineField(id, typeof(NamingContext), System.Reflection.FieldAttributes.Private | System.Reflection.FieldAttributes.Static);

                // <id> ?? (<id> = NamingContext.<EmitNewNamingContext>)
                Label end = il.DefineLabel();

                il.Emit(OpCodes.Ldsfld, fld);
                il.Emit(OpCodes.Dup);
                il.Emit(OpCodes.Brtrue, end);
                if (true)
                {
                    il.Emit(OpCodes.Pop);
                    NamingContext.EmitNewNamingContext(il, currentNamespace, currentAliases);
                    il.Emit(OpCodes.Dup);
                    il.Emit(OpCodes.Stsfld, fld);
                }

                il.MarkLabel(end);
            }
            else
            {
                il.Emit(OpCodes.Ldnull);
            }
        }

		#endregion

		#region Array Keys and Operators

		internal PhpTypeCode EmitArrayKey(ChainBuilder chain, Expression key)
		{
			PhpTypeCode result;

			if (key != null)
			{
				if (chain != null) chain.Create();
				
                // convert the key into integer if necessary and possible in compile time
                IntStringKey array_key;
                if (key.HasValue && Convert.ObjectToArrayKey(key.Value, out array_key) && array_key.IsInteger)
                {
                    il.LdcI4(array_key.Integer);
                    result = PhpTypeCode.Integer;
                }
                else
                {
                    // Emit index and box the result
                    switch (result = key.Emit(this))
                    {
                        case PhpTypeCode.Integer:
                            break;

                        case PhpTypeCode.String:
                            break;

                        default:
                            EmitBoxing(result);
                            result = PhpTypeCode.Object;
                            break;
                    }
                }
				
				if (chain != null) chain.End();
			}
			else
				result = PhpTypeCode.Invalid;

			return result;
		}
		
		private bool EmitExactStringKeyHash(PhpTypeCode keyTypeCode, Expression keyExpr)
		{
			if (keyExpr != null && keyTypeCode == PhpTypeCode.String && keyExpr.HasValue)
			{
				string skey = (string)keyExpr.Value;
				IntStringKey array_key = Convert.StringToArrayKey(skey);
				if (array_key.IsString && skey == array_key.String) // skey was not converted to int
				{
                    il.LdcI4(array_key.GetHashCode());  // == array_key.Integer == IntStringKey.StringKeyToArrayIndex(skey) // previously: skey.GetHashCode()
					return true;
				}
			}
			return false;
		}

		internal void EmitGetArrayItem(PhpTypeCode keyTypeCode, Expression keyExpr, bool reference)
		{
			MethodInfo method;
			switch (keyTypeCode)
			{
				case PhpTypeCode.Integer:
					method = (reference) ? Methods.PhpArray.GetArrayItemRef_Int32 : Methods.PhpArray.GetArrayItem_Int32;
					break;

				case PhpTypeCode.String:
					if (reference)
					{
						method = Methods.PhpArray.GetArrayItemRef_String;
					}
					else
					{
						if (EmitExactStringKeyHash(keyTypeCode, keyExpr))
							method = Methods.PhpArray.GetArrayItemExact_String;
						else
							method = Methods.PhpArray.GetArrayItem_String;
					}
					break;

				case PhpTypeCode.Object:
					method = (reference) ? Methods.PhpArray.GetArrayItemRef_Object : Methods.PhpArray.GetArrayItem_Object;
					break;

				case PhpTypeCode.Invalid:
					Debug.Assert(reference);
					method = Methods.PhpArray.GetArrayItemRef;
					break;

				default:
					Debug.Fail();
					throw null;
			}
			il.Emit(method.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, method);
		}

		internal void EmitSetArrayItem(PhpTypeCode keyTypeCode, Expression keyExpr, bool reference, bool ctor)
		{
			MethodInfo method; 
			switch (keyTypeCode)
			{
				case PhpTypeCode.Integer:
					method = (reference) ? Methods.PhpArray.SetArrayItemRef_Int32 : Methods.PhpArray.SetArrayItem_Int32;
					break;

				case PhpTypeCode.String:
					if (reference)
					{
						method = Methods.PhpArray.SetArrayItemRef_String;
					}
					else
					{
						if (EmitExactStringKeyHash(keyTypeCode, keyExpr))
							method = Methods.PhpArray.SetArrayItemExact_String;
						else
							method = Methods.PhpArray.SetArrayItem_String;
					}
					break;

				case PhpTypeCode.Object:
                    method = reference ? Methods.PhpArray.SetArrayItemRef_Object : Methods.PhpArray.SetArrayItem_Object;
					break;
					
				case PhpTypeCode.Invalid:
					method = ctor ? Methods.PhpArray.AddToEnd_Object : Methods.PhpArray.SetArrayItem;
					break;
					
				default:
					Debug.Fail();
					throw null;
			}
            il.Emit(method.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, method);
		}

		internal void EmitGetItem(PhpTypeCode keyTypeCode, Expression keyExpr, bool reference)
		{
			MethodInfo method;
			switch (keyTypeCode)
			{
				case PhpTypeCode.Integer:
					method = (reference) ? Methods.Operators.GetItemRef.Int32 : Methods.Operators.GetItem.Int32;
					break;

				case PhpTypeCode.String:

					if (reference)
					{
						method = Methods.Operators.GetItemRef.String;
					}
					else
					{
						if (EmitExactStringKeyHash(keyTypeCode, keyExpr))
							method = Methods.Operators.GetItemExact;
						else
							method = Methods.Operators.GetItem.String;
					}
					break;

				case PhpTypeCode.Object:
					method = (reference) ? Methods.Operators.GetItemRef.Object : Methods.Operators.GetItem.Object;
					break;

				case PhpTypeCode.Invalid:
					Debug.Assert(reference);
					method = Methods.Operators.GetItemRef.Keyless;
					break;

				default:
					Debug.Fail();
					throw null;
			}
			
			il.Emit(OpCodes.Call, method);	
		}

		internal void EmitSetItem(PhpTypeCode keyTypeCode, Expression keyExpr, bool reference)
		{
			MethodInfo method;
			switch (keyTypeCode)
			{
				case PhpTypeCode.Integer:
					method = (reference) ? Methods.Operators.SetItemRef.Int32 : Methods.Operators.SetItem.Int32;
					break;

				case PhpTypeCode.String:
					if (reference)
					{
						method = Methods.Operators.SetItemRef.String;
					}
					else
					{					
						if (EmitExactStringKeyHash(keyTypeCode, keyExpr))
							method = Methods.Operators.SetItemExact;
						else
							method = Methods.Operators.SetItem.String;
					}
					break;

				case PhpTypeCode.Object:
					method = (reference) ? Methods.Operators.SetItemRef.Object : Methods.Operators.SetItem.Object;
					break;

				case PhpTypeCode.Invalid:
					method = Methods.Operators.SetItem.Keyless;
					break;

				default:
					Debug.Fail();
					throw null;
			}
			il.Emit(OpCodes.Call, method);
		}

		#endregion

        #region Operators

        /// <summary>
        /// Emits most efficient form of equality comparison operator.
        /// </summary>
        /// <param name="leftExprEmitter"></param>
        /// <param name="rightExprEmitter"></param>
        internal PhpTypeCode EmitCompareEq(Func<CodeGenerator, PhpTypeCode>/*!*/leftExprEmitter, Func<CodeGenerator, PhpTypeCode>/*!*/rightExprEmitter)
        {
            Debug.Assert(leftExprEmitter != null && rightExprEmitter != null);

            this.EmitBoxing(leftExprEmitter(this));      // x = leftExpr
            var right_type = rightExprEmitter(this);     // y = rightExpr

            switch (right_type)
            {
                case PhpTypeCode.Integer:
                    this.IL.Emit(OpCodes.Call, Methods.CompareEq_object_int);
                    break;
                case PhpTypeCode.String:
                    this.IL.Emit(OpCodes.Call, Methods.CompareEq_object_string);
                    break;
                default:
                    this.EmitBoxing(right_type);
                    this.IL.Emit(OpCodes.Call, Methods.CompareEq_object_object);
                    break;
            }

            return PhpTypeCode.Boolean;
        }

        #endregion

    }
}
