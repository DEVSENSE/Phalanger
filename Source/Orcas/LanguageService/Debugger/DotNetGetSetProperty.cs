//****************************************************************************
//
//    Copyright (c) Microsoft Corporation. All rights reserved.
//    This code is licensed under the Visual Studio SDK license terms.
//    THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
//    ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
//    IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
//    PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//****************************************************************************
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.Debugger.Interop;
using System.Diagnostics;

namespace PHP.VisualStudio.PhalangerLanguageService.Debugger
{
    /// <summary>
    /// Wrapper around the display of a get/set DotNet property.  Not to be confused
    /// with a Debugger Property (IDebugProperty2).  
    /// </summary>
    internal class DotNetGetSetProperty : PropertyBase
    {
        private ObjectInfo m_noEvalInfo;
        private ObjectInfo m_propInfo;
        private EEDebugMethodField m_propGetterField;
        private bool m_propEvalAttempted;

        private DotNetGetSetProperty(
            PropertyContext context, 
            EEDebugField baseField, 
            EEDebugObject baseObject, 
            EEDebugField typeField, 
            EEDebugMethodField propGetterField)
            :base(context, baseField, baseObject, typeField)
        {
            m_noEvalInfo = new ObjectInfo(context.DebuggerContext, baseObject, typeField);
            m_propGetterField = propGetterField;
        }

        protected override string GetPropertyNameImpl(DebuggerRequestInfo req)
        {
            FIELD_INFO info;
            if (NativeMethods.Succeeded(BaseField.GetInfo(enum_FIELD_INFO_FIELDS.FIF_NAME, out info)))
            {
                return info.bstrName;
            }

            return String.Empty;
        }

        protected override string GetPropertyTypeImpl(DebuggerRequestInfo req)
        {
            return m_noEvalInfo.GetDisplayStaticTypeName(req);
        }

        protected override string GetPropertyValueImpl(DebuggerRequestInfo req)
        {
            if (!req.IsFuncEvalAllowed)
            {
                return "Func Eval is disabled at this time";
            }

            EnsurePropretyEvaluationAttempted(req);
            if (m_propInfo == null)
            {
                return "Property evaluation failed";
            }

            return m_propInfo.GetDisplayValue(req);
        }

        protected override Constants GetPropertyAttribImpl(DebuggerRequestInfo req)
        {
            Constants attrib = Constants.DBG_ATTRIB_PROPERTY;
            if (!req.IsFuncEvalAllowed && 0 != (req.Flags & enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_VALUE))
            {
                attrib |= Constants.DBG_ATTRIB_VALUE_SIDE_EFFECT;
                return attrib;
            }

            EnsurePropretyEvaluationAttempted(req);
            if (m_propInfo == null)
            {
                // Couldn't get a value for the property.  
                attrib |= Constants.DBG_ATTRIB_VALUE_ERROR;
                attrib |= Constants.DBG_ATTRIB_VALUE_SIDE_EFFECT;
                return attrib;
            }

            // Evaluation succeeded, calculate the result based on this value
            attrib |= m_propInfo.GetAttrib(req);
            return attrib;
        }

        protected override int EnumChildren(DebuggerRequestInfo req, Guid guidFilter, string pszNameFilter, out EnumDebugPropertyInfo ppEnum)
        {
            return m_propInfo.EnumChildren(
                this,
                req,
                guidFilter,
                pszNameFilter,
                out ppEnum);
        }

        private void EnsurePropretyEvaluationAttempted(DebuggerRequestInfo req)
        {
            if (m_propEvalAttempted)
            {
                return;
            }
            
            if (!req.IsFuncEvalAllowed)
            {
                return;
            }

            m_propEvalAttempted = true;
            EvaluatePropertyResult(req, out m_propInfo);
        }

        private int EvaluatePropertyResult(DebuggerRequestInfo req, out ObjectInfo disp)
        {
            DebuggerContext context = this.DebuggerContext;
            int hr;
            disp = null;

            // First bind the propery getter
            EEDebugObject propGetterObject;
            hr = context.Bind(BaseObject, m_propGetterField, out propGetterObject);
            if (NativeMethods.Failed(hr))
            {
                return hr;
            }

            // Get the function for the Getter
            IDebugFunctionObject2 propGetterFunction = propGetterObject.Object as IDebugFunctionObject2;
            if (propGetterFunction == null)
            {
                return HResult.E_NOINTERFACE;
            }

            // Need our parent for which we create the property off of
            EEDebugObject thisObject = PropertyContext.PropertyBase.BaseObject;

            // Do the evaluation
            IDebugObject[] argArray = new IDebugObject[1];
            IDebugObject propResult;
            argArray[0] = thisObject.Object;
            hr = propGetterFunction.Evaluate(
                argArray,
                new IntPtr(1),
                0,
                req.Timeout,
                out propResult);
            if (NativeMethods.Failed(hr))
            {
                return hr;
            }

            disp = new ObjectInfo(this.DebuggerContext, new EEDebugObject(propResult), TypeField);
            return HResult.S_OK;
        }

        /// <summary>
        /// Factory method to create the property.  
        /// </summary>
        /// <param name="context"></param>
        /// <param name="baseField"></param>
        /// <param name="baseObject"></param>
        /// <returns></returns>
        public static int Create(PropertyContext propContext, EEDebugField baseField, out DotNetGetSetProperty getSetProp)
        {
            Contract.ThrowIfNull(propContext);
            Contract.ThrowIfNull(baseField);
            Contract.ThrowIfFalse(baseField.IsProperty);

            try
            {
                getSetProp = Create(propContext, baseField);
            }
            catch (Exception ex)
            {
                Debug.Fail(ex.Message);
                getSetProp = null;
                return HResult.E_FAIL;
            }

            return HResult.S_OK;
        }

        private static DotNetGetSetProperty Create(PropertyContext propContext, EEDebugField baseField)
        {
            DebuggerContext context = propContext.DebuggerContext;

            // Without a parent object there is no way to evaluate the property
            if (propContext.PropertyBase == null)
            {
                throw new InvalidOperationException("No parent to bind to");
            }

            // Get the object for the property
            IDebugObject propObject;
            int hr = context.Binder.Bind(propContext.PropertyBase.BaseObject.Object, baseField.Field, out propObject);
            NativeMethods.ThrowOnFailure(hr);
            
            // Get the getter field
            IDebugPropertyField propField = (IDebugPropertyField)baseField.Field;
            IDebugMethodField propGetter;
            hr = propField.GetPropertyGetter(out propGetter);
            NativeMethods.ThrowOnFailure(hr);

            // Figure out the type of the getter
            IDebugField propType;
            IDebugField propType2;
            hr = propGetter.GetType(out propType);
            NativeMethods.ThrowOnFailure(hr);
            hr = propType.GetType(out propType2);
            NativeMethods.ThrowOnFailure(hr);

            return new DotNetGetSetProperty(
                propContext,
                baseField,
                new EEDebugObject(propObject),
                new EEDebugField(propType2),
                new EEDebugMethodField(propGetter));

        }
    }
}
