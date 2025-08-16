using CurrencyConvertor.Services.Interfaces;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Configuration;

namespace CurrencyConvertor.Services {
    /// <summary>
    /// Fallback currency service for offline scenarios (LSP - Liskov Substitution Principle)
    /// </summary>
    public class FallbackCurrencyMetadataService : ICurrencyMetadataService {
        private readonly ILoggingService _loggingService;
        private readonly List<string> _fallbackCurrencies;

        public FallbackCurrencyMetadataService(ILoggingService loggingService = null) {
            _loggingService = loggingService;
            _fallbackCurrencies = new List<string>
            {
                "EUR", "USD", "GBP", "JPY", "CAD", "AUD", "CHF", "CNY", "SEK", "NZD",
                "NOK", "DKK", "PLN", "CZK", "HUF", "RON", "BGN", "HRK", "RUB", "TRY"
            };
        }

        public async Task<List<string>> GetAvailableCurrenciesAsync() {
            _loggingService?.LogInfo("Using fallback currency list");

            // Simulate async operation for consistency with interface
            await Task.Delay(1);

            return new List<string>(_fallbackCurrencies);
        }
    }

    /// <summary>
    /// Composite service that tries primary service first, then fallback (OCP - Open/Closed Principle)
    /// Thread-safe implementation that supports concurrent operations
    /// </summary>
    public class ResilientCurrencyMetadataService : ICurrencyMetadataService {
        private readonly ICurrencyMetadataService _primaryService;
        private readonly ICurrencyMetadataService _fallbackService;
        private readonly ILoggingService _loggingService;
        private readonly object _lockObject = new object();

        public ResilientCurrencyMetadataService(
            ICurrencyMetadataService primaryService,
            ICurrencyMetadataService fallbackService,
            ILoggingService loggingService = null) {
            _primaryService = primaryService ?? throw new ArgumentNullException(nameof(primaryService));
            _fallbackService = fallbackService ?? throw new ArgumentNullException(nameof(fallbackService));
            _loggingService = loggingService;
        }

        public async Task<List<string>> GetAvailableCurrenciesAsync() {
            try {
                _loggingService?.LogInfo("Attempting to get currencies from primary service");
                var result = await _primaryService.GetAvailableCurrenciesAsync();
                _loggingService?.LogInfo($"Primary service succeeded, returned {result.Count} currencies");
                return result;
            } catch (Exception ex) {
                _loggingService?.LogWarning($"Primary service failed, using fallback: {ex.Message}");

                try {
                    var fallbackResult = await _fallbackService.GetAvailableCurrenciesAsync();
                    _loggingService?.LogInfo($"Fallback service succeeded, returned {fallbackResult.Count} currencies");
                    return fallbackResult;
                } catch (Exception fallbackEx) {
                    _loggingService?.LogError($"Both primary and fallback services failed. Primary: {ex.Message}, Fallback: {fallbackEx.Message}", fallbackEx);
                    throw new Exception($"All currency services failed. Primary error: {ex.Message}");
                }
            }
        }
    }

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

    /// <summary>
    /// Event arguments for currency refresh events
    /// </summary>
    public class CurrencyRefreshEventArgs : EventArgs {
        public bool Success { get; set; }
        public List<string> Currencies { get; set; } = new List<string>();
        public DateTime RefreshTime { get; set; }
        public bool IsManualRefresh { get; set; }
        public string Message { get; set; } = string.Empty;
        public Exception Error { get; set; }
    }

    /// <summary>
    /// Statistics about the auto-refresh service
    /// </summary>
    public class RefreshStatistics {
        public bool IsEnabled { get; set; }
        public bool IsRefreshing { get; set; }
        public TimeSpan RefreshInterval { get; set; }
        public DateTime LastRefreshTime { get; set; }
        public TimeSpan TimeSinceLastRefresh { get; set; }
        public DateTime? NextRefreshTime { get; set; }

