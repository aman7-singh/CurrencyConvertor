using System;

namespace CurrencyConvertor.Services.Interfaces {
    /// <summary>
    /// Interface for logging services (ISP - Interface Segregation)
    /// </summary>
    public interface ILoggingService {
        void LogError(string message, Exception exception = null);
        void LogWarning(string message);
        void LogInfo(string message);
    }
}