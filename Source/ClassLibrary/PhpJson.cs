/*

 Copyright (c) 2004-2010 Tomas Matousek, Jakub Misek.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

/*
 * TODO:
 * 
 * JSON_NUMERIC_CHECK (integer)
 * Encodes numeric strings as numbers. Available since PHP 5.3.3.
 * 
 * JSON_BIGINT_AS_STRING (integer)
 * Available since PHP 5.4.0.
 * 
 * JSON_PRETTY_PRINT (integer)
 * Use whitespace in returned data to format it. Available since PHP 5.4.0.
 * 
 * JSON_UNESCAPED_SLASHES (integer)
 * Don't escape /. Available since PHP 5.4.0.
 * */

using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Globalization;
using System.Security;
using System.Security.Permissions;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters;
using PHP.Core;
using PHP.Core.Reflection;
using PHP.Library;

#if SILVERLIGHT
using PHP.CoreCLR;
#else
using System.Web;
using System.Runtime.Serialization.Formatters.Binary;
#endif

namespace PHP.Library
{
    #region JSON PHP API

    /// <summary>
    /// Classes implementing Countable can be used with the count() function.
    /// </summary>
    [ImplementsType]
    public interface JsonSerializable
    {
        /// <summary>
        /// Specify data which should be serialized to JSON.
        /// </summary>
        /// <param name="context">Current <see cref="ScriptContext"/> provided by Phalanger.</param>
        /// <returns>Return data which should be serialized by <c>json_encode()</c>, see <see cref="PhpJson.Serialize"/>.</returns>
        [ImplementsMethod]
        [AllowReturnValueOverride]
        object jsonSerialize(ScriptContext context);
    }

    /// <summary>
	/// JSON encoding/decoding functions.
	/// </summary>
	/// <threadsafety static="true"/>
    [ImplementsExtension(LibraryDescriptor.ExtJson)]
	public static class PhpJson
    {
        #region static ctor

        static PhpJson()
        {
            // clear the last error variable every request
            RequestContext.RequestBegin += () =>
                {
                    PhpJson.LastError = JsonLastError.JSON_ERROR_NONE;
                };
        }

        #endregion

        #region Constants

        /// <summary>
        /// Values returned by json_last_error function.
        /// </summary>
        public enum JsonLastError : int
        {
            /// <summary>
            /// No error has occurred  	 
            /// </summary>
            [ImplementsConstant("JSON_ERROR_NONE")]
            JSON_ERROR_NONE = 0,

            /// <summary>
            /// The maximum stack depth has been exceeded  	 
            /// </summary>
            [ImplementsConstant("JSON_ERROR_DEPTH")]
            JSON_ERROR_DEPTH = 1,

            /// <summary>
            /// Occurs with underflow or with the modes mismatch.
            /// </summary>
            [ImplementsConstant("PHP_JSON_ERROR_STATE_MISMATCH")]
            PHP_JSON_ERROR_STATE_MISMATCH = 2,

            /// <summary>
            /// Control character error, possibly incorrectly encoded  	 
            /// </summary>
            [ImplementsConstant("JSON_ERROR_CTRL_CHAR")]
            JSON_ERROR_CTRL_CHAR = 3,

            /// <summary>
            /// Syntax error  	 
            /// </summary>
            [ImplementsConstant("JSON_ERROR_SYNTAX")]
            JSON_ERROR_SYNTAX = 4,

            /// <summary>
            /// 
            /// </summary>
            [ImplementsConstant("JSON_ERROR_UTF8")]
            JSON_ERROR_UTF8 = 5,
        }

        /// <summary>
        /// Options given to json_encode function.
        /// </summary>
        public enum JsonEncodeOptions
        {
            /// <summary>
            /// No options specified.
            /// </summary>
            Default = 0,

            /// <summary>
            /// All &lt; and &gt; are converted to \u003C and \u003E. 
            /// </summary>
            [ImplementsConstant("JSON_HEX_TAG")]
            JSON_HEX_TAG = 1,

            /// <summary>
            /// All &amp;s are converted to \u0026. 
            /// </summary>
            [ImplementsConstant("JSON_HEX_AMP")]
            JSON_HEX_AMP = 2,

