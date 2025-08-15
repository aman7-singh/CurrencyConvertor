using CurrencyConvertor.Services.Interfaces;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace CurrencyConvertor.Services {
    /// <summary>
    /// WPF-specific notification service implementation (SRP - Single Responsibility)
    /// </summary>
    public class WpfNotificationService : INotificationService {
        public void ShowError(string message, string title = "Error") {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public void ShowWarning(string message, string title = "Warning") {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        public void ShowInfo(string message, string title = "Information") {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    /// <summary>
    /// Enhanced input validation service implementation (SRP - Single Responsibility)
    /// Now integrates with comprehensive date validation
    /// </summary>
    public class ValidationService : IValidationService {
        private readonly DateValidationService _dateValidationService;
        private readonly ILoggingService _loggingService;

        public ValidationService(ILoggingService loggingService = null) {
            _loggingService = loggingService;
            _dateValidationService = new DateValidationService(_loggingService);
        }

        public bool IsValidAmount(string amount, out decimal validAmount) {
            validAmount = 0;

            if (string.IsNullOrWhiteSpace(amount)) {
                _loggingService?.LogWarning("Amount validation failed: empty or null input");
                return false;
            }

            if (!decimal.TryParse(amount, out validAmount)) {
                _loggingService?.LogWarning($"Amount validation failed: could not parse '{amount}' as decimal");
                return false;
            }

            if (validAmount <= 0) {
                _loggingService?.LogWarning($"Amount validation failed: value {validAmount} must be greater than zero");
                return false;
            }

            // Check for reasonable upper limit (e.g., 1 billion)
            if (validAmount > 1_000_000_000) {
                _loggingService?.LogWarning($"Amount validation failed: value {validAmount} exceeds maximum allowed amount");
                return false;
            }

            _loggingService?.LogInfo($"Amount validation passed: {validAmount}");
            return true;
        }

        public bool IsValidCurrency(string currency) {
            if (string.IsNullOrWhiteSpace(currency)) {
                _loggingService?.LogWarning("Currency validation failed: empty or null input");
                return false;
            }

            // More lenient validation - allow any non-empty currency string
            // This allows for initialization scenarios where currencies might not be fully loaded
            var isValid = currency.Trim().Length >= 3; // Most currency codes are 3 characters

            if (!isValid) {
                _loggingService?.LogWarning($"Currency validation failed: '{currency}' is too short");
            } else {
                _loggingService?.LogInfo($"Currency validation passed: '{currency}'");
            }

            return isValid;
        }

        public bool IsValidDateRange(DateTime? startDate, DateTime? endDate) {
            _loggingService?.LogInfo($"Validating date range: {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");

            var validationResult = _dateValidationService.ValidateeDateRange(startDate, endDate);

            if (!validationResult.IsValid) {
                _loggingService?.LogWarning($"Date range validation failed: {validationResult.Message}");
                return false;
            }

            if (validationResult.HasWarning) {
                _loggingService?.LogWarning($"Date range validation warning: {validationResult.Message}");
            } else {
                _loggingService?.LogInfo("Date range validation passed");
            }

            return true;
        }

        /// <summary>
        /// Enhanced date range validation with detailed result information
        /// </summary>
        /// <param name="startDate">Start date</param>
        /// <param name="endDate">End date</param>
        /// <returns>Detailed validation result</returns>
        public DateValidationResult ValidateDateRangeDetailed(DateTime? startDate, DateTime? endDate) {
            return _dateValidationService.ValidateeDateRange(startDate, endDate);
        }

        /// <summary>
        /// Validates a single date
        /// </summary>
        /// <param name="date">Date to validate</param>
        /// <returns>True if valid, false otherwise</returns>
        public bool IsValidDate(DateTime? date) {
            var validationResult = _dateValidationService.ValidateDate(date);

            if (!validationResult.IsValid) {
                _loggingService?.LogWarning($"Date validation failed: {validationResult.Message}");
            } else {
                _loggingService?.LogInfo("Date validation passed");
            }

            return validationResult.IsValid;
        }

        /// <summary>
        /// Gets a suggested valid date range
        /// </summary>
        /// <returns>Suggested date range tuple</returns>
        public (DateTime startDate, DateTime endDate) GetSuggestedDateRange() {
            return _dateValidationService.SuggestDateRange();
        }

        /// <summary>
        /// Adjusts an invalid date range to make it valid
        /// </summary>
        /// <param name="startDate">Original start date</param>
        /// <param name="endDate">Original end date</param>
        /// <returns>Adjusted valid date range</returns>
        public (DateTime startDate, DateTime endDate) AdjustToValidDateRange(DateTime? startDate, DateTime? endDate) {
            return _dateValidationService.AdjustDateRange(startDate, endDate);
        }
    }

    /// <summary>
    /// Simple logging service implementation (SRP - Single Responsibility)
    /// </summary>
    public class ConsoleLoggingService : ILoggingService {
        public void LogError(string message, Exception exception = null) {
            Console.WriteLine($"[ERROR] {DateTime.Now:yyyy-MM-dd HH:mm:ss}: {message}");
            if (exception != null) {
                Console.WriteLine($"Exception: {exception}");
            }
        }

        public void LogWarning(string message) {
            Console.WriteLine($"[WARNING] {DateTime.Now:yyyy-MM-dd HH:mm:ss}: {message}");
        }

        public void LogInfo(string message) {
            Console.WriteLine($"[INFO] {DateTime.Now:yyyy-MM-dd HH:mm:ss}: {message}");
        }
    }
}
