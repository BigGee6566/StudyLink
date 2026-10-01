using System;
using System.Text;
using StudyLink.Data.Entities;
using StudyLink.Services.Security;
using StudyLink.Web.Infrastructure;

namespace StudyLink.Web.Tutor
{
    /// <summary>One session from the tutor's side: confirm / decline (D-4), complete with attendance, cancel.</summary>
    [RequirePermission(Permission.RespondToBookings)]
    public partial class SessionDetails : BasePage
    {
        private int BookingId => QueryInt("id") ?? 0;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                Ui.FillEnum(Attendance, null, new[] { AttendanceStatus.Attended, AttendanceStatus.StudentAbsent, AttendanceStatus.TutorAbsent });
                Ui.FillEnum<ScheduleChangeReason>(CancelReason);
            }
            Bind();
        }

        private void Bind()
        {
            var d = S.Bookings.Get(Actor, BookingId);
            if (d == null)
            {
                RedirectWith("~/Tutor/Schedule.aspx", "That session could not be found.", AlertKind.Warning);
                return;
            }
            var b = d.Booking;
            Heading = b.Subject + " with " + b.StudentName;
            Subheading = Fmt.Day(b.SessionDate) + ", " + b.TimeRange + " · " + b.Campus;
            StatusBadge.Text = Fmt.Badge(b.Status);
            Reference.Text = H(b.Reference);
            Student.Text = H(b.StudentName + (string.IsNullOrEmpty(b.StudentNumber) ? "" : " (" + b.StudentNumber + ")"));
            Contact.Text = H(d.StudentEmail) + (string.IsNullOrEmpty(d.StudentCell) ? "" : "<br />" + H(d.StudentCell));
            Subject.Text = H(b.Subject + " · " + b.StudyLevel);
            When.Text = H(Fmt.Day(b.SessionDate) + ", " + b.TimeRange);
            Campus.Text = H(b.Campus);
            Fee.Text = Fmt.Money(b.Fee) + (b.FeePaid ? " <span class=\"badge badge--success\">Paid</span>" : "");

            var extra = new StringBuilder();
            void Fact(string label, string value) { if (!string.IsNullOrEmpty(value)) extra.Append("<dt>").Append(label).Append("</dt><dd>").Append(H(value)).Append("</dd>"); }
            Fact("Student's message", d.StudentMessage);
            Fact("Declined because", d.DeclineReason);
            Fact("Cancelled", d.CancelledAt.HasValue ? Fmt.DateTime(d.CancelledAt.Value) + " by " + d.CancelledBy : null);
            Fact("Attendance", d.Attendance.HasValue && d.Attendance != AttendanceStatus.NotRecorded ? Fmt.Words(d.Attendance.Value) : null);
            Fact("Your notes", d.TutorNotes);
            ExtraFacts.Text = extra.ToString();
            ReviewBox.Text = d.Review == null ? "" :
                "<div class=\"panel\"><div class=\"panel__header\"><h2 class=\"panel__title\">Student review</h2></div><div class=\"panel__body\">" +
                Ui.Stars(d.Review.Rating) + "<p class=\"mt-2 mb-0\">" + H(d.Review.Comment) + "</p></div></div>";

            RespondForm.Visible = d.CanRespond;
            CompleteForm.Visible = d.CanComplete;
            CancelForm.Visible = d.CanCancel && !d.CanRespond;
        }

        private void After(Services.Infrastructure.ServiceResult result, string message)
        {
            if (Show(result)) RedirectWith("~/Tutor/SessionDetails.aspx?id=" + BookingId, message);
            else Bind();
        }

        protected void Confirm_Click(object sender, EventArgs e) =>
            After(S.Bookings.Respond(Actor, BookingId, true, null), "Session confirmed. The student has been notified.");

        protected void Decline_Click(object sender, EventArgs e) =>
            After(S.Bookings.Respond(Actor, BookingId, false, DeclineReason.Text), "Session declined. The student has been refunded.");

        protected void Complete_Click(object sender, EventArgs e) =>
            After(S.Bookings.Complete(Actor, BookingId, Ui.EnumOf<AttendanceStatus>(Attendance) ?? AttendanceStatus.Attended, Notes.Text), "Session completed.");

        protected void Cancel_Click(object sender, EventArgs e) =>
            After(S.Bookings.Cancel(Actor, BookingId, Ui.EnumOf<ScheduleChangeReason>(CancelReason) ?? ScheduleChangeReason.Other, CancelNote.Text),
                  "Session cancelled. The student has been notified and refunded.");
    }
}