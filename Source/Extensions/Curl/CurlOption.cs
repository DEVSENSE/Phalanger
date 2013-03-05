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
    /// One of these is passed as a parameter to
    /// curl_setopt. The <c>Description</c> column of
    /// the table describes the value that should be passed as the second parameter.
    /// </summary>
    public enum CurlOption
    {

        /// <summary>
        /// Empty options
        /// </summary>
        NONE = 0,

        /// <summary>
        /// Pass a <c>true</c> parameter to enable this. When enabled, libcurl
        /// will automatically set the Referer: field in requests where it follows
        /// a Location: redirect. 
        /// </summary>
        [ImplementsConstant("CURLOPT_AUTOREFERER")]
        CURLOPT_AUTOREFERER = 58,


        /// <summary>
        /// Pass a <c>true</c> to return the raw output when CURLOPT_RETURNTRANSFER is used. 
        /// </summary>
        [ImplementsConstant("CURLOPT_BINARYTRANSFER")]
        CURLOPT_BINARYTRANSFER = 19914,

        /// <summary>
        /// Pass an <c>int</c> specifying your preferred size for the receive buffer
        /// in libcurl. The main point of this would be that the write callback gets
        /// called more often and with smaller chunks. This is just treated as a
        /// request, not an order. You cannot be guaranteed to actually get the
        /// requested size. (Added in 7.10) 
        /// </summary>
        [ImplementsConstant("CURLOPT_BUFFERSIZE")]
        CURLOPT_BUFFERSIZE = 98,
        /// <summary>
        /// Pass a <c>string</c> naming a file holding one or more certificates
        /// to verify the peer with. This only makes sense when used in combination
        /// with the <c>CURLOPT_SSL_VERIFYPEER</c> option.
        /// </summary>
        [ImplementsConstant("CURLOPT_CAINFO")]
        CURLOPT_CAINFO = 10065,
        /// <summary>
        /// Pass a <c>string</c> naming a directory holding multiple CA certificates
        /// to verify the peer with. The certificate directory must be prepared
        /// using the openssl c_rehash utility. This only makes sense when used in
        /// combination with the <c>CURLOPT_SSL_VERIFYPEER</c> option. The
        /// <c>CURLOPT_CAPATH</c> function apparently does not work in Windows due
        /// to some limitation in openssl. (Added in 7.9.8) 
        /// </summary>
        [ImplementsConstant("CURLOPT_CAPATH")]
        CURLOPT_CAPATH = 10097,
        /// <summary>
        /// Pass either CURLCLOSEPOLICY_LEAST_RECENTLY_USED or CURLCLOSEPOLICY_OLDEST.
        /// This option sets what policy libcurl should use when
        /// the connection cache is filled and one of the open connections has to be
        /// closed to make room for a new connection. 
        /// There are three other CURLCLOSEPOLICY_ constants, but cURL does not support them yet.
        /// Use <c>CURLCLOSEPOLICY_LEAST_RECENTLY_USED</c> to make
        /// libcurl close the connection that was least recently used, that connection
        /// is also least likely to be capable of re-use. Use
        /// <c>CURLCLOSEPOLICY_OLDEST</c> to make libcurl close
        /// the oldest connection, the one that was created first among the ones in
        /// the connection cache. 
        /// </summary>
        [ImplementsConstant("CURLOPT_CLOSEPOLICY")]
        CURLOPT_CLOSEPOLICY = 72,
        /// <summary>
        /// Time-out connect operations after this amount of seconds, if connects
        /// are OK within this time, then fine... This only aborts the connect
        /// phase. [Only works on unix-style/SIGALRM operating systems]
        /// </summary>
        [ImplementsConstant("CURLOPT_CONNECTTIMEOUT")]
        CURLOPT_CONNECTTIMEOUT = 78,
        /// <summary>
        /// Pass a <c>string</c> as parameter. It will be used to set a cookie
        /// in the http request. The format of the string should be NAME=CONTENTS,
        /// where NAME is the cookie name and CONTENTS is what the cookie should contain. 
        /// <para>
        /// If you need to set multiple cookies, you need to set them all using a
        /// single option and thus you need to concatenate them all in one single
        /// string. Set multiple cookies in one string like this:
        /// "name1=content1; name2=content2;" etc. 
        /// </para>
        /// <para>
        /// Using this option multiple times will only make the latest string override
        /// the previously ones.
        /// </para>
        /// </summary>
        [ImplementsConstant("CURLOPT_COOKIE")]
        CURLOPT_COOKIE = 10022,
        /// <summary>
        /// Pass a <c>string</c> as parameter. It should contain the name of your
        /// file holding cookie data to read. The cookie data may be in Netscape /
        /// Mozilla cookie data format or just regular HTTP-style headers dumped
        /// to a file.
        /// <para>
        /// Given an empty or non-existing file, this option will enable cookies
        /// for this Easy object, making it understand and parse received cookies
        /// and then use matching cookies in future request. 
        /// </para> 
        /// </summary>
        [ImplementsConstant("CURLOPT_COOKIEFILE")]
        CURLOPT_COOKIEFILE = 10031,
        /// <summary>
        /// Pass a file name as <c>string</c>. This will make libcurl write all
        /// internally known cookies to the specified file when
        /// cURL handle is closed, e.g. after a call to curl_close. If no cookies are known, no file
        /// will be created. Using this option also enables cookies for this
        /// session, so if you for example follow a location it will make matching
        /// cookies get sent accordingly.
        /// </summary>
        /// <remarks>
        /// If the cookie jar file can't be created or written to
        /// (when cURL handle is closing), libcurl will not and
        /// cannot report an error for this. Using <c>CURLOPT_VERBOSE</c> or
        /// <c>CURLOPT_DEBUGFUNCTION</c> will get a warning to display, but that
        /// is the only visible feedback you get about this possibly lethal situation.
        /// </remarks>
        [ImplementsConstant("CURLOPT_COOKIEJAR")]
        CURLOPT_COOKIEJAR = 10082,
        /// <summary>
        /// Pass a <c>bool</c> set to <c>true</c> to mark this as a new cookie
        /// "session". It will force libcurl to ignore all cookies it is about to
        /// load that are "session cookies" from the previous session. By default,
        /// libcurl always stores and loads all cookies, independent of whether they are
        /// session cookies. Session cookies are cookies without expiry date and they
        /// are meant to be alive and existing for this "session" only.
        /// </summary>
        [ImplementsConstant("CURLOPT_COOKIESESSION")]
        CURLOPT_COOKIESESSION = 96,
        /// <summary>
        /// Convert Unix newlines to CRLF newlines on transfers.
        /// </summary>
        [ImplementsConstant("CURLOPT_CRLF")]
        CURLOPT_CRLF = 27,
        /// <summary>
        /// Pass a <c>string</c> as parameter. It will be used instead of GET or
        /// HEAD when doing an HTTP request, or instead of LIST or NLST when
        /// doing an ftp directory listing. This is useful for doing DELETE or
        /// other more or less obscure HTTP requests. Don't do this at will,
        /// make sure your server supports the command first. 
        /// <para>
        /// Restore to the internal default by setting this to <c>null</c>.
        /// </para>
        /// <note>
        /// Many people have wrongly used this option to replace the entire
        /// request with their own, including multiple headers and POST contents.
        /// While that might work in many cases, it will cause libcurl to send
        /// invalid requests and it could possibly confuse the remote server badly.
        /// Use <c>CURLOPT_POST</c> and <c>CURLOPT_POSTFIELDS</c> to set POST data.
        /// Use <c>CURLOPT_HTTPHEADER</c> to replace or extend the set of headers
        /// sent by libcurl. Use <c>CURLOPT_HTTP_VERSION</c> to change HTTP version.
        /// </note>
        /// </summary>
        [ImplementsConstant("CURLOPT_CUSTOMREQUEST")]
        CURLOPT_CUSTOMREQUEST = 10036,


        /// <summary>
        /// Pass an <c>int</c>, specifying the timeout in seconds. Name resolves
        /// will be kept in memory for this number of seconds. Set to zero (0)
        /// to completely disable caching, or set to -1 to make the cached
        /// entries remain forever. By default, libcurl caches this info for 60
        /// seconds.
        /// </summary>
        [ImplementsConstant("CURLOPT_DNS_CACHE_TIMEOUT")]
        CURLOPT_DNS_CACHE_TIMEOUT = 92,
        /// <summary>
        /// Not supported.
        /// </summary>
        [ImplementsConstant("CURLOPT_DNS_USE_GLOBAL_CACHE")]
        CURLOPT_DNS_USE_GLOBAL_CACHE = 91,
        /// <summary>
        /// Pass a <c>string</c> containing the path name to the Entropy Gathering
        /// Daemon socket. It will be used to seed the random engine for SSL.
        /// </summary>
        [ImplementsConstant("CURLOPT_EDGSOCKET")]
        CURLOPT_EDGSOCKET = 10077,
        /// <summary>
        /// Sets the contents of the Accept-Encoding: header sent in an HTTP request,
        /// and enables decoding of a response when a Content-Encoding: header is
        /// received. Three encodings are supported: <c>identity</c>, which does
        /// nothing, <c>deflate</c> which requests the server to compress its
        /// response using the zlib algorithm, and <c>gzip</c> which requests the
        /// gzip algorithm. If a zero-length string is set, then an Accept-Encoding:
        /// header containing all supported encodings is sent.
        /// </summary>
        [ImplementsConstant("CURLOPT_ENCODING")]
        CURLOPT_ENCODING = 10102,
        /// <summary>
        /// Not supported.
        /// </summary>
        [ImplementsConstant("CURLOPT_ERRORBUFFER")]
        CURLOPT_ERRORBUFFER = 10010,
        /// <summary>
        /// A <c>true</c> parameter tells the library to fail silently if the
        /// HTTP code returned is equal to or larger than 300. The default
        /// action would be to return the page normally, ignoring that code. 
        /// </summary>
        [ImplementsConstant("CURLOPT_FAILONERROR")]
        CURLOPT_FAILONERROR = 45,

        /// <summary>
        /// The file that the transfer should be written to. The default is STDOUT (the browser window). 
        /// </summary>
        [ImplementsConstant("CURLOPT_FILE")]
        CURLOPT_FILE = 10001,

        /// <summary>
        /// Pass a <c>bool</c>. If it is <c>true</c>, libcurl will attempt to get
        /// the modification date of the remote document in this operation. This
        /// requires that the remote server sends the time or replies to a time
        /// querying command. The curl_getinfo function with the
        /// <c>CURLINFO_FILETIME</c> argument can be used after a
        /// transfer to extract the received time (if any).
        /// </summary>
        [ImplementsConstant("CURLOPT_FILETIME")]
        CURLOPT_FILETIME = 69,
        /// <summary>
        /// A <c>true</c> parameter tells the library to follow any Location:
        /// header that the server sends as part of an HTTP header.
        /// <note>
        /// this means that the library will re-send the same request on the
        /// new location and follow new Location: headers all the way until no
        /// more such headers are returned. <c>CURLOPT_MAXREDIRS</c> can be used
        /// to limit the number of redirects libcurl will follow.
        /// </note>
        /// </summary>
        [ImplementsConstant("CURLOPT_FOLLOWLOCATION")]
        CURLOPT_FOLLOWLOCATION = 52,
        /// <summary>
        /// Pass a <c>bool</c>. Set to <c>true</c> to make the next transfer
        /// explicitly close the connection when done. Normally, libcurl keeps all
        /// connections alive when done with one transfer in case there comes a
        /// succeeding one that can re-use them. This option should be used with
        /// caution and only if you understand what it does. Set to <c>false</c>
        /// to have libcurl keep the connection open for possibly later re-use
        /// (default behavior). 
        /// </summary>
        [ImplementsConstant("CURLOPT_FORBID_REUSE")]
        CURLOPT_FORBID_REUSE = 75,
        /// <summary>
        /// Pass a <c>bool</c>. Set to <c>true</c> to make the next transfer use a
        /// new (fresh) connection by force. If the connection cache is full before
        /// this connection, one of the existing connections will be closed as
        /// according to the selected or default policy. This option should be used
        /// with caution and only if you understand what it does. Set this to
        /// <c>false</c> to have libcurl attempt re-using an existing connection
        /// (default behavior). 
        /// </summary>
        [ImplementsConstant("CURLOPT_FRESH_CONNECT")]
        CURLOPT_FRESH_CONNECT = 74,
        /// <summary>
        /// String that will be passed to the FTP server when it requests
        /// account info.
        /// </summary>
        [ImplementsConstant("CURLOPT_FTPACCOUNT")]
        CURLOPT_FTPACCOUNT = 10134,
        /// <summary>
        /// A <c>true</c> parameter tells the library to append to the remote
        /// file instead of overwrite it. This is only useful when uploading
        /// to an ftp site. 
        /// </summary>
        [ImplementsConstant("CURLOPT_FTPAPPEND")]
        CURLOPT_FTPAPPEND = 50,
        /// <summary>
        /// A <c>true</c> parameter tells the library to just list the names of
        /// an ftp directory, instead of doing a full directory listing that
        /// would include file sizes, dates etc. 
        /// <para>
        /// This causes an FTP NLST command to be sent. Beware that some FTP
        /// servers list only files in their response to NLST; they might not
        /// include subdirectories and symbolic links.
        /// </para>
        /// </summary>
        [ImplementsConstant("CURLOPT_FTPLISTONLY")]
        CURLOPT_FTPLISTONLY = 48,
        /// <summary>
        /// Pass a <c>string</c> as parameter. It will be used to get the IP
        /// address to use for the ftp PORT instruction. The PORT instruction
        /// tells the remote server to connect to our specified IP address.
        /// The string may be a plain IP address, a host name, an network
        /// interface name (under Unix) or just a '-' letter to let the library
        /// use your systems default IP address. Default FTP operations are
        /// passive, and thus won't use PORT. 
        /// <para>
        /// You disable PORT again and go back to using the passive version
        /// by setting this option to NULL.
        /// </para>
        /// </summary>
        [ImplementsConstant("CURLOPT_FTPPORT")]
        CURLOPT_FTPPORT = 10017,
        /// <summary>
        /// When FTP over SSL/TLS is selected (with <c>CURLOPT_FTP_SSL</c>),
        /// this option can be used to change libcurl's default action which
        /// is to first try "AUTH SSL" and then "AUTH TLS" in this order,
        /// and proceed when a OK response has been received.
        /// The FTP authentication method (when is activated): CURLFTPAUTH_SSL (try SSL first),
        /// CURLFTPAUTH_TLS (try TLS first), or CURLFTPAUTH_DEFAULT (let cURL decide). 
        /// </summary>
        [ImplementsConstant("CURLOPT_FTPSSLAUTH")]
        CURLOPT_FTPSSLAUTH = 129,
        /// <summary>
        /// Pass a <c>bool</c>. If the value is <c>true</c>, cURL will attempt to
        /// create any remote directory that it fails to CWD into. CWD is the
        /// command that changes working directory. (Added in 7.10.7) 
        /// </summary>
        [ImplementsConstant("CURLOPT_FTP_CREATE_MISSING_DIRS")]
        CURLOPT_FTP_CREATE_MISSING_DIRS = 110,
        /// <summary>
        /// Pass an <c>int</c>. Causes libcurl to set a timeout period (in seconds)
        /// on the amount of time that the server is allowed to take in order to
        /// generate a response message for a command before the session is
        /// considered hung. Note that while libcurl is waiting for a response, this
        /// value overrides <c>CURLOPT_TIMEOUT</c>. It is recommended that if used in
        /// conjunction with <c>CURLOPT_TIMEOUT</c>, you set
        /// <c>CURLOPT_FTP_RESPONSE_TIMEOUT</c> to a value smaller than
        /// <c>CURLOPT_TIMEOUT</c>. (Added in 7.10.8) 
        /// </summary>
        [ImplementsConstant("CURLOPT_FTP_RESPONSE_TIMEOUT")]
        CURLOPT_FTP_RESPONSE_TIMEOUT = 112,
        /// <summary>
        /// Pass a <c>CURLFTPSSL_*</c> constant that specifies FTP SSL level.
        /// </summary>
        [ImplementsConstant("CURLOPT_FTP_SSL")]
        CURLOPT_FTP_SSL = 119,
        /// <summary>
        /// Pass a <c>bool</c>. If the value is <c>true</c>, it tells curl to use
        /// the EPRT (and LPRT) command when doing active FTP downloads (which is
        /// enabled by CURLOPT_FTPPORT). Using EPRT means that it will first attempt
        /// to use EPRT and then LPRT before using PORT, but if you pass <c>false</c>
        /// to this option, it will not try using EPRT or LPRT, only plain PORT.
        /// (Added in 7.10.5) 
        /// </summary>
        [ImplementsConstant("CURLOPT_FTP_USE_EPRT")]
        CURLOPT_FTP_USE_EPRT = 106,
        /// <summary>
        /// Pass a <c>bool</c>. If the value is <c>true</c>, it tells curl to use
        /// the EPSV command when doing passive FTP downloads (which it always does
        /// by default). Using EPSV means that it will first attempt to use EPSV
        /// before using PASV, but if you pass <c>false</c> to this option, it will
        /// not try using EPSV, only plain PASV.
        /// </summary>
        [ImplementsConstant("CURLOPT_FTP_USE_EPSV")]
        CURLOPT_FTP_USE_EPSV = 85,
        /// <summary>
        /// A <c>true</c> parameter tells the library to include the header in
        /// the body output. This is only relevant for protocols that actually
        /// have headers preceding the data (like HTTP).
        /// </summary>
        [ImplementsConstant("CURLOPT_HEADER")]
        CURLOPT_HEADER = 42,

        /// <summary>
        /// The name of a callback function where the callback function takes two parameters. 
        /// The first is the cURL resource, the second is a string with the header data to be written. 
        /// The header data must be written when using this callback function. 
        /// Return the number of bytes written. 
        /// </summary>
        /// <remarks>
        /// Provide an HeaderFunction delegate reference.
        /// This delegate gets called by libcurl as soon as there is received
        /// header data that needs to be written down. The headers are guaranteed
        /// to be written one-by-one and only complete lines are written. Parsing
        /// headers should be easy enough using this. The size of the data contained
        /// in <c>buf</c> is <c>size</c> multiplied with <c>nmemb</c>.
        /// Return the number of bytes actually written or return -1 to signal
        /// error to the library (it will cause it to abort the transfer with a
        /// <see cref="CURLcode.CURLE_WRITE_ERROR"/> return code). 
        /// </remarks>
        [ImplementsConstant("CURLOPT_HEADERFUNCTION")]
        CURLOPT_HEADERFUNCTION = 20079,
        /// <summary>
        /// Pass an array of aliases to be treated as valid HTTP
        /// 200 responses. Some servers respond with a custom header response line.
        /// For example, IceCast servers respond with "ICY 200 OK". By including
        /// this string in your list of aliases, the response will be treated as a
        /// valid HTTP header line such as "HTTP/1.0 200 OK". (Added in 7.10.3) 
        /// <note>
        /// The alias itself is not parsed for any version strings. So if your alias
        /// is "MYHTTP/9.9", libcurl will not treat the server as responding with
        /// HTTP version 9.9. Instead libcurl will use the value set by option
        /// <c>CURLOPT_HTTP_VERSION</c>. 
        /// </note>
        /// </summary>
        CURLOPT_HTTP200ALIASES = 10104,
        /// <summary>
        /// Pass an <c>int</c> as parameter, which is set to a bitmask 
        /// of <see cref="CURLhttpAuth"/>, to tell libcurl what authentication
        /// method(s) you want it to use. If more than one bit is set, libcurl will
        /// first query the site to see what authentication methods it supports and
        /// then pick the best one you allow it to use. Note that for some methods,
        /// this will induce an extra network round-trip. Set the actual name and
        /// password with the <c>CURLOPT_USERPWD</c> option. (Added in 7.10.6) 
        /// </summary>
        [ImplementsConstant("CURLOPT_HTTPAUTH")]
        CURLOPT_HTTPAUTH = 107,
        /// <summary>
        /// Pass a <c>bool</c>. <c>TRUE</c> to reset the HTTP request method to GET.
        /// Since GET is the default, this is only necessary if the request method 
        /// has been changed. 
        /// </summary>
        [ImplementsConstant("CURLOPT_HTTPGET")]
        CURLOPT_HTTPGET = 80,
        /// <summary>
        /// Pass an array containing HTTP headers to pass to
        /// the server in your HTTP request. If you add a header that is otherwise
        /// generated and used by libcurl internally, your added one will be used
        /// instead. If you add a header with no contents as in 'Accept:' (no data
        /// on the right side of the colon), the internally used header will get
        /// disabled. Thus, using this option you can add new headers, replace
        /// internal headers and remove internal headers. 
        /// <para>
        /// The first line in a request (usually containing a GET or POST) is not
        /// a header and cannot be replaced using this option. Only the lines
        /// following the request-line are headers. 
        /// </para>
        /// <para>
        /// Pass a <c>null</c> to this to reset back to no custom headers.
        /// </para>
        /// <note>
        /// The most commonly replaced headers have "shortcuts" in the options
        /// <c>CURLOPT_COOKIE</c>, <c>CURLOPT_USERAGENT</c> and <c>CURLOPT_REFERER</c>.
        /// </note>
        /// </summary>
        [ImplementsConstant("CURLOPT_HTTPHEADER")]
        CURLOPT_HTTPHEADER = 10023,

        /// <summary>
        /// Set the parameter to <c>true</c> to get the library to tunnel all
        /// operations through a given HTTP proxy. Note that there is a big
        /// difference between using a proxy and tunneling through it. If you
        /// don't know what this means, you probably don't want this tunneling option. 
        /// </summary>
        [ImplementsConstant("CURLOPT_HTTPPROXYTUNNEL")]
        CURLOPT_HTTPPROXYTUNNEL = 61,
        /// <summary>
        /// Pass a <c>CURL_HTTP_VERSION_NONE</c> (default, lets CURL decide which version to use),
        /// <c>CURL_HTTP_VERSION_1_0</c> (forces HTTP/1.0), or <c>CURL_HTTP_VERSION_1_1</c> (forces HTTP/1.1).
        /// These values force libcurl to use the specific HTTP versions. This is not
        /// sensible to do unless you have a good reason. 
        /// </summary>
        [ImplementsConstant("CURLOPT_HTTP_VERSION")]
        CURLOPT_HTTP_VERSION = 84,

        /// <summary>
        /// Provide an object, such as a <c>FileStream</c>, upon which
        /// you may need to perform an IOCTL operation. Right now, only
        /// rewind is supported.
        /// </summary>
        [ImplementsConstant("CURLOPT_IOCTLDATA")]
        CURLOPT_IOCTLDATA = 10131,
        /// <summary>
        /// When uploading a file to a remote site, this option should be used to
        /// tell libcurl what the expected size of the infile is. This value should
        /// be passed as an <c>int</c>. 
        /// </summary>
        [ImplementsConstant("CURLOPT_INFILESIZE")]
        CURLOPT_INFILESIZE = 14,
        /// <summary>
        /// When uploading a file to a remote site, this option should be used to
        /// tell libcurl what the expected size of the infile is. This value should
        /// be passed as a <c>long</c>. (Added in 7.11.0) 
        /// </summary>
        [ImplementsConstant("CURLOPT_INFILESIZE_LARGE")]
        CURLOPT_INFILESIZE_LARGE = 30115,
        /// <summary>
        /// Pass a <c>string</c> as parameter. This sets the interface name to use
        /// as the outgoing network interface. The name can be an interface name,
        /// an IP address or a host name.
        /// </summary>
        [ImplementsConstant("CURLOPT_INTERFACE")]
        CURLOPT_INTERFACE = 10062,
        /// <summary>
        /// Pass <c>CURL_IPRESOLVE_WHATEVER</c>, <c>CURL_IPRESOLVE_V4</c> or 
        /// <c>CURL_IPRESOLVE_V6</c> to specify the IP resolution method.
        /// </summary>
        [ImplementsConstant("CURLOPT_IPRESOLVE")]
        CURLOPT_IPRESOLVE = 113,
        /// <summary>
        /// Pass a <c>string</c> as parameter. Set the kerberos4 security level;
        /// this also enables kerberos4 awareness. This is a string, 'clear', 'safe',
        /// 'confidential' or 'private'. If the string is set but doesn't match
        /// one of these, 'private' will be used. Set the string to <c>null</c>
        /// to disable kerberos4. The kerberos support only works for FTP.
        /// </summary>
        CURLOPT_KRB4LEVEL = 10063,
        /// <summary>
        /// Last numeric entry in the enumeration. Don't use this in your
        /// application code.
        /// </summary>
        [ImplementsConstant("CURLOPT_LASTENTRY")]
        CURLOPT_LASTENTRY = 135,
        /// <summary>
        /// Pass an <c>int</c> as parameter. It contains the transfer speed in bytes
        /// per second that the transfer should be below during
        /// <c>CURLOPT_LOW_SPEED_TIME</c> seconds for the library to consider it
        /// too slow and abort.
        /// </summary>
        [ImplementsConstant("CURLOPT_LOW_SPEED_LIMIT")]
        CURLOPT_LOW_SPEED_LIMIT = 19,
        /// <summary>
        /// Pass an <c>int</c> as parameter. It contains the time in seconds that
        /// the transfer should be below the <c>CURLOPT_LOW_SPEED_LIMIT</c> for the
        /// library to consider it too slow and abort.
        /// </summary>
        [ImplementsConstant("CURLOPT_LOW_SPEED_TIME")]
        CURLOPT_LOW_SPEED_TIME = 20,
        /// <summary>
        /// Pass an <c>int</c>. The set number will be the persistent connection
        /// cache size. The set amount will be the maximum amount of simultaneously
        /// open connections that libcurl may cache. Default is 5, and there isn't
        /// much point in changing this value unless you are perfectly aware of how
        /// this works and changes libcurl's behaviour. This concerns connections
        /// using any of the protocols that support persistent connections. 
        /// <para>
        /// When reaching the maximum limit, cURL uses the <c>CURLOPT_CLOSEPOLICY</c>
        /// to figure out which of the existing connections to close to prevent the
        /// number of open connections to increase. 
        /// </para>
        /// <note>
        /// if you already have performed transfers with this Easy object, setting a
        /// smaller <c>CURLOPT_MAXCONNECTS</c> than before may cause open connections
        /// to get closed unnecessarily.
        /// </note>
        /// </summary>
        [ImplementsConstant("CURLOPT_MAXCONNECTS")]
        CURLOPT_MAXCONNECTS = 71,
        /// <summary>
        /// Pass an <c>int</c> as parameter. This allows you to specify the maximum
        /// size (in bytes) of a file to download. If the file requested is larger
        /// than this value, the transfer will not start and
        /// <see cref="CURLcode.CURLE_FILESIZE_EXCEEDED"/> will be returned.
        /// <note>
        /// The file size is not always known prior to download, and for such files
        /// this option has no effect even if the file transfer ends up being larger
        /// than this given limit. This concerns both FTP and HTTP transfers. 
        /// </note> 
        /// </summary>
        [ImplementsConstant("CURLOPT_MAXFILESIZE")]
        CURLOPT_MAXFILESIZE = 114,
        /// <summary>
        /// Pass a <c>long</c> as parameter. This allows you to specify the
        /// maximum size (in bytes) of a file to download. If the file requested
        /// is larger than this value, the transfer will not start and
        /// <see cref="CURLcode.CURLE_FILESIZE_EXCEEDED"/> will be returned.
        /// (Added in 7.11.0) 
        /// <note>
        /// The file size is not always known prior to download, and for such files
        /// this option has no effect even if the file transfer ends up being larger
        /// than this given limit. This concerns both FTP and HTTP transfers. 
        /// </note>
        /// </summary>
        [ImplementsConstant("CURLOPT_MAXFILESIZE_LARGE")]
        CURLOPT_MAXFILESIZE_LARGE = 30117,
        /// <summary>
        /// Pass an <c>int</c>. The set number will be the redirection limit. If
        /// that many redirections have been followed, the next redirect will cause
        /// an error (<c>CURLE_TOO_MANY_REDIRECTS</c>). This option only makes sense
        /// if the <c>CURLOPT_FOLLOWLOCATION</c> is used at the same time.
        /// </summary>
        [ImplementsConstant("CURLOPT_MAXREDIRS")]
        CURLOPT_MAXREDIRS = 68,
        /// <summary>
        /// This parameter controls the preference of libcurl between using
        /// user names and passwords from your <c>~/.netrc</c> file, relative to
        /// user names and passwords in the URL supplied with <c>CURLOPT_URL</c>. 
        /// <note>
        /// libcurl uses a user name (and supplied or prompted password)
        /// supplied with <c>CURLOPT_USERPWD</c> in preference to any of the
        /// options controlled by this parameter.
        /// </note>
        /// <para>
        /// Pass a <c>CURL_NETRC_IGNORED</c>, <c>CURL_NETRC_OPTIONAL</c> or <c>CURL_NETRC_REQUIRED</c>
        /// </para>
        /// <para>
        /// Only machine name, user name and password are taken into account
        /// (init macros and similar things aren't supported).
        /// </para>
        /// <note>
        /// libcurl does not verify that the file has the correct properties
        /// set (as the standard Unix ftp client does). It should only be
        /// readable by user.
        /// </note>
        /// </summary>
        [ImplementsConstant("CURLOPT_NETRC")]
        CURLOPT_NETRC = 51,
        /// <summary>
        /// Pass a <c>string</c> as parameter, containing the full path name to the
        /// file you want libcurl to use as .netrc file. If this option is omitted,
        /// and <c>CURLOPT_NETRC</c> is set, libcurl will attempt to find the a
        /// .netrc file in the current user's home directory. (Added in 7.10.9) 
        /// </summary>
        [ImplementsConstant("CURLOPT_NETRC_FILE")]
        CURLOPT_NETRC_FILE = 10118,
        /// <summary>
        /// A <c>true</c> parameter tells the library to not include the
        /// body-part in the output. This is only relevant for protocols that
        /// have separate header and body parts. On HTTP(S) servers, this
        /// will make libcurl do a HEAD request. 
        /// <para>
        /// To change back to GET, you should use <c>CURLOPT_HTTPGET</c>. To
        /// change back to POST, you should use <c>CURLOPT_POST</c>. Setting
        /// <c>CURLOPT_NOBODY</c> to <c>false</c> has no effect.
        /// </para>
        /// </summary>
        [ImplementsConstant("CURLOPT_NOBODY")]
        CURLOPT_NOBODY = 44,
        /// <summary>
        /// A <c>true</c> parameter tells the library to shut off progress
        /// reporting. This is default in PHP.
        /// </summary>
        [ImplementsConstant("CURLOPT_NOPROGRESS")]
        CURLOPT_NOPROGRESS = 43,
        /// <summary>
        /// Pass a <c>bool</c>. If it is <c>true</c>, libcurl will not use any
        /// functions that install signal handlers or any functions that cause
        /// signals to be sent to the process. This option is mainly here to allow
        /// multi-threaded unix applications to still set/use all timeout options
        /// etc, without risking getting signals. (Added in 7.10)
        /// <para>
        /// Consider using libcurl with ares built-in to enable asynchronous DNS
        /// lookups. It enables nice timeouts for name resolves without signals.
        /// </para> 
        /// </summary>
        [ImplementsConstant("CURLOPT_NOSIGNAL")]
        CURLOPT_NOSIGNAL = 99,
        /// <summary>
        /// Not supported.
        /// </summary>
        [ImplementsConstant("CURLOPT_PASV_HOST")]
        CURLOPT_PASV_HOST = 126,
        /// <summary>
        /// Pass an <c>int</c> specifying what remote port number to connect to,
        /// instead of the one specified in the URL or the default port for the
        /// used protocol. 
        /// </summary>
        [ImplementsConstant("CURLOPT_PORT")]
        CURLOPT_PORT = 3,
        /// <summary>
        /// A <c>true</c> parameter tells the library to do a regular HTTP post.
        /// This will also make the library use the a "Content-Type:
        /// application/x-www-form-urlencoded" header. (This is by far the most
        /// commonly used POST method).
        /// <para>
        /// Use the <c>CURLOPT_POSTFIELDS</c> option to specify what data to post
        /// and <c>CURLOPT_POSTFIELDSIZE</c> to set the data size. Optionally,
        /// you can provide data to POST using the <c>CURLOPT_READFUNCTION</c> and
        /// <c>CURLOPT_READDATA</c> options.
        /// </para>
        /// <para>
        /// You can override the default POST Content-Type: header by setting
        /// your own with <c>CURLOPT_HTTPHEADER</c>. 
        /// </para>
        /// <para>
        /// Using POST with HTTP 1.1 implies the use of a "Expect: 100-continue"
        /// header. You can disable this header with <c>CURLOPT_HTTPHEADER</c> as usual.
        /// </para> 
        /// <para>
        /// If you use POST to a HTTP 1.1 server, you can send data without knowing
        /// the size before starting the POST if you use chunked encoding. You
        /// enable this by adding a header like "Transfer-Encoding: chunked" with
        /// <c>CURLOPT_HTTPHEADER</c>. With HTTP 1.0 or without chunked transfer,
        /// you must specify the size in the request. 
        /// </para>
        /// <note>
        /// if you have issued a POST request and want to make a HEAD or GET instead,
        /// you must explictly pick the new request type using <c>CURLOPT_NOBODY</c>
        /// or <c>CURLOPT_HTTPGET</c> or similar. 
        /// </note>
        /// </summary>
        [ImplementsConstant("CURLOPT_POST")]
        CURLOPT_POST = 47,
        /// <summary>
        /// Pass a <c>string</c> as parameter, which should be the full data to post
        /// in an HTTP POST operation. You must make sure that the data is formatted
        /// the way you want the server to receive it. libcurl will not convert or
        /// encode it for you. Most web servers will assume this data to be
        /// url-encoded. Take note. 
        /// <para>
        /// This POST is a normal application/x-www-form-urlencoded kind (and
        /// libcurl will set that Content-Type by default when this option is used),
        /// which is the most commonly used one by HTML forms. See also the
        /// <c>CURLOPT_POST</c>. Using <c>CURLOPT_POSTFIELDS</c> implies
        /// <c>CURLOPT_POST</c>. 
        /// </para>
        /// <para>
        /// Using POST with HTTP 1.1 implies the use of a "Expect: 100-continue"
        /// header. You can disable this header with <c>CURLOPT_HTTPHEADER</c> as usual. 
        /// </para>
        /// <note>
        /// to make multipart/formdata posts (aka rfc1867-posts), check out the
        /// <c>CURLOPT_HTTPPOST</c> option.
        /// </note>
        /// </summary>
        [ImplementsConstant("CURLOPT_POSTFIELDS")]
        CURLOPT_POSTFIELDS = 10015,
        /// <summary>
        /// If you want to post data to the server without letting libcurl do a
        /// <c>strlen()</c> to measure the data size, this option must be used. When
        /// this option is used you can post fully binary data, which otherwise
        /// is likely to fail. If this size is set to zero, the library will use
        /// <c>strlen()</c> to get the size.
        /// </summary>
        [ImplementsConstant("CURLOPT_POSTFIELDSIZE")]
        CURLOPT_POSTFIELDSIZE = 60,
        /// <summary>
        /// Pass a <c>long</c> as parameter. Use this to set the size of the
        /// <c>CURLOPT_POSTFIELDS</c> data to prevent libcurl from doing
        /// <c>strlen()</c> on the data to figure out the size. This is the large
        /// file version of the <c>CURLOPT_POSTFIELDSIZE</c> option. (Added in 7.11.1) 
        /// </summary>
        [ImplementsConstant("CURLOPT_POSTFIELDSIZE_LARGE")]
        CURLOPT_POSTFIELDSIZE_LARGE = 30120,
        /// <summary>
        /// Pass an array of FTP commands to pass to the server after
        /// your ftp transfer request. 
        /// </summary>
        [ImplementsConstant("CURLOPT_POSTQUOTE")]
        CURLOPT_POSTQUOTE = 10039,

        /// <summary>
        /// Pass an <c>object</c> as parameter. The object can
        /// subsequently be retrieved using curl_getinfo with the
        /// <c>CURLINFO_PRIVATE</c> option. libcurl itself does
        /// nothing with this data. (Added in 7.10.3) 
        /// </summary>
        [ImplementsConstant("CURLOPT_PRIVATE")]
        CURLOPT_PRIVATE = 10103,
        /// <summary>
        /// Pass an <c>object</c> reference that will be untouched by libcurl
        /// and passed as the first argument in the progress delegate set with
        /// <c>CURLOPT_PROGRESSFUNCTION</c>.
        /// </summary>
        [ImplementsConstant("CURLOPT_PROGRESSDATA")]
        CURLOPT_PROGRESSDATA = 10057,
        /// <summary>
        /// Pass an callback function where the callback function takes three parameters. 
        /// The first is the cURL resource, the second is a file-descriptor resource, 
        /// and the third is length. Return the string containing the data. 
        /// This function gets called by libcurl at a frequent interval during data
        /// transfer. 
        /// </summary>
        // This probably doesnt work in PHP: Returning a
        // non-zero value from this delegate will cause libcurl to abort the
        // transfer and return <c>CURLE_ABORTED_BY_CALLBACK"</c>.
        [ImplementsConstant("CURLOPT_PROGRESSFUNCTION")]
        CURLOPT_PROGRESSFUNCTION = 20056,


        /// <summary>
        /// Set HTTP proxy to use. The parameter should be a <c>string</c> holding
        /// the host name or dotted IP address. To specify port number in this
        /// string, append <c>:[port]</c> to the end of the host name. The proxy
        /// string may be prefixed with <c>[protocol]://</c> since any such prefix
        /// will be ignored. The proxy's port number may optionally be specified
        /// with the separate option <c>CURLOPT_PROXYPORT</c>. 
        /// <para>
        /// NOTE: when you tell the library to use an HTTP proxy, libcurl will
        /// transparently convert operations to HTTP even if you specify an FTP
        /// URL etc. This may have an impact on what other features of the library
        /// you can use, such as <c>CURLOPT_QUOTE</c> and similar FTP specifics
        /// that don't work unless you tunnel through the HTTP proxy. Such tunneling
        /// is activated with <c>CURLOPT_HTTPPROXYTUNNEL</c>. 
        /// </para>
        /// </summary>
        [ImplementsConstant("CURLOPT_PROXY")]
        CURLOPT_PROXY = 10004,
        /// <summary>
        /// Pass a bitmask of <see cref="CURLhttpAuth"/> as the paramter, to tell
        /// libcurl what authentication method(s) you want it to use for your proxy
        /// authentication. If more than one bit is set, libcurl will first query the
        /// site to see what authentication methods it supports and then pick the best
        /// one you allow it to use. Note that for some methods, this will induce an
        /// extra network round-trip. Set the actual name and password with the
        /// <c>CURLOPT_PROXYUSERPWD</c> option. The bitmask can be constructed by
        /// or'ing together the <see cref="CURLhttpAuth"/> bits. As of this writing,
        /// only <see cref="CURLhttpAuth.CURLAUTH_BASIC"/> and
        /// <see cref="CURLhttpAuth.CURLAUTH_NTLM"/> work. (Added in 7.10.7) 
        /// </summary>
        [ImplementsConstant("CURLOPT_PROXYAUTH")]
        CURLOPT_PROXYAUTH = 111,
        /// <summary>
        /// Pass an <c>int</c> with this option to set the proxy port to connect
        /// to unless it is specified in the proxy string <c>CURLOPT_PROXY</c>.
        /// </summary>
        [ImplementsConstant("CURLOPT_PROXYPORT")]
        CURLOPT_PROXYPORT = 59,
        /// <summary>
        /// Pass a <see cref="CURLproxyType"/> to set type of the proxy.
        /// </summary>
        [ImplementsConstant("CURLOPT_PROXYTYPE")]
        CURLOPT_PROXYTYPE = 101,
        /// <summary>
        /// Pass a <c>string</c> as parameter, which should be
        /// <c>[user name]:[password]</c> to use for the connection to the
        /// HTTP proxy. Use <c>CURLOPT_PROXYAUTH</c> to decide authentication method. 
        /// </summary>
        [ImplementsConstant("CURLOPT_PROXYUSERPWD")]
        CURLOPT_PROXYUSERPWD = 10006,

        /// <summary>
        /// Pass a <c>string</c> as parameter, which should be
        /// username to use for the connection to the
        /// HTTP proxy. Use <c>CURLOPT_PROXYAUTH</c> to decide authentication method. 
        /// </summary>
        [ImplementsConstant("CURLOPT_PROXYUSERNAME")]
        CURLOPT_PROXYUSERNAME = 10000 + 175,

        /// <summary>
        /// Pass a <c>string</c> as parameter, which should be
        /// password to use for the connection to the
        /// HTTP proxy. Use <c>CURLOPT_PROXYAUTH</c> to decide authentication method. 
        /// </summary>
        [ImplementsConstant("CURLOPT_PROXYPASSWORD")]
        CURLOPT_PROXYPASSWORD = 10000 + 176,


        /// <summary>
        /// A <c>true</c> parameter tells the library to use HTTP PUT to transfer
        /// data. The data should be set with <c>CURLOPT_READDATA</c> and
        /// <c>CURLOPT_INFILESIZE</c>. 
        /// <para>
        /// This option is deprecated and starting with version 7.12.1 you should
        /// instead use <c>CURLOPT_UPLOAD</c>. 
        /// </para>
        /// </summary>
        [ImplementsConstant("CURLOPT_PUT")]
        CURLOPT_PUT = 54,
        /// <summary>
        /// Pass a reference to an array containing FTP commands to
        /// pass to the server prior to your ftp request. This will be done before
        /// any other FTP commands are issued (even before the CWD command).
        /// </summary>
        [ImplementsConstant("CURLOPT_QUOTE")]
        CURLOPT_QUOTE = 10028,
        /// <summary>
        /// Pass a <c>string</c> containing the file name. The file will be used
        /// to read from to seed the random engine for SSL. The more random the
        /// specified file is, the more secure the SSL connection will become.
        /// </summary>
        [ImplementsConstant("CURLOPT_RANDOM_FILE")]
        CURLOPT_RANDOM_FILE = 10076,
        /// <summary>
        /// Pass a <c>string</c> as parameter, which should contain the
        /// specified range you want. It should be in the format <c>X-Y</c>, where X
        /// or Y may be left out. HTTP transfers also support several intervals,
        /// separated with commas as in <c>X-Y,N-M</c>. Using this kind of multiple
        /// intervals will cause the HTTP server to send the response document
        /// in pieces (using standard MIME separation techniques). Pass a
        /// <c>null</c> to this option to disable the use of ranges. 
        /// </summary>
        [ImplementsConstant("CURLOPT_RANGE")]
        CURLOPT_RANGE = 10007,

        /// <summary>
        /// The file that the transfer should be read from when uploading. 
        /// </summary>
        [ImplementsConstant("CURLOPT_READDATA")]
        CURLOPT_READDATA = 10009,

        /// <summary>
        /// The file that the transfer should be read from when uploading. 
        /// </summary>
        [ImplementsConstant("CURLOPT_INFILE")]
        CURLOPT_INFILE = CURLOPT_READDATA,
       

        ///<summary> 
        /// Pass callback function where the callback function takes three parameters.
        /// The first is the cURL resource, the second is a stream resource provided to cURL
        /// through the option <c>CURLOPT_INFILE</c>, and the third is the maximum amount of data 
        /// to be read. The callback function must return a string with a length equal or 
        /// smaller than the amount of data requested, typically by reading it from the passed
        /// stream resource. Returning 0 will signal end-of-file  and cause it to stop 
        /// the current transfer. 
        /// </summary>
        /// <remarks>
        /// If you stop the current transfer by returning 0 "pre-maturely"
        /// (i.e before the server expected it, like when you've told you will
        /// upload N bytes and you upload less than N bytes), you may experience that
        /// the server "hangs" waiting for the rest of the data that won't come. 
        /// </remarks>
        [ImplementsConstant("CURLOPT_READFUNCTION")]
        CURLOPT_READFUNCTION = 20012,
        /// <summary>
        /// Pass a <c>string</c> as parameter. It will be used to set the Referer:
        /// header in the http request sent to the remote server. This can be used
        /// to fool servers or scripts. You can also set any custom header with
        /// <c>CURLOPT_HTTPHEADER</c>. 
        /// </summary>
        [ImplementsConstant("CURLOPT_REFERER")]
        CURLOPT_REFERER = 10016,
        /// <summary>
        /// Pass an <c>int</c> as parameter. It contains the offset in number of
        /// bytes that you want the transfer to start from. Set this option to 0
        /// to make the transfer start from the beginning (effectively disabling resume). 
        /// </summary>
        [ImplementsConstant("CURLOPT_RESUME_FROM")]
        CURLOPT_RESUME_FROM = 21,
        /// <summary>
        /// Pass a <c>long</c> as parameter. It contains the offset in number of
        /// bytes that you want the transfer to start from. (Added in 7.11.0) 
        /// </summary>
        [ImplementsConstant("CURLOPT_RESUME_FROM_LARGE")]
        CURLOPT_RESUME_FROM_LARGE = 30116,

        /// <summary>
        /// Pass a <c>bool</c> as parameter. Setting this option will make return 
        /// the transfer as a string of the return value of curl_exec() instead of
        /// outputting it out directly. 
        /// </summary>
        [ImplementsConstant("CURLOPT_RETURNTRANSFER")]
        CURLOPT_RETURNTRANSFER = 19913,


        /// <summary>
        /// Pass a <c>string</c> as parameter. The string should be the file name
        /// of your certificate. The default format is "PEM" and can be changed
        /// with <c>CURLOPT_SSLCERTTYPE</c>.
        /// </summary>
        [ImplementsConstant("CURLOPT_SSLCERT")]
        CURLOPT_SSLCERT = 10025,
        /// <summary>
        /// Pass a <c>string</c> as parameter. It will be used as the password
        /// required to use the <c>CURLOPT_SSLCERT</c> certificate. 
        /// <para>
        /// This option is replaced by <c>CURLOPT_SSLKEYPASSWD</c> and should only
        /// be used for backward compatibility. You never needed a pass phrase to
        /// load a certificate but you need one to load your private key.
        /// </para>
        /// </summary>
        [ImplementsConstant("CURLOPT_SSLCERTPASSWD")]
        CURLOPT_SSLCERTPASSWD = CURLOPT_SSLKEYPASSWD,
        /// <summary>
        /// Pass a <c>string</c> as parameter. The string should be the format of
        /// your certificate. Supported formats are "PEM" and "DER". (Added in 7.9.3) 
        /// </summary>
        [ImplementsConstant("CURLOPT_SSLCERTTYPE")]
        CURLOPT_SSLCERTTYPE = 10086,
        /// <summary>
        /// Pass a <c>string</c> as parameter. It will be used as the identifier
        /// for the crypto engine you want to use for your private key.
        /// <note>
        /// If the crypto device cannot be loaded, 
        /// <see cref="CURLcode.CURLE_SSL_ENGINE_NOTFOUND"/> is returned.
        /// </note>
        /// </summary>
        [ImplementsConstant("CURLOPT_SSLENGINE")]
        CURLOPT_SSLENGINE = 10089,
        /// <summary>
        /// Sets the actual crypto engine as the default for (asymmetric)
        /// crypto operations.
        /// <note>
        /// If the crypto device cannot be set,
        /// <see cref="CURLcode.CURLE_SSL_ENGINE_SETFAILED"/> is returned. 
        /// </note>
        /// </summary>
        [ImplementsConstant("CURLOPT_SSLENGINE_DEFAULT")]
        CURLOPT_SSLENGINE_DEFAULT = 90,
        /// <summary>
        /// Pass a <c>string</c> as parameter. The string should be the file name
        /// of your private key. The default format is "PEM" and can be changed
        /// with <c>CURLOPT_SSLKEYTYPE</c>. 
        /// </summary>
        [ImplementsConstant("CURLOPT_SSLKEY")]
        CURLOPT_SSLKEY = 10087,
        /// <summary>
        /// Pass a <c>string</c> as parameter. It will be used as the password
        /// required to use the <c>CURLOPT_SSLKEY</c> private key.
        /// </summary>
        [ImplementsConstant("CURLOPT_SSLKEYPASSWD")]
        CURLOPT_SSLKEYPASSWD = 10026,
        /// <summary>
        /// Pass a <c>string</c> as parameter. The string should be the format of
        /// your private key. Supported formats are "PEM", "DER" and "ENG". 
        /// <note>
        /// The format "ENG" enables you to load the private key from a crypto
        /// engine. In this case <c>CURLOPT_SSLKEY</c> is used as an identifier
        /// passed to the engine. You have to set the crypto engine with
        /// <c>CURLOPT_SSLENGINE</c>. "DER" format key file currently does not
        /// work because of a bug in OpenSSL. 
        /// </note>
        /// </summary>
        [ImplementsConstant("CURLOPT_SSLKEYTYPE")]
        CURLOPT_SSLKEYTYPE = 10088,
        /// <summary>
        /// Set the SSL version to be used. By default the SSL library will try to solve
        /// this by itself although some servers make this difficult, so it has to be set 
        /// manually.
        /// </summary>
        [ImplementsConstant("CURLOPT_SSLVERSION")]
        CURLOPT_SSLVERSION = 32,
        /// <summary>
        /// Pass a <c>string</c> holding the list of ciphers to use for the SSL
        /// connection. The list must be syntactically correct, it consists of
        /// one or more cipher strings separated by colons. Commas or spaces are
        /// also acceptable separators but colons are normally used, !, - and +
        /// can be used as operators. Valid examples of cipher lists include
        /// 'RC4-SHA', ´SHA1+DES´, 'TLSv1' and 'DEFAULT'. The default list is
        /// normally set when you compile OpenSSL.
        /// <para>
        /// You'll find more details about cipher lists on this URL:
        /// http://www.openssl.org/docs/apps/ciphers.html 
        /// </para>
        /// </summary>
        [ImplementsConstant("CURLOPT_SSL_CIPHER_LIST")]
        CURLOPT_SSL_CIPHER_LIST = 10083,

        /// <summary>
        /// Pass an <c>int</c>. Set if we should verify the common name from the
        /// peer certificate in the SSL handshake, set 1 to check existence, 2 to
        /// ensure that it matches the provided hostname. This is by default set
        /// to 2. (default changed in 7.10) 
        /// </summary>
        [ImplementsConstant("CURLOPT_SSL_VERIFYHOST")]
        CURLOPT_SSL_VERIFYHOST = 81,
        /// <summary>
        /// Pass a <c>bool</c> that is set to <c>false</c> to stop curl from
        /// verifying the peer's certificate (7.10 starting setting this option
        /// to non-zero by default). Alternate certificates to verify against
        /// can be specified with the <c>CURLOPT_CAINFO</c> option or a
        /// certificate directory can be specified with the <c>CURLOPT_CAPATH</c>
        /// option. As of 7.10, curl installs a default bundle.
        /// <c>CURLOPT_SSL_VERIFYHOST</c> may also need to be set to 1
        /// or 0 if <c>CURLOPT_SSL_VERIFYPEER</c> is disabled (it defaults to 2). 
        /// </summary>
        [ImplementsConstant("CURLOPT_SSL_VERIFYPEER")]
        CURLOPT_SSL_VERIFYPEER = 64,
        /// <summary>
        /// An alternative location to output errors to instead of STDERR. 
        /// </summary>
        [ImplementsConstant("CURLOPT_STDERR")]
        CURLOPT_STDERR = 10037,
        /// <summary>
        /// Pass a <c>bool</c> specifying whether the TCP_NODELAY option should be
        /// set or cleared (<c>true</c> = set, <c>false</c> = clear). The option is
        /// cleared by default. This will have no effect after the connection has
        /// been established.
        /// <para>
        /// Setting this option will disable TCP's Nagle algorithm. The purpose of
        /// this algorithm is to try to minimize the number of small packets on the
        /// network (where "small packets" means TCP segments less than the Maximum
        /// Segment Size (MSS) for the network). 
        /// </para>
        /// <para>
        /// Maximizing the amount of data sent per TCP segment is good because it
        /// amortizes the overhead of the send. However, in some cases (most notably
        /// telnet or rlogin) small segments may need to be sent without delay. This
        /// is less efficient than sending larger amounts of data at a time, and can
        /// contribute to congestion on the network if overdone. 
        /// </para> 
        /// </summary>
        [ImplementsConstant("CURLOPT_TCP_NODELAY")]
        CURLOPT_TCP_NODELAY = 121,
        /// <summary>
        /// Provide an list with variables to pass to the telnet
        /// negotiations. The variables should be in the format "option=value".
        /// libcurl supports the options 'TTYPE', 'XDISPLOC' and 'NEW_ENV'. See
        /// the TELNET standard for details. 
        /// </summary>
        [ImplementsConstant("CURLOPT_TELNETOPTIONS")]
        CURLOPT_TELNETOPTIONS = 10070,

        // Pass a member of the <c>CURLtimeCond</c> enumeration as a paramter.
        /// <summary>
        /// This defines how the <c>CURLOPT_TIMEVALUE</c> time
        /// value is treated. This feature applies to HTTP and FTP. 
        /// Use <c>CURL_TIMECOND_IFMODSINCE</c> to return the page only if 
        /// it has been modified since the time specified in CURLOPT_TIMEVALUE.
        /// If it hasn't been modified, a "304 Not Modified" header will be returned
        /// assuming <c>CURLOPT_HEADER</c> is TRUE. 
        /// Use <c>CURL_TIMECOND_IFUNMODSINCE</c> for the reverse effect.
        /// <c>CURL_TIMECOND_IFMODSINCE</c> is the default. 
        /// </summary>
        /// <remarks>
        /// The last modification time of a file is not always known and in such
        /// instances this feature will have no effect even if the given time
        /// condition would have not been met.
        /// </remarks>
        [ImplementsConstant("CURLOPT_TIMECONDITION")]
        CURLOPT_TIMECONDITION = 33,
        /// <summary>
        /// Pass a <c>int</c> as parameter containing the maximum time in seconds
        /// that you allow the libcurl transfer operation to take. Normally, name
        /// lookups can take a considerable time and limiting operations to less
        /// than a few minutes risk aborting perfectly normal operations. This
        /// option will cause curl to use the SIGALRM to enable time-outing
        /// system calls. 
        /// <note>
        /// this is not recommended to use in unix multi-threaded programs,
        /// as it uses signals unless <c>CURLOPT_NOSIGNAL</c> (see above) is set.
        /// </note>
        /// </summary>
        [ImplementsConstant("CURLOPT_TIMEOUT")]
        CURLOPT_TIMEOUT = 13,
        /// <summary>
        /// Pass a <see cref="System.DateTime"/> as parameter. This time will be
        /// used in a condition as specified with <c>CURLOPT_TIMECONDITION</c>. 
        /// </summary>
        [ImplementsConstant("CURLOPT_TIMEVALUE")]
        CURLOPT_TIMEVALUE = 34,
        /// <summary>
        /// A <c>true</c> parameter tells the library to use ASCII mode for ftp
        /// transfers, instead of the default binary transfer. For LDAP transfers
        /// it gets the data in plain text instead of HTML and for win32 systems
        /// it does not set the stdout to binary mode. This option can be usable
        /// when transferring text data between systems with different views on
        /// certain characters, such as newlines or similar.
        /// </summary>
        [ImplementsConstant("CURLOPT_TRANSFERTEXT")]
        CURLOPT_TRANSFERTEXT = 53,
        /// <summary>
        /// A <c>true</c> parameter tells the library it can continue to send
        /// authentication (user+password) when following locations, even when
        /// hostname changed. Note that this is meaningful only when setting
        /// <c>CURLOPT_FOLLOWLOCATION</c>.
        /// </summary>
        [ImplementsConstant("CURLOPT_UNRESTRICTED_AUTH")]
        CURLOPT_UNRESTRICTED_AUTH = 105,
        /// <summary>
        /// A <c>true</c> parameter tells the library to prepare for an
        /// upload. The <c>CURLOPT_READDATA</c> and <c>CURLOPT_INFILESIZE</c>
        /// or <c>CURLOPT_INFILESIZE_LARGE</c> are also interesting for uploads.
        /// If the protocol is HTTP, uploading means using the PUT request
        /// unless you tell libcurl otherwise. 
        /// <para>
        /// Using PUT with HTTP 1.1 implies the use of a "Expect: 100-continue"
        /// header. You can disable this header with <c>CURLOPT_HTTPHEADER</c> as usual. 
        /// </para>
        /// <para>
        /// If you use PUT to a HTTP 1.1 server, you can upload data without
        /// knowing the size before starting the transfer if you use chunked
        /// encoding. You enable this by adding a header like
        /// "Transfer-Encoding: chunked" with <c>CURLOPT_HTTPHEADER</c>. With
        /// HTTP 1.0 or without chunked transfer, you must specify the size.
        /// </para>
        /// </summary>
        [ImplementsConstant("CURLOPT_UPLOAD")]
        CURLOPT_UPLOAD = 46,
        /// <summary>
        /// The actual URL to deal with. The parameter should be a <c>string</c>.
        /// If the given URL lacks the protocol part ("http://" or "ftp://" etc), it
        /// will attempt to guess which protocol to use based on the given host name.
        /// <para>If the given protocol of the set URL is not supported, libcurl will return
        /// an error <c>CURLcode.</c>(<see cref="CURLcode.CURLE_UNSUPPORTED_PROTOCOL"/>)
        /// when you call Easy's <see cref="Curl.Execute"/>.</para>
        /// <para>Use <see cref="Curl.Version()"/> for detailed info
        /// on which protocols that are supported.</para>
        /// </summary>
        [ImplementsConstant("CURLOPT_URL")]
        CURLOPT_URL = 10002,
        /// <summary>
        /// Pass a <c>string</c> as parameter. It will be used to set the
        /// User-Agent: header in the http request sent to the remote server.
        /// This can be used to fool servers or scripts. You can also set any
        /// custom header with <c>CURLOPT_HTTPHEADER</c>.
        /// </summary>
        [ImplementsConstant("CURLOPT_USERAGENT")]
        CURLOPT_USERAGENT = 10018,
        /// <summary>
        /// Pass a <c>string</c> as parameter, which should be
        /// <c>[user name]:[password]</c> to use for the connection. Use
        /// <c>CURLOPT_HTTPAUTH</c> to decide authentication method. 
        /// <para>
        /// When using HTTP and <c>CURLOPT_FOLLOWLOCATION</c>, libcurl might
        /// perform several requests to possibly different hosts. libcurl will
        /// only send this user and password information to hosts using the
        /// initial host name (unless <c>CURLOPT_UNRESTRICTED_AUTH</c> is set),
        /// so if libcurl follows locations to other hosts it will not send the
        /// user and password to those. This is enforced to prevent accidental
        /// information leakage. 
        /// </para>
        /// </summary>
        [ImplementsConstant("CURLOPT_USERPWD")]
        CURLOPT_USERPWD = 10005,


        /// <summary>
        /// Username to be used for user to authenticate. Use
        /// <c>CURLOPT_HTTPAUTH</c> to decide authentication method. 
        /// </summary>
        [ImplementsConstant("CURLOPT_USERNAME")]
        CURLOPT_USERNAME = 10000 + 173,


        /// <summary>
        /// Password to be used for user to authenticate. Use
        /// <c>CURLOPT_HTTPAUTH</c> to decide authentication method. 
        /// </summary>
        [ImplementsConstant("CURLOPT_PASSWORD")]
        CURLOPT_PASSWORD = 10000 + 174,
        /// <summary>
        /// Set the parameter to <c>true</c> to get the library to display a lot
        /// of verbose information about its operations. Very useful for libcurl
        /// and/or protocol debugging and understanding. You hardly ever want this set in production use, you will
        /// almost always want this when you debug/report problems. 
        /// </summary>
        [ImplementsConstant("CURLOPT_VERBOSE")]
        CURLOPT_VERBOSE = 41,

        /// <summary>
        /// The name of a callback function where the callback function takes two parameters. 
        /// The first is the cURL resource, and the second is a string with the data to be written. 
        /// The data must be saved by using this callback function. 
        /// It must return the exact number of bytes written or the transfer will be aborted with an error. 
        /// </summary>
        /// <remarks>
        /// Pass a reference to an WriteFunction delegate.
        /// The delegate gets called by libcurl as soon as there is data received
        /// that needs to be saved. The size of the data referenced by <c>buf</c>
        /// is <c>size</c> multiplied with <c>nmemb</c>, it will not be zero
        /// terminated. Return the number of bytes actually taken care of. If
        /// that amount differs from the amount passed to your function, it'll
        /// signal an error to the library and it will abort the transfer and
        /// return <c>CURLcode.</c><see cref="CURLcode.CURLE_WRITE_ERROR"/>. 
        /// <note>This function may be called with zero bytes data if the
        /// transfered file is empty.</note>
        /// </remarks>
        [ImplementsConstant("CURLOPT_WRITEFUNCTION")]
        CURLOPT_WRITEFUNCTION = 20011,


        /// <summary>
        /// Equivalent to CURLOPT_WRITEHEADER
        /// </summary>
        //[ImplementsConstant("CURLOPT_HEADERDATA")]        //Not used in PHP
        CURLOPT_HEADERDATA = 10029,


        /// <summary>
        /// The file that the header part of the transfer is written to. 
        /// </summary>
        [ImplementsConstant("CURLOPT_WRITEHEADER")]
        CURLOPT_WRITEHEADER = CURLOPT_HEADERDATA,
        
        /// <summary>
        /// Pass a <c>string</c> of the output using full variable-replacement
        /// as described elsewhere.
        /// </summary>
        [ImplementsConstant("CURLOPT_WRITEINFO")]
        CURLOPT_WRITEINFO = 10040,

        #region Unsupported in PHP

        ///// <summary>
        ///// Object reference to pass to the <see cref="Easy.WriteFunction"/>
        ///// delegate. Note that if you specify the <c>CURLOPT_WRITEFUNCTION</c>,
        ///// this is the object you'll get as input. 
        ///// </summary>
        //[ImplementsConstant("CURLOPT_WRITEDATA")]
        //CURLOPT_WRITEDATA = 10001,

        ///// <summary>
        ///// Object reference to pass to the ssl context delegate set by the option
        ///// <c>CURLOPT_SSL_CTX_FUNCTION</c>, this is the pointer you'll get as the
        ///// second parameter, otherwise <c>null</c>. (Added in 7.11.0) 
        ///// </summary>
        //[ImplementsConstant("CURLOPT_SSL_CTX_DATA")]
        //CURLOPT_SSL_CTX_DATA = 10109,
        ///// <summary>
        ///// Reference to an <see cref="Easy.SSLContextFunction"/> delegate.
        ///// This delegate gets called by libcurl just before the initialization of
        ///// an SSL connection after having processed all other SSL related options
        ///// to give a last chance to an application to modify the behaviour of
        ///// openssl's ssl initialization. The <see cref="SSLContext"/> parameter
        ///// wraps a pointer to an openssl SSL_CTX. If an error is returned no attempt
        ///// to establish a connection is made and the perform operation will return
        ///// the error code from this callback function. Set the parm argument with
        ///// the <c>CURLOPT_SSL_CTX_DATA</c> option. This option was introduced
        ///// in 7.11.0.
        ///// <note>
        ///// To use this properly, a non-trivial amount of knowledge of the openssl
        ///// libraries is necessary. Using this function allows for example to use
        ///// openssl callbacks to add additional validation code for certificates,
        ///// and even to change the actual URI of an HTTPS request.
        ///// </note>
        ///// </summary>
        //[ImplementsConstant("CURLOPT_SSL_CTX_FUNCTION")]
        //CURLOPT_SSL_CTX_FUNCTION = 20108,

        ///// <summary>
        ///// Pass an initialized <see cref="Share"/> reference as a parameter.
        ///// Setting this option will make this <see cref="Easy"/> object use the
        ///// data from the Share object instead of keeping the data to itself. This
        ///// enables several Easy objects to share data. If the Easy objects are used
        ///// simultaneously, you MUST use the Share object's locking methods.
        ///// See <see cref="Share.SetOpt"/> for details.
        ///// </summary>
        //[ImplementsConstant("CURLOPT_SHARE")]
        //CURLOPT_SHARE = 10100,
        ///// <summary>
        ///// Not supported.
        ///// </summary>
        //[ImplementsConstant("CURLOPT_SOURCE_HOST")]
        //CURLOPT_SOURCE_HOST = 10122,
        ///// <summary>
        ///// Not supported.
        ///// </summary>
        //[ImplementsConstant("CURLOPT_SOURCE_PATH")]
        //CURLOPT_SOURCE_PATH = 10124,
        ///// <summary>
        ///// Not supported.
        ///// </summary>
        //[ImplementsConstant("CURLOPT_SOURCE_PORT")]
        //CURLOPT_SOURCE_PORT = 125,
        ///// <summary>
        ///// When doing a third-party transfer, set the source post-quote list,
        ///// as an <see cref="Slist"/>.
        ///// </summary>
        //[ImplementsConstant("CURLOPT_SOURCE_POSTQUOTE")]
        //CURLOPT_SOURCE_POSTQUOTE = 10128,
        ///// <summary>
        ///// When doing a third-party transfer, set the source pre-quote list,
        ///// as an <see cref="Slist"/>.
        ///// </summary>
        //[ImplementsConstant("CURLOPT_SOURCE_PREQUOTE")]
        //CURLOPT_SOURCE_PREQUOTE = 10127,
        ///// <summary>
        ///// When doing a third-party transfer, set a quote list,
        ///// as an <see cref="Slist"/>.
        ///// </summary>
        //[ImplementsConstant("CURLOPT_SOURCE_QUOTE")]
        //CURLOPT_SOURCE_QUOTE = 10133,
        ///// <summary>
        ///// Set the source URL for a third-party transfer.
        ///// </summary>
        //[ImplementsConstant("CURLOPT_SOURCE_URL")]
        //CURLOPT_SOURCE_URL = 10132,
        ///// <summary>
        ///// When doing 3rd party transfer, set the source user and password, as
        ///// a <c>string</c> with format <c>user:password</c>.
        ///// </summary>
        //[ImplementsConstant("CURLOPT_SOURCE_USERPWD")]
        //CURLOPT_SOURCE_USERPWD = 10123,

        ///// <summary>
        ///// Pass an <see cref="Slist"/> containing the FTP commands to pass to
        ///// the server after the transfer type is set. Disable this operation
        ///// again by setting a <c>null</c> to this option.
        ///// </summary>
        //[ImplementsConstant("CURLOPT_PREQUOTE")]
        //CURLOPT_PREQUOTE = 10093,

        ///// <summary>
        ///// Provide an <see cref="Easy.IoctlFunction"/> delegate reference.
        ///// This delegate gets called by libcurl when an IOCTL operation,
        ///// such as a rewind of a file being sent via FTP, is required on
        ///// the client side.
        ///// </summary>
        //[ImplementsConstant("CURLOPT_IOCTLFUNCTION")]
        //CURLOPT_IOCTLFUNCTION = 20130,

        /// <summary>
        /// Tells libcurl you want a multipart/formdata HTTP POST to be made and you
        /// instruct what data to pass on to the server. Pass a reference to a 
        /// <see cref="CurlForm"/> object as parameter.
        /// <para>
        /// Using POST with HTTP 1.1 implies the use of a "Expect: 100-continue"
        /// header. You can disable this header with <c>CURLOPT_HTTPHEADER</c> as usual.
        /// </para> 
        /// </summary>
        //[ImplementsConstant("CURLOPT_HTTPPOST")] // This is just inner option, I don't want user to use it
        CURLOPT_HTTPPOST = 10024,

        ///// <summary>
        ///// Pass an <c>object</c> referene to whatever you want passed to your
        ///// <see cref="Easy.DebugFunction"/> delegate's <c>extraData</c> argument.
        ///// This reference is not used internally by libcurl, it is only passed to
        ///// the delegate. 
        ///// </summary>
        //[ImplementsConstant("CURLOPT_DEBUGDATA")]
        //CURLOPT_DEBUGDATA = 10095,

        ///// <summary>
        ///// Pass a reference to an <see cref="Easy.DebugFunction"/> delegate.
        ///// <c>CURLOPT_VERBOSE</c> must be in effect. This delegate receives debug
        ///// information, as specified with the <see cref="CURLINFOTYPE"/> argument.
        ///// This function must return 0. 
        ///// </summary>
        //[ImplementsConstant("CURLOPT_DEBUGFUNCTION")]
        //CURLOPT_DEBUGFUNCTION = 20094,


        #endregion
    };

}
