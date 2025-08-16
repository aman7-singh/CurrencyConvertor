using System.Collections.Generic;
using System.Threading.Tasks;

namespace CurrencyConvertor.Services.Interfaces {
    /// <summary>
    /// Interface for currency metadata operations (ISP - Interface Segregation)
    /// </summary>
    public interface ICurrencyMetadataService {
        Task<List<string>> GetAvailableCurrenciesAsync();
    }
}