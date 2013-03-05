using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PHP.Core;
using System.Net;
using System.IO;
using System.Threading;
using System.Security;

namespace PHP.Library.Curl
{
    internal class HttpFormDataUploader : HttpBitsUploader
    {

        //enum FormType
        //{
        //    FORM_DATA,    /* form metadata (convert to network encoding if necessary) */
        //    FORM_CONTENT, /* form content  (never convert) */
        //    FORM_CALLBACK, /* 'line' points to the custom pointer we pass to the callback
        //          */
        //    FORM_FILE     /* 'line' points to a file name we should read from
        //           to create the form data (never convert) */
        //};


        class DataSegment
        {
            //public FormType Type;
            public byte[] Header;
            public byte[] Data;
            public byte[] Footer;
            public Stream ReadStream; 
        }

        private DataSegment currentData = new DataSegment();
        private string boundary;

        private LinkedList<DataSegment> data = new LinkedList<DataSegment>();

        private StringBuilder sb = new StringBuilder();
        private static readonly string table16 = "0123456789abcdef";
        private const int BOUNDARY_LENGTH = 40;


        public long ContentLength
        {
            get
            {
                long res = 0;

                foreach (var dataItem in data)
                {
                    res += dataItem.Header != null ? dataItem.Header.Length : 0;
                    res += dataItem.Data != null ? dataItem.Data.Length : 0;
                    res += dataItem.Footer != null ? dataItem.Footer.Length : 0;

                    if (dataItem.ReadStream != null && dataItem.ReadStream.CanSeek)
                        res += dataItem.ReadStream.Length;

                }
                return res;
            }
        }
        

        public HttpFormDataUploader(HttpWebRequest request) : base(request){ }

        /// <summary>
        /// Curl_FormBoundary() creates a suitable boundary string 
        /// </summary>
        /// <returns></returns>
        private string Curl_FormBoundary()
        {
            StringBuilder retstring = new StringBuilder(BOUNDARY_LENGTH + 1);

            retstring.Append("----------------------------");
            var r = new Random();

            for (int i = retstring.Length; i < BOUNDARY_LENGTH; i++)
                retstring.Append( table16[r.Next() % 16] );

            /* 28 dashes and 12 hexadecimal digits makes 12^16 (184884258895036416)
               combinations */

            return retstring.ToString();
        }

        /// <summary>
        /// Adds String.Format-style formatted data to the data chain.
        /// </summary>
        /// <param name="str"></param>
        /// <param name="args"></param>
        private void AddFormDataf(string str, params string[] args)
        {
            sb.AppendFormat(str, args);
        }


        private void AddFormFile(string fileName)
        {
            //currentData.Type = FormType.FORM_FILE;
            var filePath = Path.Combine(ScriptContext.CurrentContext.WorkingDirectory, fileName);
            
            //open a file
            currentData.ReadStream = File.Open(filePath, FileMode.Open);

            NextDataIsFooter();
        }


        private void AddFormData(object data)
        {
            //currentData.Type = FormType.FORM_CONTENT;

            PhpBytes bytes = PhpVariable.AsBytes(data);
            currentData.Data = bytes.ReadonlyData;

            NextDataIsFooter();
        }

        /// <summary>
        /// All calls to AddFormDataf is footer after calling this method. 
        /// </summary>
        /// <remarks>
        /// After call to method Done all calls to AddFormDataf are forming Header
        /// </remarks>
        private void NextDataIsFooter()
        {
            //Before this all AddFormData was header
            if (sb.Length > 0)
            {
                currentData.Header = Encoding.ASCII.GetBytes(sb.ToString());
                sb.Clear();
            }
        }

        private void Done()
        {
            // this was footer
            if (sb.Length > 0)
            {
                currentData.Footer = Encoding.ASCII.GetBytes(sb.ToString());
                sb.Clear();
            }

            data.AddLast(currentData);
            currentData = new DataSegment();
        }



