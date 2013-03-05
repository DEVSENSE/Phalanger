using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PHP.Core;

namespace PHP.Library.Curl
{

    /// <summary>
    /// This enumeration contains values used to specify the proxy type when
    /// using the <see cref="CurlOption.CURLOPT_PROXY"/> option when calling
    /// <see cref="Curl.SetOpt"/>
    /// </summary>
    public enum CURLproxyType
    {
        /// <summary>
        /// Ordinary HTTP proxy.
        /// </summary>
        [ImplementsConstant("CURLPROXY_HTTP")]
        CURLPROXY_HTTP = 0,
        /// <summary>
        /// Use if the proxy supports SOCKS4 user authentication. If you're
        /// unfamiliar with this, consult your network administrator.
        /// </summary>
        [ImplementsConstant("CURLPROXY_SOCKS4")]
        CURLPROXY_SOCKS4 = 4,
        /// <summary>
        /// Use if the proxy supports SOCKS5 user authentication. If you're
        /// unfamiliar with this, consult your network administrator.
        /// </summary>
        [ImplementsConstant("CURLPROXY_SOCKS5")]
        CURLPROXY_SOCKS5 = 5
    };

    /// <summary>
    /// This enumeration contains values used to specify the HTTP authentication
    /// when using the <see cref="CurlOption.CURLOPT_HTTPAUTH"/> option when
    /// calling <see cref="Curl.SetOpt"/>
    /// </summary>
    public enum CURLhttpAuth
    {
        /// <summary>
        /// No authentication.
        /// </summary>
        [ImplementsConstant("CURLAUTH_NONE")]
        CURLAUTH_NONE = 0,
        /// <summary>
        /// HTTP Basic authentication. This is the default choice, and the
        /// only method that is in wide-spread use and supported virtually
        /// everywhere. This is sending the user name and password over the
        /// network in plain text, easily captured by others.
        /// </summary>
        [ImplementsConstant("CURLAUTH_BASIC")]
        CURLAUTH_BASIC = 1,
        /// <summary>
        /// HTTP Digest authentication. Digest authentication is defined
        /// in RFC2617 and is a more secure way to do authentication over
        /// public networks than the regular old-fashioned Basic method.
        /// </summary>
        [ImplementsConstant("CURLAUTH_DIGEST")]
        CURLAUTH_DIGEST = 2,
        /// <summary>
        /// HTTP GSS-Negotiate authentication. The GSS-Negotiate (also known
        /// as plain "Negotiate") method was designed by Microsoft and is
        /// used in their web applications. It is primarily meant as a
        /// support for Kerberos5 authentication but may be also used along
        /// with another authentication methods. For more information see IETF
        /// draft draft-brezak-spnego-http-04.txt.
        /// <note>
        /// You need to use a version of libcurl.NET built with a suitable
        /// GSS-API library for this to work. This is not currently standard.
        /// </note>
        /// </summary>
        [ImplementsConstant("CURLAUTH_GSSNEGOTIATE")]
        CURLAUTH_GSSNEGOTIATE = 4,
        /// <summary>
        /// HTTP NTLM authentication. A proprietary protocol invented and
        /// used by Microsoft. It uses a challenge-response and hash concept
        /// similar to Digest, to prevent the password from being eavesdropped.
        /// </summary>
        [ImplementsConstant("CURLAUTH_NTLM")]       
        CURLAUTH_NTLM = 8,
        /// <summary>
        /// This is a convenience macro that sets all bits and thus makes
        /// libcurl pick any it finds suitable. libcurl will automatically
        /// select the one it finds most secure.
        /// </summary>
        [ImplementsConstant("CURLAUTH_ANY")]       
        CURLAUTH_ANY = 15,   // ~0
        /// <summary>
        /// This is a convenience macro that sets all bits except Basic
        /// and thus makes libcurl pick any it finds suitable. libcurl
        /// will automatically select the one it finds most secure.
        /// </summary>
        [ImplementsConstant("CURLAUTH_ANYSAFE")]       
        CURLAUTH_ANYSAFE = 14    // ~CURLAUTH_BASIC
    };

