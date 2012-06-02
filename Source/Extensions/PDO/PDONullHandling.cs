using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PHP.Core;

namespace PHP.Library.Data
{
    public enum PDONullHandling
    {
        [ImplementsConstant("PDO_NULL_NATURAL")]
        PDO_NULL_NATURAL = 0,
        [ImplementsConstant("PDO_NULL_EMPTY_STRING")]
        PDO_NULL_EMPTY_STRING = 1,
        [ImplementsConstant("PDO_NULL_TO_STRING")]
        PDO_NULL_TO_STRING = 2,
    }
}
