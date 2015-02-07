/*

 Copyright (c) 2006 Ladislav Prosek.  

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

//#define CODEDOM_DUMP

using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

namespace PHP.Core.CodeDom
{
    /// <summary>
    /// PHP <see cref="ICodeGenerator"/> implementation.
    /// </summary>
    internal sealed class PhpCodeGenerator : System.CodeDom.Compiler.CodeGenerator
    {
        internal delegate bool TypeIsReferenceDelegate(CodeTypeReference type);

        #region Fields

        private static readonly Regex simpleIdentifierRegex =
            new Regex(@"^[a-zA-Z_][a-zA-Z0-9_]*$");

        private static readonly Regex identifierRegex =
            new Regex(@"(?:^[a-zA-Z_][a-zA-Z0-9_]*$)|(?:^i'(?:[^'\\]|(?:\\.))+'$)");

        /// <summary>
        /// Open instance delegate that works around the extends/implements issue in base types.
        /// </summary>
        /// <remarks>
        /// http://lab.msdn.microsoft.com/productfeedback/viewfeedback.aspx?feedbackid=a5d4c475-18d2-4121-b282-6583f1695e45
        /// </remarks>
        private static readonly TypeIsReferenceDelegate typeIsInterface =
            (TypeIsReferenceDelegate)Delegate.CreateDelegate(typeof(TypeIsReferenceDelegate), null,
            typeof(CodeTypeReference).GetProperty("IsInterface", BindingFlags.Instance | BindingFlags.NonPublic).GetGetMethod(true));

        /// <summary>
        /// <B>True</B> if generating code to a compile unit (between &lt;? ?&gt;), <B>false</B> otherwise.
        /// </summary>
        private bool inCompileUnit;

        /// <summary>
        /// Imports generated so far.
        /// </summary>
        private Dictionary<string, string>
            importedNamespaces, // names aliased (imported namespaces) in current namespace
            previousImportedNamespaces; // aliases in previous scope

        /// <summary>
        /// <B>True</B> if the class that is currently being generated has a base class (i.e. is not a parent-less
        /// PHP class.
        /// </summary>
        private bool classHasParent;

        /// <summary>
        /// Custom attributes that should be applied to the compile unit that is being generated.
        /// </summary>
        private CodeAttributeDeclarationCollection assemblyAttributes;

        /// <summary>
        /// Stores information about literalness of fields.
        /// </summary>
        private Dictionary<string, bool> isFieldConstantCache = new Dictionary<string, bool>();

        /// <summary>
        /// The assembly in which the last <see cref="IsFieldConstant"/> lookup succeeded.
        /// </summary>
        private Assembly lastAssembly;

        #endregion

        #region Construction

        public PhpCodeGenerator()
        { }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the token that represents a null reference.
        /// </summary>
        protected override string NullToken
        {
            get { return SpecialWords.Null; }
        }

        internal static CodeAttributeDeclaration AppStaticAttribute
        {
            get
            { return new CodeAttributeDeclaration("\\AppStatic"); }
        }

        internal static CodeAttributeDeclaration OutAttribute
        {
            get
            { return new CodeAttributeDeclaration("out"); }
        }

        internal static CodeAttributeDeclaration ExportAttribute
        {
            get
            { return new CodeAttributeDeclaration("\\Export"); }
        }

        #endregion

        #region Supports

        /// <summary>
        /// Gets a value indicating whether the specified code generation support is provided.
        /// </summary>
        protected override bool Supports(GeneratorSupport support)
        {
            GeneratorSupport supported_by_phalanger =
                0
                | GeneratorSupport.ArraysOfArrays
                | GeneratorSupport.AssemblyAttributes
                //| GeneratorSupport.ChainedConstructorArguments
                | GeneratorSupport.ComplexExpressions
                //| GeneratorSupport.DeclareDelegates
                //| GeneratorSupport.DeclareEnums
                //| GeneratorSupport.DeclareEvents
                //| GeneratorSupport.DeclareIndexerProperties
                | GeneratorSupport.DeclareInterfaces
                //| GeneratorSupport.DeclareValueTypes
                | GeneratorSupport.EntryPointMethod
                | GeneratorSupport.GenericTypeDeclaration
                | GeneratorSupport.GenericTypeReference
                | GeneratorSupport.GotoStatements
                //| GeneratorSupport.MultidimensionalArrays
                | GeneratorSupport.MultipleInterfaceMembers
                //| GeneratorSupport.NestedTypes
                | GeneratorSupport.ParameterAttributes
                | GeneratorSupport.PartialTypes
                | GeneratorSupport.PublicStaticMembers
                | GeneratorSupport.ReferenceParameters
                | GeneratorSupport.Resources
                | GeneratorSupport.ReturnTypeAttributes
                //| GeneratorSupport.StaticConstructors
                | GeneratorSupport.TryCatchStatements
                //| GeneratorSupport.Win32Resources
            ;

            return ((support & supported_by_phalanger) == support);
        }

        #endregion

        #region IsValidIdentifier, CreateEscapedIdentifier, CreateValidIdentifier

        /// <summary>
        /// Gets a value indicating whether the specified value is a valid identifier.
        /// </summary>
        protected override bool IsValidIdentifier(string value)
        {
            if (String.IsNullOrEmpty(value) || Keywords.IsKeyword(value)) return false;

            return identifierRegex.IsMatch(value);
        }

        /// <summary>
        /// Creates an escaped identifier for the specified value.
        /// </summary>
        protected override string CreateEscapedIdentifier(string value)
        {
            if (Keywords.IsKeyword(value) || !simpleIdentifierRegex.IsMatch(value))
            {
                StringBuilder sb = new StringBuilder("i'", value.Length + 3);

                for (int i = 0; i < value.Length; i++)
                {
                    if (value[i] == '\'') sb.Append('\\');

                    sb.Append(value[i]);
                }

                if (sb[sb.Length - 1] == '\\') sb.Append('\\');
                sb.Append('\'');

                return sb.ToString();
            }
            else return value;
        }

        /// <summary>
        /// Creates a valid identifier for the specified value.
        /// </summary>
        protected override string CreateValidIdentifier(string value)
        {
            if (Keywords.IsKeyword(value))
            {
                return "_" + value;
            }
            else return value;
        }

        #endregion

        #region GetTypeOutput, OutputType, QuoteSnippetString

        private Dictionary<string, string>/*!*/ getTypeOutputCache = new Dictionary<string, string>();

        /// <summary>
        /// Gets the name of the specified data type.
        /// </summary>
        protected override string GetTypeOutput(CodeTypeReference value)
        {
            if (value.ArrayRank > 0)
            {
                // TODO
                return Keywords.Array;
            }

            string output;
            if (getTypeOutputCache.TryGetValue(value.BaseType, out output))
                return output;


            string base_type;

            if (globalTypes.Contains(value.BaseType))    // known global type ?
            {
                base_type = "." + value.BaseType;           // make it fully qualified (starting with \)
            }
            else
            {
                // GetType(); generate fully qualified name or not, using current importedNamespaces and referencedAssemblies
                var type = Helper.GetType(value, null, importedNamespaces.Keys.ToArray(), referencedAssemblies);

                if (type != null)
                {
                    base_type = type.FullName;

                    bool importFound = false;
                    // try to use alias if possible
                    foreach (var import in importedNamespaces)
                    {
                        if (base_type.StartsWith(import.Key + "."))
                        {
                            base_type = import.Value + base_type.Substring(import.Key.Length);
                            importFound = true;
                            break;
                        }
                    }
                    if (!importFound) base_type = "." + base_type;  // make it fully qualified
                }
                else
                {
                    base_type = value.BaseType;
                }
            }

            if (value.TypeArguments.Count > 0)
            {
                //For generic types name ends with grave and number of generic arguments. Remove this.
                string graveNum = string.Format("`{0:0}", value.TypeArguments.Count);
                if (base_type.EndsWith(graveNum))
                    base_type = base_type.Substring(0, base_type.Length - graveNum.Length);
            }

            StringBuilder sb = new StringBuilder(1 + base_type.Length + 8 + 16 * value.TypeArguments.Count);

            // process the base type
            for (int i = 0; i < base_type.Length; i++)
            {
                if (base_type[i] == '.') sb.Append(Tokens.NamespaceSeparator);
                else sb.Append(base_type[i]);
            }

            // process type arguments
            sb.Append(GetTypeArgumentsOutput(value.TypeArguments));

            return (getTypeOutputCache[value.BaseType] = sb.ToString());
        }

        private string GetTypeArgumentsOutput(CodeTypeReferenceCollection typeArgs)
        {
            int count;
            if (typeArgs != null && (count = typeArgs.Count) > 0)
            {
                StringBuilder sb = new StringBuilder();

                sb.Append(Tokens.GenericBracketLeft);

                sb.Append(GetTypeOutput(typeArgs[0]));
                for (int i = 1; i < count; i++)
                {
                    sb.Append(Tokens.Comma + WhiteSpace.Space);
                    sb.Append(GetTypeOutput(typeArgs[i]));
                }

                sb.Append(Tokens.GenericBracketRight);

                return sb.ToString();
            }
            else return String.Empty;
        }

        /// <summary>
        /// Generates code for the specified type.
        /// </summary>
        protected override void OutputType(CodeTypeReference typeRef)
        {
            Output.Write(GetTypeOutput(typeRef));
        }

        /// <summary>
        /// Converts the specified string by formatting it with escape codes.
        /// </summary>
        protected override string QuoteSnippetString(string value)
        {
            StringBuilder sb = new StringBuilder(value.Length + 2);
            bool need_escape = false;

            sb.Append('"');
            for (int i = 0; i < value.Length; i++)
            {
                char ch = value[i];

                if (i > 0 && (i % 80) == 0)
                {
                    // break the string to more lines, but do not separate surrogates
                    if (Char.IsHighSurrogate(ch) && i < (value.Length - 1) && Char.IsLowSurrogate(value[i + 1]))
                    {
                        sb.Append(value[++i]);
                    }
                    sb.Append("\" .\r\n");

                    // indent
                    for (int j = Indent + 1; j >= 0; j--) sb.Append(Options.IndentString);
                    sb.Append('"');
                }

                switch (ch)
                {
                    case '\n': sb.Append(@"\n"); break;
                    case '\t': sb.Append(@"\t"); break;
                    case '\r': sb.Append(@"\r"); break;
                    case '\\': sb.Append(@"\\"); break;
                    case '$': sb.Append(@"\$"); break;
                    case '"': sb.Append("\\\""); break;
                    default:
                        {
                            switch (Char.GetUnicodeCategory(ch))
                            {
                                case UnicodeCategory.Control:
                                    {
                                        sb.Append('\\');
                                        sb.Append(System.Convert.ToString((int)ch, 8));
                                        need_escape = true;
                                        continue;
                                    }
                                case UnicodeCategory.DecimalDigitNumber:
                                    {
                                        if (need_escape) goto case UnicodeCategory.Control;
                                        else goto default;
                                    }

                                default:
                                    {
                                        // TODO: Unicode escape?
                                        sb.Append(ch);
                                        break;
                                    }
                            }
                            break;
                        }
                }

                need_escape = false;
            }
            sb.Append('"');

            return sb.ToString();
        }

        #endregion

        #region Generation: Generate*

        /// <summary>
        /// Generates code for the specified argument reference expression.
        /// </summary>
        /// <remarks><c>$NAME</c></remarks>
        protected override void GenerateArgumentReferenceExpression(CodeArgumentReferenceExpression e)
        {
            OutputVariable(e.ParameterName);
        }

        /// <summary>
        /// Generates code for the specified array creation expression.
        /// </summary>
        /// <remarks><c>array(INITIALIZER1, INITIALIZER1, ...)</c></remarks>
        protected override void GenerateArrayCreateExpression(CodeArrayCreateExpression e)
        {
            Output.Write(Keywords.Array);
            Output.Write(Tokens.ParenthesisLeft);

            int init_count = e.Initializers.Count;
            for (int i = 0; i < init_count; i++)
            {
                if (i > 0) Output.Write(Tokens.Comma + WhiteSpace.Space);
                GenerateExpression(e.Initializers[i]);
            }

            Output.Write(Tokens.ParenthesisRight);
        }

        /// <summary>
        /// Generates code for the specified array indexer expression.
        /// </summary>
        /// <remarks><c>TARGET[INDEX1][INDEX2]...</c></remarks>
        protected override void GenerateArrayIndexerExpression(CodeArrayIndexerExpression e)
        {
            OutputArrayIndexerExpression(e.TargetObject, e.Indices);
        }

        /// <summary>
        /// Generates code for the specified assignment statement.
        /// </summary>
        /// <remarks><c>LEFT = RIGHT</c> or <c>LEFT.TARGET->set_Item(LEFT.INDICES, RIGHT)</c></remarks>
        protected override void GenerateAssignStatement(CodeAssignStatement e)
        {
            // indexer "set" hack
            CodeIndexerExpression indexer_exp = e.Left as CodeIndexerExpression;
            if (indexer_exp != null)
            {
                CodeExpressionCollection setter_args = new CodeExpressionCollection();
                foreach (CodeExpression exp in indexer_exp.Indices) setter_args.Add(exp);
                setter_args.Add(e.Right);

                OutputInvocation(new CodeMethodReferenceExpression(indexer_exp.TargetObject, SpecialWords.IndexerSet),
                    setter_args);
            }
            else
            {
                GenerateExpression(e.Left);

                Output.Write(WhiteSpace.Space + Tokens.Assignment + WhiteSpace.Space);
                GenerateExpression(e.Right);
            }

            Output.WriteLine(Tokens.Semicolon);
        }

        /// <summary>
        /// Generates code for the specified attach event statement.
        /// </summary>
        /// <remarks><c>EVENT->Add(LISTENER);</c></remarks>
        protected override void GenerateAttachEventStatement(CodeAttachEventStatement e)
        {
            GenerateEventReferenceExpression(e.Event);
            Output.Write(Tokens.Arrow);
            Output.Write(SpecialWords.Add);
            Output.Write(Tokens.ParenthesisLeft);

            GenerateExpression(e.Listener);

            Output.Write(Tokens.ParenthesisRight);
            Output.WriteLine(Tokens.Semicolon);
        }

        /// <summary>
        /// Generates code for the specified attribute block end.
        /// </summary>
        /// <remarks><c>:]</c></remarks>
        protected override void GenerateAttributeDeclarationsEnd(CodeAttributeDeclarationCollection attributes)
        {
            Output.Write(Tokens.AttributeBracketRight);
        }

        /// <summary>
        /// Generates code for the specified attribute block start.
        /// </summary>
        /// <remarks><c>[:</c></remarks>
        protected override void GenerateAttributeDeclarationsStart(CodeAttributeDeclarationCollection attributes)
        {
            Output.Write(Tokens.AttributeBracketLeft);
        }

        /// <summary>
        /// Generates code for the specified base reference expression.
        /// </summary>
        /// <remarks><c>parent</c></remarks>
        protected override void GenerateBaseReferenceExpression(CodeBaseReferenceExpression e)
        {
            Output.Write(SpecialWords.Parent);
        }

        /// <summary>
        /// Generates code for the specified cast expression.
        /// </summary>
        /// <remarks><c>(TARGETTYPE)EXPRESSION</c></remarks>
        protected override void GenerateCastExpression(CodeCastExpression e)
        {
            string target_type = null;

            if (e.TargetType.ArrayRank > 0)
            {
                target_type = SpecialWords.Array;
            }
            else
            {
                switch (e.TargetType.BaseType)
                {
                    case "System.Int32": target_type = SpecialWords.Int; break;
                    case "System.Double": target_type = SpecialWords.Double; break;
                    case "System.Boolean": target_type = SpecialWords.Bool; break;
                    case "System.Object": target_type = SpecialWords.Object; break;
                }
            }

            if (target_type != null)
            {
                Output.Write(Tokens.ParenthesisLeft);
                Output.Write(target_type);
                Output.Write(Tokens.ParenthesisRight);
            }

            GenerateExpression(e.Expression);
        }

        /// <summary>
        /// Generates code for the specified comment.
        /// </summary>
        /// <remarks><c>// TEXT</c> or <c>/** TEST */</c></remarks>
        protected override void GenerateComment(CodeComment e)
        {
            string[] lines = e.Text.Split('\n');

            if (e.DocComment) Output.WriteLine(Tokens.DocCommentLeft);

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].TrimEnd(' ', '\t', '\r');

                if (e.DocComment)
                {
                    Output.Write(Tokens.DocCommentMiddle);
                    line = line.Replace(Tokens.CommentRight, "*_/");
                }
                else Output.Write(Tokens.Comment);

                Output.Write(WhiteSpace.Space);
                Output.WriteLine(line);
            }

            if (e.DocComment) Output.WriteLine(Tokens.DocCommentRight);
        }

        private List<string> referencedAssemblies = null;

        /// <summary>
        /// Generates code for the specified compile unit.
        /// </summary>
        // /// <remarks><c>IMPORTS NAMESPACES</c></remarks>
        protected override void GenerateCompileUnit(CodeCompileUnit e)
        {
            GenerateCompileUnitStart(e);

            importedNamespaces = new Dictionary<string, string>();
            //globalTypes.Clear();

            inCompileUnit = true;
            try
            {
                
                // remember referencedAssemblies = e.ReferencedAssemblies
                if (e.ReferencedAssemblies != null && e.ReferencedAssemblies.Count > 0)
                {
                    referencedAssemblies  = new List<string>(e.ReferencedAssemblies.Count);
                    foreach (var reference in e.ReferencedAssemblies)
                        referencedAssemblies.Add(reference);
                }

                //// imports (all of them must be at the beginning of the unit)
                //int ns_count = e.Namespaces.Count;
                //if (ns_count > 0)
                //{
                //    for (int i = 0; i < ns_count; i++)
                //    {
                //        // import the namespace's imports
                //        GenerateNamespaceImports(e.Namespaces[i]);

                //        // import the declared namespace - PHP differs in this from e.g. C#, in which
                //        // the current scope is determined by the namespace that we are in
                //        if (!String.IsNullOrEmpty(e.Namespaces[i].Name))
                //        {
                //            GenerateNamespaceImport(new CodeNamespaceImport(e.Namespaces[i].Name));
                //        }
                //    }
                //    Output.WriteLine();
                //}

                // assembly attributes
                if (e.AssemblyCustomAttributes.Count > 0) assemblyAttributes = e.AssemblyCustomAttributes;

                // namespaces
                GenerateNamespaces(e);

                // check whether assembly attributes have been attached to a type
                if (assemblyAttributes != null)
                {
                    OutputAttributes(assemblyAttributes, false, SpecialWords.AssemblyAttr);

                    // create an artifical class
                    Output.Write(Keywords.Class + WhiteSpace.Space + SpecialWords.AssemblyInfo);
                    Output.Write(Guid.NewGuid().ToString("N"));

                    GeneratePhpStatements(new CodeStatementCollection(), true);
                    Output.WriteLine();
                }
            }
            finally
            {
                assemblyAttributes = null;
                inCompileUnit = false;
            }

            GenerateCompileUnitEnd(e);
        }

        /// <summary>
        /// Generates code for the end of a compile unit.
        /// </summary>
        /// <remarks><c>?&gt;</c></remarks>
        protected override void GenerateCompileUnitEnd(CodeCompileUnit e)
        {
            base.GenerateCompileUnitEnd(e);

            Indent--;
            Output.WriteLine(Tokens.PhpBracketRight);
        }

        /// <summary>
        /// Generates code for the start of a compile unit.
        /// </summary>
        /// <remarks><c>&lt;?</c></remarks>
        protected override void GenerateCompileUnitStart(CodeCompileUnit e)
        {
#if CODEDOM_DUMP
			using (System.IO.Stream fs = new System.IO.FileStream(System.IO.Path.Combine(@"C:\Temp\CodeDOM",
				Guid.NewGuid().ToString() + ".bin"), System.IO.FileMode.Create))
			{
				new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter().Serialize(fs, e);
			}
#endif
            Output.WriteLine(Tokens.PhpBracketLeft);
            Indent++;

            base.GenerateCompileUnitStart(e);
        }

        /// <summary>
        /// Generates code for the specified conditional statement.
        /// </summary>
        /// <remarks><c>if (CONDITION) TRUESTATEMENTS; else FALSESTATEMENTS</c></remarks>
        protected override void GenerateConditionStatement(CodeConditionStatement e)
        {
            Output.Write(Keywords.If);
            Output.Write(WhiteSpace.Space);
            Output.Write(Tokens.ParenthesisLeft);

            GenerateExpression(e.Condition);

            Output.Write(Tokens.ParenthesisRight);

            bool have_else = (e.FalseStatements.Count > 0);

            GeneratePhpStatements(e.TrueStatements, false, have_else);

            if (have_else)
            {
                Output.Write(Keywords.Else);
                GeneratePhpStatements(e.FalseStatements);
            }
        }

        /// <summary>
        /// Generates code for the specified constructor.
        /// </summary>
        /// <remarks><c>[CA]MODIFIERS function __construct(PARAMETERS) : parent(BASECTORARGS) BODY</c></remarks>
        protected override void GenerateConstructor(CodeConstructor e, CodeTypeDeclaration c)
        {
            // custom attrs
            OutputAttributes(e.CustomAttributes, false);

            // signature
            OutputMemberAccessModifier(e.Attributes);

            Output.Write(Keywords.Function);
            Output.Write(WhiteSpace.Space);
            Output.Write(SpecialWords.Construct);
            Output.Write(Tokens.ParenthesisLeft);
            OutputParameters(e.Parameters);
            Output.Write(Tokens.ParenthesisRight);

            // base ctor call
            if (classHasParent)
            {
                Output.WriteLine();

                Indent++;

                Output.Write(Tokens.Colon);
                Output.Write(WhiteSpace.Space);
                Output.Write(SpecialWords.Parent);

                Output.Write(Tokens.ParenthesisLeft);
                OutputExpressionList(e.BaseConstructorArgs);
                Output.Write(Tokens.ParenthesisRight);

                Indent--;
            }

            // body
            GeneratePhpStatements(e.Statements, true);
        }

        /// <summary>
        /// Generates code for the specified code default value expression.
        /// </summary>
        /// <remarks><c>NULL</c></remarks>
        protected override void GenerateDefaultValueExpression(CodeDefaultValueExpression e)
        {
            // TODO:
            Output.Write(SpecialWords.Null);
        }

        /// <summary>
        /// Generates code for the specified delegate creation expression.
        /// </summary>
        /// <remarks><c>new DELEGATETYPE(array(TARGETOBJECT, METHODNAME))</c></remarks>
        protected override void GenerateDelegateCreateExpression(CodeDelegateCreateExpression e)
        {
            CodeArrayCreateExpression array = new CodeArrayCreateExpression();

            CodeTypeReferenceExpression type_ref = e.TargetObject as CodeTypeReferenceExpression;
            if (type_ref != null)
            {
                // translate type reference to string
                array.Initializers.Add(new CodePrimitiveExpression(GetTypeOutput(type_ref.Type)));
            }
            else array.Initializers.Add(e.TargetObject);

            // add method name
            array.Initializers.Add(new CodePrimitiveExpression(e.MethodName));

            // generate 'new'
            OutputInstantiation(e.DelegateType, new CodeExpressionCollection(new CodeExpression[] { array }));
        }

        /// <summary>
        /// Generates code for the specified delegate invoke expression.
        /// </summary>
        /// <remarks><c>TARGETOBJECT-&gt;Invoke(PARAMETERS)</c></remarks>
        protected override void GenerateDelegateInvokeExpression(CodeDelegateInvokeExpression e)
        {
            OutputInvocation(new CodeMethodReferenceExpression(e.TargetObject, SpecialWords.Invoke), e.Parameters);
        }

        /// <summary>
        /// Generates code for the specified direction expression.
        /// </summary>
        /// <remarks><c>EXPRESSION</c></remarks>
        protected override void GenerateDirectionExpression(CodeDirectionExpression e)
        {
            GenerateExpression(e);
        }

        /// <summary>
        /// Generates code for the specified entry point method.
        /// </summary>
        /// <remarks><c>public function Main() BODY</c></remarks>
        protected override void GenerateEntryPointMethod(CodeEntryPointMethod e, CodeTypeDeclaration c)
        {
            OutputAttributes(e.CustomAttributes, false);

            Output.Write(
                Keywords.Public + WhiteSpace.Space +
                Keywords.Static + WhiteSpace.Space +
                Keywords.Function + WhiteSpace.Space +
                SpecialWords.Main + Tokens.ParenthesisLeft + Tokens.ParenthesisRight);

            // body
            GeneratePhpStatements(e.Statements, true);
        }

        /// <summary>
        /// Generates code for the specified event.
        /// </summary>
        /// <remarks>Not supported.</remarks>
        protected override void GenerateEvent(CodeMemberEvent e, CodeTypeDeclaration c)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        /// <summary>
        /// Generates code for the specified event reference expression.
        /// </summary>
        /// <remarks><c>TARGETOBJECT-&gt;EVENTNAME</c> or <c>TARGETOBJECT::EVENTNAME</c></remarks>
        protected override void GenerateEventReferenceExpression(CodeEventReferenceExpression e)
        {
            OutputMemberReference(e.TargetObject, e.EventName, true, null);
        }

        /// <summary>
        /// Generates code for the specified expression statement.
        /// </summary>
        /// <remarks>EXPRESSION;</remarks>
        protected override void GenerateExpressionStatement(CodeExpressionStatement e)
        {
            GenerateExpression(e.Expression);
            Output.WriteLine(Tokens.Semicolon);
        }

        /// <summary>
        /// Generates code for the specified member field.
        /// </summary>
        /// <remarks><c>public/protected/private static/const NAME/$NAME = INITEXPRESSION;</c></remarks>
        protected override void GenerateField(CodeMemberField e)
        {
            CodeAttributeDeclarationCollection attributes = e.CustomAttributes;

            // check whether [AppStatic] should be used
            if ((e.Attributes & (MemberAttributes.Const | MemberAttributes.Static)) == MemberAttributes.Static)
            {
                bool found = false;

                for (int i = e.CustomAttributes.Count - 1; i >= 0; i--)
                {
                    string type_name = e.CustomAttributes[i].AttributeType.BaseType;
                    if (type_name == "ThreadStatic" || type_name == "ThreadStaticAttribute" ||
                        type_name == "System.ThreadStaticAttribute")
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    attributes = new CodeAttributeDeclarationCollection(attributes);
                    attributes.Add(AppStaticAttribute);
                }
            }

            OutputAttributes(attributes, false);

            OutputMemberAccessModifier(e.Attributes);
            OutputFieldScopeModifier(e.Attributes);

            if ((e.Attributes & MemberAttributes.Const) == MemberAttributes.Const)
            {
                // no '$' prefix for constants
                Output.Write(e.Name);
            }
            else OutputVariable(e.Name);

            if (e.InitExpression != null)
            {
                Output.Write(WhiteSpace.Space + Tokens.Assignment + WhiteSpace.Space);

                GenerateExpression(e.InitExpression);
            }
            base.Output.WriteLine(Tokens.Semicolon);
        }

        /// <summary>
        /// Generates code for the specified field reference expression.
        /// </summary>
        /// <remarks><c>TARGETOBJECT-&gt;FIELDNAME</c> or <c>TARGETOBJECT::FIELDNAME</c></remarks>
        protected override void GenerateFieldReferenceExpression(CodeFieldReferenceExpression e)
        {
            OutputMemberReference(e.TargetObject, e.FieldName, true, null);
        }

        /// <summary>
        /// Generates code for the specified goto statement.
        /// </summary>
        /// <remarks><c>goto LABEL;</c></remarks>
        protected override void GenerateGotoStatement(CodeGotoStatement e)
        {
            Output.Write(SpecialWords.Goto + WhiteSpace.Space);
            Output.Write(e.Label);

            Output.WriteLine(Tokens.Semicolon);
        }

        /// <summary>
        /// Generates code for the specified indexer expression.
        /// </summary>
        /// <remarks><c>TARGET->get_Item(INDEX1, INDEX2)...</c></remarks>
        protected override void GenerateIndexerExpression(CodeIndexerExpression e)
        {
            OutputInvocation(new CodeMethodReferenceExpression(e.TargetObject, SpecialWords.IndexerGet), e.Indices);
            // TODO
            //OutputArrayIndexerExpression(e.TargetObject, e.Indices);
        }

        /// <summary>
        /// Generates code for the specified iteration statement.
        /// </summary>
        /// <remarks><c>INIT; while(TEST) STATEMENTS INCREMENT</c></remarks>
        protected override void GenerateIterationStatement(CodeIterationStatement e)
        {
            // init
            if (e.InitStatement != null)
                GenerateStatement(e.InitStatement);

            // test
            Output.Write(Keywords.While + WhiteSpace.Space + Tokens.ParenthesisLeft);
            if (e.TestExpression != null)
                GenerateExpression(e.TestExpression);
            Output.Write(Tokens.ParenthesisRight);

            int inc_index = -1;
            if (e.IncrementStatement != null)
                inc_index = e.Statements.Add(e.IncrementStatement);

            try
            {
                // statements + increment
                GeneratePhpStatements(e.Statements);
            }
            finally
            {
                if (inc_index >= 0)
                    e.Statements.RemoveAt(inc_index);
            }
        }

        /// <summary>
        /// Generates code for the specified labeled statement.
        /// </summary>
        /// <remarks><c>LABEL: STATEMENT</c> or <c>LABEL:</c> if <param name="e"/>.<see cref="CodeLabeledStatement.Statement">Statement</see> is  null</remarks>
        protected override void GenerateLabeledStatement(CodeLabeledStatement e)
        {
            if (Indent > 0)
            {
                Indent--;
                Output.Write(e.Label);
                Output.WriteLine(Tokens.Colon);
                Indent++;
            }
            else
            {
                Output.Write(e.Label);
                Output.WriteLine(Tokens.Colon);
            }

            if (e.Statement != null) GenerateStatement(e.Statement);
        }

        /// <summary>
        /// Generates code for the specified line pragma end.
        /// </summary>
        /// <remarks><c>#pragma default line #pragma default file</c></remarks>
        protected override void GenerateLinePragmaEnd(CodeLinePragma e)
        {
            if (e is CodeLinePragmaNoWrite) return;
            // wrap if generating the pragma outside <? ?>
            if (!inCompileUnit)
            {
                Output.WriteLine(Tokens.PhpBracketLeft);
                Indent++;
            }

            Output.WriteLine("#pragma default line");
            Output.WriteLine("#pragma default file");

            if (!inCompileUnit)
            {
                Indent--;
                Output.WriteLine(Tokens.PhpBracketRight);
            }
            else Output.WriteLine();
        }

        /// <summary>
        /// Generates code for the specified line pragma start.
        /// </summary>
        /// <remarks><c>#pragma file FILENAME #pragma line LINENUMBER</c></remarks>
        protected override void GenerateLinePragmaStart(CodeLinePragma e)
        {
            if (e is CodeLinePragmaNoWrite) return;
            // wrap if generating the pragma outside <? ?>
            if (!inCompileUnit)
            {
                Output.WriteLine(Tokens.PhpBracketLeft);
                Indent++;
            }
            else Output.WriteLine();

            Output.Write("#pragma file ");
            Output.WriteLine(e.FileName);
            Output.Write("#pragma line ");
            Output.WriteLine(e.LineNumber - 1); // the number applies to the current line!

            if (!inCompileUnit)
            {
                Indent--;
                Output.Write(Tokens.PhpBracketRight);
            }
        }

        /// <summary>
        /// Generates code for the specified method.
        /// </summary>
        /// <remarks><c>public/protected/private abstract/final/static function(PARAMETERS) BODY/;</c></remarks>
        protected override void GenerateMethod(CodeMemberMethod e, CodeTypeDeclaration c)
        {
            OutputAttributes(e.CustomAttributes, false);
            if (e.ReturnTypeCustomAttributes.Count > 0)
                OutputAttributes(e.ReturnTypeCustomAttributes, false, "return:");

            if (!IsCurrentInterface)
            {
                OutputMemberAccessModifier(e.Attributes);
                OutputMemberScopeModifier(e.Attributes);
            }

            // signature
            Output.Write(Keywords.Function + WhiteSpace.Space);
            Output.Write(e.Name);

            OutputTypeParameters(e.TypeParameters);

            Output.Write(Tokens.ParenthesisLeft);
            OutputParameters(e.Parameters);
            Output.Write(Tokens.ParenthesisRight);

            // body
            if (!IsCurrentInterface && ((e.Attributes & MemberAttributes.ScopeMask) != MemberAttributes.Abstract))
            {
                GeneratePhpStatements(e.Statements, true);
            }
            else Output.WriteLine(Tokens.Semicolon);
        }

        /// <summary>
        /// Generates code for the specified method invoke expression.
        /// </summary>
        /// <remarks><c>TARGETOBJECT-&gt;METHODNAME&lt;:TYPEARGS:&gt;(PARAMETERS)</c> or
        /// <c>TYPEREF::METHODNAME&lt;:TYPEARGS:&gt;(PARAMETERS)</c></remarks>
        protected override void GenerateMethodInvokeExpression(CodeMethodInvokeExpression e)
        {
            OutputInvocation(e.Method, e.Parameters);
        }

        /// <summary>
        /// Generates code for the specified method reference expression.
        /// </summary>
        /// <remarks><c>TARGETOBJECT-&gt;METHODNAME&lt;:TYPEARGS:&gt;</c> or
        /// <c>TYPEREF::METHODNAME&lt;:TYPEARGS:&gt;</c></remarks>
        protected override void GenerateMethodReferenceExpression(CodeMethodReferenceExpression e)
        {
            OutputMemberReference(e.TargetObject, e.MethodName, false, e.TypeArguments);
        }

        /// <summary>
        /// Generates code for the specified method return statement.
        /// </summary>
        /// <remarks><c>return EXPRESSION;</c></remarks>
        protected override void GenerateMethodReturnStatement(CodeMethodReturnStatement e)
        {
            Output.Write(Keywords.Return + WhiteSpace.Space);
            GenerateExpression(e.Expression);

            Output.WriteLine(Tokens.Semicolon);
        }

        /// <summary>
        /// Generates code for the specified namespace declaration.
        /// </summary>
        protected override void GenerateNamespace(CodeNamespace e)
        {
            GenerateCommentStatements(e.Comments);
            GenerateNamespaceStart(e);

            // imports are generated at the very beginning of the containing compile unit
            //GenerateNamespaceImports(e);

            GenerateTypes(e);
            GenerateNamespaceEnd(e);
        }

        /// <summary>
        /// Generates code for the end of a namespace.
        /// </summary>
        /// <remarks><c>}</c></remarks>
        protected override void GenerateNamespaceEnd(CodeNamespace e)
        {
            if (!String.IsNullOrEmpty(e.Name))
            {
                Indent--;
                Output.WriteLine(Tokens.BraceRight);
            }

            currentNamespace = null;
            importedNamespaces.Clear();
            getTypeOutputCache.Clear();

            // restore aliases in global namespace
            if (previousImportedNamespaces != null && previousImportedNamespaces.Count > 0)
                importedNamespaces = new Dictionary<string, string>(previousImportedNamespaces);
        }

        /// <summary>
        /// Converts CLR namespace name to some short form used as alias.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        /// <remarks>System.Xml.Linq to SXLinq.</remarks>
        private string FullClrNamespaceToShort(string/*!*/name)
        {
            string[] names = name.Split('.');

            StringBuilder bld = new StringBuilder(8);

            for (int i = 0; i < names.Length - 1; i++)
            {
                bld.Append(names[i][0]);
            }

            bld.Append(names[names.Length - 1]);

            return bld.ToString();
        }

        /// <summary>
        /// Generates code for the specified namespace import.
        /// </summary>
        /// <remarks><c>import namespace NAMESPACE;</c></remarks>
        protected override void GenerateNamespaceImport(CodeNamespaceImport e)
        {
            if (inCompileUnit)
            {
                if (importedNamespaces.ContainsKey(e.Namespace)) return;
            }

            string alias = FullClrNamespaceToShort(e.Namespace);

            // //import namespace <e>;
            //Output.Write(Keywords.Import + WhiteSpace.Space + Keywords.Namespace + WhiteSpace.Space);
            //Output.Write(e.Namespace.Replace(".", Tokens.NamespaceSeparator));
            //Output.WriteLine(Tokens.Semicolon);

            // use <e> as <alias>;
            Output.Write(Keywords.Use);
            Output.Write(e.Namespace.Replace(".", Tokens.NamespaceSeparator));
            Output.Write(WhiteSpace.Space + Keywords.As + WhiteSpace.Space);
            Output.Write(alias);
            Output.WriteLine(Tokens.Semicolon);

            if (inCompileUnit)
            {
                importedNamespaces.Add(e.Namespace, alias);
            }
        }

        /// <summary>
        /// Current namespace, CLR notation
        /// </summary>
        private string currentNamespace = null;

        /// <summary>
        /// Generates code for the start of a namespace.
        /// </summary>
        /// <remarks><c>namespace NAME {</c></remarks>
        protected override void GenerateNamespaceStart(CodeNamespace e)
        {
            // clear importedNamespaces
            previousImportedNamespaces = new Dictionary<string, string>(importedNamespaces);
            importedNamespaces.Clear();
            getTypeOutputCache.Clear();

            currentNamespace = string.IsNullOrEmpty(e.Name) ? null : e.Name;

            if (!String.IsNullOrEmpty(e.Name))
            {
                Output.Write(Keywords.Namespace + WhiteSpace.Space);
                Output.Write(e.Name.Replace(".", Tokens.NamespaceSeparator));

                if (Options.BracingStyle == "C")
                {
                    Output.WriteLine();
                }
                else Output.Write(WhiteSpace.Space);

                Output.WriteLine(Tokens.BraceLeft);
                Indent++;
            }
        }

        /// <summary>
        /// Generates code for the specified object creation expression.
        /// </summary>
        /// <remarks><c>new CREATETYPE(PARAMETERS)</c></remarks>
        protected override void GenerateObjectCreateExpression(CodeObjectCreateExpression e)
        {
            OutputInstantiation(e.CreateType, e.Parameters);
        }

        /// <summary>
        /// Generates code for the specified parameter declaration expression.
        /// </summary>
        /// <remarks><c>[CA]TYPE &amp;$NAME</c></remarks>
        protected override void GenerateParameterDeclarationExpression(CodeParameterDeclarationExpression e)
        {
            int out_attr_index = -1;

            if (e.Direction == FieldDirection.Out)
            {
                out_attr_index = e.CustomAttributes.Add(OutAttribute);
            }

            try
            {
                if (e.CustomAttributes.Count > 0)
                {
                    OutputAttributes(e.CustomAttributes, true);
                    Output.Write(WhiteSpace.Space);
                }
            }
            finally
            {
                if (out_attr_index >= 0) e.CustomAttributes.RemoveAt(out_attr_index);
            }

            // type hint
            OutputType(e.Type);
            Output.Write(WhiteSpace.Space);

            OutputDirection(e.Direction);
            OutputVariable(e.Name);
        }

        /// <summary>
        /// Generates code for the specified property.
        /// </summary>
        /// <remarks>Not supported.</remarks>
        protected override void GenerateProperty(CodeMemberProperty e, CodeTypeDeclaration c)
        {
            GenerateComment(new CodeComment("PROPERTY: " + e.Name));
            //throw new Exception("The method or operation is not implemented.");
        }

        /// <summary>
        /// Generates code for the specified property reference expression.
        /// </summary>
        /// <remarks><c>TARGETOBJECT-&gt;PROPERTYNAME</c> or <c>TARGETOBJECT::PROPERTYNAME</c></remarks>
        protected override void GeneratePropertyReferenceExpression(CodePropertyReferenceExpression e)
        {
            OutputMemberReference(e.TargetObject, e.PropertyName, true, null);
        }

        /// <summary>
        /// Generates code for the specified property set value reference expression.
        /// </summary>
        /// <remarks>Not supported.</remarks>
        protected override void GeneratePropertySetValueReferenceExpression(CodePropertySetValueReferenceExpression e)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        /// <summary>
        /// Generates code for the specified remove event statement.
        /// </summary>
        /// <remarks><c>EVENT->Remove(LISTENER);</c></remarks>
        protected override void GenerateRemoveEventStatement(CodeRemoveEventStatement e)
        {
            GenerateEventReferenceExpression(e.Event);
            Output.Write(Tokens.Arrow);
            Output.Write(SpecialWords.Remove);
            Output.Write(Tokens.ParenthesisLeft);

            GenerateExpression(e.Listener);

            Output.Write(Tokens.ParenthesisRight);
            Output.WriteLine(Tokens.Semicolon);
        }

        /// <summary>
        /// Outputs the code of the specified literal code fragment expression.
        /// </summary>
        /// <remarks><c>VALUE</c></remarks>
        protected override void GenerateSnippetExpression(CodeSnippetExpression e)
        {
            Output.Write(e.Value);
        }

        /// <summary>
        /// Outputs the code of the specified literal code fragment class member.
        /// </summary>
        /// <remarks><c>TEXT</c></remarks>
        protected override void GenerateSnippetMember(CodeSnippetTypeMember e)
        {
            Output.Write(e.Text);
        }

        /// <summary>
        /// Generates code for the specified this reference expression.
        /// </summary>
        /// <remarks><c>$this</c></remarks>
        protected override void GenerateThisReferenceExpression(CodeThisReferenceExpression e)
        {
            OutputVariable(SpecialWords.This);
        }

        /// <summary>
        /// Generates code for the specified throw exception statement.
        /// </summary>
        /// <remarks><c>throw TOTHROW;</c></remarks>
        protected override void GenerateThrowExceptionStatement(CodeThrowExceptionStatement e)
        {
            Output.Write(Keywords.Throw + WhiteSpace.Space);
            GenerateExpression(e.ToThrow);

            Output.WriteLine(Tokens.Semicolon);
        }

        /// <summary>
        /// Generates code for the specified try...catch...finally statement.
        /// </summary>
        /// <remarks><c>try BLOCK catch (TYPE1 $VAR1) BLOCK1 ... FINALLYBLOCK</c></remarks>
        protected override void GenerateTryCatchFinallyStatement(CodeTryCatchFinallyStatement e)
        {
            // try
            Output.Write(Keywords.Try);

            int catch_count = e.CatchClauses.Count;
            GeneratePhpStatements(e.TryStatements, true, (catch_count > 0));

            // catch
            for (int i = 0; i < catch_count; i++)
            {
                CodeCatchClause catch_clause = e.CatchClauses[i];

                Output.WriteLine(Keywords.Catch + WhiteSpace.Space + Tokens.ParenthesisLeft);
                OutputType(catch_clause.CatchExceptionType);

                Output.Write(WhiteSpace.Space);
                OutputVariable(catch_clause.LocalName);
                Output.Write(Tokens.ParenthesisRight);

                GeneratePhpStatements(catch_clause.Statements, true, (i < catch_count - 1));
            }

            // "finally"
            if (e.FinallyStatements != null)
            {
                int finally_count = e.FinallyStatements.Count;

                for (int i = 0; i < finally_count; i++)
                {
                    GenerateStatement(e.FinallyStatements[i]);
                }
            }
        }

        /// <summary>
        /// Generates code for the specified class constructor.
        /// </summary>
        /// <remarks>Not supported.</remarks>
        protected override void GenerateTypeConstructor(CodeTypeConstructor e)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        /// <summary>
        /// Generates code for the specified type of expression.
        /// </summary>
        /// <remarks><c>System:::Type::GetType("BASETYPE")</c></remarks>
        protected override void GenerateTypeOfExpression(CodeTypeOfExpression e)
        {
            /*Output.Write("System:::Type::GetType(\"");
            Output.Write(e.Type.BaseType);
            Output.Write("\")");*/
            Output.Write(Keywords.TypeOf);
            Output.Write(WhiteSpace.Space);
            OutputType(e.Type);//Output.Write(e.Type.BaseType);
            Output.Write(WhiteSpace.Space);
        }

        /// <summary>
        /// Generates code for the specified end class.
        /// </summary>
        /// <remarks><c>}</c></remarks>
        protected override void GenerateTypeEnd(CodeTypeDeclaration e)
        {
            Indent--;
            Output.WriteLine(Tokens.BraceRight);
        }

        /// <summary>
        /// Known global types. Used when outputting a type to resolve it properly and fully qualified.
        /// </summary>
        /// <remarks>
        /// This list is never cleared in this instance!
        /// Cleared automatically when <see cref="PhpCodeProvider"/> creates new instance of <see cref="PhpCodeGenerator"/>.
        /// 
        /// This list caches global types introduced within this instance.
        /// </remarks>
        private List<string>/*!*/globalTypes = new List<string>();

        /// <summary>
        /// Generates code for the specified start class.
        /// </summary>
        /// <remarks><c>partial class/interface NAME extends BASE implements/extends IFACES {</c></remarks>
        protected override void GenerateTypeStart(CodeTypeDeclaration e)
        {
            if (e.IsEnum || e.IsStruct || IsCurrentDelegate)
            {
                throw new Exception("The method or operation is not implemented.");
            }

            // if assembly custom attributes have not been generated yet, do it now
            if (assemblyAttributes != null)
            {
                OutputAttributes(assemblyAttributes, false, SpecialWords.AssemblyAttr);
                assemblyAttributes = null;
            }

            int attr_index = e.CustomAttributes.Add(ExportAttribute);
            try
            {
                OutputAttributes(e.CustomAttributes, false);
            }
            finally
            {
                e.CustomAttributes.RemoveAt(attr_index);
            }

            OutputTypeModifiers(e);
            Output.Write(e.Name);

            OutputTypeParameters(e.TypeParameters);

            // remember declared type (this won't be declared in any referenced assembly, but we need to be able to resolve it)
            string fullName = string.IsNullOrEmpty(currentNamespace) ? e.Name : (currentNamespace + "." + e.Name);
            if (!globalTypes.Contains(fullName))
                globalTypes.Add(fullName);

            int base_count = e.BaseTypes.Count;
            int iface_offset = 0;

            classHasParent = false;
            if (e.IsClass && base_count > 0)
            {
                // base class should be the first item of the BaseTypes collection
                CodeTypeReference parent = e.BaseTypes[0];

                if (!typeIsInterface(parent))
                {
                    if (parent.BaseType != "Object" && parent.BaseType != "System.Object")
                    {
                        // extends parent
                        Output.Write(WhiteSpace.Space + Keywords.Extends + WhiteSpace.Space);
                        OutputType(parent);

                        classHasParent = true;
                    }

                    iface_offset = 1;
                }
            }

            if (base_count > iface_offset)
            {
                // implements/extends interfaces
                Output.Write(WhiteSpace.Space);

                if (e.IsClass) Output.Write(Keywords.Implements);
                else Output.Write(Keywords.Extends);

                Output.Write(WhiteSpace.Space);

                OutputType(e.BaseTypes[iface_offset]);
                for (int i = iface_offset + 1; i < base_count; i++)
                {
                    Output.Write(Tokens.Comma + WhiteSpace.Space);
                    OutputType(e.BaseTypes[i]);
                }
            }

            if (Options.BracingStyle == "C")
            {
                Output.WriteLine();
            }
            else Output.Write(WhiteSpace.Space);

            Output.WriteLine(Tokens.BraceLeft);

            Indent++;
        }

        /// <summary>
        /// Generates code for the specified variable declaration statement.
        /// </summary>
        /// <remarks><c>$NAME = INITEXPRESSION;</c></remarks>
        protected override void GenerateVariableDeclarationStatement(CodeVariableDeclarationStatement e)
        {
            if (e.InitExpression != null)
            {
                OutputVariable(e.Name);
                Output.Write(WhiteSpace.Space + Tokens.Assignment + WhiteSpace.Space);

                GenerateExpression(e.InitExpression);
                Output.WriteLine(Tokens.Semicolon);
            }
        }

        /// <summary>
        /// Generates code for the specified variable reference expression.
        /// </summary>
        /// <remarks><c>$VARIABLENAME</c></remarks>
        protected override void GenerateVariableReferenceExpression(CodeVariableReferenceExpression e)
        {
            OutputVariable(e.VariableName);
        }

        /// <summary>
        /// Generates code for the given statements.
        /// </summary>
        /// <param name="statements">The statements.</param>
        /// <param name="forceBlock"><B>True</B> to force braced block even if the number of statements is less than
        /// two, <B>false</B> otherwise.</param>
        /// <param name="elseClosing"><B>True</B> to suppress generating newline if
        /// <see cref="CodeGeneratorOptions.ElseOnClosing"/> is set.</param>
        /// <remarks><c>STATEMENT</c> or <c>{ STATEMENTS }</c></remarks>
        private void GeneratePhpStatements(CodeStatementCollection/*!*/ statements, bool forceBlock, bool elseClosing)
        {
            int count = statements.Count;

            if (!forceBlock && count < 2)
            {
                Output.WriteLine();

                Indent++;

                if (count == 0) Output.WriteLine(Tokens.Semicolon);
                else GenerateStatement(statements[0]);

                Indent--;
            }
            else
            {
                if (Options.BracingStyle == "C")
                {
                    Output.WriteLine();
                }
                else Output.Write(WhiteSpace.Space);

                Output.WriteLine(Tokens.BraceLeft);
                Indent++;

                for (int i = 0; i < count; i++)
                {
                    GenerateStatement(statements[i]);
                }

                Indent--;
                Output.Write(Tokens.BraceRight);

                // check whether we should go to the new line or not
                if (elseClosing && Options.ElseOnClosing)
                {
                    Output.Write(WhiteSpace.Space);
                }
                else Output.WriteLine();
            }
        }

        /// <summary>
        /// Generates code for the given statements.
        /// </summary>
        /// <remarks><c>STATEMENT</c> or <c>{ STATEMENTS }</c></remarks>
        private void GeneratePhpStatements(CodeStatementCollection/*!*/ statements, bool forceBlock)
        {
            GeneratePhpStatements(statements, forceBlock, false);
        }

        /// <summary>
        /// Generates code for the given statements.
        /// </summary>
        /// <remarks><c>STATEMENT</c> or <c>{ STATEMENTS }</c></remarks>
        private void GeneratePhpStatements(CodeStatementCollection/*!*/ statements)
        {
            GeneratePhpStatements(statements, false, false);
        }

        #endregion

        #region Generation: Output*

        /// <summary>
        /// Outputs a variable reference.
        /// </summary>
        /// <remarks><c>$NAME</c></remarks>
        private void OutputVariable(string name)
        {
            Output.Write(Tokens.Dollar);
            Output.Write(name);
        }

        /// <summary>
        /// Outputs member access modifier.
        /// </summary>
        /// <remarks><c>public/protected/private</c></remarks>
        protected override void OutputMemberAccessModifier(MemberAttributes attributes)
        {
            // no modifiers allowed for constants
            if ((attributes & MemberAttributes.Const) != MemberAttributes.Const)
            {
                // treat internals and protected internals as publics
                if ((attributes & MemberAttributes.Private) == MemberAttributes.Private)
                {
                    Output.Write(Keywords.Private + WhiteSpace.Space);
                }
                else if ((attributes & (MemberAttributes.Family | MemberAttributes.Assembly)) == MemberAttributes.Family)
                {
                    Output.Write(Keywords.Protected + WhiteSpace.Space);
                }
                else Output.Write(Keywords.Public + WhiteSpace.Space);
            }
        }

        /// <summary>
        /// Outputs field scope modifier.
        /// </summary>
        /// <remarks><c>const/static</c></remarks>
        protected override void OutputFieldScopeModifier(MemberAttributes attributes)
        {
            if ((attributes & MemberAttributes.Const) == MemberAttributes.Const)
            {
                Output.Write(Keywords.Const + WhiteSpace.Space);
            }
            else if ((attributes & MemberAttributes.Static) == MemberAttributes.Static)
            {
                Output.Write(Keywords.Static + WhiteSpace.Space);
            }
        }

        /// <summary>
        /// Outputs member (method in particular) scope modifier.
        /// </summary>
        /// <remarks><c>abstract/final static</c></remarks>
        protected override void OutputMemberScopeModifier(MemberAttributes attributes)
        {
            switch (attributes & MemberAttributes.Static)
            {
                case MemberAttributes.Abstract:
                    {
                        Output.Write(Keywords.Abstract + WhiteSpace.Space);
                        break;
                    }

                case MemberAttributes.Final:
                    {
                        Output.Write(Keywords.Final + WhiteSpace.Space);
                        break;
                    }

                case MemberAttributes.Static:
                    {
                        Output.Write(Keywords.Static + WhiteSpace.Space);
                        return;
                    }
            }

            // static may be combined with abstract or final -> use New to indicate Static
            if ((attributes & MemberAttributes.New) == MemberAttributes.New)
            {
                Output.Write(Keywords.Static + WhiteSpace.Space);
            }
        }

        /// <summary>
        /// Outputs type (class and interface) modifiers.
        /// </summary>
        /// <remarks><c>partial class/interface</c></remarks>
        private void OutputTypeModifiers(CodeTypeDeclaration e)
        {
            Debug.Assert(!e.IsStruct && !e.IsEnum);

            if (e.IsPartial) Output.Write(Keywords.Partial + WhiteSpace.Space);

            if (e.IsClass) Output.Write(Keywords.Class + WhiteSpace.Space);
            // TODO: Keywords.Trait
            else if (e.IsInterface) Output.Write(Keywords.Interface + WhiteSpace.Space);
            else throw new NotSupportedException();
        }

        /// <summary>
        /// Outputs field direction (used for formal parameters).
        /// </summary>
        /// <remarks><c>&amp;</c></remarks>
        protected override void OutputDirection(FieldDirection dir)
        {
            if (dir != FieldDirection.In) Output.Write(Tokens.Reference);
        }

        /// <summary>
        /// Outputs reference to an instance or static member.
        /// </summary>
        /// <param name="target">An expression denoting the instance or type (if <B>null</B>, <c>$this</c> is assumed).
        /// </param>
        /// <param name="name">The member name.</param>
        /// <param name="variable"><B>True</B> if the member is a variable (i.e. property), <b>false</b> otherwise.</param>
        /// <param name="typeArgs">Optional type arguments if this is a method reference.</param>
        private void OutputMemberReference(CodeExpression target, string name, bool variable, CodeTypeReferenceCollection typeArgs)
        {
            CodeTypeReferenceExpression type_ref;
            CodeBaseReferenceExpression base_ref = null;

            if ((type_ref = target as CodeTypeReferenceExpression) != null ||
                (base_ref = target as CodeBaseReferenceExpression) != null)
            {
                // "static" reference
                if (type_ref != null) OutputType(type_ref.Type);
                else GenerateBaseReferenceExpression(base_ref);

                Output.Write(Tokens.DoubleColon);

                if (variable)
                {
                    if (type_ref != null && IsFieldConstant(type_ref, name)) Output.Write(name);
                    else OutputVariable(name);
                }
                else Output.Write(name);
            }
            else
            {
                // instance reference
                if (target == null)
                {
                    // let's suppose this is $this->NAME (might also be self::NAME but that cannot be
                    // decided according to the provided information
                    OutputVariable(SpecialWords.This);
                }
                else GenerateExpression(target);

                Output.Write(Tokens.Arrow);
                Output.Write(name);
            }

            Output.Write(GetTypeArgumentsOutput(typeArgs));
        }

        /// <summary>
        /// Outputs an array indexer expression, i.e. a target followed by brackets.
        /// </summary>
        /// <remarks><c>TARGET[INDEX1]...</c></remarks>
        private void OutputArrayIndexerExpression(CodeExpression target, CodeExpressionCollection indices)
        {
            GenerateExpression(target);

            for (int i = 0; i < indices.Count; i++)
            {
                Output.Write(Tokens.BracketLeft);
                GenerateExpression(indices[i]);
                Output.Write(Tokens.BracketRight);
            }
        }

        /// <summary>
        /// Outputs type parameter declaration (part of generic type and generic method declaration).
        /// </summary>
        /// <remarks></remarks>
        private void OutputTypeParameters(CodeTypeParameterCollection typeParams)
        {
            int count;
            if (typeParams != null && (count = typeParams.Count) > 0)
            {
                Output.Write(Tokens.GenericBracketLeft);

                Output.Write(typeParams[0].Name);
                for (int i = 1; i < count; i++)
                {
                    Output.Write(Tokens.Comma + WhiteSpace.Space);
                    Output.Write(typeParams[i].Name);
                }

                Output.Write(Tokens.GenericBracketRight);
            }
        }

        /// <summary>
        /// Outputs type instantiation.
        /// </summary>
        /// <remarks><c>new TYPE(ARGUMENTS)</c></remarks>
        private void OutputInstantiation(CodeTypeReference type, CodeExpressionCollection arguments)
        {
            Output.Write(Keywords.New + WhiteSpace.Space);

            OutputType(type);
            OutputActualArguments(arguments);
        }

        /// <summary>
        /// Outputs method invocation.
        /// </summary>
        /// <remarks><c>METHOD(ARGUMENTS)</c></remarks>
        private void OutputInvocation(CodeMethodReferenceExpression method, CodeExpressionCollection arguments)
        {
            OutputMemberReference(method.TargetObject, method.MethodName, false, method.TypeArguments);
            OutputActualArguments(arguments);
        }

        /// <summary>
        /// Outputs actual arguments including the parentheses.
        /// </summary>
        /// <remarks><c>(ARGUMENTS)</c></remarks>
        private void OutputActualArguments(CodeExpressionCollection arguments)
        {
            Output.Write(Tokens.ParenthesisLeft);

            int count = arguments.Count;
            if (count > 0)
            {
                GenerateExpression(arguments[0]);
                for (int i = 1; i < count; i++)
                {
                    Output.Write(Tokens.Comma + WhiteSpace.Space);
                    GenerateExpression(arguments[i]);
                }
            }

            Output.Write(Tokens.ParenthesisRight);
        }

        /// <summary>
        /// Outputs custom attribute declarations.
        /// </summary>
        /// <remarks><c>[ATTR1(PARAMETERS1), ATTR2(PARAMETERS2), ...]</c></remarks>
        protected override void OutputAttributeDeclarations(CodeAttributeDeclarationCollection attributes)
        {
            OutputAttributes(attributes, false, String.Empty);
        }

        /// <summary>
        /// Outputs a custom attribute argument (named or unnamed).
        /// </summary>
        /// <remarks><c>VALUE/$NAME = VALUE</c></remarks>
        protected override void OutputAttributeArgument(CodeAttributeArgument arg)
        {
            if (!String.IsNullOrEmpty(arg.Name))
            {
                // named argument
                OutputVariable(arg.Name);
                Output.Write(WhiteSpace.Space + Tokens.DoubleArrow + WhiteSpace.Space);
            }

            GenerateExpression(arg.Value);
        }

        /// <summary>
        /// Outputs custom attributes.
        /// </summary>
        /// <remarks><c>[ATTR1(PARAMETERS1), ATTR2(PARAMETERS2), ...]</c></remarks>
        private void OutputAttributes(CodeAttributeDeclarationCollection/*!*/ attributes, bool inLine)
        {
            OutputAttributes(attributes, inLine, String.Empty);
        }

        /// <summary>
        /// Outputs custom attributes.
        /// </summary>
        /// <param name="attributes">The attributes to output.</param>
        /// <param name="inLine"><B>True</B> to output the attributes inline (e.g. parameter attrs.),
        /// <B>false</B> to output the parameters to a separate line.</param>
        /// <param name="prefix">The prefix to use (e.g. <c>return:</c>).</param>
        /// <remarks><c>[PREFIX ATTR1(PARAMETERS1), ATTR2(PARAMETERS2), ...]</c></remarks>
        private void OutputAttributes(CodeAttributeDeclarationCollection/*!*/ attributes, bool inLine,
            string/*!*/ prefix)
        {
            int count = attributes.Count;

            if (count > 0)
            {
                GenerateAttributeDeclarationsStart(attributes);
                Output.Write(prefix);

                // iterate over attributes
                for (int i = 0; i < count; i++)
                {
                    if (i > 0) Output.Write(Tokens.Comma + WhiteSpace.Space);

                    CodeAttributeDeclaration attr = attributes[i];

                    OutputType(attr.AttributeType);

                    int arg_count = attr.Arguments.Count;

                    if (arg_count > 0)
                    {
                        Output.Write(Tokens.ParenthesisLeft);

                        // iretate over attribute arguments
                        OutputAttributeArgument(attr.Arguments[0]);
                        for (int j = 1; j < arg_count; j++)
                        {
                            Output.Write(Tokens.Comma + WhiteSpace.Space);
                            OutputAttributeArgument(attr.Arguments[j]);
                        }

                        Output.Write(Tokens.ParenthesisRight);
                    }
                }

                GenerateAttributeDeclarationsEnd(attributes);
                if (!inLine) Output.WriteLine();
            }
        }

        #endregion

        #region Helper methods

        /// <summary>
        /// Tries to determine whether the supplied <see cref="CodeTypeReferenceExpression"/> - name pair denotes
        /// a property (to be generated with the <B>$</B> prefix) or a constant.
        /// </summary>
        private bool IsFieldConstant(CodeTypeReferenceExpression/*!*/ typeRef, string/*!*/ fieldName)
        {
            string type_name = typeRef.Type.BaseType;
            string field_key = type_name + "::" + fieldName;

            bool result;
            if (isFieldConstantCache.TryGetValue(field_key, out result)) return result;

            if (lastAssembly != null)
            {
                // first off, look into the last assembly where we had a hit
                if (TryIsFieldConstant(lastAssembly, type_name, fieldName, out result))
                {
                    isFieldConstantCache.Add(field_key, result);
                    return result;
                }
            }

            // look into all loaded assemblies
            foreach (Assembly ass in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (TryIsFieldConstant(ass, type_name, fieldName, out result))
                {
                    lastAssembly = ass;

                    isFieldConstantCache.Add(field_key, result);
                    return result;
                }
            }

            isFieldConstantCache.Add(field_key, false);
            return false;
        }

        /// <summary>
        /// Tries to determine whether there is a given literal field in the specified assembly.
        /// </summary>
        private bool TryIsFieldConstant(Assembly ass, string typeName, string fieldName, out bool isConstant)
        {
            try
            {
                Type type = ass.GetType(typeName);
                if (type != null)
                {
                    FieldInfo info = type.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                    if (info != null && info.IsLiteral)
                    {
                        isConstant = true;
                        return true;
                    }
                }
            }
            catch (Exception)
            { }

            isConstant = false;
            return false;
        }

        #endregion

        #region Debug

#if DEBUG
        public bool Debug_IsValidIdentifier(string value)
        {
            return IsValidIdentifier(value);
        }

        public string Debug_CreateEscapedIdentifier(string value)
        {
            return CreateEscapedIdentifier(value);
        }

        public string Debug_QuoteSnippetString(string value)
        {
            return QuoteSnippetString(value);
        }
        
#endif

        #endregion
    }

    #region Debug

#if DEBUG
    public class Debug_PhpCodeGenerator
    {
        private PhpCodeGenerator gen = new PhpCodeGenerator();

        public bool Debug_IsValidIdentifier(string value)
        {
            return gen.Debug_IsValidIdentifier(value);
        }

        public string Debug_CreateEscapedIdentifier(string value)
        {
            return gen.Debug_CreateEscapedIdentifier(value);
        }

        public string Debug_QuoteSnippetString(string value)
        {
            return gen.Debug_QuoteSnippetString(value);
        }
    }
#endif

    #endregion
}
