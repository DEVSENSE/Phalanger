namespace MySql.Data.MySqlClient
{
    using MySql.Data.Common;
    using MySql.Data.MySqlClient.Properties;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Text;
    using System.Threading;

    internal class TracingDriver : Driver
    {
        private ResultSet activeResult;
        private static long driverCounter;
        private long driverId;
        private int rowSizeInBytes;

        public TracingDriver(MySqlConnectionStringBuilder settings) : base(settings)
        {
            this.driverId = Interlocked.Increment(ref driverCounter);
        }

        private bool AllFieldsAccessed(ResultSet rs)
        {
            if ((rs.Fields != null) && (rs.Fields.Length != 0))
            {
                for (int i = 0; i < rs.Fields.Length; i++)
                {
                    if (!rs.FieldRead(i))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public override void Close()
        {
            base.Close();
            MySqlTrace.TraceEvent(TraceEventType.Information, MySqlTraceEventType.ConnectionClosed, Resources.TraceCloseConnection, new object[] { this.driverId });
        }

        public override void CloseQuery(MySqlConnection connection, int statementId)
        {
            base.CloseQuery(connection, statementId);
            MySqlTrace.TraceEvent(TraceEventType.Information, MySqlTraceEventType.QueryClosed, Resources.TraceQueryDone, new object[] { this.driverId });
        }

        public override void CloseStatement(int id)
        {
            base.CloseStatement(id);
            MySqlTrace.TraceEvent(TraceEventType.Information, MySqlTraceEventType.StatementClosed, Resources.TraceStatementClosed, new object[] { this.driverId, id });
        }

        public override void ExecuteStatement(MySqlPacket packetToExecute)
        {
            base.ExecuteStatement(packetToExecute);
            int position = packetToExecute.Position;
            packetToExecute.Position = 1;
            int num2 = packetToExecute.ReadInteger(4);
            packetToExecute.Position = position;
            MySqlTrace.TraceEvent(TraceEventType.Information, MySqlTraceEventType.StatementExecuted, Resources.TraceStatementExecuted, new object[] { this.driverId, num2, base.ThreadID });
        }

        public override bool FetchDataRow(int statementId, int columns)
        {
            bool flag2;
            try
            {
                bool flag = base.FetchDataRow(statementId, columns);
                if (flag)
                {
                    this.rowSizeInBytes += (base.handler as NativeDriver).Packet.Length;
                }
                flag2 = flag;
            }
            catch (MySqlException exception)
            {
                MySqlTrace.TraceEvent(TraceEventType.Error, MySqlTraceEventType.Error, Resources.TraceFetchError, new object[] { this.driverId, exception.Number, exception.Message });
                throw exception;
            }
            return flag2;
        }

        protected override int GetResult(int statementId, ref int affectedRows, ref int insertedId)
        {
            int num2;
            try
            {
                int num = base.GetResult(statementId, ref affectedRows, ref insertedId);
                MySqlTrace.TraceEvent(TraceEventType.Information, MySqlTraceEventType.ResultOpened, Resources.TraceResult, new object[] { this.driverId, num, (int) affectedRows, (int) insertedId });
                num2 = num;
            }
            catch (MySqlException exception)
            {
                MySqlTrace.TraceEvent(TraceEventType.Information, MySqlTraceEventType.Error, Resources.TraceOpenResultError, new object[] { this.driverId, exception.Number, exception.Message });
                throw exception;
            }
            return num2;
        }

        public override ResultSet NextResult(int statementId)
        {
            if (this.activeResult != null)
            {
                if (base.Settings.UseUsageAdvisor)
                {
                    this.ReportUsageAdvisorWarnings(statementId, this.activeResult);
                }
                MySqlTrace.TraceEvent(TraceEventType.Information, MySqlTraceEventType.ResultClosed, Resources.TraceResultClosed, new object[] { this.driverId, this.activeResult.TotalRows, this.activeResult.SkippedRows, this.rowSizeInBytes });
                this.rowSizeInBytes = 0;
                this.activeResult = null;
            }
            this.activeResult = base.NextResult(statementId);
            return this.activeResult;
        }

        public override void Open()
        {
            base.Open();
            MySqlTrace.TraceEvent(TraceEventType.Information, MySqlTraceEventType.ConnectionOpened, Resources.TraceOpenConnection, new object[] { this.driverId, base.Settings.ConnectionString, base.ThreadID });
        }

        public override int PrepareStatement(string sql, ref MySqlField[] parameters)
        {
            int num = base.PrepareStatement(sql, ref parameters);
            MySqlTrace.TraceEvent(TraceEventType.Information, MySqlTraceEventType.StatementPrepared, Resources.TraceStatementPrepared, new object[] { this.driverId, sql, num });
            return num;
        }

        private void ReportUsageAdvisorWarnings(int statementId, ResultSet rs)
        {
            if (base.Settings.UseUsageAdvisor)
            {
                if (base.HasStatus(ServerStatusFlags.NoIndex))
                {
                    MySqlTrace.TraceEvent(TraceEventType.Warning, MySqlTraceEventType.UsageAdvisorWarning, Resources.TraceUAWarningNoIndex, new object[] { this.driverId, UsageAdvisorWarningFlags.NoIndex });
                }
                else if (base.HasStatus(ServerStatusFlags.BadIndex))
                {
                    MySqlTrace.TraceEvent(TraceEventType.Warning, MySqlTraceEventType.UsageAdvisorWarning, Resources.TraceUAWarningBadIndex, new object[] { this.driverId, UsageAdvisorWarningFlags.BadIndex });
                }
                if (rs.SkippedRows > 0)
                {
                    MySqlTrace.TraceEvent(TraceEventType.Warning, MySqlTraceEventType.UsageAdvisorWarning, Resources.TraceUAWarningSkippedRows, new object[] { this.driverId, UsageAdvisorWarningFlags.SkippedRows, rs.SkippedRows });
                }
                if (!this.AllFieldsAccessed(rs))
                {
                    StringBuilder builder = new StringBuilder("");
                    string str = "";
                    for (int i = 0; i < rs.Size; i++)
                    {
                        if (!rs.FieldRead(i))
                        {
                            builder.AppendFormat("{0}{1}", str, rs.Fields[i].ColumnName);
                            str = ",";
                        }
                    }
                    MySqlTrace.TraceEvent(TraceEventType.Warning, MySqlTraceEventType.UsageAdvisorWarning, Resources.TraceUAWarningSkippedColumns, new object[] { this.driverId, UsageAdvisorWarningFlags.SkippedColumns, builder.ToString() });
                }
                if (rs.Fields != null)
                {
                    foreach (MySqlField field in rs.Fields)
                    {
                        StringBuilder builder2 = new StringBuilder();
                        string str2 = "";
                        foreach (Type type in field.TypeConversions)
                        {
                            builder2.AppendFormat("{0}{1}", str2, type.Name);
                            str2 = ",";
                        }
                        if (builder2.Length > 0)
                        {
                            MySqlTrace.TraceEvent(TraceEventType.Warning, MySqlTraceEventType.UsageAdvisorWarning, Resources.TraceUAWarningFieldConversion, new object[] { this.driverId, UsageAdvisorWarningFlags.FieldConversion, field.ColumnName, builder2.ToString() });
                        }
                    }
                }
            }
        }

        public override List<MySqlError> ReportWarnings(MySqlConnection connection)
        {
            List<MySqlError> list = base.ReportWarnings(connection);
            foreach (MySqlError error in list)
            {
                MySqlTrace.TraceEvent(TraceEventType.Warning, MySqlTraceEventType.Warning, Resources.TraceWarning, new object[] { this.driverId, error.Level, error.Code, error.Message });
            }
            return list;
        }

        public override void SendQuery(MySqlPacket p)
        {
            this.rowSizeInBytes = 0;
            string sql = base.Encoding.GetString(p.Buffer, 5, p.Length - 5);
            string str2 = null;
            if (sql.Length > 300)
            {
                sql = sql.Substring(0, 300);
                str2 = new QueryNormalizer().Normalize(sql);
            }
            base.SendQuery(p);
            MySqlTrace.TraceEvent(TraceEventType.Information, MySqlTraceEventType.QueryOpened, Resources.TraceQueryOpened, new object[] { this.driverId, base.ThreadID, sql });
            if (str2 != null)
            {
                MySqlTrace.TraceEvent(TraceEventType.Information, MySqlTraceEventType.QueryNormalized, Resources.TraceQueryNormalized, new object[] { this.driverId, base.ThreadID, str2 });
            }
        }

        public override void SetDatabase(string dbName)
        {
            base.SetDatabase(dbName);
            MySqlTrace.TraceEvent(TraceEventType.Information, MySqlTraceEventType.NonQuery, Resources.TraceSetDatabase, new object[] { this.driverId, dbName });
        }
    }
}

