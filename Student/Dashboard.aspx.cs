using System;
using StudyLink.Services.Security;
using StudyLink.Web.Infrastructure;

namespace StudyLink.Web.Student
{
    /// <summary>SC6 - greeting, reminder, outstanding balance, upcoming sessions and books on loan.</summary>
    [RequirePermission(Permission.BookTutoring)]
    public partial class Dashboard : BasePage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            var d = S.Dashboards.Student(Actor);
            Heading = "Hi, " + d.FirstName + "!";
            Subheading = "Here's what's happening with your studies.";

            Reminder.Text = string.IsNullOrEmpty(d.Reminder) ? "" :
                "<div class=\"reminder\">" + Icons.Svg("bell") + "<span>" + H(d.Reminder) + "</span></div>";
            Balance.Text = d.Outstanding > 0
                ? "<div class=\"balance-card\"><div><div class=\"balance-card__label\">Outstanding balance · " + d.UnpaidItems + " unpaid item" + (d.UnpaidItems == 1 ? "" : "s") +
                  "</div><div class=\"balance-card__amount\">" + Fmt.Money(d.Outstanding) + "</div></div><a class=\"btn btn-accent\" href=\"" +
                  ResolveUrl("~/Student/Fees/PayFees.aspx") + "\">Pay now</a></div>"
                : "<div class=\"balance-card balance-card--clear\"><div><div class=\"balance-card__label\">Outstanding balance</div><div class=\"balance-card__amount\">" +
                  Fmt.Money(0m) + "</div></div><span class=\"badge badge--success\">All paid up</span></div>";

            Sessions.DataSource = d.Upcoming;
            Sessions.DataBind();
            NoSessions.Text = d.Upcoming.Count == 0 ? Ui.Empty(EmptyArt.Calendar, "No upcoming sessions", "Book a verified tutor and your sessions will show here.", "<a class=\"btn btn-sm btn-primary\" href=\"" + ResolveUrl("~/Catalogue/Tutors.aspx") + "\">Find a tutor</a>") : "";
            Rentals.DataSource = d.Rentals;
            Rentals.DataBind();
            NoRentals.Text = d.Rentals.Count == 0 ? Ui.Empty(EmptyArt.Books, "No books on loan", "Rent a prescribed textbook and it will show here with its due date.") : "";
        }
    }
}