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

    #region CustomAnnotations

    /// <summary>
	/// Represents a set of custom annotations of the <see cref="LangElement" />.
	/// Each annotation is identified by a CLR Type.
	/// </summary>
	public class CustomAnnotations
	{
		private Dictionary<Type, object> annotations = new Dictionary<Type, object>();

		internal CustomAnnotations()
		{
		}

		/// <summary>
		/// Gets an custom annotation.
		/// </summary>
		/// <typeparam name="T">Type identifying the annotation.</typeparam>
		/// <returns>An annotation object. If there is no such annotation, default value of <typeparamref name="T"/> is returned.</returns>
        /// <remarks>The behaviour of this function is different than Get() of collections in .NET. An exceptionm is not thrown if the element is not found, but the default value of T is returned.</remarks>
		public T Get<T>()
		{
			T result;
			TryGet<T>(out result);
			return result;
		}

		/// <summary>
		/// Tries to get an custom annotation.
		/// </summary>
		/// <typeparam name="T">Type identifying the annotation.</typeparam>
		/// <param name="annotation">An annotation object is stored here. If there is no such annotation, default value of <typeparamref name="T"/> is stored.</param>
		/// <returns>True if there was annotation for given type, otherwise false.</returns>
		public bool TryGet<T>(out T annotation)
		{
            object value;
            if (annotations.TryGetValue(typeof(T), out value))
            {
                annotation = (T)value;
                return true;
            }
            else
            {
                annotation = default(T);
                return false;            
            }
		}

		/// <summary>
		/// Sets an custom annotation value.
		/// </summary>
		/// <typeparam name="T">Type identifying the annotation.</typeparam>
		/// <param name="value">New value of a custom annotation.</param>
		public void Set<T>(T value)
		{
            annotations[typeof(T)] = value;
		}

		/// <summary>
		/// Removes an custom annotation binding.
		/// </summary>
		/// <typeparam name="T">Type identifying the annotation.</typeparam>
		public void Remove<T>()
		{
			annotations.Remove(typeof(T));
		}
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
		/// Provides custom annotations which can be changed during the compilation. 
		/// </summary>
        public CustomAnnotations Annotations
        {
            get
            {
                return annotations ?? (annotations = new CustomAnnotations());
            }
        }
		private CustomAnnotations annotations = null;

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

}