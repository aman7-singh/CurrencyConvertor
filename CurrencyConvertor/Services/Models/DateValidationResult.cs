namespace CurrencyConvertor.Services.Models {
    /// <summary>
    /// Represents the result of date validation
    /// </summary>
    public class DateValidationResult {
        public bool IsValid { get; }
        public string Message { get; }
        public bool HasWarning { get; }

        public DateValidationResult(bool isValid, string message, bool hasWarning = false) {
            IsValid = isValid;
            Message = message ?? string.Empty;
            HasWarning = hasWarning;
        }

        public override string ToString() {
            var status = IsValid ? "Valid" : "Invalid";
            var warning = HasWarning ? " (Warning)" : "";
            return $"{status}{warning}: {Message}";
        }
    }
}