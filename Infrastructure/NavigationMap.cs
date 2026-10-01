using System;
using System.Collections.Generic;
using System.Linq;
using StudyLink.Data.Seed;

namespace StudyLink.Web.Infrastructure
{
    public sealed class NavItem
    {
        public NavItem(string text, string url, string icon, bool secondary = false)
        {
            Text = text;
            Url = url;
            Icon = icon;
            Secondary = secondary;
        }

        public string Text { get; }
        public string Url { get; }
        public string Icon { get; }
        public bool Secondary { get; }
    }

    /// <summary>
    /// Role-based navigation (brief §8). Primary items are exactly the brief's menu for each role;
    /// secondary items ("More") reach the extra operational screens. Menus only hide links - the
    /// pages and services enforce access on their own.
    /// </summary>
    public static class NavigationMap
    {
        private static readonly Dictionary<string, NavItem[]> Menus = new Dictionary<string, NavItem[]>(StringComparer.OrdinalIgnoreCase)
        {
            [RoleNames.Student] = new[]
            {
                new NavItem("Home", "~/Student/Dashboard.aspx", "home"),
                new NavItem("Books", "~/Catalogue/Books.aspx", "book"),
                new NavItem("Tutoring", "~/Catalogue/Tutors.aspx", "cap"),
                new NavItem("My Bookings", "~/Student/Tutoring/MyBookings.aspx", "calendar"),
                new NavItem("My Rentals", "~/Student/Books/MyRentals.aspx", "library"),
                new NavItem("Fees & Payments", "~/Student/Fees/MyFees.aspx", "card"),
                new NavItem("Profile", "~/Account/Profile.aspx", "user")
            },
            [RoleNames.Tutor] = new[]
            {
                new NavItem("Home", "~/Tutor/Dashboard.aspx", "home"),
                new NavItem("Schedule", "~/Tutor/Schedule.aspx", "calendar"),
                new NavItem("Availability", "~/Tutor/Availability.aspx", "clock"),
                new NavItem("Sessions", "~/Tutor/Schedule.aspx?view=requests", "list"),
                new NavItem("Attendance", "~/Tutor/Schedule.aspx?view=past", "check"),
                new NavItem("Timesheets", "~/Tutor/Timesheet.aspx", "sheet"),
                new NavItem("Earnings", "~/Tutor/Earnings.aspx", "coins"),
                new NavItem("Profile", "~/Account/Profile.aspx", "user")
            },
            [RoleNames.Staff] = new[]
            {
                new NavItem("Home", "~/Staff/Dashboard.aspx", "home"),
                new NavItem("Tutor Verification", "~/Operations/TutorVerification.aspx", "shield"),
                new NavItem("Students", "~/Operations/Users.aspx?role=Student", "users"),
                new NavItem("Returns", "~/Operations/Returns.aspx", "undo"),
                new NavItem("Fines", "~/Operations/Fines.aspx", "alert"),
                new NavItem("Support", "~/Operations/Support.aspx", "support"),
                new NavItem("Profile", "~/Account/Profile.aspx", "user"),
                new NavItem("Password Resets", "~/Operations/PasswordResets.aspx", "key", true),
                new NavItem("Timesheets", "~/Operations/Timesheets.aspx", "sheet", true),
                new NavItem("Deliveries", "~/Operations/Deliveries.aspx", "truck", true),
                new NavItem("Reports", "~/Operations/Reports.aspx", "chart", true)
            },
            [RoleNames.Supplier] = new[]
            {
                new NavItem("Home", "~/Supplier/Dashboard.aspx", "home"),
                new NavItem("Orders", "~/Supplier/Orders.aspx", "box"),
                new NavItem("Deliveries", "~/Supplier/Deliveries.aspx", "truck"),
                new NavItem("Invoices", "~/Supplier/Invoices.aspx", "invoice"),
                new NavItem("Stock", "~/Supplier/Stock.aspx", "library"),
                new NavItem("Profile", "~/Account/Profile.aspx", "user")
            },
            [RoleNames.Manager] = new[]
            {
                new NavItem("Home", "~/Manager/Dashboard.aspx", "home"),
                new NavItem("Books", "~/Manager/Books.aspx", "book"),
                new NavItem("Inventory", "~/Manager/Books.aspx?view=inventory", "box"),
                new NavItem("Payments", "~/Manager/Payments.aspx", "card"),
                new NavItem("Rentals", "~/Operations/Returns.aspx", "library"),
                new NavItem("Reports", "~/Operations/Reports.aspx", "chart"),
                new NavItem("Staff", "~/Operations/Users.aspx?role=Staff", "users"),
                new NavItem("Suppliers", "~/Manager/SupplierOrders.aspx", "truck"),
                new NavItem("Profile", "~/Account/Profile.aspx", "user"),
                new NavItem("Tutor Verification", "~/Operations/TutorVerification.aspx", "shield", true),
                new NavItem("Timesheets", "~/Operations/Timesheets.aspx", "sheet", true),
                new NavItem("Deliveries", "~/Operations/Deliveries.aspx", "truck", true),
                new NavItem("Fines", "~/Operations/Fines.aspx", "alert", true),
                new NavItem("Categories", "~/Manager/Categories.aspx", "tag", true),
                new NavItem("Subjects", "~/Operations/Subjects.aspx", "cap", true),
                new NavItem("Reviews", "~/Operations/Reviews.aspx", "star", true),
                new NavItem("Support", "~/Operations/Support.aspx", "support", true)
            },
            [RoleNames.SystemAdmin] = new[]
            {
                new NavItem("Home", "~/Admin/Dashboard.aspx", "home"),
                new NavItem("Users", "~/Operations/Users.aspx", "users"),
                new NavItem("Roles", "~/Admin/Roles.aspx", "shield"),
                new NavItem("System Settings", "~/Admin/Settings.aspx", "settings"),
                new NavItem("Audit Logs", "~/Admin/AuditLog.aspx", "audit"),
                new NavItem("Security", "~/Admin/Security.aspx", "lock"),
                new NavItem("Backups", "~/Admin/Backups.aspx", "database"),
                new NavItem("Profile", "~/Account/Profile.aspx", "user"),
                new NavItem("Password Resets", "~/Operations/PasswordResets.aspx", "key", true),
                new NavItem("Subjects", "~/Operations/Subjects.aspx", "cap", true),
                new NavItem("Reviews", "~/Operations/Reviews.aspx", "star", true),
                new NavItem("Support", "~/Operations/Support.aspx", "support", true)
            }
        };

