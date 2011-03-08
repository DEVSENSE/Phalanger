namespace MySql.Data.MySqlClient
{
    using System;
    using System.Data.Common;
    using System.Runtime.Serialization;

    [Serializable]
    public sealed class MySqlException : DbException
    {
        private int errorCode;
        private bool isFatal;

        internal MySqlException()
        {
        }

        internal MySqlException(string msg) : base(msg)
        {
        }

        private MySqlException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        internal MySqlException(string msg, Exception ex) : base(msg, ex)
        {
        }

        internal MySqlException(string msg, int errno) : this(msg, errno, null)
        {
        }

        internal MySqlException(string msg, bool isFatal, Exception inner) : base(msg, inner)
        {
            this.isFatal = isFatal;
        }

        internal MySqlException(string msg, int errno, Exception inner) : this(msg, inner)
        {
            this.errorCode = errno;
            this.Data.Add("Server Error Code", errno);
        }

        internal bool IsFatal
        {
            get
            {
                return this.isFatal;
            }
        }

        internal bool IsQueryAborted
        {
            get
            {
                if (this.errorCode != 0x525)
                {
                    return (this.errorCode == 0x404);
                }
                return true;
            }
        }

        public int Number
        {
            get
            {
                return this.errorCode;
            }
        }
    }
}

