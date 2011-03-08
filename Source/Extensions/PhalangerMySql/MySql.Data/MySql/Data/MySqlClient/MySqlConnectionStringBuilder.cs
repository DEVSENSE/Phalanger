using System.Diagnostics;
namespace MySql.Data.MySqlClient
{
    using MySql.Data.MySqlClient.Properties;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Data.Common;
    using System.Globalization;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Text.RegularExpressions;

    public class MySqlConnectionStringBuilder : DbConnectionStringBuilder
    {
        private static Dictionary<string, PropertyDefaultValue> defaultValues = new Dictionary<string, PropertyDefaultValue>(StringComparer.OrdinalIgnoreCase);
        private static Dictionary<string, string> validKeywords = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, object> values;

        static MySqlConnectionStringBuilder()
        {
            Initialize();
        }

        public MySqlConnectionStringBuilder()
        {
            this.values = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            this.Clear();
        }

        public MySqlConnectionStringBuilder(string connStr) : this()
        {
            base.ConnectionString = connStr;
        }

        private static void AddKeywordFromProperty(PropertyInfo pi)
        {
            string str = pi.Name.ToLower(CultureInfo.InvariantCulture);
            string displayName = str;
            object[] customAttributes = pi.GetCustomAttributes(false);
            foreach (Attribute attribute in customAttributes)
            {
                if (attribute is DisplayNameAttribute)
                {
                    displayName = (attribute as DisplayNameAttribute).DisplayName;
                    break;
                }
            }
            validKeywords[str] = displayName;
            validKeywords[displayName] = displayName;
            foreach (Attribute attribute2 in customAttributes)
            {
                if (attribute2 is ValidKeywordsAttribute)
                {
                    foreach (string str3 in (attribute2 as ValidKeywordsAttribute).Keywords)
                    {
                        validKeywords[str3.ToLower(CultureInfo.InvariantCulture).Trim()] = displayName;
                    }
                }
                else if (attribute2 is DefaultValueAttribute)
                {
                    if ((attribute2 as DefaultValueAttribute).Value != null)
                        Debug.Write((attribute2 as DefaultValueAttribute).Value.ToString());

                    object defaultValue = (attribute2 as DefaultValueAttribute).Value;

                    defaultValues[displayName] = new PropertyDefaultValue(pi.PropertyType,
                        (pi.PropertyType.IsEnum) ?
                            Enum.ToObject(pi.PropertyType, Convert.ChangeType(defaultValue, typeof(Int32), CultureInfo.CurrentCulture)) :
                            Convert.ChangeType(defaultValue, pi.PropertyType, CultureInfo.CurrentCulture)
                        );
                }
            }
        }

        public override void Clear()
        {
            base.Clear();
            foreach (string str in defaultValues.Keys)
            {
                this.values[str] = defaultValues[str].DefaultValue;
            }
        }

        internal Regex GetBlobAsUTF8ExcludeRegex()
        {
            if (string.IsNullOrEmpty(this.BlobAsUTF8ExcludePattern))
            {
                return null;
            }
            return new Regex(this.BlobAsUTF8ExcludePattern);
        }

        internal Regex GetBlobAsUTF8IncludeRegex()
        {
            if (string.IsNullOrEmpty(this.BlobAsUTF8IncludePattern))
            {
                return null;
            }
            return new Regex(this.BlobAsUTF8IncludePattern);
        }

        public string GetConnectionString(bool includePass)
        {
            if (includePass)
            {
                return base.ConnectionString;
            }
            StringBuilder builder = new StringBuilder();
            string str = "";
            foreach (string str2 in this.Keys)
            {
                if ((string.Compare(str2, "password", true) != 0) && (string.Compare(str2, "pwd", true) != 0))
                {
                    builder.AppendFormat(CultureInfo.CurrentCulture, "{0}{1}={2}", new object[] { str, str2, this[str2] });
                    str = ";";
                }
            }
            return builder.ToString();
        }

        private object ChangeType(object value, Type t)
        {
            if ((t != typeof(bool)) || !(value is string))
            {
                return Convert.ChangeType(value, t, CultureInfo.CurrentCulture);
            }
            string str = value.ToString().ToLower(CultureInfo.InvariantCulture);
            switch (str)
            {
                case "yes":
                case "true":
                    return true;
            }
            if (!(str == "no") && !(str == "false"))
            {
                throw new FormatException(string.Format(Resources.InvalidValueForBoolean, value));
            }
            return false;
        }

