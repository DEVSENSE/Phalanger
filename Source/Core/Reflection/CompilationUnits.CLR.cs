/*

 Copyright (c) 2006 Tomas Matousek.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/
using System;
using System.Collections.Generic;
using System.Text;
using PHP.Core.Parsers;
using PHP.Core.Emit;
using System.Reflection.Emit;
using System.Reflection;
using System.Diagnostics.SymbolStore;
using System.IO;

namespace PHP.Core.Reflection
{
	#region CompilationUnit

	/// <summary>
	/// Base class for standard PHP script compilation unit
	/// </summary>
	public abstract class CompilationUnit : CompilationUnitBase
	{
		#region Constructor

		public CompilationUnit()
		{
			// nop
		}

		public CompilationUnit(PhpModule/*!*/ module)
			: base(module)
		{
			// nop
		}

		#endregion

		#region State

		public enum States
		{
			/// <summary>
			/// Compilation unit has just been created.
			/// </summary>
			Initial,

			/// <summary>
			/// There is an error in the unit.
			/// </summary>
			Erroneous,

			/// <summary>
			/// Source files of the unit have been parsed and the AST is available.
			/// </summary>
			Parsed,

			/// <summary>
			/// Tables needn't to be complete (the node may be involved in MPF).
			/// </summary>
			Processed,

			/// <summary>
			/// The unit has been compiled, AST is not available any more, tables are.
			/// </summary>
			Compiled,

			/// <summary>
			/// The unit has been reflected, AST is not available, tables are.
			/// </summary>
			Reflected,

			/// <summary>
			/// Pre-analysis performed.
			/// </summary>
			PreAnalyzed,

			/// <summary>
			/// Member analysis performed.
			/// </summary>
			MembersAnalyzed,

			/// <summary>
			/// Full analysis performed.
			/// </summary>
			Analyzed,

			/// <summary>
			/// Builders defined.
			/// </summary>
			BuildersDefined,

			/// <summary>
			/// AST emitted.
			/// </summary>
			Emitted
		}

		#endregion

		#region Properties

		public override bool IsPure { get { return false; } }
		public override bool IsTransient { get { return false; } }

		public States State { get { return state; } internal /* InclusionGraphBuilder */ set { state = value; } }
		protected States state;

		public List<StaticInclusion>/*!*/ Inclusions { get { return inclusions; } }
		protected readonly List<StaticInclusion>/*!*/ inclusions = new List<StaticInclusion>();

		public List<StaticInclusion>/*!*/ Includers { get { return includers; } }
		protected readonly List<StaticInclusion>/*!*/ includers = new List<StaticInclusion>();

		#endregion

		#region Clean-up

		public abstract void CleanUp(CompilationContext/*!*/ context, bool successful);

		#endregion

		#region Referenced by compiler

		/// <summary>
		/// Source file path to be used when emiting inclusion.
		/// </summary>
		public abstract string RelativeSourcePath { get; }

		/// <summary>
		/// System.Type of the main module class
		/// </summary>
		public abstract Type ScriptClassType { get; }

		/// <summary>
		/// MethodInfo of the main module 'Main' method
		/// </summary>
		public abstract MethodInfo MainHelper { get; }

		#endregion

		#region Abstract methods

		/// <summary>
		/// Reflect types, functions and constants in compilation unit
		/// </summary>
		public abstract void Reflect();


		/// <summary>
		/// Used for merging type tables (in ScriptCompilationUnit)
		/// </summary>
		/// <returns>Returns all reflected or declared types</returns>
		public abstract IEnumerable<KeyValuePair<QualifiedName, ScopedDeclaration<DType>>> GetVisibleTypes();

		/// <summary>
		/// Used for merging function tables (in ScriptCompilationUnit)
		/// </summary>
		/// <returns>Returns all reflected or declared functions</returns>
		public abstract IEnumerable<KeyValuePair<QualifiedName, ScopedDeclaration<DRoutine>>> GetVisibleFunctions();

		/// <summary>
		/// Used for merging constant tables (in ScriptCompilationUnit)
		/// </summary>
		/// <returns>Returns all reflected or declared constants</returns>
		public abstract IEnumerable<KeyValuePair<QualifiedName, ScopedDeclaration<DConstant>>> GetVisibleConstants();

		#endregion
	}

	#endregion

	#region ReflectedCompilationUnit

	/// <summary>
	/// This compilation unit is used while reflecting compiled SSA or MSA assembly.
	/// </summary>
	public sealed class ReflectedCompilationUnit : CompilationUnit
	{
		#region Construction

		/// <summary>
		/// Used by reflection.
		/// </summary>
		public ReflectedCompilationUnit(ScriptModule/*!*/ module)
			: base(module)
		{
			Debug.Assert(module != null);

			this.state = States.Compiled;
		}

		#endregion

		#region Fields

		// filled by reflect method
        private string relativePath;
		private Type scriptClassType;
		private MethodInfo mainHelper;

		private Dictionary<QualifiedName, DRoutine> functions;
		private Dictionary<QualifiedName, DType> types;
		private Dictionary<QualifiedName, DConstant> constants;

		#endregion

		#region Declaration Look-up

		public override DRoutine GetVisibleFunction(QualifiedName qualifiedName, ref string fullName/*!*/, Scope currentScope)
		{
			Debug.Assert(functions != null);

			DRoutine ret;
			if (functions.TryGetValue(qualifiedName, out ret)) return ret;

			// this won't be used because tables are merged
			throw new NotImplementedException();
		}

		public override DType GetVisibleType(QualifiedName qualifiedName, ref string fullName/*!*/, Scope currentScope,
			bool mustResolve)
		{
			Debug.Assert(types != null);

			DType ret;
			if (types.TryGetValue(qualifiedName, out ret)) return ret;

			// this won't be used because tables are merged
			throw new NotImplementedException();
		}

		public override DConstant GetVisibleConstant(QualifiedName qualifiedName, ref string fullName/*!*/, Scope currentScope)
		{
			Debug.Assert(constants != null);

			DConstant ret;
			if (constants.TryGetValue(qualifiedName, out ret)) return ret;

			// this won't be used because tables are merged
			throw new NotImplementedException();
		}

		/// <summary>
		/// Used for merging type tables (in ScriptCompilationUnit)
		/// </summary>
		/// <returns>Returns all reflected or declared types</returns>
		public override IEnumerable<KeyValuePair<QualifiedName, ScopedDeclaration<DType>>> GetVisibleTypes()
		{
			foreach (KeyValuePair<QualifiedName, DType> it in types)
				yield return new KeyValuePair<QualifiedName, ScopedDeclaration<DType>>(
					it.Key, new ReflectedScopedDeclaration<DType>(Scope.Global, it.Value));
		}

		/// <summary>
		/// Used for merging function tables (in ScriptCompilationUnit)
		/// </summary>
		/// <returns>Returns all reflected or declared functions</returns>
		public override IEnumerable<KeyValuePair<QualifiedName, ScopedDeclaration<DRoutine>>> GetVisibleFunctions()
		{
			foreach (KeyValuePair<QualifiedName, DRoutine> it in functions)
				yield return new KeyValuePair<QualifiedName, ScopedDeclaration<DRoutine>>(
					it.Key, new ReflectedScopedDeclaration<DRoutine>(Scope.Global, it.Value));
		}

		/// <summary>
		/// Used for merging constant tables (in ScriptCompilationUnit)
		/// </summary>
		/// <returns>Returns all reflected or declared constants</returns>
		public override IEnumerable<KeyValuePair<QualifiedName, ScopedDeclaration<DConstant>>> GetVisibleConstants()
		{
			foreach (KeyValuePair<QualifiedName, DConstant> it in constants)
				yield return new KeyValuePair<QualifiedName, ScopedDeclaration<DConstant>>(
					it.Key, new ReflectedScopedDeclaration<DConstant>(Scope.Global, it.Value));
		}


		/// <summary>
		/// Returns declared types - in reflected unit we don't return anything
		/// </summary>
		public override IEnumerable<PhpType>/*!*/ GetDeclaredTypes()
		{
			yield break;
		}

		/// <summary>
		/// Returns declared functions - in reflected unit we don't return anything
		/// </summary>
		public override IEnumerable<PhpFunction>/*!*/ GetDeclaredFunctions()
		{
			yield break;
		}

		/// <summary>
		/// Returns declared constants - in reflected unit we don't return anything
		/// </summary>
		public override IEnumerable<GlobalConstant>/*!*/ GetDeclaredConstants()
		{
			yield break;
		}

		#endregion

		#region Reflection

		/// <summary>
		/// Perform reflection on the compiled assembly
		/// </summary>
		public override void Reflect()
		{
			Debug.Assert(state == States.Compiled);

			// temporary dictionaries
			Dictionary<string, DTypeDesc> typesTmp = new Dictionary<string, DTypeDesc>();
			Dictionary<string, DRoutineDesc> functionsTmp = new Dictionary<string, DRoutineDesc>();
			DualDictionary<string, DConstantDesc> constantsTmp = new DualDictionary<string, DConstantDesc>
				(null, StringComparer.OrdinalIgnoreCase);

			// call reflect method of the module (and its includees)
			ScriptModule scriptModule = (ScriptModule)module;
			scriptModule.Reflect(false, typesTmp, functionsTmp, constantsTmp);

			// build local dictionaries
			functions = new Dictionary<QualifiedName, DRoutine>();
			types = new Dictionary<QualifiedName, DType>();
            constants = new Dictionary<QualifiedName, DConstant>(ConstantQualifiedNameComparer.Singleton);

			// get the <Script> class
			scriptClassType = scriptModule.ScriptType;
			if (scriptClassType == null)
				throw new ReflectionException("The compiled assembly doesn't contain main script class!");

			ScriptAttribute sa = ScriptAttribute.Reflect(scriptClassType);
            if (sa != null)
            {
                relativePath = sa.RelativePath;
            }
            else
            {
                // TODO: this needs to be revised after script library DLLs are somewhat united with SSA's and WebPages.dll
                //       (i.e. after all have ScriptAttribute)
                relativePath = ((ScriptModule)module).RelativeSourcePath;

                if (relativePath == null)
                {
                    throw new ReflectionException("Script in the compiled assembly doesn't contain ScriptAttribute!");
                }
            }

			mainHelper = scriptModule.MainHelper;

			foreach (KeyValuePair<string, DTypeDesc> val in typesTmp)
                types.Add(QualifiedName.FromClrNotation(val.Value.RealType), val.Value.Type);   // TODO: parse the val.Key
			foreach (KeyValuePair<string, DConstantDesc> val in constantsTmp)
                constants.Add(QualifiedName.FromClrNotation(val.Key, true), val.Value.GlobalConstant);// TODO: parse the val.Key
			foreach (KeyValuePair<string, DRoutineDesc> val in functionsTmp)
                functions.Add(QualifiedName.FromClrNotation(val.Key, true), val.Value.Routine);// TODO: parse the val.Key

			state = States.Reflected;
		}

		#endregion

		#region Clean-up

		public override void CleanUp(CompilationContext/*!*/ context, bool successful)
		{
		}

		#endregion

		#region Referenced by compiler

		/// <summary>
		/// Source file path to be used when emiting inclusion
		/// </summary>
		public override string RelativeSourcePath
		{
			get { return relativePath; }
		}


		/// <summary>
		/// System.Type of the main module class
		/// </summary>
		public override Type ScriptClassType
		{
			get { return scriptClassType; }
		}


		/// <summary>
		/// MethodInfo of the main module 'Main' method
		/// </summary>
		public override MethodInfo MainHelper
		{
			get { return mainHelper; }
		}

		#endregion
	}

	#endregion

	#region PureCompilationUnit

	/// <summary>
	/// Pure units: 
	/// no inclusions, 
	/// no global code except top-level statements (function decl, class decl, namespace decl, global constant decl)
	/// </summary>
	public sealed class PureCompilationUnit : CompilationUnitBase, IReductionsSink
	{
		public override bool IsPure { get { return true; } }
		public override bool IsTransient { get { return false; } }

		public PureModule PureModule { get { return (PureModule)module; } }

		private Dictionary<QualifiedName, Declaration> types = null;
		private Dictionary<QualifiedName, Declaration> functions = null;
		private Dictionary<QualifiedName, Declaration> constants = null;

		public PhpRoutine EntryPoint { get { return entryPoint; } }
		private PhpRoutine entryPoint;

		/// <summary>
		/// Whether the unit is only parsed, not compiled.
		/// </summary>
		private bool parsingOnly;

		/// <summary>
		/// For parsed-only units, skips errors related to purity violations.
		/// </summary>
		private bool relaxPurity;

		#region Construction

		public PureCompilationUnit(bool parsingOnly, bool relaxPurity)
			: base()
		{
			this.parsingOnly = parsingOnly;

			if (parsingOnly)
			{
				PureAssembly a = new PureAssembly(ApplicationContext.Default);
				this.module = a.Module = new PureModule(a);
				this.relaxPurity = relaxPurity;
			}
			else
			{
				if (relaxPurity) throw new InvalidOperationException();
			}
		}

		#endregion

		#region Declarations: look-up and enumeration

		public override DRoutine GetVisibleFunction(QualifiedName qualifiedName, ref string fullName/*!*/, Scope currentScope)
		{
			// scope is ignored here

			Declaration result;
			if (functions.TryGetValue(qualifiedName, out result))
				return (PhpFunction)result.Declaree;

			// search application context:
			return module.Assembly.ApplicationContext.GetFunction(qualifiedName, ref fullName);
		}

		public override DType GetVisibleType(QualifiedName qualifiedName, ref string fullName/*!*/, Scope currentScope,
			bool mustResolve)
		{
			// scope is ignored here

			Declaration result;
			if (types.TryGetValue(qualifiedName, out result))
				return (PhpType)result.Declaree;

			// search application context:
			return module.Assembly.ApplicationContext.GetType(qualifiedName, ref fullName);
		}

		public override DConstant GetVisibleConstant(QualifiedName qualifiedName, ref string fullName/*!*/, Scope currentScope)
		{
			// scope is ignored here

			Declaration result;
			if (constants.TryGetValue(qualifiedName, out result))
				return (GlobalConstant)result.Declaree;

			// search application context:
			return module.Assembly.ApplicationContext.GetConstant(qualifiedName, ref fullName);
		}

		public override IEnumerable<PhpType>/*!*/ GetDeclaredTypes()
		{
			// filtering is not necessary, however, we may reuse the iterator:
			return Declaration.GetDeclarees<PhpType>(types.Values);
		}

		public override IEnumerable<PhpFunction>/*!*/ GetDeclaredFunctions()
		{
			// filtering is not necessary, however, we may reuse the iterator:
			return Declaration.GetDeclarees<PhpFunction>(functions.Values);
		}

		public override IEnumerable<GlobalConstant>/*!*/ GetDeclaredConstants()
		{
			// filtering is not necessary, however, we may reuse the iterator:
			return Declaration.GetDeclarees<GlobalConstant>(constants.Values);
		}

		#endregion

		#region Parsing

		public S[] ParseSourceFiles<S>(IEnumerable<S>/*!*/ sourceUnits, ErrorSink/*!*/ errors,
			LanguageFeatures languageFeatures)
			where S : SourceUnit
		{
			if (sourceUnits == null)
				throw new ArgumentNullException("sourceFiles");
			if (errors == null)
				throw new ArgumentNullException("errors");

			functions = new Dictionary<QualifiedName, Declaration>();
			types = new Dictionary<QualifiedName, Declaration>();
			constants = new Dictionary<QualifiedName, Declaration>();

			Dictionary<PhpSourceFile, S> files = new Dictionary<PhpSourceFile, S>();

			bool success = true;
			foreach (S source_unit in sourceUnits)
			{
				PhpSourceFile source_file = source_unit.SourceFile;

				if (!files.ContainsKey(source_file))
				{
					try
					{
						source_unit.Parse(errors, this, Position.Initial, languageFeatures);
						files[source_file] = source_unit;
					}
					catch (CompilerException)
					{
						files[source_file] = null;
						success = false;
					}
					finally
					{
						// do not close opened source units now as their source might be used later by analyzer
					}
				}
			}

			if (!success)
				return null;

			S[] result = new S[files.Count];
			files.Values.CopyTo(result, 0);
			return result;
		}

		internal void SetEntryPoint(PhpRoutine/*!*/ routine)
		{
			Debug.Assert(entryPoint == null);
			entryPoint = routine;
		}

		#endregion

		#region Compilation

		private IEnumerable<SourceFileUnit>/*!*/ GenerateSourceFileUnits(IEnumerable<PhpSourceFile>/*!*/ sourceFiles,
			Encoding/*!*/ encoding)
		{
			foreach (PhpSourceFile source_file in sourceFiles)
				yield return new SourceFileUnit(this, source_file, encoding);
		}

		public bool Compile(IEnumerable<PhpSourceFile>/*!*/ sourceFiles, PureAssemblyBuilder/*!*/ assemblyBuilder,
			CompilationContext/*!*/ context, Encoding/*!*/ encoding)
		{
			if (parsingOnly)
				throw new InvalidOperationException();

			Debug.Assert(sourceFiles != null && assemblyBuilder != null && context != null && encoding != null);

			PureModuleBuilder module_builder = assemblyBuilder.DefineModule(this);
			this.module = module_builder;

			PureAssemblyBuilder assembly_builder = module_builder.PureAssemblyBuilder;

			Analyzer analyzer = null;
			SourceFileUnit[] source_units = null;

			try
			{
				// parse all files:

				source_units = ParseSourceFiles(GenerateSourceFileUnits(sourceFiles, encoding), context.Errors,
					context.Config.Compiler.LanguageFeatures);

				if (context.Errors.AnyFatalError) return false;

				Debug.Assert(source_units != null);

				analyzer = new Analyzer(context);

				// perform pre-analysis on types and functions:

				analyzer.PreAnalyze(types.Values);
				analyzer.PreAnalyze(functions.Values);

				// perform member analysis on types and functions:

				analyzer.AnalyzeMembers(types.Values);
				analyzer.AnalyzeMembers(functions.Values);

				if (context.Errors.AnyFatalError) return false;

				// perform full analysis:

				foreach (SourceFileUnit source_unit in source_units)
					analyzer.Analyze(source_unit);

				if (context.Errors.AnyFatalError) return false;

				// perform post analysis:
				analyzer.PostAnalyze();

				// check entry point presence:
				if (assembly_builder.IsExecutable && entryPoint == null)
					context.Errors.Add(Errors.MissingEntryPoint, null, Position.Invalid, PureAssembly.EntryPointName);
			}
			catch (CompilerException)
			{
				return false;
			}
			finally
			{
				// close opened streams (analyzer may need to read the source code due to conditional class declarations):
				if (source_units != null)
				{
					foreach (SourceFileUnit source_unit in source_units)
						source_unit.Close();
				}
			}

			// do not emit anything if there was parse/analysis error:
			if (context.Errors.AnyError) return false;

			DefineBuilders();

			// define constructed types:
			analyzer.DefineConstructedTypeBuilders();

			CodeGenerator cg = new CodeGenerator(context);

			foreach (SourceUnit source_unit in source_units)
			{
				source_unit.Emit(cg);
			}

			Bake();

			return true;
		}

		private void DefineBuilders()
		{
			foreach (Declaration declaration in types.Values)
			{
				((PhpType)declaration.Declaree).DefineBuilders();
			}

			foreach (Declaration declaration in functions.Values)
			{
				((PhpFunction)declaration.Declaree).DefineBuilders();
			}

			foreach (Declaration declaration in constants.Values)
			{
				((GlobalConstant)declaration.Declaree).DefineBuilders();
			}
		}

		private void Bake()
		{
			foreach (Declaration declaration in types.Values)
			{
				((PhpType)declaration.Declaree).Bake();
			}

			foreach (Declaration declaration in functions.Values)
			{
				((PhpFunction)declaration.Declaree).Bake();
			}

			// TODO: constants:
		}

		#endregion

		#region IReductionsSink Members

		public void InclusionReduced(Parser/*!*/ parser, AST.IncludingEx/*!*/ node)
		{
			if (!relaxPurity)
				parser.ErrorSink.Add(Errors.InclusionInPureUnit, parser.SourceUnit, node.Position);
		}

		public void FunctionDeclarationReduced(Parser/*!*/ parser, AST.FunctionDecl/*!*/ node)
		{
			AddDeclaration(parser.ErrorSink, node.Function, functions);
		}

		public void TypeDeclarationReduced(Parser/*!*/ parser, AST.TypeDecl/*!*/ node)
		{
			AddDeclaration(parser.ErrorSink, node.Type, types);
		}

		public void GlobalConstantDeclarationReduced(Parser/*!*/ parser, AST.GlobalConstantDecl/*!*/ node)
		{
			AddDeclaration(parser.ErrorSink, node.GlobalConstant, constants);
		}

		private void AddDeclaration(ErrorSink/*!*/ errors, IDeclaree/*!*/ member, Dictionary<QualifiedName, Declaration>/*!*/ table)
		{
			Declaration existing;
			Declaration current = member.Declaration;

			if (table.TryGetValue(member.QualifiedName, out existing))
			{
				if (CheckDeclaration(errors, member, existing))
					AddVersionToGroup(current, existing);
			}
			else
			{
				if (current.IsConditional)
					member.Version = new VersionInfo(1, null);

				// add a new declaration to the table:
				table.Add(member.QualifiedName, current);
			}
		}

		#endregion
	}

	#endregion

	#region ScriptCompilationUnit

	/// <summary>
	/// This compilation unit is used while compiling PHP script
	/// </summary>
	public sealed class ScriptCompilationUnit : CompilationUnit, IReductionsSink
	{
		#region Properties

		public ScriptModule ScriptModule { get { return (ScriptModule)module; } }
		public ScriptBuilder ScriptBuilder { get { return (ScriptBuilder)module; } }

		private readonly Dictionary<QualifiedName, ScopedDeclaration<DType>>/*!*/ visibleTypes =
			new Dictionary<QualifiedName, ScopedDeclaration<DType>>();
		private readonly Dictionary<QualifiedName, ScopedDeclaration<DRoutine>>/*!*/ visibleFunctions =
			new Dictionary<QualifiedName, ScopedDeclaration<DRoutine>>();
		private readonly Dictionary<QualifiedName, ScopedDeclaration<DConstant>>/*!*/ visibleConstants =
            new Dictionary<QualifiedName, ScopedDeclaration<DConstant>>(ConstantQualifiedNameComparer.Singleton);

		private Scope currentScope = new Scope(0);

		public List<AST.IncludingEx> InclusionExpressions { get { return inclusionExpressions; } }
		private List<AST.IncludingEx> inclusionExpressions = new List<AST.IncludingEx>();

		/// <summary>
		/// Source unit or <B>null</B> for reflected units.
		/// </summary>
		public SourceUnit SourceUnit { get { return sourceUnit; } set { sourceUnit = value; } }
		private SourceUnit sourceUnit;

		#endregion

		#region Construction

		/// <summary>
		/// Used by compiler.
		/// </summary>
		public ScriptCompilationUnit()
		{
			this.state = States.Initial;
			this.sourceUnit = null;       // to be set explicitly
		}

		#endregion

		#region Declaration Look-up

		/// <summary>
		/// Search for a declaration by its qualified name
		/// </summary>
		public override DRoutine GetVisibleFunction(QualifiedName qualifiedName, ref string fullName/*!*/, Scope currentScope)
		{
			ScopedDeclaration<DRoutine> decl;

			if (visibleFunctions.TryGetValue(qualifiedName, out decl) && decl.Scope.Start <= currentScope.Start)
				return decl.Member;

			// search application context:
			return module.Assembly.ApplicationContext.GetFunction(qualifiedName, ref fullName);
		}

		/// <summary>
		/// Search for a declaration by its qualified name
		/// </summary>
		public override DType GetVisibleType(QualifiedName qualifiedName, ref string fullName/*!*/, Scope currentScope,
			bool mustResolve)
		{
			ScopedDeclaration<DType> decl;
			if (visibleTypes.TryGetValue(qualifiedName, out decl) && decl.Scope.Start <= currentScope.Start)
				return decl.Member;

			// search application context:
			return module.Assembly.ApplicationContext.GetType(qualifiedName, ref fullName);
		}

		/// <summary>
		/// Search for a declaration by its qualified name
		/// </summary>
		public override DConstant GetVisibleConstant(QualifiedName qualifiedName, ref string fullName/*!*/, Scope currentScope)
		{
			ScopedDeclaration<DConstant> decl;
			if (visibleConstants.TryGetValue(qualifiedName, out decl) && decl.Scope.Start <= currentScope.Start)
				return decl.Member;

			// search application context:
			return module.Assembly.ApplicationContext.GetConstant(qualifiedName, ref fullName);
		}

		/// <summary>
		/// Used for merging type tables (in ScriptCompilationUnit)
		/// </summary>
		/// <returns>Returns all reflected or declared types</returns>
		public override IEnumerable<KeyValuePair<QualifiedName, ScopedDeclaration<DType>>> GetVisibleTypes()
		{
			return visibleTypes;
		}

		/// <summary>
		/// Used for merging function tables (in ScriptCompilationUnit)
		/// </summary>
		/// <returns>Returns all reflected or declared functions</returns>
		public override IEnumerable<KeyValuePair<QualifiedName, ScopedDeclaration<DRoutine>>> GetVisibleFunctions()
		{
			return visibleFunctions;
		}

		/// <summary>
		/// Used for merging constant tables (in ScriptCompilationUnit)
		/// </summary>
		/// <returns>Returns all reflected or declared constants</returns>
		public override IEnumerable<KeyValuePair<QualifiedName, ScopedDeclaration<DConstant>>> GetVisibleConstants()
		{
			return visibleConstants;
		}


		/// <summary>
		/// Return only declared types that should be emited while compiling
		/// </summary>
		public override IEnumerable<PhpType>/*!*/ GetDeclaredTypes()
		{
			return DeclaredOnly<PhpType, DType>(visibleTypes.Values);
		}

		/// <summary>
		/// Return only declared functions that should be emited while compiling
		/// </summary>
		public override IEnumerable<PhpFunction>/*!*/ GetDeclaredFunctions()
		{
			return DeclaredOnly<PhpFunction, DRoutine>(visibleFunctions.Values);
		}

		/// <summary>
		/// Return only declared constants that should be emited while compiling
		/// </summary>
		public override IEnumerable<GlobalConstant>/*!*/ GetDeclaredConstants()
		{
			return DeclaredOnly<GlobalConstant, DConstant>(visibleConstants.Values);
		}

		internal IEnumerable<Declaration>/*!*/ DeclaredOnly<TCol>(IEnumerable<ScopedDeclaration<TCol>>/*!*/ table)
			where TCol : DMember
		{
			foreach (ScopedDeclaration<TCol> decl in table)
			{
				KnownScopedDeclaration<TCol> known = decl as KnownScopedDeclaration<TCol>;
				if (known == null) continue;

				if (ReferenceEquals(known.Declaration.SourceUnit.CompilationUnit, this))
					yield return known.Declaration;
			}
		}

		internal IEnumerable<T>/*!*/ DeclaredOnly<T, TCol>(IEnumerable<ScopedDeclaration<TCol>>/*!*/ table)
			where T : IDeclaree, TCol
			where TCol : DMember
		{
			foreach (ScopedDeclaration<TCol> decl in table)
			{
				KnownScopedDeclaration<TCol> known = decl as KnownScopedDeclaration<TCol>;
				if (known == null) continue;

				if (ReferenceEquals(known.Declaration.SourceUnit.CompilationUnit, this))
					yield return (T)known.Member;
			}
		}

		#endregion

		#region Parsing

		internal void Parse(CompilationContext/*!*/ context)
		{
			Debug.Assert(state == States.Initial);
			Debug.Assert(context != null);

			context.Manager.Info(sourceUnit.SourceFile, context);

			// prepended and appended inclusions (TODO: do this better, the included compilation units can be prepared)
			AST.IncludingEx prepend = null, append = null;

			// prepend inclusion:
#if !SILVERLIGHT
			if (context.Config.Compiler.PrependFile != null)
			{
				prepend = new AST.IncludingEx(sourceUnit, new Scope(1), false, Position.Initial, InclusionTypes.Prepended,
					new AST.StringLiteral(Position.Initial, context.Config.Compiler.PrependFile));

				inclusionExpressions.Add(prepend);
			}
#endif

			// parse:

			this.module = (PhpModule)context.Manager.DefineModuleBuilder(this, context);

			ParseSourceFile(this.sourceUnit, context.Errors, context.Config.Compiler.LanguageFeatures);

            if (context.Errors.AnyFatalError)
            {
                this.state = States.Erroneous;
                throw new CompilerException();
            }

			// append inclusion:
#if !SILVERLIGHT
			if (context.Config.Compiler.AppendFile != null)
			{
				append = new AST.IncludingEx(sourceUnit, new Scope(Int32.MaxValue), false, Position.Initial, InclusionTypes.Appended,
					new AST.StringLiteral(Position.Initial, context.Config.Compiler.AppendFile));

				inclusionExpressions.Add(append);
			}
#endif

			sourceUnit.Ast.PrependedInclusion = prepend;
			sourceUnit.Ast.AppendedInclusion = append;

			this.state = States.Parsed;
		}

		public bool ParseSourceFile(SourceUnit/*!*/ sourceUnit, ErrorSink/*!*/ errors, LanguageFeatures languageFeatures)
		{
			if (sourceUnit == null)
				throw new ArgumentNullException("sourceUnit");

			if (errors == null)
				throw new ArgumentNullException("errors");

			try
			{
				sourceUnit.Parse(errors, this, Position.Initial, languageFeatures);
			}
			catch (CompilerException)
			{
				return false;
			}

			return true;
		}

		#endregion

		#region Table Population

		/// <summary>
		/// Merges the content of all tables.
		/// Returns the number of added items.
		/// </summary>
		internal int MergeTables(StaticInclusion/*!*/ inclusion)
		{
			// skip conditional inclusions (TODO):
			if (inclusion.IsConditional) return 0;

			// 
			CompilationUnit cu = inclusion.Includee;
			return
				MergeTable<DRoutine>(inclusion, visibleFunctions, cu.GetVisibleFunctions()) +
				MergeTable<DType>(inclusion, visibleTypes, cu.GetVisibleTypes()) +
				MergeTable<DConstant>(inclusion, visibleConstants, cu.GetVisibleConstants());
		}

		/// <summary>
		/// Merges the content of specified tables.
		/// Returns the number of added items.
		/// </summary>
		private static int MergeTable<T>(StaticInclusion/*!*/ inclusion,
			Dictionary<QualifiedName, ScopedDeclaration<T>> dstTable,
			IEnumerable<KeyValuePair<QualifiedName, ScopedDeclaration<T>>> srcTable)
			where T : DMember
		{
			int added_count = 0;

            Dictionary<QualifiedName, ScopedDeclaration<T>> dstTableOverwrites = null;

			foreach (KeyValuePair<QualifiedName, ScopedDeclaration<T>> entry in srcTable)
			{
				ScopedDeclaration<T> existing;

				// treat all inclusions like once-inclusions (TODO):
				if (!dstTable.TryGetValue(entry.Key, out existing))
				{
					// add the declaration to the includer with the scope of the inclusion:
					dstTable.Add(entry.Key, entry.Value.CloneWithScope(inclusion.Scope));
					added_count++;
				}
                else if (existing.Scope.Start > inclusion.Scope.Start && inclusion.Scope.IsValid)  // better Scope level?
                {
                    if (existing.Member == entry.Value.Member ||                    // mostly DMember is the same reference, just in different Scope level
                        !entry.Value.Member.IsUnknown || existing.Member.IsUnknown) // otherwise, we don't want to overwrite a Known member with an Unknown!
                    {
                        // overwrite the existing declaration with better inclusion scope:
                        if (dstTableOverwrites == null)
                            dstTableOverwrites = new Dictionary<QualifiedName, ScopedDeclaration<T>>();
                        dstTableOverwrites[entry.Key] = entry.Value.CloneWithScope(inclusion.Scope);
                    }
                }
			}

            // merge overwrites into dstTable // to avoid changing of enumerated collection above
            if (dstTableOverwrites != null)
                foreach (var entry in dstTableOverwrites)
                    dstTable[entry.Key] = entry.Value;

            //
			return added_count;
		}

		#endregion

		#region Analysis

		internal void PreAnalyzeRecursively(Analyzer/*!*/ analyzer)
		{
			Debug.Assert(state == States.Processed);

			// TODO: declared only
			analyzer.PreAnalyze(DeclaredOnly<DType>(visibleTypes.Values));
			analyzer.PreAnalyze(DeclaredOnly<DRoutine>(visibleFunctions.Values));

			state = States.PreAnalyzed;

			foreach (StaticInclusion inclusion in inclusions)
			{
				ScriptCompilationUnit s = inclusion.Includee as ScriptCompilationUnit;
				if (s == null) continue;
				if (s.State == States.Processed) s.PreAnalyzeRecursively(analyzer);
			}
		}

		internal void AnalyzeMembersRecursively(Analyzer/*!*/ analyzer)
		{
			Debug.Assert(state == States.PreAnalyzed);

			// TODO: declared only
			analyzer.AnalyzeMembers(DeclaredOnly<DType>(visibleTypes.Values));
			analyzer.AnalyzeMembers(DeclaredOnly<DRoutine>(visibleFunctions.Values));

			state = States.MembersAnalyzed;

			foreach (StaticInclusion inclusion in inclusions)
			{
				ScriptCompilationUnit s = inclusion.Includee as ScriptCompilationUnit;
				if (s == null) continue;
				if (s.State == States.PreAnalyzed) s.AnalyzeMembersRecursively(analyzer);
			}
		}

		internal void AnalyzeRecursively(Analyzer/*!*/ analyzer)
		{
			Debug.Assert(state == States.MembersAnalyzed);
			Debug.Assert(sourceUnit != null);

			analyzer.Analyze(sourceUnit);

			// source stream is not needed any more:
			sourceUnit.Close();

			state = States.Analyzed;

			foreach (StaticInclusion inclusion in inclusions)
			{
				ScriptCompilationUnit s = inclusion.Includee as ScriptCompilationUnit;
				if (s == null) continue;
				if (s.State == States.MembersAnalyzed) s.AnalyzeRecursively(analyzer);
			}
		}

		#endregion

		#region Emission

		internal void DefineBuilders(CompilationContext/*!*/ context)
		{
			// TODO: optimize - go thru declared only (list to the ScopedDeclaration?)

			foreach (PhpType type in DeclaredOnly<PhpType, DType>(visibleTypes.Values))
				type.DefineBuilders();

			foreach (PhpFunction function in DeclaredOnly<PhpFunction, DRoutine>(visibleFunctions.Values))
				function.DefineBuilders();

			foreach (GlobalConstant constant in DeclaredOnly<GlobalConstant, DConstant>(visibleConstants.Values))
				constant.DefineBuilders();

			this.State = States.BuildersDefined;
		}

		internal void Emit(CodeGenerator/*!*/ codeGenerator)
		{
			sourceUnit.Emit(codeGenerator);
			this.state = States.Emitted;
		}

		internal void Bake()
		{
			foreach (PhpType type in DeclaredOnly<PhpType, DType>(visibleTypes.Values))
				type.Bake();

			foreach (PhpFunction function in DeclaredOnly<PhpFunction, DRoutine>(visibleFunctions.Values))
				function.Bake();

			this.state = States.Compiled;
		}

		#endregion

		#region Clean-up

		public override void CleanUp(CompilationContext/*!*/ context, bool successful)
		{
			if (state != States.Reflected && state != States.Compiled)
				state = States.Initial;

			// if the unit has been parsed and compiled:
			if (sourceUnit != null)
			{
				// close opened stream:
				sourceUnit.Close();

				// unlock:
				context.Manager.UnlockForCompiling(sourceUnit.SourceFile, successful, context);
			}
		}

		#endregion

		#region IReductionsSink

		public void InclusionReduced(Parser/*!*/ parser, AST.IncludingEx node)
		{
			// just add to list and resolve later (prevents seeking there and back):
			inclusionExpressions.Add(node);
		}

		public void FunctionDeclarationReduced(Parser/*!*/ parser, AST.FunctionDecl/*!*/ node)
		{
			AddDeclaration<DRoutine>(parser.ErrorSink, node.Function, visibleFunctions);
		}

		public void TypeDeclarationReduced(Parser/*!*/ parser, AST.TypeDecl/*!*/ node)
		{
			AddDeclaration<DType>(parser.ErrorSink, node.Type, visibleTypes);
		}

		public void GlobalConstantDeclarationReduced(Parser/*!*/ parser, AST.GlobalConstantDecl/*!*/ node)
		{
			AddDeclaration<DConstant>(parser.ErrorSink, node.GlobalConstant, visibleConstants);
		}

		private void AddDeclaration<T>(ErrorSink/*!*/ errors, IDeclaree/*!*/ member,
			Dictionary<QualifiedName, ScopedDeclaration<T>>/*!*/ table)
			where T : DMember
		{
			KnownScopedDeclaration<T> existing;
			KnownScopedDeclaration<T> current;

			if (member.Declaration.IsConditional)
				current = new KnownScopedDeclaration<T>(member.Declaration.Scope, member.Declaration);
			else
				current = new KnownScopedDeclaration<T>(new Scope(0), member.Declaration);

			ScopedDeclaration<T> unkExisting;
			if (table.TryGetValue(member.QualifiedName, out unkExisting))
			{
				// shouldn't be called after analysis
				Debug.Assert(unkExisting is KnownScopedDeclaration<T>);

				existing = (KnownScopedDeclaration<T>)unkExisting;
				if (CheckDeclaration(errors, member, existing.Declaration))
					AddVersionToGroup(current.Declaration, existing.Declaration);
			}
			else
			{
				if (member.Declaration.IsConditional)
					member.Version = new VersionInfo(1, null);

				// add a new declaration to the table:
				table.Add(member.QualifiedName, current);
			}
		}

		#endregion

		#region Reflection

		public override void Reflect()
		{
			Debug.Assert(state == States.Compiled);

			// TODO: What should we do here?

			state = States.Reflected;
		}

		#endregion

		#region Referenced by compiler

		/// <summary>
		/// Source file path to be used when emiting inclusion
		/// </summary>
		public override string RelativeSourcePath
		{
			get { return SourceUnit.SourceFile.RelativePath.ToString(); }
		}


		/// <summary>
		/// System.Type of the main module class
		/// </summary>
		public override Type ScriptClassType
		{
			get { return ScriptModule.ScriptType; }
		}


		/// <summary>
		/// MethodInfo of the main module 'Main' method
		/// </summary>
		public override MethodInfo MainHelper
		{
			get { return ScriptModule.MainHelper; }
		}

		#endregion

		#region Inclusion Expression Translation

		internal void ResolveInclusions(InclusionGraphBuilder/*!*/ graphBuilder)
		{
			Debug.Assert(state == States.Parsed);

			foreach (AST.IncludingEx inclusion in inclusionExpressions)
			{
				Characteristic characteristic;
				PhpSourceFile target_file;

				ResolveInclusion(inclusion.InclusionType, inclusion, graphBuilder.Context, out characteristic, out target_file);

				if (target_file != null)
				{
					CompilationUnit includee = graphBuilder.GetNode(target_file);
					StaticInclusion static_inclusion = new StaticInclusion(this, includee, inclusion.Scope, inclusion.IsConditional, inclusion.InclusionType);

					// adds an edge:
					this.Inclusions.Add(static_inclusion);
					includee.Includers.Add(static_inclusion);

					inclusion.Inclusion = static_inclusion;
					inclusion.Characteristic = characteristic;

					graphBuilder.EdgeAdded(static_inclusion);
				}
				else
				{
					inclusion.Inclusion = null;
					inclusion.Characteristic = Characteristic.Dynamic;
				}
			}

			inclusionExpressions = null;
		}

		/// <summary>
		/// Determines characteristics and target source path according to the analysis of the inclusion expression.
		/// </summary>
		private void ResolveInclusion(InclusionTypes inclusionType, AST.IncludingEx/*!*/ inclusionExpr,
			CompilationContext/*!*/ context, out Characteristic characteristic, out PhpSourceFile targetFile)
		{
			targetFile = null;
			characteristic = Characteristic.Dynamic;

			// inclusions in dynamic code are dynamic: 
			if (InclusionTypesEnum.IsAutoInclusion(inclusionType))
			{
				Debug.Assert(inclusionExpr.Target.HasValue);

				// auto-inclusions contain explicit path:
				targetFile = DetermineStaticTarget((string)inclusionExpr.Target.Value, inclusionExpr.Target, context);
				if (targetFile != null) characteristic = Characteristic.StaticAutoInclusion;

				return;
			}
			else
			{
				if (!context.SaveOnlyAssembly && (context.Config.Compiler.EnableStaticInclusions ?? false))
				{
					// replacement //

					if (context.Config.Compiler.InclusionMappings.Count > 0)
					{
						// tries to match the pattern:
						string source_code = sourceUnit.GetSourceCode(inclusionExpr.Target.Position);
						string translated_path = InclusionMapping.TranslateExpression(context.Config.Compiler.InclusionMappings,
                            source_code, context.Config.Compiler.SourceRoot.FullFileName);

						// succeeded:
						if (translated_path != null)
						{
							targetFile = DetermineStaticTarget(translated_path, inclusionExpr.Target, context);
							if (targetFile != null) characteristic = Characteristic.StaticArgReplaced;
							return;
						}
						else
						{
							context.Errors.Add(Warnings.InclusionReplacementFailed, SourceUnit, inclusionExpr.Position, source_code);
						}
					}

					// evaluation //

					Evaluation eval = inclusionExpr.Target.EvaluatePriorAnalysis(inclusionExpr.SourceUnit);
					if (eval.HasValue)
					{
						targetFile = DetermineStaticTarget(Convert.ObjectToString(eval.Value), inclusionExpr.Target, context);
						if (targetFile != null) characteristic = Characteristic.StaticArgEvaluated;
						return;
					}
				}
			}
		}

		/// <summary>
		/// Checks whether the file defined by the path which is about to be set exists.
		/// </summary>
		/// <param name="translatedPath">The path to set. Can be either relative or absolute, canonical or not.</param>
		/// <param name="target"></param>
		/// <param name="context">Source unit.</param>
		/// <returns>Target source file a <B>null</B> reference if it cannot be statically determined.</returns>
		private PhpSourceFile DetermineStaticTarget(string translatedPath, AST.Expression target, CompilationContext/*!*/ context)
		{
            // searches for file in the following order: 
			// - incomplete absolute path => combines with RootOf(SourceRoot)
			// - relative path => searches in SourceRoot then in the script source directory
			string warning;

            // create file existance checking predicate,
            // while searching for the script existance, check the script library and file system:
            Predicate<FullPath> fileExists = null;
            if (context.ApplicationContext.ScriptLibraryDatabase != null && context.ApplicationContext.ScriptLibraryDatabase.Count > 0)
                fileExists = fileExists.OrElse(path => context.ApplicationContext.ScriptLibraryDatabase.ContainsScript(path));
            fileExists = fileExists.OrElse(path => path.FileExists);

            // try to find the inclusion target:
			FullPath full_path = PhpScript.FindInclusionTargetPath(
                new InclusionResolutionContext(
                    context.ApplicationContext,
				    sourceUnit.SourceFile.Directory,
				    context.Config.Compiler.SourceRoot,
				    context.Config.Compiler.StaticIncludePaths
                    ),
				translatedPath,
                fileExists,
                out warning);

			Debug.Assert(full_path.IsEmpty == (warning != null));   // full_path can be empty iff warning string was set

            if (full_path.IsEmpty)
			{
				context.Errors.Add(Warnings.InclusionDeferredToRuntime, SourceUnit, target.Position,
					translatedPath, warning);
				return null;
			}
            else
            {
                // list of files/directories to be skipped within static inclusion
                // only existing files/directories are included
                
                // if file is in ignore list, we will defer inclusion to runtime but not report warning
                foreach (var path in context.Config.Compiler.ForcedDynamicInclusionTranslatedFullPaths)
                    if (full_path.FullFileName.StartsWith(path))
                        return null;
            }

			return new PhpSourceFile(context.Config.Compiler.SourceRoot, full_path);
		}

		#endregion

	}

	#endregion
}
