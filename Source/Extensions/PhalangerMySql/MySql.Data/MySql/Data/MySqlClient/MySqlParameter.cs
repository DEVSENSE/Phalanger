namespace MySql.Data.MySqlClient
{
    using MySql.Data.Types;
    using System;
    using System.Collections;
    using System.ComponentModel;
    using System.Data;
    using System.Data.Common;
    using System.Text;

    [TypeConverter(typeof(MySqlParameterConverter))]
    public sealed class MySqlParameter : DbParameter, IDbDataParameter, IDataParameter, ICloneable
    {
        private MySqlParameterCollection collection;
        private System.Data.DbType dbType;
        private ParameterDirection direction;
        private System.Text.Encoding encoding;
        private bool inferType;
        private bool isNullable;
        private MySql.Data.MySqlClient.MySqlDbType mySqlDbType;
        private string paramName;
        private object paramValue;
        private IList possibleValues;
        private byte precision;
        private byte scale;
        private int size;
        private string sourceColumn;
        private bool sourceColumnNullMapping;
        private DataRowVersion sourceVersion;
        private const int UNSIGNED_MASK = 0x8000;
        private IMySqlValue valueObject;

        public MySqlParameter()
        {
            this.direction = ParameterDirection.Input;
            this.sourceVersion = DataRowVersion.Current;
            this.inferType = true;
        }

        public MySqlParameter(string parameterName, MySql.Data.MySqlClient.MySqlDbType dbType) : this(parameterName, null)
        {
            this.MySqlDbType = dbType;
        }

        public MySqlParameter(string parameterName, object value) : this()
        {
            this.ParameterName = parameterName;
            this.Value = value;
        }

        public MySqlParameter(string parameterName, MySql.Data.MySqlClient.MySqlDbType dbType, int size) : this(parameterName, dbType)
        {
            this.size = size;
        }

        public MySqlParameter(string parameterName, MySql.Data.MySqlClient.MySqlDbType dbType, int size, string sourceColumn) : this(parameterName, dbType)
        {
            this.size = size;
            this.direction = ParameterDirection.Input;
            this.sourceColumn = sourceColumn;
            this.sourceVersion = DataRowVersion.Current;
        }

        internal MySqlParameter(string name, MySql.Data.MySqlClient.MySqlDbType type, ParameterDirection dir, string col, DataRowVersion ver, object val) : this(name, type)
        {
            this.direction = dir;
            this.sourceColumn = col;
            this.sourceVersion = ver;
            this.Value = val;
        }

        public MySqlParameter(string parameterName, MySql.Data.MySqlClient.MySqlDbType dbType, int size, ParameterDirection direction, bool isNullable, byte precision, byte scale, string sourceColumn, DataRowVersion sourceVersion, object value) : this(parameterName, dbType, size, sourceColumn)
        {
            this.direction = direction;
            this.sourceVersion = sourceVersion;
            this.Value = value;
        }

        public MySqlParameter Clone()
        {
            MySqlParameter parameter = new MySqlParameter(this.paramName, this.mySqlDbType, this.direction, this.sourceColumn, this.sourceVersion, this.paramValue);
            parameter.inferType = this.inferType;
            return parameter;
        }

        internal long EstimatedSize()
        {
            if ((this.Value == null) || (this.Value == DBNull.Value))
            {
                return 4L;
            }
            if (this.Value is byte[])
            {
                return (long) (this.Value as byte[]).Length;
            }
            if (this.Value is string)
            {
                return (long) ((this.Value as string).Length * 4);
            }
            if ((this.Value is decimal) || (this.Value is float))
            {
                return 0x40L;
            }
            return 0x20L;
        }

        internal int GetPSType()
        {
            switch (this.mySqlDbType)
            {
                case MySql.Data.MySqlClient.MySqlDbType.UByte:
                    return 0x8001;

                case MySql.Data.MySqlClient.MySqlDbType.UInt16:
                    return 0x8002;

                case MySql.Data.MySqlClient.MySqlDbType.UInt32:
                    return 0x8003;

                case MySql.Data.MySqlClient.MySqlDbType.UInt64:
                    return 0x8008;

                case MySql.Data.MySqlClient.MySqlDbType.UInt24:
                    return 0x8003;

                case MySql.Data.MySqlClient.MySqlDbType.Bit:
                    return 0x8008;
            }
            return (int) this.mySqlDbType;
        }

        public override void ResetDbType()
        {
            this.inferType = true;
        }

        internal void Serialize(MySqlPacket packet, bool binary, MySqlConnectionStringBuilder settings)
        {
            if (!binary && ((this.paramValue == null) || (this.paramValue == DBNull.Value)))
            {
                packet.WriteStringNoNull("NULL");
            }
            else
            {
                if (this.ValueObject.MySqlDbType == MySql.Data.MySqlClient.MySqlDbType.Guid)
                {
                    MySqlGuid valueObject = (MySqlGuid) this.ValueObject;
                    valueObject.OldGuids = settings.OldGuids;
                    this.valueObject = valueObject;
                }
                this.ValueObject.WriteValue(packet, binary, this.paramValue, this.size);
            }
        }

        private void SetDbType(System.Data.DbType db_type)
        {
            this.dbType = db_type;
            switch (this.dbType)
            {
                case System.Data.DbType.AnsiString:
                case System.Data.DbType.String:
                    this.mySqlDbType = MySql.Data.MySqlClient.MySqlDbType.VarChar;
                    break;

                case System.Data.DbType.Byte:
                case System.Data.DbType.Boolean:
                    this.mySqlDbType = MySql.Data.MySqlClient.MySqlDbType.UByte;
                    break;

                case System.Data.DbType.Currency:
                case System.Data.DbType.Decimal:
                    this.mySqlDbType = MySql.Data.MySqlClient.MySqlDbType.Decimal;
                    break;

                case System.Data.DbType.Date:
                    this.mySqlDbType = MySql.Data.MySqlClient.MySqlDbType.Date;
                    break;

                case System.Data.DbType.DateTime:
                    this.mySqlDbType = MySql.Data.MySqlClient.MySqlDbType.DateTime;
                    break;

                case System.Data.DbType.Double:
                    this.mySqlDbType = MySql.Data.MySqlClient.MySqlDbType.Double;
                    break;

                case System.Data.DbType.Guid:
                    this.mySqlDbType = MySql.Data.MySqlClient.MySqlDbType.Guid;
                    break;

                case System.Data.DbType.Int16:
                    this.mySqlDbType = MySql.Data.MySqlClient.MySqlDbType.Int16;
                    break;

                case System.Data.DbType.Int32:
                    this.mySqlDbType = MySql.Data.MySqlClient.MySqlDbType.Int32;
                    break;

                case System.Data.DbType.Int64:
                    this.mySqlDbType = MySql.Data.MySqlClient.MySqlDbType.Int64;
                    break;

                case System.Data.DbType.SByte:
                    this.mySqlDbType = MySql.Data.MySqlClient.MySqlDbType.Byte;
                    break;

                case System.Data.DbType.Single:
                    this.mySqlDbType = MySql.Data.MySqlClient.MySqlDbType.Float;
                    break;

                case System.Data.DbType.Time:
                    this.mySqlDbType = MySql.Data.MySqlClient.MySqlDbType.Time;
                    break;

                case System.Data.DbType.UInt16:
                    this.mySqlDbType = MySql.Data.MySqlClient.MySqlDbType.UInt16;
                    break;

                case System.Data.DbType.UInt32:
                    this.mySqlDbType = MySql.Data.MySqlClient.MySqlDbType.UInt32;
                    break;

                case System.Data.DbType.UInt64:
                    this.mySqlDbType = MySql.Data.MySqlClient.MySqlDbType.UInt64;
                    break;

                case System.Data.DbType.AnsiStringFixedLength:
                case System.Data.DbType.StringFixedLength:
                    this.mySqlDbType = MySql.Data.MySqlClient.MySqlDbType.String;
                    break;

                default:
                    this.mySqlDbType = MySql.Data.MySqlClient.MySqlDbType.Blob;
                    break;
            }
            this.valueObject = MySqlField.GetIMySqlValue(this.mySqlDbType);
        }

        private void SetMySqlDbType(MySql.Data.MySqlClient.MySqlDbType mysql_dbtype)
        {
            this.mySqlDbType = mysql_dbtype;
            this.valueObject = MySqlField.GetIMySqlValue(this.mySqlDbType);
            switch (this.mySqlDbType)
            {
                case MySql.Data.MySqlClient.MySqlDbType.UByte:
                    this.dbType = System.Data.DbType.Byte;
                    return;

                case MySql.Data.MySqlClient.MySqlDbType.UInt16:
                    this.dbType = System.Data.DbType.UInt16;
                    return;

                case MySql.Data.MySqlClient.MySqlDbType.UInt32:
                case MySql.Data.MySqlClient.MySqlDbType.UInt24:
                    this.dbType = System.Data.DbType.UInt32;
                    return;

                case ((MySql.Data.MySqlClient.MySqlDbType) 0x1f8):
                case ((MySql.Data.MySqlClient.MySqlDbType) 0x1f9):
                case ((MySql.Data.MySqlClient.MySqlDbType) 0x1fa):
                case ((MySql.Data.MySqlClient.MySqlDbType) 0x1fb):
                case (MySql.Data.MySqlClient.MySqlDbType.Float | MySql.Data.MySqlClient.MySqlDbType.Int16):
                case MySql.Data.MySqlClient.MySqlDbType.VarString:
                    break;

                case MySql.Data.MySqlClient.MySqlDbType.UInt64:
                    this.dbType = System.Data.DbType.UInt64;
                    return;

                case MySql.Data.MySqlClient.MySqlDbType.Guid:
                    this.dbType = System.Data.DbType.Guid;
                    break;

                case MySql.Data.MySqlClient.MySqlDbType.Decimal:
                    this.dbType = System.Data.DbType.Decimal;
                    return;

                case MySql.Data.MySqlClient.MySqlDbType.Byte:
                    this.dbType = System.Data.DbType.SByte;
                    return;

                case MySql.Data.MySqlClient.MySqlDbType.Int16:
                    this.dbType = System.Data.DbType.Int16;
                    return;

                case MySql.Data.MySqlClient.MySqlDbType.Int32:
                case MySql.Data.MySqlClient.MySqlDbType.Int24:
                    this.dbType = System.Data.DbType.Int32;
                    return;

                case MySql.Data.MySqlClient.MySqlDbType.Float:
                    this.dbType = System.Data.DbType.Single;
                    return;

                case MySql.Data.MySqlClient.MySqlDbType.Double:
                    this.dbType = System.Data.DbType.Double;
                    return;

                case MySql.Data.MySqlClient.MySqlDbType.Timestamp:
                case MySql.Data.MySqlClient.MySqlDbType.DateTime:
                    this.dbType = System.Data.DbType.DateTime;
                    return;

                case MySql.Data.MySqlClient.MySqlDbType.Int64:
                    this.dbType = System.Data.DbType.Int64;
                    return;

                case MySql.Data.MySqlClient.MySqlDbType.Date:
                case MySql.Data.MySqlClient.MySqlDbType.Year:
                case MySql.Data.MySqlClient.MySqlDbType.Newdate:
                    this.dbType = System.Data.DbType.Date;
                    return;

                case MySql.Data.MySqlClient.MySqlDbType.Time:
                    this.dbType = System.Data.DbType.Time;
                    return;

                case MySql.Data.MySqlClient.MySqlDbType.Bit:
                    this.dbType = System.Data.DbType.UInt64;
                    return;

                case MySql.Data.MySqlClient.MySqlDbType.Enum:
                case MySql.Data.MySqlClient.MySqlDbType.Set:
                case MySql.Data.MySqlClient.MySqlDbType.VarChar:
                    this.dbType = System.Data.DbType.String;
                    return;

                case MySql.Data.MySqlClient.MySqlDbType.TinyBlob:
                case MySql.Data.MySqlClient.MySqlDbType.MediumBlob:
                case MySql.Data.MySqlClient.MySqlDbType.LongBlob:
                case MySql.Data.MySqlClient.MySqlDbType.Blob:
                    this.dbType = System.Data.DbType.Object;
                    return;

                case MySql.Data.MySqlClient.MySqlDbType.String:
                    this.dbType = System.Data.DbType.StringFixedLength;
                    return;

                default:
                    return;
            }
        }

        private void SetTypeFromValue()
        {
            if ((this.paramValue != null) && (this.paramValue != DBNull.Value))
            {
                if (this.paramValue is Guid)
                {
                    this.DbType = System.Data.DbType.Guid;
                }
                else if (this.paramValue is TimeSpan)
                {
                    this.DbType = System.Data.DbType.Time;
                }
                else if (this.paramValue is bool)
                {
                    this.DbType = System.Data.DbType.Byte;
                }
                else
                {
                    switch (Type.GetTypeCode(this.paramValue.GetType()))
                    {
                        case TypeCode.SByte:
                            this.DbType = System.Data.DbType.SByte;
                            return;

                        case TypeCode.Byte:
                            this.DbType = System.Data.DbType.Byte;
                            return;

                        case TypeCode.Int16:
                            this.DbType = System.Data.DbType.Int16;
                            return;

                        case TypeCode.UInt16:
                            this.DbType = System.Data.DbType.UInt16;
                            return;

                        case TypeCode.Int32:
                            this.DbType = System.Data.DbType.Int32;
                            return;

                        case TypeCode.UInt32:
                            this.DbType = System.Data.DbType.UInt32;
                            return;

                        case TypeCode.Int64:
                            this.DbType = System.Data.DbType.Int64;
                            return;

                        case TypeCode.UInt64:
                            this.DbType = System.Data.DbType.UInt64;
                            return;

                        case TypeCode.Single:
                            this.DbType = System.Data.DbType.Single;
                            return;

                        case TypeCode.Double:
                            this.DbType = System.Data.DbType.Double;
                            return;

                        case TypeCode.Decimal:
                            this.DbType = System.Data.DbType.Decimal;
                            return;

                        case TypeCode.DateTime:
                            this.DbType = System.Data.DbType.DateTime;
                            return;

                        case TypeCode.String:
                            this.DbType = System.Data.DbType.String;
                            return;
                    }
                    this.DbType = System.Data.DbType.Object;
                }
            }
        }

        object ICloneable.Clone()
        {
            return this.Clone();
        }

        public override string ToString()
        {
            return this.paramName;
        }

        internal MySqlParameterCollection Collection
        {
            get
            {
                return this.collection;
            }
            set
            {
                this.collection = value;
            }
        }

        public override System.Data.DbType DbType
        {
            get
            {
                return this.dbType;
            }
            set
            {
                this.SetDbType(value);
                this.inferType = false;
            }
        }

        [Category("Data")]
        public override ParameterDirection Direction
        {
            get
            {
                return this.direction;
            }
            set
            {
                this.direction = value;
            }
        }

        internal System.Text.Encoding Encoding
        {
            get
            {
                return this.encoding;
            }
            set
            {
                this.encoding = value;
            }
        }

        [Browsable(false)]
        public override bool IsNullable
        {
            get
            {
                return this.isNullable;
            }
            set
            {
                this.isNullable = value;
            }
        }

        [Category("Data"), DbProviderSpecificTypeProperty(true)]
        public MySql.Data.MySqlClient.MySqlDbType MySqlDbType
        {
            get
            {
                return this.mySqlDbType;
            }
            set
            {
                this.SetMySqlDbType(value);
                this.inferType = false;
            }
        }

        [Category("Misc")]
        public override string ParameterName
        {
            get
            {
                return this.paramName;
            }
            set
            {
                if (this.collection != null)
                {
                    this.collection.ParameterNameChanged(this, this.paramName, value);
                }
                this.paramName = value;
            }
        }

        public IList PossibleValues
        {
            get
            {
                return this.possibleValues;
            }
            internal set
            {
                this.possibleValues = value;
            }
        }

        [Category("Data")]
        public byte Precision
        {
            get
            {
                return this.precision;
            }
            set
            {
                this.precision = value;
            }
        }

        [Category("Data")]
        public byte Scale
        {
            get
            {
                return this.scale;
            }
            set
            {
                this.scale = value;
            }
        }

        [Category("Data")]
        public override int Size
        {
            get
            {
                return this.size;
            }
            set
            {
                this.size = value;
            }
        }

        [Category("Data")]
        public override string SourceColumn
        {
            get
            {
                return this.sourceColumn;
            }
            set
            {
                this.sourceColumn = value;
            }
        }

        public override bool SourceColumnNullMapping
        {
            get
            {
                return this.sourceColumnNullMapping;
            }
            set
            {
                this.sourceColumnNullMapping = value;
            }
        }

        [Category("Data")]
        public override DataRowVersion SourceVersion
        {
            get
            {
                return this.sourceVersion;
            }
            set
            {
                this.sourceVersion = value;
            }
        }

        internal bool TypeHasBeenSet
        {
            get
            {
                return !this.inferType;
            }
        }

        [Category("Data"), TypeConverter(typeof(StringConverter))]
        public override object Value
        {
            get
            {
                return this.paramValue;
            }
            set
            {
                this.paramValue = value;
                byte[] buffer = value as byte[];
                string str = value as string;
                if (buffer != null)
                {
                    this.size = buffer.Length;
                }
                else if (str != null)
                {
                    this.size = str.Length;
                }
                if (this.inferType)
                {
                    this.SetTypeFromValue();
                }
            }
        }

        private IMySqlValue ValueObject
        {
            get
            {
                return this.valueObject;
            }
        }
    }
}