        private static void Initialize()
        {
            foreach (PropertyInfo info in typeof(MySqlConnectionStringBuilder).GetProperties())
            {
                AddKeywordFromProperty(info);
            }
            AddKeywordFromProperty(typeof(MySqlConnectionStringBuilder).GetProperty("Encrypt", BindingFlags.NonPublic | BindingFlags.Instance));
        }

        private object ParseEnum(Type t, string requestedValue, string key)
        {
            object obj2;
            try
            {
                obj2 = Enum.Parse(t, requestedValue, true);
            }
            catch (ArgumentException)
            {
                throw new InvalidOperationException(string.Format(Resources.InvalidConnectionStringValue, requestedValue, key));
            }
            return obj2;
        }

        public override bool Remove(string keyword)
        {
            this.ValidateKeyword(keyword);
            string key = validKeywords[keyword];
            this.values.Remove(key);
            base.Remove(key);
            this.values[key] = defaultValues[key].DefaultValue;
            return true;
        }

        private void SetValue(string keyword, object value)
        {
            this.ValidateKeyword(keyword);
            keyword = validKeywords[keyword];
            this.Remove(keyword);
            object obj2 = null;
            if ((value is string) && (defaultValues[keyword].DefaultValue is Enum))
            {
                obj2 = this.ParseEnum(defaultValues[keyword].Type, (string) value, keyword);
            }
            else
            {
                obj2 = this.ChangeType(value, defaultValues[keyword].Type);
            }
            this.values[keyword] = obj2;
            base[keyword] = obj2;
        }

        public override bool TryGetValue(string keyword, out object value)
        {
            this.ValidateKeyword(keyword);
            return this.values.TryGetValue(validKeywords[keyword], out value);
        }

        private void ValidateKeyword(string keyword)
        {
            string key = keyword.ToLower(CultureInfo.InvariantCulture);
            if (!validKeywords.ContainsKey(key))
            {
                throw new ArgumentException(Resources.KeywordNotSupported, keyword);
            }
            if (validKeywords[key] == "Use Old Syntax")
            {
                MySqlTrace.LogWarning(-1, "Use Old Syntax is now obsolete.  Please see documentation");
            }
            if (validKeywords[key] == "Encrypt")
            {
                MySqlTrace.LogWarning(-1, "Encrypt is now obsolete. Use Ssl Mode instead");
            }
        }

        [Category("Connection"), DisplayName("Allow Batch"), Description("Allows execution of multiple SQL commands in a single statement"), DefaultValue(true), RefreshProperties(RefreshProperties.All)]
        public bool AllowBatch
        {
            get
            {
                return (bool) this.values["Allow Batch"];
            }
            set
            {
                this.SetValue("Allow Batch", value);
            }
        }

        [Category("Advanced"), RefreshProperties(RefreshProperties.All), DisplayName("Allow User Variables"), Description("Should the provider expect user variables to appear in the SQL."), DefaultValue(false)]
        public bool AllowUserVariables
        {
            get
            {
                return (bool) this.values["Allow User Variables"];
            }
            set
            {
                this.SetValue("Allow User Variables", value);
            }
        }

        [Category("Advanced"), RefreshProperties(RefreshProperties.All), DisplayName("Allow Zero Datetime"), Description("Should zero datetimes be supported"), DefaultValue(false)]
        public bool AllowZeroDateTime
        {
            get
            {
                return (bool) this.values["Allow Zero Datetime"];
            }
            set
            {
                this.SetValue("Allow Zero DateTime", value);
            }
        }

        [DisplayName("Auto Enlist"), DefaultValue(true), Category("Advanced"), RefreshProperties(RefreshProperties.All), Description("Should the connetion automatically enlist in the active connection, if there are any.")]
        public bool AutoEnlist
        {
            get
            {
                return (bool) this.values["Auto Enlist"];
            }
            set
            {
                this.SetValue("Auto Enlist", value);
            }
        }

        [Description("Pattern that matches columns that should not be treated as UTF8"), RefreshProperties(RefreshProperties.All), DefaultValue(""), Category("Advanced")]
        public string BlobAsUTF8ExcludePattern
        {
            get
            {
                return (string) this.values["BlobAsUTF8ExcludePattern"];
            }
            set
            {
                this.SetValue("BlobAsUTF8ExcludePattern", value);
            }
        }

