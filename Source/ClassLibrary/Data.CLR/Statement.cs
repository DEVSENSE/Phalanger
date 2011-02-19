/*

 Copyright (c) 2005-2006 Tomas Matousek.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.
 
*/

using System;
using System.Data;
using System.Collections;
using System.Collections.Specialized;

using PHP.Core;

namespace PHP.Library.Data
{
	/// <summary>
	/// Represents a parameterized SQL statement.
	/// </summary>
	public abstract class PhpDbStatement : PhpResource
	{
		#region Enum: ParameterType

		/// <summary>
		/// PHP type of the parameter. Parameter value will be converted accordign to this value.
		/// </summary>
		public enum ParameterType
		{
			Invalid = 0,
			String = 1,
			Double = 2,
			Integer = 3,
			Null = 4,
			Infer = 5
		}

		#endregion

		#region Bindings

		private class Binding // GENERICS: struct
		{
			public PhpReference/*!*/ Variable;
			public IDataParameter/*!*/ Parameter;
			public ParameterType Type;

			public Binding(PhpReference/*!*/ variable, IDataParameter/*!*/ parameter, ParameterType type)
			{
				Debug.Assert(variable != null && parameter != null && type != ParameterType.Invalid);

				this.Variable = variable;
				this.Parameter = parameter;
				this.Type = type;
			}
		}

		private Hashtable Bindings
		{
			get
			{
				if (_bindings == null)
					_bindings = CollectionsUtil.CreateCaseInsensitiveHashtable();

				return _bindings;
			}
		}
		private Hashtable _bindings; // GENERICS: <string, Binding>

		private bool BindingsDefined { get { return _bindings != null; } }

		#endregion

		/// <summary>
		/// Connection resource associated with the statement.
		/// </summary>
		public PhpDbConnection/*!*/ Connection { get { return connection; } }
		protected PhpDbConnection/*!*/ connection;

		/// <summary>
		/// Creates an instance of parameterized statement.
		/// </summary>
		/// <param name="resourceName">Name of the resource.</param>
		/// <param name="connection">Database connection resource.</param>
		public PhpDbStatement(string/*!*/ resourceName, PhpDbConnection/*!*/ connection)
			: base(resourceName)
		{
			if (connection == null)
				throw new ArgumentNullException("connection");

			this.connection = connection;
		}

		/// <summary>
		/// Adds a parameter to variable binding.
		/// </summary>
		/// <param name="parameter">SQL parameter.</param>
		/// <param name="variable">PHP variable passed by reference.</param>
		/// <param name="type">Parameter type specified by user.</param>
		/// <returns><B>true</B> if the binding succeeded.</returns>
		public bool AddBinding(IDataParameter/*!*/ parameter, PhpReference/*!*/ variable, ParameterType type)
		{
			if (parameter == null)
				throw new ArgumentNullException("parameter");

			if (variable == null)
				throw new ArgumentNullException("variable");

			if (type < ParameterType.String || type > ParameterType.Infer)
				throw new ArgumentOutOfRangeException("type");

			if (Bindings.ContainsKey(parameter.ParameterName))
				return false;

			Bindings.Add(parameter.ParameterName, new Binding(variable, parameter, type));
			return true;
		}

		/// <summary>
		/// Loads data from bound variables to the respective parameters.
		/// </summary>
		/// <returns>An array of parameters with loaded values.</returns>
		public IDataParameter[] PrepareParameters()
		{
			if (!BindingsDefined) return new IDataParameter[0];

			IDataParameter[] parameters = new IDataParameter[Bindings.Count];

			int i = 0;
			foreach (Binding binding in Bindings.Values)
			{
				if (binding.Parameter.Direction == ParameterDirection.InputOutput || binding.Parameter.Direction == ParameterDirection.Input)
				{
					switch (binding.Type)
					{
						case ParameterType.Double: binding.Parameter.Value = Core.Convert.ObjectToDouble(binding.Variable.Value); break;
						case ParameterType.String: binding.Parameter.Value = Core.Convert.ObjectToString(binding.Variable.Value); break;
						case ParameterType.Integer: binding.Parameter.Value = Core.Convert.ObjectToInteger(binding.Variable.Value); break;
						case ParameterType.Null: binding.Parameter.Value = DBNull.Value; break;
						case ParameterType.Infer: binding.Parameter.Value = binding.Variable.Value; break;
						default: Debug.Fail(); break;
					}
				}

				parameters[i++] = binding.Parameter;
			}

			return parameters;
		}

		/// <summary>
		/// Writes parameter values back to the bound variables.
		/// </summary>
		public void WriteParametersBack()
		{
			if (!BindingsDefined) return;

			foreach (Binding binding in Bindings.Values)
			{
				if (binding.Parameter.Direction != ParameterDirection.Input)
				{
					switch (binding.Type)
					{
						case ParameterType.Double: binding.Variable.Value = Core.Convert.ObjectToDouble(binding.Parameter.Value); break;
						case ParameterType.String: binding.Variable.Value = Core.Convert.ObjectToString(binding.Parameter.Value); break;
						case ParameterType.Integer: binding.Variable.Value = Core.Convert.ObjectToInteger(binding.Parameter.Value); break;
						case ParameterType.Null: binding.Variable.Value = binding.Parameter.Value; break;
						case ParameterType.Infer: binding.Variable.Value = binding.Parameter.Value; break;
						default: Debug.Fail(); break;
					}
				}
			}
		}
	}
}