        /// <summary>Mobile bottom tab bar (the Sprint 1 mockups' Home / Books / Fees / Profile style).</summary>
        private static readonly Dictionary<string, string[]> TabTexts = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            [RoleNames.Student] = new[] { "Home", "Books", "Fees & Payments", "Profile" },
            [RoleNames.Tutor] = new[] { "Home", "Schedule", "Timesheets", "Profile" },
            [RoleNames.Staff] = new[] { "Home", "Tutor Verification", "Returns", "Profile" },
            [RoleNames.Supplier] = new[] { "Home", "Orders", "Invoices", "Profile" },
            [RoleNames.Manager] = new[] { "Home", "Books", "Reports", "Profile" },
            [RoleNames.SystemAdmin] = new[] { "Home", "Users", "Audit Logs", "Profile" }
        };

        private static readonly string[] Order = { RoleNames.SystemAdmin, RoleNames.Manager, RoleNames.Staff, RoleNames.Supplier, RoleNames.Tutor, RoleNames.Student };

        public static IList<NavItem> For(IEnumerable<string> roles)
        {
            var held = Order.Where(r => roles.Contains(r, StringComparer.OrdinalIgnoreCase)).ToList();
            var items = new List<NavItem>();
            foreach (var role in held)
                foreach (var item in Menus[role])
                    if (items.All(i => i.Url != item.Url))
                        items.Add(item);
            return items;
        }

        public static IList<NavItem> Tabs(IEnumerable<string> roles)
        {
            var primary = Order.FirstOrDefault(r => roles.Contains(r, StringComparer.OrdinalIgnoreCase));
            if (primary == null) return new List<NavItem>();
            return TabTexts[primary].Select(t => Menus[primary].First(i => i.Text == t)).ToList();
        }

        public static string RoleLabel(IEnumerable<string> roles)
        {
            var primary = Order.FirstOrDefault(r => roles.Contains(r, StringComparer.OrdinalIgnoreCase));
            return primary == null ? "" : RoleNames.DisplayName(primary);
        }
    }
}