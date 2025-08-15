using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CurrencyConvertor.Models {
    /// <summary>
    /// Model for API response structures
    /// </summary>
    public class ConversionApiResponse {
        public decimal Amount { get; set; }
        public string Base { get; set; }
        public string Date { get; set; }
        public System.Collections.Generic.Dictionary<string, decimal> Rates { get; set; }
    }
}