            /// <summary>
            /// All ' are converted to \u0027. 
            /// </summary>
            [ImplementsConstant("JSON_HEX_APOS")]
            JSON_HEX_APOS = 4,

            /// <summary>
            /// All " are converted to \u0022. 
            /// </summary>
            [ImplementsConstant("JSON_HEX_QUOT")]
            JSON_HEX_QUOT = 8,

            /// <summary>
            /// Outputs an object rather than an array when a non-associative array is used. Especially useful when the recipient of the output is expecting an object and the array is empty. 
            /// </summary>
            [ImplementsConstant("JSON_FORCE_OBJECT")]
            JSON_FORCE_OBJECT = 16,

            /// <summary>
            /// Encodes numeric strings as numbers. 
            /// </summary>
            [ImplementsConstant("JSON_NUMERIC_CHECK")]
            JSON_NUMERIC_CHECK = 32,
        }

        /// <summary>
        /// Options given to json_decode function.
        /// </summary>
        public enum JsonDecodeOptions
        {
            Default = 0,

            /// <summary>
            /// Big integers represent as strings rather than floats.
            /// </summary>
            [ImplementsConstant("JSON_BIGINT_AS_STRING")]
            JSON_BIGINT_AS_STRING = 1,
        }

		#endregion

        #region json_encode, json_decode, json_last_error (CLR only)
#if !SILVERLIGHT

        [ImplementsFunction("json_encode")]
		public static PhpBytes Serialize(object value)
		{
            return PhpJsonSerializer.Default.Serialize(value, UnknownTypeDesc.Singleton);
		}

        [ImplementsFunction("json_encode")]
        public static PhpBytes Serialize(object value, JsonEncodeOptions options)
        {
            return new PhpJsonSerializer(
                new JsonFormatter.EncodeOptions()
                {
                    ForceObject = (options & JsonEncodeOptions.JSON_FORCE_OBJECT) != 0,
                    HexAmp = (options & JsonEncodeOptions.JSON_HEX_AMP) != 0,
                    HexApos = (options & JsonEncodeOptions.JSON_HEX_APOS) != 0,
                    HexQuot = (options & JsonEncodeOptions.JSON_HEX_QUOT) != 0,
                    HexTag = (options & JsonEncodeOptions.JSON_HEX_TAG) != 0,
                    NumericCheck = (options & JsonEncodeOptions.JSON_NUMERIC_CHECK) != 0,
                },
                new JsonFormatter.DecodeOptions()
                ).Serialize(value, UnknownTypeDesc.Singleton);
        }

        [ImplementsFunction("json_decode")]
        public static PhpReference Unserialize(PhpBytes json)
        {
            if (json == null)
                return null;

            return PhpJsonSerializer.Default.Deserialize(json, UnknownTypeDesc.Singleton);
        }

        [ImplementsFunction("json_decode")]
        public static PhpReference Unserialize(PhpBytes json, bool assoc /* = false*/)
        {
            return Unserialize(json, assoc, 512, JsonDecodeOptions.Default);
        }

        [ImplementsFunction("json_decode")]
        public static PhpReference Unserialize(PhpBytes json, bool assoc /* = false*/ , int depth /* = 512*/)
        {
            return Unserialize(json, assoc, depth, JsonDecodeOptions.Default);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="json"></param>
        /// <param name="assoc">When TRUE, returned object's will be converted into associative array s. </param>
        /// <param name="depth">User specified recursion depth. </param>
        /// <param name="options"></param>
        /// <returns></returns>
		[ImplementsFunction("json_decode")]
        public static PhpReference Unserialize(PhpBytes json, bool assoc /* = false*/ , int depth /* = 512*/  , JsonDecodeOptions options /* = 0 */)
		{
            if (json == null)
                return null;

            return new PhpJsonSerializer(
                new JsonFormatter.EncodeOptions(),
                new JsonFormatter.DecodeOptions()
                {
                    Assoc = assoc,
                    Depth = depth,
                    BigIntAsString =  (options & JsonDecodeOptions.JSON_BIGINT_AS_STRING) != 0
                }
                ).Deserialize(json, UnknownTypeDesc.Singleton);
		}

        [ImplementsFunction("json_last_error")]
        public static int GetLastError()
        {
            return (int)LastError;
        }

        [ThreadStatic]
        internal static JsonLastError LastError = JsonLastError.JSON_ERROR_NONE;

#endif
		#endregion
    }

