using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PHP.Core;

namespace PHP.Library.Data
{
    partial class PDO
    {
        #region param_type
        [ImplementsConstant("PARAM_NULL")]
        public const int PARAM_NULL = 0;
        [ImplementsConstant("PARAM_INT")]
        public const int PARAM_INT = 1;
        [ImplementsConstant("PARAM_STR")]
        public const int PARAM_STR = 2;
        [ImplementsConstant("PARAM_LOB")]
        public const int PARAM_LOB = 3;
        [ImplementsConstant("PARAM_STMT")]
        public const int PARAM_STMT = 4;
        [ImplementsConstant("PARAM_BOOL")]
        public const int PARAM_BOOL = 5;
        #endregion

        #region fetch_type
        [ImplementsConstant("FETCH_USE_DEFAULT")]
        public const int FETCH_USE_DEFAULT = 0;
        [ImplementsConstant("FETCH_LAZY")]
        public const int FETCH_LAZY = 1;
        [ImplementsConstant("FETCH_ASSOC")]
        public const int FETCH_ASSOC = 2;
        [ImplementsConstant("FETCH_NUM")]
        public const int FETCH_NUM = 3;
        [ImplementsConstant("FETCH_BOTH")]
        public const int FETCH_BOTH = 4;
        [ImplementsConstant("FETCH_OBJ")]
        public const int FETCH_OBJ = 5;
        [ImplementsConstant("FETCH_BOUND")]
        public const int FETCH_BOUND = 6; /* return true/false only; rely on bound columns */
        [ImplementsConstant("FETCH_COLUMN")]
        public const int FETCH_COLUMN = 7;	/* fetch a numbered column only */
        [ImplementsConstant("FETCH_CLASS")]
        public const int FETCH_CLASS = 8;	/* create an instance of named class, call ctor and set properties */
        [ImplementsConstant("FETCH_INTO")]
        public const int FETCH_INTO = 9;		/* fetch row into an existing object */
        [ImplementsConstant("FETCH_FUNC")]
        public const int FETCH_FUNC = 10;		/* fetch into function and return its result */
        [ImplementsConstant("FETCH_NAMED")]
        public const int FETCH_NAMED = 11;    /* like FETCH_ASSOC, but can handle duplicate names */
        [ImplementsConstant("FETCH_KEY_PAIR")]
        public const int FETCH_KEY_PAIR = 12;	/* fetch into an array where the 1st column is a key and all subsequent columns are values */
        #endregion

