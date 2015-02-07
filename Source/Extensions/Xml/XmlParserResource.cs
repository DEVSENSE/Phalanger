using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PHP.Core;
using System.Xml;
using System.IO;
using PHP.Core.Reflection;

namespace PHP.Library.Xml
{
    public sealed class XmlParserResource : PhpResource
    {
        private class HandlerWrapper
        {
            public PhpCallback Callback { get; set; }
            public XmlParserResource Parser { get; private set; }
            public string Name { get; private set; }
            private PhpCallback _currentCallback;
            public bool Bound { get; private set; }

            public void BindOrBiteMyLegsOff(DTypeDesc caller, NamingContext namingContext)
            {
                if (Callback != null)
                {
                    if (Callback.TargetInstance == null && Parser._handlerObject != null)
                        _currentCallback = new PhpCallback(Parser._handlerObject, Callback.RoutineName);
                    else
                        _currentCallback = Callback;

                    Bound = _currentCallback.Bind(true, caller, namingContext);
                }
                else
                {
                    Bound = false;
                }
            }

            public object Invoke(params object[] arguments)
            {
                if (Bound)
                {
                    return _currentCallback.Invoke(arguments);
                }
                else
                {
                    PhpException.Throw(PhpError.Warning, String.Format("Unable to call handler {0}()", Name));
                    return null;
                }
            }

            public HandlerWrapper(XmlParserResource parser, string name)
            {
                Parser = parser;
                Name = name;
            }
        }

        private enum ElementState
        {
            Beginning,
            Interior
        }

        private class ElementRecord
        {
            public int Level;
            public string ElementName;
            public ElementState State;
            public PhpArray Attributes;
        }

        private class TextRecord
        {
            public string Text;
        }

        #region Fields & Properties

        private Encoding _outputEncoding;
        private bool _processNamespaces;
        private string _namespaceSeparator;
        private Queue<string> _inputQueue;

        /// <summary>
        /// <c>True</c> iff the parser has no not-parsed data left.
        /// </summary>
        internal bool InputQueueIsEmpty { get { return _inputQueue == null || _inputQueue.Count == 0; } }

        public int CurrentLineNumber { get { return _lastLineNumber; } }
        private int _lastLineNumber;

        public int CurrentColumnNumber { get { return _lastColumnNumber; } }
        private int _lastColumnNumber;

        public int CurrentByteIndex { get { return _lastByteIndex; } }
        private int _lastByteIndex;

        public PhpCallback DefaultHandler { get { return _defaultHandler.Callback; } set { _defaultHandler.Callback = value; } }
        private HandlerWrapper _defaultHandler;

        public PhpCallback StartElementHandler { get { return _startElementHandler.Callback; } set { _startElementHandler.Callback = value; } }
        private HandlerWrapper _startElementHandler;

        public PhpCallback EndElementHandler { get { return _endElementHandler.Callback; } set { _endElementHandler.Callback = value; } }
        private HandlerWrapper _endElementHandler;

        public PhpCallback CharacterDataHandler { get { return _characterDataHandler.Callback; } set { _characterDataHandler.Callback = value; } }
        private HandlerWrapper _characterDataHandler;

        public PhpCallback StartNamespaceDeclHandler { get { return _startNamespaceDeclHandler.Callback; } set { _startNamespaceDeclHandler.Callback = value; } }
        private HandlerWrapper _startNamespaceDeclHandler;

        public PhpCallback EndNamespaceDeclHandler { get { return _endNamespaceDeclHandler.Callback; } set { _endNamespaceDeclHandler.Callback = value; } }
        private HandlerWrapper _endNamespaceDeclHandler;

        public PhpCallback ProcessingInstructionHandler { get { return _processingInstructionHandler.Callback; } set { _processingInstructionHandler.Callback = value; } }
        private HandlerWrapper _processingInstructionHandler;
        
        public DObject HandlerObject { get { return _handlerObject; } set { _handlerObject = value; } }
        private DObject _handlerObject;

        public bool EnableCaseFolding { get { return _enableCaseFolding; } set { _enableCaseFolding = value; } }
        private bool _enableCaseFolding;

