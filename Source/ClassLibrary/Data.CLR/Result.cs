/*

 Copyright (c) 2005-2006 Tomas Matousek and Martin Maly.  

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Runtime.Serialization;
using System.Data;
using System.Data.SqlClient;
using System.Collections;
using System.Collections.Generic;

using PHP.Core;
using PHP.Core.Reflection;
using System.Diagnostics;

namespace PHP.Library.Data
{
	/// <summary>
	/// Holds a result of a query.
	/// </summary>
	public abstract class PhpDbResult : PhpResource
	{
		/// <summary>
		/// Represents a single result set returned by query.
		/// </summary>
		protected sealed class ResultSet
		{
			/// <summary>
			/// Rows.
			/// </summary>
			public ArrayList Rows;

			/// <summary>
			/// Names of columns in query.
			/// </summary>
			public string[] Names;

			/// <summary>
			/// Names of SQL types of columns in query.
			/// </summary>
			public string[] DataTypes;

			/// <summary>
			/// Number of records affected by the query.
			/// </summary>
			public int RecordsAffected = -1;

            /// <summary>
            /// Custom data obtained from the row by <see cref="GetCustomData"/> callback function of specific PhpDbResult implementation.
            /// </summary>
            public object CustomData;
		}

		/// <summary>
		/// Source data reader.
		/// </summary>
		internal protected IDataReader Reader { get { return reader; } set { reader = value; } }
		private IDataReader reader;

		private PhpDbConnection connection;
		private List<ResultSet> resultSets;

		#region Fields and Properties

		/// <summary>
		/// Command whose result is represented by this instance.
		/// </summary>
		public IDbCommand Command { get { return command; } }
		internal IDbCommand command; // GENERICS: internal set 

		/// <summary>
		/// Gets the index of the current result set. Initialized to 0.
		/// </summary>
		public int CurrentSetIndex { get { return currentSetIndex; } }
		private int currentSetIndex;

		/// <summary>
		/// Gets the index of the current row or -1 if no row has been fetched yet.
		/// </summary>
		public int CurrentRowIndex { get { return currentRowIndex; } }
		private int currentRowIndex;

		/// <summary>
		/// Gets the index of the current field. Initialized to 0.
		/// </summary>
		public int CurrentFieldIndex { get { return currentFieldIndex; } }
		private int currentFieldIndex;

		/// <summary>
		/// Gets the index of the last fetched field. Initialized to -1.
		/// </summary>
		public int LastFetchedField { get { return lastFetchedField; } }
		private int lastFetchedField = -1;

		/// <summary>
		/// Gets the number of rows of the result.
		/// </summary>
		public int RowCount { get { Debug.Assert(CurrentSet.Rows != null); return CurrentSet.Rows.Count; } }

		/// <summary>
		/// Gets the number of fields of the result. Returns 0 if data are not loaded.
		/// </summary>
		public int FieldCount { get { Debug.Assert(CurrentSet.Names != null); return CurrentSet.Names.Length; } }

		/// <summary>
		/// Gets the number of records affected by the query that generates this result.
		/// Contains minus one for select queries.
		/// </summary>
		public int RecordsAffected { get { return CurrentSet.RecordsAffected; } }

		#endregion

		#region Result Sets

		/// <summary>
		/// Gets the current result set.
		/// </summary>
		protected ResultSet/*!*/ CurrentSet
		{
			get { return (ResultSet)resultSets[currentSetIndex]; }
		}

		/// <summary>
		/// Gets the number of results sets.
		/// </summary>
		public int ResultSetCount
		{
			get { return resultSets.Count; }
		}

		/// <summary>
		/// Advances the current result set index.
		/// </summary>
		/// <returns>Whether the index has been advanced.</returns>
		public bool NextResultSet()
		{
			if (currentSetIndex < resultSets.Count - 1)
			{
				currentSetIndex++;
				currentRowIndex = -1;
				currentFieldIndex = 0;
				return true;
			}

			return false;
		}

		#endregion

		#region Constructors, Population, Release

		/// <summary>
		/// Creates an instance of a result resource.
		/// </summary>
		/// <param name="connection">Database connection.</param>
		/// <param name="reader">Data reader from which to load results.</param>
		/// <param name="name">Resource name.</param>
		/// <param name="convertTypes">Whether to convert resulting values to PHP types.</param>
		/// <exception cref="ArgumentNullException">Argument is a <B>null</B> reference.</exception>
		protected PhpDbResult(PhpDbConnection/*!*/ connection, IDataReader/*!*/ reader, string/*!*/ name, bool convertTypes)
			: base(name, false)
		{
			if (connection == null)
				throw new ArgumentNullException("connection");

			if (reader == null)
				throw new ArgumentNullException("reader");

			this.reader = reader;
			this.connection = connection;
			LoadData(convertTypes);
		}

		/// <summary>
		/// Loads all data from the reader to arrays.
		/// </summary>
		/// <remarks>This method should be called before any other method.</remarks>
		private void LoadData(bool convertTypes)
		{
            this.resultSets = new List<ResultSet>(16);

            var reader = this.reader;

			do
			{
                ResultSet result_set = new ResultSet()
                {
                    Rows = new ArrayList(),
                    Names = GetNames(),
                    DataTypes = GetDataTypes(),
                    RecordsAffected = reader.RecordsAffected,
                    CustomData = GetCustomData()
                };

				while (reader.Read())
				{
					result_set.Rows.Add(this.GetValues(result_set.DataTypes, convertTypes));
				}

				resultSets.Add(result_set);
			}
			while (reader.NextResult());

			this.currentSetIndex = 0;
			this.currentRowIndex = -1;
			this.currentFieldIndex = 0;
		}

		internal void ReleaseConnection()
		{
			this.connection = null;
		}

		#endregion

		#region Virtual Methods

		/// <summary>
		/// Retrieves column names from the reader.
		/// </summary>
		/// <returns>An array of column names.</returns>
		protected virtual string[]/*!*/ GetNames()
		{
			string[] names = new string[reader.FieldCount];
			for (int i = 0; i < reader.FieldCount; i++)
				names[i] = reader.GetName(i);

			return names;
		}

		/// <summary>
		/// Retrieves column type names from the reader.
		/// </summary>
		/// <returns>An array of column type names.</returns>
		protected virtual string[]/*!*/ GetDataTypes()
		{
			string[] names = new string[reader.FieldCount];
			for (int i = 0; i < reader.FieldCount; i++)
				names[i] = reader.GetDataTypeName(i);

			return names;
		}

        /// <summary>
        /// Get custom data of current row of <see cref="Reader"/>. Used when loading data from database.
        /// </summary>
        /// <returns>Custom object associated with current row.</returns>
        protected virtual object GetCustomData()
        {
            return null;
        }

		/// <summary>
		/// Gets values of the current row from the reader.
		/// </summary>
		/// <param name="dataTypes">Column type names.</param>
		/// <param name="convertTypes">Whether to convert types of values to PHP types.</param>
		/// <returns>An array of values of cells in the current row.</returns>
		protected abstract object[]/*!*/ GetValues(string[] dataTypes, bool convertTypes);

		/// <summary>
		/// Maps SQL type name to PHP type name.
		/// </summary>
		/// <param name="typeName">SQL type name.</param>
		/// <returns>PHP type name.</returns>
		protected abstract string/*!*/ MapFieldTypeName(string typeName);

		#endregion

		#region SeekRow, SeekField, FetchNextField, FetchArray, FetchObject

		/// <summary>
		/// Moves the internal cursor to the specified row. 
		/// </summary>
		/// <returns>Whether the cursor moved and there are data available.</returns>
		public bool SeekRow(int rowIndex)
		{
			if (!CheckRowIndex(rowIndex)) return false;
			currentRowIndex = rowIndex - 1;
			currentFieldIndex = 0;
			return true;
		}

		/// <summary>
		/// Seeks to a specified field.
		/// </summary>
		/// <param name="fieldIndex">An index of the field.</param>
		/// <returns>Whether the index is in the range.</returns>
		public bool SeekField(int fieldIndex)
		{
			CheckFieldIndex(fieldIndex);
			currentFieldIndex = fieldIndex;
			return true;
		}

		/// <summary>
		/// Advances <see cref="LastFetchedField"/> counter and gets its value.
		/// </summary>
		/// <returns>Index of field to be fetched.</returns>
		public int FetchNextField()
		{
			if (lastFetchedField < FieldCount - 1) lastFetchedField++;
			return lastFetchedField;
		}

		/// <summary>
		/// Moves cursor in internal cache one ahead. Reads data from IDataReader if necessary.
		/// </summary>
		/// <returns>Whether the cursor moved and there are data available.</returns>
		private bool ReadRow()
		{
			if (currentRowIndex < RowCount - 1)
			{
				currentRowIndex++;
				currentFieldIndex = 0;
				return true;
			}

			return false;
		}

		/// <summary>
		/// Returns a PhpArray containing data from collumns in the row and move to the next row.
		/// Returns false if there are no more rows.
		/// </summary>
		/// <param name="intKeys">Whether to add integer keys.</param>
		/// <param name="stringKeys">Whether to add string keys.</param>
		/// <returns>A PHP array containing the data.</returns>
		public PhpArray FetchArray(bool intKeys, bool stringKeys)
		{
			// no more data
			if (!this.ReadRow()) return null;

			Debug.Assert(currentRowIndex >= 0 && currentRowIndex < RowCount);

			ScriptContext context = ScriptContext.CurrentContext;
			object[] oa = (object[])CurrentSet.Rows[currentRowIndex];
			PhpArray row = new PhpArray((intKeys) ? FieldCount : 0, (stringKeys) ? FieldCount : 0);

			for (int i = 0; i < FieldCount; i++)
			{
				object quoted = Core.Convert.Quote(oa[i], context);
				if (intKeys) row.Add(i, quoted);
                if (stringKeys) row[CurrentSet.Names[i]] = quoted;
			}

			return row;
		}

		/// <summary>
		/// A <see cref="DObject"/> with properties that correspond to the fetched row, 
		/// or false if there are no more rows. 
		/// </summary>
		/// <returns></returns>
		/// <remarks>
		/// Works like FetchArray but instead of storing data to associative array,
		/// FetchObject use object fields. Note, that field names are case sensitive.
		/// </remarks>
		public PhpObject FetchObject()
		{
			// no more data
			if (!this.ReadRow()) return null;

			Debug.Assert(currentRowIndex >= 0 && currentRowIndex < RowCount);

			object[] oa = (object[])CurrentSet.Rows[currentRowIndex];
			var runtimeFields = new PhpArray(FieldCount);
			for (int i = 0; i < FieldCount; i++)
			{
                runtimeFields[CurrentSet.Names[i]] = oa[i];
				//php_object.SetProperty(CurrentSet.Names[i], oa[i], null);
			}

            return new stdClass()
            {
                RuntimeFields = runtimeFields
            };            
		}

		#endregion

		#region GetSchemaTable, GetSchemaRowInfo, GetFieldName, GetFieldType, GetFieldLength, GetFieldValue

		private ArrayList schemaTables = null;       // GENERICS: List<DataTable>

		/// <summary>
		/// Gets information about schema of the current result set.
		/// </summary>
		/// <returns>Schema table.</returns>
		public DataTable GetSchemaTable()
		{
			// loads schema if not loaded yet:
			if (schemaTables == null)
			{
				connection.ReexecuteSchemaQuery(this);
				if (reader.IsClosed)
				{
					PhpException.Throw(PhpError.Warning, LibResources.GetString("cannot_retrieve_schema"));
					return null;
				}

				schemaTables = new ArrayList();
				do
				{
					schemaTables.Add(reader.GetSchemaTable());
				}
				while (reader.NextResult());
			}

			return (DataTable)schemaTables[currentSetIndex];
		}

		/// <summary>
		/// Gets schema information for a specified field.
		/// </summary>
		/// <param name="fieldIndex">Field index.</param>
		/// <returns>Data row containing column schema.</returns>
		public DataRow GetSchemaRowInfo(int fieldIndex)
		{
			if (!CheckFieldIndex(fieldIndex)) return null;
			return GetSchemaTable().Rows[fieldIndex];
		}

		/// <summary>
		/// Gets a name of the current field.
		/// </summary>
		/// <returns>The field name.</returns>
		public string GetFieldName()
		{
			return GetFieldName(currentFieldIndex);
		}

		/// <summary>
		/// Gets a name of a specified field.
		/// </summary>
		/// <param name="fieldIndex">An index of the field.</param>
		/// <returns>The field name or a <B>null</B> reference if index is out of range.</returns>
		public string GetFieldName(int fieldIndex)
		{
			if (!CheckFieldIndex(fieldIndex)) return null;
			return CurrentSet.Names[fieldIndex];
		}

		/// <summary>
		/// Gets a type of the current field.
		/// </summary>
		/// <returns>The type name.</returns>
		public string GetFieldType()
		{
			return GetFieldType(currentFieldIndex);
		}

		/// <summary>
		/// Gets a PHP name of the current field type.
		/// </summary>
		/// <returns>PHP type name.</returns>
		public string GetPhpFieldType()
		{
			return MapFieldTypeName(GetFieldType());
		}

		/// <summary>
		/// Gets a PHP name of a specified field type.
		/// </summary>
		/// <param name="fieldIndex">Field index.</param>
		/// <returns>PHP type name.</returns>
		public string GetPhpFieldType(int fieldIndex)
		{
			return MapFieldTypeName(GetFieldType(fieldIndex));
		}

		/// <summary>
		/// Gets a type of specified field.
		/// </summary>
		/// <param name="fieldIndex">An index of the field.</param>
		/// <returns>The type name.</returns>
		public string GetFieldType(int fieldIndex)
		{
			if (!CheckFieldIndex(fieldIndex)) return null;
			return CurrentSet.DataTypes[fieldIndex];
		}

		/// <summary>
		/// Gets length of the current field.
		/// </summary>
		/// <returns>The field length.</returns>
		public virtual int GetFieldLength()
		{
			return GetFieldLength(currentFieldIndex);
		}

		/// <summary>
		/// Gets length of a specified field.
		/// </summary>
		/// <param name="fieldIndex">An index of the field.</param>
		/// <returns>The field length or 0.</returns>
		public virtual int GetFieldLength(int fieldIndex)
		{
			DataRow info = GetSchemaRowInfo(fieldIndex);
			return (info != null) ? (int)info["ColumnSize"] : 0;
		}

		/// <summary>
		/// Gets a value of a specified field of the result.
		/// </summary>
		/// <param name="rowIndex">Row index.</param>
		/// <param name="fieldName">Name of the field.</param>
		/// <returns>The value or a <B>null</B> reference if row or index are out of range.</returns>
		public object GetFieldValue(int rowIndex, string fieldName)
		{
			if (!CheckRowIndex(rowIndex)) return false;

			for (int i = 0; i < CurrentSet.Names.Length; i++)
			{
				if (String.Compare(CurrentSet.Names[i], fieldName, true) == 0)
					return ((object[])CurrentSet.Rows[rowIndex])[i];
			}

			PhpException.Throw(PhpError.Notice, LibResources.GetString("field_not_exists", fieldName));
			return null;
		}

		/// <summary>
		/// Gets a value of a specified field of the result.
		/// </summary>
		/// <param name="rowIndex">Row index.</param>
		/// <param name="fieldIndex">Index of the field.</param>
		/// <returns>The value or a <B>null</B> reference if row or index are out of range.</returns>
		public object GetFieldValue(int rowIndex, int fieldIndex)
		{
			if (!CheckRowIndex(rowIndex) || !CheckFieldIndex(fieldIndex)) return null;
			return ((object[])CurrentSet.Rows[rowIndex])[fieldIndex];
		}

        /// <summary>
        /// Get custom data associated with current set.
        /// </summary>
        /// <returns></returns>
        public object GetRowCustomData()
        {
            return CurrentSet.CustomData;
        }

		#endregion

		#region Checks

		/// <summary>
		/// Checks whether a field index is valid for the current result set.
		/// </summary>
		/// <param name="fieldIndex">Field index to check.</param>
		/// <returns>Whether the index is in the range [0, <see cref="FieldCount"/>).</returns>
		/// <exception cref="PhpException">Invalid field index (Warning).</exception>
		public bool CheckFieldIndex(int fieldIndex)
		{
			if (fieldIndex < 0 || fieldIndex >= FieldCount)
			{
				PhpException.Throw(PhpError.Warning, LibResources.GetString("invalid_data_result_field_index",
						fieldIndex, this.TypeName, this.Id));

				return false;
			}

			return true;
		}

		/// <summary>
		/// Checks whether a row index is valid for the current result set.
		/// </summary>
		/// <param name="rowIndex">Row index to check.</param>
		/// <returns>Whether the index is in the range [0, <see cref="RowCount"/>).</returns>
		/// <exception cref="PhpException">Invalid row index (Warning).</exception>
		public bool CheckRowIndex(int rowIndex)
		{
			if (rowIndex < 0 || rowIndex >= RowCount)
			{
				PhpException.Throw(PhpError.Warning, LibResources.GetString("invalid_data_result_row_index",
				  rowIndex, this.TypeName, this.Id));
				return false;
			}

			return true;
		}

		#endregion
	}
}