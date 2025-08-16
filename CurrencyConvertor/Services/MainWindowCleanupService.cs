using CurrencyConvertor.Infrastructure;
using CurrencyConvertor.Services.Interfaces;
using CurrencyConvertor.ViewModels.Interfaces;
using System;

namespace CurrencyConvertor.Services {
    /// <summary>
    /// Handles MainWindow cleanup logic following SRP (Single Responsibility Principle)
    /// Separates cleanup concerns from the UI layer
    /// </summary>
    public class MainWindowCleanupService {
        private readonly ServiceContainer _serviceContainer;
        private readonly ILoggingService _loggingService;

        public MainWindowCleanupService(ServiceContainer serviceContainer) {
            _serviceContainer = serviceContainer ?? throw new ArgumentNullException(nameof(serviceContainer));
            _loggingService = serviceContainer.GetLoggingService();
        }

        /// <summary>
        /// Performs comprehensive cleanup of the MainWindow and its resources
        /// </summary>
        /// <param name="viewModel">The main view model to cleanup</param>
        public void PerformCleanup(IMainViewModel viewModel) {
            try {
                _loggingService.LogInfo("MainWindow cleanup started");

                // Log final statistics before cleanup
                LogFinalStatistics();

                // Stop auto-refresh
                StopAutoRefresh(viewModel);

                // Clean up ViewModel resources
                CleanupViewModel(viewModel);

                // Clean up service container
                CleanupServiceContainer();

                _loggingService.LogInfo("MainWindow cleanup completed successfully");
            } catch (Exception ex) {
                // If logging fails, we can't do much but at least try to continue cleanup
                HandleCleanupError(ex);
            }
        }

        /// <summary>
        /// Logs final cache and auto-refresh statistics
        /// </summary>
        private void LogFinalStatistics() {
            try {
                var cacheStats = _serviceContainer.GetCacheStatistics();
                _loggingService.LogInfo($"Final cache statistics: {cacheStats}");

                var autoRefreshStats = _serviceContainer.GetAutoRefreshStatistics();
                _loggingService.LogInfo($"Final auto-refresh statistics: {autoRefreshStats}");
            } catch (Exception ex) {
                _loggingService.LogWarning($"Failed to log final statistics: {ex.Message}");
            }
        }

        /// <summary>
        /// Stops auto-refresh functionality
        /// </summary>
        private void StopAutoRefresh(IMainViewModel viewModel) {
            try {
                viewModel?.StopAutoRefresh();
                _loggingService.LogInfo("Auto-refresh stopped");
            } catch (Exception ex) {
                _loggingService.LogWarning($"Failed to stop auto-refresh: {ex.Message}");
            }
        }

        /// <summary>
        /// Cleans up the main view model
        /// </summary>
        private void CleanupViewModel(IMainViewModel viewModel) {
            try {
                viewModel?.Dispose();
                _loggingService.LogInfo("ViewModel disposed");
            } catch (Exception ex) {
                _loggingService.LogWarning($"Failed to dispose ViewModel: {ex.Message}");
            }
        }

        /// <summary>
        /// Cleans up the service container
        /// </summary>
        private void CleanupServiceContainer() {
            try {
                _serviceContainer.Dispose();
                _loggingService.LogInfo("ServiceContainer disposed");
            } catch (Exception ex) {
                _loggingService.LogWarning($"Failed to dispose ServiceContainer: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles cleanup errors when logging service might not be available
        /// </summary>
        private void HandleCleanupError(Exception ex) {
            try {
                _loggingService?.LogError("Error during MainWindow cleanup", ex);
            } catch {
                // Last resort: console output if logging completely fails
                Console.WriteLine($"Error during MainWindow cleanup: {ex.Message}");
            }
        }
    }
}