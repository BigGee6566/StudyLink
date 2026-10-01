using System;
using System.Linq;
using StudyLink.Services.Security;
using StudyLink.Web.Infrastructure;

namespace StudyLink.Web.Catalogue
{
    /// <summary>SC12 - one title: rental terms, live stock per campus, and the student's rent / buy / waitlist actions.</summary>
    public partial class BookDetails : CataloguePage
    {
        protected string LoginUrl => ResolveUrl("~/Account/Login.aspx?ReturnUrl=" + Server.UrlEncode(Request.RawUrl));

        private int BookId => QueryInt("id") ?? 0;
        private bool IsStudent => IsSignedIn && RolePermissions.Has(CurrentUser.Roles, Permission.RentOrBuyBooks);

        protected void Page_Load(object sender, EventArgs e)
        {
            if (IsPostBack) return;
            Ui.Fill(Campus, S.Lookups.Campuses(), null, QueryInt("campus") ?? PreferredCampus());
            Bind();
        }

        private void Bind()
        {
            var campusId = Ui.Int(Campus);
            var d = S.Catalogue.Detail(BookId, campusId, IsSignedIn ? Actor : null);
            if (d == null || (!d.IsActive && !IsSignedIn))
            {
                RedirectWith("~/Catalogue/Books.aspx", "That book could not be found.", AlertKind.Warning);
                return;
            }

            Title = d.Title;
            Heading = d.Title;
            Subheading = d.Authors;
            Cover.Text = d.HasCover ? "<img src=\"" + ResolveUrl("~/Handlers/BookCover.ashx?id=" + d.BookId) + "\" alt=\"Cover of " + H(d.Title) + "\" />" : "";
            CoverTitle.Text = H(d.Title);
            BookTitle.Text = H(d.Title);
            Byline.Text = H(d.Authors) + (string.IsNullOrEmpty(d.Edition) ? "" : " · " + H(d.Edition)) + (d.PublishedYear.HasValue ? " · " + d.PublishedYear : "");
            Isbn.Text = H(d.Isbn);
            CategoryName.Text = H(d.Category);
            SubjectName.Text = H(d.Subject ?? "–");
            Publisher.Text = H(d.Publisher ?? "–");
            Description.Text = H(d.Description);

            StockList.DataSource = d.Stock;
            StockList.DataBind();

            RentTerms.Visible = d.IsRentable;
            RentalFee.Text = Fmt.Money(d.RentalFee) + " <span class=\"muted fw-normal\">(" + Fmt.Money(d.WeeklyRentalFee) + "/week)</span>";
            Period.Text = d.RentalPeriodDays + " days";
            DueDate.Text = Fmt.Date(d.DueDateIfRentedToday);
            LateFee.Text = Fmt.Money(d.LateFeePerDay) + " per day";
            SaleTerms.Visible = d.IsForSale;
            SalePrice.Text = Fmt.Money(d.SalePrice);

            var available = d.AvailableAtCampus > 0;
            StudentActions.Visible = IsStudent && d.IsActive;
            VisitorActions.Visible = !IsSignedIn;
            RentButton.Visible = d.IsRentable && available && string.IsNullOrEmpty(d.AlreadyHas);
            BuyButton.Visible = d.IsForSale && available;
            WaitlistButton.Visible = !available && !d.OnWaitlist;

            var campusName = Campus.SelectedItem?.Text;
            AvailabilityNote.Text =
                !string.IsNullOrEmpty(d.AlreadyHas) ? "<div class=\"alert-box alert-box--info\"><div>" + H(d.AlreadyHas) + "</div></div>"
                : d.OnWaitlist ? "<div class=\"alert-box alert-box--info\"><div>You are on the waitlist at " + H(campusName) + ". We will notify you when a copy is returned.</div></div>"
                : !available ? "<div class=\"alert-box alert-box--warning\"><div>No copies are available at " + H(campusName) + " right now.</div></div>"
                : IsSignedIn && !IsStudent ? "<p class=\"muted\">Only student accounts can rent or buy books.</p>" : "";
        }

        protected void Campus_Changed(object sender, EventArgs e)
        {
            if (Ui.Int(Campus) is int campus) CampusPreference.Set(Context, campus);
            Go("~/Catalogue/BookDetails.aspx?id=" + BookId + "&campus=" + Campus.SelectedValue);
        }

        protected void Rent_Click(object sender, EventArgs e)
        {
            var result = S.Rentals.Reserve(Actor, BookId, Ui.Int(Campus) ?? 0);
            if (!Show(result)) { Bind(); return; }
            var rental = S.Rentals.Get(Actor, result.Value);
            Go(rental.Status == Data.Entities.RentalStatus.Active
                ? "~/Student/Books/RentalConfirmed.aspx?rental=" + rental.RentalId
                : "~/Student/Payments/Checkout.aspx?ref=" + Server.UrlEncode(rental.Reference));
        }

        protected void Buy_Click(object sender, EventArgs e)
        {
            var result = S.Rentals.Buy(Actor, BookId, Ui.Int(Campus) ?? 0);
            if (!Show(result)) { Bind(); return; }
            var sale = S.Rentals.MyPurchases(Actor).First(s => s.SaleId == result.Value);
            Go(sale.Status == Data.Entities.SaleStatus.Completed
                ? "~/Student/Books/RentalConfirmed.aspx?sale=" + sale.SaleId
                : "~/Student/Payments/Checkout.aspx?ref=" + Server.UrlEncode(sale.Reference));
        }

        protected void Waitlist_Click(object sender, EventArgs e)
        {
            Show(S.Catalogue.JoinWaitlist(Actor, BookId, Ui.Int(Campus) ?? 0), "You're on the waitlist. We'll notify you as soon as a copy is returned.");
            Bind();
        }

        protected static string StockBadge(int available) => Books.StockBadge(available);
    }
}