        [DefaultValue(""), RefreshProperties(RefreshProperties.All), Category("Advanced"), Description("Pattern that matches columns that should be treated as UTF8")]
        public string BlobAsUTF8IncludePattern
        {
            get
            {
                return (string) this.values["BlobAsUTF8IncludePattern"];
            }
            set
            {
                this.SetValue("BlobAsUTF8IncludePattern", value);
            }
        }

        [DisplayName("Certificate File"), Category("Authentication"), DefaultValue((string) null), Description("Certificate file in PKCS#12 format (.pfx)")]
        public string CertificateFile
        {
            get
            {
                return (string) this.values["Certificate File"];
            }
            set
            {
                this.SetValue("Certificate File", value);
            }
        }

        [Category("Authentication"), DefaultValue((string) null), DisplayName("Certificate Password"), Description("Password for certificate file")]
        public string CertificatePassword
        {
            get
            {
                return (string) this.values["Certificate Password"];
            }
            set
            {
                this.SetValue("Certificate Password", value);
            }
        }

        [Description("Certificate Store Location for client certificates"), Category("Authentication"), DisplayName("Certificate Store Location"), DefaultValue(0)]
        public MySqlCertificateStoreLocation CertificateStoreLocation
        {
            get
            {
                return (MySqlCertificateStoreLocation) this.values["Certificate Store Location"];
            }
            set
            {
                this.SetValue("Certificate Store Location", value);
            }
        }

        [Category("Authentication"), DisplayName("Certificate Thumbprint"), Description("Certificate thumbprint. Can be used together with Certificate Store Location parameter to uniquely identify certificate to be used for SSL authentication."), DefaultValue((string) null)]
        public string CertificateThumbprint
        {
            get
            {
                return (string) this.values["Certificate Thumbprint"];
            }
            set
            {
                this.SetValue("Certificate Thumbprint", value);
            }
        }

        [RefreshProperties(RefreshProperties.All), DefaultValue(0), Category("Pooling"), DisplayName("Connection Lifetime"), Description("The minimum amount of time (in seconds) for this connection to live in the pool before being destroyed.")]
        public uint ConnectionLifeTime
        {
            get
            {
                return (uint) this.values["Connection LifeTime"];
            }
            set
            {
                this.SetValue("Connection LifeTime", value);
            }
        }

        [Description("Protocol to use for connection to MySQL"), Category("Connection"), DefaultValue(1), DisplayName("Connection Protocol"), ValidKeywords("protocol"), RefreshProperties(RefreshProperties.All)]
        public MySqlConnectionProtocol ConnectionProtocol
        {
            get
            {
                return (MySqlConnectionProtocol) this.values["Connection Protocol"];
            }
            set
            {
                this.SetValue("Connection Protocol", value);
            }
        }

        [Description("When true, indicates the connection state is reset when removed from the pool."), RefreshProperties(RefreshProperties.All), DisplayName("Connection Reset"), Category("Pooling"), DefaultValue(false)]
        public bool ConnectionReset
        {
            get
            {
                return (bool) this.values["Connection Reset"];
            }
            set
            {
                this.SetValue("Connection Reset", value);
            }
        }

        [ValidKeywords("connection timeout"), DisplayName("Connect Timeout"), Description("The length of time (in seconds) to wait for a connection to the server before terminating the attempt and generating an error."), DefaultValue(15), Category("Connection"), RefreshProperties(RefreshProperties.All)]
        public uint ConnectionTimeout
        {
            get
            {
                return (uint) this.values["Connect Timeout"];
            }
            set
            {
                uint num = Math.Min(value, 0x20c49b);
                if (num != value)
                {
                    MySqlTrace.LogWarning(-1, string.Concat(new object[] { "Connection timeout value too large (", value, " seconds). Changed to max. possible value", num, " seconds)" }));
                }
                this.SetValue("Connect Timeout", num);
            }
        }

        [RefreshProperties(RefreshProperties.All), Category("Advanced"), DisplayName("Convert Zero Datetime"), Description("Should illegal datetime values be converted to DateTime.MinValue"), DefaultValue(false)]
        public bool ConvertZeroDateTime
        {
            get
            {
                return (bool) this.values["Convert Zero Datetime"];
            }
            set
            {
                this.SetValue("Convert Zero DateTime", value);
            }
        }

