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
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using PHP.Core;
using PHP.Core.Reflection;

namespace PHP.Library.Xml
{
	/// <summary>
	/// Contains implementation of SimpleXML functions.
	/// </summary>
	public static class SimpleXml
	{
		#region simplexml_load_file

		/// <summary>
		/// Loads an XML file into an object.
		/// </summary>
		/// <param name="fileName">The file name.</param>
		/// <param name="className">The name of the class whose instance should be returned (must extend
		/// <see cref="SimpleXMLElement"/>).</param>
		/// <param name="options">Additional parameters (unsupported).</param>
		/// <returns>An instance of <see cref="SimpleXMLElement"/> or of the class specified by
		/// <paramref name="className"/>, or <B>false</B> on error.</returns>
		[ImplementsFunction("simplexml_load_file")]
		public static object LoadFile(string fileName, string className, int options)
		{
			XmlDocument doc = new XmlDocument();
			doc.PreserveWhitespace = true;

			using (PhpStream stream = PhpStream.Open(fileName, "rt"))
			{
				if (stream == null) return false;

				try
				{
					doc.Load(stream.RawStream);
				}
				catch (XmlException e)
				{
					PhpException.Throw(PhpError.Warning, e.Message);
					return false;
				}
				catch (IOException e)
				{
					PhpException.Throw(PhpError.Warning, e.Message);
					return false;
				}
			}

            return SimpleXMLElement.Create(className, doc.DocumentElement);
		}

		[ImplementsFunction("simplexml_load_file")]
		public static object LoadFile(string fileName, string className)
		{
			return LoadFile(fileName, className, 0);
		}

		[ImplementsFunction("simplexml_load_file")]
		public static object LoadFile(string fileName)
		{
			return LoadFile(fileName, null, 0);
		}

		#endregion

		#region simplexml_load_string

		/// <summary>
		/// Loads a string of XML into an object.
		/// </summary>
		/// <param name="data">The XML string.</param>
		/// <param name="className">The name of the class whose instance should be returned (must extend
		/// <see cref="SimpleXMLElement"/>).</param>
		/// <param name="options">Additional parameters (unsupported).</param>
		/// <returns>An instance of <see cref="SimpleXMLElement"/> or of the class specified by
		/// <paramref name="className"/>, or <B>false</B> on error.</returns>
		[ImplementsFunction("simplexml_load_string")]
		public static object LoadString(string data, string className, int options)
		{
			XmlDocument doc = new XmlDocument();
			doc.PreserveWhitespace = true;

			try
			{
				doc.LoadXml(data);
			}
			catch (XmlException e)
			{
				PhpException.Throw(PhpError.Warning, e.Message);
				return false;
			}
			catch (IOException e)
			{
				PhpException.Throw(PhpError.Warning, e.Message);
				return false;
			}

            return SimpleXMLElement.Create(className, doc.DocumentElement);
		}

		[ImplementsFunction("simplexml_load_string")]
		public static object LoadString(string data, string className)
		{
			return LoadString(data, className, 0);
		}

		[ImplementsFunction("simplexml_load_string")]
		public static object LoadString(string data)
		{
			return LoadString(data, null, 0);
		}

		#endregion

		#region simplexml_import_dom

		/// <summary>
		/// Converts a <see cref="SimpleXMLElement"/> object to a <see cref="DOMElement"/>.
		/// </summary>
		/// <param name="domNode">A <see cref="DOMNode"/>.</param>
		/// <param name="className">The name of the class whose instance should be returned (must extend
		/// <see cref="SimpleXMLElement"/>).</param>
		/// <returns>An instance of <see cref="SimpleXMLElement"/> or of the class specified by
		/// <paramref name="className"/>, or <B>false</B> on error.</returns>
		[ImplementsFunction("simplexml_import_dom")]
		public static DObject ImportDom(DObject domNode, string className)
		{
			DOMNode node = domNode as DOMNode;
			if (node == null)
			{
				PhpException.InvalidImplicitCast(node, "DOMNode", "domNode");
				return null;
			}

			if (!node.IsAssociated)
			{
				PhpException.Throw(PhpError.Warning, Resources.SimpleXmlImportNotAssociated);
				return null;
			}

			XmlNode xml_node = node.XmlNode;

			// we can import only elements (root elements if the passed node is a document)
			switch (xml_node.NodeType)
			{
				case XmlNodeType.Document:
				{
					xml_node = xml_node.OwnerDocument.DocumentElement;
					if (xml_node != null) goto case XmlNodeType.Element; else goto default;
				}

				case XmlNodeType.Element:
				{
					return SimpleXMLElement.Create(className, (XmlElement)xml_node);
				}

				default:
				{
					PhpException.Throw(PhpError.Warning, Resources.SimpleXmlInvalidNodeToImport);
					return null;
				}
			}
		}

		[ImplementsFunction("simplexml_import_dom")]
		public static DObject ImportDom(DObject node)
		{
			return ImportDom(node, null);
		}

		#endregion
	}

	/// <summary>
	/// The one and only class comprising the SimpleXML extension.
	/// </summary>
	[ImplementsType]
	public partial class SimpleXMLElement : SPL.Traversable, SPL.ArrayAccess, SPL.Countable, IEnumerable<KeyValuePair<object, object>>
	{
		#region IterationType

		/// <summary>
		/// Specifies mostly the iteration (<c>foreach</c>) behavior of a <see cref="SimpleXMLElement"/> instance.
		/// </summary>
		internal enum IterationType
		{
			/// <summary>
			/// The instance represents a nonexistent element.
			/// </summary>
			None,

			/// <summary>
			/// The instance represents an attribute.
			/// </summary>
			Attribute,

			/// <summary>
			/// The instance represents the attribute list of an element.
			/// </summary>
			AttributeList,

			/// <summary>
			/// The instance represents an element and iteration will include its siblings.
			/// </summary>
			Element,

			/// <summary>
			/// The instance represents an element and iteration will include its child elements.
			/// </summary>
			ChildElements,
		}

		#endregion

        #region IterationNamespace

        internal class IterationNamespace
        {
            /// <summary>
            /// The namespace prefix. If it is not null, the prefix is used.
            /// </summary>
            public string namespacePrefix { get; private set; }

            /// <summary>
            /// The namespace of included elements/attributes. (Namespace of prefix if prefix is used)
            /// This value is always not null valid namespace (or empty string).
            /// </summary>
            public string namespaceUri { get; private set; }

            private IterationNamespace(string prefix, string namespaceUri)
            {
                this.namespacePrefix = prefix;
                this.namespaceUri = namespaceUri;
            }

