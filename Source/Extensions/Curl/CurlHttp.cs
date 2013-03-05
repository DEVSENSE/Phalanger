using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using PHP.Core;

namespace PHP.Library.Curl
{
    internal class CurlHttp : CurlHandler
    {
        private HttpWebRequest request;
        private HttpWebResponse response;

        /// <summary>
        /// URL scheme name.
        /// </summary>
        internal override string Scheme
        {
            get
            {
                return "http://";
            }
        }

        internal override long DefaultPort
        {
            get
            {
                return (long)Port.HTTP;
            }
        }

        internal override CurlProto Protocol
        {
            get
            {
                return CurlProto.HTTP;
            }
        }

        /// <summary>
        /// Execute is modified Curl_http() which in Curl gets called from the generic Curl_do() function when a HTTP
        /// request is to be performed. This creates and sends a properly constructed
        /// HTTP request.
        /// </summary>
        /// <param name="curl"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        internal override object Execute(PhpCurlResource curl, ref CURLcode result)
        {
            UserDefined data = curl.Data;
            HttpBitsUploader uploader;
            bool terminatedCorrectly = false;
            int redirectAttempts = 0;
            bool keepVerb = false;

            result = CURLcode.CURLE_OK;

            if (data.Str[(int)DupString.SET_URL] == null)
            {
                result = CURLcode.CURLE_COULDNT_CONNECT;
                return false;
            }

            Uri uri = Utils.CompleteUri(PhpVariable.AsString(data.Str[(int)DupString.SET_URL]),
                                            Scheme,
                                            data.UsePort);


            for (; ; )
            {
                request = (HttpWebRequest)HttpWebRequest.Create(uri);

                Curl_HttpReq httpreq = (redirectAttempts == 0) || keepVerb ? setRequestMethod(data) : Curl_HttpReq.GET;
                setTimeOut(data);
                setHttpVersion(data);

                request.AllowAutoRedirect = data.FollowLocation;
                request.MaximumAutomaticRedirections = data.MaxRedirects;

                if (data.Str[(int)DupString.USERAGENT] != null)
                    request.UserAgent = PhpVariable.AsString(data.Str[(int)DupString.USERAGENT]);

                if (data.Str[(int)DupString.SET_REFERER] != null)
                    request.Referer = PhpVariable.AsString(data.Str[(int)DupString.SET_REFERER]);

                if (data.Headers != null)
                    request.SetHttpHeaders(data.Headers);

                setProxy(data);
                setCredentials(data);
                setCookies(data);

                //ssl.VerifyPeer && ssl.VerifyHost == 2 is supported by default .NET
                // other values are currently unsupported

                if (data.Str[(int)DupString.CERT] != null)
                {
                    X509Certificate cert;
                    string certPath;

                    try
                    {
                        certPath = Path.Combine(ScriptContext.CurrentContext.WorkingDirectory, PhpVariable.AsString(data.Str[(int)DupString.SSL_CAFILE]));

                        if (data.Str[(int)DupString.KEY_PASSWD] == null)
                            cert = new X509Certificate(certPath);
                        else
                            cert = new X509Certificate(certPath, PhpVariable.AsString(data.Str[(int)DupString.KEY_PASSWD]));

                        request.ClientCertificates.Add(cert);
                    }
                    catch (CryptographicException)
                    {
                        //TODO: here are more caises to differentiate
                        result = CURLcode.CURLE_SSL_CACERT_BADFILE;
                        return false;
                    }

                }


                switch (httpreq)
                {
                    case Curl_HttpReq.POST_FORM:

                        //same as POST but we can send multiple items asform-data


                        if (data.HttpPostForm != null)
                        {
                            try
                            {
                                HttpFormDataUploader formUploader = new HttpFormDataUploader(request);
                                formUploader.UploadForm(data.HttpPostForm);
                            }
                            catch (WebException ex)
                            {
                                switch (ex.Status)
                                {
                                    case WebExceptionStatus.Timeout:
                                        result = CURLcode.CURLE_OPERATION_TIMEOUTED;
                                        break;
                                    default:
                                        result = CURLcode.CURLE_COULDNT_CONNECT;// for now just this
                                        break;

                                }
                                return false;
                            }
                        }

                        break;


                    case Curl_HttpReq.PUT: /* Let's PUT the data to the server! */

                        //INFILE & INFILESIZE has to be set

                        NativeStream nativeStream = data.Infile as NativeStream;

                        if (nativeStream == null)
                            return false;

                        FileStream fs = nativeStream.RawStream as FileStream;

                        if (fs == null)
                            return false;

                        try
                        {
                            uploader = new HttpBitsUploader(request);
                            uploader.UploadFile(fs);
                        }
                        catch (WebException ex)
                        {
                            switch (ex.Status)
                            {
                                case WebExceptionStatus.Timeout:
                                    result = CURLcode.CURLE_OPERATION_TIMEOUTED;
                                    break;
                                default:
                                    result = CURLcode.CURLE_COULDNT_CONNECT;// for now just this
                                    break;

                            }
                            return false;
                        }

                        break;

                    case Curl_HttpReq.POST:
                        /* this is the simple POST, using x-www-form-urlencoded style */

                        if (String.IsNullOrEmpty(request.ContentType))// if Content-type isn't set set the default
                            request.ContentType = "application/x-www-form-urlencoded";

                        if (data.Postfields != null)
                        {
                            try
                            {
                                uploader = new HttpBitsUploader(request);
                                uploader.UploadData(data.Postfields);
                            }
                            catch (WebException ex)
                            {
                                switch (ex.Status)
                                {
                                    case WebExceptionStatus.Timeout:
                                        result = CURLcode.CURLE_OPERATION_TIMEOUTED;
                                        break;
                                    default:
                                        result = CURLcode.CURLE_COULDNT_CONNECT;// for now just this
                                        break;

                                }
                                return false;
                            }
                        }

                        break;
                }

                try
                {
                    // if we got this far, we will turn off AutoRedirect (assuming it was on), since
                    // we are ready to handle manually following certain responses. this is needed
                    // to harvest cookies that are set on any intermediate response (i.e. anything
                    // other than the last one followed), since the .NET HTTP class will use, but
                    // NOT return, cookies set on anything but the last request.
                    request.AllowAutoRedirect = false;
                    response = (HttpWebResponse)request.GetResponse();
                }
                catch (WebException ex)
                {
                    switch (ex.Status)
                    {
                        case WebExceptionStatus.Timeout:
                            result = CURLcode.CURLE_OPERATION_TIMEOUTED;
                            break;
                        case WebExceptionStatus.ConnectFailure:
                            result = CURLcode.CURLE_COULDNT_CONNECT;
                            break;
                        case WebExceptionStatus.TrustFailure:
                            result = CURLcode.CURLE_SSL_CACERT;
                            break;
                        case WebExceptionStatus.ProtocolError:
                            //Response from server was complete, but indicated protocol error as 404, 401 etc.
                            break;
                        default:
                            result = CURLcode.CURLE_COULDNT_CONNECT;// for now just this
                            break;

                    }
                    //TODO: other errorCodes

                    response = (HttpWebResponse)ex.Response;
                    //return false;
                    //error = true;
                }

                if (response == null)// just to make sure I have the response object
                    return false;


                if (data.FollowLocation)
                {
                    // see if we need to follow a redirect.
                    switch (response.StatusCode)
                    {
                        case HttpStatusCode.MovedPermanently:
                        case HttpStatusCode.Found:
                        case HttpStatusCode.SeeOther:
                        case HttpStatusCode.RedirectKeepVerb:
                            if (redirectAttempts++ >= data.MaxRedirects)
                            {
                                result = CURLcode.CURLE_TOO_MANY_REDIRECTS;
                                return false;
                            }
                            string location = response.Headers["Location"];
                            if (!string.IsNullOrWhiteSpace(location))
                            {
                                try
                                {
                                    keepVerb = response.StatusCode == HttpStatusCode.RedirectKeepVerb;
                                    data.Cookies.Add(response.Cookies);
                                    response.Close();
                                    uri = new Uri(uri, location);
                                    continue;
                                }
                                catch (Exception)
                                {
                                    // closest error code though could be confusing as it's not the user-
                                    // submitted URL that's the problem
                                    result = CURLcode.CURLE_URL_MALFORMAT;
                                    return false;
                                }
                            }
                            break;
                    }
                }

                //Save cookies
                data.Cookies.Add(response.Cookies);
                // break out of the for loop as we aren't following a redirect
                break;
            }

            byte[] headers = null;
            byte[] content = null;
            int headersLength = 0;

            if (data.IncludeHeader)
            {
                //It's necessary to put HTTP header into the result

                //first we need to create it since there isn't anywhere
                headers = Encoding.ASCII.GetBytes(response.GetHttpHeaderAsString());
                headersLength = headers.Length;
            }

            if (data.FunctionWriteHeader != null)// TODO: probably invoke before
            {
                response.InvokeHeaderFunction(curl, data.FunctionWriteHeader);
            }

            Stream writeStream = null;

            if (data.WriteFunction != null)
            {
                writeStream = new WriteFunctionStream(curl, data.WriteFunction);
            }
            else if (data.OutFile != null)
            {
                var outStream = data.OutFile as PhpStream;

                if (outStream == null)
                    return false;

                Stream fs = outStream.RawStream as Stream;

                if (fs == null)
                    return false;

                writeStream = fs;
            }
            else if (data.ReturnTransfer == false) // Output to standart output
            {
                writeStream = ScriptContext.CurrentContext.OutputStream;
            }


            if (writeStream != null)
            {
                if (headers != null) //there is http header to copy to the result
                {
                    writeStream.Write(headers, 0, headersLength);
                }

                HttpBitsDownloader reader = new HttpBitsDownloader(response);

                try
                {
                    reader.ReadToStream(writeStream, out terminatedCorrectly);
                }
                catch (WebException ex)
                {
                    switch (ex.Status)
                    {
                        case WebExceptionStatus.Timeout:
                            result = CURLcode.CURLE_OPERATION_TIMEOUTED;
                            break;
                        default:
                            result = CURLcode.CURLE_COULDNT_CONNECT;// for now just this
                            break;
                    }
                }

                if (!terminatedCorrectly)
                    result = CURLcode.CURLE_PARTIAL_FILE;

                return true;
            }
            else
            {
                // Read the response
                HttpBitsDownloader reader = new HttpBitsDownloader(response);

                try
                {
                    content = reader.ReadToEnd(headersLength, out terminatedCorrectly);
                }
                catch(WebException ex)
                {
                    switch (ex.Status)
                    {
                        case WebExceptionStatus.Timeout:
                            result = CURLcode.CURLE_OPERATION_TIMEOUTED;
                            break;
                        default:
                            result = CURLcode.CURLE_COULDNT_CONNECT;// for now just this
                            break;
                    }
                }

                if (!terminatedCorrectly)
                    result = CURLcode.CURLE_PARTIAL_FILE;

                if (headers != null) //there is http header to copy to the result
                {
                    if (content != null)
                        Buffer.BlockCopy(headers, 0, content, 0, headersLength);
                    else
                        content = headers;
                }

                if (content == null)
                    return PhpBytes.Empty;
                else
                    return new PhpBytes(content);
            }

        }

