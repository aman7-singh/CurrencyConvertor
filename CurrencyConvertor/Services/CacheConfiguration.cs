using CurrencyConvertor.Services.Interfaces;
using System;

namespace CurrencyConvertor.Services {
    /// <summary>
    /// Cache configuration implementation with hardcoded defaults
    /// Part 2 of Celonis Challenge - Configurable cache parameters
    /// </summary>
    public class CacheConfiguration : ICacheConfiguration {
        private readonly ILoggingService _loggingService;

        public CacheConfiguration(ILoggingService loggingService = null) {
            _loggingService = loggingService;
            LoadConfiguration();
        }

        public CacheEvictionStrategy EvictionStrategy { get; private set; }
        public int MaxAgeMinutes { get; private set; }
        public int MaxElements { get; private set; }
        public int CleanupIntervalMinutes { get; private set; }
        public bool IsEnabled { get; private set; }

        private void LoadConfiguration() {
            try {
                // For demonstration purposes, using hardcoded configuration
                // In production, these would come from App.config or appsettings.json
                
                // Time-based eviction: Remove items older than MAX_AGE (30 minutes)
                EvictionStrategy = CacheEvictionStrategy.TimeBased;
                MaxAgeMinutes = 30; // MAX_AGE parameter as required
                
                // Size-based eviction parameters
                MaxElements = 100; // MAX_ELEMENTS parameter as required
                
                // Cache cleanup interval
                CleanupIntervalMinutes = 5;
                
                // Enable caching
                IsEnabled = true;

                _loggingService?.LogInfo($"Cache configuration loaded: Strategy={EvictionStrategy}, MaxAge={MaxAgeMinutes}min, MaxElements={MaxElements}, Cleanup={CleanupIntervalMinutes}min, Enabled={IsEnabled}");
                
                // Note: To switch between strategies, change EvictionStrategy property
                // For size-based eviction, use: EvictionStrategy = CacheEvictionStrategy.SizeBased;
            } catch (Exception ex) {
                _loggingService?.LogError("Failed to load cache configuration, using defaults", ex);
                
                // Set safe defaults
                EvictionStrategy = CacheEvictionStrategy.TimeBased;
                MaxAgeMinutes = 30;
                MaxElements = 100;
                CleanupIntervalMinutes = 5;
                IsEnabled = true;
            }
        }

        /// <summary>
        /// Updates the cache eviction strategy at runtime
        /// Demonstrates both required eviction strategies for Celonis Challenge
        /// </summary>
        /// <param name="strategy">New eviction strategy</param>
        public void UpdateEvictionStrategy(CacheEvictionStrategy strategy) {
            EvictionStrategy = strategy;
            _loggingService?.LogInfo($"Cache eviction strategy updated to: {strategy}");
        }

        /// <summary>
        /// Updates cache parameters at runtime
        /// </summary>
        /// <param name="maxAgeMinutes">MAX_AGE for time-based eviction</param>
        /// <param name="maxElements">MAX_ELEMENTS for size-based eviction</param>
        public void UpdateCacheParameters(int maxAgeMinutes, int maxElements) {
            MaxAgeMinutes = maxAgeMinutes > 0 ? maxAgeMinutes : MaxAgeMinutes;
            MaxElements = maxElements > 0 ? maxElements : MaxElements;
            _loggingService?.LogInfo($"Cache parameters updated: MaxAge={MaxAgeMinutes}min, MaxElements={MaxElements}");
        }
    }
}