using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PHP.Core;
using System.Net;
using System.IO;

namespace PHP.Library.Curl
{
    /// <summary>
    /// Handle representing a cURL session
    /// </summary>
    [Serializable]
    public sealed class PhpCurlResource : PhpResource
    {
        private CURLcode errorCode;
        private UserDefined data;
        private CurlHandler handler;

        internal UserDefined Data
        {
            get { return data; }
        }

        internal CURLcode ErrorCode
        {
            get { return errorCode; }
        }

        internal string ErrorMsg
        {
            get { return null; }//TODO: implement through resources
        }

        /// <summary>
        /// Initialize a new handle of cURL session
        /// </summary>
        public PhpCurlResource()
            : base("Curl")
        {
            data = new UserDefined();
            handler = CurlHandler.Create(CurlProto.HTTP);// for now we just support HTTP handeling
        }

        /// <summary>
        /// Initialize a new handle of cURL session with Uri initialized
        /// </summary>
        public PhpCurlResource(string uri)
            : this()
        {
            //TODO: if uri is without http:// add it
            data.Str[(int)DupString.SET_URL] = uri;

        }

        /// <summary>
        /// Executes cURL transfer
        /// </summary>
        /// <returns></returns>
        internal object Execute()
        {
            return handler.Execute(this, ref errorCode);
        }

        /// <summary>
        /// Gets information information associated with a cURL transfer
        /// </summary>
        /// <param name="info">This parameter specifies item to be retrieved.</param>
        /// <returns>Returns an item specified by info paramter. Item can be <c>int</c>, a
        /// <c>double</c>, a <c>string</c>, a <c>DateTime</c> or an <c>object</c>.
        /// </returns>
        internal object GetInfo(CurlInfo info)
        {
            return handler.GetInfo(info);
        }

        /// <summary>
        /// Closes handle
        /// </summary>
        public override void Close()
        {
            Cookies.FlushCookies(data);
            base.Close();
        }

    }
}
