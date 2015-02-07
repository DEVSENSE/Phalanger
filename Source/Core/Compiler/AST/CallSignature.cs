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

		public ActualParam(Position p, Expression param, bool ampersand)
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

        public NamedActualParam(Position position, string name, Expression/*!*/ expression)
            : base(position)
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
		public List<ActualParam>/*!*/ Parameters { get { return parameters; } }
		private readonly List<ActualParam>/*!*/ parameters;

		/// <summary>
		/// List of generic parameters.
		/// </summary>
		public List<TypeRef>/*!*/ GenericParams
        {
            get
            {
                return this.Properties[GenericParamsPropertyKey] as List<TypeRef> ?? TypeRef.EmptyList;
            }
            private set
            {
                if (value != null && value.Count > 0)
                    this.Properties[GenericParamsPropertyKey] = value;
                else
                    this.Properties.RemoveProperty(GenericParamsPropertyKey);
            }
        }

        /// <summary>
        /// Key to property collection to get/store generic parameters list.
        /// </summary>
        private const string GenericParamsPropertyKey = "GenericParams";
		
        /// <summary>
        /// Gets true if all the Parameters (after the analysis) have the value and could be evaluated during the compilation time.
        /// </summary>
        public bool AllParamsHaveValue
        {
            get
            {
                foreach (var p in Parameters)
                    if (!p.Expression.HasValue)
                        return false;

                return true;
            }
        }

        /// <summary>
        /// Initialize new instance of <see cref="CallSignature"/>.
        /// </summary>
        /// <param name="parameters">List of parameters.</param>
        /// <param name="genericParams">List of type parameters for generics.</param>
        public CallSignature(List<ActualParam>/*!*/ parameters, List<TypeRef> genericParams)
		{
			Debug.Assert(parameters != null);

			this.parameters = parameters;
            this.GenericParams = genericParams;
		}

        /// <summary>
        /// Builds <see cref="Expression"/> that creates <see cref="PhpArray"/> with call signature parameters.
        /// </summary>
        /// <returns></returns>
        public ArrayEx/*!*/BuildPhpArray()
        {
            Debug.Assert(this.GenericParams == null || this.GenericParams.Count == 0);

            List<Item> arrayItems = new List<Item>(this.parameters.Count);
            Position pos = Position.Invalid;

            foreach (var p in this.parameters)
            {
                arrayItems.Add(new ValueItem(null, p.Expression));
                if (pos.IsValid) pos = p.Position;
                else
                {
                    pos.LastColumn = p.Position.LastColumn;
                    pos.LastLine = p.Position.LastLine;
                    pos.LastOffset = p.Position.LastOffset;
                }
            }

            return new ArrayEx(pos, arrayItems);
        }
    }

	#endregion
}
