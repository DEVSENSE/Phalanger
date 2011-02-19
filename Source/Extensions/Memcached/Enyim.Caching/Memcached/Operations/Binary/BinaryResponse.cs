using System;
using System.Text;
using PHP.Library.Memcached;

namespace Enyim.Caching.Memcached.Operations.Binary
{
	internal class BinaryResponse
	{
		//private static log4net.ILog log = log4net.LogManager.GetLogger(typeof(BinaryResponse));

		private const byte MAGIC_VALUE = 0x81;
		private const int HEADER_OPCODE = 1;
		private const int HEADER_KEY = 2; // 2-3
		private const int HEADER_EXTRA = 4;
		private const int HEADER_DATATYPE = 5;
		private const int HEADER_STATUS = 6; // 6-7
		private const int HEADER_BODY = 8; // 8-11
		private const int HEADER_OPAQUE = 12; // 12-15
		private const int HEADER_CAS = 16; // 16-23

		public byte Opcode;
		public int KeyLength;
		public byte DataType;
		public ResponseStatus StatusCode;

		public int CorrelationId;
		public ulong CAS;

		public ArraySegment<byte> Extra;
		public ArraySegment<byte> Data;

		public unsafe ResConstants Read(PooledSocket socket)
		{
			if (socket.IsAlive)
            {
                if (socket.Available == 0)
                {// TODO: nothing to read, maybe Quiet command was used, maybe server is wrong

                }

                byte[] header = new byte[24];
                socket.Read(header, 0, 24);

                fixed (byte* buffer = header)
                {
                    if (buffer[0] != MAGIC_VALUE)
                        throw new InvalidOperationException("Expected magic value " + MAGIC_VALUE + ", received: " + buffer[0]);

                    int remaining = BinaryConverter.DecodeInt32(buffer, HEADER_BODY);
                    int extraLength = buffer[HEADER_EXTRA];

                    byte[] data = new byte[remaining];
                    socket.Read(data, 0, remaining);

                    this.Extra = new ArraySegment<byte>(data, 0, extraLength);
                    this.Data = new ArraySegment<byte>(data, extraLength, data.Length - extraLength);

                    this.DataType = buffer[HEADER_DATATYPE];
                    this.Opcode = buffer[HEADER_OPCODE];
                    this.StatusCode = (ResponseStatus)BinaryConverter.DecodeInt16(buffer, HEADER_STATUS);

                    this.KeyLength = BinaryConverter.DecodeInt16(buffer, HEADER_KEY);
                    this.CorrelationId = BinaryConverter.DecodeInt32(buffer, HEADER_OPAQUE);
                    this.CAS = BinaryConverter.DecodeUInt64(buffer, HEADER_CAS);
                }
            }
            else
            {
                this.StatusCode = ResponseStatus.Undefined;
                return ResConstants.UnknownReadFalure;
            }

            //
            return StatusCodeResult(this.StatusCode);
		}

        /// <summary>
        /// Convert binary status code into PHP memcached result.
        /// </summary>
        /// <param name="statusCode"></param>
        /// <returns></returns>
        internal static ResConstants StatusCodeResult(ResponseStatus statusCode)
        {
            switch (statusCode)
            {
                case ResponseStatus.NoError:
                    return ResConstants.Success;
                case ResponseStatus.KeyNotFound:
                    return ResConstants.NotFound;
                case ResponseStatus.OutOfMemory:
                    return ResConstants.ServerError;
                case ResponseStatus.ValueTooLarge:
                    return ResConstants.ProtocolError;
                case ResponseStatus.IncrDecrOnNonNumericValue:
                    return ResConstants.Failure;
                case ResponseStatus.InvalidArguments:
                    return ResConstants.ProtocolError;
                case ResponseStatus.ItemNotStored:
                    return ResConstants.NotStored;
                case ResponseStatus.KeyExists:
                    return ResConstants.DataExists;
                case ResponseStatus.UnknownCommand:
                    return ResConstants.ProtocolError;
                default:
                    return ResConstants.ProtocolError;
            }

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
