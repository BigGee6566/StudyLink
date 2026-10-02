using System;
using System.Data.Entity.Migrations;
using System.Linq;
using StudyLink.Data.Entities;

namespace StudyLink.Data.Seed
{
    /// <summary>Canonical role names. The only place role strings are spelled out.</summary>
    public static class RoleNames
    {
        public const string Student = "Student";
        public const string Tutor = "Tutor";
        public const string Staff = "Staff";
        public const string Supplier = "Supplier";
        public const string Manager = "Manager";
        public const string SystemAdmin = "SystemAdmin";

        public static readonly string[] All = { Student, Tutor, Staff, Supplier, Manager, SystemAdmin };

        public static string DisplayName(string role) => role == SystemAdmin ? "System Admin" : role;
    }

    /// <summary>Keys of the SystemSettings table. Defaults are seeded once and then owned by the System Admin.</summary>
    public static class SettingKeys
    {
        public const string RentalPeriodDays = "Rental.PeriodDays";
        public const string MaxRenewals = "Rental.MaxRenewals";
        public const string RentalHoldMinutes = "Rental.HoldMinutes";
        public const string BookingHoldMinutes = "Booking.HoldMinutes";
        public const string CancellationCutoffHours = "Booking.CancellationCutoffHours";
        public const string MaxSessionHours = "Booking.MaxSessionHours";
        public const string AvailabilityMaxDaysAhead = "Booking.AvailabilityMaxDaysAhead";
        public const string PostponementMaxDaysAhead = "Booking.PostponementMaxDaysAhead";
        public const string TutorCommissionPercent = "Tutor.CommissionPercent";
        public const string LockoutThreshold = "Security.LockoutThreshold";
        public const string LockoutMinutes = "Security.LockoutMinutes";
        public const string ResetTokenMinutes = "Security.ResetTokenMinutes";
        public const string OrganisationName = "General.OrganisationName";
        public const string SupportEmail = "General.SupportEmail";
        public const string BackupDirectory = "Backup.Directory";

        // Shown in the public footer and on the Contact page.
        public const string ContactPhone = "Contact.Phone";
        public const string ContactEmail = "Contact.Email";
        public const string ContactHours = "Contact.Hours";
        public const string AddressAlice = "Contact.AddressAlice";
        public const string AddressEastLondon = "Contact.AddressEastLondon";
        public const string AddressBisho = "Contact.AddressBisho";
        public const string SocialFacebook = "Social.Facebook";
        public const string SocialInstagram = "Social.Instagram";
        public const string SocialX = "Social.X";
        public const string SocialLinkedIn = "Social.LinkedIn";
    }

    /// <summary>
    /// Reference data every installation needs. Run from the migrations Configuration.Seed, so it
    /// is applied on every Update-Database and is idempotent: lookups are matched by name, and
    /// settings are only inserted when missing so an administrator's changes are never overwritten.
    /// </summary>
    public static class LookupData
    {
        public static readonly string[] Campuses = { "Alice", "East London", "Bisho" };

        public static readonly (string Name, string Description)[] Roles =
        {
            (RoleNames.Student, "Rents and buys books, books tutoring sessions and pays fees. Self-registers."),
            (RoleNames.Tutor, "Publishes availability and delivers sessions. Self-registers; must be verified by Staff or a Manager."),
            (RoleNames.Staff, "Hub staff: verifies tutors, captures students, processes returns, fines and password resets."),
            (RoleNames.Supplier, "Book supplier: views orders, dispatches deliveries and uploads invoices."),
            (RoleNames.Manager, "Runs the hub: books, stock, payments, reports, staff and suppliers."),
            (RoleNames.SystemAdmin, "Manages users, roles, system settings, security, backups and audit logs.")
        };

        public static readonly string[] Subjects =
        {
            "Mathematics", "Information Systems", "English", "Accounting", "Economics",
            "Statistics", "Business Management", "Computer Science", "Law", "Psychology"
        };

        public static readonly (string Name, int Sort)[] StudyLevels =
        {
            ("First Year", 1), ("Second Year", 2), ("Third Year", 3), ("Honours", 4), ("Postgraduate", 5)
        };

        public static readonly (string Name, string Description)[] Categories =
        {
            ("Accounting & Finance", "Financial and management accounting, auditing, finance."),
            ("Business & Management", "Management, marketing, entrepreneurship."),
            ("Computer Science & IS", "Programming, databases, information systems."),
            ("Economics", "Micro, macro and development economics."),
            ("Law", "Prescribed law texts and case books."),
            ("Mathematics & Statistics", "Calculus, algebra, statistics."),
            ("Natural Sciences", "Biology, chemistry, physics, agriculture."),
            ("Humanities & Social Sciences", "Psychology, sociology, languages, education.")
        };

