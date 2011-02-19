/***************************************************************************

Copyright (c) Microsoft Corporation. All rights reserved.
This code is licensed under the Visual Studio SDK license terms.
THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.

***************************************************************************/

using System;
using System.Diagnostics.CodeAnalysis;

using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Package;

using PHP.VisualStudio.PhalangerLanguageService.Scopes;

namespace PHP.VisualStudio.PhalangerLanguageService 
{
    /// <summary>
    /// PHP source code.
    /// </summary>
    public partial class PhpSource : Source 
	{
        private PhpScope _scope = null;

        /// <summary>
        /// PHP scope associated with this source.
        /// </summary>
        internal PhpScope Scope
        {
            get
            {
                return _scope;
            }
            set
            {
                _scope = value;
            }
        }

        /// <summary>
        /// Source initialization.
        /// </summary>
        /// <param name="service">PHP Language service.</param>
        /// <param name="textLines">Source text.</param>
        /// <param name="colorizer">Source colorizer.</param>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public PhpSource(PhpLanguage service, IVsTextLines textLines, Colorizer colorizer)
			: base(service, textLines, colorizer)
        {
            
		}

        /// <summary>
        /// Gets information on what defines a comment in the language.
        /// </summary>
        /// <returns>A CommentInfo structure containing the strings that define a comment.</returns>
		public override CommentInfo GetCommentFormat()
        {
			CommentInfo ci = new CommentInfo();

			ci.UseLineComments = true;
            ci.BlockStart = "/*";
            ci.BlockEnd = "*/";
			ci.LineStart = "//";    // # too

			return ci;
		}

        public override void OnChangeLineText(TextLineChange[] lineChange, int last)
        {
            // update cached declarations
            ScopeInfo decls;

            if ((Scope != null) &&
                (decls = Scope.Scope) != null)
            {
                foreach (TextLineChange change in lineChange)
                    decls.UpdatePositions(change);
            }

            // 
            base.OnChangeLineText(lineChange, last);            
        }
        public override void Completion(IVsTextView textView, TokenInfo info, ParseReason reason)
        {
            base.Completion(textView, info, reason);
        }

        /*private void OnChar(IVsTextView textView, char ch)
        {
            char matchChar = '\0';

            switch (ch)
            {
                case '(':
                    matchChar = ')';
                    break;
                case '[':
                    matchChar = ']';
                    break;
                case '{':
                    matchChar = '}';
                    break;
                case '\'':
                    matchChar = '\'';
                    break;
                case '"':
                    matchChar = '"';
                    break;
                default:
                    return;
            }

            if (matchChar != '\0')
            {
                int l, c;
                textView.GetCaretPos(out l, out c);
                if (c < 2 || GetText(l, c - 2, l, c - 1) != "\\")
                {   // typed character is not after \
                    SetText(l, c, l, c, matchChar.ToString());
                    textView.SetCaretPos(l, c);
                }
            }
        }

        public override void OnCommand(IVsTextView textView, Microsoft.VisualStudio.VSConstants.VSStd2KCmdID command, char ch)
        {
            switch (command)
            {
                // insert matching character (brace,quote,...)
                case Microsoft.VisualStudio.VSConstants.VSStd2KCmdID.TYPECHAR:
                    OnChar(textView, ch);
                    break;
            } // problem with inserting snippets by pressing '('

            // do whatever it wants to be done
            base.OnCommand(textView, command, ch);
        }*/

	}
}