    #endregion

    #region JsonFormatter

    /// <summary>
    /// Implements a JSON formatter (serializer).
    /// </summary>
    public sealed class JsonFormatter : IFormatter
    {
        #region Tokens

        /// <summary>
        /// Contains definition of (one-character) tokens that constitute PHP serialized data.
        /// </summary>
        internal class Tokens
        {
            internal const char ObjectOpen = '{';
            internal const char ObjectClose = '}';
            internal const char ItemsSeparator = ',';
            internal const char PropertyKeyValueSeparator = ':';
            
            internal const char Quote = '"';
            internal const char Escape = '\\';

            internal const string EscapedNewLine = @"\n";
            internal const string EscapedCR = @"\r";
            internal const string EscapedTab = @"\t";
            internal const string EscapedBackspace = @"\b";
            internal const string EscapedQuote = "\\\"";
            internal const string EscapedReverseSolidus = @"\\";
            internal const string EscapedSolidus = @"\/";
            internal const string EscapedFormFeed = @"\f";
            internal const string EscapedUnicodeChar = @"\u";   // 4-digit number follows

            internal const char ArrayOpen = '[';
            internal const char ArrayClose = ']';

            internal const string NullLiteral = "null";
            internal const string TrueLiteral = "true";
            internal const string FalseLiteral = "false";

        }

        #endregion

        /// <summary>
        /// Implements the serialization functionality. Serializes an object, or graph of objects
        /// with the given root to the provided <see cref="StreamWriter"/>.
        /// </summary>
        internal class ObjectWriter
        {
            #region Fields and Properties

            private readonly ScriptContext/*!*/ context;

            /// <summary>
            /// The stream writer to write serialized data to.
            /// </summary>
            private readonly StreamWriter/*!*/ writer;

            /// <summary>
            /// Options.
            /// </summary>
            private readonly EncodeOptions/*!*/ encodeOptions;

            /// <summary>
            /// The encoding to be used when writing and reading the serialization stream.
            /// </summary>
            private Encoding encoding;

            /// <summary>
            /// Stack of objects being currently serialized. Used to avoid stack overflow and to properly outputs "recursion_detected" warning.
            /// </summary>
            private List<object> recursionStack = null;

            #endregion

            #region Construction

            /// <summary>
            /// Creates a new <see cref="ObjectWriter"/> with a given <see cref="StreamWriter"/>.
            /// </summary>
            /// <param name="context">The current <see cref="ScriptContext"/>.</param>
            /// <param name="writer">The writer to write serialized data to.</param>
            /// <param name="encodeOptions">Encoding options.</param>
            /// <param name="encoding">Encoding used for reading PhpBytes.</param>
            internal ObjectWriter(ScriptContext/*!*/ context, StreamWriter/*!*/ writer, EncodeOptions/*!*/encodeOptions, Encoding encoding)
            {
                Debug.Assert(context != null && writer != null && encodeOptions != null);

                this.context = context;
                this.writer = writer;
                this.encodeOptions = encodeOptions;
                this.encoding = encoding;
            }

            #endregion

            #region Recursion

            /// <summary>
            /// Push currently serialized array or object to the stack to prevent recursion.
            /// </summary>
            /// <param name="obj"></param>
            /// <returns></returns>
            private bool PushObject(object/*!*/obj)
            {
                Debug.Assert(obj != null);

                if (recursionStack == null)
                    recursionStack = new List<object>(8);
                else
                {
                    // check recursion
                    int hits = 0;
                    for (int i = 0; i < recursionStack.Count; i++)
                        if (recursionStack[i] == obj)
                            hits++;

                    if (hits >= 2)
                    {
                        PhpException.Throw(PhpError.Warning, LibResources.GetString("recursion_detected"));
                        return false;
                    }
                }

                recursionStack.Add(obj);
                return true;
            }

