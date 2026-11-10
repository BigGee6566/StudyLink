/* =====================================================================================================
   StudyLink - sample history (two months of activity)                              database\SeedData.sql

   Run ONCE, after database\StudyLink.sql, against a StudyLink database that already has the demo
   accounts (admin, manager, staff, supplier ...):

       sqlcmd -S .\SQLEXPRESS -E -d StudyLink -i database\SeedData.sql

   or open it in SSMS with the StudyLink database selected and press Execute. Running it again prints
   "already seeded" and changes nothing. Everything runs in one transaction: an error rolls it all back.

   What it adds (all dates are relative to the moment the script runs, so the data always looks current):
     * 30 students (10 per campus) and 15 verified tutors (5 per campus), password  StudyLink@2026
       Alice        students: lwazi.mbatha, siphokazi.ntshona, anele.gqiba, luthando.mxenge, zintle.bokwe,
                              yonela.mafani, bongani.sigcau, asanda.mtshali, khanyisa.ndamase, thabo.radebe
                    tutors:   zola.ngcobo, nosipho.mabandla, mandla.zwane, ayanda.mqhayi, siyabonga.cele
       East London  students: sinethemba.jacobs, lukhanyo.mavuso, ayabonga.tyali, nandipha.booi, odwa.nkosi,
                              inam.sityebi, lithemba.peter, ziyanda.mkhize, kamva.ndlela, unathi.gaba
                    tutors:   lindiwe.dube, kagiso.molefe, thandeka.zulu, sizwe.mthembu, bulelwa.nqweniso
       Bisho        students: sisipho.makana, mihlali.jonas, athenkosi.dyantyi, qhama.mpofu, nomvula.hani,
                              esona.mgudlwa, lubabalo.faku, okuhle.sondlo, sandile.qwesha, zukisa.nompula
                    tutors:   xolani.mbele, noluthando.gusha, vuyo.tshangela, phumza.matiwane, lutho.sokutu
       Existing accounts keep their passwords. Existing students and tutors created after the start of the
       history window are back-dated to just before it, so no activity predates an account.
     * 20 more book titles with copies at all three campuses, and extra copies of the existing titles.
     * For every verified tutor and every student (old and new), eight weeks back and two weeks ahead:
       availability, bookings (completed, cancelled, declined, expired, confirmed, awaiting tutor, awaiting
       payment), attendance, postponements (approved, declined, withdrawn, pending), reviews, weekly
       timesheets (approved, returned, submitted, draft), fees, card payments and refunds.
     * Rentals (returned, late with fines paid / waived / outstanding, damaged, lost, renewed, active,
       overdue, cancelled, awaiting payment), book sales, waitlists, supplier orders and deliveries,
       support messages, password-reset requests, notifications and the daily business metrics.
   ===================================================================================================== */
SET NOCOUNT ON;
SET XACT_ABORT ON;
-- The filtered unique indexes (one active booking per slot, one active rental per copy ...) need these;
-- sqlcmd turns QUOTED_IDENTIFIER off unless it is started with -I.
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;

IF OBJECT_ID(N'dbo.Bookings') IS NULL OR OBJECT_ID(N'dbo.TutorAvailability') IS NULL
BEGIN
    RAISERROR(N'The StudyLink tables are missing. Run database\StudyLink.sql first.', 16, 1);
    RETURN;
END;

IF EXISTS (SELECT 1 FROM dbo.AuditLogs WHERE Action = N'HistorySeeded') OR EXISTS (SELECT 1 FROM dbo.Users WHERE UserName = N'lwazi.mbatha')
BEGIN
    PRINT N'StudyLink sample data is already in this database. Nothing was changed.';
    RETURN;
END;

/* ---------- Lookups and the people who "did" things --------------------------------------------- */
DECLARE @Alice int = (SELECT CampusId FROM dbo.Campuses WHERE Name = N'Alice');
DECLARE @EL    int = (SELECT CampusId FROM dbo.Campuses WHERE Name = N'East London');
DECLARE @Bisho int = (SELECT CampusId FROM dbo.Campuses WHERE Name = N'Bisho');
DECLARE @RoleStudent int = (SELECT RoleId FROM dbo.Roles WHERE Name = N'Student');
DECLARE @RoleTutor   int = (SELECT RoleId FROM dbo.Roles WHERE Name = N'Tutor');

DECLARE @StaffA int = (SELECT TOP 1 u.UserId FROM dbo.Staff s JOIN dbo.Users u ON u.UserId = s.UserId
                       WHERE u.IsActive = 1 ORDER BY CASE WHEN u.CampusId = @Alice THEN 0 ELSE 1 END, s.StaffId);
DECLARE @StaffE int = ISNULL((SELECT TOP 1 u.UserId FROM dbo.Staff s JOIN dbo.Users u ON u.UserId = s.UserId
                              WHERE u.IsActive = 1 AND u.CampusId = @EL ORDER BY s.StaffId), @StaffA);
DECLARE @StaffIdA int = (SELECT StaffId FROM dbo.Staff WHERE UserId = @StaffA);
DECLARE @StaffIdE int = (SELECT StaffId FROM dbo.Staff WHERE UserId = @StaffE);
DECLARE @MgrUser int = (SELECT TOP 1 u.UserId FROM dbo.Managers m JOIN dbo.Users u ON u.UserId = m.UserId
                        WHERE u.IsActive = 1 ORDER BY m.ManagerId);
DECLARE @MgrId int = (SELECT ManagerId FROM dbo.Managers WHERE UserId = @MgrUser);
DECLARE @Sup1 int = (SELECT TOP 1 SupplierId FROM dbo.Suppliers ORDER BY SupplierId);
DECLARE @Sup2 int = ISNULL((SELECT TOP 1 SupplierId FROM dbo.Suppliers WHERE SupplierId <> @Sup1 ORDER BY SupplierId), @Sup1);

IF @Alice IS NULL OR @EL IS NULL OR @Bisho IS NULL OR @RoleStudent IS NULL OR @RoleTutor IS NULL
   OR @StaffA IS NULL OR @MgrUser IS NULL OR @Sup1 IS NULL
BEGIN
    RAISERROR(N'SeedData.sql needs the three campuses plus at least one Staff, Manager and Supplier account. Start the site once with the demo seed (or create those accounts) and run it again.', 16, 1);
    RETURN;
END;

/* ---------- The history window --------------------------------------------------------------- */
DECLARE @Now       datetime2(0) = SYSDATETIME();
DECLARE @Today     date = CAST(@Now AS date);
DECLARE @WeekStart date = DATEADD(day, -(DATEDIFF(day, '19000101', @Today) % 7), @Today); -- 1 Jan 1900 was a Monday
DECLARE @From      date = DATEADD(week, -8, @WeekStart);                                    -- eight weeks back
DECLARE @Until     date = DATEADD(day, 14, @Today);                                         -- two weeks ahead
DECLARE @Dash      nchar(1) = NCHAR(8211);                                                  -- the en dash the app uses

-- PBKDF2-SHA256 (100 000 iterations) of "StudyLink@2026", made with StudyLink.Services.Security.PasswordHasher.
DECLARE @PwSalt varbinary(32) = 0x60C31DB80FA6D8160C1FE1880C129DB1;
DECLARE @PwHash varbinary(64) = 0x6B213A02F5E0427534A295167F7A449F4F3A2A2948160D1AED3EB6AA842096DC;
DECLARE @PwIter int = 100000;

/* ---------- Scratch tables (dropped first so a failed run can simply be re-run in SSMS) ---------- */
DROP TABLE IF EXISTS #N, #H, #Code, #NewStu, #NewTut, #NewBook, #Tut, #TS, #Stu, #Day, #Slot, #SlotMap, #B, #PP, #Rv,
                     #PO, #POL, #Bk, #Cs, #BC, #Req, #RO, #SO, #F, #FMap, #P, #TW;

CREATE TABLE #N (n int PRIMARY KEY);
;WITH d AS (SELECT v FROM (VALUES (0), (1), (2), (3), (4), (5), (6), (7), (8), (9)) t(v))
INSERT #N (n)
SELECT a.v + 10 * b.v + 100 * c.v + 1000 * e.v + 10000 * f.v
FROM d a CROSS JOIN d b CROSS JOIN d c CROSS JOIN d e CROSS JOIN (SELECT v FROM d WHERE v <= 2) f;   -- 0 .. 29 999

-- Deterministic reference codes in the app's alphabet (no 0/O/1/I): BK-yyMMdd-XXXXXX and friends.
DECLARE @Abc char(32) = 'ABCDEFGHJKLMNPQRSTUVWXYZ23456789';
CREATE TABLE #H (n int PRIMARY KEY, h varbinary(32));
INSERT #H (n, h) SELECT n, HASHBYTES('SHA2_256', CONCAT('studylink-seed-', n)) FROM #N WHERE n BETWEEN 1 AND 22000;   -- one hash per code
CREATE TABLE #Code (n int PRIMARY KEY, c6 char(6), c12 char(12));
INSERT #Code (n, c6, c12)
SELECT x.n,
       CONCAT(SUBSTRING(@Abc, CAST(SUBSTRING(x.h,  1, 1) AS int) % 32 + 1, 1), SUBSTRING(@Abc, CAST(SUBSTRING(x.h,  2, 1) AS int) % 32 + 1, 1),
              SUBSTRING(@Abc, CAST(SUBSTRING(x.h,  3, 1) AS int) % 32 + 1, 1), SUBSTRING(@Abc, CAST(SUBSTRING(x.h,  4, 1) AS int) % 32 + 1, 1),
              SUBSTRING(@Abc, CAST(SUBSTRING(x.h,  5, 1) AS int) % 32 + 1, 1), SUBSTRING(@Abc, CAST(SUBSTRING(x.h,  6, 1) AS int) % 32 + 1, 1)),
       CONCAT(SUBSTRING(@Abc, CAST(SUBSTRING(x.h,  7, 1) AS int) % 32 + 1, 1), SUBSTRING(@Abc, CAST(SUBSTRING(x.h,  8, 1) AS int) % 32 + 1, 1),
              SUBSTRING(@Abc, CAST(SUBSTRING(x.h,  9, 1) AS int) % 32 + 1, 1), SUBSTRING(@Abc, CAST(SUBSTRING(x.h, 10, 1) AS int) % 32 + 1, 1),
              SUBSTRING(@Abc, CAST(SUBSTRING(x.h, 11, 1) AS int) % 32 + 1, 1), SUBSTRING(@Abc, CAST(SUBSTRING(x.h, 12, 1) AS int) % 32 + 1, 1),
              SUBSTRING(@Abc, CAST(SUBSTRING(x.h, 13, 1) AS int) % 32 + 1, 1), SUBSTRING(@Abc, CAST(SUBSTRING(x.h, 14, 1) AS int) % 32 + 1, 1),
              SUBSTRING(@Abc, CAST(SUBSTRING(x.h, 15, 1) AS int) % 32 + 1, 1), SUBSTRING(@Abc, CAST(SUBSTRING(x.h, 16, 1) AS int) % 32 + 1, 1),
              SUBSTRING(@Abc, CAST(SUBSTRING(x.h, 17, 1) AS int) % 32 + 1, 1), SUBSTRING(@Abc, CAST(SUBSTRING(x.h, 18, 1) AS int) % 32 + 1, 1))
FROM #H x;

BEGIN TRANSACTION;

/* =====================================================================================================
   1. People
   ===================================================================================================== */

-- Existing students and tutors get history from the start of the window, so their accounts must be older.
DECLARE @Backdate datetime2(0) = DATEADD(hour, 9, CAST(DATEADD(day, -10, @From) AS datetime2(0)));
UPDATE u SET CreatedAt = @Backdate
FROM dbo.Users u
WHERE u.CreatedAt > @Backdate
  AND EXISTS (SELECT 1 FROM dbo.UserRoles ur WHERE ur.UserId = u.UserId AND ur.RoleId IN (@RoleStudent, @RoleTutor));
UPDATE dbo.UserRoles SET AssignedAt = @Backdate WHERE AssignedAt > @Backdate AND RoleId IN (@RoleStudent, @RoleTutor);
UPDATE dbo.Students SET CreatedAt = @Backdate WHERE CreatedAt > @Backdate;
UPDATE dbo.Tutors SET CreatedAt = @Backdate WHERE CreatedAt > @Backdate;
UPDATE dbo.Tutors SET VerifiedAt = DATEADD(day, 1, @Backdate) WHERE VerificationStatus = 2 AND VerifiedAt > DATEADD(day, 1, @Backdate);
UPDATE dbo.TutorVerifications
SET SubmittedAt = CASE WHEN SubmittedAt > @Backdate THEN @Backdate ELSE SubmittedAt END,
    ReviewedAt  = CASE WHEN ReviewedAt > DATEADD(day, 1, @Backdate) THEN DATEADD(day, 1, @Backdate) ELSE ReviewedAt END
WHERE Decision = 2;

CREATE TABLE #NewStu (n int PRIMARY KEY, UserName nvarchar(50), FirstName nvarchar(50), LastName nvarchar(50), Gender int,
                      Campus nvarchar(50), Lvl nvarchar(50), Faculty nvarchar(100), S1 nvarchar(100), S2 nvarchar(100), S3 nvarchar(100));