        private void setCookies(UserDefined data)
        {
            //if (data.Cookies.Count > 0)
            //{
            var container = new CookieContainer();
            container.Add(data.Cookies);
            request.CookieContainer = container;
            //}
        }

        private void setCredentials(UserDefined data)
        {
            if (data.Str[(int)DupString.USERNAME] != null)
            {
                //This is obvious way, but unfortunatelly it doesn't work
                //httpauth set to CURLAUTH_ANY is .NET default because it will use right authentication protocol
                //request.Credentials = new NetworkCredential(
                //    PhpVariable.AsString(data.Str[(int)DupString.USERNAME]),
                //    data.Str[(int)DupString.PASSWORD] != null ? PhpVariable.AsString(data.Str[(int)DupString.PASSWORD]) : String.Empty);

                //We only support BASIC
                HttpUtils.SetBasicAuthHeader(request,
                    PhpVariable.AsString(data.Str[(int)DupString.USERNAME]),
                    data.Str[(int)DupString.PASSWORD] != null ? PhpVariable.AsString(data.Str[(int)DupString.PASSWORD]) : String.Empty);

            }
        }

        private void setProxy(UserDefined data)
        {
            string proxyAddress;
            WebProxy myProxy;

            if (data.Str[(int)DupString.PROXY] == null)
                return;

            if (data.ProxyType != CURLproxyType.CURLPROXY_HTTP)
                return;

            myProxy = new WebProxy();
            proxyAddress = PhpVariable.AsString(data.Str[(int)DupString.PROXY]);

            // Create a new Uri object.
            Uri uri = Utils.CompleteUri(proxyAddress, Scheme, data.ProxyPort);

            // Associate the newUri object to 'myProxy' object so that new myProxy settings can be set.
            myProxy.Address = uri;
            // Create a NetworkCredential object and associate it with the 
            // Proxy property of request object.

            if (data.Str[(int)DupString.PROXYUSERNAME] != null)
            {
                //data.proxyauth set to CURLAUTH_ANY is .NET default because it will use right authentication protocol

                myProxy.Credentials = new NetworkCredential(
                    PhpVariable.AsString(data.Str[(int)DupString.PROXYUSERNAME]),
                    data.Str[(int)DupString.PROXYPASSWORD] != null ? PhpVariable.AsString(data.Str[(int)DupString.PROXYPASSWORD]) : String.Empty);

            }

            request.Proxy = myProxy;

        }

