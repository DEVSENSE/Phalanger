using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization;
using PHP.Core;
using PHP.Core.Reflection;
using System.IO;
using System.Security.Permissions;

namespace PHP.Library
{
    /// <summary>
    /// Implements IFormatter for fast and safe binary serialization, which is optimized for 
    /// statically-typed code (e.g. objects which have similarly typed field and same set of fields).
    /// </summary>
    /// <remarks>
    /// Optimizations were directed towards deserialization and serialization performance was not a primary concern.
    /// </remarks>
    public sealed class PhalangerFormatter : IFormatter
    {
        #region Internal types

        /// <summary>
        /// Represents type of serialized class.
        /// </summary>
        enum ClassType
        {
            /// <summary>
            /// Represents a class with unknown type (only for purposes of serialization).
            /// </summary>
            Unknown = -1,

            /// <summary>
            /// Represents a class with strict set of members. 
            /// </summary>
            Strict = 0,

            /// <summary>
            /// Represents a class with variable set of members.
            /// </summary>
            Dynamic = 1,

            /// <summary>
            /// Represents a class which implements it's custom serializer through SPL.Serializable interface.
            /// </summary>
            Serializable = 2,
        }

        /// <summary>
        /// Represents type of referenced value. This is a first byte for each item in value table.  
        /// </summary>
        enum ReferencedValueType
        {
            /// <summary>
            /// Represents an object value. These referenced values are followed by class id and list of property 
            /// values.
            /// </summary>
            Object = 0,

            /// <summary>
            /// Represents a value wrapped into PhpReference object. These referenced values are followed by
            /// actual serialized value.
            /// </summary>
            Value = 1
        }


        /// <summary>
        /// Represents a value type code, which usually preceedes a value with unknown type or is used by arrays 
        /// and classes.
        /// </summary>
        enum ValueTypeCode
        {
            /// <summary>
            /// Represents a value with invalid type. Only for purposes of serialization.
            /// It is used when array index and value type codes are determined.
            /// </summary>
            Invalid = -2,

            /// <summary>
            /// Represents a value with unknown type. Only for purposes of serialization.
            /// It is a starting type of array value and index type before first item is read.
            /// </summary>
            Unknown = -1,

            /// <summary>
            /// Represents a generic null value. Value is automatically ommited.
            /// </summary>
            Null = 0,

            /// <summary>
            /// Represents a dynamic value. This is special type used by class fields and arrays.
            /// If this type code is specified each of depending values is preceeded by type information.
            /// </summary>
            Dynamic = 1,

            /// <summary>
            /// Represents an 8-bit boolean value.
            /// </summary>
            Boolean = 2,

            /// <summary>
            /// Represents an 8-bit signed integer value. This type is converted to Int16 if needed.
            /// </summary>
            Int8 = 3,

            /// <summary>
            /// Represents a 16-bit signed integer value. This type is converted to Int32 if needed.
            /// </summary>
            Int16 = 4,

            /// <summary>
            /// Represents a 32-bit signed integer value. This type is converted to Int64 if needed.
            /// </summary>
            Int32 = 5,

            /// <summary>
            /// Represents a 64-bit signed integer value.
            /// </summary>
            Int64 = 6,

            /// <summary>
            /// Represents a 64-bit float value.
            /// </summary>
            Float = 7,

            /// <summary>
            /// Represents a string value. String length is encoded using variable length.
            /// </summary>
            String = 8,

            /// <summary>
            /// Represents an array value. It is followed by array variable length,index and value types and
            /// a list of array value pairs.
            /// </summary>
            Array = 9,

            /// <summary>
            /// Represents an 8-bit reference. This type is converted to Reference16 if needed.
            /// </summary>
            Reference8 = 10,
            /// <summary>
            /// Represents a 16-bit reference. This type is converted to Reference32 if needed.
            /// </summary>
            Reference16 = 11,

            /// <summary>
            /// Represents a 32-bit reference.
            /// </summary>
            Reference32 = 12,

            /// <summary>
            /// Represents a binary data.
            /// </summary>
            Binary = 13
        }

        /// <summary>
        /// Represents class field description.
        /// </summary>
        private class ClassFieldDesc
        {
            /// <summary>
            /// Name of the class field.
            /// </summary>
            public string Name;

            /// <summary>
            /// Value type of the class field.
            /// </summary>
            public ValueTypeCode ValueType;
        }

        /// <summary>
        /// Represents class description for purpose of serialization and deserialization.
        /// </summary>
        private class ClassDesc
        {
            /// <summary>
            /// ID of the class.
            /// </summary>
            public int ID;

            /// <summary>
            /// Name of the class.
            /// </summary>
            public string Name;

            /// <summary>
            /// Class type.
            /// </summary>
            public ClassType ClassType;

            /// <summary>
            /// List of field descriptions.
            /// </summary>
            public List<ClassFieldDesc> Fields;

            /// <summary>
            /// A map of field orders. Used by serialization.
            /// </summary>
            public Dictionary<string, int> FieldOrderMap;

            /// <summary>
            /// Initializes a new class. Used by serialization.
            /// </summary>
            /// <param name="id">ID of the class.</param>
            /// <param name="name">Name of the class.</param>
            public ClassDesc(int id, string name)
            {
                ClassType = ClassType.Unknown;
                Fields = new List<ClassFieldDesc>();
                FieldOrderMap = new Dictionary<string, int>();
                ID = id;
                Name = name;
            }

            /// <summary>
            /// Initializes a new class. Used by deserialization.
            /// </summary>
            public ClassDesc()
            {
                Fields = new List<ClassFieldDesc>();
            }
        }

        /// <summary>
        /// Describes object's field. Used by serialization.
        /// </summary>
        private struct FieldDesc
        {
            /// <summary>
            /// Name of the field.
            /// </summary>
            public string Name;

            /// <summary>
            /// Value of the field.
            /// </summary>
            public object Value;
        }

        /// <summary>
        /// Represents processed object value. Used by serialization.
        /// </summary>
        private class ObjectDesc
        {
            /// <summary>
            /// ID of the object.
            /// </summary>
            public int ID;

            /// <summary>
            /// ID of the object's class.
            /// </summary>
            public int ClassID;

            /// <summary>
            /// String containing the serialized value. Used if the object is SPL.Serializable.
            /// </summary>
            public string SerializedValue;

            /// <summary>
            /// Describes whether the object is invalid. Invalid objects are result of an error during processing.
            /// If true, the object will serialize as null.
            /// </summary>
            public bool Invalid;

            /// <summary>
            /// List of fields and their values.
            /// </summary>
            public List<FieldDesc> Fields;

            /// <summary>
            /// Initializes object description.
            /// </summary>
            /// <param name="id">ID of the object.</param>
            /// <param name="classid">ID of object's class.</param>
            public ObjectDesc(int id, int classid)
            {
                ID = id;
                ClassID = classid;
                Fields = new List<FieldDesc>();
            }
        }

        /// <summary>
        /// Represents a referenced value. This value was encountered as PhpReference during serialization.
        /// </summary>
        private class ValueDesc
        {
            /// <summary>
            /// ID of the value.
            /// </summary>
            public int ID;

            /// <summary>
            /// Processed value.
            /// </summary>
            public object Value;

            /// <summary>
            /// Initializes the value description.
            /// </summary>
            /// <param name="id">Id of the referenced value.</param>
            /// <param name="value">Processed value.</param>
            public ValueDesc(int id, object value)
            {
                ID = id;
                Value = value;
            }
        }

        /// <summary>
        /// Represents array item description.
        /// </summary>
        private struct ArrayItemDesc
        {
            /// <summary>
            /// Processed index of the item.
            /// </summary>
            public object Index;

            /// <summary>
            /// Processed value of the item.
            /// </summary>
            public object Value;

            /// <summary>
            /// Initializes array item description structure.
            /// </summary>
            /// <param name="index">Processed index.</param>
            /// <param name="value">Processed value.</param>
            public ArrayItemDesc(object index, object value)
            {
                Index = index;
                Value = value;
            }
        }

        /// <summary>
        /// Represents processed array (PhpArray).
        /// </summary>
        private class ArrayValue
        {
            /// <summary>
            /// Current index type.
            /// </summary>
            public ValueTypeCode IndexType;

            /// <summary>
            /// Current value type.
            /// </summary>
            public ValueTypeCode ValueType;

            /// <summary>
            /// List of values.
            /// </summary>
            public List<ArrayItemDesc> Values;

            /// <summary>
            /// Initializes ArrayValue instance.
            /// </summary>
            public ArrayValue()
            {
                Values = new List<ArrayItemDesc>();
            }
        }

        /// <summary>
        /// Represents processed object reference (PhpObject).
        /// </summary>
        private class ObjectRef
        {
            /// <summary>
            /// ID of the referenced object.
            /// </summary>
            public int ObjectID;

            /// <summary>
            /// Initializes new object reference.
            /// </summary>
            /// <param name="id">Id of the referenced object.</param>
            public ObjectRef(int id)
            {
                ObjectID = id;
            }
        }

        /// <summary>
        /// Represents processed value reference (PhpReference).
        /// </summary>
        private class ValueRef
        {
            /// <summary>
            /// Referenced value ID.
            /// </summary>
            public int ValueID;

            /// <summary>
            /// Initializes new value reference.
            /// </summary>
            /// <param name="id">ID of the referenced value.</param>
            public ValueRef(int id)
            {
                ValueID = id;
            }
        }

        #endregion

        #region ObjectEncoder

        /// <summary>
        /// Encodes object value to byte array.
        /// </summary>
        private class ObjectEncoder : Serializer.ClassContextHolder
        {
            #region Fields and properties

