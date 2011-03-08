namespace MySql.Data.MySqlClient
{
    using MySql.Data.Common;
    using MySql.Data.Types;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text;
    using System.Text.RegularExpressions;

    internal class MySqlField
    {
        protected bool binaryOk;
        public string CatalogName;
        internal ColumnFlags colFlags;
        public int ColumnLength;
        public string ColumnName;
        protected DBVersion connVersion;
        public string DatabaseName;
        protected Driver driver;
        public System.Text.Encoding Encoding;
        protected int charSetIndex;
        public int maxLength;
        protected MySqlDbType mySqlDbType;
        public string OriginalColumnName;
        protected byte precision;
        public string RealTableName;
        protected byte scale;
        public string TableName;
        protected List<System.Type> typeConversions = new List<System.Type>();

        public MySqlField(Driver driver)
        {
            this.driver = driver;
            this.connVersion = driver.Version;
            this.maxLength = 1;
            this.binaryOk = true;
        }

        public void AddTypeConversion(System.Type t)
        {
            if (!this.TypeConversions.Contains(t))
            {
                this.TypeConversions.Add(t);
            }
        }

        public static IMySqlValue GetIMySqlValue(MySqlDbType type)
        {
            MySqlDbType type2 = type;
            if (type2 <= MySqlDbType.UInt24)
            {
                switch (type2)
                {
                    case MySqlDbType.Decimal:
                    case MySqlDbType.NewDecimal:
                        return new MySqlDecimal();

                    case MySqlDbType.Byte:
                        return new MySqlByte();

                    case MySqlDbType.Int16:
                        return new MySqlInt16();

                    case MySqlDbType.Int32:
                    case MySqlDbType.Int24:
                    case MySqlDbType.Year:
                        return new MySqlInt32(type, true);

                    case MySqlDbType.Float:
                        return new MySqlSingle();

                    case MySqlDbType.Double:
                        return new MySqlDouble();

                    case (MySqlDbType.Float | MySqlDbType.Int16):
                    case MySqlDbType.VarString:
                    case MySqlDbType.Enum:
                    case MySqlDbType.Set:
                    case MySqlDbType.VarChar:
                    case MySqlDbType.String:
                        goto Label_01D4;

                    case MySqlDbType.Timestamp:
                    case MySqlDbType.Date:
                    case MySqlDbType.DateTime:
                    case MySqlDbType.Newdate:
                        return new MySqlDateTime(type, true);

                    case MySqlDbType.Int64:
                        return new MySqlInt64();

                    case MySqlDbType.Time:
                        return new MySqlTimeSpan();

                    case MySqlDbType.Bit:
                        return new MySqlBit();

                    case MySqlDbType.TinyBlob:
                    case MySqlDbType.MediumBlob:
                    case MySqlDbType.LongBlob:
                    case MySqlDbType.Blob:
                    case MySqlDbType.Geometry:
                        goto Label_01E1;

                    case MySqlDbType.UByte:
                        return new MySqlUByte();

                    case MySqlDbType.UInt16:
                        return new MySqlUInt16();

                    case MySqlDbType.UInt32:
                    case MySqlDbType.UInt24:
                        return new MySqlUInt32(type, true);

                    case ((MySqlDbType) 0x1f8):
                    case ((MySqlDbType) 0x1f9):
                    case ((MySqlDbType) 0x1fa):
                    case ((MySqlDbType) 0x1fb):
                        goto Label_01FE;

                    case MySqlDbType.UInt64:
                        return new MySqlUInt64();
                }
            }
            else
            {
                switch (type2)
                {
                    case MySqlDbType.Binary:
                    case MySqlDbType.VarBinary:
                        goto Label_01E1;

                    case MySqlDbType.TinyText:
                    case MySqlDbType.MediumText:
                    case MySqlDbType.LongText:
                    case MySqlDbType.Text:
                        goto Label_01D4;

                    case MySqlDbType.Guid:
                        return new MySqlGuid();
                }
            }
            goto Label_01FE;
        Label_01D4:
            return new MySqlString(type, true);
        Label_01E1:
            return new MySqlBinary(type, true);
        Label_01FE:
            throw new MySqlException("Unknown data type");
        }

        public IMySqlValue GetValueObject()
        {
            IMySqlValue iMySqlValue = GetIMySqlValue(this.Type);
            if (((iMySqlValue is MySqlByte) && (this.ColumnLength == 1)) && this.driver.Settings.TreatTinyAsBoolean)
            {
                MySqlByte num = (MySqlByte) iMySqlValue;
                num.TreatAsBoolean = true;
                return num;
            }
            if (iMySqlValue is MySqlGuid)
            {
                MySqlGuid guid = (MySqlGuid) iMySqlValue;
                guid.OldGuids = this.driver.Settings.OldGuids;
                iMySqlValue = guid;
            }
            return iMySqlValue;
        }

        private void CheckForExceptions()
        {
            string str = string.Empty;
            if (this.OriginalColumnName != null)
            {
                str = this.OriginalColumnName.ToUpper(CultureInfo.InvariantCulture);
            }
            if (str.StartsWith("CHAR("))
            {
                this.binaryOk = false;
            }
        }

        private void SetFieldEncoding()
        {
            Hashtable characterSets = this.driver.CharacterSets;
            DBVersion version = this.driver.Version;
            if (((characterSets != null) && (this.CharacterSetIndex != -1)) && (characterSets[this.CharacterSetIndex] != null))
            {
                CharacterSet characterSet = CharSetMap.GetCharacterSet(version, (string) characterSets[this.CharacterSetIndex]);
                if ((characterSet.name.ToLower(CultureInfo.InvariantCulture) == "utf-8") && (version.Major >= 6))
                {
                    this.MaxLength = 4;
                }
                else
                {
                    this.MaxLength = characterSet.byteCount;
                }
                this.Encoding = CharSetMap.GetEncoding(version, (string) characterSets[this.CharacterSetIndex]);
            }
        }

        public void SetTypeAndFlags(MySqlDbType type, ColumnFlags flags)
        {
            this.colFlags = flags;
            this.mySqlDbType = type;
            if ((string.IsNullOrEmpty(this.TableName) && string.IsNullOrEmpty(this.RealTableName)) && (this.IsBinary && this.driver.Settings.FunctionsReturnString))
            {
                this.CharacterSetIndex = this.driver.ConnectionCharSetIndex;
            }
            if (this.IsUnsigned)
            {
                switch (type)
                {
                    case MySqlDbType.Byte:
                        this.mySqlDbType = MySqlDbType.UByte;
                        return;

                    case MySqlDbType.Int16:
                        this.mySqlDbType = MySqlDbType.UInt16;
                        return;

                    case MySqlDbType.Int32:
                        this.mySqlDbType = MySqlDbType.UInt32;
                        return;

                    case MySqlDbType.Int64:
                        this.mySqlDbType = MySqlDbType.UInt64;
                        return;

                    case MySqlDbType.Int24:
                        this.mySqlDbType = MySqlDbType.UInt24;
                        return;
                }
            }
            if (this.IsBlob)
            {
                if (this.IsBinary && this.driver.Settings.TreatBlobsAsUTF8)
                {
                    bool flag = false;
                    Regex regex = this.driver.Settings.GetBlobAsUTF8IncludeRegex();
                    Regex regex2 = this.driver.Settings.GetBlobAsUTF8ExcludeRegex();
                    if ((regex != null) && regex.IsMatch(this.ColumnName))
                    {
                        flag = true;
                    }
                    else if (((regex == null) && (regex2 != null)) && !regex2.IsMatch(this.ColumnName))
                    {
                        flag = true;
                    }
                    if (flag)
                    {
                        this.binaryOk = false;
                        this.Encoding = System.Text.Encoding.GetEncoding("UTF-8");
                        this.charSetIndex = -1;
                        this.maxLength = 4;
                    }
                }
                if (!this.IsBinary)
                {
                    if (type == MySqlDbType.TinyBlob)
                    {
                        this.mySqlDbType = MySqlDbType.TinyText;
                    }
                    else if (type == MySqlDbType.MediumBlob)
                    {
                        this.mySqlDbType = MySqlDbType.MediumText;
                    }
                    else if (type == MySqlDbType.Blob)
                    {
                        this.mySqlDbType = MySqlDbType.Text;
                    }
                    else if (type == MySqlDbType.LongBlob)
                    {
                        this.mySqlDbType = MySqlDbType.LongText;
                    }
                }
            }
            if (this.driver.Settings.RespectBinaryFlags)
            {
                this.CheckForExceptions();
            }
            if (((this.Type == MySqlDbType.String) && (this.CharacterLength == 0x24)) && !this.driver.Settings.OldGuids)
            {
                this.mySqlDbType = MySqlDbType.Guid;
            }
            if (this.IsBinary)
            {
                if (this.driver.Settings.RespectBinaryFlags)
                {
                    if (type == MySqlDbType.String)
                    {
                        this.mySqlDbType = MySqlDbType.Binary;
                    }
                    else if ((type == MySqlDbType.VarChar) || (type == MySqlDbType.VarString))
                    {
                        this.mySqlDbType = MySqlDbType.VarBinary;
                    }
                }
                if (this.CharacterSetIndex == 0x3f)
                {
                    this.CharacterSetIndex = this.driver.ConnectionCharSetIndex;
                }
                if (((this.Type == MySqlDbType.Binary) && (this.ColumnLength == 0x10)) && this.driver.Settings.OldGuids)
                {
                    this.mySqlDbType = MySqlDbType.Guid;
                }
            }
        }

        public bool AllowsNull
        {
            get
            {
                return ((this.colFlags & ColumnFlags.NOT_NULL) == ((ColumnFlags) 0));
            }
        }

        public ColumnFlags Flags
        {
            get
            {
                return this.colFlags;
            }
        }

        public int CharacterLength
        {
            get
            {
                return (this.ColumnLength / this.MaxLength);
            }
        }

        public int CharacterSetIndex
        {
            get
            {
                return this.charSetIndex;
            }
            set
            {
                this.charSetIndex = value;
                this.SetFieldEncoding();
            }
        }

        public bool IsAutoIncrement
        {
            get
            {
                return ((this.colFlags & ColumnFlags.AUTO_INCREMENT) > ((ColumnFlags) 0));
            }
        }

        public bool IsBinary
        {
            get
            {
                return (this.binaryOk && (this.CharacterSetIndex == 0x3f));
            }
        }

        public bool IsBlob
        {
            get
            {
                if (((this.mySqlDbType < MySqlDbType.TinyBlob) || (this.mySqlDbType > MySqlDbType.Blob)) && ((this.mySqlDbType < MySqlDbType.TinyText) || (this.mySqlDbType > MySqlDbType.Text)))
                {
                    return ((this.colFlags & ColumnFlags.BLOB) > ((ColumnFlags) 0));
                }
                return true;
            }
        }

        public bool IsNumeric
        {
            get
            {
                return ((this.colFlags & ColumnFlags.NUMBER) > ((ColumnFlags) 0));
            }
        }

        public bool IsPrimaryKey
        {
            get
            {
                return ((this.colFlags & ColumnFlags.PRIMARY_KEY) > ((ColumnFlags) 0));
            }
        }

        public bool IsTextField
        {
            get
            {
                return (((this.Type == MySqlDbType.VarString) || (this.Type == MySqlDbType.VarChar)) || (this.IsBlob && !this.IsBinary));
            }
        }

        public bool IsUnique
        {
            get
            {
                return ((this.colFlags & ColumnFlags.UNIQUE_KEY) > ((ColumnFlags) 0));
            }
        }

        public bool IsUnsigned
        {
            get
            {
                return ((this.colFlags & ColumnFlags.UNSIGNED) > ((ColumnFlags) 0));
            }
        }

        public int MaxLength
        {
            get
            {
                return this.maxLength;
            }
            set
            {
                this.maxLength = value;
            }
        }

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

        public MySqlDbType Type
        {
            get
            {
                return this.mySqlDbType;
            }
        }

        public List<System.Type> TypeConversions
        {
            get
            {
                return this.typeConversions;
            }
        }
    }
}

