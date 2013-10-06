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
    #region InclusionTypes

    /// <summary>
    /// Type of inclusion.
    /// </summary>
    /// <remarks>
    /// The properties of inclusion types are defined by IsXxxInclusion methods.
    /// </remarks>
    public enum InclusionTypes
    {
        Include, IncludeOnce, Require, RequireOnce, Prepended, Appended, RunSilverlight
    }

    #endregion
}
