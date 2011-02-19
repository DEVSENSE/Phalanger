using System;
using System.Collections.Generic;

using PHP.Library.Memcached;

namespace Enyim.Caching.Memcached
{
    public class ResultObj
    {
        public object value;
        public ulong cas;
    }

    /// <summary>
    /// String enumerator that returns values of another string enumerator with given prefix.
    /// </summary>
    internal class KeyEnumPrefixed : IEnumerable<string>
    {
        private readonly IEnumerable<string> baseKeys;
        private readonly string prefix;

        public KeyEnumPrefixed(string prefix, IEnumerable<string> baseKeys)
        {
            this.baseKeys = baseKeys;
            this.prefix = prefix;
        }

        #region IEnumerable<string> Members

        public IEnumerator<string> GetEnumerator()
        {
            foreach (var key in baseKeys)
                yield return prefix + key;
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        #endregion
    }

	internal interface IProtocolImplementation : IDisposable
	{
        ResConstants TryGet(string serverKey, string key, out ResultObj result);
        ResConstants Get(string serverKey, IEnumerable<string> keys, out IDictionary<string, ResultObj> result);

        ResConstants Store(StoreMode mode, string serverKey, string key, object value, ulong cas, uint expiration);
        ResConstants Remove(string serverKey, string key, int delay);
        ResConstants Mutate(MutationMode mode, string serverKey, string key, ulong startValue, ulong step, bool createIfNotExists, out ulong newValue);
        ResConstants Concatenate(ConcatenationMode mode, string serverKey, string key, ArraySegment<byte> data);

        ResConstants FlushAll(int delay);
        ResConstants Stats(out ServerStats result);

		IAuthenticator CreateAuthenticator(ISaslAuthenticationProvider provider);
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
