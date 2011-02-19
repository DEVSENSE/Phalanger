using System;
using System.Linq;
using System.Collections.Generic;
using PHP.Library.Memcached;

namespace Enyim.Caching.Memcached.Operations.Binary
{
	internal class MultiGetOperation : Operation
	{
        private readonly string serverKey;
        private readonly IEnumerable<string> keys;

        private Dictionary<string, ResultObj> result;

		public MultiGetOperation(IServerPool pool, IEnumerable<string> keys, string serverKey)
			: base(pool)
		{
            this.serverKey = serverKey;
			this.keys = keys;
		}

        public Dictionary<string, ResultObj> Result
		{
			get { return this.result; }
		}

		protected override ResConstants ExecuteAction()
		{
			// 1. map each key to a node
			// 2. build itemCount * GetQ buffer, and close it with NoOp to get the responses
			// 3. read the response of each node
			// 4. merge the responses into a dictionary

			// map each key to the appropriate server in the pool
			var splitKeys = this.SplitKeys(this.keys, this.serverKey);

            if (splitKeys == null || splitKeys.Count == 0)
                return ResConstants.NoServers;

			// we'll open 1 socket for each server
			var mgets = new List<MGetSession>(splitKeys.Count);

			var idmap = new Dictionary<string, int>();

            foreach (var group in splitKeys)
			{
				// HACK this will transform the keys again, we should precalculate them and pass to the getter
				var mg = new MGetSession(this.ServerPool, group.Key, group.Value);

				try
				{
					if (mg.Write()) mgets.Add(mg);
					mg = null;
				}
				finally
				{
					if (mg != null) ((IDisposable)mg).Dispose();
				}
			}

            var retval = new Dictionary<string, ResultObj>(StringComparer.Ordinal);

			// process each response and build a dictionary from the results
            bool someError = false;

			foreach (var mg in mgets)
				using (mg)
				{
					var results = mg.Read();
                    foreach (var de in results)
                        retval[de.Key] = de.Value;

                    //
                    someError |= mg.SomeError;
				}

            //
			this.result = retval;

            return (someError) ? ResConstants.SomeErrors : ResConstants.Success;
		}

		#region [ MGetSession                  ]
		/// <summary>
		/// Handles the MultiGet against a node
		/// </summary>
		private class MGetSession : IDisposable
		{
			private IServerPool pool;
			private IMemcachedNode node;
			private List<string> keys;
			private PooledSocket socket;

            public bool SomeError { get; private set; }

			public MGetSession(IServerPool pool, IMemcachedNode node, List<string> keys)
			{
				this.pool = pool;
				this.node = node;
				this.keys = keys;

                this.SomeError = false;
			}

			private Dictionary<int, string> requestedItemMap = new Dictionary<int, string>();
			private int lastId;

			public bool Write()
			{
				if (!this.node.IsAlive) return false;

				this.socket = this.node.Acquire();
				
				// exit early if the node is dead
				if (this.socket == null || !this.socket.IsAlive) return false;

				var transformer = this.pool.KeyTransformer;
				var buffers = new List<ArraySegment<byte>>();

				// build a GetQ for each key
				foreach (string realKey in this.keys)
				{
					string hashedKey = transformer.Transform(realKey);

					var request = new BinaryRequest(OpCode.GetQ);
					request.Key = hashedKey;

					// store the request's id so later we can find 
					// out whihc response is which item
					// this way we do not have to use GetKQ
					// (whihc sends back the item's key with the data)
					requestedItemMap[request.CorrelationId] = realKey;
					buffers.AddRange(request.CreateBuffer());
				}

				// noop forces the server to send the responses of the 
				// previous quiet commands
				var noop = new BinaryRequest(OpCode.NoOp);

				// noop always succeeds so we'll read until we get the noop's response
				this.lastId = noop.CorrelationId;
				buffers.AddRange(noop.CreateBuffer());

				try
				{
					this.socket.Write(buffers);

					// if the write failed the Read() will be skipped
					return this.socket.IsAlive;
				}
				catch
				{
					// write error most probably
                    SomeError = true;
					return false;
				}
			}

            public IDictionary<string, ResultObj> Read()
			{
				var response = new BinaryResponse();
                var retval = new Dictionary<string, ResultObj>();
				var transcoder = this.pool.Transcoder;

				try
				{
					while (true)
					{
						// if nothing else, the noop will succeed
                        if (response.Read(this.socket) == ResConstants.Success)
                        {
                            // found the noop, quit
                            if (response.CorrelationId == this.lastId) return retval;

                            string key;

                            // find the key to the response
                            if (!this.requestedItemMap.TryGetValue(response.CorrelationId, out key))
                            {
                                // we're not supposed to get here tho
                                // TODO: last err no
                                //log.WarnFormat("Found response with CorrelationId {0}, but no key is matching it.", response.CorrelationId);
                                continue;
                            }

                            //if (log.IsDebugEnabled) log.DebugFormat("Reading item {0}", key);

                            // deserialize the response
                            int flags = BinaryConverter.DecodeInt32(response.Extra, 0);
                            ResultObj obj;
                            retval[key] = obj = new ResultObj()
                            {
                                value = transcoder.Deserialize(new CacheItem((ushort)flags, response.Data)),
                                cas = response.CAS
                            };

                            if (obj.value == null)
                                SomeError = true;   // TODO: PayloadFailure ?
                        }
                        else
                        {
                            SomeError = true;
                        }

					}
				}
				catch
				{
					// read failed, return the items we've read so far
					return retval;
				}
			}

			void IDisposable.Dispose()
			{
				GC.SuppressFinalize(this);

				if (this.socket == null) return;

				try
				{
					((IDisposable)this.socket).Dispose();
					this.socket = null;
				}
				catch
				{ }
			}

			//private static log4net.ILog log = log4net.LogManager.GetLogger(typeof(MGetSession));

		}
		#endregion
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
