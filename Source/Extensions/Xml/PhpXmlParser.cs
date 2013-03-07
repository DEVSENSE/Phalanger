using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PHP.Core;
using PHP.Core.Reflection;

namespace PHP.Library.Xml
{
    public enum XmlParserError
    {
        [ImplementsConstant("XML_ERROR_NONE")]
		XML_ERROR_NONE = 0,

        [ImplementsConstant("XML_ERROR_GENERIC")]
        XML_ERROR_GENERIC = 1,

        [ImplementsConstant("XML_ERROR_NO_MEMORY")]
        XML_ERROR_NO_MEMORY = 1,

        [ImplementsConstant("XML_ERROR_SYNTAX")]
        XML_ERROR_SYNTAX = 1,

        [ImplementsConstant("XML_ERROR_NO_ELEMENTS")]
        XML_ERROR_NO_ELEMENTS = 1,

        [ImplementsConstant("XML_ERROR_INVALID_TOKEN")]
        XML_ERROR_INVALID_TOKEN = 1,

        [ImplementsConstant("XML_ERROR_UNCLOSED_TOKEN")]
        XML_ERROR_UNCLOSED_TOKEN = 1,

        [ImplementsConstant("XML_ERROR_PARTIAL_CHAR")]
        XML_ERROR_PARTIAL_CHAR = 1,

        [ImplementsConstant("XML_ERROR_TAG_MISMATCH")]
        XML_ERROR_TAG_MISMATCH = 1,

        [ImplementsConstant("XML_ERROR_DUPLICATE_ATTRIBUTE")]
        XML_ERROR_DUPLICATE_ATTRIBUTE = 1,

        [ImplementsConstant("XML_ERROR_JUNK_AFTER_DOC_ELEMENT")]
        XML_ERROR_JUNK_AFTER_DOC_ELEMENT = 1,

        [ImplementsConstant("XML_ERROR_PARAM_ENTITY_REF")]
        XML_ERROR_PARAM_ENTITY_REF = 1,

        [ImplementsConstant("XML_ERROR_UNDEFINED_ENTITY")]
        XML_ERROR_UNDEFINED_ENTITY = 1,

        [ImplementsConstant("XML_ERROR_RECURSIVE_ENTITY_REF")]
        XML_ERROR_RECURSIVE_ENTITY_REF = 1,

        [ImplementsConstant("XML_ERROR_ASYNC_ENTITY")]
        XML_ERROR_ASYNC_ENTITY = 1,

        [ImplementsConstant("XML_ERROR_BAD_CHAR_REF")]
        XML_ERROR_BAD_CHAR_REF = 1,

        [ImplementsConstant("XML_ERROR_BINARY_ENTITY_REF")]
        XML_ERROR_BINARY_ENTITY_REF = 1,

        [ImplementsConstant("XML_ERROR_ATTRIBUTE_EXTERNAL_ENTITY_REF")]
        XML_ERROR_ATTRIBUTE_EXTERNAL_ENTITY_REF = 1,

        [ImplementsConstant("XML_ERROR_MISPLACED_XML_PI")]
        XML_ERROR_MISPLACED_XML_PI = 1,

        [ImplementsConstant("XML_ERROR_UNKNOWN_ENCODING")]
        XML_ERROR_UNKNOWN_ENCODING = 1,

        [ImplementsConstant("XML_ERROR_INCORRECT_ENCODING")]
        XML_ERROR_INCORRECT_ENCODING = 1,

        [ImplementsConstant("XML_ERROR_UNCLOSED_CDATA_SECTION")]
        XML_ERROR_UNCLOSED_CDATA_SECTION = 1,

        [ImplementsConstant("XML_ERROR_EXTERNAL_ENTITY_HANDLING")]
        XML_ERROR_EXTERNAL_ENTITY_HANDLING = 1
    }

    public enum XmlOption
    {
        [ImplementsConstant("XML_OPTION_CASE_FOLDING")]
        XML_OPTION_CASE_FOLDING,
        [ImplementsConstant("XML_OPTION_SKIP_TAGSTART")]
        XML_OPTION_SKIP_TAGSTART,
        [ImplementsConstant("XML_OPTION_SKIP_WHITE")]
        XML_OPTION_SKIP_WHITE,
        [ImplementsConstant("XML_OPTION_TARGET_ENCODING")]
        XML_OPTION_TARGET_ENCODING
    }

    public static class XmlParser
    {
        #region utf8_encode, utf8_decode

