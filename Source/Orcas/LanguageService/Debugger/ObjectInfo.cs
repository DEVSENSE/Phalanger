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

namespace PHP.VisualStudio.PhalangerLanguageService.Debugger{
    /// <summary>
    /// Helper class that is used to display objects in the Debugger.
    /// </summary>
    internal class ObjectInfo {
        private const enum_FIELD_KIND sChildKind =
            enum_FIELD_KIND.FIELD_SYM_THIS | enum_FIELD_KIND.FIELD_SYM_MEMBER |
            enum_FIELD_KIND.FIELD_TYPE_ARRAY | enum_FIELD_KIND.FIELD_TYPE_CLASS |
            enum_FIELD_KIND.FIELD_TYPE_ENUM | enum_FIELD_KIND.FIELD_TYPE_PRIMITIVE |
            enum_FIELD_KIND.FIELD_TYPE_PROP | enum_FIELD_KIND.FIELD_TYPE_STRUCT;

        private enum ValueType{
            Int32,
            String,
            Unknown
        }

        private DebuggerContext m_context;
        private EEDebugObject m_object;
        private EEDebugField m_typeField;
        private ValueType? m_valueType;

        public EEDebugObject Object{
           [DebuggerStepThrough] get { return m_object; }
        }

        internal ObjectWrapper GetWrapper() { return new ObjectWrapper(m_context, Object.Object); }

        public EEDebugField Type {
            [DebuggerStepThrough] get { return m_typeField; }
        }

        public enum_FIELD_KIND Kind{
            [DebuggerStepThrough] get { return m_typeField.Kind; }
        }

        public ObjectInfo(DebuggerContext context, EEDebugObject obj, EEDebugField typeField)        {
             Contract.ThrowIfTrue(typeField.IsSymbol);   // Only types are supported here
            m_context = context;
            m_object = obj;
            m_typeField = typeField;
        }

        /// <summary>
        /// Get the raw static type name for this type.  Does no transformations on the 
        /// value to be more user friendly
        /// </summary>
        /// <param name="req"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public int GetStaticTypeName(DebuggerRequestInfo req, out string type){
            FIELD_INFO info;
            int hr = m_typeField.GetInfo(enum_FIELD_INFO_FIELDS.FIF_FULLNAME, out info);
            if (NativeMethods.Failed(hr))
            {
                type = null;
                return hr;
            }

            type = info.bstrFullName;
            return HResult.S_OK;
        }

        public string GetStaticTypeName() { return GetStaticTypeName(null); }

        public string GetStaticTypeName(DebuggerRequestInfo req){
            string name;
            int hr = GetStaticTypeName(req, out name);
            NativeMethods.ThrowOnFailure(hr);
            return name;
        }

        /// <summary>
        /// Get the string that should be displayed in the type field for this object based
        /// on the static type of the object.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public int GetDisplayStaticTypeName(DebuggerRequestInfo req, out string type){
            EnsureValueType();
            if (m_valueType != ValueType.Unknown)
            {
                switch (m_valueType)
                {
                    case ValueType.Int32:
                        type = typeof(Int32).FullName;
                        break;
                    case ValueType.String:
                        type = "String";
                        break;
                    default:
                        Debug.Fail("Unexpected enumeration value " + m_valueType.ToString());
                        type = "<Error>";
                        break;
                }

                return HResult.S_OK;
            }