    /// <summary>
    /// Contains values used to specify the HTTP version level when using
    /// the <see cref="CurlOption.CURLOPT_HTTP_VERSION"/> option in a call
    /// to <see cref="Curl.SetOpt"/>
    /// </summary>
    public enum CurlHttpVersion
    {
        /// <summary>
        /// We don't care about what version the library uses. libcurl will
        /// use whatever it thinks fit.
        /// </summary>
        [ImplementsConstant("CURL_HTTP_VERSION_NONE")]
        CURL_HTTP_VERSION_NONE = 0,
        /// <summary>
        /// Enforce HTTP 1.0 requests.
        /// </summary>
        [ImplementsConstant("CURL_HTTP_VERSION_1_0")]
        CURL_HTTP_VERSION_1_0 = 1,
        /// <summary>
        /// Enforce HTTP 1.1 requests.
        /// </summary>
        [ImplementsConstant("CURL_HTTP_VERSION_1_1")]
        CURL_HTTP_VERSION_1_1 = 2,
        /// <summary>
        /// Last entry in enumeration; do not use in application code.
        /// </summary>
        [ImplementsConstant("CURL_HTTP_VERSION_LAST")]
        CURL_HTTP_VERSION_LAST = 3
    };

#region Unsupported options

    ///// <summary>
    ///// Your handler for the <see cref="Easy.IoctlFunction"/> delegate
    ///// should return a member of this enumeration.
    ///// </summary>
    //public enum CURLIOERR
    //{
    //    /// <summary>
    //    /// Indicate that the callback processed everything okay.
    //    /// </summary>
    //    CURLIOE_OK = 0,
    //    /// <summary>
    //    /// Unknown command sent to callback. Right now, only
    //    /// <code>CURLIOCMD_RESTARTREAD</code> is supported.
    //    /// </summary>
    //    CURLIOE_UNKNOWNCMD = 1,
    //    /// <summary>
    //    /// Indicate to libcurl that a restart failed.
    //    /// </summary>
    //    CURLIOE_FAILRESTART = 2,
    //    /// <summary>
    //    /// End of enumeration marker, don't use in a client application.
    //    /// </summary>
    //    CURLIOE_LAST = 3
    //}

    ///// <summary>
    ///// Your handler for the <see cref="Easy.IoctlFunction"/>
    ///// delegate is passed one of these values as its first parameter.
    ///// Right now, the only supported value is
    ///// <code>CURLIOCMD_RESTARTREAD</code>.
    ///// </summary>
    //public enum CURLIOCMD
    //{
    //    /// <summary>
    //    /// No IOCTL operation; we should never see this.
    //    /// </summary>
    //    CURLIOCMD_NOP = 0,
    //    /// <summary>
    //    /// When this is sent, your callback may need to, for example,
    //    /// rewind a local file that is being sent via FTP.
    //    /// </summary>
    //    CURLIOCMD_RESTARTREAD = 1,
    //    /// <summary>
    //    /// End of enumeration marker, don't use in a client application.
    //    /// </summary>
    //    CURLIOCMD_LAST = 2
    //}

    ///// <summary>
    ///// A member of this enumeration is passed as the first parameter to the
    ///// <see cref="Easy.DebugFunction"/> delegate to which libcurl passes
    ///// debug messages.
    ///// </summary>
    //public enum CURLINFOTYPE
    //{
    //    /// <summary>
    //    /// The data is informational text.
    //    /// </summary>
    //    CURLINFO_TEXT = 0,
    //    /// <summary>
    //    /// The data is header (or header-like) data received from the peer.
    //    /// </summary>
    //    CURLINFO_HEADER_IN = 1,
    //    /// <summary>
    //    /// The data is header (or header-like) data sent to the peer.
    //    /// </summary>
    //    CURLINFO_HEADER_OUT = 2,
    //    /// <summary>
    //    /// The data is protocol data received from the peer.
    //    /// </summary>
    //    CURLINFO_DATA_IN = 3,
    //    /// <summary>
    //    /// The data is protocol data sent to the peer.
    //    /// </summary>
    //    CURLINFO_DATA_OUT = 4,
    //    /// <summary>
    //    /// The data is SSL-related data sent to the peer.
    //    /// </summary>
    //    CURLINFO_SSL_DATA_IN = 5,
    //    /// <summary>
    //    /// The data is SSL-related data received from the peer.
    //    /// </summary>
    //    CURLINFO_SSL_DATA_OUT = 6,
    //    /// <summary>
    //    /// End of enumeration marker, don't use in a client application.
    //    /// </summary>
    //    CURLINFO_END = 7
    //};


    ///// <summary>
    ///// This enumeration contains values used to specify the FTP SSL level
    ///// using the <see cref="CurlOption.CURLOPT_FTP_SSL"/> option when calling
    ///// <see cref="Easy.SetOpt"/>
    ///// </summary>
    //public enum CURLftpSSL
    //{
    //    /// <summary>
    //    /// Don't attempt to use SSL.
    //    /// </summary>
    //    CURLFTPSSL_NONE = 0,
    //    /// <summary>
    //    /// Try using SSL, proceed as normal otherwise.
    //    /// </summary>
    //    CURLFTPSSL_TRY = 1,
    //    /// <summary>
    //    /// Require SSL for the control connection or fail with
    //    /// <see cref="CURLcode.CURLE_FTP_SSL_FAILED"/>. 
    //    /// </summary>
    //    CURLFTPSSL_CONTROL = 2,
    //    /// <summary>
    //    /// Require SSL for all communication or fail with
    //    /// <see cref="CURLcode.CURLE_FTP_SSL_FAILED"/>.
    //    /// </summary>
    //    CURLFTPSSL_ALL = 3,
    //    /// <summary>
    //    /// End-of-enumeration marker. Do not use in a client application.
    //    /// </summary>
    //    CURLFTPSSL_LAST = 4
    //};