        public override string ToString() {
            if (!IsEnabled)
                return "Auto-refresh disabled";

            var status = IsRefreshing ? "Refreshing..." : "Ready";
            var lastRefresh = LastRefreshTime == DateTime.MinValue
                ? "Never"
                : $"{TimeSinceLastRefresh.TotalMinutes:F0} min ago";

            return $"{status} | Interval: {RefreshInterval.TotalMinutes:F0} min | Last: {lastRefresh}";
        }
    }

    /// <summary>
    /// Enhanced date validation service that follows SOLID principles (SRP - Single Responsibility)
    /// Provides comprehensive date validation for currency services
    /// Now supports configurable date ranges via App.config
    /// </summary>
    public class DateValidationService {
        private readonly ILoggingService _loggingService;

        // Default configuration constants (fallback if App.config is not available)
        private static readonly DateTime DefaultMinAllowedDate = new DateTime(1999, 1, 1); // Frankfurter API earliest date
        private static readonly DateTime DefaultMaxAllowedDate = DateTime.Today.AddDays(1); // Allow today + 1 day buffer
        private static readonly TimeSpan DefaultMaxDateRangeSpan = TimeSpan.FromDays(90); // Default 90 days range
        private static readonly TimeSpan DefaultMinDateRangeSpan = TimeSpan.FromDays(1); // Default 1 day range

        // Configurable properties loaded from App.config
        private readonly DateTime _minAllowedDate;
        private readonly DateTime _maxAllowedDate;
        private readonly TimeSpan _maxDateRangeSpan;
        private readonly TimeSpan _minDateRangeSpan;

        public DateValidationService(ILoggingService loggingService = null) {
            _loggingService = loggingService;
            
            // Load configuration from App.config
            _minAllowedDate = DefaultMinAllowedDate; // API constraint, not configurable
            _maxAllowedDate = DefaultMaxAllowedDate; // Dynamic based on today's date
            _maxDateRangeSpan = LoadMaxDateRangeFromConfig();
            _minDateRangeSpan = LoadMinDateRangeFromConfig();

            _loggingService?.LogInfo($"DateValidationService initialized: MaxRange={_maxDateRangeSpan.Days} days, MinRange={_minDateRangeSpan.Days} days");
        }

        /// <summary>
        /// Loads maximum date range from App.config
        /// </summary>
        private TimeSpan LoadMaxDateRangeFromConfig() {
            try {
                // Read from App.config using helper method
                var configValue = GetAppSetting("MaxHistoricalDataDays", null);
                if (!string.IsNullOrEmpty(configValue) && int.TryParse(configValue, out var maxDays) && maxDays > 0) {
                    _loggingService?.LogInfo($"Loaded MaxHistoricalDataDays from App.config: {maxDays} days");
                    return TimeSpan.FromDays(maxDays);
                }
                
                _loggingService?.LogWarning($"Invalid or missing MaxHistoricalDataDays in App.config: '{configValue}', using default");
            } catch (Exception ex) {
                _loggingService?.LogWarning($"Failed to load MaxHistoricalDataDays from App.config: {ex.Message}");
            }
            
            // Use default if config is not available or invalid
            const int defaultMaxDays = 90;
            _loggingService?.LogInfo($"Using default MaxHistoricalDataDays: {defaultMaxDays} days");
            return TimeSpan.FromDays(defaultMaxDays);
        }

