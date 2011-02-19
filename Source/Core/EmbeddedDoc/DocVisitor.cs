using System;
using System.Collections.Generic;
using System.Text;

namespace PHP.Core.EmbeddedDoc
{
	/// <summary>
	/// Supports visiting of the embedded documentation structure.
	/// </summary>
	public interface IDocVisitor
	{
		/// <summary>
		/// Informs the visitor that it should visit specified documentation block.
		/// </summary>
		/// <param name="block">A documentation block to be visited.</param>
		void VisitBlock(DocBlock block);

		/// <summary>
		/// Informs the visitor that it should visit specified documentation expression.
		/// </summary>
		/// <param name="expr">A documentation expression to be visited.</param>
		void VisitExpression(DocExpression expr);

		/// <summary>
		/// Informs the visitor that it should visit specified documentation element.
		/// </summary>
		/// <param name="element">A documentation expression to be visited.</param>
		void VisitElement(DocElement element);

		/// <summary>
		/// Informs the visitor that it should visit specified documentation attribute.
		/// </summary>
		/// <param name="attr">A documentation attribute to be visited.</param>
		void VisitAttribute(DocAttribute attr);
	}

	/// <summary>
	/// Analyzes documentation blocks.
	/// </summary>
	public class DocAnalyzer : IDocVisitor
	{
		public void VisitBlock(DocBlock block)
		{
		}

		public void VisitExpression(DocExpression expr)
		{
		}

		public void VisitElement(DocElement element)
		{
		}

		public void VisitAttribute(DocAttribute attribute)
		{
		}
	}

	/// <summary>
	/// Exports previously analyzed documentation blocks into XML form.
	/// </summary>
	public class DocExporter : IDocVisitor
	{
		public void VisitBlock(DocBlock block)
		{
		}

		public void VisitExpression(DocExpression expr)
		{
		}

		public void VisitElement(DocElement element)
		{
		}

		public void VisitAttribute(DocAttribute attribute)
		{
		}
	}
}
