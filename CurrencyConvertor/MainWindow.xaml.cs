using CurrencyConvertor.Infrastructure;
using CurrencyConvertor.ViewModels.Interfaces;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CurrencyConvertor {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// Following SOLID principles with minimal code-behind and dependency injection
    /// Enhanced with auto-refresh functionality
    /// </summary>
    public partial class MainWindow : Window {
        private readonly IMainViewModel _viewModel;

        public MainWindow() {
            InitializeComponent();

            // Get the service container and log the initialization
            var serviceContainer = ServiceContainer.Instance;
            var loggingService = serviceContainer.GetLoggingService();

            loggingService.LogInfo("MainWindow constructor started");

            // Use dependency injection container to create ViewModel (DIP - Dependency Inversion)
            _viewModel = serviceContainer.CreateMainViewModel();

            loggingService.LogInfo($"ViewModel created: {_viewModel.GetType().Name}");

            // Set DataContext and log
            DataContext = _viewModel;
            loggingService.LogInfo("DataContext set to ViewModel");

            // Log initial property values
            loggingService.LogInfo($"Initial ConversionResult: '{_viewModel.ConversionResult}'");
            loggingService.LogInfo($"Initial Amount: '{_viewModel.Amount}'");

            // Initialize the application
            _viewModel.Initialize();

            // Start auto-refresh with 30-minute interval
            _viewModel.StartAutoRefresh(30);
            loggingService.LogInfo("Auto-refresh started with 30-minute interval");

            loggingService.LogInfo("MainWindow initialization completed");

            // Log values after initialization
            loggingService.LogInfo($"Post-init ConversionResult: '{_viewModel.ConversionResult}'");
        }

        protected override void OnClosed(System.EventArgs e) {
            try {
                var loggingService = ServiceContainer.Instance.GetLoggingService();
                loggingService.LogInfo("MainWindow closing");

                // Stop auto-refresh
                _viewModel?.StopAutoRefresh();
                loggingService.LogInfo("Auto-refresh stopped");

                // Clean up ViewModel resources
                _viewModel?.Dispose();

                // Clean up service container
                ServiceContainer.Instance.Dispose();

                loggingService.LogInfo("MainWindow cleanup completed");
            } catch (System.Exception ex) {
                // If logging fails, we can't do much but at least try to continue cleanup
                System.Console.WriteLine($"Error during MainWindow cleanup: {ex.Message}");
            }

            base.OnClosed(e);
        }
    }
}
