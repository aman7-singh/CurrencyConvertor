using CurrencyConvertor.Models;
using CurrencyConvertor.Services.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CurrencyConvertor.Services.Interfaces {
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