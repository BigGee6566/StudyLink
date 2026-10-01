using System;
using System.Linq;
using StudyLink.Services.Security;
using StudyLink.Web.Infrastructure;

namespace StudyLink.Web.Student.Books
{
    /// <summary>SC13 - confirmation after paying for a rental or a purchase.</summary>
    [RequirePermission(Permission.RentOrBuyBooks)]
    public partial class RentalConfirmed : BasePage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            var receipt = QueryInt("payment") is int pid ? S.Payments.Receipt(Actor, pid) : null;
            var rentalId = receipt?.RentalId ?? QueryInt("rental");
            var saleId = receipt?.SaleId ?? QueryInt("sale");

            PaidRow.Visible = ReceiptLink.Visible = receipt != null;
            if (receipt != null)
            {
                Paid.Text = Fmt.Money(receipt.Amount) + " · " + H(receipt.CardBrand) + " ••" + H(receipt.CardLast4);
                ReceiptLink.HRef = ResolveUrl("~/Student/Fees/PaymentComplete.aspx?id=" + receipt.PaymentId);
            }

            if (rentalId.HasValue && S.Rentals.Get(Actor, rentalId.Value) is var r && r != null)
            {
                Heading = Title = "Rental confirmed";
                Headline.Text = "Rental confirmed!";
                Lead.Text = "Collect your book at the " + H(r.Campus) + " hub counter. Please bring your student card.";
                BookTitle.Text = H(r.BookTitle);
                Reference.Text = H(r.Reference);
                Barcode.Text = H(r.Barcode);
                Campus.Text = H(r.Campus);
                DueDate.Text = Fmt.Date(r.DueDate);
                LateFee.Text = Fmt.Money(r.LateFeePerDay) + " per day after the due date";
                return;
            }
            var sale = saleId.HasValue ? S.Rentals.MyPurchases(Actor).FirstOrDefault(s => s.SaleId == saleId.Value) : null;
            if (sale != null)
            {
                Heading = Title = "Purchase confirmed";
                Headline.Text = "Purchase confirmed!";
                Lead.Text = "Collect your book at the hub counter. It's yours to keep.";
                BookTitle.Text = H(sale.BookTitle);
                Reference.Text = H(sale.Reference);
                Barcode.Text = H(sale.Barcode);
                RentalRows.Visible = false;
                return;
            }
            Go("~/Student/Books/MyRentals.aspx");
        }
    }
}