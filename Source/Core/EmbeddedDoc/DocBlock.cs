using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace PHP.Core.EmbeddedDoc
{
	#region DocBlock
	/// <summary>
	/// Base class for various documentation blocks. Provides basic functionality for contained elements.
	/// </summary>
	public abstract class DocBlock
	{
		private Dictionary<DocElementType, DocElementSet> elements;

		/// <summary>
		/// Constructs an instance of DocBlock class from given DocElements.
		/// </summary>
		/// <param name="elements">IEnumerable, which contains tuples of element types and elements contained in this block.</param>
		public DocBlock(IEnumerable<Tuple<DocElementType, DocElement>> elements)
		{
			this.elements = new Dictionary<DocElementType, DocElementSet>();

			Dictionary<DocElementType, List<DocElement>> dict = new Dictionary<DocElementType,List<DocElement>>();

			if (elements != null)
			{
				foreach (Tuple<DocElementType, DocElement> element in elements)
				{
                    if (!dict.ContainsKey(element.Item1))
					{
                        dict.Add(element.Item1, new List<DocElement>());
					}

                    dict[element.Item1].Add(element.Item2);
				}
			}

			foreach(DocElementType type in dict.Keys)
			{
				this.elements.Add(type, new DocElementSet(dict[type], type));
			}
		}

		/// <summary>
		/// Gets an element set for specified element type.
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public DocElementSet GetElementSet(DocElementType type)
		{
			if (elements.ContainsKey(type))
			{
				return elements[type];
			}
			else
			{
				return DocElementSet.Empty;
			}
		}

		/// <summary>
		/// Determines whether an element is contained within the documentation block.
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public bool ContainsElement(DocElementType type)
		{			
			return elements.ContainsKey(type);
		}
	}
	#endregion

	#region DocFunctionBlock
	public class DocFunctionBlock : DocBlock
	{
		public DocSummaryElement Summary {	get { return GetElementSet(DocElementType.Summary).GetSingleElement<DocSummaryElement>(); } }
		public DocParamElement[]/*!*/Params { get { return GetElementSet(DocElementType.Param).GetElements<DocParamElement>(); } }
		public DocReturnElement Return { get { return GetElementSet(DocElementType.Return).GetSingleElement<DocReturnElement>(); } }
		public DocAccessElement Access { get { return GetElementSet(DocElementType.Access).GetSingleElement<DocAccessElement>(); } }

		public DocFunctionBlock(IEnumerable<Tuple<DocElementType, DocElement>> elements) : base(elements)
		{
		}
	}
	#endregion

	#region DocMethodBlock
	public class DocMethodBlock : DocFunctionBlock
	{
		/*
		public DocSummaryElement Summary { get { return GetElementSet(DocElementType.Summary).GetSingleElement<DocSummaryElement>(); } }
		public DocParamElement[] Params { get { return GetElementSet(DocElementType.Param).GetElements<DocParamElement>(); } }
		public DocReturnElement Return { get { return GetElementSet(DocElementType.Return).GetSingleElement<DocReturnElement>(); } }
		public DocAccessElement Access { get { return GetElementSet(DocElementType.Access).GetSingleElement<DocAccessElement>(); } }
		*/

		public DocMethodBlock(IEnumerable<Tuple<DocElementType, DocElement>> elements)
			: base(elements)
		{
		}
	}
	#endregion

	#region DocClassBlock
	public class DocClassBlock : DocBlock
	{
		public DocSummaryElement Summary { get { return GetElementSet(DocElementType.Summary).GetSingleElement<DocSummaryElement>(); } }
		public DocAccessElement Access { get { return GetElementSet(DocElementType.Access).GetSingleElement<DocAccessElement>(); } }

		public DocClassBlock(IEnumerable<Tuple<DocElementType, DocElement>> elements)
			: base(elements)
		{
		}
	}
	#endregion

	#region DocVarBlock
	public class DocVarBlock : DocBlock
	{
		public DocSummaryElement Summary { get { return GetElementSet(DocElementType.Summary).GetSingleElement<DocSummaryElement>(); } }
		public DocVarElement Var { get { return GetElementSet(DocElementType.Var).GetSingleElement<DocVarElement>(); } }
		public DocAccessElement Access { get { return GetElementSet(DocElementType.Access).GetSingleElement<DocAccessElement>(); } }

		public DocVarBlock(IEnumerable<Tuple<DocElementType, DocElement>> elements)
			: base(elements)
		{
		}
	}
	#endregion

}
