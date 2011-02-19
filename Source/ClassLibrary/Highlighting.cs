/*

 Copyright (c) 2005-2006 Tomas Matousek.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/
using System;
using System.Collections;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;

using PHP;
using PHP.Core;
using PHP.Core.Parsers;

#if SILVERLIGHT
using System.Windows.Browser;
#else
using System.Web;
#endif

namespace PHP.Library
{
	/// <summary>
	/// Functions for highlighting PHP source code written as HTML.
	/// </summary>
	public static class Highlighting
	{
		#region highlight_file, highlight_string, show_source

		/// <summary>
		/// Writes PHP source code from file highlighted to output.
		/// </summary>
		/// <param name="fileName">PHP source file to highlight.</param>
		/// <returns><B>True</B> if the source code was succesfuly highlighted.</returns>
		[ImplementsFunction("highlight_file")]
		public static object HighlightFile(string fileName)
		{
			return HighlightFile(fileName, false);
		}

		/// <summary>
		/// Writes PHP source code from file highlighted to output or returns this code as string
		/// according to second parameter.
		/// </summary>
		/// <param name="fileName">PHP source file to highlight.</param>
		/// <param name="returnHighlighted"><B>True</B> if highlighted code should be returned,
		/// <B>false</B> if it should be printed out.</param>
		/// <returns>Highlighted source code or - if it is printed out - <B>true</B> if the code was
		/// highlighted succesfuly, false otherwise.</returns>
		[ImplementsFunction("highlight_file")]
		public static object HighlightFile(string fileName, bool returnHighlighted)
		{
			using (PhpStream stream = PhpStream.Open(fileName, "rt"))
			{
				if (stream == null)
					return false;

				string source = stream.ReadStringContents(-1);
				Debug.Assert(source != null);

				return HighlightString(source, returnHighlighted);
			}
		}

		/// <summary>
		/// Writes highlighted PHP source code passed as string to output.
		/// </summary>
		/// <param name="str">PHP source code to highlight</param>
		/// <returns><B>True</B> if the source code was successfully highlighted.</returns>
		[ImplementsFunction("highlight_string")]
		public static object HighlightString(string str)
		{
			return HighlightString(str, false);
		}

		/// <summary>
		/// Writes PHP source code highlighted to output or returns this code as string
		/// according to second parameter.
		/// </summary>
		/// <param name="str">PHP source code</param>
		/// <param name="returnHighlighted"><B>True</B> if highlighted code should be returned,
		/// <B>false</B> if it should be printed out.</param>
		/// <returns>Highlighted source code or - if it is printed out - <B>true</B> if the code was
		/// highlighted succesfuly, false otherwise.</returns>
		[ImplementsFunction("highlight_string")]
		public static object HighlightString(string str, bool returnHighlighted)
		{
			if (str == null) str = "";

			ScriptContext context = ScriptContext.CurrentContext;
			LibraryConfiguration config = LibraryConfiguration.GetLocal(context);

			TextWriter output = returnHighlighted ? new StringWriter() : context.Output;

			bool success = Highlight(str, output, config);

			if (returnHighlighted)
				return output.ToString();
			else
				return success;
		}

		/// <summary>
		/// Alias for <see cref="HighlightFile"/>.
		/// </summary>
		[ImplementsFunction("show_source")]
		public static object ShowSource(string fileName)
		{
			return HighlightFile(fileName, false);
		}

		/// <summary>
		/// Alias for <see cref="HighlightFile"/>.
		/// </summary>
		[ImplementsFunction("show_source")]
		public static object ShowSource(string fileName, bool returnHighlighted)
		{
			return HighlightFile(fileName, returnHighlighted);
		}

		#endregion

		public static bool Highlight(string/*!*/ code, TextWriter/*!*/ output, LibraryConfiguration/*!*/ config)
		{
			if (code == null)
				throw new ArgumentNullException("code");
			if (output == null)
				throw new ArgumentNullException("output");
			if (config == null)
				throw new ArgumentNullException("config");

			Tokenizer.Features features = Tokenizer.Features.Default | Tokenizer.Features.ContextKeywords;

			Tokenizer tokenizer = new Tokenizer(new StringReader(code), features);
			Tokens token;

			output.Write("<pre>");
			output.Write("<span style='color:");
			output.Write(config.Highlighting.Background);
			output.Write("'>");

			for (; ; )
			{
				token = tokenizer.GetNextToken();

				if (token == Tokens.ERROR || token == Tokens.EOF) break;

				string fcolor = config.Highlighting.Default;
				bool is_bold = false;

				switch (tokenizer.TokenCategory)
				{
					case TokenCategory.Unknown:
					case TokenCategory.Text:
					case TokenCategory.Delimiter:
					case TokenCategory.Number:
					case TokenCategory.Identifier:
					case TokenCategory.Operator:
					case TokenCategory.WhiteSpace:
						break;

					case TokenCategory.Html:
						fcolor = config.Highlighting.Html;
						break;

					case TokenCategory.Comment:
					case TokenCategory.LineComment:
						fcolor = config.Highlighting.Comment;
						break;

					case TokenCategory.ScriptTags:
						fcolor = config.Highlighting.ScriptTags;
						break;

					case TokenCategory.Keyword:
						fcolor = config.Highlighting.Keyword;
						break;

					case TokenCategory.StringCode:
						is_bold = true;
						fcolor = config.Highlighting.String;
						break;

					case TokenCategory.String:
						fcolor = config.Highlighting.String;
						break;
				}

				output.Write("<span style='color:");
				output.Write(fcolor);
				output.Write("'>");
				if (is_bold) output.Write("<b>");

				output.Write(HttpUtility.HtmlEncode(tokenizer.TokenText));

				if (is_bold) output.Write("</b>");
				output.Write("</span>");
			}

			output.Write("</pre>");
			return token != Tokens.ERROR;
		}

	}
}