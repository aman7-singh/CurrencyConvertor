using CurrencyConvertor.Models;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CurrencyConvertor.Services.Interfaces {
    /// <summary>
    /// Focused interface for currency conversion operations (ISP - Interface Segregation)
    /// </summary>
    public interface ICurrencyConversionService {
        Task<CurrencyConversion> ConvertCurrencyAsync(string fromCurrency, string toCurrency, decimal amount);
    }

    /// <summary>
    /// Focused interface for historical data operations (ISP - Interface Segregation)
    /// </summary>
    public interface IHistoricalDataService {
        Task<List<HistoricalRate>> GetHistoricalRatesAsync(string fromCurrency, string toCurrency, DateTime startDate, DateTime endDate);
    }

    /// <summary>
    /// Focused interface for currency metadata operations (ISP - Interface Segregation)
    /// </summary>
    public interface ICurrencyMetadataService {
        Task<List<string>> GetAvailableCurrenciesAsync();
    }
}
