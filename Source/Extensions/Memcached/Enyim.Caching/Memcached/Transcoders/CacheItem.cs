using System;

namespace Enyim.Caching.Memcached
{
    /* // flags from PHP
    #define MEMC_VAL_IS_STRING     0
    #define MEMC_VAL_IS_LONG       1
    #define MEMC_VAL_IS_DOUBLE     2
    #define MEMC_VAL_IS_BOOL       3
    #define MEMC_VAL_IS_SERIALIZED 4
    #define MEMC_VAL_IS_IGBINARY   5
    #define MEMC_VAL_IS_JSON       6

    #define MEMC_VAL_COMPRESSED    (1<<4)
    */

    /// <summary>
    /// Common CacheItem flag values.
    /// </summary>
    [Flags]
    public enum CacheItemFlags : ushort
    {
        //
        // type mask
        //
        TypeString = 0,
        TypeLong = 1,
        TypeDouble = 2,
        TypeBool = 3,
        TypeSerialized = 4,
        //TypeIgBinary=5,
        TypeJSON=6,

        TypeMask = 0xf,

        //
        // other masks
        //
        Compression = 1 << 4,
    }

	/// <summary>
	/// Represents an object either being retrieved from the cache
	/// or being sent to the cache.
	/// </summary>
	public struct CacheItem
	{
		private ArraySegment<byte> data;
		private ushort flags;

		/// <summary>
		/// Initializes a new instance of <see cref="T:CacheItem"/>.
		/// </summary>
		/// <param name="flags">Custom item data.</param>
		/// <param name="data">The serialized item.</param>
		public CacheItem(ushort flags, ArraySegment<byte> data)
		{
			this.data = data;
			this.flags = flags;
		}

		/// <summary>
		/// The data representing the item being stored/retrieved.
		/// </summary>
		public ArraySegment<byte> Data
		{
			get { return this.data; }
			set { this.data = value; }
		}

		/// <summary>
		/// Flags set for this instance.
		/// </summary>
		public ushort Flags
		{
			get { return this.flags; }
			set { this.flags = value; }
		}
	}
}