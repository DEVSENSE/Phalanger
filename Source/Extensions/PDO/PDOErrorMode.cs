using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PHP.Core;

namespace PHP.Library.Data
{
    public enum PDOErrorMode
    {
        [ImplementsConstant("PDO_ERRMODE_SILENT")]
        PDO_ERRMODE_SILENT,		/* just set error codes */
        [ImplementsConstant("PDO_ERRMODE_WARNING")]
        PDO_ERRMODE_WARNING,	/* raise E_WARNING */
        [ImplementsConstant("PDO_ERRMODE_EXCEPTION")]
        PDO_ERRMODE_EXCEPTION	/* throw exceptions */
    }
}
