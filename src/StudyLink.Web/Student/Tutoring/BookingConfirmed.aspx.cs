using System;
using StudyLink.Data.Entities;
using StudyLink.Services.Security;
using StudyLink.Web.Infrastructure;

namespace StudyLink.Web.Student.Tutoring
{
    /// <summary>SC16 - confirmation after paying for (or reserving a free) session.</summary>
    [RequirePermission(Permission.BookTutoring)]
    public partial class BookingConfirmed : BasePage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            var detail = S.Bookings.Get(Actor, QueryInt("booking") ?? 0);
            if (detail == null)
            {
                Go("~/Student/Tutoring/MyBookings.aspx");
                return;
            }
            var b = detail.Booking;
            Heading = "Booking confirmed";
            Lead.Text = b.Status == BookingStatus.AwaitingTutor
                ? "Your payment was received. " + H(b.TutorName) + " has been asked to confirm the session; we'll notify you as soon as they do."
                : "Your session is booked.";
            Reference.Text = H(b.Reference);
            Tutor.Text = H(b.TutorName);
            Subject.Text = H(b.Subject + " · " + b.StudyLevel);
            When.Text = H(Fmt.Day(b.SessionDate) + ", " + b.TimeRange);
            Campus.Text = H(b.Campus);
            Status.Text = Fmt.Badge(b.Status);

            var paymentId = QueryInt("payment");
            var receipt = paymentId.HasValue ? S.Payments.Receipt(Actor, paymentId.Value) : null;
            PaidRow.Visible = receipt != null;
            ReceiptLink.Visible = receipt != null;
            if (receipt != null)
            {
                Paid.Text = Fmt.Money(receipt.Amount) + " · " + H(receipt.CardBrand) + " ••" + H(receipt.CardLast4);
                ReceiptLink.HRef = ResolveUrl("~/Student/Fees/PaymentComplete.aspx?id=" + receipt.PaymentId);
            }
        }
    }
}