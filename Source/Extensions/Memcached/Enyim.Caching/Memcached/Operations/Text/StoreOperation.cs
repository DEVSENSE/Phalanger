using System;
using System.Globalization;
using System.Text;
using PHP.Library.Memcached;

namespace Enyim.Caching.Memcached.Operations.Text
{
	internal class StoreOperation : ItemOperation
	{
		private static readonly ArraySegment<byte> DataTerminator = new ArraySegment<byte>(new byte[2] { (byte)'\r', (byte)'\n' });
		private StoreCommand mode;
		private object value;
		private uint expires;

        internal StoreOperation(IServerPool pool, StoreCommand mode, string key, string serverKey, object value, uint expires)
			: base(pool, key, serverKey, TextProtocol.MaxKeyLength)
		{
            this.mode = mode;
			this.value = value;
			this.expires = expires;
		}

		protected override ResConstants ExecuteAction()
		{
            //PHP.Core.PhpException.Throw(PHP.Core.PhpError.Notice, "DEBUG: Text.STORE: Acquiring socket, ...");

            PooledSocket socket = this.Socket;
			if (socket == null)
				return ResConstants.ConnectionSocketCreateFailure;

            //PHP.Core.PhpException.Throw(PHP.Core.PhpError.Notice, "DEBUG: Text.STORE: Socked ok, serializing value ...");

            // serialize item
			CacheItem item;

            try{ item = this.ServerPool.Transcoder.Serialize(this.value); }
            catch { return ResConstants.PayloadFailure; }

            //PHP.Core.PhpException.Throw(PHP.Core.PhpError.Notice, "DEBUG: Text.STORE: Constructing message ...");

			ushort flag = item.Flags;
			ArraySegment<byte> data = item.Data;

			// todo adjust the size to fit a request using a fnv hashed key
			StringBuilder sb = new StringBuilder(128);

			switch (mode)
			{
				case StoreCommand.Add:
					sb.Append("add ");
					break;
				case StoreCommand.Replace:
					sb.Append("replace ");
					break;
				case StoreCommand.Set:
					sb.Append("set ");
					break;

				case StoreCommand.Append:
					sb.Append("append ");
					break;

				case StoreCommand.Prepend:
					sb.Append("prepend ");
					break;

				case StoreCommand.CheckAndSet:
					sb.Append("cas ");
					break;

				default:
					throw new MemcachedClientException(mode + " is not supported.");
			}

			sb.Append(this.HashedKey);
			sb.Append(" ");
			sb.Append(flag.ToString(CultureInfo.InvariantCulture));
			sb.Append(" ");
			sb.Append(expires.ToString(CultureInfo.InvariantCulture));
			sb.Append(" ");
			sb.Append(Convert.ToString(data.Count - data.Offset, CultureInfo.InvariantCulture));

			if (mode == StoreCommand.CheckAndSet)
			{
				sb.Append(" ");
				sb.Append(Convert.ToString(this.Cas, CultureInfo.InvariantCulture));
			}

			ArraySegment<byte> commandBuffer = TextSocketHelper.GetCommandBuffer(sb.ToString());

            //PHP.Core.PhpException.Throw(PHP.Core.PhpError.Notice, "DEBUG: Text.STORE: Command prepared, writing to the socket ...");

			socket.Write(new ArraySegment<byte>[] { commandBuffer, data, StoreOperation.DataTerminator });

            //PHP.Core.PhpException.Throw(PHP.Core.PhpError.Notice, "DEBUG: Text.STORE: Command sent, reading response from the server ...");

            //
            string response = TextSocketHelper.ReadResponse(socket);

            //PHP.Core.PhpException.Throw(PHP.Core.PhpError.Notice, "DEBUG: Text.STORE: Response from the server read, " + response);

            if (String.Compare(response, "STORED", StringComparison.Ordinal) == 0)
                return ResConstants.Success;

            if (String.Compare(response, "NOT_STORED", StringComparison.Ordinal) == 0)
                return ResConstants.NotStored;

            if (String.Compare(response, "EXISTS", StringComparison.Ordinal) == 0)
                return ResConstants.DataExists;

            if (String.Compare(response, "NOT_FOUND", StringComparison.Ordinal) == 0)
                return ResConstants.NotFound;

            //
			return GetHelper.HandleResponse(response);
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
