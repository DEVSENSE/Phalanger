/*

 Copyright (c) 2012 DEVSENSE
  
 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PHP.Core;
using System.IO;
using System.Text.RegularExpressions;

namespace PHP.Library
{

    //TODO: try to output slashes in a not uniform and ugly way as PHP does

    public static class PhpGlob
    {
        #region Enums: GlobOptions, FnMatchOptions

        /// <summary>
        /// Flags used in call to <c>glob()</c>.
        /// </summary>
        [Flags]
        public enum GlobOptions
        {
            /// <summary>
            /// No flags.
            /// </summary>
            None = 0,

            /// <summary>
            /// Append system directory separator (slash) to matching directories.
            /// </summary>
            [ImplementsConstant("GLOB_MARK")]
            Mark = 0x0008,

            /// <summary>
            /// Return pattern itself if nothing matches.
            /// </summary>
            [ImplementsConstant("GLOB_NOCHECK")]
            NoCheck = 0x0010,

            /// <summary>
            /// Don't sort.
            /// </summary>
            [ImplementsConstant("GLOB_NOSORT")]
            NoSort = 0x0020,//TODO: support this somehow

            /// <summary>
            /// Expand braces ala csh.
            /// </summary>
            [ImplementsConstant("GLOB_BRACE")]
            Brace = 0x0080,

            /// <summary>
            /// Disable backslash escaping.
            /// </summary>
            [ImplementsConstant("GLOB_NOESCAPE")]
            NoEscape = 0x1000,

            /// <summary>
            /// List directories only.
            /// </summary>
            [ImplementsConstant("GLOB_ONLYDIR")]
            OnlyDir = 0x40000000,

            /// <summary>
            /// List directories only.
            /// </summary>
            [ImplementsConstant("GLOB_ERR")]
            StopOnError = 0x4
        }

        /// <summary>
        /// Flags used in call to <c>fnmatch()</c>.
        /// </summary>
        public enum FnMatchOptions
        {
            /// <summary>
            /// No flags.
            /// </summary>
            None = 0,

            /// <summary>
            /// Caseless match. Part of the GNU extension. 
            /// </summary>
            [ImplementsConstant("FNM_CASEFOLD")]
            CaseFold = 0x0010,

            /// <summary>
            /// Leading period in string must be exactly matched by period in the given pattern. 
            /// </summary>
            [ImplementsConstant("FNM_PERIOD")]
            Period = 0x0004,

            /// <summary>
            /// Disable backslash escaping. 
            /// </summary>
            [ImplementsConstant("FNM_NOESCAPE")]
            NoEscape = 0x0001,

            /// <summary>
            /// Slash in string only matches slash in the given pattern. 
            /// </summary>
            [ImplementsConstant("FNM_PATHNAME")]
            PathName = 0x0002
        }


        #endregion

        private class CharClass
        {
            private readonly StringBuilder/*!*/ _chars = new StringBuilder();

            internal void Add(char c)
            {
                if (c == ']' || c == '\\')
                {
                    _chars.Append('\\');
                }
                _chars.Append(c);
            }

            internal string MakeString()
            {
                if (_chars.Length == 0)
                {
                    return null;
                }
                if (_chars.Length == 1 && _chars[0] == '^')
                {
                    _chars.Insert(0, "\\");
                }
                _chars.Insert(0, "[");
                _chars.Append(']');
                return _chars.ToString();
            }
        }

        /// <summary>
        /// Replaces all slashes with <c>/</c>.
        /// </summary>
        /// <param name="pattern">Path pattern.</param>
        /// <returns>Canonized pattern.</returns>
        private static string CanonizePattern(string/*!*/pattern)
        {
            Debug.Assert(pattern != null);
            return pattern.Replace('\\', '/');
        }

        private static void AppendExplicitRegexChar(StringBuilder/*!*/ builder, char c)
        {
            builder.Append('[');
            if (c == '^' || c == '\\')
            {
                builder.Append('\\');
            }
            builder.Append(c);
            builder.Append(']');
        }

        internal static string/*!*/ PatternToRegex(string/*!*/ pattern, bool pathName, bool noEscape)
        {
            StringBuilder result = new StringBuilder(pattern.Length);
            result.Append("\\G");

            bool inEscape = false;
            CharClass charClass = null;

            foreach (char c in pattern)
            {
                if (inEscape)
                {
                    if (charClass != null)
                    {
                        charClass.Add(c);
                    }
                    else
                    {
                        AppendExplicitRegexChar(result, c);
                    }
                    inEscape = false;
                    continue;
                }
                else if (c == '\\' && !noEscape)
                {
                    inEscape = true;
                    continue;
                }

                if (charClass != null)
                {
                    if (c == ']')
                    {
                        string set = charClass.MakeString();
                        if (set == null)
                        {
                            // PHP regex "[]" matches nothing
                            // CLR regex "[]" throws exception
                            return String.Empty;
                        }
                        result.Append(set);
                        charClass = null;
                    }
                    else
                    {
                        charClass.Add(c);
                    }
                    continue;
                }
                switch (c)
                {
                    case '*':
                        result.Append(pathName ? "[^/]*" : ".*");
                        break;

                    case '?':
                        result.Append('.');
                        break;

                    case '[':
                        charClass = new CharClass();
                        break;

                    default:
                        AppendExplicitRegexChar(result, c);
                        break;
                }
            }

            return (charClass == null) ? result.ToString() : String.Empty;
        }

        private sealed class GlobMatcher
        {
            private readonly string/*!*/ _pattern;
            private readonly GlobOptions _flags;
            private readonly List<string>/*!*/ _result;
            private readonly bool _dirOnly;
            private bool _stripTwo;
            private bool _relative;
            private FnMatchOptions _fnMatchFlags;

            private bool NoEscapes
            {
                get { return ((_flags & GlobOptions.NoEscape) != 0); }
            }

            private bool StopOnError
            {
                get { return ((_flags & GlobOptions.StopOnError) != 0); }
            }

            private bool Mark
            {
                get { return ((_flags & GlobOptions.Mark) != 0); }
            }

            public GlobMatcher(string/*!*/ pattern, GlobOptions flags)
            {
                _pattern = CanonizePattern(pattern);
                _flags = flags;
                _result = new List<string>();
                _dirOnly = _pattern.LastCharacter() == '/' || (flags & GlobOptions.OnlyDir) != 0;

                _fnMatchFlags = NoEscapes ? FnMatchOptions.NoEscape : FnMatchOptions.None;
            }


            private static string/*!*/ Unescape(string/*!*/ path, int start)
            {
                StringBuilder unescaped = new StringBuilder();
                bool inEscape = false;
                for (int i = start; i < path.Length; i++)
                {
                    char c = path[i];
                    if (inEscape)
                    {
                        inEscape = false;
                    }
                    else if (c == '\\')
                    {
                        inEscape = true;
                        continue;
                    }
                    unescaped.Append(c);
                }

                if (inEscape)
                {
                    unescaped.Append('\\');
                }

                return unescaped.ToString();
            }

            private void TestPath(string path, int patternEnd, bool isLastPathSegment)
            {
                if (!isLastPathSegment)
                {
                    DoGlob(path, patternEnd);
                    return;
                }

                if (!NoEscapes)
                {
                    path = Unescape(path, _stripTwo ? 2 : 0);
                }
                else if (_stripTwo)
                {
                    path = path.Substring(2);
                }

                string resultPath = path;

                if (_relative)//we have to remove CWD before adding to results list
                {
                    resultPath = path.Substring(ScriptContext.CurrentContext.WorkingDirectory.Length + 1);
                }

                if (System.IO.Directory.Exists(path))
                {
                    if (Mark)
                        _result.Add(resultPath + FullPath.DirectorySeparatorString);
                    else
                        _result.Add(resultPath);
                        
                }
                else if (!_dirOnly && File.Exists(path))
                {
                    _result.Add(resultPath);
                }
            }

            internal IList<string>/*!*/ DoGlob()
            {
                if (_pattern.Length == 0)
                {
                    return ArrayUtils.EmptyStrings;
                }

                int pos = 0;
                string baseDirectory = ".";
                if (_pattern[0] == '/' ||  (_pattern.Length >= 2 && _pattern[1] == ':'))//is pattern rooted?
                {
                    bool containsWildcard;
                    pos = FindNextSeparator(0, false, out containsWildcard);
                    if (pos == _pattern.Length)
                    {
                        TestPath(_pattern, pos, true);
                        return _result;
                    }
                    if (pos > 0 || _pattern[0] == '/')
                    {
                        baseDirectory = _pattern.Substring(0, pos);
                    }
                }
                else
                {
                    _relative = true;
                    baseDirectory = CanonizePattern(ScriptContext.CurrentContext.WorkingDirectory);
                }

                _stripTwo = (baseDirectory == ".");

                try
                {
                    DoGlob(baseDirectory, pos);

                }
                catch (ArgumentNullException)
                {
                    throw;
                }
                catch (Exception)
                {
                }

                return _result;
            }

            private void DoGlob(string/*!*/ baseDirectory, int position)
            {
                if (!System.IO.Directory.Exists(baseDirectory))
                {
                    return;
                }

                bool containsWildcard;
                int patternEnd = FindNextSeparator(position, true, out containsWildcard);
                bool isLastPathSegment = (patternEnd == _pattern.Length);
                string dirSegment = _pattern.Substring(position, patternEnd - position);

                if (!isLastPathSegment)
                {
                    patternEnd++;
                }

                if (!containsWildcard)
                {
                    string path = baseDirectory + "/" + dirSegment;
                    TestPath(path, patternEnd, isLastPathSegment);
                    return;
                }

                try
                {

                    foreach (string file in FileSystemUtils.GetFileSystemEntries(baseDirectory, "*"))
                    {
                        string objectName = Path.GetFileName(file);
                        if (FnMatch(dirSegment, objectName, _fnMatchFlags))
                        {
                            TestPath(CanonizePattern(file), patternEnd, isLastPathSegment);
                        }
                    }

                }
                catch (ArgumentNullException)
                {
                    throw;
                }
                catch(Exception)
                {
                    if (StopOnError)
                        throw;
                }

                if (isLastPathSegment && dirSegment[0] == '.')
                {
                    if (FnMatch(dirSegment, ".", _fnMatchFlags))
                    {
                        string directory = baseDirectory + "/.";
                        if (_dirOnly)
                        {
                            directory += '/';
                        }
                        TestPath(directory, patternEnd, true);
                    }
                    if (FnMatch(dirSegment, "..", _fnMatchFlags))
                    {
                        string directory = baseDirectory + "/..";
                        if (_dirOnly)
                        {
                            directory += '/';
                        }
                        TestPath(directory, patternEnd, true);
                    }
                }

            }

            private int FindNextSeparator(int position, bool allowWildcard, out bool containsWildcard)
            {
                int lastSlash = -1;
                bool inEscape = false;
                containsWildcard = false;
                for (int i = position; i < _pattern.Length; i++)
                {
                    if (inEscape)
                    {
                        inEscape = false;
                        continue;
                    }
                    char c = _pattern[i];
                    if (c == '\\')
                    {
                        inEscape = true;
                        continue;
                    }
                    else if (c == '*' || c == '?' || c == '[')
                    {
                        if (!allowWildcard)
                        {
                            return lastSlash + 1;
                        }
                        else if (lastSlash >= 0)
                        {
                            return lastSlash;
                        }
                        containsWildcard = true;
                    }
                    else if (c == '/' || c == ':')
                    {
                        if (containsWildcard)
                        {
                            return i;
                        }
                        lastSlash = i;
                    }
                }
                return _pattern.Length;
            }


        }

        private class GlobUngrouper
        {
            internal abstract class GlobNode
            {
                internal readonly GlobNode/*!*/ _parent;
                protected GlobNode(GlobNode parentNode)
                {
                    _parent = parentNode ?? this;
                }
                abstract internal GlobNode/*!*/ AddChar(char c);
                abstract internal GlobNode/*!*/ StartLevel();
                abstract internal GlobNode/*!*/ AddGroup();
                abstract internal GlobNode/*!*/ FinishLevel();
                abstract internal List<StringBuilder>/*!*/ Flatten();
            }

            internal class TextNode : GlobNode
            {
                private readonly StringBuilder/*!*/ _builder;

                internal TextNode(GlobNode/*!*/ parentNode)
                    : base(parentNode)
                {
                    _builder = new StringBuilder();
                }
                internal override GlobNode/*!*/ AddChar(char c)
                {
                    if (c != 0)
                    {
                        _builder.Append(c);
                    }
                    return this;
                }
                internal override GlobNode/*!*/ StartLevel()
                {
                    return _parent.StartLevel();
                }
                internal override GlobNode/*!*/ AddGroup()
                {
                    return _parent.AddGroup();
                }
                internal override GlobNode/*!*/ FinishLevel()
                {
                    return _parent.FinishLevel();
                }
                internal override List<StringBuilder>/*!*/ Flatten()
                {
                    List<StringBuilder> result = new List<StringBuilder>(1);
                    result.Add(_builder);
                    return result;
                }
            }

            internal class ChoiceNode : GlobNode
            {
                private readonly List<SequenceNode>/*!*/ _nodes;

                internal ChoiceNode(GlobNode/*!*/ parentNode)
                    : base(parentNode)
                {
                    _nodes = new List<SequenceNode>();
                }
                internal override GlobNode/*!*/ AddChar(char c)
                {
                    SequenceNode node = new SequenceNode(this);
                    _nodes.Add(node);
                    return node.AddChar(c);
                }
                internal override GlobNode/*!*/ StartLevel()
                {
                    SequenceNode node = new SequenceNode(this);
                    _nodes.Add(node);
                    return node.StartLevel();
                }
                internal override GlobNode/*!*/ AddGroup()
                {
                    AddChar('\0');
                    return this;
                }
                internal override GlobNode/*!*/ FinishLevel()
                {
                    AddChar('\0');
                    return _parent;
                }
                internal override List<StringBuilder>/*!*/ Flatten()
                {
                    List<StringBuilder> result = new List<StringBuilder>();
                    foreach (GlobNode node in _nodes)
                    {
                        foreach (StringBuilder builder in node.Flatten())
                        {
                            result.Add(builder);
                        }
                    }
                    return result;
                }
            }

            internal class SequenceNode : GlobNode
            {
                private readonly List<GlobNode>/*!*/ _nodes;

                internal SequenceNode(GlobNode parentNode)
                    : base(parentNode)
                {
                    _nodes = new List<GlobNode>();
                }

                internal override GlobNode/*!*/ AddChar(char c)
                {
                    TextNode node = new TextNode(this);
                    _nodes.Add(node);
                    return node.AddChar(c);
                }

                internal override GlobNode/*!*/ StartLevel()
                {
                    ChoiceNode node = new ChoiceNode(this);
                    _nodes.Add(node);
                    return node;
                }

                internal override GlobNode/*!*/ AddGroup()
                {
                    return _parent;
                }

                internal override GlobNode/*!*/ FinishLevel()
                {
                    return _parent._parent;
                }

                internal override List<StringBuilder>/*!*/ Flatten()
                {
                    List<StringBuilder> result = new List<StringBuilder>();
                    result.Add(new StringBuilder());
                    foreach (GlobNode node in _nodes)
                    {
                        List<StringBuilder> tmp = new List<StringBuilder>();
                        foreach (StringBuilder builder in node.Flatten())
                        {
                            foreach (StringBuilder sb in result)
                            {
                                StringBuilder newsb = new StringBuilder(sb.ToString());
                                newsb.Append(builder.ToString());
                                tmp.Add(newsb);
                            }
                        }
                        result = tmp;
                    }
                    return result;
                }
            }

            private readonly SequenceNode/*!*/ _rootNode;
            private GlobNode/*!*/ _currentNode;
            private int _level;

            internal GlobUngrouper(int patternLength)
            {
                _rootNode = new SequenceNode(null);
                _currentNode = _rootNode;
                _level = 0;
            }

            internal void AddChar(char c)
            {
                _currentNode = _currentNode.AddChar(c);
            }

            internal void StartLevel()
            {
                _currentNode = _currentNode.StartLevel();
                _level++;
            }

            internal void AddGroup()
            {
                _currentNode = _currentNode.AddGroup();
            }

            internal void FinishLevel()
            {
                _currentNode = _currentNode.FinishLevel();
                _level--;
            }
            internal int Level
            {
                get { return _level; }
            }
            internal string[]/*!*/ Flatten()
            {
                if (_level != 0)
                {
                    return ArrayUtils.EmptyStrings;
                }
                List<StringBuilder> list = _rootNode.Flatten();
                string[] result = new string[list.Count];
                for (int i = 0; i < list.Count; i++)
                {
                    result[i] = list[i].ToString();
                }
                return result;
            }
        }

        private static string[] UngroupGlobs(string/*!*/ pattern, bool noEscape, bool brace)
        {
            GlobUngrouper ungrouper = new GlobUngrouper(pattern.Length);

            bool inEscape = false;
            foreach (char c in pattern)
            {
                if (inEscape)
                {
                    if (c != ',' && c != '{' && c != '}')
                    {
                        ungrouper.AddChar('\\');
                    }
                    ungrouper.AddChar(c);
                    inEscape = false;
                    continue;
                }
                else if (c == '\\' && !noEscape)
                {
                    inEscape = true;
                    continue;
                }

                switch (c)
                {
                    case '{':
                        if (!brace)
                            return ArrayUtils.EmptyStrings;

                        ungrouper.StartLevel();
                        break;

                    case ',':
                        if (ungrouper.Level < 1)
                        {
                            ungrouper.AddChar(c);
                        }
                        else
                        {
                            ungrouper.AddGroup();
                        }
                        break;

                    case '}':
                        if (ungrouper.Level < 1)
                        {
                            // Unbalanced closing bracket matches nothing
                            return ArrayUtils.EmptyStrings;
                        }
                        ungrouper.FinishLevel();
                        break;

                    default:
                        ungrouper.AddChar(c);
                        break;
                }
            }
            return ungrouper.Flatten();
        }

        internal static IEnumerable<string>/*!*/ GetMatches(string/*!*/ pattern, GlobOptions flags) 
        {
            if (pattern.Length == 0) {
                yield break;
            }

            bool noEscape = ((flags & GlobOptions.NoEscape) != 0);
            bool brace = ((flags & GlobOptions.Brace) != 0);

            string[] groups = UngroupGlobs(pattern, noEscape, brace);
            if (groups.Length == 0) {
                yield break;
            }

            foreach (string group in groups) {
                GlobMatcher matcher = new GlobMatcher(group, flags);
                foreach (string filename in matcher.DoGlob()) {                 
                    yield return filename.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
                }
            }            
        }

        #region fnmatch, glob

        /// <summary>
        /// Matches the given path against a pattern.
        /// </summary>
        /// <param name="pattern">A <see cref="string"/> containing a wildcard.</param>
        /// <param name="path">The <see cref="string"/> to be matched.</param>
        /// <param name="flags">Additional flags.</param>
        /// <returns><c>true</c> if the <paramref name="path"/> matches with the given 
        /// wildcard <paramref name="pattern"/>.</returns>
        [ImplementsFunction("fnmatch")]
        public static bool FnMatch(string/*!*/ pattern, string/*!*/ path, FnMatchOptions flags)
        {
            if (pattern.Length == 0)
            {
                return path.Length == 0;
            }

            bool pathName = ((flags & FnMatchOptions.PathName) != 0);
            bool noEscape = ((flags & FnMatchOptions.NoEscape) != 0);
            string regexPattern = PatternToRegex(pattern, pathName, noEscape);
            if (regexPattern.Length == 0)
            {
                return false;
            }

            if (((flags & FnMatchOptions.Period) == 0) && path.Length > 0 && path[0] == '.')
            {
                // Starting dot requires an explicit dot in the pattern
                if (regexPattern.Length < 4 || regexPattern[2] != '[' || regexPattern[3] != '.')
                {
                    return false;
                }
            }

            RegexOptions options = RegexOptions.None;
            if ((flags & FnMatchOptions.CaseFold) != 0)
            {
                options |= RegexOptions.IgnoreCase;
            }
            Match match = Regex.Match(path, regexPattern, options);
            return match != null && match.Success && (match.Length == path.Length);
        }


        /// <summary>
        /// Matches the given path against a pattern.
        /// </summary>
        /// <param name="pattern">A <see cref="string"/> containing a wildcard.</param>
        /// <param name="path">The <see cref="string"/> to be matched.</param>
        /// <returns><c>true</c> if the <paramref name="path"/> matches with the given 
        /// wildcard <paramref name="pattern"/>.</returns>
        [ImplementsFunction("fnmatch")]
        public static bool Match(string pattern, string path)
        {
            return FnMatch(pattern, path, 0);
        }

        /// <summary>
        /// Find pathnames matching a pattern.
        /// </summary>
        [ImplementsFunction("glob")]
        public static PhpArray Glob(string pattern)
        {
            return Glob(pattern, GlobOptions.None);
        }

        /// <summary>
        /// Find pathnames matching a pattern.
        /// </summary>
        [ImplementsFunction("glob")]
        public static PhpArray Glob(string pattern, GlobOptions flags)
        {
            if (pattern == null)
				return new PhpArray(0, 0);
            
            PhpArray result = new PhpArray();
            foreach (var fileName in GetMatches(pattern, flags))
            {
                result.Add(fileName);
            }

			if (result.Count == 0 && (flags & GlobOptions.NoCheck) != 0) 
                result.Add(pattern);

            return result;
        }

        #endregion

    }
}