        /// <summary>
        /// ISO-8859-1 <see cref="Encoding"/>.
        /// </summary>
        private static Encoding/*!*/ISO_8859_1_Encoding
        {
            get
            {
                if (_ISO_8859_1_Encoding == null)
                    _ISO_8859_1_Encoding = Encoding.GetEncoding("ISO-8859-1");
                
                return _ISO_8859_1_Encoding;
            }
        }
        private static Encoding _ISO_8859_1_Encoding = null;

        /// <summary>
        /// This function encodes the string data to UTF-8, and returns the encoded version. UTF-8 is
        /// a standard mechanism used by Unicode for encoding wide character values into a byte stream.
        /// UTF-8 is transparent to plain ASCII characters, is self-synchronized (meaning it is 
        /// possible for a program to figure out where in the bytestream characters start) and can be
        /// used with normal string comparison functions for sorting and such. PHP encodes UTF-8
        /// characters in up to four bytes.
        /// </summary>
        /// <param name="data">An ISO-8859-1 string. </param>
        /// <returns>Returns the UTF-8 translation of data.</returns>
        [ImplementsFunction("utf8_encode")]
        //[return:CastToFalse]
        public static object utf8_encode(object data)
        {
            if (data == null)
                return string.Empty;

            // this function transforms ISO-8859-1 binary string into UTF8 string
            // since our internal representation is native CLR string - UTF16, we have changed this semantic
            
            string encoded;

            if (data.GetType() == typeof(string))
            {
                encoded = (string)data;
            }
            else if (data.GetType() == typeof(PhpBytes))
            {
                // if we got binary string, assume it's ISO-8859-1 encoded string and convert it to System.String
                encoded = ISO_8859_1_Encoding.GetString(((PhpBytes)data).ReadonlyData);
            }
            else
            {
                encoded = Core.Convert.ObjectToString(data);
            }

            // return utf8 encoded data
            return (Configuration.Application.Globalization.PageEncoding == Encoding.UTF8) ?
                (object)encoded : // PageEncoding is UTF8, we can keep .NET string, which will be converted to UTF8 byte stream as it would be needed
                (object)new PhpBytes(Encoding.UTF8.GetBytes(encoded));   // conversion of string to byte[] would not respect UTF8 encoding, convert it now
        }

        /// <summary>
        /// This function decodes data, assumed to be UTF-8 encoded, to ISO-8859-1.
        /// </summary>
        /// <param name="data">An ISO-8859-1 string. </param>
        /// <returns>Returns the UTF-8 translation of data.</returns>
        [ImplementsFunction("utf8_decode")]
        public static PhpBytes utf8_decode(object data)
        {
            if (data == null)
                return new PhpBytes();  // empty (binary) string

            // this function converts the UTF8 representation to ISO-8859-1 representation
            // we assume CLR string (UTF16) as input as it is our internal representation

            if (data.GetType() == typeof(PhpBytes))
            {
                // if we got binary string, assume it's UTF-8 encoded and perform byte[] -(UTF-8)-> string -(ISO-8859-1)-> byte[] transformation
                // consider replacing this with something more efficient
                return new PhpBytes(ISO_8859_1_Encoding.GetBytes(Encoding.UTF8.GetString(((PhpBytes)data).ReadonlyData)));
            }
            else //if (data.GetType() == typeof(string))
            {
                var str = Core.Convert.ObjectToString(data);
                // if we got System.String string, convert it from UTF16 CLR representation into ISO-8859-1 binary representation
                return new PhpBytes(ISO_8859_1_Encoding.GetBytes(str));
            }
        }

        #endregion

        #region xml_parser_create_ns, xml_parser_create, xml_parser_free

        /// <summary>
        /// Creates a new XML parser with XML namespace support and returns a resource handle referencing
        /// it to be used by the other XML functions. 
        /// </summary>
        /// <returns>Returns a resource handle for the new XML parser.</returns>
        [ImplementsFunction("xml_parser_create_ns")]
        public static PhpResource CreateNamespaceAware()
        {
            return CreateNamespaceAware("UTF-8");
        }

        /// <summary>
        /// Creates a new XML parser with XML namespace support and returns a resource handle referencing
        /// it to be used by the other XML functions. 
        /// </summary>
        /// <param name="encoding">
        /// The optional encoding specifies the character encoding for the input/output in PHP 4. Starting
        /// from PHP 5, the input encoding is automatically detected, so that the encoding parameter
        /// specifies only the output encoding. In PHP 4, the default output encoding is the same as the
        /// input charset. In PHP 5.0.0 and 5.0.1, the default output charset is ISO-8859-1, while in PHP
        /// 5.0.2 and upper is UTF-8. The supported encodings are ISO-8859-1, UTF-8 and US-ASCII. 
        /// </param>
        /// <returns>Returns a resource handle for the new XML parser.</returns>
        [ImplementsFunction("xml_parser_create_ns")]
        public static PhpResource CreateNamespaceAware(string encoding)
        {
            return CreateNamespaceAware(encoding, ":");
        }

