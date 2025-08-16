using CurrencyConvertor.Models;
using System.Threading.Tasks;

namespace CurrencyConvertor.Services.Interfaces {
    /// <summary>
    /// Interface for currency conversion operations (ISP - Interface Segregation)
    /// </summary>
    public interface ICurrencyConversionService {
        Task<CurrencyConversion> ConvertCurrencyAsync(string fromCurrency, string toCurrency, decimal amount);
    }
}