            /// <summary>
            /// Pop the serialized object from the stack.
            /// </summary>
            private void PopObject()
            {
                Debug.Assert(recursionStack != null);
                recursionStack.RemoveAt(recursionStack.Count - 1);
            }

            #endregion

            #region Serialize, Write*

            /// <summary>
            /// Serializes an object or graph of objects to <see cref="writer"/>.
            /// </summary>
            /// <param name="graph">The object (graph) to serialize.</param>
            internal void Serialize(object graph)
            {
                if (graph == null)
                {
                    WriteNull();
                    return;
                }

                switch (Type.GetTypeCode(graph.GetType()))
                {
                    case TypeCode.Boolean:
                        WriteBoolean((bool)graph); break;

                    case TypeCode.Byte:
                    case TypeCode.SByte:
                    case TypeCode.Int16:
                    case TypeCode.Int32:
                    case TypeCode.Int64:
                    case TypeCode.UInt16:
                    case TypeCode.UInt32:
                    case TypeCode.UInt64:
                    case TypeCode.Decimal:
                        writer.Write(graph.ToString());
                        break;

                    case TypeCode.Single:
                        writer.Write(graph.ToString());
                        break;
                    case TypeCode.Double:
                        writer.Write((double)graph);
                        break;

                    case TypeCode.Char:
                        WriteString(graph.ToString());
                        break;

                    case TypeCode.String:
                        WriteString((string)graph);
                        break;

                    case TypeCode.Object:
                        {
                            PhpArray array;
                            if ((array = graph as PhpArray) != null)
                            {
                                if (PushObject(graph))
                                {
                                    WriteArray(array);
                                    PopObject();
                                }
                                else
                                    WriteNull();

                                break;
                            }

                            DObject obj;
                            JsonSerializable jsonserializeble;
                            if ((jsonserializeble = graph as JsonSerializable) != null)
                            {
                                var retval = jsonserializeble.jsonSerialize(context);
                                if ((obj = (retval as DObject)) != null)    // Handle the case where jsonSerialize() returns itself.
                                    WriteDObject(obj);
                                else
                                    Serialize(retval);
                                
                                break;
                            }

                            if ((obj = graph as DObject) != null)
                            {
                                if (PushObject(graph))
                                {
                                    WriteDObject(obj);
                                    PopObject();
                                }
                                else
                                    WriteNull();

                                break;
                            }

                            PhpReference reference;
                            if ((reference = graph as PhpReference) != null)
                            {
                                Serialize(reference.Value);
                                break;
                            }

                            PhpBytes bytes;
                            if ((bytes = graph as PhpBytes) != null)
                            {
                                WriteString(((IPhpConvertible)bytes).ToString());
                                break;
                            }

                            PhpString str;
                            if ((str = graph as PhpString) != null)
                            {
                                WriteString(str.ToString());
                                break;
                            }

                            if (graph is PhpResource)
                            {
                                WriteUnsupported(PhpResource.PhpTypeName);
                                break;
                            }

                            goto default;
                        }

                    default:
                        WriteUnsupported(graph.GetType().FullName);
                        break;
                }
            }

            /// <summary>
            /// Serializes null and throws an exception.
            /// </summary>
            /// <param name="TypeName"></param>
            private void WriteUnsupported(string TypeName)
            {
                PhpException.Throw(PhpError.Warning, LibResources.GetString("serialization_unsupported_type", TypeName));
                WriteNull();
            }

            /// <summary>
            /// Serializes <B>Null</B>.
            /// </summary>
            private void WriteNull()
            {
                writer.Write(Tokens.NullLiteral);
            }

            /// <summary>
            /// Serializes a bool value.
            /// </summary>
            /// <param name="value">The value.</param>
            private void WriteBoolean(bool value)
            {
                writer.Write(value ? Tokens.TrueLiteral : Tokens.FalseLiteral);
            }

            #region encoding strings

            /// <summary>
            /// Determines if given character is printable character. Otherwise it must be encoded.
            /// </summary>
            /// <param name="c"></param>
            /// <returns></returns>
            private static bool CharIsPrintable(char c)
            {
                return
                    (c <= 0x7f) &&   // ASCII
                    (!char.IsControl(c)) && // not control
                    (!(c >= 9 && c <= 13)); // not BS, HT, LF, Vertical Tab, Form Feed, CR
            }