INSERT #NewStu VALUES
 ( 1, N'lwazi.mbatha',      N'Lwazi',      N'Mbatha',   1, N'Alice',       N'First Year',  N'Management & Commerce',        N'Accounting',          N'Economics',           N'Statistics'),
 ( 2, N'siphokazi.ntshona', N'Siphokazi',  N'Ntshona',  2, N'Alice',       N'Second Year', N'Science & Agriculture',        N'Mathematics',         N'Computer Science',    N'Statistics'),
 ( 3, N'anele.gqiba',       N'Anele',      N'Gqiba',    2, N'Alice',       N'Third Year',  N'Law',                          N'Law',                 N'English',             NULL),
 ( 4, N'luthando.mxenge',   N'Luthando',   N'Mxenge',   1, N'Alice',       N'First Year',  N'Science & Agriculture',        N'Computer Science',    N'Information Systems', N'Mathematics'),
 ( 5, N'zintle.bokwe',      N'Zintle',     N'Bokwe',    2, N'Alice',       N'Second Year', N'Social Sciences & Humanities', N'Psychology',          N'English',             NULL),
 ( 6, N'yonela.mafani',     N'Yonela',     N'Mafani',   2, N'Alice',       N'First Year',  N'Management & Commerce',        N'Business Management', N'Accounting',          N'Economics'),
 ( 7, N'bongani.sigcau',    N'Bongani',    N'Sigcau',   1, N'Alice',       N'Third Year',  N'Management & Commerce',        N'Economics',           N'Statistics',          NULL),
 ( 8, N'asanda.mtshali',    N'Asanda',     N'Mtshali',  2, N'Alice',       N'Honours',     N'Education',                    N'Mathematics',         N'English',             N'Psychology'),
 ( 9, N'khanyisa.ndamase',  N'Khanyisa',   N'Ndamase',  2, N'Alice',       N'First Year',  N'Law',                          N'Law',                 N'Economics',           NULL),
 (10, N'thabo.radebe',      N'Thabo',      N'Radebe',   1, N'Alice',       N'Second Year', N'Science & Agriculture',        N'Statistics',          N'Mathematics',         NULL),
 (11, N'sinethemba.jacobs', N'Sinethemba', N'Jacobs',   2, N'East London', N'First Year',  N'Management & Commerce',        N'Accounting',          N'Business Management', NULL),
 (12, N'lukhanyo.mavuso',   N'Lukhanyo',   N'Mavuso',   1, N'East London', N'Second Year', N'Science & Agriculture',        N'Information Systems', N'Computer Science',    N'Statistics'),
 (13, N'ayabonga.tyali',    N'Ayabonga',   N'Tyali',    1, N'East London', N'Third Year',  N'Management & Commerce',        N'Economics',           N'Statistics',          N'Accounting'),
 (14, N'nandipha.booi',     N'Nandipha',   N'Booi',     2, N'East London', N'First Year',  N'Education',                    N'English',             N'Mathematics',         NULL),
 (15, N'odwa.nkosi',        N'Odwa',       N'Nkosi',    1, N'East London', N'Second Year', N'Law',                          N'Law',                 N'English',             NULL),
 (16, N'inam.sityebi',      N'Inam',       N'Sityebi',  2, N'East London', N'First Year',  N'Social Sciences & Humanities', N'Psychology',          N'English',             NULL),
 (17, N'lithemba.peter',    N'Lithemba',   N'Peter',    1, N'East London', N'Honours',     N'Management & Commerce',        N'Business Management', N'Economics',           NULL),
 (18, N'ziyanda.mkhize',    N'Ziyanda',    N'Mkhize',   2, N'East London', N'Second Year', N'Science & Agriculture',        N'Mathematics',         N'Statistics',          N'Computer Science'),
 (19, N'kamva.ndlela',      N'Kamva',      N'Ndlela',   1, N'East London', N'First Year',  N'Science & Agriculture',        N'Computer Science',    N'Mathematics',         NULL),
 (20, N'unathi.gaba',       N'Unathi',     N'Gaba',     2, N'East London', N'Third Year',  N'Management & Commerce',        N'Accounting',          N'Statistics',          NULL),
 (21, N'sisipho.makana',    N'Sisipho',    N'Makana',   2, N'Bisho',       N'First Year',  N'Management & Commerce',        N'Business Management', N'Economics',           NULL),
 (22, N'mihlali.jonas',     N'Mihlali',    N'Jonas',    2, N'Bisho',       N'Second Year', N'Law',                          N'Law',                 N'English',             NULL),
 (23, N'athenkosi.dyantyi', N'Athenkosi',  N'Dyantyi',  1, N'Bisho',       N'First Year',  N'Science & Agriculture',        N'Mathematics',         N'Statistics',          NULL),
 (24, N'qhama.mpofu',       N'Qhama',      N'Mpofu',    1, N'Bisho',       N'Third Year',  N'Management & Commerce',        N'Accounting',          N'Economics',           N'Business Management'),
 (25, N'nomvula.hani',      N'Nomvula',    N'Hani',     2, N'Bisho',       N'Second Year', N'Social Sciences & Humanities', N'Psychology',          N'English',             NULL),
 (26, N'esona.mgudlwa',     N'Esona',      N'Mgudlwa',  2, N'Bisho',       N'First Year',  N'Education',                    N'English',             N'Psychology',          N'Mathematics'),
 (27, N'lubabalo.faku',     N'Lubabalo',   N'Faku',     1, N'Bisho',       N'Second Year', N'Management & Commerce',        N'Economics',           N'Accounting',          NULL),
 (28, N'okuhle.sondlo',     N'Okuhle',     N'Sondlo',   2, N'Bisho',       N'Honours',     N'Law',                          N'Law',                 N'Economics',           NULL),
 (29, N'sandile.qwesha',    N'Sandile',    N'Qwesha',   1, N'Bisho',       N'First Year',  N'Science & Agriculture',        N'Information Systems', N'Mathematics',         NULL),
 (30, N'zukisa.nompula',    N'Zukisa',     N'Nompula',  2, N'Bisho',       N'Third Year',  N'Management & Commerce',        N'Statistics',          N'Accounting',          NULL);

INSERT dbo.Users (UserName, Email, FirstName, LastName, Gender, CellNumber, CampusId, PasswordHash, PasswordSalt, HashIterations,
                  IsActive, FailedLoginCount, LockoutEnd, MustChangePassword, LastLoginAt, CreatedAt, UpdatedAt, CreatedByUserId)
SELECT s.UserName, s.UserName + N'@studylink.local', s.FirstName, s.LastName, s.Gender,
       CONCAT(N'06', RIGHT(CONCAT('00000000', 31000000 + s.n * 104729), 8)), c.CampusId, @PwHash, @PwSalt, @PwIter,
       1, 0, NULL, 0, DATEADD(minute, -(s.n * 97 % 7000), @Now),
       DATEADD(minute, 480 + s.n * 17, CAST(DATEADD(day, -(10 + s.n % 8), @From) AS datetime2(0))), NULL, NULL
FROM #NewStu s JOIN dbo.Campuses c ON c.Name = s.Campus;

INSERT dbo.UserRoles (UserId, RoleId, AssignedAt, AssignedByUserId)
SELECT u.UserId, @RoleStudent, u.CreatedAt, NULL FROM #NewStu s JOIN dbo.Users u ON u.UserName = s.UserName;

INSERT dbo.Students (UserId, StudentNumber, StudyLevelId, Faculty, CreatedAt)
SELECT u.UserId,
       CONCAT('S', CASE s.Lvl WHEN N'First Year' THEN 2026 WHEN N'Second Year' THEN 2025 WHEN N'Third Year' THEN 2024 ELSE 2023 END, 41000 + s.n * 37),
       l.StudyLevelId, s.Faculty, u.CreatedAt
FROM #NewStu s JOIN dbo.Users u ON u.UserName = s.UserName JOIN dbo.StudyLevels l ON l.Name = s.Lvl;

INSERT dbo.StudentSubjects (StudentId, SubjectId, StudyLevelId, CreatedAt)
SELECT st.StudentId, sj.SubjectId, st.StudyLevelId, st.CreatedAt
FROM #NewStu s
JOIN dbo.Users u ON u.UserName = s.UserName
JOIN dbo.Students st ON st.UserId = u.UserId
CROSS APPLY (VALUES (s.S1), (s.S2), (s.S3)) v(Subj)
JOIN dbo.Subjects sj ON sj.Name = v.Subj;

CREATE TABLE #NewTut (n int PRIMARY KEY, UserName nvarchar(50), FirstName nvarchar(50), LastName nvarchar(50), Gender int, Campus nvarchar(50),
                      Qual nvarchar(100), Years int, Rate decimal(18,2), S1 nvarchar(100), L1 nvarchar(50), S2 nvarchar(100), L2 nvarchar(50),
                      S3 nvarchar(100), L3 nvarchar(50), Bio nvarchar(1000));
INSERT #NewTut VALUES
 ( 1, N'zola.ngcobo',       N'Zola',       N'Ngcobo',    1, N'Alice',       N'BSc Mathematics (Hons)',      5,  90, N'Mathematics', N'First Year', N'Statistics', N'Second Year', N'Computer Science', N'First Year',
      N'Zola makes first-year calculus feel manageable, one worked example at a time.'),
 ( 2, N'nosipho.mabandla',  N'Nosipho',    N'Mabandla',  2, N'Alice',       N'BCom Accounting (Hons)',      4,  85, N'Accounting', N'First Year', N'Accounting', N'Second Year', N'Economics', N'First Year',
      N'Nosipho tutors financial accounting and loves turning trial balances into something that makes sense.'),
 ( 3, N'mandla.zwane',      N'Mandla',     N'Zwane',     1, N'Alice',       N'LLB',                         6, 110, N'Law', N'First Year', N'Law', N'Second Year', N'English', N'First Year',
      N'Mandla helps law students read cases quickly and write clear, well-structured answers.'),
 ( 4, N'ayanda.mqhayi',     N'Ayanda',     N'Mqhayi',    2, N'Alice',       N'MA Psychology',               3,  75, N'Psychology', N'First Year', N'Psychology', N'Second Year', NULL, NULL,
      N'Ayanda covers introductory and developmental psychology, with a focus on essay planning.'),
 ( 5, N'siyabonga.cele',    N'Siyabonga',  N'Cele',      1, N'Alice',       N'BSc Computer Science',        4,  95, N'Computer Science', N'Second Year', N'Information Systems', N'Second Year', N'Mathematics', N'First Year',
      N'Siyabonga tutors programming in Java and Python and debugging techniques that save hours.'),
 ( 6, N'lindiwe.dube',      N'Lindiwe',    N'Dube',      2, N'East London', N'BCom Economics (Hons)',       5,  90, N'Economics', N'Second Year', N'Economics', N'First Year', N'Business Management', N'First Year',
      N'Lindiwe explains micro- and macroeconomics with South African examples students can relate to.'),
 ( 7, N'kagiso.molefe',     N'Kagiso',     N'Molefe',    1, N'East London', N'BSc Information Systems',     3,  80, N'Information Systems', N'First Year', N'Computer Science', N'First Year', NULL, NULL,
      N'Kagiso helps with databases, SQL and systems analysis projects.'),
 ( 8, N'thandeka.zulu',     N'Thandeka',   N'Zulu',      2, N'East London', N'BEd Senior Phase',            7,  70, N'English', N'First Year', N'Mathematics', N'First Year', NULL, NULL,
      N'Thandeka is an experienced teacher who supports academic writing and foundation mathematics.'),
 ( 9, N'sizwe.mthembu',     N'Sizwe',      N'Mthembu',   1, N'East London', N'MCom Statistics',             6, 120, N'Statistics', N'Second Year', N'Statistics', N'Third Year', N'Mathematics', N'Second Year',
      N'Sizwe tutors inferential statistics, regression and research methods.'),
 (10, N'bulelwa.nqweniso',  N'Bulelwa',    N'Nqweniso',  2, N'East London', N'BCom Business Management',    2,  65, N'Business Management', N'First Year', N'Accounting', N'First Year', NULL, NULL,
      N'Bulelwa helps first-years with business management case studies and basic bookkeeping.'),
 (11, N'xolani.mbele',      N'Xolani',     N'Mbele',     1, N'Bisho',       N'BAdmin Public Administration',4,  70, N'Business Management', N'Second Year', N'Economics', N'First Year', NULL, NULL,
      N'Xolani works with students on management theory and economics assignments.'),
 (12, N'noluthando.gusha',  N'Noluthando', N'Gusha',     2, N'Bisho',       N'BSc Mathematics',             5,  85, N'Mathematics', N'First Year', N'Mathematics', N'Second Year', N'Statistics', N'First Year',
      N'Noluthando tutors algebra, calculus and first-year statistics.'),
 (13, N'vuyo.tshangela',    N'Vuyo',       N'Tshangela', 1, N'Bisho',       N'LLB',                         3, 100, N'Law', N'First Year', N'Law', N'Third Year', NULL, NULL,
      N'Vuyo supports students in contract, criminal and constitutional law.'),
 (14, N'phumza.matiwane',   N'Phumza',     N'Matiwane',  2, N'Bisho',       N'BA English and Psychology',   4,  60, N'English', N'First Year', N'Psychology', N'First Year', NULL, NULL,
      N'Phumza helps with academic reading, essay writing and referencing.'),
 (15, N'lutho.sokutu',      N'Lutho',      N'Sokutu',    1, N'Bisho',       N'BCom Accounting',             3,  80, N'Accounting', N'First Year', N'Accounting', N'Third Year', N'Economics', N'Second Year',
      N'Lutho tutors accounting from journal entries all the way to group statements.');

INSERT dbo.Users (UserName, Email, FirstName, LastName, Gender, CellNumber, CampusId, PasswordHash, PasswordSalt, HashIterations,
                  IsActive, FailedLoginCount, LockoutEnd, MustChangePassword, LastLoginAt, CreatedAt, UpdatedAt, CreatedByUserId)
SELECT t.UserName, t.UserName + N'@studylink.local', t.FirstName, t.LastName, t.Gender,
       CONCAT(N'07', RIGHT(CONCAT('00000000', 22000000 + t.n * 130363), 8)), c.CampusId, @PwHash, @PwSalt, @PwIter,
       1, 0, NULL, 0, DATEADD(minute, -(t.n * 131 % 5000), @Now),
       DATEADD(minute, 540 + t.n * 23, CAST(DATEADD(day, -(12 + t.n % 6), @From) AS datetime2(0))), NULL, NULL
FROM #NewTut t JOIN dbo.Campuses c ON c.Name = t.Campus;

INSERT dbo.UserRoles (UserId, RoleId, AssignedAt, AssignedByUserId)
SELECT u.UserId, @RoleTutor, u.CreatedAt, NULL FROM #NewTut t JOIN dbo.Users u ON u.UserName = t.UserName;

INSERT dbo.Tutors (UserId, Qualification, ExperienceYears, HourlyRate, Bio, VerificationStatus, VerifiedAt, CreatedAt)
SELECT u.UserId, t.Qual, t.Years, t.Rate, t.Bio, 2, DATEADD(day, 2, u.CreatedAt), u.CreatedAt
FROM #NewTut t JOIN dbo.Users u ON u.UserName = t.UserName;

INSERT dbo.TutorSubjects (TutorId, SubjectId, StudyLevelId, CreatedAt)
SELECT tu.TutorId, sj.SubjectId, l.StudyLevelId, tu.CreatedAt
FROM #NewTut t
JOIN dbo.Users u ON u.UserName = t.UserName
JOIN dbo.Tutors tu ON tu.UserId = u.UserId
CROSS APPLY (VALUES (t.S1, t.L1), (t.S2, t.L2), (t.S3, t.L3)) v(Subj, Lvl)
JOIN dbo.Subjects sj ON sj.Name = v.Subj
JOIN dbo.StudyLevels l ON l.Name = v.Lvl;

INSERT dbo.TutorVerifications (TutorId, SubmittedAt, Decision, ReviewedByUserId, ReviewedByStaffId, ReviewedAt, IdentityChecked, QualificationChecked, Notes)
SELECT tu.TutorId, tu.CreatedAt, 2,
       CASE WHEN u.CampusId = @EL THEN @StaffE ELSE @StaffA END,
       CASE WHEN u.CampusId = @EL THEN @StaffIdE ELSE @StaffIdA END,
       tu.VerifiedAt, 1, 1, CONCAT(N'Certified ID and qualification checked at the ', t.Campus, N' hub.')
FROM #NewTut t JOIN dbo.Users u ON u.UserName = t.UserName JOIN dbo.Tutors tu ON tu.UserId = u.UserId;

/* Pools used by everything below: every verified tutor and every student, old and new. */
CREATE TABLE #Tut (TutorId int PRIMARY KEY, UserId int, UserName nvarchar(50), CampusId int, Rate decimal(18,2), Name nvarchar(101),
                   Created datetime2(0), SubjCnt int);
INSERT #Tut
SELECT t.TutorId, t.UserId, u.UserName, u.CampusId, t.HourlyRate, u.FirstName + N' ' + u.LastName, t.CreatedAt,
       (SELECT COUNT(*) FROM dbo.TutorSubjects x WHERE x.TutorId = t.TutorId)
FROM dbo.Tutors t JOIN dbo.Users u ON u.UserId = t.UserId
WHERE t.VerificationStatus = 2 AND u.IsActive = 1 AND EXISTS (SELECT 1 FROM dbo.TutorSubjects x WHERE x.TutorId = t.TutorId);

CREATE TABLE #TS (TutorSubjectId int PRIMARY KEY, TutorId int, Subject nvarchar(100), rn int);
INSERT #TS
SELECT ts.TutorSubjectId, ts.TutorId, sj.Name, ROW_NUMBER() OVER (PARTITION BY ts.TutorId ORDER BY ts.TutorSubjectId)
FROM dbo.TutorSubjects ts JOIN dbo.Subjects sj ON sj.SubjectId = ts.SubjectId JOIN #Tut t ON t.TutorId = ts.TutorId;

CREATE TABLE #Stu (StudentId int PRIMARY KEY, UserId int, CampusId int, Name nvarchar(101), Brand nvarchar(20), Last4 char(4));
INSERT #Stu
SELECT s.StudentId, s.UserId, u.CampusId, u.FirstName + N' ' + u.LastName,
       CASE WHEN s.StudentId % 3 = 0 THEN N'Mastercard' ELSE N'Visa' END,
       RIGHT(CONCAT('0000', CAST(SUBSTRING(HASHBYTES('MD5', CONCAT('card', s.StudentId)), 1, 4) AS bigint) % 10000), 4)
FROM dbo.Students s JOIN dbo.Users u ON u.UserId = s.UserId
WHERE u.IsActive = 1;

/* =====================================================================================================
   2. Books and copies
   ===================================================================================================== */
