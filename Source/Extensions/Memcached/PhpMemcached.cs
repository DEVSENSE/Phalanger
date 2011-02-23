using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

using PHP.Core;

using Enyim.Caching;
using Enyim.Caching.Memcached;
using Enyim.Caching.Configuration;
using System.Threading;

namespace PHP.Library.Memcached
{
    /// <summary>
    /// Implements PHP functions provided by Memcached extension.
    /// TODO: thread safe.
    /// TODO: the rest of the options.
    /// TODO: the distribution method has to use NamedIPEndPoint.Weight.
    /// </summary>
    [ImplementsType()]
    public partial class Memcached : PhpObject
    {
        #region IProtocolImplementation and IServerPool

        /// <summary>
        /// The memcached client and its collection of servers.
        /// </summary>
        private class ClientState
        {
            public IProtocolImplementation ProtocolImpl;
            public DefaultServerPool Servers;
        }

        /// <summary>
        /// The MemcachedClient client instance.
        /// </summary>
        private IProtocolImplementation ProtocolImpl;
        private DefaultServerPool Servers;

        #endregion

        #region current instance state, SetResult

        /// <summary>
        /// The result of the last executed Memcached method. 
        /// </summary>
        private ResConstants lastResult = ResConstants.Success;

        /// <summary>
        /// List (FIFO) of delayed results. Filled by getDelayedByKey(), peeked by fetch().
        /// </summary>
        private LinkedList<KeyValuePair<string, ResultObj>>/*!*/delayedResults = new LinkedList<KeyValuePair<string, ResultObj>>();

        /// <summary>
        /// Not null and not empty, if the client is persistent.
        /// </summary>
        private string PersistentId = null;

        /// <summary>
        /// Indicates if the memcached object is persistent.
        /// </summary>
        private bool IsPersistent
        {
            get
            {
                return !string.IsNullOrEmpty(PersistentId);
            }
        }

        /// <summary>
        /// Dictionary of persistent MemcachedClients. List initialized on the first use.
        /// Thread safe.
        /// </summary>
        private static Dictionary<string, ClientState> PersistentClients
        {
            get
            {
                if (_persistentClients == null)
                {   // double checked lock
                    lock (_locker)
                    {
                        if (_persistentClients == null)
                            _persistentClients = new Dictionary<string, ClientState>();
                    }
                }

                return _persistentClients;
            }
        }
        private static Dictionary<string, ClientState> _persistentClients = null;

        /// <summary>
        /// The static locker object.
        /// </summary>
        private static object _locker = new object();

        /// <summary>
        /// Sets the current lastResult and check if it succeeded.
        /// </summary>
        /// <param name="rescode"></param>
        /// <returns>True if rescode is Success.</returns>
        private bool SetResult(ResConstants rescode)
        {
            lastResult = (ResConstants)rescode;

            return (lastResult == ResConstants.Success);
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// Dispose memcached by disposing underlaying protocol.
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    if (!IsPersistent)
                    {
                        if (ProtocolImpl != null)
                            ProtocolImpl.Dispose();
                    }
                }
                finally
                {

                }
            }

