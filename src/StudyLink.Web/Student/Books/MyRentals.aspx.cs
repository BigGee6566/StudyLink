using System;
using System.Web.UI.WebControls;
using StudyLink.Data.Entities;
using StudyLink.Services.DTOs;
using StudyLink.Services.Security;
using StudyLink.Web.Infrastructure;

namespace StudyLink.Web.Student.Books
{
    /// <summary>The student's rentals (renew), purchases and waitlist places.</summary>
    [RequirePermission(Permission.RentOrBuyBooks)]
    public partial class MyRentals : BasePage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Subheading = "Books on loan, purchases and waitlist.";
            if (!IsPostBack) Bind();
        }

        private void Bind()
        {
            var rentals = S.Rentals.MyRentals(Actor);
            RentalRows.DataSource = rentals;
            RentalRows.DataBind();
            NoRentals.Text = rentals.Count == 0 ? Ui.Empty("library", "You haven't rented any books yet.") : "";

            var purchases = S.Rentals.MyPurchases(Actor);
            Purchases.DataSource = purchases;
            Purchases.DataBind();
            NoPurchases.Text = purchases.Count == 0 ? "<p class=\"muted mb-0\">No purchases yet.</p>" : "";

            var waitlist = S.Catalogue.MyWaitlist(Actor);
            Waitlist.DataSource = waitlist;
            Waitlist.DataBind();
            NoWaitlist.Text = waitlist.Count == 0 ? "<p class=\"muted mb-0\">You are not waiting for any titles.</p>" : "";
        }

        protected static bool CanRenew(object item) => item is RentalRow r && r.Status == RentalStatus.Active && !r.IsOverdue;
        protected static bool IsPending(object item) => item is RentalRow r && r.Status == RentalStatus.PendingPayment;

        protected static string StatusOf(object item)
        {
            var r = (RentalRow)item;
            if (r.IsOverdue) return "<span class=\"badge badge--danger\">" + r.DaysOverdue + " days overdue</span>" +
                                    (r.AccruedFine > 0 ? "<div class=\"small text-danger mt-1\">Fine so far " + Fmt.Money(r.AccruedFine) + "</div>" : "");
            return Fmt.Badge(r.Status);
        }

        protected void Rental_Command(object source, RepeaterCommandEventArgs e)
        {
            if (e.CommandName == "renew" && int.TryParse((string)e.CommandArgument, out var id))
                Show(S.Rentals.Renew(Actor, id), "Rental renewed. The renewal fee has been added to My Fees.");
            Bind();
        }

        protected void Waitlist_Command(object source, RepeaterCommandEventArgs e)
        {
            if (e.CommandName == "leave" && int.TryParse((string)e.CommandArgument, out var id))
                Show(S.Catalogue.LeaveWaitlist(Actor, id), "You have left the waitlist.");
            Bind();
        }
    }
}