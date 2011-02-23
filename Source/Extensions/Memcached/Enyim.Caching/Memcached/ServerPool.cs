using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.Threading;
using System.Diagnostics;
using Enyim.Caching.Configuration;
using Enyim.Caching.Memcached.Distribution;
using PHP.Library.Memcached;

namespace Enyim.Caching.Memcached
{
    /// <summary>
    /// Server pool.
    /// </summary>
	public class DefaultServerPool : IDisposable, IServerPool
	{
		// holds all dead servers which will be periodically rechecked and put back into the working servers if found alive
		private List<IMemcachedNode> deadServers = new List<IMemcachedNode>();
		// holds all of the currently working servers
		private List<IMemcachedNode> workingServers = new List<IMemcachedNode>();
		private ReadOnlyCollection<IMemcachedNode> publicWorkingServers;

		// used to synchronize read/write accesses on the server lists
		private ReaderWriterLock serverAccessLock = new ReaderWriterLock();

		private Timer isAliveTimer;
		private IMemcachedClientConfiguration configuration;

        #region Properties

        /// <summary>
        /// Current prefix added to all the key parameters (used by protocol).
        /// </summary>
        public string PrefixKey { get; set; }

        /// <summary>
        /// Current key hashing function.
        /// </summary>
        public HashConstants KeyHash
        {
            set
            {
                _KeyHash = value;
                _nodeLocator = null;
            }
            get
            {
                return _KeyHash;
            }
        }
        private HashConstants _KeyHash = HashConstants.Default;

        /// <summary>
        /// Current distribution method.
        /// </summary>
        public DistributionConstants DistributionMethod
        {
            set
            {
                _DistributionMethod = value;
                _nodeLocator = null;
            }
            get
            {
                return _DistributionMethod;
            }
        }
        private DistributionConstants _DistributionMethod = DistributionConstants.ModulA;

        #endregion

        /// <summary>
        /// Initializes the server pool.
        /// </summary>
        /// <param name="configuration">Configuration.</param>
        /// <param name="keyTransformer">Key transformer.</param>
        /// <param name="nodeLocatorType">Locator.</param>
        public DefaultServerPool(IMemcachedClientConfiguration configuration, IMemcachedKeyTransformer keyTransformer, /*TranscoderBase transcoder,*/ Type nodeLocatorType)
		{
			if (configuration == null)
				throw new ArgumentNullException("configuration", "Invalid or missing pool configuration. Check if the enyim.com/memcached section or your custom section presents in the app/web.config.");

            /*Debug.Assert(transcoder != null);*/
            Debug.Assert(keyTransformer != null);

			this.configuration = configuration;
			this.isAliveTimer = new Timer(callback_isAliveTimer, null, (int)this.configuration.SocketPool.DeadTimeout.TotalMilliseconds, (int)this.configuration.SocketPool.DeadTimeout.TotalMilliseconds);

            _keyTransformer = keyTransformer;
            /*_transcoder = transcoder;*/
            _nodeLocatorType = nodeLocatorType;
		}
        
		//public event Action<PooledSocket> SocketConnected;

		/// <summary>
		/// This will start the pool: initializes the nodelocator, warms up the socket pools, etc.
		/// </summary>
		public void Start()
		{
            //RebuildIndexes();
            _nodeLocator = null;
		}

        /// <summary>
        /// Add new servers into the pool even if Start() was called already.
        /// </summary>
        /// <param name="servers">List of servers to add.</param>
        public void AddServers(IEnumerable<NamedIPEndPoint> servers)
        {
            if (servers == null)
                return;

            this.serverAccessLock.AcquireWriterLock(Timeout.Infinite);
            try
            {

                // reset the public list
                this.publicWorkingServers = null;

                // initialize the server list
                foreach (NamedIPEndPoint ip in servers)
                {
                    MemcachedNode node = new MemcachedNode(ip, this.configuration.SocketPool, this.Authenticator);

                    this.workingServers.Add(node);
                }
            }
            finally
            {
                this.serverAccessLock.ReleaseWriterLock();
            }

            // (re)initializes the locator
            //RebuildIndexes();
            _nodeLocator = null;
        }

