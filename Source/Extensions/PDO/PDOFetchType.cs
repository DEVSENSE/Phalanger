using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PHP.Core;

namespace PHP.Library.Data
{
    public enum PDOFetchType
    {
        [ImplementsConstant("PDO_FETCH_USE_DEFAULT")]
        PDO_FETCH_USE_DEFAULT = PDO.FETCH_USE_DEFAULT,
        [ImplementsConstant("PDO_FETCH_LAZY")]
        PDO_FETCH_LAZY = PDO.FETCH_LAZY,
        [ImplementsConstant("PDO_FETCH_ASSOC")]
        PDO_FETCH_ASSOC = PDO.FETCH_ASSOC,
        [ImplementsConstant("PDO_FETCH_NUM")]
        PDO_FETCH_NUM = PDO.FETCH_NUM,
        [ImplementsConstant("PDO_FETCH_BOTH")]
        PDO_FETCH_BOTH = PDO.FETCH_BOTH,
        [ImplementsConstant("PDO_FETCH_OBJ")]
        PDO_FETCH_OBJ = PDO.FETCH_OBJ,
        [ImplementsConstant("PDO_FETCH_BOUND")]
        PDO_FETCH_BOUND = PDO.FETCH_BOUND, /* return true/false only; rely on bound columns */
        [ImplementsConstant("PDO_FETCH_COLUMN")]
        PDO_FETCH_COLUMN = PDO.FETCH_COLUMN,	/* fetch a numbered column only */
        [ImplementsConstant("PDO_FETCH_CLASS")]
        PDO_FETCH_CLASS = PDO.FETCH_CLASS,	/* create an instance of named class, call ctor and set properties */
        [ImplementsConstant("PDO_FETCH_INTO")]
        PDO_FETCH_INTO = PDO.FETCH_INTO,		/* fetch row into an existing object */
        [ImplementsConstant("PDO_FETCH_FUNC")]
        PDO_FETCH_FUNC = PDO.FETCH_FUNC,		/* fetch into function and return its result */
        [ImplementsConstant("PDO_FETCH_NAMED")]
        PDO_FETCH_NAMED = PDO.FETCH_NAMED,    /* like PDO_FETCH_ASSOC, but can handle duplicate names */
        [ImplementsConstant("PDO_FETCH_KEY_PAIR")]
        PDO_FETCH_KEY_PAIR = PDO.FETCH_KEY_PAIR,	/* fetch into an array where the 1st column is a key and all subsequent columns are values */
        [ImplementsConstant("PDO_FETCH__MAX")]
        PDO_FETCH__MAX /* must be last */
    }
}
