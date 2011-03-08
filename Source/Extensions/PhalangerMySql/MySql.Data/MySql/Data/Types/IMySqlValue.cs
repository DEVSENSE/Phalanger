namespace MySql.Data.Types
{
    using MySql.Data.MySqlClient;
    using System;
    using System.Data;

    internal interface IMySqlValue
    {
        IMySqlValue ReadValue(MySqlPacket packet, long length, bool isNull);
        void SkipValue(MySqlPacket packet);
        void WriteValue(MySqlPacket packet, bool binary, object value, int length);

        System.Data.DbType DbType { get; }

        bool IsNull { get; }

        MySql.Data.MySqlClient.MySqlDbType MySqlDbType { get; }

        string MySqlTypeName { get; }

        Type SystemType { get; }

        object Value { get; }
    }
}

