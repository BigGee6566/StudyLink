using System;
using System.Web.UI.WebControls;
using StudyLink.Data.Entities;
using StudyLink.Services.DTOs;
using StudyLink.Services.Security;
using StudyLink.Web.Infrastructure;

namespace StudyLink.Web.Tutor
{
    /// <summary>
    /// The tutor's sessions. view=requests is "Sessions" in the menu (paid bookings to confirm and
    /// postponement requests); view=past is "Attendance" (sessions to mark attended and complete).
    /// </summary>
    [RequirePermission(Permission.RespondToBookings)]
    public partial class Schedule : BasePage
    {
        private string View => Query("view") ?? "";

        protected void Page_Load(object sender, EventArgs e)
        {
            TabStrip.Text = Ui.Tabs(Request, "view", ("", "Upcoming"), ("requests", "Requests"), ("past", "Past & attendance"), ("all", "All"));
            Heading = View == "requests" ? "Sessions" : View == "past" ? "Attendance" : "Schedule";
            Subheading = View == "requests" ? "Paid bookings waiting for you to confirm, and postponement requests."
                       : View == "past" ? "Record attendance and complete past sessions."
                       : "Your confirmed and upcoming sessions.";
            if (!IsPostBack) Bind();
        }

        private void Bind()
        {
            var filter = new BookingFilter { Page = QueryInt("page") ?? 1 };
            switch (View)
            {
                case "requests": filter.Status = BookingStatus.AwaitingTutor; ListTitle.Text = "Sessions to confirm"; break;
                case "past": filter.To = DateTime.Today; filter.Status = BookingStatus.Confirmed; ListTitle.Text = "Past sessions to complete"; break;
                case "all": ListTitle.Text = "All sessions"; break;
                default: filter.UpcomingOnly = true; ListTitle.Text = "Upcoming sessions"; break;
            }
            var result = S.Bookings.ForTutor(Actor, filter);
            Rows.DataSource = result.Items;
            Rows.DataBind();
            EmptyState.Text = result.Total == 0 ? Ui.Empty("calendar", View == "past" ? "No sessions are waiting to be completed." : "No sessions here.") : "";
            Pager.Text = Ui.Pager(Request, result);

            var pending = View == "requests" ? S.Postponements.PendingForTutor(Actor) : new System.Collections.Generic.List<PostponementRow>();
            Postponements.DataSource = pending;
            Postponements.DataBind();
            PostponePanel.Visible = pending.Count > 0;
        }

        protected void Postpone_Command(object source, RepeaterCommandEventArgs e)
        {
            if (!int.TryParse((string)e.CommandArgument, out var id)) return;
            if (e.CommandName == "approve") Show(S.Postponements.Approve(Actor, id), "Session moved. The student has been notified.");
            if (e.CommandName == "decline") Show(S.Postponements.Decline(Actor, id, null), "Request declined. The student has been notified.");
            Bind();
        }
    }
}