using System;
using System.Collections.Generic;
using StudyLink.Data.Entities;
using StudyLink.Services.DTOs;
using StudyLink.Services.Infrastructure;

namespace StudyLink.Services.Interfaces
{
    public interface ISubjectService
    {
        IList<SubjectRow> StudentSubjects(Actor actor);
        ServiceResult AddStudentSubject(Actor actor, int subjectId, int studyLevelId);
        ServiceResult RemoveStudentSubject(Actor actor, int studentSubjectId);

        IList<SubjectRow> TutorSubjects(Actor actor);
        ServiceResult AddTutorSubject(Actor actor, int subjectId, int studyLevelId);
        ServiceResult RemoveTutorSubject(Actor actor, int tutorSubjectId);

        IList<CatalogueSubjectRow> Catalogue(Actor actor);
        ServiceResult AddSubject(Actor actor, string name);
        ServiceResult RenameSubject(Actor actor, int subjectId, string name);
        ServiceResult SetSubjectActive(Actor actor, int subjectId, bool active);
    }

    public interface IAvailabilityService
    {
        IList<SlotRow> ForTutor(Actor actor, DateTime from);

        /// <summary>Publishes a slot, optionally repeated weekly. Returns how many were created.</summary>
        ServiceResult<int> Add(Actor actor, int tutorSubjectId, int campusId, DateTime date, TimeSpan start, TimeSpan end, int repeatWeeks);

        ServiceResult Delete(Actor actor, int availabilityId);

        /// <summary>Open, future, bookable slots of a verified tutor (SC8).</summary>
        IList<SlotRow> OpenSlots(int tutorId, DateTime? onDate = null, int? subjectId = null);
        IList<DateTime> OpenDates(int tutorId, int? subjectId = null);
    }

    public interface ITutorSearchService
    {
        PagedResult<TutorCard> Search(TutorSearchFilter filter);
        TutorProfile Profile(int tutorId);
    }

    public interface IBookingService
    {
        /// <summary>
        /// SC8 → SC9: reserves the slot for the student (AwaitingPayment) and creates the tutoring
        /// fee. Race-safe: SERIALIZABLE + UPDLOCK/HOLDLOCK plus the filtered unique index.
        /// </summary>
        ServiceResult<int> Reserve(Actor actor, int availabilityId, string message);

        PagedResult<BookingRow> ForStudent(Actor actor, BookingFilter filter);
        PagedResult<BookingRow> ForTutor(Actor actor, BookingFilter filter);
        PagedResult<BookingRow> Search(Actor actor, BookingFilter filter);
        BookingDetail Get(Actor actor, int bookingId);

        ServiceResult Respond(Actor actor, int bookingId, bool accept, string reason);
        ServiceResult Cancel(Actor actor, int bookingId, ScheduleChangeReason category, string note);
        ServiceResult Complete(Actor actor, int bookingId, AttendanceStatus attendance, string notes);

        /// <summary>Background sweep: expire unpaid holds and unanswered requests. Returns rows changed.</summary>
        int ExpireStale();
    }

    public interface IPostponementService
    {
        ServiceResult Propose(Actor actor, int bookingId, DateTime date, TimeSpan start, TimeSpan end, ScheduleChangeReason reason, string note);
        ServiceResult Approve(Actor actor, int postponementId);
        ServiceResult Decline(Actor actor, int postponementId, string note);
        ServiceResult Withdraw(Actor actor, int postponementId);
        IList<PostponementRow> PendingForTutor(Actor actor);
    }

    public interface IReviewService
    {
        bool CanReview(Actor actor, int bookingId);
        ServiceResult Create(Actor actor, int bookingId, int rating, string comment);
        IList<ReviewRow> ForTutor(int tutorId);
        IList<ReviewRow> ForStudent(Actor actor);
        IList<ReviewRow> All(Actor actor, bool hiddenOnly);
        ServiceResult SetHidden(Actor actor, int reviewId, bool hidden, string reason);
    }

    public interface ITimesheetService
    {
        TimesheetView Week(Actor actor, DateTime anyDayInWeek);
        ServiceResult AddEntry(Actor actor, DateTime workDate, decimal hours, string description);
        ServiceResult RemoveEntry(Actor actor, int entryId);
        ServiceResult Submit(Actor actor, DateTime anyDayInWeek);
        IList<TimesheetRow> History(Actor actor);
        IList<TimesheetRow> ForReview(Actor actor, TimesheetStatus? status);
        TimesheetView Get(Actor actor, int timesheetId);
        ServiceResult Review(Actor actor, int timesheetId, bool approve, string note);
        EarningsView Earnings(Actor actor, DateTime from, DateTime to);
    }
}
