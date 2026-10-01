/* =============================================================================
   StudyLink - 02_SeedLookups.sql
   Reference data every installation needs. Mirrors Data\Seed\LookupData.cs,
   which the application also applies on start-up, so running both is harmless:
   every insert below is "only if missing". Settings an administrator has changed
   are never overwritten.

   Creates NO user accounts. The first System Admin is created through the
   /Account/Setup.aspx page on first run (ported from TutorConnect), and demo
   accounts come from the debug-only DemoSeeder (see README).
   ============================================================================= */
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
GO
USE StudyLink;
GO

DECLARE @now DATETIME2(0) = SYSDATETIME();

/* Campuses (Forte Books & Tuition Hub, University of Fort Hare) */
INSERT dbo.Campuses (Name, IsActive)
SELECT v.Name, 1 FROM (VALUES (N'Alice'), (N'East London'), (N'Bisho')) v(Name)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Campuses c WHERE c.Name = v.Name);

/* Roles - names must match StudyLink.Data.Seed.RoleNames */
INSERT dbo.Roles (Name, Description)
SELECT v.Name, v.Description FROM (VALUES
    (N'Student',     N'Rents and buys books, books tutoring sessions and pays fees. Self-registers.'),
    (N'Tutor',       N'Publishes availability and delivers sessions. Self-registers; must be verified by Staff or a Manager.'),
    (N'Staff',       N'Hub staff: verifies tutors, captures students, processes returns, fines and password resets.'),
    (N'Supplier',    N'Book supplier: views orders, dispatches deliveries and uploads invoices.'),
    (N'Manager',     N'Runs the hub: books, stock, payments, reports, staff and suppliers.'),
    (N'SystemAdmin', N'Manages users, roles, system settings, security, backups and audit logs.')
) v(Name, Description)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Roles r WHERE r.Name = v.Name);

/* Study levels - SortOrder keeps First Year before Second Year */
INSERT dbo.StudyLevels (Name, SortOrder)
SELECT v.Name, v.SortOrder FROM (VALUES
    (N'First Year', 1), (N'Second Year', 2), (N'Third Year', 3), (N'Honours', 4), (N'Postgraduate', 5)
) v(Name, SortOrder)
WHERE NOT EXISTS (SELECT 1 FROM dbo.StudyLevels l WHERE l.Name = v.Name);

/* Subjects (the three TutorConnect originals first) */
INSERT dbo.Subjects (Name, IsActive)
SELECT v.Name, 1 FROM (VALUES
    (N'Mathematics'), (N'Information Systems'), (N'English'), (N'Accounting'), (N'Economics'),
    (N'Statistics'), (N'Business Management'), (N'Computer Science'), (N'Law'), (N'Psychology')
) v(Name)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Subjects s WHERE s.Name = v.Name);

/* Book categories */
INSERT dbo.Categories (Name, Description, IsActive)
SELECT v.Name, v.Description, 1 FROM (VALUES
    (N'Accounting & Finance',          N'Financial and management accounting, auditing, finance.'),
    (N'Business & Management',         N'Management, marketing, entrepreneurship.'),
    (N'Computer Science & IS',         N'Programming, databases, information systems.'),
    (N'Economics',                     N'Micro, macro and development economics.'),
    (N'Law',                           N'Prescribed law texts and case books.'),
    (N'Mathematics & Statistics',      N'Calculus, algebra, statistics.'),
    (N'Natural Sciences',              N'Biology, chemistry, physics, agriculture.'),
    (N'Humanities & Social Sciences',  N'Psychology, sociology, languages, education.')
) v(Name, Description)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Categories c WHERE c.Name = v.Name);

/* Business rules the System Admin may tune (keys match StudyLink.Data.Seed.SettingKeys) */
INSERT dbo.SystemSettings (SettingKey, Value, Description, UpdatedAt)
SELECT v.SettingKey, v.Value, v.Description, @now FROM (VALUES
    (N'Rental.PeriodDays',                    N'7',   N'Days a textbook may be kept per rental or renewal (SC12: 1 week).'),
    (N'Rental.MaxRenewals',                   N'2',   N'How many times one rental may be renewed.'),
    (N'Rental.HoldMinutes',                   N'30',  N'Minutes a copy stays reserved while the student pays.'),
    (N'Booking.HoldMinutes',                  N'30',  N'Minutes a tutoring slot stays reserved while the student pays.'),
    (N'Booking.CancellationCutoffHours',      N'12',  N'Students cannot cancel a session within this many hours of its start.'),
    (N'Booking.MaxSessionHours',              N'8',   N'Longest single availability slot a tutor may publish (TutorConnect rule).'),
    (N'Booking.AvailabilityMaxDaysAhead',     N'365', N'How far ahead a tutor may publish availability.'),
    (N'Booking.PostponementMaxDaysAhead',     N'120', N'How far ahead a postponed session may be moved.'),
    (N'Tutor.CommissionPercent',              N'20',  N'Percentage of each session fee retained by the hub.'),
    (N'Security.LockoutThreshold',            N'5',   N'Failed sign-ins before an account is locked.'),
    (N'Security.LockoutMinutes',              N'15',  N'Minutes an account stays locked.'),
    (N'Security.ResetTokenMinutes',           N'60',  N'Minutes a password-reset link stays valid.'),
    (N'General.OrganisationName',             N'Forte Books & Tuition Hub', N'Shown on receipts and emails.'),
    (N'General.SupportEmail',                 N'support@studylink.local',   N'Reply-to address on system emails.'),
    (N'Backup.Directory',                     N'',    N'Folder for database backups on the SQL Server machine. Empty = SQL Server default.')
) v(SettingKey, Value, Description)
WHERE NOT EXISTS (SELECT 1 FROM dbo.SystemSettings s WHERE s.SettingKey = v.SettingKey);

PRINT 'StudyLink lookup data is in place.';
GO
