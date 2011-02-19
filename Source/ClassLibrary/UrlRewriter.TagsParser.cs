using System;
using System.Collections.Generic;
using System.Text;

namespace PHP.Library
{
    /// <summary>
    /// Parsing of HTML tags,
    /// primarily targeted for parsing and replacing URLs within output buffering.
    /// 
    /// Parses the input text and calls several events, when specified HTML elements are found.
    /// </summary>
    public class UrlRewriterTagsParser
    {
        #region HTML elements found events

        /// <summary>
        /// Called when tag attribute is found and parsed.
        /// Allows to modify the attribute.
        /// </summary>
        /// <param name="tagName">The tag name, where the attribute was found.</param>
        /// <param name="attributeName">The attribute name. Can be modified.</param>
        /// <param name="attributeValue">The attribute value. Can be modified.</param>
        /// <param name="attributeValueQuote">The attribute value quote character. Can be modified.</param>
        virtual protected void OnTagAttribute(string tagName, ref string attributeName, ref string attributeValue, ref char attributeValueQuote)
        {
            // modify attributeName
            // modify attributeValue
            // modify attributeValueQuote
        }

        /// <summary>
        /// Called when the whole tag is found and parsed.
        /// </summary>
        /// <param name="tagName">The tag name.</param>
        /// <param name="tagString">The whole tag input string. Can be modified.</param>
        virtual protected void OnTagElement(string tagName, ref string tagString)
        {
            // modify tagString
        }

        #endregion

        #region Parser states

        /// <summary>
        /// Internal parser states.
        /// </summary>
        public enum ParserStateNum
        {
            OuterText = 0,  // not in any HTML tag, waiting for <

            TagOpening, // <
            TagClosing, // >
            TagName,    // 

            InnerTagSpace,  // \s | \t ...

            AttributeName,  // 
            AttributePreAssigning, //
            AttributeAssigning, // =
            AttributeValueOpening,  // " | '
            AttributeValueClosing,  // " | '
            AttributeValue, //
        }

        /// <summary>
        /// Internal parser state.
        /// </summary>
        public class ParserState
        {
            public ParserStateNum state = ParserStateNum.OuterText;

            // tag
            public StringBuilder lastTagName;
            public StringBuilder lastTagString;

            // attribute
            public StringBuilder lastAttributeName;
            public char lastAttributeValueQuote;
            public StringBuilder lastAttributeValue;
        }

        #endregion

        #region Parsing HTML tags

        private bool IsWhiteSpace(char c)
        {
            return (c == ' ' || c == '\n' || c == '\t' || c == '\r');
        }

