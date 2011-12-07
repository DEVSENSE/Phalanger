/*

 Copyright (c) 2011 Jakub Misek

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/


using System;
using System.Collections;
using System.Collections.Generic;
using PHP.Core;
using PHP.Core.Reflection;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Runtime.InteropServices;

namespace PHP.Library.SPL
{
    /// <summary>
    /// The SplObserver interface is used alongside <see cref="SplSubject"/> to implement the Observer Design Pattern.
    /// </summary>
    [ImplementsType]
    public interface SplObserver
    {
        [ImplementsMethod]
        object update(ScriptContext/*!*/context, object/*SplSubject*/subject);
    }

    /// <summary>
    /// The SplSubject interface is used alongside <see cref="SplObserver"/> to implement the Observer Design Pattern.
    /// </summary>
    [ImplementsType]
    public interface SplSubject
    {
        [ImplementsMethod]
        object attach ( ScriptContext/*!*/context, object/*SplObserver*/observer );
        [ImplementsMethod]
        object detach ( ScriptContext/*!*/context, object/*SplObserver*/observer );
        [ImplementsMethod]
        object notify(ScriptContext/*!*/context);
    }
}
