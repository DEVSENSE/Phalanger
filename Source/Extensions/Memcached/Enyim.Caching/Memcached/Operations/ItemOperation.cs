using System;
using System.Diagnostics;

using PHP.Library.Memcached;

namespace Enyim.Caching.Memcached.Operations
{
    /// <summary>
	/// Base class for implementing operations working with keyed items. Handles server selection based on item key.
	/// </summary>
	internal abstract class ItemOperation : Operation
	{
        /// <summary>
        /// Transformed key. By default it's equal to Key.
        /// </summary>
		protected readonly string HashedKey;
		
		private PooledSocket socket;

		protected ItemOperation(IServerPool pool, string key, string serverKey, int maxKeyLength)
			: base(pool)
		{
            if (string.IsNullOrEmpty(key) || key.Length > maxKeyLength)
                pendingError = ResConstants.BadKeyProvided;
			
            if (string.IsNullOrEmpty(serverKey))
                serverKey = key;

            this.Key = key;
            this.ServerKey = serverKey;

            // test the key (in PHP, for all the chars c must be isgraph(c) != 0) 
            HashedKey = this.ServerPool.KeyTransformer.Transform(this.Key);
            //Debug.Assert(!String.IsNullOrEmpty(tmp), this.ServerPool.KeyTransformer + " just returned an empty key.");
            if (string.IsNullOrEmpty(HashedKey))
                pendingError = ResConstants.BadKeyProvided;

		}

        protected string Key { get; private set; }
        protected string ServerKey { get; private set; }
        public ulong Cas { get; set; }

		protected PooledSocket Socket
		{
			get
			{
				if (this.socket == null)
				{
                    // get a connection to the server which the "key" (ServerKey) belongs to
                    PooledSocket ps = this.ServerPool.Acquire(this.ServerKey);

					// null was returned, so our server is dead and no one could replace it
					// (probably all of our servers are down)
					if (ps == null)
						return null;

					this.socket = ps;
				}

				return this.socket;
			}
		}

		public override void Dispose()
		{
			GC.SuppressFinalize(this);

			if (this.socket != null)
			{
				((IDisposable)this.socket).Dispose();
				this.socket = null;
			}

			base.Dispose();
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