            /// <summary>
            /// ScriptContext for this object.
            /// </summary>
            private ScriptContext/*!*/ context;

            /// <summary>
            /// Class table. This is filled during processing.
            /// </summary>
            private Dictionary<string, ClassDesc> classTable;

            /// <summary>
            /// Class ID table. This is filled during processing.
            /// </summary>
            private Dictionary<int, ClassDesc> classIDTable;


            /// <summary>
            /// Object table. This is filled during processing.
            /// </summary>
            private Dictionary<object, ObjectDesc> objectTable;

            /// <summary>
            /// Object ID table. This is filled during processing.
            /// </summary>
            private Dictionary<int, ObjectDesc> objectIDTable;


            /// <summary>
            /// Referenced value table. This is filled during processing.
            /// </summary>
            private Dictionary<object, ValueDesc> valueTable;

            /// <summary>
            /// Referenced value ID table. This is filled during processing.
            /// </summary>
            private Dictionary<int, ValueDesc> valueIDTable;

            /// <summary>
            /// Stored number of next class to be allocated.
            /// </summary>
            public int nextClassID;

            /// <summary>
            /// Stores number of next reference to be allocated.
            /// </summary>
            public int nextValueID;

            /// <summary>
            /// Current encoding.
            /// </summary>
            Encoding encoding;

            #endregion

            #region Constructor

            /// <summary>
            /// Initializes new instance of ObjectEncoder.
            /// </summary>
            /// <param name="context">Current script context.</param>
            /// <param name="encoding">Encoding object to use for encoding strings.</param>
            /// <param name="caller">DTypeDesc of the caller's class context if it is known or UnknownTypeDesc if it should be determined lazily.</param>
            public ObjectEncoder(ScriptContext/*!*/ context, Encoding/*!*/ encoding, DTypeDesc caller)
                : base(caller)
            {
                this.encoding = encoding;
                this.context = context;
                classTable = new Dictionary<string,ClassDesc>();
                objectTable = new Dictionary<object,ObjectDesc>();
                valueTable = new Dictionary<object,ValueDesc>();
                classIDTable = new Dictionary<int,ClassDesc>();
                objectIDTable = new Dictionary<int,ObjectDesc>();
                valueIDTable = new Dictionary<int,ValueDesc>();
                nextValueID = 0;
                nextClassID = 0;
            }

            #endregion

            #region Type Code Decision

            /// <summary>
            /// Retrieves type code of 32-bit integer value. This method decides, whether
            /// the value can be fitted into 8, 16 or 32-bit signed integer.
            /// </summary>
            /// <param name="value">Integer value.</param>
            /// <returns>ValueTypeCode representing the value's type</returns>
            private static ValueTypeCode GetInt32TypeCode(int value)
            {
                if ((value & 0x7f80) == 0) return ValueTypeCode.Int8;
                else if ((value & 0x7fff8000) == 0) return ValueTypeCode.Int16;
                else return ValueTypeCode.Int32;
            }

            /// <summary>
            /// Retrieves type code of 64-bit integer value. This method decides, whether
            /// the value can be fitted into 8, 16, 32, 64-bit signed integer.
            /// </summary>
            /// <param name="value">Integer value.</param>
            /// <returns>ValueTypeCode representing the value's type</returns>
            private static ValueTypeCode GetInt64TypeCode(long value)
            {
                if ((value & 0x7f80) == 0) return ValueTypeCode.Int8;
                else if ((value & 0x7fff8000) == 0) return ValueTypeCode.Int16;
                else if ((value & 0x7fffffff80000000) == 0) return ValueTypeCode.Int32;
                else return ValueTypeCode.Int64;
            }

            /// <summary>
            /// Retrieves type code of a reference id. This method decides, whether
            /// the value can be fitted into 8, 16 or 32-bit signed integer.
            /// </summary>
            /// <param name="value">Integer value.</param>
            /// <returns>ValueTypeCode representing the value's type</returns>
            private static ValueTypeCode GetReferenceTypeCode(int value)
            {
                if ((value & 0x7f80) == 0) return ValueTypeCode.Reference8;
                else if ((value & 0x7fff8000) == 0) return ValueTypeCode.Reference16;
                else return ValueTypeCode.Reference32;
            }

            /// <summary>
            /// Get type code of a processed value. This method is used by processing functions.
            /// </summary>
            /// <param name="value">Processed value.</param>
            /// <returns>Value type code of the processed value. Returns Invalid type code if value is not recognized.</returns>
            private static ValueTypeCode GetProcessedValueTypeCode(object value)
            {
                if (value == null) return ValueTypeCode.Null;

                switch (Type.GetTypeCode(value.GetType()))
                {
					case TypeCode.Boolean:
                        return ValueTypeCode.Boolean;
					case TypeCode.Int32:
                        return GetInt32TypeCode((int)value);
					case TypeCode.Int64:
                        return GetInt64TypeCode((long)value);
					case TypeCode.Double:
                        return ValueTypeCode.Float;
					case TypeCode.String:
                        return ValueTypeCode.String;
                    case TypeCode.Object:
                        ObjectRef objref = value as ObjectRef;
						if (objref != null)
						{
							return GetReferenceTypeCode(objref.ObjectID);
						}

                        ValueRef valref = value as ValueRef;
						if (valref != null)
						{
							return GetReferenceTypeCode(valref.ValueID);
						}

                        PhpBytes bytes = value as PhpBytes;
						if (bytes != null)
						{
							return ValueTypeCode.Binary;
						}

						ArrayValue array = value as ArrayValue;
						if (array != null)
						{
							return ValueTypeCode.Array;
                        }

                        Debug.Fail();
                        return ValueTypeCode.Invalid;              

                    default: 
                        Debug.Fail();
                        return ValueTypeCode.Invalid;
                }
            }

            #endregion

            #region Encoded value length computations

            /// <summary>
            /// Gets size of encoded integer value. This depends on the value.
            /// </summary>
            /// <param name="value">Integer value.</param>
            /// <returns>Returns 1 (for -128 to 127), 2 (-32768 to 32767) or 4 (otherwise).</returns>
            private static int GetEncodedInt32Length(long value)
            {
                if ((value & 0x7f80) == 0) return 1;
                else if ((value & 0x7fff8000) == 0) return 2;
                else return 4;
            }

            /// <summary>
            /// Gets size of encoded integer value. This depends on the value.
            /// </summary>
            /// <param name="value">Integer value.</param>
            /// <returns>Returns 1 (for -128 to 127), 2 (-32768 to 32767), 4 (MININT to MAXINT) or 8.</returns>
            private static int GetEncodedInt64Length(long value)
            {
                if ((value & 0x7f80) == 0) return 1;
                else if ((value & 0x7fff8000) == 0) return 2;
                else if ((value & 0x7fffffff80000000) == 0) return 4;
                else return 8;
            }

            /// <summary>
            /// Gets size of the encoded size information. This depends on the value.
            /// </summary>
            /// <param name="value">Size to be encoded.</param>
            /// <returns>Returns 1 if size is 0 to 254, 3 if the size is 255 to 65535 and 5 otherwise.</returns>
            /// <remarks>
            /// First byte indicates size if smaller than 254. Value 254 indicates that 16-bit unsigned integer follows.
            /// Value 255 indicates that 32-bit unsigned integer follows.
            /// </remarks>
            private static int GetEncodedSizeLength(int value)
            {
                if (value < 0)
                    return 5;
                else if (value >= byte.MaxValue - 1)
                    if (value < UInt16.MaxValue)
                        return 3;
                    else
                        return 5;
                else
                    return 1;
            }

            /// <summary>
            /// Gets size of encoded internal reference number.
            /// </summary>
            /// <param name="value">Reference number to be encoded.</param>
            /// <returns>Returns 1,3 or 5 depending on the value.</returns>
            private static int GetEncodedInternalReferenceLength(int value)
            {
                return GetEncodedSizeLength(value);
            }

            /// <summary>
            /// Gets encoded length of a object/value reference.
            /// </summary>
            /// <param name="value">Reference number to encode.</param>
            /// <returns>Returns 1,2 or 4 depending on the value.</returns>
            private static int GetEncodedReferenceLength(int value)
            {                
                if ((value & 0x7f80) != value) return 1;
                else if ((value & 0x7fff8000) == 0) return 2;
                else return 4;
            }

            #endregion

            #region Processing helpers

            private static int GetEncodedStringLength(string value, Encoding encoding)
            {
                int byteCount = encoding.GetByteCount(value);
                return GetEncodedSizeLength(byteCount) + byteCount;
            }

