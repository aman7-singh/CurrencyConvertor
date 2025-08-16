using CurrencyConvertor.Infrastructure;
using CurrencyConvertor.Services;
using CurrencyConvertor.ViewModels.Interfaces;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace CurrencyConvertor {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// Refactored to follow SOLID principles with minimal code-behind and dependency injection
    /// Enhanced with proper separation of concerns and error handling
    /// </summary>
    public partial class MainWindow : Window {
        private readonly IMainViewModel _viewModel;
        private readonly MainWindowInitializer _initializer;
        private readonly MainWindowCleanupService _cleanupService;
        private readonly MainWindowErrorHandler _errorHandler;
        private readonly ServiceContainer _serviceContainer;

        public MainWindow() {
            InitializeComponent();
            
            try {
                // Initialize core services
                _serviceContainer = ServiceContainer.Instance;
                
                // Create specialized services for MainWindow concerns
                _initializer = new MainWindowInitializer(_serviceContainer);
                _cleanupService = new MainWindowCleanupService(_serviceContainer);
                _errorHandler = new MainWindowErrorHandler(
                    _serviceContainer.GetLoggingService(),
                    _serviceContainer.GetNotificationService());

                // Initialize ViewModel through the initializer service
                _viewModel = _initializer.InitializeViewModel();
                
                // Set DataContext
                DataContext = _viewModel;
                
                // Start application asynchronously
                _ = InitializeApplicationAsync();
            } catch (Exception ex) {
                // Handle critical initialization errors
                HandleCriticalInitializationError(ex);
            }
        }

        /// <summary>
        /// Initializes the application asynchronously
        /// Separated from constructor to improve startup responsiveness
        /// </summary>
        private async Task InitializeApplicationAsync() {
            try {
                await _initializer.StartApplicationAsync(_viewModel);
            } catch (Exception ex) {
                _errorHandler.HandleInitializationError(ex, "application startup");
            }
        }

        /// <summary>
        /// Handles critical errors during MainWindow construction
        /// </summary>
        private void HandleCriticalInitializationError(Exception ex) {
            try {
                // Try to create minimal error handling if services aren't available
                var loggingService = ServiceContainer.Instance?.GetLoggingService();
                var notificationService = ServiceContainer.Instance?.GetNotificationService();
                
                if (loggingService != null && notificationService != null) {
                    var errorHandler = new MainWindowErrorHandler(loggingService, notificationService);
                    errorHandler.HandleCriticalError(ex, "MainWindow initialization");
                } else {
                    // Fallback to basic error handling
                    MessageBox.Show(
                        $"Critical error during application startup:\n\n{ex.Message}\n\nThe application will now close.",
                        "Startup Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    
                    Application.Current.Shutdown(1);
                }
            } catch {
                // Last resort - basic message box
                MessageBox.Show(
                    "A critical error occurred during application startup. The application will now close.",
                    "Critical Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                
                Application.Current.Shutdown(1);
            }
        }

        /// <summary>
        /// Handles window closing with proper cleanup
        /// Refactored to use dedicated cleanup service
        /// </summary>
        protected override void OnClosed(EventArgs e) {
            try {
                _cleanupService.PerformCleanup(_viewModel);
            } catch (Exception ex) {
                _errorHandler.HandleCleanupError(ex, "MainWindow closure");
            }

            base.OnClosed(e);
        }
    }
}
