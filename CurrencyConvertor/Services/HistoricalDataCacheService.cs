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

        public async Task<List<HistoricalRate>> GetAsync(string key) {
            return await _cacheService.GetAsync(key);
        }

        public async Task SetAsync(string key, List<HistoricalRate> value) {
            await _cacheService.SetAsync(key, value);
        }

        public async Task RemoveAsync(string key) {
            await _cacheService.RemoveAsync(key);
        }

        public async Task ClearAsync() {
            await _cacheService.ClearAsync();
        }

        public CacheStatistics GetStatistics() {
            return _cacheService.GetStatistics();
        }

        public async Task CleanupAsync() {
            await _cacheService.CleanupAsync();
        }

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