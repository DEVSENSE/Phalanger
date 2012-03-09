/*

 Copyright (c) 2006 Ladislav Prosek.  

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Xml;
using System.Xml.XPath;
using System.Text;
using System.ComponentModel;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Linq;

using PHP.Core;

namespace PHP.Library.Xml
{
	/// <summary>
	/// DOM XPath implementation.
	/// </summary>
	[ImplementsType]
	public partial class DOMXPath
	{
		#region Fields and Properties

		protected internal XPathNavigator XPathNavigator;
		protected internal XmlNamespaceManager XmlNamespaceManager;

		/// <summary>
		/// Returns the <see cref="DOMDocument"/> associated with this object.
		/// </summary>
		[PhpVisible]
		public object document
		{
			get
			{ return new DOMDocument((XmlDocument)XPathNavigator.UnderlyingObject); }
		}

		#endregion

		#region Construction

		public DOMXPath()
			: base(ScriptContext.CurrentContext, true)
		{ }

		internal DOMXPath(XPathNavigator/*!*/ navigator)
			: base(ScriptContext.CurrentContext, true)
		{
			this.XPathNavigator = navigator;
			InitNamespaceManager(false);
		}

		private void InitNamespaceManager(bool isHtmlDocument)
		{
            this.XmlNamespaceManager = new XmlNamespaceManager(XPathNavigator.NameTable);

            if (isHtmlDocument)
            {
                string ns = XmlNamespaceManager.LookupNamespace("xmlns");
                if (!string.IsNullOrEmpty(ns))
                {
                }
            }
            else
            {
                XPathNodeIterator iterator = XPathNavigator.Select("//namespace::*[not(. = ../../namespace::*)]");

                while (iterator.MoveNext())
                {
                    XmlNamespaceManager.AddNamespace(iterator.Current.Name, iterator.Current.Value);
                }
            }
		}

		[PhpVisible]
		public void __construct(DOMDocument document)
		{
			this.XPathNavigator = document.XmlDocument.CreateNavigator();
            InitNamespaceManager(document._isHtmlDocument);
		}

		#endregion

		#region XPath

		/// <summary>
		/// Registeres the given namespace with the collection of known namespaces.
		/// </summary>
		/// <param name="prefix">The prefix to associate with the namespace being registered.</param>
		/// <param name="uri">The namespace to register.</param>
		/// <returns><B>True</B>.</returns>
		[PhpVisible]
		public object registerNamespace(string prefix, string uri)
		{
			XmlNamespaceManager.AddNamespace(prefix, uri);
			return true;
		}

		/// <summary>
		/// Evaluates the given XPath expression.
		/// </summary>
		/// <param name="expr">The expression to evaluate.</param>
		/// <param name="context">The context node for doing relative XPath queries. By default, the queries are
		/// relative to the root element.</param>
		/// <returns>The <see cref="DOMNodeList"/> containg the result or <B>false</B> on error.</returns>
		[PhpVisible]
		public object query(string expr, [Optional] DOMNode context)
		{
			XPathNavigator navigator = GetNavigator(context);
			if (navigator == null) return false;

			XPathNodeIterator iterator;
			try
			{
				iterator = navigator.Select(expr, XmlNamespaceManager);
			}
			catch (Exception ex)
			{
				DOMException.Throw(ExceptionCode.SyntaxError, ex.Message);
				return false;
			}

			// create the resulting node list
			return IteratorToList(iterator);
		}

		/// <summary>
		/// Evaluates the given XPath expression and returns a typed result if possible.
		/// </summary>
		/// <param name="expr">The expression to evaluate.</param>
		/// <param name="context">The context node for doing relative XPath queries. By default, the queries are
		/// relative to the root element.</param>
		/// <returns>A typed result if possible or a <see cref="DOMNodeList"/> containing all nodes matching the
		/// given <paramref name="expr"/>.</returns>
		[PhpVisible]
		public object evaluate(string expr, [Optional] DOMNode context)
		{
			XPathNavigator navigator = GetNavigator(context);
			if (navigator == null) return false;

			object result;
			try
			{
				result = navigator.Evaluate(expr, XmlNamespaceManager);
			}
			catch (Exception ex)
			{
				DOMException.Throw(ExceptionCode.SyntaxError, ex.Message);
				return false;
			}

			// the result can be bool, double, string, or iterator
			XPathNodeIterator iterator = result as XPathNodeIterator;
			if (iterator != null) return IteratorToList(iterator);
			else return result;
		}

		private XPathNavigator GetNavigator(DOMNode context)
		{
			if (context == null) return XPathNavigator;
			else
			{
				XmlNode node = context.XmlNode;

				if (node.OwnerDocument != (XmlDocument)XPathNavigator.UnderlyingObject)
				{
					DOMException.Throw(ExceptionCode.WrongDocument);
					return null;
				}

				return node.CreateNavigator();
			}
		}

		private DOMNodeList IteratorToList(XPathNodeIterator iterator)
		{
			DOMNodeList list = new DOMNodeList();
		
			while (iterator.MoveNext())
			{
				IHasXmlNode has_node = iterator.Current as IHasXmlNode;
				if (has_node != null)
				{
					IXmlDomNode node = DOMNode.Create(has_node.GetNode());
					if (node != null) list.AppendNode(node);
				}
			}

			return list;
		}

		#endregion
	}
}
