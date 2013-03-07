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
	public abstract class StaticFieldUse : VariableUse
	{
		/// <summary>Name of type which's field is being accessed</summary>
        public GenericQualifiedName TypeName { get { return typeRef.GenericQualifiedName; } }

        /// <summary>Position of <see cref="TypeName"/>.</summary>
        public Position TypeNamePosition { get { return this.typeRef.Position; } }

        /// <summary>Position of the field name.</summary>
        public Position NamePosition { get; private set; }

        protected TypeRef typeRef;
		protected DType/*!*/ type;

		/// <summary>
		/// Points to a method that emits code to be placed after the new static field value has
		/// been loaded on the evaluation stack.
		/// </summary>
		internal AssignmentCallback assignmentCallback;

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

		internal override Evaluation Analyze(Analyzer/*!*/ analyzer, ExInfoFromParent info)
		{
			access = info.Access;

            this.typeRef.Analyze(analyzer);
            this.type = this.typeRef.ResolvedTypeOrUnknown;

			analyzer.AnalyzeConstructedType(type);

			return new Evaluation(this);
		}

		#region Emit, EmitAssign, EmitIsset, EmitRead, EmitWrite, EmitEnsure

		/// <include file='Doc/Nodes.xml' path='doc/method[@name="Emit"]/*'/>
		internal override PhpTypeCode Emit(CodeGenerator/*!*/ codeGenerator)
		{
			Statistics.AST.AddNode("FieldUse.Static");
			ChainBuilder chain = codeGenerator.ChainBuilder;
			PhpTypeCode result = PhpTypeCode.Invalid;

			switch (codeGenerator.SelectAccess(access))
			{
				case AccessType.Read:
					result = EmitRead(codeGenerator, false);
					if (chain.IsMember) chain.Lengthen();
					break;

				case AccessType.ReadUnknown:
					result = EmitRead(codeGenerator, true);
					if (chain.IsMember) chain.Lengthen();
					break;

				case AccessType.ReadRef:
					if (chain.IsMember)
					{
						chain.Lengthen();
						result = EmitRead(codeGenerator, false);
					}
					else
					{
						result = EmitRead(codeGenerator, true);
					}
					break;

				case AccessType.Write:
					if (chain.IsMember)
					{
						result = EmitEnsure(codeGenerator, chain);
						chain.Lengthen();
					}
					else
					{
						assignmentCallback = EmitWrite(codeGenerator, false);
						result = PhpTypeCode.Unknown;
					}
					break;

				case AccessType.WriteRef:
					if (chain.IsMember)
					{
						result = EmitEnsure(codeGenerator, chain);
						chain.Lengthen();
					}
					else
					{
						assignmentCallback = EmitWrite(codeGenerator, true);
						result = PhpTypeCode.Unknown;
					}
					break;

				case AccessType.None:
					result = PhpTypeCode.Void;
					break;
			}

			return result;
		}

		internal override PhpTypeCode EmitAssign(CodeGenerator codeGenerator)
		{
			switch (access)
			{
				case AccessType.Write:
				case AccessType.WriteRef:
				case AccessType.WriteAndReadRef:
				case AccessType.WriteAndReadUnknown:
				case AccessType.ReadAndWrite:
				case AccessType.ReadAndWriteAndReadRef:
				case AccessType.ReadAndWriteAndReadUnknown:
					// finish the assignment by invoking the callback obtained in Emit
					assignmentCallback(codeGenerator, PhpTypeCode.Object);
					break;

				default:
					Debug.Fail();
					break;
			}

			return PhpTypeCode.Void;
		}

		internal override PhpTypeCode EmitIsset(CodeGenerator codeGenerator, bool empty)
		{
			Debug.Assert(access == AccessType.Read);

			// Do not report error messages
			codeGenerator.ChainBuilder.QuietRead = true;

			// Emit as if the node is read
			return this.Emit(codeGenerator);
		}

		internal abstract PhpTypeCode EmitRead(CodeGenerator/*!*/ codeGenerator, bool wantRef);
		internal abstract AssignmentCallback EmitWrite(CodeGenerator/*!*/ codeGenerator, bool writeRef);
		internal abstract PhpTypeCode EmitEnsure(CodeGenerator/*!*/ codeGenerator, ChainBuilder/*!*/ chain);

		#endregion

	}

	#endregion

	#region DirectStFldUse

	/// <summary>
	/// Direct static field uses (a static field accessed by field identifier).
	/// </summary>
	public sealed class DirectStFldUse : StaticFieldUse
	{
		internal override Operations Operation { get { return Operations.DirectStaticFieldUse; } }

		private VariableName propertyName;
        /// <summary>Name of static field beign accessed</summary>
        public VariableName PropertyName { get { return propertyName; } }
		private DProperty property;
		private bool runtimeVisibilityCheck;

		public DirectStFldUse(Position position, TypeRef typeRef, VariableName propertyName, Position propertyNamePosition)
            : base(position, propertyNamePosition, typeRef)
        {
            this.propertyName = propertyName;
        }

        public DirectStFldUse(Position position, GenericQualifiedName qualifiedName, Position qualifiedNamePosition, VariableName propertyName, Position propertyNamePosition)
            : this(position, DirectTypeRef.FromGenericQualifiedName(qualifiedNamePosition, qualifiedName), propertyName, propertyNamePosition)
        {
        }

		internal override Evaluation Analyze(Analyzer/*!*/ analyzer, ExInfoFromParent info)
		{
			base.Analyze(analyzer, info);

			property = analyzer.ResolveProperty(type, propertyName, position, true, analyzer.CurrentType, analyzer.CurrentRoutine, out runtimeVisibilityCheck);

			return new Evaluation(this);
		}

		#region EmitRead, EmitWrite, EmitEnsure, EmitUnset

		/// <summary>
		/// Emits IL instructions that read the value of a static field.
		/// </summary>
		/// <param name="codeGenerator">The current <see cref="CodeGenerator"/>.</param>
		/// <param name="wantRef">If <B>false</B> the field value should be left on the evaluation stack,
		/// if <B>true</B> the <see cref="PhpReference"/> should be left on the evaluation stack.</param>
		/// <remarks>
		/// Nothing is expected on the evaluation stack. A <see cref="PhpReference"/> (if <paramref name="wantRef"/>
		/// is <B>true</B>) or the field value itself (if <paramref name="wantRef"/> is <B>false</B>) is left on the
		/// evaluation stack (all PHP static fields are <see cref="PhpReference"/>s).
		/// </remarks>
		internal override PhpTypeCode EmitRead(CodeGenerator/*!*/ codeGenerator, bool wantRef)
		{
			return property.EmitGet(codeGenerator, null, wantRef, type as ConstructedType, runtimeVisibilityCheck);
		}

		/// <summary>
		/// Emits IL instructions that write a value to a static field.
		/// </summary>
		/// <param name="codeGenerator">The current <see cref="CodeGenerator"/>.</param>
		/// <param name="writeRef">If <B>true</B> the value being written is a <see cref="PhpReference"/>
		/// instance, if <B>false</B> it is an <see cref="Object"/> instance.</param>
		/// <returns>Delegate to a method that emits code to be executed when the actual value has been
		/// loaded on the evaluation stack (see <see cref="StaticFieldUse.EmitAssign"/>).</returns>
		internal override AssignmentCallback EmitWrite(CodeGenerator/*!*/ codeGenerator, bool writeRef)
		{
			return property.EmitSet(codeGenerator, null, writeRef, type as ConstructedType, runtimeVisibilityCheck);
		}

		internal override PhpTypeCode EmitEnsure(CodeGenerator/*!*/ codeGenerator, ChainBuilder/*!*/ chain)
		{
			// unknown property of a known type reported as an error during analysis
			Debug.Assert(!property.IsUnknown ||
				property.DeclaringType.IsUnknown ||
				!property.DeclaringType.IsDefinite);

			// we're only interested in a directly accessible property
			return chain.EmitEnsureStaticProperty((runtimeVisibilityCheck) ? null : property, typeRef, propertyName, chain.IsArrayItem);
		}

		/// <summary>
		/// Emits IL instructions that &quot;unset&quot; a static field.
		/// </summary>
		/// <remarks>
		/// <para>
		/// Nothing is expected on the evaluation stack. Nothing is left on the evaluation stack.
		/// </para>
		/// <para>
		/// An error throwing code is always emitted because static fields cannot be unset.
		/// </para>
		/// </remarks>
		internal override void EmitUnset(CodeGenerator/*!*/ codeGenerator)
		{
			property.EmitUnset(codeGenerator, null, type as ConstructedType, runtimeVisibilityCheck);
		}

		#endregion

		internal override void DumpTo(AstVisitor/*!*/ visitor, TextWriter/*!*/ output)
		{
			if (isMemberOf != null)
				isMemberOf.DumpTo(visitor, output);

            typeRef.DumpTo(visitor, output);
			output.Write("::$");
			output.Write(propertyName.ToString());
			DumpAccess(output);
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
	public sealed class IndirectStFldUse : StaticFieldUse
	{
		internal override Operations Operation { get { return Operations.IndirectStaticFieldUse; } }

		private Expression/*!*/ fieldNameExpr;
            /// <summary>Expression that produces name of the field</summary>
        public Expression/*!*/ FieldNameExpr{get{return fieldNameExpr;}}

		public IndirectStFldUse(Position position, TypeRef typeRef, Expression/*!*/ fieldNameExpr)
            : base(position, fieldNameExpr.Position, typeRef)
        {
            this.fieldNameExpr = fieldNameExpr;
        }

		internal override Evaluation Analyze(Analyzer/*!*/ analyzer, ExInfoFromParent info)
		{
			base.Analyze(analyzer, info);

			fieldNameExpr = fieldNameExpr.Analyze(analyzer, ExInfoFromParent.DefaultExInfo).Literalize();

			return new Evaluation(this);
		}

		#region EmitRead, EmitWrite, EmitEnsure, EmitUnset

		/// <summary>
		/// Emits IL instructions that read the value of a static field.
		/// </summary>
		/// <param name="codeGenerator">The current <see cref="CodeGenerator"/>.</param>
		/// <param name="wantRef">If <B>false</B> the field value should be left on the evaluation stack,
		/// if <B>true</B> the <see cref="PhpReference"/> should be left on the evaluation stack.</param>
		/// <remarks>
		/// Nothing is expected on the evaluation stack. A <see cref="PhpReference"/> (if <paramref name="wantRef"/>
		/// is <B>true</B>) or the field value itself (if <paramref name="wantRef"/> is <B>false</B>) is left on the
		/// evaluation stack (all PHP static fields are <see cref="PhpReference"/>s).
		/// </remarks>
		internal override PhpTypeCode EmitRead(CodeGenerator codeGenerator, bool wantRef)
		{
			return codeGenerator.EmitGetStaticPropertyOperator(type, null, fieldNameExpr, wantRef);
		}

		/// <summary>
		/// Emits IL instructions that write the value to a static field.
		/// </summary>
		/// <param name="codeGenerator">The current <see cref="CodeGenerator"/>.</param>
		/// <param name="writeRef">If <B>true</B> the value being written is a <see cref="PhpReference"/>
		/// instance, if <B>false</B> it is an <see cref="Object"/> instance.</param>
		/// <returns>Delegate to a method that emits code to be executed when the actual value has been
		/// loaded on the evaluation stack (see <see cref="StaticFieldUse.EmitAssign"/>).</returns>
		internal override AssignmentCallback EmitWrite(CodeGenerator codeGenerator, bool writeRef)
		{
			return codeGenerator.EmitSetStaticPropertyOperator(type, null, fieldNameExpr, writeRef);

			// obsolete:
			//codeGenerator.IL.Emit(OpCodes.Ldstr, className.QualifiedName.ToString());
			//codeGenerator.EmitBoxing(fieldNameExpr.Emit(codeGenerator));

			//return delegate(CodeGenerator codeGen)
			//{
			//  codeGen.EmitLoadClassContext();
			//  codeGen.EmitLoadScriptContext();
			//  codeGen.EmitLoadNamingContext();

			//  // invoke the operator
			//  codeGen.IL.EmitCall(OpCodes.Call, Methods.Operators.SetStaticProperty, null);
			//};
		}

		internal override PhpTypeCode EmitEnsure(CodeGenerator/*!*/ codeGenerator, ChainBuilder chain)
		{
			return chain.EmitEnsureStaticProperty(typeRef, null, fieldNameExpr, chain.IsArrayItem);
		}

		/// <summary>
		/// Emits IL instructions that &quot;unset&quot; a static field.
		/// </summary>
		/// <remarks>
		/// <para>
		/// Nothing is expected on the evaluation stack. Nothing is left on the evaluation stack.
		/// </para>
		/// <para>
		/// Call to the <see cref="Operators.UnsetStaticProperty"/> error throwing operator is always emitted because static
		/// fields cannot be unset.
		/// </para>
		/// </remarks>
		internal override void EmitUnset(CodeGenerator codeGenerator)
		{
			codeGenerator.EmitUnsetStaticPropertyOperator(type, null, fieldNameExpr);
		}

		#endregion

		internal override void DumpTo(AstVisitor/*!*/ visitor, TextWriter/*!*/ output)
		{
            typeRef.DumpTo(visitor, output);			
			output.Write("::{");
			fieldNameExpr.DumpTo(visitor, output);
			output.Write('}');
			DumpAccess(output);
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
