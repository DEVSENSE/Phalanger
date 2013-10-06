/*

 Copyright (c) 2004-2006 Tomas Matousek and Ladislav Prosek.
  
 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.IO;
using System.Threading;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace PHP.Core.Parsers
{
    public static class Convert
    {
        /// <summary>
        /// Converts a character to a digit.
        /// </summary>
        /// <param name="c">The character [0-9A-Za-z].</param>
        /// <returns>The digit represented by the character or <see cref="Int32.MaxValue"/> 
        /// on non-alpha-numeric characters.</returns>
        public static int AlphaNumericToDigit(char c)
        {
            if (c >= '0' && c <= '9')
                return (int)(c - '0');

            if (c >= 'a' && c <= 'z')
                return (int)(c - 'a') + 10;

            if (c >= 'A' && c <= 'Z')
                return (int)(c - 'A') + 10;

            return Int32.MaxValue;
        }

        /// <summary>
        /// Converts a character to a digit.
        /// </summary>
        /// <param name="c">The character [0-9].</param>
        /// <returns>The digit represented by the character or <see cref="Int32.MaxValue"/> 
        /// on non-numeric characters.</returns>
        public static int NumericToDigit(char c)
        {
            if (c >= '0' && c <= '9')
                return (int)(c - '0');

            return Int32.MaxValue;
        }
    }
}