            /// <summary>
            /// Determines if given character should be encoded.
            /// </summary>
            /// <param name="c"></param>
            /// <returns></returns>
            private bool CharShouldBeEncoded(char c)
            {
                switch (c)
                {
                    case '\n':
                    case '\r':
                    case '\t':
                    case '/':
                    case Tokens.Escape:
                    case '\b':
                    case '\f':
                    case Tokens.Quote:
                        return true;

                    case '\'':
                        return encodeOptions.HexApos;

                    case '<':
                        return encodeOptions.HexTag;

                    case '>':
                        return encodeOptions.HexTag;

                    case '&':
                        return encodeOptions.HexAmp;

                    default:
                        return !CharIsPrintable(c);
                }
            }

            /// <summary>
            /// Convert 16b character into json encoded character.
            /// </summary>
            /// <param name="value">The full string to be encoded.</param>
            /// <param name="i">The index of character to be encoded. Can be increased if more characters are processed.</param>
            /// <returns>The encoded part of string, from value[i] to value[i after method call]</returns>
            private string EncodeStringIncremental(string value, ref int i)
            {
                char c = value[i];

                switch (c)
                {
                    case '\n':  return (Tokens.EscapedNewLine);
                    case '\r':  return (Tokens.EscapedCR);
                    case '\t':  return (Tokens.EscapedTab);
                    case '/':   return (Tokens.EscapedSolidus);
                    case Tokens.Escape: return (Tokens.EscapedReverseSolidus);
                    case '\b':  return (Tokens.EscapedBackspace);
                    case '\f':  return (Tokens.EscapedFormFeed);
                    case Tokens.Quote:  return (encodeOptions.HexQuot ? (Tokens.EscapedUnicodeChar + "0022") : Tokens.EscapedQuote);
                    case '\'':  return (encodeOptions.HexApos ? (Tokens.EscapedUnicodeChar + "0027") : "'");
                    case '<':   return (encodeOptions.HexTag ? (Tokens.EscapedUnicodeChar + "003C") : "<");
                    case '>':   return (encodeOptions.HexTag ? (Tokens.EscapedUnicodeChar + "003E") : ">");
                    case '&':   return (encodeOptions.HexAmp ? (Tokens.EscapedUnicodeChar + "0026") : "&");
                    default:
                        {
                            if (CharIsPrintable(c))
                            {
                                int start = i++;
                                for (; i < value.Length && !CharShouldBeEncoded(value[i]); ++i)
                                    ;

                                return value.Substring(start, (i--) - start);   // accumulate characters, mostly it is entire string value (faster)
                            }
                            else
                            {
                                return (Tokens.EscapedUnicodeChar + ((int)c).ToString("X4"));
                            }
                        }
                }
            }

            #endregion

            /// <summary>
            /// Serializes JSON string.
            /// </summary>
            /// <param name="value">The string.</param>
            private void WriteString(string value)
            {
                if (encodeOptions.NumericCheck)
                {
                    int i;
                    long l;
                    double d;
                    var result = PHP.Core.Convert.StringToNumber(value, out i, out l, out d);
                    if ((result & Core.Convert.NumberInfo.IsNumber) != 0)
                    {
                        if ((result & Core.Convert.NumberInfo.Integer) != 0) writer.Write(i.ToString());
                        if ((result & Core.Convert.NumberInfo.LongInteger) != 0) writer.Write(l.ToString());
                        if ((result & Core.Convert.NumberInfo.Double) != 0) writer.Write(d.ToString());
                        return;
                    }
                }

                StringBuilder strVal = new StringBuilder(value.Length + 2);

                strVal.Append(Tokens.Quote);

                for (int i = 0; i < value.Length; ++i)
                {
                    strVal.Append(EncodeStringIncremental(value, ref i));
                }
                                
                strVal.Append(Tokens.Quote);

                writer.Write(strVal.ToString());
            }

            #region formatting JSON objects / arrays