        public bool EnableSkipWhitespace { get { return _enableSkipWhitespace; } set { _enableSkipWhitespace = value; } }
        private bool _enableSkipWhitespace;

        public int ErrorCode { get { return _errorCode; } }
        private int _errorCode;

        #endregion

        #region Helper functions

        internal static XmlParserResource ValidResource(PhpResource handle)
        {
            if (handle != null && handle.GetType() == typeof(XmlParserResource))
                return (XmlParserResource)handle;

            PhpException.Throw(PhpError.Warning, Strings.invalid_xmlresource);
            return null;
        }

        /// <summary>
        /// Convert handler into <see cref="PhpCallback"/> in XML-extension-manner.
        /// </summary>
        /// <param name="var"></param>
        /// <returns></returns>
        internal PhpCallback ObjectToXmlCallback(object var)
        {
            // empty variable
            if (PhpVariable.IsEmpty(var)) return null;

            // function name given as string:
            string name = PhpVariable.AsString(var);
            if (name != null)
                return (this.HandlerObject != null)
                    ? new PhpCallback(this.HandlerObject, name)
                    : new PhpCallback(name);            

            // default PHP callback:
            return Core.Convert.ObjectToCallback(var);
        }

        #endregion

        public bool Parse(DTypeDesc caller, NamingContext context, string input, bool isFinal)
        {            
            // the problem is when isFinal == false
            // XmlReader (more precisely XmlTextReaderImpl) synchronously waits for data from underlying stream when Read is called
            // and there is no way to tell whether we have sufficient amount of data for the next Read call
            // and if underlying stream ends prematurely, reader will get into Error state (so these simple workarounds are not possible)
            
            // current solution caches the data until isFinal == true and then performs the parsing
            // this is not memory efficient (usually this method gets called in a cycle on small chunks to save memory)

            // other way would be to let the reader wait on another thread (in thread pool), which would not be that bad
            // since XmlParser gets freed eventually

            // theoretically the best way would be to implement XmlReader, that would be able to recognize whether there is enough
            // data, but we have not further analyzed this possibility since it seems to result in unappropriate amount of work

            // yet another possible way is to use parser for inner element, and let it come into error state (not tested or thought through)
            // this does not work since inner parser can only be created when the parser reads an element (not in the beginning)

            if (isFinal)
            {
                if (input == null) input = string.Empty;
                StringBuilder sb = new StringBuilder(input.Length);

                if (_inputQueue != null)
                {
                    foreach (string s in _inputQueue)
                        sb.Append(s);

                    _inputQueue = null;
                }

                sb.Append(input);

                return ParseInternal(caller, context, sb.ToString(), null, null);                
            }
            else
            {
                //just reset these values - we are still in the beginning
                _lastLineNumber = 0;
                _lastColumnNumber = 0;
                _lastLineNumber = 0;

                if (!string.IsNullOrEmpty(input))
                {
                    if (_inputQueue == null)
                        _inputQueue = new Queue<string>();

                    _inputQueue.Enqueue(input);
                }

                return true;
            }
        }

        public bool ParseIntoStruct(DTypeDesc caller, NamingContext context, string input, PhpArray values, PhpArray indices)
        {
            return ParseInternal(caller, context, input, values, indices);
        }

