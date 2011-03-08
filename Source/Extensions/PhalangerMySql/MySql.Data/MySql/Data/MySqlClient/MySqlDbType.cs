namespace MySql.Data.MySqlClient
{
    using System;

    public enum MySqlDbType
    {
        Binary = 600,
        Bit = 0x10,
        Blob = 0xfc,
        Byte = 1,
        Date = 10,
        [Obsolete("The Datetime enum value is obsolete.  Please use DateTime.")]
        Datetime = 12,
        DateTime = 12,
        Decimal = 0,
        Double = 5,
        Enum = 0xf7,
        Float = 4,
        Geometry = 0xff,
        Guid = 800,
        Int16 = 2,
        Int24 = 9,
        Int32 = 3,
        Int64 = 8,
        LongBlob = 0xfb,
        LongText = 0x2ef,
        MediumBlob = 250,
        MediumText = 750,
        Newdate = 14,
        NewDecimal = 0xf6,
        Set = 0xf8,
        String = 0xfe,
        Text = 0x2f0,
        Time = 11,
        Timestamp = 7,
        TinyBlob = 0xf9,
        TinyText = 0x2ed,
        UByte = 0x1f5,
        UInt16 = 0x1f6,
        UInt24 = 0x1fd,
        UInt32 = 0x1f7,
        UInt64 = 0x1fc,
        VarBinary = 0x259,
        VarChar = 0xfd,
        VarString = 15,
        Year = 13
    }
}