            return GetStaticTypeName(req, out type);
        }
        public string GetDisplayStaticTypeName(DebuggerRequestInfo req){
            string name;
            int hr = GetDisplayStaticTypeName(req, out name);
            NativeMethods.ThrowOnFailure(hr);
            return name;
        }

        /// <summary>
        /// Get the string that should be displayed in the value field for this type
        /// </summary>
        /// <param name="req"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public int GetRawValue(DebuggerRequestInfo req, out string value){
            // Get the actual value for the known primitive types
            EnsureValueType();
            if (m_valueType != ValueType.Unknown){
                value = GetDisplayValueForKnownType(req);
                return HResult.S_OK;
            }

            // If this is a null reference then just return "null"
            if (m_object.IsNullReference){
                value = "null";
                return HResult.S_OK;
            }

            // If this is a non-primitive then display the full type name in brackets
            if (!m_typeField.IsPrimitive && !m_typeField.IsMethod){
                FIELD_INFO info;
                int hr = m_typeField.GetInfo(enum_FIELD_INFO_FIELDS.FIF_FULLNAME, out info);
                if (NativeMethods.Failed(hr))
                {
                    value = null;
                    return hr;
                }

                value = String.Format("{{{0}}}", info.bstrFullName);
                return HResult.S_OK;
            }

            value = "To be determined";
            return HResult.S_OK;
        }

        public string GetRawValue(DebuggerRequestInfo req){
            string value;
            int hr = GetRawValue(req, out value);
            NativeMethods.ThrowOnFailure(hr);
            return value;
        }

        /// <summary>
        /// Get the value we should show in the Display window for this information.  
        /// </summary>
        /// <param name="req"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public int GetDisplayValue(DebuggerRequestInfo req, out string value){
            int hr;

            // When this is not the derived most type we want to display the value for 
            // the derived most entry followed by {RuntimeTypeName}
            bool isDerivedMost;
            EEDebugField rtType;
            hr = this.IsDerivedMostType(out isDerivedMost, out rtType);
            if (NativeMethods.Failed(hr))
            {
                value = null;
                return hr;
            }

            if (!isDerivedMost)
            {
                return GetDisplayValueForDerivedMost(req, out value);
            }

            // Use the raw value
            return GetRawValue(req, out value);
        }

        public string GetDisplayValue(DebuggerRequestInfo req){
            string value;
            int hr = GetDisplayValue(req, out value);
            NativeMethods.ThrowOnFailure(hr);
            return value;
        }

        /// <summary>
        /// Get the attributes for this object
        /// </summary>
        /// <param name="req"></param>
        /// <param name="attrib"></param>
        /// <returns></returns>
        public int GetAttrib(DebuggerRequestInfo req, out Constants attrib){
            attrib = Constants.DBG_ATTRIB_NONE;
            attrib |= Constants.DBG_ATTRIB_DATA;

            if (!m_typeField.IsPrimitive)
            {
                attrib |= Constants.DBG_ATTRIB_CLASS;
            }

            if (IsExpandable())
            {
                attrib |= Constants.DBG_ATTRIB_OBJ_IS_EXPANDABLE;
            }

            return HResult.S_OK;
        }

        public Constants GetAttrib(DebuggerRequestInfo req){
            Constants attrib;
            int hr = GetAttrib(req, out attrib);
            NativeMethods.ThrowOnFailure(hr);
            return attrib;
        }

        /// <summary>
        /// Is the static type of this object the same as the runtime type?  
        /// </summary>
        /// <param name="req"></param>
        /// <param name="isDerivedMost"></param>
        /// <returns></returns>
        public int IsDerivedMostType(out bool isDerivedMost, out EEDebugField rtType){
            // When the object is a null reference we cannot compute it's runtime type.  In this 
            // instance we consider it to be derived most
            if (m_object.IsNullReference){
                isDerivedMost = true;
                rtType = m_typeField;
                return HResult.S_OK;
            }

            // Method's are also types 
            if (m_typeField.IsMethod){
                isDerivedMost = true;
                rtType = m_typeField;
                return HResult.S_OK;
            }

            int hr;
            EEDebugField runtimeType;
            hr = m_context.ResolveRuntimeType(m_object, out runtimeType);
            if (NativeMethods.Failed(hr)){
                isDerivedMost = true;
                rtType = m_typeField;
                return hr;
            }

            hr = runtimeType.Field.Equal(m_typeField.Field);
            rtType = runtimeType;
            if (NativeMethods.Failed(hr)){
                isDerivedMost = true;
            }else{
                isDerivedMost = (HResult.S_OK == hr);
            }
            return HResult.S_OK;
        }

        public bool IsDerivedMostType(){
            bool isDerivedMost;
            EEDebugField rtType;
            int hr = IsDerivedMostType(out isDerivedMost, out rtType);
            NativeMethods.ThrowOnFailure(hr);
            return isDerivedMost;
        }

        public int EnumChildren(PropertyBase parentProp, DebuggerRequestInfo req, Guid guidFilter, string pszNameFilter, out EnumDebugPropertyInfo ppEnum){
            // TODO: Support member specific enumeration
            if (!String.IsNullOrEmpty(pszNameFilter)){
                ppEnum = null;
                return HResult.E_FAIL;
            }
            return EnumChildren(parentProp, req, out ppEnum);
        }

        private void EnsureValueType(){
            if (m_valueType.HasValue){return;}

            m_valueType = ValueType.Unknown;

            FIELD_INFO info;
            int hr = m_typeField.GetInfo(enum_FIELD_INFO_FIELDS.FIF_FULLNAME, out info);
            if (NativeMethods.Failed(hr)){
                return;
            }

            if (0 == String.CompareOrdinal("string", info.bstrFullName)){
                m_valueType = ValueType.String;
            }
            else if (0 == String.CompareOrdinal("whole", info.bstrFullName)){
                m_valueType = ValueType.Int32;
            }
        }


        #region Value Calculation

        private string GetDisplayValueForKnownType(DebuggerRequestInfo req){
            string value = null;
            try{
                switch (m_valueType){
                    case ValueType.String:
                        value = GetDisplayValueForString(req);
                        break;
                    case ValueType.Int32:
                        value = GetDisplayValueForInt32(req);
                        break;
                }
            }
            catch (Exception ex){
                Debug.Fail(ex.Message);
                value = null;
            }
            if (value == null){value = "<Error>";}
            return value;
        }

        private string GetDisplayValueForString(DebuggerRequestInfo req){
            Contract.ThrowIfFalse(m_valueType == ValueType.String);
            if (m_object.IsNullReference){return "null";}
            byte[] data = m_object.GetValue();
            // Check for a null terminator
            int len = data.Length;
            if (len >= 2 && 0 == data[len - 1] && 0 == data[len - 2]){
                return Encoding.Unicode.GetString(data, 0, len - 2);
            }
            return Encoding.Unicode.GetString(data);
        }

        private string GetDisplayValueForInt32(DebuggerRequestInfo req){
            Contract.ThrowIfFalse(m_valueType == ValueType.Int32);
            byte[] data = m_object.GetValue();
            Contract.ThrowIfFalse(data.Length == 4, "Unexpected size for Int32 value");
            int value = 0;
            for (byte i = 0; i < data.Length; ++i){
                int cur = data[i];
                cur <<= (i * 8);
                value += cur;
            }
            return value.ToString();
        }

        private int GetDisplayValueForDerivedMost(DebuggerRequestInfo req, out string value){
            Contract.ThrowIfTrue(this.IsDerivedMostType());
            value = null;
            // Get the runtime property type
            EEDebugField rttField;
            int hr = m_context.ResolveRuntimeType(m_object, out rttField);
            if (NativeMethods.Failed(hr)){return hr;}

            // Create the display for the derived most object
            ObjectInfo dmDisp = new ObjectInfo(m_context, m_object, rttField);

            // Get the value and type string
            string rttValue, rttType;
            if (NativeMethods.Failed(dmDisp.GetDisplayValue(req, out rttValue))
                || NativeMethods.Failed(dmDisp.GetDisplayStaticTypeName(req, out rttType))){
                return NativeMethods.LastFailure;
            }

            value = string.Format("{0} {{{1}}}", rttValue, rttType);
            return HResult.S_OK;
        }

        #endregion

        #region Child Enumeration 

        /// <summary>
        /// Determine whether or not this type has any children that we want to display
        /// </summary>
        /// <returns></returns>
        private bool IsExpandable(){
            EnsureValueType();

            // If the current runtime type does not match the static type then we are expandable
            bool isDerivedMost;
            EEDebugField rtType;
            if (NativeMethods.Succeeded(IsDerivedMostType(out isDerivedMost, out rtType)) && !isDerivedMost){
                return true;
            }

            // Look through the known value types to see if they are expandable
            switch (m_valueType){
                case ValueType.Int32:
                    return false;
                case ValueType.String:
                    return false;
                case ValueType.Unknown:
                    break;
                default:
                    Debug.Assert(false, "Invalid ValueType enumeration");
                    break;
            }

            int count;
            EnumDebugFields memEnum;
            if (NativeMethods.Succeeded(EnumDebugFields.CreateForMembers(m_typeField, sChildKind, out memEnum))
                && NativeMethods.Succeeded(memEnum.GetCount(out count))
                && count > 0){
                return true;
            }
            return false;
        }

        /// <summary>
        /// Enumerate the children
        /// </summary>
        /// <param name="req"></param>
        /// <param name="ppEnum"></param>
        /// <returns></returns>
        private int EnumChildren(PropertyBase parentProp, DebuggerRequestInfo req, out EnumDebugPropertyInfo ppEnum){
            PropertyContext context = new PropertyContext(parentProp, m_object);
            // If the current runtime type does not match the static type then we are expandable
            bool isDerivedMost;
            EEDebugField rtType;
            if (NativeMethods.Succeeded(IsDerivedMostType(out isDerivedMost, out rtType)) && !isDerivedMost){
                ObjectProperty child = new ObjectProperty(context, rtType, m_object, rtType);
                return EnumDebugPropertyInfo.CreateForSingleItem(req, child, out ppEnum);
            }

            // Create an enumeration based off our field enumeration
            EnumDebugFields memEnum;
            if (NativeMethods.Failed(EnumDebugFields.CreateForMembers(m_typeField, sChildKind, out memEnum))
                || NativeMethods.Failed(EnumDebugPropertyInfo.CreateForFieldEnum(req, context, memEnum, out ppEnum))){
                ppEnum = null;
                return NativeMethods.LastFailure;
            }
            return HResult.S_OK;
        }
        #endregion
    }
}
