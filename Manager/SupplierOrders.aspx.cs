using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI.WebControls;
using StudyLink.Data.Entities;
using StudyLink.Services.DTOs;
using StudyLink.Services.Security;
using StudyLink.Web.Infrastructure;

namespace StudyLink.Web.Manager
{
    /// <summary>
    /// SC26 - Supplier Orders (hub side): place purchase orders, track deliveries against them and
    /// review supplier invoices (Submitted → Approved/Rejected → Paid).
    /// </summary>
    [RequirePermission(Permission.ManageSupplierOrders)]
    public partial class SupplierOrders : BasePage
    {
        private const int BlankLines = 6;
        private IList<LookupItem> _books;
        private IList<LookupItem> _campuses;

        private string Tab => Query("tab") ?? "";
        private int? OrderId => QueryInt("id");

        protected void Page_Load(object sender, EventArgs e)
        {
            Subheading = "Purchase orders, deliveries and supplier invoices.";
            TabStrip.Text = Ui.Tabs(Request, "tab", ("", "Orders"), ("new", "New order"), ("invoices", "Invoices"));
            if (IsPostBack) return;

            if (OrderId.HasValue) ShowDetail();
            else if (Tab == "new") ShowNew();
            else if (Tab == "invoices") ShowInvoices();
            else ShowList();
        }

        private void ShowList()
        {
            Views.SetActiveView(ListView);
            if (StatusFilter.Items.Count == 0)
            {
                Ui.FillEnum<SupplierOrderStatus>(StatusFilter, "All statuses");
                Ui.Select(StatusFilter, Query("status"));
            }
            var rows = S.SupplierOrders.Orders(Actor, Ui.EnumOf<SupplierOrderStatus>(StatusFilter));
            OrderRows.DataSource = rows;
            OrderRows.DataBind();
            NoOrders.Text = rows.Count == 0 ? Ui.Empty("box", "No orders yet.") : "";
        }

        protected void StatusFilter_Changed(object sender, EventArgs e) => Go("~/Manager/SupplierOrders.aspx?status=" + StatusFilter.SelectedValue);

        // ------------------------------------------------------------------ new order

        private void ShowNew()
        {
            Views.SetActiveView(NewView);
            Ui.Fill(NewSupplier, S.Lookups.Suppliers());
            Expected.Text = Ui.IsoDate(DateTime.Today.AddDays(7));
            var inventory = S.Inventory.Inventory(Actor, new BookFilter { PageSize = 500 }).Items;
            _books = inventory.OrderByDescending(b => b.Stock.IsLowStock).ThenBy(b => b.Title)
                .Select(b => new LookupItem(b.BookId, (b.Stock.IsLowStock ? "▼ " : "") + b.Title + " (" + b.Stock.Available + " available)")).ToList();
            _campuses = S.Lookups.Campuses();
            NewLines.DataSource = Enumerable.Range(0, BlankLines);
            NewLines.DataBind();
        }

        protected void NewLines_ItemDataBound(object sender, RepeaterItemEventArgs e)
        {
            Ui.Fill((DropDownList)e.Item.FindControl("Book"), _books, "–");
            Ui.Fill((DropDownList)e.Item.FindControl("LineCampus"), _campuses);
        }

        protected void Create_Click(object sender, EventArgs e)
        {
            var lines = new List<NewOrderLine>();
            foreach (RepeaterItem item in NewLines.Items)
            {
                var book = Ui.Int((DropDownList)item.FindControl("Book"));
                if (!book.HasValue) continue;
                var qty = Ui.Int(((TextBox)item.FindControl("Qty")).Text);
                var cost = Ui.Money(((TextBox)item.FindControl("Cost")).Text);
                if (!qty.HasValue || !cost.HasValue)
                {
                    Fail("Each chosen title needs a quantity and a unit cost.");
                    return;
                }
                lines.Add(new NewOrderLine { BookId = book.Value, CampusId = Ui.Int((DropDownList)item.FindControl("LineCampus")) ?? 0, Quantity = qty.Value, UnitCost = cost.Value });
            }
            var result = S.SupplierOrders.Create(Actor, Ui.Int(NewSupplier) ?? 0, Ui.Date(Expected.Text), OrderNotes.Text, lines);
            if (Show(result)) RedirectWith("~/Manager/SupplierOrders.aspx?id=" + result.Value, "Order placed. The supplier has been notified.");
        }

        // ------------------------------------------------------------------ order detail

        private void ShowDetail()
        {
            Views.SetActiveView(DetailView);
            var d = S.SupplierOrders.Get(Actor, OrderId.Value);
            if (d == null)
            {
                RedirectWith("~/Manager/SupplierOrders.aspx", "That order could not be found.", AlertKind.Warning);
                return;
            }
            var o = d.Order;
            Heading = "Order " + o.Reference;
            Subheading = o.Supplier + " · ordered " + Fmt.Date(o.OrderedAt) + (o.ExpectedDate.HasValue ? " · expected " + Fmt.Date(o.ExpectedDate.Value) : "") +
                         (d.AcknowledgedAt.HasValue ? " · acknowledged " + Fmt.Date(d.AcknowledgedAt.Value) : " · not yet acknowledged");
            DetailStatus.Text = Fmt.Badge(o.Status);
            DetailLines.DataSource = d.Lines;
            DetailLines.DataBind();
            DetailDeliveries.DataSource = d.Deliveries;
            DetailDeliveries.DataBind();
            NoDeliveries.Text = d.Deliveries.Count == 0 ? "<p class=\"muted mb-0\">Nothing dispatched yet.</p>" : "";
            DetailInvoices.DataSource = d.Invoices;
            DetailInvoices.DataBind();
            NoInvoices.Text = d.Invoices.Count == 0 ? "<p class=\"muted mb-0\">No invoices yet.</p>" : "";
            CancelPanel.Visible = o.Status == SupplierOrderStatus.Placed || o.Status == SupplierOrderStatus.Acknowledged;
        }

        protected void Cancel_Click(object sender, EventArgs e)
        {
            if (Show(S.SupplierOrders.Cancel(Actor, OrderId ?? 0, CancelReason.Text)))
                RedirectWith("~/Manager/SupplierOrders.aspx?id=" + OrderId, "Order cancelled. The supplier has been notified.");
        }

        // ------------------------------------------------------------------ invoices

        private void ShowInvoices()
        {
            Views.SetActiveView(InvoicesView);
            Heading = "Supplier invoices";
            var rows = S.SupplierOrders.Invoices(Actor, null);
            InvoiceRows.DataSource = rows;
            InvoiceRows.DataBind();
            NoInvoiceRows.Text = rows.Count == 0 ? Ui.Empty("invoice", "No invoices have been submitted.") : "";
        }

        protected void Invoice_Command(object source, RepeaterCommandEventArgs e)
        {
            if (!int.TryParse((string)e.CommandArgument, out var id) || !Enum.TryParse<InvoiceStatus>(e.CommandName, out var status)) return;
            if (Show(S.SupplierOrders.ReviewInvoice(Actor, id, status, InvoiceNote.Text)))
                RedirectWith("~/Manager/SupplierOrders.aspx?tab=invoices", "Invoice marked " + Fmt.Words(status).ToLowerInvariant() + ".");
        }
    }
}
