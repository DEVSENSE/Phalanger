namespace MySql.Data.MySqlClient
{
    using System;

    [Flags]
    internal enum ServerStatusFlags
    {
        AnotherQuery = 8,
        AutoCommitMode = 2,
        BadIndex = 0x10,
        CursorExists = 0x40,
        InTransaction = 1,
        LastRowSent = 0x80,
        MoreResults = 4,
        NoIndex = 0x20,
        OutputParameters = 0x1000
    }
}

