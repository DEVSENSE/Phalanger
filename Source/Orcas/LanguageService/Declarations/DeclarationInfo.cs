/*

 Copyright (c) 2008 Jakub Misek

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Package;
using Microsoft.VisualStudio.TextManager.Interop;

using System.Collections;
using System.Reflection;

using PHP.Core;
using PHP.Core.AST;
using PHP.Core.Reflection;

using PHP.VisualStudio.PhalangerLanguageService.Scopes;
using PHP.VisualStudio.PhalangerLanguageService.Analyzer;

namespace PHP.VisualStudio.PhalangerLanguageService.Declarations
{
    #region FunctionParamInfo
    /// <summary>
    /// Parameter info.
    /// </summary>
    public class FunctionParamInfo
    {
        /// <summary>
        /// Formal parameter info.
        /// </summary>
        private readonly FormalParam/*!*/_param;

        internal readonly bool IsParams = false;

        /// <summary>
        /// Parameter description.
        /// </summary>
        public readonly string Description;

        /// <summary>
        /// Parameter is optional.
        /// </summary>
        public bool IsOptional = false;

        /// <summary>
        /// Parameter info from phalangers AST.
        /// </summary>
        /// <param name="prm">Phalanger parameter info.</param>
        public FunctionParamInfo(FormalParam/*!*/param)
        {
            _param = param;

            Description = null;
            IsOptional = (_param.InitValue != null);
        }

        public FunctionParamInfo(string name, object type, string description)
            : this(name, type, description, new PHP.Core.Parsers.Position())
        {
        }

        public FunctionParamInfo(string name, object type, string description, PHP.Core.Parsers.Position position)
            : this(name, type, description, position, false)
        {
            
        }

        /// <summary>
        /// Custom parameter info.
        /// </summary>
        /// <param name="name">Parameter variable name.</param>
        /// <param name="type">Parameter type.</param>
        /// <param name="description">Parameter description.</param>
        /// <param name="position">Declaration position.</param>
        /// <param name="optional">Parameter is optional.</param>
        public FunctionParamInfo(string name, object type, string description, PHP.Core.Parsers.Position position, bool optional)
        {
            _param = new FormalParam(position, name, type, false, null, null);

            this.Description = description;
            this.IsOptional = optional;

            // params
            Type t;
            if (type != null && (t = type as Type) != null && t.IsArray)
            {
                if (t.GetCustomAttributes(typeof(ParamArrayAttribute), false) != null)
                {
                    //_param.TypeHint = t.;
                    //IsOptional = true;
                    IsParams = true;
                }
            }
        }

        /// <summary>
        /// Parameter type name.
        /// </summary>
        public string TypeName
        {
            get
            {
                return IsParams ? "..." : ResolveTypeName(_param.TypeHint);
            }
        }

        /// <summary>
        /// Parameter qualified type.
        /// </summary>
        public QualifiedName TypeQualifiedName
        {
            get
            {
                return ResolveTypeQualifiedName(_param.TypeHint);
            }
        }

        /// <summary>
        /// Parameter name.
        /// </summary>
        public string Name
        {
            get
            {
                return IsParams ? null : _param.Name.Value;
            }
        }

        /// <summary>
        /// Parameter declaration position.
        /// </summary>
        public PHP.Core.Parsers.Position Position
        {
            get
            {
                return _param.Position;
            }
        }

        /// <summary>
        /// Try to get type name.
        /// </summary>
        /// <param name="TypeHint">Type hint.</param>
        /// <returns>Type name or null.</returns>
        protected static string ResolveTypeName(object TypeHint)
        {
            if (TypeHint != null)
            {
                if (TypeHint is string)
                {
                    return ((string)TypeHint);
                }
                else if (TypeHint is KnownType)
                {
                    return ((KnownType)TypeHint).QualifiedName.Name.Value;
                }
                else if (TypeHint is GenericQualifiedName)
                {
                    return ((GenericQualifiedName)TypeHint).QualifiedName.Name.Value;
                }
                else if (TypeHint is Type)
                {
                    return ((Type)TypeHint).Name;
                }

            }

            return null;
        }

        /// <summary>
        /// Try to get parameter qualified type.
        /// </summary>
        /// <param name="TypeHint">Phalanger type hint, by Phalanger AST.</param>
        /// <returns>QualifiedName</returns>
        protected static QualifiedName ResolveTypeQualifiedName(object TypeHint)
        {
            if (TypeHint != null)
            {
                if (TypeHint is string)
                {
                    return Utils.MakeQualifiedName((string)TypeHint);
                }
                else if (TypeHint is KnownType)
                {
                    return ((KnownType)TypeHint).QualifiedName;
                }
                else if (TypeHint is GenericQualifiedName)
                {
                    return ((GenericQualifiedName)TypeHint).QualifiedName;
                }
                else if (TypeHint is Type)
                {
                    return Utils.MakeQualifiedName(((Type)TypeHint).FullName, ".");
                }

            }

            return new QualifiedName();
        }

        /// <summary>
        /// Parameter initialization expression. Can be null.
        /// </summary>
        public Expression InitValue
        {
            get
            {
                return _param.InitValue;
            }
        }
    }
    #endregion

    #region Declaration label comparer

    /// <summary>
    /// Check if the given declaration matches the criteria.
    /// </summary>
    public abstract class DeclarationMatches
    {
        /// <summary>
        /// Check if the given declaration matches the criteria.
        /// </summary>
        /// <param name="decl">Declaration to be checked.</param>
        /// <returns>True if declaration matches.</returns>
        public abstract bool Matches(DeclarationInfo decl);
    }

    /// <summary>
    /// Declaration label contains given text.
    /// Not case sensitive.
    /// </summary>
    class DeclarationLabelContains:DeclarationMatches
    {
        private readonly string contains;

        /// <summary>
        /// Init.
        /// </summary>
        /// <param name="contains">Text to be contained in declaration label.</param>
        public DeclarationLabelContains(string contains)
        {
            this.contains = (contains != null)?(contains.ToLower()):null;
        }

        /// <summary>
        /// Given declaration contains the string.
        /// </summary>
        /// <param name="decl"></param>
        /// <returns></returns>
        public override bool Matches(DeclarationInfo decl)
        {
            if (contains == null)
                return true;

            if (decl.DeclarationType == DeclarationInfo.DeclarationTypes.Variable && contains.StartsWith("$"))
            {
                return decl.Label.ToLower().StartsWith(contains.Substring(1));
            }

            return decl.Label.ToLower().Contains(contains);
        }
    }

    /// <summary>
    /// Declaration label starts with given text.
    /// Not case sensitive.
    /// </summary>
    class DeclarationLabelStarts : DeclarationMatches
    {
        private readonly string starts;

        /// <summary>
        /// Init.
        /// </summary>
        /// <param name="contains">Text to be contained in declaration label.</param>
        public DeclarationLabelStarts(string starts)
        {
            this.starts = (starts != null) ? (starts.ToLower()) : null;
        }

        /// <summary>
        /// The given declaration starts with the string.
        /// </summary>
        /// <param name="decl"></param>
        /// <returns></returns>
        public override bool Matches(DeclarationInfo decl)
        {
            if (starts == null)
                return true;

            if (decl.DeclarationType == DeclarationInfo.DeclarationTypes.Variable && starts.StartsWith("$"))
            {
                return decl.Label.ToLower().StartsWith(starts.Substring(1));
            }

            return decl.Label.ToLower().StartsWith(starts);
        }
    }

    /// <summary>
    /// Declaration label is equal to given text.
    /// Not case sensitive.
    /// </summary>
    class DeclarationLabelEqual : DeclarationMatches
    {
        private readonly string label;

        /// <summary>
        /// Init.
        /// </summary>
        /// <param name="contains">Text to be contained in declaration label.</param>
        public DeclarationLabelEqual(string label)
        {
            this.label = label;
        }

        /// <summary>
        /// The given declaration is equal to the string.
        /// </summary>
        /// <param name="decl"></param>
        /// <returns></returns>
        public override bool Matches(DeclarationInfo decl)
        {
            if (label == null)
                return true;

            if (decl.DeclarationType == DeclarationInfo.DeclarationTypes.Variable && label.StartsWith("$"))
            {
                return decl.Label == label.Substring(1);
            }

            return decl.Label == label;
        }
    }

    /// <summary>
    /// Declaration is equal to one of given texts.
    /// Not case sensitive.
    /// </summary>
    class DeclarationLabelEqualMultiple : DeclarationMatches
    {
        private readonly List<DeclarationLabelEqual> labels = new List<DeclarationLabelEqual>();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="labels">Array of texts to be tested.</param>
        public DeclarationLabelEqualMultiple(List<string> labels)
        {
            if (labels != null)
            {
                Dictionary<string, bool> added = new Dictionary<string, bool>();
                bool bNullAdded = false;

                foreach(string str in labels)
                {
                    // duplicity check
                    if(str == null)
                    {
                        if (bNullAdded) continue;
                        bNullAdded = true;
                    }
                    else
                    {
                        if (added.ContainsKey(str)) continue;
                        added.Add(str, true);
                    }

                    // add
                    this.labels.Add(new DeclarationLabelEqual(str));
                }
            }
        }

        /// <summary>
        /// The given declaration is equal to the one of the list of strings.
        /// </summary>
        /// <param name="decl"></param>
        /// <returns></returns>
        public override bool Matches(DeclarationInfo decl)
        {
            foreach (DeclarationLabelEqual label in labels)
            {
                if (label.Matches(decl))
                    return true;
            }

            return false;
        }
    }

    #endregion

    /// <summary>
    /// Base class for declaration info.
    /// Used for other specified declarations.
    /// Provides info about position, name, description, member declarations, visibility, type etc.
    /// </summary>
    /// <remarks>
    /// The declaration info is displayable in the DropDown lists, it implements DropDownMember object.
    /// Declarations of all types, variables, constants, functions, namespaces and keywords are described by this class.
    /// </remarks>
    public class DeclarationInfo : DropDownMember
    {
        #region declaration attributes objects
        
        /// <summary>
        /// Types of declarations.
        /// </summary>
        [Flags]
        public enum DeclarationTypes
        {
            Nothing = 0,
            Variable = 1,
            Constant = 2,
            Function = 4,
            Class = 8,
            Keyword = 16,
            Namespace = 32,
        }

        /// <summary>
        /// Declaration visibility in scopes.
        /// </summary>
        [Flags]
        public enum DeclarationUsages
        {
            AllChildScopes = 1,// this declaration is visible in all children scopes recursively // continue, break, functions, properties
            ThisScope = 2,  // this declaration should be visible in own scope // private, public, protected
            UntilFunctionOrClass = 4, // this declaration is visible until function or class is reached // variables
        }

        /// <summary>
        /// Declaration accessibility visibility.
        /// </summary>
        [Flags]
        public enum DeclarationVisibilities
        {
            Public=1,
            Private=2,
            Protected=4,
        }

        /// <summary>
        /// Name of the PHP constructor function.
        /// </summary>
        public const string ConstructorName = "__construct";

        /// <summary>
        /// Name of the PHP destructor function.
        /// </summary>
        public const string DestructorName = "__destruct";

        /// <summary>
        /// Checks if the given member is static by the Phalanger PhpMemberAttributes.
        /// </summary>
        /// <param name="attributes">php member attribute</param>
        /// <returns>True if the given member is static.</returns>
        public static bool IsMemberStatic(PhpMemberAttributes attributes)
        {
            return (attributes & PhpMemberAttributes.StaticMask) != 0;
        }

        /// <summary>
        /// Get the given member visibility.
        /// </summary>
        /// <param name="attributes">Php member attribute</param>
        /// <returns>Member visibility.</returns>
        public static DeclarationVisibilities GetMemberVisibility(PhpMemberAttributes attributes)
        {
            if ((attributes & PhpMemberAttributes.Protected) != 0) return DeclarationVisibilities.Protected;
            if ((attributes & PhpMemberAttributes.Private) != 0) return DeclarationVisibilities.Private;

            return DeclarationVisibilities.Public;  // Default PHP visibility.
        }

        #endregion

        #region override equals operators, DropDownMember does not handle nulls
        public static bool operator !=(DeclarationInfo m1, DeclarationInfo m2)
        {
            return !ReferenceEquals(m1, m2);
        }

        public static bool operator ==(DeclarationInfo m1, DeclarationInfo m2)
        {
            return ReferenceEquals(m1, m2);
        }

        /*public bool Equals(string label)
        {
            if (label == null)
                return false;

            if (this.Label == label)
                return true;

            if (label.StartsWith("$") && DeclarationType == DeclarationTypes.Variable)
                return (label.TrimStart(new char[] { '$' }) == Label);

            return false;
        }*/
        
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj);
        }

        #endregion

        /// <summary>
        /// Base declaration item initialization.
        /// </summary>
        /// <param name="label">Declaration item name.</param>
        /// <param name="span">Position span in the source code text.</param>
        /// <param name="glyph">Image index.</param>
        public DeclarationInfo(string label, TextSpan span, int glyph, string description, ScopeInfo parentscope)
            : base(label, span, glyph, DROPDOWNFONTATTR.FONTATTR_PLAIN)
        {
            this.ParentScope = parentscope;
            this.description = description;
        }

        #region IntelliSense display

        /// <summary>
        /// Get element description.
        /// Should be taken from the documentation or other comments.
        /// Can be null.
        /// </summary>
        /// <returns>Element description.</returns>
        public virtual string GetDescription()
        {
            return description;
        }

        /// <summary>
        /// Check if two declarations are same for display in IntelliSense, so the second one will not be displayed in IntelliSense.
        /// </summary>
        /// <param name="decl">Second declaration.</param>
        /// <returns>True if declarations are commutable.</returns>
        public bool IsSameAs(DeclarationInfo/*!*/decl)
        {
            return
                (decl.Label == Label) &&
                (decl.Glyph == Glyph) &&
                (decl.DeclarationType == DeclarationType) /*&&
                (decl.FullName == FullName)*/
                                             ;
        }

        /// <summary>
        /// File containing this declaration.
        /// </summary>
        public virtual string FileName
        {
            get
            {
                return (ParentScope != null) ? ParentScope.FileName : null;
            }
        }

        /// <summary>
        /// Full name of the declaration.
        /// Contains parent scope FullName and the declaration label.
        /// </summary>
        public virtual string FullName
        {
            get
            {
                return ((ParentScope != null) ? (ParentScope.FullName + QualifiedName.Separator) : (null)) + Label;
            }
        }

        #endregion

        #region Parameters list

        private bool ParametersInitialized = false;

        private List<FunctionParamInfo> _ObjectParameters = null;
        private DeclarationList _MembersWithParameters = null;

        /// <summary>
        /// Object call parameters. Used for functions.
        /// </summary>
        public List<FunctionParamInfo>  ObjectParameters { get { InitParametersIfNotYet(); return _ObjectParameters; } }

        /// <summary>
        /// List of declarations which call parameters could be used as own call parameters. Used for class constructors.
        /// </summary>
        public DeclarationList MembersWithParameters { get { InitParametersIfNotYet(); return _MembersWithParameters; } }
        
        /// <summary>
        /// Init the the call parameters once.
        /// Call InitParameters.
        /// </summary>
        private void InitParametersIfNotYet()
        {
            if( !ParametersInitialized )
            {
                ParametersInitialized = true;
                InitParameters();
            }
        }

        /// <summary>
        /// Perform call parameters initialization.
        /// This method is called max once.
        /// </summary>
        protected virtual void InitParameters(){/*nothing here, have to be overloaded*/}

        /// <summary>
        /// Add function/method parameter.
        /// </summary>
        /// <param name="param"></param>
        public void AddParameter(FunctionParamInfo param)
        {
            if (param != null)
            {
                if (_ObjectParameters == null)
                    _ObjectParameters = new List<FunctionParamInfo>();
                InitParametersIfNotYet();
                _ObjectParameters.Add(param);
            }
        }

        /// <summary>
        /// Add declaration which parameters will be offered for declaration call.
        /// </summary>
        /// <param name="decl"></param>
        public void AddMemberWithParameter(DeclarationInfo decl)
        {
            if (decl != null)
            {
                if (_MembersWithParameters == null)
                    _MembersWithParameters = new DeclarationList();
                InitParametersIfNotYet();
                _MembersWithParameters.Add(decl);
            }
        }

        #endregion

        #region Value and possible Members Analyzers

        /// <summary>
        /// Analyzers resolving values and members after :: or ->.
        /// </summary>
        private List<ValueAnalyzer> _Analyzers = null;

        /// <summary>
        /// Add new possible value analyzer.
        /// </summary>
        /// <param name="analyzer"></param>
        public void AddAnalyzer(ValueAnalyzer analyzer)
        {
            if (analyzer != null)
            {
                if (_Analyzers == null)
                    _Analyzers = new List<ValueAnalyzer>();

                _Analyzers.Add(analyzer);
            }
        }

        /// <summary>
        /// Avoid infinite looping.
        /// </summary>
        private bool
            bGettingObjectMembers = false,
            bGettingStaticMembers = false,
            bGettingIndirectIdentifiers = false,
            bGettingArrayDeclarations = false;

        /// <summary>
        /// List of public object members (visible after -> in IntelliSence).
        /// </summary>
        /// <param name="projectdeclarations">All project declarations.</param>
        /// <param name="result">Output list of visible members.</param>
        /// <param name="contains">String contained in returned declarations.</param>
        public void GetObjectMembers(ProjectDeclarations projectdeclarations, DeclarationList result, DeclarationMatches match)
        {
            if (_Analyzers != null && !bGettingObjectMembers )
            {
		bGettingObjectMembers = true;
                foreach (ValueAnalyzer analyzer in _Analyzers)
                    analyzer.GetObjectMembers(projectdeclarations, result, match);
		bGettingObjectMembers  = false;
            }
        }

        /// <summary>
        /// List of public static members (visible after :: in IntelliSence).
        /// </summary>
        /// <param name="projectdeclarations">All project declarations.</param>
        /// <param name="result">Output list of constant visible members.</param>
        /// <param name="contains">String contained in returned declarations.</param>
        public void GetStaticMembers(ProjectDeclarations projectdeclarations, DeclarationList result, DeclarationMatches match)
        {
            if (_Analyzers != null && !bGettingStaticMembers)
            {
		bGettingStaticMembers = true;
                foreach (ValueAnalyzer analyzer in _Analyzers)
                    analyzer.GetStaticMembers(projectdeclarations, result, match);
		bGettingStaticMembers = false;
            }
        }

        /// <summary>
        /// Get list of declarations describing an array item usage.
        /// </summary>
        /// <param name="projectdeclarations"></param>
        /// <param name="localscope"></param>
        /// <param name="result"></param>
        public void GetArrayDeclarations(ProjectDeclarations projectdeclarations, DeclarationList result)
        {
            if (_Analyzers != null && !bGettingArrayDeclarations)
            {
                bGettingArrayDeclarations = true;
                foreach (ValueAnalyzer analyzer in _Analyzers)
                    analyzer.GetArrayDeclarations(projectdeclarations, result);
                bGettingArrayDeclarations = false;
            }
        }

        /// <summary>
        /// List of namespace member types declarations (visible after ::: in IntelliSense).
        /// </summary>
        /// <param name="projectdeclarations">All project declarations.</param>
        /// <param name="result">Output list of type members.</param>
        /// <param name="match">Matching function.</param>
        public virtual void GetTypeMembers(ProjectDeclarations projectdeclarations, DeclarationList result, DeclarationMatches match)
        {
        }

        /// <summary>
        /// Declaration equals to $().
        /// </summary>
        /// <param name="projectdeclarations">Project declarations.</param>
        /// <param name="localscope">Current scope.</param>
        /// <param name="declarations">Results.</param>
        public virtual void GetIndirectIdentifiers(ProjectDeclarations projectdeclarations, ScopeInfo localscope, List<string> declarations)
        {
            if (_Analyzers != null && !bGettingIndirectIdentifiers)
            {
		bGettingIndirectIdentifiers = true;
                foreach (ValueAnalyzer analyzer in _Analyzers)
                    analyzer.GetIndirectIdentifiers(projectdeclarations, localscope, declarations);
		bGettingIndirectIdentifiers = false;
            }
        }

        /// <summary>
        /// Get string-list of analyzed values.
        /// Without duplicities and without null values.
        /// </summary>
        /// <returns></returns>
        public List<string> GetResolvedValues()
        {
            List<string> result = null;

            if (_Analyzers != null)
            {
                foreach (ValueAnalyzer analyzer in _Analyzers)
                {
                    string value = analyzer.GetValueText();

                    if ( value != null )
                    {
                        if (result == null) result = new List<string>();

                        bool exists = false;
                        foreach (string str in result)
                            if ( str == value)
                            {
                                exists = true;
                                break;
                            }

                        if ( !exists )
                            result.Add(value);
                    }
                }
            }

            return result;
        }

        #endregion

        #region Declaration Attributes

        /// <summary>
        /// Parent code scope.
        /// </summary>
        public readonly ScopeInfo ParentScope;

        /// <summary>
        /// Assigned code scope. Provides list of declarations. Can be null.
        /// </summary>
        public ScopeInfo DeclarationScope = null;

        /// <summary>
        /// Declaration description, can be null.
        /// </summary>
        protected string description;

        /// <summary>
        /// Type of the scope. ONLY ONE TYPE, NOT BITMASK.
        /// </summary>
        /// <remarks>Only one type, not mask!</remarks>
        public virtual DeclarationTypes DeclarationType
        {
            get
            {
                return DeclarationTypes.Nothing;
            }
        }

        /// <summary>
        /// Accessibility of the declaration.
        /// </summary>
        public virtual DeclarationVisibilities DeclarationVisibility
        {
            get
            {
                return DeclarationVisibilities.Public;
            }
        }

        /// <summary>
        /// Declaration visibility in different scopes.
        /// </summary>
        public virtual DeclarationUsages DeclarationUsage
        {
            get
            {
                return DeclarationUsages.ThisScope | DeclarationUsages.AllChildScopes;
            }
        }
        
        /// <summary>
        /// Declaration is static.
        /// </summary>
        public virtual bool IsStatic
        {
            get
            {
                return false;
            }
        }
        
        #endregion

    }

}