            /// <summary>
            /// Create namespace iteration type by prefix.
            /// </summary>
            /// <param name="prefix"></param>
            /// <param name="relatedNode"></param>
            /// <returns></returns>
            public static IterationNamespace CreateWithPrefix(string prefix, XmlNode relatedNode)
            {
                if (prefix == null) prefix = string.Empty;  // is using prefix, it cannot be null

                return new IterationNamespace(prefix, (relatedNode != null) ? relatedNode.GetNamespaceOfPrefix(prefix) : string.Empty);
            }

            /// <summary>
            /// Create namespace iteration type by prefix.
            /// </summary>
            /// <param name="relatedNode"></param>
            /// <returns></returns>
            public static IterationNamespace CreateWithPrefix(XmlNode/*!*/relatedNode)
            {
                return new IterationNamespace(relatedNode.Prefix, relatedNode.NamespaceURI);
            }

            /// <summary>
            /// Create namespace iteration type by full namespace URI. Attributes with default namespace (with empty prefix) will not be included.
            /// </summary>
            /// <param name="namespaceUri"></param>
            /// <returns></returns>
            public static IterationNamespace CreateWithNamespace(string namespaceUri)
            {
                if (namespaceUri == null) namespaceUri = string.Empty;  // namespaceUri is never null in .NET

                return new IterationNamespace(null, namespaceUri);  // do not use prefix, use the whole namespace (different behavior)
            }

            /// <summary>
            /// Determine if the given XML node has the namespace.
            /// </summary>
            /// <param name="node"></param>
            /// <returns></returns>
            public bool IsIn(XmlNode/*!*/node)
            {
                Debug.Assert(node != null, "Argument node cannot be null.");

                if (namespacePrefix != null)
                    return node.Prefix == namespacePrefix;
                else
                    return node.NamespaceURI == namespaceUri;
            }

            /// <summary>
            /// Get the node[prefix:name] or node[name, ns] according to the namespace iteration type.
            /// </summary>
            /// <param name="node"></param>
            /// <param name="name"></param>
            /// <returns></returns>
            public XmlElement GetFirstChildIn(XmlNode/*!*/node, string/*!*/name)
            {
                Debug.Assert(node != null, "Argument node cannot be null.");
                Debug.Assert(name != null, "Argument name cannot be null.");

                if (namespacePrefix != null)
                    return node[(namespacePrefix.Length == 0) ? (name) : (namespacePrefix + ":" + name)];
                else
                    return node[name, namespaceUri];
            }

            public XmlAttribute GetAttributeIn(XmlAttributeCollection/*!*/attributes, string/*!*/name)
            {
                Debug.Assert(attributes != null, "Argument attributes cannot be null.");
                Debug.Assert(name != null, "Argument name cannot be null.");

                if (namespacePrefix == null)
                    return attributes[name, namespaceUri];
                else // using prefix !
                    return attributes[(namespacePrefix.Length == 0) ? (name) : (namespacePrefix + ":" + name)]; // prefix:name
            }

        }

        #endregion

        #region Fields and Properties
        /// <summary>
        /// Name of a class, which will be used when initializing children. Class which extends SimpleXmlElement HAS to be used. 
        /// Non-null value means, that this instance of<see cref="SimpleXMLElement"/> was initialized with specified className.
        /// </summary>
        private string className;

		/// <summary>
		/// Non-<B>null</B> except for construction (between ctor and <see cref="__construct(string,int,bool)"/>
		/// or <see cref="XmlElement"/> setter invocation).
		/// </summary>
		private XmlElement _element;

        internal XmlElement XmlElement
        {
            get
            {
                return this._element;
            }
            set
            {
                this._element = value;
                //this.namespaceUri = this._element.GetNamespaceOfPrefix(String.Empty);
                this.iterationNamespace = IterationNamespace.CreateWithPrefix(this._element);
            }
        }

		/// <summary>
		/// Lazily created namespace manager used for XPath queries.
		/// </summary>
		private XmlNamespaceManager _namespaceManager;
		private XmlNamespaceManager namespaceManager
		{
			get
			{
				if (_namespaceManager == null)
				{
					_namespaceManager = new XmlNamespaceManager(XmlElement.OwnerDocument.NameTable);
					
					// initialize the manager with prefixes/URIs from the document
					foreach (KeyValuePair<IntStringKey, object> pair in GetNodeNamespaces(XmlElement, true))
					{
						_namespaceManager.AddNamespace(pair.Key.String, (string)pair.Value);
					}
				}
				return _namespaceManager;
			}
		}

		/// <summary>
		/// The attribute (if this instance represents an individual attribute).
		/// </summary>
		private XmlAttribute XmlAttribute;

		/// <summary>
		/// Specifies iteration behavior of this instance (what it actually represents).
		/// </summary>
		private IterationType iterationType;

        /// <summary>
		/// The prefix or namespace URI of the elements/attributes that should be iterated and dumped.
		/// </summary>
        private IterationNamespace/*!*/ iterationNamespace;

		/// <summary>
		/// A list of names of elements representing the path in the document that should be added
		/// when a field or item is written to this instance.
		/// </summary>
		/// <remarks>
		/// This field supports <c>$doc->elem1->elem2->elem3 = "value"</c>, which creates <c>elem1</c>,
		/// <c>elem2</c>, and <c>elem3</c> if they do not already exist. Becomes non-<B>null</B> when
		/// an unknown element is read.
		/// </remarks>
		private List<string> intermediateElements;

		private static readonly DTypeDesc _typeDesc = DTypeDesc.Create(typeof(SimpleXMLElement));

		private static readonly VariableName textPropertyName = new VariableName("0");
		private static readonly VariableName attributesPropertyName = new VariableName("@attributes");

		#endregion

		#region Construction

		public SimpleXMLElement()
			: base(ScriptContext.CurrentContext, true)
		{
			this.iterationType = IterationType.ChildElements;
            this.iterationNamespace = IterationNamespace.CreateWithPrefix(string.Empty, null);
            this.className = null;
		}

		internal SimpleXMLElement(XmlElement/*!*/ xmlElement, IterationType iterationType, IterationNamespace/*!*/iterationNamespace)
			: base(ScriptContext.CurrentContext, true)
		{
			Debug.Assert(xmlElement != null && iterationNamespace != null);

			this.XmlElement = xmlElement;
			this.iterationType = iterationType;
			this.iterationNamespace = iterationNamespace;
		}

		internal SimpleXMLElement(XmlElement/*!*/ xmlElement, IterationType iterationType)
			: this(xmlElement, iterationType, IterationNamespace.CreateWithPrefix(string.Empty, xmlElement)/*xmlElement.GetNamespaceOfPrefix(String.Empty)*/)
		{ }

