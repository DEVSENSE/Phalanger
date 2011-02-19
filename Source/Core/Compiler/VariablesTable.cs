/*

 Copyright (c) 2006 Tomas Matousek.
 Copyright (c) 2003-2005 Pavel Novak.
 
*/

using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Diagnostics;

namespace PHP.Core
{
	/// <summary>
	/// Table storing information about variables at compile time.
	/// </summary>
	internal class VariablesTable : IEnumerable<VariablesTable.Entry>
	{
		#region Nested Class: Entry

		/// <summary>
		/// Class for <seealso cref="PHP.Core.VariablesTable"/>. Instances are stored in that table.
		/// </summary>
		public class Entry
		{
			/// <summary>
			/// Variable place
			/// </summary>
			private Emit.IPlace variable;
			public Emit.IPlace Variable { get { return variable; } set { variable = value; } }
	
			/// <summary>
			/// Get variable name.
			/// </summary>
			public VariableName VariableName { get { return varName; } }
			private VariableName varName;

			/// <summary>
			/// Get or set if current variable is declared as PhpReference (<c>true</c>) or as Object (<c>false</c>).
			/// </summary>
			public bool IsPhpReference { get { return isPhpReference; } set { isPhpReference = value; } }
			private bool isPhpReference;

			/// <summary>
			/// Get or set if current variable is function parameter (<c>true</c>) or regular local variable (<c>false</c>).
			/// </summary>
			public bool IsParameter { get { return isParameter; } set { isParameter = value; } }
			private bool isParameter;

			/// <summary>
			/// Gets or sets if this variable is directly used.
			/// </summary>
			/// <remarks>This value is always <c>true</c> for regular local variables, it can be false only if entry
			/// represents a parameter.</remarks>
			public bool IsDirectlyUsed { get { return isDirectlyUsed; } set { isDirectlyUsed = value; } }
			private bool isDirectlyUsed;

			public Entry(VariableName varName, bool isPhpReference)
				: this(varName, isPhpReference, false)
			{
			}

			public Entry(VariableName varName, bool isPhpReference, bool isParameter)
			{
				this.varName = varName;
				this.isPhpReference = isPhpReference;
				this.isParameter = isParameter;

				// parameters are not directly used by default
				this.isDirectlyUsed = !isParameter;
			}

		}

		#endregion

		internal const int SuboptimalLocalsCount = 50;

		private Dictionary<VariableName, Entry>/*!*/ variables;
		private bool allRef = false;

		public VariablesTable()
		{
			variables = new Dictionary<VariableName, Entry>();
		}

		public VariablesTable(int size)
		{
			variables = new Dictionary<VariableName, Entry>(size);
		}

		#region Access methods

		public void Set(VariableName varName, bool isPhpReference)
		{
			Entry entry;
			if (variables.TryGetValue(varName, out entry))
			{
				entry.IsDirectlyUsed = true;

				if (allRef || isPhpReference)
					entry.IsPhpReference = true;
			}
			else if (!varName.IsAutoGlobal)
			{
				variables.Add(varName, new Entry(varName, allRef || isPhpReference));
			}
		}


		public bool AddParameter(VariableName paramName, bool isPassedByRef)
		{
			Entry entry;
			if (variables.TryGetValue(paramName, out entry))
			{
				// parameter with the same name specified twice
                if (entry.IsParameter)
                {
                    //return false;
                }
                else
                {
                    Debug.Fail();
                    return true;
                }
			}
			
            // add variable entry
			variables[paramName] = new Entry(paramName, isPassedByRef, true);   // parameter can be specified twice in PHP, the last one is used
			return true;
		}

		public void SetAllRef()
		{
			allRef = true;

			foreach (Entry variable in variables.Values)
				variable.IsPhpReference = true;
		}

		public Entry this[VariableName name]
		{
			get { return variables[name]; }
		}

		public bool Contains(VariableName name)
		{
			return variables.ContainsKey(name);
		}

		public int Count { get { return variables.Count; } }

		#endregion

		#region IEnumerable<Entry> Members

		IEnumerator<VariablesTable.Entry> IEnumerable<VariablesTable.Entry>.GetEnumerator()
		{
			return variables.Values.GetEnumerator();
		}

		#endregion

		#region IEnumerable Members

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return variables.Values.GetEnumerator();
		}

		#endregion
	}
}