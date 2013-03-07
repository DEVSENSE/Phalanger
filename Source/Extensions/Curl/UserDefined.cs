using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using PHP.Core;

namespace PHP.Library.Curl
{
    //copied from curl, ready to be used

    #region Enums

    enum Curl_HttpReq
    {
        NONE, /* first in list */
        GET,
        POST,
        POST_FORM, /* we make a difference internally */
        PUT,
        HEAD,
        CUSTOM,
        LAST /* last in list */
    };

    enum CurlSshAuth
    {
        ANY = ~0,     /* all types supported by the server */
        NONE = 0,      /* none allowed, silly but complete */
        PUBLICKEY = (1 << 0), /* public/private key files */
        PASSWORD = (1 << 1), /* password */
        HOST = (1 << 2), /* host key files */
        KEYBOARD = (1 << 3), /* keyboard interactive */
        DEFAULT = ANY
    }


    internal enum DupString : long
    {
        CERT = 0,            /* client certificate file name */
        CERT_TYPE,       /* format for certificate (default: PEM)*/
        COOKIE,          /* HTTP cookie string to send */
        COOKIEJAR,       /* dump all cookies to this file */
        CUSTOMREQUEST,   /* HTTP/FTP/RTSP request/method to use */
        DEVICE,          /* local network interface/address to use */
        ENCODING,        /* Accept-Encoding string */
        FTP_ACCOUNT,     /* ftp account data */
        FTP_ALTERNATIVE_TO_USER, /* command to send if USER/PASS fails */
        FTPPORT,         /* port to send with the FTP PORT command */
        KEY,             /* private key file name */
        KEY_PASSWD,      /* plain text private key password */
        KEY_TYPE,        /* format for private key (default: PEM) */
        KRB_LEVEL,       /* krb security level */
        NETRC_FILE,      /* if not NULL, use this instead of trying to find
                          $HOME/.netrc */
        COPYPOSTFIELDS,  /* if POST, set the fields' values here */
        PROXY,           /* proxy to use */
        SET_RANGE,       /* range, if used */
        SET_REFERER,     /* custom string for the HTTP referer field */
        SET_URL,         /* what original URL to work on */
        SSL_CAPATH,      /* CA directory name (doesn't work on windows) */
        SSL_CAFILE,      /* certificate file to verify peer against */
        SSL_CIPHER_LIST, /* list of ciphers to use */
        SSL_EGDSOCKET,   /* path to file containing the EGD daemon socket */
        SSL_RANDOM_FILE, /* path to file containing "random" data */
        USERAGENT,       /* User-Agent string */
        SSL_CRLFILE,     /* crl file to check certificate */
        SSL_ISSUERCERT,  /* issuer cert file to check certificate */
        USERNAME,        /* <username>, if used */
        PASSWORD,        /* <password>, if used */
        PROXYUSERNAME,   /* Proxy <username>, if used */
        PROXYPASSWORD,   /* Proxy <password>, if used */
        NOPROXY,         /* List of hosts which should not use the proxy, if
                          used */
        RTSP_SESSION_ID, /* Session ID to use */
        RTSP_STREAM_URI, /* Stream URI for this request */
        RTSP_TRANSPORT,  /* Transport for this session */
#if USE_LIBSSH2
      SSH_PRIVATE_KEY, /* path to the private key file for auth */
      SSH_PUBLIC_KEY,  /* path to the public key file for auth */
      SSH_HOST_PUBLIC_KEY_MD5, /* md5 of host public key in ascii hex */
      SSH_KNOWNHOSTS,  /* file name of knownhosts file */
#endif
#if (HAVE_GSSAPI) || (USE_WINDOWS_SSPI)
      SOCKS5_GSSAPI_SERVICE,  /* GSSAPI service name */
#endif
        MAIL_FROM,

#if USE_TLS_SRP
      TLSAUTH_USERNAME,     /* TLS auth <username> */
      TLSAUTH_PASSWORD,     /* TLS auth <password> */
#endif

        /* -- end of strings -- */
        LAST /* not used, just an end-of-list marker */
    };

    #endregion


    internal struct ssl_config_data
    {
        //public long Version;          /* what version the client wants to use */
        //public long CertVerifyResult; /* result from the certificate verification */
        public bool VerifyPeer;       /* set TRUE if this is desired */
        public long VerifyHost;       /* 0: no verify
                                         1: check that CN exists
                                         2: CN must match hostname */
        //char* CApath;          /* certificate dir (doesn't work on windows) */
        //char* CAfile;          /* certificate to verify peer against */
        //char* CRLfile;        /* CRL to check certificate revocation */
        //char* issuercert;     /* optional issuer certificate filename */
        //char* random_file;     /* path to file containing "random" data */
        //char* egdsocket;       /* path to file containing the EGD daemon socket */
        //char* cipher_list;     /* list of ciphers to use */
        //long numsessions;      /* SSL session id cache size */
        ////curl_ssl_ctx_callback fsslctx; /* function to initialize ssl ctx */
        //void* fsslctxp;        /* parameter for call back */
        //public bool sessionid;        /* cache session IDs or not */
        //public bool certinfo;         /* gather lots of certificate info */

#if USE_TLS_SRP
      char *username; /* TLS username (for, e.g., SRP) */
      char *password; /* TLS password (for, e.g., SRP) */
      enum CURL_TLSAUTH authtype; /* TLS authentication type (default SRP) */
#endif
    };

    internal class UserDefined
    {

        //Constants
        public const int CURL_DEFAULT_PROXY_PORT = 1080;
        public const int DEFAULT_MAXREDIRS = 50;

        #region Fields

        private bool returntransfer;
        private int proxyport; /* If non-zero, use this port number by default. If the
                         proxy string features a ":[port]" that one will override
                         this. */
        private PhpResource infile;          /* the uploaded file is read from here */
        private PhpResource outfile;         /* the fetched file goes here */
        //TODO: (MB) eventually use PhpStream so we can take advantage of Phalanger's stream abstracion although native curl just supports FILE's here
        private int useport;     /* which port to use */
        private CURLhttpAuth httpauth;     /* what kind of HTTP authentication to use (bitmask) */
        private CURLhttpAuth proxyauth;    /* what kind of proxy authentication to use (bitmask) */

        private int maxredirs;
        private object postfields;  /* if POST, set the fields' values here */
        private long postfieldsize; /* if POST, this might have a size to use instead
                                       of strlen(), and then the data *may* be binary
                                       (contain zero bytes) */
        private PhpCallback fwrite_header; /* function that stores headers */
        private PhpCallback fwrite_func;   /* function that stores the output */

        private int timeout;
        private int connecttimeout;
        private long infilesize;      /* size of file to upload, -1 means unknown */
        private PhpArray headers; /* linked list of extra headers */
        private Curl_HttpReq httpreq;   /* what kind of HTTP request (if any) is this */
        private CurlHttpVersion httpversion; /* when non-zero, a specific HTTP version requested to
                               be used in the library's request(s) */
        private ssl_config_data ssl;  /* user defined SSL stuff */
        private CURLproxyType proxytype; /* what kind of proxy that is in use */
        private bool http_follow_location; /* follow HTTP redirects */
        private bool include_header;   /* include received protocol headers in data output */
        private bool opt_no_body;      /* as set with CURLOPT_NO_BODY */
        private bool upload;           /* upload request */
        //private CurlSshAuth ssh_auth_types;   /* allowed SSH auth types */
        private readonly object[] str = new object[(int)DupString.LAST];


        // In native curl they use this form sending multipart post messages
        //  struct curl_httppost *httppost;  /* linked list of POST data */
        CurlForm curl_httppost;

        ///////////////////
        // Change structure
        //In native curl this is in SessionHandle.Change structure

        private List<string> cookielist = new List<string>();

        ///////////////////


        ////////////////////
        //Cookie structure
        // In native curl this is in SessionHandle.Cookies

        private System.Net.CookieCollection cookies = new System.Net.CookieCollection();

        ////////////////////

        //* Here follows boolean settings that define how to behave during
        //   this session. They are STATIC, set by libcurl users or at least initially
        //   and they don't change during operations. */
        //private bool tunnel_thru_httpproxy; /* use CONNECT through a HTTP proxy */

        #endregion

        #region Currently unsupported fields

        // FILE *err;         /* the stderr user data goes here */
        // void *debugdata;   /* the data that will be passed to fdebug */
        // char *errorbuffer; /* (Static) store failure messages in here */
        //string writeheader; /* write the header to this if non-NULL */
        //void *rtp_out;     /* write RTP to this if non-NULL */
        //bool post301;      /* Obey RFC 2616/10.3.2 and keep POSTs as POSTs after a
        //                    301 */
        //bool post302;      /* keep POSTs as POSTs after a 302 */
        //bool free_referer; /* set TRUE if 'referer' points to a string we
        //                    allocated */
        //  curl_seek_callback seek_func;      /* function that seeks the input */
        //  unsigned short localport; /* local port number to bind to */
        //  int localportrange; /* number of additional port numbers to test in case the
        //                         'localport' one can't be bind()ed */

        //  curl_write_callback fwrite_rtp;    /* function that stores interleaved RTP */
        //  curl_read_callback fread_func;     /* function that reads the input */
        //  int is_fread_set; /* boolean, has read callback been set to non-NULL? */
        //  int is_fwrite_set; /* boolean, has write callback been set to non-NULL? */
        //  curl_progress_callback fprogress;  /* function for progress information */
        //  curl_debug_callback fdebug;      /* function that write informational data */
        //  curl_ioctl_callback ioctl_func;  /* function for I/O control */
        //  curl_sockopt_callback fsockopt;  /* function for setting socket options */
        //  void *sockopt_client; /* pointer to pass to the socket options callback */
        //  curl_opensocket_callback fopensocket; /* function for checking/translating
        //                                           the address and opening the socket */
        //  void* opensocket_client;

        //  void *seek_client;    /* pointer to pass to the seek callback */
        //  /* the 3 curl_conv_callback functions below are used on non-ASCII hosts */
        //  /* function to convert from the network encoding: */
        //  curl_conv_callback convfromnetwork;
        //  /* function to convert to the network encoding: */
        //  curl_conv_callback convtonetwork;
        //  /* function to convert from UTF-8 encoding: */
        //  curl_conv_callback convfromutf8;

        //  void *progress_client; /* pointer to pass to the progress callback */
        //  void *ioctl_client;   /* pointer to pass to the ioctl callback */
        //  long server_response_timeout; /* in milliseconds, 0 means no timeout */
        //  long tftp_blksize ; /* in bytes, 0 means use default */
        //  long low_speed_limit; /* bytes/second */
        //  long low_speed_time;  /* number of seconds */
        //  curl_off_t max_send_speed; /* high speed limit in bytes/second for upload */
        //  curl_off_t max_recv_speed; /* high speed limit in bytes/second for download */
        //  curl_off_t set_resume_from;  /* continue [ftp] transfer from here */
        //  bool cookiesession;   /* new cookie session? */
        //  bool crlf;            /* convert crlf on ftp upload(?) */
        //  struct curl_slist *quote;     /* after connection is established */
        //  struct curl_slist *postquote; /* after the transfer */
        //  struct curl_slist *prequote; /* before the transfer, after type */
        //  struct curl_slist *source_quote;  /* 3rd party quote */
        //  struct curl_slist *source_prequote;  /* in 3rd party transfer mode - before
        //                                          the transfer on source host */
        //  struct curl_slist *source_postquote; /* in 3rd party transfer mode - after
        //                                          the transfer on source host */
        //  struct curl_slist *telnet_options; /* linked list of telnet options */
        //  struct curl_slist *resolve;     /* list of names to add/remove from
        //                                     DNS cache */
        //  curl_TimeCond timecondition; /* kind of time/date comparison */
        //  time_t timevalue;       /* what time to compare with */
        //  long dns_cache_timeout; /* DNS cache timeout */
        //  long buffer_size;      /* size of receive buffer to use */
        //  void *private_data; /* application-private data */

        //  struct Curl_one_easy *one_easy; /* When adding an easy handle to a multi
        //                                     handle, an internal 'Curl_one_easy'
        //                                     struct is created and this is a pointer
        //                                     to the particular struct associated with
        //                                     this SessionHandle */

        //  struct curl_slist *http200aliases; /* linked list of aliases for http200 */

        //  long ipver; /* the CURL_IPRESOLVE_* defines in the public header file
        //                 0 - whatever, 1 - v2, 2 - v6 */

