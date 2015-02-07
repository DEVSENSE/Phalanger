using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using PHP.Core;
using System.IO;

namespace PHP.Library.Curl
{
    internal static class HttpUtils
    {

        internal const int HTTP_HEADER_ROW_LENGTH = 30;

        delegate void HeaderRowEvent(ref StringBuilder builder);

        internal static void InvokeHeaderFunction(this HttpWebResponse response, PhpCurlResource curlResource, PhpCallback headerFunction)
        {
            StringBuilder builder = new StringBuilder(HTTP_HEADER_ROW_LENGTH);
            int startIndex = 0;

            IterateHtppHeaders(response, ref builder,
                delegate(ref StringBuilder sb)
                {
                    headerFunction.Invoke(curlResource, sb.ToString(startIndex, sb.Length - startIndex));
                    startIndex = sb.Length;
                }
                );

        }

        internal static string GetHttpHeaderAsString(this HttpWebResponse response)
        {
            WebHeaderCollection headers = response.Headers;

            if ((headers == null) || (headers.Count == 0))
            {
                return "\r\n";
            }
            StringBuilder builder = new StringBuilder(HTTP_HEADER_ROW_LENGTH * headers.Count);
            //string str = headers[string.Empty];

            IterateHtppHeaders(response, ref builder);

            return builder.ToString();
        }

        private static void IterateHtppHeaders(HttpWebResponse response, ref StringBuilder builder, HeaderRowEvent headerRowCallback = null)
        {
            WebHeaderCollection headers = response.Headers;

            builder.Append("HTTP/");
            builder.Append(response.ProtocolVersion);
            builder.Append(" ");
            builder.Append((int)response.StatusCode);
            builder.Append(" ");
            builder.Append(response.StatusDescription);
            builder.Append("\r\n");

            if (headerRowCallback != null)
                headerRowCallback(ref builder);

            //if (str != null)
            //{
            //    builder.Append(str).Append("\r\n");
            //}
            for (int i = 0; i < headers.Count; i++)
            {
                string key = headers.GetKey(i);
                string str3 = headers.Get(i);
                if (key != null && key.Length != 0)//key isn't blank string
                {
                    builder.Append(key);
                    builder.Append(": ");
                    builder.Append(str3).Append("\r\n");

                    if (headerRowCallback != null)
                        headerRowCallback(ref builder);
                }
            }
            builder.Append("\r\n");

            if (headerRowCallback != null)
                headerRowCallback(ref builder);// this should also be returned
        }

        internal static void SetHttpHeaders(this HttpWebRequest request, PhpArray array)
        {
            string headerName, headerValue;
            
            foreach (var arrayItem in array)
            {
                string headerItem = PhpVariable.AsString(arrayItem.Value);
                if (ParseHeader(headerItem, out headerName, out headerValue))
                {
                    Debug.Assert(headerName != null);
                    Debug.Assert(headerValue != null);

                    headerValue = headerValue.Trim();

                    //Accept 	        Set by the Accept property. 
                    //Connection 	    Set by the Connection property and KeepAlive property. 
                    //Content-Length 	Set by the ContentLength property. 
                    //Content-Type 	    Set by the ContentType property. 
                    //Expect 	        Set by the Expect property. 
                    //Date 	            Set by the Date property. 
                    //Host          	Set by the Host property. 
                    //If-Modified-Since Set by the IfModifiedSince property. 
                    //Range 	        Set by the AddRange method. 
                    //Referer 	        Set by the Referer property. 
                    //Transfer-Encoding Set by the TransferEncoding property (the SendChunked property must be true). 
                    //User-Agent 	    Set by the UserAgent property. 
                    switch (headerName.ToLowerInvariant())
                    {
                        case "accept":
                            request.Accept = headerValue;
                            break;
                        case "connection":
                            request.Connection = headerValue;
                            break;
                        case "content-length":
                            request.ContentLength = System.Convert.ToInt32(headerValue);
                            break;
                        case "content-type":
                            request.ContentType = headerValue;
                            break;
                        case "expect":
                            request.Expect = headerValue;
                            break;
                        case "date":
                            request.Date = System.Convert.ToDateTime(headerValue);
                            break;
                        case "host":
                            request.Host = headerValue;
                            break;
                        case "if-modified-since":
                            request.IfModifiedSince = System.Convert.ToDateTime(headerValue);
                            break;
                        case "range":
                            request.AddRange(System.Convert.ToInt32(headerValue));
                            break;
                        case "referer":
                            request.Referer = headerValue;
                            break;
                        case "transfer-encoding":
                            request.TransferEncoding = headerValue;
                            break;
                        case "user-agent":
                            request.UserAgent = headerValue;
                            break;
                        default:
                            request.Headers.Add(headerName, headerValue);
                            break;
                    }
                }

            }
        }

        internal static bool ParseHeader(string header, out string headerName, out string headerValue)
        {
            if (header != null)
            {
                int index = header.IndexOf(':');
                if (index > 0)
                {
                    headerName = header.Remove(index);
                    headerValue = header.Substring(index + 1);
                    return true;
                }
            }

            //
            headerName = null;
            headerValue = null;
            return false;
        }


        public static void SetBasicAuthHeader(WebRequest req, String userName, String userPassword)
        { 
            string authInfo = String.Format("{0}:{1}",userName, userPassword ); 
            authInfo = System.Convert.ToBase64String(Encoding.Default.GetBytes(authInfo)); 
            req.Headers["authorization"] = "Basic " + authInfo; 
        }

    }
}
