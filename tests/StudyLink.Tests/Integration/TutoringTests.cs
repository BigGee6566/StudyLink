using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StudyLink.Data.Entities;
using StudyLink.Services.DTOs;
using StudyLink.Services.Infrastructure;

namespace StudyLink.Tests.Integration
{
    [TestClass]
    [TestCategory("Integration")]
    public class TutoringTests
    {
        [TestMethod]
        public void TwoStudentsReservingTheSameSlotAtOnce_ExactlyOneWins()
        {
            using (var w = new World())
            {
                var tutor = w.NewTutor();
                var slot = w.Slot(tutor, w.Clock.Today.AddDays(2), 10, 11);
                var students = Enumerable.Range(0, 6).Select(_ => w.NewStudent()).ToList();

                // Each student races on its own connection, as separate web requests would.
                var gate = new ManualResetEventSlim(false);
                var tasks = students.Select(s => Task.Run(() =>
                {
                    using (var own = w.NewServices())
                    {
                        gate.Wait();
                        return own.Bookings.Reserve(s, slot, null);
                    }
                })).ToList();
                gate.Set();
                Task.WaitAll(tasks.Cast<Task>().ToArray());

                var results = tasks.Select(t => t.Result).ToList();
                Assert.AreEqual(1, results.Count(r => r.Succeeded), "exactly one reservation may succeed");
                Assert.IsTrue(results.Where(r => !r.Succeeded).All(r => r.Error.Contains("just booked")), string.Join(" | ", results.Select(r => r.Error)));

                w.Refresh();
                var active = new[] { BookingStatus.AwaitingPayment, BookingStatus.AwaitingTutor, BookingStatus.Confirmed };
                Assert.AreEqual(1, w.S.Db.Bookings.Count(b => b.AvailabilityId == slot && active.Contains(b.Status)));
                Assert.IsTrue(w.S.Db.TutorAvailability.Find(slot).IsBooked);
            }
        }

        [TestMethod]
        public void StudentCannotHoldTwoOverlappingSessions()
        {
            using (var w = new World())
            {
                var day = w.Clock.Today.AddDays(3);
                var tutorA = w.NewTutor();
                var tutorB = w.NewTutor();
                var slotA = w.Slot(tutorA, day, 10, 12);
                var slotB = w.Slot(tutorB, day, 11, 13);
                var student = w.NewStudent();

                Assert.IsTrue(w.S.Bookings.Reserve(student, slotA, "Integration by parts").Succeeded);
                var second = w.S.Bookings.Reserve(student, slotB, null);

                Assert.IsFalse(second.Succeeded);
                StringAssert.Contains(second.Error, "overlaps");
            }
        }

        [TestMethod]
        public void BackToBackSessionsDoNotClash()
        {
            using (var w = new World())
            {
                var day = w.Clock.Today.AddDays(3);
                var tutor = w.NewTutor();
                var first = w.Slot(tutor, day, 10, 11);
                var second = w.Slot(tutor, day, 11, 12); // half-open intervals: 11:00 end and 11:00 start
                var student = w.NewStudent();

                Assert.IsTrue(w.S.Bookings.Reserve(student, first, null).Succeeded);
                Assert.IsTrue(w.S.Bookings.Reserve(student, second, null).Succeeded);
            }
        }

        [TestMethod]
        public void UnverifiedTutor_CannotPublishAvailability_AndIsNotListed()
        {
            using (var w = new World())
            {
                var tutor = w.NewTutor(verify: false);
                var result = w.S.Availability.Add(tutor, w.TutorSubjectOf(tutor), w.Campus(), w.Clock.Today.AddDays(2),
                    TimeSpan.FromHours(9), TimeSpan.FromHours(10), 0);

                Assert.IsFalse(result.Succeeded);
                StringAssert.Contains(result.Error, "verified");
                var tutorId = w.S.Db.Tutors.Single(t => t.UserId == tutor.UserId).TutorId;
                Assert.IsFalse(w.S.TutorSearch.Search(new TutorSearchFilter { PageSize = 500 }).Items.Any(c => c.TutorId == tutorId));
                Assert.IsNull(w.S.TutorSearch.Profile(tutorId));
            }
        }

        [TestMethod]
        public void TutorCannotPublishOverlappingSlots()
        {
            using (var w = new World())
            {
                var tutor = w.NewTutor();
                var day = w.Clock.Today.AddDays(4);
                w.Slot(tutor, day, 9, 11);
                var clash = w.S.Availability.Add(tutor, w.TutorSubjectOf(tutor), w.Campus(), day, TimeSpan.FromHours(10), TimeSpan.FromHours(12), 0);
                Assert.IsFalse(clash.Succeeded);
            }
        }

