using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CurrencyConvertor.Models {
    internal class Currency : INotifyPropertyChanged {
        private string _code;
        private string _name;

        public string Code {
            get => _code;
            set {
                _code = value;
                OnPropertyChanged(nameof(Code));
            }
        }

        public string Name {
            get => _name;
            set {
                _name = value;
                OnPropertyChanged(nameof(Name));
            }
        }

        public Currency(string code, string name) {
            Code = code;
            Name = name;
        }

        public override string ToString() {
            return $"{Code} - {Name}";
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
