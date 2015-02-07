using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using PHP.Core.Reflection;

namespace PHP.Core
{
    /// <summary>
    /// Structuralized representation of PHPDoc DocBlock.
    /// </summary>
    /// <remarks>define() statements, functions, classes, class methods, and class vars, include() statements, and global variables can all be documented.
    /// See http://en.wikipedia.org/wiki/PHPDoc for specifications.</remarks>
    public sealed class PHPDocBlock
    {
        #region Nested classes: Element

        public abstract class Element
        {
            /// <summary>
            /// Every PHPDoc not starting with this character is ignored.
            /// </summary>
            private const char PHPDocFirstChar = '*';

            /// <summary>
            /// Every PHPDoc tag starts with this character.
            /// </summary>
            private const char PHPDocTagChar = '@';

            #region Tags

            /// <summary>
            /// Tag elements initialized using reflection.
            /// </summary>
            private static Dictionary<string, Func<string, string, Element>>/*!!*/elementFactories;

            static Element()
            {
                // initilize dictionary of known tags and their factories:
                elementFactories = new Dictionary<string, Func<string, string, Element>>(20, StringComparer.Ordinal);
                var types = typeof(PHPDocBlock).GetNestedTypes(System.Reflection.BindingFlags.Public);
                
                foreach (var t in types)
                {
                    if (t.IsSealed && !t.IsAbstract && typeof(Element).IsAssignableFrom(t))
                    {
                        var ctors = t.GetConstructors();
                        Debug.Assert(ctors != null && ctors.Length == 1);
                        var ctor = ctors[0];

                        Func<string, string, Element> factory = null;
                        var args = ctor.GetParameters();
                        Debug.Assert(args != null && args.Length <= 2);

                        // create function that creates the Element 't':
                        if (args.Length == 0)
                            factory = (tagName, line) => (Element)ctor.Invoke(null);
                        else if (args.Length == 1)
                            factory = (tagName, line) => (Element)ctor.Invoke(new object[]{line});
                        else
                            factory = (tagName, line) => (Element)ctor.Invoke(new object[]{tagName, line});
                        
                        // add to the dictionary according to its Name:
                        var fld = t.GetField("Name");
                        if (fld != null)
                        {
                            elementFactories.Add(TagNameHelper(fld), factory);
                        }
                        else
                        {
                            var f1 = t.GetField("Name1");
                            var f2 = t.GetField("Name2");
                            if (f1 != null && f2 != null)
                            {
                                elementFactories.Add(TagNameHelper(f1), factory);
                                elementFactories.Add(TagNameHelper(f2), factory);
                            }
                            else
                            {
                                // only these Elements do not represent a tag:
                                Debug.Assert(t.Name == typeof(ShortDescriptionElement).Name || t.Name == typeof(LongDescriptionElement).Name);
                            }
                        }
                    }
                }

                // ensure we have some tags:
                Debug.Assert(elementFactories.ContainsKey("@param"));
                Debug.Assert(elementFactories.ContainsKey("@ignore"));
                Debug.Assert(elementFactories.ContainsKey("@var"));
                // ...
            }

            /// <summary>
            /// Reads value of given field, assuming it is string constant, which value starts with <see cref="PHPDocTagChar"/>.
            /// </summary>
            private static string TagNameHelper(System.Reflection.FieldInfo fld)
            {
                Debug.Assert(fld != null);

                var tagname = fld.GetValue(null) as string;

                Debug.Assert(!string.IsNullOrEmpty(tagname));
                Debug.Assert(tagname[0] == PHPDocTagChar);

                return tagname;
            }

            private static KeyValuePair<string, Func<string, string, Element>> FindTagInfo(string/*!*/line)
            {
                Debug.Assert(!string.IsNullOrEmpty(line));
                Debug.Assert(line[0] == PHPDocTagChar);

                int endIndex = 1;
                while (endIndex < line.Length && !char.IsWhiteSpace(line[endIndex]))
                    endIndex++;

                string tagName = (endIndex < line.Length) ? line.Remove(endIndex) : line;

                Func<string,string,Element> tmp;
                if (elementFactories.TryGetValue(tagName, out tmp))
                    return new KeyValuePair<string, Func<string, string, Element>>(tagName, tmp);
                else
                    return new KeyValuePair<string, Func<string, string, Element>>();
            }

