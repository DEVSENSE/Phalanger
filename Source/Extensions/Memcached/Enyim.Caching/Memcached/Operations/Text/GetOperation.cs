using PHP.Library.Memcached;

namespace Enyim.Caching.Memcached.Operations.Text
{
	internal class GetOperation : ItemOperation
	{
        public object Result { get; private set; }
        
		internal GetOperation(IServerPool pool, string key, string serverKey)
			: base(pool, key, serverKey, TextProtocol.MaxKeyLength)
		{
		}

		protected override ResConstants ExecuteAction()
		{
			PooledSocket socket = this.Socket;

			if (socket == null)
                return ResConstants.ConnectionSocketCreateFailure;

			TextSocketHelper.SendCommand(socket, "get " + this.HashedKey);

			GetResponse r = GetHelper.ReadItem(socket);

			if (r == null)
                return ResConstants.End;

            this.Cas = r.CasValue;
            this.Result = this.ServerPool.Transcoder.Deserialize(r.Item);
			GetHelper.FinishCurrent(socket);

            if (this.Result == null)
                return ResConstants.PayloadFailure;

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
