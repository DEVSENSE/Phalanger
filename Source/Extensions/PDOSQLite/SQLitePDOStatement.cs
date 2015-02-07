using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PHP.Core;
using System.Data.SQLite;

namespace PHP.Library.Data
{
    public sealed class SQLitePDOStatement : PDOStatement
    {
        private readonly SQLiteCommand m_com;
        private SQLiteDataReader m_dr;

        internal SQLitePDOStatement(ScriptContext context, PDO pdo)
            : base(context, pdo)
        {
            this.m_com = (SQLiteCommand)pdo.Connection.CreateCommand();
        }

        public override bool ExecuteStatement()
        {
            this.m_com.Transaction = (SQLiteTransaction)this.m_pdo.Transaction;
            this.m_com.CommandTimeout = (int)this.m_pdo.GetAttribute(PDO.ATTR_TIMEOUT, 30);
            this.m_dr = this.m_com.ExecuteReader();
            return true;
        }

        protected override System.Data.IDbCommand CurrentCommand { get { return this.m_com; } }
        protected override System.Data.IDataReader CurrentReader { get { return this.m_dr; } }

        protected override void CloseReader()
        {
            if (this.m_dr != null)
            {
                this.m_dr.Close();
                this.m_dr.Dispose();
                this.m_dr = null;
            }
        }

        public override void Init(string query, Dictionary<int, object> options)
        {
            this.m_com.CommandText = query;
#if DEBUG
            System.Diagnostics.Debug.WriteLine("PDOSQLite : stmt.init query=" + query);
#endif
            if (options != null)
            {
                foreach (int key in options.Keys)
                {
                    this.m_pdo.SetAttribute(key, options[key]);
                }
            }
        }
    }
}
