using CurrencyConvertor.Services;
using CurrencyConvertor.Services.Interfaces;
using CurrencyConvertor.ViewModels.Interfaces;
using CurrencyConvertor.Infrastructure;
using CurrencyConvertor.Commands;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace CurrencyConvertor.ViewModels {
    /// <summary>
    /// ViewModel responsible only for currency conversion operations (SRP - Single Responsibility)
    /// Enhanced with auto-refresh capabilities
    /// </summary>
    public class CurrencyConversionViewModel : ViewModelBase, ICurrencyConversionViewModel {
        private readonly ICurrencyConversionService _conversionService;
        private readonly ICurrencyMetadataService _metadataService;
        private readonly IValidationService _validationService;
        private readonly ILoggingService _loggingService;
        private readonly CurrencyAutoRefreshService _autoRefreshService;

        private ObservableCollection<string> _currencies;
        private string _selectedFromCurrency;
        private string _selectedToCurrency;
        private string _amount;
        private string _conversionResult;
        private string _conversionDate;
        private bool _isConverting;
        private bool _autoRefreshEnabled;
        private string _autoRefreshStatus;

        public CurrencyConversionViewModel(
            ICurrencyConversionService conversionService,
            ICurrencyMetadataService metadataService,
            IValidationService validationService,
            ILoggingService loggingService) {
            _conversionService = conversionService ?? throw new ArgumentNullException(nameof(conversionService));
            _metadataService = metadataService ?? throw new ArgumentNullException(nameof(metadataService));
            _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));

            // Get auto-refresh service from container
            _autoRefreshService = Infrastructure.ServiceContainer.Instance.GetAutoRefreshService();

            Currencies = new ObservableCollection<string>();
            Amount = "1";
            ConversionResult = "Enter amount and select currencies";
            AutoRefreshStatus = "Auto-refresh disabled";

            // Initialize commands
            RefreshCurrenciesCommand = new RelayCommand(async () => await RefreshCurrenciesAsync());
            ToggleAutoRefreshCommand = new RelayCommand(() => ToggleAutoRefresh());

            // Subscribe to auto-refresh events
            _autoRefreshService.CurrencyRefreshCompleted += OnAutoRefreshCompleted;
            _autoRefreshService.RefreshStatusChanged += OnAutoRefreshStatusChanged;

            _loggingService.LogInfo("CurrencyConversionViewModel initialized with auto-refresh support");
        }

        #region Properties

        public ObservableCollection<string> Currencies {
            get => _currencies;
            private set => SetProperty(ref _currencies, value);
        }

        public string SelectedFromCurrency {
            get => _selectedFromCurrency;
            set {
                if (SetProperty(ref _selectedFromCurrency, value)) {
                    _loggingService.LogInfo($"From currency changed to: {value}");
                    PerformConversionAsync();
                }
            }
        }

        public string SelectedToCurrency {
            get => _selectedToCurrency;
            set {
                if (SetProperty(ref _selectedToCurrency, value)) {
                    _loggingService.LogInfo($"To currency changed to: {value}");
                    PerformConversionAsync();
                }
            }
        }

        public string Amount {
            get => _amount;
            set {
                if (SetProperty(ref _amount, value)) {
                    _loggingService.LogInfo($"Amount changed to: {value}");
                    PerformConversionAsync();
                }
            }
        }

        public string ConversionResult {
            get => _conversionResult;
            private set {
                if (SetProperty(ref _conversionResult, value)) {
                    _loggingService.LogInfo($"ConversionResult changed to: {value}");
                }
            }
        }

        public string ConversionDate {
            get => _conversionDate;
            private set {
                if (SetProperty(ref _conversionDate, value)) {
                    _loggingService.LogInfo($"ConversionDate changed to: {value}");
                }
            }
        }

        public bool AutoRefreshEnabled {
            get => _autoRefreshEnabled;
            private set => SetProperty(ref _autoRefreshEnabled, value);
        }

        public string AutoRefreshStatus {
            get => _autoRefreshStatus;
            private set => SetProperty(ref _autoRefreshStatus, value);
        }

        #endregion

        #region Commands

        public ICommand RefreshCurrenciesCommand { get; }
        public ICommand ToggleAutoRefreshCommand { get; }

        #endregion

        #region Public Methods

        public async Task LoadCurrenciesAsync() {
            try {
                _loggingService.LogInfo("Loading currencies for conversion");
                var currencies = await _metadataService.GetAvailableCurrenciesAsync();

                Currencies.Clear();
                foreach (var currency in currencies) {
                    Currencies.Add(currency);
                }

                _loggingService.LogInfo($"Loaded {currencies.Count} currencies");

                // Set default currencies if not already set
                if (string.IsNullOrEmpty(SelectedFromCurrency) && currencies.Count > 0) {
                    SelectedFromCurrency = currencies.Contains("USD") ? "USD" : currencies[0];
                    _loggingService.LogInfo($"Set default from currency: {SelectedFromCurrency}");
                }

                if (string.IsNullOrEmpty(SelectedToCurrency) && currencies.Count > 1) {
                    SelectedToCurrency = currencies.Contains("INR") ? "INR" :
                                       (currencies[0] != SelectedFromCurrency ? currencies[0] : currencies[1]);
                    _loggingService.LogInfo($"Set default to currency: {SelectedToCurrency}");
                }
            } catch (Exception ex) {
                _loggingService.LogError("Failed to load currencies", ex);
                ConversionResult = "Failed to load currencies";
                ConversionDate = "";
                throw;
            }
        }

        /// <summary>
        /// Starts auto-refresh with default interval
        /// </summary>
        public void StartAutoRefresh() {
            StartAutoRefresh(30); // Default 30 minutes
        }

        /// <summary>
        /// Starts auto-refresh with custom interval
        /// </summary>
        /// <param name="intervalMinutes">Refresh interval in minutes</param>
        public void StartAutoRefresh(int intervalMinutes) {
            _autoRefreshService.Start(TimeSpan.FromMinutes(intervalMinutes));
            AutoRefreshEnabled = true;
            _loggingService.LogInfo($"Auto-refresh started with {intervalMinutes} minute interval");
        }

        /// <summary>
        /// Stops auto-refresh
        /// </summary>
        public void StopAutoRefresh() {
            _autoRefreshService.Stop();
            AutoRefreshEnabled = false;
            _loggingService.LogInfo("Auto-refresh stopped");
        }

        /// <summary>
        /// Gets auto-refresh statistics
        /// </summary>
        /// <returns>Refresh statistics</returns>
        public RefreshStatistics GetAutoRefreshStats() {
            return _autoRefreshService.GetStatistics();
        }

        #endregion

        #region Private Methods

        private async Task RefreshCurrenciesAsync() {
            try {
                ConversionResult = "Refreshing currencies...";
                ConversionDate = "";

                // Use auto-refresh service for manual refresh
                var success = await _autoRefreshService.RefreshNowAsync();

                if (success) {
                    ConversionResult = "Currencies refreshed";
                    _loggingService.LogInfo("Manual currency refresh completed successfully");
                } else {
                    ConversionResult = "Failed to refresh currencies";
                    _loggingService.LogWarning("Manual currency refresh failed");
                }
            } catch (Exception ex) {
                _loggingService.LogError("Failed to refresh currencies", ex);
                ConversionResult = "Failed to refresh currencies";
                ConversionDate = $"Error: {ex.Message}";
            }
        }

        private void ToggleAutoRefresh() {
            if (AutoRefreshEnabled) {
                StopAutoRefresh();
            } else {
                StartAutoRefresh();
            }
        }

        private async void PerformConversionAsync() {
            // Prevent multiple simultaneous conversions
            if (_isConverting) {
                _loggingService.LogInfo("Conversion already in progress, skipping");
                return;
            }

            try {
                _isConverting = true;

                _loggingService.LogInfo($"Starting conversion: {Amount} {SelectedFromCurrency} to {SelectedToCurrency}");

                // Validate amount
                if (!_validationService.IsValidAmount(Amount, out decimal amount)) {
                    ConversionResult = "Invalid amount";
                    ConversionDate = "";
                    _loggingService.LogWarning($"Invalid amount: {Amount}");
                    return;
                }

                // Validate currencies
                if (!_validationService.IsValidCurrency(SelectedFromCurrency)) {
                    _loggingService.LogWarning($"Invalid from currency: {SelectedFromCurrency}");
                    ConversionResult = "Select source currency";
                    ConversionDate = "";
                    return;
                }

                if (!_validationService.IsValidCurrency(SelectedToCurrency)) {
                    _loggingService.LogWarning($"Invalid to currency: {SelectedToCurrency}");
                    ConversionResult = "Select target currency";
                    ConversionDate = "";
                    return;
                }

                // Don't convert if currencies are the same
                if (SelectedFromCurrency == SelectedToCurrency) {
                    ConversionResult = $"{amount:F4} {SelectedToCurrency}";
                    ConversionDate = "(same currency)";
                    _loggingService.LogInfo("Same currency selected, no conversion needed");
                    return;
                }

                // Show loading state
                ConversionResult = "Converting...";
                ConversionDate = "";

                // Perform conversion
                _loggingService.LogInfo("Calling conversion service...");
                var result = await _conversionService.ConvertCurrencyAsync(SelectedFromCurrency, SelectedToCurrency, amount);

                _loggingService.LogInfo($"Conversion service returned: {result?.FormattedResult ?? "null"}");

                ConversionResult = result.FormattedResult;
                ConversionDate = result.FormattedDate;

                _loggingService.LogInfo($"Conversion successful: {amount} {SelectedFromCurrency} = {result.ConvertedAmount} {SelectedToCurrency}");
            } catch (Exception ex) {
                _loggingService.LogError("Conversion failed", ex);
                ConversionResult = "Conversion failed";
                ConversionDate = $"Error: {ex.Message}";
            } finally {
                _isConverting = false;
            }
        }

        #endregion

        #region Event Handlers

        private void OnAutoRefreshCompleted(object sender, CurrencyRefreshEventArgs e) {
            // Update currencies if refresh was successful
            if (e.Success && e.Currencies?.Count > 0) {
                _loggingService.LogInfo($"Auto-refresh completed: {e.Currencies.Count} currencies updated");

                // Update the currency list on UI thread
                System.Windows.Application.Current?.Dispatcher.Invoke(() => {
                    var previousFrom = SelectedFromCurrency;
                    var previousTo = SelectedToCurrency;

                    Currencies.Clear();
                    foreach (var currency in e.Currencies) {
                        Currencies.Add(currency);
                    }

                    // Restore selections if they still exist
                    if (Currencies.Contains(previousFrom))
                        SelectedFromCurrency = previousFrom;
                    if (Currencies.Contains(previousTo))
                        SelectedToCurrency = previousTo;
                });
            } else {
                _loggingService.LogWarning($"Auto-refresh failed: {e.Message}");
            }
        }

        private void OnAutoRefreshStatusChanged(object sender, string status) {
            AutoRefreshStatus = status;
            AutoRefreshEnabled = _autoRefreshService.IsEnabled;
        }

        #endregion

        #region IDisposable

        protected override void Dispose(bool disposing) {
            if (disposing) {
                try {
                    // Unsubscribe from events
                    if (_autoRefreshService != null) {
                        _autoRefreshService.CurrencyRefreshCompleted -= OnAutoRefreshCompleted;
                        _autoRefreshService.RefreshStatusChanged -= OnAutoRefreshStatusChanged;
                    }

                    _loggingService?.LogInfo("CurrencyConversionViewModel disposed");
                } catch (Exception ex) {
                    _loggingService?.LogError("Error during CurrencyConversionViewModel disposal", ex);
                }
            }

            base.Dispose(disposing);
        }

        #endregion
    }
}
