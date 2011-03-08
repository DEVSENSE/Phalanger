namespace MySql.Data.MySqlClient
{
    using System;
    using System.Data.Common;
    using System.Reflection;

    public sealed class MySqlClientFactory : DbProviderFactory, IServiceProvider
    {
        private Type dbServicesType;
        public static MySqlClientFactory Instance = new MySqlClientFactory();
        private FieldInfo mySqlDbProviderServicesInstance;

        public override DbCommand CreateCommand()
        {
            return new MySqlCommand();
        }

        public override DbCommandBuilder CreateCommandBuilder()
        {
            return new MySqlCommandBuilder();
        }

        public override DbConnection CreateConnection()
        {
            return new MySqlConnection();
        }

        public override DbConnectionStringBuilder CreateConnectionStringBuilder()
        {
            return new MySqlConnectionStringBuilder();
        }

        public override DbDataAdapter CreateDataAdapter()
        {
            return new MySqlDataAdapter();
        }

        public override DbParameter CreateParameter()
        {
            return new MySqlParameter();
        }

        object IServiceProvider.GetService(Type serviceType)
        {
            if (serviceType != this.DbServicesType)
            {
                return null;
            }
            if (this.MySqlDbProviderServicesInstance == null)
            {
                return null;
            }
            return this.MySqlDbProviderServicesInstance.GetValue(null);
        }

        public override bool CanCreateDataSourceEnumerator
        {
            get
            {
                return false;
            }
        }

        private Type DbServicesType
        {
            get
            {
                if (this.dbServicesType == null)
                {
                    this.dbServicesType = Type.GetType("System.Data.Common.DbProviderServices, System.Data.Entity, \r\n                        Version=3.5.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", false);
                }
                return this.dbServicesType;
            }
        }

        private FieldInfo MySqlDbProviderServicesInstance
        {
            get
            {
                if (this.mySqlDbProviderServicesInstance == null)
                {
                    string str = Assembly.GetExecutingAssembly().FullName.Replace("MySql.Data", "MySql.Data.Entity");
                    this.mySqlDbProviderServicesInstance = Type.GetType(string.Format("MySql.Data.MySqlClient.MySqlProviderServices, {0}", str), false).GetField("Instance", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
                }
                return this.mySqlDbProviderServicesInstance;
            }
        }
    }
}