        /// <summary>
        /// Loads minimum date range from App.config
        /// </summary>
        private TimeSpan LoadMinDateRangeFromConfig() {
            try {
                // Read from App.config using helper method
                var configValue = GetAppSetting("MinHistoricalDataDays", null);
                if (!string.IsNullOrEmpty(configValue) && int.TryParse(configValue, out var minDays) && minDays > 0) {
                    _loggingService?.LogInfo($"Loaded MinHistoricalDataDays from App.config: {minDays} days");
                    return TimeSpan.FromDays(minDays);
                }
                
                _loggingService?.LogWarning($"Invalid or missing MinHistoricalDataDays in App.config: '{configValue}', using default");
            } catch (Exception ex) {
                _loggingService?.LogWarning($"Failed to load MinHistoricalDataDays from App.config: {ex.Message}");
            }
            
            // Use default if config is not available or invalid
            const int defaultMinDays = 1;
            _loggingService?.LogInfo($"Using default MinHistoricalDataDays: {defaultMinDays} days");
            return TimeSpan.FromDays(defaultMinDays);
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
        /// Gets the current maximum allowed days for historical data
        /// </summary>
        public int MaxAllowedDays => (int)_maxDateRangeSpan.TotalDays;

        /// <summary>
        /// Gets the current minimum required days for historical data
        /// </summary>
        public int MinRequiredDays => (int)_minDateRangeSpan.TotalDays;

        /// <summary>
        /// Validates a date range for historical data requests
        /// </summary>
        /// <param name="startDate">Start date of the range</param>
        /// <param name="endDate">End date of the range</param>
        /// <returns>Validation result with detailed information</returns>
        public DateValidationResult ValidateeDateRange(DateTime? startDate, DateTime? endDate) {
            _loggingService?.LogInfo($"Validating date range: {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");

            // Check for null dates
            if (!startDate.HasValue || !endDate.HasValue) {
                var message = "Both start date and end date must be provided";
                _loggingService?.LogWarning(message);
                return new DateValidationResult(false, message);
            }

            // Check date order
            if (startDate.Value > endDate.Value) {
                var message = $"Start date ({startDate.Value:yyyy-MM-dd}) must be before or equal to end date ({endDate.Value:yyyy-MM-dd})";
                _loggingService?.LogWarning(message);
                return new DateValidationResult(false, message);
            }

            // Check if dates are within allowed range
            if (startDate.Value < _minAllowedDate) {
                var message = $"Start date ({startDate.Value:yyyy-MM-dd}) cannot be before {_minAllowedDate:yyyy-MM-dd}";
                _loggingService?.LogWarning(message);
                return new DateValidationResult(false, message);
            }

            if (endDate.Value > _maxAllowedDate) {
                var message = $"End date ({endDate.Value:yyyy-MM-dd}) cannot be after {_maxAllowedDate:yyyy-MM-dd}";
                _loggingService?.LogWarning(message);
                return new DateValidationResult(false, message);
            }

            // Check date range span using configurable values
            var timeSpan = endDate.Value - startDate.Value;

            if (timeSpan < _minDateRangeSpan) {
                var message = $"Date range must be at least {_minDateRangeSpan.Days} day(s)";
                _loggingService?.LogWarning(message);
                return new DateValidationResult(false, message);
            }

            if (timeSpan > _maxDateRangeSpan) {
                var message = $"Date range cannot exceed {_maxDateRangeSpan.Days} days (configured limit)";
                _loggingService?.LogWarning(message);
                return new DateValidationResult(false, message);
            }

            // Check for weekend-only ranges (optional business rule)
            if (IsWeekendOnlyRange(startDate.Value, endDate.Value)) {
                var message = "Date range contains only weekends. Market data may be limited.";
                _loggingService?.LogInfo(message);
                return new DateValidationResult(true, message, true); // Valid but with warning
            }

            // Check for future dates
            if (startDate.Value > DateTime.Today) {
                var message = "Start date is in the future. Historical data may not be available.";
                _loggingService?.LogInfo(message);
                return new DateValidationResult(true, message, true); // Valid but with warning
            }

            _loggingService?.LogInfo("Date range validation passed");
            return new DateValidationResult(true, "Date range is valid");
        }

        /// <summary>
        /// Validates a single date for currency conversion
        /// </summary>
        /// <param name="date">Date to validate</param>
        /// <returns>Validation result</returns>
        public DateValidationResult ValidateDate(DateTime? date) {
            if (!date.HasValue) {
                return new DateValidationResult(false, "Date must be provided");
            }

            if (date.Value < _minAllowedDate) {
                return new DateValidationResult(false, $"Date cannot be before {_minAllowedDate:yyyy-MM-dd}");
            }

            if (date.Value > _maxAllowedDate) {
                return new DateValidationResult(false, $"Date cannot be after {_maxAllowedDate:yyyy-MM-dd}");
            }

            return new DateValidationResult(true, "Date is valid");
        }

        /// <summary>
        /// Suggests a valid date range based on current date
        /// </summary>
        /// <returns>Suggested date range</returns>
        public (DateTime startDate, DateTime endDate) SuggestDateRange() {
            var endDate = DateTime.Today;
            var startDate = endDate.AddDays(-30); // Default to last 30 days

            // Ensure start date is not before minimum allowed
            if (startDate < _minAllowedDate) {
                startDate = _minAllowedDate;
            }

            _loggingService?.LogInfo($"Suggested date range: {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");
            return (startDate, endDate);
        }

        /// <summary>
        /// Adjusts an invalid date range to make it valid
        /// </summary>
        /// <param name="startDate">Original start date</param>
        /// <param name="endDate">Original end date</param>
        /// <returns>Adjusted valid date range</returns>
        public (DateTime startDate, DateTime endDate) AdjustDateRange(DateTime? startDate, DateTime? endDate) {
            var suggested = SuggestDateRange();

            var adjustedStart = startDate ?? suggested.startDate;
            var adjustedEnd = endDate ?? suggested.endDate;

            // Clamp to allowed range
            if (adjustedStart < _minAllowedDate) adjustedStart = _minAllowedDate;
            if (adjustedEnd > _maxAllowedDate) {
                adjustedEnd = _maxAllowedDate;
                adjustedStart = adjustedEnd.AddDays(-1);
            }
            // Ensure proper order
            if (adjustedStart > adjustedEnd) {
                adjustedEnd = adjustedStart.AddDays(1);
                if (adjustedEnd > _maxAllowedDate) {
                    adjustedEnd = _maxAllowedDate;
                    adjustedStart = adjustedEnd.AddDays(-1);
                }
            }

            // Ensure minimum range
            if ((adjustedEnd - adjustedStart) < _minDateRangeSpan) {
                adjustedEnd = adjustedStart.AddDays(1);
                if (adjustedEnd > _maxAllowedDate) {
                    adjustedEnd = _maxAllowedDate;
                    adjustedStart = adjustedEnd.AddDays(-1);
                }
            }

            _loggingService?.LogInfo($"Adjusted date range from ({startDate:yyyy-MM-dd}, {endDate:yyyy-MM-dd}) to ({adjustedStart:yyyy-MM-dd}, {adjustedEnd:yyyy-MM-dd})");
            return (adjustedStart, adjustedEnd);
        }

        /// <summary>
        /// Checks if the date range contains only weekends
        /// </summary>
        private bool IsWeekendOnlyRange(DateTime startDate, DateTime endDate) {
            var current = startDate;
            while (current <= endDate) {
                if (current.DayOfWeek != DayOfWeek.Saturday && current.DayOfWeek != DayOfWeek.Sunday) {
                    return false; // Found a weekday
                }
                current = current.AddDays(1);
            }
            return true; // Only weekends found
        }
    }

    /// <summary>
    /// Represents the result of date validation
    /// </summary>
    public class DateValidationResult {
        public bool IsValid { get; }
        public string Message { get; }
        public bool HasWarning { get; }

        public DateValidationResult(bool isValid, string message, bool hasWarning = false) {
            IsValid = isValid;
            Message = message ?? string.Empty;
            HasWarning = hasWarning;
        }

        public override string ToString() {
            var status = IsValid ? "Valid" : "Invalid";
            var warning = HasWarning ? " (Warning)" : "";
            return $"{status}{warning}: {Message}";
        }
    }
}
