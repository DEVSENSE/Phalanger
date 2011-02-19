using System;
using System.Collections.Generic;
using System.Text;
using PHP.Core.AST;
using System.IO;

namespace PHP.Core.EmbeddedDoc
{
	/// <summary>
	/// Resolves documentation from commentaries in the AST tree.
	/// </summary>
	public class DocResolver : TreeVisitor
	{
        /// <summary>
        /// Parses the comments around the element. Set new DocBlock annotation.
        /// </summary>
        /// <param name="element">Element to be parsed.</param>
        /// <returns>DocBlock of the element.</returns>
        public static DocBlock ParseSubtree(LangElement element)
        {
            (new DocResolver()).VisitElement(element);
            return element.Annotations.Get<DocBlock>();
        }

		private IEnumerable<Tuple<DocElementType, DocElement>> ParseDocumentation(LangElement element)
		{
			CommentSet cs;

			// if there is no commentary annotation present, exit
			if (!element.Annotations.TryGet<CommentSet>(out cs))
				return null;

			// if there are no preceeding comments, exit
			if (cs.Preceeding.Count == 0)
				return null;

			// if the last preceeding comment is not doc comment, exit
            Comment lastcs = cs.Preceeding[cs.Preceeding.Count - 1];
            if (lastcs.Type != CommentType.Documentation)
				return null;

            //
            // parse the comment to resolve a collection of DocElement
            //
			Scanner scanner = new Scanner(new StringReader(lastcs.Content ?? string.Empty));

            Parser parser = new Parser() { Scanner = scanner };
			parser.Parse();

			//of there was an error during parsing, null is returned here
			return parser.Elements;
		}

		public override void VisitFunctionDecl(FunctionDecl x)
		{
			//base.VisitFunctionDecl(x);

			var list = ParseDocumentation(x);

			if (list != null)
			{
				x.Annotations.Set<DocBlock>(new DocFunctionBlock(list));
			}
		}

		public override void VisitMethodDecl(MethodDecl x)
		{
			//base.VisitMethodDecl(x);

			var list = ParseDocumentation(x);

			if (list != null)
			{
				x.Annotations.Set<DocBlock>(new DocMethodBlock(list));
			}
		}

		public override void VisitTypeDecl(TypeDecl x)
		{
            base.VisitTypeDecl(x);
            
            var list = ParseDocumentation(x);

			if (list != null)
			{
				x.Annotations.Set<DocBlock>(new DocClassBlock(list));
			}
        }

		public override void VisitFieldDeclList(FieldDeclList x)
		{
			//base.VisitFieldDeclList(x);

			var list = ParseDocumentation(x);

			if (list != null)
			{
				x.Annotations.Set<DocBlock>(new DocVarBlock(list));
			}
		}

		public override void VisitConstDeclList(ConstDeclList x)
		{
			//base.VisitConstDeclList(x);
		}
	}
}
