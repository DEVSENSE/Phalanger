using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace PHP.Library.Iconv
{
    internal class EncoderResult
    {
        public int firstFallbackCharIndex = -1;
    }

    internal class StopEncoderFallback : EncoderFallback
    {
        internal EncoderResult result;
        public StopEncoderFallback(EncoderResult result)
        {
            this.result = result;
        }

        public override EncoderFallbackBuffer CreateFallbackBuffer()
        {
            return new StopEncoderFallbackBuffer(this);
        }

        public override int MaxCharCount
        {
            get { return 0; }
        }


    }

    internal class StopEncoderFallbackBuffer : EncoderFallbackBuffer
    {
        private EncoderResult/*!*/result;

        public StopEncoderFallbackBuffer(StopEncoderFallback fallback)
        {
            this.result = fallback.result ?? new EncoderResult();
        }

        public override bool Fallback(char charUnknownHigh, char charUnknownLow, int index)
        {
            return Fallback(charUnknownHigh, index);
        }

        public override bool Fallback(char charUnknown, int index)
        {
            if (result.firstFallbackCharIndex < 0)
            {
                // TODO: Stop encoding the remaining characters
                result.firstFallbackCharIndex = index;
            }

            return true;
        }

        public override char GetNextChar()
        {
            return '\0';
        }

        public override bool MovePrevious()
        {
            return false;
        }

        public override int Remaining
        {
            get { return 0; }
        }
    }
}
