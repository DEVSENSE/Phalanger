using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PHP.Core;
using System.ComponentModel;
using PHP.Library.SPL;
using System.Data;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;

namespace PHP.Library.Data
{
    [ImplementsType]
    public abstract partial class PDOStatement : PhpObject, IteratorAggregate
    {
        protected readonly PDO m_pdo;

        public PDOStatement(ScriptContext context, PDO pdo)
            : base(context, true)
        {
            this.m_pdo = pdo;
            this.setFetchMode(context, (int)PDOFetchType.PDO_FETCH_BOTH, null, null);
        }

        public abstract bool ExecuteStatement();

        //////////////////////////
        //////////////////////////

        protected abstract IDbCommand CurrentCommand { get; }
        protected abstract IDataReader CurrentReader { get; }

        #region getIterator
        [PhpVisible]
        [ImplementsMethod]
        public object getIterator(ScriptContext context)
        {
            throw new NotImplementedException();
        }

        public static object getIterator(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((PDOStatement)instance).getIterator(stack.Context);
        }
        #endregion

        #region fetch

        private object fetch(ScriptContext context, int fetch_style)
        {
            return this.fetch(context, fetch_style, FETCH_ORI_NEXT, 0);
        }

        [PhpVisible]
        [ImplementsMethod]
        public object fetch(ScriptContext context, object fetch_style/*=null*/, object cursor_orientation/*FETCH_ORI_NEXT*/, object cursor_offset/*0*/)
        {
            PDOFetchType ft;
            if (fetch_style == null || fetch_style == Arg.Default)
                fetch_style = this.m_pdo.getAttribute(context, (int)PDOAttributeType.PDO_ATTR_DEFAULT_FETCH_MODE);
            
