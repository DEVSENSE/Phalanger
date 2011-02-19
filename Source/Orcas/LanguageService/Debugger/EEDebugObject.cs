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
    internal class EEDebugObject {
        private IDebugObject m_object;

        public IDebugObject Object{
            [DebuggerStepThrough]
            get { return m_object; }
            set{
                m_object = value;
                OnObjectChanged();
            }
        }

        public bool IsNullReference{
            get{
                int retIsNull;
                if (NativeMethods.Succeeded(m_object.IsNullReference(out retIsNull))){
                    return 0 == retIsNull ? false : true;
                }
                return false;
            }
        }

        public EEDebugObject(IDebugObject obj){
            this.Object = obj;
        }

        private void OnObjectChanged(){}

        public int GetValue(out byte[] data){
            uint size;
            int hr = m_object.GetSize(out size);
            if (NativeMethods.Failed(hr)){
                data = null;
                return hr;
            }

            byte[] value = new byte[size];
            hr = m_object.GetValue(value, size);
            if (NativeMethods.Failed(hr)){
                data = null;
                return hr;
            }

            data = value;
            return hr;
        }

        public byte[] GetValue(){
            byte[] data;
            int hr = this.GetValue(out data);
            NativeMethods.ThrowOnFailure(hr);
            return data;
        }

        public int GetType(DebuggerContext context, out EEDebugField typeField){
            return context.ResolveRuntimeType(this, out typeField);
        }

        public EEDebugField GetType(DebuggerContext context){
            EEDebugField field;
            NativeMethods.ThrowOnFailure(GetType(context, out field));
            return field;
        }

        //public static implicit operator EEDebugObject(IDebugObject a) {
        //    return new EEDebugObject(a);
        //}
    }
}
