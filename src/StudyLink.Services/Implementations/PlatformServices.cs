using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Security.Cryptography;
using StudyLink.Data;
using StudyLink.Data.Entities;
using StudyLink.Data.Seed;
using StudyLink.Services.DTOs;
using StudyLink.Services.Infrastructure;
using StudyLink.Services.Interfaces;
using StudyLink.Services.Policies;
using StudyLink.Services.Security;

namespace StudyLink.Services.Implementations
{
    public sealed class SettingsService : ISettingsService
    {
        private static readonly HashSet<string> NumericKeys = new HashSet<string>
        {
            SettingKeys.RentalPeriodDays, SettingKeys.MaxRenewals, SettingKeys.RentalHoldMinutes,
            SettingKeys.BookingHoldMinutes, SettingKeys.CancellationCutoffHours, SettingKeys.MaxSessionHours,
            SettingKeys.AvailabilityMaxDaysAhead, SettingKeys.PostponementMaxDaysAhead,
            SettingKeys.TutorCommissionPercent, SettingKeys.LockoutThreshold, SettingKeys.LockoutMinutes,
            SettingKeys.ResetTokenMinutes
        };

        private readonly StudyLinkDbContext _db;
        private readonly IAuditService _audit;
        private readonly IClock _clock;
        private Dictionary<string, string> _cache;

        public SettingsService(StudyLinkDbContext db, IAuditService audit, IClock clock)
        {
            _db = db;
            _audit = audit;
            _clock = clock;
        }

        private Dictionary<string, string> Values =>
            _cache ?? (_cache = _db.SystemSettings.AsNoTracking().ToDictionary(s => s.SettingKey, s => s.Value));

        public string GetString(string key, string fallback) =>
            Values.TryGetValue(key, out var v) && v != null ? v : fallback;

        public int GetInt(string key, int fallback) =>
            int.TryParse(GetString(key, null), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : fallback;

        public decimal GetDecimal(string key, decimal fallback) =>
            decimal.TryParse(GetString(key, null), NumberStyles.Number, CultureInfo.InvariantCulture, out var v) ? v : fallback;

        public IList<SystemSetting> List() => _db.SystemSettings.AsNoTracking().OrderBy(s => s.SettingKey).ToList();

        public ServiceResult Update(Actor actor, string key, string value)
        {
            RolePermissions.Demand(actor, Permission.ManageSettings);

            var setting = _db.SystemSettings.Find(key);
            if (setting == null)
                return ServiceResult.Fail("Unknown setting.");

            value = (value ?? string.Empty).Trim();
            if (NumericKeys.Contains(key))
            {
                if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) || n < 0 || n > 100000)
                    return ServiceResult.Fail("This setting must be a whole number between 0 and 100000.");
                if (key == SettingKeys.TutorCommissionPercent && n > 100)
                    return ServiceResult.Fail("Commission cannot exceed 100%.");
                if ((key == SettingKeys.RentalPeriodDays || key == SettingKeys.LockoutThreshold) && n == 0)
                    return ServiceResult.Fail("This setting must be at least 1.");
            }
            if (value.Length > 200)
                return ServiceResult.Fail("Setting values are limited to 200 characters.");
            if (setting.Value == value)
                return ServiceResult.Ok();

