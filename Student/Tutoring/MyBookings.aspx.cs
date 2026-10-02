using System;
using StudyLink.Data.Entities;
using StudyLink.Services.DTOs;
using StudyLink.Services.Security;
using StudyLink.Web.Infrastructure;

namespace StudyLink.Web.Student.Tutoring
{
    /// <summary>The student's tutoring sessions, filtered by tab.</summary>
    [RequirePermission(Permission.BookTutoring)]
    public partial class MyBookings : BasePage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Subheading = "Your tutoring sessions and their status.";
            TabStrip.Text = Ui.Tabs(Request, "view", ("", "Upcoming"), ("unpaid", "Awaiting payment"), ("completed", "Completed"), ("cancelled", "Cancelled"), ("all", "All"));

            var view = Query("view") ?? "";
            var filter = new BookingFilter { Page = QueryInt("page") ?? 1 };
            switch (view)
            {
                case "unpaid": filter.Status = BookingStatus.AwaitingPayment; break;
                case "completed": filter.Status = BookingStatus.Completed; break;
                case "cancelled": filter.Status = BookingStatus.Cancelled; break;
                case "all": break;
                default: filter.UpcomingOnly = true; break;
            }
            var result = S.Bookings.ForStudent(Actor, filter);
            Rows.DataSource = result.Items;
            Rows.DataBind();
            EmptyState.Text = result.Total == 0 ? Ui.Empty(EmptyArt.Calendar, "No sessions here yet", "When you book a tutor, the session and its status appear in this list.",
                "<a class=\"btn btn-sm btn-primary\" href=\"" + ResolveUrl("~/Catalogue/Tutors.aspx") + "\">Book a session</a>") : "";
            Pager.Text = Ui.Pager(Request, result);
        }
    }
}