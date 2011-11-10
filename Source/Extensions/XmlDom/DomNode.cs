/*

 Copyright (c) 2006 Ladislav Prosek.  

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Xml;
using System.Xml.Schema;
using System.Text;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using PHP.Core;
using PHP.Core.Reflection;

namespace PHP.Library.Xml
{
	/// <summary>
	/// DOM node.
	/// </summary>
	[ImplementsType]
	public partial class DOMNode : IXmlDomNode
	{
		#region IXmlDomNode Members

		XmlNode IXmlDomNode.UnderlyingObject
		{
			get { return XmlNode; }
		}

		#endregion

		#region Fields and Properties

		private XmlNode _xmlNode;
		protected internal XmlNode XmlNode
		{
			get
			{
				if (_xmlNode == null) DOMException.Throw(ExceptionCode.InvalidState);
				return _xmlNode;
			}
			set
			{
				_xmlNode = value;
			}
		}

		protected internal bool IsAssociated
		{
			get
			{ return (_xmlNode != null); }
		}

		/// <summary>
		/// Returns the name of the node (exact meaning depends on the particular subtype).
		/// </summary>
		[PhpVisible]
		public virtual string nodeName
		{
			get
			{ return XmlNode.Name; }
		}

		/// <summary>
		/// Returns or sets the value of the node (exact meaning depends on the particular subtype).
		/// </summary>
		[PhpVisible]
		public virtual object nodeValue
		{
			get
			{ return XmlNode.Value; }
			set
			{ XmlNode.Value = PHP.Core.Convert.ObjectToString(value); }
		}

		/// <summary>
		/// Returns the type of the node (to be overriden).
		/// </summary>
		[PhpVisible]
		public virtual object nodeType
		{
			get
			{
				if (!IsAssociated) PhpException.Throw(PhpError.Warning, Resources.InvalidStateError);
				else PhpException.Throw(PhpError.Warning, Resources.InvalidNodeType);
				return null;
			}
		}

		/// <summary>
		/// Returns the parent of the node.
		/// </summary>
		[PhpVisible]
		public object parentNode
		{
			get
			{
				if (!IsAssociated && GetType() != typeof(DOMNode)) return null;
				return Create(XmlNode.ParentNode);
			}
		}

		/// <summary>
		/// Returns all children of the node.
		/// </summary>
		[PhpVisible]
		public object childNodes
		{
			get
			{
				DOMNodeList list = new DOMNodeList();
				if (IsAssociated || GetType() == typeof(DOMNode))
				{
					foreach (XmlNode child in XmlNode.ChildNodes)
					{
						IXmlDomNode node = Create(child);
						if (node != null) list.AppendNode(node);
					}
				}
				return list;
			}
		}

		/// <summary>
		/// Returns the first child of the node.
		/// </summary>
		[PhpVisible]
		public object firstChild
		{
			get
			{
				if (!IsAssociated && GetType() != typeof(DOMNode)) return null;
				return Create(XmlNode.FirstChild);
			}
		}

		/// <summary>
		/// Returns the last child of the node.
		/// </summary>
		[PhpVisible]
		public object lastChild
		{
			get
			{
				if (!IsAssociated && GetType() != typeof(DOMNode)) return null;
				return Create(XmlNode.LastChild);
			}
		}

		/// <summary>
		/// Returns the previous sibling of the node.
		/// </summary>
		[PhpVisible]
		public object previousSibling
		{
			get
			{
				if (!IsAssociated && GetType() != typeof(DOMNode)) return null;
				return Create(XmlNode.PreviousSibling);
			}
		}

		/// <summary>
		/// Returns the next sibling of the node.
		/// </summary>
		[PhpVisible]
		public object nextSibling
		{
			get
			{
				if (!IsAssociated && GetType() != typeof(DOMNode)) return null;
				return Create(XmlNode.NextSibling);
			}
		}

		/// <summary>
		/// Returns a map of attributes of this node (overriden in <see cref="DOMElement"/>).
		/// </summary>
		[PhpVisible]
		public virtual object attributes
		{
			get
			{ return null; }
		}

		/// <summary>
		/// This function returns the document the current node belongs to.
		/// </summary>
		[PhpVisible]
		public object ownerDocument
		{
			get
			{ return Create(XmlNode.OwnerDocument); }
		}
		
		/// <summary>
		/// Returns the namespace URI of the node.
		/// </summary>
		[PhpVisible]
		public virtual string namespaceURI
		{
			get
			{
				string uri = XmlNode.NamespaceURI;
				return (uri.Length == 0 ? null : uri);
			}
		}

		/// <summary>
		/// Returns or sets the namespace prefix of the node.
		/// </summary>
		[PhpVisible]
		public object prefix
		{
			get
			{
				if (IsAssociated) return XmlNode.Prefix;
				
				string prefix, local_name;
				XmlDom.ParseQualifiedName(nodeName, out prefix, out local_name);

				return prefix;
			}
			set
			{ XmlNode.Prefix = PHP.Core.Convert.ObjectToString(value); }
		}

		/// <summary>
		/// Returns the local name of the node.
		/// </summary>
		[PhpVisible]
		public string localName
		{
			get
			{
				if (IsAssociated) return XmlNode.LocalName;

				string prefix, local_name;
				XmlDom.ParseQualifiedName(nodeName, out prefix, out local_name);

				return local_name;
			}
		}

		/// <summary>
		/// Returns the base URI of the node.
		/// </summary>
		[PhpVisible]
		public string baseURI
		{
			get
			{
				if (!IsAssociated && GetType() != typeof(DOMNode)) return null;
				return XmlNode.BaseURI;
			}
		}

		/// <summary>
		/// Returns or sets the text content of the node.
		/// </summary>
		[PhpVisible]
		public object textContent
		{
			get
			{ return XmlNode.InnerText; }
			set
			{ XmlNode.InnerText = PHP.Core.Convert.ObjectToString(value); }
		}

		#endregion

		#region Construction

		internal static IXmlDomNode Create(XmlNode xmlNode)
		{
			if (xmlNode == null) return null;
			switch (xmlNode.NodeType)
			{
				case XmlNodeType.Attribute: return new DOMAttr((XmlAttribute)xmlNode);
				case XmlNodeType.SignificantWhitespace:
                case XmlNodeType.Whitespace: return null;// TODO: new DOMText((XmlCharacterData)xmlNode); // also see XmlDocument.PreserveWhitespace
				case XmlNodeType.CDATA: return new DOMCdataSection((XmlCDataSection)xmlNode);
				case XmlNodeType.Comment: return new DOMComment((XmlComment)xmlNode);
				case XmlNodeType.Document: return new DOMDocument((XmlDocument)xmlNode);
				case XmlNodeType.DocumentFragment: return new DOMDocumentFragment((XmlDocumentFragment)xmlNode);
				case XmlNodeType.DocumentType: return new DOMDocumentType((XmlDocumentType)xmlNode);
				case XmlNodeType.Element: return new DOMElement((XmlElement)xmlNode);
				case XmlNodeType.Entity: return new DOMEntity((XmlEntity)xmlNode);
				case XmlNodeType.EntityReference: return new DOMEntityReference((XmlEntityReference)xmlNode);
				case XmlNodeType.Notation: return new DOMNotation((XmlNotation)xmlNode);
				case XmlNodeType.ProcessingInstruction: return new DOMProcessingInstruction((XmlProcessingInstruction)xmlNode);
				case XmlNodeType.Text: return new DOMText((XmlText)xmlNode);
				
				case XmlNodeType.XmlDeclaration:
				default:
					return null;
			}
		}

		protected override DObject CloneObjectInternal(DTypeDesc caller, ScriptContext context, bool deepCopyFields)
		{
			DOMException.Throw(ExceptionCode.InvalidState);
			return null;
		}

		#endregion

		#region Internal dump routine

		private IEnumerable<KeyValuePair<VariableName, AttributedValue>> PropertyIteratorHelper()
		{
			return base.PropertyIterator();
		}

		protected override IEnumerable<KeyValuePair<VariableName, AttributedValue>> PropertyIterator()
		{
			foreach (KeyValuePair<VariableName, AttributedValue> pair in PropertyIteratorHelper())
			{
				// filter out "linking" properties to avoid an endless dump :)
				switch (pair.Key.ToString())
				{
					case "parentNode":
					case "childNodes":
					case "firstChild":
					case "lastChild":
					case "previousSibling":
					case "nextSibling":
					case "ownerDocument":
					case "documentElement": continue;

					default: yield return pair; break;
				}
			}
		}

		#endregion

		#region Hierarchy

		protected internal virtual void Associate(XmlDocument/*!*/ document)
		{ }

		private delegate XmlNode NodeAction(DOMNode/*!*/ newNode, DOMNode auxNode);

		/// <summary>
		/// Performs a child-adding action with error checks.
		/// </summary>
		private XmlNode CheckedChildOperation(DOMNode/*!*/ newNode, DOMNode auxNode, NodeAction/*!*/ action)
		{
			newNode.Associate(XmlNode.OwnerDocument != null ? XmlNode.OwnerDocument : (XmlDocument)XmlNode);

			// check for readonly node
			if (XmlNode.IsReadOnly || (newNode.XmlNode.ParentNode != null && newNode.XmlNode.ParentNode.IsReadOnly))
			{
				DOMException.Throw(ExceptionCode.DomModificationNotAllowed);
				return null;
			}

			// check for owner document mismatch
			if (XmlNode.OwnerDocument != null ?
				XmlNode.OwnerDocument != newNode.XmlNode.OwnerDocument :
				XmlNode != newNode.XmlNode.OwnerDocument)
			{
				DOMException.Throw(ExceptionCode.WrongDocument);
				return null;
			}

			XmlNode result;
			try
			{
				result = action(newNode, auxNode);
			}
			catch (InvalidOperationException)
			{
				// the current node is of a type that does not allow child nodes of the type of the newNode node
				// or the newNode is an ancestor of this node. 
				DOMException.Throw(ExceptionCode.BadHierarchy);
				return null;
			}
			catch (ArgumentException)
			{
				// check for newNode == this which System.Xml reports as ArgumentException
				if (newNode.XmlNode == XmlNode) DOMException.Throw(ExceptionCode.BadHierarchy);
				else
				{
					// the refNode is not a child of this node
					DOMException.Throw(ExceptionCode.NotFound);
				}
				return null;
			}

			return result;
		}

		/// <summary>
		/// Adds a new child before a reference node.
		/// </summary>
		/// <param name="newNode">The new node.</param>
		/// <param name="refNode">The reference node. If not supplied, <paramref name="newNode"/> is appended
		/// to the children.</param>
		/// <returns>The inserted node.</returns>
		[PhpVisible]
		public object insertBefore(DOMNode newNode, [Optional][Nullable] DOMNode refNode)
		{
			bool is_fragment;
			if (newNode is DOMDocumentFragment)
			{
				if (!newNode.IsAssociated || !newNode.XmlNode.HasChildNodes)
				{
					PhpException.Throw(PhpError.Warning, Resources.DocumentFragmentEmpty);
					return false;
				}
				is_fragment = true;
			}
			else is_fragment = false;

			XmlNode result = CheckedChildOperation(newNode, refNode, delegate(DOMNode _newNode, DOMNode _refNode)
			{
				return XmlNode.InsertBefore(_newNode.XmlNode, (_refNode == null ? null : _refNode.XmlNode));
			});

			if (result == null) return false;
			if (is_fragment) return Create(result);
			else return newNode;
		}

		/// <summary>
		/// Replaces a child node.
		/// </summary>
		/// <param name="newNode">The new node.</param>
		/// <param name="oldNode">The old node.</param>
		/// <returns>The inserted node.</returns>
		[PhpVisible]
		public object replaceChild(DOMNode newNode, DOMNode oldNode)
		{
			XmlNode result = CheckedChildOperation(newNode, oldNode, delegate(DOMNode _newNode, DOMNode _oldNode)
			{
				return XmlNode.ReplaceChild(_newNode.XmlNode, _oldNode.XmlNode);
			});

			if (result == null) return false;
			if (newNode is DOMDocumentFragment) return Create(result);
			else return newNode;
		}

		/// <summary>
		/// Adds a new child at the end of the children.
		/// </summary>
		/// <param name="newNode">The node to add.</param>
		/// <returns>The node added.</returns>
		[PhpVisible]
		public object appendChild(DOMNode newNode)
		{
			bool is_fragment;
			if (newNode is DOMDocumentFragment)
			{
				if (!newNode.IsAssociated || !newNode.XmlNode.HasChildNodes)
				{
					PhpException.Throw(PhpError.Warning, Resources.DocumentFragmentEmpty);
					return false;
				}
				is_fragment = true;
			}
			else is_fragment = false;

			XmlNode result = CheckedChildOperation(newNode, null, delegate(DOMNode _newNode, DOMNode _)
			{
				return XmlNode.AppendChild(_newNode.XmlNode);
			});

			if (result == null) return false;
			if (is_fragment) return Create(result);
			else return newNode;
		}

		/// <summary>
		/// Removes a child from the list of children.
		/// </summary>
		/// <param name="oldNode">The node to remove.</param>
		/// <returns>The removed node.</returns>
		[PhpVisible]
		public object removeChild(DOMNode oldNode)
		{
			// check for readonly node
			if (XmlNode.IsReadOnly)
			{
				DOMException.Throw(ExceptionCode.DomModificationNotAllowed);
				return false;
			}

			try
			{
				XmlNode.RemoveChild(oldNode.XmlNode);
			}
			catch (ArgumentException)
			{
				DOMException.Throw(ExceptionCode.NotFound);
				return false;
			}

			return oldNode;
		}

		/// <summary>
		/// Checks if the node has children.
		/// </summary>
		/// <returns><B>True</B> if this node has children, <B>false</B> otherwise.</returns>
		[PhpVisible]
		public object hasChildNodes()
		{
			return XmlNode.HasChildNodes;
		}

		/// <summary>
		/// Checks if the node has attributes.
		/// </summary>
		/// <returns><B>True</B> if this node has attributes, <B>false</B> otherwise.</returns>
		[PhpVisible]
		public object hasAttributes()
		{
			XmlAttributeCollection attrs = XmlNode.Attributes;
			return (attrs != null && attrs.Count > 0);
		}

		#endregion

		#region Namespaces

		/// <summary>
		/// Gets the namespace prefix of the node based on the namespace URI.
		/// </summary>
		/// <param name="namespaceUri">The namespace URI.</param>
		/// <returns>The prefix of the namespace or <B>null</B>.</returns>
		[PhpVisible]
		public object lookupPrefix(string namespaceUri)
		{
			return XmlNode.GetPrefixOfNamespace(namespaceUri);
		}

		/// <summary>
		/// Gets the namespace URI of the node based on the prefix.
		/// </summary>
		/// <param name="prefix">The prefix.</param>
		/// <returns>The namespace URI or <B>null</B>.</returns>
		[PhpVisible]
		public object lookupNamespaceUri(string prefix)
		{
			return XmlNode.GetNamespaceOfPrefix(prefix);
		}

		/// <summary>
		/// Determines whether the given URI is the default namespace.
		/// </summary>
		/// <param name="namespaceUri">The namespace URI.</param>
		/// <returns><B>True</B> or <B>false</B>.</returns>
		[PhpVisible]
		public object isDefaultNamespace(string namespaceUri)
		{
			if (namespaceUri.Length > 0)
			{
				return (XmlNode.GetPrefixOfNamespace(namespaceUri).Length == 0);
			}
			else return false;
		}

		#endregion

		#region Utilities

		/// <summary>
		/// Normalizes the node.
		/// </summary>
		[PhpVisible]
		public void normalize()
		{
			XmlNode.Normalize();
		}

		/// <summary>
		/// Creates a copy of the node.
		/// </summary>
		/// <param name="deep">Indicates whether to copy all descendant nodes. This parameter is
		/// defaulted to <B>false</B>.</param>
		/// <returns>The cloned node.</returns>
		[PhpVisible]
		public object cloneNode([Optional] bool deep)
		{
			if (IsAssociated) return Create(XmlNode.CloneNode(deep));
			else return CloneObjectInternal(null, null, deep);
		}

		/// <summary>
		/// Indicates if two nodes are the same node.
		/// </summary>
		/// <param name="anotherNode">The other node.</param>
		/// <returns><B>True</B> or <B>false</B>.</returns>
		[PhpVisible]
		public object isSameNode(DOMNode anotherNode)
		{
			return (XmlNode == anotherNode.XmlNode);
		}

		/// <summary>
		/// Checks if a feature is supported for the specified version.
		/// </summary>
		/// <param name="feature">The feature to test.</param>
		/// <param name="version">The version number of the <paramref name="feature"/> to test.</param>
		/// <returns><B>True</B> or <B>false</B>.</returns>
		[PhpVisible]
		public object isSupported(string feature, string version)
		{
			return XmlNode.Supports(feature, version);
		}

		#endregion

		#region Not implemented

		/// <summary>
		/// Not implemented in PHP 5.1.6.
		/// </summary>
		[PhpVisible]
		public void getFeature(string feature, string version)
		{
			PhpException.Throw(PhpError.Warning, Resources.NotYetImplemented);
		}

		/// <summary>
		/// Not implemented in PHP 5.1.6.
		/// </summary>
		[PhpVisible]
		public void getUserData(string key)
		{
			PhpException.Throw(PhpError.Warning, Resources.NotYetImplemented);
		}

		/// <summary>
		/// Not implemented in PHP 5.1.6.
		/// </summary>
		[PhpVisible]
		public void setUserData(string key, object data, DOMUserDataHandler handler)
		{
			PhpException.Throw(PhpError.Warning, Resources.NotYetImplemented);
		}

		/// <summary>
		/// Not implemented in PHP 5.1.6.
		/// </summary>
		[PhpVisible]
		public void compareDocumentPosition(DOMNode anotherNode)
		{
			PhpException.Throw(PhpError.Warning, Resources.NotYetImplemented);
		}

		/// <summary>
		/// Not implemented in PHP 5.1.6.
		/// </summary>
		[PhpVisible]
		public void isEqualNode(DOMNode anotherNode)
		{
			PhpException.Throw(PhpError.Warning, Resources.NotYetImplemented);
		}

		#endregion
	}
}
