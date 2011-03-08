namespace zlib
{
    using System;
    using System.IO;

    internal class ZStreamException : IOException
    {
        public ZStreamException()
        {
        }

        public ZStreamException(string s) : base(s)
        {
        }
    }
}

