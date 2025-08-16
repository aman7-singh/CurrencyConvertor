using CurrencyConvertor.Models;
using CurrencyConvertor.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CurrencyConvertor.Services {
    /// <summary>
    /// Cached wrapper for historical data service - Part 2 of Celonis Challenge
    /// Implements query caching with thread-safe access and configurable eviction
    /// </summary>
    public class CachedHistoricalDataService : IHistoricalDataService, IDisposable {
        private readonly IHistoricalDataService _baseService;
        private readonly IHistoricalDataCacheService _cacheService;
        private readonly ILoggingService _loggingService;
        private readonly ICacheConfiguration _cacheConfiguration;

        public CachedHistoricalDataService(
            IHistoricalDataService baseService,
            IHistoricalDataCacheService cacheService,
            ICacheConfiguration cacheConfiguration,
            ILoggingService loggingService = null) {
            _baseService = baseService ?? throw new ArgumentNullException(nameof(baseService));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _cacheConfiguration = cacheConfiguration ?? throw new ArgumentNullException(nameof(cacheConfiguration));
            _loggingService = loggingService;

            _loggingService?.LogInfo("CachedHistoricalDataService initialized");
        }

        public async Task<List<HistoricalRate>> GetHistoricalRatesAsync(
            string fromCurrency, 
            string toCurrency, 
            DateTime startDate, 
            DateTime endDate) {
            
            _loggingService?.LogInfo($"Requesting historical data: {fromCurrency} -> {toCurrency}, {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");

            // If caching is disabled, go directly to the base service
            if (!_cacheConfiguration.IsEnabled) {
                _loggingService?.LogInfo("Cache disabled, fetching from base service");
                return await _baseService.GetHistoricalRatesAsync(fromCurrency, toCurrency, startDate, endDate);
            }

            try {
                // Generate cache key using the concrete implementation method
                var cacheKey = (_cacheService as HistoricalDataCacheService)?.GenerateKey(fromCurrency, toCurrency, startDate, endDate) 
                              ?? $"HIST_{fromCurrency}_{toCurrency}_{startDate:yyyy-MM-dd}_{endDate:yyyy-MM-dd}";

                // Use the cache service's GetHistoricalRatesAsync method
                return await _cacheService.GetHistoricalRatesAsync(cacheKey, async () => {
                    _loggingService?.LogInfo("Cache miss, fetching from API");
                    return await _baseService.GetHistoricalRatesAsync(fromCurrency, toCurrency, startDate, endDate);
                });
            } catch (Exception ex) {
                _loggingService?.LogError("Error in cached historical data service", ex);
                
                // Fall back to base service on cache errors
                _loggingService?.LogInfo("Falling back to base service due to cache error");
                return await _baseService.GetHistoricalRatesAsync(fromCurrency, toCurrency, startDate, endDate);
            }
        }

        /// <summary>
        /// Gets cache statistics for monitoring
        /// </summary>
        /// <returns>Current cache statistics</returns>
        public CacheStatistics GetCacheStatistics() {
            return _cacheService.GetStatistics();
        }

        /// <summary>
        /// Manually clears the cache
        /// </summary>
        public async Task ClearCacheAsync() {
            await _cacheService.ClearAsync();
            _loggingService?.LogInfo("Cache manually cleared");
        }

        /// <summary>
        /// Manually triggers cache cleanup
        /// </summary>
        public async Task CleanupCacheAsync() {
            await _cacheService.CleanupAsync();
            _loggingService?.LogInfo("Cache cleanup manually triggered");
        }

        public void Dispose() {
            try {
                (_cacheService as IDisposable)?.Dispose();
                (_baseService as IDisposable)?.Dispose();
                _loggingService?.LogInfo("CachedHistoricalDataService disposed");
            } catch (Exception ex) {
                _loggingService?.LogError("Error disposing CachedHistoricalDataService", ex);
            }
        }
    }
}