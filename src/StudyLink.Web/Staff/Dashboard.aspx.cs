using System;
using System.Linq;
using StudyLink.Services.Security;
using StudyLink.Web.Infrastructure;

namespace StudyLink.Web.Staff
{
    /// <summary>SC19 - the counter's day: verification queue, overdue books, deliveries and support.</summary>
    [RequirePermission(Permission.ProcessReturns)]
    public partial class Dashboard : BasePage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            var d = S.Dashboards.Staff(Actor);
            Heading = "Hi, " + d.FirstName + "!";
            Subheading = "Hub operations at a glance · " + Fmt.Date(DateTime.Today);
            Kpis.Text = Ui.Kpis(this, d.Kpis);

            Tutors.DataSource = d.PendingTutors.Take(5);
            Tutors.DataBind();
            NoTutors.Text = d.PendingTutors.Count == 0 ? "<p class=\"muted mb-0\">No tutors are waiting.</p>" : "";
            DeliveryList.DataSource = d.DeliveriesToReceive.Take(5);
            DeliveryList.DataBind();
            NoDeliveries.Text = d.DeliveriesToReceive.Count == 0 ? "<p class=\"muted mb-0\">Nothing in transit.</p>" : "";
            Overdue.DataSource = d.Overdue.Take(6);
            Overdue.DataBind();
            NoOverdue.Text = d.Overdue.Count == 0 ? "<p class=\"muted mb-0\">No overdue books.</p>" : "";
        }
    }
}
