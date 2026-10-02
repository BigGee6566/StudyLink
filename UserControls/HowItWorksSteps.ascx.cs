using System.Collections.Generic;
using System.Web.UI;
using StudyLink.Data.Seed;
using StudyLink.Web.Infrastructure;

namespace StudyLink.Web.UserControls
{
    /// <summary>The animated four-step "How it works" strip, for everyone, students or tutors.</summary>
    public partial class HowItWorksSteps : UserControl
    {
        public sealed class Step
        {
            public Step(string icon, string title, string text) { Icon = icon; Title = title; Text = text; }
            public string Icon { get; }
            public string Title { get; }
            public string Text { get; }
        }

        /// <summary>"general" (landing page), "students" or "tutors".</summary>
        public string Audience { get; set; } = "general";

        private IList<Step> _steps;
        protected IList<Step> Steps => _steps ?? (_steps = Build());

        private IList<Step> Build()
        {
            var settings = AppServices.Current.Settings;
            switch (Audience)
            {
                case "students":
                    return new[]
                    {
                        new Step("user", "Register", "Create a student account with your 10-digit student number and choose your campus: Alice, East London or Bisho."),
                        new Step("cap", "Book a tutor", "Search verified tutors by subject and level, pick an open slot and pay to reserve it. If the tutor declines, you are refunded in full."),
                        new Step("book", "Rent or buy books", "A copy is held for " + settings.GetInt(SettingKeys.RentalHoldMinutes, 30) + " minutes while you pay; collect it at the hub counter. Rentals run for "
                                                              + settings.GetInt(SettingKeys.RentalPeriodDays, 7) + " days and can be renewed."),
                        new Step("card", "Pay and track", "Every fee appears under My Fees. Pay one or several items by card and get a receipt by email.")
                    };
                case "tutors":
                    return new[]
                    {
                        new Step("user", "Apply", "Register as a tutor with your qualification, the subjects you teach and your hourly rate."),
                        new Step("shield", "Get verified", "Hub staff check your identity and qualifications. Until then you can sign in and complete your profile."),
                        new Step("calendar", "Publish your slots", "Publish the times you are available at your campus. A slot can never be double-booked."),
                        new Step("coins", "Teach and get paid", "Confirm sessions, record attendance and submit your weekly timesheet. Earnings are shown after the hub's commission.")
                    };
                default:
                    return new[]
                    {
                        new Step("user", "Create your account", "Students register in minutes with their student number. Tutors apply and are verified by hub staff."),
                        new Step("search", "Find what you need", "Search verified tutors by subject, or check live textbook stock at Alice, East London and Bisho."),
                        new Step("card", "Book and pay securely", "Reserve a session or a copy and pay by card. Every payment comes with an instant receipt."),
                        new Step("cap", "Learn and keep track", "Get reminders before sessions and due dates, renew your books and rate your tutor.")
                    };
            }
        }
    }
}
