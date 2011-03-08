namespace MySql.Data.MySqlClient
{
    using MySql.Data.Common;
    using MySql.Data.Types;
    using System;

    internal interface IDriver
    {
        void Close(bool isOpen);
        void CloseStatement(int statementId);
        void Configure();
        void ExecuteStatement(MySqlPacket packet);
        bool FetchDataRow(int statementId, int columns);
        void GetColumnsData(MySqlField[] columns);
        int GetResult(ref int affectedRows, ref int insertedId);
        void Open();
        bool Ping();
        int PrepareStatement(string sql, ref MySqlField[] parameters);
        IMySqlValue ReadColumnValue(int index, MySqlField field, IMySqlValue valObject);
        void Reset();
        void ResetTimeout(int timeout);
        void SendQuery(MySqlPacket packet);
        void SetDatabase(string dbName);
        void SkipColumnValue(IMySqlValue valueObject);

        ClientFlags Flags { get; }

        ServerStatusFlags ServerStatus { get; }

        int ThreadId { get; }

        DBVersion Version { get; }

        int WarningCount { get; }
    }
}

