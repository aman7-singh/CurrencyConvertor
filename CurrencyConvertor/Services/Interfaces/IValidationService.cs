using CurrencyConvertor.Services.Models;
using System;

namespace CurrencyConvertor.Services.Interfaces {
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
}