/*

 Copyright (c) 2013 DEVSENSE
  
 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace PHP.Core.Text
{
    #region TextUtils

    public static class TextUtils
    {
        /// <summary>
        /// Gets length of line break character sequence if any.
        /// </summary>
        /// <param name="text">Document text.</param>
        /// <param name="position">Index of character within <paramref name="text"/> to look at.</param>
        /// <returns>Length of line break character sequence at <paramref name="position"/>. In case of no line break, <c>0</c> is returned.</returns>
        public static int LengthOfLineBreak(string text, int position)
        {
            char c = text[position];
            if (c == '\r')
            {
                // \r
                if (++position >= text.Length || text[position] != '\n')
                    return 1;

                // \r\n
                return 2;
            }
            else
            {
                // \n
                // unicode line breaks
                if (c == '\n' || c == '\u0085' || c == '\u2028' || c == '\u2029')
                    return 1;

                return 0;
            }
        }
    }

    #endregion
}
