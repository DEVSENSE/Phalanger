/*

 Copyright (c) 2006 Tomas Matousek.  

 Copyright (c) 2008 Jakub Misek.
 
 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TextManager.Interop;
using PHP.Core.Parsers;
using PHP.Core;

namespace PHP.VisualStudio
{
    /// <summary>
    /// Helper object with small helper methods.
    /// 
    /// Converting and modifying position spans,
    /// creating qualified names.
    /// </summary>
	internal static class Utils
	{
        /// <summary>
        /// Checks if the given position is in the TextSpan.
        /// </summary>
        /// <param name="line">Line position to check.</param>
        /// <param name="col">Column position to check.</param>
        /// <param name="span"></param>
        /// <returns></returns>
		internal static bool IsInSpan(int line, int col, TextSpan span)
		{
            return !(line < span.iStartLine || line > span.iEndLine ||
                (line == span.iStartLine && col < span.iStartIndex) ||
                (line == span.iEndLine && col > span.iEndIndex));
		}

        /// <summary>
        /// Checks if the given position is in the Position.
        /// </summary>
        /// <param name="line">Line position to check.</param>
        /// <param name="col">Column position to check.</param>
        /// <param name="position"></param>
        /// <returns></returns>
		internal static bool IsInPosition(int line, int col, Position position)
		{
			return line > position.FirstLine - 1 && line < position.LastLine - 1 ||
				(line == position.FirstLine - 1 && col >= position.FirstColumn - 1) ||
				(line == position.LastLine - 1 && col <= position.LastColumn);
		}

        /// <summary>
        /// Converts the Position into the TextSpan.
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
		internal static TextSpan PositionToSpan(Position position)
		{
			TextSpan span = new TextSpan();
			span.iStartLine = position.FirstLine - 1;
			span.iStartIndex = position.FirstColumn - 1;
			span.iEndLine = position.LastLine - 1;
			span.iEndIndex = position.LastColumn;
			return span;
		}

        /// <summary>
        /// Move the given position by a TextLineChange.
        /// </summary>
        /// <param name="line">Source line position.</param>
        /// <param name="col">Source column position.</param>
        /// <param name="newline">Changed line position.</param>
        /// <param name="newcol">Changed column position.</param>
        /// <param name="change">The text line change information.</param>
        /// <returns>True if a change was made, false otherwise.</returns>
        internal static bool ChangeCoord(int line, int col, out int newline, out int newcol, TextLineChange change)
        {
            // no change
            if ((line < change.iStartLine) ||
                (line == change.iStartLine && col < change.iStartIndex))
            {
                newline = line;
                newcol = col;

                return false; // no change
            }

            // line move
            if (line > change.iOldEndLine)
            {
                newline = line + (change.iNewEndLine - change.iOldEndLine);
                newcol = col;
                return true; // line move
            }

            // other change
            TextSpan oldspan = new TextSpan();
            TextSpan newspan = new TextSpan();

            oldspan.iStartLine = newspan.iStartLine = change.iStartLine;
            oldspan.iStartIndex = newspan.iStartIndex = change.iStartIndex;
            oldspan.iEndLine = change.iOldEndLine;
            oldspan.iEndIndex = change.iOldEndIndex;
            newspan.iEndLine = change.iNewEndLine;
            newspan.iEndIndex = change.iNewEndIndex;
            
            if ( IsInSpan(line,col,oldspan) )
            {
                if ( IsInSpan(line,col,newspan) &&
                    (line != change.iOldEndLine || col != change.iOldEndIndex))
                {
                    newline = line;
                    newcol = col;
                    return false;   // no change
                }
                else
                {   // new is smaller
                    newline = change.iNewEndLine;
                    newcol = change.iNewEndIndex;
                    return true;
                }
            }
            else
            {
                newline = change.iNewEndLine;
                newcol = (col - change.iOldEndIndex) + change.iNewEndIndex;
                return true;                
            }
            
        }

        /// <summary>
        /// Update TextSpan by a TextLineChange.
        /// </summary>
        /// <param name="change">The text line change information.</param>
        /// <param name="span">Source TextSpan.</param>
        /// <param name="newSpan">Changed TextSpan.</param>
        /// <returns>True if a change was made, otherwise false.</returns>
        internal static bool UpdateSpan(TextLineChange change, TextSpan span, out TextSpan newSpan)
        {
            newSpan = span;

            if ((span.iEndLine < change.iStartLine) ||
                (span.iEndLine == change.iStartLine && span.iEndIndex <= change.iStartIndex))
                return false;

            bool bChanged = false;

            bChanged |= Utils.ChangeCoord(span.iStartLine, span.iStartIndex, out newSpan.iStartLine, out newSpan.iStartIndex, change);
            bChanged |= Utils.ChangeCoord(span.iEndLine, span.iEndIndex, out newSpan.iEndLine, out newSpan.iEndIndex, change);

            //
            return bChanged;
        }

        /// <summary>
        /// Make one TextSpan from two TextSpans.
        /// </summary>
        /// <param name="span1">First TextSpan.</param>
        /// <param name="span2">Second TextSpan.</param>
        /// <returns>Unification of given TextSpans.</returns>
        internal static TextSpan UniteSpans(TextSpan span1, TextSpan span2)
        {
            TextSpan span = span1;

            if (span2.iStartLine < span.iStartLine)
            {
                span.iStartLine = span2.iStartLine;
                span.iStartIndex = span2.iStartIndex;
            }
            else if (span2.iStartLine == span.iStartLine)
            {
                if (span2.iStartIndex < span.iStartIndex)
                    span.iStartIndex = span2.iStartIndex;
            }

            if (span2.iEndLine > span.iEndLine)
            {
                span.iEndLine = span2.iEndLine;
                span.iEndIndex = span2.iEndIndex;
            }
            else if (span2.iEndLine == span.iEndLine)
            {
                if (span2.iEndIndex < span.iEndIndex)
                    span.iEndIndex = span2.iEndIndex;
            }

            return span;
        }

        /// <summary>
        /// Make QualifiedName from the string like AAA:::BBB:::XXX
        /// </summary>
        /// <param name="name">Full namespace name string.</param>
        /// <param name="namespaceSeparator">Namespace separator.</param>
        /// <returns>Qualified name.</returns>
        internal static QualifiedName MakeQualifiedName(string name, string namespaceSeparator)
        {
            if (name == null || name.Length == 0)
                return new QualifiedName();

            string[] names = name.Split(new string[] { namespaceSeparator }, StringSplitOptions.RemoveEmptyEntries);

            if (names.Length == 1)
            {
                return new QualifiedName(new Name(name));
            }
            else
            {
                Name[] namespaces = new Name[names.Length - 1];
                for (int i = 0; i < names.Length - 1; ++i)
                    namespaces[i] = new Name(names[i]);

                return new QualifiedName(new Name(names[names.Length - 1]), namespaces);
            }
        }

        /// <summary>
        /// Make QualifiedName from the string like AAA:::BBB:::XXX, using default Phalanger namespace separator.
        /// </summary>
        /// <param name="name">Full namespace name string.</param>
        /// <returns>Qualified name.</returns>
        internal static QualifiedName MakeQualifiedName(string name)
        {
            return MakeQualifiedName(name, QualifiedName.Separator);
        }
	}
}
