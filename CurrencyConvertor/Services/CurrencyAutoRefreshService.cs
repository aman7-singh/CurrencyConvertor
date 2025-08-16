using CurrencyConvertor.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Timers;

namespace CurrencyConvertor.Services {
    /// <summary>
    /// Auto-refresh service for currency data (SRP - Single Responsibility Principle)
    /// Provides automatic currency refresh functionality with configurable intervals
    /// </summary>
    public class CurrencyAutoRefreshService : IDisposable {
        private readonly ICurrencyMetadataService _currencyService;
        private readonly ILoggingService _loggingService;
        private readonly Timer _refreshTimer;
        private readonly object _lockObject = new object();
        private bool _isRefreshing = false;
        private bool _isEnabled = false;
        private DateTime _lastRefreshTime = DateTime.MinValue;

        // Configuration
        private readonly TimeSpan _defaultRefreshInterval = TimeSpan.FromMinutes(30); // Default 30 minutes
        private readonly TimeSpan _minRefreshInterval = TimeSpan.FromMinutes(1); // Minimum 1 minute
        private readonly TimeSpan _maxRefreshInterval = TimeSpan.FromHours(24); // Maximum 24 hours

        public event EventHandler<CurrencyRefreshEventArgs> CurrencyRefreshCompleted;
        public event EventHandler<string> RefreshStatusChanged;

        public CurrencyAutoRefreshService(
            ICurrencyMetadataService currencyService,
            ILoggingService loggingService = null) {
            _currencyService = currencyService ?? throw new ArgumentNullException(nameof(currencyService));
            _loggingService = loggingService;

            _refreshTimer = new Timer();
            _refreshTimer.Elapsed += OnTimerElapsed;
            _refreshTimer.AutoReset = true;

            SetRefreshInterval(_defaultRefreshInterval);

            _loggingService?.LogInfo($"CurrencyAutoRefreshService initialized with {_defaultRefreshInterval.TotalMinutes} minute interval");
        }

        #region Properties

        public bool IsEnabled {
            get => _isEnabled;
            private set {
                if (_isEnabled != value) {
                    _isEnabled = value;
                    _loggingService?.LogInfo($"Auto-refresh {(value ? "enabled" : "disabled")}");
                }
            }
        }

        public bool IsRefreshing {
            get => _isRefreshing;
            private set {
                if (_isRefreshing != value) {
                    _isRefreshing = value;
                    OnRefreshStatusChanged(_isRefreshing ? "Refreshing currencies..." : "Ready");
                }
            }
        }

        public TimeSpan RefreshInterval {
            get => TimeSpan.FromMilliseconds(_refreshTimer.Interval);
            private set => _refreshTimer.Interval = value.TotalMilliseconds;
        }

        public DateTime LastRefreshTime => _lastRefreshTime;

        public TimeSpan TimeSinceLastRefresh => _lastRefreshTime == DateTime.MinValue
            ? TimeSpan.Zero
            : DateTime.Now - _lastRefreshTime;

        #endregion

        #region Public Methods

        /// <summary>
        /// Starts auto-refresh with default interval
        /// </summary>
        public void Start() {
            Start(_defaultRefreshInterval);
        }

        /// <summary>
        /// Starts auto-refresh with specified interval
        /// </summary>
        /// <param name="interval">Refresh interval</param>
        public void Start(TimeSpan interval) {
            lock (_lockObject) {
                SetRefreshInterval(interval);
                IsEnabled = true;
                _refreshTimer.Start();

                _loggingService?.LogInfo($"Auto-refresh started with {interval.TotalMinutes:F1} minute interval");
                OnRefreshStatusChanged($"Auto-refresh enabled (every {interval.TotalMinutes:F0} min)");
            }
        }

        /// <summary>
        /// Stops auto-refresh
        /// </summary>
        public void Stop() {
            lock (_lockObject) {
                _refreshTimer.Stop();
                IsEnabled = false;

                _loggingService?.LogInfo("Auto-refresh stopped");
                OnRefreshStatusChanged("Auto-refresh disabled");
            }
        }

