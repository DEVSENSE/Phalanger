using System;
using System.Diagnostics;

/*
  
  Designed and implemented by Tomas Matousek.
  
*/

namespace PHP
{
	/// <summary>
	/// Summary description for Utils.
	/// </summary>
	public class Misc
	{
		private Misc() { }

		/// <summary>
		/// Absolutizes range specified by an offset and a length relatively to a dimension of an array.
		/// </summary>
		/// <param name="count">The number of items in array.</param>
		/// <param name="offset">
		/// The offset of the range relative to the beginning (if non-negative) or the end of the array (if negative).
		/// If the offset underflows or overflows the dimension of array it is trimmed appropriately.
		/// </param>
		/// <param name="length">
		/// The length of the range if non-negative. Otherwise, its absolute value is the number of items
		/// which will not be included in the range from the end of the array. In the latter case 
		/// the range ends with the |<paramref name="length"/>|-th item from the end of the array (counting from zero).
		/// </param>
		public static void AbsolutizeRange(ref int offset, ref int length, int count)
		{
			// prevents overflows:
			if (offset >= count)
			{
				offset = count;
				length = 0;
				return;
			}

			// negative offset => offset is relative to the end of the string:
			if (offset < 0)
			{
				offset += count;
				if (offset < 0) offset = 0;
			}

			Debug.Assert(offset >= 0 && offset < count);

			if (length < 0)
			{
				// there is count-offset items from offset to the end of array,
				// the last |length| items is taken away:
				length = count - offset + length;
				if (length < 0) length = 0;
			}
			else
				if ((long)offset + length > count)
				{
					// interval ends on the end of array:
					length = count - offset;
				}

			Debug.Assert(length >= 0 && offset + length <= count);
		}


	}
}
