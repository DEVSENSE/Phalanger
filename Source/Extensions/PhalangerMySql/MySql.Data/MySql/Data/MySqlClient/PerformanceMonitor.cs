namespace MySql.Data.MySqlClient
{
    using MySql.Data.MySqlClient.Properties;
    using System;
    using System.Diagnostics;

    internal class PerformanceMonitor
    {
        private MySqlConnection connection;
        private static PerformanceCounter procedureHardQueries;
        private static PerformanceCounter procedureSoftQueries;

        public PerformanceMonitor(MySqlConnection connection)
        {
            this.connection = connection;
            string perfMonCategoryName = Resources.PerfMonCategoryName;
            if (connection.Settings.UsePerformanceMonitor && (procedureHardQueries == null))
            {
                try
                {
                    procedureHardQueries = new PerformanceCounter(perfMonCategoryName, "HardProcedureQueries", false);
                    procedureSoftQueries = new PerformanceCounter(perfMonCategoryName, "SoftProcedureQueries", false);
                }
                catch (Exception exception)
                {
                    MySqlTrace.LogError(connection.ServerThread, exception.Message);
                }
            }
        }

        public void AddHardProcedureQuery()
        {
            if (this.connection.Settings.UsePerformanceMonitor && (procedureHardQueries != null))
            {
                procedureHardQueries.Increment();
            }
        }

        public void AddSoftProcedureQuery()
        {
            if (this.connection.Settings.UsePerformanceMonitor && (procedureSoftQueries != null))
            {
                procedureSoftQueries.Increment();
            }
        }
    }
}

