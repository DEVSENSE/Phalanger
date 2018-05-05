/*

 Copyright (c) 2004-2006 Tomas Matousek.

 The use and distribution terms for this software are contained in the file named License.txt,
 which can be found in the root of the Phalanger distribution. By using this software
 in any fashion, you are agreeing to be bound by the terms of this license.

 You must not remove this notice from this software.

*/

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;

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
		    private class StaticInfo
			{
				public Dictionary<string, int> LibraryCalls;
				public Dictionary<QualifiedName, int> UnknownCalls;
				public Dictionary<string, int> Nodes;

				public static StaticInfo Get
				{
					get
					{
						StaticInfo info;
						var properties = ThreadStatic.Properties;
						if (properties.TryGetProperty<StaticInfo>(out info) == false || info == null)
						{
							properties.SetProperty(info = new StaticInfo());
						}
						return info;
					}
				}
			}

			[Conditional("DEBUG")]
			public static void AddLibraryFunctionCall(string name, int paramCount)
			{
				var info = StaticInfo.Get;
				if (info.LibraryCalls == null) info.LibraryCalls = new Dictionary<string, int>();

				CollectionUtils.IncrementValue(info.LibraryCalls, name + ';' + paramCount, 1);
			}

			[Conditional("DEBUG")]
			public static void AddUnknownFunctionCall(QualifiedName name)
			{
				var info = StaticInfo.Get;
				if (info.UnknownCalls == null) info.UnknownCalls = new Dictionary<QualifiedName, int>();

				CollectionUtils.IncrementValue(info.UnknownCalls, name, 1);
			}

			[Conditional("DEBUG")]
			public static void AddNode(string name)
			{
				var info = StaticInfo.Get;
				if (info.Nodes == null) info.Nodes = new Dictionary<string, int>();
				CollectionUtils.IncrementValue(info.Nodes, name, 1);
			}

			[Conditional("DEBUG")]
			public static void DumpBasic(TextWriter output)
			{
				var info = StaticInfo.Get;
				output.WriteLine("AST: LibraryFunctionCalls = {0}, UnknownFunctionCalls = {1}",
				  (info.LibraryCalls != null) ? info.LibraryCalls.Count : 0,
				  (info.UnknownCalls != null) ? info.UnknownCalls.Count : 0);
			}

			[Conditional("DEBUG")]
			public static void DumpLibraryFunctionCalls(TextWriter output)
			{
				var info = StaticInfo.Get;
				if (info.LibraryCalls == null) return;

				string[] keys = new string[info.LibraryCalls.Count];
				int[] values = new int[info.LibraryCalls.Count];

				info.LibraryCalls.Keys.CopyTo(keys, 0);
				info.LibraryCalls.Values.CopyTo(values, 0);

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
				var info = StaticInfo.Get;
				if (info.UnknownCalls == null) return;

				QualifiedName[] names = new QualifiedName[info.UnknownCalls.Count];
				info.UnknownCalls.Keys.CopyTo(names, 0);

				//TODO:Array.Sort(names);

				for (int i = 0; i < names.Length; i++)
				{
					output.WriteLine(names[i]);
				}
			}

			[Conditional("DEBUG")]
			public static void DumpNodes(TextWriter output)
			{
				var info = StaticInfo.Get;
				if (info.Nodes == null) return;

				string[] keys = new string[info.Nodes.Count];
				int[] values = new int[info.Nodes.Count];

				info.Nodes.Keys.CopyTo(keys, 0);
				info.Nodes.Values.CopyTo(values, 0);

				//TODO:Array.Sort(values,keys);

				output.WriteLine("node;emitted instances");
				for (int i = 0; i < info.Nodes.Count; i++)
				{
					output.WriteLine(keys[i] + ";" + values[i]);
				}
			}
		}

		#endregion

		[Conditional("DEBUG")]
		internal static void Dump(TextWriter/*!*/ output, string path)
		{
		    try
		    {
                CompilationEnd();
                output.WriteLine("Compiled in {0}:{1:00}.{2:000}.", duration.Minutes, duration.Seconds, duration.Milliseconds);

                AST.DumpBasic(output);

                using (StreamWriter f = File.CreateText(Path.Combine(path, "LibraryCalls.csv")))
                {
                    AST.DumpLibraryFunctionCalls(f);
                }

                using (StreamWriter f = File.CreateText(Path.Combine(path, "UnknownCalls.csv")))
                {
                    AST.DumpUnknownFunctionCalls(f);
                }

                using (StreamWriter f = File.CreateText(Path.Combine(path, "EmittedNodes.csv")))
                {
                    AST.DumpNodes(f);
                }
            }
		    catch (Exception ex)
		    {
                // Log error to stderr and move on.
                Console.Error.WriteLine("Error: " + ex);
		    }
		}
	}
}
