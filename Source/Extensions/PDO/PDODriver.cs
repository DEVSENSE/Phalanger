using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PHP.Core;
using System.Data;

namespace PHP.Library.Data
{
    public abstract class PDODriver
    {
        public abstract string Scheme { get; }
        public abstract PDOConnection OpenConnection(ScriptContext context, string dsn_data, string username, string password, object argdriver_options);
        public abstract object Quote(ScriptContext context, object strobj, PDOParamType param_type);

        public virtual string GetParameterName(string name)
        {
            return string.Format("@{0}", name);
        }

        public virtual object ConvertDbValue(object sqlValue, string dataType)
        {
            if (sqlValue == null || sqlValue.GetType() == typeof(string))
                return sqlValue;

            if (sqlValue.GetType() == typeof(double))
                return Core.Convert.DoubleToString((double)sqlValue);

            if (sqlValue == System.DBNull.Value)
                return null;

            if (sqlValue.GetType() == typeof(int))
                return ((int)sqlValue).ToString();

            if (sqlValue.GetType() == typeof(uint))
                return ((uint)sqlValue).ToString();

            if (sqlValue.GetType() == typeof(bool))
                return (bool)sqlValue ? "1" : "0";

            if (sqlValue.GetType() == typeof(byte))
                return ((byte)sqlValue).ToString();

            if (sqlValue.GetType() == typeof(sbyte))
                return ((sbyte)sqlValue).ToString();

            if (sqlValue.GetType() == typeof(short))
                return ((short)sqlValue).ToString();

            if (sqlValue.GetType() == typeof(ushort))
                return ((ushort)sqlValue).ToString();

            if (sqlValue.GetType() == typeof(float))
                return Core.Convert.DoubleToString((float)sqlValue);

            if (sqlValue.GetType() == typeof(DateTime))
                return ConvertDateTime(dataType, (DateTime)sqlValue);

            if (sqlValue.GetType() == typeof(long))
                return ((long)sqlValue).ToString();

            if (sqlValue.GetType() == typeof(ulong))
                return ((ulong)sqlValue).ToString();

            if (sqlValue.GetType() == typeof(TimeSpan))
                return ((TimeSpan)sqlValue).ToString();

            if (sqlValue.GetType() == typeof(decimal))
                return ((decimal)sqlValue).ToString();

            if (sqlValue.GetType() == typeof(byte[]))
                return new PhpBytes((byte[])sqlValue);

            return sqlValue;
        }

        protected static string ConvertDateTime(string dataType, DateTime value)
        {
            if (dataType == "DATE" || dataType == "NEWDATE")
                return value.ToString("yyyy-MM-dd");
            else
                return value.ToString("yyyy-MM-dd HH:mm:ss");
        }

        public abstract PDOStatement CreateStatement(ScriptContext context, PDO pdo);

        internal bool IsValidAttributeValue(int att, object value)
        {
            return this.IsValueValidForAttribute(att, value);
        }

        protected abstract bool IsValueValidForAttribute(int att, object value);

        public abstract object GetLastInsertId(ScriptContext context, PDO pdo, string name);
    }
}
