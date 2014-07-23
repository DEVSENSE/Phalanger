/*

 Copyright (c) 2007- DEVSENSE
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
using PHP.Core;
using System;

namespace PHP.Core.AST
{
    /// <summary>
    /// Base class for all AST nodes.
    /// </summary>
    [Serializable]
    public abstract class AstNode : IPropertyCollection
    {
        #region Fields & Properties

        /// <summary>
        /// Contains properties of this <see cref="AstNode"/>.
        /// </summary>
        private PropertyCollection _properties;

        /// <summary>
        /// Gets property collection associated with this node.
        /// </summary>
        public IPropertyCollection Properties { get { return (IPropertyCollection)this; } }

        #endregion

        #region IPropertyCollection

        void IPropertyCollection.SetProperty(object key, object value)
        {
            _properties.SetProperty(key, value);
        }

        object IPropertyCollection.GetProperty(object key)
        {
            return _properties.GetProperty(key);
        }

        public void SetProperty<T>(T value)
        {
            _properties.SetProperty<T>(value);
        }

        public T GetProperty<T>()
        {
            return _properties.GetProperty<T>();
        }

        bool IPropertyCollection.RemoveProperty(object key)
        {
            return _properties.RemoveProperty(key);
        }

        bool IPropertyCollection.RemoveProperty<T>()
        {
            return _properties.RemoveProperty<T>();
        }

        void IPropertyCollection.ClearProperties()
        {
            _properties.ClearProperties();
        }

        object IPropertyCollection.this[object key]
        {
            get
            {
                return _properties.GetProperty(key);
            }
            set
            {
                _properties.SetProperty(key, value);
            }
        }

        #endregion
    }

    /// <summary>
	/// Base class for all AST nodes representing PHP language Elements - statements and expressions.
	/// </summary>
    [Serializable]
	public abstract class LangElement : AstNode
	{
		/// <summary>
		/// Position of element in source file.
		/// </summary>
        public Text.Span Span { get; protected set; }
		
		/// <summary>
        /// Initialize the LangElement.
        /// </summary>
        /// <param name="span">The position of the LangElement in the source code.</param>
		protected LangElement(Text.Span span)
		{
			this.Span = span;
		}

        /// <summary>
        /// In derived classes, calls Visit* on the given visitor object.
        /// </summary>
        /// <param name="visitor">Visitor.</param>
        public abstract void VisitMe(TreeVisitor/*!*/visitor);
	}

    #region Scope
    
    [Serializable]
    public struct Scope
    {
        public int Start { get { return start; } }
        private int start;

        public static readonly Scope Invalid = new Scope(-1);
        public static readonly Scope Global = new Scope(0);
        public static readonly Scope Ignore = new Scope(Int32.MaxValue);

        public bool IsGlobal
        {
            get
            {
                return start == 0;
            }
        }

        public bool IsValid
        {
            get
            {
                return start >= 0;
            }
        }

        public Scope(int start)
        {
            this.start = start;
        }

        public void Increment()
        {
            start++;
        }

        public override string ToString()
        {
            return start.ToString();
        }
    }

    #endregion
}