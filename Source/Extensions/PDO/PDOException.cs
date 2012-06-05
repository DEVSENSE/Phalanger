using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PHP.Core;
using PHP.Library.SPL;

namespace PHP.Library.Data
{
    [ImplementsType]
    public class PDOException : RuntimeException
    {
        private PhpArray m_errorInfo;

        private PDOException(ScriptContext context, PhpArray errorInfo)
            : base(context, true)
        {
        }

        [PhpVisible]
        public PhpArray errorInfo { get { return this.m_errorInfo; } }

        public static void Throw(ScriptContext context, string message, PhpArray errorInfo, object code, object previous)
        {
            PHP.Library.SPL.Exception.ThrowSplException(ctx => new PDOException(ctx, errorInfo), context, message, code, previous);
        }
    }
}
