namespace MySql.Data.Common
{
    using System;

    internal class LowResolutionStopwatch
    {
        public static readonly long Frequency = 0x3e8L;
        public static readonly bool isHighResolution = false;
        private long millis = 0L;
        private long startTime;

        public static long GetTimestamp()
        {
            return (long) Environment.TickCount;
        }

        private bool IsRunning()
        {
            return (this.startTime != 0L);
        }

        public void Reset()
        {
            this.millis = 0L;
            this.startTime = 0L;
        }

        public void Start()
        {
            this.startTime = Environment.TickCount;
        }

        public static LowResolutionStopwatch StartNew()
        {
            LowResolutionStopwatch stopwatch = new LowResolutionStopwatch();
            stopwatch.Start();
            return stopwatch;
        }

        public void Stop()
        {
            long tickCount = Environment.TickCount;
            long num2 = (tickCount < this.startTime) ? ((0x7fffffffL - this.startTime) + tickCount) : (tickCount - this.startTime);
            this.millis += num2;
        }

        public TimeSpan Elapsed
        {
            get
            {
                return new TimeSpan(0, 0, 0, 0, (int) this.millis);
            }
        }

        public long ElapsedMilliseconds
        {
            get
            {
                return this.millis;
            }
        }
    }
}