        //  curl_off_t max_filesize; /* Maximum file size to download */

        //  curl_ftpfile ftp_filemethod; /* how to get to a file when FTP is used  */

        //  int ftp_create_missing_dirs; /* 1 - create directories that don't exist
        //                                  2 - the same but also allow MKD to fail once
        //                               */

        //  curl_sshkeycallback ssh_keyfunc; /* key matching callback */
        //  void *ssh_keyfunc_userp;         /* custom pointer to callback */

        ///* Here follows boolean settings that define how to behave during
        //   this session. They are STATIC, set by libcurl users or at least initially
        //   and they don't change during operations. */

        //  bool printhost;        /* printing host name in debug info */
        //  bool get_filetime;     /* get the time and get of the remote file */
        //  bool prefer_ascii;     /* ASCII rather than binary */
        //  bool ftp_append;       /* append, not overwrite, on upload */
        //  bool ftp_list_only;    /* switch FTP command for listing directories */
        //  bool ftp_use_port;     /* use the FTP PORT command */
        //  bool hide_progress;    /* don't use the progress meter */
        //  bool http_fail_on_error;  /* fail on HTTP error codes >= 300 */
        //  bool http_disable_hostname_check_before_authentication;
        //  bool http_set_referer; /* is a custom referer used */
        //  bool http_auto_referer; /* set "correct" referer when following location: */
        //  bool set_port;         /* custom port number used */
        //  enum CURL_NETRC_OPTION
        //       use_netrc;        /* defined in include/curl.h */
        //  bool verbose;          /* output verbosity */
        //  bool krb;              /* kerberos connection requested */
        //  bool reuse_forbid;     /* forbidden to be reused, close after use */
        //  bool reuse_fresh;      /* do not re-use an existing connection  */
        //  bool ftp_use_epsv;     /* if EPSV is to be attempted or not */
        //  bool ftp_use_eprt;     /* if EPRT is to be attempted or not */
        //  bool ftp_use_pret;     /* if PRET is to be used before PASV or not */

        //  curl_usessl ftp_ssl;   /* if AUTH TLS is to be attempted etc, for FTP or
        //                            IMAP or POP3 or others! */
        //  curl_ftpauth ftpsslauth; /* what AUTH XXX to be attempted */
        //  curl_ftpccc ftp_ccc;   /* FTP CCC options */
        //  bool no_signal;        /* do not use any signal/alarm handler */
        //  bool global_dns_cache; /* subject for future removal */
        //  bool tcp_nodelay;      /* whether to enable TCP_NODELAY or not */
        //  bool ignorecl;         /* ignore content length */
        //  bool ftp_skip_ip;      /* skip the IP address the FTP server passes on to
        //                            us */
        //  bool connect_only;     /* make connection, let application use the socket */
        //  bool http_te_skip;     /* pass the raw body data to the user, even when
        //                            transfer-encoded (chunked, compressed) */
        //  bool http_ce_skip;     /* pass the raw body data to the user, even when
        //                            content-encoded (chunked, compressed) */
        //  long new_file_perms;    /* Permissions to use when creating remote files */
        //  long new_directory_perms; /* Permissions to use when creating remote dirs */
        //  bool proxy_transfer_mode; /* set transfer mode (;type=<a|i>) when doing FTP
        //                               via an HTTP proxy */
        //  unsigned int scope;    /* address scope for IPv6 */
        //  long allowed_protocols;
        //  long redir_protocols;
        //#if defined(HAVE_GSSAPI) || defined(USE_WINDOWS_SSPI)
        //  long socks5_gssapi_nec; /* flag to support nec socks5 server */
        //#endif
        //  struct curl_slist *mail_rcpt; /* linked list of mail recipients */
        //  /* Common RTSP header options */
        //  Curl_RtspReq rtspreq; /* RTSP request type */
        //  long rtspversion; /* like httpversion, for RTSP */
        //  bool wildcardmatch; /* enable wildcard matching */
        //  curl_chunk_bgn_callback chunk_bgn; /* called before part of transfer starts */
        //  curl_chunk_end_callback chunk_end; /* called after part transferring
        //                                        stopped */
        //  curl_fnmatch_callback fnmatch; /* callback to decide which file corresponds
        //                                    to pattern (e.g. if WILDCARDMATCH is on) */
        //  void *fnmatch_data;

        #endregion

        #region Properties

        public PhpCallback FunctionWriteHeader
        {
            get { return fwrite_header; }
        }


        public PhpCallback WriteFunction
        {
            get { return fwrite_func; }
        }

        public object Postfields
        {
            get { return postfields; }
        }

        public PhpArray Headers
        {
            get { return headers; }
        }

        internal Curl_HttpReq Httpreq
        {
            get { return httpreq; }
        }
        public CurlHttpVersion HttpVersion
        {
            get { return httpversion; }
        }

        public bool IncludeHeader
        {
            get { return include_header; }
        }

        public bool OptNoBody
        {
            get { return opt_no_body; }
        }

        public bool Upload
        {
            get { return upload; }
        }

        public bool ReturnTransfer
        {
            get { return returntransfer; }
        }

        public CurlForm HttpPostForm
        {
            get { return curl_httppost; }
        }

        /// <summary>
        /// the uploaded file is read from here
        /// </summary>
        public PhpResource Infile
        {
            get { return infile; }
        }

        /// <summary>
        /// The fetched file
        /// </summary>
        public PhpResource OutFile
        {
            get { return outfile; }
        }

        /// <summary>
        /// which port to use
        /// </summary>
        public int UsePort
        {
            get { return useport; }
        }

        /// <summary>
        /// As in HTTP Location:
        /// </summary>
        public bool FollowLocation
        {
            get { return http_follow_location; }
        }

        /// <summary>
        /// maximum no. of http(s) redirects to follow, set to -1 for infinity
        /// </summary>
        public int MaxRedirects
        {
            get { return maxredirs; }
        }

        /// <summary>
        /// Timeout in milliseconds, 0 means no timeout
        /// </summary>
        public int Timeout
        {
            get { return timeout; }
        }

        /// <summary>
        /// ConnectTimeout in milliseconds, 0 means no timeout
        /// </summary>
        public int ConnectTimeout
        {
            get { return connecttimeout; }
        }

        public object[] Str
        {
            get { return str; }
        }

        //public bool TunnelThruHttpProxy
        //{
        //    get { return tunnel_thru_httpproxy; }
        //}

        public int ProxyPort
        {
            get { return proxyport; }
        }

        public CURLproxyType ProxyType
        {
            get { return proxytype; }
        }

        public ssl_config_data Ssl
        {
            get { return ssl; }
        }

        /// <summary>
        /// Cookies set by user
        /// </summary>
        /// <remarks>
        /// This can load cookies from files. To behave same as native curl, it's necessary to call it
        /// only just before making the request
        /// </remarks>
        public System.Net.CookieCollection Cookies
        {
            get
            {
                if (cookielist.Count > 0)
                {
                    // TODO: Not good, but so far ok... we also need port info
                    // Make property from it (implementation is in CurlHttp)
                    //Uri uri = new Uri(PhpVariable.AsString(Str[(int)DupString.SET_URL]));

                    //Load the files
                    PHP.Library.Curl.Cookies.LoadCookiesFromFiles(cookies, cookielist);

                    //Clear cookieliest to prevent loading them from files again
                    cookielist.Clear();
                }

                return cookies;

            }
        }

        #endregion

        public UserDefined()
        {

            /*
            * Initialize the UserDefined fields within a SessionHandle.
            * This may be safely called on a new or existing SessionHandle.
            */

            //CurlCode res = CurlCode.CURLE_OK;

            //data.out = stdout; /* default output to stdout */
            //data.in  = stdin;  /* default input from stdin */
            //data.err  = stderr;  /* default stderr to stderr */

            /* use fwrite as default function to store output */
            //data.fwrite_func = (curl_write_callback)fwrite;

            /* use fread as default function to read input */
            //data.fread_func = (curl_read_callback)fread;
            //data.is_fread_set = 0;
            //data.is_fwrite_set = 0;

            //data.seek_func = ZERO_NULL;
            //data.seek_client = ZERO_NULL;

            /* conversion callbacks for non-ASCII hosts */
            //data.convfromnetwork = ZERO_NULL;
            //data.convtonetwork   = ZERO_NULL;
            //data.convfromutf8    = ZERO_NULL;

            infilesize = -1;      /* we don't know any size */
            postfieldsize = -1;   /* unknown size */

            http_follow_location = false;
            maxredirs = DEFAULT_MAXREDIRS; /* allow any amount by default */

            httpreq = Curl_HttpReq.GET; /* Default HTTP request */

            httpversion = CurlHttpVersion.CURL_HTTP_VERSION_NONE;

            //data.rtspreq = RTSPREQ_OPTIONS; /* Default RTSP request */
            //data.ftp_use_epsv = TRUE;   /* FTP defaults to EPSV operations */
            //data.ftp_use_eprt = TRUE;   /* FTP defaults to EPRT operations */
            //data.ftp_use_pret = FALSE;  /* mainly useful for drftpd servers */
            //data.ftp_filemethod = FTPFILE_MULTICWD;

            //data.dns_cache_timeout = 60; /* Timeout every 60 seconds by default */

            /* Set the default size of the SSL session ID cache */
            //data.ssl.numsessions = 5;

            proxyport = CURL_DEFAULT_PROXY_PORT; /* from url.h */
            proxytype = CURLproxyType.CURLPROXY_HTTP; /* defaults to HTTP proxy */
            httpauth = CURLhttpAuth.CURLAUTH_BASIC;  /* defaults to basic */
            proxyauth = CURLhttpAuth.CURLAUTH_BASIC; /* defaults to basic */

            /* make libcurl quiet by default: */
            // data.hide_progress = TRUE;  /* CURLOPT_NOPROGRESS changes these */

            /*
            * libcurl 7.10 introduced SSL verification *by default*! This needs to be
            * switched off unless wanted.
            */
            ssl.VerifyPeer = true;
            ssl.VerifyHost = 2;
#if USE_TLS_SRP
                    ssl.authtype = CURL_TLSAUTH_NONE;
#endif
            //ssh_auth_types = CurlSshAuth.DEFAULT; /* defaults to any auth type */
            //ssl.sessionid = true; /* session ID caching enabled by default */

            //data.new_file_perms = 0644;    /* Default permissions */
            //data.new_directory_perms = 0755; /* Default permissions */

            /* for the *protocols fields we don't use the CURLPROTO_ALL convenience
                define since we internally only use the lower 16 bits for the passed
                in bitmask to not conflict with the private bits */
            //data.allowed_protocols = CURLPROTO_ALL;
            //data.redir_protocols =
            //CURLPROTO_ALL & ~(CURLPROTO_FILE | CURLPROTO_SCP); /* not FILE or SCP */

#if HAVE_GSSAPI || USE_WINDOWS_SSPI
                /*
                * disallow unprotected protection negotiation NEC reference implementation
                * seem not to follow rfc1961 section 4.3/4.4
                */
                data.socks5_gssapi_nec = FALSE;
                /* set default gssapi service name */
                res = setstropt(&data.str[STRING_SOCKS5_GSSAPI_SERVICE],
                                (char *) CURL_DEFAULT_SOCKS5_GSSAPI_SERVICE);
                if (res != CURLE_OK)
                return res;
#endif

            /* This is our preferred CA cert bundle/path since install time */
#if CURL_CA_BUNDLE
                res = setstropt(&data.str[STRING_SSL_CAFILE], (char *) CURL_CA_BUNDLE);
#elif CURL_CA_PATH
                res = setstropt(&data.str[STRING_SSL_CAPATH], (char *) CURL_CA_PATH);
#endif

            //data.wildcardmatch = FALSE;
            //data.chunk_bgn = ZERO_NULL;
            //data.chunk_end = ZERO_NULL;
        }

