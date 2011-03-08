namespace MySql.Data.MySqlClient
{
    using System;
    using System.Collections;
    using System.Transactions;

    internal class DriverTransactionManager
    {
        private static Hashtable driversInUse = new Hashtable();

        public static Driver GetDriverInTransaction(Transaction transaction)
        {
            lock (driversInUse.SyncRoot)
            {
                return (Driver) driversInUse[transaction.GetHashCode()];
            }
        }

        public static void RemoveDriverInTransaction(Transaction transaction)
        {
            lock (driversInUse.SyncRoot)
            {
                driversInUse.Remove(transaction.GetHashCode());
            }
        }

        public static void SetDriverInTransaction(Driver driver)
        {
            lock (driversInUse.SyncRoot)
            {
                driversInUse[driver.CurrentTransaction.BaseTransaction.GetHashCode()] = driver;
            }
        }
    }
}

