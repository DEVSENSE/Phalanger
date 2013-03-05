using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PHP.Library.Curl
{
    /// <summary>
    /// Default ports
    /// </summary>
    internal enum Port
    {     
        FTP 	= 21,
        FTPS 	= 990,
        TELNET 	= 23,
        HTTP 	= 80,
        HTTPS 	= 443,
        DICT 	= 2628,
        LDAP 	= 389,
        LDAPS 	= 636,
        TFTP 	= 69,
        SSH 	= 22,
        IMAP 	= 143,
        IMAPS 	= 993,
        POP3 	= 110,
        POP3S 	= 995,
        SMTP 	= 25,
        SMTPS 	= 465, /* sometimes called SSMTP */
        RTSP 	= 554,
        RTMP 	= 1935,
        RTMPT   = HTTP,
        RTMPS   = HTTPS,
        GOPHER 	= 70
    }
}
