using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Globalization;
using System.Net;
using System.Threading;
using System.Security;
using PHP.Core;

namespace PHP.Library.Curl
{
    internal partial class HttpBitsUploader
    {

        private HttpWebRequest request;

        public HttpWebRequest Request
        {
            get { return request; }
        }


        public HttpBitsUploader(HttpWebRequest/*!*/ request)
        {
            Debug.Assert(request != null);

            this.request = request;
        }

        public void UploadData(object data)
        {
            PhpBytes bytes = PhpVariable.AsBytes(data);

            UploadData(bytes.ReadonlyData);
        }

        public void UploadData(byte[] data)
        {
            try
            {
                //this.m_Method = method;//this sets someone else
                request.ContentLength = data.Length;//this is my responsability to set

                this.UploadBits( request.GetRequestStream(), null,data, null, null);
                
                //buffer2 = this.DownloadBits(request, null, null, null);
            }
            catch (Exception exception)
            {
                if (((exception is ThreadAbortException) || (exception is StackOverflowException)) || (exception is OutOfMemoryException))
                {
                    throw;
                }
                if (!(exception is WebException) && !(exception is SecurityException))
                {
                    exception = new WebException("Curl", exception);
                }
                AbortRequest();
                throw exception;
            }
        }


        public void UploadFile(FileStream fs)
        {
            try
            {
                byte[] formHeaderBytes = null;
                byte[] boundaryBytes = null;
                byte[] buffer = null;

                //Uri isn't a file
                bool needsHeaderAndBoundary = request.RequestUri.Scheme != Uri.UriSchemeFile;
                
                OpenFileInternal(needsHeaderAndBoundary, fs, ref formHeaderBytes, ref boundaryBytes, ref buffer);

                this.UploadBits( request.GetRequestStream(), fs, buffer, formHeaderBytes, boundaryBytes);
                //byte[] retObject = this.DownloadBits(request, null, null, null);

            }
            catch (Exception exception)
            {
                if (fs != null)
                {
                    fs.Close();
                    fs = null;
                }
                if (((exception is ThreadAbortException) || (exception is StackOverflowException)) || (exception is OutOfMemoryException))
                {
                    throw;
                }
                if (!(exception is WebException) && !(exception is SecurityException))
                {
                    exception = new WebException("Curl", exception);
                }
                AbortRequest();
                throw exception;
            }

        }


        protected void AbortRequest()
        {
            try
            {
                if (request != null)
                {
                    request.Abort();
                }
            }
            catch (Exception exception)
            {
                if (((exception is OutOfMemoryException) || (exception is StackOverflowException)) || (exception is ThreadAbortException))
                {
                    throw;
                }
            }
        }

       
        protected void UploadBits(Stream writeStream, Stream readStream, byte[] data, byte[] header, byte[] footer, bool DoNotCloseWriteStream = false)
        {
            if (request.RequestUri.Scheme == Uri.UriSchemeFile)
            {
                header = (byte[])(footer = null);
            }
            HttpUploadBitsState state = new HttpUploadBitsState(readStream, data, header, footer, DoNotCloseWriteStream);

            state.SetRequestStream(writeStream);
            while (!state.WriteBytes())
            {
            }
            state.Close();
        }



        private void OpenFileInternal(bool needsHeaderAndBoundary, FileStream fs, ref byte[] formHeaderBytes, ref byte[] boundaryBytes, ref byte[] buffer)
        {
            //fileName = Path.GetFullPath(fileName);

            string str = request.ContentType;
            if (str != null)
            {
                if (str.ToLower(CultureInfo.InvariantCulture).StartsWith("multipart/"))
                {
                    throw new WebException("Curl multipart");//TODO: not sure about this
                }
            }
            else
            {
                str = "application/octet-stream";
            }

            //fs = new FileStream(fileName, FileMode.Open, FileAccess.Read);
            int num = 0x2000;
            //ContentLength = -1L;
            if (request.Method.ToUpper(CultureInfo.InvariantCulture) == "POST")
            {
                if (needsHeaderAndBoundary)
                {
                    string str2 = "---------------------" + DateTime.Now.Ticks.ToString("x", NumberFormatInfo.InvariantInfo);
                    request.ContentType = "multipart/form-data; boundary=" + str2;
                    string s = "--" + str2 + "\r\nContent-Disposition: form-data; name=\"file\"; filename=\"" + Path.GetFileName(fs.Name) + "\"\r\nContent-Type: " + str + "\r\n\r\n";
                    formHeaderBytes = Encoding.UTF8.GetBytes(s);
                    boundaryBytes = Encoding.ASCII.GetBytes("\r\n--" + str2 + "--\r\n");
                }
                else
                {
                    formHeaderBytes = new byte[0];
                    boundaryBytes = new byte[0];
                }
                if (fs.CanSeek)
                {
                    request.ContentLength = (fs.Length + formHeaderBytes.Length) + boundaryBytes.Length;
                    num = (int)Math.Min(0x2000L, fs.Length);
                }
            }
            else
            {
                request.ContentType = str;
                formHeaderBytes = null;
                boundaryBytes = null;
                if (fs.CanSeek)
                {
                    request.ContentLength = fs.Length;
                    num = (int)Math.Min(0x2000L, fs.Length);
                }
            }

            buffer = new byte[num];
        }

    }
}
