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

namespace PHP.Core.Reflection
{
	#region CompilationUnitBase

	public abstract class CompilationUnitBase
	{
        /// <summary>Identifies vendor Micorosft (used by debugger)</summary>
        public const string MicrosoftVendorGuid = "994B45C4-E6E9-11D2-903F-00C04FA302A1";
        /// <summary>Identifies Phalanger language (used by debugger)</summary>
        public const string PhalangerLanguageGuid = "47414A73-A544-4f5c-8684-C461D16FF58A";
		/// <summary>
		/// Module or module builder associated with the compilation unit (one-to-one).
		/// Filled by reflection or emission.
		/// </summary>
		public IPhpModuleBuilder ModuleBuilder { get { return (IPhpModuleBuilder)module; } }
		public PhpModule Module { get { return module; } }
		protected PhpModule module;

		/// <summary>
		/// Whether the unit represents an eval et. al.
		/// </summary>
		public abstract bool IsTransient { get; }
		public abstract bool IsPure { get; }

		public virtual int TransientId { get { return TransientAssembly.InvalidEvalId; } }

		/// <summary>
		/// Symbol document writers for source files used in the compilation unit.
		/// Maps a source file name (needn't to be a PHP source file nor even a valid file name!) to the symbol document.
		/// </summary>
		private Dictionary<string, ISymbolDocumentWriter> symbolDocumentWriters; // lazy

		#region Construction

		protected CompilationUnitBase()
		{
			// nop
		}

		protected CompilationUnitBase(PhpModule/*!*/ module)
		{
			this.module = module;
		}

		#endregion

		public abstract DType GetVisibleType(QualifiedName qualifiedName, ref string/*!*/ fullName, Scope currentScope,
			bool mustResolve);
		public abstract DRoutine GetVisibleFunction(QualifiedName qualifiedName, ref string/*!*/ fullName, Scope currentScope);
		public abstract DConstant GetVisibleConstant(QualifiedName qualifiedName, ref string/*!*/ fullName, Scope currentScope);

		public abstract IEnumerable<PhpType>/*!*/ GetDeclaredTypes();
		public abstract IEnumerable<PhpFunction>/*!*/ GetDeclaredFunctions();
		public abstract IEnumerable<GlobalConstant>/*!*/ GetDeclaredConstants();

		#region Declarations

		protected void AddVersionToGroup(Declaration/*!*/ current, Declaration/*!*/ existing)
		{
			if (existing.Group == null)
				existing.Group = new DeclarationGroup(existing.IsConditional, existing.IsPartial);

			current.Group = existing.Group;

			// add the new version to the list as a second item:
			int primary_index = (existing.Declaree.Version.Index > 0) ? existing.Declaree.Version.Index + 1 : 2;
			current.Declaree.Version = new VersionInfo(primary_index - 1, existing.Declaree.Version.Next);
			existing.Declaree.Version = new VersionInfo(primary_index, current.Declaree);

			existing.Group.AddDeclaration(current.IsConditional, current.IsPartial);
		}

		protected bool CheckDeclaration(ErrorSink/*!*/ errors, IDeclaree/*!*/ member, Declaration/*!*/ existing)
		{
			Declaration current = member.Declaration;

			if (existing.IsPartial ^ current.IsPartial)
			{
				TryFixPartial(errors, current, existing);
				TryFixPartial(errors, existing, current);
			}

			if ((!existing.IsPartial || !current.IsPartial) && (!existing.IsConditional || !current.IsConditional))
			{
				// report fatal error (do not throw an exception, just don't let the analysis continue):
				member.ReportRedeclaration(errors);
				errors.Add(FatalErrors.RelatedLocation, existing.SourceUnit, existing.Position);
				return false;
			}

			return true;
		}

		private void TryFixPartial(ErrorSink/*!*/ errors, Declaration/*!*/ first, Declaration/*!*/ second)
		{
			if (!first.IsPartial && !first.IsConditional)
			{
				// report error and mark the declaration partial:
				errors.Add(Errors.MissingPartialModifier, first.SourceUnit, first.Position, first.Declaree.FullName);
				errors.Add(Errors.RelatedLocation, second.SourceUnit, second.Position);

				first.IsPartial = true;
			}
		}

		#endregion

		#region Symbol Documents

