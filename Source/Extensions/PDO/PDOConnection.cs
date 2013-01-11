using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace PHP.Library.Data
{
    public sealed class PDOConnection : PhpDbConnection
    {
        /// <summary>
        /// Underlying database connection.
        /// </summary>
        public IDbConnection/*!*/Connection { get { return this.connection; } }

        /// <summary>
        /// Pending DB data reader.
        /// </summary>
        public IDataReader PendingReader { get { return this.pendingReader; } set { this.pendingReader = value; } }

        /// <summary>
        /// Last DB command. Used internally by PDO driver.
        /// </summary>
        public IDbCommand LastCommand { get; set; }

        public PDOConnection(string/*!*/ connectionString, IDbConnection/*!*/ connection, string/*!*/ name)
            : base(connectionString, connection, name)
        {
        }

        protected override IDbCommand CreateCommand()
        {
            throw new NotImplementedException();
        }

        protected override PhpDbResult GetResult(PhpDbConnection connection, IDataReader reader, bool convertTypes)
        {
            throw new NotImplementedException();
        }
    }

//    [Obsolete("replaced into PDO", true)]
//    public abstract class PDOConnection
//    {
//        private readonly PDODriver m_driver;
//        private readonly Dictionary<int, object> m_attributes;

//        protected IDbTransaction CurrentTransaction { get { return this.m_tx; } }

//        public PDODriver Driver { get { return this.m_driver; } }

//        public PDOConnection(PDODriver driver)
//        {
//            this.m_driver = driver;
//            this.m_attributes = new Dictionary<int, object>();

//            this.SetAttributeValueNoCheck(PDOAttributeType.PDO_ATTR_AUTOCOMMIT, true);

//            this.SetAttributeDefaults();
//        }

//        internal bool SetAttribute(int att, object value)
//        {
//            return this.SetAttributeValue(att, value);
//        }

//        protected abstract void SetAttributeDefaults();

//        protected bool SetAttributeValueNoCheck(PDOAttributeType attribute, object value)
//        {
//            return this.SetAttributeValueNoCheck((int)attribute, value);
//        }
//        private bool SetAttributeValueNoCheck(int attribute, object value)
//        {
//            if (this.m_attributes.ContainsKey((int)attribute))
//                this.m_attributes[attribute] = value;
//            else
//                this.m_attributes.Add(attribute, value);
//            return true;
//        }

//        protected virtual bool SetAttributeValue(int attribute, object value)
//        {
//            if (this.ValidateAttribute(attribute, value))
//            {
//                return this.SetAttributeValueNoCheck(attribute, value);
//            }
//            else
//            {
//                return false;
//            }
//        }

//        internal object GetAttribute(int att)
//        {
//            return this.GetAttributeValue(att);
//        }

//        protected virtual object GetAttributeValue(int att)
//        {
//            if (this.m_attributes.ContainsKey(att))
//            {
//                return this.m_attributes[att];
//            }
//            else
//            {
//                return null;
//            }
//        }

//        internal bool ValidateAttribute(int attribute, object value)
//        {
//            if (Enum.IsDefined(typeof(PDOAttributeType), attribute))
//            {
//                PDOAttributeType att = (PDOAttributeType)attribute;
//                switch (att)
//                {
//                    case PDOAttributeType.PDO_ATTR_AUTOCOMMIT: return value is bool;
//                    case PDOAttributeType.PDO_ATTR_CASE: return Enum.IsDefined(typeof(PDOCaseConversion), value);
//                    //case PDOAttributeType.PDO_ATTR_CLIENT_VERSION: return false;
//                    //case PDOAttributeType.PDO_ATTR_CONNECTION_STATUS: return false;
//                    //case PDOAttributeType.PDO_ATTR_CURSOR: return false;
//                    //case PDOAttributeType.PDO_ATTR_CURSOR_NAME: return false;
//                    case PDOAttributeType.PDO_ATTR_DEFAULT_FETCH_MODE: return Enum.IsDefined(typeof(PDOFetchType), value);
//                    //case PDOAttributeType.PDO_ATTR_DRIVER_NAME: return false;
//                    //case PDOAttributeType.PDO_ATTR_DRIVER_SPECIFIC: return false;
//                    case PDOAttributeType.PDO_ATTR_EMULATE_PREPARES: return value is bool;
//                    case PDOAttributeType.PDO_ATTR_ERRMODE: return Enum.IsDefined(typeof(PDOErrorMode), value);
//                    case PDOAttributeType.PDO_ATTR_FETCH_CATALOG_NAMES: return value is bool;
//                    //case PDOAttributeType.PDO_ATTR_FETCH_TABLE_NAMES: return false;
//                    //case PDOAttributeType.PDO_ATTR_MAX_COLUMN_LEN: return false;
//                    case PDOAttributeType.PDO_ATTR_ORACLE_NULLS: return Enum.IsDefined(typeof(PDONullHandling), value);
//                    //case PDOAttributeType.PDO_ATTR_PERSISTENT: return false;
//                    //case PDOAttributeType.PDO_ATTR_PREFETCH: return false;
//                    //case PDOAttributeType.PDO_ATTR_SERVER_INFO: return false;
//                    //case PDOAttributeType.PDO_ATTR_SERVER_VERSION: return false;
//                    //case PDOAttributeType.PDO_ATTR_STATEMENT_CLASS: return false;
//                    case PDOAttributeType.PDO_ATTR_STRINGIFY_FETCHES: return value is bool;
//                    case PDOAttributeType.PDO_ATTR_TIMEOUT: return value is int;
//                }
//            }
//            return ValidateAttributeValue(attribute, value);
//        }

//        protected abstract bool ValidateAttributeValue(int attribute, object value);

//        //public abstract bool ExecuteStatement(PDOStatement stmt, out IDbCommand com, out IDataReader dr);

//        protected int GetAttributeValueInt(PDOAttributeType attribute, int defaultValue)
//        {
//            if (this.m_attributes.ContainsKey((int)attribute))
//            {
//                return (int)this.m_attributes[(int)attribute];
//            }
//            else
//            {
//                return defaultValue;
//            }
//        }

//        internal bool beginTransaction()
//        {
//            if (this.m_tx != null)
//            {
//                return false;
//            }
//            this.m_tx = this.begin_transaction();
//            this.SetAttributeValueNoCheck(PDOAttributeType.PDO_ATTR_AUTOCOMMIT, false);
//            return this.m_tx != null;
//        }

//        protected abstract IDbTransaction begin_transaction();

//        internal bool commit()
//        {
//            if (this.m_tx == null)
//            {
//                return false;
//            }
//            this.m_tx.Commit();
//            this.m_tx = null;
//            this.SetAttributeValueNoCheck(PDOAttributeType.PDO_ATTR_AUTOCOMMIT, true);
//            return true;
//        }

//        internal bool rollback()
//        {
//            if (this.m_tx == null)
//            {
//                return false;
//            }
//            this.m_tx.Rollback();
//            this.m_tx = null;
//            this.SetAttributeValueNoCheck(PDOAttributeType.PDO_ATTR_AUTOCOMMIT, true);
//            return true;
//        }
//    }
}
