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

namespace PHP.VisualStudio.PhalangerLanguageService.Debugger {
    internal class PropertyContext {
        private PropertyContext m_parent;
        private DebuggerContext m_context;
        private PropertyBase m_propertyBase;
        private EEDebugObject m_objectContext;

        public PropertyContext ParentContext { [DebuggerStepThrough]get { return m_parent; } }

        public DebuggerContext DebuggerContext { [DebuggerStepThrough]get { return m_context; } }

        public PropertyBase PropertyBase { [DebuggerStepThrough]get { return m_propertyBase; } }

        public EEDebugObject ObjectContext { [DebuggerStepThrough]get { return m_objectContext; } }

        public PropertyContext(DebuggerContext context) { m_context = context; }

        public PropertyContext(PropertyBase propBase) : this(propBase, propBase.BaseObject) { }

        public PropertyContext(PropertyBase propBase, EEDebugObject objectContext) {
            m_parent = propBase.PropertyContext;
            m_context = m_parent.DebuggerContext;
            m_propertyBase = propBase;
            m_objectContext = objectContext;
        }
    }

    /// <summary>
    /// Base functionality for the Property class
    /// </summary>
    [DebuggerDisplay("{FullName}")]
    internal abstract class PropertyBase:IDebugProperty2, IDebugProperty {
        private PropertyContext m_propContext;
        private DebuggerContext m_context;
        private EEDebugField m_baseField;
        private EEDebugObject m_baseObject;
        private EEDebugField m_typeField;

        internal PropertyWrapper GetWrapper() { return new PropertyWrapper(m_context, this, BaseObject.Object); }

        public PropertyContext PropertyContext { [DebuggerStepThrough]get { return m_propContext; } }

        public DebuggerContext DebuggerContext { [DebuggerStepThrough]get { return m_context; } }

        public EEDebugField BaseField { [DebuggerStepThrough]get { return m_baseField; } }

        public EEDebugObject BaseObject { [DebuggerStepThrough]get { return m_baseObject; } }

        public EEDebugField TypeField { [DebuggerStepThrough]get { return m_typeField; } }

        protected PropertyBase(PropertyContext context, EEDebugField baseField, EEDebugObject baseObject, EEDebugField typeField) {
            m_propContext = context;
            m_context = context.DebuggerContext;
            m_baseField = baseField;
            m_baseObject = baseObject;
            m_typeField = typeField;
        }

        #region IDebugProperty Members

        public int EnumMembers(uint dwFieldSpec, uint nRadix, ref Guid refiid, out IEnumDebugPropertyInfo ppepi) {
            throw new NotImplementedException();
        }

        public int GetExtendedInfo(uint cInfos, Guid[] rgguidExtendedInfo, object[] rgvar) {
            throw new NotImplementedException();
        }

        public int GetParent(out IDebugProperty ppDebugProp) {
            throw new NotImplementedException();
        }

        public int GetPropertyInfo(uint dwFieldSpec, uint nRadix, DebugPropertyInfo[] pPropertyInfo) {
            throw new NotImplementedException();
        }

        public int SetValueAsString(string pszValue, uint nRadix) {
            throw new NotImplementedException();
        }

        #endregion

        #region IDebugProperty2 Members

        public virtual int EnumChildren(uint dwFields, uint dwRadix, ref Guid guidFilter, ulong dwAttribFilter, string pszNameFilter, uint dwTimeout, out IEnumDebugPropertyInfo2 ppEnum) {
            EnumDebugPropertyInfo wrappedEnum;
            int hr = EnumChildren(
                new DebuggerRequestInfo(dwFields, dwRadix, dwTimeout),
                guidFilter,
                pszNameFilter,
                out wrappedEnum);
            ppEnum = wrappedEnum;
            return hr;
        }

        public int GetDerivedMostProperty(out IDebugProperty2 ppDerivedMost) {
            throw new NotImplementedException();
        }

        public int GetExtendedInfo(ref Guid guidExtendedInfo, out object pExtendedInfo) {
            throw new NotImplementedException();
        }

        public int GetMemoryBytes(out IDebugMemoryBytes2 ppMemoryBytes) {
            throw new NotImplementedException();
        }

        public int GetMemoryContext(out IDebugMemoryContext2 ppMemory) {
            throw new NotImplementedException();
        }

        public int GetParent(out IDebugProperty2 ppParent) {
            throw new NotImplementedException();
        }

        public int GetPropertyInfo(uint dwFields, uint dwRadix, uint dwTimeout, IDebugReference2[] rgpArgs, uint dwArgCount, DEBUG_PROPERTY_INFO[] pPropertyInfo) {
            DEBUG_PROPERTY_INFO info;
            int ret = GetPropertyInfo(
                new DebuggerRequestInfo(dwFields, dwRadix, dwTimeout),
                out info);
            pPropertyInfo[0] = info;
            return ret;
        }

