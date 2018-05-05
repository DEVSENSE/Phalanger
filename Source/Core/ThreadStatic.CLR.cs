/*

 Copyright (c) 2015-2016 Kendall Bennett
  
 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Diagnostics;
using System.Runtime.Remoting.Messaging;
using System.Web;

namespace PHP.Core
{
    /// <summary>
    /// Global property collection to maintain thread static variables
    /// </summary>
    public static class ThreadStatic
    {
        private const string httpContextItemsName = "PhpNet:RequestStatic";

        // Thread static variable to store the value for non-web code. We do not want to use call context
        // names slots, because stupid .net will *clone* those over for async I/O operations and you will end up
        // with a memory leak as lots of I/O competion handles will end up with copies of our properties collection.
        // Not good. So for web apps we use HttpContext.Current.Items and for console apps we use thread static.
#if !SILVERLIGHT
        // TODO: Silverlight does not support ThreadStatic, so just use regular static variables for now
        [ThreadStatic]
#endif
        private static PropertyCollectionClass threadStatic;

        /// <summary>
        /// Thread local properties collection
        /// </summary>
        [DebuggerNonUserCode]
        public static PropertyCollectionClass /*!*/ Properties
        {
            [Emitted]
            get
            {
                try
                {
                    var httpContext = HttpContext.Current;
                    if (httpContext != null)
                    {
                        // The only safe way to do request local variables in ASP.NET is to put them into the 
                        // HttpContext.Current.Items dictionary.
                        var items = httpContext.Items;
                        var properties = (PropertyCollectionClass) items[httpContextItemsName];
                        if (properties == null)
                        {
                            properties = new PropertyCollectionClass();
                            items[httpContextItemsName] = properties;
                        }
                        return properties;
                    }
                    else
                    {
                        // For console apps, use the thread static storage to keep it thread local
                        var properties = threadStatic;
                        if (properties == null) {
                            threadStatic = properties = new PropertyCollectionClass();
                        }
                        return properties;
                    }
                }
                catch (InvalidCastException)
                {
                    throw new InvalidCallContextDataException(httpContextItemsName);
                }
            }
            set
            {
                var httpContext = HttpContext.Current;
                if (httpContext != null)
                    httpContext.Items[httpContextItemsName] = value;
                else
                    threadStatic = value;
            }
        }
    }
}