		internal SimpleXMLElement(XmlElement/*!*/ xmlElement)
            : this(xmlElement, IterationType.ChildElements, IterationNamespace.CreateWithPrefix(string.Empty, xmlElement)/*xmlElement.GetNamespaceOfPrefix(String.Empty)*/)
		{ }

        internal SimpleXMLElement(XmlAttribute/*!*/ xmlAttribute, IterationNamespace/*!*/iterationNamespace)
			: this(xmlAttribute.OwnerElement, IterationType.Attribute, iterationNamespace)
		{
			this.XmlAttribute = xmlAttribute;
		}

		internal SimpleXMLElement(XmlAttribute/*!*/ xmlAttribute)
            : this(xmlAttribute.OwnerElement, IterationType.Attribute, IterationNamespace.CreateWithPrefix(string.Empty, xmlAttribute)/*xmlAttribute.GetNamespaceOfPrefix(String.Empty)*/)
		{
			this.XmlAttribute = xmlAttribute;
		}

		[PhpVisible, PhpFinal]
		public void __construct(string data, [Optional] int options, [Optional] bool dataIsUrl)
		{
			XmlDocument doc = new XmlDocument();
			doc.PreserveWhitespace = true;

			try
			{
				if (dataIsUrl)
				{
					using (PhpStream stream = PhpStream.Open(data, "rt"))
					{
						if (stream != null) doc.Load(stream.RawStream);
					}
				}
				else doc.LoadXml(data);
			}
			catch (XmlException e)
			{
				PhpException.Throw(PhpError.Warning, e.Message);
			}

			if (doc.DocumentElement == null) doc.AppendChild(doc.CreateElement("empty"));
			this.XmlElement = doc.DocumentElement;
		}

        /// <summary>
        /// Creates a new <see cref="SimpleXMLElement"/> or a derived class.
        /// </summary>
        /// <param name="className">The name of the class to create or <B>null</B>.</param>
        /// <returns>A new <see cref="SimpleXMLElement"/> or a derived class.</returns>
        internal static SimpleXMLElement Create(string className)
        {
            if (className == null) return new SimpleXMLElement();

            ScriptContext context = ScriptContext.CurrentContext;

            // try to resolve the className
            DTypeDesc type = context.ResolveType(className, null, UnknownTypeDesc.Singleton, null, ResolveTypeFlags.ThrowErrors | ResolveTypeFlags.UseAutoload);
            if (type == null) return null;

            // we will not allow className which is not derived from SimpleXMLElement
            if (!type.IsSubclassOf(_typeDesc))
            {
                PhpException.Throw(PhpError.Warning, String.Format(Resources.SimpleXmlInvalidClassName, className));
                return null;
            }

            SimpleXMLElement instance = (SimpleXMLElement)type.New(context);
            instance.className = className;

            return instance;
        }

        /// <summary>
        /// Creates a new <see cref="SimpleXMLElement"/> or a derived class.
        /// </summary>
        /// <param name="className">The name of the class to create or <B>null</B>.</param>
        /// <param name="xmlElement">The <see cref="XmlElement"/> to wrap.</param>
        /// <param name="iterationType">Iteration behavior of new instance.</param>
        /// <param name="iterationNamespace">The namespace URI of the elements/attributes that should be iterated and dumped.</param>
        /// <returns>A new <see cref="SimpleXMLElement"/> or a derived class.</returns>
        internal static SimpleXMLElement Create(string className, XmlElement/*!*/ xmlElement, IterationType iterationType, IterationNamespace/*!*/iterationNamespace)
        {
            if (className == null) return new SimpleXMLElement(xmlElement, iterationType, iterationNamespace);

            SimpleXMLElement instance = Create(className);
            instance.XmlElement = xmlElement;
            instance.iterationType = iterationType;
            instance.iterationNamespace = iterationNamespace;

            return instance;
        }

        /// <summary>
        /// Creates a new <see cref="SimpleXMLElement"/> or a derived class.
        /// </summary>
        /// <param name="className">The name of the class to create or <B>null</B>.</param>
        /// <param name="xmlElement">The <see cref="XmlElement"/> to wrap.</param>
        /// <param name="iterationType">Iteration behavior of new instance.</param>
        /// <returns>A new <see cref="SimpleXMLElement"/> or a derived class.</returns>
        internal static SimpleXMLElement Create(string className, XmlElement/*!*/ xmlElement, IterationType iterationType)
        {
            if (className == null) return new SimpleXMLElement(xmlElement, iterationType);

            SimpleXMLElement instance = Create(className);
            instance.XmlElement = xmlElement;
            instance.iterationType = iterationType;

            return instance;
        }

		/// <summary>
		/// Creates a new <see cref="SimpleXMLElement"/> or a derived class.
		/// </summary>
		/// <param name="xmlElement">The <see cref="XmlElement"/> to wrap.</param>
		/// <param name="className">The name of the class to create or <B>null</B>.</param>
		/// <returns>A new <see cref="SimpleXMLElement"/> or a derived class.</returns>
		internal static SimpleXMLElement Create(string className, XmlElement/*!*/ xmlElement)
		{
			if (className == null) return new SimpleXMLElement(xmlElement);

			SimpleXMLElement instance = Create(className);
			instance.XmlElement = xmlElement;

			return instance;
		}

        /// <summary>
        /// Creates a new <see cref="SimpleXMLElement"/> or a derived class.
        /// </summary>
        /// <param name="className">The name of the class to create or <B>null</B>.</param>
        /// <param name="xmlAttribute">The <see cref="XmlElement"/> to wrap.</param>
        /// <param name="iterationNamespace">The namespace URI of the elements/attributes that should be iterated and dumped.</param>
        /// <returns>A new <see cref="SimpleXMLElement"/> or a derived class.</returns>
        internal static SimpleXMLElement Create(string className, XmlAttribute/*!*/ xmlAttribute, IterationNamespace/*!*/iterationNamespace)
        {
            if (className == null) return new SimpleXMLElement(xmlAttribute, iterationNamespace);

            SimpleXMLElement instance = Create(className);
            instance.XmlElement = xmlAttribute.OwnerElement;
            instance.iterationType = IterationType.Attribute;
            instance.iterationNamespace = iterationNamespace;
            instance.XmlAttribute = xmlAttribute;

            return instance;
        }

        /// <summary>
        /// Creates a new <see cref="SimpleXMLElement"/> or a derived class.
        /// </summary>
        /// <param name="className">The name of the class to create or <B>null</B>.</param>
        /// <param name="xmlAttribute">The <see cref="XmlElement"/> to wrap.</param>
        /// <returns>A new <see cref="SimpleXMLElement"/> or a derived class.</returns>
        internal static SimpleXMLElement Create(string className, XmlAttribute/*!*/ xmlAttribute)
        {
            if (className == null) return new SimpleXMLElement(xmlAttribute);

            SimpleXMLElement instance = Create(className);
            instance.XmlElement = xmlAttribute.OwnerElement;
            instance.iterationType = IterationType.Attribute;
            instance.XmlAttribute = xmlAttribute;
            
            return instance;
        }