		internal ISymbolDocumentWriter GetSymbolDocumentWriter(string/*!*/ fullPath)
		{
			ModuleBuilder module_builder = ModuleBuilder.AssemblyBuilder.RealModuleBuilder;
			ISymbolDocumentWriter result = null;


#if !SILVERLIGHT
			if (module_builder.GetSymWriter() != null)
			{
				if (symbolDocumentWriters == null || !symbolDocumentWriters.TryGetValue(fullPath, out result))
				{
					if (symbolDocumentWriters == null)
						symbolDocumentWriters = new Dictionary<string, ISymbolDocumentWriter>();

					result = module_builder.DefineDocument(fullPath, new Guid(PhalangerLanguageGuid) , new Guid(MicrosoftVendorGuid), Guid.Empty);

                    symbolDocumentWriters.Add(fullPath, result);
				}
			}
#endif

			return result;
		}

		#endregion
	}

	#endregion

	#region TransientCompilationUnit

	public sealed class TransientCompilationUnit : CompilationUnitBase, IReductionsSink
	{
		public override bool IsPure { get { return false; } }
		public override bool IsTransient { get { return true; } }

		public override int TransientId { get { return TransientModule.Id; } }

		public TransientModule TransientModule { get { return (TransientModule)module; } }

		public SourceCodeUnit/*!*/ SourceUnit { get { return sourceUnit; } }
		private readonly SourceCodeUnit/*!*/ sourceUnit;

		/// <summary>
		/// Stores types, functions and constants during compilation.
		/// Dropped when the module is being baked to free the emission resources.
		/// Only descriptors of the baked elements are accessible (via <c>bakedXxx</c> lists).
		/// </summary>
		private Dictionary<QualifiedName, Declaration> types = null;
		private Dictionary<QualifiedName, Declaration> functions = null;
		private Dictionary<QualifiedName, Declaration> constants = null;

		/// <summary>
		/// Baked type, function and constant descriptors and their full names. 
		/// Available after the compilation. Used for activations each time 
		/// the transient unit is executed (via eval).
		/// Contains only unconditionally declared entities.
		/// </summary>
		private KeyValuePair<string, PhpTypeDesc>[] bakedTypes = null;
		private KeyValuePair<string, PhpRoutineDesc>[] bakedFunctions = null;
		private KeyValuePair<string, DConstantDesc>[] bakedConstants = null;
		
		public EvalKinds EvalKind { get { return evalKind; } set { evalKind = value; } }
		private EvalKinds evalKind;

		private ScriptContext/*!*/ resolvingScriptContext;
		private DTypeDesc referringType;

		/// <summary>
		/// Whether the functions and main code of the transient module is emitted to DynamicMethods.
		/// Note that DMs don't support references to *Builders so if there are any declarations in the code,
		/// the unit cannot be dynamic.
		/// </summary>
		public bool IsDynamic { get { return isDynamic; } }
		private bool isDynamic = false;

#if SILVERLIGHT
		public TransientCompilationUnit(string/*!*/ sourceCode, PhpSourceFile/*!*/ sourceFile, Encoding/*!*/ encoding, NamingContext namingContext, int line, int column, bool client)
		{
			Debug.Assert(sourceCode != null && sourceFile != null && encoding != null);
			if (client)
				this.sourceUnit = new ClientSourceCodeUnit(this, sourceCode, sourceFile, encoding, line, column);
			else
				this.sourceUnit = new SourceCodeUnit(this, sourceCode, sourceFile, encoding, line, column);
			this.sourceUnit.AddImportedNamespaces(namingContext);
		}
#else
		public TransientCompilationUnit(string/*!*/ sourceCode, PhpSourceFile/*!*/ sourceFile, Encoding/*!*/ encoding, NamingContext namingContext, int line, int column, bool client)
		{
			Debug.Assert(sourceCode != null && sourceFile != null && encoding != null);
			Debug.Assert(!client);
			this.sourceUnit = new SourceCodeUnit(this, sourceCode, sourceFile, encoding, line, column);
			this.sourceUnit.AddImportedNamespaces(namingContext);
		}
#endif

		#region Declaration look-up

		public override DRoutine GetVisibleFunction(QualifiedName qualifiedName, ref string fullName/*!*/, Scope currentScope)
		{
			// try unconditional types declared within the same eval (doesn't make a dependency):
			Declaration result;
			if (functions != null && functions.TryGetValue(qualifiedName, out result))
				return (PhpFunction)result.Declaree;

			// try functions declared on AC (doesn't make dependency):
			return module.Assembly.ApplicationContext.GetFunction(qualifiedName, ref fullName);
		}

