using CurrencyConvertor.Models;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CurrencyConvertor.Services.Interfaces {
    /// <summary>
    /// Interface for currency conversion service
    /// </summary>
    public interface ICurrencyService {
        Task<List<string>> GetCurrenciesAsync();
        Task<CurrencyConversion> GetLatestConversionAsync(string fromCurrency, string toCurrency, decimal amount);
        Task<List<HistoricalRate>> GetHistoricalRatesAsync(string fromCurrency, string toCurrency, DateTime startDate, DateTime endDate);
    }
}
