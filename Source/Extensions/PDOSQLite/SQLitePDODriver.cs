using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SQLite;
using PHP.Core;
using System.IO;
using System.Data;

namespace PHP.Library.Data
{

    public sealed class SQLitePDODriver : PDODriver
    {
        public override string Scheme { get { return "sqlite"; } }

        public override PDOConnection OpenConnection(ScriptContext context, string dsn_data, string username, string password, object argdriver_options)
        {
            //Determine file path
            string filename = dsn_data.Replace('/', Path.DirectorySeparatorChar);
            string filePath = Path.GetFullPath(Path.Combine(context.WorkingDirectory, filename));

            SQLiteConnectionStringBuilder csb = new SQLiteConnectionStringBuilder();
            csb.DataSource = filePath;
            csb.Version = 3;

            var con = new PDOConnection(csb.ConnectionString, new SQLiteConnection(), "PDO sqllite connection");
            con.Connect();

            return con;
        }

        public override object Quote(ScriptContext context, object strobj, PDOParamType param_type)
        {
            // From mysql extension
            // in addition, resulting string is quoted as '...'

            if (strobj == null)
                return string.Empty;

            // binary aware:
            if (strobj.GetType() == typeof(PhpBytes))
            {
                var strbytes = (PhpBytes)strobj;
                if (strbytes.Length == 0) return strobj;

                var bytes = strbytes.ReadonlyData;
                List<byte>/*!*/result = new List<byte>(bytes.Length + 2);
                result.Add((byte)'\'');
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
                result.Add((byte)'\'');

                return new PhpBytes(result.ToArray());
            }

            // else
            string str = Core.Convert.ObjectToString(strobj);

            StringBuilder sb = new StringBuilder();
            sb.Append('\'');
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
            sb.Append('\'');

            return sb.ToString();
        }

        public override PDOStatement CreateStatement(ScriptContext context, PDO pdo)
        {
            SQLitePDOStatement stmt = new SQLitePDOStatement(context, pdo);
            return stmt;
        }

        protected override bool IsValueValidForAttribute(int att, object value)
        {
            PDOAttributeType attE = (PDOAttributeType)att;
            switch (attE)
            {
                case PDOAttributeType.PDO_ATTR_EMULATE_PREPARES:
                    return value is bool;
                case PDOAttributeType.PDO_ATTR_ERRMODE:
                    return Enum.IsDefined(typeof(PDOErrorMode), value);
                default:
                    break;
            }
            return false;
        }

        internal static object PDO_sqliteCreateFunction(object instance, PhpStack stack)
        {
            string func_name = PHP.Core.Convert.ObjectToString(stack.PeekValue(1));
            PhpCallback callback = PHP.Core.Convert.ObjectToCallback(stack.PeekValue(2));
            object nbr = stack.PeekValueOptional(3);
            stack.RemoveFrame();

            int nbr_arg;
            if (nbr == null)
            {
                nbr_arg = -1;
            }
            else
            {
                nbr_arg = PHP.Core.Convert.ObjectToInteger(nbr);
            }

            Delegate d = new Func<object[], object>(callback.Invoke);

            SQLiteFunction.RegisterFunction(func_name, FunctionType.Scalar, nbr_arg, d);
            return null;
        }

        public override object GetLastInsertId(ScriptContext context, PDO pdo, string name)
        {
            return ((SQLiteConnection)pdo.Connection).LastInsertRowId;
        }
    }
}

