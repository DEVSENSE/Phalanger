using System;
using System.Collections.Generic;
using PHP.Library.Memcached;

namespace Enyim.Caching.Memcached.Operations.Binary
{
	/// <summary>
	/// Memcached client.
	/// </summary>
	internal class BinaryProtocol : IProtocolImplementation
	{
        /// <summary>
        /// Maximum key length in Text protocol.
        /// </summary>
        public const int MaxKeyLength = 0xffff;

        private IServerPool pool;

		public BinaryProtocol(IServerPool pool)
		{
			this.pool = pool;
        }

        #region IDisposable

        void IDisposable.Dispose()
		{
			this.Dispose();
		}

		~BinaryProtocol()
		{
			try { ((IDisposable)this).Dispose(); }
			catch { }
		}

		/// <summary>
		/// Releases all resources allocated by this instance
		/// </summary>
		/// <remarks>Technically it's not really neccesary to call this, since the client does not create "really" disposable objects, so it's safe to assume that when 
		/// the AppPool shuts down all resources will be released correctly and no handles or such will remain in the memory.</remarks>
		public void Dispose()
		{
			if (this.pool != null)
			{
				((IDisposable)this.pool).Dispose();
				this.pool = null;
			}
        }

        #endregion

        ResConstants IProtocolImplementation.Store(StoreMode mode, string serverKey, string key, object value, ulong cas, uint expires)
		{
			if (value == null)
                return ResConstants.PayloadFailure;

            StoreCommand cmd = (StoreCommand)mode;  // CheckAndSet automatically in binary protocol

            using (StoreOperation s = new StoreOperation(pool, cmd, pool.PrefixKey + key, serverKey, value, expires))
			{
                s.Cas = cas;
				return s.Execute();
			}
		}

        ResConstants IProtocolImplementation.TryGet(string serverKey, string key, out ResultObj result)
		{
            using (GetOperation g = new GetOperation(this.pool, pool.PrefixKey + key, serverKey))
            {
                ResConstants ret = g.Execute();
                result = new ResultObj() { value = g.Result, cas = g.Cas };
                return ret;
            }
		}

        ResConstants IProtocolImplementation.Mutate(MutationMode mode, string serverKey, string key, ulong defaultValue, ulong delta, bool createIfNotExists, out ulong newValue)
		{
            using (MutatorOperation m = new MutatorOperation(this.pool, mode, pool.PrefixKey + key, serverKey, defaultValue, delta, createIfNotExists ? 0 : 0xffffffff/*all bits to 1 to fail, if the key does not exist*/))
			{
                ResConstants ret = m.Execute();
                newValue = m.Result;
                return ret;
			}
		}

        ResConstants IProtocolImplementation.Remove(string serverKey, string key, int delay)
		{
            using (DeleteOperation g = new DeleteOperation(this.pool, pool.PrefixKey + key, serverKey, delay))
			{
				return g.Execute();
			}
		}

        ResConstants IProtocolImplementation.FlushAll(int delay)
		{
			using (FlushOperation f = new FlushOperation(this.pool, delay))
			{
				return f.Execute();
			}
		}

        ResConstants IProtocolImplementation.Concatenate(ConcatenationMode mode, string serverKey, string key, ArraySegment<byte> data)
		{
            if (pool.Transcoder.Compression)
            {
                PHP.Core.PhpException.Throw(PHP.Core.PhpError.Warning, "cannot append/prepend with compression turned on");
                return ResConstants.Failure;
            }

            if (data.Count == 0)
                return ResConstants.Success;

            using (ConcatenationOperation co = new ConcatenationOperation(this.pool, mode, pool.PrefixKey + key, serverKey, data))
			{
				return co.Execute();
			}
		}

        ResConstants IProtocolImplementation.Stats(out ServerStats result)
		{
			using (StatsOperation so = new StatsOperation(this.pool))
			{
				var ret = so.Execute();
                result = so.Results;
				return ret;
			}
		}

		IAuthenticator IProtocolImplementation.CreateAuthenticator(ISaslAuthenticationProvider provider)
		{
			return new BinaryAuthenticator(provider);
		}

        ResConstants IProtocolImplementation.Get(string serverKey, IEnumerable<string> keys, out IDictionary<string, ResultObj> result)
		{
            if (!string.IsNullOrEmpty(pool.PrefixKey))
                keys = new KeyEnumPrefixed(pool.PrefixKey, keys);

			using (var mg = new MultiGetOperation(this.pool, keys, serverKey))
			{
                ResConstants ret = mg.Execute();
                result = mg.Result;
                return ret;
			}
		}
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
