using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;

using ICSharpCode.NRefactory.Parser.AST;

namespace ClassDynamizer
{
	static class Convertor
	{
		/// <summary>
		/// Generates code that converts an object to the target type.
		/// </summary>
		public static Expression ConvertTo(string name, Expression value, TypeReference targetType,
			BlockStatement blockStatement, Statement failStmt, bool allowNull, bool isOptional, int seqNum)
		{
			if (Utility.IsType(targetType, "System.Object"))
			{
				// no conversion needed
				return value;
			}

			string temp_local_name = String.Format("tmp{0}", seqNum);

			// create the "conversion failed" block
			ArrayList parameters = new ArrayList();
			parameters.Add(value);
			parameters.Add(new PrimitiveExpression(targetType.Type, targetType.Type));
			parameters.Add(new PrimitiveExpression(name, name));

			// the statements to execute when the cast failed
			BlockStatement fail_block = new BlockStatement();
			fail_block.AddChild(new StatementExpression(new InvocationExpression(new FieldReferenceExpression(
				new TypeReferenceExpression("PhpException"), "InvalidImplicitCast"),
				parameters)));
			fail_block.AddChild(failStmt);

			// try to determine whether the target type is a reference or value type
			Type system_type = Type.GetType(targetType.SystemType);
			if (system_type != null && system_type.IsValueType)
			{
				// value type
				LocalVariableDeclaration temp_local;

				if (isOptional)
				{
					temp_local = new LocalVariableDeclaration(targetType);
					temp_local.Variables.Add(new VariableDeclaration(temp_local_name));

					blockStatement.AddChild(temp_local);

					BlockStatement new_block_stmt = new BlockStatement();

					IfElseStatement opt_stmt = new IfElseStatement(new BinaryOperatorExpression(
						value, BinaryOperatorType.InEquality, new FieldReferenceExpression(
						new IdentifierExpression("Arg"), "Default")), new_block_stmt,
						new StatementExpression(new AssignmentExpression(new IdentifierExpression(temp_local_name),
						AssignmentOperatorType.Assign, new ObjectCreateExpression(targetType, new ArrayList()))));

					blockStatement.AddChild(opt_stmt);
					blockStatement = new_block_stmt;
				}

				IfElseStatement if_stmt = new IfElseStatement(new UnaryOperatorExpression(
					new ParenthesizedExpression(new BinaryOperatorExpression(value, BinaryOperatorType.TypeCheck,
					new TypeReferenceExpression(targetType))), UnaryOperatorType.Not), fail_block);

				blockStatement.AddChild(if_stmt);
				if (isOptional)
				{
					blockStatement.AddChild(new StatementExpression(new AssignmentExpression(
						new IdentifierExpression(temp_local_name), AssignmentOperatorType.Assign,
						new CastExpression(targetType, value))));

					return new IdentifierExpression(temp_local_name);
				}
				else return new CastExpression(targetType, value);
			}
			else
			{
				// probably a reference type
				LocalVariableDeclaration temp_local = new LocalVariableDeclaration(targetType);
				blockStatement.AddChild(temp_local);

				if (isOptional)
				{
					// first check for Arg.Default
					temp_local.Variables.Add(
						new VariableDeclaration(temp_local_name, new PrimitiveExpression(null, String.Empty)));

					BlockStatement new_block_stmt = new BlockStatement();

					IfElseStatement opt_stmt = new IfElseStatement(new BinaryOperatorExpression(
						value, BinaryOperatorType.InEquality, new FieldReferenceExpression(
						new IdentifierExpression("Arg"), "Default")), new_block_stmt);

					blockStatement.AddChild(opt_stmt);
					blockStatement = new_block_stmt;

					// then perform the as-cast
					blockStatement.AddChild(new StatementExpression(new AssignmentExpression(
						new IdentifierExpression(temp_local_name), AssignmentOperatorType.Assign,
						CreateAsCastExpression(value, targetType))));
				}
				else
				{
					// perform the as-cast
					temp_local.Variables.Add(
						new VariableDeclaration(temp_local_name, CreateAsCastExpression(value, targetType)));
				}

				IfElseStatement if_stmt = new IfElseStatement(new BinaryOperatorExpression(
					new IdentifierExpression(temp_local_name), BinaryOperatorType.Equality,
					new PrimitiveExpression(null, String.Empty)), fail_block);

				if (allowNull)
				{
					// throw only if the value is not of targetType and not null
					if_stmt.Condition = new BinaryOperatorExpression(if_stmt.Condition,
						BinaryOperatorType.LogicalAnd, new BinaryOperatorExpression(
						value, BinaryOperatorType.InEquality, new PrimitiveExpression(null, String.Empty)));
				}

				blockStatement.AddChild(if_stmt);
				
				return new IdentifierExpression(temp_local_name);
			}
		}

		private static Expression CreateAsCastExpression(Expression value, TypeReference targetType)
		{
			if (Utility.IsType(targetType, "System.String"))
			{
				// create special PhpVariable.AsString invocation expression
				return new InvocationExpression(new FieldReferenceExpression(new IdentifierExpression("PhpVariable"),
					"AsString"), new ArrayList(new object[] { value }));
			}
			else
			{
				return new BinaryOperatorExpression(
					value, BinaryOperatorType.AsCast, new TypeReferenceExpression(targetType));
			}
		}
	}
}
