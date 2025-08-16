using CurrencyConvertor.Services.Interfaces;
using System;
using System.Windows;

namespace CurrencyConvertor.Services {
    /// <summary>
    /// Centralized error handling strategy for the MainWindow
    /// Follows SRP by handling all error scenarios in one place
    /// </summary>
    public class MainWindowErrorHandler {
        private readonly ILoggingService _loggingService;
        private readonly INotificationService _notificationService;

        public MainWindowErrorHandler(ILoggingService loggingService, INotificationService notificationService) {
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        }

        /// <summary>
        /// Handles initialization errors with appropriate user feedback
        /// </summary>
        /// <param name="ex">The exception that occurred</param>
        /// <param name="context">Additional context about where the error occurred</param>
        public void HandleInitializationError(Exception ex, string context = "initialization") {
            var message = $"Failed during {context}: {ex.Message}";
            
            _loggingService.LogError(message, ex);
            
            _notificationService.ShowError(
                $"The application failed to initialize properly.\n\nError: {ex.Message}\n\nPlease restart the application or contact support if the problem persists.",
                "Initialization Error");
        }

        /// <summary>
        /// Handles configuration errors with graceful degradation
        /// </summary>
        /// <param name="ex">The exception that occurred</param>
        /// <param name="context">Additional context about the configuration</param>
        public void HandleConfigurationError(Exception ex, string context = "configuration") {
            var message = $"Configuration error in {context}: {ex.Message}";
            
            _loggingService.LogWarning(message);
            
            // For configuration errors, we typically don't show user notifications
            // as the application can continue with defaults
        }

        /// <summary>
        /// Handles cleanup errors with minimal disruption
        /// </summary>
        /// <param name="ex">The exception that occurred</param>
        /// <param name="context">Additional context about the cleanup operation</param>
        public void HandleCleanupError(Exception ex, string context = "cleanup") {
            var message = $"Error during {context}: {ex.Message}";
            
            try {
                _loggingService.LogError(message, ex);
            } catch {
                // If logging fails during cleanup, fall back to console
                Console.WriteLine(message);
                Console.WriteLine(ex.ToString());
            }
            
            // Don't show user notifications during cleanup as the window is closing
        }

        /// <summary>
        /// Handles unexpected runtime errors
        /// </summary>
        /// <param name="ex">The exception that occurred</param>
        /// <param name="context">Additional context about the operation</param>
        /// <param name="showToUser">Whether to show the error to the user</param>
        public void HandleRuntimeError(Exception ex, string context = "runtime operation", bool showToUser = true) {
            var message = $"Runtime error during {context}: {ex.Message}";
            
            _loggingService.LogError(message, ex);
            
            if (showToUser) {
                _notificationService.ShowError(
                    $"An unexpected error occurred during {context}.\n\nError: {ex.Message}\n\nThe application will continue, but some features may not work correctly.",
                    "Runtime Error");
            }
        }

        /// <summary>
        /// Handles critical errors that require application shutdown
        /// </summary>
        /// <param name="ex">The exception that occurred</param>
        /// <param name="context">Additional context about the critical operation</param>
        public void HandleCriticalError(Exception ex, string context = "critical operation") {
            var message = $"Critical error during {context}: {ex.Message}";
            
            _loggingService.LogError(message, ex);
            
            var result = MessageBox.Show(
                $"A critical error occurred that requires the application to close.\n\nError: {ex.Message}\n\nWould you like to view detailed error information?",
                "Critical Error",
                MessageBoxButton.YesNo,
                MessageBoxImage.Error);

            if (result == MessageBoxResult.Yes) {
                MessageBox.Show(
                    $"Detailed Error Information:\n\n{ex.ToString()}",
                    "Error Details",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }

            Application.Current.Shutdown(1);
        }
    }
}