using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Diagnostics;

using PHP.Library;
using PHP.Library.Memcached;

namespace Enyim.Caching.Memcached
{
	/// <summary>
	/// Default <see cref="T:ITranscoder"/> implementation. Primitive types are manually serialized, the rest is serialized using <see cref="T:BinarySerializer"/>.
	/// </summary>
    public sealed class PhpTranscoder : TranscoderBase
	{
        /// <summary>
        /// Method used to serialize the values.
        /// </summary>
        public SerializerConstants Serializer
        {
            get
            {
                return _serializer;
            }
            set
            {
                switch (value)
                {
                    // currently supported serializers
                    case SerializerConstants.Php:
                    case SerializerConstants.JSON:
                        this._serializer = value;
                        return; // ok

                    // otherwise unsupported
                    default:
                        throw new ArgumentOutOfRangeException("serializer");
                }
            }
        }
        private SerializerConstants _serializer = SerializerConstants.Php;

        /// <summary>
        /// Data below this size are not compressed.
        /// </summary>
        private const int CompressThreshold = 100;   // 100 is in PHP, but GZipStream is more efficient for longer data

        /// <summary>
        /// Add compression flag if the data meet the condition
        /// </summary>
        /// <param name="item"></param>
        /// <param name="compressionFlagAllowed">Is the compression in allowed.</param>
        private void AddCompressionEventually(CacheItem item, bool compressionFlagAllowed)
        {
            if (compressionFlagAllowed && item.Data.Count > CompressThreshold)
                item.Flags |= (ushort)CacheItemFlags.Compression;
        }

        protected override CacheItem DoSerialize(object value, bool compressionFlagAllowed)
		{
            // note: numbers must not be corrupted using PHP serialization (otherwise inc/decr will not work)

            string strval;

            if ( (strval = value as string) != null )
            {
                CacheItem item = new CacheItem(
                        (ushort)CacheItemFlags.TypeString,
                        new ArraySegment<byte>(System.Text.UTF8Encoding.UTF8.GetBytes(strval)));

                AddCompressionEventually(item, compressionFlagAllowed);

                return item;
            }
            else if (value is int || value is long)
            {
                return new CacheItem(
                        (ushort)CacheItemFlags.TypeLong,
                        new ArraySegment<byte>(System.Text.ASCIIEncoding.ASCII.GetBytes(value.ToString())));
            }
            else if (value is double)
            {
                return new CacheItem(
                        (ushort)CacheItemFlags.TypeDouble,
                        new ArraySegment<byte>(System.Text.ASCIIEncoding.ASCII.GetBytes(value.ToString())));
            }
            else if (value is bool)
            {
                return new CacheItem(
                        (ushort)CacheItemFlags.TypeBool,
                        new ArraySegment<byte>(
                            ((bool)value) ? (new byte[] { (byte)'1' }) : new byte[] { (byte)'0' }
                            ));
            }
            else
            {
                CacheItem item;

                switch (Serializer)
                {
                    case SerializerConstants.Php:
                        item = new CacheItem((ushort)CacheItemFlags.TypeSerialized, new ArraySegment<byte>(PhpSerializer.Default.Serialize(value, new PHP.Core.Reflection.UnknownTypeDesc()).Data));
                        break;
                    case SerializerConstants.JSON:
                        item = new CacheItem((ushort)CacheItemFlags.TypeJSON, new ArraySegment<byte>(PhpJsonSerializer.Default.Serialize(value, new PHP.Core.Reflection.UnknownTypeDesc()).Data));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException("Serializer");
                }   

                AddCompressionEventually(item, compressionFlagAllowed);
                
                return item;
            }
		}

        private static string DeserializeString(CacheItem item, System.Text.Encoding encoding)
        {
            Debug.Assert(item.Data.Offset == 0 && item.Data.Count == item.Data.Array.Length);

            return encoding.GetString(item.Data.Array);
        }

        protected override object DoDeserialize(CacheItem item)
        {
            try
            {
                switch ((CacheItemFlags)(item.Flags & (ushort)CacheItemFlags.TypeMask))
                {
                    case CacheItemFlags.TypeString:
                        return DeserializeString(item, System.Text.UTF8Encoding.UTF8);
                    case CacheItemFlags.TypeLong:
                        {
                            string value = DeserializeString(item, System.Text.ASCIIEncoding.ASCII);
                            try { return int.Parse(value); }
                            catch (OverflowException) { return long.Parse(value); }
                        }
                    case CacheItemFlags.TypeDouble:
                        return double.Parse(DeserializeString(item, System.Text.ASCIIEncoding.ASCII));
                    case CacheItemFlags.TypeBool:
                        return (bool)(item.Data.Count > 0 && item.Data.Array[item.Data.Offset] == '1');
                    case CacheItemFlags.TypeSerialized:
                        return PhpSerializer.Default.Deserialize(new PHP.Core.PhpBytes(item.Data.Array), new PHP.Core.Reflection.UnknownTypeDesc());
                    case CacheItemFlags.TypeJSON:
                        return PhpJsonSerializer.Default.Deserialize(new PHP.Core.PhpBytes(item.Data.Array), new PHP.Core.Reflection.UnknownTypeDesc());
                    default:
                        return null;
                }
            }
            catch
            {
                return null;
            }
        }
	}
}
