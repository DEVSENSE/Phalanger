/*

 Copyright (c) 2007 Tomas Petricek
  
 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Globalization;
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
        protected readonly HttpContext/*!*/httpContext;
        
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
            if (UsingIntegratedPipeline)
                return new IntegratedPipelineHeaders();
            
            return new HttpHeaders(true);
        }

        /// <summary>
        /// Try to attach the Headers object into the HttpRuntime.
        /// Do not allow instantiating this class from outside.
        /// </summary>
        private HttpHeaders(bool attach)
        {
            this.httpContext = HttpContext.Current;
                
            if (attach)
            {
                if (this.httpContext != null)
                    TryAttachApplication(this.httpContext.ApplicationInstance);
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
            protected Encoding encoding;
            protected string encodingAsString;

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
            public virtual void SetEncoding(HttpResponse/*!*/response)
            {
                if (IsSpecial(encodingAsString))
                    response.AppendHeader("content-encoding", encodingAsString);
                // by default, set the Encoding properly
                else
                    response.ContentEncoding = this.Encoding;
            }

            /// <summary>
            /// Special encodings, that should be added as header (not via ContentEncoding property, since it is not real encoding).
            /// </summary>
            /// <param name="encodingAsString">Encoding as string.</param>
            /// <returns>Tru if encoding should be set via headers.</returns>
            protected static bool IsSpecial(string encodingAsString)
            {
                    // following values must be set as a string,
                    // it cannot be converted to proper Encoding
                return encodingAsString == "gzip" || encodingAsString == "deflate";
            }
        }

        /// <summary>
        /// Current content-encoding header if set.
        /// </summary>
        protected StringEncoding contentEncoding
        {
            get { return _contentEncoding ?? (_contentEncoding = CreateStringEncoding()); }
            set { _contentEncoding = value; }
        }
        private StringEncoding _contentEncoding;

        /// <summary>
        /// Create StringEncoding object according to the current implementation of Headers.
        /// </summary>
        /// <returns></returns>
        protected virtual StringEncoding CreateStringEncoding()
        {
            return new StringEncoding();
        }

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
        internal readonly Dictionary<string, string> headers = new Dictionary<string, string>();

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
                HttpResponse response = this.httpContext.Response;
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

            //if (this.httpContext != null)
            //{
            //    try
            //    {
            //        HttpResponse response = this.httpContext.Response;
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
            HttpResponse response = this.httpContext.Response;
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
            if (contentType == null || CultureInfo.InvariantCulture.TextInfo.ToLower(contentType).StartsWith("text/"))
                return RequestContext.CurrentContext.DefaultResponseEncoding;
            else
                return Configuration.Application.Globalization.PageEncoding;
                
        }

        #endregion

        #region Integrated Pipeline

        private class IntegratedPipelineHeaders : HttpHeaders
        {
            #region Fields

            /// <summary>
            /// Value of "X-Powered-By" header.
            /// </summary>
            private static readonly string/*!*/PoweredByHeader = PhalangerVersion.ProductName + " " + PhalangerVersion.Current;

            #endregion

            #region ctor

            public IntegratedPipelineHeaders()
                :base(false)
            {
                if (httpContext != null)
                    httpContext.Response.Headers["X-Powered-By"] = PoweredByHeader;
            }

            #endregion

            #region StringEncoding (for Integrated pipeline)

            private class IntegratedPipelineStringEncoding : StringEncoding
            {
                public override void SetEncoding(HttpResponse response)
                {
                    if (IsSpecial(encodingAsString))
                        response.Headers["content-encoding"] = encodingAsString;
                    // by default, set the Encoding properly
                    else
                        base.SetEncoding(response);
                }
            }
            protected override StringEncoding CreateStringEncoding()
            {
                return new IntegratedPipelineStringEncoding();
            }

            #endregion

            #region HttpHeaders

            /// <summary>
            /// Set/remove the header in integrated pipeline mode.
            /// </summary>
            /// <param name="header">The header name. Case insensitive.</param>
            /// <returns>The header value.</returns>
            /// <exception cref="System.FormatException">Given expires header has invalid format.</exception>
            public override string this[string header]
            {
                get
                {
                    return base[header] ?? httpContext.Response.Headers[header];
                }
                set
                {
                    base[header] = value;

                    // store the header immediately into the buffered response
                    //header = header.ToLowerInvariant();
                    var response = httpContext.Response;

                    if (header.EqualsOrdinalIgnoreCase("location"))
                    {
                        response.RedirectLocation = location;
                    }
                    else if (header.EqualsOrdinalIgnoreCase("content-type"))
                    {
                        response.ContentType = contentType;
                        response.ContentEncoding = contentEncoding.Encoding;
                    }
                    //else if (header.EqualsOrdinalIgnoreCase("set-cookie"))
                    //{
                    //    response.AddHeader(header, value);
                    //}
                    else if (header.EqualsOrdinalIgnoreCase("content-length"))
                    {
                        // ignore content-length header, it is set correctly by IIS. If set by the app, mostly it is not correct value (strlen() issue).
                    }
                    else if (header.EqualsOrdinalIgnoreCase("content-encoding"))
                    {
                        if (_contentEncoding != null) _contentEncoding.SetEncoding(response);// on IntegratedPipeline, set immediately to Headers
                        else response.ContentEncoding = RequestContext.CurrentContext.DefaultResponseEncoding;
                    }
                    else if (header.EqualsOrdinalIgnoreCase("expires"))
                    {
                        SetExpires(response, value);
                    }
                    else if (header.EqualsOrdinalIgnoreCase("cache-control"))
                    {
                        CacheLimiter(response, value, null);// ignore invalid cache limiter?
                    }
                    else if (header.EqualsOrdinalIgnoreCase("set-cookie"))
                    {
                        if (value != null)
                            response.AddHeader(header, value);
                    }
                    else
                    {
                        if (value != null) response.Headers[header] = value;
                        else response.Headers.Remove(header);
                    }
                }
            }

            public override void Flush(HttpContext ctx)
            {
                flushed = true;
                // do not flush on Integrated Pipeline
            }

            public override void Clear()
            {
                base.Clear();

                HttpResponse response = httpContext.Response;

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

            //        var context = httpContext;
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

        #region Cache-Control

        /// <summary>
        /// Parse given cache-control header value and set it properly into the HttpContext.Response.Cache object.
        /// </summary>
        /// <param name="response">Current <see cref="HttpContext.Response"/>.</param>
        /// <param name="newLimiter">String value of response cache-header.</param>
        /// <param name="invalidCacheLimiterCallback">Callback function called when invalid cache-limiter value is found. Can be null to take no action.</param>
        public static void CacheLimiter(HttpResponse/*!*/response, string newLimiter, Action<string> invalidCacheLimiterCallback)
        {
            if (string.IsNullOrEmpty(newLimiter))
                return;

            Debug.Assert(response != null);

            // store the header into HttpHeaders.headers dictionary (because of classic pipeline and to allow reading of the CacheLimiter value later)
            var context = ScriptContext.CurrentContext;
            if (context != null && context.Headers != null)
                context.Headers.headers["cache-control"] = newLimiter;
            
            //
            var compareInfo = CultureInfo.CurrentCulture.CompareInfo;

            if (newLimiter.IndexOf(',') < 0)
            {
                CacheLimiterInternal(response, newLimiter, invalidCacheLimiterCallback, compareInfo);
            }
            else
            {
                foreach (var singleLimiter in newLimiter.Split(','))
                    CacheLimiterInternal(response, singleLimiter, invalidCacheLimiterCallback, compareInfo);
            }
        }

        /// <summary>
        /// Updates the cache control of the given HttpContext.
        /// </summary>
        /// <param name="response">Current HttpResponse instance.</param>
        /// <param name="singleLimiter">Cache limiter passed to the session_cache_limiter() PHP function.</param>
        /// <param name="compareInfo">The current compare info used internally.</param>
        /// <param name="invalidCacheLimiterCallback">Function called when invalid limiter is found.</param>
        private static void CacheLimiterInternal(HttpResponse response, string/*!*/singleLimiter, Action<string> invalidCacheLimiterCallback, CompareInfo/*!*/compareInfo)
        {
            Debug.Assert(singleLimiter != null);

            singleLimiter = singleLimiter.Trim();

            if (singleLimiter.Length == 0)
                return;

            if (compareInfo.Compare(singleLimiter, "private", CompareOptions.IgnoreCase) == 0)
                response.Cache.SetCacheability(HttpCacheability.Private);
            else if (compareInfo.Compare(singleLimiter, "public", CompareOptions.IgnoreCase) == 0)
                response.Cache.SetCacheability(HttpCacheability.Public);
            else if (compareInfo.Compare(singleLimiter, "no-cache", CompareOptions.IgnoreCase) == 0)
                response.Cache.SetCacheability(HttpCacheability.NoCache);
            else if (compareInfo.Compare(singleLimiter, "private_no_expire", CompareOptions.IgnoreCase) == 0)
                response.Cache.SetCacheability(HttpCacheability.Private);
            else if (compareInfo.Compare(singleLimiter, "nocache", CompareOptions.IgnoreCase) == 0)
                response.Cache.SetCacheability(HttpCacheability.NoCache);
            else if (compareInfo.Compare(singleLimiter, "must-revalidate", CompareOptions.IgnoreCase) == 0)
                response.Cache.SetRevalidation(HttpCacheRevalidation.AllCaches);
            else if (compareInfo.Compare(singleLimiter, "no-store", CompareOptions.IgnoreCase) == 0)
                response.Cache.SetNoStore();
            else if (compareInfo.Compare(singleLimiter, "no-transform", CompareOptions.IgnoreCase) == 0)
                response.Cache.SetNoTransforms();
            else if (compareInfo.Compare(singleLimiter, "proxy-revalidate", CompareOptions.IgnoreCase) == 0)
                response.Cache.SetRevalidation(HttpCacheRevalidation.ProxyCaches);
            else
            {
                // <key = value> pairs
                int eqindex = 0;
                if ((eqindex = singleLimiter.IndexOf('=')) > 0 && eqindex < singleLimiter.Length - 1)// does not allow '=' at start or end
                {
                    string key = singleLimiter.Substring(0, eqindex).TrimEnd();
                    string value = singleLimiter.Substring(eqindex + 1).TrimStart();

                    int intvalue;

                    if (compareInfo.Compare(key, "max-age", CompareOptions.IgnoreCase) == 0 && int.TryParse(value, out intvalue))
                    {
                        response.Cache.SetMaxAge(new TimeSpan(0, 0, intvalue));// "max-age=seconds"
                        return;
                    }
                    else if (compareInfo.Compare(key, "s-maxage", CompareOptions.IgnoreCase) == 0 && int.TryParse(value, out intvalue))
                    {
                        response.Cache.SetProxyMaxAge(new TimeSpan(0, 0, intvalue));// "s-maxage=seconds"
                        return;
                    }
                }

                // not valid cache-control header
                if (invalidCacheLimiterCallback != null)
                    invalidCacheLimiterCallback(singleLimiter);
            }
        }

        #endregion

        #region Expires

        /// <summary>
        /// Set the Expires HTTP header properly. Parse the given string.
        /// </summary>
        /// <param name="response">HttpResponse to set the Expires header to.</param>
        /// <param name="value">The raw value of Expires header.</param>
        private static void SetExpires(HttpResponse/*!*/response, string value)
        {
            if (value != null)
            {
                int intvalue;
                if (int.TryParse(value, out intvalue))
                {
                    response.Expires = intvalue;
                }
                else
                {
                    DateTime date;
                    if (!DateTime.TryParse(value, out date))
                    {
                        Func<string, string[], string> remover = (/*!*/str, /*!*/prefixes) =>
                            {
                                foreach (var prefix in prefixes)
                                    if (str.StartsWith(prefix))
                                        return str.Substring(prefix.Length);
                                return str;
                            };
                        // remove(ignore) the day of week
                        value = remover(value, new string[] { "Mon,", "Tue,", "Wed,", "Thu,", "Fri,", "Sat,", "Sun," });
                        if (!DateTime.TryParse(value, out date))
                        {
                            throw new ArgumentException("Not a valid DateTime!", "value");
                        }
                    }

                    response.ExpiresAbsolute = date;
                }
            }
            else
            {
                response.Expires = -1;
            }
        }

        #endregion
    }
}
