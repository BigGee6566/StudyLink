using System;
using StudyLink.Data.Entities;
using StudyLink.Services.Security;
using StudyLink.Web.Infrastructure;

namespace StudyLink.Web.Operations
{
    /// <summary>Approve or send back tutors' weekly timesheets.</summary>
    [RequirePermission(Permission.ReviewTimesheets)]
    public partial class Timesheets : BasePage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Subheading = "Weekly tutor hours waiting for approval.";
            TabStrip.Text = Ui.Tabs(Request, "status", ("", "Submitted"), ("Approved", "Approved"), ("Rejected", "Sent back"), ("all", "All"));
            if (!IsPostBack) Bind();
        }

        private void Bind()
        {
            var statusText = Query("status");
            var status = statusText == "all" ? (TimesheetStatus?)null : Ui.EnumOf<TimesheetStatus>(statusText) ?? TimesheetStatus.Submitted;
            var rows = S.Timesheets.ForReview(Actor, status);
            Rows.DataSource = rows;
            Rows.DataBind();
            EmptyState.Text = rows.Count == 0 ? Ui.Empty("sheet", "No timesheets in this list.") : "";

            var t = QueryInt("id") is int id ? S.Timesheets.Get(Actor, id) : null;
            DetailPanel.Visible = t != null;
            if (t == null) return;
            SheetTitle.Text = H(t.TutorName) + " · week of " + Fmt.Date(t.WeekStart);
            SheetStatus.Text = Fmt.Badge(t.Status);
            SheetTotal.Text = Fmt.Hours(t.TotalHours);
            Entries.DataSource = t.Entries;
            Entries.DataBind();
            ReviewForm.Visible = t.Status == TimesheetStatus.Submitted;
        }

        private void Review(bool approve)
        {
            if (Show(S.Timesheets.Review(Actor, QueryInt("id") ?? 0, approve, Note.Text)))
                RedirectWith("~/Operations/Timesheets.aspx", approve ? "Timesheet approved." : "Timesheet sent back to the tutor.");
            else Bind();
        }

        protected void Approve_Click(object sender, EventArgs e) => Review(true);
        protected void Reject_Click(object sender, EventArgs e) => Review(false);
    }
}
