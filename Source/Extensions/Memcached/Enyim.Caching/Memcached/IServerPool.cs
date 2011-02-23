using System;
using System.Collections.Generic;

using Enyim.Caching.Memcached.Distribution;

namespace Enyim.Caching.Memcached
{
	/// <summary>
	/// Provides custom server pool implementations
	/// </summary>
	public interface IServerPool : IDisposable
	{
        /// <summary>
        /// Transcoder.
        /// </summary>
        TranscoderBase Transcoder { get; }
		
        /// <summary>
        /// Locator.
        /// </summary>
        IMemcachedNodeLocator NodeLocator { get; }

        /// <summary>
        /// Key transformer.
        /// </summary>
		IMemcachedKeyTransformer KeyTransformer { get; }

        /// <summary>
        /// Prefix key.
        /// </summary>
        string PrefixKey { get; }
        
        /// <summary>
        /// Acquire socket from pool.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
		PooledSocket Acquire(string key);
		
        /// <summary>
        /// Get servers in pool.
        /// </summary>
        /// <returns></returns>
        IEnumerable<IMemcachedNode> GetServers();
        
        /// <summary>
        /// Server count in the pool.
        /// </summary>
        int ServersCount { get; }

        /// <summary>
        /// Authenticator.
        /// </summary>
		IAuthenticator Authenticator { get; set; }

        /// <summary>
        /// Start the pool.
        /// </summary>
		void Start();
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
