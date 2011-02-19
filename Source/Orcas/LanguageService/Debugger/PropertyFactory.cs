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

namespace PHP.VisualStudio.PhalangerLanguageService.Debugger {
    internal class PropertyFactory {
        private DebuggerContext m_context;

        public PropertyFactory(DebuggerContext context) {
            m_context = context;
        }

        public int CreateProperty(PropertyContext context, IDebugField field, out PropertyBase ppProp) {
            return CreateProperty(context, new EEDebugField(field), out ppProp);
        }

        public int CreateProperty(PropertyContext context, EEDebugField field, out PropertyBase ppProp) {
            Contract.ThrowIfNull(context);
            Contract.ThrowIfNull(field);

            if(field.IsSymbol) {
                return CreateSymbolProperty(context, field, out ppProp);
            } else if(field.IsPrimitive) {
                return CreatePrimitiveProperty(context, field, out ppProp);
            }

            ppProp = null;
            return HResult.E_FAIL;
        }

        public int CreatePrimitiveProperty(PropertyContext propContext, EEDebugField field, out PropertyBase ppProp) {
            Contract.ThrowIfNull(propContext);
            Contract.ThrowIfNull(field);
            Contract.ThrowIfFalse(field.IsPrimitive);

            // For a primitev we need to bind the field inside of the parent object
            EEDebugObject primObject;
            DebuggerContext context = propContext.DebuggerContext;
            if(NativeMethods.Failed(context.Bind(propContext.ObjectContext, field, out primObject))) {
                ppProp = null;
                return HResult.E_FAIL;
            }

            ppProp = new ObjectProperty(propContext, field, primObject, field);
            return HResult.S_OK;
        }

        public int CreateSymbolProperty(PropertyContext propContext, EEDebugField field, out PropertyBase ppProp) {
            Contract.ThrowIfNull(propContext);
            Contract.ThrowIfNull(field);
            Contract.ThrowIfFalse(field.IsSymbol);

            if(field.IsLocal || field.IsParam) {
                return CreateLocalOrParamProperty(propContext, field, out ppProp);
            } else if(field.IsProperty) {
                return CreateDotNetGetSetProperty(propContext, field, out ppProp);
            } else if(field.IsMethod) {
                return CreateMethodProperty(propContext, new EEDebugContainerField(field.Field), out ppProp);
            } else if(field.IsMember) {
                return CreateMemberProperty(propContext, field, out ppProp);
            }

            ppProp = null;
            return HResult.E_FAIL;
        }

        public int CreateMethodProperty(PropertyContext propContext, EEDebugContainerField containerField, out PropertyBase ppProp) {
            Contract.ThrowIfNull(containerField);
            Contract.ThrowIfFalse(containerField.IsMethod);

            // First get the object for the method.  For methods we need to just bind into null since
            // we don't need a context object.  Method context is implicitly used by the Debugger
            DebuggerContext context = propContext.DebuggerContext;
            EEDebugObject methodObject;
            if(NativeMethods.Failed(context.Bind(propContext.ObjectContext, containerField, out methodObject))) {
                ppProp = null;
                return HResult.E_FAIL;
            }

            // Get the type of the method.  This makes us call GetType twice to get down to the actual type
            EEDebugField methodType;
            if(NativeMethods.Failed(containerField.GetType(out methodType))) {
                ppProp = null;
                return HResult.E_FAIL;
            }

            ppProp = new MethodProperty(propContext, containerField, methodObject, methodType);
            return HResult.S_OK;
        }

        public PHPMethodProperty CreatePHPMethodProperty(PropertyContext propContext, EEDebugContainerField containerField, IDebugObject locals) {
            Contract.ThrowIfNull(containerField);
            Contract.ThrowIfNull(locals);
            Contract.ThrowIfFalse(containerField.IsMethod);

            // First get the object for the method.  For methods we need to just bind into null since
            // we don't need a context object.  Method context is implicitly used by the Debugger
            DebuggerContext context = propContext.DebuggerContext;
            EEDebugObject methodObject;
            NativeMethods.ThrowOnFailure(
                context.Bind(propContext.ObjectContext, containerField, out methodObject));

            // Get the type of the method.  This makes us call GetType twice to get down to the actual type
            EEDebugField methodType;
            NativeMethods.ThrowOnFailure(
                containerField.GetType(out methodType));

            return new PHPMethodProperty(propContext, containerField, methodObject, methodType, locals);

        }

        public int CreateLocalOrParamProperty(PropertyContext propContext, EEDebugField localField, out PropertyBase ppProp) {
            Contract.ThrowIfNull(propContext);
            Contract.ThrowIfNull(localField);
            Contract.ThrowIfFalse(localField.IsLocal || localField.IsParam);

            // First we need to bind the local in the context of the method.  Our 
            // PropertyContext object will be the method in this case
            if(propContext.ObjectContext == null) {
                ppProp = null;
                return HResult.E_FAIL;
            }

            EEDebugObject localObject;
            if(NativeMethods.Failed(propContext.DebuggerContext.Bind(propContext.ObjectContext, localField, out localObject))) {
                ppProp = null;
                return HResult.E_FAIL;
            }

            // Get the type of the local
            EEDebugField typeField;
            if(NativeMethods.Failed(localObject.GetType(propContext.DebuggerContext, out typeField))) {
                ppProp = null;
                return HResult.E_FAIL;
            }

            ppProp = new LocalOrParamProperty(propContext, localField, localObject, typeField);
            return HResult.S_OK;
        }

        public int CreateMemberProperty(PropertyContext propContext, EEDebugField memberField, out PropertyBase ppProp) {
            Contract.ThrowIfNull(propContext);
            Contract.ThrowIfNull(memberField);
            Contract.ThrowIfFalse(memberField.IsMember);

            // Firstly we want to get the object for our member here.  To do this we have to 
            // have a parent object to bind inside of.  
            if(propContext.ObjectContext == null) {
                throw new InvalidOperationException("Need a parent object for the member");
            }

            EEDebugObject memberObject;
            if(NativeMethods.Failed(m_context.Bind(propContext.ObjectContext, memberField, out memberObject))) {
                ppProp = null;
                return NativeMethods.LastFailure;
            }

            // Now get the type of the member
            int hr;
            EEDebugField typeField;
            if(NativeMethods.Failed(
                hr = memberObject.GetType(propContext.DebuggerContext, out typeField))) {
                    ppProp = null;
                    return NativeMethods.LastFailure;
            }
            if(hr == HResult.S_FALSE) {
                ppProp = null;
                return HResult.E_UNEXPECTED;
            }

            ppProp = new MemberProperty(propContext, memberField, memberObject, typeField);
            return HResult.S_OK;
        }

        public int CreateDotNetGetSetProperty(PropertyContext context, EEDebugField field, out PropertyBase ppProp) {
            Contract.ThrowIfNull(context);
            Contract.ThrowIfNull(field);

            DotNetGetSetProperty prop;
            if(NativeMethods.Failed(DotNetGetSetProperty.Create(context, field, out prop))) {
                ppProp = null;
                return HResult.E_FAIL;
            }

            ppProp = prop;
            return HResult.S_OK;
        }
    }

}

