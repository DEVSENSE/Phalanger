/*

 Copyright (c) 2008 Jakub Misek

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Package;
using Microsoft.VisualStudio.TextManager.Interop;

using PHP.Core.Parsers;

using PHP.VisualStudio.PhalangerLanguageService.Scopes;
using PHP.VisualStudio.PhalangerLanguageService.Declarations;
using PHPParsing = PHP.VisualStudio.PhalangerLanguageService.Parsing;
using PHP.VisualStudio.PhalangerLanguageService.OrcasLists;

namespace PHP.VisualStudio.PhalangerLanguageService
{
    /// <summary>
    /// PHP source code object implementation.
    /// 
    /// Defines methods for IntelliSense requests,
    /// between Microsoft Visual Studio and Source code.
    /// </summary>
    internal class PhpScope : AuthoringScope
    {
        #region properties
        /// <summary>
        /// PHP language service.
        /// </summary>
        private readonly PhpLanguage language;

        /// <summary>
        /// PHP source object associated with this scope.
        /// </summary>
        private readonly PhpSource source;

        /// <summary>
        /// Global scope of declarations.
        /// </summary>
        private readonly ScopeInfo scope;

        /// <summary>
        /// Global scope of declarations.
        /// </summary>
        public ScopeInfo/*!*/Scope
        {
            get
            {
                return scope;
            }
        }

        /// <summary>
        /// All declaration scopes in this project (including this one).
        /// </summary>
        private readonly ProjectDeclarations projectdeclarations;

        #endregion

        #region Declarations chain

        /*// <summary>
        /// One step in selecting member.
        /// </summary>
        internal class IntellisenseChainNode
        {
            public enum SelectType
            {
                local,
                objectmember,
                staticmember
            }

            public readonly SelectType Type;
            public readonly string Name;
            public readonly Tokens Token;

            public IntellisenseChainNode(string Name, SelectType Type, Tokens Token)
            {
                this.Name = Name;
                this.Type = Type;
                this.Token = Token;
            }
        }*/

        /// <summary>
        /// Check if the given token should be function name.
        /// </summary>
        /// <param name="token">Token.</param>
        /// <returns>Tru if token is some identifier.</returns>
        internal static bool TokenShouldBeFunctionName(Tokens token)
        {
            return (token == Tokens.T_STRING ||
                    token == Tokens.T_NAMESPACE_NAME ||
                    token == Tokens.T_CONSTRUCT ||
                    token == Tokens.T_DESTRUCT ||
                    token == Tokens.T_VARIABLE ||
                    token == Tokens.T_PARENT ||
                    token == Tokens.T_SELF ||
                    token == Tokens.T_ECHO ||
                    token == Tokens.T_PRINT ||
                    token == Tokens.T_EVAL ||
                    token == Tokens.T_ISSET ||
                    token == Tokens.T_UNSET ||
                    token == Tokens.T_NEW ||
                    token == Tokens.T_CLONE ||
                    token == Tokens.T_INCLUDE ||
                    token == Tokens.T_REQUIRE ||
                    token == Tokens.T_RETURN ||
                    token == Tokens.T_INCLUDE_ONCE ||
                    token == Tokens.T_REQUIRE_ONCE ||
                    token == Tokens.T_GLOBAL ||
                    token == Tokens.T_BREAK ||
                    token == Tokens.T_CONTINUE ||
                    token == Tokens.T_EMPTY ||
                    token == Tokens.T_STRING_TYPE ||
                    token == Tokens.T_INT_TYPE);
        }

        /// <summary>
        /// List of members visible in local scope.
        /// </summary>
        /// <param name="result">Output list of local declarations.</param>
        /// <param name="match">String match in each of added declaration.</param>
        /// <param name="line">Declarations visible at this line.</param>
        /// <param name="col">Declarations visible at the given line and this column.</param>
        private void GetLocalDeclarations(DeclarationList result, DeclarationMatches match, int line, int col)
        {
            if (Scope == null)
                return;

            ScopeInfo cur_scope = Scope.GetScopeAt(line, col);

            cur_scope.GetLocalDeclarations(result, 0, match, projectdeclarations);
        }

        /*/// <summary>
        /// Find the way, the object is being selected.
        /// </summary>
        /// <param name="line">Line.</param>
        /// <param name="col">Column.</param>
        /// <param name="reason">Parse reason.</param>
        /// <param name="before1">Will be filled with token before the chain.</param>
        /// <param name="before2">Will be filled with the token before token "before1"</param>
        /// <returns>Chain stack.</returns>
        private Stack<IntellisenseChainNode> GetMembersChain(int line, int col, ParseReason reason, out Tokens before1, out Tokens before2)
        {
            before1 = (Tokens)0;
            before2 = (Tokens)0;

            // analyze the expression (go back throw the object full name
            Stack<IntellisenseChainNode> chain = new Stack<IntellisenseChainNode>();
            string cur_name = null;
            Tokens cur_name_token = (Tokens)0;
            int cur_line = line;

            TokenInfo cur_token = new TokenInfo();
            TokenInfo[] curline_tokens = source.GetColorizer().GetLineInfo(source.GetTextLines(), cur_line, source.ColorState);
            int cur_tokenindex;

            if ( curline_tokens != null )
            {
                cur_tokenindex = source.GetTokenInfoAt(curline_tokens, col, ref cur_token);
            }
            else
            {   // nothing here, CompleteWorld on empty line probably pressed
                cur_tokenindex = -1;
                cur_token = null;
            }            

            int cur_level = 0;  // parent level

            while (cur_token != null && cur_level >= 0 && before2 == 0)
            {
                Tokens cur_tokentoken = (Tokens)cur_token.Token;

                if (cur_tokentoken != Tokens.T_WHITESPACE) // skip white spaces
                {
                    if (before1 != 0)
                    {
                        before2 = cur_tokentoken;
                    }
                    else
                    switch (cur_tokentoken)
                    {
                        // everything what should be part of chain
                        case Tokens.T_OBJECT_OPERATOR:
                            if (cur_level == 0)
                            {
                                chain.Push(new IntellisenseChainNode(cur_name, IntellisenseChainNode.SelectType.objectmember, cur_name_token));
                                cur_name = null;
                            }
                            break;
                        case Tokens.T_DOUBLE_COLON:
                            if (cur_level == 0)
                            {
                                chain.Push(new IntellisenseChainNode(cur_name, IntellisenseChainNode.SelectType.staticmember, cur_name_token));
                                cur_name = null;
                            }
                            break;
                        case Tokens.T_LPAREN:
                        case Tokens.T_LBRACKET:
                            --cur_level;
                            break;
                        case Tokens.T_RPAREN:
                        case Tokens.T_RBRACKET:
                            ++cur_level;
                            break;
                        // end of chain
                        default:
                            if (cur_level == 0)
                            {
                                if (TokenShouldBeFunctionName(cur_tokentoken))
                                {
                                    if (cur_name != null)
                                    {
                                        before1 = cur_tokentoken;// end of chain parsing, parse last token before and exit
                                        break;
                                    }

                                    cur_name = source.GetLine(cur_line).Substring(cur_token.StartIndex, cur_token.EndIndex + 1 - cur_token.StartIndex);
                                    cur_name_token = cur_tokentoken;
                                }
                                else
                                {   // end of chain parsing, parse last token before and exit
                                    before1 = cur_tokentoken;
                                }
                            }
                            break;
                    }
                }
                

                // go to the next token (left)
                --cur_tokenindex;
                while (cur_tokenindex < 0)
                {
                    if ((--cur_line) < 0)
                        break;

                    curline_tokens = source.GetColorizer().GetLineInfo(source.GetTextLines(), cur_line, source.ColorState);
                    if (curline_tokens == null)
                        cur_tokenindex = -1;
                    else
                        cur_tokenindex = curline_tokens.Length - 1;
                }

                if (cur_line < 0)
                    break;

                cur_token = curline_tokens[cur_tokenindex];
            }

            if (cur_name != null)
            {
                // the first identifier
                chain.Push(new IntellisenseChainNode(cur_name, IntellisenseChainNode.SelectType.local, cur_name_token));
            }

            if (chain.Count == 0)
            {   // nothing to select, select all local members
                chain.Push(new IntellisenseChainNode(null, IntellisenseChainNode.SelectType.local, 0));
            }

            //
            return chain;
        }

        /// <summary>
        /// Fill declaration list with possible members finishing the given chain.
        /// </summary>
        /// <param name="chain"></param>
        /// <param name="result"></param>
        /// <param name="line"></param>
        /// <param name="col"></param>
        /// <param name="lastname">The last identifier name.</param>
        private List<DeclarationInfo> SelectMembers(Stack<IntellisenseChainNode> chain, int line, int col, out string lastname, out Tokens lasttoken)
        {
            List<DeclarationInfo> decls = new DeclarationList();
            string parentname = null;

            lastname = null;
            lasttoken = 0;

            while (chain.Count > 0)
            {
                IntellisenseChainNode node = chain.Pop();
                lastname = node.Name;
                lasttoken = node.Token;

                switch (node.Type)
                {
                    case IntellisenseChainNode.SelectType.local:
                        {
                            string nodeName = node.Name;
                            if (nodeName != null)
                                nodeName = nodeName.TrimStart(new char[] { '$' });    // remove $ from the begin, so select even classes and functions

                            DeclarationMatches match;

                            if (chain.Count == 0)
                                match = new DeclarationLabelContains(nodeName);
                            else
                                match = new DeclarationLabelEqual(nodeName);

                            GetLocalDeclarations(decls, match, line, col);
                        }
                        break;
                    case IntellisenseChainNode.SelectType.objectmember:
                    case IntellisenseChainNode.SelectType.staticmember:
                        {
                            List<DeclarationInfo> tmpdecls = new DeclarationList();
                            foreach (DeclarationInfo decl in decls)
                            {
                                tmpdecls.Add(decl);
                            }

                            decls.Clear();

                            DeclarationMatches match;
                            if (chain.Count == 0)
                                match = new DeclarationLabelContains(node.Name);
                            else
                                match = new DeclarationLabelEqual(node.Name);

                            switch (node.Type)
                            {
                                case IntellisenseChainNode.SelectType.objectmember:
                                    foreach (DeclarationInfo decl in tmpdecls)
                                        decl.GetObjectMembers(projectdeclarations, decls, match);
                                    break;
                                case IntellisenseChainNode.SelectType.staticmember:
                                    foreach (DeclarationInfo decl in tmpdecls)
                                        decl.GetStaticMembers(projectdeclarations, decls, match);
                                    break;
                            }
                        }


                        break;
                }

                parentname = node.Name;

                if (parentname != null)
                    parentname = parentname.TrimStart(new char[] { '$' });
            }

            //
            return decls;
        }*/
        
        /// <summary>
        /// Search for DeclarationInfo used at the given position.
        /// </summary>
        /// <param name="line">Line position.</param>
        /// <param name="col">Column position.</param>
        /// <returns>List of declarations match the word on the given position.</returns>
        private DeclarationInfo GetDeclarationAt(int line, int col)
        {
            string lastword;
            Tokens lasttoken;
            Tokens prevtok1,prevtok2;
            bool localdeclaration;
            //Stack<IntellisenseChainNode> memberschain = GetMembersChain(line, col, ParseReason.QuickInfo, out prevtok1,out prevtok2);
            //List<DeclarationInfo> declarations = SelectMembers(memberschain, line, col, out lastword, out lasttoken);
            DeclarationList declarations = AvailableDeclarationsAt(line, col, true, out prevtok2, out prevtok1, out lastword, out lasttoken, out localdeclaration);

            if (lastword == null || declarations.Count == 0)
                return null;

            return declarations[0];
        }

        /// <summary>
        /// Modify (add/filter) list of declarations available at the given position by its context.
        /// </summary>
        /// <param name="lastword">currently typed word</param>
        /// <param name="lasttoken">currently typed world token name</param>
        /// <param name="declarations">list of declarations to be displayed in IntelliSense.</param>
        /// <param name="localdeclarations">Declarations are from local scope.</param>
        /// <param name="memberschain_length">number of elements in expression</param>
        private void OfferNotDefinetDeclarations(string lastword, Tokens lasttoken, DeclarationList declarations, bool localdeclarations, Tokens lastTokenBeforeChain1, Tokens lastTokenBeforeChain2)
        {
            // special cases

            // class XXX {extends}
            if (lastTokenBeforeChain2 == Tokens.T_CLASS && lastTokenBeforeChain1 == Tokens.T_STRING )
            {
                declarations.Clear();
                declarations.Add(new DeclarationInfo("extends", new TextSpan(), 206, null, null));
                return;
            }

            if (lastTokenBeforeChain1 == Tokens.T_AMP)
            {   // ignore & // function &foo
                lastTokenBeforeChain1 = lastTokenBeforeChain2;
                lastTokenBeforeChain2 = 0;
            }

            // special cases
            switch (lastTokenBeforeChain1)
            {
                case Tokens.T_FUNCTION: // declaring function
                    declarations.Clear();
                    if (lastword != null)
                        declarations.Add(new DeclarationInfo(lastword, new TextSpan(), 72, "(new function)", null));
                    break;
                case Tokens.T_CONST:    // declaring constant
                    declarations.Clear();
                    if (lastword != null)
                        declarations.Add(new DeclarationInfo(lastword, new TextSpan(), 10, "(new constant)",null));
                    break;
                case Tokens.T_CLASS:    // declaring class
                    declarations.Clear();
                    if (lastword != null)
                        declarations.Add(new DeclarationInfo(lastword, new TextSpan(), 0, "(new class)", null));
                    break;
                case Tokens.T_NEW:  // only classes and namespaces
                    {
                        declarations.FilterType(DeclarationInfo.DeclarationTypes.Class | DeclarationInfo.DeclarationTypes.Namespace);
                    }
                    break;
                case Tokens.T_VAR:      // declaring new variable
                    declarations.Clear();
                    if (lastword != null)
                        declarations.Add(new VariableDeclaration(
                                lastword.TrimStart(new char[] { '$' }),    // remove $
                                DeclarationInfo.DeclarationVisibilities.Public,
                                new Position(),
                                "(new variable)",
                                null, null));
                    break;
                case Tokens.T_EXTENDS:  // offer only classes
                    {
                        declarations.FilterType(DeclarationInfo.DeclarationTypes.Class);
                    }
                    break;
                case Tokens.T_NAMESPACE:
                    {
                        if (lastTokenBeforeChain2 == Tokens.T_IMPORT)// offer only namespaces
                        {   // import namespace ... (remove class declarations, keep only namespace declarations)
                            declarations.FilterType(DeclarationInfo.DeclarationTypes.Namespace);
                        }
                    }
                    break;
                default:
                        // declaring new variable
                        if ( lastword != null &&
                            (lastword.StartsWith("$") ^ !localdeclarations) &&
                            (lasttoken == Tokens.T_STRING || lasttoken == Tokens.T_VARIABLE))
                        {
                            string lastword_withoutdollar = lastword.TrimStart(new char[] { '$' });
                            bool somethingsimilarexists = false;// declarations.Count > 0;
                            foreach (DeclarationInfo decl in declarations)
                                if (//decl.DeclarationType == DeclarationInfo.DeclarationTypes.Variable &&
                                    decl.Label.StartsWith(lastword_withoutdollar))
                                {
                                    somethingsimilarexists = true;
                                    break;
                                }

                            // there is nothing with similar name, offer it
                            if (!somethingsimilarexists)
                            {
                                declarations.Add(new VariableDeclaration(
                                lastword_withoutdollar,
                                DeclarationInfo.DeclarationVisibilities.Public,
                                new Position(),
                                "(new variable)",
                                null, null));
                            }
                        }

                    //
                    break;
            }

            // 
            /*if (lastTokenBeforeChain1 == Tokens.T_DOLLAR)
            {   // ${variable}
                int i = 0;
                while (i < declarations.Count)
                {   // filter only variables after DOLLAR
                    if (declarations[i].DeclarationType != DeclarationInfo.DeclarationTypes.Variable)
                    {   // remove [i]
                        declarations.RemoveAt(i);
                    }
                    else
                    {
                        ++i;
                    }
                }
                // ${$}
                //if (lastTokenBeforeChain2 != Tokens.T_DOLLAR)   // not $${$}
                {   // add second DOLLAR after first DOLLAR
                    if (lastword == null || lastword == "")
                        declarations.Add(new DeclarationInfo("$", new TextSpan(), 42, null, null));
                }
                return;
            }*/

            // ->$
            // $$
            // ::$
            if (lasttoken == Tokens.T_DOLLAR || lasttoken == Tokens.T_OBJECT_OPERATOR || lasttoken == Tokens.T_DOUBLE_COLON)
            {
                declarations.Add(new DeclarationInfo("$", new TextSpan(), 42, null, null));
            }

            // :::
            if (lasttoken == Tokens.T_DOUBLE_COLON)
            {
                declarations.Add(new DeclarationInfo(":", new TextSpan(), 42, null, null));
            }
        }

        /// <summary>
        /// Get the list of declarations available at the given position.
        /// </summary>
        /// <param name="line">Position line.</param>
        /// <param name="col">Position column.</param>
        /// <param name="FullyTyped">Declaration on position should be fully typed.</param>
        /// <param name="prevtoken2">2 tokens before the expression on the given position.</param>
        /// <param name="prevtoken1">1 token before the expression on the given position.</param>
        /// <param name="lastword">Word on the given position.</param>
        /// <param name="lasttoken">Token on the given position.</param>
        /// <param name="typinglocal">Typed word is from local scope, not an object member declaration</param>
        /// <returns>List of declarations available at the given position.</returns>
        private DeclarationList AvailableDeclarationsAt(int line, int col, bool FullyTyped, out Tokens prevtoken2, out Tokens prevtoken1, out string lastword, out Tokens lasttoken, out bool typinglocal)
        {
            TokenInfo cur_token = new TokenInfo();
            TokenInfo[] curline_tokens = source.GetColorizer().GetLineInfo(source.GetTextLines(), line, source.ColorState);
            int cur_tokenindex = -1;

            prevtoken1 = prevtoken2 = lasttoken = 0;
            lastword = null;
            typinglocal = true;

            if (curline_tokens != null)
            {
                cur_tokenindex = source.GetTokenInfoAt(curline_tokens, col, ref cur_token);

                if (cur_tokenindex >= 0)
                {
                    string linecode = source.GetLine(line);

                    // lastword
                    lasttoken = (Tokens)cur_token.Token;
                    lastword = linecode.Substring(cur_token.StartIndex, cur_token.EndIndex + 1 - cur_token.StartIndex);

                    // select possible declarations here
                    int prevtokenindex = cur_tokenindex;
                    PHPParsing.PartialTreeBuilder treeBuilder = new PHPParsing.PartialTreeBuilder();
                    PHP.Core.AST.LangElement NotCompletedElement = null;
                    PHP.Core.AST.Expression exp = 
                        FullyTyped?
                        treeBuilder.BuildCompletedPartialTree(linecode, curline_tokens, ref cur_tokenindex):
                        treeBuilder.BuildIncompletePartialTree(linecode, curline_tokens, ref cur_tokenindex, out NotCompletedElement);

                    DeclarationList result = PHPParsing.GetDeclarations.GetDeclarationsByName(projectdeclarations, scope.GetScopeAt(line, col), false, exp, NotCompletedElement);
                    //DeclarationList result = AvailableDeclarationsAt(line, col, curline_tokens, linecode, ref cur_tokenindex, FullyTyped);

                    PHP.Core.AST.VarLikeConstructUse isvar = exp as PHP.Core.AST.VarLikeConstructUse;
                    typinglocal = !(isvar != null && isvar.IsMemberOf != null);// && (cur_tokenindex >= (prevtokenindex - 1));

                    // tokens before
                    while (cur_tokenindex>=0)
                    {
                        Tokens token = (Tokens)curline_tokens[cur_tokenindex].Token;
                        if (token != Tokens.T_WHITESPACE)
                        {
                            if (prevtoken1 == 0)
                            {
                                prevtoken1 = token;
                            }
                            else
                            {
                                prevtoken2 = token;
                                break;
                            }
                        }

                        --cur_tokenindex;
                    }

                    // done
                    return result;
                }
            }

            // else
            {   // nothing here, CompleteWorld on empty line probably pressed
                DeclarationList result = new DeclarationList();

                GetLocalDeclarations(result, new DeclarationLabelContains(null), line, col);

                return result;
            }
        }

        /*private DeclarationList AvailableDeclarationsAt(int line, int col, TokenInfo[]tokens, string linecode, ref int tokenindex, bool calledFromMember)
        {
            if (tokenindex < 0)
                return null;

            //
            // scan for identifier on the current position
            //
            TokenInfo tokeninfo = tokens[tokenindex];
            Tokens token = (Tokens)tokeninfo.Token;
            DeclarationMatches match;
            string identifier = null;

            if (token == Tokens.T_NAMESPACE_NAME)   // namespace name
            {
                identifier = linecode.Substring(tokeninfo.StartIndex, tokeninfo.EndIndex + 1 - tokeninfo.StartIndex);

                if (calledFromMember)
                {   // namespace name is complete
                    return GetNamespaceDeclarationAt(line, col, tokens, linecode, ref tokenindex);
                }
                else
                {
                    --tokenindex;

                    string namespaceFullName = linecode.Substring(tokeninfo.StartIndex, tokeninfo.EndIndex + 1 - tokeninfo.StartIndex);
                    string[] names = namespaceFullName.Split(new string[] { PHP.Core.QualifiedName.Separator }, StringSplitOptions.None);
                    string parentnamespacename = null;
                    for (int i = 0; i < names.Length - 1; ++i)
                        if (parentnamespacename == null) parentnamespacename = names[i];
                        else parentnamespacename += PHP.Core.QualifiedName.Separator + names[i];

                    // select parent namespace
                    DeclarationList parents = scope.GetScopeAt(line, col).GetDeclarationsByName(Utils.MakeQualifiedName(parentnamespacename), projectdeclarations);

                    // get parent members containing last name
                    match = new DeclarationLabelContains(names[names.Length - 1]);
                    DeclarationList result = new DeclarationList();
                    if (parents != null)
                        foreach (DeclarationInfo decl in parents)
                            decl.GetTypeMembers(projectdeclarations, result, match);

                    return result;
                }
            }
            else if (TokenShouldBeFunctionName(token)) // identifier
            {
                // identifier to be selected
                identifier = linecode.Substring(tokeninfo.StartIndex, tokeninfo.EndIndex + 1 - tokeninfo.StartIndex);

                -- tokenindex;
            }
            else if ((token == Tokens.T_RPAREN || token == Tokens.T_RBRACE) && calledFromMember)  // ->${enclosed expression}->
            {
                -- tokenindex;
                DeclarationList result = AvailableDeclarationsAt(line, col, tokens, linecode, ref tokenindex, true);
                if(result != null)
                {
                    Tokens tokenbefore = (tokenindex < 0) ? 0 : (Tokens)tokens[tokenindex].Token;   // next token
                    if (tokenbefore == Tokens.T_OBJECT_OPERATOR || tokenbefore == Tokens.T_DOUBLE_COLON)
                    {   // chain continues indirectly // to do: see doc, if its possible "$a->$b->$c"
                        List<string> labels = new List<string>();
                        foreach (DeclarationInfo decl in result)decl.GetIndirectIdentifiers(projectdeclarations, scope.GetScopeAt(line, col), labels);
                        DeclarationMatches varlabels = new DeclarationLabelEqualMultiple(labels);
                        result = DeclarationMembersAt(line, col, tokens, linecode, ref tokenindex, true, varlabels);
                        tokenbefore = (tokenindex<0)?0:(Tokens)tokens[tokenindex].Token;
                    }

                    if (result != null && (tokenbefore == Tokens.T_LPAREN || tokenbefore == Tokens.T_LBRACE))
                    {
                        --tokenindex;
                        // count dollars
                        int nIndirects = 0;
                        while (tokenindex >= 0 && (Tokens)tokens[tokenindex].Token == Tokens.T_DOLLAR)
                        {
                            ++nIndirects;
                            --tokenindex;
                        }

                        // select local variables indirectly
                        if (nIndirects > 0)
                        {
                            match = null;

                            for (int indirect = nIndirects; indirect > 0; --indirect)
                            {
                                List<string> labels = new List<string>();
                                foreach (DeclarationInfo decl in result)
                                    decl.GetIndirectIdentifiers(projectdeclarations, scope.GetScopeAt(line, col), labels);

                                match = new DeclarationLabelEqualMultiple(labels);

                                if (indirect > 1)
                                {   // refresh result for next loop
                                    result.Clear();
                                    GetLocalDeclarations(result, match, line, col);
                                }
                            }

                            //
                            // select declarations available on the current position with this match
                            //
                            return DeclarationMembersAt(line, col, tokens, linecode, ref tokenindex, calledFromMember, match);
                        }
                        else
                        {
                            return result;
                        }
                    }
                }

                return null;// not handled
            }
            else if (token == Tokens.T_DOLLAR && !calledFromMember) // start of typing variable name probably
            {
                identifier = "$";
                --tokenindex;
            }
            else if (!calledFromMember)
            {
                // any declaration
                identifier = null;
            }
            else
            {
                // wrong token
                return null;
            }

            //
            // declaration to be selected from members (by label)
            //
            if (calledFromMember)// this identifier is fully typed
            {
                // count dollars
                int nIndirects = 0;
                while (tokenindex>=0 && (Tokens)tokens[tokenindex].Token == Tokens.T_DOLLAR)
                {
                    ++nIndirects;
                    --tokenindex;
                }
                
                // select local variables indirectly
                if(nIndirects>0 || (identifier != null && identifier.StartsWith("$")))
                {
                    DeclarationList result = new DeclarationList();
                    GetLocalDeclarations(result, new DeclarationLabelEqual(identifier), line, col);

                    for (int indirect = nIndirects; indirect > 0; --indirect )
                    {
                        List<string> labels = new List<string>();
                        foreach(DeclarationInfo decl in result)
                            decl.GetIndirectIdentifiers(projectdeclarations, scope.GetScopeAt(line, col), labels);

                        DeclarationMatches varlabels = new DeclarationLabelEqualMultiple(labels);

                        result.Clear();
                        GetLocalDeclarations(result, varlabels, line, col);
                    }

                    return result;
                }
                else
                {
                    match = new DeclarationLabelEqual(identifier);   // members to be selected by label
                }
            }
            else// this word is being typed now
            {
                // if there is a dollar, select identifier from local variables
                if (identifier != null && identifier.StartsWith("$"))
                {
                    DeclarationList result = new DeclarationList();
                    GetLocalDeclarations(result, new DeclarationLabelContains(identifier), line, col);
                    return result;
                }
                else
                {
                    match = new DeclarationLabelContains(identifier);   // members to be selected by label
                }
            }

            //
            // select declarations available on the current position with this identifier
            //
            return DeclarationMembersAt(line, col, tokens, linecode, ref tokenindex, calledFromMember, match);
        }

        private DeclarationList DeclarationMembersAt(int line, int col, TokenInfo[]tokens, string linecode, ref int tokenindex, bool calledFromMember, DeclarationMatches match)
        {
            Tokens token = (tokenindex < 0 || tokens == null) ? 0 : (Tokens)tokens[tokenindex].Token;

            DeclarationList result = new DeclarationList();

            switch (token)
            {
                case Tokens.T_OBJECT_OPERATOR:  // ...->{match}
                case Tokens.T_DOUBLE_COLON:     // ...::{match}
                    {
                        -- tokenindex;
                        DeclarationList parents = AvailableDeclarationsAt(line, col, tokens, linecode, ref tokenindex, true);

                        if (parents != null)
                            foreach (DeclarationInfo decl in parents)
                            {
                                if (token == Tokens.T_OBJECT_OPERATOR)
                                    decl.GetObjectMembers(projectdeclarations, result, match);
                                else if (token == Tokens.T_DOUBLE_COLON)
                                    decl.GetStaticMembers(projectdeclarations, result, match);
                            }
                    }
                    break;
                case Tokens.T_COLON:    // ...:::{empty}    // so parser does not recognize ...:::  as T_NAMESPACE_NAME
                    {
                        if ( tokenindex > 0 && (Tokens)tokens[tokenindex-1].Token == Tokens.T_DOUBLE_COLON)
                        {
                            tokenindex -= 2;

                            DeclarationList parents = GetNamespaceDeclarationAt(line, col, tokens, linecode, ref tokenindex);

                            if (parents != null)
                                foreach (DeclarationInfo decl in parents)
                                {
                                    decl.GetTypeMembers(projectdeclarations, result, match);
                                }
                        }
                    }
                    break;
                default:        // {match}
                    GetLocalDeclarations(result, match, line, col);
                    break;
            }

            return result;
        }

        /// <summary>
        /// Get declaration of namespace at given position.
        /// </summary>
        /// <param name="tokens">List of token on the line.</param>
        /// <param name="linecode">The line source code.</param>
        /// <param name="tokenindex">Token index.</param>
        /// <returns>List of declarations.</returns>
        private DeclarationList GetNamespaceDeclarationAt(int line, int col, TokenInfo[] tokens, string linecode, ref int tokenindex)
        {
            if (tokenindex < 0)
                return null;

            TokenInfo tokeninfo = tokens[tokenindex];
            string identifier = linecode.Substring(tokeninfo.StartIndex, tokeninfo.EndIndex + 1 - tokeninfo.StartIndex);
            Tokens token = (Tokens)tokens[tokenindex].Token;

            switch (token)
            {
                case Tokens.T_NAMESPACE_NAME:
                case Tokens.T_STRING:
                    {
                        ScopeInfo localscope = Scope.GetScopeAt(line, col);
                        --tokenindex;

                        return localscope.GetDeclarationsByName(Utils.MakeQualifiedName(identifier), projectdeclarations);
                    }
                default:
                    return null;
            }
        }*/

        #endregion

        #region  AuthoringScope

        /// <summary>
        /// Init the scope.
        /// </summary>
        /// <param name="language">A language object.</param>
        /// <param name="declarations">A source code global (root) code scope.</param>
        /// <param name="projectdeclarations">All project source code scopes.</param>
        /// <param name="source">A source code object.</param>
        public PhpScope(/*Module module, */PhpLanguage language, ScopeInfo/*!*/declarations, ProjectDeclarations projectdeclarations, PhpSource source) 
		{
			//this.module = module;
			this.language = language;
            this.source = source;
            this.scope = declarations;
            this.projectdeclarations = projectdeclarations;

            source.Scope = this;
		}

        /// <summary>
        /// Get ToolTipText for the given position.
        /// </summary>
        /// <param name="line">Line.</param>
        /// <param name="col">Column.</param>
        /// <param name="span">Output token range.</param>
        /// <returns>Token tooltip text or null.</returns>
		public override string GetDataTipText(int line, int col, out TextSpan span)
		{
            span = new TextSpan();
            
            // get token on position
            TokenInfo[] TokensOnLine = source.GetColorizer().GetLineInfo(source.GetTextLines(), line, source.ColorState);
            TokenInfo info = new TokenInfo();
            int iCurToken = source.GetTokenInfoAt(TokensOnLine, col, ref info);

            Tokens infotoken = (Tokens)info.Token;

            if (iCurToken >= 0)
            {
                // copy current token position
                // VS will cache this position and GetDataTipText should not be called for this area again
                span.iStartLine = span.iEndLine = line;
                span.iStartIndex = info.StartIndex;
                span.iEndIndex = info.EndIndex + 1;

                // if token should have declaration
                if (TokenShouldBeFunctionName(infotoken))
                {   // only identifiers and variables, ...
                    // try to find declaration
                    DeclarationInfo decl = GetDeclarationAt(line, col);

                    if (decl != null)
                    {
                        // get display text
                        string fullname = decl.FullName;

                        return string.Format("{0}{1}", decl.GetDescription(), (fullname != null && fullname != decl.Label) ? ("\n\n" + fullname) : null);
                    }
                }

            }

            // else
            return null;
            //return infotoken.ToString();
		}
        
        /// <summary>
        /// Get list of declarations for IntelliSense.
        /// </summary>
        /// <param name="view">Text view object.</param>
        /// <param name="line">Line.</param>
        /// <param name="col">Column.</param>
        /// <param name="info">Current token.</param>
        /// <param name="reason">Parse reason.</param>
        /// <returns>List of declarations.</returns>
        public override Microsoft.VisualStudio.Package.Declarations GetDeclarations(IVsTextView view, int line, int col, TokenInfo info, ParseReason reason)
		{
            //System.Diagnostics.Debug.Print("GetDeclarations line({0}), col({1}), TokenInfo(type {2} at {3}-{4} triggers {5}), reason({6})",
            //        line, col, info.Type, info.StartIndex, info.EndIndex, info.Trigger, reason);

            //Tokens lasttok1, lasttok2;
            //string lastword;
            //Tokens lasttoken;
            //bool localdeclaration;

            ///*Stack<IntellisenseChainNode> memberschain = GetMembersChain(line, Math.Max(0,col-1), reason, out lasttok1,out lasttok2);
            //int memberschain_length = memberschain.Count;
            //List<DeclarationInfo> declarations = SelectMembers(memberschain, line,col,out lastword, out lasttoken);

            //// if the lastworld does not exist in declarations, add it (possible new variable declaration, description of "new variable")
            //OfferNotDefinetDeclarations(lastword, lasttoken, declarations, memberschain_length, lasttok1,lasttok2);

            //// Show snippets
            //if (memberschain_length <= 1)
            //{
            //    language.AddSnippets(declarations, lastword);
            //}
            
            //// done
            //return new PhpDeclarations(
            //    language, declarations,
            //    (memberschain_length <= 1) &&   // first members of the chain
            //    lasttok1 != Tokens.T_DOLLAR &&  // dollar was not typed before this identifier
            //    lasttoken != Tokens.T_VARIABLE  // identifier does not starts with dollar
            //    );*/

            //DeclarationList decls = AvailableDeclarationsAt(line, col - 1, false, out lasttok2, out lasttok1, out lastword, out lasttoken, out localdeclaration);

            //// filter/modify selected declarations
            //// new variable/function/class declaration, ...
            //OfferNotDefinetDeclarations(lastword, lasttoken, decls, localdeclaration, lasttok1, lasttok2);

            //// TODO: Show snippets according to current language context
            //if (localdeclaration)
            //{
            //    language.AddSnippets(decls, lastword);
            //}

            //return new PhpDeclarations(
            //    language, decls, localdeclaration,
            //    lastword == null || !lastword.StartsWith("$"),    // add $ before variable-declaration name ?
            //    lasttoken == Tokens.T_NAMESPACE_NAME    // IntelliSense is replacing whole token, but declarations are last namespace chain parts only
            //    );

            return null;
		}

        /// <summary>
        /// Returns a list of overloaded method signatures for a specified method name.
        /// </summary>
        /// <param name="line">The line number where the parse for method signatures started.</param>
        /// <param name="col">The offset into the line where the parse for method signatures started.</param>
        /// <param name="name">The name of the method for which to get permutations.</param>
        /// <returns>If successful, returns a Methods object; otherwise, returns a null value.</returns>
        /// <remarks>This method is called to obtain a list of overloaded methods to show in the IntelliSense method tip. Note that the parse operation has been completed by the time this method is called.
        /// In the default managed package framework implementation, the Source class method MethodTip is called when a parse operation returns MethodTip from the TokenTriggers enumeration. This in turn triggers a quick parse with the reason MethodTip from the ParseReason enumeration. When that parse operation completes, the GetMethods method is called to return a list of method signatures matching the specified string. Note that the returned object is always your implementation of the Methods class.
        /// </remarks>
        public override Methods GetMethods(int line, int col, string name)
		{
            string[] names = name.Split(new string[] { PHP.Core.QualifiedName.Separator }, StringSplitOptions.RemoveEmptyEntries);
            name = names[names.Length - 1]; // only last name from fully qualified name (in case of using namespaces)

            // get list of declarations on position
            string lastword;
            Tokens lasttoken;
            Tokens prevtok1, prevtok2;
            bool localdeclaration;
            DeclarationList declarations = AvailableDeclarationsAt(line, col, true, out prevtok2, out prevtok1, out lastword, out lasttoken, out localdeclaration);

            PhpMethods methods = new PhpMethods();

            if (declarations != null)
            foreach (DeclarationInfo decl in declarations)
                /*if ((decl.DeclarationType &
                    // what should have parameters list
                    (DeclarationInfo.DeclarationTypes.Function | DeclarationInfo.DeclarationTypes.Class | DeclarationInfo.DeclarationTypes.Variable | DeclarationInfo.DeclarationTypes.Constant))
                    != 0)*/
                {
                    if (decl.Label == name)
                        methods.AddMethod(decl);
                }

			return methods;
		}

        /// <summary>
        /// Returns a URI (Universal Resource Identifier) based on the current location in the source and the specified command.
        /// </summary>
        /// <param name="cmd">A value from the VSConstants..::.VSStd97CmdID enumeration that determines what kind of destination URI must be returned. This is the command the user entered, typically from a context menu.</param>
        /// <param name="textView">The IVsTextView object containing the text under the cursor.</param>
        /// <param name="line">The line number containing the text under the cursor.</param>
        /// <param name="col">The offset into the line containing the text under the cursor.</param>
        /// <param name="span">A TextSpan object marking the selected text area for which the URI is determined.</param>
        /// <returns>If successful, returns a string containing the URI; otherwise, returns a null value.</returns>
		public override string Goto(VSConstants.VSStd97CmdID cmd, IVsTextView textView, int line, int col, out TextSpan span)
		{
            span = new TextSpan();

            if ( cmd == VSConstants.VSStd97CmdID.GotoDecl || cmd == VSConstants.VSStd97CmdID.GotoDefn )
            {
                // find declaration at position
                DeclarationInfo decl = GetDeclarationAt(line, col);

                if (decl != null)
                {
                    // navigate to the declaration origin position
                    string filename = decl.FileName;
                    if (filename == null)
                        return null;

                    System.IO.FileInfo fi = new System.IO.FileInfo(filename);

                    if (fi.Exists)
                    {
                        span = decl.Span;
                        return fi.FullName;
                    }
                    else
                    {
                        System.Windows.Forms.MessageBox.Show(
                            string.Format("The definition is located in '{0}'.\nThis location cannot be opened.", filename),
                            "Phalanger LanguageService",
                            System.Windows.Forms.MessageBoxButtons.OK,
                            System.Windows.Forms.MessageBoxIcon.Information);
                    }
                }
            }
            else
            {
                // not catched command
                System.Windows.Forms.MessageBox.Show(string.Format("Command {0}.", cmd));
            }
            
            return null;
        }

        #endregion
    }
}
