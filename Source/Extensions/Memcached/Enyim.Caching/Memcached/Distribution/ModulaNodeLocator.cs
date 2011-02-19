using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Security.Cryptography;
using System.Diagnostics;

namespace Enyim.Caching.Memcached.Distribution
{
	/// <summary>
	/// This is a ketama-like consistent hashing based node locator. Used when no other <see cref="T:IMemcachedNodeLocator"/> is specified for the pool.
	/// </summary>
    public sealed class ModulaNodeLocator : IMemcachedNodeLocator
	{
        private IMemcachedNode[] servers;
		private bool isInitialized;
        private HashAlgorithm hash;

        void IMemcachedNodeLocator.Initialize(ICollection<IMemcachedNode> nodes, HashAlgorithm hash)
		{
			if (this.isInitialized)
				throw new InvalidOperationException("Instance is already initialized.");

            this.hash = hash;

			// locking on this is rude but easy
			lock (this)
			{
				if (this.isInitialized)
					throw new InvalidOperationException("Instance is already initialized.");

                servers = nodes.ToArray();

				this.isInitialized = true;
			}
		}

		IMemcachedNode IMemcachedNodeLocator.Locate(string key)
		{
			if (!this.isInitialized)
				throw new InvalidOperationException("You must call Initialize first");

			if (key == null)
				throw new ArgumentNullException("key");

            if (servers.Length > 0)
            {
                uint itemKeyHash = BitConverter.ToUInt32(hash.ComputeHash(Encoding.UTF8.GetBytes(key)), 0);
                return servers[itemKeyHash % servers.Length];
            }
            else
                return null;
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
