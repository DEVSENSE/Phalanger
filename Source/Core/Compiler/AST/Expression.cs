using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        #region ExpressionCompiler

        abstract class ExpressionCompiler<T> : IExpressionCompiler where T : Expression
        {
            #region Generic IExpressionCompiler implementation

            public virtual object GetValue(T node) { return null; }

            public virtual PhpTypeCode GetValueTypeCode(T node) { return PhpTypeCode.Unknown; }

            public virtual Evaluation EvaluatePriorAnalysis(T node, CompilationSourceUnit sourceUnit)
            {
                // in-evaluable by default:
                return new Evaluation(node);
            }

            public abstract Evaluation Analyze(T node, Analyzer analyzer, ExInfoFromParent info);

            public abstract PhpTypeCode Emit(T node, CodeGenerator codeGenerator);

            public virtual object Evaluate(T node, object value)
            {
                return null;
            }

            public virtual object Evaluate(T node, object leftValue, object rightValue)
            {
                return null;
            }

            public virtual bool IsDeeplyCopied(T node, CopyReason reason, int nestingLevel)
            {
                return true;
            }

            public virtual bool StoreOnAssignment(T node)
            {
                return true;
            }

            public virtual bool IsCustomAttributeArgumentValue(T node)
            {
                return HasValue(node);
            }

            #endregion

            #region IExpressionCompiler

            public AccessType Access { get { return access; } set { access = value; } }
            protected AccessType access = AccessType.None;

            public bool HasValue(Expression node) { return GetValueTypeCode((T)node) != PhpTypeCode.Unknown; }

            object IExpressionCompiler.GetValue(Expression node) { return GetValue((T)node); }

            PhpTypeCode IExpressionCompiler.GetValueTypeCode(Expression node) { return GetValueTypeCode((T)node); }

            Evaluation IExpressionCompiler.EvaluatePriorAnalysis(Expression node, CompilationSourceUnit sourceUnit)
            {
                return EvaluatePriorAnalysis((T)node, sourceUnit);
            }

            Evaluation IExpressionCompiler.Analyze(Expression node, Analyzer analyzer, ExInfoFromParent info)
            {
                return Analyze((T)node, analyzer, info);
            }

            PhpTypeCode IExpressionCompiler.Emit(Expression node, CodeGenerator codeGenerator)
            {
                return Emit((T)node, codeGenerator);
            }

            object IExpressionCompiler.Evaluate(Expression node, object value)
            {
                return Evaluate((T)node, value);
            }

            object IExpressionCompiler.Evaluate(Expression node, object leftValue, object rightValue)
            {
                return Evaluate((T)node, leftValue, rightValue);
            }

            bool IExpressionCompiler.IsDeeplyCopied(Expression node, CopyReason reason, int nestingLevel)
            {
                return IsDeeplyCopied((T)node, reason, nestingLevel);
            }

            bool IExpressionCompiler.IsCustomAttributeArgumentValue(Expression node)
            {
                return IsCustomAttributeArgumentValue((T)node);
            }

            bool IExpressionCompiler.StoreOnAssignment(Expression node)
            {
                return StoreOnAssignment((T)node);
            }

            #endregion
        }

        #endregion

        #region VarLikeConstructUseCompiler

        abstract class VarLikeConstructUseCompiler<T> : ExpressionCompiler<T> where T : VarLikeConstructUse
        {
            public override Evaluation Analyze(T/*!*/node, Analyzer/*!*/ analyzer, ExInfoFromParent info)
            {
                if (node.IsMemberOf != null)
                    node.IsMemberOf.Analyze(analyzer, new ExInfoFromParent(this, DetermineAccessType(node, info.Access)));

                return new Evaluation(node);
            }

            /// <summary>
            /// Determine the AccessType based in <c>isMemberOf</c> type and <c>AccessType</c> of parent.
            /// </summary>
            /// <param name="node">Instance.</param>
            /// <param name="parentInfoAccess"></param>
            /// <returns></returns>
            private AccessType DetermineAccessType(T/*!*/node, AccessType parentInfoAccess)
            {
                Debug.Assert(node.IsMemberOf != null);

                switch (parentInfoAccess)
                {
                    case AccessType.Write:
                        // Example: $x->f()->c = "foo";
                        // Chain is read up to function call "$x->f()", the rest is written "->c"
                        return (node.IsMemberOf is FunctionCall) ? AccessType.Read : AccessType.Write;

                    case AccessType.WriteRef:
                        // Example: $x->f()->c =& $v;
                        // Chain is read up to function call "$x->f()", the rest is written or written ref "->c"
                        return (node.IsMemberOf is FunctionCall) ? AccessType.Read : AccessType.Write;

                    case AccessType.ReadRef:
                        return (node.IsMemberOf is FunctionCall || node is FunctionCall) ? AccessType.Read : AccessType.Write;

                    case AccessType.ReadAndWriteAndReadRef:
                    case AccessType.WriteAndReadRef:
                    case AccessType.ReadAndWrite:
                        // Example: $x->f()->c = "foo";
                        // Chain is read up to function call "$x->f()", the rest is both read and written "->c"
                        return (node.IsMemberOf is FunctionCall) ? AccessType.Read : AccessType.ReadAndWrite;

                    case AccessType.WriteAndReadUnknown:
                    case AccessType.ReadAndWriteAndReadUnknown:
                        return (node.IsMemberOf is FunctionCall) ? AccessType.Read : parentInfoAccess;

                    case AccessType.ReadUnknown:
                        return (node.IsMemberOf is FunctionCall || node is FunctionCall) ? AccessType.Read : AccessType.ReadUnknown;

                    default:
                        return AccessType.Read;
                }
            }            
        }

        #endregion

        #region ConstantDeclCompiler

        abstract class ConstantDeclCompiler<T> : INodeCompiler, IConstantDeclCompiler where T : ConstantDecl
        {
            /// <summary>
            /// Whether the node has been analyzed.
            /// </summary>
            protected bool analyzed;

            public abstract KnownConstant Constant { get; }

            public virtual void Analyze(T/*!*/node, Analyzer/*!*/ analyzer)
            {
                if (!this.analyzed && Constant != null) // J: Constant can be null, if there was an error
                {
                    Evaluation eval = node.Initializer.Analyze(analyzer, ExInfoFromParent.DefaultExInfo);
                    if (eval.HasValue)
                    {
                        Constant.SetValue(eval.Value);
                    }
                    else
                    {
                        node.Initializer = eval.Expression;
                        Constant.SetNode(node);
                    }

                    this.analyzed = true;
                }
            }

            #region IConstantDeclCompiler Members

            void IConstantDeclCompiler.Analyze(ConstantDecl node, Analyzer analyzer)
            {
                Analyze((T)node, analyzer);
            }

            #endregion
        }

        #endregion
    }

    #region AccessType

    /// <summary>
    /// Access type - describes context within which an expression is used.
    /// </summary>
    public enum AccessType : byte
    {
        None,          // serves for case when Expression is body of a ExpressionStmt.
        // It is useless to push its value on the stack in that case
        Read,
        Write,         // this access can only have VariableUse of course
        ReadAndWrite,  // dtto, it serves for +=,*=, etc.
        ReadRef,       // this access can only have VarLikeConstructUse and RefAssignEx (eg. f($a=&$b); where decl. is: function f(&$x) {} )
        WriteRef,      // this access can only have VariableUse of course
        ReadUnknown,   // this access can only have VarLikeConstructUse and NewEx, 
        // when they are act. param whose related formal param is not known
        WriteAndReadRef,		/*this access can only have VariableUse, it is used in case like:
													function f(&$x) {}
													f($a=$b);
												*/
        WriteAndReadUnknown, //dtto, but it is used when the signature of called function is not known 
        /* It is because of implementation of code generation that we
            * do not use an AccessType WriteRefAndReadRef in case of ReafAssignEx
            * f(&$x){} 
            * f($a=&$b)
            */
        ReadAndWriteAndReadRef, //for f($a+=$b);
        ReadAndWriteAndReadUnknown
    }

    #endregion        

    #region ExInfoFromParent

    /// <summary>
    /// Structure used to pass inherited attributes during expression analyzis.
    /// </summary>
    internal struct ExInfoFromParent
    {
        public AccessType Access { get { return access; } set { access = value; } }
        public AccessType access;

        /// <summary>
        /// Used only by DirectVarUse to avoid assigning to $this. 
        /// Can be null reference if not needed.
        /// </summary>
        public object Parent { get { return parent; } }
        private object parent;

        public readonly static ExInfoFromParent DefaultExInfo = new ExInfoFromParent(null);

        public ExInfoFromParent(object parent)
        {
            this.parent = parent;
            this.access = AccessType.Read;
        }

        public ExInfoFromParent(object parent, AccessType access)
        {
            this.parent = parent;
            this.access = access;
        }
    }

    #endregion

    #region IExpressionCompiler

    /// <summary>
    /// Base compiler <see cref="Expression"/> extension interface .
    /// </summary>
    internal interface IExpressionCompiler : INodeCompiler
    {
        /// <summary>
        /// Gets the access type of the expression.
        /// </summary>
        AccessType Access { get; set; }

        /// <summary>
        /// Gets value indicating whether the expression has compile-time value. (literals only).
        /// </summary>
        bool HasValue(Expression/*!*/node);

        /// <summary>
        /// Gets compile-time value of the expression. (literals only).
        /// </summary>
        object GetValue(Expression/*!*/node);

        /// <summary>
        /// Gets compile-time value type code of the expression. Returns <see cref="PhpTypeCode.Unknown"/> if the value if unknown.
        /// </summary>
        PhpTypeCode GetValueTypeCode(Expression/*!*/node);

        /// <summary>
        /// Whether the expression can be used as a value of a custom attribute argument
        /// (a constant expression, CLR array, CLR type object).
        /// </summary>
        bool IsCustomAttributeArgumentValue(Expression/*!*/node);

        /// <summary>
        /// Evaluates value of given <paramref name="node"/> before actual analysis, with no additional information provided.
        /// </summary>
        /// <param name="node"><see cref="Expression"/> instance, associated with this <see cref="INodeCompiler"/>.</param>
        /// <param name="sourceUnit">Containing <see cref="SourceUnit"/>.</param>
        /// <returns><see cref="Evaluation"/> with the result of the operation.</returns>
        /// <remarks>This method allows compiler to resolve static inclusions before analysis,
        /// when application constants or literals are used within the expression.</remarks>
        Evaluation EvaluatePriorAnalysis(Expression/*!*/node, CompilationSourceUnit/*!*/sourceUnit);

        /// <summary>
        /// Performs analysis of given expression, before actual emit.
        /// </summary>
        /// <param name="node"><see cref="Expression"/> instance, associated with this <see cref="INodeCompiler"/>.</param>
        /// <param name="analyzer">Analyzer object.</param>
        /// <param name="info">Access information from parent node.</param>
        /// <returns><see cref="Evaluation"/> containing this or new expression, as a result of this operation.</returns>
        Evaluation Analyze(Expression/*!*/node, Analyzer/*!*/ analyzer, ExInfoFromParent info);

        /// <summary>
        /// Emits given <paramref name="node"/> into IL.
        /// </summary>
        /// <param name="node"><see cref="Expression"/> instance, associated with this <see cref="INodeCompiler"/>.</param>
        /// <param name="codeGenerator"><see cref="CodeGenerator"/> object.</param>
        /// <returns>Whether this operation left a value on stack, the return value contains its type code. Othervise it returns <see cref="PhpTypeCode.Void"/>.</returns>
        PhpTypeCode Emit(Expression/*!*/node, CodeGenerator/*!*/ codeGenerator);

        /// <summary>
        /// Helper function to compute an unary operation.
        /// </summary>
        /// <param name="node"><see cref="Expression"/> instance, associated with this <see cref="INodeCompiler"/>.</param>
        /// <param name="value">Operand of the operation.</param>
        /// <returns>Computed value.</returns>
        object Evaluate(Expression/*!*/node, object value);

        /// <summary>
        /// Helper function to compute a binary operation.
        /// </summary>
        /// <param name="node"><see cref="Expression"/> instance, associated with this <see cref="INodeCompiler"/></param>
        /// <param name="leftValue">Left operand.</param>
        /// <param name="rightValue">Right operand.</param>
        /// <returns>Computed value.</returns>
        object Evaluate(Expression/*!*/node, object leftValue, object rightValue);

        /// <summary>
        /// Whether compiler should emit deep variable copying, when passing expression as an assignment, a function parameter or as a function return value.
        /// </summary>
        /// <param name="node"><see cref="Expression"/> instance, associated with this <see cref="INodeCompiler"/>.</param>
        /// <param name="reason">The reason of copying.</param>
        /// <param name="nestingLevel"></param>
        /// <returns>Whether compiler should perform variable copying.</returns>
        bool IsDeeplyCopied(Expression/*!*/node, CopyReason reason, int nestingLevel);

        /// <summary>
        /// Whether an expression represented by this node should be stored to a temporary local if assigned.
        /// </summary>
        bool StoreOnAssignment(Expression/*!*/node);
    }

    #endregion

    #region IConstantDeclCompiler, ConstantDeclHelper

    interface IConstantDeclCompiler
    {
        KnownConstant Constant { get; }
        void Analyze(ConstantDecl/*!*/node, Analyzer/*!*/ analyzer);
    }

    static class ConstantDeclHelper
    {
        public static void Analyze(this ConstantDecl/*!*/node, Analyzer/*!*/ analyzer)
        {
            node.NodeCompiler<IConstantDeclCompiler>().Analyze(node, analyzer);
        }
    }

    #endregion        
}