        /// <summary>
        /// Recreate all MemcachedNode items.
        /// </summary>
        public void ReaddServers()
        {
            List<NamedIPEndPoint> ipEndPoints = new List<NamedIPEndPoint>();
            foreach (var x in WorkingServers) ipEndPoints.Add(x.EndPoint);

            this.serverAccessLock.AcquireWriterLock(Timeout.Infinite);
            try { this.workingServers.Clear(); }
            finally { this.serverAccessLock.ReleaseWriterLock(); }

            AddServers(ipEndPoints);
        }

        /// <summary>
		/// Checks if a dead node is working again.
		/// </summary>
		/// <param name="state"></param>
		private void callback_isAliveTimer(object state)
		{
            if (this.deadServers.Count == 0)
                return;

			this.serverAccessLock.AcquireReaderLock(Timeout.Infinite);

			try
			{
				if (this.deadServers.Count == 0)
					return;

				List<IMemcachedNode> resurrectList = this.deadServers.FindAll(delegate(IMemcachedNode node) { return node.Ping(); });

				if (resurrectList.Count > 0)
				{
					this.serverAccessLock.UpgradeToWriterLock(Timeout.Infinite);

					resurrectList.ForEach(delegate(IMemcachedNode node)
					{
						// maybe it got removed while we were waiting for the writer lock upgrade?
						if (this.deadServers.Remove(node))
							this.workingServers.Add(node);
					});

					//this.RebuildIndexes();
                    _nodeLocator = null;
				}
			}
			finally
			{
				this.serverAccessLock.ReleaseLock();
			}
		}

		/// <summary>
		/// Marks a node as dead (unusable)
		///  - moves the node to the  "dead list"
		///  - recreates the locator based on the new list of still functioning servers
		/// </summary>
		/// <param name="node"></param>
		private void MarkAsDead(IMemcachedNode node)
		{
			this.serverAccessLock.UpgradeToWriterLock(Timeout.Infinite);

			try
			{
				// server gained AoeREZ while AFK?
				if (!node.IsAlive)
				{
					this.workingServers.Remove(node);
					this.deadServers.Add(node);

					//this.RebuildIndexes();
                    _nodeLocator = null;
				}
			}
			finally
			{
				this.serverAccessLock.ReleaseLock();
			}
		}

		/// <summary>
		/// Returns the <see cref="t:IKeyTransformer"/> instance used by the pool
		/// </summary>
		public IMemcachedKeyTransformer KeyTransformer
		{
            get { return this._keyTransformer; }
            set
            {
                Debug.Assert(value != null);
                this._keyTransformer = value;
            }
		}
        private IMemcachedKeyTransformer _keyTransformer;

        private static System.Security.Cryptography.HashAlgorithm CreateHashAlgorithm(HashConstants hashFunc)
        {
            switch (hashFunc)
            {
                case HashConstants.Default:
                    return new Hashes.Hash_OneAtATime();
                case HashConstants.CRC:
                    return new Hashes.Hash_CRC32();
                case HashConstants.MD5:
                    return new Hashes.Hash_MD5();
                case HashConstants.MURMUR:
                    return new Hashes.Hash_Murmur();
                case HashConstants.FNV1_32:
                    return new Hashes.Hash_FNV1_32();
                case HashConstants.FNV1A_32:
                    return new Hashes.Hash_FNV1a_32();
                case HashConstants.FNV1_64:
                    return new Hashes.Hash_FNV1_64();
                case HashConstants.FNV1A_64:
                    return new Hashes.Hash_FNV1a_64();
                case HashConstants.HSIEH:
                    return new Hashes.Hash_HSIEH();
                    
                default:
                    throw new NotSupportedException(hashFunc.ToString());
            }
        }

