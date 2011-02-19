/*

 Copyright (c) 2004-2006 Tomas Matousek.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using PHP.Core.Reflection;
using System.CodeDom.Compiler;

namespace PHP.Core
{
	public sealed partial class Statistics
	{
		#region Inclusions

		public static bool DrawGraph = false;

		internal struct Inclusions
		{
			private static IndentedTextWriter output;
			private static Dictionary<SourceUnit, int> sourceUnits;
			private static int nodeId;
			private static int edgeId;

			[Conditional("DEBUG")]
			public static void InitializeGraph()
			{
				if (!DrawGraph) return;

				output = new IndentedTextWriter(new StreamWriter("C:\\Inclusions.dot"), "  ");
				sourceUnits = new Dictionary<SourceUnit, int>();
				nodeId = 1;
				edgeId = 1;

				output.WriteLine("digraph Inclusions");
				output.WriteLine("{");
				output.Indent++;

				output.WriteLine("node [shape=box];");
				output.WriteLine();
			}

			[Conditional("DEBUG")]
			public static void AddNode(CompilationUnit/*!*/ compilationUnit)
			{
				if (!DrawGraph) return;

				if (compilationUnit is ScriptCompilationUnit)
				{
					SourceUnit sourceUnit = ((ScriptCompilationUnit)compilationUnit).SourceUnit;

					int node_id;
					if (!sourceUnits.TryGetValue(sourceUnit, out node_id))
						sourceUnits.Add(sourceUnit, node_id = nodeId++);

					output.Write(node_id);
					output.Write(" [label=\"");
					output.Write(sourceUnit.SourceFile.RelativePath);
					output.WriteLine("\"];");
				}
				else
				{
					output.Write(-1);
					output.Write(" [label=\"ReflectedUnit\"];");
				}
				output.Flush();
			}

			[Conditional("DEBUG")]
			public static void AddEdge(StaticInclusion/*!*/ inclusion)
			{
				if (!DrawGraph) return;

				output.Write(sourceUnits[inclusion.Includer.SourceUnit]);
				output.Write(" -> ");
				if (inclusion.Includee is ScriptCompilationUnit)
					output.Write(sourceUnits[((ScriptCompilationUnit)inclusion.Includee).SourceUnit]);
				else
					output.Write("ReflectedUnit");
				output.Write(" [label={0}{1}];", edgeId++, (inclusion.IsConditional) ? ", style=dotted" : "");
				output.WriteLine();
				output.Flush();
			}

			[Conditional("DEBUG")]
			public static void BakeGraph()
			{
				if (!DrawGraph) return;

				output.Indent--;
				output.WriteLine("}");
				output.Close();
			}
		}

		#endregion

	}
}