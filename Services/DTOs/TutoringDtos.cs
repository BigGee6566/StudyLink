using System;
using System.Collections.Generic;
using StudyLink.Data.Entities;

namespace StudyLink.Services.DTOs
{
    public sealed class SubjectRow
    {
        public int Id { get; set; }
        public int SubjectId { get; set; }
        public string Subject { get; set; }
        public int StudyLevelId { get; set; }
        public string StudyLevel { get; set; }
        public bool SubjectActive { get; set; }
        public bool InUse { get; set; }
    }

    public sealed class CatalogueSubjectRow
    {
        public int SubjectId { get; set; }
        public string Name { get; set; }
        public bool IsActive { get; set; }
        public int Tutors { get; set; }
        public int Students { get; set; }
    }

    public sealed class TutorSearchFilter
    {
        public int? SubjectId { get; set; }
        public int? StudyLevelId { get; set; }
        public int? CampusId { get; set; }
        public string Text { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 12;
    }

    public sealed class TutorCard
    {
        public int TutorId { get; set; }
        public string FullName { get; set; }
        public string Initials { get; set; }
        public string Qualification { get; set; }
        public int ExperienceYears { get; set; }
        public decimal HourlyRate { get; set; }
        public string Campus { get; set; }
        public string Subjects { get; set; }
        public decimal? AverageRating { get; set; }
        public int ReviewCount { get; set; }
        public DateTime? NextAvailable { get; set; }
    }

    public sealed class SlotRow
    {
        public int AvailabilityId { get; set; }
        public int TutorSubjectId { get; set; }
        public string Subject { get; set; }
        public string StudyLevel { get; set; }
        public string Campus { get; set; }
        public DateTime Date { get; set; }
        public TimeSpan Start { get; set; }
        public TimeSpan End { get; set; }
        public bool IsBooked { get; set; }
        public decimal Fee { get; set; }
        public string BookedBy { get; set; }
        public string BookingReference { get; set; }
        public BookingStatus? BookingStatus { get; set; }

        public decimal Hours => Math.Round((decimal)(End - Start).TotalMinutes / 60m, 2);
        public string TimeRange => $"{Start:hh\\:mm}–{End:hh\\:mm}";
    }

    public sealed class ReviewRow
    {
        public int ReviewId { get; set; }
        public int BookingId { get; set; }
        public string StudentName { get; set; }
        public string TutorName { get; set; }
        public string Subject { get; set; }
        public byte Rating { get; set; }
        public string Comment { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsHidden { get; set; }
        public string HiddenReason { get; set; }
    }

    public sealed class TutorProfile
    {
        public TutorCard Card { get; set; }
        public string Bio { get; set; }
        public TutorVerificationStatus Status { get; set; }
        public IList<SubjectRow> Subjects { get; set; } = new List<SubjectRow>();
        public IList<SlotRow> OpenSlots { get; set; } = new List<SlotRow>();
        public IList<ReviewRow> Reviews { get; set; } = new List<ReviewRow>();
    }

