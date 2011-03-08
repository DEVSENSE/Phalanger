namespace MySql.Data.MySqlClient
{
    using System;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential)]
    internal struct PropertyDefaultValue
    {
        public System.Type Type;
        public object DefaultValue;
        public PropertyDefaultValue(System.Type t, object v)
        {
            this.Type = t;
            this.DefaultValue = v;
        }
    }
}

