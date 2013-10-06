using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using PHP.Core.Parsers;

namespace PHP.Core
{
    #region SourceUnit

    public abstract class SourceUnit
    {
        /// <summary>
        /// Source file containing the unit. For evals, it can be even a non-php source file.
        /// Used for emitting debug information and error reporting.
        /// </summary>
        public PhpSourceFile/*!*/ SourceFile { get { return sourceFile; } }
        protected readonly PhpSourceFile/*!*/ sourceFile;

        public AST.GlobalCode Ast { get { return ast; } }
        protected AST.GlobalCode ast;

        /// <summary>
        /// Dictionary of PHP aliases.
        /// </summary>
        public Dictionary<string, QualifiedName>/*!*/ Aliases { get { return aliases; } }
        private readonly Dictionary<string, QualifiedName>/*!*/ aliases = new Dictionary<string, QualifiedName>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Current namespace (in case we are compiling through eval from within namespace).
        /// </summary>
        public QualifiedName? CurrentNamespace { get { return currentNamespace; } }
        private QualifiedName? currentNamespace = null;

        public List<QualifiedName>/*!*/ImportedNamespaces { get { return importedNamespaces; } }
        private readonly List<QualifiedName>/*!*/importedNamespaces = new List<QualifiedName>();
        public bool HasImportedNamespaces { get { return this.importedNamespaces != null && this.importedNamespaces.Count != 0; } }

        /// <summary>
        /// Encoding of the file or the containing file.
        /// </summary>
        public Encoding/*!*/ Encoding { get { return encoding; } }
        protected readonly Encoding/*!*/ encoding;

        /// <summary>
        /// Gets value indicating whether we are in pure mode.
        /// </summary>
        public virtual bool IsPure { get { return false; } }

        /// <summary>
        /// Gets value indicating whether we are processing transient unit.
        /// </summary>
        public virtual bool IsTransient { get { return false; } }

        #region Construction

        public SourceUnit(PhpSourceFile/*!*/ sourceFile, Encoding/*!*/ encoding)
        {
            Debug.Assert(sourceFile != null && encoding != null);

            this.sourceFile = sourceFile;
            this.encoding = encoding;
        }

        #endregion

        /// <summary>
        /// Gets a piece of source code.
        /// </summary>
        /// <param name="position">Position of the piece to get.</param>
        /// <returns>Source code.</returns>
        public abstract string GetSourceCode(Position position);

        public abstract void Parse(ErrorSink/*!*/ errors, IReductionsSink/*!*/ reductionsSink,
            Parsers.Position initialPosition, LanguageFeatures features);

        public abstract void Close();

        #region Source Position Mapping (#pragma line/file)

        public const int DefaultLine = Int32.MinValue;
        public const string DefaultFile = null;

        private List<int> mappedPathsAnchors;
        private List<string> mappedPaths;
        private List<int> mappedLinesAnchors;
        private List<int> mappedLines;

        internal void AddSourceFileMapping(int realLine, string mappedFullPath)
        {
            if (mappedPathsAnchors == null)
            {
                mappedPathsAnchors = new List<int>();
                mappedPaths = new List<string>();
            }

            mappedPathsAnchors.Add(realLine);
            mappedPaths.Add(mappedFullPath);
        }

        internal void AddSourceLineMapping(int realLine, int mappedLine)
        {
            if (mappedLinesAnchors == null)
            {
                mappedLinesAnchors = new List<int>();
                mappedLines = new List<int>();
            }

            mappedLinesAnchors.Add(realLine);
            mappedLines.Add(mappedLine);
        }

        public string/*!*/ GetMappedFullSourcePath(int realLine)
        {
            if (mappedPathsAnchors == null) return sourceFile.FullPath;
            Debug.Assert(mappedPaths != null);

            int index = mappedPathsAnchors.BinarySearch(realLine);

            // the line containing the pragma:
            string result;
            if (index >= 0)
            {
                result = mappedPaths[index];
            }
            else
            {
                index = ~index - 1;
                result = (index < 0) ? sourceFile.FullPath : mappedPaths[index];
            }

            return (result != DefaultFile) ? result : sourceFile.FullPath;
        }

        public int GetMappedLine(int realLine)
        {
            if (mappedLinesAnchors == null) return realLine;
            Debug.Assert(mappedLines != null);

            int index = mappedLinesAnchors.BinarySearch(realLine);

            // the line containing the pragma:
            if (index >= 0)
                return (mappedLines[index] != 0) ? mappedLines[index] : realLine;

            index = ~index - 1;

            return (index < 0 || mappedLines[index] == DefaultLine) ? realLine :
                mappedLines[index] + realLine - mappedLinesAnchors[index];
        }

        #endregion

        /// <summary>
        /// Used to merge namespaces included by the caller of 'eval' function.
        /// </summary>
        /// <param name="namingContext">Naming context of the caller</param>
        public void AddImportedNamespaces(NamingContext namingContext)
        {
            if (namingContext == null) return;

            this.currentNamespace = namingContext.CurrentNamespace;
            if (namingContext.Aliases != null)
                foreach (var alias in namingContext.Aliases)
                    this.Aliases.Add(alias.Key, alias.Value);
        }
    }

    #endregion
}