            _audit.Record(actor, "SettingChanged", "SystemSetting", key, setting.Value, value);
            setting.Value = value;
            setting.UpdatedAt = _clock.Now;
            setting.UpdatedByUserId = actor.UserId;
            _db.SaveChanges();
            _cache = null;
            return ServiceResult.Ok();
        }
    }

    public sealed class AuditService : IAuditService
    {
        private readonly StudyLinkDbContext _db;
        private readonly IClock _clock;

        public AuditService(StudyLinkDbContext db, IClock clock)
        {
            _db = db;
            _clock = clock;
        }

        public void Record(Actor actor, string action, string entityType, object entityId,
                           string previousState = null, string newState = null, string details = null)
        {
            actor = actor ?? Actor.System;
            _db.AuditLogs.Add(new AuditLog
            {
                ActorUserId = actor.IsSystem ? (int?)null : actor.UserId,
                ActorRole = actor.PrimaryRole,
                Action = Truncate(action, 50),
                EntityType = Truncate(entityType, 40),
                EntityId = Truncate(Convert.ToString(entityId, CultureInfo.InvariantCulture) ?? "-", 40),
                PreviousState = Truncate(previousState, 1000),
                NewState = Truncate(newState, 1000),
                Details = Truncate(details, 500),
                IpAddress = Truncate(actor.IpAddress, 45),
                CreatedAt = _clock.Now
            });
        }

        public PagedResult<AuditRow> Search(Actor actor, AuditFilter filter)
        {
            RolePermissions.Demand(actor, Permission.ViewAuditLogs);
            var q = _db.AuditLogs.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(filter.Action)) q = q.Where(a => a.Action == filter.Action);
            if (!string.IsNullOrWhiteSpace(filter.EntityType)) q = q.Where(a => a.EntityType == filter.EntityType);
            if (!string.IsNullOrWhiteSpace(filter.Actor))
                q = q.Where(a => a.Actor.UserName.Contains(filter.Actor) || a.Actor.LastName.Contains(filter.Actor));
            if (filter.From.HasValue) q = q.Where(a => a.CreatedAt >= filter.From.Value);
            if (filter.To.HasValue)
            {
                var to = filter.To.Value.Date.AddDays(1);
                q = q.Where(a => a.CreatedAt < to);
            }

            var total = q.Count();
            var page = Math.Max(1, filter.Page);
            var rows = q.OrderByDescending(a => a.AuditId)
                .Skip((page - 1) * filter.PageSize).Take(filter.PageSize)
                .Select(a => new AuditRow
                {
                    AuditId = a.AuditId,
                    CreatedAt = a.CreatedAt,
                    ActorName = a.Actor == null ? "System" : a.Actor.FirstName + " " + a.Actor.LastName + " (" + a.Actor.UserName + ")",
                    ActorRole = a.ActorRole,
                    Action = a.Action,
                    EntityType = a.EntityType,
                    EntityId = a.EntityId,
                    PreviousState = a.PreviousState,
                    NewState = a.NewState,
                    Details = a.Details,
                    IpAddress = a.IpAddress
                }).ToList();
            return new PagedResult<AuditRow>(rows, total, page, filter.PageSize);
        }

        public IList<string> ActionNames(Actor actor)
        {
            RolePermissions.Demand(actor, Permission.ViewAuditLogs);
            return _db.AuditLogs.Select(a => a.Action).Distinct().OrderBy(a => a).ToList();
        }

        internal static string Truncate(string value, int max) =>
            value == null ? null : value.Length <= max ? value : value.Substring(0, max);
    }

    public sealed class NotificationService : INotificationService
    {
        private readonly StudyLinkDbContext _db;
        private readonly IClock _clock;

        public NotificationService(StudyLinkDbContext db, IClock clock)
        {
            _db = db;
            _clock = clock;
        }

        public void Notify(int userId, NotificationType type, string title, string message, string link = null, string dedupKey = null)
        {
            if (userId <= 0)
                return;
            if (dedupKey != null && (_db.Notifications.Any(n => n.DedupKey == dedupKey)
                                     || _db.Notifications.Local.Any(n => n.DedupKey == dedupKey)))
                return;

            _db.Notifications.Add(new Notification
            {
                UserId = userId,
                Type = type,
                Title = AuditService.Truncate(title, 150),
                Message = AuditService.Truncate(message, 500),
                LinkUrl = AuditService.Truncate(link, 300),
                DedupKey = AuditService.Truncate(dedupKey, 100),
                CreatedAt = _clock.Now
            });
        }

        public void NotifyHolders(Permission permission, NotificationType type, string title, string message, string link = null)
        {
            var roles = RolePermissions.RolesWith(permission).ToList();
            var userIds = _db.UserRoles
                .Where(ur => roles.Contains(ur.Role.Name) && ur.User.IsActive)
                .Select(ur => ur.UserId).Distinct().ToList();
            foreach (var id in userIds)
                Notify(id, type, title, message, link);
        }

        public PagedResult<NotificationRow> ForUser(int userId, int page, int pageSize)
        {
            var q = _db.Notifications.AsNoTracking().Where(n => n.UserId == userId);
            var total = q.Count();
            page = Math.Max(1, page);
            var rows = Project(q.OrderByDescending(n => n.NotificationId).Skip((page - 1) * pageSize).Take(pageSize)).ToList();
            return new PagedResult<NotificationRow>(rows, total, page, pageSize);
        }

        public IList<NotificationRow> Latest(int userId, int count) =>
            Project(_db.Notifications.AsNoTracking().Where(n => n.UserId == userId)
                .OrderByDescending(n => n.NotificationId).Take(count)).ToList();

        public int UnreadCount(int userId) => _db.Notifications.Count(n => n.UserId == userId && !n.IsRead);

        public void MarkRead(int userId, int notificationId)
        {
            // Ownership is part of the lookup, so nobody can mark someone else's notification.
            var n = _db.Notifications.FirstOrDefault(x => x.NotificationId == notificationId && x.UserId == userId);
            if (n == null || n.IsRead)
                return;
            n.IsRead = true;
            _db.SaveChanges();
        }

        public void MarkAllRead(int userId)
        {
            _db.Database.ExecuteSqlCommand(
                "UPDATE dbo.Notifications SET IsRead = 1 WHERE UserId = @p0 AND IsRead = 0", userId);
        }

        private static IQueryable<NotificationRow> Project(IQueryable<Notification> q) =>
            q.Select(n => new NotificationRow
            {
                NotificationId = n.NotificationId,
                Type = n.Type,
                Title = n.Title,
                Message = n.Message,
                LinkUrl = n.LinkUrl,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt
            });
    }

    /// <summary>
    /// Sends through System.Net.Mail using the &lt;mailSettings&gt; in Web.config. In development
    /// that is a pickup directory (App_Data\MailDrop), so every email can be opened as a .eml file.
    /// </summary>
    public sealed class SmtpEmailSender : IEmailSender
    {
        private readonly string _from;

        public SmtpEmailSender(string fromAddress)
        {
            _from = fromAddress;
        }

        public void Send(string to, string subject, string htmlBody)
        {
            if (string.IsNullOrWhiteSpace(to))
                return;
            try
            {
                using (var message = new MailMessage(_from, to.Trim(), subject, htmlBody) { IsBodyHtml = true })
                using (var client = new SmtpClient())
                {
                    client.Send(message);
                }
            }
            catch (Exception ex)
            {
                // Email is a courtesy copy of what the app already shows; never fail the action for it.
                Trace.TraceError("StudyLink email to {0} failed: {1}", to, ex.Message);
            }
        }
    }

    public sealed class DiskFileStore : IFileStore
    {
        private readonly string _root;

        public DiskFileStore(string root)
        {
            _root = root;
        }

        public string Save(string area, byte[] content, string extension)
        {
            var folder = Path.Combine(_root, SafeSegment(area));
            Directory.CreateDirectory(folder);
            // Stored name is random: the uploader's file name never becomes a path.
            var name = Guid.NewGuid().ToString("N") + SafeExtension(extension);
            File.WriteAllBytes(Path.Combine(folder, name), content);
            return name;
        }

        public byte[] Read(string area, string storedName)
        {
            var path = Path.Combine(_root, SafeSegment(area), SafeSegment(storedName));
            return File.Exists(path) ? File.ReadAllBytes(path) : null;
        }

        private static string SafeSegment(string s)
        {
            var name = Path.GetFileName(s ?? string.Empty);
            if (string.IsNullOrEmpty(name) || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                throw new ArgumentException("Invalid file name.");
            return name;
        }

        private static string SafeExtension(string ext)
        {
            ext = (ext ?? string.Empty).Trim().ToLowerInvariant();
            if (!ext.StartsWith(".")) ext = "." + ext;
            return ext.Length <= 6 && ext.Skip(1).All(char.IsLetterOrDigit) ? ext : ".bin";
        }
    }

    /// <summary>
    /// Simulated card gateway (D-6). Validates format with <see cref="CardValidator"/>, approves
    /// valid cards, and declines the published test card 4000 0000 0000 0002 so the failure path
    /// can be demonstrated. Card numbers never leave this method.
    /// </summary>
    public sealed class SimulatedCardGateway : IPaymentGateway
    {
        public const string DeclineTestCard = "4000000000000002";
        private readonly IClock _clock;

        public SimulatedCardGateway(IClock clock)
        {
            _clock = clock;
        }

        public string Name => "StudyLink simulated gateway";

        public GatewayResult Charge(decimal amount, CardDetails card, string description)
        {
            var check = CardValidator.Check(card?.Number, card?.Expiry, card?.Cvv, card?.Holder, _clock.Today);
            if (!check.IsValid)
                return new GatewayResult { Approved = false, DeclineReason = check.Error };

            var digits = new string(card.Number.Where(char.IsDigit).ToArray());
            if (digits == DeclineTestCard)
                return new GatewayResult { Approved = false, Brand = check.Brand, Last4 = check.Last4, DeclineReason = "The card was declined by the bank." };
            if (amount <= 0)
                return new GatewayResult { Approved = false, DeclineReason = "Nothing to charge." };

            return new GatewayResult { Approved = true, Brand = check.Brand, Last4 = check.Last4, Reference = "SIM-" + ReferenceGenerator.RandomCode(12) };
        }

        public GatewayResult Refund(string originalReference, decimal amount) =>
            new GatewayResult { Approved = amount > 0, Reference = "SIMR-" + ReferenceGenerator.RandomCode(12) };
    }

    /// <summary>Human-readable, unguessable references: BK-260930-7K3Q9D, SL-PAY-20260930-X4…</summary>
    public static class ReferenceGenerator
    {
        private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // no 0/O/1/I confusion

        public static string New(string prefix, DateTime now) =>
            prefix + "-" + now.ToString("yyMMdd", CultureInfo.InvariantCulture) + "-" + RandomCode(6);

        public static string Payment(DateTime now) =>
            "SL-PAY-" + now.ToString("yyyyMMdd", CultureInfo.InvariantCulture) + "-" + RandomCode(6);

        public static string RandomCode(int length)
        {
            var bytes = PasswordHasher.RandomBytes(length);
            var chars = new char[length];
            for (var i = 0; i < length; i++)
                chars[i] = Alphabet[bytes[i] % Alphabet.Length];
            return new string(chars);
        }

        /// <summary>URL-safe secret for password-reset links, plus its SHA-256 (the only part stored).</summary>
        public static string NewToken(out byte[] hash)
        {
            var token = Convert.ToBase64String(PasswordHasher.RandomBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
            hash = HashToken(token);
            return token;
        }

        public static byte[] HashToken(string token)
        {
            using (var sha = SHA256.Create())
                return sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(token ?? string.Empty));
        }
    }
}
