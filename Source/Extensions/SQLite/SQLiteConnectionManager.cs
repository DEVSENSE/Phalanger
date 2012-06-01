using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PHP.Library.Data
{
    internal sealed class SQLiteConnectionManager : ConnectionManager
    {
        protected override PhpDbConnection CreateConnection(string/*!*/ connectionString)
        {
            return new PhpSQLiteDbConnection(connectionString);
        }
    }

}
