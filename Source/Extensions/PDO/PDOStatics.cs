using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PHP.Core;

namespace PHP.Library.Data
{
    public static partial class PDOStatics
    {
        //
        [ImplementsFunction("pdo_drivers")]
        public static string[] Drivers()
        {
            return PDOLibraryDescriptor.GetDrivers();
        }

        public static PhpArray BuildErrorInfo(string sqlstate, object driver_error, string message)
        {
            PhpArray arr = new PhpArray();
            arr.Add(0, sqlstate);
            arr.Add(1, driver_error);
            arr.Add(2, message);
            return arr;
        }
    }
}
