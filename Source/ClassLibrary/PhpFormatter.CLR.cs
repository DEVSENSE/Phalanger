/*

 Copyright (c) 2005-2006 Ladislav Prosek.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

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

namespace PHP.Library
{
	/// <summary>
	/// Implements a PHP-compatible formatter (serializer).
	/// </summary>
	public sealed class PhpFormatter : IFormatter
	{
		#region Tokens

		/// <summary>
		/// Contains definition of (one-character) tokens that constitute PHP serialized data.
		/// </summary>
		internal class Tokens
		{
			internal const char BraceOpen = '{';
			internal const char BraceClose = '}';
			internal const char Colon = ':';
			internal const char Semicolon = ';';
			internal const char Quote = '"';

			internal const char Null = 'N';
			internal const char Boolean = 'b';
			internal const char Integer = 'i';
			internal const char Double = 'd';
			internal const char String = 's';
			internal const char Array = 'a';
			internal const char Object = 'O'; // instance of a class that does not implement SPL.Serializable
			internal const char ObjectSer = 'C'; // instance of a class that implements SPL.Serializable
            internal const char ClrObject = 'T';    // instance of CLR object, serialized using binary formatter

			internal const char Reference = 'R'; // &-like reference
			internal const char ObjectRef = 'r'; // same instance reference (PHP5 object semantics)
		}

		#endregion

		/// <summary>
		/// Implements the serialization functionality. Serializes an object, or graph of objects
		/// with the given root to the provided <see cref="StreamWriter"/>.
		/// </summary>
		internal class ObjectWriter : Serializer.ClassContextHolder
		{
			#region Fields and Properties

			private ScriptContext/*!*/ context;

			/// <summary>
			/// The stream writer to write serialized data to.
			/// </summary>
			private StreamWriter/*!*/ writer;

			/// <summary>
			/// Object ID counter used by the <B>r</B> and <B>R</B> tokens.
			/// </summary>
			private int sequenceNumber;

			/// <summary>
			/// Maintains a sequence number for every <see cref="DObject"/> and <see cref="PhpReference"/>
			/// that have already been serialized.
			/// </summary>
            private Dictionary<object, int> serializedRefs { get { return _serializedRefs ?? (_serializedRefs = new Dictionary<object, int>()); } }
			private Dictionary<object, int> _serializedRefs;

			#endregion

			#region Construction

			/// <summary>
			/// Creates a new <see cref="ObjectWriter"/> with a given <see cref="StreamWriter"/>.
			/// </summary>
			/// <param name="context">The current <see cref="ScriptContext"/>.</param>
			/// <param name="writer">The writer to write serialized data to.</param>
            /// <param name="caller">DTypeDesc of the caller's class context if it is known or UnknownTypeDesc if it should be determined lazily.</param>
            internal ObjectWriter(ScriptContext/*!*/ context, StreamWriter/*!*/ writer, DTypeDesc caller)
                : base(caller)
			{
				Debug.Assert(context != null && writer != null);
				this.context = context;
				this.writer = writer;
			}

			#endregion

			#region Serialize and Write*

			/// <summary>
			/// Serializes an object or graph of objects to <see cref="writer"/>.
			/// </summary>
			/// <param name="graph">The object (graph) to serialize.</param>
			/// <remarks>
			/// This is just a switch over <paramref name="graph"/>'s type that delegates the task
			/// to one of <see cref="WriteNull"/>, <see cref="WriteBoolean"/>, <see cref="WriteInteger"/>,
			/// <see cref="WriteDouble"/>, <see cref="WriteString"/>, <see cref="WriteReference"/>,
			/// <see cref="WriteBinaryData"/>, <see cref="WriteArray"/> and <see cref="WriteObject"/>.
			/// </remarks>
			internal void Serialize(object graph)
			{
				sequenceNumber++;

				if (graph == null) WriteNull();
				else
				{
					switch (Type.GetTypeCode(graph.GetType()))
					{
						case TypeCode.Boolean: WriteBoolean((bool)graph); break;
						case TypeCode.Int32: WriteInteger((int)graph); break;
						case TypeCode.Int64: WriteInteger((long)graph); break;
						case TypeCode.Double: WriteDouble((double)graph); break;
						case TypeCode.String: WriteString((string)graph); break;
						case TypeCode.Object:
							{
								PhpReference reference = graph as PhpReference;
								if (reference != null)
								{
									WriteReference(reference);
									break;
								}

								PhpBytes bytes = graph as PhpBytes;
								if (bytes != null)
								{
                                    WriteBinaryData(bytes.ReadonlyData);
									break;
								}

								PhpString str = graph as PhpString;
								if (str != null)
								{
									WriteString(str.ToString());
									break;
								}

								PhpArray array = graph as PhpArray;
								if (array != null)
								{
									WriteArray(array);
									break;
								}

								DObject obj = graph as DObject;
								if (obj != null)
								{
                                    WriteObject(obj);
									break;
								}

								PhpResource res = graph as PhpResource;
								if (res != null)
								{
									// resources are serialized as 0
									WriteInteger(0);
									break;
								}
								goto default;
							}

						default: throw new SerializationException(LibResources.GetString("serialization_unsupported_type",
									 graph.GetType().FullName));
					}
				}
			}

			/// <summary>
			/// Serializes <B>Null</B>.
			/// </summary>
			private void WriteNull()
			{
				writer.Write(Tokens.Null);
				writer.Write(Tokens.Semicolon);
			}

			/// <summary>
			/// Serializes a bool value.
			/// </summary>
			/// <param name="value">The value.</param>
			private void WriteBoolean(bool value)
			{
				writer.Write(Tokens.Boolean);
				writer.Write(Tokens.Colon);
				writer.Write(value ? '1' : '0');
				writer.Write(Tokens.Semicolon);
			}

			/// <summary>
			/// Serializes an integer.
			/// </summary>
			/// <param name="value">The integer.</param>
			private void WriteInteger(long value)
			{
				writer.Write(Tokens.Integer);
				writer.Write(Tokens.Colon);
				writer.Write(value);
				writer.Write(Tokens.Semicolon);
			}

			/// <summary>
			/// Serializes a double.
			/// </summary>
			/// <param name="value">The double.</param>
			private void WriteDouble(double value)
			{
				writer.Write(Tokens.Double);
				writer.Write(Tokens.Colon);

				// handle NaN, +Inf, -Inf
				if (Double.IsNaN(value)) writer.Write("NAN");
				else if (Double.IsPositiveInfinity(value)) writer.Write("INF");
				else if (Double.IsNegativeInfinity(value)) writer.Write("-INF");
				else writer.Write(value.ToString("R", NumberFormatInfo.InvariantInfo));

				writer.Write(Tokens.Semicolon);
			}

			/// <summary>
			/// Serializes a string.
			/// </summary>
			/// <param name="value">The string.</param>
			private void WriteString(string value)
			{
                byte[] binaryValue = writer.Encoding.GetBytes(value);

                writer.Write(Tokens.String);
				writer.Write(Tokens.Colon);
                writer.Write(binaryValue.Length);
				writer.Write(Tokens.Colon);
				writer.Write(Tokens.Quote);

                // flush the StreamWriter before accessing its underlying stream
                writer.Flush();

                writer.BaseStream.Write(binaryValue, 0, binaryValue.Length);
				writer.Write(Tokens.Quote);
				writer.Write(Tokens.Semicolon);
			}

			/// <summary>
			/// Serializes binary data.
			/// </summary>
			/// <param name="value">The data.</param>
			private void WriteBinaryData(byte[] value)
			{
				writer.Write(Tokens.String);
				writer.Write(Tokens.Colon);
				writer.Write(value.Length);
				writer.Write(Tokens.Colon);
				writer.Write(Tokens.Quote);

				// flush the StreamWriter before accessing its underlying stream
				writer.Flush();

				writer.BaseStream.Write(value, 0, value.Length);
				writer.Write(Tokens.Quote);
				writer.Write(Tokens.Semicolon);
			}

			/// <summary>
			/// Serializes a <see cref="PhpReference"/>.
			/// </summary>
			/// <param name="value">The reference.</param>
			private void WriteReference(PhpReference value)
			{
				sequenceNumber--;
				if (!value.IsAliased)
				{
					Serialize(value.Value);
					return;
				}

				int seq;
				if (serializedRefs.TryGetValue(value, out seq))
				{
					// this reference has already been serialized -> write out its seq. number
					writer.Write(Tokens.Reference);
					writer.Write(Tokens.Colon);
					writer.Write(seq);
					writer.Write(Tokens.Semicolon);
				}
				else
				{
					serializedRefs.Add(value, sequenceNumber + 1);

					if (value.Value is DObject && serializedRefs.TryGetValue(value.Value, out seq))
					{
						// this reference's value has already been serialized -> write out its seq. number
						// (this is to handle situations such as array($x, &$x), where $x is an object instance)
						writer.Write(Tokens.Reference);
						writer.Write(Tokens.Colon);
						writer.Write(seq);
						writer.Write(Tokens.Semicolon);
					}
					else Serialize(value.Value);
				}
			}

			/// <summary>
			/// Serializes a <see cref="PhpArray"/>.
			/// </summary>
			/// <param name="value">The array.</param>
			private void WriteArray(PhpArray value)
			{
				serializedRefs[value] = sequenceNumber;

				writer.Write(Tokens.Array);
				writer.Write(Tokens.Colon);
				writer.Write(value.Count);
				writer.Write(Tokens.Colon);
				writer.Write(Tokens.BraceOpen);

				// write out array items in the correct order
				foreach (KeyValuePair<IntStringKey, object> entry in value)
				{
					Serialize(entry.Key.Object);

					// don't assign a seq number to array keys
					sequenceNumber--;
					Serialize(entry.Value);
				}

				writer.Write(Tokens.BraceClose);
			}

			/// <summary>
			/// Serializes a <see cref="DObject"/>.
			/// </summary>
			/// <param name="value">The object.</param>
            /// <remarks>Avoids redundant serialization of the same object by using <see cref="serializedRefs"/>.</remarks>
			private void WriteObject(DObject value)
			{
				int seq;
				if (serializedRefs.TryGetValue(value, out seq))
				{
					// this object instance has already been serialized -> write out its seq. number
					writer.Write(Tokens.ObjectRef);
					writer.Write(Tokens.Colon);
					writer.Write(seq);
					writer.Write(Tokens.Semicolon);
					sequenceNumber--;
				}
				else
				{
                    serializedRefs.Add(value, sequenceNumber);

                    //
                    if (value.GetType() == typeof(ClrObject) || value is IClrValue)
                        WriteClrObjectInternal(value.RealObject);
                    else
                        WritePhpObjectInternal(value);
                }
            }

            /// <summary>
            /// Serializes <see cref="DObject"/> using PHP serialization.
            /// </summary>
            /// <param name="value">The object.</param>
            private void WritePhpObjectInternal(DObject/*!*/value)
            {
                byte[] binaryClassName;

                // determine class name
                bool avoid_pic_name = false;
                string class_name = null;
                __PHP_Incomplete_Class pic = value as __PHP_Incomplete_Class;
                if (pic != null)
                {
                    if (pic.__PHP_Incomplete_Class_Name.IsSet)
                    {
                        avoid_pic_name = true;
                        class_name = pic.__PHP_Incomplete_Class_Name.Value as string;
                    }
                }
                if (value is stdClass) class_name = stdClass.ClassName;
                if (class_name == null) class_name = value.TypeName;

                // is the instance PHP5.1 Serializable?
                if (value.RealObject is Library.SPL.Serializable)
                {
                    context.Stack.AddFrame();
                    object res = PhpVariable.Dereference(value.InvokeMethod("serialize", null, context));
                    if (res == null)
                    {
                        // serialize returned NULL -> serialize the instance as NULL
                        WriteNull();
                        return;
                    }

                    byte[] resdata = null;

                    if (res is PhpString)
                    {
                        res = res.ToString();
                    }

                    if (res is string)
                    {
                        resdata = writer.Encoding.GetBytes((string)res);
                    }
                    else if (res is PhpBytes)
                    {
                        resdata = ((PhpBytes)res).ReadonlyData;
                    }

                    if (resdata == null)
                    {
                        // serialize did not return NULL nor a string -> throw an exception
                        SPL.Exception.ThrowSplException(
                            _ctx => new SPL.Exception(_ctx, true),
                            context,
                            string.Format(CoreResources.serialize_must_return_null_or_string, value.TypeName), 0, null);
                    }

                    writer.Write(Tokens.ObjectSer);
                    writer.Write(Tokens.Colon);

                    binaryClassName = writer.Encoding.GetBytes(class_name);

                    // write out class name
                    writer.Write(binaryClassName.Length);
                    writer.Write(Tokens.Colon);
                    writer.Write(Tokens.Quote);

                    // flush the StreamWriter before accessing its underlying stream
                    writer.Flush();

                    writer.BaseStream.Write(binaryClassName, 0, binaryClassName.Length);
                    writer.Write(Tokens.Quote);
                    writer.Write(Tokens.Colon);

                    // write out the result of serialize
                    writer.Write(resdata.Length);
                    writer.Write(Tokens.Colon);
                    writer.Write(Tokens.BraceOpen);

                    // flush the StreamWriter before accessing its underlying stream
                    writer.Flush();

                    writer.BaseStream.Write(resdata, 0, resdata.Length);
                    writer.Write(Tokens.BraceClose);
                    return;
                }

                // try to call the __sleep method
                bool sleep_called;
                PhpArray ser_props = value.Sleep(ClassContext, context, out sleep_called);

                if (sleep_called && ser_props == null)
                {
                    // __sleep did not return an array -> serialize the instance as NULL
                    WriteNull();
                    return;
                }

                writer.Write(Tokens.Object);
                writer.Write(Tokens.Colon);

                // write out class name
                binaryClassName = writer.Encoding.GetBytes(class_name);

                // write out class name
                writer.Write(binaryClassName.Length);
                writer.Write(Tokens.Colon);
                writer.Write(Tokens.Quote);

                // flush the StreamWriter before accessing its underlying stream
                writer.Flush();

                writer.BaseStream.Write(binaryClassName, 0, binaryClassName.Length);
                writer.Write(Tokens.Quote);
                writer.Write(Tokens.Colon);

                // write out property count
                if (ser_props != null) writer.Write(ser_props.Count);
                else writer.Write(value.Count - (avoid_pic_name ? 1 : 0));
                writer.Write(Tokens.Colon);
                writer.Write(Tokens.BraceOpen);

                // write out properties
                if (ser_props != null) WriteSleepResult(value, ser_props);
                else WriteAllProperties(value, avoid_pic_name);

                writer.Write(Tokens.BraceClose);
            }

            /// <summary>
            /// Serializes an object using .NET binary formatter.
            /// </summary>
            /// <param name="realObject">The object.</param>
            private void WriteClrObjectInternal(object realObject)
            {
                writer.Write(Tokens.ClrObject);
                writer.Write(Tokens.Colon);
                writer.Write(Tokens.BraceOpen);

                // flush the StreamWriter before accessing its underlying stream
                writer.Flush();
                
                // serialize CLR object
                var formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                formatter.Serialize(writer.BaseStream, realObject);
                
                //
                writer.Write(Tokens.BraceClose);
            }

			/// <summary>
			/// Serializes properties whose names have been returned by <c>__sleep</c>.
			/// </summary>
			/// <param name="value">The instance containing the properties to serialize.</param>
			/// <param name="propertiesToSerialize">The array containing names of the properties to serialize.</param>
			private void WriteSleepResult(DObject value, PhpArray propertiesToSerialize)
			{
				// serialize the properties whose names have been returned by __sleep

				foreach (KeyValuePair<string, object> pair in Serialization.EnumerateSerializableProperties(
					value, propertiesToSerialize, context))
				{
					// write out the property name and the property value
					Serialize(pair.Key);
					sequenceNumber--; // don't assign a seq number to property names
					Serialize(pair.Value);
				}
			}

			/// <summary>
			/// Serializes all properties of a given instance.
			/// </summary>
			/// <param name="value">The instance containing the properties to serialize.</param>
			/// <param name="avoidPicName">If <B>true</B>, the property named <c>__PHP_Incomplete_Class_Name</c>
			/// should not be serialized.</param>
			private void WriteAllProperties(DObject value, bool avoidPicName)
			{
				// if have no sleep result, serialize all instance properties

				foreach (KeyValuePair<string, object> pair in Serialization.EnumerateSerializableProperties(value))
				{
					if (avoidPicName && pair.Key == __PHP_Incomplete_Class.ClassNameFieldName)
					{
						// skip the __PHP_Incomplete_Class_Name field
						continue;
					}

					// write out the property name and the property value
					Serialize(pair.Key);
					sequenceNumber--; // don't assign a seq number to property names
					Serialize(pair.Value);
				}
			}

			#endregion
		}

		/// <summary>
		/// Implements the deserialization functionality. Deserializes the data on the provided
		/// <see cref="Stream"/> and reconstitutes the graph of objects.
		/// </summary>
        internal class ObjectReader : Serializer.ClassContextHolder
		{
			#region BackReference

			/// <summary>
			/// Intermediate representation of a <B>r</B> or <B>R</B> record in serialized stream.
			/// </summary>
			private class BackReference
			{
				/// <summary>
				/// The index referenced by this back-reference record.
				/// </summary>
				private int index;

				/// <summary>
				/// If <B>true</B>, this is a proper <B>&amp;</B> reference (<B>R</B>), if <B>false</B>,
				/// this is an object instance reference (<B>r</B>) following the PHP 5 reference
				/// semantics in objects.
				/// </summary>
				private bool isProper;

				/// <summary>
				/// Creates a new <see cref="BackReference"/> with a given index.
				/// </summary>
				/// <param name="index">The index of the record being referred to.</param>
				/// <param name="isProper">Indicates whether this is a <B>&amp;</B> reference, or
				/// just object identity (valid only for objects - class instances).</param>
				internal BackReference(int index, bool isProper)
				{
					this.index = index;
					this.isProper = isProper;
				}

				/// <summary>
				/// Returns the index that is being referred to.
				/// </summary>
				internal int Index
				{
					get { return index; }
				}

				/// <summary>
				/// Returns <B>true</B> is this a <B>&amp;</B> reference. See <see cref="isProper"/>.
				/// </summary>
				internal bool IsProper
				{
					get { return isProper; }
				}
			}

			#endregion

			#region Fields and Properties

			private readonly ScriptContext/*!*/ context;

			/// <summary>
			/// The stream to read serialized data from.
			/// </summary>
			private readonly Stream/*!*/ stream;

            /// <summary>
            /// Encoding to be used for conversion from binary to unicode strings.
            /// </summary>
            private readonly Encoding/*!*/ encoding;

			/// <summary>
            /// List of objects deserialized from the reader.
			/// </summary>
			/// <remarks>
			/// In its first phase, the deserializer reads the input stream token by token and stores the
			/// deserialized items to this <see cref="List{T}"/>. If a proper back-reference (<B>&amp;</B>)
			/// is encountered, the referenced item is converted to <see cref="PhpReference"/> and a
			/// <see cref="BackReference"/> instance is stored to <see cref="atoms"/>. End of array item and
			/// object property lists are delimited by the <see cref="delimiter"/> singleton. In the second phase,
			/// after the whole stream has been read, the object graph is built from this list (see
			/// <see cref="BuildObjectGraph"/>).
			/// </remarks>
			private List<object> atoms;

			/// <summary>
			/// Maps sequence numbers used in the serialized stream to indices in the <see cref="atoms"/>
			/// list.
			/// </summary>
			/// <remarks>
			/// This <see cref="List{T}"/> is built simultaneously with <see cref="atoms"/> during the
			/// first &quot;parsing&quot; phase.
			/// </remarks>
			private List<int> sequenceMap;

			/// <summary>
            /// The lookahead symbol of the parser input (i.e. the <see cref="Consume"/>).
			/// </summary>
			private char lookAhead;

            /// <summary>
            /// Tells whether Consume methods are in Unicode reading mode. Legacy only, will be removed.
            /// </summary>
            private bool unicodeMode;

            /// <summary>
            /// Used for switching back from Unicode mode. Legacy only, will be removed.
            /// </summary>
            private long lastUnicodeCharacterPos;

            /// <summary>
            /// Used by Unicode consume to buffer bytes and read characters, if possible.
            /// </summary>
            private byte[] miniByteBuffer;

            /// <summary>
            /// Used by Unicode consume to buffer chars.
            /// </summary>
            private char[] miniCharBuffer;

            /// <summary>
            /// Used by Unicode consume to decode characters;
            /// </summary>
            private Decoder decoder;

			/// <summary>
			/// If <B>true</B>, there are no more characters in the input stream.
			/// </summary>
			private bool endOfStream;

			/// <summary>
			/// If <B>true</B>, the next item being added to the <see cref="atoms"/> list should not be
			/// assigned a sequence number.
			/// </summary>
			private bool skipSequenceNumber;

			/// <summary>
			/// Marks ends of array items and ends of object properties in the <see cref="atoms"/> list.
			/// </summary>
			private static object delimiter = new object();

			/// <summary>
			/// Current position in the <see cref="atoms"/> list during object graph building.
			/// </summary>
			private int atomCounter;

            /// <summary>
            /// Temporarily used <see cref="StringBuilder"/>. Remember it to save GC.
            /// This method always returns the same instance of <see cref="StringBuilder"/>, it will always reset its <see cref="StringBuilder.Length"/> to <c>0</c>.
            /// </summary>
            private StringBuilder/*!*/GetTemporaryStringBuilder(int initialCapacity)
            {
                var tmp = tmpStringBuilder;

                if (tmp != null)
                {
                    tmp.Length = 0;
                }
                else
                {
                    tmpStringBuilder = tmp = new StringBuilder(initialCapacity, int.MaxValue);
                }

                return tmp;
            }
            private StringBuilder tmpStringBuilder;
            
			#endregion

			#region Construction

			/// <summary>
			/// Creates a new <see cref="ObjectReader"/> with a given <see cref="StreamReader"/>.
			/// </summary>
			/// <param name="context">The current <see cref="ScriptContext"/>.</param>
			/// <param name="stream">The stream to read serialized data from.</param>
            /// <param name="encoding">Encoding used to read serialized strings.</param>
            /// <param name="caller">DTypeDesc of the caller's class context if it is known or UnknownTypeDesc if it should be determined lazily.</param>
            internal ObjectReader(ScriptContext/*!*/ context, Stream/*!*/ stream, Encoding/*!*/ encoding, DTypeDesc caller)
                :base(caller)
			{
				Debug.Assert(context != null && stream != null);
				this.context = context;
				this.stream = stream;
                this.encoding = encoding;
				this.atoms = new List<object>();
				this.sequenceMap = new List<int>();
                this.miniByteBuffer = new byte[1];
                this.miniCharBuffer = new char[1];
                this.decoder = encoding.GetDecoder();

				// read look ahead character
				Consume();
			}

			#endregion

			#region Parser helpers: Throw*, Consume, AddAtom

			/// <summary>
			/// Throws a <see cref="SerializationException"/> due to an unexpected character.
			/// </summary>
			private void ThrowUnexpected()
			{
				throw new SerializationException(LibResources.GetString("unexpected_character_in_stream"));
			}

			/// <summary>
			/// Throws a <see cref="SerializationException"/> due to an unexpected end of stream.
			/// </summary>
			private void ThrowEndOfStream()
			{
				throw new SerializationException(LibResources.GetString("unexpected_end_of_stream"));
			}

			/// <summary>
			/// Throws a <see cref="SerializationException"/> due to an data type.
			/// </summary>
			private void ThrowInvalidDataType()
			{
				throw new SerializationException(LibResources.GetString("invalid_data_bad_type"));
			}

			/// <summary>
			/// Throws a <see cref="SerializationException"/> due to an invalid length marker.
			/// </summary>
			private void ThrowInvalidLength()
			{
				throw new SerializationException(LibResources.GetString("invalid_data_bad_length"));
			}

			/// <summary>
			/// Throws a <see cref="SerializationException"/> due to an invalid back-reference.
			/// </summary>
			private void ThrowInvalidReference()
			{
				throw new SerializationException(LibResources.GetString("invalid_data_bad_back_reference"));
			}

			/// <summary>
			/// Consumes the look ahead character and moves to the next character in the input stream.
			/// </summary>
			/// <returns>The old (consumed) look ahead character.</returns>
            /// <remarks>The consumed value is 8-bit, always in range 0x00 - 0xff.</remarks>
			private char Consume()
			{
                if (unicodeMode)
                {
                    unicodeMode = false;
                    endOfStream = false;
                    stream.Seek(lastUnicodeCharacterPos, SeekOrigin.Begin);
                    Consume(); // update lookahead
                }

				if (endOfStream) ThrowEndOfStream();

				char ret = lookAhead;
				int next = stream.ReadByte();

				if (next == -1)
				{
					endOfStream = true;
					lookAhead = (char)0;
				}
				else lookAhead = (char)next;
				return ret;
			}

            /// <summary>
            /// Consumes Unicode character based on encoding.
            /// </summary>
            /// <returns></returns>
            private char ConsumeLegacy()
            {
                if (!unicodeMode && !endOfStream)
                {
                    unicodeMode = true;
                    stream.Seek(stream.Position - 1, SeekOrigin.Begin);
                    ConsumeLegacy();
                }

                if (endOfStream) ThrowEndOfStream();

                lastUnicodeCharacterPos = stream.Position;
                char ret = lookAhead;

                while (true)
                {
                    bool completed;
                    int bytesUsed;
                    int charsUsed;

                    int next = stream.ReadByte();

                    if (next == -1)
                    {
                        endOfStream = true;
                        lookAhead = (char)0;
                        return ret;
                    }

                    miniByteBuffer[0] = unchecked((byte)next);

                    decoder.Convert(miniByteBuffer, 0, 1, miniCharBuffer, 0, 1, false, out bytesUsed, out charsUsed, out completed);

                    if (charsUsed == 1) break;
                }

                lookAhead = miniCharBuffer[0];
                return ret;
            }

			/// <summary>
			/// Consumes a given look ahead character and moves to the next character in the input stream.
			/// </summary>
			/// <param name="ch">The character that should be consumed.</param>
			/// <remarks>If <paramref name="ch"/> does not match current look ahead character,
			/// <see cref="ThrowUnexpected"/> is called.</remarks>
			private void Consume(char ch)
			{
                if (unicodeMode)
                {
                    unicodeMode = false;
                    endOfStream = false;
                    stream.Seek(lastUnicodeCharacterPos, SeekOrigin.Begin);
                    Consume(); // update lookahead
                }

				if (endOfStream) ThrowEndOfStream();

				if (lookAhead != ch) ThrowUnexpected();
                int next = stream.ReadByte();

				if (next == -1)
				{
					endOfStream = true;
					lookAhead = (char)0;
				}
				else lookAhead = (char)next;
			}

            /// <summary>
            /// Tries to consume a given look ahead character and, if successful, moves to the next character in the input stream.
            /// </summary>
            /// <param name="ch">The character that should be consumed.</param>
            /// <remarks>If <paramref name="ch"/> does not match current look ahead character,
            /// <see cref="ThrowUnexpected"/> is called.</remarks>
            /// <returns>True if a character was successfully consumed, otherwise false.</returns>
            private bool TryConsume(char ch)
            {
                if (unicodeMode)
                {
                    unicodeMode = false;
                    endOfStream = false;
                    stream.Seek(lastUnicodeCharacterPos, SeekOrigin.Begin);
                    Consume(); // update lookahead
                }

                if (endOfStream) return false;

                if (lookAhead != ch) return false;
                int next = stream.ReadByte();

                if (next == -1)
                {
                    endOfStream = true;
                    lookAhead = (char)0;
                }
                else lookAhead = (char)next;

                return true;
            }

            private void Seek(long position)
            {
                stream.Seek(position, SeekOrigin.Begin);

                if (unicodeMode)
                    ConsumeLegacy();
                else
                    Consume();
            }

			/// <summary>
			/// Adds an item to the <see cref="atoms"/> list and optionally assigns a sequence number to it.
			/// </summary>
			/// <param name="obj">The item to add.</param>
			private void AddAtom(object obj)
			{
				if (!skipSequenceNumber) sequenceMap.Add(atoms.Count);
				else skipSequenceNumber = false;

				atoms.Add(obj);
			}

			#endregion

            #region Utils

            /// <summary>
            /// Quickly check if the look ahead byte is digit. Assumes the value is in range 0x00 - 0xff.
            /// </summary>
            /// <param name="lookAhead">The lookAhead byte value.</param>
            /// <returns>True if value is in range '0'-'9'.</returns>
            private static bool IsDigit(char lookAhead)
            {
                return Digit(lookAhead) != -1;
            }

            /// <summary>
            /// Quickly determine the numeric value of given lookAhead byte.
            /// </summary>
            /// <param name="lookAhead">The lookAhead byte value.</param>
            /// <returns></returns>
            private static int Digit(char lookAhead)
            {
                int num = unchecked((int)lookAhead - (int)'0');
                return (num >= 0 && num <= 9) ? num : -1;
            }

            #endregion

            #region Parser

            /// <summary>
			/// The top-level parser method. 
			/// </summary>
			/// <remarks>Just a switch over the look ahead characters that delegates the work to one of
			/// <see cref="ParseNull"/>, <see cref="ParseBoolean"/>, <see cref="ParseInteger"/>, <see cref="ParseDouble"/>,
			/// <see cref="ParseString"/>, <see cref="ParseArray"/>, <see cref="ParseObject"/>, <see cref="ParseReference"/>,
			/// <see cref="ParseObjectRef"/>.</remarks>
			private void Parse()
			{
				switch (Consume())
				{
					case Tokens.Null: ParseNull(); break;
					case Tokens.Boolean: ParseBoolean(); break;
					case Tokens.Integer: ParseInteger(); break;
					case Tokens.Double: ParseDouble(); break;
					case Tokens.String: ParseString(); break;
					case Tokens.Array: ParseArray(); break;
					case Tokens.Object: ParseObject(false); break;
					case Tokens.ObjectSer: ParseObject(true); break;
                    case Tokens.ClrObject: ParseClrObject(); break;
					case Tokens.Reference: ParseReference(); break;
					case Tokens.ObjectRef: ParseObjectRef(); break;

					default: ThrowUnexpected(); break;
				}
			}

			/// <summary>
			/// Reads a signed 64-bit integer number from the <see cref="stream"/>.
			/// </summary>
			/// <returns>The integer.</returns>
			private long ReadInteger()
			{
				// pattern:
				// [+-]?[0-9]+

                long number = 0;
                
                bool minus = (lookAhead == '-');
                if (minus || (lookAhead == '+'))
                    Consume();

                int digit;  // == Digit(lookAhead)
                if ((digit = Digit(lookAhead)) == -1)
                    ThrowUnexpected();

                do
                {
                    // let it overflow just as PHP does
                    number = unchecked((10 * number) + digit);
                    Consume();

                } while ((digit = Digit(lookAhead)) != -1);

				return (minus ? unchecked(-number) : number);
			}

			/// <summary>
			/// Reads a double-precision floating point number from the <see cref="stream"/>.
			/// </summary>
			/// <returns>The double.</returns>
			private double ReadDouble()
			{
				// pattern:
				// NAN
				// [+-]INF
				// [+-]?[0-9]*[.]?[0-9]*([eE][+-]?[0-9]+)?

                // NaN
				if (lookAhead == 'N')
				{
					Consume();
					Consume('A');
					Consume('N');
					return Double.NaN;
				}

                // mantissa + / -
                int sign = 1;
                if (lookAhead == '+') Consume();
				else if (lookAhead == '-')
				{
					sign = -1;
					Consume();
				}

				// Infinity
				if (lookAhead == 'I')
				{
					Consume();
					Consume('N');
					Consume('F');
					return (sign > 0 ? Double.PositiveInfinity : Double.NegativeInfinity);
				}

                // reconstruct the number:
                StringBuilder number = GetTemporaryStringBuilder(16);
                if (sign < 0) number.Append('-');

                // [^;]*
                while (Tokens.Semicolon != lookAhead)
                {
                    number.Append(lookAhead);
                    Consume();
                }

                double result;
                if (!Double.TryParse(number.ToString(), NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent | NumberStyles.AllowLeadingSign, NumberFormatInfo.InvariantInfo, out result))
                    ThrowUnexpected();

                return result;
			}

			/// <summary>
			/// Reads a string with a given length surrounded by quotes from the <see cref="stream"/>.
			/// </summary>
			/// <param name="length">The expected length of the string.</param>
			/// <returns>Byte array or null if string appears to be unicode (old functionality of serialize).</returns>
			private byte[]/*!*/ReadString(int length)
			{
                //ASCII character - we can expect if will be always there (UTF16 is not supported).
				Consume(Tokens.Quote);

                if (endOfStream) ThrowEndOfStream();

                if (length > 0)
                {
                    byte[] buffer = new byte[length];

                    // use current lookahead
                    buffer[0] = unchecked((byte)lookAhead);

                    // read the rest from stream
                    int rlen = stream.Read(buffer, 1, length - 1);
                    // unicode string would be longer or of same length, so it is safe to fail if end of stream was reached
                    if (rlen != length - 1) ThrowEndOfStream();

                    // this just updates lookahead, and returns the lastahead which we already used
                    Consume();

                    // try to consume
                    bool success = TryConsume(Tokens.Quote);

                    if (success)                        
                        return buffer;
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    return ArrayUtils.EmptyBytes;
                }
			}

            /// <summary>
            /// Reads a string with a given length surrounded by quotes from the <see cref="stream"/>.
            /// </summary>
            /// <param name="length">The expected length of the string.</param>
            /// <returns>The string or null.</returns>
            private string ReadStringUnicode(int length)
            {
                var bytes = ReadString(length);

                if (bytes == null) return null;

                return encoding.GetString(bytes);
            }

            /// <summary>
            /// LEGACY functionality, will be removed in future.
            /// </summary>
            /// <param name="length"></param>
            /// <returns></returns>
            private string ReadStringLegacy(int length)
            {
                var sb = GetTemporaryStringBuilder(length);

                Consume(Tokens.Quote);
                while (length-- > 0) sb.Append(ConsumeLegacy());
                Consume(Tokens.Quote);

                return sb.ToString();
            }

			/// <summary>
			/// Parses the <B>N</B> token.
			/// </summary>
			private void ParseNull()
			{
				Consume(Tokens.Semicolon);
				AddAtom(null);
			}

			/// <summary>
			/// Parses the <B>b</B> token.
			/// </summary>
			private void ParseBoolean()
			{
				Consume(Tokens.Colon);
				switch (Consume())
				{
					case '0': AddAtom(false); break;
					case '1': AddAtom(true); break;
					default: ThrowUnexpected(); break;
				}
				Consume(Tokens.Semicolon);
			}

			/// <summary>
			/// Parses the <B>i</B> token.
			/// </summary>
			private void ParseInteger()
			{
				Consume(Tokens.Colon);

				long i = ReadInteger();
				if (i >= Int32.MinValue && i <= Int32.MaxValue) AddAtom((int)i);
				else AddAtom(i);
				
				Consume(Tokens.Semicolon);
			}

			/// <summary>
			/// Parses the <B>d</B> token.
			/// </summary>
			private void ParseDouble()
			{
				Consume(Tokens.Colon);
				AddAtom(ReadDouble());
				Consume(Tokens.Semicolon);
			}

			/// <summary>
			/// Parses the <B>s</B> token.
			/// </summary>
			private void ParseString()
			{
				Consume(Tokens.Colon);
				int length = (unchecked((int)ReadInteger()));
				if (length < 0) ThrowInvalidLength();

                long position = stream.Position;
                Consume(Tokens.Colon);
				var str = ReadString(length);

                if (str != null && TryConsume(Tokens.Semicolon))
                {
                    AddAtom(new PhpBytes(str));
                }
                else
                {
                    Seek(position);
                    AddAtom(ReadStringLegacy(length));
                    Consume(Tokens.Semicolon);
                }
			}

			/// <summary>
			/// Parses the <B>a</B> token.
			/// </summary>
			private void ParseArray()
			{
				Consume(Tokens.Colon);
				int length = (unchecked((int)ReadInteger()));
				if (length < 0) ThrowInvalidLength();

				Consume(Tokens.Colon);
				AddAtom(new PhpArray(length / 2, length / 2));
				Consume(Tokens.BraceOpen);

				while (length-- > 0)
				{
					skipSequenceNumber = true;

					Parse();

                    // J: do not encode byte[] to string
                    //if (atoms[atoms.Count - 1] is PhpBytes)
                    //{
                    //    atoms[atoms.Count - 1] = encoding.GetString(((PhpBytes)atoms[atoms.Count - 1]).ReadonlyData);
                    //}

					Parse();
				}
				atoms.Add(delimiter);

				Consume(Tokens.BraceClose);
			}

			/// <summary>
			/// Parses the <B>O</B> and <B>C</B> tokens.
			/// </summary>
			/// <param name="serializable">If <B>true</B>, the last token eaten was <B>C</B>, otherwise <B>O</B>.</param>
			private void ParseObject(bool serializable)
			{
				Consume(Tokens.Colon);
				int length = (unchecked((int)ReadInteger()));
				if (length < 0) ThrowInvalidLength();

                long position = stream.Position;
                Consume(Tokens.Colon);
                string class_name = ReadStringUnicode(length);

                if (class_name == null)
                {
                    Seek(position);
                    class_name = ReadStringLegacy(length);
                }

				Consume(Tokens.Colon);
				length = (unchecked((int)ReadInteger()));
				if (length < 0) ThrowInvalidLength();

				Consume(Tokens.Colon);

				// bind to the specified class
				DObject obj = Serialization.GetUninitializedInstance(class_name, context);

				if (obj == null)
				{
					throw new SerializationException(LibResources.GetString("class_instantiation_failed",
						class_name));
				}

				// check whether the instance is PHP5.1 Serializable
				if (serializable && !(obj.RealObject is Library.SPL.Serializable))
				{
					throw new SerializationException(LibResources.GetString("class_has_no_unserializer",
						class_name));
				}
				AddAtom(obj);
				atoms.Add(serializable);

				Consume(Tokens.BraceOpen);

				if (serializable)
				{
                    if (length > 0)
                    {
                        // add serialized representation to be later passed to unserialize
                        if (endOfStream) ThrowEndOfStream();

                        byte[] buffer = new byte[length];

                        // use current lookahead
                        buffer[0] = unchecked((byte)lookAhead);

                        // read the rest from stream
                        int rlen = stream.Read(buffer, 1, length - 1);
                        if (rlen != length - 1) ThrowEndOfStream();

                        // this just updates lookahead, and returns the lastahead which we already used
                        Consume();

                        atoms.Add(new PhpBytes(buffer));
                    }
                    else
                    {
                        atoms.Add(PhpBytes.Empty);
                    }
				}
				else
				{
					// parse properties
					while (length-- > 0)
					{
						skipSequenceNumber = true;

						// parse property name
						Parse();

						// verify that the name is either string or int
						object name = atoms[atoms.Count - 1];

                        if (name is PhpBytes)   // property name needs to be string
                            name = atoms[atoms.Count - 1] = encoding.GetString(((PhpBytes)name).ReadonlyData);
                        
						if (!(name is string))
						{
							if (!(name is int)) ThrowInvalidDataType();
							atoms[atoms.Count - 1] = name.ToString();
						}

						// parse property value
						Parse();
					}
					atoms.Add(delimiter);
				}

				Consume(Tokens.BraceClose);
			}

            /// <summary>
            /// Parses the <B>T</B> token.
            /// </summary>
            /// <remarks>Expects CLR object formatted using <see cref="System.Runtime.Serialization.Formatters.Binary.BinaryFormatter"/>.</remarks>
            private void ParseClrObject()
            {
                // T,{DATA}

                Consume(Tokens.Colon);
                if (lookAhead != Tokens.BraceOpen)
                    ThrowUnexpected();

                var formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                var obj = formatter.Deserialize(stream);
                AddAtom(ClrObject.WrapDynamic(obj));

                atoms.Add(false);       // !serializable
                atoms.Add(delimiter);   // end

                // restore lookAhead state:
                int next = stream.ReadByte();
                if (next == -1)
                {
                    endOfStream = true;
                    lookAhead = (char)0;
                }
                else lookAhead = (char)next;
                Consume(Tokens.BraceClose);
            }

			/// <summary>
			/// Parses the <B>R</B> token.
			/// </summary>
			private void ParseReference()
			{
				Consume(Tokens.Colon);
				int seq_number = (unchecked((int)ReadInteger())) - 1;
				Consume(Tokens.Semicolon);

				if (seq_number < 0 || seq_number >= sequenceMap.Count) ThrowInvalidReference();
				int index = sequenceMap[seq_number];

				// make the referenced atom a PhpReference
				PhpReference reference = atoms[index] as PhpReference;
				if (reference == null)
				{
					reference = new PhpReference(atoms[index]);
					atoms[index] = reference;
				}

				atoms.Add(new BackReference(index, true));
			}

			/// <summary>
			/// Parses the <B>r</B> token.
			/// </summary>
			private void ParseObjectRef()
			{
				Consume(Tokens.Colon);
				int seq_number = (unchecked((int)ReadInteger())) - 1;
				Consume(Tokens.Semicolon);

				if (seq_number < 0 || seq_number >= sequenceMap.Count) ThrowInvalidReference();
				int index = sequenceMap[seq_number];

				atoms.Add(new BackReference(index, false));
			}

			#endregion

			#region BuildObjectGraph, Deserialize

			/// <summary>
			/// Builds the object graph from <see cref="atoms"/>.
			/// </summary>
			/// <returns></returns>
			private object BuildObjectGraph()
			{
				object atom = atoms[atomCounter++];

				if (atom != null /*&& Type.GetTypeCode(atom.GetType()) == TypeCode.Object*//* note (Jakub): useless check, in result much slower than a few .isinst */)
                {
					// back reference (either r or R)
					BackReference back_ref = atom as BackReference;
					if (back_ref != null)
					{
                        PhpReference reference;
                        
                        object ref_val = atoms[back_ref.Index];

						if (back_ref.IsProper) return ref_val;

						// object references should reference objects only
						reference = ref_val as PhpReference;
						if ((reference != null && !(reference.Value is DObject)) &&
							!(ref_val is DObject)) ThrowInvalidReference();

						return ref_val;
					}

					// dereference an eventual reference
					object value = PhpVariable.Dereference(atom);

					// array
					PhpArray array = value as PhpArray;
					if (array != null)
					{
						while (atoms[atomCounter] != delimiter)
						{
                            object arraykey = BuildObjectGraph();
                            object arrayvalue = BuildObjectGraph();

                            if (arraykey is PhpBytes)// IntStringKey does not allow PhpBytes yet
                                arraykey = encoding.GetString(((PhpBytes)arraykey).ReadonlyData);

                            array.Add(arraykey, arrayvalue);
						}
						atomCounter++; // for the delimiter
						return atom;
					}

					// object
					DObject obj = value as DObject;
					if (obj != null)
					{
						BuildDObject(obj);
						return atom;
					}
				}

				// no special treatment for the rest of the types
				return atom;
			}

			/// <summary>
			/// Builds a <see cref="DObject"/> from atoms (the object itself given as parameter).
			/// </summary>
			/// <param name="obj">The instance.</param>
			private void BuildDObject(DObject obj)
			{
				bool serializable = ((bool)atoms[atomCounter++] == true);

				if (serializable && obj.RealObject is Library.SPL.Serializable)
				{
					// pass the serialized data to unserialize
					context.Stack.AddFrame(BuildObjectGraph());
					obj.InvokeMethod("unserialize", null, context);
					return;
				}

				while (atoms[atomCounter] != delimiter)
				{
					string property_name = (string)BuildObjectGraph();
					object property_value = BuildObjectGraph();

					Debug.Assert(property_name != null);
					Serialization.SetProperty(obj, property_name, property_value, context);
				}
				atomCounter++; // for the delimiter

				// invoke __wakeup on the deserialized instance
				obj.Wakeup(ClassContext, context);
			}

			/// <summary>
			/// Deserializes the data from the <see cref="stream"/> and reconstitutes the graph of objects.
			/// </summary>
			/// <returns>The top object of the deserialized graph.</returns>
			internal object Deserialize()
			{
				// parsing phase
				Parse();

				// object building phase
				atomCounter = 0;
				return BuildObjectGraph();
			}

			#endregion
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
		private readonly Encoding encoding;

        /// <summary>
        /// DTypeDesc of the caller class context known already or UnknownTypeDesc if class context should be determined lazily.
        /// </summary>
        private readonly DTypeDesc caller;

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

		#region Construction

        ///// <summary>
        ///// Creates a new <see cref="PhpFormatter"/> with <see cref="ASCIIEncoding"/> and
        ///// default <see cref="Context"/>.
        ///// </summary>
        //public PhpFormatter()
        //{
        //    this.encoding = new ASCIIEncoding();
        //}

		/// <summary>
		/// Creates a new <see cref="PhpFormatter"/> with a given <see cref="Encoding"/> and
		/// default <see cref="Context"/>.
		/// </summary>
		/// <param name="encoding">The encoding to be used when writing and reading the serialization stream.</param>
        /// <param name="caller">DTypeDesc of the caller's class context if it is known or UnknownTypeDesc if it should be determined lazily.</param>
        public PhpFormatter(Encoding encoding, DTypeDesc caller)
		{
            this.caller = caller;

			// no UTF8 BOM!
			if (encoding is UTF8Encoding)
                this.encoding = new UTF8Encoding(false);
			else
				this.encoding = (encoding ?? new ASCIIEncoding());
		}

		#endregion

		#region Serialize and Deserialize

		/// <summary>
		/// Serializes an object, or graph of objects with the given root to the provided stream.
		/// </summary>
		/// <param name="serializationStream">The stream where the formatter puts the serialized data.</param>
		/// <param name="graph">The object, or root of the object graph, to serialize.</param>
		public void Serialize(Stream serializationStream, object graph)
		{
			if (serializationStream == null)
			{
				throw new ArgumentNullException("serializationStream");
			}
			serializationPermission.Demand();

			StreamWriter stream_writer = new StreamWriter(serializationStream, encoding);
			ObjectWriter object_writer = new ObjectWriter(ScriptContext.CurrentContext, stream_writer, caller);
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
		public object Deserialize(Stream serializationStream)
		{
			if (serializationStream == null)
			{
				throw new ArgumentNullException("serializationStream");
			}
			serializationPermission.Demand();

			ScriptContext context = ScriptContext.CurrentContext;
			ObjectReader object_reader = new ObjectReader(context, serializationStream, encoding, caller);
            return object_reader.Deserialize();
		}

		#endregion
	}
}