		#endregion

		#region Internal overrides: Conversions, Dump, and Cloning

		/// <summary>
		/// Internal to-<see cref="PhpBytes"/> conversion.
		/// </summary>
		public override PhpBytes ToPhpBytes()
		{
			return new PhpBytes(ToString());
		}

		/// <summary>
		/// Internal to-<see cref="string"/> conversion.
		/// </summary>
		public override string ToString(bool throwOnError, out bool success)
		{
            success = true;

            if (XmlAttribute != null) return XmlAttribute.Value;

            // concatenate text nodes that are immediate children of this element
            StringBuilder sb = new StringBuilder();

            foreach (XmlNode child in XmlElement.ChildNodes)
            {
                string text = GetNodeText(child);
                if (text != null) sb.Append(text);
            }

            return sb.ToString();
		}

        /// <summary>
        /// String representation of the XML element.
        /// </summary>
        /// <returns>XML element content.</returns>
        public override string ToString()
        {
            bool success;
            return ToString(false, out success);
        }
		
		/// <summary>
		/// Internal to-<see cref="int"/> conversion.
		/// </summary>
		public override int ToInteger()
		{
            return PHP.Core.Convert.StringToInteger(ToString());
		}

        /// <summary>
        /// Internal to-<see cref="long"/> conversion.
        /// </summary>
        public override long ToLongInteger()
        {
            return PHP.Core.Convert.StringToLongInteger(ToString());
        }

        /// <summary>
        /// Internal to-<see cref="double"/> conversion.
        /// </summary>
        public override double ToDouble()
        {
            return PHP.Core.Convert.StringToDouble(ToString());
        }

        /// <summary>
        /// Internal to-<see cref="bool"/> conversion.
        /// </summary>
        public override bool ToBoolean()
        {
            switch (this.iterationType)
            {
                case IterationType.Attribute:
                    return true;

                #region modified from this.GetEnumerator()

                case IterationType.Element:
                    {
                        // find at least one sibling:
                        for (XmlNode sibling = XmlElement; sibling != null; sibling = sibling.NextSibling)
                            if (sibling.NodeType == XmlNodeType.Element && sibling.LocalName.Equals(XmlElement.LocalName, StringComparison.Ordinal) && iterationNamespace.IsIn(sibling))
                                return true;
                        return false;
                    }

                case IterationType.ChildElements:
                    {
                        // find at least one child element:
                        foreach (XmlNode child in XmlElement)
                            if (child.NodeType == XmlNodeType.Element && iterationNamespace.IsIn(child))
                                return true;
                        return false;
                    }

                case IterationType.AttributeList:
                    {
                        // find at least one attribute
                        foreach (XmlAttribute attr in XmlElement.Attributes)
                            if (!attr.Name.Equals("xmlns", StringComparison.Ordinal) && iterationNamespace.IsIn(attr))
                                return true;
                        return false;
                    }

                #endregion

                default:
                    // return true iff the instance has at least one property
                    return this.GetEnumerator().MoveNext();
            }
        }


		/// <summary>
		/// Internal dump enumeration.
		/// </summary>
		protected override IEnumerable<KeyValuePair<VariableName, AttributedValue>> PropertyIterator()
		{
			switch (iterationType)
			{
				case IterationType.None: yield break;
				case IterationType.Attribute:
				{
					yield return new KeyValuePair<VariableName, AttributedValue>
						(textPropertyName, new AttributedValue(XmlAttribute.Value));
					yield break;
				}
			}

			OrderedHashtable<string> properties = new OrderedHashtable<string>(XmlElement.ChildNodes.Count);
			StringBuilder text = null;

			foreach (XmlNode child in XmlElement.ChildNodes)
			{
				if (properties.Count == 0)
				{
					string text_data = GetNodeText(child);
					if (text_data != null)
					{
						if (text == null) text = new StringBuilder(text_data);
						else text.Append(text_data);
					}
				}
				
				if (child.NodeType == XmlNodeType.Element)
				{
                    if ((iterationType == IterationType.ChildElements || iterationType == IterationType.Element) &&
                        iterationNamespace.IsIn(child))
                    {
                        text = null;
                        object child_value = GetChildElementValue(className, (XmlElement)child);

                        OrderedHashtable<string>.Element element = properties.GetElement(child.LocalName);
                        if (element == null)
                        {
                            // the first element of this name
                            properties.Add(child.LocalName, child_value);
                        }
                        else
                        {
                            // a next element of this name -> create/add to array
                            PhpArray array = element.Value as PhpArray;
                            if (array == null)
                            {
                                array = new PhpArray(2);
                                array.Add(element.Value);
                            }
                            array.Add(child_value);

                            element.Value = array;
                        }
                    }
				}

			}

			// yield return attributes (if present)
			XmlAttributeCollection attributes = XmlElement.Attributes;
			if (attributes != null && attributes.Count > 0)
			{
				PhpArray attr_array = new PhpArray(0, attributes.Count);
				foreach (XmlAttribute attribute in attributes)
				{
					if (iterationNamespace.IsIn(attribute) && attribute.Name != "xmlns")
					{
						attr_array.Add(attribute.LocalName, attribute.Value);
					}
				}

				if (attr_array.Count > 0)
				{
					yield return new KeyValuePair<VariableName, AttributedValue>
						(attributesPropertyName, new AttributedValue(attr_array));
				}
			}

			// yield return the inner text
			if (text != null)
			{
				yield return new KeyValuePair<VariableName, AttributedValue>
					(textPropertyName, new AttributedValue(text.ToString()));
			}
			else
			{
				// yield return all child elements
				foreach (KeyValuePair<string, object> pair in properties)
				{
					yield return new KeyValuePair<VariableName, AttributedValue>
						(new VariableName(pair.Key), new AttributedValue(pair.Value));
				}
			}
		}

		/// <summary>
		/// Invoked when the instance is being cloned.
		/// </summary>
		protected override DObject CloneObjectInternal(DTypeDesc caller, ScriptContext context, bool deepCopyFields)
		{
			SimpleXMLElement clone;
			if (iterationType == IterationType.Attribute)
			{
				clone = Create(className, XmlAttribute, iterationNamespace);
			}
            else clone = Create(className, XmlElement, iterationType, iterationNamespace);

			if (intermediateElements != null) clone.intermediateElements = new List<string>(intermediateElements);
			clone._namespaceManager = _namespaceManager;

			return clone;
		}

