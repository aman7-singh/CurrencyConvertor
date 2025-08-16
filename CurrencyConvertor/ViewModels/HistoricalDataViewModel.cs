using CurrencyConvertor.Commands;
using CurrencyConvertor.Models;
using CurrencyConvertor.Services.Interfaces;
using CurrencyConvertor.ViewModels.Interfaces;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Configuration;

namespace CurrencyConvertor.ViewModels {
    /// <summary>
    /// ViewModel responsible only for historical data operations (SRP - Single Responsibility)
    /// </summary>
    public class HistoricalDataViewModel : ViewModelBase, IHistoricalDataViewModel {
        private readonly IHistoricalDataService _historicalDataService;
        private readonly IValidationService _validationService;
        private readonly INotificationService _notificationService;
        private readonly ILoggingService _loggingService;

        private DateTime? _startDate;
        private DateTime? _endDate;
        private ObservableCollection<HistoricalRate> _historicalRates;
        private string _fromCurrency;
        private string _toCurrency;

        public HistoricalDataViewModel(
            IHistoricalDataService historicalDataService,
            IValidationService validationService,
            INotificationService notificationService,
            ILoggingService loggingService) {
            _historicalDataService = historicalDataService ?? throw new ArgumentNullException(nameof(historicalDataService));
            _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));

            HistoricalRates = new ObservableCollection<HistoricalRate>();

            // Set default date range from configuration
            var defaultDays = LoadDefaultDateRangeFromConfig();
            StartDate = DateTime.Today.AddDays(-defaultDays);
            EndDate = DateTime.Today;

            _loggingService?.LogInfo($"HistoricalDataViewModel initialized with {defaultDays}-day default range");

            LoadHistoricalDataCommand = new RelayCommand(async () => await LoadHistoricalDataAsync(), CanLoadHistoricalData);
        }

        /// <summary>
        /// Loads default date range from App.config
        /// </summary>
        private int LoadDefaultDateRangeFromConfig() {
            try {
                // Read from App.config using helper method
                var configValue = GetAppSetting("DefaultHistoricalDataDays", null);
                if (!string.IsNullOrEmpty(configValue) && int.TryParse(configValue, out var defaultDays) && defaultDays > 0) {
                    _loggingService?.LogInfo($"Loaded DefaultHistoricalDataDays from App.config: {defaultDays} days");
                    return defaultDays;
                }
                
                _loggingService?.LogWarning($"Invalid or missing DefaultHistoricalDataDays in App.config: '{configValue}', using default");
            } catch (Exception ex) {
                _loggingService?.LogWarning($"Failed to load DefaultHistoricalDataDays from App.config: {ex.Message}");
            }
            
            // Use fallback default if config is not available or invalid
            const int fallbackDefault = 30;
            _loggingService?.LogInfo($"Using fallback DefaultHistoricalDataDays: {fallbackDefault} days");
            return fallbackDefault;
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

        #region Properties

        public DateTime? StartDate {
            get => _startDate;
            set {
                if (SetProperty(ref _startDate, value)) {
                    LoadHistoricalDataAsync();
                }
            }
        }

        public DateTime? EndDate {
            get => _endDate;
            set {
                if (SetProperty(ref _endDate, value)) {
                    LoadHistoricalDataAsync();
                }
            }
        }

        public ObservableCollection<HistoricalRate> HistoricalRates {
            get => _historicalRates;
            private set => SetProperty(ref _historicalRates, value);
        }

        #endregion

        #region Commands

        public ICommand LoadHistoricalDataCommand { get; }

        #endregion

        #region Public Methods

        public void SetCurrencyPair(string fromCurrency, string toCurrency) {
            if (_fromCurrency != fromCurrency || _toCurrency != toCurrency) {
                _fromCurrency = fromCurrency;
                _toCurrency = toCurrency;
                LoadHistoricalDataAsync();
            }
        }

        #endregion

        #region Private Methods

        private async Task LoadHistoricalDataAsync() {
            try {
                if (!_validationService.IsValidCurrency(_fromCurrency) ||
                    !_validationService.IsValidCurrency(_toCurrency)) {
                    return;
                }

                if (!_validationService.IsValidDateRange(StartDate, EndDate)) {
                    if (StartDate.HasValue && EndDate.HasValue && StartDate.Value > EndDate.Value) {
                        _notificationService.ShowWarning("Start date must be before end date", "Invalid Date Range");
                    }
                    return;
                }

                _loggingService.LogInfo($"Loading historical data for {_fromCurrency} to {_toCurrency}");

                var rates = await _historicalDataService.GetHistoricalRatesAsync(
                    _fromCurrency, _toCurrency, StartDate.Value, EndDate.Value);

                HistoricalRates.Clear();
                foreach (var rate in rates.OrderByDescending(r => r.Date)) {
                    HistoricalRates.Add(rate);
                }

                _loggingService.LogInfo($"Loaded {rates.Count} historical rates");
            } catch (Exception ex) {
                _loggingService.LogError("Failed to load historical data", ex);
                _notificationService.ShowError($"Error loading historical data: {ex.Message}");
            }
        }

        private bool CanLoadHistoricalData() {
            return _validationService.IsValidCurrency(_fromCurrency) &&
                   _validationService.IsValidCurrency(_toCurrency) &&
                   _validationService.IsValidDateRange(StartDate, EndDate);
        }

        #endregion
    }
}