        public CURLcode SetOption(CurlOption option, object value)
        {
            //char *argptr;
            CURLcode result = CURLcode.CURLE_OK;

#if !CURL_DISABLE_HTTP
            long bigsize;
#endif

            switch (option)
            {
                case CurlOption.CURLOPT_RETURNTRANSFER://this option isn't in libcurl, but PHP supports it
                    returntransfer = (bool)value;
                    break;

                case CurlOption.CURLOPT_CUSTOMREQUEST:
                    /*
                     * Set a custom string to use as request
                     */
                    str[(int)DupString.CUSTOMREQUEST] = value;
                    /* we don't set
                       data->set.httpreq = HTTPREQ_CUSTOM;
                       here, we continue as if we were using the already set type
                       and this just changes the actual request keyword */
                    break;

                case CurlOption.CURLOPT_UPLOAD:
                case CurlOption.CURLOPT_PUT:
                    /*
                     * We want to sent data to the remote host. If this is HTTP, that equals
                     * using the PUT request.
                     */
                    upload = (bool)value;
                    if (upload)
                    {
                        /* If this is HTTP, PUT is what's needed to "upload" */
                        httpreq = Curl_HttpReq.PUT;
                        opt_no_body = false; /* this is implied */
                    }
                    else
                        /* In HTTP, the opposite of upload is GET (unless NOBODY is true as
                           then this can be changed to HEAD later on) */
                        httpreq = Curl_HttpReq.GET;
                    break;

                case CurlOption.CURLOPT_FOLLOWLOCATION:
                    /*
                     * Follow Location: header hints on a HTTP-server.
                     */
                    http_follow_location = (bool)value;
                    break;

                case CurlOption.CURLOPT_MAXREDIRS:
                    /*
                     * The maximum amount of hops you allow curl to follow Location:
                     * headers. This should mostly be used to detect never-ending loops.
                     */
                    maxredirs = (int)value;

                    if (maxredirs > 1) // This is from php extension, not from native curl
                        http_follow_location = true;

                    break;

                case CurlOption.CURLOPT_POST:
                    /* Does this option serve a purpose anymore? Yes it does, when
                       CURLOPT_POSTFIELDS isn't used and the POST data is read off the
                       callback! */
                    if ((bool)value)
                    {
                        if (httpreq != Curl_HttpReq.POST_FORM)
                            httpreq = Curl_HttpReq.POST;
                        opt_no_body = false; /* this is implied */
                    }
                    else
                        httpreq = Curl_HttpReq.GET;
                    break;

                case CurlOption.CURLOPT_POSTFIELDS:
                    /*
                     * Like above, but use static data instead of copying it.
                     */
                    postfields = value;
                    /* Release old copied data. */
                    str[(int)DupString.COPYPOSTFIELDS] = null;
                    httpreq = Curl_HttpReq.POST;
                    break;

                case CurlOption.CURLOPT_POSTFIELDSIZE:
                    /*
                     * The size of the POSTFIELD data to prevent libcurl to do strlen() to
                     * figure it out. Enables binary posts.
                     */
                    bigsize = (long)value;

                    if (postfieldsize < bigsize &&
                       postfields == str[(int)DupString.COPYPOSTFIELDS])
                    {
                        /* Previous CURLOPT_COPYPOSTFIELDS is no longer valid. */
                        str[(int)DupString.COPYPOSTFIELDS] = null;
                        postfields = null;
                    }

                    postfieldsize = bigsize;
                    break;

                case CurlOption.CURLOPT_REFERER:
                    /*
                     * String to set in the HTTP Referer: field.
                     */
                    //(MB) what is data.change structure? why it exists?
                    //if(data.change.referer_alloc) {
                    //  free(data.change.referer);
                    //  data.change.referer_alloc = FALSE;
                    //}
                    str[(int)DupString.SET_REFERER] = value;

                    //data->change.referer = data->set.str[STRING_SET_REFERER];
                    break;

                case CurlOption.CURLOPT_USERAGENT:
                    /*
                     * String to use in the HTTP User-Agent field
                     */
                    str[(int)DupString.USERAGENT] = value;
                    break;

                case CurlOption.CURLOPT_HEADER:
                    /*
                     * Set to include the header in the general data output stream.
                     */
                    include_header = (bool)value;
                    break;

                case CurlOption.CURLOPT_HEADERFUNCTION:
                    /*
                     * Set header write callback
                     */
                    fwrite_header = (PhpCallback)value;
                    break;

                case CurlOption.CURLOPT_WRITEFUNCTION:
                    /*
                     * Set data write callback
                     */
                    fwrite_func = (PhpCallback)value;

                    break;

                case CurlOption.CURLOPT_HTTPHEADER:
                    /*
                     * Set a list with HTTP headers to use (or replace internals with)
                     */
                    headers = (PhpArray)value;
                    break;

                case CurlOption.CURLOPT_HTTPGET:
                    /*
                     * Set to force us do HTTP GET
                     */
                    if ((bool)value)
                    {
                        httpreq = Curl_HttpReq.GET;
                        upload = false; /* switch off upload */
                        opt_no_body = false; /* this is implied */
                    }
                    break;

                case CurlOption.CURLOPT_HTTP_VERSION:
                    /*
                     * This sets a requested HTTP version to be used. The value is one of
                     * the listed enums in curl/curl.h.
                     */
                    httpversion = (CurlHttpVersion)value;
                    break;

                case CurlOption.CURLOPT_INFILE:
                    /*
                     * FILE pointer to read the file to be uploaded from. Or possibly
                     * used as argument to the read callback.
                     */
                    infile = (PhpResource)value;
                    break;
                case CurlOption.CURLOPT_INFILESIZE:
                    /*
                     * If known, this should inform curl about the file size of the
                     * to-be-uploaded file.
                     */
                    infilesize = (long)value;
                    break;
                case CurlOption.CURLOPT_INFILESIZE_LARGE:
                    /*
                     * If known, this should inform curl about the file size of the
                     * to-be-uploaded file.
                     */
                    infilesize = (long)value;
                    break;

                case CurlOption.CURLOPT_URL:
                    /*
                     * The URL to fetch.
                     */
                    //if(data->change.url_alloc) {
                    //  /* the already set URL is allocated, free it first! */
                    //  free(data->change.url);
                    //  data->change.url_alloc=FALSE;
                    //}

                    str[(int)DupString.SET_URL] = value;

                    //data->change.url = data->set.str[STRING_SET_URL];
                    break;
                case CurlOption.CURLOPT_PORT:
                    /*
                     * The port number to use when getting the URL
                     */
                    useport = (int)value;
                    break;
                case CurlOption.CURLOPT_TIMEOUT:
                    /*
                     * The maximum time you allow curl to use for a single transfer
                     * operation.
                     */
                    timeout = (int)value * 1000;
                    break;

                case CurlOption.CURLOPT_CONNECTTIMEOUT:
                    /*
                     * The maximum time you allow curl to use to connect.
                     */
                    connecttimeout = (int)value * 1000;
                    break;

                case CurlOption.CURLOPT_FILE:
                    /*
                     * FILE pointer to write to or include in the data write callback
                     */
                    outfile = (PhpResource)value;
                    break;

#if !CURL_DISABLE_PROXY

                case CurlOption.CURLOPT_PROXYPORT:
                    /*
                     * Explicitly set HTTP proxy port number.
                     */
                    proxyport = (int)value;
                    break;

                case CurlOption.CURLOPT_PROXYAUTH:
                    /*
                     * Set HTTP Authentication type BITMASK.
                     */
                    {
                        CURLhttpAuth auth = (CURLhttpAuth)value;

                        //(MB) PHP doesn't have this option although it is in curl
                        //* the DIGEST_IE bit is only used to set a special marker, for all the
                        //   rest we need to handle it as normal DIGEST */
                        //data->state.authproxy.iestyle = (bool)((auth & CURLAUTH_DIGEST_IE)?
                        //                                       TRUE:FALSE);

                        //if(auth & CURLhttpAuth.CURLAUTH_DIGEST_IE) {
                        //  auth |= CURLhttpAuth.CURLAUTH_DIGEST; /* set standard digest bit */
                        //  auth &= ~CURLhttpAuth.CURLAUTH_DIGEST_IE; /* unset ie digest bit */
                        //}
                        /* switch off bits we can't support */
#if !USE_NTLM
                        auth &= ~CURLhttpAuth.CURLAUTH_NTLM; /* no NTLM without SSL */
#endif
#if !USE_HTTP_NEGOTIATE
                        auth &= ~CURLhttpAuth.CURLAUTH_GSSNEGOTIATE; /* no GSS-Negotiate without GSSAPI or WINDOWS_SSPI */
#endif
                        //if(auth != 0)
                        //  return CURLE_NOT_BUILT_IN; /* no supported types left! */

                        proxyauth = auth;
                    }
                    break;

                case CurlOption.CURLOPT_PROXY:
                    /*
                     * Set proxy server:port to use as HTTP proxy.
                     *
                     * If the proxy is set to "" we explicitly say that we don't want to use a
                     * proxy (even though there might be environment variables saying so).
                     *
                     * Setting it to NULL, means no proxy but allows the environment variables
                     * to decide for us.
                     */
                    str[(int)DupString.PROXY] = value;
                    break;

                case CurlOption.CURLOPT_PROXYTYPE:
                    /*
                     * Set proxy type. HTTP/HTTP_1_0/SOCKS4/SOCKS4a/SOCKS5/SOCKS5_HOSTNAME
                     */
                    if ((CURLproxyType)proxytype != CURLproxyType.CURLPROXY_HTTP)
                        PhpException.ArgumentValueNotSupported("value", ((CURLproxyType)proxytype).ToString());

                    proxytype = (CURLproxyType)value;
                    break;

                case CurlOption.CURLOPT_PROXYUSERPWD:
                    /*
                     * user:password needed to use the proxy
                     */
                    setstropt_userpwd(value,
                        ref str[(int)DupString.PROXYUSERNAME],
                        ref str[(int)DupString.PROXYPASSWORD]);

                    break;

                case CurlOption.CURLOPT_PROXYUSERNAME:
                    /*
                     * authentication user name to use in the operation
                     */
                    str[(int)DupString.PROXYUSERNAME] = value;
                    break;
                case CurlOption.CURLOPT_PROXYPASSWORD:
                    /*
                     * authentication password to use in the operation
                     */
                    str[(int)DupString.PROXYPASSWORD] = value;
                    break;
#endif

                case CurlOption.CURLOPT_SSL_VERIFYPEER:
                    /*
                        * Enable peer SSL verifying.
                        */

                    if (((bool)value) == false)
                        PhpException.ArgumentValueNotSupported("value", false);

                    ssl.VerifyPeer = (bool)value;
                    break;


                case CurlOption.CURLOPT_SSL_VERIFYHOST:
                    /*
                     * Enable verification of the CN contained in the peer certificate
                     */

                    if (((int)value) != 2)
                        PhpException.ArgumentValueNotSupported("value", (int)value);

                    ssl.VerifyHost = (int)value;
                    break;

                case CurlOption.CURLOPT_SSLCERT:
                    /*
                     * String that holds file name of the SSL certificate to use
                     */
                    str[(int)DupString.CERT] = value;
                    break;

                case CurlOption.CURLOPT_SSLCERTPASSWD:
                    /*
                     * String that holds the SSL or SSH private key password.
                     */
                    str[(int)DupString.KEY_PASSWD] = value;
                    break;

                case CurlOption.CURLOPT_NOBODY:
                    /*
                     * Do not include the body part in the output data stream.
                     */
                    opt_no_body = (bool)value;
                    break;

                case CurlOption.CURLOPT_HTTPAUTH:
                    /*
                     * Set HTTP Authentication type BITMASK.
                     */
                    {
                        CURLhttpAuth auth = (CURLhttpAuth)value;

                        // PHP doesn't have this option although it's in curl
                        //* the DIGEST_IE bit is only used to set a special marker, for all the
                        //   rest we need to handle it as normal DIGEST */
                        //data->state.authhost.iestyle = (bool)((auth & CURLAUTH_DIGEST_IE)?
                        //                                      TRUE:FALSE);

                        //if(auth & CURLAUTH_DIGEST_IE) {
                        //  auth |= CURLAUTH_DIGEST; /* set standard digest bit */
                        //  auth &= ~CURLAUTH_DIGEST_IE; /* unset ie digest bit */
                        //}

                        /* switch off bits we can't support */
#if !USE_NTLM
                        auth &= ~CURLhttpAuth.CURLAUTH_NTLM; /* no NTLM without SSL */
#endif
#if !USE_HTTP_NEGOTIATE
                        auth &= ~CURLhttpAuth.CURLAUTH_GSSNEGOTIATE; /* no GSS-Negotiate without GSSAPI or WINDOWS_SSPI */
#endif

                        //if(auth != null)
                        //  return CURLE_NOT_BUILT_IN; /* no supported types left! */

                        httpauth = auth;
                    }
                    break;

                case CurlOption.CURLOPT_USERPWD:
                    /*
                     * user:password to use in the operation
                     */
                    setstropt_userpwd(value,
                        ref str[(int)DupString.USERNAME],
                        ref str[(int)DupString.PASSWORD]);

                    break;
                case CurlOption.CURLOPT_USERNAME:
                    /*
                     * authentication user name to use in the operation
                     */
                    str[(int)DupString.USERNAME] = value;
                    break;
                case CurlOption.CURLOPT_PASSWORD:
                    /*
                     * authentication password to use in the operation
                     */
                    str[(int)DupString.PASSWORD] = value;
                    break;


#if !(CURL_DISABLE_COOKIES)
                  //case CURLOPT_COOKIE:
                  //  /*
                  //   * Cookie string to send to the remote server in the request.
                  //   */
                  //  result = setstropt(&data->set.str[STRING_COOKIE],
                  //                     va_arg(param, char *));
                  //  break;

                  case CurlOption.CURLOPT_COOKIEFILE:
                    /*
                     * Set cookie file to read and parse. Can be used multiple times.
                     */

                    /* append the cookie file name to the list of file names, and deal with
                         them later */
                    cookielist.Add((string) value );
                    
                    
                    break;

                  case CurlOption.CURLOPT_COOKIEJAR:
                    /*
                     * Set cookie file name to dump all cookies to when we're done.
                     */
                    str[(int)DupString.COOKIEJAR] = value;

                    //*
                    // * Activate the cookie parser. This may or may not already
                    // * have been made.
                    // */
                    //data->cookies = Curl_cookie_init(data, NULL, data->cookies,
                    //                                 data->set.cookiesession);
                    break;
#endif

                  case CurlOption.CURLOPT_HTTPPOST:
                    /*
                     * Set to make us do HTTP POST
                     */
                    curl_httppost = value as CurlForm;
                    httpreq = Curl_HttpReq.POST_FORM;
                    opt_no_body = false; /* this is implied */
                    break;

                #region UNSUPPORTED OPTIONS

                //case CurlOption.CURLOPT_CAINFO:
                //    /*
                //     * Set CA info for SSL connection. Specify file name of the CA certificate
                //     */

                //    //NOTE: Managed CURL uses CA in windows storage of certificates. It's not recommended to use this option
                //    //PhpException.Throw(PhpError.Notice,
                //    //    "For performance reasons, you shouldn't add a client certificate with CAINFO option, just add it to your certificates storage."

                //    str[(int)DupString.SSL_CAFILE] = value;
                //    break;

                //case CURLOPT_DNS_CACHE_TIMEOUT:
                //  data->set.dns_cache_timeout = va_arg(param, long);
                //  break;
                //case CURLOPT_DNS_USE_GLOBAL_CACHE:
                //{
                //  /* remember we want this enabled */
                //  long use_cache = va_arg(param, long);
                //  data->set.global_dns_cache = (bool)(0 != use_cache);
                //}
                //break;
                //case CURLOPT_SSL_CIPHER_LIST:
                //  /* set a list of cipher we want to use in the SSL connection */
                //  result = setstropt(&data->set.str[STRING_SSL_CIPHER_LIST],
                //                     va_arg(param, char *));
                //  break;

                //case CURLOPT_RANDOM_FILE:
                //  /*
                //   * This is the path name to a file that contains random data to seed
                //   * the random SSL stuff with. The file is only used for reading.
                //   */
                //  result = setstropt(&data->set.str[STRING_SSL_RANDOM_FILE],
                //                     va_arg(param, char *));
                //  break;
                //case CURLOPT_EGDSOCKET:
                //  /*
                //   * The Entropy Gathering Daemon socket pathname
                //   */
                //  result = setstropt(&data->set.str[STRING_SSL_EGDSOCKET],
                //                     va_arg(param, char *));
                //  break;
                //case CURLOPT_MAXCONNECTS:
                //  /*
                //   * Set the absolute number of maximum simultaneous alive connection that
                //   * libcurl is allowed to have.
                //   */
                //  result = Curl_ch_connc(data, data->state.connc, va_arg(param, long));
                //  break;
                //case CURLOPT_FORBID_REUSE:
                //  /*
                //   * When this transfer is done, it must not be left to be reused by a
                //   * subsequent transfer but shall be closed immediately.
                //   */
                //  data->set.reuse_forbid = (bool)(0 != va_arg(param, long));
                //  break;
                //case CURLOPT_FRESH_CONNECT:
                //  /*
                //   * This transfer shall not use a previously cached connection but
                //   * should be made with a fresh new connect!
                //   */
                //  data->set.reuse_fresh = (bool)(0 != va_arg(param, long));
                //  break;
                //case CURLOPT_VERBOSE:
                //  /*
                //   * Verbose means infof() calls that give a lot of information about
                //   * the connection and transfer procedures as well as internal choices.
                //   */
                //  data->set.verbose = (bool)(0 != va_arg(param, long));
                //  break;

                //case CURLOPT_NOPROGRESS:
                //  /*
                //   * Shut off the internal supported progress meter
                //   */
                //  data->set.hide_progress = (bool)(0 != va_arg(param, long));
                //  if(data->set.hide_progress)
                //    data->progress.flags |= PGRS_HIDE;
                //  else
                //    data->progress.flags &= ~PGRS_HIDE;
                //  break;

                //case CURLOPT_FAILONERROR:
                //  /*
                //   * Don't output the >=300 error code HTML-page, but instead only
                //   * return error.
                //   */
                //  data->set.http_fail_on_error = (bool)(0 != va_arg(param, long));
                //  break;
                //  case CURLOPT_FILETIME:
                //    /*
                //     * Try to get the file time of the remote document. The time will
                //     * later (possibly) become available using curl_easy_getinfo().
                //     */
                //    data->set.get_filetime = (bool)(0 != va_arg(param, long));
                //    break;
                //  case CURLOPT_FTP_CREATE_MISSING_DIRS:
                //    /*
                //     * An FTP option that modifies an upload to create missing directories on
                //     * the server.
                //     */
                //    switch(va_arg(param, long)) {
                //    case 0:
                //      data->set.ftp_create_missing_dirs = 0;
                //      break;
                //    case 1:
                //      data->set.ftp_create_missing_dirs = 1;
                //      break;
                //    case 2:
                //      data->set.ftp_create_missing_dirs = 2;
                //      break;
                //    default:
                //      /* reserve other values for future use */
                //      result = CURLE_UNKNOWN_OPTION;
                //      break;
                //    }
                //    break;
                //  case CURLOPT_SERVER_RESPONSE_TIMEOUT:
                //    /*
                //     * Option that specifies how quickly an server response must be obtained
                //     * before it is considered failure. For pingpong protocols.
                //     */
                //    data->set.server_response_timeout = va_arg( param , long ) * 1000;
                //    break;
                //  case CURLOPT_TFTP_BLKSIZE:
                //    /*
                //     * TFTP option that specifies the block size to use for data transmission
                //     */
                //    data->set.tftp_blksize = va_arg(param, long);
                //    break;
                //  case CURLOPT_DIRLISTONLY:
                //    /*
                //     * An option that changes the command to one that asks for a list
                //     * only, no file info details.
                //     */
                //    data->set.ftp_list_only = (bool)(0 != va_arg(param, long));
                //    break;
                //  case CURLOPT_APPEND:
                //    /*
                //     * We want to upload and append to an existing file.
                //     */
                //    data->set.ftp_append = (bool)(0 != va_arg(param, long));
                //    break;
                //  case CURLOPT_FTP_FILEMETHOD:
                //    /*
                //     * How do access files over FTP.
                //     */
                //    data->set.ftp_filemethod = (curl_ftpfile)va_arg(param, long);
                //    break;
                //  case CURLOPT_NETRC:
                //    /*
                //     * Parse the $HOME/.netrc file
                //     */
                //    data->set.use_netrc = (enum CURL_NETRC_OPTION)va_arg(param, long);
                //    break;
                //  case CURLOPT_NETRC_FILE:
                //    /*
                //     * Use this file instead of the $HOME/.netrc file
                //     */
                //    result = setstropt(&data->set.str[STRING_NETRC_FILE],
                //                       va_arg(param, char *));
                //    break;
                //  case CURLOPT_TRANSFERTEXT:
                //    /*
                //     * This option was previously named 'FTPASCII'. Renamed to work with
                //     * more protocols than merely FTP.
                //     *
                //     * Transfer using ASCII (instead of BINARY).
                //     */
                //    data->set.prefer_ascii = (bool)(0 != va_arg(param, long));
                //    break;
                //  case CURLOPT_TIMECONDITION:
                //    /*
                //     * Set HTTP time condition. This must be one of the defines in the
                //     * curl/curl.h header file.
                //     */
                //    data->set.timecondition = (curl_TimeCond)va_arg(param, long);
                //    break;
                //  case CURLOPT_TIMEVALUE:
                //    /*
                //     * This is the value to compare with the remote document with the
                //     * method set with CURLOPT_TIMECONDITION
                //     */
                //    data->set.timevalue = (time_t)va_arg(param, long);
                //    break;
                //  case CURLOPT_SSLVERSION:
                //    /*
                //     * Set explicit SSL version to try to connect with, as some SSL
                //     * implementations are lame.
                //     */
                //    data->set.ssl.version = va_arg(param, long);
                //    break;

                //#ifndef CURL_DISABLE_HTTP
                //  case CURLOPT_AUTOREFERER:
                //    /*
                //     * Switch on automatic referer that gets set if curl follows locations.
                //     */
                //    data->set.http_auto_referer = (bool)(0 != va_arg(param, long));
                //    break;

                //  case CURLOPT_ENCODING:
                //    /*
                //     * String to use at the value of Accept-Encoding header.
                //     *
                //     * If the encoding is set to "" we use an Accept-Encoding header that
                //     * encompasses all the encodings we support.
                //     * If the encoding is set to NULL we don't send an Accept-Encoding header
                //     * and ignore an received Content-Encoding header.
                //     *
                //     */
                //    argptr = va_arg(param, char *);
                //    result = setstropt(&data->set.str[STRING_ENCODING],
                //                       (argptr && !*argptr)?
                //                       (char *) ALL_CONTENT_ENCODINGS: argptr);
                //    break;

                //case CURLOPT_UNRESTRICTED_AUTH:
                //  /*
                //   * Send authentication (user+password) when following locations, even when
                //   * hostname changed.
                //   */
                //  data->set.http_disable_hostname_check_before_authentication =
                //    (bool)(0 != va_arg(param, long));
                //  break;


                //case CURLOPT_POSTREDIR:
                //{
                //  /*
                //   * Set the behaviour of POST when redirecting
                //   * CURL_REDIR_GET_ALL - POST is changed to GET after 301 and 302
                //   * CURL_REDIR_POST_301 - POST is kept as POST after 301
                //   * CURL_REDIR_POST_302 - POST is kept as POST after 302
                //   * CURL_REDIR_POST_ALL - POST is kept as POST after 301 and 302
                //   * other - POST is kept as POST after 301 and 302
                //   */
                //  long postRedir = va_arg(param, long);
                //  data->set.post301 = (bool)((postRedir & CURL_REDIR_POST_301)?TRUE:FALSE);
                //  data->set.post302 = (bool)((postRedir & CURL_REDIR_POST_302)?TRUE:FALSE);
                //}
                //break;


                //case CURLOPT_COPYPOSTFIELDS:
                //  /*
                //   * A string with POST data. Makes curl HTTP POST. Even if it is NULL.
                //   * If needed, CURLOPT_POSTFIELDSIZE must have been set prior to
                //   *  CURLOPT_COPYPOSTFIELDS and not altered later.
                //   */
                //  argptr = va_arg(param, char *);

                //  if(!argptr || data->set.postfieldsize == -1)
                //    result = setstropt(&data->set.str[STRING_COPYPOSTFIELDS], argptr);
                //  else {
                //    /*
                //     *  Check that requested length does not overflow the size_t type.
                //     */

                //    if((data->set.postfieldsize < 0) ||
                //       ((sizeof(curl_off_t) != sizeof(size_t)) &&
                //        (data->set.postfieldsize > (curl_off_t)((size_t)-1))))
                //      result = CURLE_OUT_OF_MEMORY;
                //    else {
                //      char * p;

                //      (void) setstropt(&data->set.str[STRING_COPYPOSTFIELDS], NULL);

                //      /* Allocate even when size == 0. This satisfies the need of possible
                //         later address compare to detect the COPYPOSTFIELDS mode, and
                //         to mark that postfields is used rather than read function or
                //         form data.
                //      */
                //      p = malloc((size_t)(data->set.postfieldsize?
                //                          data->set.postfieldsize:1));

                //      if(!p)
                //        result = CURLE_OUT_OF_MEMORY;
                //      else {
                //        if(data->set.postfieldsize)
                //          memcpy(p, argptr, (size_t)data->set.postfieldsize);

                //        data->set.str[STRING_COPYPOSTFIELDS] = p;
                //      }
                //    }
                //  }

                //  data->set.postfields = data->set.str[STRING_COPYPOSTFIELDS];
                //  data->set.httpreq = HTTPREQ_POST;
                //  break;

                //case CURLOPT_POSTFIELDSIZE_LARGE:
                //  /*
                //   * The size of the POSTFIELD data to prevent libcurl to do strlen() to
                //   * figure it out. Enables binary posts.
                //   */
                //  bigsize = va_arg(param, curl_off_t);

                //  if(data->set.postfieldsize < bigsize &&
                //     data->set.postfields == data->set.str[STRING_COPYPOSTFIELDS]) {
                //    /* Previous CURLOPT_COPYPOSTFIELDS is no longer valid. */
                //    (void) setstropt(&data->set.str[STRING_COPYPOSTFIELDS], NULL);
                //    data->set.postfields = NULL;
                //  }

                //  data->set.postfieldsize = bigsize;
                //  break;


                //case CURLOPT_HTTP200ALIASES:
                //  /*
                //   * Set a list of aliases for HTTP 200 in response header
                //   */
                //  data->set.http200aliases = va_arg(param, struct curl_slist *);
                //  break;

                //#if !(CURL_DISABLE_COOKIES)
                //  case CURLOPT_COOKIE:
                //    /*
                //     * Cookie string to send to the remote server in the request.
                //     */
                //    result = setstropt(&data->set.str[STRING_COOKIE],
                //                       va_arg(param, char *));
                //    break;


                //  case CURLOPT_COOKIESESSION:
                //    /*
                //     * Set this option to TRUE to start a new "cookie session". It will
                //     * prevent the forthcoming read-cookies-from-file actions to accept
                //     * cookies that are marked as being session cookies, as they belong to a
                //     * previous session.
                //     *
                //     * In the original Netscape cookie spec, "session cookies" are cookies
                //     * with no expire date set. RFC2109 describes the same action if no
                //     * 'Max-Age' is set and RFC2965 includes the RFC2109 description and adds
                //     * a 'Discard' action that can enforce the discard even for cookies that
                //     * have a Max-Age.
                //     *
                //     * We run mostly with the original cookie spec, as hardly anyone implements
                //     * anything else.
                //     */
                //    data->set.cookiesession = (bool)(0 != va_arg(param, long));
                //    break;

                //  case CURLOPT_COOKIELIST:
                //    argptr = va_arg(param, char *);

                //    if(argptr == NULL)
                //      break;

                //    if(Curl_raw_equal(argptr, "ALL")) {
                //      /* clear all cookies */
                //      Curl_cookie_clearall(data->cookies);
                //      break;
                //    }
                //    else if(Curl_raw_equal(argptr, "SESS")) {
                //      /* clear session cookies */
                //      Curl_cookie_clearsess(data->cookies);
                //      break;
                //    }
                //    else if(Curl_raw_equal(argptr, "FLUSH")) {
                //      /* flush cookies to file */
                //      Curl_flush_cookies(data, 0);
                //      break;
                //    }

                //    if(!data->cookies)
                //      /* if cookie engine was not running, activate it */
                //      data->cookies = Curl_cookie_init(data, NULL, NULL, TRUE);

                //    argptr = strdup(argptr);
                //    if(!argptr) {
                //      result = CURLE_OUT_OF_MEMORY;
                //      break;
                //    }

                //    if(checkprefix("Set-Cookie:", argptr))
                //      /* HTTP Header format line */
                //      Curl_cookie_add(data, data->cookies, TRUE, argptr + 11, NULL, NULL);

                //    else
                //      /* Netscape format line */
                //      Curl_cookie_add(data, data->cookies, FALSE, argptr, NULL, NULL);

                //    free(argptr);
                //    break;
                //#endif /* CURL_DISABLE_COOKIES */


                //#endif   /* CURL_DISABLE_HTTP */

                //#if !CURL_DISABLE_PROXY

                //case CurlOption.CURLOPT_HTTPPROXYTUNNEL:
                //    /*
                //     * Tunnel operations through the proxy instead of normal proxy use
                //     */
                //    tunnel_thru_httpproxy = (bool)value;
                //    break;

                //  case CURLOPT_PROXY_TRANSFER_MODE:
                //    /*
                //     * set transfer mode (;type=<a|i>) when doing FTP via an HTTP proxy
                //     */
                //    switch (va_arg(param, long)) {
                //    case 0:
                //      data->set.proxy_transfer_mode = FALSE;
                //      break;
                //    case 1:
                //      data->set.proxy_transfer_mode = TRUE;
                //      break;
                //    default:
                //      /* reserve other values for future use */
                //      result = CURLE_UNKNOWN_OPTION;
                //      break;
                //    }
                //    break;
                //#endif   /* CURL_DISABLE_PROXY */

                //#if HAVE_GSSAPI || USE_WINDOWS_SSPI
                //  case CURLOPT_SOCKS5_GSSAPI_SERVICE:
                //    /*
                //     * Set gssapi service name
                //     */
                //    result = setstropt(&data->set.str[STRING_SOCKS5_GSSAPI_SERVICE],
                //                       va_arg(param, char *));
                //    break;

                //  case CURLOPT_SOCKS5_GSSAPI_NEC:
                //    /*
                //     * set flag for nec socks5 support
                //     */
                //    data->set.socks5_gssapi_nec = (bool)(0 != va_arg(param, long));
                //    break;
                //#endif

                //case CURLOPT_WRITEHEADER:
                //  /*
                //   * Custom pointer to pass the header write callback function
                //   */
                //  data->set.writeheader = (void *)va_arg(param, void *);
                //  break;
                //case CURLOPT_ERRORBUFFER:
                //  /*
                //   * Error buffer provided by the caller to get the human readable
                //   * error string in.
                //   */
                //  data->set.errorbuffer = va_arg(param, char *);
                //  break;

                //case CURLOPT_FTPPORT:
                //  /*
                //   * Use FTP PORT, this also specifies which IP address to use
                //   */
                //  result = setstropt(&data->set.str[STRING_FTPPORT],
                //                     va_arg(param, char *));
                //  data->set.ftp_use_port = (bool)(NULL != data->set.str[STRING_FTPPORT]);
                //  break;

                //case CURLOPT_FTP_USE_EPRT:
                //  data->set.ftp_use_eprt = (bool)(0 != va_arg(param, long));
                //  break;

                //case CURLOPT_FTP_USE_EPSV:
                //  data->set.ftp_use_epsv = (bool)(0 != va_arg(param, long));
                //  break;

                //case CURLOPT_FTP_USE_PRET:
                //  data->set.ftp_use_pret = (bool)(0 != va_arg(param, long));
                //  break;

                //case CURLOPT_FTP_SSL_CCC:
                //  data->set.ftp_ccc = (curl_ftpccc)va_arg(param, long);
                //  break;

                //case CURLOPT_FTP_SKIP_PASV_IP:
                //  /*
                //   * Enable or disable FTP_SKIP_PASV_IP, which will disable/enable the
                //   * bypass of the IP address in PASV responses.
                //   */
                //  data->set.ftp_skip_ip = (bool)(0 != va_arg(param, long));
                //  break;

                //case CURLOPT_LOW_SPEED_LIMIT:
                //  /*
                //   * The low speed limit that if transfers are below this for
                //   * CURLOPT_LOW_SPEED_TIME, the transfer is aborted.
                //   */
                //  data->set.low_speed_limit=va_arg(param, long);
                //  break;
                //case CURLOPT_MAX_SEND_SPEED_LARGE:
                //  /*
                //   * When transfer uploads are faster then CURLOPT_MAX_SEND_SPEED_LARGE
                //   * bytes per second the transfer is throttled..
                //   */
                //  data->set.max_send_speed=va_arg(param, curl_off_t);
                //  break;
                //case CURLOPT_MAX_RECV_SPEED_LARGE:
                //  /*
                //   * When receiving data faster than CURLOPT_MAX_RECV_SPEED_LARGE bytes per
                //   * second the transfer is throttled..
                //   */
                //  data->set.max_recv_speed=va_arg(param, curl_off_t);
                //  break;
                //case CURLOPT_LOW_SPEED_TIME:
                //  /*
                //   * The low speed time that if transfers are below the set
                //   * CURLOPT_LOW_SPEED_LIMIT during this time, the transfer is aborted.
                //   */
                //  data->set.low_speed_time=va_arg(param, long);
                //  break;

                //case CurlOption.CURLOPT_TIMEOUT_MS:
                //  data.set.timeout = (long)value;
                //  break;


                //case CURLOPT_CONNECTTIMEOUT_MS:
                //  data->set.connecttimeout = va_arg(param, long);
                //  break;


                //  case CURLOPT_POSTQUOTE:
                //    /*
                //     * List of RAW FTP commands to use after a transfer
                //     */
                //    data->set.postquote = va_arg(param, struct curl_slist *);
                //    break;
                //  case CURLOPT_PREQUOTE:
                //    /*
                //     * List of RAW FTP commands to use prior to RETR (Wesley Laxton)
                //     */
                //    data->set.prequote = va_arg(param, struct curl_slist *);
                //    break;
                //  case CURLOPT_QUOTE:
                //    /*
                //     * List of RAW FTP commands to use before a transfer
                //     */
                //    data->set.quote = va_arg(param, struct curl_slist *);
                //    break;
                //  case CURLOPT_RESOLVE:
                //    /*
                //     * List of NAME:[address] names to populate the DNS cache with
                //     * Prefix the NAME with dash (-) to _remove_ the name from the cache.
                //     *
                //     * Names added with this API will remain in the cache until explicitly
                //     * removed or the handle is cleaned up.
                //     *
                //     * This API can remove any name from the DNS cache, but only entries
                //     * that aren't actually in use right now will be pruned immediately.
                //     */
                //    data->set.resolve = va_arg(param, struct curl_slist *);
                //    data->change.resolve = data->set.resolve;
                //    break;
                //  case CURLOPT_PROGRESSFUNCTION:
                //    /*
                //     * Progress callback function
                //     */
                //    data->set.fprogress = va_arg(param, curl_progress_callback);
                //    if(data->set.fprogress)
                //      data->progress.callback = TRUE; /* no longer internal */
                //    else
                //      data->progress.callback = FALSE; /* NULL enforces internal */

                //    break;
                //  case CURLOPT_PROGRESSDATA:
                //    /*
                //     * Custom client data to pass to the progress callback
                //     */
                //    data->set.progress_client = va_arg(param, void *);
                //    break;

                //#ifndef CURL_DISABLE_PROXY

                //  case CURLOPT_NOPROXY:
                //    /*
                //     * proxy exception list
                //     */
                //    result = setstropt(&data->set.str[STRING_NOPROXY],
                //                       va_arg(param, char *));
                //    break;
                //#endif

                //  case CURLOPT_RANGE:
                //    /*
                //     * What range of the file you want to transfer
                //     */
                //    result = setstropt(&data->set.str[STRING_SET_RANGE],
                //                       va_arg(param, char *));
                //    break;
                //  case CURLOPT_RESUME_FROM:
                //    /*
                //     * Resume transfer at the give file position
                //     */
                //    data->set.set_resume_from = va_arg(param, long);
                //    break;
                //  case CURLOPT_RESUME_FROM_LARGE:
                //    /*
                //     * Resume transfer at the give file position
                //     */
                //    data->set.set_resume_from = va_arg(param, curl_off_t);
                //    break;
                //  case CURLOPT_DEBUGFUNCTION:
                //    /*
                //     * stderr write callback.
                //     */
                //    data->set.fdebug = va_arg(param, curl_debug_callback);
                //    /*
                //     * if the callback provided is NULL, it'll use the default callback
                //     */
                //    break;
                //  case CURLOPT_DEBUGDATA:
                //    /*
                //     * Set to a void * that should receive all error writes. This
                //     * defaults to CURLOPT_STDERR for normal operations.
                //     */
                //    data->set.debugdata = va_arg(param, void *);
                //    break;
                //  case CURLOPT_STDERR:
                //    /*
                //     * Set to a FILE * that should receive all error writes. This
                //     * defaults to stderr for normal operations.
                //     */
                //    data->set.err = va_arg(param, FILE *);
                //    if(!data->set.err)
                //      data->set.err = stderr;
                //    break;
                //  case CURLOPT_WRITEFUNCTION:
                //    /*
                //     * Set data write callback
                //     */
                //    data->set.fwrite_func = va_arg(param, curl_write_callback);
                //    if(!data->set.fwrite_func) {
                //      data->set.is_fwrite_set = 0;
                //      /* When set to NULL, reset to our internal default function */
                //      data->set.fwrite_func = (curl_write_callback)fwrite;
                //    }
                //    else
                //      data->set.is_fwrite_set = 1;
                //    break;
                //  case CURLOPT_READFUNCTION:
                //    /*
                //     * Read data callback
                //     */
                //    data->set.fread_func = va_arg(param, curl_read_callback);
                //    if(!data->set.fread_func) {
                //      data->set.is_fread_set = 0;
                //      /* When set to NULL, reset to our internal default function */
                //      data->set.fread_func = (curl_read_callback)fread;
                //    }
                //    else
                //      data->set.is_fread_set = 1;
                //    break;
                //  case CURLOPT_SEEKFUNCTION:
                //    /*
                //     * Seek callback. Might be NULL.
                //     */
                //    data->set.seek_func = va_arg(param, curl_seek_callback);
                //    break;
                //  case CURLOPT_SEEKDATA:
                //    /*
                //     * Seek control callback. Might be NULL.
                //     */
                //    data->set.seek_client = va_arg(param, void *);
                //    break;
                //  case CURLOPT_CONV_FROM_NETWORK_FUNCTION:
                //    /*
                //     * "Convert from network encoding" callback
                //     */
                //    data->set.convfromnetwork = va_arg(param, curl_conv_callback);
                //    break;
                //  case CURLOPT_CONV_TO_NETWORK_FUNCTION:
                //    /*
                //     * "Convert to network encoding" callback
                //     */
                //    data->set.convtonetwork = va_arg(param, curl_conv_callback);
                //    break;
                //  case CURLOPT_CONV_FROM_UTF8_FUNCTION:
                //    /*
                //     * "Convert from UTF-8 encoding" callback
                //     */
                //    data->set.convfromutf8 = va_arg(param, curl_conv_callback);
                //    break;
                //  case CURLOPT_IOCTLFUNCTION:
                //    /*
                //     * I/O control callback. Might be NULL.
                //     */
                //    data->set.ioctl_func = va_arg(param, curl_ioctl_callback);
                //    break;
                //  case CURLOPT_IOCTLDATA:
                //    /*
                //     * I/O control data pointer. Might be NULL.
                //     */
                //    data->set.ioctl_client = va_arg(param, void *);
                //    break;

                //  case CURLOPT_SSLCERTTYPE:
                //    /*
                //     * String that holds file type of the SSL certificate to use
                //     */
                //    result = setstropt(&data->set.str[STRING_CERT_TYPE],
                //                       va_arg(param, char *));
                //    break;
                //  case CURLOPT_SSLKEY:
                //    /*
                //     * String that holds file name of the SSL key to use
                //     */
                //    result = setstropt(&data->set.str[STRING_KEY],
                //                       va_arg(param, char *));
                //    break;
                //  case CURLOPT_SSLKEYTYPE:
                //    /*
                //     * String that holds file type of the SSL key to use
                //     */
                //    result = setstropt(&data->set.str[STRING_KEY_TYPE],
                //                       va_arg(param, char *));
                //    break;
                //  case CURLOPT_SSLENGINE:
                //    /*
                //     * String that holds the SSL crypto engine.
                //     */
                //    argptr = va_arg(param, char *);
                //    if(argptr && argptr[0])
                //      result = Curl_ssl_set_engine(data, argptr);
                //    break;

                //  case CURLOPT_SSLENGINE_DEFAULT:
                //    /*
                //     * flag to set engine as default.
                //     */
                //    result = Curl_ssl_set_engine_default(data);
                //    break;
                //  case CURLOPT_CRLF:
                //    /*
                //     * Kludgy option to enable CRLF conversions. Subject for removal.
                //     */
                //    data->set.crlf = (bool)(0 != va_arg(param, long));
                //    break;

                //  case CURLOPT_INTERFACE:
                //    /*
                //     * Set what interface or address/hostname to bind the socket to when
                //     * performing an operation and thus what from-IP your connection will use.
                //     */
                //    result = setstropt(&data->set.str[STRING_DEVICE],
                //                       va_arg(param, char *));
                //    break;
                //  case CURLOPT_LOCALPORT:
                //    /*
                //     * Set what local port to bind the socket to when performing an operation.
                //     */
                //    data->set.localport = curlx_sltous(va_arg(param, long));
                //    break;
                //  case CURLOPT_LOCALPORTRANGE:
                //    /*
                //     * Set number of local ports to try, starting with CURLOPT_LOCALPORT.
                //     */
                //    data->set.localportrange = curlx_sltosi(va_arg(param, long));
                //    break;
                //  case CURLOPT_KRBLEVEL:
                //    /*
                //     * A string that defines the kerberos security level.
                //     */
                //    result = setstropt(&data->set.str[STRING_KRB_LEVEL],
                //                       va_arg(param, char *));
                //    data->set.krb = (bool)(NULL != data->set.str[STRING_KRB_LEVEL]);
                //    break;

                //#ifdef USE_SSLEAY
                //    /* since these two options are only possible to use on an OpenSSL-
                //       powered libcurl we #ifdef them on this condition so that libcurls
                //       built against other SSL libs will return a proper error when trying
                //       to set this option! */
                //  case CURLOPT_SSL_CTX_FUNCTION:
                //    /*
                //     * Set a SSL_CTX callback
                //     */
                //    data->set.ssl.fsslctx = va_arg(param, curl_ssl_ctx_callback);
                //    break;
                //  case CURLOPT_SSL_CTX_DATA:
                //    /*
                //     * Set a SSL_CTX callback parameter pointer
                //     */
                //    data->set.ssl.fsslctxp = va_arg(param, void *);
                //    break;
                //  case CURLOPT_CERTINFO:
                //    data->set.ssl.certinfo = (bool)(0 != va_arg(param, long));
                //    break;
                //#endif

                //  case CURLOPT_CAPATH:
                //    /*
                //     * Set CA path info for SSL connection. Specify directory name of the CA
                //     * certificates which have been prepared using openssl c_rehash utility.
                //     */
                //    /* This does not work on windows. */
                //    result = setstropt(&data->set.str[STRING_SSL_CAPATH],
                //                       va_arg(param, char *));
                //    break;
                //  case CURLOPT_CRLFILE:
                //    /*
                //     * Set CRL file info for SSL connection. Specify file name of the CRL
                //     * to check certificates revocation
                //     */
                //    result = setstropt(&data->set.str[STRING_SSL_CRLFILE],
                //                       va_arg(param, char *));
                //    break;
                //  case CURLOPT_ISSUERCERT:
                //    /*
                //     * Set Issuer certificate file
                //     * to check certificates issuer
                //     */
                //    result = setstropt(&data->set.str[STRING_SSL_ISSUERCERT],
                //                       va_arg(param, char *));
                //    break;
                //  case CURLOPT_TELNETOPTIONS:
                //    /*
                //     * Set a linked list of telnet options
                //     */
                //    data->set.telnet_options = va_arg(param, struct curl_slist *);
                //    break;

                //  case CURLOPT_BUFFERSIZE:
                //    /*
                //     * The application kindly asks for a differently sized receive buffer.
                //     * If it seems reasonable, we'll use it.
                //     */
                //    data->set.buffer_size = va_arg(param, long);

                //    if((data->set.buffer_size> (BUFSIZE -1 )) ||
                //       (data->set.buffer_size < 1))
                //      data->set.buffer_size = 0; /* huge internal default */

                //    break;

                //  case CURLOPT_NOSIGNAL:
                //    /*
                //     * The application asks not to set any signal() or alarm() handlers,
                //     * even when using a timeout.
                //     */
                //    data->set.no_signal = (bool)(0 != va_arg(param, long));
                //    break;

                //  case CURLOPT_SHARE:
                //  {
                //    struct Curl_share *set;
                //    set = va_arg(param, struct Curl_share *);

                //    /* disconnect from old share, if any */
                //    if(data->share) {
                //      Curl_share_lock(data, CURL_LOCK_DATA_SHARE, CURL_LOCK_ACCESS_SINGLE);

                //      if(data->dns.hostcachetype == HCACHE_SHARED) {
                //        data->dns.hostcache = NULL;
                //        data->dns.hostcachetype = HCACHE_NONE;
                //      }

                //      if(data->share->cookies == data->cookies)
                //        data->cookies = NULL;

                //      data->share->dirty--;

                //      Curl_share_unlock(data, CURL_LOCK_DATA_SHARE);
                //      data->share = NULL;
                //    }

                //    /* use new share if it set */
                //    data->share = set;
                //    if(data->share) {

                //      Curl_share_lock(data, CURL_LOCK_DATA_SHARE, CURL_LOCK_ACCESS_SINGLE);

                //      data->share->dirty++;

                //      if(data->share->hostcache) {
                //        /* use shared host cache, first free the private one if any */
                //        if(data->dns.hostcachetype == HCACHE_PRIVATE)
                //          Curl_hash_destroy(data->dns.hostcache);

                //        data->dns.hostcache = data->share->hostcache;
                //        data->dns.hostcachetype = HCACHE_SHARED;
                //      }
                //#if !defined(CURL_DISABLE_HTTP) && !defined(CURL_DISABLE_COOKIES)
                //      if(data->share->cookies) {
                //        /* use shared cookie list, first free own one if any */
                //        if(data->cookies)
                //          Curl_cookie_cleanup(data->cookies);
                //        /* enable cookies since we now use a share that uses cookies! */
                //        data->cookies = data->share->cookies;
                //      }
                //#endif   /* CURL_DISABLE_HTTP */
                //      Curl_share_unlock(data, CURL_LOCK_DATA_SHARE);

                //    }
                //    /* check for host cache not needed,
                //     * it will be done by curl_easy_perform */
                //  }
                //  break;

                //  case CURLOPT_PRIVATE:
                //    /*
                //     * Set private data pointer.
                //     */
                //    data->set.private_data = va_arg(param, void *);
                //    break;

                //  case CURLOPT_MAXFILESIZE:
                //    /*
                //     * Set the maximum size of a file to download.
                //     */
                //    data->set.max_filesize = va_arg(param, long);
                //    break;

                //#ifdef USE_SSL
                //  case CURLOPT_USE_SSL:
                //    /*
                //     * Make transfers attempt to use SSL/TLS.
                //     */
                //    data->set.ftp_ssl = (curl_usessl)va_arg(param, long);
                //    break;
                //#endif
                //  case CURLOPT_FTPSSLAUTH:
                //    /*
                //     * Set a specific auth for FTP-SSL transfers.
                //     */
                //    data->set.ftpsslauth = (curl_ftpauth)va_arg(param, long);
                //    break;

                //  case CURLOPT_IPRESOLVE:
                //    data->set.ipver = va_arg(param, long);
                //    break;

                //  case CURLOPT_MAXFILESIZE_LARGE:
                //    /*
                //     * Set the maximum size of a file to download.
                //     */
                //    data->set.max_filesize = va_arg(param, curl_off_t);
                //    break;

                //  case CURLOPT_TCP_NODELAY:
                //    /*
                //     * Enable or disable TCP_NODELAY, which will disable/enable the Nagle
                //     * algorithm
                //     */
                //    data->set.tcp_nodelay = (bool)(0 != va_arg(param, long));
                //    break;

                //  case CURLOPT_FTP_ACCOUNT:
                //    result = setstropt(&data->set.str[STRING_FTP_ACCOUNT],
                //                       va_arg(param, char *));
                //    break;

                //  case CURLOPT_IGNORE_CONTENT_LENGTH:
                //    data->set.ignorecl = (bool)(0 != va_arg(param, long));
                //    break;

                //  case CURLOPT_CONNECT_ONLY:
                //    /*
                //     * No data transfer, set up connection and let application use the socket
                //     */
                //    data->set.connect_only = (bool)(0 != va_arg(param, long));
                //    break;

                //  case CURLOPT_FTP_ALTERNATIVE_TO_USER:
                //    result = setstropt(&data->set.str[STRING_FTP_ALTERNATIVE_TO_USER],
                //                       va_arg(param, char *));
                //    break;

                //  case CURLOPT_SOCKOPTFUNCTION:
                //    /*
                //     * socket callback function: called after socket() but before connect()
                //     */
                //    data->set.fsockopt = va_arg(param, curl_sockopt_callback);
                //    break;

                //  case CURLOPT_SOCKOPTDATA:
                //    /*
                //     * socket callback data pointer. Might be NULL.
                //     */
                //    data->set.sockopt_client = va_arg(param, void *);
                //    break;

                //  case CURLOPT_OPENSOCKETFUNCTION:
                //    /*
                //     * open/create socket callback function: called instead of socket(),
                //     * before connect()
                //     */
                //    data->set.fopensocket = va_arg(param, curl_opensocket_callback);
                //    break;

                //  case CURLOPT_OPENSOCKETDATA:
                //    /*
                //     * socket callback data pointer. Might be NULL.
                //     */
                //    data->set.opensocket_client = va_arg(param, void *);
                //    break;

                //  case CURLOPT_SSL_SESSIONID_CACHE:
                //    data->set.ssl.sessionid = (bool)(0 != va_arg(param, long));
                //    break;

                //#ifdef USE_LIBSSH2
                //    /* we only include SSH options if explicitly built to support SSH */
                //  case CURLOPT_SSH_AUTH_TYPES:
                //    data->set.ssh_auth_types = va_arg(param, long);
                //    break;

                //  case CURLOPT_SSH_PUBLIC_KEYFILE:
                //    /*
                //     * Use this file instead of the $HOME/.ssh/id_dsa.pub file
                //     */
                //    result = setstropt(&data->set.str[STRING_SSH_PUBLIC_KEY],
                //                       va_arg(param, char *));
                //    break;

                //  case CURLOPT_SSH_PRIVATE_KEYFILE:
                //    /*
                //     * Use this file instead of the $HOME/.ssh/id_dsa file
                //     */
                //    result = setstropt(&data->set.str[STRING_SSH_PRIVATE_KEY],
                //                       va_arg(param, char *));
                //    break;
                //  case CURLOPT_SSH_HOST_PUBLIC_KEY_MD5:
                //    /*
                //     * Option to allow for the MD5 of the host public key to be checked
                //     * for validation purposes.
                //     */
                //    result = setstropt(&data->set.str[STRING_SSH_HOST_PUBLIC_KEY_MD5],
                //                       va_arg(param, char *));
                //    break;
                //#ifdef HAVE_LIBSSH2_KNOWNHOST_API
                //  case CURLOPT_SSH_KNOWNHOSTS:
                //    /*
                //     * Store the file name to read known hosts from.
                //     */
                //    result = setstropt(&data->set.str[STRING_SSH_KNOWNHOSTS],
                //                       va_arg(param, char *));
                //    break;

                //  case CURLOPT_SSH_KEYFUNCTION:
                //    /* setting to NULL is fine since the ssh.c functions themselves will
                //       then rever to use the internal default */
                //    data->set.ssh_keyfunc = va_arg(param, curl_sshkeycallback);
                //    break;

                //  case CURLOPT_SSH_KEYDATA:
                //    /*
                //     * Custom client data to pass to the SSH keyfunc callback
                //     */
                //    data->set.ssh_keyfunc_userp = va_arg(param, void *);
                //    break;
                //#endif /* HAVE_LIBSSH2_KNOWNHOST_API */

                //#endif /* USE_LIBSSH2 */

                //  case CURLOPT_HTTP_TRANSFER_DECODING:
                //    /*
                //     * disable libcurl transfer encoding is used
                //     */
                //    data->set.http_te_skip = (bool)(0 == va_arg(param, long));
                //    break;

                //  case CURLOPT_HTTP_CONTENT_DECODING:
                //    /*
                //     * raw data passed to the application when content encoding is used
                //     */
                //    data->set.http_ce_skip = (bool)(0 == va_arg(param, long));
                //    break;

                //  case CURLOPT_NEW_FILE_PERMS:
                //    /*
                //     * Uses these permissions instead of 0644
                //     */
                //    data->set.new_file_perms = va_arg(param, long);
                //    break;

                //  case CURLOPT_NEW_DIRECTORY_PERMS:
                //    /*
                //     * Uses these permissions instead of 0755
                //     */
                //    data->set.new_directory_perms = va_arg(param, long);
                //    break;

                //  case CURLOPT_ADDRESS_SCOPE:
                //    /*
                //     * We always get longs when passed plain numericals, but for this value we
                //     * know that an unsigned int will always hold the value so we blindly
                //     * typecast to this type
                //     */
                //    data->set.scope = curlx_sltoui(va_arg(param, long));
                //    break;

                //  case CURLOPT_PROTOCOLS:
                //    /* set the bitmask for the protocols that are allowed to be used for the
                //       transfer, which thus helps the app which takes URLs from users or other
                //       external inputs and want to restrict what protocol(s) to deal
                //       with. Defaults to CURLPROTO_ALL. */
                //    data->set.allowed_protocols = va_arg(param, long);
                //    break;

                //  case CURLOPT_REDIR_PROTOCOLS:
                //    /* set the bitmask for the protocols that libcurl is allowed to follow to,
                //       as a subset of the CURLOPT_PROTOCOLS ones. That means the protocol needs
                //       to be set in both bitmasks to be allowed to get redirected to. Defaults
                //       to all protocols except FILE and SCP. */
                //    data->set.redir_protocols = va_arg(param, long);
                //    break;

                //  case CURLOPT_MAIL_FROM:
                //    result = setstropt(&data->set.str[STRING_MAIL_FROM],
                //                       va_arg(param, char *));
                //    break;

                //  case CURLOPT_MAIL_RCPT:
                //    /* get a list of mail recipients */
                //    data->set.mail_rcpt = va_arg(param, struct curl_slist *);
                //    break;

                //  case CURLOPT_RTSP_REQUEST:
                //    {
                //      /*
                //       * Set the RTSP request method (OPTIONS, SETUP, PLAY, etc...)
                //       * Would this be better if the RTSPREQ_* were just moved into here?
                //       */
                //      long curl_rtspreq = va_arg(param, long);
                //      Curl_RtspReq rtspreq = RTSPREQ_NONE;
                //      switch(curl_rtspreq) {
                //        case CURL_RTSPREQ_OPTIONS:
                //          rtspreq = RTSPREQ_OPTIONS;
                //          break;

                //        case CURL_RTSPREQ_DESCRIBE:
                //          rtspreq = RTSPREQ_DESCRIBE;
                //          break;

                //        case CURL_RTSPREQ_ANNOUNCE:
                //          rtspreq = RTSPREQ_ANNOUNCE;
                //          break;

                //        case CURL_RTSPREQ_SETUP:
                //          rtspreq = RTSPREQ_SETUP;
                //          break;

                //        case CURL_RTSPREQ_PLAY:
                //          rtspreq = RTSPREQ_PLAY;
                //          break;

                //        case CURL_RTSPREQ_PAUSE:
                //          rtspreq = RTSPREQ_PAUSE;
                //          break;

                //        case CURL_RTSPREQ_TEARDOWN:
                //          rtspreq = RTSPREQ_TEARDOWN;
                //          break;

                //        case CURL_RTSPREQ_GET_PARAMETER:
                //          rtspreq = RTSPREQ_GET_PARAMETER;
                //          break;

                //        case CURL_RTSPREQ_SET_PARAMETER:
                //          rtspreq = RTSPREQ_SET_PARAMETER;
                //          break;

                //        case CURL_RTSPREQ_RECORD:
                //          rtspreq = RTSPREQ_RECORD;
                //          break;

                //        case CURL_RTSPREQ_RECEIVE:
                //          rtspreq = RTSPREQ_RECEIVE;
                //          break;
                //        default:
                //          rtspreq = RTSPREQ_NONE;
                //      }

                //      data->set.rtspreq = rtspreq;
                //    break;
                //    }


                //  case CURLOPT_RTSP_SESSION_ID:
                //    /*
                //     * Set the RTSP Session ID manually. Useful if the application is
                //     * resuming a previously established RTSP session
                //     */
                //    result = setstropt(&data->set.str[STRING_RTSP_SESSION_ID],
                //                       va_arg(param, char *));
                //    break;

                //  case CURLOPT_RTSP_STREAM_URI:
                //    /*
                //     * Set the Stream URI for the RTSP request. Unless the request is
                //     * for generic server options, the application will need to set this.
                //     */
                //    result = setstropt(&data->set.str[STRING_RTSP_STREAM_URI],
                //                       va_arg(param, char *));
                //    break;

                //  case CURLOPT_RTSP_TRANSPORT:
                //    /*
                //     * The content of the Transport: header for the RTSP request
                //     */
                //    result = setstropt(&data->set.str[STRING_RTSP_TRANSPORT],
                //                       va_arg(param, char *));
                //    break;

                //  case CURLOPT_RTSP_CLIENT_CSEQ:
                //    /*
                //     * Set the CSEQ number to issue for the next RTSP request. Useful if the
                //     * application is resuming a previously broken connection. The CSEQ
                //     * will increment from this new number henceforth.
                //     */
                //    data->state.rtsp_next_client_CSeq = va_arg(param, long);
                //    break;

                //  case CURLOPT_RTSP_SERVER_CSEQ:
                //    /* Same as the above, but for server-initiated requests */
                //    data->state.rtsp_next_client_CSeq = va_arg(param, long);
                //    break;

                //  case CURLOPT_INTERLEAVEDATA:
                //    data->set.rtp_out = va_arg(param, void *);
                //    break;
                //  case CURLOPT_INTERLEAVEFUNCTION:
                //    /* Set the user defined RTP write function */
                //    data->set.fwrite_rtp = va_arg(param, curl_write_callback);
                //    break;

                //  case CURLOPT_WILDCARDMATCH:
                //    data->set.wildcardmatch = (bool)(0 != va_arg(param, long));
                //    break;
                //  case CURLOPT_CHUNK_BGN_FUNCTION:
                //    data->set.chunk_bgn = va_arg(param, curl_chunk_bgn_callback);
                //    break;
                //  case CURLOPT_CHUNK_END_FUNCTION:
                //    data->set.chunk_end = va_arg(param, curl_chunk_end_callback);
                //    break;
                //  case CURLOPT_FNMATCH_FUNCTION:
                //    data->set.fnmatch = va_arg(param, curl_fnmatch_callback);
                //    break;
                //  case CURLOPT_CHUNK_DATA:
                //    data->wildcard.customptr = va_arg(param, void *);
                //    break;
                //  case CURLOPT_FNMATCH_DATA:
                //    data->set.fnmatch_data = va_arg(param, void *);
                //    break;
                //#ifdef USE_TLS_SRP
                //  case CURLOPT_TLSAUTH_USERNAME:
                //    result = setstropt(&data->set.str[STRING_TLSAUTH_USERNAME],
                //                       va_arg(param, char *));
                //    if (data->set.str[STRING_TLSAUTH_USERNAME] && !data->set.ssl.authtype)
                //      data->set.ssl.authtype = CURL_TLSAUTH_SRP; /* default to SRP */
                //    break;
                //  case CURLOPT_TLSAUTH_PASSWORD:
                //    result = setstropt(&data->set.str[STRING_TLSAUTH_PASSWORD],
                //                       va_arg(param, char *));
                //    if (data->set.str[STRING_TLSAUTH_USERNAME] && !data->set.ssl.authtype)
                //      data->set.ssl.authtype = CURL_TLSAUTH_SRP; /* default to SRP */
                //    break;
                //  case CURLOPT_TLSAUTH_TYPE:
                //    if (strncmp((char *)va_arg(param, char *), "SRP", strlen("SRP")) == 0)
                //      data->set.ssl.authtype = CURL_TLSAUTH_SRP;
                //    else
                //      data->set.ssl.authtype = CURL_TLSAUTH_NONE;
                //    break;
                //#endif

                #endregion

                default:
                    /* unknown tag and its companion, just ignore: */
                    //result = CURLE_UNKNOWN_OPTION;
                    PhpException.ArgumentValueNotSupported("option", option.ToString());
                    break;
            }

            return result;
        }

