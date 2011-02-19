using System;
using System.Text;
using ICSharpCode.NRefactory.Parser.AST;
using System.Collections;
using System.Collections.Generic;

namespace ClassDynamizer
{
	class Dynamizer
	{
		/// <summary>
		/// Dynamizes a given compile unit.
		/// </summary>
		public CompilationUnit Dynamize(CompilationUnit unit)
		{
			CompilationUnit out_unit = new CompilationUnit();

			// add Phalanger imports
			Utility.AddImport(unit, out_unit, "PHP.Core");
			Utility.AddImport(unit, out_unit, "PHP.Core.Reflection");

			foreach (INode node in unit.Children)
			{
				// add all original imports
				if (node is UsingDeclaration) out_unit.Children.Add(node);

				// process namespaces
				NamespaceDeclaration ns_decl = node as NamespaceDeclaration;
				if (ns_decl != null)
				{
					NamespaceDeclaration out_ns_decl = new NamespaceDeclaration(ns_decl.Name);
					out_unit.Children.Add(out_ns_decl);

					foreach (INode subnode in ns_decl.Children)
					{
						TypeDeclaration type_decl = subnode as TypeDeclaration;
						if (type_decl != null)
						{
							type_decl = Dynamize(type_decl);
							if (type_decl != null) out_ns_decl.Children.Add(type_decl);
						}
					}
				}

				// as well as top-level types without namespaces
				TypeDeclaration bare_type_decl = node as TypeDeclaration;
				if (bare_type_decl != null)
				{
					bare_type_decl = Dynamize(bare_type_decl);
					if (bare_type_decl != null) out_unit.Children.Add(bare_type_decl);
				}
			}

			return out_unit;
		}

		/// <summary>
		/// Dynamizes a given type.
		/// </summary>
		private TypeDeclaration Dynamize(TypeDeclaration type)
		{
			if (!Utility.IsDecoratedByAttribute(type, "PHP.Core.ImplementsType")) return null;

			TypeDeclaration out_type = new TypeDeclaration(type.Modifier, new List<AttributeSection>());
			out_type.Name = type.Name;

			AddSerializibility(type, out_type);
			FixInheritance(type, out_type);
			DynamizeMembers(type, out_type);

			return out_type;
		}

		#region Serializibility & Inheritance

		/// <summary>
		/// Makes sure that the PHP-visible type is serializable.
		/// </summary>
		private void AddSerializibility(TypeDeclaration type, TypeDeclaration outType)
		{
			// make the type serializable
			if (!Utility.IsDecoratedByAttribute(type, "System.SerializableAttribute"))
			{
				AttributeSection section = new AttributeSection();
				section.Attributes.Add(new ICSharpCode.NRefactory.Parser.AST.Attribute("Serializable", null, null));
				outType.Attributes.Add(section);

				ConstructorDeclaration ctor = new ConstructorDeclaration(type.Name, 
					((type.Modifier & Modifier.Sealed) == Modifier.Sealed ? Modifier.Private : Modifier.Protected),
					new List<ParameterDeclarationExpression>(), null);

				ctor.Parameters.Add(
					new ParameterDeclarationExpression(new TypeReference("System.Runtime.Serialization.SerializationInfo"), "info"));
				ctor.Parameters.Add(
					new ParameterDeclarationExpression(new TypeReference("System.Runtime.Serialization.StreamingContext"), "context"));

				ctor.ConstructorInitializer = new ConstructorInitializer();
				ctor.ConstructorInitializer.ConstructorInitializerType = ConstructorInitializerType.Base;
					
				ctor.ConstructorInitializer.Arguments.Add(new IdentifierExpression("info"));
				ctor.ConstructorInitializer.Arguments.Add(new IdentifierExpression("context"));

				ctor.Body = new BlockStatement();

				outType.AddChild(ctor);
			}
		}

