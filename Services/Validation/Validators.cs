using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace StudyLink.Services.Validation
{
    /// <summary>
    /// Server-side rules ported from TutorConnect UserValidator. Client-side validation is a
    /// convenience only; these run on every submission regardless.
    /// </summary>
    public static class UserValidator
    {
        // Same patterns as TutorConnect so existing data and habits carry over.
        private static readonly Regex EmailPattern = new Regex(@"^[^@\s]+@[^@\s.]+(\.[^@\s.]+)+$", RegexOptions.Compiled);
        private static readonly Regex CellPattern = new Regex(@"^(0\d{9}|\+27\d{9})$", RegexOptions.Compiled);
        private static readonly Regex UserNamePattern = new Regex(@"^[A-Za-z0-9._-]{3,50}$", RegexOptions.Compiled);
        private static readonly Regex StudentNumberPattern = new Regex(@"^[A-Za-z0-9]{5,20}$", RegexOptions.Compiled);

        public const int MinPasswordLength = 8;

        public static string NormaliseCell(string cell) => (cell ?? string.Empty).Replace(" ", string.Empty).Trim();

        public static bool IsValidEmail(string email) =>
            !string.IsNullOrWhiteSpace(email) && email.Length <= 100 && EmailPattern.IsMatch(email.Trim());

        public static bool IsValidCell(string cell) => CellPattern.IsMatch(NormaliseCell(cell));

        public static bool IsValidUserName(string userName) =>
            !string.IsNullOrWhiteSpace(userName) && UserNamePattern.IsMatch(userName.Trim());

        public static bool IsValidStudentNumber(string number) =>
            !string.IsNullOrWhiteSpace(number) && StudentNumberPattern.IsMatch(number.Trim());

        /// <summary>BR-02/03: at least 8 characters, with a letter and a digit, matching the confirmation.</summary>
        public static string CheckPassword(string password, string confirm)
        {
            if (string.IsNullOrEmpty(password) || password.Length < MinPasswordLength)
                return "Password must be at least 8 characters long.";
            if (password.Length > 128)
                return "Password must be 128 characters or fewer.";
            if (!password.Any(char.IsLetter) || !password.Any(char.IsDigit))
                return "Password must contain at least one letter and one number.";
            if (confirm != null && !string.Equals(password, confirm, StringComparison.Ordinal))
                return "The passwords do not match.";
            return null;
        }

        /// <summary>Validates the personal details every account shares. Returns the first problem, or null.</summary>
        public static string CheckPersonalDetails(string userName, string firstName, string lastName, string email, string cell)
        {
            var errors = new List<string>();
            if (!IsValidUserName(userName))
                return "Username must be 3–50 characters: letters, numbers, dot, dash or underscore.";
            if (string.IsNullOrWhiteSpace(firstName) || firstName.Trim().Length > 50)
                return "First name is required (50 characters max).";
            if (string.IsNullOrWhiteSpace(lastName) || lastName.Trim().Length > 50)
                return "Last name is required (50 characters max).";
            if (!IsValidEmail(email))
                return "Enter a valid email address.";
            if (!IsValidCell(cell))
                return "Enter a South African cell number, e.g. 0821234567 or +27821234567.";
            return null;
        }
    }

    public static class TextRules
    {
        /// <summary>Trims and returns null for empty input, so optional fields store NULL not "".</summary>
        public static string Clean(string value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;
            var trimmed = value.Trim();
            return trimmed.Length > maxLength ? trimmed.Substring(0, maxLength) : trimmed;
        }

        public static bool TooLong(string value, int maxLength) => value != null && value.Trim().Length > maxLength;
    }
}