            #endregion

            /// <summary>
            /// Prepares given <paramref name="line"/>.
            /// 
            /// If the line creates new PHPDoc element, new <see cref="Element"/>
            /// is instantiated and returned in <paramref name="next"/>.
            /// </summary>
            /// <param name="line">Line to parse. Cannot be <c>null</c> reference.</param>
            /// <param name="next">Outputs new element that will follow current element. Parsing will continue using this element.</param>
            /// <returns>If the line can be parsed, method returns <c>true</c>.</returns>
            internal static bool TryParseLine(ref string/*!*/line, out Element next)
            {
                if (line == null)
                    throw new ArgumentNullException("line");

                next = null;

                int startIndex = 0;
                while (startIndex < line.Length && char.IsWhiteSpace(line[startIndex])) startIndex++;   // skip whitespaces

                // invalid PHPDoc line (not starting with '*'):
                if (startIndex == line.Length || line[startIndex] != PHPDocFirstChar)
                    return false;

                // trim starting '*' and whitespaces
                startIndex++;   // skip '*'
                while (startIndex < line.Length && char.IsWhiteSpace(line[startIndex])) startIndex++;   // skip whitespaces

                if (startIndex == line.Length)
                {
                    line = string.Empty;
                }
                else
                {
                    // trim end
                    int endIndex = line.Length;
                    while (endIndex > startIndex && char.IsWhiteSpace(line[endIndex - 1])) endIndex--;  // skip whitespaces from end
                    line = line.Substring(startIndex, endIndex - startIndex).Replace("{@*}", "*/");
                }

                // check "*/" at the end
                if (line.Length == 1 && line[0] == '/')
                    return false;   // empty line
                if (line.Length >= 2 && line[line.Length - 1] == '/' && line[line.Length - 2] == '*')  // "*/" found at the end
                    line = line.Remove(line.Length - 2);

                // TODO: any whitespace sequence is converted into single space, but only outside <pre> block

                // check tags:
                next = CreateElement(line);                    

                // 
                return true;
            }

            /// <summary>
            /// Parses given <paramref name="line"/> and updates current content.
            /// </summary>
            /// <param name="line">Line to parse. Line is trimmed and does not start with '*'. Cannot be <c>null</c> reference.</param>
            /// <param name="next">Next element to continue parsing with.</param>
            internal abstract void ParseLine(string/*!*/line, out Element next);

            /// <summary>
            /// Reads tag at the beginning of line and tries to create corresponding <see cref="Element"/> instance.
            /// </summary>
            /// <param name="line">PHPDoc comment line. Assuming the line starts with a PHPDoc tag. Otherwise, or if tag is not recognized, <c>null</c> is returned..</param>
            private static Element CreateElement(string/*!*/line)
            {
                Debug.Assert(line != null);

                if (line.Length == 0 || line[0] != PHPDocTagChar)
                    return null;

                // try to match known tags:
                var tagInfo = FindTagInfo(line);
                if (tagInfo.Key != null)
                {
                    Debug.Assert(tagInfo.Value != null);
                    
                    // initialize new tag element
                    return tagInfo.Value(tagInfo.Key, line);
                }
                
                // unrecognized tag:
                return null;
            }

            /// <summary>
            /// Checks if given string starts with tag name, which is followed by new line or a whitespace.
            /// </summary>
            private static bool IsTagLine(string/*!*/line, string/*!*/tagName)
            {
                Debug.Assert(line != null);
                Debug.Assert(tagName != null);

                return line.StartsWith(tagName, StringComparison.Ordinal) &&
                    (line.Length == tagName.Length || char.IsWhiteSpace(line[tagName.Length]));
            }

            /// <summary>
            /// Returns <c>true</c> if current element does not contain any information and can be ignored.
            /// </summary>
            internal virtual bool IsEmpty { get { return false; } }
        }

        /// <summary>
        /// Short description.
        /// </summary>
        public sealed class ShortDescriptionElement : Element
        {
            /// <summary>
            /// Character defining the end of PHPDoc short description.
            /// </summary>
            private const char EndChar = '.';

