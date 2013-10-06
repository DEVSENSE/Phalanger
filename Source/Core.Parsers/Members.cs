/*

 Copyright (c) 2007- DEVSENSE
 Copyright (c) 2006 Tomas Matousek.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;

namespace PHP.Core
{
    #region PhpMemberAttributes

    [Flags]
    public enum PhpMemberAttributes : short
    {
        None = 0,

        Public = 0,
        Private = 1,
        Protected = 2,
        NamespacePrivate = Private,

        Static = 4,
        AppStatic = Static | 8,
        Abstract = 16,
        Final = 32,

        /// <summary>
        /// The type is an interface.
        /// </summary>
        Interface = 64,

        /// <summary>
        /// The type is a trait.
        /// </summary>
        Trait = 128,

        /// <summary>
        /// The member is a constructor.
        /// </summary>
        Constructor = 256,

        /// <summary>
        /// The member is imported type, function or global constant with ambiguous fully qualified name.
        /// </summary>
        Ambiguous = 1024,

        /// <summary>
        /// The member needs to be activated before it can be resolved.
        /// TODO: useful when analysis checks whether there are any imported conditional types/functions.
        /// TODO: add the first conditional to the AC, ignore the others. Add the flag handling to Resolve* and to analyzer.
        /// </summary>
        InactiveConditional = 2048,

        StaticMask = Static | AppStatic,
        VisibilityMask = Public | Private | Protected | NamespacePrivate,
        SpecialMembersMask = Constructor,
        PartialMerged = Abstract | Final
    }

    #endregion
}
