using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PHP.Core;

namespace PHP.Library.Data
{
    partial class SQLite
    {
        /// <summary>
        /// Query result array format.
        /// </summary>
        [Flags]
        public enum QueryResultKeys
        {
            /// <summary>
            /// Add items keyed by column names.
            /// </summary>
            [ImplementsConstant("SQLITE_ASSOC")]
            ColumnNames = 1,

            /// <summary>
            /// Add items keyed by column indices.
            /// </summary>
            [ImplementsConstant("SQLITE_NUM")]
            Numbers = 2,

            /// <summary>
            /// Add both items keyed by column names and items keyd by column indices.
            /// </summary>
            [ImplementsConstant("SQLITE_BOTH")]
            Both = Numbers | ColumnNames
        }
    }
}