            /// <summary>
            /// Restricts type code for indices. Basically, handles only Int8 - Int32 and String.
            /// Other type codes result in Invalid type code.
            /// </summary>
            /// <param name="typeCode"></param>
            /// <param name="newTypeCode"></param>
            private static void RestrictKeyTypeCode(ref ValueTypeCode typeCode, ValueTypeCode newTypeCode)
            {
                switch (typeCode)
                {
                    case ValueTypeCode.Unknown:
                        switch (newTypeCode)
                        {
                            case ValueTypeCode.String:
                            case ValueTypeCode.Int8:
                            case ValueTypeCode.Int16:
                            case ValueTypeCode.Int32:
                                typeCode = newTypeCode;
                                return;
                            default:
                                typeCode = ValueTypeCode.Invalid;
                                return;
                        }

                    case ValueTypeCode.String:
                        switch (newTypeCode)
                        {
                            case ValueTypeCode.String:
                                return;
                            case ValueTypeCode.Int8:
                            case ValueTypeCode.Int16:
                            case ValueTypeCode.Int32:
                                typeCode = ValueTypeCode.Dynamic;
                                return;
                            default:
                                typeCode = ValueTypeCode.Invalid;
                                return;
                        }

                    case ValueTypeCode.Int8:
                        switch (newTypeCode)
                        {
                            case ValueTypeCode.String:
                                typeCode = ValueTypeCode.Dynamic;
                                return;
                            case ValueTypeCode.Int8:
                                return;
                            case ValueTypeCode.Int16:
                            case ValueTypeCode.Int32:
                                typeCode = newTypeCode;
                                return;
                            default:
                                typeCode = ValueTypeCode.Invalid;
                                return;
                        }

                    case ValueTypeCode.Int16:
                        switch (newTypeCode)
                        {
                            case ValueTypeCode.String:
                                typeCode = ValueTypeCode.Dynamic;
                                return;
                            case ValueTypeCode.Int8:
                            case ValueTypeCode.Int16:
                                return;
                            case ValueTypeCode.Int32:
                                typeCode = newTypeCode;
                                return;
                            default:
                                typeCode = ValueTypeCode.Invalid;
                                return;
                        }

                    case ValueTypeCode.Int32:
                        switch (newTypeCode)
                        {
                            case ValueTypeCode.String:
                                typeCode = ValueTypeCode.Dynamic;
                                return;
                            case ValueTypeCode.Int8:
                            case ValueTypeCode.Int16:                                
                            case ValueTypeCode.Int32:
                                return;
                            default:
                                typeCode = ValueTypeCode.Invalid;
                                return;
                        }

                    case ValueTypeCode.Dynamic:
                        switch (newTypeCode)
                        {
                            case ValueTypeCode.String:
                            case ValueTypeCode.Int8:
                            case ValueTypeCode.Int16:                                
                            case ValueTypeCode.Int32:
                                return;
                            default:
                                typeCode = ValueTypeCode.Invalid;
                                return;
                        }

                    default:
                        typeCode = ValueTypeCode.Invalid;
                        return;
                }                
            }

            /// <summary>
            /// Restricts type code for general values. Basically this handles mainly int8 -> int16 -> int32 -> int64 and reference chains.
            /// Other values are cast to Dynamic when other type is used.
            /// </summary>
            /// <param name="typeCode"></param>
            /// <param name="newTypeCode"></param>
            private static void RestrictValueTypeCode(ref ValueTypeCode typeCode, ValueTypeCode newTypeCode)
            {
                switch(typeCode)
                {
                    case ValueTypeCode.Unknown:
                        typeCode = newTypeCode;
                        return;
                    case ValueTypeCode.Int8:
                        switch (newTypeCode)
                        {
                            case ValueTypeCode.Int8:
                                return;
                            case ValueTypeCode.Int16:
                            case ValueTypeCode.Int32:
                            case ValueTypeCode.Int64:
                                typeCode = newTypeCode;
                                return;
                            default:
                                typeCode = ValueTypeCode.Dynamic;
                                return;
                        }
 
                    case ValueTypeCode.Int16:
                        switch (newTypeCode)
                        {
                            case ValueTypeCode.Int8:                                
                            case ValueTypeCode.Int16:
                                return;
                            case ValueTypeCode.Int32:
                            case ValueTypeCode.Int64:
                                typeCode = newTypeCode;
                                return;
                            default:
                                typeCode = ValueTypeCode.Dynamic;
                                return;
                        }

                    case ValueTypeCode.Int32:
                        switch (newTypeCode)
                        {
                            case ValueTypeCode.Int8:
                            case ValueTypeCode.Int16:
                            case ValueTypeCode.Int32:
                                return;
                            case ValueTypeCode.Int64:
                                typeCode = newTypeCode;
                                return;
                            default:
                                typeCode = ValueTypeCode.Dynamic;
                                return;
                        }
                    case ValueTypeCode.Int64:
                        switch (newTypeCode)
                        {
                            case ValueTypeCode.Int8:
                            case ValueTypeCode.Int16:
                            case ValueTypeCode.Int32:
                            case ValueTypeCode.Int64:
                                return;
                            default:
                                typeCode = ValueTypeCode.Dynamic;
                                return;
                        }
                    case ValueTypeCode.Reference8:
                        switch (newTypeCode)
                        {
                            case ValueTypeCode.Reference8:
                                return;
                            case ValueTypeCode.Reference16:
                            case ValueTypeCode.Reference32:
                                typeCode = newTypeCode;
                                return;
                            default:
                                typeCode = ValueTypeCode.Dynamic;
                                return;
                        }
                    case ValueTypeCode.Reference16:
                        switch (newTypeCode)
                        {
                            case ValueTypeCode.Reference8:
                            case ValueTypeCode.Reference16:
                                return;
                            case ValueTypeCode.Reference32:
                                typeCode = newTypeCode;
                                return;
                            default:
                                typeCode = ValueTypeCode.Dynamic;
                                return;
                        }
                    case ValueTypeCode.Reference32:
                        switch (newTypeCode)
                        {
                            case ValueTypeCode.Reference8:
                            case ValueTypeCode.Reference16:
                            case ValueTypeCode.Reference32:
                                return;
                            default:
                                typeCode = ValueTypeCode.Dynamic;
                                return;
                        }
                    default:
                        if (typeCode != newTypeCode)
                            typeCode = ValueTypeCode.Dynamic;
                        return;
                }
            }

            /// <summary>
            /// Restricts type of array's field and class itself.
            /// </summary>
            /// <param name="classDesc"></param>
            /// <param name="fieldName"></param>
            /// <param name="typeCode"></param>
            /// <param name="classCreated">Tells whether class was just created.</param>
            private static void RestrictClassFieldType(ClassDesc classDesc, string fieldName, ValueTypeCode typeCode, bool classCreated)
            {                
                int current;
                if (!classDesc.FieldOrderMap.TryGetValue(fieldName, out current))
                {
                    //field isn't present
                    ClassFieldDesc fieldDesc = new ClassFieldDesc();
                    fieldDesc.Name = fieldName;
                    fieldDesc.ValueType = typeCode;
                    classDesc.FieldOrderMap.Add(fieldName, classDesc.Fields.Count);
                    classDesc.Fields.Add(fieldDesc);                    
 
                    if (!classCreated)
                    {
                        // make class dynamic
                        classDesc.ClassType = ClassType.Dynamic;
                    }

                    return;
                }
                else
                {
                    // restrict the current type
                    ClassFieldDesc fieldDesc = classDesc.Fields[current];
                    RestrictValueTypeCode(ref fieldDesc.ValueType, typeCode);
                }
            }

            /// <summary>
            /// Restricts type of class based on list of object's fields. Presumption is, that object's fields were already added to class.
            /// </summary>
            /// <param name="classDesc"></param>
            /// <param name="fields"></param>
            private static void RestrictClassType(ClassDesc classDesc, List<FieldDesc> fields)
            {
                switch(classDesc.ClassType)
                {
                    case ClassType.Strict:
                        // classDesc already contains fields present in object
                        // if fields.Count is higher than number of classDesc fields, function was called in invalid context (class should be marked dynamic) 
                        // if fields.Count is lower than number classDesc fields, one of the classDesc fields is not present in the object
                        // if fields.Count is same as number of classDesc fields, class is still strict
                        // (this can be easily proved)
                        if (classDesc.Fields.Count != fields.Count)
                        {
                            if (classDesc.Fields.Count < fields.Count)
                            {
                                Debug.Fail();
                            }
                            else
                            {
                                classDesc.ClassType = ClassType.Dynamic;
                            }
                        }

                        return;
                    case ClassType.Dynamic:
                        return;
                    default:
                        Debug.Fail();
                        return;
                }
            }

            #endregion

            #region Encoding methods

            private static void EncodeClassType(ClassType typeCode, byte[] buffer, int offset)
            {
                buffer[offset] = (byte)typeCode;
            }

            private static void EncodeObjectType(ReferencedValueType typeCode, byte[] buffer, int offset)
            {
                buffer[offset] = (byte)typeCode;
            }

            private static void EncodeTypeCode(ValueTypeCode typeCode, byte[] buffer, int offset)
            {
                buffer[offset] = (byte)typeCode;
            }

            private static void EncodeBoolean(bool value, byte[] buffer, int offset)
            {
                buffer[offset] = (value ? (byte)1 : (byte)0);
            }

            private static void EncodeInt8(int value, byte[] buffer, int offset)
            {
                unchecked
                {
                    buffer[offset] = (byte)value;
                }
            }

            private static void EncodeInt16(int value, byte[] buffer, int offset)
            {
                unchecked
                {
                    buffer[offset] = (byte)(value >> 8);
                    buffer[offset + 1] = (byte)value;
                }
            }

            private static void EncodeInt32(int value, byte[] buffer, int offset)
            {
                unchecked
                {
                    buffer[offset] = (byte)(value >> 24);
                    buffer[offset + 1] = (byte)(value >> 16);
                    buffer[offset + 2] = (byte)(value >> 8);
                    buffer[offset + 3] = (byte)(value);
                }
            }

            private static void EncodeInt64(long value, byte[] buffer, int offset)
            {
                unchecked
                {
                    buffer[offset] = (byte)(value >> 56);
                    buffer[offset + 1] = (byte)(value >> 48);
                    buffer[offset + 2] = (byte)(value >> 40);
                    buffer[offset + 3] = (byte)(value >> 32);
                    buffer[offset + 4] = (byte)(value >> 24);
                    buffer[offset + 5] = (byte)(value >> 16);
                    buffer[offset + 6] = (byte)(value >> 8);
                    buffer[offset + 7] = (byte)(value);
                }
            }