    ///// <summary>
    ///// This enumeration contains values used to specify the FTP SSL
    ///// authorization level using the
    ///// <see cref="CurlOption.CURLOPT_FTPSSLAUTH"/> option when calling
    ///// <see cref="Easy.SetOpt"/>
    ///// </summary>
    //public enum CURLftpAuth
    //{
    //    /// <summary>
    //    /// Let <c>libcurl</c> decide on the authorization scheme.
    //    /// </summary>
    //    CURLFTPAUTH_DEFAULT = 0,
    //    /// <summary>
    //    /// Use "AUTH SSL".
    //    /// </summary>
    //    CURLFTPAUTH_SSL = 1,
    //    /// <summary>
    //    /// Use "AUTH TLS".
    //    /// </summary>
    //    CURLFTPAUTH_TLS = 2,
    //    /// <summary>
    //    /// End-of-enumeration marker. Do not use in a client application.
    //    /// </summary>
    //    CURLFTPAUTH_LAST = 3
    //};

    ///// <summary>
    ///// This enumeration contains values used to specify the IP resolution
    ///// method when using the <see cref="CurlOption.CURLOPT_IPRESOLVE"/>
    ///// option in a call to <see cref="Easy.SetOpt"/>
    ///// </summary>
    //public enum CURLipResolve
    //{
    //    /// <summary>
    //    /// Default, resolves addresses to all IP versions that your system
    //    /// allows.
    //    /// </summary>
    //    CURL_IPRESOLVE_WHATEVER = 0,
    //    /// <summary>
    //    /// Resolve to ipv4 addresses.
    //    /// </summary>
    //    CURL_IPRESOLVE_V4 = 1,
    //    /// <summary>
    //    /// Resolve to ipv6 addresses.
    //    /// </summary>
    //    CURL_IPRESOLVE_V6 = 2
    //};


    ///// <summary>
    ///// Contains values used to specify the preference of libcurl between
    ///// using user names and passwords from your ~/.netrc file, relative to
    ///// user names and passwords in the URL supplied with
    ///// <see cref="CurlOption.CURLOPT_URL"/>. This is passed when using
    ///// the <see cref="CurlOption.CURLOPT_NETRC"/> option in a call
    ///// to <see cref="Easy.SetOpt"/>
    ///// </summary>
    //public enum CURLnetrcOption
    //{
    //    /// <summary>
    //    /// The library will ignore the file and use only the information
    //    /// in the URL. This is the default. 
    //    /// </summary>
    //    CURL_NETRC_IGNORED = 0,
    //    /// <summary>
    //    /// The use of your ~/.netrc file is optional, and information in the
    //    /// URL is to be preferred. The file will be scanned with the host
    //    /// and user name (to find the password only) or with the host only,
    //    /// to find the first user name and password after that machine,
    //    /// which ever information is not specified in the URL. 
    //    /// <para>
    //    /// Undefined values of the option will have this effect.
    //    /// </para>
    //    /// </summary>
    //    CURL_NETRC_OPTIONAL = 1,
    //    /// <summary>
    //    /// This value tells the library that use of the file is required,
    //    /// to ignore the information in the URL, and to search the file
    //    /// with the host only.
    //    /// </summary>
    //    CURL_NETRC_REQUIRED = 2,
    //    /// <summary>
    //    /// Last entry in enumeration; do not use in application code.
    //    /// </summary>
    //    CURL_NETRC_LAST = 3
    //};

    ///// <summary>
    ///// Contains values used to specify the SSL version level when using
    ///// the <see cref="CurlOption.CURLOPT_SSLVERSION"/> option in a call
    ///// to <see cref="Easy.SetOpt"/>
    ///// </summary>
    //public enum CURLsslVersion
    //{
    //    /// <summary>
    //    /// Use whatever version the SSL library selects.
    //    /// </summary>
    //    CURL_SSLVERSION_DEFAULT = 0,
    //    /// <summary>
    //    /// Use TLS version 1.
    //    /// </summary>
    //    CURL_SSLVERSION_TLSv1 = 1,
    //    /// <summary>
    //    /// Use SSL version 2. This is not a good option unless it's the
    //    /// only version supported by the remote server.
    //    /// </summary>
    //    CURL_SSLVERSION_SSLv2 = 2,
    //    /// <summary>
    //    /// Use SSL version 3. This is a preferred option.
    //    /// </summary>
    //    CURL_SSLVERSION_SSLv3 = 3,
    //    /// <summary>
    //    /// Last entry in enumeration; do not use in application code.
    //    /// </summary>
    //    CURL_SSLVERSION_LAST = 4
    //};

