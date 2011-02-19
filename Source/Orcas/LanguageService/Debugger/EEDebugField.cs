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
    /// Simple wrapper around IDebugField.  It will load all of the information about 
    /// the field into the class.  Mainly this is used to make Debugging the Debugger 
    /// a little bit easier.
    /// </summary>
    [DebuggerDisplay("{DisplayValue}")]
    internal  class EEDebugField
    {
        private IDebugField m_field;
        private enum_FIELD_KIND m_kind;
#if DEBUG
        private FIELD_INFO m_info;
        private Exception m_ex;
#endif

        public IDebugField Field
        {
            [DebuggerStepThrough]
            get { return m_field; }
            set
            {
                m_field = value;
                OnFieldChanged();
            }
        }

        public enum_FIELD_KIND Kind
        {
            [DebuggerStepThrough]
            get { return m_kind; }
        }

        public bool IsSymbol
        {
            [DebuggerStepThrough]
            get { return 0 != (enum_FIELD_KIND.FIELD_KIND_SYMBOL & m_kind); }
        }

        public bool IsMethod
        {
            [DebuggerStepThrough]
            get { return 0 != (enum_FIELD_KIND.FIELD_TYPE_METHOD & m_kind); }
        }

        public bool IsLocal
        {
            [DebuggerStepThrough]
            get { return 0 != (enum_FIELD_KIND.FIELD_SYM_LOCAL & m_kind); }
        }

        public bool IsParam
        {
            [DebuggerStepThrough]
            get { return 0 != (enum_FIELD_KIND.FIELD_SYM_PARAM & m_kind); }
        }

        public bool IsPrimitive
        {
            [DebuggerStepThrough]
            get { return 0 != (enum_FIELD_KIND.FIELD_TYPE_PRIMITIVE & m_kind); }
        }

        public bool IsType
        {
            [DebuggerStepThrough]
            get { return 0 != (enum_FIELD_KIND.FIELD_KIND_TYPE & m_kind); }
        }

        public bool IsMember
        {
            [DebuggerStepThrough]
            get { return 0 != (enum_FIELD_KIND.FIELD_SYM_MEMBER & m_kind); }
        }

        public bool IsProperty
        {
            [DebuggerStepThrough]
            get { return 0 != (enum_FIELD_KIND.FIELD_TYPE_PROP & m_kind); }
        }

        private string DisplayValue
        {
            [DebuggerStepThrough]
            get
            {
#if DEBUG
                return m_info.bstrFullName;
#else
                return ToString();
#endif
            }
        }


        public EEDebugField()
        {
            m_field = null;
            OnFieldChanged();
        }

        public EEDebugField(IDebugField field)
        {
            m_field = field;
            OnFieldChanged();
        }
       
        public FIELD_INFO GetInfo(enum_FIELD_INFO_FIELDS flags)
        {
            FIELD_INFO info;
            NativeMethods.ThrowOnFailure(GetInfo(flags, out info));
            return info;
        }
         

        public int GetInfo(enum_FIELD_INFO_FIELDS flags, out FIELD_INFO info)
        {
            FIELD_INFO[] arr = new FIELD_INFO[1];
            int hr = m_field.GetInfo((uint)flags, arr);
            if (NativeMethods.Succeeded(hr))
            {
                info = arr[0];
            }
            else
            {
                info = new FIELD_INFO();
            }

            return hr;
        }

        public int GetType(out EEDebugField typeField)
        {
            IDebugField field;
            int hr = m_field.GetType(out field);
            if (NativeMethods.Failed(hr))
            {
                typeField = null;
                return hr;
            }

            typeField = new EEDebugField(field);
            return hr;
        }
        public EEDebugField Type {
            get {
                EEDebugField type;
                NativeMethods.ThrowOnFailure(GetType(out type));
                return type;
            }
        }

        protected virtual void OnFieldChanged()
        {
            if (null == m_field)
            {
                m_kind = enum_FIELD_KIND.FIELD_KIND_NONE;
#if DEBUG
                m_info = new FIELD_INFO();
#endif
            }
            else
            {
                uint kind;
                NativeMethods.ThrowOnFailure(m_field.GetKind(out kind));
                m_kind = (enum_FIELD_KIND)kind;
            }

            ReloadDebugInfo();
        }

        protected virtual void ReloadDebugInfo()
        {
#if DEBUG
            m_ex = null;
            if (null == m_field)
            {
                m_info = new FIELD_INFO();
                m_ex = null;
                return;
            }

            try
            {
                m_info = GetInfo(enum_FIELD_INFO_FIELDS.FIF_ALL);
            }
            catch (Exception ex)
            {
                m_ex = ex;
            }
#endif
        }
        //public static implicit operator EEDebugField (IDebugField a){
        //    return new EEDebugField(a);
        //}
    }

    /// <summary>
    /// Simple wrapper on top of the IDebugContainerField interface.
    /// </summary>
    internal class EEDebugContainerField:EEDebugField
    {
        private IDebugContainerField m_containerField;

        public IDebugContainerField ContainerField
        {
            [DebuggerStepThrough] get { return m_containerField; }
            set{
                // Set the base value and we'll propagate up on the resulting event
                this.Field = value;
            }
        }

        public EEDebugContainerField(IDebugContainerField field):base(field){}

        public EEDebugContainerField(IDebugField field):base(field){}

        protected override void OnFieldChanged(){
            base.OnFieldChanged();
            m_containerField = (IDebugContainerField)this.Field;
        }

        public int EnumFields(enum_FIELD_KIND kind, enum_FIELD_MODIFIERS mod, string nameFilter, NAME_MATCH match, out IEnumDebugFields ppEnum){
            return m_containerField.EnumFields(
                (uint)kind,
                (uint)mod,
                nameFilter,
                match,
                out ppEnum);
        }

        public int EnumFieldsForDisplay(out IEnumDebugFields ppEnum){
            return EnumFieldsForDisplay(
                null,
                NAME_MATCH.nmNone,
                out ppEnum);
        }
        public IEnumDebugFields EnumFieldsForDisplay() {
            IEnumDebugFields enumfd;
            NativeMethods.ThrowOnFailure(EnumFieldsForDisplay(out enumfd));
            return enumfd;
        }

        public int EnumFieldsForDisplay(string nameFilter, NAME_MATCH match, out IEnumDebugFields ppEnum){
            return this.EnumFields(
                enum_FIELD_KIND.FIELD_KIND_ALL,
                enum_FIELD_MODIFIERS.FIELD_MOD_ALL,
                nameFilter,
                match,
                out ppEnum);
        }

        public int EnumFields(enum_FIELD_KIND kind, out IEnumDebugFields ppEnum){
            return EnumFields(kind, enum_FIELD_MODIFIERS.FIELD_MOD_ALL, null, NAME_MATCH.nmCaseInsensitive, out ppEnum);
        }

        public static bool TryCreate(IDebugField field, out EEDebugContainerField created){
            IDebugContainerField container = field as IDebugContainerField;
            if (container == null)
            {
                created = null;
                return false;
            }

            created = new EEDebugContainerField(container);
            return true;
        }
        //public static implicit operator EEDebugContainerField(IDebugContainerField a) {
        //    return new EEDebugContainerField(a);
        //}
        //public static explicit operator EEDebugContainerField(IDebugField a) {
        //    return new EEDebugContainerField(a);
        //}
    }

    /// <summary>
    /// Simple wrapper on top of the IDebugMethodField interface
    /// </summary>
    internal class EEDebugMethodField:EEDebugContainerField {
        private IDebugMethodField m_methodField;

        public IDebugMethodField MethodField{
            [DebuggerStepThrough]
            get { return m_methodField; }
            [DebuggerStepThrough]            
            set{// Set the base value and we'll propagate up the resulting event
                this.Field = value;
            }
        }

        public EEDebugMethodField(IDebugMethodField field): base(field){}

        public EEDebugMethodField(IDebugField field): base(field){}

        public int GetThis(out EEDebugClassField classField){
            IDebugClassField field;
            int hr = m_methodField.GetThis(out field);
            if (NativeMethods.Succeeded(hr)){
                classField = new EEDebugClassField(field);
            }else{
                classField = null;
            }
            return hr;
        }

        protected override void OnFieldChanged(){
            base.OnFieldChanged();
            m_methodField = (IDebugMethodField)this.Field;
        }
        //public static implicit operator EEDebugMethodField(IDebugMethodField a) {
        //    return new EEDebugMethodField(a);
        //}
        //public static explicit operator EEDebugMethodField(IDebugField a) {
        //    return new EEDebugMethodField(a);
        //}
    }

    internal class EEDebugClassField:EEDebugContainerField {
        private IDebugClassField m_classField;

        public IDebugClassField ClassField{
            [DebuggerStepThrough]
            get { return m_classField; }
            [DebuggerStepThrough]
            set { this.Field = value; }
        }

        public EEDebugClassField(IDebugClassField field): base(field){}

        public int GetThisObject(DebuggerContext context, out EEDebugObject thisObject){
            return context.Bind(null, this, out thisObject);
        }

        protected override void OnFieldChanged(){
            base.OnFieldChanged();
            m_classField = (IDebugClassField)Field;
        }
        //public static implicit operator EEDebugClassField(IDebugClassField a) {
        //    return new EEDebugClassField(a);
        //}
        //public static explicit operator EEDebugClassField(IDebugField a) {
        //    return new EEDebugClassField(a);
        //}
    }
}
