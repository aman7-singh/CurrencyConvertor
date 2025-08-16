using CurrencyConvertor.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CurrencyConvertor.Services.Interfaces {
    /// <summary>
    /// Interface for cache operations - Part 2 of Celonis Challenge
    /// Thread-safe caching with configurable eviction strategies
    /// </summary>
    public interface ICacheService<TKey, TValue> {
        /// <summary>
        /// Gets an item from the cache
        /// </summary>
        /// <param name="key">Cache key</param>
        /// <returns>Cached value or null if not found</returns>
        Task<TValue> GetAsync(TKey key);
        
        /// <summary>
        /// Adds or updates an item in the cache
        /// </summary>
        /// <param name="key">Cache key</param>
        /// <param name="value">Value to cache</param>
        Task SetAsync(TKey key, TValue value);
        
        /// <summary>
        /// Removes an item from the cache
        /// </summary>
        /// <param name="key">Cache key</param>
        Task RemoveAsync(TKey key);
        
        /// <summary>
        /// Clears all items from the cache
        /// </summary>
        Task ClearAsync();
        
        /// <summary>
        /// Gets cache statistics
        /// </summary>
        CacheStatistics GetStatistics();
        
        /// <summary>
        /// Manually triggers cache cleanup
        /// </summary>
        Task CleanupAsync();
    }

    /// <summary>
    /// Specialized interface for historical data caching
    /// </summary>
    public interface IHistoricalDataCacheService : ICacheService<string, List<HistoricalRate>> {
        /// <summary>
        /// Generates a cache key for historical data queries
        /// </summary>
        /// <param name="fromCurrency">Source currency</param>
        /// <param name="toCurrency">Target currency</param>
        /// <param name="startDate">Start date</param>
        /// <param name="endDate">End date</param>
        /// <returns>Cache key</returns>
        string GenerateKey(string fromCurrency, string toCurrency, DateTime startDate, DateTime endDate);
    }

    /// <summary>
    /// Interface for cache configuration
    /// </summary>
    public interface ICacheConfiguration {
        /// <summary>
        /// Cache eviction strategy
        /// </summary>
        CacheEvictionStrategy EvictionStrategy { get; }
        
        /// <summary>
        /// Maximum age for time-based eviction (in minutes)
        /// </summary>
        int MaxAgeMinutes { get; }
        
        /// <summary>
        /// Maximum number of elements for size-based eviction
        /// </summary>
        int MaxElements { get; }
        
        /// <summary>
        /// Cache cleanup interval (in minutes)
        /// </summary>
        int CleanupIntervalMinutes { get; }
        
        /// <summary>
        /// Whether caching is enabled
        /// </summary>
        bool IsEnabled { get; }
    }

    /// <summary>
    /// Cache eviction strategies as required by Celonis Challenge
    /// </summary>
    public enum CacheEvictionStrategy {
        /// <summary>
        /// Time-based eviction: Remove items older than MAX_AGE
        /// </summary>
        TimeBased,
        
        /// <summary>
        /// Size-based eviction: Remove oldest items when exceeding MAX_ELEMENTS
        /// </summary>
        SizeBased
    }

    /// <summary>
    /// Cache statistics for monitoring
    /// </summary>
    public class CacheStatistics {
        public int TotalItems { get; set; }
        public int HitCount { get; set; }
        public int MissCount { get; set; }
        public double HitRatio => TotalRequests > 0 ? (double)HitCount / TotalRequests : 0.0;
        public int TotalRequests => HitCount + MissCount;
        public DateTime LastCleanup { get; set; }
        public int ItemsEvicted { get; set; }
        public long MemoryUsageBytes { get; set; }
        
        public override string ToString() {
            return $"Cache: {TotalItems} items, {HitRatio:P1} hit ratio ({HitCount}/{TotalRequests} requests), Last cleanup: {LastCleanup:HH:mm:ss}";
        }
    }
}