CREATE TABLE #NewBook (n int PRIMARY KEY, Isbn nvarchar(20), Title nvarchar(200), Authors nvarchar(200), Edition nvarchar(30), Publisher nvarchar(100),
                       Yr int, Category nvarchar(80), Subject nvarchar(100), Price decimal(18,2), Weekly decimal(18,2), Late decimal(18,2),
                       Reorder int, CA int, CE int, CB int);
INSERT #NewBook VALUES
 ( 1, N'9781119448952', N'Accounting Principles', N'Jerry Weygandt, Paul Kimmel, Donald Kieso', N'13th', N'Wiley', 2018, N'Accounting & Finance', N'Accounting', 920, 60, 12, 3, 3, 2, 2),
 ( 2, N'9781260247824', N'Managerial Accounting', N'Ray Garrison, Eric Noreen, Peter Brewer', N'17th', N'McGraw-Hill', 2021, N'Accounting & Finance', N'Accounting', 880, 60, 12, 2, 2, 2, 1),
 ( 3, N'9780135183793', N'Corporate Finance', N'Jonathan Berk, Peter DeMarzo', N'5th', N'Pearson', 2020, N'Accounting & Finance', N'Accounting', 990, 65, 14, 2, 2, 1, 1),
 ( 4, N'9781319105990', N'Macroeconomics', N'N. Gregory Mankiw', N'10th', N'Worth Publishers', 2019, N'Economics', N'Economics', 760, 55, 10, 3, 3, 2, 2),
 ( 5, N'9780134184241', N'Microeconomics', N'Robert Pindyck, Daniel Rubinfeld', N'9th', N'Pearson', 2018, N'Economics', N'Economics', 810, 55, 10, 2, 2, 2, 1),
 ( 6, N'9780190758622', N'Introduction to Business Management', N'Barney Erasmus, John Strydom, Sonja Rudansky-Kloppers', N'11th', N'Oxford University Press Southern Africa', 2019, N'Business & Management', N'Business Management', 560, 40, 8, 3, 3, 3, 2),
 ( 7, N'9780135886151', N'Marketing Management', N'Philip Kotler, Kevin Lane Keller', N'16th', N'Pearson', 2021, N'Business & Management', N'Business Management', 850, 55, 10, 2, 1, 2, 1),
 ( 8, N'9780262046305', N'Introduction to Algorithms', N'Thomas Cormen, Charles Leiserson, Ronald Rivest, Clifford Stein', N'4th', N'MIT Press', 2022, N'Computer Science & IS', N'Computer Science', 1150, 75, 15, 3, 1, 0, 0),
 ( 9, N'9780134802213', N'Starting Out with Java', N'Tony Gaddis', N'7th', N'Pearson', 2019, N'Computer Science & IS', N'Computer Science', 780, 55, 10, 3, 2, 2, 1),
 (10, N'9781337627900', N'Database Systems: Design, Implementation, and Management', N'Carlos Coronel, Steven Morris', N'13th', N'Cengage', 2018, N'Computer Science & IS', N'Information Systems', 840, 60, 12, 2, 2, 2, 1),
 (11, N'9781119496625', N'Systems Analysis and Design', N'Alan Dennis, Barbara Wixom, Roberta Roth', N'7th', N'Wiley', 2018, N'Computer Science & IS', N'Information Systems', 790, 55, 10, 2, 2, 1, 1),
 (12, N'9780135851159', N'Linear Algebra and Its Applications', N'David Lay, Steven Lay, Judi McDonald', N'6th', N'Pearson', 2021, N'Mathematics & Statistics', N'Mathematics', 870, 60, 12, 2, 2, 1, 1),
 (13, N'9781260091991', N'Discrete Mathematics and Its Applications', N'Kenneth Rosen', N'8th', N'McGraw-Hill', 2019, N'Mathematics & Statistics', N'Mathematics', 930, 65, 12, 3, 0, 1, 0),
 (14, N'9781305251809', N'Probability and Statistics for Engineering and the Sciences', N'Jay Devore', N'9th', N'Cengage', 2016, N'Mathematics & Statistics', N'Statistics', 880, 60, 12, 2, 2, 2, 1),
 (15, N'9780199041862', N'The Law of Contract in South Africa', N'Dale Hutchison, Chris-James Pretorius (eds.)', N'3rd', N'Oxford University Press Southern Africa', 2017, N'Law', N'Law', 690, 45, 10, 2, 2, 1, 2),
 (16, N'9780639110448', N'Criminal Law', N'C.R. Snyman', N'7th', N'LexisNexis', 2020, N'Law', N'Law', 720, 50, 10, 2, 2, 1, 2),
 (17, N'9781319132491', N'Psychology', N'David Myers, C. Nathan DeWall', N'13th', N'Worth Publishers', 2021, N'Humanities & Social Sciences', N'Psychology', 820, 55, 10, 2, 2, 2, 1),
 (18, N'9781319066376', N'Abnormal Psychology', N'Ronald Comer', N'10th', N'Worth Publishers', 2018, N'Humanities & Social Sciences', N'Psychology', 760, 50, 10, 1, 1, 1, 1),
 (19, N'9780131523159', N'Writing Academic English', N'Alice Oshima, Ann Hogue', N'4th', N'Pearson Longman', 2006, N'Humanities & Social Sciences', N'English', 420, 30, 6, 3, 3, 2, 2),
 (20, N'9780135188743', N'Campbell Biology', N'Lisa Urry, Michael Cain, Steven Wasserman', N'12th', N'Pearson', 2020, N'Natural Sciences', NULL, 1050, 70, 14, 2, 2, 1, 1);

DECLARE @BooksAt datetime2(0) = DATEADD(hour, 10, CAST(DATEADD(day, -14, @From) AS datetime2(0)));
INSERT dbo.Books (Isbn, Title, Authors, Edition, Publisher, PublishedYear, CategoryId, SubjectId, Description, CoverImage, CoverContentType,
                  SalePrice, WeeklyRentalFee, LateFeePerDay, ReorderLevel, IsRentable, IsForSale, IsActive, PreferredSupplierId,
                  CreatedByManagerId, UpdatedByManagerId, CreatedAt, UpdatedAt)
SELECT b.Isbn, b.Title, b.Authors, b.Edition, b.Publisher, b.Yr, c.CategoryId, sj.SubjectId,
       CONCAT(N'Prescribed text for ', ISNULL(b.Subject, b.Category), N' modules at Forte Books & Tuition Hub campuses.'), NULL, NULL,
       b.Price, b.Weekly, b.Late, b.Reorder, 1, 1, 1, CASE WHEN b.n % 2 = 0 THEN @Sup2 ELSE @Sup1 END,
       @MgrId, NULL, DATEADD(minute, b.n * 7, @BooksAt), NULL
FROM #NewBook b
JOIN dbo.Categories c ON c.Name = b.Category
LEFT JOIN dbo.Subjects sj ON sj.Name = b.Subject
WHERE NOT EXISTS (SELECT 1 FROM dbo.Books x WHERE x.Isbn = b.Isbn);

-- Opening stock of the new titles.
;WITH want AS (
    SELECT bk.BookId, v.CampusId, v.Qty, bk.CreatedAt
    FROM #NewBook b JOIN dbo.Books bk ON bk.Isbn = b.Isbn
    CROSS APPLY (VALUES (@Alice, b.CA), (@EL, b.CE), (@Bisho, b.CB)) v(CampusId, Qty)
), copies AS (
    SELECT w.BookId, w.CampusId, w.CreatedAt, ROW_NUMBER() OVER (ORDER BY w.BookId, w.CampusId, n.n) AS rn
    FROM want w JOIN (VALUES (1), (2), (3), (4), (5), (6)) n(n) ON n.n <= w.Qty
)
INSERT dbo.BookCopies (BookId, CampusId, Barcode, Condition, Status, AcquiredAt, DeliveryLineId, Notes)
SELECT c.BookId, c.CampusId, CONCAT('SL-', RIGHT(CONCAT('000000', c.BookId), 6), '-', LEFT(k.c6, 5)), 1, 1, c.CreatedAt, NULL, NULL
FROM copies c JOIN #Code k ON k.n = 18000 + c.rn;

-- The original titles get at least two copies at every campus.
;WITH have AS (
    SELECT bk.BookId, cp.CampusId, bk.CreatedAt,
           (SELECT COUNT(*) FROM dbo.BookCopies x WHERE x.BookId = bk.BookId AND x.CampusId = cp.CampusId) AS Cnt
    FROM dbo.Books bk CROSS JOIN (VALUES (@Alice), (@EL), (@Bisho)) cp(CampusId)
    WHERE bk.IsActive = 1 AND NOT EXISTS (SELECT 1 FROM #NewBook nb WHERE nb.Isbn = bk.Isbn)
), copies AS (
    SELECT h.BookId, h.CampusId, ROW_NUMBER() OVER (ORDER BY h.BookId, h.CampusId, n.n) AS rn
    FROM have h JOIN (VALUES (1), (2)) n(n) ON n.n <= 2 - h.Cnt
    WHERE h.Cnt < 2
)
INSERT dbo.BookCopies (BookId, CampusId, Barcode, Condition, Status, AcquiredAt, DeliveryLineId, Notes)
SELECT c.BookId, c.CampusId, CONCAT('SL-', RIGHT(CONCAT('000000', c.BookId), 6), '-', LEFT(k.c6, 5)), 2, 1, @BooksAt, NULL,
       N'Transferred in to cover second-semester demand.'
FROM copies c JOIN #Code k ON k.n = 18500 + c.rn;

/* =====================================================================================================
   3. Supply chain (before rentals, so delivered copies can be rented out)
   ===================================================================================================== */
CREATE TABLE #PO (o int PRIMARY KEY, SupplierId int, OrderedAt datetime2(0), Status int, Notes nvarchar(500), Reference nvarchar(20), OrderId int NULL,
                  DeliveryRef nvarchar(20) NULL, DeliveryId int NULL);
INSERT #PO (o, SupplierId, OrderedAt, Status, Notes) VALUES
 (1, @Sup1, DATEADD(hour, 10, CAST(DATEADD(day,  3, @From) AS datetime2(0))), 4, N'Semester stock for economics and first-year management.'),
 (2, @Sup2, DATEADD(hour, 11, CAST(DATEADD(day, 12, @From) AS datetime2(0))), 4, N'Accounting titles for Bisho and writing guides for Alice.'),
 (3, @Sup1, DATEADD(hour,  9, CAST(DATEADD(day, 24, @From) AS datetime2(0))), 4, N'Mathematics and psychology restock.'),
 (4, @Sup2, DATEADD(hour, 14, CAST(DATEADD(day, 38, @From) AS datetime2(0))), 3, N'Programming and database textbooks; balance to follow.'),
 (5, @Sup1, DATEADD(hour, 10, CAST(DATEADD(day, -6, @Today) AS datetime2(0))), 2, N'Low stock: algorithms and discrete maths.'),
 (6, @Sup2, DATEADD(hour, 15, CAST(DATEADD(day, -1, @Today) AS datetime2(0))), 1, N'Criminal Law copies for Bisho.');
UPDATE p SET Reference = CONCAT('PO-', FORMAT(p.OrderedAt, 'yyMMdd'), '-', k.c6) FROM #PO p JOIN #Code k ON k.n = 17000 + p.o;

INSERT dbo.SupplierOrders (Reference, SupplierId, CreatedByManagerId, Status, OrderedAt, ExpectedDate, AcknowledgedAt, Notes)
SELECT Reference, SupplierId, @MgrId, Status, OrderedAt, DATEADD(day, 10, CAST(OrderedAt AS date)),
       CASE WHEN Status >= 2 THEN DATEADD(hour, 20, OrderedAt) END, Notes
FROM #PO;
UPDATE p SET OrderId = o.OrderId FROM #PO p JOIN dbo.SupplierOrders o ON o.Reference = p.Reference;

CREATE TABLE #POL (o int, Isbn nvarchar(20), CampusId int, Qty int, Delivered int, LineId int NULL, DeliveryLineId int NULL);
INSERT #POL (o, Isbn, CampusId, Qty, Delivered) VALUES
 (1, N'9781319105990', @Alice, 4, 4), (1, N'9780190758622', @EL, 3, 3),
 (2, N'9781119448952', @Bisho, 3, 3), (2, N'9780131523159', @Alice, 4, 4),
 (3, N'9780135851159', @EL, 3, 3),    (3, N'9781319132491', @Bisho, 2, 2),
 (4, N'9780134802213', @Alice, 5, 3), (4, N'9781337627900', @EL, 4, 2),
 (5, N'9780262046305', @Alice, 4, 0), (5, N'9781260091991', @EL, 4, 0),
 (6, N'9780639110448', @Bisho, 3, 0);

INSERT dbo.SupplierOrderLines (OrderId, BookId, CampusId, QuantityOrdered, QuantityDelivered, UnitCost)
SELECT p.OrderId, b.BookId, l.CampusId, l.Qty, l.Delivered, ROUND(b.SalePrice * 0.7, 0)
FROM #POL l JOIN #PO p ON p.o = l.o JOIN dbo.Books b ON b.Isbn = l.Isbn;
UPDATE l SET LineId = ol.LineId
FROM #POL l JOIN #PO p ON p.o = l.o JOIN dbo.Books b ON b.Isbn = l.Isbn
JOIN dbo.SupplierOrderLines ol ON ol.OrderId = p.OrderId AND ol.BookId = b.BookId AND ol.CampusId = l.CampusId;

UPDATE p SET DeliveryRef = CONCAT('DL-', FORMAT(DATEADD(day, 5, p.OrderedAt), 'yyMMdd'), '-', k.c6)
FROM #PO p JOIN #Code k ON k.n = 17100 + p.o
WHERE EXISTS (SELECT 1 FROM #POL l WHERE l.o = p.o AND l.Delivered > 0);

INSERT dbo.Deliveries (Reference, OrderId, Status, DispatchedAt, DispatchedByUserId, ReceivedAt, ReceivedByUserId, Note)
SELECT p.DeliveryRef, p.OrderId, 2, DATEADD(day, 5, p.OrderedAt), su.UserId, DATEADD(hour, 22, DATEADD(day, 5, p.OrderedAt)), @StaffA,
       N'Boxes checked against the delivery note.'
FROM #PO p JOIN dbo.Suppliers su ON su.SupplierId = p.SupplierId
WHERE p.DeliveryRef IS NOT NULL;
UPDATE p SET DeliveryId = d.DeliveryId FROM #PO p JOIN dbo.Deliveries d ON d.Reference = p.DeliveryRef;

INSERT dbo.DeliveryLines (DeliveryId, OrderLineId, Quantity, QuantityAccepted)
SELECT p.DeliveryId, l.LineId, l.Delivered, l.Delivered FROM #POL l JOIN #PO p ON p.o = l.o WHERE l.Delivered > 0;
UPDATE l SET DeliveryLineId = dl.DeliveryLineId
FROM #POL l JOIN #PO p ON p.o = l.o JOIN dbo.DeliveryLines dl ON dl.DeliveryId = p.DeliveryId AND dl.OrderLineId = l.LineId;

;WITH copies AS (
    SELECT ol.BookId, l.CampusId, l.DeliveryLineId, d.ReceivedAt, p.Reference,
           ROW_NUMBER() OVER (ORDER BY l.DeliveryLineId, n.n) AS rn
    FROM #POL l JOIN #PO p ON p.o = l.o
    JOIN dbo.SupplierOrderLines ol ON ol.LineId = l.LineId
    JOIN dbo.Deliveries d ON d.DeliveryId = p.DeliveryId
    JOIN (VALUES (1), (2), (3), (4), (5), (6)) n(n) ON n.n <= l.Delivered
    WHERE l.Delivered > 0
)
INSERT dbo.BookCopies (BookId, CampusId, Barcode, Condition, Status, AcquiredAt, DeliveryLineId, Notes)
SELECT c.BookId, c.CampusId, CONCAT('SL-', RIGHT(CONCAT('000000', c.BookId), 6), '-', LEFT(k.c6, 5)), 1, 1, c.ReceivedAt, c.DeliveryLineId,
       CONCAT(N'Received on order ', c.Reference)
FROM copies c JOIN #Code k ON k.n = 19000 + c.rn;

/* =====================================================================================================
   4. Tutoring: availability and bookings
   ===================================================================================================== */
CREATE TABLE #Day (D date PRIMARY KEY);
INSERT #Day SELECT DATEADD(day, n, @From) FROM #N WHERE n <= DATEDIFF(day, @From, @Until);

CREATE TABLE #Slot (SlotKey int IDENTITY PRIMARY KEY, TutorId int, TutorSubjectId int, CampusId int, D date, StartT time(0), EndT time(0),
                    StartAt datetime2(0), EndAt datetime2(0), CreatedAt datetime2(0), Hrs decimal(5,2), Past bit, IsBooked bit NOT NULL DEFAULT 0,
                    AvailabilityId int NULL);

