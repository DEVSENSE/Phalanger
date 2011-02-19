using System.Collections.Generic;
using System.Security.Cryptography;

namespace Enyim.Caching.Memcached.Distribution
{
	/// <summary>
	/// Defines a locator class which maps item keys to memcached servers.
	/// </summary>
	public interface IMemcachedNodeLocator
	{
		/// <summary>
		/// Initializes the locator.
		/// </summary>
		/// <param name="nodes">The memcached nodes defined in the configuration.</param>
        /// <param name="hash">Hashing algorithm used by the locator.</param>
        void Initialize(ICollection<IMemcachedNode> nodes, HashAlgorithm hash);
		/// <summary>
		/// Returns the memcached node the specified key belongs to.
		/// </summary>
		/// <param name="key">The key of the item to be located.</param>
		/// <returns>The <see cref="T:MemcachedNode"/> the specifed item belongs to</returns>
		IMemcachedNode Locate(string key);
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
