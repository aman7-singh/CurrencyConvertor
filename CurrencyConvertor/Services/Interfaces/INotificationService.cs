using System;

namespace CurrencyConvertor.Services.Interfaces {
    /// <summary>
    /// Interface for user notification services (ISP - Interface Segregation)
    /// </summary>
    public interface INotificationService {
        void ShowError(string message, string title = "Error");
        void ShowWarning(string message, string title = "Warning");
        void ShowInfo(string message, string title = "Information");
    }
}