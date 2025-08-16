using CurrencyConvertor.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CurrencyConvertor.Services {
    /// <summary>
    /// Composite service that tries primary service first, then fallback (OCP - Open/Closed Principle)
    /// Thread-safe implementation that supports concurrent operations
    /// </summary>
    public class ResilientCurrencyMetadataService : ICurrencyMetadataService {
        private readonly ICurrencyMetadataService _primaryService;
        private readonly ICurrencyMetadataService _fallbackService;
        private readonly ILoggingService _loggingService;
        private readonly object _lockObject = new object();

        public ResilientCurrencyMetadataService(
            ICurrencyMetadataService primaryService,
            ICurrencyMetadataService fallbackService,
            ILoggingService loggingService = null) {
            _primaryService = primaryService ?? throw new ArgumentNullException(nameof(primaryService));
            _fallbackService = fallbackService ?? throw new ArgumentNullException(nameof(fallbackService));
            _loggingService = loggingService;
        }

        public async Task<List<string>> GetAvailableCurrenciesAsync() {
            try {
                _loggingService?.LogInfo("Attempting to get currencies from primary service");
                var result = await _primaryService.GetAvailableCurrenciesAsync();
                _loggingService?.LogInfo($"Primary service succeeded, returned {result.Count} currencies");
                return result;
            } catch (Exception ex) {
                _loggingService?.LogWarning($"Primary service failed, using fallback: {ex.Message}");

                try {
                    var fallbackResult = await _fallbackService.GetAvailableCurrenciesAsync();
                    _loggingService?.LogInfo($"Fallback service succeeded, returned {fallbackResult.Count} currencies");
                    return fallbackResult;
                } catch (Exception fallbackEx) {
                    _loggingService?.LogError($"Both primary and fallback services failed. Primary: {ex.Message}, Fallback: {fallbackEx.Message}", fallbackEx);
                    throw new Exception($"All currency services failed. Primary error: {ex.Message}");
                }
            }
        }
    }
}