        private long MaxFileSize()
        {
            long max = 0;

            foreach (var dataItem in data)
            {
                if (dataItem.ReadStream != null && dataItem.ReadStream.CanSeek)
                {
                    max = Math.Max(max, dataItem.ReadStream.Length);
                }
            }

            return max;
        }

        private byte[] CreateFileReadBuffer()
        {
            long bufferSize;
            long maxFileSize = MaxFileSize();

            if (maxFileSize == 0)
                return null;

            bufferSize = Math.Min(0x2000, maxFileSize);
            return new byte[bufferSize];
        }

        private void Upload()
        {
            byte[] fileBuffer = null;
            bool lastItem;

            Request.ContentType = "multipart/form-data; boundary=" + boundary;
            Request.ContentLength = ContentLength;

            try
            {
                var writeStream = Request.GetRequestStream();
                foreach (var dataItem in data)
                {
                    lastItem = dataItem == data.Last.Value;

                    if (dataItem.ReadStream != null)
                    {
                        if (fileBuffer == null)
                            fileBuffer = CreateFileReadBuffer();

                        //Send file
                        UploadBits(writeStream, dataItem.ReadStream, fileBuffer, dataItem.Header, dataItem.Footer, !lastItem);

                        //ReadStream was closed, just set it to null
                        dataItem.ReadStream = null;
                    }
                    else
                    {
                        //Send data
                        UploadBits(writeStream, null, dataItem.Data, dataItem.Header, dataItem.Footer, !lastItem);
                    }
                }

            }
            catch (Exception exception)
            {
                //Close all possibly opened files
                foreach (var dataItem in data)
                {
                    if (dataItem.ReadStream != null)
                    {
                        dataItem.ReadStream.Close();
                        dataItem.ReadStream = null;
                    }
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
            

            Close();
        }

        private void Close()
        {
            data.Clear();
        }

        public void UploadForm(CurlForm form)
        {
            //Count this
            //request.ContentLength = data.Length;//this is my responsability to set

            boundary = Curl_FormBoundary();
            CurlForm.FormFileItem fileItem = null;

            /* Make the first line of the output */
            // Assignment to Request.ContentType takes care about it
            //AddFormDataStr("{0}; boundary={1}\r\n",
            //    /*custom_content_type != null ? custom_content_type :*/ "Content-Type: multipart/form-data",
            //             boundary);

            /* we DO NOT include that line in the total size of the POST, since it'll be
               part of the header! */

            foreach (var item in form.Data)
            {
                AddFormDataf("\r\n");

                /* boundary */
                AddFormDataf("--{0}\r\n", boundary);

                /* Maybe later this should be disabled when a custom_content_type is
                   passed, since Content-Disposition is not meaningful for all multipart
                   types.
                */
                AddFormDataf("Content-Disposition: form-data; name=\"");

                AddFormDataf(item.Name);

                AddFormDataf("\"");

                //TODO: we just support one file send

                if (item.GetType() == typeof(CurlForm.FormFileItem))
                {
                    fileItem = (CurlForm.FormFileItem)item;

                    //Path.GetFileName(fs.Name)

                    AddFormDataf(" ;filename=\"{0}\"",
                                fileItem.FileName);

                    if (fileItem.ContentType != null)
                    {
                        /* we have a specified type */
                        AddFormDataf("\r\nContent-Type: {0}",
                                    fileItem.ContentType);
                    }
                    
                }

                AddFormDataf("\r\n\r\n");

                if (fileItem != null)
                {
                    //AddFile
                    AddFormFile(fileItem.FileName);
                }
                else
                {
                    AddFormData(item.Data);
                }

                if (item == form.Data.Last.Value) // Last item
                {
                    /* end-boundary for everything */
                    AddFormDataf("\r\n--{0}--\r\n", boundary);
                }

                Done();
                
            }

            Upload();

        }
    }
}