            private void WriteJsonObject(IEnumerable<KeyValuePair<string, object>> items)
            {
                writer.Write(Tokens.ObjectOpen);

                bool bFirst = true;
                foreach (var x in items)
                {
                    if (bFirst) bFirst = false;
                    else writer.Write(Tokens.ItemsSeparator);

                    WriteString(x.Key);
                    writer.Write(Tokens.PropertyKeyValueSeparator);

                    Serialize(x.Value);
                }

                writer.Write(Tokens.ObjectClose);
            }

            private void WriteJsonArray(IEnumerable<object> items)
            {
                writer.Write(Tokens.ArrayOpen);

                bool bFirst = true;
                foreach (var x in items)
                {
                    if (bFirst) bFirst = false;
                    else writer.Write(Tokens.ItemsSeparator);

                    Serialize(x);
                }

                writer.Write(Tokens.ArrayClose);
            }

            private IEnumerable<KeyValuePair<string, object>> JsonObjectProperties(PhpArray/*!*/value)
            {
                foreach (var x in value)
                    yield return new KeyValuePair<string, object>(x.Key.ToString(), x.Value);
            }

            private IEnumerable<KeyValuePair<string, object>> JsonObjectProperties(DObject/*!*/value, bool avoidPicName)
            {
                foreach (KeyValuePair<string, object> pair in Serialization.EnumerateSerializableProperties(value))
                {
                    if (avoidPicName && pair.Key == __PHP_Incomplete_Class.ClassNameFieldName)
                    {
                        // skip the __PHP_Incomplete_Class_Name field
                        continue;
                    }

                    yield return pair;
                }
            }

            #endregion

            /// <summary>
            /// Serializes a <see cref="PhpArray"/>.
            /// </summary>
            /// <param name="value">The array.</param>
            private void WriteArray(PhpArray value)
            {
                if (encodeOptions.ForceObject || (value.StringCount > 0 || value.MaxIntegerKey + 1 != value.IntegerCount))
                    WriteJsonObject(JsonObjectProperties(value));
                else
                    WriteJsonArray(value.Values);
            }

            /// <summary>
            /// Serializes a <see cref="DObject"/>.
            /// </summary>
            /// <param name="value">The object.</param>
            private void WriteDObject(DObject value)
            {
                __PHP_Incomplete_Class pic;
                
                // write out properties
                WriteJsonObject(JsonObjectProperties(value, (pic = value as __PHP_Incomplete_Class) != null && pic.__PHP_Incomplete_Class_Name.IsSet));
            }

            #endregion
        }

        /// <summary>
        /// Implements the deserialization functionality. Deserializes the data on the provided
        /// <see cref="StreamReader"/> and reconstitutes the graph of objects.
        /// </summary>
        internal class ObjectReader
        {
            #region Fields and Properties

            private readonly ScriptContext/*!*/ context;

            /// <summary>
            /// The stream reader to read serialized data from.
            /// </summary>
            private readonly StreamReader/*!*/ reader;

            /// <summary>
            /// Decoding options.
            /// </summary>
            private readonly DecodeOptions/*!*/decodeOptions;

            #endregion

            #region Construction

            /// <summary>
            /// Creates a new <see cref="ObjectReader"/> with a given <see cref="StreamReader"/>.
            /// </summary>
            /// <param name="context">The current <see cref="ScriptContext"/>.</param>
            /// <param name="reader">The reader to reader serialized data from.</param>
            /// <param name="decodeOptions"></param>
            internal ObjectReader(ScriptContext/*!*/ context, StreamReader/*!*/ reader, DecodeOptions/*!*/decodeOptions)
            {
                Debug.Assert(context != null && reader != null && decodeOptions != null);

                this.context = context;
                this.reader = reader;
                this.decodeOptions = decodeOptions;
            }

            #endregion

            /// <summary>
            /// De-serializes the data is <see cref="reader"/> and reconstitutes the graph of objects.
            /// </summary>
            /// <returns>The top object of the deserialized graph. Null in case of error.</returns>
            internal object Deserialize()
            {
                var scanner = new JsonScanner(reader, decodeOptions);
                var parser = new Json.Parser(context, decodeOptions) { Scanner = scanner };

                try
                {
                    if (!parser.Parse())
                        throw new Exception("Syntax error");
                }
                catch (Exception)
                {
                    PhpJson.LastError = PhpJson.JsonLastError.JSON_ERROR_SYNTAX;
                    return null;
                }

