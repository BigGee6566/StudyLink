using System;
using System.Collections.Generic;
using System.IO;
using System.Web.UI.WebControls;
using StudyLink.Data.Entities;
using StudyLink.Services.Security;
using StudyLink.Web.Infrastructure;

namespace StudyLink.Web.Supplier
{
    /// <summary>Supplier side of SC26: acknowledge, dispatch (part) deliveries and upload invoices for one order.</summary>
    [RequirePermission(Permission.SupplierPortal)]
    public partial class OrderDetails : BasePage
    {
        protected bool CanDispatch { get; private set; }

        private int OrderId => QueryInt("id") ?? 0;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack) Bind();
        }

        private void Bind()
        {
            var d = S.SupplierOrders.Get(Actor, OrderId);
            if (d == null)
            {
                RedirectWith("~/Supplier/Orders.aspx", "That order could not be found.", AlertKind.Warning);
                return;
            }
            var o = d.Order;
            Heading = "Order " + o.Reference;
            Subheading = "Ordered " + Fmt.Date(o.OrderedAt) + (o.ExpectedDate.HasValue ? " · expected " + Fmt.Date(o.ExpectedDate.Value) : "") +
                         (string.IsNullOrEmpty(d.Notes) ? "" : " · " + d.Notes);
            StatusBadge.Text = Fmt.Badge(o.Status);
            AckPanel.Visible = o.Status == SupplierOrderStatus.Placed;
            CanDispatch = o.Status == SupplierOrderStatus.Acknowledged || o.Status == SupplierOrderStatus.PartiallyDelivered;
            Lines.DataSource = d.Lines;
            Lines.DataBind();
            DispatchPanel.Visible = CanDispatch;

            Deliveries.DataSource = d.Deliveries;
            Deliveries.DataBind();
            NoDeliveries.Text = d.Deliveries.Count == 0 ? "<p class=\"muted mb-0\">Nothing dispatched yet.</p>" : "";
            InvoiceList.DataSource = d.Invoices;
            InvoiceList.DataBind();
            InvoiceForm.Visible = o.Status != SupplierOrderStatus.Cancelled;
        }

        protected void Ack_Click(object sender, EventArgs e)
        {
            if (Show(S.SupplierOrders.Acknowledge(Actor, OrderId))) RedirectWith("~/Supplier/OrderDetails.aspx?id=" + OrderId, "Order acknowledged.");
        }

        protected void Dispatch_Click(object sender, EventArgs e)
        {
            var quantities = new Dictionary<int, int>();
            foreach (RepeaterItem item in Lines.Items)
            {
                var box = (TextBox)item.FindControl("Qty");
                if (!box.Visible) continue;
                var qty = Ui.Int(box.Text);
                if (!qty.HasValue || qty < 0)
                {
                    Fail("Enter a whole number of copies for each line (0 to skip).");
                    return;
                }
                if (qty > 0) quantities[Ui.Int(((HiddenField)item.FindControl("LineId")).Value) ?? 0] = qty.Value;
            }
            var result = S.SupplierOrders.Dispatch(Actor, OrderId, quantities, DispatchNote.Text);
            if (Show(result)) RedirectWith("~/Supplier/OrderDetails.aspx?id=" + OrderId, "Delivery dispatched. The hub will confirm what it receives.");
        }

        protected void Upload_Click(object sender, EventArgs e)
        {
            var amount = Ui.Money(Amount.Text);
            if (!amount.HasValue)
            {
                Fail("Enter the invoice amount.");
                return;
            }
            byte[] content = null;
            if (InvoicePdf.HasFile)
                using (var ms = new MemoryStream())
                {
                    InvoicePdf.PostedFile.InputStream.CopyTo(ms);
                    content = ms.ToArray();
                }
            var result = S.SupplierOrders.UploadInvoice(Actor, OrderId, InvoiceNumber.Text, amount.Value, InvoicePdf.FileName, content);
            if (Show(result)) RedirectWith("~/Supplier/OrderDetails.aspx?id=" + OrderId, "Invoice uploaded for review.");
        }
    }
}
