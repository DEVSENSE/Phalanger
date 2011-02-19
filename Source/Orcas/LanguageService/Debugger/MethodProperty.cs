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

namespace PHP.VisualStudio.PhalangerLanguageService.Debugger{
    /// <summary>
    /// IDebugProperty2 for methods. 
    /// </summary>
    internal class MethodProperty:PropertyBase {
        private EEDebugMethodField m_methodField;

        public MethodProperty(PropertyContext context, EEDebugField baseField, EEDebugObject baseObject, EEDebugField typeField)
            : base(context, baseField, baseObject, typeField){
            m_methodField = new EEDebugMethodField(baseField.Field);
        }

        protected override string GetPropertyValueImpl(DebuggerRequestInfo req){
            FIELD_INFO info = this.BaseField.GetInfo(enum_FIELD_INFO_FIELDS.FIF_FULLNAME);
            return info.bstrFullName;
        }

        protected override string GetPropertyTypeImpl(DebuggerRequestInfo req){
            EEDebugField type;
            NativeMethods.ThrowOnFailure(BaseField.GetType(out type));

            FIELD_INFO info = type.GetInfo(enum_FIELD_INFO_FIELDS.FIF_FULLNAME);
            return info.bstrFullName;
        }

        protected override Constants GetPropertyAttribImpl(DebuggerRequestInfo info){
            Constants flags = 0;
            flags |= Constants.DBG_ATTRIB_METHOD;
            flags |= Constants.DBG_ATTRIB_OBJ_IS_EXPANDABLE;
            return flags;
        }

        protected override int EnumChildren(DebuggerRequestInfo req, Guid guidFilter, string pszNameFilter, out EnumDebugPropertyInfo ppEnum){
            int hr;
            IEnumDebugFields enumFields;

            if (guidFilter == FilterGuids.Locals)
            {
                hr = m_methodField.MethodField.EnumLocals(this.DebuggerContext.Address, out enumFields);
            }
            else if (guidFilter == FilterGuids.AllLocals)
            {
                hr = m_methodField.MethodField.EnumAllLocals(this.DebuggerContext.Address, out enumFields);
            }
            else if (guidFilter == FilterGuids.Args)
            {
                hr = m_methodField.MethodField.EnumParameters(out enumFields);
            }
            else if (guidFilter == FilterGuids.LocalsPlusArgs)
            {
                IEnumDebugFields enumArgs;
                hr = m_methodField.MethodField.EnumLocals(this.DebuggerContext.Address, out enumFields);
                if (NativeMethods.Failed(hr))
                {
                    ppEnum = null;
                    return hr;
                }

                hr = m_methodField.MethodField.EnumParameters(out enumArgs);
                if (NativeMethods.Failed(hr))
                {
                    ppEnum = null;
                    return hr;
                }

                EnumDebugFields combined;
                hr = EnumDebugFields.Create(out combined, enumFields, enumArgs);
                if (NativeMethods.Failed(hr))
                {
                    ppEnum = null;
                    return hr;
                }

                return EnumDebugPropertyInfo.CreateForFieldEnum(
                    req,
                    new PropertyContext(this),
                    combined,
                    out ppEnum);
            }
            else
            {
                hr = HResult.E_FAIL;
                enumFields = null;
            }

            if (enumFields == null && NativeMethods.Succeeded(hr))
            {
                hr = HResult.E_FAIL;
            }   

            if (NativeMethods.Failed(hr))
            {
                ppEnum = null;
                return hr;
            }

            return EnumDebugPropertyInfo.CreateForFieldEnum(
                req,
                new PropertyContext(this),
                enumFields,
                out ppEnum);
        }
    }
}