                PhpJson.LastError = PhpJson.JsonLastError.JSON_ERROR_NONE;
                return parser.Result;
            }
        }

        #region Fields and properties

        /// <summary>
        /// Serialization security permission demanded in <see cref="Serialize"/>.
        /// </summary>
        private static SecurityPermission serializationPermission =
            new SecurityPermission(SecurityPermissionFlag.SerializationFormatter);

        /// <summary>
        /// The encoding to be used when writing and reading the serialization stream.
        /// </summary>
        private Encoding encoding;

        /// <summary>
        /// Gets or sets the encoding to be used when writing and reading the serialized stream.
        /// </summary>
        public Encoding Encoding
        {
            get { return encoding; }
            set { encoding = (value ?? new ASCIIEncoding()); }
        }

        /// <summary>
        /// Gets or sets the serialization binder that performs type lookups during deserialization.
        /// </summary>
        public SerializationBinder Binder
        {
            get { return null; }
            set { throw new NotSupportedException(LibResources.GetString("serialization_binder_unsupported")); }
        }

        /// <summary>
        /// Gets or sets the streaming context used for serialization and deserialization.
        /// </summary>
        public StreamingContext Context
        {
            get { return new StreamingContext(StreamingContextStates.Persistence); }
            set { throw new NotSupportedException(LibResources.GetString("streaming_context_unsupported")); }
        }

        /// <summary>
        /// Gets or sets the surrogate selector used by the current formatter.
        /// </summary>
        public ISurrogateSelector SurrogateSelector
        {
            get { return null; }
            set { throw new NotSupportedException(LibResources.GetString("surrogate_selector_unsupported")); }
        }

        #endregion

        #region Options

        /// <summary>
        /// Encode (serialize) options. All false.
        /// </summary>
        public class EncodeOptions
        {
            public bool HexTag = false, HexAmp = false, HexApos = false, HexQuot = false, ForceObject = false, NumericCheck = false;
        }

        /// <summary>
        /// Decode (unserialize) options.
        /// </summary>
        public class DecodeOptions
        {
            public bool BigIntAsString = false;

            /// <summary>
            /// When TRUE, returned object s will be converted into associative array s. 
            /// </summary>
            public bool Assoc = false;

            /// <summary>
            /// User specified recursion depth. 
            /// </summary>
            public int Depth = 512;
        }

        private readonly EncodeOptions encodeOptions;
        private readonly DecodeOptions decodeOptions;

        #endregion

        #region Construction

        ///// <summary>
        ///// Creates a new <see cref="PhpFormatter"/> with <see cref="ASCIIEncoding"/> and
        ///// default <see cref="Context"/>.
        ///// </summary>
        //public JsonFormatter()
        //    :this(new ASCIIEncoding(), new EncodeOptions(), new DecodeOptions())
        //{
            
        //}

        /// <summary>
        /// Creates a new <see cref="PhpFormatter"/> with a given <see cref="Encoding"/> and
        /// default <see cref="Context"/>.
        /// </summary>
        /// <param name="encoding">The encoding to be used when writing and reading the serialization stream.</param>
        /// <param name="encodeOptions">Options used to encode the data stream.</param>
        /// <param name="decodeOptions">Options used to decode the data stream.</param>
        /// <param name="caller">DTypeDesc of the caller's class context if it is known or UnknownTypeDesc if it should be determined lazily.</param>
        public JsonFormatter(Encoding encoding, EncodeOptions encodeOptions, DecodeOptions decodeOptions, DTypeDesc caller)
        {
            // no UTF8 BOM!
            if (encoding is UTF8Encoding)
                this.encoding = new UTF8Encoding(false);
            else
                this.encoding = (encoding ?? new ASCIIEncoding());

            // options
            this.encodeOptions = encodeOptions;
            this.decodeOptions = decodeOptions;
        }

        #endregion

        #region Serialize and Deserialize

