using System;
using System.Collections.Generic;
using System.Text;
using PHP.Core.Reflection;
using PHP.Core;
using PHP.Core.AST;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.IO;
using System.Reflection;


namespace PHP.Core.CodeDom
{

    /// <summary>Implements <see cref="ICodeParser"/> for PHP language</summary>
    /// <remarks>The purpose of this class is to translate code tree representation that is used by PHP internally to .NET's CodeDOM.</remarks>
    public class PhpCodeDomParser : CodeParser
    {
        private List<string> /*!*/ references = new List<string>();
        public PhpCodeDomParser() : this(null) { }
        public PhpCodeDomParser(List<string> references)
        {
            if (references == null || references.Count == 0)
            {
                this.references.AddRange(new string[]{
                    "System.Windows.Forms",
                    "System",
                    "System.Data",
                    "System.Drawing",
                    "System.Xml",
                    "mscorlib",
                    "PhpNetCore",
                    "PhpNetClassLibrary"});
            }
            else
            {
                this.references.AddRange(references);
            }
        }
        /// <summary>Compiles the specified text stream into a <see cref="System.CodeDom.CodeCompileUnit"/>.</summary>
        /// <param name="codeStream">A <see cref="System.IO.TextReader"/> that is used to read the code to be parsed.</param>
        /// <returns>A <see cref="System.CodeDom.CodeCompileUnit"/> containing the code model produced from parsing the code.</returns>
        public override CodeCompileUnit Parse(TextReader/*!*/ codeStream)
        {
            PhpCodeDomParserImplementation impl = new PhpCodeDomParserImplementation(EventMode, DelegateMode, this);
            return impl.Parse(codeStream.ReadToEnd());
        }
        /// <summary>Mode of translating delegate-like constructs</summary>
        /// <remarks>PHP creates delegate using sth. like new System:::EventHandler(array($this, "mtd"))</remarks>
        public DelegateModes DelegateMode { get { return delegateMode; } set { delegateMode = value; } }
        /// <summary>Containd value of the <see cref="DelegateMode"/> property</summary>
        private DelegateModes delegateMode = DelegateModes.ByType;
        /// <summary>Possible way of recognizing delegates</summary>
        public enum DelegateModes
        {
            /// <summary>Try to find type (only in already compiled DLLs). If type being constructed is delegate, translate as creation of delegate.</summary>
            ByType,
            /// <summary>All constructors that are feed by CTor non-associative array with 2 elements, where 2nd is string literal, ale treated as ctors of delegate.</summary>
            AllPossible,
            /// <summary>No delegate CTors are produced</summary>
            None
        }
        /// <summary>Mode of translating event-attach/detach-like constructs</summary>
        /// <remarks>PHP attaches/detaches events using sth. like $instance->Event-Add(delegate) resp. $instance->Event-Remove(delegate)</remarks>
        public EventModes EventMode { get { return eventMode; } set { eventMode = value; } }
        /// <summary>Contains value of the <see cref="EventMode"/> property.</summary>
        private EventModes eventMode = EventModes.WithDelegateOnly;
        /// <summary>Possible ways of recognizing events</summary>
        public enum EventModes
        {
            /// <summary>Only when argument is creation of delegate</summary>
            WithDelegateOnly,
            /// <summary>All statements in format (somethink)->Name->Add(something) (or Remove)</summary>
            AllPossible,
            /// <summary>No event attach/remove statements are produced</summary>
            None
        }

        /// <summary>Implements translation for <see cref="PhpCodeDomParser"/></summary>
        protected class PhpCodeDomParserImplementation
        {
            /// <summary>Mode of translating event-attach/detach-like constructs</summary>
            /// <remarks>PHP attaches/detaches events using sth. like $instance->Event-Add(delegate) resp. $instance->Event-Remove(delegate)</remarks>
            public EventModes EventMode { get { return eventMode; } /*set { eventMode = value; }*/ }
            /// <summary>Contains value of the <see cref="EventMode"/> property.</summary>
            private EventModes eventMode = EventModes.WithDelegateOnly;
            /// <summary>Mode of translating delegate-like constructs</summary>
            /// <remarks>PHP creates delegate using sth. like new System:::EventHandler(array($this, "mtd"))</remarks>
            public DelegateModes DelegateMode { get { return delegateMode; } /*set { delegateMode = value; }*/ }
            /// <summary>Containd value of the <see cref="DelegateMode"/> property</summary>
            private DelegateModes delegateMode = DelegateModes.ByType;
            /// <summary>Owner of this instance</summary>
            private PhpCodeDomParser /*!*/ owner;
            /// <summary>CTor</summary>
            /// <param name="eventMode">Mode of translating event-attach/detach-like constructs</param>
            /// <param name="delegateMode">Mode of translating delegate-like constructs</param>
            /// <param name="owner">Instance of <see cref="PhpCodeDomParser"/> that created this instance</param>
            public PhpCodeDomParserImplementation(EventModes eventMode, DelegateModes delegateMode, PhpCodeDomParser /*!*/ owner)
            {
                if (owner == null) throw new ArgumentNullException("owner");
                this.eventMode = eventMode;
                this.delegateMode = delegateMode;
                this.owner = owner;
            }
            #region Helpers
            /// <summary>Guess if <see cref="NewEx"/> creates new delegate or not</summary>
            /// <param name="New">Expression to make guess for</param>
            /// <returns>Booleand indicating if expression should be treated as delegate c¨reation depending on <see cref="DelegateMode"/>.</returns>
            protected bool LooksLikeDelegate(NewEx New)
            {
                switch (DelegateMode)
                {
                    case DelegateModes.AllPossible:
                        return CanBeDelegateConstruction(New);
                    case DelegateModes.ByType:
                        Type t = GetType(TranslateTypeRef(New.ClassNameRef, null));
                        if (t == null) return false;
                        return t.IsSubclassOf(typeof(System.Delegate)) || t.Equals(typeof(Delegate));
                    default: return false;
                }
            }
            /// <summary>Tries to get <see cref="Type"/> from <see cref="CodeTypeReference"/></summary>
            /// <param name="t">A <see cref="CodeTypeReference"/></param>
            /// <returns><see cref="Type"/> if found or null.</returns>
            protected Type GetType(CodeTypeReference t)
            {
                return Helper.GetType(t, /*CurrentBlockAliases*/null/*J:aliases processed by parser already*/, imports.ToArray(), owner.references);
            }

