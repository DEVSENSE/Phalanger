/*

 Copyright (c) 2006 Tomas Matousek.  

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

 TODO:
   filter - see url_scanner_ex.c
   session starts rewriter (session.use_trans_sid)
   url_rewriter.tags
*/

using System;
using System.Web;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Generic;

using PHP.Core;

namespace PHP.Library
{
	public sealed class UrlRewriter
	{
		#region Construction and Properties

		private UrlRewriter()
		{
		}

		private Dictionary<string, string> Variables
		{
			get
			{
				if (_variables == null)
					_variables = new Dictionary<string, string>();

				return _variables;
			}
		}
		private Dictionary<string, string> _variables; // GENERICS 

		private PhpCallback filterCallback;

		private static UrlRewriter/*!*/ GetOrCreate()
		{
			if (current == null) current = new UrlRewriter();
			return current;
		}

		private PhpCallback/*!*/ GetOrCreateFilterCallback(ScriptContext/*!*/ context)
		{
			if (filterCallback == null)
				filterCallback = new PhpCallback(new RoutineDelegate(Filter), context);

			return filterCallback;
		}

		#endregion

        #region object

        #endregion

        #region Filter

        /// <summary>
        /// Tags parser, modifies the specified elements in the HTML code.
        /// </summary>
        private class TagsUrlRewriter: UrlRewriterTagsParser
        {
            public TagsUrlRewriter(UrlRewriter rewriter)
            {
                this.rewriter = rewriter;
            }

            private readonly UrlRewriter rewriter;

            Regex protocolPattern = new Regex(@"^[a-zA-Z]+:(\\|//).*", RegexOptions.Compiled);

            protected override void OnTagAttribute(string tagName, ref string attributeName, ref string attributeValue, ref char attributeValueQuote)
            {
                string[] attrs;

                if (ScriptContext.CurrentContext.Config.Session.UrlRewriterTags.TryGetValue(tagName.ToLower(), out attrs))
                {
                    for (int i = 0; i < attrs.Length; i++)
                    {
                        if (string.Equals(attrs[i], attributeName, StringComparison.InvariantCultureIgnoreCase))
                        {
                            if (protocolPattern.IsMatch(attributeValue))
                                return; // the URL must NOT be an absolute URL (not start with the protocol name)

                            // modify attribute value
                            if (attributeValue.Contains("?"))
                                attributeValue += "&";
                            else
                                attributeValue += "?";
                            
                            bool bFirst = true;
                            foreach (KeyValuePair<string, string> item in rewriter.Variables)
                            {
                                if (bFirst)
                                    bFirst = false;
                                else
                                    attributeValue += "&";

                                attributeValue += item.Key + "=" + item.Value;
                            }                            

                            return;
                        }
                    }
                }
            }
            protected override void OnTagElement(string tagName, ref string tagString)
            {
                // modify the "form" element
                const string strFormTag = "form";

                if ( string.Compare(tagName, strFormTag, StringComparison.InvariantCultureIgnoreCase) == 0 &&
                     ScriptContext.CurrentContext.Config.Session.UrlRewriterTags.ContainsKey(strFormTag) )
                {
                    foreach (KeyValuePair<string, string> item in rewriter.Variables)
                        tagString += string.Format("<input type=\"hidden\" name=\"{0}\" value=\"{1}\" />", item.Key, item.Value);
                }
            }
        }

        private TagsUrlRewriter parser;
        private TagsUrlRewriter.ParserState parserState = new TagsUrlRewriter.ParserState();

        private object Filter(object instance, PhpStack/*!*/ stack)
		{
            StringBuilder result = new StringBuilder();

			Debug.Assert(stack.ArgCount >= 1, "Called by output buffering, so should be ok");

            string data = PhpVariable.AsString(stack.PeekValueUnchecked(1));

			stack.RemoveFrame();

            // parse the text
            if (parser == null)
                parser = new TagsUrlRewriter(this);

            return parser.ParseHtml(parserState, data);
		}

		#endregion

		#region Current

		/// <summary>
		/// A context associated with the current thread.
		/// </summary>
		public static UrlRewriter Current { get { return current; } }
		[ThreadStatic]
		private static UrlRewriter current;

		/// <summary>
		/// Clears thread static field. Called on request end.
		/// </summary>
		public static void Clear()
		{
			current = null;
		}

		/// <summary>
		/// Registers <see cref="Clear"/> called on request end.
		/// </summary>
		static UrlRewriter()
		{
            RequestContext.RequestEnd += new Action(Clear);
		}

		#endregion

		#region output_add_rewrite_var, output_reset_rewrite_vars

		[ImplementsFunction("output_add_rewrite_var")]
		public static bool AddRewriteVariable(string name, string value)
		{
			if (String.IsNullOrEmpty(name))
			{
				PhpException.InvalidArgument("name", LibResources.GetString("arg:null_or_empty"));
				return false;
			}

			ScriptContext context = ScriptContext.CurrentContext;
            UrlRewriter rewriter = UrlRewriter.GetOrCreate();
            BufferedOutput output = context.BufferedOutput;

            // some output flush
            output.Flush();
            
            rewriter.Variables[name] = value;

            // start UrlRewriter filtering if not yet
            if (output.FindLevelByFilter( rewriter.filterCallback ) < 0)
            {
                // create new output buffer level (URL-Rewriter is not started yet)
                int Level = output.IncreaseLevel();
                output.SetFilter(rewriter.GetOrCreateFilterCallback(context), Level);
                output.SetLevelName(Level, "URL-Rewriter");
            }

            context.IsOutputBuffered = true;    // turn on output buffering if not yet

            return true;
		}

		[ImplementsFunction("output_reset_rewrite_vars")]
		public static bool ResetRewriteVariables()
		{
			/*PhpException.FunctionNotSupported();
			return false;*/

            
            ScriptContext context = ScriptContext.CurrentContext;
            UrlRewriter rewriter = UrlRewriter.Current;
            BufferedOutput output = context.BufferedOutput;
	  
            if (rewriter == null ||
                output.Level == 0 ||
                output.GetFilter() != rewriter.filterCallback)
              {
                  return false;
              }
            
            // some output flush
            output.Flush();

            rewriter.Variables.Clear();
            output.DecreaseLevel(false);

            if (output.Level == 0)
                context.IsOutputBuffered = false;
            
            return true;

        }

		#endregion
	}
}
