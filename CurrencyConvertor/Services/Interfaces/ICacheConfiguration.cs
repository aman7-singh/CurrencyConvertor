using CurrencyConvertor.Services.Enums;

namespace CurrencyConvertor.Services.Interfaces {
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
}