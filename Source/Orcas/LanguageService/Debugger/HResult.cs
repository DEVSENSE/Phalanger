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
using System.Runtime.InteropServices;
using System.Diagnostics;
using Microsoft.VisualStudio;

namespace PHP.VisualStudio.PhalangerLanguageService.Debugger
{
    /// <summary>Contains varuaos HResult values</summary>
    internal static class HResult
    {
        /// <summary>Generic HResult for success</summary>
        /// <seealso cref="VSConstants.S_OK"/>
        public const int S_OK = VSConstants.S_OK;
        /// <summary>HResult for false</summary>
        /// <seealso cref="VSConstants.S_FALSE"/>
        public const int S_FALSE = VSConstants.S_FALSE;
        /// <summary>Evaluation exception</summary>
        public const int S_EVAL_EXCEPTION = 0x40002;
        /// <summary>A return value that indicates that the result of the method call is outside of the error cases the client code can readily handle.</summary>
        /// <seealso cref="VSConstants.E_UNEXPECTED"/>
        public const int E_UNEXPECTED = VSConstants.E_UNEXPECTED;//unchecked((int)0x8000FFFF);
        /// <summary>Error HRESULT for the call to a method that is not implemented.</summary>
        /// <seealso cref="VSConstants.E_NOTIMPL"/>
        public const int E_NOTIMPL = VSConstants.E_NOTIMPL;// unchecked((int)0x80004001);
        /// <summary>Error HRESULT for out of memory.</summary>
        /// <seealso cref="E_OUTOFMEMORY"/>
        public const int E_OUTOFMEMORY = VSConstants.E_OUTOFMEMORY;// unchecked((int)0x8007000E);
        /// <summary>Error HRESULT for an invalid argument.</summary>
        /// <seealso cref="VSConstants.E_INVALIDARG"/>
        public const int E_INVALIDARG = VSConstants.E_INVALIDARG;// unchecked((int)0x80070057);
        /// <summary>Error HRESULT for the request of a not implemented interface.</summary>
        /// <seealso cref="VSConstants.E_NOINTERFACE"/>
        public const int E_NOINTERFACE = VSConstants.E_NOINTERFACE;// unchecked((int)0x80004002);
        /// <summary>A return value that indicates that an invalid pointer, usually null, was passed as a parameter.</summary>
        /// <seealso cref="VSConstants.E_POINTER"/>
        public const int E_POINTER = VSConstants.E_POINTER;// unchecked((int)0x80004003);
        /// <summary>A return value that indicates an invalid handle.</summary>
        /// <seealso cref="VSConstants.E_HANDLE"/>
        /// <seealso cref="VSConstants.E_HANDLE"/>
        public const int E_HANDLE = VSConstants.E_HANDLE;// unchecked((int)0x80070006);
        /// <summary>A return value that may indicate an explicit cancellation action or some process that could no longer proceed after (for instance) both undo and rollback failed.</summary>
        /// <seealso cref="VSConstants.E_ABORT"/>
        public const int E_ABORT = VSConstants.E_ABORT;// unchecked((int)0x80004004);
        /// <summary>Error HRESULT for a generic failure.</summary>
        /// <seealso cref="VSConstants.E_FAIL"/>
        public const int E_FAIL = VSConstants.E_FAIL;// unchecked((int)0x80004005);
        /// <summary>A return value that describes a general access denied error.</summary>
        /// <seealso cref="VSConstants.E_ACCESSDENIED"/>
        public const int E_ACCESSDENIED = VSConstants.E_ACCESSDENIED;// unchecked((int)0x80070005);
        /// <summary>A return value that describes attempt to access non-existent member</summary>
        public const int DISP_E_MEMBERNOTFOUND = unchecked((int)0x80020003);
    }

    /// <summary>Contains native static methods for debugger support</summary>
    internal static class NativeMethods
    {
        /// <summary>Contains value of the <see cref="LastFailure"/> property</summary>
        [ThreadStatic]
        private static int m_lastFailure;
        /// <summary>Gets last result with failüre meaning passed to <see cref="Failed"/></summary>
        /// <returns>Last result passed to <see cref="Failed"/> with fauilture meaning</returns>
        public static int LastFailure{
            [DebuggerStepThrough] get { return m_lastFailure; }
        }

        /// <summary>test if given result is success</summary>
        /// )<param name="hr">Result to test</param>
        /// <returns>True when <paramref name="hr"/> is greater than or equal to zero; false otherwise.</returns>
        [DebuggerStepThrough]
        public static bool Succeeded(int hr){
            return (hr >= 0);
        }

        /// <summary>Test if given result is failure and if so, remembers it</summary>
        /// <param name="hr">Result to test</param>
        /// <returns>True when <paramref name="hr"/> is less tha zero; false otherwise</returns>
        /// <remarks>Last failure is remembered in <see cref="LastFailure"/></remarks>
        [DebuggerStepThrough]
        public static bool Failed(int hr){
            if (hr < 0){
                m_lastFailure = hr;
                return true;
            }
            return false;
        }
        /// <summary>Throws an exception if result is faliure</summary>
        /// <param name="hr">Result to test</param>
        /// <exception cref="Exception">When <paramref name="hr"/> means error, appropriate exception is thrown.</exception>
        [DebuggerStepThrough]
        public static void ThrowOnFailure(int hr){
            if (Failed(hr)){
                Marshal.ThrowExceptionForHR(hr);
            }
        }

    }
}
