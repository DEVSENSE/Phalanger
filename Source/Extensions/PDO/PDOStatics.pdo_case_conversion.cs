using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PHP.Core;

namespace PHP.Library.Data
{
    partial class PDOStatics
    {
        public enum pdo_case_conversion
        {
            [ImplementsConstant("PDO_CASE_NATURAL")]
            PDO_CASE_NATURAL,
            [ImplementsConstant("PDO_CASE_UPPER")]
            PDO_CASE_UPPER,
            [ImplementsConstant("PDO_CASE_LOWER")]
            PDO_CASE_LOWER
        };
    }
}