		#endregion

		#region Internal overrides: Property access

		/// <summary>
		/// Property reading (i.e. child element getter).
		/// </summary>
		protected override object PropertyReadHandler(string name, DTypeDesc caller, out bool handled)
		{
			handled = true;

            XmlElement child = iterationNamespace.GetFirstChildIn(XmlElement, name);// XmlElement[name, namespaceUri];

            if (child != null)
            {
                return Create(className, child, IterationType.Element, iterationNamespace /*operating on the current namespace $element->children('namespace ...')->link*/);
            }
            else
            {
                SimpleXMLElement elem = Create(className, XmlElement, IterationType.None);

                if (intermediateElements != null)
                {
                    elem.intermediateElements = new List<string>(intermediateElements);
                }
                else
                {
                    elem.intermediateElements = new List<string>();
                }

                elem.intermediateElements.Add(name);

                return elem;
            }
		}

		/// <summary>
		/// Property writing (i.e. child element setter).
		/// </summary>
		protected override bool PropertyWriteHandler(object name, object value, DTypeDesc caller)
		{
			string name_str = name as string;
			if (name_str == null) return false;

			BuildUpIntermediateElements();

			XmlElement child = null;

			// try to find the child element of the given local name & namespace URI
			foreach (XmlNode node in XmlElement.ChildNodes)
			{
				if (node.NodeType == XmlNodeType.Element &&
					node.LocalName == name_str &&
					iterationNamespace.IsIn(node)/*node.NamespaceURI == namespaceUri*/)
				{
					if (child != null)
					{
						// duplicate!
						PhpException.Throw(PhpError.Warning,
							String.Format(Resources.SimpleXmlAssignmentToDuplicateNodes, name_str));
						return false;
					}
					else child = (XmlElement)node;
				}
			}

			if (child == null)
			{
				child = XmlElement.OwnerDocument.CreateElement(name_str, iterationNamespace.namespaceUri);
				XmlElement.AppendChild(child);
			}

			// check value type
			if (value != null && Type.GetTypeCode(value.GetType()) == TypeCode.Object)
			{
				PhpException.Throw(PhpError.Warning, Resources.SimpleXmlUnsupportedWriteConversion);
				return false;
			}

			child.InnerText = Core.Convert.ObjectToString(value);
			return true;
		}

		/// <summary>
		/// Property unsetting (i.e. child element remover).
		/// </summary>
		protected override bool PropertyUnsetHandler(string name, DTypeDesc caller)
		{
			List<XmlNode> to_remove = new List<XmlNode>();

			// remove all child elements of the given local name & namespace URI
			foreach (XmlNode node in XmlElement.ChildNodes)
			{
				if (node.NodeType == XmlNodeType.Element &&
					node.LocalName == name &&
					iterationNamespace.IsIn(node)/*node.NamespaceURI == namespaceUri*/)
				{
					to_remove.Add(node);
				}
			}

			if (to_remove.Count == 0) return false;
			else
			{
				foreach (XmlNode node in to_remove)
				{
					XmlElement.RemoveChild(node);
				}
				return true;
			}
		}

		/// <summary>
		/// Property isset testing (i.e. child element existence test).
		/// </summary>
		public override object PropertyIssetHandler(string name, DTypeDesc caller, out bool handled)
		{
			handled = true;

            XmlElement child = iterationNamespace.GetFirstChildIn(XmlElement, name);// XmlElement[name, namespaceUri];

            if (child != null) return Create(className, child);
			else return null;
		}

		#endregion

        #region IPhpComparable CompareTo

        public override int CompareTo(object obj, IComparer comparer)
        {
            Debug.Assert(comparer != null, "Invalid argument");

            string strobj;
            if ((strobj = obj as string) != null)
            {
                switch (iterationType)
                {
                    case IterationType.Attribute:
                        return PhpComparer.CompareString(XmlAttribute.Value, strobj);
                    case IterationType.Element:
                    case IterationType.ChildElements:
                        return PhpComparer.CompareString(GetPhpInnerText(XmlElement), strobj);
                    default:
                        break;
                }
            }

            return base.CompareTo(obj, comparer);
        }

        #endregion

        #region IPhpConvertible ToPhpArray

        /// <summary>
        /// Get inner text, child elements only (not recursive).
        /// </summary>
        /// <param name="child"></param>
        /// <returns></returns>
        private string GetPhpInnerText(XmlNode child)
        {
            string NodeValue = null;

            foreach (XmlNode x in child.ChildNodes)
                if (x.NodeType == XmlNodeType.Text)
                    NodeValue = NodeValue + x.InnerText;

            return NodeValue;
        }

        /// <summary>
        /// Returns given child node as a SimpleXMLElement, or as a simple string.
        /// It depends on its child nodes. (Because of PHP; node is represented as a string, if it has a child node of type Text)
        /// </summary>
        /// <param name="child"></param>
        /// <returns></returns>
        private object GetPhpChildElement(XmlNode child)
        {
            if (child == null || child.NodeType != XmlNodeType.Element || !iterationNamespace.IsIn(child)/*child.NamespaceURI != namespaceUri*/)
                return null;

            // check if the node contains Text node, return only the string
            string NodeValue = GetPhpInnerText(child);

            if (NodeValue != null)
                return NodeValue;

            // otherwise
            return Create(className, (XmlElement)child);
        }

        /// <summary>
        /// Overrides conversion of SimpleXMLElement to array.
        /// </summary>
        /// <returns></returns>
        public override PhpArray ToPhpArray()
        {
            PhpArray array = new PhpArray();

            if (XmlAttribute != null)
            {
                array.AddToEnd(XmlAttribute.Value);
            }
            else
            {
                foreach (XmlNode child in XmlElement)
                {
                    object childElement = GetPhpChildElement(child);

                    if (childElement != null)
                    {
                        if (array.ContainsKey(child.LocalName))
                        {
                            object item = array[child.LocalName];
                            PhpArray arrayitem = item as PhpArray;

                            if (arrayitem == null)
                            {
                                arrayitem = new PhpArray(2);
                                arrayitem.Add(item);
                                arrayitem.Add(childElement);
                                array[child.LocalName] = arrayitem;
                            }
                            else
                            {
                                arrayitem.Add(childElement);
                            }
                        }
                        else
                            array.Add(child.LocalName, childElement);
                    }
                }
            }

            return array;
        }

        #endregion

        #region Operations

        /// <summary>
		/// Return a well-formed XML string based on this <see cref="SimpleXMLElement"/>.
		/// </summary>
		[PhpVisible]
		public object asXML([Optional] string fileName)
		{
			// determine output encoding
			Encoding encoding = XmlDom.GetNodeEncoding(XmlElement);