        private static void setstropt_userpwd(object value, ref object userName, ref object password)
        {
            string strOption = PhpVariable.AsString(value);

            if (strOption == null)
            {
                /* we treat a NULL passed in as a hint to clear existing info */
                userName = null;
                password = null;

                return;
            }

            string[] parts = strOption.Split(':');
            if (parts.Length > 1)
            {
                /* store username part of option */
                userName = parts[0];
                /* store password part of option */
                password = parts[1];
            }
        }

    };

    #region Other curl structures (not used)

    /*
     * The 'connectdata' struct MUST have all the connection oriented stuff as we
     * may have several simultaneous connections and connection structs in memory.
     *
     * The 'struct UserDefined' must only contain data that is set once to go for
     * many (perhaps) independent connections. Values that are generated or
     * calculated internally for the "session handle" must be defined within the
     * 'struct UrlState' instead.
     */
    //internal struct SessionHandle
    //{
    //    //struct Names dns;
    //    //struct Curl_multi *multi;    /* if non-NULL, points to the multi handle
    //    //                                struct to which this "belongs" */
    //    //struct Curl_one_easy *multi_pos; /* if non-NULL, points to its position
    //    //                                    in multi controlling structure to assist
    //    //                                    in removal. */
    //    //struct Curl_share *share;    /* Share, handles global variable mutexing */
    //    //struct SingleRequest req;    /* Request-specific data */
    //    public UserDefined set;      /* values set by the libcurl user */
    //    //  struct DynamicStatic change; /* possibly modified userdefined data */
    //    //  struct CookieInfo *cookies;  /* the cookies, read from files and servers.
    //    //                                  NOTE that the 'cookie' field in the
    //    //                                  UserDefined struct defines if the "engine"
    //    //                                  is to be used or not. */
    //    //  struct Progress progress;    /* for all the progress meter data */
    //    //  struct UrlState state;       /* struct for fields used for state info and
    //    //                                  other dynamic purposes */
    //    //  struct WildcardData wildcard; /* wildcard download state info */
    //    //  struct PureInfo info;        /* stats, reports and info data */
    //    //#if CURL_DOES_CONVERSIONS && HAVE_ICONV
    //    //  iconv_t outbound_cd;         /* for translating to the network encoding */
    //    //  iconv_t inbound_cd;          /* for translating from the network encoding */
    //    //  iconv_t utf8_cd;             /* for translating to UTF8 */
    //    //#endif /* CURL_DOES_CONVERSIONS && HAVE_ICONV */
    //    //  unsigned int magic;          /* set to a CURLEASY_MAGIC_NUMBER */
    //};




