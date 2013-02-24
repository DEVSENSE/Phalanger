/*

 Copyright (c) 2013 Ashod Nakashian.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Xml;
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
            get { return getAttributeCount(); }
        }

        /// <summary>
        /// The base URI of the node.
        /// </summary>
        [PhpVisible]
        public object baseURI
        {
            get { return _reader != null ? _reader.BaseURI : ""; }
        }

        /// <summary>
        /// Depth of the node in the tree, starting at 0.
        /// </summary>
        [PhpVisible]
        public object depth
        {
            get { return _reader != null ? _reader.Depth : 0; }
        }

        /// <summary>
        /// Indicates if node has attributes.
        /// </summary>
        [PhpVisible]
        public object hasAttributes
        {
            get { return _reader != null && _reader.HasAttributes; }
        }

        /// <summary>
        /// Indicates if node has a text value.
        /// </summary>
        [PhpVisible]
        public object hasValue
        {
            get { return _reader != null && _reader.HasValue; }
        }

        /// <summary>
        /// Indicates if attribute is defaulted from DTD.
        /// </summary>
        [PhpVisible]
        public object isDefault
        {
            get { return _reader != null && _reader.IsDefault; }
        }

        /// <summary>
        /// Indicates if node is an empty element tag.
        /// </summary>
        [PhpVisible]
        public object isEmptyElement
        {
            get { return _reader != null && _reader.IsEmptyElement; }
        }

        /// <summary>
        /// The local name of the node.
        /// </summary>
        [PhpVisible]
        public object localName
        {
            get { return _reader != null ? _reader.LocalName : ""; }
        }

        /// <summary>
        /// The qualified name of the node.
        /// </summary>
        [PhpVisible]
        public object name
        {
            get { return _reader != null ? _reader.Name : ""; }
        }

        /// <summary>
        /// The URI of the namespace associated with the node.
        /// </summary>
        [PhpVisible]
        public object namespaceURI
        {
            get { return _reader != null ? _reader.NamespaceURI : ""; }
        }

        /// <summary>
        /// The node type for the node.
        /// </summary>
        [PhpVisible]
        public object nodeType
        {
            get { return _reader != null ? (int)_reader.NodeType : 0; }
        }

        /// <summary>
        /// The prefix of the namespace associated with the node.
        /// </summary>
        [PhpVisible]
        public object prefix
        {
            get { return _reader != null ? _reader.Prefix : ""; }
        }

        /// <summary>
        /// The text value of the node.
        /// </summary>
        [PhpVisible]
        public object value
        {
            get { return _reader != null ? _reader.Value : ""; }
        }

        /// <summary>
        /// The xml:lang scope which the node resides.
        /// </summary>
        [PhpVisible]
        public object xmlLang
        {
            get { return _reader != null ? _reader.XmlLang : ""; }
        }

        #endregion

        #region Methods
        
        [PhpVisible]
        public bool close()
        {
            if (_reader != null)
            {
                XmlReader old = _reader;
                _reader = null;
                old.Close();
            }

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
            return _reader != null ? _reader.GetAttribute(name) : "";
        }

        [PhpVisible]
        public string getAttributeNo(int index)
        {
            return _reader != null ? _reader.GetAttribute(index) : "";
        }

        [PhpVisible]
        public string getAttributeNs(string localName, string namespaceURI)
        {
            return _reader != null ? _reader.GetAttribute(localName, namespaceURI) : "";
        }

        [PhpVisible]
        public bool getParserProperty(int property)
        {
            return true;
        }

        [PhpVisible]
        public bool isValid()
        {
            return _reader != null;
        }

        [PhpVisible]
        public bool lookupNamespace(string prefix)
        {
            return _reader != null && _reader.LookupNamespace(prefix) != null;
        }

        [PhpVisible]
        public bool moveToAttribute(string name)
        {
            return _reader.MoveToAttribute(name);
        }

        [PhpVisible]
        public bool moveToAttributeNo(int index)
        {
            if (_reader == null || index < 0 || index >= getAttributeCount())
            {
                return false;
            }

            moveToElement();
            moveToFirstAttribute();
            int j = 0;
            while (j < index)
            {
                _reader.MoveToNextAttribute();
                ++j;
            }

            return j < index;
        }

        [PhpVisible]
        public bool moveToAttributeNs(string localName, string namespaceURI)
        {
            return _reader != null && _reader.MoveToAttribute(localName, namespaceURI);
        }

        [PhpVisible]
        public bool moveToElement()
        {
            return _reader != null && _reader.MoveToElement();
        }

        [PhpVisible]
        public bool moveToFirstAttribute()
        {
            return _reader != null && _reader.MoveToFirstAttribute();
        }

        [PhpVisible]
        public bool moveToNextAttribute()
        {
            return _reader != null && _reader.MoveToNextAttribute();
        }

        [PhpVisible]
        public bool next([Optional] string localname)
        {
            _reader.Skip();
            if (string.IsNullOrEmpty(localname))
            {
                return !_reader.EOF;
            }

            while (_reader.LocalName != localname && !_reader.EOF)
            {
                _reader.Skip();
            }

            return _reader.LocalName == localname && !_reader.EOF;
        }

        [PhpVisible]
        public bool open(string URI, [Optional] string encoding, [Optional] int options)
        {
            return true;
        }

        [PhpVisible]
        public bool read()
        {
            return _reader != null && _reader.Read();
        }

        [PhpVisible]
        public string readInnerXML()
        {
            return _reader != null ? _reader.ReadInnerXml() : "";
        }

        [PhpVisible]
        public string readOuterXML()
        {
            return _reader != null ? _reader.ReadOuterXml() : "";
        }

        [PhpVisible]
        public string readString()
        {
            return _reader != null ? _reader.ReadString() : "";
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
            try
            {
                close();

                if (string.IsNullOrWhiteSpace(source))
                {
                    //TODO: Get current file and line.
                    Console.Write("Warning: XMLReader::XML(): Empty string supplied as input in %s on line %d");
                    return false;
                }

                _reader = XmlReader.Create(new StringReader(source));

                // Prime.
                read();
                if ("xml".EqualsOrdinalIgnoreCase(_reader.Name))
                {
                    read();
                }

                return true;
            }
            catch (Exception)
            {
            }

            return false;
        }

        #endregion

        protected int getAttributeCount()
        {
            return _reader != null ? _reader.AttributeCount : 0;
        }

        #region Representation

        private XmlReader _reader;

        #endregion
    }
}
