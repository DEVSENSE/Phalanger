using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PHP.Core;
using PHP.Library.SPL;
using System.ComponentModel;
using PHP.Core.Reflection;
using System.Runtime.Serialization;

namespace PHP.Library.Data
{
    [ImplementsType]
    public class PDOException : RuntimeException
    {
        #region Implementation Details

        /// <summary>
        /// For internal purposes only.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public PDOException(ScriptContext context, bool newInstance)
            : base(context, newInstance)
        {
        }

        /// <summary>
        /// For internal purposes only.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public PDOException(ScriptContext context, DTypeDesc caller)
            : base(context, caller)
        {
        }

        #endregion

        #region Serialization (CLR only)
#if !SILVERLIGHT

        /// <summary>
        /// Deserializing constructor.
        /// </summary>
        protected PDOException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

#endif
        #endregion

        private PhpArray m_errorInfo;

        private PDOException(ScriptContext context, PhpArray errorInfo)
            : base(context, true)
        {
            this.m_errorInfo = errorInfo;
        }

        [PhpVisible]
        public PhpArray errorInfo { get { return this.m_errorInfo; } }

        public static void Throw(ScriptContext context, string message, PhpArray errorInfo, object code, object previous)
        {
            PHP.Library.SPL.Exception.ThrowSplException(ctx => new PDOException(ctx, errorInfo), context, message, code, previous);
        }
    }
}