    /// <summary>
    /// The connectdata struct contains all fields and variables that should be
    /// unique for an entire connection.
    /// </summary>
    //internal struct ConnectData
    //{
    //    /// <summary>
    //    /// 'data' is the CURRENT SessionHandle using this connection -- take great
    //    ///  caution that this might very well vary between different times this
    //    ///  connection is used!
    //    /// </summary>
    //    public SessionHandle data;

    //    //  /* chunk is for HTTP chunked encoding, but is in the general connectdata
    //    //     struct only because we can do just about any protocol through a HTTP proxy
    //    //     and a HTTP proxy may in fact respond using chunked encoding */
    //    //  struct Curl_chunker chunk;

    //    //  bool inuse; /* This is a marker for the connection cache logic. If this is
    //    //                 TRUE this handle is being used by an easy handle and cannot
    //    //                 be used by any other easy handle without careful
    //    //                 consideration (== only for pipelining). */

    //    //  /**** Fields set when inited and not modified again */
    //    //  long connectindex; /* what index in the connection cache connects index this
    //    //                        particular struct has */

    //    //  /* 'dns_entry' is the particular host we use. This points to an entry in the
    //    //     DNS cache and it will not get pruned while locked. It gets unlocked in
    //    //     Curl_done(). This entry will be NULL if the connection is re-used as then
    //    //     there is no name resolve done. */
    //    //  struct Curl_dns_entry *dns_entry;

