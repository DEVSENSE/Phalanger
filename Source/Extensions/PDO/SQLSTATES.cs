using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PHP.Library.Data
{
    /// <summary>
    /// http://developer.mimer.com/documentation/html_92/Mimer_SQL_Mobile_DocSet/App_Return_Codes2.html
    /// </summary>
    public static class SQLSTATES
    {
        /// <summary>
        /// Success
        /// </summary>
        public const string Success = "00000";

        #region Warning
        public const string Warning = "01000";
        public const string Warning_disconnect_error = "01002";
        /// <summary>
        /// null value eliminated in set function
        /// </summary>
        public const string Warning_null = "01003";
        /// <summary>
        /// string data, right truncation
        /// </summary>
        public const string Warning_right_truncation = "01004";
        /// <summary>
        /// insufficient item descriptor areas
        /// </summary>
        public const string Warning_insufficient_descriptor = "01005";
        /// <summary>
        /// privilege not revoked
        /// </summary>
        public const string Warning_privilege_not_revoked = "01006";
        /// <summary>
        /// privilege not granted
        /// </summary>
        public const string Warning_privileges_not_granted = "01007";
        /// <summary>
        /// implicit zero-bit padding
        /// </summary>
        public const string Warning_implicit_padding = "01008";
        /// <summary>
        /// error in row
        /// </summary>
        public const string Warning_error_in_row = "01S01";
        /// <summary>
        /// option value changed
        /// </summary>
        public const string Warning_option_value_changed = "01S02";
        /// <summary>
        /// cancel treated as close
        /// </summary>
        public const string Warning_cancel_as_close = "01S05";
        /// <summary>
        /// attempt to fetch before the result set returned the first rowset
        /// </summary>
        public const string Warning_resultset = "01S06";
        /// <summary>
        /// fractional truncation
        /// </summary>
        public const string Warning_fractionnal_truncation = "01S07";
        #endregion

        /// <summary>
        /// No Data
        /// </summary>
        public const string NoData = "02000";

        //TODO : complete list
        #region Dynamic SQL error
        #endregion
    }
}
