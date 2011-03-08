namespace zlib
{
    using System;

    internal sealed class Tree
    {
        internal static readonly byte[] _dist_code = new byte[] { 
            0, 1, 2, 3, 4, 4, 5, 5, 6, 6, 6, 6, 7, 7, 7, 7, 
            8, 8, 8, 8, 8, 8, 8, 8, 9, 9, 9, 9, 9, 9, 9, 9, 
            10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 
            11, 11, 11, 11, 11, 11, 11, 11, 11, 11, 11, 11, 11, 11, 11, 11, 
            12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 
            12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 
            13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 
            13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 
            14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 
            14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 
            14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 
            14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 
            15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 
            15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 
            15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 
            15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 
            0, 0, 0x10, 0x11, 0x12, 0x12, 0x13, 0x13, 20, 20, 20, 20, 0x15, 0x15, 0x15, 0x15, 
            0x16, 0x16, 0x16, 0x16, 0x16, 0x16, 0x16, 0x16, 0x17, 0x17, 0x17, 0x17, 0x17, 0x17, 0x17, 0x17, 
            0x18, 0x18, 0x18, 0x18, 0x18, 0x18, 0x18, 0x18, 0x18, 0x18, 0x18, 0x18, 0x18, 0x18, 0x18, 0x18, 
            0x19, 0x19, 0x19, 0x19, 0x19, 0x19, 0x19, 0x19, 0x19, 0x19, 0x19, 0x19, 0x19, 0x19, 0x19, 0x19, 
            0x1a, 0x1a, 0x1a, 0x1a, 0x1a, 0x1a, 0x1a, 0x1a, 0x1a, 0x1a, 0x1a, 0x1a, 0x1a, 0x1a, 0x1a, 0x1a, 
            0x1a, 0x1a, 0x1a, 0x1a, 0x1a, 0x1a, 0x1a, 0x1a, 0x1a, 0x1a, 0x1a, 0x1a, 0x1a, 0x1a, 0x1a, 0x1a, 
            0x1b, 0x1b, 0x1b, 0x1b, 0x1b, 0x1b, 0x1b, 0x1b, 0x1b, 0x1b, 0x1b, 0x1b, 0x1b, 0x1b, 0x1b, 0x1b, 
            0x1b, 0x1b, 0x1b, 0x1b, 0x1b, 0x1b, 0x1b, 0x1b, 0x1b, 0x1b, 0x1b, 0x1b, 0x1b, 0x1b, 0x1b, 0x1b, 
            0x1c, 0x1c, 0x1c, 0x1c, 0x1c, 0x1c, 0x1c, 0x1c, 0x1c, 0x1c, 0x1c, 0x1c, 0x1c, 0x1c, 0x1c, 0x1c, 
            0x1c, 0x1c, 0x1c, 0x1c, 0x1c, 0x1c, 0x1c, 0x1c, 0x1c, 0x1c, 0x1c, 0x1c, 0x1c, 0x1c, 0x1c, 0x1c, 
            0x1c, 0x1c, 0x1c, 0x1c, 0x1c, 0x1c, 0x1c, 0x1c, 0x1c, 0x1c, 0x1c, 0x1c, 0x1c, 0x1c, 0x1c, 0x1c, 
            0x1c, 0x1c, 0x1c, 0x1c, 0x1c, 0x1c, 0x1c, 0x1c, 0x1c, 0x1c, 0x1c, 0x1c, 0x1c, 0x1c, 0x1c, 0x1c, 
            0x1d, 0x1d, 0x1d, 0x1d, 0x1d, 0x1d, 0x1d, 0x1d, 0x1d, 0x1d, 0x1d, 0x1d, 0x1d, 0x1d, 0x1d, 0x1d, 
            0x1d, 0x1d, 0x1d, 0x1d, 0x1d, 0x1d, 0x1d, 0x1d, 0x1d, 0x1d, 0x1d, 0x1d, 0x1d, 0x1d, 0x1d, 0x1d, 
            0x1d, 0x1d, 0x1d, 0x1d, 0x1d, 0x1d, 0x1d, 0x1d, 0x1d, 0x1d, 0x1d, 0x1d, 0x1d, 0x1d, 0x1d, 0x1d, 
            0x1d, 0x1d, 0x1d, 0x1d, 0x1d, 0x1d, 0x1d, 0x1d, 0x1d, 0x1d, 0x1d, 0x1d, 0x1d, 0x1d, 0x1d, 0x1d
         };
        internal static readonly byte[] _length_code = new byte[] { 
            0, 1, 2, 3, 4, 5, 6, 7, 8, 8, 9, 9, 10, 10, 11, 11, 
            12, 12, 12, 12, 13, 13, 13, 13, 14, 14, 14, 14, 15, 15, 15, 15, 
            0x10, 0x10, 0x10, 0x10, 0x10, 0x10, 0x10, 0x10, 0x11, 0x11, 0x11, 0x11, 0x11, 0x11, 0x11, 0x11, 
            0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x13, 0x13, 0x13, 0x13, 0x13, 0x13, 0x13, 0x13, 
            20, 20, 20, 20, 20, 20, 20, 20, 20, 20, 20, 20, 20, 20, 20, 20, 
            0x15, 0x15, 0x15, 0x15, 0x15, 0x15, 0x15, 0x15, 0x15, 0x15, 0x15, 0x15, 0x15, 0x15, 0x15, 0x15, 
            0x16, 0x16, 0x16, 0x16, 0x16, 0x16, 0x16, 0x16, 0x16, 0x16, 0x16, 0x16, 0x16, 0x16, 0x16, 0x16, 
            0x17, 0x17, 0x17, 0x17, 0x17, 0x17, 0x17, 0x17, 0x17, 0x17, 0x17, 0x17, 0x17, 0x17, 0x17, 0x17, 
            0x18, 0x18, 0x18, 0x18, 0x18, 0x18, 0x18, 0x18, 0x18, 0x18, 0x18, 0x18, 0x18, 0x18, 0x18, 0x18, 
            0x18, 0x18, 0x18, 0x18, 0x18, 0x18, 0x18, 0x18, 0x18, 0x18, 0x18, 0x18, 0x18, 0x18, 0x18, 0x18, 
            0x19, 0x19, 0x19, 0x19, 0x19, 0x19, 0x19, 0x19, 0x19, 0x19, 0x19, 0x19, 0x19, 0x19, 0x19, 0x19, 
            0x19, 0x19, 0x19, 0x19, 0x19, 0x19, 0x19, 0x19, 0x19, 0x19, 0x19, 0x19, 0x19, 0x19, 0x19, 0x19, 
            0x1a, 0x1a, 0x1a, 0x1a, 0x1a, 0x1a, 0x1a, 0x1a, 0x1a, 0x1a, 0x1a, 0x1a, 0x1a, 0x1a, 0x1a, 0x1a, 
            0x1a, 0x1a, 0x1a, 0x1a, 0x1a, 0x1a, 0x1a, 0x1a, 0x1a, 0x1a, 0x1a, 0x1a, 0x1a, 0x1a, 0x1a, 0x1a, 
            0x1b, 0x1b, 0x1b, 0x1b, 0x1b, 0x1b, 0x1b, 0x1b, 0x1b, 0x1b, 0x1b, 0x1b, 0x1b, 0x1b, 0x1b, 0x1b, 
            0x1b, 0x1b, 0x1b, 0x1b, 0x1b, 0x1b, 0x1b, 0x1b, 0x1b, 0x1b, 0x1b, 0x1b, 0x1b, 0x1b, 0x1b, 0x1c
         };
        internal static readonly int[] base_dist = new int[] { 
            0, 1, 2, 3, 4, 6, 8, 12, 0x10, 0x18, 0x20, 0x30, 0x40, 0x60, 0x80, 0xc0, 
            0x100, 0x180, 0x200, 0x300, 0x400, 0x600, 0x800, 0xc00, 0x1000, 0x1800, 0x2000, 0x3000, 0x4000, 0x6000
         };
        internal static readonly int[] base_length = new int[] { 
            0, 1, 2, 3, 4, 5, 6, 7, 8, 10, 12, 14, 0x10, 20, 0x18, 0x1c, 
            0x20, 40, 0x30, 0x38, 0x40, 80, 0x60, 0x70, 0x80, 160, 0xc0, 0xe0, 0
         };
        private const int BL_CODES = 0x13;
        internal static readonly byte[] bl_order = new byte[] { 
            0x10, 0x11, 0x12, 0, 8, 7, 9, 6, 10, 5, 11, 4, 12, 3, 13, 2, 
            14, 1, 15
         };
        internal const int Buf_size = 0x10;
        private const int D_CODES = 30;
        internal const int DIST_CODE_LEN = 0x200;
        internal short[] dyn_tree;
        internal const int END_BLOCK = 0x100;
        internal static readonly int[] extra_blbits = new int[] { 
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
            2, 3, 7
         };
        internal static readonly int[] extra_dbits = new int[] { 
            0, 0, 0, 0, 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6, 
            7, 7, 8, 8, 9, 9, 10, 10, 11, 11, 12, 12, 13, 13
         };
        internal static readonly int[] extra_lbits = new int[] { 
            0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 2, 2, 2, 2, 
            3, 3, 3, 3, 4, 4, 4, 4, 5, 5, 5, 5, 0
         };
        private static readonly int HEAP_SIZE = ((2 * L_CODES) + 1);
        private static readonly int L_CODES = 0x11e;
        private const int LENGTH_CODES = 0x1d;
        private const int LITERALS = 0x100;
        private const int MAX_BITS = 15;
        internal const int MAX_BL_BITS = 7;
        internal int max_code;
        internal const int REP_3_6 = 0x10;
        internal const int REPZ_11_138 = 0x12;
        internal const int REPZ_3_10 = 0x11;
        internal StaticTree stat_desc;

        internal static int bi_reverse(int code, int len)
        {
            int number = 0;
            do
            {
                number |= code & 1;
                code = SupportClass.URShift(code, 1);
                number = number << 1;
            }
            while (--len > 0);
            return SupportClass.URShift(number, 1);
        }

        internal void build_tree(Deflate s)
        {
            int num2;
            int num5;
            short[] tree = this.dyn_tree;
            short[] numArray2 = this.stat_desc.static_tree;
            int elems = this.stat_desc.elems;
            int num4 = -1;
            s.heap_len = 0;
            s.heap_max = HEAP_SIZE;
            for (num2 = 0; num2 < elems; num2++)
            {
                if (tree[num2 * 2] != 0)
                {
                    s.heap[++s.heap_len] = num4 = num2;
                    s.depth[num2] = 0;
                }
                else
                {
                    tree[(num2 * 2) + 1] = 0;
                }
            }
            while (s.heap_len < 2)
            {
                num5 = s.heap[++s.heap_len] = (num4 < 2) ? ++num4 : 0;
                tree[num5 * 2] = 1;
                s.depth[num5] = 0;
                s.opt_len--;
                if (numArray2 != null)
                {
                    s.static_len -= numArray2[(num5 * 2) + 1];
                }
            }
            this.max_code = num4;
            num2 = s.heap_len / 2;
            while (num2 >= 1)
            {
                s.pqdownheap(tree, num2);
                num2--;
            }
            num5 = elems;
            do
            {
                num2 = s.heap[1];
                s.heap[1] = s.heap[s.heap_len--];
                s.pqdownheap(tree, 1);
                int index = s.heap[1];
                s.heap[--s.heap_max] = num2;
                s.heap[--s.heap_max] = index;
                tree[num5 * 2] = (short) (tree[num2 * 2] + tree[index * 2]);
                s.depth[num5] = (byte) (Math.Max(s.depth[num2], s.depth[index]) + 1);
                tree[(num2 * 2) + 1] = tree[(index * 2) + 1] = (short) num5;
                s.heap[1] = num5++;
                s.pqdownheap(tree, 1);
            }
            while (s.heap_len >= 2);
            s.heap[--s.heap_max] = s.heap[1];
            this.gen_bitlen(s);
            gen_codes(tree, num4, s.bl_count);
        }

        internal static int d_code(int dist)
        {
            if (dist >= 0x100)
            {
                return _dist_code[0x100 + SupportClass.URShift(dist, 7)];
            }
            return _dist_code[dist];
        }

        internal void gen_bitlen(Deflate s)
        {
            int num4;
            short[] numArray = this.dyn_tree;
            short[] numArray2 = this.stat_desc.static_tree;
            int[] numArray3 = this.stat_desc.extra_bits;
            int num = this.stat_desc.extra_base;
            int index = this.stat_desc.max_length;
            int num9 = 0;
            int num6 = 0;
            while (num6 <= 15)
            {
                s.bl_count[num6] = 0;
                num6++;
            }
            numArray[(s.heap[s.heap_max] * 2) + 1] = 0;
            int num3 = s.heap_max + 1;
            while (num3 < HEAP_SIZE)
            {
                num4 = s.heap[num3];
                num6 = numArray[(numArray[(num4 * 2) + 1] * 2) + 1] + 1;
                if (num6 > index)
                {
                    num6 = index;
                    num9++;
                }
                numArray[(num4 * 2) + 1] = (short) num6;
                if (num4 <= this.max_code)
                {
                    s.bl_count[num6] = (short) (s.bl_count[num6] + 1);
                    int num7 = 0;
                    if (num4 >= num)
                    {
                        num7 = numArray3[num4 - num];
                    }
                    short num8 = numArray[num4 * 2];
                    s.opt_len += num8 * (num6 + num7);
                    if (numArray2 != null)
                    {
                        s.static_len += num8 * (numArray2[(num4 * 2) + 1] + num7);
                    }
                }
                num3++;
            }
            if (num9 != 0)
            {
                do
                {
                    num6 = index - 1;
                    while (s.bl_count[num6] == 0)
                    {
                        num6--;
                    }
                    s.bl_count[num6] = (short) (s.bl_count[num6] - 1);
                    s.bl_count[num6 + 1] = (short) (s.bl_count[num6 + 1] + 2);
                    s.bl_count[index] = (short) (s.bl_count[index] - 1);
                    num9 -= 2;
                }
                while (num9 > 0);
                for (num6 = index; num6 != 0; num6--)
                {
                    num4 = s.bl_count[num6];
                    while (num4 != 0)
                    {
                        int num5 = s.heap[--num3];
                        if (num5 <= this.max_code)
                        {
                            if (numArray[(num5 * 2) + 1] != num6)
                            {
                                s.opt_len += (num6 - numArray[(num5 * 2) + 1]) * numArray[num5 * 2];
                                numArray[(num5 * 2) + 1] = (short) num6;
                            }
                            num4--;
                        }
                    }
                }
            }
        }

        internal static void gen_codes(short[] tree, int max_code, short[] bl_count)
        {
            short[] numArray = new short[0x10];
            short num = 0;
            for (int i = 1; i <= 15; i++)
            {
                numArray[i] = num = (short) ((num + bl_count[i - 1]) << 1);
            }
            for (int j = 0; j <= max_code; j++)
            {
                int index = tree[(j * 2) + 1];
                if (index != 0)
                {
                    short num5;
                    numArray[index] = (short) ((num5 = numArray[index]) + 1);
                    tree[j * 2] = (short) bi_reverse(num5, index);
                }
            }
        }
    }
}

