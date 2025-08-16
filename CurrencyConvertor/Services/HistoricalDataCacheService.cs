using CurrencyConvertor.Models;
using CurrencyConvertor.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace CurrencyConvertor.Services {
    /// <summary>
    /// Specialized cache service for historical currency data
    /// Part 2 of Celonis Challenge - Thread-safe caching for repeated queries
    /// </summary>
    public class HistoricalDataCacheService : IHistoricalDataCacheService {
        private readonly ICacheService<string, List<HistoricalRate>> _cacheService;
        private readonly ILoggingService _loggingService;

        public HistoricalDataCacheService(
            ICacheService<string, List<HistoricalRate>> cacheService,
            ILoggingService loggingService = null) {
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _loggingService = loggingService;
        }

        public async Task<List<HistoricalRate>> GetHistoricalRatesAsync(string cacheKey, Func<Task<List<HistoricalRate>>> dataFactory) {
            try {
                // Try to get from cache first
                var cachedData = await _cacheService.GetAsync(cacheKey);
                if (cachedData != null && cachedData.Count > 0) {
                    _loggingService?.LogInfo($"Retrieved {cachedData.Count} historical rates from cache for key: {cacheKey}");
                    return cachedData;
                }

                // Cache miss - get data from the factory
                _loggingService?.LogInfo($"Cache miss for key: {cacheKey}, fetching from data source");
                var freshData = await dataFactory();

                // Store in cache for future use
                if (freshData != null && freshData.Count > 0) {
                    await _cacheService.SetAsync(cacheKey, freshData);
                    _loggingService?.LogInfo($"Cached {freshData.Count} historical rates for key: {cacheKey}");
                }

                return freshData ?? new List<HistoricalRate>();
            } catch (Exception ex) {
                _loggingService?.LogError($"Error getting historical rates for key: {cacheKey}", ex);
                throw;
            }
        }

        public async Task ClearAsync() {
            await _cacheService.ClearAsync();
            _loggingService?.LogInfo("Historical data cache cleared");
        }

        public async Task CleanupAsync() {
            if (_cacheService is ThreadSafeCacheService<string, List<HistoricalRate>> threadSafeCache) {
                await threadSafeCache.CleanupAsync();
                _loggingService?.LogInfo("Historical data cache cleanup completed");
            }
        }

        public CacheStatistics GetStatistics() {
            if (_cacheService is ThreadSafeCacheService<string, List<HistoricalRate>> threadSafeCache) {
                return threadSafeCache.GetStatistics();
            }
            return new CacheStatistics();
        }

        // Additional helper methods for cache key generation
        public string GenerateKey(string fromCurrency, string toCurrency, DateTime startDate, DateTime endDate) {
            if (string.IsNullOrWhiteSpace(fromCurrency) || string.IsNullOrWhiteSpace(toCurrency)) {
                throw new ArgumentException("Currency codes cannot be null or empty");
            }

            // Create a deterministic key based on the query parameters
            var keyData = $"{fromCurrency.ToUpperInvariant()}_{toCurrency.ToUpperInvariant()}_{startDate:yyyy-MM-dd}_{endDate:yyyy-MM-dd}";
            
            // Optionally hash the key to ensure consistent length and avoid special characters
            using (var sha256 = SHA256.Create()) {
                var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(keyData));
                var hashString = Convert.ToBase64String(hashBytes).Replace("/", "_").Replace("+", "-").Replace("=", "");
                
                // Keep the readable part for debugging and add hash for uniqueness
                var cacheKey = $"HIST_{keyData}_{hashString.Substring(0, 8)}";
                
                _loggingService?.LogInfo($"Generated cache key: {cacheKey}");
                return cacheKey;
            }
        }
    }
}