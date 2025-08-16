using System;

namespace CurrencyConvertor.Services.Models {
    /// <summary>
    /// Statistics about cache performance
    /// </summary>
    public class CacheStatistics {
        public int TotalItems { get; set; }
        public int HitCount { get; set; }
        public int MissCount { get; set; }
        public double HitRatio => HitCount + MissCount > 0 ? (double)HitCount / (HitCount + MissCount) : 0;
        public DateTime LastCleanup { get; set; }
        public int ItemsEvicted { get; set; }
        public long MemoryUsageBytes { get; set; }

        public override string ToString() {
            return $"Size: {TotalItems}, Hits: {HitCount}, Misses: {MissCount}, Hit Ratio: {HitRatio:P2}";
        }
    }
}