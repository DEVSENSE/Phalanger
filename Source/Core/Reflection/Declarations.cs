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

using PHP.Core;
using PHP.Core.AST;
using PHP.Core.Parsers;
using System.Diagnostics;

namespace PHP.Core.Reflection
{
	public enum DeclarationKind
	{
		Type,
		Function,
		Constant
	}

	public interface IDeclaree
	{
		QualifiedName QualifiedName { get; }
		VersionInfo Version { get; set; }
		Declaration/*!*/ Declaration { get; }
		string/*!*/ FullName { get; }

		void ReportRedeclaration(ErrorSink/*!*/ errors);
	}

	public sealed class DeclarationGroup
	{
		public int UnconditionalCount { get { return unconditionalCount; } }
		private int unconditionalCount;

		public int ConditionalCount { get { return conditionalCount; } }
		private int conditionalCount;

		public int PartialCount { get { return partialCount; } }
		private int partialCount;

		public Declaration Aggregate { get { return aggregate; } set { aggregate = value; } }
		private Declaration aggregate;

		public DeclarationGroup(bool isConditional, bool isPartial)
		{
			this.conditionalCount = 0;
			this.unconditionalCount = 0;
			this.partialCount = 0;
			this.aggregate = null;

			AddDeclaration(isConditional, isPartial);
		}

		public void AddDeclaration(bool isConditional, bool isPartial)
		{
			Debug.Assert(!(isPartial && isConditional), "partial cannot be conditional");

			if (isPartial) partialCount++;
			else if (isConditional) conditionalCount++;
			else unconditionalCount++;
		}
	}

	internal interface IDeclarationNode
	{
		void PreAnalyze(Analyzer/*!*/ analyzer);
		void AnalyzeMembers(Analyzer/*!*/ analyzer);
	}

	public sealed class Declaration
	{
		public bool IsConditional { get { return isConditional; } }
		private bool isConditional;

        public CompilationSourceUnit/*!*/ SourceUnit { get { return sourceUnit; } }
        private readonly CompilationSourceUnit/*!*/ sourceUnit;

		/// <summary>
		/// Compile-time non-pure only. Used when added to the containing compilation unit.
		/// </summary>
		public Scope Scope { get { return scope; } internal set /* friend PureCompilationUnit */ { scope = value; } }
		private Scope scope;

		public IDeclaree/*!*/ Declaree { get { return declaree; } }
		private readonly IDeclaree/*!*/ declaree;

		/// <summary>
		/// Types stores their AST nodes here to allow fast member-analysis.
		/// Should be nulled by the analysis (to cut AST off).
		/// </summary>
		internal IDeclarationNode Node { get { return node; } /* friend ClassDecl */  set { node = value; } }
		private IDeclarationNode node;

		/// <summary>
		/// Declaration group or null if there is only single declaration.
		/// Can be non-null for both conditional and unconditional decls (during merging).
		/// </summary>
		public DeclarationGroup Group { get { return group; } set { group = value; } }
		private DeclarationGroup group;

        public Text.Span Span { get { return span; } }
        private readonly Text.Span span;

		/// <summary>
		/// Set by analyzer.
		/// </summary>
        public bool IsUnreachable { get { return isUnreachable; } internal set { isUnreachable = value; } }
        private bool isUnreachable = true;

		/// <summary>
		/// Set by analyzer.
		/// </summary>
		public bool IsSynthetic { get { return isSynthetic; } internal set { isSynthetic = value; } }
		private bool isSynthetic = false;

		public bool IsPartial { get { return isPartial; } internal /* friend PureCompilationUnit */ set { isPartial = value; } }
		private bool isPartial = false;

		// if function is inside unknown class it can't be declared at compile time
		private bool isInsideIncompleteClass = false;
		public bool IsInsideIncompleteClass { get { return isInsideIncompleteClass; } internal set { isInsideIncompleteClass = value; } }

        public Declaration(CompilationSourceUnit/*!*/ sourceUnit, IDeclaree/*!*/ declaree, bool isPartial, bool isConditional,
            Scope scope, Text.Span position)
		{
			this.sourceUnit = sourceUnit;
			this.declaree = declaree;
			this.scope = scope;
			this.span = position;
			this.isPartial = isPartial;
			this.isConditional = isConditional;
		}

		public Declaration GetExactVersion(Declaration/*!*/ declaration)
		{
			IDeclaree declaree = this.declaree;
			do
			{
				if (ReferenceEquals(declaree, declaration.Declaree))
					return declaree.Declaration;

				declaree = declaree.Version.Next;
			}
			while (declaree != null);

			return null;
		}

		public object GetNode()
		{
			if (node == null)
				throw new InvalidOperationException();

			return node;
		}

		internal static IEnumerable<T> GetDeclarees<T>(IEnumerable<Declaration>/*!*/ table)
			where T : IDeclaree
		{
			foreach (Declaration decl in table)
				yield return (T)decl.Declaree;
		}
	}
}