        [RefreshProperties(RefreshProperties.All), ValidKeywords("initial catalog"), Category("Connection"), Description("Database to use initially"), DefaultValue("")]
        public string Database
        {
            get
            {
                return (this.values["database"] as string);
            }
            set
            {
                this.SetValue("database", value);
            }
        }

        [Category("Connection"), DisplayName("Default Command Timeout"), Description("The default timeout that MySqlCommand objects will use\r\n                     unless changed."), DefaultValue(30), ValidKeywords("command timeout"), RefreshProperties(RefreshProperties.All)]
        public uint DefaultCommandTimeout
        {
            get
            {
                return (uint) this.values["Default Command Timeout"];
            }
            set
            {
                this.SetValue("Default Command Timeout", value);
            }
        }

        [DefaultValue(false), Description("Should the connection use SSL."), Obsolete("Use Ssl Mode instead."), Category("Authentication")]
        internal bool Encrypt
        {
            get
            {
                return (this.SslMode != MySqlSslMode.None);
            }
            set
            {
                this.SetValue("Ssl Mode", value ? MySqlSslMode.Preferred : MySqlSslMode.None);
            }
        }

        [Category("Advanced"), Description("Should all server functions be treated as returning string?"), DefaultValue(false), DisplayName("Functions Return String")]
        public bool FunctionsReturnString
        {
            get
            {
                return (bool) this.values["Functions Return String"];
            }
            set
            {
                this.SetValue("Functions Return String", value);
            }
        }

        [Description("Character set this connection should use"), RefreshProperties(RefreshProperties.All), Category("Advanced"), DisplayName("Character Set"), DefaultValue(""), ValidKeywords("charset")]
        public string CharacterSet
        {
            get
            {
                return (string) this.values["Character Set"];
            }
            set
            {
                this.SetValue("Character Set", value);
            }
        }

        [DisplayName("Ignore Prepare"), Description("Instructs the provider to ignore any attempts to prepare a command."), RefreshProperties(RefreshProperties.All), DefaultValue(true), Category("Advanced")]
        public bool IgnorePrepare
        {
            get
            {
                return (bool) this.values["Ignore Prepare"];
            }
            set
            {
                this.SetValue("Ignore Prepare", value);
            }
        }

        [Description("Should this session be considered interactive?"), Category("Advanced"), DefaultValue(false), ValidKeywords("interactive"), RefreshProperties(RefreshProperties.All), DisplayName("Interactive Session")]
        public bool InteractiveSession
        {
            get
            {
                return (bool) this.values["Interactive Session"];
            }
            set
            {
                this.SetValue("Interactive Session", value);
            }
        }

        public override object this[string keyword]
        {
            get
            {
                return this.values[validKeywords[keyword]];
            }
            set
            {
                this.ValidateKeyword(keyword);
                if (value == null)
                {
                    this.Remove(keyword);
                }
                else
                {
                    this.SetValue(keyword, value);
                }
            }
        }

        [Description("For TCP connections, idle connection time measured in seconds, before the first keepalive packet is sent.A value of 0 indicates that keepalive is not used."), DisplayName("Keep Alive"), DefaultValue(0)]
        public uint Keepalive
        {
            get
            {
                return (uint) this.values["Keep Alive"];
            }
            set
            {
                this.SetValue("Keep Alive", value);
            }
        }

        [RefreshProperties(RefreshProperties.All), Description("Enables output of diagnostic messages"), DefaultValue(false), Category("Connection")]
        public bool Logging
        {
            get
            {
                return (bool) this.values["Logging"];
            }
            set
            {
                this.SetValue("Logging", value);
            }
        }

        [Category("Pooling"), DefaultValue(100), RefreshProperties(RefreshProperties.All), ValidKeywords("max pool size"), DisplayName("Maximum Pool Size"), Description("The maximum number of connections allowed in the pool.")]
        public uint MaximumPoolSize
        {
            get
            {
                return (uint) this.values["Maximum Pool Size"];
            }
            set
            {
                this.SetValue("Maximum Pool Size", value);
            }
        }

        [Category("Pooling"), ValidKeywords("min pool size"), RefreshProperties(RefreshProperties.All), DisplayName("Minimum Pool Size"), Description("The minimum number of connections allowed in the pool."), DefaultValue(0)]
        public uint MinimumPoolSize
        {
            get
            {
                return (uint) this.values["Minimum Pool Size"];
            }
            set
            {
                this.SetValue("Minimum Pool Size", value);
            }
        }

