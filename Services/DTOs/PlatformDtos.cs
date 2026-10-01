using System;
using StudyLink.Data.Entities;

namespace StudyLink.Services.DTOs
{
    /// <summary>Card fields exactly as typed on SC9/SC15. Lives only for the duration of one request.</summary>
    public sealed class CardDetails
    {
        public string Number { get; set; }
        public string Expiry { get; set; }
        public string Cvv { get; set; }
        public string Holder { get; set; }
    }

    public sealed class AuditFilter
    {
        public string Action { get; set; }
        public string EntityType { get; set; }
        public string Actor { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 25;
    }

    public sealed class AuditRow
    {
        public long AuditId { get; set; }
        public DateTime CreatedAt { get; set; }
        public string ActorName { get; set; }
        public string ActorRole { get; set; }
        public string Action { get; set; }
        public string EntityType { get; set; }
        public string EntityId { get; set; }
        public string PreviousState { get; set; }
        public string NewState { get; set; }
        public string Details { get; set; }
        public string IpAddress { get; set; }
    }

    public sealed class NotificationRow
    {
        public int NotificationId { get; set; }
        public NotificationType Type { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public string LinkUrl { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
