namespace MySql.Data.MySqlClient
{
    using MySql.Data.MySqlClient.Properties;
    using System;
    using System.Diagnostics;
    using System.Reflection;

    public class MySqlTrace
    {
        private static bool qaEnabled = false;
        private static string qaHost;
        private static TraceSource source = new TraceSource("mysql");

        static MySqlTrace()
        {
            foreach (TraceListener listener in source.Listeners)
            {
                if (listener.GetType().ToString().Contains("MySql.EMTrace.EMTraceListener"))
                {
                    qaEnabled = true;
                    break;
                }
            }
        }

        public static void DisableQueryAnalyzer()
        {
            qaEnabled = false;
            foreach (TraceListener listener in source.Listeners)
            {
                if (listener.GetType().ToString().Contains("EMTraceListener"))
                {
                    source.Listeners.Remove(listener);
                    break;
                }
            }
        }

        public static void EnableQueryAnalyzer(string host, int postInterval)
        {
            if (!qaEnabled)
            {
                TraceListener listener = (TraceListener) Activator.CreateInstance("MySql.EMTrace", "MySql.EMTrace.EMTraceListener", false, BindingFlags.CreateInstance, null, new object[] { host, postInterval }, null, null, null).Unwrap();
                if (listener == null)
                {
                    throw new MySqlException(Resources.UnableToEnableQueryAnalysis);
                }
                source.Listeners.Add(listener);
                Switch.Level = ~SourceLevels.Off;
            }
        }

        internal static void LogError(int id, string msg)
        {
            Source.TraceEvent(TraceEventType.Error, id, msg, new object[] { MySqlTraceEventType.NonQuery, -1 });
            Trace.TraceError(msg);
        }

        internal static void LogInformation(int id, string msg)
        {
            Source.TraceEvent(TraceEventType.Information, id, msg, new object[] { MySqlTraceEventType.NonQuery, -1 });
            Trace.TraceInformation(msg);
        }

        internal static void LogWarning(int id, string msg)
        {
            Source.TraceEvent(TraceEventType.Warning, id, msg, new object[] { MySqlTraceEventType.NonQuery, -1 });
            Trace.TraceWarning(msg);
        }

        internal static void TraceEvent(TraceEventType eventType, MySqlTraceEventType mysqlEventType, string msgFormat, params object[] args)
        {
            Source.TraceEvent(eventType, (int) mysqlEventType, msgFormat, args);
        }

        public static TraceListenerCollection Listeners
        {
            get
            {
                return source.Listeners;
            }
        }

        public static bool QueryAnalysisEnabled
        {
            get
            {
                return qaEnabled;
            }
        }

        internal static TraceSource Source
        {
            get
            {
                return source;
            }
        }

        public static SourceSwitch Switch
        {
            get
            {
                return source.Switch;
            }
            set
            {
                source.Switch = value;
            }
        }
    }
}

