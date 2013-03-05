using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;

namespace PHP.Library.Curl
{
    internal static class Utils
    {

        public static Uri CompleteUri(string uri, string defaultScheme, int port)
        {     
            if (uri.IndexOf("://") == -1)
            {
                uri = defaultScheme + uri;
            }

            var resultUri = new Uri(uri);

            if (resultUri.IsDefaultPort && port != 0)
                resultUri = CreateUriWithExplicitPort(resultUri, port);  // TODO: if port is specified in uri don't use explicitly set port

            return resultUri;
        }

        public static Uri CreateUriWithExplicitPort(Uri uri, int port)
        {
            UriBuilder uriBuilder = new UriBuilder(uri.Scheme,uri.Host, port, uri.AbsolutePath, uri.Query);

            if (uri.UserInfo.Length > 0)
            {
                var user = uri.UserInfo.Split(':');

                uriBuilder.UserName = user[0];
                uriBuilder.Password = user.Length > 1 ? user[1] : "";
            }


            return uriBuilder.Uri;
        }


        struct ContentType
        {
            public string Extension;
            public string Type;

            public ContentType(string Extension, string Type)
            {
                this.Extension = Extension;
                this.Type = Type;
            }

        };

        static readonly ContentType[] ContentTypes ={
                new ContentType(".gif",  "image/gif"),
                new ContentType(".jpg",  "image/jpeg"),
                new ContentType(".jpeg", "image/jpeg"),
                new ContentType(".txt",  "text/plain"),
                new ContentType(".html", "text/html"),
                new ContentType(".xml", "application/xml")
              };


        /// <summary>
        ///  What kind of Content-Type to use on un-specified files with unrecognized extensions. 
        /// </summary>
        public const string HTTPPOST_CONTENTTYPE_DEFAULT = "application/octet-stream";



        /// <summary>
        /// Provides content type for filename if one of the known types
        ///  (else either the prevtype or the default is returned).
        /// </summary>
        /// <param name="filename">Name of the file</param>
        /// <param name="previousType">Name of ContentType chosen by previous call of this method</param>
        /// <returns>Valid contenttype for filename</returns>
        public static string ContentTypeForFilename(string filename, string previousType = null)
        {
            string contenttype = HTTPPOST_CONTENTTYPE_DEFAULT;
            /*
             * No type was specified, we scan through a few well-known
             * extensions and pick the first we match!
             */

            if (previousType != null)
                /* default to the previously set/used! */
                contenttype = previousType;
            else
                contenttype = HTTPPOST_CONTENTTYPE_DEFAULT;


            if (filename == null)  /* in case a NULL was passed in */
                return contenttype;

            foreach (var ct in ContentTypes)
                if (filename.Length >= ct.Extension.Length)
                {
                    if (filename.IndexOf(ct.Extension, filename.Length - ct.Extension.Length) != -1)
                    {
                        contenttype = ct.Type;
                        break;
                    }
                }


            /* we have a contenttype by now */
            return contenttype;
        }


    }
}
