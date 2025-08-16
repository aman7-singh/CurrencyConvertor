using CurrencyConvertor.Services.Models;
using System.Threading.Tasks;

namespace CurrencyConvertor.Services.Interfaces {
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
}