            int fetch_style_int = PHP.Core.Convert.ObjectToInteger(fetch_style);
            if (!Enum.IsDefined(typeof(PDOFetchType), fetch_style_int))
            {
                PDOException.Throw(context, "Invalid fetch_style value", null, null, null);
                return null;
            }
            ft = (PDOFetchType)fetch_style_int;
            var dr = this.CurrentReader;
            switch (ft)
            {
                case PDOFetchType.PDO_FETCH_ASSOC:
                    return Fetch_Assoc(m_pdo.Driver, dr, false) ?? (object)false;
                case PDOFetchType.PDO_FETCH_NUM:
                    return Fetch_Num(m_pdo.Driver, dr) ?? (object)false;
                case PDOFetchType.PDO_FETCH_BOTH:
                case PDOFetchType.PDO_FETCH_USE_DEFAULT:
                    return Fetch_Assoc(m_pdo.Driver, dr, true) ?? (object)false;
                default:
                    throw new NotImplementedException();
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

        private static PhpArray Fetch_Assoc(PDODriver driver, IDataReader dr, bool withNum)
        {
            if (dr.Read())
            {
                int fieldCount = dr.FieldCount;
                PhpArray arr = new PhpArray(fieldCount * (withNum ? 2 : 1));

                for (int i = 0; i < fieldCount; i++)
                {
                    string fName = dr.GetName(i);
                    object value = driver.ConvertDbValue(dr.GetValue(i), dr.GetDataTypeName(i));

                    arr.Add(fName, value);
                    if (withNum)
                        arr.Add(i, value);
                }
                return arr;
            }
            else
            {
                return null;
            }
        }

        private static PhpArray Fetch_Num(PDODriver driver, IDataReader dr)
        {
            if (dr.Read())
            {
                object[] values = new object[dr.FieldCount];
                dr.GetValues(values);

                for (int i = 0; i < values.Length; i++)
                    values[i] = driver.ConvertDbValue(values[i], dr.GetDataTypeName(i));
                
                return new PhpArray(values);
            }
            else
            {
                return null;
            }
        }

        #endregion

        #region close
        [PhpVisible]
        [ImplementsMethod]
        public object closeCursor(ScriptContext context)
        {
            this.CloseReader();
            return null;
        }

        protected abstract void CloseReader();

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object closeCursor(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((PDOStatement)instance).closeCursor(stack.Context);
        }
        #endregion

        public abstract void Init(string query, Dictionary<int, object> options);

        enum PreparedMode
        {
            None,
            Named,
            Numbers,
        }

        private PreparedMode m_prepMode = PreparedMode.None;
        private Dictionary<string, string> m_prepName = null;
        private List<string> m_prepNum = null;

        private static readonly Regex regName = new Regex(@"[\w_]+", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        internal void Prepare(ScriptContext context, string query, Dictionary<int, object> options)
        {
            this.m_prepMode = PreparedMode.None;
            this.m_prepName = new Dictionary<string, string>();
            this.m_prepNum = new List<string>();
            int pos = 0;
            StringBuilder sbRewritten = new StringBuilder();
            while (pos < query.Length)
            {
                char c = query[pos];
                switch (c)
                {
                    case '?':
                        {
                            if (this.m_prepMode == PreparedMode.None)
                            {
                                this.m_prepMode = PreparedMode.Numbers;
                            }
                            else
                            {
                                if (this.m_prepMode != PreparedMode.Numbers)
                                {
                                    PDOException.Throw(context, "Mixed parameter mode : use only '?' or ':name' pattern", null, null, null);
                                    return;
                                }
                            }
                            int paramNum = this.m_prepNum.Count;
                            string pName = this.m_pdo.Driver.GetParameterName("p" + paramNum);
                            this.m_prepNum.Insert(paramNum, pName);
                            sbRewritten.Append(pName);
                        }
                        break;
                    case ':':
                        {
                            if (this.m_prepMode == PreparedMode.None)
                            {
                                this.m_prepMode = PreparedMode.Named;
                            }
                            else
                            {
                                if (this.m_prepMode != PreparedMode.Named)
                                {
                                    PDOException.Throw(context, "Mixed parameter mode : use only '?' or ':name' pattern", null, null, null);
                                    return;
                                }
                            }
                            Match m = regName.Match(query, pos);
                            string paramName = m.Value;
                            string pName = this.m_pdo.Driver.GetParameterName(paramName);
                            this.m_prepName[paramName] = pName;
                            sbRewritten.Append(pName);
                            pos += paramName.Length;
                        }
                        break;
                    case '"':
                        sbRewritten.Append(c);
                        this.SkipToNext(query, sbRewritten, ref pos, '"');
                        break;
                    case '\'':
                        sbRewritten.Append(c);
                        this.SkipToNext(query, sbRewritten, ref pos, '\'');
                        break;
                    default:
                        sbRewritten.Append(c);
                        break;
                }
                pos++;
            }

            //this.CurrentCommand.CommandText = sbRewritten.ToString();
            this.Init(sbRewritten.ToString(), options);
            string[] arrParams = null;
            switch (this.m_prepMode)
            {
                case PreparedMode.Named:
                    arrParams = this.m_prepName.Values.ToArray();
                    break;
                case PreparedMode.Numbers:
                    arrParams = this.m_prepNum.ToArray();
                    break;
                case PreparedMode.None:
                default:
                    break;
            }
            this.CurrentCommand.Parameters.Clear();
            if (arrParams != null)
            {
                foreach (string paramName in arrParams)
                {
                    var param = this.CurrentCommand.CreateParameter();
                    param.ParameterName = paramName;
                    this.CurrentCommand.Parameters.Add(param);
                }
            }
            this.CurrentCommand.Prepare();
        }
        private void SkipToNext(string query, StringBuilder sbRewritten, ref int pos, char next)
        {
            SkipToNext(query, sbRewritten, ref pos, next, '\\');
        }
        private void SkipToNext(string query, StringBuilder sbRewritten, ref int pos, char next, char escapeChar)
        {
            while (++pos < query.Length)
            {
                char c = query[pos];
                sbRewritten.Append(c);
                if (c == next)
                {
                    break;
                }
                if (c == escapeChar)
                {
                    pos++;
                }
            }
        }

        #region bindValue

        [PhpVisible]
        [ImplementsMethod]
        public object bindValue(ScriptContext context, object parameter, object value, object data_type/*=null*/)
        {
            PDOParamType? dt = null;
            if (data_type != null && data_type != Arg.Default)
                dt = (PDOParamType)data_type;

            return this.bindValue(parameter, value, dt);
        }

        private bool bindValue(object param, object value, PDOParamType? type)
        {
            if (this.m_prepMode == PreparedMode.None)
            {
                PhpException.Throw(PhpError.Warning, "PDO statement not prepared or no parameters to bind");
                return false;
            }
            IDataParameter p;
            switch (this.m_prepMode)
            {
                case PreparedMode.Named:
                    string pName = PHP.Core.Convert.ObjectToString(param);
                    string pNameSql;

                    if (pName.Length > 0 && pName[0] == ':')
                    {
                        pName = pName.Substring(1);
                    }

                    if (this.m_prepName != null && this.m_prepName.TryGetValue(pName, out pNameSql))
                    {
                        p = (IDataParameter)this.CurrentCommand.Parameters[pNameSql];
                    }
                    else
                    {
                        PhpException.Throw(PhpError.Warning, "Parameter '" + pName + "' not found");
                        return false;
                    }
                    break;
                case PreparedMode.Numbers:
                    int pId = PHP.Core.Convert.ObjectToInteger(param);
                    if (this.m_prepNum == null || pId >= this.m_prepNum.Count)
                    {
                        PhpException.Throw(PhpError.Warning, "Parameter n° " + pId + " not found");
                        return false;
                    }
                    else
                    {
                        p = (IDataParameter)this.CurrentCommand.Parameters[this.m_prepNum[pId]];
                    }
                    break;
                default:
                    throw new NotImplementedException("Unknown prepared mode in PDOStatement::bindValue");
            }

            p.Value = value ?? DBNull.Value;
            if (type.HasValue)
            {
                switch (type.Value)
                {
                    case PDOParamType.PDO_PARAM_BOOL: p.DbType = DbType.Boolean; break;
                    case PDOParamType.PDO_PARAM_INT: p.DbType = DbType.Int32; break;
                    case PDOParamType.PDO_PARAM_LOB: p.DbType = DbType.Binary; break;
                    case PDOParamType.PDO_PARAM_NULL: break; //TODO : set right type
                    case PDOParamType.PDO_PARAM_STMT: break; //TODO : find what it is
                    case PDOParamType.PDO_PARAM_STR: p.DbType = DbType.String; break;
                }
            }
            return true;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object bindValue(object instance, PhpStack stack)
        {
            object parameter = stack.PeekValue(1);
            object value = stack.PeekReference(2);
            object data_type = stack.PeekValueOptional(3);
            stack.RemoveFrame();
            return ((PDOStatement)instance).bindValue(stack.Context, parameter, value, data_type);
        }
        #endregion

        #region execute
        
        private bool ExecuteInternal(object input_parameters)
        {
            if (input_parameters != null)
            {
                PhpArray arr;
                if ((arr = input_parameters as PhpArray) != null)
                {
                    if (arr.Count != 0)
                    {
                        PreparedMode mode = PreparedMode.None;
                        foreach (var item in arr)
                        {
                            Debug.Assert(item.Key.Object != null);

                            if (item.Key.IsString && (mode == PreparedMode.Named || mode == PreparedMode.None))
                            {
                                mode = PreparedMode.Named;
                            }
                            else if (item.Key.IsInteger && (mode == PreparedMode.Numbers || mode == PreparedMode.None))
                            {
                                mode = PreparedMode.Numbers;
                            }
                            else
                            {
                                PhpException.Throw(PhpError.Warning, "Invalid bind parameter " + item.Key.Object.ToString());
                                return false;
                            }

                            // bind the parameter
                            if (!this.bindValue(item.Key.Object, item.Value, PDOParamType.PDO_PARAM_STR))
                            {
                                PhpException.Throw(PhpError.Warning, "Can't bind parameter " + item.Key.Object.ToString());
                                return false;
                            }
                        }
                    }
                }
                else
                {
                    PhpException.InvalidArgumentType("input_parameters", PhpArray.PhpTypeName);
                    return false;
                }
            }
            return this.ExecuteStatement();
        }

        [PhpVisible, ImplementsMethod]
        public object execute(ScriptContext context, [Optional] object input_parameters)
        {
            return this.ExecuteInternal((input_parameters != Arg.Default) ? input_parameters : null);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object execute(object instance, PhpStack stack)
        {
            object input_parameters = stack.PeekValueOptional(1);
            stack.RemoveFrame();
            return ((PDOStatement)instance).execute(stack.Context, input_parameters);
        }
        #endregion

        #region fetchColumn
        
        [PhpVisible, ImplementsMethod]
        public object fetchColumn(ScriptContext context, object column_number/*=0*/)
        {
            object ret = this.fetch(context, PDO.FETCH_NUM);
            if (ret is bool && (bool)ret == false)
                return false;
            
            PhpArray arr = (PhpArray)ret;
            int col = (column_number == Arg.Default) ? 0 : PHP.Core.Convert.ObjectToInteger(column_number);
            return arr[col];
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object fetchColumn(object instance, PhpStack stack)
        {
            object column_number = stack.PeekValueOptional(1);
            stack.RemoveFrame();
            return ((PDOStatement)instance).fetchColumn(stack.Context, column_number);
        }
        #endregion

        #region rowCount
        [PhpVisible, ImplementsMethod]
        public object rowCount(ScriptContext context)
        {
            if (this.CurrentReader != null)
            {
                return this.CurrentReader.RecordsAffected;
            }
            return -1;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object rowCount(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((PDOStatement)instance).rowCount(stack.Context);
        }
        #endregion

        #region fetchAll
        
        [PhpVisible, ImplementsMethod]
        public object fetchAll(ScriptContext context, [Optional]object fetch_style/*=null*/, [Optional]object fetch_argument/*=null*/, [Optional]object ctor_args/*=null*/)
        {
            PhpArray arr = new PhpArray();

            PDOFetchType fetch;
            if (fetch_style == null || fetch_style == Arg.Default)
                fetch = PDOFetchType.PDO_FETCH_BOTH;
            else
                fetch = (PDOFetchType)(int)fetch_style;

            // TODO: fetch bitwise combinations (group, unique, ...)

            if (fetch == PDOFetchType.PDO_FETCH_COLUMN)
            {
                int column = (fetch_argument == null || fetch_argument == Arg.Default) ? 0 : Core.Convert.ObjectToInteger(fetch_argument);
                while (true)
                {
                    var ret = this.fetch(context, (int)PDOFetchType.PDO_FETCH_NUM) as PhpArray;
                    if (ret == null)
                        break;

                    arr.AddToEnd(ret[column]);
                }
            }
            else
            {
                while (true)
                {
                    object ret = this.fetch(context, (int)fetch);
                    if ((ret is bool && ((bool)ret) == false) || ret == null)
                    {
                        break;
                    }
                    else
                    {
                        arr.AddToEnd(ret);
                    }
                }
            }

            return arr;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object fetchAll(object instance, PhpStack stack)
        {
            object fetch_style = stack.PeekReferenceOptional(1);
            object fetch_argument = stack.PeekReferenceOptional(2);
            object ctor_args = stack.PeekReferenceOptional(3);
            stack.RemoveFrame();

            return ((PDOStatement)instance).fetchAll(stack.Context, fetch_style, fetch_argument, ctor_args);
        }
        #endregion
    }
}
