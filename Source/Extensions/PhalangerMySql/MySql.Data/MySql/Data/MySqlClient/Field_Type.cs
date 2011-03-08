namespace MySql.Data.MySqlClient
{
    using System;

    internal enum Field_Type : byte
    {
        BLOB = 0xfc,
        BYTE = 1,
        DATE = 10,
        DATETIME = 12,
        DECIMAL = 0,
        DOUBLE = 5,
        ENUM = 0xf7,
        FLOAT = 4,
        INT24 = 9,
        LONG = 3,
        LONG_BLOB = 0xfb,
        LONGLONG = 8,
        MEDIUM_BLOB = 250,
        NEWDATE = 14,
        NULL = 6,
        SET = 0xf8,
        SHORT = 2,
        STRING = 0xfe,
        TIME = 11,
        TIMESTAMP = 7,
        TINY_BLOB = 0xf9,
        VAR_STRING = 0xfd,
        YEAR = 13
    }
}

