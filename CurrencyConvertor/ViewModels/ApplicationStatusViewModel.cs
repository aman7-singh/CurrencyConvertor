using CurrencyConvertor.Services.Interfaces;
using CurrencyConvertor.ViewModels.Interfaces;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CurrencyConvertor.ViewModels {
    /// <summary>
    /// ViewModel responsible only for application status (SRP - Single Responsibility)
    /// </summary>
    public class ApplicationStatusViewModel : ViewModelBase, IApplicationStatusViewModel {
        private readonly ILoggingService _loggingService;
        private string _statusMessage;
        private bool _isLoading;

        public ApplicationStatusViewModel(ILoggingService loggingService) {
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            StatusMessage = "Ready";
        }

        #region Properties

        public string StatusMessage {
            get => _statusMessage;
            private set {
                if (SetProperty(ref _statusMessage, value)) {
                    _loggingService.LogInfo($"Status changed: {value}");
                }
            }
        }

        public bool IsLoading {
            get => _isLoading;
            private set => SetProperty(ref _isLoading, value);
        }

        #endregion

        #region Public Methods

        public void SetStatus(string message) {
            StatusMessage = message;
        }

        public void SetLoadingState(bool isLoading) {
            IsLoading = isLoading;
        }

        public void SetStatus(string message, bool isLoading) {
            StatusMessage = message;
            IsLoading = isLoading;
        }

        #endregion
    }
}