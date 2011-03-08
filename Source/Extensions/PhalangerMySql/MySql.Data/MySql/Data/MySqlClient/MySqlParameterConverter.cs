namespace MySql.Data.MySqlClient
{
    using System;
    using System.ComponentModel;
    using System.ComponentModel.Design.Serialization;
    using System.Data;
    using System.Globalization;
    using System.Reflection;

    internal class MySqlParameterConverter : TypeConverter
    {
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            return ((destinationType == typeof(InstanceDescriptor)) || base.CanConvertTo(context, destinationType));
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(InstanceDescriptor))
            {
                ConstructorInfo constructor = typeof(MySqlParameter).GetConstructor(new Type[] { typeof(string), typeof(MySqlDbType), typeof(int), typeof(ParameterDirection), typeof(bool), typeof(byte), typeof(byte), typeof(string), typeof(DataRowVersion), typeof(object) });
                MySqlParameter parameter = (MySqlParameter) value;
                return new InstanceDescriptor(constructor, new object[] { parameter.ParameterName, parameter.DbType, parameter.Size, parameter.Direction, parameter.IsNullable, parameter.Precision, parameter.Scale, parameter.SourceColumn, parameter.SourceVersion, parameter.Value });
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}