    ///// <summary>
    ///// Contains values used to specify the time condition when using
    ///// the <see cref="CurlOption.CURLOPT_TIMECONDITION"/> option in a call
    ///// to <see cref="Easy.SetOpt"/>
    ///// </summary>
    //public enum CURLtimeCond
    //{
    //    /// <summary>
    //    /// Use no time condition.
    //    /// </summary>
    //    CURL_TIMECOND_NONE = 0,
    //    /// <summary>
    //    /// The time condition is true if the resource has been modified
    //    /// since the date/time passed in
    //    /// <see cref="CurlOption.CURLOPT_TIMEVALUE"/>.
    //    /// </summary>
    //    CURL_TIMECOND_IFMODSINCE = 1,
    //    /// <summary>
    //    /// True if the resource has not been modified since the date/time
    //    /// passed in <see cref="CurlOption.CURLOPT_TIMEVALUE"/>.
    //    /// </summary>
    //    CURL_TIMECOND_IFUNMODSINCE = 2,
    //    /// <summary>
    //    /// True if the resource's last modification date/time equals that
    //    /// passed in <see cref="CurlOption.CURLOPT_TIMEVALUE"/>.
    //    /// </summary>
    //    CURL_TIMECOND_LASTMOD = 3,
    //    /// <summary>
    //    /// Last entry in enumeration; do not use in application code.
    //    /// </summary>
    //    CURL_TIMECOND_LAST = 4
    //};

