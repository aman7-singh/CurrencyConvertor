using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CurrencyConvertor.Models {
    /// <summary>
    /// Model representing a currency conversion result
    /// </summary>
    public class CurrencyConversion {
        public decimal Amount { get; set; }
        public string FromCurrency { get; set; }
        public string ToCurrency { get; set; }
        public decimal ConvertedAmount { get; set; }
        public DateTime Date { get; set; }

        public string FormattedResult => $"{ConvertedAmount:F4} {ToCurrency}";
        public string FormattedDate => $"(as of {Date:yyyy-MM-dd})";
    }
}
