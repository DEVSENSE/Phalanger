using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;

namespace PHP.Library.Data
{
    public sealed class SQLServerPDODriver : PDODriver
    {
        public const int SQLSRV_TXN_READ_UNCOMMITTED = PDO.ATTR_DRIVER_SPECIFIC + 1;

        public override string Scheme { get { return "sqlsrv"; } }

        public override PDOConnection OpenConnection(Core.ScriptContext context, string dsn_data, string username, string password, object argdriver_options)
        {
            throw new NotImplementedException();
        }

        public override object Quote(Core.ScriptContext context, object strobj, PDOParamType param_type)
        {
            throw new NotImplementedException();
        }

        public override PDOStatement CreateStatement(Core.ScriptContext context, PDO pdo)
        {
            throw new NotImplementedException();
        }

        protected override bool IsValueValidForAttribute(int att, object value)
        {
            throw new NotImplementedException();
        }

        public override object GetLastInsertId(Core.ScriptContext context, PDO pdo, string name)
        {
            using (var com = ((SqlConnection)pdo.Connection).CreateCommand())
            {
                com.Transaction = (SqlTransaction)pdo.Transaction;
                com.CommandText = "SELECT @@IDENTITY";
                return com.ExecuteScalar();
            }
        }
    }
}
