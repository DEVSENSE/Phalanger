namespace MySql.Data.MySqlClient
{
    using System;

    public enum MySqlSslMode
    {
        None = 0,
        Prefered = 1,
        Preferred = 1,
        Required = 2,
        VerifyCA = 3,
        VerifyFull = 4
    }
}

