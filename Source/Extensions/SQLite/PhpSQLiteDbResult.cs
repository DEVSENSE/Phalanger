using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PHP.Core;
using System.Data;
using System.Data.SQLite;

namespace PHP.Library.Data
{
    public sealed class PhpSQLiteDbResult : PhpDbResult
    {
        /// <summary>
        /// Creates an instance of a result resource.
        /// </summary>
        /// <param name="connection">Database connection.</param>
        /// <param name="reader">Data reader from which to load results.</param>
        /// <param name="convertTypes">Whether to convert resulting values to PHP types.</param>
        /// <exception cref="ArgumentNullException">Argument is a <B>null</B> reference.</exception>
        public PhpSQLiteDbResult(PhpDbConnection/*!*/ connection, IDataReader/*!*/ reader, bool convertTypes)
            : base(connection, reader, "SQLite result", convertTypes)
        {
            // no code in here
        }

        internal static PhpSQLiteDbResult ValidResult(PhpResource handle)
        {
            PhpSQLiteDbResult result = handle as PhpSQLiteDbResult;
            if (result != null && result.IsValid) return result;

            PhpException.Throw(PhpError.Warning, LibResources.GetString("invalid_result_resource"));
            return null;
        }

        protected override object[] GetValues(string[] dataTypes, bool convertTypes)
        {
            SQLiteDataReader my_reader = (SQLiteDataReader)Reader;

            object[] oa = new object[my_reader.FieldCount];

            if (convertTypes)
            {
                for (int i = 0; i < Reader.FieldCount; i++)
                {
                    oa[i] = ConvertDbValue(dataTypes[i], my_reader.GetValue(i));
                }
            }
            else
            {
                for (int i = 0; i < Reader.FieldCount; i++)
                {
                    oa[i] = my_reader.GetValue(i);
                }
            }

            return oa;
        }

        private object ConvertDbValue(string dataType, object sqlValue)
        {
            if (sqlValue == null || sqlValue.GetType() == typeof(string) || sqlValue == System.DBNull.Value)
                return sqlValue;

            if (sqlValue.GetType() == typeof(double))
                return Core.Convert.DoubleToString((double)sqlValue);

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

            //if (sqlValue.GetType() == typeof(DateTime))
            //    return ConvertDateTime(dataType, (DateTime)sqlValue);

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

            //MySqlDateTime sql_date_time = sqlValue as MySqlDateTime;
            //if (sqlValue.GetType() == typeof(MySqlDateTime))
            //{
            //    MySqlDateTime sql_date_time = (MySqlDateTime)sqlValue;
            //    if (sql_date_time.IsValidDateTime)
            //        return ConvertDateTime(dataType, sql_date_time.GetDateTime());

            //    if (dataType == "DATE" || dataType == "NEWDATE")
            //        return "0000-00-00";
            //    else
            //        return "0000-00-00 00:00:00";
            //}

            Debug.Fail("Unexpected DB field type " + sqlValue.GetType() + ".");
            return sqlValue.ToString();
        }

        //private string ConvertDateTime(string dataType, DateTime value)
        //{
        //    if (dataType == "DATE" || dataType == "NEWDATE")
        //        return value.ToString("yyyy-MM-dd");
        //    else
        //        return value.ToString("yyyy-MM-dd HH:mm:ss");
        //}

        protected override string MapFieldTypeName(string typeName)
        {
            switch (typeName)
            {
                case "VARCHAR":
                    return "string";

                case "INT":
                case "BIGINT":
                case "MEDIUMINT":
                case "SMALLINT":
                case "TINYINT":
                    return "int";

                case "FLOAT":
                case "DOUBLE":
                case "DECIMAL":
                    return "real";

                case "YEAR":
                    return "year";

                case "DATE":
                case "NEWDATE":
                    return "date";

                case "TIMESTAMP":
                    return "timestamp";

                case "DATETIME":
                    return "datetime";

                case "TIME":
                    return "time";

                case "SET":
                    return "set";

                case "ENUM":
                    return "enum";

                case "TINY_BLOB":
                case "MEDIUM_BLOB":
                case "LONG_BLOB":
                case "BLOB":
                    return "blob";

                // not in PHP:
                case "BIT":
                    return "bit";

                case null:
                case "NULL":
                    return "NULL";

                default:
                    return "unknown";
            }
        }
    }
}
