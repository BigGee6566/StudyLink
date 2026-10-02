using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using StudyLink.Data;
using StudyLink.Data.Entities;
using StudyLink.Data.Seed;
using StudyLink.Services.DTOs;
using StudyLink.Services.Infrastructure;
using StudyLink.Services.Interfaces;
using StudyLink.Services.Policies;
using StudyLink.Services.Security;
using StudyLink.Services.Validation;

namespace StudyLink.Services.Implementations
{
    /// <summary>Shared account plumbing: creating users, loading profiles, uniqueness checks.</summary>
    internal static class Accounts
    {
        public const string DuplicateAccount = "That username or email address is already registered.";

        public static string CheckUnique(StudyLinkDbContext db, string userName, string email, int? excludeUserId = null)
        {
            userName = (userName ?? string.Empty).Trim();
            email = (email ?? string.Empty).Trim();
            if (db.Users.Any(u => u.UserName == userName && u.UserId != excludeUserId))
                return "That username is already taken.";
            if (db.Users.Any(u => u.Email == email && u.UserId != excludeUserId))
                return "That email address is already registered.";
            return null;
        }

        public static string CheckDetails(StudyLinkDbContext db, PersonalDetails d)
        {
            var error = UserValidator.CheckPersonalDetails(d.UserName, d.FirstName, d.LastName, d.Email, d.CellNumber);
            if (error != null) return error;
            if (!db.Campuses.Any(c => c.CampusId == d.CampusId && c.IsActive))
                return "Select your campus.";
            return null;
        }

        public static User NewUser(PersonalDetails d, string password, DateTime now, int? createdBy)
        {
            // Accounts created by staff get an unguessable random password and must set their own
            // through the emailed link; nobody ever sees or types a temporary password.
            var hashed = PasswordHasher.Create(password ?? Convert.ToBase64String(PasswordHasher.RandomBytes(32)));
            return new User
            {
                UserName = d.UserName.Trim(),
                FirstName = d.FirstName.Trim(),
                LastName = d.LastName.Trim(),
                Email = d.Email.Trim(),
                CellNumber = UserValidator.NormaliseCell(d.CellNumber),
                Gender = d.Gender,
                CampusId = d.CampusId,
                PasswordHash = hashed.Hash,
                PasswordSalt = hashed.Salt,
                HashIterations = hashed.Iterations,
                IsActive = true,
                MustChangePassword = password == null,
                CreatedAt = now,
                CreatedByUserId = createdBy
            };
        }

        public static void AddRole(StudyLinkDbContext db, User user, string role, DateTime now, int? assignedBy)
        {
            var roleId = db.Roles.Where(r => r.Name == role).Select(r => r.RoleId).Single();
            user.UserRoles.Add(new UserRole { RoleId = roleId, AssignedAt = now, AssignedByUserId = assignedBy });
        }

        public static IList<string> RolesOf(StudyLinkDbContext db, int userId) =>
            db.UserRoles.Where(ur => ur.UserId == userId).Select(ur => ur.Role.Name).ToList();

        public static bool IsLastActiveAdmin(StudyLinkDbContext db, int userId) =>
            db.UserRoles.Any(ur => ur.UserId == userId && ur.Role.Name == RoleNames.SystemAdmin)
            && !db.UserRoles.Any(ur => ur.UserId != userId && ur.Role.Name == RoleNames.SystemAdmin && ur.User.IsActive);

        public static string GenerateEmployeeNumber(StudyLinkDbContext db, string prefix)
        {
            for (var i = 0; i < 20; i++)
            {
                var candidate = prefix + ReferenceGenerator.RandomCode(5);
                if (!db.Staff.Any(s => s.EmployeeNumber == candidate) && !db.Managers.Any(m => m.EmployeeNumber == candidate))
                    return candidate;
            }
            throw new InvalidOperationException("Could not generate a unique employee number.");
        }

        public static UserDetail Detail(StudyLinkDbContext db, int userId)
        {
            var u = db.Users.AsNoTracking().Include(x => x.Campus).Include(x => x.CreatedBy)
                .FirstOrDefault(x => x.UserId == userId);
            if (u == null) return null;

            var d = new UserDetail
            {
                UserId = u.UserId, UserName = u.UserName, FirstName = u.FirstName, LastName = u.LastName,
                Email = u.Email, CellNumber = u.CellNumber, Gender = u.Gender, CampusId = u.CampusId,
                Campus = u.Campus.Name, IsActive = u.IsActive, LockoutEnd = u.LockoutEnd,
                FailedLoginCount = u.FailedLoginCount, LastLoginAt = u.LastLoginAt, CreatedAt = u.CreatedAt,
                CreatedBy = u.CreatedBy == null ? null : u.CreatedBy.FirstName + " " + u.CreatedBy.LastName,
                Roles = RolesOf(db, userId)
            };

            var s = db.Students.AsNoTracking().Include(x => x.StudyLevel).FirstOrDefault(x => x.UserId == userId);
            if (s != null)
            {
                d.StudentId = s.StudentId; d.StudentNumber = s.StudentNumber; d.StudyLevelId = s.StudyLevelId;
                d.StudyLevel = s.StudyLevel?.Name; d.Faculty = s.Faculty;
            }
            var t = db.Tutors.AsNoTracking().FirstOrDefault(x => x.UserId == userId);
            if (t != null)
            {
                d.TutorId = t.TutorId; d.TutorStatus = t.VerificationStatus; d.Qualification = t.Qualification;
                d.ExperienceYears = t.ExperienceYears; d.HourlyRate = t.HourlyRate; d.Bio = t.Bio;
            }
            var st = db.Staff.AsNoTracking().FirstOrDefault(x => x.UserId == userId);
            if (st != null)
            {
                d.StaffId = st.StaffId; d.EmployeeNumber = st.EmployeeNumber; d.JobTitle = st.JobTitle;
            }
            var sp = db.Suppliers.AsNoTracking().FirstOrDefault(x => x.UserId == userId);
            if (sp != null)
            {
                d.SupplierId = sp.SupplierId; d.CompanyName = sp.CompanyName; d.ContactPerson = sp.ContactPerson;
                d.VatNumber = sp.VatNumber; d.SupplierPhone = sp.Phone; d.SupplierAddress = sp.Address;
            }
            var m = db.Managers.AsNoTracking().FirstOrDefault(x => x.UserId == userId);
            if (m != null)
            {
                d.ManagerId = m.ManagerId;
                d.EmployeeNumber = d.EmployeeNumber ?? m.EmployeeNumber;
            }
            d.AdminId = db.Admins.Where(x => x.UserId == userId).Select(x => (int?)x.AdminId).FirstOrDefault();
            return d;
        }

        public static string Html(string s) => WebUtility.HtmlEncode(s ?? string.Empty);
    }

    public sealed class AuthService : IAuthService
    {
        private const string GenericFailure = "Incorrect username or password.";

        private readonly StudyLinkDbContext _db;
        private readonly ISettingsService _settings;
        private readonly IAuditService _audit;
        private readonly IClock _clock;

        public AuthService(StudyLinkDbContext db, ISettingsService settings, IAuditService audit, IClock clock)
        {
            _db = db;
            _settings = settings;
            _audit = audit;
            _clock = clock;
        }

        public SignInResult SignIn(string identifier, string password, string ipAddress)
        {
            identifier = (identifier ?? string.Empty).Trim();
            if (identifier.Length == 0 || string.IsNullOrEmpty(password))
                return new SignInResult { Error = "Enter your username (or email) and password." };

            var now = _clock.Now;
            var user = _db.Users.FirstOrDefault(u => u.UserName == identifier || u.Email == identifier);
            if (user == null)
            {
                // Spend the same time as a real check so response timing does not reveal unknown users.
                PasswordHasher.Verify(password, new byte[32], new byte[16], PasswordHasher.DefaultIterations);
                return new SignInResult { Error = GenericFailure };
            }

            // AT-05: a locked account gets the same message as a wrong password.
            if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > now)
                return new SignInResult { Error = GenericFailure };

