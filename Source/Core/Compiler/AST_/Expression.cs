using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using PHP.Core.Parsers;
using PHP.Core.Reflection;
using PHP.Core.Emit;
using PHP.Core.AST;

namespace PHP.Core.Compiler.AST
{
    partial class NodeCompilers
    {
        abstract class ExpressionCompiler : IExpressionCompiler
        {
            public AccessType Access { get { return access; } }
            protected AccessType access = AccessType.None;

            #region IExpressionCompiler

            Evaluation IExpressionCompiler.EvaluatePriorAnalysis(Expression node, Reflection.SourceUnit sourceUnit)
            {
                // in-evaluable by default:
                return new Evaluation(node);
            }

            public abstract Evaluation Analyze(Expression node, Analyzer analyzer, ExInfoFromParent info);

            public abstract PhpTypeCode Emit(Expression node, CodeGenerator codeGenerator);

            object IExpressionCompiler.Evaluate(Expression node, object value)
            {
                return null;
            }

            object IExpressionCompiler.Evaluate(Expression node, object leftValue, object rightValue)
            {
                return null;
            }

            bool IExpressionCompiler.IsDeeplyCopied(Expression node, CopyReason reason, int nestingLevel)
            {
                return true;
            }

            bool IExpressionCompiler.StoreOnAssignment(Expression node)
            {
                return true;
            }

            #endregion
        }
    }
}
