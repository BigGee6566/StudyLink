using System;
using System.Linq;
using System.Web.UI.WebControls;
using StudyLink.Data.Entities;
using StudyLink.Services.DTOs;
using StudyLink.Services.Security;
using StudyLink.Web.Infrastructure;

namespace StudyLink.Web.Manager
{
    /// <summary>Every card payment and refund, with per-item refunds back to the original card.</summary>
    [RequirePermission(Permission.ManagePayments)]
    public partial class Payments : BasePage
    {
        protected bool CanRefund { get; private set; }

        protected void Page_Load(object sender, EventArgs e)
        {
            Subheading = "Card payments, failed attempts and refunds.";
            if (IsPostBack) return;
            Ui.FillEnum<PaymentType>(TypeFilter, "All types");
            Ui.FillEnum<PaymentStatus>(StatusList, "All statuses");
            Ui.Select(TypeFilter, Query("type"));
            Ui.Select(StatusList, Query("status"));
            From.Text = Query("from");
            To.Text = Query("to");
            SearchText.Text = Query("q");
            Bind();
        }

        private void Bind()
        {
            var result = S.Payments.Search(Actor, new PaymentFilter
            {
                From = Ui.Date(From.Text),
                To = Ui.Date(To.Text),
                Type = Ui.EnumOf<PaymentType>(TypeFilter),
                Status = Ui.EnumOf<PaymentStatus>(StatusList),
                Search = SearchText.Text,
                Page = QueryInt("page") ?? 1
            });
            Rows.DataSource = result.Items;
            Rows.DataBind();
            EmptyState.Text = result.Total == 0 ? Ui.Empty("card", "No payments match.") : "";
            Pager.Text = Ui.Pager(Request, result);
            var completed = result.Items.Where(p => p.Status == PaymentStatus.Completed).ToList();
            Totals.Text = Ui.Kpis(this, new[]
            {
                new Kpi("Received (this page)", Fmt.Money(completed.Where(p => p.Type == PaymentType.Charge).Sum(p => p.Amount)), null, null, "green"),
                new Kpi("Refunded (this page)", Fmt.Money(completed.Where(p => p.Type == PaymentType.Refund).Sum(p => p.Amount)), null, null, "orange"),
                new Kpi("Failed attempts (this page)", result.Items.Count(p => p.Status == PaymentStatus.Failed).ToString(), null, null, "red")
            });

            var receipt = QueryInt("id") is int id ? S.Payments.Receipt(Actor, id) : null;
            ReceiptPanel.Visible = receipt != null;
            if (receipt == null) return;
            CanRefund = receipt.Type == PaymentType.Charge && receipt.Status == PaymentStatus.Completed;
            ReceiptRef.Text = H(receipt.Reference);
            ReceiptStatus.Text = Fmt.Badge(receipt.Type == PaymentType.Refund ? "Refund" : receipt.Status.ToString());
            ReceiptStudent.Text = H(receipt.StudentName + " (" + receipt.StudentNumber + ") · " + receipt.StudentEmail);
            ReceiptDate.Text = Fmt.DateTime(receipt.CreatedAt);
            ReceiptCard.Text = string.IsNullOrEmpty(receipt.CardLast4) ? "–" : H(receipt.CardBrand + " ending " + receipt.CardLast4);
            ReceiptGateway.Text = H(receipt.GatewayReference ?? "–");
            ReceiptAmount.Text = Fmt.Money(receipt.Amount);
            Lines.DataSource = receipt.Lines;
            Lines.DataBind();
        }

        protected void Search_Click(object sender, EventArgs e) =>
            Go("~/Manager/Payments.aspx?from=" + From.Text + "&to=" + To.Text + "&type=" + TypeFilter.SelectedValue + "&status=" + StatusList.SelectedValue +
               "&q=" + Server.UrlEncode(SearchText.Text.Trim()));

        protected void Line_Command(object source, RepeaterCommandEventArgs e)
        {
            if (e.CommandName != "refund" || !int.TryParse((string)e.CommandArgument, out var feeId)) return;
            if (Show(S.Payments.Refund(Actor, feeId, RefundReason.Text)))
                RedirectWith(Request.RawUrl, "Refund issued to the student's card.");
            else Bind();
        }
    }
}
