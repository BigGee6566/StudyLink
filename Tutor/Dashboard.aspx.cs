using System;
using System.Globalization;
using StudyLink.Data.Entities;
using StudyLink.Services.DTOs;
using StudyLink.Services.Security;
using StudyLink.Web.Infrastructure;

namespace StudyLink.Web.Tutor
{
    /// <summary>SC10 - the tutor's day: sessions today, utilisation, requests waiting and the timesheet week.</summary>
    [RequirePermission(Permission.ManageAvailability)]
    public partial class Dashboard : BasePage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            var d = S.Dashboards.Tutor(Actor);
            Heading = "Hi, " + d.FirstName + "!";
            Subheading = Fmt.Date(DateTime.Today) + " · " + d.SessionsToday + " session" + (d.SessionsToday == 1 ? "" : "s") + " today";

            StatusNote.Text =
                d.Status == TutorVerificationStatus.PendingVerification ? "<div class=\"alert-box alert-box--warning\">" + Icons.Svg("shield") + "<div><strong>Pending verification.</strong> Hub staff are checking your identity and qualifications. You can complete your profile and subjects now; you'll be able to publish availability once verified.</div></div>"
                : d.Status == TutorVerificationStatus.Rejected ? "<div class=\"alert-box alert-box--error\"><div>Your tutor application was not approved. Please contact the hub for details.</div></div>"
                : d.Status == TutorVerificationStatus.Suspended ? "<div class=\"alert-box alert-box--error\"><div>Your tutor profile is suspended, so students cannot book you. Please contact the hub.</div></div>"
                : "";

            Kpis.Text = Ui.Kpis(this, new[]
            {
                new Kpi("Sessions today", d.SessionsToday.ToString(CultureInfo.InvariantCulture), null, "~/Tutor/Schedule.aspx"),
                new Kpi("Utilisation (this week)", d.UtilisationPercent.ToString("0", CultureInfo.InvariantCulture) + "%", "Booked share of your published hours", null, "navy"),
                new Kpi("Awaiting your response", (d.AwaitingResponse + d.PendingPostponements).ToString(CultureInfo.InvariantCulture),
                        d.PendingPostponements > 0 ? d.PendingPostponements + " postponement request(s)" : "Paid sessions to confirm", "~/Tutor/Schedule.aspx?view=requests",
                        d.AwaitingResponse + d.PendingPostponements > 0 ? "orange" : "green"),
                new Kpi("Earnings this month", Fmt.Money(d.EarningsThisMonth), "After commission", "~/Tutor/Earnings.aspx", "green")
            });

            Today.DataSource = d.Today;
            Today.DataBind();
            NoToday.Text = d.Today.Count == 0 ? Ui.Empty("calendar", "No sessions today.") : "";
            Upcoming.DataSource = d.Upcoming;
            Upcoming.DataBind();
            NoUpcoming.Text = d.Upcoming.Count == 0 ? "<p class=\"muted mb-0\">Nothing booked yet.</p>" : "";

            WeekOf.Text = Fmt.Date(d.WeekStart);
            WeekHours.Text = Fmt.Hours(d.TimesheetHours);
            WeekStatus.Text = d.TimesheetStatus.HasValue ? Fmt.Badge(d.TimesheetStatus.Value) : "<span class=\"muted\">Not started</span>";
            OpenSlots.Text = d.OpenSlots + " open slot" + (d.OpenSlots == 1 ? "" : "s") + " published";
            RatingText.Text = d.AverageRating.HasValue ? "Average " + d.AverageRating.Value.ToString("0.0", CultureInfo.InvariantCulture) + " / 5" : "No reviews yet";
        }
    }
}