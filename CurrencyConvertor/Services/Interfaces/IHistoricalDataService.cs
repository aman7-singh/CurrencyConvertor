using CurrencyConvertor.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CurrencyConvertor.Services.Interfaces {
    /// <summary>
    /// Interface for historical data operations (ISP - Interface Segregation)
    /// </summary>
    public interface IHistoricalDataService {
        Task<List<HistoricalRate>> GetHistoricalRatesAsync(string fromCurrency, string toCurrency, DateTime startDate, DateTime endDate);
    }
}