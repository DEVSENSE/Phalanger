/*

 Copyright (c) 2004-2006 Tomas Matousek, Vaclav Novak and Martin Maly.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Reflection.Emit;
using System.Diagnostics;

using PHP.Core.Parsers;

namespace PHP.Core.AST
{
	#region ConstantUse

	/// <summary>
	/// Base class for constant uses.
	/// </summary>
    [Serializable]
	public abstract class ConstantUse : Expression
	{
		public ConstantUse(Position position)
			: base(position)
		{
		}
	}

	#endregion

	#region GlobalConstUse

	/// <summary>
	/// Global constant use (constants defined by <c>define</c> function).
	/// </summary>
    [Serializable]
    public sealed class GlobalConstUse : ConstantUse
	{
        public override Operations Operation { get { return Operations.GlobalConstUse; } }

		public QualifiedName Name { get { return name; } }
		private QualifiedName name;

        /// <summary>
        /// Name used when the <see cref="Name"/> is not found. Used when reading global constant in a namespace context.
        /// </summary>
        internal QualifiedName? FallbackName { get { return fallbackName; } }
        private QualifiedName? fallbackName;

		public GlobalConstUse(Position position, QualifiedName name, QualifiedName? fallbackName)
			: base(position)
		{
			this.name = name;
            this.fallbackName = fallbackName;
		}

		/// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitGlobalConstUse(this);
        }
	}

	#endregion

	#region ClassConstUse

	/// <summary>
	/// Class constant use.
	/// </summary>
    [Serializable]
    public class ClassConstUse : ConstantUse
	{
		public override Operations Operation { get { return Operations.ClassConstUse; } }

        /// <summary>
        /// Class name. May have an empty <see cref="Name"/> if the class is referenced indirectly.
        /// </summary>
        public GenericQualifiedName ClassName { get { return this.typeRef.GenericQualifiedName; } }

        /// <summary>
        /// Class type reference.
        /// </summary>
        public TypeRef/*!*/TypeRef { get { return this.typeRef; } }
        private readonly TypeRef/*!*/typeRef;
        
		public VariableName Name { get { return name; } }
		private readonly VariableName name;

        /// <summary>
        /// Position of <see cref="Name"/> part of the constant use.
        /// </summary>
        public Position NamePosition { get; private set; }

		public ClassConstUse(Position position, GenericQualifiedName className, Position classNamePosition, string/*!*/ name, Position namePosition)
            : this(position, DirectTypeRef.FromGenericQualifiedName(classNamePosition, className), name, namePosition)
		{
		}

        public ClassConstUse(Position position, TypeRef/*!*/typeRef, string/*!*/ name, Position namePosition)
            : base(position)
        {
            Debug.Assert(typeRef != null);
            Debug.Assert(!string.IsNullOrEmpty(name));

            this.typeRef = typeRef;
			this.name = new VariableName(name);
            this.NamePosition = namePosition;
        }

		/// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitClassConstUse(this);
        }
	}

    /// <summary>
    /// Pseudo class constant use.
    /// </summary>
    [Serializable]
    public sealed class PseudoClassConstUse : ClassConstUse
    {
        /// <summary>
        /// Possible types of pseudo class constant.
        /// </summary>
        public enum Types
        {
            Class
        }

        /// <summary>Type of pseudoconstant</summary>
        public Types Type { get { return consttype; } }
        private Types consttype;
        
        public PseudoClassConstUse(Position position, GenericQualifiedName className, Position classNamePosition, Types type, Position namePosition)
            : this(position, DirectTypeRef.FromGenericQualifiedName(classNamePosition, className), type, namePosition)
		{
		}

        public PseudoClassConstUse(Position position, TypeRef/*!*/typeRef, Types type, Position namePosition)
            : base(position, typeRef, type.ToString().ToLowerInvariant(), namePosition)
        {
            this.consttype = type;
        }

        public override object Value
        {
            get
            {
                switch (this.consttype)
                {
                    case Types.Class:
                        var className = this.ClassName;
                        if (string.IsNullOrEmpty(className.QualifiedName.Name.Value) ||
                            className.QualifiedName.IsStaticClassName ||
                            className.QualifiedName.IsSelfClassName)
                            return null;

                        return className.QualifiedName.ToString();

                    default:
                        throw new InvalidOperationException();
                }
            }
        }

        public override void VisitMe(TreeVisitor visitor)
        {
            base.VisitMe(visitor);
        }
    }

	#endregion

    #region PseudoConstUse

    /// <summary>
	/// Pseudo-constant use (PHP keywords: __LINE__, __FILE__, __DIR__, __FUNCTION__, __METHOD__, __CLASS__, __NAMESPACE__)
	/// </summary>
    [Serializable]
    public sealed class PseudoConstUse : Expression
	{
        public override Operations Operation { get { return Operations.PseudoConstUse; } }

		public enum Types { Line, File, Class, Function, Method, Namespace, Dir }

		private Types type;
        /// <summary>Type of pseudoconstant</summary>
        public Types Type { get { return type; } }

		public PseudoConstUse(Position position, Types type)
			: base(position)
		{
			this.type = type;
		}

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitPseudoConstUse(this);
        }
	}

	#endregion
}
