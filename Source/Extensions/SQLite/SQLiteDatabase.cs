using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PHP.Core;
using System.ComponentModel;
using PHP.Core.Reflection;
using System.Runtime.InteropServices;

namespace PHP.Library.Data
{
    [ImplementsType]
    public class SQLiteDatabase : PhpObject
    {
        //#region Internals
        ///// <summary>
        ///// For internal purposes only.
        ///// </summary>
        //[EditorBrowsable(EditorBrowsableState.Never)]
        //public SQLiteDatabase(ScriptContext/*!*/context, bool newInstance)
        //    : base(context, newInstance)
        //{ }

        ///// <summary>
        ///// For internal purposes only.
        ///// </summary>
        //[EditorBrowsable(EditorBrowsableState.Never)]
        //public SQLiteDatabase(ScriptContext/*!*/context, DTypeDesc caller)
        //    : base(context, caller)
        //{ }
        ////#endregion

        ////#region __construct

        public SQLiteDatabase(string filename)
            : base(ScriptContext.CurrentContext, true)
        {
            this.m_filename = filename;
        }

        private string m_filename;
        private PhpResource m_con = null;

        [ImplementsMethod]
        public object __construct(ScriptContext context, string filename, int mode, PhpReference error)
        {
            if (mode == 0)
            {
                mode = SQLite.DEFAULT_FILE_MODE;
            }
            this.m_filename = filename;
            this.m_con = SQLite.Open(this.m_filename, mode, error);
            return null;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object __construct(object instance, PhpStack stack)
        {
            object argFileName = stack.PeekValue(1);
            object argMode = stack.PeekValueOptional(2);
            PhpReference error = stack.PeekReferenceOptional(3);
            stack.RemoveFrame();

            string filename = PHP.Core.Convert.ObjectToString(argFileName);
            int mode = PHP.Core.Convert.ObjectToInteger(argMode);
            return ((SQLiteDatabase)instance).__construct(stack.Context, filename, mode, error);
        }

        [ImplementsMethod]
        [return: CastToFalse]
        public object query(ScriptContext context, object query, object resultType, PhpReference error)
        {
            SQLite.QueryResultKeys rt = SQLite.QueryResultKeys.Both;
            int vRt = PHP.Core.Convert.ObjectToInteger(resultType);
            if (Enum.IsDefined(typeof(SQLite.QueryResultKeys), vRt))
            {
                rt = (SQLite.QueryResultKeys)vRt;
            }

            PhpSQLiteDbResult result = (PhpSQLiteDbResult)SQLite.Query(this.m_con, query, rt, error);
            if (result != null)
            {
                return new SQLiteResult(result);
            }
            else
            {
                return null;
            }
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object query(object instance, PhpStack stack)
        {
            object query = stack.PeekValue(1);
            object resultType = stack.PeekValueOptional(2);
            PhpReference error = stack.PeekReferenceOptional(3);
            stack.RemoveFrame();
            return ((SQLiteDatabase)instance).query(stack.Context, query, resultType, error);
        }

        [ImplementsMethod]
        public object exec(ScriptContext context, object query, object error)
        {
            return SQLite.Exec(this.m_con, query, error as PhpReference);
        }
    }
}