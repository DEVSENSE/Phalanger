namespace MySql.Data.MySqlClient
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Threading;

    internal class MySqlPoolManager
    {
        private static List<MySqlPool> clearingPools = new List<MySqlPool>();
        internal static int maxConnectionIdleTime = 180;
        private static Hashtable pools = new Hashtable();
        private static Timer timer = new Timer(new TimerCallback(MySqlPoolManager.CleanIdleConnections), null, maxConnectionIdleTime * 0x3e8, maxConnectionIdleTime * 0x3e8);

        public static void CleanIdleConnections(object obj)
        {
            List<Driver> list = new List<Driver>();
            lock (pools.SyncRoot)
            {
                foreach (string str in pools.Keys)
                {
                    list.AddRange((pools[str] as MySqlPool).RemoveOldIdleConnections());
                }
            }
            foreach (Driver driver in list)
            {
                driver.Close();
            }
        }

        public static void ClearAllPools()
        {
            lock (pools.SyncRoot)
            {
                List<string> list = new List<string>(pools.Count);
                foreach (string str in pools.Keys)
                {
                    list.Add(str);
                }
                foreach (string str2 in list)
                {
                    ClearPoolByText(str2);
                }
            }
        }

        public static void ClearPool(MySqlConnectionStringBuilder settings)
        {
            ClearPoolByText(settings.ConnectionString);
        }

        private static void ClearPoolByText(string key)
        {
            lock (pools.SyncRoot)
            {
                if (pools.ContainsKey(key))
                {
                    MySqlPool item = pools[key] as MySqlPool;
                    clearingPools.Add(item);
                    item.Clear();
                    pools.Remove(key);
                }
            }
        }

        public static MySqlPool GetPool(MySqlConnectionStringBuilder settings)
        {
            string connectionString = settings.ConnectionString;
            lock (pools.SyncRoot)
            {
                MySqlPool pool = pools[connectionString] as MySqlPool;
                if (pool == null)
                {
                    pool = new MySqlPool(settings);
                    pools.Add(connectionString, pool);
                }
                else
                {
                    pool.Settings = settings;
                }
                return pool;
            }
        }

        public static void ReleaseConnection(Driver driver)
        {
            MySqlPool pool = driver.Pool;
            if (pool != null)
            {
                pool.ReleaseConnection(driver);
            }
        }

        public static void RemoveClearedPool(MySqlPool pool)
        {
            clearingPools.Remove(pool);
        }

        public static void RemoveConnection(Driver driver)
        {
            MySqlPool pool = driver.Pool;
            if (pool != null)
            {
                pool.RemoveConnection(driver);
            }
        }
    }
}

