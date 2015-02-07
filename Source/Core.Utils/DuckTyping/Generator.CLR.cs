/*

 Copyright (c) 2006 Tomas Petricek

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Collections.Generic;
using System.Text;

using PHP.Core;
using PHP.Core.AST;
using System.CodeDom.Compiler;
using System.CodeDom;
using System.IO;

namespace PHP.Core
{
	/// <summary>
	/// Describes one module which is being processed by duck type generator.
	/// </summary>
	internal class DuckModule
	{
		/// <summary>
		/// Gets the name of the file associated with this module.
		/// </summary>
		public string Filename { get { return filename; } }
		private string filename;

		/// <summary>
		/// Tells whether public global functions are present in the module.
		/// </summary>
		/// <remarks>
		/// Determines if the class which wraps global functions will be created. At the beginning 
		/// of the duck type generation this is set to false and when first global function
		/// declaration is found it is set to true.
		/// </remarks>
		public bool Globals { get { return globals; } set { globals = value; } }
		private bool globals;

		/// <summary>
		/// Tells whether public global types are declared in the module.
		/// </summary>
		public bool Types { get { return types; } set { types = value; } }
		private bool types;

		/// <summary>
		/// CodeDom type declaration which describes class which wraps global functions.
		/// </summary>
		public CodeTypeDeclaration GlobalClass { get { return globalClass; } }
		private CodeTypeDeclaration globalClass;

		/// <summary>
		/// List of CodeDom type declarations which describes PHP class wrappers.
		/// </summary>
		public List<CodeTypeDeclaration> Classes { get { return classes; } }
		private List<CodeTypeDeclaration> classes;

		/// <summary>
		/// Initializes DuckModule class.
		/// </summary>
		/// <param name="filename">Filename associated with the module.</param>
		public DuckModule(string filename)
		{
			classes = new List<CodeTypeDeclaration>();
			this.filename = filename;
			globals = false;
			types = false;
			globalClass = new CodeTypeDeclaration();
			globalClass.Name = DuckTypeGenerator.MakeModuleName(filename) + "Globals";
			globalClass.IsInterface = true;
			globalClass.CustomAttributes.Add(
				new CodeAttributeDeclaration(new CodeTypeReference(typeof(DuckTypeAttribute)),
				new CodeAttributeArgument("GlobalFunctions", new CodePrimitiveExpression(true)))
				);
		}
	}

	/// <summary>
	/// Collects information about duck types and then generates CodeDOM objects, which are used for
	/// the actual code.
	/// </summary>
	/// <remarks>
	/// The class is singleton.
	/// </remarks>
	class DuckTypeGenerator : TreeVisitor
	{
		/// <summary>
		/// Singleton instance of the class.
		/// </summary>
		public static DuckTypeGenerator Instance { get { return instance ?? (instance = new DuckTypeGenerator()); } }
		private static DuckTypeGenerator instance;

		private DuckTypeGenerator()
		{
			moduleList = new List<DuckModule>();
		}

		/// <summary>
		/// Modules which have been processed by the duck type generator.
		/// </summary>
		List<DuckModule> moduleList;

		/// <summary>
		/// Current module for storing type and function declarations.
		/// </summary>
		DuckModule currentModule;

		/// <summary>
		/// Current class.
		/// </summary>
		CodeTypeDeclaration currentClass;

		/// <summary>
		/// Makes a name that looks like C# name (upper case first char).
		/// </summary>
		public static string MakeCSharpName(string name)
		{
			return name[0].ToString().ToUpper() + name.Substring(1);
		}

		/// <summary>
		/// Makes a name that looks like C# field name (lower case first char).
		/// </summary>
		public static string MakeCSharpFieldName(string name)
		{
			return name[0].ToString().ToLower() + name.Substring(1);
		}

        public static string MakeModuleName(string name)
        {
            return MakeCSharpName(Path.GetFileNameWithoutExtension(name).Replace(".", ""));
        }

		/// <summary>
		/// Generate duck-type interface file for the specified AST.
		/// </summary>
		/// <param name="globalCode">Source AST</param>
		public void ProcessModule(GlobalCode globalCode)
		{
			currentModule = new DuckModule(globalCode.SourceUnit.SourceFile.RelativePath.ToString());
			globalCode.VisitMe(this);
			moduleList.Add(currentModule);
		}

		/// <summary>
		/// Generates code units that will be used for generating.
		/// </summary>
		/// <param name="targetPath"></param>
		/// <param name="targetNamespace"></param>
		/// <returns></returns>
		public IEnumerable<CodeCompileUnit> GenerateCodeUnits(FullPath targetPath, string targetNamespace)
		{
			CodeCompileUnit compileUnit = new CodeCompileUnit();
			CodeNamespace space = new CodeNamespace(targetNamespace ?? "Generated");
			compileUnit.Namespaces.Add(space);

			space.Types.Add(GenerateFactoryClass());

			compileUnit.UserData.Add("ID", "DuckTypeFactory");

			yield return compileUnit;

			foreach (DuckModule module in moduleList)
			{
				compileUnit = new CodeCompileUnit();
				space = new CodeNamespace(targetNamespace ?? "Generated");
				compileUnit.Namespaces.Add(space);

				if (module.Globals)
				{
					space.Types.Add(module.GlobalClass);
				}
				
				foreach(CodeTypeDeclaration cls in module.Classes)
				{
					space.Types.Add(cls);
				}

				compileUnit.UserData.Add("ID", MakeModuleName(module.Filename));

				if (module.Globals || module.Types)
					yield return compileUnit;
			}			
		}

		/// <summary>
		/// Generates factory class which is used for creating 
		/// </summary>
		/// <returns></returns>
		private CodeTypeDeclaration GenerateFactoryClass()
		{
			CodeTypeDeclaration factory = new CodeTypeDeclaration();

			factory.Attributes = MemberAttributes.Public;
			factory.Name = "DuckTypeFactory";
			factory.IsPartial = true;

			CodeMemberField instanceField = new CodeMemberField("DuckTypeFactory", "instance");
			instanceField.Attributes = MemberAttributes.Private | MemberAttributes.Static;
			instanceField.CustomAttributes.Add(new CodeAttributeDeclaration(new CodeTypeReference(typeof(ThreadStaticAttribute))));
			factory.Members.Add(instanceField);

			CodeMemberProperty instanceProp = new CodeMemberProperty();
			instanceProp.Attributes = MemberAttributes.Public | MemberAttributes.Static;
			instanceProp.HasGet = true;
			instanceProp.HasSet = false;
			instanceProp.Type = new CodeTypeReference("DuckTypeFactory");
			instanceProp.Name = "Instance";
			instanceProp.GetStatements.Add(
				new CodeConditionStatement(
					new CodeBinaryOperatorExpression(
						new CodeVariableReferenceExpression("instance"),
						CodeBinaryOperatorType.IdentityEquality,
						new CodePrimitiveExpression(null)
					),
					new CodeAssignStatement(
						new CodeVariableReferenceExpression("instance"),
						new CodeObjectCreateExpression(
							new CodeTypeReference("DuckTypeFactory")
							)
						)
					)
				);
			instanceProp.GetStatements.Add(
				new CodeMethodReturnStatement(
					new CodeVariableReferenceExpression("instance")
					)
				);

			factory.Members.Add(instanceProp);

			//context and global class fields
			CodeMemberField field;

			field = new CodeMemberField(typeof(ScriptContext), "context");
			field.Attributes = MemberAttributes.Private;
			factory.Members.Add(field);
		
			foreach (DuckModule module in moduleList)
			{
				if (!module.Globals) continue;

				field = new CodeMemberField(module.GlobalClass.Name, MakeCSharpFieldName(module.GlobalClass.Name));
				field.Attributes = MemberAttributes.Private;
				factory.Members.Add(field);
			}

			//static ctor
			CodeConstructor ctor = new CodeConstructor();
			ctor.Attributes = MemberAttributes.Private;

			// this.context = ScriptContext.Current;			
			ctor.Statements.Add(
				new CodeAssignStatement(
					new CodeVariableReferenceExpression("context"),
					new CodePropertyReferenceExpression(
						new CodeTypeReferenceExpression(typeof(ScriptContext)),
							"CurrentContext"
						)
					)
				);

			ctor.Statements.Add(
				new CodeMethodInvokeExpression(new CodeThisReferenceExpression(), "Init")
				);


			foreach (DuckModule module in moduleList)
			{
				if (!module.Globals && !module.Types) continue;

				// context.IncludeScript("<<path>>", libraryRepresentative);
				ctor.Statements.Add(
					new CodeMethodInvokeExpression(
						new CodeMethodReferenceExpression(
							new CodeVariableReferenceExpression("context"),
							"IncludeScriptOnce"
							),
						new CodePrimitiveExpression(module.Filename),
						new CodeVariableReferenceExpression("libraryRepresentative")
						)
					);
			}

			foreach (DuckModule module in moduleList)
			{
				if (!module.Globals) continue;

				// this.<module name> = context.NewObject<<module name>>();
				ctor.Statements.Add(
					new CodeAssignStatement(
						new CodeVariableReferenceExpression(MakeCSharpFieldName(module.GlobalClass.Name)),						
						new CodeMethodInvokeExpression(
							new CodeMethodReferenceExpression(
								new CodeVariableReferenceExpression("context"),
								"NewObject",
								new CodeTypeReference(module.GlobalClass.Name)
								)
							)
						)
					);
			}

			factory.Members.Add(ctor);

			//global function class properties
			foreach (DuckModule module in moduleList)
			{
				if (!module.Globals) continue;

				CodeMemberProperty prop = new CodeMemberProperty();

				prop.Attributes = MemberAttributes.Public;
				prop.HasGet = true;
				prop.HasSet = false;
				prop.Type = new CodeTypeReference(module.GlobalClass.Name);
				prop.Name = module.GlobalClass.Name;
				// return this.<module name>;
				prop.GetStatements.Add(
					new CodeMethodReturnStatement(
						new CodeVariableReferenceExpression(MakeCSharpFieldName(module.GlobalClass.Name))
						)
					);

				factory.Members.Add(prop);
			}

			//class factory methods	
			foreach (DuckModule module in moduleList)
			{
				foreach (CodeTypeDeclaration type in module.Classes)
				{
					CodeMemberMethod meth = new CodeMemberMethod();

					meth.Attributes = MemberAttributes.Public;
					meth.Name = "New" + type.Name;
					meth.ReturnType = new CodeTypeReference(type.Name);

					CodeParameterDeclarationExpression param = new CodeParameterDeclarationExpression();
					param.CustomAttributes.Add(new CodeAttributeDeclaration(new CodeTypeReference(typeof(ParamArrayAttribute))));
					param.Name = "ctorArguments";
					param.Type = new CodeTypeReference(typeof(object[]));
					meth.Parameters.Add(param);

					meth.Statements.Add(
						new CodeMethodReturnStatement(
							new CodeMethodInvokeExpression(
								new CodeMethodReferenceExpression(
									new CodeVariableReferenceExpression("context"),
									"NewObject",
									new CodeTypeReference(type.Name)
									),
								new CodePrimitiveExpression(type.Name),
								new CodeVariableReferenceExpression("ctorArguments")
								)								
							)
						);

					factory.Members.Add(meth);
				}
			}

			return factory;
		}

		/// <summary>
		/// Process class - set currentClass and process methods
		/// </summary>
		public override void VisitTypeDecl(TypeDecl x)
		{
            var cmt = x.PHPDoc;
            if (cmt == null) return;

			if (cmt.Access == Reflection.PhpMemberAttributes.Public)
			{
				currentModule.Types = true;
				currentClass = GenerateTypeDecl(cmt, x);
				currentModule.Classes.Add(currentClass);
			}

			base.VisitTypeDecl(x);
		}

        ///// <summary>
        ///// Process eval expression - if the eval was produced by the TypeDecl, we process the original TypeDecl.
        ///// </summary>
        //public override void VisitEvalEx(EvalEx x)
        //{
        //    TypeDecl typeDecl;

        //    if (x.Annotations.TryGet<TypeDecl>(out typeDecl))
        //    {
        //        docResolver.VisitTypeDecl(typeDecl);
        //        VisitTypeDecl(typeDecl);
        //    }

        //    base.VisitEvalEx(x);
        //}

		/// <summary>
		/// Generate type declaration
		/// </summary>
		public CodeTypeDeclaration GenerateTypeDecl(PHPDocBlock cmt, TypeDecl x)
		{
			CodeTypeDeclaration cls = new CodeTypeDeclaration(MakeCSharpName(x.Name.Value));

            var summary = cmt.Summary;
			if (!string.IsNullOrEmpty(summary))
			{
				cls.Comments.Add(new CodeCommentStatement("<summary>\n " + summary + "\n </summary>", true));
			}

			cls.IsInterface = true;
			cls.CustomAttributes.Add(new CodeAttributeDeclaration(new CodeTypeReference(typeof(DuckTypeAttribute))));

			return cls;			
		}

		/// <summary>
		/// Add list of fields to the current class.
		/// </summary>
		public override void VisitFieldDeclList(FieldDeclList x)
		{
            var cmt = x.PHPDoc;
            if (cmt == null) return;

			if (cmt.Access == Reflection.PhpMemberAttributes.Public)
			{
				foreach (FieldDecl fld in x.Fields)
				{					
					currentClass.Members.Add(GenerateFieldCode(cmt, fld.Name.Value));
				}
			}
		}

		/// <summary>
		/// Add field as a property with getter &amp; setter
		/// </summary>
		private CodeMemberProperty GenerateFieldCode(PHPDocBlock cmt, string name)
		{
			CodeMemberProperty prop = new CodeMemberProperty();

            var vartag = cmt.GetElement<PHPDocBlock.VarTag>();
            if (vartag != null)
                prop.Type = ParseType(vartag.TypeNamesArray);
			else
				prop.Type = new CodeTypeReference(typeof(object));

			prop.CustomAttributes.Add(new CodeAttributeDeclaration(new CodeTypeReference(typeof(DuckNameAttribute)),
				new CodeAttributeArgument(new CodePrimitiveExpression(name))));

            var summary = cmt.Summary;
			if (!string.IsNullOrEmpty(summary))
			{
				prop.Comments.Add(new CodeCommentStatement("<summary>\n " + summary + "\n </summary>", true));
			}

			prop.HasGet = prop.HasSet = true;
			prop.Name = MakeCSharpName(name);
			return prop;
		}

		/// <summary>
		/// Add method to the current class...
		/// </summary>
		public override void VisitMethodDecl(MethodDecl x)
		{
			// Skip constructors
            if (x.Name == PHP.Core.Reflection.DObject.SpecialMethodNames.Construct) return;

            var cmt = x.PHPDoc;
            if (cmt == null) return;

			if (cmt.Access == Reflection.PhpMemberAttributes.Public)
			{
				currentClass.Members.Add(GenerateFunctionCode(cmt, x.Name.Value));
			}
		}

		/// <summary>
		/// Add global function to the interface with global functions
		/// </summary>
		public override void VisitFunctionDecl(FunctionDecl x)
		{
            var cmt = x.PHPDoc;
            if (cmt == null) return;

			if (cmt.Access == Reflection.PhpMemberAttributes.Public)
			{
				currentModule.Globals = true;
				currentModule.GlobalClass.Members.Add(GenerateFunctionCode(cmt, x.Function.Name.Value));
			}			
		}

		/// <summary>
		/// Process function or method declaration and return corresponding CodeDom object.
		/// </summary>
		private CodeMemberMethod GenerateFunctionCode(PHPDocBlock cmt, string name)
		{
			// Add method for each global function
			CodeMemberMethod meth = new CodeMemberMethod();
			meth.CustomAttributes.Add(new CodeAttributeDeclaration(new CodeTypeReference(typeof(DuckNameAttribute)),
				new CodeAttributeArgument(new CodePrimitiveExpression(name))));
			meth.Name = MakeCSharpName(name);

			// Return & Summary comment
            string summary = cmt.Summary;
				
			if (!string.IsNullOrEmpty(summary))
			{
				meth.Comments.Add(new CodeCommentStatement("<summary>\n " + summary + "\n </summary>", true));
			}
			if (cmt.Returns != null)
			{
                string retHelp = cmt.Returns.Description;
				if (retHelp.Length > 0)
					meth.Comments.Add(new CodeCommentStatement("<returns>" + retHelp + "</returns>", true));
			}

			// Parse return type 
			if (cmt.Returns != null && cmt.Returns.TypeNames != null)
			{
                var types = cmt.Returns.TypeNamesArray;
                if (types.Length > 1)
					meth.Comments.Add(new CodeCommentStatement("NOT SUPPORTED: Multiple return types are currently TODO."));

                meth.ReturnType = ParseType(types);
			}

			// Process parameters
			foreach (var p in cmt.Params)
			{
                string paramName = p.VariableName;
				string paramHelp = p.Description;
                if (paramName != null && paramHelp.Length > 0)
                {
                    paramName = paramName.TrimStart('$');
                    meth.Comments.Add(new CodeCommentStatement("<param name=\"" + paramName + "\">" + paramHelp + "</param>", true));
                }

				CodeTypeReference paramType = ParseType(p.TypeNamesArray);
				meth.Parameters.Add(new CodeParameterDeclarationExpression(paramType, paramName));
			}

			return meth;
		}

		#region Utils

		/// <summary>
		/// Transforms parsed type information into CLR type name.
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		private static CodeTypeReference ResolveType(string typename)
		{
            if (string.IsNullOrEmpty(typename))
                return null;

            if (typename.StartsWith("array"))
            {
                var attrs = typename.Substring("array".Length);

                // array or array[]
                if (attrs == string.Empty || attrs == "[]")
				    return new CodeTypeReference(typeof(IDuckEnumerable<object>));

                if (attrs[0] == '[')
                {
                    // find matching right brace:
                    int afterBrace = 1;
                    int level = 1;
                    while (afterBrace < attrs.Length && level > 0)
                    {
                        char c = attrs[afterBrace ++];

                        if (c == '[') level ++;
                        else if (c == ']') level --;
                    }

                    if (level != 0) // not matching braces, just an array
                        return new CodeTypeReference(typeof(IDuckEnumerable<object>));

                    //
                    var indexTypeStr = attrs.Substring(1, afterBrace - 2);
                    var valueTypeStr = (afterBrace < attrs.Length) ? attrs.Substring(afterBrace) : null;

                    if (string.IsNullOrEmpty(indexTypeStr))
                    {
                        // array[]T
                        CodeTypeReference valueType = ResolveType(valueTypeStr) ?? new CodeTypeReference(typeof(object));
                        return new CodeTypeReference(typeof(IDuckEnumerable<>).FullName, valueType);
                    }
                    else if (string.IsNullOrEmpty(valueTypeStr))
                    {
                        // array[T]
                        CodeTypeReference indexType = ResolveType(indexTypeStr) ?? new CodeTypeReference(typeof(IDuckEnumerable<object>));
                        return new CodeTypeReference(typeof(IDuckKeyedEnumerable<,>).FullName, indexType, new CodeTypeReference(typeof(object)));
                    }
                    else if (!string.IsNullOrEmpty(indexTypeStr) && !string.IsNullOrEmpty(valueTypeStr))
                    {
                        // array[T]U
                        CodeTypeReference indexType = ResolveType(indexTypeStr) ?? new CodeTypeReference(typeof(IDuckEnumerable<object>));
                        CodeTypeReference valueType = ResolveType(valueTypeStr) ?? new CodeTypeReference(typeof(IDuckEnumerable<object>));

                        return new CodeTypeReference(typeof(IDuckKeyedEnumerable<,>).FullName, indexType, valueType);
                    }
                    else
                    {
                        Debug.Assert(false);
                        return null;
                    }
                }
            }

			switch (typename.ToLowerInvariant())
			{
                case "int":
				case "integer":
					return new CodeTypeReference(typeof(int));
				case "string":
					return new CodeTypeReference(typeof(string));
				case "float":
					return new CodeTypeReference(typeof(float));
				case "double":
					return new CodeTypeReference(typeof(double));
				case "long":
					return new CodeTypeReference(typeof(long));
                case "bool":
				case "boolean":
					return new CodeTypeReference(typeof(bool));
				case "mixed":
					return new CodeTypeReference(typeof(object));
                case "void":
                    return null;
				default:
					// Php type that is declared as an interface earlier...
					return new CodeTypeReference(typename);
			}
		}

		/// <summary>
		/// Parse PHP type from the comment and return CodeTypeReference.
		/// </summary>
		private static CodeTypeReference ParseType(string[] typenames)
		{
            return ResolveType((typenames != null && typenames.Length > 0) ? typenames[0] : null);
		}

        #endregion

        /// <summary>
        /// Generates duck type interfaces for given <see cref="sourceAsts"/>.
        /// </summary>
        /// <param name="sourceAsts">Enumeration of <see cref="GlobalCode"/> - AST of source files containing PHP source code.</param>
        /// <param name="duckPath">Target path.</param>
        /// <param name="duckNamespace">Target namespace.</param>
        public static void GenerateDuckInterfaces(IEnumerable<GlobalCode>/*!!*/ sourceAsts, string duckPath, string duckNamespace)
        {
            // check parameters:
            if (sourceAsts == null)
                throw new ArgumentNullException("sourceAsts");
            if (duckPath == null)
                throw new ArgumentNullException("duckPath");

            // create directory for duck types if not exists:
            Directory.CreateDirectory(duckPath);

            // process source code:
            foreach (var ast in sourceAsts)
            {
                DuckTypeGenerator.Instance.ProcessModule(ast);
            }

            foreach (CodeCompileUnit unit in DuckTypeGenerator.Instance.GenerateCodeUnits(new FullPath(duckPath), duckNamespace))
            {
                string file = unit.UserData["ID"].ToString() + ".cs";
                FullPath path = new FullPath(file, new FullPath(duckPath));

                string dir = Path.GetDirectoryName(path);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                CodeDomProvider provider = CodeDomProvider.CreateProvider("csharp");
                using (StreamWriter wr = new StreamWriter(path.ToString()))
                {
                    provider.GenerateCodeFromCompileUnit(unit, wr, new CodeGeneratorOptions());
                }
            }
        }
	}	
}