        [TestMethod]
        public void PayThenTutorConfirms_FullFlow()
        {
            using (var w = new World())
            {
                var tutor = w.NewTutor(rate: 120m);
                var slot = w.Slot(tutor, w.Clock.Today.AddDays(2), 14, 15);
                var student = w.NewStudent();

                var booking = w.S.Bookings.Reserve(student, slot, "Chapter 4");
                Assert.IsTrue(booking.Succeeded, booking.Error);
                Assert.AreEqual(BookingStatus.AwaitingPayment, w.S.Db.Bookings.Find(booking.Value).Status);

                var fee = w.FeeFor(bookingId: booking.Value);
                Assert.AreEqual(120m, w.S.Db.Fees.Find(fee).Amount, "fee = hourly rate × 1 hour (SC8)");

                var paid = w.S.Payments.Pay(student, new List<int> { fee }, World.Card(), Guid.NewGuid());
                Assert.IsTrue(paid.Succeeded, paid.Error);
                w.Refresh();
                Assert.AreEqual(BookingStatus.AwaitingTutor, w.S.Db.Bookings.Find(booking.Value).Status);
                Assert.AreEqual(FeeStatus.Paid, w.S.Db.Fees.Find(fee).Status);

                var receipt = w.S.Payments.Receipt(student, paid.Value);
                Assert.AreEqual("1111", receipt.CardLast4);
                Assert.AreEqual(booking.Value, receipt.BookingId);
                Assert.IsTrue(w.Email.Sent.Any(e => e.Item2.Contains(receipt.Reference)), "receipt emailed (SC16/SC17)");

                Assert.IsTrue(w.S.Bookings.Respond(tutor, booking.Value, true, null).Succeeded);
                w.Refresh();
                Assert.AreEqual(BookingStatus.Confirmed, w.S.Db.Bookings.Find(booking.Value).Status);
            }
        }

        [TestMethod]
        public void TutorDeclinesPaidBooking_SlotReleasedAndFeeRefunded()
        {
            using (var w = new World())
            {
                var tutor = w.NewTutor();
                var slot = w.Slot(tutor, w.Clock.Today.AddDays(2), 9, 10);
                var student = w.NewStudent();
                var booking = w.S.Bookings.Reserve(student, slot, null).Value;
                var fee = w.FeeFor(bookingId: booking);
                Assert.IsTrue(w.S.Payments.Pay(student, new List<int> { fee }, World.Card(), Guid.NewGuid()).Succeeded);

                Assert.IsFalse(w.S.Bookings.Respond(tutor, booking, false, " ").Succeeded, "decline needs a reason");
                Assert.IsTrue(w.S.Bookings.Respond(tutor, booking, false, "Clinic appointment").Succeeded);

                w.Refresh();
                Assert.AreEqual(BookingStatus.Declined, w.S.Db.Bookings.Find(booking).Status);
                Assert.IsFalse(w.S.Db.TutorAvailability.Find(slot).IsBooked, "slot free for others again");
                Assert.AreEqual(FeeStatus.Refunded, w.S.Db.Fees.Find(fee).Status);
                Assert.AreEqual(1, w.S.Db.Payments.Count(p => p.Type == PaymentType.Refund && p.Student.UserId == student.UserId));

                // Another student can now take the freed slot.
                Assert.IsTrue(w.S.Bookings.Reserve(w.NewStudent(), slot, null).Succeeded);
            }
        }

        [TestMethod]
        public void UnpaidReservationExpires_AndSweepReleasesSlot()
        {
            using (var w = new World())
            {
                var tutor = w.NewTutor();
                var slot = w.Slot(tutor, w.Clock.Today.AddDays(2), 9, 10);
                var student = w.NewStudent();
                var booking = w.S.Bookings.Reserve(student, slot, null).Value;
                var fee = w.FeeFor(bookingId: booking);

                w.Clock.Advance(TimeSpan.FromMinutes(31));
                var summary = w.S.Maintenance.Run();
                Assert.IsTrue(summary.BookingsExpired >= 1);

                w.Refresh();
                Assert.AreEqual(BookingStatus.Expired, w.S.Db.Bookings.Find(booking).Status);
                Assert.AreEqual(FeeStatus.Cancelled, w.S.Db.Fees.Find(fee).Status);
                Assert.IsFalse(w.S.Db.TutorAvailability.Find(slot).IsBooked);

                var late = w.S.Payments.Pay(student, new List<int> { fee }, World.Card(), Guid.NewGuid());
                Assert.IsFalse(late.Succeeded, "an expired reservation can no longer be paid");
            }
        }

        [TestMethod]
        public void StudentCannotCancelInsideCutoff_ButTutorCan()
        {
            using (var w = new World())
            {
                var tutor = w.NewTutor();
                var slot = w.Slot(tutor, w.Clock.Today, 15, 16); // today 15:00, clock is 09:00 → 6 h away
                var student = w.NewStudent();
                var booking = w.S.Bookings.Reserve(student, slot, null).Value;
                Assert.IsTrue(w.S.Payments.Pay(student, new List<int> { w.FeeFor(bookingId: booking) }, World.Card(), Guid.NewGuid()).Succeeded);
                Assert.IsTrue(w.S.Bookings.Respond(tutor, booking, true, null).Succeeded);

                var byStudent = w.S.Bookings.Cancel(student, booking, ScheduleChangeReason.Medical, null);
                Assert.IsFalse(byStudent.Succeeded);
                StringAssert.Contains(byStudent.Error, "12 hours");

                Assert.IsTrue(w.S.Bookings.Cancel(tutor, booking, ScheduleChangeReason.Other, "Family emergency").Succeeded);
                w.Refresh();
                Assert.AreEqual(FeeStatus.Refunded, w.S.Db.Fees.Single(f => f.BookingId == booking).Status);
            }
        }

