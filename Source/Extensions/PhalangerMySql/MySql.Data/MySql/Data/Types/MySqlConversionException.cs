namespace MySql.Data.Types
{
    using System;

    [Serializable]
    public class MySqlConversionException : Exception
    {
        public MySqlConversionException(string msg) : base(msg)
        {
        }
    }
}

