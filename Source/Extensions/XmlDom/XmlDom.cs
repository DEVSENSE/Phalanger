/*

 Copyright (c) 2006 Ladislav Prosek.  

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Xml;
using System.Text;
using System.Globalization;
using System.Collections.Generic;

using PHP.Core;
using PHP.Core.Reflection;

namespace PHP.Library.Xml
{
	#region Constants

	/// <summary>
	/// Enumerates possible DOM node types.
	/// </summary>
	public enum NodeType
	{
		[ImplementsConstant("XML_ELEMENT_NODE")]
		Element = 1,

		[ImplementsConstant("XML_ATTRIBUTE_NODE")]
		Attribute = 2,

		[ImplementsConstant("XML_TEXT_NODE")]
		Text = 3,

		[ImplementsConstant("XML_CDATA_SECTION_NODE")]
		CharacterDataSection = 4,

		[ImplementsConstant("XML_ENTITY_REF_NODE")]
		EntityReference = 5,

		[ImplementsConstant("XML_ENTITY_NODE")]
		Entity = 6,

		[ImplementsConstant("XML_PI_NODE")]
		ProcessingInstruction = 7,

		[ImplementsConstant("XML_COMMENT_NODE")]
		Comment = 8,

		[ImplementsConstant("XML_DOCUMENT_NODE")]
		Document = 9,

		[ImplementsConstant("XML_DOCUMENT_TYPE_NODE")]
		DocumentType = 10,

		[ImplementsConstant("XML_DOCUMENT_FRAG_NODE")]
		DocumentFragment = 11,

		[ImplementsConstant("XML_NOTATION_NODE")]
		Notation = 12,

		[ImplementsConstant("XML_HTML_DOCUMENT_NODE")]
		HtmlDocument = 13,

		[ImplementsConstant("XML_DTD_NODE")]
		Dtd = 14,

		[ImplementsConstant("XML_ELEMENT_DECL_NODE")]
		ElementDecl = 15,

		[ImplementsConstant("XML_ATTRIBUTE_DECL_NODE")]
		AttributeDecl = 16,

		[ImplementsConstant("XML_ENTITY_DECL_NODE")]
		EntityDecl = 17,

		[ImplementsConstant("XML_NAMESPACE_DECL_NODE")]
		NamespaceDecl = 18,

		[ImplementsConstant("XML_LOCAL_NAMESPACE")]
		LocalNamespace = 18
	}

	/// <summary>
	/// Enumerates who-knows-what. (TODO)
	/// </summary>
	public enum AttributeType
	{
		[ImplementsConstant("XML_ATTRIBUTE_CDATA")]
		CharacterData = 1,

		[ImplementsConstant("XML_ATTRIBUTE_ID")]
		Id = 2,

		[ImplementsConstant("XML_ATTRIBUTE_IDREF")]
		IdReference = 3,

		[ImplementsConstant("XML_ATTRIBUTE_IDREFS")]
		IdReferences = 4,

		[ImplementsConstant("XML_ATTRIBUTE_ENTITY")]
		Entity = 5,

		[ImplementsConstant("XML_ATTRIBUTE_NMTOKEN")]
		Token = 7,

		[ImplementsConstant("XML_ATTRIBUTE_NMTOKENS")]
		Tokens = 8,

		[ImplementsConstant("XML_ATTRIBUTE_ENUMERATION")]
		Enumeration = 9,

		[ImplementsConstant("XML_ATTRIBUTE_NOTATION")]
		Notation = 10
	}

	#endregion

	public interface IXmlDomNode
	{
		XmlNode UnderlyingObject { get; }
	}

	/// <summary>
	/// Implements constants and functions.
	/// </summary>
	public static class XmlDom
	{
		/// <summary>
		/// Converts a <see cref="SimpleXMLElement"/> object to a <see cref="DOMElement"/>.
		/// </summary>
		[ImplementsFunction("dom_import_simplexml")]
		public static DObject ImportSimpleXml(DObject node)
		{
			SimpleXMLElement sxe_node = node as SimpleXMLElement;
			if (sxe_node == null)
			{
				PhpException.InvalidImplicitCast(node, "SimpleXMLElement", "node");
				return null;
			}

			return (DObject)DOMNode.Create(sxe_node.XmlElement);
		}

		#region Utilities

		internal static void ParseQualifiedName(string qualifiedName, out string prefix, out string localName)
		{
			if (qualifiedName == null)
			{
				prefix = null;
				localName = null;
			}
			else
			{
				int index = qualifiedName.IndexOf(':');
				if (index >= 0)
				{
					prefix = qualifiedName.Substring(0, index);
					localName = qualifiedName.Substring(index + 1);
				}
				else
				{
					prefix = String.Empty;
					localName = qualifiedName;
				}
			}
		}

		internal static Encoding/*!*/ GetNodeEncoding(XmlNode xmlNode)
		{
			XmlDocument xml_document = xmlNode.OwnerDocument;
			if (xml_document == null) xml_document = (XmlDocument)xmlNode;

			Encoding encoding;

			XmlDeclaration decl = xml_document.FirstChild as XmlDeclaration;
			if (decl != null && !String.IsNullOrEmpty(decl.Encoding))
			{
				encoding = Encoding.GetEncoding(decl.Encoding);
			}
			else encoding = Configuration.Application.Globalization.PageEncoding;

			// no BOM for UTF-8 please!
			if (encoding is UTF8Encoding) return new UTF8Encoding(false);
			else return encoding;
		}

		#endregion
	}
}