        [TestMethod]
        public void CompletionRecordsAttendance_TimesheetAndReviewFollow()
        {
            using (var w = new World())
            {
                var tutor = w.NewTutor(rate: 100m);
                var slot = w.Slot(tutor, w.Clock.Today, 10, 12);
                var student = w.NewStudent();
                var booking = w.S.Bookings.Reserve(student, slot, null).Value;
                Assert.IsTrue(w.S.Payments.Pay(student, new List<int> { w.FeeFor(bookingId: booking) }, World.Card(), Guid.NewGuid()).Succeeded);
                Assert.IsTrue(w.S.Bookings.Respond(tutor, booking, true, null).Succeeded);

                Assert.IsFalse(w.S.Bookings.Complete(tutor, booking, AttendanceStatus.Attended, null).Succeeded, "BR-23: not before it ends");
                w.Clock.Now = w.Clock.Today.AddHours(12).AddMinutes(5);
                Assert.IsTrue(w.S.Bookings.Complete(tutor, booking, AttendanceStatus.Attended, "Covered limits").Succeeded);

                var sheet = w.S.Timesheets.Week(tutor, w.Clock.Today);
                Assert.AreEqual(2m, sheet.TotalHours, "attended hours flow into the timesheet (UC11)");
                Assert.IsTrue(w.S.Timesheets.Submit(tutor, w.Clock.Today).Succeeded);
                var staff = w.NewStaff();
                Assert.IsTrue(w.S.Timesheets.Review(staff, sheet.TimesheetId ?? w.S.Timesheets.Week(tutor, w.Clock.Today).TimesheetId.Value, true, null).Succeeded);

                var earnings = w.S.Timesheets.Earnings(tutor, w.Clock.Today.AddDays(-1), w.Clock.Today);
                Assert.AreEqual(200m, earnings.Gross);
                Assert.AreEqual(160m, earnings.Net, "20% default commission");

                Assert.IsTrue(w.S.Reviews.CanReview(student, booking));
                Assert.IsTrue(w.S.Reviews.Create(student, booking, 5, "Very clear").Succeeded);
                Assert.IsFalse(w.S.Reviews.Create(student, booking, 4, "again").Succeeded, "BR-17: once per booking");
            }
        }

        [TestMethod]
        public void PostponementApproved_MovesBookingAndKeepsOriginalInHistory()
        {
            using (var w = new World())
            {
                var tutor = w.NewTutor();
                var day = w.Clock.Today.AddDays(3);
                var slot = w.Slot(tutor, day, 9, 10);
                var student = w.NewStudent();
                var booking = w.S.Bookings.Reserve(student, slot, null).Value;
                Assert.IsTrue(w.S.Payments.Pay(student, new List<int> { w.FeeFor(bookingId: booking) }, World.Card(), Guid.NewGuid()).Succeeded);
                Assert.IsTrue(w.S.Bookings.Respond(tutor, booking, true, null).Succeeded);

                var wrongLength = w.S.Postponements.Propose(student, booking, day.AddDays(1), TimeSpan.FromHours(9), TimeSpan.FromHours(11), ScheduleChangeReason.ExamPreparation, null);
                Assert.IsFalse(wrongLength.Succeeded, "must keep the paid session length");
                Assert.IsTrue(w.S.Postponements.Propose(student, booking, day.AddDays(1), TimeSpan.FromHours(13), TimeSpan.FromHours(14), ScheduleChangeReason.ExamPreparation, "Test moved").Succeeded);
                Assert.IsFalse(w.S.Postponements.Propose(student, booking, day.AddDays(2), TimeSpan.FromHours(13), TimeSpan.FromHours(14), ScheduleChangeReason.Other, null).Succeeded, "one pending proposal");

                var pending = w.S.Postponements.PendingForTutor(tutor).Single();
                Assert.IsTrue(w.S.Postponements.Approve(tutor, pending.PostponementId).Succeeded);

                w.Refresh();
                var moved = w.S.Db.Bookings.Find(booking);
                Assert.AreEqual(day.AddDays(1), moved.SessionDate);
                Assert.AreEqual(TimeSpan.FromHours(13), moved.StartTime);
                Assert.IsFalse(w.S.Db.TutorAvailability.Find(slot).IsBooked, "old slot released");
                Assert.AreEqual(day, w.S.Db.PostponementRequests.Single(p => p.BookingId == booking).OriginalDate);
            }
        }
    }
}