        private static IMemcachedNodeLocator CreateNodeLocator(DistributionConstants distrFunc)
        {
            switch (distrFunc)
            {
                case DistributionConstants.ModulA:
                    return new ModulaNodeLocator();
                case DistributionConstants.Consistent:
                    return new DefaultNodeLocator();

                default:
                    throw new NotSupportedException(distrFunc.ToString());
            }
        }

        /// <summary>
        /// Get locator.
        /// </summary>
        public IMemcachedNodeLocator NodeLocator
        {
            get
            {
                if (_nodeLocator == null)
                {
                    // recreate and reinitialize the node locator
                    this.serverAccessLock.UpgradeToWriterLock(Timeout.Infinite);

                    try
                    {
                        if (_nodeLocator == null)
                        {
                            IMemcachedNodeLocator l = CreateNodeLocator(DistributionMethod);// (_nodeLocatorType == null) ? new ModulaNodeLocator() : (IMemcachedNodeLocator)Enyim.Reflection.FastActivator.CreateInstance(_nodeLocatorType);
                            l.Initialize(this.workingServers, CreateHashAlgorithm(KeyHash));
                            _nodeLocator = l;
                        }                        
                    }
                    finally
                    {
                        this.serverAccessLock.ReleaseLock();
                    }
                }
                return _nodeLocator;
            }
        }
        private IMemcachedNodeLocator _nodeLocator = null;

        /// <summary>
        /// Get/set locator type.
        /// </summary>
        public Type NodeLocatorType
        {
            get { return _nodeLocatorType; }
            private set
            {
                if (_nodeLocatorType != value)
                {
                    _nodeLocatorType = value;
                    //RebuildIndexes();
                    _nodeLocator = null;
                }
            }
        }
        private Type _nodeLocatorType = null;
        
        /// <summary>
        /// Transcoder performs serialization and deserialization of objects.
        /// </summary>
        public readonly PhpTranscoder Transcoder = new PhpTranscoder();
		/*{
			get { return this._transcoder; }
            set
            {
                Debug.Assert(value != null);
                this._transcoder = value;
            }
		}
        private TranscoderBase _transcoder;*/

		/// <summary>
		/// Finds the <see cref="T:MemcachedNode"/> which is responsible for the specified item
		/// </summary>
		/// <param name="itemKey"></param>
		/// <returns></returns>
		private IMemcachedNode LocateNode(string itemKey)
		{
			this.serverAccessLock.AcquireReaderLock(Timeout.Infinite);

			try
			{
				IMemcachedNode node = this.NodeLocator.Locate(itemKey);
				if (node == null)
					return null;

				if (node.IsAlive)
					return node;

				this.MarkAsDead(node);

				return this.LocateNode(itemKey);
			}
			finally
			{
				this.serverAccessLock.ReleaseLock();
			}
		}

        /// <summary>
        /// Get item from pool.
        /// </summary>
        /// <param name="itemKey">Item key.</param>
        /// <returns>Socket or <b>null</b> reference if server according to the key is not found.</returns>
		public PooledSocket Acquire(string itemKey)
		{
			if (this.serverAccessLock == null)
				throw new ObjectDisposedException("ServerPool");

			IMemcachedNode server = this.LocateNode(itemKey);

			if (server == null)
				return null;

			return server.Acquire();
		}

        /// <summary>
        /// Get collection of nodes.
        /// </summary>
		public ReadOnlyCollection<IMemcachedNode> WorkingServers
		{
			get
			{
				if (this.publicWorkingServers == null)
				{
					this.serverAccessLock.AcquireReaderLock(Timeout.Infinite);

					try
					{
						if (this.publicWorkingServers == null)
						{
							this.publicWorkingServers = new ReadOnlyCollection<IMemcachedNode>(this.workingServers.ToArray());
						}
					}
					finally
					{
						this.serverAccessLock.ReleaseLock();
					}
				}

				return this.publicWorkingServers;
			}
		}