        public static readonly (string Key, string Value, string Description)[] Settings =
        {
            (SettingKeys.RentalPeriodDays, "7", "Days a textbook may be kept per rental or renewal (SC12: 1 week)."),
            (SettingKeys.MaxRenewals, "2", "How many times one rental may be renewed."),
            (SettingKeys.RentalHoldMinutes, "30", "Minutes a copy stays reserved while the student pays."),
            (SettingKeys.BookingHoldMinutes, "30", "Minutes a tutoring slot stays reserved while the student pays."),
            (SettingKeys.CancellationCutoffHours, "12", "Students cannot cancel a session within this many hours of its start."),
            (SettingKeys.MaxSessionHours, "8", "Longest single availability slot a tutor may publish (TutorConnect rule)."),
            (SettingKeys.AvailabilityMaxDaysAhead, "365", "How far ahead a tutor may publish availability."),
            (SettingKeys.PostponementMaxDaysAhead, "120", "How far ahead a postponed session may be moved."),
            (SettingKeys.TutorCommissionPercent, "20", "Percentage of each session fee retained by the hub."),
            (SettingKeys.LockoutThreshold, "5", "Failed sign-ins before an account is locked."),
            (SettingKeys.LockoutMinutes, "15", "Minutes an account stays locked."),
            (SettingKeys.ResetTokenMinutes, "60", "Minutes a password-reset link stays valid."),
            (SettingKeys.OrganisationName, "Forte Books & Tuition Hub", "Shown on receipts and emails."),
            (SettingKeys.SupportEmail, "support@studylink.local", "Reply-to address on system emails."),
            (SettingKeys.BackupDirectory, "", "Folder for database backups on the SQL Server machine. Empty = SQL Server default."),
            (SettingKeys.ContactPhone, "To be confirmed", "Hub phone number shown in the footer and on the Contact page."),
            (SettingKeys.ContactEmail, "To be confirmed", "Hub email address shown in the footer and on the Contact page."),
            (SettingKeys.ContactHours, "Mon-Fri 08:00-16:30", "Counter opening hours shown in the footer and on the Contact page."),
            (SettingKeys.AddressAlice, "Hub counter, Alice campus (address to be confirmed)", "Alice campus address."),
            (SettingKeys.AddressEastLondon, "Hub counter, East London campus (address to be confirmed)", "East London campus address."),
            (SettingKeys.AddressBisho, "Hub counter, Bisho campus (address to be confirmed)", "Bisho campus address."),
            (SettingKeys.SocialFacebook, "", "Facebook page URL. Empty = icon shown as a placeholder without a link."),
            (SettingKeys.SocialInstagram, "", "Instagram profile URL. Empty = placeholder."),
            (SettingKeys.SocialX, "", "X (Twitter) profile URL. Empty = placeholder."),
            (SettingKeys.SocialLinkedIn, "", "LinkedIn page URL. Empty = placeholder.")
        };

        public static void Apply(StudyLinkDbContext db)
        {
            var now = DateTime.Now;

            db.Campuses.AddOrUpdate(c => c.Name, Campuses.Select(n => new Campus { Name = n, IsActive = true }).ToArray());
            db.Roles.AddOrUpdate(r => r.Name, Roles.Select(r => new Role { Name = r.Name, Description = r.Description }).ToArray());
            db.StudyLevels.AddOrUpdate(l => l.Name, StudyLevels.Select(l => new StudyLevel { Name = l.Name, SortOrder = l.Sort }).ToArray());

            // Subjects and categories: add missing ones only, so an admin's deactivation sticks.
            foreach (var name in Subjects.Where(n => !db.Subjects.Any(s => s.Name == n)))
                db.Subjects.Add(new Subject { Name = name, IsActive = true });
            foreach (var c in Categories.Where(c => !db.Categories.Any(x => x.Name == c.Name)))
                db.Categories.Add(new Category { Name = c.Name, Description = c.Description, IsActive = true });
            foreach (var s in Settings.Where(s => !db.SystemSettings.Any(x => x.SettingKey == s.Key)))
                db.SystemSettings.Add(new SystemSetting { SettingKey = s.Key, Value = s.Value, Description = s.Description, UpdatedAt = now });

            db.SaveChanges();
        }
    }
}
