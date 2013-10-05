/*

 Copyright (c) 2007- DEVSENSE
 Copyright (c) 2004-2006 Tomas Matousek, Ladislav Prosek, Vaclav Novak and Martin Maly.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Reflection;
using System.Diagnostics;
using System.Collections;

using PHP.Core.Parsers;

namespace PHP.Core.AST
{
	#region FunctionCall

    [Serializable]
	public abstract class FunctionCall : VarLikeConstructUse
	{
		protected CallSignature callSignature;
        /// <summary>GetUserEntryPoint calling signature</summary>
        public CallSignature CallSignature { get { return callSignature; } internal set { callSignature = value; } }

		/// <summary>
        /// Position of called function name in source code.
        /// </summary>
        public Position NamePosition { get; protected set; }

		public FunctionCall(Position position, Position namePosition, List<ActualParam>/*!*/ parameters, List<TypeRef>/*!*/ genericParams)
			: base(position)
		{
			Debug.Assert(parameters != null);

			this.callSignature = new CallSignature(parameters, genericParams);
            this.NamePosition = namePosition;
		}
	}

	#endregion

	#region DirectFcnCall

    [Serializable]
    public sealed class DirectFcnCall : FunctionCall
	{
        public override Operations Operation { get { return Operations.DirectCall; } }

        /// <summary>
		/// Simple name for methods.
		/// </summary>
		private QualifiedName qualifiedName;
        private QualifiedName? fallbackQualifiedName;
        /// <summary>Simple name for methods.</summary>
        public QualifiedName QualifiedName { get { return qualifiedName; } }
        public QualifiedName? FallbackQualifiedName { get { return fallbackQualifiedName; } }

        public DirectFcnCall(Position position,
            QualifiedName qualifiedName, QualifiedName? fallbackQualifiedName, Position qualifiedNamePosition,
            List<ActualParam>/*!*/ parameters, List<TypeRef>/*!*/ genericParams)
            : base(position, qualifiedNamePosition, parameters, genericParams)
		{
            this.qualifiedName = qualifiedName;
            this.fallbackQualifiedName = fallbackQualifiedName;
		}

		/// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitDirectFcnCall(this);
        }
	}

	#endregion

	#region IndirectFcnCall

    [Serializable]
    public sealed class IndirectFcnCall : FunctionCall
	{
        public override Operations Operation { get { return Operations.IndirectCall; } }

		public Expression/*!*/ NameExpr { get { return nameExpr; } }
		internal Expression/*!*/ nameExpr;

		public IndirectFcnCall(Position p, Expression/*!*/ nameExpr, List<ActualParam>/*!*/ parameters, List<TypeRef>/*!*/ genericParams)
            : base(p, nameExpr.Position, parameters, genericParams)
		{
			this.nameExpr = nameExpr;
		}

		/// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitIndirectFcnCall(this);
        }
	}

	#endregion

	#region StaticMtdCall

    [Serializable]
	public abstract class StaticMtdCall : FunctionCall
	{
        public GenericQualifiedName ClassName { get { return typeRef.GenericQualifiedName; } }
        protected readonly TypeRef/*!*/typeRef;

        /// <summary>
        /// Position of <see cref="ClassName"/> in source code.
        /// </summary>
        public Position ClassNamePosition { get { return this.typeRef.Position; } }

        public TypeRef/*!*/ TypeRef { get { return this.typeRef; } }

        public StaticMtdCall(Position position, Position methodNamePosition, GenericQualifiedName className, Position classNamePosition, List<ActualParam>/*!*/ parameters, List<TypeRef>/*!*/ genericParams)
            : this(position, methodNamePosition, DirectTypeRef.FromGenericQualifiedName(classNamePosition, className), parameters, genericParams)
		{	
		}

        public StaticMtdCall(Position position, Position methodNamePosition, TypeRef typeRef, List<ActualParam>/*!*/ parameters, List<TypeRef>/*!*/ genericParams)
            : base(position, methodNamePosition, parameters, genericParams)
        {
            Debug.Assert(typeRef != null);

            this.typeRef = typeRef;
        }
	}

	#endregion

	#region DirectStMtdCall

    [Serializable]
	public sealed class DirectStMtdCall : StaticMtdCall
	{
        public override Operations Operation { get { return Operations.DirectStaticCall; } }

		private Name methodName;
        public Name MethodName { get { return methodName; } }
		
		public DirectStMtdCall(Position position, ClassConstUse/*!*/ classConstant,
            List<ActualParam>/*!*/ parameters, List<TypeRef>/*!*/ genericParams)
			: base(position, classConstant.NamePosition, classConstant.TypeRef, parameters, genericParams)
		{
			this.methodName = new Name(classConstant.Name.Value);
		}

		public DirectStMtdCall(Position position, GenericQualifiedName className, Position classNamePosition,
            Name methodName, Position methodNamePosition, List<ActualParam>/*!*/ parameters, List<TypeRef>/*!*/ genericParams)
			: base(position, methodNamePosition, className, classNamePosition, parameters, genericParams)
		{
			this.methodName = methodName;
		}

		/// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitDirectStMtdCall(this);
        }
	}

	#endregion

	#region IndirectStMtdCall

    [Serializable]
    public sealed class IndirectStMtdCall : StaticMtdCall
	{
        public override Operations Operation { get { return Operations.IndirectStaticCall; } }

		private CompoundVarUse/*!*/ methodNameVar;
        /// <summary>Expression that represents name of method</summary>
        public CompoundVarUse/*!*/ MethodNameVar { get { return methodNameVar; } }

		public IndirectStMtdCall(Position position,
                                 GenericQualifiedName className, Position classNamePosition, CompoundVarUse/*!*/ mtdNameVar,
	                             List<ActualParam>/*!*/ parameters, List<TypeRef>/*!*/ genericParams)
            : base(position, mtdNameVar.Position, className, classNamePosition, parameters, genericParams)
		{
			this.methodNameVar = mtdNameVar;
		}

        public IndirectStMtdCall(Position position,
                                 TypeRef/*!*/typeRef, CompoundVarUse/*!*/ mtdNameVar,
                                 List<ActualParam>/*!*/ parameters, List<TypeRef>/*!*/ genericParams)
            : base(position, mtdNameVar.Position, typeRef, parameters, genericParams)
        {
            this.methodNameVar = mtdNameVar;
        }

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitIndirectStMtdCall(this);
        }
	}

	#endregion
}
