using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PHP.Core;

namespace PHP.Library.Data
{
    public abstract class PDODriver
    {
        public abstract string Scheme { get; }
        public abstract PDOConnection OpenConnection(ScriptContext context, string dsn_data, string username, string password, object argdriver_options);
        public abstract object Quote(ScriptContext context, object strobj, PDOStatics.pdo_param_type param_type);
    }
}
