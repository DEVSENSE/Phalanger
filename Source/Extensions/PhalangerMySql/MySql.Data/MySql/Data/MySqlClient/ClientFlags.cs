namespace MySql.Data.MySqlClient
{
    using System;

    [Flags]
    internal enum ClientFlags : ulong
    {
        COMPRESS = 0x20L,
        CONNECT_WITH_DB = 8L,
        FOUND_ROWS = 2L,
        IGNORE_SIGPIPE = 0x1000L,
        IGNORE_SPACE = 0x100L,
        INTERACTIVE = 0x400L,
        LOCAL_FILES = 0x80L,
        LONG_FLAG = 4L,
        LONG_PASSWORD = 1L,
        MULTI_RESULTS = 0x20000L,
        MULTI_STATEMENTS = 0x10000L,
        NO_SCHEMA = 0x10L,
        ODBC = 0x40L,
        PROTOCOL_41 = 0x200L,
        PS_MULTI_RESULTS = 0x40000L,
        RESERVED = 0x4000L,
        SECURE_CONNECTION = 0x8000L,
        SSL = 0x800L,
        TRANSACTIONS = 0x2000L
    }
}