INSERT #Slot (TutorId, TutorSubjectId, CampusId, D, StartT, EndT, StartAt, EndAt, CreatedAt, Hrs, Past)
SELECT x.TutorId, ts.TutorSubjectId, x.CampusId, x.D, TIMEFROMPARTS(x.Hr, 0, 0, 0, 0), TIMEFROMPARTS(x.Hr + x.Len, 0, 0, 0, 0),
       x.StartAt, DATEADD(hour, x.Len, x.StartAt),
       GREATEST(DATEADD(hour, 1, x.Created), LEAST(DATEADD(hour, 18, CAST(DATEADD(day, -(2 + x.H % 6), x.D) AS datetime2(0))), DATEADD(hour, -3, @Now))),
       x.Len, CASE WHEN x.D < @Today THEN 1 ELSE 0 END
FROM (
    SELECT t.TutorId, t.CampusId, t.Created, t.SubjCnt, d.D, h.Hr,
           DATEADD(hour, h.Hr, CAST(d.D AS datetime2(0))) AS StartAt,
           CAST(SUBSTRING(HASHBYTES('MD5', CONCAT('slot', t.TutorId, '|', d.D, '|', h.Hr)), 1, 4) AS bigint) AS H,
           CASE WHEN h.Hr IN (8, 13) AND CAST(SUBSTRING(HASHBYTES('MD5', CONCAT('len', t.TutorId, '|', d.D, '|', h.Hr)), 1, 4) AS bigint) % 5 = 0
                THEN 2 ELSE 1 END AS Len
    FROM #Tut t CROSS JOIN #Day d CROSS JOIN (VALUES (8), (10), (13), (15)) h(Hr)
    WHERE DATEDIFF(day, '19000101', d.D) % 7 <> 6                                   -- no Sundays
      AND d.D > CAST(t.Created AS date)
) x
CROSS APPLY (SELECT TOP 1 TutorSubjectId FROM #TS WHERE TutorId = x.TutorId
             ORDER BY HASHBYTES('MD5', CONCAT('subj', x.TutorId, '|', x.D, '|', x.Hr, '|', TutorSubjectId))) ts
WHERE x.H % 100 < 30
  AND (x.D < @Today OR x.StartAt > DATEADD(hour, 2, @Now))
  AND NOT EXISTS (SELECT 1 FROM dbo.TutorAvailability a JOIN dbo.TutorSubjects s ON s.TutorSubjectId = a.TutorSubjectId
                  WHERE s.TutorId = x.TutorId AND a.AvailabilityDate = x.D
                    AND a.StartTime < TIMEFROMPARTS(x.Hr + x.Len, 0, 0, 0, 0) AND TIMEFROMPARTS(x.Hr, 0, 0, 0, 0) < a.EndTime);

CREATE TABLE #B (BKey int IDENTITY PRIMARY KEY, SlotKey int, TutorId int, StudentId int, TutorSubjectId int, CampusId int, D date,
                 StartT time(0), EndT time(0), StartAt datetime2(0), EndAt datetime2(0), Hrs decimal(5,2), Fee decimal(18,2), Past bit,
                 Bucket int NULL, Status int NULL, CreatedAt datetime2(0) NULL, HoldExpiresAt datetime2(0) NULL, PaidAt datetime2(0) NULL,
                 RespondedAt datetime2(0) NULL, DeclineReason nvarchar(200) NULL, CancelledAt datetime2(0) NULL, CancelledBy int NULL,
                 CancelCat int NULL, CancelReason nvarchar(200) NULL, CompletedAt datetime2(0) NULL, Msg nvarchar(500) NULL,
                 Attendance int NULL, Notes nvarchar(1000) NULL, Reference nvarchar(20) NULL, BookingId int NULL,
                 Subject nvarchar(100), TutorName nvarchar(101), SlotCreated datetime2(0), ByStudent bit NULL);

INSERT #B (SlotKey, TutorId, StudentId, TutorSubjectId, CampusId, D, StartT, EndT, StartAt, EndAt, Hrs, Fee, Past, Subject, TutorName, SlotCreated)
SELECT sl.SlotKey, sl.TutorId, st.StudentId, sl.TutorSubjectId, sl.CampusId, sl.D, sl.StartT, sl.EndT, sl.StartAt, sl.EndAt, sl.Hrs,
       ROUND(t.Rate * sl.Hrs, 2), sl.Past, ts.Subject, t.Name, sl.CreatedAt
FROM #Slot sl
JOIN #Tut t ON t.TutorId = sl.TutorId
JOIN #TS ts ON ts.TutorSubjectId = sl.TutorSubjectId
CROSS APPLY (SELECT TOP 1 s.StudentId FROM #Stu s
             ORDER BY CASE WHEN s.CampusId = sl.CampusId
                            AND CAST(SUBSTRING(HASHBYTES('MD5', CONCAT('local', sl.SlotKey)), 1, 4) AS bigint) % 100 < 85 THEN 0 ELSE 1 END,
                      HASHBYTES('MD5', CONCAT('who', sl.SlotKey, '|', s.StudentId))) st
WHERE CAST(SUBSTRING(HASHBYTES('MD5', CONCAT('book', sl.SlotKey)), 1, 4) AS bigint) % 100 < CASE WHEN sl.Past = 1 THEN 52 ELSE 40 END;

-- A student is never in two sessions at once.
;WITH d AS (SELECT ROW_NUMBER() OVER (PARTITION BY StudentId, D, StartT ORDER BY BKey) AS rn FROM #B)
DELETE FROM d WHERE rn > 1;
DELETE b FROM #B b
WHERE EXISTS (SELECT 1 FROM #B o WHERE o.StudentId = b.StudentId AND o.D = b.D AND o.BKey < b.BKey AND o.StartT < b.EndT AND b.StartT < o.EndT);
DELETE b FROM #B b
WHERE EXISTS (SELECT 1 FROM dbo.Bookings o WHERE o.StudentId = b.StudentId AND o.SessionDate = b.D AND o.Status IN (1, 2, 3, 4)
                AND o.StartTime < b.EndT AND b.StartT < o.EndTime);

UPDATE #B SET Bucket = CAST(SUBSTRING(HASHBYTES('MD5', CONCAT('status', BKey)), 1, 4) AS bigint) % 100;
UPDATE #B SET Status = CASE
        WHEN Past = 1 THEN CASE WHEN Bucket < 72 OR Bucket >= 95 THEN 4 WHEN Bucket < 82 THEN 5 WHEN Bucket < 88 THEN 6 ELSE 7 END
        ELSE CASE WHEN Bucket < 60 THEN 3 WHEN Bucket < 80 THEN 2 WHEN Bucket < 86 THEN 1 WHEN Bucket < 94 THEN 5 ELSE 6 END END;
UPDATE #B SET Status = CASE WHEN Past = 1 THEN 4 ELSE 3 END WHERE Fee = 0 AND Status IN (1, 7);   -- free sessions never wait for payment
UPDATE #B SET Status = 3 WHERE Status = 1 AND StartAt < DATEADD(hour, 24, @Now);                   -- only far-off sessions sit unpaid

-- When the student booked: after the slot was published, at least 14 hours before the session, and in the past.
UPDATE #B SET CreatedAt = DATEADD(minute,
        CAST(SUBSTRING(HASHBYTES('MD5', CONCAT('created', BKey)), 1, 4) AS bigint)
        % GREATEST(30, DATEDIFF(minute, SlotCreated, LEAST(DATEADD(hour, -14, StartAt), DATEADD(hour, -1, @Now)))), SlotCreated);
UPDATE #B SET CreatedAt = DATEADD(minute, -4, @Now), HoldExpiresAt = DATEADD(minute, 26, @Now) WHERE Status = 1;
UPDATE #B SET HoldExpiresAt = DATEADD(minute, 30, CreatedAt) WHERE Status = 7;

UPDATE #B SET ByStudent = CASE WHEN CAST(SUBSTRING(HASHBYTES('MD5', CONCAT('canceller', BKey)), 1, 4) AS bigint) % 100 < 65 THEN 1 ELSE 0 END
WHERE Status = 5;

UPDATE #B SET PaidAt = DATEADD(minute, 2 + CAST(SUBSTRING(HASHBYTES('MD5', CONCAT('paid', BKey)), 1, 4) AS bigint) % 18, CreatedAt)
WHERE Fee > 0 AND (Status IN (2, 3, 4, 6)
                   OR (Status = 5 AND (ByStudent = 0 OR CAST(SUBSTRING(HASHBYTES('MD5', CONCAT('cpaid', BKey)), 1, 4) AS bigint) % 100 < 75)));

UPDATE #B SET RespondedAt = GREATEST(DATEADD(minute, 5, COALESCE(PaidAt, CreatedAt)),
        LEAST(DATEADD(minute, 60 + CAST(SUBSTRING(HASHBYTES('MD5', CONCAT('resp', BKey)), 1, 4) AS bigint) % 1000, COALESCE(PaidAt, CreatedAt)),
              DATEADD(hour, -2, StartAt), DATEADD(minute, -20, @Now)))
WHERE Status IN (3, 4, 6)
   OR (Status = 5 AND (Fee = 0 OR PaidAt IS NOT NULL) AND CAST(SUBSTRING(HASHBYTES('MD5', CONCAT('conf', BKey)), 1, 4) AS bigint) % 100 < 70);

-- Cancellations: students must cancel 12+ hours ahead (the hub's rule); tutors may cancel later. Unpaid ones are cancelled during the hold.
UPDATE b SET CancelledAt = CASE
        WHEN b.PaidAt IS NULL AND b.Fee > 0 THEN DATEADD(minute, 8 + h.H % 18, b.CreatedAt)
        ELSE DATEADD(minute, h.H % GREATEST(1, DATEDIFF(minute, x.Lo, x.Hi)), x.Lo) END,
    CancelledBy = CASE WHEN b.ByStudent = 1 OR (b.PaidAt IS NULL AND b.Fee > 0) THEN s.UserId ELSE t.UserId END,
    CancelCat = 1 + h.H % 5
FROM #B b
JOIN #Stu s ON s.StudentId = b.StudentId
JOIN #Tut t ON t.TutorId = b.TutorId
CROSS APPLY (SELECT CAST(SUBSTRING(HASHBYTES('MD5', CONCAT('cancel', b.BKey)), 1, 4) AS bigint) AS H) h
CROSS APPLY (SELECT DATEADD(minute, 10, COALESCE(b.RespondedAt, b.PaidAt, b.CreatedAt)) AS Lo,
                    LEAST(DATEADD(hour, CASE WHEN b.ByStudent = 1 THEN -13 ELSE -2 END, b.StartAt), DATEADD(minute, -5, @Now)) AS Hi) x
WHERE b.Status = 5;
UPDATE #B SET ByStudent = 1 WHERE Status = 5 AND PaidAt IS NULL AND Fee > 0;
UPDATE #B SET CancelReason = CASE WHEN ByStudent = 1
        THEN CHOOSE(CancelCat, N'A class test was moved to the same time.', N'I have the flu and will not make it.',
                    N'I need the time to study for a test the next morning.', N'Our group assignment is due that afternoon.',
                    N'Family matter at home that weekend.')
        ELSE CHOOSE(CancelCat, N'A departmental meeting was moved to this slot.', N'Doctor''s appointment I could not move.',
                    N'I am invigilating an exam at that time.', N'Marking deadline for my own course.', N'Transport problems on the day.') END
WHERE Status = 5;

UPDATE #B SET DeclineReason = CHOOSE(1 + CAST(SUBSTRING(HASHBYTES('MD5', CONCAT('decline', BKey)), 1, 4) AS bigint) % 4,
        N'I am not available at this time after all.', N'That topic is outside what I tutor at this level.',
        N'It clashes with one of my own lectures.', N'Please book a longer session for this module.')
WHERE Status = 6;

UPDATE #B SET CompletedAt = LEAST(DATEADD(minute, 30 + CAST(SUBSTRING(HASHBYTES('MD5', CONCAT('done', BKey)), 1, 4) AS bigint) % 300, EndAt),
                                  DATEADD(minute, -10, @Now)),
              Attendance = CASE WHEN CAST(SUBSTRING(HASHBYTES('MD5', CONCAT('attend', BKey)), 1, 4) AS bigint) % 100 < 8 THEN 2 ELSE 1 END
WHERE Status = 4;
UPDATE #B SET Notes = CASE WHEN Attendance = 2 THEN N'Student did not arrive; waited 20 minutes and sent a message.'
        ELSE CHOOSE(1 + CAST(SUBSTRING(HASHBYTES('MD5', CONCAT('notes', BKey)), 1, 4) AS bigint) % 6,
             N'Worked through past exam questions and set practice problems for next time.',
             N'Revised the key concepts from this week''s lectures and cleared up the student''s questions.',
             N'Went through the tutorial sheet step by step.',
             N'Focused on exam technique and managing time in the test.',
             N'Covered the topics the student struggled with in the last test.',
             N'Built a one-page summary together for the upcoming test.') END
WHERE Status = 4;

UPDATE #B SET Msg = CHOOSE(1 + CAST(SUBSTRING(HASHBYTES('MD5', CONCAT('msg', BKey)), 1, 4) AS bigint) % 6,
        N'Please help with last week''s tutorial questions.', N'I have a test next week.', N'Can we go through past papers?',
        N'I am struggling with the second chapter.', N'I need help with my assignment questions.', N'Exam preparation, please.')
WHERE CAST(SUBSTRING(HASHBYTES('MD5', CONCAT('hasmsg', BKey)), 1, 4) AS bigint) % 100 < 45;

UPDATE b SET Reference = CONCAT('BK-', FORMAT(b.CreatedAt, 'yyMMdd'), '-', k.c6) FROM #B b JOIN #Code k ON k.n = b.BKey;

/* ---------- 5. Postponements ---------------------------------------------------------------------- */
CREATE TABLE #PP (BKey int PRIMARY KEY, Status int, OD date, OS time(0), OE time(0), PD date, PS time(0), PE time(0), OStartAt datetime2(0),
                  Cat int, CreatedAt datetime2(0) NULL, RespondedAt datetime2(0) NULL, TutorNote nvarchar(200) NULL);

-- Approved: the session was moved; the booking now sits on the slot the approval created.
INSERT #PP (BKey, Status, OD, OS, OE, PD, PS, PE, OStartAt, Cat)
SELECT b.BKey, 2, o.OD, b.StartT, b.EndT, b.D, b.StartT, b.EndT, DATEADD(hour, DATEPART(hour, b.StartAt), CAST(o.OD AS datetime2(0))),
       1 + CAST(SUBSTRING(HASHBYTES('MD5', CONCAT('ppcat', b.BKey)), 1, 4) AS bigint) % 5
