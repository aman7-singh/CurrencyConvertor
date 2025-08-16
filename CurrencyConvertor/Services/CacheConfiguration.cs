using CurrencyConvertor.Services.Interfaces;
using CurrencyConvertor.Services.Enums;
using System;

namespace CurrencyConvertor.Services {
    /// <summary>
    /// Cache configuration implementation reading from App.config
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
                // Load eviction strategy from App.config
                var strategyString = GetAppSetting("CacheEvictionStrategy", "TimeBased");
                if (Enum.TryParse<CacheEvictionStrategy>(strategyString, true, out var strategy)) {
                    EvictionStrategy = strategy;
                } else {
                    EvictionStrategy = CacheEvictionStrategy.TimeBased;
                    _loggingService?.LogWarning($"Invalid cache eviction strategy '{strategyString}', using TimeBased");
                }

                // Load MAX_AGE for time-based eviction from App.config
                var maxAgeString = GetAppSetting("CacheMaxAgeMinutes", "30");
                if (int.TryParse(maxAgeString, out var maxAge) && maxAge > 0) {
                    MaxAgeMinutes = maxAge;
                } else {
                    MaxAgeMinutes = 30;
                    _loggingService?.LogWarning($"Invalid CacheMaxAgeMinutes '{maxAgeString}', using 30 minutes");
                }

                // Load MAX_ELEMENTS for size-based eviction from App.config
                var maxElementsString = GetAppSetting("CacheMaxElements", "100");
                if (int.TryParse(maxElementsString, out var maxElements) && maxElements > 0) {
                    MaxElements = maxElements;
                } else {
                    MaxElements = 100;
                    _loggingService?.LogWarning($"Invalid CacheMaxElements '{maxElementsString}', using 100 elements");
                }

                // Load cleanup interval from App.config
                var cleanupIntervalString = GetAppSetting("CacheCleanupIntervalMinutes", "5");
                if (int.TryParse(cleanupIntervalString, out var cleanupInterval) && cleanupInterval > 0) {
                    CleanupIntervalMinutes = cleanupInterval;
                } else {
                    CleanupIntervalMinutes = 5;
                    _loggingService?.LogWarning($"Invalid CacheCleanupIntervalMinutes '{cleanupIntervalString}', using 5 minutes");
                }

                // Load enabled flag from App.config
                var enabledString = GetAppSetting("CacheEnabled", "true");
                if (bool.TryParse(enabledString, out var enabled)) {
                    IsEnabled = enabled;
                } else {
                    IsEnabled = true;
                    _loggingService?.LogWarning($"Invalid CacheEnabled '{enabledString}', using true");
                }

                _loggingService?.LogInfo($"Cache configuration loaded from App.config: Strategy={EvictionStrategy}, MaxAge={MaxAgeMinutes}min, MaxElements={MaxElements}, Cleanup={CleanupIntervalMinutes}min, Enabled={IsEnabled}");
            } catch (Exception ex) {
                _loggingService?.LogError("Failed to load cache configuration from App.config, using defaults", ex);
                
                // Set safe defaults
                EvictionStrategy = CacheEvictionStrategy.TimeBased;
                MaxAgeMinutes = 30;
                MaxElements = 100;
                CleanupIntervalMinutes = 5;
                IsEnabled = true;
            }
        }

        /// <summary>
        /// Helper method to get app settings using reflection (works with .NET Framework 4.8)
        /// </summary>
        private string GetAppSetting(string key, string defaultValue) {
            try {
                // Use reflection to access ConfigurationManager
                var configManagerType = Type.GetType("System.Configuration.ConfigurationManager, System.Configuration, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
                if (configManagerType != null) {
                    var appSettingsProperty = configManagerType.GetProperty("AppSettings");
                    var appSettings = appSettingsProperty?.GetValue(null) as System.Collections.Specialized.NameValueCollection;
                    return appSettings?[key] ?? defaultValue;
                }
            } catch (Exception ex) {
                _loggingService?.LogWarning($"Could not read app setting '{key}': {ex.Message}");
            }
            
            return defaultValue;
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