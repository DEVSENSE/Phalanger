using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PHP.Core;
using System.ComponentModel;
using System.IO;
using System.Data;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;

namespace PHP.Library.Data
{
    /// <summary>
    /// The PDO class
    /// </summary>
    [ImplementsType]
    public partial class PDO : PhpObject
    {
        private static readonly Regex sm_regexDSNSplit;
        /// <summary>
        /// Static constructor
        /// </summary>
        static PDO()
        {
            sm_regexDSNSplit = new Regex(@"(?<Keyword>\w+)\s*=\s*(?<Value>.*)((?=\W$)|\z);?", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        }

        /// <summary>
        /// Split DSN parameters
        /// </summary>
        /// <param name="dsn">DSN to split</param>
        /// <returns>Values</returns>
        public static System.Collections.Specialized.NameValueCollection SplitDsnParams(string dsn)
        {
            var arr = new System.Collections.Specialized.NameValueCollection();
            Match m = sm_regexDSNSplit.Match(dsn);
            while (m.Success)
            {
                string name = m.Groups["Keyword"].Value;
                string value = m.Groups["Vaue"].Value;

                arr.Add(name, value);
                m = m.NextMatch();
            }
            return arr;
        }

        private PDODriver m_driver;
        private PDOConnection m_con;
        private IDbTransaction m_tx;

        /// <summary>
        /// The driver instance
        /// </summary>
        public PDODriver Driver { get { return this.m_driver; } }
        /// <summary>
        /// Current transaction
        /// </summary>
        public IDbTransaction Transaction { get { return this.m_tx; } }
        /// <summary>
        /// Current database connection.
        /// </summary>
        public PDOConnection PDOConnection { get { return this.m_con; } }

        /// <summary>
        /// Current database connection.
        /// </summary>
        public IDbConnection Connection { get { return this.m_con.Connection; } }

        #region Constructor
        /// <summary>
        /// For internal purposes only.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public PDO(ScriptContext/*!*/context, bool newInstance)
            : base(context, newInstance)
        { }

        /// <summary>
        /// For internal purposes only.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public PDO(ScriptContext/*!*/context, PHP.Core.Reflection.DTypeDesc caller)
            : base(context, caller)
        { }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object __construct(object instance, PhpStack stack)
        {
            object argDSN = stack.PeekValue(1);
            object argUsername = stack.PeekValueOptional(2);
            object argPassword = stack.PeekValueOptional(3);
            object argDriverOptions = stack.PeekValueOptional(4);
            stack.RemoveFrame();
            return ((PDO)instance).__construct(stack.Context, argDSN, argUsername, argPassword, argDriverOptions);
        }

        [PhpVisible]
        [ImplementsMethod]
        public object __construct(ScriptContext context, object argdsn, [Optional] object argusername, [Optional] object argpassword, [Optional] object argdriver_options)
        {
            string dsn = PHP.Core.Convert.ObjectToString(argdsn);
            string username = (argusername == Arg.Default) ? null : PHP.Core.Convert.ObjectToString(argusername);
            string password = (argpassword == Arg.Default) ? null : PHP.Core.Convert.ObjectToString(argpassword);
            object driver_options = (argdriver_options == Arg.Default) ? null : argdriver_options;

            if (string.IsNullOrEmpty(dsn))
                throw new ArgumentNullException();
            
            const string uri = "uri:";
            if (dsn.StartsWith(uri))
            {
                Uri url = new Uri(dsn.Substring(uri.Length));
                throw new NotImplementedException("PDO uri handling");
            }
            string[] items = dsn.Split(new char[] { ':' }, 2);
            if (items.Length == 1)
            {
                //TODO : try to search for aliasing
                throw new NotImplementedException("PDO DSN aliasing");
            }
            if (items.Length == 2)
            {
                string drvName = items[0];
                this.m_driver = PDOLibraryDescriptor.GetProvider(drvName);
                if (this.m_driver == null)
                {
                    PDOException.Throw(context, "Driver not found", null, null, null);
                    return null;
                }
                this.m_con = this.m_driver.OpenConnection(context, items[1], username, password, driver_options);
            }

            if (this.m_driver == null || this.m_con == null)
            {
                PDOException.Throw(context, "Invalid DSN", null, null, null);
                return null;
            }

            //Defaults
            this.SetAttributeValueNoCheck(ATTR_AUTOCOMMIT, true);
            this.SetAttributeValueNoCheck(ATTR_DEFAULT_FETCH_MODE, FETCH_BOTH);
            this.SetAttributeValueNoCheck(ATTR_DRIVER_NAME, this.m_driver.Scheme);
            this.SetAttributeValueNoCheck(ATTR_ORACLE_NULLS, NULL_NATURAL);
            this.SetAttributeValueNoCheck(ATTR_STRINGIFY_FETCHES, false);
            this.SetAttributeValueNoCheck(ATTR_TIMEOUT, 30000);

            return null;
        }
        #endregion

        #region getAvailableDrivers
        [PhpVisible]
        [ImplementsMethod]
        public static object getAvailableDrivers(ScriptContext context)
        {
            PhpArray arr = new PhpArray(PDOStatics.Drivers());
            return arr;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object getAvailableDrivers(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return getAvailableDrivers(stack.Context);
        }
        #endregion

        #region Attributes
        private readonly Dictionary<int, object> m_attributes = new Dictionary<int, object>();

        public bool SetAttribute(int att, object value)
        {
            if (this.m_driver.IsValidAttributeValue(att, value))
            {
                return this.SetAttributeValueNoCheck(att, value);
            }
            else
            {
                return false;
            }
        }

        internal bool SetAttributeValueNoCheck(int att, object value)
        {
            this.m_attributes[att] = value;
            return true;
        }
        #endregion

        #region setAttribute
        [PhpVisible]
        [ImplementsMethod]
        public object setAttribute(ScriptContext context, object attribute, object value)
        {
            int attInt = PHP.Core.Convert.ObjectToInteger(attribute);
            return this.SetAttribute(attInt, value);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object setAttribute(object instance, PhpStack stack)
        {
            object attribute = stack.PeekValue(1);
            object value = stack.PeekValue(2);
            stack.RemoveFrame();
            return ((PDO)instance).setAttribute(stack.Context, attribute, value);
        }
        #endregion

        #region getAttribute
        [PhpVisible]
        [ImplementsMethod]
        public object getAttribute(ScriptContext context, object attribute)
        {
            int attInt = PHP.Core.Convert.ObjectToInteger(attribute);
            return this.GetAttribute(attInt, null);
        }

        public object GetAttribute(int attribute, object defaultValue)
        {
            if (this.m_attributes.ContainsKey(attribute))
            {
                return this.m_attributes[attribute];
            }
            return defaultValue;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object getAttribute(object instance, PhpStack stack)
        {
            object attribute = stack.PeekValue(1);
            stack.RemoveFrame();
            return ((PDO)instance).getAttribute(stack.Context, attribute);
        }
        #endregion

        #region quote
        
        [PhpVisible]
        [ImplementsMethod]
        public object quote(ScriptContext context, object str, [Optional] object parameter_type)
        {
            PDOParamType pt = PDOParamType.PDO_PARAM_STR;
            if (parameter_type != null && parameter_type != Arg.Default)
                pt = (PDOParamType)PHP.Core.Convert.ObjectToInteger(parameter_type);

            return this.m_driver.Quote(context, PHP.Core.Convert.ObjectToString(str), pt);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object quote(object instance, PhpStack stack)
        {
            object str = stack.PeekValue(1);
            object paramType = stack.PeekValueOptional(2);
            stack.RemoveFrame();
            return ((PDO)instance).quote(stack.Context, str, paramType);
        }
        #endregion

        #region query
        
        [PhpVisible]
        [ImplementsMethod]
        public object query(ScriptContext context, object statement, [Optional] object fetch_to_mode, [Optional] object fetch_to_dest, [Optional] object fetch_to_args)
        {
            string query = PHP.Core.Convert.ObjectToString(statement);
            PDOStatement stmt = this.m_driver.CreateStatement(context, this);

            stmt.Init(query, null);

            if (fetch_to_mode != null && fetch_to_mode != Arg.Default)
                stmt.setFetchMode(context,
                    fetch_to_mode,
                    (fetch_to_dest != Arg.Default) ? fetch_to_dest : null,
                    (fetch_to_args != Arg.Default) ? fetch_to_args : null);
            
            if (stmt.ExecuteStatement())
            {
                return stmt;
            }
            else
            {
                return false;
            }
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object query(object instance, PhpStack stack)
        {
            object statement = stack.PeekValue(1);
            object fetch_to_mode = stack.PeekValueOptional(2);
            object fetch_to_dest = stack.PeekValueOptional(3);
            object fetch_to_args = stack.PeekValueOptional(4);
            stack.RemoveFrame();
            return ((PDO)instance).query(stack.Context, statement, fetch_to_mode, fetch_to_dest, fetch_to_args);
        }
        #endregion

        #region prepare
        
        [PhpVisible]
        [ImplementsMethod]
        public object prepare(ScriptContext context, object statement, [Optional] object driver_options)
        {
            string query = PHP.Core.Convert.ObjectToString(statement);
            Dictionary<int, object> options = new Dictionary<int, object>();
            if (driver_options is PhpArray)
            {
                PhpArray arr = (PhpArray)driver_options;
                foreach (var key in arr.Keys)
                {
                    Debug.Assert(!key.IsInteger);
                    int keyInt = key.Integer;
                    options.Add(keyInt, arr[key]);
                }
            }

            PDOStatement stmt = this.m_driver.CreateStatement(context, this);
            stmt.Prepare(context, query, options);
            return stmt;
        }

        public static object prepare(object instance, PhpStack stack)
        {
            object statement = stack.PeekValue(1);
            object driver_options = stack.PeekValueOptional(2);
            stack.RemoveFrame();
            return ((PDO)instance).prepare(stack.Context, statement, driver_options);
        }
        #endregion

        #region beginTransaction
        [PhpVisible]
        [ImplementsMethod]
        public object beginTransaction(ScriptContext context)
        {
            if (this.m_tx != null)
                return false;

            this.PDOConnection.ClosePendingReader();

            this.m_tx = this.Connection.BeginTransaction();
            return true;
        }

        public static object beginTransaction(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((PDO)instance).beginTransaction(stack.Context);
        }
        #endregion

        #region commit
        [PhpVisible]
        [ImplementsMethod]
        public object commit(ScriptContext context)
        {
            if (this.m_tx != null)
            {
                this.PDOConnection.ClosePendingReader();

                this.m_tx.Commit();
                this.m_tx.Dispose();
                this.m_tx = null;
                return true;
            }
            return false;
        }

        public static object commit(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((PDO)instance).commit(stack.Context);
        }
        #endregion

        #region rollback
        [PhpVisible]
        [ImplementsMethod]
        public object rollback(ScriptContext context)
        {
            if (this.m_tx != null)
            {
                this.PDOConnection.ClosePendingReader();

                this.m_tx.Rollback();
                this.m_tx.Dispose();
                this.m_tx = null;
                return true;
            }
            return false;

        }

        public static object rollback(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((PDO)instance).rollback(stack.Context);
        }
        #endregion

        #region errorCode
        [ImplementsMethod, PhpVisible]
        public object errorCode(ScriptContext context)
        {
            throw new NotImplementedException();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object errorCode(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((PDO)instance).errorCode(stack.Context);
        }
        #endregion

        #region errorInfo
        [ImplementsMethod, PhpVisible]
        public object errorInfo(ScriptContext context)
        {
            throw new NotImplementedException();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object errorInfo(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((PDO)instance).errorInfo(stack.Context);
        }
        #endregion

        #region exec
        [ImplementsMethod, PhpVisible]
        public object exec(ScriptContext context, object statement)
        {
            throw new NotImplementedException();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object exec(object instance, PhpStack stack)
        {
            object statement = stack.PeekValue(1);
            stack.RemoveFrame();
            return ((PDO)instance).exec(stack.Context, statement);
        }
        #endregion

        #region inTransaction
        [ImplementsMethod, PhpVisible]
        public object inTransaction(ScriptContext context)
        {
            return this.Transaction != null;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object inTransaction(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((PDO)instance).inTransaction(stack.Context);
        }
        #endregion

        #region lastInsertId
        
        [ImplementsMethod, PhpVisible]
        public object lastInsertId(ScriptContext context, [Optional] object name)
        {
            return this.getLastInsertId(
                context,
                (name != Arg.Default && name != null) ? Core.Convert.ObjectToString(name) : null);
        }

        private object getLastInsertId(ScriptContext context, string name)
        {
            return this.m_driver.GetLastInsertId(context, this, name);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object lastInsertId(object instance, PhpStack stack)
        {
            object name = stack.PeekValueOptional(1);
            stack.RemoveFrame();
            return ((PDO)instance).lastInsertId(stack.Context, name);
        }
        #endregion
    }
}
