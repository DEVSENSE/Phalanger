using System;
using System.Text;
using System.Collections.Generic;
using ICSharpCode.NRefactory.Parser.AST;

namespace ClassDynamizer
{
	static class Utility
	{
		public static string OptionalAttrType = "System.Runtime.InteropServices.OptionalAttribute";

		/// <summary>
		/// Determines whether an attribute given by its full type name is contained in a attribute collections.
		/// </summary>
		public static bool IsDecoratedByAttribute(AttributedNode node, string attrTypeName)
		{
			return IsDecoratedByAttribute(node.Attributes, attrTypeName);
		}

		/// <summary>
		/// Determines whether an attribute given by its full type name is contained in a attribute collections.
		/// </summary>
		public static bool IsDecoratedByAttribute(List<AttributeSection> attributes, string attrTypeName)
		{
			string short_name = attrTypeName;

			int index = short_name.LastIndexOf('.');
			if (index > 0) short_name = short_name.Substring(index + 1);

			string shortest_name = short_name;
			string noattr_name = attrTypeName;

			if (shortest_name.EndsWith("Attribute"))
			{
				shortest_name = shortest_name.Substring(0, shortest_name.Length - 9);
				noattr_name = noattr_name.Substring(0, noattr_name.Length - 9);
			}

			foreach (AttributeSection section in attributes)
			{
				foreach (ICSharpCode.NRefactory.Parser.AST.Attribute attr in section.Attributes)
				{
					if (attr.Name == attrTypeName ||
						attr.Name == noattr_name ||
						attr.Name == short_name ||
						attr.Name == shortest_name) return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Determines whether a type given by its full name corresponds to a type reference.
		/// </summary>
		public static bool IsType(TypeReference type, string typeName)
		{
			if (type.SystemType == typeName) return true;

			int index = typeName.LastIndexOf('.');
			return (index > 0 && type.SystemType == typeName.Substring(index + 1));
		}

		/// <summary>
		/// Makes the given node [EditorBrowsable(Never)].
		/// </summary>
		public static void MakeNonBrowsable(AttributedNode node)
		{
			AttributeSection section = new AttributeSection();
			ICSharpCode.NRefactory.Parser.AST.Attribute attribute =
				new ICSharpCode.NRefactory.Parser.AST.Attribute("System.ComponentModel.EditorBrowsable",
				new List<Expression>(), null);

			attribute.PositionalArguments.Add(new FieldReferenceExpression(new TypeReferenceExpression(
				"System.ComponentModel.EditorBrowsableState"), "Never"));

			section.Attributes.Add(attribute);
			node.Attributes.Add(section);
		}

		/// <summary>
		/// Transforms a member modifier into an expression evaluating to the corresponding
		/// Phalanger PhpMemberAttributes.
		/// </summary>
		public static Expression ModifierToMemberAttributes(Modifier modifier)
		{
			Expression result = NullExpression.Instance;

			if ((modifier & Modifier.Public) == Modifier.Public) OrFlag(ref result, "PhpMemberAttributes", "Public");
			if ((modifier & Modifier.Protected) == Modifier.Protected) OrFlag(ref result, "PhpMemberAttributes", "Protected");
			if ((modifier & Modifier.Private) == Modifier.Private) OrFlag(ref result, "PhpMemberAttributes", "Private");

			if ((modifier & Modifier.Static) == Modifier.Static) OrFlag(ref result, "PhpMemberAttributes", "Static");
			if ((modifier & Modifier.Abstract) == Modifier.Abstract) OrFlag(ref result, "PhpMemberAttributes", "Abstract");
			if ((modifier & Modifier.Sealed) == Modifier.Sealed) OrFlag(ref result, "PhpMemberAttributes", "Final");

			if (result.IsNull) OrFlag(ref result, "PhpMemberAttributes", "None");

			return result;
		}

		/// <summary>
		/// Ors flags.
		/// </summary>
		public static void OrFlag(ref Expression expr, string enumTypeName, string enumFieldName)
		{
			Expression field = new FieldReferenceExpression(new TypeReferenceExpression(enumTypeName), enumFieldName);

			if (expr.IsNull)
			{
				expr = field;
			}
			else
			{
				expr = new BinaryOperatorExpression(expr, BinaryOperatorType.BitwiseOr, field);
			}
		}

		/// <summary>
		/// Adds an import (using) if it is not specified in another compile unit.
		/// </summary>
		public static void AddImport(CompilationUnit unit, CompilationUnit outUnit, string ns)
		{
			if (unit != null)
			{
				foreach (INode node in unit.Children)
				{
					UsingDeclaration decl = node as UsingDeclaration;
					if (decl != null)
					{
						foreach (Using us in decl.Usings)
						{
							if (us.Name == ns) return;
						}
					}
				}
			}

			outUnit.AddChild(new UsingDeclaration(ns));
		}
	}
}
