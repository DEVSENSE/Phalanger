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

namespace PHP.VisualStudio.PhalangerLanguageService.Debugger
{
    /// <summary>
    /// Used to display non-symbol based values in the Debugger.  
    /// </summary>
    internal class ObjectProperty:PropertyBase
    {
        private ObjectInfo m_info;

        public ObjectProperty(PropertyContext context, EEDebugField baseField, EEDebugObject baseObject, EEDebugField typeField)
            :base(context, baseField, baseObject, typeField)
        {
            m_info = new ObjectInfo(context.DebuggerContext, baseObject, typeField);
        }

        protected override string GetPropertyNameImpl(DebuggerRequestInfo req)
        {
            return m_info.GetDisplayStaticTypeName(req);
        }

        protected override Constants GetPropertyAttribImpl(DebuggerRequestInfo req)
        {
            return m_info.GetAttrib(req);
        }

        protected override string GetPropertyValueImpl(DebuggerRequestInfo req)
        {
            return m_info.GetDisplayValue(req);
        }

        protected override string GetPropertyTypeImpl(DebuggerRequestInfo req)
        {
            return m_info.GetStaticTypeName(req);
        }
        /// <summary>
        /// When enumerating the children of an Ojbect/Type we want to show the fields and properties
        /// </summary>
        /// <param name="req"></param>
        /// <param name="guidFilter"></param>
        /// <param name="pszNameFilter"></param>
        /// <param name="ppEnum"></param>
        /// <returns></returns>
        protected override int EnumChildren(DebuggerRequestInfo req, Guid guidFilter, string pszNameFilter, out EnumDebugPropertyInfo ppEnum)
        {
            return m_info.EnumChildren(
                this,
                req,
                guidFilter,
                pszNameFilter,
                out ppEnum);
        }


    }
}
