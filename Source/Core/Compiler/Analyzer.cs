/*

 Copyright (c) 2006 Tomas Matousek.
 Copyright (c) 2003-2005 Vaclav Novak.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

using PHP.Core.AST;
using PHP.Core.Emit;
using PHP.Core.Parsers;
using PHP.Core.Reflection;
using System.Reflection.Emit;

#if SILVERLIGHT
using PHP.CoreCLR;
#endif

namespace PHP.Core
{
	internal interface IPostAnalyzable
	{
		void PostAnalyze(Analyzer/*!*/ analyzer);
	}

	#region Evaluation

	internal struct Evaluation
	{
		public object Value;
		public bool HasValue;
		public Expression Expression;

		public Evaluation(Expression/*!*/ expression)
			: this(expression, null, false)
		{
		}

		public Evaluation(Expression/*!*/ expression, object value)
			: this(expression, value, true)
		{
		}

		public Evaluation(Expression/*!*/ expression, object value, bool hasValue)
		{
			this.Expression = expression;
			this.Value = value;
			this.HasValue = hasValue;
		}

		/// <summary>
		/// Converts the expression to a literal if we have a value and the expression is not yet a literal.
		/// Returns the converted expression.
		/// Used when the evaluation cannot continue as some other part of the operation is not evaluable.
		/// </summary>
		public Expression/*!*/ Literalize()
		{
			if (HasValue && !Expression.HasValue)
				return Expression = Literal.Create(Expression.Position, Value, Expression.Access);
			else
				return Expression;
		}

		internal Evaluation Evaluate(Expression/*!*/ parent, out Expression/*!*/ expr)
		{
			if (HasValue)
			{
				expr = Expression;
				return new Evaluation(parent, parent.Evaluate(Value));
			}

			expr = Literalize();
			return new Evaluation(parent);
		}

		internal static Evaluation Evaluate(Expression/*!*/ parent, Evaluation eval1, out Expression/*!*/ expr1,
			Evaluation eval2, out Expression/*!*/ expr2)
		{
			if (eval1.HasValue && eval2.HasValue)
			{
				expr1 = eval1.Expression;
				expr2 = eval2.Expression;
				return new Evaluation(parent, parent.Evaluate(eval1.Value, eval2.Value));
			}

			expr1 = eval1.Literalize();
			expr2 = eval2.Literalize();
			return new Evaluation(parent);
		}

		internal Evaluation ReadOnlyEvaluate(Expression/*!*/ parent)
		{
			if (HasValue)
				return new Evaluation(parent, parent.Evaluate(Value));

			return new Evaluation(parent);
		}

		internal static Evaluation ReadOnlyEvaluate(Expression/*!*/ parent, Evaluation eval1, Evaluation eval2)
		{
			if (eval1.HasValue && eval2.HasValue)
				return new Evaluation(parent, parent.Evaluate(eval1.Value, eval2.Value));

			return new Evaluation(parent);
		}
	}

	#endregion

	/// <summary>
	/// Analyzes the AST previously built by the Parser. 
	/// Evaluates node attributes that can't be evaluated from down to up during building AST.
	/// Fills tables.
	/// Does some basic optimizations like constant expressions evaluating 
	/// and unreachable code eliminating.
	/// Calls compiling of included source files.
	/// </summary>
	public sealed class Analyzer : AstVisitor
	{
		internal enum States
		{
			Initial,
			PreAnalysisStarted,
			MemberAnalysisStarted,
			FullAnalysisStarted,
			PostAnalysisStarted,
		}

		internal States State { get { return state; } }
		private States state;


		#region Fields and Properties

		/// <summary>
		/// Analyzed AST.
        /// Must be internally modifiable in order to allow partial class declaration to change the sourceUnit during the analysis
		/// </summary>
        public SourceUnit SourceUnit { get { return sourceUnit; } internal set { sourceUnit = value; } }
		private SourceUnit sourceUnit; 

		/// <summary>
		/// Current scope. Available only during full analysis.
		/// </summary>
		public Scope CurrentScope { get { return currentScope; } set { currentScope = value; } }
		private Scope currentScope = Scope.Invalid;

		/// <summary>
		/// The current compilation context.
		/// </summary>
		public override CompilationContext Context { get { return context; } }
		private CompilationContext context;

		/// <summary>
		/// The current error sink.
		/// </summary>
		internal ErrorSink ErrorSink { get { return context.Errors; } }

		internal List<IPostAnalyzable>/*!*/ PostAnalyzed { get { return postAnalyzed; } }
		private List<IPostAnalyzable>/*!*/ postAnalyzed = new List<IPostAnalyzable>();

		#endregion

		#region Construction

		internal Analyzer(CompilationContext/*!*/ context)
		{
			this.context = context;

			condLevel = 0;
			loopNestingLevel = 0;
			state = States.Initial;
		}

		#endregion

		#region Analysis Entry Points

		internal void PreAnalyze(IEnumerable<Declaration>/*!*/ declarations)
		{
			state = States.PreAnalysisStarted;

			foreach (Declaration decl in declarations)
			{
				this.sourceUnit = decl.SourceUnit;

				if (decl.Node != null)
					decl.Node.PreAnalyze(this);
			}
		}

		internal void AnalyzeMembers(IEnumerable<Declaration>/*!*/ declarations)
		{
			state = States.MemberAnalysisStarted;

			foreach (Declaration decl in declarations)
			{
				this.sourceUnit = decl.SourceUnit;

				if (decl.Node != null)
					decl.Node.AnalyzeMembers(this);
			}
		}

		/// <summary>
		/// Analyzes the AST of the source unit.
		/// </summary>
		internal void Analyze(SourceUnit/*!*/ sourceUnit)
		{
			state = States.FullAnalysisStarted;

			this.sourceUnit = sourceUnit;
			this.currentNamespace = sourceUnit.CurrentNamespace;
			this.currentScope = Scope.Global;

			sourceUnit.Ast.Analyze(this);

			this.currentScope = Scope.Invalid;
		}

		internal void PostAnalyze()
		{
			state = States.PostAnalysisStarted;

			foreach (IPostAnalyzable node in postAnalyzed)
				node.PostAnalyze(this);

			postAnalyzed = null;
		}

		private Parser AstBuilder
		{
			get
			{
				if (_astBuilder == null)
				{
					_astBuilder = new Parser();
					_astBuilder.AllowGlobalCode = true;
				}
				return _astBuilder;
			}
		}
		private Parser _astBuilder;

		/// <summary>
		/// Builds AST from the given source code string.
		/// Returns <B>null</B> if the AST cannot be built (new declarations appears in the code).
		/// </summary>
		internal List<Statement> BuildAst(Position initialPosition, string/*!*/ sourceCode)
		{
			Parser.ReductionsCounter counter = new Parser.ReductionsCounter();

            var ast = BuildAst(initialPosition, sourceCode, counter);

			if (counter.FunctionCount + counter.TypeCount + counter.ConstantCount > 0)
				return null;

			return ast.Statements;
		}

        /// <summary>
        /// Builds AST from the given source code string. Does not check for declarations in the source code.
        /// </summary>
        public AST.GlobalCode BuildAst(Position initialPosition, string/*!*/ sourceCode, Parser.ReductionsCounter counter)
        {
            StringReader source_reader = new StringReader(sourceCode);

            AST.GlobalCode ast = AstBuilder.Parse(sourceUnit, source_reader, ErrorSink, counter,
                initialPosition, Lexer.LexicalStates.ST_IN_SCRIPTING, context.Config.Compiler.LanguageFeatures);

            return ast;
        }

		#endregion

		#region Nested Types: Locations

		internal enum Locations
		{
			GlobalCode,
			FunctionDecl,
			MethodDecl,
			TypeDecl,
			ActualParam,
			Switch
		}

		private abstract class Location
		{
			public abstract Locations Kind { get; }
		}

		#region DeclLocation

		private abstract class DeclLocation : Location
		{
			internal int NestingLevel { get { return nestingLevel; } }
			private int nestingLevel;

			internal DeclLocation(int nestingLevel)
			{
				this.nestingLevel = nestingLevel;
			}
		}

		#endregion

		#region RoutineDeclLoc

		private class RoutineDeclLoc : DeclLocation
		{
			internal PhpRoutine/*!*/ Routine { get { return routine; } }
			private PhpRoutine/*!*/ routine;

			public override Locations Kind
			{
				get { return routine.IsFunction ? Locations.FunctionDecl : Locations.MethodDecl; }
			}

			internal RoutineDeclLoc(PhpRoutine/*!*/ routine, int nestingLevel)
				: base(nestingLevel)
			{
				this.routine = routine;
			}
		}

		#endregion

		#region TypeDeclLocation

		private class TypeDeclLocation : DeclLocation
		{
			public override Locations Kind
			{
				get { return Locations.TypeDecl; }
			}

			public PhpType/*!*/ Type { get { return type; } }
			private PhpType/*!*/ type;

			internal TypeDeclLocation(PhpType/*!*/ type, int nestingLevel)
				: base(nestingLevel)
			{
				this.type = type;
			}
		}

		#endregion

		#region ActParamsLoc

		/// <summary>
		/// Represents location in some actual parameter in function call.
		/// </summary>
		/// <remarks>
		/// It maintains information about formal parameters
		/// declaration and currently analyzed actual parameter index to answer if the
		/// actual parameter shall be passed by reference or not.
		/// </remarks>
		private class ActualParamsLocation : Location
		{
			public override Locations Kind
			{
				get { return Locations.ActualParam; }
			}

			private int currentParam;

			/// <summary>
			/// Says which parameter shall be passed by reference.
			/// </summary>
			private RoutineSignature/*!*/ signature;

			/// <summary>
			/// Actual parameters count (needn't equal to the signature's size).
			/// </summary>
			private int actualParamCount;

			internal ActualParamsLocation(RoutineSignature/*!*/ signature, int actualParamCount)
			{
				this.currentParam = -1;
				this.signature = signature;
				this.actualParamCount = actualParamCount;
			}

			/// <summary>
			/// Updates information about location.
			/// </summary>
			internal void MoveToNextParam()
			{
				currentParam++;
				Debug.Assert(currentParam < actualParamCount);
			}

			/// <summary>
			/// Says if the just now analyzed actual parameter shall be passed be reference
			/// </summary>
			/// <remarks>
			/// Called only from <see cref="ActualParam.Analyze"/>.
			/// </remarks>
			/// <returns>
			/// <B>true</B> if the just now analyzed actual parameter shall be passed be reference
			/// </returns>
			internal bool ActParamPassedByRef()
			{
				Debug.Assert(!signature.IsUnknown);
				return signature.IsAlias(currentParam);
			}

			internal bool ActParamDeclIsUnknown()
			{
				return signature.IsUnknown;
			}
		}

		#endregion

		#region SwitchLoc

		/// <summary>
		/// Represents location in switch statement
		/// </summary>
		/// <remarks>
		/// <B>SwitchLoc</B> is used to store information about compile-time known case values
		/// and used default section. That is used to report some warnings.
		/// </remarks>
		private class SwitchLocation : Location
		{
			public override Locations Kind
			{
				get { return Locations.Switch; }
			}

			internal ArrayList ConstCases;
			internal bool ContainsDefault;

			internal SwitchLocation()
			{
				ConstCases = new ArrayList();
				ContainsDefault = false;
			}
		}

		#endregion

		#endregion

		#region Locations

		/// <summary>
		/// Stack of locations.
		/// </summary>
		/// <remarks>
		/// Used to maintain information on the actual position 
		/// of the analyzer in the AST. It says "what I am analyzing now".
		/// </remarks>
		private readonly Stack<Location>/*!*/ locationStack = new Stack<Location>();

		/// <summary>
		/// Stack of <see cref="TypeDeclLocation"/> instances.
		/// </summary>
		/// <remarks>
		/// Represents (direct or indirect) nesting of classes declarations.
		/// </remarks>
		private readonly Stack<TypeDeclLocation>/*!*/ typeDeclStack = new Stack<TypeDeclLocation>();

		/// <summary>
		/// Routine stack.
		/// </summary>
		/// <remarks>
		/// Represents (direct or indirect) nesting of functions/method declarations.
		/// </remarks>
		private readonly Stack<RoutineDeclLoc>/*!*/ routineDeclStack = new Stack<RoutineDeclLoc>();

		public QualifiedName? CurrentNamespace { get { return currentNamespace; } }
		private QualifiedName? currentNamespace;

		/// <summary>
		/// Level of code conditionality (zero means an unconditional code).
		/// </summary>
		private int condLevel;

		/// <summary>
		/// Gets level of right now analyzed piece of code nesting in loops
		/// </summary>
		internal int LoopNestingLevel { get { return loopNestingLevel; } }
		private int loopNestingLevel;

		/// <summary>
		/// Is currently analyzed code unreachable? (TODO: too simple analysis, needs to be improved due to introduction of goto's)
		/// </summary>
		private bool unreachableCode;

		/// <summary>
		/// This field serves to ensure, that unreachable code warning is not reported on every 
		/// statement in unreachable block od statements, but only once
		/// </summary>
		private bool unreachableCodeReported;

		private Location CurrentLocation
		{
			get
			{
				return locationStack.Peek();
			}
		}

		internal PhpType CurrentType
		{
			get
			{
				return (typeDeclStack.Count > 0) ? typeDeclStack.Peek().Type : null;
			}
		}

		internal PhpRoutine CurrentRoutine
		{
			get
			{
				return (routineDeclStack.Count > 0) ? routineDeclStack.Peek().Routine : null;
			}
		}

		/// <summary>
		/// Checks whether any of the classes that contains the code is in complete -
		/// this is used for resolving whether function can be declared (in incomplete class
		/// it must be declared later at runtime)
		/// </summary>
		/// <returns></returns>
		public bool IsInsideIncompleteClass()
		{
			foreach (TypeDeclLocation loc in typeDeclStack)
			{
				if (!loc.Type.IsComplete) return true;
			}
			return false;
		}

		internal void AddCurrentRoutineProperty(RoutineProperties property)
		{
			if (CurrentRoutine != null)
				CurrentRoutine.Properties |= property;
		}

		/// <summary>
		/// Whether the argument passing semantics is by-ref.
		/// </summary>
		internal bool ActParamPassedByRef()
		{
			return ((ActualParamsLocation)CurrentLocation).ActParamPassedByRef();
		}

		/// <summary>
		/// Whether the argument passing semantics is known for the current actual argument.
		/// </summary>
		internal bool ActParamDeclIsUnknown()
		{
			return ((ActualParamsLocation)CurrentLocation).ActParamDeclIsUnknown();
		}

		#endregion

		#region Conditional code

		/// <summary>
		/// Notices the analyzer, that conditional code is entered.
		/// </summary>
		internal void EnterConditionalCode()
		{
			condLevel++;
		}

		/// <summary>
		/// Notices the analyzer, that conditional code is leaved.
		/// </summary>
		internal void LeaveConditionalCode()
		{
			condLevel--;

			// because the unreachable code is not analyzed, this will unset unreachableCode
			// at the end of conditional block of code
			LeaveUnreachableCode();

			Debug.Assert(condLevel >= 0);
		}

		/// <summary>
		/// Says if right now analyzed code is part of conditional block
		/// </summary>
		/// <returns>
		/// <B>true</B> if right now analyzed code is part of conditional block
		/// </returns>
		internal bool IsThisCodeConditional()
		{
			return condLevel > 0;
		}

		#endregion

		#region Unreachable code

		/// <summary>
		/// Says if the right now analyzed AST node represents part of conditional code
		/// </summary>
		/// <returns>
		/// <B>true</B> if the right now analyzed AST node represents part of conditional code
		/// </returns>
		internal bool IsThisCodeUnreachable()
		{
			return unreachableCode;
		}

		/// <summary>
		/// Notices the Analyzer that unreachable code has been entered
		/// </summary>
		/// <remarks>
		/// Unreachable code is code behind <see cref="JumpStmt"/> in the same conditional block 
		/// but only if it is not declaration in global code.
		/// Unreachable code is also code in while(false) body and if(false) then statement,
		/// if(true)... else statement etc.
		/// </remarks>
		internal void EnterUnreachableCode()
		{
			unreachableCode = true;
			unreachableCodeReported = false;
		}

		/// <summary>
		/// Notices the Analyzer that unreachable code has been leaved
		/// </summary>
		/// <remarks>
		/// This method is called only from <see cref="LeaveConditionalCode"/> because unreachable code ends 
		/// at the end of conditional block and from <see cref="GlobalCode.Analyze"/> 
		/// because unreachable declarations in global code are valid.
		/// </remarks>
		internal void LeaveUnreachableCode()
		{
			unreachableCode = false;
		}

		internal void ReportUnreachableCode(Position position)
		{
			if (!unreachableCodeReported)
			{
				ErrorSink.Add(Warnings.UnreachableCodeDetected, SourceUnit, position);
				unreachableCodeReported = true;
			}
		}

		#endregion

		#region Variables and Labels

		/// <summary>
		/// Gets the variables table for the right now analyzed scope.
		/// </summary>
		internal VariablesTable CurrentVarTable
		{
			get
			{
				if (CurrentRoutine != null)
					return CurrentRoutine.Builder.LocalVariables;
				else
					return sourceUnit.Ast.VarTable;
			}
		}

		/// <summary>
		/// Gets the variables table for the right now analyzed scope.
		/// </summary>
		internal Dictionary<VariableName, Statement> CurrentLabels
		{
			get
			{
				if (CurrentRoutine != null)
					return CurrentRoutine.Builder.Labels;
				else
					return sourceUnit.Ast.Labels;
			}
		}

		#endregion

		#region ConstructedTypes

		// TODO: Should ConstructedTypes be moved to CompilationUnitBase?
		// + DefineConstructedTypesBuilders will be called in DefineBuilders of the CU
		// + if persisted in any way, it should be persisted per CU
		// - duplicates among CUs

		/// <summary>
		/// Stores all constructed types found in the source codes.
		/// </summary>
		private readonly Dictionary<DTypeDescs, ConstructedType>/*!*/ constructedTypes = new Dictionary<DTypeDescs, ConstructedType>();

		internal ConstructedType/*!*/ CreateConstructedType(DTypeDesc/*!*/ genericType, DTypeDesc[]/*!!*/ arguments, int argCount)
		{
			ConstructedType result;

			if (genericType.IsUnknown)
			{
				Array.Resize(ref arguments, argCount);
				result = new ConstructedType(genericType, arguments);
			}
			else
			{
				DTypeDescs tuple = new DTypeDescs(genericType, arguments, argCount);

				if (!constructedTypes.TryGetValue(tuple, out result))
				{
					Array.Resize(ref arguments, argCount);
					result = new ConstructedType(genericType, arguments);
					constructedTypes.Add(tuple, result);
				}
			}

			return result;
		}

		/// <summary>
		/// Should be called on types which are created during full analysis.
		/// </summary>
		internal ConstructedType AnalyzeConstructedType(DType type)
		{
			ConstructedType cted = type as ConstructedType;
			if (cted != null)
			{
				cted.Analyze(this);
			}
			return cted;
		}

		internal void DefineConstructedTypeBuilders()
		{
			foreach (ConstructedType type in constructedTypes.Values)
			{
				// perform the analysis on the type if it hasn't been performed previously:
				type.Analyze(this);

				// define builders:
				type.DefineBuilders();
			}
		}

		#endregion

		#region Enter/Leave functions

		internal void EnterNamespace(NamespaceDecl ns)
		{
			Debug.Assert(!currentNamespace.HasValue, "Namespace nesting not supported");
			currentNamespace = ns.QualifiedName;
		}

		internal void LeaveNamespace()
		{
			currentNamespace = null;
		}

		/// <summary>
		/// Notices the analyzer that function declaration is entered.
		/// </summary>
		internal void EnterFunctionDeclaration(PhpRoutine/*!*/ function)
		{
            Debug.Assert(function.IsFunction);

			RoutineDeclLoc f = new RoutineDeclLoc(function, locationStack.Count);
			routineDeclStack.Push(f);
			locationStack.Push(f);

			EnterConditionalCode();
		}

		internal void LeaveFunctionDeclaration()
		{
			Debug.Assert(routineDeclStack.Count > 0);
			Debug.Assert(locationStack.Count > 0);
			Debug.Assert(locationStack.Peek() is RoutineDeclLoc);
			Debug.Assert(routineDeclStack.Peek() == locationStack.Peek());

			routineDeclStack.Pop();
			locationStack.Pop();

			LeaveConditionalCode();
		}

		/// <summary>
		/// Notices the analyzer that method declaration is entered.
		/// </summary>
		internal void EnterMethodDeclaration(PhpMethod/*!*/ method)
		{
			//function declared within a method is global function 
			//=> method is only declared direct within a class declaration
			Debug.Assert(locationStack.Peek().Kind == Locations.TypeDecl);

			RoutineDeclLoc m = new RoutineDeclLoc(method, locationStack.Count);
			routineDeclStack.Push(m);
			locationStack.Push(m);

			EnterConditionalCode();
		}

		internal void LeaveMethodDeclaration()
		{
			Debug.Assert(routineDeclStack.Count > 0);
			Debug.Assert(locationStack.Count > 0);
			Debug.Assert(locationStack.Peek() is RoutineDeclLoc);
			Debug.Assert(routineDeclStack.Peek() == locationStack.Peek());

			routineDeclStack.Pop();
			locationStack.Pop();

			LeaveConditionalCode();
		}

        /// <summary>
		/// Notices the analyzer that class declaration is entered.
		/// </summary>
		internal void EnterTypeDecl(PhpType type)
		{
			TypeDeclLocation c = new TypeDeclLocation(type, locationStack.Count);
			typeDeclStack.Push(c);
			locationStack.Push(c);
		}

		internal void LeaveTypeDecl()
		{
			Debug.Assert(typeDeclStack.Count > 0);
			Debug.Assert(locationStack.Count > 0);
			Debug.Assert(locationStack.Peek() is TypeDeclLocation);
			Debug.Assert(typeDeclStack.Peek() == locationStack.Peek());

			typeDeclStack.Pop();
			locationStack.Pop();
		}


		internal void EnterActualParams(RoutineSignature/*!*/ signature, int actualParamCount)
		{
			locationStack.Push(new ActualParamsLocation(signature, actualParamCount));
		}

		internal void LeaveActualParams()
		{
			Debug.Assert(locationStack.Peek() is ActualParamsLocation);
			locationStack.Pop();
		}

		internal void EnterActParam()
		{
			((ActualParamsLocation)CurrentLocation).MoveToNextParam();
		}

		internal void LeaveActParam()
		{
			//do nothing
		}

		internal void EnterLoopBody()
		{
			EnterConditionalCode();
			loopNestingLevel++;
		}

		internal void LeaveLoopBody()
		{
			LeaveConditionalCode();
			loopNestingLevel--;
			Debug.Assert(loopNestingLevel > -1);
		}

		#endregion

		#region Switch Statement Handling

		internal void EnterSwitchBody()
		{
			loopNestingLevel++;

			locationStack.Push(new SwitchLocation());
		}

		internal void LeaveSwitchBody()
		{
			loopNestingLevel--;
			Debug.Assert(loopNestingLevel > -1);

			Debug.Assert(locationStack.Peek() is SwitchLocation);
			locationStack.Pop();
		}

		internal void AddConstCaseToCurrentSwitch(object value, Position position)
		{
			SwitchLocation current_switch = (SwitchLocation)CurrentLocation;

			if (current_switch.ConstCases.IndexOf(value) > -1)
				ErrorSink.Add(Warnings.MultipleSwitchCasesWithSameValue, SourceUnit, position, value);
			else
				current_switch.ConstCases.Add(value);
		}

		internal void AddDefaultToCurrentSwitch(Position position)
		{
			SwitchLocation current_switch = (SwitchLocation)CurrentLocation;

			if (current_switch.ContainsDefault)
				ErrorSink.Add(Warnings.MoreThenOneDefaultInSwitch, SourceUnit, position);
			else
				current_switch.ContainsDefault = true;
		}

		#endregion

		#region Name Resolving

		private Scope GetReferringScope(PhpType referringType, PhpRoutine referringRoutine)
		{
			if (referringType != null) return referringType.Declaration.Scope;
            if (referringRoutine is PhpFunction) return ((PhpFunction)referringRoutine).Declaration.Scope;
            //if (referringRoutine is PhpLambdaFunction) ...

			// used for global statements during full analysis:
			Debug.Assert(currentScope.IsValid, "Scope is available only during full analysis.");
			return currentScope;
		}

		public DRoutine/*!*/ ResolveFunctionName(QualifiedName qualifiedName, Position position)
		{
			Debug.Assert(currentScope.IsValid, "Scope is available only during full analysis.");

			QualifiedName? alias;
			DRoutine result = sourceUnit.ResolveFunctionName(qualifiedName, currentScope, out alias, ErrorSink, position, false);

			if (result.IsUnknown)
			{
				if (alias.HasValue)
					ErrorSink.Add(Warnings.UnknownFunctionUsedWithAlias, SourceUnit, position, qualifiedName, alias);
				else
					ErrorSink.Add(Warnings.UnknownFunctionUsed, SourceUnit, position, qualifiedName);
			}

			return result;
		}

		public DType ResolveType(object typeNameOrPrimitiveType, PhpType referringType, PhpRoutine referringRoutine,
				Position position, bool mustResolve)
		{
			Debug.Assert(typeNameOrPrimitiveType == null || typeNameOrPrimitiveType is PrimitiveType
			  || typeNameOrPrimitiveType is GenericQualifiedName);

			DType result = typeNameOrPrimitiveType as PrimitiveType;
			if (result != null)
				return result;

			if (typeNameOrPrimitiveType != null)
			{
				return ResolveTypeName((GenericQualifiedName)typeNameOrPrimitiveType, referringType,
							referringRoutine, position, mustResolve);
			}

			return null;
		}

		public DType/*!*/ ResolveTypeName(QualifiedName qualifiedName, PhpType referringType,
			PhpRoutine referringRoutine, Position position, bool mustResolve)
		{
			DType result;

			if (qualifiedName.IsSelfClassName)
			{
				if (referringType != null)
				{
					result = referringType;
				}
				else
				{
					// we are sure the self is used incorrectly in function:
					if (referringRoutine != null)
						ErrorSink.Add(Errors.SelfUsedOutOfClass, SourceUnit, position);

					// global code can be included to the method:
					result = UnknownType.UnknownSelf;
				}
			}
            else if (qualifiedName.IsStaticClassName)
            {
                if (referringType != null)
                {
                    if (referringType.IsFinal)
                    {
                        // we are sure the 'static' == 'self'
                        result = referringType;
                    }
                    else
                    {
                        if (referringRoutine != null)
                            referringRoutine.Properties |= RoutineProperties.LateStaticBinding;

                        result = StaticType.Singleton;
                    }
                }
                else
                {
                    // we are sure the static is used incorrectly in function:
                    //if (referringRoutine != null) // do not allow 'static' in global code:
                        ErrorSink.Add(Errors.StaticUsedOutOfClass, SourceUnit, position);

                    // global code can be included to the method:
                    result = UnknownType.UnknownStatic;
                }
            }
            else if (qualifiedName.IsParentClassName)
            {
                if (referringType != null)
                {
                    if (referringType.IsInterface)
                    {
                        ErrorSink.Add(Errors.ParentUsedOutOfClass, SourceUnit, position);
                        result = UnknownType.UnknownParent;
                    }
                    else
                    {
                        DType base_type = referringType.Base;
                        if (base_type == null)
                        {
                            ErrorSink.Add(Errors.ClassHasNoParent, SourceUnit, position, referringType.FullName);
                            result = UnknownType.UnknownParent;
                        }
                        else
                        {
                            result = base_type;
                        }
                    }
                }
                else
                {
                    // we are sure the self is used incorrectly when we are in a function:
                    if (referringRoutine != null)
                        ErrorSink.Add(Errors.ParentUsedOutOfClass, SourceUnit, position);

                    // global code can be included to the method:
                    result = UnknownType.UnknownParent;
                }
            }
            else
            {
                // try resolve the name as a type parameter name:
                if (qualifiedName.IsSimpleName)
                {
                    result = ResolveTypeParameterName(qualifiedName.Name, referringType, referringRoutine);
                    if (result != null)
                        return result;
                }

                Scope referring_scope = GetReferringScope(referringType, referringRoutine);
                QualifiedName? alias;
                result = sourceUnit.ResolveTypeName(qualifiedName, referring_scope, out alias, ErrorSink, position, mustResolve);

                ReportUnknownType(result, alias, position);
            }

			return result;
		}

		private GenericParameter ResolveTypeParameterName(Name name, PhpType referringType, PhpRoutine referringRoutine)
		{
			GenericParameter result = null;

			if (referringRoutine != null)
			{
				result = referringRoutine.Signature.GetGenericParameter(name);
				if (result != null)
					return result;
			}

			if (referringType != null)
			{
				result = referringType.GetGenericParameter(name);
				if (result != null)
					return result;
			}

			return result;
		}

		private void ReportUnknownType(DType/*!*/ type, QualifiedName? alias, Position position)
		{
			if (type.IsUnknown)
			{
				if (alias.HasValue)
					ErrorSink.Add(Warnings.UnknownClassUsedWithAlias, SourceUnit, position, type.FullName, alias);
				else
					ErrorSink.Add(Warnings.UnknownClassUsed, SourceUnit, position, type.FullName);
			}
		}

		public DType/*!*/ ResolveTypeName(GenericQualifiedName genericName, PhpType referringType,
			PhpRoutine referringRoutine, Position position, bool mustResolve)
		{
			DType type = ResolveTypeName(genericName.QualifiedName, referringType, referringRoutine, position, mustResolve);

			DTypeDesc[] arguments = (genericName.GenericParams.Length > 0) ? new DTypeDesc[genericName.GenericParams.Length] : DTypeDesc.EmptyArray;

			for (int i = 0; i < arguments.Length; i++)
			{
				arguments[i] = ResolveType(genericName.GenericParams[i], referringType, referringRoutine, position, mustResolve).TypeDesc;
			}

			return type.MakeConstructedType(this, arguments, position);
		}

		/// <summary>
		/// Gets the type for specified attribute type name.
		/// </summary>
		public DType/*!*/ ResolveCustomAttributeType(QualifiedName qualifiedName, Scope referringScope, Position position)
		{
			if (qualifiedName.IsAppStaticAttributeName)
			{
				return CustomAttribute.AppStaticAttribute;
			}
			else if (qualifiedName.IsExportAttributeName)
			{
				return CustomAttribute.ExportAttribute;
			}
			else if (qualifiedName.IsOutAttributeName)
			{
				return CustomAttribute.OutAttribute;
			}
			else
			{
				QualifiedName? alias;
				QualifiedName name = new QualifiedName(new Name(qualifiedName.Name.Value + "Attribute"), qualifiedName.Namespaces);

				DType type = sourceUnit.ResolveTypeName(name, referringScope, out alias, ErrorSink, position, true);

				if (type.IsUnknown)
					type = sourceUnit.ResolveTypeName(qualifiedName, referringScope, out alias, ErrorSink, position, true);

				ReportUnknownType(type, alias, position);

				return type;
			}
		}


        /// <summary>
        /// Resolves a method of given <see cref="DType"/> by its name.
        /// </summary>
        /// <param name="type">The type of routine being resolved.</param>
        /// <param name="methodName">The name of routine to be resolved.</param>
        /// <param name="position">Position of method call used for error reporting.</param>
        /// <param name="referringType">The type where the seached routine is being called. Can be <c>null</c>.</param>
        /// <param name="referringRoutine">The routine where the searched routine is being called. Can be <c>null</c>.</param>
        /// <param name="calledStatically">True if the searched routine is called statically - if it uses static method call syntax.
        /// This affects the __call or __callStatic method lookup.
        /// It affects also the error reporting, where for instance method calls, the bad visibility error is
        /// ignored and falls back to return <see cref="UnknownMethod"/>.</param>
        /// <param name="checkVisibilityAtRuntime">Will determine if the routine call must be checked for visibility at runtime.</param>
        /// <param name="isCallMethod">Will determine if __call or __callStatic magic methods were found instead.</param>
        /// <returns>The resolved routine. Cannot return <c>null</c>.</returns>
		public DRoutine/*!*/ ResolveMethod(DType/*!*/ type, Name methodName, Position position,
			PhpType referringType, PhpRoutine referringRoutine, bool calledStatically,
            out bool checkVisibilityAtRuntime, out bool isCallMethod)
		{
			checkVisibilityAtRuntime = false;
            isCallMethod = false;

			// we cannot resolve a method unless we know the inherited members:
			if (type.IsDefinite)
			{
				KnownType known;

				// the method is a constructor:
				if (methodName.IsConstructName || (known = type as KnownType) != null && methodName.Equals(known.QualifiedName.Name))
					return ResolveConstructor(type, position, referringType, referringRoutine, out checkVisibilityAtRuntime);

				DRoutine routine;
				GetMemberResult member_result;

				member_result = type.GetMethod(methodName, referringType, out routine);

                // Look for __call or __callStatic magic methods if no method was found:
                // Note: __call when looking for instance method is disabled, since there can be the searched method in some future override.
                if (member_result == GetMemberResult.NotFound && calledStatically)
                {
                    // in PHP, it is possible to call instance methods statically if we are in instance method context.
                    // In such case we have to look for __call instead of __callStatic:
                    
                    // determine the proper call method:
                    // use __call for instance method invocation, including static method invocation within the current type (e.g. A::foo(), parent::foo(), ...)
                    // use __callStatic for static method invocation
                    Name callMethodName =
                        (!calledStatically ||   // just to have complete condition here, always false
                        (referringRoutine != null && referringType != null && !referringRoutine.IsStatic &&  // in non-static method
                        type.TypeDesc.IsAssignableFrom(referringType.TypeDesc)) // {CurrentType} is inherited from or equal {type}
                        ) ? DObject.SpecialMethodNames.Call : DObject.SpecialMethodNames.CallStatic;

                    member_result = type.GetMethod(callMethodName, referringType, out routine);

                    if (member_result != GetMemberResult.NotFound)
                        isCallMethod = true;
                }

				switch (member_result)
				{
					case GetMemberResult.OK:
						return routine;

					case GetMemberResult.NotFound:
                        if (calledStatically) // throw an error only in we are looking for static method, instance method can be defined in some future inherited class
                            ErrorSink.Add(Errors.UnknownMethodCalled, SourceUnit, position, type.FullName, methodName);
						return new UnknownMethod(type, methodName.Value);

					case GetMemberResult.BadVisibility:
						{
                            if (!calledStatically)    // instance method will check the routine dynamically, there can be some override later
                                return new UnknownMethod(type, methodName.Value);

							if (referringType == null && referringRoutine == null)
							{
								// visibility must be checked at run-time:
								checkVisibilityAtRuntime = true;
								return routine;
							}
							else
							{
								// definitive error:
								if (routine.IsPrivate)
								{
									ErrorSink.Add(Errors.PrivateMethodCalled, SourceUnit, position, type.FullName, methodName.Value,
										referringType.FullName);
								}
								else
								{
									ErrorSink.Add(Errors.ProtectedMethodCalled, SourceUnit, position, type.FullName, methodName.Value,
					  referringType.FullName);
								}

								return new UnknownMethod(type, methodName.Value);
							}
						}

					default:
						Debug.Fail();
						return null;
				}
			}
			else
			{
				// warning (if any) reported by the type resolver:
				return new UnknownMethod(type, methodName.Value);
			}
		}

		/// <summary>
		/// Resolves constructor of the specified type within the current context (location).
		/// </summary>
		public DRoutine/*!*/ ResolveConstructor(DType/*!*/ type, Position position, PhpType referringType,
			PhpRoutine referringRoutine, out bool checkVisibilityAtRuntime)
		{
			checkVisibilityAtRuntime = false;
			KnownRoutine ctor;

			// Do resolve ctor despite of the indefiniteness of the type to make error reporting consistent
			// when accessing the constructors thru the new operator.

			switch (type.GetConstructor(referringType, out ctor))
			{
				case GetMemberResult.OK:
					return ctor;

				case GetMemberResult.NotFound:
					// default ctor to be used:
					return new UnknownMethod(type);

				case GetMemberResult.BadVisibility:
					if (referringType == null && referringRoutine == null)
					{
						// visibility must be checked at run-time:
						checkVisibilityAtRuntime = true;
						return ctor;
					}
					else
					{
						// definitive error:
						if (ctor.IsPrivate)
						{
							ErrorSink.Add(Errors.PrivateCtorCalled, SourceUnit, position, type.FullName,
								ctor.FullName, referringType.FullName);
						}
						else
						{
							ErrorSink.Add(Errors.ProtectedCtorCalled, SourceUnit, position, type.FullName,
								ctor.FullName, referringType.FullName);
						}

						return new UnknownMethod(type);
					}

				default:
					Debug.Fail();
					return null;
			}
		}

		/// <summary>
		/// Resolves static properties.
		/// </summary>
		public DProperty/*!*/ ResolveProperty(DType/*!*/ type, VariableName propertyName, Position position, bool staticOnly,
			PhpType referringType, PhpRoutine referringRoutine, out bool checkVisibilityAtRuntime)
		{
			Debug.Assert(type != null);

			checkVisibilityAtRuntime = false;

			// we cannot resolve a property unless we know the inherited members:
			if (type.IsDefinite)
			{
				DProperty property;
				GetMemberResult member_result = type.GetProperty(propertyName, referringType, out property);

				switch (member_result)
				{
					case GetMemberResult.OK:
						if (staticOnly && !property.IsStatic) goto case GetMemberResult.NotFound;
						return property;

					case GetMemberResult.NotFound:
						ErrorSink.Add(Errors.UnknownPropertyAccessed, SourceUnit, position, type.FullName, propertyName);
						return new UnknownProperty(type, propertyName.Value);

					case GetMemberResult.BadVisibility:
						if (referringType == null && referringRoutine == null)
						{
							// visibility must be checked at run-time:
							checkVisibilityAtRuntime = true;
							return property;
						}
						else
						{
							// definitive error:
							if (property.IsPrivate)
							{
								ErrorSink.Add(Errors.PrivatePropertyAccessed, SourceUnit, position, type.FullName, propertyName.Value,
									referringType.FullName);
							}
							else
							{
								ErrorSink.Add(Errors.ProtectedPropertyAccessed, SourceUnit, position, type.FullName, propertyName.Value,
									referringType.FullName);
							}

							return new UnknownProperty(type, propertyName.Value);
						}

					default:
						Debug.Fail();
						throw null;
				}
			}
			else
			{
				// warning (if any) reported by the type resolver:
				return new UnknownProperty(type, propertyName.Value);
			}
		}

		internal DConstant ResolveClassConstantName(DType/*!*/ type, VariableName constantName,
			Position position, PhpType referringType, PhpRoutine referringRoutine, out bool checkVisibilityAtRuntime)
		{
			checkVisibilityAtRuntime = false;

			// we cannot resolve a class constant unless we know the inherited members:
			if (type.IsDefinite)
			{
				ClassConstant constant;
				GetMemberResult member_result = type.GetConstant(constantName, referringType, out constant);

				switch (member_result)
				{
					case GetMemberResult.OK:
						return constant;

					case GetMemberResult.NotFound:
						ErrorSink.Add(Errors.UnknownClassConstantAccessed, SourceUnit, position, type.FullName, constantName);
						return new UnknownClassConstant(type, constantName.Value);

					case GetMemberResult.BadVisibility:
						if (referringType == null && referringRoutine == null)
						{
							// visibility must be checked at run-time:
							checkVisibilityAtRuntime = true;
							return constant;
						}
						else
						{
							// definitive error:
							if (constant.IsPrivate)
							{
								ErrorSink.Add(Errors.PrivateConstantAccessed, SourceUnit, position, type.FullName, constantName.Value,
				  referringType.FullName);
							}
							else
							{
								ErrorSink.Add(Errors.ProtectedConstantAccessed, SourceUnit, position, type.FullName, constantName.Value,
				  referringType.FullName);
							}

							return new UnknownClassConstant(type, constantName.Value);
						}

					default:
						Debug.Fail();
						throw null;
				}
			}
			else
			{
				// warning (if any) reported by the type resolver:
				return new UnknownClassConstant(type, constantName.Value);
			}
		}

		internal DConstant ResolveGlobalConstantName(QualifiedName qualifiedName, Position position)
		{
			Debug.Assert(currentScope.IsValid, "Scope is available only during full analysis.");

			QualifiedName? alias;
			DConstant result = sourceUnit.ResolveConstantName(qualifiedName, currentScope, out alias, ErrorSink, position, false);

			if (result.IsUnknown)
			{
				if (alias.HasValue)
					ErrorSink.Add(Warnings.UnknownConstantUsedWithAlias, SourceUnit, position, qualifiedName, alias);
				else
				{
					// TODO:
					// ErrorSink.Add(Warnings.UnknownConstantUsed, SourceUnit, position, qualifiedName);
					// do not report unknown constants (they may be defined by define() as well as added at run-time by Phalanger)
					// future feature: analyzed define()'s and check run-time added constants
				}
			}

			return result;
		}

		#endregion

		#region Miscellaneous

		public void AddLambdaFcnDeclaration(FunctionDecl decl)
		{
			sourceUnit.Ast.Statements.Add(decl);
		}

#if !SILVERLIGHT
		internal void SetEntryPoint(PhpRoutine/*!*/ routine, Position position)
		{
			// pure entry point is a static parameterless "Main" method/function:
			if (!sourceUnit.CompilationUnit.IsPure || !routine.Name.Equals(PureAssembly.EntryPointName)
			  || !routine.IsStatic || routine.Signature.ParamCount > 0)
				return;

			PureCompilationUnit pcu = (PureCompilationUnit)sourceUnit.CompilationUnit;

			if (pcu.EntryPoint != null)
			{
				ErrorSink.Add(Errors.EntryPointRedefined, SourceUnit, position);
				ErrorSink.Add(Errors.RelatedLocation, pcu.EntryPoint.SourceUnit, pcu.EntryPoint.Position);
			}
			else
			{
				pcu.SetEntryPoint(routine);
			}
		}
#else
		internal void SetEntryPoint(PhpRoutine/*!*/ routine, Position position)
		{
			// nothing to do here on silverlight..
		}
#endif

		internal static void ValidateLabels(ErrorSink/*!*/ errors, SourceUnit/*!*/ sourceUnit,
			Dictionary<VariableName, Statement>/*!*/ labels)
		{
			foreach (KeyValuePair<VariableName, Statement> entry in labels)
			{
				LabelStmt label = entry.Value as LabelStmt;
				if (label != null)
				{
					if (!label.IsReferred)
						errors.Add(Warnings.UnusedLabel, sourceUnit, label.Position, entry.Key);
				}
				else
				{
					errors.Add(Errors.UndefinedLabel, sourceUnit, entry.Value.Position, entry.Key);
				}
			}
		}

		#endregion
	}
}
