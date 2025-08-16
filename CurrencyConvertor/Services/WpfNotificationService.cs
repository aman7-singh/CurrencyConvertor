using CurrencyConvertor.Services.Interfaces;
using System.Windows;

namespace CurrencyConvertor.Services {
    /// <summary>
    /// WPF-specific notification service implementation (SRP - Single Responsibility)
    /// </summary>
    public class WpfNotificationService : INotificationService {
        public void ShowError(string message, string title = "Error") {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public void ShowWarning(string message, string title = "Warning") {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        public void ShowInfo(string message, string title = "Information") {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}