		public override DType GetVisibleType(QualifiedName qualifiedName, ref string fullName/*!*/, Scope currentScope,
			bool mustResolve)
		{
			// try unconditional types declared within the same eval (doesn't make a dependency):
			Declaration result;
			if (types != null && types.TryGetValue(qualifiedName, out result))
				return (PhpType)result.Declaree;

			// search application context (doens't make a dependency):
			DType type = module.Assembly.ApplicationContext.GetType(qualifiedName, ref fullName);
			if (type != null)
				return type;

			// do not add a dependency if not necessary:
			if (!mustResolve) return null;

			// try types declared on SC (makes a dependency);
			// use referring type to allow resolving self and parent; autoload is available here;
            DTypeDesc desc = resolvingScriptContext.ResolveType(fullName, null, referringType, null, ResolveTypeFlags.UseAutoload | ResolveTypeFlags.SkipGenericNameParsing);

			if (desc != null)
			{
				// TODO: remember the dependency
				return desc.Type;
			}

			return null;
		}

		public override DConstant GetVisibleConstant(QualifiedName qualifiedName, ref string fullName/*!*/, Scope currentScope)
		{
			// try unconditional constants declared within the same eval (doesn't make a dependency):
			Declaration result;
			if (constants != null && constants.TryGetValue(qualifiedName, out result))
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

		#region Compilation

		TransientAssemblyBuilder assembly_builder;
		TransientModuleBuilder module_builder;

		/// <summary>
		/// Called before 'Compile' to initialize module &amp; assembly builders, so they can be used by the caller.
		/// </summary>
		internal bool PreCompile(CompilationContext/*!*/ context, ScriptContext/*!*/ scriptContext,
			SourceCodeDescriptor descriptor, EvalKinds kind, DTypeDesc referringType)
		{
			this.resolvingScriptContext = scriptContext;
			this.referringType = referringType;

			// TODO: isDynamic is tricky...
			//  .. we need to define module_builder before any type/etc.. is reduced from the parser
			//  .. but we don't know whether it will be dynamic in advance!

			this.assembly_builder = scriptContext.ApplicationContext.TransientAssemblyBuilder;
			this.module_builder = assembly_builder.DefineModule(this, context.Config.Compiler.Debug,
				descriptor.ContainingTransientModuleId, kind, descriptor.ContainingSourcePath);
			this.module = module_builder;
			this.evalKind = kind;

            sourceUnit.Parse(
                context.Errors, this,
                new Position(descriptor.Line, descriptor.Column, 0, descriptor.Line, descriptor.Column, 0),
                context.Config.Compiler.LanguageFeatures);

			if (context.Errors.AnyFatalError) return false;

			// any declaration implies non-dynamicity:
			// TODO: this mode needs to be checked...
			// isDynamic = types == null && functions == null && constants == null;

			return true;
		}


		/// <summary>
		/// Compiles the transient unit. 'PreCompile' should be called first!
		/// </summary>
		internal bool Compile(CompilationContext/*!*/ context, EvalKinds kind)
		{
			Analyzer analyzer = null;

			try
			{
				analyzer = new Analyzer(context);

				// perform pre-analysis on types and functions:
				if (types != null)
					analyzer.PreAnalyze(types.Values);

				if (functions != null)
					analyzer.PreAnalyze(functions.Values);

				// perform member analysis on types and functions:
				if (types != null)
					analyzer.AnalyzeMembers(types.Values);
				if (functions != null)
					analyzer.AnalyzeMembers(functions.Values);
				
				if (context.Errors.AnyFatalError) return false;

				// perform full analysis:
				analyzer.Analyze(sourceUnit);

				if (context.Errors.AnyFatalError) return false;

				// perform post analysis:
				analyzer.PostAnalyze();
			}
			catch (CompilerException)
			{
				return false;
			}
			finally
			{
				resolvingScriptContext = null;
				referringType = null;
			}

			// do not emit anything if there was parse/analysis error:
			if (context.Errors.AnyError) return false;

			DefineBuilders();

			// define constructed types:
			analyzer.DefineConstructedTypeBuilders();

			CodeGenerator cg = new CodeGenerator(context);
			sourceUnit.Emit(cg);
			return true;
		}

        internal void PostCompile(SourceCodeDescriptor descriptor)
        {
            module_builder.Bake();
            Bake();

            // TODO: analyzer.GetTypeDependencies();
            var dependentTypes = new List<KeyValuePair<string, DTypeDesc>>(bakedTypes != null ? bakedTypes.Length : 0);
            if (bakedTypes != null)
                foreach (var type in bakedTypes)
                {
                    // add base as this type dependency:
                    AddDependentType(type.Value, dependentTypes, type.Value.Base);

                    // do the same for type.Value.Interfaces
                    var ifaces = type.Value.Interfaces;
                    if (ifaces != null && ifaces.Length > 0)
                        for (int i = 0; i < ifaces.Length; i++)
                            AddDependentType(type.Value, dependentTypes, ifaces[i]);
                }

            //
            module = assembly_builder.TransientAssembly.AddModule(module_builder, dependentTypes, sourceUnit.Code, descriptor);
        }

        private static void AddDependentType(PhpTypeDesc/*!*/selfType, List<KeyValuePair<string, DTypeDesc>>/*!*/dependentTypes, DTypeDesc dependentType)
        {
            if (dependentType != null && dependentType is PhpTypeDesc && !IsSameCompilationUnit(selfType, dependentType))
                dependentTypes.Add(new KeyValuePair<string, DTypeDesc>(dependentType.MakeFullName(), dependentType));
        }

        private static bool IsSameCompilationUnit(PhpTypeDesc/*!*/selfType, DTypeDesc dependentType)
        {
            Debug.Assert(selfType != null && dependentType != null);

            if (object.ReferenceEquals(dependentType.RealType.Module, selfType.RealType.Module))
            {
                int selfTransientId, dependentTransientId;
                string selfFileName, dependentFileName;
                string selfTypeName, dependentTypeName;

                ReflectionUtils.ParseTypeId(selfType.RealType, out selfTransientId, out selfFileName, out selfTypeName);
                if (selfTransientId != PHP.Core.Reflection.TransientAssembly.InvalidEvalId) // always true, => TransientCompilationUnit
                {
                    ReflectionUtils.ParseTypeId(dependentType.RealType, out dependentTransientId, out dependentFileName, out dependentTypeName);
                    // transient modules, must have same ids
                    return selfTransientId == dependentTransientId;
                }
                else
                {
                    // same module, not transient modules
                    return true;
                }
            }
            else
            {
                // different modules => different units for sure
                return false;
            }
        }

		private void DefineBuilders()
		{
			if (types != null)
			{
				foreach (Declaration declaration in types.Values)
				{
					((PhpType)declaration.Declaree).DefineBuilders();
				}
			}

			if (functions != null)
			{
				foreach (Declaration declaration in functions.Values)
				{
					((PhpFunction)declaration.Declaree).DefineBuilders();
				}
			}

			// TODO (constants that are not evaluable needs to be converted to DM):
			if (constants != null)
			{
				foreach (Declaration declaration in constants.Values)
				{
					((GlobalConstant)declaration.Declaree).DefineBuilders();
				}
			}
		}

		private void Bake()
		{
			if (types != null)
			{
				bakedTypes = new KeyValuePair<string, PhpTypeDesc>[types.Count];
			
				int i = 0;
				foreach (Declaration declaration in types.Values)
				{
					PhpType type = (PhpType)declaration.Declaree;

					// store full name before calling Bake() as it nulls the PhpType:
					string full_name = type.FullName;
					PhpTypeDesc baked = type.Bake();

					// baked is null if the type is indefinite 
					// (its base class may be evaluated when the module's main method is executed):
					if (baked != null && !declaration.IsConditional)
						bakedTypes[i++] = new KeyValuePair<string, PhpTypeDesc>(full_name, baked);
				}
				
				// trim:
				Array.Resize(ref bakedTypes, i);
				
				types = null;
			}

			if (functions != null)
			{
				bakedFunctions = new KeyValuePair<string, PhpRoutineDesc>[functions.Count];

				int i = 0;
				foreach (Declaration declaration in functions.Values)
				{
					PhpFunction function = (PhpFunction)declaration.Declaree;

					string full_name = function.FullName;
					PhpRoutineDesc baked = function.Bake();

					if (!declaration.IsConditional)
						bakedFunctions[i++] = new KeyValuePair<string, PhpRoutineDesc>(full_name, baked);
				}

				// trim:
				Array.Resize(ref bakedFunctions, i);
				
				functions = null;
			}

			if (constants != null)
			{
				bakedConstants = new KeyValuePair<string, DConstantDesc>[constants.Count];

				int i = 0;
				foreach (Declaration declaration in constants.Values)
				{
					GlobalConstant constant = (GlobalConstant)declaration.Declaree;

					string full_name = constant.FullName;
					DConstantDesc baked = constant.Bake();

					if (!declaration.IsConditional)
						bakedConstants[i++] = new KeyValuePair<string, DConstantDesc>(full_name, baked);
				}

				// trim:
				Array.Resize(ref bakedConstants, i);
				
				constants = null;
			}
		}

        /// <summary>
		/// Declares types unconditionally declared in this module on the given <see cref="ScriptContext"/>.
		/// Although, we can emit the Declare helper, it is not necessary as we can do it here for types 
		/// and functions. Only constants, which cannot be evaluated at compile time (they are dependent 
		/// on other eval-time evaluated constants are emitted (TODO).
		/// </summary>
        public void Declare(ScriptContext/*!*/ context)
		{
			if (bakedTypes != null)
			{
				foreach (KeyValuePair<string, PhpTypeDesc> entry in bakedTypes)
				{
					//// checks for conflict on AC:
					//if (module.Assembly.ApplicationContext.Types.ContainsKey(entry.Key))
					//  PhpException.Throw(PhpError.Error, CoreResources.GetString("type_redeclared", entry.Key));

					// checks for conflict on SC:
					if (entry.Value.IsGeneric)
						context.DeclareGenericType(entry.Value, entry.Key);
					else
						context.DeclareType(entry.Value, entry.Key);

                    // moved to TypesProvider.FindAndProvideType
                    //
                    //// When class is compiled in runtime, autoload is invoked on base class (if isn't already declared). 
                    //// We have to call autoload on the base class also in transient assembly
                    //if (entry.Value.Base is PhpTypeDesc)
                    //{
                    //    var baseDesc = context.ResolveType(entry.Value.Base.MakeSimpleName(), null, caller, null, ResolveTypeFlags.UseAutoload);
                    //    // if (baseDesc != entry.Value.Base) we have to invalidate the cache
                    //}
				}
			}
			
			if (bakedFunctions != null)
			{
				foreach (KeyValuePair<string, PhpRoutineDesc> entry in bakedFunctions)
				{
					//// checks for conflict on AC:
					//if (module.Assembly.ApplicationContext.Functions.ContainsKey(entry.Key))
					//  PhpException.Throw(PhpError.Error, CoreResources.GetString("type_redeclared", entry.Value));

					// checks for conflict on SC:
					context.DeclareFunction(new PhpRoutineDesc(entry.Value.MemberAttributes, entry.Value.ArglessStub, false), entry.Key);
				}
			}

            if (bakedConstants != null)
            {
                foreach (var entry in this.bakedConstants)
                {
                    // checks for conflict on SC:
                    //if (constant.HasValue)
                    context.DeclareConstant(entry.Key, entry.Value.LiteralValue);
                }	
            }
		}

		#endregion

		#region IReductionsSink Members

		public void InclusionReduced(Parser/*!*/ parser, AST.IncludingEx/*!*/ node)
		{
			// make all inclusions dynamic:
#if !SILVERLIGHT
			node.Inclusion = null;
			node.Characteristic = Characteristic.Dynamic;
#endif
		}

		public void FunctionDeclarationReduced(Parser/*!*/ parser, AST.FunctionDecl/*!*/ node)
		{
			if (functions == null) functions = new Dictionary<QualifiedName, Declaration>();
			AddDeclaration(parser.ErrorSink, node.Function, functions);
		}

		public void TypeDeclarationReduced(Parser/*!*/ parser, AST.TypeDecl/*!*/ node)
		{
			if (types == null) types = new Dictionary<QualifiedName, Declaration>();
			AddDeclaration(parser.ErrorSink, node.Type, types);
		}

		public void GlobalConstantDeclarationReduced(Parser/*!*/ parser, AST.GlobalConstantDecl/*!*/ node)
		{
			if (constants == null) constants = new Dictionary<QualifiedName, Declaration>();
			AddDeclaration(parser.ErrorSink, node.GlobalConstant, constants);
		}

		private void AddDeclaration(ErrorSink/*!*/ errors, IDeclaree/*!*/ member, Dictionary<QualifiedName, Declaration>/*!*/ table)
		{
			Declaration existing;
			Declaration current = member.Declaration;

			if (table.TryGetValue(member.QualifiedName, out existing))
			{
				// partial declarations are not allowed in transient code => nothing to check;
				if (CheckDeclaration(errors, member, existing))
					AddVersionToGroup(current, existing);
			}
			else
			{
				// add a new declaration to the table:
				table.Add(member.QualifiedName, current);
			}
		}

		#endregion
	}

	#endregion
}
