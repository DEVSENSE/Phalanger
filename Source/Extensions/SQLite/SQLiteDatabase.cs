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
        public SQLiteDatabase(ScriptContext context, object filename)
            : base(context, true)
        {
            this.__construct(context, filename);
        }
        public SQLiteDatabase(ScriptContext context, object filename, object argMode)
            : base(context, true)
        {
            this.__construct(context, filename, argMode);
        }

        public SQLiteDatabase(ScriptContext context, object filename, object argMode, object error)
            : base(context, true)
        {
            this.__construct(context, filename, argMode, error);
        }

        private string m_filename;
        private PhpResource m_con = null;

        [ImplementsMethod]
        public object __construct(ScriptContext context, object argFileName)
        {
            return this.__construct(context, argFileName, SQLite.DEFAULT_FILE_MODE, null);
        }

        [ImplementsMethod]
        public object __construct(ScriptContext context, object argFileName, object argMode)
        {
            return this.__construct(context, argFileName, argMode, null);
        }

        [ImplementsMethod]
        public object __construct(ScriptContext context, object argFileName, object argMode, object error)
        {
            string filename = PHP.Core.Convert.ObjectToString(argFileName);
            int mode = PHP.Core.Convert.ObjectToInteger(argMode);
            if (mode == 0)
            {
                mode = SQLite.DEFAULT_FILE_MODE;
            }
            this.m_filename = filename;
            this.m_con = SQLite.Open(this.m_filename, mode, error as PhpReference);
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

        [ImplementsMethod, PhpVisible]
        public object exec(ScriptContext context, object query, object error)
        {
            return SQLite.Exec(this.m_con, query, error as PhpReference);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object exec(object instance, PhpStack stack)
        {
            object query = stack.PeekValue(1);
            object error = stack.PeekReferenceOptional(2);
            stack.RemoveFrame();
            return ((SQLiteDatabase)instance).exec(stack.Context, query, error);
        }

        [ImplementsMethod, PhpVisible]
        public object createFunction(ScriptContext context, object function_name, object callback)
        {
            return this.createFunction(context, function_name, callback, -1);
        }

        [ImplementsMethod, PhpVisible]
        public object createFunction(ScriptContext context, object function_name, object callback, object num_args)
        {
            string fn = PHP.Core.Convert.ObjectToString(function_name);
            int na = PHP.Core.Convert.ObjectToInteger(num_args);
            SQLite.CreateFunction(this.m_con, fn, callback, na);
            return null;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object createFunction(object instance, PhpStack stack)
        {
            object function_name = stack.PeekValue(1);
            object callback = stack.PeekValue(2);
            object num_args = stack.PeekValueOptional(3);
            stack.RemoveFrame();
            return ((SQLiteDatabase)instance).createFunction(stack.Context, function_name, callback, num_args);
        }
    }
}