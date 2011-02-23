
namespace Enyim.Hashes
{
	/// <summary>
	/// Combines multiple hash codes into one.
	/// </summary>
	public class HashCodeCombiner
	{
		private int currentHash;

        /// <summary>
        /// Initialize the combiner with a default value.
        /// </summary>
		public HashCodeCombiner() : this(0x1505) { }

        /// <summary>
        /// Initialize the combiner.
        /// </summary>
        /// <param name="initialValue"></param>
		public HashCodeCombiner(int initialValue)
		{
			this.currentHash = initialValue;
		}

        /// <summary>
        /// Combine two values.
        /// </summary>
        /// <param name="code1"></param>
        /// <param name="code2"></param>
        /// <returns></returns>
		public static int Combine(int code1, int code2)
		{
			return ((code1 << 5) + code1) ^ code2;
		}

        /// <summary>
        /// Combine current value with given value.
        /// </summary>
        /// <param name="value"></param>
		public void Add(int value)
		{
			this.currentHash = HashCodeCombiner.Combine(this.currentHash, value);
		}

        /// <summary>
        /// Current value.
        /// </summary>
		public int CurrentHash
		{
			get { return this.currentHash; }
		}

        /// <summary>
        /// Combine three values.
        /// </summary>
        /// <param name="code1"></param>
        /// <param name="code2"></param>
        /// <param name="code3"></param>
        /// <returns></returns>
		public static int Combine(int code1, int code2, int code3)
		{
			return HashCodeCombiner.Combine(HashCodeCombiner.Combine(code1, code2), code3);
		}

        /// <summary>
        /// Combine four values.
        /// </summary>
        /// <param name="code1"></param>
        /// <param name="code2"></param>
        /// <param name="code3"></param>
        /// <param name="code4"></param>
        /// <returns></returns>
		public static int Combine(int code1, int code2, int code3, int code4)
		{
			return HashCodeCombiner.Combine(HashCodeCombiner.Combine(code1, code2), HashCodeCombiner.Combine(code3, code4));
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
