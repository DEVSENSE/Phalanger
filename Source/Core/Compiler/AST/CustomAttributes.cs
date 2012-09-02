/*

 Copyright (c) 2006 Tomas Matousek.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using PHP.Core.Parsers;
using PHP.Core.Reflection;
using System.Reflection.Emit;
using System.Reflection;

namespace PHP.Core.AST
{
	[Flags]
	public enum PhpAttributeTargets
	{
		Assembly = 1,
		Function = 2,
		Method = 4,
		Class = 8,
		Interface = 16,
		Property = 32,
		Constant = 64,
		Parameter = 128,
		ReturnValue = 256,
		GenericParameter = 512,

		Routines = Function | Method,
		Types = Class | Interface,
		ClassMembers = Method | Property | Constant,

		All = Assembly | Function | Method | Class | Interface | Property | Constant | Parameter | ReturnValue | GenericParameter
	}

	public enum SpecialAttributes
	{
		AttributeUsage,
		AppStatic,
		Export,
		Out
	}

	internal interface IPhpCustomAttributeProvider
	{
		PhpAttributeTargets AttributeTarget { get; }  // unused
		AttributeTargets AcceptsTargets { get; }
		int GetAttributeUsageCount(DType/*!*/ type, CustomAttribute.TargetSelectors selector);
		void ApplyCustomAttribute(SpecialAttributes kind, Attribute attribute, CustomAttribute.TargetSelectors selector);
		void EmitCustomAttribute(CustomAttributeBuilder/*!*/ builder, CustomAttribute.TargetSelectors selector);
	}

	#region CustomAttributes

	public struct CustomAttributes
	{
		public List<CustomAttribute> Attributes { get { return attributes; } }
		private List<CustomAttribute> attributes;

		/// <summary>
		/// Creates a set of custom attributes.
		/// </summary>
		public CustomAttributes(List<CustomAttribute> attributes)
		{
			this.attributes = attributes;
		}

		internal void Merge(CustomAttributes other)
		{
			if (other.attributes == null)
				return;

			if (attributes == null)
			{
				attributes = other.attributes;
				return;
			}

			attributes.AddRange(other.attributes);

			other.attributes = null;
		}

		internal void AnalyzeMembers(Analyzer/*!*/ analyzer, Scope referringScope)
		{
			if (attributes == null) return;

			foreach (CustomAttribute attribute in attributes)
				attribute.AnalyzeMembers(analyzer, referringScope);
		}

		internal void Analyze(Analyzer/*!*/ analyzer, IPhpCustomAttributeProvider/*!*/ target)
		{
			if (attributes == null) return;

			bool duplicate_found = false;

			foreach (CustomAttribute attribute in attributes)
				attribute.Analyze(analyzer, target, ref duplicate_found);
		}

		internal int Count(DType/*!*/ attributeType, AST.CustomAttribute.TargetSelectors selector)
		{
			if (attributes == null) return 0;

			int count = 0;

			foreach (CustomAttribute attribute in attributes)
			{
				if (attribute.TargetSelector == selector && attribute.Type.Equals(attributeType))
					count++;
			}

			return count;
		}

		internal void Emit(CodeGenerator/*!*/ codeGen, IPhpCustomAttributeProvider/*!*/ target)
		{
			if (attributes == null) return;

			foreach (CustomAttribute attribute in attributes)
				attribute.Emit(codeGen, target);
		}
	}

	#endregion

	#region CustomAttribute

	public sealed class CustomAttribute : LangElement, IPostAnalyzable
	{
		#region Special Attributes

		internal static readonly DType/*!*/ AppStaticAttribute =
	  new ClrType(DTypeDesc.Create(typeof(AppStaticAttribute)), new QualifiedName(Name.AppStaticName));


		internal static readonly DType/*!*/ ExportAttribute =
		  new ClrType(DTypeDesc.Create(typeof(ExportAttribute)), new QualifiedName(Name.ExportName));
        internal static readonly DType/*!*/ DllImportAttribute =
          new ClrType(DTypeDesc.Create(typeof(System.Runtime.InteropServices.DllImportAttribute)), new QualifiedName(Name.DllImportName));

		// Is it correct to create ClrType here as the mscorlib reflection only creates type-desc via the cache.
		// the ClrType is created lazily and only if not yet created (which won't be the case).
		internal static readonly DType/*!*/ OutAttribute =
		  new ClrType(DTypeDesc.Create(typeof(System.Runtime.InteropServices.OutAttribute)), new QualifiedName(Name.OutName));
		
		#endregion

		#region Nested Types: TargetSelectors

		/// <summary>
		/// Available target selectors. Lowercased names are reported to the user.
		/// The mapping to the <see cref="AttributeTargets"/> is used for correct usage checking.
		/// </summary>
		public enum TargetSelectors
		{
			Default = AttributeTargets.All,
			Return = AttributeTargets.ReturnValue,
			Assembly = AttributeTargets.Assembly,
			Module = AttributeTargets.Module
		}

		#endregion

		public TargetSelectors TargetSelector { get { return targetSelector; } internal /* friend Parser */ set { targetSelector = value; } }
		private TargetSelectors targetSelector;

		public QualifiedName QualifiedName { get { return qualifiedName; } }
		private QualifiedName qualifiedName;

		public CallSignature CallSignature { get { return callSignature; } }
		private CallSignature callSignature;

		public List<NamedActualParam>/*!*/ NamedParameters { get { return namedParameters; } }
		private List<NamedActualParam>/*!*/ namedParameters;

		internal DType/*!MA*/ Type { get { return type; } }
		private DType/*!MA*/ type;

		private RoutineSignature/*!A*/ overload;
		private IPhpCustomAttributeProvider/*!MA*/ target;
		private bool isEmitted;

		public CustomAttribute(Position position, QualifiedName qualifiedName, List<ActualParam>/*!*/ parameters,
				List<NamedActualParam>/*!*/ namedParameters)
			: base(position)
		{
			this.qualifiedName = qualifiedName;
			this.namedParameters = namedParameters;
			this.callSignature = new CallSignature(parameters, TypeRef.EmptyList);
		}

		#region Analysis

		/// <summary>
		/// Analyzes the attribute type. Parameters are left to the full analysis as we want to resolve global and class 
		/// constants used there. Resolving attribute type during member analysis enables to them to influence the 
		/// member and full analysis of their respective targets.
		/// </summary>
		internal void AnalyzeMembers(Analyzer/*!*/ analyzer, Scope referringScope)
		{
			// resolve attribute type:
			type = analyzer.ResolveCustomAttributeType(qualifiedName, referringScope, position);

			// let the Assembly Builder know that this attribute is defined on it;
			// we need the Builder to be able to count the defined attributes in analysis:
			if (targetSelector == TargetSelectors.Assembly || targetSelector == TargetSelectors.Module)
			{
				analyzer.SourceUnit.CompilationUnit.ModuleBuilder.AssemblyBuilder.CustomAttributeDefined(analyzer.ErrorSink, this);
			}
		}

		/// <summary>
		/// Analyses attribute parameters, resolves the constructor, the fields, and the properties 
		/// assigned in the parameters.
		/// </summary>
		internal void Analyze(Analyzer/*!*/ analyzer, IPhpCustomAttributeProvider/*!*/ target, ref bool duplicateFound)
		{
			Debug.Assert(type != null);

			this.target = target;

			// check selector:
			if (((int)target.AcceptsTargets & (int)targetSelector) == 0)
			{
				analyzer.ErrorSink.Add(Errors.InvalidAttributeTargetSelector, analyzer.SourceUnit, position,
					targetSelector.ToString().ToLower(System.Globalization.CultureInfo.InvariantCulture));
			}

			// resolve constructor for the regular attributes:
			if (!ResolveConstructor(analyzer))
				return;

			// evaluate actual arguments:
			bool all_evaluable;
			AnalyzeActualArguments(analyzer, out all_evaluable);

			// check usage:
			if (!CheckAttributeUsage(analyzer, ref duplicateFound))
				return;

			if (!all_evaluable)
				return;

			// process special and pseudo-attributes:
			ApplySpecialAttributes(analyzer);
		}

		private void ApplySpecialAttributes(Analyzer/*!*/ analyzer)
		{
			if (type.Equals(AppStaticAttribute))
			{
				Debug.Assert(callSignature.Parameters.Count == 0, "Should be checked by ResolveOverload");
				ApplySpecialAttribute(analyzer, SpecialAttributes.AppStatic, null);
				isEmitted = false;
			}
			else if (type.Equals(ExportAttribute))
			{
				Debug.Assert(callSignature.Parameters.Count == 0, "Should be checked by ResolveOverload");

				if (!analyzer.SourceUnit.CompilationUnit.IsPure)
				{
					analyzer.ErrorSink.Add(Errors.ExportAttributeInNonPureUnit, analyzer.SourceUnit, this.position);
				}
				else
				{
					ApplySpecialAttribute(analyzer, SpecialAttributes.Export, Core.ExportAttribute.Default);
				}

				isEmitted = false;
			}
			else if (type.Equals(OutAttribute))
			{
				Debug.Assert(callSignature.Parameters.Count == 0, "Should be checked by ResolveOverload");
				ApplySpecialAttribute(analyzer, SpecialAttributes.Out, new System.Runtime.InteropServices.OutAttribute());
				isEmitted = true;
			}
            else if(type.Equals(DllImportAttribute)){
                isEmitted = false;
            }
			else if (ReferenceEquals(type.TypeDesc, DTypeDesc.AttributeUsageAttributeTypeDesc))
			{
				// set usage of the attribute defined by this attribute's target //

				Debug.Assert(callSignature.Parameters.Count > 0, "Missing arguments should be checked by ResolveOverload");

				int valid_on = Convert.ObjectToInteger(callSignature.Parameters[0].Expression.Value);

				AttributeUsageAttribute usage = new AttributeUsageAttribute((AttributeTargets)valid_on);

				foreach (NamedActualParam param in namedParameters)
				{
					if (param.Name.Equals("AllowMultiple"))
					{
						usage.AllowMultiple = Convert.ObjectToBoolean(param.Expression.Value);
					}
					else if (param.Name.Equals("Inherited"))
					{
						usage.Inherited = Convert.ObjectToBoolean(param.Expression.Value);
					}
				}

				ApplySpecialAttribute(analyzer, SpecialAttributes.AttributeUsage, usage);
				isEmitted = true;
			}
			else
			{
				isEmitted = true;
			}
		}

		private void ApplySpecialAttribute(Analyzer/*!*/ analyzer, SpecialAttributes kind, Attribute attribute)
		{
			switch (targetSelector)
			{
				case TargetSelectors.Assembly:
				case TargetSelectors.Module:
					analyzer.SourceUnit.CompilationUnit.ModuleBuilder.AssemblyBuilder.ApplyCustomAttribute(kind, attribute, targetSelector);
					break;

				default:
					target.ApplyCustomAttribute(kind, attribute, targetSelector);
					break;
			}
		}

		private bool ResolveConstructor(Analyzer/*!*/ analyzer)
		{
			if (!type.IsDefinite)
			{
				// attribute type has to be known definitely:
				analyzer.ErrorSink.Add(Errors.UnknownCustomAttribute, analyzer.SourceUnit, position,
				  type.FullName, type.FullName + "Attribute");

				return false;
			}

			if (!type.IsCustomAttributeType)
			{
				analyzer.ErrorSink.Add(Errors.NotCustomAttributeClass, analyzer.SourceUnit, position, type.FullName);
				return false;
			}

			// resolve ctor overload in global context (only public ctors are visible):
			bool check_visibility;
			DRoutine constructor = analyzer.ResolveConstructor(type, position, null, null, out check_visibility);
			Debug.Assert(!check_visibility);

			if (constructor.ResolveOverload(analyzer, callSignature, position, out overload) == DRoutine.InvalidOverloadIndex)
			{
				analyzer.ErrorSink.Add(Errors.ClassHasNoVisibleCtor, analyzer.SourceUnit, position, type.FullName);
				return false;
			}

			return true;
		}

		private void AnalyzeActualArguments(Analyzer/*!*/ analyzer, out bool allEvaluable)
		{
			callSignature.Analyze(analyzer, overload, ExInfoFromParent.DefaultExInfo, false);

			allEvaluable = true;

			// parameters:
			foreach (ActualParam param in callSignature.Parameters)
			{
				if (!param.Expression.IsCustomAttributeArgumentValue)
				{
					// expression has to be evaluable:
					analyzer.ErrorSink.Add(Errors.InvalidAttributeExpression, analyzer.SourceUnit, param.Position);
					allEvaluable = false;
				}
			}

			// named parameters:
			foreach (NamedActualParam param in namedParameters)
			{
				param.Analyze(analyzer, type);

				if (!param.Expression.IsCustomAttributeArgumentValue)
				{
					// expression has to be evaluable:
					analyzer.ErrorSink.Add(Errors.InvalidAttributeExpression, analyzer.SourceUnit, param.Position);
					allEvaluable = false;
				}
			}
		}

		private bool CheckAttributeUsage(Analyzer/*!*/ analyzer, ref bool duplicateFound)
		{
			// check usage target of this attribute:
			bool is_usage_definite;
			AttributeUsageAttribute specific_usage = type.GetCustomAttributeUsage(out is_usage_definite);
			if (is_usage_definite)
			{
				if (!CheckAttributeUsage(analyzer, specific_usage, ref duplicateFound)) return false;
			}
			else
			{
				// usage constraints cannot be determined now; ckeck the usage later during post-analysis:
				analyzer.PostAnalyzed.Add(this);
			}

			return true;
		}

		private bool CheckAttributeUsage(Analyzer/*!*/ analyzer, AttributeUsageAttribute usage, ref bool duplicateFound)
		{
			if (usage != null && (target.AcceptsTargets & usage.ValidOn) == 0)
			{
				analyzer.ErrorSink.Add(Errors.InvalidAttributeUsage, analyzer.SourceUnit, position, qualifiedName.ToString());
				return false;
			}

			// check duplicate usage of this attribute:
			if (!duplicateFound && (usage == null || !usage.AllowMultiple) && target.GetAttributeUsageCount(type, targetSelector) > 1)
			{
				analyzer.ErrorSink.Add(Errors.DuplicateAttributeUsage, analyzer.SourceUnit, position, qualifiedName.ToString());
				duplicateFound = true;
				return false;
			}

			return true;
		}

		void IPostAnalyzable.PostAnalyze(Analyzer/*!*/ analyzer)
		{
			// Note, usage needn't to be definite even now (it is not set unless the [AttributeUsage] is explicitly specified)
			bool is_usage_definite;
			bool duplicateFound = false; // TODO: duplicates should be tracked!!!
			CheckAttributeUsage(analyzer, type.GetCustomAttributeUsage(out is_usage_definite), ref duplicateFound);
		}

		#endregion

		#region Emission
        internal void Emit(CodeGenerator/*!*/ codeGen, IPhpCustomAttributeProvider/*!*/ target) {
            Emit(codeGen, target, false);
        }

		internal void Emit(CodeGenerator/*!*/ codeGen, IPhpCustomAttributeProvider/*!*/ target, bool force)
		{
			// skip attributes that are not emitted:
			if (!isEmitted && !force) return;

			ConstructorInfo real_ctor;

			// TODO: type conversions (in analysis during overload resolution?)

            Type[] real_ctor_parameter_types = new Type[callSignature.Parameters.Count];

			if (type is ClrType)
			{
				real_ctor = (ConstructorInfo)((ClrMethod.Overload)overload).Method;
                var real_params = real_ctor.GetParameters();

                for (int i = 0; i < real_ctor_parameter_types.Length; i++)
                    real_ctor_parameter_types[i] = real_params[i].ParameterType;
			}
			else
			{
				Debug.Assert(type is PhpType);
				real_ctor = ((PhpType)type).ClrConstructorInfos[0];
                
                // Do not try to call GetParameters(), all parameters are of type object,
                // GetParameters() of not baked PhpType throws.
                for (int i = 0; i < real_ctor_parameter_types.Length; i++)
                    real_ctor_parameter_types[i] = PHP.Core.Emit.Types.Object[0];
			}

			// ctor args:
			object[] ctor_args = new object[callSignature.Parameters.Count];
			
			for (int i = 0; i < callSignature.Parameters.Count; i++)
			{
				Expression expr = callSignature.Parameters[i].Expression;
				
				TypeOfEx type_of = expr as TypeOfEx;
				if (type_of != null)
				{
					ctor_args[i] = type_of.ClassNameRef.ResolvedType.RealType;
				}
				else
				{
                    ctor_args[i] = ConvertToClr.ObjectToType(expr.Value, real_ctor_parameter_types[i]);
				}
			}	

			List<FieldInfo> fields = new List<FieldInfo>();
			List<PropertyInfo> properties = new List<PropertyInfo>();
			List<object> field_values = new List<object>();
			List<object> prop_values = new List<object>();

			foreach (NamedActualParam param in namedParameters)
			{
				object value = param.Expression.Value;

				MemberInfo real_member = param.Property.RealMember;
				FieldInfo real_field;
                PropertyInfo real_property;

                if ((real_property = real_member as PropertyInfo) != null ||    // regular CLR property
                    (param.Property is PhpField && (real_property = ((PhpField)param.Property).ExportedProperty) != null))  // or PHP property (real field below is PhpReference, we have to use its export stub)
                {
                    properties.Add(real_property);
                    prop_values.Add(ConvertToClr.ObjectToType(value, real_property.PropertyType));
                }
				else if ((real_field = real_member as FieldInfo) != null)
				{
					fields.Add(real_field);
                    field_values.Add(ConvertToClr.ObjectToType(value, real_field.FieldType));
				}
                else
                {
                    Debug.Fail("Cannot resolve attribute named parameter!");
                }
			}

			CustomAttributeBuilder builder = new CustomAttributeBuilder(real_ctor, ctor_args,
				properties.ToArray(), prop_values.ToArray(), fields.ToArray(), field_values.ToArray());

			switch (targetSelector)
			{
				case TargetSelectors.Assembly:
				case TargetSelectors.Module:
					codeGen.CompilationUnit.ModuleBuilder.AssemblyBuilder.EmitCustomAttribute(builder, targetSelector);
					break;

				default:
					target.EmitCustomAttribute(builder, targetSelector);
					break;
			}
		}

		#endregion

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitCustomAttribute(this);
        }
	}

	#endregion
}
