using System;
using StudyLink.Data.Entities;
using StudyLink.Services.Infrastructure;
using StudyLink.Services.Security;
using StudyLink.Web.Infrastructure;

namespace StudyLink.Web.Operations
{
    /// <summary>SC22 - verify, reject, suspend or reinstate tutors. Every decision is audited and notifies the tutor.</summary>
    [RequirePermission(Permission.VerifyTutors)]
    public partial class TutorVerification : BasePage
    {
        private int? TutorId => QueryInt("id");

        protected void Page_Load(object sender, EventArgs e)
        {
            Subheading = "Check identity and qualifications before tutors can be booked.";
            TabStrip.Text = Ui.Tabs(Request, "status", ("", "Pending"), ("Verified", "Verified"), ("Rejected", "Rejected"), ("Suspended", "Suspended"));
            if (!IsPostBack) Bind();
        }

        protected string OpenUrl(int tutorId) => Ui.WithQuery(Request, "id", tutorId.ToString());

        private void Bind()
        {
            var status = Ui.EnumOf<TutorVerificationStatus>(Query("status")) ?? TutorVerificationStatus.PendingVerification;
            var queue = S.Verification.Queue(Actor, status);
            Queue.DataSource = queue;
            Queue.DataBind();
            EmptyState.Text = queue.Count == 0 ? Ui.Empty("shield", status == TutorVerificationStatus.PendingVerification ? "No tutors are waiting for verification." : "No tutors in this list.") : "";

            var app = TutorId.HasValue ? S.Verification.Get(Actor, TutorId.Value) : null;
            AppPanel.Visible = app != null;
            Hint.Text = app == null ? "<div class=\"panel\"><div class=\"panel__body\">" + Ui.Empty("shield", "Choose a tutor to review their application.") + "</div></div>" : "";
            if (app == null) return;

            var s = app.Summary;
            AppName.Text = H(s.FullName);
            AppStatus.Text = Fmt.Badge(s.Status);
            AppEmail.Text = H(s.Email);
            AppCell.Text = H(app.CellNumber);
            AppCampus.Text = H(s.Campus);
            AppQualification.Text = H(s.Qualification);
            AppExperience.Text = s.ExperienceYears + " years";
            AppRate.Text = Fmt.Money(s.HourlyRate) + " per hour";
            AppSubjects.Text = H(s.Subjects);
            AppBio.Text = H(app.Bio);
            History.DataSource = app.History;
            History.DataBind();

            var undecided = s.Status == TutorVerificationStatus.PendingVerification || s.Status == TutorVerificationStatus.Rejected;
            DecideForm.Visible = ApproveButton.Visible = undecided;
            RejectButton.Visible = s.Status == TutorVerificationStatus.PendingVerification;
            SuspendButton.Visible = s.Status == TutorVerificationStatus.Verified;
            ReinstateButton.Visible = s.Status == TutorVerificationStatus.Suspended;
        }

        private void After(ServiceResult result, string message)
        {
            if (Show(result)) RedirectWith(Ui.WithQuery(Request, "id", null), message);
            else Bind();
        }

        protected void Approve_Click(object sender, EventArgs e) =>
            After(S.Verification.Decide(Actor, TutorId ?? 0, true, IdentityChecked.Checked, QualificationChecked.Checked, Notes.Text), "Tutor verified. They can now publish availability.");

        protected void Reject_Click(object sender, EventArgs e) =>
            After(S.Verification.Decide(Actor, TutorId ?? 0, false, IdentityChecked.Checked, QualificationChecked.Checked, Notes.Text), "Application rejected. The tutor has been notified.");

        protected void Suspend_Click(object sender, EventArgs e) =>
            After(S.Verification.Suspend(Actor, TutorId ?? 0, Notes.Text), "Tutor suspended.");

        protected void Reinstate_Click(object sender, EventArgs e) =>
            After(S.Verification.Reinstate(Actor, TutorId ?? 0, Notes.Text), "Tutor reinstated.");
    }
}