        /// <summary>
        /// Creates a new XML parser with XML namespace support and returns a resource handle referencing
        /// it to be used by the other XML functions. 
        /// </summary>
        /// <param name="encoding">
        /// The optional encoding specifies the character encoding for the input/output in PHP 4. Starting
        /// from PHP 5, the input encoding is automatically detected, so that the encoding parameter
        /// specifies only the output encoding. In PHP 4, the default output encoding is the same as the
        /// input charset. In PHP 5.0.0 and 5.0.1, the default output charset is ISO-8859-1, while in PHP
        /// 5.0.2 and upper is UTF-8. The supported encodings are ISO-8859-1, UTF-8 and US-ASCII. 
        /// </param>
        /// <param name="namespaceSeparator">
        /// With a namespace aware parser tag parameters passed to the various handler functions will 
        /// consist of namespace and tag name separated by the string specified in seperator.
        /// </param>
        /// <returns>Returns a resource handle for the new XML parser.</returns>
        [ImplementsFunction("xml_parser_create_ns")]
        public static PhpResource CreateNamespaceAware(string encoding, string namespaceSeparator)
        {
            return new XmlParserResource(Encoding.GetEncoding(encoding), true, namespaceSeparator);
        }

        /// <summary>
        /// Creates a new XML parser and returns a resource handle referencing it to be used by the other
        /// XML functions. 
        /// </summary>
        /// <returns>Returns a resource handle for the new XML parser.</returns>
        [ImplementsFunction("xml_parser_create")]
        public static PhpResource Create()
        {
            return Create("UTF-8");
        }

        /// <summary>
        /// Creates a new XML parser and returns a resource handle referencing it to be used by the other
        /// XML functions. 
        /// </summary>
        /// <param name="encoding">
        /// The optional encoding specifies the character encoding for the input/output in PHP 4. Starting
        /// from PHP 5, the input encoding is automatically detected, so that the encoding parameter
        /// specifies only the output encoding. In PHP 4, the default output encoding is the same as the
        /// input charset. If empty string is passed, the parser attempts to identify which encoding the
        /// document is encoded in by looking at the heading 3 or 4 bytes. In PHP 5.0.0 and 5.0.1, the
        /// default output charset is ISO-8859-1, while in PHP 5.0.2 and upper is UTF-8. The supported
        /// encodings are ISO-8859-1, UTF-8 and US-ASCII. 
        /// </param>
        /// <returns>Returns a resource handle for the new XML parser.</returns>
        [ImplementsFunction("xml_parser_create")]
        public static PhpResource Create(string encoding)
        {
            return new XmlParserResource(Encoding.GetEncoding(encoding), false, null);
        }

        /// <summary>
        /// Frees the given XML parser. 
        /// </summary>
        /// <param name="parser">A reference to the XML parser to free.</param>
        /// <returns>
        /// This function returns FALSE if parser does not refer to a valid parser, or else it frees the 
        /// parser and returns TRUE.
        /// </returns>
        [ImplementsFunction("xml_parser_free")]
        public static bool Free(PhpResource parser)
        {
            XmlParserResource xmlParser = XmlParserResource.ValidResource(parser);
            if (xmlParser == null)
                return false;

            // Since .NET hasn't online XML parser, we need the whole XML data to parse them properly.
            // Notice user, he has to parse the XML by passing is_final=true to the last xml_parse function call.
            if (!xmlParser.InputQueueIsEmpty)
                PhpException.Throw(PhpError.Notice, Strings.not_parsed_data_left);

            xmlParser.Dispose();
            return true;

        }
        #endregion

        #region xml_parse, xml_parse_into_struct

