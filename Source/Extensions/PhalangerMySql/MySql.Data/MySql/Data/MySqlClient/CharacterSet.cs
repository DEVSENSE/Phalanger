namespace MySql.Data.MySqlClient
{
    using System;

    internal class CharacterSet
    {
        public int byteCount;
        public string name;

        public CharacterSet(string name, int byteCount)
        {
            this.name = name;
            this.byteCount = byteCount;
        }
    }
}

