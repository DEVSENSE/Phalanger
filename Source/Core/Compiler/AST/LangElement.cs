/*

 Copyright (c) 2004-2006 Tomas Matousek, Ladislav Prosek, and Vaclav Novak.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/
using System.Diagnostics;
using System.Collections.Generic;
using PHP.Core.Parsers;
using PHP.Core.AST;
using System;

namespace PHP.Core.AST
{
    #region AstNode

    //public enum NodeIds
    //{
    //}

	public abstract class AstNode
	{
		// public abstract NodeIds NodeId { get; }
    }

    #endregion

    /// <summary>
	/// Base class for all AST nodes representing PHP language Elements - statements and expressions.
	/// </summary>
	public abstract class LangElement : AstNode
	{
		/// <summary>
		/// Position of element in source file.
		/// </summary>
        public Position Position
        {
            get
            {
                return position;
            }
        }
		protected Position position;
		
		/// <summary>
        /// Initialize the LangElement.
        /// </summary>
        /// <param name="p">The position of the LangElement in the source code.</param>
		protected LangElement(Position p)
		{
			position = p;
		}

        /// <summary>
        /// In derived classes, calls Visit* on the given visitor object.
        /// </summary>
        /// <param name="visitor">Visitor.</param>
        public abstract void VisitMe(TreeVisitor/*!*/ visitor);
	}

    /// <summary>
    /// Interface for elements that can hold an instance of <see cref="PHPDocBlock"/>.
    /// </summary>
    public interface IHasPhpDoc
    {
        /// <summary>
        /// Associated <see cref="PHPDocBlock"/> instance or <c>null</c> reference if the element has no PHPDoc block.
        /// </summary>
        PHPDocBlock PHPDoc { get; set; }
    }

}