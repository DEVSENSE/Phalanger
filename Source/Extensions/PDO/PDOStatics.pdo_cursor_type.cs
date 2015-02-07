﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PHP.Core;

namespace PHP.Library.Data
{
    partial class PDOStatics
    {
        public enum pdo_cursor_type
        {
            [ImplementsConstant("PDO_CURSOR_FWDONLY")]
            PDO_CURSOR_FWDONLY = 0,		/* forward only cursor (default) */
            [ImplementsConstant("PDO_CURSOR_SCROLL")]
            PDO_CURSOR_SCROLL = 1,		/* scrollable cursor */
        };
    }
}
