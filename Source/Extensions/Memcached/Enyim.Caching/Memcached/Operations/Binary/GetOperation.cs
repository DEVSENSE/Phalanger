using PHP.Library.Memcached;

namespace Enyim.Caching.Memcached.Operations.Binary
{
	internal class GetOperation : ItemOperation
	{
        public GetOperation(IServerPool pool, string key, string serverKey)
            :base(pool, key, serverKey, BinaryProtocol.MaxKeyLength) 
        {
        }

		protected override ResConstants ExecuteAction()
		{
			PooledSocket socket = this.Socket;
            if (socket == null)
                return ResConstants.ConnectionSocketCreateFailure;

			BinaryRequest request = new BinaryRequest(OpCode.Get);
			request.Key = this.HashedKey;
			request.Write(socket);

			BinaryResponse response = new BinaryResponse();

            var code = response.Read(socket);

            if (code == ResConstants.Success)
            {
                int flags = BinaryConverter.DecodeInt32(response.Extra, 0);

                this.Cas = response.CAS;
                this.Result = this.ServerPool.Transcoder.Deserialize(new CacheItem((ushort)flags, response.Data));

                if (this.Result == null)
                    return ResConstants.PayloadFailure;
            }

            return code;
		}

        public object Result { get; private set; }

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
