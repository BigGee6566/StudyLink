using System;
using System.Collections.Generic;
using System.Linq;
using StudyLink.Data.Entities;

namespace StudyLink.Services.Policies
{
    /// <summary>
    /// Pure scheduling rules - no database, so every rule is unit-tested directly.
    /// Rule IDs (BR-xx) are the TutorConnect business-rule numbers they were ported from.
    /// </summary>
    public static class SchedulePolicy
    {
        /// <summary>
        /// Half-open interval overlap: a slot ending 12:00 does NOT clash with one starting 12:00
        /// (TutorConnect BR-12/14/20).
        /// </summary>
        public static bool Overlaps(TimeSpan aStart, TimeSpan aEnd, TimeSpan bStart, TimeSpan bEnd) =>
            aStart < bEnd && bStart < aEnd;

        public static bool Overlaps(DateTime aDate, TimeSpan aStart, TimeSpan aEnd, DateTime bDate, TimeSpan bStart, TimeSpan bEnd) =>
            aDate.Date == bDate.Date && Overlaps(aStart, aEnd, bStart, bEnd);

        /// <summary>BR-06/07 plus the max-length and how-far-ahead limits. Returns an error or null.</summary>
        public static string CheckSlot(DateTime date, TimeSpan start, TimeSpan end, DateTime now, int maxHours, int maxDaysAhead)
        {
            if (end <= start)
                return "The end time must be after the start time.";
            if ((end - start).TotalMinutes < 30)
                return "A session must be at least 30 minutes long.";
            if ((end - start).TotalHours > maxHours)
                return $"A single slot cannot be longer than {maxHours} hours.";
            if (date.Date + start <= now)
                return "The slot must start in the future.";
            if (date.Date > now.Date.AddDays(maxDaysAhead))
                return $"Slots can be published at most {maxDaysAhead} days ahead.";
            return null;
        }

        public static decimal HoursBetween(TimeSpan start, TimeSpan end) =>
            Math.Round((decimal)(end - start).TotalMinutes / 60m, 2);

        /// <summary>Session fee = hourly rate × length, rounded to cents (SC8 "Session Fee").</summary>
        public static decimal SessionFee(decimal hourlyRate, TimeSpan start, TimeSpan end) =>
            Math.Round(hourlyRate * HoursBetween(start, end), 2, MidpointRounding.AwayFromZero);

        /// <summary>Students may cancel only until the cut-off before the session starts.</summary>
        public static bool IsBeforeCutoff(DateTime sessionDate, TimeSpan start, DateTime now, int cutoffHours) =>
            sessionDate.Date + start - now >= TimeSpan.FromHours(cutoffHours);

        /// <summary>BR-23: a session can be completed only once its end time has passed.</summary>
        public static bool HasEnded(DateTime sessionDate, TimeSpan end, DateTime now) => sessionDate.Date + end <= now;

        public static DateTime WeekStart(DateTime date)
        {
            var diff = ((int)date.DayOfWeek + 6) % 7; // Monday = 0
            return date.Date.AddDays(-diff);
        }

        public static bool IsActive(BookingStatus status) =>
            status == BookingStatus.AwaitingPayment || status == BookingStatus.AwaitingTutor || status == BookingStatus.Confirmed;

        /// <summary>Utilisation = hours delivered ÷ hours published, as a percentage (SC10 CR4).</summary>
        public static decimal UtilisationPercent(decimal hoursBooked, decimal hoursPublished) =>
            hoursPublished <= 0 ? 0 : Math.Round(Math.Min(100m, hoursBooked / hoursPublished * 100m), 1);
    }

    public static class RentalPolicy
    {
        public static DateTime DueDate(DateTime from, int periodDays) => from.Date.AddDays(periodDays);

        public static bool IsOverdue(RentalStatus status, DateTime dueDate, DateTime today) =>
            status == RentalStatus.Active && today.Date > dueDate.Date;

        public static int DaysLate(DateTime dueDate, DateTime returnedOn) =>
            Math.Max(0, (returnedOn.Date - dueDate.Date).Days);

        /// <summary>
        /// Overdue fine = days late × the title's late fee per day, capped at the book's sale price
        /// so a forgotten book never costs more than buying it.
        /// </summary>
        public static decimal OverdueFine(int daysLate, decimal lateFeePerDay, decimal salePrice)
        {
            if (daysLate <= 0 || lateFeePerDay <= 0)
                return 0m;
            var fine = daysLate * lateFeePerDay;
            return salePrice > 0 ? Math.Min(fine, salePrice) : fine;
        }

        /// <summary>Returns why a rental cannot be renewed, or null when it can.</summary>
        public static string CheckRenewal(RentalStatus status, DateTime dueDate, DateTime today, int renewalCount, int maxRenewals, int waitingStudents)
        {
            if (status != RentalStatus.Active)
                return "Only an active rental can be renewed.";
            if (IsOverdue(status, dueDate, today))
                return "This rental is overdue. Return the book to the hub; overdue rentals cannot be renewed.";
            if (renewalCount >= maxRenewals)
                return $"This rental has already been renewed the maximum of {maxRenewals} times.";
            if (waitingStudents > 0)
                return "Another student is waiting for this title, so it cannot be renewed.";
            return null;
        }
    }

    /// <summary>Stock figures derived from copy statuses - never stored, so they cannot drift.</summary>
    public sealed class StockSummary
    {
        public int Available { get; set; }
        public int Reserved { get; set; }
        public int Rented { get; set; }
        public int Overdue { get; set; }
        public int Sold { get; set; }
        public int Lost { get; set; }
        public int Damaged { get; set; }
        public int WrittenOff { get; set; }
        public int ReorderLevel { get; set; }

