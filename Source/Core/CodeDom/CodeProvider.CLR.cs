/*

 Copyright (c) 2006 Ladislav Prosek.  

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.IO;
using System.Text;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;

using System.ComponentModel;

namespace PHP.Core.CodeDom {
    public class PhpCodeProvider:CodeDomProvider {
        #region Fields

        private PhpCodeGenerator codeGenerator = new PhpCodeGenerator();
        private PhpCodeCompiler codeCompiler = new PhpCodeCompiler();
        /// <summary>Referenced assemblies</summary>
        private List<string> references=new List<string>();

        #endregion

        #region Properties

        /// <summary>
        /// Gets the default file name extension to use for PHP source code files.
        /// </summary>
        public override string FileExtension {
            get { return "php"; }
        }

        /// <summary>
        /// Gets a language features identifier.
        /// </summary>
        public override LanguageOptions LanguageOptions {
            get { return LanguageOptions.CaseInsensitive; }
        }

        #endregion

        public void AddReference(string assemblyName) {
            references.Add(assemblyName);
        }

        /// <summary>
        /// Returns a PHP code compiler.
        /// </summary>
        [Obsolete("Callers should not use the ICodeCompiler interface and should " +
            "instead use the methods directly on the CodeDomProvider class.")]
        public override ICodeCompiler CreateCompiler() {
            return codeCompiler;
        }

        /// <summary>
        /// Returns a PHP code generator.
        /// </summary>
        [Obsolete("Callers should not use the ICodeGenerator interface and should " +
            "instead use the methods directly on the CodeDomProvider class.")]
        public override ICodeGenerator CreateGenerator() {
            return codeGenerator;
        }

        /// <summary>
        /// Generates code for the specified Code Document Object Model (CodeDOM) member declaration and sends it to the
        /// specified text writer, using the specified options.
        /// </summary>
        public override void GenerateCodeFromMember(CodeTypeMember member, TextWriter writer, CodeGeneratorOptions options) {
            codeGenerator.GenerateCodeFromMember(member, writer, options);
        }

        /// <summary>
        /// Gets a <see cref="TypeConverter"/> for the specified data type.
        /// </summary>
        public override TypeConverter GetConverter(Type type) {
            if(type == typeof(MemberAttributes)) {
                return PhpMemberAttributeConverter.Default;
            }
            if(type == typeof(System.Reflection.TypeAttributes)) {
                return PhpTypeAttributeConverter.Default;
            }
            return base.GetConverter(type);
        }

        /// <summary> 
        /// Returns a PHP code parser. 
        /// </summary> 
        [Obsolete("Callers should not use the ICodeCompiler interface and should instead use the methods directly on the CodeDomProvider class.")]
        public override ICodeParser CreateParser() {
            PhpCodeDomParser ret = new PhpCodeDomParser(references);
            ret.DelegateMode = PhpCodeDomParser.DelegateModes.AllPossible;
            return ret;
        }

        /*
        private class FakeParser : CodeParser  {
            public override CodeCompileUnit Parse(TextReader codeStream) {
                CodeCompileUnit tree = new CodeCompileUnit();
                tree.Namespaces.Add (new CodeNamespace("WindowsApplication1"));
                tree.Namespaces[0].Types.Add(new CodeTypeDeclaration ("Form1"));
                tree.Namespaces[0].Types[0].IsClass=true;
                tree.Namespaces[0].Types[0].BaseTypes.Add(System.Type.GetType("System.Windows.Forms.Form"));
                tree.Namespaces[0].Types[0].Members.Add(new CodeMemberField(/*System.Type.GetType("System.ComponentModel.IContainer")* /typeof(object),"components"));
                CodeConstructor CTor = new CodeConstructor();
                CTor.Statements.Add(new CodeMethodInvokeExpression(new CodeThisReferenceExpression(), "InitializeComponent", new CodeExpression[] { }));
                CTor.Attributes = MemberAttributes.Public;
                tree.Namespaces[0].Types[0].Members.Add(CTor);
                CodeMemberMethod InitializeComponent = new CodeMemberMethod();
                tree.Namespaces[0].Types[0].Members.Add(InitializeComponent);
                InitializeComponent.Name = "InitializeComponent";
                InitializeComponent.Statements.Add(new CodeVariableDeclarationStatement(/*System.Type.GetType("System.Windows.Forms.Button")* /typeof(object),"Button1"));
                InitializeComponent.Statements.Add(new CodeAssignStatement (new CodeVariableReferenceExpression ("Button1"),new CodeObjectCreateExpression(System.Type.GetType("System.Windows.Forms.Button"),new CodeExpression[]{})));
                InitializeComponent.Statements.Add(new CodeMethodInvokeExpression(new CodeThisReferenceExpression(), "SuspendLayout"));
                InitializeComponent.Statements.Add(new CodeCommentStatement(""));
                InitializeComponent.Statements.Add(new CodeCommentStatement("Button1"));
                InitializeComponent.Statements.Add(new CodeCommentStatement(""));
                InitializeComponent.Statements.Add(new CodeAssignStatement(new CodePropertyReferenceExpression(new CodeVariableReferenceExpression("Button1"), "Location"), new CodeObjectCreateExpression(System.Type.GetType("System.Drawing.Point"), new CodeExpression[] { new CodePrimitiveExpression((int)12), new CodePrimitiveExpression((int)12) })));
                InitializeComponent.Statements.Add(new CodeAssignStatement(new CodePropertyReferenceExpression(new CodeVariableReferenceExpression("Button1"), "Name"), new CodePrimitiveExpression("Button1")));
                InitializeComponent.Statements.Add(new CodeAssignStatement(new CodePropertyReferenceExpression(new CodeVariableReferenceExpression("Button1"), "Size"), new CodeObjectCreateExpression(System.Type.GetType("System.Drawing.Size"), new CodeExpression[] { new CodePrimitiveExpression((int)96), new CodePrimitiveExpression((int)23) })));
                InitializeComponent.Statements.Add(new CodeAssignStatement(new CodePropertyReferenceExpression(new CodeVariableReferenceExpression("Button1"), "TabIndex"), new CodePrimitiveExpression((int)0)));
                InitializeComponent.Statements.Add(new CodeAssignStatement(new CodePropertyReferenceExpression(new CodeVariableReferenceExpression("Button1"), "Text"), new CodePrimitiveExpression("I'm PHP button!")));
                InitializeComponent.Statements.Add(new CodeAssignStatement(new CodePropertyReferenceExpression(new CodeVariableReferenceExpression("Button1"), "UseVisualStyleBackColor"), new CodePrimitiveExpression(true)));
                InitializeComponent.Statements.Add(new CodeMethodInvokeExpression(new CodeThisReferenceExpression(), "SuspendLayout"));
                InitializeComponent.Statements.Add(new CodeCommentStatement(""));
                InitializeComponent.Statements.Add(new CodeCommentStatement("Form1"));
                InitializeComponent.Statements.Add(new CodeCommentStatement(""));
                InitializeComponent.Statements.Add(new CodeAssignStatement(new CodePropertyReferenceExpression(new CodeThisReferenceExpression (), "AutoScaleDimensions"), new CodeObjectCreateExpression(System.Type.GetType("System.Drawing.SizeF"), new CodeExpression[] { new CodePrimitiveExpression((System.Single )6.0), new CodePrimitiveExpression((System.Single )13.0) })));
                InitializeComponent.Statements.Add(new CodeAssignStatement(new CodePropertyReferenceExpression(new CodeThisReferenceExpression(), "AutoScaleMode"), new CodeFieldReferenceExpression(new CodeTypeReferenceExpression  (System.Type.GetType("System.Windows.Forms.AutoScaleMode")), "Font")));
                InitializeComponent.Statements.Add(new CodeAssignStatement(new CodePropertyReferenceExpression(new CodeThisReferenceExpression(), "ClientSize"), new CodeObjectCreateExpression(System.Type.GetType("System.Drawing.Size"), new CodeExpression[] { new CodePrimitiveExpression((int)284), new CodePrimitiveExpression((int)264) })));
                InitializeComponent.Statements.Add(new CodeMethodInvokeExpression(new CodePropertyReferenceExpression(new CodeThisReferenceExpression(), "Controls"), "Add", new CodeExpression[] { new CodeVariableReferenceExpression("Button1") }));
                InitializeComponent.Statements.Add(new CodeAssignStatement(new CodePropertyReferenceExpression(new CodeThisReferenceExpression(), "Name"), new CodePrimitiveExpression("Form1")));
                InitializeComponent.Statements.Add(new CodeAssignStatement(new CodePropertyReferenceExpression(new CodeThisReferenceExpression(), "Text"), new CodePrimitiveExpression("PHP form")));
                InitializeComponent.Statements.Add(new CodeMethodInvokeExpression(new CodeThisReferenceExpression(), "ResumeLayout", new CodeExpression[] {new CodePrimitiveExpression(true) }));
                return tree; 
            }
        }*/
    }
}
