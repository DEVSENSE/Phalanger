using System;
using System.Text;

namespace PHP.Library.Memcached
{
    /// <summary>
    /// 
    /// </summary>
    public enum OptionsConstants
    {
        /// <summary>
        /// Specifies the serializer to use for serializing non-scalar values.
        /// The valid serializers are Memcached::SERIALIZER_PHP or Memcached::SERIALIZER_IGBINARY.
        /// The latter is supported only when memcached is configured with --enable-memcached-igbinary
        /// option and the igbinary extension is loaded.
        /// 
        /// Type: integer, default: Memcached::SERIALIZER_PHP.
        /// </summary>
        //[ImplementsConstant("OPT_SERIALIZER")]
        Serializer = -1003,

        /// <summary>
        /// This can be used to create a "domain" for your item keys.
        /// The value specified here will be prefixed to each of the keys.
        /// It cannot be longer than 128 characters and will reduce the maximum available key size.
        /// The prefix is applied only to the item keys, not to the server keys.
        /// 
        /// Type: string, default: "".
        /// </summary>
        //[ImplementsConstant("OPT_PREFIX_KEY")]
        PrefixKey = -1002,

        /// <summary>
        /// Enables or disables payload compression. When enabled, 
        /// item values longer than a certain threshold (currently 100 bytes)
        /// will be compressed during storage and decompressed during retrieval transparently.
        /// 
        /// Type: boolean, default: TRUE.
        /// </summary>
        //[ImplementsConstant("OPT_COMPRESSION")]
        Compression = -1001,

        /// <summary>
        /// Enables or disables asynchronous I/O. This is the fastest transport available for storage functions.
        /// Type: boolean, default: FALSE.
        /// </summary>
        //[ImplementsConstant("OPT_NO_BLOCK")]
        NoBlock = 0, // Not implemented

        /// <summary>
        /// Enables or disables the no-delay feature for connecting sockets (may be faster in some environments).
        /// Type: boolean, default: FALSE.
        /// </summary>
        //[ImplementsConstant("OPT_TCP_NODELAY")]
        TcpNoDelay = 1, // Not implemented

        /// <summary>
        ///Specifies the hashing algorithm used for the item keys.
        ///The valid values are supplied via Memcached::HASH_* constants.
        ///Each hash algorithm has its advantages and its disadvantages.
        ///Go with the default if you don't know or don't care.
        ///
        /// Type: integer, default: Memcached::HASH_DEFAULT
        /// </summary>
        //[ImplementsConstant("OPT_HASH")]
        Hash = 2,

        /// <summary>
        /// Specifies the method of distributing item keys to the servers.
        /// Currently supported methods are modulo and consistent hashing.
        /// Consistent hashing delivers better distribution and allows servers
        /// to be added to the cluster with minimal cache losses.
        /// 
        /// Type: integer, default: Memcached::DISTRIBUTION_MODULA.
        /// </summary>
        //[ImplementsConstant("OPT_DISTRIBUTION")]
        Distribution = 9,
        
        /// <summary>
        /// Enables or disables compatibility with libketama-like behavior.
        /// When enabled, the item key hashing algorithm is set to MD5 and distribution is set to be weighted consistent hashing distribution. This is useful because other libketama-based clients (Python, Ruby, etc.) with the same server configuration will be able to access the keys transparently.
        /// Note: It is highly recommended to enable this option if you want to use consistent hashing, and it may be enabled by default in future releases.
        /// Type: boolean, default: FALSE.
        /// </summary>
        //[ImplementsConstant("OPT_LIBKETAMA_COMPATIBLE")]
        LibketamaCompatible = 16,
        
        /// <summary>
        /// Enables or disables buffered I/O.
        /// Enabling buffered I/O causes storage commands to "buffer" instead of being sent.
        /// Any action that retrieves data causes this buffer to be sent to the remote connection.
        /// Quitting the connection or closing down the connection will also cause the buffered data to be pushed to the remote connection.
        /// Type: boolean, default: FALSE.
        /// </summary>
        //[ImplementsConstant("OPT_BUFFER_WRITES")]
        BufferWrites = 10, // Not implemented

        /// <summary>
        /// Enable the use of the binary protocol. Please note that you cannot toggle this option on an open connection.
        /// Type: boolean, default: FALSE.
        /// </summary>
        //[ImplementsConstant("OPT_BINARY_PROTOCOL")]
        BinaryProtocol = 18,

        /// <summary>
        /// The maximum socket send buffer in bytes.
        /// Type: integer, default: varies by platform/kernel configuration.
        /// </summary>
        //[ImplementsConstant("OPT_SOCKET_SEND_SIZE")]
        SocketSendSize = 4, // Not implemented

        /// <summary>
        /// The maximum socket receive buffer in bytes.
        /// Type: integer, default: varies by platform/kernel configuration.
        /// </summary>
        //[ImplementsConstant("OPT_SOCKET_RECV_SIZE")]
        SocketRecvSize = 5, // Not implemented