            private static void EncodeFloat(double value, byte[] buffer, int offset)
            {
                EncodeInt64(BitConverter.DoubleToInt64Bits(value), buffer, offset);
            }

            private static void EncodeInt32Dynamic(int value, byte[] buffer, ref int offset)
            {
                if ((value & 0x7f80) == 0) 
                {
                    EncodeTypeCode(ValueTypeCode.Int8, buffer, offset);
                    offset += 1;
                    EncodeInt8(value, buffer, offset);
                    offset += 1;
                }
                else if ((value & 0x7fff8000) == 0) 
                {
                    EncodeTypeCode(ValueTypeCode.Int16, buffer, offset);
                    offset += 1;
                    EncodeInt16(value, buffer, offset);
                    offset += 2;
                }
                else 
                {
                    EncodeTypeCode(ValueTypeCode.Int32, buffer, offset);
                    offset += 1;
                    EncodeInt32(value, buffer, offset);
                    offset += 4;
                }
            }

            private static void EncodeInt64Dynamic(long value, byte[] buffer, ref int offset)
            {
                if ((value & 0x7f80) == 0) 
                {
                    EncodeTypeCode(ValueTypeCode.Int8, buffer, offset);
                    offset += 1;
                    EncodeInt8((int)value, buffer, offset);
                    offset += 1;
                }
                else if ((value & 0x7fff8000) == 0) 
                {
                    EncodeTypeCode(ValueTypeCode.Int16, buffer, offset);
                    offset += 1;
                    EncodeInt16((int)value, buffer, offset);
                    offset += 2;
                }
                else if ((value & 0x7fffffff80000000) == 0) 
                {
                    EncodeTypeCode(ValueTypeCode.Int32, buffer, offset);
                    offset += 1;
                    EncodeInt32((int)value, buffer, offset);
                    offset += 4;
                }
                else 
                {
                    EncodeTypeCode(ValueTypeCode.Int64, buffer, offset);
                    offset += 1;
                    EncodeInt64(value, buffer, offset);
                    offset += 8;
                }
            }

            private static void EncodeReferenceDynamic(int value, byte[] buffer, ref int offset)
            {
                if ((value & 0x7f80) == 0) 
                {
                    EncodeTypeCode(ValueTypeCode.Reference8, buffer, offset);
                    offset += 1;
                    EncodeInt8(value, buffer, offset);
                    offset += 1;
                }
                else if ((value & 0x7fff8000) == 0) 
                {
                    EncodeTypeCode(ValueTypeCode.Reference16, buffer, offset);
                    offset += 1;
                    EncodeInt16(value, buffer, offset);
                    offset += 2;
                }
                else 
                {
                    EncodeTypeCode(ValueTypeCode.Reference32, buffer, offset);
                    offset += 1;
                    EncodeInt32(value, buffer, offset);
                    offset += 4;
                }
            }

            private static void EncodeSize(int value, byte[] buffer, ref int offset)
            {
                if (value < 0)
                {
                    buffer[offset] = 255;
                    EncodeInt32(value, buffer, offset + 1);
                    offset += 5;
                }
                else if (value >= byte.MaxValue - 1)
                {
                    if (value < UInt16.MaxValue)
                    {
                        buffer[offset] = 254;
                        EncodeInt16(value, buffer, offset+1);
                        offset += 3;
                    }
                    else
                    {
                        buffer[offset] = 255;
                        EncodeInt32(value, buffer, offset + 1);
                        offset += 5;
                    }
                }
                else
                {
                    buffer[offset] = (byte)value;
                    offset += 1;
                }
            }

            private static void EncodeInternalReference(int value, byte[] buffer, ref int offset)
            {
                EncodeSize(value, buffer, ref offset);
            }

            private static void EncodeString(string value, Encoding encoding, byte[] buffer, ref int offset)
            {
                EncodeSize(value.Length, buffer, ref offset);
                offset += encoding.GetBytes(value, 0, value.Length, buffer, offset);
            }

            #endregion

            #region Value processing

            /// <summary>
            /// Processes value and fills tables. Also, updates length of the serialized data.
            /// </summary>
            /// <param name="value">Value to use.</param>
            /// <returns>Transformed value.</returns>
            private object Process(object value)
            {
                switch (Type.GetTypeCode(value.GetType()))
                {
					case TypeCode.Boolean:
					case TypeCode.Int32:
					case TypeCode.Int64:
					case TypeCode.Double:
					case TypeCode.String:
                        return value;
                    case TypeCode.Object:
                		PhpReference reference = value as PhpReference;
						if (reference != null)
						{
							return ProcessReference(reference);
						}

						PhpBytes bytes = value as PhpBytes;
						if (bytes != null)
						{
							return value;
						}

						PhpString str = value as PhpString;
						if (str != null)
						{
							return str.ToString();
						}

						PhpArray array = value as PhpArray;
						if (array != null)
						{
							return ProcessArray(array);
						}

						DObject obj = value as DObject;
						if (obj != null)
						{
                            return ProcessObject(obj);
						}

						PhpResource res = value as PhpResource;
						if (res != null)
						{
							return 0;
						}

                        throw new SerializationException(
                            LibResources.GetString("serialization_unsupported_type", value.GetType().FullName)
                            );                       

                    default: 
                        throw new SerializationException(
                            LibResources.GetString("serialization_unsupported_type", value.GetType().FullName)
                            );
                }
            }

            /// <summary>
            /// Processes a PhpReference.
            /// </summary>
            /// <param name="value">PhpReference value.</param>
            /// <returns>ValueRef object representing the reference.</returns>
            public object ProcessReference(PhpReference value)
            {
				if (!value.IsAliased)
				{
                    // unwrap the value, it will be serialized as is
					return Process(value.Value);
				}

				ValueDesc valueDesc;
				if (valueTable.TryGetValue(value, out valueDesc))
				{
                    // we have already seen this reference, transform it to known reference
                    return new ValueRef(valueDesc.ID);
				}
				else
				{
                    // we haven't seen this reference yet, process it and safe it as value
                    valueDesc = new ValueDesc(nextValueID++, Process(value.Value));
                    valueTable.Add(value, valueDesc);
                    valueIDTable.Add(valueDesc.ID, valueDesc);
                    return new ValueRef(valueDesc.ID);
				}
            }

            /// <summary>
            /// Processes a PhpArray. This method will also determine index and value type codes.
            /// </summary>
            /// <param name="value">PhpArray value.</param>
            /// <returns>ArrayValue object represeting the array.</returns>
            public ArrayValue ProcessArray(PhpArray value)
            {
                //initialize
                ArrayValue array = new ArrayValue();
                array.IndexType = ValueTypeCode.Unknown;
                array.ValueType = ValueTypeCode.Unknown;

                // go through each key-value pair
				foreach (KeyValuePair<IntStringKey, object> entry in value)
				{
                    // process key and value
                    object key = Process(entry.Key.Object);
                    object itemValue = Process(entry.Value);

                    // restrict type codes
                    RestrictKeyTypeCode(ref array.IndexType, GetProcessedValueTypeCode(key));
                    RestrictValueTypeCode(ref array.ValueType, GetProcessedValueTypeCode(itemValue));

                    // add the processed key-value pair
                    array.Values.Add(new ArrayItemDesc(key, itemValue));
				}

                // fail if array has invalid type specified
                if (array.IndexType == ValueTypeCode.Invalid)
                {
                    Debug.Fail();
                    return null;
                }

                return array;
            }

            /// <summary>
            /// Processes a DObject. This method can create new class if needed.
            /// </summary>
            /// <param name="value">DObject value.</param>
            /// <returns>ObjectRef object reprenting the DObject.</returns>
            public ObjectRef ProcessObject(DObject value)
            {
            	ObjectDesc objectDesc;
			    if (objectTable.TryGetValue(value, out objectDesc))
			    {
				    // this object instance has already been serialized -> return its ID
                    return new ObjectRef(objectDesc.ID);
			    }
			    else
			    {
				    // determine class name
				    bool avoidPICName = false;
				    string className = null;
				    __PHP_Incomplete_Class pic = value as __PHP_Incomplete_Class;
				    if (pic != null)
				    {
					    if (pic.__PHP_Incomplete_Class_Name.IsSet)
					    {
						    avoidPICName = true;
						    className = pic.__PHP_Incomplete_Class_Name.Value as string;
					    }
				    }

				    if (className == null) className = value.TypeName;

                    // create class desc if it is not yet created
                    ClassDesc classDesc;
                    bool classCreated = false;
                    if (!classTable.TryGetValue(className, out classDesc))
                    {
                        classDesc = new ClassDesc(nextClassID++, className);
                        classTable.Add(className, classDesc);
                        classIDTable.Add(classDesc.ID, classDesc);
                        classCreated = true;
                    }

                    //create new object desc
                    ObjectDesc newObjectDesc = new ObjectDesc(nextValueID++, classDesc.ID);
                    objectTable.Add(value, newObjectDesc);
                    objectIDTable.Add(newObjectDesc.ID, newObjectDesc);

				    // is the instance PHP5.1 Serializable?
				    if (value.RealObject is Library.SPL.Serializable)
				    {
                        // update class desc
                        switch(classDesc.ClassType)
                        {
                            case ClassType.Unknown:
                                classDesc.ClassType = ClassType.Serializable;
                                break;
                            case ClassType.Serializable:
                                break;
                            default:
                                Debug.Fail();
                                classDesc.ClassType = ClassType.Dynamic;
                                break;
                        }

                        // call serialize method
					    context.Stack.AddFrame();
					    object res = PhpVariable.Dereference(value.InvokeMethod("serialize", null, context));
					    if (res == null)
                        {
                            //null was returned, invalidate object
                            newObjectDesc.Invalid = true;
                            return new ObjectRef(newObjectDesc.ID);
					    }

				        string res_str = PhpVariable.AsString(res);
				        if (res_str == null)
				        {
					        // serialize did not return NULL nor a string -> throw an exception
					        SPL.Exception e = new SPL.Exception(context, true);
					        e.__construct(context, LibResources.GetString("serialize_must_return_null_or_string", value.TypeName), 0);

					        throw new PhpUserException(e);
				        }

                        newObjectDesc.SerializedValue = res_str;

                        //return object reference
					    return new ObjectRef(newObjectDesc.ID);
				    }

                    // update class desc
                    switch (classDesc.ClassType)
                    {
                        case ClassType.Unknown:
                            classDesc.ClassType = ClassType.Strict;
                            break;
                        case ClassType.Strict:
                        case ClassType.Dynamic:
                            break;
                        default:
                            Debug.Fail();
                            classDesc.ClassType = ClassType.Dynamic;
                            break;
                    }

				    // try to call the __sleep method
				    bool sleep_called = false;
                    PhpArray ser_props = value.Sleep(ClassContext, context, out sleep_called);

				    if (sleep_called && ser_props == null)
				    {
                        newObjectDesc.Invalid = true;
                        return new ObjectRef(newObjectDesc.ID);
				    }

                    if (ser_props != null)
                    {
                        return ProcessFields(value, avoidPICName, classDesc, classCreated, newObjectDesc);
                    }

                    //go trough each field
                    return ProcessFields(value, avoidPICName, classDesc, classCreated, newObjectDesc);
                }
            }

