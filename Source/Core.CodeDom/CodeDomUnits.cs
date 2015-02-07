/*

 Copyright (c) 2006 Tomas Matousek.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/
using System;
using System.Collections.Generic;
using System.Text;
using PHP.Core.Parsers;
using PHP.Core.Emit;
using System.Reflection.Emit;
using System.Reflection;
using System.Diagnostics.SymbolStore;
using System.IO;
using PHP.Core.CodeDom;
using PHP.Core.Reflection;

namespace PHP.Core.Reflection {
    /// <summary>
    /// Support class for CodeDOM implementation. 
    /// Provides methods for parsing the PHP code into AST.
    /// </summary>
    /// <remarks>Used to parse isolated text containin PHP code. So, neither module nor assembly is known.</remarks>
    public class CodeDomCompilationUnit:CompilationUnitBase, IReductionsSink {
        #region Construction

        private readonly bool isPure;

        /// <summary>
        /// Creates new compilation unit for parsing PHP code
        /// </summary>
        /// <param name="isPure">Is the unit parsing PURE code?</param>
        public CodeDomCompilationUnit(bool isPure) {
            this.isPure = isPure;
            this.module = new CodeDomModule(this);
        }

        #endregion

        #region Base Class Implementations

        public override bool IsTransient { get { return false; } }
        public override bool IsPure { get { return isPure; } }

        public override DType GetVisibleType(QualifiedName qualifiedName, ref string fullName, Scope currentScope, bool mustResolve) {
            throw new Exception("The method or operation is not implemented.");
        }

        public override DRoutine GetVisibleFunction(QualifiedName qualifiedName, ref string fullName, Scope currentScope) {
            throw new Exception("The method or operation is not implemented.");
        }

        public override DConstant GetVisibleConstant(QualifiedName qualifiedName, ref string fullName, Scope currentScope) {
            throw new Exception("The method or operation is not implemented.");
        }

        public override IEnumerable<PhpType> GetDeclaredTypes() {
            throw new Exception("The method or operation is not implemented.");
        }

        public override IEnumerable<PhpFunction> GetDeclaredFunctions() {
            throw new Exception("The method or operation is not implemented.");
        }

        public override IEnumerable<GlobalConstant> GetDeclaredConstants() {
            throw new Exception("The method or operation is not implemented.");
        }

        #endregion

        #region IReductionsSink Members

        public void InclusionReduced(Parser parser, PHP.Core.AST.IncludingEx decl) {
        }

        public void FunctionDeclarationReduced(Parser parser, PHP.Core.AST.FunctionDecl decl) {
        }

        public void TypeDeclarationReduced(Parser parser, PHP.Core.AST.TypeDecl decl) {
        }

        public void GlobalConstantDeclarationReduced(Parser parser, PHP.Core.AST.GlobalConstantDecl decl) {
        }

        #endregion

        #region Implementation

        /// <summary>
        /// Implements trivial error sink that throws exception with the first error
        /// </summary>
        class ParserErrorSink:ErrorSink {
            protected override bool Add(int id, string message,
                ErrorSeverity severity, int group, string fullPath, ErrorPosition pos) {
                if(severity.IsFatal)
                    PhpException.Throw(PhpError.CompileError, "Parsing failed: " + message);
                return true;
            }
        }


        /// <summary>
        /// Parse PHP code and return its AST.
        /// </summary>
        /// <param name="code">The PHP code to be parsed.</param>
        /// <param name="encoding">Encoding of the source code.</param>
        /// <param name="lang">Language features that may change parser behavior.</param>
        /// <param name="file">PHP Source file with the file name &amp; location</param>
        /// <returns>Returns the parsed AST node.</returns>
        public AST.GlobalCode ParseString(string code, Encoding encoding, PhpSourceFile file, LanguageFeatures lang) {
            PhpScriptSourceUnit srcUnit = new PhpScriptSourceUnit
                (this, code, file, encoding, 0, 0);

            using(StringReader reader = new StringReader(code)) {
                Parser parser = new Parser();
                return parser.Parse(srcUnit, reader, new ParserErrorSink(), this,
                    Position.Initial, Lexer.LexicalStates.INITIAL, lang);
            }
        }

        #endregion
    }
}
//TODO: Comments
namespace PHP.Core.CodeDom{
    internal class CodeDomModule:PhpModule{
        public CodeDomModule(CodeDomCompilationUnit cu) : base(cu, new CodeDomAssembly(null)) { ((CodeDomAssembly)base.Assembly).Module = this; }

        protected override CompilationUnitBase CreateCompilationUnit() {
            throw new Exception("The method or operation is not implemented.");
        }

        public override void Reflect(bool full, Dictionary<string, DTypeDesc> types, Dictionary<string, DRoutineDesc> functions, DualDictionary<string, DConstantDesc> constants) {
            throw new Exception("The method or operation is not implemented.");
        }
    }
    internal class CodeDomAssembly:PhpAssembly {
    /// <param name="module">Can be null, but then property <see cref="Module"/> must be initialized later.</param>
    public CodeDomAssembly(CodeDomModule module) : base(new ApplicationContext(false,false,false)) { this.module = module;}
        internal CodeDomModule Module{
            get{return module;}
            set{
                if(module != null) throw new InvalidOperationException("Module can be set only if it is null");
                module=value;
        }}
        private CodeDomModule module;
        public override PhpModule GetModule(PhpSourceFile name) {
            return module;
        }
    }
}