        /// <summary>
        /// In non-blocking mode this set the value of the timeout during socket connection, in milliseconds.
        /// Type: integer, default: 1000.
        /// </summary>
        //[ImplementsConstant("OPT_CONNECT_TIMEOUT")]
        ConnectTimeout = 14, // Not implemented

        /// <summary>
        /// The amount of time, in seconds, to wait until retrying a failed connection attempt.
        /// Type: integer, default: 0.
        /// </summary>
        //[ImplementsConstant("OPT_RETRY_TIMEOUT")]
        RetryTimeout = 15, // Not implemented

        /// <summary>
        /// Socket sending timeout, in microseconds. In cases where you cannot use non-blocking I/O this will allow you to still have timeouts on the sending of data.
        /// Type: integer, default: 0.
        /// </summary>
        //[ImplementsConstant("OPT_SEND_TIMEOUT")]
        SendTimeout = 19, // Not implemented

        /// <summary>
        /// Socket reading timeout, in microseconds. In cases where you cannot use non-blocking I/O this will allow you to still have timeouts on the reading of data.
        /// Type: integer, default: 0.
        /// </summary>
        //[ImplementsConstant("OPT_RECV_TIMEOUT")]
        RecvTimeout = 15, // Not implemented

        /// <summary>
        /// Timeout for connection polling, in milliseconds.
        /// Type: integer, default: 1000.
        /// </summary>
        //[ImplementsConstant("OPT_POLL_TIMEOUT")]
        PollTimeout = 8, // Not implemented

        /// <summary>
        /// Enables or disables caching of DNS lookups.
        /// Type: boolean, default: FALSE.
        /// </summary>
        //[ImplementsConstant("OPT_CACHE_LOOKUPS")]
        CacheLookups = 6, // Not implemented

        /// <summary>
        /// Specifies the failure limit for server connection attempts. The server will be removed after this many continuous connection failures.
        /// Type: integer, default: 0.
        /// </summary>
        //[ImplementsConstant("OPT_SERVER_FAILURE_LIMIT")]
        ServerFailureLimit = 21, // Not implemented

    }

    /// <summary>
    /// 
    /// </summary>
    [Flags]
    public enum GetConstants
    {
        /// <summary>
        /// A flag for Memcached::getMulti() and Memcached::getMultiByKey() to ensure that
        /// the keys are returned in the same order as they were requested in.
        /// Non-existing keys get a default value of NULL.
        /// </summary>
        //[ImplementsConstant("GET_PRESERVE_ORDER")]
        PreserveOrder = 1,

    }

    /// <summary>
    /// 
    /// </summary>
    public enum DistributionConstants
    {
        /// <summary>
        /// Modulo-based key distribution algorithm.
        /// </summary>
        //[ImplementsConstant("DISTRIBUTION_MODULA")]
        ModulA = 0,

        /// <summary>
        /// Consistent hashing key distribution algorithm (based on libketama).
        /// </summary>
        //[ImplementsConstant("DISTRIBUTION_CONSISTENT")]
        Consistent = 1,

        /// <summary>
        /// The last index in the enum - distribution constants count. For internal use only.
        /// </summary>
        Count
    }

    /// <summary>
    /// 
    /// </summary>
    public enum SerializerConstants
    {
        /// <summary>
        /// The default PHP serializer.
        /// </summary>
        //[ImplementsConstant("SERIALIZER_PHP")]
        Php = 1,

        /// <summary>
        /// The » igbinary serializer. Instead of textual representation it stores PHP data structures in a compact binary form, resulting in space and time gains.
        /// </summary>
        //[ImplementsConstant("SERIALIZER_IGBINARY")]
        IgBinary = 2,

        /// <summary>
        /// The JSON serializer. Requires PHP 5.2.10+.
        /// </summary>
        //[ImplementsConstant("SERIALIZER_JSON")]
        JSON = 3,
    }

    /// <summary>
    /// 
    /// </summary>
    public enum HashConstants
    {
        /// <summary>
        /// The default (Jenkins one-at-a-time) item key hashing algorithm.
        /// </summary>
        //[ImplementsConstant("HASH_DEFAULT")]
        Default = 0,

        /// <summary>
        /// MD5 item key hashing algorithm.
        /// </summary>
        //[ImplementsConstant("HASH_MD5")]
        MD5 = 1,

        /// <summary>
        /// CRC item key hashing algorithm.
        /// </summary>
        //[ImplementsConstant("HASH_CRC")]
        CRC = 2,

        /// <summary>
        /// FNV1_64 item key hashing algorithm.
        /// </summary>
        //[ImplementsConstant("HASH_FNV1_64")]
        FNV1_64 = 3,

