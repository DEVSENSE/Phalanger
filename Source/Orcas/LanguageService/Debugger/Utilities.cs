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
    /// <summary>
    /// Handy wrapper for types commonly passed around in the Expression Evaluator
    /// </summary>
    internal class DebuggerContext {
        private IDebugSymbolProvider m_symbolProvider;
        private IDebugAddress m_address;
        private IDebugBinder m_binder;

        public IDebugSymbolProvider SymbolProvider {[DebuggerStepThrough]get { return m_symbolProvider; }}

        public IDebugAddress Address {[DebuggerStepThrough]get { return m_address; }}

        public IDebugBinder Binder {[DebuggerStepThrough]get { return m_binder; }}
        [DebuggerStepThrough]
        public DebuggerContext(IDebugSymbolProvider provider, IDebugAddress address, IDebugBinder binder) {
            m_symbolProvider = provider;
            m_address = address;
            m_binder = binder;
        }

        public int ResolveRuntimeType(EEDebugObject eeObject, out EEDebugField eeType) {
            Contract.ThrowIfNull(eeObject);

            IDebugField type;
            int hr = m_binder.ResolveRuntimeType(eeObject.Object, out type);
            if(NativeMethods.Failed(hr)) {
                eeType = null;
                return hr;
            }
            if(type == null) {
                eeType = null;
                return hr;
            }

            eeType = new EEDebugField(type);
            return HResult.S_OK;
        }

        public int Bind(EEDebugObject eeObject, EEDebugField eeField, out EEDebugObject boundObject) {
            if(null == Binder) {
                boundObject = null;
                return HResult.E_FAIL;
            }

            IDebugObject bound;
            int hr = Binder.Bind(
                eeObject == null ? null : eeObject.Object,
                eeField.Field,
                out bound);
            if(NativeMethods.Succeeded(hr)) {
                boundObject = new EEDebugObject(bound);
            } else {
                boundObject = null;
            }

            return hr;
        }
    }

    internal class DebuggerRequestInfo {
        private enum_DEBUGPROP_INFO_FLAGS m_flags;
        private uint m_radix;
        private uint m_timeout;

        public enum_DEBUGPROP_INFO_FLAGS Flags {[DebuggerStepThrough] get { return m_flags; }}

        public uint Radix {[DebuggerStepThrough]get { return m_radix; }}

        public uint Timeout {[DebuggerStepThrough]get { return m_timeout; }}

        public bool IsFuncEvalAllowed {
            [DebuggerStepThrough]get { return 0 == (m_flags & enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_NOFUNCEVAL); }
        }

        public bool RequestName {
            [DebuggerStepThrough]get { return 0 != (m_flags & enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_NAME); }
        }

        public bool RequestValue {
            [DebuggerStepThrough]get { return 0 != (m_flags & enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_VALUE); }
        }

        public bool RequestFullName {
            [DebuggerStepThrough] get { return 0 != (m_flags & enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_FULLNAME); }
        }

        public bool RequestAttrib {
            [DebuggerStepThrough]get { return 0 != (m_flags & enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_ATTRIB); }
        }

        public bool RequestProperty {
            [DebuggerStepThrough]get { return 0 != (m_flags & enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_PROP); }
        }

        public bool RequestType {
            [DebuggerStepThrough]get { return 0 != (m_flags & enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_TYPE); }
        }

        public static enum_DEBUGPROP_INFO_FLAGS AllRequestFlags {
            [DebuggerStepThrough]
            get {
                enum_DEBUGPROP_INFO_FLAGS reqFlags =
                 enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_NAME |
                 enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_FULLNAME |
                 enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_ATTRIB |
                 enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_TYPE |
                 enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_VALUE |
                 enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_PROP;
                return reqFlags;
            }
        }

        public enum_DEBUGPROP_INFO_FLAGS NonRequestFlags {
            [DebuggerStepThrough]get {return m_flags & (~DebuggerRequestInfo.AllRequestFlags);}
        }

        public DebuggerRequestInfo(uint flags, uint radix, uint timeout)
            : this((enum_DEBUGPROP_INFO_FLAGS)flags, radix, timeout) {}

        public DebuggerRequestInfo(enum_DEBUGPROP_INFO_FLAGS flags, uint radix, uint timeout) {
            m_flags = flags;
            m_radix = radix;
            m_timeout = timeout;
        }

        /// <summary>
        /// Copy the current request for new data but keep all of the non request flags 
        /// such as nofunceval
        /// </summary>
        /// <param name="flags"></param>
        /// <returns></returns>
        public DebuggerRequestInfo CreateSubRequest(enum_DEBUGPROP_INFO_FLAGS flags) {
            flags |= this.NonRequestFlags;
            return new DebuggerRequestInfo(flags, Radix, Timeout);
        }
    }

    /// <summary>Variaos contract-enforcement (check) methods</summary>
    internal static class Contract {
        [DebuggerStepThrough]
        public static void ThrowIfNull<T>(T value)
            where T:class {
            ThrowIfNull(value, "Unexpected null");
        }
        [DebuggerStepThrough]
        public static void ThrowIfNull<T>(T value, string message) {
            if(null == value) {
                Violation(message);
            }
        }
        [DebuggerStepThrough]
        public static void ThrowIfFalse(bool value) {
            ThrowIfFalse(value, "Unexpected false");
        }
        [DebuggerStepThrough]
        public static void ThrowIfFalse(bool value, string message) {
            if(!value) {
                Violation(message);
            }
        }
        [DebuggerStepThrough]
        public static void ThrowIfTrue(bool value) {
            ThrowIfTrue(value, "Unexpected true");
        }
        [DebuggerStepThrough]
        public static void ThrowIfTrue(bool value, string message) {
            if(value) {
                Violation(message);
            }
        }
        [DebuggerStepThrough]
        private static void Violation(string message) {
            Debug.Assert(false, message);
            throw new ContractException(message);
        }
    }

    /// <summary>Thrown when method contract is violated</summary>
    [global::System.Serializable]
    public class ContractException:Exception {
        //
        // For guidelines regarding the creation of new exception types, see
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
        // and
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
        //
        [DebuggerStepThrough]
        public ContractException() { }
        [DebuggerStepThrough]
        public ContractException(string message) : base(message) { }
        [DebuggerStepThrough]
        public ContractException(string message, Exception inner) : base(message, inner) { }
        protected ContractException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }

    /// <summary>Guids for member filtering</summary>
    internal class FilterGuids {
        public static Guid Locals = new Guid("B200F725-E725-4C53-B36A-1EC27AEF12EF");
        public static Guid AllLocals = new Guid("196DB21F-5F22-45A9-B5A3-32CDDB30DB06");
        public static Guid Args = new Guid("804BCCEA-0475-4AE7-8A46-1862688AB863");
        public static Guid LocalsPlusArgs = new Guid("E74721BB-10C0-40F5-807F-920D37F95419");
        public static Guid AllLocalsPlusArgs = new Guid("00000000-0000-0000-0000-000000000000");
        public static Guid Registers = new Guid("00000000-0000-0000-0000-000000000000");
        public static Guid This = new Guid("ADD901FD-BFC9-48B2-B0C7-68B459539D7A");
    }
}
