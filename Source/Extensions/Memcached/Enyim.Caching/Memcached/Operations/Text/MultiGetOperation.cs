using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

using Enyim.Caching.Memcached.Distribution;
using PHP.Library.Memcached;

namespace Enyim.Caching.Memcached.Operations.Text
{
	internal class MultiGetOperation : Operation
	{
		//private static log4net.ILog log = log4net.LogManager.GetLogger(typeof(MultiGetOperation));

        private readonly string serverKey;
        private readonly IEnumerable<string> keys;

        private Dictionary<string, ResultObj> result;

        public MultiGetOperation(IServerPool pool, IEnumerable<string> keys, string serverKey)
			: base(pool)
		{
            this.serverKey = serverKey;
            this.keys = keys;
		}

		protected override ResConstants ExecuteAction()
		{
			// {hashed key -> normal key}: will be used when mapping the returned items back to the original keys
			Dictionary<string, string> hashedToReal = new Dictionary<string, string>(StringComparer.Ordinal);

			// {normal key -> hashed key}: we have to hash all keys anyway, so we better cache them to improve performance instead of doing the hashing later again
			Dictionary<string, string> realToHashed = new Dictionary<string, string>(StringComparer.Ordinal);

			IMemcachedKeyTransformer transformer = this.ServerPool.KeyTransformer;

			// and store them with the originals so we can map the returned items 
			// to the original keys
			foreach (string s in this.keys)
			{
				string hashed = transformer.Transform(s);

				hashedToReal[hashed] = s;
				realToHashed[s] = hashed;
			}

			// map each key to the appropriate server in the pool
			IMemcachedNodeLocator locator = this.ServerPool.NodeLocator;
            IDictionary<IMemcachedNode, List<string>> splitKeys = this.SplitKeys(this.keys, this.serverKey);

            if (splitKeys == null || splitKeys.Count == 0)
                return ResConstants.NoServers;

			// we'll open 1 socket for each server
			List<PooledSocket> sockets = new List<PooledSocket>();

            bool someErrors = false;

			try
			{
				// send a 'gets' to each server
				foreach (var de in splitKeys)
				{
					var server = de.Key;
                    if (!server.IsAlive)
                    {
                        someErrors = true;
                        continue;
                    }

					PooledSocket socket = server.Acquire();
                    if (socket == null)
                    {
                        someErrors = true;
                        continue;
                    }
					sockets.Add(socket);

					// gets <keys>
					//
					// keys: key key key key
					StringBuilder commandBuilder = new StringBuilder("gets");

					foreach (var item in de.Value)
						commandBuilder.Append(" ").Append(realToHashed[item]);

					TextSocketHelper.SendCommand(socket, commandBuilder.ToString());
				}

                Dictionary<string, ResultObj> retval = new Dictionary<string, ResultObj>(StringComparer.Ordinal);

				// process each response and build a dictionary from the results
				foreach (PooledSocket socket in sockets)
				{
					try
					{
						GetResponse r;

						while ((r = GetHelper.ReadItem(socket)) != null)
						{
							string originalKey = hashedToReal[r.Key];

                            ResultObj obj;
                            retval[originalKey] = obj = new ResultObj()
                             {
                                 cas = r.CasValue,
                                 value = this.ServerPool.Transcoder.Deserialize(r.Item)
                             };

                            if (obj.value == null)
                                someErrors = true;//TODO: PayloadFailure ?
						}
					}
					catch (NotSupportedException)
					{
						throw;
					}
					catch (Exception /*e*/)
					{
                        // TODO: php error
						//log.Error(e);
					}
				}

				this.result = retval;
			}
			finally
			{
				if (sockets != null)
					foreach (PooledSocket socket in sockets)
						((IDisposable)socket).Dispose();
			}

            return (someErrors) ? ResConstants.SomeErrors : ResConstants.Success;
		}

        public IDictionary<string, ResultObj> Result
		{
			get { return this.result; }
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
