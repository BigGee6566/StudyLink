using System;
using StudyLink.Services.Security;
using StudyLink.Web.Infrastructure;

namespace StudyLink.Web.Student.Fees
{
    /// <summary>SC14 - balance, unpaid items, payments with receipts and the settled-fee history.</summary>
    [RequirePermission(Permission.PayFees)]
    public partial class MyFees : BasePage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Subheading = "Everything you owe and everything you've paid.";
            var st = S.Fees.Statement(Actor);

            Balance.Text = st.Outstanding > 0
                ? "<div class=\"balance-card\"><div><div class=\"balance-card__label\">Outstanding balance</div><div class=\"balance-card__amount\">" +
                  Fmt.Money(st.Outstanding) + "</div></div><a class=\"btn btn-accent btn-lg\" href=\"" + ResolveUrl("~/Student/Fees/PayFees.aspx") + "\">Pay now</a></div>"
                : "<div class=\"balance-card balance-card--clear\"><div><div class=\"balance-card__label\">Outstanding balance</div><div class=\"balance-card__amount\">" +
                  Fmt.Money(0m) + "</div></div><span class=\"badge badge--success\">All paid up</span></div>";

            PayAll.NavigateUrl = "~/Student/Fees/PayFees.aspx";
            PayAll.Visible = st.Unpaid.Count > 0;
            Unpaid.DataSource = st.Unpaid;
            Unpaid.DataBind();
            NoUnpaid.Text = st.Unpaid.Count == 0 ? "<p class=\"muted mb-0\">Nothing to pay.</p>" : "";

            PaidYear.Text = Fmt.Money(st.PaidThisYear);
            Payments.DataSource = st.Payments;
            Payments.DataBind();
            NoPayments.Text = st.Payments.Count == 0 ? Ui.Empty(EmptyArt.Receipt, "No payments yet", "Receipts for everything you pay will be kept here.") : "";

            History.DataSource = st.History;
            History.DataBind();
            NoHistory.Text = st.History.Count == 0 ? Ui.Empty(EmptyArt.Receipt, "No settled fees yet", null) : "";
        }
    }
}