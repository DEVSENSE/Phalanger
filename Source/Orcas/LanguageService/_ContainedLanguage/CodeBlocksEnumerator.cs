/***************************************************************************

Copyright (c) Microsoft Corporation. All rights reserved.
This code is licensed under the Visual Studio SDK license terms.
THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.

***************************************************************************/

using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.VisualStudio.TextManager.Interop;

using Microsoft.Samples.VisualStudio.IronPythonInference;
using IronPython.Compiler;
using IronPython.Compiler.Ast;

using VSConstants = Microsoft.VisualStudio.VSConstants;
using ErrorHandler = Microsoft.VisualStudio.ErrorHandler;
using SystemState = IronPython.Runtime.SystemState;

namespace PHP.VisualStudio.PhalangerLanguageService {
    internal class CodeBlocksEnumerator : IVsEnumCodeBlocks {
        private int currentIndex;
        private List<TextSpanAndCookie> blocks;
        private Tokenizer tokenizer;

        private const string beginExternalSource = "ExternalSource";
        private const string beginSnippetStm = "Snippet Statement";
        private const string endSnippetStm = "End Snippet Statement";
        private const string beginSnippetMember = "begin snippet member";
        private const string endSnippetMember = "end snippet member";
        private const string endExternalSource = "End ExternalSource";

        // This class implements a very simple parser based on the IronPython tokenizer.
        // This enumeration is the list of the states of the parser.
        private enum SimpleParserState {
            WaitForExternalSource,
            WaitForSnippet,
            WaitForEndSnippet,
            WaitForEndExternal
        }

        /// <summary>
        /// Builds a new enumerator from a text buffer.
        /// </summary>
        public CodeBlocksEnumerator(IVsTextLines buffer) {
            if (null == buffer) {
                throw new ArgumentNullException("buffer");
            }
            blocks = new List<TextSpanAndCookie>();
            SearchForCodeBlocks(buffer);
        }

        /// <summary>
        /// Private constructor used for the Clone functionality.
        /// </summary>
        private CodeBlocksEnumerator(CodeBlocksEnumerator original) {
            this.currentIndex = original.currentIndex;
            this.blocks = new List<TextSpanAndCookie>(original.blocks);
        }

