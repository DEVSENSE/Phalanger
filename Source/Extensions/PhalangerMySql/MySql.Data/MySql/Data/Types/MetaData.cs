namespace MySql.Data.Types
{
    using MySql.Data.MySqlClient;
    using System;
    using System.Globalization;

    internal class MetaData
    {
        public static bool IsNumericType(string typename)
        {
            switch (typename.ToLower(CultureInfo.InvariantCulture))
            {
                case "int":
                case "integer":
                case "numeric":
                case "decimal":
                case "dec":
                case "fixed":
                case "tinyint":
                case "mediumint":
                case "bigint":
                case "real":
                case "double":
                case "float":
                case "serial":
                case "smallint":
                    return true;
            }
            return false;
        }

        public static MySqlDbType NameToType(string typeName, bool unsigned, bool realAsFloat, MySqlConnection connection)
        {
            switch (typeName.ToUpper(CultureInfo.InvariantCulture))
            {
                case "CHAR":
                    return MySqlDbType.String;

                case "VARCHAR":
                    return MySqlDbType.VarChar;

                case "DATE":
                    return MySqlDbType.Date;

                case "DATETIME":
                    return MySqlDbType.DateTime;

                case "NUMERIC":
                case "DECIMAL":
                case "DEC":
                case "FIXED":
                    if (!connection.driver.Version.isAtLeast(5, 0, 3))
                    {
                        return MySqlDbType.Decimal;
                    }
                    return MySqlDbType.NewDecimal;

                case "YEAR":
                    return MySqlDbType.Year;

                case "TIME":
                    return MySqlDbType.Time;

                case "TIMESTAMP":
                    return MySqlDbType.Timestamp;

                case "SET":
                    return MySqlDbType.Set;

                case "ENUM":
                    return MySqlDbType.Enum;

                case "BIT":
                    return MySqlDbType.Bit;

                case "TINYINT":
                    if (unsigned)
                    {
                        return MySqlDbType.UByte;
                    }
                    return MySqlDbType.Byte;

                case "BOOL":
                case "BOOLEAN":
                    return MySqlDbType.Byte;

                case "SMALLINT":
                    if (unsigned)
                    {
                        return MySqlDbType.UInt16;
                    }
                    return MySqlDbType.Int16;

                case "MEDIUMINT":
                    if (unsigned)
                    {
                        return MySqlDbType.UInt24;
                    }
                    return MySqlDbType.Int24;

                case "INT":
                case "INTEGER":
                    if (unsigned)
                    {
                        return MySqlDbType.UInt32;
                    }
                    return MySqlDbType.Int32;

                case "SERIAL":
                    return MySqlDbType.UInt64;

                case "BIGINT":
                    if (unsigned)
                    {
                        return MySqlDbType.UInt64;
                    }
                    return MySqlDbType.Int64;

                case "FLOAT":
                    return MySqlDbType.Float;

                case "DOUBLE":
                    return MySqlDbType.Double;

                case "REAL":
                    if (realAsFloat)
                    {
                        return MySqlDbType.Float;
                    }
                    return MySqlDbType.Double;

                case "TEXT":
                    return MySqlDbType.Text;

                case "BLOB":
                    return MySqlDbType.Blob;

                case "LONGBLOB":
                    return MySqlDbType.LongBlob;

                case "LONGTEXT":
                    return MySqlDbType.LongText;

                case "MEDIUMBLOB":
                    return MySqlDbType.MediumBlob;

                case "MEDIUMTEXT":
                    return MySqlDbType.MediumText;

                case "TINYBLOB":
                    return MySqlDbType.TinyBlob;

                case "TINYTEXT":
                    return MySqlDbType.TinyText;

                case "BINARY":
                    return MySqlDbType.Binary;

                case "VARBINARY":
                    return MySqlDbType.VarBinary;
            }
            throw new MySqlException("Unhandled type encountered");
        }

        public static bool SupportScale(string typename)
        {
            string str2;
            if (((str2 = typename.ToLower(CultureInfo.InvariantCulture)) == null) || ((!(str2 == "numeric") && !(str2 == "decimal")) && (!(str2 == "dec") && !(str2 == "real"))))
            {
                return false;
            }
            return true;
        }
    }
}

