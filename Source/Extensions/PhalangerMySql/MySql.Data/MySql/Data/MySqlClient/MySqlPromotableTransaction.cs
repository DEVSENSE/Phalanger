namespace MySql.Data.MySqlClient
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Threading;
    using System.Transactions;

    internal sealed class MySqlPromotableTransaction : IPromotableSinglePhaseNotification, ITransactionPromoter
    {
        private Transaction baseTransaction;
        private MySqlConnection connection;
        [ThreadStatic]
        private static Stack<MySqlTransactionScope> globalScopeStack;
        private Stack<MySqlTransactionScope> scopeStack;

        public MySqlPromotableTransaction(MySqlConnection connection, Transaction baseTransaction)
        {
            this.connection = connection;
            this.baseTransaction = baseTransaction;
        }

        void IPromotableSinglePhaseNotification.Initialize()
        {
            string name = Enum.GetName(typeof(System.Transactions.IsolationLevel), this.baseTransaction.IsolationLevel);
            System.Data.IsolationLevel iso = (System.Data.IsolationLevel) Enum.Parse(typeof(System.Data.IsolationLevel), name);
            MySqlTransaction simpleTransaction = this.connection.BeginTransaction(iso);
            if (globalScopeStack == null)
            {
                globalScopeStack = new Stack<MySqlTransactionScope>();
            }
            this.scopeStack = globalScopeStack;
            this.scopeStack.Push(new MySqlTransactionScope(this.connection, this.baseTransaction, simpleTransaction));
        }

        void IPromotableSinglePhaseNotification.Rollback(SinglePhaseEnlistment singlePhaseEnlistment)
        {
            this.scopeStack.Peek().Rollback(singlePhaseEnlistment);
            this.scopeStack.Pop();
        }

        void IPromotableSinglePhaseNotification.SinglePhaseCommit(SinglePhaseEnlistment singlePhaseEnlistment)
        {
            this.scopeStack.Pop().SinglePhaseCommit(singlePhaseEnlistment);
        }

        byte[] ITransactionPromoter.Promote()
        {
            throw new NotSupportedException();
        }

        public Transaction BaseTransaction
        {
            get
            {
                if (this.scopeStack.Count > 0)
                {
                    return this.scopeStack.Peek().baseTransaction;
                }
                return null;
            }
        }

        public bool InRollback
        {
            get
            {
                return ((this.scopeStack.Count > 0) && (this.scopeStack.Peek().rollbackThreadId == Thread.CurrentThread.ManagedThreadId));
            }
        }
    }
}

