using CurrencyConvertor.Models;
using CurrencyConvertor.Services;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace CurrencyConvertor.ViewModels.Interfaces {
    /// <summary>
    /// Interface for currency conversion ViewModel (ISP - Interface Segregation)
    /// Enhanced with auto-refresh capabilities
    /// </summary>
    public interface ICurrencyConversionViewModel {
        ObservableCollection<string> Currencies { get; }
        string SelectedFromCurrency { get; set; }
        string SelectedToCurrency { get; set; }
        string Amount { get; set; }
        string ConversionResult { get; }
        string ConversionDate { get; }

        // Auto-refresh properties
        bool AutoRefreshEnabled { get; }
        string AutoRefreshStatus { get; }

        ICommand RefreshCurrenciesCommand { get; }
        ICommand ToggleAutoRefreshCommand { get; }

        // Auto-refresh methods
        void StartAutoRefresh();
        void StartAutoRefresh(int intervalMinutes);
        void StopAutoRefresh();
        RefreshStatistics GetAutoRefreshStats();
    }

    /// <summary>
    /// Interface for historical data ViewModel (ISP - Interface Segregation)
    /// </summary>
    public interface IHistoricalDataViewModel {
        DateTime? StartDate { get; set; }
        DateTime? EndDate { get; set; }
        ObservableCollection<HistoricalRate> HistoricalRates { get; }

        ICommand LoadHistoricalDataCommand { get; }
    }

    /// <summary>
    /// Interface for application status ViewModel (ISP - Interface Segregation)
    /// </summary>
    public interface IApplicationStatusViewModel {
        string StatusMessage { get; }
        bool IsLoading { get; }
    }

    /// <summary>
    /// Combined interface for the main ViewModel (ISP compliant composition)
    /// </summary>
    public interface IMainViewModel : ICurrencyConversionViewModel, IHistoricalDataViewModel, IApplicationStatusViewModel, IDisposable {
        void Initialize();
    }
}