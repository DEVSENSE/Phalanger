using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PHP.Core;
using System.ComponentModel;
using PHP.Library.SPL;
using System.Data;
using System.Text.RegularExpressions;

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
            this.setFetchMode(context, (int)PDOFetchType.PDO_FETCH_BOTH);
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
                fetch_style = this.m_pdo.getAttribute(context, (int)PDOAttributeType.PDO_ATTR_DEFAULT_FETCH_MODE);
            }
            int fetch_style_int = PHP.Core.Convert.ObjectToInteger(fetch_style);
            if (!Enum.IsDefined(typeof(PDOFetchType), fetch_style_int))
            {
                throw new PDOException("Invalid fetch_style value");
            }
            ft = (PDOFetchType)fetch_style_int;
            var dr = this.CurrentReader;
            switch (ft)
            {
                case PDOFetchType.PDO_FETCH_ASSOC:
                    return Fetch_Assoc(dr, false);
                case PDOFetchType.PDO_FETCH_NUM:
                    return Fetch_Num(dr);
                case PDOFetchType.PDO_FETCH_BOTH:
                case PDOFetchType.PDO_FETCH_USE_DEFAULT:
                    return Fetch_Assoc(dr, true);
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

        private static object Fetch_Assoc(IDataReader dr, bool withNum)
        {
            if (dr.Read())
            {
                PhpArray arr = new PhpArray();
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
                return arr;
            }
            else
            {
                return false;
            }
        }

        private static object Fetch_Num(IDataReader dr)
        {
            if (dr.Read())
            {
                object[] values = new object[dr.FieldCount];
                dr.GetValues(values);
                return new PhpArray(values);
            }
            else
            {
                return false;
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
        private readonly Dictionary<string, string> m_prepName = new Dictionary<string, string>();
        private readonly List<string> m_prepNum = new List<string>();

        private static readonly Regex regName = new Regex(@"[\w_]+", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        internal void Prepare(string query, Dictionary<int, object> options)
        {
            this.m_prepMode = PreparedMode.None;
            this.m_prepName.Clear();
            this.m_prepNum.Clear();
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
                                    throw new PDOException("Mixed parameter mode : use only '?' or ':name' pattern");
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
                                    throw new PDOException("Mixed parameter mode : use only '?' or ':name' pattern");
                                }
                            }
                            Match m = regName.Match(query, pos);
                            string paramName = m.Value;
                            string pName = this.m_pdo.Driver.GetParameterName(paramName);
                            this.m_prepName.Add(paramName, pName);
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
        public object bindValue(ScriptContext context, object parameter, object value)
        {
            return this.bindValue(context, parameter, value, null);
        }

        [PhpVisible]
        [ImplementsMethod]
        public object bindValue(ScriptContext context, object parameter, object value, object data_type)
        {
            PDOParamType? dt = null;
            if (data_type != null && Enum.IsDefined(typeof(PDOParamType), data_type))
            {
                dt = (PDOParamType)data_type;
            }
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
                    if (pName.Length > 0 && pName[0] == ':')
                    {
                        pName = pName.Substring(1);
                    }
                    if (!this.m_prepName.ContainsKey(pName))
                    {
                        PhpException.Throw(PhpError.Warning, "Parameter '" + pName + "' not found");
                        return false;
                    }
                    else
                    {
                        p = (IDataParameter)this.CurrentCommand.Parameters[this.m_prepName[pName]];
                    }
                    break;
                case PreparedMode.Numbers:
                    int pId = PHP.Core.Convert.ObjectToInteger(param);
                    if (pId >= this.m_prepNum.Count)
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
        [PhpVisible, ImplementsMethod]
        public object execute(ScriptContext context)
        {
            return this.ExecuteInternal(null);
        }

        private bool ExecuteInternal(object input_parameters)
        {
            if (input_parameters != null)
            {
                if (input_parameters is PhpArray)
                {
                    PhpArray arr = (PhpArray)input_parameters;
                    PreparedMode mode = PreparedMode.None;
                    foreach (IntStringKey key in arr.Keys)
                    {
                        if (key.IsInteger && (mode == PreparedMode.Numbers || mode == PreparedMode.None))
                        {
                            mode = PreparedMode.Numbers;
                            if (!this.bindValue(key.Integer, arr[key], PDOParamType.PDO_PARAM_STR))
                            {
                                PhpException.Throw(PhpError.Warning, "Can't bind parameter n°" + key.Integer);
                                return false;
                            }
                            continue;
                        }
                        if (key.IsString && (mode == PreparedMode.Named || mode == PreparedMode.None))
                        {
                            mode = PreparedMode.Named;
                            if (!this.bindValue(key.String, arr[key], PDOParamType.PDO_PARAM_STR))
                            {
                                PhpException.Throw(PhpError.Warning, "Can't bind parameter " + key.String);
                                return false;
                            }
                            continue;
                        }
                        PhpException.Throw(PhpError.Warning, "Invalid prepared statement");
                        return false;
                    }
                }
                else
                {
                    PhpException.Throw(PhpError.Warning, "input_parameters is not an array");
                    return false;
                }
            }
            return this.ExecuteStatement();
        }

        [PhpVisible, ImplementsMethod]
        public object execute(ScriptContext context, object input_parameters)
        {
            return this.ExecuteInternal(input_parameters);
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
        public object fetchColumn(ScriptContext context)
        {
            return this.fetchColumn(context, 0);
        }

        [PhpVisible, ImplementsMethod]
        public object fetchColumn(ScriptContext context, object column_number)
        {
            object ret = this.fetch(context, PDO.FETCH_NUM);
            if (ret is bool && (bool)ret == false)
            {
                return false;
            }
            PhpArray arr = (PhpArray)ret;
            int col = PHP.Core.Convert.ObjectToInteger(column_number);
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
        public object fetchAll(ScriptContext context)
        {
            return this.fetchAll(context, null, null, null);
        }

        [PhpVisible, ImplementsMethod]
        public object fetchAll(ScriptContext context, object fetch_style)
        {
            return this.fetchAll(context, fetch_style, null, null);
        }

        [PhpVisible, ImplementsMethod]
        public object fetchAll(ScriptContext context, object fetch_style, object fetch_argument)
        {
            return this.fetchAll(context, fetch_style, fetch_argument, null);
        }

        [PhpVisible, ImplementsMethod]
        public object fetchAll(ScriptContext context, object fetch_style, object fetch_argument, object ctor_args)
        {
            PhpArray arr = new PhpArray();
            PDOFetchType fetch;
            if (fetch_style == null)
            {
                fetch = PDOFetchType.PDO_FETCH_BOTH;
            }
            else
            {
                fetch = (PDOFetchType)fetch_style;
            }

            while (true)
            {
                switch (fetch)
                {
                    case PDOFetchType.PDO_FETCH_BOTH:
                    case PDOFetchType.PDO_FETCH_ASSOC:
                    case PDOFetchType.PDO_FETCH_NUM:
                        break;
                    default:
                        throw new NotImplementedException();
                }
                object ret = this.fetch(context, fetch_style);
                if (ret != null && (ret is bool && ((bool)ret) != false))
                {
                    arr.AddToEnd(ret);
                }
                else
                {
                    break;
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
