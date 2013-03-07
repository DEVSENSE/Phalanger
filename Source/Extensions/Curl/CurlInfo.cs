using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PHP.Core;

namespace PHP.Library.Curl
{
    /// <summary>
    /// This enumeration is used to extract information associated with an
    /// <see cref="Curl"/> transfer. Specifically, a member of this
    /// enumeration is passed as the first argument to
    /// <see cref="Curl.GetInfo(PhpResource, CurlInfo)"/> specifying the item to retrieve in the
    /// second argument, which is a reference to an <c>int</c>, a
    /// <c>double</c>, a <c>string</c>, a <c>DateTime</c> or an <c>object</c>.
    /// </summary>
    public enum CurlInfo
    {
        /// <summary>
        /// The second argument receives the elapsed time, as a <c>double</c>,
        /// in seconds, from the start until the connect to the remote host
        /// (or proxy) was completed. 
        /// </summary>
        [ImplementsConstant("CURLINFO_CONNECT_TIME")]
        CONNECT_TIME = 0x300005,
        /// <summary>
        /// The second argument receives, as a <c>double</c>, the content-length
        /// of the download. This is the value read from the Content-Length: field. 
        /// </summary>
        [ImplementsConstant("CURLINFO_CONTENT_LENGTH_DOWNLOAD")]
        CONTENT_LENGTH_DOWNLOAD = 0x30000F,
        /// <summary>
        /// The second argument receives, as a <c>double</c>, the specified size
        /// of the upload. 
        /// </summary>
        [ImplementsConstant("CURLINFO_CONTENT_LENGTH_UPLOAD")]
        CONTENT_LENGTH_UPLOAD = 0x300010,
        /// <summary>
        /// The second argument receives, as a <c>string</c>, the content-type of
        /// the downloaded object. This is the value read from the Content-Type:
        /// field. If you get <c>null</c>, it means that the server didn't
        /// send a valid Content-Type header or that the protocol used
        /// doesn't support this. 
        /// </summary>
        [ImplementsConstant("CURLINFO_CONTENT_TYPE")]
        CONTENT_TYPE = 0x100012,
        /// <summary>
        /// The second argument receives, as a <c>string</c>, the last
        /// used effective URL. 
        /// </summary>
        [ImplementsConstant("CURLINFO_EFFECTIVE_URL")]
        EFFECTIVE_URL = 0x100001,
        /// <summary>
        /// The second argument receives, as a <c>long</c>, the remote time
        /// of the retrieved document. You should construct a <c>DateTime</c>
        /// from this value, as shown in the <c>InfoDemo</c> sample. If you
        /// get a date in the distant
        /// past, it can be because of many reasons (unknown, the server
        /// hides it or the server doesn't support the command that tells
        /// document time etc) and the time of the document is unknown. Note
        /// that you must tell the server to collect this information before
        /// the transfer is made, by using the 
        /// <see cref="CurlOption.CURLOPT_FILETIME"/> option to
        /// <see cref="Curl.SetOpt"/>. (Added in 7.5) 
        /// </summary>
        [ImplementsConstant("CURLINFO_FILETIME")]
        FILETIME = 0x20000E,
        /// <summary>
        /// The second argument receives an <c>int</c> specifying the total size
        /// of all the headers received. 
        /// </summary>
        [ImplementsConstant("CURLINFO_HEADER_SIZE")]
        HEADER_SIZE = 0x20000B,
        /// <summary>
        /// The second argument receives, as an <c>int</c>, a bitmask indicating
        /// the authentication method(s) available. The meaning of the bits is
        /// explained in the documentation of
        /// <see cref="CurlOption.CURLOPT_HTTPAUTH"/>. (Added in 7.10.8) 
        /// </summary>
        [ImplementsConstant("CURLINFO_HTTPAUTH_AVAIL")]
        HTTPAUTH_AVAIL = 0x200017,
        /// <summary>
        /// The second argument receives an <c>int</c> indicating the numeric
        /// connect code for the HTTP request.
        /// </summary>
        [ImplementsConstant("CURLINFO_HTTP_CONNECTCODE")]
        HTTP_CONNECTCODE = 0x200016,
        /// <summary>
        /// End-of-enumeration marker; do not use in client applications.
        /// </summary>
        [ImplementsConstant("CURLINFO_LASTONE")]
        LASTONE = 0x1C,
        /// <summary>
        /// The second argument receives, as a <c>double</c>, the time, in
        /// seconds it took from the start until the name resolving was
        /// completed. 
        /// </summary>
        [ImplementsConstant("CURLINFO_NAMELOOKUP_TIME")]
        NAMELOOKUP_TIME = 0x300004,
        /// <summary>
        /// Never used.
        /// </summary>
        [ImplementsConstant("CURLINFO_NONE")]
        NONE = 0x0,
        /// <summary>
        /// The second argument receives an <c>int</c> indicating the
        /// number of current connections. (Added in 7.13.0)
        /// </summary>
        [ImplementsConstant("CURLINFO_NUM_CONNECTS")]
        NUM_CONNECTS = 0x20001A,
        /// <summary>
        /// The second argument receives an <c>int</c> indicating the operating
        /// system error number: <c>_errro</c> or <c>GetLastError()</c>,
        /// depending on the platform. (Added in 7.12.2)
        /// </summary>
        [ImplementsConstant("CURLINFO_OS_ERRNO")]
        OS_ERRNO = 0x200019,
        /// <summary>
        /// The second argument receives, as a <c>double</c>, the time, in
        /// seconds, it took from the start until the file transfer is just about
        /// to begin. This includes all pre-transfer commands and negotiations
        /// that are specific to the particular protocol(s) involved. 
        /// </summary>
        [ImplementsConstant("CURLINFO_PRETRANSFER_TIME")]
        PRETRANSFER_TIME = 0x300006,
        /// <summary>
        /// The second argument receives a reference to the private data
        /// associated with the <see cref="Curl"/> object (set with the
        /// <see cref="CurlOption.CURLOPT_PRIVATE"/> option to
        /// <see cref="Curl.SetOpt"/>. (Added in 7.10.3) 
        /// </summary>
        [ImplementsConstant("CURLINFO_PRIVATE")]
        PRIVATE = 0x100015,
        /// <summary>
        /// The second argument receives, as an <c>int</c>, a bitmask
        /// indicating the authentication method(s) available for your
        /// proxy authentication. This will be a bitmask of
        /// <see cref="CURLhttpAuth"/> enumeration constants.
        /// (Added in 7.10.8) 
        /// </summary>
        [ImplementsConstant("CURLINFO_PROXYAUTH_AVAIL")]
        PROXYAUTH_AVAIL = 0x200018,
        /// <summary>
        /// The second argument receives an <c>int</c> indicating the total
        /// number of redirections that were actually followed. (Added in 7.9.7) 
        /// </summary>
        [ImplementsConstant("CURLINFO_REDIRECT_COUNT")]
        REDIRECT_COUNT = 0x200014,
        /// <summary>
        /// The second argument receives, as a <c>double</c>, the total time, in
        /// seconds, for all redirection steps include name lookup, connect,
        /// pretransfer and transfer before final transaction was started.
        /// <c>CURLINFO_REDIRECT_TIME</c> contains the complete execution
        /// time for multiple redirections. (Added in 7.9.7) 
        /// </summary>
        [ImplementsConstant("CURLINFO_REDIRECT_TIME")]
        REDIRECT_TIME = 0x300013,
        /// <summary>
        /// The second argument receives an <c>int</c> containing the total size
        /// of the issued requests. This is so far only for HTTP requests. Note
        /// that this may be more than one request if
        /// <see cref="CurlOption.CURLOPT_FOLLOWLOCATION"/> is <c>true</c>.
        /// </summary>
        [ImplementsConstant("CURLINFO_REQUEST_SIZE")]
        REQUEST_SIZE = 0x20000C,
        /// <summary>
        /// The second argument receives an <c>int</c> with the last received HTTP
        /// or FTP code. This option was known as <c>CURLINFO_HTTP_CODE</c> in
        /// libcurl 7.10.7 and earlier. In actual version is called CURLINFO_RESPONSE_CODE
        /// but in PHP it's still CURLINFO_HTTP_CODE
        /// </summary>
        [ImplementsConstant("CURLINFO_HTTP_CODE")]
        HTTP_CODE = 0x200002,
        /// <summary>
        /// The second argument receives a <c>double</c> with the total amount of
        /// bytes that were downloaded. The amount is only for the latest transfer
        /// and will be reset again for each new transfer. 
        /// </summary>
        [ImplementsConstant("CURLINFO_SIZE_DOWNLOAD")]
        SIZE_DOWNLOAD = 0x300008,
        /// <summary>
        /// The second argument receives a <c>double</c> with the total amount
        /// of bytes that were uploaded. 
        /// </summary>
        [ImplementsConstant("CURLINFO_SIZE_UPLOAD")]
        SIZE_UPLOAD = 0x300007,
        /// <summary>
        /// The second argument receives a <c>double</c> with the average
        /// download speed that cURL measured for the complete download. 
        /// </summary>
        [ImplementsConstant("CURLINFO_SPEED_DOWNLOAD")]
        SPEED_DOWNLOAD = 0x300009,
        /// <summary>
        /// The second argument receives a <c>double</c> with the average
        /// upload speed that libcurl measured for the complete upload. 
        /// </summary>
        [ImplementsConstant("CURLINFO_SPEED_UPLOAD")]
        SPEED_UPLOAD = 0x30000A,
        /// <summary>
        /// The second argument receives an list containing
        /// the names of the available SSL engines.
        /// </summary>
        [ImplementsConstant("CURLINFO_SSL_ENGINES")]
        SSL_ENGINES = 0x40001B,
        /// <summary>
        /// The second argument receives an <c>int</c> with the result of
        /// the certificate verification that was requested (using the
        /// <see cref="CurlOption.CURLOPT_SSL_VERIFYPEER"/> option in
        /// <see cref="Curl.SetOpt"/>. 
        /// </summary>
        [ImplementsConstant("CURLINFO_SSL_VERIFYRESULT")]
        SSL_VERIFYRESULT = 0x20000D,
        /// <summary>
        /// The second argument receives a <c>double</c> specifying the time,
        /// in seconds, from the start until the first byte is just about to be
        /// transferred. This includes <c>CURLINFO_PRETRANSFER_TIME</c> and
        /// also the time the server needs to calculate the result. 
        /// </summary>
        [ImplementsConstant("CURLINFO_STARTTRANSFER_TIME")]
        STARTTRANSFER_TIME = 0x300011,
        /// <summary>
        /// The second argument receives a <c>double</c> indicating the total transaction
        /// time in seconds for the previous transfer. This time does not include
        /// the connect time, so if you want the complete operation time,
        /// you should add the <c>CURLINFO_CONNECT_TIME</c>. 
        /// </summary>
        [ImplementsConstant("CURLINFO_TOTAL_TIME")]
        TOTAL_TIME = 0x300003,
    };
}