        /// <summary>
        /// Serializes an object, or graph of objects with the given root to the provided stream.
        /// </summary>
        /// <param name="serializationStream">The stream where the formatter puts the serialized data.</param>
        /// <param name="graph">The object, or root of the object graph, to serialize.</param>
        public void Serialize(Stream/*!*/serializationStream, object graph)
        {
            if (serializationStream == null)
                throw new ArgumentNullException("serializationStream");
            
            serializationPermission.Demand();

            StreamWriter stream_writer = new StreamWriter(serializationStream, encoding);
            ObjectWriter object_writer = new ObjectWriter(ScriptContext.CurrentContext, stream_writer, encodeOptions, encoding);

            try
            {
                object_writer.Serialize(graph);
            }
            finally
            {
                stream_writer.Flush();
            }
        }

        /// <summary>
        /// Deserializes the data on the provided stream and reconstitutes the graph of objects.
        /// </summary>
        /// <param name="serializationStream">The stream containing the data to deserialize.</param>
        /// <returns>The top object of the deserialized graph.</returns>
        public object Deserialize(Stream/*!*/serializationStream)
        {
            if (serializationStream == null)
                throw new ArgumentNullException("serializationStream");
            
            serializationPermission.Demand();

            ScriptContext context = ScriptContext.CurrentContext;
            ObjectReader object_reader = new ObjectReader(context, new StreamReader(serializationStream, encoding), decodeOptions);
            return object_reader.Deserialize();
        }

        #endregion
    }

    #endregion

    #region JsonScanner

    public class JsonScanner : Json.Lexer, PHP.Core.Parsers.GPPG.ITokenProvider<Json.SemanticValueType, Json.Position>
    {
        Json.SemanticValueType tokenSemantics;
        Json.Position tokenPosition;

        private readonly PHP.Library.JsonFormatter.DecodeOptions/*!*/decodeOptions;

        public JsonScanner(TextReader/*!*/ reader, PHP.Library.JsonFormatter.DecodeOptions/*!*/decodeOptions)
            : base(reader)
        {
            Debug.Assert(decodeOptions != null);

            this.decodeOptions = decodeOptions;
        }

        #region ITokenProvider<SemanticValueType,Position> Members

        public Json.SemanticValueType TokenValue
        {
            get { return tokenSemantics; }
        }

        public Json.Position TokenPosition
        {
            get { return tokenPosition; }
        }

        public new int GetNextToken()
        {
            tokenPosition = new Json.Position();
            tokenSemantics = new Json.SemanticValueType();

            Json.Tokens token = base.GetNextToken();

            switch (token)
            {
                case Json.Tokens.STRING_BEGIN:
                    while ((token = base.GetNextToken()) != Json.Tokens.STRING_END)
                    {
                        if (token == Json.Tokens.ERROR || token == Json.Tokens.EOF)
                            throw new Exception("Syntax error, unexpected " + TokenChunkLength.ToString());
                    }
                    token = Json.Tokens.STRING;
                    tokenSemantics.obj = base.QuotedStringContent;
                    break;
                case Json.Tokens.INTEGER:
                case Json.Tokens.DOUBLE:
                    {
                        int i;
                        long l;
                        double d;
                        string numtext = yytext();
                        switch (PHP.Core.Convert.StringToNumber(numtext, out i, out l, out d) & PHP.Core.Convert.NumberInfo.TypeMask)
                        {
                            case PHP.Core.Convert.NumberInfo.Double:
                                if (decodeOptions.BigIntAsString && token == Json.Tokens.INTEGER)
                                    tokenSemantics.obj = numtext;   // it was integer, but converted to double because it was too long
                                else
                                    tokenSemantics.obj = d;
                                break;
                            case PHP.Core.Convert.NumberInfo.Integer:
                                tokenSemantics.obj = i;
                                break;
                            case PHP.Core.Convert.NumberInfo.LongInteger:
                                tokenSemantics.obj = l;
                                break;
                            default:
                                tokenSemantics.obj = numtext;
                                break;

                        }
                    }
                    break;
            }

            return (int)token;
        }

        public void ReportError(string[] expectedTokens)
        {
        }

        #endregion
    }

    #endregion
}


namespace PHP.Library.Json
{
    public partial class Lexer
    {
        private char Map(char c)
        {
            return (c > SByte.MaxValue) ? 'a' : c;
        }
    }
}