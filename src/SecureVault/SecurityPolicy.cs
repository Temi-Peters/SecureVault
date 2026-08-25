using System;
using System.Text.RegularExpressions;

namespace SecureVault
{
    public static class SecurityPolicy
    {
        // Parameters you can tweak
        public const int MinLength = 10;
        public const int MinUpper = 1;
        public const int MinLower = 1;
        public const int MinDigits = 1;
        public const int MinSymbols = 1;

        public static bool ValidatePasswordComplexity(string password, out string reason)
        {
            reason = null;
            if (string.IsNullOrEmpty(password))
            {
                reason = "Password cannot be empty.";
                return false;
            }

            if (password.Length < MinLength)
            {
                reason = $"Password must be at least {MinLength} characters.";
                return false;
            }

            int uppers = 0, lowers = 0, digits = 0, symbols = 0;
            foreach (char c in password)
            {
                if (char.IsUpper(c)) uppers++;
                else if (char.IsLower(c)) lowers++;
                else if (char.IsDigit(c)) digits++;
                else symbols++;
            }

            if (uppers < MinUpper) { reason = $"Password must contain at least {MinUpper} uppercase letter."; return false; }
            if (lowers < MinLower) { reason = $"Password must contain at least {MinLower} lowercase letter."; return false; }
            if (digits < MinDigits) { reason = $"Password must contain at least {MinDigits} digit."; return false; }
            if (symbols < MinSymbols) { reason = $"Password must contain at least {MinSymbols} symbol (e.g. !@#$)."; return false; }

            // Optionally reject very common patterns (simple example)
            string lower = password.ToLowerInvariant();
            if (lower.Contains("password") || lower.Contains("12345") || lower.Contains("qwerty"))
            {
                reason = "Password is too common. Choose something less guessable.";
                return false;
            }

            return true;
        }
    }
}
