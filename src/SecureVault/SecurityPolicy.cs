using System;
using System.Text.RegularExpressions;

namespace SecureVault
{
    public static class SecurityPolicy
    {
        // These constants define the minimum requirements for a valid master password
        // They are defined here as constants so the policy can be tightened
        // in one place without having to hunt through the validation logic below
        public const int MinLength = 10;          // Must be at least 10 characters long
        public const int MinUpper = 1;         // Must have at least 1 uppercase letter
        public const int MinLower = 1;         // Must have at least 1 lowercase letter
        public const int MinDigits = 1;       // Must have at least 1 number
        public const int MinSymbols = 1;          // Must have at least 1 symbol like ! @ # $

        // Checks whether a password meets all the complexity requirements
        // Returns true if the password is acceptable
        // Returns false and sets the reason string if it is not
        // The reason string tells the user exactly which rule they broke
        public static bool ValidatePasswordComplexity(string password,
            out string reason)
        {
            // Set reason to null to start with
            // It will only be set to a message if a rule is broken
            reason = null;

            // Reject completely empty passwords before checking anything else
            if (string.IsNullOrEmpty(password))
            {
                reason = "Password cannot be empty.";
                return false;
            }

            // Check the minimum length first since this is the most common failure
            if (password.Length < MinLength)
            {
                reason = $"Password must be at least {MinLength} characters.";
                return false;
            }

            // Count how many of each character type the password contains
            // We do this in a single pass through the password for efficiency
            int uppers = 0, lowers = 0, digits = 0, symbols = 0;
            foreach (char c in password)
            {
                if (char.IsUpper(c)) uppers++;       // A B C D etc
                else if (char.IsLower(c)) lowers++; // a b c d etc
                else if (char.IsDigit(c)) digits++; // 0 1 2 3 etc
                else symbols++;                // ! @ # $ % etc
            }

            // Check each character type requirement in turn
            // Each one returns a specific message so the user knows
            // exactly what they need to add to their password
            if (uppers < MinUpper)
            {
                reason = $"Password must contain at least {MinUpper} uppercase letter.";
                return false;
            }

            if (lowers < MinLower)
            {
                reason = $"Password must contain at least {MinLower} lowercase letter.";
                return false;
            }

            if (digits < MinDigits)
            {
                reason = $"Password must contain at least {MinDigits} digit.";
                return false;
            }

            if (symbols < MinSymbols)
            {
                reason = $"Password must contain at least {MinSymbols} symbol (e.g. !@#$).";
                return false;
            }

            // Convert the password to lowercase once so we can check
            // for common patterns without worrying about capitalisation
            string lower = password.ToLowerInvariant();

            // Reject passwords that contain very well known weak patterns
            // Even if a password like "Password1!" passes all the rules above
            // it is still far too easy to guess because attackers specifically
            // try common words and patterns first
            if (lower.Contains("password") ||
                lower.Contains("12345") ||
                lower.Contains("qwerty"))
            {
                reason = "Password is too common. Choose something less guessable.";
                return false;
            }

            // If we get here the password passed all the checks
            return true;
        }
    }
}
