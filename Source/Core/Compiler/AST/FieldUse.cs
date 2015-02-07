/*

 Copyright (c) 2004-2006 Tomas Matousek, Ladislav Prosek, Vaclav Novak and Martin Maly.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.IO;
using System.Diagnostics;
using System.Reflection.Emit;
using PHP.Core.Emit;
using PHP.Core.Parsers;
using PHP.Core.Reflection;

namespace PHP.Core.AST
{
    #region StaticFieldUse

    /// <summary>
    /// Base class for static field uses.
    /// </summary>
    [Serializable]
    public abstract class StaticFieldUse : VariableUse
    {
        /// <summary>Name of type which's field is being accessed</summary>
        public GenericQualifiedName TypeName { get { return typeRef.GenericQualifiedName; } }

        /// <summary>Position of <see cref="TypeName"/>.</summary>
        public Position TypeNamePosition { get { return this.typeRef.Position; } }

        /// <summary>Position of the field name.</summary>
        public Position NamePosition { get; private set; }

        internal TypeRef TypeRef { get { return typeRef; } }
        protected TypeRef typeRef;

        public StaticFieldUse(Position position, Position namePosition, GenericQualifiedName typeName, Position typeNamePosition)
            : this(position, namePosition, DirectTypeRef.FromGenericQualifiedName(typeNamePosition, typeName))
        {
        }

        public StaticFieldUse(Position position, Position namePosition, TypeRef typeRef)
            : base(position)
        {
            Debug.Assert(typeRef != null);

            this.typeRef = typeRef;
            this.NamePosition = namePosition;
        }
    }

    #endregion

    #region DirectStFldUse

    /// <summary>
    /// Direct static field uses (a static field accessed by field identifier).
    /// </summary>
    [Serializable]
    public sealed class DirectStFldUse : StaticFieldUse
    {
        public override Operations Operation { get { return Operations.DirectStaticFieldUse; } }

        private VariableName propertyName;
        /// <summary>Name of static field beign accessed</summary>
        public VariableName PropertyName { get { return propertyName; } }

        public DirectStFldUse(Position position, TypeRef typeRef, VariableName propertyName, Position propertyNamePosition)
            : base(position, propertyNamePosition, typeRef)
        {
            this.propertyName = propertyName;
        }

        public DirectStFldUse(Position position, GenericQualifiedName qualifiedName, Position qualifiedNamePosition, VariableName propertyName, Position propertyNamePosition)
            : this(position, DirectTypeRef.FromGenericQualifiedName(qualifiedNamePosition, qualifiedName), propertyName, propertyNamePosition)
        {
        }

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitDirectStFldUse(this);
        }
    }

    #endregion

    #region IndirectStFldUse

    /// <summary>
    /// Indirect static field used - a static field accessed by run-time evaluated name.
    /// </summary>
    [Serializable]
    public sealed class IndirectStFldUse : StaticFieldUse
    {
        public override Operations Operation { get { return Operations.IndirectStaticFieldUse; } }

        /// <summary>Expression that produces name of the field</summary>
        public Expression/*!*/ FieldNameExpr { get { return fieldNameExpr; } internal set { fieldNameExpr = value; } }
        private Expression/*!*/ fieldNameExpr;
        
        public IndirectStFldUse(Position position, TypeRef typeRef, Expression/*!*/ fieldNameExpr)
            : base(position, fieldNameExpr.Position, typeRef)
        {
            this.fieldNameExpr = fieldNameExpr;
        }

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitIndirectStFldUse(this);
        }
    }

    #endregion
}