        #region Parser
        private void SearchForCodeBlocks(IVsTextLines buffer) {
            // We don't want any change in the buffer while we are parsing,
            // so we have to lock it.
            ErrorHandler.ThrowOnFailure(buffer.LockBufferEx((uint)BufferLockFlags.BLF_READ));
            try {
                // Find the total number of lines in the buffer.
                int totalLines;
                ErrorHandler.ThrowOnFailure(buffer.GetLineCount(out totalLines));
                // Set the initial values for the variables used during the parsing.
                SimpleParserState state = SimpleParserState.WaitForExternalSource;
                TextSpanAndCookie blockSpan = new TextSpanAndCookie();

                // Parse all the lines in the buffer
                for (int line = 0; line < totalLines; ++line) {
                    // Get the text of the current line.
                    int lineLen;
                    ErrorHandler.ThrowOnFailure(buffer.GetLengthOfLine(line, out lineLen));
                    if (0 == lineLen) {
                        // The line is empty, no point in parsing it.
                        continue;
                    }
                    string lineText;
                    ErrorHandler.ThrowOnFailure(buffer.GetLineText(line, 0, line, lineLen, out lineText));

                    // Create the tokenizer.
                    CompilerContext context = new CompilerContext("", new QuietCompilerSink());
                    using (SystemState systemState = new SystemState()) {
                        tokenizer = new Tokenizer(lineText.ToCharArray(), true, systemState, context);

                        Token token = null;
                        string commentText;

                        // Read all the token looking for the code blocks inside a Snippet Statements
                        // nested in an External Source statement. Note that the standard IronPython
                        // parser does not return such statements and this is the reason for this
                        // parser.
                        while (!tokenizer.IsEndOfFile) {
                            token = tokenizer.Next();

                            // This parser is strange in that it is only interested in comments:
                            // an external code statement is in the form
                            //     #ExternalSource("PathOfTheOriginalFile", originalLineNumber)
                            //     ... (some code) ...
                            //     #End ExternalSource
                            // and a snippet statement is
                            //     # Snippet Statement
                            //     ... (some code) ...
                            //     #End Snippet Statement
                            // So if we want to find the text region inside a snippet nested
                            // inside an external source, we are only interested in the comment tokens.

                            if (TokenKind.Comment != token.Kind) {
                                continue;
                            }

                            // The comments are line comments, so the comment's text is everything that
                            // is after the beginning of the comment.
                            commentText = CleanCommentStart(lineText.Substring(tokenizer.StartLocation.Column));
                            if (string.IsNullOrEmpty(commentText)) {
                                continue;
                            }

                            switch (state) {
                                case SimpleParserState.WaitForExternalSource:
                                    // This function returns a non zero value only if the comment text
                                    // is a valid external source statment.
                                    blockSpan.ulHTMLCookie = ParseExternalSource(commentText);
                                    if (0 != blockSpan.ulHTMLCookie) {
                                        // The CodeDOM provider is adding 1 to the line number, but in this
                                        // case the number is actualy the HTML editor's cookie, so we have to
                                        // restore the original value.
                                        blockSpan.ulHTMLCookie -= 1;
                                        state = SimpleParserState.WaitForSnippet;
                                    }
                                    break;

                                case SimpleParserState.WaitForSnippet:
                                    // Check if this comment is the beginning of a snippet block.
                                    if (IsBeginSnippet(commentText)) {
                                        // This is the beginning of a snippet block, so
                                        // the actual code will start at the beginning of the
                                        // next line.
                                        blockSpan.CodeSpan.iStartLine = line + 1;
                                        // Set the default for the start index.
                                        blockSpan.CodeSpan.iStartIndex = 0;

                                        // Now we have to find the end of the snippet section
                                        // to complete the span of the code.
                                        state = SimpleParserState.WaitForEndSnippet;
                                    } else if (IsEndExternalSource(commentText)) {
                                        // This was and external block not related to the HTML editor.
                                        // Reset the text span and wait for the next external source.
                                        blockSpan = new TextSpanAndCookie();
                                        state = SimpleParserState.WaitForExternalSource;
                                    }
                                    break;

                                case SimpleParserState.WaitForEndSnippet:
                                    if (IsEndSnippet(commentText)) {
                                        // The code block ends at the end of the line before
                                        // this token.
                                        // Update the data about the code span and add the
                                        // block to the list of the blocks found.
                                        blockSpan.CodeSpan.iEndLine = line - 1;
                                        ErrorHandler.ThrowOnFailure(
                                            buffer.GetLengthOfLine(line - 1, out blockSpan.CodeSpan.iEndIndex));
                                        blocks.Add(blockSpan);

                                        blockSpan = new TextSpanAndCookie();
                                        state = SimpleParserState.WaitForEndExternal;
                                    }
                                    break;

                                case SimpleParserState.WaitForEndExternal:
                                    // We expect only one snippet block inside the external source
                                    // section, so here we skip everything between the end of the first
                                    // snippet block and the end of the external code section.
                                    if (IsEndExternalSource(commentText)) {
                                        state = SimpleParserState.WaitForExternalSource;
                                    }
                                    break;
                            }
                        }
                    }
                }
            } finally {
                // Make sure that the buffer is always unlocked when we exit this function.
                buffer.UnlockBufferEx((uint)BufferLockFlags.BLF_READ);
            }
        }

        // This is a very simple parsing function that check if a string is in the form
        // ExternalSource("some string", 123) and returns the number.
        // Returns 0 if the string is not in the right format.
        private static uint ParseExternalSource(string text) {
            //const string externalSourceTag = "ExternalSource";
            int index = 0;
            // Verify that the 'ExternalSource' token is present.
            if (!MatchToken(text, index, beginExternalSource)) {
                return 0;
            }
            index += beginExternalSource.Length;
            // Skip whitespaces before '('
            index = SkipWhitespaces(text, index);
            if ((index == text.Length) || (text[index] != '(')) {
                return 0;
            }
            // Skip whitespaces before '"'
            index = SkipWhitespaces(text, index + 1);
            if ((index == text.Length) || (text[index] != '"')) {
                return 0;
            }
            // Move after the '"'
            ++index;
            // Skip all the chars up to the next '"'
            while ((index < text.Length) && (text[index] != '"')) {
                ++index;
            }
            if (index == text.Length) {
                return 0;
            }
            // Skip whitespaces before ','
            index = SkipWhitespaces(text, index + 1);
            if ((index == text.Length) || (text[index] != ',')) {
                return 0;
            }
            // Skip whitespaces
            index = SkipWhitespaces(text, index + 1);
            if (index == text.Length) {
                return 0;
            }
            // Build the string that stores the line number.
            StringBuilder numberBuilder = new StringBuilder(text.Length - index);
            while ((index < text.Length) && char.IsDigit(text[index])) {
                numberBuilder.Append(text[index]);
                ++index;
            }
            if ((index == text.Length) || (numberBuilder.Length == 0)) {
                return 0;
            }
            uint lineNumber = 0;
            try {
                lineNumber = uint.Parse(numberBuilder.ToString(), System.Globalization.CultureInfo.InvariantCulture);
            } catch (System.FormatException) {
                return 0;
            }
            // Make sure that after the number there is the right parentesis.
            index = SkipWhitespaces(text, index);
            if ((index == text.Length) || (text[index] != ')')) {
                return 0;
            }
            return lineNumber;
        }