    //    //  /* 'ip_addr' is the particular IP we connected to. It points to a struct
    //    //     within the DNS cache, so this pointer is only valid as long as the DNS
    //    //     cache entry remains locked. It gets unlocked in Curl_done() */
    //    //  Curl_addrinfo *ip_addr;

    //    //  /* 'ip_addr_str' is the ip_addr data as a human readable string.
    //    //     It remains available as long as the connection does, which is longer than
    //    //     the ip_addr itself. */
    //    //  char ip_addr_str[MAX_IPADR_LEN];

    //    //  unsigned int scope;    /* address scope for IPv6 */

    //    //  int socktype;  /* SOCK_STREAM or SOCK_DGRAM */

    //    //  struct hostname host;
    //    //  struct hostname proxy;

    //    /// <summary>
    //    /// which port to use locally
    //    /// </summary>
    //    public long port;

    //    /// <summary>
    //    /// what remote port to connect to,
    //    /// not the proxy port!
    //    /// </summary>
    //    public ushort remote_port;

    //    //  /* 'primary_ip' and 'primary_port' get filled with peer's numerical
    //    //     ip address and port number whenever an outgoing connection is
    //    //     *attemted* from the primary socket to a remote address. When more
    //    //     than one address is tried for a connection these will hold data
    //    //     for the last attempt. When the connection is actualy established
    //    //     these are updated with data which comes directly from the socket. */

