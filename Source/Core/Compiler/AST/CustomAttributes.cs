/*

 Copyright (c) 2013 DEVSENSE.
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
using System.Reflection.Emit;
using System.Reflection;
using PHP.Core.AST;
using PHP.Core.Parsers;
using PHP.Core.Reflection;
using System.Diagnostics;

namespace PHP.Core.Compiler.AST
{
    internal interface IPhpCustomAttributeProvider
    {
        PhpAttributeTargets AttributeTarget { get; }
        AttributeTargets AcceptsTargets { get; }
        int GetAttributeUsageCount(DType/*!*/ type, CustomAttribute.TargetSelectors selector);
        void ApplyCustomAttribute(SpecialAttributes kind, Attribute attribute, CustomAttribute.TargetSelectors selector);
        void EmitCustomAttribute(CustomAttributeBuilder/*!*/ builder, CustomAttribute.TargetSelectors selector);
    }

    #region SpecialCustomAttribute

    internal static class SpecialCustomAttribute
    {
        // Is it correct to create ClrType here as the mscorlib reflection only creates type-desc via the cache.
        // the ClrType is created lazily and only if not yet created (which won't be the case).

        public static readonly DType/*!*/ AppStaticAttribute =
            new ClrType(DTypeDesc.Create(typeof(AppStaticAttribute)), new QualifiedName(Name.AppStaticName));

        public static readonly DType/*!*/ ExportAttribute =
            new ClrType(DTypeDesc.Create(typeof(ExportAttribute)), new QualifiedName(Name.ExportName));

        public static readonly DType/*!*/ DllImportAttribute =
            new ClrType(DTypeDesc.Create(typeof(System.Runtime.InteropServices.DllImportAttribute)), new QualifiedName(Name.DllImportName));
                
        public static readonly DType/*!*/ OutAttribute =
            new ClrType(DTypeDesc.Create(typeof(System.Runtime.InteropServices.OutAttribute)), new QualifiedName(Name.OutName));
    }

    #endregion

    #region ICustomAttributesCompiler

    internal interface ICustomAttributesCompiler : INodeCompiler
    {
        void AnalyzeMembers(CustomAttributes/*!*/node, Analyzer/*!*/ analyzer, Scope referringScope);        
        void Analyze(CustomAttributes/*!*/node, Analyzer/*!*/ analyzer, IPhpCustomAttributeProvider/*!*/ target);        
        int Count(CustomAttributes/*!*/node, DType/*!*/ attributeType, CustomAttribute.TargetSelectors selector);
        void Emit(CustomAttributes/*!*/node, CodeGenerator/*!*/ codeGenerator, IPhpCustomAttributeProvider/*!*/ target);
    }

    internal static class CustomAttributesCompilerHelper
    {
        private static ICustomAttributesCompiler/*!*/Compiler(this CustomAttributes/*!*/node) { return node.NodeCompiler<ICustomAttributesCompiler>(); }

        public static void AnalyzeMembers(this CustomAttributes/*!*/node, Analyzer/*!*/ analyzer, Scope referringScope)
        {
            node.Compiler().AnalyzeMembers(node, analyzer, referringScope);
        }
        public static void Analyze(this CustomAttributes/*!*/node, Analyzer/*!*/ analyzer, IPhpCustomAttributeProvider/*!*/ target)
        {
            node.Compiler().Analyze(node, analyzer, target);
        }
        public static int Count(this CustomAttributes/*!*/node, DType/*!*/ attributeType, CustomAttribute.TargetSelectors selector)
        {
            return node.Compiler().Count(node, attributeType, selector);
        }
        public static void Emit(this CustomAttributes/*!*/node, CodeGenerator/*!*/ codeGenerator, IPhpCustomAttributeProvider/*!*/ target)
        {
            node.Compiler().Emit(node, codeGenerator, target);
        }
    }

    #endregion

    #region ICustomAttributeCompiler

    internal interface ICustomAttributeCompiler : INodeCompiler
    {
        DType/*!MA*/ ResolvedType { get; }
        void AnalyzeMembers(Analyzer/*!*/ analyzer, Scope referringScope);
        void Analyze(Analyzer/*!*/ analyzer, IPhpCustomAttributeProvider/*!*/ target, ref bool duplicateFound);
        void Emit(CodeGenerator/*!*/ codeGen, IPhpCustomAttributeProvider/*!*/ target);
        void Emit(CodeGenerator/*!*/ codeGen, IPhpCustomAttributeProvider/*!*/ target, bool force);
    }

    internal static class CustomAttributeCompilerHelper
    {
        private static ICustomAttributeCompiler/*!*/Compiler(this CustomAttribute/*!*/node) { return node.NodeCompiler<ICustomAttributeCompiler>(); }

        public static DType GetResolvedType(this CustomAttribute/*!*/node)
        {
            return node.Compiler().ResolvedType;
        }

        public static void AnalyzeMembers(this CustomAttribute/*!*/node, Analyzer/*!*/ analyzer, Scope referringScope)
        {
            node.Compiler().AnalyzeMembers(analyzer, referringScope);
        }
        public static void Analyze(this CustomAttribute/*!*/node, Analyzer/*!*/ analyzer, IPhpCustomAttributeProvider/*!*/ target, ref bool duplicateFound)
        {
            node.Compiler().Analyze(analyzer, target, ref duplicateFound);
        }
        public static void Emit(this CustomAttribute/*!*/node, CodeGenerator/*!*/ codeGen, IPhpCustomAttributeProvider/*!*/ target)
        {
            node.Compiler().Emit(codeGen, target);
        }
        public static void Emit(this CustomAttribute/*!*/node, CodeGenerator/*!*/ codeGen, IPhpCustomAttributeProvider/*!*/ target, bool force)
        {
            node.Compiler().Emit(codeGen, target, force);
        }
    }

    #endregion

    partial class NodeCompilers
    {
        #region CustomAttributes

        [NodeCompiler(typeof(CustomAttributes), Singleton = true)]
        sealed class CustomAttributesCompiler : ICustomAttributesCompiler
        {
            public void AnalyzeMembers(CustomAttributes/*!*/node, Analyzer/*!*/ analyzer, Scope referringScope)
            {
                if (node.Attributes == null) return;

                foreach (CustomAttribute attribute in node.Attributes)
                    attribute.AnalyzeMembers(analyzer, referringScope);
            }

            public void Analyze(CustomAttributes/*!*/node, Analyzer/*!*/ analyzer, IPhpCustomAttributeProvider/*!*/ target)
            {
                if (node.Attributes == null) return;

                bool duplicate_found = false;

                foreach (CustomAttribute attribute in node.Attributes)
                    attribute.Analyze(analyzer, target, ref duplicate_found);
            }

            public int Count(CustomAttributes/*!*/node, DType/*!*/ attributeType, CustomAttribute.TargetSelectors selector)
            {
                if (node.Attributes == null) return 0;

                int count = 0;

                foreach (CustomAttribute attribute in node.Attributes)
                {
                    if (attribute.TargetSelector == selector && attribute.GetResolvedType().Equals(attributeType))
                        count++;
                }

                return count;
            }

            public void Emit(CustomAttributes/*!*/node, CodeGenerator/*!*/ codeGenerator, IPhpCustomAttributeProvider/*!*/ target)
            {
                if (node.Attributes == null) return;

                foreach (CustomAttribute attribute in node.Attributes)
                    attribute.Emit(codeGenerator, target);
            }
        }

        #endregion

        #region CustomAttribute

        [NodeCompiler(typeof(CustomAttribute))]
        sealed class CustomAttributeCompiler : ICustomAttributeCompiler, IPostAnalyzable
        {
            private readonly CustomAttribute/*!*/node;

            public DType/*!MA*/ ResolvedType { get { return type; } }
            private DType/*!MA*/ type;

            private RoutineSignature/*!A*/ overload;
            private IPhpCustomAttributeProvider/*!MA*/ target;
            private bool isEmitted;

            public CustomAttributeCompiler(CustomAttribute/*!*/node)
            {
                this.node = node;
            }

            #region Analysis

            /// <summary>
            /// Analyzes the attribute type. Parameters are left to the full analysis as we want to resolve global and class 
            /// constants used there. Resolving attribute type during member analysis enables to them to influence the 
            /// member and full analysis of their respective targets.
            /// </summary>
            public void AnalyzeMembers(Analyzer/*!*/ analyzer, Scope referringScope)
            {
                // resolve attribute type:
                type = analyzer.ResolveCustomAttributeType(node.QualifiedName, referringScope, node.Span);

                // let the Assembly Builder know that this attribute is defined on it;
                // we need the Builder to be able to count the defined attributes in analysis:
                if (node.TargetSelector == CustomAttribute.TargetSelectors.Assembly || node.TargetSelector == CustomAttribute.TargetSelectors.Module)
                {
                    analyzer.SourceUnit.CompilationUnit.ModuleBuilder.AssemblyBuilder.CustomAttributeDefined(analyzer.ErrorSink, node);
                }
            }

            /// <summary>
            /// Analyses attribute parameters, resolves the constructor, the fields, and the properties 
            /// assigned in the parameters.
            /// </summary>
            public void Analyze(Analyzer/*!*/ analyzer, IPhpCustomAttributeProvider/*!*/ target, ref bool duplicateFound)
            {
                Debug.Assert(type != null);

                this.target = target;

                // check selector:
                if (((int)target.AcceptsTargets & (int)node.TargetSelector) == 0)
                {
                    analyzer.ErrorSink.Add(Errors.InvalidAttributeTargetSelector, analyzer.SourceUnit, node.Span,
                        node.TargetSelector.ToString().ToLower(System.Globalization.CultureInfo.InvariantCulture));
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
                if (type.Equals(SpecialCustomAttribute.AppStaticAttribute))
                {
                    Debug.Assert(node.CallSignature.Parameters.Empty(), "Should be checked by ResolveOverload");
                    ApplySpecialAttribute(analyzer, SpecialAttributes.AppStatic, null);
                    isEmitted = false;
                }
                else if (type.Equals(SpecialCustomAttribute.ExportAttribute))
                {
                    Debug.Assert(node.CallSignature.Parameters.Empty(), "Should be checked by ResolveOverload");

                    if (!analyzer.SourceUnit.CompilationUnit.IsPure)
                    {
                        analyzer.ErrorSink.Add(Errors.ExportAttributeInNonPureUnit, analyzer.SourceUnit, node.Span);
                    }
                    else
                    {
                        ApplySpecialAttribute(analyzer, SpecialAttributes.Export, Core.ExportAttribute.Default);
                    }

                    isEmitted = false;
                }
                else if (type.Equals(SpecialCustomAttribute.OutAttribute))
                {
                    Debug.Assert(node.CallSignature.Parameters.Empty(), "Should be checked by ResolveOverload");
                    ApplySpecialAttribute(analyzer, SpecialAttributes.Out, new System.Runtime.InteropServices.OutAttribute());
                    isEmitted = true;
                }
                else if (type.Equals(SpecialCustomAttribute.DllImportAttribute))
                {
                    isEmitted = false;
                }
                else if (ReferenceEquals(type.TypeDesc, DTypeDesc.AttributeUsageAttributeTypeDesc))
                {
                    // set usage of the attribute defined by this attribute's target //

                    Debug.Assert(node.CallSignature.Parameters.Empty(), "Missing arguments should be checked by ResolveOverload");

                    int valid_on = Convert.ObjectToInteger(node.CallSignature.Parameters[0].Expression.GetValue());

                    AttributeUsageAttribute usage = new AttributeUsageAttribute((AttributeTargets)valid_on);

                    foreach (NamedActualParam param in node.NamedParameters)
                    {
                        if (param.Name.Equals("AllowMultiple"))
                        {
                            usage.AllowMultiple = Convert.ObjectToBoolean(param.Expression.GetValue());
                        }
                        else if (param.Name.Equals("Inherited"))
                        {
                            usage.Inherited = Convert.ObjectToBoolean(param.Expression.GetValue());
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
                switch (node.TargetSelector)
                {
                    case CustomAttribute.TargetSelectors.Assembly:
                    case CustomAttribute.TargetSelectors.Module:
                        analyzer.SourceUnit.CompilationUnit.ModuleBuilder.AssemblyBuilder.ApplyCustomAttribute(kind, attribute, node.TargetSelector);
                        break;

                    default:
                        target.ApplyCustomAttribute(kind, attribute, node.TargetSelector);
                        break;
                }
            }

            private bool ResolveConstructor(Analyzer/*!*/ analyzer)
            {
                if (!type.IsDefinite)
                {
                    // attribute type has to be known definitely:
                    analyzer.ErrorSink.Add(Errors.UnknownCustomAttribute, analyzer.SourceUnit, node.Span,
                      type.FullName, type.FullName + "Attribute");

                    return false;
                }

                if (!type.IsCustomAttributeType)
                {
                    analyzer.ErrorSink.Add(Errors.NotCustomAttributeClass, analyzer.SourceUnit, node.Span, type.FullName);
                    return false;
                }

                // resolve ctor overload in global context (only public ctors are visible):
                bool check_visibility;
                DRoutine constructor = analyzer.ResolveConstructor(type, node.Span, null, null, out check_visibility);
                Debug.Assert(!check_visibility);

                if (constructor.ResolveOverload(analyzer, node.CallSignature, node.Span, out overload) == DRoutine.InvalidOverloadIndex)
                {
                    analyzer.ErrorSink.Add(Errors.ClassHasNoVisibleCtor, analyzer.SourceUnit, node.Span, type.FullName);
                    return false;
                }

                return true;
            }

            private void AnalyzeActualArguments(Analyzer/*!*/ analyzer, out bool allEvaluable)
            {
                node.CallSignature.Analyze(analyzer, overload, ExInfoFromParent.DefaultExInfo, false);

                allEvaluable = true;

                // parameters:
                foreach (ActualParam param in node.CallSignature.Parameters)
                {
                    if (!param.Expression.IsCustomAttributeArgumentValue())
                    {
                        // expression has to be evaluable:
                        analyzer.ErrorSink.Add(Errors.InvalidAttributeExpression, analyzer.SourceUnit, param.Span);
                        allEvaluable = false;
                    }
                }

                // named parameters:
                foreach (NamedActualParam param in node.NamedParameters)
                {
                    param.Analyze(analyzer, type);

                    if (!param.Expression.IsCustomAttributeArgumentValue())
                    {
                        // expression has to be evaluable:
                        analyzer.ErrorSink.Add(Errors.InvalidAttributeExpression, analyzer.SourceUnit, param.Span);
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
                    analyzer.ErrorSink.Add(Errors.InvalidAttributeUsage, analyzer.SourceUnit, node.Span, node.QualifiedName.ToString());
                    return false;
                }

                // check duplicate usage of this attribute:
                if (!duplicateFound && (usage == null || !usage.AllowMultiple) && target.GetAttributeUsageCount(type, node.TargetSelector) > 1)
                {
                    analyzer.ErrorSink.Add(Errors.DuplicateAttributeUsage, analyzer.SourceUnit, node.Span, node.QualifiedName.ToString());
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

            public void Emit(CodeGenerator/*!*/ codeGen, IPhpCustomAttributeProvider/*!*/ target)
            {
                Emit(codeGen, target, false);
            }

            public void Emit(CodeGenerator/*!*/ codeGen, IPhpCustomAttributeProvider/*!*/ target, bool force)
            {
                // skip attributes that are not emitted:
                if (!isEmitted && !force) return;

                ConstructorInfo real_ctor;

                // TODO: type conversions (in analysis during overload resolution?)

                var parameters = node.CallSignature.Parameters;
                Type[] real_ctor_parameter_types = new Type[parameters.Length];

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
                object[] ctor_args = new object[parameters.Length];

                for (int i = 0; i < parameters.Length; i++)
                {
                    Expression expr = parameters[i].Expression;

                    TypeOfEx type_of = expr as TypeOfEx;
                    if (type_of != null)
                    {
                        ctor_args[i] = type_of.ClassNameRef.ResolvedType().RealType;
                    }
                    else
                    {
                        ctor_args[i] = ConvertToClr.ObjectToType(expr.GetValue(), real_ctor_parameter_types[i]);
                    }
                }

                List<FieldInfo> fields = new List<FieldInfo>();
                List<PropertyInfo> properties = new List<PropertyInfo>();
                List<object> field_values = new List<object>();
                List<object> prop_values = new List<object>();

                foreach (NamedActualParam param in node.NamedParameters)
                {
                    object value = param.Expression.GetValue();

                    MemberInfo real_member = param.GetProperty().RealMember;
                    FieldInfo real_field;
                    PropertyInfo real_property;

                    if ((real_property = real_member as PropertyInfo) != null ||    // regular CLR property
                        (param.GetProperty() is PhpField && (real_property = ((PhpField)param.GetProperty()).ExportedProperty) != null))  // or PHP property (real field below is PhpReference, we have to use its export stub)
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

                switch (node.TargetSelector)
                {
                    case CustomAttribute.TargetSelectors.Assembly:
                    case CustomAttribute.TargetSelectors.Module:
                        codeGen.CompilationUnit.ModuleBuilder.AssemblyBuilder.EmitCustomAttribute(builder, node.TargetSelector);
                        break;

                    default:
                        target.EmitCustomAttribute(builder, node.TargetSelector);
                        break;
                }
            }

            #endregion
        }

        #endregion
    }
}