    public sealed class BookingFilter
    {
        public BookingStatus? Status { get; set; }
        public bool UpcomingOnly { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public string Search { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public sealed class BookingRow
    {
        public int BookingId { get; set; }
        public string Reference { get; set; }
        public string StudentName { get; set; }
        public string StudentNumber { get; set; }
        public string TutorName { get; set; }
        public string Subject { get; set; }
        public string StudyLevel { get; set; }
        public string Campus { get; set; }
        public DateTime SessionDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public decimal Fee { get; set; }
        public BookingStatus Status { get; set; }
        public bool FeePaid { get; set; }
        public DateTime CreatedAt { get; set; }

        public string TimeRange => $"{StartTime:hh\\:mm}–{EndTime:hh\\:mm}";
    }

    public sealed class PostponementRow
    {
        public int PostponementId { get; set; }
        public int BookingId { get; set; }
        public string BookingReference { get; set; }
        public string StudentName { get; set; }
        public string Subject { get; set; }
        public DateTime OriginalDate { get; set; }
        public TimeSpan OriginalStart { get; set; }
        public TimeSpan OriginalEnd { get; set; }
        public DateTime ProposedDate { get; set; }
        public TimeSpan ProposedStart { get; set; }
        public TimeSpan ProposedEnd { get; set; }
        public ScheduleChangeReason ReasonCategory { get; set; }
        public string StudentNote { get; set; }
        public PostponementStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public string TutorResponseNote { get; set; }
    }

    /// <summary>One booking as a specific viewer sees it, with what that viewer may do next.</summary>
    public sealed class BookingDetail
    {
        public BookingRow Booking { get; set; }
        public int StudentUserId { get; set; }
        public int TutorUserId { get; set; }
        public string StudentEmail { get; set; }
        public string StudentCell { get; set; }
        public string TutorEmail { get; set; }
        public string StudentMessage { get; set; }
        public string DeclineReason { get; set; }
        public DateTime? HoldExpiresAt { get; set; }
        public DateTime? PaidAt { get; set; }
        public DateTime? CancelledAt { get; set; }
        public string CancelledBy { get; set; }
        public ScheduleChangeReason? CancelReasonCategory { get; set; }
        public string CancelReason { get; set; }
        public DateTime? CompletedAt { get; set; }
        public AttendanceStatus? Attendance { get; set; }
        public string TutorNotes { get; set; }
        public int? FeeId { get; set; }
        public FeeStatus? FeeStatus { get; set; }
        public ReviewRow Review { get; set; }
        public PostponementRow PendingPostponement { get; set; }
        public IList<PostponementRow> PostponementHistory { get; set; } = new List<PostponementRow>();

        public bool CanPay { get; set; }
        public bool CanCancel { get; set; }
        public bool CanRespond { get; set; }
        public bool CanComplete { get; set; }
        public bool CanPostpone { get; set; }
        public bool CanReview { get; set; }
        public string CancelBlockedReason { get; set; }
    }

    public sealed class TimesheetEntryRow
    {
        public int EntryId { get; set; }
        public DateTime WorkDate { get; set; }
        public decimal Hours { get; set; }
        public string Description { get; set; }
        public int? BookingId { get; set; }
        public string BookingReference { get; set; }
    }

    public sealed class TimesheetView
    {
        public int? TimesheetId { get; set; }
        public int TutorId { get; set; }
        public string TutorName { get; set; }
        public DateTime WeekStart { get; set; }
        public DateTime WeekEnd => WeekStart.AddDays(6);
        public TimesheetStatus Status { get; set; }
        public decimal TotalHours { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public string ReviewedBy { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string ReviewNote { get; set; }
        public IList<TimesheetEntryRow> Entries { get; set; } = new List<TimesheetEntryRow>();
        public bool CanEdit => Status == TimesheetStatus.Draft || Status == TimesheetStatus.Rejected;
    }

    public sealed class TimesheetRow
    {
        public int TimesheetId { get; set; }
        public int TutorId { get; set; }
        public string TutorName { get; set; }
        public DateTime WeekStart { get; set; }
        public TimesheetStatus Status { get; set; }
        public decimal TotalHours { get; set; }
        public int Entries { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public string ReviewNote { get; set; }
    }

    public sealed class EarningsLine
    {
        public DateTime SessionDate { get; set; }
        public string Reference { get; set; }
        public string StudentName { get; set; }
        public string Subject { get; set; }
        public decimal Hours { get; set; }
        public decimal Fee { get; set; }
        public decimal Commission { get; set; }
        public decimal Net { get; set; }
    }

    public sealed class EarningsView
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public decimal CommissionPercent { get; set; }
        public int Sessions { get; set; }
        public decimal Hours { get; set; }
        public decimal Gross { get; set; }
        public decimal Commission { get; set; }
        public decimal Net { get; set; }
        public decimal ApprovedTimesheetHours { get; set; }
        public IList<EarningsLine> Lines { get; set; } = new List<EarningsLine>();
    }
}
