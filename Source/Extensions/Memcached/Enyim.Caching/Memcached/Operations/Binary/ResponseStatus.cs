using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Enyim.Caching.Memcached.Operations.Binary
{
    /// <summary>
    /// Binary response statuses.
    /// </summary>
    internal enum ResponseStatus : short   // 2-byte field
    {
        Undefined = -1,

        NoError = 0x0000,
        KeyNotFound = 0x0001,
        KeyExists = 0x0002,
        ValueTooLarge = 0x0003,
        InvalidArguments = 0x0004,
        ItemNotStored = 0x0005,
        IncrDecrOnNonNumericValue = 0x0006,

        UnknownCommand = 0x0081,
        OutOfMemory = 0x0082,
    }
}
