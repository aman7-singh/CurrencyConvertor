using System;
using System.Collections.Generic;

namespace CurrencyConvertor.Services {
    /// <summary>
    /// Event arguments for currency refresh events
    /// </summary>
    public class CurrencyRefreshEventArgs : EventArgs {
        public bool Success { get; set; }
        public List<string> Currencies { get; set; } = new List<string>();
        public DateTime RefreshTime { get; set; }
        public bool IsManualRefresh { get; set; }
        public string Message { get; set; } = string.Empty;
        public Exception Error { get; set; }
    }
}