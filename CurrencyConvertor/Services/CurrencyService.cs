using CurrencyConvertor.Models;
using CurrencyConvertor.Services.Interfaces;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace CurrencyConvertor.Services {
    /// <summary>
    /// Service implementation for currency operations using Frankfurter API
    /// Now implementing segregated interfaces (ISP - Interface Segregation Principle)
    /// Fixed to support concurrent I/O operations using semaphore and individual WebClient instances
    /// </summary>
    public class CurrencyService : ICurrencyService, ICurrencyConversionService, IHistoricalDataService, ICurrencyMetadataService, IDisposable {
        private const string Host = "api.frankfurter.app";
        private const string DateFormat = "yyyy-MM-dd";
        private readonly ILoggingService _loggingService;
        private readonly SemaphoreSlim _semaphore;

        public CurrencyService(ILoggingService loggingService = null) {
            _loggingService = loggingService;

            // Semaphore to limit concurrent requests and prevent WebClient conflicts
            _semaphore = new SemaphoreSlim(3, 3); // Allow up to 3 concurrent requests
        }

        #region ICurrencyMetadataService Implementation

        public async Task<List<string>> GetAvailableCurrenciesAsync() {
            return await GetCurrenciesAsync();
        }

        #endregion

        #region ICurrencyConversionService Implementation

        public async Task<CurrencyConversion> ConvertCurrencyAsync(string fromCurrency, string toCurrency, decimal amount) {
            return await GetLatestConversionAsync(fromCurrency, toCurrency, amount);
        }

        #endregion

        #region IHistoricalDataService Implementation

        // Implementation already exists in GetHistoricalRatesAsync

        #endregion

        #region ICurrencyService Implementation (maintains backward compatibility - OCP)

        public async Task<List<string>> GetCurrenciesAsync() {
            await _semaphore.WaitAsync();
            WebClient webClient = null;

            try {
                _loggingService?.LogInfo("Fetching currencies from API");

                // Create a new WebClient instance for this request to avoid concurrency issues
                webClient = CreateWebClient();
                var url = $"https://{Host}/currencies";

                _loggingService?.LogInfo($"Making request to: {url}");
                var response = await webClient.DownloadStringTaskAsync(url);

                _loggingService?.LogInfo($"Response received, length: {response.Length}");
                var currencies = JsonConvert.DeserializeObject<Dictionary<string, string>>(response);

                if (currencies == null || currencies.Count == 0) {
                    throw new Exception("No currencies returned from API");
                }

                _loggingService?.LogInfo($"Successfully fetched {currencies.Count} currencies");
                return new List<string>(currencies.Keys);
            } catch (WebException webEx) {
                var response = webEx.Response as HttpWebResponse;
                var errorMessage = response != null
                    ? $"API Error {(int)response.StatusCode}: {response.StatusDescription}. URL: https://{Host}/currencies"
                    : $"Network error: {webEx.Message}";

                _loggingService?.LogError(errorMessage, webEx);
                throw new Exception(errorMessage);
            } catch (Exception ex) {
                var errorMessage = $"Failed to fetch currencies: {ex.Message}";
                _loggingService?.LogError(errorMessage, ex);
                throw new Exception(errorMessage);
            } finally {
                webClient?.Dispose();
                _semaphore.Release();
            }
        }

        public async Task<CurrencyConversion> GetLatestConversionAsync(string fromCurrency, string toCurrency, decimal amount) {
            await _semaphore.WaitAsync();
            WebClient webClient = null;

            try {
                _loggingService?.LogInfo($"Converting {amount} {fromCurrency} to {toCurrency}");

                // Input validation
                if (string.IsNullOrWhiteSpace(fromCurrency))
                    throw new ArgumentException("From currency cannot be empty", nameof(fromCurrency));

                if (string.IsNullOrWhiteSpace(toCurrency))
                    throw new ArgumentException("To currency cannot be empty", nameof(toCurrency));

                if (amount <= 0)
                    throw new ArgumentException("Amount must be greater than zero", nameof(amount));

                // Create a new WebClient instance for this request
                webClient = CreateWebClient();
                var url = $"https://{Host}/latest?amount={amount}&from={fromCurrency}&to={toCurrency}";
                _loggingService?.LogInfo($"Making conversion request to: {url}");

                var response = await webClient.DownloadStringTaskAsync(url);
                _loggingService?.LogInfo($"Conversion response received, length: {response.Length}");

                var result = JsonConvert.DeserializeObject<ConversionApiResponse>(response);

                if (result == null) {
                    throw new Exception("Invalid response from conversion API");
                }

                if (result.Rates == null || result.Rates.Count == 0) {
                    throw new Exception($"No exchange rates returned for {fromCurrency} to {toCurrency}");
                }

                if (!result.Rates.ContainsKey(toCurrency)) {
                    throw new Exception($"No exchange rate found for {fromCurrency} to {toCurrency}");
                }

                var conversion = new CurrencyConversion {
                    Amount = result.Amount,
                    FromCurrency = fromCurrency,
                    ToCurrency = toCurrency,
                    ConvertedAmount = result.Rates[toCurrency],
                    Date = DateTime.Parse(result.Date)
                };

                _loggingService?.LogInfo($"Successfully converted {amount} {fromCurrency} to {conversion.ConvertedAmount} {toCurrency}");
                return conversion;
            } catch (WebException webEx) {
                var response = webEx.Response as HttpWebResponse;
                var errorMessage = response != null
                    ? $"API Error {(int)response.StatusCode}: {response.StatusDescription}"
                    : $"Network error: {webEx.Message}";

                _loggingService?.LogError(errorMessage, webEx);
                throw new Exception(errorMessage);
            } catch (ArgumentException argEx) {
                _loggingService?.LogError($"Invalid argument: {argEx.Message}", argEx);
                throw;
            } catch (Exception ex) {
                var errorMessage = $"Failed to get latest conversion: {ex.Message}";
                _loggingService?.LogError(errorMessage, ex);
                throw new Exception(errorMessage);
            } finally {
                webClient?.Dispose();
                _semaphore.Release();
            }
        }

        public async Task<List<HistoricalRate>> GetHistoricalRatesAsync(string fromCurrency, string toCurrency, DateTime startDate, DateTime endDate) {
            await _semaphore.WaitAsync();
            WebClient webClient = null;

            try {
                _loggingService?.LogInfo($"Fetching historical rates for {fromCurrency} to {toCurrency} from {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");

                // Input validation
                if (string.IsNullOrWhiteSpace(fromCurrency))
                    throw new ArgumentException("From currency cannot be empty", nameof(fromCurrency));

                if (string.IsNullOrWhiteSpace(toCurrency))
                    throw new ArgumentException("To currency cannot be empty", nameof(toCurrency));

                if (startDate > endDate)
                    throw new ArgumentException("Start date must be before or equal to end date");

                // Create a new WebClient instance for this request
                webClient = CreateWebClient();
                var start = startDate.ToString(DateFormat);
                var end = endDate.ToString(DateFormat);
                var url = $"https://{Host}/{start}..{end}?from={fromCurrency}&to={toCurrency}";

                _loggingService?.LogInfo($"Making historical data request to: {url}");
                var response = await webClient.DownloadStringTaskAsync(url);
                _loggingService?.LogInfo($"Historical data response received, length: {response.Length}");

                var result = JsonConvert.DeserializeObject<HistoricalApiResponse>(response);

                if (result == null) {
                    throw new Exception("Invalid response from historical data API");
                }

                var historicalRates = new List<HistoricalRate>();
                if (result.Rates != null) {
                    foreach (var rate in result.Rates) {
                        if (rate.Value != null && rate.Value.ContainsKey(toCurrency)) {
                            historicalRates.Add(new HistoricalRate {
                                Date = DateTime.Parse(rate.Key),
                                FromCurrency = fromCurrency,
                                ToCurrency = toCurrency,
                                Rate = rate.Value[toCurrency]
                            });
                        }
                    }
                }

                _loggingService?.LogInfo($"Successfully fetched {historicalRates.Count} historical rates");
                return historicalRates;
            } catch (WebException webEx) {
                var response = webEx.Response as HttpWebResponse;
                var errorMessage = response != null
                    ? $"API Error {(int)response.StatusCode}: {response.StatusDescription}"
                    : $"Network error: {webEx.Message}";

                _loggingService?.LogError(errorMessage, webEx);
                throw new Exception(errorMessage);
            } catch (ArgumentException argEx) {
                _loggingService?.LogError($"Invalid argument: {argEx.Message}", argEx);
                throw;
            } catch (Exception ex) {
                var errorMessage = $"Failed to get historical rates: {ex.Message}";
                _loggingService?.LogError(errorMessage, ex);
                throw new Exception(errorMessage);
            } finally {
                webClient?.Dispose();
                _semaphore.Release();
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Creates a new WebClient instance with proper configuration
        /// </summary>
        private WebClient CreateWebClient() {
            var client = new WebClient();
            client.Headers.Add("User-Agent", "CurrencyConverter/1.0");
            client.Encoding = Encoding.UTF8;
            return client;
        }

        #endregion

        public void Dispose() {
            _semaphore?.Dispose();
        }
    }
}