    ///// <summary>
    ///// These are options available to build a multi-part form section
    ///// in a call to <see cref="MultiPartForm.AddSection"/>
    ///// </summary>
    //public enum CURLformoption
    //{
    //    /// <summary>
    //    /// Another possibility to send options to
    //    /// <see cref="MultiPartForm.AddSection"/> is this option, that
    //    /// passes a <see cref="CurlForms"/> array reference as its value.
    //    /// Each <see cref="CurlForms"/> array element has a
    //    /// <see cref="CURLformoption"/> and a <c>string</c>. All available
    //    /// options can be used in an array, except the <c>CURLFORM_ARRAY</c>
    //    /// option itself! The last argument in such an array must always be
    //    /// <c>CURLFORM_END</c>. 
    //    /// </summary>
    //    CURLFORM_ARRAY = 8,
    //    /// <summary>
    //    /// Followed by a <c>string</c>, tells libcurl that a buffer is to be
    //    /// used to upload data instead of using a file.
    //    /// </summary>
    //    CURLFORM_BUFFER = 11,
    //    /// <summary>
    //    /// Followed by an <c>int</c> with the size of the
    //    /// <c>CURLFORM_BUFFERPTR</c> byte array, tells libcurl the length of
    //    /// the data to upload. 
    //    /// </summary>
    //    CURLFORM_BUFFERLENGTH = 13,
    //    /// <summary>
    //    /// Followed by a <c>byte[]</c> array, tells libcurl the address of
    //    /// the buffer containing data to upload (as indicated with
    //    /// <c>CURLFORM_BUFFER</c>). You must also use
    //    /// <c>CURLFORM_BUFFERLENGTH</c> to set the length of the buffer area. 
    //    /// </summary>
    //    CURLFORM_BUFFERPTR = 12,
    //    /// <summary>
    //    /// Specifies extra headers for the form POST section. This takes an
    //    /// <see cref="Slist"/> prepared in the usual way using
    //    /// <see cref="Slist.Append"/> and appends the list of headers to
    //    /// those libcurl automatically generates.
    //    /// </summary>
    //    CURLFORM_CONTENTHEADER = 15,
    //    /// <summary>
    //    /// Followed by an <c>int</c> setting the length of the contents. 
    //    /// </summary>
    //    CURLFORM_CONTENTSLENGTH = 6,
    //    /// <summary>
    //    /// Followed by a <c>string</c> with a content-type will make cURL
    //    /// use this given content-type for this file upload part, possibly
    //    /// instead of an internally chosen one. 
    //    /// </summary>
    //    CURLFORM_CONTENTTYPE = 14,
    //    /// <summary>
    //    /// Followed by a <c>string</c> is used for the contents of this part, the
    //    /// actual data to send away. If you'd like it to contain zero bytes,
    //    /// you need to set the length of the name with
    //    /// <c>CURLFORM_CONTENTSLENGTH</c>. 
    //    /// </summary>
    //    CURLFORM_COPYCONTENTS = 4,
    //    /// <summary>
    //    /// Followed by a <c>string</c> used to set the name of this part.
    //    /// If you'd like it to contain zero bytes, you need to set the
    //    /// length of the name with <c>CURLFORM_NAMELENGTH</c>. 
    //    /// </summary>
    //    CURLFORM_COPYNAME = 1,
    //    /// <summary>
    //    /// This should be the last argument to a call to
    //    /// <see cref="MultiPartForm.AddSection"/>.
    //    /// </summary>
    //    CURLFORM_END = 17,
    //    /// <summary>
    //    /// Followed by a file name, makes this part a file upload part. It
    //    /// sets the file name field to the actual file name used here,
    //    /// it gets the contents of the file and passes as data and sets the
    //    /// content-type if the given file match one of the new internally
    //    /// known file extension. For <c>CURLFORM_FILE</c> the user may send
    //    /// one or more files in one part by providing multiple <c>CURLFORM_FILE</c>
    //    /// arguments each followed by the filename (and each <c>CURLFORM_FILE</c>
    //    /// is allowed to have a <c>CURLFORM_CONTENTTYPE</c>). 
    //    /// </summary>
    //    CURLFORM_FILE = 10,
    //    /// <summary>
    //    /// Followed by a file name, and does the file read: the contents
    //    /// will be used in as data in this part. 
    //    /// </summary>
    //    CURLFORM_FILECONTENT = 7,
    //    /// <summary>
    //    /// Followed by a <c>string</c> file name, will make libcurl use the
    //    /// given name in the file upload part, instead of the actual file
    //    /// name given to <c>CURLFORM_FILE</c>. 
    //    /// </summary>
    //    CURLFORM_FILENAME = 16,
    //    /// <summary>
    //    /// Followed by an <c>int</c> setting the length of the name. 
    //    /// </summary>
    //    CURLFORM_NAMELENGTH = 3,
    //    /// <summary>
    //    /// Not used.
    //    /// </summary>
    //    CURLFORM_NOTHING = 0,
    //    /// <summary>
    //    /// No longer used.
    //    /// </summary>
    //    CURLFORM_OBSOLETE = 9,
    //    /// <summary>
    //    /// No longer used.
    //    /// </summary>
    //    CURLFORM_OBSOLETE2 = 18,
    //    /// <summary>
    //    /// Followed by a <c>byte[]</c> used for the contents of this part.
    //    /// If you'd like it to contain zero bytes, you need to set the
    //    /// length of the name with <c>CURLFORM_CONTENTSLENGTH</c>. 
    //    /// </summary>
    //    CURLFORM_PTRCONTENTS = 5,
    //    /// <summary>
    //    /// Followed by a <c>byte[]</c> used for the name of this part.
    //    /// If you'd like it to contain zero bytes, you need to set the
    //    /// length of the name with <c>CURLFORM_NAMELENGTH</c>. 
    //    /// </summary>
    //    CURLFORM_PTRNAME = 2
    //};

    ///// <summary>
    ///// One of these is returned by <see cref="MultiPartForm.AddSection"/>.
    ///// </summary>
    //public enum CURLFORMcode
    //{
    //    /// <summary>
    //    /// The section was added properly.
    //    /// </summary>
    //    CURL_FORMADD_OK = 0,
    //    /// <summary>
    //    /// Out-of-memory when adding the section.
    //    /// </summary>
    //    CURL_FORMADD_MEMORY = 1,
    //    /// <summary>
    //    /// Invalid attempt to add the same option more than once to a
    //    /// section.
    //    /// </summary>
    //    CURL_FORMADD_OPTION_TWICE = 2,
    //    /// <summary>
    //    /// Invalid attempt to pass a <c>null</c> string or byte array in
    //    /// one of the arguments.
    //    /// </summary>
    //    CURL_FORMADD_NULL = 3,
    //    /// <summary>
    //    /// Invalid attempt to pass an unrecognized option in one of the
    //    /// arguments.
    //    /// </summary>
    //    CURL_FORMADD_UNKNOWN_OPTION = 4,
    //    /// <summary>
    //    /// Incomplete argument lists.
    //    /// </summary>
    //    CURL_FORMADD_INCOMPLETE = 5,
    //    /// <summary>
    //    /// Invalid attempt to provide a nested <c>CURLFORM_ARRAY</c>.
    //    /// </summary>
    //    CURL_FORMADD_ILLEGAL_ARRAY = 6,
    //    /// <summary>
    //    /// This will not be returned so long as HTTP is enabled, which
    //    /// it always is in libcurl.NET.
    //    /// </summary>
    //    CURL_FORMADD_DISABLED = 7,
    //    /// <summary>
    //    /// End-of-enumeration marker; do not use in application code.
    //    /// </summary>
    //    CURL_FORMADD_LAST = 8
    //};

