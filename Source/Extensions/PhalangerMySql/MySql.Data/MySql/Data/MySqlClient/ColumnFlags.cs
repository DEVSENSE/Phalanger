namespace MySql.Data.MySqlClient
{
    using System;

    public enum ColumnFlags:int
    {
        AUTO_INCREMENT = 0x200,
        BINARY = 0x80,
        BLOB = 0x10,
        ENUM = 0x100,
        MULTIPLE_KEY = 8,
        NOT_NULL = 1,
        NUMBER = 0x8000,
        PRIMARY_KEY = 2,
        SET = 0x800,
        TIMESTAMP = 0x400,
        UNIQUE_KEY = 4,
        UNSIGNED = 0x20,
        ZERO_FILL = 0x40
    }
}