        public int GetReference(out IDebugReference2 ppReference) {
            throw new NotImplementedException();
        }

        public int GetSize(out uint pdwSize) {
            throw new NotImplementedException();
        }

        public int SetValueAsReference(IDebugReference2[] rgpArgs, uint dwArgCount, IDebugReference2 pValue, uint dwTimeout) {
            throw new NotImplementedException();
        }

        public int SetValueAsString(string pszValue, uint dwRadix, uint dwTimeout) {
            throw new NotImplementedException();
        }

        #endregion

        public virtual int GetPropertyInfo(DebuggerRequestInfo req, out DEBUG_PROPERTY_INFO retInfo) {
            DEBUG_PROPERTY_INFO info = new DEBUG_PROPERTY_INFO();
            bool anyFailed = false;
            if(req.RequestName && !GetPropertyName(req, ref info)) {
                anyFailed = true;
            }

            if(req.RequestFullName && !GetPropertyFullName(req, ref info)) {
                anyFailed = true;
            }

            if(req.RequestAttrib && !GetPropertyAttrib(req, ref info)) {
                anyFailed = true;
            }

            if(req.RequestType && !GetPropertyType(req, ref info)) {
                anyFailed = true;
            }

            if(req.RequestValue && !GetPropertyValue(req, ref info)) {
                anyFailed = true;
            }

            if(req.RequestProperty) {
                info.pProperty = this;
                info.dwFields |= (uint)enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_PROP;
            }

            retInfo = info;
            if(anyFailed) {
                if(0 == info.dwFields) {
                    return HResult.E_FAIL;
                } else {
                    return HResult.S_FALSE;
                }
            }

            return HResult.S_OK;
        }

        private bool GetPropertyName(DebuggerRequestInfo req, ref DEBUG_PROPERTY_INFO info) {
            try {
                info.bstrName = GetPropertyNameImpl(req);
                info.dwFields |= (uint)enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_NAME;
                return true;
            } catch(Exception ex) {
                Debug.Fail(ex.Message);
                return false;
            }
        }

        protected virtual string GetPropertyNameImpl(DebuggerRequestInfo req) {
            FIELD_INFO info = m_baseField.GetInfo(enum_FIELD_INFO_FIELDS.FIF_NAME);
            return info.bstrName;
        }
        public string Name { get { return GetPropertyNameImpl(null); } }


        private bool GetPropertyFullName(DebuggerRequestInfo req, ref DEBUG_PROPERTY_INFO info) {
            try {
                info.bstrFullName = GetPropertyFullNameImpl(req);
                info.dwFields |= (uint)enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_FULLNAME;
                return true;
            } catch(Exception ex) {
                Debug.Fail(ex.Message);
                return false;
            }
        }

        protected virtual string GetPropertyFullNameImpl(DebuggerRequestInfo req) {
            FIELD_INFO info = m_baseField.GetInfo(enum_FIELD_INFO_FIELDS.FIF_FULLNAME);
            return info.bstrFullName;
        }
        public string FullName { get { return GetPropertyFullNameImpl(null); } }

        private bool GetPropertyValue(DebuggerRequestInfo req, ref DEBUG_PROPERTY_INFO info) {
            try {
                info.bstrValue = GetPropertyValueImpl(req);
                info.dwFields |= (uint)enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_VALUE;
                return true;
            } catch(Exception ex) {
                Debug.Fail(ex.Message);
                return false;
            }
        }

        protected abstract string GetPropertyValueImpl(DebuggerRequestInfo req);

        private bool GetPropertyType(DebuggerRequestInfo req, ref DEBUG_PROPERTY_INFO info) {
            try {
                info.bstrType = GetPropertyTypeImpl(req);
                info.dwFields |= (uint)enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_TYPE;
                return true;
            } catch(Exception ex) {
                Debug.Fail(ex.Message);
                return false;
            }
        }

        protected abstract string GetPropertyTypeImpl(DebuggerRequestInfo req);

        private bool GetPropertyAttrib(DebuggerRequestInfo req, ref DEBUG_PROPERTY_INFO info) {
            try {
                info.dwAttrib = (ulong)GetPropertyAttribImpl(req);
                info.dwFields |= (uint)enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_ATTRIB;
                return true;
            } catch(Exception ex) {
                Debug.Fail(ex.Message);
                return false;
            }
        }

        protected abstract Constants GetPropertyAttribImpl(DebuggerRequestInfo info);

        protected abstract int EnumChildren(DebuggerRequestInfo req, Guid filter, string pszNameFilter, out EnumDebugPropertyInfo ppEnum);
        public EnumDebugPropertyInfo GetChildren(DebuggerRequestInfo req, Guid filter, string pszNameFilter) {
            EnumDebugPropertyInfo ppEnum;
            NativeMethods.ThrowOnFailure(EnumChildren(req, filter, pszNameFilter, out ppEnum));
            return ppEnum;
        }
    }
}
