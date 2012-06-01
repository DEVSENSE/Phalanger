using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PHP.Core;

namespace PHP.Library.Data
{
    public enum PDOAttributeType : int
    {
        [ImplementsConstant("PDO_ATTR_AUTOCOMMIT")]
        PDO_ATTR_AUTOCOMMIT,	/* use to turn on or off auto-commit mode */
        [ImplementsConstant("PDO_ATTR_PREFETCH")]
        PDO_ATTR_PREFETCH,		/* configure the prefetch size for drivers that support it. Size is in KB */
        [ImplementsConstant("PDO_ATTR_TIMEOUT")]
        PDO_ATTR_TIMEOUT,		/* connection timeout in seconds */
        [ImplementsConstant("PDO_ATTR_ERRMODE")]
        PDO_ATTR_ERRMODE,		/* control how errors are handled */
        [ImplementsConstant("PDO_ATTR_SERVER_VERSION")]
        PDO_ATTR_SERVER_VERSION,	/* database server version */
        [ImplementsConstant("PDO_ATTR_CLIENT_VERSION")]
        PDO_ATTR_CLIENT_VERSION,	/* client library version */
        [ImplementsConstant("PDO_ATTR_SERVER_INFO")]
        PDO_ATTR_SERVER_INFO,		/* server information */
        [ImplementsConstant("PDO_ATTR_CONNECTION_STATUS")]
        PDO_ATTR_CONNECTION_STATUS,	/* connection status */
        [ImplementsConstant("PDO_ATTR_CASE")]
        PDO_ATTR_CASE,				/* control case folding for portability */
        [ImplementsConstant("PDO_ATTR_CURSOR_NAME")]
        PDO_ATTR_CURSOR_NAME,		/* name a cursor for use in "WHERE CURRENT OF <name>" */
        [ImplementsConstant("PDO_ATTR_CURSOR")]
        PDO_ATTR_CURSOR,			/* cursor type */
        [ImplementsConstant("PDO_ATTR_ORACLE_NULLS")]
        PDO_ATTR_ORACLE_NULLS,		/* convert empty strings to NULL */
        [ImplementsConstant("PDO_ATTR_PERSISTENT")]
        PDO_ATTR_PERSISTENT,		/* pconnect style connection */
        [ImplementsConstant("PDO_ATTR_STATEMENT_CLASS")]
        PDO_ATTR_STATEMENT_CLASS,	/* array(classname, array(ctor_args)) to specify the class of the constructed statement */
        [ImplementsConstant("PDO_ATTR_FETCH_TABLE_NAMES")]
        PDO_ATTR_FETCH_TABLE_NAMES, /* include table names in the column names, where available */
        [ImplementsConstant("PDO_ATTR_FETCH_CATALOG_NAMES")]
        PDO_ATTR_FETCH_CATALOG_NAMES, /* include the catalog/db name names in the column names, where available */
        [ImplementsConstant("PDO_ATTR_DRIVER_NAME")]
        PDO_ATTR_DRIVER_NAME,		  /* name of the driver (as used in the constructor) */
        [ImplementsConstant("PDO_ATTR_STRINGIFY_FETCHES")]
        PDO_ATTR_STRINGIFY_FETCHES,	/* converts integer/float types to strings during fetch */
        [ImplementsConstant("PDO_ATTR_MAX_COLUMN_LEN")]
        PDO_ATTR_MAX_COLUMN_LEN,	/* make database calculate maximum length of data found in a column */
        [ImplementsConstant("PDO_ATTR_DEFAULT_FETCH_MODE")]
        PDO_ATTR_DEFAULT_FETCH_MODE, /* Set the default fetch mode */
        [ImplementsConstant("PDO_ATTR_EMULATE_PREPARES")]
        PDO_ATTR_EMULATE_PREPARES,  /* use query emulation rather than native */

        /* this defines the start of the range for driver specific options.
         * Drivers should define their own attribute constants beginning with this
         * value. */
        [ImplementsConstant("PDO_ATTR_DRIVER_SPECIFIC")]
        PDO_ATTR_DRIVER_SPECIFIC = 1000
    }
}
