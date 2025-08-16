using CurrencyConvertor.Services;
using CurrencyConvertor.Services.Interfaces;
using CurrencyConvertor.Services.Models;
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

        // Refactored: MainWindow specialized services
        public MainWindowInitializer GetMainWindowInitializer() => new MainWindowInitializer(this);
        public MainWindowCleanupService GetMainWindowCleanupService() => new MainWindowCleanupService(this);
        public MainWindowErrorHandler GetMainWindowErrorHandler() => new MainWindowErrorHandler(_loggingService, _notificationService);

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
