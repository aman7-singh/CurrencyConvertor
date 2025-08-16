using CurrencyConvertor.Models;
using CurrencyConvertor.Services;
using CurrencyConvertor.Services.Interfaces;
using CurrencyConvertor.ViewModels.Interfaces;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace CurrencyConvertor.ViewModels {
    /// <summary>
    /// Main ViewModel that composes specialized ViewModels (SRP - Single Responsibility)
    /// Follows Composition over Inheritance and Dependency Injection (DIP)
    /// Enhanced with auto-refresh capabilities
    /// </summary>
    public class MainViewModel : ViewModelBase, IMainViewModel {
        private readonly ICurrencyConversionViewModel _conversionViewModel;
        private readonly IHistoricalDataViewModel _historicalDataViewModel;
        private readonly IApplicationStatusViewModel _statusViewModel;
        private readonly IInitializationService _initializationService;
        private readonly INotificationService _notificationService;
        private readonly ILoggingService _loggingService;

        public MainViewModel(
            ICurrencyConversionViewModel conversionViewModel,
            IHistoricalDataViewModel historicalDataViewModel,
            IApplicationStatusViewModel statusViewModel,
            IInitializationService initializationService,
            INotificationService notificationService,
            ILoggingService loggingService) {
            _conversionViewModel = conversionViewModel ?? throw new ArgumentNullException(nameof(conversionViewModel));
            _historicalDataViewModel = historicalDataViewModel ?? throw new ArgumentNullException(nameof(historicalDataViewModel));
            _statusViewModel = statusViewModel ?? throw new ArgumentNullException(nameof(statusViewModel));
            _initializationService = initializationService ?? throw new ArgumentNullException(nameof(initializationService));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));

            _loggingService.LogInfo("MainViewModel constructor completed");

            // Subscribe to events
            _initializationService.StatusChanged += OnInitializationStatusChanged;
            _initializationService.LoadingStateChanged += OnInitializationLoadingStateChanged;

            // Subscribe to all ViewModels' property changes for proper UI binding
            if (_conversionViewModel is ViewModelBase conversionVm) {
                conversionVm.PropertyChanged += OnConversionViewModelPropertyChanged;
            }

            if (_historicalDataViewModel is ViewModelBase historicalVm) {
                historicalVm.PropertyChanged += OnHistoricalViewModelPropertyChanged;
            }

            if (_statusViewModel is ViewModelBase statusVm) {
                statusVm.PropertyChanged += OnStatusViewModelPropertyChanged;
            }

            _loggingService.LogInfo("MainViewModel event subscriptions completed");
        }

        #region ICurrencyConversionViewModel Implementation (Delegation)

        public ObservableCollection<string> Currencies => _conversionViewModel.Currencies;

        public string SelectedFromCurrency {
            get => _conversionViewModel.SelectedFromCurrency;
            set {
                _loggingService.LogInfo($"MainViewModel.SelectedFromCurrency set to: {value}");
                _conversionViewModel.SelectedFromCurrency = value;
            }
        }

        public string SelectedToCurrency {
            get => _conversionViewModel.SelectedToCurrency;
            set {
                _loggingService.LogInfo($"MainViewModel.SelectedToCurrency set to: {value}");
                _conversionViewModel.SelectedToCurrency = value;
            }
        }

        public string Amount {
            get => _conversionViewModel.Amount;
            set {
                _loggingService.LogInfo($"MainViewModel.Amount set to: {value}");
                _conversionViewModel.Amount = value;
            }
        }

        public string ConversionResult => _conversionViewModel.ConversionResult;
        public string ConversionDate => _conversionViewModel.ConversionDate;
        public ICommand RefreshCurrenciesCommand => _conversionViewModel.RefreshCurrenciesCommand;

        // Auto-refresh properties and methods
        public bool AutoRefreshEnabled => _conversionViewModel.AutoRefreshEnabled;
        public string AutoRefreshStatus => _conversionViewModel.AutoRefreshStatus;
        public ICommand ToggleAutoRefreshCommand => _conversionViewModel.ToggleAutoRefreshCommand;

        public void StartAutoRefresh() {
            _conversionViewModel.StartAutoRefresh();
        }

        public void StartAutoRefresh(int intervalMinutes) {
            _conversionViewModel.StartAutoRefresh(intervalMinutes);
        }

        public void StopAutoRefresh() {
            _conversionViewModel.StopAutoRefresh();
        }

        public RefreshStatistics GetAutoRefreshStats() {
            return _conversionViewModel.GetAutoRefreshStats();
        }

        #endregion

        #region IHistoricalDataViewModel Implementation (Delegation)

        public DateTime? StartDate {
            get => _historicalDataViewModel.StartDate;
            set => _historicalDataViewModel.StartDate = value;
        }

        public DateTime? EndDate {
            get => _historicalDataViewModel.EndDate;
            set => _historicalDataViewModel.EndDate = value;
        }

        public ObservableCollection<HistoricalRate> HistoricalRates => _historicalDataViewModel.HistoricalRates;
        public ICommand LoadHistoricalDataCommand => _historicalDataViewModel.LoadHistoricalDataCommand;

        #endregion

        #region IApplicationStatusViewModel Implementation (Delegation)

        public string StatusMessage => _statusViewModel.StatusMessage;
        public bool IsLoading => _statusViewModel.IsLoading;

        #endregion

        #region IMainViewModel Implementation

        public async void Initialize() {
            try {
                _loggingService.LogInfo("Initializing main application");

                var success = await _initializationService.InitializeApplicationAsync();

                if (success) {
                    _loggingService.LogInfo("Initialization service completed successfully");

                    // Load currencies for conversion
                    if (_conversionViewModel is CurrencyConversionViewModel conversionVm) {
                        _loggingService.LogInfo("Loading currencies through conversion view model");
                        await conversionVm.LoadCurrenciesAsync();
                        _loggingService.LogInfo("Currencies loaded successfully");
                    } else {
                        _loggingService.LogWarning("Conversion view model is not of expected type");
                    }
                } else {
                    _loggingService.LogWarning("Initialization service failed, showing fallback message");

                    // Show fallback message
                    _notificationService.ShowWarning(
                        "Failed to initialize application with live data. Using fallback currency list. Check your internet connection and try again.",
                        "Initialization Warning");
                }
            } catch (Exception ex) {
                _loggingService.LogError("Critical error during initialization", ex);
                _notificationService.ShowError($"Critical error during initialization: {ex.Message}");
            }
        }

        #endregion

        #region Event Handlers

        private void OnInitializationStatusChanged(object sender, string status) {
            _loggingService.LogInfo($"Initialization status changed: {status}");
            if (_statusViewModel is ApplicationStatusViewModel statusVm) {
                statusVm.SetStatus(status);
            }
        }

        private void OnInitializationLoadingStateChanged(object sender, bool isLoading) {
            _loggingService.LogInfo($"Initialization loading state changed: {isLoading}");
            if (_statusViewModel is ApplicationStatusViewModel statusVm) {
                statusVm.SetLoadingState(isLoading);
            }
        }

        private void OnConversionViewModelPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            _loggingService.LogInfo($"Conversion ViewModel property changed: {e.PropertyName}");

            // Forward ALL property changes from the conversion view model
            OnPropertyChanged(e.PropertyName);

            // Specifically handle currency changes for historical data
            if (e.PropertyName == nameof(SelectedFromCurrency) || e.PropertyName == nameof(SelectedToCurrency)) {
                if (_historicalDataViewModel is HistoricalDataViewModel historicalVm) {
                    _loggingService.LogInfo($"Updating historical data for currency pair: {SelectedFromCurrency} -> {SelectedToCurrency}");
                    historicalVm.SetCurrencyPair(SelectedFromCurrency, SelectedToCurrency);
                }
            }
        }

        private void OnHistoricalViewModelPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            _loggingService.LogInfo($"Historical ViewModel property changed: {e.PropertyName}");
            OnPropertyChanged(e.PropertyName);
        }

        private void OnStatusViewModelPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            _loggingService.LogInfo($"Status ViewModel property changed: {e.PropertyName}");
            OnPropertyChanged(e.PropertyName);
        }

        #endregion

        #region IDisposable Implementation

        protected override void Dispose(bool disposing) {
            if (disposing) {
                try {
                    _loggingService.LogInfo("Disposing main view model");

                    // Unsubscribe from events
                    _initializationService.StatusChanged -= OnInitializationStatusChanged;
                    _initializationService.LoadingStateChanged -= OnInitializationLoadingStateChanged;

                    if (_conversionViewModel is ViewModelBase conversionVm) {
                        conversionVm.PropertyChanged -= OnConversionViewModelPropertyChanged;
                    }

                    if (_historicalDataViewModel is ViewModelBase historicalVm) {
                        historicalVm.PropertyChanged -= OnHistoricalViewModelPropertyChanged;
                    }

                    if (_statusViewModel is ViewModelBase statusVm) {
                        statusVm.PropertyChanged -= OnStatusViewModelPropertyChanged;
                    }

                    // Dispose components if they implement IDisposable
                    (_conversionViewModel as IDisposable)?.Dispose();
                    (_historicalDataViewModel as IDisposable)?.Dispose();
                    (_statusViewModel as IDisposable)?.Dispose();
                    (_initializationService as IDisposable)?.Dispose();

                    _loggingService.LogInfo("MainViewModel disposal completed");
                } catch (Exception ex) {
                    _loggingService?.LogError("Error during disposal", ex);
                }
            }

            base.Dispose(disposing);
        }

        #endregion
    }
}
