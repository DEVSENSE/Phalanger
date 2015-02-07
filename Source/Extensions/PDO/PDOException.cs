using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PHP.Core;

namespace PHP.Library.Data
{
    //TODO
    public class PDOException : Exception
    {
        public PDOException(string message)
            : base(message)
        {

        }
    }
}