            public string Text { get; private set; }

            public ShortDescriptionElement()
            {

            }

            internal override void ParseLine(string/*!*/line, out Element next)
            {
                next = null;

                // Short Description can be followed by Long Description.
                // It can be only 3 lines long, otherwise only the first line is taken
                // It is terminated by empty line or a dot.

                if (this.Text != null && (this.Text.LastCharacter() == (int)EndChar))
                {
                    next = new LongDescriptionElement(line);
                }
                else if (line.Length == 0)
                {
                    next = new LongDescriptionElement(null);
                }
                else if (this.Text.CharsCount('\n') >= 2)
                {
                    // short description has already 3 lines,
                    // only first line is taken, the rest is for LongDescriptionElement
                    int firstLineEndIndex = this.Text.IndexOf('\n');
                    Debug.Assert(firstLineEndIndex != -1);

                    next = new LongDescriptionElement(this.Text.Substring(firstLineEndIndex + 1));
                    this.Text = this.Text.Remove(firstLineEndIndex);
                }
                else
                {
                    this.Text = (this.Text != null) ? (this.Text + '\n' + line) : line;
                }
            }

            internal override bool IsEmpty { get { return string.IsNullOrWhiteSpace(this.Text); } }

            public override string ToString()
            {
                return this.Text ?? string.Empty;
            }
        }

        /// <summary>
        /// Long description.
        /// </summary>
        public sealed class LongDescriptionElement : Element
        {
            public string Text { get; private set; }

            public LongDescriptionElement(string initialText)
            {
                this.Text = initialText;
            }

            internal override void ParseLine(string line, out Element next)
            {
                // Long Description can only be followed by PHPDoc tag (handled in TryParseLine)

                next = null;
                this.Text = (this.Text != null) ? (this.Text + '\n' + line) : line;
            }

            internal override bool IsEmpty { get { return string.IsNullOrWhiteSpace(this.Text); } }

            public override string ToString()
            {
                return this.Text ?? string.Empty;
            }
        }

        public abstract class EmptyTag : Element
        {
            internal override void ParseLine(string line, out Element next)
            {
                next = null;
                // ignored
            }
        }

        /// <summary>
        /// Documents an abstract class, class variable or method.
        /// </summary>
        public sealed class AbstractTag : EmptyTag
        {
            public const string Name = "@abstract";

            public override string ToString()
            {
                return Name;
            }
        }

        /// <summary>
        /// Documents access control for an element. @access private indicates that documentation of element be prevented.
        /// </summary>
        public sealed class AccessTag : Element
        {
            public const string Name = "@access";

            private const string IsPublic = "public";
            private const string IsPrivate = "private";
            private const string IsProtected = "protected";

            /// <summary>
            /// Resolved access modifier. (public, private or protected)
            /// </summary>
            public PhpMemberAttributes Access { get { return attributes & PhpMemberAttributes.VisibilityMask; } }
            private readonly PhpMemberAttributes attributes;

            private string AccessString
            {
                get
                {
                    switch (Access)
                    {
                        case PhpMemberAttributes.Private: return IsPrivate;
                        case PhpMemberAttributes.Protected: return IsProtected;
                        default: return IsPublic;
                    }
                }
            }

            public AccessTag(string/*!*/line)
            {
                if (line.Length > Name.Length)
                    // public, private or protected
                    switch (line.Substring(Name.Length + 1).Trim().ToLowerInvariant())
                    {
                        case IsPublic: attributes = PhpMemberAttributes.Public; break;
                        case IsPrivate: attributes = PhpMemberAttributes.Private; break;
                        case IsProtected: attributes = PhpMemberAttributes.Protected; break;
                        default:
                            System.Diagnostics.Debug.WriteLine("Unexpected access modifier in PHPDoc @access tag, line:" + line);
                            break;
                    }
            }

            internal override void ParseLine(string line, out Element next)
            {
                next = null;
                // ignored
            }

            public override string ToString()
            {
                return Name + " " + AccessString;
            }
        }

        public abstract class SingleLineTag : Element
        {
            protected readonly string text;

