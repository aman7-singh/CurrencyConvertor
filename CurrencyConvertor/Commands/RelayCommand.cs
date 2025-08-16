﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace CurrencyConvertor.Commands {

    /// <summary>
    /// RelayCommand implementation for MVVM command binding
    /// </summary>
    public class RelayCommand : ICommand {
        private readonly Action<object> _execute;
        private readonly Predicate<object> _canExecute;

        public RelayCommand(Action<object> execute, Predicate<object> canExecute = null) {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public RelayCommand(Action execute, Func<bool> canExecute = null) {
            if (execute == null)
                throw new ArgumentNullException(nameof(execute));

            _execute = _ => execute();
            _canExecute = canExecute != null ? (Predicate<object>)(_ => canExecute()) : null;
        }

        public event EventHandler CanExecuteChanged {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter) {
            return _canExecute?.Invoke(parameter) ?? true;
        }

        public void Execute(object parameter) {
            _execute(parameter);
        }
    }
}
