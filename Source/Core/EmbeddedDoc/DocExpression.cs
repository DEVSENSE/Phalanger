using System;
using System.Collections.Generic;
using System.Text;

namespace PHP.Core.EmbeddedDoc
{
	#region DocExpression	
	/// <summary>
	/// Represents one part of description attribute in the embedded documentation, which are in 
	/// the following form: {@tag arg1 arg2}. Expressions are usually texts, examples, links etc.
	/// </summary>
	/// <remarks>
	/// Description attributes are always the last attribute of an documentation element. They are
	/// used for example in @param or @method elements.
	/// </remarks>
	public abstract class DocExpression
	{
		public virtual void Visit(IDocVisitor visitor)
		{
			visitor.VisitExpression(this);
		}
	}
	#endregion

	#region DocTextExpr
	/// <summary>
	/// An expression representing plain text in embedded documentation.
	/// </summary>
	public class DocTextExpr : DocExpression
	{
		/// <summary>
		/// Text represented by the documentation expression.
		/// </summary>
		public string Text { get { return text; } }
		private string text;

		public DocTextExpr(string text)
		{
			this.text = text;
		}
	}

	/// <summary>
	/// An expression representing inline link tag in embedded documentation.
	/// </summary>
	public class DocLinkExpr : DocExpression
	{
		/// <summary>
		/// URL of the link.
		/// </summary>
		public string URL { get { return url; } }
		private string url;

		/// <summary>
		/// Description of the link.
		/// </summary>
		public string Text { get { return text; } }
		private string text;

		public DocLinkExpr(string url, string description)
		{
			this.url = url;
            this.text = description;
		}
	}

	/// <summary>
	/// An documentation expression representing @example inline tag of the embedded documentation.
	/// </summary>
	public class DocExampleExpr : DocExpression
	{
		/// <summary>
		/// Path to the example file.
		/// </summary>
		public string Path { get { return path; } }
		private string path;

		/// <summary>
		/// Starting line of the example code.
		/// </summary>
		public int StartLine { get { return startLine; } }
		int startLine;

		/// <summary>
		/// Length of the example code in lines.
		/// </summary>
		public int NumberOfLines { get { return numberOfLines; } }
		int numberOfLines;


		/// <summary>
		/// Creates new instance of DocExampleExpr class.
		/// </summary>
		/// <param name="path">Path to the example file.</param>
		/// <param name="startLine">Starting line of the example code.</param>
		/// <param name="numberOfLines">Length of the example code in lines.</param>
		public DocExampleExpr(string path, int startLine, int numberOfLines)
		{
			this.path = path;
			this.startLine = startLine;
			this.numberOfLines = numberOfLines;
		}
	}
	#endregion

	/*
	/// <summary>
	/// An documentation expression representing {@id} inline tag of the embedded documentation.
	/// </summary>
	public class DocIdExpr : DocInline
	{
		string sectionID;
	}

	/// <summary>
	/// Expression representing {@internal}} inline tag in the embedded documentation. This special 
	/// tag is used for specification of a block of expressions which are included only in internal
	/// documentation.
	/// </summary>
	public class DocInternalExpr : DocInline
	{
		DocExpression[] exprs;
	}

	/// <summary>
	/// Expression representing {@inheritdoc} inline tag in the embedded documentation. This tag is
	/// used for inheriting the documentation of the item from the parent class. 
	/// </summary>
	public class DocInheritExpr : DocInline
	{
	}
	*/
}
