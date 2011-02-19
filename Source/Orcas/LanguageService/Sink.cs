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
using System.Diagnostics.CodeAnalysis;

//using Hosting = IronPython.Hosting;

using Microsoft.VisualStudio.Package;
using Microsoft.VisualStudio.TextManager.Interop;
using PHP.Core;

namespace PHP.VisualStudio.PhalangerLanguageService 
{
    /// <summary>
    /// Translate Phalangers ErrorSink.Add to AuthoringSink.AddError.
    /// </summary>
	public class ParserErrorSink : ErrorSink
	{
		private AuthoringSink/*!*/ authoringSink;

        private int sinkErrorsCount = 0;

        /// <summary>
        /// Amount of errors added into the authoring sink.
        /// </summary>
        public int SinkErrorsCount
        {
            get
            {
                return sinkErrorsCount;
            }
        }

		public ParserErrorSink(AuthoringSink/*!*/ authoringSink)
		{
			Debug.Assert(authoringSink != null);
			this.authoringSink = authoringSink;
		}

		private Severity ToVsSeverity(ErrorSeverity severity)
		{
			switch (severity.Value)
			{
				case ErrorSeverity.Values.Error: return Severity.Error;
				case ErrorSeverity.Values.Warning: return Severity.Warning;
				case ErrorSeverity.Values.FatalError: return Severity.Error;
			}
            return 0;
		}

		protected override bool Add(int id, string message, ErrorSeverity severity, int group, string fullPath, ErrorPosition pos)
		{
			TextSpan span = new TextSpan();
			if (pos.IsValid)
			{
				span.iStartLine = pos.FirstLine - 1;
				span.iStartIndex = pos.FirstColumn - 1;
				span.iEndLine = pos.LastLine - 1;
				span.iEndIndex = pos.LastColumn;
			}

			authoringSink.AddError(fullPath, message, span, ToVsSeverity(severity));

            ++sinkErrorsCount;

			return true;
		}
	}

    /// <summary>
    /// Empty Error sink.
    /// </summary>
    public class EmptyErrorSink : ErrorSink
    {
        public EmptyErrorSink()
        {
        }
    
        protected override bool Add(int id, string message, ErrorSeverity severity, int group, string fullPath, ErrorPosition pos)
        {
            return true;
        }
    }
/*	
	public class PythonSink : IronPython.Hosting.CompilerSink {
		AuthoringSink authoringSink;

		public PythonSink(AuthoringSink authoringSink) {
			this.authoringSink = authoringSink;
		}

		private static TextSpan CodeToText(Hosting.CodeSpan code) {
			TextSpan span = new TextSpan();
			if (code.StartLine > 0) {
				span.iStartLine = code.StartLine - 1;
			}
			span.iStartIndex = code.StartColumn;
			if (code.EndLine > 0) {
				span.iEndLine = code.EndLine - 1;
			}
			span.iEndIndex = code.EndColumn;
			return span;
		}

		public override void AddError(string path, string message, string lineText, Hosting.CodeSpan location, int errorCode, Hosting.Severity severity) {
			TextSpan span = new TextSpan();
			if (location.StartLine > 0) {
				span.iStartLine = location.StartLine - 1;
			}
			span.iStartIndex = location.StartColumn;
			if (location.EndLine > 0) {
				span.iEndLine = location.EndLine - 1;
			}
			span.iEndIndex = location.EndColumn;
			authoringSink.AddError(path, message, span, Severity.Error);
		}

		[SuppressMessage("Microsoft.Naming", "CA1725:ParameterNamesShouldMatchBaseDeclaration")]
		public override void MatchPair(Hosting.CodeSpan span, Hosting.CodeSpan endContext, int priority) {
			authoringSink.MatchPair(CodeToText(span), CodeToText(endContext), priority);
		}

		public override void EndParameters(Hosting.CodeSpan span) {
			authoringSink.EndParameters(CodeToText(span));
		}

		public override void NextParameter(Hosting.CodeSpan span) {
			authoringSink.NextParameter(CodeToText(span));
		}

		public override void QualifyName(Hosting.CodeSpan selector, Hosting.CodeSpan span, string name) {
			authoringSink.QualifyName(CodeToText(selector), CodeToText(span), name);
		}

		public override void StartName(Hosting.CodeSpan span, string name) {
			authoringSink.StartName(CodeToText(span), name);
		}

		[SuppressMessage("Microsoft.Naming", "CA1725:ParameterNamesShouldMatchBaseDeclaration")]
		public override void StartParameters(Hosting.CodeSpan span) {
			authoringSink.StartParameters(CodeToText(span));
		}
	}*/
}
