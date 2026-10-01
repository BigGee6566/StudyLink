using System;
using System.Web.UI.WebControls;
using StudyLink.Data.Entities;
using StudyLink.Services.Security;
using StudyLink.Web.Infrastructure;

namespace StudyLink.Web.Tutor
{
    /// <summary>Weekly timesheet: auto-filled from attended sessions, extra work added by hand, then submitted.</summary>
    [RequirePermission(Permission.SubmitTimesheet)]
    public partial class Timesheet : BasePage
    {
        protected bool CanEdit { get; private set; }

        private DateTime Week => Ui.Date(Query("week")) ?? DateTime.Today;

        protected void Page_Load(object sender, EventArgs e)
        {
            Subheading = "Log your hours each week and submit them for approval.";
            if (!IsPostBack) WorkDate.Text = Ui.IsoDate(DateTime.Today < Week.AddDays(7) && DateTime.Today >= Week ? DateTime.Today : Week);
            Bind();
        }

        private void Bind()
        {
            var t = S.Timesheets.Week(Actor, Week);
            CanEdit = t.CanEdit;
            WeekTitle.Text = "Week of " + Fmt.Date(t.WeekStart) + " – " + Fmt.Date(t.WeekEnd);
            Total.Text = Fmt.Hours(t.TotalHours);
            WeekStatus.Text = Fmt.Badge(t.TimesheetId.HasValue ? t.Status : TimesheetStatus.Draft);
            ReviewNote.Text = string.IsNullOrEmpty(t.ReviewNote) ? "" :
                "<div class=\"alert-box alert-box--" + (t.Status == TimesheetStatus.Rejected ? "warning" : "info") + "\"><div><strong>" +
                H(t.ReviewedBy) + ":</strong> " + H(t.ReviewNote) + "</div></div>";

            Entries.DataSource = t.Entries;
            Entries.DataBind();
            NoEntries.Text = t.Entries.Count == 0 ? Ui.Empty("sheet", "No hours logged for this week.") : "";
            AddForm.Visible = t.CanEdit;
            SubmitButton.Visible = t.CanEdit && t.Entries.Count > 0;

            PrevWeek.HRef = ResolveUrl("~/Tutor/Timesheet.aspx?week=" + Ui.IsoDate(t.WeekStart.AddDays(-7)));
            ThisWeek.HRef = ResolveUrl("~/Tutor/Timesheet.aspx");
            NextWeek.HRef = ResolveUrl("~/Tutor/Timesheet.aspx?week=" + Ui.IsoDate(t.WeekStart.AddDays(7)));

            History.DataSource = S.Timesheets.History(Actor);
            History.DataBind();
        }

        protected void Add_Click(object sender, EventArgs e)
        {
            var date = Ui.Date(WorkDate.Text);
            var hours = Ui.Money(Hours.Text);
            if (!date.HasValue || !hours.HasValue) Fail("Enter the date and the number of hours.");
            else if (Show(S.Timesheets.AddEntry(Actor, date.Value, hours.Value, Description.Text), "Entry added."))
            {
                Hours.Text = Description.Text = "";
            }
            Bind();
        }

        protected void Submit_Click(object sender, EventArgs e)
        {
            Show(S.Timesheets.Submit(Actor, Week), "Timesheet submitted for approval.");
            Bind();
        }

        protected void Entry_Command(object source, RepeaterCommandEventArgs e)
        {
            if (e.CommandName == "remove" && int.TryParse((string)e.CommandArgument, out var id))
                Show(S.Timesheets.RemoveEntry(Actor, id), "Entry removed.");
            Bind();
        }
    }
}