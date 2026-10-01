using System;
using System.Globalization;
using System.Linq;
using System.Web;

namespace StudyLink.Web.Infrastructure
{
    /// <summary>Display formatting shared by every page (money, dates, status badges).</summary>
    public static class Fmt
    {
        private static readonly CultureInfo Gb = CultureInfo.GetCultureInfo("en-GB");

        public static string Money(decimal value) => "R " + value.ToString("N2", Gb);
        public static string Money(object value) => value == null ? "–" : Money(Convert.ToDecimal(value, Gb));
        public static string Date(DateTime value) => value.ToString("d MMM yyyy", Gb);
        public static string Date(DateTime? value) => value.HasValue ? Date(value.Value) : "–";
        public static string DateTime(DateTime value) => value.ToString("d MMM yyyy, HH:mm", Gb);
        public static string DateTime(DateTime? value) => value.HasValue ? DateTime(value.Value) : "–";
        public static string Day(DateTime value) => value.ToString("ddd d MMM", Gb);
        public static string Time(TimeSpan value) => value.ToString(@"hh\:mm", CultureInfo.InvariantCulture);
        public static string Range(TimeSpan start, TimeSpan end) => Time(start) + "–" + Time(end);
        public static string Hours(decimal hours) => hours.ToString("0.##", Gb) + " h";
        public static string H(object value) => HttpUtility.HtmlEncode(Convert.ToString(value, Gb));

        /// <summary>"AwaitingTutor" → "Awaiting tutor".</summary>
        public static string Words(object value)
        {
            var s = Convert.ToString(value, Gb) ?? string.Empty;
            if (s == "SystemAdmin") return "System Admin";
            return string.Concat(s.Select((c, i) => i > 0 && char.IsUpper(c) && !char.IsUpper(s[i - 1]) ? " " + char.ToLowerInvariant(c) : c.ToString()));
        }

        public static string Ago(DateTime value)
        {
            var span = System.DateTime.Now - value;
            if (span.TotalMinutes < 1) return "just now";
            if (span.TotalMinutes < 60) return (int)span.TotalMinutes + " min ago";
            if (span.TotalHours < 24) return (int)span.TotalHours + " h ago";
            if (span.TotalDays < 7) return (int)span.TotalDays + " d ago";
            return Date(value);
        }

        /// <summary>Status pill (TutorConnect .badge design) with a colour chosen from the status meaning.</summary>
        public static string Badge(object status)
        {
            var name = Convert.ToString(status, Gb) ?? string.Empty;
            return "<span class=\"badge badge--" + Tone(name) + "\">" + H(Words(name)) + "</span>";
        }

        public static string Tone(string status)
        {
            switch (status)
            {
                case "Paid": case "Completed": case "Confirmed": case "Verified": case "Approved": case "Available":
                case "Received": case "Returned": case "Delivered": case "Resolved": case "Succeeded": case "Attended":
                case "Active": case "True":
                    return "success";
                case "Pending": case "PendingVerification": case "AwaitingTutor": case "AwaitingPayment": case "Submitted":
                case "Placed": case "Acknowledged": case "Reserved": case "Draft": case "Open": case "InProgress":
                case "Dispatched": case "PartiallyDelivered": case "Running": case "PendingTutorReview": case "PendingPayment":
                case "Low":
                    return "pending";
                case "Cancelled": case "Declined": case "Rejected": case "Failed": case "Lost": case "Damaged": case "WrittenOff":
                case "Suspended": case "Expired": case "Overdue": case "Unpaid": case "Outstanding": case "StudentAbsent":
                case "TutorAbsent": case "Inactive": case "Locked": case "False": case "Out of stock":
                    return "danger";
                case "Rented": case "Refunded": case "Waived": case "Charge": case "Refund": case "Upcoming":
                    return "info";
                default:
                    return "neutral";
            }
        }
    }
}