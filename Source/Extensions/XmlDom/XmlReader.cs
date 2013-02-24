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
            get { return _reader == null ? "" :
                    (!string.IsNullOrEmpty(_reader.Name) ? _reader.Name : getNodeTypeName());
            }
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
                try
                {
                    XmlReader old = _reader;
                    _reader = null;
                    old.Close();
                }
                catch (Exception)
                {
                }
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
            return (_reader != null && _reader.NodeType == XmlNodeType.Element) ? _reader.GetAttribute(name) : "";
        }

        [PhpVisible]
        public string getAttributeNo(int index)
        {
            return (_reader != null && _reader.NodeType == XmlNodeType.Element) ? _reader.GetAttribute(index) : "";
        }

        [PhpVisible]
        public string getAttributeNs(string localName, string namespaceURI)
        {
            return (_reader != null && _reader.NodeType == XmlNodeType.Element) ? _reader.GetAttribute(localName, namespaceURI) : "";
        }

        [PhpVisible]
        public bool getParserProperty(int property)
        {
            bool oldValue;
            return _parserProperties.TryGetValue(property, out oldValue) && oldValue;
        }

        [PhpVisible]
        public bool isValid()
        {
            return _reader != null && _reader.ReadState != ReadState.Error;
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
            if (string.IsNullOrWhiteSpace(URI))
            {
                //TODO: Get current file and line.
                Console.WriteLine("Warning: XMLReader::open(): Empty string supplied as input in %s on line %d");
                return false;
            }

            _source = URI;
            _uriSource = true;
            _encoding = encoding;
            _options = options;
            return createReader();
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
        public bool setParserProperty(int property, bool newValue)
        {
            if (_reader == null)
            {
                return false;
            }

            bool oldValue;
            if (!_parserProperties.TryGetValue(property, out oldValue) ||
                oldValue != newValue)
            {
                _parserProperties[property] = newValue;
                return createReader();
            }

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
            if (string.IsNullOrWhiteSpace(source))
            {
                //TODO: Get current file and line.
                Console.Write("Warning: XMLReader::XML(): Empty string supplied as input in %s on line %d");
                return false;
            }

            _source = source;
            _uriSource = false;
            _encoding = encoding;
            _options = options;
            return createReader();
        }

        #endregion

        #region Implementation

        protected int getAttributeCount()
        {
            return _reader != null ? _reader.AttributeCount : 0;
        }

        private XmlReaderSettings createSettings()
        {
            var settings = new XmlReaderSettings();

            settings.ValidationType = getParserProperty(VALIDATE) ? ValidationType.DTD : ValidationType.None;
            settings.DtdProcessing = getParserProperty(LOADDTD) ? DtdProcessing.Parse : DtdProcessing.Ignore;
            settings.XmlResolver = new XmlUrlResolver();
            return settings;
        }

        private bool createReader()
        {
            close();
            try
            {
                var settings = createSettings();
                _reader = _uriSource ? XmlReader.Create(_source, settings) : XmlReader.Create(new StringReader(_source), settings);

                initialize();
                return true;
            }
            catch (Exception ex)
            {
                Console.Write("Error: " + ex.ToString());
                close();
            }

            return false;
        }

        private void initialize()
        {
            // Prime.
            read();
            if ("xml".EqualsOrdinalIgnoreCase(_reader.Name))
            {
                read();
            }
        }

        private string getNodeTypeName()
        {
            if (_reader == null)
            {
                return "";
            }

            switch ((int)_reader.NodeType)
            {
                case NONE:
                    return "#none";
                case ELEMENT:
                    return "#element";
                case ATTRIBUTE:
                    return "#attribute";
                case TEXT:
                    return "#text";
                case CDATA:
                    return "#cdata";
                case ENTITY_REF:
                    return "#entityref";
                case ENTITY:
                    return "#entity";
                case PI:
                    return "#pi";
                case COMMENT:
                    return "#comment";
                case DOC:
                    return "#doc";
                case DOC_TYPE:
                    return "#doctype";
                case DOC_FRAGMENT:
                    return "#docfragment";
                case NOTATION:
                    return "#notation";
                case WHITESPACE:
                    return "";
                case SIGNIFICANT_WHITESPACE:
                    return "";
                case END_ELEMENT:
                    return "#endelement";
                case END_ENTITY:
                    return "#endentity";
                case XML_DECLARATION:
                    return "#xmldeclaration";
            }

            return "";
        }

        #endregion

        #region Representation

        private XmlReader _reader;

        private readonly Dictionary<int, bool> _parserProperties = new Dictionary<int, bool>(4);
        private string _source;
        private string _encoding;
        private int _options;
        private bool _uriSource;

        #endregion
    }
}