FROM #B b
CROSS APPLY (SELECT DATEADD(day, -(1 + CAST(SUBSTRING(HASHBYTES('MD5', CONCAT('ppday', b.BKey)), 1, 4) AS bigint) % 3), b.D) AS OD) o
WHERE b.Status = 4
  AND CAST(SUBSTRING(HASHBYTES('MD5', CONCAT('pp', b.BKey)), 1, 4) AS bigint) % 100 < 6
  AND DATEDIFF(day, '19000101', o.OD) % 7 <> 6
  AND DATEADD(hour, 6, COALESCE(b.RespondedAt, b.PaidAt, b.CreatedAt)) < DATEADD(hour, DATEPART(hour, b.StartAt), CAST(o.OD AS datetime2(0)))
  AND NOT EXISTS (SELECT 1 FROM #Slot s WHERE s.TutorId = b.TutorId AND s.D = o.OD AND s.StartT < b.EndT AND b.StartT < s.EndT)
  AND NOT EXISTS (SELECT 1 FROM #B s WHERE s.StudentId = b.StudentId AND s.D = o.OD AND s.StartT < b.EndT AND b.StartT < s.EndT)
  AND NOT EXISTS (SELECT 1 FROM dbo.TutorAvailability a JOIN dbo.TutorSubjects ts ON ts.TutorSubjectId = a.TutorSubjectId
                  WHERE ts.TutorId = b.TutorId AND a.AvailabilityDate = o.OD AND a.StartTime < b.EndT AND b.StartT < a.EndTime);

-- Declined and withdrawn: the session stayed where it was.
INSERT #PP (BKey, Status, OD, OS, OE, PD, PS, PE, OStartAt, Cat)
SELECT b.BKey, CASE WHEN x.B < 9 THEN 3 ELSE 4 END, b.D, b.StartT, b.EndT,
       CASE WHEN DATEDIFF(day, '19000101', p.PD) % 7 = 6 THEN DATEADD(day, 1, p.PD) ELSE p.PD END,
       '16:00', '17:00', b.StartAt, 1 + CAST(SUBSTRING(HASHBYTES('MD5', CONCAT('ppcat', b.BKey)), 1, 4) AS bigint) % 5
FROM #B b
CROSS APPLY (SELECT CAST(SUBSTRING(HASHBYTES('MD5', CONCAT('pp', b.BKey)), 1, 4) AS bigint) % 100 AS B) x
CROSS APPLY (SELECT DATEADD(day, 1 + CAST(SUBSTRING(HASHBYTES('MD5', CONCAT('ppday', b.BKey)), 1, 4) AS bigint) % 3, b.D) AS PD) p
WHERE b.Status = 4 AND x.B BETWEEN 6 AND 10
  AND DATEADD(hour, 6, COALESCE(b.RespondedAt, b.PaidAt, b.CreatedAt)) < b.StartAt;

-- Pending: upcoming confirmed sessions whose student asked to move them; the tutor has not answered yet.
INSERT #PP (BKey, Status, OD, OS, OE, PD, PS, PE, OStartAt, Cat)
SELECT b.BKey, 1, b.D, b.StartT, b.EndT,
       CASE WHEN DATEDIFF(day, '19000101', p.PD) % 7 = 6 THEN DATEADD(day, 1, p.PD) ELSE p.PD END,
       '16:00', '17:00', b.StartAt, 1 + CAST(SUBSTRING(HASHBYTES('MD5', CONCAT('ppcat', b.BKey)), 1, 4) AS bigint) % 5
FROM #B b
CROSS APPLY (SELECT DATEADD(day, 1 + CAST(SUBSTRING(HASHBYTES('MD5', CONCAT('ppday', b.BKey)), 1, 4) AS bigint) % 4, b.D) AS PD) p
WHERE b.Status = 3 AND b.StartAt > DATEADD(hour, 30, @Now) AND b.RespondedAt < DATEADD(minute, -30, @Now)
  AND CAST(SUBSTRING(HASHBYTES('MD5', CONCAT('pp', b.BKey)), 1, 4) AS bigint) % 100 < 14;

-- When the request was made and answered: after the tutor confirmed, well before the original time.
UPDATE p SET CreatedAt = CASE WHEN p.Status = 1
        THEN LEAST(DATEADD(minute, -3, @Now), GREATEST(DATEADD(minute, 20, b.RespondedAt), DATEADD(minute, -(60 + h.H % 2000), @Now)))
        ELSE DATEADD(minute, h.H % GREATEST(1, DATEDIFF(minute, x.Lo, x.Hi)), x.Lo) END
FROM #PP p JOIN #B b ON b.BKey = p.BKey
CROSS APPLY (SELECT CAST(SUBSTRING(HASHBYTES('MD5', CONCAT('ppwhen', p.BKey)), 1, 4) AS bigint) AS H) h
CROSS APPLY (SELECT DATEADD(hour, 1, COALESCE(b.RespondedAt, b.PaidAt, b.CreatedAt)) AS Lo, DATEADD(hour, -3, p.OStartAt) AS Hi) x;
UPDATE p SET RespondedAt = LEAST(DATEADD(minute, 30 + CAST(SUBSTRING(HASHBYTES('MD5', CONCAT('ppans', p.BKey)), 1, 4) AS bigint) % 240, p.CreatedAt),
                                 DATEADD(hour, -1, p.OStartAt)),
             TutorNote = CASE p.Status WHEN 3 THEN N'I am fully booked that day, so let us keep the original time.'
                                       WHEN 2 THEN N'No problem, see you then.' END
FROM #PP p WHERE p.Status <> 1;

-- An approved move: the original slot was freed and a new slot was created at the moment of approval.
INSERT #Slot (TutorId, TutorSubjectId, CampusId, D, StartT, EndT, StartAt, EndAt, CreatedAt, Hrs, Past, IsBooked)
SELECT b.TutorId, b.TutorSubjectId, b.CampusId, p.OD, p.OS, p.OE, p.OStartAt, DATEADD(hour, CAST(b.Hrs AS int), p.OStartAt), b.SlotCreated, b.Hrs, 1, 0
FROM #PP p JOIN #B b ON b.BKey = p.BKey WHERE p.Status = 2;
UPDATE s SET CreatedAt = p.RespondedAt FROM #Slot s JOIN #B b ON b.SlotKey = s.SlotKey JOIN #PP p ON p.BKey = b.BKey WHERE p.Status = 2;

/* ---------- Write slots, bookings and sessions ---------------------------------------------------- */
UPDATE s SET IsBooked = 1 FROM #Slot s JOIN #B b ON b.SlotKey = s.SlotKey WHERE b.Status IN (1, 2, 3, 4);

CREATE TABLE #SlotMap (SlotKey int PRIMARY KEY, AvailabilityId int);
MERGE dbo.TutorAvailability AS t
USING #Slot AS s ON 1 = 0
WHEN NOT MATCHED THEN
    INSERT (TutorSubjectId, CampusId, AvailabilityDate, StartTime, EndTime, IsBooked, CreatedAt)
    VALUES (s.TutorSubjectId, s.CampusId, s.D, s.StartT, s.EndT, s.IsBooked, s.CreatedAt)
OUTPUT s.SlotKey, inserted.AvailabilityId INTO #SlotMap (SlotKey, AvailabilityId);

INSERT dbo.Bookings (Reference, StudentId, TutorId, TutorSubjectId, AvailabilityId, CampusId, SessionDate, StartTime, EndTime, Fee, Status,
                     StudentMessage, HoldExpiresAt, PaidAt, TutorRespondedAt, DeclineReason, CancelledAt, CancelledByUserId,
                     CancelReasonCategory, CancelReason, CompletedAt, CreatedAt)
SELECT b.Reference, b.StudentId, b.TutorId, b.TutorSubjectId, m.AvailabilityId, b.CampusId, b.D, b.StartT, b.EndT, b.Fee, b.Status,
       b.Msg, b.HoldExpiresAt, b.PaidAt, b.RespondedAt, b.DeclineReason, b.CancelledAt, b.CancelledBy, b.CancelCat, b.CancelReason,
       b.CompletedAt, b.CreatedAt
FROM #B b JOIN #SlotMap m ON m.SlotKey = b.SlotKey;
UPDATE b SET BookingId = x.BookingId FROM #B b JOIN dbo.Bookings x ON x.Reference = b.Reference;

INSERT dbo.TutoringSessions (BookingId, SessionDate, Attendance, HoursDelivered, TutorNotes, CreatedAt)
SELECT BookingId, D, Attendance, CASE WHEN Attendance = 1 THEN Hrs ELSE 0 END, Notes, CompletedAt FROM #B WHERE Status = 4;

INSERT dbo.PostponementRequests (BookingId, StudentId, TutorId, OriginalDate, OriginalStart, OriginalEnd, ProposedDate, ProposedStart, ProposedEnd,
                                 ReasonCategory, StudentNote, Status, CreatedAt, TutorResponseNote, RespondedAt)
SELECT b.BookingId, b.StudentId, b.TutorId, p.OD, p.OS, p.OE, p.PD, p.PS, p.PE, p.Cat,
       CHOOSE(p.Cat, N'My test was moved to the original day.', N'I have a clinic appointment at that time.',
              N'I need that day to prepare for a test.', N'An assignment is due that day.', N'I will be away from campus that day.'),
       p.Status, p.CreatedAt, p.TutorNote, p.RespondedAt
FROM #PP p JOIN #B b ON b.BKey = p.BKey;

/* ---------- 6. Reviews ---------------------------------------------------------------------------- */
CREATE TABLE #Rv (BKey int PRIMARY KEY, Rating int, Comment nvarchar(500), CreatedAt datetime2(0), Hidden bit NOT NULL DEFAULT 0);
INSERT #Rv (BKey, Rating, CreatedAt)
SELECT b.BKey,
       CASE WHEN r.R < 45 THEN 5 WHEN r.R < 80 THEN 4 WHEN r.R < 93 THEN 3 WHEN r.R < 98 THEN 2 ELSE 1 END,
       LEAST(DATEADD(minute, 60 + CAST(SUBSTRING(HASHBYTES('MD5', CONCAT('rvwhen', b.BKey)), 1, 4) AS bigint) % 2800, b.CompletedAt), DATEADD(minute, -5, @Now))
FROM #B b
CROSS APPLY (SELECT CAST(SUBSTRING(HASHBYTES('MD5', CONCAT('rating', b.BKey)), 1, 4) AS bigint) % 100 AS R) r
WHERE b.Status = 4 AND b.Attendance = 1 AND CAST(SUBSTRING(HASHBYTES('MD5', CONCAT('review', b.BKey)), 1, 4) AS bigint) % 100 < 60;
UPDATE #Rv SET Comment = CHOOSE((Rating - 1) * 3 + 1 + CAST(SUBSTRING(HASHBYTES('MD5', CONCAT('rvtext', BKey)), 1, 4) AS bigint) % 3,
        N'The tutor arrived 20 minutes late and seemed unprepared.', N'We did not cover anything I asked for.', N'Not worth the fee for me.',
        N'Started late and we ran out of time for my questions.', N'Hard to follow; it went too fast for me.', N'Not what I expected from the profile.',
        N'It was okay, but it felt a bit rushed.', N'Useful, but I needed more practice questions.', N'Decent session; the venue was noisy.',
        N'Helpful session, I would book again.', N'Good explanations, though we ran out of time at the end.', N'Clear and friendly; the worked examples made sense.',
        N'Explained everything clearly and patiently. I finally understand it.', N'Brilliant session, went through every question I brought.',
        N'Very well prepared, and the practice questions helped a lot.');
;WITH h AS (SELECT TOP (2) * FROM #Rv WHERE Rating <= 2 ORDER BY HASHBYTES('MD5', CONCAT('hide', BKey)))
UPDATE h SET Hidden = 1;

INSERT dbo.Reviews (BookingId, Rating, Comment, CreatedAt, IsHidden, HiddenReason, HiddenAt, HiddenByUserId)
SELECT b.BookingId, r.Rating, r.Comment, r.CreatedAt, r.Hidden,
       CASE WHEN r.Hidden = 1 THEN N'Personal remarks about the tutor; hidden by the hub.' END,
       CASE WHEN r.Hidden = 1 THEN LEAST(DATEADD(hour, 20, r.CreatedAt), DATEADD(minute, -2, @Now)) END,
       CASE WHEN r.Hidden = 1 THEN @StaffA END
FROM #Rv r JOIN #B b ON b.BKey = r.BKey;

/* ---------- 7. Timesheets ------------------------------------------------------------------------- */
CREATE TABLE #TW (TutorId int, WeekStart date, CampusId int, CreatedAt datetime2(0), Status int NULL, SubmittedAt datetime2(0) NULL,
                  ReviewedBy int NULL, ReviewedAt datetime2(0) NULL, Note nvarchar(300) NULL, Prep bit NOT NULL DEFAULT 0, TimesheetId int NULL,
                  PRIMARY KEY (TutorId, WeekStart));
INSERT #TW (TutorId, WeekStart, CampusId, CreatedAt)
SELECT b.TutorId, w.Ws, t.CampusId, MIN(s.CreatedAt)
FROM dbo.TutoringSessions s
JOIN dbo.Bookings b ON b.BookingId = s.BookingId
JOIN #Tut t ON t.TutorId = b.TutorId
CROSS APPLY (SELECT DATEADD(day, -(DATEDIFF(day, '19000101', s.SessionDate) % 7), s.SessionDate) AS Ws) w
WHERE s.Attendance = 1 AND s.SessionDate >= @From
  AND NOT EXISTS (SELECT 1 FROM dbo.Timesheets x WHERE x.TutorId = b.TutorId AND x.WeekStart = w.Ws)
GROUP BY b.TutorId, w.Ws, t.CampusId;

UPDATE #TW SET Status = CASE
        WHEN WeekStart >= @WeekStart THEN 1
        WHEN WeekStart = DATEADD(day, -7, @WeekStart) THEN 2
        WHEN CAST(SUBSTRING(HASHBYTES('MD5', CONCAT('tsrej', TutorId, '|', WeekStart)), 1, 4) AS bigint) % 100 < 6 THEN 4
        ELSE 3 END,
    Prep = CASE WHEN CAST(SUBSTRING(HASHBYTES('MD5', CONCAT('prep', TutorId, '|', WeekStart)), 1, 4) AS bigint) % 100 < 30
                 AND DATEADD(day, 4, WeekStart) < @Today THEN 1 ELSE 0 END;
UPDATE #TW SET SubmittedAt = LEAST(DATEADD(minute, 480 + CAST(SUBSTRING(HASHBYTES('MD5', CONCAT('tssub', TutorId, '|', WeekStart)), 1, 4) AS bigint) % 600,
                                           CAST(DATEADD(day, 7, WeekStart) AS datetime2(0))), DATEADD(minute, -15, @Now))
WHERE Status IN (2, 3, 4);
UPDATE #TW SET ReviewedBy = CASE WHEN CampusId = @EL THEN @StaffE ELSE @StaffA END,
               ReviewedAt = LEAST(DATEADD(hour, 20 + CAST(SUBSTRING(HASHBYTES('MD5', CONCAT('tsrev', TutorId, '|', WeekStart)), 1, 4) AS bigint) % 30, SubmittedAt),
                                  DATEADD(minute, -10, @Now)),
               Note = CASE WHEN Status = 4 THEN N'Please add the preparation hours you mentioned and resubmit.'
                           WHEN CAST(SUBSTRING(HASHBYTES('MD5', CONCAT('tsnote', TutorId, '|', WeekStart)), 1, 4) AS bigint) % 100 < 30
                           THEN N'Hours match the booked sessions.' END
WHERE Status IN (3, 4);

INSERT dbo.Timesheets (TutorId, WeekStart, Status, TotalHours, SubmittedAt, ReviewedByUserId, ReviewedAt, ReviewNote, CreatedAt)
SELECT TutorId, WeekStart, Status, 0, SubmittedAt, ReviewedBy, ReviewedAt, Note, CreatedAt FROM #TW;
UPDATE w SET TimesheetId = t.TimesheetId FROM #TW w JOIN dbo.Timesheets t ON t.TutorId = w.TutorId AND t.WeekStart = w.WeekStart;