            /// <summary>
            /// Processes object's fields.
            /// </summary>
            /// <param name="value">DObject value.</param>
            /// <param name="avoidPICName">Tells whether to avoid PHP incomplete class name.</param>
            /// <param name="classDesc">Class description.</param>
            /// <param name="classCreated">Tells whether the class was created while processing this object.</param>
            /// <param name="newObjectDesc">New object description.</param>
            /// <returns>Object reference to the object.</returns>
            private ObjectRef ProcessFields(DObject value, bool avoidPICName, ClassDesc classDesc, bool classCreated, ObjectDesc newObjectDesc)
            {
                foreach (KeyValuePair<string, object> pair in
                    Serialization.EnumerateSerializableProperties(value))
                {
                    if (avoidPICName && pair.Key == __PHP_Incomplete_Class.ClassNameFieldName)
                    {
                        // skip the __PHP_Incomplete_Class_Name field
                        continue;
                    }

                    FieldDesc field;
                    field.Name = pair.Key;
                    field.Value = Process(pair.Value);
                    newObjectDesc.Fields.Add(field);
                    RestrictClassFieldType(classDesc, field.Name, GetProcessedValueTypeCode(field.Value), classCreated);
                }

                // restrict class type based on object field list
                if (!classCreated) RestrictClassType(classDesc, newObjectDesc.Fields);

                return new ObjectRef(newObjectDesc.ID);
            }

            #endregion 

            #region Buffer length computations

            /// <summary>
            /// Counts size of encoded data using given value and filled tables.
            /// </summary>
            /// <returns></returns>
            private void ComputeBufferLength(object value, out int bufferLength, out int forwardLookup, out int[] encodedValueLengths)
            {
                int[] valueTableLengths = new int[nextValueID];
                int classTableLength = 0;
                int valueTableLength = 0;
                int valueLength = 0;

                if (nextClassID > 0 || nextValueID > 0)
                {
                    //add count of classes
                    classTableLength = GetEncodedSizeLength(nextClassID);

                    //add encoded classes length 
                    for (int i = 0; i < nextClassID; i++)
                    {
                        Debug.Assert(classIDTable.ContainsKey(i));

                        ClassDesc classDesc = classIDTable[i];

                        //class type identifier and class name
                        classTableLength += 1 + GetEncodedStringLength(classDesc.Name, encoding);

                        switch (classDesc.ClassType)
                        {
                            case ClassType.Serializable:
                                //no fields, value is serialized
                                break;
                            case ClassType.Dynamic:
                            case ClassType.Strict:
                                //number of fields
                                classTableLength += GetEncodedSizeLength(classDesc.Fields.Count);

                                foreach (ClassFieldDesc fieldDesc in classDesc.Fields)
                                {
                                    //field name and type identifier
                                    classTableLength += GetEncodedStringLength(fieldDesc.Name, encoding) + 1;
                                }
                                break;
                            default:
                                Debug.Fail();
                                break;
                        }
                    }

                    //count of objects
                    valueTableLength = GetEncodedSizeLength(nextValueID);

                    //add object and values lengths
                    for (int i = 0; i < nextValueID; i++)
                    {
                        Debug.Assert(objectIDTable.ContainsKey(i) || valueIDTable.ContainsKey(i));

                        valueTableLengths[i] = 0;

                        if (objectIDTable.ContainsKey(i))
                        {
                            // object type
                            valueTableLengths[i] += 1;

                            ObjectDesc objectDesc = objectIDTable[i];

                            Debug.Assert(classIDTable.ContainsKey(objectDesc.ClassID));

                            ClassDesc classDesc = classIDTable[objectDesc.ClassID];

                            // class reference
                            valueTableLengths[i] += GetEncodedInternalReferenceLength(objectDesc.ClassID);

                            if (objectDesc.Invalid)
                            {
                                continue;
                            }

                            switch (classDesc.ClassType)
                            {
                                case ClassType.Serializable:

                                    //serialized data string
                                    valueTableLengths[i] += GetEncodedStringLength(objectDesc.SerializedValue, encoding);
                                    break;
                                case ClassType.Strict:
                                case ClassType.Dynamic:
                                    valueTableLengths[i] += GetEncodedSizeLength(objectDesc.Fields.Count);

                                    //currently, both dynamic and strict objects specify fieldNumber
                                    foreach (FieldDesc fieldDesc in objectDesc.Fields)
                                    {
                                        Debug.Assert(classDesc.FieldOrderMap.ContainsKey(fieldDesc.Name));

                                        int fieldOrder = classDesc.FieldOrderMap[fieldDesc.Name];

                                        Debug.Assert(fieldDesc.Name == classDesc.Fields[fieldOrder].Name);

                                        ValueTypeCode typeCode = classDesc.Fields[fieldOrder].ValueType;

                                        valueTableLengths[i] += GetEncodedInternalReferenceLength(fieldOrder);
                                        valueTableLengths[i] += GetEncodedValueLength(fieldDesc.Value, typeCode);
                                    }
                                    break;
                                default:
                                    Debug.Fail();
                                    break;
                            }
                        }
                        else
                        {
                            //referenced value
                            ValueDesc valueDesc = valueIDTable[i];
                            valueTableLengths[i] += 1 + GetEncodedValueLength(valueDesc.ID, ValueTypeCode.Dynamic);
                        }

                        // add the encoded value length to the aggregated counter
                        valueTableLength += valueTableLengths[i];

                        // add value length, which is encoded in lookup table
                        valueTableLength += GetEncodedSizeLength(valueTableLengths[i]);
                    }
                }

                //length of the serialized value
                valueLength = GetEncodedValueLength(value, ValueTypeCode.Dynamic);

                int forwardLookupLength = GetEncodedSizeLength(classTableLength + valueTableLength);

                bufferLength = forwardLookupLength + classTableLength + valueTableLength + valueLength;
                forwardLookup = classTableLength + valueTableLength;
                encodedValueLengths = valueTableLengths;
            }

