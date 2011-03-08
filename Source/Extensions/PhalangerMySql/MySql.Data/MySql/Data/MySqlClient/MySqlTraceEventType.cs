namespace MySql.Data.MySqlClient
{
    using System;

    public enum MySqlTraceEventType
    {
        ConnectionClosed = 2,
        ConnectionOpened = 1,
        Error = 13,
        NonQuery = 10,
        QueryClosed = 6,
        QueryNormalized = 14,
        QueryOpened = 3,
        ResultClosed = 5,
        ResultOpened = 4,
        StatementClosed = 9,
        StatementExecuted = 8,
        StatementPrepared = 7,
        UsageAdvisorWarning = 11,
        Warning = 12
    }
}

