namespace MySql.Data.Common
{
    using System;

    internal class Platform
    {
        private static bool inited;
        private static bool isMono;

        private Platform()
        {
        }

        private static void Init()
        {
            inited = true;
            isMono = Type.GetType("Mono.Runtime") != null;
        }

        public static bool IsMono()
        {
            if (!inited)
            {
                Init();
            }
            return isMono;
        }

        public static bool IsWindows()
        {
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.Win32S:
                case PlatformID.Win32Windows:
                case PlatformID.Win32NT:
                    return true;
            }
            return false;
        }
    }
}

