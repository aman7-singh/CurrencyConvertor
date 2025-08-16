using CurrencyConvertor.Services.Interfaces;
using System;

namespace CurrencyConvertor.Services {
    /// <summary>
    /// Simple logging service implementation (SRP - Single Responsibility)
    /// </summary>
    public class ConsoleLoggingService : ILoggingService {
        public void LogError(string message, Exception exception = null) {
            Console.WriteLine($"[ERROR] {DateTime.Now:yyyy-MM-dd HH:mm:ss}: {message}");
            if (exception != null) {
                Console.WriteLine($"Exception: {exception}");
            }
        }

        public void LogWarning(string message) {
            Console.WriteLine($"[WARNING] {DateTime.Now:yyyy-MM-dd HH:mm:ss}: {message}");
        }

        public void LogInfo(string message) {
            Console.WriteLine($"[INFO] {DateTime.Now:yyyy-MM-dd HH:mm:ss}: {message}");
        }
    }
}