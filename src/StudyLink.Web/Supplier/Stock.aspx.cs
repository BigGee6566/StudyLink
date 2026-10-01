using System;
using StudyLink.Services.Security;
using StudyLink.Web.Infrastructure;

namespace StudyLink.Web.Supplier
{
    /// <summary>The hub's live stock of the titles this supplier is the preferred supplier for.</summary>
    [RequirePermission(Permission.SupplierPortal)]
    public partial class Stock : BasePage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Subheading = "Hub stock of the titles you supply, so you can plan ahead.";
            var rows = S.SupplierOrders.SuppliedTitles(Actor);
            Rows.DataSource = rows;
            Rows.DataBind();
            EmptyState.Text = rows.Count == 0 ? Ui.Empty("library", "You are not the preferred supplier for any titles yet.") : "";
        }
    }
}
