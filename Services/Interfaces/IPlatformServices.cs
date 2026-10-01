using System.Collections.Generic;
using StudyLink.Data.Entities;
using StudyLink.Services.DTOs;
using StudyLink.Services.Infrastructure;
using StudyLink.Services.Security;

namespace StudyLink.Services.Interfaces
{
    /// <summary>Typed access to SystemSettings with the seeded defaults as fall-backs.</summary>
    public interface ISettingsService
    {
        int GetInt(string key, int fallback);
        decimal GetDecimal(string key, decimal fallback);
        string GetString(string key, string fallback);
        IList<SystemSetting> List();
        ServiceResult Update(Actor actor, string key, string value);
    }

    /// <summary>
    /// Append-only audit trail. <see cref="Record"/> stages the row on the caller's context so it
    /// commits in the same transaction as the change it describes.
    /// </summary>
    public interface IAuditService
    {
        void Record(Actor actor, string action, string entityType, object entityId,
                    string previousState = null, string newState = null, string details = null);

        PagedResult<AuditRow> Search(Actor actor, AuditFilter filter);
        IList<string> ActionNames(Actor actor);
    }

    public interface INotificationService
    {
        /// <summary>Stages a notification; the caller's SaveChanges commits it. Duplicate dedup keys are ignored.</summary>
        void Notify(int userId, NotificationType type, string title, string message, string link = null, string dedupKey = null);

        /// <summary>Notifies every active user whose role holds the permission (e.g. all tutor verifiers).</summary>
        void NotifyHolders(Permission permission, NotificationType type, string title, string message, string link = null);

        PagedResult<NotificationRow> ForUser(int userId, int page, int pageSize);
        IList<NotificationRow> Latest(int userId, int count);
        int UnreadCount(int userId);
        void MarkRead(int userId, int notificationId);
        void MarkAllRead(int userId);
    }

    public interface IEmailSender
    {
        /// <summary>Best effort: a mail failure is logged and never undoes the business action.</summary>
        void Send(string to, string subject, string htmlBody);
    }

    /// <summary>Private file storage (supplier invoices) outside the public web root.</summary>
    public interface IFileStore
    {
        string Save(string area, byte[] content, string extension);
        byte[] Read(string area, string storedName);
    }

    public sealed class GatewayResult
    {
        public bool Approved { get; set; }
        public string Reference { get; set; }
        public string Brand { get; set; }
        public string Last4 { get; set; }
        public string DeclineReason { get; set; }
    }

    /// <summary>
    /// Payment provider abstraction (D-6). StudyLink ships a simulated card gateway; a real
    /// provider (PayFast, Peach, Yoco…) is a new implementation of this interface only.
    /// </summary>
    public interface IPaymentGateway
    {
        string Name { get; }
        GatewayResult Charge(decimal amount, CardDetails card, string description);
        GatewayResult Refund(string originalReference, decimal amount);
    }
}