            internal SingleLineTag(string/*!*/tagName, string/*!*/line)
            {
                Debug.Assert(line.StartsWith(tagName));

                if (line.Length > tagName.Length)
                    this.text = line.Substring(tagName.Length + 1).Trim();
            }

            internal override void ParseLine(string line, out Element next)
            {
                next = null;
                // other lines are ignored
            }

            internal override bool IsEmpty
            {
                get
                {
                    return string.IsNullOrWhiteSpace(text);
                }
            }
        }

        /// <summary>
        /// Documents the author of the current element.
        /// </summary>
        public sealed class AuthorTag : SingleLineTag
        {
            public const string Name = "@author";

            /// <summary>
            /// author name &lt;author@email&gt;
            /// </summary>
            public string Author { get { return text; } }

            public AuthorTag(string/*!*/line)
                :base(Name, line)
            {
                
            }

            public override string ToString()
            {
                return Name + " " + Author;
            }
        }

        /// <summary>
        /// Documents copyright information.
        /// </summary>
        public sealed class CopyrightTag : SingleLineTag
        {
            public const string Name = "@copyright";

            /// <summary>
            /// name date
            /// </summary>
            public string Copyright { get { return text; } }

            public CopyrightTag(string/*!*/line)
                : base(Name, line)
            {

            }

            public override string ToString()
            {
                return Name + " " + Copyright;
            }
        }

        /// <summary>
        /// Documents a method as deprecated.
        /// </summary>
        public sealed class DeprecatedTag : SingleLineTag
        {
            public const string Name1 = "@deprecated";
            public const string Name2 = "@deprec";

            /// <summary>
            /// version
            /// </summary>
            public string Version { get { return text; } }

            public DeprecatedTag(string tagName, string/*!*/line)
                : base(tagName, line)
            {

            }

            public override string ToString()
            {
                return Name1 + " " + Version;
            }
        }

        /// <summary>
        /// Documents the location of an external saved example file.
        /// </summary>
        public sealed class ExampleTag : SingleLineTag
        {
            public const string Name = "@example";
            
            /// <summary>
            /// /path/to/example
            /// </summary>
            public string Example { get { return text; } }

            public ExampleTag(string/*!*/line)
                : base(Name, line)
            {

            }

            public override string ToString()
            {
                return Name + " " + Example;
            }
        }

        /// <summary>
        /// Documents an exception thrown by a method.
        /// </summary>
        public sealed class ExceptionTag : SingleLineTag
        {
            public const string Name1 = "@exception";
            public const string Name2 = "@throws";

            /// <summary>
            /// version
            /// </summary>
            public string Exception { get { return text; } }

            public ExceptionTag(string tagName, string/*!*/line)
                : base(tagName, line)
            {

            }

            public override string ToString()
            {
                return Name1 + " " + this.Exception;
            }
        }

        /// <summary>
        /// Documents any tag in a form of "type [$varname] [multilined-description]".
        /// </summary>
        public abstract class TypeVarDescTag : Element
        {
            /// <summary>
            /// Character separating type names within <see cref="TypeNames"/> property.
            /// </summary>
            public const char TypeNamesSeparator = '|';

            /// <summary>
            /// Optional. Type names separated by '|'.
            /// </summary>
            public readonly string TypeNames;

            /// <summary>
            /// Array of type names. Cannot be <c>null</c>. Can be an empty array.
            /// </summary>
            public string[]/*!!*/TypeNamesArray { get { return string.IsNullOrEmpty(TypeNames) ? ArrayUtils.EmptyStrings : TypeNames.Split(new char[] { TypeNamesSeparator }, StringSplitOptions.RemoveEmptyEntries); } }

            /// <summary>
            /// Optional. Variable name, starts with '$'.
            /// </summary>
            public readonly string VariableName;

            /// <summary>
            /// Optional. Element description.
            /// </summary>
            public string Description { get; private set; }

