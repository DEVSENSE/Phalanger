//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Data.SQLite;
//using System.Data;

//namespace PHP.Library.Data
//{
//    public sealed class SQLitePDOConnection : PDOConnection
//    {
//        private readonly SQLiteConnection m_con;

//        internal SQLiteConnection Connection { get { return this.m_con; } }

//        internal SQLitePDOConnection(PDODriver drv, SQLiteConnection con)
//            : base(drv)
//        {
//            this.m_con = con;
//        }

//        protected override void SetAttributeDefaults()
//        {
//            this.SetAttributeValueNoCheck(PDOAttributeType.PDO_ATTR_DRIVER_NAME, this.Driver.Scheme);
//            this.SetAttributeValueNoCheck(PDOAttributeType.PDO_ATTR_DEFAULT_FETCH_MODE, PDOFetchType.PDO_FETCH_BOTH);
//        }

//        protected override bool ValidateAttributeValue(int attribute, object value)
//        {
//            return false;
//        }

//        //public override bool ExecuteStatement(PDOStatement stmt, out IDbCommand com, out IDataReader dr)
//        //{
//        //    SQLiteCommand scom = this.m_con.CreateCommand();
//        //    scom.Transaction = (SQLiteTransaction)this.CurrentTransaction;
//        //    scom.CommandText = stmt.queryString;
//        //    scom.CommandType = CommandType.Text;
//        //    scom.CommandTimeout = this.GetAttributeValueInt(PDOAttributeType.PDO_ATTR_TIMEOUT, 30000);
//        //    SQLiteDataReader sdr = scom.ExecuteReader();

//        //    com = scom;
//        //    dr = sdr;

//        //    return true;
//        //}

//        protected override IDbTransaction begin_transaction()
//        {
//            return this.m_con.BeginTransaction();
//        }
//    }
//}
