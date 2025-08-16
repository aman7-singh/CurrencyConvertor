using CurrencyConvertor.Infrastructure;
using CurrencyConvertor.Services.Interfaces;
using CurrencyConvertor.ViewModels.Interfaces;
using System;
using System.Threading.Tasks;

namespace CurrencyConvertor.Services {
    /// <summary>
    /// Handles MainWindow initialization logic following SRP (Single Responsibility Principle)
    /// Separates initialization concerns from the UI layer
    /// </summary>
    public class MainWindowInitializer {
        private readonly ServiceContainer _serviceContainer;
        private readonly ILoggingService _loggingService;

        public MainWindowInitializer(ServiceContainer serviceContainer) {
            _serviceContainer = serviceContainer ?? throw new ArgumentNullException(nameof(serviceContainer));
            _loggingService = serviceContainer.GetLoggingService();
        }

        /// <summary>
        /// Initializes the MainWindow ViewModel and related services
        /// </summary>
        /// <returns>The initialized MainViewModel</returns>
        public IMainViewModel InitializeViewModel() {
            try {
                _loggingService.LogInfo("MainWindow initialization started");

                // Create ViewModel using dependency injection
                var viewModel = _serviceContainer.CreateMainViewModel();
                _loggingService.LogInfo($"ViewModel created: {viewModel.GetType().Name}");

                // Log initial property values
                LogInitialViewModelState(viewModel);

                // Log configuration values
                LogConfigurationValues();

                return viewModel;
            } catch (Exception ex) {
                _loggingService.LogError("Failed to initialize ViewModel", ex);
                throw new InvalidOperationException("MainWindow initialization failed", ex);
            }
        }

        /// <summary>
        /// Starts the application initialization process
        /// </summary>
        /// <param name="viewModel">The main view model to initialize</param>
        public async Task StartApplicationAsync(IMainViewModel viewModel) {
            try {
                if (viewModel == null) {
                    throw new ArgumentNullException(nameof(viewModel));
                }

                _loggingService.LogInfo("Starting application initialization");

                // Initialize the application
                viewModel.Initialize();

                // Start auto-refresh with 30-minute interval
                viewModel.StartAutoRefresh(30);
                _loggingService.LogInfo("Auto-refresh started with 30-minute interval");

                _loggingService.LogInfo("MainWindow initialization completed");

                // Log values after initialization
                LogPostInitializationState(viewModel);
            } catch (Exception ex) {
                _loggingService.LogError("Application startup failed", ex);
                throw;
            }
        }

        /// <summary>
        /// Logs initial ViewModel state
        /// </summary>
        private void LogInitialViewModelState(IMainViewModel viewModel) {
            try {
                _loggingService.LogInfo($"Initial ConversionResult: '{viewModel.ConversionResult}'");
                _loggingService.LogInfo($"Initial Amount: '{viewModel.Amount}'");
            } catch (Exception ex) {
                _loggingService.LogWarning($"Failed to log initial ViewModel state: {ex.Message}");
            }
        }

        /// <summary>
        /// Logs post-initialization ViewModel state
        /// </summary>
        private void LogPostInitializationState(IMainViewModel viewModel) {
            try {
                _loggingService.LogInfo($"Post-init ConversionResult: '{viewModel.ConversionResult}'");
            } catch (Exception ex) {
                _loggingService.LogWarning($"Failed to log post-initialization ViewModel state: {ex.Message}");
            }
        }

        /// <summary>
        /// Logs current configuration values
        /// </summary>
        private void LogConfigurationValues() {
            try {
                var cacheConfig = _serviceContainer.GetCacheConfiguration();
                _loggingService.LogInfo($"Active configuration - Cache: Strategy={cacheConfig.EvictionStrategy}, MaxAge={cacheConfig.MaxAgeMinutes}min, MaxElements={cacheConfig.MaxElements}, Enabled={cacheConfig.IsEnabled}");
            } catch (Exception ex) {
                _loggingService.LogWarning($"Failed to log configuration values: {ex.Message}");
            }
        }
    }
}