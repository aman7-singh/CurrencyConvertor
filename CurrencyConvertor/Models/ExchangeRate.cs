using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CurrencyConvertor.Models {
    public class ExchangeRate {
        public DateTime Date { get; set; }
        public string FromCurrency { get; set; }
        public string ToCurrency { get; set; }
        public decimal Rate { get; set; }
        public decimal ConvertedAmount { get; set; }

        public ExchangeRate(DateTime date, string fromCurrency, string toCurrency, decimal rate, decimal originalAmount) {
            Date = date;
            FromCurrency = fromCurrency;
            ToCurrency = toCurrency;
            Rate = rate;
            ConvertedAmount = originalAmount * rate;
        }
    }
}
