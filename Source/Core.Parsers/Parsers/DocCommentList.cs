using PHP.Core.AST;
using PHP.Core.Text;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace PHP.Core.Parsers
{
    /// <summary>
    /// Helper class containing list of DOC comments during tokenization.
    /// Provides searching for DOC comment above given position.
    /// </summary>
    internal class DocCommentList : TreeVisitor
    {
        private struct DocInfo
        {
            /// <summary>
            /// DOC comment instance.
            /// </summary>
            public PHPDocBlock PhpDoc;

            /// <summary>
            /// DOC comment span including following whitespace.
            /// </summary>
            public Span Extent;
        }

        #region Fields & Properties

        /// <summary>
        /// Ordered list of DOC comments. Can be <c>null</c>.
        /// </summary>
        private List<DocInfo> _doclist;

        /// <summary>
        /// Extent of included DOC comments span.
        /// </summary>
        public Span Extent { get { return _span; } }
        private Span _span = Span.Invalid;

        #endregion

        /// <summary>
        /// Inserts DOC block into the list.
        /// </summary>
        public void AppendBlock(PHPDocBlock/*!*/phpDoc, int endPosition)
        {
            Debug.Assert(phpDoc != null);
            Debug.Assert(endPosition >= phpDoc.Span.End);
            Debug.Assert(_doclist == null || _doclist.Count == 0 || _doclist.Last().Extent.Start < phpDoc.Span.Start, "Blocks have to be appended in order.");

            var docinfo = new DocInfo()
            {
                PhpDoc = phpDoc,
                Extent = Span.FromBounds(phpDoc.Span.Start, endPosition)
            };

            var list = _doclist;
            if (list == null)
            {
                _doclist = list = new List<DocInfo>(4);
            }
            
            list.Add(docinfo);

            _span = Span.FromBounds(list[0].Extent.Start, list.Last().Extent.End);
        }

        /// <summary>
        /// Finds DOC comment above given position, removes it from the internal list and returns its reference.
        /// </summary>
        public bool TryReleaseBlock(int position, out PHPDocBlock phpdoc)
        {
            var index = FindIndex(position - 1);
            if (index >= 0)
            {
                var list = _doclist;
                phpdoc = list[index].PhpDoc;
                list.RemoveAt(index);
                _span = (list.Count == 0)
                    ? Span.Invalid
                    : Span.FromBounds(list[0].Extent.Start, list.Last().Extent.End);

                //
                return true;
            }

            //
            phpdoc = null;
            return false;
        }

        /// <summary>
        /// Merges collected DOC comments into the AST.
        /// Annotates declarations, inserts into statement lists where possible.
        /// </summary>
        /// <param name="ast"></param>
        public void Merge(GlobalCode ast)
        {
            if (ast != null && _span.IsValid)
            {
                this.VisitGlobalCode(ast);
            }
        }

        /// <summary>
        /// Gets value indicating whether collected DOC comments may annotate given span.
        /// </summary>
        private bool IntersectsWith(Span span)
        {
            return _span.IntersectsWith(span);
        }

        /// <summary>
        /// Finds index of DOC comment at given position.
        /// </summary>
        private int FindIndex(int position)
        {
            if (_span.Contains(position))
            {
                Debug.Assert(_doclist != null);
                int index = FindIndex(_doclist, position);
                if (index >= 0 && _doclist[index].Extent.Contains(position))
                {
                    return index;
                }
            }

            return -1;
        }

        /// <summary>
        /// Binary search.
        /// </summary>
        private static int FindIndex(List<DocInfo>/*!*/list, int position)
        {
            int a = 0, b = list.Count - 1;
            while (a <= b)
            {
                int x = (a + b) >> 1;
                var doc = list[x];
                if (doc.Extent.Contains(position))
                    return x;

                if (position > doc.Extent.Start)
                    a = x + 1;
                else
                    b = x - 1;
            }

            return -1;
        }

        /// <summary>
        /// Finds DOC comment at given position and annotates statement with it.
        /// </summary>
        private void TryAnnotate(LangElement/*!*/stmt, int start)
        {
            PHPDocBlock phpdoc;
            if (TryReleaseBlock(start, out phpdoc))
            {
                stmt.SetPHPDoc(phpdoc);
            }
        }

        #region TreVisitor Members

        public override void VisitFunctionDecl(FunctionDecl x)
        {
            TryAnnotate(x, x.EntireDeclarationPosition.Start);

            if (IntersectsWith(x.EntireDeclarationPosition))
                base.VisitFunctionDecl(x);
        }
        
        public override void VisitTypeDecl(TypeDecl x)
        {
            TryAnnotate(x, x.EntireDeclarationPosition.Start);
            
            if (IntersectsWith(x.EntireDeclarationPosition))
                base.VisitTypeDecl(x);
        }

        public override void VisitMethodDecl(MethodDecl x)
        {
            TryAnnotate(x, x.EntireDeclarationPosition.Start);

            if (IntersectsWith(x.EntireDeclarationPosition))
                base.VisitMethodDecl(x);
        }

        public override void VisitFieldDeclList(FieldDeclList x)
        {
            TryAnnotate(x, x.Span.Start);
        }

        public override void VisitConstDeclList(ConstDeclList x)
        {
            TryAnnotate(x, x.Span.Start);
        }

        public override void VisitGlobalConstDeclList(GlobalConstDeclList x)
        {
            TryAnnotate(x, x.Span.Start);
        }

        public override void VisitNamespaceDecl(NamespaceDecl x)
        {
            TryAnnotate(x, x.Span.Start);

            if (x.IsSimpleSyntax || IntersectsWith(x.Span))
                base.VisitNamespaceDecl(x);
        }

        #endregion
    }
}
