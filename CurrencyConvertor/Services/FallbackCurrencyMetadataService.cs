using CurrencyConvertor.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CurrencyConvertor.Services {
    /// <summary>
    /// Fallback currency service for offline scenarios (LSP - Liskov Substitution Principle)
    /// </summary>
    public class FallbackCurrencyMetadataService : ICurrencyMetadataService {
        private readonly ILoggingService _loggingService;
        private readonly List<string> _fallbackCurrencies;

        public FallbackCurrencyMetadataService(ILoggingService loggingService = null) {
            _loggingService = loggingService;
            _fallbackCurrencies = new List<string>
            {
                "EUR", "USD", "GBP", "JPY", "CAD", "AUD", "CHF", "CNY", "SEK", "NZD",
                "NOK", "DKK", "PLN", "CZK", "HUF", "RON", "BGN", "HRK", "RUB", "TRY"
            };
        }

        public async Task<List<string>> GetAvailableCurrenciesAsync() {
            _loggingService?.LogInfo("Using fallback currency list");

            // Simulate async operation for consistency with interface
            await Task.Delay(1);

            return new List<string>(_fallbackCurrencies);
        }
    }
}