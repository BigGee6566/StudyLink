using System;
using StudyLink.Services.Security;
using StudyLink.Web.Infrastructure;

namespace StudyLink.Web.Supplier
{
    /// <summary>Deliveries this supplier has dispatched and whether the hub has received them.</summary>
    [RequirePermission(Permission.SupplierPortal)]
    public partial class Deliveries : BasePage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Subheading = "Dispatched stock and the hub's receipt of it.";
            var rows = S.SupplierOrders.MyDeliveries(Actor);
            Rows.DataSource = rows;
            Rows.DataBind();
            EmptyState.Text = rows.Count == 0 ? Ui.Empty("truck", "You haven't dispatched any deliveries yet.") : "";
        }
    }
}