        /// <summary>
        /// Parses an XML document. The handlers for the configured events are called as many times as 
        /// necessary. 
        /// </summary>
        /// <param name="namingContext">Current namign context.</param>
        /// <param name="caller">Current caller desc.</param>
        /// <param name="parser">A reference to the XML parser to use.</param>
        /// <param name="data">
        /// Chunk of data to parse. A document may be parsed piece-wise by calling xml_parse() several 
        /// times with new data, as long as the is_final parameter is set and TRUE when the last data is 
        /// parsed. 
        /// </param>
        /// <returns>
        /// <para>Returns 1 on success or 0 on failure.</para>
        /// <para>
        /// For unsuccessful parses, error information can be retrieved with xml_get_error_code(), 
        /// xml_error_string(), xml_get_current_line_number(), xml_get_current_column_number() and 
        /// xml_get_current_byte_index(). 
        /// </para>
        /// </returns>
        [ImplementsFunction("xml_parse", FunctionImplOptions.NeedsClassContext | FunctionImplOptions.NeedsNamingContext)]
        public static int Parse(NamingContext/*!*/ namingContext, DTypeDesc caller, PhpResource parser, string data)
        {
            return Parse(namingContext, caller, parser, data, false);
        }

        /// <summary>
        /// Parses an XML document. The handlers for the configured events are called as many times as 
        /// necessary. 
        /// </summary>
        /// <param name="namingContext">Current namign context.</param>
        /// <param name="caller">Current caller desc.</param>
        /// <param name="parser">A reference to the XML parser to use.</param>
        /// <param name="data">
        /// Chunk of data to parse. A document may be parsed piece-wise by calling xml_parse() several 
        /// times with new data, as long as the is_final parameter is set and TRUE when the last data is 
        /// parsed. 
        /// </param>
        /// <param name="is_final">If set and TRUE, data is the last piece of data sent in this parse.</param>
        /// <returns>
        /// <para>Returns 1 on success or 0 on failure.</para>
        /// <para>
        /// For unsuccessful parses, error information can be retrieved with xml_get_error_code(), 
        /// xml_error_string(), xml_get_current_line_number(), xml_get_current_column_number() and 
        /// xml_get_current_byte_index(). 
        /// </para>
        /// </returns>
        [ImplementsFunction("xml_parse", FunctionImplOptions.NeedsClassContext | FunctionImplOptions.NeedsNamingContext)]
        public static int Parse(NamingContext/*!*/ namingContext, DTypeDesc caller, PhpResource parser, string data, bool is_final)
        {
            XmlParserResource xmlParser = XmlParserResource.ValidResource(parser);

            if (xmlParser != null)
            {
                return xmlParser.Parse(caller, namingContext, data, is_final) ? 1 : 0;
            }

            return 0;
        }

        /// <summary>
        /// This function parses an XML string into 2 parallel array structures, one (index) containing
        /// pointers to the location of the appropriate values in the values array. These last two 
        /// parameters must be passed by reference. 
        /// </summary>
        /// <param name="namingContext">Current namign context.</param>
        /// <param name="caller">Current caller desc.</param>
        /// <param name="parser">A reference to the XML parser. </param>
        /// <param name="data">A string containing the XML data. </param>
        /// <param name="values">An array containing the values of the XML data.</param>
        /// <returns>
        /// Returns 0 for failure and 1 for success. This is not the same as FALSE and TRUE, be careful
        /// with operators such as ===.
        /// </returns>
        [ImplementsFunction("xml_parse_into_struct", FunctionImplOptions.NeedsClassContext | FunctionImplOptions.NeedsNamingContext)]
        public static int ParseIntoStruct(NamingContext/*!*/ namingContext, DTypeDesc caller, PhpResource parser, string data, PhpReference values)
        {
            return ParseIntoStruct(namingContext, caller, parser, data, values, null);
        }

        /// <summary>
        /// This function parses an XML string into 2 parallel array structures, one (index) containing
        /// pointers to the location of the appropriate values in the values array. These last two 
        /// parameters must be passed by reference. 
        /// </summary>
        /// <param name="namingContext">Current namign context.</param>
        /// <param name="caller">Current caller desc.</param>
        /// <param name="parser">A reference to the XML parser. </param>
        /// <param name="data">A string containing the XML data. </param>
        /// <param name="values">An array containing the values of the XML data.</param>
        /// <param name="index">
        /// An array containing pointers to the location of the appropriate values in the $values.
        /// </param>
        /// <returns>
        /// Returns 0 for failure and 1 for success. This is not the same as FALSE and TRUE, be careful
        /// with operators such as ===.
        /// </returns>
        [ImplementsFunction("xml_parse_into_struct", FunctionImplOptions.NeedsClassContext | FunctionImplOptions.NeedsNamingContext)]
        public static int ParseIntoStruct(NamingContext/*!*/ namingContext, DTypeDesc caller, PhpResource parser, string data, PhpReference values, PhpReference index)
        {
            if (values == null)
            {
                PhpException.Throw(PhpError.Warning, "values argument should not be null");
                return 0;
            }

            values.Value = new PhpArray();
            if (index != null) index.Value = new PhpArray();

            XmlParserResource xmlParser = parser as XmlParserResource;

            if (xmlParser != null)
            {
                return xmlParser.ParseIntoStruct(caller, namingContext, data, (PhpArray)values.Value, index != null ? (PhpArray)index.Value : null) ? 1 : 0;
            }

            PhpException.Throw(PhpError.Warning, "parser argument should contain valid XML parser");
            return 0;
        }

