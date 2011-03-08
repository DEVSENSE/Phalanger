namespace zlib
{
    using System;
    using System.IO;

    internal class ZInputStream : BinaryReader
    {
        protected internal byte[] buf;
        protected internal byte[] buf1;
        protected internal int bufsize;
        protected internal bool compress;
        protected internal int flush;
        private Stream in_Renamed;
        public long maxInput;
        private bool nomoreinput;
        protected internal ZStream z;

        public ZInputStream(Stream in_Renamed) : base(in_Renamed)
        {
            this.z = new ZStream();
            this.bufsize = 0x200;
            this.buf1 = new byte[1];
            this.InitBlock();
            this.in_Renamed = in_Renamed;
            this.z.inflateInit();
            this.compress = false;
            this.z.next_in = this.buf;
            this.z.next_in_index = 0;
            this.z.avail_in = 0;
        }

        public ZInputStream(Stream in_Renamed, int level) : base(in_Renamed)
        {
            this.z = new ZStream();
            this.bufsize = 0x200;
            this.buf1 = new byte[1];
            this.InitBlock();
            this.in_Renamed = in_Renamed;
            this.z.deflateInit(level);
            this.compress = true;
            this.z.next_in = this.buf;
            this.z.next_in_index = 0;
            this.z.avail_in = 0;
        }

        public override void Close()
        {
            this.in_Renamed.Close();
        }

        private void InitBlock()
        {
            this.flush = 0;
            this.buf = new byte[this.bufsize];
        }

        public int read(byte[] b, int off, int len)
        {
            int num;
            if (len == 0)
            {
                return 0;
            }
            this.z.next_out = b;
            this.z.next_out_index = off;
            this.z.avail_out = len;
            do
            {
                if ((this.z.avail_in == 0) && !this.nomoreinput)
                {
                    this.z.next_in_index = 0;
                    int bufsize = this.bufsize;
                    if (this.maxInput > 0L)
                    {
                        if (this.TotalIn < this.maxInput)
                        {
                            bufsize = (int) Math.Min(this.maxInput - this.TotalIn, (long) this.bufsize);
                        }
                        else
                        {
                            this.z.avail_in = -1;
                        }
                    }
                    if (this.z.avail_in != -1)
                    {
                        this.z.avail_in = SupportClass.ReadInput(this.in_Renamed, this.buf, 0, bufsize);
                    }
                    if (this.z.avail_in == -1)
                    {
                        this.z.avail_in = 0;
                        this.nomoreinput = true;
                    }
                }
                if (this.compress)
                {
                    num = this.z.deflate(this.flush);
                }
                else
                {
                    num = this.z.inflate(this.flush);
                }
                if (this.nomoreinput && (num == -5))
                {
                    return -1;
                }
                if ((num != 0) && (num != 1))
                {
                    throw new ZStreamException((this.compress ? "de" : "in") + "flating: " + this.z.msg);
                }
                if (this.nomoreinput && (this.z.avail_out == len))
                {
                    return -1;
                }
            }
            while ((this.z.avail_out > 0) && (num == 0));
            return (len - this.z.avail_out);
        }

        public override int Read()
        {
            if (this.read(this.buf1, 0, 1) == -1)
            {
                return -1;
            }
            return (this.buf1[0] & 0xff);
        }

        public long skip(long n)
        {
            int num = 0x200;
            if (n < num)
            {
                num = (int) n;
            }
            byte[] target = new byte[num];
            return (long) SupportClass.ReadInput(this.BaseStream, target, 0, target.Length);
        }

        public virtual int FlushMode
        {
            get
            {
                return this.flush;
            }
            set
            {
                this.flush = value;
            }
        }

        public virtual long TotalIn
        {
            get
            {
                return this.z.total_in;
            }
        }

        public virtual long TotalOut
        {
            get
            {
                return this.z.total_out;
            }
        }
    }
}

