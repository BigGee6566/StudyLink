using System;
using System.Linq;
using StudyLink.Services.Security;
using StudyLink.Web.Infrastructure;

namespace StudyLink.Web.Supplier
{
    /// <summary>SC18 - the supplier's open orders, invoices and the hub's low stock on titles they supply.</summary>
    [RequirePermission(Permission.SupplierPortal)]
    public partial class Dashboard : BasePage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            var d = S.Dashboards.Supplier(Actor);
            Heading = "Welcome, " + d.CompanyName;
            Subheading = "Orders from Forte Books & Tuition Hub.";
            Kpis.Text = Ui.Kpis(this, d.Kpis);
            Orders.DataSource = d.OpenOrders;
            Orders.DataBind();
            NoOrders.Text = d.OpenOrders.Count == 0 ? Ui.Empty("box", "No open orders.") : "";
            Invoices.DataSource = d.RecentInvoices;
            Invoices.DataBind();
            NoInvoices.Text = d.RecentInvoices.Count == 0 ? "<p class=\"muted mb-0\">No invoices uploaded yet.</p>" : "";
            LowStock.DataSource = d.LowStockTitles.Take(8);
            LowStock.DataBind();
            NoLowStock.Text = d.LowStockTitles.Count == 0 ? "<p class=\"muted mb-0\">All your titles are above their reorder levels.</p>" : "";
        }
    }
}
