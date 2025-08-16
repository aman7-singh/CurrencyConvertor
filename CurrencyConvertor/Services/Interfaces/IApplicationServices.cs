using CurrencyConvertor.Models;
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

    /// <summary>
    /// Interface for currency metadata operations (ISP - Interface Segregation)
    /// </summary>
    public interface ICurrencyMetadataService {
        Task<List<string>> GetAvailableCurrenciesAsync();
    }

    /// <summary>
    /// Interface for currency conversion operations (ISP - Interface Segregation)
    /// </summary>
    public interface ICurrencyConversionService {
        Task<CurrencyConversion> ConvertCurrencyAsync(string fromCurrency, string toCurrency, decimal amount);
    }

    /// <summary>
    /// Interface for historical data operations (ISP - Interface Segregation)
    /// </summary>
    public interface IHistoricalDataService {
        Task<List<HistoricalRate>> GetHistoricalRatesAsync(string fromCurrency, string toCurrency, DateTime startDate, DateTime endDate);
    }

    /// <summary>
    /// Interface for cache configuration (ISP - Interface Segregation)
    /// </summary>
    public interface ICacheConfiguration {
        CacheEvictionStrategy EvictionStrategy { get; }
        int MaxAgeMinutes { get; }
        int MaxElements { get; }
        int CleanupIntervalMinutes { get; }
        bool IsEnabled { get; }
    }

    /// <summary>
    /// Generic interface for cache services (ISP - Interface Segregation)
    /// </summary>
    public interface ICacheService<TKey, TValue> {
        Task<TValue> GetAsync(TKey key);
        Task SetAsync(TKey key, TValue value);
        Task<bool> ContainsAsync(TKey key);
        Task RemoveAsync(TKey key);
        Task ClearAsync();
        CacheStatistics GetStatistics();
        Task CleanupAsync();
    }

    /// <summary>
    /// Interface for historical data cache operations (ISP - Interface Segregation)
    /// </summary>
    public interface IHistoricalDataCacheService {
        Task<List<HistoricalRate>> GetHistoricalRatesAsync(string cacheKey, Func<Task<List<HistoricalRate>>> dataFactory);
        Task ClearAsync();
        Task CleanupAsync();
        CacheStatistics GetStatistics();
    }
}

/// <summary>
/// Cache eviction strategies
/// </summary>
public enum CacheEvictionStrategy {
    TimeBased,
    SizeBased
}

/// <summary>
/// Statistics about cache performance
/// </summary>
public class CacheStatistics {
    public int TotalItems { get; set; }
    public int HitCount { get; set; }
    public int MissCount { get; set; }
    public double HitRatio => HitCount + MissCount > 0 ? (double)HitCount / (HitCount + MissCount) : 0;
    public DateTime LastCleanup { get; set; }
    public int ItemsEvicted { get; set; }
    public long MemoryUsageBytes { get; set; }

    public override string ToString() {
        return $"Size: {TotalItems}, Hits: {HitCount}, Misses: {MissCount}, Hit Ratio: {HitRatio:P2}";
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
