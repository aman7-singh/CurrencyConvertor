using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CurrencyConvertor.Models {
    /// <summary>
    /// Model representing a historical exchange rate
    /// </summary>
    public class HistoricalRate {
        public DateTime Date { get; set; }
        public string FromCurrency { get; set; }
        public string ToCurrency { get; set; }
        public decimal Rate { get; set; }

        public string FormattedDate => Date.ToString("yyyy-MM-dd");
        public string FormattedRate => Rate.ToString("F4");
    }
}