		/// <summary>
		/// Makes sure that the PHP-visible type derives from PhpObject and adds appropriate constructors.
		/// </summary>
		private void FixInheritance(TypeDeclaration type, TypeDeclaration outType)
		{
			// make the type inherit from PhpObject
			bool has_base = false;

			foreach (TypeReference base_type in type.BaseTypes)
			{
				// TODO: base this decision on an attribute?
				if (!base_type.Type.StartsWith("I") && !base_type.Type.StartsWith("SPL."))
				{
					has_base = true;
					break;
				}
			}

			if (!has_base) outType.BaseTypes.Add(new TypeReference("PhpObject"));

			// add the necessary constructors
			bool has_short_ctor = false;
			bool has_long_ctor = false;

			BlockStatement default_ctor_body = null;

			foreach (INode member in type.Children)
			{
				ConstructorDeclaration ctor = member as ConstructorDeclaration;
				if (ctor != null)
				{
					if (ctor.Parameters.Count == 2 && Utility.IsType(ctor.Parameters[0].TypeReference, "PHP.Core.ScriptContext"))
					{
						if (Utility.IsType(ctor.Parameters[1].TypeReference, "System.Boolean")) has_short_ctor = true;
						if (Utility.IsType(ctor.Parameters[1].TypeReference, "PHP.Core.Reflection.DTypeDesc")) has_long_ctor = true;
					}
					else if (ctor.Parameters.Count == 0)
					{
						default_ctor_body = ctor.Body;
					}
				}
			}

			if (!has_short_ctor)
			{
				ConstructorDeclaration ctor = new ConstructorDeclaration(type.Name, Modifier.Public,
					new List<ParameterDeclarationExpression>(), null);
				ctor.Parameters.Add(new ParameterDeclarationExpression(new TypeReference("ScriptContext"), "context"));
				ctor.Parameters.Add(new ParameterDeclarationExpression(new TypeReference("Boolean"), "newInstance"));

				ctor.ConstructorInitializer = new ConstructorInitializer();
				ctor.ConstructorInitializer.ConstructorInitializerType = ConstructorInitializerType.Base;
				ctor.ConstructorInitializer.Arguments.Add(new IdentifierExpression("context"));
				ctor.ConstructorInitializer.Arguments.Add(new IdentifierExpression("newInstance"));

				if (default_ctor_body == null) ctor.Body = new BlockStatement();
				else ctor.Body = default_ctor_body;

				Utility.MakeNonBrowsable(ctor);
				outType.AddChild(ctor);
			}

			if (!has_long_ctor)
			{
				ConstructorDeclaration ctor = new ConstructorDeclaration(type.Name, Modifier.Public,
					new List<ParameterDeclarationExpression>(), null);
				ctor.Parameters.Add(new ParameterDeclarationExpression(new TypeReference("ScriptContext"), "context"));
				ctor.Parameters.Add(new ParameterDeclarationExpression(new TypeReference("DTypeDesc"), "caller"));

				IdentifierExpression context_param = new IdentifierExpression("context");
				IdentifierExpression caller_param = new IdentifierExpression("caller");

				ctor.ConstructorInitializer = new ConstructorInitializer();
				ctor.ConstructorInitializer.ConstructorInitializerType = ConstructorInitializerType.This;
				ctor.ConstructorInitializer.Arguments.Add(context_param);
				ctor.ConstructorInitializer.Arguments.Add(new PrimitiveExpression(true, String.Empty));

				InvocationExpression invocation = new InvocationExpression(
						new FieldReferenceExpression(new ThisReferenceExpression(), "InvokeConstructor"),
						new ArrayList());

				invocation.Arguments.Add(context_param);
				invocation.Arguments.Add(caller_param);

				ctor.Body = new BlockStatement();
				ctor.Body.AddChild(new StatementExpression(invocation));

				Utility.MakeNonBrowsable(ctor);
				outType.AddChild(ctor);
			}
		}

		#endregion

