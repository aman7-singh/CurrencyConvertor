using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CurrencyConvertor.Services.Interfaces {
    /// <summary>
    /// Interface for user notification services (ISP - Interface Segregation)
    /// </summary>
    public interface INotificationService {
        void ShowError(string message, string title = "Error");
        void ShowWarning(string message, string title = "Warning");
        void ShowInfo(string message, string title = "Information");
    }

    /// <summary>
    /// Enhanced interface for input validation services (ISP - Interface Segregation)
    /// Now includes comprehensive date validation capabilities
    /// </summary>
    public interface IValidationService {
        // Amount validation
        bool IsValidAmount(string amount, out decimal validAmount);

        // Currency validation
        bool IsValidCurrency(string currency);

        // Basic date range validation
        bool IsValidDateRange(DateTime? startDate, DateTime? endDate);

        // Enhanced date validation methods
        bool IsValidDate(DateTime? date);
        DateValidationResult ValidateDateRangeDetailed(DateTime? startDate, DateTime? endDate);
        (DateTime startDate, DateTime endDate) GetSuggestedDateRange();
        (DateTime startDate, DateTime endDate) AdjustToValidDateRange(DateTime? startDate, DateTime? endDate);
    }

    /// <summary>
    /// Interface for logging services (ISP - Interface Segregation)
    /// </summary>
    public interface ILoggingService {
        void LogError(string message, Exception exception = null);
        void LogWarning(string message);
        void LogInfo(string message);
    }

    /// <summary>
    /// Interface for application initialization (SRP - Single Responsibility)
    /// </summary>
    public interface IInitializationService {
        Task<bool> InitializeApplicationAsync();
        event EventHandler<string> StatusChanged;
        event EventHandler<bool> LoadingStateChanged;
    }
}
