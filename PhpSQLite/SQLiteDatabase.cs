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
        /// <summary>
        /// For internal purposes only.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public SQLiteDatabase(ScriptContext/*!*/context, bool newInstance)
            : base(context, newInstance)
        { }

        /// <summary>
        /// For internal purposes only.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public SQLiteDatabase(ScriptContext/*!*/context, DTypeDesc caller)
            : base(context, caller)
        { }
        //#endregion

        //#region __construct
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object __construct(object instance, PhpStack stack)
        {
            object filename = stack.PeekValue(1);
            object mode = stack.PeekValueOptional(2);
            object error = stack.PeekValueOptional(3);
            stack.RemoveFrame();
            string s = null;
            ((SQLiteDatabase)instance).__construct(stack.Context, null, ref s);
            //TODO : return error
            return null;
        }

        //public SQLiteDatabase(string filename, int mode, ref string error)
        //    : base(ScriptContext.CurrentContext, true)
        //{
        //    this.m_con = SQLite.Open(filename, mode, ref error);
        //}

        //public SQLiteDatabase(string filename, int mode)
        //    : base(ScriptContext.CurrentContext, true)
        //{
        //    string error = null;
        //    this.m_con = SQLite.Open(filename, mode, ref error);
        //}

        public SQLiteDatabase(string filename)
            : base(ScriptContext.CurrentContext, true)
        {
            this.m_filename = filename;
        }

        private string m_filename;
        private PhpResource m_con = null;

        [PhpVisible]
        public void __construct(ScriptContext context, [Optional]int? mode, [Optional] ref string error)
        {
            int m;
            if (mode.HasValue)
                m = mode.Value;
            else
                m = SQLite.DEFAULT_FILE_MODE;
            this.m_con = SQLite.Open(this.m_filename, m, ref error);
        }

        [PhpVisible]
        [return: CastToFalse]
        public SQLiteResult query(object query, SQLite.QueryResultKeys resultType, ref object error)
        {
            PhpSQLiteDbResult result = (PhpSQLiteDbResult)SQLite.Query(this.m_con, query, resultType, ref error);
            if (result != null)
            {
                return new SQLiteResult(result);
            }
            else
            {
                return null;
            }
        }
    }
}