        [DefaultValue(false), Category("Advanced"), DisplayName("Old Guids"), Description("Treat binary(16) columns as guids")]
        public bool OldGuids
        {
            get
            {
                return (bool) this.values["Old Guids"];
            }
            set
            {
                this.SetValue("Old Guids", value);
            }
        }

        [PasswordPropertyText(true), Description("Indicates the password to be used when connecting to the data source."), Category("Security"), DefaultValue(""), ValidKeywords("pwd"), RefreshProperties(RefreshProperties.All)]
        public string Password
        {
            get
            {
                return (string) this.values["Password"];
            }
            set
            {
                this.SetValue("Password", value);
            }
        }

        [Category("Security"), DisplayName("Persist Security Info"), Description("When false, security-sensitive information, such as the password, is not returned as part of the connection if the connection is open or has ever been in an open state."), DefaultValue(false), RefreshProperties(RefreshProperties.All)]
        public bool PersistSecurityInfo
        {
            get
            {
                return (bool) this.values["Persist Security Info"];
            }
            set
            {
                this.SetValue("Persist Security Info", value);
            }
        }

        [ValidKeywords("pipe"), DisplayName("Pipe Name"), Description("Name of pipe to use when connecting with named pipes (Win32 only)"), DefaultValue("MYSQL"), Category("Connection"), RefreshProperties(RefreshProperties.All)]
        public string PipeName
        {
            get
            {
                return (string) this.values["Pipe Name"];
            }
            set
            {
                this.SetValue("Pipe Name", value);
            }
        }

        [Description("When true, the connection object is drawn from the appropriate pool, or if necessary, is created and added to the appropriate pool."), DefaultValue(true), RefreshProperties(RefreshProperties.All), Category("Pooling")]
        public bool Pooling
        {
            get
            {
                return (bool) this.values["Pooling"];
            }
            set
            {
                this.SetValue("Pooling", value);
            }
        }

        [RefreshProperties(RefreshProperties.All), Description("Port to use for TCP/IP connections"), DefaultValue(0xcea), Category("Connection")]
        public uint Port
        {
            get
            {
                return (uint) this.values["Port"];
            }
            set
            {
                this.SetValue("Port", value);
            }
        }

        [RefreshProperties(RefreshProperties.All), Description("Indicates how many stored procedures can be cached at one time. A value of 0 effectively disables the procedure cache."), ValidKeywords("procedure cache, procedurecache"), Category("Advanced"), DefaultValue(0x19), DisplayName("Procedure Cache Size")]
        public uint ProcedureCacheSize
        {
            get
            {
                return (uint) this.values["Procedure Cache Size"];
            }
            set
            {
                this.SetValue("Procedure Cache Size", value);
            }
        }

        [DefaultValue(true), RefreshProperties(RefreshProperties.All), Category("Advanced"), DisplayName("Respect Binary Flags"), Description("Should binary flags on column metadata be respected.")]
        public bool RespectBinaryFlags
        {
            get
            {
                return (bool) this.values["Respect Binary Flags"];
            }
            set
            {
                this.SetValue("Respect Binary Flags", value);
            }
        }

        [Category("Connection"), Description("Server to connect to"), ValidKeywords("host, data source, datasource, address, addr, network address"), RefreshProperties(RefreshProperties.All), DefaultValue("")]
        public string Server
        {
            get
            {
                return (this.values["server"] as string);
            }
            set
            {
                this.SetValue("server", value);
            }
        }

        [Description("Name of the shared memory object to use"), DefaultValue("MYSQL"), RefreshProperties(RefreshProperties.All), DisplayName("Shared Memory Name"), Category("Connection")]
        public string SharedMemoryName
        {
            get
            {
                return (string) this.values["Shared Memory Name"];
            }
            set
            {
                this.SetValue("Shared Memory Name", value);
            }
        }

        [Category("Advanced"), Description("Allow Sql Server syntax.  A value of yes allows symbols to be enclosed with [] instead of ``.  This does incur a performance hit so only use when necessary."), DisplayName("Sql Server Mode"), ValidKeywords("sqlservermode, sql server mode"), DefaultValue(false)]
        public bool SqlServerMode
        {
            get
            {
                return (bool) this.values["Sql Server Mode"];
            }
            set
            {
                this.SetValue("Sql Server Mode", value);
            }
        }

