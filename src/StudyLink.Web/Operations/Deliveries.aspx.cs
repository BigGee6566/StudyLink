using System;
using System.Collections.Generic;
using System.Web.UI.WebControls;
using StudyLink.Services.Security;
using StudyLink.Web.Infrastructure;

namespace StudyLink.Web.Operations
{
    /// <summary>Receive supplier deliveries. Accepted quantities become new book copies (stock) at each line's campus.</summary>
    [RequirePermission(Permission.ReceiveDeliveries)]
    public partial class Deliveries : BasePage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Subheading = "Check dispatched stock against the delivery note and receive it into inventory.";
            if (!IsPostBack) Bind();
        }

        private void Bind()
        {
            var list = S.SupplierOrders.DeliveriesToReceive(Actor);
            List.DataSource = list;
            List.DataBind();
            EmptyState.Text = list.Count == 0 ? "<div class=\"panel\"><div class=\"panel__body\">" + Ui.Empty("truck", "No deliveries are waiting to be received.") + "</div></div>" : "";
        }

        protected void Delivery_Command(object source, RepeaterCommandEventArgs e)
        {
            if (!int.TryParse((string)e.CommandArgument, out var deliveryId)) return;
            var note = ((TextBox)e.Item.FindControl("Note")).Text;
            if (e.CommandName == "reject")
            {
                if (Show(S.SupplierOrders.RejectDelivery(Actor, deliveryId, note))) RedirectWith("~/Operations/Deliveries.aspx", "Delivery rejected. The supplier has been notified.");
                return;
            }

            var accepted = new Dictionary<int, int>();
            foreach (RepeaterItem line in ((Repeater)e.Item.FindControl("Lines")).Items)
            {
                var id = Ui.Int(((HiddenField)line.FindControl("LineId")).Value);
                var qty = Ui.Int(((TextBox)line.FindControl("Accepted")).Text);
                if (!id.HasValue || !qty.HasValue)
                {
                    Fail("Enter the accepted quantity for every line.");
                    return;
                }
                accepted[id.Value] = qty.Value;
            }
            if (Show(S.SupplierOrders.Receive(Actor, deliveryId, accepted, note)))
                RedirectWith("~/Operations/Deliveries.aspx", "Delivery received. New copies are now in stock.");
        }
    }
}