        private bool ParseInternal(DTypeDesc caller, NamingContext context, string xml, PhpArray values, PhpArray indices)
        {
            StringReader stringReader = new StringReader(xml);
            XmlTextReader reader = new XmlTextReader(stringReader);
            Stack<ElementRecord> elementStack = new Stack<ElementRecord>();
            TextRecord textChunk = null;

            _startElementHandler.BindOrBiteMyLegsOff(caller, context);
            _endElementHandler.BindOrBiteMyLegsOff(caller, context);
            _defaultHandler.BindOrBiteMyLegsOff(caller, context);
            _startNamespaceDeclHandler.BindOrBiteMyLegsOff(caller, context);
            _endNamespaceDeclHandler.BindOrBiteMyLegsOff(caller, context);
            _characterDataHandler.BindOrBiteMyLegsOff(caller, context);
            _processingInstructionHandler.BindOrBiteMyLegsOff(caller, context);

            while (reader.ReadState == ReadState.Initial || reader.ReadState == ReadState.Interactive)
            {
                try
                {
                    reader.Read();
                }
                catch (XmlException)
                {
                    _lastLineNumber = reader.LineNumber;
                    _lastColumnNumber = reader.LinePosition;
                    _lastByteIndex = -1;
                    _errorCode = (int)XmlParserError.XML_ERROR_GENERIC;
                    return false;
                }

                //these are usually required
                _lastLineNumber = reader.LineNumber;
                _lastColumnNumber = reader.LinePosition;

                // we cannot do this - we could if we had underlying stream, but that would require
                // encoding string -> byte[] which is pointless


                switch (reader.ReadState)
                {
                    case ReadState.Error:
                        //report error
                        break;
                    case ReadState.EndOfFile:
                        //end of file
                        break;
                    case ReadState.Closed:
                    case ReadState.Initial:
                        //nonsense
                        Debug.Fail();
                        break;
                    case ReadState.Interactive:
                        //debug step, that prints out the current state of the parser (pretty printed)
                        //Debug_ParseStep(reader);
                        ParseStep(reader, elementStack, ref textChunk, values, indices);
                        break;
                }

                if (reader.ReadState == ReadState.Error || reader.ReadState == ReadState.EndOfFile || reader.ReadState == ReadState.Closed)
                    break;
            }

            return true;
        }

        private void ParseStep(XmlReader reader, Stack<ElementRecord> elementStack, ref TextRecord textChunk, PhpArray values, PhpArray indices)
        {
            string elementName;
            bool emptyElement;
            ElementRecord currentElementRecord = null;

            switch (reader.NodeType)
            {
                case XmlNodeType.Element:
                    elementName = reader.Name;
                    emptyElement = reader.IsEmptyElement;
                    PhpArray attributeArray = new PhpArray();

                    if (_processNamespaces && elementName.IndexOf(":") >= 0)
                    {
                        string localName = elementName.Substring(elementName.IndexOf(":") + 1);
                        elementName = reader.NamespaceURI + _namespaceSeparator + localName;
                    }

                    if (reader.MoveToFirstAttribute())
                    {
                        do
                        {                           
                            if (_processNamespaces && reader.Name.StartsWith("xmlns:"))
                            {
                                string namespaceID = reader.Name.Substring(6);
                                string namespaceUri = reader.Value;

                                if (_startNamespaceDeclHandler.Callback != null)
                                    _startNamespaceDeclHandler.Invoke(this, namespaceID, namespaceUri);

                                continue;
                            }

                            attributeArray.Add(_enableCaseFolding ? reader.Name.ToUpperInvariant() : reader.Name, reader.Value);
                        }
                        while (reader.MoveToNextAttribute());   
                    }

                    // update current top of stack
                    if (elementStack.Count != 0)
                    {
                        currentElementRecord = elementStack.Peek();

                        UpdateValueAndIndexArrays(currentElementRecord, ref textChunk, values, indices, true);

                        if (currentElementRecord.State == ElementState.Beginning)
                            currentElementRecord.State = ElementState.Interior;
                    }

                    // push the element into the stack (needed for parse_into_struct)
                    elementStack.Push(
                        new ElementRecord() { 
                            ElementName = elementName,
                            Level = reader.Depth, 
                            State = ElementState.Beginning, 
                            Attributes = (PhpArray)attributeArray.DeepCopy() 
                        });

                    if (_startElementHandler.Callback != null)
                        _startElementHandler.Invoke(this, _enableCaseFolding ? elementName.ToUpperInvariant() : elementName, attributeArray);
                    else
                        if (_defaultHandler.Callback != null) _defaultHandler.Invoke(this, "");

                    if (emptyElement) goto case XmlNodeType.EndElement;    // and end the element immediately (<element/>, XmlNodeType.EndElement will not be called)
                    
                    break;


                case XmlNodeType.EndElement:
                    elementName = reader.Name;

                    if (_processNamespaces && elementName.IndexOf(":") >= 0)
                    {
                        string localName = elementName.Substring(elementName.IndexOf(":") + 1);
                        elementName = reader.NamespaceURI + _namespaceSeparator + localName;
                    }

                    // pop the top element record
                    currentElementRecord = elementStack.Pop();

                    UpdateValueAndIndexArrays(currentElementRecord, ref textChunk, values, indices, false);

                    if (_endElementHandler.Callback != null)
                        _endElementHandler.Invoke(this, _enableCaseFolding ? elementName.ToUpperInvariant() : elementName);
                    else
                        if (_defaultHandler.Callback != null) _defaultHandler.Invoke(this, "");
                    break;


                case XmlNodeType.Whitespace:
                case XmlNodeType.Text:
                case XmlNodeType.CDATA:
                    if (textChunk == null)
                    {
                        textChunk = new TextRecord() { Text = reader.Value };
                    }
                    else
                    {
                        textChunk.Text += reader.Value;
                    }

                    if (_characterDataHandler.Callback != null)
                        _characterDataHandler.Invoke(this, reader.Value);
                    else
                        if (_defaultHandler.Callback != null) _defaultHandler.Invoke(this, reader.Value);
                    break;

                case XmlNodeType.ProcessingInstruction:

                    if (_processingInstructionHandler.Callback != null)
                        _processingInstructionHandler.Invoke(this, reader.Name, reader.Value);
                    else
                        if (_defaultHandler.Callback != null) _defaultHandler.Invoke(this, string.Empty);
                    break;
            }
        }

