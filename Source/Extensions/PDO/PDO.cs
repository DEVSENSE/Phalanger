using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PHP.Core;
using System.ComponentModel;
using System.IO;

namespace PHP.Library.Data
{
    [ImplementsType]
    public partial class PDO : PhpObject
    {
        private PDODriver m_driver;
        private PDOConnection m_connection;

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
            object argUsername = stack.PeekReferenceOptional(2);
            object argPassword = stack.PeekReferenceOptional(3);
            object argDriverOptions = stack.PeekReferenceOptional(4);
            stack.RemoveFrame();
            return ((PDO)instance).__construct(stack.Context, argDSN, argUsername, argPassword, argDriverOptions);
        }

        [PhpVisible]
        [ImplementsMethod]
        public object __construct(ScriptContext context, object argdsn, object argusername, object argpassword, object argdriver_options)
        {
            string dsn = PHP.Core.Convert.ObjectToString(argdsn);
            string username = PHP.Core.Convert.ObjectToString(argusername);
            string password = PHP.Core.Convert.ObjectToString(argpassword);
            if (string.IsNullOrEmpty(dsn))
            {
                throw new ArgumentNullException();
            }

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
                    throw new PDOException("Driver not found");
                }
                this.m_connection = this.m_driver.OpenConnection(context, items[1], username, password, argdriver_options);
            }

            if (this.m_driver == null || this.m_connection == null)
            {
                throw new PDOException("Invalid DSN");
            }
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

        #region setAttribute
        [PhpVisible]
        [ImplementsMethod]
        public object setAttribute(ScriptContext context, object attribute, object value)
        {
            int attInt = PHP.Core.Convert.ObjectToInteger(attribute);
            return this.m_connection.SetAttribute(attInt, value);
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
            return this.m_connection.GetAttribute(attInt);
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
        public object quote(ScriptContext context, object str)
        {
            return quote(context, str, PARAM_STR);
        }

        [PhpVisible]
        [ImplementsMethod]
        public object quote(ScriptContext context, object str, object parameter_type)
        {
            PDOStatics.pdo_param_type pt = PDOStatics.pdo_param_type.PDO_PARAM_STR;
            if (parameter_type != null)
            {
                int ptInt = PHP.Core.Convert.ObjectToInteger(parameter_type);
                if (Enum.IsDefined(typeof(PDOStatics.pdo_param_type), ptInt))
                {
                    pt = (PDOStatics.pdo_param_type)ptInt;
                }
            }
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
        public object query(ScriptContext context, object statement)
        {
            return this.query(context, statement, null, null, null);
        }

        [PhpVisible]
        [ImplementsMethod]
        public object query(ScriptContext context, object statement, object fetch_to_mode)
        {
            return this.query(context, statement, fetch_to_mode, null, null);
        }

        [PhpVisible]
        [ImplementsMethod]
        public object query(ScriptContext context, object statement, object fetch_to_mode, object fetch_to_dest)
        {
            return this.query(context, statement, fetch_to_mode, fetch_to_dest, null);
        }

        [PhpVisible]
        [ImplementsMethod]
        public object query(ScriptContext context, object statement, object fetch_to_mode, object fetch_to_dest, object fetch_to_args)
        {
            PDOStatement stmt = new PDOStatement(context, this.m_connection);
            stmt.SetQuery(PHP.Core.Convert.ObjectToString(statement));
            if (fetch_to_mode != null)
            {
                stmt.setFetchMode(context, fetch_to_mode, fetch_to_dest, fetch_to_args);
            }
            stmt.ExecuteInternal();
            return stmt;
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

        #region beginTransaction

        #endregion
    }
}