            protected TypeVarDescTag(string/*!*/tagName, string/*!*/line, bool allowVariableName)
            {
                Debug.Assert(line.StartsWith(tagName));

                // [type] [$varname] [description]

                int index = tagName.Length; // current index within line
                int descStart = index;  // start of description, moved when [type] or [$varname] found

                // try to find [type]
                string word = NextWord(line, ref index);
                if (word != null && word[0] != '$')
                {
                    this.TypeNames = word;
                    descStart = index;
                    word = NextWord(line, ref index);
                }

                // try to find [$varname]
                if (word != null && allowVariableName && word[0] == '$')
                {
                    this.VariableName = word;
                    descStart = index;
                }

                if (descStart < line.Length)
                    this.Description = line.Substring(descStart).TrimStart(null/*default whitespace characters*/);
            }

            #region Helpers

            private static string NextWord(string/*!*/text, ref int index)
            {   
                // skip whitespaces:
                while (index < text.Length && char.IsWhiteSpace(text[index]))
                    index++;

                // read word:
                int startIndex = index;
                while (index < text.Length && !char.IsWhiteSpace(text[index]))
                    index++;

                // cut off the word:
                if (startIndex < index)
                    return text.Substring(startIndex, index - startIndex);
                else
                    return null;
            }

            #endregion

            internal override void ParseLine(string line, out Element next)
            {
                next = null;

                // add the line into description:
                Description = string.IsNullOrWhiteSpace(Description) ? line : (Description + '\n' + line);
            }

            internal override bool IsEmpty
            {
                get
                {
                    return string.IsNullOrEmpty(this.TypeNames) && string.IsNullOrEmpty(this.VariableName) && string.IsNullOrWhiteSpace(this.Description);
                }
            }
        }

        /// <summary>
        /// Documents a global variable or its use in a function or method.
        /// @global	type $globalvarname
        /// </summary>
        public sealed class GlobalTag : TypeVarDescTag
        {
            public const string Name = "@global";

            public GlobalTag(string/*!*/line)
                :base(Name, line, true)
            {
            }

            public override string ToString()
            {
                return Name + " " + TypeNames + " " + VariableName;
            }
        }

        /// <summary>
        /// Prevents the documentation of an element.
        /// </summary>
        public sealed class IgnoreTag : EmptyTag
        {
            public const string Name = "@ignore";

            public override string ToString()
            {
                return Name;
            }
        }

        public abstract class TextTag : Element
        {
            public string Text { get; private set; }

            public TextTag(string/*!*/tagName, string/*!*/line)
            {
                Debug.Assert(line.StartsWith(tagName));
                this.Text = line.Substring(tagName.Length).TrimStart(null);
            }

            internal override void  ParseLine(string line, out Element next)
            {
                next = null;
                this.Text = string.IsNullOrEmpty(this.Text) ? line : (this.Text + '\n' + line);
            }
        }

        /// <summary>
        /// Private information for advanced developers.
        /// </summary>
        public sealed class InternalTag : TextTag
        {
            public const string Name = "@internal";

            public InternalTag(string/*!*/line)
                :base(Name, line)
            {                
            }

            public override string ToString()
            {
                return Name + "\n" + Text;
            }
        }

        ///// <summary>
        ///// URL information.
        ///// </summary>
        //public sealed class LinkTag : SingleLineTag
        //{
        //    public const string Name = "@link";

        //    /// <summary>
        //    /// URL
        //    /// </summary>
        //    public string Url { get { return this.text; } }

        //    public LinkTag(string/*!*/line)
        //        :base(Name, line)
        //    {

        //    }
        //}

        /// <summary>
        /// Specifies an alias for a variable. For example, $GLOBALS['myvariable'] becomes $myvariable.
        /// </summary>
        public sealed class NameTag : SingleLineTag
        {
            public const string Name = "@name";

            /// <summary>
            /// Variable name. Empty string or a name starting with '$' character.
            /// </summary>
            public string VariableName { get { return string.IsNullOrEmpty(this.text) ? string.Empty : ((this.text[0] == '$') ? this.text : ('$' + this.text)); } }

            public NameTag(string/*!*/line)
                : base(Name, line)
            {
                
            }
        }

        /// <summary>
        /// phpdoc.de compatibility "phpDocumentor tags".
        /// </summary>
        public sealed class MagicTag : EmptyTag
        {
            public const string Name = "@magic";

            public override string ToString()
            {
                return Name;
            }
        }

        /// <summary>
        /// Documents a group of related classes and functions.
        /// </summary>
        public sealed class PackageTag : SingleLineTag
        {
            public const string Name = "@package";

