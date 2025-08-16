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
    /// Enhanced with auto-refresh functionality and App.config integration
    /// </summary>
    public partial class MainWindow : Window {
        private readonly IMainViewModel _viewModel;

        public MainWindow() {
            InitializeComponent();

            // Get the service container and log the initialization
            var serviceContainer = ServiceContainer.Instance;
            var loggingService = serviceContainer.GetLoggingService();

            loggingService.LogInfo("MainWindow constructor started - App.config Integration Complete");

            // Use dependency injection container to create ViewModel (DIP - Dependency Inversion)
            _viewModel = serviceContainer.CreateMainViewModel();

            loggingService.LogInfo($"ViewModel created: {_viewModel.GetType().Name}");

            // Set DataContext and log
            DataContext = _viewModel;
            loggingService.LogInfo("DataContext set to ViewModel");

            // Log initial property values
            loggingService.LogInfo($"Initial ConversionResult: '{_viewModel.ConversionResult}'");
            loggingService.LogInfo($"Initial Amount: '{_viewModel.Amount}'");

            // Log configuration values being used
            var cacheConfig = serviceContainer.GetCacheConfiguration();
            loggingService.LogInfo($"Active configuration - Cache: Strategy={cacheConfig.EvictionStrategy}, MaxAge={cacheConfig.MaxAgeMinutes}min, MaxElements={cacheConfig.MaxElements}, Enabled={cacheConfig.IsEnabled}");

            // Initialize the application
            _viewModel.Initialize();

            // Start auto-refresh with 30-minute interval
            _viewModel.StartAutoRefresh(30);
            loggingService.LogInfo("Auto-refresh started with 30-minute interval");

            // Test configuration asynchronously (optional - shows App.config values)
            _ = Task.Run(async () => {
                await Task.Delay(2000); // Wait for initialization to complete
                await TestConfigurationIntegration();
                await serviceContainer.TestServicesAsync();
            });

            loggingService.LogInfo("MainWindow initialization completed with App.config integration");

            // Log values after initialization
            loggingService.LogInfo($"Post-init ConversionResult: '{_viewModel.ConversionResult}'");
        }

        /// <summary>
        /// Tests App.config integration and logs results
        /// </summary>
        private async Task TestConfigurationIntegration() {
            try {
                var serviceContainer = ServiceContainer.Instance;
                var loggingService = serviceContainer.GetLoggingService();
                
                loggingService.LogInfo("=== Testing App.config Integration ===");
                
                // Test DateValidationService configuration
                var dateValidationService = new CurrencyConvertor.Services.DateValidationService(loggingService);
                loggingService.LogInfo($"Historical Data Config - Max: {dateValidationService.MaxAllowedDays} days, Min: {dateValidationService.MinRequiredDays} days");
                
                // Test cache configuration
                var cacheConfig = serviceContainer.GetCacheConfiguration();
                loggingService.LogInfo($"Cache Config - Strategy: {cacheConfig.EvictionStrategy}, MaxAge: {cacheConfig.MaxAgeMinutes}min, MaxElements: {cacheConfig.MaxElements}");
                
                // Test HistoricalDataViewModel default range
                var historicalVm = serviceContainer.CreateHistoricalDataViewModel();
                if (historicalVm.StartDate.HasValue && historicalVm.EndDate.HasValue) {
                    var defaultDays = (historicalVm.EndDate.Value - historicalVm.StartDate.Value).Days;
                    loggingService.LogInfo($"Historical ViewModel Default Range: {defaultDays} days (configured in App.config)");
                }
                
                loggingService.LogInfo("=== App.config Integration Test Complete ===");
            } catch (Exception ex) {
                var serviceContainer = ServiceContainer.Instance;
                var loggingService = serviceContainer.GetLoggingService();
                loggingService.LogError("App.config integration test failed", ex);
            }
        }

        protected override void OnClosed(System.EventArgs e) {
            try {
                var serviceContainer = ServiceContainer.Instance;
                var loggingService = serviceContainer.GetLoggingService();
                loggingService.LogInfo("MainWindow closing");

                // Log final cache statistics
                var cacheStats = serviceContainer.GetCacheStatistics();
                loggingService.LogInfo($"Final cache statistics: {cacheStats}");

                // Stop auto-refresh
                _viewModel?.StopAutoRefresh();
                loggingService.LogInfo("Auto-refresh stopped");

                // Clean up ViewModel resources
                _viewModel?.Dispose();

                // Clean up service container (includes cache disposal)
                serviceContainer.Dispose();

                loggingService.LogInfo("MainWindow cleanup completed");
            } catch (System.Exception ex) {
                // If logging fails, we can't do much but at least try to continue cleanup
                System.Console.WriteLine($"Error during MainWindow cleanup: {ex.Message}");
            }

            base.OnClosed(e);
        }
    }
}
