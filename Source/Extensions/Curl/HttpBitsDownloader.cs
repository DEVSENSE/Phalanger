using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using System.Diagnostics;

namespace PHP.Library.Curl
{
    internal class HttpBitsDownloader
    {
        private HttpWebResponse response;
        private long Length;
        private long ContentLength;
        private Stream ReadStream;
        private byte[] InnerBuffer;
        private Stream WriteStream;

        private ScatterGatherBuffers SgBuffers;

        public HttpBitsDownloader(HttpWebResponse/*!*/ response)
        {
            Debug.Assert(response != null);

            this.response = response;
        }

        /// <summary>
        /// This is fast method for reading whole stream of binary data
        /// </summary>
        /// <param name="reserveToAllocate"></param>
        /// <param name="terminatedCorrectly"></param>
        /// <returns></returns>
        /// <remarks>This method closes reponse stream.</remarks>
        public byte[] ReadToEnd(int reserveToAllocate, out bool terminatedCorrectly)
        {
            terminatedCorrectly = true;
            try
            {
                int bytesRetrieved = SetResponse();

                while (RetrieveBytes(ref bytesRetrieved)) { }

                return SgBuffers.ToArray(reserveToAllocate);
            }
            catch(IOException)
            {
                terminatedCorrectly = false;

                if (SgBuffers != null)
                    return SgBuffers.ToArray(reserveToAllocate);
                else
                    return null;
            }
            finally
            {
                Close();
            }

        }

        /// <summary>
        /// This is fast method for reading whole stream of binary data
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="terminatedCorrectly"></param>
        /// <remarks>This method closes reponse stream.</remarks>
        public void ReadToStream(Stream stream, out bool terminatedCorrectly)
        {
            terminatedCorrectly = true;
            WriteStream = stream;

            try
            {
                int bytesRetrieved = SetResponse();

                while (RetrieveBytes(ref bytesRetrieved)) { }

                return ;
            }
            catch (IOException)
            {
                terminatedCorrectly = false;
            }
            finally
            {
                Close();
            }

        }

        private void Close()
        {
            this.InnerBuffer = null;

            if (this.ReadStream != null)
            {
                this.ReadStream.Close();
                //response.close() isn't necessary because we closed stream from response.GetResponseStream
            }

            //Don't close WriteStream, that is up to user
        }

        private int SetResponse()
        {
            ContentLength = response.ContentLength;
            if ((ContentLength == -1L) || (ContentLength > 0x10000L))
            {
                Length = 0x10000L;
            }
            else
            {
                Length = ContentLength;
            }

            if (WriteStream == null)
            {
                if (ContentLength > 0x7fffffffL)
                {
                    throw new WebException("MessageLengthLimitExceeded", WebExceptionStatus.MessageLengthLimitExceeded);
                }

                SgBuffers = new ScatterGatherBuffers(this.Length);
            }

            InnerBuffer = new byte[Length];
            ReadStream = response.GetResponseStream();

            if ((ReadStream != null) && (ReadStream != Stream.Null))
            {
                return ReadStream.Read(InnerBuffer, 0, (int)Length);
            }
            return 0;
        }


        private bool RetrieveBytes(ref int bytesRetrieved)
        {
            if (bytesRetrieved > 0)
            {
                if (WriteStream != null)
			        WriteStream.Write(this.InnerBuffer, 0, bytesRetrieved);
		        else
                    SgBuffers.Write(InnerBuffer, 0, bytesRetrieved);

                bytesRetrieved = ReadStream.Read(InnerBuffer, 0, (int)Length);
                return true;
            }

            return false;
        }


    }
}