    //    //  char primary_ip[MAX_IPADR_LEN];
    //    //  long primary_port;

    //    //  /* 'local_ip' and 'local_port' get filled with local's numerical
    //    //     ip address and port number whenever an outgoing connection is
    //    //     **established** from the primary socket to a remote address. */

    //    //  char local_ip[MAX_IPADR_LEN];
    //    //  long local_port;

    //    //  char *user;    /* user name string, allocated */
    //    //  char *passwd;  /* password string, allocated */

    //    //  char *proxyuser;    /* proxy user name string, allocated */
    //    //  char *proxypasswd;  /* proxy password string, allocated */
    //    //  curl_proxytype proxytype; /* what kind of proxy that is in use */

    //    //  int httpversion;        /* the HTTP version*10 reported by the server */
    //    //  int rtspversion;        /* the RTSP version*10 reported by the server */

    //    //  struct timeval now;     /* "current" time */
    //    //  struct timeval created; /* creation time */
    //    //  curl_socket_t sock[2]; /* two sockets, the second is used for the data
    //    //                            transfer when doing FTP */

    //    //  Curl_recv *recv[2];
    //    //  Curl_send *send[2];

    //    //  struct ssl_connect_data ssl[2]; /* this is for ssl-stuff */
    //    //  struct ssl_config_data ssl_config;

