using CurrencyConvertor.Services;
using CurrencyConvertor.Services.Interfaces;
using CurrencyConvertor.ViewModels;
using CurrencyConvertor.ViewModels.Interfaces;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CurrencyConvertor.Infrastructure {
    /// <summary>
    /// Simple dependency injection container following DIP (Dependency Inversion Principle)
    /// Enhanced with auto-refresh currency service
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

        private void RegisterServices() {
            // Core services
            _loggingService = new ConsoleLoggingService();
            _notificationService = new WpfNotificationService();
            _validationService = new ValidationService(_loggingService);

            // Currency services - using the same instance that implements multiple interfaces
            var currencyService = new CurrencyService(_loggingService);
            _currencyService = currencyService;
            _conversionService = currencyService;
            _historicalDataService = currencyService;

            // Resilient metadata service with fallback
            var fallbackMetadataService = new FallbackCurrencyMetadataService(_loggingService);
            _metadataService = new ResilientCurrencyMetadataService(
                currencyService, fallbackMetadataService, _loggingService);

            // Auto-refresh service
            _autoRefreshService = new CurrencyAutoRefreshService(_metadataService, _loggingService);

            // Initialization service
            _initializationService = new ApplicationInitializationService(_currencyService, _loggingService);

            _loggingService.LogInfo("Services registered successfully with auto-refresh capability");
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
            _loggingService.LogInfo("Creating HistoricalDataViewModel");
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
            _loggingService.LogInfo("Creating MainViewModel with auto-refresh integration");
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

        #region Diagnostic Methods

        /// <summary>
        /// Test basic service functionality for debugging
        /// </summary>
        public async System.Threading.Tasks.Task<bool> TestServicesAsync() {
            try {
                _loggingService.LogInfo("Starting service diagnostics...");

                // Test validation service
                var isValidAmount = _validationService.IsValidAmount("1.0", out decimal amount);
                _loggingService.LogInfo($"Amount validation test: {isValidAmount}, Amount: {amount}");

                var isValidCurrency = _validationService.IsValidCurrency("EUR");
                _loggingService.LogInfo($"Currency validation test: {isValidCurrency}");

                // Test enhanced date validation
                TestDateValidation();

                // Test auto-refresh service
                TestAutoRefreshService();

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