        private void setHttpVersion(UserDefined data)
        {
            switch (data.HttpVersion)
            {
                //case CurlHttpVersion.CURL_HTTP_VERSION_NONE:
                //case CurlHttpVersion.CURL_HTTP_VERSION_LAST:
                //    do nothing, default will be used
                //    break;
                case CurlHttpVersion.CURL_HTTP_VERSION_1_0:
                    request.ProtocolVersion = HttpVersion.Version10;
                    break;
                case CurlHttpVersion.CURL_HTTP_VERSION_1_1:
                    request.ProtocolVersion = HttpVersion.Version11;
                    break;
            }
        }

        private Curl_HttpReq setRequestMethod(UserDefined data)
        {
            Curl_HttpReq httpreq = data.Httpreq;

            if ( // (conn->handler->protocol&(CURLPROTO_HTTP|CURLPROTO_FTP)) && //(MB) I'm handeling http request, so I don't need to check this
                data.Upload)
            {
                httpreq = Curl_HttpReq.PUT;
            }

            // Now set the request.Method to the proper request string
            if (data.Str[(int)DupString.CUSTOMREQUEST] != null)
                request.Method = PhpVariable.AsString(data.Str[(int)DupString.CUSTOMREQUEST]);
            else
            {
                if (data.OptNoBody)
                    request.Method = "HEAD";
                else
                {
                    switch (httpreq)
                    {
                        case Curl_HttpReq.POST:
                        case Curl_HttpReq.POST_FORM:
                            request.Method = "POST";
                            break;
                        case Curl_HttpReq.PUT:
                            request.Method = "PUT";
                            break;
                        default: /* this should never happen */
                        case Curl_HttpReq.GET:
                            request.Method = "GET";
                            break;
                        case Curl_HttpReq.HEAD:
                            request.Method = "HEAD";
                            break;
                    }
                }
            }

            return httpreq;
        }

