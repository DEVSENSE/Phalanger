using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using PHP.Core;

namespace PHP.Library.Curl
{

    internal class WriteFunctionStream : Stream
    {
        private PhpCurlResource curl;
        private PhpCallback writeFunction;

        public WriteFunctionStream(PhpCurlResource curl,PhpCallback writeFunction)
        {
            this.curl = curl;
            this.writeFunction = writeFunction;
        }

        public override bool CanRead
        {
            get { return false; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return true; }
        }

        public override void Flush()
        {
            //nop
        }

        public override long Length
        {
            get { throw new NotImplementedException(); }
        }

        public override long Position
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            // THIS IS UGLY AND NOT OPTIMAL
            byte[] buf = new byte[count];            
            Buffer.BlockCopy(buffer, 0, buf, 0, count);


            writeFunction.Invoke(curl, new PhpBytes(buf));
            
        }
    }
}
