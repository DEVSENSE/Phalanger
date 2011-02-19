//© Đonny 2009. part of Phalanger project
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Debugger.Interop;
using Microsoft.Win32;
using Microsoft.VisualStudio;
using System.Diagnostics;

namespace PHP.VisualStudio.PhalangerLanguageService.Debugger {
    internal class PHPMethodProperty :PropertyBase {

        private PropertyWrapper LocalsProperty;
        private ObjectWrapper LocalsObject;


        public PHPMethodProperty (PropertyContext context, EEDebugField baseField, EEDebugObject baseObject, EEDebugField typeField, IDebugObject locals ):
            base(context,baseField,baseObject,typeField){
            this.LocalsObject = new ObjectWrapper(context.DebuggerContext, locals);
            this.LocalsProperty=LocalsObject.GetProperty();
        }


        protected override string GetPropertyValueImpl(DebuggerRequestInfo req) {
            FIELD_INFO info = this.BaseField.GetInfo(enum_FIELD_INFO_FIELDS.FIF_FULLNAME);
            return info.bstrFullName;
        }

        protected override string GetPropertyTypeImpl(DebuggerRequestInfo req) {
            EEDebugField type;
            NativeMethods.ThrowOnFailure(BaseField.GetType(out type));

            FIELD_INFO info = type.GetInfo(enum_FIELD_INFO_FIELDS.FIF_FULLNAME);
            return info.bstrFullName;
        }

        protected override Constants GetPropertyAttribImpl(DebuggerRequestInfo info) {
            Constants flags = 0;
            flags |= Constants.DBG_ATTRIB_METHOD;
            flags |= Constants.DBG_ATTRIB_OBJ_IS_EXPANDABLE;
            return flags;
        }

        private Dictionary<string, DEBUG_PROPERTY_INFO> getLocals() {
            IDebugFunctionObject getEnumerator = PHPDebugExpression.GetFunction(PropertyContext.DebuggerContext, "System.Collections.IEnumerable", "GetEnumerator",LocalsObject.Object);
            IDebugObject enumeratorObj;
            NativeMethods.ThrowOnFailure(
                getEnumerator.Evaluate(new IDebugObject[0], IntPtr.Zero, 0, out enumeratorObj));
            Dictionary<string, DEBUG_PROPERTY_INFO> ret = new Dictionary<string, DEBUG_PROPERTY_INFO>();
            EnumeratorWrapper enumerator = new EnumeratorWrapper(PropertyContext.DebuggerContext, enumeratorObj);
            while(enumerator.MoveNext()) {
                var kwp = new ObjectWrapper(PropertyContext.DebuggerContext, enumerator.Current);
                string key = ((KeyValuePair<string, object>)kwp.GetManagedObject().GetValue()).Key;
                string kwptype= kwp.GetProperty().GetPropertyInfo().bstrType;
                IDebugFunctionObject get_Value = PHPDebugExpression.GetFunction(PropertyContext.DebuggerContext, kwptype, "get_Key",kwp.Object);
                IDebugObject value;
                NativeMethods.ThrowOnFailure(
                    get_Value.Evaluate(new IDebugObject[0], IntPtr.Zero, 0, out value));
                var valueo = new ObjectWrapper(PropertyContext.DebuggerContext,value);
                PropertyWrapper valuep = valueo.GetProperty();
                ret.Add(key, valuep.GetPropertyInfo());
            }
            return ret;
        }

        private class EnumeratorWrapper : IEnumerator<IDebugObject> {

            private IDebugFunctionObject moveNext;
            private IDebugFunctionObject get_current;
            private IDebugFunctionObject reset;
            private DebuggerContext context;
            private IDebugObject enumerator;
            public EnumeratorWrapper(DebuggerContext context, IDebugObject enumerator) {
                this.context = context;
                this.enumerator = enumerator;
                this.moveNext = PHPDebugExpression.GetFunction(context,"System.Collections.IEnumerator", "MoveNext",enumerator);
                this.get_current = PHPDebugExpression.GetFunction(context, "System.Collections.IEnumerator", "get_Current", enumerator);
                this.reset = PHPDebugExpression.GetFunction(context, "System.Collections.IEnumerator", "Reset", enumerator);
            }

            public IDebugObject Current {
                get {
                    IDebugObject ppResult;
                    NativeMethods.ThrowOnFailure(
                        get_current.Evaluate(new IDebugObject[0], IntPtr.Zero , 0, out ppResult));
                    return ppResult;
                }
            }

            public bool MoveNext() {
                IDebugObject ppResult;
                NativeMethods.ThrowOnFailure(
                    moveNext.Evaluate(new IDebugObject[0], IntPtr.Zero, 0, out ppResult));
                return (bool) new ObjectWrapper(context, ppResult).GetManagedObject().GetValue();
            }

            public void Reset() {
                IDebugObject ppResult;
                NativeMethods.ThrowOnFailure(
                    reset.Evaluate(new IDebugObject[0], IntPtr.Zero, 0, out ppResult));
            }

            void IDisposable.Dispose() {
                moveNext=null;
                get_current=null;
                reset=null;
                context=null;
                enumerator=null;
            }
            object System.Collections.IEnumerator.Current {
                [DebuggerStepThrough] get { return Current; }
            }

        }

        public override int EnumChildren(uint dwFields, uint dwRadix, ref Guid guidFilter, ulong dwAttribFilter, string pszNameFilter, uint dwTimeout, out IEnumDebugPropertyInfo2 ppEnum) {
            var locals = getLocals();
            if(locals == null) {
                ppEnum = null;
                return HResult.E_NOTIMPL;
            } else {
                ppEnum = new ChildEnum(locals);
                return HResult.S_OK;
            }
        }

        protected override int EnumChildren(DebuggerRequestInfo req, Guid guidFilter, string pszNameFilter, out EnumDebugPropertyInfo ppEnum) {
            ppEnum = null;
            return HResult.E_NOTIMPL;
        }


        private class ChildEnum:IEnumDebugPropertyInfo2 {

            private IEnumerator<KeyValuePair<string, DEBUG_PROPERTY_INFO>> enumerator;
            private Dictionary<string, DEBUG_PROPERTY_INFO> dic;

            public ChildEnum(Dictionary<string, DEBUG_PROPERTY_INFO> dic) {
                this.dic = dic;
                enumerator = dic.GetEnumerator();
            }



            #region IEnumDebugPropertyInfo2 Members

            public int Clone(out IEnumDebugPropertyInfo2 ppEnum) {
                ppEnum = new ChildEnum(dic);
                return HResult.S_OK;
            }

            public int GetCount(out uint pcelt) {
                pcelt = (uint)dic.Count;
                return HResult.S_OK;
            }

            public int Next(uint celt, DEBUG_PROPERTY_INFO[] rgelt, out uint pceltFetched) {
                if(celt != rgelt.Length) {
                    pceltFetched = 0;
                    return HResult.E_INVALIDARG;
                }
                int i = 0;
                while(enumerator.MoveNext()) {
                    rgelt[i] = enumerator.Current.Value;
                    i += 1;
                    if(i == celt) break;
                }
                pceltFetched = (uint)i;
                return i == 0 ? HResult.S_FALSE : HResult.S_OK;
            }

            public int Reset() {
                enumerator.Reset();
                return HResult.S_OK;
            }

            public int Skip(uint celt) {
                int i=0;
                while(enumerator.MoveNext()) {
                    if(++i == celt) break;
                }
                return i == celt ? HResult.S_FALSE : HResult.S_OK;
            }

            #endregion
        }

 
    }
}
