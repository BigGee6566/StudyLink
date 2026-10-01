using System;
using System.Linq;
using StudyLink.Data.Entities;
using StudyLink.Services.DTOs;
using StudyLink.Services.Security;
using StudyLink.Web.Infrastructure;

namespace StudyLink.Web.Supplier
{
    /// <summary>Invoices this supplier has uploaded and their review / payment status.</summary>
    [RequirePermission(Permission.SupplierPortal)]
    public partial class Invoices : BasePage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Subheading = "Submitted, approved and paid invoices.";
            var rows = S.SupplierOrders.MyInvoices(Actor);
            Kpis.Text = Ui.Kpis(this, new[]
            {
                new Kpi("Awaiting review", Fmt.Money(rows.Where(r => r.Status == InvoiceStatus.Submitted).Sum(r => r.Amount)), null, null, "orange"),
                new Kpi("Approved, unpaid", Fmt.Money(rows.Where(r => r.Status == InvoiceStatus.Approved).Sum(r => r.Amount)), null, null, "navy"),
                new Kpi("Paid", Fmt.Money(rows.Where(r => r.Status == InvoiceStatus.Paid).Sum(r => r.Amount)), null, null, "green")
            });
            Rows.DataSource = rows;
            Rows.DataBind();
            EmptyState.Text = rows.Count == 0 ? Ui.Empty("invoice", "No invoices uploaded yet.") : "";
        }
    }
}
