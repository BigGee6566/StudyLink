using System;
using System.Text;
using StudyLink.Data.Entities;
using StudyLink.Data.Seed;
using StudyLink.Services.Security;
using StudyLink.Web.Infrastructure;

namespace StudyLink.Web.Student.Tutoring
{
    /// <summary>One session from the student's side: pay, cancel, postpone (BR-16) and review (BR-19).</summary>
    [RequirePermission(Permission.BookTutoring)]
    public partial class BookingDetails : BasePage
    {
        private int BookingId => QueryInt("id") ?? 0;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                Ui.FillEnum<ScheduleChangeReason>(PostponeReason);
                Ui.FillEnum<ScheduleChangeReason>(CancelReason);
            }
            Bind();
        }

        private void Bind()
        {
            var d = S.Bookings.Get(Actor, BookingId);
            if (d == null)
            {
                RedirectWith("~/Student/Tutoring/MyBookings.aspx", "That booking could not be found.", AlertKind.Warning);
                return;
            }
            var b = d.Booking;
            Heading = b.Subject + " with " + b.TutorName;
            Subheading = Fmt.Day(b.SessionDate) + ", " + b.TimeRange;
            StatusBadge.Text = Fmt.Badge(b.Status);
            Reference.Text = H(b.Reference);
            Tutor.Text = H(b.TutorName);
            Subject.Text = H(b.Subject + " · " + b.StudyLevel);
            When.Text = H(Fmt.Day(b.SessionDate) + ", " + b.TimeRange);
            Campus.Text = H(b.Campus);
            Fee.Text = Fmt.Money(b.Fee) + (d.FeeStatus.HasValue ? " " + Fmt.Badge(d.FeeStatus.Value) : "");

            var extra = new StringBuilder();
            void Fact(string label, string value) { if (!string.IsNullOrEmpty(value)) extra.Append("<dt>").Append(label).Append("</dt><dd>").Append(H(value)).Append("</dd>"); }
            Fact("Your message", d.StudentMessage);
            Fact("Paid", d.PaidAt.HasValue ? Fmt.DateTime(d.PaidAt.Value) : null);
            Fact("Declined because", d.DeclineReason);
            Fact("Cancelled", d.CancelledAt.HasValue ? Fmt.DateTime(d.CancelledAt.Value) + " by " + d.CancelledBy : null);
            Fact("Cancellation reason", d.CancelReasonCategory.HasValue ? Fmt.Words(d.CancelReasonCategory.Value) + (string.IsNullOrEmpty(d.CancelReason) ? "" : " – " + d.CancelReason) : null);
            Fact("Attendance", d.Attendance.HasValue && d.Attendance != AttendanceStatus.NotRecorded ? Fmt.Words(d.Attendance.Value) : null);
            Fact("Tutor notes", d.TutorNotes);
            ExtraFacts.Text = extra.ToString();

            PayBox.Visible = d.CanPay && d.FeeId.HasValue;
            if (PayBox.Visible)
            {
                HoldUntil.Text = d.HoldExpiresAt.HasValue ? Fmt.DateTime(d.HoldExpiresAt.Value) : "the hold ends";
                PayLink.HRef = ResolveUrl("~/Student/Payments/Checkout.aspx?fees=" + d.FeeId.Value);
            }

            PendingPostpone.Visible = d.PendingPostponement != null;
            if (d.PendingPostponement != null)
            {
                var p = d.PendingPostponement;
                PendingText.Text = "You asked to move this session to <strong>" + H(Fmt.Day(p.ProposedDate) + ", " + Fmt.Range(p.ProposedStart, p.ProposedEnd)) +
                                   "</strong>. Waiting for the tutor to respond.";
            }
            PostponeForm.Visible = d.CanPostpone && d.PendingPostponement == null;
            CancelForm.Visible = d.CanCancel;
            CancelPolicy.Text = "Paid sessions cancelled at least " + S.Settings.GetInt(SettingKeys.CancellationCutoffHours, 24) +
                                " hours before the start are refunded in full.";
            CancelBlocked.Text = !d.CanCancel && !string.IsNullOrEmpty(d.CancelBlockedReason)
                ? "<div class=\"alert-box alert-box--info\"><div>" + H(d.CancelBlockedReason) + "</div></div>" : "";

            ReviewForm.Visible = d.CanReview && d.Review == null;
            ReviewShown.Visible = d.Review != null;
            if (d.Review != null)
                ReviewText.Text = Ui.Stars(d.Review.Rating) + "<p class=\"mt-2 mb-0\">" + H(d.Review.Comment) + "</p>";

            Postponements.DataSource = d.PostponementHistory;
            Postponements.DataBind();
            PostponeHistory.Visible = d.PostponementHistory.Count > 0;
        }

        protected void Cancel_Click(object sender, EventArgs e)
        {
            var reason = Ui.EnumOf<ScheduleChangeReason>(CancelReason) ?? ScheduleChangeReason.Other;
            if (Show(S.Bookings.Cancel(Actor, BookingId, reason, CancelNote.Text)))
                RedirectWith("~/Student/Tutoring/BookingDetails.aspx?id=" + BookingId, "The session has been cancelled.");
            else Bind();
        }

        protected void Postpone_Click(object sender, EventArgs e)
        {
            var date = Ui.Date(NewDate.Text);
            var start = Ui.Time(NewStart.Text);
            var end = Ui.Time(NewEnd.Text);
            if (!date.HasValue || !start.HasValue || !end.HasValue)
            {
                Fail("Enter the new date, start and end time.");
                Bind();
                return;
            }
            var reason = Ui.EnumOf<ScheduleChangeReason>(PostponeReason) ?? ScheduleChangeReason.Other;
            if (Show(S.Postponements.Propose(Actor, BookingId, date.Value, start.Value, end.Value, reason, PostponeNote.Text)))
                RedirectWith("~/Student/Tutoring/BookingDetails.aspx?id=" + BookingId, "Your request was sent to the tutor.");
            else Bind();
        }

        protected void Withdraw_Click(object sender, EventArgs e)
        {
            var d = S.Bookings.Get(Actor, BookingId);
            if (d?.PendingPostponement != null && Show(S.Postponements.Withdraw(Actor, d.PendingPostponement.PostponementId)))
                RedirectWith("~/Student/Tutoring/BookingDetails.aspx?id=" + BookingId, "Your postponement request was withdrawn.");
            else Bind();
        }

        protected void Review_Click(object sender, EventArgs e)
        {
            if (!int.TryParse(Rating.SelectedValue, out var rating))
            {
                Fail("Choose a star rating.");
                Bind();
                return;
            }
            if (Show(S.Reviews.Create(Actor, BookingId, rating, ReviewComment.Text)))
                RedirectWith("~/Student/Tutoring/BookingDetails.aspx?id=" + BookingId, "Thank you for your review.");
            else Bind();
        }
    }
}