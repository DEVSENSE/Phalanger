using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
namespace Lex
{
	public sealed class BitSet : IComparable<BitSet>
	{
		private delegate ulong BinOp(ulong x, ulong y);
		private class BitSetEnum : IEnumerator<int>, IDisposable, IEnumerator
		{
			private int idx = -1;
			private int bit = 64;
			private BitSet p;
			public int Current
			{
				get
				{
					return this.bit + (this.p.offs[this.idx] << 6);
				}
			}
			object IEnumerator.Current
			{
				get
				{
					return this.Current;
				}
			}
			public BitSetEnum(BitSet x)
			{
				this.p = x;
			}
			public void Reset()
			{
				this.idx = -1;
				this.bit = 64;
			}
			public bool MoveNext()
			{
				this.advance();
				return this.idx < this.p.inuse;
			}
			private void advance()
			{
				int num = 64;
				if (this.idx < 0)
				{
					this.idx++;
					this.bit = -1;
				}
				while (this.idx < this.p.inuse)
				{
					ulong num2 = this.p.bits[this.idx];
					while (++this.bit < num)
					{
						ulong num3 = 1uL << this.bit;
						ulong num4 = num2 & num3;
						if (num4 != 0uL)
						{
							return;
						}
					}
					this.idx++;
					this.bit = -1;
				}
			}
			public void Dispose()
			{
			}
		}
		private const int LG_BITS = 6;
		private const int BITS = 64;
		private const int BITS_M1 = 63;
		private const uint prime = 1299827u;
		private int[] offs;
		private ulong[] bits;
		private int inuse;
		public int Count
		{
			get
			{
				return this.GetLength();
			}
		}
		public BitSet()
		{
			this.bits = new ulong[4];
			this.offs = new int[4];
			this.inuse = 0;
		}
		public BitSet(int nbits) : this()
		{
		}
		public BitSet(int nbits, bool val) : this()
		{
			for (int i = 0; i < nbits; i++)
			{
				this.Set(i, val);
			}
		}
		public BitSet(BitSet set)
		{
			this.bits = new ulong[set.bits.Length];
			this.offs = new int[set.offs.Length];
			Array.Copy(set.bits, 0, this.bits, 0, set.bits.Length);
			Array.Copy(set.offs, 0, this.offs, 0, set.offs.Length);
			this.inuse = set.inuse;
		}
		private void new_block(int i, int b)
		{
			if (this.inuse == this.bits.Length)
			{
				ulong[] destinationArray = new ulong[this.inuse + 4];
				int[] destinationArray2 = new int[this.inuse + 4];
				Array.Copy(this.bits, 0, destinationArray, 0, this.inuse);
				Array.Copy(this.offs, 0, destinationArray2, 0, this.inuse);
				this.bits = destinationArray;
				this.offs = destinationArray2;
			}
			this.insert_block(i, b);
		}
		private void insert_block(int i, int b)
		{
			Array.Copy(this.bits, i, this.bits, i + 1, this.inuse - i);
			Array.Copy(this.offs, i, this.offs, i + 1, this.inuse - i);
			this.offs[i] = b;
			this.bits[i] = 0uL;
			this.inuse++;
		}
		private int BinarySearch(int[] x, int i, int m, int val)
		{
			int j = i;
			int num = m;
			while (j < num)
			{
				int num2 = (j + num) / 2;
				if (val < x[num2])
				{
					num = num2;
				}
				else
				{
					if (val <= x[num2])
					{
						return num2;
					}
					j = num2 + 1;
				}
			}
			return -j;
		}
		public void Set(int bit, bool val)
		{
			int num = bit >> 6;
			int num2 = this.BinarySearch(this.offs, 0, this.inuse, num);
			if (num2 < 0)
			{
				num2 = -num2;
				this.new_block(num2, num);
			}
			else
			{
				if (num2 >= this.inuse || this.offs[num2] != num)
				{
					this.new_block(num2, num);
				}
			}
			if (val)
			{
				this.bits[num2] |= 1uL << bit;
				return;
			}
			this.bits[num2] &= ~(1uL << bit);
		}
		public void ClearAll()
		{
			this.inuse = 0;
		}
		public bool Get(int bit)
		{
			int num = bit >> 6;
			int num2 = this.BinarySearch(this.offs, 0, this.inuse, num);
			return num2 >= 0 && num2 < this.inuse && this.offs[num2] == num && 0uL != (this.bits[num2] & 1uL << bit);
		}
		public void And(BitSet set)
		{
			this.binop(this, set, new BitSet.BinOp(BitSet.AND));
		}
		public void Or(BitSet set)
		{
			this.binop(this, set, new BitSet.BinOp(BitSet.OR));
		}
		public void Xor(BitSet set)
		{
			this.binop(this, set, new BitSet.BinOp(BitSet.XOR));
		}
		public static ulong AND(ulong x, ulong y)
		{
			return x & y;
		}
		public static ulong OR(ulong x, ulong y)
		{
			return x | y;
		}
		public static ulong XOR(ulong x, ulong y)
		{
			return x ^ y;
		}
		private void binop(BitSet a, BitSet b, BitSet.BinOp op)
		{
			int num = a.inuse + b.inuse;
			ulong[] array = new ulong[num];
			int[] array2 = new int[num];
			int num2 = 0;
			int num3 = a.bits.Length;
			int num4 = b.bits.Length;
			int num5 = 0;
			int num6 = 0;
			while (num5 < num3 || num6 < num4)
			{
				ulong num7;
				int num8;
				if (num5 < num3 && (num6 >= num4 || a.offs[num5] < b.offs[num6]))
				{
					num7 = op(a.bits[num5], 0uL);
					num8 = a.offs[num5];
					num5++;
				}
				else
				{
					if (num6 < num4 && (num5 >= num3 || a.offs[num5] > b.offs[num6]))
					{
						num7 = op(0uL, b.bits[num6]);
						num8 = b.offs[num6];
						num6++;
					}
					else
					{
						num7 = op(a.bits[num5], b.bits[num6]);
						num8 = a.offs[num5];
						num5++;
						num6++;
					}
				}
				if (num7 != 0uL)
				{
					array[num2] = num7;
					array2[num2] = num8;
					num2++;
				}
			}
			if (num2 > 0)
			{
				a.bits = new ulong[num2];
				a.offs = new int[num2];
				a.inuse = num2;
				Array.Copy(array, 0, a.bits, 0, num2);
				Array.Copy(array2, 0, a.offs, 0, num2);
				return;
			}
			this.bits = new ulong[4];
			this.offs = new int[4];
			a.inuse = 0;
		}
		public override int GetHashCode()
		{
			ulong num = 1299827uL;
			for (int i = 0; i < this.inuse; i++)
			{
				num ^= this.bits[i] * (ulong)((long)this.offs[i]);
			}
			return (int)(num >> 32 ^ num);
		}
		public int GetLength()
		{
			if (this.inuse == 0)
			{
				return 0;
			}
			return 1 + this.offs[this.inuse - 1] << 6;
		}
		public override bool Equals(object obj)
		{
			return obj != null && obj is BitSet && BitSet.Equals(this, (BitSet)obj);
		}
		public static bool Equals(BitSet a, BitSet b)
		{
			int num = 0;
			int num2 = 0;
			while (num < a.inuse || num2 < b.inuse)
			{
				if (num < a.inuse && (num2 >= b.inuse || a.offs[num] < b.offs[num2]))
				{
					if (a.bits[num++] != 0uL)
					{
						return false;
					}
				}
				else
				{
					if (num2 < b.inuse && (num >= a.inuse || a.offs[num] > b.offs[num2]))
					{
						if (b.bits[num2++] != 0uL)
						{
							return false;
						}
					}
					else
					{
						if (a.bits[num++] != b.bits[num2++])
						{
							return false;
						}
					}
				}
			}
			return true;
		}
		public int CompareTo(BitSet a)
		{
			if (this.inuse < a.inuse)
			{
				return -1;
			}
			if (this.inuse > a.inuse)
			{
				return 1;
			}
			int num = 0;
			int num2 = 0;
			while (num < this.inuse || num2 < a.inuse)
			{
				if (num < this.inuse && (num2 >= a.inuse || this.offs[num] < a.offs[num2]))
				{
					if (this.bits[num++] != 0uL)
					{
						return -1;
					}
				}
				else
				{
					if (num2 < a.inuse && (num >= this.inuse || this.offs[num] > a.offs[num2]))
					{
						if (a.bits[num2++] != 0uL)
						{
							return 1;
						}
					}
					else
					{
						long num3 = (long)(a.bits[num2++] - this.bits[num++]);
						if (num3 < 0L)
						{
							return -1;
						}
						if (num3 > 0L)
						{
							return 1;
						}
					}
				}
			}
			return 0;
		}
		public IEnumerator<int> GetEnumerator()
		{
			return new BitSet.BitSetEnum(this);
		}
		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append('{');
			foreach (int current in this)
			{
				if (stringBuilder.Length > 1)
				{
					stringBuilder.Append(", ");
				}
				stringBuilder.Append(current);
			}
			stringBuilder.Append('}');
			return stringBuilder.ToString();
		}
	}
}
