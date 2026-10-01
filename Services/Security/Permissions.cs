using System;
using System.Collections.Generic;
using System.Linq;
using StudyLink.Data.Seed;
using StudyLink.Services.Infrastructure;

namespace StudyLink.Services.Security
{
    /// <summary>
    /// Everything a signed-in user can be allowed to do. Pages declare the permission they need,
    /// services demand it, and the navigation menu is filtered by it - so the role → capability
    /// rules live in exactly one place (<see cref="RolePermissions"/>) instead of role-name checks
    /// scattered through the code.
    /// </summary>
    public enum Permission
    {
        // Student
        BookTutoring,
        RentOrBuyBooks,
        PayFees,
        ReviewTutors,

        // Tutor
        ManageAvailability,
        RespondToBookings,
        SubmitTimesheet,

        // Staff (some shared with Manager)
        VerifyTutors,
        CaptureStudents,
        ProcessReturns,
        ManageFines,
        ReviewTimesheets,
        HandlePasswordResets,
        HandleSupport,
        ReceiveDeliveries,
        ViewOperationalReports,

        // Supplier
        SupplierPortal,

        // Manager
        ManageBooks,
        ManagePayments,
        ManageSupplierOrders,
        ReviewInvoices,
        CreateStaffAndSuppliers,
        ViewAllReports,
        ManageSubjects,
        ModerateReviews,

        // System Admin
        ManageAllUsers,
        AssignRoles,
        ManageSettings,
        ManageSecurity,
        ManageBackups,
        ViewAuditLogs
    }

    public static class RolePermissions
    {
        private static readonly IReadOnlyDictionary<string, Permission[]> Matrix =
            new Dictionary<string, Permission[]>(StringComparer.OrdinalIgnoreCase)
            {
                [RoleNames.Student] = new[]
                {
                    Permission.BookTutoring, Permission.RentOrBuyBooks, Permission.PayFees, Permission.ReviewTutors
                },
                [RoleNames.Tutor] = new[]
                {
                    Permission.ManageAvailability, Permission.RespondToBookings, Permission.SubmitTimesheet
                },
                [RoleNames.Staff] = new[]
                {
                    Permission.VerifyTutors, Permission.CaptureStudents, Permission.ProcessReturns,
                    Permission.ManageFines, Permission.ReviewTimesheets, Permission.HandlePasswordResets,
                    Permission.HandleSupport, Permission.ReceiveDeliveries, Permission.ViewOperationalReports
                },
                [RoleNames.Supplier] = new[]
                {
                    Permission.SupplierPortal
                },
                [RoleNames.Manager] = new[]
                {
                    Permission.VerifyTutors, Permission.CaptureStudents, Permission.ProcessReturns,
                    Permission.ManageFines, Permission.ReviewTimesheets, Permission.HandleSupport,
                    Permission.ReceiveDeliveries, Permission.ViewOperationalReports,
                    Permission.ManageBooks, Permission.ManagePayments, Permission.ManageSupplierOrders,
                    Permission.ReviewInvoices, Permission.CreateStaffAndSuppliers, Permission.ViewAllReports,
                    Permission.ManageSubjects, Permission.ModerateReviews
                },
                [RoleNames.SystemAdmin] = new[]
                {
                    Permission.ManageAllUsers, Permission.AssignRoles, Permission.CreateStaffAndSuppliers,
                    Permission.ManageSettings, Permission.ManageSecurity, Permission.ManageBackups,
                    Permission.ViewAuditLogs, Permission.HandlePasswordResets, Permission.HandleSupport,
                    Permission.ManageSubjects, Permission.ModerateReviews
                }
            };

        public static IEnumerable<Permission> For(string role) =>
            Matrix.TryGetValue(role ?? string.Empty, out var perms) ? perms : Enumerable.Empty<Permission>();

        public static bool Has(IEnumerable<string> roles, Permission permission) =>
            roles != null && roles.Any(r => For(r).Contains(permission));

        public static bool Has(Actor actor, Permission permission) =>
            actor != null && Has(actor.Roles, permission);

        /// <summary>Roles holding a permission - used to notify "everyone who can verify tutors", etc.</summary>
        public static IEnumerable<string> RolesWith(Permission permission) =>
            Matrix.Where(kv => kv.Value.Contains(permission)).Select(kv => kv.Key);

        /// <summary>Throws <see cref="AccessDeniedException"/> unless the actor holds the permission.</summary>
        public static void Demand(Actor actor, Permission permission)
        {
            if (!Has(actor, permission))
                throw new AccessDeniedException("You do not have permission to perform this action.");
        }

        /// <summary>Roles a given creator may create accounts for (brief §7).</summary>
        public static IEnumerable<string> CreatableRoles(Actor creator)
        {
            if (creator.IsInRole(RoleNames.SystemAdmin))
                return new[] { RoleNames.Manager, RoleNames.Staff, RoleNames.Supplier, RoleNames.SystemAdmin, RoleNames.Student };
            if (creator.IsInRole(RoleNames.Manager))
                return new[] { RoleNames.Staff, RoleNames.Supplier, RoleNames.Student };
            if (creator.IsInRole(RoleNames.Staff))
                return new[] { RoleNames.Student };
            return Enumerable.Empty<string>();
        }
    }
}
