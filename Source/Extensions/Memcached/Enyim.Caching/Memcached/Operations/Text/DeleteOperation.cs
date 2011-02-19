using System;
using PHP.Library.Memcached;

namespace Enyim.Caching.Memcached.Operations.Text
{
	internal sealed class DeleteOperation : ItemOperation
	{
        private readonly int delay;
        private readonly bool noreply;

        internal DeleteOperation(IServerPool pool, string key, string serverKey, int delay)
            : base(pool, key, serverKey, TextProtocol.MaxKeyLength)
		{
            this.delay = delay;
            this.noreply = false;
		}

		protected override ResConstants ExecuteAction()
		{
			PooledSocket socket = this.Socket;
			if (socket == null)
                return ResConstants.ConnectionSocketCreateFailure;

            // request
            string command = "delete " + this.HashedKey;
            if (delay > 0) command += " " + delay.ToString();   // server version should be checked, if delay is possible (version 1.2+), otherwise INVALID_ARGUMENT
            if (noreply) command += " noreply";

			TextSocketHelper.SendCommand(socket, command);

            // response
            if (!noreply)
            {
                string response = TextSocketHelper.ReadResponse(socket);

                if (String.Compare(response, "DELETED", StringComparison.Ordinal) == 0)
                    return ResConstants.Success;
                if (String.Compare(response, "NOT_FOUND", StringComparison.Ordinal) == 0)
                    return ResConstants.NotFound;

                //
                return GetHelper.HandleResponse(response);
            }
            else
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
