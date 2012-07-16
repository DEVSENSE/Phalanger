/*

 Copyright (c) 2006 Tomas Matousek.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/
using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

using PHP.Core;
using PHP.Core.Parsers;
using PHP.Core.Emit;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.CodeDom.Compiler;
using System.CodeDom;

namespace PHP.Core.Reflection
{
	internal enum Characteristic
	{
		StaticArgEvaluated,
		StaticArgReplaced,
		StaticAutoInclusion,
		Dynamic
	}

	#region InclusionMapping

	/// <summary>
	/// Defines an inclusion mapping.
	/// </summary>
	[Serializable]
	public struct InclusionMapping
	{
		/// <summary>
		/// Name identifying the mapping.
		/// </summary>
		public string Name { get { return name; } }
		private string name;

		/// <summary>
		/// Replacement.
		/// </summary>
		public string Replacement { get { return replacement; } }
		private string/*!*/ replacement;

		/// <summary>
		/// Pattern.
		/// </summary>
		public Regex Pattern { get { return pattern; } }
		private Regex/*!*/ pattern;

        /// <summary>
        /// Group name interpreted as the source root of application, <see cref="ApplicationConfiguration.CompilerSection.SourceRoot"/>.
        /// </summary>
        private const string SourceRootGroupName = "${SourceRoot}";

		/// <summary>
		/// Creates an inclusion mapping.
		/// </summary>
		/// <exception cref="ArgumentException"><paramref name="pattern"/> is not a valid regular expression pattern.</exception>
		/// <exception cref="ArgumentNullException"><paramref name="pattern"/> or <paramref name="replacement"/> is a <B>null</B> reference.</exception>
		public InclusionMapping(string/*!*/ pattern, string/*!*/ replacement, string name)
		{
			if (pattern == null)
				throw new ArgumentNullException("pattern");
			if (replacement == null)
				throw new ArgumentNullException("replacement");

			this.pattern = new Regex(pattern, RegexOptions.IgnoreCase);
			this.name = name;
			this.replacement = replacement;
		}


		/// <summary>
		/// Translates expression (a parameter of include/require) according to the pattern specified in the configuration.
		/// </summary>
		/// <param name="expression">The expression to be translated via regexp pattern.</param>
		/// <param name="mappings">A list of mappings.</param>
        /// <param name="sourceRoot">The <see cref="ApplicationConfiguration.CompilerSection.SourceRoot"/> used to patch <see cref="InclusionMapping.Replacement"/> string.</param>
		internal static string TranslateExpression(IEnumerable<InclusionMapping>/*!*/ mappings, string/*!*/ expression, string sourceRoot)
		{
			Debug.Assert(mappings != null && expression != null);
			string trimmed_expression = expression.Trim();

			foreach (InclusionMapping mapping in mappings)
			{
				// the regex not empty => perform translation:
				Match m = mapping.Pattern.Match(trimmed_expression);

				// regex matches:
                if (m.Success)
                    return m.Result(mapping.Replacement.Replace(SourceRootGroupName, sourceRoot));
			}

			// no regex does match:
			return null;
		}
	}

	#endregion

	#region InclusionGraphBuilder

	internal sealed class InclusionGraphBuilder : IDisposable
	{
		public CompilationContext/*!*/ Context { get { return context; } }
		private readonly CompilationContext/*!*/ context;

		public Dictionary<PhpSourceFile, CompilationUnit> Nodes { get { return nodes; } }
		private readonly Dictionary<PhpSourceFile, CompilationUnit> nodes = new Dictionary<PhpSourceFile, CompilationUnit>();

		public List<StaticInclusion> PendingInclusions { get { return pendingInclusions; } }
		private readonly List<StaticInclusion> pendingInclusions = new List<StaticInclusion>();

		public InclusionGraphBuilder(CompilationContext/*!*/ context)
		{
			this.context = context;
			Statistics.Inclusions.InitializeGraph();
		}

		#region Graph Building Operations

		internal void NodeAdded(CompilationUnit/*!*/ compilationUnit)
		{
			Statistics.Inclusions.AddNode(compilationUnit);
		}

		internal void EdgeAdded(StaticInclusion/*!*/ staticInclusion)
		{
			Statistics.Inclusions.AddEdge(staticInclusion);
		}

		public void Dispose()
		{
			Statistics.Inclusions.BakeGraph();
		}

		#endregion

		public bool AnalyzeDfsTree(PhpSourceFile/*!*/ rootSourceFile)
		{
			CompilationUnit root = GetNode(rootSourceFile);

			ScriptCompilationUnit rootScript = root as ScriptCompilationUnit;
			if (rootScript != null && rootScript.State == CompilationUnit.States.Initial)
			{
				Analyzer analyzer = null;

				try
				{
					// builds the tree of parsed units via DFS:
					ProcessNode(rootScript);

					// finishes pending inclusions via MFP:
					ProcessPendingInclusions();

					analyzer = new Analyzer(context);

					// pre-analysis:
					rootScript.PreAnalyzeRecursively(analyzer);

					// member analysis:
					rootScript.AnalyzeMembersRecursively(analyzer);

					if (context.Errors.AnyFatalError) return false;

					// full analysis:
					rootScript.AnalyzeRecursively(analyzer);

					if (context.Errors.AnyFatalError) return false;

					// perform post analysis:
					analyzer.PostAnalyze();

					if (context.Errors.AnyError) return false;

					// TODO:
					// define constructed types:
					analyzer.DefineConstructedTypeBuilders();
				}
				catch (CompilerException)
				{
					root.State = CompilationUnit.States.Erroneous;
					return false;
				}

				if (context.Errors.AnyError) return false;
			}
			else if (root.State != CompilationUnit.States.Analyzed && root.State != CompilationUnit.States.Reflected)
			{
				return false;
			}

			return true;
		}

		public void EmitAllUnits(CodeGenerator/*!*/ codeGenerator)
		{
#if DEBUG
			Console.WriteLine("Generating code ...");
#endif

			foreach (ScriptCompilationUnit unit in SelectNonReflectedUnits(nodes.Values))
			{
				Debug.WriteLine("IG", "DefineBuilders: " + unit.SourceUnit.SourceFile);
				unit.DefineBuilders(context);
			}

			foreach (ScriptCompilationUnit unit in SelectNonReflectedUnits(nodes.Values))
			{
				Debug.WriteLine("IG", "Emit: " + unit.SourceUnit.SourceFile);
				unit.Emit(codeGenerator);
			}

			foreach (ScriptCompilationUnit unit in SelectNonReflectedUnits(nodes.Values))
			{
				Debug.WriteLine("IG", "Bake: " + unit.SourceUnit.SourceFile);
				unit.Bake();
			}

			foreach (ScriptCompilationUnit unit in SelectNonReflectedUnits(nodes.Values))
			{
				Debug.WriteLine("IG", "Persist: " + unit.SourceUnit.SourceFile);
				codeGenerator.Context.Manager.Persist(unit, codeGenerator.Context);
			}
		}

		/// <summary>
		/// Selects only units that are in other than 'Reflected' state. This prevents us from 
		/// trying to build 'Reflected' units (which is of course impossible)
		/// </summary>
		private IEnumerable<ScriptCompilationUnit> SelectNonReflectedUnits(Dictionary<PhpSourceFile, 
			CompilationUnit>.ValueCollection values)
		{
			foreach (CompilationUnit unit in values)
				if (unit.State != CompilationUnit.States.Reflected) yield return (ScriptCompilationUnit)unit;
		}
        
		public void CleanAllUnits(CompilationContext/*!*/ context, bool successful)
		{
			foreach (CompilationUnit unit in nodes.Values)
				unit.CleanUp(context, successful);
		}

		/// <summary>
		/// Gets the node of the graph associated with the specified source file.
		/// First, look up the table of processed nodes. 
		/// If not there, check compiled units maintained by the manager.
		/// If it is not found in the manager's cache the source file is locked so that other compilers will 
		/// wait until we finish compilation of the node. The new node is created if the compilation unit doesn't exist for it.
		/// </summary>
		internal CompilationUnit GetNode(PhpSourceFile/*!*/ sourceFile)
		{
			CompilationUnit result;
			if (!nodes.TryGetValue(sourceFile, out result))
			{
				ScriptModule module;

                module = (ScriptModule)context.Manager.LockForCompiling(sourceFile, context);

				if (module != null)
				{
					result = module.CompilationUnit;
				}
				else
				{
					ScriptCompilationUnit scriptResult = new ScriptCompilationUnit();
					scriptResult.SourceUnit = new SourceFileUnit(scriptResult, sourceFile, context.Config.Globalization.PageEncoding);
					result = scriptResult;
				}
				nodes.Add(sourceFile, result);
				NodeAdded(result);
			}

			return result;
		}

		private void ProcessNode(ScriptCompilationUnit/*!*/ node)
		{
			Debug.Assert(node.State == CompilationUnit.States.Initial);

			// parses the unit and fills its tables:
			node.Parse(context);

			// resolves outgoing edges:
			node.ResolveInclusions(this);

			// follow DFS tree edges:
			foreach (StaticInclusion edge in node.Inclusions)
			{
				switch (edge.Includee.State)
				{
					case CompilationUnit.States.Initial:
						Debug.Assert(edge.Includee is ScriptCompilationUnit);

						// recursive descent:
						ProcessNode((ScriptCompilationUnit)edge.Includee); // TODO: consider!
						node.MergeTables(edge);
						break;

					case CompilationUnit.States.Parsed:
						// edge closing a cycle:
						pendingInclusions.Add(edge);
						break;

					case CompilationUnit.States.Processed:
						// transverse edge to already processed subtree:
						node.MergeTables(edge);
						break;

					case CompilationUnit.States.Compiled:
						// descent edge to the compiled node:
						edge.Includee.Reflect();
						node.MergeTables(edge);
						break;

					case CompilationUnit.States.Analyzed:
					case CompilationUnit.States.Reflected:
						// descent or transverse edge to already analyzed or compiled and reflected node:
						node.MergeTables(edge);
						break;

					default:
						Debug.Fail("Unexpected CU state");
						throw null;
				}
			}

			node.State = CompilationUnit.States.Processed;
		}

		/// <summary>
		/// Minimal fixpoint algorithm.
		/// </summary>
		private void ProcessPendingInclusions()
		{
			while (pendingInclusions.Count > 0)
			{
				StaticInclusion inclusion = pendingInclusions[pendingInclusions.Count - 1];
				pendingInclusions.RemoveAt(pendingInclusions.Count - 1);

				Debug.Assert(inclusion.Includer.State == CompilationUnit.States.Processed);
				Debug.Assert(inclusion.Includee.State == CompilationUnit.States.Processed);

				if (inclusion.Includer.MergeTables(inclusion) > 0)
				{
					foreach (StaticInclusion incoming in inclusion.Includer.Includers)
						pendingInclusions.Add(incoming);
				}
			}
		}
	}

	#endregion

	#region StaticInclusion

	public sealed class StaticInclusion
	{
		public ScriptCompilationUnit/*!*/ Includer { get { return includer; } }
		private readonly ScriptCompilationUnit/*!*/ includer;

		public CompilationUnit/*!*/ Includee { get { return includee; } }
		private readonly CompilationUnit/*!*/ includee;

		public Scope Scope { get { return scope; } }
		private readonly Scope scope;

		public InclusionTypes InclusionType { get { return inclusionType; } }
		private readonly InclusionTypes inclusionType;

		public bool IsConditional { get { return isConditional; } }
		private bool isConditional;

		public StaticInclusion(ScriptCompilationUnit/*!*/ includer, CompilationUnit/*!*/ includee, Scope scope,
			bool isConditional, InclusionTypes inclusionType)
		{
			this.scope = scope;
			this.inclusionType = inclusionType;
			this.includee = includee;
			this.includer = includer;
			this.isConditional = isConditional;
		}
	}

	#endregion
}