INSERT dbo.TimesheetEntries (TimesheetId, BookingId, WorkDate, Hours, Description)
SELECT w.TimesheetId, b.BookingId, s.SessionDate, s.HoursDelivered, CONCAT(N'Tutoring session ', b.Reference, N' ', @Dash, N' ', sj.Name)
FROM #TW w
JOIN dbo.Bookings b ON b.TutorId = w.TutorId
JOIN dbo.TutoringSessions s ON s.BookingId = b.BookingId
JOIN dbo.TutorSubjects ts ON ts.TutorSubjectId = b.TutorSubjectId
JOIN dbo.Subjects sj ON sj.SubjectId = ts.SubjectId
WHERE s.Attendance = 1 AND s.SessionDate >= w.WeekStart AND s.SessionDate < DATEADD(day, 7, w.WeekStart);

INSERT dbo.TimesheetEntries (TimesheetId, BookingId, WorkDate, Hours, Description)
SELECT TimesheetId, NULL, DATEADD(day, 4, WeekStart),
       CASE WHEN CAST(SUBSTRING(HASHBYTES('MD5', CONCAT('prephrs', TutorId, '|', WeekStart)), 1, 4) AS bigint) % 2 = 0 THEN 1.00 ELSE 1.50 END,
       N'Preparing worksheets and marking practice tests'
FROM #TW WHERE Prep = 1;

