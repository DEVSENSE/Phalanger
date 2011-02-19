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

    #region IEnumDebugPropertyInfo2 implementations

    internal abstract class EnumDebugPropertyInfo:IEnumDebugPropertyInfo2, IEnumerable<PropertyBase> {
        private DebuggerRequestInfo m_req;

        public DebuggerRequestInfo RequestInfo{
            [DebuggerStepThrough]
            get { return m_req; }
            [DebuggerStepThrough]
            set { m_req = value; }
        }

        protected EnumDebugPropertyInfo(DebuggerRequestInfo req){
            m_req = req;
        }

        #region IEnumDebugPropertyInfo2 Members

        int IEnumDebugPropertyInfo2.Clone(out IEnumDebugPropertyInfo2 ppEnum){
            EnumDebugPropertyInfo tempEnum;
            int hr = Clone(out tempEnum);
            ppEnum = tempEnum;
            return hr;
        }

        int IEnumDebugPropertyInfo2.GetCount(out uint pcelt){
            int count;
            if (NativeMethods.Failed(GetCount(out count)))
            {
                pcelt = 0;
                return NativeMethods.LastFailure;
            }

            checked { pcelt = (uint)count; }
            return HResult.S_OK;
        }
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)]
        public uint Count {
            get {
                uint count;
                NativeMethods.ThrowOnFailure(((IEnumDebugPropertyInfo2)(this)).GetCount(out count));
                return count;
            }
        }

        int IEnumDebugPropertyInfo2.Next(uint celt, DEBUG_PROPERTY_INFO[] rgelt, out uint pceltFetched){
            int requested;
            checked { requested = (int)celt; }

            int found = 0;
            for (int i = 0; i < requested; ++i){
                PropertyBase cur;
                if ( HResult.S_OK != Next(out cur) ){
                    checked { pceltFetched = (uint)found; }
                    return HResult.E_FAIL;
                }

                DEBUG_PROPERTY_INFO info;
                if (NativeMethods.Failed(cur.GetPropertyInfo(RequestInfo, out info))){
                    checked { pceltFetched = (uint)found; }
                    return NativeMethods.LastFailure;
                }

                rgelt[i] = info;
                ++found;
            }

            checked { pceltFetched = (uint)found; }
            return HResult.S_OK;
        }

        [DebuggerStepThrough]
        int IEnumDebugPropertyInfo2.Reset(){
            return Reset();
        }

        int IEnumDebugPropertyInfo2.Skip(uint celt){
            int count;
            checked { count = (int)celt; }
            return Skip(count);
        }

        #endregion

        #region Abstract Members

        protected abstract int Clone(out EnumDebugPropertyInfo ppEnum);

        protected abstract int Next(out PropertyBase propBase);

        protected abstract int Reset();

        protected abstract int Skip(int count);

        protected abstract int GetCount(out int count);

        #endregion

        #region Public 

        public IEnumerable<PropertyBase> GetAllPropertyBase(){
            // Clone this enumeration so that we don't lose state and only request that
            // IDebugProperty2 instances be returned
            EnumDebugPropertyInfo clone;
            if ( NativeMethods.Succeeded(Clone(out clone)) 
                &&NativeMethods.Succeeded(clone.Reset()))
            {
                clone.RequestInfo = m_req.CreateSubRequest(enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_PROP);

                PropertyBase cur;
                int hr = clone.Next(out cur);
                while (HResult.S_OK == hr)
                {
                    yield return cur;
                    hr = clone.Next(out cur);
                }
            }
        }

        #endregion

        #region Factory Methods

        public static int CreateForSingleItem(DebuggerRequestInfo req, PropertyContext context, EEDebugField field, out EnumDebugPropertyInfo ppEnum){
            PropertyFactory factory = new PropertyFactory(context.DebuggerContext);
            PropertyBase propBase;
            if (NativeMethods.Failed(factory.CreateProperty(context, field, out propBase)))
            {
                ppEnum = null;
                return NativeMethods.LastFailure;
            }

            return CreateForSingleItem(req, propBase, out ppEnum);
        }

        public static int CreateForSingleItem(DebuggerRequestInfo req, PropertyBase propBase, out EnumDebugPropertyInfo ppEnum){
            List<PropertyBase> list = new List<PropertyBase>();
            list.Add(propBase);

            ppEnum = new EnumDebugPropertyInfoViaList(req, list);
            return HResult.S_OK;
        }

        public static int CreateForFieldEnum(DebuggerRequestInfo req, PropertyContext context, IEnumDebugFields fieldEnum, out EnumDebugPropertyInfo ppEnum){
            EnumDebugFields myEnum;
            if (NativeMethods.Failed(EnumDebugFields.Create(fieldEnum, out myEnum)))
            {
                ppEnum = null;
                return NativeMethods.LastFailure;
            }

            return CreateForFieldEnum(req, context, myEnum, out ppEnum);
        }

        public static int CreateForFieldEnum(DebuggerRequestInfo req, PropertyContext context, EnumDebugFields fieldEnum, out EnumDebugPropertyInfo ppEnum){
            ppEnum = new EnumDebugPropretyInfoViaFields(req, context, fieldEnum);
            return HResult.S_OK;
        }

        public static int CreateForChildFields(DebuggerRequestInfo req, PropertyContext context, EEDebugField field, out EnumDebugPropertyInfo ppEnum){
            EEDebugContainerField container;
            if (!EEDebugContainerField.TryCreate(field.Field, out container))
            {
                ppEnum = null;
                return HResult.E_FAIL;
            }

            IEnumDebugFields rawEnum;
            if ( NativeMethods.Failed(container.EnumFieldsForDisplay(out rawEnum)))
            {
                ppEnum = null;
                return NativeMethods.LastFailure;
            }

            return CreateForFieldEnum(req, context, rawEnum, out ppEnum);
        }

        /// <summary>
        /// When the runtime type of a property is not the same as the static type of the property
        /// we want to show a special display.  It will create a single property where the runtime
        /// type of the object is displayed
        /// </summary>
        /// <param name="req"></param>
        /// <param name="curProp"></param>
        /// <param name="eeObject"></param>
        /// <param name="ppEnum"></param>
        /// <returns></returns>
        public static int CreateForDerivedMost(DebuggerRequestInfo req, PropertyBase curProp, EEDebugObject eeObject, out EnumDebugPropertyInfo ppEnum){
            // First get the runtime type
            EEDebugField rtType;
            if (NativeMethods.Failed(eeObject.GetType(curProp.DebuggerContext, out rtType)))
            {
                ppEnum = null;
                return HResult.E_FAIL;
            }

            // Now create the property
            PropertyFactory factory = new PropertyFactory(curProp.DebuggerContext);
            PropertyContext context = new PropertyContext(curProp.PropertyContext.PropertyBase, curProp.PropertyContext.ObjectContext);
            ObjectProperty objProp = new ObjectProperty(
                context,
                rtType,
                eeObject,
                rtType);
            return EnumDebugPropertyInfo.CreateForSingleItem(
                req,
                objProp,
                out ppEnum);
        }


        #endregion

        #region IEnumerable<PropertyBase> Members
        /// <summary>Gets type-safe enumerator of elelents</summary>
        public virtual IEnumerator<PropertyBase> GetEnumerator() {
            return new Enumerator(this);
        }

        /// <summary>Retrieves item ta specifici index </summary>
        /// <param name="index">Index to retrieve item on</param>
        /// <returns>Item at index <paramref name="index"/></returns>
        /// <exception cref="ArgumentOutOfRangeException">Index is out of range</exception>
        public virtual PropertyBase GetItem(int index) {
            if(index < 0 || index >= this.Count) throw new ArgumentOutOfRangeException("index");
            int i = 0;
            foreach(PropertyBase item in this) {
                if(i++ == index) return item;
            }
            throw new ArgumentOutOfRangeException("index");
        }

        /// <summary>gets type-unsafe enumerator to enumerate through enumeration of elements</summary>
        [DebuggerStepThrough]
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        /// <summary>Implements enumerator over <see cref="EnumDebugPropertyInfo"/></summary>
        private class Enumerator:IEnumerator<PropertyBase> {
            /// <summary>CTor</summary>
            /// <param name="enumeration">To enumerate over</param>
            public Enumerator(EnumDebugPropertyInfo enumeration) {
                EnumDebugPropertyInfo clone;
                NativeMethods.ThrowOnFailure(enumeration.Clone(out clone));
                this.enumeration = clone;
            }
            /// <summary>The <see cref="EnumDebugPropertyInfo"/> to enumerate over</summary>
            private EnumDebugPropertyInfo enumeration;
            /// <summary>Contains value of the <see cref="Current"/> property</summary>
            private PropertyBase cur;
            /// <summary>True when this instance was disposed</summary>
            private bool disposed;
            /// <summary>gets current value of enumeration</summary>
            /// <exception cref="ObjectDisposedException">Enumerator was dispoised</exception>
            public PropertyBase Current {
                [DebuggerStepThrough]
                get {
                    if(disposed) throw new ObjectDisposedException("EnumDebugPropertyInfo.Enumerator");
                    return cur;
                }
            }
            /// <summary>Disposes the enumerator</summary>
            public void Dispose() {
                enumeration = null;
                cur = null;
                disposed = true;
            }
            /// <summary>Gets current item in type-unsafe way</summary>
            object System.Collections.IEnumerator.Current {
                [DebuggerStepThrough] get { return Current; }
            }

            /// <summary>Advances this enumerator to next value</summary>
            /// <exception cref="ObjectDisposedException">The enumerator was disposed</exception>
            public bool MoveNext() {
                if(disposed) throw new ObjectDisposedException("EnumDebugPropertyInfo.Enumerator");
                int hr;
                NativeMethods.ThrowOnFailure(hr = enumeration.Next(out cur));
                return hr == HResult.S_OK;
            }
            /// <summary>Resets enumerator</summary>
            public void Reset() {
                enumeration.Reset();
                cur = null;
            }
        }

        #endregion
    }

    internal class EnumDebugPropertyInfoViaList:EnumDebugPropertyInfo {
        private int m_index;
        private List<PropertyBase> m_list;

        public EnumDebugPropertyInfoViaList(DebuggerRequestInfo req, List<PropertyBase> list)
            :base(req){
            m_list = list;
        }

        protected override int Clone(out EnumDebugPropertyInfo ppEnum){
            ppEnum =new EnumDebugPropertyInfoViaList(
                RequestInfo,
                new List<PropertyBase>(m_list));
            return HResult.S_OK;
        }

        protected override int Next(out PropertyBase propBase){
            if (m_index >= m_list.Count)
            {
                propBase = null;
                return HResult.S_FALSE;
            }

            propBase = m_list[m_index];
            m_index++;
            return HResult.S_OK;
        }

        public override IEnumerator<PropertyBase> GetEnumerator() {
            return m_list.GetEnumerator();
        }

        public override PropertyBase GetItem(int index) {
            return m_list[index];
        }

        protected override int Reset(){
            m_index = 0;
            return HResult.S_OK;
        }

        protected override int Skip(int count){
            if (m_index + count <= m_list.Count){
                m_index += count;
                return HResult.S_OK;
            }

            return HResult.E_FAIL;
        }

        protected override int GetCount(out int count){
            count = m_list.Count;
            return HResult.S_OK;
        }
    }

    internal class EnumDebugPropretyInfoViaFields:EnumDebugPropertyInfo
    {
        private PropertyContext m_propContext;
        private PropertyFactory m_propFactory;
        private EnumDebugFields m_enum;

        public EnumDebugPropretyInfoViaFields(DebuggerRequestInfo req, PropertyContext propContext, EnumDebugFields enumFields)
            :base(req)
        {
            m_propContext = propContext;
            m_propFactory = new PropertyFactory(propContext.DebuggerContext);
            m_enum = enumFields;
        }

        protected override int Clone(out EnumDebugPropertyInfo ppEnum)
        {
            EnumDebugFields fieldClone;
            if (NativeMethods.Failed(m_enum.Clone(out fieldClone))
                || NativeMethods.Failed(fieldClone.Reset()))
            {
                ppEnum = null;
                return NativeMethods.LastFailure;
            }

            ppEnum = new EnumDebugPropretyInfoViaFields(RequestInfo, m_propContext, fieldClone);
            return HResult.S_OK;
        }

        protected override int Next(out PropertyBase propBase)
        {
            IDebugField field;
            int hr = m_enum.Next(out field);
            if (hr != HResult.S_OK)
            {
                propBase = null;
                return hr;
            }

            return m_propFactory.CreateProperty(m_propContext, new EEDebugField(field), out propBase);
        }

        protected override int Reset()
        {
            return m_enum.Reset();
        }

        protected override int Skip(int count)
        {
            return m_enum.Skip(count);
        }

        protected override int GetCount(out int count)
        {
            return m_enum.GetCount(out count);
        }
    }

    #endregion

    #region IEnumDebugFields implementations

    internal abstract class EnumDebugFields:IEnumDebugFields {
        #region IEnumDebugFields Members

        int IEnumDebugFields.Clone(out IEnumDebugFields ppEnum){
            EnumDebugFields clone;
            int hr = Clone(out clone);
            ppEnum = clone;
            return hr;
        }

        int IEnumDebugFields.GetCount(out uint pcelt){
            int count;
            if (NativeMethods.Succeeded(GetCount(out count)))
            {
                checked { pcelt = (uint)count; }
                return HResult.S_OK;
            }
            else
            {
                pcelt = 0;
                return HResult.E_FAIL;
            }
        }

        int IEnumDebugFields.Next(uint celt, IDebugField[] rgelt, ref uint pceltFetched){
            int requested;
            checked { requested = (int)celt; }

            int found = 0;
            for (int i = 0; i < requested; ++i)
            {
                IDebugField cur;
                int hr = Next(out cur);
                if ( HResult.S_OK != hr )
                {
                    checked { pceltFetched = (uint)found; }
                    return hr;
                }

                rgelt[i] = cur;
            }

            checked { pceltFetched = (uint)found; }
            return HResult.S_OK;
        }

        [DebuggerStepThrough]
        int IEnumDebugFields.Reset(){
            return Reset();
        }

        int IEnumDebugFields.Skip(uint celt){
            int count;
            checked { count = (int)celt; }
            return Skip(count);
        }

        #endregion

        public abstract int Clone(out EnumDebugFields clone);

        public abstract int GetCount(out int count);

        public abstract int Next(out IDebugField  field);

        public abstract int Reset();

        public abstract int Skip(int count);

        public IEnumerable<IDebugField> GetAll(){
            EnumDebugFields clone;
            if (NativeMethods.Succeeded(Clone(out clone)) && NativeMethods.Succeeded(clone.Reset())){
                IDebugField cur;
                int hr = Next(out cur);
                while (HResult.S_OK == hr){
                    yield return cur;
                    hr = Next(out cur);
                }
            }
        }

        public IEnumerable<EEDebugField> GetAllWrapped(){
            foreach (var cur in GetAll()){
                yield return new EEDebugField(cur);
            }
        }

        #region Factory Methods

        public static int Create(IEnumDebugFields rawEnum, out EnumDebugFields ppEnum){
            if (rawEnum == null){
                ppEnum = null;
                return HResult.E_FAIL;
            }

            ppEnum = new EnumDebugFieldsSimpleWrapper(rawEnum);
            return HResult.S_OK;
        }

        public static int Create(out EnumDebugFields ppEnum, params IEnumDebugFields[] enumList){
            List<IDebugField> list = new List<IDebugField>();
            foreach (IEnumDebugFields pEnum in enumList){
                if (pEnum == null){continue;}

                EnumDebugFields myEnum;
                if (NativeMethods.Failed(Create(pEnum, out myEnum))){
                    ppEnum = null;
                    return NativeMethods.LastFailure;
                }

                list.AddRange(myEnum.GetAll());
            }

            ppEnum = new EnumDebugFieldsViaList(list);
            return HResult.S_OK;
        }

        public static int CreateForMembers(EEDebugField field, enum_FIELD_KIND kind, out EnumDebugFields ppEnum){
            // Get the container
            EEDebugContainerField containerField;
            if (!EEDebugContainerField.TryCreate(field.Field, out containerField)){
                ppEnum = null;
                return HResult.E_FAIL;
            }

            IEnumDebugFields memEnum;
            if (NativeMethods.Failed(containerField.EnumFields(kind, out memEnum)) || NativeMethods.Failed(Create(memEnum, out ppEnum))) {
                ppEnum = null;
                return NativeMethods.LastFailure;
            }

            return HResult.S_OK;
        }

        #endregion
    }

    internal class EnumDebugFieldsSimpleWrapper:EnumDebugFields {
        private IEnumDebugFields m_enum;

        public EnumDebugFieldsSimpleWrapper(IEnumDebugFields pEnum){
            m_enum = pEnum;
        }

        public override int Clone(out EnumDebugFields enumClone){
            IEnumDebugFields clone;
            if (NativeMethods.Failed(m_enum.Clone(out clone)) || NativeMethods.Failed(clone.Reset())){
                enumClone = null;
                return NativeMethods.LastFailure;
            }

            enumClone = new EnumDebugFieldsSimpleWrapper(clone);
            return HResult.S_OK;
        }

        public override int GetCount(out int count){
            uint uCount;
            if (NativeMethods.Failed(m_enum.GetCount(out uCount))){
                count = 0;
                return NativeMethods.LastFailure;
            }

            checked { count = (int)uCount; }
            return HResult.S_OK;
        }

        public override int Next(out IDebugField field){
            IDebugField[] arr = new IDebugField[1];
            uint found = 0;
            if (NativeMethods.Failed(m_enum.Next(1, arr, ref found))){
                field = null;
                return NativeMethods.LastFailure;
            }

            if (1 != found || arr[0] == null){
                field = null;
                return HResult.E_FAIL;
            }

            field = arr[0];
            return HResult.S_OK;
        }

        public override int Reset(){
            return m_enum.Reset();
        }

        public override int Skip(int count){
            uint uCount;
            checked { uCount = (uint)count; }
            return m_enum.Skip(uCount);
        }
    }

    internal class EnumDebugFieldsViaList:EnumDebugFields {
        private int m_index;
        private List<IDebugField> m_list;

        public EnumDebugFieldsViaList(List<IDebugField> list){
            m_list = list;
        }

        public override int Clone(out EnumDebugFields clone){
            clone = new EnumDebugFieldsViaList(new List<IDebugField>(m_list));
            return HResult.S_OK;
        }

        public override int GetCount(out int count){
            count = m_list.Count;
            return HResult.S_OK;
        }

        public override int Next(out IDebugField field){
            if (m_index >= m_list.Count){
                field = null;
                return HResult.S_FALSE;
            }

            field = m_list[m_index];
            m_index++;
            return HResult.S_OK;
        }

        public override int Reset(){
            m_index = 0;
            return HResult.S_OK;
        }

        public override int Skip(int count){
            if (m_index + count > m_list.Count){
                return HResult.E_FAIL;
            }

            m_index += count;
            return HResult.S_OK;
        }
    }

    #endregion

 
}
