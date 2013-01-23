using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PHP.Core;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace PHP.Library.Data
{
    partial class PDOStatement
    {
        [ImplementsConstant("FETCH_ORI_NEXT")]
        public const int FETCH_ORI_NEXT = 0;
        [ImplementsConstant("FETCH_ORI_PRIOR")]
        public const int FETCH_ORI_PRIOR = 1;
        [ImplementsConstant("FETCH_ORI_FIRST")]
        public const int FETCH_ORI_FIRST = 2;
        [ImplementsConstant("FETCH_ORI_LAST")]
        public const int FETCH_ORI_LAST = 3;
        [ImplementsConstant("FETCH_ORI_ABS")]
        public const int FETCH_ORI_ABS = 4;
        [ImplementsConstant("FETCH_ORI_REL")]
        public const int FETCH_ORI_REL = 5;
        
        #region setFetchMode
        
        [PhpVisible]
        [ImplementsMethod]
        [return: CastToFalse]
        public virtual object setFetchMode(ScriptContext context, object fetch_to_mode, [Optional]object fetch_to_dest/*=null*/, [Optional]object fetch_to_args/*=null*/)
        {
            return false;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object setFetchMode(object instance, PhpStack stack)
        {
            object fetch_to_mode = stack.PeekValue(1);
            object fetch_to_dest = stack.PeekValueOptional(2);
            object fetch_to_args = stack.PeekValueOptional(3);
            return ((PDOStatement)instance).setFetchMode(stack.Context, fetch_to_mode, fetch_to_dest, fetch_to_args);
        }
        #endregion
    }
}
