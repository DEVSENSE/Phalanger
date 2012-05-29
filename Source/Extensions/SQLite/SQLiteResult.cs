using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PHP.Core;

namespace PHP.Library.Data
{
    [ImplementsType]
    public class SQLiteResult : PhpResource
    {
        private readonly PhpSQLiteDbResult m_res;

        internal SQLiteResult(PhpDbResult res)
            : base("sqlite result")
        {
            this.m_res = (PhpSQLiteDbResult)res;
        }

        [PhpVisible]
        public PhpArray fetchAll(object result_type, object decode_binary)
        {
            return SQLite.FetchAll(this.m_res, result_type, decode_binary);
        }
    }
}