        /// <summary>
        /// Parses the given text, using the given current parser state.
        /// </summary>
        /// <param name="state"></param>
        /// <param name="text"></param>
        public string ParseHtml( ParserState state, string text )
        {
            StringBuilder result = new StringBuilder();

            foreach (char c in text)
            {
                switch (state.state)
                {
                    case ParserStateNum.OuterText:  // -> TagOpening
                        if (c == '<')
                        {
                            state.state = ParserStateNum.TagOpening;
                            state.lastTagString = new StringBuilder();
                        }
                        break;
                    case ParserStateNum.TagOpening: // -> TagClosing, TagName
                        if (c == '>')
                        {
                            state.state = ParserStateNum.TagClosing;
                        }
                        else
                        {
                            state.state = ParserStateNum.TagName;
                            state.lastTagName = new StringBuilder();
                        }
                        break;
                    case ParserStateNum.TagClosing: // not reachable
                        throw new InvalidOperationException("This state should not be reachable.");
                    case ParserStateNum.TagName:    // -> InnerTagSpace, TagClosing
                        if (IsWhiteSpace(c) || c == '/')
                            state.state = ParserStateNum.InnerTagSpace;
                        else if (c == '>')
                            state.state = ParserStateNum.TagClosing;
                        break;
                    case ParserStateNum.InnerTagSpace:  // -> TagClosing, AttributeName
                        if (c == '>')
                            state.state = ParserStateNum.TagClosing;
                        else if (c == '/')
                            {}
                        else if (!IsWhiteSpace(c))
                        {
                            state.state = ParserStateNum.AttributeName;
                            state.lastAttributeName = new StringBuilder();
                        }
                        break;
                    case ParserStateNum.AttributeName:  // -> AttributeAssigning, AttributePreAssigning, TagClosing
                        if (c == '=')
                        {
                            state.lastAttributeValue = new StringBuilder();
                            state.state = ParserStateNum.AttributeAssigning;
                        }
                        else if (IsWhiteSpace(c))
                        {
                            state.state = ParserStateNum.AttributePreAssigning;
                        }
                        else if (c == '>')
                        {
                            state.lastTagString.Append(state.lastAttributeName);
                            state.state = ParserStateNum.TagClosing;
                        }
                        break;
                    case ParserStateNum.AttributePreAssigning:  // -> AttributeAssigning, TagClosing, AttributeName
                        if (c == '=')
                        {
                            state.lastAttributeValue = new StringBuilder();
                            state.state = ParserStateNum.AttributeAssigning;
                        }
                        else if (IsWhiteSpace(c))
                        {}
                        else if (c == '>')
                        {
                            state.lastTagString.Append(state.lastAttributeName);
                            state.state = ParserStateNum.TagClosing;
                        }
                        else
                        {
                            state.lastTagString.Append(state.lastAttributeName);
                            state.lastTagString.Append(' ');    // attribute name without value

                            state.state = ParserStateNum.AttributeName;
                            state.lastAttributeName = new StringBuilder();
                        }
                        break;
                    case ParserStateNum.AttributeAssigning: // -> AttributeValueOpening, AttributeValue, TagClosing
                        if (c == '\'' || c == '\"')
                        {
                            state.lastAttributeValueQuote = c;
                            state.state = ParserStateNum.AttributeValueOpening;
                        }
                        else if (!IsWhiteSpace(c))
                        {
                            state.lastAttributeValueQuote = '\0';
                            state.state = ParserStateNum.AttributeValue;
                        }
                        else if (c == '>')
                        {
                            state.lastTagString.Append(state.lastAttributeName); // attribute name without value
                            state.state = ParserStateNum.TagClosing;
                        }
                        break;
                    case ParserStateNum.AttributeValueOpening:
                        throw new InvalidOperationException("This state should not be reachable.");
                    case ParserStateNum.AttributeValueClosing:
                        throw new InvalidOperationException("This state should not be reachable.");
                    case ParserStateNum.AttributeValue: // -> AttributeValueClosing, TagClosing
                        if ( c == state.lastAttributeValueQuote || (state.lastAttributeValueQuote == '\0' && IsWhiteSpace(c)) )
                        {
                            state.state = ParserStateNum.AttributeValueClosing;
                        }
                        else if (state.lastAttributeValueQuote == '\0' && c == '>')
                        {
                            Parse_AttributeValueClosing(state, c, result);
                            state.state = ParserStateNum.TagClosing;
                        }
                        break;
                }

                switch (state.state)
                {
                    case ParserStateNum.OuterText: Parse_OuterText(state, c, result); break;
                    case ParserStateNum.TagOpening: Parse_TagOpening(state, c, result); break;
                    case ParserStateNum.TagClosing: Parse_TagClosing(state, c, result); break;
                    case ParserStateNum.TagName: Parse_TagName(state, c, result); break;
                    case ParserStateNum.InnerTagSpace: Parse_InnerTagSpace(state, c, result); break;
                    case ParserStateNum.AttributeName: Parse_AttributeName(state, c, result); break;
                    case ParserStateNum.AttributePreAssigning: Parse_AttributePreAssigning(state, c, result); break;
                    case ParserStateNum.AttributeAssigning: Parse_AttributeAssigning(state, c, result); break;
                    case ParserStateNum.AttributeValueOpening: Parse_AttributeValueOpening(state, c, result); break;
                    case ParserStateNum.AttributeValueClosing: Parse_AttributeValueClosing(state, c, result); break;
                    case ParserStateNum.AttributeValue: Parse_AttributeValue(state, c, result); break;
                }
            }

            return result.ToString();
        }

        private void Parse_OuterText( ParserState state, char c, StringBuilder result )
        {
            result.Append(c);
        }
        private void Parse_TagOpening(ParserState state, char c, StringBuilder result)
        {
            state.lastTagString.Append(c);            
        }
        private void Parse_TagClosing(ParserState state, char c, StringBuilder result)
        {
            state.lastTagString.Append(c);
            
            string tagString = state.lastTagString.ToString();

            OnTagElement(state.lastTagName.ToString(), ref tagString);

            result.Append(tagString);

            state.state = ParserStateNum.OuterText;            
        }
        private void Parse_TagName(ParserState state, char c, StringBuilder result)
        {
            state.lastTagString.Append(c);
            state.lastTagName.Append(c);
        }
        private void Parse_InnerTagSpace(ParserState state, char c, StringBuilder result)
        {
            state.lastTagString.Append(c);
        }
        private void Parse_AttributeName(ParserState state, char c, StringBuilder result)
        {
            state.lastAttributeName.Append(c);
        }
        private void Parse_AttributePreAssigning(ParserState state, char c, StringBuilder result)
        {}
        private void Parse_AttributeAssigning(ParserState state, char c, StringBuilder result)
        {}
        private void Parse_AttributeValueOpening(ParserState state, char c, StringBuilder result)
        {
            state.state = ParserStateNum.AttributeValue;
        }
        private void Parse_AttributeValueClosing(ParserState state, char c, StringBuilder result)
        {
            string
                attName = state.lastAttributeName.ToString(),
                attValue = state.lastAttributeValue.ToString();

            OnTagAttribute(state.lastTagName.ToString(), ref attName, ref attValue, ref state.lastAttributeValueQuote);

            state.lastTagString.Append(attName);
            state.lastTagString.Append('=');
            if (state.lastAttributeValueQuote != '\0')  state.lastTagString.Append(state.lastAttributeValueQuote);
            state.lastTagString.Append(attValue);
            if (state.lastAttributeValueQuote != '\0') state.lastTagString.Append(state.lastAttributeValueQuote);

            state.state = ParserStateNum.InnerTagSpace;
        }
        private void Parse_AttributeValue(ParserState state, char c, StringBuilder result)
        {
            state.lastAttributeValue.Append(c);
        }

        #endregion
    }
}
