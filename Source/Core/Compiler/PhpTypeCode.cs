using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PHP.Core
{
    /// <summary>
    /// Type codes of Phalanger special variables.
    /// </summary>
    public enum PhpTypeCode : byte
    {
        /// <summary>The type code of the <see cref="string"/> type.</summary>
        String,
        /// <summary>The type code of the <see cref="int"/> type.</summary>
        Integer,
        /// <summary>The type code of the <see cref="long"/> type.</summary>
        LongInteger,
        /// <summary>The type code of the <see cref="bool"/> type.</summary>
        Boolean,
        /// <summary>The type code of the <see cref="double"/> type.</summary>
        Double,

        /// <summary>The type code of the <see cref="object"/> type and of a <B>null</B> reference.</summary>
        Object,
        /// <summary>The type code of the <see cref="object"/>&amp; type.</summary>
        ObjectAddress,

        /// <summary>The type code of the <see cref="PHP.Core.PhpReference"/> type.</summary>
        PhpReference,
        /// <summary>The type code of the types assignable to <see cref="PHP.Core.PhpArray"/> type.</summary>
        PhpArray,
        /// <summary>The type code of the types assignable to <see cref="PHP.Core.Reflection.DObject"/> type.</summary>
        DObject,
        /// <summary>The type code of the types assignable to <see cref="PHP.Core.PhpResource"/> type.</summary>
        PhpResource,
        /// <summary>The type code of the <see cref="PHP.Core.PhpBytes"/> type.</summary>
        PhpBytes,
        /// <summary>The type code of the <see cref="PHP.Core.PhpString"/> type.</summary>
        PhpString,
        /// <summary>The type code of the <see cref="PHP.Core.PhpRuntimeChain"/> type.</summary>
        PhpRuntimeChain,

        /// <summary>The type code of a callable PHP object. Used as a type hint only.</summary>
        PhpCallable,

        /// <summary>The type code of the types which are not PHP.NET ones.</summary>
        Invalid,
        /// <summary>The type code of the <see cref="System.Void"/> type.</summary>
        Void,
        /// <summary>An unknown type. Means the type cannot or shouldn't be determined.</summary>
        Unknown
    }
}