			if (fileName == null)
			{
				// return the XML string
				using (MemoryStream stream = new MemoryStream())
				{
					// use a XML writer and set its Formatting property to Formatting.Indented
					using (XmlTextWriter writer = new XmlTextWriter(stream, encoding))
					{
						//writer.Formatting = Formatting.Indented;
						if (XmlElement.ParentNode is XmlDocument) XmlElement.ParentNode.WriteTo(writer);
						else XmlElement.WriteTo(writer);
					}

					return new PhpBytes(stream.ToArray());
				}
			}
			else
			{
				// write XML to the file
				using (PhpStream stream = PhpStream.Open(fileName, "wt"))
				{
					if (stream == null) return false;

					try
					{
						using (XmlTextWriter writer = new XmlTextWriter(stream.RawStream, encoding))
						{
							//writer.Formatting = Formatting.Indented;
							if (XmlElement.ParentNode is XmlDocument) XmlElement.ParentNode.WriteTo(writer);
							else XmlElement.WriteTo(writer);
						}
					}
					catch (XmlException e)
					{
						PhpException.Throw(PhpError.Warning, e.Message);
						return false;
					}
				}
				return true;
			}
		}

		/// <summary>
		/// Runs an XPath query on the XML data.
		/// </summary>
		/// <param name="path">The XPath query string.</param>
		/// <returns>A <see cref="PhpArray"/> of <see cref="SimpleXMLElement"/>s or <B>false</B>.</returns>
		[PhpVisible]
		public object xpath(string path)
		{
			if (iterationType != IterationType.ChildElements && iterationType != IterationType.Element) return false;

			XPathNavigator navigator = XmlElement.CreateNavigator();
			XPathNodeIterator iterator;

			// execute the query
			try
			{
				iterator = navigator.Select(path, namespaceManager);
			}
			catch (Exception e)
			{
				PhpException.Throw(PhpError.Warning, e.Message);
				return false;
			}

			PhpArray result = new PhpArray();

			// add the returned nodes to the resulting array
			while (iterator.MoveNext())
			{
				XmlNode node = iterator.Current.UnderlyingObject as XmlNode;
				if (node != null)
				{
					switch (node.NodeType)
					{
						case XmlNodeType.Element:
						{
                            result.Add(Create(className, (XmlElement)node));
							break;
						}
						case XmlNodeType.Attribute:
						{
                            result.Add(Create(className, (XmlAttribute)node));
							break;
						}

						case XmlNodeType.CDATA:
						case XmlNodeType.SignificantWhitespace:
						case XmlNodeType.Text:
						case XmlNodeType.Whitespace:
						{
                            result.Add(Create(className, (XmlElement)node.ParentNode));
							break;
						}
					}
				}
			}

			return result;
		}

		/// <summary>
		/// Creates a prefix/ns context for the next XPath query.
		/// </summary>
		/// <param name="prefix">The namespace prefix.</param>
		/// <param name="namespaceUri">The namespace URI.</param>
		/// <returns><B>True</B> on success, <B>false</B> on error.</returns>
		[PhpVisible]
		public bool registerXPathNamespace(string prefix, string namespaceUri)
		{
			try
			{
				namespaceManager.AddNamespace(prefix, namespaceUri);
			}
			catch (Exception e)
			{
				PhpException.Throw(PhpError.Warning, e.Message);
				return false;
			}

			return true;
		}

		/// <summary>
		/// Identifies the element's attributes.
		/// </summary>
		/// <param name="ns">Namespace URI or prefix of the attributes to identify.</param>
		/// <param name="isPrefix">If <B>true</B> <paramref name="ns"/> denotes a prefix, if <B>false</B> it
		/// is a namespace URI.</param>
		/// <returns>A new <see cref="SimpleXMLElement"/> wrapping the same element but enumerating and
		/// dumping only the matching attributes.</returns>
		[PhpVisible]
		public SimpleXMLElement attributes([Optional] string ns, [Optional] bool isPrefix)
		{
			if (iterationType != IterationType.ChildElements && iterationType != IterationType.Element) return null;

			/*if (isPrefix)
			{
				ns = XmlElement.GetNamespaceOfPrefix(ns);
			}*/

            return Create(className, XmlElement, IterationType.AttributeList, (ns == null) ? iterationNamespace : (isPrefix ? IterationNamespace.CreateWithPrefix(ns, XmlElement) : IterationNamespace.CreateWithNamespace(ns)));
		}

		/// <summary>
		/// Identifies the element's child elements.
		/// </summary>
		/// <param name="ns">Namespace URI or prefix of the elements to identify.</param>
		/// <param name="isPrefix">If <B>true</B> <paramref name="ns"/> denotes a prefix, if <B>false</B> it
		/// is a namespace URI.</param>
		/// <returns>A new <see cref="SimpleXMLElement"/> wrapping the same element but enumerating and
		/// dumping only the matching elements.</returns>
		[PhpVisible]
        public SimpleXMLElement children([Optional] string ns, [Optional] bool isPrefix)
        {
            if (iterationType != IterationType.ChildElements && iterationType != IterationType.Element) return null;

            /*if (isPrefix)
            {
                ns = XmlElement.GetNamespaceOfPrefix(ns);
            }*/

            return Create(className, XmlElement, IterationType.ChildElements, (ns == null) ? iterationNamespace : (isPrefix ? IterationNamespace.CreateWithPrefix(ns, XmlElement) : IterationNamespace.CreateWithNamespace(ns)));
        }

		/// <summary>
		/// Returns namespaces used by children of this node.
		/// </summary>
		/// <param name="recursive">If <B>true</B> returns namespaces used by all children recursively.</param>
		/// <returns>An <see cref="PhpArray"/> keyed by prefix with values being namespace URIs.</returns>
		[PhpVisible]
		public PhpArray getNamespaces([Optional] bool recursive)
		{
			return GetNodeNamespaces(XmlElement, recursive);
		}

		/// <summary>
		/// Returns namespaces used by the document.
		/// </summary>
		/// <param name="recursive">If <B>true</B> returns namespaces used by all nodes recursively.</param>
		/// <returns>An <see cref="PhpArray"/> keyed by prefix with values being namespace URIs.</returns>
		[PhpVisible]
		public PhpArray getDocNamespaces([Optional] bool recursive)
		{
			return GetNodeNamespaces(XmlElement.OwnerDocument, recursive);
		}

		/// <summary>
		/// Gets the name of the XML element.
		/// </summary>
		[PhpVisible]
		public string getName()
		{
			return (XmlAttribute != null ? XmlAttribute.LocalName : XmlElement.LocalName);
		}