    //    //  struct ConnectBits bits;    /* various state-flags for this connection */

    //    // /* connecttime: when connect() is called on the current IP address. Used to
    //    //    be able to track when to move on to try next IP - but only when the multi
    //    //    interface is used. */
    //    //  struct timeval connecttime;
    //    //  /* The two fields below get set in Curl_connecthost */
    //    //  int num_addr; /* number of addresses to try to connect to */
    //    //  long timeoutms_per_addr; /* how long time in milliseconds to spend on
    //    //                              trying to connect to each IP address */

    //    /// <summary>
    //    /// Connection's protocol handler
    //    /// </summary>
    //    public CurlHandler handler;
    //    //  const struct Curl_handler *given;   /* The protocol first given */

    //    //  long ip_version; /* copied from the SessionHandle at creation time */

    //    //  /**** curl_get() phase fields */

    //    //  curl_socket_t sockfd;   /* socket to read from or CURL_SOCKET_BAD */
    //    //  curl_socket_t writesockfd; /* socket to write to, it may very
    //    //                                well be the same we read from.
    //    //                                CURL_SOCKET_BAD disables */

    //    //  /** Dynamicly allocated strings, may need to be freed before this **/
    //    //  /** struct is killed.                                             **/
    //    //  struct dynamically_allocated_data {
    //    //    char *proxyuserpwd; /* free later if not NULL! */
    //    //    char *uagent; /* free later if not NULL! */
    //    //    char *accept_encoding; /* free later if not NULL! */
    //    //    char *userpwd; /* free later if not NULL! */
    //    //    char *rangeline; /* free later if not NULL! */
    //    //    char *ref; /* free later if not NULL! */
    //    //    char *host; /* free later if not NULL */
    //    //    char *cookiehost; /* free later if not NULL */
    //    //    char *rtsp_transport; /* free later if not NULL */
    //    //  } allocptr;

    //    //  int sec_complete; /* if kerberos is enabled for this connection */
    //    //#if defined(HAVE_KRB4) || defined(HAVE_GSSAPI)
    //    //  enum protection_level command_prot;
    //    //  enum protection_level data_prot;
    //    //  enum protection_level request_data_prot;
    //    //  size_t buffer_size;
    //    //  struct krb4buffer in_buffer;
    //    //  void *app_data;
    //    //  const struct Curl_sec_client_mech *mech;
    //    //  struct sockaddr_in local_addr;
    //    //#endif

    //    //  /* the two following *_inuse fields are only flags, not counters in any way.
    //    //     If TRUE it means the channel is in use, and if FALSE it means the channel
    //    //     is up for grabs by one. */

    //    //  bool readchannel_inuse;  /* whether the read channel is in use by an easy
    //    //                              handle */
    //    //  bool writechannel_inuse; /* whether the write channel is in use by an easy
    //    //                              handle */
    //    //  bool server_supports_pipelining; /* TRUE if server supports pipelining,
    //    //                                      set after first response */

    //    //  struct curl_llist *send_pipe; /* List of handles waiting to
    //    //                                   send on this pipeline */
    //    //  struct curl_llist *recv_pipe; /* List of handles waiting to read
    //    //                                   their responses on this pipeline */
    //    //  struct curl_llist *pend_pipe; /* List of pending handles on
    //    //                                   this pipeline */
    //    //  struct curl_llist *done_pipe; /* Handles that are finished, but
    //    //                                   still reference this connectdata */
    //    //#define MAX_PIPELINE_LENGTH 5

    //    //  char* master_buffer; /* The master buffer allocated on-demand;
    //    //                          used for pipelining. */
    //    //  size_t read_pos; /* Current read position in the master buffer */
    //    //  size_t buf_len; /* Length of the buffer?? */


    //    //  curl_seek_callback seek_func; /* function that seeks the input */
    //    //  void *seek_client;            /* pointer to pass to the seek() above */

    //    //  /*************** Request - specific items ************/

    //    //  /* previously this was in the urldata struct */
    //    //  curl_read_callback fread_func; /* function that reads the input */
    //    //  void *fread_in;           /* pointer to pass to the fread() above */

    //    //  struct ntlmdata ntlm;     /* NTLM differs from other authentication schemes
    //    //                               because it authenticates connections, not
    //    //                               single requests! */
    //    //  struct ntlmdata proxyntlm; /* NTLM data for proxy */

    //    //  char syserr_buf [256]; /* buffer for Curl_strerror() */

    //    //#ifdef CURLRES_ASYNCH
    //    //  /* data used for the asynch name resolve callback */
    //    //  struct Curl_async async;
    //    //#endif

    //    //  /* These three are used for chunked-encoding trailer support */
    //    //  char *trailer; /* allocated buffer to store trailer in */
    //    //  int trlMax;    /* allocated buffer size */
    //    //  int trlPos;    /* index of where to store data */

    //    //  union {
    //    //    struct ftp_conn ftpc;
    //    //    struct ssh_conn sshc;
    //    //    struct tftp_state_data *tftpc;
    //    //    struct imap_conn imapc;
    //    //    struct pop3_conn pop3c;
    //    //    struct smtp_conn smtpc;
    //    //    struct rtsp_conn rtspc;
    //    //    void *generic;
    //    //  } proto;

    //    //  int cselect_bits; /* bitmask of socket events */
    //    //  int waitfor;      /* current READ/WRITE bits to wait for */

    //    //#if defined(HAVE_GSSAPI) || defined(USE_WINDOWS_SSPI)
    //    //  int socks5_gssapi_enctype;
    //    //#endif

    //    //  long verifypeer;
    //    //  long verifyhost;

    //    //  /* When this connection is created, store the conditions for the local end
    //    //     bind. This is stored before the actual bind and before any connection is
    //    //     made and will serve the purpose of being used for comparison reasons so
    //    //     that subsequent bound-requested connections aren't accidentally re-using
    //    //     wrong connections. */
    //    //  char *localdev;
    //    //  unsigned short localport;
    //    //  int localportrange;

    //};

    #endregion


    class old
    {
    }


}