		/// <summary>
		/// Adds argfull and argless stubs for all PHP visible members.
		/// </summary>
		private void DynamizeMembers(TypeDeclaration type, TypeDeclaration outType)
		{
			List<Statement> populate_statements = new List<Statement>();

			foreach (INode member in type.Children)
			{
				AttributedNode node = member as AttributedNode;
				if (node != null && Utility.IsDecoratedByAttribute(node, "PHP.Core.PhpVisibleAttribute"))
				{
					MethodDeclaration method_decl;
					PropertyDeclaration prop_decl;

					if ((method_decl = member as MethodDeclaration) != null)
					{
						populate_statements.Add(DynamizeMethod(method_decl, outType));
					}
					else if ((prop_decl = member as PropertyDeclaration) != null)
					{
						populate_statements.Add(DynamizeProperty(prop_decl, outType));
					}
					else throw new InvalidOperationException("PhpVisible applied to invalid member");
				}
			}

			// add the __PopulateTypeDesc method
			MethodDeclaration populator = new MethodDeclaration(
				"__PopulateTypeDesc",
				Modifier.Private | Modifier.Static,
				new TypeReference("void", "System.Void"), new List<ParameterDeclarationExpression>(), null);

			populator.Parameters.Add(new ParameterDeclarationExpression(new TypeReference("PhpTypeDesc"), "desc"));

			populator.Body = new BlockStatement();
			foreach (Statement stmt in populate_statements)
			{
				if (stmt != null) populator.Body.AddChild(stmt);
			}

			outType.AddChild(populator);
		}

		#region Method dynamization

		/// <summary>
		/// Adds stubs for a PhpVisible method.
		/// </summary>
		private Statement DynamizeMethod(MethodDeclaration method, TypeDeclaration outType)
		{
			bool has_this;
			MethodDeclaration argfull = CreateArgfull(method, false, out has_this);

			if (has_this)
			{
				outType.AddChild(argfull);
				argfull = CreateArgfull(method, true, out has_this);
			}
			outType.AddChild(argfull);

			MethodDeclaration argless = CreateArgless(method);
			outType.AddChild(argless);

			// return an expression to be put to __PopulateTypeDesc
			ArrayList parameters = new ArrayList();
			parameters.Add(new PrimitiveExpression(method.Name, method.Name));
			parameters.Add(Utility.ModifierToMemberAttributes(argfull.Modifier));

			ArrayList del_params = new ArrayList();
			del_params.Add(new FieldReferenceExpression(
				new TypeReferenceExpression(((TypeDeclaration)method.Parent).Name),
				method.Name));

			parameters.Add(new ObjectCreateExpression(new TypeReference("RoutineDelegate"), del_params));

			return new StatementExpression(new InvocationExpression(
				new FieldReferenceExpression(new IdentifierExpression("desc"), "AddMethod"),
				parameters));
		}

		/// <summary>
		/// Creates an argfull stub for the specified implementation method.
		/// </summary>
		private MethodDeclaration CreateArgfull(MethodDeclaration template, bool skipThisParams, out bool hasThisParams)
		{
			hasThisParams = false;

			MethodDeclaration method = new MethodDeclaration(template.Name, template.Modifier,
				new TypeReference("Object"), new List<ParameterDeclarationExpression>(), new List<AttributeSection>());

			method.Body = new BlockStatement();

			Expression[] arguments = new Expression[template.Parameters.Count];

			// prepend a ScriptContext parameter and make all parameters Objects
			// (TODO: PhpReferences for ref parameters)
			method.Parameters.Add(new ParameterDeclarationExpression(new TypeReference("ScriptContext"), "__context"));

			int arg_counter = 0;
			foreach (ParameterDeclarationExpression param in template.Parameters)
			{
				ParameterDeclarationExpression new_param =
					new ParameterDeclarationExpression(new TypeReference("Object"), param.ParameterName);

				bool optional = false;

				if (Utility.IsDecoratedByAttribute(param.Attributes, Utility.OptionalAttrType))
				{
					AttributeSection section = new AttributeSection();
					new_param.Attributes.Add(section);
					section.Attributes.Add(new ICSharpCode.NRefactory.Parser.AST.Attribute(Utility.OptionalAttrType, null, null));

					optional = true;
				}

				bool this_param = Utility.IsDecoratedByAttribute(param.Attributes, "PHP.Core.ThisAttribute");
				if (this_param) hasThisParams = true;

				if (this_param && skipThisParams)
				{
					arguments[arg_counter++] = new PrimitiveExpression(null, String.Empty);
				}
				else
				{
					// generate conversion
					arguments[arg_counter++] = Convertor.ConvertTo(
						template.Name,
						new IdentifierExpression(param.ParameterName),
						param.TypeReference,
						method.Body,
						new ReturnStatement(new PrimitiveExpression(null, String.Empty)),
						Utility.IsDecoratedByAttribute(param.Attributes, "PHP.Core.NullableAttribute") || this_param,
						optional,
						arg_counter);

					method.Parameters.Add(new_param);
				}
			}

			// invoke the template method
			InvocationExpression invocation = new InvocationExpression(new IdentifierExpression(template.Name),
				new ArrayList(arguments));

			if (template.TypeReference.SystemType == "System.Void")
			{
				method.Body.AddChild(new StatementExpression(invocation));
				method.Body.AddChild(new ReturnStatement(new PrimitiveExpression(null, String.Empty)));
			}
			else method.Body.AddChild(new ReturnStatement(invocation));

			if (!hasThisParams || skipThisParams) Utility.MakeNonBrowsable(method);
			return method;
		}

