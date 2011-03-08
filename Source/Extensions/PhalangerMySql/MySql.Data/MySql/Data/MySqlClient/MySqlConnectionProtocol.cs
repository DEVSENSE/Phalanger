namespace MySql.Data.MySqlClient
{
    using System;

    public enum MySqlConnectionProtocol
    {
        Memory = 4,
        NamedPipe = 2,
        Pipe = 2,
        SharedMemory = 4,
        Socket = 1,
        Sockets = 1,
        Tcp = 1,
        Unix = 3,
        UnixSocket = 3
    }
}

