using System;
using System.Threading.Tasks;

namespace CurrencyConvertor.Services.Interfaces {
    /// <summary>
    /// Interface for application initialization (SRP - Single Responsibility)
    /// </summary>
    public interface IInitializationService {
        Task<bool> InitializeApplicationAsync();
        event EventHandler<string> StatusChanged;
        event EventHandler<bool> LoadingStateChanged;
    }
}