        private void UpdateValueAndIndexArrays(ElementRecord elementRecord, ref TextRecord textRecord, PhpArray values, PhpArray indices, bool middle)
        {
            // if we have no valid data in the middle, just end
            if (middle && textRecord == null)
                return;

            if (!middle && elementRecord.State == ElementState.Interior)
                UpdateValueAndIndexArrays(elementRecord, ref textRecord, values, indices, true);
            
            if (values != null)
            {
                PhpArray arrayRecord = new PhpArray();

                arrayRecord.Add("tag", elementRecord.ElementName);
                arrayRecord.Add("level", elementRecord.Level);

                if (elementRecord.State == ElementState.Beginning)
                    arrayRecord.Add("type", middle ? "open" : "complete");
                else
                    arrayRecord.Add("type", middle ? "cdata" : "close");

                if (textRecord != null)
                    arrayRecord.Add("value", textRecord.Text);

                if (elementRecord.State == ElementState.Beginning && elementRecord.Attributes.Count != 0)
                    arrayRecord.Add("attributes", elementRecord.Attributes);

                values.Add(arrayRecord);

                if (indices != null)
                {
                    PhpArray elementIndices;

                    if (!indices.ContainsKey(elementRecord.ElementName))
                    {
                        elementIndices = new PhpArray();
                        indices.Add(elementRecord.ElementName, elementIndices);
                    }
                    else
                        elementIndices = (PhpArray)indices[elementRecord.ElementName];

                    // add the max index (last inserted value)
                    elementIndices.Add(values.MaxIntegerKey);
                }
            }

            textRecord = null;
        }

        public XmlParserResource(Encoding outputEncoding, bool processNamespaces, string namespaceSeparator)
            : base("XmlParser")
        {
            _outputEncoding = outputEncoding;
            _processNamespaces = processNamespaces;
            _namespaceSeparator = namespaceSeparator != null ? namespaceSeparator.Substring(0, 1) : ":";
            _defaultHandler = new HandlerWrapper(this, "default");
            _startElementHandler = new HandlerWrapper(this, "startElement");
            _endElementHandler = new HandlerWrapper(this, "endElement");
            _characterDataHandler = new HandlerWrapper(this, "characterDataHandler");
            _startNamespaceDeclHandler = new HandlerWrapper(this, "startNamespaceDeclHandler");
            _endNamespaceDeclHandler = new HandlerWrapper(this, "endNamespaceDeclHandler");
            _processingInstructionHandler = new HandlerWrapper(this, "processingInstructionHandler");

            _enableCaseFolding = true;
            _enableSkipWhitespace = false;
        }
    }
}
