/*

 Copyright (c) 2006 Tomas Matousek.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Collections;

using PHP.Core.Parsers;

namespace PHP.Core.AST
{
	#region ActualParam

    [Serializable]
	public sealed class ActualParam : LangElement
	{
		public Expression/*!*/ Expression { get { return expression; } }
		internal Expression/*!*/ expression;

        /// <summary>
        /// Gets value indicating whether the parameter is prefixed by <c>&amp;</c> character.
        /// </summary>
        public bool Ampersand { get { return ampersand; } }
		private bool ampersand;

		public ActualParam(Text.Span p, Expression param, bool ampersand)
			: base(p)
		{
			Debug.Assert(param != null);
			this.expression = param;
			this.ampersand = ampersand;
		}

		/// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitActualParam(this);
        }
	}

	#endregion

	#region NamedActualParam

    [Serializable]
    public sealed class NamedActualParam : LangElement
	{
		public Expression/*!*/ Expression { get { return expression; } }
		internal Expression/*!*/ expression;

		public VariableName Name { get { return name; } }
		private VariableName name;

        public NamedActualParam(Text.Span span, string name, Expression/*!*/ expression)
            : base(span)
        {
            this.name = new VariableName(name);
            this.expression = expression;
        }

		/// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitNamedActualParam(this);
        }
	}

	#endregion

	#region CallSignature

    [Serializable]
    public sealed class CallSignature : AstNode
	{
		/// <summary>
		/// List of actual parameters (<see cref="ActualParam"/> nodes).
		/// </summary>	
		public ActualParam[]/*!*/ Parameters { get { return parameters; } }
		private readonly ActualParam[]/*!*/ parameters;

		/// <summary>
		/// List of generic parameters.
		/// </summary>
        public TypeRef[]/*!*/ GenericParams
        {
            get { return this.GetProperty<TypeRef[]>() ?? EmptyArray<TypeRef>.Instance; }
            set
            {
                if (value.Any())
                    this.SetProperty<TypeRef[]>(value);
                else
                    this.Properties.RemoveProperty<TypeRef[]>();
            }
        }

        /// <summary>
        /// Initialize new instance of <see cref="CallSignature"/>.
        /// </summary>
        /// <param name="parameters">List of parameters.</param>
        public CallSignature(IList<ActualParam> parameters)
            : this(parameters, null)
        {
        }
        
        /// <summary>
        /// Initialize new instance of <see cref="CallSignature"/>.
        /// </summary>
        /// <param name="parameters">List of parameters.</param>
        /// <param name="genericParams">List of type parameters for generics.</param>
        public CallSignature(IList<ActualParam> parameters, IList<TypeRef> genericParams)
		{
			this.parameters = parameters.AsArray();
            this.GenericParams = genericParams.AsArray();
		}        
    }

	#endregion
}
