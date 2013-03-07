using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PHP.Library.Curl
{
    internal abstract class CurlHandler
    {

        internal static CurlHandler Create(CurlProto protocol)
        {
            switch(protocol)
            {
                case CurlProto.HTTP:
                    return new CurlHttp();
                    
                default:
                    throw new NotSupportedException();
            }
        }



        /// <summary>
        /// URL scheme name.
        /// </summary>
        internal abstract string Scheme
        {
            get;
        }

        internal abstract long DefaultPort
        {
            get;
        }

        internal abstract CurlProto Protocol
        {
            get;
        }


        internal abstract object Execute(PhpCurlResource curl, ref CURLcode result);

        internal abstract object GetInfo(CurlInfo info);

        #region UNSUPPORTED STUFF

        //unsigned int flags;     /* Extra particular characteristics, see PROTOPT_* */

        ///// <summary>
        ///// Complement to setup_connection_internals().
        ///// </summary>
        //CURLcode setup_connection(ConnectData conn);

        //Curl_done_func done;

        /* If the curl_do() function is better made in two halves, this
         * curl_do_more() function will be called afterwards, if set. For example
         * for doing the FTP stuff after the PASV/PORT command.
         */
        //Curl_do_more_func do_more;

        //* This function *MAY* be set to a protocol-dependent function that is run
        // * after the connect() and everything is done, as a step in the connection.
        // * The 'done' pointer points to a bool that should be set to TRUE if the
        // * function completes before return. If it doesn't complete, the caller
        // * should call the curl_connecting() function until it is.
        // */
        //CURLcode connect_it(ConnectData conn, ref bool done);

        /* See above. Currently only used for FTP. */
        //CURLcode (*connecting)(struct connectdata *, bool *done);
        //CURLcode (*doing)(struct connectdata *, bool *done);

        //* Called from the multi interface during the PROTOCONNECT phase, and it
        //   should then return a proper fd set */
        //int (*proto_getsock)(struct connectdata *conn,
        //                     curl_socket_t *socks,
        //                     int numsocks);

        //* Called from the multi interface during the DOING phase, and it should
        //   then return a proper fd set */
        //int (*doing_getsock)(struct connectdata *conn,
        //                     curl_socket_t *socks,
        //                     int numsocks);

        //* Called from the multi interface during the DO_DONE, PERFORM and
        //   WAITPERFORM phases, and it should then return a proper fd set. Not setting
        //   this will make libcurl use the generic default one. */
        //int (*perform_getsock)(const struct connectdata *conn,
        //                       curl_socket_t *socks,
        //                       int numsocks);

        //* This function *MAY* be set to a protocol-dependent function that is run
        // * by the curl_disconnect(), as a step in the disconnection.  If the handler
        // * is called because the connection has been considered dead, dead_connection
        // * is set to TRUE.
        // */
        //CURLcode disconnect(ConnectData conn, bool dead_connection);

        #endregion
    };

}