    ///// <summary>
    ///// Contains values used to specify the order in which cached connections
    ///// are closed. One of these is passed as the
    ///// <see cref="CURLoption.CURLOPT_CLOSEPOLICY"/> option in a call
    ///// to <see cref="Easy.SetOpt"/>
    ///// </summary>
    //public enum CURLclosePolicy
    //{
    //    /// <summary>
    //    /// No close policy. Never use this.
    //    /// </summary>
    //    CURLCLOSEPOLICY_NONE = 0,
    //    /// <summary>
    //    /// Close the oldest cached connections first.
    //    /// </summary>
    //    CURLCLOSEPOLICY_OLDEST = 1,
    //    /// <summary>
    //    /// Close the least recently used connections first.
    //    /// </summary>
    //    CURLCLOSEPOLICY_LEAST_RECENTLY_USED = 2,
    //    /// <summary>
    //    /// Close the connections with the least traffic first.
    //    /// </summary>
    //    CURLCLOSEPOLICY_LEAST_TRAFFIC = 3,
    //    /// <summary>
    //    /// Close the slowest connections first.
    //    /// </summary>
    //    CURLCLOSEPOLICY_SLOWEST = 4,
    //    /// <summary>
    //    /// Currently unimplemented.
    //    /// </summary>
    //    CURLCLOSEPOLICY_CALLBACK = 5,
    //    /// <summary>
    //    /// End-of-enumeration marker; do not use in application code.
    //    /// </summary>
    //    CURLCLOSEPOLICY_LAST = 6
    //};

    ///// <summary>
    ///// Contains values used to initialize libcurl internally. One of
    ///// these is passed in the call to <see cref="Curl.GlobalInit"/>.
    ///// </summary>
    //public enum CURLinitFlag
    //{
    //    /// <summary>
    //    /// Initialise nothing extra. This sets no bit.
    //    /// </summary>
    //    CURL_GLOBAL_NOTHING = 0,
    //    /// <summary>
    //    /// Initialize SSL.
    //    /// </summary>
    //    CURL_GLOBAL_SSL = 1,
    //    /// <summary>
    //    /// Initialize the Win32 socket libraries.
    //    /// </summary>
    //    CURL_GLOBAL_WIN32 = 2,
    //    /// <summary>
    //    /// Initialize everything possible. This sets all known bits.
    //    /// </summary>
    //    CURL_GLOBAL_ALL = 3,
    //    /// <summary>
    //    /// Equivalent to <c>CURL_GLOBAL_ALL</c>.
    //    /// </summary>
    //    CURL_GLOBAL_DEFAULT = CURL_GLOBAL_ALL
    //};

    ///// <summary>
    ///// Members of this enumeration should be passed to
    ///// <see cref="Share.SetOpt"/> when it is called with the
    ///// <c>CURLSHOPT_SHARE</c> or <c>CURLSHOPT_UNSHARE</c> options
    ///// provided in the <see cref="CURLSHoption"/> enumeration.
    ///// </summary>
    //public enum CURLlockData
    //{
    //    /// <summary>
    //    /// Not used.
    //    /// </summary>
    //    CURL_LOCK_DATA_NONE = 0,
    //    /// <summary>
    //    /// Used internally by libcurl.
    //    /// </summary>
    //    CURL_LOCK_DATA_SHARE = 1,
    //    /// <summary>
    //    /// Cookie data will be shared across the <see cref="Easy"/> objects
    //    /// using this shared object.
    //    /// </summary>
    //    CURL_LOCK_DATA_COOKIE = 2,
    //    /// <summary>
    //    /// Cached DNS hosts will be shared across the <see cref="Easy"/>
    //    /// objects using this shared object. 
    //    /// </summary>
    //    CURL_LOCK_DATA_DNS = 3,
    //    /// <summary>
    //    /// Not supported yet.
    //    /// </summary>
    //    CURL_LOCK_DATA_SSL_SESSION = 4,
    //    /// <summary>
    //    /// Not supported yet.
    //    /// </summary>
    //    CURL_LOCK_DATA_CONNECT = 5,
    //    /// <summary>
    //    /// End-of-enumeration marker; do not use in application code.
    //    /// </summary>
    //    CURL_LOCK_DATA_LAST = 6
    //};

