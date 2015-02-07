using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace PHP.Core
{
    /// <summary>
    /// Manages active <see cref="PhpResource"/> instances across the current thread.
    /// </summary>
    internal sealed class PhpResourceManager
    {
        #region Fields & Properties

        /// <summary>
        /// Lazily initialized list of <see cref="PhpResource"/>s created during this web request.
        /// </summary>
        /// <remarks>
        /// The resources are disposed of when the request is over.
        /// <seealso cref="RegisterResource"/><seealso cref="CleanUpResources"/>
        /// </remarks>
        [ThreadStatic]
        private static LinkedList<WeakReference> resources;

        #endregion

        #region Construction

        static PhpResourceManager()
        {
            // Registers the cleanup function to the request end event,
            // so any pending resources will be automatically closed.
            RequestContext.RequestEnd += CleanUpResources;
        }

        #endregion

        #region RegisterResource, UnregisterResource, CleanupResources

        /// <summary>
        /// Registers a resource that should be disposed of when the request is over.
        /// </summary>
        /// <param name="res">The resource.</param>
        internal static LinkedListNode<WeakReference> RegisterResource(PhpResource/*!*/res)
        {
            Debug.Assert(res != null);
            //Debug.Assert(this method can only be called on the request thread)

            if (resources == null)
                resources = new LinkedList<WeakReference>();

            return resources.AddFirst(new WeakReference(res));
        }

        /// <summary>
        /// Unregisters disposed resource.
        /// </summary>
        internal static void UnregisterResource(LinkedListNode<WeakReference>/*!*/node)
        {
            Debug.Assert(node != null);
            
            node.List.Remove(node); // node.list == resources
        }

        /// <summary>
        /// Disposes of <see cref="PhpResource"/>s created during this web request.
        /// </summary>
        internal static void CleanUpResources()
        {
            if (resources != null)
            {
                for (var p = resources.First; p != null; )
                {
                    var next = p.Next;
                    if (p.Value.IsAlive)
                    {
                        var phpresource = (PhpResource)p.Value.Target;
                        if (phpresource != null)
                            phpresource.Close();
                    }
                    p = next;
                }

                resources = null;
            }
        }

        #endregion
    }
}
