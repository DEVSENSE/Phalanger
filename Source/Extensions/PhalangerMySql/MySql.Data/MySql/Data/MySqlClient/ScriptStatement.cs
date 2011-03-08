namespace MySql.Data.MySqlClient
{
    using System;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential)]
    internal struct ScriptStatement
    {
        public string text;
        public int line;
        public int position;
    }
}