    ///// <summary>
    ///// Values containing the type of shared access requested when libcurl
    ///// calls the <see cref="Share.LockFunction"/> delegate.
    ///// </summary>
    //public enum CURLlockAccess
    //{
    //    /// <summary>
    //    /// Unspecified action; the delegate should never receive this.
    //    /// </summary>
    //    CURL_LOCK_ACCESS_NONE = 0,
    //    /// <summary>
    //    /// The delegate receives this call when libcurl is requesting
    //    /// read access to the shared resource.
    //    /// </summary>
    //    CURL_LOCK_ACCESS_SHARED = 1,
    //    /// <summary>
    //    /// The delegate receives this call when libcurl is requesting
    //    /// write access to the shared resource.
    //    /// </summary>
    //    CURL_LOCK_ACCESS_SINGLE = 2,
    //    /// <summary>
    //    /// End-of-enumeration marker; do not use in application code.
    //    /// </summary>
    //    CURL_LOCK_ACCESS_LAST = 3
    //};

    ///// <summary>
    ///// Contains return codes from many of the functions in the
    ///// <see cref="Share"/> class.
    ///// </summary>
    //public enum CURLSHcode
    //{
    //    /// <summary>
    //    /// The function succeeded.
    //    /// </summary>
    //    CURLSHE_OK = 0,
    //    /// <summary>
    //    /// A bad option was passed to <see cref="Share.SetOpt"/>.
    //    /// </summary>
    //    CURLSHE_BAD_OPTION = 1,
    //    /// <summary>
    //    /// An attempt was made to pass an option to
    //    /// <see cref="Share.SetOpt"/> while the Share object is in use.
    //    /// </summary>
    //    CURLSHE_IN_USE = 2,
    //    /// <summary>
    //    /// The <see cref="Share"/> object's internal handle is invalid.
    //    /// </summary>
    //    CURLSHE_INVALID = 3,
    //    /// <summary>
    //    /// Out of memory. This is a severe problem.
    //    /// </summary>
    //    CURLSHE_NOMEM = 4,
    //    /// <summary>
    //    /// End-of-enumeration marker; do not use in application code.
    //    /// </summary>
    //    CURLSHE_LAST = 5
    //};

    ///// <summary>
    ///// A member of this enumeration is passed to the function
    ///// <see cref="Share.SetOpt"/> to configure a <see cref="Share"/>
    ///// transfer. 
    ///// </summary>
    //public enum CURLSHoption
    //{
    //    /// <summary>
    //    /// Start-of-enumeration; do not use in application code.
    //    /// </summary>
    //    CURLSHOPT_NONE = 0,
    //    /// <summary>
    //    /// The parameter, which should be a member of the
    //    /// <see cref="CURLlockData"/> enumeration, specifies a type of
    //    /// data that should be shared.
    //    /// </summary>
    //    CURLSHOPT_SHARE = 1,
    //    /// <summary>
    //    /// The parameter, which should be a member of the
    //    /// <see cref="CURLlockData"/> enumeration, specifies a type of
    //    /// data that should be unshared.
    //    /// </summary>
    //    CURLSHOPT_UNSHARE = 2,
    //    /// <summary>
    //    /// The parameter should be a reference to a
    //    /// <see cref="Share.LockFunction"/> delegate. 
    //    /// </summary>
    //    CURLSHOPT_LOCKFUNC = 3,
    //    /// <summary>
    //    /// The parameter should be a reference to a
    //    /// <see cref="Share.UnlockFunction"/> delegate. 
    //    /// </summary>
    //    CURLSHOPT_UNLOCKFUNC = 4,
    //    /// <summary>
    //    /// The parameter allows you to specify an object reference that
    //    /// will passed to the <see cref="Share.LockFunction"/> delegate and
    //    /// the <see cref="Share.UnlockFunction"/> delegate. 
    //    /// </summary>
    //    CURLSHOPT_USERDATA = 5,
    //    /// <summary>
    //    /// End-of-enumeration; do not use in application code.
    //    /// </summary>
    //    CURLSHOPT_LAST = 6
    //};

    ///// <summary>
    ///// A member of this enumeration is passed to the function
    ///// <see cref="Curl.GetVersionInfo"/> 
    ///// </summary>
    //public enum CURLversion
    //{
    //    /// <summary>
    //    /// Capabilities associated with the initial version of libcurl.
    //    /// </summary>
    //    CURLVERSION_FIRST = 0,
    //    /// <summary>
    //    /// Capabilities associated with the second version of libcurl.
    //    /// </summary>
    //    CURLVERSION_SECOND = 1,
    //    /// <summary>
    //    /// Capabilities associated with the third version of libcurl.
    //    /// </summary>
    //    CURLVERSION_THIRD = 2,
    //    /// <summary>
    //    /// Same as <c>CURLVERSION_THIRD</c>.
    //    /// </summary>
    //    CURLVERSION_NOW = CURLVERSION_THIRD,
    //    /// <summary>
    //    /// End-of-enumeration marker; do not use in application code.
    //    /// </summary>
    //    CURLVERSION_LAST = 3
    //};

