using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PHP.Core;
using System.ComponentModel;
using PHP.Library.SPL;
using System.Data;

namespace PHP.Library.Data
{
    [ImplementsType]
    public sealed class PDOStatement : PhpObject, IteratorAggregate
    {
        private readonly PDOConnection m_con;
        private string m_query;
        private IDataReader m_dr;
        private IDbCommand m_com;

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

        internal PDOStatement(ScriptContext context, PDOConnection connection)
            : base(context, true)
        {
            this.m_con = connection;
            this.setFetchMode(context, (int)PDOFetchType.PDO_FETCH_BOTH);
        }

        public string queryString { get { return this.m_query; } }

        [PhpVisible]
        [ImplementsMethod]
        [return: CastToFalse]
        public object setFetchMode(ScriptContext context, object fetch_to_mode)
        {
            return setFetchMode(context, fetch_to_mode, null, null);
        }
        [PhpVisible]
        [ImplementsMethod]
        [return: CastToFalse]
        public object setFetchMode(ScriptContext context, object fetch_to_mode, object fetch_to_dest)
        {
            return setFetchMode(context, fetch_to_mode, fetch_to_dest, null);
        }
        [PhpVisible]
        [ImplementsMethod]
        [return: CastToFalse]
        public object setFetchMode(ScriptContext context, object fetch_to_mode, object fetch_to_dest, object fetch_to_args)
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

        internal void SetQuery(string query)
        {
            this.m_query = query;
        }

        internal bool ExecuteInternal()
        {
            return this.m_con.ExecuteStatement(this, out this.m_com, out this.m_dr);
        }

        public object getIterator(ScriptContext context)
        {
            throw new NotImplementedException();
        }

        #region fetch
        [PhpVisible]
        [ImplementsMethod]
        public object fetch(ScriptContext context)
        {
            return this.fetch(context, null, FETCH_ORI_NEXT, 0);
        }

        [PhpVisible]
        [ImplementsMethod]
        public object fetch(ScriptContext context, object fetch_style)
        {
            return this.fetch(context, fetch_style, FETCH_ORI_NEXT, 0);
        }
        [PhpVisible]
        [ImplementsMethod]
        public object fetch(ScriptContext context, object fetch_style, object cursor_orientation)
        {
            return this.fetch(context, fetch_style, cursor_orientation, 0);
        }
        [PhpVisible]
        [ImplementsMethod]
        public object fetch(ScriptContext context, object fetch_style, object cursor_orientation, object cursor_offset)
        {
            PDOFetchType ft;
            if (fetch_style == null)
            {
                fetch_style = this.m_con.GetAttribute((int)PDOAttributeType.PDO_ATTR_DEFAULT_FETCH_MODE);
            }
            int fetch_style_int = PHP.Core.Convert.ObjectToInteger(fetch_style);
            if (!Enum.IsDefined(typeof(PDOFetchType), fetch_style_int))
            {
                throw new PDOException("Invalid fetch_style value");
            }
            ft = (PDOFetchType)fetch_style_int;
            if (this.m_dr.Read())
            {
                PhpArray arr;
                switch (ft)
                {
                    case PDOFetchType.PDO_FETCH_ASSOC:
                        arr = new PhpArray();
                        Fetch_Assoc(this.m_dr, arr, false);
                        return arr;
                    case PDOFetchType.PDO_FETCH_NUM:
                        arr = Fetch_Num(this.m_dr);
                        return arr;
                    case PDOFetchType.PDO_FETCH_BOTH:
                        arr = new PhpArray();
                        Fetch_Assoc(this.m_dr, arr, true);
                        return arr;
                    case PDOFetchType.PDO_FETCH_USE_DEFAULT:
                        arr = new PhpArray();
                        Fetch_Assoc(this.m_dr, arr, true);
                        return arr;
                    default:
                        throw new NotImplementedException();
                }
            }
            else
            {
                return false;
            }
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object fetch(object instance, PhpStack stack)
        {
            object style = stack.PeekValueOptional(1);
            object orientation = stack.PeekValueOptional(2);
            object cursor_offset = stack.PeekValueOptional(3);
            stack.RemoveFrame();
            return ((PDOStatement)instance).fetch(stack.Context, style, orientation, cursor_offset);
        }

        private static void Fetch_Assoc(IDataReader dr, PhpArray arr, bool withNum)
        {
            for (int i = 0; i < dr.FieldCount; i++)
            {
                string fName = dr.GetName(i);
                object value;
                if (dr.IsDBNull(i))
                {
                    value = null;
                }
                else
                {
                    value = dr.GetValue(i);
                }
                arr.Add(fName, value);
                if (withNum)
                {
                    arr.Add(i, value);
                }
            }
        }

        private static PhpArray Fetch_Num(IDataReader dr)
        {
            object[] values = new object[dr.FieldCount];
            dr.GetValues(values);
            return new PhpArray(values);
        }
        #endregion

        #region close
        [PhpVisible]
        [ImplementsMethod]
        public void Close(ScriptContext context)
        {
            if (this.m_dr != null)
            {
                this.m_dr.Dispose();
                this.m_dr = null;
            }
            if (this.m_com != null)
            {
                this.m_com.Dispose();
                this.m_com = null;
            }
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void Close(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            ((PDOStatement)instance).Close(stack.Context);
        }
        #endregion
    }
}
