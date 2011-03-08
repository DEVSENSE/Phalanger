namespace zlib
{
    using System;

    internal sealed class Deflate
    {
        internal short bi_buf;
        internal int bi_valid;
        private const int BL_CODES = 0x13;
        internal short[] bl_count = new short[0x10];
        internal Tree bl_desc = new Tree();
        internal short[] bl_tree = new short[0x4e];
        internal int block_start;
        private const int BlockDone = 1;
        private const int Buf_size = 0x10;
        private const int BUSY_STATE = 0x71;
        private static Config[] config_table = new Config[] { new Config(0, 0, 0, 0, 0), new Config(4, 4, 8, 4, 1), new Config(4, 5, 0x10, 8, 1), new Config(4, 6, 0x20, 0x20, 1), new Config(4, 4, 0x10, 0x10, 2), new Config(8, 0x10, 0x20, 0x20, 2), new Config(8, 0x10, 0x80, 0x80, 2), new Config(8, 0x20, 0x80, 0x100, 2), new Config(0x20, 0x80, 0x102, 0x400, 2), new Config(0x20, 0x102, 0x102, 0x1000, 2) };
        internal int d_buf;
        private const int D_CODES = 30;
        internal Tree d_desc = new Tree();
        internal byte data_type;
        private const int DEF_MEM_LEVEL = 8;
        internal byte[] depth = new byte[(2 * L_CODES) + 1];
        internal short[] dyn_dtree = new short[0x7a];
        internal short[] dyn_ltree = new short[HEAP_SIZE * 2];
        private const int DYN_TREES = 2;
        private const int END_BLOCK = 0x100;
        private const int FAST = 1;
        private const int FINISH_STATE = 0x29a;
        private const int FinishDone = 3;
        private const int FinishStarted = 2;
        internal int good_match;
        internal int hash_bits;
        internal int hash_mask;
        internal int hash_shift;
        internal int hash_size;
        internal short[] head;
        internal int[] heap = new int[(2 * L_CODES) + 1];
        internal int heap_len;
        internal int heap_max;
        private static readonly int HEAP_SIZE = ((2 * L_CODES) + 1);
        private const int INIT_STATE = 0x2a;
        internal int ins_h;
        internal int l_buf;
        private static readonly int L_CODES = 0x11e;
        internal Tree l_desc = new Tree();
        internal int last_eob_len;
        internal int last_flush;
        internal int last_lit;
        private const int LENGTH_CODES = 0x1d;
        internal int level;
        internal int lit_bufsize;
        private const int LITERALS = 0x100;
        internal int lookahead;
        internal int match_available;
        internal int match_length;
        internal int match_start;
        internal int matches;
        private const int MAX_BITS = 15;
        internal int max_chain_length;
        internal int max_lazy_match;
        private const int MAX_MATCH = 0x102;
        private const int MAX_MEM_LEVEL = 9;
        private const int MAX_WBITS = 15;
        internal byte method;
        private static readonly int MIN_LOOKAHEAD = 0x106;
        private const int MIN_MATCH = 3;
        private const int NeedMore = 0;
        internal int nice_match;
        internal int noheader;
        internal int opt_len;
        internal int pending;
        internal byte[] pending_buf;
        internal int pending_buf_size;
        internal int pending_out;
        private const int PRESET_DICT = 0x20;
        internal short[] prev;
        internal int prev_length;
        internal int prev_match;
        private const int REP_3_6 = 0x10;
        private const int REPZ_11_138 = 0x12;
        private const int REPZ_3_10 = 0x11;
        private const int SLOW = 2;
        internal int static_len;
        private const int STATIC_TREES = 1;
        internal int status;
        private const int STORED = 0;
        private const int STORED_BLOCK = 0;
        internal int strategy;
        internal ZStream strm;
        internal int strstart;
        internal int w_bits;
        internal int w_mask;
        internal int w_size;
        internal byte[] window;
        internal int window_size;
        private const int Z_ASCII = 1;
        private const int Z_BINARY = 0;
        private const int Z_BUF_ERROR = -5;
        private const int Z_DATA_ERROR = -3;
        private const int Z_DEFAULT_COMPRESSION = -1;
        private const int Z_DEFAULT_STRATEGY = 0;
        private const int Z_DEFLATED = 8;
        private static readonly string[] z_errmsg = new string[] { "need dictionary", "stream end", "", "file error", "stream error", "data error", "insufficient memory", "buffer error", "incompatible version", "" };
        private const int Z_ERRNO = -1;
        private const int Z_FILTERED = 1;
        private const int Z_FINISH = 4;
        private const int Z_FULL_FLUSH = 3;
        private const int Z_HUFFMAN_ONLY = 2;
        private const int Z_MEM_ERROR = -4;
        private const int Z_NEED_DICT = 2;
        private const int Z_NO_FLUSH = 0;
        private const int Z_OK = 0;
        private const int Z_PARTIAL_FLUSH = 1;
        private const int Z_STREAM_END = 1;
        private const int Z_STREAM_ERROR = -2;
        private const int Z_SYNC_FLUSH = 2;
        private const int Z_UNKNOWN = 2;
        private const int Z_VERSION_ERROR = -6;

        internal Deflate()
        {
        }

        internal void _tr_align()
        {
            this.send_bits(2, 3);
            this.send_code(0x100, StaticTree.static_ltree);
            this.bi_flush();
            if ((((1 + this.last_eob_len) + 10) - this.bi_valid) < 9)
            {
                this.send_bits(2, 3);
                this.send_code(0x100, StaticTree.static_ltree);
                this.bi_flush();
            }
            this.last_eob_len = 7;
        }

        internal void _tr_flush_block(int buf, int stored_len, bool eof)
        {
            int num;
            int num2;
            int num3 = 0;
            if (this.level > 0)
            {
                if (this.data_type == 2)
                {
                    this.set_data_type();
                }
                this.l_desc.build_tree(this);
                this.d_desc.build_tree(this);
                num3 = this.build_bl_tree();
                num = SupportClass.URShift((int) ((this.opt_len + 3) + 7), 3);
                num2 = SupportClass.URShift((int) ((this.static_len + 3) + 7), 3);
                if (num2 <= num)
                {
                    num = num2;
                }
            }
            else
            {
                num = num2 = stored_len + 5;
            }
            if (((stored_len + 4) <= num) && (buf != -1))
            {
                this._tr_stored_block(buf, stored_len, eof);
            }
            else if (num2 == num)
            {
                this.send_bits(2 + (eof ? 1 : 0), 3);
                this.compress_block(StaticTree.static_ltree, StaticTree.static_dtree);
            }
            else
            {
                this.send_bits(4 + (eof ? 1 : 0), 3);
                this.send_all_trees(this.l_desc.max_code + 1, this.d_desc.max_code + 1, num3 + 1);
                this.compress_block(this.dyn_ltree, this.dyn_dtree);
            }
            this.init_block();
            if (eof)
            {
                this.bi_windup();
            }
        }

        internal void _tr_stored_block(int buf, int stored_len, bool eof)
        {
            this.send_bits(eof ? 1 : 0, 3);
            this.copy_block(buf, stored_len, true);
        }

        internal bool _tr_tally(int dist, int lc)
        {
            this.pending_buf[this.d_buf + (this.last_lit * 2)] = (byte) SupportClass.URShift(dist, 8);
            this.pending_buf[(this.d_buf + (this.last_lit * 2)) + 1] = (byte) dist;
            this.pending_buf[this.l_buf + this.last_lit] = (byte) lc;
            this.last_lit++;
            if (dist == 0)
            {
                this.dyn_ltree[lc * 2] = (short) (this.dyn_ltree[lc * 2] + 1);
            }
            else
            {
                this.matches++;
                dist--;
                this.dyn_ltree[((Tree._length_code[lc] + 0x100) + 1) * 2] = (short) (this.dyn_ltree[((Tree._length_code[lc] + 0x100) + 1) * 2] + 1);
                this.dyn_dtree[Tree.d_code(dist) * 2] = (short) (this.dyn_dtree[Tree.d_code(dist) * 2] + 1);
            }
            if (((this.last_lit & 0x1fff) == 0) && (this.level > 2))
            {
                int number = this.last_lit * 8;
                int num2 = this.strstart - this.block_start;
                for (int i = 0; i < 30; i++)
                {
                    number += (int) (this.dyn_dtree[i * 2] * (5L + Tree.extra_dbits[i]));
                }
                number = SupportClass.URShift(number, 3);
                if ((this.matches < (this.last_lit / 2)) && (number < (num2 / 2)))
                {
                    return true;
                }
            }
            return (this.last_lit == (this.lit_bufsize - 1));
        }

        internal void bi_flush()
        {
            if (this.bi_valid == 0x10)
            {
                this.put_short(this.bi_buf);
                this.bi_buf = 0;
                this.bi_valid = 0;
            }
            else if (this.bi_valid >= 8)
            {
                this.put_byte((byte) this.bi_buf);
                this.bi_buf = (short) SupportClass.URShift((int) this.bi_buf, 8);
                this.bi_valid -= 8;
            }
        }

        internal void bi_windup()
        {
            if (this.bi_valid > 8)
            {
                this.put_short(this.bi_buf);
            }
            else if (this.bi_valid > 0)
            {
                this.put_byte((byte) this.bi_buf);
            }
            this.bi_buf = 0;
            this.bi_valid = 0;
        }

        internal int build_bl_tree()
        {
            this.scan_tree(this.dyn_ltree, this.l_desc.max_code);
            this.scan_tree(this.dyn_dtree, this.d_desc.max_code);
            this.bl_desc.build_tree(this);
            int index = 0x12;
            while (index >= 3)
            {
                if (this.bl_tree[(Tree.bl_order[index] * 2) + 1] != 0)
                {
                    break;
                }
                index--;
            }
            this.opt_len += (((3 * (index + 1)) + 5) + 5) + 4;
            return index;
        }

        internal void compress_block(short[] ltree, short[] dtree)
        {
            int num3 = 0;
            if (this.last_lit != 0)
            {
                do
                {
                    int dist = ((this.pending_buf[this.d_buf + (num3 * 2)] << 8) & 0xff00) | (this.pending_buf[(this.d_buf + (num3 * 2)) + 1] & 0xff);
                    int c = this.pending_buf[this.l_buf + num3] & 0xff;
                    num3++;
                    if (dist == 0)
                    {
                        this.send_code(c, ltree);
                    }
                    else
                    {
                        int index = Tree._length_code[c];
                        this.send_code((index + 0x100) + 1, ltree);
                        int length = Tree.extra_lbits[index];
                        if (length != 0)
                        {
                            c -= Tree.base_length[index];
                            this.send_bits(c, length);
                        }
                        dist--;
                        index = Tree.d_code(dist);
                        this.send_code(index, dtree);
                        length = Tree.extra_dbits[index];
                        if (length != 0)
                        {
                            dist -= Tree.base_dist[index];
                            this.send_bits(dist, length);
                        }
                    }
                }
                while (num3 < this.last_lit);
            }
            this.send_code(0x100, ltree);
            this.last_eob_len = ltree[0x201];
        }

        internal void copy_block(int buf, int len, bool header)
        {
            this.bi_windup();
            this.last_eob_len = 8;
            if (header)
            {
                this.put_short((short) len);
                this.put_short((short) ~len);
            }
            this.put_byte(this.window, buf, len);
        }

        internal int deflate(ZStream strm, int flush)
        {
            if ((flush > 4) || (flush < 0))
            {
                return -2;
            }
            if (((strm.next_out == null) || ((strm.next_in == null) && (strm.avail_in != 0))) || ((this.status == 0x29a) && (flush != 4)))
            {
                strm.msg = z_errmsg[4];
                return -2;
            }
            if (strm.avail_out == 0)
            {
                strm.msg = z_errmsg[7];
                return -5;
            }
            this.strm = strm;
            int num = this.last_flush;
            this.last_flush = flush;
            if (this.status == 0x2a)
            {
                int b = (8 + ((this.w_bits - 8) << 4)) << 8;
                int num3 = ((this.level - 1) & 0xff) >> 1;
                if (num3 > 3)
                {
                    num3 = 3;
                }
                b |= num3 << 6;
                if (this.strstart != 0)
                {
                    b |= 0x20;
                }
                b += 0x1f - (b % 0x1f);
                this.status = 0x71;
                this.putShortMSB(b);
                if (this.strstart != 0)
                {
                    this.putShortMSB((int) SupportClass.URShift(strm.adler, 0x10));
                    this.putShortMSB((int) (strm.adler & 0xffffL));
                }
                strm.adler = strm._adler.adler32(0L, null, 0, 0);
            }
            if (this.pending != 0)
            {
                strm.flush_pending();
                if (strm.avail_out == 0)
                {
                    this.last_flush = -1;
                    return 0;
                }
            }
            else if (((strm.avail_in == 0) && (flush <= num)) && (flush != 4))
            {
                strm.msg = z_errmsg[7];
                return -5;
            }
            if ((this.status == 0x29a) && (strm.avail_in != 0))
            {
                strm.msg = z_errmsg[7];
                return -5;
            }
            if (((strm.avail_in != 0) || (this.lookahead != 0)) || ((flush != 0) && (this.status != 0x29a)))
            {
                int num4 = -1;
                switch (config_table[this.level].func)
                {
                    case 0:
                        num4 = this.deflate_stored(flush);
                        break;

                    case 1:
                        num4 = this.deflate_fast(flush);
                        break;

                    case 2:
                        num4 = this.deflate_slow(flush);
                        break;
                }
                switch (num4)
                {
                    case 2:
                    case 3:
                        this.status = 0x29a;
                        break;
                }
                if ((num4 == 0) || (num4 == 2))
                {
                    if (strm.avail_out == 0)
                    {
                        this.last_flush = -1;
                    }
                    return 0;
                }
                if (num4 == 1)
                {
                    if (flush == 1)
                    {
                        this._tr_align();
                    }
                    else
                    {
                        this._tr_stored_block(0, 0, false);
                        if (flush == 3)
                        {
                            for (int i = 0; i < this.hash_size; i++)
                            {
                                this.head[i] = 0;
                            }
                        }
                    }
                    strm.flush_pending();
                    if (strm.avail_out == 0)
                    {
                        this.last_flush = -1;
                        return 0;
                    }
                }
            }
            if (flush == 4)
            {
                if (this.noheader != 0)
                {
                    return 1;
                }
                this.putShortMSB((int) SupportClass.URShift(strm.adler, 0x10));
                this.putShortMSB((int) (strm.adler & 0xffffL));
                strm.flush_pending();
                this.noheader = -1;
                if (this.pending == 0)
                {
                    return 1;
                }
            }
            return 0;
        }

        internal int deflate_fast(int flush)
        {
            int num = 0;
            do
            {
                bool flag;
                do
                {
                    if (this.lookahead < MIN_LOOKAHEAD)
                    {
                        this.fill_window();
                        if ((this.lookahead < MIN_LOOKAHEAD) && (flush == 0))
                        {
                            return 0;
                        }
                        if (this.lookahead == 0)
                        {
                            this.flush_block_only(flush == 4);
                            if (this.strm.avail_out == 0)
                            {
                                if (flush == 4)
                                {
                                    return 2;
                                }
                                return 0;
                            }
                            if (flush != 4)
                            {
                                return 1;
                            }
                            return 3;
                        }
                    }
                    if (this.lookahead >= 3)
                    {
                        this.ins_h = ((this.ins_h << this.hash_shift) ^ (this.window[this.strstart + 2] & 0xff)) & this.hash_mask;
                        num = this.head[this.ins_h] & 0xffff;
                        this.prev[this.strstart & this.w_mask] = this.head[this.ins_h];
                        this.head[this.ins_h] = (short) this.strstart;
                    }
                    if (((num != 0L) && (((this.strstart - num) & 0xffff) <= (this.w_size - MIN_LOOKAHEAD))) && (this.strategy != 2))
                    {
                        this.match_length = this.longest_match(num);
                    }
                    if (this.match_length >= 3)
                    {
                        flag = this._tr_tally(this.strstart - this.match_start, this.match_length - 3);
                        this.lookahead -= this.match_length;
                        if ((this.match_length <= this.max_lazy_match) && (this.lookahead >= 3))
                        {
                            this.match_length--;
                            do
                            {
                                this.strstart++;
                                this.ins_h = ((this.ins_h << this.hash_shift) ^ (this.window[this.strstart + 2] & 0xff)) & this.hash_mask;
                                num = this.head[this.ins_h] & 0xffff;
                                this.prev[this.strstart & this.w_mask] = this.head[this.ins_h];
                                this.head[this.ins_h] = (short) this.strstart;
                            }
                            while (--this.match_length != 0);
                            this.strstart++;
                        }
                        else
                        {
                            this.strstart += this.match_length;
                            this.match_length = 0;
                            this.ins_h = this.window[this.strstart] & 0xff;
                            this.ins_h = ((this.ins_h << this.hash_shift) ^ (this.window[this.strstart + 1] & 0xff)) & this.hash_mask;
                        }
                    }
                    else
                    {
                        flag = this._tr_tally(0, this.window[this.strstart] & 0xff);
                        this.lookahead--;
                        this.strstart++;
                    }
                }
                while (!flag);
                this.flush_block_only(false);
            }
            while (this.strm.avail_out != 0);
            return 0;
        }

        internal int deflate_slow(int flush)
        {
            int num = 0;
        Label_0002:
            do
            {
                bool flag;
                do
                {
                    if (this.lookahead < MIN_LOOKAHEAD)
                    {
                        this.fill_window();
                        if ((this.lookahead < MIN_LOOKAHEAD) && (flush == 0))
                        {
                            return 0;
                        }
                        if (this.lookahead == 0)
                        {
                            if (this.match_available != 0)
                            {
                                flag = this._tr_tally(0, this.window[this.strstart - 1] & 0xff);
                                this.match_available = 0;
                            }
                            this.flush_block_only(flush == 4);
                            if (this.strm.avail_out == 0)
                            {
                                if (flush == 4)
                                {
                                    return 2;
                                }
                                return 0;
                            }
                            if (flush != 4)
                            {
                                return 1;
                            }
                            return 3;
                        }
                    }
                    if (this.lookahead >= 3)
                    {
                        this.ins_h = ((this.ins_h << this.hash_shift) ^ (this.window[this.strstart + 2] & 0xff)) & this.hash_mask;
                        num = this.head[this.ins_h] & 0xffff;
                        this.prev[this.strstart & this.w_mask] = this.head[this.ins_h];
                        this.head[this.ins_h] = (short) this.strstart;
                    }
                    this.prev_length = this.match_length;
                    this.prev_match = this.match_start;
                    this.match_length = 2;
                    if (((num != 0) && (this.prev_length < this.max_lazy_match)) && (((this.strstart - num) & 0xffff) <= (this.w_size - MIN_LOOKAHEAD)))
                    {
                        if (this.strategy != 2)
                        {
                            this.match_length = this.longest_match(num);
                        }
                        if ((this.match_length <= 5) && ((this.strategy == 1) || ((this.match_length == 3) && ((this.strstart - this.match_start) > 0x1000))))
                        {
                            this.match_length = 2;
                        }
                    }
                    if ((this.prev_length < 3) || (this.match_length > this.prev_length))
                    {
                        if (this.match_available != 0)
                        {
                            if (this._tr_tally(0, this.window[this.strstart - 1] & 0xff))
                            {
                                this.flush_block_only(false);
                            }
                            this.strstart++;
                            this.lookahead--;
                            if (this.strm.avail_out == 0)
                            {
                                return 0;
                            }
                        }
                        else
                        {
                            this.match_available = 1;
                            this.strstart++;
                            this.lookahead--;
                        }
                        goto Label_0002;
                    }
                    int num2 = (this.strstart + this.lookahead) - 3;
                    flag = this._tr_tally((this.strstart - 1) - this.prev_match, this.prev_length - 3);
                    this.lookahead -= this.prev_length - 1;
                    this.prev_length -= 2;
                    do
                    {
                        if (++this.strstart <= num2)
                        {
                            this.ins_h = ((this.ins_h << this.hash_shift) ^ (this.window[this.strstart + 2] & 0xff)) & this.hash_mask;
                            num = this.head[this.ins_h] & 0xffff;
                            this.prev[this.strstart & this.w_mask] = this.head[this.ins_h];
                            this.head[this.ins_h] = (short) this.strstart;
                        }
                    }
                    while (--this.prev_length != 0);
                    this.match_available = 0;
                    this.match_length = 2;
                    this.strstart++;
                }
                while (!flag);
                this.flush_block_only(false);
            }
            while (this.strm.avail_out != 0);
            return 0;
        }

        internal int deflate_stored(int flush)
        {
            int num = 0xffff;
            if (num > (this.pending_buf_size - 5))
            {
                num = this.pending_buf_size - 5;
            }
            do
            {
                do
                {
                    if (this.lookahead <= 1)
                    {
                        this.fill_window();
                        if ((this.lookahead == 0) && (flush == 0))
                        {
                            return 0;
                        }
                        if (this.lookahead == 0)
                        {
                            this.flush_block_only(flush == 4);
                            if (this.strm.avail_out == 0)
                            {
                                if (flush != 4)
                                {
                                    return 0;
                                }
                                return 2;
                            }
                            if (flush != 4)
                            {
                                return 1;
                            }
                            return 3;
                        }
                    }
                    this.strstart += this.lookahead;
                    this.lookahead = 0;
                    int num2 = this.block_start + num;
                    if ((this.strstart == 0) || (this.strstart >= num2))
                    {
                        this.lookahead = this.strstart - num2;
                        this.strstart = num2;
                        this.flush_block_only(false);
                        if (this.strm.avail_out == 0)
                        {
                            return 0;
                        }
                    }
                }
                while ((this.strstart - this.block_start) < (this.w_size - MIN_LOOKAHEAD));
                this.flush_block_only(false);
            }
            while (this.strm.avail_out != 0);
            return 0;
        }

        internal int deflateEnd()
        {
            if (((this.status != 0x2a) && (this.status != 0x71)) && (this.status != 0x29a))
            {
                return -2;
            }
            this.pending_buf = null;
            this.head = null;
            this.prev = null;
            this.window = null;
            if (this.status != 0x71)
            {
                return 0;
            }
            return -3;
        }

        internal int deflateInit(ZStream strm, int level)
        {
            return this.deflateInit(strm, level, 15);
        }

        internal int deflateInit(ZStream strm, int level, int bits)
        {
            return this.deflateInit2(strm, level, 8, bits, 8, 0);
        }

        internal int deflateInit2(ZStream strm, int level, int method, int windowBits, int memLevel, int strategy)
        {
            int num = 0;
            strm.msg = null;
            if (level == -1)
            {
                level = 6;
            }
            if (windowBits < 0)
            {
                num = 1;
                windowBits = -windowBits;
            }
            if ((((memLevel < 1) || (memLevel > 9)) || ((method != 8) || (windowBits < 9))) || (((windowBits > 15) || (level < 0)) || (((level > 9) || (strategy < 0)) || (strategy > 2))))
            {
                return -2;
            }
            strm.dstate = this;
            this.noheader = num;
            this.w_bits = windowBits;
            this.w_size = ((int) 1) << this.w_bits;
            this.w_mask = this.w_size - 1;
            this.hash_bits = memLevel + 7;
            this.hash_size = ((int) 1) << this.hash_bits;
            this.hash_mask = this.hash_size - 1;
            this.hash_shift = ((this.hash_bits + 3) - 1) / 3;
            this.window = new byte[this.w_size * 2];
            this.prev = new short[this.w_size];
            this.head = new short[this.hash_size];
            this.lit_bufsize = ((int) 1) << (memLevel + 6);
            this.pending_buf = new byte[this.lit_bufsize * 4];
            this.pending_buf_size = this.lit_bufsize * 4;
            this.d_buf = this.lit_bufsize / 2;
            this.l_buf = 3 * this.lit_bufsize;
            this.level = level;
            this.strategy = strategy;
            this.method = (byte) method;
            return this.deflateReset(strm);
        }

        internal int deflateParams(ZStream strm, int _level, int _strategy)
        {
            int num = 0;
            if (_level == -1)
            {
                _level = 6;
            }
            if (((_level < 0) || (_level > 9)) || ((_strategy < 0) || (_strategy > 2)))
            {
                return -2;
            }
            if ((config_table[this.level].func != config_table[_level].func) && (strm.total_in != 0L))
            {
                num = strm.deflate(1);
            }
            if (this.level != _level)
            {
                this.level = _level;
                this.max_lazy_match = config_table[this.level].max_lazy;
                this.good_match = config_table[this.level].good_length;
                this.nice_match = config_table[this.level].nice_length;
                this.max_chain_length = config_table[this.level].max_chain;
            }
            this.strategy = _strategy;
            return num;
        }

        internal int deflateReset(ZStream strm)
        {
            strm.total_in = strm.total_out = 0L;
            strm.msg = null;
            strm.data_type = 2;
            this.pending = 0;
            this.pending_out = 0;
            if (this.noheader < 0)
            {
                this.noheader = 0;
            }
            this.status = (this.noheader != 0) ? 0x71 : 0x2a;
            strm.adler = strm._adler.adler32(0L, null, 0, 0);
            this.last_flush = 0;
            this.tr_init();
            this.lm_init();
            return 0;
        }

        internal int deflateSetDictionary(ZStream strm, byte[] dictionary, int dictLength)
        {
            int length = dictLength;
            int sourceIndex = 0;
            if ((dictionary == null) || (this.status != 0x2a))
            {
                return -2;
            }
            strm.adler = strm._adler.adler32(strm.adler, dictionary, 0, dictLength);
            if (length >= 3)
            {
                if (length > (this.w_size - MIN_LOOKAHEAD))
                {
                    length = this.w_size - MIN_LOOKAHEAD;
                    sourceIndex = dictLength - length;
                }
                Array.Copy(dictionary, sourceIndex, this.window, 0, length);
                this.strstart = length;
                this.block_start = length;
                this.ins_h = this.window[0] & 0xff;
                this.ins_h = ((this.ins_h << this.hash_shift) ^ (this.window[1] & 0xff)) & this.hash_mask;
                for (int i = 0; i <= (length - 3); i++)
                {
                    this.ins_h = ((this.ins_h << this.hash_shift) ^ (this.window[i + 2] & 0xff)) & this.hash_mask;
                    this.prev[i & this.w_mask] = this.head[this.ins_h];
                    this.head[this.ins_h] = (short) i;
                }
            }
            return 0;
        }

        internal void fill_window()
        {
            int num;
            int num4;
        Label_0000:
            num4 = (this.window_size - this.lookahead) - this.strstart;
            if (((num4 == 0) && (this.strstart == 0)) && (this.lookahead == 0))
            {
                num4 = this.w_size;
            }
            else if (num4 == -1)
            {
                num4--;
            }
            else if (this.strstart >= ((this.w_size + this.w_size) - MIN_LOOKAHEAD))
            {
                int num2;
                Array.Copy(this.window, this.w_size, this.window, 0, this.w_size);
                this.match_start -= this.w_size;
                this.strstart -= this.w_size;
                this.block_start -= this.w_size;
                num = this.hash_size;
                int index = num;
                do
                {
                    num2 = this.head[--index] & 0xffff;
                    this.head[index] = (num2 >= this.w_size) ? ((short) (num2 - this.w_size)) : ((short) 0);
                }
                while (--num != 0);
                num = this.w_size;
                index = num;
                do
                {
                    num2 = this.prev[--index] & 0xffff;
                    this.prev[index] = (num2 >= this.w_size) ? ((short) (num2 - this.w_size)) : ((short) 0);
                }
                while (--num != 0);
                num4 += this.w_size;
            }
            if (this.strm.avail_in != 0)
            {
                num = this.strm.read_buf(this.window, this.strstart + this.lookahead, num4);
                this.lookahead += num;
                if (this.lookahead >= 3)
                {
                    this.ins_h = this.window[this.strstart] & 0xff;
                    this.ins_h = ((this.ins_h << this.hash_shift) ^ (this.window[this.strstart + 1] & 0xff)) & this.hash_mask;
                }
                if ((this.lookahead < MIN_LOOKAHEAD) && (this.strm.avail_in != 0))
                {
                    goto Label_0000;
                }
            }
        }

        internal void flush_block_only(bool eof)
        {
            this._tr_flush_block((this.block_start >= 0) ? this.block_start : -1, this.strstart - this.block_start, eof);
            this.block_start = this.strstart;
            this.strm.flush_pending();
        }

        internal void init_block()
        {
            for (int i = 0; i < L_CODES; i++)
            {
                this.dyn_ltree[i * 2] = 0;
            }
            for (int j = 0; j < 30; j++)
            {
                this.dyn_dtree[j * 2] = 0;
            }
            for (int k = 0; k < 0x13; k++)
            {
                this.bl_tree[k * 2] = 0;
            }
            this.dyn_ltree[0x200] = 1;
            this.opt_len = this.static_len = 0;
            this.last_lit = this.matches = 0;
        }

        internal void lm_init()
        {
            this.window_size = 2 * this.w_size;
            this.head[this.hash_size - 1] = 0;
            for (int i = 0; i < (this.hash_size - 1); i++)
            {
                this.head[i] = 0;
            }
            this.max_lazy_match = config_table[this.level].max_lazy;
            this.good_match = config_table[this.level].good_length;
            this.nice_match = config_table[this.level].nice_length;
            this.max_chain_length = config_table[this.level].max_chain;
            this.strstart = 0;
            this.block_start = 0;
            this.lookahead = 0;
            this.match_length = this.prev_length = 2;
            this.match_available = 0;
            this.ins_h = 0;
        }

        internal int longest_match(int cur_match)
        {
            int num = this.max_chain_length;
            int strstart = this.strstart;
            int num5 = this.prev_length;
            int num6 = (this.strstart > (this.w_size - MIN_LOOKAHEAD)) ? (this.strstart - (this.w_size - MIN_LOOKAHEAD)) : 0;
            int lookahead = this.nice_match;
            int num8 = this.w_mask;
            int num9 = this.strstart + 0x102;
            byte num10 = this.window[(strstart + num5) - 1];
            byte num11 = this.window[strstart + num5];
            if (this.prev_length >= this.good_match)
            {
                num = num >> 2;
            }
            if (lookahead > this.lookahead)
            {
                lookahead = this.lookahead;
            }
            do
            {
                int index = cur_match;
                if (((this.window[index + num5] == num11) && (this.window[(index + num5) - 1] == num10)) && ((this.window[index] == this.window[strstart]) && (this.window[++index] == this.window[strstart + 1])))
                {
                    strstart += 2;
                    index++;
                    while ((((this.window[++strstart] == this.window[++index]) && (this.window[++strstart] == this.window[++index])) && ((this.window[++strstart] == this.window[++index]) && (this.window[++strstart] == this.window[++index]))) && (((this.window[++strstart] == this.window[++index]) && (this.window[++strstart] == this.window[++index])) && (((this.window[++strstart] == this.window[++index]) && (this.window[++strstart] == this.window[++index])) && (strstart < num9))))
                    {
                    }
                    int num4 = 0x102 - (num9 - strstart);
                    strstart = num9 - 0x102;
                    if (num4 > num5)
                    {
                        this.match_start = cur_match;
                        num5 = num4;
                        if (num4 >= lookahead)
                        {
                            break;
                        }
                        num10 = this.window[(strstart + num5) - 1];
                        num11 = this.window[strstart + num5];
                    }
                }
            }
            while (((cur_match = this.prev[cur_match & num8] & 0xffff) > num6) && (--num != 0));
            if (num5 <= this.lookahead)
            {
                return num5;
            }
            return this.lookahead;
        }

        internal void pqdownheap(short[] tree, int k)
        {
            int n = this.heap[k];
            for (int i = k << 1; i <= this.heap_len; i = i << 1)
            {
                if ((i < this.heap_len) && smaller(tree, this.heap[i + 1], this.heap[i], this.depth))
                {
                    i++;
                }
                if (smaller(tree, n, this.heap[i], this.depth))
                {
                    break;
                }
                this.heap[k] = this.heap[i];
                k = i;
            }
            this.heap[k] = n;
        }

        internal void put_byte(byte c)
        {
            this.pending_buf[this.pending++] = c;
        }

        internal void put_byte(byte[] p, int start, int len)
        {
            Array.Copy(p, start, this.pending_buf, this.pending, len);
            this.pending += len;
        }

        internal void put_short(int w)
        {
            this.put_byte((byte) w);
            this.put_byte((byte) SupportClass.URShift(w, 8));
        }

        internal void putShortMSB(int b)
        {
            this.put_byte((byte) (b >> 8));
            this.put_byte((byte) b);
        }

        internal void scan_tree(short[] tree, int max_code)
        {
            int num2 = -1;
            int num4 = tree[1];
            int num5 = 0;
            int num6 = 7;
            int num7 = 4;
            if (num4 == 0)
            {
                num6 = 0x8a;
                num7 = 3;
            }
            tree[((max_code + 1) * 2) + 1] = (short) SupportClass.Identity((long) 0xffffL);
            for (int i = 0; i <= max_code; i++)
            {
                int num3 = num4;
                num4 = tree[((i + 1) * 2) + 1];
                if ((++num5 >= num6) || (num3 != num4))
                {
                    if (num5 < num7)
                    {
                        this.bl_tree[num3 * 2] = (short) (this.bl_tree[num3 * 2] + num5);
                    }
                    else if (num3 != 0)
                    {
                        if (num3 != num2)
                        {
                            this.bl_tree[num3 * 2] = (short) (this.bl_tree[num3 * 2] + 1);
                        }
                        this.bl_tree[0x20] = (short) (this.bl_tree[0x20] + 1);
                    }
                    else if (num5 <= 10)
                    {
                        this.bl_tree[0x22] = (short) (this.bl_tree[0x22] + 1);
                    }
                    else
                    {
                        this.bl_tree[0x24] = (short) (this.bl_tree[0x24] + 1);
                    }
                    num5 = 0;
                    num2 = num3;
                    if (num4 == 0)
                    {
                        num6 = 0x8a;
                        num7 = 3;
                    }
                    else if (num3 == num4)
                    {
                        num6 = 6;
                        num7 = 3;
                    }
                    else
                    {
                        num6 = 7;
                        num7 = 4;
                    }
                }
            }
        }

        internal void send_all_trees(int lcodes, int dcodes, int blcodes)
        {
            this.send_bits(lcodes - 0x101, 5);
            this.send_bits(dcodes - 1, 5);
            this.send_bits(blcodes - 4, 4);
            for (int i = 0; i < blcodes; i++)
            {
                this.send_bits(this.bl_tree[(Tree.bl_order[i] * 2) + 1], 3);
            }
            this.send_tree(this.dyn_ltree, lcodes - 1);
            this.send_tree(this.dyn_dtree, dcodes - 1);
        }

        internal void send_bits(int value_Renamed, int length)
        {
            int num = length;
            if (this.bi_valid > (0x10 - num))
            {
                int number = value_Renamed;
                this.bi_buf = (short) (((ushort) this.bi_buf) | ((ushort) ((number << this.bi_valid) & 0xffff)));
                this.put_short(this.bi_buf);
                this.bi_buf = (short) SupportClass.URShift(number, (int) (0x10 - this.bi_valid));
                this.bi_valid += num - 0x10;
            }
            else
            {
                this.bi_buf = (short) (((ushort) this.bi_buf) | ((ushort) ((value_Renamed << this.bi_valid) & 0xffff)));
                this.bi_valid += num;
            }
        }

        internal void send_code(int c, short[] tree)
        {
            this.send_bits(tree[c * 2] & 0xffff, tree[(c * 2) + 1] & 0xffff);
        }

        internal void send_tree(short[] tree, int max_code)
        {
            int num2 = -1;
            int num4 = tree[1];
            int num5 = 0;
            int num6 = 7;
            int num7 = 4;
            if (num4 == 0)
            {
                num6 = 0x8a;
                num7 = 3;
            }
            for (int i = 0; i <= max_code; i++)
            {
                int c = num4;
                num4 = tree[((i + 1) * 2) + 1];
                if ((++num5 >= num6) || (c != num4))
                {
                    if (num5 < num7)
                    {
                        do
                        {
                            this.send_code(c, this.bl_tree);
                        }
                        while (--num5 != 0);
                    }
                    else if (c != 0)
                    {
                        if (c != num2)
                        {
                            this.send_code(c, this.bl_tree);
                            num5--;
                        }
                        this.send_code(0x10, this.bl_tree);
                        this.send_bits(num5 - 3, 2);
                    }
                    else if (num5 <= 10)
                    {
                        this.send_code(0x11, this.bl_tree);
                        this.send_bits(num5 - 3, 3);
                    }
                    else
                    {
                        this.send_code(0x12, this.bl_tree);
                        this.send_bits(num5 - 11, 7);
                    }
                    num5 = 0;
                    num2 = c;
                    if (num4 == 0)
                    {
                        num6 = 0x8a;
                        num7 = 3;
                    }
                    else if (c == num4)
                    {
                        num6 = 6;
                        num7 = 3;
                    }
                    else
                    {
                        num6 = 7;
                        num7 = 4;
                    }
                }
            }
        }

        internal void set_data_type()
        {
            int num = 0;
            int number = 0;
            int num3 = 0;
            while (num < 7)
            {
                num3 += this.dyn_ltree[num * 2];
                num++;
            }
            while (num < 0x80)
            {
                number += this.dyn_ltree[num * 2];
                num++;
            }
            while (num < 0x100)
            {
                num3 += this.dyn_ltree[num * 2];
                num++;
            }
            this.data_type = (num3 > SupportClass.URShift(number, 2)) ? ((byte) 0) : ((byte) 1);
        }

        internal static bool smaller(short[] tree, int n, int m, byte[] depth)
        {
            return ((tree[n * 2] < tree[m * 2]) || ((tree[n * 2] == tree[m * 2]) && (depth[n] <= depth[m])));
        }

        internal void tr_init()
        {
            this.l_desc.dyn_tree = this.dyn_ltree;
            this.l_desc.stat_desc = StaticTree.static_l_desc;
            this.d_desc.dyn_tree = this.dyn_dtree;
            this.d_desc.stat_desc = StaticTree.static_d_desc;
            this.bl_desc.dyn_tree = this.bl_tree;
            this.bl_desc.stat_desc = StaticTree.static_bl_desc;
            this.bi_buf = 0;
            this.bi_valid = 0;
            this.last_eob_len = 8;
            this.init_block();
        }

        internal class Config
        {
            internal int func;
            internal int good_length;
            internal int max_chain;
            internal int max_lazy;
            internal int nice_length;

            internal Config(int good_length, int max_lazy, int nice_length, int max_chain, int func)
            {
                this.good_length = good_length;
                this.max_lazy = max_lazy;
                this.nice_length = nice_length;
                this.max_chain = max_chain;
                this.func = func;
            }
        }
    }
}