        #endregion

        #region xml_parser_get_option, xml_parser_set_option

        /// <summary>
        /// Sets an option in an XML parser. 
        /// </summary>
        /// <param name="parser">A reference to the XML parser to set an option in. </param>
        /// <param name="option">
        /// One of the following options: XML_OPTION_CASE_FOLDING, XML_OPTION_SKIP_TAGSTART,
        /// XML_OPTION_SKIP_WHITE, XML_OPTION_TARGET_ENCODING.
        /// </param>
        /// <param name="value">The option's new value. </param>
        /// <returns>
        /// This function returns FALSE if parser does not refer to a valid parser, or if the option could
        /// not be set. Else the option is set and TRUE is returned.
        /// </returns>
        [ImplementsFunction("xml_parser_set_option")]
        public static bool SetOption(PhpResource parser, int option, object value)
        {
            XmlParserResource xmlParser = parser as XmlParserResource;

            if (xmlParser != null)
            {
                switch ((XmlOption)option)
                {
                    case XmlOption.XML_OPTION_CASE_FOLDING:
                        xmlParser.EnableCaseFolding = Core.Convert.ObjectToBoolean(value);
                        return true;
                    case XmlOption.XML_OPTION_SKIP_WHITE:
                        xmlParser.EnableSkipWhitespace = Core.Convert.ObjectToBoolean(value);
                        return true;
                    case XmlOption.XML_OPTION_SKIP_TAGSTART:
                        break;
                    case XmlOption.XML_OPTION_TARGET_ENCODING:
                        break;
                }

                PhpException.Throw(PhpError.Warning, "invalid option value");
                return false;
            }
            else
            {
                PhpException.Throw(PhpError.Warning, "invalid parser");
                return false;

            }
        }

        /// <summary>
        /// Gets an option value from an XML parser. 
        /// </summary>
        /// <param name="parser">A reference to the XML parser to get an option from. </param>
        /// <param name="option">
        /// Which option to fetch. XML_OPTION_CASE_FOLDING and XML_OPTION_TARGET_ENCODING are available.
        /// </param>
        /// <returns>
        /// This function returns FALSE if parser does not refer to a valid parser or if option isn't valid
        /// (generates also a E_WARNING). Else the option's value is returned. 
        /// </returns>
        [ImplementsFunction("xml_parser_get_option")]
        public static object GetOption(PhpResource parser, int option)
        {
            XmlParserResource xmlParser = parser as XmlParserResource;

            if (xmlParser != null)
            {
                switch ((XmlOption)option)
                {
                    case XmlOption.XML_OPTION_CASE_FOLDING:
                        return xmlParser.EnableCaseFolding;
                    case XmlOption.XML_OPTION_SKIP_WHITE:
                        return xmlParser.EnableSkipWhitespace;
                    case XmlOption.XML_OPTION_SKIP_TAGSTART:
                        break;
                    case XmlOption.XML_OPTION_TARGET_ENCODING:
                        break;
                }

                PhpException.Throw(PhpError.Warning, "invalid option value");
                return false;
            }
            else
            {
                PhpException.Throw(PhpError.Warning, "invalid parser");
                return false;

            }
        }

        #endregion

        #region xml_error_string, xml_get_error_code

        /// <summary>
        /// Gets the XML parser error string associated with the given code.
        /// </summary>
        /// <param name="code">An error code from xml_get_error_code().</param>
        /// <returns>
        /// Returns a string with a textual description of the error code, or FALSE if no description 
        /// was found.
        /// </returns>
        [ImplementsFunction("xml_error_string")]
        [return: CastToFalse]
        public static string ErrorString(int code)
        {
            if (code == (int)XmlParserError.XML_ERROR_GENERIC)
                return "Generic XML parser error - Phalanger does not currently support error strings.";
            else if (code == (int)XmlParserError.XML_ERROR_NONE)
                return "No Error.";
            else
                return "Unknown XML parser error.";
        }

