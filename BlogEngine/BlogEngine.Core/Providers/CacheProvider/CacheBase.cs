using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Extensions.Caching.Memory;

namespace BlogEngine.Core.Providers.CacheProvider
{
    /// <summary>
    /// Cache Base - abstract wrapper around IMemoryCache for BlogEngine.
    /// </summary>
    public abstract class CacheBase : IEnumerable
    {
        /// <summary>Count of items tracked (approximate).</summary>
        public abstract int Count { get; }

        /// <summary>Not applicable for IMemoryCache. Returns 0.</summary>
        public virtual long EffectivePercentagePhysicalMemoryLimit => 0;

        /// <summary>Not applicable for IMemoryCache. Returns 0.</summary>
        public virtual long EffectivePrivateBytesLimit => 0;

        /// <summary>Gets or sets a cache item by key.</summary>
        public abstract object this[string key] { get; set; }

        /// <summary>Adds an item to the cache with expiration options.</summary>
        public abstract object Add(string key, object value, DateTime absoluteExpiration, TimeSpan slidingExpiration);

        /// <summary>Gets an item from the cache.</summary>
        public abstract object Get(string key);

        /// <summary>Gets an enumerator over cache keys (not fully supported with IMemoryCache).</summary>
        public abstract IDictionaryEnumerator GetEnumerator();

        /// <summary>Inserts an item into the cache.</summary>
        public abstract void Insert(string key, object value);

        /// <summary>Inserts an item with absolute and sliding expiration.</summary>
        public abstract void Insert(string key, object value, DateTime absoluteExpiration, TimeSpan slidingExpiration);

        /// <summary>Removes an item from the cache.</summary>
        public abstract object Remove(string key);

        /// <summary>Resets (clears) the cache.</summary>
        public abstract void Reset();

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
