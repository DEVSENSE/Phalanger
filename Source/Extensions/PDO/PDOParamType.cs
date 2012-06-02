using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PHP.Core;

namespace PHP.Library.Data
{
    public enum PDOParamType : int
    {
        [ImplementsConstant("PDO_PARAM_NULL")]
        PDO_PARAM_NULL,

        /// <summary>
        /// int as in long (the php native int type).
        /// If you mark a column as an int, PDO expects get_col to return
        /// a pointer to a long
        /// </summary>
        [ImplementsConstant("PDO_PARAM_INT")]
        PDO_PARAM_INT,

        /* get_col ptr should point to start of the string buffer */
        [ImplementsConstant("PDO_PARAM_STR")]
        PDO_PARAM_STR,

        /* get_col: when len is 0 ptr should point to a php_stream *,
         * otherwise it should behave like a string. Indicate a NULL field
         * value by setting the ptr to NULL */
        [ImplementsConstant("PDO_PARAM_LOB")]
        PDO_PARAM_LOB,

        /* get_col: will expect the ptr to point to a new PDOStatement object handle,
         * but this isn't wired up yet */
        [ImplementsConstant("PDO_PARAM_STMT")]
        PDO_PARAM_STMT, /* hierarchical result set */

        /* get_col ptr should point to a zend_bool */
        [ImplementsConstant("PDO_PARAM_BOOL")]
        PDO_PARAM_BOOL
    }
}
