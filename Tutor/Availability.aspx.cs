using System;
using System.Linq;
using System.Web.UI.WebControls;
using StudyLink.Data.Entities;
using StudyLink.Services.DTOs;
using StudyLink.Services.Security;
using StudyLink.Web.Infrastructure;

namespace StudyLink.Web.Tutor
{
    /// <summary>Publish and remove availability slots (BR-06/07/20/22). Only verified tutors can publish.</summary>
    [RequirePermission(Permission.ManageAvailability)]
    public partial class Availability : BasePage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Subheading = "Publish the times students can book you.";
            if (IsPostBack) return;
            var me = S.Profiles.Get(CurrentUser.UserId);
            var subjects = S.Subjects.TutorSubjects(Actor).Where(s => s.SubjectActive).Select(s => new LookupItem(s.Id, s.Subject + " · " + s.StudyLevel));
            Ui.Fill(TutorSubject, subjects);
            Ui.Fill(Campus, S.Lookups.Campuses(), null, me.CampusId);
            SlotDate.Text = Ui.IsoDate(DateTime.Today.AddDays(1));
            VerifyNote.Text = me.TutorStatus != TutorVerificationStatus.Verified
                ? "<div class=\"alert-box alert-box--warning\"><div>You can publish availability once hub staff have verified your profile.</div></div>" : "";
            Bind();
        }

        private void Bind()
        {
            var slots = S.Availability.ForTutor(Actor, DateTime.Today);
            Slots.DataSource = slots;
            Slots.DataBind();
            NoSlots.Text = slots.Count == 0 ? Ui.Empty("clock", "You have no published slots from today onwards.") : "";
            if (TutorSubject.Items.Count == 0)
                NoSlots.Text += "<p class=\"text-center\"><a href=\"" + ResolveUrl("~/Tutor/Subjects.aspx") + "\">Add the subjects you tutor first.</a></p>";
        }

        protected void Add_Click(object sender, EventArgs e)
        {
            var date = Ui.Date(SlotDate.Text);
            var start = Ui.Time(Start.Text);
            var end = Ui.Time(EndTime.Text);
            if (!Ui.Int(TutorSubject).HasValue || !date.HasValue || !start.HasValue || !end.HasValue)
                Fail("Choose a subject, date, start and end time.");
            else
            {
                var result = S.Availability.Add(Actor, Ui.Int(TutorSubject).Value, Ui.Int(Campus) ?? 0, date.Value, start.Value, end.Value, Ui.Int(Repeat) ?? 0);
                Show(result, result.Succeeded ? (result.Value == 1 ? "Slot published." : result.Value + " slots published.") : null);
            }
            Bind();
        }

        protected void Slot_Command(object source, RepeaterCommandEventArgs e)
        {
            if (e.CommandName == "delete" && int.TryParse((string)e.CommandArgument, out var id))
                Show(S.Availability.Delete(Actor, id), "Slot removed.");
            Bind();
        }
    }
}