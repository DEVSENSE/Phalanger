namespace MySql.Data.Common
{
    using MySql.Data.MySqlClient;
    using MySql.Data.MySqlClient.Properties;
    using System;
    using System.Globalization;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential)]
    internal struct DBVersion
    {
        private int major;
        private int minor;
        private int build;
        private string srcString;
        public DBVersion(string s, int major, int minor, int build)
        {
            this.major = major;
            this.minor = minor;
            this.build = build;
            this.srcString = s;
        }

        public int Major
        {
            get
            {
                return this.major;
            }
        }
        public int Minor
        {
            get
            {
                return this.minor;
            }
        }
        public int Build
        {
            get
            {
                return this.build;
            }
        }
        public static DBVersion Parse(string versionString)
        {
            int startIndex = 0;
            int index = versionString.IndexOf('.', startIndex);
            if (index == -1)
            {
                throw new MySqlException(Resources.BadVersionFormat);
            }
            int major = Convert.ToInt32(versionString.Substring(startIndex, index - startIndex).Trim(), NumberFormatInfo.InvariantInfo);
            startIndex = index + 1;
            index = versionString.IndexOf('.', startIndex);
            if (index == -1)
            {
                throw new MySqlException(Resources.BadVersionFormat);
            }
            int minor = Convert.ToInt32(versionString.Substring(startIndex, index - startIndex).Trim(), NumberFormatInfo.InvariantInfo);
            startIndex = index + 1;
            int num5 = startIndex;
            while ((num5 < versionString.Length) && char.IsDigit(versionString, num5))
            {
                num5++;
            }
            return new DBVersion(versionString, major, minor, Convert.ToInt32(versionString.Substring(startIndex, num5 - startIndex).Trim(), NumberFormatInfo.InvariantInfo));
        }

        public bool isAtLeast(int majorNum, int minorNum, int buildNum)
        {
            return ((this.major > majorNum) || (((this.major == majorNum) && (this.minor > minorNum)) || (((this.major == majorNum) && (this.minor == minorNum)) && (this.build >= buildNum))));
        }

        public override string ToString()
        {
            return this.srcString;
        }
    }
}

