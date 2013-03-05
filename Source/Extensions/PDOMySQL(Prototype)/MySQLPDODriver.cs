using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PHP.Core;
using MySql.Data.MySqlClient;
using System.Data;

namespace PHP.Library.Data
{
    public sealed class MySQLPDODriver : PDODriver
    {
        public const int MYSQL_ATTR_USE_BUFFERED_QUERY = PDO.ATTR_DRIVER_SPECIFIC + 1;
        public const int MYSQL_ATTR_INIT_COMMAND = PDO.ATTR_DRIVER_SPECIFIC + 2;
        public const int MYSQL_ATTR_READ_DEFAULT_FILE = PDO.ATTR_DRIVER_SPECIFIC + 3;
        public const int MYSQL_ATTR_READ_DEFAULT_GROUP = PDO.ATTR_DRIVER_SPECIFIC + 4;
        public const int MYSQL_ATTR_MAX_BUFFER_SIZE = PDO.ATTR_DRIVER_SPECIFIC + 5;
        public const int MYSQL_ATTR_DIRECT_QUERY = PDO.ATTR_DRIVER_SPECIFIC + 6;
        public const int MYSQL_ATTR_FOUND_ROWS = PDO.ATTR_DRIVER_SPECIFIC + 7;
        public const int MYSQL_ATTR_IGNORE_SPACE = PDO.ATTR_DRIVER_SPECIFIC + 8;
        public const int MYSQL_ATTR_COMPRESS = PDO.ATTR_DRIVER_SPECIFIC + 9;
        public const int MYSQL_ATTR_SSL_CA = PDO.ATTR_DRIVER_SPECIFIC + 10;
        public const int MYSQL_ATTR_SSL_CAPATH = PDO.ATTR_DRIVER_SPECIFIC + 11;
        public const int MYSQL_ATTR_SSL_CERT = PDO.ATTR_DRIVER_SPECIFIC + 12;
        public const int MYSQL_ATTR_CIPHER = PDO.ATTR_DRIVER_SPECIFIC + 13;
        public const int MYSQL_ATTR_KEY = PDO.ATTR_DRIVER_SPECIFIC + 14;

        public override string Scheme { get { return "mysql"; } }

        public override object Quote(ScriptContext context, object strobj, PDOParamType param_type)
        {
            if (strobj == null)
                return string.Empty;

            // binary aware:
            if (strobj.GetType() == typeof(PhpBytes))
            {
                var strbytes = (PhpBytes)strobj;
                if (strbytes.Length == 0) return strobj;

                var bytes = strbytes.ReadonlyData;
                List<byte>/*!*/result = new List<byte>(bytes.Length);
                for (int i = 0; i < bytes.Length; i++)
                {
                    switch (bytes[i])
                    {
                        case (byte)'\0': result.Add((byte)'\\'); goto default;
                        case (byte)'\\': result.Add((byte)'\\'); goto default;
                        case (byte)'\n': result.Add((byte)'\\'); result.Add((byte)'n'); break;
                        case (byte)'\r': result.Add((byte)'\\'); result.Add((byte)'r'); break;
                        case (byte)'\u001a': result.Add((byte)'\\'); result.Add((byte)'Z'); break;
                        case (byte)'\'': result.Add((byte)'\\'); goto default;
                        case (byte)'"': result.Add((byte)'\\'); goto default;
                        default: result.Add(bytes[i]); break;
                    }
                }

                return new PhpBytes(result.ToArray());
            }

            // else
            string str = Core.Convert.ObjectToString(strobj);

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < str.Length; i++)
            {
                char c = str[i];
                switch (c)
                {
                    case '\0': sb.Append(@"\0"); break;
                    case '\\': sb.Append(@"\\"); break;
                    case '\n': sb.Append(@"\n"); break;
                    case '\r': sb.Append(@"\r"); break;
                    case '\u001a': sb.Append(@"\Z"); break;
                    case '\'': sb.Append(@"\'"); break;
                    case '"': sb.Append("\\\""); break;
                    default: sb.Append(c); break;
                }
            }

            return sb.ToString();
        }

        public override PDOStatement CreateStatement(ScriptContext context, PDO pdo)
        {
            throw new NotImplementedException();
        }

        protected override bool IsValueValidForAttribute(int att, object value)
        {
            return false;
        }

        public override System.Data.IDbConnection OpenConnection(ScriptContext context, string dsn_data, string username, string password, object argdriver_options)
        {
            var opt = PDO.SplitDsnParams(dsn_data);

            MySqlConnectionStringBuilder msb = new MySqlConnectionStringBuilder();
            foreach (string key in opt.Keys)
            {
                string value = opt[key];
                switch (key)
                {
                    case "host": msb.Server = value; break;
                    case "port": msb.Port = uint.Parse(value); break;
                    case "db_name": msb.Database = value; break;
                    case "unix_socket": throw new NotSupportedException(); break;
                    case "charset": msb.CharacterSet = value; break;
                    default:
                        throw new NotSupportedException();
                }
            }
            if (!string.IsNullOrEmpty(username))
            {
                msb.UserID = username;
            }
            if (!string.IsNullOrEmpty(password))
            {
                msb.Password = password;
            }
            if (argdriver_options is PhpArray)
            {
                PhpArray optArr = (PhpArray)argdriver_options;
                foreach (var key in optArr.Keys)
                {
                    if (key.IsInteger)
                    {
                        object value = optArr[key];
                        switch (key.Integer)
                        {
                            case MYSQL_ATTR_USE_BUFFERED_QUERY: break;
                            case MYSQL_ATTR_INIT_COMMAND: break;
                            case MYSQL_ATTR_READ_DEFAULT_FILE: break;
                            case MYSQL_ATTR_READ_DEFAULT_GROUP: break;
                            case MYSQL_ATTR_MAX_BUFFER_SIZE: break;
                            case MYSQL_ATTR_DIRECT_QUERY: break;
                            case MYSQL_ATTR_FOUND_ROWS: msb.UseAffectedRows = PHP.Core.Convert.ObjectToBoolean(value); break;
                            case MYSQL_ATTR_IGNORE_SPACE: break;
                            case MYSQL_ATTR_COMPRESS: msb.UseCompression = PHP.Core.Convert.ObjectToBoolean(value); break;
                            case MYSQL_ATTR_SSL_CA: break;
                            case MYSQL_ATTR_SSL_CAPATH: break;
                            case MYSQL_ATTR_SSL_CERT: msb.CertificateFile = PHP.Core.Convert.ObjectToString(value); break;
                            case MYSQL_ATTR_CIPHER: break;
                            case MYSQL_ATTR_KEY: msb.CertificatePassword = System.IO.File.ReadAllText(PHP.Core.Convert.ObjectToString(value)); break;
                            default:
                                throw new NotSupportedException();
                        }
                    }
                }
            }

            MySqlConnection con = new MySqlConnection(msb.ConnectionString);
            Action clear = null;
            clear = () =>
            {
                con.Dispose();
                RequestContext.RequestEnd -= clear;
            };
            RequestContext.RequestEnd += clear;
            con.Open();

            return con;
        }

        public override object GetLastInsertId(ScriptContext context, PDO pdo, string name)
        {
            MySqlConnection con = (MySqlConnection)pdo.Connection;
            using (var com = con.CreateCommand())
            {
                com.CommandText = "SELECT LAST_INSERT_ID()";
                com.Transaction = (MySqlTransaction)pdo.Transaction;
                return com.ExecuteScalar();
            }
        }
    }
}