            /// <summary>
            /// Counts size of the encoded value.
            /// </summary>
            /// <param name="value"></param>
            /// <param name="valueType"></param>
            /// <returns></returns>
            private int GetEncodedValueLength(object value, ValueTypeCode valueType)
            {
                Debug.Assert(valueType != ValueTypeCode.Unknown && valueType != ValueTypeCode.Invalid);

                int typeInfoLength = (valueType == ValueTypeCode.Dynamic?1:0);

                if (value == null) 
                {
                    Debug.Assert(valueType == ValueTypeCode.Null || valueType == ValueTypeCode.Dynamic);
                    return typeInfoLength;
                }

                switch (Type.GetTypeCode(value.GetType()))
                {
					case TypeCode.Boolean:
                        Debug.Assert(valueType == ValueTypeCode.Boolean || valueType == ValueTypeCode.Dynamic);
                        return typeInfoLength + 1;
					case TypeCode.Int32:
                        switch(valueType)
                        {
                            case ValueTypeCode.Int8:
                                return typeInfoLength + 1;
                            case ValueTypeCode.Int16:
                                return typeInfoLength + 2;
                            case ValueTypeCode.Int32:
                                return typeInfoLength + 4;
                            case ValueTypeCode.Int64:
                                return typeInfoLength + 8;
                            case ValueTypeCode.Dynamic:
                                return typeInfoLength + GetEncodedInt32Length((int)value);
                            default:
                                Debug.Fail();
                                return 0;
                        }
					case TypeCode.Int64:
                        switch(valueType)
                        {
                            case ValueTypeCode.Int8:
                                return typeInfoLength + 1;
                            case ValueTypeCode.Int16:
                                return typeInfoLength + 2;
                            case ValueTypeCode.Int32:
                                return typeInfoLength + 4;
                            case ValueTypeCode.Int64:
                                return typeInfoLength + 8;
                            case ValueTypeCode.Dynamic:
                                return typeInfoLength + GetEncodedInt64Length((long)value);
                            default:
                                Debug.Fail();
                                return 0;
                        }
					case TypeCode.Double:
                        Debug.Assert(valueType == ValueTypeCode.Float || valueType == ValueTypeCode.Dynamic);
                        return typeInfoLength + 8;
					case TypeCode.String:
                        Debug.Assert(valueType == ValueTypeCode.String || valueType == ValueTypeCode.Dynamic);
                        return typeInfoLength + GetEncodedStringLength((string)value, encoding);
                    case TypeCode.Object:
                        ObjectRef objref = value as ObjectRef;
						if (objref != null)
						{
                            switch(valueType)
                            {
                                case ValueTypeCode.Reference8:
                                    return typeInfoLength + 1;
                                case ValueTypeCode.Reference16:
                                    return typeInfoLength + 2;
                                case ValueTypeCode.Reference32:
                                    return typeInfoLength + 4;
                                case ValueTypeCode.Dynamic:
                                    return typeInfoLength + GetEncodedReferenceLength(objref.ObjectID);
                                default:
                                    Debug.Fail();
                                    return 0;
                            }
						}

                        ValueRef valref = value as ValueRef;
						if (valref != null)
						{
                            switch(valueType)
                            {
                                case ValueTypeCode.Reference8:
                                    return typeInfoLength + 1;
                                case ValueTypeCode.Reference16:
                                    return typeInfoLength + 2;
                                case ValueTypeCode.Reference32:
                                    return typeInfoLength + 4;
                                case ValueTypeCode.Dynamic:
                                    return typeInfoLength + GetEncodedReferenceLength(valref.ValueID);
                                default:
                                    Debug.Fail();
                                    return 0;
                            }
						}

                        PhpBytes bytes = value as PhpBytes;
						if (bytes != null)
						{
                            Debug.Assert(valueType == ValueTypeCode.Binary || valueType == ValueTypeCode.Dynamic);
							return typeInfoLength + GetEncodedSizeLength(bytes.Length) + bytes.Length;
						}

						ArrayValue array = value as ArrayValue;
						if (array != null)
						{
                            Debug.Assert(valueType == ValueTypeCode.Array || valueType == ValueTypeCode.Dynamic);

                            // index type code, value type code and array size
                            int length = typeInfoLength + 2 + GetEncodedSizeLength(array.Values.Count);

                            for(int i = 0; i < array.Values.Count; i++)
                            {
                                length += GetEncodedValueLength(array.Values[i].Index, array.IndexType);
                                length += GetEncodedValueLength(array.Values[i].Value, array.ValueType);
                            }

                            return length;
                        }

                        Debug.Fail();
                        return 0;              

                    default: 
                        Debug.Fail();
                        return 0;
                }
            }

            #endregion

            #region Encoding

            /// <summary>
            /// Encodes whole value and tables into the buffer.
            /// </summary>
            /// <returns></returns>
            private void Encode(object value, int forwardLookup, int[] valueTableLengths, byte[] buffer, ref int bufferPosition)
            {
                //encode forward lookup
                EncodeSize(forwardLookup, buffer, ref bufferPosition);

                if (nextClassID > 0 || nextValueID > 0)
                {
                    //encode class count
                    EncodeSize(nextClassID, buffer, ref bufferPosition);

                    //encode classes 
                    for (int i = 0; i < nextClassID; i++)
                    {
                        Debug.Assert(classIDTable.ContainsKey(i));

                        ClassDesc classDesc = classIDTable[i];

                        //class type identifier and class name
                        EncodeClassType(classDesc.ClassType, buffer, bufferPosition);
                        bufferPosition += 1;
                        EncodeString(classDesc.Name, encoding, buffer, ref bufferPosition);

                        switch (classDesc.ClassType)
                        {
                            case ClassType.Serializable:
                                //no fields, value is serialized
                                break;
                            case ClassType.Dynamic:
                            case ClassType.Strict:
                                //number of fields
                                EncodeSize(classDesc.Fields.Count, buffer, ref bufferPosition);

                                foreach (ClassFieldDesc fieldDesc in classDesc.Fields)
                                {
                                    //field name and type identifier
                                    EncodeTypeCode(fieldDesc.ValueType, buffer, bufferPosition);
                                    bufferPosition += 1;
                                    EncodeString(fieldDesc.Name, encoding, buffer, ref bufferPosition);
                                }
                                break;
                            default:
                                Debug.Fail();
                                break;
                        }
                    }

                    //encode object count
                    EncodeSize(nextValueID, buffer, ref bufferPosition);

                    for (int i = 0; i < nextValueID; i++)
                    {
                        EncodeSize(valueTableLengths[i], buffer, ref bufferPosition);

                        Debug.Assert(objectIDTable.ContainsKey(i) || valueIDTable.ContainsKey(i));

                        if (objectIDTable.ContainsKey(i))
                        {
                            EncodeObjectType(ReferencedValueType.Object, buffer, bufferPosition);
                            bufferPosition += 1;

                            ObjectDesc objectDesc = objectIDTable[i];

                            Debug.Assert(classIDTable.ContainsKey(objectDesc.ClassID));

                            ClassDesc classDesc = classIDTable[objectDesc.ClassID];

                            // class reference
                            EncodeInternalReference(objectDesc.ClassID, buffer, ref bufferPosition);

                            if (objectDesc.Invalid)
                            {
                                continue;
                            }

                            switch (classDesc.ClassType)
                            {
                                case ClassType.Serializable:
                                    //serialized data string
                                    EncodeString(objectDesc.SerializedValue, encoding, buffer, ref bufferPosition);
                                    break;
                                case ClassType.Strict:
                                case ClassType.Dynamic:
                                    //number of fields
                                    EncodeSize(objectDesc.Fields.Count, buffer, ref bufferPosition);

                                    //currently, both dynamic and strict objects specify fieldNumber
                                    foreach (FieldDesc fieldDesc in objectDesc.Fields)
                                    {
                                        Debug.Assert(classDesc.FieldOrderMap.ContainsKey(fieldDesc.Name));

                                        int fieldOrder = classDesc.FieldOrderMap[fieldDesc.Name];

                                        Debug.Assert(fieldDesc.Name == classDesc.Fields[fieldOrder].Name);

                                        ValueTypeCode typeCode = classDesc.Fields[fieldOrder].ValueType;

                                        EncodeInternalReference(fieldOrder, buffer, ref bufferPosition);
                                        EncodeValue(fieldDesc.Value, typeCode, buffer, ref bufferPosition);
                                    }
                                    break;
                                default:
                                    Debug.Fail();
                                    break;
                            }
                        }
                        else
                        {
                            //referenced value
                            EncodeObjectType(ReferencedValueType.Value, buffer, bufferPosition);
                            bufferPosition += 1;
                            ValueDesc valueDesc = valueIDTable[i];
                            EncodeValue(valueDesc.Value, ValueTypeCode.Dynamic, buffer, ref bufferPosition);
                        }
                    }
                }

                // encode value
                EncodeValue(value, ValueTypeCode.Dynamic, buffer, ref bufferPosition);
            }

