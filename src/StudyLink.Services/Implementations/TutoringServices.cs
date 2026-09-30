using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Globalization;
using System.Linq;
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
    /// <summary>Resolves the signed-in user's profile rows. BR-24: ids always come from the actor, never the form.</summary>
    internal static class Profiles
    {
        public static int? StudentId(StudyLinkDbContext db, Actor actor) =>
            db.Students.Where(s => s.UserId == actor.UserId).Select(s => (int?)s.StudentId).FirstOrDefault();

        public static int? TutorId(StudyLinkDbContext db, Actor actor) =>
            db.Tutors.Where(t => t.UserId == actor.UserId).Select(t => (int?)t.TutorId).FirstOrDefault();

        public static int RequireStudent(StudyLinkDbContext db, Actor actor) =>
            StudentId(db, actor) ?? throw new AccessDeniedException("Only students can do this.");

        public static int RequireTutor(StudyLinkDbContext db, Actor actor) =>
            TutorId(db, actor) ?? throw new AccessDeniedException("Only tutors can do this.");

        public static string Time(TimeSpan t) => t.ToString(@"hh\:mm", CultureInfo.InvariantCulture);
        public static string When(DateTime date, TimeSpan start, TimeSpan end) =>
            date.ToString("ddd d MMM yyyy", CultureInfo.GetCultureInfo("en-ZA")) + ", " + Time(start) + "–" + Time(end);
    }

    public sealed class SubjectService : ISubjectService
    {
        private readonly StudyLinkDbContext _db;
        private readonly IAuditService _audit;
        private readonly IClock _clock;

        public SubjectService(StudyLinkDbContext db, IAuditService audit, IClock clock)
        {
            _db = db;
            _audit = audit;
            _clock = clock;
        }

        public IList<SubjectRow> StudentSubjects(Actor actor)
        {
            var studentId = Profiles.RequireStudent(_db, actor);
            return _db.StudentSubjects.AsNoTracking().Where(s => s.StudentId == studentId)
                .OrderBy(s => s.Subject.Name).ThenBy(s => s.StudyLevel.SortOrder)
                .Select(s => new SubjectRow
                {
                    Id = s.StudentSubjectId, SubjectId = s.SubjectId, Subject = s.Subject.Name,
                    StudyLevelId = s.StudyLevelId, StudyLevel = s.StudyLevel.Name, SubjectActive = s.Subject.IsActive
                }).ToList();
        }

        public ServiceResult AddStudentSubject(Actor actor, int subjectId, int studyLevelId)
        {
            var studentId = Profiles.RequireStudent(_db, actor);
            var error = CheckSubject(subjectId, studyLevelId);
            if (error != null) return ServiceResult.Fail(error);
            // BR-05: no duplicate subject + level.
            if (_db.StudentSubjects.Any(s => s.StudentId == studentId && s.SubjectId == subjectId && s.StudyLevelId == studyLevelId))
                return ServiceResult.Fail("You have already added this subject at this level.");
            _db.StudentSubjects.Add(new StudentSubject { StudentId = studentId, SubjectId = subjectId, StudyLevelId = studyLevelId, CreatedAt = _clock.Now });
            return Save("You have already added this subject at this level.");
        }

        public ServiceResult RemoveStudentSubject(Actor actor, int studentSubjectId)
        {
            var studentId = Profiles.RequireStudent(_db, actor);
            var row = _db.StudentSubjects.FirstOrDefault(s => s.StudentSubjectId == studentSubjectId && s.StudentId == studentId);
            if (row == null) return ServiceResult.Fail("Subject not found.");
            _db.StudentSubjects.Remove(row);
            _db.SaveChanges();
            return ServiceResult.Ok();
        }

        public IList<SubjectRow> TutorSubjects(Actor actor)
        {
            var tutorId = Profiles.RequireTutor(_db, actor);
            var active = new[] { BookingStatus.AwaitingPayment, BookingStatus.AwaitingTutor, BookingStatus.Confirmed };
            return _db.TutorSubjects.AsNoTracking().Where(s => s.TutorId == tutorId)
                .OrderBy(s => s.Subject.Name).ThenBy(s => s.StudyLevel.SortOrder)
                .Select(s => new SubjectRow
                {
                    Id = s.TutorSubjectId, SubjectId = s.SubjectId, Subject = s.Subject.Name,
                    StudyLevelId = s.StudyLevelId, StudyLevel = s.StudyLevel.Name, SubjectActive = s.Subject.IsActive,
                    InUse = _db.Bookings.Any(b => b.TutorSubjectId == s.TutorSubjectId && active.Contains(b.Status))
                }).ToList();
        }

        public ServiceResult AddTutorSubject(Actor actor, int subjectId, int studyLevelId)
        {
            var tutorId = Profiles.RequireTutor(_db, actor);
            var error = CheckSubject(subjectId, studyLevelId);
            if (error != null) return ServiceResult.Fail(error);
            if (_db.TutorSubjects.Any(s => s.TutorId == tutorId && s.SubjectId == subjectId && s.StudyLevelId == studyLevelId))
                return ServiceResult.Fail("You already tutor this subject at this level.");
            _db.TutorSubjects.Add(new TutorSubject { TutorId = tutorId, SubjectId = subjectId, StudyLevelId = studyLevelId, CreatedAt = _clock.Now });
            return Save("You already tutor this subject at this level.");
        }

        public ServiceResult RemoveTutorSubject(Actor actor, int tutorSubjectId)
        {
            var tutorId = Profiles.RequireTutor(_db, actor);
            var row = _db.TutorSubjects.FirstOrDefault(s => s.TutorSubjectId == tutorSubjectId && s.TutorId == tutorId);
            if (row == null) return ServiceResult.Fail("Subject not found.");
            // BR-21: history (bookings) and published slots keep the subject in use.
            if (_db.Bookings.Any(b => b.TutorSubjectId == tutorSubjectId))
                return ServiceResult.Fail("This subject has bookings, so it cannot be removed. Delete its open slots instead.");
            var openSlots = _db.TutorAvailability.Where(a => a.TutorSubjectId == tutorSubjectId).ToList();
            if (openSlots.Any(a => a.IsBooked))
                return ServiceResult.Fail("A slot for this subject is booked, so it cannot be removed.");
            _db.TutorAvailability.RemoveRange(openSlots);
            _db.TutorSubjects.Remove(row);
            _db.SaveChanges();
            return ServiceResult.Ok();
        }

        public IList<CatalogueSubjectRow> Catalogue(Actor actor)
        {
            RolePermissions.Demand(actor, Permission.ManageSubjects);
            return _db.Subjects.AsNoTracking().OrderBy(s => s.Name).Select(s => new CatalogueSubjectRow
            {
                SubjectId = s.SubjectId, Name = s.Name, IsActive = s.IsActive,
                Tutors = _db.TutorSubjects.Count(t => t.SubjectId == s.SubjectId),
                Students = _db.StudentSubjects.Count(t => t.SubjectId == s.SubjectId)
            }).ToList();
        }

        public ServiceResult AddSubject(Actor actor, string name)
        {
            RolePermissions.Demand(actor, Permission.ManageSubjects);
            name = (name ?? string.Empty).Trim();
            if (name.Length < 2 || name.Length > 100) return ServiceResult.Fail("Subject names are 2–100 characters.");
            if (_db.Subjects.Any(s => s.Name == name)) return ServiceResult.Fail("That subject already exists.");
            var subject = _db.Subjects.Add(new Subject { Name = name, IsActive = true });
            var result = Save("That subject already exists.");
            if (result.Succeeded)
            {
                _audit.Record(actor, "SubjectCreated", "Subject", subject.SubjectId, newState: name);
                _db.SaveChanges();
            }
            return result;
        }

        public ServiceResult RenameSubject(Actor actor, int subjectId, string name)
        {
            RolePermissions.Demand(actor, Permission.ManageSubjects);
            name = (name ?? string.Empty).Trim();
            var subject = _db.Subjects.Find(subjectId);
            if (subject == null) return ServiceResult.Fail("Subject not found.");
            if (name.Length < 2 || name.Length > 100) return ServiceResult.Fail("Subject names are 2–100 characters.");
            if (_db.Subjects.Any(s => s.Name == name && s.SubjectId != subjectId)) return ServiceResult.Fail("That subject already exists.");
            _audit.Record(actor, "SubjectRenamed", "Subject", subjectId, subject.Name, name);
            subject.Name = name;
            return Save("That subject already exists.");
        }

        public ServiceResult SetSubjectActive(Actor actor, int subjectId, bool active)
        {
            RolePermissions.Demand(actor, Permission.ManageSubjects);
            var subject = _db.Subjects.Find(subjectId);
            if (subject == null) return ServiceResult.Fail("Subject not found.");
            if (subject.IsActive == active) return ServiceResult.Ok();
            // Part 12.6 (TutorConnect): deactivating stops new use; existing bookings are unaffected.
            subject.IsActive = active;
            _audit.Record(actor, active ? "SubjectActivated" : "SubjectDeactivated", "Subject", subjectId);
            _db.SaveChanges();
            return ServiceResult.Ok();
        }

        private string CheckSubject(int subjectId, int studyLevelId)
        {
            if (!_db.Subjects.Any(s => s.SubjectId == subjectId && s.IsActive)) return "That subject is not currently offered.";
            if (!_db.StudyLevels.Any(l => l.StudyLevelId == studyLevelId)) return "Select a level.";
            return null;
        }

        private ServiceResult Save(string duplicateMessage)
        {
            try
            {
                _db.SaveChanges();
                return ServiceResult.Ok();
            }
            catch (DbUpdateException ex) when (DbErrors.IsUniqueViolation(ex))
            {
                return ServiceResult.Fail(duplicateMessage);
            }
        }
    }

    public sealed class AvailabilityService : IAvailabilityService
    {
        private readonly StudyLinkDbContext _db;
        private readonly ISettingsService _settings;
        private readonly IClock _clock;

        public AvailabilityService(StudyLinkDbContext db, ISettingsService settings, IClock clock)
        {
            _db = db;
            _settings = settings;
            _clock = clock;
        }

        public IList<SlotRow> ForTutor(Actor actor, DateTime from)
        {
            var tutorId = Profiles.RequireTutor(_db, actor);
            var active = new[] { BookingStatus.AwaitingPayment, BookingStatus.AwaitingTutor, BookingStatus.Confirmed, BookingStatus.Completed };
            var fromDate = from.Date;
            return _db.TutorAvailability.AsNoTracking()
                .Where(a => a.TutorSubject.TutorId == tutorId && a.AvailabilityDate >= fromDate)
                .OrderBy(a => a.AvailabilityDate).ThenBy(a => a.StartTime)
                .Select(a => new
                {
                    a.AvailabilityId, a.TutorSubjectId, Subject = a.TutorSubject.Subject.Name, Level = a.TutorSubject.StudyLevel.Name,
                    Campus = a.Campus.Name, a.AvailabilityDate, a.StartTime, a.EndTime, a.IsBooked, a.TutorSubject.Tutor.HourlyRate,
                    Booking = _db.Bookings.Where(b => b.AvailabilityId == a.AvailabilityId && active.Contains(b.Status))
                        .Select(b => new { b.Reference, b.Status, Name = b.Student.User.FirstName + " " + b.Student.User.LastName })
                        .FirstOrDefault()
                }).ToList()
                .Select(a => new SlotRow
                {
                    AvailabilityId = a.AvailabilityId, TutorSubjectId = a.TutorSubjectId, Subject = a.Subject, StudyLevel = a.Level,
                    Campus = a.Campus, Date = a.AvailabilityDate, Start = a.StartTime, End = a.EndTime, IsBooked = a.IsBooked,
                    Fee = SchedulePolicy.SessionFee(a.HourlyRate, a.StartTime, a.EndTime),
                    BookedBy = a.Booking?.Name, BookingReference = a.Booking?.Reference, BookingStatus = a.Booking?.Status
                }).ToList();
        }

        public ServiceResult<int> Add(Actor actor, int tutorSubjectId, int campusId, DateTime date, TimeSpan start, TimeSpan end, int repeatWeeks)
        {
            RolePermissions.Demand(actor, Permission.ManageAvailability);
            var tutorId = Profiles.RequireTutor(_db, actor);
            var tutor = _db.Tutors.Find(tutorId);
            if (tutor.VerificationStatus != TutorVerificationStatus.Verified)
                return ServiceResult<int>.Fail("Your tutor profile must be verified by the hub before you can publish availability.");

            var ts = _db.TutorSubjects.Include(s => s.Subject).FirstOrDefault(s => s.TutorSubjectId == tutorSubjectId && s.TutorId == tutorId);
            if (ts == null) return ServiceResult<int>.Fail("Choose one of your subjects.");
            if (!ts.Subject.IsActive) return ServiceResult<int>.Fail("That subject is no longer offered.");
            if (!_db.Campuses.Any(c => c.CampusId == campusId && c.IsActive)) return ServiceResult<int>.Fail("Choose a campus.");
            if (repeatWeeks < 0 || repeatWeeks > 12) return ServiceResult<int>.Fail("Repeat for 0–12 extra weeks.");

            var now = _clock.Now;
            var maxHours = _settings.GetInt(SettingKeys.MaxSessionHours, 8);
            var maxDays = _settings.GetInt(SettingKeys.AvailabilityMaxDaysAhead, 365);
            var created = 0;
            var skipped = new List<string>();

            for (var week = 0; week <= repeatWeeks; week++)
            {
                var day = date.Date.AddDays(7 * week);
                var error = SchedulePolicy.CheckSlot(day, start, end, now, maxHours, maxDays);
                if (error != null)
                {
                    if (week == 0) return ServiceResult<int>.Fail(error);
                    skipped.Add(day.ToString("d MMM") + ": " + error);
                    continue;
                }
                // BR-20: a tutor cannot publish two overlapping slots (across all their subjects).
                var clash = _db.TutorAvailability.Any(a => a.TutorSubject.TutorId == tutorId && a.AvailabilityDate == day
                                                           && a.StartTime < end && start < a.EndTime);
                if (clash)
                {
                    if (week == 0 && repeatWeeks == 0) return ServiceResult<int>.Fail("This overlaps one of your existing slots.");
                    skipped.Add(day.ToString("d MMM") + ": overlaps an existing slot");
                    continue;
                }
                _db.TutorAvailability.Add(new TutorAvailability
                {
                    TutorSubjectId = tutorSubjectId, CampusId = campusId, AvailabilityDate = day,
                    StartTime = start, EndTime = end, CreatedAt = now
                });
                created++;
            }

            if (created == 0)
                return ServiceResult<int>.Fail("No slots were created: " + string.Join("; ", skipped));
            _db.SaveChanges();
            return ServiceResult<int>.Ok(created);
        }

        public ServiceResult Delete(Actor actor, int availabilityId)
        {
            RolePermissions.Demand(actor, Permission.ManageAvailability);
            var tutorId = Profiles.RequireTutor(_db, actor);
            var slot = _db.TutorAvailability.FirstOrDefault(a => a.AvailabilityId == availabilityId && a.TutorSubject.TutorId == tutorId);
            if (slot == null) return ServiceResult.Fail("Slot not found.");
            // BR-22: a slot that has (or had) a booking is part of that booking's history.
            if (slot.IsBooked || _db.Bookings.Any(b => b.AvailabilityId == availabilityId))
                return ServiceResult.Fail("This slot has a booking and cannot be deleted. Cancel the booking instead.");
            _db.TutorAvailability.Remove(slot);
            _db.SaveChanges();
            return ServiceResult.Ok();
        }

        public IList<SlotRow> OpenSlots(int tutorId, DateTime? onDate = null, int? subjectId = null)
        {
            var now = _clock.Now;
            var today = now.Date;
            var q = _db.TutorAvailability.AsNoTracking().Where(a =>
                a.TutorSubject.TutorId == tutorId && !a.IsBooked && a.AvailabilityDate >= today
                && a.TutorSubject.Subject.IsActive
                && a.TutorSubject.Tutor.VerificationStatus == TutorVerificationStatus.Verified
                && a.TutorSubject.Tutor.User.IsActive);
            if (onDate.HasValue)
            {
                var d = onDate.Value.Date;
                q = q.Where(a => a.AvailabilityDate == d);
            }
            if (subjectId.HasValue)
                q = q.Where(a => a.TutorSubject.SubjectId == subjectId.Value);

            return q.OrderBy(a => a.AvailabilityDate).ThenBy(a => a.StartTime).Take(200)
                .Select(a => new
                {
                    a.AvailabilityId, a.TutorSubjectId, Subject = a.TutorSubject.Subject.Name, Level = a.TutorSubject.StudyLevel.Name,
                    Campus = a.Campus.Name, a.AvailabilityDate, a.StartTime, a.EndTime, a.TutorSubject.Tutor.HourlyRate
                }).ToList()
                .Where(a => a.AvailabilityDate + a.StartTime > now)
                .Select(a => new SlotRow
                {
                    AvailabilityId = a.AvailabilityId, TutorSubjectId = a.TutorSubjectId, Subject = a.Subject, StudyLevel = a.Level,
                    Campus = a.Campus, Date = a.AvailabilityDate, Start = a.StartTime, End = a.EndTime,
                    Fee = SchedulePolicy.SessionFee(a.HourlyRate, a.StartTime, a.EndTime)
                }).ToList();
        }

        public IList<DateTime> OpenDates(int tutorId, int? subjectId = null) =>
            OpenSlots(tutorId, null, subjectId).Select(s => s.Date).Distinct().OrderBy(d => d).ToList();
    }

    public sealed class TutorSearchService : ITutorSearchService
    {
        private readonly StudyLinkDbContext _db;
        private readonly IAvailabilityService _availability;
        private readonly IReviewService _reviews;
        private readonly IClock _clock;

        public TutorSearchService(StudyLinkDbContext db, IAvailabilityService availability, IReviewService reviews, IClock clock)
        {
            _db = db;
            _availability = availability;
            _reviews = reviews;
            _clock = clock;
        }

        public PagedResult<TutorCard> Search(TutorSearchFilter f)
        {
            var today = _clock.Today;
            // BR-26: only verified tutors with active accounts are ever listed.
            var q = _db.Tutors.AsNoTracking().Where(t => t.VerificationStatus == TutorVerificationStatus.Verified && t.User.IsActive);
            if (f.SubjectId.HasValue)
                q = q.Where(t => t.Subjects.Any(s => s.SubjectId == f.SubjectId.Value && s.Subject.IsActive
                                                     && (!f.StudyLevelId.HasValue || s.StudyLevelId == f.StudyLevelId.Value)));
            else if (f.StudyLevelId.HasValue)
                q = q.Where(t => t.Subjects.Any(s => s.StudyLevelId == f.StudyLevelId.Value));
            if (f.CampusId.HasValue)
                q = q.Where(t => t.User.CampusId == f.CampusId.Value
                                 || _db.TutorAvailability.Any(a => a.TutorSubject.TutorId == t.TutorId && a.CampusId == f.CampusId.Value && a.AvailabilityDate >= today));
            if (!string.IsNullOrWhiteSpace(f.Text))
            {
                var text = f.Text.Trim();
                q = q.Where(t => t.User.FirstName.Contains(text) || t.User.LastName.Contains(text) || t.Qualification.Contains(text)
                                 || t.Subjects.Any(s => s.Subject.Name.Contains(text)));
            }

            var total = q.Count();
            var page = Math.Max(1, f.Page);
            var raw = q.OrderBy(t => t.User.FirstName).ThenBy(t => t.User.LastName)
                .Skip((page - 1) * f.PageSize).Take(f.PageSize)
                .Select(t => new
                {
                    t.TutorId, t.User.FirstName, t.User.LastName, t.Qualification, t.ExperienceYears, t.HourlyRate,
                    Campus = t.User.Campus.Name,
                    Subjects = t.Subjects.Where(s => s.Subject.IsActive).Select(s => s.Subject.Name).Distinct(),
                    // Hidden reviews never move a public rating (TutorConnect moderation rule).
                    Ratings = _db.Reviews.Where(r => r.Booking.TutorId == t.TutorId && !r.IsHidden).Select(r => (int)r.Rating),
                    Next = _db.TutorAvailability.Where(a => a.TutorSubject.TutorId == t.TutorId && !a.IsBooked && a.AvailabilityDate >= today)
                        .Min(a => (DateTime?)a.AvailabilityDate)
                }).ToList();

            var cards = raw.Select(t => new TutorCard
            {
                TutorId = t.TutorId, FullName = t.FirstName + " " + t.LastName,
                Initials = (t.FirstName.Substring(0, 1) + t.LastName.Substring(0, 1)).ToUpperInvariant(),
                Qualification = t.Qualification, ExperienceYears = t.ExperienceYears, HourlyRate = t.HourlyRate, Campus = t.Campus,
                Subjects = string.Join(", ", t.Subjects.OrderBy(s => s)),
                ReviewCount = t.Ratings.Count(),
                AverageRating = t.Ratings.Any() ? Math.Round((decimal)t.Ratings.Average(), 1) : (decimal?)null,
                NextAvailable = t.Next
            }).ToList();
            return new PagedResult<TutorCard>(cards, total, page, f.PageSize);
        }

        public TutorProfile Profile(int tutorId)
        {
            var t = _db.Tutors.AsNoTracking().Include(x => x.User.Campus)
                .FirstOrDefault(x => x.TutorId == tutorId && x.VerificationStatus == TutorVerificationStatus.Verified && x.User.IsActive);
            if (t == null) return null;

            var reviews = _reviews.ForTutor(tutorId);
            return new TutorProfile
            {
                Card = new TutorCard
                {
                    TutorId = t.TutorId, FullName = t.User.FullName,
                    Initials = (t.User.FirstName.Substring(0, 1) + t.User.LastName.Substring(0, 1)).ToUpperInvariant(),
                    Qualification = t.Qualification, ExperienceYears = t.ExperienceYears, HourlyRate = t.HourlyRate,
                    Campus = t.User.Campus.Name, ReviewCount = reviews.Count,
                    AverageRating = reviews.Any() ? Math.Round((decimal)reviews.Average(r => (int)r.Rating), 1) : (decimal?)null
                },
                Bio = t.Bio,
                Status = t.VerificationStatus,
                Subjects = _db.TutorSubjects.AsNoTracking().Where(s => s.TutorId == tutorId && s.Subject.IsActive)
                    .OrderBy(s => s.Subject.Name).Select(s => new SubjectRow
                    {
                        Id = s.TutorSubjectId, SubjectId = s.SubjectId, Subject = s.Subject.Name,
                        StudyLevelId = s.StudyLevelId, StudyLevel = s.StudyLevel.Name, SubjectActive = true
                    }).ToList(),
                OpenSlots = _availability.OpenSlots(tutorId),
                Reviews = reviews
            };
        }
    }

    public sealed class BookingService : IBookingService
    {
        private const string SlotTaken = "Someone has just booked this time slot. Please choose another time.";
        private static readonly BookingStatus[] Active = { BookingStatus.AwaitingPayment, BookingStatus.AwaitingTutor, BookingStatus.Confirmed };

        private readonly StudyLinkDbContext _db;
        private readonly ISettingsService _settings;
        private readonly IAuditService _audit;
        private readonly INotificationService _notifications;
        private readonly IPaymentService _payments;
        private readonly IClock _clock;

        public BookingService(StudyLinkDbContext db, ISettingsService settings, IAuditService audit,
                              INotificationService notifications, IPaymentService payments, IClock clock)
        {
            _db = db;
            _settings = settings;
            _audit = audit;
            _notifications = notifications;
            _payments = payments;
            _clock = clock;
        }

        public ServiceResult<int> Reserve(Actor actor, int availabilityId, string message)
        {
            RolePermissions.Demand(actor, Permission.BookTutoring);
            var studentId = Profiles.RequireStudent(_db, actor);
            if (TextRules.TooLong(message, 500))
                return ServiceResult<int>.Fail("Your message to the tutor is limited to 500 characters.");

            var now = _clock.Now;
            try
            {
                using (var tx = _db.Database.BeginTransaction(IsolationLevel.Serializable))
                {
                    // Lock the slot row first so two students reserving the same slot queue up here
                    // (ported from TutorConnect AcceptRequest: UPDLOCK, HOLDLOCK inside SERIALIZABLE).
                    var exists = _db.Database.SqlQuery<int>(
                        "SELECT AvailabilityId FROM dbo.TutorAvailability WITH (UPDLOCK, HOLDLOCK) WHERE AvailabilityId = @p0",
                        availabilityId).Any();
                    if (!exists)
                        return ServiceResult<int>.Fail("That time slot no longer exists.");

                    var slot = _db.TutorAvailability
                        .Include(a => a.TutorSubject.Tutor.User).Include(a => a.TutorSubject.Subject)
                        .Single(a => a.AvailabilityId == availabilityId);
                    var tutor = slot.TutorSubject.Tutor;

                    if (tutor.VerificationStatus != TutorVerificationStatus.Verified || !tutor.User.IsActive)
                        return ServiceResult<int>.Fail("This tutor is not currently accepting bookings.");
                    if (!slot.TutorSubject.Subject.IsActive)
                        return ServiceResult<int>.Fail("This subject is no longer offered.");
                    if (slot.AvailabilityDate.Date + slot.StartTime <= now)
                        return ServiceResult<int>.Fail("This time slot has already started or passed.");
                    // BR-10: the slot must be open.
                    if (slot.IsBooked || _db.Bookings.Any(b => b.AvailabilityId == availabilityId && Active.Contains(b.Status)))
                        return ServiceResult<int>.Fail(SlotTaken);

                    var date = slot.AvailabilityDate;
                    // BR-12: the student cannot be in two sessions at once.
                    if (_db.Bookings.Any(b => b.StudentId == studentId && Active.Contains(b.Status) && b.SessionDate == date
                                              && b.StartTime < slot.EndTime && slot.StartTime < b.EndTime))
                        return ServiceResult<int>.Fail("You already have a session booked that overlaps this time.");
                    // BR-14: nor the tutor (defensive - BR-20 already stops overlapping slots).
                    if (_db.Bookings.Any(b => b.TutorId == tutor.TutorId && Active.Contains(b.Status) && b.SessionDate == date
                                              && b.StartTime < slot.EndTime && slot.StartTime < b.EndTime))
                        return ServiceResult<int>.Fail(SlotTaken);

                    var fee = SchedulePolicy.SessionFee(tutor.HourlyRate, slot.StartTime, slot.EndTime);
                    var booking = new Booking
                    {
                        Reference = ReferenceGenerator.New("BK", now),
                        StudentId = studentId, TutorId = tutor.TutorId, TutorSubjectId = slot.TutorSubjectId,
                        AvailabilityId = slot.AvailabilityId, CampusId = slot.CampusId,
                        SessionDate = date, StartTime = slot.StartTime, EndTime = slot.EndTime,
                        Fee = fee, StudentMessage = TextRules.Clean(message, 500), CreatedAt = now,
                        Status = fee > 0 ? BookingStatus.AwaitingPayment : BookingStatus.AwaitingTutor,
                        HoldExpiresAt = fee > 0 ? now.AddMinutes(_settings.GetInt(SettingKeys.BookingHoldMinutes, 30)) : (DateTime?)null
                    };
                    _db.Bookings.Add(booking);
                    slot.IsBooked = true;
                    if (fee > 0)
                        FeeLedger.Add(_db, studentId, FeeType.TutoringFee, fee,
                            "Tutoring – " + slot.TutorSubject.Subject.Name + " with " + tutor.User.FullName + ", " + Profiles.When(date, slot.StartTime, slot.EndTime),
                            now, booking: booking);
                    else
                        NotifyTutorOfBooking(booking, tutor.UserId, slot.TutorSubject.Subject.Name);

                    _db.SaveChanges();
                    tx.Commit();
                    return ServiceResult<int>.Ok(booking.BookingId);
                }
            }
            catch (Exception ex) when (DbErrors.IsConcurrencyConflict(ex))
            {
                // Lost the race: the filtered unique index or the deadlock monitor stopped a double booking.
                return ServiceResult<int>.Fail(SlotTaken);
            }
        }

        internal void NotifyTutorOfBooking(Booking booking, int tutorUserId, string subject)
        {
            _notifications.Notify(tutorUserId, NotificationType.BookingCreated, "New session to confirm",
                subject + " on " + Profiles.When(booking.SessionDate, booking.StartTime, booking.EndTime) + ". Please confirm or decline.",
                "~/Tutor/SessionDetails.aspx?id=" + booking.BookingId);
        }

        public PagedResult<BookingRow> ForStudent(Actor actor, BookingFilter f)
        {
            var studentId = Profiles.RequireStudent(_db, actor);
            return Page(_db.Bookings.Where(b => b.StudentId == studentId), f);
        }

        public PagedResult<BookingRow> ForTutor(Actor actor, BookingFilter f)
        {
            var tutorId = Profiles.RequireTutor(_db, actor);
            return Page(_db.Bookings.Where(b => b.TutorId == tutorId), f);
        }

        public PagedResult<BookingRow> Search(Actor actor, BookingFilter f)
        {
            if (!RolePermissions.Has(actor, Permission.ViewOperationalReports))
                throw new AccessDeniedException("You cannot view all bookings.");
            return Page(_db.Bookings, f);
        }

        private PagedResult<BookingRow> Page(IQueryable<Booking> q, BookingFilter f)
        {
            q = q.AsNoTracking();
            if (f.Status.HasValue) q = q.Where(b => b.Status == f.Status.Value);
            if (f.UpcomingOnly)
            {
                var today = _clock.Today;
                q = q.Where(b => b.SessionDate >= today && (b.Status == BookingStatus.AwaitingPayment
                                                             || b.Status == BookingStatus.AwaitingTutor || b.Status == BookingStatus.Confirmed));
            }
            if (f.From.HasValue) { var from = f.From.Value.Date; q = q.Where(b => b.SessionDate >= from); }
            if (f.To.HasValue) { var to = f.To.Value.Date; q = q.Where(b => b.SessionDate <= to); }
            if (!string.IsNullOrWhiteSpace(f.Search))
            {
                var s = f.Search.Trim();
                q = q.Where(b => b.Reference.Contains(s) || b.Student.User.LastName.Contains(s) || b.Student.StudentNumber.Contains(s)
                                 || b.Tutor.User.LastName.Contains(s) || b.TutorSubject.Subject.Name.Contains(s));
            }

            var total = q.Count();
            var page = Math.Max(1, f.Page);
            var ordered = f.UpcomingOnly
                ? q.OrderBy(b => b.SessionDate).ThenBy(b => b.StartTime)
                : q.OrderByDescending(b => b.SessionDate).ThenByDescending(b => b.StartTime);
            var rows = Project(ordered.Skip((page - 1) * f.PageSize).Take(f.PageSize)).ToList();
            return new PagedResult<BookingRow>(rows, total, page, f.PageSize);
        }

        private IQueryable<BookingRow> Project(IQueryable<Booking> q) =>
            q.Select(b => new BookingRow
            {
                BookingId = b.BookingId, Reference = b.Reference,
                StudentName = b.Student.User.FirstName + " " + b.Student.User.LastName, StudentNumber = b.Student.StudentNumber,
                TutorName = b.Tutor.User.FirstName + " " + b.Tutor.User.LastName,
                Subject = b.TutorSubject.Subject.Name, StudyLevel = b.TutorSubject.StudyLevel.Name, Campus = b.Campus.Name,
                SessionDate = b.SessionDate, StartTime = b.StartTime, EndTime = b.EndTime, Fee = b.Fee, Status = b.Status,
                FeePaid = b.Fee == 0 || _db.Fees.Any(x => x.BookingId == b.BookingId && x.Status == FeeStatus.Paid),
                CreatedAt = b.CreatedAt
            });

        public BookingDetail Get(Actor actor, int bookingId)
        {
            var b = _db.Bookings.AsNoTracking()
                .Include(x => x.Student.User).Include(x => x.Tutor.User).Include(x => x.CancelledBy)
                .FirstOrDefault(x => x.BookingId == bookingId);
            if (b == null) return null;

            var isStudent = b.Student.UserId == actor.UserId;
            var isTutor = b.Tutor.UserId == actor.UserId;
            var isStaff = RolePermissions.Has(actor, Permission.ViewOperationalReports);
            // BR-24: someone else's booking looks exactly like a missing one.
            if (!isStudent && !isTutor && !isStaff) return null;

            var now = _clock.Now;
            var row = Project(_db.Bookings.Where(x => x.BookingId == bookingId)).Single();
            var fee = _db.Fees.AsNoTracking().Where(f => f.BookingId == bookingId).OrderByDescending(f => f.FeeId).FirstOrDefault();
            var session = _db.TutoringSessions.AsNoTracking().FirstOrDefault(s => s.BookingId == bookingId);
            var review = _db.Reviews.AsNoTracking().Where(r => r.BookingId == bookingId).Select(r => new ReviewRow
            {
                ReviewId = r.ReviewId, BookingId = r.BookingId, Rating = r.Rating, Comment = r.Comment, CreatedAt = r.CreatedAt,
                IsHidden = r.IsHidden, HiddenReason = r.HiddenReason
            }).FirstOrDefault();
            var postponements = _db.PostponementRequests.AsNoTracking().Where(p => p.BookingId == bookingId)
                .OrderByDescending(p => p.PostponementId).ToList().Select(ToRow).ToList();

            var cutoff = _settings.GetInt(SettingKeys.CancellationCutoffHours, 12);
            var beforeCutoff = SchedulePolicy.IsBeforeCutoff(b.SessionDate, b.StartTime, now, cutoff);
            var d = new BookingDetail
            {
                Booking = row, StudentUserId = b.Student.UserId, TutorUserId = b.Tutor.UserId,
                StudentEmail = b.Student.User.Email, StudentCell = b.Student.User.CellNumber, TutorEmail = b.Tutor.User.Email,
                StudentMessage = b.StudentMessage, DeclineReason = b.DeclineReason, HoldExpiresAt = b.HoldExpiresAt,
                PaidAt = b.PaidAt, CancelledAt = b.CancelledAt, CancelledBy = b.CancelledBy?.FullName,
                CancelReasonCategory = b.CancelReasonCategory, CancelReason = b.CancelReason, CompletedAt = b.CompletedAt,
                Attendance = session?.Attendance, TutorNotes = session?.TutorNotes,
                FeeId = fee?.FeeId, FeeStatus = fee?.Status, Review = review,
                PendingPostponement = postponements.FirstOrDefault(p => p.Status == PostponementStatus.PendingTutorReview),
                PostponementHistory = postponements
            };

            var active = SchedulePolicy.IsActive(b.Status);
            d.CanPay = isStudent && b.Status == BookingStatus.AwaitingPayment && fee != null && fee.Status == FeeStatus.Unpaid;
            d.CanRespond = isTutor && b.Status == BookingStatus.AwaitingTutor;
            d.CanComplete = isTutor && b.Status == BookingStatus.Confirmed && SchedulePolicy.HasEnded(b.SessionDate, b.EndTime, now);
            d.CanPostpone = isStudent && b.Status == BookingStatus.Confirmed && d.PendingPostponement == null && b.SessionDate.Date + b.StartTime > now;
            d.CanReview = isStudent && b.Status == BookingStatus.Completed && review == null
                          && session != null && session.Attendance == AttendanceStatus.Attended;
            if (active)
            {
                if (isTutor || RolePermissions.Has(actor, Permission.ManagePayments))
                    d.CanCancel = true;
                else if (isStudent)
                {
                    d.CanCancel = b.Status == BookingStatus.AwaitingPayment || beforeCutoff;
                    if (!d.CanCancel)
                        d.CancelBlockedReason = $"Sessions can be cancelled up to {cutoff} hours before they start. Contact your tutor or the hub.";
                }
            }
            return d;
        }

        public ServiceResult Respond(Actor actor, int bookingId, bool accept, string reason)
        {
            RolePermissions.Demand(actor, Permission.RespondToBookings);
            var tutorId = Profiles.RequireTutor(_db, actor);
            var booking = _db.Bookings.Include(b => b.Student.User).Include(b => b.TutorSubject.Subject).Include(b => b.Tutor)
                .FirstOrDefault(b => b.BookingId == bookingId && b.TutorId == tutorId);
            if (booking == null) return ServiceResult.Fail("Booking not found.");
            if (booking.Status != BookingStatus.AwaitingTutor)
                return ServiceResult.Fail("This booking is no longer waiting for your response.");
            if (booking.Tutor.VerificationStatus != TutorVerificationStatus.Verified)
                return ServiceResult.Fail("Your tutor profile is not verified, so you cannot accept sessions.");
            if (!accept && string.IsNullOrWhiteSpace(reason))
                return ServiceResult.Fail("Tell the student why you are declining.");

            var now = _clock.Now;
            booking.TutorRespondedAt = now;
            var when = Profiles.When(booking.SessionDate, booking.StartTime, booking.EndTime);
            if (accept)
            {
                booking.Status = BookingStatus.Confirmed;
                _notifications.Notify(booking.Student.UserId, NotificationType.BookingConfirmed, "Session confirmed",
                    booking.TutorSubject.Subject.Name + " on " + when + " is confirmed by your tutor.",
                    "~/Student/Tutoring/BookingDetails.aspx?id=" + bookingId);
                _db.SaveChanges();
                return ServiceResult.Ok();
            }

            booking.Status = BookingStatus.Declined;
            booking.DeclineReason = TextRules.Clean(reason, 200);
            ReleaseSlot(booking);
            _audit.Record(actor, "BookingDeclined", "Booking", bookingId, "AwaitingTutor", "Declined", booking.DeclineReason);
            _notifications.Notify(booking.Student.UserId, NotificationType.BookingDeclined, "Session declined",
                "Your tutor declined " + booking.TutorSubject.Subject.Name + " on " + when + ": " + booking.DeclineReason
                + " Any payment is refunded to your card.", "~/Student/Tutoring/BookingDetails.aspx?id=" + bookingId);
            _db.SaveChanges();
            return RefundOrCancelFee(actor, bookingId, "Tutor declined the session");
        }

        public ServiceResult Cancel(Actor actor, int bookingId, ScheduleChangeReason category, string note)
        {
            if (TextRules.TooLong(note, 200)) return ServiceResult.Fail("The note is limited to 200 characters.");
            if (!Enum.IsDefined(typeof(ScheduleChangeReason), category)) return ServiceResult.Fail("Choose a reason.");

            var booking = _db.Bookings.Include(b => b.Student.User).Include(b => b.Tutor.User).Include(b => b.TutorSubject.Subject)
                .FirstOrDefault(b => b.BookingId == bookingId);
            if (booking == null) return ServiceResult.Fail("Booking not found.");

            var isStudent = booking.Student.UserId == actor.UserId;
            var isTutor = booking.Tutor.UserId == actor.UserId;
            var isManager = RolePermissions.Has(actor, Permission.ManagePayments);
            if (!isStudent && !isTutor && !isManager) return ServiceResult.Fail("Booking not found.");
            if (!SchedulePolicy.IsActive(booking.Status)) return ServiceResult.Fail("This booking can no longer be cancelled.");

            var now = _clock.Now;
            var cutoff = _settings.GetInt(SettingKeys.CancellationCutoffHours, 12);
            if (isStudent && !isManager && booking.Status != BookingStatus.AwaitingPayment
                && !SchedulePolicy.IsBeforeCutoff(booking.SessionDate, booking.StartTime, now, cutoff))
                return ServiceResult.Fail($"Sessions can be cancelled up to {cutoff} hours before they start. Contact your tutor or the hub.");

            var previous = booking.Status;
            booking.Status = BookingStatus.Cancelled;
            booking.CancelledAt = now;
            booking.CancelledByUserId = actor.UserId;
            booking.CancelReasonCategory = category;
            booking.CancelReason = TextRules.Clean(note, 200);
            booking.HoldExpiresAt = null;
            ReleaseSlot(booking);

            // Pending postponements die with the booking (ported from TutorConnect CancelAsync).
            foreach (var p in _db.PostponementRequests.Where(p => p.BookingId == bookingId && p.Status == PostponementStatus.PendingTutorReview))
            {
                p.Status = PostponementStatus.Cancelled;
                p.RespondedAt = now;
            }

            _audit.Record(actor, "BookingCancelled", "Booking", bookingId, previous.ToString(), "Cancelled",
                category + (booking.CancelReason == null ? "" : ": " + booking.CancelReason));
            var when = Profiles.When(booking.SessionDate, booking.StartTime, booking.EndTime);
            var message = booking.TutorSubject.Subject.Name + " on " + when + " was cancelled by " + actor.UserName + ".";
            if (!isStudent)
                _notifications.Notify(booking.Student.UserId, NotificationType.BookingCancelled, "Session cancelled",
                    message + " Any payment is refunded.", "~/Student/Tutoring/BookingDetails.aspx?id=" + bookingId);
            if (!isTutor && previous != BookingStatus.AwaitingPayment)
                _notifications.Notify(booking.Tutor.UserId, NotificationType.BookingCancelled, "Session cancelled", message,
                    "~/Tutor/SessionDetails.aspx?id=" + bookingId);
            _db.SaveChanges();
            return RefundOrCancelFee(actor, bookingId, "Session cancelled");
        }

        public ServiceResult Complete(Actor actor, int bookingId, AttendanceStatus attendance, string notes)
        {
            RolePermissions.Demand(actor, Permission.RespondToBookings);
            var tutorId = Profiles.RequireTutor(_db, actor);
            if (attendance != AttendanceStatus.Attended && attendance != AttendanceStatus.StudentAbsent)
                return ServiceResult.Fail("Record whether the student attended.");
            if (TextRules.TooLong(notes, 1000)) return ServiceResult.Fail("Session notes are limited to 1000 characters.");

            var booking = _db.Bookings.Include(b => b.Student.User).Include(b => b.TutorSubject.Subject)
                .FirstOrDefault(b => b.BookingId == bookingId && b.TutorId == tutorId);
            if (booking == null) return ServiceResult.Fail("Booking not found.");
            if (booking.Status != BookingStatus.Confirmed) return ServiceResult.Fail("Only a confirmed session can be completed.");
            var now = _clock.Now;
            // BR-23: not before the session has ended.
            if (!SchedulePolicy.HasEnded(booking.SessionDate, booking.EndTime, now))
                return ServiceResult.Fail("You can complete the session once it has ended.");

            booking.Status = BookingStatus.Completed;
            booking.CompletedAt = now;
            _db.TutoringSessions.Add(new TutoringSession
            {
                BookingId = bookingId, SessionDate = booking.SessionDate, Attendance = attendance,
                HoursDelivered = attendance == AttendanceStatus.Attended ? SchedulePolicy.HoursBetween(booking.StartTime, booking.EndTime) : 0m,
                TutorNotes = TextRules.Clean(notes, 1000), CreatedAt = now
            });
            _notifications.Notify(booking.Student.UserId, NotificationType.BookingCompleted, "Session completed",
                attendance == AttendanceStatus.Attended
                    ? "How was " + booking.TutorSubject.Subject.Name + "? Leave a review for your tutor."
                    : "Your tutor recorded that you missed " + booking.TutorSubject.Subject.Name + " on " + booking.SessionDate.ToString("d MMM") + ".",
                "~/Student/Tutoring/BookingDetails.aspx?id=" + bookingId);
            try
            {
                _db.SaveChanges();
            }
            catch (DbUpdateException ex) when (DbErrors.IsUniqueViolation(ex))
            {
                return ServiceResult.Fail("This session has already been completed.");
            }
            return ServiceResult.Ok();
        }

        public int ExpireStale()
        {
            var now = _clock.Now;
            var today = now.Date;
            var changed = 0;

            // Unpaid reservations past their hold release the slot for other students.
            var unpaid = _db.Bookings.Include(b => b.Student)
                .Where(b => b.Status == BookingStatus.AwaitingPayment && (b.HoldExpiresAt < now || b.SessionDate < today))
                .ToList();
            foreach (var b in unpaid)
            {
                b.Status = BookingStatus.Expired;
                b.HoldExpiresAt = null;
                ReleaseSlot(b);
                FeeLedger.CancelUnpaid(_db, bookingId: b.BookingId);
                _notifications.Notify(b.Student.UserId, NotificationType.BookingExpired, "Reservation expired",
                    "Your reservation " + b.Reference + " was released because it was not paid in time.",
                    "~/Student/Tutoring/BookSession.aspx");
                changed++;
            }

            // A paid booking the tutor never answered before it started is declined and refunded.
            var unanswered = _db.Bookings.Include(b => b.Student)
                .Where(b => b.Status == BookingStatus.AwaitingTutor && b.SessionDate <= today).ToList()
                .Where(b => b.SessionDate + b.StartTime <= now).ToList();
            foreach (var b in unanswered)
            {
                b.Status = BookingStatus.Declined;
                b.DeclineReason = "The tutor did not respond before the session started.";
                ReleaseSlot(b);
                _notifications.Notify(b.Student.UserId, NotificationType.BookingDeclined, "Session not confirmed",
                    "Your tutor did not confirm " + b.Reference + " in time. Your payment is refunded.",
                    "~/Student/Tutoring/BookingDetails.aspx?id=" + b.BookingId);
                changed++;
            }
            _db.SaveChanges();
            foreach (var b in unanswered)
                RefundOrCancelFee(Actor.System, b.BookingId, "Tutor did not respond");
            return changed;
        }

        private void ReleaseSlot(Booking booking)
        {
            var slot = _db.TutorAvailability.Find(booking.AvailabilityId);
            if (slot != null && !_db.Bookings.Any(b => b.AvailabilityId == slot.AvailabilityId && b.BookingId != booking.BookingId
                                                        && Active.Contains(b.Status)))
                slot.IsBooked = false;
        }

        private ServiceResult RefundOrCancelFee(Actor actor, int bookingId, string reason)
        {
            var fee = _db.Fees.Where(f => f.BookingId == bookingId).OrderByDescending(f => f.FeeId).FirstOrDefault();
            if (fee == null) return ServiceResult.Ok();
            if (fee.Status == FeeStatus.Unpaid)
            {
                fee.Status = FeeStatus.Cancelled;
                _db.SaveChanges();
                return ServiceResult.Ok();
            }
            return fee.Status == FeeStatus.Paid ? _payments.RefundForRule(actor, fee.FeeId, reason) : ServiceResult.Ok();
        }

        internal static PostponementRow ToRow(PostponementRequest p) => new PostponementRow
        {
            PostponementId = p.PostponementId, BookingId = p.BookingId, OriginalDate = p.OriginalDate, OriginalStart = p.OriginalStart,
            OriginalEnd = p.OriginalEnd, ProposedDate = p.ProposedDate, ProposedStart = p.ProposedStart, ProposedEnd = p.ProposedEnd,
            ReasonCategory = p.ReasonCategory, StudentNote = p.StudentNote, Status = p.Status, CreatedAt = p.CreatedAt,
            TutorResponseNote = p.TutorResponseNote
        };
    }

    public sealed class PostponementService : IPostponementService
    {
        private static readonly BookingStatus[] Active = { BookingStatus.AwaitingPayment, BookingStatus.AwaitingTutor, BookingStatus.Confirmed };

        private readonly StudyLinkDbContext _db;
        private readonly ISettingsService _settings;
        private readonly IAuditService _audit;
        private readonly INotificationService _notifications;
        private readonly IClock _clock;

        public PostponementService(StudyLinkDbContext db, ISettingsService settings, IAuditService audit,
                                   INotificationService notifications, IClock clock)
        {
            _db = db;
            _settings = settings;
            _audit = audit;
            _notifications = notifications;
            _clock = clock;
        }

        public ServiceResult Propose(Actor actor, int bookingId, DateTime date, TimeSpan start, TimeSpan end, ScheduleChangeReason reason, string note)
        {
            var studentId = Profiles.RequireStudent(_db, actor);
            var now = _clock.Now;
            if (end <= start) return ServiceResult.Fail("The end time must be after the start time.");
            if (date.Date + start <= now) return ServiceResult.Fail("The new time must be in the future.");
            if (date.Date > now.Date.AddDays(_settings.GetInt(SettingKeys.PostponementMaxDaysAhead, 120)))
                return ServiceResult.Fail("A session can be postponed at most " + _settings.GetInt(SettingKeys.PostponementMaxDaysAhead, 120) + " days ahead.");
            if (TextRules.TooLong(note, 200)) return ServiceResult.Fail("The note is limited to 200 characters.");
            if (!Enum.IsDefined(typeof(ScheduleChangeReason), reason)) return ServiceResult.Fail("Choose a reason.");

            var booking = _db.Bookings.Include(b => b.Tutor).Include(b => b.TutorSubject.Subject)
                .FirstOrDefault(b => b.BookingId == bookingId && b.StudentId == studentId);
            if (booking == null) return ServiceResult.Fail("Booking not found.");
            if (booking.Status != BookingStatus.Confirmed) return ServiceResult.Fail("Only a confirmed session can be postponed.");
            // The fee was paid for this length of session, so the new time must be the same length.
            if (end - start != booking.EndTime - booking.StartTime)
                return ServiceResult.Fail("The new time must be the same length as the booked session (" + SchedulePolicy.HoursBetween(booking.StartTime, booking.EndTime) + " h).");
            if (_db.PostponementRequests.Any(p => p.BookingId == bookingId && p.Status == PostponementStatus.PendingTutorReview))
                return ServiceResult.Fail("You already have a postponement waiting for the tutor.");

            _db.PostponementRequests.Add(new PostponementRequest
            {
                BookingId = bookingId, StudentId = studentId, TutorId = booking.TutorId,
                OriginalDate = booking.SessionDate, OriginalStart = booking.StartTime, OriginalEnd = booking.EndTime,
                ProposedDate = date.Date, ProposedStart = start, ProposedEnd = end, ReasonCategory = reason,
                StudentNote = TextRules.Clean(note, 200), CreatedAt = now
            });
            _audit.Record(actor, "PostponementRequested", "Booking", bookingId, details: Profiles.When(date, start, end));
            _notifications.Notify(booking.Tutor.UserId, NotificationType.BookingPostponeRequested, "Postponement requested",
                "A student asked to move " + booking.TutorSubject.Subject.Name + " to " + Profiles.When(date, start, end) + ".",
                "~/Tutor/SessionDetails.aspx?id=" + bookingId);
            try
            {
                _db.SaveChanges();
            }
            catch (DbUpdateException ex) when (DbErrors.IsUniqueViolation(ex))
            {
                return ServiceResult.Fail("You already have a postponement waiting for the tutor.");
            }
            return ServiceResult.Ok();
        }

        public ServiceResult Approve(Actor actor, int postponementId)
        {
            RolePermissions.Demand(actor, Permission.RespondToBookings);
            var tutorId = Profiles.RequireTutor(_db, actor);
            try
            {
                using (var tx = _db.Database.BeginTransaction(IsolationLevel.Serializable))
                {
                    var p = _db.PostponementRequests.FirstOrDefault(x => x.PostponementId == postponementId && x.TutorId == tutorId);
                    if (p == null || p.Status != PostponementStatus.PendingTutorReview)
                        return ServiceResult.Fail("This request has already been answered.");
                    var booking = _db.Bookings.Include(b => b.Student.User).Include(b => b.TutorSubject.Subject)
                        .Single(b => b.BookingId == p.BookingId);
                    if (booking.Status != BookingStatus.Confirmed)
                        return ServiceResult.Fail("The session is no longer confirmed.");
                    if (p.ProposedDate + p.ProposedStart <= _clock.Now)
                        return ServiceResult.Fail("The proposed time has already passed. Decline and ask for a new time.");

                    var date = p.ProposedDate;
                    // Same overlap guards as a new booking, excluding this booking itself.
                    if (_db.Bookings.Any(b => b.TutorId == tutorId && b.BookingId != booking.BookingId && Active.Contains(b.Status)
                                              && b.SessionDate == date && b.StartTime < p.ProposedEnd && p.ProposedStart < b.EndTime)
                        || _db.TutorAvailability.Any(a => a.TutorSubject.TutorId == tutorId && a.AvailabilityId != booking.AvailabilityId
                                                          && a.AvailabilityDate == date && a.StartTime < p.ProposedEnd && p.ProposedStart < a.EndTime && a.IsBooked))
                        return ServiceResult.Fail("You already have a session at the proposed time.");
                    if (_db.Bookings.Any(b => b.StudentId == booking.StudentId && b.BookingId != booking.BookingId && Active.Contains(b.Status)
                                              && b.SessionDate == date && b.StartTime < p.ProposedEnd && p.ProposedStart < b.EndTime))
                        return ServiceResult.Fail("The student already has another session at the proposed time.");

                    var now = _clock.Now;
                    var newSlot = _db.TutorAvailability.Add(new TutorAvailability
                    {
                        TutorSubjectId = booking.TutorSubjectId, CampusId = booking.CampusId, AvailabilityDate = date,
                        StartTime = p.ProposedStart, EndTime = p.ProposedEnd, IsBooked = true, CreatedAt = now
                    });
                    var oldSlot = _db.TutorAvailability.Find(booking.AvailabilityId);
                    if (oldSlot != null) oldSlot.IsBooked = false;

                    booking.Availability = newSlot;
                    booking.SessionDate = date;
                    booking.StartTime = p.ProposedStart;
                    booking.EndTime = p.ProposedEnd;
                    p.Status = PostponementStatus.Approved;
                    p.RespondedAt = now;

                    _audit.Record(actor, "PostponementApproved", "Booking", booking.BookingId,
                        Profiles.When(p.OriginalDate, p.OriginalStart, p.OriginalEnd), Profiles.When(date, p.ProposedStart, p.ProposedEnd));
                    _notifications.Notify(booking.Student.UserId, NotificationType.BookingPostponeApproved, "Postponement approved",
                        booking.TutorSubject.Subject.Name + " is now on " + Profiles.When(date, p.ProposedStart, p.ProposedEnd) + ".",
                        "~/Student/Tutoring/BookingDetails.aspx?id=" + booking.BookingId);
                    _db.SaveChanges();
                    tx.Commit();
                    return ServiceResult.Ok();
                }
            }
            catch (Exception ex) when (DbErrors.IsConcurrencyConflict(ex))
            {
                return ServiceResult.Fail("Something changed while approving. Refresh and try again.");
            }
        }

        public ServiceResult Decline(Actor actor, int postponementId, string note)
        {
            RolePermissions.Demand(actor, Permission.RespondToBookings);
            var tutorId = Profiles.RequireTutor(_db, actor);
            if (string.IsNullOrWhiteSpace(note)) return ServiceResult.Fail("Tell the student why you cannot move the session.");
            if (TextRules.TooLong(note, 200)) return ServiceResult.Fail("The note is limited to 200 characters.");
            var p = _db.PostponementRequests.Include(x => x.Student.User)
                .FirstOrDefault(x => x.PostponementId == postponementId && x.TutorId == tutorId);
            if (p == null || p.Status != PostponementStatus.PendingTutorReview)
                return ServiceResult.Fail("This request has already been answered.");
            p.Status = PostponementStatus.Declined;
            p.TutorResponseNote = note.Trim();
            p.RespondedAt = _clock.Now;
            _audit.Record(actor, "PostponementDeclined", "Booking", p.BookingId, details: p.TutorResponseNote);
            _notifications.Notify(p.Student.UserId, NotificationType.BookingPostponeDeclined, "Postponement declined",
                "Your tutor could not move the session: " + p.TutorResponseNote + " The original time stands.",
                "~/Student/Tutoring/BookingDetails.aspx?id=" + p.BookingId);
            _db.SaveChanges();
            return ServiceResult.Ok();
        }

        public ServiceResult Withdraw(Actor actor, int postponementId)
        {
            var studentId = Profiles.RequireStudent(_db, actor);
            var p = _db.PostponementRequests.Include(x => x.Tutor.User)
                .FirstOrDefault(x => x.PostponementId == postponementId && x.StudentId == studentId);
            if (p == null || p.Status != PostponementStatus.PendingTutorReview)
                return ServiceResult.Fail("This request has already been answered.");
            p.Status = PostponementStatus.Cancelled;
            p.RespondedAt = _clock.Now;
            _audit.Record(actor, "PostponementWithdrawn", "Booking", p.BookingId);
            _notifications.Notify(p.Tutor.UserId, NotificationType.BookingPostponeWithdrawn, "Postponement withdrawn",
                "The student withdrew their request to move a session.", "~/Tutor/SessionDetails.aspx?id=" + p.BookingId);
            _db.SaveChanges();
            return ServiceResult.Ok();
        }

        public IList<PostponementRow> PendingForTutor(Actor actor)
        {
            var tutorId = Profiles.RequireTutor(_db, actor);
            return _db.PostponementRequests.AsNoTracking()
                .Where(p => p.TutorId == tutorId && p.Status == PostponementStatus.PendingTutorReview)
                .OrderBy(p => p.CreatedAt)
                .Select(p => new { P = p, Student = p.Student.User.FirstName + " " + p.Student.User.LastName, p.Booking.Reference, Subject = p.Booking.TutorSubject.Subject.Name })
                .ToList()
                .Select(x =>
                {
                    var row = BookingService.ToRow(x.P);
                    row.StudentName = x.Student;
                    row.BookingReference = x.Reference;
                    row.Subject = x.Subject;
                    return row;
                }).ToList();
        }
    }

    public sealed class ReviewService : IReviewService
    {
        private readonly StudyLinkDbContext _db;
        private readonly IAuditService _audit;
        private readonly INotificationService _notifications;
        private readonly IClock _clock;

        public ReviewService(StudyLinkDbContext db, IAuditService audit, INotificationService notifications, IClock clock)
        {
            _db = db;
            _audit = audit;
            _notifications = notifications;
            _clock = clock;
        }

        public bool CanReview(Actor actor, int bookingId)
        {
            var studentId = Profiles.StudentId(_db, actor);
            // BR-16/17/19: own booking, completed, attended, not yet reviewed.
            return studentId.HasValue && _db.Bookings.Any(b =>
                b.BookingId == bookingId && b.StudentId == studentId && b.Status == BookingStatus.Completed
                && _db.TutoringSessions.Any(s => s.BookingId == bookingId && s.Attendance == AttendanceStatus.Attended)
                && !_db.Reviews.Any(r => r.BookingId == bookingId));
        }

        public ServiceResult Create(Actor actor, int bookingId, int rating, string comment)
        {
            RolePermissions.Demand(actor, Permission.ReviewTutors);
            if (rating < 1 || rating > 5) return ServiceResult.Fail("Choose a rating from 1 to 5 stars.");
            if (TextRules.TooLong(comment, 500)) return ServiceResult.Fail("Comments are limited to 500 characters.");
            if (!CanReview(actor, bookingId)) return ServiceResult.Fail("You can review only your own completed sessions, once.");

            var booking = _db.Bookings.Include(b => b.Tutor).Single(b => b.BookingId == bookingId);
            _db.Reviews.Add(new Review { BookingId = bookingId, Rating = (byte)rating, Comment = TextRules.Clean(comment, 500), CreatedAt = _clock.Now });
            _notifications.Notify(booking.Tutor.UserId, NotificationType.ReviewReceived, "New review",
                "A student rated your session " + rating + "/5.", "~/Tutor/Reviews.aspx");
            try
            {
                _db.SaveChanges();
            }
            catch (DbUpdateException ex) when (DbErrors.IsUniqueViolation(ex))
            {
                return ServiceResult.Fail("You have already reviewed this session."); // AT-21 backstop
            }
            return ServiceResult.Ok();
        }

        public IList<ReviewRow> ForTutor(int tutorId) =>
            Query(_db.Reviews.Where(r => r.Booking.TutorId == tutorId && !r.IsHidden)).ToList();

        public IList<ReviewRow> ForStudent(Actor actor)
        {
            var studentId = Profiles.RequireStudent(_db, actor);
            // The author still sees a hidden review, with the reason (TutorConnect moderation rule).
            return Query(_db.Reviews.Where(r => r.Booking.StudentId == studentId)).ToList();
        }

        public IList<ReviewRow> All(Actor actor, bool hiddenOnly)
        {
            RolePermissions.Demand(actor, Permission.ModerateReviews);
            return Query(_db.Reviews.Where(r => !hiddenOnly || r.IsHidden)).Take(300).ToList();
        }

        public ServiceResult SetHidden(Actor actor, int reviewId, bool hidden, string reason)
        {
            RolePermissions.Demand(actor, Permission.ModerateReviews);
            var review = _db.Reviews.Find(reviewId);
            if (review == null) return ServiceResult.Fail("Review not found.");
            if (hidden && string.IsNullOrWhiteSpace(reason)) return ServiceResult.Fail("Give a reason for hiding the review.");
            if (TextRules.TooLong(reason, 200)) return ServiceResult.Fail("The reason is limited to 200 characters.");
            review.IsHidden = hidden;
            review.HiddenReason = hidden ? reason.Trim() : null;
            review.HiddenAt = hidden ? _clock.Now : (DateTime?)null;
            review.HiddenByUserId = hidden ? actor.UserId : (int?)null;
            _audit.Record(actor, hidden ? "ReviewHidden" : "ReviewRestored", "Review", reviewId, details: review.HiddenReason);
            _db.SaveChanges();
            return ServiceResult.Ok();
        }

        private static IQueryable<ReviewRow> Query(IQueryable<Review> q) =>
            q.AsNoTracking().OrderByDescending(r => r.CreatedAt).Select(r => new ReviewRow
            {
                ReviewId = r.ReviewId, BookingId = r.BookingId,
                StudentName = r.Booking.Student.User.FirstName + " " + r.Booking.Student.User.LastName.Substring(0, 1) + ".",
                TutorName = r.Booking.Tutor.User.FirstName + " " + r.Booking.Tutor.User.LastName,
                Subject = r.Booking.TutorSubject.Subject.Name, Rating = r.Rating, Comment = r.Comment, CreatedAt = r.CreatedAt,
                IsHidden = r.IsHidden, HiddenReason = r.HiddenReason
            });
    }

    public sealed class TimesheetService : ITimesheetService
    {
        private readonly StudyLinkDbContext _db;
        private readonly ISettingsService _settings;
        private readonly IAuditService _audit;
        private readonly INotificationService _notifications;
        private readonly IClock _clock;

        public TimesheetService(StudyLinkDbContext db, ISettingsService settings, IAuditService audit,
                                INotificationService notifications, IClock clock)
        {
            _db = db;
            _settings = settings;
            _audit = audit;
            _notifications = notifications;
            _clock = clock;
        }

        public TimesheetView Week(Actor actor, DateTime anyDayInWeek)
        {
            var tutorId = Profiles.RequireTutor(_db, actor);
            var weekStart = SchedulePolicy.WeekStart(anyDayInWeek);
            var sheet = Sync(tutorId, weekStart);
            return sheet == null
                ? new TimesheetView { TutorId = tutorId, WeekStart = weekStart, Status = TimesheetStatus.Draft }
                : View(sheet.TimesheetId);
        }

        /// <summary>
        /// Brings an editable timesheet up to date with the attended sessions in its week (UC11).
        /// Creates the timesheet only when there is something to record.
        /// </summary>
        private Timesheet Sync(int tutorId, DateTime weekStart)
        {
            var weekEnd = weekStart.AddDays(7);
            var sheet = _db.Timesheets.Include(t => t.Entries).FirstOrDefault(t => t.TutorId == tutorId && t.WeekStart == weekStart);
            if (sheet != null && sheet.Status != TimesheetStatus.Draft && sheet.Status != TimesheetStatus.Rejected)
                return sheet;

            var sessions = _db.TutoringSessions
                .Where(s => s.Booking.TutorId == tutorId && s.SessionDate >= weekStart && s.SessionDate < weekEnd
                            && s.Attendance == AttendanceStatus.Attended)
                .Select(s => new { s.BookingId, s.SessionDate, s.HoursDelivered, s.Booking.Reference, Subject = s.Booking.TutorSubject.Subject.Name })
                .ToList();
            if (sheet == null && sessions.Count == 0)
                return null;
            if (sheet == null)
                sheet = _db.Timesheets.Add(new Timesheet { TutorId = tutorId, WeekStart = weekStart, Status = TimesheetStatus.Draft, CreatedAt = _clock.Now });

            foreach (var s in sessions.Where(s => sheet.Entries.All(e => e.BookingId != s.BookingId)))
                sheet.Entries.Add(new TimesheetEntry
                {
                    BookingId = s.BookingId, WorkDate = s.SessionDate, Hours = s.HoursDelivered,
                    Description = "Tutoring session " + s.Reference + " – " + s.Subject
                });
            sheet.TotalHours = sheet.Entries.Sum(e => e.Hours);
            _db.SaveChanges();
            return sheet;
        }

        private TimesheetView View(int timesheetId)
        {
            var t = _db.Timesheets.AsNoTracking().Include(x => x.Tutor.User).Include(x => x.ReviewedBy)
                .Single(x => x.TimesheetId == timesheetId);
            return new TimesheetView
            {
                TimesheetId = t.TimesheetId, TutorId = t.TutorId, TutorName = t.Tutor.User.FullName, WeekStart = t.WeekStart,
                Status = t.Status, TotalHours = t.TotalHours, SubmittedAt = t.SubmittedAt, ReviewedAt = t.ReviewedAt,
                ReviewedBy = t.ReviewedBy?.FullName, ReviewNote = t.ReviewNote,
                Entries = _db.TimesheetEntries.AsNoTracking().Where(e => e.TimesheetId == timesheetId)
                    .OrderBy(e => e.WorkDate).ThenBy(e => e.EntryId)
                    .Select(e => new TimesheetEntryRow
                    {
                        EntryId = e.EntryId, WorkDate = e.WorkDate, Hours = e.Hours, Description = e.Description,
                        BookingId = e.BookingId, BookingReference = e.Booking.Reference
                    }).ToList()
            };
        }

        public ServiceResult AddEntry(Actor actor, DateTime workDate, decimal hours, string description)
        {
            RolePermissions.Demand(actor, Permission.SubmitTimesheet);
            var tutorId = Profiles.RequireTutor(_db, actor);
            if (hours < 0.25m || hours > 12m) return ServiceResult.Fail("Hours must be between 0.25 and 12.");
            if (string.IsNullOrWhiteSpace(description) || description.Trim().Length > 200)
                return ServiceResult.Fail("Describe the work (200 characters max).");
            if (workDate.Date > _clock.Today) return ServiceResult.Fail("You cannot record hours in the future.");

            var weekStart = SchedulePolicy.WeekStart(workDate);
            var sheet = Sync(tutorId, weekStart)
                        ?? _db.Timesheets.Add(new Timesheet { TutorId = tutorId, WeekStart = weekStart, Status = TimesheetStatus.Draft, CreatedAt = _clock.Now });
            if (sheet.Status != TimesheetStatus.Draft && sheet.Status != TimesheetStatus.Rejected)
                return ServiceResult.Fail("This week's timesheet has already been submitted.");
            if (sheet.Entries.Where(e => e.WorkDate == workDate.Date).Sum(e => e.Hours) + hours > 24)
                return ServiceResult.Fail("That would record more than 24 hours on one day.");

            sheet.Entries.Add(new TimesheetEntry { WorkDate = workDate.Date, Hours = Math.Round(hours, 2), Description = description.Trim() });
            sheet.TotalHours = sheet.Entries.Sum(e => e.Hours);
            _db.SaveChanges();
            return ServiceResult.Ok();
        }

        public ServiceResult RemoveEntry(Actor actor, int entryId)
        {
            var tutorId = Profiles.RequireTutor(_db, actor);
            var entry = _db.TimesheetEntries.Include(e => e.Timesheet).FirstOrDefault(e => e.EntryId == entryId && e.Timesheet.TutorId == tutorId);
            if (entry == null) return ServiceResult.Fail("Entry not found.");
            if (entry.BookingId.HasValue) return ServiceResult.Fail("Session hours come from the booking and cannot be removed.");
            if (entry.Timesheet.Status != TimesheetStatus.Draft && entry.Timesheet.Status != TimesheetStatus.Rejected)
                return ServiceResult.Fail("This timesheet has already been submitted.");
            var sheet = entry.Timesheet;
            _db.TimesheetEntries.Remove(entry);
            _db.SaveChanges();
            sheet.TotalHours = _db.TimesheetEntries.Where(e => e.TimesheetId == sheet.TimesheetId).Select(e => (decimal?)e.Hours).Sum() ?? 0;
            _db.SaveChanges();
            return ServiceResult.Ok();
        }

        public ServiceResult Submit(Actor actor, DateTime anyDayInWeek)
        {
            RolePermissions.Demand(actor, Permission.SubmitTimesheet);
            var tutorId = Profiles.RequireTutor(_db, actor);
            var weekStart = SchedulePolicy.WeekStart(anyDayInWeek);
            if (weekStart > _clock.Today) return ServiceResult.Fail("You can submit a week once it has started.");
            var sheet = Sync(tutorId, weekStart);
            if (sheet == null || sheet.Entries.Count == 0) return ServiceResult.Fail("There are no hours to submit for this week.");
            if (sheet.Status != TimesheetStatus.Draft && sheet.Status != TimesheetStatus.Rejected)
                return ServiceResult.Fail("This timesheet has already been submitted.");

            sheet.Status = TimesheetStatus.Submitted;
            sheet.SubmittedAt = _clock.Now;
            sheet.ReviewNote = null;
            _audit.Record(actor, "TimesheetSubmitted", "Timesheet", sheet.TimesheetId, newState: sheet.TotalHours + " h");
            _notifications.NotifyHolders(Permission.ReviewTimesheets, NotificationType.TimesheetSubmitted, "Timesheet submitted",
                actor.UserName + " submitted " + sheet.TotalHours + " hours for the week of " + weekStart.ToString("d MMM") + ".",
                "~/Operations/Timesheets.aspx?id=" + sheet.TimesheetId);
            _db.SaveChanges();
            return ServiceResult.Ok();
        }

        public IList<TimesheetRow> History(Actor actor)
        {
            var tutorId = Profiles.RequireTutor(_db, actor);
            return Rows(_db.Timesheets.Where(t => t.TutorId == tutorId)).ToList();
        }

        public IList<TimesheetRow> ForReview(Actor actor, TimesheetStatus? status)
        {
            RolePermissions.Demand(actor, Permission.ReviewTimesheets);
            return Rows(_db.Timesheets.Where(t => !status.HasValue || t.Status == status.Value)).Take(200).ToList();
        }

        private static IQueryable<TimesheetRow> Rows(IQueryable<Timesheet> q) =>
            q.AsNoTracking().OrderByDescending(t => t.WeekStart).Select(t => new TimesheetRow
            {
                TimesheetId = t.TimesheetId, TutorId = t.TutorId, TutorName = t.Tutor.User.FirstName + " " + t.Tutor.User.LastName,
                WeekStart = t.WeekStart, Status = t.Status, TotalHours = t.TotalHours, Entries = t.Entries.Count(),
                SubmittedAt = t.SubmittedAt, ReviewNote = t.ReviewNote
            });

        public TimesheetView Get(Actor actor, int timesheetId)
        {
            var owner = _db.Timesheets.Where(t => t.TimesheetId == timesheetId).Select(t => (int?)t.Tutor.UserId).FirstOrDefault();
            if (owner == null) return null;
            if (owner != actor.UserId && !RolePermissions.Has(actor, Permission.ReviewTimesheets)) return null;
            return View(timesheetId);
        }

        public ServiceResult Review(Actor actor, int timesheetId, bool approve, string note)
        {
            RolePermissions.Demand(actor, Permission.ReviewTimesheets);
            var sheet = _db.Timesheets.Include(t => t.Tutor).FirstOrDefault(t => t.TimesheetId == timesheetId);
            if (sheet == null) return ServiceResult.Fail("Timesheet not found.");
            if (sheet.Status != TimesheetStatus.Submitted) return ServiceResult.Fail("Only a submitted timesheet can be reviewed.");
            if (sheet.Tutor.UserId == actor.UserId) return ServiceResult.Fail("You cannot approve your own timesheet.");
            if (!approve && string.IsNullOrWhiteSpace(note)) return ServiceResult.Fail("Explain what the tutor must correct.");

            sheet.Status = approve ? TimesheetStatus.Approved : TimesheetStatus.Rejected;
            sheet.ReviewedByUserId = actor.UserId;
            sheet.ReviewedAt = _clock.Now;
            sheet.ReviewNote = TextRules.Clean(note, 300);
            _audit.Record(actor, approve ? "TimesheetApproved" : "TimesheetRejected", "Timesheet", timesheetId,
                "Submitted", sheet.Status.ToString(), sheet.ReviewNote);
            _notifications.Notify(sheet.Tutor.UserId, NotificationType.TimesheetReviewed,
                approve ? "Timesheet approved" : "Timesheet returned",
                "Your timesheet for the week of " + sheet.WeekStart.ToString("d MMM") + (approve ? " was approved." : " needs changes: " + sheet.ReviewNote),
                "~/Tutor/Timesheet.aspx?week=" + sheet.WeekStart.ToString("yyyy-MM-dd"));
            _db.SaveChanges();
            return ServiceResult.Ok();
        }

        public EarningsView Earnings(Actor actor, DateTime from, DateTime to)
        {
            var tutorId = Profiles.RequireTutor(_db, actor);
            from = from.Date;
            to = to.Date;
            var toExclusive = to.AddDays(1);
            var commission = _settings.GetDecimal(SettingKeys.TutorCommissionPercent, 20);

            var lines = _db.TutoringSessions.AsNoTracking()
                .Where(s => s.Booking.TutorId == tutorId && s.Attendance != AttendanceStatus.TutorAbsent
                            && s.SessionDate >= from && s.SessionDate < toExclusive)
                .OrderByDescending(s => s.SessionDate)
                .Select(s => new
                {
                    s.SessionDate, s.Booking.Reference, s.Booking.Fee, s.HoursDelivered,
                    Student = s.Booking.Student.User.FirstName + " " + s.Booking.Student.User.LastName,
                    Subject = s.Booking.TutorSubject.Subject.Name
                }).ToList()
                .Select(s =>
                {
                    var net = MoneyPolicy.TutorShare(s.Fee, commission);
                    return new EarningsLine
                    {
                        SessionDate = s.SessionDate, Reference = s.Reference, StudentName = s.Student, Subject = s.Subject,
                        Hours = s.HoursDelivered, Fee = s.Fee, Net = net, Commission = s.Fee - net
                    };
                }).ToList();

            return new EarningsView
            {
                From = from, To = to, CommissionPercent = commission, Lines = lines, Sessions = lines.Count,
                Hours = lines.Sum(l => l.Hours), Gross = lines.Sum(l => l.Fee), Commission = lines.Sum(l => l.Commission),
                Net = lines.Sum(l => l.Net),
                ApprovedTimesheetHours = _db.Timesheets.Where(t => t.TutorId == tutorId && t.Status == TimesheetStatus.Approved
                                                                   && t.WeekStart >= from && t.WeekStart < toExclusive)
                                             .Select(t => (decimal?)t.TotalHours).Sum() ?? 0
            };
        }
    }
}
