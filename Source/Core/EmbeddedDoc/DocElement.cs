using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace PHP.Core.EmbeddedDoc
{
	#region DocElementType
	/// <summary>
	/// Specifies the type of a documentation element (or section in other words).
	/// </summary>
	public enum DocElementType
	{
		/// <summary>
		/// Unknown documentation element. 
		/// </summary>
		Unknown,

		/// <summary>
		/// Represents "@abstract" documentation element, which specifies that class or method is
		/// abstract, in the meaning that it should be redefined in non-abstract derived class.
		/// </summary>
		Abstract,

		/// <summary>
		/// Represents "@access" documentation element which specifies the visibility of language
		/// element's documentation - public access means that element's documentation is always 
		/// exported, on the other hand private access specifies that element's documentation
		/// should not be exported.
		/// </summary>
		Access,

		/// <summary>
		/// Represents "@author" documentation element which specifies the author of a language 
		/// element.
		/// </summary>
		Author,

		/// <summary>
		/// Represents "@category" documentation element which specifies the category of a package.
		/// </summary>
		Category,

		/// <summary>
		/// Represents "@copyright" documentation element which specifies the copyright information
		/// of a language element.
		/// </summary>
		Copyright,

		/// <summary>
		/// Represents "@deprecated" documentation element which specifies deprecated declaration which
		/// should not be used anymore.
		/// </summary>
		Deprecated,

		/// <summary>
		/// Represents "@example" documentation element which specifies an example code section.
		/// </summary>
		Example,

		/// <summary>
		/// Represents "@final" documentation element which specifies that a method is final and should
		/// not be furtherly overridden by derived classes.
		/// </summary>
		Final,

		/// <summary>
		/// Represents "@filesource" documentation element which specifies that the whole source file
		/// should be included in the documentation.
		/// </summary>
		FileSource,

		/// <summary>
		/// Represents "@global" documentation element which specifies a) global variable declaration
		/// detailing its type and declaration and b) global variable reference in a method or function
		/// using global statement detailing variable's type and description.
		/// </summary>
		Global,

		/// <summary>
		/// Represents "@ignore" documentation element which specifies that the language element should 
		/// not be documented (usually used on duplicit declarations).
		/// </summary>
		Ignore,

		/// <summary>
		/// Represents "@internal" documentation element which specifing that documentation of the language
		/// element should not be included in public documentation.
		/// </summary>
		Internal,

		/// <summary>
		/// Represents "@license" documentation element which specifies URL to a license associated with the
		/// language element.
		/// </summary>
		Licence,

		/// <summary>
		/// Represents "@link" documentation element which specifies a link associated with the language
		/// element.
		/// </summary>
		Link,

		/// <summary>
		/// Represents "@method" documentation element which specifies documentation of a "magic method"
		/// inside a class.
		/// </summary>
		Method,

		/// <summary>
		/// Represents "@name" documentation element which names a global variable's name to make it more
		/// comprehensible.
		/// </summary>
		Name,

		/// <summary>
		/// Represents "@package" documentation element which specifies a package association of a page (all 
		/// functions, defines, includes and requires) or class (and its elements).
		/// </summary>
		Package,

		/// <summary>
		/// Represents "@param" documentation element, which specifies the type and description of a function 
		/// or method parameter.
		/// </summary>
		Param,

		/// <summary>
		/// Represents "@property", "@property-read" and "@property-write" documentation elements, which
		/// specify read/write "magical properties" of the class.
		/// </summary>
		Property,

		/// <summary>
		/// Represents "@return" documentation element, which specifies return type of a method or function.
		/// </summary>
		Return,

		/// <summary>
		/// Represents "@see" documentation element, which specifies a link to an another documented element
		/// under a "see" header.
		/// </summary>
		See,

		/// <summary>
		/// Represents "@since" documentation element, which specifies a release version of a documented element.
		/// </summary>
		Since,

		/// <summary>
		/// Represents "@static" documentation element, which specifies that a variable or a method in the class
		/// is static.
		/// </summary>
		Static,

		/// <summary>
		/// Represents "@staticvar" documentation element, which specifies a type and description of a static
		/// variable inside a function or method.
		/// </summary>
		Staticvar,

		/// <summary>
		/// TODO
		/// </summary>
		Subpackage,

		/// <summary>
		/// Represents a beginning documentation element.
		/// </summary>
		Summary,

		/// <summary>
		/// TODO
		/// </summary>
		Todo,

		/// <summary>
		/// TODO
		/// </summary>
		Tutorial,

		/// <summary>
		/// TODO
		/// </summary>
		Uses,

		/// <summary>
		/// TODO
		/// </summary>
		Var,

		/// <summary>
		/// TODO
		/// </summary>
		Version
	}
	#endregion

	#region DocElementSet
	/// <summary>
	/// A set of documentation elements (i.e. @param or @example).
	/// </summary>
	public class DocElementSet : IEnumerable<DocElement>
	{
		private DocElement[] elements;
		private DocElementType elementType;

		private static DocElementSet empty = new DocElementSet(new DocElement[0], DocElementType.Unknown);
		public static DocElementSet Empty
		{
			get { return empty; }
		}

		/// <summary>
		/// Contructs a new element set.
		/// </summary>
		/// <param name="elements">A collection of </param>
		/// <param name="elementType"></param>
		internal DocElementSet(ICollection<DocElement>/*!*/ elements, DocElementType elementType)
		{
			this.elements = new DocElement[elements.Count];
			elements.CopyTo(this.elements, 0);
			this.elementType = elementType;
		}

		/// <summary>
		/// Returns the last element in the element set.
		/// </summary>
		/// <returns></returns>
		public DocElement GetSingleElement()
		{
			return elements[elements.Length - 1];
		}

		/// <summary>
		/// Returns a collection containing the elements in this set.
		/// </summary>
		/// <returns></returns>
		public DocElement[] GetElements()
		{
			return elements;
		}

		/// <summary>
		/// Returns the last element in the element set, casted to the specified type.
		/// </summary>
		/// <returns></returns>
		public T GetSingleElement<T>() where T : DocElement
		{
			if (elements == null || elements.Length == 0) return default(T);
			return (T)elements[elements.Length - 1];
		}


		/// <summary>
		/// Returns a collection containing the elements in this set, casted to the specified type.
		/// </summary>
		/// <returns></returns>
		public T[]/*!*/GetElements<T>() where T : DocElement
		{
			T[] tmp = new T[elements.Length];
			for (int i = 0; i < elements.Length; i++)
				tmp[i] = (T)elements[i];
			return tmp;
		}

		/// <summary>
		/// Returns an enumerator of elements contained in this set.
		/// </summary>
		/// <returns></returns>
		public IEnumerator<DocElement> GetEnumerator()
		{
			throw new NotImplementedException();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
	#endregion

	#region DocElement
	/// <summary>
	/// Base class for elements of an embedded documentation block (i.e. @param or @example).
	/// </summary>
	public abstract class DocElement
	{
		private DocAttribute[] attributes;

		internal DocElement(ICollection<DocAttribute> attributes)
		{
			this.attributes = new DocAttribute[attributes.Count];
			attributes.CopyTo(this.attributes, 0);
		}

		/// <summary>
		/// Gets an attribute with a specified index.
		/// </summary>
		/// <param name="index">Index of an attribute.</param>
		/// <returns>An attribute with specified index</returns>
		public DocAttribute this[int index]
		{
			get
			{
				return attributes[index];
			}
		}

		/// <summary>
		/// Gets a first attribute with specified type.
		/// </summary>
		/// <typeparam name="T">Type of an attribute.</typeparam>
		/// <returns>
		/// A first <see cref="DocAttribute"/> contained within the element, which is of type <typeparamref name="T"/>.
		/// The result is of type <typeparamref name="T"/>. Or null if requested value is not present in the element.
		/// </returns>
		public T Get<T>() where T : DocAttribute
		{
			foreach(DocAttribute attr in attributes)
			{
				if (attr is T) return attr as T;
			}

			return null;
		}

		/// <summary>
		/// Gets an attribute with specified index casted to the 
		/// </summary>
		/// <typeparam name="T">Type of an attribute.</typeparam>
		/// <param name="index">Index of an attribute.</param>
		/// <returns>
		/// An attribute with specified index casted to the specified type or null if requested
		/// value is not present in the element.
		/// </returns>
		public T Get<T>(int index) where T : DocAttribute
		{
			return attributes[index] as T;
		}

		/// <summary>
		/// Gets the count of attributes of this <see cref="DocElement" />.
		/// </summary>
		public int Count { get { return attributes.Length; } }

		/// <summary>
		/// Gets an element's type.
		/// </summary>
		/// <returns></returns>
		public abstract DocElementType GetElementType();
	}
	#endregion

	#region DocSummaryElement
	/// <summary>
	/// 
	/// </summary>
	public class DocSummaryElement : DocElement
	{
		public DocCompoundAttribute Description { get { return Get<DocCompoundAttribute>(0); } }

		public DocSummaryElement(DocCompoundAttribute description)
			: base(new List<DocAttribute>(new DocAttribute[] { description }))
		{
		}

		public override DocElementType GetElementType()
		{
			return DocElementType.Summary;
		}
	}
	#endregion

	#region DocParamElement
	/// <summary>
	/// 
	/// </summary>
	public class DocParamElement : DocElement
	{
		public DocTypeAttribute Type { get { return Get<DocTypeAttribute>(0); } }
		public DocIdentifierAttribute Identifier { get { return Get<DocIdentifierAttribute>(1); } }
		public DocCompoundAttribute Description { get { return Get<DocCompoundAttribute>(2); } }

		public DocParamElement(DocTypeAttribute type, DocIdentifierAttribute identifier, DocCompoundAttribute description)
			: base(new List<DocAttribute>(new DocAttribute[] { type, identifier, description }))
		{
		}

		public override DocElementType GetElementType()
		{
			return DocElementType.Param;
		}
	}
	#endregion

	#region DocReturnElement
	public class DocReturnElement : DocElement
	{
		public DocTypeAttribute Type { get { return Get<DocTypeAttribute>(0); } }
		public DocCompoundAttribute Description { get { return Get<DocCompoundAttribute>(1); } }

		public DocReturnElement(DocTypeAttribute type, DocCompoundAttribute description)
			: base(new List<DocAttribute>(new DocAttribute[] { type, description }))
		{
		}

		public override DocElementType GetElementType()
		{
			return DocElementType.Return;
		}
	}
	#endregion

	#region DocVarElement
	public class DocVarElement : DocElement
	{
		public DocTypeAttribute Type { get { return Get<DocTypeAttribute>(0); } }
		public DocCompoundAttribute Description { get { return Get<DocCompoundAttribute>(1); } }

		public DocVarElement(DocTypeAttribute type, DocCompoundAttribute description)
			: base(new List<DocAttribute>(new DocAttribute[] { type, description }))
		{
		}

		public override DocElementType GetElementType()
		{
			return DocElementType.Var;
		}
	}
	#endregion

	#region DocAccessElement
	public enum DocAccessModifier
	{
		Public,
		Protected,
		Private
	}

	public class DocAccessElement : DocElement
	{
		public DocAccessModifier Modifier { get { return Get<DocAccessModifierAttribute>(0).Modifier; } }

		public DocAccessElement(DocAccessModifierAttribute modifier)
			: base(new List<DocAttribute>(new DocAttribute[] { modifier }))
		{
		}

		public override DocElementType GetElementType()
		{
			return DocElementType.Access;
		}
	}
	#endregion
}
