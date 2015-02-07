/*

 Copyright (c) 2004-2006 Tomas Matousek, Vaclav Novak and Martin Maly.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Diagnostics;

using PHP.Core.Parsers;

namespace PHP.Core.AST
{
    /// <summary>
    /// Represents a concatenation expression (dot PHP operator).
    /// </summary>
    [Serializable]
    public sealed class ConcatEx : Expression
    {
        public override Operations Operation { get { return Operations.ConcatN; } }

        public Expression[]/*!*/ Expressions { get { return this.expressions; } internal set { this.expressions = value; } }
        private Expression[]/*!*/ expressions;

        /// <summary>
        /// Initialize the ConcatEx AST node and optimize the subtree if possible. Look for child expressions and chain possible concatenations. This prevents StackOverflowException in case of huge concatenation expressions.
        /// </summary>
        /// <param name="span"></param>
        /// <param name="expressions">List of expressions to concatenate.</param>
        /// <remarks>This method tries to propagate child concatenations and chain them.</remarks>
        public ConcatEx(Text.Span span, IList<Expression>/*!*/ expressions)
            : base(span)
        {
            Debug.Assert(expressions != null);
            this.expressions = expressions.AsArray();
        }

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitConcatEx(this);
        }
    }
}

