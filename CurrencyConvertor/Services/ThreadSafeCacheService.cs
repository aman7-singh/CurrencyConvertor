using CurrencyConvertor.Models;
using CurrencyConvertor.Services.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace CurrencyConvertor.Services {
    /// <summary>
    /// Thread-safe cache implementation for Celonis Challenge Part 2
    /// Supports both time-based and size-based eviction strategies
    /// </summary>
    public class ThreadSafeCacheService<TKey, TValue> : ICacheService<TKey, TValue>, IDisposable {
        private readonly ICacheConfiguration _configuration;
        private readonly ILoggingService _loggingService;
        private readonly ConcurrentDictionary<TKey, CacheItem<TValue>> _cache;
        private readonly ReaderWriterLockSlim _lock;
        private readonly System.Timers.Timer _cleanupTimer;
        private readonly CacheStatistics _statistics;
        
        private bool _disposed = false;

        public ThreadSafeCacheService(ICacheConfiguration configuration, ILoggingService loggingService = null) {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _loggingService = loggingService;
            _cache = new ConcurrentDictionary<TKey, CacheItem<TValue>>();
            _lock = new ReaderWriterLockSlim();
            _statistics = new CacheStatistics();
            
            // Set up automatic cleanup timer
            _cleanupTimer = new System.Timers.Timer(TimeSpan.FromMinutes(_configuration.CleanupIntervalMinutes).TotalMilliseconds);
            _cleanupTimer.Elapsed += OnCleanupTimer;
            _cleanupTimer.AutoReset = true;
            _cleanupTimer.Start();
            
            _loggingService?.LogInfo($"Cache service initialized with {_configuration.EvictionStrategy} strategy");
        }

        public async Task<TValue> GetAsync(TKey key) {
            if (!_configuration.IsEnabled) {
                _statistics.MissCount++;
                return default(TValue);
            }

            return await Task.Run(() => {
                _lock.EnterReadLock();
                try {
                    if (_cache.TryGetValue(key, out var cacheItem)) {
                        // Check if item is still valid
                        if (IsItemValid(cacheItem)) {
                            cacheItem.LastAccessed = DateTime.UtcNow;
                            _statistics.HitCount++;
                            _loggingService?.LogInfo($"Cache hit for key: {key}");
                            return cacheItem.Value;
                        } else {
                            // Item expired, remove it
                            _cache.TryRemove(key, out _);
                            _statistics.MissCount++;
                            _loggingService?.LogInfo($"Cache miss (expired) for key: {key}");
                            return default(TValue);
                        }
                    }
                    
                    _statistics.MissCount++;
                    _loggingService?.LogInfo($"Cache miss for key: {key}");
                    return default(TValue);
                } finally {
                    _lock.ExitReadLock();
                }
            });
        }

        public async Task SetAsync(TKey key, TValue value) {
            if (!_configuration.IsEnabled) {
                return;
            }

            await Task.Run(() => {
                _lock.EnterWriteLock();
                try {
                    var cacheItem = new CacheItem<TValue> {
                        Value = value,
                        CreatedAt = DateTime.UtcNow,
                        LastAccessed = DateTime.UtcNow
                    };

                    _cache.AddOrUpdate(key, cacheItem, (k, existing) => cacheItem);
                    
                    _loggingService?.LogInfo($"Cache set for key: {key}");
                    
                    // Check if we need to evict items based on size
                    if (_configuration.EvictionStrategy == CacheEvictionStrategy.SizeBased) {
                        EvictOldestItemsIfNeeded();
                    }
                } finally {
                    _lock.ExitWriteLock();
                }
            });
        }

        public async Task RemoveAsync(TKey key) {
            await Task.Run(() => {
                _cache.TryRemove(key, out _);
                _loggingService?.LogInfo($"Cache remove for key: {key}");
            });
        }

        public async Task ClearAsync() {
            await Task.Run(() => {
                _lock.EnterWriteLock();
                try {
                    var count = _cache.Count;
                    _cache.Clear();
                    _loggingService?.LogInfo($"Cache cleared, removed {count} items");
                } finally {
                    _lock.ExitWriteLock();
                }
            });
        }

        public CacheStatistics GetStatistics() {
            _lock.EnterReadLock();
            try {
                _statistics.TotalItems = _cache.Count;
                _statistics.MemoryUsageBytes = EstimateMemoryUsage();
                return new CacheStatistics {
                    TotalItems = _statistics.TotalItems,
                    HitCount = _statistics.HitCount,
                    MissCount = _statistics.MissCount,
                    LastCleanup = _statistics.LastCleanup,
                    ItemsEvicted = _statistics.ItemsEvicted,
                    MemoryUsageBytes = _statistics.MemoryUsageBytes
                };
            } finally {
                _lock.ExitReadLock();
            }
        }

        public async Task CleanupAsync() {
            await Task.Run(() => {
                _lock.EnterWriteLock();
                try {
                    var initialCount = _cache.Count;
                    var evictedCount = 0;

                    if (_configuration.EvictionStrategy == CacheEvictionStrategy.TimeBased) {
                        evictedCount = EvictExpiredItems();
                    } else if (_configuration.EvictionStrategy == CacheEvictionStrategy.SizeBased) {
                        evictedCount = EvictOldestItemsIfNeeded();
                    }

                    _statistics.LastCleanup = DateTime.UtcNow;
                    _statistics.ItemsEvicted += evictedCount;

                    if (evictedCount > 0) {
                        _loggingService?.LogInfo($"Cache cleanup completed: {evictedCount} items evicted ({initialCount} -> {_cache.Count})");
                    }
                } finally {
                    _lock.ExitWriteLock();
                }
            });
        }

        private bool IsItemValid(CacheItem<TValue> item) {
            if (_configuration.EvictionStrategy == CacheEvictionStrategy.TimeBased) {
                var age = DateTime.UtcNow - item.CreatedAt;
                return age.TotalMinutes <= _configuration.MaxAgeMinutes;
            }
            
            return true; // For size-based eviction, items are always valid until manually evicted
        }

        private int EvictExpiredItems() {
            var expiredKeys = new List<TKey>();
            var cutoffTime = DateTime.UtcNow.AddMinutes(-_configuration.MaxAgeMinutes);

            foreach (var kvp in _cache) {
                if (kvp.Value.CreatedAt < cutoffTime) {
                    expiredKeys.Add(kvp.Key);
                }
            }

            foreach (var key in expiredKeys) {
                _cache.TryRemove(key, out _);
            }

            return expiredKeys.Count;
        }

        private int EvictOldestItemsIfNeeded() {
            var evictedCount = 0;
            
            while (_cache.Count > _configuration.MaxElements) {
                // Find the oldest item by creation time
                var oldestItem = _cache
                    .OrderBy(kvp => kvp.Value.CreatedAt)
                    .FirstOrDefault();

                if (oldestItem.Key != null) {
                    if (_cache.TryRemove(oldestItem.Key, out _)) {
                        evictedCount++;
                    }
                } else {
                    break; // No more items to evict
                }
            }

            return evictedCount;
        }

        private long EstimateMemoryUsage() {
            // Simple estimation - in a real implementation, you might use more sophisticated memory measurement
            return _cache.Count * 1024; // Assume 1KB per cache item on average
        }

        private async void OnCleanupTimer(object sender, ElapsedEventArgs e) {
            try {
                await CleanupAsync();
            } catch (Exception ex) {
                _loggingService?.LogError("Error during automatic cache cleanup", ex);
            }
        }

        public void Dispose() {
            if (!_disposed) {
                _cleanupTimer?.Stop();
                _cleanupTimer?.Dispose();
                _lock?.Dispose();
                _cache?.Clear();
                _disposed = true;
                
                _loggingService?.LogInfo("Cache service disposed");
            }
        }
    }

    /// <summary>
    /// Cache item wrapper with metadata
    /// </summary>
    internal class CacheItem<T> {
        public T Value { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastAccessed { get; set; }
    }
}