		/// <summary>
		/// Creates an argless stub for the specified implementation method.
		/// </summary>
		private MethodDeclaration CreateArgless(MethodDeclaration template)
		{
			MethodDeclaration method = new MethodDeclaration(template.Name, Modifier.Public | Modifier.Static,
				new TypeReference("Object"), new List<ParameterDeclarationExpression>(), new List<AttributeSection>());

			method.Parameters.Add(new ParameterDeclarationExpression(new TypeReference("Object"), "instance"));
			method.Parameters.Add(new ParameterDeclarationExpression(new TypeReference("PhpStack"), "stack"));

			method.Body = new BlockStatement();

			// stack.CalleeName = <template name>
			method.Body.AddChild(new StatementExpression(new AssignmentExpression(new FieldReferenceExpression(
				new IdentifierExpression("stack"), "CalleeName"), AssignmentOperatorType.Assign,
				new PrimitiveExpression(template.Name, template.Name))));

			// peek arguments
			int arg_counter = 0, shift = 0;
			foreach (ParameterDeclarationExpression param in template.Parameters)
			{
				arg_counter++;

				LocalVariableDeclaration arg_local = new LocalVariableDeclaration(new TypeReference("Object"));

				Expression initializer;
				if (Utility.IsDecoratedByAttribute(param.Attributes, "PHP.Core.ThisAttribute"))
				{
					initializer = new IdentifierExpression("instance");
					shift++;
				}
				else
				{
					ArrayList peek_params = new ArrayList();
					peek_params.Add(new PrimitiveExpression(arg_counter - shift, String.Empty));

					if (Utility.IsDecoratedByAttribute(param.Attributes, Utility.OptionalAttrType))
					{
						initializer = new InvocationExpression(new FieldReferenceExpression(
							new IdentifierExpression("stack"), "PeekValueOptional"), peek_params);
					}
					else
					{
						initializer = new InvocationExpression(new FieldReferenceExpression(
							new IdentifierExpression("stack"), "PeekValue"), peek_params);
					}
				}

				arg_local.Variables.Add(new VariableDeclaration(String.Format("arg{0}", arg_counter), initializer));
				method.Body.AddChild(arg_local);
			}

			// stack.RemoveFrame()
			method.Body.AddChild(new StatementExpression(new InvocationExpression(new FieldReferenceExpression(
				new IdentifierExpression("stack"), "RemoveFrame"), new ArrayList())));

			// return [invoke argfull]
			ArrayList argfull_params = new ArrayList();
			argfull_params.Add(new FieldReferenceExpression(new IdentifierExpression("stack"), "Context"));
			for (int i = 0; i < template.Parameters.Count; i++)
			{
				argfull_params.Add(new IdentifierExpression(String.Format("arg{0}", i + 1)));
			}

			if ((template.Modifier & Modifier.Static) == Modifier.Static)
			{
				method.Body.AddChild(new ReturnStatement(new InvocationExpression(new IdentifierExpression(
					template.Name), argfull_params)));
			}
			else
			{
				method.Body.AddChild(new ReturnStatement(new InvocationExpression(
					new FieldReferenceExpression(new ParenthesizedExpression(
					new CastExpression(new TypeReference(((TypeDeclaration)template.Parent).Name),
					new IdentifierExpression("instance"))), template.Name), argfull_params)));
			}

			Utility.MakeNonBrowsable(method);
			return method;
		}

