using System;
using System.Linq;
using StudyLink.Services.Security;
using StudyLink.Web.Infrastructure;

namespace StudyLink.Web.Manager
{
    /// <summary>SC20 - revenue, stock, payments and the queues that need a manager. All figures are live.</summary>
    [RequirePermission(Permission.ManageBooks)]
    public partial class Dashboard : BasePage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            var d = S.Dashboards.Manager(Actor);
            Heading = "Hi, " + d.FirstName + "!";
            Subheading = "Forte Books & Tuition Hub at a glance · " + Fmt.Date(DateTime.Today);
            Kpis.Text = Ui.Kpis(this, d.Kpis);
            Trend.Text = Ui.BarChart(d.RevenueTrend, Fmt.Money);
            Payments.DataSource = d.RecentPayments.Take(8);
            Payments.DataBind();
            LowStock.DataSource = d.LowStock.Take(8);
            LowStock.DataBind();
            NoLowStock.Text = d.LowStock.Count == 0 ? "<p class=\"muted mb-0\">Every title is above its reorder level.</p>" : "";
            PendingTutors.Text = d.PendingTutors.Count + " waiting";
            DeliveriesText.Text = d.DeliveriesToReceive.Count + " in transit";
            OverdueText.Text = d.Overdue.Count + " overdue rental" + (d.Overdue.Count == 1 ? "" : "s");
        }
    }
}
