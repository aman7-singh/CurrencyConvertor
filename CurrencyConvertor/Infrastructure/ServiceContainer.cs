using CurrencyConvertor.Services;
using CurrencyConvertor.Services.Interfaces;
using CurrencyConvertor.ViewModels;
using CurrencyConvertor.ViewModels.Interfaces;
using CurrencyConvertor.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CurrencyConvertor.Infrastructure {
    /// <summary>
    /// Simple dependency injection container following DIP (Dependency Inversion Principle)
    /// Enhanced with auto-refresh currency service and Part 2 caching functionality
    /// </summary>
    public class ServiceContainer {
        private static ServiceContainer _instance;
        private static readonly object Lock = new object();

        public static ServiceContainer Instance {
            get {
                if (_instance == null) {
                    lock (Lock) {
                        if (_instance == null)
                            _instance = new ServiceContainer();
                    }
                }
                return _instance;
            }
        }

        private ServiceContainer() {
            RegisterServices();
        }

        // Service registrations
        private ILoggingService _loggingService;
        private INotificationService _notificationService;
        private IValidationService _validationService;
        private ICurrencyService _currencyService;
        private ICurrencyMetadataService _metadataService;
        private ICurrencyConversionService _conversionService;
        private IHistoricalDataService _historicalDataService;
        private IInitializationService _initializationService;
        private CurrencyAutoRefreshService _autoRefreshService;

        // Part 2: Cache services for Celonis Challenge
        private ICacheConfiguration _cacheConfiguration;
        private ICacheService<string, List<HistoricalRate>> _cacheService;
        private IHistoricalDataCacheService _historicalCacheService;

        private void RegisterServices() {
            // Core services
            _loggingService = new ConsoleLoggingService();
            _notificationService = new WpfNotificationService();
            _validationService = new ValidationService(_loggingService);

            // Part 2: Cache configuration and services
            _cacheConfiguration = new CacheConfiguration(_loggingService);
            _cacheService = new ThreadSafeCacheService<string, List<HistoricalRate>>(_cacheConfiguration, _loggingService);
            _historicalCacheService = new HistoricalDataCacheService(_cacheService, _loggingService);

            // Currency services - using the same instance that implements multiple interfaces
            var baseCurrencyService = new CurrencyService(_loggingService);
            _currencyService = baseCurrencyService;
            _conversionService = baseCurrencyService;

            // Part 2: Wrap historical data service with caching
            _historicalDataService = new CachedHistoricalDataService(
                baseCurrencyService, 
                _historicalCacheService, 
                _cacheConfiguration, 
                _loggingService);

            // Resilient metadata service with fallback
            var fallbackMetadataService = new FallbackCurrencyMetadataService(_loggingService);
            _metadataService = new ResilientCurrencyMetadataService(
                baseCurrencyService, fallbackMetadataService, _loggingService);

            // Auto-refresh service
            _autoRefreshService = new CurrencyAutoRefreshService(_metadataService, _loggingService);

            // Initialization service
            _initializationService = new ApplicationInitializationService(_currencyService, _loggingService);

            _loggingService.LogInfo($"Services registered successfully with caching ({_cacheConfiguration.EvictionStrategy} strategy) and auto-refresh capability");
        }

        #region Service Getters

        public ILoggingService GetLoggingService() => _loggingService;
        public INotificationService GetNotificationService() => _notificationService;
        public IValidationService GetValidationService() => _validationService;
        public ICurrencyService GetCurrencyService() => _currencyService;
        public ICurrencyMetadataService GetMetadataService() => _metadataService;
        public ICurrencyConversionService GetConversionService() => _conversionService;
        public IHistoricalDataService GetHistoricalDataService() => _historicalDataService;
        public IInitializationService GetInitializationService() => _initializationService;
        public CurrencyAutoRefreshService GetAutoRefreshService() => _autoRefreshService;

        // Part 2: Cache service getters
        public ICacheConfiguration GetCacheConfiguration() => _cacheConfiguration;
        public IHistoricalDataCacheService GetHistoricalCacheService() => _historicalCacheService;

        #endregion

        #region ViewModel Factory Methods

        public ICurrencyConversionViewModel CreateConversionViewModel() {
            _loggingService.LogInfo("Creating CurrencyConversionViewModel with auto-refresh");
            return new CurrencyConversionViewModel(
                _conversionService,
                _metadataService,
                _validationService,
                _loggingService);
        }

        public IHistoricalDataViewModel CreateHistoricalDataViewModel() {
            _loggingService.LogInfo("Creating HistoricalDataViewModel with caching support");
            return new HistoricalDataViewModel(
                _historicalDataService,
                _validationService,
                _notificationService,
                _loggingService);
        }

        public IApplicationStatusViewModel CreateStatusViewModel() {
            _loggingService.LogInfo("Creating ApplicationStatusViewModel");
            return new ApplicationStatusViewModel(_loggingService);
        }

        public IMainViewModel CreateMainViewModel() {
            _loggingService.LogInfo("Creating MainViewModel with auto-refresh integration and caching");
            return new MainViewModel(
                CreateConversionViewModel(),
                CreateHistoricalDataViewModel(),
                CreateStatusViewModel(),
                _initializationService,
                _notificationService,
                _loggingService);
        }

        #endregion

        #region Auto-Refresh Management

        /// <summary>
        /// Starts auto-refresh with default 30-minute interval
        /// </summary>
        public void StartAutoRefresh() {
            _autoRefreshService.Start();
        }

        /// <summary>
        /// Starts auto-refresh with custom interval
        /// </summary>
        /// <param name="intervalMinutes">Refresh interval in minutes</param>
        public void StartAutoRefresh(int intervalMinutes) {
            _autoRefreshService.Start(TimeSpan.FromMinutes(intervalMinutes));
        }

        /// <summary>
        /// Stops auto-refresh
        /// </summary>
        public void StopAutoRefresh() {
            _autoRefreshService.Stop();
        }

        /// <summary>
        /// Manually triggers a currency refresh
        /// </summary>
        public async System.Threading.Tasks.Task<bool> RefreshCurrenciesAsync() {
            return await _autoRefreshService.RefreshNowAsync();
        }

        /// <summary>
        /// Gets auto-refresh statistics
        /// </summary>
        public RefreshStatistics GetAutoRefreshStatistics() {
            return _autoRefreshService.GetStatistics();
        }

        #endregion

        #region Part 2: Cache Management

        /// <summary>
        /// Gets cache statistics for monitoring
        /// </summary>
        public CacheStatistics GetCacheStatistics() {
            return _historicalCacheService.GetStatistics();
        }

        /// <summary>
        /// Manually clears the historical data cache
        /// </summary>
        public async System.Threading.Tasks.Task ClearCacheAsync() {
            await _historicalCacheService.ClearAsync();
            _loggingService.LogInfo("Cache manually cleared from ServiceContainer");
        }

        /// <summary>
        /// Manually triggers cache cleanup
        /// </summary>
        public async System.Threading.Tasks.Task CleanupCacheAsync() {
            await _historicalCacheService.CleanupAsync();
            _loggingService.LogInfo("Cache cleanup manually triggered from ServiceContainer");
        }

        #endregion

        #region Diagnostic Methods

        /// <summary>
        /// Test basic service functionality including caching for debugging
        /// </summary>
        public async System.Threading.Tasks.Task<bool> TestServicesAsync() {
            try {
                _loggingService.LogInfo("Starting service diagnostics with caching tests...");

                // Test validation service
                var isValidAmount = _validationService.IsValidAmount("1.0", out decimal amount);
                _loggingService.LogInfo($"Amount validation test: {isValidAmount}, Amount: {amount}");

                var isValidCurrency = _validationService.IsValidCurrency("EUR");
                _loggingService.LogInfo($"Currency validation test: {isValidCurrency}");

                // Test enhanced date validation
                TestDateValidation();

                // Test auto-refresh service
                TestAutoRefreshService();

                // Test cache functionality
                await TestCacheFunctionalityAsync();

                // Test currency metadata service
                var currencies = await _metadataService.GetAvailableCurrenciesAsync();
                _loggingService.LogInfo($"Currency metadata test: Retrieved {currencies.Count} currencies");

                if (currencies.Count > 1) {
                    // Test conversion service
                    var conversion = await _conversionService.ConvertCurrencyAsync(currencies[0], currencies[1], 1.0m);
                    _loggingService.LogInfo($"Conversion test: 1 {currencies[0]} = {conversion.ConvertedAmount} {currencies[1]}");
                }

                _loggingService.LogInfo("Service diagnostics completed successfully");
                return true;
            } catch (Exception ex) {
                _loggingService.LogError("Service diagnostics failed", ex);
                return false;
            }
        }

        /// <summary>
        /// Test cache functionality for Part 2 verification
        /// </summary>
        private async System.Threading.Tasks.Task TestCacheFunctionalityAsync() {
            _loggingService.LogInfo("Testing cache functionality (Part 2)...");

            try {
                // Test cache statistics
                var initialStats = GetCacheStatistics();
                _loggingService.LogInfo($"Initial cache stats: {initialStats}");

                // Test cache configuration
                _loggingService.LogInfo($"Cache configuration: Strategy={_cacheConfiguration.EvictionStrategy}, MaxAge={_cacheConfiguration.MaxAgeMinutes}min, MaxElements={_cacheConfiguration.MaxElements}, Enabled={_cacheConfiguration.IsEnabled}");

                // Test historical data caching if currencies are available
                var currencies = await _metadataService.GetAvailableCurrenciesAsync();
                if (currencies.Count >= 2) {
                    var fromCurrency = currencies[0];
                    var toCurrency = currencies[1];
                    var startDate = DateTime.Today.AddDays(-7);
                    var endDate = DateTime.Today;

                    _loggingService.LogInfo($"Testing historical data caching: {fromCurrency} -> {toCurrency}");

                    // First request - should be cache miss
                    var firstRequest = await _historicalDataService.GetHistoricalRatesAsync(fromCurrency, toCurrency, startDate, endDate);
                    var statsAfterFirst = GetCacheStatistics();
                    _loggingService.LogInfo($"After first request: {statsAfterFirst}");

                    // Second identical request - should be cache hit
                    var secondRequest = await _historicalDataService.GetHistoricalRatesAsync(fromCurrency, toCurrency, startDate, endDate);
                    var statsAfterSecond = GetCacheStatistics();
                    _loggingService.LogInfo($"After second request: {statsAfterSecond}");

                    // Verify cache hit
                    if (statsAfterSecond.HitCount > initialStats.HitCount) {
                        _loggingService.LogInfo("Cache hit test passed!");
                    } else {
                        _loggingService.LogWarning("Cache hit test failed - no hit recorded");
                    }
                }

                _loggingService.LogInfo("Cache functionality tests completed");
            } catch (Exception ex) {
                _loggingService.LogError("Cache functionality test failed", ex);
            }
        }

        /// <summary>
        /// Test the enhanced date validation functionality
        /// </summary>
        private void TestDateValidation() {
            _loggingService.LogInfo("Testing enhanced date validation...");

            // Test 1: Valid date range
            var startDate = DateTime.Today.AddDays(-30);
            var endDate = DateTime.Today;
            var result = _validationService.ValidateDateRangeDetailed(startDate, endDate);
            _loggingService.LogInfo($"Valid date range test: {result}");

            // Test 2: Invalid date range (start > end)
            result = _validationService.ValidateDateRangeDetailed(endDate, startDate);
            _loggingService.LogInfo($"Invalid date range test: {result}");

            // Test 3: Date too far in the past
            var oldDate = new DateTime(1990, 1, 1);
            result = _validationService.ValidateDateRangeDetailed(oldDate, DateTime.Today);
            _loggingService.LogInfo($"Old date range test: {result}");

            // Test 4: Future date
            var futureDate = DateTime.Today.AddDays(30);
            result = _validationService.ValidateDateRangeDetailed(DateTime.Today, futureDate);
            _loggingService.LogInfo($"Future date range test: {result}");

            // Test 5: Get suggested date range
            var suggested = _validationService.GetSuggestedDateRange();
            _loggingService.LogInfo($"Suggested date range: {suggested.startDate:yyyy-MM-dd} to {suggested.endDate:yyyy-MM-dd}");

            // Test 6: Adjust invalid date range
            var adjusted = _validationService.AdjustToValidDateRange(oldDate, futureDate);
            _loggingService.LogInfo($"Adjusted date range: {adjusted.startDate:yyyy-MM-dd} to {adjusted.endDate:yyyy-MM-dd}");

            _loggingService.LogInfo("Date validation tests completed");
        }

        /// <summary>
        /// Test the auto-refresh service functionality
        /// </summary>
        private void TestAutoRefreshService() {
            _loggingService.LogInfo("Testing auto-refresh service...");

            // Test statistics when stopped
            var stats = _autoRefreshService.GetStatistics();
            _loggingService.LogInfo($"Initial auto-refresh stats: {stats}");

            // Test setting custom interval
            _autoRefreshService.SetRefreshInterval(TimeSpan.FromMinutes(15));
            _loggingService.LogInfo("Set custom refresh interval to 15 minutes");

            // Test event subscription
            _autoRefreshService.CurrencyRefreshCompleted += OnCurrencyRefreshCompleted;
            _autoRefreshService.RefreshStatusChanged += OnRefreshStatusChanged;

            _loggingService.LogInfo("Auto-refresh service tests completed");
        }

        private void OnCurrencyRefreshCompleted(object sender, CurrencyRefreshEventArgs e) {
            var refreshType = e.IsManualRefresh ? "Manual" : "Auto";
            _loggingService.LogInfo($"{refreshType} refresh completed: {e.Message}");
        }

        private void OnRefreshStatusChanged(object sender, string status) {
            _loggingService.LogInfo($"Auto-refresh status: {status}");
        }

        #endregion

        #region IDisposable Support

        public void Dispose() {
            try {
                _loggingService?.LogInfo("Disposing ServiceContainer");

                // Stop auto-refresh and dispose
                _autoRefreshService?.Stop();
                _autoRefreshService?.Dispose();

                // Part 2: Dispose cache services
                (_cacheService as IDisposable)?.Dispose();
                (_historicalDataService as IDisposable)?.Dispose();

                // Dispose other services
                (_currencyService as IDisposable)?.Dispose();
                (_initializationService as IDisposable)?.Dispose();

                _loggingService?.LogInfo("ServiceContainer disposal completed");
            } catch (Exception ex) {
                _loggingService?.LogError("Error during ServiceContainer disposal", ex);
            }
        }

        #endregion
    }
}
