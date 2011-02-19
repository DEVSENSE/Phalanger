using System;
using System.Collections.Generic;
using System.Text;

namespace PHP.Core.EmbeddedDoc
{
	#region DocAttribute
	/// <summary>
	/// Describes the documentation element's argument.
	/// </summary>
	/// <remarks>
	/// Documentation elements are in the form "@element argument1 argument2 ...".
	/// </remarks>
	public abstract class DocAttribute
	{
	}
	#endregion

	#region DocTypeAttribute
	/// <summary>
	/// Raw data type which is parsed by documentation parser.
	/// </summary>
	public abstract class DocRawType
	{
	}

	/// <summary>
	/// Raw data type identifier as parsed by the documentation parser.
	/// </summary>
	public class DocRawTypeIdentifier : DocRawType
	{
		public string ID { get { return id; } }
		private string id;

		public DocRawTypeIdentifier(string id)
		{
			this.id = id;
		}
	}

	/// <summary>
	/// Raw array data type, optionally with index and value types, as parsed by the documentation parser.
	/// </summary>
	public class DocRawTypeArray : DocRawType
	{
		public DocRawType IndexType { get { return indexType; } }
		private DocRawType indexType;

		public DocRawType ValueType { get { return valueType; } }
		private DocRawType valueType;

		public DocRawTypeArray(DocRawType indexType, DocRawType valueType)
		{
			this.indexType = indexType;
			this.valueType = valueType;
		}
	}

	/// <summary>
	/// Describes the documentation element's type argument.
	/// </summary>
	/// <remarks>
	/// Type attributes can be in following forms:
	///  - single PHP type (int, bool, float, string, object) and PHP array type (array, array[]type, array[type]type)
	///  - multiple PHP types (e.g. "int|string")
	///  - strong .NET type (e.g. "IEnumerable&lt;T&gt;")
	/// </remarks>
	public class DocTypeAttribute : DocAttribute
	{
		/// <summary>
		/// An array of type identifiers. 
		/// </summary>
		/// <remarks>
		/// It is not possible to make some more friendly form prior to analysis of the documentation
		/// in the whole project (or website).
		/// </remarks>
		public DocRawType[] RawDataTypes { get { return rawDataTypes; } }
		DocRawType[] rawDataTypes;

		public DocTypeAttribute(ICollection<DocRawType> dataTypes)
		{
			this.rawDataTypes = new DocRawType[dataTypes.Count];
			dataTypes.CopyTo(this.rawDataTypes, 0);
		}
	}
	#endregion

	#region DocCompoundAttribute
	/// <summary>
	/// Describes the documentation element's compound attribute (which is usually the last attribute).
	/// Comprises of a list of documentation expressions.
	/// </summary>
	public class DocCompoundAttribute : DocAttribute
	{
		/// <summary>
		/// Expressions contained within compound attribute. 
		/// </summary>
		public DocExpression[] Expressions { get { return expressions; } }
		DocExpression[] expressions;

		public DocCompoundAttribute(ICollection<DocExpression> expressions)
		{
			this.expressions = new DocExpression[expressions.Count];
			expressions.CopyTo(this.expressions, 0);
		}
	}
	#endregion

	#region DocIdentifierAttribute
	/// <summary>
	/// An identifier attribute.
	/// </summary>
	public class DocIdentifierAttribute : DocAttribute
	{
		/// <summary>
		/// An identifier not containing the "$" prefix.
		/// </summary>
		public String Identifier { get { return identifier; } }
		String identifier;

		public DocIdentifierAttribute(String identifier)
		{
			this.identifier = identifier;
		}
	}
	#endregion

	#region DocIdentifierAttribute
	/// <summary>
	/// An access modifier attribute.
	/// </summary>
	public class DocAccessModifierAttribute : DocAttribute
	{
		/// <summary>
		/// An access modifier.
		/// </summary>
		public DocAccessModifier Modifier { get { return modifier; } }
		DocAccessModifier modifier;

		public DocAccessModifierAttribute(DocAccessModifier modifier)
		{
			this.modifier = modifier;
		}
	}
	#endregion
}
