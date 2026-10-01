using System;
using StudyLink.Data.Entities;
using StudyLink.Services.DTOs;
using StudyLink.Services.Security;
using StudyLink.Web.Infrastructure;

namespace StudyLink.Web.Operations
{
    /// <summary>Counter returns: scan a copy, record its condition; overdue and damage fines are raised automatically.</summary>
    [RequirePermission(Permission.ProcessReturns)]
    public partial class Returns : BasePage
    {
        private int? RentalId => QueryInt("rental");

        protected void Page_Load(object sender, EventArgs e)
        {
            Subheading = "Receive returned books and follow up overdue rentals.";
            TabStrip.Text = Ui.Tabs(Request, "view", ("", "On loan"), ("overdue", "Overdue"), ("returned", "Returned"), ("all", "All"));
            if (IsPostBack) return;
            Ui.FillEnum<CopyCondition>(Condition);
            Ui.Select(Condition, ((int)CopyCondition.Good).ToString());
            if (Query("overdue") == "1") { Go("~/Operations/Returns.aspx?view=overdue"); return; }
            BindRental();
            BindList();
        }

        private void BindRental()
        {
            var r = RentalId.HasValue ? S.Rentals.Get(Actor, RentalId.Value) : null;
            ReturnPanel.Visible = r != null;
            if (r == null) return;
            RentalRef.Text = H(r.Reference);
            RentalStatus.Text = r.IsOverdue ? "<span class=\"badge badge--danger\">" + r.DaysOverdue + " days overdue</span>" : Fmt.Badge(r.Status);
            BookTitle.Text = H(r.BookTitle);
            CopyBarcode.Text = H(r.Barcode);
            StudentName.Text = H(r.StudentName + " (" + r.StudentNumber + ")");
            CampusName.Text = H(r.Campus);
            Rented.Text = Fmt.Date(r.RentedAt);
            Due.Text = Fmt.Date(r.DueDate);
            Accrued.Text = Fmt.Money(r.AccruedFine);
            Outstanding.Text = Fmt.Money(r.FinesOutstanding);
            ReturnForm.Visible = r.Status == Data.Entities.RentalStatus.Active;
        }

        private void BindList()
        {
            var view = Query("view") ?? "";
            var filter = new RentalFilter { Search = Query("q"), Page = QueryInt("page") ?? 1 };
            if (view == "") filter.Status = Data.Entities.RentalStatus.Active;
            if (view == "overdue") filter.OverdueOnly = true;
            if (view == "returned") filter.Status = Data.Entities.RentalStatus.Returned;
            var result = S.Rentals.Search(Actor, filter);
            Rows.DataSource = result.Items;
            Rows.DataBind();
            EmptyState.Text = result.Total == 0 ? Ui.Empty("library", view == "overdue" ? "No overdue rentals." : "No rentals in this list.") : "";
            Pager.Text = Ui.Pager(Request, result);
        }

        protected void Find_Click(object sender, EventArgs e)
        {
            var r = S.Rentals.FindActiveByBarcode(Actor, Barcode.Text);
            if (r == null)
            {
                Fail("No active rental was found for \"" + Barcode.Text.Trim() + "\".");
                BindList();
                return;
            }
            Go("~/Operations/Returns.aspx?rental=" + r.RentalId);
        }

        protected void Return_Click(object sender, EventArgs e)
        {
            var damage = Ui.Money(DamageFine.Text);
            if (!damage.HasValue)
            {
                Fail("Enter the damage fine as an amount (0 if none).");
                BindRental();
                return;
            }
            var result = S.Rentals.ProcessReturn(Actor, new ReturnRequest
            {
                RentalId = RentalId ?? 0,
                Condition = Ui.EnumOf<CopyCondition>(Condition) ?? CopyCondition.Good,
                DamageFine = damage.Value,
                Notes = Notes.Text
            });
            if (!Show(result))
            {
                BindRental();
                return;
            }
            var r = result.Value;
            var message = "Book received (" + r.Reference + ")." +
                (r.DaysLate > 0 ? " " + r.DaysLate + " days late: overdue fine " + Fmt.Money(r.OverdueFine) + "." : " Returned on time.") +
                (r.DamageFine > 0 ? " Damage fine " + Fmt.Money(r.DamageFine) + "." : "") +
                (r.WaitlistNotified ? " The next student on the waitlist has been notified." : "");
            RedirectWith("~/Operations/Returns.aspx", message);
        }

        protected void Lost_Click(object sender, EventArgs e)
        {
            if (Show(S.Rentals.MarkLost(Actor, RentalId ?? 0, Notes.Text)))
                RedirectWith("~/Operations/Returns.aspx", "Copy marked as lost and the replacement fine raised.");
            else BindRental();
        }
    }
}
