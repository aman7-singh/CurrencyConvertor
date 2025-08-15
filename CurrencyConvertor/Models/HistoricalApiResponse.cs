using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CurrencyConvertor.Models {
    public class HistoricalApiResponse {
        public decimal Amount { get; set; }
        public string Base { get; set; }
        public DateTime Start_Date { get; set; }
        public DateTime End_Date { get; set; }
        public System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, decimal>> Rates { get; set; }
    }
}
