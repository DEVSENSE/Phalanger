using System;
using System.Collections.Generic;

using PHP.Library.Memcached;
using PHP.Core;

namespace Enyim.Caching.Memcached.Operations
{
    /// <summary>
	/// Base class for implementing operations.
	/// </summary>
	internal abstract class Operation : IDisposable
	{
		//private static log4net.ILog log = log4net.LogManager.GetLogger(typeof(Operation));

		private bool isDisposed;
		private IServerPool serverPool;

        protected ResConstants pendingError = ResConstants.Success;   // an error set in .ctor and checked within the Execute method, if not Success, the code is return immediately.

		protected Operation(IServerPool serverPool)
		{
            if (serverPool == null || serverPool.ServersCount == 0)
                pendingError = ResConstants.NoServers;

			this.serverPool = serverPool;
		}

        public ResConstants Execute()
		{
            // error already occurred within class constructor (bad key, or another parameter?), return it
            if (pendingError != ResConstants.Success)
            {
                //PhpException.Throw(PhpError.Notice, "DEBUG: Preconditions failed, " + pendingError);
                return pendingError;
            }

            // process the request
			try
			{
                if (this.CheckDisposed(false))
                {
                    //PhpException.Throw(PhpError.Notice, "DEBUG: Operation already Disposed, invalid internal usage, CLIENT_ERROR");
                    return ResConstants.ClientError;
                }

                // do the action
                //PhpException.Throw(PhpError.Notice, "DEBUG: Executing the action ...");

				return this.ExecuteAction();
			}
			catch (NotSupportedException /*e*/)
			{
                //PhpException.Throw(PhpError.Notice, e.Message + "\n" + e.StackTrace);
				throw;
			}
			catch (Exception e)
			{
				//log.Error(e);
                PhpException.Throw(PhpError.Notice, e.Message + "\n" + e.StackTrace);
			}

            return ResConstants.ClientError;
		}

		protected IServerPool ServerPool
		{
			get { return this.serverPool; }
		}

        protected abstract ResConstants ExecuteAction();

		protected bool CheckDisposed(bool throwOnError)
		{
			if (throwOnError && this.isDisposed)
				throw new ObjectDisposedException("Operation");

			return this.isDisposed;
		}

		/// <summary>
		/// Maps each key in the list to a MemcachedNode.
        /// If the serverKey is not empty, it's used instead of keys to locate the server node.
		/// </summary>
		/// <param name="keys"></param>
        /// <param name="serverKey">Use this key instead of keys to locate the proper server node.</param>
		/// <returns></returns>
        protected Dictionary<IMemcachedNode, List<string>> SplitKeys(IEnumerable<string> keys, string serverKey)
		{
			var retval = new Dictionary<IMemcachedNode, List<string>>(MemcachedNode.Comparer.Instance);
			var kt = this.serverPool.KeyTransformer;
			var locator = this.serverPool.NodeLocator;

            if (string.IsNullOrEmpty(serverKey))
            {
                foreach (var key in keys)
                {
                    var node = locator.Locate(kt.Transform(key));
                    if (node != null)
                    {
                        List<string> list;

                        if (!retval.TryGetValue(node, out list))
                            retval[node] = list = new List<string>();

                        list.Add(key);
                    }
                }
            }
            else
            {   // use only one server node corresponding to serverKey

                var node = locator.Locate(kt.Transform(serverKey));
                if (node != null)
                {
                    List<string> list = new List<string>();
                    retval[node] = list;

                    foreach (var key in keys)
                    {
                        list.Add(key);
                    }
                }
            }

			return retval;
		}

		~Operation()
		{
			try { ((IDisposable)this).Dispose(); }
			catch { }
		}

		#region [ IDisposable                  ]
		public virtual void Dispose()
		{
			this.isDisposed = true;
		}

		void IDisposable.Dispose()
		{
			this.Dispose();
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