            /// <summary>Returns value indicating if <see cref="NewEx"/> can syntactically be creation of delegate</summary>
            /// <param name="New">Expression to makde decission for</param>
            /// <returns>
            /// True for <see cref="NewEx">NewEx</see>s with one argument which is
            /// <see cref="ArrayEx"/> with 2 non-associative (without index) elements (no rereferce elements)
            /// and 2nd element's value is <see cref="StringLiteral"/>.
            /// </returns>
            private bool CanBeDelegateConstruction(NewEx New)
            {
                return
                    New.CallSignature.Parameters.Count == 1 &&
                    New.CallSignature.Parameters[0].Expression is ArrayEx &&
                    ((ArrayEx)New.CallSignature.Parameters[0].Expression).Items.Count == 2 &&
                    ((ArrayEx)New.CallSignature.Parameters[0].Expression).Items[0].Index == null &&
                    ((ArrayEx)New.CallSignature.Parameters[0].Expression).Items[1].Index == null &&
                    ((ArrayEx)New.CallSignature.Parameters[0].Expression).Items[0] is ValueItem &&
                    ((ArrayEx)New.CallSignature.Parameters[0].Expression).Items[1] is ValueItem &&
                    ((ValueItem)((ArrayEx)New.CallSignature.Parameters[0].Expression).Items[1]).ValueExpr is StringLiteral;
            }
            /// <summary>Stores name of file currently being translated</summary>
            private string currentFile;
            /// <summary>Stores name of class currently being translated</summary>
            private string currentClass;
            /// <summary>Stores fully qualified name of method currently beigng translated</summary>
            private string currentMethod;
            /// <summary>Stores name of method currently being translated</summary>
            private string currentFunction;
            /// <summary>Stores name of namespace currently being translated</summary>
            private string currentNamespace;
            /// <summary>Gets expression that gets current context fot PHP script</summary>
            protected static CodePropertyReferenceExpression CurrentContext
            {
                get
                {
                    return new CodePropertyReferenceExpression(
                        new CodeTypeReferenceExpression(typeof(ScriptContext)), "CurrentContext");
                }
            }
            /// <summary>Converts anything to <see cref="IStatementInsertContext"/> if it is implemented by that 'anything'</summary>
            /// <param name="obj">Object to convert</param>
            /// <returns><paramref name="obj"/> if it implements <see cref="IStatementInsertContext"/>; null otherwise</returns>
            protected static IStatementInsertContext getIC(object /*!*/ obj)
            {
                return obj is IStatementInsertContext ? (IStatementInsertContext)obj : null;
            }
            /// <summary>Converts anything to <see cref="ICodeBlockContext"/> if it is implemented by that 'anything'</summary>
            /// <param name="obj">Object to convert</param>
            /// <returns><paramref name="obj"/> if it implements <see cref="ICodeBlockContext"/>; null otherwise</returns>
            protected static ICodeBlockContext getICodeBlockContext(object/*!*/obj)
            {
                return obj is ICodeBlockContext ? (ICodeBlockContext)obj : null;
            }
            /// <summary>Helper for translation for block-level statements. Performs type conversion from <see cref="MethodContextBase"/> to <see cref="MethodContext"/> and from <see cref="IBlockContext"/> to <see cref="ICodeBlockContext"/></summary>
            /// <param name="methodQ">Something that should be <see cref="MethodContext"/></param>
            /// <param name="blockQ">Something that may be <see cref="BlockStatementContext"/></param>
            /// <param name="method">Returns <paramref name="methodQ"/></param>
            /// <param name="block">Returns <paramref name="blockQ"/> if <paramref name="blockQ"/> is <see cref="ICodeBlockContext"/>, otherwise null</param>
            /// <param name="errorElement">The element to throw <see cref="PhpToCodeDomNotSupportedException"/> on</param>
            /// <exception cref="PhpToCodeDomNotSupportedException"><paramref name="methodQ"/> is not <see cref="MethodContext"/></exception>
            private static void MethodAndBlock(MethodContextBase methodQ, IBlockContext blockQ, ref  MethodContext method, ref  ICodeBlockContext block, LangElement /*!*/ errorElement)
            {
                if (methodQ is MethodContext) method = (MethodContext)methodQ; else throw new PhpToCodeDomNotSupportedException("GetUserEntryPoint context is required to be method for block-level statements", errorElement);
                block = blockQ as ICodeBlockContext;// is ICodeBlockContext ? (ICodeBlockContext)blockQ : null;
            }
            /// <summary>Counter for <see cref="SwitchVarName"/></summary>
            private static int __SwitchVarName = 0;
            /// <summary>Gets name for variable to switch by (used when translating the switch statement)</summary>
            private static string SwitchVarName { get { return string.Format("__switchVar__{0:000}", __SwitchVarName++); } }
            /// <summary>Types of PHP loops</summary>
            private enum Loops
            {
                /// <summary>switch</summary>
                Switch,
                /// <summary>for</summary>
                For,
                /// <summary>foreach</summary>
                Foreach,
                /// <summary>do, while</summary>
                While
            }
            /// <summary>Counter of start labels for <see cref="LabelName"/></summary>
            private static Dictionary<Loops, int> __Labels1 = new Dictionary<Loops, int>();
            /// <summary>Counter of end labels for <see cref="LabelName"/></summary>
            private static Dictionary<Loops, int> __Labels2 = new Dictionary<Loops, int>();
            /// <summary>Initializer</summary>
            static PhpCodeDomParserImplementation()
            {
                __Labels1.Add(Loops.For, 0);
                __Labels1.Add(Loops.Foreach, 0);
                __Labels1.Add(Loops.Switch, 0);
                __Labels1.Add(Loops.While, 0);
                __Labels2.Add(Loops.For, 0);
                __Labels2.Add(Loops.Foreach, 0);
                __Labels2.Add(Loops.Switch, 0);
                __Labels2.Add(Loops.While, 0);
            }
            /// <summary>Gets unique name of label for loop</summary>
            /// <param name="type">Type of loop to get label for</param>
            /// <param name="start">True if this is label of start of loop; otherwise false</param>
            /// <returns>Name of label</returns>
            private static string /*!*/ LabelName(Loops type, bool start)
            {
                return string.Format("__{0}__label__{1:F}__{2:000}", start ? "Start" : "End", type, (start ? __Labels1 : __Labels2)[type]++);
            }
            /// <summary>Gets value indicationg if given <see cref="DirectVarUse"/> refers to property or field</summary>
            /// <param name="use"><see cref="DirectVarUse"/> to make resolution for</param>
            /// <param name="method">GetUserEntryPoint used for resolving context</param>
            /// <returns>True if <paramref name="use"/> reffres to property, false if it reffers to field or resolution cannot be done</returns>
            private bool IsProperty(DirectVarUse /*!*/ use, MethodContextBase /*!*/ method)
            {
                if (use.IsMemberOf == null) return false;
                Type SearchIn = null;
                if (use.IsMemberOf != null && use.IsMemberOf is DirectVarUse && ((DirectVarUse)use.IsMemberOf).VarName.Value == "this")
                {
                    if (/*Member is defined in current implementation*/
                        method is MethodContext && ((MethodContext)method).Class != null &&
                        ((MethodContext)method).Class.getPropertyOrField(((DirectVarUse)use).VarName.Value) != null
                    )
                    {
                        CodeTypeMember member = ((MethodContext)method).Class.getPropertyOrField(((DirectVarUse)use).VarName.Value);
                        if (member is CodeMemberField) SearchIn = GetType(((CodeMemberField)member).Type);
                        else if (member is CodeMemberProperty) SearchIn = GetType(((CodeMemberProperty)member).Type);
                    }
                    else/*Member is inherited*/
                    {
                        if (method is MethodContext && ((MethodContext)method).Class != null &&
                            ((MethodContext)method).Class.Context.BaseTypes.Count > 0
                        )
                            SearchIn = GetType(((MethodContext)method).Class.Context.BaseTypes[0]);//Types are translated in such way that first is base class (extends) and then interfaces (implements)
                    }
                }
                else
                {
                    SearchIn = GetType(use.IsMemberOf, method);
                }
                if (SearchIn != null)
                    try
                    {
                        return SearchIn.GetProperty(use.VarName.Value, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) != null;
                    }
                    catch (AmbiguousMatchException)
                    {
                        return true;//Better to do this without try-catch, but this is the easy way (see TableLeyoutPanel - it causes this ex. to be thrown.)
                    }
                return false;
            }
            /// <summary>Tryes to get type of <see cref="VarLikeConstructUse"/></summary>
            /// <param name="use"><see cref="VarLikeConstructUse"/> to get type of</param>
            /// <param name="method">GetUserEntryPoint used for resolving context</param>
            /// <returns>Type of value returned by given member. Type of <see cref="System.Object"/> if type cannot be infered.</returns>
            private Type /*!*/ GetType(VarLikeConstructUse /*!*/ use, MethodContextBase /*!*/ method)
            {
                if (use is ItemUse)
                {
                    Type Type = GetType(((ItemUse)use).Array, method);
                    if (Type.IsArray) return Type.GetElementType();
                    return typeof(object);
                }
                Type OwnerType = null;//type of part at left from ->
                if (use.IsMemberOf != null && use.IsMemberOf is DirectVarUse && ((DirectVarUse)use.IsMemberOf).VarName.Value == "this")
                {
                    //$this->use must be handled in special way 
                    if (method is MethodContext && ((MethodContext)method).Class != null)
                    {
                        //$this must be used in class (otherwise it is treated as variable)
                        TypeContext Class = ((MethodContext)method).Class;
                        if (use is DirectVarUse && Class.getPropertyOrField(((DirectVarUse)use).VarName.Value) != null)
                        {
                            //$this->use is declared in derived class (in PHP)
                            CodeTypeMember member = Class.getPropertyOrField(((DirectVarUse)use).VarName.Value);
                            if (member is CodeMemberField) OwnerType = GetType(((CodeMemberField)member).Type);
                            else if (member is CodeMemberProperty) OwnerType = GetType(((CodeMemberProperty)member).Type);
                        }
                        else if (Class.Context.BaseTypes.Count > 0)
                        {
                            //$this is declare din base class (compiled)
                            string UseName = null;
                            if (use is DirectFcnCall) UseName = ((DirectFcnCall)use).QualifiedName.Name.Value;
                            else if (use is DirectVarUse) UseName = ((DirectVarUse)use).VarName.Value;
                            if (UseName != null)
                            {//Must be able to get name of $this->use
                                Type BaseType = GetType(Class.Context.BaseTypes[0]);
                                MemberInfo[] Members = BaseType.GetMember(UseName);
                                if (Members.Length == 1)
                                {//I do not want to deal with overloaded functions (by now)
                                    if (Members[0] is FieldInfo) OwnerType = ((FieldInfo)Members[0]).FieldType;
                                    else if (Members[0] is PropertyInfo) OwnerType = ((PropertyInfo)Members[0]).PropertyType;
                                    else if (Members[0] is MethodInfo) OwnerType = ((MethodInfo)Members[0]).ReturnType;
                                }
                            }
                        }
                        if (OwnerType == null) return typeof(object);
                        return OwnerType;//By resolving $this->Var I've resolved 2 levels at once
                    }
                }
                else if (use.IsMemberOf != null)
                {//Recursive
                    OwnerType = GetType(use.IsMemberOf, method);
                }
                //Now I know type of part at left of -> (OwnerType)
                //If there is any ->
                //Or I know that I do not know ;-) (OwnerType == null)
                //One think is left - static members
                string MemberName = null;
                if (use is DirectStMtdCall)
                {
                    MemberName = ((DirectStMtdCall)use).MethodName.Value;
                    OwnerType = GetType(TranslateGenericQualifiedName(((DirectStMtdCall)use).ClassName, true));
                }
                else if (use is DirectStFldUse)
                {
                    MemberName = ((DirectStFldUse)use).PropertyName.Value;
                    OwnerType = GetType(TranslateGenericQualifiedName(((DirectStFldUse)use).TypeName, true));
                }

                if (use is DirectFcnCall)
                { // ->fce()
                    if (OwnerType == null) return typeof(object);
                    MethodInfo mtd = OwnerType.GetMethod(((DirectFcnCall)use).QualifiedName.Name.Value, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (mtd != null) return mtd.ReturnType;
                }
                else if (use is DirectStMtdCall)
                { // ::fce()
                    if (OwnerType == null) return typeof(object);
                    MethodInfo mtd = OwnerType.GetMethod(((DirectStMtdCall)use).MethodName.Value, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                    if (mtd != null) return mtd.ReturnType;
                }
                else if (use is DirectVarUse && use.IsMemberOf == null)
                { // $var
                    if (method.Contains(((DirectVarUse)use).VarName.Value))
                        return GetType(method[((DirectVarUse)use).VarName.Value].Type);
                }
                else if (use is DirectVarUse)
                { // ->var
                    if (OwnerType == null) return typeof(object);
                    PropertyInfo prp = OwnerType.GetProperty(((DirectVarUse)use).VarName.Value, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (prp != null) return prp.PropertyType;
                    FieldInfo fld = OwnerType.GetField(((DirectVarUse)use).VarName.Value, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (fld != null) return fld.FieldType;
                }
                else if (use is DirectStFldUse)
                {// ::$var
                    if (OwnerType == null) return typeof(object);
                    PropertyInfo prp = OwnerType.GetProperty(((DirectStFldUse)use).PropertyName.Value, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                    if (prp != null) return prp.PropertyType;
                    FieldInfo fld = OwnerType.GetField(((DirectStFldUse)use).PropertyName.Value, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                    if (fld != null) return fld.FieldType;
                }
                return typeof(object);
            }
            /// <summary>Gets value indicationg if given <see cref="DirectStFldUse"/> refers to property or field</summary>
            /// <param name="use"><see cref="DirectStFldUse"/> to make resolution for</param>
            /// <returns>True if <paramref name="use"/> reffres to property, false if it reffers to field or resolution cannot be done</returns>
            private bool IsProperty(DirectStFldUse /*!*/ use)
            {
                Type type = GetType(TranslateGenericQualifiedName(use.TypeName, true));
                if (type == null) return false;
                return type.GetProperty(use.PropertyName.Value, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic) != null;
            }
            /// <summary>Compares two <see cref="CodeTypeReference">CodeTypeReferences</see></summary>
            /// <param name="T1">A <see cref="CodeTypeReference"/></param>
            /// <param name="T2">A <see cref="CodeTypeReference"/></param>
            /// <returns>true if both arguments represents the same type (or both are null); false otherwise</returns>
            private static bool TypeEquals(CodeTypeReference T1, CodeTypeReference T2)
            {
                if ((T1 == null && T2 != null) || (T2 == null && T1 != null)) return false;
                if (T1 == null && T2 == null) return true;
                if (
                    T1.BaseType == T2.BaseType &&
                    T2.ArrayRank == T1.ArrayRank &&
                    T1.TypeArguments.Count == T2.TypeArguments.Count
                )
                {
                    if (T1.ArrayRank > 0 && !TypeEquals(T1.ArrayElementType, T2.ArrayElementType)) return false;
                    for (int i = 0; i < T1.TypeArguments.Count; i++)
                    {
                        if (!TypeEquals(T1.TypeArguments[i], T2.TypeArguments[i])) return false;
                    }
                    return true;
                }
                else return false;
            }
            /// <summary>Gets <see cref="CodeLinePragma"/> for given line</summary>
            /// <param name="Line">Line number (0-based)</param>
            /// <returns><see cref="CodeLinePragmaNoWrite"/> with <see cref="CodeLinePragma.FileName"/> set to <see cref="currentFile"/></returns>
            private CodeLinePragma getPragma(int Line)
            {
                return new CodeLinePragmaNoWrite(currentFile, Line);
            }
            #endregion
            /// <summary>Compiles the specified string into a <see cref="System.CodeDom.CodeCompileUnit"/>.</summary>
            /// <param name="codeText">A <see cref="System.String"/> that contains code to be parsed.</param>
            /// <returns>A <see cref="System.CodeDom.CodeCompileUnit"/> containing the code model produced from parsing the code.</returns>
            public CodeCompileUnit Parse(String/*!*/ codeText)
            {
                currentFile = "C:\\";
                PhpSourceFile fl = new PhpSourceFile(new FullPath("C:\\"), new FullPath("C:\\"));//TODO: Is there any way how to obtain paths???
                CodeDomCompilationUnit cu = new CodeDomCompilationUnit(true);//TODO:IsPure or Not???
                GlobalCode gc = cu.ParseString(codeText, Encoding.UTF8, fl, LanguageFeatures.PhpClr);//TODO: Language features
                return Translate(gc);
            }
            #region High level translations
            /// <summary>Translates tree of PHP code into .NET's CodeDOM tree representation</summary>
            /// <param name="gc">Parsed PHP code</param>
            /// <returns><paramref name="gc"/> translated into <see cref="CodeCompileUnit"/></returns>
            /// <exception cref="PhpToCodeDomNotSupportedException">PHP construct that is not (currently) supported for translation into CodeDOM has been reached.</exception>
            protected CodeCompileUnit Translate(GlobalCode/*!*/ gc)
            {
                /*CodeCompileUnit ret = new CodeCompileUnit();
                ret.Namespaces.Add(new CodeNamespace());
                //import namespace System:::Windows:::Forms;
                ret.Namespaces[0].Imports.Add(new CodeNamespaceImport("System.Windows.Forms"));
                //import namespace System:::ComponentModel;
                ret.Namespaces[0].Imports.Add(new CodeNamespaceImport("System.ComponentModel"));
                //import namespace System:::Drawing;
                ret.Namespaces[0].Imports.Add(new CodeNamespaceImport("System.Drawing"));
                //import namespace TestApp1;
                ret.Namespaces[0].Imports.Add(new CodeNamespaceImport("TestApp1"));
                //namespace TestApp1 {
                ret.Namespaces.Add(new CodeNamespace("TestApp1"));
                //class Form1
                ret.Namespaces[1].Types.Add(new CodeTypeDeclaration("Form1"));
                ret.Namespaces[1].Types[0].Attributes = MemberAttributes.Public;
                //extends System:::Windows:::Forms:::Form
                ret.Namespaces[1].Types[0].BaseTypes.Add(new CodeTypeReference("System.Windows.Forms.Form"));
                //private $button1;
                ret.Namespaces[1].Types[0].Members.Add(new CodeMemberField(new CodeTypeReference(typeof(System.Object)), "button1") { Attributes = MemberAttributes.Private });
                //public function __construct()
                ret.Namespaces[1].Types[0].Members.Add(new CodeTypeConstructor() { Attributes = MemberAttributes.Public });
                //$this->InitializeComponent();
                ((CodeTypeConstructor)ret.Namespaces[1].Types[0].Members[1]).Statements.Add(new CodeExpressionStatement(new CodeMethodInvokeExpression(new CodeMethodReferenceExpression(new CodeThisReferenceExpression(),"InitializeComponent"),new CodeExpression[]{})));
                //public function InitializeComponent()
                ret.Namespaces[1].Types[0].Members.Add(new CodeMemberMethod() { Name = "InitializeComponent", Attributes = MemberAttributes.Public, ReturnType = new CodeTypeReference(typeof(System.Object)) });
                ((CodeMemberMethod)ret.Namespaces[1].Types[0].Members[2]).Statements.AddRange(new CodeStatement[]{
                    //$this->button1 = new System:::Windows:::Forms:::Button()
                    new CodeAssignStatement(
                        new CodeFieldReferenceExpression(new CodeThisReferenceExpression(),"button1"),
                        new CodeObjectCreateExpression("System.Windows.Forms.Button",new CodeExpression[]{})
                    ),
                    //$this->SuspendLayout();
                    new CodeExpressionStatement(new CodeMethodInvokeExpression(new CodeThisReferenceExpression(),"SuspendLayout",new CodeExpression[]{})),
                    //$this->button1->Location = new System:::Drawing:::Point(96, 109);
                    new CodeAssignStatement(
                        new CodePropertyReferenceExpression(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(),"button1"),"Location"),
                        new CodeObjectCreateExpression("System.Drawing.Point",new CodeExpression[]{new CodePrimitiveExpression((int)96),new CodePrimitiveExpression((int)109)})
                    ),
                    //$this->button1->Name = "button1";
                    new CodeAssignStatement(
                        new CodePropertyReferenceExpression(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(),"button1"),"Name"),
                        new CodePrimitiveExpression("button1")
                    ),
                    // $this->button1->Size = new System:::Drawing:::Size(100, 55);
                    new CodeAssignStatement(
                        new CodePropertyReferenceExpression(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(),"button1"),"Size"),
                        new CodeObjectCreateExpression("System.Drawing.Size",new CodeExpression[]{new CodePrimitiveExpression((int)100),new CodePrimitiveExpression((int)55)})
                    ),
                    //$this->button1->TabIndex = 1;
                    new CodeAssignStatement(
                        new CodePropertyReferenceExpression(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(),"buton1"),"TabIndex"),
                        new CodePrimitiveExpression((int)1)
                    ),
                    //$this->button1->Text = "button1";
                    new CodeAssignStatement(
                        new CodePropertyReferenceExpression(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(),"button1"),"Text"),
                        new CodePrimitiveExpression("button1")
                    ),
                    //$this->button1->UseVisualStyleBackColor = true;
                    new CodeAssignStatement(
                        new CodePropertyReferenceExpression(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(),"button1"),"UseVisualStyleBackColor"),
                        new CodePrimitiveExpression(true)
                    ),
                    //$this->ClientSize = new System:::Drawing:::Size(292, 273);
                    new CodeAssignStatement(
                        new CodePropertyReferenceExpression(new CodeThisReferenceExpression(),"ClientSize"),
                        new CodeObjectCreateExpression("System.Drawing.Size",new CodeExpression[]{
                            new CodePrimitiveExpression((int)292),new CodePrimitiveExpression((int)273)})
                    ),
                    // $this->Controls->Add($this->button1);
                    new CodeExpressionStatement(
                        new CodeMethodInvokeExpression(new CodePropertyReferenceExpression(new CodeThisReferenceExpression(),"Controls"),"Add",
                            new CodeExpression[]{new CodeFieldReferenceExpression(new CodeThisReferenceExpression(),"button1")})
                    ),
                    //$this->Name = "Form1";
                    new CodeAssignStatement(
                        new CodePropertyReferenceExpression(new CodeThisReferenceExpression(),"Name"),
                        new CodePrimitiveExpression("Form1")
                    ),
                    //$this->Text = "Form1";
                    new CodeAssignStatement(
                        new CodePropertyReferenceExpression(new CodeThisReferenceExpression(),"Text"),
                        new CodePrimitiveExpression("Hi, I'm Form1")
                    ),
                    //$this->ResumeLayout(false);
                    new CodeExpressionStatement(
                        new CodeMethodInvokeExpression(
                            new CodeThisReferenceExpression(),"ResumeLayout",
                            new CodeExpression[]{new CodePrimitiveExpression(false)})
                    )
                });
                return ret;*/
                this.aliases.Clear();
                this.imports.Clear();
                CodeCompileUnit ret = new CodeCompileUnit();
                CodeNamespace DefaultNamespace = new CodeNamespace();

                PushAliases(gc.SourceUnit.Aliases);
                ret.Namespaces.Add(DefaultNamespace);
                

                if (gc.SourceUnit.HasImportedNamespaces)
                    foreach (QualifiedName Namespace in gc.SourceUnit.ImportedNamespaces)
                    {
                        DefaultNamespace.Imports.Add(new CodeNamespaceImport(getCLRName(Namespace)));
                        imports.Add(getCLRName(Namespace));
                    }

                TranslateBlock(gc.Statements, new MethodContextBase(), new FileContext(ret));

                PopAliases();
                return ret;
            }

            #region Aliases valid for current block

            private Dictionary<string, string> CurrentBlockAliases { get { return (aliases.Count > 0) ? aliases.Peek() : null; } }
            private Stack<Dictionary<string, string>>/*!*/aliases = new Stack<Dictionary<string, string>>();
            private void PushAliases(Dictionary<string, QualifiedName>/*!*/aliases)
            {
                Debug.Assert(aliases != null);
                Dictionary<string, string> clrAliases = new Dictionary<string, string>(aliases.Count);
                foreach (var pair in aliases)
                    clrAliases.Add(pair.Key, pair.Value.ToClrNotation(0, 0));

                this.aliases.Push(clrAliases);
            }
            private void PopAliases()
            {
                Debug.Assert(this.aliases.Count > 0);
                this.aliases.Pop();
            }

            #endregion

            /// <summary>Contains value of the <see cref="Imports"/> property</summary>
            private readonly List<string> imports = new List<string>();
            /// <summary>List of currently imported namespaces</summary>
            protected List<string> Imports { get { return imports; } }
            /// <summary>Translates sequence of PHP statements into sequence of CodeDOM objects</summary>
            /// <param name="statements">Statements to translate</param>
            /// <param name="method">GetUserEntryPoint context in which the statements are placed</param>
            /// <param name="block">Block context in which the statemenst are placed</param>
            protected void TranslateBlock(IEnumerable<Statement> /*!*/ statements, MethodContextBase /*!*/ method, /*!*/ IBlockContext block)
            {
                foreach (Statement statement in statements)
                {
                    if (statement is BlockStmt) TranslateStatement((BlockStmt)statement, method, block);
                    else if (statement is EchoStmt) TranslateStatement((EchoStmt)statement, method, block);
                    else if (statement is EmptyStmt) TranslateStatement((EmptyStmt)statement, method, block);
                    else if (statement is ExpressionStmt) TranslateStatement((ExpressionStmt)statement, method, block);
                    else if (statement is ForeachStmt) TranslateStatement((ForeachStmt)statement, method, block);
                    else if (statement is ForStmt) TranslateStatement((ForStmt)statement, method, block);
                    else if (statement is FunctionDecl) throw new PhpToCodeDomNotSupportedException(Localizations.Strings.cdp_unsup_global_method_func, statement);
                    else if (statement is GlobalConstDeclList) throw new PhpToCodeDomNotSupportedException(Localizations.Strings.cdp_unsup_global_constants, statement);
                    else if (statement is GlobalStmt) throw new PhpToCodeDomNotSupportedException(Localizations.Strings.cdp_unsup_global_statement, statement);
                    else if (statement is GotoStmt) TranslateStatement((GotoStmt)statement, method, block);
                    else if (statement is IfStmt) TranslateStatement((IfStmt)statement, method, block);
                    else if (statement is JumpStmt) TranslateStatement((JumpStmt)statement, method, block);
                    else if (statement is LabelStmt) TranslateStatement((LabelStmt)statement, method, block);
                    else if (statement is NamespaceDecl) TranslateNamespace((NamespaceDecl)statement, block);
                    else if (statement is StaticStmt) TranslateStatement((StaticStmt)statement, method, block);
                    else if (statement is SwitchStmt) TranslateStatement((SwitchStmt)statement, method, block);
                    else if (statement is ThrowStmt) TranslateStatement((ThrowStmt)statement, method, block);
                    else if (statement is TryStmt) TranslateStatement((TryStmt)statement, method, block);
                    else if (statement is TypeDecl) TranslateTypeDecl((TypeDecl)statement, block);
                    else if (statement is UnsetStmt) TranslateStatement((UnsetStmt)statement, method, block);
                    else if (statement is WhileStmt) TranslateStatement((WhileStmt)statement, method, block);
                    else throw new PhpToCodeDomNotSupportedException(string.Format(Localizations.Strings.cdp_unsup_unknown_statement, statement.GetType().FullName), statement);
                }
            }
            /// <summary>Translates sequence of members of Type into sequence of CodeDOM objects</summary>
            /// <param name="members">Members to translate</param>
            /// <param name="block">Block context in which the statemenst are placed</param>
            /// <exception cref="PhpToCodeDomNotSupportedException">Some block contains unsupported construct (as goes from <see cref="TranslateMemberList(ConstDeclList,IBlockContext)"/>, <see cref="TranslateMemberList(FieldDeclList,IBlockContext)"/>, <see cref="TranslateMethod"/>)</exception>
            protected void TranslateBlock(IEnumerable<TypeMemberDecl> /*!*/ members, IBlockContext /*!*/ block)
            {
                foreach (TypeMemberDecl Member in members)
                {
                    if (Member is ConstDeclList)
                    {
                        TranslateMemberList((ConstDeclList)Member, block);
                    }
                    else if (Member is FieldDeclList)
                    {
                        TranslateMemberList((FieldDeclList)Member, block);
                    }
                    else if (Member is MethodDecl)
                    {
                        TranslateMethod((MethodDecl)Member, block);
                    }
                }
            }
            #endregion
            #region Specialized sub-statement-level translations
            /// <summary>Translates PHP <see cref="GenericQualifiedName"/> into COdeDOM <see cref="CodeTypeReference"/></summary>
            /// <param name="PHPName">PHP type name to be translated</param>
            /// <param name="IntoFull">True to emit fully qualified name; otherwise only last part of name will be emitted</param>
            /// <returns>Representation of <paramref name="PHPName"/> in CodeDOM</returns>
            protected CodeTypeReference /*!*/ TranslateGenericQualifiedName(GenericQualifiedName /*!*/ PHPName, bool IntoFull)
            {
                CodeTypeReference ret = new CodeTypeReference(IntoFull ? getCLRName(PHPName.QualifiedName) : PHPName.QualifiedName.Name.Value);
                foreach (object GParam in PHPName.GenericParams)
                {
                    if (GParam is GenericQualifiedName)
                    {
                        ret.TypeArguments.Add(TranslateGenericQualifiedName((GenericQualifiedName)GParam, true));
                    }
                    else if (GParam is PrimitiveType)
                    {
                        ret.TypeArguments.Add(((PrimitiveType)GParam).RealType);
                    }
                }
                return ret;
            }
            /// <summary>Translates PHP application of custom attribute into CodeDOM one</summary>
            /// <param name="Attribute">Attribute to translate</param>
            /// <returns>CodeDom representation of <paramref name="Attribute"/>. May return null if <paramref name="Attribute"/> should be skipped.</returns>
            /// <remarks>Property/field initialization is ignored</remarks> 
            /// <exception cref="PhpToCodeDomNotSupportedException">Expression used to initialize attribute is either not supported or is not supported in this context (see <see cref="TranslateExpression"/>)</exception>
            protected CodeAttributeDeclaration TranslateAttribute(CustomAttribute /*!*/ Attribute)
            {
                if (Attribute.QualifiedName.Name.Value == "Export") return null;
                CodeAttributeDeclaration ret = new CodeAttributeDeclaration(getCLRName(Attribute.QualifiedName));
                foreach (CodeExpression param in TranslateParams(Attribute.CallSignature.Parameters, new MethodContextBase(), null, null))
                    ret.Arguments.Add(new CodeAttributeArgument(param));
                foreach (NamedActualParam Param in Attribute.NamedParameters)
                    ret.Arguments.Add(new CodeAttributeArgument(Param.Name.Value, TranslateExpression(Param.Expression, new MethodContextBase(), null)));
                return ret;
            }
            /// <summary>Translates "flat" PHP if statement into recursive CodeDOM equivalent (like C# uses)</summary>
            /// <param name="Conditions">List of 'if', 'elseif' and 'else' clausules</param>
            /// <param name="start">Index to <paramref name="Conditions"/> where to start with translation</param>
            /// <param name="Method">GetUserEntryPoint for declaring local variables</param>
            /// <param name="block">Block to add statements to</param>
            /// <exception cref="ArgumentOutOfRangeException"><paramref name="start"/> >= <paramref name="Conditions"/>.<see cref="List&lt;ConditionalStmnt>.Count">Count</see></exception>
            /// <exception cref="ArgumentException"><paramref name="start"/> = <paramref name="Conditions"/>.<see cref="List&lt;ConditionalStmnt>.Count">Count</see> and <see cref="ConditionalStmt.Condition"/> of current condition is null</exception>
            /// <exception cref="PhpToCodeDomNotSupportedException">
            /// Current condition is else condition (has <see cref="ConditionalStmt.Condition"/> null) and it is not the last condition.
            /// =or=
            /// Expression in condition is not supported (as goes from <see cref="TranslateExpression"/>)
            /// </exception>
            private void TranslateConditions(List<ConditionalStmt> Conditions, int start, MethodContext Method, IBlockContext block)
            {
                if (start >= Conditions.Count) throw new ArgumentOutOfRangeException("start", Localizations.Strings.cdp_start_must_be_less_tahn_number_of_conditions);
                if (start == Conditions.Count - 1 && Conditions[start].Condition == null)
                    throw new ArgumentException(Localizations.Strings.cdp_unsup_else_separatelly);
                if (Conditions[start].Condition == null)
                    throw new PhpToCodeDomNotSupportedException(Localizations.Strings.cdp_unsup_else_last, Conditions[start].Statement);
                CodeConditionStatement If = new CodeConditionStatement(TranslateExpression(Conditions[start].Condition, Method, block as IStatementInsertContext));
                If.LinePragma = getPragma(Conditions[start].Condition.Position.FirstLine - 1);
                block.AddObject(If, Conditions[start].Condition);
                IfStatementContext IfContext = new IfStatementContext(Method, block as BlockStatementContext, If, false, this);
                TranslateBlock(new List<Statement>(new Statement[] { Conditions[start].Statement }), Method, IfContext);
                if (start + 2 == Conditions.Count && Conditions[start + 1].Condition == null)
                {//else
                    IfStatementContext ElseContext = new IfStatementContext(Method, block as BlockStatementContext, If, true, this);
                    TranslateBlock(new List<Statement>(new Statement[] { Conditions[start + 1].Statement }), Method, ElseContext);
                }
                else if (start + 1 < Conditions.Count)
                {//'elseif' is translated as 'else if'
                    IfStatementContext ElseContext = new IfStatementContext(Method, block as BlockStatementContext, If, true, this);
                    TranslateConditions(Conditions, start + 1, Method, ElseContext);
                }
            }
            #region Expressions
            /// <summary>Translates PHP expression to CodeDOM expression</summary>
            /// <param name="Expression">Expression to translate</param>
            /// <param name="method">GetUserEntryPoint with declaration of local variables</param>
            /// <returns>CodeDOM representation of <paramref name="Expression"/></returns>
            /// <param name="IC">Context for inserting statements when expression cannot be inlined. (Can be null)</param>
            /// <remarks>Not all expressions can be translated to do exactly same thing</remarks>
            /// <exception cref="PhpToCodeDomNotSupportedException">
            /// <paramref name="Expression"/> is <see cref="RefAssignEx"/>
            /// =or=
            /// <paramref name="Expression"/> is <see cref="EmptyEx"/>
            /// =or=
            /// <paramref name="Expression"/> is <see cref="IncludingEx"/>
            /// =or=
            /// <paramref name="Expression"/> is either <see cref="AST.Linq.LinqExpression"/>, <see cref="AST.Linq.LinqOpChain"/>, <see cref="AST.Linq.LinqTuple"/> or <see cref="AST.Linq.LinqTupleItemAccess"/>
            /// =or=
            /// <paramref name="Expression"/> is of another unknown and thus unsupported type
            /// =or=
            /// Some sub-expression is not supported (as goes from <see cref="TranslateVariableUse"/>, <see cref="TranslateExpression"/>, <see cref="TranslateBinaryOperation"/>, <see cref="TranslateConcatExpression"/>, <see cref="TranslateGenericQualifiedName"/>, <see cref="TranslateTypeRef"/>, <see cref="TranslateList"/>, <see cref="TranslateNew"/>, <see cref="TranslateShellExec"/>, <see cref="TranslateUnaryOperator"/>, <see cref="TranslateVarLikeConstructUse"/>, <see cref="TranslateArray"/>)
            /// </exception>
            protected CodeExpression /*!*/ TranslateExpression(Expression /*!*/ Expression, MethodContextBase/*!*/ method, IStatementInsertContext IC)
            {
                if (Expression is ArrayEx)
                {//array()
                    return TranslateArray((ArrayEx)Expression, method, IC);
                }
                else if (Expression is RefAssignEx)
                {//$target =& $source
                    throw new PhpToCodeDomNotSupportedException(Localizations.Strings.cdp_unsup_ref_assign, Expression);
                }
                else if (Expression is ValueAssignEx)
                {//$target = $source
                    if (Expression.Operation == Operations.AssignValue)
                        return new CodeBinaryOperatorExpression(
                            TranslateVariableUse(((ValueAssignEx)Expression).LValue, method, IC),
                            CodeBinaryOperatorType.Assign,
                            TranslateExpression(((ValueAssignEx)Expression).RValue, method, IC));
                    else
                        return TranslateBinaryOperation(new BinaryEx(Expression.Operation, ((ValueAssignEx)Expression).LValue, ((ValueAssignEx)Expression).RValue), method, IC);
                }
                else if (Expression is BinaryEx)
                {//$left × $right
                    return TranslateBinaryOperation((BinaryEx)Expression, method, IC);
                }
                else if (Expression is ConcatEx)
                {//"string $var string $var"
                    return TranslateConcatExpression((ConcatEx)Expression, method, IC);
                }
                else if (Expression is ConditionalEx)
                {//$cond ? $true : $false
                    return new CodeMethodInvokeExpression(
                        new CodeTypeReferenceExpression(typeof(Helper)), "iif",
                        new CodeExpression[]{
                            TranslateExpression(((ConditionalEx)Expression).CondExpr,method,IC),
                            TranslateExpression(((ConditionalEx)Expression).TrueExpr,method,IC),   
                            TranslateExpression(((ConditionalEx)Expression).FalseExpr,method,IC)});
                }
                else if (Expression is ClassConstUse)
                {//class::const
                    return new CodeFieldReferenceExpression(
                        new CodeTypeReferenceExpression(TranslateGenericQualifiedName(((ClassConstUse)Expression).ClassName, true)),
                        ((ClassConstUse)Expression).Name.Value);
                }
                else if (Expression is GlobalConstUse)
                {//const
                    switch (((GlobalConstUse)Expression).Name.ToString().ToLower())
                    {
                        case "true": return new CodePrimitiveExpression(true);
                        case "false": return new CodePrimitiveExpression(false);
                        case "null": return new CodePrimitiveExpression(null);
                        default: return new CodeMethodInvokeExpression(
                         CurrentContext, "GetConstantValue", new CodeExpression[]{
                            new CodePrimitiveExpression(((GlobalConstUse)Expression).Name.Name.Value)});
                    }
                }
                else if (Expression is EmptyEx)
                {//
                    throw new PhpToCodeDomNotSupportedException(Localizations.Strings.cdp_unsup_empty_ex, Expression);
                }
                else if (Expression is ExitEx)
                {//exit($param)
                    return new CodeMethodInvokeExpression(
                        CurrentContext,
                        "Die",
                        new CodeExpression[]{
                            ((ExitEx)Expression).ResulExpr  == null ? 
                                new CodePrimitiveExpression(null) :
                                TranslateExpression(((ExitEx)Expression).ResulExpr,method,IC)});
                }
                else if (Expression is EvalEx)
                {//eval($code)
                    throw new PhpToCodeDomNotSupportedException(Localizations.Strings.cdp_unsup_eval, Expression);//TODO: Support it somehow!
                }
                else if (Expression is IncDecEx)
                {
                    if (((IncDecEx)Expression).Post)//$a++
                        return new CodeMethodInvokeExpression(
                            new CodeTypeReferenceExpression(typeof(Helper)),
                            "ReturnFirst",
                            new CodeExpression[]{
                                TranslateVariableUse(((IncDecEx)Expression).Variable,method,IC),
                                new CodeBinaryOperatorExpression(
                                    TranslateVariableUse(((IncDecEx)Expression).Variable,method,IC),
                                    CodeBinaryOperatorType.Assign,
                                    new CodeBinaryOperatorExpression(
                                        TranslateVariableUse(((IncDecEx)Expression).Variable,method,IC),
                                        ((IncDecEx)Expression).Inc ? CodeBinaryOperatorType.Add : CodeBinaryOperatorType.Subtract,
                                        new CodePrimitiveExpression(1)))});
                    else//++$a
                        return new CodeBinaryOperatorExpression(
                            TranslateVariableUse(((IncDecEx)Expression).Variable, method, IC),
                            CodeBinaryOperatorType.Assign,
                            new CodeBinaryOperatorExpression(
                                TranslateVariableUse(((IncDecEx)Expression).Variable, method, IC),
                                ((IncDecEx)Expression).Inc ? CodeBinaryOperatorType.Add : CodeBinaryOperatorType.Subtract,
                                new CodePrimitiveExpression(1)));
                }
                else if (Expression is IncludingEx)
                {//include $file
                    throw new PhpToCodeDomNotSupportedException(Localizations.Strings.cdp_unsup_include_require, Expression);
                }
                else if (Expression is InstanceOfEx)
                {//$a instanceof Type 
                    /*return new CodeMethodInvokeExpression(
                        new CodeMethodInvokeExpression(
                            TranslateExpression(((InstanceOfEx)Expression).Expression,method,IC),
                            "GetType",new CodeExpression[]{}),
                        "Equals",
                        new CodeExpression[]{
                            new CodeTypeOfExpression(
                                TranslateTypeRef(((InstanceOfEx)Expression).ClassNameRef))});*/
                    return new CodeMethodInvokeExpression(
                        new CodeTypeOfExpression(TranslateTypeRef(((InstanceOfEx)Expression).ClassNameRef, method)),
                        "IsInstanceOfType",
                        new CodeExpression[] { TranslateExpression(((InstanceOfEx)Expression).Expression, method, IC) });
                }
                else if (Expression is IssetEx)
                {//isset($var)
                    return TranslateIsSet((IssetEx)Expression, method, IC);
                }
                else if (Expression is AST.Linq.LinqExpression)
                {
                    throw new PhpToCodeDomNotSupportedException(Localizations.Strings.cdp_unsup_LINQ, Expression);
                }
                else if (Expression is AST.Linq.LinqOpChain)
                {
                    throw new PhpToCodeDomNotSupportedException(Localizations.Strings.cdp_unsup_LINQ, Expression);
                }
                else if (Expression is AST.Linq.LinqTuple)
                {
                    throw new PhpToCodeDomNotSupportedException(Localizations.Strings.cdp_unsup_LINQ, Expression);
                }
                else if (Expression is AST.Linq.LinqTupleItemAccess)
                {
                    throw new PhpToCodeDomNotSupportedException(Localizations.Strings.cdp_unsup_LINQ, Expression);
                }
                else if (Expression is ListEx)
                {//list($v1,$v2) = $array
                    return TranslateList((ListEx)Expression, method, IC);
                }
                else if (Expression is Literal)
                {
                    return new CodePrimitiveExpression(((Literal)Expression).Value);
                }
                else if (Expression is NewEx)
                {//new Instance()
                    return TranslateNew((NewEx)Expression, method, IC);
                }
                else if (Expression is PseudoConstUse)
                {//__FILE__
                    return Translate__const((PseudoConstUse)Expression);
                }
                else if (Expression is ShellEx)
                {//`$command`
                    return TranslateShellExec((ShellEx)Expression, method, IC);
                }
                else if (Expression is TypeOfEx)
                {
                    return new CodeTypeOfExpression(TranslateTypeRef(((TypeOfEx)Expression).ClassNameRef, method));
                }
                else if (Expression is UnaryEx)
                {//× $a
                    return TranslateUnaryOperator((UnaryEx)Expression, method, IC);
                }
                else if (Expression is VarLikeConstructUse)
                {//$a
                    return TranslateVarLikeConstructUse((VarLikeConstructUse)Expression, method, IC);
                }
                else
                {
                    throw new PhpToCodeDomNotSupportedException(string.Format(Localizations.Strings.cdp_unsup_unsup_ex, Expression.GetType().FullName), Expression);
                }
            }
            #region Sub-expression constructs
            /// <summary>Translates PHP binary operator to CodeDOM binary operator</summary>
            /// <param name="op">Operator to translate (make sure that it is binary operator)</param>
            /// <returns>CodeDOM equivalent of <paramref name="op"/></returns>
            /// <remarks>Translates only binary operators, and only some of them - only thos rhat have equivalent in CodeDOM</remarks>
            /// <param name="element">Element to thrown <see cref="PhpToCodeDomNotSupportedException"/> on</param>
            /// <exception cref="PhpToCodeDomNotSupportedException"><paramref name="op"/> is either not a binary operator or it is not supported by this function</exception>
            protected CodeBinaryOperatorType TranslateBinaryOperator(Operations op, LangElement /*!*/ element)
            {
                switch (op)
                {
                    case Operations.Add: return CodeBinaryOperatorType.Add;
                    case Operations.And: return CodeBinaryOperatorType.BooleanAnd;
                    case Operations.BitAnd: return CodeBinaryOperatorType.BitwiseAnd;
                    case Operations.BitOr: return CodeBinaryOperatorType.BitwiseOr;
                    case Operations.Concat: return CodeBinaryOperatorType.Add;
                    case Operations.Div: return CodeBinaryOperatorType.Divide;
                    case Operations.Equal: return CodeBinaryOperatorType.ValueEquality;
                    case Operations.GreaterThan: return CodeBinaryOperatorType.GreaterThan;
                    case Operations.GreaterThanOrEqual: return CodeBinaryOperatorType.GreaterThanOrEqual;
                    case Operations.Identical: return CodeBinaryOperatorType.IdentityEquality;
                    case Operations.LessThan: return CodeBinaryOperatorType.LessThan;
                    case Operations.LessThanOrEqual: return CodeBinaryOperatorType.LessThanOrEqual;
                    //case Operations.Minus: return CodeBinaryOperatorType.Subtract;
                    case Operations.Mod: return CodeBinaryOperatorType.Modulus;
                    case Operations.Mul: return CodeBinaryOperatorType.Multiply;
                    case Operations.NotIdentical: return CodeBinaryOperatorType.IdentityInequality;
                    case Operations.Or: return CodeBinaryOperatorType.BooleanOr; ;
                    //case Operations.Plus: return CodeBinaryOperatorType.Add;
                    case Operations.Sub: return CodeBinaryOperatorType.Subtract;
                    default: throw new PhpToCodeDomNotSupportedException(string.Format(Localizations.Strings.cdp_unsup_unsup_binop, op, (int)op), element);
                }
            }
            /// <summary>Translates PHP assign shortcut operator to CodeDOM non-assign operator this operator is shortcut to</summary>
            /// <param name="op">Operator to be translated</param>
            /// <returns>Translated operator</returns>
            /// <param name="element">Element to thrown <see cref="PhpToCodeDomNotSupportedException"/> on</param>
            ///<exception cref="PhpToCodeDomNotSupportedException"><paramref name="op"/> is not of supported operators (not all PHP assign shortcuts are supported by this function)</exception>
            protected CodeBinaryOperatorType TranslateAssignOperatorToNonAssign(Operations op, LangElement /*!*/ element)
            {
                switch (op)
                {
                    case Operations.AssignAdd: return CodeBinaryOperatorType.Add;
                    case Operations.AssignAnd: return CodeBinaryOperatorType.BooleanAnd;
                    case Operations.AssignAppend: return CodeBinaryOperatorType.Add;
                    case Operations.AssignDiv: return CodeBinaryOperatorType.Divide;
                    case Operations.AssignMod: return CodeBinaryOperatorType.Modulus;
                    case Operations.AssignMul: return CodeBinaryOperatorType.Multiply;
                    case Operations.AssignOr: return CodeBinaryOperatorType.BitwiseOr;
                    case Operations.AssignSub: return CodeBinaryOperatorType.Subtract;
                    default: throw new PhpToCodeDomNotSupportedException(string.Format(Localizations.Strings.cdp_unsup_unsup_assign, op, (int)op), element);
                }
            }
            #endregion
            #region General expressions
            /// <summary>Translates PHP binary operation into as close as possible equivalent in CodeDOM</summary>
            /// <param name="Operation">Operation to translated</param>
            /// <param name="method">GetUserEntryPoint for declaring local variables</param>
            /// <param name="IC">Context for adding statements</param>
            /// <returns>Operation translated</returns>
            /// <remarks>
            /// Some operations are not translated 1:1.
            /// If operand non-1:1-translated operation has side effect, the side effect can occure multiple times!
            /// Namely: Assign shotrcuts (/=, .=, *=, ...) and boolean Xor.
            /// Some operations are translate as core function calls instead of operators (bitwise xor, shifts)
            /// </remarks>
            protected CodeExpression TranslateBinaryOperation(BinaryEx /*!*/ Operation, MethodContextBase /*!*/ method, IStatementInsertContext IC)
            {
                switch (Operation.Operation)
                {
                    case Operations.AssignAdd:
                    case Operations.AssignAnd:
                    case Operations.AssignAppend:
                    case Operations.AssignDiv:
                    case Operations.AssignMod:
                    case Operations.AssignMul:
                    case Operations.AssignOr:
                    case Operations.AssignSub://Left = (Left § Right)
                        return new CodeBinaryOperatorExpression(
                            TranslateExpression(Operation.LeftExpr, method, IC),
                            CodeBinaryOperatorType.Assign,
                            new CodeBinaryOperatorExpression(
                                TranslateExpression(Operation.LeftExpr, method, IC),
                                TranslateAssignOperatorToNonAssign(Operation.Operation, Operation),
                                TranslateExpression(Operation.RightExpr, method, IC)));
                    //TODO: What does it mean? case Operations.AssignPrepend:
                    case Operations.AssignShiftLeft:
                    case Operations.AssignShiftRight://Left = Operators.AssignShiftX(Left, Right)
                        return new CodeBinaryOperatorExpression(
                            TranslateExpression(Operation.LeftExpr, method, IC),
                            CodeBinaryOperatorType.Assign,
                            new CodeMethodInvokeExpression(
                                new CodeTypeReferenceExpression(typeof(Operators)),
                                Operation.Operation == Operations.AssignShiftLeft ? "ShiftLeft" : "ShiftRight",
                                new CodeExpression[]{
                                    TranslateExpression(Operation.LeftExpr,method,IC),
                                    TranslateExpression(Operation.RightExpr,method,IC)}));
                    case Operations.AssignValue:
                        return new CodeBinaryOperatorExpression(
                            TranslateExpression(Operation.LeftExpr, method, IC),
                            CodeBinaryOperatorType.Assign,
                            TranslateExpression(Operation.RightExpr, method, IC));
                    case Operations.AssignXor://Left = Operators.BitOperation(Left, Right, BitOp.Xor)
                        return new CodeBinaryOperatorExpression(
                            TranslateExpression(Operation.LeftExpr, method, IC),
                            CodeBinaryOperatorType.Assign,
                            new CodeMethodInvokeExpression(
                                new CodeTypeReferenceExpression(typeof(Operators)),
                                "BitOperation",
                                new CodeExpression[]{
                                    TranslateExpression(Operation.LeftExpr,method,IC),
                                    TranslateExpression(Operation.RightExpr,method,IC),
                                    new CodeFieldReferenceExpression(
                                        new CodeTypeReferenceExpression(typeof(Operators.BitOp)),
                                        "Xor")}));
                    case Operations.BitXor:
                        return new CodeMethodInvokeExpression(
                            new CodeTypeReferenceExpression(typeof(Operators)),
                            "BitOperation",
                            new CodeExpression[]{
                                TranslateExpression(Operation.LeftExpr,method,IC),
                                TranslateExpression(Operation.RightExpr,method,IC),
                                new CodeFieldReferenceExpression(
                                    new CodeTypeReferenceExpression(typeof(Operators.BitOp)),
                                    "Xor")});
                    case Operations.NotEqual://(Left == Right) == False
                        return new CodeBinaryOperatorExpression(
                            new CodeBinaryOperatorExpression(
                                TranslateExpression(Operation.LeftExpr, method, IC),
                                CodeBinaryOperatorType.ValueEquality,
                                TranslateExpression(Operation.RightExpr, method, IC)),
                            CodeBinaryOperatorType.ValueEquality,
                            new CodePrimitiveExpression(false));
                    case Operations.ShiftLeft:
                    case Operations.ShiftRight:
                        return new CodeMethodInvokeExpression(
                            new CodeTypeReferenceExpression(typeof(Operators)),
                            Operation.Operation == Operations.ShiftLeft ? "ShiftLeft" : "ShiftRight",
                            new CodeExpression[]{
                                TranslateExpression(Operation.LeftExpr,method,IC),
                                TranslateExpression(Operation.RightExpr,method,IC)});
                    case Operations.Xor://(Left Or Right) And ((Left And Right) == False)
                        return new CodeBinaryOperatorExpression(
                            new CodeBinaryOperatorExpression(
                                TranslateExpression(Operation.LeftExpr, method, IC),
                                CodeBinaryOperatorType.BooleanOr,
                                TranslateExpression(Operation.RightExpr, method, IC)),
                            CodeBinaryOperatorType.BooleanAnd,
                            new CodeBinaryOperatorExpression(
                                new CodeBinaryOperatorExpression(
                                    TranslateExpression(Operation.LeftExpr, method, IC),
                                    CodeBinaryOperatorType.BooleanAnd,
                                    TranslateExpression(Operation.RightExpr, method, IC)),
                                CodeBinaryOperatorType.ValueEquality,
                                new CodePrimitiveExpression(false)));
                    default:
                        return new CodeBinaryOperatorExpression(
                            TranslateExpression(Operation.LeftExpr, method, IC),
                            TranslateBinaryOperator(Operation.Operation, Operation),
                            TranslateExpression(Operation.RightExpr, method, IC));
                }
            }
            /// <summary>Translates unary operator from PHP to CodeDOM</summary>
            /// <param name="op">Operator to translate</param>
            /// <param name="method">GetUserEntryPoint for defining local variables</param>
            /// <param name="IC">Context for adding statements</param>
            /// <returns>Translated expression</returns>
            /// <remarks>Since there is no support for unary operators in CodeDOM, translations are not 1:1.</remarks>
            /// <exception cref="PhpToCodeDomNotSupportedException">Unknown unary operator =or= Sub-expression is not supported (as goes from <see cref="TranslateExpression"/>)</exception>
            CodeExpression TranslateUnaryOperator(UnaryEx op, MethodContextBase method, IStatementInsertContext IC)
            {
                Type type = null;
                switch (op.Operation)
                {
                    case Operations.Plus:
                        return TranslateExpression(op.Expr, method, IC);
                    case Operations.Minus:
                        return new CodeBinaryOperatorExpression(
                            new CodePrimitiveExpression(-1), CodeBinaryOperatorType.Multiply,
                            TranslateExpression(op.Expr, method, IC));
                    case Operations.LogicNegation:
                        return new CodeBinaryOperatorExpression(
                            TranslateExpression(op.Expr, method, IC),
                            CodeBinaryOperatorType.ValueEquality,
                            new CodePrimitiveExpression(false));
                    case Operations.BitNegation:
                        return new CodeMethodInvokeExpression(
                            new CodeTypeReferenceExpression(typeof(Operators)),
                            "BitNot",
                            new CodeExpression[] { TranslateExpression(op.Expr, method, IC) });
                    case Operations.Int8Cast: type = typeof(SByte); goto case Operations.BoolCast;
                    case Operations.Int16Cast: type = typeof(Int16); goto case Operations.BoolCast;
                    case Operations.Int32Cast: type = typeof(Int32); goto case Operations.BoolCast;
                    case Operations.Int64Cast: type = typeof(Int64); goto case Operations.BoolCast;
                    case Operations.UInt8Cast: type = typeof(Byte); goto case Operations.BoolCast;
                    case Operations.UInt16Cast: type = typeof(UInt16); goto case Operations.BoolCast;
                    case Operations.UInt32Cast: type = typeof(UInt32); goto case Operations.BoolCast;
                    case Operations.UInt64Cast: type = typeof(UInt64); goto case Operations.BoolCast;
                    case Operations.DecimalCast: type = typeof(Decimal); goto case Operations.BoolCast;
                    case Operations.DoubleCast: type = typeof(Double); goto case Operations.BoolCast;
                    case Operations.FloatCast: type = typeof(Single); goto case Operations.BoolCast;
                    case Operations.UnicodeCast:
                    case Operations.StringCast: type = typeof(String); goto case Operations.BoolCast;
                    case Operations.ObjectCast: type = typeof(Object); goto case Operations.BoolCast;
                    case Operations.ArrayCast: type = typeof(PhpArray); goto case Operations.BoolCast;
                    case Operations.BoolCast: type = type == null ? typeof(Boolean) : type;
                        return new CodeCastExpression(type, TranslateExpression(op.Expr, method, IC));
                    case Operations.UnsetCast:
                        return new CodeMethodInvokeExpression(
                            new CodeTypeReferenceExpression(typeof(Helper)), "ReturnNull",
                            new CodeExpression[] { TranslateExpression(op.Expr, method, IC) });
                    case Operations.Clone:
                        return new CodeMethodInvokeExpression(new CodeTypeReferenceExpression(typeof(PhpVariable)),
                            "Copy",
                            new CodeExpression[]{
                                TranslateExpression(op.Expr,method,IC),
                                new CodeFieldReferenceExpression(
                                    new CodeTypeReferenceExpression(typeof(CopyReason)),"Assigned")});
                    case Operations.Print://Helper.Print(Expr,<context>)
                        return new CodeMethodInvokeExpression(
                            new CodeTypeReferenceExpression(typeof(Helper)), "Print",
                            new CodeExpression[]{
                                TranslateExpression(op.Expr,method,IC),
                                CurrentContext});
                    case Operations.AtSign:
                        return new CodeMethodInvokeExpression(
                            new CodeTypeReferenceExpression(typeof(Helper)), "NoError",
                            new CodeExpression[] { TranslateExpression(op.Expr, method, IC) });
                    default:
                        throw new PhpToCodeDomNotSupportedException(string.Format(Localizations.Strings.cdp_unsup_unsup_unop, op.Operation), op);
                }
            }
            /// <summary>Translates call of PHP constructor to CodeDOM</summary>
            /// <param name="New">CTor to translate</param>
            /// <param name="method">GetUserEntryPoint for declaring local variables</param>
            /// <param name="IC">COntext for adding statements</param>
            /// <returns>Translated sxpression</returns>
            /// <exception cref="PhpToCodeDomNotSupportedException">One of sub-expressions is not supported (as goes from <see cref="TranslateTypeRef"/>, <see cref="TranslateParams"/>)</exception>
            protected CodeExpression TranslateNew(NewEx New, MethodContextBase method, IStatementInsertContext IC)
            {
                if (LooksLikeDelegate(New))
                    return TranslateDelegateCreation(New, method, IC);
                else
                {
                    var tr = TranslateTypeRef(New.ClassNameRef, method);
                    Type Type = GetType(tr);
                    ConstructorInfo CTor = null;
                    if (Type != null)
                    {
                        var cs = Type.GetConstructors();
                        if (cs.Length == 1) CTor = cs[0];
                        else
                            foreach (ConstructorInfo c in cs)
                                if (c.GetParameters().Length == New.CallSignature.Parameters.Count)
                                {
                                    CTor = c;
                                    break;
                                }
                    }
                    return new CodeObjectCreateExpression(
                        tr,
                        TranslateParams(New.CallSignature.Parameters, method, IC, CTor.GetParameters()));
                }
            }
            /// <summary>Translates delegate creation from PHP to CodeDOM</summary>
            /// <param name="New">Expression used to create delegate.
            /// Delegate must be created using <see cref="NewEx"/> with only one parameter type <see cref="ArrayEx"/> that consits of 2 <see cref="ValueItem"/>-s without index where <see cref="ValueItem.ValueExpr">ValueExpr</see> of 2nd item is <see cref="StringLiteral"/>.</param>
            /// <param name="method">GetUserEntryPoint for declaring local variables.</param>
            /// <param name="IC">Context for inserting statements</param>
            /// <returns>Translated expression</returns>
            /// <exception cref="PhpToCodeDomNotSupportedException">
            /// <paramref name="New"/> does not obey rules menitoned above.
            /// =or=
            /// Expression used as delegate's object is not supported (see <see cref="TranslateExpression"/>). 
            /// </exception>
            protected CodeDelegateCreateExpression TranslateDelegateCreation(NewEx New, MethodContextBase method, IStatementInsertContext IC)
            {
                try
                {
                    return new CodeDelegateCreateExpression(
                        TranslateTypeRef(New.ClassNameRef, method),
                        (((ValueItem)((ArrayEx)New.CallSignature.Parameters[0].Expression).Items[0]).ValueExpr is StringLiteral) ?
                        new CodeTypeReferenceExpression(((string)((StringLiteral)((ValueItem)((ArrayEx)New.CallSignature.Parameters[0].Expression).Items[0]).ValueExpr).Value).Replace(":::", ".")) :
                        TranslateExpression(((ValueItem)((ArrayEx)New.CallSignature.Parameters[0].Expression).Items[0]).ValueExpr, method, IC),
                        (string)((StringLiteral)((ValueItem)((ArrayEx)New.CallSignature.Parameters[0].Expression).Items[1]).ValueExpr).Value);
                }
                catch (Exception ex)
                {
                    throw new PhpToCodeDomNotSupportedException(Localizations.Strings.cdp_unsup_unsup_delegate_creartion, ex, New);
                }
            }
            /// <summary>Gets name of variable from <see cref="DirectVarUse"/></summary>
            /// <param name="dvu"><see cref="DirectVarUse"/> to be translated</param>
            /// <param name="method">GetUserEntryPoint fro declaring local variables</param>
            /// <returns>Name of variable used</returns>
            protected string TranslateDirectVarUse(DirectVarUse /*!*/ dvu, MethodContextBase /*!*/ method)
            {
                CodeExpression ret = TranslateVariableUse(dvu, method, null);
                if (ret is CodeVariableReferenceExpression)
                    return ((CodeVariableReferenceExpression)ret).VariableName;
                else
                    throw new PhpToCodeDomNotSupportedException(Localizations.Strings.cdp_unsup_CodeVariableReferenceExpression_excpected, dvu);
            }
            /// <summary>Translate use of variable in PHP into CodeDOM</summary>
            /// <param name="use"><see cref="VariableUse"/> to be translated</param>
            /// <param name="method">GetUserEntryPoint for declaring local variables</param>
            /// <param name="IC">Block for inserting aditional statements</param>
            /// <returns>Expression containing access to variale</returns>
            /// <remarks>Not each kind of access to variable in PHP can be repsesented in CodeDOM. Namely indirect accesses cannot. They are represented via objects from <see cref="Helper"/> class. This is done only for CodeDOM pusposes. So translated indirect access cannot be used in runtime, because it is not implemented (by now).</remarks>
            /// <exception cref="PhpToCodeDomNotSupportedException">
            /// <paramref name="use"/> is neither of <see cref="ItemUse"/> <see cref="DirectVarUse"/>, <see cref="IndirectVarUse"/>, <see cref="DirectStFldUse"/>, <see cref="DirectStFldUse"/>
            /// =or=
            /// some sub-construct is not supported (as comes from <see cref="TranslateVariableUse"/>, <see cref="TranslateVarLikeConstructUse"/>, <see cref="TranslateExpression"/>, <see cref="TranslateGenericQualifiedName"/>, <see cref="TranslateDirectVarUse"/>, <see cref="TranslateDirectStFldUse"/>)
            /// =or=
            /// Attempt to use static variable inside method that is not member of class.
            /// </exception>
            protected CodeExpression/*!*/ TranslateVariableUse(VariableUse /*!*/ use, MethodContextBase /*!*/ method, IStatementInsertContext IC)
            {
                if (use is ItemUse)
                {                                         //$var[$index]
                    return new CodeArrayIndexerExpression(TranslateVarLikeConstructUse(((ItemUse)use).Array, method, IC), TranslateExpression(((ItemUse)use).Index, method, IC));
                }
                else if (use is DirectVarUse && use.IsMemberOf == null)
                {   //$var
                    if (((DirectVarUse)use).VarName.Value == "this")
                        return new CodeThisReferenceExpression();
                    else
                        if (method is MethodContext && ((MethodContext)method).StaticVariables.ContainsKey(((DirectVarUse)use).VarName.Value))
                            if (((MethodContext)method).Parent is TypeContext)
                                return new CodeFieldReferenceExpression(
                                    new CodeTypeReferenceExpression(((TypeContext)((MethodContext)method).Parent).Context.Name),
                                        ((MethodContext)method).StaticVariables[((DirectVarUse)use).VarName.Value]);
                            else
                                throw new PhpToCodeDomNotSupportedException(Localizations.Strings.cdp_unsup_static_var, use);
                        else
                        {
                            string name = ((DirectVarUse)use).VarName.Value;
                            if (method.CanAdd)
                            {
                                CodeVariableDeclarationStatement var;
                                try
                                {
                                    var = method.Add(ref name);
                                }
                                catch (NotSupportedException ex)
                                {
                                    throw new PhpToCodeDomNotSupportedException(ex.Message, ex, use);
                                }
                                if (var != null)
                                    var.LinePragma = getPragma(use.Position.FirstLine - 1);
                            }
                            return new CodeVariableReferenceExpression(name);
                        }
                }
                else if (use is DirectVarUse)
                {                             //$obj->var
                    return TranslateDirectVarUse((DirectVarUse)use, method, IC);
                }
                else if (use is IndirectVarUse && use.IsMemberOf == null)
                { //$$var
                    return new CodePropertyReferenceExpression(
                        new CodeObjectCreateExpression(new CodeTypeReference("PHP.Core.CodeDom.Helper.IndirectVarAccess"), new CodeExpression[]{
                            TranslateExpression(((IndirectVarUse)use).VarNameEx,method,IC)}),
                            "Access");
                }
                else if (use is IndirectVarUse)
                {                           //$obj->$var
                    return new CodePropertyReferenceExpression(
                        new CodeObjectCreateExpression(new CodeTypeReference("PHP.Core.CodeDom.Helper.IndirectInstFldAccess"), new CodeExpression[]{
                            TranslateVarLikeConstructUse(use.IsMemberOf,method,IC),
                            TranslateExpression(((IndirectVarUse)use).VarNameEx,method,IC)}),
                            "Access");
                }
                else if (use is DirectStFldUse)
                {                           //class::$var
                    return TranslateDirectStFldUse((DirectStFldUse)use, method, IC);
                }
                else if (use is IndirectStFldUse)
                {                         //class::$$var
                    return new CodePropertyReferenceExpression(
                        new CodeObjectCreateExpression(new CodeTypeReference("PHP.Core.CodeDom.Helper.IndirectStFldAccess"), new CodeExpression[]{
                            new CodeTypeOfExpression(TranslateGenericQualifiedName(((IndirectStFldUse)use).TypeName,true)),
                            TranslateExpression(((IndirectStFldUse)use).FieldNameExpr,method,IC)}),
                        "Access");
                }
                else throw new PhpToCodeDomNotSupportedException(string.Format(Localizations.Strings.cdp_unsup_unknown_var_kind, use.GetType().FullName), use);
            }
            /// <summary>Translates <see cref="DirectVarUse"/> ($obj->var) to <see cref="CodeFieldReferenceExpression"/> or <see cref="CodePropertyReferenceExpression"/></summary>
            /// <param name="use">A <see cref="DirectVarUse"/> to translate. <paramref name="use"/>.<see cref="PHP.Core.AST.VarLikeConstructUse.IsMemberOf">IsMemberOf</see> must not be null.</param>
            /// <param name="method">GetUserEntryPoint for declaring local variables</param>
            /// <param name="IC">Block for inserting aditional statements</param>
            /// <returns><see cref="CodeFieldReferenceExpression"/> or <see cref="CodePropertyReferenceExpression"/></returns>
            /// <exception cref="NullReferenceException"><paramref name="use"/>.<see cref="PHP.Core.AST.VarLikeConstructUse.IsMemberOf">IsMemberOf</see> is null (it represents $var instead of $obj->var)</exception>
            /// <exception cref="PhpToCodeDomNotSupportedException">Sub-construct is not supported (see <see cref="TranslateVarLikeConstructUse"/>)</exception>
            protected CodeExpression TranslateDirectVarUse(DirectVarUse /*!*/ use, MethodContextBase /*!*/ method, IStatementInsertContext IC)
            {
                if (use.IsMemberOf == null) throw new NullReferenceException(Localizations.Strings.cdp_unsup_null);
                if (IsProperty(use, method))
                    return new CodePropertyReferenceExpression(TranslateVarLikeConstructUse(use.IsMemberOf, method, IC), use.VarName.Value);
                else
                    return new CodeFieldReferenceExpression(TranslateVarLikeConstructUse(use.IsMemberOf, method, IC), use.VarName.Value);
            }
            /// <summary>Translates <see cref="DirectStFldUse"/> (class::$var) to <see cref="CodeFieldReferenceExpression"/> or <see cref="CodePropertyReferenceExpression"/></summary>
            /// <param name="method">GetUserEntryPoint for declaring local variables</param>
            /// <param name="IC">Block for inserting aditional statements</param>
            /// <param name="use">A <see cref="DirectStFldUse"/> to translate</param>
            /// <returns><see cref="CodeFieldReferenceExpression"/> or <see cref="CodePropertyReferenceExpression"/></returns>
            protected CodeExpression TranslateDirectStFldUse(DirectStFldUse /*!*/ use, MethodContextBase /*!*/ method, IStatementInsertContext IC)
            {
                if (IsProperty(use))
                    return new CodePropertyReferenceExpression(new CodeTypeReferenceExpression(TranslateGenericQualifiedName(use.TypeName, true)), use.PropertyName.Value);
                else
                    return new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(TranslateGenericQualifiedName(use.TypeName, true)), use.PropertyName.Value);
            }
            /// <summary>Attempts to get method being called</summary>
            /// <param name="target">Object method is invoked on</param>
            /// <param name="name">GetUserEntryPoint name</param>
            /// <param name="parcount">Number of parameters</param>
            /// <param name="context">Context</param>
            /// <returns>If found returns method of target object with given name and possibly same number of parameters; null otherwise</returns>
            private MethodInfo GetHintMethod(CodeExpression target, string name, int parcount, MethodContextBase context)
            {
                var extype = GetTypeOfExpression(target, context);
                Type ttype;
                if (extype != null && (ttype = GetType(extype)) != null)
                {
                    List<MethodInfo> methods = new List<MethodInfo>();
                    foreach (var m in ttype.GetMethods())
                        if (m.Name.ToLower() == name.ToLower())
                            methods.Add(m);
                    if (methods.Count == 1) return methods[0];
                    else foreach (MethodInfo m in methods)
                            if (m.GetParameters().Length == parcount)
                                return m;
                    if (methods.Count > 0) return methods[0];
                }
                return null;
            }
            /// <summary>Translates <see cref="VarLikeConstructUse"/> to CodeDOM <see cref="CodeExpression"/>.</summary>
            /// <param name="use"><see cref="VarLikeConstructUse"/> to translate</param>
            /// <param name="method">method for declaring variables</param>
            /// <param name="IC">Blosk for inserting aditional statements</param>
            /// <returns><see cref="CodeExpression"/> that repsesents <paramref name="use"/></returns>
            /// <remarks>
            /// This method deals with method calls. Only direct method calls are fully implemented. Indirect calls are implemented via helper method of <see cref="Helper"/>. Semantic of helper method is not exactly the same as of PHP indirect call. Type parameters for indirect calls are ignored!
            /// Variable usages are passed to <see cref="TranslateVariableUse"/>.
            /// </remarks>
            /// <exception cref="PhpToCodeDomNotSupportedException">
            /// Attempt to translate call of instance method without instance specified.
            /// =or=
            /// <paramref name="use"/> is neither of <see cref="DirectFcnCall"/>, <see cref="IndirectFcnCall"/>, <see cref="StaticMtdCall"/>, <see cref="IndirectStMtdCall"/>, <see cref="VariableUse"/>
            /// =or=
            /// Sub-construct is not supported (as goes from <see cref="TranslateVarLikeConstructUse"/>, <see cref="TranslateParams"/>, <see cref="TranslateTypeRef"/>, <see cref="TranslateVariableUse"/>)
            /// </exception>
            protected CodeExpression /*!*/TranslateVarLikeConstructUse(VarLikeConstructUse /*!*/use, MethodContextBase /*!*/method, IStatementInsertContext IC)
            {
                if (use is DirectFcnCall)
                {
                    string name = ((DirectFcnCall)use).QualifiedName.Name.Value;
                    
                    if (use.IsMemberOf == null)
                    {
                        //throw new PhpToCodeDomNotSupportedException(Localizations.Strings.cdp_unsup_nonobj_func, use);
                        /*CodeMethodInvokeExpression reti = new CodeMethodInvokeExpression();
                        reti.GetUserEntryPoint = new CodeMethodReferenceExpression();
                        reti.GetUserEntryPoint.TargetObject = new CodeTypeReferenceExpression("");
                        reti.GetUserEntryPoint.MethodName = ((DirectFcnCall)use).QualifiedName.Name.Value;
                        reti.Parameters.AddRange(TranslateParams(((DirectFcnCall)use).CallSignature.Parameters, method, IC));
                        return reti;*/

                        CodeMethodInvokeExpression reti = new CodeMethodInvokeExpression(
                            CurrentContext, "Call",
                            new CodeExpression[] { new CodePrimitiveExpression(name) });

                        reti.Parameters.AddRange(TranslateParams(((DirectFcnCall)use).CallSignature.Parameters, method, IC, null));
                        return reti;
                    }
                    else
                    {
                        var target = TranslateVarLikeConstructUse(use.IsMemberOf, method, IC);
                        MethodInfo cmethod = GetHintMethod(target, name, ((DirectFcnCall)use).CallSignature.Parameters.Count, method);
                        CodeMethodInvokeExpression ret = new CodeMethodInvokeExpression(
                            target, name,
                            TranslateParams(((DirectFcnCall)use).CallSignature.Parameters, method, IC, cmethod == null ? null : cmethod.GetParameters())
                            );
                        foreach (TypeRef GPar in ((DirectFcnCall)use).CallSignature.GenericParams)
                            ret.Method.TypeArguments.Add(TranslateTypeRef(GPar, method));
                        return ret;
                    }
                }
                else if (use is IndirectFcnCall)
                {
                    if (use.IsMemberOf == null)
                    {
                        CodeMethodInvokeExpression reti = new CodeMethodInvokeExpression(CurrentContext, "Call",
                            new CodeExpression[]{
                                new CodeCastExpression(typeof(string),TranslateExpression(((IndirectFcnCall)use).NameExpr,method,IC))});
                        reti.Parameters.AddRange(
                            TranslateParams(((IndirectFcnCall)use).CallSignature.Parameters, method, IC, null));
                        return reti;
                    }
                    List<CodeExpression> Params = new List<CodeExpression>(2 + ((IndirectFcnCall)use).CallSignature.Parameters.Count);
                    Params.Add(TranslateVarLikeConstructUse(use.IsMemberOf, method, IC));
                    Params.Add(TranslateExpression(((IndirectFcnCall)use).NameExpr, method, IC));
                    Params.AddRange(TranslateParams(((IndirectFcnCall)use).CallSignature.Parameters, method, IC, null));
                    CodeMethodInvokeExpression ret = new CodeMethodInvokeExpression(
                        new CodeTypeReferenceExpression(typeof(Helper)), "CallIndirectInstance",
                        Params.ToArray());
                    //Note: Generic parameters are simply ignored
                    return ret;
                }
                else if (use is DirectStMtdCall)
                {
                    var ttype = new CodeTypeReferenceExpression(TranslateGenericQualifiedName(((DirectStMtdCall)use).ClassName, true));
                    string name = ((DirectStMtdCall)use).MethodName.Value;
                    MethodInfo cmethod = GetHintMethod(ttype, name, ((DirectStMtdCall)use).CallSignature.Parameters.Count, method);
                    CodeMethodInvokeExpression ret = new CodeMethodInvokeExpression(
                        ttype,
                        ((DirectStMtdCall)use).MethodName.Value,
                        TranslateParams(((DirectStMtdCall)use).CallSignature.Parameters, method, IC, cmethod == null ? null : cmethod.GetParameters()));
                    foreach (TypeRef GPar in ((StaticMtdCall)use).CallSignature.GenericParams)
                        ret.Method.TypeArguments.Add(TranslateTypeRef(GPar, method));
                    return ret;
                }
                else if (use is IndirectStMtdCall)
                {
                    List<CodeExpression> Params = new List<CodeExpression>(2 + ((IndirectStMtdCall)use).CallSignature.Parameters.Count);
                    Params.Add(new CodeTypeOfExpression(TranslateGenericQualifiedName(((IndirectStMtdCall)use).ClassName, true)));
                    Params.Add(TranslateVariableUse(((IndirectStMtdCall)use).MethodNameVar, method, IC));
                    Params.AddRange(TranslateParams(((IndirectStMtdCall)use).CallSignature.Parameters, method, IC, null));
                    CodeMethodInvokeExpression ret = new CodeMethodInvokeExpression(
                        new CodeTypeReferenceExpression(typeof(Helper)), "CallIndirectStatic",
                        Params.ToArray());
                    return ret;
                    //Note: Generic parameters are simply ignored
                }
                else if (use is VariableUse)
                {
                    return TranslateVariableUse((VariableUse)use, method, IC);
                }
                else throw new PhpToCodeDomNotSupportedException(string.Format(Localizations.Strings.cdp_unsup_unknown_varlike_construct_use, use.GetType().FullName), use);
            }
            /// <summary>Translates list of PHP actual parameters of method to list of CodeDOM expressions</summary>
            /// <param name="Params">Parameters to be translated</param>
            /// <param name="method">GetUserEntryPoint for declaring local variables</param>
            /// <param name="IC">Context for inserting additional statements</param>
            /// <returns>Array of expressions translated from PHP to CodeDOM for each parameter</returns>
            /// <param name="TargetSignature">Parameter hints. Size doesn't have to match size of <paramref name="Params"/>. Can be even empty or null.</param>
            /// <exception cref="PhpToCodeDomNotSupportedException">May be thrown by <see cref="TranslateParam"/>.</exception>
            protected CodeExpression[] TranslateParams(List<ActualParam> Params, MethodContextBase method, IStatementInsertContext IC, ParameterInfo[] TargetSignature)
            {
                if (TargetSignature == null) TargetSignature = new ParameterInfo[] { };
                List<CodeExpression> ret = new List<CodeExpression>(Params.Count);
                int i = 0;
                foreach (ActualParam Param in Params)
                {
                    ret.Add(TranslateParam(Param, method, IC, TargetSignature.Length > i ? TargetSignature[i] : null));
                    i += 1;
                }
                return ret.ToArray();
            }
            /// <summary>Translates actual parameter of method from PHP to CodeDOM</summary>
            /// <param name="Param">Parameter to be translated</param>
            /// <param name="method">GetUserEntryPoint fro declaring variables</param>
            /// <param name="IC">Context for inserting additional statements</param>
            /// <param name="Hint">May contain casting hint for parameter. Currently utilized only with arrays.</param> 
            /// <returns>Expression thet represents parameter translated from PHP to CodeDOM</returns>
            /// <exception cref="PhpToCodeDomNotSupportedException">My be thrown by <see cref="TranslateExpression"/></exception>
            protected CodeExpression TranslateParam(ActualParam /*!*/Param, MethodContextBase /*!*/ method, IStatementInsertContext IC, ParameterInfo Hint)
            {
                var ret = TranslateExpression(Param.Expression, method, IC);
                if (ret is CodeArrayCreateExpression && Hint != null && !((CodeArrayCreateExpression)ret).CreateType.Equals(Hint.ParameterType))
                    ((CodeArrayCreateExpression)ret).CreateType = new CodeTypeReference(Hint.ParameterType);
                return ret;
            }
            #endregion
            #region Very specific
            /// <summary>Translates PHP array "constructor" to CodeDOM</summary>
            /// <param name="array">Array "constructor" to be translated</param>
            /// <param name="method">GetUserEntryPoint for declaring local variables</param>
            /// <param name="IC">Context for adding aditional statements</param>
            /// <returns>Translated expression</returns>
            /// <remarks>
            /// If <paramref name="array"/> does not contain any associative item (it is item with <see cref="Item.Index"/> != null) array initialization is translated inline like C# object[] = new object[]{item1,item2,item3}.
            /// This does not require <paramref name="IC"/>.
            /// If there is any associative item in the list, expression is translated using <paramref name="IC"/> as creation of new variable of type <see cref="PhpArray"/> and then filled using <see cref="PhpArray.SetArrayItem(object)"/> for non-associative items and <see cref="PhpArray.SetArrayItem(object,object)"/> for associative ones.
            /// In this case function returns refernce to newly created variable.
            /// </remarks>
            /// <exception cref="PhpToCodeDomNotSupportedException">
            /// Array contains item passed by reference.
            /// =or=
            /// Array contains item that is neither <see cref="ValueItem"/> nor <see cref="RefItem"/>
            /// =or=
            /// Array contains an associative item and <paramref name="IC"/> is null
            /// =or=
            /// Any sub-expression contains unsupported construct (see <see cref="TranslateExpression"/>, <see cref="TranslateNotAssociativeArrayItems"/>)
            /// </exception>
            protected CodeExpression TranslateArray(ArrayEx array, MethodContextBase method, IStatementInsertContext IC)
            {
                bool isAssociative = false;
                foreach (Item item in array.Items)
                    if (item is RefItem)
                        throw new PhpToCodeDomNotSupportedException(Localizations.Strings.cdp_unsup_array_item_reference, ((RefItem)item).RefToGet);
                    else if (item is ValueItem)
                    {
                        if (item.Index != null)
                        {
                            isAssociative = true;
                            break;
                        }
                    }
                    else throw new PhpToCodeDomNotSupportedException(string.Format(Localizations.Strings.cdp_unsup_unknown_array_item_type, item.GetType().FullName), ((ValueItem)item).ValueExpr);
                if (isAssociative)
                {
                    if (IC == null) throw new PhpToCodeDomNotSupportedException(Localizations.Strings.cdp_unsup_assoc_arr_init_context, array);
                    string varName = string.Format("__array_{0:000}", arrUnqCnt++);
                    IC.Insert(new CodeVariableDeclarationStatement(typeof(PhpArray), varName), array);
                    IC.Insert(new CodeAssignStatement(new CodeVariableReferenceExpression(varName),
                        new CodeObjectCreateExpression(typeof(PhpArray), new CodeExpression[]{
                            new CodePrimitiveExpression(array.Items.Count)})), array);
                    foreach (Item item in array.Items)
                        IC.Insert(new CodeExpressionStatement(
                            new CodeMethodInvokeExpression(new CodeVariableReferenceExpression(varName),
                            "SetArrayItem",
                            item.Index == null ?
                                new CodeExpression[] { TranslateExpression(((ValueItem)item).ValueExpr, method, IC) } :
                                new CodeExpression[]{
                                    TranslateExpression(item.Index,method,IC),
                                    TranslateExpression(((ValueItem)item).ValueExpr,method,IC)})), array);
                    return new CodeVariableReferenceExpression(varName);
                }
                else
                {
                    var ret = new CodeArrayCreateExpression(typeof(object),
                        TranslateNotAssociativeArrayItems(array.Items, method, IC, array));
                    CodeTypeReference etype = null;
                    foreach (CodeExpression item in ret.Initializers)
                    {
                        var type = GetTypeOfExpression(item, method);
                        if (type == null) { etype = null; break; }
                        if (etype == null) { etype = type; continue; }
                        if (!TypeEquals(type, etype)) { etype = null; break; }
                    }
                    if (etype != null) ret.CreateType = etype;
                    else
                    {//Pass2
                        Type t2 = null;
                        foreach (CodeExpression item in ret.Initializers)
                        {
                            var type = GetTypeOfExpression(item, method);
                            if (type == null) { t2 = null; break; }
                            var tcurr = GetType(type);
                            if (tcurr == null) { t2 = null; break; }
                            if (t2 == null) { t2 = tcurr; continue; }
                            t2 = CommonBase(tcurr, t2, true);
                            if (t2.Equals(typeof(object))) break;
                        }
                        if (t2 != null) ret.CreateType = new CodeTypeReference(t2);
                    }
                    return ret;
                }
            }
            /// <summary>Attempts to determine commn base type of 2 types</summary>
            /// <param name="t1">A <see cref="Type"/></param>
            /// <param name="t2">A <see cref="Type"/></param>
            /// <param name="iface">True to return interface if no class is possible</param>
            /// <returns>Common base type of <paramref name="t1"/> and <paramref name="t2"/></returns>
            /// <exception cref="ArgumentNullException"><paramref name="t1"/> or <paramref name="t2"/> is null</exception>
            private Type CommonBase(Type t1, Type t2, bool iface)
            {
                if (t1 == null) throw new ArgumentNullException("t1");
                if (t2 == null) throw new ArgumentNullException("t2");
                if (t1.IsAssignableFrom(t2)) return t1;
                if (t2.IsAssignableFrom(t1)) return t2;
                Type cb = typeof(object);
                if (!t1.BaseType.Equals(typeof(object)))
                    cb = CommonBase(t1.BaseType, t2, false);
                if (cb.Equals(typeof(object)) && !t2.BaseType.Equals(typeof(object)))
                    cb = CommonBase(t1, t2.BaseType, false);
                if (cb.Equals(typeof(object)) && iface)
                {
                    List<Type> ifaces = new List<Type>();
                    foreach (Type i1 in t1.GetInterfaces())
                        foreach (Type i2 in t2.GetInterfaces())
                            if (i1.Equals(i2)) ifaces.Add(i1);
                    if (ifaces.Count == 1) cb = ifaces[0];
                    else if (ifaces.Count > 0)
                    {
                        foreach (Type i in ifaces.ToArray())
                            foreach (Type ibase in i.GetInterfaces())
                                ifaces.Remove(ibase);
                        cb = ifaces[0];
                    }
                }
                return cb;
            }
            /// <summary>Attempts to determine return type of an expression</summary>
            /// <param name="expression">Expression to determine type of</param>
            /// <param name="context">Context. May be null, but certain expressions requires context to be known</param>
            /// <returns>Type returned by expression; null if not successfull</returns>
            private CodeTypeReference GetTypeOfExpression(CodeExpression /*!*/ expression, MethodContextBase context)
            {
                if (expression == null) throw new ArgumentNullException("expression");
                if (expression is CodeArgumentReferenceExpression)
                {
                    if (context as MethodContext == null) return null;
                    foreach (CodeParameterDeclarationExpression par in ((MethodContext)context).Context.Parameters)
                        if (par.Name == ((CodeArgumentReferenceExpression)expression).ParameterName)
                            return par.Type;
                    return null;
                } if (expression is CodeSnippetExpression)
                {//Not supported
                } if (expression is CodeArrayIndexerExpression)
                {
                    CodeTypeReference toe = GetTypeOfExpression(((CodeArrayIndexerExpression)expression).TargetObject, context);
                    return toe.ArrayElementType;
                }
                else if (expression is CodeBaseReferenceExpression)
                {
                    if (context == null) return null;
                    if (context.Class.Context.BaseTypes.Count > 0)
                    {
                        Type t = GetType(context.Class.Context.BaseTypes[0]);
                        if (t != null && !t.IsInterface)
                            return context.Class.Context.BaseTypes[0];
                    }
                    return new CodeTypeReference(typeof(object));
                }
                else if (expression is CodeBinaryOperatorExpression)
                {
                    switch (((CodeBinaryOperatorExpression)expression).Operator)
                    {
                        case CodeBinaryOperatorType.Assign:
                            return GetTypeOfExpression(((CodeBinaryOperatorExpression)expression).Right, context);
                        case CodeBinaryOperatorType.Add:
                            CodeTypeReference l = GetTypeOfExpression(((CodeBinaryOperatorExpression)expression).Left, context);
                            CodeTypeReference r = GetTypeOfExpression(((CodeBinaryOperatorExpression)expression).Right, context);
                            if ((l != null && l.ArrayRank == 0 && (l.BaseType == "System.String" || l.BaseType == "System.Char")) || (r != null && r.ArrayRank == 0 && (r.BaseType == "System.String" || r.BaseType == "System.Char")))
                                return new CodeTypeReference(typeof(string));
                            return null;
                        case CodeBinaryOperatorType.BitwiseAnd:
                        case CodeBinaryOperatorType.BitwiseOr:
                        case CodeBinaryOperatorType.Divide:
                        case CodeBinaryOperatorType.Multiply:
                        case CodeBinaryOperatorType.Subtract:
                            CodeTypeReference lft = GetTypeOfExpression(((CodeBinaryOperatorExpression)expression).Left, context);
                            CodeTypeReference rgt = GetTypeOfExpression(((CodeBinaryOperatorExpression)expression).Right, context);
                            if (lft == null && rgt == null) return null;
                            if (lft == null || lft.ArrayRank != 0) return rgt;
                            if (rgt == null || rgt.ArrayRank != 0) return lft;
                            Type lType = GetType(lft);
                            Type rType = GetType(rgt);
                            string lName = lft.BaseType; string rName = rgt.BaseType;
                            if (lType != null && lType.IsEnum) lName = Enum.GetUnderlyingType(lType).FullName;
                            if (rType != null && rType.IsEnum) rName = Enum.GetUnderlyingType(rType).FullName;
                            if (rgt.BaseType == lft.BaseType) return rgt;
                            if (rName == lName) return new CodeTypeReference(rName);
                            string Boolean = typeof(bool).FullName;
                            string Byte = typeof(byte).FullName;
                            string SByte = typeof(sbyte).FullName;
                            string Short = typeof(short).FullName;
                            string UShort = typeof(ushort).FullName;
                            string Integer = typeof(int).FullName;
                            string UInteger = typeof(uint).FullName;
                            string Long = typeof(long).FullName;
                            string ULong = typeof(ulong).FullName;
                            string Char = typeof(char).FullName;
                            string Single = typeof(float).FullName;
                            string Double = typeof(double).FullName;
                            string String = typeof(string).FullName;
                            string Decimal = typeof(decimal).FullName;
                            string ret = null;
                            if (rName == Decimal || lName == Decimal || lName == String || rName == String || lName == Char || rName == Char)
                                ret = Decimal;
                            else if (lName == Double || rName == Double) ret = Double;
                            else if (lName == Single || rName == Single) ret = Single;
                            else if (((CodeBinaryOperatorExpression)expression).Operator == CodeBinaryOperatorType.Divide)
                                ret = Double;
                            else if ((lName == Long || rName == ULong) && lName != rName) ret = Long;
                            else if (lName == Long || rName == Long) ret = Long;
                            else if (lName == ULong || rName == ULong) ret = ULong;
                            else if ((lName == Integer || rName == UInteger) && lName != rName) ret = Single;
                            else if (lName == Integer || rName == Integer) ret = Integer;
                            else if (lName == UInteger || rName == UInteger) ret = UInteger;
                            else if ((lName == Short || rName == UShort) && lName != rName) ret = Integer;
                            else if (lName == Short || rName == Short) ret = Short;
                            else if (lName == UShort || rName == UShort) ret = UShort;
                            else if ((lName == Byte || rName == SByte) && lName != rName) ret = Short;
                            else if (lName == Byte || rName == Byte) ret = Byte;
                            else if (lName == SByte || rName == SByte) ret = SByte;
                            else if (lName == Boolean && lName == Boolean)
                                switch (((CodeBinaryOperatorExpression)expression).Operator)
                                {
                                    case CodeBinaryOperatorType.Add:
                                    case CodeBinaryOperatorType.Subtract:
                                        ret = Integer;
                                        break;
                                    case CodeBinaryOperatorType.BitwiseAnd:
                                    case CodeBinaryOperatorType.BitwiseOr:
                                    case CodeBinaryOperatorType.Divide:
                                    case CodeBinaryOperatorType.Multiply:
                                        ret = Boolean;
                                        break;
                                }
                            if (ret != null) return new CodeTypeReference(ret);
                            return lft;
                        case CodeBinaryOperatorType.BooleanAnd:
                        case CodeBinaryOperatorType.BooleanOr:
                        case CodeBinaryOperatorType.GreaterThan:
                        case CodeBinaryOperatorType.GreaterThanOrEqual:
                        case CodeBinaryOperatorType.IdentityEquality:
                        case CodeBinaryOperatorType.IdentityInequality:
                        case CodeBinaryOperatorType.LessThan:
                        case CodeBinaryOperatorType.LessThanOrEqual:
                        case CodeBinaryOperatorType.ValueEquality:
                            return new CodeTypeReference(typeof(bool));
                        case CodeBinaryOperatorType.Modulus:
                            return new CodeTypeReference(typeof(int));
                    }
                }
                else if (expression is CodeThisReferenceExpression)
                {
                    if (context == null) return null;
                    var ret = new CodeTypeReference(context.Class.Context.Name);
                    if (context.Class.Context.TypeParameters.Count > 0)
                        foreach (CodeTypeParameter tp in context.Class.Context.TypeParameters)
                            ret.TypeArguments.Add(new CodeTypeReference(tp));
                    return ret;
                }
                else if (expression is CodePrimitiveExpression)
                {
                    if (((CodePrimitiveExpression)expression).Value == null) return new CodeTypeReference(typeof(object));
                    return new CodeTypeReference(((CodePrimitiveExpression)expression).Value.GetType());
                }
                else if (expression is CodeIndexerExpression)
                {
                    CodeTypeReference tobj = GetTypeOfExpression(((CodeIndexerExpression)expression).TargetObject, context);
                    if (tobj == null) return null;
                    Type t = GetType(tobj);
                    foreach (MemberInfo mi in t.GetDefaultMembers())
                        if (mi is PropertyInfo && ((PropertyInfo)mi).GetIndexParameters().Length == ((CodeIndexerExpression)expression).Indices.Count)
                            return new CodeTypeReference(((PropertyInfo)mi).PropertyType);
                    return null;
                }
                else if (expression is CodeDelegateInvokeExpression)
                {
                    CodeTypeReference DelegateType = GetTypeOfExpression(((CodeDelegateInvokeExpression)expression).TargetObject, context);
                    if (DelegateType == null) return null;
                    Type dType = GetType(DelegateType);
                    if (dType == null) return null;
                    if (typeof(Delegate).IsAssignableFrom(dType))
                    {
                        MethodInfo invoke = dType.GetMethod("Invoke");
                        if (invoke != null) return new CodeTypeReference(invoke.ReturnType);
                    }
                    return null;
                }
                else if (expression is CodeEventReferenceExpression)
                {//Not suppoirted
                }
                else if (expression is CodeDirectionExpression)
                {
                    return GetTypeOfExpression(((CodeDirectionExpression)expression).Expression, context);
                }
                else if (expression is CodeVariableReferenceExpression)
                {
                    if (context == null) return null;
                    if (context.Contains(((CodeVariableReferenceExpression)expression).VariableName))
                        return context[((CodeVariableReferenceExpression)expression).VariableName].Type;
                    return null;
                }
                else if (expression is CodeParameterDeclarationExpression)
                {
                    return ((CodeParameterDeclarationExpression)expression).Type;
                }
                else if (expression is CodeDefaultValueExpression)
                {
                    return ((CodeDefaultValueExpression)expression).Type;
                }
                else if (expression is CodeObjectCreateExpression)
                {
                    return ((CodeObjectCreateExpression)expression).CreateType;
                }
                else if (expression is CodeMethodInvokeExpression)
                {
                    return GetTypeOfExpression(((CodeMethodInvokeExpression)expression).Method, context);
                }
                else if (expression is CodeMethodReferenceExpression)
                {
                    CodeTypeReference TObjType = GetTypeOfExpression(((CodeMethodReferenceExpression)expression).TargetObject, context);
                    if (TObjType == null) return null;
                    Type type = null;
                    bool Protected = false;
                    if (context != null && ((context.Class.Context.Name == TObjType.BaseType && context.Class.Context.TypeParameters.Count == TObjType.TypeArguments.Count) || ((CodeMethodReferenceExpression)(expression)).TargetObject is CodeThisReferenceExpression))
                    {
                        //Private Methods in this class
                        foreach (CodeTypeMember member in context.Class.Context.Members)
                            if (member is CodeMemberMethod && ((CodeMemberMethod)member).Name == ((CodeMethodReferenceExpression)expression).MethodName)
                                return ((CodeMemberMethod)member).ReturnType;
                        if (context.Class.Context.BaseTypes.Count > 0)
                        {
                            type = GetType(context.Class.Context.BaseTypes[0]);
                            if (type != null && type.IsInterface) type = null;
                            Protected = true;
                        }
                    }

                    if (type == null) type = GetType(TObjType);
                    if (type == null) return null;
                    Type btype;
                    Protected = Protected || ((CodeMethodReferenceExpression)expression).TargetObject is CodeBaseReferenceExpression ||
                        (context != null && context.Class.Context.BaseTypes.Count > 0 &&
                        (btype = GetType(context.Class.Context.BaseTypes[0])) != null &&
                        !btype.IsInterface && btype.IsAssignableFrom(type));
                    foreach (MethodInfo fi in type.GetMethods((Protected ? BindingFlags.NonPublic | BindingFlags.Public : BindingFlags.Public) | BindingFlags.Instance | BindingFlags.Static))
                        if ((fi.IsFamily || fi.IsFamilyOrAssembly || fi.IsPublic) && fi.Name == ((CodeMethodReferenceExpression)expression).MethodName)
                            return new CodeTypeReference(fi.ReturnType);
                    return null;
                }
                else if (expression is CodeDelegateCreateExpression)
                {
                    return ((CodeDelegateCreateExpression)expression).DelegateType;
                }
                else if (expression is CodePropertyReferenceExpression)
                {
                    CodeTypeReference TObjType = GetTypeOfExpression(((CodePropertyReferenceExpression)expression).TargetObject, context);
                    if (TObjType == null) return null;
                    Type type = null;
                    bool Protected = false;
                    if (context != null && ((context.Class.Context.Name == TObjType.BaseType && context.Class.Context.TypeParameters.Count == TObjType.TypeArguments.Count) || ((CodePropertyReferenceExpression)(expression)).TargetObject is CodeThisReferenceExpression))
                    {
                        //Private Propertys in this class
                        foreach (CodeTypeMember member in context.Class.Context.Members)
                            if (member is CodeMemberProperty && ((CodeMemberProperty)member).Name == ((CodePropertyReferenceExpression)expression).PropertyName)
                                return ((CodeMemberProperty)member).Type;
                        if (context.Class.Context.BaseTypes.Count > 0)
                        {
                            type = GetType(context.Class.Context.BaseTypes[0]);
                            if (type != null && type.IsInterface) type = null;
                            Protected = true;
                        }
                    }

                    if (type == null) type = GetType(TObjType);
                    if (type == null) return null;
                    Type btype;
                    Protected = Protected || ((CodePropertyReferenceExpression)expression).TargetObject is CodeBaseReferenceExpression ||
                        (context != null && context.Class.Context.BaseTypes.Count > 0 &&
                        (btype = GetType(context.Class.Context.BaseTypes[0])) != null &&
                        !btype.IsInterface && btype.IsAssignableFrom(type));
                    foreach (PropertyInfo fi in type.GetProperties((Protected ? BindingFlags.NonPublic | BindingFlags.Public : BindingFlags.Public) | BindingFlags.Instance | BindingFlags.Static))
                    {
                        MethodInfo getter = fi.GetGetMethod();
                        MethodInfo setter = fi.GetSetMethod();
                        if ((
                            (getter != null && (getter.IsFamily || getter.IsFamilyOrAssembly || getter.IsPublic)) ||
                            (setter != null && (setter.IsFamily || setter.IsFamilyOrAssembly || setter.IsPublic))
                            ) && fi.Name == ((CodePropertyReferenceExpression)expression).PropertyName &&
                            fi.GetIndexParameters().Length == 0)
                            return new CodeTypeReference(fi.PropertyType);
                    }
                    return null;
                }
                else if (expression is CodeFieldReferenceExpression)
                {
                    CodeTypeReference TObjType = GetTypeOfExpression(((CodeFieldReferenceExpression)expression).TargetObject, context);
                    if (TObjType == null) return null;
                    Type type = null;
                    bool Protected = false;
                    if (context != null && ((context.Class.Context.Name == TObjType.BaseType && context.Class.Context.TypeParameters.Count == TObjType.TypeArguments.Count) || ((CodeFieldReferenceExpression)(expression)).TargetObject is CodeThisReferenceExpression))
                    {
                        //Private fields in this class
                        foreach (CodeTypeMember member in context.Class.Context.Members)
                            if (member is CodeMemberField && ((CodeMemberField)member).Name == ((CodeFieldReferenceExpression)expression).FieldName)
                                return ((CodeMemberField)member).Type;
                        if (context.Class.Context.BaseTypes.Count > 0)
                        {
                            type = GetType(context.Class.Context.BaseTypes[0]);
                            if (type != null && type.IsInterface) type = null;
                            Protected = true;
                        }
                    }

                    if (type == null) type = GetType(TObjType);
                    if (type == null) return null;
                    Type btype;
                    Protected = Protected || ((CodeFieldReferenceExpression)expression).TargetObject is CodeBaseReferenceExpression ||
                        (context != null && context.Class.Context.BaseTypes.Count > 0 &&
                        (btype = GetType(context.Class.Context.BaseTypes[0])) != null &&
                        !btype.IsInterface && btype.IsAssignableFrom(type));
                    foreach (FieldInfo fi in type.GetFields((Protected ? (BindingFlags.NonPublic | BindingFlags.Public) : BindingFlags.Public) | BindingFlags.Instance | BindingFlags.Static))
                        if ((fi.IsFamily || fi.IsFamilyOrAssembly || fi.IsPublic) && fi.Name == ((CodeFieldReferenceExpression)expression).FieldName)
                            return new CodeTypeReference(fi.FieldType);
                    return null;
                }
                else if (expression is CodeArrayCreateExpression)
                {
                    return new CodeTypeReference(((CodeArrayCreateExpression)expression).CreateType, 1);
                }
                else if (expression is CodeCastExpression)
                {
                    return ((CodeCastExpression)expression).TargetType;
                }
                else if (expression is CodeTypeOfExpression)
                {
                    return new CodeTypeReference(typeof(Type));
                }
                else if (expression is CodePropertySetValueReferenceExpression)
                {//Not supported
                }
                else if (expression is CodeTypeReferenceExpression)
                {
                    return ((CodeTypeReferenceExpression)expression).Type;
                }
                else
                {
                    return null;
                }
                return null;
            }

            /// <summary>Translates list of items of non-associative (no indexes, values only) array initialization to CodeDOM list of expressions</summary>
            /// <param name="items">Items to translate</param>
            /// <param name="method">method for declaring local variables</param>
            /// <param name="IC">Context for adding additional statements</param>
            /// <param name="array">Array to throw <see cref="PhpToCodeDomNotSupportedException"/> on in case it cannot be thrown on item</param>
            /// <returns>Array of <see cref="CodeExpression"/> taht represents content of array</returns>
            /// <exception cref="PhpToCodeDomNotSupportedException">
            /// A passed-by-reference item of array reached.
            /// =or=
            /// Any sub-expression contains unsupported construct (<see cref="TranslateExpression"/>)
            /// </exception>
            protected CodeExpression[] TranslateNotAssociativeArrayItems(IEnumerable<Item> items, MethodContextBase method, IStatementInsertContext IC, ArrayEx array)
            {
                List<CodeExpression> ret = new List<CodeExpression>();
                foreach (Item item in items)
                    if (item is ValueItem) ret.Add(TranslateExpression(((ValueItem)item).ValueExpr, method, IC));
                    else throw new PhpToCodeDomNotSupportedException(Localizations.Strings.cdp_unsup_array_item_reference, item.Index == null ? (item is RefItem ? (LangElement)((RefItem)item).RefToGet : array) : (LangElement)item.Index);
                return ret.ToArray();
            }
            /// <summary>Translates PHP execution expression `` to CodeDom call of <see cref="Execution.ShellExec"/> method</summary>
            /// <param name="command">Command do translate</param>
            /// <param name="method">GetUserEntryPoint for declaring local variables</param>
            /// <param name="IC">Block for inserting additional statements</param>
            /// <returns>Translated expression</returns>
            /// <exception cref="PhpToCodeDomNotSupportedException">Some of sub-expressions are not supported (as goes from <see cref="TranslateExpression"/>)</exception>
            CodeMethodInvokeExpression TranslateShellExec(ShellEx command, MethodContextBase method, IStatementInsertContext IC)
            {
                return new CodeMethodInvokeExpression(
                    new CodeTypeReferenceExpression(typeof(Execution)),
                    "ShellExec",
                    new CodeExpression[] { TranslateExpression(command.Command, method, IC) });
            }
            /// <summary>Translates PHP pseudo-constant to CodeDOM (one-way)</summary>
            /// <param name="__">PseudoConstant to translate</param>
            /// <returns>Literal that represents this pseudo-constant</returns>
            /// <remarks>This is one-way translation</remarks>
            /// <exception cref="PhpToCodeDomNotSupportedException"><paramref name="__"/> is unknown type of pseudo-constant</exception>
            protected /*!*/CodeExpression Translate__const(PseudoConstUse /*!*/__)
            {
                switch (__.Type)
                {
                    case PseudoConstUse.Types.Class:
                        return new CodePrimitiveExpression(currentClass);
                    case PseudoConstUse.Types.File:
                        return new CodePrimitiveExpression(currentFile);
                    case PseudoConstUse.Types.Dir:
                        return new CodePrimitiveExpression(Path.GetDirectoryName(currentFile));
                    case PseudoConstUse.Types.Function:
                        return new CodePrimitiveExpression(currentFunction);
                    case PseudoConstUse.Types.Line:
                        return new CodePrimitiveExpression(__.Position.FirstLine);
                    case PseudoConstUse.Types.Method:
                        return new CodePrimitiveExpression(currentMethod);
                    case PseudoConstUse.Types.Namespace:
                        return new CodePrimitiveExpression(currentNamespace);
                    default: throw new PhpToCodeDomNotSupportedException(string.Format(Localizations.Strings.cdp_unsup_pseudoconstant_type, __.Type, (int)__.Type), __);
                }
            }
            /// <summary>Helper variable for <see cref="TranslateList"/></summary>
            private int arrUnqCnt = 0;
            /// <summary>Translates PHP list expression to CodeDOM</summary>
            /// <param name="list">Expression to translate</param>
            /// <param name="method">GetUserEntryPoint for declaring local variables</param>
            /// <param name="IC">Block for inserting statements. Can be null, but see exceptions.</param>
            /// <returns>Translated statements</returns>
            /// <remarks>Inserts additional statements into <paramref name="IC"/></remarks>
            /// <exception cref="PhpToCodeDomNotSupportedException"><paramref name="IC"/> is null</exception>
            protected CodeVariableReferenceExpression /*!*/TranslateList(ListEx /*!*/ list, MethodContextBase /*!*/ method, IStatementInsertContext IC)
            {
                if (IC == null)
                    throw new PhpToCodeDomNotSupportedException(Localizations.Strings.cdp_unsup_list_context, list);
                string ArrayName = string.Format("__array_{0:000}", arrUnqCnt++);
                IC.Insert(new CodeVariableDeclarationStatement(typeof(PhpArray), ArrayName), list);
                IC.Insert(new CodeAssignStatement(
                    new CodeVariableReferenceExpression(ArrayName),
                    new CodeCastExpression(typeof(PhpArray), TranslateExpression(list.RValue, method, IC))), list);
                int i = 0;
                foreach (Expression l in list.LValues)
                    if (!(l is EmptyEx))
                        IC.Insert(new CodeAssignStatement(
                            TranslateExpression(l, method, IC),
                            new CodeMethodInvokeExpression(
                                new CodeVariableReferenceExpression(ArrayName),
                                "GetArrayItem",
                                new CodeExpression[]{
                                    new CodePrimitiveExpression(i++),
                                    new CodePrimitiveExpression(false)})), l);
                return new CodeVariableReferenceExpression(ArrayName);
            }
            /// <summary>Translates PHP isset construct to CodeDOM equivalent</summary>
            /// <param name="isset">Expression to translate</param>
            /// <param name="method">GetUserEntryPoint for declaring local variables</param>
            /// <param name="IC">Context for inserting additional statements</param>
            /// <returns>Translated expression</returns>
            /// <remarks>
            /// isset is translated using and-separated list if tests of identity with null
            /// isset($a,$b,$c) is translated as (a != null) &amp;&amp; ((b != null) &amp;&amp; (c != null))  
            /// </remarks>
            protected CodeExpression /*!*/TranslateIsSet(IssetEx /*!*/isset, MethodContextBase /*!*/ method, IStatementInsertContext IC)
            {
                if (isset.VarList.Count <= 0)
                    throw new PhpToCodeDomNotSupportedException(Localizations.Strings.cdp_unsup_isset_no_var, isset);
                else
                    return TranslateIsSetList(isset.VarList, 0, method, IC);
            }
            /// <summary>Translate list of variables in argument list of isset PHP construct to sequence of CodeDOM null tests concatenated using boolean and operator.</summary>
            /// <param name="list">Arguments of isset</param>
            /// <param name="start">Index in <paramref name="list"/> where to start translation</param>
            /// <param name="method">GetUserEntryPoint for declaring local variables</param>
            /// <param name="IC">Context for inserting additional statements</param>
            /// <returns>Translated expression</returns>
            protected CodeExpression /*!*/ TranslateIsSetList(List<VariableUse>/*!*/ list, int start, MethodContextBase /*!*/method, IStatementInsertContext IC)
            {
                if (list.Count < start + 1) throw new ArgumentOutOfRangeException("start", Localizations.Strings.cdp_not_enough_items_in_list);
                else if (start < 0) throw new ArgumentOutOfRangeException("start", Localizations.Strings.cdp_start_less_than_zero);
                else if (start + 1 == list.Count)
                    return new CodeBinaryOperatorExpression(
                        TranslateVariableUse(list[start], method, IC), CodeBinaryOperatorType.IdentityInequality, new CodePrimitiveExpression(null));
                else
                    return new CodeBinaryOperatorExpression(
                        new CodeBinaryOperatorExpression(
                            TranslateVariableUse(list[start], method, IC), CodeBinaryOperatorType.IdentityInequality, new CodePrimitiveExpression(null)),
                        CodeBinaryOperatorType.BooleanAnd,
                        TranslateIsSetList(list, start + 1, method, IC));
            }
            /// <summary>Converts list of expressions contained in <see cref="ConcatEx"/> to recursive CodeDOM expression</summary>
            /// <param name="List">List containing expressions to convert</param>
            /// <param name="Start">Index of expression to start conversion from</param>
            /// <param name="method">GetUserEntryPoint for declaring local variables</param>
            /// <param name="IC">Context for inserting additional statements</param>
            /// <returns><see cref="CodeExpression"/> containin translated expression</returns>
            /// <exception cref="ArgumentOutOfRangeException"><paramref name="List"/> contains less items then <paramref name="Start"/> + 1 =or= <paramref name="Start"/> is less than zero</exception>
            protected CodeExpression TranslateConcatExpressionList(List<Expression> /*!*/ List, int Start, MethodContextBase /*!*/ method, IStatementInsertContext IC)
            {
                if (List.Count <= Start)
                    throw new ArgumentOutOfRangeException("Start", Localizations.Strings.cdp_unsup_not_enough_expressions_in_list);
                else if (Start < 0)
                    throw new ArgumentOutOfRangeException("Start", Localizations.Strings.cdp_start_greater_than_zero);
                else if (List.Count == Start + 1)
                    return TranslateExpression(List[Start], method, IC);
                else
                    return new CodeBinaryOperatorExpression(
                        TranslateExpression(List[Start], method, IC),
                        CodeBinaryOperatorType.Add,
                        TranslateConcatExpressionList(List, Start + 1, method, IC));
            }
            /// <summary>Translates "flat" PHP <see cref="ConcatEx"/> to recursive CodeDOM <see cref="CodeBinaryOperatorExpression"/></summary>
            /// <param name="c">Expression to translate</param>
            /// <param name="method">GetUserEntryPoint for declaring local variables</param>
            /// <param name="IC">Context for inserting additional statements</param>
            /// <returns>Translated expression</returns>
            /// <exception cref="PhpToCodeDomNotSupportedException"><paramref name="c"/>.<see cref="ConcatEx.Expressions">List</see> contains no expression</exception>
            protected CodeExpression TranslateConcatExpression(ConcatEx /*!*/ c, MethodContextBase /*!*/ method, IStatementInsertContext IC)
            {
                if (c.Expressions.Count <= 0)
                    throw new PhpToCodeDomNotSupportedException(Localizations.Strings.cdp_unsup_empty_ex_list, c);
                else
                    return TranslateConcatExpressionList(c.Expressions, 0, method, IC);
            }
            #endregion
            #endregion
            #region Types and signatures
            /// <summary>Translates declaration of generic parameter (in header of type) from PHP to CodeDOM</summary>
            /// <param name="GPar">PHP Generic parameter declaration</param>
            /// <returns>CodeDOM representation of <paramref name="GPar"/></returns>
            protected CodeTypeParameter /*!*/ TranslateGenericParameterDeclaration(GenericParameterDesc /*!*/ GPar)
            {
                CodeTypeParameter ret = new CodeTypeParameter(GPar.Type.FullName);
                return ret;
            }
            /// <summary>Translates declaration of generic parameter (in header of method) from PHP to CodeDOM</summary>
            /// <param name="GPar">PHP Generic parameter declaration</param>
            /// <returns>CodeDOM representation of <paramref name="GPar"/></returns>
            protected CodeTypeParameter /*!*/ TranslateFormalTypeParam(FormalTypeParam /*!*/ GPar)
            {
                CodeTypeParameter ret = new CodeTypeParameter(GPar.Name.Value);
                return ret;
            }
            /// <summary>Translate declaration of formal parameter of method from PHP to CodeDOM</summary>
            /// <param name="Param">Parameter to be translated</param>
            /// <returns><paramref name="Param"/>'s representation in COdeDOM</returns>
            /// <remarks>Ignores parameter initialization!</remarks>
            /// <exception cref="PhpToCodeDomNotSupportedException">Custom attribute initialization contains unsupported construct (<see cref="TranslateAttribute"/>)</exception>
            protected CodeParameterDeclarationExpression /*!*/ TranslateParameter(FormalParam /*!*/ Param)
            {
                CodeParameterDeclarationExpression ret = new CodeParameterDeclarationExpression(
                    Param.TypeHint == null ? new CodeTypeReference(typeof(object)) :
                    Param.TypeHint is PrimitiveType ?
                        new CodeTypeReference(((PrimitiveType)Param.TypeHint).RealType) :
                        TranslateGenericQualifiedName((GenericQualifiedName)Param.TypeHint, true),
                    Param.Name.Value);
                if (Param.Attributes.Attributes != null)
                    foreach (CustomAttribute Attr in Param.Attributes.Attributes)
                    {
                        CodeAttributeDeclaration attr = TranslateAttribute(Attr);
                        if (attr != null) ret.CustomAttributes.Add(attr);
                    }
                //Warning: Parameter initialization is ignored
                if (Param.IsOut) ret.Direction = FieldDirection.Out;
                if (Param.PassedByRef) ret.Direction = FieldDirection.Ref;
                if (Param.IsOut && Param.PassedByRef) ret.Direction = FieldDirection.Ref | FieldDirection.Out;
                return ret;
            }
            /// <summary>Helper delgate for <see cref="TranslateModifiers"/></summary>
            /// <param name="PHP">Member attribute to be compared with internal value using internal type of comparison</param>
            /// <returns>Result of comperison</returns>
            private delegate bool dAnd(PhpMemberAttributes PHP);
            /// <summary>Translates <see cref="PhpMemberAttributes"/> to <see cref="MemberAttributes"/></summary>
            /// <param name="Modifiers">A <see cref="PhpMemberAttributes"/> to translate</param>
            /// <returns><paramref name="Modifiers"/>'s representation in <see cref="MemberAttributes"/></returns>
            protected MemberAttributes /*!*/ TranslateModifiers(PhpMemberAttributes /*!*/ Modifiers)
            {
                dAnd And = delegate(PhpMemberAttributes PHP) { return (Modifiers & PHP) == PHP; };
                //dAnd Eqs = delegate(PhpMemberAttributes PHP) { return (Modifiers & PHP) != 0; };
                dAnd vis = delegate(PhpMemberAttributes PHP) { return (Modifiers & PhpMemberAttributes.VisibilityMask) == PHP; };
                MemberAttributes ret = 0;
                //if(And(PhpMemberAttributes.NamespacePrivate)) ret |= MemberAttributes.Assembly;
                if (vis(PhpMemberAttributes.Private)) ret |= MemberAttributes.Private;
                if (vis(PhpMemberAttributes.Protected)) ret |= MemberAttributes.Family;
                if (vis(PhpMemberAttributes.Public)) ret |= MemberAttributes.Public;

                if (And(PhpMemberAttributes.Abstract)) ret |= MemberAttributes.Abstract;
                if (And(PhpMemberAttributes.AppStatic) || And(PhpMemberAttributes.Static)) ret |= MemberAttributes.Static;
                if (And(PhpMemberAttributes.Final)) ret |= MemberAttributes.Final;
                if (And(PhpMemberAttributes.Interface)) ret |= MemberAttributes.Abstract;

                //if(Eqs(PhpMemberAttributes.Static)) ret |= MemberAttributes.Static;
                return ret;
            }
            /// <summary>Translates <see cref="TypeRef"/> to <see cref="CodeTypeReference"/></summary>
            /// <param name="typeRef"><see cref="TypeRef"/> to be translated</param>
            /// <returns><see cref="CodeTypeReference"/> representation of <paramref name="typeRef"/></returns>
            /// <remarks>Indirect type reference (<see cref="IndirectTypeRef"/> is currently not supported)</remarks>
            /// <exception cref="PhpToCodeDomNotSupportedException">
            /// <paramref name="typeRef"/> is <see cref="IndirectTypeRef"/>
            /// =or=
            /// <paramref name="typeRef"/> is neither <see cref="DirectTypeRef"/> nor <see cref="PrimitiveTypeRef"/>
            /// =or=
            /// some sub-construct is not supported (as comes from <see cref="TranslateTypeRef"/>, <see cref="getCLRName"/>)
            /// </exception>
            /// <param name="context">Context for current-type detrmination. Can be null.</param>
            protected /*!*/CodeTypeReference TranslateTypeRef(TypeRef /*!*/ typeRef, MethodContextBase context)
            {
                if (typeRef is DirectTypeRef)
                {
                    string CLRName = getCLRName(((DirectTypeRef)typeRef).ClassName);
                    CodeTypeReference ret;
                    if (typeRef.GenericParams.Count == 0 && context != null && context.Class != null && context.Class.Context.Name == CLRName)
                    {
                        //Current class hack
                        ret = new CodeTypeReference(/*(string.IsNullOrEmpty(currentNamespace) ? "" : currentNamespace.TrimEnd(new char[] { ':' }).Replace(":::", ".") + ".") +*/CLRName);
                    }
                    else
                    {
                        ret = new CodeTypeReference(CLRName);
                        foreach (TypeRef GPar in typeRef.GenericParams)
                            ret.TypeArguments.Add(TranslateTypeRef(GPar, context));
                    }
                    return ret;
                }
                else if (typeRef is IndirectTypeRef)
                {
                    throw new PhpToCodeDomNotSupportedException(Localizations.Strings.cdp_unsup_indirect_type_ref, typeRef);
                }
                else if (typeRef is PrimitiveTypeRef)
                    return new CodeTypeReference(((PrimitiveTypeRef)typeRef).Type.RealType);
                else throw new PhpToCodeDomNotSupportedException(string.Format(Localizations.Strings.cdp_unsup_unknown_ref_kind), typeRef);
            }
            #endregion
            #region Class-level fields
            /// <summary>Translates declaration of PHP class-level constant to CodeDOM static constant field</summary>
            /// <param name="Const">Constant to be translated</param>
            /// <param name="List">List in which <paramref name="Const"/> is declared</param>
            /// <returns>CodeDOM representation of <paramref name="Const"/></returns>
            /// <exception cref="PhpToCodeDomNotSupportedException">Custom attribute initialization expression or constant initialization expression is not either supported or supported in current context (see <see cref="TranslateExpression"/>)</exception>
            protected CodeMemberField /*!*/ TranslateClassConst(ClassConstantDecl /*!*/ Const, ConstDeclList /*!*/ List)
            {
                CodeMemberField ret = new CodeMemberField(typeof(object), Const.Name.Value);
                ret.Attributes = TranslateModifiers(List.Modifiers);
                ret.Attributes |= MemberAttributes.Static | MemberAttributes.Const;
                if (List.Attributes.Attributes != null)
                    foreach (CustomAttribute attribute in List.Attributes.Attributes)
                    {
                        CodeAttributeDeclaration attr = TranslateAttribute(attribute);
                        if (attribute != null) ret.CustomAttributes.Add(attr);
                    }
                ret.InitExpression = TranslateExpression(Const.Initializer, new MethodContextBase(), null);
                return ret;
            }
            /// <summary>Translates declaration of PHP class-level field to CodeDOM field</summary>
            /// <param name="Field">Field to be translated</param>
            /// <param name="List">List in which <paramref name="Field"/> is declared</param>
            /// <param name="block">If field requires property, property is added to this block</param>
            /// <returns>CodeDOM representation of <paramref name="Field"/></returns>
            /// <exception cref="PhpToCodeDomNotSupportedException">Custom attribute initialization expression or variable initialization expression is not either supported or supported in current context (see <see cref="TranslateExpression"/>)</exception>
            protected CodeMemberField /*!*/ TranslateField(FieldDecl /*!*/ Field, FieldDeclList /*!*/ List, IBlockContext /*!*/ block)
            {
                // <remarks>If filed implements or overrides something, that field-backed property is generated</remarks>
                // Analysis required :-(
                /*if(Field.Field.Implements.Count > 0 || Field.Field.Overrides != null) {
                    CodeMemberField ret = new CodeMemberField(typeof(object), Field.Name.Value);
                    ret.Attributes = TranslateModifiers(List.Modifiers);
                    foreach(CustomAttribute attribute in List.Attributes.Attributes)
                        ret.CustomAttributes.Add(TranslateAttribute(attribute));
                    if(Field.HasInitVal)
                        ret.InitExpression = TranslateExpression(Field.Initializer, new MethodContextBase(),null);
                    CodeMemberProperty FieldProperty = new CodeMemberProperty();
                    string name = ret.Name;
                    ret.Name = "__field__" + name;
                    FieldProperty.Name = name;
                    FieldProperty.CustomAttributes.AddRange(ret.CustomAttributes);
                    FieldProperty.Attributes = ret.Attributes;
                    ret.Attributes &= ~MemberAttributes.Public;
                    ret.Attributes &= ~MemberAttributes.Family;
                    FieldProperty.HasGet = true;
                    FieldProperty.HasSet = true;
                    FieldProperty.GetStatements.Add(new CodeMethodReturnStatement(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), "__field__" + name)));
                    FieldProperty.SetStatements.Add(new CodeAssignStatement(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), "__field__" + name), new CodeVariableReferenceExpression("value")));
                    block.AddObject(FieldProperty);
                    return ret;
                } else {*/
                CodeMemberField ret = new CodeMemberField(typeof(object), Field.Name.Value);
                ret.Attributes = TranslateModifiers(List.Modifiers);
                if (List.Attributes.Attributes != null)
                    foreach (CustomAttribute attribute in List.Attributes.Attributes)
                    {
                        CodeAttributeDeclaration attr = TranslateAttribute(attribute);
                        if (attribute != null) ret.CustomAttributes.Add(attr);
                    }
                if (Field.HasInitVal)
                    ret.InitExpression = TranslateExpression(Field.Initializer, new MethodContextBase(), null);
                return ret;
                /*}*/
            }
            #endregion
            #endregion

            #region Specialized block-level translations
            /// <summary>Translates PHP namespace declarartion and its content to CodeDOM and emits it into containing block</summary>
            /// <param name="sNamespace">Declaration of PHP namespace to be translated</param>
            /// <param name="block">Block this namespace is containded in (should be <see cref="FileContext"/>)</param>
            protected void TranslateNamespace(NamespaceDecl /*!*/ sNamespace, IBlockContext /*!*/ block)
            {
                PushAliases(sNamespace.Aliases);

                CodeNamespace cNamespace = (CodeNamespace)
                    block.AddObject(new CodeNamespace(getCLRName(sNamespace.QualifiedName)), sNamespace);
                currentNamespace = sNamespace.QualifiedName.ToString();
                TranslateBlock(sNamespace.Statements, new MethodContextBase(), new NamespaceContext(cNamespace));

                PopAliases();
            }

            /// <summary>Translates declaration of PHP type to CodeDOM and emits its content</summary>
            /// <param name="sType">PHP type declaration to be translated</param>
            /// <param name="block">Block the type is contained in (should be <see cref="FileContext"/> or <see cref="NamespaceContext"/>)</param>
            /// <remarks>When translating base types and interfaces, first must be translatedd and added to list base class!</remarks>
            protected void TranslateTypeDecl(TypeDecl /*!*/ sType, IBlockContext /*!*/ block)
            {
                CodeTypeDeclaration cType = (CodeTypeDeclaration)
                    block.AddObject(new CodeTypeDeclaration(sType.Name.Value), sType);
                cType.LinePragma = getPragma(sType.Position.FirstLine - 1);
                //Basic settings
                cType.IsClass = true;
                cType.IsEnum = false;
                cType.IsPartial = sType.PartialKeyword;

                //TODO: sType.DocComment
                //Base and implemented types
                //Base class must be first in list!, then interfaces
                if (sType.BaseClassName.HasValue) cType.BaseTypes.Add(TranslateGenericQualifiedName(sType.BaseClassName.Value, true));
                foreach (GenericQualifiedName Interface in sType.ImplementsList)
                    cType.BaseTypes.Add(TranslateGenericQualifiedName(Interface, true));
                //Attributes
                if (sType.Attributes.Attributes != null)
                    foreach (CustomAttribute Attribute in sType.Attributes.Attributes)
                    {
                        CodeAttributeDeclaration attr = TranslateAttribute(Attribute);
                        if (attr != null) cType.CustomAttributes.Add(attr);
                    }
                //Modifiers
                Dictionary<TypeAttributes, bool> typeAttributes = new Dictionary<TypeAttributes, bool>();
                foreach (TypeAttributes val in System.Enum.GetValues(typeof(TypeAttributes)))
                    if (!typeAttributes.ContainsKey(val))
                        typeAttributes.Add(val, false);
                typeAttributes[TypeAttributes.Class] = !sType.Type.IsValueType;
                cType.IsStruct = sType.Type.IsValueType;
                typeAttributes[TypeAttributes.Abstract] = sType.Type.IsAbstract;
                typeAttributes[TypeAttributes.Public] = sType.Type.IsPublic;
                //typeAttributes[TypeAttributes.NestedFamily] = sType.Type.IsProtected;
                //typeAttributes[TypeAttributes.NestedPrivate] = sType.Type.IsPrivate;
                typeAttributes[TypeAttributes.Interface] = sType.Type.IsInterface;
                typeAttributes[TypeAttributes.NotPublic] = sType.Type.IsPrivate;
                typeAttributes[TypeAttributes.Sealed] = sType.Type.IsFinal;
                TypeAttributes ta = 0;
                foreach (KeyValuePair<TypeAttributes, bool> attr in typeAttributes) if (attr.Value) ta |= attr.Key;
                cType.TypeAttributes = ta;

                //Generic information
                if (sType.Type.IsGeneric)
                {
                    foreach (GenericParameterDesc GPar in sType.Type.GenericParams)
                    {
                        cType.TypeParameters.Add(TranslateGenericParameterDeclaration(GPar));
                    }
                }
                //Translate members
                if (sType.Namespace == null)
                    currentClass = sType.Name.ToString();
                else currentClass = sType.Namespace.QualifiedName.ToString() + sType.Name.ToString();
                TranslateBlock(sType.Members, new TypeContext(cType));
            }

            /// <summary>Translates header of method from PHP to CodeDOM</summary>
            /// <param name="Method">GetUserEntryPoint to translate</param>
            /// <param name="block">Block this method is member of</param>
            /// <exception cref="PhpToCodeDomNotSupportedException">
            /// GetUserEntryPoint cannot be added to <paramref name="block"/>.
            /// =or=
            /// Some statement contained in method is not supported or consists of unsupported constructs (see <see cref="TranslateStatement"/>)
            /// =or=
            /// Some construct in method header is usupported (see <see cref="TranslateAttribute"/>,<see cref="TranslateParameter"/>
            /// </exception>
            protected void TranslateMethod(MethodDecl /*!*/ Method, IBlockContext /*!*/ block)
            {
                CodeMemberMethod cMethod;
                if (Method.Name.IsConstructName)
                {
                    cMethod = new CodeConstructor();
                    if (Method.BaseCtorParams != null)
                        ((CodeConstructor)cMethod).BaseConstructorArgs.AddRange(TranslateParams(Method.BaseCtorParams, new MethodContextBase(), null, null));
                }
                else
                    cMethod = new CodeMemberMethod();
                cMethod.LinePragma = getPragma(Method.Position.FirstLine - 1);
                cMethod.Name = Method.Name.Value;
                if (Method.Attributes.Attributes != null)
                    foreach (CustomAttribute Attr in Method.Attributes.Attributes)
                    {
                        CodeAttributeDeclaration attr = TranslateAttribute(Attr);
                        if (attr != null)
                        {
                            if (Attr.TargetSelector == CustomAttribute.TargetSelectors.Return)
                                cMethod.ReturnTypeCustomAttributes.Add(attr);
                            else
                                cMethod.CustomAttributes.Add(attr);
                        }
                    }
                cMethod.Attributes = TranslateModifiers(Method.Modifiers);
                cMethod.ReturnType = new CodeTypeReference(typeof(object));
                foreach (FormalTypeParam TParam in Method.TypeSignature.TypeParams)
                    cMethod.TypeParameters.Add(TranslateFormalTypeParam(TParam));
                foreach (FormalParam Param in Method.Signature.FormalParams)
                    cMethod.Parameters.Add(TranslateParameter(Param));
                block.AddObject(cMethod, Method);
                MethodContext methodContext = new MethodContext(cMethod, block);
                currentFunction = Method.Name.Value;
                currentMethod = currentClass + "::" + Method.Name.Value;
                TranslateBlock(Method.Body, methodContext, methodContext);
            }

            /// <summary>Translates list of class-level declared contants into CodeDOM declarations of constant fields</summary>
            /// <param name="List">List of constants to translate</param>
            /// <param name="block">Block representing type in which constants are declared</param>
            /// <exception cref="PhpToCodeDomNotSupportedException">Custom attribute initialization expression or constant initialization expression is not either supported or supported in current context (see <see cref="TranslateClassConst"/>)</exception>
            protected void TranslateMemberList(ConstDeclList /*!*/ List, /*!*/ IBlockContext block)
            {
                foreach (ClassConstantDecl Const in List.Constants)
                {
                    var TConst = TranslateClassConst(Const, List);
                    TConst.LinePragma = getPragma(Const.Position.FirstLine - 1);
                    block.AddObject(TConst, List);
                }
            }

            /// <summary>Translates list of class-level declared fields into CodeDOM declarations of fields</summary>
            /// <param name="List">List of fields to translate</param>
            /// <param name="block">Block representing type in which fields are declared</param>
            /// <exception cref="PhpToCodeDomNotSupportedException">Custom attribute initialization expression or variable initialization expression is not either supported or supported in current context (see <see cref="TranslateField"/>)</exception>        
            protected void TranslateMemberList(FieldDeclList /*!*/ List, /*!*/ IBlockContext block)
            {
                foreach (FieldDecl Field in List.Fields)
                {
                    var TField = TranslateField(Field, List, block);
                    TField.LinePragma = getPragma(Field.Position.FirstLine - 1);
                    block.AddObject(TField, List);
                }
            }
            #endregion

            #region Specialized statement-level translations
            #region Multi-line
            /// <summary>Translates while statement from PHP to CodeDOM</summary><param name="statement">Statement to translate</param><param name="method">GetUserEntryPoint for declaring local variables</param><param name="block">Block for adding statements</param>
            /// <exception cref="PhpToCodeDomNotSupportedException">Expression or statement used in this statement is not supported (see <see cref="TranslateExpression"/>, <see cref="TranslateBlock(IEnumerable&lt;Statement>,MethodContextBase,IBlockContext)"/>)</exception>
            protected void TranslateStatement(WhileStmt statement, MethodContextBase method, IBlockContext block)
            {
                MethodContext Method = null; ICodeBlockContext Block = null;
                MethodAndBlock(method, block, ref Method, ref Block, statement);
                if (Block != null) Block.ResetInsertContextToEnd();
                string Label1 = LabelName(Loops.While, true);
                string Label2 = LabelName(Loops.While, false);
                CodeIterationStatement For = new CodeIterationStatement();
                For.LinePragma = getPragma(statement.Position.FirstLine - 1);
                if (statement.CondExpr == null || statement.LoopType == WhileStmt.Type.Do)
                    For.TestExpression = new CodePrimitiveExpression(true);
                else
                    For.TestExpression = TranslateExpression(statement.CondExpr, method, Block);
                //For.InitStatement=new CodeExpressionStatement(new CodePrimitiveExpression(null));
                //For.IncrementStatement=new CodeExpressionStatement( new CodePrimitiveExpression(null));
                ForStatementContext context = new ForStatementContext(Method, Block as BlockStatementContext, For, Label2, Label1, this);
                TranslateBlock(new List<Statement>(new Statement[] { statement.Body }), method, context);
                var l1 = new CodeLabeledStatement(Label1);
                l1.LinePragma = getPragma(statement.Position.FirstLine - 1);
                For.Statements.Add(l1);
                block.AddObject(For, statement);
                if (statement.LoopType == WhileStmt.Type.Do && statement.CondExpr != null)
                {
                    context.ResetInsertContextToEnd();
                    var condition = new CodeConditionStatement(
                        new CodeBinaryOperatorExpression(
                            TranslateExpression(statement.CondExpr, Method, context), CodeBinaryOperatorType.ValueEquality, new CodePrimitiveExpression(false)),
                        new CodeStatement[] { new CodeGotoStatement(Label2) });
                    condition.LinePragma = getPragma(statement.Position.LastLine - 1);
                    context.AddObject(condition, statement);
                }
                var l2 = new CodeLabeledStatement(Label2);
                l2.LinePragma = getPragma(statement.Position.LastLine - 1);
                block.AddObject(l2, statement);
            }
            /// <summary>Translates block statement from PHP to CodeDOM</summary><param name="statement">Statement to translate</param><param name="method">GetUserEntryPoint for declaring local variables</param><param name="block">Block for adding statements</param>
            /// <remarks>Blocks are flattened</remarks>
            /// <exception cref="PhpToCodeDomNotSupportedException">Some statemen is not supported <see cref="TranslateBlock(IEnumerable&lt;Statement>,MethodContextBase,IBlockContext)"/></exception>
            protected void TranslateStatement(BlockStmt statement, MethodContextBase method, IBlockContext block)
            {
                TranslateBlock(statement.Statements, method, block);
            }
            private static int foreachcnt = 0;
            /// <summary>Translates for-each statement from PHP to CodeDOM</summary><param name="statement">Statement to translate</param><param name="method">GetUserEntryPoint for declaring local variables</param><param name="block">Block for adding statements</param>
            /// <exception cref="PhpToCodeDomNotSupportedException">Expression or statement used in this statement is not supported (see <see cref="TranslateExpression"/>, <see cref="TranslateBlock(IEnumerable&lt;Statement>,MethodContextBase,IBlockContext)"/>, <see cref="TranslateVariableUse"/>)</exception>
            protected void TranslateStatement(ForeachStmt statement, MethodContextBase method, IBlockContext block)
            {
                MethodContext Method = null; ICodeBlockContext Block = null;
                MethodAndBlock(method, block, ref  Method, ref Block, statement);
                if (Block != null) Block.ResetInsertContextToEnd();
                string Array2 = string.Format("__foreach_copy_of_array__{0:000}", foreachcnt);
                string ContInd = string.Format("__foreach_continue_indicator__{0:000}", foreachcnt++);
                string Label1 = LabelName(Loops.Foreach, true);
                string Label2 = LabelName(Loops.Foreach, false);
                var declarearray2 = new CodeVariableDeclarationStatement(typeof(PhpArray), Array2);
                declarearray2.LinePragma = getPragma(statement.Position.FirstLine - 1);
                block.AddObject(declarearray2, statement);
                var initarray2 = new CodeAssignStatement(new CodeVariableReferenceExpression(Array2),
                    TranslateExpression(statement.Enumeree, Method, Block));
                initarray2.LinePragma = getPragma(statement.Position.FirstLine - 1);
                block.AddObject(initarray2, statement);
                CodeTypeReferenceExpression PhpArrays = new CodeTypeReferenceExpression("PHP.Library.PhpArrays");
                CodeIterationStatement For = new CodeIterationStatement(
                    new CodeVariableDeclarationStatement(
                        typeof(object), ContInd, new CodeMethodInvokeExpression(
                            PhpArrays, "Reset", new CodeExpression[] { new CodeVariableReferenceExpression(Array2) })),
                    new CodeBinaryOperatorExpression(
                        new CodeVariableReferenceExpression(ContInd), CodeBinaryOperatorType.IdentityInequality,
                        new CodePrimitiveExpression(false)),
                    new CodeAssignStatement(new CodeVariableReferenceExpression(ContInd),
                        new CodeMethodInvokeExpression(PhpArrays, "Next", new CodeExpression[]{
                            new CodeVariableReferenceExpression(Array2)})));
                For.LinePragma = getPragma(statement.Position.FirstLine - 1);
                ForStatementContext context = new ForStatementContext(Method, Block as BlockStatementContext, For, Label2, Label1, this);
                block.AddObject(For, statement);
                if (statement.KeyVariable != null)
                {
                    var Assignkey = new CodeAssignStatement(
                            TranslateVariableUse(statement.KeyVariable.Variable, method, context),
                            new CodeMethodInvokeExpression(PhpArrays, "Key",
                                new CodeExpression[] { new CodeVariableReferenceExpression(Array2) }));
                    Assignkey.LinePragma = getPragma(statement.Position.FirstLine - 1);
                    For.Statements.Add(Assignkey);
                }
                var assingcurrent = new CodeAssignStatement(
                        TranslateVariableUse(statement.ValueVariable.Variable, method, context),
                        new CodeMethodInvokeExpression(PhpArrays, "Current",
                        new CodeExpression[] { new CodeVariableReferenceExpression(Array2) }));
                assingcurrent.LinePragma = getPragma(statement.Position.FirstLine - 1);
                For.Statements.Add(assingcurrent);
                TranslateBlock(new Statement[] { statement.Body }, method, context);
                var label1 = new CodeLabeledStatement(Label1);
                label1.LinePragma = getPragma(statement.Position.FirstLine - 1);
                For.Statements.Add(label1);
                var label2 = new CodeLabeledStatement(Label2);
                label2.LinePragma = getPragma(statement.Position.FirstLine - 1);
                block.AddObject(label2, statement);
            }
            /// <summary>Translates for statement from PHP to CodeDOM</summary><param name="statement">Statement to translate</param><param name="method">GetUserEntryPoint for declaring local variables</param><param name="block">Block for adding statements</param>
            /// <exception cref="PhpToCodeDomNotSupportedException">
            /// Parent block does not support adding of required statements.
            /// =or=
            /// Any sub-statement or expression in for loop is not supported (see <see cref="TranslateBlock(IEnumerable&lt;Statement>,MethodContextBase,IBlockContext)"/>, <see cref="TranslateExpression"/>)
            /// </exception>
            protected void TranslateStatement(ForStmt statement, MethodContextBase method, IBlockContext block)
            {
                MethodContext Method = null; ICodeBlockContext Block = null;
                MethodAndBlock(method, block, ref  Method, ref Block, statement);
                if (Block != null) Block.ResetInsertContextToEnd();
                CodeIterationStatement For = new CodeIterationStatement();
                For.LinePragma = getPragma(statement.Position.FirstLine - 1);
                if (statement.InitExList.Count > 1)//If there is more than 1 initializations
                    for (int i = 0; i < statement.InitExList.Count - 1; i++)
                        block.AddObject(TranslateExpression(statement.InitExList[i], Method, Block), statement);
                if (statement.InitExList.Count > 0)
                {
                    For.InitStatement = new CodeExpressionStatement(TranslateExpression(statement.InitExList[statement.InitExList.Count - 1], Method, Block));
                    For.InitStatement.LinePragma = getPragma(statement.InitExList[statement.InitExList.Count - 1].Position.FirstLine - 1);
                }
                if (statement.CondExList.Count > 1)//If there is more than one 'conditions'
                    for (int i = 0; i < statement.CondExList.Count - 1; i++)
                        block.AddObject(TranslateExpression(statement.CondExList[i], Method, Block), statement);
                if (statement.CondExList.Count > 0)
                    For.TestExpression = TranslateExpression(statement.CondExList[statement.CondExList.Count - 1], Method, Block);
                block.AddObject(For, statement);
                string Label1 = LabelName(Loops.For, true);
                string Label2 = LabelName(Loops.For, false);
                ForStatementContext context = new ForStatementContext(Method, Block as BlockStatementContext, For, Label2, Label1, this);
                TranslateBlock(new List<Statement>(new Statement[] { statement.Body }), method, context);
                For.Statements.Add(new CodeLabeledStatement(Label1));
                context.ResetInsertContextToEnd();
                if (statement.ActionExList.Count > 1)//If there is more than one incementations
                    for (int i = 0; i < statement.ActionExList.Count - 1; i++)
                        context.AddObject(TranslateExpression(statement.ActionExList[i], method, context), statement);
                if (statement.ActionExList.Count > 0)
                {
                    For.IncrementStatement = new CodeExpressionStatement(TranslateExpression(statement.ActionExList[statement.ActionExList.Count - 1], method, context));
                    For.IncrementStatement.LinePragma = getPragma(statement.ActionExList[statement.ActionExList.Count - 1].Position.FirstLine - 1);
                }
                if (statement.CondExList.Count > 1)//If there is more than one condition
                    for (int i = 0; i < statement.CondExList.Count - 1; i++)
                        context.AddObject(TranslateExpression(statement.CondExList[i], method, context), statement);
                block.AddObject(new CodeLabeledStatement(Label2), statement);
            }
            /// <summary>Translates if statement from PHP to CodeDOM</summary><param name="statement">Statement to translate</param><param name="method">GetUserEntryPoint for declaring local variables</param><param name="block">Block for adding statements</param>
            /// <exception cref="PhpToCodeDomNotSupportedException">Part of If statement is not supported (see <see cref="TranslateConditions"/>) =or= if statement cannot be placed in current block =or= <paramref name="method"/> is not <see cref="MethodContext"/></exception>
            protected void TranslateStatement(IfStmt statement, MethodContextBase method, IBlockContext block)
            {
                MethodContext Method = null; ICodeBlockContext Block = null;
                MethodAndBlock(method, block, ref  Method, ref Block, statement);
                if (Block != null) Block.ResetInsertContextToEnd();
                TranslateConditions(statement.Conditions, 0, Method, block);
            }
            /// <summary>Counter for labels used by <see cref="TranslateStatement(SwitchStmt,MethodContextBase,IBlockContext)"/></summary>
            protected int switch_case = 0;
            /// <summary>Translates switch statement from PHP to CodeDOM</summary><param name="statement">Statement to translate</param><param name="method">GetUserEntryPoint for declaring local variables</param><param name="block">Block for adding statements</param>
            /// <remarks>Switch is translates to if-s and goto-s</remarks>
            /// <exception cref="PhpToCodeDomNotSupportedException">
            /// Item of switch statement is neither <see cref="CaseItem"/> nor <see cref="DefaultItem"/>
            /// =or=
            /// Parent block does not support adding required statements
            /// =or=
            /// Statement or expression insite switch statement is not supported (see <see cref="TranslateBlock(IEnumerable&lt;Statement>,MethodContextBase,IBlockContext)"/>, <see cref="TranslateExpression"/>)
            /// </exception>
            protected void TranslateStatement(SwitchStmt statement, MethodContextBase method, IBlockContext block)
            {
                MethodContext Method = null; ICodeBlockContext Block = null;
                MethodAndBlock(method, block, ref  Method, ref Block, statement);
                if (Block != null) Block.ResetInsertContextToEnd();
                string SwitchVar = SwitchVarName;
                var switchvar = new CodeVariableDeclarationStatement(typeof(object), SwitchVar);
                switchvar.LinePragma = getPragma(statement.Position.FirstLine - 1);
                block.AddObject(switchvar, statement);
                var initswitchvar = new CodeAssignStatement(
                    new CodeVariableReferenceExpression(SwitchVar),
                    TranslateExpression(statement.SwitchValue, Method, Block));
                initswitchvar.LinePragma = getPragma(statement.Position.FirstLine - 1);
                block.AddObject(initswitchvar, statement);
                string Label2 = LabelName(Loops.Switch, false);
                int switchNo = switch_case++;
                for (int i = 0; i < statement.SwitchItems.Count; i++)
                {
                    SwitchItem CurrentItem = statement.SwitchItems[i];
                    CodeExpression Condition;
                    if (CurrentItem is CaseItem)
                        Condition = new CodeBinaryOperatorExpression(
                            new CodeVariableReferenceExpression(SwitchVar),
                            CodeBinaryOperatorType.ValueEquality,
                            TranslateExpression(((CaseItem)CurrentItem).CaseVal, Method, Block));
                    else if (CurrentItem is DefaultItem)
                        Condition = new CodePrimitiveExpression(true);
                    else throw new PhpToCodeDomNotSupportedException(string.Format(Localizations.Strings.cdp_unsup_unknown_switch, CurrentItem.GetType().FullName), CurrentItem);
                    CodeConditionStatement CurrentIf = new CodeConditionStatement(Condition);
                    CurrentIf.LinePragma = getPragma(CurrentItem.Position.FirstLine - 1);
                    CurrentIf.TrueStatements.Add(new CodeLabeledStatement(
                        string.Format("__switch__{0:000}__case{1:000}", switchNo, i)));
                    CurrentIf.TrueStatements[CurrentIf.TrueStatements.Count - 1].LinePragma = getPragma(CurrentItem.Position.FirstLine - 1);
                    CaseContext context = new CaseContext(Method, Block as BlockStatementContext, CurrentIf, Label2, this);
                    block.AddObject(CurrentIf, statement);
                    TranslateBlock(CurrentItem.Statements, method, context);
                    if (i < statement.SwitchItems.Count - 1)
                    {
                        var GoTo = new CodeGotoStatement(string.Format("__switch__{0:000}__case{1:000}", switchNo, i + 1));
                        GoTo.LinePragma = getPragma(statement.SwitchItems[i].Position.LastLine - 1);
                        CurrentIf.TrueStatements.Add(GoTo);
                    }
                }
                var label2 = new CodeLabeledStatement(Label2);
                label2.LinePragma = getPragma(statement.Position.LastLine - 1);
                block.AddObject(label2, statement);
            }
            /// <summary>Translates try statement from PHP to CodeDOM</summary><param name="statement">Statement to translate</param><param name="method">GetUserEntryPoint for declaring local variables</param><param name="block">Block for adding statements</param>
            /// <exception cref="PhpToCodeDomNotSupportedException">Some part of statement is not supported (see <see cref="TranslateBlock(IEnumerable&lt;Statement>,MethodContextBase,IBlockContext)"/>, <see cref="TranslateDirectVarUse"/>)</exception>
            protected void TranslateStatement(TryStmt statement, MethodContextBase method, IBlockContext block)
            {
                MethodContext Method = null; ICodeBlockContext Block = null;
                MethodAndBlock(method, block, ref  Method, ref Block, statement);
                CodeTryCatchFinallyStatement Try = new CodeTryCatchFinallyStatement();
                Try.LinePragma = getPragma(statement.Position.FirstLine - 1);
                block.AddObject(Try, statement);
                TryStatementContext TryContext = new TryStatementContext(Method, Block as BlockStatementContext, Try, this);
                TranslateBlock(statement.Statements, Method, TryContext);
                foreach (CatchItem Catch in statement.Catches)
                {
                    Try.CatchClauses.Add(new CodeCatchClause(TranslateDirectVarUse(Catch.Variable, Method), TranslateGenericQualifiedName(Catch.ClassName, true)));
                    CatchStatementContext @catch = new CatchStatementContext(Method, Block as BlockStatementContext, Try, Try.CatchClauses.Count - 1, this);
                    TranslateBlock(Catch.Statements, Method, @catch);
                }
            }
            #endregion
            #region Single-line
            /// <summary>Translates empty statement from PHP to CodeDOM</summary><param name="statement">Statement to translate</param><param name="method">GetUserEntryPoint for declaring local variables</param><param name="block">Block for adding statements</param>
            /// <remarks>This method actually does nothing</remarks>
            protected void TranslateStatement(EmptyStmt statement, MethodContextBase method, IBlockContext block)
            {
                //Do nothing
            }
            /// <summary>Translates echo statement from PHP to CodeDOM</summary><param name="statement">Statement to translate</param><param name="method">GetUserEntryPoint for declaring local variables</param><param name="block">Block for adding statements</param>
            /// <exception cref="PhpToCodeDomNotSupportedException">Statement cannot be added to current block =or= Expression is not supported (see <see cref="TranslateExpression"/>)</exception>
            protected void TranslateStatement(EchoStmt statement, MethodContextBase method, IBlockContext block)
            {
                foreach (Expression PHPExpr in statement.Parameters)
                {
                    CodeExpressionStatement stm = new CodeExpressionStatement(
                        new CodeMethodInvokeExpression(
                            CurrentContext, "Echo",
                            new CodeExpression[] { TranslateExpression(PHPExpr, method, getIC(block)) }));
                    stm.LinePragma = getPragma(statement.Position.FirstLine - 1);
                    block.AddObject(stm, statement);
                }
            }
            /// <summary>Translates expression statement from PHP to CodeDOM</summary><param name="statement">Statement to translate</param><param name="method">GetUserEntryPoint for declaring local variables</param><param name="block">Block for adding statements</param>
            /// <exception cref="PhpToCodeDomNotSupportedException">Statement cannot be added to curent block =or= Part of expression is not supported (see <see cref="TranslateExpression"/>)</exception>
            /// <remarks>If expression contained in statement is <see cref="ValueAssignEx"/> then <see cref="CodeAssignStatement"/> is produced; otheriwise <see cref="CodeExpressionStatement"/>.</remarks>
            protected void TranslateStatement(ExpressionStmt statement, MethodContextBase method, IBlockContext block)
            {
                const string TypeSet = "TypeSet";
                if (statement.Expression is ValueAssignEx)
                {
                    CodeAssignStatement cas = new CodeAssignStatement(
                        TranslateVariableUse(((ValueAssignEx)statement.Expression).LValue, method, getIC(block)),
                        TranslateExpression(((ValueAssignEx)statement.Expression).RValue, method, getIC(block)));
                    cas.LinePragma = getPragma(statement.Position.FirstLine - 1);
                    block.AddObject(cas, statement);
                    //this members and local variables typing
                    //this is not necessary for WinForms itself designer to work, but it is nice side effect of necessity to track types in order to distinguish fields and properties when member is referenced. 
                    if (
                        cas.Left is CodeFieldReferenceExpression &&
                        ((CodeFieldReferenceExpression)cas.Left).TargetObject is CodeThisReferenceExpression &&
                        cas.Right is CodeObjectCreateExpression &&
                        method.Class != null &&
                        method.Class.getPropertyOrField(((CodeFieldReferenceExpression)cas.Left).FieldName) != null &&
                        method.Class.getPropertyOrField(((CodeFieldReferenceExpression)cas.Left).FieldName) is CodeMemberField &&
                        !method.Class.getPropertyOrField(((CodeFieldReferenceExpression)cas.Left).FieldName).UserData.Contains(TypeSet)
                    )
                    {//Types of this class members
                        CodeMemberField cmf = (CodeMemberField)method.Class.getPropertyOrField(((CodeFieldReferenceExpression)cas.Left).FieldName);
                        CodeFieldReferenceExpression thf = (CodeFieldReferenceExpression)cas.Left;
                        if (TypeEquals(cmf.Type, new CodeTypeReference(typeof(object))))
                        {
                            cmf.Type = ((CodeObjectCreateExpression)cas.Right).CreateType;
                            cmf.UserData.Add(TypeSet, true);
                        }
                        else if (!TypeEquals(cmf.Type, ((CodeObjectCreateExpression)cas.Right).CreateType))
                        {
                            cmf.Type = new CodeTypeReference(typeof(object));
                            cmf.UserData.Add(TypeSet, true);
                        }
                    }
                    else if (
                       cas.Left is CodeVariableReferenceExpression &&
                       cas.Right is CodeObjectCreateExpression &&
                       method.Contains(((CodeVariableReferenceExpression)cas.Left).VariableName) &&
                       !method[((CodeVariableReferenceExpression)cas.Left).VariableName].UserData.Contains(TypeSet)
                   )
                    {//Types of local variables
                        if (
                            TypeEquals(method[((CodeVariableReferenceExpression)cas.Left).VariableName].Type, new CodeTypeReference(typeof(object)))
                        )
                        {
                            method[((CodeVariableReferenceExpression)cas.Left).VariableName].Type = ((CodeObjectCreateExpression)cas.Right).CreateType;
                            method[((CodeVariableReferenceExpression)cas.Left).VariableName].UserData.Add(TypeSet, true);
                        }
                        else if (
                           !TypeEquals(method[((CodeVariableReferenceExpression)cas.Left).VariableName].Type, ((CodeObjectCreateExpression)cas.Right).CreateType)
                       )
                        {
                            method[((CodeVariableReferenceExpression)cas.Left).VariableName].Type = new CodeTypeReference(typeof(object));
                            method[((CodeVariableReferenceExpression)cas.Left).VariableName].UserData.Add(TypeSet, true);
                        }
                    }
                }
                else if (
                      (EventMode == EventModes.AllPossible || EventMode == EventModes.WithDelegateOnly) &&
                      statement.Expression is DirectFcnCall &&
                      (((DirectFcnCall)statement.Expression).QualifiedName.Name.Value == "Remove" || ((DirectFcnCall)statement.Expression).QualifiedName.Name.Value == "Add") &&
                      ((DirectFcnCall)statement.Expression).CallSignature.GenericParams.Count == 0 &&
                      ((DirectFcnCall)statement.Expression).CallSignature.Parameters.Count == 1 &&
                      (EventMode == EventModes.AllPossible || (((DirectFcnCall)statement.Expression).CallSignature.Parameters[0].Expression is NewEx && LooksLikeDelegate((NewEx)((DirectFcnCall)statement.Expression).CallSignature.Parameters[0].Expression))) &&
                      ((DirectFcnCall)statement.Expression).IsMemberOf is DirectVarUse
                      )
                    TranslateEvent((DirectFcnCall)statement.Expression, method, block);
                else
                {
                    var ces = new CodeExpressionStatement(TranslateExpression(statement.Expression, method, getIC(block)));
                    ces.LinePragma = getPragma(statement.Position.FirstLine - 1);
                    block.AddObject(ces, statement);
                }
            }
            /// <summary>Translate event attach or detach from PHP to CodeDOM</summary>
            /// <param name="dfc">Expression used to attach/detach event in PHP</param>
            /// <param name="method">GetUserEntryPoint for declaring local variables</param>
            /// <param name="block">Block for inserting statements</param>
            /// <exception cref="PhpToCodeDomNotSupportedException">
            /// Any sub-construct is not supported (see <see cref="TranslateExpression"/>, <see cref="TranslateVarLikeConstructUse"/>)
            /// =or=
            /// Name of event accessor is neither Add nor Remove
            /// =or=
            /// <paramref name="dfc"/> is invalid
            /// </exception>
            /// <remarks>
            /// PHP adds/removes event listeners using syntax (target)->EventName->Add/Remove((listener)) this requires <paramref name="dfc"/> to:
            /// <see cref="DirectFcnCall.QualifiedName">QualifiedName</see> is 'Add' or 'Remove',
            /// <see cref="FunctionCall.CallSignature">CallSignature</see>.<see cref="CallSignature.GenericParams">GenericParams</see> is empty,
            /// <see cref="FunctionCall.CallSignature">CallSignature</see>.<see cref="CallSignature.Parameters">Parameters</see> has one item - the evcent listener,
            /// <see cref="VarLikeConstructUse.IsMemberOf">IsMemberOf</see> is <see cref="DirectVarUse"/> with name of the event in <see cref="DirectVarUse.VarName">VarName</see>,
            /// <see cref="VarLikeConstructUse.IsMemberOf">IsMemberOf</see>.<see cref="VarLikeConstructUse.IsMemberOf">IsMemberOf</see> is not null (it is target of event).
            /// </remarks>
            protected void TranslateEvent(DirectFcnCall dfc, MethodContextBase method, IBlockContext block)
            {
                try
                {
                    CodeExpression target = TranslateVarLikeConstructUse(((DirectVarUse)dfc.IsMemberOf).IsMemberOf, method, getIC(block));
                    string name = ((DirectVarUse)dfc.IsMemberOf).VarName.Value;
                    CodeExpression @delegate = TranslateExpression(dfc.CallSignature.Parameters[0].Expression, method, getIC(block));
                    switch (dfc.QualifiedName.Name.Value)
                    {
                        case "Add":
                            var stm = new CodeAttachEventStatement(target, name, @delegate);
                            stm.LinePragma = getPragma(dfc.Position.FirstLine - 1);
                            block.AddObject(stm, dfc);
                            return;
                        case "Remove":
                            var stmR = new CodeRemoveEventStatement(target, name, @delegate);
                            stmR.LinePragma = getPragma(dfc.Position.FirstLine - 1);
                            block.AddObject(stmR, dfc);
                            return;
                        default: throw new PhpToCodeDomNotSupportedException(Localizations.Strings.cdp_unsup_unknown_event_op, dfc);
                    }
                }
                catch (Exception ex)
                {
                    throw new PhpToCodeDomNotSupportedException(Localizations.Strings.cdp_unsup_invalid_event, ex, dfc);
                }
            }
            /// <summary>Translates goto statement from PHP to CodeDOM</summary><param name="statement">Statement to translate</param><param name="method">GetUserEntryPoint for declaring local variables</param><param name="block">Block for adding statements</param>
            /// <exception cref="PhpToCodeDomNotSupportedException">Statement cannot be added into current block</exception>
            protected void TranslateStatement(GotoStmt statement, MethodContextBase method, IBlockContext block)
            {
                CodeGotoStatement GoTo = new CodeGotoStatement(statement.LabelName.Value);
                GoTo.LinePragma = getPragma(statement.Position.FirstLine - 1);
                block.AddObject(GoTo, statement);
            }
            /// <summary>Translates jump statement from PHP to CodeDOM</summary><param name="statement">Statement to translate</param><param name="method">GetUserEntryPoint for declaring local variables</param><param name="block">Block for adding statements</param>
            /// <exception cref="PhpToCodeDomNotSupportedException">
            /// Break/continue argument is not integral literal.
            /// =or=
            /// Current block does not support adding goto statements (for break/continue).
            /// =or=
            /// Current block does not (correctly) support adding of <see cref="CodeBreakTargetRequest"/> (for break/continue).
            /// =or=
            /// Current block does not support reaturn statements (for return)
            /// =or=
            /// Return expression is not supported (as goes from <see cref="TranslateExpression"/>)
            /// </exception>
            protected void TranslateStatement(JumpStmt statement, MethodContextBase method, IBlockContext block)
            {
                if (statement.Type == JumpStmt.Types.Return)
                {
                    if (statement.Expression == null)
                    {
                        var ret = new CodeMethodReturnStatement();
                        ret.LinePragma = getPragma(statement.Position.FirstLine - 1);
                        block.AddObject(ret, statement);
                    }
                    else
                    {
                        var ret = new CodeMethodReturnStatement(TranslateExpression(statement.Expression, method, block is IStatementInsertContext ? (IStatementInsertContext)block : null));
                        ret.LinePragma = getPragma(statement.Position.FirstLine - 1);
                        block.AddObject(ret, statement);
                    }
                }
                else
                {
                    int levels;
                    if (statement.Expression == null) levels = 1;
                    else if (statement.Expression is IntLiteral) levels = (int)((IntLiteral)statement.Expression).Value;
                    else if (statement.Expression is LongIntLiteral) levels = (int)(long)((LongIntLiteral)statement.Expression).Value;
                    else throw new PhpToCodeDomNotSupportedException(Localizations.Strings.cdp_unsup_break_non_constant, statement);
                    CodeBreakTargetRequest btr = new CodeBreakTargetRequest(levels, statement.Type, statement);
                    block.AddObject(btr, statement);
                    if (btr.Target != null && btr.Target != "")
                    {
                        var GoTo = new CodeGotoStatement(btr.Target);
                        GoTo.LinePragma = getPragma(statement.Position.FirstLine - 1);
                        block.AddObject(GoTo, statement);
                    }
                    else
                        throw new PhpToCodeDomNotSupportedException(Localizations.Strings.cdp_unsup_no_jump_label, statement);
                }
            }
            /// <summary>Translates label statement from PHP to CodeDOM</summary><param name="statement">Statement to translate</param><param name="method">GetUserEntryPoint for declaring local variables</param><param name="block">Block for adding statements</param>
            /// <exception cref="PhpToCodeDomNotSupportedException">Current block does not allow adding of this type of statement</exception>
            protected void TranslateStatement(LabelStmt statement, MethodContextBase method, IBlockContext block)
            {
                CodeLabeledStatement lbl = new CodeLabeledStatement(statement.Name.Value);
                lbl.LinePragma = getPragma(statement.Position.FirstLine - 1);
                block.AddObject(lbl, statement);
            }
            /// <summary>Translates static statement from PHP to CodeDOM</summary><param name="statement">Statement to translate</param><param name="method">GetUserEntryPoint for declaring local variables</param><param name="block">Block for adding statements</param>
            /// <exception cref="PhpToCodeDomNotSupportedException">
            /// <paramref name="method"/> is not <see cref="MethodContext"/>.
            /// =or=
            /// Parent of <paramref name="method"/> does not accept field declaration.
            /// =or=
            /// There are two static variables with same name in the method.
            /// =or=
            /// <see cref="TranslateExpression"/> does not support expression used to initialize the variable (if any).
            /// </exception>
            protected void TranslateStatement(StaticStmt statement, MethodContextBase method, IBlockContext block)
            {
                if (method is MethodContext)
                {
                    foreach (StaticVarDecl var in statement.StVarList)
                    {
                        string stvName = string.Format("__static_variable__{0}__for__{1}", var.Variable.VarName.Value, ((MethodContext)method).Context.Name);
                        if (!((MethodContext)method).StaticVariables.ContainsKey(var.Variable.VarName.Value))
                        {
                            var field = new CodeMemberField(typeof(object), stvName);
                            field.LinePragma = getPragma(statement.Position.FirstLine - 1);
                            CodeMemberField fld = (CodeMemberField)
                                ((MethodContext)method).Parent.AddObject(field, statement);
                            fld.Attributes = MemberAttributes.Private | MemberAttributes.Static;
                            ((MethodContext)method).StaticVariables.Add(var.Variable.VarName.Value, stvName);
                            if (var.Initializer != null)
                                fld.InitExpression = TranslateExpression(var.Initializer, method, block is IStatementInsertContext ? (IStatementInsertContext)block : null);
                        }
                        else
                            throw new PhpToCodeDomNotSupportedException(Localizations.Strings.cdp_unsup_2_stat_vars_with_same_name, statement);
                    }
                }
                else
                    throw new PhpToCodeDomNotSupportedException(Localizations.Strings.cdp_unsup_static_var_otside_method, statement);
            }
            /// <summary>Translates throw statement from PHP to CodeDOM</summary><param name="statement">Statement to translate</param><param name="method">GetUserEntryPoint for declaring local variables</param><param name="block">Block for adding statements</param>
            /// <exception cref="PhpToCodeDomNotSupportedException">Expression that produces exception being thrown is not supported (see <see cref="TranslateExpression"/>)</exception>
            protected void TranslateStatement(ThrowStmt statement, MethodContextBase method, IBlockContext block)
            {
                CodeThrowExceptionStatement Throw = new CodeThrowExceptionStatement(TranslateExpression(statement.Expression, method, getIC(block)));
                Throw.LinePragma = getPragma(statement.Position.FirstLine - 1);
                block.AddObject(Throw, statement);
            }
            /// <summary>Translates unset statement from PHP to CodeDOM</summary><param name="statement">Statement to translate</param><param name="method">GetUserEntryPoint for declaring local variables</param><param name="block">Block for adding statements</param>
            /// <remarks>
            /// All unset statements are translated as assignment of null, which is actual behavior of PHP only for local variables.
            /// In order to distinguish if translation is correct or not localo variables are set directly to null and in other cases <see cref="Helper.unset"/> is used
            /// </remarks>
            /// <exception cref="PhpToCodeDomNotSupportedException">Unseted variable consists of unsupported construct (<see cref="TranslateVariableUse"/>)</exception>
            protected void TranslateStatement(UnsetStmt /*!*/statement, MethodContextBase /*!*/method, IBlockContext /*!*/ block)
            {
                foreach (VariableUse var in statement.VarList)
                {
                    CodeStatement stm = new CodeAssignStatement(TranslateVariableUse(var, method, getIC(block)),
                        (var is DirectVarUse && var.IsMemberOf == null) ?
                        (CodeExpression)new CodePrimitiveExpression(null) :
                        (CodeExpression)new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(typeof(Helper)), "unset"));
                    stm.LinePragma = getPragma(statement.Position.FirstLine - 1);
                    /*if(var is IndirectVarUse) {
                    } else if(var is DirectVarUse) {
                        if(var.IsMemberOf == null) 
                            stm = new CodeAssignStatement(TranslateVariableUse(var,method), new CodePrimitiveExpression(null));
                        else
                            stm = new CodeExpressionStatement(
                                new CodeMethodInvokeExpression(
                                    new CodeTypeReferenceExpression(typeof(Convert)),
                                    "UnsetProperty",
                                    new CodeExpression[]{
                                        TranslateVarLikeConstructUse(var.IsMemberOf,method),
                                        ((DirectVarUse)var).VarName,
                                        
                    } else if(var is ItemUse) {
                        stm = new CodeExpressionStatement(
                            new CodeMethodInvokeExpression(
                                new CodeTypeReferenceExpression(typeof(Operators)),"UnsetItem",
                                new CodeExpression[]{
                                    TranslateVariableUse(((ItemUse)var).Array, method),
                                    TranslateExpression(((ItemUse)var).Index, method)
                                }));
                    } else if(var is DirectStFldUse){
                    } else if(var is IndirectStFldUse){
                    }else throw new PhpToCodeDomNotSupportedException(string.Format("Unsupported variable use {0} reached.",var.GetType().FullName));
                    */
                    block.AddObject(stm, statement);
                }
            }

            #endregion
            #endregion

            /// <summary>Gets CLR name of <see cref="QualifiedName"/> without trailing dot (.)</summary>
            /// <param name="name"><see cref="QualifiedName"/> to get name for.</param>
            /// <returns>Uses <see cref="QualifiedName.ToClrNotation"/> and removes traling dot (.) if any</returns>
            private String getCLRName(QualifiedName name)
            {
                String CLR = name.ToClrNotation(0, 0);
                if (CLR.EndsWith(".")) return CLR.Substring(0, CLR.Length - 1);
                return CLR;
            }
            #region Contexts
            #region Bases
            /// <summary>Represent context for local variables</summary>
            /// <remarks>
            /// This class implements context where no local variables are present (such as class or namespace).
            /// Derived class for methods (and CTors, accessors etc.) is <see cref="MethodContext"/>
            /// </remarks>
            protected class MethodContextBase : IEnumerable<string>
            {
                /// <summary>If overriden in derived class returns <see cref="T:System.Collections.Generic.List`1[System.String]"/> that enumerates through list of names of local variables.</summary>
                /// <returns>Instance of <see cref="T:System.Collections.Generic.IEnumerator`1[System.String]"/> that enumerates through list of names of local variables (if current context supports local variables; otherwise it enumerates through empty list of items)</returns>
                virtual public IEnumerator<string> GetEnumerator()
                {
                    return new List<string>().GetEnumerator();
                }
                /// <summary>Gets value indicating if given name is contained in list of local variables</summary>
                /// <param name="Name">Name to search for</param>
                /// <returns>True if name if present otherwise false</returns>
                virtual public bool Contains(string Name)
                {
                    foreach (string name in this)
                        if (name == Name) return true;
                    return false;
                }
                /// <summary>If implemented in derived class gets <see cref="CodeVariableDeclarationStatement"/> for variable with given name</summary>
                /// <param name="index">Name of variable</param>
                /// <remarks>If derived class returns true from <see cref="CanAdd"/>, it must support this property</remarks>
                /// <exception cref="NotImplementedException">This property is not implemented in derived class. This implementation throws it always.</exception>
                /// <example cref="KeyNotFoundException">Variable with name <paramref name="index"/> is not declared</example>
                virtual public CodeVariableDeclarationStatement this[string index]
                {
                    get { throw new NotImplementedException(Localizations.Strings.cdp_unsup_loc_var_context); }
                }
                /// <summary>Implements <see cref="System.Collections.IEnumerable.GetEnumerator"/></summary>
                /// <returns><see cref="GetEnumerator"/></returns>
                /// <remarks>Use type-safe <see cref="GetEnumerator"/> instead.</remarks>
                [Obsolete("Use type-safe GetEnumerator instead")]
                System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
                {
                    return GetEnumerator();
                }
                /// <summary>If implemented in derived class adds name of local variable into list of local variables' names and declares that variable</summary>
                /// <param name="name">Name of variable to add. Note: This method can change the name</param>
                /// <returns>Variable declaration (or null if no variable was added due to duplicity)</returns>
                /// <exception cref="NotSupportedException">This implementation throws it always. Any implementation can throw it if any or specific variable cannot be added.</exception>
                virtual public CodeVariableDeclarationStatement Add(ref string name)
                {
                    throw new NotSupportedException(Localizations.Strings.cdp_unsup_loc_var_class_namespace);
                }
                /// <summary>Returns value indicating if local variables can be added into this context</summary>
                /// <returns>This implementation always returns false</returns>
                /// <remarks>Instance returning true must support <see craf="Add"/>, <see cref="GetEnumerator"/> and <see cref="this"/></remarks>
                virtual public bool CanAdd { get { return false; } }
                /// <summary>If implemented in derived class returns class the method is in or null if method is not in class</summary>
                /// <returns>This implementation always returns null</returns>
                virtual public TypeContext Class { get { return null; } }
            }
            /// <summary>Provides base class for all containers the code can be placed inside such as file, namespace, class, method, try clausule etc.</summary>
            protected interface IBlockContext
            {
                /// <summary>Adds <see cref="CodeObject"/> into current context</summary>
                /// <param name="Object"><see cref="CodeObject"/> to be added</param>
                /// <returns><paramref name="Object"/></returns>
                /// <param name="throwOn">Element to throw <see cref="PhpToCodeDomNotSupportedException"/> on</param>
                /// <exception cref="PhpToCodeDomNotSupportedException">Object of type <b>T</b> is not supported in this block</exception>
                CodeObject AddObject(CodeObject Object, LangElement throwOn);
            }
            /// <summary>Interface represents block of code where statements can be added and inserted</summary>
            protected interface ICodeBlockContext : IBlockContext, IStatementInsertContext
            {
                /// <summary>Moves place where <see cref="IStatementInsertContext"/> works after specified statement</summary>
                /// <param name="Statement">Statement after which <see cref="IStatementInsertContext"/> will insert statements</param>
                /// <exception cref="ArgumentException"><paramref name="Statement"/> is not one of statements in block</exception>
                void SetInsertContextAfter(CodeStatement Statement);
                /// <summary>Moves place where <see cref="IStatementInsertContext"/> works before specified statement</summary>
                /// <param name="Statement">Statement before which <see cref="IStatementInsertContext"/> will insert statements</param>
                /// <exception cref="ArgumentException"><paramref name="Statement"/> is not one of statements in block</exception>
                void SetInsertContextBefore(CodeStatement Statement);
                /// <summary>Moves place where <see cref="IStatementInsertContext"/> works at the beginning of block</summary>
                void ResetInserContextToStart();
                /// <summary>Moves place where <see cref="IStatementInsertContext"/> works at the end of block</summary>
                void ResetInsertContextToEnd();
            }
            /// <summary>Interface that allows inserting statements into itseltf</summary>
            /// <remarks>Used when some PHP inline expression cannot be represented inline in CodeDOM</remarks>
            protected interface IStatementInsertContext
            {
                /// <summary>Inserts statement into context</summary>
                /// <param name="Statement">Statement co insetr</param>
                /// <param name="throwOn">Element to throw <see cref="PhpToCodeDomNotSupportedException"/> on if insertion is not supported</param>
                /// <returns><paramref name="Statement"/></returns>
                /// <exception cref="PhpToCodeDomNotSupportedException">Insertion is not supported</exception>
                CodeStatement Insert(CodeStatement /*!*/ Statement, LangElement /*!*/ throwOn);
            }
            /// <summary>Represents request for creating break (jump) statement target</summary>
            /// <remarks>Thare must be specific support for this in acutal implementation of <see cref="IBlockContext"/>.</remarks>
            protected class CodeBreakTargetRequest : CodeObject
            {
                /// <summary>Containd value of the <see cref="Levels"/> property</summary>
                private int levels;
                /// <summary>Contains value of the <see cref="Type"/> property</summary>
                private JumpStmt.Types type;
                /// <summary>Type of this jump. Can be <see cref="JumpStmt.Types.Continue"/> or <see cref="JumpStmt.Types.Break"/></summary>
                public JumpStmt.Types Type { get { return type; } }
                /// <summary>CTor</summary>
                /// <param name="levels">Levels to break from current level (1 is default)</param>
                /// <exception cref="ArgumentOutOfRangeException"><paramref name="levels"/> is less than 1</exception>
                /// <param name="type">Type of this jump (continue or break)</param>
                /// <exception cref="PhpToCodeDomNotSupportedException"><paramref name="type"/> is neither <see cref="JumpStmt.Types.Break"/> nor <see cref="JumpStmt.Types.Continue"/></exception>
                /// <param name="throwOn">Element to throw <see cref="PhpToCodeDomNotSupportedException"/> on</param>
                public CodeBreakTargetRequest(int levels, JumpStmt.Types type, LangElement /*!*/ throwOn)
                {
                    if (levels < 1) throw new ArgumentOutOfRangeException("levels", Localizations.Strings.cdp_unsup_levels_0);
                    this.levels = levels;
                    if (type != JumpStmt.Types.Break && type != JumpStmt.Types.Continue) throw new PhpToCodeDomNotSupportedException(Localizations.Strings.cdp_unsup_break_continue_only, throwOn);
                    this.type = type;
                }
                /// <summary>Levels to break</summary>
                /// <remarks><see cref="IBlockContext"/> implementation is allowed to decrease this property when it implements one of levels being "broken" (for, foreach, switch, do, while).</remarks>
                public int Levels { get { return levels; } set { levels = value; } }
                /// <summary>Contains value of the <see cref="Target"/> property</summary>
                private string target;
                /// <summary>This property is used by implementation of <see cref="IBlockContext"/> to return name of lable that will became target of jump</summary>
                public string Target { get { return target; } set { target = value; } }
            }
            /// <summary>Common base for all block statements (statements that can contain another statements)</summary>
            protected abstract class BlockStatementContext : ICodeBlockContext
            {
                /// <summary>Statements in this block</summary>
                protected abstract CodeStatementCollection Statements { get; }
                /// <summary>Identifies this block as loop block</summary>
                /// <remarks>This is for translation of break statements. Loops are for, do, while, foreach and switch</remarks>
                protected abstract bool IsLoop { get; }
                /// <summary>Statement this block is context for</summary>
                protected abstract CodeStatement Context { get; }
                /// <summary>Contains value of the <see cref="Method"/> property</summary>
                private /*!*/ MethodContext method;
                /// <summary>GetUserEntryPoint this block lies in</summary>
                public /*!*/ MethodContext Method { get { return method; } }
                /// <summary>Contains value of the <see cref="Parent"/> property</summary>
                private BlockStatementContext parent;
                /// <summary>Immediate parent of this block if it is another block statement</summary>
                public BlockStatementContext Parent { get { return parent; } }
                /// <summary>Immediate parent of this block. It can be another block or method.</summary>
                public ICodeBlockContext AnyParent { get { return Parent == null ? (ICodeBlockContext)Method : (ICodeBlockContext)Parent; } }
                /// <summary>Contains value of the <see cref="Owner"/> property</summary>
                private readonly PhpCodeDomParserImplementation owner;
                /// <summary>Owner of¨this instance</summary>
                protected PhpCodeDomParserImplementation Owner { get { return owner; } }
                /// <summary>CTor</summary>
                /// <param name="method">GetUserEntryPoint the block lies in</param>
                /// <param name="block">Immediate parent of this block if it is not <paramref name="method"/></param>
                /// <param name="owner">Owner of this instance</param>
                public BlockStatementContext(MethodContext /*!*/ method, BlockStatementContext block, PhpCodeDomParserImplementation owner)
                {
                    this.method = method;
                    this.parent = block;
                    this.owner = owner;
                }
                //private string currentFile;

                #region Inserting statements
                /// <summary>Index where inserted statemens are added. -1 means end of statement list.</summary>
                private int InsertionPoint = -1;
                /// <summary>Inserts statement into method</summary>
                /// <param name="Statement">Statement co insetr</param>
                /// <param name="throwOn">Ignored</param>
                /// <returns><paramref name="Statement"/></returns>
                public virtual CodeStatement Insert(CodeStatement Statement, LangElement throwOn)
                {
                    if (InsertionPoint < 0)
                        Statements.Add(Statement);
                    else
                        Statements.Insert(InsertionPoint++, Statement);
                    return Statement;
                }
                /// <summary>Moves place where <see cref="IStatementInsertContext"/> works after specified statement</summary>
                /// <param name="Statement">Statement after which <see cref="IStatementInsertContext"/> will insert statements</param>
                /// <exception cref="ArgumentException"><paramref name="Statement"/> is not one of statements in block</exception>
                public virtual void SetInsertContextAfter(CodeStatement Statement)
                {
                    int index = Statements.IndexOf(Statement);
                    if (index >= 0) InsertionPoint = index + 1;
                    else throw new ArgumentException(Localizations.Strings.cdp_statement_not_found, "Statement");
                }
                /// <summary>Moves place where <see cref="IStatementInsertContext"/> works before specified statement</summary>
                /// <param name="Statement">Statement before which <see cref="IStatementInsertContext"/> will insert statements</param>
                /// <exception cref="ArgumentException"><paramref name="Statement"/> is not one of statements in block</exception>
                public virtual void SetInsertContextBefore(CodeStatement Statement)
                {
                    int index = Statements.IndexOf(Statement);
                    if (index >= 0) InsertionPoint = index;
                    else throw new ArgumentException(Localizations.Strings.cdp_statement_not_found, "Statement");
                }
                /// <summary>Moves place where <see cref="IStatementInsertContext"/> works at the beginning of block</summary>
                public virtual void ResetInserContextToStart()
                {
                    InsertionPoint = 0;
                }
                /// <summary>Moves place where <see cref="IStatementInsertContext"/> works at the end of block</summary>
                /// <remarks>This is default state</remarks>
                public virtual void ResetInsertContextToEnd()
                {
                    InsertionPoint = -1;
                }
                #endregion

                /// <summary>Adds object into current context</summary>
                /// <param name="Object">Object to add</param>
                /// <returns><paramref name="Object"/></returns>
                /// <exception cref="PhpToCodeDomNotSupportedException">
                /// <paramref name="Object"/> is neither <see cref="CodeExpression"/> nor <see cref="CodeStatement"/> nor <see cref="CodeBreakTargetRequest"/>
                /// <param name="throwOn">Element to thrown <see cref="PhpToCodeDomNotSupportedException"/> on</param>
                /// </exception>
                public virtual CodeObject AddObject(CodeObject Object, LangElement throwOn)
                {
                    if (Object is CodeExpression)
                    {
                        var exs = new CodeExpressionStatement((CodeExpression)Object);
                        exs.LinePragma = Owner.getPragma(throwOn.Position.FirstLine - 1);
                        Statements.Add(exs);
                    }
                    else if (Object is CodeStatement)
                        Statements.Add((CodeStatement)Object);
                    else if (Object is CodeBreakTargetRequest)
                    {
                        if (IsLoop)
                            if (((CodeBreakTargetRequest)Object).Levels == 1)
                                GetJumpLabel((CodeBreakTargetRequest)Object, throwOn);
                            else
                            {
                                --((CodeBreakTargetRequest)Object).Levels;
                                AnyParent.AddObject(Object, throwOn);
                            }
                        else
                            AnyParent.AddObject(Object, throwOn);
                    }
                    else
                        throw new PhpToCodeDomNotSupportedException(string.Format(Localizations.Strings.cdp_unsup_not_allowed_at_block_level, Object.GetType().Name), throwOn);
                    return Object;
                }
                /// <summary>If overriden in derived class writes name of labe to jump to into given parameter</summary>
                /// <param name="Jump">Represents type of jump (break or continue) and is target of assignment of name of lable to jump to.</param>
                /// <param name="throwOn">Element to thrown <see cref="PhpToCodeDomNotSupportedException"/> on</param>
                /// <exception cref="PhpToCodeDomNotSupportedException">This implementation throws it always</exception>
                public virtual void GetJumpLabel(CodeBreakTargetRequest Jump, LangElement /*!*/ throwOn)
                {
                    throw new PhpToCodeDomNotSupportedException(Localizations.Strings.cdp_unsup_only_loop_jump, throwOn);
                }
            }
            #endregion

            /// <summary>Represents context for local variable inside any method</summary>
            protected sealed class MethodContext : MethodContextBase, ICodeBlockContext
            {
                /// <summary>GetUserEntryPoint represented by this context</summary>
                public CodeMemberMethod Context { get { return context; } }
                /// <summary>Declaration statements</summary>
                private Dictionary<string, CodeVariableDeclarationStatement> declarations = new Dictionary<string, CodeVariableDeclarationStatement>();
                /// <summary>CTor</summary>
                /// <param name="Context">GetUserEntryPoint that will becomme context for this instance</param>
                /// <param name="parent">Parent of this method (ie. class)</param>
                /// <remarks>Do not add formal parameters to method after passed here!</remarks>
                public MethodContext(CodeMemberMethod /*!*/ Context, IBlockContext /*!*/ parent)
                {
                    this.context = Context;
                    foreach (CodeParameterDeclarationExpression Param in Context.Parameters)
                        list.Add(Param.Name);
                    this.parent = parent;
                }
                /// <summary>Contains value of the <see cref="Parent"/> property</summary>
                private IBlockContext/*!*/ parent;
                /// <summary>Parent of this method (ie. class)</summary>
                public IBlockContext /*!*/ Parent { get { return parent; } }
                /// <summary>Returns value indicating if local variables can be added into this context</summary>
                /// <returns>This implementation always returns true</returns>    
                public override bool CanAdd { get { return true; } }
                /// <summary>Context of this block</summary>
                private CodeMemberMethod context;
                /// <summary>List of names of local variables (including method formal parameters)</summary>
                private List<string> list = new List<string>();
                /// <summary>Index of last local variable declaration statement added to method</summary>
                private int LastIndex = -1;
                /// <summary>Adds name of local variable into list of local variables' names and declaret that variable.</summary>
                /// <param name="name">Name of variable to add. Note: This method can chnge the name.</param>
                /// <returns>Variable declaration (or null if no variable was added due to duplicity)</returns>
                public override CodeVariableDeclarationStatement Add(ref string /*!*/ name)
                {
                    if (rename_hack.ContainsKey(name))
                    {
                        name = rename_hack[name];
                        return null;
                    }
                    else if (!list.Contains(name))
                    {
                        if (this.Context.Name == "InitializeComponent" && name == "resources")
                        {
                            //From some reason designer complains that "The name 'resources' is already used by another object." when the "resources" name is reported. So, lets virually rename it. Designer renames it back to resources, but this does not matter as the name is correct for compilation.
                            name = "resources_hacked_variable_name_as_long_as_nobody_will_hopefully_use_it_as_name_of_local_variable_in_designergenrated_source_code_of_the_InitializeComponent_method";
                            rename_hack.Add("resources", name);
                        }
                        CodeVariableDeclarationStatement ret;
                        list.Add(name);
                        Context.Statements.Insert(++LastIndex, ret = new CodeVariableDeclarationStatement(typeof(object), name));
                        declarations.Add(name, ret);
                        return ret;
                    }
                    return null;
                }
                /// <summary>local variable renaming hacks</summary>
                private Dictionary<string, string> rename_hack = new Dictionary<string, string>();
                /// <summary>Gets declaration statement for given variable</summary>
                /// <param name="index">name of variable to get declaration staatement for</param>
                /// <exception cref="KeyNotFoundException">Variable with name <paramref name="index"/> is not declared</exception>
                public override CodeVariableDeclarationStatement this[string index]
                {
                    get
                    {
                        return declarations[index];
                    }
                }
                /// <summary>Gets value indicating if given name is contained in list of local variables</summary>
                /// <param name="Name">Name to search for</param>
                /// <returns>True if name if present otherwise false</returns>
                public override bool Contains(string Name)
                {
                    return list.Contains(Name);
                }
                /// <summary>Returns <see cref="T:System.Collections.Generic.IEnumerator`1[System.String]"/> that enumerates through list of names of local variables.</summary>
                /// <returns>Instance of <see cref="T:System.Collections.Generic.IEnumerator`1[System.String]"/> that enumerates through list of names of local variables</returns>
                public override IEnumerator<string> GetEnumerator()
                {
                    return list.GetEnumerator();
                }
                /// <summary>Adds <see cref="CodeObject"/> into this method context</summary>
                /// <param name="Object">Object to be added</param>
                /// <param name="throwOn">Element to throw <see cref="PhpToCodeDomNotSupportedException"/> on</param>
                /// <returns><paramref name="Object"/><paramref name="Object"/> is neither <see cref="CodeExpression"/> nor <see cref="CodeStatement"/></returns> 
                public CodeObject AddObject(CodeObject Object, LangElement throwOn)
                {
                    if (Object is CodeExpression)
                        Context.Statements.Add((CodeExpression)Object);
                    else if (Object is CodeStatement)
                        Context.Statements.Add((CodeStatement)Object);
                    else
                        throw new PhpToCodeDomNotSupportedException(string.Format(Localizations.Strings.cdp_unsup_not_allowed_at_method_level, Object.GetType().Name), throwOn);
                    return Object;
                }
                /// <summary>Index where inserted statemens are added. -1 means end of statement list.</summary>
                private int InsertionPoint = -1;
                /// <summary>Inserts statement into method</summary>
                /// <param name="Statement">Statement co insetr</param>
                /// <param name="throwOn">Ignored</param>
                /// <returns><paramref name="Statement"/></returns>
                public CodeStatement Insert(CodeStatement Statement, LangElement throwOn)
                {
                    if (InsertionPoint < 0)
                        Context.Statements.Add(Statement);
                    else
                        Context.Statements.Insert(InsertionPoint++, Statement);
                    return Statement;
                }
                /// <summary>Moves place where <see cref="IStatementInsertContext"/> works after specified statement</summary>
                /// <param name="Statement">Statement after which <see cref="IStatementInsertContext"/> will insert statements</param>
                /// <exception cref="ArgumentException"><paramref name="Statement"/> is not one of statements in block</exception>
                public void SetInsertContextAfter(CodeStatement Statement)
                {
                    int index = Context.Statements.IndexOf(Statement);
                    if (index >= 0) InsertionPoint = index + 1;
                    else throw new ArgumentException(Localizations.Strings.cdp_statement_not_found, "Statement");
                }
                /// <summary>Moves place where <see cref="IStatementInsertContext"/> works before specified statement</summary>
                /// <param name="Statement">Statement before which <see cref="IStatementInsertContext"/> will insert statements</param>
                /// <exception cref="ArgumentException"><paramref name="Statement"/> is not one of statements in block</exception>
                public void SetInsertContextBefore(CodeStatement Statement)
                {
                    int index = Context.Statements.IndexOf(Statement);
                    if (index >= 0) InsertionPoint = index;
                    else throw new ArgumentException(Localizations.Strings.cdp_statement_not_found, "Statement");
                }
                /// <summary>Moves place where <see cref="IStatementInsertContext"/> works at the beginning of block</summary>
                public void ResetInserContextToStart()
                {
                    InsertionPoint = 0;
                }
                /// <summary>Moves place where <see cref="IStatementInsertContext"/> works at the end of block</summary>
                /// <remarks>This is default state</remarks>
                public void ResetInsertContextToEnd()
                {
                    InsertionPoint = -1;
                }
                /// <summary>Contains value of the <see cref="StaticVariables"/> property</summary>
                private readonly Dictionary<string, string> staticVariables = new Dictionary<string, string>();
                /// <summary>Dictionary of static variables. Contains PHP names of static variables as keys and CodeDOM field names as values.</summary>
                public Dictionary<string, string> StaticVariables { get { return staticVariables; } }
                /// <summary>If block containing this method is <see cref="TypeContext"/> returns it</summary>
                /// <returns>Parent block of this method if it is <see cref="TypeContext"/>; otherwise null</returns>
                public override TypeContext Class
                {
                    get
                    {
                        if (parent is TypeContext) return (TypeContext)parent;
                        return null;
                    }
                }
            }

            #region Block statements
            /// <summary>Represents block of statements in try clausule</summary>
            protected sealed class TryStatementContext : BlockStatementContext
            {
                /// <summary>CTor</summary>
                /// <param name="method">GetUserEntryPoint in which statement lies</param>
                /// <param name="parent">Immediate parent of this statement (null if it lies directly in method)</param>
                /// <param name="context">Try-Catch-Finally statement which is context for this class</param>
                /// <param name="owner">owner of this instance</param>
                public TryStatementContext(MethodContext /*!*/ method, BlockStatementContext parent, CodeTryCatchFinallyStatement /*!*/ context, PhpCodeDomParserImplementation owner)
                    : base(method, parent, owner) { this.context = context; }
                /// <summary>Current context</summary>
                private /*!*/ CodeTryCatchFinallyStatement context;
                /// <summary>Statements in try clausule</summary>
                protected /*!*/ override CodeStatementCollection Statements { get { return context.TryStatements; } }
                /// <summary>Current context</summary>
                protected /*!*/ override CodeStatement Context { get { return context; } }
                /// <summary>Try is not loop</summary>
                protected override bool IsLoop { get { return false; } }
            }
            /// <summary>Represents block of statements in catch clausule</summary>
            protected sealed class CatchStatementContext : BlockStatementContext
            {
                /// <summary>CTor</summary>
                /// <param name="method">GetUserEntryPoint in which statement lies</param>
                /// <param name="parent">Immediate parent of this statement (null if it lies directly in method)</param>
                /// <param name="context">Try-Catch-Finally statement which is context for this class</param>
                /// <param name="index">Index of catch clausule in statement</param>
                /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is greater than or equal to count of catch clausules in <paramref name="context"/></exception>
                /// <param name="owner">Owner of this instance</param>
                public CatchStatementContext(MethodContext /*!*/ method, BlockStatementContext parent, CodeTryCatchFinallyStatement /*!*/ context, int index, PhpCodeDomParserImplementation owner)
                    : base(method, parent, owner) { this.context = context; if (index >= context.CatchClauses.Count) throw new ArgumentException(Localizations.Strings.cdp_index_not_within_catch_calusules, "index"); else this.index = index; }
                /// <summary>Current context</summary>
                private /*!*/ CodeTryCatchFinallyStatement context;
                /// <summary>Index of catch clausule in context</summary>
                private int index;
                /// <summary>Statements in catch clausule</summary>
                protected /*!*/ override CodeStatementCollection Statements { get { return Clausule.Statements; } }
                /// <summary>Current context</summary>
                protected /*!*/ override CodeStatement Context { get { return context; } }
                /// <summary>Catch clausule that is context for this block</summary>
                public CodeCatchClause Clausule { get { return context.CatchClauses[index]; } }
                /// <summary>Catch is not loop</summary>
                protected override bool IsLoop { get { return false; } }
            }
            /// <summary>Represents block of statements in finally clausule</summary>
            protected sealed class FinallyStatementContext : BlockStatementContext
            {
                /// <summary>CTor</summary>
                /// <param name="method">GetUserEntryPoint in which statement lies</param>
                /// <param name="parent">Immediate parent of this statement (null if it lies directly in method)</param>
                /// <param name="context">Try-Catch-Finally statement which is context for this class</param>
                /// <param name="owner">Owner of this instance</param>
                public FinallyStatementContext(MethodContext /*!*/ method, BlockStatementContext parent, CodeTryCatchFinallyStatement /*!*/ context, PhpCodeDomParserImplementation owner)
                    : base(method, parent, owner) { this.context = context; }
                /// <summary>Current context</summary>
                private /*!*/ CodeTryCatchFinallyStatement context;
                /// <summary>Statements in finally clausule</summary>
                protected /*!*/ override CodeStatementCollection Statements { get { return context.TryStatements; } }
                /// <summary>Current context</summary>
                protected /*!*/ override CodeStatement Context { get { return context; } }
                /// <summary>Finally is not loop</summary>
                protected override bool IsLoop { get { return false; } }
            }
            /// <summary>Represents context for 'if' and 'else' clausules of if statement</summary>
            protected sealed class IfStatementContext : BlockStatementContext
            {
                /// <summary>Context of this block</summary>
                private CodeConditionStatement /*!*/context;
                /// <summary>True if this block represents 'else' clausule; otherwise false</summary>
                private bool @else;
                /// <summary>CTor</summary>
                /// <param name="method">GetUserEntryPoint this statement is contained in</param>
                /// <param name="block">Immediate parent of this statement. Null if it is <paramref name="method"/>.</param>
                /// <param name="context">Context this statement represents</param>
                /// <param name="else">True if this instance represents an 'else' clausule. Otherwise false.</param>
                /// <param name="owner">Owner of this intance</param>
                public IfStatementContext(MethodContext /*!*/ method, BlockStatementContext block, CodeConditionStatement /*!*/ context, bool @else, PhpCodeDomParserImplementation owner)
                    : base(method, block, owner)
                {
                    this.context = context;
                    this.@else = @else;
                }
                /// <summary>Statements in this block</summary>
                protected override CodeStatementCollection/*!*/ Statements
                {
                    get { return @else ? context.FalseStatements : context.TrueStatements; }
                }
                /// <summary>If is not a loop</summary>
                protected override bool IsLoop { get { return false; } }
                /// <summary>Context for this block</summary>
                protected override CodeStatement /*!*/ Context { get { return context; } }
            }
            /// <summary>Represents context for case or default clausule of switch statement</summary>
            protected sealed class CaseContext : BlockStatementContext
            {
                /// <summary>Name of label after whole switch statement</summary>
                private string /*!*/endlabel;
                /// <summary>CTor</summary>
                /// <param name="method">GetUserEntryPoint this block is contained in</param>
                /// <param name="block">Block that is immediate parent of this switch statement. Null if it is <paramref name="method"/>.</param>
                /// <param name="context">Context for this case clausule</param>
                /// <param name="endlabel">Name of label after whole switch statement</param>
                /// <param name="owner">Owner of this instance</param>
                public CaseContext(MethodContext/*!*/ method, BlockStatementContext block, CodeConditionStatement /*!*/ context, string /*!*/ endlabel, PhpCodeDomParserImplementation owner)
                    : base(method, block, owner)
                {
                    this.endlabel = endlabel;
                    this.context = context;
                }
                /// <summary>If statement that is context for this case clausule</summary>
                private /*!*/ CodeConditionStatement context;
                /// <summary>Switch is a loop</summary>
                protected override bool IsLoop { get { return true; } }
                /// <summary>Context for this block</summary>
                protected override CodeStatement /*!*/Context { get { return context; } }
                /// <summary>Statemenst for this case clausule</summary>
                protected override CodeStatementCollection /*!*/ Statements { get { return context.TrueStatements; } }
                /// <summary>Writes name of label after the switch statement into given variable</summary>
                /// <param name="throwOn">Ignored</param>
                /// <param name="Jump">Request for label</param>
                public override void GetJumpLabel(CodeBreakTargetRequest /*!*/ Jump, LangElement throwOn) { Jump.Target = endlabel; }
            }
            /// <summary>Represents context for for statement</summary>
            protected sealed class ForStatementContext : BlockStatementContext
            {
                /// <summary>Ctor</summary>
                /// <param name="method">GetUserEntryPoint the statement is in</param>
                /// <param name="block">Block that is immediate parent of the statement (null if immediate parent is <paramref name="method"/>)</param>
                /// <param name="context">Context of this statement</param>
                /// <param name="endlabel">Label after whole for statement</param>
                /// <param name="firstlabel">Label just before end of for statement</param>
                /// <param name="owner">Owner of this instance</param>
                public ForStatementContext(MethodContext/*!*/ method, BlockStatementContext block, CodeIterationStatement /*!*/ context, string /*!*/ endlabel, string /*!*/ firstlabel, PhpCodeDomParserImplementation owner)
                    : base(method, block, owner)
                {
                    this.context = context;
                    this.endlabel = endlabel;
                    this.firstlabel = firstlabel;
                }
                /// <summary>Label after whole for statement</summary>
                private string /*!*/ endlabel;
                /// <summary>Label just before end of for statement</summary>
                private string /*!*/ firstlabel;
                /// <summary>COdeDOM representation of for statement</summary>
                private CodeIterationStatement context;
                /// <summary>True - For is a loop</summary>
                protected override bool IsLoop { get { return true; } }
                /// <summary>Context of this statement</summary>
                protected override CodeStatement Context { get { return context; } }
                /// <summary>Collection of statements inside this statement</summary>
                protected override CodeStatementCollection Statements { get { return context.Statements; } }
                /// <summary>Writes label for break/or continue to given variable</summary>
                /// <param name="Jump">Variable to write name of label to</param>
                /// <param name="throwOn">Ignored</param>
                public override void GetJumpLabel(CodeBreakTargetRequest Jump, LangElement throwOn)
                {
                    Jump.Target = Jump.Type == JumpStmt.Types.Break ? endlabel : firstlabel;
                }
            }
            #endregion

            /// <summary>Represents block context for whole file</summary>
            protected sealed class FileContext : IBlockContext
            {
                /// <summary><see cref="CodeCompileUnit"/> that represents the file</summary>
                private CodeCompileUnit context;
                /// <summary>CTor</summary>
                /// <param name="context"><see cref="CodeCompileUnit"/> that represents the file</param>
                public FileContext(CodeCompileUnit context) { this.context = context; }
                /// <summary>Adds <see cref="CodeObject"/> into current file</summary>
                /// <param name="Object"><see cref="CodeObject"/> to be added</param>
                /// <returns><paramref name="Object"/></returns>
                /// <param name="throwOn">Element to throw <see cref="PhpToCodeDomNotSupportedException"/> on</param>
                /// <exception cref="PhpToCodeDomNotSupportedException">Object of type <b>T</b> is not supported in this block</exception>
                /// <remarks>File-level supports only <see cref="CodeCommentStatement"/>, <see cref="CodeNamespace"/> and <see cref="CodeTypeDeclaration"/> (including <see cref="CodeTypeDelegate"/>)</remarks>
                public CodeObject AddObject(CodeObject Object, LangElement throwOn)
                {
                    if (Object is CodeCommentStatement) context.Namespaces[0].Comments.Add((CodeCommentStatement)Object);
                    else if (Object is CodeNamespace) context.Namespaces.Add((CodeNamespace)Object);
                    else if (Object is CodeTypeDeclaration) context.Namespaces[0].Types.Add((CodeTypeDeclaration)Object);
                    else throw new PhpToCodeDomNotSupportedException(string.Format(Localizations.Strings.cdp_unsup_not_allowed_at_file_level, Object.GetType().FullName), throwOn);
                    return Object;
                }
            }
            /// <summary>Represents namespace</summary> 
            protected sealed class NamespaceContext : IBlockContext
            {
                /// <summary><see cref="CodeCompileUnit"/> that represents the file</summary>
                private CodeNamespace context;
                /// <summary>CTor</summary>
                /// <param name="context"><see cref="CodeNamespace"/> that represents the namespace</param>
                public NamespaceContext(CodeNamespace context) { this.context = context; }
                /// <summary>Adds <see cref="CodeObject"/> into current namespace</summary>
                /// <param name="Object"><see cref="CodeObject"/> to be added</param>
                /// <returns><paramref name="Object"/></returns>
                /// <param name="throwOn">Element to throw <see cref="PhpToCodeDomNotSupportedException"/> on</param>
                /// <exception cref="PhpToCodeDomNotSupportedException">Object represented by <paramref name="Object"/> is not supported in this block</exception>
                /// <remarks>Namespace-level supports only <see cref="CodeCommentStatement"/> and <see cref="CodeTypeDeclaration"/> (including <see cref="CodeTypeDelegate"/>)</remarks>
                public CodeObject AddObject(CodeObject Object, LangElement throwOn)
                {
                    if (Object is CodeCommentStatement) context.Comments.Add((CodeCommentStatement)Object);
                    else if (Object is CodeTypeDeclaration) context.Types.Add((CodeTypeDeclaration)Object);
                    else throw new PhpToCodeDomNotSupportedException(string.Format(Localizations.Strings.cdp_unsup_not_allowed_at_namespace_level, Object.GetType().FullName), throwOn);
                    return Object;
                }
            }
            /// <summary>Represents type (Class, Structure, Enumeration, Delegate, Interface)</summary>
            protected sealed class TypeContext : IBlockContext
            {
                /// <summary><see cref="CodeTypeDeclaration"/> that represents this type</summary>
                private CodeTypeDeclaration context;
                /// <summary><see cref="CodeTypeDeclaration"/> that represents this type</summary>
                public CodeTypeDeclaration Context { get { return context; } }
                /// <summary>CTor</summary>
                /// <param name="context"><see cref="CodeTypeDeclaration"/> that represents the type</param>
                public TypeContext(CodeTypeDeclaration context) { this.context = context; }
                /// <summary>Adds <see cref="CodeObject"/> into current type</summary>
                /// <param name="Object"><see cref="CodeObject"/> to be added. This must be <see cref="CodeTypeMember"/></param>
                /// <returns><paramref name="Object"/></returns>
                /// <exception cref="PhpToCodeDomNotSupportedException"><paramref name="Object"/> is not <see cref="CodeTypeMember"/> or object represented by <paramref name="Object"/> is not supported in current block debending on what kind of type is represented by current block</exception>
                /// <remarks>
                /// Following checks are performed:
                /// <list type="list">
                /// <item>Fields cannot be added into interfaces</item>
                /// <item>Only static constant fields can be added into enumerations</item>
                /// <item>Nothiong can be added into delegates</item>
                /// </list>
                /// </remarks>
                /// <param name="throwOn">Element to throw <see cref="PhpToCodeDomNotSupportedException"/> on</param>
                public CodeObject AddObject(CodeObject Object, LangElement throwOn)
                {
                    if (context is CodeTypeDelegate) throw new PhpToCodeDomNotSupportedException(Localizations.Strings.cdp_unsup_anything_delegate, throwOn);
                    if (Object is CodeTypeMember)
                    {
                        if (Object is CodeMemberField && context.IsInterface) throw new PhpToCodeDomNotSupportedException(Localizations.Strings.cdp_unsup_filed_delegate, throwOn);
                        if (!context.IsEnum)
                        {
                            context.Members.Add((CodeTypeMember)Object);
                        }
                        else if (Object is CodeMemberField)
                        {
                            if ((((CodeMemberField)(Object)).Attributes & MemberAttributes.Const) == MemberAttributes.Const && (((CodeMemberField)Object).Attributes & MemberAttributes.Static) == MemberAttributes.Static)
                                context.Members.Add((CodeMemberField)Object);
                            else
                                throw new PhpToCodeDomNotSupportedException(Localizations.Strings.cdp_unsup_in_enum, throwOn);
                        }
                        else
                        {
                            throw new PhpToCodeDomNotSupportedException(Localizations.Strings.cdp_unsup_in_enum_other, throwOn);
                        }
                        if ((Object is CodeMemberField || Object is CodeMemberProperty) && !FieldsAndProperties.ContainsKey(((CodeTypeMember)Object).Name))
                            FieldsAndProperties.Add(((CodeTypeMember)Object).Name, (CodeTypeMember)Object);
                    }
                    else
                    {
                        throw new PhpToCodeDomNotSupportedException(string.Format(Localizations.Strings.cdp_unsup_unsupported_at_type_level, Object.GetType().FullName), throwOn);
                    }
                    return Object;
                }
                /// <summary>Contains dictionary of properties and fields defined on this class (no inherited members) keyed by its names</summary>
                private Dictionary<string, CodeTypeMember> FieldsAndProperties = new Dictionary<string, CodeTypeMember>();
                /// <summary>Gets property or field defined on this class with given name</summary>
                /// <param name="Name">Name of member to get</param>
                /// <returns>Member with given name which is <see cref="CodeMemberField"/> or <see cref="CodeMemberProperty"/> or null if there is no such member</returns>
                public CodeTypeMember getPropertyOrField(string /*!*/ Name)
                {
                    if (FieldsAndProperties.ContainsKey(Name)) return FieldsAndProperties[Name];
                    return null;
                }
            }
        }
            #endregion
        /// <summary>Represents error meaning that something is not supported fro PHP-to-CodeDOM translation</summary>
        public class PhpToCodeDomNotSupportedException : NotSupportedException
        {
            /// <summary>Element that caused the error</summary>
            private LangElement /*!*/ element;
            /// <summary>CTor from message and element</summary>
            /// <param name="message">Exception message</param>
            /// <param name="element">Element that casused the exception</param>
            /// <exception cref="ArgumentNullException"><paramref name="element"/> is null</exception>
            public PhpToCodeDomNotSupportedException(string message, LangElement /*!*/ element) :
                this(message, null, element) { }
            /// <summary>CTor from message, inner exception and element</summary>
            /// <param name="message">Exception message</param>
            /// <param name="element">Element that casused the exception</param>
            /// <param name="innerException">Exception thatcaused this exception</param>
            /// <exception cref="ArgumentNullException"><paramref name="element"/> is null</exception>
            public PhpToCodeDomNotSupportedException(string message, Exception innerException, LangElement /*!*/ element)
                : base(message, innerException)
            {
                if (element == null) throw new ArgumentNullException("element");
                this.element = element;
            }
            /// <summary>Gets a message that describes the current exception.</summary>
            /// <returns>The error message that explains the reason for the exception, or an empty ("").</returns>
            public override string Message
            {
                get
                {
                    return base.Message + " @" + element.Position.FirstLine.ToString() + "," + element.Position.FirstColumn.ToString();
                }
            }
            /// <summary>Element that caused the exception</summary>
            public LangElement Element { get { return element; } }
            /// <summary>Line where element that caused the exception starts</summary>
            public int Line { get { return element.Position.FirstLine; } }
            /// <summary>Column where element that caused the exception starts</summary>
            public int Column { get { return element.Position.FirstColumn; } }
            /// <summary><see cref="Parsers.Position">Position</see> of <see cref="Element"/></summary>
            public Parsers.Position Position { get { return element.Position; } }
        }
    }
    /// <summary>This <see cref="CodeLinePragma "/> is reported by PHP->COdeDOM translator bud should be ignored by COdeDOM->PHP translator</summary>
    internal class CodeLinePragmaNoWrite : CodeLinePragma
    {
        /// <summary>Initializes a new instance of the System.CodeDom.CodeLinePragma class.</summary>
        /// <param name="fileName">The file name of the associated file.</param>
        /// <param name="Line">The line number to store a reference to.</param>
        public CodeLinePragmaNoWrite(string fileName, int Line) : base(fileName, Line) { }
    }
}