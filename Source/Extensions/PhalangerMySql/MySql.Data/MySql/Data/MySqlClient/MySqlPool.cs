namespace MySql.Data.MySqlClient
{
    using MySql.Data.MySqlClient.Properties;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Threading;

    internal sealed class MySqlPool
    {
        private AutoResetEvent autoEvent;
        private int available;
        private bool beingCleared;
        private Queue<Driver> idlePool;
        private List<Driver> inUsePool;
        private uint maxSize;
        private uint minSize;
        private MySql.Data.MySqlClient.ProcedureCache procedureCache;
        private MySqlConnectionStringBuilder settings;

        public MySqlPool(MySqlConnectionStringBuilder settings)
        {
            this.minSize = settings.MinimumPoolSize;
            this.maxSize = settings.MaximumPoolSize;
            this.available = (int) this.maxSize;
            this.autoEvent = new AutoResetEvent(false);
            if (this.minSize > this.maxSize)
            {
                this.minSize = this.maxSize;
            }
            this.settings = settings;
            this.inUsePool = new List<Driver>((int) this.maxSize);
            this.idlePool = new Queue<Driver>((int) this.maxSize);
            for (int i = 0; i < this.minSize; i++)
            {
                this.EnqueueIdle(this.CreateNewPooledConnection());
            }
            this.procedureCache = new MySql.Data.MySqlClient.ProcedureCache((int) settings.ProcedureCacheSize);
        }

        internal void Clear()
        {
            lock (((ICollection) this.idlePool).SyncRoot)
            {
                this.beingCleared = true;
                while (this.idlePool.Count > 0)
                {
                    this.idlePool.Dequeue().Close();
                }
            }
        }

        private Driver CreateNewPooledConnection()
        {
            Driver driver = Driver.Create(this.settings);
            driver.Pool = this;
            return driver;
        }

        private void EnqueueIdle(Driver driver)
        {
            driver.IdleSince = DateTime.Now;
            this.idlePool.Enqueue(driver);
        }

        public Driver GetConnection()
        {
            int num = (int) (this.settings.ConnectionTimeout * 0x3e8);
            int millisecondsTimeout = num;
            DateTime now = DateTime.Now;
            while (millisecondsTimeout > 0)
            {
                Driver driver = this.TryToGetDriver();
                if (driver != null)
                {
                    return driver;
                }
                if (!this.autoEvent.WaitOne(millisecondsTimeout, false))
                {
                    break;
                }
                millisecondsTimeout = num - ((int) DateTime.Now.Subtract(now).TotalMilliseconds);
            }
            throw new MySqlException(Resources.TimeoutGettingConnection);
        }

        private Driver GetPooledConnection()
        {
            Driver item = null;
            lock (((ICollection) this.idlePool).SyncRoot)
            {
                if (this.HasIdleConnections)
                {
                    item = this.idlePool.Dequeue();
                }
            }
            if (item != null)
            {
                try
                {
                    item.ResetTimeout((int) (this.Settings.ConnectionTimeout * 0x3e8));
                }
                catch (Exception)
                {
                    item.Close();
                    item = null;
                }
            }
            if (item != null)
            {
                if (!item.Ping())
                {
                    item.Close();
                    item = null;
                }
                else if (this.settings.ConnectionReset)
                {
                    item.Reset();
                }
            }
            if (item == null)
            {
                item = this.CreateNewPooledConnection();
            }
            lock (((ICollection) this.inUsePool).SyncRoot)
            {
                this.inUsePool.Add(item);
            }
            return item;
        }

        public void ReleaseConnection(Driver driver)
        {
            lock (((ICollection) this.inUsePool).SyncRoot)
            {
                if (this.inUsePool.Contains(driver))
                {
                    this.inUsePool.Remove(driver);
                }
            }
            if (driver.ConnectionLifetimeExpired() || this.beingCleared)
            {
                driver.Close();
            }
            else
            {
                lock (((ICollection) this.idlePool).SyncRoot)
                {
                    this.EnqueueIdle(driver);
                }
            }
            Interlocked.Increment(ref this.available);
            this.autoEvent.Set();
        }

        public void RemoveConnection(Driver driver)
        {
            lock (((ICollection) this.inUsePool).SyncRoot)
            {
                if (this.inUsePool.Contains(driver))
                {
                    this.inUsePool.Remove(driver);
                    Interlocked.Increment(ref this.available);
                    this.autoEvent.Set();
                }
            }
            if (this.beingCleared && (this.NumConnections == 0))
            {
                MySqlPoolManager.RemoveClearedPool(this);
            }
        }

        internal List<Driver> RemoveOldIdleConnections()
        {
            List<Driver> list = new List<Driver>();
            DateTime now = DateTime.Now;
            lock (((ICollection) this.idlePool).SyncRoot)
            {
                while (this.idlePool.Count > this.minSize)
                {
                    Driver item = this.idlePool.Peek();
                    if (item.IdleSince.Add(new TimeSpan(0, 0, MySqlPoolManager.maxConnectionIdleTime)).CompareTo(now) >= 0)
                    {
                        return list;
                    }
                    list.Add(item);
                    this.idlePool.Dequeue();
                }
            }
            return list;
        }

        private Driver TryToGetDriver()
        {
            Driver pooledConnection;
            if (Interlocked.Decrement(ref this.available) < 0)
            {
                Interlocked.Increment(ref this.available);
                return null;
            }
            try
            {
                pooledConnection = this.GetPooledConnection();
            }
            catch (Exception exception)
            {
                MySqlTrace.LogError(-1, exception.Message);
                Interlocked.Increment(ref this.available);
                throw;
            }
            return pooledConnection;
        }

        public bool BeingCleared
        {
            get
            {
                return this.beingCleared;
            }
        }

        private bool HasIdleConnections
        {
            get
            {
                return (this.idlePool.Count > 0);
            }
        }

        private int NumConnections
        {
            get
            {
                return (this.idlePool.Count + this.inUsePool.Count);
            }
        }

        public MySql.Data.MySqlClient.ProcedureCache ProcedureCache
        {
            get
            {
                return this.procedureCache;
            }
        }

        public MySqlConnectionStringBuilder Settings
        {
            get
            {
                return this.settings;
            }
            set
            {
                this.settings = value;
            }
        }
    }
}