        /// <summary>Copies the hub still owns and could circulate.</summary>
        public int InStock => Available + Reserved + Rented;
        public bool IsLowStock => Available < ReorderLevel;
        public bool IsOutOfStock => Available == 0;

        public static StockSummary From(IEnumerable<CopyStatus> statuses, int overdue, int reorderLevel)
        {
            var list = statuses.ToList();
            return new StockSummary
            {
                Available = list.Count(s => s == CopyStatus.Available),
                Reserved = list.Count(s => s == CopyStatus.Reserved),
                Rented = list.Count(s => s == CopyStatus.Rented),
                Sold = list.Count(s => s == CopyStatus.Sold),
                Lost = list.Count(s => s == CopyStatus.Lost),
                Damaged = list.Count(s => s == CopyStatus.Damaged),
                WrittenOff = list.Count(s => s == CopyStatus.WrittenOff),
                Overdue = overdue,
                ReorderLevel = reorderLevel
            };
        }
    }

    public static class MoneyPolicy
    {
        public static decimal Round(decimal amount) => Math.Round(amount, 2, MidpointRounding.AwayFromZero);

        /// <summary>Tutor's share of a fee after the hub's commission.</summary>
        public static decimal TutorShare(decimal fee, decimal commissionPercent) =>
            Round(fee * (100m - Math.Max(0, Math.Min(100, commissionPercent))) / 100m);

        /// <summary>A payment must settle each selected fee in full and the total must match exactly.</summary>
        public static string CheckAllocation(decimal paymentAmount, IEnumerable<decimal> feeAmounts)
        {
            var fees = feeAmounts.ToList();
            if (fees.Count == 0)
                return "Select at least one fee to pay.";
            if (fees.Any(a => a <= 0))
                return "Fee amounts must be greater than zero.";
            if (Round(fees.Sum()) != Round(paymentAmount))
                return "The payment total does not match the selected fees.";
            return null;
        }
    }

    public sealed class CardCheck
    {
        public bool IsValid => Error == null;
        public string Error { get; set; }
        public string Brand { get; set; }
        public string Last4 { get; set; }
    }

    /// <summary>
    /// Card format checks for the simulated gateway (D-6). The full number and CVV are used only
    /// in memory for these checks and are never stored or logged.
    /// </summary>
    public static class CardValidator
    {
        public static CardCheck Check(string number, string expiry, string cvv, string holder, DateTime today)
        {
            var digits = new string((number ?? string.Empty).Where(char.IsDigit).ToArray());
            var result = new CardCheck();

            if (string.IsNullOrWhiteSpace(holder) || holder.Trim().Length < 2)
                return Fail(result, "Enter the cardholder's name.");
            if (digits.Length < 13 || digits.Length > 19 || !PassesLuhn(digits))
                return Fail(result, "The card number is not valid.");

            result.Brand = BrandOf(digits);
            if (result.Brand == null)
                return Fail(result, "Only Visa, Mastercard and American Express cards are accepted.");
            if (!TryParseExpiry(expiry, out var lastValidDay))
                return Fail(result, "Enter the expiry date as MM/YY.");
            if (lastValidDay < today.Date)
                return Fail(result, "This card has expired.");

            var cvvDigits = (cvv ?? string.Empty).Trim();
            var cvvLength = result.Brand == "Amex" ? 4 : 3;
            if (cvvDigits.Length != cvvLength || !cvvDigits.All(char.IsDigit))
                return Fail(result, $"The security code must be {cvvLength} digits.");

            result.Last4 = digits.Substring(digits.Length - 4);
            return result;
        }

        public static bool PassesLuhn(string digits)
        {
            var sum = 0;
            var doubleIt = false;
            for (var i = digits.Length - 1; i >= 0; i--)
            {
                var d = digits[i] - '0';
                if (doubleIt)
                {
                    d *= 2;
                    if (d > 9) d -= 9;
                }
                sum += d;
                doubleIt = !doubleIt;
            }
            return sum % 10 == 0;
        }

        public static string BrandOf(string digits)
        {
            if (digits.StartsWith("4")) return "Visa";
            if (digits.StartsWith("34") || digits.StartsWith("37")) return "Amex";
            if (digits.Length >= 2)
            {
                var two = int.Parse(digits.Substring(0, 2));
                if (two >= 51 && two <= 55) return "Mastercard";
                if (digits.Length >= 4)
                {
                    var four = int.Parse(digits.Substring(0, 4));
                    if (four >= 2221 && four <= 2720) return "Mastercard";
                }
            }
            return null;
        }

        /// <summary>MM/YY → last day of that month.</summary>
        public static bool TryParseExpiry(string expiry, out DateTime lastValidDay)
        {
            lastValidDay = DateTime.MinValue;
            var parts = (expiry ?? string.Empty).Trim().Split('/');
            if (parts.Length != 2 || !int.TryParse(parts[0], out var month) || !int.TryParse(parts[1], out var year))
                return false;
            if (month < 1 || month > 12 || year < 0 || year > 99)
                return false;
            var first = new DateTime(2000 + year, month, 1);
            lastValidDay = first.AddMonths(1).AddDays(-1);
            return true;
        }

        private static CardCheck Fail(CardCheck result, string error)
        {
            result.Error = error;
            return result;
        }
    }
}