            if (!PasswordHasher.Verify(password, user.PasswordHash, user.PasswordSalt, user.HashIterations))
            {
                user.FailedLoginCount++;
                if (user.FailedLoginCount >= _settings.GetInt(SettingKeys.LockoutThreshold, 5))
                {
                    user.LockoutEnd = now.AddMinutes(_settings.GetInt(SettingKeys.LockoutMinutes, 15));
                    user.FailedLoginCount = 0;
                    _audit.Record(new Actor(user.UserId, user.UserName, RolesOf(user.UserId), ipAddress),
                        "AccountLocked", "User", user.UserId, details: "Too many failed sign-in attempts.");
                }
                _db.SaveChanges();
                return new SignInResult { Error = GenericFailure };
            }

            if (!user.IsActive)
                return new SignInResult { Error = "This account is not active. Please contact the hub." };

            user.FailedLoginCount = 0;
            user.LockoutEnd = null;
            user.LastLoginAt = now;
            if (PasswordHasher.NeedsRehash(user.HashIterations))
            {
                var fresh = PasswordHasher.Create(password);
                user.PasswordHash = fresh.Hash;
                user.PasswordSalt = fresh.Salt;
                user.HashIterations = fresh.Iterations;
            }
            _db.SaveChanges();

            return new SignInResult
            {
                Succeeded = true,
                UserId = user.UserId,
                UserName = user.UserName,
                DisplayName = user.FirstName + " " + user.LastName,
                Roles = RolesOf(user.UserId),
                MustChangePassword = user.MustChangePassword
            };
        }

        public bool IsActive(int userId) => _db.Users.Any(u => u.UserId == userId && u.IsActive);

        public IList<string> RolesOf(int userId) => Accounts.RolesOf(_db, userId);

        public ServiceResult ChangePassword(Actor actor, string currentPassword, string newPassword, string confirmPassword)
        {
            var user = _db.Users.Find(actor.UserId);
            if (user == null)
                return ServiceResult.Fail("Account not found.");
            if (!PasswordHasher.Verify(currentPassword, user.PasswordHash, user.PasswordSalt, user.HashIterations))
                return ServiceResult.Fail("Your current password is incorrect.");
            var error = UserValidator.CheckPassword(newPassword, confirmPassword);
            if (error != null)
                return ServiceResult.Fail(error);
            if (currentPassword == newPassword)
                return ServiceResult.Fail("Choose a password different from your current one.");

            var hashed = PasswordHasher.Create(newPassword);
            user.PasswordHash = hashed.Hash;
            user.PasswordSalt = hashed.Salt;
            user.HashIterations = hashed.Iterations;
            user.MustChangePassword = false;
            user.UpdatedAt = _clock.Now;
            _audit.Record(actor, "PasswordChanged", "User", user.UserId);
            _db.SaveChanges();
            return ServiceResult.Ok();
        }

        public bool SetupRequired() => !_db.UserRoles.Any(ur => ur.Role.Name == RoleNames.SystemAdmin);

