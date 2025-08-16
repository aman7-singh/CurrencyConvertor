using System;

namespace CurrencyConvertor.Services {
    /// <summary>
    /// Statistics about the auto-refresh service
    /// </summary>
    public class RefreshStatistics {
        public bool IsEnabled { get; set; }
        public bool IsRefreshing { get; set; }
        public TimeSpan RefreshInterval { get; set; }
        public DateTime LastRefreshTime { get; set; }
        public TimeSpan TimeSinceLastRefresh { get; set; }
        public DateTime? NextRefreshTime { get; set; }

        public override string ToString() {
            if (!IsEnabled)
                return "Auto-refresh disabled";

            var status = IsRefreshing ? "Refreshing..." : "Ready";
            var lastRefresh = LastRefreshTime == DateTime.MinValue
                ? "Never"
                : $"{TimeSinceLastRefresh.TotalMinutes:F0} min ago";

            return $"{status} | Interval: {RefreshInterval.TotalMinutes:F0} min | Last: {lastRefresh}";
        }
    }
}