            /// <summary>
            /// Name of the package.
            /// </summary>
            public string PackageName { get { return this.text; } }

            public PackageTag(string/*!*/line)
                : base(Name, line)
            {

            }
        }

        /// <summary>
        /// Documents a group of related classes and functions within a package.
        /// </summary>
        public sealed class SubPackageTag : SingleLineTag
        {
            public const string Name = "@subpackage";

            /// <summary>
            /// Name of the sub-package.
            /// </summary>
            public string SubPackageName { get { return this.text; } }

            public SubPackageTag(string/*!*/line)
                : base(Name, line)
            {

            }
        }

        /// <summary>
        /// Documents a parameter.
        /// @param type [$varname] description
        /// </summary>
        public sealed class ParamTag : TypeVarDescTag
        {
            public const string Name = "@param";

            public ParamTag(string/*!*/line)
                : base(Name, line, true)
            {
            }

            public override string ToString()
            {
                return Name + " " + TypeNames + " " + VariableName + "\n" + Description;
            }
        }

        /// <summary>
        /// Documents function return value. This tag should not be used for constructors or methods defined with a void return type
        /// @return type [description]
        /// </summary>
        public sealed class ReturnTag : TypeVarDescTag
        {
            public const string Name = "@return";

            public ReturnTag(string/*!*/line)
                : base(Name, line, false)
            {
            }

            public override string ToString()
            {
                return Name + " " + TypeNames + "\n" + Description;
            }
        }

        /// <summary>
        /// Documents an association to any element (global variable, include, page, class, function, define, method, variable).
        /// </summary>
        public sealed class SeeTag : SingleLineTag
        {
            public const string Name = "@see";

            /// <summary>
            /// element
            /// </summary>
            public string ElementName { get { return this.text; } }

            public SeeTag(string/*!*/line)
                : base(Name, line)
            {

            }
        }

        /// <summary>
        /// Documents when a method was added to a class.
        /// </summary>
        public sealed class SinceTag : SingleLineTag
        {
            public const string Name = "@since";

            /// <summary>
            /// version
            /// </summary>
            public string Version { get { return this.text; } }

            public SinceTag(string/*!*/line)
                : base(Name, line)
            {

            }
        }

        /// <summary>
        /// Documents a static class or method.
        /// </summary>
        public sealed class StaticTag : EmptyTag
        {
            public const string Name = "@static";

            public override string ToString()
            {
                return Name;
            }
        }

        /// <summary>
        /// Documents a static variable's use in a function or class.
        /// </summary>
        public sealed class StaticVarTag : TypeVarDescTag
        {
            public const string Name = "@staticvar";

            public StaticVarTag(string/*!*/line)
                :base(Name, line, false)
            {

            }

            public override string ToString()
            {
                return Name + " " + this.TypeNames;
            }
        }

        /// <summary>
        /// Documents things that need to be done to the code at a later date.
        /// </summary>
        public sealed class TodoTag : TextTag
        {
            public const string Name = "@todo";

            public TodoTag(string/*!*/line)
                :base(Name, line)
            {
            }

            public override string ToString()
            {
                return Name + "\n" + Text;
            }
        }

        public sealed class VarTag : TypeVarDescTag
        {
            public const string Name = "@var";

            public VarTag(string/*!*/line)
                : base(Name, line, false)
            {
            }

            public override string ToString()
            {
                return Name + " " + this.TypeNames;
            }
        }

        public sealed class VersionTag : SingleLineTag
        {
            public const string Name = "@version";

            public string Version { get { return this.text; } }

            public VersionTag(string/*!*/line)
                : base(Name, line)
            {
            }

