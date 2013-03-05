using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace PHP.Library.Iconv
{
    internal class TranslitEncoderFallback : EncoderFallback
    {


        public override EncoderFallbackBuffer CreateFallbackBuffer()
        {
            return new TranslitEncoderFallbackBuffer(this);
        }

        public override int MaxCharCount
        {
            get { return TranslitEncoderFallbackBuffer.transliterationsMaxCharCount; }
        }

        
    }

    internal class TranslitEncoderFallbackBuffer : EncoderFallbackBuffer
    {
        /// <summary>
        /// String that will be returned as the replacement for the fallbacked character.
        /// </summary>
        private string currentReplacement = null;

        /// <summary>
        /// Index in the <see cref="currentReplacement"/>.
        /// </summary>
        private int currentReplacementIndex;

        private bool IsIndexValid(int index)
        {
            return (currentReplacement != null && index >= 0 && index < currentReplacement.Length);
        }

        private static Dictionary<char, string>/*!!*/transliterations;
        internal static int transliterationsMaxCharCount;

        static TranslitEncoderFallbackBuffer()
        {
            transliterations = new Dictionary<char, string>(3900);

            // initialize the transliterations table:

            // load "translit.def" file content:
            using (var translit = new System.IO.StringReader(Strings.translit))
            {
                string line;
                while ((line = translit.ReadLine()) != null)
                {
                    // remove comments:
                    int cut_from = line.IndexOf('#');
                    if (cut_from >= 0) line = line.Remove(cut_from);

                    // skip empty lines:
                    if (line.Length == 0) continue;

                    //
                    string[] parts = line.Split('\t');  // HEX\tTRANSLIT\t
                    Debug.Assert(parts != null && parts.Length == 3);

                    int charNumber = int.Parse(parts[0], System.Globalization.NumberStyles.HexNumber);
                    string str = parts[1];

                    if (transliterationsMaxCharCount < str.Length)
                        transliterationsMaxCharCount = str.Length;

                    transliterations[(char)charNumber] = str;
                }
            }
        }

        public TranslitEncoderFallbackBuffer(TranslitEncoderFallback fallback)
        {

        }

        public override bool Fallback(char charUnknownHigh, char charUnknownLow, int index)
        {
            return false;
        }

        public override bool Fallback(char charUnknown, int index)
        {
            if (transliterations.TryGetValue(charUnknown, out currentReplacement))
            {
                currentReplacementIndex = -1;
                return true;
            }
            else
            {
                return false;
            }
        }

        public override char GetNextChar()
        {
            ++currentReplacementIndex;

            if (IsIndexValid(currentReplacementIndex))
                return currentReplacement[currentReplacementIndex];
            else
                return '\0';
        }

        public override bool MovePrevious()
        {
            if (currentReplacementIndex >= 0 && currentReplacement != null)
            {
                currentReplacementIndex--;
                return true;
            }

            return false;
        }

        public override int Remaining
        {
            get { return IsIndexValid(currentReplacementIndex + 1) ? (currentReplacement.Length - currentReplacementIndex - 1) : 0; }
        }
    }
}
