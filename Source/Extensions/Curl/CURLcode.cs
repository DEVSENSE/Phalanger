using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PHP.Core;

namespace PHP.Library.Curl
{
    /// <summary>
    /// Status code returned from Curl functions.
    /// </summary>
    public enum CURLcode
    {
        /// <summary>
        /// All fine. Proceed as usual.
        /// </summary>
        [ImplementsConstant("CURLE_OK")]
        CURLE_OK = 0,
        /// <summary>
        /// Aborted by callback. An internal callback returned "abort"
        /// to libcurl. 
        /// </summary>
        [ImplementsConstant("CURLE_ABORTED_BY_CALLBACK")]
        CURLE_ABORTED_BY_CALLBACK = 42,
        /// <summary>
        /// Internal error. A function was called in a bad order.
        /// </summary>
        [ImplementsConstant("CURLE_BAD_CALLING_ORDER")]
        CURLE_BAD_CALLING_ORDER = 44,
        /// <summary>
        /// Unrecognized transfer encoding.
        /// </summary>
        [ImplementsConstant("CURLE_BAD_CONTENT_ENCODING")]
        CURLE_BAD_CONTENT_ENCODING = 61,
        /// <summary>
        /// Attempting FTP resume beyond file size.
        /// </summary>
        [ImplementsConstant("CURLE_BAD_DOWNLOAD_RESUME")]
        CURLE_BAD_DOWNLOAD_RESUME = 36,
        /// <summary>
        /// Internal error. A function was called with a bad parameter.
        /// </summary>
        [ImplementsConstant("CURLE_BAD_FUNCTION_ARGUMENT")]
        CURLE_BAD_FUNCTION_ARGUMENT = 43,
        /// <summary>
        /// Bad password entered. An error was signaled when the password was
        /// entered. This can also be the result of a "bad password" returned
        /// from a specified password callback. 
        /// </summary>
        [ImplementsConstant("CURLE_BAD_PASSWORD_ENTERED")]
        CURLE_BAD_PASSWORD_ENTERED = 46,
        /// <summary>
        /// Failed to connect to host or proxy. 
        /// </summary>
        [ImplementsConstant("CURLE_COULDNT_CONNECT")]
        CURLE_COULDNT_CONNECT = 7,
        /// <summary>
        /// Couldn't resolve host. The given remote host was not resolved. 
        /// </summary>
        [ImplementsConstant("CURLE_COULDNT_RESOLVE_HOST")]
        CURLE_COULDNT_RESOLVE_HOST = 6,
        /// <summary>
        /// Couldn't resolve proxy. The given proxy host could not be resolved.
        /// </summary>
        [ImplementsConstant("CURLE_COULDNT_RESOLVE_PROXY")]
        CURLE_COULDNT_RESOLVE_PROXY = 5,
        /// <summary>
        /// Very early initialization code failed. This is likely to be an
        /// internal error or problem. 
        /// </summary>
        [ImplementsConstant("CURLE_FAILED_INIT")]
        CURLE_FAILED_INIT = 2,
        /// <summary>
        /// Maximum file size exceeded.
        /// </summary>
        [ImplementsConstant("CURLE_FILESIZE_EXCEEDED")]
        CURLE_FILESIZE_EXCEEDED = 63,
        /// <summary>
        /// A file given with FILE:// couldn't be opened. Most likely
        /// because the file path doesn't identify an existing file. Did
        /// you check file permissions? 
        /// </summary>
        [ImplementsConstant("CURLE_FILE_COULDNT_READ_FILE")]
        CURLE_FILE_COULDNT_READ_FILE = 37,
        /// <summary>
        /// We were denied access when trying to login to an FTP server or
        /// when trying to change working directory to the one given in the URL. 
        /// </summary>
        [ImplementsConstant("CURLE_FTP_ACCESS_DENIED")]
        CURLE_FTP_ACCESS_DENIED = 9,
        /// <summary>
        /// An internal failure to lookup the host used for the new
        /// connection.
        /// </summary>
        [ImplementsConstant("CURLE_FTP_CANT_GET_HOST")]
        CURLE_FTP_CANT_GET_HOST = 15,
        /// <summary>
        /// A bad return code on either PASV or EPSV was sent by the FTP
        /// server, preventing libcurl from being able to continue. 
        /// </summary>
        [ImplementsConstant("CURLE_FTP_CANT_RECONNECT")]
        CURLE_FTP_CANT_RECONNECT = 16,
        /// <summary>
        /// The FTP SIZE command returned error. SIZE is not a kosher FTP
        /// command, it is an extension and not all servers support it. This
        /// is not a surprising error. 
        /// </summary>
        [ImplementsConstant("CURLE_FTP_COULDNT_GET_SIZE")]
        CURLE_FTP_COULDNT_GET_SIZE = 32,
        /// <summary>
        /// This was either a weird reply to a 'RETR' command or a zero byte
        /// transfer complete. 
        /// </summary>
        [ImplementsConstant("CURLE_FTP_COULDNT_RETR_FILE")]
        CURLE_FTP_COULDNT_RETR_FILE = 19,
        /// <summary>
        /// libcurl failed to set ASCII transfer type (TYPE A).
        /// </summary>
        [ImplementsConstant("CURLE_FTP_COULDNT_SET_ASCII")]
        CURLE_FTP_COULDNT_SET_ASCII = 29,
        /// <summary>
        /// Received an error when trying to set the transfer mode to binary.
        /// </summary>
        [ImplementsConstant("CURLE_FTP_COULDNT_SET_BINARY")]
        CURLE_FTP_COULDNT_SET_BINARY = 17,
        /// <summary>
        /// FTP couldn't STOR file. The server denied the STOR operation.
        /// The error buffer usually contains the server's explanation to this. 
        /// </summary>
        [ImplementsConstant("CURLE_FTP_COULDNT_STOR_FILE")]
        CURLE_FTP_COULDNT_STOR_FILE = 25,
        /// <summary>
        /// The FTP REST command returned error. This should never happen
        /// if the server is sane. 
        /// </summary>
        [ImplementsConstant("CURLE_FTP_COULDNT_USE_REST")]
        CURLE_FTP_COULDNT_USE_REST = 31,
        /// <summary>
        /// The FTP PORT command returned error. This mostly happen when
        /// you haven't specified a good enough address for libcurl to use.
        /// See <see cref="CurlOption.CURLOPT_FTPPORT"/>. 
        /// </summary>
        [ImplementsConstant("CURLE_FTP_PORT_FAILED")]
        CURLE_FTP_PORT_FAILED = 30,
        /// <summary>
        /// When sending custom "QUOTE" commands to the remote server, one
        /// of the commands returned an error code that was 400 or higher. 
        /// </summary>
        [ImplementsConstant("CURLE_FTP_QUOTE_ERROR")]
        CURLE_FTP_QUOTE_ERROR = 21,
        /// <summary>
        /// Requested FTP SSL level failed.
        /// </summary>
        [ImplementsConstant("CURLE_FTP_SSL_FAILED")]
        CURLE_FTP_SSL_FAILED = 64,
        /// <summary>
        /// The FTP server rejected access to the server after the password
        /// was sent to it. It might be because the username and/or the
        /// password were incorrect or just that the server is not allowing
        /// you access for the moment etc. 
        /// </summary>
        [ImplementsConstant("CURLE_FTP_USER_PASSWORD_INCORRECT")]
        CURLE_FTP_USER_PASSWORD_INCORRECT = 10,
        /// <summary>
        /// FTP servers return a 227-line as a response to a PASV command.
        /// If libcurl fails to parse that line, this return code is
        /// passed back. 
        /// </summary>
        [ImplementsConstant("CURLE_FTP_WEIRD_227_FORMAT")]
        CURLE_FTP_WEIRD_227_FORMAT = 14,
        /// <summary>
        /// After having sent the FTP password to the server, libcurl expects
        /// a proper reply. This error code indicates that an unexpected code
        /// was returned. 
        /// </summary>
        [ImplementsConstant("CURLE_FTP_WEIRD_PASS_REPLY")]
        CURLE_FTP_WEIRD_PASS_REPLY = 11,
        /// <summary>
        /// libcurl failed to get a sensible result back from the server as
        /// a response to either a PASV or a EPSV command. The server is flawed. 
        /// </summary>
        [ImplementsConstant("CURLE_FTP_WEIRD_PASV_REPLY")]
        CURLE_FTP_WEIRD_PASV_REPLY = 13,
        /// <summary>
        /// After connecting to an FTP server, libcurl expects to get a
        /// certain reply back. This error code implies that it got a strange
        /// or bad reply. The given remote server is probably not an
        /// OK FTP server. 
        /// </summary>
        [ImplementsConstant("CURLE_FTP_WEIRD_SERVER_REPLY")]
        CURLE_FTP_WEIRD_SERVER_REPLY = 8,
        /// <summary>
        /// After having sent user name to the FTP server, libcurl expects a
        /// proper reply. This error code indicates that an unexpected code
        /// was returned. 
        /// </summary>
        [ImplementsConstant("CURLE_FTP_WEIRD_USER_REPLY")]
        CURLE_FTP_WEIRD_USER_REPLY = 12,
        /// <summary>
        /// After a completed file transfer, the FTP server did not respond a
        /// proper "transfer successful" code. 
        /// </summary>
        [ImplementsConstant("CURLE_FTP_WRITE_ERROR")]
        CURLE_FTP_WRITE_ERROR = 20,
        /// <summary>
        /// Function not found. A required LDAP function was not found.
        /// </summary>
        [ImplementsConstant("CURLE_FUNCTION_NOT_FOUND")]
        CURLE_FUNCTION_NOT_FOUND = 41,
        /// <summary>
        /// Nothing was returned from the server, and under the circumstances,
        /// getting nothing is considered an error.
        /// </summary>
        [ImplementsConstant("CURLE_GOT_NOTHING")]
        CURLE_GOT_NOTHING = 52,
        /// <summary>
        /// This is an odd error that mainly occurs due to internal confusion.
        /// </summary>
        [ImplementsConstant("CURLE_HTTP_POST_ERROR")]
        CURLE_HTTP_POST_ERROR = 34,
        /// <summary>
        /// The HTTP server does not support or accept range requests.
        /// </summary>
        [ImplementsConstant("CURLE_HTTP_RANGE_ERROR")]
        CURLE_HTTP_RANGE_ERROR = 33,
        /// <summary>
        /// This is returned if <see cref="CurlOption.CURLOPT_FAILONERROR"/>
        /// is set TRUE and the HTTP server returns an error code that
        /// is >= 400. 
        /// </summary>
        [ImplementsConstant("CURLE_HTTP_RETURNED_ERROR")]
        CURLE_HTTP_RETURNED_ERROR = 22,
        /// <summary>
        /// Interface error. A specified outgoing interface could not be
        /// used. Set which interface to use for outgoing connections'
        /// source IP address with <see cref="CurlOption.CURLOPT_INTERFACE"/>. 
        /// </summary>
        [ImplementsConstant("CURLE_INTERFACE_FAILED")]
        CURLE_INTERFACE_FAILED = 45,
        /// <summary>
        /// End-of-enumeration marker; do not use in client applications.
        /// </summary>
        [ImplementsConstant("CURLE_LAST")]
        CURLE_LAST = 67,
        /// <summary>
        /// LDAP cannot bind. LDAP bind operation failed.
        /// </summary>
        [ImplementsConstant("CURLE_LDAP_CANNOT_BIND")]
        CURLE_LDAP_CANNOT_BIND = 38,
        /// <summary>
        /// Invalid LDAP URL.
        /// </summary>
        [ImplementsConstant("CURLE_LDAP_INVALID_URL")]
        CURLE_LDAP_INVALID_URL = 62,
        /// <summary>
        /// LDAP search failed.
        /// </summary>
        [ImplementsConstant("CURLE_LDAP_SEARCH_FAILED")]
        CURLE_LDAP_SEARCH_FAILED = 39,
        /// <summary>
        /// Library not found. The LDAP library was not found.
        /// </summary>
        [ImplementsConstant("CURLE_LIBRARY_NOT_FOUND")]
        CURLE_LIBRARY_NOT_FOUND = 40,
        /// <summary>
        /// Malformat user. User name badly specified. *Not currently used*
        /// </summary>
        [ImplementsConstant("CURLE_MALFORMAT_USER")]
        CURLE_MALFORMAT_USER = 24,
        /// <summary>
        /// This is not an error. This used to be another error code in an
        /// old libcurl version and is currently unused. 
        /// </summary>
        [ImplementsConstant("CURLE_OBSOLETE")]
        CURLE_OBSOLETE = 50,
        /// <summary>
        /// Operation timeout. The specified time-out period was reached
        /// according to the conditions. 
        /// </summary>
        [ImplementsConstant("CURLE_OPERATION_TIMEOUTED")]
        CURLE_OPERATION_TIMEOUTED = 28,
        /// <summary>
        /// Out of memory. A memory allocation request failed. This is serious
        /// badness and things are severely messed up if this ever occurs. 
        /// </summary>
        [ImplementsConstant("CURLE_OUT_OF_MEMORY")]
        CURLE_OUT_OF_MEMORY = 27,
        /// <summary>
        /// A file transfer was shorter or larger than expected. This
        /// happens when the server first reports an expected transfer size,
        /// and then delivers data that doesn't match the previously
        /// given size. 
        /// </summary>
        [ImplementsConstant("CURLE_PARTIAL_FILE")]
        CURLE_PARTIAL_FILE = 18,
        /// <summary>
        /// There was a problem reading a local file or an error returned by
        /// the read callback. 
        /// </summary>
        [ImplementsConstant("CURLE_READ_ERROR")]
        CURLE_READ_ERROR = 26,
        /// <summary>
        /// Failure with receiving network data.
        /// </summary>
        [ImplementsConstant("CURLE_RECV_ERROR")]
        CURLE_RECV_ERROR = 56,
        /// <summary>
        /// Failed sending network data.
        /// </summary>
        [ImplementsConstant("CURLE_SEND_ERROR")]
        CURLE_SEND_ERROR = 55,
        /// <summary>
        /// Sending the data requires a rewind that failed.
        /// </summary>
        [ImplementsConstant("CURL_SEND_FAIL_REWIND")]
        CURL_SEND_FAIL_REWIND = 65,
        /// <summary>
        /// Share is in use.
        /// </summary>
        [ImplementsConstant("CURLE_SHARE_IN_USE")]
        CURLE_SHARE_IN_USE = 57,
        /// <summary>
        /// Problem with the CA cert (path? access rights?) 
        /// </summary>
        [ImplementsConstant("CURLE_SSL_CACERT")]
        CURLE_SSL_CACERT = 60,
        /// <summary>
        /// There's a problem with the local client certificate. 
        /// </summary>
        [ImplementsConstant("CURLE_SSL_CERTPROBLEM")]
        CURLE_SSL_CERTPROBLEM = 58,
        /// <summary>
        /// Couldn't use specified cipher. 
        /// </summary>
        [ImplementsConstant("CURLE_SSL_CIPHER")]
        CURLE_SSL_CIPHER = 59,
        /// <summary>
        /// A problem occurred somewhere in the SSL/TLS handshake. It
        /// could be certificates (file formats, paths, permissions),
        /// passwords, and others. 
        /// </summary>
        [ImplementsConstant("CURLE_SSL_CONNECT_ERROR")]
        CURLE_SSL_CONNECT_ERROR = 35,
        /// <summary>
        /// Failed to initialize SSL engine.
        /// </summary>
        [ImplementsConstant("CURLE_SSL_ENGINE_INITFAILED")]
        CURLE_SSL_ENGINE_INITFAILED = 66,
        /// <summary>
        /// The specified crypto engine wasn't found. 
        /// </summary>
        [ImplementsConstant("CURLE_SSL_ENGINE_NOTFOUND")]
        CURLE_SSL_ENGINE_NOTFOUND = 53,
        /// <summary>
        /// Failed setting the selected SSL crypto engine as default!
        /// </summary>
        [ImplementsConstant("CURLE_SSL_ENGINE_SETFAILED")]
        CURLE_SSL_ENGINE_SETFAILED = 54,
        /// <summary>
        /// The remote server's SSL certificate was deemed not OK.
        /// </summary>
        [ImplementsConstant("CURLE_SSL_PEER_CERTIFICATE")]
        CURLE_SSL_PEER_CERTIFICATE = 51,
        /// <summary>
        /// A telnet option string was improperly formatted.
        /// </summary>
        [ImplementsConstant("CURLE_TELNET_OPTION_SYNTAX")]
        CURLE_TELNET_OPTION_SYNTAX = 49,
        /// <summary>
        /// Too many redirects. When following redirects, libcurl hit the
        /// maximum amount. Set your limit with
        /// <see cref="CurlOption.CURLOPT_MAXREDIRS"/>. 
        /// </summary>
        [ImplementsConstant("CURLE_TOO_MANY_REDIRECTS")]
        CURLE_TOO_MANY_REDIRECTS = 47,
        /// <summary>
        /// An option set with <see cref="CurlOption.CURLOPT_TELNETOPTIONS"/>
        /// was not recognized/known. Refer to the appropriate documentation. 
        /// </summary>
        [ImplementsConstant("CURLE_UNKNOWN_TELNET_OPTION")]
        CURLE_UNKNOWN_TELNET_OPTION = 48,
        /// <summary>
        /// The URL you passed to libcurl used a protocol that this libcurl
        /// does not support. The support might be a compile-time option that
        /// wasn't used, it can be a misspelled protocol string or just a
        /// protocol libcurl has no code for. 
        /// </summary>
        [ImplementsConstant("CURLE_UNSUPPORTED_PROTOCOL")]
        CURLE_UNSUPPORTED_PROTOCOL = 1,
        /// <summary>
        /// The URL was not properly formatted. 
        /// </summary>
        [ImplementsConstant("CURLE_URL_MALFORMAT")]
        CURLE_URL_MALFORMAT = 3,
        /// <summary>
        /// URL user malformatted. The user-part of the URL syntax was not
        /// correct. 
        /// </summary>
        [ImplementsConstant("CURLE_URL_MALFORMAT_USER")]
        CURLE_URL_MALFORMAT_USER = 4,
        /// <summary>
        /// An error occurred when writing received data to a local file,
        /// or an error was returned to libcurl from a write callback. 
        /// </summary>
        [ImplementsConstant("CURLE_WRITE_ERROR")]
        CURLE_WRITE_ERROR = 23,

        /// <summary>
        ///  Could not load CACERT file, missing or wrong format
        /// </summary>
        [ImplementsConstant("CURLE_SSL_CACERT_BADFILE")]
        CURLE_SSL_CACERT_BADFILE = 77, 
    };

}
