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
    internal class MemberProperty:PropertyBase {
        private ObjectInfo m_info;

        public MemberProperty(PropertyContext context, EEDebugField baseField, EEDebugObject baseObject, EEDebugField typeField)
            : base(context, baseField, baseObject, typeField){
            m_info = new ObjectInfo(context.DebuggerContext, baseObject, typeField);
        }

        protected override Constants GetPropertyAttribImpl(DebuggerRequestInfo req){
            Constants attrib = m_info.GetAttrib(req);
            FIELD_INFO info = this.BaseField.GetInfo(enum_FIELD_INFO_FIELDS.FIF_MODIFIERS);

            // Update the visibility of this member
            enum_FIELD_MODIFIERS mod = (enum_FIELD_MODIFIERS)info.dwModifiers;
            switch (enum_FIELD_MODIFIERS.FIELD_MOD_ACCESS_MASK & mod){
                case enum_FIELD_MODIFIERS.FIELD_MOD_ACCESS_PUBLIC:
                    attrib |= Constants.DBG_ATTRIB_ACCESS_PUBLIC;
                    break;
                case enum_FIELD_MODIFIERS.FIELD_MOD_ACCESS_PRIVATE:
                    attrib |= Constants.DBG_ATTRIB_ACCESS_PRIVATE;
                    break;
                case enum_FIELD_MODIFIERS.FIELD_MOD_ACCESS_PROTECTED:
                    attrib |= Constants.DBG_ATTRIB_ACCESS_PROTECTED;
                    break;
                case enum_FIELD_MODIFIERS.FIELD_MOD_ACCESS_FRIEND:
                    attrib |= Constants.DBG_ATTRIB_ACCESS_PROTECTED;
                    break;
            }
            return attrib;
        }

        protected override string GetPropertyNameImpl(DebuggerRequestInfo req){
            return BaseField.GetInfo(enum_FIELD_INFO_FIELDS.FIF_NAME).bstrName;
        }

        protected override string GetPropertyValueImpl(DebuggerRequestInfo req){
            return m_info.GetDisplayValue(req);
        }

        protected override string GetPropertyTypeImpl(DebuggerRequestInfo req)
        {
            return m_info.GetDisplayStaticTypeName(req);
        }

        protected override int EnumChildren(DebuggerRequestInfo req, Guid guidFilter, string pszNameFilter, out EnumDebugPropertyInfo ppEnum)
        {
            return m_info.EnumChildren(this, req, guidFilter, pszNameFilter, out ppEnum);
        }
        
    }
}
