using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Dynamic;
using PHP.Core.Reflection;
using System.Linq.Expressions;
using System.Reflection;

namespace PHP.Core.Binders
{
    using PHP.Core.Emit;

    /// <summary>
    /// 
    /// </summary>
    public sealed class PhpGetMemberBinder : DynamicMetaObjectBinder
    {
        #region Fields

        protected readonly string _fieldName;
        protected readonly DTypeDesc _classContext;
        protected readonly bool _issetSemantics;
        protected readonly Type/*!*/_returnType;

        #endregion

        #region Properties

        protected bool ClassContextIsKnown { get { return _classContext == null || !_classContext.IsUnknown; } }
        protected bool IsIndirect { get { return _fieldName == null; } }
        protected bool WantReference { get { return this._returnType == Types.PhpReference[0]; } }
        
        #endregion

        public PhpGetMemberBinder(string fieldName, DTypeDesc classContext, bool issetSemantics, Type/*!*/returnType)
        {
            this._fieldName = fieldName;
            this._classContext = classContext;
            this._issetSemantics = issetSemantics;
            this._returnType = returnType;
        }

        public override Type ReturnType { get { return this._returnType; } }

        public override DynamicMetaObject Bind(DynamicMetaObject target, DynamicMetaObject[] args)
        {
            if (target.Value is IDynamicMetaObjectProvider)
            {
                throw new NotImplementedException();
                //Translate arguments to DLR standard
                //TODO: Create DlrCompatibilityInvokeBinder because it has to be derived from InvokeMemberBinder
                //return target.BindInvokeMember(this,args);
            }

            return FallbackInvokeMember(target, args);
        }