            /// <summary>
            /// Counts size of the encoded value.
            /// </summary>
            /// <param name="value"></param>
            /// <param name="valueType"></param>
            /// <param name="buffer"></param>
            /// <param name="bufferPosition"></param>
            /// <returns></returns>
            private void EncodeValue(object value, ValueTypeCode valueType, byte[] buffer, ref int bufferPosition)
            {
                Debug.Assert(valueType != ValueTypeCode.Unknown && valueType != ValueTypeCode.Invalid);

                if (value == null) 
                {
                    switch (valueType)
                    {
                        case ValueTypeCode.Null:
                            return;
                        case ValueTypeCode.Dynamic:
                            EncodeTypeCode(ValueTypeCode.Null, buffer, bufferPosition);
                            bufferPosition += 1;
                            return;
                        default:
                            Debug.Fail();
                            return;
                    }
                }

                switch (Type.GetTypeCode(value.GetType()))
                {
					case TypeCode.Boolean:
                        switch (valueType)
                        {
                            case ValueTypeCode.Boolean:
                                EncodeBoolean((bool)value, buffer, bufferPosition);
                                bufferPosition += 1;
                                return;
                            case ValueTypeCode.Dynamic:
                                EncodeTypeCode(ValueTypeCode.Boolean, buffer, bufferPosition);
                                bufferPosition += 1;
                                EncodeBoolean((bool)value, buffer, bufferPosition);
                                bufferPosition += 1;
                                return;
                            default:
                                Debug.Fail();
                                return;
                        }
					case TypeCode.Int32:
                        switch(valueType)
                        {
                            case ValueTypeCode.Int8:
                                EncodeInt8((int)value, buffer, bufferPosition);
                                bufferPosition += 1;
                                return;
                            case ValueTypeCode.Int16:
                                EncodeInt16((int)value, buffer, bufferPosition);
                                bufferPosition += 2;
                                return;
                            case ValueTypeCode.Int32:
                                EncodeInt32((int)value, buffer, bufferPosition);
                                bufferPosition += 4;
                                return;
                            case ValueTypeCode.Int64:
                                EncodeInt64((long)value, buffer, bufferPosition);
                                bufferPosition += 8;
                                return;
                            case ValueTypeCode.Dynamic:
                                EncodeInt32Dynamic((int)value, buffer, ref bufferPosition);
                                return;
                            default:
                                Debug.Fail();
                                return;
                        }
					case TypeCode.Int64:
                        switch(valueType)
                        {
                            case ValueTypeCode.Int8:
                                EncodeInt8((int)(long)value, buffer, bufferPosition);
                                bufferPosition += 1;
                                return;
                            case ValueTypeCode.Int16:
                                EncodeInt16((int)(long)value, buffer, bufferPosition);
                                bufferPosition += 2;
                                return;
                            case ValueTypeCode.Int32:
                                EncodeInt32((int)(long)value, buffer, bufferPosition);
                                bufferPosition += 4;
                                return;
                            case ValueTypeCode.Int64:
                                EncodeInt64((long)value, buffer, bufferPosition);
                                bufferPosition += 8;
                                return;
                            case ValueTypeCode.Dynamic:
                                EncodeInt64Dynamic((long)value, buffer, ref bufferPosition);
                                return;
                            default:
                                Debug.Fail();
                                return;
                        }
					case TypeCode.Double:
                        switch (valueType)
                        {
                            case ValueTypeCode.Float:
                                EncodeFloat((double)value, buffer, bufferPosition);
                                bufferPosition += 8;
                                return;
                            case ValueTypeCode.Dynamic:
                                EncodeTypeCode(ValueTypeCode.Float, buffer, bufferPosition);
                                bufferPosition += 1;
                                EncodeFloat((double)value, buffer, bufferPosition);
                                bufferPosition += 8;
                                return;
                            default:
                                Debug.Fail();
                                return;
                        }
					case TypeCode.String:
                        switch (valueType)
                        {
                            case ValueTypeCode.String:
                                EncodeString((string)value, encoding, buffer, ref bufferPosition);
                                return;
                            case ValueTypeCode.Dynamic:
                                EncodeTypeCode(ValueTypeCode.String, buffer, bufferPosition);
                                bufferPosition += 1;
                                EncodeString((string)value, encoding, buffer, ref bufferPosition);
                                return;
                            default:
                                Debug.Fail();
                                return;
                        }
                    case TypeCode.Object:
                        ObjectRef objref = value as ObjectRef;
						if (objref != null)
						{
                            //table is encoded backwards, we need to change references this way
                            switch(valueType)
                            {
                                case ValueTypeCode.Reference8:
                                    EncodeInt8(objref.ObjectID, buffer, bufferPosition);
                                    bufferPosition += 1;
                                    return;
                                case ValueTypeCode.Reference16:
                                    EncodeInt16(objref.ObjectID, buffer, bufferPosition);
                                    bufferPosition += 2;
                                    return;
                                case ValueTypeCode.Reference32:
                                    EncodeInt32(objref.ObjectID, buffer, bufferPosition);
                                    bufferPosition += 4;
                                    return;
                                case ValueTypeCode.Dynamic:
                                    EncodeReferenceDynamic(objref.ObjectID, buffer, ref bufferPosition);
                                    return;
                                default:
                                    Debug.Fail();
                                    return;
                            }
						}

                        ValueRef valref = value as ValueRef;
						if (valref != null)
						{
                            //table is encoded backwards, we need to encode references this way
                            switch(valueType)
                            {
                                case ValueTypeCode.Reference8:
                                    EncodeInt8(valref.ValueID, buffer, bufferPosition);
                                    bufferPosition += 1;
                                    return;
                                case ValueTypeCode.Reference16:
                                    EncodeInt16(valref.ValueID, buffer, bufferPosition);
                                    bufferPosition += 2;
                                    return;
                                case ValueTypeCode.Reference32:
                                    EncodeInt32(valref.ValueID, buffer, bufferPosition);
                                    bufferPosition += 4;
                                    return;
                                case ValueTypeCode.Dynamic:
                                    EncodeReferenceDynamic(valref.ValueID, buffer, ref bufferPosition);
                                    return;
                                default:
                                    Debug.Fail();
                                    return;
                            }
						}

                        PhpBytes bytes = value as PhpBytes;
						if (bytes != null)
						{
                            switch (valueType)
                            {
                                case ValueTypeCode.Binary:
							        break;
                                case ValueTypeCode.Dynamic:
                                    EncodeTypeCode(ValueTypeCode.Binary, buffer, bufferPosition);
                                    bufferPosition += 1;
							        break;
                                default:
                                    Debug.Fail();
                                    return;
                            }

                            EncodeSize(bytes.Length, buffer, ref bufferPosition);
                            Buffer.BlockCopy(bytes.ReadonlyData, 0, buffer, bufferPosition, bytes.Length);
                            bufferPosition += bytes.Length;

                            return;
						}

						ArrayValue array = value as ArrayValue;
						if (array != null)
						{
                            switch (valueType)
                            {
                                case ValueTypeCode.Array:
							        break;
                                case ValueTypeCode.Dynamic:
                                    EncodeTypeCode(ValueTypeCode.Array, buffer, bufferPosition);
                                    bufferPosition += 1;
							        break;
                                default:
                                    Debug.Fail();
                                    return;
                            }

                            EncodeTypeCode(array.IndexType, buffer, bufferPosition);
                            bufferPosition += 1;
                            EncodeTypeCode(array.ValueType, buffer, bufferPosition);
                            bufferPosition += 1;
                            EncodeSize(array.Values.Count, buffer, ref bufferPosition);

                            for(int i = 0; i < array.Values.Count; i++)
                            {
                                EncodeValue(array.Values[i].Index, array.IndexType, buffer, ref bufferPosition);
                                EncodeValue(array.Values[i].Value, array.ValueType, buffer, ref bufferPosition);
                            }

                            return;
                        }

                        Debug.Fail();
                        return;              

                    default: 
                        Debug.Fail();
                        return;
                }
            }

            #endregion

            #region Serialize

            public byte[] Serialize(object value)
            {
                object processedValue = Process(value);

                int bufferLength;
                int forwardLookup;
                int[] valueLengths;

                ComputeBufferLength(processedValue, out bufferLength, out forwardLookup, out valueLengths);

                byte[] buffer = new byte[bufferLength];
                int bufferPosition = 0;

                Encode(processedValue, forwardLookup, valueLengths, buffer, ref bufferPosition);

                return buffer;
            }

            #endregion
        }

        #endregion

        #region ObjectDecoder

        private class ObjectDecoder : Serializer.ClassContextHolder
        {
            Encoding encoding;
            ScriptContext context;

            List<ClassDesc> classTable;
            List<object> objectTable;

            #region Constructor

            public ObjectDecoder(ScriptContext/*!*/ context, Encoding/*!*/ encoding, DTypeDesc caller)
                : base(caller)
            {
                this.encoding = encoding;
                this.context = context;
                this.classTable = new List<ClassDesc>();
                this.objectTable = new List<object>();
            }

            #endregion

            #region Decode helpers

            public static ValueTypeCode DecodeValueType(byte[] buffer, int bufferPosition)
            {
                return (ValueTypeCode)buffer[bufferPosition];
            }

            public static ClassType DecodeClassType(byte[] buffer, int bufferPosition)
            {
                return (ClassType)buffer[bufferPosition];
            }

            public static ReferencedValueType DecodeObjectType(byte[] buffer, int bufferPosition)
            {
                return (ReferencedValueType)buffer[bufferPosition];
            }

            public static int DecodeInt8(byte[] buffer, int bufferPosition)
            {
                return buffer[bufferPosition];
            }

            public static int DecodeInt16(byte[] buffer, int bufferPosition)
            {
                return (buffer[bufferPosition] << 8) | buffer[bufferPosition + 1];
            }

            public static int DecodeInt32(byte[] buffer, int bufferPosition)
            {
                return (buffer[bufferPosition] << 24) | (buffer[bufferPosition + 1] << 16) | 
                    (buffer[bufferPosition + 2] << 8) | buffer[bufferPosition + 3];
            }

            public static long DecodeInt64(byte[] buffer, int bufferPosition)
            {
                return (buffer[bufferPosition] << 56) | (buffer[bufferPosition + 1] << 48) | 
                    (buffer[bufferPosition + 2] << 40) | (buffer[bufferPosition + 3] << 32) |
                    (buffer[bufferPosition + 4] << 24) | (buffer[bufferPosition + 5] << 16) | 
                    (buffer[bufferPosition + 6] << 8) | buffer[bufferPosition + 7];
            }

            public static double DecodeFloat(byte[] buffer, int bufferPosition)
            {
                return BitConverter.Int64BitsToDouble(DecodeInt64(buffer, bufferPosition));
            }

            public static bool DecodeBoolean(byte[] buffer, int bufferPosition)
            {
                return buffer[bufferPosition] != 0;
            }

            public static string DecodeString(Encoding encoding, byte[] buffer, ref int bufferPosition)
            {
                int length = DecodeSize(buffer, ref bufferPosition);
                string ret = encoding.GetString(buffer, bufferPosition, length);
                bufferPosition += length;
                return ret;
            }

            private static int DecodeSize(byte[] buffer, ref int bufferPosition)
            {
                if (buffer[bufferPosition] < 254)
                {
                    int ret = buffer[bufferPosition];
                    bufferPosition += 1;
                    return ret;
                }
                else if (buffer[bufferPosition] == 254)
                {
                    bufferPosition += 1;
                    int ret = DecodeInt16(buffer, bufferPosition);
                    bufferPosition += 2;
                    return ret;
                }
                else
                {
                    bufferPosition += 1;
                    int ret = DecodeInt32(buffer, bufferPosition);
                    bufferPosition += 4;
                    return ret;
                }
            }

            private static int DecodeInternalReference(byte[] buffer, ref int bufferPosition)
            {
                return DecodeSize(buffer, ref bufferPosition);
            }

            #endregion

