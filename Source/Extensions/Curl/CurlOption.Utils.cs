/*

 Copyright (c) 2005-2011 Devsense.  

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PHP.Core;

namespace PHP.Library.Curl
{

    /// <summary>
    /// Utilities for validation and conversion of <see cref="CurlOption"/>
    /// </summary>
    public static class CurlOptionUtils
    {

        /// <summary>
        /// Validate if value has appropriate type for option or can be converted to one
        /// </summary>
        public static object ValidateAndConvert(this CurlOption option, object value, out bool success)//TODO:Review this function
        {
            switch (option)
            {
                case CurlOption.CURLOPT_AUTOREFERER:
                case CurlOption.CURLOPT_BINARYTRANSFER:
                case CurlOption.CURLOPT_COOKIESESSION:
                //case CurlOptions.CURLOPT_CERTINFO:		
                case CurlOption.CURLOPT_CRLF:
                case CurlOption.CURLOPT_DNS_USE_GLOBAL_CACHE:
                case CurlOption.CURLOPT_FAILONERROR:
                case CurlOption.CURLOPT_FILETIME:
                case CurlOption.CURLOPT_FOLLOWLOCATION:
                case CurlOption.CURLOPT_FORBID_REUSE:
                case CurlOption.CURLOPT_FRESH_CONNECT:
                case CurlOption.CURLOPT_FTP_USE_EPRT:
                case CurlOption.CURLOPT_FTP_USE_EPSV:
                case CurlOption.CURLOPT_FTPAPPEND:
                //case CurlOptions.CURLOPT_FTPASCII:		
                case CurlOption.CURLOPT_FTPLISTONLY:
                case CurlOption.CURLOPT_HEADER:
                //case CurlOptions.CURLINFO_HEADER_OUT:
                case CurlOption.CURLOPT_HTTPGET:
                case CurlOption.CURLOPT_HTTPPROXYTUNNEL:
                //case CurlOptions.CURLOPT_MUTE:		
                case CurlOption.CURLOPT_NETRC:
                case CurlOption.CURLOPT_NOBODY:
                case CurlOption.CURLOPT_NOPROGRESS:
                case CurlOption.CURLOPT_NOSIGNAL:
                case CurlOption.CURLOPT_POST:
                case CurlOption.CURLOPT_PUT:
                case CurlOption.CURLOPT_RETURNTRANSFER:
                case CurlOption.CURLOPT_SSL_VERIFYPEER:
                case CurlOption.CURLOPT_TRANSFERTEXT:
                case CurlOption.CURLOPT_UNRESTRICTED_AUTH:
                case CurlOption.CURLOPT_UPLOAD:
                case CurlOption.CURLOPT_VERBOSE:

                    return PHP.Core.Convert.TryObjectToBoolean(value, out success);

                case CurlOption.CURLOPT_BUFFERSIZE:
                case CurlOption.CURLOPT_CLOSEPOLICY:
                case CurlOption.CURLOPT_CONNECTTIMEOUT:
                //case CurlOptions.CURLOPT_CONNECTTIMEOUT_MS:
                case CurlOption.CURLOPT_DNS_CACHE_TIMEOUT:
                case CurlOption.CURLOPT_FTPSSLAUTH:
                case CurlOption.CURLOPT_HTTP_VERSION:
                case CurlOption.CURLOPT_HTTPAUTH:
                case CurlOption.CURLOPT_INFILESIZE:
                case CurlOption.CURLOPT_LOW_SPEED_LIMIT:
                case CurlOption.CURLOPT_LOW_SPEED_TIME:
                case CurlOption.CURLOPT_MAXCONNECTS:
                case CurlOption.CURLOPT_MAXREDIRS:
                case CurlOption.CURLOPT_PORT:
                //case CurlOptions.CURLOPT_PROTOCOLS:
                case CurlOption.CURLOPT_PROXYAUTH:
                case CurlOption.CURLOPT_PROXYPORT:
                case CurlOption.CURLOPT_PROXYTYPE:
                //case CurlOptions.CURLOPT_REDIR_PROTOCOLS:
                case CurlOption.CURLOPT_RESUME_FROM:
                case CurlOption.CURLOPT_SSL_VERIFYHOST:
                case CurlOption.CURLOPT_SSLVERSION:
                case CurlOption.CURLOPT_TIMECONDITION:
                case CurlOption.CURLOPT_TIMEOUT:
                //case CurlOptions.CURLOPT_TIMEOUT_MS:
                case CurlOption.CURLOPT_TIMEVALUE:

                    return PHP.Core.Convert.TryObjectToInt32(value, out success);

                case CurlOption.CURLOPT_CAINFO:
                case CurlOption.CURLOPT_CAPATH:
                case CurlOption.CURLOPT_COOKIE:
                case CurlOption.CURLOPT_COOKIEFILE:
                case CurlOption.CURLOPT_COOKIEJAR:
                case CurlOption.CURLOPT_CUSTOMREQUEST:
                //case CurlOptions.CURLOPT_EGDSOCKET:		
                case CurlOption.CURLOPT_ENCODING:
                case CurlOption.CURLOPT_FTPPORT:
                case CurlOption.CURLOPT_INTERFACE:
                case CurlOption.CURLOPT_KRB4LEVEL:
                case CurlOption.CURLOPT_POSTFIELDS:
                case CurlOption.CURLOPT_PROXY:
                case CurlOption.CURLOPT_PROXYUSERPWD:
                case CurlOption.CURLOPT_PROXYUSERNAME:
                case CurlOption.CURLOPT_PROXYPASSWORD:
                case CurlOption.CURLOPT_RANDOM_FILE:
                case CurlOption.CURLOPT_RANGE:
                case CurlOption.CURLOPT_REFERER:
                case CurlOption.CURLOPT_SSL_CIPHER_LIST:
                case CurlOption.CURLOPT_SSLCERT:
                //case CurlOptions.CURLOPT_SSLCERTPASSWD://it is equivavalent to CurlOptions.CURLOPT_SSLKEYPASSWD
                case CurlOption.CURLOPT_SSLCERTTYPE:
                case CurlOption.CURLOPT_SSLENGINE:
                case CurlOption.CURLOPT_SSLENGINE_DEFAULT:
                case CurlOption.CURLOPT_SSLKEY:
                case CurlOption.CURLOPT_SSLKEYPASSWD:
                case CurlOption.CURLOPT_SSLKEYTYPE:
                case CurlOption.CURLOPT_URL:
                case CurlOption.CURLOPT_USERAGENT:
                case CurlOption.CURLOPT_USERPWD:
                case CurlOption.CURLOPT_USERNAME:
                case CurlOption.CURLOPT_PASSWORD:

                    success = PhpVariable.IsString(value);
                    return value;

                case CurlOption.CURLOPT_HTTP200ALIASES:
                case CurlOption.CURLOPT_HTTPHEADER:
                case CurlOption.CURLOPT_POSTQUOTE:
                case CurlOption.CURLOPT_QUOTE:

                    success = value is PhpArray;
                    return value;

                case CurlOption.CURLOPT_FILE:
                case CurlOption.CURLOPT_INFILE:
                case CurlOption.CURLOPT_STDERR:
                case CurlOption.CURLOPT_WRITEHEADER:

                    success = value is PhpResource;
                    return value;

                case CurlOption.CURLOPT_HEADERFUNCTION:
                //case CurlOptions.CURLOPT_PASSWDFUNCTION:
                case CurlOption.CURLOPT_PROGRESSFUNCTION:
                case CurlOption.CURLOPT_READFUNCTION:
                case CurlOption.CURLOPT_WRITEFUNCTION:

                    PhpCallback callback = PHP.Core.Convert.ObjectToCallback(value, false);
                    success = true;// if it's not successful exception is thrown
                    return callback;

                default:
                    success = false;
                    return null;
            }
        }



    }

}