        /// <summary>
        /// FNV1_64A item key hashing algorithm.
        /// </summary>
        //[ImplementsConstant("HASH_FNV1A_64")]
        FNV1A_64 = 4,

        /// <summary>
        /// FNV1_32 item key hashing algorithm.
        /// </summary>
        //[ImplementsConstant("HASH_FNV1_32")]
        FNV1_32 = 5,

        /// <summary>
        /// FNV1_32A item key hashing algorithm.
        /// </summary>
        //[ImplementsConstant("HASH_FNV1A_32")]
        FNV1A_32 = 6,

        /// <summary>
        /// Hsieh item key hashing algorithm.
        /// </summary>
        //[ImplementsConstant("HASH_HSIEH")]
        HSIEH = 7,

        /// <summary>
        /// Murmur item key hashing algorithm.
        /// </summary>
        //[ImplementsConstant("HASH_MURMUR")]
        MURMUR = 8,

        /// <summary>
        /// The last index in the enum - hash constants count. For internal use only.
        /// </summary>
        Count
    }

    /// <summary>
    /// 
    /// </summary>
    public enum ResConstants
    {
        /// <summary>
        /// The operation was successful.
        /// </summary>
        //[ImplementsConstant("RES_SUCCESS")]
        Success = 0,

        /// <summary>
        /// The operation failed in some fashion.
        /// </summary>
        //[ImplementsConstant("RES_FAILURE")]
        Failure = 1,

        /// <summary>
        /// DNS lookup failed.
        /// </summary>
        //[ImplementsConstant("RES_HOST_LOOKUP_FAILURE")]
        HostLookupFailure = 2,

        /// <summary>
        /// Failed to read network data.
        /// </summary>
        //[ImplementsConstant("RES_UNKNOWN_READ_FAILURE")]
        UnknownReadFalure = 7,

        /// <summary>
        /// Bad command in memcached protocol.
        /// </summary>
        //[ImplementsConstant("RES_PROTOCOL_ERROR")]
        ProtocolError = 8,

        /// <summary>
        /// Error on the client side.
        /// </summary>
        //[ImplementsConstant("RES_CLIENT_ERROR")]
        ClientError = 9,

        /// <summary>
        /// Error on the server side.
        /// </summary>
        //[ImplementsConstant("RES_SERVER_ERROR")]
        ServerError = 10,

        /// <summary>
        /// Failed to write network data.
        /// </summary>
        //[ImplementsConstant("RES_WRITE_FAILURE")]
        WriteFailure = 5,

        /// <summary>
        /// Failed to do compare-and-swap: item you are trying to store has been modified since you last fetched it.
        /// </summary>
        //[ImplementsConstant("RES_DATA_EXISTS")]
        DataExists = 12,

        /// <summary>
        /// Item was not stored: but not because of an error. This normally means that either the condition for an "add" or a "replace" command wasn't met, or that the item is in a delete queue.
        /// </summary>
        //[ImplementsConstant("RES_NOTSTORED")]
        NotStored = 14,

        /// <summary>
        /// Item with this key was not found (with "get" operation or "cas" operations).
        /// </summary>
        //[ImplementsConstant("RES_NOTFOUND")]
        NotFound = 16,

        /// <summary>
        /// Partial network data read error.
        /// </summary>
        //[ImplementsConstant("RES_PARTIAL_READ")]
        PartialRead = 18,

        /// <summary>
        /// Some errors occurred during multi-get.
        /// </summary>
        //[ImplementsConstant("RES_SOME_ERRORS")]
        SomeErrors = 19,

        /// <summary>
        /// Server list is empty.
        /// </summary>
        //[ImplementsConstant("RES_NO_SERVERS")]
        NoServers = 20,

        /// <summary>
        /// End of result set.
        /// </summary>
        //[ImplementsConstant("RES_END")]
        End = 21,

        /// <summary>
        /// System error.
        /// </summary>
        //[ImplementsConstant("RES_ERRNO")]
        ErrNo = 26,

        /// <summary>
        /// The operation was buffered.
        /// </summary>
        //[ImplementsConstant("RES_BUFFERED")]
        Buffered = 32,

        /// <summary>
        /// The operation timed out.
        /// </summary>
        //[ImplementsConstant("RES_TIMEOUT")]
        Timeout = 31,

        /// <summary>
        /// Bad key.
        /// </summary>
        //[ImplementsConstant("RES_BAD_KEY_PROVIDED")]
        BadKeyProvided = 33,

        /// <summary>
        /// Failed to create network socket.
        /// </summary>
        //[ImplementsConstant("RES_CONNECTION_SOCKET_CREATE_FAILURE")]
        ConnectionSocketCreateFailure = 11,

        /// <summary>
        /// Payload failure: could not compress/decompress or serialize/unserialize the value.
        /// </summary>
        //[ImplementsConstant("RES_PAYLOAD_FAILURE")]
        PayloadFailure = -1001,
    }
}