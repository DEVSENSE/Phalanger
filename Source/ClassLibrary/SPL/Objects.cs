/*

 Copyright (c) 2004-2006 Ladislav Prosek.  

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/
using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;

using PHP.Core;
using PHP.Core.Reflection;

namespace PHP.Library
{
	/// <summary>
	/// Contains object-related class library functions.
	/// </summary>
	/// <threadsafety static="true"/>
	public static class SplObjects
	{
		/// <summary>
        /// Return hash id for given object.
        /// </summary>
        /// <param name="obj">Object instance to get its hash code.</param>
        /// <returns>Hexadecimal number as string.</returns>
        [ImplementsFunction("spl_object_hash")]
        public static string ObjectHash(object obj)
        {
            DObject dobj = obj as DObject;

            if (dobj == null)
            {
                PhpException.Throw(PhpError.Warning, LibResources.GetString("unexpected_arg_given", 1, DObject.PhpTypeName, PhpVariable.GetTypeName(obj).ToLower()));
                return null;
            }

            return dobj.GetHashCode().ToString("x32");
        }

        /// <summary>
        /// This function returns an array with the current available SPL classes.
        /// </summary>
        /// <returns>Returns an array containing the currently available SPL classes.</returns>
        [ImplementsFunction("spl_classes")]
        public static PhpArray/*!*/SplClasses()
        {
            var array = new PhpArray(32);

            // TODO: (J) list SPL classes http://www.php.net/manual/en/function.spl-classes.php
            // array.Add( "class_name", "class_name" );

            return array;
        }

	}
}