            base.Dispose(disposing);
        }

        #endregion

        #region __construct, initialize

        /// <summary>
        /// Creates a Memcached instance representing the connection to the memcache servers. 
        /// </summary>
        public Memcached()
            : this(string.Empty) { }

        /// <summary>
        /// Creates a Memcached instance representing the connection to the memcache servers. 
        /// </summary>
        /// <param name="persistent_id">
        /// By default the Memcached instances are destroyed at the end of the request.
        /// To create an instance that persists between requests, use persistent_id  to specify
        /// a unique ID for the instance. All instances created with the same persistent_id
        /// will share the same connection.</param>
        internal Memcached(string persistent_id)
            : base(ScriptContext.CurrentContext, true)
        {
            __construct(persistent_id);
        }

        /// <summary>
        /// PHP ctor.
        /// </summary>
        /// <param name="persistent_id"></param>
        [PhpVisible, PhpFinal, ImplementsMethod]
        public void __construct([Optional] string persistent_id /* = "" */)
        {
            this.PersistentId = persistent_id;

            //
            ClientState state;

            if (string.IsNullOrEmpty(persistent_id) || !PersistentClients.TryGetValue(persistent_id, out state))
            {
                // create new client and remember its instance by the persistent_id
                StorePersistentClient(state = CreateNewClient(false, null), persistent_id);
            }

            // save state
            this.ProtocolImpl = state.ProtocolImpl;
            this.Servers = state.Servers;            
        }

        /// <summary>
        /// Create new protocol implementation based on the given arugment.
        /// </summary>
        /// <param name="binary"></param>
        /// <param name="servers"></param>
        /// <returns></returns>
        private static IProtocolImplementation CreateProtocolImplementation(bool binary, IServerPool servers)
        {
            if (binary)
                return new Enyim.Caching.Memcached.Operations.Binary.BinaryProtocol(servers);
            else
                return new Enyim.Caching.Memcached.Operations.Text.TextProtocol(servers);
        }

        /// <summary>
        /// Store given client state into the persistent cache.
        /// </summary>
        /// <param name="state"></param>
        /// <param name="persistentId"></param>
        private static void StorePersistentClient( ClientState state, string persistentId )
        {
            if (!string.IsNullOrEmpty(persistentId))
                PersistentClients[persistentId] = state;
        }

        /// <summary>
        /// Create new MemcachedClient object instance with default settings (as it is in PHP).
        /// </summary>
        /// <returns>New MemcachedClient instance.</returns>
        private ClientState CreateNewClient(bool binary, DefaultServerPool serverPool)
        {
            MemcachedClientConfiguration config = new MemcachedClientConfiguration();

            // TODO: setup config

            DefaultServerPool newpool = serverPool ?? new DefaultServerPool(config, new DefaultKeyTransformer(), /*new PhpTranscoder(),*/ null); // pool of servers, used then by addServer, addServers, getServerList, getServerByKey, ...

            newpool.Transcoder.Compression = true; // default option

            ClientState state = new ClientState();
            state.ProtocolImpl = CreateProtocolImplementation(binary, newpool);
            state.Servers = newpool;
            return state;
        }

        #endregion

        #region add, addByKey, set, setByKey, cas, casByKey, replace, replaceByKey, delete, deleteByKey

        /// <summary>
        /// Memcached::add() is similar to Memcached::set, but the operation fails if the key  already exists on the server. 
        /// </summary>
        /// <param name="key">The key under which to store the value. </param>
        /// <param name="value">The value to store. </param>
        /// <returns>Returns TRUE on success or FALSE on failure. The Memcached::getResultCode will return Memcached::RES_NOTSTORED if the key already exists.</returns>
        //[PhpVisible, ImplementsMethod]
        public bool add(string key, object value) { return addByKey(null, key, value, 0); }
        /// <summary>
        /// Memcached::add() is similar to Memcached::set, but the operation fails if the key  already exists on the server. 
        /// </summary>
        /// <param name="key">The key under which to store the value. </param>
        /// <param name="value">The value to store. </param>
        /// <param name="expiration">The expiration time, defaults to 0. See Expiration Times for more info. </param>
        /// <returns>Returns TRUE on success or FALSE on failure. The Memcached::getResultCode will return Memcached::RES_NOTSTORED if the key already exists.</returns>
        [PhpVisible, ImplementsMethod]
        public bool add(string key, object value, int expiration) { return addByKey(null, key, value, expiration); }
        
        /// <summary>
        /// Add an item under a new key on a specific server
        /// Memcached::addByKey() is functionally equivalent to Memcached::add,
        /// except that the free-form server_key  can be used to map the key
        /// to a specific server. This is useful if you need to keep a bunch
        /// of related keys on a certain server. 
        /// </summary>
        /// <param name="server_key">The key identifying the server to store the value on. </param>
        /// <param name="key">The key under which to store the value. </param>
        /// <param name="value">The value to store. </param>
        /// <returns>Returns TRUE on success or FALSE on failure. The Memcached::getResultCode will return Memcached::RES_NOTSTORED if the key already exists. </returns>
        [PhpVisible, ImplementsMethod]
        public bool addByKey(string server_key, string key, object value) { return addByKey(server_key, key, value, 0); }
        /// <summary>
        /// Add an item under a new key on a specific server
        /// Memcached::addByKey() is functionally equivalent to Memcached::add,
        /// except that the free-form server_key  can be used to map the key
        /// to a specific server. This is useful if you need to keep a bunch
        /// of related keys on a certain server. 
        /// </summary>
        /// <param name="server_key">The key identifying the server to store the value on. </param>
        /// <param name="key">The key under which to store the value. </param>
        /// <param name="value">The value to store. </param>
        /// <param name="expiration">The expiration time, defaults to 0. See Expiration Times for more info. </param>
        /// <returns>Returns TRUE on success or FALSE on failure. The Memcached::getResultCode will return Memcached::RES_NOTSTORED if the key already exists. </returns>
        [PhpVisible, ImplementsMethod]
        public bool addByKey(string server_key, string key, object value, int expiration)
        {
            if (expiration < 0)
                return SetResult(ResConstants.NotStored);

            return SetResult(ProtocolImpl.Store(StoreMode.Add, server_key, key, value, 0, (uint)expiration));
        }

        /// <summary>
        /// Memcached::cas() performs a "check and set" operation, so that the item
        /// will be stored only if no other client has updated it since it was last
        /// fetched by this client. The check is done via the cas_token  parameter which
        /// is a unique 64-bit value assigned to the existing item by memcache. See the documentation
        /// for Memcached::get* methods for how to obtain this token. Note that the token is
        /// represented as a double due to the limitations of PHP's integer space. 
        /// </summary>
        /// <param name="cas_token">Unique value associated with the existing item. Generated by memcache. </param>
        /// <param name="key">The key under which to store the value. </param>
        /// <param name="value">The value to store. </param>
        /// <returns>Returns TRUE on success or FALSE on failure. The Memcached::getResultCode will
        /// return Memcached::RES_DATA_EXISTS if the item you are trying to store has been modified since you last fetched it. </returns>
        [PhpVisible, ImplementsMethod]
        public bool cas(double cas_token, string key, object value)
        {
            return casByKey(cas_token, null, key, value, 0);
        }

        /// <summary>
        /// Memcached::cas() performs a "check and set" operation, so that the item
        /// will be stored only if no other client has updated it since it was last
        /// fetched by this client. The check is done via the cas_token  parameter which
        /// is a unique 64-bit value assigned to the existing item by memcache. See the documentation
        /// for Memcached::get* methods for how to obtain this token. Note that the token is
        /// represented as a double due to the limitations of PHP's integer space. 
        /// </summary>
        /// <param name="cas_token">Unique value associated with the existing item. Generated by memcache. </param>
        /// <param name="key">The key under which to store the value. </param>
        /// <param name="value">The value to store. </param>
        /// <param name="expiration">The expiration time, defaults to 0. See Expiration Times for more info. </param>
        /// <returns>Returns TRUE on success or FALSE on failure. The Memcached::getResultCode will
        /// return Memcached::RES_DATA_EXISTS if the item you are trying to store has been modified since you last fetched it. </returns>
        [PhpVisible, ImplementsMethod]
        public bool cas(double cas_token, string key, object value, int expiration)
        {
            return casByKey(cas_token, null, key, value, expiration);
        }

        /// <summary>
        /// Memcached::casByKey() is functionally equivalent to Memcached::cas, except that the free-form server_key
        /// can be used to map the key  to a specific server. This is useful if you need to keep a bunch o
        /// f related keys on a certain server. 
        /// </summary>
        /// <param name="cas_token">Unique value associated with the existing item. Generated by memcache. </param>
        /// <param name="server_key">The key identifying the server to store the value on. </param>
        /// <param name="key">The key under which to store the value. </param>
        /// <param name="value">The value to store. </param>
        /// <returns>Returns TRUE on success or FALSE on failure. The Memcached::getResultCode will return Memcached::RES_DATA_EXISTS
        /// if the item you are trying to store has been modified since you last fetched it. </returns>
        [PhpVisible, ImplementsMethod]
        public bool casByKey(double cas_token, string server_key, string key, object value)
        {
            return casByKey(cas_token, server_key, key, value, 0);
        }

        /// <summary>
        /// Memcached::casByKey() is functionally equivalent to Memcached::cas, except that the free-form server_key
        /// can be used to map the key  to a specific server. This is useful if you need to keep a bunch o
        /// f related keys on a certain server. 
        /// </summary>
        /// <param name="cas_token">Unique value associated with the existing item. Generated by memcache. </param>
        /// <param name="server_key">The key identifying the server to store the value on. </param>
        /// <param name="key">The key under which to store the value. </param>
        /// <param name="value">The value to store. </param>
        /// <param name="expiration">The expiration time, defaults to 0. See Expiration Times for more info. </param>
        /// <returns>Returns TRUE on success or FALSE on failure. The Memcached::getResultCode will return Memcached::RES_DATA_EXISTS
        /// if the item you are trying to store has been modified since you last fetched it. </returns>
        [PhpVisible, ImplementsMethod]
        public bool casByKey(double cas_token, string server_key, string key, object value, int expiration)
        {
            if (expiration < 0)
                return SetResult(ResConstants.NotStored);

            return SetResult(ProtocolImpl.Store(StoreMode.Set, server_key, key, value, (ulong)cas_token, (uint)expiration));
        }

        /// <summary>
        /// Memcached::set() stores the value  on a memcache server under the specified key .
        /// The expiration  parameter can be used to control when the value is considered expired.
        ///  
        /// The value can be any valid PHP type except for resources, because those cannot be
        /// represented in a serialized form. If the Memcached::OPT_COMPRESSION option is turned on,
        /// the serialized value will also be compressed before storage. 
        /// </summary>
        /// <param name="key">The key under which to store the value. </param>
        /// <param name="value">The value to store. </param>
        /// <returns>Returns TRUE on success or FALSE on failure. Use Memcached::getResultCode if necessary. </returns>
        [PhpVisible, ImplementsMethod]
        public bool set(string key, object value) { return setByKey(null, key, value, 0); }
        /// <summary>
        /// Memcached::set() stores the value  on a memcache server under the specified key .
        /// The expiration  parameter can be used to control when the value is considered expired.
        ///  
        /// The value can be any valid PHP type except for resources, because those cannot be
        /// represented in a serialized form. If the Memcached::OPT_COMPRESSION option is turned on,
        /// the serialized value will also be compressed before storage. 
        /// </summary>
        /// <param name="key">The key under which to store the value. </param>
        /// <param name="value">The value to store. </param>
        /// <param name="expiration">The expiration time, defaults to 0. See Expiration Times for more info. </param>
        /// <returns>Returns TRUE on success or FALSE on failure. Use Memcached::getResultCode if necessary. </returns>
        [PhpVisible, ImplementsMethod]
        public bool set(string key, object value, int expiration) { return setByKey(null, key, value, expiration); }

        /// <summary>
        /// Memcached::setByKey() is functionally equivalent to Memcached::set,
        /// except that the free-form server_key  can be used to map the key  to a specific server.
        /// This is useful if you need to keep a bunch of related keys on a certain server. 
        /// </summary>
        /// <param name="server_key">The key identifying the server to store the value on. </param>
        /// <param name="key">The key under which to store the value. </param>
        /// <param name="value">The value to store. </param>
        /// <returns>Returns TRUE on success or FALSE on failure. Use Memcached::getResultCode if necessary. </returns>
        [PhpVisible, ImplementsMethod]
        public bool setByKey(string server_key, string key, object value) { return setByKey(server_key, key, value, 0); }
        /// <summary>
        /// Memcached::setByKey() is functionally equivalent to Memcached::set,
        /// except that the free-form server_key  can be used to map the key  to a specific server.
        /// This is useful if you need to keep a bunch of related keys on a certain server. 
        /// </summary>
        /// <param name="server_key">The key identifying the server to store the value on. </param>
        /// <param name="key">The key under which to store the value. </param>
        /// <param name="value">The value to store. </param>
        /// <param name="expiration">The expiration time, defaults to 0. See Expiration Times for more info. </param>
        /// <returns>Returns TRUE on success or FALSE on failure. Use Memcached::getResultCode if necessary. </returns>
        [PhpVisible, ImplementsMethod]
        public bool setByKey(string server_key, string key, object value, int expiration)
        {
            if (expiration < 0)
                return SetResult(ResConstants.Success);

            //PhpException.Throw(PhpError.Notice, "DEBUG: setByKey begin, protocol " + ProtocolImpl.ToString());

            return SetResult(ProtocolImpl.Store(StoreMode.Set, server_key, key, value, 0, (uint)expiration));
        }

        /// <summary>
        /// Memcached::replace() is similar to Memcached::set, but the operation fails if the key  does not exist on the server. 
        /// </summary>
        /// <param name="key">The key under which to store the value. </param>
        /// <param name="value">The value to store. </param>
        /// <returns>Returns TRUE on success or FALSE on failure.
        /// The Memcached::getResultCode will return Memcached::RES_NOTSTORED if the key does not exist. </returns>
        [PhpVisible, ImplementsMethod]
        public bool replace(string key, object value) { return replaceByKey(null, key, value, 0); }
        /// <summary>
        /// Memcached::replace() is similar to Memcached::set, but the operation fails if the key  does not exist on the server. 
        /// </summary>
        /// <param name="key">The key under which to store the value. </param>
        /// <param name="value">The value to store. </param>
        /// <param name="expiration">The expiration time, defaults to 0. See Expiration Times for more info. </param>
        /// <returns>Returns TRUE on success or FALSE on failure.
        /// The Memcached::getResultCode will return Memcached::RES_NOTSTORED if the key does not exist. </returns>
        [PhpVisible, ImplementsMethod]
        public bool replace(string key, object value, int expiration) { return replaceByKey(null, key, value, expiration); }


        /// <summary>
        /// Memcached::replace() is similar to Memcached::set, but the operation fails if the key  does not exist on the server. 
        /// </summary>
        /// <param name="server_key">The key to locate the server node.</param>
        /// <param name="key">The key under which to store the value. </param>
        /// <param name="value">The value to store. </param>
        /// <returns>Returns TRUE on success or FALSE on failure.
        /// The Memcached::getResultCode will return Memcached::RES_NOTSTORED if the key does not exist. </returns>
        [PhpVisible, ImplementsMethod]
        public bool replaceByKey(string server_key, string key, object value) { return replaceByKey(server_key, key, value, 0); }

        /// <summary>
        /// Memcached::replace() is similar to Memcached::set, but the operation fails if the key  does not exist on the server. 
        /// </summary>
        /// <param name="server_key">The key to locate the server node.</param>
        /// <param name="key">The key under which to store the value. </param>
        /// <param name="value">The value to store. </param>
        /// <param name="expiration">The expiration time, defaults to 0. See Expiration Times for more info. </param>
        /// <returns>Returns TRUE on success or FALSE on failure.
        /// The Memcached::getResultCode will return Memcached::RES_NOTSTORED if the key does not exist. </returns>
        [PhpVisible, ImplementsMethod]
        public bool replaceByKey(string server_key, string key, object value, int expiration)
        {
            if (expiration < 0)
                return SetResult(ResConstants.NotStored);

            return SetResult(ProtocolImpl.Store(StoreMode.Replace, server_key, key, value, 0, (uint)expiration));
        }

        /// <summary>
        /// Memcached::delete() deletes the key  from the server. The time  parameter is the amount
        /// of time in seconds (or Unix time until which) the client wishes the server to refuse add
        /// and replace commands for this key. For this amount of time, the item is put into a delete
        /// queue, which means that it won't possible to retrieve it by the get command, but add and
        /// replace command with this key will also fail (the set command will succeed, however). After
        /// the time passes, the item is finally deleted from server memory. The parameter time 
        /// defaults to 0 (which means that the item will be deleted immediately and further storage
        /// commands with this key will succeed). 
        /// </summary>
        /// <param name="key">The key to be deleted. </param>
        /// <returns>Returns TRUE on success or FALSE on failure.
        /// The Memcached::getResultCode will return Memcached::RES_NOTFOUND if the key does not exist. </returns>
        [PhpVisible, ImplementsMethod]
        public bool delete(string key) { return deleteByKey(null, key, 0); }

        /// <summary>
        /// Memcached::delete() deletes the key  from the server. The time  parameter is the amount
        /// of time in seconds (or Unix time until which) the client wishes the server to refuse add
        /// and replace commands for this key. For this amount of time, the item is put into a delete
        /// queue, which means that it won't possible to retrieve it by the get command, but add and
        /// replace command with this key will also fail (the set command will succeed, however). After
        /// the time passes, the item is finally deleted from server memory. The parameter time 
        /// defaults to 0 (which means that the item will be deleted immediately and further storage
        /// commands with this key will succeed). 
        /// </summary>
        /// <param name="key">The key to be deleted. </param>
        /// <param name="time">The amount of time the server will wait to delete the item. </param>
        /// <returns>Returns TRUE on success or FALSE on failure.
        /// The Memcached::getResultCode will return Memcached::RES_NOTFOUND if the key does not exist. </returns>
        [PhpVisible, ImplementsMethod]
        public bool delete(string key, int time) { return deleteByKey(null, key, time); }

        /// <summary>
        /// Memcached::deleteByKey() is functionally equivalent to Memcached::delete, except that
        /// the free-form server_key  can be used to map the key  to a specific server. 
        /// </summary>
        /// <param name="server_key">The key identifying the server to store the value on. </param>
        /// <param name="key">The key to be deleted. </param>
        /// <returns>Returns TRUE on success or FALSE on failure.
        /// The Memcached::getResultCode will return Memcached::RES_NOTFOUND if the key does not exist. </returns>
        [PhpVisible, ImplementsMethod]
        public bool deleteByKey(string server_key, string key) { return deleteByKey(server_key, key, 0); }
        /// <summary>
        /// Memcached::deleteByKey() is functionally equivalent to Memcached::delete, except that
        /// the free-form server_key  can be used to map the key  to a specific server. 
        /// </summary>
        /// <param name="server_key">The key identifying the server to store the value on. </param>
        /// <param name="key">The key to be deleted. </param>
        /// <param name="time">The amount of time the server will wait to delete the item. </param>
        /// <returns>Returns TRUE on success or FALSE on failure.
        /// The Memcached::getResultCode will return Memcached::RES_NOTFOUND if the key does not exist. </returns>
        [PhpVisible, ImplementsMethod]
        public bool deleteByKey(string server_key, string key, int time)
        {
            return SetResult(ProtocolImpl.Remove(server_key, key, time));
        }

        #endregion

        #region addServer, addServers, getServerByKey, getServerList

        /// <summary>
        /// Memcached::addServer() adds the specified server to the server pool.
        /// No connection is established to the server at this time, but if you are
        /// using consistent key distribution option (via Memcached::DISTRIBUTION_CONSISTENT
        /// or Memcached::OPT_LIBKETAMA_COMPATIBLE), some of the internal data structures
        /// will have to be updated. Thus, if you need to add multiple servers, it is better
        /// to use Memcached::addServers as the update then happens only once.
        ///  
        /// The same server may appear multiple times in the server pool, because no duplication
        /// checks are made. This is not advisable; instead, use the weight option to increase
        /// the selection weighting of this server. 
        /// </summary>
        /// <param name="host">The hostname of the memcache server.
        /// If the hostname is invalid, data-related operations will set
        /// Memcached::RES_HOST_LOOKUP_FAILURE  result code. </param>
        /// <param name="port">The port on which memcache is running. Usually, this is 11211. </param>
        /// <returns>Returns TRUE on success or FALSE on failure. </returns>
        [PhpVisible, ImplementsMethod]
        public bool addServer(string host, int port) { return addServer(host, port, 0); }
        /// <summary>
        /// Memcached::addServer() adds the specified server to the server pool.
        /// No connection is established to the server at this time, but if you are
        /// using consistent key distribution option (via Memcached::DISTRIBUTION_CONSISTENT
        /// or Memcached::OPT_LIBKETAMA_COMPATIBLE), some of the internal data structures
        /// will have to be updated. Thus, if you need to add multiple servers, it is better
        /// to use Memcached::addServers as the update then happens only once.
        ///  
        /// The same server may appear multiple times in the server pool, because no duplication
        /// checks are made. This is not advisable; instead, use the weight option to increase
        /// the selection weighting of this server. 
        /// </summary>
        /// <param name="host">The hostname of the memcache server.
        /// If the hostname is invalid, data-related operations will set
        /// Memcached::RES_HOST_LOOKUP_FAILURE  result code. </param>
        /// <param name="port">The port on which memcache is running. Usually, this is 11211. </param>
        /// <param name="weight">The weight of the server relative to the total weight of all
        /// the servers in the pool. This controls the probability of the server being selected
        /// for operations. This is used only with consistent distribution option and usually
        /// corresponds to the amount of memory available to memcache on that server. </param>
        /// <returns>Returns TRUE on success or FALSE on failure. </returns>
        [PhpVisible, ImplementsMethod]
        public bool addServer(string host, int port, int weight/* =0 */)
        {
            lastResult = ResConstants.Success;

            try
            {
                Servers.AddServers(new NamedIPEndPoint[] { new NamedIPEndPoint(host, port, weight) });
            }
            catch
            {
                lastResult = ResConstants.HostLookupFailure;
                return false;
            }


            return true;
        }

        /// <summary>
        /// Memcached::addServers() adds servers  to the server pool. Each entry in servers
        /// is supposed to an array containing hostname, port, and, optionally, weight of
        /// the server. No connection is established to the servers at this time.
        /// 
        /// The same server may appear multiple times in the server pool, because no duplication
        /// checks are made. This is not advisable; instead, use the weight option to increase
        /// the selection weighting of this server. 
        /// </summary>
        /// <param name="servers">Array of the servers to add to the pool. Each server is an array {host, port[, weight]}</param>
        /// <returns>Returns TRUE on success or FALSE on failure. </returns>
        [PhpVisible, ImplementsMethod]
        public bool addServers(PhpArray servers)
        {
            lastResult = ResConstants.Success;

            if (servers == null)
                return true;

            List<NamedIPEndPoint> endppoints = new List<NamedIPEndPoint>();

            foreach (var x in servers.Values)
            {
                string host = null;
                int port = 11211;
                int weight = 0;

                PhpArray arr;
                
                // { host, port[, weight] }
                if ((arr = x as PhpArray) != null)
                {
                    host = PhpVariable.AsString(arr.GetArrayItem(0, true));
                    
                    object portobj = arr.GetArrayItem(1, true);
                    if (portobj is int)
                        port = (int)portobj;
                    else
                        return false;

                    object weightobj = arr.GetArrayItem(2, true);//can be null
                    weight = (weightobj != null && weightobj is int) ? (int)weightobj : 0;
                    
                    
                }
                // "host:port"
                else if ( (host = PhpVariable.AsString(x)) != null )
                {
                    string[] addr = host.Split(new char[] { ':' });

                    if (addr == null || addr.Length != 2)
                        return false;   // wrong format

                    host = addr[0];
                    if (!int.TryParse(addr[1], out port))
                        return false;   // wrong format
                }

                // add to the collection
                if (host != null)
                {
                    try { endppoints.Add(new NamedIPEndPoint(host, port, weight)); }
                    catch
                    {
                        // unable to resolve
                        lastResult = ResConstants.HostLookupFailure;
                        return false;
                    }
                }
                else
                {
                    // wrong format
                    lastResult = ResConstants.Failure;
                    return false;
                }
            }

            Servers.AddServers(endppoints); // add collection of IPEndPoints

            return true;
        }

        /// <summary>
        /// Memcached::getServerByKey() returns the server that would be selected by
        /// a particular server_key  in all the Memcached::*ByKey() operations. 
        /// </summary>
        /// <param name="server_key">The key identifying the server to store the value on. </param>
        /// <returns>Returns TRUE on success or FALSE on failure. Use Memcached::getResultCode if necessary. </returns>
        [PhpVisible, ImplementsMethod]
        [return: CastToFalse()]
        public PhpArray getServerByKey(string server_key)
        {
            var kt = Servers.KeyTransformer;
            var locator = Servers.NodeLocator;

            var node = locator.Locate(kt.Transform(server_key));

            return GetServerPhpInfo(node);
        }
        /// <summary>
        /// Memcached::getServerList() returns the list of all servers that are in its server pool.
        /// </summary>
        /// <returns>The list of all servers in the server pool.</returns>
        [PhpVisible, ImplementsMethod]
        [return: CastToFalse()]
        public PhpArray getServerList()
        {
            PhpArray result = new PhpArray();
            int i = 0;

            foreach (var x in Servers.WorkingServers)
            {
                result.Add(i, GetServerPhpInfo(x));

                ++i;
            }

            return result;
        }

        private PhpArray GetServerPhpInfo(IMemcachedNode node)
        {
            if (node == null)
                return null;

            PhpArray serverinfo = new PhpArray();

            serverinfo.Add("host", node.EndPoint.HostName);
            serverinfo.Add("port", node.EndPoint.Port);
            serverinfo.Add("weight", node.EndPoint.Weight);

            return serverinfo;
        }

        #endregion

        #region append, appendByKey, prepend, prependByKey

        /// <summary>
        /// Memcached::append() appends the given value  string to the value of
        /// an existing item. The reason that value  is forced to be a string is
        /// that appending mixed types is not well-defined.
        /// 
        /// Note: If the Memcached::OPT_COMPRESSION is enabled, the operation
        /// will fail and a warning will be issued, because appending compressed
        /// data to a value that is potentially already compressed is not possible. 
        /// </summary>
        /// <param name="key">The key under which to store the value. </param>
        /// <param name="value">The string to append. </param>
        /// <returns>Returns TRUE on success or FALSE on failure. The Memcached::getResultCode will return Memcached::RES_NOTSTORED if the key does not exist. </returns>
        [PhpVisible, ImplementsMethod]
        public bool append(string key, string value)
        {
            return appendByKey(null, key, value);
        }

        /// <summary>
        /// Memcached::appendByKey() is functionally equivalent to Memcached::append, except that
        /// the free-form server_key  can be used to map the key  to a specific server. 
        /// </summary>
        /// <param name="server_key">The key identifying the server to store the value on. </param>
        /// <param name="key">The key under which to store the value. </param>
        /// <param name="value">The string to append. </param>
        /// <returns>Returns TRUE on success or FALSE on failure. The Memcached::getResultCode will return Memcached::RES_NOTSTORED if the key does not exist. </returns>
        [PhpVisible, ImplementsMethod]
        public bool appendByKey(string server_key, string key, string value)
        {
            return SetResult(ProtocolImpl.Concatenate(ConcatenationMode.Append, server_key, key, new ArraySegment<byte>(PhpVariable.AsBytes(value).Data)));
        }

        /// <summary>
        /// Memcached::prepend() prepends the given value  string to the value of an existing item. The reason that value
        /// is forced to be a string is that prepending mixed types is not well-defined.
        ///  
        /// Note: If the Memcached::OPT_COMPRESSION is enabled, the operation will fail and a warning
        /// will be issued, because prepending compressed data to a value that is potentially already compressed is not possible. 
        /// </summary>
        /// <param name="key">The key of the item to prepend the data to. </param>
        /// <param name="value">The string to prepend. </param>
        /// <returns>Returns TRUE on success or FALSE on failure. The Memcached::getResultCode will return Memcached::RES_NOTSTORED if the key does not exist. </returns>
        [PhpVisible, ImplementsMethod]
        public bool prepend(string key, string value)
        {
            return prependByKey(null, key, value);
        }

        /// <summary>
        /// Memcached::prependByKey() is functionally equivalent to Memcached::prepend,
        /// except that the free-form server_key  can be used to map the key  to a specific server. 
        /// </summary>
        /// <param name="server_key">The key identifying the server to store the value on. </param>
        /// <param name="key">The key of the item to prepend the data to. </param>
        /// <param name="value">The string to prepend. </param>
        /// <returns>Returns TRUE on success or FALSE on failure.
        /// The Memcached::getResultCode will return Memcached::RES_NOTSTORED if the key does not exist. </returns>
        [PhpVisible, ImplementsMethod]
        public bool prependByKey(string server_key, string key, string value)
        {
            return SetResult(ProtocolImpl.Concatenate(ConcatenationMode.Prepend, server_key, key, new ArraySegment<byte>(PhpVariable.AsBytes(value).Data)));
        }

        #endregion

        #region increment, decrement

        /// <summary>
        /// Memcached::decrement() decrements a numeric item's value by the specified offset .
        /// If the item's value is not numeric, it is treated as if the value were 0. If the operation
        /// would decrease the value below 0, the new value will be 0. Memcached::decrement() will
        /// fail if the item does not exist. 
        /// </summary>
        /// <param name="key">The key of the item to decrement. </param>
        /// <returns>Returns item's new value on success or FALSE  on failure.
        /// The Memcached::getResultCode will return Memcached::RES_NOTFOUND if the key does not exist. </returns>
        [PhpVisible, ImplementsMethod]
        [return: CastToFalse()]
        public int decrement(string key) { return decrement(key, 1); }

        /// <summary>
        /// Memcached::decrement() decrements a numeric item's value by the specified offset .
        /// If the item's value is not numeric, it is treated as if the value were 0. If the operation
        /// would decrease the value below 0, the new value will be 0. Memcached::decrement() will
        /// fail if the item does not exist. 
        /// </summary>
        /// <param name="key">The key of the item to decrement. </param>
        /// <param name="offset">The amount by which to decrement the item's value. </param>
        /// <returns>Returns item's new value on success or FALSE  on failure.
        /// The Memcached::getResultCode will return Memcached::RES_NOTFOUND if the key does not exist. </returns>
        [PhpVisible, ImplementsMethod]
        [return: CastToFalse()]
        public int decrement(string key, int offset)
        {
            if (offset < 0)
                return -1;

            ulong newval;
            if (SetResult(ProtocolImpl.Mutate(MutationMode.Decrement, null, key, ulong.MaxValue/*not used*/, (ulong)offset, false, out newval)))
                return (int)newval;
            else
                return -1;
        }

        /// <summary>
        /// Memcached::increment() increments a numeric item's value by the specified offset . If the item's value
        /// is not numeric, it is treated as if the value were 0. Memcached::increment() will fail if the item does not exist. 
        /// </summary>
        /// <param name="key">The key of the item to increment. </param>
        /// <returns>Returns new item's value on success or FALSE  on failure.
        /// The Memcached::getResultCode will return Memcached::RES_NOTFOUND if the key does not exist. </returns>
        [PhpVisible, ImplementsMethod]
        [return: CastToFalse()]
        public int increment(string key) { return increment(key, 1); }
        /// <summary>
        /// Memcached::increment() increments a numeric item's value by the specified offset . If the item's value
        /// is not numeric, it is treated as if the value were 0. Memcached::increment() will fail if the item does not exist. 
        /// </summary>
        /// <param name="key">The key of the item to increment. </param>
        /// <param name="offset">The amount by which to increment the item's value. </param>
        /// <returns>Returns new item's value on success or FALSE  on failure.
        /// The Memcached::getResultCode will return Memcached::RES_NOTFOUND if the key does not exist. </returns>
        [PhpVisible, ImplementsMethod]
        [return: CastToFalse()]
        public int increment(string key, int offset)
        {
            if (offset < 0)
                return -1;

            ulong newval;
            if (SetResult(ProtocolImpl.Mutate(MutationMode.Increment, null, key, ulong.MinValue/*not used*/, (ulong)offset, false, out newval)))
                return (int)newval;
            else
                return -1;
        }
        
        #endregion

        #region flush

        /// <summary>
        /// Memcached::flush() invalidates all existing cache items immediately (by default)
        /// or after the delay  specified. After invalidation none of the items will be returned
        /// in response to a retrieval command (unless it's stored again under the same key
        /// after Memcached::flush() has invalidated the items). The flush does not actually free
        /// all the memory taken up by the existing items; that will happen gradually as new items are stored. 
        /// </summary>
        /// <returns>Returns TRUE on success or FALSE on failure. Use Memcached::getResultCode if necessary. </returns>
        [PhpVisible, ImplementsMethod]
        public bool flush() { return flush(0); }

        /// <summary>
        /// Memcached::flush() invalidates all existing cache items immediately (by default)
        /// or after the delay  specified. After invalidation none of the items will be returned
        /// in response to a retrieval command (unless it's stored again under the same key
        /// after Memcached::flush() has invalidated the items). The flush does not actually free
        /// all the memory taken up by the existing items; that will happen gradually as new items are stored. 
        /// </summary>
        /// <param name="delay">Number of seconds to wait before invalidating the items. </param>
        /// <returns>Returns TRUE on success or FALSE on failure. Use Memcached::getResultCode if necessary. </returns>
        [PhpVisible, ImplementsMethod]
        public bool flush(int delay)
        {
            ProtocolImpl.FlushAll(delay);

            lastResult = ResConstants.Success;
            return true;
        }

        #endregion

        #region get, getByKey

        /// <summary>
        /// Memcached::get() returns the item that was previously stored under the key .
        /// If the item is found and cas_token  variable is provided, it will contain the CAS token value
        /// for the item. See Memcached::cas  for how to use CAS tokens. Read-through caching callback may
        /// be specified via cache_cb  parameter. 
        /// </summary>
        /// <returns>Returns the value stored in the cache or FALSE  otherwise. The Memcached::getResultCode will return Memcached::RES_NOTFOUND if the key does not exist. </returns>
        [PhpVisible, ImplementsMethod]
        [return: CastToFalse()]
        public object get(string key) { return getByKey(null, key, null, null); }

        /// <summary>
        /// Memcached::get() returns the item that was previously stored under the key .
        /// If the item is found and cas_token  variable is provided, it will contain the CAS token value
        /// for the item. See Memcached::cas  for how to use CAS tokens. Read-through caching callback may
        /// be specified via cache_cb  parameter. 
        /// </summary>
        /// <param name="key">The key of the item to retrieve. </param>
        /// <param name="cache_cb">Read-through caching callback or NULL. </param>
        /// <returns>Returns the value stored in the cache or FALSE  otherwise. The Memcached::getResultCode will return Memcached::RES_NOTFOUND if the key does not exist. </returns>
        [PhpVisible, ImplementsMethod]
        [return: CastToFalse()]
        public object get(string key, PhpCallback cache_cb) { return getByKey(null, key, cache_cb, null); }

        /// <summary>
        /// Memcached::get() returns the item that was previously stored under the key .
        /// If the item is found and cas_token  variable is provided, it will contain the CAS token value
        /// for the item. See Memcached::cas  for how to use CAS tokens. Read-through caching callback may
        /// be specified via cache_cb  parameter. 
        /// </summary>
        /// <param name="key">The key of the item to retrieve. </param>
        /// <param name="cache_cb">Read-through caching callback or NULL. </param>
        /// <param name="cas_token">The variable to store the CAS token in. </param>
        /// <returns>Returns the value stored in the cache or FALSE  otherwise. The Memcached::getResultCode will return Memcached::RES_NOTFOUND if the key does not exist. </returns>
        [PhpVisible, ImplementsMethod]
        [return: CastToFalse()]
        public object get(string key, PhpCallback cache_cb, PhpReference cas_token) { return getByKey(null, key, cache_cb, cas_token); }


        /// <summary>
        /// Memcached::getByKey() is functionally equivalent to Memcached::get,
        /// except that the free-form server_key  can be used to map the key  to a specific server. 
        /// </summary>
        /// <param name="server_key">The key identifying the server to store the value on. </param>
        /// <param name="key">The key of the item to fetch. </param>
        /// <returns>Returns the value stored in the cache or FALSE  otherwise.
        /// The Memcached::getResultCode will return Memcached::RES_NOTFOUND if the key does not exist. </returns>
        [PhpVisible, ImplementsMethod]
        [return: CastToFalse()]
        public object getByKey(string server_key, string key) { return getByKey(server_key, key, null, null); }
        /// <summary>
        /// Memcached::getByKey() is functionally equivalent to Memcached::get,
        /// except that the free-form server_key  can be used to map the key  to a specific server. 
        /// </summary>
        /// <param name="server_key">The key identifying the server to store the value on. </param>
        /// <param name="key">The key of the item to fetch. </param>
        /// <param name="cache_cb">Read-through caching callback or NULL</param>
        /// <returns>Returns the value stored in the cache or FALSE  otherwise.
        /// The Memcached::getResultCode will return Memcached::RES_NOTFOUND if the key does not exist. </returns>
        [PhpVisible, ImplementsMethod]
        [return: CastToFalse()]
        public object getByKey(string server_key, string key, PhpCallback cache_cb) { return getByKey(server_key, key, cache_cb, null); }
        /// <summary>
        /// Memcached::getByKey() is functionally equivalent to Memcached::get,
        /// except that the free-form server_key  can be used to map the key  to a specific server. 
        /// </summary>
        /// <param name="server_key">The key identifying the server to store the value on. </param>
        /// <param name="key">The key of the item to fetch. </param>
        /// <param name="cache_cb">Read-through caching callback or NULL</param>
        /// <param name="cas_token">The variable to store the CAS token in. </param>
        /// <returns>Returns the value stored in the cache or FALSE  otherwise.
        /// The Memcached::getResultCode will return Memcached::RES_NOTFOUND if the key does not exist. </returns>
        [PhpVisible, ImplementsMethod]
        [return: CastToFalse()]
        public object getByKey(string server_key, string key, PhpCallback cache_cb, PhpReference cas_token)
        {
            ResultObj result;
            if (!SetResult(ProtocolImpl.TryGet(server_key, key, out result)))
                return null;

            // result not null
            Debug.Assert(result != null, "This should not be null. See protocols implementation");

            // cas_token
            if (cas_token != null)
                cas_token.Value = (double)result.cas;// according to memcached PHP implementation, cas token is just explicitly converted to double

            // cache_cb
            if (result.value == null && cache_cb != null)
            {
                PhpReference valueref = new PhpReference();

                object ret = cache_cb.Invoke(this, key, valueref);

                result.value = valueref.Value;
                //if (cas_token != null)  cas_token.Value = 0;    // clear result cas too

                // if the callback returns true, store the value onto the server automatically, with no expiration
                if (ret != null && ret is bool && (bool)ret == true && result.value != null)
                {   // store the value onto the server
                    ProtocolImpl.Store(StoreMode.Add, server_key, key, result.value, 0, 0/*no expiration, as it is in PHP implementation*/);
                }
            }

            return result.value;
        }

        #endregion

        #region getDelayed, getDelayedByKey, fetch, fetchAll

        /// <summary>
        /// Memcached::getDelayed() issues a request to memcache for multiple items the keys
        /// of which are specified in the keys  array. The method does not wait for response and
        /// returns right away. When you are ready to collect the items, call either Memcached::fetch
        /// or Memcached::fetchAll. If with_cas  is true, the CAS token values will also be requested.
        ///  
        /// Instead of fetching the results explicitly, you can specify a result callback via value_cb parameter. 
        /// </summary>
        /// <param name="keys">Array of keys to request. </param>
        /// <returns>Returns TRUE on success or FALSE on failure. Use Memcached::getResultCode if necessary. </returns>
        [PhpVisible, ImplementsMethod]
        public bool getDelayed(PhpArray keys) { return getDelayedByKey(null, keys, false, null); }
        /// <summary>
        /// Memcached::getDelayed() issues a request to memcache for multiple items the keys
        /// of which are specified in the keys  array. The method does not wait for response and
        /// returns right away. When you are ready to collect the items, call either Memcached::fetch
        /// or Memcached::fetchAll. If with_cas  is true, the CAS token values will also be requested.
        ///  
        /// Instead of fetching the results explicitly, you can specify a result callback via value_cb parameter. 
        /// </summary>
        /// <param name="keys">Array of keys to request. </param>
        /// <param name="with_cas">Whether to request CAS token values also. </param>
        /// <returns>Returns TRUE on success or FALSE on failure. Use Memcached::getResultCode if necessary. </returns>
        [PhpVisible, ImplementsMethod]
        public bool getDelayed(PhpArray keys, bool with_cas) { return getDelayedByKey(null, keys, with_cas, null); }
        /// <summary>
        /// Memcached::getDelayed() issues a request to memcache for multiple items the keys
        /// of which are specified in the keys  array. The method does not wait for response and
        /// returns right away. When you are ready to collect the items, call either Memcached::fetch
        /// or Memcached::fetchAll. If with_cas  is true, the CAS token values will also be requested.
        ///  
        /// Instead of fetching the results explicitly, you can specify a result callback via value_cb parameter. 
        /// </summary>
        /// <param name="keys">Array of keys to request. </param>
        /// <param name="with_cas">Whether to request CAS token values also. </param>
        /// <param name="value_cb">The result callback or NULL. </param>
        /// <returns>Returns TRUE on success or FALSE on failure. Use Memcached::getResultCode if necessary. </returns>
        [PhpVisible, ImplementsMethod]
        public bool getDelayed(PhpArray keys, bool with_cas, PhpCallback value_cb) { return getDelayedByKey(null, keys, with_cas, value_cb); }

        /// <summary>
        /// Memcached::getDelayedByKey() is functionally equivalent to Memcached::getDelayed, except
        /// that the free-form server_key  can be used to map the keys  to a specific server. 
        /// </summary>
        /// <param name="server_key">The key identifying the server to store the value on. </param>
        /// <param name="keys">Array of keys to request. </param>
        /// <returns>Returns TRUE on success or FALSE on failure. Use Memcached::getResultCode if necessary. </returns>
        [PhpVisible, ImplementsMethod]
        public bool getDelayedByKey(string server_key, PhpArray keys) { return getDelayedByKey(server_key, keys, false, null); }
        /// <summary>
        /// Memcached::getDelayedByKey() is functionally equivalent to Memcached::getDelayed, except
        /// that the free-form server_key  can be used to map the keys  to a specific server. 
        /// </summary>
        /// <param name="server_key">The key identifying the server to store the value on. </param>
        /// <param name="keys">Array of keys to request. </param>
        /// <param name="with_cas">Whether to request CAS token values also. </param>
        /// <returns>Returns TRUE on success or FALSE on failure. Use Memcached::getResultCode if necessary. </returns>
        [PhpVisible, ImplementsMethod]
        public bool getDelayedByKey(string server_key, PhpArray keys, bool with_cas) { return getDelayedByKey(server_key, keys, with_cas, null); }
        /// <summary>
        /// Memcached::getDelayedByKey() is functionally equivalent to Memcached::getDelayed, except
        /// that the free-form server_key  can be used to map the keys  to a specific server. 
        /// </summary>
        /// <param name="server_key">The key identifying the server to store the value on. </param>
        /// <param name="keys">Array of keys to request. </param>
        /// <param name="with_cas">Whether to request CAS token values also. </param>
        /// <param name="value_cb">The result callback or NULL. </param>
        /// <returns>Returns TRUE on success or FALSE on failure. Use Memcached::getResultCode if necessary. </returns>
        [PhpVisible, ImplementsMethod]
        public bool getDelayedByKey(string server_key, PhpArray keys, bool with_cas, PhpCallback value_cb)
        {
            delayedResults.Clear(); // discard previous results if any

            var values = getMultiInternal(server_key, keys, GetConstants.PreserveOrder);    // get items, keep ordering of keys
            if (values == null)
                return false;

            if (value_cb != null)
            {
                // call callback for every item
                foreach (var x in values)
                {
                    try { value_cb.Invoke(this, getFetchedItem(x.Key, x.Value, with_cas)); }
                    catch { lastResult = ResConstants.Failure; break; }                    
                }
            }
            else
            {
                // add every item into the buffer (delayedResults)
                foreach (var x in values)
                {
                    if (!with_cas)
                        x.Value.cas = 0;// ignore the cas
                    
                    delayedResults.AddLast(x);
                }
            }

            //
            return (lastResult == ResConstants.Success);
        }

        /// <summary>
        /// Internal fetch method, fetches items from provided (key;value) dictionary.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="withCas"></param>
        internal static PhpArray getFetchedItem(string key, ResultObj value, bool withCas)
        {
            PhpArray item = new PhpArray();

            item.Add("key", key);
            item.Add("value", value.value);

            if (withCas) // cas was required
                item.Add("cas", (double)value.cas);

            return item;
        }

        /// <summary>
        /// Memcached::fetch() retrieves the next result from the last request. 
        /// </summary>
        /// <returns>Returns the next result or FALSE otherwise.
        /// The Memcached::getResultCode will return Memcached::RES_END if result set is exhausted. </returns>
        [PhpVisible, ImplementsMethod]
        [return: CastToFalse()]
        public PhpArray fetch()
        {
            if (delayedResults.Count == 0)
            {
                lastResult = ResConstants.End;
                return null;
            }

            var item = delayedResults.First;
            delayedResults.RemoveFirst();

            return getFetchedItem(item.Value.Key, item.Value.Value, item.Value.Value.cas != 0);
        }
        /// <summary>
        /// Memcached::fetchAll() retrieves all the remaining results from the last request. 
        /// </summary>
        /// <returns>Returns the results or FALSE on failure.
        /// Use Memcached::getResultCode if necessary. </returns>
        [PhpVisible, ImplementsMethod]
        [return: CastToFalse()]
        public PhpArray fetchAll()
        {
            PhpArray result = new PhpArray();
            PhpArray item;
            while ((item = fetch()) != null)
            {
                result.Add(item);
            }

            return (lastResult == ResConstants.End) ? result : null;
        }


        #endregion

        #region getMulti, getMultiByKey

        /// <summary>
        /// Memcached::getMulti() is similar to Memcached::get, but instead of a single key item,
        /// it retrieves multiple items the keys of which are specified in the keys  array. If cas_tokens
        /// variable is provided, it is filled with the CAS token values for the found items.
        ///  
        /// Note: Unlike Memcached::get it is not possible to specify a read-through cache callback
        /// for Memcached::getMulti(), because the memcache protocol does not provide information on which
        /// keys were not found in the multi-key request.
        /// 
        /// The flags parameter can be used to specify additional options for Memcached::getMulti().
        /// Currently, the only available option is Memcached::GET_PRESERVE_ORDER that ensures that
        /// the keys are returned in the same order as they were requested in. 
        /// </summary>
        /// <param name="keys">Array of keys to retrieve. </param>
        /// <returns>Returns the array of found items or FALSE  on failure. Use Memcached::getResultCode if necessary. </returns>
        [PhpVisible, ImplementsMethod]
        [return: CastToFalse()]
        public object getMulti(PhpArray keys) { return getMultiByKey(null, keys, null, 0); }
        /// <summary>
        /// Memcached::getMulti() is similar to Memcached::get, but instead of a single key item,
        /// it retrieves multiple items the keys of which are specified in the keys  array. If cas_tokens
        /// variable is provided, it is filled with the CAS token values for the found items.
        ///  
        /// Note: Unlike Memcached::get it is not possible to specify a read-through cache callback
        /// for Memcached::getMulti(), because the memcache protocol does not provide information on which
        /// keys were not found in the multi-key request.
        /// 
        /// The flags parameter can be used to specify additional options for Memcached::getMulti().
        /// Currently, the only available option is Memcached::GET_PRESERVE_ORDER that ensures that
        /// the keys are returned in the same order as they were requested in. 
        /// </summary>
        /// <param name="keys">Array of keys to retrieve. </param>
        /// <param name="cas_tokens">The variable to store the CAS tokens for the found items. </param>
        /// <returns>Returns the array of found items or FALSE  on failure. Use Memcached::getResultCode if necessary. </returns>
        [PhpVisible, ImplementsMethod]
        [return: CastToFalse()]
        public object getMulti(PhpArray keys, PhpReference cas_tokens) { return getMultiByKey(null, keys, cas_tokens, 0); }

        /// <summary>
        /// Memcached::getMulti() is similar to Memcached::get, but instead of a single key item,
        /// it retrieves multiple items the keys of which are specified in the keys  array. If cas_tokens
        /// variable is provided, it is filled with the CAS token values for the found items.
        ///  
        /// Note: Unlike Memcached::get it is not possible to specify a read-through cache callback
        /// for Memcached::getMulti(), because the memcache protocol does not provide information on which
        /// keys were not found in the multi-key request.
        /// 
        /// The flags parameter can be used to specify additional options for Memcached::getMulti().
        /// Currently, the only available option is Memcached::GET_PRESERVE_ORDER that ensures that
        /// the keys are returned in the same order as they were requested in. 
        /// </summary>
        /// <param name="keys">Array of keys to retrieve. </param>
        /// <param name="cas_tokens">The variable to store the CAS tokens for the found items. </param>
        /// <param name="flags">The flags for the get operation. </param>
        /// <returns>Returns the array of found items or FALSE  on failure. Use Memcached::getResultCode if necessary. </returns>
        [PhpVisible, ImplementsMethod]
        [return: CastToFalse()]
        public object getMulti(PhpArray keys, PhpReference cas_tokens, int flags) { return getMultiByKey(null, keys, cas_tokens, flags); }


        /// <summary>
        /// Memcached::getMultiByKey() is functionally equivalent to Memcached::getMulti,
        /// except that the free-form server_key  can be used to map the keys  to a specific server. 
        /// </summary>
        /// <param name="server_key">The key identifying the server to store the value on. </param>
        /// <param name="keys">Array of keys to retrieve. </param>
        /// <returns>Returns the array of found items or FALSE  on failure. Use Memcached::getResultCode if necessary. </returns>
        [PhpVisible, ImplementsMethod]
        [return: CastToFalse()]
        public object getMultiByKey(string server_key, PhpArray keys) { return getMultiByKey(server_key, keys, null, 0); }
        /// <summary>
        /// Memcached::getMultiByKey() is functionally equivalent to Memcached::getMulti,
        /// except that the free-form server_key  can be used to map the keys  to a specific server. 
        /// </summary>
        /// <param name="server_key">The key identifying the server to store the value on. </param>
        /// <param name="keys">Array of keys to retrieve. </param>
        /// <param name="cas_tokens">The variable to store the CAS tokens for the found items. </param>
        /// <returns>Returns the array of found items or FALSE  on failure. Use Memcached::getResultCode if necessary. </returns>
        [PhpVisible, ImplementsMethod]
        [return: CastToFalse()]
        public object getMultiByKey(string server_key, PhpArray keys, PhpReference cas_tokens) { return getMultiByKey(server_key, keys, cas_tokens, 0); }
        /// <summary>
        /// Memcached::getMultiByKey() is functionally equivalent to Memcached::getMulti,
        /// except that the free-form server_key  can be used to map the keys  to a specific server. 
        /// </summary>
        /// <param name="server_key">The key identifying the server to store the value on. </param>
        /// <param name="keys">Array of keys to retrieve. </param>
        /// <param name="cas_tokens">The variable to store the CAS tokens for the found items. </param>
        /// <param name="flags">The flags for the get operation. </param>
        /// <returns>Returns the array of found items or FALSE  on failure. Use Memcached::getResultCode if necessary. </returns>
        [PhpVisible, ImplementsMethod]
        [return: CastToFalse()]
        public object getMultiByKey(string server_key, PhpArray keys, PhpReference cas_tokens, int flags)
        {
            var values = getMultiInternal(server_key, keys, (GetConstants)flags);
            if (values == null)
                return null;

            // put the values into the PHP array
            PhpArray result = new PhpArray();
            PhpArray cass = new PhpArray();

            foreach (var x in values)
            {
                result.Add(x.Key, x.Value.value);
                cass.Add(x.Key, (double)x.Value.cas);   // according to memcached PHP implementation, cas token is just explicitly converted to double
            }
            
            //
            if (cas_tokens != null)
                cas_tokens.Value = cass;

            //
            return result;
        }

        /// <summary>
        /// Get requested items.
        /// </summary>
        /// <param name="server_key"></param>
        /// <param name="keys"></param>
        /// <param name="flags"></param>
        /// <returns>List of items. If PreserverOrder is specified, values of missng keys are added with its default value (null).</returns>
        private List<KeyValuePair<string, ResultObj>> getMultiInternal(string server_key, PhpArray keys, GetConstants flags)
        {
            // collect list of keys as strings
            List<string> keysList = getValidKeysList(keys);

            if (keysList == null || keysList.Count == 0)
            {
                lastResult = ResConstants.BadKeyProvided;
                return null;
            }

            // get all the values
            IDictionary<string,ResultObj> values;
            var rescode = ProtocolImpl.Get(server_key, keysList, out values);
            if (!SetResult(rescode) && lastResult != ResConstants.SomeErrors)   // also SOME_ERRORS is allowed as successful result
                return null;

            if (values == null)
            {   // should not be reachable
                lastResult = ResConstants.NotFound;
                return null;   // cannot get values
            }

            // result
            List<KeyValuePair<string, ResultObj>> result = new List<KeyValuePair<string, ResultObj>>(values.Count);

            if ((flags & GetConstants.PreserveOrder) != 0)
            {
                foreach (var key in keysList)
                {
                    ResultObj value;

                    if (!values.TryGetValue(key, out value))
                    {
                        value = new ResultObj();
                        value.value = null;
                        value.cas = 0;
                    }

                    result.Add(new KeyValuePair<string,ResultObj>(key, value));
                }
            }
            else
            {
                foreach (var x in values)
                {
                    result.Add(x);
                }
            }

            //
            return result;
        }

        /// <summary>
        /// Get list of valid keys from keys PhpArray.
        /// </summary>
        /// <param name="keys"></param>
        /// <returns></returns>
        private List<string>    getValidKeysList(PhpArray keys)
        {
            if (keys == null)
                return null;

            List<string> keysList = new List<string>();
            foreach (var keyobj in keys.Values)
            {
                string key = PhpVariable.AsString(keyobj);

                if (key != null)
                    keysList.Add(key);
            }

            return keysList;
        }

        #endregion

        #region setMulti, setMultiByKey

        /// <summary>
        /// Memcached::setMulti() is similar to Memcached::set, but instead of a single key/value item,
        /// it works on multiple items specified in items .
        /// The expiration  time applies to all the items at once. 
        /// </summary>
        /// <param name="items">An array of key/value pairs to store on the server. </param>
        /// <returns>Returns TRUE on success or FALSE on failure. Use Memcached::getResultCode if necessary. </returns>
        [PhpVisible, ImplementsMethod]
        public bool setMulti(PhpArray items) { return setMultiByKey(null, items, 0); }
        /// <summary>
        /// Memcached::setMulti() is similar to Memcached::set, but instead of a single key/value item,
        /// it works on multiple items specified in items .
        /// The expiration  time applies to all the items at once. 
        /// </summary>
        /// <param name="items">An array of key/value pairs to store on the server. </param>
        /// <param name="expiration">The expiration time, defaults to 0. See Expiration Times for more info. </param>
        /// <returns>Returns TRUE on success or FALSE on failure. Use Memcached::getResultCode if necessary. </returns>
        [PhpVisible, ImplementsMethod]
        public bool setMulti(PhpArray items, int expiration) { return setMultiByKey(null, items, expiration); }

        /// <summary>
        /// Memcached::setMultiByKey() is functionally equivalent to Memcached::setMulti,
        /// except that the free-form server_key  can be used to map the keys from items  to a specific server.
        /// This is useful if you need to keep a bunch of related keys on a certain server. 
        /// </summary>
        /// <param name="server_key">The key identifying the server to store the value on. </param>
        /// <param name="items">An array of key/value pairs to store on the server. </param>
        /// <returns>Returns TRUE on success or FALSE on failure. Use Memcached::getResultCode if necessary. </returns>
        [PhpVisible, ImplementsMethod]
        public bool setMultiByKey(string server_key, PhpArray items) { return setMultiByKey(server_key, items, 0); }
        /// <summary>
        /// Memcached::setMultiByKey() is functionally equivalent to Memcached::setMulti,
        /// except that the free-form server_key  can be used to map the keys from items  to a specific server.
        /// This is useful if you need to keep a bunch of related keys on a certain server. 
        /// </summary>
        /// <param name="server_key">The key identifying the server to store the value on. </param>
        /// <param name="items">An array of key/value pairs to store on the server. </param>
        /// <param name="expiration">The expiration time, defaults to 0. See Expiration Times for more info. </param>
        /// <returns>Returns TRUE on success or FALSE on failure. Use Memcached::getResultCode if necessary. </returns>
        [PhpVisible, ImplementsMethod]
        public bool setMultiByKey(string server_key, PhpArray items, int expiration)
        {
            if (items == null)
                return SetResult(ResConstants.ClientError);

            if (expiration < 0)
                return SetResult(ResConstants.NotStored);

            bool bOk = true;

            foreach (var x in items)
            {
                string key = x.Key.String;
                object value = x.Value;

                if (key != null)
                {
                    bOk &= setByKey(server_key, key, value, expiration);// sets some result code
                }
            }

            return bOk;
        }

        #endregion

        #region getOption, setOption, getResultCode, getResultMessage

        /// <summary>
        /// This method returns the value of a Memcached option . Some options correspond to the ones defined by
        /// libmemcached, and some are specific to the extension.
        /// 
        /// See Memcached Constants for more information. 
        /// </summary>
        /// <param name="option">One of the Memcached::OPT_* constants. </param>
        /// <returns>Returns the value of the requested option, or FALSE on error. </returns>
        [PhpVisible, ImplementsMethod]
        [return: CastToFalse()]
        public object getOption(int option)
        {
            switch ((OptionsConstants)option)
            {
                case OptionsConstants.PrefixKey:
                    return Servers.PrefixKey ?? string.Empty;  // do not return FALSE
                case OptionsConstants.BinaryProtocol:
                    return (bool)(ProtocolImpl is Enyim.Caching.Memcached.Operations.Binary.BinaryProtocol);
                case OptionsConstants.Hash:
                    return (int)Servers.KeyHash;
                case OptionsConstants.Distribution:
                    return (int)Servers.DistributionMethod;
                case OptionsConstants.LibketamaCompatible:
                    return (bool)(Servers.KeyHash == HashConstants.MD5 && Servers.DistributionMethod == DistributionConstants.Consistent);
                case OptionsConstants.Compression:
                    return (bool)Servers.Transcoder.Compression;
                case OptionsConstants.Serializer:
                    if (Servers.Transcoder is PhpTranscoder) return (int)SerializerConstants.Php;
                    throw new NotImplementedException("getOption() does not handle current serializer!");
                default:
                    PhpException.ArgumentValueNotSupported("option");
                    return false;
            }
        }

        private long tolong(object value)
        {
            return (value != null) ? ((value is int) ? ((long)(int)value) : ((value is long) ? ((long)value) : ((value is bool) ? (((bool)value) ? 1 : 0) : (0)))) : 0;
        }

        /// <summary>
        /// This method sets the value of a Memcached option .
        /// Some options correspond to the ones defined by libmemcached, and some are specific to the extension.
        /// See Memcached Constants for more information.
        ///  
        /// The options listed below require values specified via constants.
        /// - Memcached::OPT_HASH requires Memcached::HASH_* values.
        /// - Memcached::OPT_DISTRIBUTION requires Memcached::DISTRIBUTION_* values.
        /// </summary>
        /// <param name="option"></param>
        /// <param name="value"></param>
        /// <returns>Returns TRUE on success or FALSE on failure. </returns>
        [PhpVisible, ImplementsMethod]
        public bool setOption(int option, object value)
        {
            switch ((OptionsConstants)option)
            {
                case OptionsConstants.PrefixKey:
                    {
                        string prefix = PhpVariable.AsString(value);

                        if (prefix != null && prefix.Length > 128 && String.IsNullOrEmpty(Servers.KeyTransformer.Transform(prefix)))
                        {
                            PhpException.Throw(PhpError.Warning, "bad key provided");
                            return false;
                        }

                        Servers.PrefixKey = prefix;
                        return true;
                    }
                case OptionsConstants.BinaryProtocol:
                    return SwitchProtocol(tolong(value) > 0);
                case OptionsConstants.Hash:
                    {
                        long hashFunc = tolong(value);

                        if (hashFunc < 0 || hashFunc >= (long)HashConstants.Count)
                            return false;

                        Servers.KeyHash = (HashConstants)hashFunc;

                        return true;
                    }
                case OptionsConstants.Distribution:
                    {
                        long distributionFunc = tolong(value);

                        if (distributionFunc < 0 || distributionFunc >= (long)DistributionConstants.Count)
                            return false;

                        Servers.DistributionMethod = (DistributionConstants)distributionFunc;

                        return true;
                    }
                case OptionsConstants.Compression:
                    Servers.Transcoder.Compression = (tolong(value) > 0);
                    return true;
                case OptionsConstants.Serializer:
                    try
                    {
                        Servers.Transcoder.Serializer = (SerializerConstants)tolong(value);
                        return true;
                    }
                    catch   // invalid serializer provided, switch to default
                    {
                        PhpException.Throw(PhpError.Warning, "invalid serializer provided");
                        Servers.Transcoder.Serializer = SerializerConstants.Php;
                        return false;
                    }
                default:
                    PhpException.ArgumentValueNotSupported("option");
                    return false;
            }
        }

        /// <summary>
        /// Set new protocol (binary/text).
        /// </summary>
        /// <param name="wantBinary">Use binary protocol?</param>
        /// <returns>True if it succeeded.</returns>
        private bool SwitchProtocol(bool wantBinary)
        {
            Debug.Assert(ProtocolImpl != null);
            
            bool isBinaryNow = (ProtocolImpl is Enyim.Caching.Memcached.Operations.Binary.BinaryProtocol);
            
            if (isBinaryNow != wantBinary)
            {
                ClientState state = CreateNewClient(wantBinary, Servers);

                // copy servers / reconnect them!
                state.Servers.ReaddServers();

                // refresh the persistent client, if current client is persistent
                StorePersistentClient(state, PersistentId);

                ProtocolImpl = state.ProtocolImpl;
                Servers = state.Servers;
            }

            return true;
        }

        /// <summary>
        /// Memcached::getResultCode() returns one of the Memcached::RES_* constants that is the result of the last executed Memcached method. 
        /// </summary>
        /// <returns>Result code of the last Memcached operation. </returns>
        [PhpVisible, ImplementsMethod]
        public int getResultCode()
        {
            return (int)lastResult;
        }

        /// <summary>
        /// Memcached::getResultMessage() returns a string that describes the result code of the last executed Memcached method. 
        /// </summary>
        /// <returns>Message describing the result of the last Memcached operation. </returns>
        [PhpVisible, ImplementsMethod]
        public string getResultMessage()
        {
            string constantName = lastResult.ToString();

            // make it human readable, prepend space between all the capitals
            Regex expCap = new Regex(@"[a-z][A-Z]");
            constantName = expCap.Replace(constantName, delegate(Match match)
            {
                if (match.Length == 2)
                    return match.Value[0] + " " + match.Value[1];
                else
                    return match.Value;
            });

            // make it upper, as it is in PHP
            return constantName.ToUpper();
        }

        #endregion

        #region getStats, getVersion

        /// <summary>
        /// Memcached::getStats() returns an array containing the state of all available memcache servers.
        /// See » memcache protocol specification for details on these statistics. 
        /// </summary>
        /// <returns>Array of server statistics, one entry per server. </returns>
        [PhpVisible, ImplementsMethod]
        [return: CastToFalse()]
        public PhpArray getStats()
        {
            ServerStats stats;
            if (!SetResult(ProtocolImpl.Stats(out stats)))
                return null;

            Debug.Assert(stats != null, "Should not be null here.");

            PhpArray result = new PhpArray();

            foreach (var x in stats.results)
            {
                PhpArray stat = new PhpArray();

                foreach (var row in x.Value)
                {
                    stat.Add(row.Key, row.Value);
                }

                result.Add(x.Key.HostName + ":" + x.Key.Port, stat);
            }

            return result;
        }

        /// <summary>
        /// Memcached::getVersion() returns an array containing the version info for all available memcache servers. 
        /// </summary>
        /// <returns>Array of server versions, one entry per server. </returns>
        [PhpVisible, ImplementsMethod]
        [return: CastToFalse()]
        public PhpArray getVersion()
        {
            ServerStats stats;
            if (!SetResult(ProtocolImpl.Stats(out stats)))
                return null;

            Debug.Assert(stats != null, "Should not be null here.");

            PhpArray result = new PhpArray();

            foreach (var x in stats.results)
            {
                PhpArray stat = new PhpArray();

                string version;

                if (!x.Value.TryGetValue(ServerStats.StatKeys[(int)StatItem.Version], out version))
                {
                    version = null;
                }

                result.Add(x.Key.HostName + ":" + x.Key.Port, version);
            }

            return result;
        }

        #endregion

    }
}