UPDATE t SET TotalHours = x.Hrs
FROM dbo.Timesheets t JOIN (SELECT TimesheetId, SUM(Hours) AS Hrs FROM dbo.TimesheetEntries GROUP BY TimesheetId) x ON x.TimesheetId = t.TimesheetId
WHERE t.TimesheetId IN (SELECT TimesheetId FROM #TW);

/* =====================================================================================================
   8. Rentals and sales, in date order, so a copy is never rented or sold twice at the same time
   ===================================================================================================== */
CREATE TABLE #Bk (BookId int PRIMARY KEY, Title nvarchar(200), Weekly decimal(18,2), Late decimal(18,2), Price decimal(18,2));
INSERT #Bk SELECT BookId, Title, WeeklyRentalFee, LateFeePerDay, SalePrice FROM dbo.Books WHERE IsActive = 1;

-- Copies that already have rental or sale history are left alone.
CREATE TABLE #Cs (CopyId int PRIMARY KEY, BookId int, CampusId int, FreeFrom datetime2(0), Gone bit, FinalStatus int NULL, FinalCond int NULL);
INSERT #Cs (CopyId, BookId, CampusId, FreeFrom, Gone)
SELECT c.CopyId, c.BookId, c.CampusId, c.AcquiredAt,
       CASE WHEN c.Status <> 1 OR EXISTS (SELECT 1 FROM dbo.Rentals r WHERE r.CopyId = c.CopyId)
                 OR EXISTS (SELECT 1 FROM dbo.Sales s WHERE s.CopyId = c.CopyId) THEN 1 ELSE 0 END
FROM dbo.BookCopies c JOIN #Bk b ON b.BookId = c.BookId;

CREATE TABLE #BC (CampusId int, BookId int, PRIMARY KEY (CampusId, BookId));
INSERT #BC SELECT DISTINCT CampusId, BookId FROM #Cs WHERE Gone = 0;

CREATE TABLE #Req (ReqId int IDENTITY PRIMARY KEY, Kind char(1), StudentId int, CampusId int, BookId int, At datetime2(0), O int, Ren int, Seq int NULL);

-- Rentals spread over the window.
INSERT #Req (Kind, StudentId, CampusId, BookId, At, O, Ren)
SELECT 'R', s.StudentId, cp.CampusId, bk.BookId, x.At, x.O, x.Ren
FROM (SELECT n,
             DATEADD(minute, 510 + CAST(SUBSTRING(HASHBYTES('MD5', CONCAT('rtime', n)), 1, 4) AS bigint) % 450,
                     CAST(DATEADD(day, CAST(SUBSTRING(HASHBYTES('MD5', CONCAT('rday', n)), 1, 4) AS bigint) % DATEDIFF(day, @From, @Today), @From) AS datetime2(0))) AS At,
             CAST(SUBSTRING(HASHBYTES('MD5', CONCAT('rout', n)), 1, 4) AS bigint) % 100 AS O,
             CASE WHEN CAST(SUBSTRING(HASHBYTES('MD5', CONCAT('rren', n)), 1, 4) AS bigint) % 100 < 14 THEN 1
                  WHEN CAST(SUBSTRING(HASHBYTES('MD5', CONCAT('rren', n)), 1, 4) AS bigint) % 100 < 20 THEN 2 ELSE 0 END AS Ren
      FROM #N WHERE n BETWEEN 1 AND 200) x
CROSS APPLY (SELECT TOP 1 StudentId, CampusId FROM #Stu ORDER BY HASHBYTES('MD5', CONCAT('rstu', x.n, '|', StudentId))) s
CROSS APPLY (SELECT TOP 1 v.CampusId FROM (VALUES (@Alice), (@EL), (@Bisho)) v(CampusId)
             ORDER BY CASE WHEN v.CampusId = s.CampusId AND CAST(SUBSTRING(HASHBYTES('MD5', CONCAT('rhome', x.n)), 1, 4) AS bigint) % 100 < 88 THEN 0 ELSE 1 END,
                      HASHBYTES('MD5', CONCAT('rcamp', x.n, '|', v.CampusId))) cp
CROSS APPLY (SELECT TOP 1 BookId FROM #BC WHERE CampusId = cp.CampusId ORDER BY HASHBYTES('MD5', CONCAT('rbook', x.n, '|', BookId))) bk
WHERE DATEDIFF(day, '19000101', CAST(x.At AS date)) % 7 <> 6 AND x.At < DATEADD(hour, -1, @Now);

-- Two rentals reserved a few minutes ago and not paid yet.
INSERT #Req (Kind, StudentId, CampusId, BookId, At, O, Ren)
SELECT TOP (2) 'R', s.StudentId, s.CampusId, bk.BookId, DATEADD(minute, -6, @Now), 999, 0
FROM #Stu s
CROSS APPLY (SELECT TOP 1 BookId FROM #BC WHERE CampusId = s.CampusId ORDER BY HASHBYTES('MD5', CONCAT('pend', s.StudentId, '|', BookId))) bk
ORDER BY HASHBYTES('MD5', CONCAT('pendstu', s.StudentId));

-- Sales bought in the same checkout as a rental (one card payment covers both).
INSERT #Req (Kind, StudentId, CampusId, BookId, At, O, Ren)
SELECT 'S', r.StudentId, r.CampusId, bk.BookId, r.At, 50, 0
FROM #Req r
CROSS APPLY (SELECT TOP 1 BookId FROM #BC WHERE CampusId = r.CampusId AND BookId <> r.BookId
             ORDER BY HASHBYTES('MD5', CONCAT('sbask', r.ReqId, '|', BookId))) bk
WHERE r.Kind = 'R' AND r.O < 900 AND r.O >= 3 AND r.ReqId % 15 = 0;

-- Stand-alone sales.
INSERT #Req (Kind, StudentId, CampusId, BookId, At, O, Ren)
SELECT 'S', s.StudentId, s.CampusId, bk.BookId, x.At, x.O, 0
FROM (SELECT n,
             DATEADD(minute, 540 + CAST(SUBSTRING(HASHBYTES('MD5', CONCAT('stime', n)), 1, 4) AS bigint) % 420,
                     CAST(DATEADD(day, CAST(SUBSTRING(HASHBYTES('MD5', CONCAT('sday', n)), 1, 4) AS bigint) % DATEDIFF(day, @From, @Today), @From) AS datetime2(0))) AS At,
             CAST(SUBSTRING(HASHBYTES('MD5', CONCAT('sout', n)), 1, 4) AS bigint) % 100 AS O
      FROM #N WHERE n BETWEEN 1 AND 34) x
CROSS APPLY (SELECT TOP 1 StudentId, CampusId FROM #Stu ORDER BY HASHBYTES('MD5', CONCAT('sstu', x.n, '|', StudentId))) s
CROSS APPLY (SELECT TOP 1 BookId FROM #BC WHERE CampusId = s.CampusId ORDER BY HASHBYTES('MD5', CONCAT('sbook', x.n, '|', BookId))) bk
WHERE DATEDIFF(day, '19000101', CAST(x.At AS date)) % 7 <> 6 AND x.At < DATEADD(hour, -1, @Now);

UPDATE r SET Seq = x.rn FROM #Req r JOIN (SELECT ReqId, ROW_NUMBER() OVER (ORDER BY At, Kind, ReqId) AS rn FROM #Req) x ON x.ReqId = r.ReqId;

CREATE TABLE #RO (Seq int PRIMARY KEY, StudentId int, CopyId int, BookId int, CampusId int, RentedAt datetime2(0), Due date, FinalDue date,
                  ReturnedAt datetime2(0) NULL, Status int, Fee decimal(18,2), RenCount int, HoldExp datetime2(0) NULL, RetCond int NULL,
                  ProcBy int NULL, R1At datetime2(0) NULL, R2At datetime2(0) NULL, FineType int NULL, FineDays int NULL, FineAmt decimal(18,2) NULL,
                  FineStatus int NULL, FineAt datetime2(0) NULL, FinePaidAt datetime2(0) NULL, FineWaivedAt datetime2(0) NULL,
                  Reference nvarchar(20) NULL, RentalId int NULL, R1Id int NULL, R2Id int NULL, FineId int NULL);
CREATE TABLE #SO (Seq int PRIMARY KEY, StudentId int, CopyId int, BookId int, CampusId int, At datetime2(0), Status int, Price decimal(18,2),
                  HoldExp datetime2(0) NULL, CompletedAt datetime2(0) NULL, Reference nvarchar(20) NULL, SaleId int NULL);

DECLARE @i int = 1, @max int = (SELECT ISNULL(MAX(Seq), 0) FROM #Req);
DECLARE @k char(1), @st int, @cp int, @bk int, @at datetime2(0), @o int, @ren int, @copy int, @h bigint, @proc int;
DECLARE @wk decimal(18,2), @lt decimal(18,2), @pr decimal(18,2), @due date, @fin date, @ret datetime2(0), @rs int, @rc int;
DECLARE @r1 datetime2(0), @r2 datetime2(0), @ft int, @fd int, @fa decimal(18,2), @fs int, @fat datetime2(0), @cond int;

WHILE @i <= @max
BEGIN
    SELECT @k = Kind, @st = StudentId, @cp = CampusId, @bk = BookId, @at = At, @o = O, @ren = Ren FROM #Req WHERE Seq = @i;
    SET @copy = NULL;
    SELECT TOP 1 @copy = CopyId FROM #Cs
    WHERE BookId = @bk AND CampusId = @cp AND Gone = 0 AND FreeFrom <= @at
    ORDER BY HASHBYTES('MD5', CONCAT('copy', @i, '|', CopyId));

    IF @copy IS NOT NULL
    BEGIN
        SELECT @wk = Weekly, @lt = Late, @pr = Price FROM #Bk WHERE BookId = @bk;
        SET @h = CAST(SUBSTRING(HASHBYTES('MD5', CONCAT('rent', @i)), 1, 4) AS bigint);
        SET @proc = CASE WHEN @cp = @EL THEN @StaffE ELSE @StaffA END;

        IF @k = 'S'
        BEGIN
            IF @o < 6
            BEGIN   -- reserved, never paid: the hold lapsed
                INSERT #SO (Seq, StudentId, CopyId, BookId, CampusId, At, Status, Price, HoldExp, CompletedAt)
                VALUES (@i, @st, @copy, @bk, @cp, @at, 3, @pr, DATEADD(minute, 30, @at), NULL);
                UPDATE #Cs SET FreeFrom = DATEADD(minute, 30, @at) WHERE CopyId = @copy;
            END
            ELSE
            BEGIN
                INSERT #SO (Seq, StudentId, CopyId, BookId, CampusId, At, Status, Price, HoldExp, CompletedAt)
                VALUES (@i, @st, @copy, @bk, @cp, @at, 2, @pr, NULL, DATEADD(minute, 4, @at));
                UPDATE #Cs SET Gone = 1, FinalStatus = 4 WHERE CopyId = @copy;
            END
        END
        ELSE IF @o = 999
        BEGIN       -- awaiting payment right now
            INSERT #RO (Seq, StudentId, CopyId, BookId, CampusId, RentedAt, Due, FinalDue, Status, Fee, RenCount, HoldExp)
            VALUES (@i, @st, @copy, @bk, @cp, @at, DATEADD(day, 7, CAST(@at AS date)), DATEADD(day, 7, CAST(@at AS date)), 1, @wk, 0, DATEADD(minute, 30, @at));
            UPDATE #Cs SET Gone = 1, FinalStatus = 2 WHERE CopyId = @copy;
        END
        ELSE IF @o < 3
        BEGIN       -- reserved, never paid
            INSERT #RO (Seq, StudentId, CopyId, BookId, CampusId, RentedAt, Due, FinalDue, Status, Fee, RenCount, HoldExp)
            VALUES (@i, @st, @copy, @bk, @cp, @at, DATEADD(day, 7, CAST(@at AS date)), DATEADD(day, 7, CAST(@at AS date)), 5, @wk, 0, DATEADD(minute, 30, @at));
            UPDATE #Cs SET FreeFrom = DATEADD(minute, 30, @at) WHERE CopyId = @copy;
        END
        ELSE
        BEGIN
            SET @due = DATEADD(day, 7, CAST(@at AS date));
            SET @r1 = NULL; SET @r2 = NULL; SET @rc = 0;
            IF @ren >= 1
            BEGIN
                SET @r1 = DATEADD(minute, 540 + @h % 300, CAST(DATEADD(day, -1, @due) AS datetime2(0)));
                IF @r1 > DATEADD(minute, -10, @Now) SET @r1 = NULL ELSE SET @rc = 1;
            END
            IF @ren >= 2 AND @r1 IS NOT NULL
            BEGIN
                SET @r2 = DATEADD(minute, 540 + @h % 280, CAST(DATEADD(day, 6, @due) AS datetime2(0)));
                IF @r2 > DATEADD(minute, -10, @Now) SET @r2 = NULL ELSE SET @rc = 2;
            END
            SET @fin = DATEADD(day, 7 * @rc, @due);
            SET @ret = NULL; SET @ft = NULL; SET @fd = NULL; SET @fa = NULL; SET @fs = NULL; SET @fat = NULL; SET @cond = NULL;

            IF (@o BETWEEN 3 AND 5 OR @o BETWEEN 31 AND 33) AND @fin < DATEADD(day, -14, @Today)
            BEGIN   -- never came back: written off as lost two weeks after the due date
                SET @rs = 4; SET @ft = 3; SET @fd = 0; SET @fa = @pr;
                SET @fat = DATEADD(hour, 11, CAST(DATEADD(day, 14, @fin) AS datetime2(0)));
                SET @fs = CASE WHEN @h % 2 = 0 THEN 2 ELSE 1 END;
            END
            ELSE
            BEGIN
                -- 25-30: still has it; 10-24: back late; otherwise back on time (6-9 of those come back damaged)
                SET @ret = CASE WHEN @o BETWEEN 25 AND 30 THEN NULL
                                WHEN @o BETWEEN 10 AND 24 THEN DATEADD(minute, 540 + @h % 420, CAST(DATEADD(day, 1 + @h % 8, @fin) AS datetime2(0)))
                                ELSE DATEADD(minute, 540 + @h % 420, CAST(DATEADD(day, -(@h % 4), @fin) AS datetime2(0))) END;
                IF @ret IS NOT NULL AND @ret < DATEADD(day, 1, @at) SET @ret = DATEADD(day, 1, @at);
                IF @ret IS NOT NULL AND @ret > DATEADD(minute, -30, @Now) SET @ret = NULL;                         -- not back yet

                IF @ret IS NULL
                    SET @rs = 2
                ELSE
                BEGIN
                    SET @rs = 3;
                    SET @fd = DATEDIFF(day, @fin, CAST(@ret AS date));
                    IF @fd > 0
                    BEGIN
                        SET @ft = 1; SET @fa = @fd * @lt; SET @fat = @ret;
                        SET @fs = CASE WHEN @h % 100 < 60 THEN 2 WHEN @h % 100 < 85 THEN 3 ELSE 1 END;
                    END
                    ELSE IF @o BETWEEN 6 AND 9
                    BEGIN
                        SET @ft = 2; SET @fa = ROUND(@pr * 0.25, 0); SET @fat = @ret; SET @fs = CASE WHEN @h % 2 = 0 THEN 2 ELSE 1 END; SET @cond = 5;
                    END
                    IF @fd < 0 SET @fd = 0;
                END
            END

            INSERT #RO (Seq, StudentId, CopyId, BookId, CampusId, RentedAt, Due, FinalDue, ReturnedAt, Status, Fee, RenCount, HoldExp, RetCond, ProcBy,
                        R1At, R2At, FineType, FineDays, FineAmt, FineStatus, FineAt, FinePaidAt, FineWaivedAt)
            VALUES (@i, @st, @copy, @bk, @cp, @at, @due, @fin, @ret, @rs, @wk, @rc, NULL,
                    CASE WHEN @rs = 3 THEN ISNULL(@cond, 2) END, CASE WHEN @rs IN (3, 4) THEN @proc END,
                    @r1, @r2, @ft, @fd, @fa, @fs, @fat,
                    CASE WHEN @fs = 2 THEN LEAST(DATEADD(minute, 15 + @h % 4000, @fat), DATEADD(minute, -5, @Now)) END,
                    CASE WHEN @fs = 3 THEN LEAST(DATEADD(minute, 120 + @h % 2400, @fat), DATEADD(minute, -5, @Now)) END);

            UPDATE #Cs
            SET FreeFrom    = CASE WHEN @rs = 3 THEN @ret ELSE FreeFrom END,
                Gone        = CASE WHEN @rs = 3 AND ISNULL(@cond, 0) <> 5 THEN 0 ELSE 1 END,
                FinalStatus = CASE WHEN @rs = 3 AND @cond = 5 THEN 6 WHEN @rs = 3 THEN FinalStatus WHEN @rs = 2 THEN 3 ELSE 5 END,
                FinalCond   = CASE WHEN @rs = 3 AND @cond = 5 THEN 5 ELSE FinalCond END
            WHERE CopyId = @copy;
        END
    END
    SET @i += 1;
END;

UPDATE r SET Reference = CONCAT('RN-', FORMAT(r.RentedAt, 'yyMMdd'), '-', k.c6) FROM #RO r JOIN #Code k ON k.n = 14000 + r.Seq;
INSERT dbo.Rentals (Reference, StudentId, CopyId, RentedAt, DueDate, ReturnedAt, Status, Fee, RenewalCount, HoldExpiresAt, ReturnCondition, ProcessedByUserId, CreatedAt)
SELECT Reference, StudentId, CopyId, RentedAt, FinalDue, ReturnedAt, Status, Fee, RenCount, HoldExp, RetCond, ProcBy, RentedAt FROM #RO;
UPDATE r SET RentalId = x.RentalId FROM #RO r JOIN dbo.Rentals x ON x.Reference = r.Reference;

INSERT dbo.RentalRenewals (RentalId, RenewedAt, PreviousDueDate, NewDueDate, Fee)
SELECT RentalId, R1At, Due, DATEADD(day, 7, Due), Fee FROM #RO WHERE R1At IS NOT NULL
UNION ALL
SELECT RentalId, R2At, DATEADD(day, 7, Due), DATEADD(day, 14, Due), Fee FROM #RO WHERE R2At IS NOT NULL;
UPDATE r SET R1Id = x.RenewalId FROM #RO r JOIN dbo.RentalRenewals x ON x.RentalId = r.RentalId AND x.NewDueDate = DATEADD(day, 7, r.Due) WHERE r.R1At IS NOT NULL;
UPDATE r SET R2Id = x.RenewalId FROM #RO r JOIN dbo.RentalRenewals x ON x.RentalId = r.RentalId AND x.NewDueDate = DATEADD(day, 14, r.Due) WHERE r.R2At IS NOT NULL;

INSERT dbo.Fines (RentalId, Type, Amount, DaysLate, Status, Reason, CreatedAt, CreatedByUserId, WaivedByUserId, WaivedAt, WaiverReason)
SELECT RentalId, FineType, FineAmt, FineDays, FineStatus,
       CASE FineType WHEN 1 THEN CONCAT(N'Returned ', FineDays, N' day(s) late') WHEN 2 THEN N'Cover torn and pages water-damaged'
                     ELSE N'Not returned; written off as lost' END,
       FineAt, ProcBy, CASE WHEN FineStatus = 3 THEN ProcBy END, FineWaivedAt,
       CASE WHEN FineStatus = 3 THEN N'First late return; waived as a courtesy.' END
FROM #RO WHERE FineType IS NOT NULL;
UPDATE r SET FineId = x.FineId FROM #RO r JOIN dbo.Fines x ON x.RentalId = r.RentalId WHERE r.FineType IS NOT NULL;

UPDATE s SET Reference = CONCAT('SL-', FORMAT(s.At, 'yyMMdd'), '-', k.c6) FROM #SO s JOIN #Code k ON k.n = 14500 + s.Seq;
INSERT dbo.Sales (Reference, StudentId, CopyId, Price, Status, HoldExpiresAt, CreatedAt, CompletedAt)
SELECT Reference, StudentId, CopyId, Price, Status, HoldExp, At, CompletedAt FROM #SO;
UPDATE s SET SaleId = x.SaleId FROM #SO s JOIN dbo.Sales x ON x.Reference = s.Reference;

UPDATE c SET Status = s.FinalStatus, Condition = ISNULL(s.FinalCond, c.Condition)
FROM dbo.BookCopies c JOIN #Cs s ON s.CopyId = c.CopyId WHERE s.FinalStatus IS NOT NULL;

-- Waitlists on the two scarce titles, at campuses with no copy on the shelf.
INSERT dbo.BookWaitlists (BookId, StudentId, CampusId, IsActive, CreatedAt, NotifiedAt)
SELECT x.BookId, x.StudentId, x.CampusId, 1, DATEADD(hour, -(6 + x.H % 200), @Now), NULL
FROM (
    SELECT bk.BookId, s.StudentId, s.CampusId, CAST(SUBSTRING(HASHBYTES('MD5', CONCAT('wait', bk.BookId, '|', s.StudentId)), 1, 4) AS bigint) AS H,
           ROW_NUMBER() OVER (PARTITION BY bk.BookId, s.CampusId ORDER BY HASHBYTES('MD5', CONCAT('wait', bk.BookId, '|', s.StudentId))) AS rn
    FROM dbo.Books bk CROSS JOIN #Stu s
    WHERE bk.Isbn IN (N'9780262046305', N'9781260091991')
      AND NOT EXISTS (SELECT 1 FROM dbo.BookCopies c WHERE c.BookId = bk.BookId AND c.CampusId = s.CampusId AND c.Status = 1)
      AND NOT EXISTS (SELECT 1 FROM dbo.BookWaitlists w WHERE w.BookId = bk.BookId AND w.StudentId = s.StudentId AND w.CampusId = s.CampusId)
) x WHERE x.rn <= 2;

/* =====================================================================================================
   9. Fees, card payments and refunds (Booking/Rental/Sale/Fine -> Fee -> PaymentAllocation -> Payment)
   ===================================================================================================== */
CREATE TABLE #F (FKey int IDENTITY PRIMARY KEY, StudentId int, Type int, Description nvarchar(200), Amount decimal(18,2), Status int,
                 CreatedAt datetime2(0), SettledAt datetime2(0) NULL, BookingId int NULL, RentalId int NULL, RenewalId int NULL, SaleId int NULL,
                 FineId int NULL, PaidAt datetime2(0) NULL, RefundAt datetime2(0) NULL, FeeId int NULL);

-- Tutoring
INSERT #F (StudentId, Type, Description, Amount, Status, CreatedAt, SettledAt, BookingId, PaidAt, RefundAt)
SELECT b.StudentId, 1,
       LEFT(CONCAT(N'Tutoring ', @Dash, N' ', b.Subject, N' with ', b.TutorName, N', ', FORMAT(COALESCE(p.OD, b.D), 'ddd d MMM yyyy', 'en-GB'), N', ',
                   FORMAT(b.StartAt, 'HH:mm'), @Dash, FORMAT(b.EndAt, 'HH:mm')), 200),
       b.Fee,
       CASE WHEN b.Status IN (2, 3, 4) THEN 2 WHEN b.Status = 6 THEN 4 WHEN b.Status = 5 AND b.PaidAt IS NOT NULL THEN 4
            WHEN b.Status = 1 THEN 1 ELSE 3 END,
       b.CreatedAt,
       CASE WHEN b.Status IN (2, 3, 4) THEN b.PaidAt WHEN b.Status = 6 THEN b.RespondedAt WHEN b.Status = 5 THEN b.CancelledAt
            WHEN b.Status = 7 THEN b.HoldExpiresAt END,
       b.BookingId, b.PaidAt,
       CASE WHEN b.Status = 6 THEN b.RespondedAt WHEN b.Status = 5 AND b.PaidAt IS NOT NULL THEN b.CancelledAt END
FROM #B b LEFT JOIN #PP p ON p.BKey = b.BKey AND p.Status = 2
WHERE b.Fee > 0;

-- Rentals, renewals, sales and fines
INSERT #F (StudentId, Type, Description, Amount, Status, CreatedAt, SettledAt, RentalId, PaidAt)
SELECT r.StudentId, 2, LEFT(CONCAT(N'Textbook rental ', @Dash, N' "', b.Title, N'" (7 days, ', c.Name, N')'), 200), r.Fee,
       CASE r.Status WHEN 5 THEN 3 WHEN 1 THEN 1 ELSE 2 END, r.RentedAt,
       CASE r.Status WHEN 5 THEN r.HoldExp WHEN 1 THEN NULL ELSE DATEADD(minute, 4, r.RentedAt) END,
       r.RentalId, CASE WHEN r.Status IN (2, 3, 4) THEN DATEADD(minute, 4, r.RentedAt) END
FROM #RO r JOIN #Bk b ON b.BookId = r.BookId JOIN dbo.Campuses c ON c.CampusId = r.CampusId;

INSERT #F (StudentId, Type, Description, Amount, Status, CreatedAt, SettledAt, RenewalId, PaidAt)
SELECT r.StudentId, 3, LEFT(CONCAT(N'Rental renewal ', @Dash, N' "', b.Title, N'" to ', FORMAT(x.NewDue, 'd MMM yyyy', 'en-GB')), 200), r.Fee,
       CASE WHEN x.Unpaid = 1 THEN 1 ELSE 2 END, x.At, CASE WHEN x.Unpaid = 1 THEN NULL ELSE DATEADD(minute, 3, x.At) END,
       x.RenewalId, CASE WHEN x.Unpaid = 1 THEN NULL ELSE DATEADD(minute, 3, x.At) END
FROM #RO r JOIN #Bk b ON b.BookId = r.BookId
CROSS APPLY (VALUES (r.R1Id, r.R1At, DATEADD(day, 7, r.Due)), (r.R2Id, r.R2At, DATEADD(day, 14, r.Due))) v(RenewalId, At, NewDue)
CROSS APPLY (SELECT v.RenewalId, v.At, v.NewDue,
                    CASE WHEN r.Status = 2 AND v.At > DATEADD(day, -10, @Now)
                              AND CAST(SUBSTRING(HASHBYTES('MD5', CONCAT('renpay', v.RenewalId)), 1, 4) AS bigint) % 100 < 30 THEN 1 ELSE 0 END AS Unpaid) x
WHERE v.RenewalId IS NOT NULL;

INSERT #F (StudentId, Type, Description, Amount, Status, CreatedAt, SettledAt, SaleId, PaidAt)
SELECT s.StudentId, 4, LEFT(CONCAT(N'Book purchase ', @Dash, N' "', b.Title, N'" (', c.Name, N')'), 200), s.Price,
       CASE s.Status WHEN 2 THEN 2 ELSE 3 END, s.At, CASE s.Status WHEN 2 THEN s.CompletedAt ELSE s.HoldExp END,
       s.SaleId, CASE WHEN s.Status = 2 THEN s.CompletedAt END
FROM #SO s JOIN #Bk b ON b.BookId = s.BookId JOIN dbo.Campuses c ON c.CampusId = s.CampusId;

INSERT #F (StudentId, Type, Description, Amount, Status, CreatedAt, SettledAt, FineId, PaidAt)
SELECT r.StudentId, CASE r.FineType WHEN 1 THEN 5 WHEN 2 THEN 6 ELSE 7 END,
       LEFT(CASE r.FineType WHEN 1 THEN CONCAT(N'Overdue fine ', @Dash, N' "', b.Title, N'" (', r.FineDays, N' day(s) late)')
                            WHEN 2 THEN CONCAT(N'Damage fine ', @Dash, N' "', b.Title, N'"')
                            ELSE CONCAT(N'Lost book ', @Dash, N' "', b.Title, N'"') END, 200),
       r.FineAmt, CASE r.FineStatus WHEN 2 THEN 2 WHEN 3 THEN 5 ELSE 1 END, r.FineAt,
       CASE r.FineStatus WHEN 2 THEN r.FinePaidAt WHEN 3 THEN r.FineWaivedAt END, r.FineId, r.FinePaidAt
FROM #RO r JOIN #Bk b ON b.BookId = r.BookId WHERE r.FineType IS NOT NULL AND r.FineAmt > 0;

-- Write the fees and remember their ids.
CREATE TABLE #FMap (FKey int PRIMARY KEY, FeeId int);
MERGE dbo.Fees AS t
USING #F AS s ON 1 = 0
WHEN NOT MATCHED THEN
    INSERT (StudentId, Type, Description, Amount, Status, CreatedAt, SettledAt, BookingId, RentalId, RentalRenewalId, SaleId, FineId)
    VALUES (s.StudentId, s.Type, s.Description, s.Amount, s.Status, s.CreatedAt, s.SettledAt, s.BookingId, s.RentalId, s.RenewalId, s.SaleId, s.FineId)
OUTPUT s.FKey, inserted.FeeId INTO #FMap (FKey, FeeId);
UPDATE f SET FeeId = m.FeeId FROM #F f JOIN #FMap m ON m.FKey = f.FKey;

-- One card payment per checkout: fees a student paid at the same moment share a payment.
CREATE TABLE #P (PKey int IDENTITY PRIMARY KEY, StudentId int, PaidAt datetime2(0), Amount decimal(18,2), Reference nvarchar(30) NULL, PaymentId int NULL);
INSERT #P (StudentId, PaidAt, Amount)
SELECT StudentId, PaidAt, SUM(Amount) FROM #F WHERE PaidAt IS NOT NULL AND Amount > 0 GROUP BY StudentId, PaidAt ORDER BY PaidAt, StudentId;
UPDATE p SET Reference = CONCAT('SL-PAY-', FORMAT(p.PaidAt, 'yyyyMMdd'), '-', k.c6) FROM #P p JOIN #Code k ON k.n = 3000 + p.PKey;

INSERT dbo.Payments (Reference, StudentId, Type, Method, Status, Amount, CardBrand, CardLast4, GatewayReference, FailureReason, CheckoutToken,
                     RefundOfPaymentId, Note, CreatedAt, CompletedAt, ProcessedByUserId)
SELECT p.Reference, p.StudentId, 1, 1, 2, p.Amount, s.Brand, s.Last4, CONCAT('SIM-', k.c12), NULL, NEWID(), NULL, NULL, p.PaidAt, p.PaidAt, NULL
FROM #P p JOIN #Stu s ON s.StudentId = p.StudentId JOIN #Code k ON k.n = 3000 + p.PKey;
UPDATE p SET PaymentId = x.PaymentId FROM #P p JOIN dbo.Payments x ON x.Reference = p.Reference;

INSERT dbo.PaymentAllocations (PaymentId, FeeId, Amount)
SELECT p.PaymentId, f.FeeId, f.Amount FROM #F f JOIN #P p ON p.StudentId = f.StudentId AND p.PaidAt = f.PaidAt WHERE f.Amount > 0;

-- Refunds go back to the card that paid (declined sessions and cancelled paid sessions).
INSERT dbo.Payments (Reference, StudentId, Type, Method, Status, Amount, CardBrand, CardLast4, GatewayReference, FailureReason, CheckoutToken,
                     RefundOfPaymentId, Note, CreatedAt, CompletedAt, ProcessedByUserId)
SELECT CONCAT('SL-REF-', FORMAT(f.RefundAt, 'yyyyMMdd'), '-', k.c6), f.StudentId, 2, 1, 2, f.Amount, s.Brand, s.Last4, CONCAT('SIMR-', k.c12),
       NULL, NULL, p.PaymentId, LEFT(N'Refund: ' + f.Description, 200), f.RefundAt, f.RefundAt, NULL
FROM #F f
JOIN #P p ON p.StudentId = f.StudentId AND p.PaidAt = f.PaidAt
JOIN #Stu s ON s.StudentId = f.StudentId
JOIN #Code k ON k.n = 8000 + f.FKey
WHERE f.RefundAt IS NOT NULL;

/* =====================================================================================================
   10. Operations queues and notifications
   ===================================================================================================== */
INSERT dbo.SupportMessages (UserId, Name, Email, CampusId, Subject, Body, Status, CreatedAt, HandledByUserId, HandledAt, ResponseNote)
SELECT u.UserId, u.FirstName + N' ' + u.LastName, u.Email, u.CampusId, v.Subj, v.Body, v.Status,
       DATEADD(hour, -v.HoursAgo, @Now),
       CASE WHEN v.Status > 1 THEN CASE WHEN u.CampusId = @EL THEN @StaffE ELSE @StaffA END END,
       CASE WHEN v.Status > 1 THEN DATEADD(hour, -v.HoursAgo + 3, @Now) END, v.Reply
FROM (VALUES
    (N'lwazi.mbatha',      N'Card payment shows twice', N'My bank app shows the rental payment twice. Was I charged twice?', 3, 260,
     N'Only one payment went through; the second line was a pending authorisation that your bank has released.'),
    (N'nandipha.booi',     N'Can I change my tutor?', N'I would like to continue with a different English tutor. Is that possible?', 3, 190,
     N'Yes - book any verified tutor from Find a Tutor; your history stays on your profile.'),
    (N'qhama.mpofu',       N'Accounting Principles at Bisho', N'Will more copies of Accounting Principles come to Bisho before exams?', 2, 70,
     N'A restock is on order; we will let you know when it arrives.'),
    (N'ziyanda.mkhize',    N'Fine on a returned book', N'I returned my statistics book on the due date but I see a fine.', 2, 30, NULL),
    (N'mihlali.jonas',     N'Session receipt', N'Where can I download a receipt for my tutoring payments?', 1, 9, NULL),
    (N'sandile.qwesha',    N'Opening hours during exams', N'Is the East London hub open on Saturdays during the exam period?', 1, 4, NULL)
) v(UserName, Subj, Body, Status, HoursAgo, Reply)
JOIN dbo.Users u ON u.UserName = v.UserName;

INSERT dbo.PasswordResetRequests (UserId, Identifier, Message, Status, CreatedAt, HandledByUserId, HandledAt, ResponseNote, RequestIp)
SELECT u.UserId, u.UserName, v.Msg, v.Status, DATEADD(hour, -v.HoursAgo, @Now),
       CASE WHEN v.Status = 3 THEN @StaffA END, CASE WHEN v.Status = 3 THEN DATEADD(hour, -v.HoursAgo + 2, @Now) END,
       CASE WHEN v.Status = 3 THEN N'We could not confirm your identity. Please visit the hub with your student card.' END, NULL
FROM (VALUES (N'kamva.ndlela', N'I changed phones and lost my saved password.', 1, 5),
             (N'esona.mgudlwa', N'Locked out after too many attempts.', 1, 2),
             (N'lithemba.peter', N'Please reset my password, I cannot remember it.', 3, 120)) v(UserName, Msg, Status, HoursAgo)
JOIN dbo.Users u ON u.UserName = v.UserName;

-- Students: confirmations of their upcoming sessions.
INSERT dbo.Notifications (UserId, Title, Message, Type, LinkUrl, IsRead, CreatedAt, DedupKey)
SELECT s.UserId, N'Session confirmed',
       CONCAT(b.Subject, N' on ', FORMAT(b.D, 'ddd d MMM', 'en-GB'), N' at ', FORMAT(b.StartAt, 'HH:mm'), N' is confirmed by your tutor.'),
       21, CONCAT(N'~/Student/Tutoring/BookingDetails.aspx?id=', b.BookingId),
       CASE WHEN b.RespondedAt < DATEADD(day, -2, @Now) THEN 1 ELSE 0 END, b.RespondedAt, NULL
FROM #B b JOIN #Stu s ON s.StudentId = b.StudentId WHERE b.Status = 3;

-- Tutors: requests waiting for an answer, and postponement requests.
INSERT dbo.Notifications (UserId, Title, Message, Type, LinkUrl, IsRead, CreatedAt, DedupKey)
SELECT t.UserId, N'New booking request',
       CONCAT(s.Name, N' booked ', b.Subject, N' on ', FORMAT(b.D, 'ddd d MMM', 'en-GB'), N' at ', FORMAT(b.StartAt, 'HH:mm'), N'. Please confirm or decline.'),
       20, CONCAT(N'~/Tutor/SessionDetails.aspx?id=', b.BookingId), 0, COALESCE(b.PaidAt, b.CreatedAt), NULL
FROM #B b JOIN #Tut t ON t.TutorId = b.TutorId JOIN #Stu s ON s.StudentId = b.StudentId WHERE b.Status = 2;

INSERT dbo.Notifications (UserId, Title, Message, Type, LinkUrl, IsRead, CreatedAt, DedupKey)
SELECT t.UserId, N'Postponement requested',
       CONCAT(s.Name, N' asked to move ', b.Subject, N' on ', FORMAT(b.D, 'ddd d MMM', 'en-GB'), N' to ', FORMAT(p.PD, 'ddd d MMM', 'en-GB'), N' at 16:00.'),
       10, CONCAT(N'~/Tutor/SessionDetails.aspx?id=', b.BookingId), 0, p.CreatedAt, NULL
FROM #PP p JOIN #B b ON b.BKey = p.BKey JOIN #Tut t ON t.TutorId = b.TutorId JOIN #Stu s ON s.StudentId = b.StudentId WHERE p.Status = 1;

-- Students: recent refunds and outstanding fines.
INSERT dbo.Notifications (UserId, Title, Message, Type, LinkUrl, IsRead, CreatedAt, DedupKey)
SELECT s.UserId, N'Refund issued',
       LEFT(CONCAT(N'R ', FORMAT(f.Amount, 'N2', 'en-GB'), N' was refunded to your card: ', f.Description), 500),
       46, N'~/Student/Fees/MyFees.aspx', CASE WHEN f.RefundAt < DATEADD(day, -3, @Now) THEN 1 ELSE 0 END, f.RefundAt, NULL
FROM #F f JOIN #Stu s ON s.StudentId = f.StudentId WHERE f.RefundAt > DATEADD(day, -21, @Now);

INSERT dbo.Notifications (UserId, Title, Message, Type, LinkUrl, IsRead, CreatedAt, DedupKey)
SELECT s.UserId, N'Fine added', LEFT(CONCAT(N'A fine of R ', FORMAT(f.Amount, 'N2', 'en-GB'), N' was added: ', f.Description), 500),
       44, N'~/Student/Fees/MyFees.aspx', 0, f.CreatedAt, NULL
FROM #F f JOIN #Stu s ON s.StudentId = f.StudentId WHERE f.FineId IS NOT NULL AND f.Status = 1;

-- Tutors: reviewed timesheets; staff: timesheets waiting for review.
INSERT dbo.Notifications (UserId, Title, Message, Type, LinkUrl, IsRead, CreatedAt, DedupKey)
SELECT t.UserId, CASE w.Status WHEN 3 THEN N'Timesheet approved' ELSE N'Timesheet returned' END,
       CONCAT(N'Your timesheet for the week of ', FORMAT(w.WeekStart, 'd MMM', 'en-GB'),
              CASE w.Status WHEN 3 THEN N' was approved.' ELSE CONCAT(N' needs changes: ', w.Note) END),
       28, CONCAT(N'~/Tutor/Timesheet.aspx?week=', FORMAT(w.WeekStart, 'yyyy-MM-dd')),
       CASE WHEN w.ReviewedAt < DATEADD(day, -4, @Now) THEN 1 ELSE 0 END, w.ReviewedAt, NULL
FROM #TW w JOIN #Tut t ON t.TutorId = w.TutorId WHERE w.Status IN (3, 4) AND w.ReviewedAt > DATEADD(day, -21, @Now);

INSERT dbo.Notifications (UserId, Title, Message, Type, LinkUrl, IsRead, CreatedAt, DedupKey)
SELECT r.UserId, N'Timesheet submitted',
       CONCAT(t.UserName, N' submitted ', CAST(ts.TotalHours AS decimal(9,2)), N' hours for the week of ', FORMAT(w.WeekStart, 'd MMM', 'en-GB'), N'.'),
       27, CONCAT(N'~/Operations/Timesheets.aspx?id=', w.TimesheetId), 0, w.SubmittedAt, NULL
FROM #TW w JOIN #Tut t ON t.TutorId = w.TutorId JOIN dbo.Timesheets ts ON ts.TimesheetId = w.TimesheetId
CROSS JOIN (SELECT DISTINCT UserId FROM (VALUES (@StaffA), (@StaffE)) v(UserId)) r
WHERE w.Status = 2;

-- Suppliers: their newest orders.
INSERT dbo.Notifications (UserId, Title, Message, Type, LinkUrl, IsRead, CreatedAt, DedupKey)
SELECT su.UserId, N'New purchase order', CONCAT(N'Order ', p.Reference, N' was placed by the hub.'),
       60, CONCAT(N'~/Supplier/OrderDetails.aspx?id=', p.OrderId), CASE WHEN p.Status >= 2 THEN 1 ELSE 0 END, p.OrderedAt, NULL
FROM #PO p JOIN dbo.Suppliers su ON su.SupplierId = p.SupplierId WHERE p.o >= 5;

/* =====================================================================================================
   11. Daily business metrics for every day in the window (same formulas as ReportingServices.CaptureMetrics).
       Tutor stats are refreshed by the site's background sweep a few minutes after it starts.
   ===================================================================================================== */
MERGE dbo.DailyMetrics AS m
USING (
    SELECT d.D,
           ISNULL((SELECT SUM(Amount) FROM dbo.Payments p WHERE p.Status = 2 AND p.Type = 1 AND p.CreatedAt >= d.D AND p.CreatedAt < DATEADD(day, 1, d.D)), 0) AS Revenue,
           ISNULL((SELECT SUM(Amount) FROM dbo.Payments p WHERE p.Status = 2 AND p.Type = 2 AND p.CreatedAt >= d.D AND p.CreatedAt < DATEADD(day, 1, d.D)), 0) AS Refunds,
           (SELECT COUNT(*) FROM dbo.Payments p WHERE p.Status = 2 AND p.Type = 1 AND p.CreatedAt >= d.D AND p.CreatedAt < DATEADD(day, 1, d.D)) AS PaymentsCount,
           (SELECT COUNT(*) FROM dbo.Bookings b WHERE b.CreatedAt >= d.D AND b.CreatedAt < DATEADD(day, 1, d.D) AND b.Status <> 7) AS BookingsCreated,
           (SELECT COUNT(*) FROM dbo.TutoringSessions s WHERE s.SessionDate = d.D) AS SessionsCompleted,
           (SELECT COUNT(*) FROM dbo.Rentals r WHERE r.RentedAt >= d.D AND r.RentedAt < DATEADD(day, 1, d.D) AND r.Status IN (2, 3, 4)) AS RentalsStarted,
           (SELECT COUNT(*) FROM dbo.Rentals r WHERE r.ReturnedAt >= d.D AND r.ReturnedAt < DATEADD(day, 1, d.D)) AS RentalsReturned,
           (SELECT COUNT(*) FROM dbo.Rentals r WHERE r.Status = 2 AND r.DueDate < d.D) AS OverdueRentals,
           (SELECT COUNT(*) FROM dbo.Sales s WHERE s.Status = 2 AND s.CompletedAt >= d.D AND s.CompletedAt < DATEADD(day, 1, d.D)) AS BooksSold,
           ISNULL((SELECT SUM(Amount) FROM dbo.Fines f WHERE f.CreatedAt >= d.D AND f.CreatedAt < DATEADD(day, 1, d.D)), 0) AS FinesIssued,
           (SELECT COUNT(*) FROM dbo.UserRoles ur WHERE ur.RoleId = @RoleStudent AND ur.AssignedAt >= d.D AND ur.AssignedAt < DATEADD(day, 1, d.D)) AS NewStudents,
           (SELECT COUNT(*) FROM dbo.Tutors t WHERE t.CreatedAt >= d.D AND t.CreatedAt < DATEADD(day, 1, d.D)) AS NewTutors,
           CASE WHEN d.D < @Today THEN DATEADD(minute, 1439, CAST(d.D AS datetime2(0))) ELSE @Now END AS CapturedAt
    FROM #Day d WHERE d.D <= @Today
) AS s ON m.MetricDate = s.D
WHEN MATCHED THEN UPDATE SET Revenue = s.Revenue, Refunds = s.Refunds, PaymentsCount = s.PaymentsCount, BookingsCreated = s.BookingsCreated,
     SessionsCompleted = s.SessionsCompleted, RentalsStarted = s.RentalsStarted, RentalsReturned = s.RentalsReturned,
     OverdueRentals = s.OverdueRentals, BooksSold = s.BooksSold, FinesIssued = s.FinesIssued, NewStudents = s.NewStudents,
     NewTutors = s.NewTutors, CapturedAt = s.CapturedAt
WHEN NOT MATCHED THEN INSERT (MetricDate, Revenue, Refunds, PaymentsCount, BookingsCreated, SessionsCompleted, RentalsStarted, RentalsReturned,
                              OverdueRentals, BooksSold, FinesIssued, NewStudents, NewTutors, CapturedAt)
     VALUES (s.D, s.Revenue, s.Refunds, s.PaymentsCount, s.BookingsCreated, s.SessionsCompleted, s.RentalsStarted, s.RentalsReturned,
             s.OverdueRentals, s.BooksSold, s.FinesIssued, s.NewStudents, s.NewTutors, s.CapturedAt);

/* =====================================================================================================
   12. Done
   ===================================================================================================== */
DECLARE @Summary nvarchar(500) = CONCAT(
    N'Sample history ', FORMAT(@From, 'd MMM yyyy', 'en-GB'), N' to ', FORMAT(@Until, 'd MMM yyyy', 'en-GB'), N': ',
    (SELECT COUNT(*) FROM #NewStu), N' students, ', (SELECT COUNT(*) FROM #NewTut), N' tutors, ',
    (SELECT COUNT(*) FROM dbo.Books b WHERE b.Isbn IN (SELECT Isbn FROM #NewBook)), N' titles, ',
    (SELECT COUNT(*) FROM #B), N' bookings, ', (SELECT COUNT(*) FROM #PP), N' postponements, ',
    (SELECT COUNT(*) FROM #Rv), N' reviews, ', (SELECT COUNT(*) FROM #TW), N' timesheets, ',
    (SELECT COUNT(*) FROM #RO), N' rentals, ', (SELECT COUNT(*) FROM #SO), N' sales, ',
    (SELECT COUNT(*) FROM #P), N' payments, ', (SELECT COUNT(*) FROM #F WHERE RefundAt IS NOT NULL), N' refunds.');

INSERT dbo.AuditLogs (ActorUserId, ActorRole, Action, EntityType, EntityId, PreviousState, NewState, Details, IpAddress, CreatedAt)
VALUES (NULL, N'System', N'HistorySeeded', N'System', N'SeedData.sql', NULL, NULL, @Summary, NULL, @Now);

COMMIT TRANSACTION;

PRINT @Summary;
PRINT N'New students and tutors sign in with the password StudyLink@2026.';
