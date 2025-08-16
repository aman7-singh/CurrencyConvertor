using CurrencyConvertor.Services.Interfaces;
using CurrencyConvertor.Services.Models;
using System;

namespace CurrencyConvertor.Services {
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
}