    ///// <summary>
    ///// A bitmask of libcurl features
    ///// </summary>
    //public enum CURLversionFeatureBitmask
    //{
    //    /// <summary>
    //    /// Supports IPv6.
    //    /// </summary>
    //    CURL_VERSION_IPV6 = 0x01,
    //    /// <summary>
    //    /// Supports kerberos4 (when using FTP).
    //    /// </summary>
    //    CURL_VERSION_KERBEROS4 = 0x02,
    //    /// <summary>
    //    /// Supports SSL (HTTPS/FTPS).
    //    /// </summary>
    //    CURL_VERSION_SSL = 0x04,
    //    /// <summary>
    //    /// Supports HTTP deflate using libz.
    //    /// </summary>
    //    CURL_VERSION_LIBZ = 0x08,
    //    /// <summary>
    //    /// Supports HTTP NTLM (added in 7.10.6).
    //    /// </summary>
    //    CURL_VERSION_NTLM = 0x10,
    //    /// <summary>
    //    /// Supports HTTP GSS-Negotiate (added in 7.10.6).
    //    /// </summary>
    //    CURL_VERSION_GSSNEGOTIATE = 0x20,
    //    /// <summary>
    //    /// libcurl was built with extra debug capabilities built-in. This
    //    /// is mainly of interest for libcurl hackers. (added in 7.10.6) 
    //    /// </summary>
    //    CURL_VERSION_DEBUG = 0x40,
    //    /// <summary>
    //    /// libcurl was built with support for asynchronous name lookups,
    //    /// which allows more exact timeouts (even on Windows) and less
    //    /// blocking when using the multi interface. (added in 7.10.7) 
    //    /// </summary>
    //    CURL_VERSION_ASYNCHDNS = 0x80,
    //    /// <summary>
    //    /// libcurl was built with support for SPNEGO authentication
    //    /// (Simple and Protected GSS-API Negotiation Mechanism, defined
    //    /// in RFC 2478.) (added in 7.10.8) 
    //    /// </summary>
    //    CURL_VERSION_SPNEGO = 0x100,
    //    /// <summary>
    //    /// libcurl was built with support for large files.
    //    /// </summary>
    //    CURL_VERSION_LARGEFILE = 0x200,
    //    /// <summary>
    //    /// libcurl was built with support for IDNA, domain names with
    //    /// international letters. 
    //    /// </summary>
    //    CURL_VERSION_IDN = 0x400
    //};

    ///// <summary>
    ///// The status code associated with an <see cref="Easy"/> object in a
    ///// <see cref="Multi"/> operation. One of these is returned in response
    ///// to reading the <see cref="MultiInfo.Msg"/> property.
    ///// </summary>
    //public enum CURLMSG
    //{
    //    /// <summary>
    //    /// First entry in the enumeration, not used.
    //    /// </summary>
    //    CURLMSG_NONE = 0,
    //    /// <summary>
    //    /// The associated <see cref="Easy"/> object completed.
    //    /// </summary>
    //    CURLMSG_DONE = 1,
    //    /// <summary>
    //    /// End-of-enumeration marker, not used.
    //    /// </summary>
    //    CURLMSG_LAST = 2
    //};

    ///// <summary>
    ///// Contains return codes for many of the functions in the
    ///// <see cref="Multi"/> class.
    ///// </summary>
    //public enum CURLMcode
    //{
    //    /// <summary>
    //    /// You should call <see cref="Multi.Perform"/> again before calling
    //    /// <see cref="Multi.Select"/>.
    //    /// </summary>
    //    CURLM_CALL_MULTI_PERFORM = -1,
    //    /// <summary>
    //    /// The function succeded.
    //    /// </summary>
    //    CURLM_OK = 0,
    //    /// <summary>
    //    /// The internal <see cref="Multi"/> is bad.
    //    /// </summary>
    //    CURLM_BAD_HANDLE = 1,
    //    /// <summary>
    //    /// One of the <see cref="Easy"/> handles associated with the
    //    /// <see cref="Multi"/> object is bad.
    //    /// </summary>
    //    CURLM_BAD_EASY_HANDLE = 2,
    //    /// <summary>
    //    /// Out of memory. This is a severe problem.
    //    /// </summary>
    //    CURLM_OUT_OF_MEMORY = 3,
    //    /// <summary>
    //    /// Internal error deep within the libcurl library.
    //    /// </summary>
    //    CURLM_INTERNAL_ERROR = 4,
    //    /// <summary>
    //    /// End-of-enumeration marker, not used.
    //    /// </summary>
    //    CURLM_LAST = 5
    //};

#endregion

}

