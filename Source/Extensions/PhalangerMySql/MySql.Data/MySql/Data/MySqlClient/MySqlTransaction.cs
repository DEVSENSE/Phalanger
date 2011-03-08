namespace MySql.Data.MySqlClient
{
    using System;
    using System.Data;
    using System.Data.Common;

    public sealed class MySqlTransaction : DbTransaction
    {
        private MySqlConnection conn;
        private System.Data.IsolationLevel level;
        private bool open;

        internal MySqlTransaction(MySqlConnection c, System.Data.IsolationLevel il)
        {
            this.conn = c;
            this.level = il;
            this.open = true;
        }

        public override void Commit()
        {
            if ((this.conn == null) || ((this.conn.State != ConnectionState.Open) && !this.conn.SoftClosed))
            {
                throw new InvalidOperationException("Connection must be valid and open to commit transaction");
            }
            if (!this.open)
            {
                throw new InvalidOperationException("Transaction has already been committed or is not pending");
            }
            new MySqlCommand("COMMIT", this.conn).ExecuteNonQuery();
            this.open = false;
        }

        protected override void Dispose(bool disposing)
        {
            if ((((this.conn != null) && (this.conn.State == ConnectionState.Open)) || this.conn.SoftClosed) && this.open)
            {
                this.Rollback();
            }
            base.Dispose(disposing);
        }

        public override void Rollback()
        {
            if ((this.conn == null) || ((this.conn.State != ConnectionState.Open) && !this.conn.SoftClosed))
            {
                throw new InvalidOperationException("Connection must be valid and open to rollback transaction");
            }
            if (!this.open)
            {
                throw new InvalidOperationException("Transaction has already been rolled back or is not pending");
            }
            new MySqlCommand("ROLLBACK", this.conn).ExecuteNonQuery();
            this.open = false;
        }

        public MySqlConnection Connection
        {
            get
            {
                return this.conn;
            }
        }

        protected override System.Data.Common.DbConnection DbConnection
        {
            get
            {
                return this.conn;
            }
        }

        public override System.Data.IsolationLevel IsolationLevel
        {
            get
            {
                return this.level;
            }
        }
    }
}

