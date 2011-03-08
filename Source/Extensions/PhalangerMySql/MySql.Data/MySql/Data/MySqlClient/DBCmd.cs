namespace MySql.Data.MySqlClient
{
    using System;

    internal enum DBCmd : byte
    {
        BINLOG_DUMP = 0x12,
        CLOSE_STMT = 0x19,
        CONNECT = 11,
        CONNECT_OUT = 20,
        CREATE_DB = 5,
        DEBUG = 13,
        DELAYED_INSERT = 0x10,
        DROP_DB = 6,
        EXECUTE = 0x17,
        FETCH = 0x1c,
        FIELD_LIST = 4,
        CHANGE_USER = 0x11,
        INIT_DB = 2,
        LONG_DATA = 0x18,
        PING = 14,
        PREPARE = 0x16,
        PROCESS_INFO = 10,
        PROCESS_KILL = 12,
        QUERY = 3,
        QUIT = 1,
        REGISTER_SLAVE = 0x15,
        RELOAD = 7,
        RESET_STMT = 0x1a,
        SET_OPTION = 0x1b,
        SHUTDOWN = 8,
        SLEEP = 0,
        STATISTICS = 9,
        TABLE_DUMP = 0x13,
        TIME = 15
    }
}