		/// <summary>
		/// Adds a child element to this XML element.
		/// </summary>
		/// <param name="qualifiedName">The qualified name of the element to add.</param>
		/// <param name="value">The optional element value.</param>
		/// <param name="namespaceUri">The optional element namespace URI.</param>
		/// <returns>The <see cref="SimpleXMLElement"/> of the child.</returns>
		[PhpVisible]
		public SimpleXMLElement addChild(string qualifiedName, [Optional] string value, [Optional] string namespaceUri)
		{
			XmlElement child;
			try
			{
                if (namespaceUri == null) namespaceUri = iterationNamespace.namespaceUri;// this.namespaceUri;
				child = XmlElement.OwnerDocument.CreateElement(qualifiedName, namespaceUri);
				
				if (value != null) child.InnerText = value;
				
				XmlElement.AppendChild(child);
			}
			catch (Exception e)
			{
				PhpException.Throw(PhpError.Warning, e.Message);
				return null;
			}

            return Create(className, child);
		}

		/// <summary>
		/// Adds an attribute to this XML element.
		/// </summary>
		/// <param name="qualifiedName">The qualified name of the attribute to add.</param>
		/// <param name="value">The attribute value.</param>
		/// <param name="namespaceUri">The optional attribute namespace URI.</param>
		[PhpVisible]
		public void addAttribute(string qualifiedName, string value, [Optional] string namespaceUri)
		{
			try
			{
                if (namespaceUri == null) namespaceUri = iterationNamespace.namespaceUri;// this.namespaceUri;
				XmlAttribute attr = XmlElement.OwnerDocument.CreateAttribute(qualifiedName, namespaceUri);
				
				attr.Value = value;

				XmlElement.Attributes.Append(attr);
			}
			catch (Exception e)
			{
				PhpException.Throw(PhpError.Warning, e.Message);
			}
		}

		#endregion

		#region Helper methods

		/// <summary>
		/// Wraps a node or returns its inner text if it is an element containing nothing but text.
		/// </summary>
		private static object GetChildElementValue(string className, XmlElement xmlElement)
		{
			// determine whether all children are text-like and concat them
			StringBuilder text = null;

			foreach (XmlNode child in xmlElement.ChildNodes)
			{
				string child_text = GetNodeText(child);
				if (child_text != null)
				{
					if (text == null) text = new StringBuilder(child_text);
					else text.Append(child_text);
				}
                else return Create(className, xmlElement);
			}

            return (text == null ? (object)Create(className, xmlElement) : text.ToString());
		}

		/// <summary>
		/// Returns the text data if the supplied node is treated as &quot;text&quot;.
		/// </summary>
		private static string GetNodeText(XmlNode node)
		{
			switch (node.NodeType)
			{
				case XmlNodeType.EntityReference: return "&" + node.Name + ";";

				case XmlNodeType.CDATA:
				case XmlNodeType.SignificantWhitespace:
				case XmlNodeType.Text:
				case XmlNodeType.Whitespace: return node.Value;
			}

			return null;
		}

        /// <summary>
		/// Returns an array of namespaces used by children of the given node (recursively).
		/// </summary>
        private static PhpArray GetNodeNamespaces(XmlNode xmlNode, bool recursive)
        {
            PhpArray result = new PhpArray();

            XPathNavigator navigator = xmlNode.CreateNavigator();
            XPathNodeIterator iterator = navigator.Select(recursive ? "//namespace::*" : "/*/namespace::*");

            string default_ns = null;

            while (iterator.MoveNext())
            {
                string prefix = iterator.Current.Name;
                if (prefix != "xml")
                {
                    if (prefix.Length == 0)
                    {
                        // do not add the default namespace into the array yet, should be placed at the beginning once (see later)
                        default_ns = iterator.Current.Value;
                    }
                    else
                    {
                        // there may be duplicates
                        result[prefix] = iterator.Current.Value;
                    }
                }
            }

            // the default ns should be at the beginning of the array
            if (default_ns != null)
                result.Prepend(String.Empty, default_ns);

            return result;
        }

		/// <summary>
		/// Returns the <paramref name="index"/>th sibling with the same local name and namespace URI or <B>null</B>.
		/// </summary>
		private XmlElement GetSiblingForIndex(int index)
		{
			if (index <= 0) return XmlElement;

			// getting index-th element of this name
			XmlNode node = XmlElement;
			while ((node = node.NextSibling) != null)
			{
				if (node.NodeType == XmlNodeType.Element &&
					node.LocalName == XmlElement.LocalName &&
					node.NamespaceURI == XmlElement.NamespaceURI) index--;

				if (index == 0) return (XmlElement)node;
			}

			return null;
		}

		/// <summary>
		/// Returns the <param name="index"/>th attribute with the current namespace URI or<B>null</B>.
		/// </summary>
		private XmlAttribute GetAttributeForIndex(int index)
		{
			foreach (XmlAttribute attr in XmlElement.Attributes)
			{
				if (iterationNamespace.IsIn(attr))
				{
					if (index == 0) return attr;
					index--;
				}
			}

			return null;
		}

		/// <summary>
		/// Creates elements stored in <see cref="intermediateElements"/> when it turns out that
		/// there will be a write.
		/// </summary>
		/// <remarks><seealso cref="intermediateElements"/></remarks>
		private void BuildUpIntermediateElements()
		{
			if (intermediateElements != null)
			{
				XmlElement element = XmlElement;
				
				// create all missing elements on the path
				foreach (string element_name in intermediateElements)
				{
                    XmlElement subelement = iterationNamespace.GetFirstChildIn(element, element_name);// element[element_name, namespaceUri];
					if (subelement == null)
					{
                        subelement = element.OwnerDocument.CreateElement(element_name, iterationNamespace.namespaceUri/*this.namespaceUri*/);
						element.AppendChild(subelement);
					}
					element = subelement;
				}

				XmlElement = element;
				iterationType = IterationType.Element;

				intermediateElements = null;
			}
		}

		#endregion

        #region IEnumerable<KeyValuePair<object,object>> Members