        private static bool IsBeginSnippet(string text) {
            return StartsWithToken(text, beginSnippetStm) || StartsWithToken(text, beginSnippetMember);
        }

        private static bool IsEndSnippet(string text) {
            return StartsWithToken(text, endSnippetStm) || StartsWithToken(text, endSnippetMember);
        }

        private static bool IsEndExternalSource(string text) {
            return StartsWithToken(text, endExternalSource);
        }

        // Verify that a string starts with a specific token and that after the token
        // there is at least one white space or the end of the string.
        private static bool StartsWithToken(string text, string token) {
            // Make sure that the comment begins with the end snippet token.
            if (!MatchToken(text, 0, token)) {
                return false;
            }
            int index = token.Length;
            // Make sure that there is at least one space (or the end of the comment)
            // after the end of the token.
            index = SkipWhitespaces(text, index);
            if ((index == text.Length) || (index > token.Length)) {
                return true;
            }
            return false;
        }

        private static string CleanCommentStart(string text) {
            if (string.IsNullOrEmpty(text)) {
                return null;
            }
            int index = 0;
            while ((index < text.Length) && (char.IsWhiteSpace(text[index]) || text[index] == '#')) {
                ++index;
            }
            if (index == text.Length) {
                return null;
            }
            return text.Substring(index);
        }

        private static int SkipWhitespaces(string text, int startIndex) {
            int index = startIndex;
            while ((index < text.Length) && char.IsWhiteSpace(text[index])) {
                ++index;
            }
            return index;
        }

        private static bool MatchToken(string text, int startIndex, string token) {
            int stringIndex = startIndex;
            int tokenIndex = 0;

            while ((stringIndex < text.Length) && (tokenIndex < token.Length) &&
                   (text[stringIndex] == token[tokenIndex])) {
                ++stringIndex;
                ++tokenIndex;
            }
            if (tokenIndex < token.Length) {
                return false;
            }
            return true;
        }
        #endregion

        public int Clone(out IVsEnumCodeBlocks ppEnum) {
            ppEnum = new CodeBlocksEnumerator(this);
            return VSConstants.S_OK;
        }

        public int Next(uint celt, TextSpanAndCookie[] rgelt, out uint pceltFetched) {
            if (0 == celt) {
                pceltFetched = 0;
                return VSConstants.S_OK;
            }
            if ((null == rgelt) || (0 == rgelt.Length)) {
                throw new ArgumentNullException("rgelt");
            }
            if (rgelt.Length < celt) {
                throw new System.ArgumentException("rgelt");
            }
            pceltFetched = 0;
            while ((currentIndex < blocks.Count) && (pceltFetched<celt)) {
                rgelt[pceltFetched].ulHTMLCookie = blocks[currentIndex].ulHTMLCookie;
                rgelt[pceltFetched].CodeSpan = blocks[currentIndex].CodeSpan;
                ++currentIndex;
                ++pceltFetched;
            }
            return (pceltFetched==celt) ? VSConstants.S_OK : VSConstants.S_FALSE;
        }

        public int Reset() {
            currentIndex = 0;
            return VSConstants.S_OK;
        }

        public int Skip(uint celt) {
            currentIndex += (int)celt;
            if ((currentIndex > blocks.Count) || (currentIndex < 0)) {
                currentIndex = blocks.Count;
            }
            return VSConstants.S_OK;
        }
    }
}
