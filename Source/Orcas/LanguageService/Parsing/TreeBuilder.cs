/*

 Copyright (c) 2008 Jakub Misek

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Package;
using Microsoft.VisualStudio.TextManager.Interop;

using System.Collections;
using System.Reflection;

using PHP.Core;
using PHP.Core.Parsers;
using PHP.Core.AST;
using PHP.Core.Reflection;

namespace PHP.VisualStudio.PhalangerLanguageService.Parsing
{
    internal class PatternMatch
    {
        #region matching objects
        /// <summary>
        /// object that matches on the specified position of tokens array
        /// </summary>
        protected interface IMatching
        {
            /// <summary>
            /// matches on specified position of tokens array
            /// </summary>
            /// <param name="tokens">tokens array</param>
            /// <param name="tokenindex">token position, out next token position</param>
            /// <returns>true if matches</returns>
            bool Matches(string linecode, TokenInfo[] tokens, ref int tokenindex);

            /// <summary>
            /// result of the matching
            /// </summary>
            object Result { get; }

            /// <summary>
            /// Match position
            /// </summary>
            PHP.Core.Parsers.Position Position { get; }
        }

        /// <summary>
        /// Match one token (with specified number of repeats).
        /// </summary>
        protected class MatchingToken : IMatching
        {
            private readonly Tokens token;
            private readonly int min, max;
            private int ntimes = 0;
            private string _Result;
            public MatchingToken(Tokens token)
                :this(token,1,1)
            {
                
            }
            public MatchingToken(Tokens token, int min, int max)
            {
                this.token = token;
                this.min = min;
                this.max = max;
            }

            public bool Matches(string linecode, TokenInfo[] tokens, ref int tokenindex)
            {
                int tokenindex2 = tokenindex;
                int n = 0;
                while (tokenindex2 >= 0 && (Tokens)tokens[tokenindex2].Token == token && n < max)
                {
                    --tokenindex2;
                    ++n;
                }

                ntimes = n;

                if (n >= min && n <= max)
                {
                    if (n == 0)
                        _Result = string.Empty;
                    else
                        _Result = GetTokenText(linecode, tokens[tokenindex2+1], tokens[tokenindex]);
                    tokenindex = tokenindex2;
                    return true;
                }
                else
                {
                    
                    return false;
                }
            }

            public int Repeats
            {
                get
                {
                    return ntimes;
                }
            }

            /// <summary>
            /// the text of matched tokens
            /// </summary>
            public object Result
            {
                get
                {
                    return _Result;
                }
            }

            public PHP.Core.Parsers.Position Position
            {
                get
                {
                    return new PHP.Core.Parsers.Position();
                }
            }
        }

        /// <summary>
        /// Match selected tokens.
        /// </summary>
        protected class MatchingTokenList : IMatching
        {
            private readonly Tokens[]tokenlist;
            private string _Result;
            bool isOptional;

            public MatchingTokenList(Tokens[] tokenlist)
                :this(tokenlist,false)
            {

            }

            public MatchingTokenList(Tokens[] tokenlist, bool isOptional)
            {
                this.isOptional = isOptional;
                this.tokenlist = tokenlist;
            }

            public bool Matches(string linecode, TokenInfo[] tokens, ref int tokenindex)
            {
                if (tokenindex >= 0)
                    foreach (Tokens token in tokenlist)
                    {
                        if ((Tokens)tokens[tokenindex].Token == token)
                        {
                            _Result = GetTokenText(linecode, tokens[tokenindex]);
                            --tokenindex;
                            return true;
                        }
                    }

                if (isOptional)
                {
                    _Result = string.Empty;
                    return true;
                }                

                return false;
            }

            /// <summary>
            /// the text of matched token
            /// </summary>
            public object Result
            {
                get
                {
                    return _Result;
                }
            }

            public PHP.Core.Parsers.Position Position
            {
                get
                {
                    return new PHP.Core.Parsers.Position();
                }
            }
        }

        /// <summary>
        /// Match by the matching method.
        /// </summary>
        protected class MatchingByMethod : IMatching
        {
            public delegate object BuildTreeDelegate(string linecode, TokenInfo[] tokens, ref int tokenindex);

            private readonly BuildTreeDelegate BuildTree;

            private object _Result = null;

            private bool isOptional = false;

            public MatchingByMethod(BuildTreeDelegate BuildTree)
                :this(BuildTree,false)
            {
                
            }
            public MatchingByMethod(BuildTreeDelegate BuildTree, bool isOptional)
            {
                this.BuildTree = BuildTree;
                this.isOptional = isOptional;
            }

            public bool Matches(string linecode, TokenInfo[] tokens, ref int tokenindex)
            {
                int tokenindex2 = tokenindex;
                object exp = BuildTree(linecode, tokens, ref tokenindex2);

                if (exp == null)
                {
                    _Result = null;
                    return isOptional;
                }
                else
                {
                    _Result = exp;
                    tokenindex = tokenindex2;
                    return true;
                }
            }

            public Object Result
            {
                get
                {
                    return _Result;
                }
            }

            public PHP.Core.Parsers.Position Position
            {
                get
                {
                    return new PHP.Core.Parsers.Position();
                }
            }
        }

        /// <summary>
        /// Skip the block (right to left).
        /// (..) or [...] or {...} or combinations !!!
        /// the result of the operation (BlockStart) is the starting token of the block or 0.
        /// </summary>
        protected class MatchingBlock : IMatching
        {
            Tokens blockstarttoken = (Tokens)0;

            public bool Matches(string linecode, TokenInfo[] tokens, ref int tokenindex)
            {
                int level = 0;
                int tokenindex2 = tokenindex;

                do
                {
                    if (tokenindex2 >= 0)
                    {
                        // token on the position
                        TokenInfo tokeninfo = tokens[tokenindex2];
                        Tokens token = (Tokens)tokeninfo.Token;

                        switch (token)
                        {
                            case Tokens.T_RBRACE:
                            case Tokens.T_RBRACKET:
                            case Tokens.T_RPAREN:
                                ++level;
                                --tokenindex2;

                                break;
                            case Tokens.T_LBRACE:
                            case Tokens.T_LBRACKET:
                            case Tokens.T_LPAREN:
                                --level;
                                --tokenindex2;

                                if (level == 0) blockstarttoken = token;

                                break;
                            default:
                                if (level > 0)
                                {
                                    --tokenindex2;
                                }
                                break;
                        }
                    }
                    else
                    {
                        return false;
                    }

                } while (level > 0);

                // new token index
                tokenindex = tokenindex2;
                return true;
            }

            /// <summary>
            /// Founded block starts with (T_LBRACE|T_LBRACKET|T_LPAREN|0).
            /// </summary>
            public Tokens BlockStart
            {
                get
                {
                    return blockstarttoken;
                }
            }

            public Object Result
            {
                get
                {
                    return null;
                }
            }

            public PHP.Core.Parsers.Position Position
            {
                get
                {
                    return new PHP.Core.Parsers.Position();
                }
            }
        }

        /// <summary>
        /// Match the list of Matching objects (right to left).
        /// </summary>
        protected class MatchingPattern : IMatching
        {
            IMatching[] pattern;
            public MatchingPattern(IMatching[] pattern)
            {
                this.pattern = pattern;
            }

            public bool Matches(string linecode, TokenInfo[] tokens, ref int tokenindex)
            {
                int tokenindex2 = tokenindex;
                for (int i = pattern.Length - 1; i >= 0; --i)
                {
                    if (!pattern[i].Matches(linecode, tokens, ref tokenindex2))
                        return false;
                }

                // ok
                tokenindex = tokenindex2;
                return true;
            }

            public Object Result
            {
                get
                {
                    return null;
                }
            }

            public PHP.Core.Parsers.Position Position
            {
                get
                {
                    return new PHP.Core.Parsers.Position();
                }
            }
        }

        #endregion
        
        /// <summary>
        /// Get text of the given token.
        /// </summary>
        /// <param name="linecode"></param>
        /// <param name="tokeninfo"></param>
        /// <returns></returns>
        protected static string GetTokenText(string linecode, TokenInfo tokeninfo)
        {
            return GetTokenText(linecode,tokeninfo,tokeninfo);
        }

        /// <summary>
        /// Get text of the given token.
        /// </summary>
        /// <param name="linecode"></param>
        /// <param name="tokeninfo"></param>
        /// <returns></returns>
        protected static string GetTokenText(string linecode, TokenInfo tokeninfo1, TokenInfo tokeninfo2)
        {
            return linecode.Substring(tokeninfo1.StartIndex, tokeninfo2.EndIndex + 1 - tokeninfo1.StartIndex);
        }
    }

    /// <summary>
    /// Methods for building the partial AST from the array of tokens.
    /// </summary>
    internal class PartialTreeBuilder : PatternMatch
    {
        #region Tree building

        /// <summary>
        /// Token index where the incomplete declaration name is placed.
        /// </summary>
        protected int incompletetokenindex = -1;

        /// <summary>
        /// the language element which is not fully typed, in the IntelliSense there should be displayed similar variants
        /// </summary>
        protected LangElement NotCompletedElement = null;

        /// <summary>
        /// Build partial tree.
        /// </summary>
        /// <param name="linecode"></param>
        /// <param name="tokens"></param>
        /// <param name="righttokenindex"></param>
        /// <returns></returns>
        protected Expression BuildPartialTree(string linecode, TokenInfo[] tokens, ref int righttokenindex)
        {
            Expression result;

            // test know patterns, the order matters !

            // A::B // DirectStMtdCall, DirectStFldUse, ClassConstUse, A is QualifiedName
            if ((result = Match_StaticUse(linecode, tokens, ref righttokenindex) as Expression) != null)
                return result;

            /*// A::$b    // static property  // A is ClassName or indirect class name (expression)
            if ((result = Match_DirectStFldUse(linecode, tokens, ref righttokenindex) as Expression) != null)
                return result;

            // A::b     // static function or class const use
            if ((result = Match_ClassConstUse(linecode, tokens, ref righttokenindex) as Expression) != null)
                return result;*/

            // $a, ?->a, ?->$a, ?->
            if ((result = Match_VarLikeConstructUse(linecode, tokens, ref righttokenindex) as Expression) != null)
                return result;

            // A:::B, B
            if ((result = Match_GlobalConstUse(linecode, tokens, ref righttokenindex) as Expression) != null)
                return result;
            
            return null;
        }

        /// <summary>
        /// Build partial tree with incomplete last declaration name.
        /// </summary>
        /// <param name="linecode"></param>
        /// <param name="tokens"></param>
        /// <param name="righttokenindex"></param>
        /// <returns>Always some expression, cannot be null.</returns>
        public Expression BuildIncompletePartialTree(string linecode, TokenInfo[] tokens, ref int righttokenindex, out LangElement NotCompletedElement)
        {
            incompletetokenindex = righttokenindex;

            Expression exp = BuildPartialTree(linecode, tokens, ref righttokenindex);

            if (exp == null)
            {   // any local declaration
                exp = new DirectVarUse(new Position(), new VariableName());  // any local declaration
                this.NotCompletedElement = exp;
            }

            NotCompletedElement = this.NotCompletedElement;

            return exp;
        }

        /// <summary>
        /// Build partial tree with completed declaration names.
        /// </summary>
        /// <param name="linecode"></param>
        /// <param name="tokens"></param>
        /// <param name="righttokenindex"></param>
        /// <returns></returns>
        public Expression BuildCompletedPartialTree(string linecode, TokenInfo[] tokens, ref int righttokenindex)
        {
            incompletetokenindex = -1;

            return BuildPartialTree(linecode, tokens, ref righttokenindex);
        }
        
        #endregion


        #region matching methods
        #region VarLikeConstructUse
        /// <summary>
        /// Building VarLikeConstructUse
        /// </summary>
        internal class VarLikeConstructUseBuilder
        {
            /// <summary>
            /// Dollars count
            /// </summary>
            public readonly int DollarsCount;

            /// <summary>
            /// Variable name.
            /// </summary>
            public readonly string VarName;

            /// <summary>
            /// VarName is followed by ...
            /// </summary>
            public readonly FollowedBy Followed;

            /// <summary>
            /// VarName is followed by ...
            /// </summary>
            public enum FollowedBy
            {
                Nothing, Parentheses, Braces
            }

            /// <summary>
            /// Init.
            /// </summary>
            /// <param name="DollarsCount"></param>
            /// <param name="VarName"></param>
            public VarLikeConstructUseBuilder(int DollarsCount, string VarName, FollowedBy Followed)
            {
                this.DollarsCount = DollarsCount;
                this.VarName = VarName;
                this.Followed = Followed;
            }
        }

        /// <summary>
        /// VarLikeConstructUseBuilder
        /// Collects info about    $..[identifier | varname[...] | varname(...) | {...} ]  // TODO: ending with (),[],{}
        /// </summary>
        /// <param name="linecode"></param>
        /// <param name="tokens"></param>
        /// <param name="tokenindex"></param>
        /// <returns></returns>
        private VarLikeConstructUseBuilder Match_VarLikeConstructUseBuilder(string linecode, TokenInfo[] tokens, ref int tokenindex)
        {
            bool varnameIsOptional = (tokenindex == incompletetokenindex);

            MatchingToken dollars = new MatchingToken(Tokens.T_DOLLAR, 0, int.MaxValue);    // [0 - inf.] dollars
            MatchingTokenList varname = new MatchingTokenList(new Tokens[] { Tokens.T_STRING, Tokens.T_VARIABLE }, varnameIsOptional);
            MatchingBlock blockafter = new MatchingBlock();
            
            //
            // $..$varname
            //
            MatchingPattern pattern = new MatchingPattern(new IMatching[] { dollars, varname, blockafter });

            int tokenindex2 = tokenindex;
            if (pattern.Matches(linecode, tokens, ref tokenindex2))
            {
                // count dollars
                int ndollars = dollars.Repeats;
                if (((string)varname.Result).StartsWith("$"))
                    ++ndollars;

                string varnamevalue = ((string)varname.Result).TrimStart(new char[] { '$' }).Trim();

                // followed by ?
                VarLikeConstructUseBuilder.FollowedBy followed = VarLikeConstructUseBuilder.FollowedBy.Nothing;
                if (IsFollowedBy(tokens, tokenindex, Tokens.T_LPAREN))
                    followed = VarLikeConstructUseBuilder.FollowedBy.Parentheses;
                /*else if (IsFollowedBy(tokens, tokenindex, Tokens.T_LBRACKET))
                    followed = VarLikeConstructUseBuilder.FollowedBy.Braces;*/

                switch (blockafter.BlockStart)
                {
                    case Tokens.T_LBRACKET:
                        followed = VarLikeConstructUseBuilder.FollowedBy.Braces;
                        break;
                    case Tokens.T_LPAREN:
                        followed = VarLikeConstructUseBuilder.FollowedBy.Parentheses;
                        break;
                    default:
                        break;
                }
                
                // $..$varnamevalue
                tokenindex = tokenindex2;
                return new VarLikeConstructUseBuilder(ndollars, varnamevalue, followed);
            }                

            return null;
        }

        /// <summary>
        /// VarLikeConstructUse builder.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="isMemberOf"></param>
        /// <param name="tokenindex">index of the last token of the varname</param>
        /// <param name="position"></param>
        /// <returns></returns>
        public VarLikeConstructUse BuildVarLikeConstructUse(VarLikeConstructUseBuilder builder, VarLikeConstructUse isMemberOf, int tokenindex, Position position)
        {
            int nindirects = (isMemberOf == null) ? (builder.DollarsCount - 1) : (builder.DollarsCount);

            // variable use
            VarLikeConstructUse retvar = null;

            if (nindirects >= 0)   //(!(isMemberOf == null && builder.DollarsCount == 0))
            {
                // is followed by ( ? => function call
                switch (builder.Followed)
                {
                    case VarLikeConstructUseBuilder.FollowedBy.Nothing:
                    case VarLikeConstructUseBuilder.FollowedBy.Braces:
                        retvar = new DirectVarUse(position, (builder.DollarsCount == 0) ? (builder.VarName) : ("$" + builder.VarName));  // direct var use
                        if (builder.Followed == VarLikeConstructUseBuilder.FollowedBy.Braces)
                            retvar = new ItemUse(position, (DirectVarUse)retvar, null);
                        break;
                    case VarLikeConstructUseBuilder.FollowedBy.Parentheses:
                        retvar = new DirectFcnCall(position, new QualifiedName(new Name(builder.VarName)), new List<ActualParam>(), new List<TypeRef>());
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
            else
            {
                // not handled
                return null;
            }

            // not fully typed declaration name
            if (tokenindex == incompletetokenindex)
            {
                NotCompletedElement = retvar;
            }

            // indirects            
            if (nindirects > 0)
            {
                retvar = new IndirectVarUse(position, nindirects, retvar);
            }

            // done
            retvar.IsMemberOf = isMemberOf;
            return retvar;
        }
        #endregion
        /// <summary>
        /// VarLikeConstructUse (DirectVarUse,IndirectVarUse,ItemUse,DirectFcnCall,...)
        /// </summary>
        /// <param name="linecode"></param>
        /// <param name="tokens"></param>
        /// <param name="tokenindex"></param>
        /// <returns></returns>
        private VarLikeConstructUse Match_VarLikeConstructUse(string linecode, TokenInfo[] tokens, ref int tokenindex)
        {
            MatchingByMethod varname = new MatchingByMethod(Match_VarLikeConstructUseBuilder);
            
            int tokenindex2 = tokenindex;
            if (varname.Matches(linecode, tokens, ref tokenindex2))
            {
                // try to match isMemberOf
                VarLikeConstructUse isMemberOf = Match_isMemberOf(linecode, tokens, ref tokenindex2);

                // VarLikeConstructUseBuilder
                VarLikeConstructUseBuilder varnameResult = (VarLikeConstructUseBuilder)varname.Result;

                VarLikeConstructUse ret = BuildVarLikeConstructUse(varnameResult, isMemberOf, tokenindex, varname.Position);

                if (ret != null)
                {
                    tokenindex = tokenindex2;
                    return ret;
                }
            }

            return null;
        }

        /// <summary>
        /// VarLikeConstructUse
        /// </summary>
        /// <param name="linecode"></param>
        /// <param name="tokens"></param>
        /// <param name="tokenindex"></param>
        /// <returns></returns>
        private VarLikeConstructUse Match_isMemberOf(string linecode, TokenInfo[] tokens, ref int tokenindex)
        {
            MatchingByMethod varuse = new MatchingByMethod(Match_VarLikeConstructUse);
            MatchingToken objectoperator = new MatchingToken(Tokens.T_OBJECT_OPERATOR);
            
            // VarLikeConstructUse->
            MatchingPattern pattern = new MatchingPattern(
                new IMatching[] { varuse, objectoperator });

            if (pattern.Matches(linecode, tokens, ref tokenindex))
            {
                return varuse.Result as VarLikeConstructUse;
            }

            return null;
        }

        /// <summary>
        /// DirectStMtdCall, DirectStFldUse, ClassConstUse
        /// 
        /// QN::constant
        /// QN::$staticProperty
        /// QN::staticFnc()
        /// </summary>
        /// <param name="linecode"></param>
        /// <param name="tokens"></param>
        /// <param name="tokenindex"></param>
        /// <returns></returns>
        private Expression Match_StaticUse(string linecode, TokenInfo[] tokens, ref int tokenindex)
        {
            MatchingByMethod qualifiedname = new MatchingByMethod(Match_QualifiedName);

            // QN::
            if (incompletetokenindex == tokenindex)
            {
                MatchingPattern pattern = new MatchingPattern(
                    new IMatching[] { qualifiedname, new MatchingToken(Tokens.T_DOUBLE_COLON) });

                if (pattern.Matches(linecode, tokens, ref tokenindex))
                {
                    ClassConstUse retvar = new ClassConstUse(pattern.Position, new GenericQualifiedName((QualifiedName)qualifiedname.Result), string.Empty);

                    NotCompletedElement = retvar;
                    return retvar;
                }
            }

            // QN::$
            if (incompletetokenindex == tokenindex)
            {
                MatchingPattern pattern = new MatchingPattern(
                    new IMatching[] {
                                    qualifiedname,
                                    new MatchingToken(Tokens.T_DOUBLE_COLON),
                                    new MatchingToken(Tokens.T_DOLLAR)
                                });

                if (pattern.Matches(linecode, tokens, ref tokenindex))
                {
                    DirectStFldUse retvar = new DirectStFldUse(pattern.Position, new GenericQualifiedName((QualifiedName)qualifiedname.Result), new VariableName(string.Empty));

                    NotCompletedElement = retvar;
                    return retvar;
                }
            }

            // QN::string
            // QN::string (
            {
                IMatching varname = new MatchingToken(Tokens.T_STRING);
                MatchingPattern pattern = new MatchingPattern(
                    new IMatching[] {
                                    qualifiedname,
                                    new MatchingToken(Tokens.T_DOUBLE_COLON),
                                    varname
                                });

                int curtokenindex = tokenindex;
                if (pattern.Matches(linecode, tokens, ref tokenindex))
                {
                    if (IsFollowedBy(tokens, curtokenindex, Tokens.T_LPAREN))
                    {
                        DirectStMtdCall retvar = new DirectStMtdCall(pattern.Position, new GenericQualifiedName((QualifiedName)qualifiedname.Result), new Name(((string)varname.Result).Trim()), new List<ActualParam>(), new List<TypeRef>());

                        NotCompletedElement = retvar;
                        return retvar;
                    }
                    else
                    {
                        ClassConstUse retvar = new ClassConstUse(pattern.Position, new GenericQualifiedName((QualifiedName)qualifiedname.Result), ((string)varname.Result).Trim());

                        NotCompletedElement = retvar;
                        return retvar;
                    }
                }
            }
            
            
            // QN::variablename
            {
                IMatching varname = new MatchingToken(Tokens.T_VARIABLE);
                MatchingPattern pattern = new MatchingPattern(
                    new IMatching[] {
                                    qualifiedname,
                                    new MatchingToken(Tokens.T_DOUBLE_COLON),
                                    varname
                                });

                if (pattern.Matches(linecode, tokens, ref tokenindex))
                {
                    DirectStFldUse retvar = new DirectStFldUse(pattern.Position, new GenericQualifiedName((QualifiedName)qualifiedname.Result), new VariableName(((string)varname.Result).Trim()));

                    NotCompletedElement = retvar;
                    return retvar;
                }
            }

            return null;
        }

        /// <summary>
        /// returns DirectStFldUse
        /// </summary>
        /// <param name="linecode"></param>
        /// <param name="tokens"></param>
        /// <param name="tokenindex"></param>
        /// <returns></returns>
        private DirectStFldUse Match_DirectStFldUse(string linecode, TokenInfo[] tokens, ref int tokenindex)
        {
            MatchingByMethod qualifiedname = new MatchingByMethod(Match_QualifiedName);

            if (qualifiedname.Result == null)
                return null;

            IMatching variablename;
            
            if (incompletetokenindex != tokenindex)
            {
                variablename = new MatchingToken(Tokens.T_VARIABLE);
            }
            else
            {   // the variablename should be an empty string "", "$" or "$variablename"
                variablename = new MatchingTokenList(new Tokens[] { Tokens.T_DOLLAR, Tokens.T_VARIABLE }, true);
            }

            // XXX::$variablename
            MatchingPattern pattern = new MatchingPattern(
                new IMatching[] { qualifiedname, new MatchingToken(Tokens.T_DOUBLE_COLON), variablename });

            int tokenindex2 = tokenindex;
            if (pattern.Matches(linecode, tokens, ref tokenindex2))
            {
                // try to match isMemberOf
                VarLikeConstructUse isMemberOf = Match_isMemberOf(linecode, tokens, ref tokenindex2);

                DirectStFldUse retvar = new DirectStFldUse(pattern.Position, new GenericQualifiedName((QualifiedName)qualifiedname.Result), new VariableName(((string)variablename.Result).Trim()));
                
                retvar.IsMemberOf = isMemberOf;

                // not fully typed declaration name
                if(tokenindex == incompletetokenindex)
                {
                    NotCompletedElement = retvar;
                }

                tokenindex = tokenindex2;
                return retvar;
            }

            return null;
        }
        /// <summary>
        /// returns ClassConstUse
        /// </summary>
        /// <param name="linecode"></param>
        /// <param name="tokens"></param>
        /// <param name="tokenindex"></param>
        /// <returns></returns>
        private Expression Match_ClassConstUse(string linecode, TokenInfo[] tokens, ref int tokenindex)
        {
            MatchingByMethod qualifiedname = new MatchingByMethod(Match_QualifiedName); // A or A:::B:::C
            IMatching variablename;
            
            if (incompletetokenindex != tokenindex)
            {
                variablename = new MatchingByMethod(Match_String);
            }
            else
            {   // string "constant" or empty string ""
                variablename = new MatchingTokenList(new Tokens[] { Tokens.T_STRING }, true);
            }            

            // XXX::constant
            MatchingPattern pattern = new MatchingPattern(
                new IMatching[]{qualifiedname,new MatchingToken(Tokens.T_DOUBLE_COLON),variablename} );

            int tokenindex2 = tokenindex;
            if (pattern.Matches(linecode, tokens, ref tokenindex2))
            {
                Expression retvar;

                if (IsFollowedBy(tokens, tokenindex, Tokens.T_LPAREN))
                {
                    QualifiedName parentqname = ((qualifiedname.Result == null) ? (new QualifiedName()) : ((QualifiedName)qualifiedname.Result));

                    List<Name> parentnames = new List<Name>();
                    if (parentqname.Namespaces != null)
                        foreach (Name n in parentqname.Namespaces)
                            parentnames.Add(n);
                    
                    if(parentqname.Name.Value != null)
                        parentnames.Add(parentqname.Name);

                    QualifiedName newqname = new QualifiedName(new Name(((string)variablename.Result).Trim()), parentnames.ToArray());

                    retvar = new DirectFcnCall(pattern.Position, newqname, new List<ActualParam>(), new List<TypeRef>());
                }
                else
                {
                    retvar = new ClassConstUse(pattern.Position, new GenericQualifiedName((qualifiedname.Result == null)?(new QualifiedName()):((QualifiedName)qualifiedname.Result)), ((string)variablename.Result).Trim());
                }

                if (tokenindex == incompletetokenindex)
                {
                    NotCompletedElement = retvar;
                }

                tokenindex = tokenindex2;
                return retvar;
            }

            return null;
        }

        /// <summary>
        /// returns GlobalConstUse
        /// </summary>
        /// <param name="linecode"></param>
        /// <param name="tokens"></param>
        /// <param name="tokenindex"></param>
        /// <returns></returns>
        private GlobalConstUse Match_GlobalConstUse(string linecode, TokenInfo[] tokens, ref int tokenindex)
        {
            // namespace or string alone

            MatchingByMethod qualifiedname = new MatchingByMethod(Match_QualifiedName); // A or A:::B:::C
            
            MatchingPattern pattern;

            int tokenindex2 = tokenindex;

            // A or A:::B:::C
            pattern = new MatchingPattern(new IMatching[] { qualifiedname });

            if (pattern.Matches(linecode, tokens, ref tokenindex2))
            {
                GlobalConstUse retvar = new GlobalConstUse(qualifiedname.Position, (QualifiedName)qualifiedname.Result);

                if (tokenindex == incompletetokenindex)
                {
                    NotCompletedElement = retvar;
                }

                tokenindex = tokenindex2;
                return retvar;
            }


            // just typing this name ?
            if (tokenindex == incompletetokenindex)
            {   // A:::B:::?
                pattern = new MatchingPattern(
                    new IMatching[] { qualifiedname, new MatchingToken(Tokens.T_DOUBLE_COLON), new MatchingToken(Tokens.T_COLON) });

                tokenindex2 = tokenindex;
                if (pattern.Matches(linecode, tokens, ref tokenindex2))
                {
                    QualifiedName parentqname = (QualifiedName)qualifiedname.Result;

                    List<Name> parentnames = new List<Name>();
                    if (parentqname.Namespaces != null)
                        foreach (Name n in parentqname.Namespaces)
                            parentnames.Add(n);
                    parentnames.Add(parentqname.Name);

                    QualifiedName newqname = new QualifiedName(new Name(), parentnames.ToArray());
                    GlobalConstUse retvar = new GlobalConstUse(qualifiedname.Position, newqname);

                    NotCompletedElement = retvar;
                    tokenindex = tokenindex2;
                    return retvar;
                }
            }

            return null;
        }

        /// <summary>
        /// returns QualifiedName
        /// </summary>
        /// <param name="linecode"></param>
        /// <param name="tokens"></param>
        /// <param name="tokenindex"></param>
        /// <returns></returns>
        private object Match_QualifiedName(string linecode, TokenInfo[] tokens, ref int tokenindex)
        {
            MatchingByMethod name = new MatchingByMethod(Match_String);
            MatchingByMethod namespc = new MatchingByMethod(Match_Namespace);

            // A:::B:::C
            if (namespc.Matches(linecode, tokens, ref tokenindex))
            {
                Name[] names = namespc.Result as Name[];
                List<Name> namespacename = new List<Name>();

                for (int i = 0; i < names.Length - 1; ++i)
                    namespacename.Add(names[i]);

                return new QualifiedName(names[names.Length - 1], namespacename.ToArray());
            }

            // A
            if (name.Matches(linecode, tokens, ref tokenindex))
            {
                return new QualifiedName(new Name(((string)name.Result).Trim()));
            }

            return null;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="linecode"></param>
        /// <param name="tokens"></param>
        /// <param name="tokenindex"></param>
        /// <returns></returns>
        private Name[] Match_Namespace(string linecode, TokenInfo[] tokens, ref int tokenindex)
        {
            MatchingToken namespacename = new MatchingToken(Tokens.T_NAMESPACE_NAME);

            if (namespacename.Matches(linecode, tokens, ref tokenindex))
            {
                string strname = namespacename.Result as string;
                string[] strnames = strname.Split(new string[] { QualifiedName.Separator }, StringSplitOptions.RemoveEmptyEntries);

                Name[] names = new Name[strnames.Length];
                for (int i = 0; i < names.Length; ++i)
                    names[i] = new Name(strnames[i].Trim());

                return names;
            }


            return null;
        }

        private string Match_String(string linecode, TokenInfo[] tokens, ref int tokenindex)
        {
            if (tokenindex >= 0 && tokenindex < tokens.Length && TokenShouldBeIdentifier((Tokens)tokens[tokenindex].Token))
            {
                TokenInfo t = tokens[tokenindex];
                --tokenindex;
                return linecode.Substring(t.StartIndex, t.EndIndex + 1 - t.StartIndex);
            }

            return null;
        }
        private string Match_StringAndVariable(string linecode, TokenInfo[] tokens, ref int tokenindex)
        {
            if (tokenindex >= 0 && tokenindex < tokens.Length && (TokenShouldBeIdentifier((Tokens)tokens[tokenindex].Token) || (Tokens)tokens[tokenindex].Token == Tokens.T_VARIABLE))
            {
                TokenInfo t = tokens[tokenindex];
                --tokenindex;
                return linecode.Substring(t.StartIndex, t.EndIndex + 1 - t.StartIndex);
            }

            return null;
        }

        #endregion

        #region helping methods

        /// <summary>
        /// Check if the token on specified position is followed by specified token.
        /// </summary>
        /// <param name="tokens">Array of tokens.</param>
        /// <param name="tokenindex">Current token index.</param>
        /// <param name="isfollowing">Token to be checked if is following.</param>
        /// <returns></returns>
        private bool IsFollowedBy(TokenInfo[] tokens, int tokenindex, Tokens isfollowing)
        {
            return (tokenindex >= 0 && tokenindex + 1 < tokens.Length && (Tokens)tokens[tokenindex + 1].Token == isfollowing);
        }

        /// <summary>
        /// Check if the given token should be function name.
        /// </summary>
        /// <param name="token">Token.</param>
        /// <returns>True if token is some identifier. (string + native function names + types)</returns>
        private bool TokenShouldBeIdentifier(Tokens token)
        {
            return (token == Tokens.T_STRING ||
                    token == Tokens.T_CONSTRUCT ||
                    token == Tokens.T_DESTRUCT ||
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
                    token == Tokens.T_VAR ||
                    token == Tokens.T_STRING_TYPE ||
                    token == Tokens.T_INT_TYPE);
        }

        #endregion


        /*/// <summary>
        /// Code scope.
        /// </summary>
        protected readonly ScopeInfo globalscope;

        /// <summary>
        /// Init.
        /// </summary>
        /// <param name="Scope">Code scope.</param>
        public PartialTreeBuilder(ScopeInfo globalscope)
        {
            this.globalscope = globalscope;
        }

        /// <summary>
        /// Build partial AST used for intellisense.
        /// </summary>
        /// <param name="line">Current line index.</param>
        /// <param name="col">Current column index.</param>
        /// <param name="tokens">Array of tokens on the current line.</param>
        /// <param name="linecode">Source code on the current line.</param>
        /// <param name="tokenindex">Index of the token where parsing will be started.</param>
        /// <returns>Partial AST expression (Variable use or Constant use).</returns>
        public Expression BuildPartialTree(int line, int col, TokenInfo[] tokens, string linecode, int righttokenindex)
        {
            if (tokens == null || righttokenindex >= tokens.Length)
                return null;

            Expression ret = BuildPartialTree(line, col, globalscope.GetScopeAt(line, col), tokens, linecode, righttokenindex);

            if (ret == null)
            {
                ret = new DirectVarUse(new Position(line, col, col, line, col, col), string.Empty);
            }

            return ret;
        }

        /// <summary>
        /// Build partial AST used for intellisense.
        /// </summary>
        /// <param name="line">Current line index.</param>
        /// <param name="col">Current column index.</param>
        /// <param name="scope">Local scope.</param>
        /// <param name="tokens">Array of tokens on the current line.</param>
        /// <param name="linecode">Source code on the current line.</param>
        /// <param name="righttokenindex">Index of the token where parsing will be started.</param>
        /// <returns>Partial AST expression (Variable use or Constant use) or null.</returns>
        private Expression BuildPartialTree(int line, int col, ScopeInfo scope, TokenInfo[] tokens, string linecode, int righttokenindex)
        {
            if (righttokenindex < 0)
                return null;

            // token on the position
            TokenInfo tokeninfo = tokens[righttokenindex];
            Tokens token = (Tokens)tokeninfo.Token;
            string tokentext = GetTokenText(linecode, tokeninfo);

            Position position = new Position(line,tokeninfo.StartIndex,tokeninfo.StartIndex,line,tokeninfo.EndIndex,tokeninfo.EndIndex);

            // 
            switch (token)
            {
                case Tokens.T_RBRACE:   // }    // indirect variable by expression
                    // todo
                    break;
                case Tokens.T_RBRACKET: // ]    // array access
                    // skip array index expression
                    righttokenindex = SkipBlock(tokens, righttokenindex);
                    return BuildPartialTree(line, col, scope, tokens, linecode, righttokenindex);
                case Tokens.T_RPAREN:   // )    // function call
                    // skip function call parameters
                    righttokenindex = SkipBlock(tokens, righttokenindex);
                    return BuildPartialTree(line, col, scope, tokens, linecode, righttokenindex);
                default:
                    // identifier (DirectVarUse, IndirectVarUse, DirectFncCall)
                    if (PhpScope.TokenShouldBeFunctionName(token))
                    {
                        // after ::             // ClassConstUse, DirectStaticFieldUse, IndirectStaticFieldUse, 

                        // after :::            // GlobalConstUse

                        // else (after $ or -> or nothing)  // DirectFcnCall, IndirectFcnCall, VarUse, DirectVarUse, IndirectVarUse, ...
                        
                        // 1) followed by ( // function call
                        if (IsFollowedBy(tokens, righttokenindex, Tokens.T_LPAREN))
                        {
                            DirectFcnCall fcncall = new DirectFcnCall(position, new QualifiedName(new Name(tokentext)), null, null);
                            Expression isMemberOf;

                            Expression result = BuildIndirectUse(line, col, scope, tokens, linecode, righttokenindex - 1, fcncall, tokentext != null && tokentext.StartsWith("$"), out isMemberOf);
                            fcncall.IsMemberOf = isMemberOf as VarLikeConstructUse;

                            return result;
                        }
                        // 2) followed by [ // array use
                        else if (IsFollowedBy(tokens, righttokenindex, Tokens.T_LBRACKET))
                        {
                            // not implemented yet
                        }
                        // 3) else // variable use
                        else
                        {
                            DirectVarUse varuse = new DirectVarUse(position, tokentext.TrimStart(new char[] { '$' }));
                            Expression isMemberOf;

                            Expression result = BuildIndirectUse(line, col, scope, tokens, linecode, righttokenindex - 1, varuse, tokentext != null && tokentext.StartsWith("$"), out isMemberOf);
                            varuse.IsMemberOf = isMemberOf as VarLikeConstructUse;

                            return result;
                        }
                    }
                    break;
            }

            //
            // last token should be empty yet
            //

            // try to build expression before ->

            // try to build expression before ::

            // try to build expression before :::


            // nothing recognized
            return null;
        }

        /// <summary>
        /// Build call or indirect call of the given expression.
        /// </summary>
        /// <param name="line"></param>
        /// <param name="col"></param>
        /// <param name="scope"></param>
        /// <param name="tokens"></param>
        /// <param name="linecode"></param>
        /// <param name="righttokenindex">Current position.</param>
        /// <param name="varNameEx">The expression after the given position.</param>
        /// <param name="varStartsWithDollar">varNameEx name starts with dollar</param>
        /// <param name="varIsMemberOf">The value to be assigned to varNameEx.isMemberOf</param>
        /// <returns></returns>
        private Expression BuildIndirectUse(int line, int col, ScopeInfo scope, TokenInfo[] tokens, string linecode, int righttokenindex, Expression varNameEx, bool varStartsWithDollar, out Expression varIsMemberOf)
        {
            varIsMemberOf = null;

            if (righttokenindex < 0)
                return varNameEx;

            // skip and count dollars
            int dollars = 0;

            while (righttokenindex >= 0 && (Tokens)tokens[righttokenindex].Token == Tokens.T_DOLLAR)
            {
                ++dollars;
                --righttokenindex;
            }

            // is member of
            varIsMemberOf = BuildMemberOf(line, col, scope, tokens, linecode, righttokenindex);

            if (varIsMemberOf != null && varStartsWithDollar)
            {
                // check the dollar in varNameEx too if varname follows object
                ++dollars;
            }
            
            // encapsulate indirects (dollars-time)
            if (dollars > 0)
            {
                VarLikeConstructUse indirect = new IndirectVarUse(varNameEx.Position, dollars, varNameEx);
                indirect.IsMemberOf = varIsMemberOf as VarLikeConstructUse;
                varIsMemberOf = null;

                // done
                return indirect;
            }
            else
            {
                // done
                return varNameEx;
            }

        }

        /// <summary>
        /// Build expression before an object operator.
        /// </summary>
        /// <param name="line">Current line index.</param>
        /// <param name="col">Current column index.</param>
        /// <param name="scope">Local scope.</param>
        /// <param name="tokens">Array of tokens on the current line.</param>
        /// <param name="linecode">Source code on the current line.</param>
        /// <param name="righttokenindex">Index of the token where parsing will be started.</param>
        /// <returns>Expression before -></returns>
        private Expression BuildMemberOf(int line, int col, ScopeInfo scope, TokenInfo[] tokens, string linecode, int righttokenindex)
        {
            if (righttokenindex < 0)
                return null;

            // token on the position
            TokenInfo tokeninfo = tokens[righttokenindex];
            Tokens token = (Tokens)tokeninfo.Token;
            
            // 
            switch (token)
            {
                case Tokens.T_OBJECT_OPERATOR: // ->    // object members
                    return BuildPartialTree(line, col, scope, tokens, linecode, righttokenindex - 1);
                case Tokens.T_DOUBLE_COLON: // ::   // const object
                    break;
                case Tokens.T_COLON:    // :::  // namespace delimiter
                    break;
                default:
                    // not handled
                    break;
            }

            // nothing recognized
            return null;
        }

        /// <summary>
        /// Check if the token on specified position is followed by specified token.
        /// </summary>
        /// <param name="tokens">Array of tokens.</param>
        /// <param name="tokenindex">Current token index.</param>
        /// <param name="isfollowing">Token to be checked if is following.</param>
        /// <returns></returns>
        private bool IsFollowedBy(TokenInfo[] tokens, int tokenindex, Tokens isfollowing)
        {
            return (tokenindex >= 0 && tokenindex + 1 < tokens.Length && (Tokens)tokens[tokenindex + 1].Token == isfollowing);
        }

        /// <summary>
        /// Skip block ending in the given position.
        /// Returns new token index.
        /// </summary>
        /// <param name="tokens">Array of tokens.</param>
        /// <param name="tokenindex">Index of the token where the block ends.</param>
        /// <returns>Nw token index before the block.</returns>
        private int SkipBlock(TokenInfo[] tokens, int tokenindex)
        {
            int level = 0;

            do
            {
                if (tokenindex >= 0)
                {
                    // token on the position
                    TokenInfo tokeninfo = tokens[tokenindex];
                    Tokens token = (Tokens)tokeninfo.Token;

                    switch (token)
                    {
                        case Tokens.T_RBRACE:
                        case Tokens.T_RBRACKET:
                        case Tokens.T_RPAREN:
                            ++level;
                            break;
                        case Tokens.T_LBRACE:
                        case Tokens.T_LBRACKET:
                        case Tokens.T_LPAREN:
                            --level;
                            break;
                    }

                    --tokenindex;
                }
                else
                {
                    break;
                }

            } while (level > 0);

            // new token index
            return tokenindex;
        }*/

        
    }
}