        [DisplayName("Ssl Mode"), Category("Security"), Description("SSL properties for connection"), DefaultValue(0)]
        public MySqlSslMode SslMode
        {
            get
            {
                return (MySqlSslMode) this.values["Ssl Mode"];
            }
            set
            {
                this.SetValue("Ssl Mode", value);
            }
        }

        [RefreshProperties(RefreshProperties.All), DefaultValue(false), DisplayName("Treat Blobs As UTF8"), Category("Advanced"), Description("Should binary blobs be treated as UTF8")]
        public bool TreatBlobsAsUTF8
        {
            get
            {
                return (bool) this.values["Treat Blobs As UTF8"];
            }
            set
            {
                this.SetValue("Treat Blobs As UTF8", value);
            }
        }

        [DisplayName("Treat Tiny As Boolean"), Category("Advanced"), RefreshProperties(RefreshProperties.All), Description("Should the provider treat TINYINT(1) columns as boolean."), DefaultValue(true)]
        public bool TreatTinyAsBoolean
        {
            get
            {
                return (bool) this.values["Treat Tiny As Boolean"];
            }
            set
            {
                this.SetValue("Treat Tiny As Boolean", value);
            }
        }

        [Description("Should the returned affected row count reflect affected rows instead of found rows?"), Category("Advanced"), DisplayName("Use Affected Rows"), DefaultValue(false)]
        public bool UseAffectedRows
        {
            get
            {
                return (bool) this.values["Use Affected Rows"];
            }
            set
            {
                this.SetValue("Use Affected Rows", value);
            }
        }

        [DefaultValue(false), Category("Connection"), ValidKeywords("compress"), RefreshProperties(RefreshProperties.All), DisplayName("Use Compression"), Description("Should the connection ues compression")]
        public bool UseCompression
        {
            get
            {
                return (bool) this.values["Use Compression"];
            }
            set
            {
                this.SetValue("Use Compression", value);
            }
        }

        [DisplayName("Use Old Syntax"), DefaultValue(false), ValidKeywords("old syntax, oldsyntax"), RefreshProperties(RefreshProperties.All), Obsolete("Use Old Syntax is no longer needed.  See documentation"), Category("Connection"), Description("Allows the use of old style @ syntax for parameters")]
        public bool UseOldSyntax
        {
            get
            {
                return (bool) this.values["Use Old Syntax"];
            }
            set
            {
                this.SetValue("Use Old Syntax", value);
            }
        }

        [Description("Indicates that performance counters should be updated during execution."), DefaultValue(false), ValidKeywords("userperfmon, perfmon"), RefreshProperties(RefreshProperties.All), DisplayName("Use Performance Monitor"), Category("Advanced")]
        public bool UsePerformanceMonitor
        {
            get
            {
                return (bool) this.values["Use Performance Monitor"];
            }
            set
            {
                this.SetValue("Use Performance Monitor", value);
            }
        }

        [RefreshProperties(RefreshProperties.All), Description("Indicates if stored procedure bodies will be available for parameter detection."), ValidKeywords("procedure bodies"), Category("Advanced"), DefaultValue(true), DisplayName("Use Procedure Bodies")]
        public bool UseProcedureBodies
        {
            get
            {
                return (bool) this.values["Use Procedure Bodies"];
            }
            set
            {
                this.SetValue("Use Procedure Bodies", value);
            }
        }

        [Category("Security"), DisplayName("User Id"), Description("Indicates the user ID to be used when connecting to the data source."), DefaultValue(""), ValidKeywords("uid, username, user name, user"), RefreshProperties(RefreshProperties.All)]
        public string UserID
        {
            get
            {
                return (string) this.values["User Id"];
            }
            set
            {
                this.SetValue("User Id", value);
            }
        }

        [ValidKeywords("usage advisor"), DisplayName("Use Usage Advisor"), DefaultValue(false), RefreshProperties(RefreshProperties.All), Description("Logs inefficient database operations"), Category("Advanced")]
        public bool UseUsageAdvisor
        {
            get
            {
                return (bool) this.values["Use Usage Advisor"];
            }
            set
            {
                this.SetValue("Use Usage Advisor", value);
            }
        }
    }
}

