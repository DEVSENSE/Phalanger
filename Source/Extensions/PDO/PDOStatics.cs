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
    }
}
