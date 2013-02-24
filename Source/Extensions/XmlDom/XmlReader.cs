/*

 Copyright (c) 2013 Ashod Nakashian.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Runtime.InteropServices;
using PHP.Core;

namespace PHP.Library.Xml
{
    /// <summary>
    /// DOM node.
    /// </summary>
    [ImplementsType]
    public partial class XMLReader
    {
        #region XmlReader node types

        public const int NONE = 0;
        public const int ELEMENT = 1;
        public const int ATTRIBUTE = 2;
        public const int TEXT = 3;
        public const int CDATA = 4;
        public const int ENTITY_REF = 5;
        public const int ENTITY = 6;
        public const int PI = 7;
        public const int COMMENT = 8;
        public const int DOC = 9;
        public const int DOC_TYPE = 10;
        public const int DOC_FRAGMENT = 11;
        public const int NOTATION = 12;
        public const int WHITESPACE = 13;
        public const int SIGNIFICANT_WHITESPACE = 14;
        public const int END_ELEMENT = 15;
        public const int END_ENTITY = 16;
        public const int XML_DECLARATION = 17;
        public const int LOADDTD = 1;
        public const int DEFAULTATTRS = 2;
        public const int VALIDATE = 3;
        public const int SUBST_ENTITIES = 4;

        #endregion

        #region Properties

        /// <summary>
        /// The number of attributes on the node.
        /// </summary>
        [PhpVisible]
        public object attributeCount
        {
            get { return 0; }
        }

        /// <summary>
        /// The base URI of the node.
        /// </summary>
        [PhpVisible]
        public object baseURI
        {
            get { return ""; }
        }

        /// <summary>
        /// Depth of the node in the tree, starting at 0.
        /// </summary>
        [PhpVisible]
        public object depth
        {
            get { return 0; }
        }

        /// <summary>
        /// Indicates if node has attributes.
        /// </summary>
        [PhpVisible]
        public object hasAttributes
        {
            get { return false; }
        }

        /// <summary>
        /// Indicates if node has a text value.
        /// </summary>
        [PhpVisible]
        public object hasValue
        {
            get { return false; }
        }

        /// <summary>
        /// Indicates if attribute is defaulted from DTD.
        /// </summary>
        [PhpVisible]
        public object isDefault
        {
            get { return true; }
        }

        /// <summary>
        /// Indicates if node is an empty element tag.
        /// </summary>
        [PhpVisible]
        public object isEmptyElement
        {
            get { return true; }
        }

        /// <summary>
        /// The local name of the node.
        /// </summary>
        [PhpVisible]
        public object localName
        {
            get { return ""; }
        }

        /// <summary>
        /// The qualified name of the node.
        /// </summary>
        [PhpVisible]
        public object name
        {
            get { return ""; }
        }

        /// <summary>
        /// The URI of the namespace associated with the node.
        /// </summary>
        [PhpVisible]
        public object namespaceURI
        {
            get { return ""; }
        }

        /// <summary>
        /// The node type for the node.
        /// </summary>
        [PhpVisible]
        public object nodeType
        {
            get { return 0; }
        }

        /// <summary>
        /// The prefix of the namespace associated with the node.
        /// </summary>
        [PhpVisible]
        public object prefix
        {
            get { return ""; }
        }

        /// <summary>
        /// The text value of the node.
        /// </summary>
        [PhpVisible]
        public object value
        {
            get { return ""; }
        }

        /// <summary>
        /// The xml:lang scope which the node resides.
        /// </summary>
        [PhpVisible]
        public object xmlLang
        {
            get { return ""; }
        }

        #endregion

        #region Methods
        
        [PhpVisible]
        public bool close()
        {
            return true;
        }

        [PhpVisible]
        public object expand([Optional] DOMNode basenode)
        {
            return false;
        }

        [PhpVisible]
        public string getAttribute(string name)
        {
            return "";
        }

        [PhpVisible]
        public string getAttributeNo(int index)
        {
            return "";
        }

        [PhpVisible]
        public string getAttributeNs(string localName, string namespaceURI)
        {
            return "";
        }

        [PhpVisible]
        public bool getParserProperty(int property)
        {
            return true;
        }

        [PhpVisible]
        public bool isValid()
        {
            return true;
        }

        [PhpVisible]
        public bool lookupNamespace(string prefix)
        {
            return true;
        }

        [PhpVisible]
        public bool moveToAttribute(string name)
        {
            return true;
        }

        [PhpVisible]
        public bool moveToAttributeNo(int index)
        {
            return true;
        }

        [PhpVisible]
        public bool moveToAttributeNs(string localName, string namespaceURI)
        {
            return true;
        }

        [PhpVisible]
        public bool moveToElement()
        {
            return true;
        }

        [PhpVisible]
        public bool moveToFirstAttribute()
        {
            return true;
        }

        [PhpVisible]
        public bool moveToNextAttribute()
        {
            return true;
        }

        [PhpVisible]
        public bool next([Optional] string localname)
        {
            return true;
        }

        [PhpVisible]
        public bool open(string URI, [Optional] string encoding, [Optional] int options)
        {
            return true;
        }

        [PhpVisible]
        public bool read()
        {
            return false;
        }

        [PhpVisible]
        public string readInnerXML()
        {
            return "";
        }

        [PhpVisible]
        public string readOuterXML()
        {
            return "";
        }

        [PhpVisible]
        public string readString()
        {
            return "";
        }

        [PhpVisible]
        public bool setParserProperty(int property, bool value)
        {
            return true;
        }

        [PhpVisible]
        public bool setRelaxNGSchema(string filename)
        {
            return true;
        }

        [PhpVisible]
        public bool setRelaxNGSchemaSource(string source)
        {
            return true;
        }

        [PhpVisible]
        public bool setSchema(string filename)
        {
            return true;
        }

        [PhpVisible]
        public bool xml(string source, [Optional] string encoding, [Optional] int options)
        {
            return false;
        }

        #endregion

        #region Representation

        private DOMDocument _domDocument;

        #endregion
    }
}
