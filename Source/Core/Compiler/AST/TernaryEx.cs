/*

 Copyright (c) 2004-2006 Tomas Matousek, Vaclav Novak and Martin Maly.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Reflection.Emit;
using System.Diagnostics;
using PHP.Core.Parsers;
using PHP.Core.Emit;

namespace PHP.Core.AST
{
	/// <summary>
	/// Conditional expression.
	/// </summary>
	public sealed class ConditionalEx : Expression
	{
		internal override Operations Operation { get { return Operations.Conditional; } }

		private Expression/*!*/ condExpr;
		private Expression trueExpr;
		private Expression/*!*/ falseExpr;
        /// <summary>Contition</summary>
        public Expression/*!*/ CondExpr { get { return condExpr; } }
        /// <summary>Expression evaluated when <see cref="CondExpr"/> is true</summary>
        public Expression/*!*/ TrueExpr { get { return trueExpr ?? condExpr; } }
        /// <summary><summary>Expression evaluated when <see cref="CondExpr"/> is false</summary></summary>
        public Expression/*!*/ FalseExpr { get { return falseExpr; } }

		public ConditionalEx(Position position, Expression/*!*/ condExpr, Expression trueExpr, Expression/*!*/ falseExpr)
			: base(position)
		{
            Debug.Assert(condExpr != null);
            // Debug.Assert(trueExpr != null); // allowed to enable ternary shortcut
            Debug.Assert(falseExpr != null);

			this.condExpr = condExpr;
			this.trueExpr = trueExpr;
			this.falseExpr = falseExpr;
		}

		public ConditionalEx(Expression/*!*/ condExpr, Expression/*!*/ trueExpr, Expression/*!*/ falseExpr)
			: this(Position.Invalid, condExpr, trueExpr, falseExpr)
		{
		}

		#region Analysis

		internal override Evaluation Analyze(Analyzer/*!*/ analyzer, ExInfoFromParent info)
		{
			access = info.Access;

			Evaluation cond_eval = condExpr.Analyze(analyzer, ExInfoFromParent.DefaultExInfo);

			if (cond_eval.HasValue)
			{
                if (Convert.ObjectToBoolean(cond_eval.Value))
                {
                    if (trueExpr != null)
                        return trueExpr.Analyze(analyzer, ExInfoFromParent.DefaultExInfo);
                    else
                        return cond_eval;   // condExpr ?: falseExpr    // ternary shortcut
                }
                else
                    return falseExpr.Analyze(analyzer, ExInfoFromParent.DefaultExInfo);
			}
			else
			{
                if (trueExpr != null)
                {
                    analyzer.EnterConditionalCode();
                    trueExpr = trueExpr.Analyze(analyzer, ExInfoFromParent.DefaultExInfo).Literalize();
                    analyzer.LeaveConditionalCode();
                }

				analyzer.EnterConditionalCode();
				falseExpr = falseExpr.Analyze(analyzer, ExInfoFromParent.DefaultExInfo).Literalize();
				analyzer.LeaveConditionalCode();

				return new Evaluation(this);
			}
		}

		// obsolete:
		//internal override bool TryEvaluate(out object value)
		//{
		//  object o;
		//  if (condExpr.TryEvaluate(out o))
		//  {
		//    return (Convert.ObjectToBoolean(o)) ? trueExpr.TryEvaluate(out value) : falseExpr.TryEvaluate(out value);
		//  }
		//  else
		//  {
		//    value = null;
		//    return false;
		//  }

		//}

		#endregion

		internal override bool IsDeeplyCopied(CopyReason reason, int nestingLevel)
		{
			return (trueExpr ?? condExpr).IsDeeplyCopied(reason, nestingLevel) || falseExpr.IsDeeplyCopied(reason, nestingLevel);
		}

		/// <include file='Doc/Nodes.xml' path='doc/method[@name="Emit"]/*'/>
		internal override PhpTypeCode Emit(CodeGenerator codeGenerator)
		{
			Statistics.AST.AddNode("TernaryEx");
			Debug.Assert(access == AccessType.Read || access == AccessType.None);

			Label end_label = codeGenerator.IL.DefineLabel();
			
            if (trueExpr != null)   // standard ternary operator
            {
                Label else_label = codeGenerator.IL.DefineLabel();
                
                // IF (<(bool) condition>) THEN
                codeGenerator.EmitConversion(condExpr, PhpTypeCode.Boolean);
                codeGenerator.IL.Emit(OpCodes.Brfalse, else_label);
                {
                    codeGenerator.EmitBoxing(trueExpr.Emit(codeGenerator));
                    codeGenerator.IL.Emit(OpCodes.Br, end_label);
                }
                // ELSE
                codeGenerator.IL.MarkLabel(else_label, true);
                {
                    codeGenerator.EmitBoxing(falseExpr.Emit(codeGenerator));
                }
            }
            else
            {   // ternary shortcut:
                var il = codeGenerator.IL;

                // condExpr ?? rightExpr

                il.EmitBoxing(condExpr.Emit(codeGenerator));
                
                // IF (<stack>):
                il.Emit(OpCodes.Dup);
                il.Emit(OpCodes.Call, Methods.Convert.ObjectToBoolean);

                codeGenerator.IL.Emit(OpCodes.Brtrue, end_label);
                // ELSE:
                {
                    il.Emit(OpCodes.Pop);
                    il.EmitBoxing(falseExpr.Emit(codeGenerator));                    
                }
            }

			// END IF;
			codeGenerator.IL.MarkLabel(end_label, true);


			if (access == AccessType.None)
			{
				codeGenerator.IL.Emit(OpCodes.Pop);
				return PhpTypeCode.Void;
			}

			return PhpTypeCode.Object;
		}

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitConditionalEx(this);
        }
	}
}

