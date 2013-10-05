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
using System.Net;
using System.IO;

namespace PHP.Library.Curl
{
    /// <summary>
    /// Implements PHP functions provided by Curl extension.
    /// </summary>
    public static class Curl
    {

        #region curl_close

        /// <summary>
        /// Close a cURL session
        /// </summary> 
        [ImplementsFunction("curl_close")]
        public static void Close(PhpResource ch)
        {
            PhpCurlResource curlHandle = ch as PhpCurlResource;

            if (curlHandle == null)
                return;

            curlHandle.Close();
        }

        #endregion

        #region curl_copy_handle

        /// <summary>
        /// Copy a cURL handle along with all of it's preferences
        /// </summary> 
        [ImplementsFunction("curl_copy_handle")]
        public static PhpResource CopyHandle(PhpResource ch)
        {
            PhpException.FunctionNotSupported(PhpError.Warning);
            return null;
        }

        #endregion

        #region curl_errno

        /// <summary>
        /// Return an integer containing the last error number
        /// </summary> 
        [ImplementsFunction("curl_errno")]
        public static object Errno(PhpResource ch)
        {
            //PhpException.FunctionNotSupported(PhpError.Warning);

            PhpCurlResource curlHandle = ch as PhpCurlResource;

            if (curlHandle == null)
                return null;

            return (int)curlHandle.ErrorCode;

        }

        #endregion

        #region curl_error

        /// <summary>
        /// Return a string contain the last error for the current session
        /// </summary> 
        [ImplementsFunction("curl_error")]
        public static string Error(PhpResource ch)
        {
            PhpCurlResource curlHandle = ch as PhpCurlResource;

            if (curlHandle == null)
                return null;
            
            switch(curlHandle.ErrorCode)
            {
                case CURLcode.CURLE_OPERATION_TIMEOUTED:
                    return "Connection time-out";
                case CURLcode.CURLE_COULDNT_CONNECT:
                    return "couldn't connect to host";
                default:
                    return "";
            }
        }

        #endregion

        #region curl_exec

        /// <summary>
        /// Perform a cURL session
        /// </summary> 
        [ImplementsFunction("curl_exec")]
        public static object Execute(PhpResource ch)
        {
            //PhpException.FunctionNotSupported(PhpError.Warning);

            PhpCurlResource curlHandle = ch as PhpCurlResource;

            if (curlHandle == null)
                return false;

            return curlHandle.Execute();

        }

        #endregion

        #region curl_getinfo

        /// <summary>
        /// Get information regarding a specific transfer
        /// </summary> 
        [ImplementsFunction("curl_getinfo")]
        public static object GetInfo(PhpResource ch)
        {
            PhpException.FunctionNotSupported(PhpError.Warning);
            return null;
        }


        /// <summary>
        /// Get information regarding a specific transfer
        /// </summary> 
        [ImplementsFunction("curl_getinfo")]
        public static object GetInfo(PhpResource ch, CurlInfo option)
        {
            PhpCurlResource curlHandle = ch as PhpCurlResource;

            if (curlHandle == null)
                return false;

            return curlHandle.GetInfo(option);
        }

        #endregion

        #region curl_init

        /// <summary>
        /// Initialize a cURL session
        /// </summary> 
        [ImplementsFunction("curl_init")]
        public static PhpResource Init()
        {
            return new PhpCurlResource();
        }

        /// <summary>
        /// Initialize a cURL session
        /// </summary> 
        [ImplementsFunction("curl_init")]
        public static PhpResource Init(string url)
        {
            return new PhpCurlResource(url);
        }

        #endregion

        #region curl_setopt

        /// <summary>
        /// Set an option for a cURL transfer
        /// </summary> 
        [ImplementsFunction("curl_setopt")]
        public static bool SetOpt(PhpResource ch, CurlOption option, object value)
        {
            PhpCurlResource curlHandle = ch as PhpCurlResource;

            if (curlHandle == null)
                return false;

            bool valid = false;
            value = option.ValidateAndConvert(value,out valid);

            if (valid)
            {
                curlHandle.Data.SetOption(option,value);
                return true;
            }
                //exceptions:
            else if (option == CurlOption.CURLOPT_POSTFIELDS)
            {
                //ValidateAndConvert checks just for string
                // this option can be also array or object

                if (value != null && value.GetType() == typeof(PhpArray))
                {
                    PhpArray arr = (PhpArray)value;
                    var form = CurlForm.Create(arr);
                    curlHandle.Data.SetOption(CurlOption.CURLOPT_HTTPPOST, form);
                }
            }

            return false;
        }


        #endregion

        #region curl_setopt_array

        /// <summary>
        /// Set an array of option for a cURL transfer
        /// </summary> 
        [ImplementsFunction("curl_setopt_array")]
        public static bool SetOptArray(PhpResource ch, PhpArray options)
        {
            var result = true;
            foreach (var item in options)
            {
                result &= SetOpt(ch, (CurlOption) item.Key.Integer, item.Value);
            }
            return result;
        }


        #endregion

        #region curl_version

        /// <summary>
        /// Return cURL version information.
        /// </summary> 
        [ImplementsFunction("curl_version")]
        public static PhpArray Version()
        {
            PhpArray result = new PhpArray();

            PhpArray protocols = new PhpArray(1);
            protocols.Add("http");
            protocols.Add("https");

            // In wordpress
            // >= 7.15.2 needs 
            //   CURLOPT_FOLLOWLOCATION
            //   CURLOPT_MAXREDIRS
            // >= 7.10.5
            //   CURLOPT_ENCODING
            // >= 7.19.4.
            //   CURLOPT_PROTOCOLS

            result.Add("version", "7.21.0"); //(MB) I'll try this for testing purposes
            result.Add("protocols", protocols);



            return result;
        }

