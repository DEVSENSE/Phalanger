/*

 Copyright (c) 2004-2006 Tomas Matousek, Vaclav Novak and Martin Maly.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System.Diagnostics;
using System.Reflection.Emit;
using PHP.Core.Emit;
using PHP.Core.Parsers;
using PHP.Core.Reflection;

namespace PHP.Core.AST
{
	/// <summary>
	/// Binary expression.
	/// </summary>
	public sealed class BinaryEx : Expression
	{
		internal Expression/*!*/ LeftExpr { get { return leftExpr; } }
		private Expression/*!*/ leftExpr;

		internal Expression/*!*/ RightExpr { get { return rightExpr; } }
		private Expression/*!*/ rightExpr;

		internal override Operations Operation { get { return operation; } }
		private Operations operation;

		#region Construction

		public BinaryEx(Position position, Operations operation, Expression/*!*/ leftExpr, Expression/*!*/ rightExpr)
			: base(position)
		{
			Debug.Assert(leftExpr != null && rightExpr != null);
			this.operation = operation;
			this.leftExpr = leftExpr;
			this.rightExpr = rightExpr;
		}

		public BinaryEx(Operations operation, Expression leftExpr, Expression rightExpr)
			: this(Position.Invalid, operation, leftExpr, rightExpr)
		{
		}

		#endregion

		#region Analysis

		internal override Evaluation EvaluatePriorAnalysis(SourceUnit/*!*/ sourceUnit)
		{
			Evaluation left_eval = leftExpr.EvaluatePriorAnalysis(sourceUnit);
			Evaluation right_eval = leftExpr.EvaluatePriorAnalysis(sourceUnit);

			return Evaluation.ReadOnlyEvaluate(this, left_eval, right_eval);
		}

		/// <include file='Doc/Nodes.xml' path='doc/method[@name="Expression.Analyze"]/*'/>
		internal override Evaluation Analyze(Analyzer/*!*/ analyzer, ExInfoFromParent info)
		{
			access = info.Access;
			ExInfoFromParent operand_info = ExInfoFromParent.DefaultExInfo;

			Evaluation left_eval = leftExpr.Analyze(analyzer, operand_info);
			Evaluation right_eval;

			// Boolean expression evaluation semantics:
			if (operation == Operations.Or)
			{
				analyzer.EnterConditionalCode();
				right_eval = rightExpr.Analyze(analyzer, operand_info);
				analyzer.LeaveConditionalCode();
			}
			else
			{
				right_eval = rightExpr.Analyze(analyzer, operand_info);
			}

			Evaluation result = Evaluation.Evaluate(this, left_eval, out leftExpr, right_eval, out rightExpr);

			// division by zero check:
			if ((operation == Operations.Div || operation == Operations.Mod) && result.HasValue && result.Value is bool && (bool)result.Value == false)
			{
				analyzer.ErrorSink.Add(Warnings.DivisionByZero, analyzer.SourceUnit, rightExpr.Position);
			}
			else if ((operation == Operations.Div || operation == Operations.Mod) && right_eval.HasValue && right_eval.Value is int && (int)right_eval.Value == 0)
			{
				result = new Evaluation(this, false);
				analyzer.ErrorSink.Add(Warnings.DivisionByZero, analyzer.SourceUnit, rightExpr.Position);
			}

			return result;
		}

		internal override object Evaluate(object leftValue, object rightValue)
		{
			switch (operation)
			{
				case Operations.Xor:
					return Convert.ObjectToBoolean(leftValue) ^ Convert.ObjectToBoolean(rightValue);

				case Operations.Or:
					return Convert.ObjectToBoolean(leftValue) || Convert.ObjectToBoolean(rightValue);

				case Operations.And:
					return Convert.ObjectToBoolean(leftValue) && Convert.ObjectToBoolean(rightValue);

				case Operations.BitOr:
					return Operators.BitOperation(leftValue, rightValue, Operators.BitOp.Or);

				case Operations.BitXor:
					return Operators.BitOperation(leftValue, rightValue, Operators.BitOp.Xor);

				case Operations.BitAnd:
					return Operators.BitOperation(leftValue, rightValue, Operators.BitOp.And);

				case Operations.Equal:
					return PhpComparer./*Default.*/CompareEq(leftValue, rightValue);

				case Operations.NotEqual:
					return !PhpComparer./*Default.*/CompareEq(leftValue, rightValue);

				case Operations.Identical:
					return Operators.StrictEquality(leftValue, rightValue);

				case Operations.NotIdentical:
					return !Operators.StrictEquality(leftValue, rightValue);

				case Operations.LessThan:
					return PhpComparer.Default.Compare(leftValue, rightValue) < 0;

				case Operations.GreaterThan:
					return PhpComparer.Default.Compare(leftValue, rightValue) > 0;

				case Operations.LessThanOrEqual:
					return PhpComparer.Default.Compare(leftValue, rightValue) <= 0;

				case Operations.GreaterThanOrEqual:
					return PhpComparer.Default.Compare(leftValue, rightValue) >= 0;

				case Operations.ShiftRight:
					return Operators.ShiftRight(leftValue, rightValue); // int

				case Operations.ShiftLeft:
					return Operators.ShiftLeft(leftValue, rightValue);  // int

				case Operations.Add:
					return Operators.Add(leftValue, rightValue);

				case Operations.Sub:
					return Operators.Subtract(leftValue, rightValue);

				case Operations.Mul:
					return Operators.Multiply(leftValue, rightValue);

				case Operations.Div:
					return Operators.Divide(leftValue, rightValue);

				case Operations.Mod:
					return Operators.Remainder(leftValue, rightValue);

				case Operations.Concat:
					return Operators.Concat(leftValue, rightValue);

				default:
					throw null;
			}
		}

		#endregion

		#region Code emission

		/// <summary>
		/// Whether the result of binary expression should be deeply copied.
		/// </summary>
		/// <include file='Doc/Nodes.xml' path='doc/method[@name="IsDeeplyCopied"]/*'/>
		/// <returns>All operators returns immutable values. Hence, returns <B>false</B>.</returns>
		internal override bool IsDeeplyCopied(CopyReason reason, int nestingLevel)
		{
            switch (operation)
            {
                // respective operators returns immutable values:
                case Operations.Xor:
                case Operations.Or:
                case Operations.And:
                case Operations.BitOr:
                case Operations.BitXor:
                case Operations.BitAnd:
                case Operations.Equal:
                case Operations.NotEqual:
                case Operations.Identical:
                case Operations.NotIdentical:
                case Operations.LessThan:
                case Operations.GreaterThan:
                case Operations.LessThanOrEqual:
                case Operations.GreaterThanOrEqual:
                case Operations.ShiftLeft:
                case Operations.ShiftRight:
                case Operations.Add:
                case Operations.Sub:
                case Operations.Mul:
                case Operations.Div:
                case Operations.Mod:
                case Operations.Concat:
                    return false;
            }
			Debug.Fail("Illegal operation type.");
			return true;
		}

		/// <remarks>
		/// Nothing is expected at the evaluation stack. If AST node is read by other node,
		/// the operation result is left at the stack, otherwise it is poped from the stack.
		/// </remarks>
		/// <include file='Doc/Nodes.xml' path='doc/method[@name="Emit"]/*'/>
		internal override PhpTypeCode Emit(CodeGenerator codeGenerator)
		{
			Debug.Assert(access == AccessType.None || access == AccessType.Read);
			Statistics.AST.AddNode("BinaryEx");

			PhpTypeCode returned_typecode;
			PhpTypeCode lo_typecode;
			PhpTypeCode ro_typecode;

			switch (operation)
			{
				#region Arithmetic Operations

				case Operations.Add:
					// Template: x + y : Operators.Add(x,y) [overloads]

                    switch (lo_typecode = leftExpr.Emit(codeGenerator))
                    {
                        case PhpTypeCode.Double:
                            switch (ro_typecode = rightExpr.Emit(codeGenerator))
                            {
                                case PhpTypeCode.Integer:
                                    codeGenerator.IL.Emit(OpCodes.Conv_R8);
                                    goto case PhpTypeCode.Double;   // fallback:
                                case PhpTypeCode.Double:
                                    codeGenerator.IL.Emit(OpCodes.Add);
                                    returned_typecode = PhpTypeCode.Double;
                                    break;
                                default:
                                    codeGenerator.EmitBoxing(ro_typecode);
                                    returned_typecode = codeGenerator.EmitMethodCall(Methods.Operators.Add.Double_Object);
                                    break;
                            }
                            
                            break;
                        default:
                            codeGenerator.EmitBoxing(lo_typecode);
                            ro_typecode = rightExpr.Emit(codeGenerator);

                            switch (ro_typecode)
                            {
                                case PhpTypeCode.Integer:
                                    returned_typecode = codeGenerator.EmitMethodCall(Methods.Operators.Add.Object_Int32);
                                    break;

                                case PhpTypeCode.Double:
                                    returned_typecode = codeGenerator.EmitMethodCall(Methods.Operators.Add.Object_Double);
                                    break;

                                default:
                                    codeGenerator.EmitBoxing(ro_typecode);
                                    returned_typecode = codeGenerator.EmitMethodCall(Methods.Operators.Add.Object_Object);
                                    break;
                            }
                            break;
                    }
                    break;

				case Operations.Sub:
					//Template: "x - y"        Operators.Subtract(x,y) [overloads]
					lo_typecode = leftExpr.Emit(codeGenerator);
                    switch (lo_typecode)
                    {
                        case PhpTypeCode.Integer:
                            codeGenerator.EmitBoxing(rightExpr.Emit(codeGenerator));
                            returned_typecode = codeGenerator.EmitMethodCall(Methods.Operators.Subtract.Int32_Object);
                            break;
                        case PhpTypeCode.Double:
                            switch (ro_typecode = rightExpr.Emit(codeGenerator))
                            {
                                case PhpTypeCode.Integer:
                                    codeGenerator.IL.Emit(OpCodes.Conv_R8);
                                    goto case PhpTypeCode.Double;   // fallback:
                                case PhpTypeCode.Double:
                                    codeGenerator.IL.Emit(OpCodes.Sub);
                                    returned_typecode = PhpTypeCode.Double;
                                    break;
                                default:
                                    codeGenerator.EmitBoxing(ro_typecode);
                                    returned_typecode = codeGenerator.EmitMethodCall(Methods.Operators.Subtract.Double_Object);
                                    break;
                            }
                            
                            break;
                        default:
                            codeGenerator.EmitBoxing(lo_typecode);
                            ro_typecode = rightExpr.Emit(codeGenerator);
                            if (ro_typecode == PhpTypeCode.Integer)
                            {
                                returned_typecode = codeGenerator.EmitMethodCall(Methods.Operators.Subtract.Object_Int);
                            }
                            else
                            {
                                codeGenerator.EmitBoxing(ro_typecode);
                                returned_typecode = codeGenerator.EmitMethodCall(Methods.Operators.Subtract.Object_Object);
                            }
                            break;
                    }
					break;

				case Operations.Div:
					//Template: "x / y"   Operators.Divide(x,y)

					lo_typecode = leftExpr.Emit(codeGenerator);
					switch (lo_typecode)
					{
						case PhpTypeCode.Integer:
							codeGenerator.EmitBoxing(rightExpr.Emit(codeGenerator));
							returned_typecode = codeGenerator.EmitMethodCall(Methods.Operators.Divide.Int32_Object);
							break;

						case PhpTypeCode.Double:
                            switch (ro_typecode = rightExpr.Emit(codeGenerator))
                            {
                                case PhpTypeCode.Double:
                                    codeGenerator.IL.Emit(OpCodes.Div);
                                    returned_typecode = PhpTypeCode.Double;
                                    break;
                                default:
                                    codeGenerator.EmitBoxing(ro_typecode);
                                    returned_typecode = codeGenerator.EmitMethodCall(Methods.Operators.Divide.Double_Object);
                                    break;
                            }
                            break;

						default:
							codeGenerator.EmitBoxing(lo_typecode);
							ro_typecode = rightExpr.Emit(codeGenerator);

							switch (ro_typecode)
							{
								case PhpTypeCode.Integer:
									returned_typecode = codeGenerator.EmitMethodCall(Methods.Operators.Divide.Object_Int32);
									break;

								case PhpTypeCode.Double:
									returned_typecode = codeGenerator.EmitMethodCall(Methods.Operators.Divide.Object_Double);
									break;

								default:
									codeGenerator.EmitBoxing(ro_typecode);
									returned_typecode = codeGenerator.EmitMethodCall(Methods.Operators.Divide.Object_Object);
									break;
							}
							break;
					}
					break;

				case Operations.Mul:
					switch (lo_typecode = leftExpr.Emit(codeGenerator))
                    {
                        case PhpTypeCode.Double:
                            // "x * (double)y"
                            // Operators.Multiply((double)x,(object)y)

                            switch (ro_typecode = rightExpr.Emit(codeGenerator))
                            {
                                case PhpTypeCode.Integer:
                                    codeGenerator.IL.Emit(OpCodes.Conv_R8);
                                    goto case PhpTypeCode.Double;   // fallback:
                                case PhpTypeCode.Double:
                                    codeGenerator.IL.Emit(OpCodes.Mul);
                                    returned_typecode = PhpTypeCode.Double;
                                    break;
                                default:
                                    codeGenerator.EmitBoxing(ro_typecode);
                                    returned_typecode = codeGenerator.EmitMethodCall(Methods.Operators.Multiply.Double_Object);
							        break;
                            }
                    
                            break;
                        default:
                            //Template: "x * y"  Operators.Multiply((object)x,y) [overloads]
                            codeGenerator.EmitBoxing(lo_typecode);

                            ro_typecode = rightExpr.Emit(codeGenerator);
					        switch (ro_typecode)
					        {
						        case PhpTypeCode.Integer:
							        returned_typecode = codeGenerator.EmitMethodCall(Methods.Operators.Multiply.Object_Int32);
							        break;

						        case PhpTypeCode.Double:
							        returned_typecode = codeGenerator.EmitMethodCall(Methods.Operators.Multiply.Object_Double);
							        break;

						        default:
							        codeGenerator.EmitBoxing(ro_typecode);
							        returned_typecode = codeGenerator.EmitMethodCall(Methods.Operators.Multiply.Object_Object);
							        break;
					        }
                            break;
                    }					
					break;

				case Operations.Mod:
					//Template: "x % y"        Operators.Remainder(x,y)
					codeGenerator.EmitBoxing(leftExpr.Emit(codeGenerator));
					ro_typecode = rightExpr.Emit(codeGenerator);
					switch (ro_typecode)
					{
						case PhpTypeCode.Integer:
							returned_typecode = codeGenerator.EmitMethodCall(Methods.Operators.Remainder.Object_Int32);
							break;

						default:
							codeGenerator.EmitBoxing(ro_typecode);
							returned_typecode = codeGenerator.EmitMethodCall(Methods.Operators.Remainder.Object_Object);
							break;
					}
					break;

				case Operations.ShiftLeft:

					// LOAD Operators.ShiftLeft(box left, box right);
					codeGenerator.EmitBoxing(leftExpr.Emit(codeGenerator));
					codeGenerator.EmitBoxing(rightExpr.Emit(codeGenerator));
					returned_typecode = codeGenerator.EmitMethodCall(Methods.Operators.ShiftLeft);
					break;

				case Operations.ShiftRight:

					// LOAD Operators.ShiftRight(box left, box right);
					codeGenerator.EmitBoxing(leftExpr.Emit(codeGenerator));
					codeGenerator.EmitBoxing(rightExpr.Emit(codeGenerator));
					returned_typecode = codeGenerator.EmitMethodCall(Methods.Operators.ShiftRight);
					break;

				#endregion

				#region Boolean and Bitwise Operations

				case Operations.And:
					returned_typecode = EmitBinaryBooleanOperation(codeGenerator, true);
					break;

				case Operations.Or:
					returned_typecode = EmitBinaryBooleanOperation(codeGenerator, false);
					break;

				case Operations.Xor:

					// LOAD <(bool) leftSon> == <(bool) rightSon>;
					codeGenerator.EmitConversion(leftExpr, PhpTypeCode.Boolean);
					codeGenerator.EmitConversion(rightExpr, PhpTypeCode.Boolean);
                    codeGenerator.IL.Emit(OpCodes.Ceq);

                    codeGenerator.IL.Emit(OpCodes.Ldc_I4_0);
                    codeGenerator.IL.Emit(OpCodes.Ceq);

					returned_typecode = PhpTypeCode.Boolean;
					break;

				case Operations.BitAnd:
					returned_typecode = EmitBitOperation(codeGenerator, Operators.BitOp.And);
					break;

				case Operations.BitOr:
					returned_typecode = EmitBitOperation(codeGenerator, Operators.BitOp.Or);
					break;

				case Operations.BitXor:
					returned_typecode = EmitBitOperation(codeGenerator, Operators.BitOp.Xor);
					break;

				#endregion

				#region Comparing Operations

				case Operations.Equal:

					// LOAD PhpComparer.Default.CompareEq
					returned_typecode = EmitComparison(codeGenerator, true);
					break;

				case Operations.NotEqual:

					// LOAD PhpComparer.Default.CompareEq == false
					EmitComparison(codeGenerator, true);
                    codeGenerator.IL.Emit(OpCodes.Ldc_I4_0);
                    codeGenerator.IL.Emit(OpCodes.Ceq);

					returned_typecode = PhpTypeCode.Boolean;
					break;

				case Operations.GreaterThan:

					// LOAD PhpComparer.Default.Compare > 0;
					EmitComparison(codeGenerator, false);
                    codeGenerator.IL.Emit(OpCodes.Ldc_I4_0);
                    codeGenerator.IL.Emit(OpCodes.Cgt);

					returned_typecode = PhpTypeCode.Boolean;
					break;

				case Operations.LessThan:

					// LOAD PhpComparer.Default.Compare < 0;
					EmitComparison(codeGenerator, false);
                    codeGenerator.IL.Emit(OpCodes.Ldc_I4_0);
                    codeGenerator.IL.Emit(OpCodes.Clt);

					returned_typecode = PhpTypeCode.Boolean;
					break;

				case Operations.GreaterThanOrEqual:

					// LOAD PhpComparer.Default.Compare >= 0 (not less than)
					EmitComparison(codeGenerator, false);
                    codeGenerator.IL.Emit(OpCodes.Ldc_I4_0);
                    codeGenerator.IL.Emit(OpCodes.Clt);
                    codeGenerator.IL.Emit(OpCodes.Ldc_I4_0);
                    codeGenerator.IL.Emit(OpCodes.Ceq);

					returned_typecode = PhpTypeCode.Boolean;
					break;

				case Operations.LessThanOrEqual:

					// LOAD PhpComparer.Default.Compare >= 0 (not greater than)
					EmitComparison(codeGenerator, false);
                    codeGenerator.IL.Emit(OpCodes.Ldc_I4_0);
                    codeGenerator.IL.Emit(OpCodes.Cgt);
                    codeGenerator.IL.Emit(OpCodes.Ldc_I4_0);
                    codeGenerator.IL.Emit(OpCodes.Ceq);

					returned_typecode = PhpTypeCode.Boolean;
					break;

				case Operations.Identical:

					// LOAD Operators.StrictEquality(box left,box right);
					codeGenerator.EmitBoxing(leftExpr.Emit(codeGenerator));
					codeGenerator.EmitBoxing(rightExpr.Emit(codeGenerator));
                    codeGenerator.IL.Emit(OpCodes.Call, Methods.Operators.StrictEquality);

					returned_typecode = PhpTypeCode.Boolean;
					break;

				case Operations.NotIdentical:

					// LOAD Operators.StrictEquality(box left,box right) == false;
					codeGenerator.EmitBoxing(leftExpr.Emit(codeGenerator));
					codeGenerator.EmitBoxing(rightExpr.Emit(codeGenerator));
                    codeGenerator.IL.Emit(OpCodes.Call, Methods.Operators.StrictEquality);
                    codeGenerator.IL.Emit(OpCodes.Ldc_I4_0);
                    codeGenerator.IL.Emit(OpCodes.Ceq);

					returned_typecode = PhpTypeCode.Boolean;
					break;

				#endregion

				case Operations.Concat:
					returned_typecode = ConcatEx.EmitConcat(codeGenerator, leftExpr, rightExpr);
					break;

				default:
					throw null;
			}

			switch (access)
			{
				case AccessType.Read:
					// Result is read, do nothing.
					break;

				case AccessType.None:
					// Result is not read, pop the result
                    codeGenerator.IL.Emit(OpCodes.Pop);
					returned_typecode = PhpTypeCode.Void;
					break;
			}

			return returned_typecode;
		}

		/// <summary>
		/// Emits bit operation <see cref="leftExpr"/> OP <see cref="rightExpr"/>.
		/// </summary>
		/// <param name="codeGenerator">A code generator.</param>
		/// <param name="op">The operation.</param>
		/// <returns>A type code of the result.</returns>
		private PhpTypeCode EmitBitOperation(CodeGenerator/*!*/ codeGenerator, Operators.BitOp op)
		{
			ILEmitter il = codeGenerator.IL;

			// LOAD Operators.BitOperation(box <leftSon>, box <rightSon>);
			codeGenerator.EmitBoxing(leftExpr.Emit(codeGenerator));
			codeGenerator.EmitBoxing(rightExpr.Emit(codeGenerator));
			il.Emit(OpCodes.Ldc_I4, (int)op);
			il.Emit(OpCodes.Call, Methods.Operators.BitOperation);

			return PhpTypeCode.Object;
		}

		/// <summary>
		/// Emits binary boolean operation (AND or OR).
		/// </summary>
		/// <param name="codeGenerator">A code generator.</param>
		/// <param name="isAnd">Whether to emit AND.</param>
		/// <returns>A type code of the result.</returns>
		private PhpTypeCode EmitBinaryBooleanOperation(CodeGenerator codeGenerator, bool isAnd)
		{
			ILEmitter il = codeGenerator.IL;
			Label partial_eval_label = il.DefineLabel();
			Label end_label = il.DefineLabel();

			// IF [!]<(bool) leftSon> THEN GOTO partial_eval;
			codeGenerator.EmitConversion(leftExpr, PhpTypeCode.Boolean);
			il.Emit(isAnd ? OpCodes.Brfalse : OpCodes.Brtrue, partial_eval_label);

			// LOAD <(bool) leftSon>;
			codeGenerator.EmitConversion(rightExpr, PhpTypeCode.Boolean);

			il.Emit(OpCodes.Br, end_label);
			il.MarkLabel(partial_eval_label, true);
			il.LdcI4(isAnd ? 0 : 1);
			il.MarkLabel(end_label, true);

			return PhpTypeCode.Boolean;
		}

		/// <summary>
		/// Emits call to a default comparator method.
		/// </summary>
		/// <param name="codeGenerator">A code generator.</param>
		/// <param name="equality">Whether to emit equality comparison (or generic comparison otherwise).</param>
		/// <returns>A type code of the result.</returns>
		private PhpTypeCode EmitComparison(CodeGenerator codeGenerator, bool equality)
		{
            PhpTypeCode x, y;
			// PhpComparer.Default.<CompareEq | Compare>(box left, box right <|, false>);
			/*changed to static method*/ //codeGenerator.IL.Emit(OpCodes.Ldsfld, Fields.PhpComparer_Default);
			
			if (equality)
			{
                return codeGenerator.EmitCompareEq(cg => this.leftExpr.Emit(cg), cg => this.rightExpr.Emit(cg));
			}
			else
			{
                x = leftExpr.Emit(codeGenerator);

                if (x == PhpTypeCode.Integer)
                {
                    y = rightExpr.Emit(codeGenerator);

                    // int, ?

                    if (y == PhpTypeCode.Integer)
                    {
                        // int, int
                        codeGenerator.IL.Emit(OpCodes.Call, Methods.CompareOp_int_int);
                        return PhpTypeCode.Integer;
                    }
                    else
                    {
                        codeGenerator.EmitBoxing(y);

                        // int, object
                        codeGenerator.IL.LdcI4(0);  // throws = false
                        codeGenerator.IL.Emit(OpCodes.Call, Methods.CompareOp_int_object_bool);
                        return PhpTypeCode.Integer;
                    }

                }
                else
                {
                    codeGenerator.EmitBoxing(x);

                    y = rightExpr.Emit(codeGenerator);

                    // object, ?

                    if (y == PhpTypeCode.Integer)
                    {
                        // object, int
                        codeGenerator.IL.LdcI4(0);  // throws = false
                        codeGenerator.IL.Emit(OpCodes.Call, Methods.CompareOp_object_int_bool);
                        return PhpTypeCode.Integer;
                    }
                    else
                    {
                        codeGenerator.EmitBoxing(y);

                        // object, object
                        codeGenerator.IL.LdcI4(0);  // throws = false
                        codeGenerator.IL.Emit(OpCodes.Call, Methods.CompareOp_object_object_bool);
                        return PhpTypeCode.Integer;
                    }
                }
			}
		}

		#endregion

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitBinaryEx(this);
        }
	}
}