        public ServiceResult CreateFirstAdmin(PersonalDetails details, string password, string confirm, string ipAddress)
        {
            if (!SetupRequired())
                return ServiceResult.Fail("Setup has already been completed. Sign in instead.");

            var error = Accounts.CheckDetails(_db, details)
                        ?? UserValidator.CheckPassword(password, confirm)
                        ?? Accounts.CheckUnique(_db, details.UserName, details.Email);
            if (error != null)
                return ServiceResult.Fail(error);

            var now = _clock.Now;
            var user = Accounts.NewUser(details, password, now, null);
            Accounts.AddRole(_db, user, RoleNames.SystemAdmin, now, null);
            _db.Users.Add(user);
            _db.Admins.Add(new Admin { User = user });
            _db.SaveChanges();
            _audit.Record(new Actor(user.UserId, user.UserName, new[] { RoleNames.SystemAdmin }, ipAddress),
                "FirstAdminCreated", "User", user.UserId);
            _db.SaveChanges();
            return ServiceResult.Ok();
        }
    }

    public sealed class RegistrationService : IRegistrationService
    {
        private readonly StudyLinkDbContext _db;
        private readonly IAuditService _audit;
        private readonly INotificationService _notifications;
        private readonly IClock _clock;
        private readonly RateLimiter _limiter;

        public RegistrationService(StudyLinkDbContext db, IAuditService audit, INotificationService notifications,
                                   IClock clock, RateLimiter limiter = null)
        {
            _db = db;
            _audit = audit;
            _notifications = notifications;
            _clock = clock;
            _limiter = limiter ?? RateLimiter.Registration;
        }

        public string CheckAccountAvailable(string userName, string email) => Accounts.CheckUnique(_db, userName, email);

        public ServiceResult<int> RegisterStudent(StudentRegistration r, string ipAddress)
        {
            if (!_limiter.TryAcquire("register:" + ipAddress))
                return ServiceResult<int>.Fail("Too many registration attempts. Please wait 15 minutes and try again.");

            var error = Accounts.CheckDetails(_db, r)
                        ?? (UserValidator.IsValidStudentNumber(r.StudentNumber) ? null : UserValidator.StudentNumberMessage)
                        ?? (TextRules.TooLong(r.Faculty, 100) ? "Faculty is limited to 100 characters." : null)
                        ?? (r.StudyLevelId.HasValue && !_db.StudyLevels.Any(l => l.StudyLevelId == r.StudyLevelId) ? "Select a valid year of study." : null)
                        ?? UserValidator.CheckPassword(r.Password, r.ConfirmPassword)
                        ?? (r.AcceptedTerms ? null : "Please accept the terms and the POPIA privacy notice to continue.")
                        ?? Accounts.CheckUnique(_db, r.UserName, r.Email);
            if (error != null)
                return ServiceResult<int>.Fail(error);

            var number = r.StudentNumber.Trim().ToUpperInvariant();
            if (_db.Students.Any(s => s.StudentNumber == number))
                return ServiceResult<int>.Fail("That student number is already registered. Use 'Forgot password' if it is yours.");

            var now = _clock.Now;
            var user = Accounts.NewUser(r, r.Password, now, null);
            Accounts.AddRole(_db, user, RoleNames.Student, now, null);
            _db.Users.Add(user);
            _db.Students.Add(new Student
            {
                User = user, StudentNumber = number, StudyLevelId = r.StudyLevelId,
                Faculty = TextRules.Clean(r.Faculty, 100), CreatedAt = now
            });

            try
            {
                _db.SaveChanges();
            }
            catch (DbUpdateException ex) when (DbErrors.IsUniqueViolation(ex))
            {
                return ServiceResult<int>.Fail(Accounts.DuplicateAccount);
            }

            _audit.Record(new Actor(user.UserId, user.UserName, new[] { RoleNames.Student }, ipAddress),
                "StudentRegistered", "User", user.UserId, newState: number);
            _db.SaveChanges();
            return ServiceResult<int>.Ok(user.UserId);
        }

        public ServiceResult<int> RegisterTutor(TutorRegistration r, string ipAddress)
        {
            if (!_limiter.TryAcquire("register:" + ipAddress))
                return ServiceResult<int>.Fail("Too many registration attempts. Please wait 15 minutes and try again.");

            var subjects = (r.Subjects ?? new List<SubjectChoice>())
                .GroupBy(s => new { s.SubjectId, s.StudyLevelId }).Select(g => g.First()).ToList();
            var subjectIds = subjects.Select(s => s.SubjectId).Distinct().ToList();
            var levelIds = subjects.Select(s => s.StudyLevelId).Distinct().ToList();

            var error = Accounts.CheckDetails(_db, r)
                        ?? (string.IsNullOrWhiteSpace(r.Qualification) || r.Qualification.Trim().Length > 100 ? "Enter your highest qualification (100 characters max)." : null)
                        ?? (r.ExperienceYears < 0 || r.ExperienceYears > 70 ? "Years of experience must be between 0 and 70." : null)
                        ?? (r.HourlyRate < 0 || r.HourlyRate > 100000 ? "Hourly rate must be between R0 and R100 000." : null)
                        ?? (TextRules.TooLong(r.Bio, 1000) ? "Your bio is limited to 1000 characters." : null)
                        ?? (subjects.Count == 0 ? "Choose at least one subject you can tutor." : null)
                        ?? (_db.Subjects.Count(s => subjectIds.Contains(s.SubjectId) && s.IsActive) != subjectIds.Count ? "One of the chosen subjects is no longer offered." : null)
                        ?? (_db.StudyLevels.Count(l => levelIds.Contains(l.StudyLevelId)) != levelIds.Count ? "Select a valid level for each subject." : null)
                        ?? UserValidator.CheckPassword(r.Password, r.ConfirmPassword)
                        ?? (r.AcceptedTerms ? null : "Please accept the terms and the POPIA privacy notice to continue.")
                        ?? Accounts.CheckUnique(_db, r.UserName, r.Email);
            if (error != null)
                return ServiceResult<int>.Fail(error);

            var now = _clock.Now;
            var user = Accounts.NewUser(r, r.Password, now, null);
            Accounts.AddRole(_db, user, RoleNames.Tutor, now, null);
            _db.Users.Add(user);

            var tutor = new Tutor
            {
                User = user,
                Qualification = r.Qualification.Trim(),
                ExperienceYears = r.ExperienceYears,
                HourlyRate = Math.Round(r.HourlyRate, 2),
                Bio = TextRules.Clean(r.Bio, 1000),
                VerificationStatus = TutorVerificationStatus.PendingVerification,
                CreatedAt = now
            };
            _db.Tutors.Add(tutor);
            foreach (var s in subjects)
                _db.TutorSubjects.Add(new TutorSubject { Tutor = tutor, SubjectId = s.SubjectId, StudyLevelId = s.StudyLevelId, CreatedAt = now });
            _db.TutorVerifications.Add(new TutorVerification { Tutor = tutor, SubmittedAt = now, Decision = VerificationDecision.Pending });

            try
            {
                _db.SaveChanges();
            }
            catch (DbUpdateException ex) when (DbErrors.IsUniqueViolation(ex))
            {
                return ServiceResult<int>.Fail(Accounts.DuplicateAccount);
            }

            _audit.Record(new Actor(user.UserId, user.UserName, new[] { RoleNames.Tutor }, ipAddress),
                "TutorRegistered", "Tutor", tutor.TutorId, newState: "PendingVerification");
            _notifications.NotifyHolders(Permission.VerifyTutors, NotificationType.TutorRegistrationSubmitted,
                "New tutor awaiting verification",
                user.FirstName + " " + user.LastName + " registered as a tutor (" + tutor.Qualification + ").",
                "~/Operations/TutorVerification.aspx?id=" + tutor.TutorId);
            _db.SaveChanges();
            return ServiceResult<int>.Ok(user.UserId);
        }
    }

    public sealed class UserManagementService : IUserManagementService
    {
        private static readonly string[] AssignableRoles = { RoleNames.Staff, RoleNames.Manager, RoleNames.SystemAdmin };

        private readonly StudyLinkDbContext _db;
        private readonly IAuditService _audit;
        private readonly INotificationService _notifications;
        private readonly IPasswordResetService _resets;
        private readonly IClock _clock;

        public UserManagementService(StudyLinkDbContext db, IAuditService audit, INotificationService notifications,
                                     IPasswordResetService resets, IClock clock)
        {
            _db = db;
            _audit = audit;
            _notifications = notifications;
            _resets = resets;
            _clock = clock;
        }

        /// <summary>
        /// Which accounts an actor may see and manage: Admin → everyone; Manager → staff, suppliers
        /// and students; Staff → students only. Null means "no restriction".
        /// </summary>
        private string[] Scope(Actor actor)
        {
            if (RolePermissions.Has(actor, Permission.ManageAllUsers)) return null;
            if (RolePermissions.Has(actor, Permission.CreateStaffAndSuppliers)) return new[] { RoleNames.Staff, RoleNames.Supplier, RoleNames.Student };
            if (RolePermissions.Has(actor, Permission.CaptureStudents)) return new[] { RoleNames.Student };
            throw new AccessDeniedException("You do not have permission to manage accounts.");
        }

        private bool InScope(Actor actor, int userId)
        {
            var scope = Scope(actor);
            if (scope == null) return true;
            var roles = Accounts.RolesOf(_db, userId);
            // Every role the target holds must be within scope: a Manager cannot manage an admin who is also staff.
            return roles.Count > 0 && roles.All(scope.Contains);
        }

        public IList<string> CreatableRoles(Actor actor) => RolePermissions.CreatableRoles(actor).ToList();

        public PagedResult<UserRow> Search(Actor actor, UserFilter f)
        {
            var scope = Scope(actor);
            var q = _db.Users.AsNoTracking().AsQueryable();
            if (scope != null)
                q = q.Where(u => u.UserRoles.Any() && u.UserRoles.All(ur => scope.Contains(ur.Role.Name)));
            if (!string.IsNullOrWhiteSpace(f.Role))
                q = q.Where(u => u.UserRoles.Any(ur => ur.Role.Name == f.Role));
            if (f.Active.HasValue)
                q = q.Where(u => u.IsActive == f.Active.Value);
            if (f.CampusId.HasValue)
                q = q.Where(u => u.CampusId == f.CampusId.Value);
            if (!string.IsNullOrWhiteSpace(f.Search))
            {
                var s = f.Search.Trim();
                q = q.Where(u => u.UserName.Contains(s) || u.Email.Contains(s) || u.FirstName.Contains(s)
                                 || u.LastName.Contains(s) || _db.Students.Any(st => st.UserId == u.UserId && st.StudentNumber.Contains(s)));
            }

            var total = q.Count();
            var page = Math.Max(1, f.Page);
            var now = _clock.Now;
            var rows = q.OrderBy(u => u.LastName).ThenBy(u => u.FirstName)
                .Skip((page - 1) * f.PageSize).Take(f.PageSize)
                .Select(u => new
                {
                    u.UserId, u.UserName, u.FirstName, u.LastName, u.Email, u.CellNumber, Campus = u.Campus.Name,
                    Roles = u.UserRoles.Select(ur => ur.Role.Name), u.IsActive, u.LockoutEnd, u.LastLoginAt, u.CreatedAt,
                    StudentNumber = _db.Students.Where(st => st.UserId == u.UserId).Select(st => st.StudentNumber).FirstOrDefault(),
                    Company = _db.Suppliers.Where(sp => sp.UserId == u.UserId).Select(sp => sp.CompanyName).FirstOrDefault(),
                    TutorStatus = _db.Tutors.Where(t => t.UserId == u.UserId).Select(t => (TutorVerificationStatus?)t.VerificationStatus).FirstOrDefault()
                }).ToList()
                .Select(u => new UserRow
                {
                    UserId = u.UserId, UserName = u.UserName, FullName = u.FirstName + " " + u.LastName,
                    Email = u.Email, CellNumber = u.CellNumber, Campus = u.Campus,
                    Roles = string.Join(", ", u.Roles.Select(RoleNames.DisplayName)),
                    IsActive = u.IsActive, IsLocked = u.LockoutEnd.HasValue && u.LockoutEnd > now,
                    LastLoginAt = u.LastLoginAt, CreatedAt = u.CreatedAt,
                    Extra = u.StudentNumber ?? u.Company ?? (u.TutorStatus.HasValue ? u.TutorStatus.Value.ToString() : null)
                }).ToList();
            return new PagedResult<UserRow>(rows, total, page, f.PageSize);
        }

        public UserDetail Get(Actor actor, int userId)
        {
            if (!InScope(actor, userId))
                throw new AccessDeniedException("You cannot view this account.");
            return Accounts.Detail(_db, userId);
        }

        public ServiceResult<int> CreateAccount(Actor actor, CreateAccountRequest r, string setupLinkBase)
        {
            if (!RolePermissions.CreatableRoles(actor).Contains(r.Role))
                throw new AccessDeniedException("You cannot create " + RoleNames.DisplayName(r.Role) + " accounts.");

            var error = Accounts.CheckDetails(_db, r) ?? Accounts.CheckUnique(_db, r.UserName, r.Email) ?? CheckRoleDetails(r, null);
            if (error != null)
                return ServiceResult<int>.Fail(error);

            var now = _clock.Now;
            var user = Accounts.NewUser(r, null, now, actor.UserId);
            Accounts.AddRole(_db, user, r.Role, now, actor.UserId);
            _db.Users.Add(user);
            AddProfile(user, r);

            try
            {
                _db.SaveChanges();
            }
            catch (DbUpdateException ex) when (DbErrors.IsUniqueViolation(ex))
            {
                return ServiceResult<int>.Fail("That username, email, student number or employee number is already in use.");
            }

            _audit.Record(actor, "UserCreated", "User", user.UserId, newState: r.Role + ": " + user.UserName);
            _notifications.Notify(user.UserId, NotificationType.AccountCreated, "Welcome to StudyLink",
                "Your " + RoleNames.DisplayName(r.Role) + " account was created by " + actor.UserName + ".");
            _db.SaveChanges();
            _resets.IssueSetupLink(actor, user.UserId, setupLinkBase);
            return ServiceResult<int>.Ok(user.UserId);
        }

        private string CheckRoleDetails(CreateAccountRequest r, int? existingUserId)
        {
            switch (r.Role)
            {
                case RoleNames.Supplier:
                    if (string.IsNullOrWhiteSpace(r.CompanyName) || r.CompanyName.Trim().Length > 120) return "Enter the supplier's company name.";
                    if (string.IsNullOrWhiteSpace(r.ContactPerson) || r.ContactPerson.Trim().Length > 100) return "Enter a contact person.";
                    if (string.IsNullOrWhiteSpace(r.Phone) || r.Phone.Trim().Length > 20) return "Enter the supplier's phone number.";
                    if (TextRules.TooLong(r.VatNumber, 20) || TextRules.TooLong(r.Address, 250)) return "VAT number or address is too long.";
                    return null;
                case RoleNames.Student:
                    if (!UserValidator.IsValidStudentNumber(r.StudentNumber)) return UserValidator.StudentNumberMessage;
                    var number = r.StudentNumber.Trim().ToUpperInvariant();
                    if (_db.Students.Any(s => s.StudentNumber == number && s.UserId != existingUserId)) return "That student number is already registered.";
                    return null;
                case RoleNames.Staff:
                case RoleNames.Manager:
                    if (TextRules.TooLong(r.EmployeeNumber, 20) || TextRules.TooLong(r.JobTitle, 60)) return "Employee number or job title is too long.";
                    var emp = TextRules.Clean(r.EmployeeNumber, 20);
                    if (emp != null && (_db.Staff.Any(s => s.EmployeeNumber == emp && s.UserId != existingUserId)
                                        || _db.Managers.Any(m => m.EmployeeNumber == emp && m.UserId != existingUserId)))
                        return "That employee number is already in use.";
                    return null;
                default:
                    return null;
            }
        }

        private void AddProfile(User user, CreateAccountRequest r)
        {
            switch (r.Role)
            {
                case RoleNames.Staff:
                    _db.Staff.Add(new StaffMember
                    {
                        User = user,
                        EmployeeNumber = TextRules.Clean(r.EmployeeNumber, 20) ?? Accounts.GenerateEmployeeNumber(_db, "ST"),
                        JobTitle = TextRules.Clean(r.JobTitle, 60) ?? "Hub Assistant"
                    });
                    break;
                case RoleNames.Manager:
                    _db.Managers.Add(new Manager { User = user, EmployeeNumber = TextRules.Clean(r.EmployeeNumber, 20) ?? Accounts.GenerateEmployeeNumber(_db, "MG") });
                    break;
                case RoleNames.SystemAdmin:
                    _db.Admins.Add(new Admin { User = user });
                    break;
                case RoleNames.Supplier:
                    _db.Suppliers.Add(new Supplier
                    {
                        User = user, CompanyName = r.CompanyName.Trim(), ContactPerson = r.ContactPerson.Trim(),
                        VatNumber = TextRules.Clean(r.VatNumber, 20), Phone = r.Phone.Trim(), Address = TextRules.Clean(r.Address, 250)
                    });
                    break;
                case RoleNames.Student:
                    _db.Students.Add(new Student
                    {
                        User = user, StudentNumber = r.StudentNumber.Trim().ToUpperInvariant(),
                        StudyLevelId = r.StudyLevelId, Faculty = TextRules.Clean(r.Faculty, 100), CreatedAt = _clock.Now
                    });
                    break;
            }
        }

        public ServiceResult UpdateDetails(Actor actor, int userId, CreateAccountRequest r)
        {
            if (!InScope(actor, userId))
                throw new AccessDeniedException("You cannot edit this account.");

            var user = _db.Users.Find(userId);
            if (user == null) return ServiceResult.Fail("Account not found.");
            r.UserName = user.UserName; // usernames never change

            var error = Accounts.CheckDetails(_db, r) ?? Accounts.CheckUnique(_db, r.UserName, r.Email, userId);
            if (error != null) return ServiceResult.Fail(error);

            var before = $"{user.FirstName} {user.LastName}, {user.Email}, {user.CellNumber}";
            user.FirstName = r.FirstName.Trim();
            user.LastName = r.LastName.Trim();
            user.Email = r.Email.Trim();
            user.CellNumber = UserValidator.NormaliseCell(r.CellNumber);
            user.Gender = r.Gender;
            user.CampusId = r.CampusId;
            user.UpdatedAt = _clock.Now;

            var student = _db.Students.FirstOrDefault(s => s.UserId == userId);
            if (student != null && !string.IsNullOrWhiteSpace(r.StudentNumber))
            {
                r.Role = RoleNames.Student;
                error = CheckRoleDetails(r, userId);
                if (error != null) return ServiceResult.Fail(error);
                student.StudentNumber = r.StudentNumber.Trim().ToUpperInvariant();
                student.StudyLevelId = r.StudyLevelId;
                student.Faculty = TextRules.Clean(r.Faculty, 100);
            }
            var supplier = _db.Suppliers.FirstOrDefault(s => s.UserId == userId);
            if (supplier != null && !string.IsNullOrWhiteSpace(r.CompanyName))
            {
                r.Role = RoleNames.Supplier;
                error = CheckRoleDetails(r, userId);
                if (error != null) return ServiceResult.Fail(error);
                supplier.CompanyName = r.CompanyName.Trim();
                supplier.ContactPerson = r.ContactPerson.Trim();
                supplier.VatNumber = TextRules.Clean(r.VatNumber, 20);
                supplier.Phone = r.Phone.Trim();
                supplier.Address = TextRules.Clean(r.Address, 250);
            }
            var staff = _db.Staff.FirstOrDefault(s => s.UserId == userId);
            if (staff != null && r.JobTitle != null)
                staff.JobTitle = TextRules.Clean(r.JobTitle, 60) ?? staff.JobTitle;

            _audit.Record(actor, "UserUpdated", "User", userId, before, $"{user.FirstName} {user.LastName}, {user.Email}, {user.CellNumber}");
            try
            {
                _db.SaveChanges();
            }
            catch (DbUpdateException ex) when (DbErrors.IsUniqueViolation(ex))
            {
                return ServiceResult.Fail("That email or student number is already in use.");
            }
            return ServiceResult.Ok();
        }

        public ServiceResult SetActive(Actor actor, int userId, bool active, string reason)
        {
            if (!InScope(actor, userId))
                throw new AccessDeniedException("You cannot change this account.");
            if (userId == actor.UserId)
                return ServiceResult.Fail("You cannot deactivate your own account.");

            var user = _db.Users.Find(userId);
            if (user == null) return ServiceResult.Fail("Account not found.");
            if (user.IsActive == active) return ServiceResult.Ok();
            if (!active && Accounts.IsLastActiveAdmin(_db, userId))
                return ServiceResult.Fail("This is the last active System Admin and cannot be deactivated.");
            if (!active && string.IsNullOrWhiteSpace(reason))
                return ServiceResult.Fail("Give a reason for deactivating the account.");

            user.IsActive = active;
            user.UpdatedAt = _clock.Now;
            _audit.Record(actor, active ? "UserActivated" : "UserDeactivated", "User", userId,
                active ? "Inactive" : "Active", active ? "Active" : "Inactive", TextRules.Clean(reason, 500));
            if (!active)
                _notifications.Notify(userId, NotificationType.AccountDeactivated, "Account deactivated",
                    "Your StudyLink account was deactivated. Contact the hub if you believe this is a mistake.");
            _db.SaveChanges();
            return ServiceResult.Ok();
        }

        public ServiceResult AssignRole(Actor actor, int userId, string role)
        {
            RolePermissions.Demand(actor, Permission.AssignRoles);
            if (!AssignableRoles.Contains(role))
                return ServiceResult.Fail("Only Staff, Manager and System Admin roles can be assigned here. Suppliers are created as supplier accounts; students and tutors register themselves.");

            var user = _db.Users.Include(u => u.UserRoles.Select(ur => ur.Role)).FirstOrDefault(u => u.UserId == userId);
            if (user == null) return ServiceResult.Fail("Account not found.");
            if (user.UserRoles.Any(ur => ur.Role.Name == role)) return ServiceResult.Ok();
            if (user.UserRoles.Any(ur => ur.Role.Name == RoleNames.Student || ur.Role.Name == RoleNames.Tutor || ur.Role.Name == RoleNames.Supplier))
                return ServiceResult.Fail("Student, tutor and supplier accounts cannot also hold staff roles. Create a separate staff account.");

            var now = _clock.Now;
            Accounts.AddRole(_db, user, role, now, actor.UserId);
            if (role == RoleNames.Staff && !_db.Staff.Any(s => s.UserId == userId))
                _db.Staff.Add(new StaffMember { UserId = userId, EmployeeNumber = Accounts.GenerateEmployeeNumber(_db, "ST"), JobTitle = "Hub Assistant" });
            if (role == RoleNames.Manager && !_db.Managers.Any(m => m.UserId == userId))
                _db.Managers.Add(new Manager { UserId = userId, EmployeeNumber = Accounts.GenerateEmployeeNumber(_db, "MG") });
            if (role == RoleNames.SystemAdmin && !_db.Admins.Any(a => a.UserId == userId))
                _db.Admins.Add(new Admin { UserId = userId });

            _audit.Record(actor, "RoleAssigned", "User", userId, newState: role);
            _notifications.Notify(userId, NotificationType.Administrative, "New role assigned",
                "You were given the " + RoleNames.DisplayName(role) + " role. Sign out and in again to see its menu.");
            _db.SaveChanges();
            return ServiceResult.Ok();
        }

        public ServiceResult RemoveRole(Actor actor, int userId, string role)
        {
            RolePermissions.Demand(actor, Permission.AssignRoles);
            var link = _db.UserRoles.Include(ur => ur.Role).FirstOrDefault(ur => ur.UserId == userId && ur.Role.Name == role);
            if (link == null) return ServiceResult.Ok();
            if (_db.UserRoles.Count(ur => ur.UserId == userId) == 1)
                return ServiceResult.Fail("An account must keep at least one role. Deactivate it instead.");
            if (role == RoleNames.SystemAdmin && (userId == actor.UserId || Accounts.IsLastActiveAdmin(_db, userId)))
                return ServiceResult.Fail("You cannot remove your own System Admin role or that of the last active System Admin.");

            _db.UserRoles.Remove(link);
            _audit.Record(actor, "RoleRemoved", "User", userId, previousState: role);
            _db.SaveChanges();
            return ServiceResult.Ok();
        }

        public ServiceResult Unlock(Actor actor, int userId)
        {
            if (!RolePermissions.Has(actor, Permission.ManageSecurity) && !RolePermissions.Has(actor, Permission.ManageAllUsers))
                throw new AccessDeniedException("You cannot unlock accounts.");
            var user = _db.Users.Find(userId);
            if (user == null) return ServiceResult.Fail("Account not found.");
            user.LockoutEnd = null;
            user.FailedLoginCount = 0;
            _audit.Record(actor, "AccountUnlocked", "User", userId);
            _db.SaveChanges();
            return ServiceResult.Ok();
        }

        public IList<RoleRow> Roles(Actor actor)
        {
            if (!RolePermissions.Has(actor, Permission.AssignRoles) && !RolePermissions.Has(actor, Permission.ManageAllUsers))
                throw new AccessDeniedException("You cannot view role administration.");
            var counts = _db.Roles.Select(r => new
            {
                r.Name, r.Description,
                Total = r.UserRoles.Count(),
                Active = r.UserRoles.Count(ur => ur.User.IsActive)
            }).ToList();
            return RoleNames.All.Select(name =>
            {
                var c = counts.FirstOrDefault(x => x.Name == name);
                return new RoleRow
                {
                    Name = name,
                    DisplayName = RoleNames.DisplayName(name),
                    Description = c?.Description,
                    TotalMembers = c?.Total ?? 0,
                    ActiveMembers = c?.Active ?? 0,
                    Permissions = RolePermissions.For(name).Select(p => p.ToString()).ToList(),
                    SelfRegister = name == RoleNames.Student || name == RoleNames.Tutor
                };
            }).ToList();
        }

        public IList<UserRow> LockedAccounts(Actor actor)
        {
            if (!RolePermissions.Has(actor, Permission.ManageSecurity))
                throw new AccessDeniedException("You cannot view security information.");
            var now = _clock.Now;
            return _db.Users.AsNoTracking().Where(u => u.LockoutEnd > now)
                .Select(u => new UserRow
                {
                    UserId = u.UserId, UserName = u.UserName, FullName = u.FirstName + " " + u.LastName,
                    Email = u.Email, IsActive = u.IsActive, IsLocked = true, LastLoginAt = u.LockoutEnd
                }).ToList();
        }
    }

    public sealed class ProfileService : IProfileService
    {
        private readonly StudyLinkDbContext _db;
        private readonly IAuditService _audit;
        private readonly INotificationService _notifications;
        private readonly IClock _clock;

        public ProfileService(StudyLinkDbContext db, IAuditService audit, INotificationService notifications, IClock clock)
        {
            _db = db;
            _audit = audit;
            _notifications = notifications;
            _clock = clock;
        }

        public UserDetail Get(int userId) => Accounts.Detail(_db, userId);

        public ServiceResult SetPhoto(Actor actor, byte[] image)
        {
            var error = ImageRules.Check(image);
            if (error != null) return ServiceResult.Fail(error);
            var photo = _db.UserPhotos.Find(actor.UserId) ?? _db.UserPhotos.Add(new UserPhoto { UserId = actor.UserId });
            photo.Photo = image;
            photo.ContentType = ImageRules.Detect(image);
            photo.UpdatedAt = _clock.Now;
            _audit.Record(actor, "ProfilePhotoChanged", "User", actor.UserId);
            _db.SaveChanges();
            return ServiceResult.Ok();
        }

        public ServiceResult RemovePhoto(Actor actor)
        {
            var photo = _db.UserPhotos.Find(actor.UserId);
            if (photo == null) return ServiceResult.Ok();
            _db.UserPhotos.Remove(photo);
            _audit.Record(actor, "ProfilePhotoRemoved", "User", actor.UserId);
            _db.SaveChanges();
            return ServiceResult.Ok();
        }

        public PhotoFile Photo(Actor viewer, int userId)
        {
            var photo = _db.UserPhotos.AsNoTracking().FirstOrDefault(p => p.UserId == userId);
            if (photo == null) return null;

            var owner = _db.Users.AsNoTracking().Where(u => u.UserId == userId)
                .Select(u => new
                {
                    u.IsActive,
                    Roles = u.UserRoles.Select(r => r.Role.Name),
                    TutorVerified = _db.Tutors.Any(t => t.UserId == userId && t.VerificationStatus == TutorVerificationStatus.Verified)
                }).Single();
            var ownerKind = AvatarPolicy.OwnerKind(owner.Roles.ToList(), owner.TutorVerified && owner.IsActive);
            var isOwn = viewer != null && viewer.UserId == userId;
            var viewerKinds = AvatarPolicy.ViewerKinds(viewer?.Roles);
            var tutorsOwner = viewer != null && viewerKinds.Contains(AvatarViewer.Tutor)
                              && _db.Bookings.Any(b => b.Tutor.UserId == viewer.UserId && b.Student.UserId == userId);

            if (!AvatarPolicy.CanView(isOwn, viewerKinds, ownerKind, tutorsOwner)) return null;
            return new PhotoFile
            {
                Data = photo.Photo, ContentType = photo.ContentType, UpdatedAt = photo.UpdatedAt,
                IsPublic = AvatarPolicy.IsPublic(ownerKind)
            };
        }

        public DateTime? PhotoVersion(int userId) =>
            _db.UserPhotos.Where(p => p.UserId == userId).Select(p => (DateTime?)p.UpdatedAt).FirstOrDefault();

        public IList<string> RequestableFields(int userId)
        {
            var fields = new List<string> { "First name", "Last name" };
            if (_db.Students.Any(s => s.UserId == userId)) fields.Add("Student number");
            return fields;
        }

        public ServiceResult UpdateContact(Actor actor, string email, string cellNumber, int campusId, Gender gender)
        {
            // BR-24: always the signed-in user's own row, never an id from the form.
            var user = _db.Users.Find(actor.UserId);
            if (user == null) return ServiceResult.Fail("Account not found.");
            if (!UserValidator.IsValidEmail(email)) return ServiceResult.Fail("Enter a valid email address.");
            if (!UserValidator.IsValidCell(cellNumber)) return ServiceResult.Fail("Enter a South African cell number, e.g. 0821234567.");
            if (!_db.Campuses.Any(c => c.CampusId == campusId && c.IsActive)) return ServiceResult.Fail("Select your campus.");
            var clash = Accounts.CheckUnique(_db, user.UserName, email, user.UserId);
            if (clash != null) return ServiceResult.Fail(clash);

            user.Email = email.Trim();
            user.CellNumber = UserValidator.NormaliseCell(cellNumber);
            user.CampusId = campusId;
            user.Gender = gender;
            user.UpdatedAt = _clock.Now;
            _db.SaveChanges();
            return ServiceResult.Ok();
        }

        public ServiceResult UpdateTutorDetails(Actor actor, string qualification, int experienceYears, decimal hourlyRate, string bio)
        {
            var tutor = _db.Tutors.FirstOrDefault(t => t.UserId == actor.UserId);
            if (tutor == null) return ServiceResult.Fail("Only tutors have tutoring details.");
            if (string.IsNullOrWhiteSpace(qualification) || qualification.Trim().Length > 100) return ServiceResult.Fail("Enter your highest qualification (100 characters max).");
            if (experienceYears < 0 || experienceYears > 70) return ServiceResult.Fail("Years of experience must be between 0 and 70.");
            if (hourlyRate < 0 || hourlyRate > 100000) return ServiceResult.Fail("Hourly rate must be between R0 and R100 000.");
            if (TextRules.TooLong(bio, 1000)) return ServiceResult.Fail("Your bio is limited to 1000 characters.");

            if (tutor.HourlyRate != hourlyRate)
                _audit.Record(actor, "TutorRateChanged", "Tutor", tutor.TutorId, tutor.HourlyRate.ToString("0.00"), hourlyRate.ToString("0.00"));
            tutor.Qualification = qualification.Trim();
            tutor.ExperienceYears = experienceYears;
            tutor.HourlyRate = Math.Round(hourlyRate, 2); // applies to new bookings only; existing ones keep their fee
            tutor.Bio = TextRules.Clean(bio, 1000);
            _db.SaveChanges();
            return ServiceResult.Ok();
        }

        public ServiceResult RequestChange(Actor actor, string fieldName, string requestedValue, string reason)
        {
            if (!RequestableFields(actor.UserId).Contains(fieldName))
                return ServiceResult.Fail("That field cannot be changed by request.");
            requestedValue = (requestedValue ?? string.Empty).Trim();
            if (requestedValue.Length == 0 || requestedValue.Length > 200)
                return ServiceResult.Fail("Enter the new value (200 characters max).");
            if (fieldName == "Student number" && !UserValidator.IsValidStudentNumber(requestedValue))
                return ServiceResult.Fail(UserValidator.StudentNumberMessage);
            if (_db.ProfileUpdateRequests.Any(p => p.UserId == actor.UserId && p.FieldName == fieldName && p.Status == RequestStatus.Pending))
                return ServiceResult.Fail("You already have a pending request for this field.");

            var detail = Accounts.Detail(_db, actor.UserId);
            var current = fieldName == "First name" ? detail.FirstName : fieldName == "Last name" ? detail.LastName : detail.StudentNumber;
            _db.ProfileUpdateRequests.Add(new ProfileUpdateRequest
            {
                UserId = actor.UserId, FieldName = fieldName, CurrentValue = current, RequestedValue = requestedValue,
                Reason = TextRules.Clean(reason, 300), CreatedAt = _clock.Now
            });
            _notifications.NotifyHolders(Permission.HandleSupport, NotificationType.ProfileUpdateRequested,
                "Profile update requested", detail.FullName + " asked to change their " + fieldName.ToLowerInvariant() + ".",
                "~/Operations/Support.aspx");
            _db.SaveChanges();
            return ServiceResult.Ok();
        }

        public IList<ProfileChangeRow> MyRequests(int userId) =>
            Query().Where(p => p.UserId == userId).OrderByDescending(p => p.CreatedAt).ToList();

        public IList<ProfileChangeRow> Pending(Actor actor)
        {
            RolePermissions.Demand(actor, Permission.HandleSupport);
            return Query().OrderBy(p => p.Status).ThenByDescending(p => p.CreatedAt).Take(200).ToList();
        }

        private IQueryable<ProfileChangeRow> Query() =>
            _db.ProfileUpdateRequests.AsNoTracking().Select(p => new ProfileChangeRow
            {
                RequestId = p.RequestId, UserId = p.UserId, UserName = p.User.UserName,
                FullName = p.User.FirstName + " " + p.User.LastName, FieldName = p.FieldName,
                CurrentValue = p.CurrentValue, RequestedValue = p.RequestedValue, Reason = p.Reason,
                Status = p.Status, CreatedAt = p.CreatedAt,
                HandledBy = p.HandledBy == null ? null : p.HandledBy.FirstName + " " + p.HandledBy.LastName,
                ResponseNote = p.ResponseNote
            });

        public ServiceResult Review(Actor actor, int requestId, bool approve, string note)
        {
            RolePermissions.Demand(actor, Permission.HandleSupport);
            var request = _db.ProfileUpdateRequests.Find(requestId);
            if (request == null || request.Status != RequestStatus.Pending)
                return ServiceResult.Fail("This request has already been handled.");
            if (!approve && string.IsNullOrWhiteSpace(note))
                return ServiceResult.Fail("Give a reason when declining a request.");

            if (approve)
            {
                var user = _db.Users.Find(request.UserId);
                switch (request.FieldName)
                {
                    case "First name":
                        if (request.RequestedValue.Length > 50) return ServiceResult.Fail("First names are limited to 50 characters.");
                        user.FirstName = request.RequestedValue;
                        break;
                    case "Last name":
                        if (request.RequestedValue.Length > 50) return ServiceResult.Fail("Last names are limited to 50 characters.");
                        user.LastName = request.RequestedValue;
                        break;
                    case "Student number":
                        var number = request.RequestedValue.ToUpperInvariant();
                        if (_db.Students.Any(s => s.StudentNumber == number && s.UserId != request.UserId))
                            return ServiceResult.Fail("That student number belongs to another account.");
                        _db.Students.First(s => s.UserId == request.UserId).StudentNumber = number;
                        break;
                }
                user.UpdatedAt = _clock.Now;
            }

            request.Status = approve ? RequestStatus.Completed : RequestStatus.Rejected;
            request.HandledByUserId = actor.UserId;
            request.HandledAt = _clock.Now;
            request.ResponseNote = TextRules.Clean(note, 200);
            _audit.Record(actor, approve ? "ProfileChangeApproved" : "ProfileChangeRejected", "User", request.UserId,
                request.CurrentValue, approve ? request.RequestedValue : null, request.FieldName);
            _notifications.Notify(request.UserId, NotificationType.ProfileUpdateReviewed,
                approve ? "Profile change approved" : "Profile change declined",
                "Your request to change your " + request.FieldName.ToLowerInvariant() + " was " + (approve ? "approved." : "declined: " + note));
            _db.SaveChanges();
            return ServiceResult.Ok();
        }
    }

    public sealed class PasswordResetService : IPasswordResetService
    {
        private readonly StudyLinkDbContext _db;
        private readonly IAuditService _audit;
        private readonly INotificationService _notifications;
        private readonly IEmailSender _email;
        private readonly ISettingsService _settings;
        private readonly IClock _clock;
        private readonly RateLimiter _limiter;

        public PasswordResetService(StudyLinkDbContext db, IAuditService audit, INotificationService notifications,
                                    IEmailSender email, ISettingsService settings, IClock clock, RateLimiter limiter = null)
        {
            _db = db;
            _audit = audit;
            _notifications = notifications;
            _email = email;
            _settings = settings;
            _clock = clock;
            _limiter = limiter ?? RateLimiter.PasswordReset;
        }

        public ServiceResult Request(string identifier, string message, string ipAddress)
        {
            identifier = (identifier ?? string.Empty).Trim();
            if (identifier.Length == 0 || identifier.Length > 100)
                return ServiceResult.Fail("Enter your username, email address or student number.");
            if (!_limiter.TryAcquire("reset:" + ipAddress))
                return ServiceResult.Fail("Too many requests. Please wait 15 minutes and try again.");

            var userId = _db.Users.Where(u => u.UserName == identifier || u.Email == identifier).Select(u => (int?)u.UserId).FirstOrDefault()
                         ?? _db.Students.Where(s => s.StudentNumber == identifier).Select(s => (int?)s.UserId).FirstOrDefault();

            // One open request per account is enough; repeated clicks do not flood staff.
            if (userId.HasValue && _db.PasswordResetRequests.Any(r => r.UserId == userId && r.Status == RequestStatus.Pending))
                return ServiceResult.Ok();

            _db.PasswordResetRequests.Add(new PasswordResetRequest
            {
                UserId = userId, Identifier = identifier, Message = TextRules.Clean(message, 300),
                CreatedAt = _clock.Now, RequestIp = ipAddress
            });
            _notifications.NotifyHolders(Permission.HandlePasswordResets, NotificationType.PasswordResetRequested,
                "Password reset requested", "A password reset was requested for \"" + identifier + "\".",
                "~/Operations/PasswordResets.aspx");
            _db.SaveChanges();
            return ServiceResult.Ok();
        }

        public IList<ResetRequestRow> List(Actor actor, RequestStatus? status)
        {
            RolePermissions.Demand(actor, Permission.HandlePasswordResets);
            var q = _db.PasswordResetRequests.AsNoTracking().AsQueryable();
            if (status.HasValue) q = q.Where(r => r.Status == status.Value);
            return q.OrderByDescending(r => r.RequestId).Take(200).Select(r => new ResetRequestRow
            {
                RequestId = r.RequestId, Identifier = r.Identifier, UserId = r.UserId,
                MatchedUser = r.User == null ? null : r.User.FirstName + " " + r.User.LastName + " (" + r.User.UserName + ")",
                MatchedEmail = r.User == null ? null : r.User.Email,
                Roles = r.User == null ? null : r.User.UserRoles.Select(ur => ur.Role.Name).FirstOrDefault(),
                Message = r.Message, Status = r.Status, CreatedAt = r.CreatedAt,
                HandledBy = r.HandledBy == null ? null : r.HandledBy.FirstName + " " + r.HandledBy.LastName,
                HandledAt = r.HandledAt, ResponseNote = r.ResponseNote, RequestIp = r.RequestIp
            }).ToList();
        }

        public ServiceResult Approve(Actor actor, int requestId, string note, string resetLinkBase)
        {
            RolePermissions.Demand(actor, Permission.HandlePasswordResets);
            var request = _db.PasswordResetRequests.Include(r => r.User).FirstOrDefault(r => r.RequestId == requestId);
            if (request == null || request.Status != RequestStatus.Pending)
                return ServiceResult.Fail("This request has already been handled.");
            if (request.User == null)
                return ServiceResult.Fail("No account matches this request. Reject it instead.");
            if (!request.User.IsActive)
                return ServiceResult.Fail("The account is deactivated. Reactivate it before resetting the password.");
            if (request.UserId == actor.UserId)
                return ServiceResult.Fail("You cannot approve your own password reset.");

            request.Status = RequestStatus.Approved;
            request.HandledByUserId = actor.UserId;
            request.HandledAt = _clock.Now;
            request.ResponseNote = TextRules.Clean(note, 200);
            var link = IssueToken(request.User, request.RequestId, TimeSpan.FromMinutes(_settings.GetInt(SettingKeys.ResetTokenMinutes, 60)), resetLinkBase);
            _audit.Record(actor, "PasswordResetApproved", "User", request.UserId, details: "Request " + requestId);
            _db.SaveChanges();

            _email.Send(request.User.Email, "Reset your StudyLink password",
                "<p>Hello " + Accounts.Html(request.User.FirstName) + ",</p><p>Hub staff have approved your password reset. "
                + "Use the link below within " + _settings.GetInt(SettingKeys.ResetTokenMinutes, 60) + " minutes to choose a new password:</p>"
                + "<p><a href=\"" + link + "\">" + Accounts.Html(link) + "</a></p><p>If you did not ask for this, contact the hub.</p>");
            return ServiceResult.Ok();
        }

        public ServiceResult Reject(Actor actor, int requestId, string note)
        {
            RolePermissions.Demand(actor, Permission.HandlePasswordResets);
            var request = _db.PasswordResetRequests.Find(requestId);
            if (request == null || request.Status != RequestStatus.Pending)
                return ServiceResult.Fail("This request has already been handled.");
            if (string.IsNullOrWhiteSpace(note))
                return ServiceResult.Fail("Give a reason for rejecting the request.");
            request.Status = RequestStatus.Rejected;
            request.HandledByUserId = actor.UserId;
            request.HandledAt = _clock.Now;
            request.ResponseNote = TextRules.Clean(note, 200);
            _audit.Record(actor, "PasswordResetRejected", "PasswordResetRequest", requestId, details: request.ResponseNote);
            _db.SaveChanges();
            return ServiceResult.Ok();
        }

        public void IssueSetupLink(Actor actor, int userId, string resetLinkBase)
        {
            var user = _db.Users.Find(userId);
            if (user == null) return;
            var link = IssueToken(user, null, TimeSpan.FromDays(3), resetLinkBase);
            _db.SaveChanges();
            _email.Send(user.Email, "Your StudyLink account is ready",
                "<p>Hello " + Accounts.Html(user.FirstName) + ",</p><p>A StudyLink account was created for you (username <b>"
                + Accounts.Html(user.UserName) + "</b>). Choose your password within 3 days using this link:</p>"
                + "<p><a href=\"" + link + "\">" + Accounts.Html(link) + "</a></p>");
        }

        private string IssueToken(User user, int? requestId, TimeSpan lifetime, string linkBase)
        {
            var now = _clock.Now;
            // Only the newest link works: older unused tokens for this user are closed.
            foreach (var old in _db.PasswordResetTokens.Where(t => t.UserId == user.UserId && t.UsedAt == null))
                old.UsedAt = now;

            var token = ReferenceGenerator.NewToken(out var hash);
            _db.PasswordResetTokens.Add(new PasswordResetToken
            {
                UserId = user.UserId, TokenHash = hash, CreatedAt = now, ExpiresAt = now.Add(lifetime), RequestId = requestId
            });
            return (linkBase ?? string.Empty) + Uri.EscapeDataString(token);
        }

        private PasswordResetToken FindValid(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return null;
            var hash = ReferenceGenerator.HashToken(token);
            var now = _clock.Now;
            var match = _db.PasswordResetTokens.Include(t => t.User).FirstOrDefault(t => t.TokenHash == hash);
            return match != null && match.UsedAt == null && match.ExpiresAt > now && match.User.IsActive ? match : null;
        }

        public bool IsTokenValid(string token) => FindValid(token) != null;

        public ServiceResult ResetWithToken(string token, string newPassword, string confirmPassword, string ipAddress)
        {
            var match = FindValid(token);
            if (match == null)
                return ServiceResult.Fail("This link is invalid or has expired. Ask the hub for a new one.");
            var error = UserValidator.CheckPassword(newPassword, confirmPassword);
            if (error != null)
                return ServiceResult.Fail(error);

            var now = _clock.Now;
            var hashed = PasswordHasher.Create(newPassword);
            var user = match.User;
            user.PasswordHash = hashed.Hash;
            user.PasswordSalt = hashed.Salt;
            user.HashIterations = hashed.Iterations;
            user.MustChangePassword = false;
            user.LockoutEnd = null;
            user.FailedLoginCount = 0;
            user.UpdatedAt = now;
            match.UsedAt = now;
            if (match.RequestId.HasValue)
            {
                var request = _db.PasswordResetRequests.Find(match.RequestId.Value);
                if (request != null) request.Status = RequestStatus.Completed;
            }
            _audit.Record(new Actor(user.UserId, user.UserName, Accounts.RolesOf(_db, user.UserId), ipAddress), "PasswordReset", "User", user.UserId);
            _db.SaveChanges();
            return ServiceResult.Ok();
        }
    }

    public sealed class TutorVerificationService : ITutorVerificationService
    {
        private readonly StudyLinkDbContext _db;
        private readonly IAuditService _audit;
        private readonly INotificationService _notifications;
        private readonly IClock _clock;

        public TutorVerificationService(StudyLinkDbContext db, IAuditService audit, INotificationService notifications, IClock clock)
        {
            _db = db;
            _audit = audit;
            _notifications = notifications;
            _clock = clock;
        }

        public IList<VerificationRow> Queue(Actor actor, TutorVerificationStatus? status)
        {
            RolePermissions.Demand(actor, Permission.VerifyTutors);
            var q = _db.Tutors.AsNoTracking().AsQueryable();
            if (status.HasValue) q = q.Where(t => t.VerificationStatus == status.Value);
            return q.OrderBy(t => t.CreatedAt).Select(t => new
            {
                t.TutorId, t.UserId, t.User.FirstName, t.User.LastName, t.User.Email, Campus = t.User.Campus.Name,
                t.Qualification, t.ExperienceYears, t.HourlyRate, t.VerificationStatus, t.CreatedAt,
                Subjects = t.Subjects.Select(s => s.Subject.Name + " (" + s.StudyLevel.Name + ")"),
                Submitted = t.Verifications.OrderByDescending(v => v.VerificationId).Select(v => (DateTime?)v.SubmittedAt).FirstOrDefault()
            }).ToList().Select(t => new VerificationRow
            {
                TutorId = t.TutorId, UserId = t.UserId, FullName = t.FirstName + " " + t.LastName, Email = t.Email,
                Campus = t.Campus, Qualification = t.Qualification, ExperienceYears = t.ExperienceYears,
                HourlyRate = t.HourlyRate, Status = t.VerificationStatus, SubmittedAt = t.Submitted ?? t.CreatedAt,
                Subjects = string.Join(", ", t.Subjects)
            }).ToList();
        }

        public TutorApplication Get(Actor actor, int tutorId)
        {
            var row = Queue(actor, null).FirstOrDefault(r => r.TutorId == tutorId);
            if (row == null) return null;
            var tutor = _db.Tutors.AsNoTracking().Include(t => t.User).First(t => t.TutorId == tutorId);
            return new TutorApplication
            {
                Summary = row,
                CellNumber = tutor.User.CellNumber,
                Bio = tutor.Bio,
                History = _db.TutorVerifications.AsNoTracking().Where(v => v.TutorId == tutorId)
                    .OrderByDescending(v => v.VerificationId)
                    .Select(v => new VerificationHistoryRow
                    {
                        SubmittedAt = v.SubmittedAt, Decision = v.Decision, ReviewedAt = v.ReviewedAt,
                        ReviewedBy = v.ReviewedBy == null ? null : v.ReviewedBy.FirstName + " " + v.ReviewedBy.LastName,
                        IdentityChecked = v.IdentityChecked, QualificationChecked = v.QualificationChecked, Notes = v.Notes
                    }).ToList()
            };
        }

        public ServiceResult Decide(Actor actor, int tutorId, bool approve, bool identityChecked, bool qualificationChecked, string notes)
        {
            RolePermissions.Demand(actor, Permission.VerifyTutors);
            var tutor = _db.Tutors.Include(t => t.User).FirstOrDefault(t => t.TutorId == tutorId);
            if (tutor == null) return ServiceResult.Fail("Tutor not found.");
            if (tutor.VerificationStatus != TutorVerificationStatus.PendingVerification)
                return ServiceResult.Fail("This tutor is not awaiting verification.");
            if (tutor.UserId == actor.UserId)
                return ServiceResult.Fail("You cannot verify your own tutor account.");
            if (approve && (!identityChecked || !qualificationChecked))
                return ServiceResult.Fail("Confirm that you checked the tutor's identity and qualification before approving.");
            if (!approve && string.IsNullOrWhiteSpace(notes))
                return ServiceResult.Fail("Explain why the application is rejected; the tutor will see this.");

            var now = _clock.Now;
            var verification = _db.TutorVerifications.Where(v => v.TutorId == tutorId && v.Decision == VerificationDecision.Pending)
                                   .OrderByDescending(v => v.VerificationId).FirstOrDefault()
                               ?? _db.TutorVerifications.Add(new TutorVerification { TutorId = tutorId, SubmittedAt = tutor.CreatedAt });
            verification.Decision = approve ? VerificationDecision.Approved : VerificationDecision.Rejected;
            verification.ReviewedByUserId = actor.UserId;
            verification.ReviewedByStaffId = _db.Staff.Where(s => s.UserId == actor.UserId).Select(s => (int?)s.StaffId).FirstOrDefault();
            verification.ReviewedAt = now;
            verification.IdentityChecked = identityChecked;
            verification.QualificationChecked = qualificationChecked;
            verification.Notes = TextRules.Clean(notes, 500);

            tutor.VerificationStatus = approve ? TutorVerificationStatus.Verified : TutorVerificationStatus.Rejected;
            tutor.VerifiedAt = approve ? now : (DateTime?)null;

            _audit.Record(actor, approve ? "TutorVerified" : "TutorRejected", "Tutor", tutorId,
                "PendingVerification", tutor.VerificationStatus.ToString(), verification.Notes);
            _notifications.Notify(tutor.UserId,
                approve ? NotificationType.TutorVerificationApproved : NotificationType.TutorVerificationRejected,
                approve ? "You are now a verified tutor" : "Tutor application not approved",
                approve ? "Your tutor profile was verified. Publish your availability so students can book you."
                        : "Your application was not approved: " + verification.Notes,
                approve ? "~/Tutor/Availability.aspx" : "~/Account/Profile.aspx");
            _db.SaveChanges();
            return ServiceResult.Ok();
        }

        public ServiceResult Suspend(Actor actor, int tutorId, string reason) =>
            ChangeStatus(actor, tutorId, TutorVerificationStatus.Verified, TutorVerificationStatus.Suspended, reason, "TutorSuspended",
                "Your tutor profile was suspended: ");

        public ServiceResult Reinstate(Actor actor, int tutorId, string reason) =>
            ChangeStatus(actor, tutorId, TutorVerificationStatus.Suspended, TutorVerificationStatus.Verified, reason, "TutorReinstated",
                "Your tutor profile was reinstated. ");

        private ServiceResult ChangeStatus(Actor actor, int tutorId, TutorVerificationStatus from, TutorVerificationStatus to,
                                           string reason, string action, string message)
        {
            RolePermissions.Demand(actor, Permission.VerifyTutors);
            if (string.IsNullOrWhiteSpace(reason))
                return ServiceResult.Fail("A reason is required.");
            var tutor = _db.Tutors.Find(tutorId);
            if (tutor == null || tutor.VerificationStatus != from)
                return ServiceResult.Fail("The tutor's status has changed. Refresh and try again.");
            tutor.VerificationStatus = to;
            _audit.Record(actor, action, "Tutor", tutorId, from.ToString(), to.ToString(), TextRules.Clean(reason, 500));
            _notifications.Notify(tutor.UserId, NotificationType.Administrative, "Tutor status changed", message + reason.Trim());
            _db.SaveChanges();
            return ServiceResult.Ok();
        }
    }

    public sealed class LookupService : ILookupService
    {
        private readonly StudyLinkDbContext _db;

        public LookupService(StudyLinkDbContext db)
        {
            _db = db;
        }

        public IList<LookupItem> Campuses() =>
            _db.Campuses.AsNoTracking().Where(c => c.IsActive).OrderBy(c => c.CampusId)
                .Select(c => new LookupItem { Id = c.CampusId, Name = c.Name }).ToList();

        public IList<LookupItem> Subjects(bool activeOnly = true) =>
            _db.Subjects.AsNoTracking().Where(s => !activeOnly || s.IsActive).OrderBy(s => s.Name)
                .Select(s => new LookupItem { Id = s.SubjectId, Name = s.Name }).ToList();

        public IList<LookupItem> StudyLevels() =>
            _db.StudyLevels.AsNoTracking().OrderBy(l => l.SortOrder)
                .Select(l => new LookupItem { Id = l.StudyLevelId, Name = l.Name }).ToList();

        public IList<LookupItem> Categories(bool activeOnly = true) =>
            _db.Categories.AsNoTracking().Where(c => !activeOnly || c.IsActive).OrderBy(c => c.Name)
                .Select(c => new LookupItem { Id = c.CategoryId, Name = c.Name }).ToList();

        public IList<LookupItem> Suppliers() =>
            _db.Suppliers.AsNoTracking().Where(s => s.User.IsActive).OrderBy(s => s.CompanyName)
                .Select(s => new LookupItem { Id = s.SupplierId, Name = s.CompanyName }).ToList();
    }
}
