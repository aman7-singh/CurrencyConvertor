using CurrencyConvertor.Models;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CurrencyConvertor.ViewModels {
    internal class MainViewModel: ViewModelBase {


        private ObservableCollection<Currency> _availableCurrencies;
        private Currency _selectedSourceCurrency;
        private Currency _selectedTargetCurrency;
        private decimal _sourceAmount;
        private decimal _convertedAmount;
        private DateTime _latestRateDate;
        private DateTime _startDate;
        private DateTime _endDate;
        private ObservableCollection<ExchangeRate> _historicalRates;
        private bool _isLoading;
        private string _errorMessage;

        #region Properties

        public ObservableCollection<Currency> AvailableCurrencies {
            get => _availableCurrencies;
            set => SetProperty(ref _availableCurrencies, value);
        }

        public Currency SelectedSourceCurrency {
            get => _selectedSourceCurrency;
            set {
                if (SetProperty(ref _selectedSourceCurrency, value)) {
                    _ = Task.Run(async () => await UpdateConversionAsync());
                }
            }
        }

        public Currency SelectedTargetCurrency {
            get => _selectedTargetCurrency;
            set {
                if (SetProperty(ref _selectedTargetCurrency, value)) {
                    _ = Task.Run(async () => await UpdateConversionAsync());
                }
            }
        }

        public decimal SourceAmount {
            get => _sourceAmount;
            set {
                if (SetProperty(ref _sourceAmount, value)) {
                    _ = Task.Run(async () => await UpdateConversionAsync());
                }
            }
        }

        public decimal ConvertedAmount {
            get => _convertedAmount;
            set => SetProperty(ref _convertedAmount, value);
        }

        public DateTime LatestRateDate {
            get => _latestRateDate;
            set => SetProperty(ref _latestRateDate, value);
        }

        public DateTime StartDate {
            get => _startDate;
            set {
                if (SetProperty(ref _startDate, value)) {
                    _ = Task.Run(async () => await UpdateHistoricalDataAsync());
                }
            }
        }

        public DateTime EndDate {
            get => _endDate;
            set {
                if (SetProperty(ref _endDate, value)) {
                    _ = Task.Run(async () => await UpdateHistoricalDataAsync());
                }
            }
        }

        public ObservableCollection<ExchangeRate> HistoricalRates {
            get => _historicalRates;
            set => SetProperty(ref _historicalRates, value);
        }

        public bool IsLoading {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public string ErrorMessage {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        #endregion


    }
}
