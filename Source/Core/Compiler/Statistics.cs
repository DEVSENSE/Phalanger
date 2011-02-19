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

#if !SILVERLIGHT
using System.CodeDom.Compiler; // IndentedTextWriter
#endif

namespace PHP.Core
{

	public sealed partial class Statistics
	{
		private static DateTime start;
		private static TimeSpan duration;

		[Conditional("DEBUG")]
		internal static void CompilationStarted()
		{
			start = DateTime.Now;
		}

		[Conditional("DEBUG")]
		internal static void CompilationEnd()
		{
			duration = DateTime.Now - start;
		}

		#region AST

		internal struct AST
		{
            [ThreadStatic]
			private static Dictionary<string, int> libraryCalls;
            [ThreadStatic]
            private static Dictionary<QualifiedName, int> unknownCalls;
            [ThreadStatic]
            private static Dictionary<string, int> nodes;

			[Conditional("DEBUG")]
			public static void AddLibraryFunctionCall(string name, int paramCount)
			{
				if (libraryCalls == null) libraryCalls = new Dictionary<string, int>();

				CollectionUtils.IncrementValue(libraryCalls, name + ';' + paramCount, 1);
			}

			[Conditional("DEBUG")]
			public static void AddUnknownFunctionCall(QualifiedName name)
			{
				if (unknownCalls == null) unknownCalls = new Dictionary<QualifiedName, int>();

				CollectionUtils.IncrementValue(unknownCalls, name, 1);
			}

			[Conditional("DEBUG")]
			public static void AddNode(string name)
			{
				if (nodes == null) nodes = new Dictionary<string, int>();
				CollectionUtils.IncrementValue(nodes, name, 1);
			}

			[Conditional("DEBUG")]
			public static void DumpBasic(TextWriter output)
			{
				output.WriteLine("AST: LibraryFunctionCalls = {0}, UnknownFunctionCalls = {1}",
				  (libraryCalls != null) ? libraryCalls.Count : 0,
				  (unknownCalls != null) ? unknownCalls.Count : 0);
			}

			[Conditional("DEBUG")]
			public static void DumpLibraryFunctionCalls(TextWriter output)
			{
				if (libraryCalls == null) return;

				string[] keys = new string[libraryCalls.Count];
				int[] values = new int[libraryCalls.Count];

				libraryCalls.Keys.CopyTo(keys, 0);
				libraryCalls.Values.CopyTo(values, 0);

				//TODO:Array.Sort(values, keys);

				output.WriteLine("name;params;count");
				for (int i = keys.Length - 1; i >= 0; i--)
				{
					output.WriteLine(keys[i] + ";" + values[i]);
				}
			}

			[Conditional("DEBUG")]
			public static void DumpUnknownFunctionCalls(TextWriter output)
			{
				if (unknownCalls == null) return;

				QualifiedName[] names = new QualifiedName[unknownCalls.Count];
				unknownCalls.Keys.CopyTo(names, 0);

				//TODO:Array.Sort(names);

				for (int i = 0; i < names.Length; i++)
				{
					output.WriteLine(names[i]);
				}
			}

			[Conditional("DEBUG")]
			public static void DumpNodes(TextWriter output)
			{
				if (nodes == null) return;

				string[] keys = new string[nodes.Count];
				int[] values = new int[nodes.Count];

				nodes.Keys.CopyTo(keys, 0);
				nodes.Values.CopyTo(values, 0);

				//TODO:Array.Sort(values,keys);

				output.WriteLine("node;emitted instances");
				for (int i = 0; i < nodes.Count; i++)
				{
					output.WriteLine(keys[i] + ";" + values[i]);
				}
			}
		}

		#endregion

		[Conditional("DEBUG")]
		internal static void Dump(TextWriter/*!*/ output, string path)
		{
			Statistics.CompilationEnd();
			output.WriteLine(string.Format
				("Compiled in {0}:{1:00}.{2:000}.", duration.Minutes, duration.Seconds, duration.Milliseconds));

			AST.DumpBasic(output);

			using (StreamWriter f = File.CreateText(Path.Combine(path, "LibraryCalls.csv")))
				AST.DumpLibraryFunctionCalls(f);

			using (StreamWriter f = File.CreateText(Path.Combine(path, "UnknownCalls.csv")))
				AST.DumpUnknownFunctionCalls(f);

			using (StreamWriter f = File.CreateText(Path.Combine(path, "EmittedNodes.csv")))
				AST.DumpNodes(f);
		}
	}
}