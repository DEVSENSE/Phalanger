/*

 Copyright (c) 2004-2006 Tomas Matousek and Ladislav Prosek.
  
 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Diagnostics;

namespace PHP.Core
{
#if DEBUG

    // GENERICS: replace with VSUnit
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class TestAttribute : Attribute
    {
        public TestAttribute()
        {
            this.One = false;
        }

        public TestAttribute(bool one)
        {
            this.One = one;
        }

        public readonly bool One;
    }

#endif
}