        /// <summary>
        /// Gets the XML parser error code. 
        /// </summary>
        /// <param name="parser">A reference to the XML parser to get error code from.</param>
        /// <returns>
        /// This function returns FALSE if parser does not refer to a valid parser, or else it returns 
        /// one of the error codes.
        /// </returns>
        [ImplementsFunction("xml_get_error_code")]
        public static object GetErrorCode(PhpResource parser)
        {
            XmlParserResource xmlParser = parser as XmlParserResource;

            if (xmlParser != null)
            {
                return xmlParser.ErrorCode;
            }

            return false;
        }

        #endregion

        #region xml_get_current_byte_index, xml_get_current_column_number, xml_get_current_line_number

        /// <summary>
        /// Gets the current byte index of the given XML parser. 
        /// </summary>
        /// <param name="parser">A reference to the XML parser to get byte index from.</param>
        /// <returns>
        /// This function returns FALSE if parser does not refer to a valid parser, or else it returns 
        /// which byte index the parser is currently at in its data buffer (starting at 0). 
        /// </returns>
        [ImplementsFunction("xml_get_current_byte_index")]
        [return: CastToFalse]
        public static int GetCurrentByteIndex(PhpResource parser)
        {
            XmlParserResource xmlParser = parser as XmlParserResource;

            if (xmlParser != null)
            {
                return xmlParser.CurrentByteIndex;
            }

            return -1;
        }

        /// <summary>
        /// Gets the current column number of the given XML parser. 
        /// </summary>
        /// <param name="parser">A reference to the XML parser to get column number from. </param>
        /// <returns>
        /// This function returns FALSE if parser does not refer to a valid parser, or else it returns 
        /// which column on the current line (as given by xml_get_current_line_number()) the parser is 
        /// currently at. 
        /// </returns>
        [ImplementsFunction("xml_get_current_column_number")]
        [return: CastToFalse]
        public static int GetCurrentColumnNumber(PhpResource parser)
        {
            XmlParserResource xmlParser = parser as XmlParserResource;

            if (xmlParser != null)
            {
                return xmlParser.CurrentColumnNumber;
            }

            return -1;
        }

        /// <summary>
        /// Gets the current line number for the given XML parser. 
        /// </summary>
        /// <param name="parser">A reference to the XML parser to get line number from.</param>
        /// <returns>
        /// This function returns FALSE if parser does not refer to a valid parser, or else it returns 
        /// which line the parser is currently at in its data buffer. 
        /// </returns>
        [ImplementsFunction("xml_get_current_line_number")]
        [return: CastToFalse]
        public static int GetCurrentLineNumber(PhpResource parser)
        {
            XmlParserResource xmlParser = parser as XmlParserResource;

            if (xmlParser != null)
            {
                return xmlParser.CurrentLineNumber;
            }

            return -1;
        }

        #endregion

        #region xml_set_object
        
        /// <summary>
        /// This function allows to use parser inside object. All callback functions could be set with 
        /// xml_set_element_handler() etc and assumed to be methods of object. 
        /// </summary>
        /// <param name="parser">A reference to the XML parser to use inside the object. </param>
        /// <param name="objRef">The object where to use the XML parser.</param>
        /// <returns>Returns TRUE on success or FALSE on failure. </returns>
        [ImplementsFunction("xml_set_object")]
        public static bool SetObject(PhpResource parser, PhpReference objRef)
        {
            XmlParserResource xmlParser = parser as XmlParserResource;

            if (xmlParser != null)
            {
                DObject dObject = objRef != null ? objRef.Value as DObject : null;

                if (dObject != null)
                {
                    xmlParser.HandlerObject = dObject;
                }
                else
                {
                    xmlParser.HandlerObject = null;
                }

                return true;
            }

            return false;
        }

        #endregion

        #region xml_set_default_handler, xml_set_unparsed_entity_decl_handler
        /// <summary>
        /// Sets the default handler function for the XML parser parser.
        /// </summary>
        /// <param name="parser">
        /// A reference to the XML parser to set up default handler function. 
        /// </param>
        /// <param name="default_handler_obj">
        /// String (or array) containing the name of a function that must exist when xml_parse() is 
        /// called for parser. 
        /// </param>
        /// <returns>Returns TRUE on success or FALSE on failure.</returns>
        [ImplementsFunction("xml_set_default_handler")]
        public static bool SetDefaultHandler(PhpResource parser, object default_handler_obj)
        {
            XmlParserResource xmlParser = XmlParserResource.ValidResource(parser);
            if (xmlParser == null)
                return false;

            var default_handler = xmlParser.ObjectToXmlCallback(default_handler_obj);

            if (default_handler != null)
            {
                xmlParser.DefaultHandler = default_handler;

                return true;
            }

            return false;
        }