            public override string ToString()
            {
                return Name + " " + this.Version;
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// Empty singleton <see cref="Element"/> array.
        /// </summary>
        private static readonly Element[]/*!*/EmptyElements = new Element[0];

        /// <summary>
        /// Original PHPDoc text, including comment tags.
        /// </summary>
        private readonly string doccomment;

        /// <summary>
        /// Parsed data. Lazily initialized.
        /// </summary>
        private Element[] elements;

        /// <summary>
        /// Elements within the PHPDoc block. Some elements may be ignored due to missing information.
        /// Cannot be <c>null</c> reference.
        /// </summary>
        public Element[]/*!*/Elements
        {
            get
            {
                Initialize();
                return this.elements ?? EmptyElements;
            }
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes new instance of <see cref="PHPDocBlock"/>.
        /// </summary>
        public PHPDocBlock(string doccomment)
        {
            this.doccomment = doccomment;
        }

        /// <summary>
        /// Parses PHPDoc comment if not yet. 
        /// </summary>
        private void Initialize()
        {
            if (this.elements != null || string.IsNullOrEmpty(this.doccomment))
                return; // nothing to do

            //
            lock (this)
            {
                // double-checked lock
                if (this.elements == null)
                {
                    var elements = ParseNoLock(this.doccomment);
                    Debug.Assert(elements != null);

                    this.elements = elements.ToArray();
                }
            }
        }

        /// <summary>
        /// Parses given <paramref name="doccomment"/> into a list of <see cref="Element"/> instances.
        /// </summary>
        private static List<Element>/*!*/ParseNoLock(string doccomment)
        {
            Debug.Assert(doccomment != null);

            var result = new List<Element>();
            var reader = new StringReader(doccomment);
            string line;
            Element tmp;
            
            Element current = new ShortDescriptionElement();
            
            while ((line = reader.ReadLine()) != null)
            {
                if (Element.TryParseLine(ref line, out tmp))    // validate the line, process tags
                {
                    Debug.Assert(line != null);

                    if (tmp == null)    // no new element created
                        current.ParseLine(line, out tmp);       // pass the line into the current element

                    if (tmp != null)    // new element created, it is already initialized with the current line
                    {
                        if (!current.IsEmpty)
                            result.Add(current);

                        current = tmp;  // it is current element from now
                    }
                }
            }

            // add the last found element
            if (!current.IsEmpty)
                result.Add(current);

            //
            return result;
        }

        #endregion

        #region Helper access methods

        public T GetElement<T>()  where T: Element
        {
            var elements = this.Elements;
            for (int i = 0; i < elements.Length; i++)
                if (elements[i] is T)
                    return (T)elements[i];

            return null;
        }

        /// <summary>
        /// Enumerate all the '@param' tags.
        /// </summary>
        public IEnumerable<ParamTag> Params
        {
            get
            {
                return this.Elements.OfType<ParamTag>();
            }
        }

        /// <summary>
        /// Gets '@return' tag or <c>null</c>.
        /// </summary>
        public ReturnTag Returns
        {
            get
            {
                return GetElement<ReturnTag>();
            }
        }

        /// <summary>
        /// Whether the PHPDoc block contains '@ignore' tag.
        /// </summary>
        public bool IsIgnored
        {
            get
            {
                return GetElement<IgnoreTag>() != null;
            }
        }

        /// <summary>
        /// Gets short description or <c>null</c>.
        /// </summary>
        public string ShortDescription
        {
            get
            {
                var tag = GetElement<ShortDescriptionElement>();
                return (tag != null) ? tag.Text : null;
            }
        }

        /// <summary>
        /// Gets long description or <c>null</c>.
        /// </summary>
        public string LongDescription
        {
            get
            {
                var tag = GetElement<LongDescriptionElement>();
                return (tag != null) ? tag.Text : null;
            }
        }

        /// <summary>
        /// Gets whole description, as a concatenation of <see cref="ShortDescription"/> and <see cref="LongDescription"/>.
        /// </summary>
        public string Summary
        {
            get
            {
                var shortdesc = ShortDescription;
                var longdesc = LongDescription;

                if (shortdesc != null || longdesc != null)
                {
                    if (shortdesc == null)
                        return longdesc;

                    if (longdesc == null)
                        return shortdesc;

                    return shortdesc + '\n' + longdesc;
                }

                return null;
            }
        }

        /// <summary>
        /// Gets '@access' value or 'Public' if no such tag is found.
        /// </summary>
        public PhpMemberAttributes Access
        {
            get
            {
                var access = GetElement<AccessTag>();
                return (access != null) ? access.Access : PhpMemberAttributes.Public;
            }
        }
        
        #endregion
    }
}
