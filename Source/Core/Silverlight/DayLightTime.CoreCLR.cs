using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace PHP.CoreCLR
{
    public class DaylightTime
    {
        private DateTime _start;
        private DateTime _end;
        private TimeSpan _delta;

        private DaylightTime() { }

        public DaylightTime(DateTime start, DateTime end, TimeSpan delta)
        {
            _start = start;
            _end = end;
            _delta = delta;
        }

        public DateTime Start { get { return _start; } }
        public DateTime End { get { return _end; } }
        public TimeSpan Delta { get { return _delta; } }
    }

}