        public new IEnumerator<KeyValuePair<object, object>> GetEnumerator()
		{
			switch (iterationType)
			{
				case IterationType.Element:
				{
					// yield return siblings
					for (XmlNode sibling = XmlElement; sibling != null; sibling = sibling.NextSibling)
					{
                        if (sibling.NodeType == XmlNodeType.Element && sibling.LocalName.Equals(XmlElement.LocalName, StringComparison.Ordinal) && iterationNamespace.IsIn(sibling) /*sibling.NamespaceURI == namespaceUri*/)
						{
							yield return new KeyValuePair<object, object>
                                (sibling.LocalName, Create(className, (XmlElement)sibling, IterationType.ChildElements, iterationNamespace /* preserve namespaceUri */));
						}
					}
					break;
				}

				case IterationType.ChildElements:
				{
					// yield return child elements
					foreach (XmlNode child in XmlElement)
					{
						if (child.NodeType == XmlNodeType.Element && iterationNamespace.IsIn(child) /*child.NamespaceURI == namespaceUri*/)
						{   
                            yield return new KeyValuePair<object, object>
                                (child.LocalName, Create(className, (XmlElement)child));
						}
                        /*object childElement = GetPhpChildElement(child);
                        if (childElement != null)
                            yield return new KeyValuePair<object, object>
                                (child.LocalName, childElement);
                         */
					}
					break;
				}

				case IterationType.AttributeList:
				{
					// yield return attributes
					foreach (XmlAttribute attr in XmlElement.Attributes)
					{
						if (!attr.Name.Equals("xmlns", StringComparison.Ordinal) && iterationNamespace.IsIn(attr)/*attr.NamespaceURI == namespaceUri*/)
						{
							yield return new KeyValuePair<object, object>
                                (attr.LocalName, Create(className, attr));
						}
					}
					break;
				}
			}
		}

		#endregion

		#region IEnumerable Members

		IEnumerator IEnumerable.GetEnumerator()
		{
			foreach (KeyValuePair<object, object> pair in this)
			{
				yield return new DictionaryEntry(pair.Key, pair.Value);
			}
		}

		#endregion

		#region SPL.ArrayAccess Members

		[PhpVisible]
		public object offsetGet(object index)
		{
			IntStringKey key;
			if (!Core.Convert.ObjectToArrayKey(index, out key)) return null;

			if (key.IsInteger)
			{
				switch (iterationType)
				{
					case IterationType.AttributeList:
					{
						// return the index-th attribute
						XmlAttribute attr = GetAttributeForIndex(key.Integer);
                        return (attr != null ? Create(className, attr) : null);
					}

					case IterationType.ChildElements:
					case IterationType.Element:
					{
						// returning the index-th sibling of the same name
						XmlElement element = GetSiblingForIndex(key.Integer);
                        return (element != null ? Create(className, element) : null);
					}
				}
			}
			else
			{
				if (iterationType == IterationType.AttributeList ||
					iterationType == IterationType.ChildElements ||
					iterationType == IterationType.Element)
				{
					// getting an attribute
                    XmlAttribute attr = iterationNamespace.GetAttributeIn(XmlElement.Attributes, key.String);// XmlElement.Attributes[key.String, namespaceUri];
					return (attr != null ? Create(className, attr, iterationNamespace) : null);
				}
			}
			return null;
		}

		[PhpVisible]
		public object offsetSet(object index, object value)
		{
			IntStringKey key;
			if (!Core.Convert.ObjectToArrayKey(index, out key)) return null;

			BuildUpIntermediateElements();

			if (iterationType == IterationType.AttributeList ||
				iterationType == IterationType.ChildElements ||
				iterationType == IterationType.Element)
			{
				if (value != null && Type.GetTypeCode(value.GetType()) == TypeCode.Object)
				{
					PhpException.Throw(PhpError.Warning, Resources.SimpleXmlUnsupportedWriteConversion);
				}
				else
				{
					string value_str = Core.Convert.ObjectToString(value);

					if (key.IsInteger)
					{
						if (iterationType == IterationType.AttributeList)
						{
							// setting value of the index-th attribute
							XmlAttribute attr = GetAttributeForIndex(key.Integer);
							if (attr != null) attr.Value = value_str;
						}
						else
						{
							// setting value of the index-th sibling of the same name
							XmlElement element = GetSiblingForIndex(key.Integer);

							if (element == null)
							{
								element = XmlElement.OwnerDocument.CreateElement(XmlElement.LocalName, iterationNamespace.namespaceUri);
								XmlElement.ParentNode.AppendChild(element);
							}

							element.InnerText = value_str;
						}
					}
					else
					{
						// setting an attribute
                        XmlAttribute attr = iterationNamespace.GetAttributeIn(XmlElement.Attributes, key.String);// XmlElement.Attributes[key.String, namespaceUri];

						if (attr == null)
						{
                            attr = XmlElement.Attributes.Append(XmlElement.OwnerDocument.CreateAttribute(key.String, iterationNamespace.namespaceUri));
						}

						attr.Value = value_str;
					}
				}
			}
			return null;
		}

		[PhpVisible]
		public object offsetUnset(object index)
		{
			IntStringKey key;
			if (!Core.Convert.ObjectToArrayKey(index, out key)) return null;

			if (iterationType == IterationType.AttributeList ||
				iterationType == IterationType.ChildElements ||
				iterationType == IterationType.Element)
			{
				if (key.IsInteger)
				{
					if (iterationType == IterationType.AttributeList)
					{
						// removing the index-th attribute
						XmlAttribute attr = GetAttributeForIndex(key.Integer);
						if (attr != null) XmlElement.Attributes.Remove(attr);
					}
					else
					{
						// removing the index-th sibling of the same name
						XmlElement element = GetSiblingForIndex(key.Integer);
						if (element != null) XmlElement.ParentNode.RemoveChild(element);
					}
				}
				else
				{
					// removing an attribute
                    XmlAttribute attr = iterationNamespace.GetAttributeIn(XmlElement.Attributes, key.String);// XmlElement.Attributes[key.String, namespaceUri];
					if (attr != null)
                        XmlElement.Attributes.Remove(attr);
				}
			}
			return null;
		}

		[PhpVisible]
		public object offsetExists(object index)
		{
			IntStringKey key;
			if (!Core.Convert.ObjectToArrayKey(index, out key)) return null;

			if (iterationType == IterationType.AttributeList ||
				iterationType == IterationType.ChildElements ||
				iterationType == IterationType.Element)
			{
				if (key.IsInteger)
				{
					if (iterationType == IterationType.AttributeList)
					{
						// testing the index-th attribute
						return (GetAttributeForIndex(key.Integer) != null);
					}
					else
					{
						// testing the index-th sibling of the same name
						return (GetSiblingForIndex(key.Integer) != null);
					}
				}
				else
				{
					// testing an attribute
                    return iterationNamespace.GetAttributeIn(XmlElement.Attributes, key.String) != null;// (XmlElement.Attributes[key.String, namespaceUri] != null);
				}
			}
			return null;
		}

		#endregion

        #region SPL.Countable

        /// <summary>
        /// Count childs in the element.
        /// </summary>
        /// <returns></returns>
        [PhpVisible]
        public object count()
        {
            int _count = 0;

            foreach (KeyValuePair<object,object> x in this)
                ++_count;

            return _count;
        }

        #endregion
    }

}