        private DynamicMetaObject/*!*/ FallbackInvokeMember(DynamicMetaObject target/*!*/, DynamicMetaObject/*!*/[]/*!*/ args)
        {
            // determine run time values and additional restrictions:
            DTypeDesc classContext = this._classContext;
            string fieldName = this._fieldName;
            BindingRestrictions restrictions = BindingRestrictions.GetTypeRestriction(target.Expression, target.LimitType); //target.Restrictions;

            int currentArg = 0;
            if (!ClassContextIsKnown)
            {
                Debug.Assert(args.Length > currentArg, "Not enough arguments!");
                Debug.Assert(args[currentArg].Value == null || Types.DTypeDesc[0].IsAssignableFrom(args[currentArg].LimitType), "Wrong class context type!");
                classContext = (DTypeDesc)args[currentArg].Value;
                Debug.Assert(classContext == null || !classContext.IsUnknown, "Class context should be known at run time!");

                restrictions = restrictions.Merge(BindingRestrictions.GetInstanceRestriction(args[currentArg].Expression, classContext));
                
                currentArg++;
            }
            if (IsIndirect)
            {
                Debug.Assert(args.Length > currentArg, "Not enough arguments!");
                Debug.Assert(Types.String[0].IsAssignableFrom(args[currentArg].LimitType), "Wrong field name type!");
                fieldName = (string)args[currentArg].Value;

                restrictions = restrictions.Merge(
                    BindingRestrictions.GetExpressionRestriction(
                        Expression.Equal(
                            args[currentArg].Expression,
                            Expression.Constant(fieldName, Types.String[0]))));

                currentArg++;
            }

            // 
            ////Debug.Assert(!(var is PhpReference) && name != null);
            Debug.Assert(target.HasValue && target.LimitType != Types.PhpReference[0], "Target should not be PhpReference!");

            DObject obj;
            ////// a property of a DObject:
            if ((obj = target.Value as DObject) != null)
            {
                if (obj is ClrObject /*|| obj is IClrValue // IClrValue -> ClrValue<T> -> already in restriction */)
                {
                    // ((DObject)target).RealType == <obj>.RealType
                    restrictions.Merge(
                        BindingRestrictions.GetInstanceRestriction(
                            Expression.Property(Expression.Convert(target.Expression, Types.DObject[0]), Properties.DObject_RealType),
                            obj.RealType));
                }

                ////    return GetObjectProperty(obj, name, caller, quiet);
                DPropertyDesc property;
                GetMemberResult result = obj.TypeDesc.GetInstanceProperty(new VariableName(fieldName), classContext, out property);

                switch (result)
                {
                    case GetMemberResult.OK:
                        ////object value = property.Get(this);
                        ////PhpReference reference = value as PhpReference;

                        if (property.Member is PhpField || property.Member is PhpVisibleProperty)
                        {
                            var realType = property.DeclaringType.RealType;
                            FieldInfo realField = (property.Member is PhpField) ? property.PhpField.RealField : null;
                            PropertyInfo realProperty = (property.Member is PhpVisibleProperty) ? ((PhpVisibleProperty)property.Member).RealProperty : null;

                            Debug.Assert(realField != null ^ realProperty != null);

                            MemberExpression getter = null;

                            if (realField != null)
                                getter = Expression.Field(Expression.Convert(target.Expression, realType), realField);
                            else if (realProperty != null)
                                getter = Expression.Property(Expression.Convert(target.Expression, realType), realProperty);


                            if (Types.PhpReference[0].IsAssignableFrom(getter.Type))
                            {
                                var reference = Expression.Variable(Types.PhpReference[0]);
                                var assignment = Expression.Assign(reference, getter);

                                if (WantReference)
                                {
                                    ////value = property.Get(this);
                                    ////reference = value as PhpReference;

                                    var returnLabel = Expression.Label(this._returnType);

                                    ////if (reference != null && reference.IsSet)
                                    ////{
                                    ////    reference.IsAliased = true;
                                    ////    return reference;
                                    ////}

                                    var isset = Expression.IfThen(
                                        Expression.Property(assignment, Properties.PhpReference_IsSet),
                                        Expression.Block(
                                            Expression.Assign(Expression.Property(reference, Properties.PhpReference_IsAliased), Expression.Constant(true)),
                                            Expression.Return(returnLabel, reference)));

                                    ////// the CT property has been unset -> try to invoke __get
                                    ////PhpReference get_ref = InvokeGetterRef(name, caller, out getter_exists);
                                    ////if (getter_exists) return (get_ref == null ? new PhpReference() : get_ref);

                                    ////if (reference == null)
                                    ////{
                                    ////    reference = new PhpReference(value);
                                    ////    property.Set(this, reference);
                                    ////}
                                    ////else
                                    ////{
                                    ////    reference.IsAliased = true;
                                    ////    reference.IsSet = true;
                                    ////}
                                    Func<DObject, string, DTypeDesc, PhpReference, PhpReference> notsetOperation = (self, name, caller, refrnc) =>
                                        {
                                            bool getter_exists;
                                            // the CT property has been unset -> try to invoke __get
                                            PhpReference get_ref = self.InvokeGetterRef(name, caller, out getter_exists);
                                            if (getter_exists) return get_ref ?? new PhpReference();

                                            Debug.Assert(refrnc != null);

                                            refrnc.IsAliased = true;
                                            refrnc.IsSet = true;

                                            return refrnc;
                                        };

                                    ////return reference;

                                    return new DynamicMetaObject(
                                        Expression.Block(this._returnType,
                                            new[]{reference},
                                            new Expression[]{
                                                isset,
                                                Expression.Label(returnLabel,
                                                    Expression.Call(null, notsetOperation.Method, Expression.Convert(target.Expression, Types.DObject[0]), Expression.Constant(fieldName), Expression.Constant(classContext, Types.DTypeDesc[0]), reference))
                                            }),
                                            restrictions);
                                }
                                else
                                {
                                    ////if (reference != null && !reference.IsSet)
                                    ////{
                                    ////    // the property is CT but has been unset
                                    ////    if (issetSemantics)
                                    ////    {
                                    ////        bool handled;
                                    ////        return PropertyIssetHandler(name, caller, out handled);
                                    ////    }
                                    ////    else return GetRuntimeField(name, caller);
                                    ////}
                                    ////else return value;


                                    Func<DObject, string, DTypeDesc, object> notsetOperation;
                                    if (_issetSemantics) notsetOperation = (self, name, caller) =>
                                        {
                                            return PhpVariable.Dereference(self.GetRuntimeField(name, caller));
                                        };
                                    else notsetOperation = (self, name, caller) =>
                                        {
                                            bool handled;
                                            return PhpVariable.Dereference(self.PropertyIssetHandler(name, caller, out handled));
                                        };
                                    var value =
                                        Expression.Block(this._returnType,
                                            new[] { reference },
                                            Expression.Condition(
                                                Expression.Property(assignment, Properties.PhpReference_IsSet),
                                                Expression.Field(reference, Fields.PhpReference_Value),
                                                Expression.Call(null, notsetOperation.Method, Expression.Convert(target.Expression, Types.DObject[0]), Expression.Constant(fieldName), Expression.Constant(classContext, Types.DTypeDesc[0]))
                                        ));

                                    return new DynamicMetaObject(value, restrictions);
                                }
                            }
                            else
                            {
                                if (WantReference)
                                {
                                    return new DynamicMetaObject(
                                        Expression.New(Constructors.PhpReference_Object, Expression.Convert(getter, Types.Object[0])),
                                        restrictions);
                                }
                                else
                                {
                                    return new DynamicMetaObject(
                                        Expression.Call(Methods.PhpVariable.Dereference, Expression.Convert(getter, Types.Object[0])),
                                        restrictions);
                                }
                            }
                        }
                        else if (property.Member is ClrProperty)
                        {
                            var realType = property.DeclaringType.RealType;
                            var realProperty = property.ClrProperty.RealProperty;

                            // (target.{RealObject|realValue}).<realProperty>
                            Expression value = Expression.Convert(
                                            BinderHelper.ClrObjectWrapDynamic(
                                                Expression.Property(
                                                    BinderHelper.ClrRealObject(target, realType),
                                                    realProperty)),
                                            Types.Object[0]);

                            if (WantReference) value = BinderHelper.MakePhpReference(value);

                            return new DynamicMetaObject(value, restrictions);
                        }
                        else if (property.Member is ClrField)
                        {
                            var realType = property.DeclaringType.RealType;
                            var realField = property.ClrField.FieldInfo;

                            // (target.{RealObject|realValue}).<realField>
                            Expression value = Expression.Convert(
                                            BinderHelper.ClrObjectWrapDynamic(
                                                Expression.Field(
                                                    BinderHelper.ClrRealObject(target, realType),
                                                    realField)),
                                            Types.Object[0]);

                            if (WantReference) value = BinderHelper.MakePhpReference(value);

                            return new DynamicMetaObject(value, restrictions);
                        }
                        else
                            throw new NotImplementedException();

                    case GetMemberResult.NotFound:
                        if (WantReference)
                        {
                            Func<DObject, string, DTypeDesc, PhpReference> op = (self, name, caller) =>
                            {
                                PhpReference reference;
                                object value;
                                bool getter_exists;

                                // search in RT fields
                                OrderedHashtable<string>.Element element;
                                if (self.RuntimeFields != null && (element = self.RuntimeFields.GetElement(name)) != null)
                                {
                                    value = element.Value;
                                    reference = value as PhpReference;

                                    if (reference == null)
                                    {
                                        // it is correct to box the value without making a deep copy since there was a single pointer on value
                                        // before this operation (by invariant) and there will be a single one after the operation as well:
                                        reference = new PhpReference(value);
                                        element.Value = reference;
                                    }

                                    return reference;
                                }

                                // property is not present -> try to invoke __get
                                reference = self.InvokeGetterRef(name, caller, out getter_exists);
                                if (getter_exists) return (reference == null) ? new PhpReference() : reference;

                                // (no notice/warning/error thrown by PHP)

                                // add the field
                                reference = new PhpReference();
                                if (self.RuntimeFields == null) self.RuntimeFields = new OrderedHashtable<string>();
                                self.RuntimeFields[name] = reference;

                                return reference;
                            };

                            return new DynamicMetaObject(
                                Expression.Call(null, op.Method, Expression.Convert(target.Expression, Types.DObject[0]), Expression.Constant(fieldName), Expression.Constant(classContext, Types.DTypeDesc[0])),
                                restrictions);
                        }
                        else
                        {
                            ////if (issetSemantics)
                            ////{
                            ////    OrderedHashtable<string>.Element element;
                            ////    if (RuntimeFields != null && (element = RuntimeFields.GetElement(name)) != null)
                            ////    {
                            ////        return element.Value;
                            ////    }
                            ////    else
                            ////    {
                            ////        bool handled;
                            ////        return PropertyIssetHandler(name, caller, out handled);
                            ////    }
                            ////}
                            ////else return GetRuntimeField(name, caller);

                            if (_issetSemantics)
                            {
                                Func<DObject, string, DTypeDesc, object> notsetOperation = (self, name, caller) =>
                                {
                                    OrderedHashtable<string>.Element element;
                                    if (self.RuntimeFields != null && (element = self.RuntimeFields.GetElement(name)) != null)
                                        return element.Value;

                                    bool handled;
                                    return self.PropertyIssetHandler(name, caller, out handled);
                                };

                                return new DynamicMetaObject(
                                    Expression.Call(Methods.PhpVariable.Dereference,
                                        Expression.Call(null, notsetOperation.Method, Expression.Convert(target.Expression, Types.DObject[0]), Expression.Constant(fieldName), Expression.Constant(classContext, Types.DTypeDesc[0]))),
                                    restrictions);
                            }
                            else
                            {
                                return new DynamicMetaObject(
                                    Expression.Call(
                                        Methods.PhpVariable.Dereference,
                                        Expression.Call(
                                            Expression.Convert(target.Expression, Types.DObject[0]),
                                            Methods.DObject_GetRuntimeField, Expression.Constant(fieldName), Expression.Constant(classContext, Types.DTypeDesc[0]))),
                                    restrictions);
                            };
                            
                            
                        }
                    case GetMemberResult.BadVisibility:
                        {
                            ////PhpException.PropertyNotAccessible(
                            ////    property.DeclaringType.MakeFullName(),
                            ////    name.ToString(),
                            ////    (caller == null ? String.Empty : caller.MakeFullName()),
                            ////    property.IsProtected);

                            string stringResourceKey = property.IsProtected ? "protected_property_accessed" : "private_property_accessed";

                            return new DynamicMetaObject(
                                Expression.Block(this._returnType,
                                    Expression.Call(null, Methods.PhpException.Throw,
                                        Expression.Constant(PhpError.Error, Types.PhpError_String[0]),
                                        Expression.Constant(CoreResources.GetString(stringResourceKey, property.DeclaringType.MakeFullName(), fieldName, (classContext == null ? String.Empty : classContext.MakeFullName())))),
                                    WantReference ? (Expression)Expression.New(Constructors.PhpReference_Void) : Expression.Constant(null)
                                    ),
                                restrictions);
                        }
                }
            }

            ////// warnings:
            ////if (!quiet) // not in isset() operator only
            ////{
            if (!_issetSemantics)
            {
                ////    if (PhpVariable.IsEmpty(var))
                ////        // empty:
                ////        PhpException.Throw(PhpError.Notice, CoreResources.GetString("empty_used_as_object"));
                ////    else
                ////        // PhpArray, string, scalar type:
                ////        PhpException.VariableMisusedAsObject(var, false);
                
                Action<object> error = (var) =>
                {
                    if (PhpVariable.IsEmpty(var))
                        // empty:
                        PhpException.Throw(PhpError.Notice, CoreResources.GetString("empty_used_as_object"));
                    else
                        // PhpArray, string, scalar type:
                        PhpException.VariableMisusedAsObject(var, false);
                };

                return new DynamicMetaObject(
                    Expression.Block(this._returnType,
                        Expression.Call(error.Method, target.Expression),
                        WantReference ? (Expression)Expression.New(Constructors.PhpReference_Void) : Expression.Constant(null)),
                    (target.HasValue && target.Value == null) ?
                        BindingRestrictions.GetInstanceRestriction(target.Expression, null) :
                        BindingRestrictions.GetTypeRestriction(target.Expression, target.LimitType));
            }
            ////}
            
            ////// property does not exist
            ////return null;
            return new DynamicMetaObject(
                Expression.Constant(null),
                (target.HasValue && target.Value == null) ?
                    BindingRestrictions.GetInstanceRestriction(target.Expression, null) :
                    BindingRestrictions.GetTypeRestriction(target.Expression, target.LimitType));
        }
    }
}