        private void setTimeOut(UserDefined data)
        {
            // Curl default is 300000 milliseconds == five minutes
            // .NET default is 100000 milliseconds, we'll leave it there as a default

            int timeout_set = 0;

            if (data.Timeout > 0)
                timeout_set |= 1;

            if (data.ConnectTimeout > 0)
                timeout_set |= 2;

            switch (timeout_set)
            {
                case 1:
                    request.Timeout = data.Timeout;
                    break;
                case 2:
                    request.Timeout = data.ConnectTimeout;
                    break;
                case 3:
                    if (data.Timeout < data.ConnectTimeout)
                        request.Timeout = data.Timeout;
                    else
                        request.Timeout = data.ConnectTimeout;
                    break;
            }

        }


        internal override object GetInfo(CurlInfo info)
        {
            //Dictionary<CurlInfo, string> curlInfoNames = new Dictionary<CurlInfo,string>();

            //curlInfoNames.Add(CurlInfo.CURLINFO_EFFECTIVE_URL,"url");
            //curlInfoNames.Add(CurlInfo.CURLINFO_CONTENT_TYPE, "content_type");
            //curlInfoNames.Add(CurlInfo.CURLINFO_HTTP_CODE, "http_code");
            //curlInfoNames.Add(CurlInfo.CURLINFO_HEADER_SIZE, "header_size");
            //curlInfoNames.Add(CurlInfo.CURLINFO_REQUEST_SIZE, "request_size");
            //curlInfoNames.Add(CurlInfo.CURLINFO_FILETIME, "filetime");
            //curlInfoNames.Add(CurlInfo.CURLINFO_SSL_VERIFYRESULT, "ssl_verify_result");
            //curlInfoNames.Add(CurlInfo.CURLINFO_REDIRECT_COUNT, "redirect_count");
            //curlInfoNames.Add(CurlInfo.CURLINFO_TOTAL_TIME, "total_time");
            //curlInfoNames.Add(CurlInfo.CURLINFO_NAMELOOKUP_TIME, "namelookup_time");
            //curlInfoNames.Add(CurlInfo.CURLINFO_CONNECT_TIME, "connect_time");
            //curlInfoNames.Add(CurlInfo.CURLINFO_PRETRANSFER_TIME, "pretransfer_time");
            //curlInfoNames.Add(CurlInfo.CURLINFO_SIZE_UPLOAD, "size_upload");
            //curlInfoNames.Add(CurlInfo.CURLINFO_SIZE_DOWNLOAD, "size_download");
            //curlInfoNames.Add(CurlInfo.CURLINFO_SPEED_DOWNLOAD, "speed_download");
            //curlInfoNames.Add(CurlInfo.CURLINFO_SPEED_UPLOAD, "speed_upload");
            //curlInfoNames.Add(CurlInfo.CURLINFO_CONTENT_LENGTH_DOWNLOAD, "download_content_length");
            //curlInfoNames.Add(CurlInfo.CURLINFO_CONTENT_LENGTH_UPLOAD, "upload_content_length");
            //curlInfoNames.Add(CurlInfo.CURLINFO_STARTTRANSFER_TIME, "starttransfer_time");
            //curlInfoNames.Add(CurlInfo.CURLINFO_REDIRECT_TIME, "redirect_time");

            if (response == null)
                return false;

            switch (info)
            {
                case CurlInfo.EFFECTIVE_URL:
                    return response.ResponseUri.AbsoluteUri;
                case CurlInfo.HTTP_CODE:
                    return (int)response.StatusCode;
                case CurlInfo.CONTENT_TYPE:

                    if (String.IsNullOrEmpty(response.ContentType))
                        return false;

                    return response.ContentType;

            }

            return false;
        }


    }
}
