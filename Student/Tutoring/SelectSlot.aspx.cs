using System;
using System.Linq;
using StudyLink.Data.Entities;
using StudyLink.Services.Security;
using StudyLink.Web.Infrastructure;

namespace StudyLink.Web.Student.Tutoring
{
    /// <summary>SC8 - choose an open slot; Continue reserves it (race-safe) and creates the session fee.</summary>
    [RequirePermission(Permission.BookTutoring)]
    public partial class SelectSlot : BasePage
    {
        private DateTime? _selected;

        private int TutorId => QueryInt("tutor") ?? 0;

        protected void Page_Load(object sender, EventArgs e)
        {
            var profile = S.TutorSearch.Profile(TutorId);
            if (profile == null || profile.Status != TutorVerificationStatus.Verified)
            {
                RedirectWith("~/Catalogue/Tutors.aspx", "That tutor is not available for booking.", AlertKind.Warning);
                return;
            }
            Heading = "Book " + profile.Card.FullName;
            Subheading = "Choose a date and time that suits you.";
            TutorInitials.Text = H(profile.Card.Initials);
            TutorName.Text = H(profile.Card.FullName);
            TutorRate.Text = Fmt.Money(profile.Card.HourlyRate) + " per hour · " + H(profile.Card.Campus);

            if (IsPostBack) return;
            var subjects = profile.Subjects.GroupBy(s => s.SubjectId).Select(g => new Services.DTOs.LookupItem(g.Key, g.First().Subject)).ToList();
            Ui.Fill(Subject, subjects, "All subjects", QueryInt("subject"));
            BindSlots();
        }

        private void BindSlots()
        {
            var subject = Ui.Int(Subject);
            var dates = S.Availability.OpenDates(TutorId, subject);
            _selected = Ui.Date(Query("date")) is DateTime d && dates.Contains(d.Date) ? d.Date : dates.Cast<DateTime?>().FirstOrDefault();
            Dates.DataSource = dates;
            Dates.DataBind();
            NoDates.Text = dates.Count == 0 ? Ui.Empty("calendar", "This tutor has no open slots right now. Please check back soon.") : "";

            var slots = _selected.HasValue ? S.Availability.OpenSlots(TutorId, _selected, subject) : new System.Collections.Generic.List<Services.DTOs.SlotRow>();
            Slots.DataSource = slots;
            Slots.DataBind();
            DateLabel.Text = _selected.HasValue ? Fmt.Date(_selected.Value) : "";
            NoSlots.Text = _selected.HasValue && slots.Count == 0 ? "<p class=\"muted mb-0\">No open times on this day.</p>" : "";
            ContinueButton.Visible = dates.Count > 0;
        }

        protected bool IsSelected(DateTime d) => _selected.HasValue && d.Date == _selected.Value;

        protected string DateUrl(DateTime d) =>
            ResolveUrl("~/Student/Tutoring/SelectSlot.aspx?tutor=" + TutorId + (Ui.Int(Subject) is int s ? "&subject=" + s : "") + "&date=" + Ui.IsoDate(d));

        protected void Subject_Changed(object sender, EventArgs e) =>
            Go("~/Student/Tutoring/SelectSlot.aspx?tutor=" + TutorId + (Ui.Int(Subject) is int s ? "&subject=" + s : ""));

        protected void Continue_Click(object sender, EventArgs e)
        {
            if (!int.TryParse(SlotId.Value, out var slotId))
            {
                Fail("Choose a time slot first.");
                BindSlots();
                return;
            }
            var result = S.Bookings.Reserve(Actor, slotId, Message.Text);
            if (!Show(result))
            {
                BindSlots();
                return;
            }
            var detail = S.Bookings.Get(Actor, result.Value);
            Go(detail.Booking.Status == BookingStatus.AwaitingPayment && detail.FeeId.HasValue
                ? "~/Student/Payments/Checkout.aspx?fees=" + detail.FeeId.Value
                : "~/Student/Tutoring/BookingConfirmed.aspx?booking=" + result.Value);
        }
    }
}