        /// <summary>
        /// Manually triggers a refresh (doesn't affect timer)
        /// </summary>
        /// <returns>True if refresh was successful</returns>
        public async Task<bool> RefreshNowAsync() {
            return await PerformRefreshAsync(true);
        }

        /// <summary>
        /// Sets the refresh interval (applies if auto-refresh is running)
        /// </summary>
        /// <param name="interval">New refresh interval</param>
        public void SetRefreshInterval(TimeSpan interval) {
            // Validate interval
            if (interval < _minRefreshInterval) {
                interval = _minRefreshInterval;
                _loggingService?.LogWarning($"Refresh interval too short, adjusted to {_minRefreshInterval.TotalMinutes} minutes");
            } else if (interval > _maxRefreshInterval) {
                interval = _maxRefreshInterval;
                _loggingService?.LogWarning($"Refresh interval too long, adjusted to {_maxRefreshInterval.TotalHours} hours");
            }

            lock (_lockObject) {
                RefreshInterval = interval;
                _loggingService?.LogInfo($"Refresh interval set to {interval.TotalMinutes:F1} minutes");

                if (IsEnabled) {
                    OnRefreshStatusChanged($"Auto-refresh enabled (every {interval.TotalMinutes:F0} min)");
                }
            }
        }

        /// <summary>
        /// Gets refresh statistics
        /// </summary>
        /// <returns>Refresh statistics</returns>
        public RefreshStatistics GetStatistics() {
            return new RefreshStatistics {
                IsEnabled = IsEnabled,
                IsRefreshing = IsRefreshing,
                RefreshInterval = RefreshInterval,
                LastRefreshTime = LastRefreshTime,
                TimeSinceLastRefresh = TimeSinceLastRefresh,
                NextRefreshTime = IsEnabled && LastRefreshTime != DateTime.MinValue
                    ? LastRefreshTime.Add(RefreshInterval)
                    : (DateTime?)null
            };
        }

        #endregion

        #region Private Methods

        private async void OnTimerElapsed(object sender, ElapsedEventArgs e) {
            await PerformRefreshAsync(false);
        }

        private async Task<bool> PerformRefreshAsync(bool isManual) {
            // Prevent multiple simultaneous refreshes
            if (IsRefreshing) {
                _loggingService?.LogInfo("Refresh already in progress, skipping");
                return false;
            }

            try {
                IsRefreshing = true;
                var refreshType = isManual ? "Manual" : "Automatic";
                _loggingService?.LogInfo($"{refreshType} currency refresh started");

                var currencies = await _currencyService.GetAvailableCurrenciesAsync();
                _lastRefreshTime = DateTime.Now;

                var eventArgs = new CurrencyRefreshEventArgs {
                    Success = true,
                    Currencies = currencies,
                    RefreshTime = _lastRefreshTime,
                    IsManualRefresh = isManual,
                    Message = $"Successfully refreshed {currencies.Count} currencies"
                };

                OnCurrencyRefreshCompleted(eventArgs);
                _loggingService?.LogInfo($"{refreshType} refresh completed successfully: {currencies.Count} currencies");

                return true;
            } catch (Exception ex) {
                var eventArgs = new CurrencyRefreshEventArgs {
                    Success = false,
                    Currencies = new List<string>(),
                    RefreshTime = DateTime.Now,
                    IsManualRefresh = isManual,
                    Message = $"Refresh failed: {ex.Message}",
                    Error = ex
                };

                OnCurrencyRefreshCompleted(eventArgs);
                _loggingService?.LogError($"Currency refresh failed", ex);

                return false;
            } finally {
                IsRefreshing = false;
            }
        }

        private void OnCurrencyRefreshCompleted(CurrencyRefreshEventArgs args) {
            CurrencyRefreshCompleted?.Invoke(this, args);
        }

        private void OnRefreshStatusChanged(string status) {
            RefreshStatusChanged?.Invoke(this, status);
        }

        #endregion

        public void Dispose() {
            try {
                _refreshTimer?.Stop();
                _refreshTimer?.Dispose();
                _loggingService?.LogInfo("CurrencyAutoRefreshService disposed");
            } catch (Exception ex) {
                _loggingService?.LogError("Error disposing CurrencyAutoRefreshService", ex);
            }
        }
    }
}