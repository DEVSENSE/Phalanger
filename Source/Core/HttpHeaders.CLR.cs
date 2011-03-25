/*

 Copyright (c) 2007 Tomas Petricek
  
 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Web;

using PHP.Library;

namespace PHP.Core
{
	/// <summary>
    /// Used for proper handling of setting/getting current response headers.
    /// 
    /// In case of IIS Classic Mode, headers must be cached and flushed in 'PreSendRequestHeaders' event, because .NET doesn't allow this behavior.
    /// In case of IIS Integrated Pipeline, request headers can be accessed (set/modified/removed) within HttpRequest object in any time.
	/// </summary>
    public class HttpHeaders : IEnumerable<KeyValuePair<string, string>>
    {
        #region Initialization

        #region HttpRuntime.UsingIntegratedPipeline helper

        /// <summary>
        /// Equivalent to <c>HttpRuntime.UsingIntegratedPipeline</c> if this property exists. Otherwise it returns <b>false</b>.
        /// </summary>
        public static readonly bool UsingIntegratedPipeline = UsingIntegratedPipelineHelper;

        /// <summary>
        /// Helper that dynamically calls getter of <b>HttpRuntime</b>.<b>UsingIntegratedPipeline</b>.
        /// It cannot be used in compile time since .NET 2.0 (without SP) and Mono does not have this method defined at all.
        /// </summary>
        private static bool UsingIntegratedPipelineHelper
        {
            get
            {
                var p = typeof(HttpRuntime).GetProperty("UsingIntegratedPipeline");

                return (p != null) ? (bool)p.GetGetMethod().Invoke(null, ArrayUtils.EmptyObjects) : false;
            }
        }
        

        #endregion

        /// <summary>
        /// Create proper HttpHeaders object based on the current HttpRuntime environment.
        /// </summary>
        /// <returns>Instance of HttpHeaders object.</returns>
        public static HttpHeaders Create()
        {
            if (/*HttpRuntime.*/UsingIntegratedPipeline)
                return new IntegratedPipelineHeaders();
            
            return new HttpHeaders(true);
        }

        /// <summary>
        /// Try to attach the Headers object into the HttpRuntime.
        /// Do not allow instantiating this class from outside.
        /// </summary>
        private HttpHeaders(bool attach)
        {
            if (attach)
            {
                var context = HttpContext.Current;

                if (context != null)
                    TryAttachApplication(context.ApplicationInstance);
            }
        }

        #endregion

        #region PreSendRequestHeaders event

        /// <summary>
        /// Determines if the PreSendRequestHeaders event was already set.
        /// </summary>
        static bool attached = false;

        private static void TryAttachApplication(HttpApplication hta)
        {
            if (!attached && hta != null)
            {
                hta.PreSendRequestHeaders += new EventHandler(PreSendRequestHeaders);

                //
                attached = true;
            }
        }

        private static void PreSendRequestHeaders(object sender, EventArgs e)
        {
            HttpContext ctx = ((HttpApplication)sender).Context;
            ScriptContext.CurrentContext.Headers.Flush(ctx);
        }

        #endregion

        #region Headers, special headers

        /// <summary>
        /// Represents the current encoding that was set by string or Encoding instance.
        /// </summary>
        protected class StringEncoding
        {
            private Encoding encoding;
            private string encodingAsString;

            public Encoding Encoding
            {
                get
                {
                    return encoding ?? ((encodingAsString != null) ? Encoding.GetEncoding(encodingAsString) : null);
                }
                set
                {
                    encodingAsString = null;
                    encoding = value;
                }
            }
            public string String
            {
                get
                {
                    return encodingAsString ?? ((encoding != null) ? encoding.HeaderName : null);
                }
                set
                {
                    encoding = null;
                    encodingAsString = value;
                }
            }

            /// <summary>
            /// Set the encoding into the HttpResponse object.
            /// </summary>
            /// <param name="response"></param>
            public void SetEncoding(HttpResponse/*!*/response)
            {
                switch (encodingAsString)
                {
                    // following values must be set as a string,
                    // it cannot be converted to proper Encoding
                    case "gzip":
                    case "deflate":
                        response.AppendHeader("content-encoding", encodingAsString);
                        break;
                    
                    // by default, set the Encoding properly
                    default:
                        response.ContentEncoding = this.Encoding;
                        break;
                }
            }
        }

        /// <summary>
        /// Current content-encoding header if set.
        /// </summary>
        protected StringEncoding contentEncoding
        {
            get { return _contentEncoding ?? (_contentEncoding = new StringEncoding()); }
            set { _contentEncoding = null; }
        }
        private StringEncoding _contentEncoding;

        /// <summary>
        /// Current location header if set.
        /// </summary>
        protected string location;

        /// <summary>
        /// Current content-type header if set.
        /// </summary>
        protected string contentType;

        /// <summary>
        /// All the other headers that was set by PHP application.
        /// </summary>
        protected readonly Dictionary<string, string> headers = new Dictionary<string, string>();

        #endregion

        #region public headers methods

        /// <summary>
        /// Get or Set any header to be sent within response.
        /// </summary>
        /// <param name="header">Header name, case insensitive.</param>
        /// <returns>The header value, or null if the header was not set.</returns>
        public virtual string this[string header]
        {
            get
            {
                header = header.ToLower();

                switch (header)
                {
                    case "location":
                        return location;
                    case "content-type":
                        return contentType;
                    case "content-encoding":
                        return (_contentEncoding != null) ? contentEncoding.String : null;
                    default:
                        {
                            string value;
                            if (headers.TryGetValue(header, out value))
                                return value;
                            else
                                return null;
                        }
                }
            }
            set
            {
                header = header.ToLower();

                switch (header)
                {
                    case "location":
                        OnLocationSet(value);
                        location = value;
                        break;
                    case "content-type":
                        contentType = value;
                        contentEncoding.Encoding = ContentTypeEncoding(value);
                        break;
                    case "content-encoding":
                        if (value != null)
                            contentEncoding.String = value;
                        else
                            contentEncoding = null; // clear the encoding
                        break;
                    default:
                        if (value != null)
                            headers[header] = value;
                        else
                            headers.Remove(header);

                        break;
                }
            }
        }

        /// <summary>
        /// Clear all headers if null is given.
        /// </summary>
        public virtual void Clear()
        {
            if (location != null)
            {
                HttpResponse response = HttpContext.Current.Response;
                if (response.StatusCode == 302)
                    response.StatusCode = 200;

                location = null;
            }

            contentEncoding.Encoding = RequestContext.CurrentContext.DefaultResponseEncoding;
            contentType = null;
            headers.Clear();
        }

        #region IEnumerable

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            foreach (KeyValuePair<string, string> header in this)
                yield return header;
        }

        #endregion

        #region IEnumerable<string, string>

        /// <summary>
        /// Returns all headers currently set by the web application.
        /// Including content-type, content-encoding, location, ...
        /// </summary>
        /// <returns></returns>
        public virtual IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            if (location != null)
                yield return new KeyValuePair<string, string>("location", location);

            if (_contentEncoding != null)
                yield return new KeyValuePair<string, string>("content-encoding", contentEncoding.String);

            if (contentType != null)
                yield return new KeyValuePair<string, string>("content-type", contentType);

            //if (HttpContext.Current != null)
            //{
            //    try
            //    {
            //        HttpResponse response = HttpContext.Current.Response;
            //        foreach (string key in response.Headers.Keys)
            //        {
            //            string values = response.Headers[key];
            //            if (values != null)
            //                foreach (var value in values.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            //                    mergedheaders[key.ToLower()] = value;
            //        }
            //    }
            //    catch{}
            //}

            foreach (KeyValuePair<string, string> pair in headers)
                yield return pair;
        }

        #endregion

        #region Flushing headers into Response

        /// <summary>
        /// If headers were flushed already.
        /// </summary>
        protected bool flushed = false;

        /// <summary>
        /// Write headers to ASP.NET HttpContext. Can be called multiple times, it will be flushed only once.
        /// </summary>
        public virtual void Flush(HttpContext ctx)
        {
            if (flushed) return;
            flushed = true;

            try
            {
                if (location != null)
                    ctx.Response.Redirect(location, false);

                if (_contentEncoding != null)
                    contentEncoding.SetEncoding(ctx.Response);

                if (contentType != null)
                    ctx.Response.ContentType = contentType;

                foreach (KeyValuePair<string, string> pair in headers)
                {
                    try
                    {
                        ctx.Response.AppendHeader(pair.Key, pair.Value);
                    }
                    catch (HttpException e)
                    {
                        PhpException.Throw(PhpError.Warning, CoreResources.GetString("invalid_header", pair.Key + ": " + pair.Value, e.Message));
                    }
                }
            }
            catch (HttpException e)
            {
                PhpException.Throw(PhpError.Warning, e.Message);
            }
        }

        #endregion

        #endregion

        #region helper headers methods

        /// <summary>
        /// Update the status code when location is set.
        /// </summary>
        /// <param name="location"></param>
        protected virtual void OnLocationSet(string location)
        {
            // set status code 302 unless the 201 or a 3xx status code has already been set 
            HttpResponse response = HttpContext.Current.Response;
            if (location != null && response.StatusCode != 201 && (response.StatusCode < 300 || response.StatusCode >= 400))
                response.StatusCode = 302;
        }

        /// <summary>
        /// Get content encoding depending on the content type.
        /// </summary>
        /// <param name="contentType"></param>
        /// <returns></returns>
        protected virtual Encoding ContentTypeEncoding(string contentType)
        {
            if (contentType == null || contentType.ToLower().StartsWith("text/"))
                return RequestContext.CurrentContext.DefaultResponseEncoding;
            else
                return Configuration.Application.Globalization.PageEncoding;
                
        }

        #endregion

        #region Integrated Pipeline

        private class IntegratedPipelineHeaders : HttpHeaders
        {
            #region ctor

            public IntegratedPipelineHeaders()
                :base(false)
            {

            }

            #endregion

            #region HttpHeaders

            public override string this[string header]
            {
                get
                {
                    return base[header] ?? HttpContext.Current.Response.Headers[header];
                }
                set
                {
                    base[header] = value;

                    // store the header immediately into the buffered response
                    header = header.ToLower();
                    var response = HttpContext.Current.Response;

                    switch (header)
                    {
                        case "location":
                            response.RedirectLocation = location;
                            break;
                        case "content-type":
                            response.ContentType = contentType;
                            response.ContentEncoding = contentEncoding.Encoding;
                            break;
                        case "content-encoding":
                            if (_contentEncoding != null) _contentEncoding.SetEncoding(response);
                            else response.ContentEncoding = RequestContext.CurrentContext.DefaultResponseEncoding;
                            break;
                        default:
                            if (value != null)
                                response.Headers[header] = value;
                            else
                                response.Headers.Remove(header);

                            break;
                    }
                }
            }

            public override void Flush(HttpContext ctx)
            {
                flushed = true;
                // do not flush here
            }

            public override void Clear()
            {
                base.Clear();

                HttpResponse response = HttpContext.Current.Response;

                response.RedirectLocation = null;
                response.ContentEncoding = RequestContext.CurrentContext.DefaultResponseEncoding;
                response.ContentType = null;
                response.Headers.Clear();
            }

            //public override IEnumerator<KeyValuePair<string, string>> GetEnumerator()
            //{
            //    if (!flushed)
            //    {
            //        // yield return buffered headers
            //        IEnumerator<KeyValuePair<string, string>> bufferedHeaders = base.GetEnumerator();
            //        while (bufferedHeaders.MoveNext())
            //            yield return bufferedHeaders.Current;
            //    }
            //    else
            //    {
            //        // return flushed headers from HttpContext (may be also set by ASP application)

            //        var context = HttpContext.Current;
            //        if (context != null)
            //        {
            //            HttpResponse response = context.Response;

            //            foreach (string key in response.Headers.Keys)
            //            {
            //                string values = response.Headers[key];
            //                if (values != null)
            //                    foreach (var value in values.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            //                        yield return new KeyValuePair<string, string>(key, value);
            //            }
            //        }
            //    }
            //}

            #endregion
        }

        #endregion


    }
}