		#endregion

		#region Property dynamization

		/// <summary>
		/// Adds stubs for a PhpVisible property.
		/// </summary>
		private Statement DynamizeProperty(PropertyDeclaration property, TypeDeclaration outType)
		{
			MethodDeclaration getter = null, setter = null;

			if (property.HasGetRegion)
			{
				// add the getter stub
				getter = new MethodDeclaration(
					"__get_" + property.Name, Modifier.Private | Modifier.Static,
					new TypeReference("Object"),
					new List<ParameterDeclarationExpression>(),
					new List<AttributeSection>());

				getter.Parameters.Add(new ParameterDeclarationExpression(new TypeReference("Object"), "instance"));

				getter.Body = new BlockStatement();
				getter.Body.AddChild(new ReturnStatement(new FieldReferenceExpression(new ParenthesizedExpression(
					new CastExpression(new TypeReference(((TypeDeclaration)property.Parent).Name),
					new IdentifierExpression("instance"))), property.Name)));

				outType.AddChild(getter);
			}

			if (property.HasSetRegion)
			{
				// add the setter stub
				setter = new MethodDeclaration(
					"__set_" + property.Name, Modifier.Private | Modifier.Static,
					new TypeReference("void", "System.Void"),
					new List<ParameterDeclarationExpression>(),
					new List<AttributeSection>());

				setter.Parameters.Add(new ParameterDeclarationExpression(new TypeReference("Object"), "instance"));
				setter.Parameters.Add(new ParameterDeclarationExpression(new TypeReference("Object"), "value"));

				setter.Body = new BlockStatement();

				Expression rhs = Convertor.ConvertTo(
					property.Name,
					new IdentifierExpression("value"),
					property.TypeReference,
					setter.Body,
					new ReturnStatement(NullExpression.Instance),
					false,
					false,
					1);

				setter.Body.AddChild(new StatementExpression(new AssignmentExpression(
					(new FieldReferenceExpression(new ParenthesizedExpression(
					new CastExpression(new TypeReference(((TypeDeclaration)property.Parent).Name),
					new IdentifierExpression("instance"))), property.Name)),
					AssignmentOperatorType.Assign, rhs)));

				outType.AddChild(setter);
			}

			// return an expression to be put to __PopulateTypeDesc
			ArrayList parameters = new ArrayList();
			parameters.Add(new PrimitiveExpression(property.Name, property.Name));
			parameters.Add(Utility.ModifierToMemberAttributes(property.Modifier));

			if (getter != null)
			{
				ArrayList del_params = new ArrayList();
				del_params.Add(new FieldReferenceExpression(
					new TypeReferenceExpression(((TypeDeclaration)property.Parent).Name),
					getter.Name));

				parameters.Add(new ObjectCreateExpression(new TypeReference("GetterDelegate"), del_params));
			}
			else parameters.Add(new PrimitiveExpression(null, String.Empty));

			if (setter != null)
			{
				ArrayList del_params = new ArrayList();
				del_params.Add(new FieldReferenceExpression(
					new TypeReferenceExpression(((TypeDeclaration)property.Parent).Name),
					setter.Name));

				parameters.Add(new ObjectCreateExpression(new TypeReference("SetterDelegate"), del_params));
			}
			else parameters.Add(new PrimitiveExpression(null, String.Empty));

			return new StatementExpression(new InvocationExpression(
				new FieldReferenceExpression(new IdentifierExpression("desc"), "AddProperty"),
				parameters));
		}

		#endregion
	}
}
