using System;
using StudyLink.Data.Entities;
using StudyLink.Services.Security;
using StudyLink.Web.Infrastructure;

namespace StudyLink.Web.Student.Fees
{
    /// <summary>SC17 - payment confirmation and printable receipt (a copy is also emailed).</summary>
    [RequirePermission(Permission.PayFees)]
    public partial class PaymentComplete : BasePage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            var r = S.Payments.Receipt(Actor, QueryInt("id") ?? 0);
            if (r == null)
            {
                RedirectWith("~/Student/Fees/MyFees.aspx", "That receipt could not be found.", AlertKind.Warning);
                return;
            }
            var refund = r.Type == PaymentType.Refund;
            Heading = refund ? "Refund" : "Payment complete";
            Subheading = "Receipt " + r.Reference;
            Headline.Text = refund ? "Refund issued" : "Payment successful!";
            Organisation.Text = H(r.Organisation);
            Amount.Text = Fmt.Money(r.Amount);
            Reference.Text = H(r.Reference);
            Date.Text = Fmt.DateTime(r.CreatedAt);
            Student.Text = H(r.StudentName + (string.IsNullOrEmpty(r.StudentNumber) ? "" : " (" + r.StudentNumber + ")"));
            Card.Text = string.IsNullOrEmpty(r.CardLast4) ? "–" : H(r.CardBrand) + " ending " + H(r.CardLast4);
            Gateway.Text = H(r.GatewayReference ?? "–");
            Lines.DataSource = r.Lines;
            Lines.DataBind();
            Total.Text = Fmt.Money(r.Amount);
            OutstandingAfter.Text = Fmt.Money(r.OutstandingAfter);
        }
    }
}