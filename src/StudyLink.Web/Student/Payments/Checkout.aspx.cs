using System;
using System.Collections.Generic;
using System.Linq;
using StudyLink.Services.Security;
using StudyLink.Web.Infrastructure;

namespace StudyLink.Web.Student.Payments
{
    /// <summary>
    /// SC9 - pay for what was just reserved (booking, rental or purchase) or any chosen fees.
    /// A checkout token kept in (encrypted) ViewState makes a double submit return the first payment.
    /// </summary>
    [RequirePermission(Permission.PayFees)]
    public partial class Checkout : BasePage
    {
        private IList<int> FeeIds
        {
            get => ViewState["fees"] as List<int> ?? new List<int>();
            set => ViewState["fees"] = value.ToList();
        }

        private Guid Token
        {
            get => ViewState["token"] is Guid g ? g : Guid.Empty;
            set => ViewState["token"] = value;
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            Subheading = "Secure card payment";
            if (IsPostBack) return;

            var reference = Query("ref");
            FeeIds = !string.IsNullOrEmpty(reference)
                ? S.Fees.Statement(Actor).Unpaid.Where(f => f.SourceReference == reference).Select(f => f.FeeId).ToList()
                : (Query("fees") ?? "").Split(',').Select(Ui.Int).Where(i => i.HasValue).Select(i => i.Value).ToList();
            Token = Guid.NewGuid();
            Bind();
        }

        private void Bind()
        {
            var summary = S.Fees.PrepareCheckout(Actor, FeeIds);
            if (!summary.Succeeded)
            {
                CheckoutArea.Visible = false;
                NothingArea.Visible = true;
                NothingMessage.Text = Ui.Empty("card", summary.Error,
                    "<a class=\"btn btn-primary\" href=\"" + ResolveUrl("~/Student/Fees/MyFees.aspx") + "\">Go to My Fees</a>");
                return;
            }
            Lines.DataSource = summary.Value.Fees;
            Lines.DataBind();
            Total.Text = Fmt.Money(summary.Value.Total);
            PayButton.Text = "Pay " + Fmt.Money(summary.Value.Total);
            HoldNote.Text = summary.Value.HoldExpiresAt.HasValue
                ? "<div class=\"alert-box alert-box--info\">" + Icons.Svg("clock") + "<div>We're holding this for you until <strong>" +
                  summary.Value.HoldExpiresAt.Value.ToString("HH:mm") + "</strong>. Pay before then to secure it.</div></div>"
                : "";
        }

        protected void Pay_Click(object sender, EventArgs e)
        {
            var result = S.Payments.Pay(Actor, FeeIds, Card.Card, Token);
            Card.Clear();
            if (!result.Succeeded)
            {
                Fail(result.Error);
                Token = Guid.NewGuid();
                Bind();
                return;
            }

            var receipt = S.Payments.Receipt(Actor, result.Value);
            if (receipt?.BookingId != null)
                Go("~/Student/Tutoring/BookingConfirmed.aspx?booking=" + receipt.BookingId + "&payment=" + result.Value);
            else if (receipt?.RentalId != null || receipt?.SaleId != null)
                Go("~/Student/Books/RentalConfirmed.aspx?payment=" + result.Value);
            else
                Go("~/Student/Fees/PaymentComplete.aspx?id=" + result.Value);
        }
    }
}