        /// <summary>
        /// Sets the unparsed entity declaration handler function for the XML parser parser. 
        /// </summary>
        /// <param name="parser">
        /// A reference to the XML parser to set up unparsed entity declaration handler function. 
        /// </param>
        /// <param name="unparsed_entity_decl_handler_obj">
        /// String (or array) containing the name of a function that must exist when xml_parse() is 
        /// called for parser. 
        /// </param>
        /// <returns>Returns TRUE on success or FALSE on failure. </returns>
        [ImplementsFunction("xml_set_unparsed_entity_decl_handler", FunctionImplOptions.NotSupported)]
        public static bool SetUnparsedEntityDeclHandler(PhpResource parser, object unparsed_entity_decl_handler_obj)
        {
            XmlParserResource xmlParser = XmlParserResource.ValidResource(parser);
            if (xmlParser == null)
                return false;

            PhpException.FunctionNotSupported(PhpError.Warning);
            return false;
        }

        #endregion

        #region xml_set_element_handler, xml_set_character_data_handler
        /// <summary>
        /// Sets the element handler functions for the XML parser. start_element_handler and 
        /// end_element_handler are strings containing the names of functions that must exist 
        /// when xml_parse() is called for parser.  
        /// </summary>
        /// <param name="parser">
        /// A reference to the XML parser to set up start and end element handler functions. 
        /// </param>
        /// <param name="start_element_handler_obj">
        /// String (or array) containing the name of a function that must exist when xml_parse() is 
        /// called for parser. 
        /// </param>
        /// <param name="end_element_handler_obj">
        /// String (or array) containing the name of a function that must exist when xml_parse() is 
        /// called for parser. 
        /// </param>        
        /// <returns>Returns TRUE on success or FALSE on failure. </returns>
        [ImplementsFunction("xml_set_element_handler")]
        public static bool SetElementHandler(PhpResource parser, object start_element_handler_obj, object end_element_handler_obj)
        {
            XmlParserResource xmlParser = XmlParserResource.ValidResource(parser);
            if (xmlParser == null)
                return false;

            var start_element_handler = xmlParser.ObjectToXmlCallback(start_element_handler_obj);
            var end_element_handler = xmlParser.ObjectToXmlCallback(end_element_handler_obj);

            if (start_element_handler != null && end_element_handler != null)
            {
                xmlParser.StartElementHandler = start_element_handler;
                xmlParser.EndElementHandler = end_element_handler;

                return true;
            }

            return false;
        }

        /// <summary>
        /// Sets the character data handler function for the XML parser parser.  
        /// </summary>
        /// <param name="parser">
        /// A reference to the XML parser to set up character data handler function.
        /// </param>
        /// <param name="character_data_handler_obj">
        /// String (or array) containing the name of a function that must exist when xml_parse() is 
        /// called for parser. 
        /// </param>
        /// <returns>Returns TRUE on success or FALSE on failure. </returns>
        [ImplementsFunction("xml_set_character_data_handler")]
        public static bool SetCharacterDataHandler(PhpResource parser, object character_data_handler_obj)
        {
            XmlParserResource xmlParser = XmlParserResource.ValidResource(parser);
            if (xmlParser == null)
                return false;

            var character_data_handler = xmlParser.ObjectToXmlCallback(character_data_handler_obj);

            if (character_data_handler != null)
            {
                xmlParser.CharacterDataHandler = character_data_handler;

                return true;
            }

            return false;
        }

        #endregion

        #region xml_set_start_namespace_decl_handler, xml_set_end_namespace_decl_handler

        /// <summary>
        /// Set a handler to be called when a namespace is declared. Namespace declarations occur 
        /// inside start tags. But the namespace declaration start handler is called before the start 
        /// tag handler for each namespace declared in that start tag.  
        /// </summary>
        /// <param name="parser">
        /// A reference to the XML parser. 
        /// </param>
        /// <param name="start_namespace_decl_handler_obj">
        /// String (or array) containing the name of a function that must exist when xml_parse() is 
        /// called for parser. 
        /// </param>
        /// <returns>Returns TRUE on success or FALSE on failure. </returns>
        [ImplementsFunction("xml_set_start_namespace_decl_handler")]
        public static bool SetStartNamespaceDeclHandler(PhpResource parser, object start_namespace_decl_handler_obj)
        {
            XmlParserResource xmlParser = XmlParserResource.ValidResource(parser);
            if (xmlParser == null)
                return false;

            var start_namespace_decl_handler = xmlParser.ObjectToXmlCallback(start_namespace_decl_handler_obj);

            if (start_namespace_decl_handler != null)
            {
                xmlParser.StartNamespaceDeclHandler = start_namespace_decl_handler;

                return true;
            }

            return false;
        }

