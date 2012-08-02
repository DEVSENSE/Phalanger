/*

 Copyright (c) 2004-2006 Tomas Matousek and Vaclav Novak.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.SymbolStore;

using PHP.Core;
using PHP.Core.Parsers;
using PHP.Core.Emit;
using PHP.Core.Reflection;

namespace PHP.Core.AST
{
	#region GlobalCode

	/// <summary>
	/// Represents a container for global statements.
	/// </summary>
	/// <remarks>
	/// PHP source file can contain global code definition which is represented in AST 
	/// by GlobalCode node. Finally, it is emitted into Main() method of concrete PHPPage 
	/// class. The sample code below illustrates a part of PHP global code
	/// </remarks>
	public sealed class GlobalCode : AstNode, IHasPhpDoc
	{
		/// <summary>
		/// Array of nodes representing statements in PHP global code
		/// </summary>
		public List<Statement>/*!*/ Statements { get { return statements; } }
		private readonly List<Statement>/*!*/ statements;

		/// <summary>
		/// Global variables. Not available in pure mode, non-null otherwise.
		/// </summary>
		internal VariablesTable/*!*/ VarTable { get { return varTable; } }
		private readonly VariablesTable/*!*/ varTable;

		/// <summary>
		/// Labels (PHP6 feature).
		/// </summary>
		internal Dictionary<VariableName, Statement> Labels { get { return labels; } }
		private Dictionary<VariableName, Statement> labels;

		/// <summary>
		/// Represented source unit.
		/// </summary>
		public SourceUnit/*!*/ SourceUnit { get { return sourceUnit; } }
		private readonly SourceUnit/*!*/ sourceUnit;

		internal IncludingEx PrependedInclusion { get { return prependedInclusion; } set { prependedInclusion = value; } }
		private IncludingEx prependedInclusion;

		internal IncludingEx AppendedInclusion { get { return appendedInclusion; } set { appendedInclusion = value; } }
		private IncludingEx appendedInclusion;

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the GlobalCode class.
		/// </summary>
		public GlobalCode(List<Statement>/*!*/ statements, SourceUnit/*!*/ sourceUnit)
		{
			Debug.Assert(statements != null && sourceUnit != null);

			this.sourceUnit = sourceUnit;
			this.statements = statements;
			this.prependedInclusion = null;
			this.AppendedInclusion = null;

			if (!sourceUnit.CompilationUnit.IsPure)
			{
				varTable = new VariablesTable(20);
				varTable.SetAllRef();
				labels = new Dictionary<VariableName, Statement>();
			}
		}

		#endregion

		#region Analysis

		internal void Analyze(Analyzer/*!*/ analyzer)
		{
			analyzer.LeaveUnreachableCode();
			
			ExInfoFromParent info = new ExInfoFromParent(this);

			// analyze auto-prepended inclusion (no code reachability checks):
			if (prependedInclusion != null)
			{
				info.Access = AccessType.None;
				prependedInclusion.Analyze(analyzer, info);
			}

			for (int i = 0; i < statements.Count; i++)
			{
				if (analyzer.IsThisCodeUnreachable() && statements[i].IsDeclaration)
				{
					//unreachable declarations in global code are valid
					analyzer.LeaveUnreachableCode();
					statements[i] = statements[i].Analyze(analyzer);
					analyzer.EnterUnreachableCode();
				}
				else
				{
					statements[i] = statements[i].Analyze(analyzer);
				}
			}

			if (!sourceUnit.CompilationUnit.IsPure)
				Analyzer.ValidateLabels(analyzer.ErrorSink, sourceUnit, labels);

			// analyze auto-prepended inclusion (no code reachability checks):
			if (appendedInclusion != null)
			{
				info.Access = AccessType.Read;
				appendedInclusion.Analyze(analyzer, info);
			}

			analyzer.LeaveUnreachableCode();
		}

		#endregion

		#region Emission

		/// <include file='Doc/Nodes.xml' path='doc/method[@name="Emit"]/*'/>
		internal void Emit(CodeGenerator/*!*/ codeGenerator)
		{
			// TODO: improve
			codeGenerator.EnterGlobalCodeDeclaration(this.varTable, labels, sourceUnit);

            // custom body prolog emittion:
            PluginHandler.EmitBeforeBody(codeGenerator.IL, statements);

            //
			if (codeGenerator.CompilationUnit.IsTransient)
			{
				codeGenerator.DefineLabels(labels);

				codeGenerator.ChainBuilder.Create();

				foreach (Statement statement in statements)
					statement.Emit(codeGenerator);

				codeGenerator.ChainBuilder.End();

				// return + appended file emission:
				codeGenerator.EmitRoutineEpilogue(this, true);
			}
#if !SILVERLIGHT
			else if (codeGenerator.CompilationUnit.IsPure)
			{
				codeGenerator.ChainBuilder.Create();

				foreach (Statement statement in statements)
				{
					// skip empty statements in global code (they emit sequence points, which is undesirable):
					if (!(statement is EmptyStmt))
						statement.Emit(codeGenerator);
				}

				codeGenerator.ChainBuilder.End();
			}
			else
			{
				ScriptCompilationUnit unit = (ScriptCompilationUnit)codeGenerator.CompilationUnit;

				ILEmitter il = codeGenerator.IL;

                if (codeGenerator.Context.Config.Compiler.Debug)
                {
                    codeGenerator.MarkSequencePoint(1, 1, 1, 2);
                    il.Emit(OpCodes.Nop);
                }

				codeGenerator.DefineLabels(labels);

				// CALL <self>.<Declare>(context); 
				codeGenerator.EmitLoadScriptContext();
				il.Emit(OpCodes.Call, unit.ScriptBuilder.DeclareHelperBuilder);

				// IF (<is main script>) CALL <prepended script>.Main()
				if (prependedInclusion != null)
					prependedInclusion.Emit(codeGenerator);

				codeGenerator.ChainBuilder.Create();

				foreach (Statement statement in statements)
					statement.Emit(codeGenerator);

				codeGenerator.ChainBuilder.End();

				// return + appended file emission:
				codeGenerator.EmitRoutineEpilogue(this, false);
			}
#endif
			codeGenerator.LeaveGlobalCodeDeclaration();
		}

		#endregion

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitGlobalCode(this);
        }

        /// <summary>
        /// <see cref="PHPDocBlock"/> instance or <c>null</c> reference.
        /// </summary>
        public PHPDocBlock PHPDoc { get; set; }
	}

	#endregion

	#region NamespaceDecl

	public sealed class NamespaceDecl : Statement, IHasPhpDoc
	{
		internal override bool IsDeclaration { get { return true; } }

        /// <summary>
        /// Whether the namespace was declared using PHP simple syntax.
        /// </summary>
        public readonly bool IsSimpleSyntax;

		public QualifiedName QualifiedName { get { return qualifiedName; } }
		private QualifiedName qualifiedName;

        /// <summary>
        /// Dictionary of PHP aliases.
        /// </summary>
        public Dictionary<string, QualifiedName>/*!*/ Aliases { get { return aliases; } }
        private readonly Dictionary<string, QualifiedName>/*!*/ aliases = new Dictionary<string, QualifiedName>(StringComparer.OrdinalIgnoreCase);
        
		public bool IsAnonymous { get { return isAnonymous; } }
		private readonly bool isAnonymous;

		public List<Statement>/*!*/ Statements
		{
			get { return statements; }
			internal /* friend Parser */ set { statements = value; }
		}
		private List<Statement>/*!*/ statements;

		#region Construction

        public NamespaceDecl(Position p)
            : base(p)
        {
            this.isAnonymous = true;
            this.qualifiedName = new QualifiedName(Core.Name.EmptyBaseName, Core.Name.EmptyNames);
            this.IsSimpleSyntax = false;
        }

		public NamespaceDecl(Position p, List<string>/*!*/ names, bool simpleSyntax)
			: base(p)
		{
			this.isAnonymous = false;
			this.qualifiedName = new QualifiedName(names, false, true);
            this.IsSimpleSyntax = simpleSyntax;
		}

        /// <summary>
        /// Finish parsing of namespace, complete its position.
        /// </summary>
        /// <param name="p"></param>
        public void UpdatePosition(Position p)
        {
            this.position = p;
        }

		#endregion

		internal override Statement/*!*/ Analyze(Analyzer/*!*/ analyzer)
		{
			analyzer.EnterNamespace(this);

            this.Statements.Analyze(analyzer);
			
			analyzer.LeaveNamespace();

			return this;
		}

		internal override void Emit(CodeGenerator/*!*/ codeGenerator)
		{
			foreach (Statement statement in statements)
			{
                if (!(statement is EmptyStmt))
				    statement.Emit(codeGenerator);
			}
		}

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitNamespaceDecl(this);
        }

        /// <summary>
        /// <see cref="PHPDocBlock"/> instance or <c>null</c> reference.
        /// </summary>
        public PHPDocBlock PHPDoc { get; set; }
	}

	#endregion

	#region GlobalConstDeclList, GlobalConstantDecl

	public sealed class GlobalConstDeclList : Statement, IPhpCustomAttributeProvider, IHasPhpDoc
	{
		private CustomAttributes attributes;

		private readonly List<GlobalConstantDecl>/*!*/ constants;
        public List<GlobalConstantDecl>/*!*/ Constants { get { return constants; } }

		public GlobalConstDeclList(Position position, List<GlobalConstantDecl>/*!*/ constants, List<CustomAttribute> attributes)
			: base(position)
		{
			Debug.Assert(constants != null);

			this.constants = constants;
			this.attributes = new CustomAttributes(attributes);
		}

		internal override Statement/*!*/ Analyze(Analyzer/*!*/ analyzer)
		{
			attributes.AnalyzeMembers(analyzer, analyzer.CurrentScope);
			attributes.Analyze(analyzer, this);

			bool is_unreachable = analyzer.IsThisCodeUnreachable();

			foreach (GlobalConstantDecl cd in constants)
			{
				cd.GlobalConstant.Declaration.IsUnreachable = is_unreachable;
				// cd.Constant.CustomAttributes = attributes;
				cd.Analyze(analyzer);
			}

			if (is_unreachable)
			{
				analyzer.ReportUnreachableCode(position);
				return EmptyStmt.Unreachable;
			}
			else
			{
				return this;
			}
		}

		internal override void Emit(CodeGenerator/*!*/ codeGenerator)
		{
			// TODO: initialization
		}

		#region IPhpCustomAttributeProvider Members

		public PhpAttributeTargets AttributeTarget { get { return PhpAttributeTargets.Constant; } }
		public AttributeTargets AcceptsTargets { get { return AttributeTargets.Field; } }

		public int GetAttributeUsageCount(DType/*!*/ type, CustomAttribute.TargetSelectors selector)
		{
			return attributes.Count(type, selector);
		}

		public void ApplyCustomAttribute(SpecialAttributes kind, Attribute attribute, CustomAttribute.TargetSelectors selector)
		{
			foreach (GlobalConstantDecl cd in constants)
				cd.ApplyCustomAttribute(kind, attribute, selector);
		}

		public void EmitCustomAttribute(CustomAttributeBuilder/*!*/ builder, CustomAttribute.TargetSelectors selector)
		{
			foreach (GlobalConstantDecl cd in constants)
				cd.EmitCustomAttribute(builder);
		}

		#endregion

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitGlobalConstDeclList(this);
        }

        /// <summary>
        /// <see cref="PHPDocBlock"/> instance or <c>null</c> reference.
        /// </summary>
        public PHPDocBlock PHPDoc { get; set; }
	}

	public sealed class GlobalConstantDecl : ConstantDecl
	{
		private NamespaceDecl/*!*/ ns;

		public override KnownConstant Constant { get { return constant; } }
		internal GlobalConstant GlobalConstant { get { return constant; } }
		private readonly GlobalConstant/*!*/ constant;

		public GlobalConstantDecl(SourceUnit/*!*/ sourceUnit, Position position, bool isConditional, Scope scope,
			string/*!*/ name, NamespaceDecl ns, Expression/*!*/ initializer)
			: base(position, name, initializer)
		{
			this.ns = ns;

			QualifiedName qn = (ns != null) ? new QualifiedName(new Name(name), ns.QualifiedName) : new QualifiedName(new Name(name));
			constant = new GlobalConstant(qn, PhpMemberAttributes.Public, sourceUnit, isConditional, scope, position);

			constant.SetNode(this);
		}

		internal void ApplyCustomAttribute(SpecialAttributes kind, Attribute attribute, CustomAttribute.TargetSelectors selector)
		{
			switch (kind)
			{
				case SpecialAttributes.Export:
					constant.ExportInfo = (ExportAttribute)attribute;
					break;

				default:
					Debug.Fail("N/A");
					throw null;
			}
		}

		internal void EmitCustomAttribute(CustomAttributeBuilder/*!*/ builder)
		{
			constant.RealFieldBuilder.SetCustomAttribute(builder);
		}

        internal override void Analyze(Analyzer analyzer)
        {
            if (!this.analyzed)
            {
                base.Analyze(analyzer);

                // check some special constants (ignoring namespace)
                if (this.Name.Value == GlobalConstant.Null.FullName ||
                    this.Name.Value == GlobalConstant.False.FullName ||
                    this.Name.Value == GlobalConstant.True.FullName)
                    analyzer.ErrorSink.Add(FatalErrors.ConstantRedeclared, analyzer.SourceUnit, Position, this.Name.Value);
            }
        }

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitGlobalConstantDecl(this);
        }
	}

	#endregion

}