        /// <summary>
        /// Servers count.
        /// </summary>
        public int ServersCount
        {
            get
            {
                return workingServers.Count;
            }
        }

        /// <summary>
        /// Servers count.
        /// </summary>
        public int Count
        {
            get
            {
                return this.workingServers.Count;
            }
        }

        /// <summary>
        /// Found nodes according to given keys.
        /// </summary>
        /// <param name="keys"></param>
        /// <returns></returns>
		public IDictionary<IMemcachedNode, IList<string>> SplitKeys(IEnumerable<string> keys)
		{
			Dictionary<IMemcachedNode, IList<string>> keysByNode = new Dictionary<IMemcachedNode, IList<string>>(MemcachedNode.Comparer.Instance);

			IList<string> nodeKeys;
			IMemcachedNode node;

			foreach (string key in keys)
			{
				node = this.LocateNode(key);

				if (!keysByNode.TryGetValue(node, out nodeKeys))
				{
					nodeKeys = new List<string>();
					keysByNode.Add(node, nodeKeys);
				}

				nodeKeys.Add(key);
			}

			return keysByNode;
		}

        /// <summary>
        /// Finalizes the pool.
        /// </summary>
		~DefaultServerPool()
		{
			try { ((IDisposable)this).Dispose(); }
			catch { }
		}

		#region [ IDisposable                  ]
		void IDisposable.Dispose()
		{
			ReaderWriterLock rwl = this.serverAccessLock;

			if (Interlocked.CompareExchange(ref this.serverAccessLock, null, rwl) == null)
				return;

			GC.SuppressFinalize(this);

			try
			{
				rwl.UpgradeToWriterLock(Timeout.Infinite);

				Action<IMemcachedNode> cleanupNode = node =>
				{
					//node.SocketConnected -= this.OnSocketConnected;
					node.Dispose();
				};

				// dispose the nodes (they'll kill connections, etc.)
				this.deadServers.ForEach(cleanupNode);
				this.workingServers.ForEach(cleanupNode);

				this.deadServers.Clear();
				this.workingServers.Clear();

				this._nodeLocator = null;

				this.isAliveTimer.Dispose();
				this.isAliveTimer = null;
			}
			finally
			{
				rwl.ReleaseLock();
			}
		}
		#endregion

		#region IServerPool Members

		IMemcachedKeyTransformer IServerPool.KeyTransformer
		{
			get { return this.KeyTransformer; }
		}

        TranscoderBase IServerPool.Transcoder
		{
			get { return this.Transcoder; }
		}

		//IAuthenticator IServerPool.Authenticator
		//{
		//    get { return this.authenticator; }
		//}

		PooledSocket IServerPool.Acquire(string key)
		{
			return this.Acquire(key);
		}

		IEnumerable<IMemcachedNode> IServerPool.GetServers()
		{
            return this.WorkingServers;
		}

		void IServerPool.Start()
		{
			this.Start();
		}

		//event Action<PooledSocket> IServerPool.SocketConnected
		//{
		//    add { this.SocketConnected += value; }
		//    remove { this.SocketConnected -= value; }
		//}

		#endregion

        /// <summary>
        /// Authenticator.
        /// </summary>
		public IAuthenticator Authenticator { get; set; }
	}
}

#region [ License information          ]
/* ************************************************************
 * 
 *    Copyright (c) 2010 Attila Kiskó, enyim.com
 *    
 *    Licensed under the Apache License, Version 2.0 (the "License");
 *    you may not use this file except in compliance with the License.
 *    You may obtain a copy of the License at
 *    
 *        http://www.apache.org/licenses/LICENSE-2.0
 *    
 *    Unless required by applicable law or agreed to in writing, software
 *    distributed under the License is distributed on an "AS IS" BASIS,
 *    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *    See the License for the specific language governing permissions and
 *    limitations under the License.
 *    
 * ************************************************************/
#endregion
