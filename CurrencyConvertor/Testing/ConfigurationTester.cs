using CurrencyConvertor.Infrastructure;
using CurrencyConvertor.Services.Interfaces;
using System;
using System.Threading.Tasks;

namespace CurrencyConvertor.Testing {
    /// <summary>
    /// Simple test utility to verify App.config integration
    /// </summary>
    public static class ConfigurationTester {
        /// <summary>
        /// Tests that all configuration values are being read from App.config
        /// </summary>
        public static async Task TestConfigurationAsync() {
            try {
                Console.WriteLine("=== App.config Integration Test ===");
                Console.WriteLine();

                var serviceContainer = ServiceContainer.Instance;
                var loggingService = serviceContainer.GetLoggingService();
                var validationService = serviceContainer.GetValidationService();
                var cacheConfig = serviceContainer.GetCacheConfiguration();

                // Test historical data configuration
                Console.WriteLine("Historical Data Configuration:");
                if (validationService is CurrencyConvertor.Services.ValidationService valService) {
                    // Access the DateValidationService through reflection or create a new instance to test
                    var dateValidationService = new CurrencyConvertor.Services.DateValidationService(loggingService);
                    Console.WriteLine($"  Max Allowed Days: {dateValidationService.MaxAllowedDays}");
                    Console.WriteLine($"  Min Required Days: {dateValidationService.MinRequiredDays}");
                }

                // Test cache configuration
                Console.WriteLine();
                Console.WriteLine("Cache Configuration:");
                Console.WriteLine($"  Eviction Strategy: {cacheConfig.EvictionStrategy}");
                Console.WriteLine($"  Max Age Minutes: {cacheConfig.MaxAgeMinutes}");
                Console.WriteLine($"  Max Elements: {cacheConfig.MaxElements}");
                Console.WriteLine($"  Cleanup Interval: {cacheConfig.CleanupIntervalMinutes} minutes");
                Console.WriteLine($"  Cache Enabled: {cacheConfig.IsEnabled}");

                // Test HistoricalDataViewModel default
                Console.WriteLine();
                Console.WriteLine("Historical Data ViewModel:");
                var historicalViewModel = serviceContainer.CreateHistoricalDataViewModel();
                var defaultStartDate = historicalViewModel.StartDate;
                var defaultEndDate = historicalViewModel.EndDate;
                if (defaultStartDate.HasValue && defaultEndDate.HasValue) {
                    var daysDifference = (defaultEndDate.Value - defaultStartDate.Value).Days;
                    Console.WriteLine($"  Default Date Range: {daysDifference} days (from {defaultStartDate.Value:yyyy-MM-dd} to {defaultEndDate.Value:yyyy-MM-dd})");
                }

                Console.WriteLine();
                Console.WriteLine("? Configuration test completed successfully!");
                Console.WriteLine("Check the console logs above for 'Loaded ... from App.config' messages to confirm App.config integration is working.");

            } catch (Exception ex) {
                Console.WriteLine($"? Configuration test failed: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }
    }
}