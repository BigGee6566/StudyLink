using System;
using StudyLink.Services.Security;
using StudyLink.Web.Infrastructure;

namespace StudyLink.Web.Supplier
{
    /// <summary>Purchase orders the hub has placed with this supplier.</summary>
    [RequirePermission(Permission.SupplierPortal)]
    public partial class Orders : BasePage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Subheading = "Acknowledge orders, dispatch stock and invoice.";
            TabStrip.Text = Ui.Tabs(Request, "view", ("", "Open"), ("all", "All orders"));
            var rows = S.SupplierOrders.MyOrders(Actor, Query("view") != "all");
            Rows.DataSource = rows;
            Rows.DataBind();
            EmptyState.Text = rows.Count == 0 ? Ui.Empty("box", "No orders here.") : "";
        }
    }
}