        #region pdo_attribute_type
        [ImplementsConstant("ATTR_AUTOCOMMIT")]
        public const int ATTR_AUTOCOMMIT = 0;	/* use to turn on or off auto-commit mode */
        [ImplementsConstant("ATTR_PREFETCH")]
        public const int ATTR_PREFETCH = 1;		/* configure the prefetch size for drivers that support it. Size is in KB */
        [ImplementsConstant("ATTR_TIMEOUT")]
        public const int ATTR_TIMEOUT = 2;		/* connection timeout in seconds */
        [ImplementsConstant("ATTR_ERRMODE")]
        public const int ATTR_ERRMODE = 3;		/* control how errors are handled */
        [ImplementsConstant("ATTR_SERVER_VERSION")]
        public const int ATTR_SERVER_VERSION = 4;	/* database server version */
        [ImplementsConstant("ATTR_CLIENT_VERSION")]
        public const int ATTR_CLIENT_VERSION = 5;	/* client library version */
        [ImplementsConstant("ATTR_SERVER_INFO")]
        public const int ATTR_SERVER_INFO = 6;		/* server information */
        [ImplementsConstant("ATTR_CONNECTION_STATUS")]
        public const int ATTR_CONNECTION_STATUS = 7;	/* connection status */
        [ImplementsConstant("ATTR_CASE")]
        public const int ATTR_CASE = 8;				/* control case folding for portability */
        [ImplementsConstant("ATTR_CURSOR_NAME")]
        public const int ATTR_CURSOR_NAME = 9;		/* name a cursor for use in "WHERE CURRENT OF <name>" */
        [ImplementsConstant("ATTR_CURSOR")]
        public const int ATTR_CURSOR = 10;			/* cursor type */
        [ImplementsConstant("ATTR_ORACLE_NULLS")]
        public const int ATTR_ORACLE_NULLS = 11;		/* convert empty strings to NULL */
        [ImplementsConstant("ATTR_PERSISTENT")]
        public const int ATTR_PERSISTENT = 12;		/* pconnect style connection */
        [ImplementsConstant("ATTR_STATEMENT_CLASS")]
        public const int ATTR_STATEMENT_CLASS = 13;	/* array(classname=1; array(ctor_args)) to specify the class of the constructed statement */
        [ImplementsConstant("ATTR_FETCH_TABLE_NAMES")]
        public const int ATTR_FETCH_TABLE_NAMES = 14; /* include table names in the column names=1; where available */
        [ImplementsConstant("ATTR_FETCH_CATALOG_NAMES")]
        public const int ATTR_FETCH_CATALOG_NAMES = 15; /* include the catalog/db name names in the column names=1; where available */
        [ImplementsConstant("ATTR_DRIVER_NAME")]
        public const int ATTR_DRIVER_NAME = 16;		  /* name of the driver (as used in the constructor) */
        [ImplementsConstant("ATTR_STRINGIFY_FETCHES")]
        public const int ATTR_STRINGIFY_FETCHES = 17;	/* converts integer/float types to strings during fetch */
        [ImplementsConstant("ATTR_MAX_COLUMN_LEN")]
        public const int ATTR_MAX_COLUMN_LEN = 18;	/* make database calculate maximum length of data found in a column */
        [ImplementsConstant("ATTR_DEFAULT_FETCH_MODE")]
        public const int ATTR_DEFAULT_FETCH_MODE = 19; /* Set the default fetch mode */
        [ImplementsConstant("ATTR_EMULATE_PREPARES")]
        public const int ATTR_EMULATE_PREPARES = 20;  /* use query emulation rather than native */

        /* this defines the start of the range for driver specific options.
         * Drivers should define their own attribute constants beginning with this
         * value. */
        [ImplementsConstant("ATTR_DRIVER_SPECIFIC")]
        public const int ATTR_DRIVER_SPECIFIC = 1000;
        #endregion

        #region pdo_error_mode
        [ImplementsConstant("ERRMODE_SILENT")]
        public const int ERRMODE_SILENT = 0;		/* just set error codes */
        [ImplementsConstant("ERRMODE_WARNING")]
        public const int ERRMODE_WARNING = 1;	/* raise E_WARNING */
        [ImplementsConstant("ERRMODE_EXCEPTION")]
        public const int ERRMODE_EXCEPTION = 2;	/* throw exceptions */
        #endregion

        #region pdo_case_conversion
        [ImplementsConstant("CASE_NATURAL")]
        public const int CASE_NATURAL = 0;
        [ImplementsConstant("CASE_UPPER")]
        public const int CASE_UPPER = 1;
        [ImplementsConstant("CASE_LOWER")]
        public const int CASE_LOWER = 2;
        #endregion

        #region pdo_null_handling
        [ImplementsConstant("NULL_NATURAL")]
        public const int NULL_NATURAL = 0;
        [ImplementsConstant("NULL_EMPTY_STRING")]
        public const int NULL_EMPTY_STRING = 1;
        [ImplementsConstant("NULL_TO_STRING")]
        public const int NULL_TO_STRING = 2;
        #endregion

        #region pdo_cursor_type
        [ImplementsConstant("CURSOR_FWDONLY")]
        public const int CURSOR_FWDONLY = 0;		/* forward only cursor (default) */
        [ImplementsConstant("CURSOR_SCROLL")]
        public const int CURSOR_SCROLL = 1;		/* scrollable cursor */
        #endregion

        #region MySQL

        [ImplementsConstant("MYSQL_ATTR_INIT_COMMAND")]
        public const int MYSQL_ATTR_INIT_COMMAND = 1002;

        [ImplementsConstant("MYSQL_ATTR_USE_BUFFERED_QUERY")]
        public const int MYSQL_ATTR_USE_BUFFERED_QUERY = 1000;

        #endregion
    }
}
