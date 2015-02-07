using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PHP.Core;
using System.Data;

namespace PHP.Library.Data
{
    public abstract class PDODriver
    {
        public abstract string Scheme { get; }
        public abstract IDbConnection OpenConnection(ScriptContext context, string dsn_data, string username, string password, object argdriver_options);
        public abstract object Quote(ScriptContext context, object strobj, PDOParamType param_type);

        public virtual string GetParameterName(string name)
        {
            return string.Format("@{0}", name);
        }

        public abstract PDOStatement CreateStatement(ScriptContext context, PDO pdo);

        internal bool IsValidAttributeValue(int att, object value)
        {
            return this.IsValueValidForAttribute(att, value);
        }

        protected abstract bool IsValueValidForAttribute(int att, object value);
    }
}
