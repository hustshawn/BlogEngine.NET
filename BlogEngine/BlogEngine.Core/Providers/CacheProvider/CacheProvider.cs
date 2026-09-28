using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Extensions.Caching.Memory;

namespace BlogEngine.Core.Providers.CacheProvider
{
    /// <summary>
    /// Abstracts away access to the cache using IMemoryCache.
    /// Replaces the original System.Web.Caching.Cache wrapper.
    /// </summary>
    public class CacheProvider : CacheBase
    {
        private readonly IMemoryCache _cache;
        private readonly string _keyPrefix;

        // Track keys so we can enumerate/reset — IMemoryCache has no enumeration API.
        private readonly ConcurrentDictionary<string, byte> _trackedKeys = new ConcurrentDictionary<string, byte>();

        /// <summary>
        /// Creates a new CacheProvider using the specified IMemoryCache, scoped to one blog instance.
        /// </summary>
        public CacheProvider(IMemoryCache cache, Guid blogId)
        {
            _cache = cache;
            _keyPrefix = blogId + "_";
        }

        /// <summary>
        /// Gets the approximate number of items in the cache for this blog instance.
        /// </summary>
        public override int Count => _trackedKeys.Count;

        /// <summary>
        /// Gets or sets the cache item at the specified key.
        /// </summary>
        public override object this[string key]
        {
            get => _cache.Get(_keyPrefix + key);
            set
            {
                _cache.Set(_keyPrefix + key, value);
                _trackedKeys[_keyPrefix + key] = 0;
            }
        }

        /// <summary>
        /// Adds the specified item to the cache.
        /// </summary>
        public override object Add(string key, object value, DateTime absoluteExpiration, TimeSpan slidingExpiration)
        {
            var fullKey = _keyPrefix + key;
            var existing = _cache.Get(fullKey);
            if (existing != null)
                return existing;

            var options = new MemoryCacheEntryOptions();
            if (absoluteExpiration != DateTime.MaxValue && absoluteExpiration > DateTime.MinValue)
                options.SetAbsoluteExpiration(absoluteExpiration);
            if (slidingExpiration > TimeSpan.Zero)
                options.SetSlidingExpiration(slidingExpiration);

            options.RegisterPostEvictionCallback((k, v, reason, state) =>
            {
                _trackedKeys.TryRemove(k.ToString(), out _);
            });

            _cache.Set(fullKey, value, options);
            _trackedKeys[fullKey] = 0;
            return value;
        }

        /// <summary>
        /// Retrieves the specified item from the cache.
        /// </summary>
        public override object Get(string key)
        {
            return _cache.Get(_keyPrefix + key);
        }

        /// <summary>
        /// Gets an enumerator. Note: IMemoryCache doesn't support enumeration natively.
        /// This returns a simple enumerator over tracked keys.
        /// </summary>
        public override IDictionaryEnumerator GetEnumerator()
        {
            var dict = new Dictionary<string, object>();
            foreach (var kvp in _trackedKeys)
            {
                var val = _cache.Get(kvp.Key);
                if (val != null)
                    dict[kvp.Key] = val;
            }
            return ((IDictionary)dict).GetEnumerator();
        }

        /// <summary>
        /// Inserts an item into the cache using default expiration.
        /// </summary>
        public override void Insert(string key, object value)
        {
            var fullKey = _keyPrefix + key;
            var options = new MemoryCacheEntryOptions()
                .RegisterPostEvictionCallback((k, v, reason, state) =>
                {
                    _trackedKeys.TryRemove(k.ToString(), out _);
                });

            _cache.Set(fullKey, value, options);
            _trackedKeys[fullKey] = 0;
        }

        /// <summary>
        /// Inserts an item with expiration policies.
        /// </summary>
        public override void Insert(string key, object value, DateTime absoluteExpiration, TimeSpan slidingExpiration)
        {
            var fullKey = _keyPrefix + key;
            var options = new MemoryCacheEntryOptions();
            if (absoluteExpiration != DateTime.MaxValue && absoluteExpiration > DateTime.MinValue)
                options.SetAbsoluteExpiration(absoluteExpiration);
            if (slidingExpiration > TimeSpan.Zero)
                options.SetSlidingExpiration(slidingExpiration);

            options.RegisterPostEvictionCallback((k, v, reason, state) =>
            {
                _trackedKeys.TryRemove(k.ToString(), out _);
            });

            _cache.Set(fullKey, value, options);
            _trackedKeys[fullKey] = 0;
        }

        /// <summary>
        /// Removes the specified item from the cache.
        /// </summary>
        public override object Remove(string key)
        {
            var fullKey = _keyPrefix + key;
            var value = _cache.Get(fullKey);
            _cache.Remove(fullKey);
            _trackedKeys.TryRemove(fullKey, out _);
            return value;
        }

        /// <summary>
        /// Resets the cache by removing all items belonging to this blog instance.
        /// </summary>
        public override void Reset()
        {
            foreach (var key in _trackedKeys.Keys)
            {
                if (key.StartsWith(_keyPrefix))
                {
                    _cache.Remove(key);
                    _trackedKeys.TryRemove(key, out _);
                }
            }
        }
    }
}
