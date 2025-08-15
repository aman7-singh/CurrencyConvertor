using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CurrencyConvertor.Services.Interfaces;


namespace CurrencyConvertor.Services {
    /// <summary>
    /// Application initialization service (SRP - Single Responsibility)
    /// </summary>
    public class ApplicationInitializationService : IInitializationService {
        private readonly ICurrencyService _currencyService;
        private readonly ILoggingService _loggingService;

        public event EventHandler<string> StatusChanged;
        public event EventHandler<bool> LoadingStateChanged;

        public ApplicationInitializationService(
            ICurrencyService currencyService,
            ILoggingService loggingService) {
            _currencyService = currencyService ?? throw new ArgumentNullException(nameof(currencyService));
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
        }

        public async Task<bool> InitializeApplicationAsync() {
            try {
                _loggingService.LogInfo("Starting application initialization");
                OnStatusChanged("Loading currencies...");
                OnLoadingStateChanged(true);

                var currencies = await _currencyService.GetCurrenciesAsync();

                _loggingService.LogInfo($"Successfully loaded {currencies.Count} currencies");
                OnStatusChanged("Ready");

                return true;
            } catch (Exception ex) {
                _loggingService.LogError("Failed to initialize application", ex);
                OnStatusChanged("Failed to load currencies");
                return false;
            } finally {
                OnLoadingStateChanged(false);
            }
        }

        protected virtual void OnStatusChanged(string status) {
            StatusChanged?.Invoke(this, status);
        }

        protected virtual void OnLoadingStateChanged(bool isLoading) {
            LoadingStateChanged?.Invoke(this, isLoading);
        }
    }
}
