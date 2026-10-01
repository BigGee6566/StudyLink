using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudyLink.Data.Entities
{
    public class Subject
    {
        public int SubjectId { get; set; }

        [Required, StringLength(100), Index(IsUnique = true)]
        public string Name { get; set; }

        /// <summary>Deactivate rather than delete a subject that is in use (TutorConnect AT-25).</summary>
        public bool IsActive { get; set; } = true;
    }

    public class StudyLevel
    {
        public int StudyLevelId { get; set; }

        [Required, StringLength(50), Index(IsUnique = true)]
        public string Name { get; set; }

        /// <summary>First Year sorts before Second Year; alphabetical order would not.</summary>
        public int SortOrder { get; set; }
    }

    public class StudentSubject
    {
        public int StudentSubjectId { get; set; }

        [Index("UQ_StudentSubject", 1, IsUnique = true)]
        public int StudentId { get; set; }
        public virtual Student Student { get; set; }

        [Index("UQ_StudentSubject", 2, IsUnique = true)]
        public int SubjectId { get; set; }
        public virtual Subject Subject { get; set; }

        [Index("UQ_StudentSubject", 3, IsUnique = true)]
        public int StudyLevelId { get; set; }
        public virtual StudyLevel StudyLevel { get; set; }

        public DateTime CreatedAt { get; set; }
    }

    public class TutorSubject
    {
        public int TutorSubjectId { get; set; }

        [Index("UQ_TutorSubject", 1, IsUnique = true)]
        public int TutorId { get; set; }
        public virtual Tutor Tutor { get; set; }

        [Index("UQ_TutorSubject", 2, IsUnique = true)]
        public int SubjectId { get; set; }
        public virtual Subject Subject { get; set; }

        [Index("UQ_TutorSubject", 3, IsUnique = true)]
        public int StudyLevelId { get; set; }
        public virtual StudyLevel StudyLevel { get; set; }

        public DateTime CreatedAt { get; set; }
    }

    /// <summary>A bookable slot (TutorConnect TutorAvailability). IsBooked is set while an active booking holds it.</summary>
    [Table("TutorAvailability")]
    public class TutorAvailability
    {
        [Key]
        public int AvailabilityId { get; set; }

        public int TutorSubjectId { get; set; }
        public virtual TutorSubject TutorSubject { get; set; }

        public int CampusId { get; set; }
        public virtual Campus Campus { get; set; }

        [Column(TypeName = "date")]
        public DateTime AvailabilityDate { get; set; }

        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }

        public bool IsBooked { get; set; }
        public DateTime CreatedAt { get; set; }

        [Timestamp]
        public byte[] RowVersion { get; set; }
    }

    /// <summary>
    /// Replaces TutorConnect SessionRequest + Booking (D-4). The slot's date and times are copied
    /// in so the booking still shows what was agreed if the tutor later edits their availability.
    /// </summary>
    public class Booking
    {
        public int BookingId { get; set; }

        [Required, StringLength(20), Index(IsUnique = true)]
        public string Reference { get; set; }

        public int StudentId { get; set; }
        public virtual Student Student { get; set; }

        public int TutorId { get; set; }
        public virtual Tutor Tutor { get; set; }

        public int TutorSubjectId { get; set; }
        public virtual TutorSubject TutorSubject { get; set; }

        public int AvailabilityId { get; set; }
        public virtual TutorAvailability Availability { get; set; }

        public int CampusId { get; set; }
        public virtual Campus Campus { get; set; }

        [Column(TypeName = "date")]
        public DateTime SessionDate { get; set; }

        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }

        public decimal Fee { get; set; }
        public BookingStatus Status { get; set; }

        [StringLength(500)]
        public string StudentMessage { get; set; }

        public DateTime? HoldExpiresAt { get; set; }
        public DateTime? PaidAt { get; set; }
        public DateTime? TutorRespondedAt { get; set; }

        [StringLength(200)]
        public string DeclineReason { get; set; }

        // Cancellation: who, when and why (ported from TutorConnect's CK_Booking_CancelConsistency).
        public DateTime? CancelledAt { get; set; }

        public int? CancelledByUserId { get; set; }
        public virtual User CancelledBy { get; set; }

        public ScheduleChangeReason? CancelReasonCategory { get; set; }

        [StringLength(200)]
        public string CancelReason { get; set; }

        public DateTime? CompletedAt { get; set; }
        public DateTime CreatedAt { get; set; }

        [Timestamp]
        public byte[] RowVersion { get; set; }

        [NotMapped]
        public decimal DurationHours => (decimal)(EndTime - StartTime).TotalMinutes / 60m;
    }

    /// <summary>Delivery and attendance record, written when the tutor completes the session.</summary>
    public class TutoringSession
    {
        [Key]
        public int SessionId { get; set; }

        [Index(IsUnique = true)]
        public int BookingId { get; set; }
        public virtual Booking Booking { get; set; }

        [Column(TypeName = "date")]
        public DateTime SessionDate { get; set; }

        public AttendanceStatus Attendance { get; set; }
        public decimal HoursDelivered { get; set; }

        [StringLength(1000)]
        public string TutorNotes { get; set; }

        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Ported 1:1 from TutorConnect. The booking is untouched while a proposal is pending; the
    /// original and proposed times both live here. One pending proposal per booking is enforced
    /// by a filtered unique index created in the migration.
    /// </summary>
    public class PostponementRequest
    {
        [Key]
        public int PostponementId { get; set; }

        public int BookingId { get; set; }
        public virtual Booking Booking { get; set; }

        public int StudentId { get; set; }
        public virtual Student Student { get; set; }

        public int TutorId { get; set; }
        public virtual Tutor Tutor { get; set; }

        [Column(TypeName = "date")]
        public DateTime OriginalDate { get; set; }
        public TimeSpan OriginalStart { get; set; }
        public TimeSpan OriginalEnd { get; set; }

        [Column(TypeName = "date")]
        public DateTime ProposedDate { get; set; }
        public TimeSpan ProposedStart { get; set; }
        public TimeSpan ProposedEnd { get; set; }

        public ScheduleChangeReason ReasonCategory { get; set; }

        [StringLength(200)]
        public string StudentNote { get; set; }

        public PostponementStatus Status { get; set; } = PostponementStatus.PendingTutorReview;
        public DateTime CreatedAt { get; set; }

        [StringLength(200)]
        public string TutorResponseNote { get; set; }

        public DateTime? RespondedAt { get; set; }
    }

    /// <summary>
    /// One review per completed booking (BR-17). No StudentId column: the reviewer is derived
    /// through the booking so the two can never disagree (TutorConnect D-10).
    /// </summary>
    public class Review
    {
        public int ReviewId { get; set; }

        [Index(IsUnique = true)]
        public int BookingId { get; set; }
        public virtual Booking Booking { get; set; }

        public byte Rating { get; set; }

        [StringLength(500)]
        public string Comment { get; set; }

        public DateTime CreatedAt { get; set; }

        public bool IsHidden { get; set; }

        [StringLength(200)]
        public string HiddenReason { get; set; }

        public DateTime? HiddenAt { get; set; }

        public int? HiddenByUserId { get; set; }
        public virtual User HiddenBy { get; set; }
    }

    public class Timesheet
    {
        public int TimesheetId { get; set; }

        [Index("UQ_Timesheet_TutorWeek", 1, IsUnique = true)]
        public int TutorId { get; set; }
        public virtual Tutor Tutor { get; set; }

        /// <summary>Always a Monday.</summary>
        [Column(TypeName = "date"), Index("UQ_Timesheet_TutorWeek", 2, IsUnique = true)]
        public DateTime WeekStart { get; set; }

        public TimesheetStatus Status { get; set; } = TimesheetStatus.Draft;
        public decimal TotalHours { get; set; }

        public DateTime? SubmittedAt { get; set; }

        public int? ReviewedByUserId { get; set; }
        public virtual User ReviewedBy { get; set; }

        public DateTime? ReviewedAt { get; set; }

        [StringLength(300)]
        public string ReviewNote { get; set; }

        public DateTime CreatedAt { get; set; }

        public virtual ICollection<TimesheetEntry> Entries { get; set; } = new List<TimesheetEntry>();
    }

    public class TimesheetEntry
    {
        [Key]
        public int EntryId { get; set; }

        public int TimesheetId { get; set; }
        public virtual Timesheet Timesheet { get; set; }

        public int? BookingId { get; set; }
        public virtual Booking Booking { get; set; }

        [Column(TypeName = "date")]
        public DateTime WorkDate { get; set; }

        public decimal Hours { get; set; }

        [Required, StringLength(200)]
        public string Description { get; set; }
    }
}