        /// <summary>
        /// Set a handler to be called when leaving the scope of a namespace declaration. This will 
        /// be called, for each namespace declaration, after the handler for the end tag of the 
        /// element in which the namespace was declared. 
        /// </summary>
        /// <param name="parser">
        /// A reference to the XML parser.
        /// </param>
        /// <param name="end_namespace_decl_handler_obj">
        /// String (or array) containing the name of a function that must exist when xml_parse() is 
        /// called for parser. 
        /// </param>
        /// <returns>Returns TRUE on success or FALSE on failure. </returns>
        [ImplementsFunction("xml_set_end_namespace_decl_handler")]
        public static bool SetEndNamespaceDeclHandler(PhpResource parser, object end_namespace_decl_handler_obj)
        {
            XmlParserResource xmlParser = XmlParserResource.ValidResource(parser);
            if (xmlParser == null)
                return false;

            var end_namespace_decl_handler = xmlParser.ObjectToXmlCallback(end_namespace_decl_handler_obj);

            if (end_namespace_decl_handler != null)
            {
                xmlParser.EndNamespaceDeclHandler = end_namespace_decl_handler;

                return true;
            }

            return false;
        }

        #endregion

        #region xml_set_notation_decl_handler, xml_set_processing_instruction_handler, xml_set_external_entity_ref_handler
        /// <summary>
        /// Sets the notation declaration handler function for the XML parser parser. 
        /// </summary>
        /// <param name="parser">
        /// A reference to the XML parser to set up notation declaration handler function. 
        /// </param>
        /// <param name="notation_decl_handler_obj">
        /// String (or array) containing the name of a function that must exist when xml_parse() is 
        /// called for parser. 
        /// </param>
        /// <returns>Returns TRUE on success or FALSE on failure. </returns>
        [ImplementsFunction("xml_set_notation_decl_handler", FunctionImplOptions.NotSupported)]
        public static bool SetNotationDeclHandler(PhpResource parser, object notation_decl_handler_obj)
        {
            XmlParserResource xmlParser = XmlParserResource.ValidResource(parser);
            if (xmlParser == null)
                return false;

            PhpException.FunctionNotSupported(PhpError.Warning);
            return false;
        }

        /// <summary>
        /// Sets the processing instruction (PI) handler function for the XML parser parser. 
        /// </summary>
        /// <param name="parser">
        /// A reference to the XML parser to set up processing instruction (PI) handler function.  
        /// </param>
        /// <param name="processing_instruction_handler_obj">
        /// String (or array) containing the name of a function that must exist when xml_parse() is 
        /// called for parser. 
        /// </param>
        /// <returns>Returns TRUE on success or FALSE on failure. </returns>
        [ImplementsFunction("xml_set_processing_instruction_handler")]
        public static bool SetProcessingInstructionHandler(PhpResource parser, object processing_instruction_handler_obj)
        {
            XmlParserResource xmlParser = XmlParserResource.ValidResource(parser);
            if (xmlParser == null)
                return false;

            var processing_instruction_handler = xmlParser.ObjectToXmlCallback(processing_instruction_handler_obj);

            if (processing_instruction_handler != null)
            {
                xmlParser.ProcessingInstructionHandler = processing_instruction_handler;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Sets the external entity reference handler function for the XML parser parser.  
        /// </summary>
        /// <param name="parser">
        /// A reference to the XML parser to set up external entity reference handler function. 
        /// </param>
        /// <param name="external_entity_ref_handler_obj">
        /// String (or array) containing the name of a function that must exist when xml_parse() is 
        /// called for parser. 
        /// </param>
        /// <returns>Returns TRUE on success or FALSE on failure. </returns>
        [ImplementsFunction("xml_set_external_entity_ref_handler", FunctionImplOptions.NotSupported)]
        public static bool SetExternalEntityRefHandler(PhpResource parser, object external_entity_ref_handler_obj)
        {
            XmlParserResource xmlParser = XmlParserResource.ValidResource(parser);
            if (xmlParser == null)
                return false;

            PhpException.FunctionNotSupported(PhpError.Warning);
            return false;
        }
        #endregion
    }
}
