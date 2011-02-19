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
    internal class LocalOrParamProperty : PropertyBase{
        private ObjectInfo m_info;

        public LocalOrParamProperty(PropertyContext context, EEDebugField baseField, EEDebugObject baseObject, EEDebugField typeField)
            : base(context, baseField, baseObject, typeField){
            m_info = new ObjectInfo(context.DebuggerContext, baseObject, typeField);
        }

        /// <summary>
        /// Whenever we have a local value that is not the derived most type, we want to show
        /// and expandable field where the child is just a ObjectProperty with the derived 
        /// most information
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        protected override Constants GetPropertyAttribImpl(DebuggerRequestInfo req){
            return m_info.GetAttrib(req);
        }

        protected override string GetPropertyNameImpl(DebuggerRequestInfo req){
            return BaseField.GetInfo(enum_FIELD_INFO_FIELDS.FIF_NAME).bstrName;
        }

        protected override string GetPropertyTypeImpl(DebuggerRequestInfo req){
            return m_info.GetDisplayStaticTypeName(req);
        }

        protected override string GetPropertyValueImpl(DebuggerRequestInfo req){
            return m_info.GetDisplayValue(req);
        }

        protected override int EnumChildren(DebuggerRequestInfo req, Guid guidFilter, string pszNameFilter, out EnumDebugPropertyInfo ppEnum){
            return m_info.EnumChildren(this, req, guidFilter, pszNameFilter, out ppEnum);
        }

    }
}
