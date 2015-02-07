/*

 Copyright (c) 2013 Jakub Misek

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/


using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Diagnostics;
using PHP.Core.Emit;
using PHP.Core.Parsers;

namespace PHP.Core.AST
{
    /// <summary>
    /// Represents <c>yield</c> expression for the support for PHP Generator.
    /// </summary>
    public sealed class YieldEx : Expression
    {
        #region Fields & Properties

        /// <summary>
        /// Represents the key expression in case of <c>yield key =&gt; value</c> form.
        /// Can be a <c>null</c> reference in case of key is not provided.
        /// </summary>
        public Expression KeyExpr { get { return _keyEx; } }

        /// <summary>
        /// Represents the value expression in case of <c>yield key =&gt; value</c> or <c>yield value</c> forms.
        /// Can be a <c>null</c> reference in case of yield is used in read context. (see Generator::send()).
        /// </summary>
        public Expression ValueExpr { get { return _valueEx; } }

        /// <summary>
        /// <c>yield</c> parameters.
        /// </summary>
        private readonly Expression _keyEx, _valueEx;

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes new instance of <see cref="YieldEx"/>.
        /// </summary>
        public YieldEx(Position position)
            : this(position, null, null)
        {
        }

        /// <summary>
        /// Initializes new instance of <see cref="YieldEx"/>.
        /// </summary>
        public YieldEx(Position position, Expression keyEx, Expression valueEx)
            : base(position)
        {
            if (keyEx != null && valueEx == null) throw new ArgumentException();

            _keyEx = keyEx;
            _valueEx = valueEx;
        }

        #endregion

        #region Expression

        public override Operations Operation { get { return Operations.Yield; } }

        internal override Evaluation Analyze(Analyzer analyzer, ExInfoFromParent info)
        {
            throw new NotImplementedException();
        }

        internal override PhpTypeCode Emit(CodeGenerator codeGenerator)
        {
            throw new NotImplementedException();
        }

        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitYieldEx(this);
        }

        #endregion
    }
}
