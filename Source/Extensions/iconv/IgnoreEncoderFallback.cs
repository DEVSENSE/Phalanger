using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace PHP.Library.Iconv
{
    internal class IgnoreEncoderFallback : EncoderFallback
    {


        public override EncoderFallbackBuffer CreateFallbackBuffer()
        {
            return new IgnoreEncoderFallbackBuffer(this);
        }

        public override int MaxCharCount
        {
            get { return 0; }
        }


    }

    internal class IgnoreEncoderFallbackBuffer : EncoderFallbackBuffer
    {
        public IgnoreEncoderFallbackBuffer(IgnoreEncoderFallback fallback)
        {

        }

        public override bool Fallback(char charUnknownHigh, char charUnknownLow, int index)
        {
            return true;
        }

        public override bool Fallback(char charUnknown, int index)
        {
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
