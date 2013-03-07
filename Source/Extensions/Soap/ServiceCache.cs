using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Threading;
using PHP.Core;

namespace PHP.Library.Soap
{

    internal sealed class ServiceCache
    {
        internal struct MemoryCacheKey
        {
            string wsdlLocation;
            WsdlCache type;
            int contentHash;


            internal WsdlCache Type
            {
                get { return type; }
              }


            internal string WsdlLocation
            {
                get
                {
                    return wsdlLocation;
                }
            }

            internal string Key
            {
                get
                {
                    if (type == WsdlCache.None)
                        return wsdlLocation + "#" + contentHash;
                    else
                        return wsdlLocation;
                }
            }

            internal MemoryCacheKey(string wsdlLocation, WsdlCache type):
                this(wsdlLocation, 0, type)
            {
            }

            internal MemoryCacheKey(string wsdlLocation, int contentHash, WsdlCache type)
            {
                Debug.Assert((type == WsdlCache.None && contentHash != 0) ||
                             (type != WsdlCache.None && contentHash == 0));

                this.wsdlLocation = wsdlLocation;
                this.contentHash = contentHash;
                this.type = type;
            }

            public override int GetHashCode()
            {
                return Key.GetHashCode();
            }
        }


        internal static class MemoryCache
        {

       

            private static ReaderWriterLockSlim cacheLock = new ReaderWriterLockSlim();
            private static Dictionary<string, Assembly> innerCache = new Dictionary<string, Assembly>();

            public static Assembly Get(MemoryCacheKey key)
            {
                Assembly ass;
                cacheLock.EnterReadLock();
                try
                {
                    innerCache.TryGetValue(key.Key, out ass);
                    return ass;
                }
                finally
                {
                    cacheLock.ExitReadLock();
                }
            }

            public static void Add(MemoryCacheKey key, Assembly serviceAssembly)
            {
                Assembly ass;

                cacheLock.EnterWriteLock();
                try
                {

                    if (key.Type == WsdlCache.None)
                    {
                        if (innerCache.TryGetValue(key.WsdlLocation, out ass))
                        {
                            // key which has also content hash is present without contentHash
                            // that means different type of caching was used for this wsdl
                            // => delete this key
                            innerCache.Remove(key.WsdlLocation);
                        }
                    }

                    innerCache.Add(key.Key, serviceAssembly);
                }
                finally
                {
                    cacheLock.ExitWriteLock();
                }
            }

        }


        private WsdlCache type;

        private string wsdlLocation;
        private string wsdlContent;
        private string absoluteWsdlLocation;
        private int contentHash;
        private event CacheMissEvent cacheMiss;


        public delegate Assembly CacheMissEvent(string wsdlPath, string wsdlContent);


        public WsdlCache Type
        {
            get { return type; }
        }

        public string WsdlContent
        {
            get
            {
                init();
                return wsdlContent;
            }
        }

        public int ContentHash
        {
            get
            {
                init();
                return contentHash;
            }
        }

        public string AbsoluteWsdlLocation
        {
            get
            {
                init();
                return absoluteWsdlLocation;
            }
        }


        public ServiceCache(string wsdlLocation, WsdlCache type, CacheMissEvent cacheMiss)
        {
            this.type = type;
            this.wsdlLocation = wsdlLocation;
            this.cacheMiss = cacheMiss;
        }

        private void init()
        {
            if (wsdlContent == null)
            {
                wsdlContent = WsdlHelper.GetWsdlContent(wsdlLocation, out absoluteWsdlLocation);
                contentHash = wsdlContent.GetHashCode();
            }
        }

        private MemoryCacheKey Key
        {
            get
            {
                if (type == WsdlCache.None)
                {
                    return new MemoryCacheKey(wsdlLocation,ContentHash,type);
                }
                else
                    return new MemoryCacheKey(wsdlLocation,type);
            }
        }

        /// <summary>
        /// Returns assembly from cache if it's present. If not CacheMiss event is invoked and return argument
        /// is pushed to the cache
        /// </summary>
        public Assembly GetOrAdd()
        {
            //First try memory cache layer
            Assembly ass = MemoryCache.Get(Key);

            if (ass != null)
                return ass;

            //It wasn't found in memory cache, check the file
            ass = CompiledAssemblyCache.CheckCacheForAssembly(wsdlLocation, ContentHash);

            if (ass != null)// there was hit, save it to memory cache
                MemoryCache.Add(Key, ass);
            else
            {
                //Nothing in file cache, call cache miss event
                if (cacheMiss != null)
                {
                    ass = cacheMiss(AbsoluteWsdlLocation, WsdlContent);
                    if (ass != null)
                    {
                        MemoryCache.Add(Key, ass);

                        //rename temporary assembly in order to cache it for later use
                        CompiledAssemblyCache.RenameTempAssembly(ass.Location, wsdlLocation, ContentHash);
                    }
                }
            }

            return ass;
        }

    }






}