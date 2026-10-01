using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudyLink.Data.Entities
{
    public class Notification
    {
        public int NotificationId { get; set; }

        [Index("IX_Notification_UserUnread", 1)]
        public int UserId { get; set; }
        public virtual User User { get; set; }

        [Required, StringLength(150)]
        public string Title { get; set; }

        [Required, StringLength(500)]
        public string Message { get; set; }

        public NotificationType Type { get; set; }

        [StringLength(300)]
        public string LinkUrl { get; set; }

        [Index("IX_Notification_UserUnread", 2)]
        public bool IsRead { get; set; }

        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Set by the background sweep ("RentalOverdue:123:2026-09-30") so a reminder is sent once
        /// even if the sweep runs many times. Unique where not null (filtered index in migration).
        /// </summary>
        [StringLength(100)]
        public string DedupKey { get; set; }
    }

    /// <summary>Append-only. The application has no UPDATE or DELETE path against this table.</summary>
    public class AuditLog
    {
        [Key]
        public long AuditId { get; set; }

        public int? ActorUserId { get; set; }
        public virtual User Actor { get; set; }

        [Required, StringLength(30)]
        public string ActorRole { get; set; }

        [Required, StringLength(50), Index("IX_AuditLog_Action")]
        public string Action { get; set; }

        [Required, StringLength(40), Index("IX_AuditLog_Entity", 1)]
        public string EntityType { get; set; }

        [Required, StringLength(40), Index("IX_AuditLog_Entity", 2)]
        public string EntityId { get; set; }

        [StringLength(1000)]
        public string PreviousState { get; set; }

        [StringLength(1000)]
        public string NewState { get; set; }

        [StringLength(500)]
        public string Details { get; set; }

        [StringLength(45)]
        public string IpAddress { get; set; }

        [Index("IX_AuditLog_Created")]
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>The brief's "Reports" table: a log of every report generated or exported.</summary>
    public class ReportRun
    {
        public int ReportRunId { get; set; }

        [Required, StringLength(50)]
        public string ReportKey { get; set; }

        [StringLength(300)]
        public string Parameters { get; set; }

        [StringLength(10)]
        public string Format { get; set; }

        public int RowCount { get; set; }

        public int GeneratedByUserId { get; set; }
        public virtual User GeneratedBy { get; set; }

        public DateTime GeneratedAt { get; set; }
    }

    /// <summary>
    /// One row per business day, written by the background sweep from real transactions. Trend
    /// charts read this; today's figures are always computed live.
    /// </summary>
    public class DailyMetric
    {
        [Key, Column(TypeName = "date"), DatabaseGenerated(DatabaseGeneratedOption.None)]
        public DateTime MetricDate { get; set; }

        public decimal Revenue { get; set; }
        public decimal Refunds { get; set; }
        public int PaymentsCount { get; set; }
        public int BookingsCreated { get; set; }
        public int SessionsCompleted { get; set; }
        public int RentalsStarted { get; set; }
        public int RentalsReturned { get; set; }
        public int OverdueRentals { get; set; }
        public int BooksSold { get; set; }
        public decimal FinesIssued { get; set; }
        public int NewStudents { get; set; }
        public int NewTutors { get; set; }
        public DateTime CapturedAt { get; set; }
    }

    /// <summary>Per-tutor aggregates refreshed by the sweep for the utilisation report.</summary>
    public class TutorStat
    {
        [Key, ForeignKey("Tutor"), DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int TutorId { get; set; }
        public virtual Tutor Tutor { get; set; }

        public int SessionsCompleted { get; set; }
        public int SessionsCancelled { get; set; }
        public decimal HoursDelivered { get; set; }
        public decimal HoursPublished { get; set; }
        public decimal UtilisationPercent { get; set; }
        public decimal? AverageRating { get; set; }
        public int ReviewCount { get; set; }
        public decimal GrossEarnings { get; set; }
        public DateTime RefreshedAt { get; set; }
    }

    public class BackupLog
    {
        [Key]
        public int BackupId { get; set; }

        [Required, StringLength(400)]
        public string FilePath { get; set; }

        public BackupStatus Status { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public long? SizeBytes { get; set; }

        public int RequestedByUserId { get; set; }
        public virtual User RequestedBy { get; set; }

        [StringLength(500)]
        public string Message { get; set; }
    }
}