            private void BuildClassTable(byte[] buffer, ref int bufferPosition)
            {
                int classCount = DecodeSize(buffer, ref bufferPosition);

                for (int i = 0; i < classCount; i++)
                {
                    ClassDesc classDesc = new ClassDesc();

                    classDesc.ID = i;

                    classDesc.ClassType = DecodeClassType(buffer, bufferPosition);
                    bufferPosition += 1;
                    classDesc.Name = DecodeString(encoding, buffer, ref bufferPosition);

                    classTable.Add(classDesc);

                    switch (classDesc.ClassType)
                    {
                        case ClassType.Serializable:
                            continue;
                        case ClassType.Strict:
                        case ClassType.Dynamic:
                            int fieldCount = DecodeSize(buffer, ref bufferPosition);

                            for (int j = 0; j < fieldCount; j++)
                            {
                                ClassFieldDesc fieldDesc = new ClassFieldDesc();

                                fieldDesc.ValueType = DecodeValueType(buffer, bufferPosition);
                                bufferPosition += 1;
                                fieldDesc.Name = DecodeString(encoding, buffer, ref bufferPosition);

                                classDesc.Fields.Add(fieldDesc);
                            }
                            continue;
                        default:
                            Debug.Fail();
                            continue;
                    }                    
                }
            }

            private void BuildObjectTable(byte[] buffer, ref int bufferPosition)
            {
                int valueCount = DecodeSize(buffer, ref bufferPosition);
                int[] valueContinuations = new int[valueCount];
                ReferencedValueType[] objectTypes = new ReferencedValueType[valueCount];

                for (int i = 0; i < valueCount; i++)
                {
                    int size = DecodeSize(buffer, ref bufferPosition);

                    valueContinuations[i] = bufferPosition;

                    objectTypes[i] = DecodeObjectType(buffer, bufferPosition);
                    bufferPosition += 1;

                    if (objectTypes[i] == ReferencedValueType.Value)
                    {
                        //empty reference - will be filled later
                        objectTable.Add(new PhpReference());
                    }
                    else
                    {
                        //decode class id
                        int classID = DecodeInternalReference(buffer, ref bufferPosition);
                        Debug.Assert(classTable.Count > classID);
                        ClassDesc classDesc = classTable[classID];

                        //create empty object for later use
                        DObject obj = Serialization.GetUninitializedInstance(classDesc.Name, context);

                        objectTable.Add(obj);
                    }

                    bufferPosition = valueContinuations[i] + size;
                }

                for (int i = 0; i < valueCount; i++)
                {
                    //move to the right position
                    bufferPosition = valueContinuations[i];

                    //skip value type
                    bufferPosition += 1;

                    if (objectTypes[i] == ReferencedValueType.Value)
                    {
                        //decode referenced value
                        object value = DecodeValue(ValueTypeCode.Dynamic, buffer, ref bufferPosition);
                        ((PhpReference)objectTable[i]).Value = value;
                    }
                    else
                    {
                        //decode class id
                        int classID = DecodeInternalReference(buffer, ref bufferPosition);
                        Debug.Assert(classTable.Count > classID);
                        ClassDesc classDesc = classTable[classID];

                        DObject obj = ((DObject)objectTable[i]);

                        if (obj == null)
                        {
                            throw new SerializationException(LibResources.GetString("class_instantiation_failed",
                                classDesc.Name));
                        }

                        switch (classDesc.ClassType)
                        {
                            case ClassType.Serializable:
                                // check whether the instance is PHP5.1 Serializable
                                if (obj.RealObject is Library.SPL.Serializable)
                                {
                                    throw new SerializationException(LibResources.GetString("class_has_no_unserializer",
                                        classDesc.Name));
                                }

                                //read serialized data
                                int dataSize = DecodeSize(buffer, ref bufferPosition);
                                byte[] data = new byte[dataSize];
                                Buffer.BlockCopy(buffer, bufferPosition, data, 0, dataSize);
                                bufferPosition += dataSize;

                                // pass the serialized data to unserialize
                                context.Stack.AddFrame(new PhpBytes(data));
                                obj.InvokeMethod("unserialize", null, context);

                                objectTable.Add(obj);
                                break;
                            case ClassType.Strict:
                            case ClassType.Dynamic:
                                int propertyCount = DecodeSize(buffer, ref bufferPosition);

                                for (int j = 0; j < propertyCount; j++)
                                {
                                    int fieldNumber = DecodeInternalReference(buffer, ref bufferPosition);
                                    ClassFieldDesc fieldDesc = classDesc.Fields[fieldNumber];
                                    object propertyValue = DecodeValue(fieldDesc.ValueType, buffer, ref bufferPosition);

                                    Serialization.SetProperty(obj, fieldDesc.Name, propertyValue, context);
                                }

                                obj.Wakeup(ClassContext, context);
                                break;
                            default:
                                Debug.Fail();
                                break;
                        }
                    }
                }
            }

            private object BuildReferenceValue(int reference)
            {
                return objectTable[reference];
            }
            
            private object DecodeValue(ValueTypeCode typeCode, byte[] buffer, ref int bufferPosition)
            {
                if (typeCode == ValueTypeCode.Dynamic)
                {
                    typeCode = DecodeValueType(buffer, bufferPosition);
                    bufferPosition += 1;
                }

                switch (typeCode)
                {
                    case ValueTypeCode.Dynamic:
                        Debug.Fail();
                        return null;
                    case ValueTypeCode.Null:
                        return null;
                    case ValueTypeCode.Int8:
                        int i8 = DecodeInt8(buffer, bufferPosition);
                        bufferPosition += 1;
                        return i8;
                    case ValueTypeCode.Int16:
                        int i16 = DecodeInt16(buffer, bufferPosition);
                        bufferPosition += 2;
                        return i16;
                    case ValueTypeCode.Int32:
                        int i32 = DecodeInt32(buffer, bufferPosition);
                        bufferPosition += 4;
                        return i32;
                    case ValueTypeCode.Int64:
                        long i64 = DecodeInt64(buffer, bufferPosition);
                        bufferPosition += 8;
                        return i64;
                    case ValueTypeCode.Float:
                        double d = DecodeFloat(buffer, bufferPosition);
                        bufferPosition += 8;
                        return d;
                    case ValueTypeCode.Boolean:
                        bool b = DecodeBoolean(buffer, bufferPosition);
                        bufferPosition += 1;
                        return b;
                    case ValueTypeCode.String:
                        return DecodeString(encoding, buffer, ref bufferPosition);
                    case ValueTypeCode.Array:
                        PhpArray array = new PhpArray();

                        ValueTypeCode indexType = DecodeValueType(buffer, bufferPosition);
                        bufferPosition += 1;
                        ValueTypeCode valueType = DecodeValueType(buffer, bufferPosition);
                        bufferPosition += 1;
                        int length = DecodeSize(buffer, ref bufferPosition);

                        for (int i = 0; i < length; i++)
                        {
                            object index = DecodeValue(indexType, buffer, ref bufferPosition);
                            object value = DecodeValue(valueType, buffer, ref bufferPosition);

                            array.Add(index, value);
                        }

                        return array;
                    case ValueTypeCode.Binary:
                        int dataSize = DecodeSize(buffer, ref bufferPosition);
                        byte[] data = new byte[dataSize];
                        Buffer.BlockCopy(buffer, bufferPosition, data, 0, dataSize);
                        bufferPosition += dataSize;
                        return new PhpBytes(data);
                    case ValueTypeCode.Reference8:
                        int refsm = DecodeInt8(buffer, bufferPosition);
                        bufferPosition += 1;
                        return objectTable[refsm];
                    case ValueTypeCode.Reference16:
                        int refmed = DecodeInt16(buffer, bufferPosition);
                        bufferPosition += 2;
                        return objectTable[refmed];
                    case ValueTypeCode.Reference32:
                        int refl = DecodeInt32(buffer, bufferPosition);
                        bufferPosition += 4;
                        return objectTable[refl];
                    default:
                        Debug.Fail();
                        return null;
                }
            }

            public object Deserialize(byte[] buffer)
            {
                int bufferPosition = 0;

                int forwardLookup = DecodeSize(buffer, ref bufferPosition);

                if (forwardLookup > 0)
                {
                    BuildClassTable(buffer, ref bufferPosition);
                    BuildObjectTable(buffer, ref bufferPosition);
                }

                return DecodeValue(ValueTypeCode.Dynamic, buffer, ref bufferPosition);
            }
        }

        #endregion

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
        //public PhalangerFormatter()
        //{
        //    this.encoding = new ASCIIEncoding();
        //}

		/// <summary>
		/// Creates a new <see cref="PhpFormatter"/> with a given <see cref="Encoding"/> and
		/// default <see cref="Context"/>.
		/// </summary>
		/// <param name="encoding">The encoding to be used when writing and reading the serialization stream.</param>
        /// <param name="caller">The DTypeDesc of the caller class context or UnknownTypeDesc is class context is not known yet.</param>
        public PhalangerFormatter(Encoding encoding, DTypeDesc caller)
		{
            this.caller = caller;

			// no UTF8 BOM!
			if (encoding is UTF8Encoding) this.encoding = new UTF8Encoding(false);
			else
			{
				this.encoding = (encoding == null ? new ASCIIEncoding() : encoding);
			}
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

            ObjectEncoder object_encoder = new ObjectEncoder(ScriptContext.CurrentContext, encoding, caller);

			try
			{
                byte[] buffer = object_encoder.Serialize(graph);
                serializationStream.Write(buffer, 0, buffer.Length);
			}
			finally
			{
                serializationStream.Flush();
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


            long length = serializationStream.Length - serializationStream.Position;
            byte[] buffer = new byte[length];
            serializationStream.Read(buffer, 0, (int)length);

            ObjectDecoder object_reader = new ObjectDecoder(ScriptContext.CurrentContext, encoding, caller);

            return object_reader.Deserialize(buffer);
		}

		#endregion
    }
}
