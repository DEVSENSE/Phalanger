using System;
using System.IO;
using System.IO.Compression;

namespace Enyim.Caching.Memcached
{
    /// <summary>
	/// Provides an interface for serializing items for Memcached.
	/// </summary>
    public abstract class TranscoderBase
    {
        /// <summary>
        /// Indicates if the compression within Serialization process is enabled. Default is false (not explicitly set).
        /// </summary>
        public bool Compression { get; set; }

        /// <summary>
        /// Determines if the given item has enabled compression.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private bool IsCompressed(CacheItem item)
        {
            return ((item.Flags & (ushort)CacheItemFlags.Compression) != 0);
        }

        #region GZipStream usage

        private byte[] Compress(byte[] array, int offset, int count)
        {
            var outputStream = new MemoryStream();
            using (var zipStream = new GZipStream(outputStream, CompressionMode.Compress, true))
            {
                zipStream.Write(array, offset, count);
            }
            outputStream.Position = 0;
            byte[] compressed = new byte[outputStream.Length];
            outputStream.Read(compressed, 0, compressed.Length);

            return compressed;
        }

        private byte[] Uncompress(byte[] array, int offset, int count)
        {
            var inputStream = new MemoryStream(array, offset, count, false);
            var outputStream = new MemoryStream();

            using (var zipStream = new GZipStream(inputStream, CompressionMode.Decompress))
            {
                byte[] buffer = new byte[128];
                int numRead;
                while ((numRead = zipStream.Read(buffer, 0, buffer.Length)) != 0)
                {
                    outputStream.Write(buffer, 0, numRead);
                }
            }

            outputStream.Position = 0;
            byte[] compressed = new byte[outputStream.Length];
            outputStream.Read(compressed, 0, compressed.Length);

            return compressed;
        }

        #endregion

        /// <summary>
        /// Serializes an object for storing in the cache.
        /// </summary>
        /// <param name="o">The object to serialize</param>
        /// <returns>The serialized object</returns>
        /// <remarks>Method can throw an exception, then the RES_PAYLOAD_FAILURE should be returned by caller method.</remarks>
        public CacheItem Serialize(object o)
        {
            // serializes the object
            CacheItem item = DoSerialize(o, Compression);

            // perform GZIP compression, if enabled and data are long enough
            if (IsCompressed(item))
            {
                item.Data = new ArraySegment<byte>(Compress(item.Data.Array, item.Data.Offset, item.Data.Count));
            }

            return item;
        }

        /// <summary>
        /// De-serializes the <see cref="T:CacheItem"/> into an object.
        /// </summary>
        /// <param name="item">The stream that contains the data to de-serialize.</param>
        /// <returns>The deserialized object</returns>
        /// <remarks>Method can throw an exception, then the RES_PAYLOAD_FAILURE should be returned by caller method.</remarks>
        public object Deserialize(CacheItem item)
        {
            // uncompress data first
            if (IsCompressed(item))
            {
                item.Data = new ArraySegment<byte>(Uncompress(item.Data.Array, item.Data.Offset, item.Data.Count));
            }

            // de-serializes the data
            return DoDeserialize(item);
        }

        /// <summary>
        /// Perform serialization.
        /// </summary>
        /// <param name="o">Object to serialize.</param>
        /// <param name="compressionFlagAllowed">Allow compression.</param>
        /// <returns></returns>
        protected abstract CacheItem DoSerialize(Object o, bool compressionFlagAllowed);

        /// <summary>
        /// Perform deserialization.
        /// </summary>
        /// <param name="item">Item to be deserialized.</param>
        /// <returns>Deserialized object.</returns>
        protected abstract object DoDeserialize(CacheItem item);
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
