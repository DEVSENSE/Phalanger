using PHP.Library.Memcached;

namespace Enyim.Caching.Memcached.Operations.Text
{
	internal sealed class FlushOperation : Operation
	{
        private readonly int delay;
        private readonly bool noreply;

        public FlushOperation(IServerPool pool, int delay)
            : base(pool)
        {
            this.delay = delay;
            this.noreply = false;
        }

		protected override ResConstants ExecuteAction()
		{
			foreach (IMemcachedNode server in this.ServerPool.GetServers())
			{
				if (!server.IsAlive) continue;

				using (PooledSocket socket = server.Acquire())
				{
					if (socket != null)
					{
                        // request
                        string command = "flush_all";

                        if (delay > 0)  command += " " + delay.ToString();
                        if (noreply) command += " noreply";
                        
                        TextSocketHelper.SendCommand(socket, command);

                        // response
                        if (!noreply)
                        {
                            string response = TextSocketHelper.ReadResponse(socket); // No-op the response to avoid data hanging around.

                            // ignore response ...
                            //if (string.Compare(response, "OK", System.StringComparison.Ordinal) != 0)
                            //    return GetHelper.HandleResponse(response);
                        }
					}
				}
			}

			return ResConstants.Success;
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
