/*

 Copyright (c) 2006 Ladislav Prosek.  

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.IO;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Schema;
using System.Text;
using System.ComponentModel;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using PHP.Core;

namespace PHP.Library.Xml
{
	/// <summary>
	/// DOM document.
	/// </summary>
	[ImplementsType]
	public partial class DOMDocument : DOMNode
	{
		#region Fields and Properties

		protected internal XmlDocument XmlDocument
		{
			get
			{ return (XmlDocument)XmlNode; }
			set
			{ XmlNode = value; }
		}

		private bool _formatOutput;
		private bool _validateOnParse;
        internal bool _isHtmlDocument;

		/// <summary>
		/// Returns &quot;#document&quot;.
		/// </summary>
		[PhpVisible]
		public override string nodeName
		{
			get
			{ return "#document"; }
		}

		/// <summary>
		/// Returns <B>null</B>.
		/// </summary>
		[PhpVisible]
		public override object nodeValue
		{
			get
			{ return null; }
			set
			{ }
		}

		/// <summary>
		/// Returns the type of the node (<see cref="NodeType.Document"/>).
		/// </summary>
		[PhpVisible]
		public override object nodeType
		{
			get
			{ return (int)NodeType.Document; }
		}

		/// <summary>
		/// Returns the node containing the DOCTYPE declaration.
		/// </summary>
		[PhpVisible]
		public object doctype
		{
			get
			{
				XmlDocumentType doc_type = XmlDocument.DocumentType;
				return (doc_type == null ? null : DOMNode.Create(doc_type));
			}
		}

		/// <summary>
		/// Returns the DOM implementation.
		/// </summary>
		[PhpVisible]
		public object implementation
		{
			get
			{ return new DOMImplementation(); }
		}

		/// <summary>
		/// Returns the root element of this document.
		/// </summary>
		[PhpVisible]
		public object documentElement
		{
			get
			{
				XmlElement root = XmlDocument.DocumentElement;
				return (root == null ? null : DOMNode.Create(root));
			}
		}

		/// <summary>
		/// Returns the encoding of this document.
		/// </summary>
		[PhpVisible]
		public object actualEncoding
		{
			get
			{ return this.encoding; }
		}

		/// <summary>
		/// Returns the encoding of this document.
		/// </summary>
		[PhpVisible]
		public object xmlEncoding
		{
			get
			{ return this.encoding; }
		}

		/// <summary>
		/// Returns or set the encoding of this document.
		/// </summary>
		[PhpVisible]
		public object encoding
		{
			get
			{
				XmlDeclaration decl = GetXmlDeclaration();
				if (decl != null) return decl.Encoding;
				return null;
			}
			set
			{
				string enc = PHP.Core.Convert.ObjectToString(value);

				XmlDeclaration decl = GetXmlDeclaration();
				if (decl != null) decl.Encoding = enc;
				else
				{
					decl = XmlDocument.CreateXmlDeclaration("1.0", enc, null);
					XmlDocument.InsertBefore(decl, XmlDocument.FirstChild);
				}
			}
		}

		/// <summary>
		/// Returns or sets the standalone flag of this document.
		/// </summary>
		[PhpVisible]
		public object xmlStandalone
		{
			get
			{ return this.standalone; }
			set
			{ this.standalone = value; }
		}

		/// <summary>
		/// Returns or sets the standalone flag of this document.
		/// </summary>
		[PhpVisible]
		public object standalone
		{
			get
			{
				XmlDeclaration decl = GetXmlDeclaration();
				return (decl == null || (decl.Standalone != "no"));
			}
			set
			{
				string stand = (PHP.Core.Convert.ObjectToBoolean(value) ? "yes" : "no");

				XmlDeclaration decl = GetXmlDeclaration();
				if (decl != null) decl.Standalone = stand;
				else
				{
					decl = XmlDocument.CreateXmlDeclaration("1.0", null, stand);
					XmlDocument.InsertBefore(decl, XmlDocument.FirstChild);
				}
			}
		}

		/// <summary>
		/// Returns or sets the XML version of this document.
		/// </summary>
		[PhpVisible]
		public object xmlVersion
		{
			get
			{ return this.version; }
			set
			{ this.version = value; }
		}

		/// <summary>
		/// Returns or sets the XML version of this document.
		/// </summary>
		[PhpVisible]
		public object version
		{
			get
			{
				XmlDeclaration decl = GetXmlDeclaration();
				return (decl == null ? "1.0" : decl.Version);
			}
			set
			{
				string ver = PHP.Core.Convert.ObjectToString(value);

				XmlDeclaration decl = GetXmlDeclaration();
				if (decl != null)
				{
					XmlDeclaration new_decl = XmlDocument.CreateXmlDeclaration(ver, decl.Encoding, decl.Standalone);
					XmlDocument.ReplaceChild(new_decl, decl);
				}
				else
				{
					decl = XmlDocument.CreateXmlDeclaration(ver, null, null);
					XmlDocument.InsertBefore(decl, XmlDocument.FirstChild);
				}
			}
		}

		/// <summary>
		/// Returns <B>true</B>.
		/// </summary>
		[PhpVisible]
		public object strictErrorChecking
		{
			get
			{ return true; }
			set
			{ }
		}

		/// <summary>
		/// Returns the base URI of this document.
		/// </summary>
		[PhpVisible]
		public object documentURI
		{
			get
			{ return XmlDocument.BaseURI; }
			set
			{ }
		}

		/// <summary>
		/// Returns <B>null</B>.
		/// </summary>
		[PhpVisible]
		public object config
		{
			get
			{ return null; }
		}

		/// <summary>
		/// Returns or sets whether XML is formatted by <see cref="save(string,int)"/> and <see cref="saveXML(DOMNode)"/>.
		/// </summary>
		[PhpVisible]
		public object formatOutput
		{
			get
			{ return _formatOutput; }
			set
			{ _formatOutput = PHP.Core.Convert.ObjectToBoolean(value); }
		}

		/// <summary>
		/// Returns of sets whether XML is validated against schema by <see cref="load(DOMDocument,string,int)"/> and
		/// <see cref="loadXML(DOMDocument,string,int)"/>.
		/// </summary>
		[PhpVisible]
		public object validateOnParse
		{
			get
			{ return _validateOnParse; }
			set
			{ _validateOnParse = PHP.Core.Convert.ObjectToBoolean(value); }
		}

		/// <summary>
		/// Returns <B>false</B>.
		/// </summary>
		[PhpVisible]
		public object resolveExternals
		{
			get
			{ return false; }
			set
			{ }
		}

		/// <summary>
		/// Returns or sets whether whitespace should be preserved by this XML document.
		/// </summary>
		[PhpVisible]
		public object preserveWhiteSpace
		{
			get
			{ return XmlDocument.PreserveWhitespace; }
			set
			{ XmlDocument.PreserveWhitespace = PHP.Core.Convert.ObjectToBoolean(value); }
		}

		/// <summary>
		/// Returns <B>false</B>.
		/// </summary>
		[PhpVisible]
		public object recover
		{
			get
			{ return false; }
			set
			{ }
		}

		/// <summary>
		/// Returns <B>false</B>.
		/// </summary>
		[PhpVisible]
		public object substituteEntities
		{
			get
			{ return false; }
			set
			{ }
		}

		#endregion

		#region Construction

		public DOMDocument()
			: base(ScriptContext.CurrentContext, true)
		{
			this.XmlDocument = new XmlDocument();
			this.XmlDocument.PreserveWhitespace = true;
		}

		internal DOMDocument(XmlDocument/*!*/ xmlDocument)
			: base(ScriptContext.CurrentContext, true)
		{
			this.XmlDocument = xmlDocument;
		}

		protected override PHP.Core.Reflection.DObject CloneObjectInternal(PHP.Core.Reflection.DTypeDesc caller, ScriptContext context, bool deepCopyFields)
		{
			return new DOMDocument(XmlDocument);
		}

		[PhpVisible]
		public virtual void __construct([Optional] string version, [Optional] string encoding)
		{
			// append the corresponding XML declaration to the document
			if (version == null) version = "1.0";
			XmlDocument.AppendChild(XmlDocument.CreateXmlDeclaration("1.0", encoding, String.Empty));
		}

		#endregion

		#region Node factory

		/// <summary>
		/// Creates an element with the specified name and inner text.
		/// </summary>
		/// <param name="tagName">The qualified name of the element.</param>
		/// <param name="value">The inner text (value) of the element.</param>
		/// <returns>A new <see cref="DOMElement"/>.</returns>
		[PhpVisible]
		public object createElement(string tagName, [Optional] string value)
		{
			XmlElement element = XmlDocument.CreateElement(tagName);
			if (value != null) element.InnerText = value;
			return new DOMElement(element);
		}

		/// <summary>
		/// Creates a new document fragment.
		/// </summary>
		/// <returns>A new <see cref="DOMDocumentFragment"/>.</returns>
		[PhpVisible]
		public object createDocumentFragment()
		{
			XmlDocumentFragment fragment = XmlDocument.CreateDocumentFragment();
			return new DOMDocumentFragment(fragment);
		}

		/// <summary>
		/// Creates a new text node with the specified text.
		/// </summary>
		/// <param name="data">The text for the text node.</param>
		/// <returns>A new <see cref="DOMText"/>.</returns>
		[PhpVisible]
		public object createTextNode(string data)
		{
			XmlText text = XmlDocument.CreateTextNode(data);
			return new DOMText(text);
		}

		/// <summary>
		/// Creates a comment node containing the specified data.
		/// </summary>
		/// <param name="data">The comment data.</param>
		/// <returns>A new <see cref="DOMComment"/>.</returns>
		[PhpVisible]
		public object createComment(string data)
		{
			XmlComment comment = XmlDocument.CreateComment(data);
			return new DOMComment(comment);
		}

		/// <summary>
		/// Creates a CDATA section containing the specified data.
		/// </summary>
		/// <param name="data">The content of the new CDATA section.</param>
		/// <returns>A new <see cref="DOMCdataSection"/>.</returns>
		[PhpVisible]
		public object createCDATASection(string data)
		{
			XmlCDataSection cdata = XmlDocument.CreateCDataSection(data);
			return new DOMCdataSection(cdata);
		}

		/// <summary>
		/// Creates a processing instruction with the specified name and data.
		/// </summary>
		/// <param name="target">The name of the processing instruction.</param>
		/// <param name="data">The data for the processing instruction.</param>
		/// <returns>A new <see cref="DOMProcessingInstruction"/>.</returns>
		[PhpVisible]
		public object createProcessingInstruction(string target, string data)
		{
			XmlProcessingInstruction pi = XmlDocument.CreateProcessingInstruction(target, data);
			return new DOMProcessingInstruction(pi);
		}

		/// <summary>
		/// Creates an attribute with the specified name.
		/// </summary>
		/// <param name="name">The qualified name of the attribute.</param>
		/// <returns>A new <see cref="DOMAttr"/>.</returns>
		[PhpVisible]
		public object createAttribute(string name)
		{
			XmlAttribute attribute = XmlDocument.CreateAttribute(name);
			return new DOMAttr(attribute);
		}

		/// <summary>
		/// Creates an entity reference with the specified name.
		/// </summary>
		/// <param name="name">The name of the entity reference.</param>
		/// <returns>A new <see cref="DOMEntityReference"/>.</returns>
		[PhpVisible]
		public object createEntityReference(string name)
		{
			XmlEntityReference entref = XmlDocument.CreateEntityReference(name);
			return new DOMEntityReference(entref);
		}

		/// <summary>
		/// Creates an element with the specified namespace URI and qualified name.
		/// </summary>
		/// <param name="namespaceUri">The namespace URI of the element.</param>
		/// <param name="qualifiedName">The qualified name of the element.</param>
		/// <param name="value">The inner text (value) of the element.</param>
		/// <returns>A new <see cref="DOMElement"/>.</returns>
		[PhpVisible]
		public object createElementNS(string namespaceUri, string qualifiedName, [Optional] string value)
		{
			XmlElement element = XmlDocument.CreateElement(qualifiedName, namespaceUri);
			if (value != null) element.InnerText = value;
			return new DOMElement(element);
		}

		/// <summary>
		/// Creates an attribute with the specified namespace URI and qualified name.
		/// </summary>
		/// <param name="namespaceUri">The namespace URI of the attribute.</param>
		/// <param name="qualifiedName">The qualified name of the attribute.</param>
		/// <returns>A new <see cref="DOMAttr"/>.</returns>
		[PhpVisible]
		public object createAttributeNS(string namespaceUri, string qualifiedName)
		{
			XmlAttribute attribute = XmlDocument.CreateAttribute(qualifiedName, namespaceUri);
			return new DOMAttr(attribute);
		}

		#endregion

		#region Child elements

		/// <summary>
		/// Gets all descendant elements with the matching tag name.
		/// </summary>
		/// <param name="name">The tag name. Use <B>*</B> to return all elements within the element tree.</param>
		/// <returns>A <see cref="DOMNodeList"/>.</returns>
		[PhpVisible]
		public object getElementsByTagName(string name)
		{
			DOMNodeList list = new DOMNodeList();

			// enumerate elements in the default namespace
			foreach (XmlNode node in XmlDocument.GetElementsByTagName(name))
			{
				IXmlDomNode dom_node = DOMNode.Create(node);
				if (dom_node != null) list.AppendNode(dom_node);
			}

			// enumerate all namespaces
			XPathNavigator navigator = XmlDocument.CreateNavigator();
			XPathNodeIterator iterator = navigator.Select("//namespace::*[not(. = ../../namespace::*)]");

			while (iterator.MoveNext())
			{
				string prefix = iterator.Current.Name;
				if (!String.IsNullOrEmpty(prefix) && prefix != "xml")
				{
					// enumerate elements in this namespace
					foreach (XmlNode node in XmlDocument.GetElementsByTagName(name, iterator.Current.Value))
					{
						IXmlDomNode dom_node = DOMNode.Create(node);
						if (dom_node != null) list.AppendNode(dom_node);
					}
				}
			}

			return list;
		}

		/// <summary>
		/// Gets all descendant elements with the matching namespace URI and local name.
		/// </summary>
		/// <param name="namespaceUri">The namespace URI.</param>
		/// <param name="localName">The local name. Use <B>*</B> to return all elements within the element tree.</param>
		/// <returns>A <see cref="DOMNodeList"/>.</returns>
		[PhpVisible]
		public object getElementsByTagNameNS(string namespaceUri, string localName)
		{
			DOMNodeList list = new DOMNodeList();

			foreach (XmlNode node in XmlDocument.GetElementsByTagName(localName, namespaceUri))
			{
				IXmlDomNode dom_node = DOMNode.Create(node);
				if (dom_node != null) list.AppendNode(dom_node);
			}

			return list;
		}

		/// <summary>
		/// Not yet implemented.
		/// </summary>
		[PhpVisible]
		public void getElementById(string elementId)
		{
			PhpException.Throw(PhpError.Warning, Resources.NotYetImplemented);
		}

		#endregion

		#region Hierarchy

		/// <summary>
		/// Imports a node from another document to the current document.
		/// </summary>
		/// <param name="importedNode">The node being imported.</param>
		/// <param name="deep"><B>True</B> to perform deep clone; otheriwse <B>false</B>.</param>
		/// <returns>The imported <see cref="DOMNode"/>.</returns>
		[PhpVisible]
		public object importNode(DOMNode importedNode, bool deep)
		{
			if (importedNode.IsAssociated)
			{
				return DOMNode.Create(XmlDocument.ImportNode(importedNode.XmlNode, deep));
			}
			else
			{
				importedNode.Associate(XmlDocument);
				return importedNode;
			}
		}

		/// <summary>
		/// Not implemented in PHP 5.1.6.
		/// </summary>
		[PhpVisible]
		public void adoptNode(DOMNode source)
		{
			PhpException.Throw(PhpError.Warning, Resources.NotYetImplemented);
		}

		/// <summary>
		/// Puts the entire XML document into a &quot;normal&quot; form.
		/// </summary>
		[PhpVisible]
		public void normalizeDocument()
		{
			XmlDocument.Normalize();
		}

		/// <summary>
		/// Not implemented in PHP 5.1.6.
		/// </summary>
		[PhpVisible]
		public void renameNode(DOMNode node, string namespaceUri, string qualifiedName)
		{
			PhpException.Throw(PhpError.Warning, Resources.NotYetImplemented);
		}

		private XmlDeclaration GetXmlDeclaration()
		{
			return (XmlNode.FirstChild as XmlDeclaration);
		}

		#endregion

		#region Load and Save

		/// <summary>
		/// Loads the XML document from the specified URL.
		/// </summary>
		/// <param name="instance">The <see cref="DOMDocument"/> instance or <B>null</B>.</param>
		/// <param name="fileName">URL for the file containing the XML document to load.</param>
		/// <param name="options">Undocumented.</param>
		/// <returns>A new <see cref="DOMDocument"/> or <B>false</B> if <paramref name="instance"/>p
		/// is <B>null</B>, <B>true</B> or <B>false</B> otherwise.</returns>
		[PhpVisible]
		public static object load([This] DOMDocument instance, string fileName, [Optional] int options)
		{
			// this method can be called both statically and via an instance
			bool static_call;
			if (instance == null)
			{
				static_call = true;
				instance = new DOMDocument();
			}
			else static_call = false;

            instance._isHtmlDocument = false;

			using (PhpStream stream = PhpStream.Open(fileName, "rt"))
			{
				if (stream == null) return false;

				try
				{
					if (instance._validateOnParse)
					{
						// create a validating XML reader
						XmlReaderSettings settings = new XmlReaderSettings();
#pragma warning disable 618
						settings.ValidationType = ValidationType.Auto;
#pragma warning restore 618

						instance.XmlDocument.Load(XmlReader.Create(stream.RawStream, settings));
					}
					else instance.XmlDocument.Load(stream.RawStream);
				}
				catch (XmlException e)
				{
                    PhpLibXml.IssueXmlError(new PhpLibXml.XmlError(PhpLibXml.LIBXML_ERR_ERROR, 0, 0, 0, e.Message, fileName));
					return false;
				}
				catch (IOException e)
				{
                    PhpLibXml.IssueXmlError(new PhpLibXml.XmlError(PhpLibXml.LIBXML_ERR_ERROR, 0, 0, 0, e.Message, fileName));
					return false;
				}
			}

			return (static_call ? instance : (object)true);
		}

		/// <summary>
		/// Loads the XML document from the specified string.
		/// </summary>
		/// <param name="instance">The <see cref="DOMDocument"/> instance or <B>null</B>.</param>
		/// <param name="xmlString">The XML string.</param>
		/// <param name="options">Undocumented.</param>
		/// <returns>A new <see cref="DOMDocument"/> or <B>false</B> if <paramref name="instance"/>p
		/// is <B>null</B>, <B>true</B> or <B>false</B> otherwise.</returns>
		[PhpVisible]
		public static object loadXML([This] DOMDocument instance, string xmlString, [Optional] int options)
		{
			// this method can be called both statically and via an instance
			bool static_call;
			if (instance == null)
			{
				static_call = true;
				instance = new DOMDocument();
			}
			else static_call = false;

            var result = instance.loadXMLInternal(xmlString, options, false);
            return static_call ? instance : (object)result;
		}

        /// <summary>
        /// Loads provided XML string into this <see cref="DOMDocument"/>.
        /// </summary>
        /// <param name="xmlString">String representing XML document.</param>
        /// <param name="options">PHP options.</param>
        /// <param name="isHtml">Whether the <paramref name="xmlString"/> represents XML generated from HTML document (then it may contain some invalid XML characters).</param>
        /// <returns></returns>
        private bool loadXMLInternal(string xmlString, int options, bool isHtml)
        {
            this._isHtmlDocument = isHtml;

            var stream = new StringReader(xmlString);

            try
			{
                XmlReaderSettings settings = new XmlReaderSettings();

                // validating XML reader
                if (this._validateOnParse)
#pragma warning disable 618
                    settings.ValidationType = ValidationType.Auto;
#pragma warning restore 618

                // do not check invalid characters in HTML (XML)
                if (isHtml)
                    settings.CheckCharacters = false;

                // load the document
                this.XmlDocument.Load(XmlReader.Create(stream, settings));

                // done
                return true;
			}
			catch (XmlException e)
			{
                PhpLibXml.IssueXmlError(new PhpLibXml.XmlError(PhpLibXml.LIBXML_ERR_ERROR, 0, 0, 0, e.Message, null));
				return false;
			}
			catch (IOException e)
			{
                PhpLibXml.IssueXmlError(new PhpLibXml.XmlError(PhpLibXml.LIBXML_ERR_ERROR, 0, 0, 0, e.Message, null));
				return false;
			}
        }
		
		/// <summary>
		/// Saves the XML document to the specified stream.
		/// </summary>
		/// <param name="fileName">The location of the file where the document should be saved.</param>
		/// <param name="options">Unsupported.</param>
		/// <returns>The number of bytes written or <B>false</B> on error.</returns>
		[PhpVisible]
		public object save(string fileName, [Optional] int options)
		{
			using (PhpStream stream = PhpStream.Open(fileName, "wt"))
			{
				if (stream == null) return false;

				try
				{
					// direct stream write indents
					if (_formatOutput) XmlDocument.Save(stream.RawStream);
					else
					{
						Encoding encoding = XmlDom.GetNodeEncoding(XmlNode);

						using (XmlTextWriter writer = new XmlTextWriter(stream.RawStream, encoding))
						{
							XmlDocument.Save(writer);
						}
					}
				}
				catch (XmlException e)
				{
                    PhpLibXml.IssueXmlError(new PhpLibXml.XmlError(PhpLibXml.LIBXML_ERR_ERROR, 0, 0, 0, e.Message, fileName));
					return null;
				}
				catch (IOException e)
				{
                    PhpLibXml.IssueXmlError(new PhpLibXml.XmlError(PhpLibXml.LIBXML_ERR_ERROR, 0, 0, 0, e.Message, fileName));
					return false;
				}

				// TODO:
				return (stream.RawStream.CanSeek ? stream.RawStream.Position : 1);
			}
		}
		
		/// <summary>
		/// Returns the string representation of this document.
		/// </summary>
		/// <param name="node">The node to dump (the entire document if <B>null</B>).</param>
		/// <returns>The string representation of the document / the specified node or <B>false</B>.</returns>
		[PhpVisible]
		public object saveXML([Optional] DOMNode node)
		{
			XmlNode xml_node;

			if (node == null) xml_node = XmlNode;
			else
			{
				xml_node = node.XmlNode;
				if (xml_node.OwnerDocument != XmlDocument && xml_node != XmlNode)
				{
					DOMException.Throw(ExceptionCode.WrongDocument);
					return false;
				}
			}

			// determine output encoding
			Encoding encoding = XmlDom.GetNodeEncoding(xml_node);

			using (MemoryStream stream = new MemoryStream())
			{
				// use a XML writer and set its Formatting proprty to Formatting.Indented
				using (XmlTextWriter writer = new XmlTextWriter(stream, encoding))
				{
					writer.Formatting = (_formatOutput ? Formatting.Indented : Formatting.None);
					xml_node.WriteTo(writer);
				}

				return new PhpBytes(stream.ToArray());
			}
		}

        /// <summary>
        /// Processes HTML errors, if any.
        /// </summary>
        /// <param name="htmlDoc"><see cref="HtmlAgilityPack.HtmlDocument"/> instance to process errors from.</param>
        /// <param name="filename">HTML file name or <c>null</c> if HTML has been loaded from a string.</param>
        private void CheckHtmlErrors(HtmlAgilityPack.HtmlDocument/*!*/htmlDoc, string filename)
        {
            Debug.Assert(htmlDoc != null);

            foreach (var error in htmlDoc.ParseErrors)
            {
                switch (error.Code)
                {
                    case HtmlAgilityPack.HtmlParseErrorCode.EndTagNotRequired:
                    case HtmlAgilityPack.HtmlParseErrorCode.TagNotOpened:
                        break;
                    default:
                        PhpLibXml.IssueXmlError(new PhpLibXml.XmlError(PhpLibXml.LIBXML_ERR_ERROR, 0, error.Line, error.LinePosition, "(" + error.Code.ToString() + ")" + error.Reason, filename));
                        break;
                }
            }
        }

		/// <summary>
        /// Loads HTML from a string.
		/// </summary>
        /// <param name="source">
        /// String containing HTML document.
        /// </param>
		[PhpVisible]
        public object loadHTML(string source)
		{
            if (string.IsNullOrEmpty(source))
                return false;

            return loadHTML(new StringReader(source), null);
		}

        /// <summary>
        /// Loads HTML from a file.
        /// </summary>
        /// <param name="sourceFile">
        /// Path to a file containing HTML document.
        /// </param>
		[PhpVisible]
		public object loadHTMLFile(string sourceFile)
		{
            using (PhpStream stream = PhpStream.Open(sourceFile, "rt"))
            {
                if (stream == null) return false;

                return loadHTML(new StreamReader(stream.RawStream), sourceFile);
            }
		}

        /// <summary>
        /// Load HTML DOM from given <paramref name="stream"/>.
        /// </summary>
        private object loadHTML(TextReader stream, string filename)
        {
            HtmlAgilityPack.HtmlDocument htmlDoc = new HtmlAgilityPack.HtmlDocument();

            // setup HTML parser
            htmlDoc.OptionOutputAsXml = true;
            //htmlDoc.OptionOutputOriginalCase = true;  // NOTE: we need lower-cased names because of XPath queries
            //htmlDoc.OptionFixNestedTags = true;
            htmlDoc.OptionCheckSyntax = false;
            htmlDoc.OptionWriteEmptyNodes = true;
            
            // load HTML (from string or a stream)
            htmlDoc.Load(stream);

            CheckHtmlErrors(htmlDoc, filename);

            // save to string as XML
            using (StringWriter sw = new StringWriter())
            {
                htmlDoc.Save(sw);

                // load as XML
                return this.loadXMLInternal(sw.ToString(), 0, true);
            }
        }

		/// <summary>
		/// Not implemented (TODO: need an HTML parser for this).
		/// </summary>
		[PhpVisible]
		public object saveHTML()
		{
            //TODO: use the HTML parse to same HTML
            return saveXML(null);
		}

		/// <summary>
		/// Not implemented (TODO: need an HTML parser for this).
		/// </summary>
		[PhpVisible]
        public object saveHTMLFile(string file)
        {            
            //TODO: use the HTML parse to same HTML
            return save(file, 0);
		}

		#endregion

		#region XInclude

		/// <summary>
		/// Not implemented (TODO: need a XInclude implementation for this).
		/// </summary>
		[PhpVisible]
		public void xinclude([Optional] int options)
		{
			PhpException.Throw(PhpError.Warning, Resources.NotYetImplemented);
		}

		#endregion

		#region Validation

		/// <summary>
		/// Not implemented (System.Xml does not support post-load DTD validation).
		/// </summary>
		[PhpVisible]
		public void validate()
		{
			PhpException.Throw(PhpError.Warning, Resources.PostLoadDtdUnsupported);
		}

		/// <summary>
		/// Validates the document against the specified XML schema.
		/// </summary>
		/// <param name="schemaFile">URL for the file containing the XML schema to load.</param>
		/// <returns><B>True</B> or <B>false</B>.</returns>
		[PhpVisible]
		public object schemaValidate(string schemaFile)
		{
			XmlSchema schema;

			using (PhpStream stream = PhpStream.Open(schemaFile, "rt"))
			{
				if (stream == null) return false;

				try
				{
					schema = XmlSchema.Read(stream.RawStream, null);
				}
				catch (XmlException e)
				{
                    PhpLibXml.IssueXmlError(new PhpLibXml.XmlError(PhpLibXml.LIBXML_ERR_WARNING, 0, 0, 0, e.Message, schemaFile));
					return false;
				}
				catch (IOException e)
				{
					PhpLibXml.IssueXmlError(new PhpLibXml.XmlError(PhpLibXml.LIBXML_ERR_ERROR, 0, 0, 0, e.Message, schemaFile));
                    return false;
				}
			}

			XmlDocument.Schemas.Add(schema);
			try
			{
				XmlDocument.Validate(null);
			}
			catch (XmlException)
			{
				return false;
			}
			finally
			{
				XmlDocument.Schemas.Remove(schema);
			}
			return true;
		}

		/// <summary>
		/// Validates the document against the specified XML schema.
		/// </summary>
		/// <param name="schemaString">The XML schema string.</param>
		/// <returns><B>True</B> or <B>false</B>.</returns>
		[PhpVisible]
		public object schemaValidateSource(string schemaString)
		{
			XmlSchema schema;

			try
			{
				schema = XmlSchema.Read(new System.IO.StringReader(schemaString), null);
			}
			catch (XmlException e)
			{
                PhpLibXml.IssueXmlError(new PhpLibXml.XmlError(PhpLibXml.LIBXML_ERR_WARNING, 0, 0, 0, e.Message, null));
				return false;
			}

			XmlDocument.Schemas.Add(schema);
			try
			{
				XmlDocument.Validate(null);
			}
			catch (XmlException)
			{
				return false;
			}
			finally
			{
				XmlDocument.Schemas.Remove(schema);
			}
			return true;
		}

		/// <summary>
		/// Not implemented (TODO: will need a Relax NG validator for this).
		/// </summary>
		[PhpVisible]
		public void relaxNGValidate(string schemaFile)
		{
			PhpException.Throw(PhpError.Warning, Resources.RelaxNGUnsupported);
		}

		/// <summary>
		/// Not implemented (TODO: will need a Relax NG validator for this).
		/// </summary>
		[PhpVisible]
		public void relaxNGValidateSource(string schema)
		{
			PhpException.Throw(PhpError.Warning, Resources.RelaxNGUnsupported);
		}

		#endregion
	}

	/// <summary>
	/// DOM document fragment.
	/// </summary>
	[ImplementsType]
	public partial class DOMDocumentFragment : DOMNode
	{
		#region Fields and Properties

		protected internal XmlDocumentFragment XmlDocumentFragment
		{
			get
			{ return (XmlDocumentFragment)XmlNode; }
			set
			{ XmlNode = value; }
		}

		/// <summary>
		/// Returns &quot;#document-fragment&quot;.
		/// </summary>
		[PhpVisible]
		public override string nodeName
		{
			get
			{ return "#document-fragment"; }
		}

		/// <summary>
		/// Returns <B>null</B>.
		/// </summary>
		[PhpVisible]
		public override object nodeValue
		{
			get
			{ return null; }
			set
			{ }
		}

		/// <summary>
		/// Returns the namespace URI of the node.
		/// </summary>
		[PhpVisible]
		public override string namespaceURI
		{
			get
			{ return (IsAssociated ? base.namespaceURI : null); }
		}

		/// <summary>
		/// Returns the type of the node (<see cref="NodeType.DocumentFragment"/>).
		/// </summary>
		[PhpVisible]
		public override object nodeType
		{
			get
			{ return (int)NodeType.DocumentFragment; }
		}

		#endregion

		#region Construction

		public DOMDocumentFragment()
			: base(ScriptContext.CurrentContext, true)
		{ }

		internal DOMDocumentFragment(XmlDocumentFragment/*!*/ xmlDocumentFragment)
			: base(ScriptContext.CurrentContext, true)
		{
			this.XmlDocumentFragment = xmlDocumentFragment;
		}

		protected override PHP.Core.Reflection.DObject CloneObjectInternal(PHP.Core.Reflection.DTypeDesc caller, ScriptContext context, bool deepCopyFields)
		{
			return new DOMDocumentFragment(XmlDocumentFragment);
		}

		[PhpVisible]
		public void __construct()
		{ }

		#endregion

		#region Hierarchy

		protected internal override void Associate(XmlDocument document)
		{
			if (!IsAssociated)
			{
				XmlDocumentFragment = document.CreateDocumentFragment();
			}
		}

		#endregion

		#region Operations

		/// <summary>
		/// Appends (well-formed) XML data to this document fragment.
		/// </summary>
		/// <param name="data">The data to append.</param>
		/// <returns><B>True</B> or <B>false</B>.</returns>
		[PhpVisible]
		public object appendXML(string data)
		{
			try
			{
				XmlDocumentFragment.InnerXml += data;
			}
			catch (XmlException)
			{
				return false;
			}
			return true;
		}

		#endregion
	}
}