        /// <summary>
        /// Return cURL version information.
        /// </summary> 
        [ImplementsFunction("curl_version")]
        public static PhpArray Version(int version)
        {
            return Version();
        }

        #endregion


        #region curl_multi_add_handle

        /// <summary>
        /// Add a normal cURL handle to a cURL multi handle
        /// </summary> 
        [ImplementsFunction("curl_multi_add_handle")]
        public static int MultiAddHandle(PhpResource mh, PhpResource ch)
        {
            var mh1 = mh as PhpCurlMultiResource;
            var ch1 = ch as PhpCurlResource;
            if (mh1 == null || ch1 == null)
                return (int)CURLcode.CURLM_INTERNAL_ERROR;
            mh1.Add(ch1);
            return 0;
        }

        #endregion

        #region curl_multi_close

        /// <summary>
        /// Close a set of cURL handles
        /// </summary> 
        [ImplementsFunction("curl_multi_close")]
        public static void MultiClose(PhpResource mh)
        {
            var mh1 = mh as PhpCurlMultiResource;
            if (mh1 == null)
                return;
            mh1.Close();
        }

        #endregion

        #region curl_multi_exec

        /// <summary>
        /// Run the sub-connections of the current cURL handle
        /// </summary> 
        [ImplementsFunction("curl_multi_exec")]
        public static int MultiExec(PhpResource mh, ref int still_running)
        {
            var mh1 = mh as PhpCurlMultiResource;
            if (mh1 == null)
                return (int) CURLcode.CURLM_INTERNAL_ERROR;
            mh1.StartIfNeeded();
            still_running = mh1.StillRunning;
            return (int)(mh1.SomeResultIsReady ? CURLcode.CURLM_OK : CURLcode.CURLM_CALL_MULTI_PERFORM);
        }

        #endregion

        #region curl_multi_getcontent

        /// <summary>
        /// Return the content of a cURL handle if CURLOPT_RETURNTRANSFER is set
        /// </summary> 
        [ImplementsFunction("curl_multi_getcontent")]
        public static string MultiGetContent(PhpResource ch)
        {
            var ch1 = ch as PhpCurlResource;
            if (ch1 == null)
                return null;
            var mh = ch1.MultiParent;
            if (mh == null)
                return null;
            return mh.GetResult(ch1).ToString();
        }

        #endregion

        #region curl_multi_info_read

        /// <summary>
        /// Get information about the current transfers.
        /// </summary>
        /// <param name="mh">A cURL multi handle returned by curl_multi_init().</param>
        /// <returns>On success, returns an associative array for the message, FALSE on failure.</returns>
        [ImplementsFunction("curl_multi_info_read")]
        public static object MultiInfoRead(PhpResource mh)
        {
            int tmp = 0;
            return MultiInfoRead(mh, ref tmp);
        }

        /// <summary>
        /// Get information about the current transfers
        /// </summary> 
        [ImplementsFunction("curl_multi_info_read")]
        public static object MultiInfoRead(PhpResource mh, ref int msgs_in_queue)
        {
            var mh1 = mh as PhpCurlMultiResource;
            if (mh1 == null)
                return false;
            var handle = mh1.NextCompleted();
            msgs_in_queue = mh1.StillRunning;
            if (handle == null)
                return false;
            var arr = new PhpArray();
            arr["msg"] = (int)CURLcode.CURLMSG_DONE;
            arr["result"] = (int)handle.ErrorCode;
            arr["handle"] = handle;
            return arr;
        }

        #endregion

        #region curl_multi_init

        /// <summary>
        /// Returns a new cURL multi handle
        /// </summary> 
        [ImplementsFunction("curl_multi_init")]
        public static PhpResource MultiInit()
        {
            return new PhpCurlMultiResource();
        }

        #endregion

        #region curl_multi_remove_handle

        /// <summary>
        /// Remove a multi handle from a set of cURL handles
        /// </summary> 
        [ImplementsFunction("curl_multi_remove_handle")]
        public static int MultiRemoveHandle(PhpResource mh, PhpResource ch)
        {
            var mh1 = mh as PhpCurlMultiResource;
            var ch1 = ch as PhpCurlResource;
            if (mh1 == null || ch1 == null)
                return (int)CURLcode.CURLM_INTERNAL_ERROR;
            mh1.Remove(ch1);
            return 0;
        }

        #endregion

        #region curl_multi_select

        /// <summary>
        /// Wait for activity on any curl_multi connection.
        /// </summary>
        /// <param name="mh">A cURL multi handle returned by curl_multi_init().</param>
        /// <returns>On success, returns the number of descriptors contained in the descriptor sets. On failure, this function will return -1 on a select failure or timeout (from the underlying select system call).</returns>
        [ImplementsFunction("curl_multi_select")]
        public static int MultiSelect(PhpResource mh)
        {
            return MultiSelect(mh, 0);
        }

        /// <summary>
        /// Get all the sockets associated with the cURL extension, which can then be "selected"
        /// </summary> 
        [ImplementsFunction("curl_multi_select")]
        public static int MultiSelect(PhpResource mh, double timeout)
        {
            var mh1 = mh as PhpCurlMultiResource;
            if (mh1 == null)
                return -1;
            mh1.WaitAny(timeout == 0 ? TimeSpan.MaxValue : TimeSpan.FromSeconds(timeout));
            return 1;
        }

        #endregion

    }
}